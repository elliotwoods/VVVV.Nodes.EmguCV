#region usings
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using SlimDX;
using SlimDX.Direct3D9;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.SlimDX;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

#endregion usings

//here you can change the vertex type
using VertexType = VVVV.Utils.SlimDX.TexturedVertex;
using System.Collections.Generic;
using Emgu.CV.CvEnum;

namespace VVVV.Nodes.EmguCV
{
    class ImageInstance
    {
        public int Width, Height;
		Image<Rgba, byte> Image;
		public bool Initialised = false;
		public Object Lock = new Object();

        public void InitialiseImage(ImageRGB imgIn)
        {
			lock (Lock)
			{
				if (imgIn == null || !imgIn.Initialised)
				{
					Image = null;
					Initialised = false;
					return;
				}
				imgIn.ImageUpdate += new EventHandler(imgIn_ImageUpdate);
				imgIn.ImageAttributesUpdate += new EventHandler<ImageAttributesChangedEventArgs>(imgIn_ImageAttributesUpdate);

				Allocate(imgIn.ImageAttributes);
			}
        }

		void imgIn_ImageAttributesUpdate(object sender, ImageAttributesChangedEventArgs e)
		{
			Allocate(e.Attributes);
		}

		void Allocate(CVImageAttributes imageAttributes)
		{
			Width = imageAttributes.Width;
			Height = imageAttributes.Height;

			Image = new Image<Rgba, byte>(Width, Height);
			Initialised = true;
		}

		void imgIn_ImageUpdate(object sender, EventArgs e)
		{
			var imageRGB = sender as ImageRGB;
			if (imageRGB != null)
			{
				try
				{
					lock (Lock)
						lock (imageRGB.GetLock())
							if (imageRGB.Ptr != null)
							{
								_isFresh = true;
								CvInvoke.cvCvtColor(imageRGB.Ptr, Image.Ptr, COLOR_CONVERSION.CV_RGB2RGBA);
							}
				}
				catch
				{

				}
			}
		}

		public void Close()
		{
			Image = null;
			Initialised = false;
		}

		bool _isFresh;
		public bool isFresh
		{
			get
			{
				if (_isFresh)
				{
					_isFresh = false;
					return true;
				} else
					return false;
			}
			set
			{
				_isFresh = isFresh;
			}
		}

		public IntPtr Ptr
		{
			get
			{
				return Image.MIplImage.imageData;
			}
		}
    }

    #region PluginInfo
    [PluginInfo(Name = "AsTexture",
                Category = "EmguCV",
                Version = "RGB",
                Help = "Converts IPLImage to Texture",
                Tags = "")]
    #endregion PluginInfo
    public class AsTextureNode : DXTextureOutPluginBase, IPluginEvaluate
    {
        #region fields & pins
        [Input("Image")]
        IDiffSpread<ImageRGB> FPinInImage;

        [Import]
        ILogger FLogger;

        //track the current texture slice
        int FCurrentSlice;

		Dictionary<int, ImageInstance> FImageInstances = new Dictionary<int, ImageInstance>();

        bool FTexReady = false;
        #endregion fields & pins

        // import host and hand it to base constructor
        [ImportingConstructor()]
        public AsTextureNode(IPluginHost host)
            : base(host)
        {
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            CheckChanges(SpreadMax);
			Update();
        }

		private void CheckChanges(int FImageInstancescount)
        {
			bool needsInit = false;
			ImageRGB imgIn;

			if (FPinInImage[0] == null)
			{
				FImageInstances.Clear();
				needsInit = true;
			} else if (FImageInstances.Count != FPinInImage.SliceCount)
			{
				needsInit = false;

				//shrink local
				if (FImageInstances.Count > FPinInImage.SliceCount)
				{
					for (int i = FPinInImage.SliceCount; i < FImageInstances.Count; i++)
						FImageInstances.Remove(i);
					needsInit = true;
				}
				//grow local
				else
				{
					for (int i = FImageInstances.Count; i < FPinInImage.SliceCount; i++)
					{
						if (FPinInImage[i] == null || !FPinInImage[0].Initialised)
							break;

						ImageInstance attribs = new ImageInstance();

						//presume BGR input
						attribs.InitialiseImage(FPinInImage[i]);
						FImageInstances.Add(i, attribs);
					}
					needsInit = true;
				}

			}

			SetSliceCount(FImageInstances.Count);

			//seems a shame to have to reinitialise absolutely everything..
			if (needsInit)
				Reinitialize();

			//check for data changes
			bool needsUpdate = false;
			for (int i = 0; i < FImageInstances.Count; i++)
			{
				if (FImageInstances[i].isFresh)
					needsUpdate = true;
			}
			//if (needsUpdate)
				//Update();
        }

        //this method gets called, when Reinitialize() was called in evaluate,
        //or a graphics device asks for its data
        protected override Texture CreateTexture(int Slice, Device device)
        {
            FLogger.Log(LogType.Debug, "Creating new texture at slice: " + Slice);

			if (FImageInstances.Count > 0)
			{
				if (FImageInstances[Slice].Initialised)
					//return new Texture(device,  Math.Max(FWidths[Slice], 1), Math.Max(FHeights[Slice], 1), 1, Usage.None, Format.R8G8B8, Pool.Managed);
					return TextureUtils.CreateTexture(device, Math.Max(FImageInstances[Slice].Width, 1), Math.Max(FImageInstances[Slice].Height, 1));
				else
					return TextureUtils.CreateTexture(device, 1, 1);
			}
			else
			{
				return TextureUtils.CreateTexture(device, 1, 1);
			}
        }

        //this method gets called, when Update() was called in evaluate,
        //or a graphics device asks for its texture, here you fill the texture with the actual data
        //this is called for each renderer, careful here with multiscreen setups, in that case
        //calculate the pixels in evaluate and just copy the data to the device texture here
        protected unsafe override void UpdateTexture(int Slice, Texture texture)
        {
			if (FImageInstances.Count < Slice)
				return;

			if (FImageInstances[Slice].Initialised)
			{
                Surface srf = texture.GetSurfaceLevel(0);
                DataRectangle rect = srf.LockRectangle(LockFlags.Discard);

				lock(FImageInstances[Slice].Lock)
					rect.Data.WriteRange(FImageInstances[Slice].Ptr, FImageInstances[Slice].Width * FImageInstances[Slice].Height * 4);

                srf.UnlockRectangle();
            }
        }
    }
}

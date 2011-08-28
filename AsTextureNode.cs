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
    class ImageAttributes
    {
        public int Width, Height;
		Image<Rgba, byte> Image;
		public bool Initialised = false;
		public Object Lock = new Object();

        public void InitialiseImage(ImageRGB imgIn)
        {
			lock (Lock)
			{
				if (imgIn == null)
				{
					Image = null;
					Initialised = false;
					return;
				}

				Width = imgIn.Width;
				Height = imgIn.Height;
				if (Width > 0 && Height > 0)
				{
					Image = new Image<Rgba, byte>(Width, Height);
					Initialised = true;
				}
				else
				{
					Initialised = false;
				}
			}
        }

		public void Load(ImageRGB ImageSource)
		{
			if (ImageSource == null)
				Close();

			if (Initialised && ImageSource.FrameChanged)
			{
				if (ImageSource.FrameAttributesChanged)
					InitialiseImage(ImageSource);

				try
				{
					lock (Lock)
						lock (ImageSource.Lock)
							if (ImageSource.Ptr != null)
								CvInvoke.cvCvtColor(ImageSource.Ptr, Image.Ptr, COLOR_CONVERSION.CV_RGB2RGBA);
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

        Dictionary<int, ImageAttributes> FImageAttributes = new Dictionary<int,ImageAttributes>();

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
            SetSliceCount(SpreadMax);

            CheckChanges(SpreadMax);

			CheckUpdates();
        }
        private void CheckChanges(int count)
        {
			bool needsInit = false;
			ImageRGB imgIn;

			//check for change size
			if (FImageAttributes.Count != FPinInImage.SliceCount)
			{
				needsInit = true;

				//shrink local
				if (FImageAttributes.Count > FPinInImage.SliceCount)
				{
					for (int i = FPinInImage.SliceCount; i < FImageAttributes.Count; i++)
						FImageAttributes.Remove(i);
				}
				//grow local
				else
				{
					for (int i = FImageAttributes.Count; i < FPinInImage.SliceCount; i++)
					{
						ImageAttributes attribs = new ImageAttributes();

						//presume BGR input
						attribs.InitialiseImage(FPinInImage[i]);
						FImageAttributes.Add(i, attribs);
					}
				}

			}

			//check for data changes
			for (int i = 0; i < count; i++)
			{
				imgIn = FPinInImage[i];

				// this should be dealt with by ImageRGB.FrameAttributesChanged
				if (imgIn.Width != FImageAttributes[i].Width || imgIn.Height != FImageAttributes[i].Height)
				{
					ImageAttributes attribs = new ImageAttributes();

					//presume BGR input
					attribs.InitialiseImage(imgIn);
					FImageAttributes[i] = attribs;

					needsInit = true;
				}
			}

			//seems a shame to have to reinitialise absolutely everything..
			if (needsInit)
                Reinitialize();
        }

		void CheckUpdates()
		{
			foreach (KeyValuePair<int, ImageAttributes> img in FImageAttributes)
				img.Value.Load(FPinInImage[img.Key]);
			Update();
		}

        //this method gets called, when Reinitialize() was called in evaluate,
        //or a graphics device asks for its data
        protected override Texture CreateTexture(int Slice, Device device)
        {
            FLogger.Log(LogType.Debug, "Creating new texture at slice: " + Slice);

			if (FImageAttributes[Slice].Initialised)
                //return new Texture(device,  Math.Max(FWidths[Slice], 1), Math.Max(FHeights[Slice], 1), 1, Usage.None, Format.R8G8B8, Pool.Managed);
				return TextureUtils.CreateTexture(device, Math.Max(FImageAttributes[Slice].Width, 1), Math.Max(FImageAttributes[Slice].Height, 1));
            else
                return TextureUtils.CreateTexture(device, 1, 1);
            
        }

        //this method gets called, when Update() was called in evaluate,
        //or a graphics device asks for its texture, here you fill the texture with the actual data
        //this is called for each renderer, careful here with multiscreen setups, in that case
        //calculate the pixels in evaluate and just copy the data to the device texture here
        protected unsafe override void UpdateTexture(int Slice, Texture texture)
        {
			if (FImageAttributes[Slice].Initialised)
			{
                Surface srf = texture.GetSurfaceLevel(0);
                DataRectangle rect = srf.LockRectangle(LockFlags.Discard);

				lock(FImageAttributes[Slice].Lock)
					rect.Data.WriteRange(FImageAttributes[Slice].Ptr, FImageAttributes[Slice].Width * FImageAttributes[Slice].Height * 4);

                srf.UnlockRectangle();
            }
        }
    }
}

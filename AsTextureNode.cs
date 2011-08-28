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

        public void InitialiseImage(Image<Bgr, byte> imgIn)
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

		public void Resample(Image<Bgr, byte> ImageSource)
		{
			if (ImageSource == null)
				Close();

			if (Initialised)
				CvInvoke.cvCvtColor(ImageSource.Ptr, Image.Ptr, COLOR_CONVERSION.CV_RGB2RGBA);
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
                Version = "",
                Help = "Converts IPLImage to Texture",
                Tags = "")]
    #endregion PluginInfo
    public class AsTextureNode : DXTextureOutPluginBase, IPluginEvaluate
    {
        #region fields & pins
        [Input("Image")]
        IDiffSpread<Image<Bgr, byte>> FPinInImage;

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

            if (FPinInImage.IsChanged)
            {
                CheckChanges(SpreadMax);

				Update();
            }

        }
        private void CheckChanges(int count)
        {
            //this is all manual at the moment
            //ideally we flag images when they change attributes

            bool needsInit;
            bool newInit = false;
            Image<Bgr, byte> imgIn;
            for (int i=0; i<FPinInImage.SliceCount; i++)
            {
                imgIn = FPinInImage[i];
                if (FImageAttributes.ContainsKey(i))
                {
					if (imgIn != null)
					{
						if (imgIn.Width == FImageAttributes[i].Width && imgIn.Height == FImageAttributes[i].Height)
						{
							needsInit = false;
						}
						else
						{
							FImageAttributes.Remove(i);
							needsInit = true;
						}
					}
					else
					{
						needsInit = false;
					}
                } else
                    needsInit = true;

                if (needsInit)
                {
                    ImageAttributes attribs = new ImageAttributes();
                    
                    //presume BGR
                    attribs.InitialiseImage(imgIn);
                    FImageAttributes.Add(i, attribs);
                    newInit = true;
                }
            }

            if (newInit)
                Reinitialize();

			foreach (KeyValuePair<int,ImageAttributes> img in FImageAttributes)
				img.Value.Resample(FPinInImage[img.Key]);
			

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

				rect.Data.WriteRange(FImageAttributes[Slice].Ptr, FImageAttributes[Slice].Width * FImageAttributes[Slice].Height * 4);

                srf.UnlockRectangle();
            }
        }
    }
}

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
    #region PluginInfo
    [PluginInfo(Name = "AsTexture",
                Category = "EmguCV",
                Version = "RGB",
                Help = "Converts IPLImage to Texture",
                Tags = "")]
    #endregion PluginInfo
	public class AsTextureNode : DXTextureOutPluginBase
    {
        #region fields & pins
		ISpread<CVImageLink> FPinInImage;

		Map<>
        bool FTexReady = false;
        #endregion fields & pins

        // import host and hand it to base constructor
        [ImportingConstructor()]
        public AsTextureNode(IPluginHost host)
            : base(host)
        {
        }

        protected override void EvaluateInternal(int SpreadMax)
        {
            SetSliceCount(SpreadMax);
			Update();
        }

		protected override void Reinit()
		{
			Reinitialize();
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

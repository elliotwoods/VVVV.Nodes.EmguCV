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
using System.Drawing;

namespace VVVV.Nodes.EmguCV
{
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
		ISpread<CVImage> FPinInImage;

		[Import]
		ILogger FLogger;

		private Spread<AsTextureImageInstance> FImageInstances = new Spread<AsTextureImageInstance>(0);

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

		private void CheckChanges(int count)
		{
			if (FPinInImage[0] == null)
			{
				FImageInstances.SliceCount = 0;
				Reinitialize();
				return;
			}

			bool needsInit = false;

			if (FImageInstances.SliceCount != count)
			{
				FImageInstances.SliceCount = count;
				needsInit = true;
			}


			for (int i = 0; i < count; i++)
			{
				if (FImageInstances[i] == null)
				{
					FImageInstances[i] = new AsTextureImageInstance();
				}

				FImageInstances[i].Initialise(FPinInImage[i]);

				needsInit |= FImageInstances[i].ReinitialiseTexture;
			}

			SetSliceCount(count);

			//seems a shame to have to reinitialise absolutely everything..
			if (needsInit)
				Reinitialize();
		}

		//this method gets called, when Reinitialize() was called in evaluate,
		//or a graphics device asks for its data
		protected override Texture CreateTexture(int Slice, Device device)
		{
			FLogger.Log(LogType.Debug, "Creating new texture at slice: " + Slice);

			if (FImageInstances.SliceCount > 0)
			{

				if (FImageInstances[Slice].Initialised && FImageInstances[Slice].Attributes.Initialised)
					return CVImageUtils.CreateTexture(FImageInstances[Slice].Attributes, device);
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
			if (FImageInstances.SliceCount < Slice)
				return;

			if (FImageInstances[Slice].IsReady())
			{
				Surface srf = texture.GetSurfaceLevel(0);
				DataRectangle rect = srf.LockRectangle(LockFlags.Discard);

				lock (FImageInstances[Slice].Lock)
					rect.Data.WriteRange(FImageInstances[Slice].Ptr, FImageInstances[Slice].BytesPerFrame);

				srf.UnlockRectangle();
			}
		}
	}
}

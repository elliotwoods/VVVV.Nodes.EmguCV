#region using
using System.ComponentModel.Composition;
using System.Drawing;
using System;

using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;
#endregion

namespace VVVV.Nodes.EmguCV.StructuredLight
{
	#region PluginInfo
	[PluginInfo(Name = "Encode", Category = "EmguCV.StructuredLight", Help = "Encode structured light patterns", Author = "", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class EncodeNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Frame", IsSingle=true, MinValue=0)]
		IDiffSpread<int> FPinInFrame;

		[Input("Properties", IsSingle=true)]
		IDiffSpread<IPayload> FPinInProperties;

		[Output("Output")]
		ISpread<CVImageLink> FPinOutOutput;

		[Output("Status")]
		ISpread<string> FStatus;

		[Import()]
		ILogger FLogger;

		IPayload FPayload;
		CVImageLink FOutput = new CVImageLink();
		CVImage FImage = new CVImage();

		bool FFirstRun = true;
		bool FNeedsUpdate = false;
		#endregion fields&pins

		[ImportingConstructor()]
		public EncodeNode()
		{

		}

		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun)
			{
				FPinOutOutput[0] = FOutput;
				FFirstRun = false;
			}

			if (FPinInProperties.IsChanged)
			{
				FPayload = FPinInProperties[0];
				if (FPayload==null)
				{
					FStatus[0] = "Needs properties";
					FImage.Initialise(new Size(1,1), TColourFormat.L8);
					return;
				}

				FImage.Initialise(FPayload.FrameAttributes);
				FNeedsUpdate = true;
			}

			if (FNeedsUpdate || FPinInFrame.IsChanged)
			{
				Update();
				FNeedsUpdate = false;
			}
		}

		unsafe void Update()
		{
			int frame = FPinInFrame[0];

			byte* outPix = (byte*)FImage.Data.ToPointer();

			fixed (ulong* inPix = &FPayload.Data[0])
			{
				ulong* mov = inPix;

				if (FPayload.Balanced)
				{
					byte high = frame % 2 == 0 ? (byte)255 : (byte)0;
					byte low = high == (byte)255 ? (byte)0 : (byte)255;

					frame /= 2;
				
					for (uint y = 0; y < FPayload.Height; y++)
						for (uint x = 0; x < FPayload.Width; x++)
							*outPix++ = (*mov++ & (ulong)1 << frame) == ((ulong)1 << (int)frame) ? high : low;
				}
				else
					for (uint y = 0; y < FPayload.Height; y++)
						for (uint x = 0; x < FPayload.Width; x++)
							*outPix++ = (*mov++ & (ulong)1 << frame) == ((ulong)1 << (int)frame) ? (byte)255 : (byte)0;
			}

			FOutput.Send(FImage);
		}


		public void Dispose()
		{
			FImage.Dispose();
			FOutput.Dispose();
		}
	}
}

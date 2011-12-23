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
	[PluginInfo(Name = "CameraSpace", Category = "EmguCV.StructuredLight", Help = "Preview structured light data", Author = "", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class CameraSpaceNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input", IsSingle = true)]
		IDiffSpread<ScanSet> FPinInInput;

		[Input("Threshold", IsSingle = true, MinValue=0, MaxValue=1)]
		IDiffSpread<float> FPinInThreshold;

		[Output("Output")]
		ISpread<CVImageLink> FPinOutOutput;

		[Output("Status")]
		ISpread<string> FStatus;

		[Import()]
		ILogger FLogger;

		CVImage FOutput = new CVImage();
		ScanSet FScanSet;
		bool FFirstRun = true;

		bool FDataUpdated = false;
		bool FAttributesUpdated = false;
		bool FAllocated = false;
		#endregion fields&pins

		[ImportingConstructor()]
		public CameraSpaceNode()
		{

		}

		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun)
			{
				FPinOutOutput[0] = new CVImageLink();
				FFirstRun = false;
			}

			if (FPinInInput.IsChanged)
			{
				FScanSet = FPinInInput[0];
				if (FScanSet != null)
				{
					FScanSet.UpdateAttributes += new EventHandler(FScanSet_UpdateAttributes);
					FScanSet.UpdateData += new EventHandler(FScanSet_UpdateData);

					FAttributesUpdated = FScanSet.Initialised;
					FDataUpdated = FScanSet.DataAvailable;
				}
			}

			if (FAttributesUpdated)
			{
				FOutput.Initialise(FScanSet.CameraSize, TColourFormat.L8);
				FDataUpdated = FScanSet.DataAvailable;
				FAllocated = true;

				FAttributesUpdated = false;
			}

			if (FDataUpdated || FPinInThreshold.IsChanged)
			{
				if (FAllocated)
				{
					UpdateData();
					FPinOutOutput[0].Send(FOutput);
				}

				FDataUpdated = false;
			}
		}

		void FScanSet_UpdateData(object sender, EventArgs e)
		{
			FDataUpdated = true;
		}

		void FScanSet_UpdateAttributes(object sender, EventArgs e)
		{
			FAttributesUpdated = true;
		}

		unsafe void UpdateData()
		{
			int PixelCount = FScanSet.CameraSize.Width * FScanSet.CameraSize.Height;
			byte* p = (byte*)FOutput.Data.ToPointer();

			int factor = (int)(Math.Log((double)FScanSet.Payload.PixelCount) / Math.Log(2)) - 8;
			fixed (ulong* indexFixed = &FScanSet.Data[0])
			{
				fixed (float* strideFixed = &FScanSet.Stride[0])
				{
					float threshold = FPinInThreshold[0] * 255.0f;

					ulong* index = indexFixed;
					float* stride = strideFixed;

					ulong decoded = 0;

					if (factor > 0)
					{
						for (int i = 0; i < PixelCount; i++)
						{
							if (!FScanSet.GetValue(*index++, ref decoded))
								continue;
							if (Math.Abs(*stride++) > threshold)
								*p++ = (byte)((decoded >> factor) & ~((ulong)1 << 8));
							else
								*p++ = 0;
						}
					}
					else
					{
						for (int i = 0; i < PixelCount; i++)
						{
							decoded = FScanSet.Payload.DataInverse[*index++];
							if (Math.Abs(*stride++) > threshold)
								*p++ = (byte)((decoded << (-factor)) & ~((ulong)1 << 8));
							else
								*p++ = 0;
						}
					}

				}
			}

			FPinOutOutput[0].Send(FOutput);
		}

		public void Dispose()
		{
		}
	}
}

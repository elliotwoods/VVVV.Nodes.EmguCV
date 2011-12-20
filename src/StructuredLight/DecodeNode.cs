#region using
using System.ComponentModel.Composition;
using System.Drawing;
using System;

using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;
using System.Diagnostics;
#endregion

namespace VVVV.Nodes.EmguCV.StructuredLight
{
	#region PluginInfo
	[PluginInfo(Name = "Decode", Category = "EmguCV.StructuredLight", Help = "Decode structured light patterns", Author = "", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class DecodeNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input", IsSingle=true)]
		ISpread<CVImageLink> FPinInInput;

		[Input("Frame", IsSingle = true, MinValue = 0)]
		IDiffSpread<int> FPinInFrame;

		[Input("Apply", IsSingle = true)]
		ISpread<bool> FPinInApply;

		[Input("Reset", IsSingle = true, IsBang=true)]
		ISpread<bool> FPinInReset;

		[Input("Properties", IsSingle=true)]
		IDiffSpread<IPayload> FPinInProperties;

		[Output("Output")]
		ISpread<ScanSet> FPinOutOutput;

		[Output("Status")]
		ISpread<string> FStatus;

		[Import()]
		ILogger FLogger;

		Object FLock = new Object();
		IPayload FPayload;
		CVImageLink FInput = new CVImageLink();

		bool FAllocated = false;
		CVImage FGreyscale = new CVImage();
		CVImage FHigh = new CVImage();
		CVImage FLow = new CVImage();
		ScanSet FScanSet = new ScanSet();

		bool FFirstRun = true;
		#endregion fields&pins

		[ImportingConstructor()]
		public DecodeNode()
		{

		}

		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun)
			{
				FPinOutOutput[0] = FScanSet;
				FFirstRun = false;
			}

			if (FPinInProperties.IsChanged)
			{
				FPayload = FPinInProperties[0];
				FScanSet.Clear();

				if (FPayload != null)
				{
					FScanSet.Payload = FPayload;
				}
			}

			if (FPinInInput[0] != FInput)
			{
				FInput = FPinInInput[0];
				if (FInput != null)
				{
					AddListeners();
					FInput.ImageAttributesUpdate += new EventHandler<ImageAttributesChangedEventArgs>(FInput_ImageAttributesUpdate);
					FInput.ImageUpdate += new EventHandler(FInput_ImageUpdate);

					if (FInput.Allocated)
						Allocate();
				}
			}

			if (FPinInApply[0])
				Apply();

			if (FPinInReset[0])
				FScanSet.Clear();
		}

		EventHandler<ImageAttributesChangedEventArgs> FAttributesChangedHandler;
		EventHandler FUpdateHandler = null;

		void AddListeners()
		{
			RemoveListeners();

			FAttributesChangedHandler = new EventHandler<ImageAttributesChangedEventArgs>(FInput_ImageAttributesUpdate);
			FUpdateHandler = new EventHandler(FInput_ImageUpdate);

			FInput.ImageAttributesUpdate += FAttributesChangedHandler;
			FInput.ImageUpdate += FUpdateHandler;
		}

		void RemoveListeners()
		{
			if (FUpdateHandler == null || FInput==null)
				return;

			FInput.ImageAttributesUpdate -= FAttributesChangedHandler;
			FInput.ImageUpdate -= FUpdateHandler;
		}

		void FInput_ImageAttributesUpdate(object sender, ImageAttributesChangedEventArgs e)
		{
			Allocate();
		}

		void Allocate()
		{
			lock (FLock)
			{
				FGreyscale.Initialise(FInput.ImageAttributes.Size, TColourFormat.L8);
				FHigh.Initialise(FGreyscale.ImageAttributes);
				FLow.Initialise(FGreyscale.ImageAttributes);
				FScanSet.Allocate(FInput.ImageAttributes.Size);

				FAllocated = true;
			}
		}

		void FInput_ImageUpdate(object sender, EventArgs e)
		{
			
		}


		void Apply()
		{
			if (!FAllocated || FPayload == null)
				return;

			int frame = (int)FPinInFrame[0];

			lock (FLock)
			{
				if (FPayload.Balanced)
				{
					bool positive = frame % 2 == 0;
					FInput.GetImage(positive ? FHigh : FLow);

					if (!positive)
						ApplyBalanced(frame/2);
				}

				FScanSet.OnUpdateData();
			}
		}

		unsafe void ApplyBalanced(int frame)
		{
			uint CameraPixelCount = FInput.ImageAttributes.PixelsPerFrame;

			fixed (ulong* dataFixed = &FScanSet.Data[0])
			{
				fixed (float* strideFixed = &FScanSet.Stride[0])
				{
					ulong* data = dataFixed;
					float* stride = strideFixed;

					byte* high = (byte*)FHigh.Data.ToPointer();
					byte* low = (byte*)FLow.Data.ToPointer();

					for (uint i = 0; i < CameraPixelCount; i++)
					{
						*stride++ = (float)(*high - *low);

						if (*high++ > *low++)
							*data++ |= (ulong)1 << frame;
						else
							*data++ &= ~((ulong)1 << frame);

					}
				}
			}
		}

		public void Dispose()
		{
		}
	}
}

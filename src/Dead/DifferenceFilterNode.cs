#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.EmguCV
{
	#region PluginInfo
	[PluginInfo(Name = "DifferenceFilter", Category = "EmguCV", Version = "RGB32F", Help = "Pipet in image", Tags = "")]
	#endregion PluginInfo
	public class DifferenceFilterL16Node : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input", IsSingle = true)]
		ISpread<ImageL16> FPinInInput;

		[Input("Threshold")]
		IDiffSpread<Double> FPinInThreshold;


		[Input("Hold", IsBang=true)]
		IDiffSpread<bool> FPinInHold;

		[Output("Output")]
		ISpread<ImageL16> FPinOutput;

		[Output("Status")]
		ISpread<string> FStatus;

		[Import]
		ILogger FLogger;

		ImageL16 FInput;
		ImageL16 FOutput;
		Object FLock = new Object();

		Image<Gray, ushort> FHoldImage;
		Image<Gray, ushort> FThresholded;
		int FThreshold = 0;

		bool FHold = false;
		#endregion fields & pins

		[ImportingConstructor]
		public DifferenceFilterL16Node(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInInput[0] != FInput)
			{
				FInput = FPinInInput[0];
				FInput.ImageUpdate += new EventHandler(FImage_ImageUpdate);
				FOutput = new ImageL16();
				FThresholded = new Image<Gray, ushort>(new Size(FInput.Width, FInput.Height));
			}

			lock (FLock)
			{
				FHold = FPinInHold[0];
				FThreshold = (int) FPinInThreshold[0];
			}

		}

		unsafe void FImage_ImageUpdate(object sender, EventArgs e)
		{
			var image = sender as ImageL16;
			if (image == null)
				return;

			lock (FLock)
			{
				if (FHold)
					FHoldImage = image.GetImage();

				for (int i = 0; i < FWidth * FHeight; i++)
				{
					ushort* src = image.Ptr.ToPointer();
					ushort* held = FHoldImage.MIplImage.imageData.ToPointer();
					ushort* dst = FThresholded.MIplImage.imageData.ToPointer();

					if (((int)*src) - ((int)*dst) > FThreshold)
						*dst = *src;
					else
						*dst = 0;
				}

				FImageDepth.SetImage(FThresholded);

			}
		}

		void Output()
		{
			FPinOutput.SliceCount = FOutput.Count;

			for (int i = 0; i < FOutput.Count; i++)
			{
				FPinOutput[i] = FOutput[i];
			}

		}
	}
}

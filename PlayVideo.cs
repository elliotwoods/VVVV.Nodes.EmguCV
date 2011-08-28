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
using System.Threading;

#endregion usings

namespace VVVV.Nodes.EmguCV
{
	#region PluginInfo
	[PluginInfo(Name = "VideoPlayer",
				Category = "EmguCV",
				Version = "",
				Help = "Plays AVI files into IPLImage, using libavcodec(?)",
				Tags = "")]
	#endregion PluginInfo
	public class PlayVideoNode : IPluginEvaluate
	{
		#region fields & pins

		[Input("Filename", StringType = StringType.Filename)]
        IDiffSpread<string> FPinInFilename;

		[Input("Play")]
		IDiffSpread<bool> FPinInPlay;

		[Input("Loop")]
		IDiffSpread<bool> FPinInLoop;

		[Output("Image")]
		ISpread<Image<Bgr, byte>> FPinOutImage;

		[Output("Position")]
		ISpread<double> FPinOutPosition;

		[Output("Length")]
		ISpread<double> FPinOutLength;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

		[Import]
		ILogger FLogger;

		IPluginHost FHost;

		//track the current texture slice
		int FCurrentSlice;

		Thread FCaptureThread;
		Object FCaptureThreadLock = new Object();
		bool FCaptureThreadRun = false;

		Image<Bgr, byte> FImage;
		Capture FCapture;
		bool FCapturePlay = false;
		bool FCaptureLoop = false;
		double FCaptureFPS;
		double FCapturePosition;
		double FCaptureLength;
		int FCapturePeriod;
		bool FHasCapture;

		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor()]
		public PlayVideoNode(IPluginHost host)
		{
			FHost = host;
		}

		public void Dispose()
		{
			FCaptureThreadRun = false;
			FCaptureThread.Join();
			GC.SuppressFinalize(this);
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInFilename.IsChanged)
			{
				InitialiseCamera(FPinInFilename[0]);
			}

			if (FPinInPlay.IsChanged)
			{
				lock (FCaptureThreadLock)
				{
					FCapturePlay = FPinInPlay[0];
				}
			}

			if (FPinInLoop.IsChanged)
			{
				lock (FCaptureThreadLock)
				{
					FCaptureLoop = FPinInLoop[0];
				}
			}

			if (FCaptureThreadRun)
			{
				FPinOutImage[0] = FImage;
				FPinOutPosition[0] = FCapturePosition;
				FPinOutLength[0] = FCaptureLength;
			}
		}

		private void InitialiseCamera(string filename)
		{
			CloseCamera();
			try
			{
				FCapture = new Capture(filename); //create a video player
				FCaptureFPS = FCapture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FPS);
				FCaptureLength = FCapture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_COUNT) / FCaptureFPS;
				FCapturePeriod = (int)(1000.0d / FCaptureFPS);
			}
			catch
			{
				FPinOutStatus[0] = "Player open failed";
				return;
			}

			FPinOutStatus[0] = "Player open success";
			FHasCapture = true;

			FCaptureThreadRun = true;
			FCaptureThread = new Thread(fnCapture);
			FCaptureThread.Start();
		}
		private void CloseCamera()
		{
			if (FHasCapture)
				FCapture.Dispose();
			FHasCapture = false;
		}

		private void fnCapture()
		{
			while (FCaptureThreadRun)
			{
				lock (FCaptureThreadLock)
				{
					if ((FImage == null && !FCapturePlay) || FCapturePlay)
					{
						FImage = FCapture.QueryFrame();
						FPinOutImage[0] = FImage;
					}

					if (FCaptureLoop)
						if (FCapture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_FRAMES) + 1 == FCapture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_COUNT))
							FCapture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_FRAMES, 0.0d);

					FCapturePosition = FCapture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_MSEC) / 1000.0d;
				}
				Thread.Sleep(FCapturePeriod);
			}
		}
	
	}
}

#region usings

using System;
using System.ComponentModel.Composition;
using Emgu.CV.CvEnum;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

#endregion usings

namespace VVVV.Nodes.EmguCV
{
	class CaptureVideoInstance : IDisposable
	{
		public string Status;
		public CVImageLink Output = new CVImageLink();

		private int FCameraID = -1;
		private int FWidth = 0;
		private int FHeight = 0;

		private int FRequestedWidth = 0;
		private int FRequestedHeight = 0;

		Thread FCaptureThread;
		bool FCaptureRunThread;
		Object FCaptureLock = new Object();

		Capture FCapture;

		public bool IsRunning;

		Stopwatch FTimer = new Stopwatch();
		TimeSpan FFramePeriod = new TimeSpan(0);

		public int CameraID
		{
			get
			{
				return FCameraID;
			}
		}

		public int Width
		{
			get
			{
				return FWidth;
			}
		}

		public int Height
		{
			get
			{
				return FHeight;
			}
		}

		public int FramesPerSecond
		{
			get
			{
				if (FFramePeriod.TotalSeconds > 0)
					return (int)(1.0 / FFramePeriod.TotalSeconds);
				else
					return 0;
			}
		}

		public void Initialise(int id, int width, int height)
		{
			if (id == FCameraID && width == FRequestedWidth && height == FRequestedHeight)
				return;

			Close();
			lock (FCaptureLock)
			{
				try
				{
					FCapture = new Capture(id);
					FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, width);
					FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, height);
				}
				catch (Exception e)
				{
					Status = "Camera open failed";
					IsRunning = false;
					return;
				}

				Status = "OK";
				IsRunning = true;

				FCameraID = id;

				FWidth = FCapture.Width;
				FHeight = FCapture.Height;

				FRequestedWidth = width;
				FRequestedHeight = height;
			}

			FCaptureRunThread = true;
			FCaptureThread = new Thread(Capture);
			FCaptureThread.Start();
		}

		private void Capture()
		{

			FTimer.Start();

			while (FCaptureRunThread)
			{
				FFramePeriod = FTimer.Elapsed;
				FTimer.Reset();
				FTimer.Start();

				lock (FCaptureLock)
				{
					IImage capbuffer = FCapture.QueryFrame();
					if (ImageUtils.IsIntialised(capbuffer))
						Output.Send(capbuffer);
				}

				//allow a gap where we're not locked
				Thread.Sleep(1);
			}
		}

		public void Close()
		{
			if (!IsRunning) return;

			FCaptureRunThread = false;
			FCaptureThread.Join(100);
			FCapture.Dispose();
			IsRunning = false;
		}

		public void  Dispose()
		{
			Close();
		}
}
	#region PluginInfo
	[PluginInfo(Name = "VideoIn",
			  Category = "EmguCV",
			  Version = "",
			  Help = "Captures from DShow device to IPLImage",
			  Tags = "")]
	#endregion PluginInfo
	public class CaptureVideoNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins

		[Input("Camera ID", DefaultValue = 0, MinValue = 0)]
		IDiffSpread<int> FPinInCameraID;

		[Input("Width", DefaultValue = 640, MinValue = 0)]
		IDiffSpread<int> FPinInWidth;

		[Input("Height", DefaultValue = 480, MinValue = 0)]
		IDiffSpread<int> FPinInHeight;

		[Output("Image")]
		ISpread<CVImageLink> FPinOutImage;

		[Output("FPS")]
		ISpread<int> FPinOutFPS;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

		[Import]
		ILogger FLogger;

		IPluginHost FHost;
		private Spread<CaptureVideoInstance> FCaptures = new Spread<CaptureVideoInstance>(0);

		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor]
		public CaptureVideoNode(IPluginHost host)
		{
			FHost = host;
		}

		public void Dispose()
		{
			for (int i = 0; i < FCaptures.SliceCount; i++)
				FCaptures[i].Close();

			FCaptures.SliceCount = 0;

			GC.SuppressFinalize(this);
		}

		//called when data for any output pin is requested
		public void Evaluate(int spreadMax)
		{
			CheckChanges();
			
			GiveOutputs();
		}

		void GiveOutputs()
		{
			int count = FCaptures.SliceCount;

			FPinOutImage.SliceCount = count;
			FPinOutFPS.SliceCount = count;
			FPinOutStatus.SliceCount = count;

			for (int i=0; i<count; i++)
			{
				FPinOutStatus[i] = FCaptures[i].Status;
				FPinOutFPS[i] = FCaptures[i].FramesPerSecond;
				FPinOutImage[i] = FCaptures[i].Output;
			}
		}

		private void CheckChanges()
		{
			if (FCaptures.SliceCount != FPinInCameraID.SliceCount)
			{
				while (FCaptures.SliceCount < FPinInCameraID.SliceCount)
					FCaptures.Add<CaptureVideoInstance>(new CaptureVideoInstance());
				for (int iDispose = FPinInCameraID.SliceCount; iDispose < FCaptures.SliceCount; iDispose++)
					FCaptures[iDispose].Dispose();
				FCaptures.SliceCount = FPinInCameraID.SliceCount;
			}

			for (int i = 0; i < FPinInCameraID.SliceCount; i++)
			{
				if (FCaptures[i] == null)
					FCaptures[i] = new CaptureVideoInstance();


				FCaptures[i].Initialise(FPinInCameraID[i], FPinInWidth[i], FPinInHeight[i]);
			}
		}
	}
}

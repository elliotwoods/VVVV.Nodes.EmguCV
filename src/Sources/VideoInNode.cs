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
		private int FCameraID = -1;
		private int FWidth = 0;
		private int FHeight = 0;

		public string Status;

		Thread FCaptureThread;
		bool FRunCaptureThread;

		public CVImage Image = new CVImage();
		Capture FCapture;
		Object FLockCapture = new Object();

		public bool IsRunning;

		Image<Bgr, byte> FBuffer;

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
				return (int) (1.0 / FFramePeriod.TotalSeconds);
			}
		}

		public void Initialise(int id, int width, int height)
		{
			Close();
			lock (FLockCapture)
			{
				try
				{
					FCapture = new Capture(id);
					FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, width);
					FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, height);
				}
				catch
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

				FBuffer = new Image<Bgr, byte>(new Size(FWidth, FHeight));
			}

			FRunCaptureThread = true;
			FCaptureThread = new Thread(Capture);
			FCaptureThread.Start();
		}

		private void Capture()
		{

			FTimer.Start();

			while (FRunCaptureThread)
			{
				FTimer.Reset();
				FTimer.Start();

				lock (FLockCapture)
				{
					FBuffer = FCapture.QueryFrame();

					if (FBuffer != null)
						lock (Image.GetLock())
							Image.SetImage(FBuffer);
				}

				FFramePeriod = FTimer.Elapsed;

				//allow a gap where we're not locked
				Thread.Sleep(1);
			}
		}

		public void Close()
		{
			if (!IsRunning) return;

			FRunCaptureThread = false;
			FCaptureThread.Join(100);
			FCapture.Dispose();
			FBuffer.Dispose();
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
		ISpread<CVImage> FPinOutImage;

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
				FPinOutImage[i] = FCaptures[i].Image;
			}
		}

		private void CheckChanges()
		{
			if (FCaptures.SliceCount != FPinInCameraID.SliceCount)
				FCaptures.SliceCount = FPinInCameraID.SliceCount;

			for (int i = 0; i < FPinInCameraID.SliceCount; i++)
			{
				if (FCaptures[i] == null)
					FCaptures[i] = new CaptureVideoInstance();

				bool change = false;
				change |= FCaptures[i].CameraID != FPinInCameraID[i];
				change |= FCaptures[i].Width != FPinInWidth[i] && FPinInWidth.IsChanged;
				change |= FCaptures[i].Height != FPinInHeight[i] && FPinInHeight.IsChanged;

				if (change)
					FCaptures[i].Initialise(FPinInCameraID[i], FPinInWidth[i], FPinInHeight[i]);


			}
		}
	}
}

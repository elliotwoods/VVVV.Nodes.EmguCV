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

#endregion usings

namespace VVVV.Nodes.EmguCV
{
	class CaptureVideoInstance
	{
		public int CameraID = -1;
		public string Status;

		Thread FCaptureThread;
		bool FRunCaptureThread;

		public ImageRGB Image = new ImageRGB();
		Capture FCapture;
		public bool IsRunning;

		Image<Bgr, byte> FBuffer;

		public void Initialise(int id, int width, int height)
		{
			Close();
			try
			{
				FCapture = new Capture(id); //create a camera captue
				SetSize(width, height);
			}
			catch
			{
				Status = "Camera open failed";
				IsRunning = false;
				return;
			}

			Status = "Camera open success";
			IsRunning = true;

			CameraID = id;

			FBuffer = new Image<Bgr, byte>(new System.Drawing.Size(FCapture.Width, FCapture.Height));

			FRunCaptureThread = true;
			FCaptureThread = new Thread(Capture);
			FCaptureThread.Start();
		}

		private void Capture()
		{
			while (FRunCaptureThread)
			{
				
				FBuffer = FCapture.QueryFrame();
					
				lock (Image.GetLock())
					Image.SetImage(FBuffer);

				//allow a gap where we're not locked
				Thread.Sleep(5);
			}
		}

		void SetSize(int width, int height)
		{
			FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, width);
			FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, height);
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
		ISpread<int> FWidth;

		[Input("Height", DefaultValue = 480, MinValue = 0)] 
		ISpread<int> FHeight;

		[Input("SetResolution", DefaultValue = 0, IsBang = true)] 
		ISpread<bool> FSetResolution;

		[Output("Image")]
		ISpread<ImageRGB> FPinOutImage;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

		[Import]
		ILogger FLogger;

		IPluginHost FHost;
		Dictionary<int, CaptureVideoInstance> FCaptures = new Dictionary<int, CaptureVideoInstance>();

		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor]
		public CaptureVideoNode(IPluginHost host)
		{
			FHost = host;
		}

		public void Dispose()
		{
			foreach (KeyValuePair<int, CaptureVideoInstance> capture in FCaptures)
				capture.Value.Close();

			GC.SuppressFinalize(this);
		}

		//called when data for any output pin is requested
		public void Evaluate(int spreadMax)
		{
			Resize(spreadMax);
			GiveOutputs();

			for (int i = 0; i < spreadMax; i++)
			{
				if(FSetResolution[i])
				{
					FCaptures[i].Initialise(FPinInCameraID[i], FWidth[i], FHeight[i]);
				}
			}
			
		}

		void GiveOutputs()
		{
			foreach (KeyValuePair<int, CaptureVideoInstance> capture in FCaptures)
			{
				FPinOutStatus[capture.Key] = capture.Value.Status;
			}
		}

		private void Resize(int spreadMax)
		{
			FPinOutStatus.SliceCount = spreadMax;
			FPinOutImage.SliceCount = spreadMax;

			if (spreadMax == 0)
			{
				FCaptures.Clear();
				return;
			}

			for (int i = 0; i < spreadMax; i++)
			{
				if (!FCaptures.ContainsKey(i))
				{
					FCaptures.Add(i, new CaptureVideoInstance());
					FCaptures[i].Initialise(FPinInCameraID[i], FWidth[i], FHeight[i]);
					FPinOutImage[i] = FCaptures[i].Image;
				}
			}

			if (FCaptures.Count > spreadMax)
			{
				for (int i = spreadMax; i < FCaptures.Count; i++)
				{
					FCaptures.Remove(i);
				}
			}
		}
	}
}

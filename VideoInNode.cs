#region usings
using System;
using System.ComponentModel.Composition;
using VVVV.Core.Logging;
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
		Thread FCaptureThread;
		bool FRunCaptureThread;
		Capture FCapture;
		Image<Bgr, byte> FBuffer;

		public int CameraID = -1;
		public string Status;
		public ImageRGB Image = new ImageRGB();
		public bool IsRunning;
		
		public void Initialise(int id)
		{
			Close();
			
			try
			{
				FCapture = new Capture(id); //create a camera captue
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

				lock (Image.Lock)
					Image.Img = FBuffer;
				
				//allow a gap where we're not locked
				Thread.Sleep(5);
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
		
		[Input("Camera ID", DefaultValue = 0, MinValue=0)]
		IDiffSpread<int> FPinInCameraID;
		[Output("Image")]
		ISpread<ImageRGB> FPinOutImage;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

		[Import]
		ILogger FLogger;

		Dictionary<int, CaptureVideoInstance> FCaptures = new Dictionary<int, CaptureVideoInstance>();

		#endregion fields & pins

		public void Dispose()
		{
			foreach (KeyValuePair<int, CaptureVideoInstance> capture in FCaptures)
				capture.Value.Close();

			GC.SuppressFinalize(this);
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (SpreadMax == 0)
			{
				FCaptures.Clear();
				ResizeOutput(0);
				return;
			}
			
			if (FCaptures.Count != SpreadMax) ResizeOutput(SpreadMax);

			for (int i = 0; i < SpreadMax; i++)
			{
				if (!FCaptures.ContainsKey(i))
				{
					FCaptures.Add(i, new CaptureVideoInstance());
					FCaptures[i].Initialise(FPinInCameraID[i]);
				}
				if (FCaptures[i].CameraID != FPinInCameraID[i])
					FCaptures[i].Initialise(FPinInCameraID[i]);
			}

			if (FCaptures.Count > SpreadMax)
			{
				for (int i = SpreadMax; i < FCaptures.Count; i++)
				{
					FCaptures.Remove(i);
				}
			}
			
			GiveOutputs();
		}

		void GiveOutputs()
		{
			foreach (KeyValuePair<int, CaptureVideoInstance> capture in FCaptures)
			{
				if (capture.Value.Image.FrameChanged)
				{
					FPinOutImage[capture.Key] = capture.Value.Image;
				}
				FPinOutStatus[capture.Key] = capture.Value.Status;
			}
		}

		void ResizeOutput(int count)
		{
			FPinOutStatus.SliceCount = count;
			FPinOutImage.SliceCount = count;

			for (int i = 0; i < count; i++)
			{
				if (FPinOutImage[i] == null)
				{
					FPinOutImage[i] = new ImageRGB();
				}
			}
		}
    }
}

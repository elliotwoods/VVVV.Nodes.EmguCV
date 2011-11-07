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

#endregion usings

namespace VVVV.Nodes.EmguCV
{
	#region PluginInfo
	[PluginInfo(Name = "Encode",
			  Category = "EmguCV",
			  Version = "Greycode",
			  Help = "Generates greycode patterns",
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
		ISpread<ImageL> FPinOutImage;

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

			for (int i = 0; i < count; i++)
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

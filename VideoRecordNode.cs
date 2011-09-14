using System.ComponentModel.Composition;
using Emgu.CV;
using Emgu.CV.CvEnum;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EmguCV
{
	#region PluginInfo
	[PluginInfo(Name = "VideoRecord", Category = "EmguCV", Help = "RecordsVideo", Author = "alg", Credits = "sugokuGENKI", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class VideoRecordNode : IPluginEvaluate
	{
		[Input("ImageIn")] 
		private ISpread<ImageRGB> FInput;

		[Input("File Path", StringType = StringType.Filename, DefaultString = "")] 
		private ISpread<string> FPath;

		[Input("Record", DefaultValue = 0)] 
		private ISpread<bool> FRecord;

		private Spread<bool> FPRecord;
		private Spread<VideoWriter> FVideoWriter;

		[ImportingConstructor]
		public VideoRecordNode()
		{
			FPRecord = new Spread<bool>(1);
			FVideoWriter = new Spread<VideoWriter>(1);
		}

		public void Evaluate(int SpreadMax)
		{
			if (FRecord[0])
			{
				if (!FPRecord[0])
				{
					var capture = new Capture(0);
					//Capture.GetCaptureProperty(CAP_PROP.CV_CAP_PROP_FOURCC))
					int codec = CvInvoke.CV_FOURCC('t', 's', 'c', 'c');
					FVideoWriter[0] = new VideoWriter(@"C:\test.avi", codec, 25, 640, 480, true);
				}

				FVideoWriter[0].WriteFrame(FInput[0].Img);

				
			}
			else
			{
				if(FVideoWriter[0] != null) FVideoWriter[0].Dispose();
			}

			FPRecord = (Spread<bool>)FRecord.Clone();
		}
	}
}

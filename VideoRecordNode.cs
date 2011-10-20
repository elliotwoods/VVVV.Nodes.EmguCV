using System.Threading;
using Emgu.CV;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EmguCV
{
	public class VideoRecordInstance : ImageProcessingInstance
	{
		private VideoWriter FVideoWriter;
		
		public ImageRGB Image { private get; set;}

		public void Initialise(ImageRGB image, string filePath, int codec)
		{	
			try
			{
				FVideoWriter = new VideoWriter(filePath, codec, 25, image.Width, image.Height, true);
				Image = image;
			}
			catch
			{
				IsRunning = false;
				return;
			}

			RunCaptureThread = true;
			CaptureThread = new Thread(Process);
			CaptureThread.Start();
			IsRunning = true;
		}

		public override void Close()
		{
			if (!IsRunning) return;

			base.Close();
			FVideoWriter.Dispose();
		}

		public override void Process()
		{
			while (RunCaptureThread)
			{
				FVideoWriter.WriteFrame(Image.Image);
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "VideoRecord", Category = "EmguCV", Help = "RecordsVideo", Author = "alg", Credits = "sugokuGENKI", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class VideoRecordNode : ImageProcessingNode<VideoRecordInstance>
	{
		[Input("ImageIn")] 
		private ISpread<ImageRGB> FInput;

		[Input("File Path", StringType = StringType.Filename, DefaultString = "test.avi")] 
		private ISpread<string> FPath;

		[Input("Codec FOURCC", StringType = StringType.String, DefaultString = "tscc", IsSingle = true)] 
		private ISpread<string> FCodec;

		[Input("Record", DefaultValue = 0)] 
		private ISpread<bool> FRecord;

		private Spread<bool> FPRecord = new Spread<bool>(1);
		//private Dictionary<int, VideoRecordInstance> FWritersByIndex = new Dictionary<int, VideoRecordInstance>();

		public override void Evaluate(int spreadMax)
		{
			base.Evaluate(spreadMax);

			int codec = CvInvoke.CV_FOURCC(FCodec[0][0], FCodec[0][1], FCodec[0][2], FCodec[0][3]);

			for (int i = 0; i < spreadMax; i++)
			{
				if(FInput[i] == null || !FRecord[i])
				{
					InstancesByIndex[i].Close();
				}
				else if(FRecord[i] && !FPRecord[i])
				{
					//Start record
					InstancesByIndex[i].Initialise(FInput[i], FPath[i], codec);
				}
			}

			FPRecord = (Spread<bool>)FRecord.Clone();
		}
	}
}

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Emgu.CV;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EmguCV
{
	class VideoRecordInstance : CaptureInstance
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

			IsRunning = true;
			RunCaptureThread = true;
			CaptureThread = new Thread(Process);
			CaptureThread.Start();
		}

		public override void Close()
		{
			if(!IsRunning) return;

			base.Close();
			FVideoWriter.Dispose();
		}

		public override void Process()
		{
			while (RunCaptureThread)
			{
				FVideoWriter.WriteFrame(Image.Img);
			}
		}
	}
	#region PluginInfo
	[PluginInfo(Name = "VideoRecord", Category = "EmguCV", Help = "RecordsVideo", Author = "alg", Credits = "sugokuGENKI", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class VideoRecordNode : IPluginEvaluate
	{
		[Input("ImageIn")] 
		private ISpread<ImageRGB> FInput;

		[Input("File Path", StringType = StringType.Filename, DefaultString = "test.avi")] 
		private ISpread<string> FPath;

		[Input("Codec FOURCC", StringType = StringType.String, DefaultString = "tscc", IsSingle = true)] 
		private ISpread<string> FCodec;

		[Input("Record", DefaultValue = 0)] 
		private ISpread<bool> FRecord;

		private Spread<bool> FPRecord;
		private Dictionary<int, VideoRecordInstance> FWritersByIndex;

		[ImportingConstructor]
		public VideoRecordNode()
		{
			FPRecord = new Spread<bool>(1);
			FWritersByIndex = new Dictionary<int, VideoRecordInstance>();
		}

		public void Evaluate(int SpreadMax)
		{
			CheckWritersSize(SpreadMax);

			int codec = CvInvoke.CV_FOURCC(FCodec[0][0], FCodec[0][1], FCodec[0][2], FCodec[0][3]);

			for (int i = 0; i < SpreadMax; i++)
			{
				if(FInput[i] == null || !FRecord[i])
				{
					FWritersByIndex[i].Close();
				}
				else if(FRecord[i] && !FPRecord[i])
				{
					//Start record
					FWritersByIndex[i].Initialise(FInput[i], FPath[i], codec);
				}
			}

			FPRecord = (Spread<bool>)FRecord.Clone();
		}

		private void CheckWritersSize(int spreadMax)
		{
			if(FWritersByIndex.Count < spreadMax)
			{
				for (int i = 0; i < spreadMax; i++)
				{
					if (!FWritersByIndex.ContainsKey(i))
					{
						FWritersByIndex.Add(i, new VideoRecordInstance());
					}
				}

				
			}

			if (FWritersByIndex.Count > spreadMax) return;
			
			for (int i = spreadMax; i < FWritersByIndex.Count; i++)
			{
				FWritersByIndex.Remove(i);
			}
		}
	}
}

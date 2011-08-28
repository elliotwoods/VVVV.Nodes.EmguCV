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

#endregion usings

namespace VVVV.Nodes.EmguCV
{
	#region PluginInfo
	[PluginInfo(Name = "FaceTracking", Category = "EmguCV", Help = "Tracks faces XY and Width/Height", Tags = "")]
	#endregion PluginInfo
	public class FaceTrackingNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Image", IsSingle = true)]
		ISpread<ImageRGB> FImage;

		[Input("Haar Table", DefaultString = "haarcascade_frontalface_alt2.xml", IsSingle = true, StringType = StringType.Filename)] 
		IDiffSpread<string> FPath;

		[Input("Enabled", DefaultValue = 1)]
		ISpread<bool> FEnabled;

		[Output("Face")] 
		ISpread<Vector2D> FFaceXY;

		[Output("Width")] 
		ISpread<double> FWidth;

		[Output("Height")] 
		ISpread<double> FHeight;

		[Import]
		ILogger FLogger;

		private readonly Spread<HaarCascade> FFace; 
		private readonly Thread FTrackingThread;
		private bool FShouldDetect;
		
		private Vector2D FMinimumSourceXY;
		private readonly Vector2D FMinimumDestXY;
		private readonly Vector2D FMaximumDestXY;

		#endregion fields & pins

		[ImportingConstructor]
		public FaceTrackingNode(IPluginHost host)
		{
			FFace = new Spread<HaarCascade>(1);

			FMinimumSourceXY = new Vector2D(0, 0);
			FMinimumDestXY = new Vector2D(-1, 1);
			FMaximumDestXY = new Vector2D(1, -1);

			FTrackingThread = new Thread(TrackingThread);
		}

		private void TrackingThread()
		{
			while (FShouldDetect)
			{
				lock(this)
				{
					var stride = (640 * 3);
					var align = stride % 4;

					if (align != 0)
					{
						stride += 4 - align;
					}

					Image<Bgr, Byte> image = FImage[0].Img;
					Image<Gray, Byte> grayImage = image.Convert<Gray, Byte>();

					grayImage._EqualizeHist();

					MCvAvgComp[][] faceDetected = grayImage.DetectHaarCascade(FFace[0], 1.8, 4, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(image.Width / 8, image.Height / 8));

					foreach (MCvAvgComp f in faceDetected[0])
					{
						var faceVector = new Vector2D(f.rect.X + f.rect.Width / 2, f.rect.Y + f.rect.Height / 2);

						Vector2D maximumSourceXY = new Vector2D(image.Width, image.Height);

						FFaceXY[0] = VMath.Map(faceVector, FMinimumSourceXY, maximumSourceXY, FMinimumDestXY, FMaximumDestXY, TMapMode.Float);
						FWidth[0] = VMath.Map(f.rect.Width, FMinimumSourceXY.x, maximumSourceXY.x, 0, 2, TMapMode.Float);
						FHeight[0] = VMath.Map(f.rect.Height, FMinimumSourceXY.y, maximumSourceXY.y, 0, 2, TMapMode.Float);
					}
				}
				
			}
			
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FFace.SliceCount = FPath.SliceCount;

			if(FPath.IsChanged)
			{
				for (int i = 0; i < FPath.SliceCount; i++)
				{
					//if(string.IsNullOrEmpty(FPath[i])) continue;

					FFace[i] = new HaarCascade(FPath[i]);
				}
			}

			FShouldDetect = this.FImage[0] != null && FFace[0] != null;

			if (FShouldDetect && FTrackingThread.ThreadState == ThreadState.Unstarted)
			{
				FTrackingThread.Start();
				FLogger.Log(LogType.Message, "Tracking thread started.");
			}

		}
	}
}

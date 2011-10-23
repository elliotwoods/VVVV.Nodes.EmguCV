#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.EmguCV
{
	public class TrackingObject
	{
		public Vector2D Position;
		public Vector2D Scale;
	}

	public class TrackingInstance : ImageProcessingInstance
	{
		private readonly Vector2D FMinimumSourceXY = new Vector2D(0, 0);
		private readonly Vector2D FMinimumDestXY = new Vector2D(-0.5, 0.5);
		private readonly Vector2D FMaximumDestXY = new Vector2D(0.5, -0.5);
		
		private ImageRGB FSource;
		private HaarCascade FHaarCascade;

		private readonly List<TrackingObject> FTrackingObjects = new List<TrackingObject>();
		
		public List<TrackingObject> TrackingObjects
		{
			get { return FTrackingObjects; }
		}

		public void Initialise(ImageRGB image, string haarPath)
		{
			Close();

			try
			{
				FHaarCascade = new HaarCascade(haarPath);
			}
			catch
			{
				IsRunning = false;
				return;
			}

			FSource = image;

			RunCaptureThread = true;
			CaptureThread = new Thread(Process);
			CaptureThread.Start();
			IsRunning = true;
		}

		public override void Close()
		{
			if (!IsRunning) return;

			base.Close();
			FHaarCascade.Dispose();
		}

		public override void Process()
		{
			while (RunCaptureThread)
			{
					lock (this)
					{
						if (!FSource.Initialised) continue;
						
						Image<Gray, Byte> grayImage = FSource.Image.Convert<Gray, Byte>();

						var stride = (grayImage.Width * 3);
						var align = stride % 4;

						if (align != 0)
						{
							stride += 4 - align;
						}

						grayImage._EqualizeHist();

						MCvAvgComp[] objectsDetected = FHaarCascade.Detect(grayImage, 1.8, 1, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(grayImage.Width / 10, grayImage.Height / 10));

						FTrackingObjects.Clear();

						foreach (MCvAvgComp f in objectsDetected)
						{
							TrackingObject trackingObject = new TrackingObject();

							Vector2D objectCenterPosition = new Vector2D(f.rect.X + f.rect.Width / 2, f.rect.Y + f.rect.Height / 2);
							Vector2D maximumSourceXY = new Vector2D(grayImage.Width, grayImage.Height);

							trackingObject.Position = VMath.Map(objectCenterPosition, FMinimumSourceXY, maximumSourceXY, FMinimumDestXY, FMaximumDestXY, TMapMode.Float);
							trackingObject.Scale = VMath.Map(new Vector2D(f.rect.Width, f.rect.Height), FMinimumSourceXY.x, maximumSourceXY.x, 0, 1, TMapMode.Float);

							FTrackingObjects.Add(trackingObject);
						}
					}
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "ObjectTracking", Category = "EmguCV", Help = "Tracks faces and eyes", Author = "alg, sugokuGENKI", Tags = "")]
	#endregion PluginInfo
	public class ObjectTrackingNode : ImageProcessingNode<TrackingInstance>
	{
		#region fields & pins
		[Input("Image")]
		IDiffSpread<ImageRGB> FPinInImages;

		[Input("Haar Table", DefaultString = "haarcascade_frontalface_alt2.xml", IsSingle = true, StringType = StringType.Filename)] 
		IDiffSpread<string> FHaarPath;

		[Input("Enabled", DefaultValue = 1)]
		ISpread<bool> FEnabled;

		private Spread<bool> FPEnabled = new Spread<bool>(1);

		[Output("Position")] 
		ISpread<ISpread<Vector2D>> FPinOutPositionXY;

		[Output("Scale")] 
		ISpread<ISpread<Vector2D>> FPinOutScaleXY;

		[Import]
		ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public override void Evaluate(int spreadMax)
		{
			base.Evaluate(spreadMax);

			for (int i = 0; i < spreadMax; i++)
			{
				if((!FPEnabled[i] && FEnabled[i]) || FHaarPath.IsChanged)
				{
					InstancesByIndex[i].Initialise(FPinInImages[i], FHaarPath[i]);
				}
				else if(!FEnabled[i])
				{
					InstancesByIndex[i].Close();
				}
			}

			OutputFaces();

			FPEnabled = (Spread<bool>) FEnabled.Clone();
		}

		void OutputFaces()
		{
			FPinOutPositionXY.SliceCount = InstancesByIndex.Count;
			FPinOutScaleXY.SliceCount = InstancesByIndex.Count;

			foreach (KeyValuePair<int, TrackingInstance> tracker in InstancesByIndex)
			{
				int count = tracker.Value.TrackingObjects.Count;
				FPinOutPositionXY[tracker.Key].SliceCount = count;
				FPinOutScaleXY[tracker.Key].SliceCount = count;

				for (int i = 0; i < count; i++)
				{
					try
					{
						FPinOutPositionXY[tracker.Key][i] = tracker.Value.TrackingObjects[i].Position;
						FPinOutScaleXY[tracker.Key][i] = tracker.Value.TrackingObjects[i].Scale;
					}
					catch
					{
						FLogger.Log(LogType.Error, "Desync in threads");
					}
				}
			}
		}
	}
}

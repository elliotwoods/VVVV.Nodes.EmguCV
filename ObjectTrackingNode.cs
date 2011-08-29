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
	class TrackingObject
	{
		public Vector2D Position;
		public Vector2D Scale;
	}

	class TrackingInstance
	{
		private readonly Vector2D CMinimumSourceXY = new Vector2D(0, 0);
		private readonly Vector2D CMinimumDestXY = new Vector2D(-1, 1);
		private readonly Vector2D CMaximumDestXY = new Vector2D(1, -1);

		private Thread FTrackingThread;
		private bool FIsRunning;
		
		public List<TrackingObject> Objects = new List<TrackingObject>();
		
		private ImageRGB FSource;
		private Image<Gray, byte> FGrayImage;
		private HaarCascade FFaceHaarCascade;

		public TrackingInstance(ImageRGB image, HaarCascade cascade)
		{
			FSource = image;
			FFaceHaarCascade = cascade;
			FTrackingThread = new Thread(FindFacesThread);
			FTrackingThread.Start();

			FIsRunning = true;
		}

		public void Close()
		{
			if (!FIsRunning) return;
			
			FIsRunning = false;
			FTrackingThread.Join(100);
			FTrackingThread = null;
		}

		void FindFacesThread()
		{
			while (FIsRunning)
			{
				if (FSource.FrameChanged)
					lock(this)
					{
						FGrayImage = FSource.Img.Convert<Gray, Byte>();

						var stride = (FGrayImage.Width * 3);
						var align = stride % 4;

						if (align != 0)
						{
							stride += 4 - align;
						}
						
						FGrayImage._EqualizeHist();

						MCvAvgComp[] objectsDetected = FFaceHaarCascade.Detect(FGrayImage, 1.8, 4, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(FGrayImage.Width / 8, FGrayImage.Height / 8));

						Objects.Clear();

						foreach (MCvAvgComp f in objectsDetected)
						{
							TrackingObject trackingObject = new TrackingObject();
							
							Vector2D objectCenterPosition = new Vector2D(f.rect.X + f.rect.Width / 2, f.rect.Y + f.rect.Height / 2);
							Vector2D maximumSourceXY = new Vector2D(FGrayImage.Width, FGrayImage.Height);

							trackingObject.Position = VMath.Map(objectCenterPosition, CMinimumSourceXY, maximumSourceXY, CMinimumDestXY, CMaximumDestXY, TMapMode.Float);
							trackingObject.Scale = VMath.Map(new Vector2D(f.rect.Width, f.rect.Height), CMinimumSourceXY.x, maximumSourceXY.x, 0, 2, TMapMode.Float);

							Objects.Add(trackingObject);
						}
					}
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "ObjectTracking", Category = "EmguCV", Help = "Tracks faces and eyes", Author = "alg, sugokuGENKI", Tags = "")]
	#endregion PluginInfo
	public class ObjectTrackingNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Image", IsSingle = true)]
		ISpread<ImageRGB> FPinInImages;

		[Input("Haar Table", DefaultString = "haarcascade_frontalface_alt2.xml", IsSingle = true, StringType = StringType.Filename)] 
		IDiffSpread<string> FFacePath;

		[Input("Enabled", DefaultValue = 1)]
		ISpread<bool> FEnabled;

		[Output("Position")] 
		ISpread<ISpread<Vector2D>> FPinOutPositionXY;

		[Output("Scale")] 
		ISpread<ISpread<Vector2D>> FPinOutScaleXY;

		[Output("Status")]
		ISpread<string> FStatus;

		[Import]
		ILogger FLogger;

		private HaarCascade FHaarCascade;

		private readonly Dictionary<int, TrackingInstance> FTrackers = new Dictionary<int,TrackingInstance>();
		

		#endregion fields & pins

		public void Dispose()
		{
			foreach (KeyValuePair<int, TrackingInstance> tracker in FTrackers)
				tracker.Value.Close();
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FFacePath.IsChanged)
			{
				if (FFacePath.SliceCount > 0)
				{
					try
					{
						FHaarCascade = new HaarCascade(FFacePath[0]);
					}
					catch
					{
						for (int i = 0; i < FStatus.SliceCount; i++)
							FStatus[i] = "Loading cascade xml failed";
					}
				}
			}

			if (FHaarCascade == null)
			{
				FStatus.SliceCount = 1;
				FStatus[0] = "Please load haar cascade xml";
				return;
			}

			UpdateTrackers();

			OutputFaces();
		}

		void UpdateTrackers()
		{
			for (int i = 0; i < FPinInImages.SliceCount; i++)
			{
				if (!FTrackers.ContainsKey(i))
					FTrackers.Add(i, new TrackingInstance(FPinInImages[i], FHaarCascade));
				else if (FPinInImages[i].FrameAttributesChanged)
					FTrackers[i] = new TrackingInstance(FPinInImages[i], FHaarCascade);
			}

			if (FTrackers.Count <= FPinInImages.SliceCount) return;
			
			for (int i = FPinInImages.SliceCount; i < FTrackers.Count; i++)
			{
				FTrackers.Remove(i);
			}
		}

		void OutputFaces()
		{
			FPinOutPositionXY.SliceCount = FTrackers.Count;
			FPinOutScaleXY.SliceCount = FTrackers.Count;

			foreach (KeyValuePair<int, TrackingInstance> tracker in FTrackers)
			{
				int count = tracker.Value.Objects.Count;
				FPinOutPositionXY[tracker.Key].SliceCount = count;
				FPinOutScaleXY[tracker.Key].SliceCount = count;

				for (int i = 0; i < count; i++)
				{
					FPinOutPositionXY[tracker.Key][i] = tracker.Value.Objects[i].Position;
					FPinOutScaleXY[tracker.Key][i] = tracker.Value.Objects[i].Scale;
				}
			}
		}
	}
}

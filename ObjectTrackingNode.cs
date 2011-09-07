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
		private readonly Vector2D FMinimumSourceXY = new Vector2D(0, 0);
		private readonly Vector2D FMinimumDestXY = new Vector2D(-1, 1);
		private readonly Vector2D FMaximumDestXY = new Vector2D(1, -1);

		private Thread FTrackingThread;
		private bool FIsRunning;
		
		private ImageRGB FSource;
		private Image<Gray, byte> FGrayImage;

		private HaarCascade FHaarCascade;

		public List<TrackingObject> Objects = new List<TrackingObject>();

		private string FHaarPath;

		public string HaarPath
		{
			get { return FHaarPath; }
			
			set
			{
				FHaarPath = value;
				FHaarCascade = new HaarCascade(value);
			}
		}

		public ImageRGB Source
		{
			get { return FSource; }
			set { FSource = value; }
		}

		public TrackingInstance(ImageRGB image, string haarPath)
		{
			FSource = image;
			HaarPath = haarPath;
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

						MCvAvgComp[] objectsDetected = FHaarCascade.Detect(FGrayImage, 1.8, 4, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(FGrayImage.Width / 8, FGrayImage.Height / 8));

						Objects.Clear();

						foreach (MCvAvgComp f in objectsDetected)
						{
							TrackingObject trackingObject = new TrackingObject();
							
							Vector2D objectCenterPosition = new Vector2D(f.rect.X + f.rect.Width / 2, f.rect.Y + f.rect.Height / 2);
							Vector2D maximumSourceXY = new Vector2D(FGrayImage.Width, FGrayImage.Height);

							trackingObject.Position = VMath.Map(objectCenterPosition, FMinimumSourceXY, maximumSourceXY, FMinimumDestXY, FMaximumDestXY, TMapMode.Float);
							trackingObject.Scale = VMath.Map(new Vector2D(f.rect.Width, f.rect.Height), FMinimumSourceXY.x, maximumSourceXY.x, 0, 2, TMapMode.Float);

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
		[Input("Image")]
		IDiffSpread<ImageRGB> FPinInImages;

		[Input("Haar Table", DefaultString = "haarcascade_frontalface_alt2.xml", IsSingle = true, StringType = StringType.Filename)] 
		IDiffSpread<string> FHaarPath;

		[Input("Enabled", DefaultValue = 1)]
		ISpread<bool> FEnabled;

		[Output("Position")] 
		ISpread<ISpread<Vector2D>> FPinOutPositionXY;

		[Output("Scale")] 
		ISpread<ISpread<Vector2D>> FPinOutScaleXY;

		[Import]
		ILogger FLogger;

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

			UpdateTrackers();

			if(FHaarPath.IsChanged)
			{
				if (FHaarPath.SliceCount > 0)
				{
					UpdateHaars();
				}
			}

			if(FPinInImages.IsChanged)
			{
				if(FPinInImages.SliceCount > 0) UpdateImages();
			}

			OutputFaces();
		}

		void UpdateTrackers()
		{
			for (int i = FPinInImages.SliceCount; i < FTrackers.Count; i++)
			{
				FTrackers.Remove(i);
			}

			for (int i = 0; i < FPinInImages.SliceCount; i++)
			{
				if(FPinInImages[i] == null)
				{
					FTrackers.Remove(i);
					continue;
				}

				if (!FTrackers.ContainsKey(i))
				{
					if(FPinInImages[i].Img == null) continue;

					FTrackers.Add(i, new TrackingInstance(FPinInImages[i], FHaarPath[i]));
				}
				else if (FPinInImages[i].FrameAttributesChanged)
				{
					FTrackers[i] = new TrackingInstance(FPinInImages[i], FHaarPath[i]);
				}	
			}

		}

		void UpdateHaars()
		{
			int count = FTrackers.Count;
			
			for (int i = 0; i < count; i++)
			{
				try
				{
					FTrackers[i].HaarPath = FHaarPath[i];
				}
				catch
				{
					FLogger.Log(LogType.Error, "Loading Haar failed at slice ", i.ToString());
				}
			}
		}

		void UpdateImages()
		{
			for (int i = 0; i < FTrackers.Count; i++)
			{
				FTrackers[i].Source = FPinInImages[i];
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
					try
					{
						FPinOutPositionXY[tracker.Key][i] = tracker.Value.Objects[i].Position;
						FPinOutScaleXY[tracker.Key][i] = tracker.Value.Objects[i].Scale;
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

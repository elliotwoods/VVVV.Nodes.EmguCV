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
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.EmguCV
{
	class FaceTrackingFace
	{
		public Vector2D Position;
		public Vector2D Scale;
	}
	class FaceTrackingInstance
	{
		readonly Vector2D CMinimumSourceXY = new Vector2D(0, 0);
		readonly Vector2D CMinimumDestXY = new Vector2D(-1, 1);
		readonly Vector2D CMaximumDestXY = new Vector2D(1, -1);

		private Thread FTrackingThread;

		bool IsRunning;
		public List<FaceTrackingFace> Faces = new List<FaceTrackingFace>();
		ImageRGB FSource = null;
		Image<Gray, byte> FGrayImage;
		HaarCascade FHaarCascade;

		public FaceTrackingInstance(ImageRGB image, HaarCascade cascade)
		{
			FSource = image;
			FHaarCascade = cascade;
			FTrackingThread = new Thread(fnFindFacesThread);
			FTrackingThread.Start();

			IsRunning = true;
		}

		public void Close()
		{
			if (IsRunning)
			{
				IsRunning = false;
				FTrackingThread.Join(100);
				FTrackingThread = null;
			}
		}

		void fnFindFacesThread()
		{
			while (IsRunning)
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

						MCvAvgComp[] faceDetected = FHaarCascade.Detect(FGrayImage, 1.8, 4, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(FGrayImage.Width / 8, FGrayImage.Height / 8));


						Faces.Clear();

						foreach (MCvAvgComp f in faceDetected)
						{
							FaceTrackingFace face = new FaceTrackingFace();
							var faceVector = new Vector2D(f.rect.X + f.rect.Width / 2, f.rect.Y + f.rect.Height / 2);

							Vector2D CMaximumSourceXY = new Vector2D(FGrayImage.Width, FGrayImage.Height);

							face.Position = VMath.Map(faceVector, CMinimumSourceXY, CMaximumSourceXY, CMinimumDestXY, CMaximumDestXY, TMapMode.Float);
							face.Scale = VMath.Map(new Vector2D(f.rect.Width, f.rect.Height), CMinimumSourceXY.x, CMaximumSourceXY.x, 0, 2, TMapMode.Float);

							Faces.Add(face);
						}
					}
				
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "FaceTracking", Category = "EmguCV", Help = "Tracks faces XY and Width/Height", Tags = "")]
	#endregion PluginInfo
	public class FaceTrackingNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Image", IsSingle = true)]
		ISpread<ImageRGB> FPinInImages;

		[Input("Haar Table", DefaultString = "haarcascade_frontalface_alt2.xml", IsSingle = true, StringType = StringType.Filename)] 
		IDiffSpread<string> FPath;

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

		HaarCascade FHaarCascade;
		Dictionary<int, FaceTrackingInstance> FFaceTrackers = new Dictionary<int,FaceTrackingInstance>();

		#endregion fields & pins

		[ImportingConstructor]
		public FaceTrackingNode(IPluginHost host)
		{

		}

		public void Dispose()
		{
			foreach (KeyValuePair<int, FaceTrackingInstance> tracker in FFaceTrackers)
				tracker.Value.Close();
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FPath.IsChanged)
			{
				if (FPath.SliceCount > 0)
				{
					try
					{
						FHaarCascade = new HaarCascade(FPath[0]);
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
				if (!FFaceTrackers.ContainsKey(i))
					FFaceTrackers.Add(i, new FaceTrackingInstance(FPinInImages[i], FHaarCascade));
				else if (FPinInImages[i].FrameAttributesChanged)
					FFaceTrackers[i] = new FaceTrackingInstance(FPinInImages[i], FHaarCascade);
			}

			if (FFaceTrackers.Count > FPinInImages.SliceCount)
			{
				for (int i = FPinInImages.SliceCount; i < FFaceTrackers.Count; i++)
				{
					FFaceTrackers.Remove(i);
				}
			}
		}

		void OutputFaces()
		{
			FPinOutPositionXY.SliceCount = FFaceTrackers.Count;
			FPinOutScaleXY.SliceCount = FFaceTrackers.Count;

			foreach (KeyValuePair<int, FaceTrackingInstance> tracker in FFaceTrackers)
			{
				int count = tracker.Value.Faces.Count;
				FPinOutPositionXY[tracker.Key].SliceCount = count;
				FPinOutScaleXY[tracker.Key].SliceCount = count;

				for (int i = 0; i < count; i++)
				{
					FPinOutPositionXY[tracker.Key][i] = tracker.Value.Faces[i].Position;
					FPinOutScaleXY[tracker.Key][i] = tracker.Value.Faces[i].Scale;
				}
			}
		}
	}
}

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
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.EmguCV
{
	class FaceTrackingElement
	{
		public Vector2D Position;
		public Vector2D Scale;
	}

	class FaceTrackingInstance
	{
		private readonly Vector2D CMinimumSourceXY = new Vector2D(0, 0);
		private readonly Vector2D CMinimumDestXY = new Vector2D(-1, 1);
		private readonly Vector2D CMaximumDestXY = new Vector2D(1, -1);

		private Thread FTrackingThread;
		private bool FIsRunning;
		
		public List<FaceTrackingElement> Faces = new List<FaceTrackingElement>();
		public List<FaceTrackingElement> Eyes = new List<FaceTrackingElement>(); 
		
		private ImageRGB FSource = null;
		private Image<Gray, byte> FGrayImage;
		private HaarCascade FFaceHaarCascade;
		private HaarCascade FEyesHaarCascade;

		public FaceTrackingInstance(ImageRGB image, HaarCascade cascade)
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

						MCvAvgComp[] faceDetected = FFaceHaarCascade.Detect(FGrayImage, 1.8, 4, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(FGrayImage.Width / 8, FGrayImage.Height / 8));

						Faces.Clear();

						foreach (MCvAvgComp f in faceDetected)
						{
							FaceTrackingElement face = new FaceTrackingElement();
							
							Vector2D facePosition = new Vector2D(f.rect.X + f.rect.Width / 2, f.rect.Y + f.rect.Height / 2);
							Vector2D maximumSourceXY = new Vector2D(FGrayImage.Width, FGrayImage.Height);

							face.Position = VMath.Map(facePosition, CMinimumSourceXY, maximumSourceXY, CMinimumDestXY, CMaximumDestXY, TMapMode.Float);
							face.Scale = VMath.Map(new Vector2D(f.rect.Width, f.rect.Height), CMinimumSourceXY.x, maximumSourceXY.x, 0, 2, TMapMode.Float);

							Faces.Add(face);

							FGrayImage.ROI = f.rect;
							MCvAvgComp[] eyeDetected = FFaceHaarCascade.Detect(FGrayImage, 1.8, 4, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(FGrayImage.Width/8, FGrayImage.Height/8));
							FGrayImage.ROI = Rectangle.Empty;

							foreach (var e in eyeDetected)
							{
								FaceTrackingElement eye = new FaceTrackingElement();

								Vector2D eyePosition = new Vector2D(e.rect.X + e.rect.Width / 2, e.rect.Y + f.rect.Height / 2);

								eye.Position = VMath.Map(eyePosition, CMinimumSourceXY, maximumSourceXY, CMinimumDestXY, CMaximumDestXY, TMapMode.Float);
								eye.Scale = VMath.Map(new Vector2D(e.rect.Width, e.rect.Height), CMinimumSourceXY.x, maximumSourceXY.x, 0, 2, TMapMode.Float);

								Eyes.Add(eye);
							}
						}
					}
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "FaceTracking", Category = "EmguCV", Help = "Tracks faces and eyes", Author = "alg, sugokuGENKI", Tags = "")]
	#endregion PluginInfo
	public class FaceTrackingNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Image", IsSingle = true)]
		ISpread<ImageRGB> FPinInImages;

		[Input("Face Haar Table", DefaultString = "haarcascade_frontalface_alt2.xml", IsSingle = true, StringType = StringType.Filename)] 
		IDiffSpread<string> FFacePath;

		[Input("Eyes Haar Table", DefaultString = null, IsSingle = true, StringType = StringType.Filename)] 
		IDiffSpread<string> FEyesPath;

		[Input("Enabled", DefaultValue = 1)]
		ISpread<bool> FEnabled;

		[Output("Face Position")] 
		ISpread<ISpread<Vector2D>> FPinOutFacePositionXY;

		[Output("Face Scale")] 
		ISpread<ISpread<Vector2D>> FPinOutFaceScaleXY;

		[Output("Eye Position")] 
		ISpread<ISpread<Vector2D>> FPinOutEyePositionXY;

		[Output("Eye Scale")]
		ISpread<ISpread<Vector2D>> FPinOutEyeScaleXY;

		[Output("Status")]
		ISpread<string> FStatus;

		[Import]
		ILogger FLogger;

		private HaarCascade FFaceHaarCascade;
		private HaarCascade FEyesHaarCascade;

		private Dictionary<int, FaceTrackingInstance> FFaceTrackers = new Dictionary<int,FaceTrackingInstance>();
		

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
			if(FFacePath.IsChanged)
			{
				if (FFacePath.SliceCount > 0)
				{
					try
					{
						FFaceHaarCascade = new HaarCascade(FFacePath[0]);
					}
					catch
					{
						for (int i = 0; i < FStatus.SliceCount; i++)
							FStatus[i] = "Loading cascade xml failed";
					}
				}
			}

			if(FEyesPath.IsChanged)
			{
				if(FEyesPath.SliceCount > 0)
				{
					try
					{
						FEyesHaarCascade = new HaarCascade(FEyesPath[0]);
					}
					catch
					{
						
						FLogger.Log(LogType.Error, "Eyes haar cascade was not loaded");
					}
				}
			}

			if (FFaceHaarCascade == null)
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
					FFaceTrackers.Add(i, new FaceTrackingInstance(FPinInImages[i], FFaceHaarCascade));
				else if (FPinInImages[i].FrameAttributesChanged)
					FFaceTrackers[i] = new FaceTrackingInstance(FPinInImages[i], FFaceHaarCascade);
			}

			if (FFaceTrackers.Count <= FPinInImages.SliceCount) return;
			
			for (int i = FPinInImages.SliceCount; i < FFaceTrackers.Count; i++)
			{
				FFaceTrackers.Remove(i);
			}
		}

		void OutputFaces()
		{
			FPinOutFacePositionXY.SliceCount = FFaceTrackers.Count;
			FPinOutFaceScaleXY.SliceCount = FFaceTrackers.Count;

			foreach (KeyValuePair<int, FaceTrackingInstance> tracker in FFaceTrackers)
			{
				int faceCount = tracker.Value.Faces.Count;
				FPinOutFacePositionXY[tracker.Key].SliceCount = faceCount;
				FPinOutFaceScaleXY[tracker.Key].SliceCount = faceCount;

				for (int i = 0; i < faceCount; i++)
				{
					FPinOutFacePositionXY[tracker.Key][i] = tracker.Value.Faces[i].Position;
					FPinOutFaceScaleXY[tracker.Key][i] = tracker.Value.Faces[i].Scale;
				}

				int eyeCount = tracker.Value.Eyes.Count;
				FPinOutEyePositionXY[tracker.Key].SliceCount = eyeCount;
				FPinOutEyeScaleXY[tracker.Key].SliceCount = eyeCount;

				for (int j = 0; j < eyeCount; j++)
				{
					FPinOutEyePositionXY[tracker.Key][j] = tracker.Value.Eyes[j].Position;
					FPinOutEyeScaleXY[tracker.Key][j] = tracker.Value.Eyes[j].Scale;
				}
			}
		}
	}
}

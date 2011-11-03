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
{	class FindBoardInstance
	{
		readonly Vector2D CMinimumSourceXY = new Vector2D(0, 0);
		readonly Vector2D CMinimumDestXY = new Vector2D(-1, 1);
		readonly Vector2D CMaximumDestXY = new Vector2D(1, -1);

		ImageRGB FSource = null;
		Image<Gray, byte> FGrayImageFront, FGrayImageBack;

		Size BoardSize = new Size(10, 7);
		PointF[] FFoundPointsFront, FFoundPointsBack;

		private Object FLockGreyFront = new Object();
		private Object FLockGreyBack = new Object();
		private Object FLockFoundPointsFront = new Object();
		private Object FLockFoundPointsBack = new Object();
		private Thread FFindThread;

		public bool Enabled = true;
		private bool isRunning = false;

		public FindBoardInstance(ImageRGB image, bool enabled)
		{
			FSource = image;

			FSource.ImageUpdate += new EventHandler(FSource_ImageUpdate);
			this.isRunning = true;
			this.Enabled = enabled;
			this.FFindThread = new Thread(fnThread);
			this.FFindThread.Start();
		}

		void FSource_ImageUpdate(object sender, EventArgs e)
		{
			var imageRGB = sender as ImageRGB;

			if (imageRGB != null)
			{
				if (Enabled)
					updateGrey(imageRGB);
			}
		}

		public void Close()
		{
			isRunning = false;
		}

		public void SetSize(int x, int y)
		{
			BoardSize.Width = x;
			BoardSize.Height = y;

			FFoundPointsFront = new PointF[x * y];
			FFoundPointsBack = new PointF[x * y];
		}

		void updateGrey(ImageRGB image)
		{
			lock (FLockGreyBack)
			{
				FGrayImageBack = image.Image.Convert<Gray, Byte>();
			}
		}

		void findBoards()
		{
			lock (FLockGreyFront)
			{
				lock (FLockGreyBack)
				{
					Image<Gray, byte> swap = FGrayImageBack;
					FGrayImageBack = FGrayImageFront;
					FGrayImageFront = swap;
				}
			}

			//no image yet
			if (FGrayImageFront == null)
				return;

			lock (FLockFoundPointsFront)
			{
				lock (FLockFoundPointsBack)
				{
					PointF[] swap = FFoundPointsBack;
					FFoundPointsFront = FFoundPointsBack;
					FFoundPointsFront = swap;
				}
			}

			lock (FLockGreyFront)
			{
				FFoundPointsBack = CameraCalibration.FindChessboardCorners(FGrayImageFront, BoardSize, CALIB_CB_TYPE.ADAPTIVE_THRESH);
			}
		}

		void fnThread()
		{
			while (isRunning)
			{
				if (Enabled)
					findBoards();

				Thread.Sleep(1);
			}
		}

		public bool GetFoundCorners(List<Vector2D> points) {

			points.Clear();

			lock (FLockFoundPointsFront)
			{
				if (FFoundPointsFront == null) {
					return false;
				}
				

				for (int i = 0; i < BoardSize.Width * BoardSize.Height; i++)
				{
					points.Add(new Vector2D(FFoundPointsFront[i].X, FFoundPointsFront[i].Y));
				}

				return true;
			}

		}
	}

	#region PluginInfo
	[PluginInfo(Name = "FindBoard", Category = "EmguCV", Help = "Finds chessboard corners XY", Tags = "")]
	#endregion PluginInfo
	public class FindBoardNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Image", IsSingle = true)]
		ISpread<ImageRGB> FPinInImages;
		
		[Input("Board size X", IsSingle=true, DefaultValue=8)]
		IDiffSpread<int> FPinInBoardSizeX;

		[Input("Board size Y", IsSingle=true, DefaultValue=6)]
		IDiffSpread<int> FPinInBoardSizeY;

		[Input("Enabled", DefaultValue = 1)]
		IDiffSpread<bool> FPinInEnabled;

		[Output("Position")]
		ISpread<ISpread<Vector2D>> FPinOutPositionXY;

		[Output("Status")]
		ISpread<string> FStatus;

		[Import]
		ILogger FLogger;

		Dictionary<int, FindBoardInstance> FBoardTrackers = new Dictionary<int, FindBoardInstance>();

		#endregion fields & pins

		[ImportingConstructor]
		public FindBoardNode(IPluginHost host)
		{

		}

		public void Dispose()
		{
			foreach (KeyValuePair<int, FindBoardInstance> tracker in FBoardTrackers)
				tracker.Value.Close();
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			UpdateInstances();

			CheckParams();

			Output();
		}

		void UpdateInstances()
		{
			for (int i = 0; i < FPinInImages.SliceCount; i++)
			{
				if (!FBoardTrackers.ContainsKey(i) && FPinInImages[i] != null){
					FBoardTrackers.Add(i, new FindBoardInstance(FPinInImages[i], FPinInEnabled[0]));
					FBoardTrackers[i].SetSize(FPinInBoardSizeX[0], FPinInBoardSizeY[0]);
				}
			}

			if (FBoardTrackers.Count > FPinInImages.SliceCount)
			{
				for (int i = FPinInImages.SliceCount; i < FBoardTrackers.Count; i++)
				{
					FBoardTrackers.Remove(i);
				}
			}
		}

		void CheckParams()
		{
			if (FPinInBoardSizeX.IsChanged || FPinInBoardSizeY.IsChanged)
			{
				foreach (KeyValuePair<int, FindBoardInstance> tracker in FBoardTrackers)
				{
					tracker.Value.SetSize(FPinInBoardSizeX[0], FPinInBoardSizeY[0]);
				}
			}

			if (FPinInEnabled.IsChanged)
				foreach (KeyValuePair<int, FindBoardInstance> tracker in FBoardTrackers)
				{
					tracker.Value.Enabled = FPinInEnabled[0];
				}
		}

		void Output()
		{
			FPinOutPositionXY.SliceCount = FBoardTrackers.Count;

			foreach (KeyValuePair<int, FindBoardInstance> tracker in FBoardTrackers)
			{
				List<Vector2D> foundPoints = new List<Vector2D>();
				tracker.Value.GetFoundCorners(foundPoints);
				int count = foundPoints.Count;
				FPinOutPositionXY[tracker.Key].SliceCount = count;

				for (int i = 0; i < count; i++)
				{
					FPinOutPositionXY[tracker.Key][i] = foundPoints[i];
				}
			}
		}
	}
}

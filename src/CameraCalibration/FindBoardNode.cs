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
	#region PluginInfo
	[PluginInfo(Name = "FindBoard", Category = "EmguCV", Help = "Finds chessboard corners XY", Tags = "")]
	#endregion PluginInfo
	public class FindBoardNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Image")]
		ISpread<CVImageLink> FPinInInput;
		
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

		private ProcessInputThreaded<FindBoardInstance> FTrackers;
		
		#endregion fields & pins

		[ImportingConstructor]
		public FindBoardNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FTrackers == null)
				FTrackers = new ProcessInputThreaded<FindBoardInstance>(FPinInInput);

			FTrackers.CheckInputSize();
			CheckParams();
			Output();
		}

		void CheckParams()
		{
			if (FPinInBoardSizeX.IsChanged || FPinInBoardSizeY.IsChanged)
				for (int i=0; i<FTrackers.SliceCount; i++)
				{
					FTrackers[i].SetSize(FPinInBoardSizeX[0], FPinInBoardSizeY[0]);
				}

			if (FPinInEnabled.IsChanged)
				for (int i = 0; i < FTrackers.SliceCount; i++)
				{
					FTrackers[i].Enabled = FPinInEnabled[0];
				}
		}

		void Output()
		{
			FPinOutPositionXY.SliceCount = FTrackers.SliceCount;

			for (int i = 0; i < FTrackers.SliceCount; i++)
			{
				FPinOutPositionXY[i] = FTrackers[i].GetFoundCorners();
			}
		}
	}
}

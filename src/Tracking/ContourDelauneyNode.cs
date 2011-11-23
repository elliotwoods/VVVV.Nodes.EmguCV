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
	[PluginInfo(Name = "ContourDelauney", Category = "EmguCV", Version="", Help = "Convert contour perimeter to triangles", Tags = "")]
	#endregion PluginInfo
	public class ContourDelauneyNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		ISpread<ContourPerimeter> FPinInInput;

		[Input("Apply")]
		ISpread<bool> FPinInApply;

		[Output("Vertex position")]
		ISpread<ISpread<Vector2D>> FPinOutPosition;

		[Output("Triangle area")]
		ISpread<ISpread<double>> FPinOutArea;

		[Output("Triangle centroid")]
		ISpread<ISpread<Vector2D>> FPinOutCentroid;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public ContourDelauneyNode(IPluginHost host)
		{

		}

		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInInput[0] == null)
			{
				FPinOutPosition.SliceCount = 0;
				FPinOutArea.SliceCount = 0;
				FPinOutCentroid.SliceCount = 0;
				return;
			}

			FPinOutPosition.SliceCount = SpreadMax;
			FPinOutArea.SliceCount = SpreadMax;
			FPinOutCentroid.SliceCount = SpreadMax;

			Triangle2DF[] delaunayTriangles;
			PlanarSubdivision subdivision;
			for (int i = 0; i < SpreadMax; i++)
			{
				if (!FPinInApply[i])
					continue;

				if (FPinInInput[i].Points.Length > 1000)
					continue;

				subdivision = new PlanarSubdivision(FPinInInput[i].Points.Clone() as PointF[]);
				delaunayTriangles = subdivision.GetDelaunayTriangles();

				FPinOutPosition[i].SliceCount = delaunayTriangles.Length * 3;
				FPinOutArea[i].SliceCount = delaunayTriangles.Length;
				FPinOutCentroid[i].SliceCount = delaunayTriangles.Length;

				for (int j = 0; j < delaunayTriangles.Length; j++)
				{
					FPinOutPosition[i][j * 3 + 0] = new Vector2D(delaunayTriangles[j].V0.X, delaunayTriangles[j].V0.Y);
					FPinOutPosition[i][j * 3 + 1] = new Vector2D(delaunayTriangles[j].V1.X, delaunayTriangles[j].V1.Y);
					FPinOutPosition[i][j * 3 + 2] = new Vector2D(delaunayTriangles[j].V2.X, delaunayTriangles[j].V2.Y);

					FPinOutArea[i][j] = delaunayTriangles[j].Area;
					FPinOutCentroid[i][j] = new Vector2D(delaunayTriangles[j].Centeroid.X, delaunayTriangles[j].Centeroid.Y);
				}

			}
		}

	}
}

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
	[PluginInfo(Name = "Extrinsics", Category = "EmguCV", Version="Split", Help = "Split intrinsics out", Tags = "")]
	#endregion PluginInfo
	public class ExtrinsicsSplitNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Extrinsics")]
		ISpread<ExtrinsicCameraParameters> FPinInExtrinsics;

		[Output("Transform")]
		ISpread<Matrix4x4> FPinOutTransform;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public ExtrinsicsSplitNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FPinOutTransform.SliceCount = SpreadMax;
			
			for (int i=0; i<SpreadMax; i++)
			{
				ExtrinsicCameraParameters extrinsics = FPinInExtrinsics[i];
				if (extrinsics == null)
					continue;

				Matrix<double> t = extrinsics.ExtrinsicMatrix;
				if (extrinsics == null)
					FPinOutTransform[i] = new Matrix4x4();
				else
				{
					Matrix4x4 m = new Matrix4x4();
					for (int x = 0; x < 3; x++)
						for (int y = 0; y < 4; y++)
							m[y, x] = t[x, y];

					m[0, 3] = 0;
					m[1, 3] = 0;
					m[2, 3] = 0;
					m[3, 3] = 1;

					FPinOutTransform[i] = m;
				}
			}
		}

	}
}

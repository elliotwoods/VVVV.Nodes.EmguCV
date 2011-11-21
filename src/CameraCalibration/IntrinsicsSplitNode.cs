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
	[PluginInfo(Name = "Intrinsics", Category = "EmguCV", Version="Split", Help = "Split intrinsics out", Tags = "")]
	#endregion PluginInfo
	public class IntrinsicsSplitNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Intrinsics")]
		ISpread<IntrinsicCameraParameters> FPinInIntinsics;

		[Output("Distortion Coefficients")]
		ISpread<Double> FPinOutDistiortonCoefficients;

		[Output("Camera Matrix")]
		ISpread<Double> FPinOutCameraMatrix;

		[Output("Camera")]
		ISpread<Matrix4x4> FPinOutCamreaTransform;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public IntrinsicsSplitNode(IPluginHost host)
		{

		}

		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			IntrinsicCameraParameters intrinsics = FPinInIntinsics[0];
			if (intrinsics != null)
			{
				FPinOutDistiortonCoefficients.SliceCount = 5;
				for (int i = 0; i < 5; i++ )
					FPinOutDistiortonCoefficients[i] = intrinsics.DistortionCoeffs[i,0];

				FPinOutCameraMatrix.SliceCount = 9;

				for (int j=0; j<3; j++)
					for (int i = 0; i < 3; i++)
					{
						FPinOutCameraMatrix[j + i * 3] = intrinsics.IntrinsicMatrix[i, j];
					}

				FPinOutCamreaTransform.SliceCount = 1; 
				Matrix4x4 m = new Matrix4x4();
				m[0, 0] = intrinsics.IntrinsicMatrix[0, 0];
				m[1, 1] = intrinsics.IntrinsicMatrix[1, 1];
				m[2, 0] = intrinsics.IntrinsicMatrix[0, 2];
				m[2, 1] = intrinsics.IntrinsicMatrix[1, 2];
				m[2, 2] = 1;
				m[2, 3] = 1;
				m[3, 3] = 0;

				FPinOutCamreaTransform[0] = m;
			}
		}

	}
}

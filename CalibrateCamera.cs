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
	[PluginInfo(Name = "CalibrateCamera", Category = "EmguCV", Help = "Finds intrinsics for a single camera", Tags = "")]
	#endregion PluginInfo
	public class CalibrateCameraNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Object Points")]
		ISpread<Vector3D> FPinInObject;

		[Input("Image Points")]
		ISpread<Vector2D> FPinInImage;

		[Input("Resolution XY")]
		ISpread<int> FPinInSensorSize;

		[Input("CV_CALIB_USE_INTRINSIC_GUESS", IsSingle = true)]
		ISpread<bool> FPinInIntrinsicGuess;

		[Input("CV_CALIB_FIX_ASPECT_RATIO", IsSingle = true)]
		ISpread<bool> FPinInFixAspectRatio;

		[Input("CV_CALIB_FIX_PRINCIPAL_POINT", IsSingle = true)]
		ISpread<bool> FPinInFixPincipalPoint;

		[Input("CV_CALIB_ZERO_TANGENT_DIST", IsSingle = true)]
		ISpread<bool> FPinInZeroTangent;

		[Input("CV_CALIB_FIX_FOCAL_LENGTH", IsSingle = true)]
		ISpread<bool> FPinInFixFocalLength;

		[Input("CV_CALIB_FIX_KI", IsSingle = true)]
		ISpread<bool> FPinInFixDistortion;

		[Input("CV_CALIB_RATIONAL_MODEL", IsSingle = true)]
		ISpread<bool> FPinInRationalModel;

		[Input("Do", IsBang=true, IsSingle=true)]
		ISpread<bool> FPinInDo;

		[Output("Intrinsics")]
		ISpread<IntrinsicCameraParameters> FPinOutIntinsics;

		[Output("Extrinsics Per Board")]
		ISpread<ExtrinsicCameraParameters> FPinOutExtrinsics;

		[Output("Reprojection Error")]
		ISpread<Double> FPinOutError;

		[Output("Status")]
		ISpread<string> FStatus;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public CalibrateCameraNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInDo[0])
			{
				int nPointsPerImage = FPinInObject.SliceCount;
				if (nPointsPerImage == 0)
				{
					FStatus[0] = "Insufficient points";
					return;
				}
				int nImages = FPinInImage.SliceCount / nPointsPerImage;

				MCvPoint3D32f[][] objectPoints = new MCvPoint3D32f[nImages][];
				PointF[][] imagePoints = new PointF[nImages][];
				Size imageSize = new Size(FPinInSensorSize[0], FPinInSensorSize[1]);
				CALIB_TYPE flags = new CALIB_TYPE();
				IntrinsicCameraParameters intrinsicParam = new IntrinsicCameraParameters();
				ExtrinsicCameraParameters[] extrinsicsPerView;
				GetFlags(out flags);

				if (FPinInIntrinsicGuess[0])
				{
					Matrix<double> mat = intrinsicParam.IntrinsicMatrix;
					mat[0, 0] = FPinInSensorSize[0];
					mat[1, 1] = FPinInSensorSize[1];
					mat[0, 2] = FPinInSensorSize[0] / 2.0d;
					mat[1, 2] = FPinInSensorSize[1] / 2.0d;
					mat[2, 2] = 1;

				}

				for (int i=0; i<nImages; i++)
				{
					objectPoints[i] = new MCvPoint3D32f[nPointsPerImage];
					imagePoints[i] = new PointF[nPointsPerImage];

					for (int j=0; j<nPointsPerImage; j++)
					{
						objectPoints[i][j].x = (float)FPinInObject[j].x;
						objectPoints[i][j].y = (float)FPinInObject[j].y;
						objectPoints[i][j].z = (float)FPinInObject[j].z;
						
						imagePoints[i][j].X = (float)FPinInImage[i*nPointsPerImage + j].x;
						imagePoints[i][j].Y = (float)FPinInImage[i*nPointsPerImage + j].y;
					}
				}

				try
				{
					FPinOutError[0] = CameraCalibration.CalibrateCamera(objectPoints, imagePoints, imageSize, intrinsicParam, flags, out extrinsicsPerView);
					FPinOutIntinsics[0] = intrinsicParam;


					FPinOutExtrinsics.SliceCount = nImages;
					for (int i = 0; i < nImages; i++)
						FPinOutExtrinsics[i] = extrinsicsPerView[i];

					FStatus[0] = "OK";
				}
				catch (Exception e)  {
					FStatus[0] = e.Message;
				}
			}

		}

		private void GetFlags(out CALIB_TYPE flags)
		{
			flags = 0;
			if (FPinInIntrinsicGuess[0])
				flags |= CALIB_TYPE.CV_CALIB_USE_INTRINSIC_GUESS;

			if (FPinInFixAspectRatio[0])
				flags |= CALIB_TYPE.CV_CALIB_FIX_ASPECT_RATIO;

			if (FPinInFixPincipalPoint[0])
				flags |= CALIB_TYPE.CV_CALIB_FIX_PRINCIPAL_POINT;

			if (FPinInZeroTangent[0])
				flags |= CALIB_TYPE.CV_CALIB_ZERO_TANGENT_DIST;
 
			if (FPinInFixFocalLength[0])
				flags |= CALIB_TYPE.CV_CALIB_FIX_FOCAL_LENGTH;
 
			if (FPinInFixDistortion[0])
				flags |=  (CALIB_TYPE.CV_CALIB_FIX_K1 | CALIB_TYPE.CV_CALIB_FIX_K2 | CALIB_TYPE.CV_CALIB_FIX_K3 | CALIB_TYPE.CV_CALIB_FIX_K4 | CALIB_TYPE.CV_CALIB_FIX_K5 | CALIB_TYPE.CV_CALIB_FIX_K6);

			if (FPinInRationalModel[0])
				flags |= CALIB_TYPE.CV_CALIB_RATIONAL_MODEL;
		}
	}
}

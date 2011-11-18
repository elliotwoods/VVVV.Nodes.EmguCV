using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using VVVV.PluginInterfaces.V2;
using System;
using Emgu.CV.Structure;

namespace VVVV.Nodes.EmguCV
{
	public class OpticalFlowHSInstance : IFilterInstance
	{
		private Size FSize;

		private Image<Gray, byte> FCurrent;
		private Image<Gray, byte> FPrevious;
		private Image<Gray, float> FVelocityX;
		private Image<Gray, float> FVelocityY;

		private double FLambda = 0.1;
		public double Lambda
		{	set
			{
				if (value < 0)
					value = 0;

				if (value > 1)
					value = 1;

				FLambda = value;
			}
		}

		private int FIterations = 100;
		public int Iterations
		{
			set
			{
				if (value < 1)
					value = 1;
				if (value > 500)
					value = 500;

				FIterations = value;
			}
		}

		public bool UsePrevious = false;

		protected override void Initialise()
		{
			FSize = FInput.ImageAttributes.Size;
			FOutput.Image.Initialise(FSize, TColourFormat.RGB32F);

			FCurrent = new Image<Gray, byte>(FSize);
			FPrevious = new Image<Gray, byte>(FSize);
			FVelocityX = new Image<Gray, float>(FSize);
			FVelocityY = new Image<Gray, float>(FSize);
		}

		public override void Process()
		{
			Image<Gray, byte> swap = FPrevious;
			FPrevious = FCurrent;
			FCurrent = swap;

			COLOR_CONVERSION route = ImageUtils.ConvertRoute(FInput.ImageAttributes.ColourFormat, TColourFormat.L8);
			CvInvoke.cvCvtColor(FInput.Image.CvMat, FCurrent.Ptr, route);

			OpticalFlow.HS(FPrevious, FCurrent, UsePrevious, FVelocityX, FVelocityY, FLambda, new MCvTermCriteria(FIterations));

			CopyToRgb();
			FOutput.Send();

		}

		private unsafe void CopyToRgb()
		{
			float* sourcex = (float*) FVelocityX.MIplImage.imageData.ToPointer();
			float* sourcey = (float*) FVelocityY.MIplImage.imageData.ToPointer();
			float* dest = (float*) FOutput.Image.Data.ToPointer();

			for (int i = 0; i < FSize.Width * FSize.Height; i++)
			{
				*dest++ = *sourcex++;
				*dest++ = *sourcey++;
				*dest++ = 0.0f;
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "OpticalFlowHS", Category = "EmguCV", Help = "Perform HS optical flow on image", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class OpticalFlowHSNode : IFilterNode<OpticalFlowHSInstance>
	{
		[Input("Lambda", DefaultValue = 0.1, MinValue = 0, MaxValue = 1)]
		IDiffSpread<double> FPinInLambda;

		[Input("Maximum Iterations", DefaultValue = 100, MinValue = 1, MaxValue = 500)]
		IDiffSpread<int> FPinInIterations;

		[Input("Use Previous Velocity")]
		IDiffSpread<bool> FPinInUsePrevious;

		protected override void Update(int SpreadMax)
		{
			if (FPinInLambda.IsChanged)
				for (int i = 0; i < SpreadMax; i++)
					FProcessor[i].Lambda = FPinInLambda[0];

			if (FPinInIterations.IsChanged)
				for (int i = 0; i < SpreadMax; i++)
					FProcessor[i].Iterations = FPinInIterations[0];

			if (FPinInUsePrevious.IsChanged)
				for (int i = 0; i < SpreadMax; i++)
					FProcessor[i].UsePrevious = FPinInUsePrevious[0];
		}
	}
}

using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using VVVV.PluginInterfaces.V2;
using System;
using Emgu.CV.Structure;

namespace VVVV.Nodes.EmguCV
{
	public class OpticalFlowLKInstance : IFilterInstance
	{
		private Size FSize;

		private Image<Gray, byte> FCurrent;
		private Image<Gray, byte> FPrevious;
		private Image<Gray, float> FVelocityX;
		private Image<Gray, float> FVelocityY;

		private Size FWindowSize = new Size(5, 5);
		public int WindowSize
		{
			get
			{
				return FWindowSize.Width;
			}
			set
			{
				if (value < 1)
					value = 1;

				if (value > 15)
					value = 15;

				value += (value+1) % 2;

				FWindowSize.Width = value;
				FWindowSize.Height = value;
			}
		}

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
			OpticalFlow.LK(FPrevious, FCurrent, FWindowSize, FVelocityX, FVelocityY);

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
	[PluginInfo(Name = "OpticalFlowLK", Category = "EmguCV", Help = "Perform LK optical flow on image", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class OpticalFlowLKNode : IFilterNode<OpticalFlowLKInstance>
	{
		[Input("Window Size", IsSingle = true, DefaultValue=5, MinValue=1, MaxValue=15)]
		IDiffSpread<int> FPinInWindowSize;

		protected override void Update(int SpreadMax)
		{
			if (FPinInWindowSize.IsChanged)
				for (int i=0; i<SpreadMax; i++)
					FProcessor[i].WindowSize = FPinInWindowSize[0];
		}
	}
}

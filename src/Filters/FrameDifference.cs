#region using
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System;
using VVVV.Utils.VColor;

#endregion

namespace VVVV.Nodes.EmguCV
{
	public enum TDifferenceMode
	{
		Positive,
		Negative,
		AbsoluteDifference
	}

	public class FrameDifferenceInstance : IFilterInstance
	{
		CVImage FBuffer = new CVImage();

		public double Threshold = 0.1;
		private bool FThresholdEnabled = false;
		public bool ThresholdEnabled
		{
			set
			{
				FThresholdEnabled = value;
				ReInitialise();
			}
		}

		public TDifferenceMode DifferenceMode = TDifferenceMode.AbsoluteDifference;

		public override void Initialise()
		{

			if (FThresholdEnabled)
			{
				FOutput.Image.Initialise(FInput.ImageAttributes.Size, TColourFormat.L8);
				FBuffer.Initialise(FInput.ImageAttributes.Size, TColourFormat.L8);
			}
			else
			{
				FOutput.Image.Initialise(FInput.ImageAttributes);
				FBuffer.Initialise(FInput.ImageAttributes);
			}
		}

		public override void Process()
		{
			if (FThresholdEnabled)
			{
				if (FInput.ImageAttributes.ColourFormat != TColourFormat.L8)
				{
					FInput.Image.GetImage(TColourFormat.L8, FOutput.Image);

					if (DifferenceMode == TDifferenceMode.AbsoluteDifference)
						CvInvoke.cvAbsDiff(FOutput.CvMat, FBuffer.CvMat, FOutput.CvMat);

					CvInvoke.cvThreshold(FOutput.CvMat, FOutput.CvMat, 255.0d * Threshold, 255, THRESH.CV_THRESH_BINARY);

					FInput.Image.GetImage(TColourFormat.L8, FBuffer);
				}
				else
				{
					if (DifferenceMode == TDifferenceMode.AbsoluteDifference)
					{
						if (!FInput.LockForReading())
							return;
						CvInvoke.cvAbsDiff(FInput.CvMat, FBuffer.CvMat, FOutput.CvMat);
						FInput.ReleaseForReading();
					}

					CvInvoke.cvThreshold(FOutput.CvMat, FOutput.CvMat, 255.0d * Threshold, 255, THRESH.CV_THRESH_BINARY);
				}
			} else {
				if (DifferenceMode == TDifferenceMode.AbsoluteDifference)
				{
					if (!FInput.LockForReading())
						return;
					CvInvoke.cvAbsDiff(FInput.CvMat, FBuffer.CvMat, FOutput.CvMat);
					FInput.ReleaseForReading();
				}

				FBuffer.SetImage(FInput.Image);
			}

			FOutput.Send();
		}

	}

	#region PluginInfo
	[PluginInfo(Name = "FrameDifference", Category = "EmguCV", Version = "", Help = "Output difference between frames", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class FrameDifferenceNode : IFilterNode<FrameDifferenceInstance>
	{
		[Input("Threshold")]
		IDiffSpread<double> FThreshold;

		[Input("Threshold Enabled")]
		IDiffSpread<bool> FThresholdEnabled;

		[Input("Difference Mode", DefaultEnumEntry = "AbsoluteDifference")]
		IDiffSpread<TDifferenceMode> FDifferenceMode;

		protected override void Update(int InstanceCount)
		{
			if (FThreshold.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Threshold = FThreshold[i];

			if (FThresholdEnabled.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].ThresholdEnabled = FThresholdEnabled[i];

			if (FDifferenceMode.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].DifferenceMode = FDifferenceMode[i];
		}
	}
}

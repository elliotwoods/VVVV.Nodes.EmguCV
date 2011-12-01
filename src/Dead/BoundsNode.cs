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
	public class BoundsInstance : IDestinationInstance
	{
		public Spread<double> Minimum
		{
			get
			{
				Spread<double> value = new Spread<double>(FChannelCount);

				if (FChannelCount > 0)
					value[0] = FMinimum.v0;
				if (FChannelCount > 1)
					value[1] = FMinimum.v1;
				if (FChannelCount > 2)
					value[2] = FMinimum.v2;
				if (FChannelCount > 3)
					value[3] = FMinimum.v3;

				return value;
			}
		}

		public Spread<double> Maximum
		{
			get
			{
				Spread<double> value = new Spread<double>(FChannelCount);

				if (FChannelCount > 0)
					value[0] = FMaximum.v0;
				if (FChannelCount > 1)
					value[1] = FMaximum.v1;
				if (FChannelCount > 2)
					value[2] = FMaximum.v2;
				if (FChannelCount > 3)
					value[3] = FMaximum.v3;

				return value;
			}
		}

		MCvScalar FMinimum = new MCvScalar();
		MCvScalar FMaximum = new MCvScalar();
		int FChannelCount = 1;

		public override void Process()
		{
			FChannelCount = ImageUtils.CountChannels(FInput.ImageAttributes.ColourFormat);

			if (!FInput.LockForReading())
				return;
			CvInvoke.cvAvgSdv(FInput.CvMat, ref FAverage, ref FStandardDeviation, IntPtr.Zero);
			FInput.ReleaseForReading();
		}

	}

	#region PluginInfo
	[PluginInfo(Name = "Bounds", Category = "EmguCV", Version = "", Help = "Returns the upper and lower bounds of the pixel values per channel", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class BoundsNode : IDestinationNode<BoundsInstance>
	{
		[Output("Minimum")]
		ISpread<ISpread<double>> FMinimun;

		[Output("Maximum")]
		ISpread<ISpread<double>> FStandardDeviation;

		protected override void Update(int InstanceCount)
		{
			FAverage.SliceCount = InstanceCount;
			FStandardDeviation.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
				FAverage[i] = FProcessor[i].Average;
				FStandardDeviation[i] = FProcessor[i].StandardDeviation;
			}
		}
	}
}

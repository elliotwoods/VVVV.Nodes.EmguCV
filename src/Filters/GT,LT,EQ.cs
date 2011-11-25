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
	public class GTInstance : IFilterInstance
	{

		public double Threshold = 0.5;

		public override void Initialise()
		{
			FOutput.Image.Initialise(FInput.Image.ImageAttributes.Size, TColourFormat.L8);
		}

		public override void Process()
		{
			if (!FInput.LockForReading())
				return;
			CvInvoke.cvCmpS(FInput.CvMat, Threshold, FOutput.CvMat, CMP_TYPE.CV_CMP_GT);
			FInput.ReleaseForReading();
			FOutput.Send();
		}

	}

	#region PluginInfo
	[PluginInfo(Name = ">", Category = "EmguCV", Version = "Filter, Scalar", Help = "Greater than", Author = "", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class GTNode : IFilterNode<GTInstance>
	{
		[Input("Input 2", DefaultValue = 0.5)]
		IDiffSpread<double> FThreshold;

		protected override void Update(int InstanceCount)
		{
			if (FThreshold.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Threshold = FThreshold[i];
		}
	}

	public class LTInstance : IFilterInstance
	{

		public double Threshold = 0.5;

		public override void Initialise()
		{
			FOutput.Image.Initialise(FInput.Image.ImageAttributes.Size, TColourFormat.L8);
		}

		public override void Process()
		{
			if (!FInput.LockForReading())
				return;
			CvInvoke.cvCmpS(FInput.CvMat, Threshold, FOutput.CvMat, CMP_TYPE.CV_CMP_LT);
			FInput.ReleaseForReading();
			FOutput.Send();
		}

	}
	#region PluginInfo
	[PluginInfo(Name = "<", Category = "EmguCV", Version = "Filter, Scalar", Help = "Less than", Author = "", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class LTNode : IFilterNode<LTInstance>
	{
		[Input("Input 2", DefaultValue = 0.5)]
		IDiffSpread<double> FThreshold;

		protected override void Update(int InstanceCount)
		{
			if (FThreshold.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Threshold = FThreshold[i];
		}
	}

	public class EQInstance : IFilterInstance
	{

		public double Threshold = 0.5;

		public override void Initialise()
		{
			FOutput.Image.Initialise(FInput.Image.ImageAttributes.Size, TColourFormat.L8);
		}

		public override void Process()
		{
			if (!FInput.LockForReading())
				return;
			CvInvoke.cvCmpS(FInput.CvMat, Threshold, FOutput.CvMat, CMP_TYPE.CV_CMP_EQ);
			FInput.ReleaseForReading();
			FOutput.Send();
		}

	}

	#region PluginInfo
	[PluginInfo(Name = "=", Category = "EmguCV", Version = "Filter, Scalar", Help = "Equal to", Author = "", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class EQNode : IFilterNode<EQInstance>
	{
		[Input("Input 2", DefaultValue = 0.5)]
		IDiffSpread<double> FThreshold;

		protected override void Update(int InstanceCount)
		{
			if (FThreshold.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Threshold = FThreshold[i];
		}
	}
}

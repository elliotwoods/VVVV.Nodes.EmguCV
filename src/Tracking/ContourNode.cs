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
	public class ContourInstance : IDestinationInstance
	{
		public bool Enabled = true;

		CVImage FGrayscale = new CVImage();

		public override void Initialise()
		{
			FGrayscale.Initialise(FInput.ImageAttributes.Size, TColourFormat.L8);
		}

		override public void Process()
		{
			if (!Enabled)
				return;

			FInput.Image.GetImage(TColourFormat.L8, FGrayscale);

		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Contour", Category = "EmguCV", Help = "Finds contours in binary image", Tags = "")]
	#endregion PluginInfo
	public class ContourNode : IDestinationNode<ContourInstance>
	{
		#region fields & pins
		[Input("Enabled", DefaultValue = 1)]
		IDiffSpread<bool> FPinInEnabled;

		[Output("Position")]
		ISpread<ISpread<Vector2D>> FPinOutPositionXY;
		#endregion fields & pins

		protected override void Update(int InstanceCount)
		{
			CheckParams(InstanceCount);
			Output(InstanceCount);
		}

		void CheckParams(int InstanceCount)
		{
			if (FPinInEnabled.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Enabled = FPinInEnabled[0];
				}
		}

		void Output(int InstanceCount)
		{
		}
	}
}

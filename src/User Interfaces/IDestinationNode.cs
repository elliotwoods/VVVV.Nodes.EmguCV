using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EmguCV
{
	public abstract class IDestinationNode<T> : IPluginEvaluate where T : IDestinationInstance, new()
	{
		[Input("Input", Order = -1)]
		private ISpread<CVImageLink> FPinInInputImage;

		protected ProcessDestination<T> FProcessor;

		public void Evaluate(int SpreadMax)
		{
			if (FProcessor == null)
				FProcessor = new ProcessDestination<T>(FPinInInputImage);

			FProcessor.CheckInputSize(SpreadMax);

			Update(FProcessor.SliceCount);
		}

		protected abstract void Update(int InstanceCount);
	}
}

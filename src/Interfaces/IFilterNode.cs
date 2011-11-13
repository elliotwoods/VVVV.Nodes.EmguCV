using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EmguCV
{
	public abstract class IFilterNode<T> : IPluginEvaluate where T : IFilterInstance, new()
	{
		[Input("Input", Order = -1)]
		private ISpread<CVImageLink> FInput;

		[Output("Output", Order = -1)]
		private ISpread<CVImageLink> FOutput;

		protected ProcessInputOutputThreaded<T> FProcessor;

		public void Evaluate(int SpreadMax)
		{
			if (FProcessor == null)
				FProcessor = new ProcessInputOutputThreaded<T>(FInput, FOutput);

			FProcessor.CheckInputSize(SpreadMax);

			Update(SpreadMax);
		}

		protected abstract void Update(int SpreadMax);
	}
}

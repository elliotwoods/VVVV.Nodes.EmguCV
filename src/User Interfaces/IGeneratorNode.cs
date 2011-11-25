using System;
using System.Collections.Generic;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EmguCV
{
	public abstract class IGeneratorNode<T> : IPluginEvaluate, IDisposable where T : IGeneratorInstance, new()
	{
		[Output("Output", Order = -1)]
		private ISpread<CVImageLink> FOutput;

		protected ProcessGenerator<T> FProcessor;

		public void Evaluate(int SpreadMax)
		{
			if (FProcessor == null)
				FProcessor = new ProcessGenerator<T>(FOutput, SpreadMax);

			FProcessor.CheckSliceCount(SpreadMax);

			Update(SpreadMax);
		}

		protected abstract void Update(int InstanceCount);

		public void Dispose()
		{
			FProcessor.Dispose();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EmguCV
{
	public abstract class IGeneratorNode<T> : IPluginEvaluate, IDisposable where T : IGeneratorInstance, new()
	{
		[Input("Enabled")]
		private ISpread<bool> FPinInEnabled;

		[Output("Output", Order = -1)]
		private ISpread<CVImageLink> FPinOutOutput;

		[Output("Status")]
		private ISpread<string> FPinOutStatus;

		protected ProcessGenerator<T> FProcessor;

		public void Evaluate(int SpreadMax)
		{
			if (FProcessor == null)
				FProcessor = new ProcessGenerator<T>(FPinOutOutput, SpreadMax);

			FProcessor.CheckSliceCount(SpreadMax);

			for (int i = 0; i < SpreadMax; i++)
				FProcessor[i].Enabled = FPinInEnabled[i];

			Update(SpreadMax);

			FPinOutStatus.SliceCount = SpreadMax;
			for (int i = 0; i < SpreadMax; i++)
				FPinOutStatus[i] = FProcessor[i].Status;
		}

		protected abstract void Update(int InstanceCount);

		public void Dispose()
		{
			
		}
	}
}

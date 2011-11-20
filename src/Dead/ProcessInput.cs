using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EmguCV
{
	class ProcessInput<T> where T : IInstance, IInstanceInput, IDisposable, new()
	{
		CVImageInputSpread FInput;
		public CVImageInputSpread Input { get { return FInput;  } }

		Spread<T> FProcess = new Spread<T>(0);

		public ProcessInput(ISpread<CVImageLink> inputPin)
		{
			FInput = new CVImageInputSpread(inputPin);
		}

		#region Spread access
		public T GetProcessor(int index)
		{
			return FProcess[index];
		}

		public CVImageInput GetInput(int index)
		{
			return FInput[index];
		}

		public int SliceCount
		{
			get
			{
				return FInput.SliceCount;
			}
		}
		#endregion

		public bool CheckInputSize()
		{
			bool changed = FInput.CheckInputSize();

			for (int i = FProcess.SliceCount; i < FInput.SliceCount; i++)
				Add(FInput[i]);

			if (FProcess.SliceCount > FInput.SliceCount)
			{
				for (int i = FInput.SliceCount; i < FProcess.SliceCount; i++)
					FProcess[i].Dispose();
				FProcess.SliceCount = FInput.SliceCount;
			}

			return changed;
		}

		private void Add(CVImageInput input)
		{
			T addition = new T();
			addition.SetInput(input);
			addition.Initialise();

			FProcess.Add(addition);
		}

		protected void Dispose(int i)
		{
			FProcess[i].Dispose();
		}

		protected void Resize(int count)
		{
			FProcess.SliceCount = count;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.Threading;

namespace VVVV.Nodes.EmguCV
{
	class ProcessInputThreaded<T> where T : IInstanceThreaded, IInstanceInput, IDisposable, new()
	{
		CVImageInputSpread FInput;
		public CVImageInputSpread Input { get { return FInput; } }

		Spread<T> FProcess = new Spread<T>(0);

		Thread FThread;
		bool FThreadRunning = false;

		public ProcessInputThreaded(ISpread<CVImageLink> inputPin)
		{
			FInput = new CVImageInputSpread(inputPin);

			CheckInputSize();
			StartThread();
		}

		private void ThreadedFunction()
		{
			while (FThreadRunning)
			{
				for (int i = 0; i < SliceCount; i++)
				{
					if (FInput[i].ImageChanged)
						FProcess[i].Process();
				}
				Thread.Sleep(1);
			}
		}

		private void StartThread()
		{
			FThreadRunning = true;
			FThread = new Thread(ThreadedFunction);
			FThread.Start();
		}

		private void StopThread()
		{
			if (FThreadRunning)
			{
				FThreadRunning = false;
				FThread.Join();
			}
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
				{
					FProcess[i].Dispose();
					FProcess[i] = default(T);
				}

				FProcess.SliceCount = FInput.SliceCount;
			}

			return changed;
		}

		private void Add(CVImageInput input)
		{
			CVImageOutput output = new CVImageOutput();
			T addition = new T();

			addition.SetInput(input);

			FProcess.Add(addition);
		}

		public T this[int index]
		{
			get
			{
				return FProcess[index];
			}
		}
	}
}

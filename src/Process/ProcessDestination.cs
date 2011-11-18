using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.Threading;

namespace VVVV.Nodes.EmguCV
{
	public class ProcessDestination<T> where T : IInstanceThreaded, IInstanceInput, IDisposable, new()
	{
		CVImageInputSpread FInput;
		public CVImageInputSpread Input { get { return FInput; } }

		Spread<T> FProcess = new Spread<T>(0);

		Thread FThread;
		bool FThreadRunning = false;

		public ProcessDestination(ISpread<CVImageLink> inputPin)
		{
			FInput = new CVImageInputSpread(inputPin);
			
			CheckInputSize();
			StartThread();
		}

		private void ThreadedFunction()
		{
			while (FThreadRunning)
			{
				if (FInput.Connected)
				{
					for (int i = 0; i < SliceCount; i++)
					{
						//remove this hack
						FProcess[i].SetInput(FInput[i]);

						if (FInput[i].ImageChanged)
							for (int iProcess = i; iProcess < SliceCount; iProcess += (FInput.SliceCount > 0 ? FInput.SliceCount : int.MaxValue))
								FProcess[iProcess].Process();
					}

					Thread.Sleep(1);
				}
				else
				{
					Thread.Sleep(10);
				}

				
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
				return FProcess.SliceCount;
			}
		}
		#endregion

		public bool CheckInputSize()
		{
			return CheckInputSize(FInput.SliceCount);
		}

		public bool CheckInputSize(int SpreadMax)
		{
			if (!FInput.CheckInputSize() && FProcess.SliceCount==FInput.SliceCount)
				return false;
		
			for (int i = FProcess.SliceCount; i < SpreadMax; i++)
				Add(FInput[i]);

			if (FProcess.SliceCount > SpreadMax)
			{
				for (int i = SpreadMax; i < FProcess.SliceCount; i++)
					Dispose(i);

				FProcess.SliceCount = SpreadMax;
			}

			return true;
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

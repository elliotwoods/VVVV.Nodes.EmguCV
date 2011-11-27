using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.Threading;

namespace VVVV.Nodes.EmguCV
{
	public class ProcessGenerator<T> : IDisposable where T : IGeneratorInstance, new()
	{
		CVImageOutputSpread FOutput;
		public CVImageOutputSpread Output { get { return FOutput; } }

		Spread<T> FProcess = new Spread<T>(0);

		Thread FThread;
		bool FThreadRunning = false;
		Object FLockProcess = new Object();

		public ProcessGenerator(ISpread<CVImageLink> outputPin, int SliceCount)
		{
			FOutput = new CVImageOutputSpread(outputPin);
			CheckSliceCount(SliceCount);

			T testThreaded = new T();
			if (testThreaded.NeedsThread())
				StartThread();
		}

		private void ThreadedFunction()
		{
			while (FThreadRunning)
			{
				lock (FLockProcess)
				{
					try
					{
						for (int i = 0; i < FProcess.SliceCount; i++)
							FProcess[i].Process();
					}
					catch (Exception e)
					{
						ImageUtils.Log(e);
					}
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

		public CVImageOutput GetOutput(int index)
		{
			return FOutput[index];
		}

		public int SliceCount
		{
			get
			{
				return FProcess.SliceCount;
			}
		}
		#endregion

		/// <summary>
		/// Check whether a resize is necessary
		/// </summary>
		/// <param name="SpreadMax"></param>
		/// <returns>Resize occured</returns>
		public bool CheckSliceCount(int SpreadMax)
		{
			if (FProcess.SliceCount == SpreadMax)
				return false;

			lock (FLockProcess)
			{
				for (int i = FProcess.SliceCount; i < SpreadMax; i++)
					Add();

				if (FProcess.SliceCount > SpreadMax)
				{
					for (int i = SpreadMax; i < FProcess.SliceCount; i++)
						Dispose(i);

					FProcess.SliceCount = SpreadMax;
					FOutput.SliceCount = SpreadMax;
				}

				FOutput.AlignOutputPins();
			}

			return true;
		}

		private void Add()
		{
			CVImageOutput output = new CVImageOutput();
			T addition = new T();

			addition.SetOutput(output);

			FProcess.Add(addition);
			FOutput.Add(output);
		}

		public T this[int index]
		{
			get
			{
				return FProcess[index];
			}
		}

		protected void Resize(int count)
		{
			FProcess.SliceCount = count;
			FOutput.AlignOutputPins();
		}

		public void Dispose()
		{
			StopThread();

			foreach (var process in FProcess)
				process.Dispose();

			FOutput.Dispose();
		}

		protected void Dispose(int i)
		{
			FProcess[i].Dispose();
			FOutput[i].Dispose();
		}
	}
}

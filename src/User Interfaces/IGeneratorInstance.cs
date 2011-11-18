using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.EmguCV
{
	public abstract class IGeneratorInstance : IInstance, IInstanceOutput, IDisposable
	{
		protected CVImageOutput FOutput;

		public virtual bool NeedsThread()
		{
			return true;
		}

		public virtual void Initialise() { }

		bool FFirstRun = true;
		virtual public bool NeedsInitialise()
		{
			if (FFirstRun)
			{
				FFirstRun = false;
				return true;
			}
			return false;
		}

		public void Process()
		{
			Generate();
		}

		public void SetOutput(CVImageOutput output)
		{
			FOutput = output;
		}

		/// <summary>
		/// For threaded generators you must override this function
		/// For non-threaded generators, you use your own function
		/// </summary>
		protected virtual void Generate() { }

		virtual public void Dispose()
		{

		}
	}
}

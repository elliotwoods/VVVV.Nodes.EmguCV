using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.EmguCV
{
	public abstract class IGeneratorInstance : IInstanceThreaded, IInstanceOutput, IDisposable
	{
		protected CVImageOutput FOutput;

		public virtual bool NeedsThread()
		{
			return true;
		}

		public void Process()
		{
			Generate();
		}

		public void SetOutput(CVImageOutput output)
		{
			FOutput = output;
		}

		public bool Allocate()
		{
			//not sure what exactly we should be doing here
			//since generators dont need to allocate against an inputted image
			return true;
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

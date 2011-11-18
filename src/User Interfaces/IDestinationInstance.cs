using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.EmguCV
{
	public abstract class IDestinationInstance : IInstance, IInstanceInput, IDisposable
	{
		protected CVImageInput FInput;

		virtual public void Initialise() { }

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

		abstract public void Process();
		
		public void SetInput(CVImageInput input)
		{
			FInput = input;
		}

		virtual public void Dispose()
		{

		}
	}
}

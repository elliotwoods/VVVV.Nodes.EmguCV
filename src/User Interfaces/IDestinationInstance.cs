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

		private bool FNeedsInitialisation = true;
		virtual public bool NeedsInitialise()
		{
			if (FNeedsInitialisation)
			{
				FNeedsInitialisation = false;
				return true;
			}
			return false;
		}

		abstract public void Process();
		
		public void SetInput(CVImageInput input)
		{
			FInput = input;
		}

		public bool HasInput(CVImageInput input)
		{
			return FInput == input;
		}

		virtual public void Dispose()
		{

		}
	}
}

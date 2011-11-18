using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.EmguCV
{
	public abstract class IDestinationInstance : IInstanceThreaded, IInstance, IInstanceInput, IDisposable
	{
		protected CVImageInput FInput;

		virtual public void Initialise() { }
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

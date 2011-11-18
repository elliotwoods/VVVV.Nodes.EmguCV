using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.EmguCV
{
	public abstract class IDestinationInstance : IInstanceThreaded, IInstanceInput, IDisposable
	{
		protected CVImageInput FInput;

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

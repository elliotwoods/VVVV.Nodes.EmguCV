using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.EmguCV
{
	public abstract class IFilterInstance : IInstanceThreaded, IInstanceInput, IInstanceOutput, IDisposable
	{
		protected CVImageInput FInput;
		protected CVImageOutput FOutput;

		abstract protected void AllocateOutput();
		abstract public void Process();

		public void SetInput(CVImageInput input)
		{
			FInput = input;
		}

		public void SetOutput(CVImageOutput output)
		{
			FOutput = output;
		}

		public bool Allocate()
		{
			if (FInput == null)
				return false;

			if (FInput.Allocated) {
				try
				{
					AllocateOutput();
					return true;
				}
				catch
				{
					return false;
				}
			}
			else
				return false;
		}

		virtual public void Dispose()
		{

		}
	}
}

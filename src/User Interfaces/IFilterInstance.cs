using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.EmguCV
{
	public abstract class IFilterInstance : IInstance, IInstanceInput, IInstanceOutput, IDisposable
	{
		protected CVImageInput FInput;
		protected CVImageOutput FOutput;

		/// <summary>
		/// Override this function. It is called whenever the input image's attributes changes or you ask to reallocate
		/// </summary>
		abstract public void Initialise();

		bool FNeedsInitialisation = true;
		virtual public bool NeedsInitialise()
		{
			if (FNeedsInitialisation)
			{
				FNeedsInitialisation = false;
				return true;
			}
			return false;
		}

		protected void ReInitialise()
		{
			FNeedsInitialisation = true;
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

		public void SetOutput(CVImageOutput output)
		{
			FOutput = output;
		}

		/// <summary>
		/// Override this with false if your filter
		/// doesn't need to run every frame
		/// </summary>
		/// <returns></returns>
		virtual public bool IsFast()
		{
			return true;
		}

		virtual public void Dispose()
		{

		}
	}
}

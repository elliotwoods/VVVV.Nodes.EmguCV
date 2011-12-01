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

		private Object FLockStatus = new Object();
		private string FStatus;
		public string Status
		{
			get
			{
				lock (FLockStatus)
					return FStatus;
			}
			set
			{
				lock (FLockStatus)
					FStatus = value;
			}
		}

		protected bool FEnabled;
		public bool Enabled
		{
			get
			{
				return FEnabled;
			}
			set
			{
				if (FEnabled == value)
					return;

				FEnabled = value;
				if (FEnabled)
					Enable();
				else
					Disable();

			}
		}

		/// <summary>
		/// Override this function if you need to do something when FEnabled goes high.
		/// </summary>
		virtual protected void Enable() { }

		/// <summary>
		/// Override this function if you need to do something when FEnabled goes low.
		/// </summary>
		virtual protected void Disable() { }

		virtual public void Dispose()
		{

		}
	}
}

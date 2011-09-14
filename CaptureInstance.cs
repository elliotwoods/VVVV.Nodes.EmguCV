using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VVVV.Nodes.EmguCV
{
	abstract class CaptureInstance
	{
		protected Thread CaptureThread;
		protected bool RunCaptureThread;
		protected bool IsRunning;

		public virtual void Close()
		{
			RunCaptureThread = false;
			CaptureThread.Join(100);
		}

		public abstract void Process();
	}
}

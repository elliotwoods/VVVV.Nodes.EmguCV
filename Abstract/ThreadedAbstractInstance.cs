using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VVVV.Nodes.EmguCV
{
	public abstract class ThreadedAbstractInstance: AbstractInstance
	{
		protected Thread CaptureThread;
		protected bool RunCaptureThread;
		protected bool IsRunning;

		public override void Close()
		{
			RunCaptureThread = false;
			CaptureThread.Join(100);
			IsRunning = false;
		}
	}
}

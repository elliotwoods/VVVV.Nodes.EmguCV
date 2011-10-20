using System.Threading;

namespace VVVV.Nodes.EmguCV
{
	public abstract class ImageProcessingInstance
	{
		protected Thread CaptureThread;
		protected bool RunCaptureThread;
		protected bool IsRunning;

		protected string Status;
		
		public virtual void Close()
		{
			RunCaptureThread = false;
			CaptureThread.Join(100);
			IsRunning = false;
		}

		public abstract void Process();
		
	}
}

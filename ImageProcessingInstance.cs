using System.Threading;

namespace VVVV.Nodes.EmguCV
{
	public abstract class ImageProcessingInstance
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using System.Threading;

namespace VVVV.Nodes.EmguCV
{
	public class CVImageDoubleBuffer
	{
		#region Data
		private CVImage FBackBuffer = new CVImage();
		private CVImage FFrontBuffer = new CVImage();
		private CVImageAttributes FImageAttributes;
		private bool FAllocated = false;
		#endregion

		#region Locking
		private ReaderWriterLock FFrontLock = new ReaderWriterLock();
		private Object FBackLock = new Object();
		private Object FAttributesLock = new Object();
		public static int LockTimeout = 10000;
		#endregion

		#region Events
		//these virtual functions are overwriten in child classes that want to trigger events
		public virtual void OnImageUpdate() { }
		public virtual void OnImageAttributesUpdate(CVImageAttributes attributes) { }
		#endregion

		#region Swapping and copying
		/// <summary>
		/// Swap the front buffer and back buffer
		/// </summary>
		public void Swap()
		{
			lock (FBackLock)
			{
				FFrontLock.AcquireWriterLock(LockTimeout);
				try
				{
					CVImage swap = FBackBuffer;
					FBackBuffer = FFrontBuffer;
					FFrontBuffer = swap;
				}
				finally
				{
					FFrontLock.ReleaseWriterLock();
				}
			}
		}
		#endregion

		#region Get/set the image
		/// <summary>
		/// Copy the input image into the back buffer
		/// </summary>
		/// <param name="image">Input image</param>
		public void SetImage(CVImage image)
		{
			bool Reinitialise;

			lock (FBackLock)
				Reinitialise = FBackBuffer.SetImage(image);

			FAllocated = true;
			OnImageUpdate();

			if (Reinitialise)
			{
				lock (FAttributesLock)
					FImageAttributes = image.ImageAttributes.Clone() as CVImageAttributes;
				OnImageAttributesUpdate(FImageAttributes);
			}

			Swap();
		}

		/// <summary>
		/// Copy the input image into the back buffer
		/// </summary>
		/// <param name="image">Input image</param>
		public void SetImage(IImage image)
		{
			bool Reinitialise;

			lock (FBackLock)
				Reinitialise = FBackBuffer.SetImage(image);
			
			FAllocated = true;
			OnImageUpdate();

			if (Reinitialise)
			{
				lock (FAttributesLock)
					FImageAttributes = FBackBuffer.ImageAttributes.Clone() as CVImageAttributes;
				OnImageAttributesUpdate(FImageAttributes);
			}

			Swap();
		}
		#endregion

		#region Accessors
		/// <summary>
		/// Get the front buffer. Be sure to lock the front buffer for reading!
		/// </summary>
		public CVImage Image
		{
			get { return FFrontBuffer; }
		}

		public CVImageAttributes ImageAttributes
		{
			get {
				lock (FAttributesLock)
					return FImageAttributes;
			}
		}

		public bool Allocated
		{
			get
			{
				return FAllocated;
			}
		}

		public ReaderWriterLock FrontLock
		{
			get
			{
				return FFrontLock;
			}
		}
		#endregion
	}
}

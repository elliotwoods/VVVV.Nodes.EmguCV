using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Runtime.InteropServices;

namespace VVVV.Nodes.EmguCV
{

	public class ImageAttributesChangedEventArgs : EventArgs
	{
		public CVImageAttributes Attributes { get; private set; }

		public ImageAttributesChangedEventArgs(CVImageAttributes attributes)
		{
			this.Attributes = attributes;
		}
	}

	public abstract class ImageBase
	{
		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
		static extern private void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

		protected Object FLock = new Object();
		protected CVImageAttributes FImageAttributes = new CVImageAttributes();


		public bool HasAllocatedImage
		{
			get
			{
				return GetImage() != null;
			}
		}

		#region Events

		#region ImageUpdate
		public event EventHandler ImageUpdate;

		protected void OnImageUpdate()
		{
			if (ImageUpdate == null)
				return;
			ImageUpdate(this, EventArgs.Empty);
		}
		#endregion

		#region ImageAttributesUpdate
		public event EventHandler<ImageAttributesChangedEventArgs> ImageAttributesUpdate;

		protected void OnImageAttributesUpdate(CVImageAttributes attributes)
		{
			if (ImageAttributesUpdate == null)
				return;
			ImageAttributesUpdate(this, new ImageAttributesChangedEventArgs(attributes));
		}
		#endregion

		#endregion

		public object GetLock()
		{
			return FLock;
		}

		abstract public IImage GetImage();

		public CVImageAttributes ImageAttributes
		{
			get
			{
				return FImageAttributes;
			}
		}

		public TColourFormat NativeFormat
		{
			get
			{
				return ImageAttributes.ColourFormat;
			}
		}

		abstract public void Allocate();

		public int Width
		{
			get
			{
				return FImageAttributes.Width;
			}
		}

		public int Height
		{
			get
			{
				return FImageAttributes.Height;
			}
		}

		public System.Drawing.Size Size
		{
			get
			{
				if (GetImage() == null)
					return new System.Drawing.Size(0,0);
				else
					return GetImage().Size;
			}
		}

		public IntPtr Ptr
		{
			get
			{
				return GetImage().Ptr;
			}
		}
	}
}

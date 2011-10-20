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

	public class ImageRGB
	{
		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
		static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

		private Object FLock = new Object();
		private Image<Bgr, byte> FImage;
		private CVImageAttributes FImageAttributes = new CVImageAttributes();

		public bool Initialised
		{
			get
			{
				return FImage != null;
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

		public CVImageAttributes ImageAttributes
		{
			get
			{
				return FImageAttributes;
			}
		}

		public TColourData NativeType
		{
			get
			{
				return ImageAttributes.ColourType;
			}
		}

		public Image<Bgr, byte> Image
		{
			get
			{
				return FImage;
			}
		}

		public void SetImage(Image<Bgr, byte> value)
		{
			lock (FLock)
			{
				bool changedAttributes = FImageAttributes.CheckChanges(TColourData.RGB8, value.Size);

				if (ImageAttributes.Initialised)
				{
					if (changedAttributes)
					{
						Allocate();
						OnImageAttributesUpdate(ImageAttributes);
					}

					this.FImage = value;

					if (ImageUpdate != null)
						OnImageUpdate();
				}
			}
		}

		private void Allocate()
		{
			FImage = new Image<Bgr, byte>(ImageAttributes.Width, ImageAttributes.Height);
		}

		public int Width
		{
			get
			{
				if (FImage == null)
					return 0;
				else
					return FImage.Size.Width;
			}
		}

		public int Height
		{
			get
			{
				if (FImage == null)
					return 0;
				else
					return FImage.Size.Height;
			}
		}

		public IntPtr Ptr
		{
			get
			{
				return FImage.Ptr;
			}
		}
	}
}

using System;
using Emgu.CV;
using Emgu.CV.Structure;

namespace VVVV.Nodes.EmguCV
{
	class ImageRGB
	{
		public Object Lock = new Object();
		private Image<Bgr, byte> FImg;

		public bool FrameAttributesChanged;

		public bool FrameChanged { get; private set; }

		public ImageRGB()
		{
			FrameChanged = false;
		}

		public Image<Bgr, byte> Img
		{
			get
			{
				return FImg;
			}

			set
			{
				FImg = value;
				if (value == null)
				{
					FrameAttributesChanged = true;
					FrameChanged = false;
				}
				else
				{
					if (Width != value.Width || Height != value.Height)
						FrameAttributesChanged = true;
					else
						FrameAttributesChanged = false;
				}
				FrameChanged = true;
			}
		}

		public int Width
		{
			get 
			{
				return Img == null ? 0 : Img.Width;
			}
		}

		public int Height
		{
			get 
			{
				return Img == null ? 0 : Img.Height;
			}
		}

		public IntPtr Ptr
		{
			get
			{
				return Img.Ptr;
			}
		}
	}
}

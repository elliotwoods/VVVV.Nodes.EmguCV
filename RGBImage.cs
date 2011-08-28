using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

namespace VVVV.Nodes.EmguCV
{
	class ImageRGB
	{
		public Object Lock = new Object();
		private Image<Bgr, byte> _Img;
		public bool FrameChanged = false;
		public bool FrameAttributesChanged = false;

		public Image<Bgr, byte> Img
		{
			get
			{
				return _Img;
			}

			set
			{
				_Img = value;
				if (value == null)
				{
					FrameAttributesChanged = true;
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
				if (Img == null)
					return 0;
				else
					return Img.Width;
			}
		}

		public int Height
		{
			get
			{
				if (Img == null)
					return 0;
				else
					return Img.Height;
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

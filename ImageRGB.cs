using System;
using Emgu.CV;
using Emgu.CV.Structure;

namespace VVVV.Nodes.EmguCV
{
	class ImageRGB
	{
		public Object Lock = new Object();
		private Image<Bgr, byte> FImg;

		//TODO: FrameAttributesChanges must be private for set i think. 
		//Setting it "true" in VideoPlayerNode looks like a hack.
		
		public bool FrameAttributesChanged { get; set; }
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
					FrameChanged = true;

					if (Width != value.Width || Height != value.Height) FrameAttributesChanged = true;
					else FrameAttributesChanged = false;
				}
				
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

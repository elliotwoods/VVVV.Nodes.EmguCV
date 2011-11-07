using System;
using Emgu.CV;
using Emgu.CV.Structure;

namespace VVVV.Nodes.EmguCV.Abstract
{
	public abstract class ImageProcessingInstance: AbstractInstance
	{
		protected bool Initialised;
		protected Object Lock = new object();
		protected Image<Bgr, byte> BufferImage;
		protected ImageRGB Image;

		public void SetImage(ImageRGB image)
		{
			Image = image;
		}

		public override void Close()
		{
			BufferImage = null;
			Initialised = false;
		}

		public override void Initialise()
		{
			Close();

			lock(Lock)
			{
				if (Image == null || !Image.HasAllocatedImage)
				{
					Image = null;
					Initialised = false;
					return;
				}

				Image.ImageUpdate += ImageUpdate;
				Image.ImageAttributesUpdate += ImageAttributesUpdate;

				Initialised = true;
			}
		}

		protected virtual void ImageUpdate(object sender, EventArgs e)
		{
			Process();
		}

		protected virtual void ImageAttributesUpdate(object sender, ImageAttributesChangedEventArgs e)
		{
			
		}

		protected virtual void Allocate(CVImageAttributes attributes)
		{
			BufferImage = new Image<Bgr, byte>(attributes.Width, attributes.Height);
		}
	}
}

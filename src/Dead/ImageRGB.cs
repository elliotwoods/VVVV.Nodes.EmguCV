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


	public class ImageRGB : ImageBase
	{

		private Image<Bgr, byte> FImage;

		public override IImage GetImage()
		{
			return FImage;
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
				bool changedAttributes = FImageAttributes.CheckChanges(TColourFormat.RGB8, value.Size);

				if (ImageAttributes.Initialised)
				{
					if (changedAttributes)
					{
						Allocate();
						OnImageAttributesUpdate(ImageAttributes);
					}

					this.FImage = value;

					OnImageUpdate();
				}
			}
		}

		override public void Allocate()
		{
			FImage = new Image<Bgr, byte>(ImageAttributes.Width, ImageAttributes.Height);
		}
	}
}

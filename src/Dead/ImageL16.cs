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


	public class ImageL16: ImageBase
	{

		private Image<Gray, ushort> FImage;


		public override IImage GetImage()
		{
			return FImage;
		}

		public Image<Gray, ushort> Image
		{
			get
			{
				return FImage;
			}
		}

		public void SetImage(Image<Gray, ushort> value)
		{
			lock (FLock)
			{
				bool changedAttributes = FImageAttributes.CheckChanges(TColourFormat.L16, value.Size);

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
			FImage = new Image<Gray, ushort>(ImageAttributes.Width, ImageAttributes.Height);
		}
	}
}

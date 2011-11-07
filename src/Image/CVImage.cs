using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Runtime.InteropServices;
using Emgu.CV.CvEnum;

namespace VVVV.Nodes.EmguCV
{
	class CVImage : ImageBase
	{
		IImage FImage;

		public override IImage GetImage()
		{
			return FImage;
		}

		public void Initialise(CVImageAttributes attributes)
		{
			Initialise(attributes.Size, attributes.ColourFormat);
		}
		public void Initialise(System.Drawing.Size size, TColourFormat format)
		{
			bool changedAttributes = FImageAttributes.CheckChanges(format, size);

			if (changedAttributes)
			{
				Allocate();
				OnImageAttributesUpdate(ImageAttributes);
			}
		}

		public IImage GetImage(TColourFormat format)
		{
			if (format == this.NativeFormat)
				return this.GetImage();
			else
			{
				return CVImageUtils.CreateConverted(FImage, format);
			}
		}

		public unsafe void SetImage(IImage source)
		{
			lock (FLock)
			{
				TColourFormat sourceFormat = CVImageUtils.GetFormat(source);
				Initialise(source.Size, sourceFormat);

				CvInvoke.cvCopy(source.Ptr, FImage.Ptr, (new Image<Gray, byte>(Width, Height,new Gray(1.0d))).Ptr);

				OnImageUpdate();
			}
		}

		public void SetImage(CVImage source)
		{
			lock (FLock)
			{
				lock (source.FLock)
				{
					Initialise(source.Size, source.NativeFormat);

					CvInvoke.cvCopy(source.Ptr, FImage.Ptr, (new Image<Gray, byte>(Width, Height, new Gray(1.0d))).Ptr);

					OnImageUpdate();
				}
			}
		}

		override public void Allocate()
		{
			// perhaps this.GetImage cannot be assigned to?
			FImage = CVImageUtils.CreateImage(this.Width, this.Height, this.NativeFormat);
		}
	}
}

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
	public class CVImage : ImageBase
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

		public bool Initialise(System.Drawing.Size size, TColourFormat format)
		{
			bool changedAttributes = FImageAttributes.CheckChanges(format, size);

			if (changedAttributes)
			{
				Allocate();
				return true;
			}
			else
				return false;
		}

		public void GetImage(TColourFormat format, CVImage target)
		{
			if (format == this.NativeFormat)
				CVImageUtils.CopyImage(this, target);
			else
				CVImageUtils.CopyImageConverted(this, target);
		}

		public unsafe bool SetImage(IImage source)
		{
			if (source == null)
				return false;

			TColourFormat sourceFormat = CVImageUtils.GetFormat(source);
			bool Reinitialise = Initialise(source.Size, sourceFormat);

			CVImageUtils.CopyImage(source, this);

			return Reinitialise;
		}

		public bool SetImage(CVImage source)
		{
			if (source == null)
				return false;

			bool Reinitialise = Initialise(source.Size, source.NativeFormat);

			CVImageUtils.CopyImage(source, this);

			return Reinitialise;
		}

		override public void Allocate()
		{
			FImage = CVImageUtils.CreateImage(this.Width, this.Height, this.NativeFormat);
		}
	}
}

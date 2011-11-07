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
		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
		static extern protected void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

		IImage FImage;

		public override IImage GetImage()
		{
			return FImage;
		}

		public IImage GetImage(TColourData format)
		{
			if (format == this.NativeType)
				return this.GetImage();
			else
			{
				COLOR_CONVERSION route = CVImageConversion.Convert(this.NativeType, format);
				if (route==COLOR_CONVERSION.CV_COLORCVT_MAX)
				{
					throw(new Exception("Unsupported conversion"));
				} else {
					IImage converted = CVImageConversion.CreateImage(this.Width, this.Height, this.NativeType);
					CvInvoke.cvCvtColor(this.Ptr, converted.Ptr, route);
					return converted;
				}
			}
		}

		public void SetImage(IImage source)
		{
			lock (FLock)
			{
				if (ImageAttributes.Initialised)
				{
					TColourData sourceFormat = CVImageConversion.GetFormat(source);
					bool changedAttributes = FImageAttributes.CheckChanges(sourceFormat, source.Size);

					if (changedAttributes)
					{
						Allocate();
						OnImageAttributesUpdate(ImageAttributes);
					}

					CopyMemory(this.GetImage().Ptr, source.Ptr, FImageAttributes.SizeInBytes);

					OnImageUpdate();
				}
			}
		}

		override public void Allocate()
		{
			// perhaps this.GetImage cannot be assigned to?
			FImage = CVImageConversion.CreateImage(this.Width, this.Height, this.NativeType);
		}
	}
}

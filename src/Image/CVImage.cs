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
	public class CVImage : ImageBase, IDisposable
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

			if (changedAttributes || this.Allocated == false)
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
				ImageUtils.CopyImage(this, target);
			else
				ImageUtils.CopyImageConverted(this, target);
		}

		public void GetImage(CVImage target)
		{
			GetImage(target.ImageAttributes.ColourFormat, target);
		}

		public unsafe bool SetImage(IImage source)
		{
			if (source == null)
				return false;

			TColourFormat sourceFormat = ImageUtils.GetFormat(source);
			bool Reinitialise = Initialise(source.Size, sourceFormat);

			ImageUtils.CopyImage(source, this);

			return Reinitialise;
		}

		public bool SetImage(CVImage source)
		{
			if (source == null)
				return false;

			bool Reinitialise = Initialise(source.Size, source.NativeFormat);

			ImageUtils.CopyImage(source, this);

			return Reinitialise;
		}

		/// <summary>
		/// Copy data from pointer. Presume we're initialised and data is of correct size
		/// </summary>
		/// <param name="rawData">Raw pixel data of correct size</param>
		public void SetPixels(IntPtr rawData)
		{
			if (rawData == IntPtr.Zero)
				return;

			ImageUtils.CopyImage(rawData, this);
		}

		override public void Allocate()
		{
			FImage = ImageUtils.CreateImage(this.Width, this.Height, this.NativeFormat);
		}

		public void LoadFile(string filename)
		{
			this.SetImage(new Image<Bgr, byte>(filename));
		}

		public void Dispose()
		{
			if (FImage != null)
			{
				FImage.Dispose();
				FImage = null;
			}
		}
	}
}

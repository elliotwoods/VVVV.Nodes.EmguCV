using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;

namespace VVVV.Nodes.EmguCV
{
	class CVImageLink
	{
		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
		static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

		#region Private data
		/// <summary>
		/// Only one image type should be in use
		/// at any one time within an Image instance
		/// </summary>
		private Object FLock = new Object();
		private CVImageAttributes FImageAttributes = new CVImageAttributes();
		
		//converted images
		private IImage FIImage;
		private Image<Rgb, byte> FImageRGB;
		private Image<Rgba, byte> FImageRGBA;
		private Image<Gray, byte> FImageGray;
		#endregion

		#region Public data
		public delegate void ImageChangedHandler(CVImageLink image, TColourData native);
		public event ImageChangedHandler ImageUpdate;

		public delegate void ImageAttributesChangedHandler(CVImageAttributes attributes);
		public event ImageChangedHandler ImageAttributesUpdate;
		#endregion

		public CVImageAttributes ImageAttributes
		{
			get
			{
				return FImageAttributes;
			}
		}

		public TColourData NativeType {
			get {
				return ImageAttributes.ColourType;
			}
		}

		public void GetImage(Image<Rgba, byte> image)
		{
			if (NativeType == TColourData.RGBA8)
			{
				if (FImageRGBA != null)
				{
					lock (FLock)
					{
						image = FImageRGBA.Copy();
					}
				}
			} else {
				if (FImageRGBA == null)
					FImageRGBA = new Image<Rgba, byte>(Width, Height);

				lock (FLock)
				{
					switch (ImageAttributes.ColourType)
					{
						case TColourData.L8:
							CvInvoke.cvCvtColor(FImageGray.Ptr, FImageRGBA.Ptr, COLOR_CONVERSION.CV_GRAY2RGB);
							break;

						case TColourData.RGBA8:
							CvInvoke.cvCvtColor(FImageRGB.Ptr, FImageRGBA.Ptr, COLOR_CONVERSION.CV_BGRA2BGR);
							break;
					}
					image = FImageRGBA.Copy();
				}			
			}
		}

		public void UpdateImage(Image<Bgr, byte> value)
		{
			//through some delusion. so far this appears to be true ?
			//i.e. bgr is actually rgb
			SetImage(value, TColourData.RGB8);
		}

		public void SetImage(Image<Rgb, byte> value)
		{
			SetImage(value, TColourData.RGB8);
		}

		public void SetImage(Image<Rgba, byte> value)
		{
			SetImage(value, TColourData.RGB8);
		}

		public void SetImage(Image<Gray, byte> value)
		{
			SetImage(value, TColourData.L8);
		}

		private void SetImage(IImage value, TColourData type)
		{
			lock (FLock)
			{
				bool changedAttributes = FImageAttributes.CheckChanges(type, value.Size);

				if (ImageAttributes.Initialised)
				{

					if (changedAttributes)
					{
						Allocate();
					}

					switch (type)
					{
						case TColourData.RGB8:
							CopyMemory(FImageRGB.Ptr, value.Ptr, (uint)(Width * Height * value.NumberOfChannels));
							break;
					}
					

					if (ImageUpdate != null)
						ImageUpdate(this, type);
				}
			}
		}

		private void Allocate()
		{
			switch(ImageAttributes.ColourType) {
				case TColourData.L8:
					FImageGray = new Image<Gray, byte>(ImageAttributes.Width, ImageAttributes.Height);
					FIImage = FImageGray;
					break;

				case TColourData.RGB8:
					FImageRGB = new Image<Rgb, byte>(ImageAttributes.Width, ImageAttributes.Height);
					FIImage = FImageRGB;
					break;
			
				case TColourData.RGBA8:
					FImageRGBA = new Image<Rgba, byte>(ImageAttributes.Width, ImageAttributes.Height);
					FIImage = FImageRGBA;
					break;
			}
		}

		public int Width
		{
			get
			{
				if (FIImage == null)
					return 0;
				else
					return FIImage.Size.Width;
			}
		}

		public int Height
		{
			get
			{
				if (FIImage == null)
					return 0;
				else
					return FIImage.Size.Height;
			}
		}

		public IntPtr Ptr
		{
			get
			{
				return FIImage.Ptr;
			}
		}
	}
}

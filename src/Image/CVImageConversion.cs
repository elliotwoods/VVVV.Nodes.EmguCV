using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.CvEnum;
using Emgu.CV;
using Emgu.CV.Structure;

namespace VVVV.Nodes.EmguCV
{
	class CVImageConversion
	{
		public static COLOR_CONVERSION Convert(TColourData src, TColourData dst)
		{
			switch (src)
			{
				case TColourData.L8:
					{
						switch (dst)
						{
							case TColourData.RGBA8:
								return COLOR_CONVERSION.CV_GRAY2RGBA;
						}
						break;
					}

				case TColourData.RGB8:
					{
						switch (dst)
						{
							case TColourData.RGBA8:
								return COLOR_CONVERSION.CV_RGB2RGBA;
						}
						break;
					}
			}

			return COLOR_CONVERSION.CV_COLORCVT_MAX;
		}

		public static IImage CreateImage(int width, int height, TColourData format)
		{
			switch(format)
			{
				case TColourData.L8:
					return new Image<Gray, byte>(width, height);
				case TColourData.L16:
					return new Image<Gray, ushort>(width, height);


				case TColourData.RGB8:
					return new Image<Rgb, byte>(width, height);
				case TColourData.RGB32F:
					return new Image<Rgb, float>(width, height);

				case TColourData.RGBA8:
					return new Image<Rgba, byte>(width, height);
			}

			throw (new NotImplementedException("We have not implemented the automatic creation of this image type"));
		}

		public static TColourData GetFormat(IImage image)
		{
			Image<Gray, byte> ImageL8 = image as Image<Gray, byte>;
			if (ImageL8 != null)
				return TColourData.L8;

			Image<Gray, ushort> ImageL16 = image as Image<Gray, ushort>;
			if (ImageL16 != null)
				return TColourData.L16;
			
			Image<Rgb, byte> ImageRGB8 = image as Image<Rgb, byte>;
			if (ImageRGB8 != null)
				return TColourData.RGB8;

			Image<Rgb, float> ImageRGB32F = image as Image<Rgb, float>;
			if (ImageRGB32F != null)
				return TColourData.RGB32F;

			Image<Rgba, byte> ImageRGBA8 = image as Image<Rgba, byte>;
			if (ImageRGBA8 != null)
				return TColourData.RGBA8;

			return TColourData.UnInitialised;
		}

		public static uint BytesPerPixel(TColourData format)
		{
			switch (format)
			{
				case TColourData.L8:
					return 1;
				case TColourData.L16:
					return 2;

				case TColourData.RGB8:
					return 3;
				case TColourData.RGB32F:
					return 3 * sizeof(float);

				case TColourData.RGBA8:
					return 4 * sizeof(float);

				default:
					throw(new NotImplementedException("We haven't implemented BytesPerPixel for this type"));
			}
		}
	}
}

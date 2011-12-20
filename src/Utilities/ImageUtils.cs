using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.CvEnum;
using Emgu.CV;
using Emgu.CV.Structure;
using SlimDX.Direct3D9;
using System.Drawing;
using System.Runtime.InteropServices;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EmguCV
{
	public class ImageUtils
	{
		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
		public static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

		public static void Log(Exception e)
		{
			System.Diagnostics.Debug.Print(e.Message);
		}

		public static COLOR_CONVERSION ConvertRoute(TColourFormat src, TColourFormat dst)
		{
			switch (src)
			{
				case TColourFormat.L8:
					{
						switch (dst)
						{
							case TColourFormat.RGBA8:
								return COLOR_CONVERSION.CV_GRAY2RGBA;
						}
						break;
					}

				case TColourFormat.RGB8:
					{
						switch (dst)
						{
							case TColourFormat.L8:
								return COLOR_CONVERSION.CV_RGB2GRAY;

							case TColourFormat.RGBA8:
								return COLOR_CONVERSION.CV_RGB2RGBA;
						}
						break;
					}

				case TColourFormat.RGB32F:
					{
						switch (dst)
						{
							case TColourFormat.RGBA32F:
								return COLOR_CONVERSION.CV_RGB2RGBA;
						}
						break;
					}
			}

			return COLOR_CONVERSION.CV_COLORCVT_MAX;
		}

		public static IImage CreateImage(int width, int height, TColourFormat format)
		{
			switch(format)
			{
				case TColourFormat.L8:
					return new Image<Gray, byte>(width, height);
				case TColourFormat.L16:
					return new Image<Gray, ushort>(width, height);
				case TColourFormat.L32S:
					return new Image<Gray, int>(width, height);
				case TColourFormat.L32F:
					return new Image<Gray, float>(width, height);

				case TColourFormat.RGB8:
					return new Image<Rgb, byte>(width, height);
				case TColourFormat.RGB32F:
					return new Image<Rgb, float>(width, height);

				case TColourFormat.RGBA8:
					return new Image<Rgba, byte>(width, height);
				case TColourFormat.RGBA32F:
					return new Image<Rgba, float>(width, height);
			}

			throw (new NotImplementedException("We have not implemented the automatic creation of this image type"));
		}

		public static TColourFormat GetFormat(IImage image)
		{
			Image<Gray, byte> ImageL8 = image as Image<Gray, byte>;
			if (ImageL8 != null)
				return TColourFormat.L8;

			Image<Gray, ushort> ImageL16 = image as Image<Gray, ushort>;
			if (ImageL16 != null)
				return TColourFormat.L16;
			
			Image<Rgb, byte> ImageRGB8 = image as Image<Rgb, byte>;
			if (ImageRGB8 != null)
				return TColourFormat.RGB8;
			//camera captures seem to arrive as bgr even though rgb
			//may need to revisit this later on
			Image<Bgr, byte> ImageBGR8 = image as Image<Bgr, byte>;
			if (ImageBGR8 != null)
				return TColourFormat.RGB8;

			Image<Rgb, float> ImageRGB32F = image as Image<Rgb, float>;
			if (ImageRGB32F != null)
				return TColourFormat.RGB32F;

			Image<Rgba, byte> ImageRGBA8 = image as Image<Rgba, byte>;
			if (ImageRGBA8 != null)
				return TColourFormat.RGBA8;

			return TColourFormat.UnInitialised;
		}

		public static uint BytesPerPixel(TColourFormat format)
		{
			switch (format)
			{
				case TColourFormat.L8:
					return 1;
				case TColourFormat.L16:
					return 2;

				case TColourFormat.RGB8:
					return 3;

				case TColourFormat.RGB32F:
					return 3 * sizeof(float);

				case TColourFormat.RGBA8:
					return 4;

				case TColourFormat.RGBA32F:
					return 4 * sizeof(float);

				default:
					throw(new NotImplementedException("We haven't implemented BytesPerPixel for this type"));
			}
		}

		public static int ChannelCount(TColourFormat format)
		{
			switch (format)
			{
				case TColourFormat.L8:
					return 1;
				case TColourFormat.L16:
					return 1;

				case TColourFormat.RGB8:
					return 3;

				case TColourFormat.RGB32F:
					return 3;

				case TColourFormat.RGBA8:
					return 4;

				case TColourFormat.RGBA32F:
					return 4;

				default:
					return 0;
			}
		}

		public static TChannelFormat ChannelFormat(TColourFormat format)
		{
			switch(format)
			{
				case TColourFormat.L8:
				case TColourFormat.RGB8:
				case TColourFormat.RGBA8:
					return TChannelFormat.Byte;

				case TColourFormat.L16:
					return TChannelFormat.UShort;

				case TColourFormat.L32F:
				case TColourFormat.RGB32F:
				case TColourFormat.RGBA32F:
					return TChannelFormat.Float;

				default:
					throw (new Exception("We haven't implemented ChannelFormat for this TColourFormat"));
			}
		}

		public static Format GetDXFormat(TColourFormat format)
		{
			switch (format)
			{
				case TColourFormat.L8:
					return Format.L8;
				case TColourFormat.L16:
					return Format.L16;

				case TColourFormat.RGBA32F:
					return Format.A32B32G32R32F;

				case TColourFormat.RGBA8:
					return Format.A8R8G8B8;

				default:
					throw (new NotImplementedException("Cannot create a texture to match Image's format"));
			}
		}

		public static string AsString(TColourFormat format)
		{
			switch (format)
			{
				case TColourFormat.L8:
					return "L8";
				case TColourFormat.L16:
					return "L16";

				case TColourFormat.RGB8:
					return "RGB8";

				case TColourFormat.RGB32F:
					return "RGB32F";

				case TColourFormat.RGBA8:
					return "RGBA8";

				case TColourFormat.RGBA32F:
					return "RGBA32F";

				default:
					throw (new NotImplementedException("We haven't implemented AsString for this type"));
			}
		}

		public static Texture CreateTexture(CVImageAttributes attributes, Device device)
		{
			TColourFormat format = attributes.ColourFormat;
			TColourFormat newFormat;
			bool useConverted = NeedsConversion(format, out newFormat);

			try
			{
				return new Texture(device, Math.Max(attributes.Width, 1), Math.Max(attributes.Height, 1), 1, Usage.None, GetDXFormat(useConverted ? newFormat : format), Pool.Managed);
			}
			catch (Exception e)
			{
				ImageUtils.Log(e);
				return new Texture(device, 1, 1, 1, Usage.None, Format.X8R8G8B8, Pool.Managed);
			}
		}

		public static bool NeedsConversion(TColourFormat format, out TColourFormat targetFormat)
		{
			switch(format)
			{
				case TColourFormat.RGB8:
					targetFormat = TColourFormat.RGBA8;
					return true;

				case TColourFormat.RGB32F:
					targetFormat = TColourFormat.RGBA32F;
					return true;

				default:
					targetFormat = TColourFormat.UnInitialised;
					return false;
			}
		}

		public static void CopyImage(CVImage source, CVImage target)
		{
			if (source.Size != target.Size)
				throw (new Exception("Can't copy between these 2 images, they differ in dimensions"));

			if (source.NativeFormat != target.NativeFormat)
				throw (new Exception("Can't copy between these 2 images, they differ in pixel colour format"));

			CopyImage(source.CvMat, target.CvMat, target.ImageAttributes.BytesPerFrame);
		}

		public static void CopyImage(IImage source, CVImage target)
		{
			if (source.Size != target.Size)
				throw (new Exception("Can't copy between these 2 images, they differ in dimensions"));

			if (GetFormat(source) != target.NativeFormat)
				throw (new Exception("Can't copy between these 2 images, they differ in pixel colour format"));

			CopyImage(source.Ptr, target.CvMat, target.ImageAttributes.BytesPerFrame);
		}

		public static void CopyImage(IntPtr rawData, CVImage target)
		{
			CopyMemory(target.Data, rawData, target.ImageAttributes.BytesPerFrame);
		}

		/// <summary>
		/// Copys by hand raw image data from source to target
		/// </summary>
		/// <param name="source">CvArray object</param>
		/// <param name="target">CvArray object</param>
		/// <param name="size">Size in bytes</param>
		public static void CopyImage(IntPtr source, IntPtr target, uint size)
		{
			IntPtr sourceRaw;
			IntPtr targetRaw;

			int step;
			Size dims;

			CvInvoke.cvGetRawData(source, out sourceRaw, out step, out dims);
			CvInvoke.cvGetRawData(target, out targetRaw, out step, out dims);

			CopyMemory(targetRaw, sourceRaw, size);
		}

		public static void CopyImageConverted(CVImage source, CVImage target)
		{
			//CvInvoke.cvConvert(source.CvMat, target.CvMat);
			//return;

			COLOR_CONVERSION route = ConvertRoute(source.NativeFormat, target.NativeFormat);

			if (route == COLOR_CONVERSION.CV_COLORCVT_MAX)
			{
				CvInvoke.cvConvert(source.CvMat, target.CvMat);
			} else {
				try
				{
					CvInvoke.cvCvtColor(source.CvMat, target.CvMat, route);
				}
				catch
				{
					//CV likes to throw here sometimes, but the next frame it's fine
				}
			}

		}

		public static bool IsIntialised(IImage image)
		{
			if (image == null)
				return false;

			if (image.Size.Width==0 || image.Size.Height==0)
				return false;

			return true;
		}

		/// <summary>
		/// Get a pixel's channels as doubles
		/// </summary>
		/// <param name="source">Image to lookup</param>
		/// <param name="row">0.0 to 1.0</param>
		/// <param name="column">0.0 to 1.0</param>
		/// <returns></returns>
		public static Spread<double> GetPixelAsDoubles(CVImage source, double x, double y)
		{
			uint row = (uint) (x * (double)source.Width);
			uint col = (uint) (y * (double)source.Height);

			return GetPixelAsDoubles(source, row, col);
		}

		public static unsafe Spread<double> GetPixelAsDoubles(CVImage source, uint column, uint row)
		{
			TColourFormat format = source.ImageAttributes.ColourFormat;
			uint channelCount = (uint)ChannelCount(format);

			if (channelCount == 0)
			{
				return new Spread<double>(0);
			}

			uint width = (uint)source.Width;
			uint height = (uint)source.Height;
			Spread<double> output = new Spread<double>((int)channelCount);

			row %= height;
			column %= width;

			switch (ChannelFormat(format))
			{
				case TChannelFormat.Byte:
					{
						byte* d = (byte*)source.Data.ToPointer();
						for (uint channel = 0; channel < channelCount; channel++)
						{
							output[(int)channel] = (double)d[(column + row * width) * channelCount + channel];
						}
						break;
					}

				case TChannelFormat.Float:
					{
						float* d = (float*)source.Data.ToPointer();
						for (uint channel = 0; channel < channelCount; channel++)
						{
							output[(int)channel] = (double)d[(column + row * width) * channelCount + channel];
						}
						break;
					}

				case TChannelFormat.UShort:
					{
						ushort* d = (ushort*)source.Data.ToPointer();
						for (uint channel = 0; channel < channelCount; channel++)
						{
							output[(int)channel] = (double)d[(column + row * width) * channelCount + channel];
						}
						break;
					}
			}
			return output;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using System.Drawing;

namespace VVVV.Nodes.EmguCV
{
	class AsTextureImageInstance
	{
		//    [DllImport("msvcrt.dll", EntryPoint = "memcpy")]
		//    public unsafe static extern void CopyMemory(IntPtr pDest, IntPtr pSrc, int length);

		public int Width { get; private set; }
		public int Height { get; private set; }

		CVImage FImage;
		CVImage FBuffer;
		CVImage FBufferConverted;

		TColourFormat FConvertedFormat;
		bool FNeedsConversion = false;

		public bool Initialised { get; private set; }
		public bool ReinitialiseTexture { get; private set; }

		public Object Lock = new Object();

		public void Initialise(CVImage image)
		{
			if (image == FImage && Initialised)
				return;

			lock (Lock)
			{
				RemoveListeners();

				if (image == null || !image.HasAllocatedImage)
				{
					FBuffer = null;
					Initialised = false;
					return;
				}

				FImage = image;
				Allocate();
			}
		}

		#region events
		
		private void AddListeners()
		{
			FImage.ImageUpdate += ImageUpdate;
			FImage.ImageAttributesUpdate += ImageAttributesUpdate;
		}

		private void RemoveListeners()
		{
			if (Initialised)
			{
				FImage.ImageUpdate -= ImageUpdate;
				FImage.ImageAttributesUpdate -= ImageAttributesUpdate;
			}
		}
		#endregion

		public void Reinitialized()
		{
			ReinitialiseTexture = false;
		}

		void Allocate()
		{
			Width = FImage.Width;
			Height = FImage.Height;

			FBuffer = new CVImage();
			FBuffer.SetImage(FImage);

			CheckNeedsConversion();
			Initialised = true;
			ReinitialiseTexture = true;
		}

		void CheckNeedsConversion()
		{
			FNeedsConversion = CVImageUtils.NeedsConversion(FBuffer.NativeFormat, out FConvertedFormat);
			
			if (FNeedsConversion)
			{
				FBufferConverted = new CVImage();
				FBufferConverted.Initialise(FBuffer.Size, FConvertedFormat);
				FBuffer.GetImage(FConvertedFormat, FBufferConverted);
			}
		}

		unsafe void ImageUpdate(object sender, EventArgs e)
		{
			var image = sender as CVImage;

			if (image == null)
				return;

			try
			{
				lock (Lock)
				{
					lock (image.GetLock())
						if (image.Ptr != null)
						{
							_isFresh = true;
							FBuffer.SetImage(image);
						}

					if (FNeedsConversion)
						FBuffer.GetImage(FConvertedFormat, FBufferConverted);
				}
			}
			catch
			{

			}
		}

		void ImageAttributesUpdate(object sender, ImageAttributesChangedEventArgs e)
		{
			Allocate();
		}

		public void Close()
		{
			FBuffer = null;
			Initialised = false;
		}

		bool _isFresh;
		public bool isFresh
		{
			get
			{
				if (_isFresh && IsReady())
				{
					_isFresh = false;
					return true;
				}
				else
					return false;
			}
			set
			{
				_isFresh = isFresh;
			}
		}

		private CVImage GetBuffer()
		{
			if (FNeedsConversion)
				return FBufferConverted;
			else
				return FBuffer;
		}

		public bool IsReady()
		{
			return Initialised;
		}

		public IntPtr Ptr
		{
			get
			{
				IntPtr data;
				int step;
				Size size;


				CvInvoke.cvGetRawData(GetBuffer().Ptr, out data, out step, out size);

				return data;
			}
		}

		public uint BytesPerFrame
		{
			get
			{
				return (FNeedsConversion ? FBuffer.ImageAttributes.BytesPerFrame * 4 / 3 : FBuffer.ImageAttributes.BytesPerFrame);
			}
		}

		public CVImageAttributes Attributes
		{
			get
			{
				return FBuffer.ImageAttributes;
			}
		}
	}
}

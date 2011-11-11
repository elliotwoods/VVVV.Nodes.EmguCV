using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using System.Drawing;
using SlimDX.Direct3D9;
using SlimDX;
using VVVV.Utils.SlimDX;

namespace VVVV.Nodes.EmguCV
{
	class AsTextureImageInstance
	{
		//    [DllImport("msvcrt.dll", EntryPoint = "memcpy")]
		//    public unsafe static extern void CopyMemory(IntPtr pDest, IntPtr pSrc, int length);

		public int Width { get; private set; }
		public int Height { get; private set; }

		CVImageInput FImageInput = new CVImageInput();

		CVImage FBufferConverted;
		TColourFormat FConvertedFormat;
		bool FNeedsConversion = false;

		public Object Lock = new Object();

		public void Initialise(CVImageLink input)
		{
			//are we already initialised to this input?
			if (input == FImageInput.Link && Initialised)
				return;

			lock (Lock)
			{
				if (input == null)
				{
					FImageInput.Disconnect();
				}
				else
				{
					FImageInput.Connect(input);
				}
			}
		}

		public bool Initialised
		{
			get
			{
				//do we need other checks here as well?
				return FImageInput.Connected;
			}
		}

		public bool ImageAttributesChanged
		{
			get
			{
				return FImageInput.ImageAttributesChanged;
			}
		}

		public Texture CreateTexture(Device device)
		{
			if (Initialised && FImageInput.Allocated)
			{
				FNeedsConversion = CVImageUtils.NeedsConversion(FImageInput.ImageAttributes.ColourFormat, out FConvertedFormat);
				if (FNeedsConversion)
				{
					FBufferConverted = new CVImage();
					FBufferConverted.Initialise(FImageInput.ImageAttributes.Size, FConvertedFormat);
				}
				return CVImageUtils.CreateTexture(FBufferConverted.ImageAttributes, device);
			} 
			else
				return TextureUtils.CreateTexture(device, 1, 1);
		}

		public void UpdateTexture(Texture texture)
		{
			if (!Initialised)
				return;

			if (!FImageInput.ImageChanged)
				return;

			if (FImageInput.ImageAttributesChanged)
			{
				FImageInput.ImageAttributesChanged = true; //reset flag as it will now have dropped
				return;
			}

			Surface srf = texture.GetSurfaceLevel(0);
			DataRectangle rect = srf.LockRectangle(LockFlags.Discard);

			FImageInput.LockForReading();
			try
			{
				CVImage buffer;
				if (FNeedsConversion)
				{
					CVImageUtils.CopyImageConverted(FImageInput.Image, FBufferConverted);
					buffer = FBufferConverted;
				}
				else
				{
					buffer = FImageInput.Image;
				}
				rect.Data.WriteRange(buffer.Data, buffer.ImageAttributes.BytesPerFrame);
			}
			finally
			{
				FImageInput.UnlockForReading();
			}
				
			srf.UnlockRectangle();
		}
	}
}

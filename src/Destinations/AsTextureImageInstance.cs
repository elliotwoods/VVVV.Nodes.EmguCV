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
	class AsTextureImageInstance : IDestinationInstance
	{
		//    [DllImport("msvcrt.dll", EntryPoint = "memcpy")]
		//    public unsafe static extern void CopyMemory(IntPtr pDest, IntPtr pSrc, int length);

		public int Width { get; private set; }
		public int Height { get; private set; }

		CVImage FBufferConverted;
		TColourFormat FConvertedFormat;
		bool FNeedsConversion = false;

		private Dictionary<Texture, bool> FNeedsRefresh = new Dictionary<Texture,bool>();

		public override void Initialise()
		{
			
		}

		public override void Process()
		{
			foreach (var key in FNeedsRefresh.Keys.ToList())
			{
				FNeedsRefresh[key] = true;
			}
		}

		public bool Initialised
		{
			get
			{
				//do we need other checks here as well?
				if (FInput == null)
					return false;
				if (!FInput.Allocated)
					return false;
				return true;
			}
		}

		public Texture CreateTexture(Device device)
		{
			if (Initialised)
			{
				Texture output;
				FNeedsConversion = ImageUtils.NeedsConversion(FInput.ImageAttributes.ColourFormat, out FConvertedFormat);
				if (FNeedsConversion)
				{
					FBufferConverted = new CVImage();
					FBufferConverted.Initialise(FInput.ImageAttributes.Size, FConvertedFormat);
					output = ImageUtils.CreateTexture(FBufferConverted.ImageAttributes, device);
				} else
					output = ImageUtils.CreateTexture(FInput.ImageAttributes, device);

				FNeedsRefresh.Add(output, true);
				return output;
			} 
			else
				return TextureUtils.CreateTexture(device, 1, 1);
		}

		public void UpdateTexture(Texture texture)
		{
			if (!Initialised)
				return;

			if (!FInput.Allocated)
				return;

			if (FInput.ImageAttributesChanged)
			{
				FInput.ImageAttributesChanged = true; //reset flag as it will now have dropped
				return;
			}

			if (!FNeedsRefresh.ContainsKey(texture))
				return;

			if (!FNeedsRefresh[texture])
				return;
			FNeedsRefresh[texture] = false;

			Surface srf = texture.GetSurfaceLevel(0);
			DataRectangle rect = srf.LockRectangle(LockFlags.Discard);

			if (FNeedsConversion)
			{
				FInput.Image.GetImage(FBufferConverted);
				rect.Data.WriteRange(FBufferConverted.Data, FBufferConverted.ImageAttributes.BytesPerFrame);
			}
			else
			{
				FInput.LockForReading();
				try
				{
					rect.Data.WriteRange(FInput.Data, FInput.ImageAttributes.BytesPerFrame);
				}
				finally
				{
					FInput.ReleaseForReading();
				}
			}

			
				
			srf.UnlockRectangle();
		}

		public void Dispose()
		{

		}
	}
}

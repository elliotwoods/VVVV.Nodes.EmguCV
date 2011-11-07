using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace VVVV.Nodes.EmguCV
{
	public enum TColourFormat { UnInitialised, RGB8, RGB32F, RGBA8, L8, L16};

	public class CVImageAttributes
	{
		public TColourFormat ColourFormat;
		public Size FSize = new Size();

		public CVImageAttributes()
		{
			ColourFormat = TColourFormat.UnInitialised;
			FSize = new Size(0, 0);
		}

		public CVImageAttributes(TColourFormat c, int w, int h)
		{
			ColourFormat = c;
			FSize.Width = w;
			FSize.Height = h;
		}

		public bool CheckChanges(TColourFormat c, Size s)
		{
			bool changed = false;
			if (c != ColourFormat)
			{
				ColourFormat = c;
				changed = true;
			}

			if (s != FSize)
			{
				FSize = s;
				changed = true;
			}
			return changed;
		}

		public bool Initialised
		{
			get
			{
				return ColourFormat != TColourFormat.UnInitialised;
			}
		}
		public int Width
		{
			get
			{
				return FSize.Width;
			}
		}

		public int Height
		{
			get
			{
				return FSize.Height;
			}
		}

		public Size Size
		{
			get
			{
				return FSize;
			}
		}

		public uint BytesPerPixel
		{
			get
			{
				return CVImageUtils.BytesPerPixel(ColourFormat);
			}
		}

		public uint BytesPerFrame
		{
			get
			{
				return this.BytesPerPixel * (uint)this.Width * (uint)this.Height;
			}
		}
	}
}

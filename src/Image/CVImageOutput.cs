using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.EmguCV
{
	public class CVImageOutput : IDisposable
	{
		CVImageLink FLink = new CVImageLink();
		public CVImageLink Link { get { return FLink; } }

		public CVImage Image = new CVImage();

		public void Send()
		{
			Link.SetImage(Image);
		}

		public CVImageOutput()
		{
			// we shouldn't put an image here to start with
			// the action of assigning an image to here is important
		}

		public IntPtr Data
		{
			get
			{
				return Image.Data;
			}
		}

		public IntPtr CvMat
		{
			get
			{
				return Image.CvMat;
			}
		}

		public void Dispose()
		{
			
		}
	}
}

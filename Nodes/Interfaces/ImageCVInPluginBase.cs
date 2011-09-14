#region usings
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using SlimDX;
using SlimDX.Direct3D9;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.SlimDX;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

#endregion usings

//here you can change the vertex type
using VertexType = VVVV.Utils.SlimDX.TexturedVertex;
using System.Collections.Generic;
using Emgu.CV.CvEnum;

namespace VVVV.Nodes.EmguCV
{
	class InputImage
	{
		public CVImageAttributes ImageAttributes = new CVImageAttributes();

		public int Width
		{
			get
			{
				return ImageAttributes.Width;
			}
		}
		
		public int Height
		{
			get
			{
				return ImageAttributes.Height;
			}
		}
		
		Image<Rgba, byte> FImage;
		public bool Initialised = false;
		public Object Lock = new Object();

		public void Initialise(CVImageLink image)
		{
			lock (Lock)
			{
				if (image == null)
				{
					FImage = null;
					Initialised = false;
					return;
				} else {
					UpdateAttributes(image.ImageAttributes);
				}
			}
		}

		public void UpdateAttributes(CVImageAttributes attributes) 
		{
			ImageAttributes = attributes;

			if (Width > 0 && Height > 0)
			{
				//allocate local image
				FImage = new Image<Rgba, byte>(Width, Height);
				Initialised = true;
			}
			else
			{
				FImage = null;
				Initialised = false;
			}
		}

		public void UpdateImage(CVImageLink image, TColourData native)
		{
			if (Initialised)
			{
				try
				{
					image.GetImage(FImage);
				}
				catch
				{

				}
			}
		}

		public void Close()
		{
			FImage = null;
			Initialised = false;
		}


		public IntPtr Ptr
		{
			get
			{
				return FImage.MIplImage.imageData;
			}
		}
	}

	abstract class ImageCVInPluginBase : IPluginEvaluate
	{
		#region fields & pins
		[Input("Image")]
		ISpread<CVImageLink> FPinInImage;

		[Import]
		ILogger FLogger;

		//track the current texture slice
		int FCurrentSlice;

		Dictionary<int, InputImage> FImages = new Dictionary<int, InputImage>();
		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor()]
		public ImageCVInPluginBase(IPluginHost host)
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			CheckChanges(SpreadMax);
			EvaluateInternal(SpreadMax);
		}

		abstract protected void EvaluateInternal(int SpreadMax);

		private void CheckChanges(int count)
		{
			bool needsInit = false;
			CVImageLink imgIn;

			//check for change spread size
			if (FImages.Count != FPinInImage.SliceCount)
			{
				needsInit = true;

				//shrink local
				if (FImages.Count > FPinInImage.SliceCount)
				{
					for (int i = FPinInImage.SliceCount; i < FImages.Count; i++)
						FImages.Remove(i);
				}
				//grow local
				else
				{
					for (int i = FImages.Count; i < FPinInImage.SliceCount; i++)
					{
						InputImage attribs = new InputImage();

						//presume RGB input
						attribs.Initialise(FPinInImage[i]);
						FImages.Add(i, attribs);
					}
				}

			}

			//seems a shame to have to reinitialise absolutely everything..
			if (needsInit)
				Reinit();
		}

		abstract protected void Reinit();
	}
}

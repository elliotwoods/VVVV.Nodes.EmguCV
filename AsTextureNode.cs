#region usings

using System;
using System.ComponentModel.Composition;
using SlimDX;
using SlimDX.Direct3D9;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.SlimDX;

using Emgu.CV;
using Emgu.CV.Structure;

#endregion usings

//here you can change the vertex type
using System.Collections.Generic;
using Emgu.CV.CvEnum;

namespace VVVV.Nodes.EmguCV
{
	class ImageInstance
	{
		public int Width { get; private set; } 
		public int Height { get; private set; }
		
		Image<Rgba, byte> FBufferImage;
		
		public bool Initialised { get; private set; }
		public bool ReinitialiseTexture { get; private set; }

		bool FIsFresh;
		public bool IsFresh
		{
			get
			{
				if (FIsFresh)
				{
					FIsFresh = false;
					return true;
				}
				return false;
			}
			/*set
			{
				FIsFresh = IsFresh;
			}*/
		}

		public IntPtr Ptr
		{
			get
			{
				return FBufferImage.MIplImage.imageData;
			}
		}
		
		public Object Lock = new Object();

		public void InitialiseImage(ImageRGB image)
		{
			lock (Lock)
			{
				if (image == null || !image.Initialised)
				{
					FBufferImage = null;
					Initialised = false;
					return;
				}

				image.ImageUpdate += ImageUpdate;
				image.ImageAttributesUpdate += ImageAttributesUpdate;

				Allocate(image.ImageAttributes);
			}
		}

		public void Reinitialized()
		{
			ReinitialiseTexture = false;
		}

		void Allocate(CVImageAttributes imageAttributes)
		{
			Width = imageAttributes.Width;
			Height = imageAttributes.Height;

			FBufferImage = new Image<Rgba, byte>(Width, Height);
			Initialised = true;
		}

		void ImageAttributesUpdate(object sender, ImageAttributesChangedEventArgs e)
		{
			Allocate(e.Attributes);
			ReinitialiseTexture = true;
		}

		void ImageUpdate(object sender, EventArgs e)
		{
			var imageRGB = sender as ImageRGB;
			
			if (imageRGB == null || !imageRGB.Initialised) return;
			
			try
			{
				lock (Lock)
					lock (imageRGB.GetLock())
						if (imageRGB.Ptr != null)
						{
							FIsFresh = true;
							CvInvoke.cvCvtColor(imageRGB.Ptr, FBufferImage.Ptr, COLOR_CONVERSION.CV_RGB2RGBA);
						}
			}
			catch
			{

			}
		}

		public void Close()
		{
			FBufferImage = null;
			Initialised = false;
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "AsTexture",
			  Category = "EmguCV",
			  Version = "RGB",
			  Help = "Converts IPLImage to Texture",
			  Tags = "")]
	#endregion PluginInfo
	public class AsTextureNode : DXTextureOutPluginBase, IPluginEvaluate
	{
		#region fields & pins
		[Input("Image")]
		IDiffSpread<ImageRGB> FPinInImage;

		[Import]
		ILogger FLogger;

		readonly Dictionary<int, ImageInstance> FImageInstances = new Dictionary<int, ImageInstance>();
		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor]
		public AsTextureNode(IPluginHost host)
			: base(host)
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int spreadMax)
		{
			for (int i = 0; i < FImageInstances.Count; i++)
			{
				if (!FImageInstances[i].ReinitialiseTexture) continue;
				
				Reinitialize();
				FImageInstances[i].Reinitialized();
			}

			CheckChanges();
			Update();
		}

		private void CheckChanges()
		{
			bool needsInit = false;

			if (FPinInImage[0] == null)
			{
				FImageInstances.Clear();
				needsInit = true;
			}
			else if (FImageInstances.Count != FPinInImage.SliceCount)
			{
				//shrink local
				if (FImageInstances.Count > FPinInImage.SliceCount)
				{
					for (int i = FPinInImage.SliceCount; i < FImageInstances.Count; i++)
						FImageInstances.Remove(i);
					needsInit = true;
				}
				//grow local
				else
				{
					for (int i = FImageInstances.Count; i < FPinInImage.SliceCount; i++)
					{
						if (FPinInImage[i] == null || !FPinInImage[0].Initialised)
							break;

						ImageInstance attribs = new ImageInstance();

						//presume BGR input
						attribs.InitialiseImage(FPinInImage[i]);
						FImageInstances.Add(i, attribs);
					}
					needsInit = true;
				}

			}

			SetSliceCount(FImageInstances.Count);

			//seems a shame to have to reinitialise absolutely everything..
			if (needsInit)
				Reinitialize();
		}

		//this method gets called, when Reinitialize() was called in evaluate,
		//or a graphics device asks for its data
		protected override Texture CreateTexture(int slice, Device device)
		{
			FLogger.Log(LogType.Debug, "Creating new texture at slice: " + slice);

			if (FImageInstances.Count > 0)
			{
				if (FImageInstances[slice].Initialised)
					return TextureUtils.CreateTexture(device, Math.Max(FImageInstances[slice].Width, 1), Math.Max(FImageInstances[slice].Height, 1));
				
				return TextureUtils.CreateTexture(device, 1, 1);
			}
			return TextureUtils.CreateTexture(device, 1, 1);
		}

		//this method gets called, when Update() was called in evaluate,
		//or a graphics device asks for its texture, here you fill the texture with the actual data
		//this is called for each renderer, careful here with multiscreen setups, in that case
		//calculate the pixels in evaluate and just copy the data to the device texture here
		protected unsafe override void UpdateTexture(int slice, Texture texture)
		{
			if (FImageInstances.Count < slice)
				return;

			if (FImageInstances[slice].Initialised)
			{
				Surface srf = texture.GetSurfaceLevel(0);
				DataRectangle rect = srf.LockRectangle(LockFlags.Discard);

				lock (FImageInstances[slice].Lock)
					rect.Data.WriteRange(FImageInstances[slice].Ptr, FImageInstances[slice].Width * FImageInstances[slice].Height * 4);

				srf.UnlockRectangle();
			}
		}
	}
}

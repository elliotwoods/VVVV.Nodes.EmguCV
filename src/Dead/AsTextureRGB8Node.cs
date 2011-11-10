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
	class ImageRGB8Instance
	{
		public int Width { get; private set; } 
		public int Height { get; private set; }

		ImageRGB FImage;
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
		}

		public IntPtr Ptr
		{
			get
			{
				return FBufferImage.MIplImage.imageData;
			}
		}
		
		public Object Lock = new Object();

		public ImageRGB Image
		{
			get
			{
				return FImage;
			}

			set
			{
				Initialise(value);
			}
		}

		private void Initialise(ImageRGB image)
		{
			lock (Lock)
			{
				RemoveListeners();

				if (image == null || !image.HasAllocatedImage)
				{
					FBufferImage = null;
					Initialised = false;
					return;
				}

				FImage = image;
				Allocate();

				if (Initialised)
				{
					AddListeners();
					Load(image);
				}
			}
		}

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

		public void Reinitialized()
		{
			ReinitialiseTexture = false;
		}

		private void Allocate()
		{
			Width = FImage.Width;
			Height = FImage.Height;

			FBufferImage = new Image<Rgba, byte>(Width, Height);
			Initialised = true;
		}

		private void ImageAttributesUpdate(object sender, ImageAttributesChangedEventArgs e)
		{
			Allocate();
			ReinitialiseTexture = true;
		}

		private void ImageUpdate(object sender, EventArgs e)
		{
			var imageRGB = sender as ImageRGB;
			
			if (imageRGB == null || !imageRGB.HasAllocatedImage) return;
			
			try
			{
				lock (Lock)
					Load(imageRGB);
			}
			catch
			{

			}
		}

		public void Load(ImageRGB image)
		{
			lock (image.GetLock())
				if (image.Ptr != null)
				{
					FIsFresh = true;
					CvInvoke.cvCvtColor(image.Ptr, FBufferImage.Ptr, COLOR_CONVERSION.CV_RGB2RGBA);
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
	public class AsTextureRGB8Node : DXTextureOutPluginBase, IPluginEvaluate
	{
		#region fields & pins
		[Input("Image")]
		IDiffSpread<ImageRGB> FPinInImage;

		[Import]
		ILogger FLogger;

		private Spread<ImageRGB8Instance> FImageInstances = new Spread<ImageRGB8Instance>(0);
		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor]
		public AsTextureRGB8Node(IPluginHost host)
			: base(host)
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int spreadMax)
		{
			for (int i = 0; i < FImageInstances.SliceCount; i++)
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
			bool reinitialiseTexture = false;

			//if nothing is connected, our first slice is null
			if (FPinInImage[0] == null)
			{
				FImageInstances.SliceCount = 0;
				reinitialiseTexture = true;
			}
			else
			{
				reinitialiseTexture |= LoadInputs();
			}

			SetSliceCount(FImageInstances.SliceCount);

			//seems a shame to have to reinitialise absolutely everything..
			if (reinitialiseTexture)
				Reinitialize();
		}

		private bool LoadInputs()
		{
			bool hasChanges = false;

			if (FImageInstances.SliceCount != FPinInImage.SliceCount)
			{
				FImageInstances.SliceCount = FPinInImage.SliceCount;
				hasChanges = true;
			}

			for (int i = 0; i < FImageInstances.SliceCount; i++)
			{
				if (FImageInstances[i] == null)
				{
					FImageInstances[i] = new ImageRGB8Instance();
				}

				//check whether image has changed
				//if so reinitialise
				if (FImageInstances[i].Image != FPinInImage[i])
				{
					FImageInstances[i].Image = FPinInImage[i];
					hasChanges = true;
				}
			}

			return hasChanges;
	}

		//this method gets called, when Reinitialize() was called in evaluate,
		//or a graphics device asks for its data
		protected override Texture CreateTexture(int slice, Device device)
		{
			FLogger.Log(LogType.Debug, "Creating new texture at slice: " + slice);

			if (FImageInstances.SliceCount > 0)
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
			if (FImageInstances.SliceCount == 0)
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

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
using System.Drawing;

namespace VVVV.Nodes.EmguCV
{
	class AsTextureImageInstance
	{
	//    [DllImport("msvcrt.dll", EntryPoint = "memcpy")]
	//    public unsafe static extern void CopyMemory(IntPtr pDest, IntPtr pSrc, int length);

		public int Width, Height;
		CVImage FBuffer;
		IImage FConvertedImage;
		bool FConverted = false;
		bool FAllocated = false;

		public bool Initialised = false;
		public bool NeedsCreateTexture = false;
		public Object Lock = new Object();

		public void InitialiseImage(CVImage imgIn)
		{
			lock (Lock)
			{
				if (imgIn == null || !imgIn.HasAllocatedImage)
				{
					FBuffer = null;
					Initialised = false;
					return;
				}
				imgIn.ImageUpdate += new EventHandler(imgIn_ImageUpdate);
				imgIn.ImageAttributesUpdate += new EventHandler<ImageAttributesChangedEventArgs>(imgIn_ImageAttributesUpdate);

				Allocate(imgIn.ImageAttributes);
			}
		}

		void imgIn_ImageAttributesUpdate(object sender, ImageAttributesChangedEventArgs e)
		{
			Allocate(e.Attributes);
		}

		void Allocate(CVImageAttributes imageAttributes)
		{
			Width = imageAttributes.Width;
			Height = imageAttributes.Height;

			FBuffer = new CVImage();
			FBuffer.Initialise(imageAttributes);
			FBuffer.Allocate();

			Initialised = true;
			NeedsCreateTexture = true;
		}

		unsafe void imgIn_ImageUpdate(object sender, EventArgs e)
		{
			var image = sender as CVImage;
			if (image != null)
			{
				try
				{
					lock (Lock)
					{
						lock (image.GetLock())
							if (image.Ptr != null)
							{
								_isFresh = true;
								FBuffer.SetImage(image);
								FAllocated = true;
							}

						TColourFormat newFormat;
						if (CVImageUtils.NeedsConversion(FBuffer.NativeFormat, out newFormat))
						{
							//this is slow as creates new thing every frame
							FConvertedImage = FBuffer.GetImage(newFormat);
							FConverted = true;
							FAllocated = true;
						}
						else
							FConverted = false;
					}
				}
				catch
				{

				}
			}
		}

		public void Close()
		{
			FBuffer = null;
			Initialised = false;
			FAllocated = false;
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

		private IImage GetBuffer()
		{
			if (FConverted)
				return FConvertedImage;
			else
				return FBuffer.GetImage();
		}

		public bool IsReady()
		{
			return FAllocated && Initialised;
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
				return (FConverted ? FBuffer.ImageAttributes.BytesPerFrame * 4 / 3 : FBuffer.ImageAttributes.BytesPerFrame);
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

	#region PluginInfo
	[PluginInfo(Name = "AsTexture",
				Category = "EmguCV",
				Version = "",
				Help = "Converts IPLImage to Texture",
				Tags = "")]
	#endregion PluginInfo
	public class AsTextureNode : DXTextureOutPluginBase, IPluginEvaluate
	{
		#region fields & pins
		[Input("Image")]
		ISpread<CVImage> FPinInImage;

		[Import]
		ILogger FLogger;

		Dictionary<int, AsTextureImageInstance> FImageInstances = new Dictionary<int, AsTextureImageInstance>();
		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor()]
		public AsTextureNode(IPluginHost host)
			: base(host)
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			CheckChanges(SpreadMax);
			Update();
		}

		private void CheckChanges(int count)
		{
			bool needsInit = false;
			CVImage imgIn;

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
						//if (FPinInImage[i] == null || !FPinInImage[i].HasAllocatedImage)
						//    break;

						AsTextureImageInstance instance = new AsTextureImageInstance();

						instance.InitialiseImage(FPinInImage[i]);
						FImageInstances.Add(i, instance);
					}
					needsInit = true;
				}

			}


			SetSliceCount(FImageInstances.Count);

			//seems a shame to have to reinitialise absolutely everything..
			if (needsInit)
				Reinitialize();

			//check for data changes
			//bool needsUpdate = false;
			//for (int i = 0; i < FImageInstances.Count; i++)
			//{
			//    if (FImageInstances[i].isFresh)
			//        needsUpdate = true;
			//}
			//if (needsUpdate)
			//Update();
		}

		//this method gets called, when Reinitialize() was called in evaluate,
		//or a graphics device asks for its data
		protected override Texture CreateTexture(int Slice, Device device)
		{
			FLogger.Log(LogType.Debug, "Creating new texture at slice: " + Slice);

			if (FImageInstances.Count > 0)
			{

				if (FImageInstances[Slice].Initialised && FImageInstances[Slice].Attributes.Initialised)
					return CVImageUtils.CreateTexture(FImageInstances[Slice].Attributes, device);
				else
					return TextureUtils.CreateTexture(device, 1, 1);
			}
			else
			{
				return TextureUtils.CreateTexture(device, 1, 1);
			}
		}

		//this method gets called, when Update() was called in evaluate,
		//or a graphics device asks for its texture, here you fill the texture with the actual data
		//this is called for each renderer, careful here with multiscreen setups, in that case
		//calculate the pixels in evaluate and just copy the data to the device texture here
		protected unsafe override void UpdateTexture(int Slice, Texture texture)
		{
			if (FImageInstances.Count < Slice)
				return;

			if (FImageInstances[Slice].IsReady())
			{
				Surface srf = texture.GetSurfaceLevel(0);
				DataRectangle rect = srf.LockRectangle(LockFlags.Discard);

				lock (FImageInstances[Slice].Lock)
					rect.Data.WriteRange(FImageInstances[Slice].Ptr, FImageInstances[Slice].BytesPerFrame);

				srf.UnlockRectangle();
			}
		}
	}
}

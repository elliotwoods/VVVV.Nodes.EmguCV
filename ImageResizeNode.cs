using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using VVVV.Nodes.EmguCV.Abstract;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EmguCV
{
	public class ResizeInstance:ImageProcessingInstance
	{
		public ImageRGB Output = new ImageRGB();

		private Size FSize;

		public Size Size
		{
			get { return FSize; }
			set
			{
				FSize = value;
				Allocate(new CVImageAttributes(TColourData.RGB8, FSize.Width, FSize.Height));
			}
		}

		protected override void Process()
		{
			if (Image == null || !Image.Initialised || BufferImage == null) return;
			try
			{
				lock (Lock)
				{
					lock (Image.GetLock())
					{
						CvInvoke.cvResize(Image.Ptr, BufferImage.Ptr, INTER.CV_INTER_LINEAR);
						Output.SetImage(BufferImage);
					}
				}
				
			}
			catch
			{
				Status = "Can't lock image";
			}
			
		}
	}
	
	#region PluginInfo
	[PluginInfo(Name = "ImageResize", Category = "EmguCV", Help = "Resize Image", Author = "alg", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class ImageResizeNode: ThreadedNode<ResizeInstance>
	{
		[Input("Image")] 
		private IDiffSpread<ImageRGB> FInput;

		[Input("Width")] private ISpread<int> FWidth;

		[Input("Height")] private ISpread<int> FHeight;

		[Input("Init", IsSingle = true, IsBang = true)] 
		private ISpread<bool> FInit;

		[Output("Image")] private ISpread<ImageRGB> FImageOutput;

		public override void Evaluate(int spreadMax)
		{
			base.Evaluate(spreadMax);

			for (int i = 0; i < spreadMax; i++)
			{
				if(FInput[i] == null)
				{
					InstancesByIndex[i].Close();
				}

				if(FInit[0])
				{
					InstancesByIndex[i].SetImage(FInput[i]);
					InstancesByIndex[i].Initialise();
					InstancesByIndex[i].Size = new Size(FWidth[i], FHeight[i]);
				}
			}

			Output();
		}

		private void Output()
		{
			foreach(KeyValuePair<int, ResizeInstance> pair in InstancesByIndex)
			{
				FImageOutput[pair.Key] = InstancesByIndex[pair.Key].Output;
			}
		}
	}
}

#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.EmguCV
{
	#region PluginInfo
	[PluginInfo(Name = "Pipet", Category = "EmguCV", Version = "RGB32F", Help = "Pipet in image", Tags = "")]
	#endregion PluginInfo
	public class PipetRGB32F : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Image", IsSingle = true)]
		ISpread<ImageRGB32F> FPinInImages;

		[Input("Input")]
		IDiffSpread<Vector2D> FPinInInput;

		[Output("Output")]
		ISpread<Vector3D> FPinOutput;

		[Output("Status")]
		ISpread<string> FStatus;

		[Import]
		ILogger FLogger;

		ImageRGB32F FImage;
		List<Vector2D> FInput = new List<Vector2D>();
		List<Vector3D> FOutput = new List<Vector3D>();
		Object FLock = new Object();
		#endregion fields & pins

		[ImportingConstructor]
		public PipetRGB32F(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInImages[0] != FImage)
			{
				FImage = FPinInImages[0];
				FImage.ImageUpdate += new EventHandler(FImage_ImageUpdate);
			}

			lock (FLock)
			{
				while (FInput.Count < SpreadMax)
					FInput.Add(new Vector2D());
				while (FInput.Count > SpreadMax)
					FInput.RemoveAt(0);
				while (FOutput.Count < FInput.Count)
					FOutput.Add(new Vector3D());
				while (FOutput.Count > FInput.Count)
					FOutput.RemoveAt(0);

				for (int i = 0; i < FPinInInput.SliceCount; i++)
				{
					FInput[i] = FPinInInput[i];
				}
				Output();
			}

		}

		unsafe void FImage_ImageUpdate(object sender, EventArgs e)
		{
			var image = sender as ImageRGB32F;
			if (image == null)
				return;

			lock (FLock)
			{
				int Slices = FInput.Count;

				for (int i = 0; i < Slices; i++)
				{
					uint x, y;
					x = (uint)FInput[i].x;
					y = (uint)FInput[i].y;

					if (x >= image.Width || y >= image.Height)
						FOutput[i] = new Vector3D(0, 0, 0);
					else
					{
						float* xyzs = (float*)image.Image.MIplImage.imageData.ToPointer();
						float* xyz = xyzs + (3 * (x + y * image.Width));

						FOutput[i] = new Vector3D((double)xyz[0], (double)xyz[1], (double)xyz[2]);
					}
				}
			}
		}

		void Output()
		{
			FPinOutput.SliceCount = FOutput.Count;

			for (int i = 0; i < FOutput.Count; i++)
			{
				FPinOutput[i] = FOutput[i];
			}

		}
	}
}

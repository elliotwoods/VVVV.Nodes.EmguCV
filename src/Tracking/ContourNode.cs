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
	struct ContourData
	{
		public Rectangle Bounds;
		public double Area;
	}

	public class ContourInstance : IDestinationInstance
	{
		public bool Enabled = true;

		CVImage FGrayscale = new CVImage();

		Object FLockResults = new Object();
		Spread<Vector4D> FBoundingBox = new Spread<Vector4D>(0);
		Spread<double> FArea = new Spread<double>(0);

		public ISpread<Vector4D> BoundingBox
		{
			get
			{
				lock (FLockResults)
					return FBoundingBox.Clone<Vector4D>();
			}
		}

		public ISpread<double> Area
		{
			get
			{
				lock (FLockResults)
					return FArea.Clone<double>();
			}
		}

		public override void Initialise()
		{
			FGrayscale.Initialise(FInput.ImageAttributes.Size, TColourFormat.L8);
		}

		override public void Process()
		{
			if (!Enabled)
				return;

			FInput.Image.GetImage(TColourFormat.L8, FGrayscale);
			Image<Gray, byte> img = FGrayscale.GetImage() as Image<Gray, byte>;

			if (img != null)
			{
				//Seriously EmguCV? what the fuck is up with your syntax?
				//both ways of skinning this cat involve fucking a moose

				Contour<Point> contour = img.FindContours();

				List<ContourData> results = new List<ContourData>();

				ContourData c;
				for (; contour != null; contour = contour.HNext)
				{
					c = new ContourData();
					c.Area = contour.Area;
					c.Bounds = contour.BoundingRectangle;

					results.Add(c);
				}

				lock (FLockResults)
				{
					FBoundingBox.SliceCount = results.Count;
					FArea.SliceCount = results.Count;

					for (int i = 0; i < results.Count; i++)
					{
						c = results[i];

						FBoundingBox[i] = new Vector4D(((double)c.Bounds.X / (double)img.Width) * 2.0d - 1.0d,
							 1.0d - ((double)c.Bounds.Y / (double)img.Height) * 2.0d,
							 (double)c.Bounds.Width * 2.0d / (double)img.Width,
							 (double)c.Bounds.Height * 2.0d / (double)img.Height);

						FArea[i] = (double)c.Area*  (4.0d / (double)(img.Width * img.Height));
					}
				}

			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Contour", Category = "EmguCV", Help = "Finds contours in binary image", Tags = "")]
	#endregion PluginInfo
	public class ContourNode : IDestinationNode<ContourInstance>
	{
		#region fields & pins
		[Input("Enabled", DefaultValue = 1)]
		IDiffSpread<bool> FPinInEnabled;

		[Output("Bounding box")]
		ISpread<ISpread<Vector4D>> FPinOutBounds;

		[Output("Area")]
		ISpread<ISpread<double>> FPinOutArea;
		#endregion fields & pins

		protected override void Update(int InstanceCount)
		{
			CheckParams(InstanceCount);
			Output(InstanceCount);
		}

		void CheckParams(int InstanceCount)
		{
			if (FPinInEnabled.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Enabled = FPinInEnabled[0];
				}
		}

		void Output(int InstanceCount)
		{
			FPinOutArea.SliceCount = InstanceCount;
			FPinOutBounds.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
				FPinOutArea[i] = FProcessor[i].Area;
				FPinOutBounds[i] = FProcessor[i].BoundingBox;
			}
		}
	}
}

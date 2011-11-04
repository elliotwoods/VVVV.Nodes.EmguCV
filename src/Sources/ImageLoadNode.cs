using System.ComponentModel.Composition;
using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;

namespace VVVV.Nodes.EmguCV
{

	#region PluginInfo
	[PluginInfo(Name = "ImageLoad", Category = "EmguCV", Help = "Loads RGB texture", Author = "alg", Tags = "")]
	#endregion PluginInfo
	public class ImageLoadNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Filename", StringType = StringType.Filename, DefaultString = null)] 
		IDiffSpread<string> FPinInFilename;

		[Output("Image")] 
		ISpread<ImageRGB> FPinOutImage;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

		private Spread<string> FFilename;
		private Spread<ImageRGB> FImages;

		[Import]
		ILogger FLogger;
		#endregion fields&pins

		[ImportingConstructor]
		public ImageLoadNode()
		{
			FFilename = new Spread<string>(1);
			FImages = new Spread<ImageRGB>(1);
		}

		public void Evaluate(int spreadMax)
		{
			if (!FPinInFilename.IsChanged || FPinInFilename.SliceCount < 1) return;

			FPinOutImage.SliceCount = spreadMax;
			FPinOutStatus.SliceCount = spreadMax;
			FImages.SliceCount = spreadMax;

			for (int i = 0; i < spreadMax; i++)
			{
				if(FPinInFilename[i] == FFilename[i]) continue;

				FPinOutImage[i] = new ImageRGB();
				
				try
				{
					FImages[i] = new ImageRGB();
					FPinOutImage[i] = FImages[i];
					Image<Bgr, byte> image = new Image<Bgr, byte>(FPinInFilename[i]);

					FImages[i].SetImage(image);

					FPinOutStatus[i] = "OK";
				}
				catch
				{
					FPinOutStatus[i] = "Failed";
				}
			}

			FFilename = (Spread<string>) FPinInFilename.Clone();
		}
	}
}

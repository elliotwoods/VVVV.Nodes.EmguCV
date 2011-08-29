using System;
using System.ComponentModel.Composition;
using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;

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

		private Spread<string> FPrevFilename;
		
		[Import]
		ILogger FLogger;
		#endregion fields&pins

		[ImportingConstructor]
		public ImageLoadNode()
		{
			FPrevFilename = new Spread<string>(1);
		}

		public void Evaluate(int SpreadMax)
		{
			if (!FPinInFilename.IsChanged || FPinInFilename.SliceCount < 1) return;

			FPinOutImage.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
			{
				if(FPinInFilename[i] == FPrevFilename[i]) continue;

				FPinOutImage[i] = new ImageRGB();
				
				try
				{
					FPinOutImage[i].Img = new Image<Bgr, byte>(FPinInFilename[i]);
				}
				catch
				{
					FLogger.Log(LogType.Error, "ImageLoad: Cant't load image file");
				}
			}

			FPrevFilename = (Spread<string>) FPinInFilename.Clone();
		}
    }
}

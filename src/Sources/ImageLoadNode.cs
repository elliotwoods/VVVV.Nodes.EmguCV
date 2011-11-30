using System.ComponentModel.Composition;
using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;

namespace VVVV.Nodes.EmguCV
{
	public class ImageLoadInstance : IGeneratorInstance
	{
		string FLoadedImage = "";
		
		public override bool NeedsThread()
		{
			return false;
		}

		public string Filename
		{
			set
			{
				if (FLoadedImage != value)
				{
					LoadImage(value);
				}
			}
		}

		public void Reload()
		{
			LoadImage(FLoadedImage);
		}

		private void LoadImage(string filename)
		{
			try
			{
				FOutput.Image.LoadFile(filename);
				FLoadedImage = filename;
				FOutput.Send();
				Status = "OK";
			}
			catch
			{
				Status = "Image load failed";
				FLoadedImage = "";
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "ImageLoad", Category = "EmguCV", Help = "Loads RGB texture", Author = "alg", Tags = "")]
	#endregion PluginInfo
	public class ImageLoadNode : IGeneratorNode<ImageLoadInstance>
	{
		#region fields & pins
		[Input("Filename", StringType = StringType.Filename, DefaultString = null)] 
		IDiffSpread<string> FPinInFilename;

		[Input("Reload", IsBang = true)]
		ISpread<bool> FPinInReload;

		[Import]
		ILogger FLogger;
		#endregion fields&pins

		[ImportingConstructor]
		public ImageLoadNode()
		{

		}

		protected override void Update(int InstanceCount)
		{
			if (FPinInFilename.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Filename = FPinInFilename[i];

			for (int i = 0; i < InstanceCount; i++)
			{
				if (FPinInReload[i])
					FProcessor[i].Reload();
			}
		}
	}
}

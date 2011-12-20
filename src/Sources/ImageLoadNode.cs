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
		string FFilename = "";
		
		public override bool NeedsThread()
		{
			return false;
		}

		public string Filename
		{
			set
			{
				if (FFilename != value)
				{
					FFilename = value;
					LoadImage();
				}
			}
		}

		public void Reload()
		{
			LoadImage();
		}

		private void LoadImage()
		{
			try
			{
				FOutput.Image.LoadFile(FFilename);
				FOutput.Send();
				Status = "OK";
			}
			catch
			{
				Status = "Image load failed";
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

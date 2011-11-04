#region usings
using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using Emgu.CV.UI;
#endregion usings

namespace VVVV.Nodes.EmguCV
{
	#region PluginInfo
	[PluginInfo(Name = "Renderer",
	Category = "EmguCV",
	Help = "Render an EmguCV Image",
	Tags = "",
	AutoEvaluate = true)]
	#endregion PluginInfo
	public class GUITemplateNode : UserControl, IPluginEvaluate
	{
		#region fields & pins

		[Input("Input")]
		ISpread<ImageRGB> FInput;


		[Import()]
		ILogger FLogger;

		//gui controls
		ImageBox FImageBox;

		#endregion fields & pins

		#region constructor and init

		public GUITemplateNode()
		{
			//setup the gui
			InitializeComponent();
		}

		void InitializeComponent()
		{
			//clear controls in case init is called multiple times
			Controls.Clear();

			//add imagebox
			FImageBox = new ImageBox();

			//add to controls
			Controls.Add(FImageBox);
		}



		#endregion constructor and init

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{


		}
	}
}
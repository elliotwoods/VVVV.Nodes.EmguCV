#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Beep", Category = "System", Help = "Perform system beep", Tags = "")]
	#endregion PluginInfo
	public class SystemBeepNode : IPlugin
	{
		#region fields & pins
		[Input("Input", IsBang=true, IsSingle=true)]
		ISpread<bool> FInput;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FInput[0])
				System.Media.SystemSounds.Beep.Play();
		}
		
		public bool AutoEvaluate
        {
            get { return true; }
        }
 
        public void Configurate(IPluginConfig input)
        {
           
        }
		public void SetPluginHost(IPluginHost Host)
        {
           
        }

 
	}
}

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
	[PluginInfo(Name = "Length", Category = "3D", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class C3DLengthNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		ISpread<Vector3D> FInput;

		[Output("Output")]
		ISpread<double> FOutput;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
				FOutput[i] = FInput[i].Length;

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}

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

using OpenNI;

#endregion usings

namespace VVVV.Nodes.EmguCV
{

	class OpenNIContext {
		public Context context;
		public bool running = false;
	}

	#region PluginInfo
	[PluginInfo(Name = "Context", Category = "OpenNI", Help = "OpenNI context loader", Tags = "")]
	#endregion PluginInfo
	public class OpenNIContextNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Filename", StringType=StringType.Filename)]
		ISpread<string> FPinInFilename;

		[Input("Open", IsBang = true, IsSingle = true)]
		ISpread<bool> FPinInOpen;

		[Output("Context")]
		ISpread<OpenNIContext> FPinOutContext;

		[Output("Status")]
		ISpread<String> FPinOutStatus;

		[Import]
		ILogger FLogger;

		string FConfig;
		Context FContext;
		OpenNIContext FOutput = new OpenNIContext();
		
		#endregion fields & pins

		[ImportingConstructor]
		public OpenNIContextNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInOpen[0] && FPinInFilename[0] != "")
			{
				FConfig = FPinInFilename[0];

				try
				{
					FContext = new Context();
					FContext.RunXmlScriptFromFileEx(FPinInFilename[0]);

					FOutput.context = FContext;
					FOutput.running = true;
					FPinOutStatus[0] = "OK";
				}
				catch (StatusException e)
				{
					FLogger.Log(LogType.Error, e.Message);
					FOutput.running = false;
					FPinOutStatus[0] = e.Message;
				}
			}

			FPinOutContext[0] = FOutput;

		}

	}
}

#region usings
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using SlimDX;
using SlimDX.Direct3D9;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.SlimDX;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Threading;

#endregion usings

namespace VVVV.Nodes.EmguCV
{
    #region PluginInfo
    [PluginInfo(Name = "VideoIn",
                Category = "EmguCV",
                Version = "",
                Help = "Captures from DShow device to IPLImage",
                Tags = "")]
    #endregion PluginInfo
    public class CaptureVideoNode : IPluginEvaluate
    {
        #region fields & pins

        [Input("Camera ID", DefaultValue = 0)]
        IDiffSpread<int> FPinInCameraID;

        [Output("Image")]
        ISpread<Image<Bgr, byte>> FPinOutImage;

        [Output("Status")]
        ISpread<string> FPinOutStatus;

        [Import]
        ILogger FLogger;

        IPluginHost FHost;

        //track the current texture slice
        int FCurrentSlice;

        Thread      FCaptureThread;
        bool        FRunCaptureThread = false;

        Image<Bgr, byte>        FImage;
        Capture                 FCapture;
        bool                    FHasCapture;

        #endregion fields & pins

        // import host and hand it to base constructor
        [ImportingConstructor()]
		public CaptureVideoNode(IPluginHost host)
        {
            FHost = host;
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (FPinInCameraID.IsChanged)
            {
                InitialiseCamera(FPinInCameraID[0]);
            }

            if (FRunCaptureThread)
                FPinOutImage[0] = FImage;
        }

		private void InitialiseCamera(int id)
        {
            CloseCamera();
            try
            {
                FCapture = new Capture(id); //create a camera captue
            }
            catch
            {
				FPinOutStatus[0] = "Camera open failed";
                return;
            }

			FPinOutStatus[0] = "Camera open success";
            FHasCapture = true;

            FRunCaptureThread = true;
            FCaptureThread = new Thread(fnCapture);
            FCaptureThread.Start();
        }
        private void CloseCamera()
        {
            if (FHasCapture)
                FCapture.Dispose();
            FHasCapture = false;
        }

        private void fnCapture()
        {
            while (FRunCaptureThread)
            {
                FImage = FCapture.QueryFrame();
                FPinOutImage[0] = FImage;
            }
        }
    }
}

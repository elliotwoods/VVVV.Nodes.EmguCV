#region using
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System;
using VVVV.Utils.VColor;
using System.Diagnostics;

#endregion

namespace VVVV.Nodes.EmguCV
{
	public class ChangeInstance : IDestinationInstance
	{
		int FFrames = 0;
		public int Frames
		{
			get
			{
				int f = FFrames;
				FFrames = 0;
				return f;
			}
		}

		public override void Process()
		{
			FFrames++;
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Framerate", Category = "EmguCV", Version = "", Help = "Report the framerate that an image is being updated at", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class ChangeNode : IDestinationNode<ChangeInstance>
	{
		[Output("Output", DimensionNames=new string[]{"frames"})]
		ISpread<int> FOutput;

		protected override void Update(int InstanceCount)
		{
			FOutput.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
				FOutput[i] = FProcessor[i].Frames;
			}
		}
	}
}

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

#endregion usings

namespace VVVV.Nodes.EmguCV
{
	#region PluginInfo
	[PluginInfo(Name = "PointCloud", Category = "EmguCV", Help = "Make your OpenNI point cloud", Tags = "")]
	#endregion PluginInfo
	public class EX9_GeometryPointCloudNode : DXMeshOutPluginBase, IPluginEvaluate
	{
		#region fields & pins

		[Input("XYZ Image")]
		IDiffSpread<ImageRGB32F> FPinInImage;

		[Input("Filename", StringType=StringType.Filename)]
		ISpread<string> FPinInFilename;

		[Import()]
		ILogger FLogger;

		ImageRGB32F FImage;
		bool FNeedsUpdate = false;
		bool FInitialised = false;
		bool FLoaded = false;
		string FFilename;

		object FLock = new object();

		Vector3D[] xyz = new Vector3D[640 * 480];
		#endregion fields & pin

		// import host and hand it to base constructor
		[ImportingConstructor()]
		public EX9_GeometryPointCloudNode(IPluginHost host)
			: base(host)
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInFilename[0] != "" && !FLoaded)
			{
				FFilename = FPinInFilename[0];
				if (FFilename.EndsWith(".x"))
					FLoaded = true;
				else
					FLoaded = false;
			}

			//recreate mesh
			if (!FInitialised && FLoaded)
			{
				Reinitialize();
				FInitialised = true;
			}

			bool update = false;
			lock (FLock)
			{
				//update mesh
				update = FNeedsUpdate;
				FNeedsUpdate = false;
			}
			if (update)
				Update();

			
			if (FImage != FPinInImage[0] && FPinInImage[0] != null)
			{
				FImage = FPinInImage[0];
				FImage.ImageUpdate += new EventHandler(FImage_ImageUpdate);
			}	
		}

		unsafe void FImage_ImageUpdate(object sender, EventArgs e)
		{
			var image = sender as ImageRGB32F;
			if (image == null)
				return;

			float* c = (float*)image.Image.MIplImage.imageData.ToPointer() ;
			lock (FLock)
			{
				for (int i = 0; i < 640; i++)
					for (int j = 0; j < 480; j++)
					{
						xyz[i + j * 480] = new Vector3D((double)c[3*(i+j*640)+0],(double)c[3*(i+j*640)+1],(double)c[3*(i+j*640)+2]);
					}
				FNeedsUpdate = true;
			}
		}

		//this method gets called, when Reinitialize() was called in evaluate,
		//or a graphics device asks for its data
		protected override Mesh CreateMesh(Device device)
		{
			FLogger.Log(LogType.Debug, "Creating Mesh...");

			return Mesh.CreateSphere(device, 1, 32, 32);
			//return Mesh.FromFile(device, FFilename, MeshFlags.Dynamic);
		}

		//this method gets called, when Update() was called in evaluate,
		//or a graphics device asks for its mesh, here you can alter the data of the mesh
		protected override void UpdateMesh(Mesh mesh)
		{
			//do something with the mesh data
			var vertices = mesh.LockVertexBuffer(LockFlags.None);

			for (int i = 0; i < mesh.VertexCount; i++)
			{
				//get the vertex content
				var pos = vertices.Read<Vector3>();
				var norm = vertices.Read<Vector3>();

				pos.X = (float)xyz[i].x;
				pos.Y = (float)xyz[i].y;
				pos.Z = (float)xyz[i].z;

				//to write the data move the stream position back!
				vertices.Position -= mesh.BytesPerVertex;
				vertices.Write(pos);
				vertices.Write(norm);
			}

			mesh.UnlockVertexBuffer();
		}
	}
}

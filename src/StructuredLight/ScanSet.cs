using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace VVVV.Nodes.EmguCV.StructuredLight
{
	class ScanSet
	{
		/// <summary>
		/// Raw data (encoded)
		/// </summary>
		public ulong[] Data;

		/// <summary>
		/// How far on average the pixel value stepped
		/// </summary>
		public float[] Stride;

		public IPayload Payload;
		public Size CameraSize;
		public int CameraPixelCount
		{
			get
			{
				return CameraSize.Width * CameraSize.Height;
			}
		}
		public Size ProjectorSize
		{
			get
			{
				return Payload.Size;
			}
		}

		public event EventHandler UpdateAttributes;
		public void OnUpdateAttributes()
		{
			FInitialised = true;
			if (UpdateAttributes != null)
				UpdateAttributes(this, EventArgs.Empty);
		}

		public event EventHandler UpdateData;
		public void OnUpdateData()
		{
			FDataAvailable = true;
			if (UpdateData != null)
				UpdateData(this, EventArgs.Empty);
		}

		bool FDataAvailable = false;
		public bool DataAvailable
		{
			get
			{
				return FDataAvailable;
			}
		}

		bool FInitialised = false;
		public bool Initialised
		{
			get
			{
				return FInitialised;
			}
		}

		public void Allocate(Size CameraSize)
		{
			this.CameraSize = CameraSize;
			this.Data = new ulong[CameraSize.Width * CameraSize.Height];
			this.Stride = new float[CameraSize.Width * CameraSize.Height];
			this.OnUpdateAttributes();
			FInitialised = true;
		}

		public unsafe void Clear()
		{
			if (!this.Initialised)
				return;

			int n = this.CameraPixelCount;
			fixed (ulong* indexFixed = &this.Data[0])
			{
				ulong* index = indexFixed;
				for (int i = 0; i < n; i++)
					*index++ = 0;
			}

			FDataAvailable = false;
		}

		public bool GetValue(ulong index, ref ulong output)
		{
			if (index > Payload.MaxIndex)
				return false;

			output = Payload.DataInverse[index];

			return true;
		}
	}
}

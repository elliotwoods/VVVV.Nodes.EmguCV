using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EmguCV
{
	class CVImageInputSpreadWith<T> : CVImageInputSpread where T : INeedsCVImageLink, IDisposable, new()
	{
		Spread<T> FWithSpread = new Spread<T>(0);

		public CVImageInputSpreadWith(ISpread<CVImageLink> inputPin) : base(inputPin)
		{
		}

		
		public T GetWith(int index)
		{
			return FWithSpread[index];
		}

		protected override void AddWith(CVImageInput input)
		{
			T addition = new T();
			addition.Initialise(input);
			FWithSpread.Add(addition);
		}

		protected override void DisposeWith(int i)
		{
			FWithSpread[i].Dispose();
		}

		protected override void ResizeWith(int count)
		{
			FWithSpread.SliceCount = count;
		}
	}
}

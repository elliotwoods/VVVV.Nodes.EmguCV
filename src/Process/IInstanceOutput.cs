using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.EmguCV
{
	public interface IInstanceOutput
	{
		void SetOutput(CVImageOutput output);
	}
}

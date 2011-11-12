using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.EmguCV
{
	interface INeedsCVImageLink
	{
		void Initialise(CVImageInput input);
	}
}

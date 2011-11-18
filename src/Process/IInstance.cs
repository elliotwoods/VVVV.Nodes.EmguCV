using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.EmguCV
{
	interface IInstance
	{
		void Initialise();
		void Process();
		bool NeedsInitialise();
	}
}

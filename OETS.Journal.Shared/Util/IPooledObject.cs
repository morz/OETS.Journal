using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OETS.Shared.Util
{
	public interface IPooledObject
	{
		void Cleanup();
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeTemp.Interface
{
	public  interface IAppLogger
	{
		void Information(string message);
		void Warning(string message);
		void Error(string message);
	}
}

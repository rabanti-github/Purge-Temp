using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeTemp.Interface
{
	public interface ILoggerFactory
	{
		IAppLogger CreateAppLogger();
		IPurgeLogger CreatePurgeLogger();
	}
}

using Microsoft.Extensions.DependencyInjection;
using PurgeTemp.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeTemp.Logger
{
	public class LoggerFactory : ILoggerFactory
	{
		private readonly IServiceProvider serviceProvider;

		public LoggerFactory(IServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider;
		}
		public IAppLogger CreateAppLogger()
		{
			return serviceProvider.GetRequiredService<IAppLogger>();
		}

		public IPurgeLogger CreatePurgeLogger()
		{
			return serviceProvider.GetRequiredService<IPurgeLogger>();
		}
	}
}

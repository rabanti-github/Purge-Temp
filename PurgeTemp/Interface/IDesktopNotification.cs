using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PurgeTemp.Logger.DesktopNotification;

namespace PurgeTemp.Interface
{
	public interface IDesktopNotification
	{
		public enum Status
		{
			GENERAL,
			OK,
			SKIP,
			ERROR
		}

		public void ShowNotification(string title, string message, Status status);
	}
}

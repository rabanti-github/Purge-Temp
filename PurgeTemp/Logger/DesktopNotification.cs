/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2026
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using Microsoft.Toolkit.Uwp.Notifications;
using PurgeTemp.Interface;
using PurgeTemp.Utils;
using static PurgeTemp.Interface.IDesktopNotification;

namespace PurgeTemp.Logger
{
	/// <summary>
	/// Class to handle desktop notifications
	/// </summary>
	public class DesktopNotification : IDesktopNotification
	{
		private readonly ISettings settings;
		private	readonly PathUtils pathUtils;

		public DesktopNotification(ISettings settings, PathUtils pathUtils)
		{
			this.settings = settings;
			this.pathUtils = pathUtils;
		}

		public void ShowNotification(string title, string message, Status status)
		{
			if (!settings.ShowPurgeMessage)
			{
				return;
			}
			string icon;
			switch (status)
			{
				case Status.OK:
					icon = "trashcan-ok128.png";
					break;
				case Status.SKIP:
					icon = "trashcan-skipped128.png";
					break;
				case Status.ERROR:
					icon = "trashcan-error128.png";
					break;
				default:
					icon = "trashcan128.png";
					break;
			}
			String iconPath = null;
			if (!string.IsNullOrEmpty(settings.PurgeMessageLogoFile))
			{
				string path = pathUtils.GetPath(settings.PurgeMessageLogoFile);
				if (File.Exists(path))
				{
					iconPath = path;
				}
				else
				{
					iconPath = pathUtils.GetPath("./resources/" + icon);
				}
			}
			else
			{
				iconPath = pathUtils.GetPath("./resources/" + icon);
			}
			new ToastContentBuilder()
					.AddToastActivationInfo("", ToastActivationType.Foreground)
					.AddText(settings.TestEnvironmentMessage + title)
					.AddText(message)
					.AddAppLogoOverride(new Uri(iconPath))
					.Show();
		}
	}
}

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
			string icon = GetIconFileName(status);
			string iconPath = ResolveIconPath(icon);
			new ToastContentBuilder()
					.AddToastActivationInfo("", ToastActivationType.Foreground)
					.AddText(settings.TestEnvironmentMessage + title)
					.AddText(message)
					.AddAppLogoOverride(new Uri(iconPath))
					.Show();
		}

		internal static string GetIconFileName(Status status)
		{
			switch (status)
			{
				case Status.OK:
					return "trashcan-ok128.png";
				case Status.SKIP:
					return "trashcan-skipped128.png";
				case Status.ERROR:
					return "trashcan-error128.png";
				default:
					return "trashcan128.png";
			}
		}

		internal string ResolveIconPath(string icon)
		{
			if (!string.IsNullOrEmpty(settings.PurgeMessageLogoFile))
			{
				string path = pathUtils.GetPath(settings.PurgeMessageLogoFile);
				if (File.Exists(path))
				{
					return path;
				}
				else
				{
					return pathUtils.GetPath("./resources/" + icon);
				}
			}
			else
			{
				return pathUtils.GetPath("./resources/" + icon);
			}
		}
	}
}

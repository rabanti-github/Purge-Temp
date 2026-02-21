/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2026
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using PurgeTemp.Controller;
using PurgeTemp.Interface;
using PurgeTemp.Logger;
using PurgeTemp.Utils;
using PurgeTempTest.Utils;
using static PurgeTemp.Interface.IDesktopNotification;

namespace PurgeTempTest
{
	// ========== Stubs ==========

	internal class NullLoggerFactory : ILoggerFactory
	{
		public IAppLogger CreateAppLogger() => new NullAppLogger();
		public IPurgeLogger CreatePurgeLogger() => throw new NotImplementedException();
	}

	internal class NullAppLogger : IAppLogger
	{
		public void Error(string message) { }
		public void Information(string message) { }
		public void Warning(string message) { }
	}

	// ========== Tests ==========

	public class DesktopNotificationTest
	{
		// ========== GetIconFileName tests ==========

		[Theory(DisplayName = "Test that GetIconFileName returns the correct icon file name for each status")]
		[InlineData(Status.OK, "trashcan-ok128.png")]
		[InlineData(Status.SKIP, "trashcan-skipped128.png")]
		[InlineData(Status.ERROR, "trashcan-error128.png")]
		[InlineData(Status.GENERAL, "trashcan128.png")]
		public void GetIconFileNameTest(Status status, string expectedIcon)
		{
			string icon = DesktopNotification.GetIconFileName(status);
			Assert.Equal(expectedIcon, icon);
		}

		// ========== ResolveIconPath tests ==========

		[Fact(DisplayName = "Test that ResolveIconPath returns a default resource path when PurgeMessageLogoFile is empty")]
		public void ResolveIconPathWithEmptyLogoFileTest()
		{
			Settings settings = CreateSettings("");
			DesktopNotification notification = new DesktopNotification(settings, CreatePathUtils(settings));
			string result = notification.ResolveIconPath("trashcan-ok128.png");
			Assert.Contains("resources", result);
			Assert.Contains("trashcan-ok128.png", result);
		}

		[Fact(DisplayName = "Test that ResolveIconPath returns a default resource path when PurgeMessageLogoFile does not exist on disk")]
		public void ResolveIconPathWithNonExistentLogoFileTest()
		{
			Settings settings = CreateSettings("nonexistent_logo_file_xyz.png");
			DesktopNotification notification = new DesktopNotification(settings, CreatePathUtils(settings));
			string result = notification.ResolveIconPath("trashcan-error128.png");
			Assert.Contains("resources", result);
			Assert.Contains("trashcan-error128.png", result);
		}

		[Fact(DisplayName = "Test that ResolveIconPath returns the custom logo path when PurgeMessageLogoFile exists on disk")]
		public void ResolveIconPathWithExistingLogoFileTest()
		{
			string tempFile = Path.GetTempFileName();
			try
			{
				Settings settings = CreateSettings(tempFile);
				DesktopNotification notification = new DesktopNotification(settings, CreatePathUtils(settings));
				string result = notification.ResolveIconPath("trashcan-ok128.png");
				Assert.Equal(tempFile, result);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		// ========== Helper methods ==========

		private static Settings CreateSettings(string logoFile)
		{
			Settings settings = TestSettingsProvider.GetSettings(Path.GetTempPath());
			settings.OverrideSetting(Settings.Keys.PurgeMessageLogoFile, logoFile);
			return settings;
		}

		private static PathUtils CreatePathUtils(Settings settings)
		{
			return new PathUtils(settings, new NullLoggerFactory());
		}
	}
}

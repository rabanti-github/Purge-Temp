/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2026
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using PurgeTemp.Interface;
using PurgeTemp.Utils;
using Serilog;

namespace PurgeTemp.Logger
{
	/// <summary>
	/// Class to log application events and messages
	/// </summary>
	public class AppLogger : IAppLogger
	{
		public const string LOGFILE_NAME_TEMPLATE = "appLog_";
		public const string LOGFILE_EXTENSION = ".txt";
		private readonly ISettings settings;
		private readonly PathUtils pathUtils;

		private ILogger logger;

		public AppLogger(ISettings settings, PathUtils pathUtils)
		{
			this.settings = settings;
			this.pathUtils = pathUtils;
			Initialize();
		}

		private void Initialize()
		{
			LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
				.MinimumLevel.Verbose();

			// Configure application logger
			if (settings.LogEnabled)
			{
				string logFolder = null;
				// Configure file logging if enabled
				if (this.pathUtils.CheckSystemRelevantFolder(settings.LoggingFolder, false).IsNotValid)
				{
					// Fallback to application path
					logFolder = pathUtils.GetPath("./");
				}
				else
				{
					logFolder = pathUtils.GetPath(settings.LoggingFolder);
				}
				string logFilePath = Path.Combine(logFolder, LOGFILE_NAME_TEMPLATE + LOGFILE_EXTENSION);

				int? bytes = settings.LogRotationBytes == 0 ? null : settings.LogRotationBytes;
				int? versions = settings.LogRotationVersions == 0 ? null : settings.LogRotationVersions;

				loggerConfiguration.WriteTo.File(
					logFilePath,
					rollingInterval: RollingInterval.Day,
					fileSizeLimitBytes: bytes,
					rollOnFileSizeLimit: true,
					retainedFileCountLimit: versions,
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} {Message}{NewLine}"
				);
			}
			else
			{
				// Configure console logging if file logging is disabled
				loggerConfiguration.WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} {Message}{NewLine}");
			}

			logger = loggerConfiguration.CreateLogger();
		}

		public void Information(string message)
		{
			logger.Information(message);
		}

		public void Warning(string message)
		{
			logger.Warning(message);
		}

		public void Error(string message)
		{
			logger.Error(message);
		}
	}
}

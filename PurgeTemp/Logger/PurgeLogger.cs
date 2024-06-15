/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2024
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using Serilog;
using PurgeTemp.Utils;
using PurgeTemp.Interface;

namespace PurgeTemp.Logger
{
	/// <summary>
	/// Class to handle the logging of file operations during purge attempts
	/// </summary>
	public class PurgeLogger : IPurgeLogger
	{
		private readonly ISettings settings;
		private readonly PathUtils pathUtils;
		private ILogger logger;

		public PurgeLogger(ISettings settings, PathUtils pathUtils)
		{
			this.settings = settings;
			this.pathUtils = pathUtils;
			Initialize();
		}

		private void Initialize()
		{
			LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
			.MinimumLevel.Verbose();

			// Configure purge logger
			if (settings.LogEnabled && settings.LogAllFiles)
			{
				// Configure file logging if enabled
				string logFolder = pathUtils.GetPath(settings.LoggingFolder);
				string logFilePath = Path.Combine(logFolder, "purgeLog_.txt");

				loggerConfiguration.WriteTo.File(
					logFilePath,
					rollingInterval: RollingInterval.Day,
					fileSizeLimitBytes: settings.LogRotationBytes,
					rollOnFileSizeLimit: true,
					retainedFileCountLimit: settings.LogRotationVersions,
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


		public void PurgeInfo(string source, string file)
		{
			Info(source, null, false, file);
		}

		public void MoveInfo(string source, string target, string file)
		{
			Info(source, target, true, file);
		}

		public void SkipMoveInfo(string source, string target, int skippedFiles)
		{
			SkipInfo(source, target, false, skippedFiles);
		}

		public void SkipPurgeInfo(string source, int skippedFiles)
		{
			SkipInfo(source, null, true, skippedFiles);
		}

		public void SkipInfo(string source, string destination, bool isPurged, int skippedFiles)
		{
			string action = isPurged ? "P" : "M";
			string target = isPurged ? string.Empty : destination;
			if (isPurged)
			{
				logger.Information($"{action}\t{source} (Skipped {skippedFiles} files)");
			}
			else
			{
				logger.Information($"{action}\t{source} => {target} (Skipped {skippedFiles} files)");
			}
		}

		public void Info(string source, string destination, bool isPurged, string file)
		{
			string action = isPurged ? "P" : "M";
			string target = isPurged ? string.Empty : destination;
			string logEntry = $"{action}\t{source}";

			if (!isPurged)
			{
				logEntry += $" => {target}";
			}

			logger.Information(logEntry);
		}

	}
}


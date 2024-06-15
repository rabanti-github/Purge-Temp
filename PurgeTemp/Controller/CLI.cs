/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2024
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Configuration;
using PurgeTemp.Utils;
using System.Text;

namespace PurgeTemp.Controller
{
	/// <summary>
	/// Class to handle CLI arguments
	/// </summary>
	public class CLI
	{
		private readonly PathUtils pathUtils;

		public CLI(PathUtils pathUtils)
		{
			this.pathUtils = pathUtils;
		}

		[Option('s', "settings-file", Required = false, HelpText = "Path to a settings file that overrides the default settings. CLI arguments may override particular settings of that file.")]
		public string SettingsFile { get; set; }

		[Option('h', "help", Required = false, HelpText = "Display this help screen.")]
		public bool Help { get; set; }

		[Option('v', "stage-versions", Required = false, HelpText = "Number of stage versions.")]
		public int? StageVersions { get; set; }

		[Option('p', "stage-name-prefix", Required = false, HelpText = "Prefix for stage folder names.")]
		public string StageNamePrefix { get; set; }

		[Option('a', "append-number-on-first-stage", Required = false, HelpText = "If true, the stage number ia appended to the first stage folder name.")]
		public bool? AppendNumberOnFirstStage { get; set; }

		[Option('d', "stage-version-delimiter", Required = false, HelpText = "Delimiter for stage version numbers.")]
		public string StageVersionDelimiter { get; set; }

		[Option('t', "staging-delay-seconds", Required = false, HelpText = "Staging delay in seconds. A purge can only be executed if this period is exceeded since the last purge.")]
		public int? StagingDelaySeconds { get; set; }

		[Option('m', "show-purge-message", Required = false, HelpText = "Show purge message as desktop notifications.")]
		public bool? ShowPurgeMessage { get; set; }

		[Option('i', "purge-message-logo", Required = false, HelpText = "Purge message logo file for desktop notifications.")]
		public string PurgeMessageLogoFile { get; set; }

		[Option('f', "logging-folder", Required = false, HelpText = "Logging folder.")]
		public string LoggingFolder { get; set; }

		[Option('e', "log-enabled", Required = false, HelpText = "Enable logging.")]
		public bool? LogEnabled { get; set; }

		[Option('b', "log-rotation-bytes", Required = false, HelpText = "Log rotation size in bytes.")]
		public int? LogRotationBytes { get; set; }

		[Option('o', "log-rotation-versions", Required = false, HelpText = "Number of log rotation versions.")]
		public int? LogRotationVersions { get; set; }

		[Option('a', "log-all-files", Required = false, HelpText = "Log all files (when purge is executed).")]
		public bool? LogAllFiles { get; set; }

		[Option('g', "staging-timestamp-file", Required = false, HelpText = "Staging timestamp file name.")]
		public string StagingTimestampFile { get; set; }

		[Option('r', "stage-root-folder", Required = false, HelpText = "Stage root folder.")]
		public string StageRootFolder { get; set; }

		[Option('c', "config-folder", Required = false, HelpText = "Config folder.")]
		public string ConfigFolder { get; set; }

		[Option('z', "temp-folder", Required = false, HelpText = "Temporary folder.")]
		public string TempFolder { get; set; }

		[Option('k', "skip-token-file", Required = false, HelpText = "Skip token file name.")]
		public string SkipTokenFile { get; set; }

		[Option('y', "timestamp-format", Required = false, HelpText = "Timestamp format.")]
		public string TimeStampFormat { get; set; }

		[Option('l', "stage-last-name-suffix", Required = false, HelpText = "Name suffix of the last stage folder.")]
		public string StageLastNameSuffix { get; set; }

		[Option('u', "remove-empty-stage-folders", Required = false, HelpText = "Removes intermediate stage folders if empty.")]
		public bool? RemoveEmptyStageFolders { get; set; }

		[Option('q', "file-log-amount-threshold", Required = false, HelpText = "File log amount threshold. If more than this number of files are in a stage folder, only the remaining number will be logged on purge after exceeding this number.")]
		public int? FileLogAmountThreshold { get; set; }


		public Settings ParseSetting(Settings defaultSettings, IConfiguration configuration, string[] args)
		{
			CLI opts = null;

			Parser.Default.ParseArguments<CLI>(args)
				.WithParsed(parsed => opts = parsed);

			if (opts.Help)
			{
				DisplayHelp<CLI>(Parser.Default.ParseArguments<CLI>(args), args);
				Environment.Exit(0);
			}

			if (opts != null && !string.IsNullOrEmpty(opts.SettingsFile))
			{
				string settingPath = pathUtils.GetPath(opts.SettingsFile);
				defaultSettings.LoadSettings(configuration, settingPath);
			}
			else if (defaultSettings == null)
			{
				defaultSettings = new Settings(configuration);
			}

			if (opts != null)
			{
				if (opts.StageVersions.HasValue)
				   defaultSettings.OverrideSetting("AppSettings:StageVersions", opts.StageVersions.Value);

			   if (!string.IsNullOrEmpty(opts.StageNamePrefix))
				   defaultSettings.OverrideSetting("AppSettings:StageNamePrefix", opts.StageNamePrefix);

			   if (opts.AppendNumberOnFirstStage.HasValue)
				   defaultSettings.OverrideSetting("AppSettings:AppendNumberOnFirstStage", opts.AppendNumberOnFirstStage.Value);

			   if (!string.IsNullOrEmpty(opts.StageVersionDelimiter))
				   defaultSettings.OverrideSetting("AppSettings:StageVersionDelimiter", opts.StageVersionDelimiter);

			   if (opts.StagingDelaySeconds.HasValue)
				   defaultSettings.OverrideSetting("AppSettings:StagingDelaySeconds", opts.StagingDelaySeconds.Value);

			   if (opts.ShowPurgeMessage.HasValue)
				   defaultSettings.OverrideSetting("AppSettings:ShowPurgeMessage", opts.ShowPurgeMessage.Value);

			   if (!string.IsNullOrEmpty(opts.PurgeMessageLogoFile))
				   defaultSettings.OverrideSetting("AppSettings:PurgeMessageLogoFile", opts.PurgeMessageLogoFile);

			   if (!string.IsNullOrEmpty(opts.LoggingFolder))
				   defaultSettings.OverrideSetting("AppSettings:LoggingFolder", opts.LoggingFolder);

			   if (opts.LogEnabled.HasValue)
				   defaultSettings.OverrideSetting("AppSettings:LogEnabled", opts.LogEnabled.Value);

			   if (opts.LogRotationBytes.HasValue)
				   defaultSettings.OverrideSetting("AppSettings:LogRotationBytes", opts.LogRotationBytes.Value);

			   if (opts.LogRotationVersions.HasValue)
				   defaultSettings.OverrideSetting("AppSettings:LogRotationVersions", opts.LogRotationVersions.Value);

			   if (opts.LogAllFiles.HasValue)
				   defaultSettings.OverrideSetting("AppSettings:LogAllFiles", opts.LogAllFiles.Value);

			   if (!string.IsNullOrEmpty(opts.StagingTimestampFile))
				   defaultSettings.OverrideSetting("AppSettings:StagingTimestampFile", opts.StagingTimestampFile);

			   if (!string.IsNullOrEmpty(opts.StageRootFolder))
				   defaultSettings.OverrideSetting("AppSettings:StageRootFolder", opts.StageRootFolder);

			   if (!string.IsNullOrEmpty(opts.ConfigFolder))
				   defaultSettings.OverrideSetting("AppSettings:ConfigFolder", opts.ConfigFolder);

			   if (!string.IsNullOrEmpty(opts.TempFolder))
				   defaultSettings.OverrideSetting("AppSettings:TempFolder", opts.TempFolder);

			   if (!string.IsNullOrEmpty(opts.SkipTokenFile))
				   defaultSettings.OverrideSetting("AppSettings:SkipTokenFile", opts.SkipTokenFile);

			   if (!string.IsNullOrEmpty(opts.TimeStampFormat))
				   defaultSettings.OverrideSetting("AppSettings:TimeStampFormat", opts.TimeStampFormat);

			   if (!string.IsNullOrEmpty(opts.StageLastNameSuffix))
				   defaultSettings.OverrideSetting("AppSettings:StageLastNameSuffix", opts.StageLastNameSuffix);

				if (opts.RemoveEmptyStageFolders.HasValue)
					defaultSettings.OverrideSetting("AppSettings:RemoveEmptyStageFolders", opts.RemoveEmptyStageFolders);

				if (opts.FileLogAmountThreshold.HasValue)
				   defaultSettings.OverrideSetting("AppSettings:FileLogAmountThreshold", opts.FileLogAmountThreshold.Value);
		   }
			return defaultSettings;
		}

		private void DisplayHelp<T>(ParserResult<T> parserResult, string[] args)
		{
			HelpText helpText = HelpText.AutoBuild(parserResult, h => {return h;}, e => e);
			helpText.AdditionalNewLineAfterOption = false;
			helpText.Heading = $"{AssemblyUtils.GetAssemblyName()} v{AssemblyUtils.GetVersion()}";
			helpText.Copyright = AssemblyUtils.GetCopyright();
			helpText.AddPreOptionsLine(GetCLIPrefix());
			//helpText.AddPreOptionsLine( "This is a sample application to demonstrate command line parsing.");
			Console.WriteLine(helpText);
		}

		private string GetCLIPrefix()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("\n");
			sb.Append(AssemblyUtils.GetDescription());
			sb.Append("\n\nProject URL: ");
			sb.Append(AssemblyUtils.GetRepositoryURL());
			sb.Append("\n-------------------------\nArguments\n=========");
			return sb.ToString();
		}


	}
}

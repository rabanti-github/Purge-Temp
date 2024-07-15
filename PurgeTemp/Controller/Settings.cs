/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2024
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using Microsoft.Extensions.Configuration;
using PurgeTemp.Interface;

namespace PurgeTemp.Controller
{
	/// <summary>
	/// Class to hold configuration information (settings)
	/// </summary>
	public class Settings : ISettings
	{
		public static class Keys
		{
			public const string AppendNumberOnFirstStage = "AppSettings:AppendNumberOnFirstStage";
			public const string ConfigFolder = "AppSettings:ConfigFolder";
			public const string FileLogAmountThreshold = "AppSettings:FileLogAmountThreshold";
			public const string LogAllFiles = "AppSettings:LogAllFiles";
			public const string LogEnabled = "AppSettings:LogEnabled";
			public const string LoggingFolder = "AppSettings:LoggingFolder";
			public const string LogRotationBytes = "AppSettings:LogRotationBytes";
			public const string LogRotationVersions = "AppSettings:LogRotationVersions";
			public const string PurgeMessageLogoFile = "AppSettings:PurgeMessageLogoFile";
			public const string RemoveEmptyStageFolders = "AppSettings:RemoveEmptyStageFolders";
			public const string ShowPurgeMessage = "AppSettings:ShowPurgeMessage";
			public const string SkipTokenFile = "AppSettings:SkipTokenFile";
			public const string StageLastNameSuffix = "AppSettings:StageLastNameSuffix";
			public const string StageNamePrefix = "AppSettings:StageNamePrefix";
			public const string StageRootFolder = "AppSettings:StageRootFolder";
			public const string StageVersions = "AppSettings:StageVersions";
			public const string StageVersionDelimiter = "AppSettings:StageVersionDelimiter";
			public const string StagingDelaySeconds = "AppSettings:StagingDelaySeconds";
			public const string StagingTimestampFile = "AppSettings:StagingTimestampFile";
			public const string TempFolder = "AppSettings:TempFolder";
			public const string TimeStampFormat = "AppSettings:TimeStampFormat";
		}

		internal const string APP_SETTINGS_FILE = "appsettings.json";

		private readonly IConfiguration configuration;
		private readonly Dictionary<string, object> overrides;

		public Settings(IConfiguration configuration, string path)
		{
			this.overrides = new Dictionary<string, object>();
			this.configuration = configuration;
			LoadSettings(configuration, path);
		}

		public Settings(IConfiguration configuration)
		{
			this.configuration = configuration;
			this.overrides = new Dictionary<string, object>();
		}

		public bool AppendNumberOnFirstStage => GetValue<bool>(Keys.AppendNumberOnFirstStage);
		public string ConfigFolder => GetValue<string>(Keys.ConfigFolder);
		public int StageVersions => GetValue<int>(Keys.StageVersions);
		public string StageNamePrefix => GetValue<string>(Keys.StageNamePrefix);
		public bool RemoveEmptyStageFolders => GetValue<bool>(Keys.RemoveEmptyStageFolders);
		public string StageVersionDelimiter => GetValue<string>(Keys.StageVersionDelimiter);
		public int StagingDelaySeconds => GetValue<int>(Keys.StagingDelaySeconds);
		public bool ShowPurgeMessage => GetValue<bool>(Keys.ShowPurgeMessage);
		public string PurgeMessageLogoFile => GetValue<string>(Keys.PurgeMessageLogoFile);
		public string LoggingFolder => GetValue<string>(Keys.LoggingFolder);
		public bool LogEnabled => GetValue<bool>(Keys.LogEnabled);
		public int LogRotationBytes => GetValue<int>(Keys.LogRotationBytes);
		public int LogRotationVersions => GetValue<int>(Keys.LogRotationVersions);
		public bool LogAllFiles => GetValue<bool>(Keys.LogAllFiles);
		public string StagingTimestampFile => GetValue<string>(Keys.StagingTimestampFile);
		public string StageRootFolder => GetValue<string>(Keys.StageRootFolder);
		public string TempFolder => GetValue<string>(Keys.TempFolder);
		public string SkipTokenFile => GetValue<string>(Keys.SkipTokenFile);
		public string TimeStampFormat => GetValue<string>(Keys.TimeStampFormat);
		public string StageLastNameSuffix => GetValue<string>(Keys.StageLastNameSuffix);
		public int FileLogAmountThreshold => GetValue<int>(Keys.FileLogAmountThreshold);


		private T GetValue<T>(string key)
		{
			if (overrides.ContainsKey(key))
			{
				object value = overrides[key];
				if (value is T typedValue)
				{
					return typedValue;
				}
				else if (value is string stringValue)
				{
					return (T)Convert.ChangeType(stringValue, typeof(T));
				}
				else
				{
					throw new InvalidCastException($"Cannot cast '{value.GetType()}' to '{typeof(T)}'");
				}
			}
			var configValue = configuration.GetValue<string>(key);
			if (configValue != null)
			{
				return (T)Convert.ChangeType(configValue, typeof(T));
			}

			return configuration.GetValue<T>(key);
		}

		public void OverrideSetting<T>(string key, T value)
		{
			overrides[key] = value;
		}

		public void LoadSettings(IConfiguration configuration, string path)
		{
			foreach (var kvp in configuration.AsEnumerable())
			{
				overrides[kvp.Key] = configuration.GetValue<object>(kvp.Key);
			}
		}
	}
}

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

		public int StageVersions => GetValue<int>("AppSettings:StageVersions");
		public string StageNamePrefix => GetValue<string>("AppSettings:StageNamePrefix");
		public bool AppendNumberOnFirstStage => GetValue<bool>("AppSettings:AppendNumberOnFirstStage");
        public bool RemoveEmptyStageFolders => GetValue<bool>("AppSettings:RemoveEmptyStageFolders");
        public string StageVersionDelimiter => GetValue<string>("AppSettings:StageVersionDelimiter");
		public int StagingDelaySeconds => GetValue<int>("AppSettings:StagingDelaySeconds");
		public bool ShowPurgeMessage => GetValue<bool>("AppSettings:ShowPurgeMessage");
		public string PurgeMessageLogoFile => GetValue<string>("AppSettings:PurgeMessageLogoFile");
		public string LoggingFolder => GetValue<string>("AppSettings:LoggingFolder");
		public bool LogEnabled => GetValue<bool>("AppSettings:LogEnabled");
		public int LogRotationBytes => GetValue<int>("AppSettings:LogRotationBytes");
		public int LogRotationVersions => GetValue<int>("AppSettings:LogRotationVersions");
		public bool LogAllFiles => GetValue<bool>("AppSettings:LogAllFiles");
		public string StagingTimestampFile => GetValue<string>("AppSettings:StagingTimestampFile");
		public string StageRootFolder => GetValue<string>("AppSettings:StageRootFolder");
		public string ConfigFolder => GetValue<string>("AppSettings:ConfigFolder");
		public string TempFolder => GetValue<string>("AppSettings:TempFolder");
		public string SkipTokenFile => GetValue<string>("AppSettings:SkipTokenFile");
		public string TimeStampFormat => GetValue<string>("AppSettings:TimeStampFormat");
		public string StageLastNameSuffix => GetValue<string>("AppSettings:StageLastNameSuffix");
		public int FileLogAmountThreshold => GetValue<int>("AppSettings:FileLogAmountThreshold");


		private T GetValue<T>(string key)
		{
			if (overrides.ContainsKey(key))
			{
				return (T)overrides[key];
			}
			return configuration.GetValue<T>(key);
		}

		public void OverrideSetting<T>(string key, T value)
		{
			overrides[key] = value;
		}

		public void LoadSettings(IConfiguration configuration, string path)
		{
			//var configBuilder = new ConfigurationBuilder()
			//	.AddJsonFile(path, optional: true, reloadOnChange: true);
			//IConfiguration configuration = configBuilder.Build();
			foreach (var kvp in configuration.AsEnumerable())
			{
				overrides[kvp.Key] = configuration.GetValue<object>(kvp.Key);
			}
		}
	}
}

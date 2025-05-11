using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeTemp.Interface
{
	public interface ISettings
	{
		bool AppendNumberOnFirstStage { get; }
		string ConfigFolder { get; }
		int FileLogAmountThreshold { get; }
		bool LogAllFiles { get; }
		int StageVersions { get; }
		bool LogEnabled { get; }
		string LoggingFolder { get; }
		int LogRotationBytes { get; }
		int LogRotationVersions { get; }
		string PurgeMessageLogoFile { get; }
		bool RemoveEmptyStageFolders { get; }
		bool ShowPurgeMessage { get; }
		string SkipTokenFile { get; }
		string StageLastNameSuffix { get; }
		string StageNamePrefix { get; }
		string StageRootFolder { get; }
		string StageVersionDelimiter { get; }
		int StagingDelaySeconds { get; }
		string StagingTimestampFile { get; }
		string TempFolder { get; }
		string TimeStampFormat { get; }
		void OverrideSetting<T>(string key, T value);
		string TestEnvironmentMessage { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeTemp.Interface
{
    public interface ISettings
    {
        int StageVersions { get; }
        string StageNamePrefix { get; }
        bool AppendNumberOnFirstStage { get; }
        bool RemoveEmptyStageFolders { get; }
        string StageVersionDelimiter { get; }
        int StagingDelaySeconds { get; }
        bool ShowPurgeMessage { get; }
        string PurgeMessageLogoFile { get; }
        string LoggingFolder { get; }
        bool LogEnabled { get; }
        int LogRotationBytes { get; }
        int LogRotationVersions { get; }
        bool LogAllFiles { get; }
        string StagingTimestampFile { get; }
        string StageRootFolder { get; }
        string ConfigFolder { get; }
        string TempFolder { get; }
        string SkipTokenFile { get; }
        string TimeStampFormat { get; }
        string StageLastNameSuffix { get; }
        int FileLogAmountThreshold { get; }

        void OverrideSetting<T>(string key, T value);
    }
}

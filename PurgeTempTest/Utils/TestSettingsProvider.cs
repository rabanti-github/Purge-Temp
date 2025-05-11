using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PurgeTemp.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeTempTest.Utils
{
	public static class TestSettingsProvider
	{
		public static Settings GetSettings(string tempFolderPath, Dictionary<string, string>? overrides = null)
		{
			// Base JSON template for settings
			string jsonTemplate = @"
        {
            ""AppSettings"": {
                ""AppendNumberOnFirstStage"": true,
                ""ConfigFolder"": ""./config"",
                ""FileLogAmountThreshold"": 1000,
                ""StageVersions"": 4,
                ""StageNamePrefix"": ""purge-temp"",
                ""StageVersionDelimiter"": ""-"",
                ""StagingDelaySeconds"": 21600,
                ""ShowPurgeMessage"": false,
                ""PurgeMessageLogoFile"": """",
                ""LoggingFolder"": ""./log"",
                ""LogEnabled"": false,
                ""LogRotationBytes"": 10485760,
                ""LogRotationVersions"": 10,
                ""LogAllFiles"": false,
                ""StagingTimestampFile"": ""./last-purge.txt"",
                ""StageRootFolder"": ""C:\\purge-temp"",
                ""TempFolder"": ""./temp"",
                ""SkipTokenFile"": ""./SKIP.txt"",
                ""TimeStampFormat"": ""yyyy-MM-dd HH:mm:ss"",
                ""StageLastNameSuffix"": ""LAST""
            }
        }";

			// Deserialize the JSON template
			var jsonSettings = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonTemplate);

			// Override folder paths to use the tempFolderPath
			jsonSettings["AppSettings"]["LoggingFolder"] = $"{tempFolderPath}\\log";
			jsonSettings["AppSettings"]["StagingTimestampFile"] = $"{tempFolderPath}\\last-purge.txt";
			jsonSettings["AppSettings"]["StageRootFolder"] = $"{tempFolderPath}\\stage-root";
			jsonSettings["AppSettings"]["ConfigFolder"] = $"{tempFolderPath}\\config";
			jsonSettings["AppSettings"]["TempFolder"] = $"{tempFolderPath}\\temp";
			jsonSettings["AppSettings"]["SkipTokenFile"] = $"{tempFolderPath}\\SKIP.txt";


			// Apply any additional overrides
			if (overrides != null)
			{
				foreach (KeyValuePair<string, string> kvp in overrides)
				{
					string value = kvp.Value;
					if (value == null)
					{
						value = "";
					}
					if (kvp.Key.StartsWith("AppSettings:"))
					{
						jsonSettings["AppSettings"][kvp.Key.Substring(12)] = value;
					}
					else
					{
						jsonSettings["AppSettings"][kvp.Key] = value;
					}
				}
			}

			// Convert the dictionary back to JSON
			string updatedJson = JsonConvert.SerializeObject(jsonSettings);

			// Build configuration from the updated JSON
			IConfiguration configuration = new ConfigurationBuilder()
				.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(updatedJson)))
				.Build();
			return new Settings(configuration);
		}
	}
}

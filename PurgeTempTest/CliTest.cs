using Newtonsoft.Json.Linq;
using PurgeTemp.Controller;
using PurgeTemp.Logger;
using PurgeTempTest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Maps;

namespace PurgeTempTest
{
	public class CliTest : IClassFixture<TempFolderFixture>
	{
		private readonly TempFolderFixture fixture;

		public CliTest(TempFolderFixture fixture)
		{
			this.fixture = fixture;
		}

		[Theory(DisplayName = "Test of the CLI property to enable the stage number on the first folder")]
		[InlineData(true, "a", "-", "a-1")]
		[InlineData(false, "a", "-", "a")]
		[InlineData(true, "a", "", "a1")]
		[InlineData(false, "a", "", "a")]
		[InlineData(true, "temp-", "-", "temp--1")]
		[InlineData(false, "temp-", "-", "temp-")]
		public void AppendNumberOnFirstStageTest(bool givenAppend, string givenPrefix, string givenDelimiter, string expectedFolderName)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();

			// Create test settings with basic override
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, givenPrefix);
			testSettings.OverrideSetting(Settings.Keys.StageVersions, 1);
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, givenDelimiter);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, givenAppend);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			// Initial purge
			purgeController.ExecutePurge();

			string expectedFolder = Path.Combine(stageRootFolder, expectedFolderName);

			List<string> filesInRoot = Directory.GetDirectories(stageRootFolder).ToList<string>();
			Assert.Contains(expectedFolder, filesInRoot);
		}

		[Theory(DisplayName = "Test of the CLI property to control the name of the config folder")]
		[InlineData("a")]
		[InlineData("config")]
		[InlineData("cfg")]
		public void ConfigFolderTest(string givenName)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string expectedFolder = Path.Combine(stageRootFolder, givenName);
			AssertAdministrativeFolderTest(Settings.Keys.ConfigFolder, expectedFolder, stageRootFolder);
		}

		[Theory(DisplayName = "Test of the failing CLI property to control the name of the config folder")]
		[InlineData("NUL", "purge-temp")]
		[InlineData("<", "purge-temp")]
		[InlineData("\t", "purge-temp")]
		[InlineData("cfg ", "purge-temp")]
		[InlineData("C:\\program files", "purge-temp")] // NOTE: For security reasons avoid running this test case as admin
		public void ConfigFolderFailTest(string givenConfigName, string givenStageName)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			AssertAdministrativeFolderFailTest(Settings.Keys.ConfigFolder, givenConfigName, stageRootFolder, givenStageName);
		}

		[Theory(DisplayName = "Test of the failing CLI property to control the name of the config folder if it conflicts with the stage folders")]
		[InlineData("purge-temp-LAST", "purge-temp")]
		[InlineData("purge-temp", "purge-temp")]
		public void ConfigFolderFailTest2(string givenConfigName, string givenStageName)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string absolutePath = Path.Combine(stageRootFolder, givenConfigName);
			AssertAdministrativeFolderFailTest(Settings.Keys.ConfigFolder, absolutePath, stageRootFolder, givenStageName);
		}

		[Theory(DisplayName = "Test of the CLI property to control the number of logged files when a purge is performed")]
		[InlineData(0, 1, 1)]
		[InlineData(0, 4, 1)]
		[InlineData(1, 1, 1)]
		[InlineData(3, 3, 3)]
		[InlineData(3, 4, 4)]
		[InlineData(3, 5, 4)]
		[InlineData(4, 10, 5)]
		public void FileLogAmountThresholdTest(int givenThreshold, int givenFiles, int expectedLogLines)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string expectedLogFolder = Path.Combine(stageRootFolder, "log");
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			testSettings.OverrideSetting(Settings.Keys.FileLogAmountThreshold, givenThreshold);
			testSettings.OverrideSetting(Settings.Keys.LoggingFolder, expectedLogFolder);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			testSettings.OverrideSetting(Settings.Keys.LogEnabled, true);
			testSettings.OverrideSetting(Settings.Keys.LogAllFiles, true);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 2);
			string stageFolder1 = Path.Combine(stageRootFolder, "purge-temp");
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// Initial purge
			purgeController.ExecutePurge();
			for (int i = 0; i < givenFiles; i++)
			{
				string currentNumber = (i + 1).ToString();
				FileUtils.CreateFile(stageFolder1, "file" + currentNumber + ".txt", "Content of file " + currentNumber);
			}
			Thread.Sleep(3000);
			purgeController.ExecutePurge();
			string[] files = Directory.GetFiles(expectedLogFolder, PurgeLogger.LOGFILE_NAME_TEMPLATE + "*");
			int logLines = FileUtils.CountLines(files[0]);
			Assert.Equal(logLines, expectedLogLines);
		}

		[Theory(DisplayName = "Test of the failing CLI property to control the number of logged files, when the number is negative")]
		[InlineData(-1)]
		[InlineData(-2)]
		[InlineData(-10)]
		public void FileLogAmountThresholdFailTest(int threshold)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();

			// Create test settings with basic override
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.FileLogAmountThreshold, threshold);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			purgeController.ExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}

		[Theory(DisplayName = "Test of the CLI property to control the name of the logging folder")]
		[InlineData("a")]
		[InlineData("logging")]
		[InlineData("log")]
		public void LoggingFolderTest(string givenName)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string expectedFolder = Path.Combine(stageRootFolder, givenName);
			Dictionary<string, object> additionalSettings = new Dictionary<string, object>();
			additionalSettings.Add(Settings.Keys.LogEnabled, true);
			AssertAdministrativeFolderTest(Settings.Keys.LoggingFolder, expectedFolder, stageRootFolder, additionalSettings);
		}

		[Theory(DisplayName = "Test of the failing CLI property to control the name of the logging folder")]
		[InlineData("NUL", "purge-temp")]
		[InlineData("<", "purge-temp")]
		[InlineData("\t", "purge-temp")]
		[InlineData("log ", "purge-temp")]
		[InlineData("C:\\program files", "purge-temp")] // NOTE: For security reasons avoid running this test case as admin
		public void LoggingFolderFailTest(string givenConfigName, string givenStageName)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Dictionary<string, object> additionalSettings = new Dictionary<string, object>();
			additionalSettings.Add(Settings.Keys.LogEnabled, true);
			AssertAdministrativeFolderFailTest(Settings.Keys.LoggingFolder, givenConfigName, stageRootFolder, givenStageName, additionalSettings);
		}

		[Theory(DisplayName = "Test of the failing CLI property to control the name of the logging folder if it conflicts with the stage folders")]
		[InlineData("purge-temp-LAST", "purge-temp")]
		[InlineData("purge-temp", "purge-temp")]
		public void LoggingFolderFailTest2(string givenConfigName, string givenStageName)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string absolutePath = Path.Combine(stageRootFolder, givenConfigName);
			Dictionary<string, object> additionalSettings = new Dictionary<string, object>();
			additionalSettings.Add(Settings.Keys.LogEnabled, true);
			AssertAdministrativeFolderFailTest(Settings.Keys.LoggingFolder, absolutePath, stageRootFolder, givenStageName, additionalSettings);
		}


		[Theory(DisplayName = "Test of the CLI property to control the stage versions")]
		[InlineData(2)]
		[InlineData(4)]
		[InlineData(1)] // No last folder assumed
		public void StageVersionTest(int versions)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();

			// Create test settings with basic override
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageVersions, versions);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			List<string> excludedFolders = new List<string>();
			excludedFolders.Add(testSettings.ConfigFolder);
			excludedFolders.Add(testSettings.TempFolder);
			excludedFolders.Add(testSettings.LoggingFolder);

			// Initial purge
			purgeController.ExecutePurge();

			string[] filesInRoot = Directory.GetDirectories(stageRootFolder);
			int counter = 0;
			foreach (string file in filesInRoot)
			{
				if (excludedFolders.Contains(file))
				{ continue; }
				counter++;
			}
			Assert.Equal(versions, counter);
		}

		[Theory(DisplayName = "Test of the failing CLI property to control the stage versions, when the number is zero or negative")]
		[InlineData(0)]
		[InlineData(-1)]
		[InlineData(-2)]
		[InlineData(-10)]
		public void StageVersionFailTest(int versions)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();

			// Create test settings with basic override
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageVersions, versions);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			purgeController.ExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}

		[Theory(DisplayName = "Test of the CLI property to control the stage name prefix")]
		[InlineData("purge-temp")]
		[InlineData("a")]
		[InlineData("1")]
		[InlineData(".test")]
		[InlineData(" test")]
		[InlineData("Test")]
		public void StageNamePrefixTest(string givenPrefix)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();

			// Create test settings with basic override
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, givenPrefix);
			testSettings.OverrideSetting(Settings.Keys.StageVersions, 3);
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
			testSettings.OverrideSetting(Settings.Keys.StageLastNameSuffix, "LAST");
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, true);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			// Initial purge
			purgeController.ExecutePurge();

			string expectedFolder1 = Path.Combine(stageRootFolder, givenPrefix + "-1");
			string expectedFolder2 = Path.Combine(stageRootFolder, givenPrefix + "-2");
			string expectedFolder3 = Path.Combine(stageRootFolder, givenPrefix + "-LAST");

			List<string> filesInRoot = Directory.GetDirectories(stageRootFolder).ToList<string>();
			Assert.Contains(expectedFolder1, filesInRoot);
			Assert.Contains(expectedFolder2, filesInRoot);
			Assert.Contains(expectedFolder3, filesInRoot);

		}

		[Theory(DisplayName = "Test of the failing CLI property to control the stage name prefix")]
		[InlineData("")]
		//[InlineData(null)] // is application error
		[InlineData("\\")]
		[InlineData("/")]
		[InlineData("*")]
		[InlineData("?")]
		[InlineData("|")]
		[InlineData("<")]
		[InlineData(">")]
		[InlineData("\"")]
		[InlineData(":")]
		[InlineData("CON")]
		[InlineData("NUL")]
		[InlineData("LPT1")]
		[InlineData("test.")]
		[InlineData("test ")]
		[InlineData(" ")]
		[InlineData(".")]
		public void StageNamePrefixFailTest(string givenPrefix)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();

			// Create test settings with basic override
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, givenPrefix);
			testSettings.OverrideSetting(Settings.Keys.StageVersions, 3);
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
			testSettings.OverrideSetting(Settings.Keys.StageLastNameSuffix, "LAST");
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, true);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			// Initial purge
			purgeController.ExecutePurge();

			string expectedFolder1 = Path.Combine(stageRootFolder, "purge-temp-1");
			string expectedFolder2 = Path.Combine(stageRootFolder, "purge-temp-2");
			string expectedFolder3 = Path.Combine(stageRootFolder, "purge-temp-LAST");

			List<string> filesInRoot = Directory.GetDirectories(stageRootFolder).ToList<string>();
			Assert.Contains(expectedFolder1, filesInRoot);
			Assert.Contains(expectedFolder2, filesInRoot);
			Assert.Contains(expectedFolder3, filesInRoot);

		}

		[Theory(DisplayName = "Test of the failing CLI property to control the stage name prefix for combined cases")]
		[InlineData("LPT", "", "LAST")] // Leads to LPT2
		[InlineData("COM", "", "LAST")] // Leads to COM2
		[InlineData("LP", "", "T1")] // Leads to LPT1
		[InlineData("NU", "", "L")] // Leads to NUL
		[InlineData("C", "", "ON")]// Leads to CON
		[InlineData("A", "U", "X")]// Leads to AUX
		[InlineData("LP", "T", "LAST")]// Leads to LPT2
		public void StageNamePrefixFailTest2(string givenPrefix, string givenDelimiter, string givenLastSuffix)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();

			// Create test settings with basic override
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, givenPrefix);
			testSettings.OverrideSetting(Settings.Keys.StageVersions, 3);
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, givenDelimiter);
			testSettings.OverrideSetting(Settings.Keys.StageLastNameSuffix, givenLastSuffix);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			// Initial purge
			purgeController.ExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}


		private static Settings ArrangeSettings(string stageRootFolder)
		{
			Dictionary<string, string> overrides = new Dictionary<string, string>
		{
			{ Settings.Keys.StageRootFolder, stageRootFolder },
			{ Settings.Keys.StagingDelaySeconds, "2" },
		};

			return TestSettingsProvider.GetSettings(stageRootFolder, overrides);
		}

		private void AssertAdministrativeFolderTest(string key, string value, string stageRootFolder, Dictionary<string, object> additionalSettings = null)
		{
			// Create test settings with basic override
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(key, value);
			if (additionalSettings != null)
			{
				foreach (var additionalSetting in additionalSettings)
				{
					testSettings.OverrideSetting(additionalSetting.Key, additionalSetting.Value);
				}
			}
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			// Initial purge
			purgeController.ExecutePurge();
			List<string> filesInRoot = Directory.GetDirectories(stageRootFolder).ToList<string>();
			Assert.Contains(value, filesInRoot);
		}


		private void AssertAdministrativeFolderFailTest(string key, string value, string stageRootFolder, string givenStageName, Dictionary<string, object> additionalSettings = null)
		{

			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(key, value);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, givenStageName);
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			if (additionalSettings != null)
			{
				foreach (var additionalSetting in additionalSettings)
				{
					testSettings.OverrideSetting(additionalSetting.Key, additionalSetting.Value);
				}
			}
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			// Initial purge
			purgeController.ExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}
	}
}

using Newtonsoft.Json.Linq;
using PurgeTemp.Controller;
using PurgeTemp.Logger;
using PurgeTemp.Utils;
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
		public void ConfigFolderFailTest(string givenConfigName, string givenStageName)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			AssertAdministrativeFolderFailTest(Settings.Keys.ConfigFolder, givenConfigName, stageRootFolder, givenStageName);
		}

		[Fact(DisplayName = "Test of the failing CLI property to control the name of the config folder on a protected system folder")]
		public void ConfigFolderFailTest2()
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string protectedTestFolder = PathUtils.TestSystemPath;
			AssertAdministrativeFolderFailTest(Settings.Keys.ConfigFolder, protectedTestFolder, stageRootFolder, "purge-temp");
		}

		[Theory(DisplayName = "Test of the failing CLI property to control the name of the config folder if it conflicts with the stage folders")]
		[InlineData("purge-temp-LAST", "purge-temp")]
		[InlineData("purge-temp", "purge-temp")]
		public void ConfigFolderFailTest3(string givenConfigName, string givenStageName)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string absolutePath = Path.Combine(stageRootFolder, givenConfigName);
			AssertAdministrativeFolderFailTest(Settings.Keys.ConfigFolder, absolutePath, stageRootFolder, givenStageName);
		}

		[Theory(DisplayName = "Test of the CLI property to control the number of logged files when a purge is performed")]
		[InlineData(-1, 1, 1)]
		[InlineData(-1, 5, 5)]
		[InlineData(0, 1, 1)]
		[InlineData(0, 4, 1)]
		[InlineData(1, 1, 1)]
		[InlineData(3, 3, 3)]
		[InlineData(3, 4, 4)]
		[InlineData(3, 5, 4)]
		[InlineData(4, 10, 5)]
		public void FileLogAmountThresholdTest(int? givenThreshold, int givenFiles, int expectedLogLines)
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
				TestFileUtils.CreateFile(stageFolder1, "file" + currentNumber + ".txt", "Content of file " + currentNumber);
			}
			Thread.Sleep(3000);
			purgeController.ExecutePurge();
			string[] files = Directory.GetFiles(expectedLogFolder, PurgeLogger.LOGFILE_NAME_TEMPLATE + "*");
			int logLines = TestFileUtils.CountLines(files[0]);
			Assert.Equal(expectedLogLines, logLines);
		}

		[Theory(DisplayName = "Test of the failing CLI property to control the number of logged files, when the number is smaller -1")]
		[InlineData(-2)]
		[InlineData(-5)]
		[InlineData(-10)]
		public void FileLogAmountThresholdFailTest(int threshold)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();

			// Create test settings with basic override
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.FileLogAmountThreshold, threshold);
            testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
            PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			purgeController.ExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}


		[Theory(DisplayName = "Test of the CLI property to enable logging of moved/purged files when a purge is performed")]
		[InlineData(false)]
		[InlineData(true)]
		public void LogAllFilesTest(bool logAllFilesEnabled)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string expectedLogFolder = Path.Combine(stageRootFolder, "log");
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			testSettings.OverrideSetting(Settings.Keys.LoggingFolder, expectedLogFolder);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			testSettings.OverrideSetting(Settings.Keys.LogEnabled, true);
			testSettings.OverrideSetting(Settings.Keys.LogAllFiles, logAllFilesEnabled);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 2);
			string stageFolder1 = Path.Combine(stageRootFolder, "purge-temp");
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// Initial purge
			purgeController.ExecutePurge();
			for (int i = 0; i < 5; i++)
			{
				string currentNumber = (i + 1).ToString();
				TestFileUtils.CreateFile(stageFolder1, "file" + currentNumber + ".txt", "Content of file " + currentNumber);
			}
			Thread.Sleep(3000);
			purgeController.ExecutePurge();
			string[] files = Directory.GetFiles(expectedLogFolder, PurgeLogger.LOGFILE_NAME_TEMPLATE + "*");
			if (logAllFilesEnabled)
			{
				Assert.True(files.Length > 0);
				int logLines = TestFileUtils.CountLines(files[0]);
				Assert.Equal(5, logLines);
			}
			else
			{
				Assert.True(files.Length == 0);
			}
		}

		[Theory(DisplayName = "Test of the CLI property to enable logging in general")]
		[InlineData(false)]
		[InlineData(true)]
		public void LogEnabledTest(bool logEnabled)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string expectedLogFolder = Path.Combine(stageRootFolder, "log");
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.LoggingFolder, expectedLogFolder);
			testSettings.OverrideSetting(Settings.Keys.LogEnabled, logEnabled);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// Initial purge
			purgeController.ExecutePurge();
			if (logEnabled)
			{
				Assert.True(Directory.Exists(expectedLogFolder));
				string[] files = Directory.GetFiles(expectedLogFolder);
				Assert.True(files.Length > 0);
			}
			else
			{
				Assert.False(Directory.Exists(expectedLogFolder));
			}
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
		public void LoggingFolderFailTest(string givenConfigName, string givenStageName)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Dictionary<string, object> additionalSettings = new Dictionary<string, object>();
			additionalSettings.Add(Settings.Keys.LogEnabled, true);
            AssertAdministrativeFolderFailTest(Settings.Keys.LoggingFolder, givenConfigName, stageRootFolder, givenStageName, additionalSettings);
		}

		[Fact(DisplayName = "Test of the failing CLI property to control the name of the logging folder, on a protected system, folder")]
		public void LoggingFolderFailTest2()
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string protectedTestFolder = PathUtils.TestSystemPath;
			Dictionary<string, object> additionalSettings = new Dictionary<string, object>();
			additionalSettings.Add(Settings.Keys.LogEnabled, true);
			AssertAdministrativeFolderFailTest(Settings.Keys.LoggingFolder, protectedTestFolder, stageRootFolder, "purge-temp", additionalSettings);
		}

		[Theory(DisplayName = "Test of the failing CLI property to control the name of the logging folder if it conflicts with the stage folders")]
		[InlineData("purge-temp-LAST", "purge-temp")]
		[InlineData("purge-temp", "purge-temp")]
		public void LoggingFolderFailTest3(string givenConfigName, string givenStageName)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string absolutePath = Path.Combine(stageRootFolder, givenConfigName);
			Dictionary<string, object> additionalSettings = new Dictionary<string, object>();
			additionalSettings.Add(Settings.Keys.LogEnabled, true);
			AssertAdministrativeFolderFailTest(Settings.Keys.LoggingFolder, absolutePath, stageRootFolder, givenStageName, additionalSettings);
		}

		[Theory(DisplayName = "Test of the failing CLI property to control the number of logfile bytes until a new logfile is created")]
		[InlineData(1, 0, 1)]
		[InlineData(10, 0, 1)]
		[InlineData(1 , 10, 1)]
		[InlineData(2 , 10 , 2)]
		[InlineData(6, 10, 6)]
		public void LogRotationBytesTest(int givenFileCount, int givenBytes, int expectedFiles)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string expectedLogFolder = Path.Combine(stageRootFolder, "log");
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			testSettings.OverrideSetting(Settings.Keys.LoggingFolder, expectedLogFolder);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
            testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
            testSettings.OverrideSetting(Settings.Keys.LogEnabled, true);
			testSettings.OverrideSetting(Settings.Keys.LogAllFiles, true);
			testSettings.OverrideSetting(Settings.Keys.LogRotationBytes, givenBytes);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 2);
			string stageFolder1 = Path.Combine(stageRootFolder, "purge-temp");
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// Initial purge
			purgeController.ExecutePurge();
			for (int i = 0; i < givenFileCount; i++)
			{
				string currentNumber = (i + 1).ToString();
				TestFileUtils.CreateFile(stageFolder1, "file" + currentNumber + ".txt", "Content of file " + currentNumber);
			}
			Thread.Sleep(3000);
			purgeController.ExecutePurge();
			string[] files = Directory.GetFiles(expectedLogFolder, PurgeLogger.LOGFILE_NAME_TEMPLATE + "*");
			Assert.Equal(expectedFiles, files.Length);
		}

		[Theory(DisplayName = "Test of the failing CLI property to control the number of logfile bytes until a new logfile is created, when the number is negative")]
		[InlineData(-1)]
		[InlineData(-2)]
		[InlineData(-5)]
		[InlineData(-10)]
		public void LogRotationBytesFailTest(int threshold)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();

			// Create test settings with basic override
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.LogRotationBytes, threshold);
            testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
            PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			purgeController.ExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}

		[Theory(DisplayName = "Test of the failing CLI property to control the number of logfiles to retain during the log rotation")]
		[InlineData(1, 1, 1)]
		[InlineData(10, 1, 1)]
		[InlineData(10, 3, 3)]
		[InlineData(5, 7, 5)]
		[InlineData(10, 0, 10)]
		public void LogRotationVersionsTest(int givenFileCount, int givenLogVersions, int expectedFiles)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string expectedLogFolder = Path.Combine(stageRootFolder, "log");
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			testSettings.OverrideSetting(Settings.Keys.LoggingFolder, expectedLogFolder);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			testSettings.OverrideSetting(Settings.Keys.LogEnabled, true);
			testSettings.OverrideSetting(Settings.Keys.LogAllFiles, true);
			testSettings.OverrideSetting(Settings.Keys.LogRotationBytes, 10);
			testSettings.OverrideSetting(Settings.Keys.LogRotationVersions, givenLogVersions);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 2);
			string stageFolder1 = Path.Combine(stageRootFolder, "purge-temp");
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// Initial purge
			purgeController.ExecutePurge();
			for (int i = 0; i < givenFileCount; i++)
			{
				string currentNumber = (i + 1).ToString();
				TestFileUtils.CreateFile(stageFolder1, "file" + currentNumber + ".txt", "Content of file " + currentNumber);
			}
			Thread.Sleep(3000);
			purgeController.ExecutePurge();
			string[] files = Directory.GetFiles(expectedLogFolder, PurgeLogger.LOGFILE_NAME_TEMPLATE + "*");
			Assert.Equal(expectedFiles, files.Length);
		}

		[Theory(DisplayName = "Test of the failing CLI property to control the number of logfiles to retain during the log rotation, when the number is negative")]
		[InlineData(-1)]
		[InlineData(-2)]
		[InlineData(-5)]
		[InlineData(-10)]
		public void LogRotationVersionsFailTest(int threshold)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();

			// Create test settings with basic override
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.LogRotationVersions, threshold);
            testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
            PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			purgeController.ExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}

		[Theory(DisplayName = "Test of the CLI property to set a valid logo file")]
		[InlineData("trashcan32.png", true)]
		[InlineData("notExistingFile.png", false)] // fallback icon expected
		public void PurgeMessageLogoFileTest(string fileName, bool existing)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			String logoFilePath;
			if (existing)
			{
				logoFilePath = TestFileUtils.CopyEmbeddedResourceToFolder(fileName, stageRootFolder);
			}
            else
            {
				logoFilePath = fileName;
			}

            // Create test settings with basic override
            Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, true);
			testSettings.OverrideSetting(Settings.Keys.PurgeMessageLogoFile, logoFilePath);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Thread.Sleep(1500);
			purgeController.ExecutePurge();
			// Only feasible test: No crash (and maybe the tester recognizes two popups)
			Assert.Equal(PurgeController.ExecutionEvaluation.CanExecute, purgeController.ExecutionState);
		}

        [Theory(DisplayName = "Test of the failing CLI property to set a valid logo file")]
        [InlineData("NUL")]
        [InlineData("<")]
        [InlineData("\t")]
        [InlineData("file ")]
        [InlineData("a.a. ")]
        public void PurgeMessageLogoFileFailTest(string givenLogoFileName)
        {
            // Arrange
            string stageRootFolder = fixture.CreateUniqueTempFolder();

            // Create test settings with basic override
            Settings testSettings = ArrangeSettings(stageRootFolder);
            testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, true);
            testSettings.OverrideSetting(Settings.Keys.PurgeMessageLogoFile, givenLogoFileName);

            PurgeController purgeController = Program.GetTestEnvironment(testSettings);
            purgeController.ExecutePurge();
            Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
        }

        [Theory(DisplayName = "Test of the CLI property to remove or keep empty stage folders")]
        [InlineData(false, 4, 1, 4)]
        [InlineData(false, 4, 2, 4)]
        [InlineData(true, 4, 1, 2)] // additional empty first folder
        [InlineData(true, 4, 2, 3)] // additional empty first folder
        [InlineData(true, 4, 4, 4)]
        public void RemoveEmptyStageFoldersTest(bool removeEmptyFolders, int versions, int iterationsWithFiles, int expectedFolderCount)
		{
            string stageRootFolder = fixture.CreateUniqueTempFolder();
            Settings testSettings = ArrangeSettings(stageRootFolder);
            testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, removeEmptyFolders);
            testSettings.OverrideSetting(Settings.Keys.StageVersions, versions);
            testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
            testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
            testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 2);
            PurgeController purgeController = Program.GetTestEnvironment(testSettings);
            string stageFolder1 = Path.Combine(stageRootFolder, "purge-temp");
            purgeController.ExecutePurge(); // Initial purge
            for (int i = 0; i < iterationsWithFiles; i++)
			{
                TestFileUtils.CreateFile(stageFolder1, "file.txt", "Content of file");
                Thread.Sleep(2500);
                purgeController.ExecutePurge();
            }
            string[] folders = Directory.GetDirectories(stageRootFolder, "purge-temp*");
			Assert.Equal(expectedFolderCount, folders.Length);
        }

        [Theory(DisplayName = "Test of the CLI property to show desktop notifications on purge")]
        [InlineData(true, "trashcan32.png")]
        [InlineData(false, "shouldNotBeShown.png")]
        public void ShowPurgeMessageTest(bool showNotification, string fileName)
        {
            // Arrange
            string stageRootFolder = fixture.CreateUniqueTempFolder();
            string logoFilePath = TestFileUtils.CopyEmbeddedResourceToFolder(fileName, stageRootFolder);
            if (showNotification)
            {
                
            }
            else
            {
                logoFilePath = fileName;
            }

            // Create test settings with basic override
            Settings testSettings = ArrangeSettings(stageRootFolder);
            testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, showNotification);
            testSettings.OverrideSetting(Settings.Keys.PurgeMessageLogoFile, logoFilePath);
            PurgeController purgeController = Program.GetTestEnvironment(testSettings);
            Thread.Sleep(1500);
            purgeController.ExecutePurge();
            // Only feasible test: No crash (and maybe the tester recognizes jus onepopup)
            Assert.Equal(PurgeController.ExecutionEvaluation.CanExecute, purgeController.ExecutionState);
        }

        [Theory(DisplayName = "Test of the CLI property to skip purges by a skip token (file)")]
        [InlineData(true, "a", 3, 4)]
        [InlineData(true, "skip.txt", 1, 2)]
        [InlineData(true, "xyz.abc", 0, 1)]
        [InlineData(false, "a", 3, 0)]
        [InlineData(false, "skip.txt", 1, 0)]
        [InlineData(false, "xyz.abc", 0, 0)]
        public void SkipTokenFileTest(bool givenAddToken, string givenTokenFile, int givenFilesInStage, int expectedFileCount)
        {
            // Arrange
            string stageRootFolder = fixture.CreateUniqueTempFolder();

            // Create test settings with basic override
            Settings testSettings = ArrangeSettings(stageRootFolder);
            testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
            testSettings.OverrideSetting(Settings.Keys.StageVersions, 1);
            testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
            testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
            testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			if (givenAddToken)
			{
                testSettings.OverrideSetting(Settings.Keys.SkipTokenFile, givenTokenFile);
            }
            PurgeController purgeController = Program.GetTestEnvironment(testSettings);

            // Initial purge
            purgeController.ExecutePurge();

            string expectedFolder1 = Path.Combine(stageRootFolder, "purge-temp-1");
			List<string> expectedFiles = new List<string>();
			for(int i = 1; i <= givenFilesInStage; i++)
			{
				string file =  TestFileUtils.CreateFile(expectedFolder1, "file" + i + ".txt", "Content of file");
				expectedFiles.Add(file);
            }
            string skipFile = TestFileUtils.CreateFile(expectedFolder1, givenTokenFile, "");
			expectedFiles.Add(skipFile);

            Thread.Sleep(1500);
            purgeController.ExecutePurge();

            List<string> filesInStage = Directory.GetFiles(expectedFolder1).ToList<string>();
			Assert.Equal(expectedFileCount, filesInStage.Count);
			foreach(string file in filesInStage)
			{
				Assert.Contains(file, expectedFiles);
			}
        }

        [Theory(DisplayName = "Test of the failing CLI property to skip purges by a skip token (file)")]
        [InlineData("NUL")]
        [InlineData("<")]
        [InlineData("\t")]
        [InlineData("file ")]
        [InlineData("a.a. ")]
        public void SkipTokenFileFailTest(string givenSkipTokenFileName)
        {
            // Arrange
            string stageRootFolder = fixture.CreateUniqueTempFolder();

            // Create test settings with basic override
            Settings testSettings = ArrangeSettings(stageRootFolder);
            testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
            testSettings.OverrideSetting(Settings.Keys.SkipTokenFile, givenSkipTokenFileName);

            PurgeController purgeController = Program.GetTestEnvironment(testSettings);
            purgeController.ExecutePurge();
            Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
        }

        [Theory(DisplayName = "Test of the CLI property to control the name suffix of the last stage folder")]
        [InlineData("a")]
        [InlineData("LAST")]
        [InlineData(" LAST")]
        [InlineData("-")]
        [InlineData("99")]
        public void StageLastNameSuffixTest(string givenSuffix)
        {
            // Arrange
            string stageRootFolder = fixture.CreateUniqueTempFolder();
            // Create test settings with basic override
            Settings testSettings = ArrangeSettings(stageRootFolder);
            testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
            testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
            testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
            testSettings.OverrideSetting(Settings.Keys.StageLastNameSuffix, givenSuffix);

            string expectedFolder = Path.Combine(stageRootFolder, "purge-temp-" + givenSuffix);
            PurgeController purgeController = Program.GetTestEnvironment(testSettings);
            purgeController.ExecutePurge();

            List<string> folders = Directory.GetDirectories(stageRootFolder).ToList<string>();
            Assert.Contains(expectedFolder, folders);
        }

        [Theory(DisplayName = "Test of the CLI property to control the name suffix of the last stage folder with invalid values (auto-sanitation)")]
        [InlineData("NUL")]
        [InlineData("<")]
        [InlineData("\t")]
        [InlineData("LAST ")]
        [InlineData(" ")]
        [InlineData("/")]
        [InlineData("a.a. ")]
        public void StageLastNameSuffixFailTest(string givenSuffix)
        {
            // Arrange
            string stageRootFolder = fixture.CreateUniqueTempFolder();

            Settings testSettings = ArrangeSettings(stageRootFolder);
            testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
            testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
            testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
            testSettings.OverrideSetting(Settings.Keys.StageLastNameSuffix, givenSuffix);

            PurgeController purgeController = Program.GetTestEnvironment(testSettings);
            purgeController.ExecutePurge();
            Assert.Equal(PurgeController.ExecutionEvaluation.CanExecute, purgeController.ExecutionState);
            string expectedFolder = Path.Combine(stageRootFolder, "purge-temp-LAST");
        }

		[Theory(DisplayName = "Test of the CLI property to control the name suffix of the last stage folder with invalid values (combind cases)")]
		[InlineData("NU","L")]
		[InlineData("LP", "T1")]
		[InlineData("P", "RN")]
		public void StageLastNameSuffixFailTest2(string givenPrefix, string givenSuffix)
		{
			// Arrange
			string stageRootFolder = fixture.CreateUniqueTempFolder();

			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, givenPrefix);
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "");
			testSettings.OverrideSetting(Settings.Keys.StageLastNameSuffix, givenSuffix);

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
			testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
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
			testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			// Initial purge
			purgeController.ExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}

		[Theory(DisplayName = "Test of the CLI property to control the stage versions")]
		[InlineData(2)]
		[InlineData(4)]
		[InlineData(1)] // No last folder assumed
		public void StageVersionsTest(int versions)
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
            testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
            PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			purgeController.ExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}

        [Theory(DisplayName = "Test of the CLI property to control the stage version delimiter")]
        [InlineData("-")]
        [InlineData("#")]
        [InlineData(" ")]
        [InlineData("")]
        public void StageVersionDelimiterTest(string delimiter)
		{
            // Arrange
            string stageRootFolder = fixture.CreateUniqueTempFolder();
            // Create test settings with basic override
            Settings testSettings = ArrangeSettings(stageRootFolder);
            testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, true);
            testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, delimiter);
            testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
            PurgeController purgeController = Program.GetTestEnvironment(testSettings);
            // Initial purge
            purgeController.ExecutePurge();
            string expectedFolder1 = Path.Combine(stageRootFolder, "purge-temp" + delimiter + "1");
            string expectedFolder2 = Path.Combine(stageRootFolder, "purge-temp" + delimiter + "2");
            string expectedFolder3 = Path.Combine(stageRootFolder, "purge-temp" + delimiter + "LAST");
            List<string> filesInRoot = Directory.GetDirectories(stageRootFolder).ToList<string>();
            Assert.Contains(expectedFolder1, filesInRoot);
            Assert.Contains(expectedFolder2, filesInRoot);
            Assert.Contains(expectedFolder3, filesInRoot);
        }

        [Theory(DisplayName = "Test of the CLI property to control the stage version delimiter when automatic sanitation is applied")]
        [InlineData("$")]
        [InlineData("\\")]
        [InlineData("<")]
        [InlineData("NUL")]
        [InlineData("LPT1")]
        public void StageVersionDelimiterAutoSanitizeTest(string delimiter)
        {
            // Arrange
            string stageRootFolder = fixture.CreateUniqueTempFolder();
            // Create test settings with basic override
            Settings testSettings = ArrangeSettings(stageRootFolder);
            testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
            testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, true);
            testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, delimiter);
            testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
            PurgeController purgeController = Program.GetTestEnvironment(testSettings);
            // Initial purge
            purgeController.ExecutePurge();
            string expectedFolder1 = Path.Combine(stageRootFolder, "purge-temp" + PathUtils.DEFAULT_DELIMITER + "1");
            string expectedFolder2 = Path.Combine(stageRootFolder, "purge-temp" + PathUtils.DEFAULT_DELIMITER + "2");
            string expectedFolder3 = Path.Combine(stageRootFolder, "purge-temp" + PathUtils.DEFAULT_DELIMITER + "LAST");
            List<string> filesInRoot = Directory.GetDirectories(stageRootFolder).ToList<string>();
            Assert.Contains(expectedFolder1, filesInRoot);
            Assert.Contains(expectedFolder2, filesInRoot);
            Assert.Contains(expectedFolder3, filesInRoot);
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
            testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
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

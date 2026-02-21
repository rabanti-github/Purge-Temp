using PurgeTemp;
using PurgeTemp.Controller;
using PurgeTemp.Utils;
using PurgeTempTest.Utils;

namespace PurgeTempTest
{
	public class PurgeControllerTest : IClassFixture<TempFolderFixture>
	{
		private readonly TempFolderFixture fixture;

		public PurgeControllerTest(TempFolderFixture fixture)
		{
			this.fixture = fixture;
		}

		// ========== ValidateGeneralSettings tests ==========

		[Fact(DisplayName = "Test that ValidateGeneralSettings succeeds with valid default settings")]
		public void ValidateGeneralSettingsSuccessTest()
		{
			Settings testSettings = ArrangeSettings(fixture.CreateUniqueTempFolder());
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Result result = purgeController.ValidateGeneralSettings();
			Assert.True(result.IsValid);
			Assert.Equal(ErrorCodes.Success, result.ErrorCode);
		}

		[Theory(DisplayName = "Test that ValidateGeneralSettings fails when StageVersions is below 1")]
		[InlineData(0)]
		[InlineData(-1)]
		[InlineData(-100)]
		public void ValidateGeneralSettingsStageVersionsFailTest(int givenVersions)
		{
			Settings testSettings = ArrangeSettings(fixture.CreateUniqueTempFolder());
			testSettings.OverrideSetting(Settings.Keys.StageVersions, givenVersions);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Result result = purgeController.ValidateGeneralSettings();
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.InvalidNumberOfStages, result.ErrorCode);
		}

		[Theory(DisplayName = "Test that ValidateGeneralSettings fails when FileLogAmountThreshold is below -1")]
		[InlineData(-2)]
		[InlineData(-10)]
		public void ValidateGeneralSettingsFileLogAmountFailTest(int givenThreshold)
		{
			Settings testSettings = ArrangeSettings(fixture.CreateUniqueTempFolder());
			testSettings.OverrideSetting(Settings.Keys.FileLogAmountThreshold, givenThreshold);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Result result = purgeController.ValidateGeneralSettings();
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.InvalidFileLogAmount, result.ErrorCode);
		}

		[Theory(DisplayName = "Test that ValidateGeneralSettings accepts valid FileLogAmountThreshold values")]
		[InlineData(-1)]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(100)]
		public void ValidateGeneralSettingsFileLogAmountValidTest(int givenThreshold)
		{
			Settings testSettings = ArrangeSettings(fixture.CreateUniqueTempFolder());
			testSettings.OverrideSetting(Settings.Keys.FileLogAmountThreshold, givenThreshold);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Result result = purgeController.ValidateGeneralSettings();
			Assert.True(result.IsValid);
		}

		[Theory(DisplayName = "Test that ValidateGeneralSettings fails when LogRotationBytes is negative")]
		[InlineData(-1)]
		[InlineData(-100)]
		public void ValidateGeneralSettingsLogRotationBytesFailTest(int givenBytes)
		{
			Settings testSettings = ArrangeSettings(fixture.CreateUniqueTempFolder());
			testSettings.OverrideSetting(Settings.Keys.LogRotationBytes, givenBytes);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Result result = purgeController.ValidateGeneralSettings();
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.InvalidLogRotationBytes, result.ErrorCode);
		}

		[Theory(DisplayName = "Test that ValidateGeneralSettings fails when LogRotationVersions is negative")]
		[InlineData(-1)]
		[InlineData(-50)]
		public void ValidateGeneralSettingsLogRotationVersionsFailTest(int givenVersions)
		{
			Settings testSettings = ArrangeSettings(fixture.CreateUniqueTempFolder());
			testSettings.OverrideSetting(Settings.Keys.LogRotationVersions, givenVersions);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Result result = purgeController.ValidateGeneralSettings();
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.InvalidLogRotationVersions, result.ErrorCode);
		}

		[Theory(DisplayName = "Test that ValidateGeneralSettings fails when PurgeMessageLogoFile has invalid name and ShowPurgeMessage is true")]
		[InlineData("NUL")]
		[InlineData("<")]
		[InlineData("\t")]
		public void ValidateGeneralSettingsInvalidLogoFileFailTest(string givenLogoFile)
		{
			Settings testSettings = ArrangeSettings(fixture.CreateUniqueTempFolder());
			testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, true);
			testSettings.OverrideSetting(Settings.Keys.PurgeMessageLogoFile, givenLogoFile);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Result result = purgeController.ValidateGeneralSettings();
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.InvalidPurgeMessageLogoFile, result.ErrorCode);
		}

		[Fact(DisplayName = "Test that ValidateGeneralSettings does not validate logo file when ShowPurgeMessage is false")]
		public void ValidateGeneralSettingsLogoFileSkippedWhenNotShownTest()
		{
			Settings testSettings = ArrangeSettings(fixture.CreateUniqueTempFolder());
			testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
			testSettings.OverrideSetting(Settings.Keys.PurgeMessageLogoFile, "NUL");
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Result result = purgeController.ValidateGeneralSettings();
			Assert.True(result.IsValid);
		}

		[Theory(DisplayName = "Test that ValidateGeneralSettings fails when SkipTokenFile has invalid name")]
		[InlineData("NUL")]
		[InlineData("<")]
		[InlineData("\t")]
		public void ValidateGeneralSettingsInvalidSkipTokenFailTest(string givenSkipToken)
		{
			Settings testSettings = ArrangeSettings(fixture.CreateUniqueTempFolder());
			testSettings.OverrideSetting(Settings.Keys.SkipTokenFile, givenSkipToken);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Result result = purgeController.ValidateGeneralSettings();
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.InvalidSkipTokenFile, result.ErrorCode);
		}

		// ========== MaintainAdministrativeFolders tests ==========

		[Fact(DisplayName = "Test that MaintainAdministrativeFolders succeeds with valid settings")]
		public void MaintainAdministrativeFoldersSuccessTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// ValidateGeneralSettings must pass first
			Result validateResult = purgeController.ValidateGeneralSettings();
			Assert.True(validateResult.IsValid);
			Result result = purgeController.MaintainAdministrativeFolders();
			Assert.True(result.IsValid);
			Assert.Equal(ErrorCodes.Success, result.ErrorCode);
		}

		[Fact(DisplayName = "Test that MaintainAdministrativeFolders creates config and temp folders")]
		public void MaintainAdministrativeFoldersCreatesDirectoriesTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			string configFolder = Path.Combine(stageRootFolder, "config");
			string tempFolder = Path.Combine(stageRootFolder, "temp");
			testSettings.OverrideSetting(Settings.Keys.ConfigFolder, configFolder);
			testSettings.OverrideSetting(Settings.Keys.TempFolder, tempFolder);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.MaintainAdministrativeFolders();
			Assert.True(Directory.Exists(configFolder));
			Assert.True(Directory.Exists(tempFolder));
		}

		[Fact(DisplayName = "Test that MaintainAdministrativeFolders creates log folder when logging is enabled")]
		public void MaintainAdministrativeFoldersCreatesLogFolderTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			string logFolder = Path.Combine(stageRootFolder, "log-test");
			testSettings.OverrideSetting(Settings.Keys.LogEnabled, true);
			testSettings.OverrideSetting(Settings.Keys.LoggingFolder, logFolder);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.MaintainAdministrativeFolders();
			Assert.True(Directory.Exists(logFolder));
		}

		[Fact(DisplayName = "Test that MaintainAdministrativeFolders does not create log folder when logging is disabled")]
		public void MaintainAdministrativeFoldersNoLogFolderWhenDisabledTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			string logFolder = Path.Combine(stageRootFolder, "log-test");
			testSettings.OverrideSetting(Settings.Keys.LogEnabled, false);
			testSettings.OverrideSetting(Settings.Keys.LoggingFolder, logFolder);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.MaintainAdministrativeFolders();
			Assert.False(Directory.Exists(logFolder));
		}

		[Fact(DisplayName = "Test that MaintainAdministrativeFolders fails when config folder conflicts with stage folder")]
		public void MaintainAdministrativeFoldersConfigConflictTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			// Set config folder to conflict with stage folder name
			string conflictingFolder = Path.Combine(stageRootFolder, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.ConfigFolder, conflictingFolder);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Result result = purgeController.MaintainAdministrativeFolders();
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.AdministrativePathConflictsWithStagePath, result.ErrorCode);
		}

		[Fact(DisplayName = "Test that MaintainAdministrativeFolders fails when logging folder has invalid name")]
		public void MaintainAdministrativeFoldersInvalidLogFolderTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.LogEnabled, true);
			testSettings.OverrideSetting(Settings.Keys.LoggingFolder, "NUL");
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Result result = purgeController.MaintainAdministrativeFolders();
			Assert.True(result.IsNotValid);
		}

		// ========== CanExecutePurge tests ==========

		[Fact(DisplayName = "Test that CanExecutePurge returns CanExecute on first run with valid settings")]
		public void CanExecutePurgeFirstRunTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// Setup required folders first
			purgeController.MaintainAdministrativeFolders();
			PurgeController.ExecutionEvaluation result = purgeController.CanExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.CanExecute, result);
		}

		[Fact(DisplayName = "Test that CanExecutePurge returns TimeSinceLastPurgeTooShort when called too soon after a purge")]
		public void CanExecutePurgeTooSoonTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 10);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// Execute initial purge to write the token
			purgeController.ExecutePurge();
			// Immediately check again without waiting
			PurgeController.ExecutionEvaluation result = purgeController.CanExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.TimeSinceLastPurgeTooShort, result);
		}

		[Fact(DisplayName = "Test that CanExecutePurge returns CanExecute when enough time has passed")]
		public void CanExecutePurgeAfterDelayTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.ExecutePurge();
			Thread.Sleep(1500);
			PurgeController.ExecutionEvaluation result = purgeController.CanExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.CanExecute, result);
		}

		[Fact(DisplayName = "Test that CanExecutePurge returns SkippedByToken when skip token file is present")]
		public void CanExecutePurgeSkipTokenTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			testSettings.OverrideSetting(Settings.Keys.SkipTokenFile, "SKIP.txt");
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// Execute initial purge to create stage folders
			purgeController.ExecutePurge();
			// Create skip token in the initial stage folder
			string stageFolder = Path.Combine(stageRootFolder, "purge-temp");
			TestFileUtils.CreateFile(stageFolder, "SKIP.txt", "");
			Thread.Sleep(1500);
			PurgeController.ExecutionEvaluation result = purgeController.CanExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.SkippedByToken, result);
		}

		[Fact(DisplayName = "Test that CanExecutePurge returns InvalidArguments when timestamp file has invalid format")]
		public void CanExecutePurgeInvalidTimestampTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
			string configFolder = Path.Combine(stageRootFolder, "config");
			testSettings.OverrideSetting(Settings.Keys.ConfigFolder, configFolder);
			testSettings.OverrideSetting(Settings.Keys.StagingTimestampFile, "last-purge.txt");
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// Execute initial purge to create config folder and token
			purgeController.ExecutePurge();
			// The timestamp file is at configFolder/last-purge.txt
			string timestampFile = Path.Combine(configFolder, "last-purge.txt");
			Assert.True(File.Exists(timestampFile), "Timestamp file should exist after purge");
			// Overwrite with invalid content
			File.WriteAllText(timestampFile, "not-a-valid-timestamp");
			PurgeController.ExecutionEvaluation result = purgeController.CanExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, result);
		}

		// ========== WriteLastPurgeToken tests ==========

		[Fact(DisplayName = "Test that WriteLastPurgeToken succeeds and creates a timestamp file")]
		public void WriteLastPurgeTokenSuccessTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// Create config folder first
			purgeController.MaintainAdministrativeFolders();
			Result result = purgeController.WriteLastPurgeToken();
			Assert.True(result.IsValid);
			Assert.Equal(ErrorCodes.Success, result.ErrorCode);
		}

		[Fact(DisplayName = "Test that WriteLastPurgeToken writes a parseable timestamp")]
		public void WriteLastPurgeTokenContentTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			string configFolder = Path.Combine(stageRootFolder, "config");
			testSettings.OverrideSetting(Settings.Keys.ConfigFolder, configFolder);
			testSettings.OverrideSetting(Settings.Keys.StagingTimestampFile, "last-purge.txt");
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.MaintainAdministrativeFolders();
			purgeController.WriteLastPurgeToken();
			string timestampFile = Path.Combine(configFolder, "last-purge.txt");
			Assert.True(File.Exists(timestampFile));
			string content = File.ReadAllText(timestampFile);
			bool parsed = DateTime.TryParseExact(content, testSettings.TimeStampFormat,
				null, System.Globalization.DateTimeStyles.None, out DateTime parsedTime);
			Assert.True(parsed);
			// Timestamp should be recent (within last 10 seconds)
			Assert.True((DateTime.Now - parsedTime).TotalSeconds < 10);
		}

		[Fact(DisplayName = "Test that WriteLastPurgeToken fails when config folder path is invalid")]
		public void WriteLastPurgeTokenFailTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			// Use an invalid path that File.WriteAllText cannot write to (invalid characters)
			testSettings.OverrideSetting(Settings.Keys.ConfigFolder, "Z:\\nonexistent\\invalid<>path");
			testSettings.OverrideSetting(Settings.Keys.StagingTimestampFile, "token.txt");
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Result result = purgeController.WriteLastPurgeToken();
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.CouldNotCreateLastPurgeToken, result.ErrorCode);
		}

		// ========== ExecutePurge integration tests ==========

		[Fact(DisplayName = "Test that ExecutePurge returns success error code on valid first execution")]
		public void ExecutePurgeSuccessTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			int errorCode = purgeController.ExecutePurge();
			Assert.Equal(ErrorCodes.Success, errorCode);
			Assert.Equal(PurgeController.ExecutionEvaluation.CanExecute, purgeController.ExecutionState);
		}

		[Fact(DisplayName = "Test that ExecutePurge returns InvalidArguments state when settings are invalid")]
		public void ExecutePurgeInvalidSettingsTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageVersions, -1);
			testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			int errorCode = purgeController.ExecutePurge();
			Assert.Equal(ErrorCodes.InvalidNumberOfStages, errorCode);
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}

		[Fact(DisplayName = "Test that ExecutePurge returns ExecutionTooFrequent when called too soon")]
		public void ExecutePurgeTooFrequentTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 60);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.ExecutePurge();
			// Immediately call again
			int errorCode = purgeController.ExecutePurge();
			Assert.Equal(ErrorCodes.ExecutionTooFrequent, errorCode);
			Assert.Equal(PurgeController.ExecutionEvaluation.TimeSinceLastPurgeTooShort, purgeController.ExecutionState);
		}

		[Fact(DisplayName = "Test that ExecutePurge returns SkipTokenFound when skip token is present")]
		public void ExecutePurgeSkipTokenFoundTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			testSettings.OverrideSetting(Settings.Keys.SkipTokenFile, "SKIP.txt");
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.ExecutePurge();
			string stageFolder = Path.Combine(stageRootFolder, "purge-temp");
			TestFileUtils.CreateFile(stageFolder, "SKIP.txt", "");
			Thread.Sleep(1500);
			int errorCode = purgeController.ExecutePurge();
			Assert.Equal(ErrorCodes.SkipTokenFound, errorCode);
			Assert.Equal(PurgeController.ExecutionEvaluation.SkippedByToken, purgeController.ExecutionState);
		}

		[Fact(DisplayName = "Test that ExecutePurge returns error when admin folder definition conflicts with stage folders")]
		public void ExecutePurgeAdminFolderConflictTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
			// Make config folder conflict with a stage folder
			string conflictingFolder = Path.Combine(stageRootFolder, "purge-temp-LAST");
			testSettings.OverrideSetting(Settings.Keys.ConfigFolder, conflictingFolder);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			int errorCode = purgeController.ExecutePurge();
			Assert.Equal(ErrorCodes.AdministrativePathConflictsWithStagePath, errorCode);
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}

		[Theory(DisplayName = "Test that ExecutePurge returns error when stage folder name resolves to a reserved Windows name")]
		[InlineData("NU", "", "L")]
		[InlineData("CO", "", "N")]
		[InlineData("A", "U", "X")]
		public void ExecutePurgeReservedStageFolderTest(string givenPrefix, string givenDelimiter, string givenLastSuffix)
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, givenPrefix);
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, givenDelimiter);
			testSettings.OverrideSetting(Settings.Keys.StageLastNameSuffix, givenLastSuffix);
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			int errorCode = purgeController.ExecutePurge();
			Assert.NotEqual(ErrorCodes.Success, errorCode);
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}

		[Fact(DisplayName = "Test that ExecutePurge creates stage folders on success")]
		public void ExecutePurgeCreatesStageFoldersTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
			testSettings.OverrideSetting(Settings.Keys.StageVersions, 3);
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, true);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.ExecutePurge();
			string folder1 = Path.Combine(stageRootFolder, "purge-temp-1");
			string folder2 = Path.Combine(stageRootFolder, "purge-temp-2");
			string folderLast = Path.Combine(stageRootFolder, "purge-temp-LAST");
			Assert.True(Directory.Exists(folder1));
			Assert.True(Directory.Exists(folder2));
			Assert.True(Directory.Exists(folderLast));
		}

		[Fact(DisplayName = "Test that ExecutePurge correctly rotates files through stages")]
		public void ExecutePurgeRotatesFilesTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
			testSettings.OverrideSetting(Settings.Keys.StageVersions, 2);
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// Initial purge creates folders
			purgeController.ExecutePurge();
			string stageFolder1 = Path.Combine(stageRootFolder, "purge-temp");
			string stageFolderLast = Path.Combine(stageRootFolder, "purge-temp-LAST");
			// Add a file to stage 1
			TestFileUtils.CreateFile(stageFolder1, "rotate-test.txt", "test content");
			Thread.Sleep(1500);
			// Second purge should move file to last stage
			purgeController.ExecutePurge();
			Assert.Empty(Directory.GetFiles(stageFolder1));
			Assert.Contains(Path.Combine(stageFolderLast, "rotate-test.txt"), Directory.GetFiles(stageFolderLast));
		}

		[Fact(DisplayName = "Test that ExecutePurge deletes files from the last stage")]
		public void ExecutePurgeDeletesLastStageFilesTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
			testSettings.OverrideSetting(Settings.Keys.StageVersions, 2);
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.ExecutePurge();
			string stageFolder1 = Path.Combine(stageRootFolder, "purge-temp");
			TestFileUtils.CreateFile(stageFolder1, "delete-test.txt", "content");
			Thread.Sleep(1500);
			purgeController.ExecutePurge();
			// File is now in LAST stage
			Thread.Sleep(1500);
			purgeController.ExecutePurge();
			// File should be purged from LAST stage
			string stageFolderLast = Path.Combine(stageRootFolder, "purge-temp-LAST");
			Assert.Empty(Directory.GetFiles(stageFolderLast));
		}

		[Fact(DisplayName = "Test that ExecutePurge returns the correct error code for each validation failure type")]
		public void ExecutePurgeValidationErrorCodeTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.LogRotationBytes, -5);
			testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			int errorCode = purgeController.ExecutePurge();
			Assert.Equal(ErrorCodes.InvalidLogRotationBytes, errorCode);
		}

		[Fact(DisplayName = "Test that ExecutionState is set correctly after successful purge")]
		public void ExecutionStateAfterSuccessTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.ExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.CanExecute, purgeController.ExecutionState);
		}

		[Fact(DisplayName = "Test that ExecutePurge handles consecutive purges correctly")]
		public void ExecutePurgeConsecutiveCallsTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			int firstResult = purgeController.ExecutePurge();
			Assert.Equal(ErrorCodes.Success, firstResult);
			Thread.Sleep(1500);
			int secondResult = purgeController.ExecutePurge();
			Assert.Equal(ErrorCodes.Success, secondResult);
			Thread.Sleep(1500);
			int thirdResult = purgeController.ExecutePurge();
			Assert.Equal(ErrorCodes.Success, thirdResult);
		}

		// ========== Targeted coverage tests for defensive branches ==========

		[Fact(DisplayName = "Test that ExecutePurge hits the InvalidArguments switch case when timestamp is corrupt")]
		public void ExecutePurgeInvalidArgumentsSwitchCaseTest()
		{
			// Covers line 81: switch case ExecutionEvaluation.InvalidArguments in ExecutePurge
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
			string configFolder = Path.Combine(stageRootFolder, "config");
			testSettings.OverrideSetting(Settings.Keys.ConfigFolder, configFolder);
			testSettings.OverrideSetting(Settings.Keys.StagingTimestampFile, "last-purge.txt");
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// First purge succeeds and writes a valid timestamp
			int firstResult = purgeController.ExecutePurge();
			Assert.Equal(ErrorCodes.Success, firstResult);
			// Corrupt the timestamp file
			string timestampFile = Path.Combine(configFolder, "last-purge.txt");
			File.WriteAllText(timestampFile, "not-a-valid-timestamp");
			// Second purge should hit the InvalidArguments case in the switch
			int secondResult = purgeController.ExecutePurge();
			Assert.Equal(ErrorCodes.InvalidArguments, secondResult);
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}

		[Fact(DisplayName = "Test that ExecutePurge returns CouldNotDeleteLastFolder when last stage folder is locked")]
		public void ExecutePurgeCouldNotDeleteLastFolderTest()
		{
			// Covers line 138: catch block for Directory.Delete failure
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
			testSettings.OverrideSetting(Settings.Keys.StageVersions, 2);
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.ExecutePurge();
			// Place a file in stage 1 so the LAST folder gets content after rotation
			string stageFolder1 = Path.Combine(stageRootFolder, "purge-temp");
			TestFileUtils.CreateFile(stageFolder1, "locked-file.txt", "content");
			Thread.Sleep(1500);
			purgeController.ExecutePurge();
			// Now locked-file.txt is in LAST folder. Lock a file in it to prevent deletion.
			string lastFolder = Path.Combine(stageRootFolder, "purge-temp-LAST");
			string lockedFilePath = Path.Combine(lastFolder, "locked-file.txt");
			using (FileStream fs = new FileStream(lockedFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				Thread.Sleep(1500);
				int errorCode = purgeController.ExecutePurge();
				Assert.Equal(ErrorCodes.CouldNotDeleteLastFolder, errorCode);
			}
		}

		[Fact(DisplayName = "Test that ExecutePurge returns CouldNotRenameStageFolder when folder rename is blocked")]
		public void ExecutePurgeCouldNotRenameStageFolderTest()
		{
			// Covers line 160: catch block for Directory.Move failure
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.StageVersionDelimiter, "-");
			testSettings.OverrideSetting(Settings.Keys.StageVersions, 3);
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, true);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.ExecutePurge();
			// Place a file in stage 1 so it has content to move
			string stageFolder1 = Path.Combine(stageRootFolder, "purge-temp-1");
			string lockedFilePath = TestFileUtils.CreateFile(stageFolder1, "block-rename.txt", "content");
			// Lock a file in stage 1 to prevent Directory.Move from succeeding
			using (FileStream fs = new FileStream(lockedFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				Thread.Sleep(1500);
				int errorCode = purgeController.ExecutePurge();
				Assert.Equal(ErrorCodes.CouldNotRenameStageFolder, errorCode);
			}
		}

		[Fact(DisplayName = "Test that ExecutePurge hits OtherErrors switch case when CanExecutePurge throws")]
		public void ExecutePurgeOtherErrorsSwitchCaseTest()
		{
			// Covers line 96: switch case ExecutionEvaluation.OtherErrors in ExecutePurge
			// Locking the timestamp file causes File.ReadAllText in CanExecutePurge to throw,
			// which is caught by its catch block returning OtherErrors
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			string configFolder = Path.Combine(stageRootFolder, "config");
			testSettings.OverrideSetting(Settings.Keys.ConfigFolder, configFolder);
			testSettings.OverrideSetting(Settings.Keys.StagingTimestampFile, "last-purge.txt");
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, false);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// First purge succeeds and writes a valid timestamp
			int firstResult = purgeController.ExecutePurge();
			Assert.Equal(ErrorCodes.Success, firstResult);
			Thread.Sleep(1500);
			// Lock the timestamp file so File.ReadAllText in CanExecutePurge throws
			string timestampFile = Path.Combine(configFolder, "last-purge.txt");
			using (FileStream fs = new FileStream(timestampFile, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				int secondResult = purgeController.ExecutePurge();
				Assert.Equal(ErrorCodes.UnknownError, secondResult);
				Assert.Equal(PurgeController.ExecutionEvaluation.OtherErrors, purgeController.ExecutionState);
			}
		}

		// ========== RemoveEmptyStageFolders tests ==========

		[Fact(DisplayName = "Test that RemoveEmptyStageFolders preserves stage folder containing only a subdirectory")]
		public void RemoveEmptyStageFoldersKeepsFolderWithSubdirectoryTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, true);
			testSettings.OverrideSetting(Settings.Keys.StageVersions, 2);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// Initial purge creates folders
			purgeController.ExecutePurge();
			string stageFolder1 = Path.Combine(stageRootFolder, "purge-temp");
			string stageFolderLast = Path.Combine(stageRootFolder, "purge-temp-LAST");
			// Place a file in a subdirectory within stage 1
			string subDir = Path.Combine(stageFolder1, "subfolder");
			Directory.CreateDirectory(subDir);
			File.WriteAllText(Path.Combine(subDir, "nested-file.txt"), "content");
			Thread.Sleep(1500);
			// Second purge rotates stage 1 → LAST; LAST now has the subdirectory
			purgeController.ExecutePurge();
			// The LAST folder should still exist because it contains a subdirectory with a file
			Assert.True(Directory.Exists(stageFolderLast));
			Assert.True(File.Exists(Path.Combine(stageFolderLast, "subfolder", "nested-file.txt")));
		}

		[Fact(DisplayName = "Test that RemoveEmptyStageFolders removes truly empty stage folders")]
		public void RemoveEmptyStageFoldersDeletesEmptyFolderTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			testSettings.OverrideSetting(Settings.Keys.RemoveEmptyStageFolders, true);
			testSettings.OverrideSetting(Settings.Keys.StageVersions, 3);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, "purge-temp");
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, true);
			testSettings.OverrideSetting(Settings.Keys.StagingDelaySeconds, 1);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			// Initial purge creates all 3 stage folders
			purgeController.ExecutePurge();
			// Place a file only in stage 1
			string stageFolder1 = Path.Combine(stageRootFolder, "purge-temp-1");
			TestFileUtils.CreateFile(stageFolder1, "test.txt", "content");
			Thread.Sleep(1500);
			// Second purge: stage 1 (with file) → stage 2, LAST was empty → deleted, stage 1 recreated
			purgeController.ExecutePurge();
			// Stage 2 should exist (has the file), LAST should have been removed (was empty)
			string stageFolder2 = Path.Combine(stageRootFolder, "purge-temp-2");
			string stageFolderLast = Path.Combine(stageRootFolder, "purge-temp-LAST");
			Assert.True(Directory.Exists(stageFolder2));
			Assert.False(Directory.Exists(stageFolderLast));
		}

		// ========== Targeted coverage tests for uncovered branches (batch 2) ==========

		[Fact(DisplayName = "Test that MaintainAdministrativeFolders fails when TempFolder resolves to a system-relevant path")]
		public void MaintainAdministrativeFoldersTempFolderIsSystemPathTest()
		{
			// Covers line 270: tempFolderValidation.IsNotValid in MaintainAdministrativeFolders
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			// Config folder is valid and in the test temp folder
			string configFolder = Path.Combine(stageRootFolder, "config");
			testSettings.OverrideSetting(Settings.Keys.ConfigFolder, configFolder);
			// Set TempFolder to the test system path so it fails system directory check
			testSettings.OverrideSetting(Settings.Keys.TempFolder, PathUtils.TestSystemPath);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			Result result = purgeController.MaintainAdministrativeFolders();
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.PathIsSystemDirectory, result.ErrorCode);
		}

		[Fact(DisplayName = "Test that CheckStageFolders fails when a stage folder resolves to a system-relevant path")]
		public void ExecutePurgeStageFolderIsSystemDirectoryTest()
		{
			// Covers line 331: stageValidation.ErrorCode == ErrorCodes.PathIsSystemDirectory in CheckStageFolders
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			// Admin folders use the test temp folder (valid)
			string configFolder = Path.Combine(stageRootFolder, "config");
			string tempFolder = Path.Combine(stageRootFolder, "temp");
			testSettings.OverrideSetting(Settings.Keys.ConfigFolder, configFolder);
			testSettings.OverrideSetting(Settings.Keys.TempFolder, tempFolder);
			testSettings.OverrideSetting(Settings.Keys.LogEnabled, false);
			testSettings.OverrideSetting(Settings.Keys.ShowPurgeMessage, false);
			// Set stage root to C:\ and prefix to the test system path name component
			// so the stage folder resolves to the full TestSystemPath
			string testSystemPath = PathUtils.TestSystemPath;
			string parentOfSystemPath = Path.GetDirectoryName(testSystemPath)!;
			string systemPathLastComponent = Path.GetFileName(testSystemPath);
			testSettings.OverrideSetting(Settings.Keys.StageRootFolder, parentOfSystemPath);
			testSettings.OverrideSetting(Settings.Keys.StageNamePrefix, systemPathLastComponent);
			testSettings.OverrideSetting(Settings.Keys.StageVersions, 1);
			testSettings.OverrideSetting(Settings.Keys.AppendNumberOnFirstStage, false);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			int errorCode = purgeController.ExecutePurge();
			Assert.Equal(ErrorCodes.StageFolderIsSystemDirectory, errorCode);
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, purgeController.ExecutionState);
		}

		[Fact(DisplayName = "Test that CanExecutePurge returns InvalidArguments when path arguments resolve to null")]
		public void CanExecutePurgeNullPathsTest()
		{
			// Covers line 365: null path check in CanExecutePurge
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			Settings testSettings = ArrangeSettings(stageRootFolder);
			// Set both ConfigFolder and StagingTimestampFile to empty so GetPath("","") returns null
			testSettings.OverrideSetting(Settings.Keys.ConfigFolder, "");
			testSettings.OverrideSetting(Settings.Keys.StagingTimestampFile, "");
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			PurgeController.ExecutionEvaluation result = purgeController.CanExecutePurge();
			Assert.Equal(PurgeController.ExecutionEvaluation.InvalidArguments, result);
		}

		// ========== Program API tests ==========

		[Fact(DisplayName = "Test that GetTestEnvironment with null settings returns a valid PurgeController")]
		public void GetTestEnvironmentWithNullSettingsReturnsControllerTest()
		{
			PurgeController purgeController = Program.GetTestEnvironment(null, Array.Empty<string>());
			Assert.NotNull(purgeController);
		}

		// ========== Helper methods ==========

		private static Settings ArrangeSettings(string stageRootFolder)
		{
			Dictionary<string, string> overrides = new Dictionary<string, string>
			{
				{ Settings.Keys.StageRootFolder, stageRootFolder },
				{ Settings.Keys.StagingDelaySeconds, "2" },
			};
			return TestSettingsProvider.GetSettings(stageRootFolder, overrides);
		}
	}
}

using PurgeTemp;
using PurgeTemp.Controller;
using PurgeTemp.Interface;
using PurgeTemp.Utils;
using PurgeTempTest.Utils;

namespace PurgeTempTest
{
	public class PathUtilsTest : IClassFixture<TempFolderFixture>
	{
		private readonly TempFolderFixture fixture;
		private readonly ILoggerFactory nullLoggerFactory = new NullLoggerFactory();

		public PathUtilsTest(TempFolderFixture fixture)
		{
			this.fixture = fixture;
		}

		// ========== Static members / constants tests ==========

		[Fact(DisplayName = "Test that ReservedNames contains all expected Windows reserved device names")]
		public void ReservedNamesContainsExpectedValuesTest()
		{
			Assert.Contains("NUL", PathUtils.ReservedNames);
			Assert.Contains("CON", PathUtils.ReservedNames);
			Assert.Contains("AUX", PathUtils.ReservedNames);
			Assert.Contains("PRN", PathUtils.ReservedNames);
			Assert.Contains("COM1", PathUtils.ReservedNames);
			Assert.Contains("LPT9", PathUtils.ReservedNames);
		}

		[Fact(DisplayName = "Test that ReservedNames lookup is case-insensitive")]
		public void ReservedNamesCaseInsensitiveTest()
		{
			Assert.Contains("nul", PathUtils.ReservedNames);
			Assert.Contains("con", PathUtils.ReservedNames);
			Assert.Contains("Aux", PathUtils.ReservedNames);
		}

		[Fact(DisplayName = "Test that TestSystemPath is a non-empty string that resolves under C:\\")]
		public void TestSystemPathIsValidStringTest()
		{
			Assert.False(string.IsNullOrEmpty(PathUtils.TestSystemPath));
			Assert.StartsWith("C:\\", PathUtils.TestSystemPath);
		}

		[Fact(DisplayName = "Test that default constants have expected values")]
		public void DefaultConstantsHaveCorrectValuesTest()
		{
			Assert.Equal("-", PathUtils.DEFAULT_DELIMITER);
			Assert.Equal("purge-temp", PathUtils.DEFAULT_TEMP_FOLDER_NAME);
			Assert.Equal("LAST", PathUtils.DEFAULT_LAST_FOLDER_NAME_TOKEN);
		}

		// ========== GetPath (two-arg) tests ==========

		[Theory(DisplayName = "Test that GetPath returns null when both arguments are null or empty")]
		[InlineData(null, null)]
		[InlineData("", "")]
		[InlineData(null, "")]
		[InlineData("", null)]
		public void GetPathBothEmptyOrNullReturnsNullTest(string basePath, string token)
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			string result = sut.GetPath(basePath, token);
			Assert.Null(result);
		}

		[Fact(DisplayName = "Test that GetPath returns basePath when token is empty")]
		public void GetPathEmptyTokenReturnsBasePathTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			string result = sut.GetPath(@"C:\some\base", "");
			Assert.Equal(@"C:\some\base", result);
		}

		[Fact(DisplayName = "Test that GetPath returns absolute token directly, ignoring basePath")]
		public void GetPathAbsoluteTokenIgnoresBasePathTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			string result = sut.GetPath(@"C:\base", @"D:\absolute\path");
			Assert.Equal(@"D:\absolute\path", result);
		}

		[Fact(DisplayName = "Test that GetPath combines basePath with plain relative token")]
		public void GetPathPlainRelativeTokenCombinesWithBaseTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			string result = sut.GetPath(@"C:\base", "subfolder");
			Assert.Equal(@"C:\base\subfolder", result);
		}

		[Fact(DisplayName = "Test that GetPath strips './' prefix when combining relative token with basePath")]
		public void GetPathDotSlashRelativeTokenCombinesTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			string result = sut.GetPath(@"C:\base", "./subfolder");
			Assert.Equal(@"C:\base\subfolder", result);
		}

		[Fact(DisplayName = "Test that GetPath resolves '../' prefix by using the parent of basePath")]
		public void GetPathDotDotSlashRelativeTokenUsesParentTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			string result = sut.GetPath(@"C:\base\child", "../sibling");
			Assert.Equal(@"C:\base\sibling", result);
		}

		[Fact(DisplayName = "Test that GetPath returns null when navigating above a drive root (Parent is null)")]
		public void GetPathDotDotSlashAboveDriveRootReturnsNullTest()
		{
			// DirectoryInfo("C:\\").Parent is null; accessing .FullName throws NullReferenceException,
			// which is caught and causes GetPath to return null.
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			string result = sut.GetPath(@"C:\", "../sibling");
			Assert.Null(result);
		}

		[Fact(DisplayName = "Test that GetPath resolves a relative basePath via assembly location")]
		public void GetPathRelativeBasePathIsResolvedFromAssemblyTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			string result = sut.GetPath("./config", "file.txt");
			Assert.NotNull(result);
			Assert.EndsWith("file.txt", result);
			Assert.True(result.Length > "file.txt".Length);
		}

		// ========== GetPath (single-arg) tests ==========

		[Fact(DisplayName = "Test that GetPath with empty token returns the assembly directory")]
		public void GetPathSingleArgEmptyReturnsAssemblyPathTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			string result = sut.GetPath("");
			Assert.NotNull(result);
			Assert.True(Directory.Exists(result));
		}

		[Fact(DisplayName = "Test that GetPath with absolute token returns the token unchanged")]
		public void GetPathSingleArgAbsoluteTokenReturnsTokenTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string result = sut.GetPath(stageRootFolder);
			Assert.Equal(stageRootFolder, result);
		}

		[Fact(DisplayName = "Test that GetPath with plain relative token appends it to the assembly path")]
		public void GetPathSingleArgRelativeTokenCombinesWithAssemblyPathTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			string result = sut.GetPath("config");
			Assert.NotNull(result);
			Assert.EndsWith("config", result);
		}

		// ========== CreateFolder tests ==========

		[Fact(DisplayName = "Test that CreateFolder creates a new folder successfully")]
		public void CreateFolderCreatesNewFolderTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			PathUtils sut = CreatePathUtils(stageRootFolder);
			string newFolder = Path.Combine(stageRootFolder, "new-folder");
			Assert.False(Directory.Exists(newFolder));
			Result result = sut.CreateFolder(newFolder);
			Assert.True(result.IsValid);
			Assert.Equal(ErrorCodes.Success, result.ErrorCode);
			Assert.True(Directory.Exists(newFolder));
		}

		[Fact(DisplayName = "Test that CreateFolder succeeds silently when folder already exists")]
		public void CreateFolderExistingFolderSucceedsTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			PathUtils sut = CreatePathUtils(stageRootFolder);
			Result result = sut.CreateFolder(stageRootFolder);
			Assert.True(result.IsValid);
		}

		[Fact(DisplayName = "Test that CreateFolder with invalid path and isStageFolder=true returns CouldNotCreateNewStageFolder")]
		public void CreateFolderInvalidPathStageFolderErrorTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			Result result = sut.CreateFolder("Z:\\invalid<>path", isStageFolder: true);
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.CouldNotCreateNewStageFolder, result.ErrorCode);
		}

		[Fact(DisplayName = "Test that CreateFolder with invalid path and isStageFolder=false returns CouldNotCreateAdministrativeFolder")]
		public void CreateFolderInvalidPathAdminFolderErrorTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			Result result = sut.CreateFolder("Z:\\invalid<>path", isStageFolder: false);
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.CouldNotCreateAdministrativeFolder, result.ErrorCode);
		}

		// ========== GetInitStageFolder tests ==========

		[Fact(DisplayName = "Test that GetInitStageFolder returns prefix only when AppendNumberOnFirstStage is false")]
		public void GetInitStageFolderWithoutNumberTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			PathUtils sut = CreatePathUtils(stageRootFolder, new Dictionary<string, string>
			{
				{ Settings.Keys.StageRootFolder, stageRootFolder },
				{ Settings.Keys.StageNamePrefix, "purge-temp" },
				{ Settings.Keys.StageVersionDelimiter, "-" },
				{ Settings.Keys.AppendNumberOnFirstStage, "false" },
				{ Settings.Keys.StageVersions, "2" },
			});
			Result<string> result = sut.GetInitStageFolder();
			Assert.True(result.IsValid);
			Assert.Equal(Path.Combine(stageRootFolder, "purge-temp"), result.Value);
		}

		[Fact(DisplayName = "Test that GetInitStageFolder appends '-1' when AppendNumberOnFirstStage is true and versions > 0")]
		public void GetInitStageFolderWithNumberAppendedTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			PathUtils sut = CreatePathUtils(stageRootFolder, new Dictionary<string, string>
			{
				{ Settings.Keys.StageRootFolder, stageRootFolder },
				{ Settings.Keys.StageNamePrefix, "purge-temp" },
				{ Settings.Keys.StageVersionDelimiter, "-" },
				{ Settings.Keys.AppendNumberOnFirstStage, "true" },
				{ Settings.Keys.StageVersions, "4" },
			});
			Result<string> result = sut.GetInitStageFolder();
			Assert.True(result.IsValid);
			Assert.Equal(Path.Combine(stageRootFolder, "purge-temp-1"), result.Value);
		}

		[Fact(DisplayName = "Test that GetInitStageFolder returns prefix only when AppendNumberOnFirstStage=true but StageVersions=0")]
		public void GetInitStageFolderAppendNumberFalseWhenVersionsZeroTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			PathUtils sut = CreatePathUtils(stageRootFolder, new Dictionary<string, string>
			{
				{ Settings.Keys.StageRootFolder, stageRootFolder },
				{ Settings.Keys.StageNamePrefix, "purge-temp" },
				{ Settings.Keys.StageVersionDelimiter, "-" },
				{ Settings.Keys.AppendNumberOnFirstStage, "true" },
				{ Settings.Keys.StageVersions, "0" },
			});
			Result<string> result = sut.GetInitStageFolder();
			Assert.True(result.IsValid);
			// stageVersions=0 fails `stageVersions > 0`, so no number is appended
			Assert.Equal(Path.Combine(stageRootFolder, "purge-temp"), result.Value);
		}

		// ========== GetStageFolders tests ==========

		[Theory(DisplayName = "Test that GetStageFolders returns the expected number of stage folders")]
		[InlineData(1, 1)]
		[InlineData(2, 2)]
		[InlineData(3, 3)]
		[InlineData(4, 4)]
		public void GetStageFoldersCountTest(int versions, int expectedCount)
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			PathUtils sut = CreatePathUtils(stageRootFolder, new Dictionary<string, string>
			{
				{ Settings.Keys.StageRootFolder, stageRootFolder },
				{ Settings.Keys.StageNamePrefix, "purge-temp" },
				{ Settings.Keys.StageVersionDelimiter, "-" },
				{ Settings.Keys.StageLastNameSuffix, "LAST" },
				{ Settings.Keys.AppendNumberOnFirstStage, "false" },
				{ Settings.Keys.StageVersions, versions.ToString() },
			});
			Result<List<string>> result = sut.GetStageFolders();
			Assert.True(result.IsValid);
			Assert.Equal(expectedCount, result.Value.Count);
		}

		[Fact(DisplayName = "Test that GetStageFolders returns correct folder names for 4 versions")]
		public void GetStageFoldersFourVersionsNamesTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			PathUtils sut = CreatePathUtils(stageRootFolder, new Dictionary<string, string>
			{
				{ Settings.Keys.StageRootFolder, stageRootFolder },
				{ Settings.Keys.StageNamePrefix, "purge-temp" },
				{ Settings.Keys.StageVersionDelimiter, "-" },
				{ Settings.Keys.StageLastNameSuffix, "LAST" },
				{ Settings.Keys.AppendNumberOnFirstStage, "false" },
				{ Settings.Keys.StageVersions, "4" },
			});
			Result<List<string>> result = sut.GetStageFolders();
			Assert.True(result.IsValid);
			Assert.Equal(Path.Combine(stageRootFolder, "purge-temp"), result.Value[0]);
			Assert.Equal(Path.Combine(stageRootFolder, "purge-temp-2"), result.Value[1]);
			Assert.Equal(Path.Combine(stageRootFolder, "purge-temp-3"), result.Value[2]);
			Assert.Equal(Path.Combine(stageRootFolder, "purge-temp-LAST"), result.Value[3]);
		}

		[Fact(DisplayName = "Test that GetStageFolders first folder ends with '-1' when AppendNumberOnFirstStage is true")]
		public void GetStageFoldersAppendNumberOnFirstStageTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			PathUtils sut = CreatePathUtils(stageRootFolder, new Dictionary<string, string>
			{
				{ Settings.Keys.StageRootFolder, stageRootFolder },
				{ Settings.Keys.StageNamePrefix, "purge-temp" },
				{ Settings.Keys.StageVersionDelimiter, "-" },
				{ Settings.Keys.StageLastNameSuffix, "LAST" },
				{ Settings.Keys.AppendNumberOnFirstStage, "true" },
				{ Settings.Keys.StageVersions, "3" },
			});
			Result<List<string>> result = sut.GetStageFolders();
			Assert.True(result.IsValid);
			Assert.Equal(Path.Combine(stageRootFolder, "purge-temp-1"), result.Value[0]);
			Assert.Equal(Path.Combine(stageRootFolder, "purge-temp-2"), result.Value[1]);
			Assert.Equal(Path.Combine(stageRootFolder, "purge-temp-LAST"), result.Value[2]);
		}

		[Fact(DisplayName = "Test that GetStageFolders propagates failure when GetInitStageFolder fails due to invalid settings")]
		public void GetStageFoldersReturnsFailWhenInitStageFolderFailsTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			// "not-a-bool" causes Convert.ChangeType to throw FormatException inside GetInitStageFolder's try block,
			// which is caught and returned as Fail — exercising the IsNotValid branch in GetStageFolders.
			PathUtils sut = CreatePathUtils(stageRootFolder, new Dictionary<string, string>
			{
				{ Settings.Keys.AppendNumberOnFirstStage, "not-a-bool" },
			});
			Result<List<string>> result = sut.GetStageFolders();
			Assert.True(result.IsNotValid);
		}

		// ========== SanitizeSettings tests ==========

		[Fact(DisplayName = "Test that SanitizeSettings leaves valid delimiter, prefix and suffix unchanged")]
		public void SanitizeSettingsValidValuesUnchangedTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			var result = sut.SanitizeSettings("-", "my-folder", "LAST");
			Assert.Equal("-", result.delimiter);
			Assert.Equal("my-folder", result.prefix);
			Assert.Equal("LAST", result.suffixForLast);
		}

		[Theory(DisplayName = "Test that SanitizeSettings replaces null or empty delimiter with empty string")]
		[InlineData(null)]
		[InlineData("")]
		public void SanitizeSettingsEmptyDelimiterBecomesEmptyStringTest(string delimiter)
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			var result = sut.SanitizeSettings(delimiter, "my-folder", "LAST");
			Assert.Equal("", result.delimiter);
		}

		[Theory(DisplayName = "Test that SanitizeSettings replaces delimiter with invalid characters")]
		[InlineData("<")]
		[InlineData(">")]
		[InlineData("|")]
		public void SanitizeSettingsInvalidCharsInDelimiterReplacedTest(string delimiter)
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			var result = sut.SanitizeSettings(delimiter, "my-folder", "LAST");
			Assert.Equal(PathUtils.DEFAULT_DELIMITER, result.delimiter);
		}

		[Theory(DisplayName = "Test that SanitizeSettings replaces a reserved Windows name used as delimiter")]
		[InlineData("NUL")]
		[InlineData("CON")]
		[InlineData("AUX")]
		public void SanitizeSettingsReservedDelimiterReplacedTest(string delimiter)
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			var result = sut.SanitizeSettings(delimiter, "my-folder", "LAST");
			Assert.Equal(PathUtils.DEFAULT_DELIMITER, result.delimiter);
		}

		[Theory(DisplayName = "Test that SanitizeSettings replaces invalid prefix with the default folder name")]
		[InlineData("")]
		[InlineData("NUL")]
		[InlineData("invalid<")]
		public void SanitizeSettingsInvalidPrefixReplacedWithDefaultTest(string prefix)
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			var result = sut.SanitizeSettings("-", prefix, "LAST");
			Assert.Equal(PathUtils.DEFAULT_TEMP_FOLDER_NAME, result.prefix);
		}

		[Theory(DisplayName = "Test that SanitizeSettings replaces invalid suffix with the default last folder token")]
		[InlineData("")]
		[InlineData("NUL")]
		[InlineData("invalid<")]
		public void SanitizeSettingsInvalidSuffixReplacedWithDefaultTest(string suffix)
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			var result = sut.SanitizeSettings("-", "my-folder", suffix);
			Assert.Equal(PathUtils.DEFAULT_LAST_FOLDER_NAME_TOKEN, result.suffixForLast);
		}

		[Fact(DisplayName = "Test that the two-arg SanitizeSettings overload returns correct delimiter and prefix")]
		public void SanitizeSettingsTwoArgOverloadTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			var result = sut.SanitizeSettings("-", "my-folder");
			Assert.Equal("-", result.delimiter);
			Assert.Equal("my-folder", result.prefix);
		}

		// ========== IsValidFolderName tests ==========

		[Theory(DisplayName = "Test that IsValidFolderName fails with EmptyFolderName for null, empty, or pure-separator input")]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("/")]
		[InlineData("\\")]
		public void IsValidFolderNameEmptyReturnsEmptyFolderNameErrorTest(string name)
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			Result result = sut.IsValidFolderName(name, false);
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.EmptyFolderName, result.ErrorCode);
		}

		[Theory(DisplayName = "Test that IsValidFolderName fails with IllegalCharactersInFolderName for names with invalid chars")]
		[InlineData("folder<name")]
		[InlineData("folder>name")]
		[InlineData("folder|name")]
		[InlineData("folder\tname")]
		[InlineData("C:")]
		public void IsValidFolderNameInvalidCharsReturnsIllegalCharactersErrorTest(string name)
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			Result result = sut.IsValidFolderName(name, false);
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.IllegalCharactersInFolderName, result.ErrorCode);
		}

		[Theory(DisplayName = "Test that IsValidFolderName rejects bare Windows reserved device names as ReservedNameAsFolderName")]
		[InlineData("NUL")]
		[InlineData("CON")]
		[InlineData("AUX")]
		[InlineData("PRN")]
		[InlineData("COM1")]
		[InlineData("LPT9")]
		public void IsValidFolderNameReservedNameIsRejectedTest(string name)
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			Result result = sut.IsValidFolderName(name, false);
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.ReservedNameAsFolderName, result.ErrorCode);
		}

		[Theory(DisplayName = "Test that IsValidFolderName fails with InvalidFolderNameSuffix for names ending with space or dot")]
		[InlineData("folder ")]
		[InlineData("folder.")]
		public void IsValidFolderNameTrailingSpaceOrDotReturnsInvalidSuffixErrorTest(string name)
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			Result result = sut.IsValidFolderName(name, false);
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.InvalidFolderNameSuffix, result.ErrorCode);
		}

		[Fact(DisplayName = "Test that IsValidFolderName fails with PathIsSystemDirectory for the test system path")]
		public void IsValidFolderNameSystemPathReturnsPathIsSystemDirectoryErrorTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			Result result = sut.IsValidFolderName(PathUtils.TestSystemPath, false);
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.PathIsSystemDirectory, result.ErrorCode);
		}

		[Theory(DisplayName = "Test that IsValidFolderName returns Success for valid folder names")]
		[InlineData("valid-folder")]
		[InlineData("my.folder")]
		[InlineData("123")]
		[InlineData("purge-temp")]
		public void IsValidFolderNameValidNameReturnsSuccessTest(string name)
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			Result result = sut.IsValidFolderName(name, false);
			Assert.True(result.IsValid);
			Assert.Equal(ErrorCodes.Success, result.ErrorCode);
		}

		[Fact(DisplayName = "Test that IsValidFolderName returns the same error code regardless of the isWarning flag")]
		public void IsValidFolderNameIsWarningFlagDoesNotAffectResultTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			Result resultWarning = sut.IsValidFolderName("", true);
			Result resultError = sut.IsValidFolderName("", false);
			Assert.Equal(resultWarning.ErrorCode, resultError.ErrorCode);
			Assert.Equal(resultWarning.IsNotValid, resultError.IsNotValid);
		}

		// ========== CheckSystemRelevantFolder tests ==========

		[Theory(DisplayName = "Test that CheckSystemRelevantFolder fails with EmptyFolderName for null or empty input")]
		[InlineData(null)]
		[InlineData("")]
		public void CheckSystemRelevantFolderEmptyInputReturnsEmptyFolderNameErrorTest(string path)
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			Result result = sut.CheckSystemRelevantFolder(path);
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.EmptyFolderName, result.ErrorCode);
		}

		[Fact(DisplayName = "Test that CheckSystemRelevantFolder fails for the dedicated test system path")]
		public void CheckSystemRelevantFolderTestSystemPathReturnsFailTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			Result result = sut.CheckSystemRelevantFolder(PathUtils.TestSystemPath);
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.PathIsSystemDirectory, result.ErrorCode);
		}

		[Fact(DisplayName = "Test that CheckSystemRelevantFolder fails for the Windows system directory")]
		public void CheckSystemRelevantFolderWindowsPathReturnsFailTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			string windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			Result result = sut.CheckSystemRelevantFolder(windowsPath);
			Assert.True(result.IsNotValid);
			Assert.Equal(ErrorCodes.PathIsSystemDirectory, result.ErrorCode);
		}

		[Fact(DisplayName = "Test that CheckSystemRelevantFolder returns Success for a valid temp folder path")]
		public void CheckSystemRelevantFolderValidPathReturnsSuccessTest()
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			PathUtils sut = CreatePathUtils(stageRootFolder);
			Result result = sut.CheckSystemRelevantFolder(stageRootFolder);
			Assert.True(result.IsValid);
		}

		[Fact(DisplayName = "Test that CheckSystemRelevantFolder with logError=false still returns the correct result")]
		public void CheckSystemRelevantFolderLogErrorFalseStillReturnsCorrectResultTest()
		{
			PathUtils sut = CreatePathUtils(fixture.CreateUniqueTempFolder());
			Result resultLog = sut.CheckSystemRelevantFolder(PathUtils.TestSystemPath, logError: true);
			Result resultNoLog = sut.CheckSystemRelevantFolder(PathUtils.TestSystemPath, logError: false);
			Assert.Equal(resultLog.ErrorCode, resultNoLog.ErrorCode);
			Assert.Equal(resultLog.IsNotValid, resultNoLog.IsNotValid);
		}

		// ========== Helper methods ==========

		private PathUtils CreatePathUtils(string stageRootFolder, Dictionary<string, string> overrides = null)
		{
			Settings settings = TestSettingsProvider.GetSettings(stageRootFolder, overrides);
			return new PathUtils(settings, nullLoggerFactory);
		}

		private class NullLoggerFactory : ILoggerFactory
		{
			public IAppLogger CreateAppLogger() => new NullAppLogger();
			public IPurgeLogger CreatePurgeLogger() => new NullPurgeLogger();

			private class NullAppLogger : IAppLogger
			{
				public void Information(string message) { }
				public void Warning(string message) { }
				public void Error(string message) { }
			}

			private class NullPurgeLogger : IPurgeLogger
			{
				public void PurgeInfo(string source, string file) { }
				public void MoveInfo(string source, string target, string file) { }
				public void SkipMoveInfo(string source, string target, int skippedFiles, bool skipAll = false) { }
				public void SkipPurgeInfo(string source, int skippedFiles, bool skipAll = false) { }
				public void SkipInfo(string source, string destination, bool isPurged, int skippedFiles, bool skipAll = false) { }
				public void Info(string source, string destination, bool isPurged, string file) { }
			}
		}
	}
}

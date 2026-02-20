using PurgeTemp.Controller;
using PurgeTemp.Interface;
using PurgeTemp.Utils;
using PurgeTempTest.Utils;

namespace PurgeTempTest
{
	public class FileUtilsTest : IClassFixture<TempFolderFixture>
	{
		private readonly TempFolderFixture fixture;
		private readonly ILoggerFactory nullLoggerFactory = new NullLoggerFactory();

		public FileUtilsTest(TempFolderFixture fixture)
		{
			this.fixture = fixture;
		}

		// ========== IsValidFileName tests ==========

		[Theory(DisplayName = "Test that IsValidFileName returns false for null or empty input")]
		[InlineData(null)]
		[InlineData("")]
		public void IsValidFileNameNullOrEmptyReturnsFalseTest(string fileName)
		{
			FileUtils sut = CreateFileUtils(fixture.CreateUniqueTempFolder());
			Assert.False(sut.IsValidFileName(fileName));
		}

		[Theory(DisplayName = "Test that IsValidFileName returns false when fileName ends with a path separator")]
		[InlineData("file\\")]
		[InlineData("folder\\name\\")]
		public void IsValidFileNameTrailingSeparatorReturnsFalseTest(string fileName)
		{
			FileUtils sut = CreateFileUtils(fixture.CreateUniqueTempFolder());
			Assert.False(sut.IsValidFileName(fileName));
		}

		[Theory(DisplayName = "Test that IsValidFileName returns false for names containing invalid characters")]
		[InlineData("file<name.txt")]
		[InlineData("file>name.txt")]
		[InlineData("file|name.txt")]
		[InlineData("file\tname.txt")]
		[InlineData("file*name.txt")]
		[InlineData("file?name.txt")]
		[InlineData("file\"name.txt")]
		public void IsValidFileNameInvalidCharsReturnsFalseTest(string fileName)
		{
			FileUtils sut = CreateFileUtils(fixture.CreateUniqueTempFolder());
			Assert.False(sut.IsValidFileName(fileName));
		}

		[Theory(DisplayName = "Test that IsValidFileName returns false for Windows reserved device names")]
		[InlineData("NUL")]
		[InlineData("CON")]
		[InlineData("AUX")]
		[InlineData("PRN")]
		[InlineData("COM1")]
		[InlineData("LPT9")]
		public void IsValidFileNameReservedNameReturnsFalseTest(string fileName)
		{
			FileUtils sut = CreateFileUtils(fixture.CreateUniqueTempFolder());
			Assert.False(sut.IsValidFileName(fileName));
		}

		[Theory(DisplayName = "Test that IsValidFileName returns false for names ending with a space or dot")]
		[InlineData("file ")]
		[InlineData("file.")]
		public void IsValidFileNameTrailingSpaceOrDotReturnsFalseTest(string fileName)
		{
			FileUtils sut = CreateFileUtils(fixture.CreateUniqueTempFolder());
			Assert.False(sut.IsValidFileName(fileName));
		}

		[Theory(DisplayName = "Test that IsValidFileName returns true for valid file names")]
		[InlineData("file.txt")]
		[InlineData("my-file.txt")]
		[InlineData("123")]
        [InlineData(".file")]
        [InlineData("report.2026.pdf")]
		public void IsValidFileNameValidNamesReturnsTrueTest(string fileName)
		{
			FileUtils sut = CreateFileUtils(fixture.CreateUniqueTempFolder());
			Assert.True(sut.IsValidFileName(fileName));
		}

		[Theory(DisplayName = "Test that IsValidFileName returns true for a path whose last component is a valid name")]
		[InlineData("subfolder\\file.txt")]
        [InlineData("subfolder\\.file")]
        [InlineData("a\\b\\c.txt")]
		public void IsValidFileNamePathWithValidLastComponentReturnsTrueTest(string fileName)
		{
			FileUtils sut = CreateFileUtils(fixture.CreateUniqueTempFolder());
			Assert.True(sut.IsValidFileName(fileName));
		}

		[Fact(DisplayName = "Test that IsValidFileName accepts a path that uses forward slashes by normalizing separators first")]
		public void IsValidFileNameForwardSlashPathReturnsTrueTest()
		{
			// Without the fix the entire string becomes the lastToken and '/' is treated as
			// an invalid filename character, causing an incorrect false result.
			FileUtils sut = CreateFileUtils(fixture.CreateUniqueTempFolder());
			Assert.True(sut.IsValidFileName("subdir/file.txt"));
		}

		// ========== LogFilesToProcess tests ==========

		[Fact(DisplayName = "Test that LogFilesToProcess logs nothing when the current folder contains no files")]
		public void LogFilesToProcessEmptyFolderLogsNothingTest()
		{
			string root = fixture.CreateUniqueTempFolder();
			string folder1 = Path.Combine(root, "stage");
			string folder2 = Path.Combine(root, "stage-LAST");
			Directory.CreateDirectory(folder1);
			Directory.CreateDirectory(folder2);
			var (sut, logger) = CreateFileUtilsWithCapture(root);

			sut.LogFilesToProcess(new List<string> { folder1, folder2 }, folder2);

			Assert.Empty(logger.PurgeInfoCalls);
			Assert.Empty(logger.SkipPurgeInfoCalls);
		}

		[Fact(DisplayName = "Test that LogFilesToProcess calls PurgeInfo for each file in the last folder")]
		public void LogFilesToProcessLastFolderCallsPurgeInfoTest()
		{
			string root = fixture.CreateUniqueTempFolder();
			string folder1 = Path.Combine(root, "stage");
			string folder2 = Path.Combine(root, "stage-LAST");
			Directory.CreateDirectory(folder1);
			Directory.CreateDirectory(folder2);
			File.WriteAllText(Path.Combine(folder2, "a.txt"), "");
			File.WriteAllText(Path.Combine(folder2, "b.txt"), "");
			var (sut, logger) = CreateFileUtilsWithCapture(root, new Dictionary<string, string>
			{
				{ Settings.Keys.FileLogAmountThreshold, "-1" },
			});

			sut.LogFilesToProcess(new List<string> { folder1, folder2 }, folder2);

			Assert.Equal(2, logger.PurgeInfoCalls.Count);
			Assert.All(logger.PurgeInfoCalls, call => Assert.Equal(folder2, call.source));
			Assert.Empty(logger.MoveInfoCalls);
			Assert.Empty(logger.SkipPurgeInfoCalls);
		}

		[Fact(DisplayName = "Test that LogFilesToProcess calls MoveInfo for each file in a non-last folder")]
		public void LogFilesToProcessNonLastFolderCallsMoveInfoTest()
		{
			string root = fixture.CreateUniqueTempFolder();
			string folder1 = Path.Combine(root, "stage");
			string folder2 = Path.Combine(root, "stage-LAST");
			Directory.CreateDirectory(folder1);
			Directory.CreateDirectory(folder2);
			File.WriteAllText(Path.Combine(folder1, "x.txt"), "");
			var (sut, logger) = CreateFileUtilsWithCapture(root, new Dictionary<string, string>
			{
				{ Settings.Keys.FileLogAmountThreshold, "-1" },
			});

			sut.LogFilesToProcess(new List<string> { folder1, folder2 }, folder1);

			Assert.Single(logger.MoveInfoCalls);
			Assert.Equal(folder1, logger.MoveInfoCalls[0].source);
			Assert.Equal(folder2, logger.MoveInfoCalls[0].target);
			Assert.Empty(logger.PurgeInfoCalls);
			Assert.Empty(logger.SkipMoveInfoCalls);
		}

		[Fact(DisplayName = "Test that MoveInfo receives a file path relative to currentFolder, not targetFolder")]
		public void LogFilesToProcessMoveInfoFilePathRelativeToCurrentFolderTest()
		{
			// Use deliberately different-length folder names so that an incorrect use of
			// targetFolder.Length (the bug) produces a different substring than
			// currentFolder.Length (the fix).
			string root = fixture.CreateUniqueTempFolder();
			string currentFolder = Path.Combine(root, "s");           // shorter
			string targetFolder  = Path.Combine(root, "stage-LAST");  // longer
			Directory.CreateDirectory(currentFolder);
			Directory.CreateDirectory(targetFolder);
			string filePath = Path.Combine(currentFolder, "data.txt");
			File.WriteAllText(filePath, "");
			var (sut, logger) = CreateFileUtilsWithCapture(root, new Dictionary<string, string>
			{
				{ Settings.Keys.FileLogAmountThreshold, "-1" },
			});

			sut.LogFilesToProcess(new List<string> { currentFolder, targetFolder }, currentFolder);

			Assert.Single(logger.MoveInfoCalls);
			string expectedFileArg = filePath.Substring(currentFolder.Length - 1);
			Assert.Equal(expectedFileArg, logger.MoveInfoCalls[0].file);
		}

		[Fact(DisplayName = "Test that LogFilesToProcess calls SkipPurgeInfo with skipAll=true when threshold is 0 in the last folder")]
		public void LogFilesToProcessThresholdZeroLastFolderSkipPurgeInfoSkipAllTrueTest()
		{
			string root = fixture.CreateUniqueTempFolder();
			string folder1 = Path.Combine(root, "stage");
			string folder2 = Path.Combine(root, "stage-LAST");
			Directory.CreateDirectory(folder1);
			Directory.CreateDirectory(folder2);
			File.WriteAllText(Path.Combine(folder2, "a.txt"), "");
			File.WriteAllText(Path.Combine(folder2, "b.txt"), "");
			var (sut, logger) = CreateFileUtilsWithCapture(root, new Dictionary<string, string>
			{
				{ Settings.Keys.FileLogAmountThreshold, "0" },
			});

			sut.LogFilesToProcess(new List<string> { folder1, folder2 }, folder2);

			Assert.Empty(logger.PurgeInfoCalls);
			Assert.Single(logger.SkipPurgeInfoCalls);
			Assert.Equal(folder2, logger.SkipPurgeInfoCalls[0].source);
			Assert.Equal(2, logger.SkipPurgeInfoCalls[0].skippedFiles);
			Assert.True(logger.SkipPurgeInfoCalls[0].skipAll);
		}

		[Fact(DisplayName = "Test that LogFilesToProcess calls SkipMoveInfo with skipAll=true when threshold is 0 in a non-last folder")]
		public void LogFilesToProcessThresholdZeroNonLastFolderSkipMoveInfoSkipAllTrueTest()
		{
			string root = fixture.CreateUniqueTempFolder();
			string folder1 = Path.Combine(root, "stage");
			string folder2 = Path.Combine(root, "stage-LAST");
			Directory.CreateDirectory(folder1);
			Directory.CreateDirectory(folder2);
			File.WriteAllText(Path.Combine(folder1, "a.txt"), "");
			File.WriteAllText(Path.Combine(folder1, "b.txt"), "");
			var (sut, logger) = CreateFileUtilsWithCapture(root, new Dictionary<string, string>
			{
				{ Settings.Keys.FileLogAmountThreshold, "0" },
			});

			sut.LogFilesToProcess(new List<string> { folder1, folder2 }, folder1);

			Assert.Empty(logger.MoveInfoCalls);
			Assert.Single(logger.SkipMoveInfoCalls);
			Assert.Equal(folder1, logger.SkipMoveInfoCalls[0].source);
			Assert.Equal(folder2, logger.SkipMoveInfoCalls[0].target);
			Assert.Equal(2, logger.SkipMoveInfoCalls[0].skippedFiles);
			Assert.True(logger.SkipMoveInfoCalls[0].skipAll);
		}

		[Fact(DisplayName = "Test that LogFilesToProcess logs up to the threshold and reports the rest as skipped in the last folder")]
		public void LogFilesToProcessThresholdLimitsLoggingInLastFolderTest()
		{
			string root = fixture.CreateUniqueTempFolder();
			string folder1 = Path.Combine(root, "stage");
			string folder2 = Path.Combine(root, "stage-LAST");
			Directory.CreateDirectory(folder1);
			Directory.CreateDirectory(folder2);
			File.WriteAllText(Path.Combine(folder2, "a.txt"), "");
			File.WriteAllText(Path.Combine(folder2, "b.txt"), "");
			File.WriteAllText(Path.Combine(folder2, "c.txt"), "");
			var (sut, logger) = CreateFileUtilsWithCapture(root, new Dictionary<string, string>
			{
				{ Settings.Keys.FileLogAmountThreshold, "1" },
			});

			sut.LogFilesToProcess(new List<string> { folder1, folder2 }, folder2);

			Assert.Single(logger.PurgeInfoCalls);
			Assert.Single(logger.SkipPurgeInfoCalls);
			Assert.Equal(2, logger.SkipPurgeInfoCalls[0].skippedFiles);
			Assert.False(logger.SkipPurgeInfoCalls[0].skipAll);
		}

		[Fact(DisplayName = "Test that LogFilesToProcess logs up to the threshold and reports the rest as skipped in a non-last folder")]
		public void LogFilesToProcessThresholdLimitsLoggingInNonLastFolderTest()
		{
			string root = fixture.CreateUniqueTempFolder();
			string folder1 = Path.Combine(root, "stage");
			string folder2 = Path.Combine(root, "stage-LAST");
			Directory.CreateDirectory(folder1);
			Directory.CreateDirectory(folder2);
			File.WriteAllText(Path.Combine(folder1, "a.txt"), "");
			File.WriteAllText(Path.Combine(folder1, "b.txt"), "");
			File.WriteAllText(Path.Combine(folder1, "c.txt"), "");
			var (sut, logger) = CreateFileUtilsWithCapture(root, new Dictionary<string, string>
			{
				{ Settings.Keys.FileLogAmountThreshold, "1" },
			});

			sut.LogFilesToProcess(new List<string> { folder1, folder2 }, folder1);

			Assert.Single(logger.MoveInfoCalls);
			Assert.Single(logger.SkipMoveInfoCalls);
			Assert.Equal(2, logger.SkipMoveInfoCalls[0].skippedFiles);
			Assert.False(logger.SkipMoveInfoCalls[0].skipAll);
		}

		[Fact(DisplayName = "Test that LogFilesToProcess logs all files and skips nothing when threshold is -1")]
		public void LogFilesToProcessThresholdMinusOneLogsAllFilesTest()
		{
			string root = fixture.CreateUniqueTempFolder();
			string folder1 = Path.Combine(root, "stage");
			string folder2 = Path.Combine(root, "stage-LAST");
			Directory.CreateDirectory(folder1);
			Directory.CreateDirectory(folder2);
			File.WriteAllText(Path.Combine(folder2, "a.txt"), "");
			File.WriteAllText(Path.Combine(folder2, "b.txt"), "");
			File.WriteAllText(Path.Combine(folder2, "c.txt"), "");
			var (sut, logger) = CreateFileUtilsWithCapture(root, new Dictionary<string, string>
			{
				{ Settings.Keys.FileLogAmountThreshold, "-1" },
			});

			sut.LogFilesToProcess(new List<string> { folder1, folder2 }, folder2);

			Assert.Equal(3, logger.PurgeInfoCalls.Count);
			Assert.Empty(logger.SkipPurgeInfoCalls);
		}

		[Fact(DisplayName = "Test that LogFilesToProcess logs all files and skips nothing when threshold exceeds file count")]
		public void LogFilesToProcessThresholdAboveFileCountLogsAllTest()
		{
			string root = fixture.CreateUniqueTempFolder();
			string folder1 = Path.Combine(root, "stage");
			string folder2 = Path.Combine(root, "stage-LAST");
			Directory.CreateDirectory(folder1);
			Directory.CreateDirectory(folder2);
			File.WriteAllText(Path.Combine(folder2, "a.txt"), "");
			File.WriteAllText(Path.Combine(folder2, "b.txt"), "");
			var (sut, logger) = CreateFileUtilsWithCapture(root, new Dictionary<string, string>
			{
				{ Settings.Keys.FileLogAmountThreshold, "100" },
			});

			sut.LogFilesToProcess(new List<string> { folder1, folder2 }, folder2);

			Assert.Equal(2, logger.PurgeInfoCalls.Count);
			Assert.Empty(logger.SkipPurgeInfoCalls);
		}

		// ========== Helper methods ==========

		private FileUtils CreateFileUtils(string tempFolder, Dictionary<string, string> overrides = null)
		{
			Settings settings = TestSettingsProvider.GetSettings(tempFolder, overrides);
			return new FileUtils(settings, nullLoggerFactory);
		}

		private (FileUtils sut, CapturingPurgeLogger purgeLogger) CreateFileUtilsWithCapture(
			string tempFolder, Dictionary<string, string> overrides = null)
		{
			Settings settings = TestSettingsProvider.GetSettings(tempFolder, overrides);
			var capturingLogger = new CapturingPurgeLogger();
			return (new FileUtils(settings, new CapturingLoggerFactory(capturingLogger)), capturingLogger);
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

		private class CapturingLoggerFactory : ILoggerFactory
		{
			private readonly CapturingPurgeLogger purgeLogger;

			public CapturingLoggerFactory(CapturingPurgeLogger purgeLogger)
			{
				this.purgeLogger = purgeLogger;
			}

			public IAppLogger CreateAppLogger() => new NullAppLogger();
			public IPurgeLogger CreatePurgeLogger() => purgeLogger;

			private class NullAppLogger : IAppLogger
			{
				public void Information(string message) { }
				public void Warning(string message) { }
				public void Error(string message) { }
			}
		}

		private class CapturingPurgeLogger : IPurgeLogger
		{
			public List<(string source, string file)> PurgeInfoCalls { get; } = new();
			public List<(string source, string target, string file)> MoveInfoCalls { get; } = new();
			public List<(string source, string target, int skippedFiles, bool skipAll)> SkipMoveInfoCalls { get; } = new();
			public List<(string source, int skippedFiles, bool skipAll)> SkipPurgeInfoCalls { get; } = new();

			public void PurgeInfo(string source, string file) => PurgeInfoCalls.Add((source, file));
			public void MoveInfo(string source, string target, string file) => MoveInfoCalls.Add((source, target, file));
			public void SkipMoveInfo(string source, string target, int skippedFiles, bool skipAll = false) => SkipMoveInfoCalls.Add((source, target, skippedFiles, skipAll));
			public void SkipPurgeInfo(string source, int skippedFiles, bool skipAll = false) => SkipPurgeInfoCalls.Add((source, skippedFiles, skipAll));
			public void SkipInfo(string source, string destination, bool isPurged, int skippedFiles, bool skipAll = false) { }
			public void Info(string source, string destination, bool isPurged, string file) { }
		}
	}
}

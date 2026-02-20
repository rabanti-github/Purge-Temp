using Microsoft.Extensions.Configuration;
using PurgeTemp.Controller;
using PurgeTemp.Interface;
using PurgeTemp.Utils;
using PurgeTempTest.Utils;

namespace PurgeTempTest
{
	public class CLITest : IClassFixture<TempFolderFixture>
	{
		private readonly TempFolderFixture fixture;
		private readonly IConfiguration emptyConfig = new ConfigurationBuilder().Build();

		public CLITest(TempFolderFixture fixture)
		{
			this.fixture = fixture;
		}

		// ========== ParseSetting — general behaviour ==========

		[Fact(DisplayName = "Test that ParseSetting with no arguments returns the default settings unchanged")]
		public void ParseSettingWithNoArgsReturnsSettingsUnchangedTest()
		{
			string tempFolder = fixture.CreateUniqueTempFolder();
			CLI sut = CreateCLI(tempFolder);
			Settings settings = CreateSettings(tempFolder);
			int originalStageVersions = settings.StageVersions;

			Settings result = sut.ParseSetting(settings, emptyConfig, Array.Empty<string>());

			Assert.Equal(originalStageVersions, result.StageVersions);
		}

		[Fact(DisplayName = "Test that ParseSetting with an unrecognized argument does not throw (null-opts guard)")]
		public void ParseSettingWithUnrecognizedArgDoesNotThrowTest()
		{
			// Before the fix, opts remained null when parsing failed and opts.Help threw NullReferenceException.
			string tempFolder = fixture.CreateUniqueTempFolder();
			CLI sut = CreateCLI(tempFolder);
			Settings settings = CreateSettings(tempFolder);

			// Suppress CommandLineParser's error output to Console.Error during this test.
			TextWriter savedError = Console.Error;
			Console.SetError(TextWriter.Null);
			try
			{
				Exception ex = Record.Exception(
					() => sut.ParseSetting(settings, emptyConfig, new[] { "--unknown-invalid-flag-xyz" }));
				Assert.Null(ex);
			}
			finally
			{
				Console.SetError(savedError);
			}
		}

		[Fact(DisplayName = "Test that ParseSetting does not override a setting that was not provided on the command line")]
		public void ParseSettingUnprovidedOptionDoesNotOverrideSettingTest()
		{
			string tempFolder = fixture.CreateUniqueTempFolder();
			CLI sut = CreateCLI(tempFolder);
			Settings settings = CreateSettings(tempFolder);
			string originalPrefix = settings.StageNamePrefix;

			// Pass only --stage-versions; StageNamePrefix must stay at its default value.
			Settings result = sut.ParseSetting(settings, emptyConfig, new[] { "--stage-versions", "7" });

			Assert.Equal(originalPrefix, result.StageNamePrefix);
		}

		// ========== ParseSetting — int options ==========

		[Theory(DisplayName = "Test that ParseSetting correctly applies an integer option to the expected settings key")]
		[InlineData("--stage-versions", "7", nameof(Settings.StageVersions), 7)]
		[InlineData("--file-log-amount-threshold", "50", nameof(Settings.FileLogAmountThreshold), 50)]
		[InlineData("--log-rotation-bytes", "2048", nameof(Settings.LogRotationBytes), 2048)]
		[InlineData("--log-rotation-versions", "5", nameof(Settings.LogRotationVersions), 5)]
		[InlineData("--staging-delay-seconds", "7200", nameof(Settings.StagingDelaySeconds), 7200)]
		public void ParseSettingIntOptionOverridesSettingTest(string flag, string value, string propertyName, int expected)
		{
			string tempFolder = fixture.CreateUniqueTempFolder();
			CLI sut = CreateCLI(tempFolder);
			Settings settings = CreateSettings(tempFolder);

			Settings result = sut.ParseSetting(settings, emptyConfig, new[] { flag, value });

			int actual = (int)typeof(Settings).GetProperty(propertyName)!.GetValue(result)!;
			Assert.Equal(expected, actual);
		}

		// ========== ParseSetting — string options ==========

		[Theory(DisplayName = "Test that ParseSetting correctly applies a string option to the expected settings key")]
		[InlineData("--stage-name-prefix", "my-prefix", nameof(Settings.StageNamePrefix))]
		[InlineData("--stage-version-delimiter", "_", nameof(Settings.StageVersionDelimiter))]
		[InlineData("--stage-last-name-suffix", "END", nameof(Settings.StageLastNameSuffix))]
		[InlineData("--logging-folder", "custom-logs", nameof(Settings.LoggingFolder))]
		[InlineData("--purge-message-logo", "logo.png", nameof(Settings.PurgeMessageLogoFile))]
		[InlineData("--staging-timestamp-file", "ts.txt", nameof(Settings.StagingTimestampFile))]
		[InlineData("--stage-root-folder", "custom-stage", nameof(Settings.StageRootFolder))]
		[InlineData("--config-folder", "custom-config", nameof(Settings.ConfigFolder))]
		[InlineData("--temp-folder", "custom-temp", nameof(Settings.TempFolder))]
		[InlineData("--skip-token-file", "SKIP.txt", nameof(Settings.SkipTokenFile))]
		[InlineData("--timestamp-format", "yyyy/MM/dd", nameof(Settings.TimeStampFormat))]
		public void ParseSettingStringOptionOverridesSettingTest(string flag, string value, string propertyName)
		{
			string tempFolder = fixture.CreateUniqueTempFolder();
			CLI sut = CreateCLI(tempFolder);
			Settings settings = CreateSettings(tempFolder);

			Settings result = sut.ParseSetting(settings, emptyConfig, new[] { flag, value });

			string actual = (string)typeof(Settings).GetProperty(propertyName)!.GetValue(result)!;
			Assert.Equal(value, actual);
		}

		// ========== ParseSetting — bool options ==========

		[Theory(DisplayName = "Test that ParseSetting correctly applies a boolean option to the expected settings key")]
		[InlineData("--append-number-on-first-stage", "true", nameof(Settings.AppendNumberOnFirstStage), true)]
		[InlineData("--append-number-on-first-stage", "false", nameof(Settings.AppendNumberOnFirstStage), false)]
		[InlineData("--log-all-files", "true", nameof(Settings.LogAllFiles), true)]
		[InlineData("--log-all-files", "false", nameof(Settings.LogAllFiles), false)]
		[InlineData("--log-enabled", "true", nameof(Settings.LogEnabled), true)]
		[InlineData("--log-enabled", "false", nameof(Settings.LogEnabled), false)]
		[InlineData("--show-purge-message", "true", nameof(Settings.ShowPurgeMessage), true)]
		[InlineData("--show-purge-message", "false", nameof(Settings.ShowPurgeMessage), false)]
		[InlineData("--remove-empty-stage-folders", "true", nameof(Settings.RemoveEmptyStageFolders), true)]
		[InlineData("--remove-empty-stage-folders", "false", nameof(Settings.RemoveEmptyStageFolders), false)]
		public void ParseSettingBoolOptionOverridesSettingTest(string flag, string value, string propertyName, bool expected)
		{
			// The --remove-empty-stage-folders cases also verify the bug fix:
			// before the fix, opts.RemoveEmptyStageFolders (bool?) was stored directly and caused
			// InvalidCastException when Settings.RemoveEmptyStageFolders was later accessed.
			string tempFolder = fixture.CreateUniqueTempFolder();
			CLI sut = CreateCLI(tempFolder);
			Settings settings = CreateSettings(tempFolder);

			Settings result = sut.ParseSetting(settings, emptyConfig, new[] { flag, value });

			bool actual = (bool)typeof(Settings).GetProperty(propertyName)!.GetValue(result)!;
			Assert.Equal(expected, actual);
		}

		// ========== ParseSetting — Help branch ==========

		[Fact(DisplayName = "Test that ParseSetting with --help invokes the exit action and writes help text to the console")]
		public void ParseSettingWithHelpFlagInvokesExitAndWritesHelpTextTest()
		{
			string tempFolder = fixture.CreateUniqueTempFolder();
			bool exitCalled = false;
			CLI sut = CreateCLI(tempFolder, () => exitCalled = true);
			Settings settings = CreateSettings(tempFolder);

			TextWriter savedOut = Console.Out;
			using StringWriter sw = new StringWriter();
			Console.SetOut(sw);
			try
			{
				sut.ParseSetting(settings, emptyConfig, new[] { "--help" });
			}
			finally
			{
				Console.SetOut(savedOut);
			}

			Assert.True(exitCalled, "Exit action should have been called by the --help flag.");
			string output = sw.ToString();
			Assert.Contains("PurgeTemp", output);
			Assert.Contains("Arguments", output);
		}

		// ========== ParseSetting — SettingsFile branch ==========

		[Fact(DisplayName = "Test that ParseSetting with --settings-file loads the provided configuration into the settings")]
		public void ParseSettingWithSettingsFileLoadsConfigurationTest()
		{
			// Note: Settings.LoadSettings currently ignores its path parameter and loads from the
			// IConfiguration passed to ParseSetting. The --settings-file flag therefore controls
			// which IConfiguration is merged, not which file is read from disk.
			string tempFolder = fixture.CreateUniqueTempFolder();
			CLI sut = CreateCLI(tempFolder);

			IConfiguration configWithKnownValues = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string>
				{
					{ "AppSettings:StageVersions", "42" }
				})
				.Build();

			Settings settings = CreateSettings(tempFolder);
			Settings result = sut.ParseSetting(settings, configWithKnownValues, new[] { "--settings-file", "any-file.json" });

			Assert.Equal(42, result.StageVersions);
		}

		// ========== ParseSetting — null defaultSettings branch ==========

		[Fact(DisplayName = "Test that ParseSetting creates a non-null Settings when defaultSettings is null and no settings file is provided")]
		public void ParseSettingWithNullDefaultSettingsCreatesNewSettingsTest()
		{
			string tempFolder = fixture.CreateUniqueTempFolder();
			CLI sut = CreateCLI(tempFolder);

			Settings result = sut.ParseSetting(null, emptyConfig, Array.Empty<string>());

			Assert.NotNull(result);
		}

		// ========== GetCLIPrefix ==========

		[Fact(DisplayName = "Test that GetCLIPrefix returns a string containing the expected sections")]
		public void GetCLIPrefixContainsExpectedSectionsTest()
		{
			string tempFolder = fixture.CreateUniqueTempFolder();
			CLI sut = CreateCLI(tempFolder);

			string result = sut.GetCLIPrefix();

			Assert.False(string.IsNullOrEmpty(result));
			Assert.Contains("Project URL:", result);
			Assert.Contains("Arguments", result);
			Assert.Contains("=========", result);
		}

		// ========== Helper methods ==========

		private CLI CreateCLI(string tempFolder, Action exitAction = null)
		{
			Settings settings = TestSettingsProvider.GetSettings(tempFolder);
			PathUtils pathUtils = new PathUtils(settings, new NullLoggerFactory());
			return new CLI(pathUtils, exitAction ?? (() => { }));
		}

		private Settings CreateSettings(string tempFolder, Dictionary<string, string>? overrides = null)
		{
			return TestSettingsProvider.GetSettings(tempFolder, overrides);
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

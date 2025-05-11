using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PurgeTemp.Controller;
using PurgeTempTest.Utils;

namespace PurgeTempTest
{
	/// <summary>
	/// Class to test several triggers to skip purge executions
	/// </summary>
	public class SkipTest : IClassFixture<TempFolderFixture>
	{
		private readonly TempFolderFixture fixture;

		public SkipTest(TempFolderFixture fixture)
		{
			this.fixture = fixture;
		}

		[Theory(DisplayName = "Test of the skipping behavior based on the elapsed time")]
		[InlineData(1, 2, true)]  // expected no skip
		[InlineData(5, 7, true)]  // expected no skip
		[InlineData(10, 12, true)] // expected no skip
		[InlineData(3, 2, false)]   // expected skip
		[InlineData(8, 5, false)]   // expected skip
		[InlineData(15, 10, false)] // expected skip
		public void PurgeSkipTest(int givenSkipSeconds, int testDelaySeconds, bool expectedExecuted)
		{
			string stageFolder1, stageFolder2;
			Settings testSettings;
			Arrange(givenSkipSeconds, out stageFolder1, out stageFolder2, out testSettings);

			// Act 1: Execute initial purge
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.ExecutePurge();

			// Assert initial folder is empty
			Assert.Empty(Directory.GetFiles(stageFolder1));

			// Write test files into the initial folder

			string testFilePath1 = TestFileUtils.CreateFile(stageFolder1, "testfile1.txt", "This is test file 1.");
			string testFilePath2 = TestFileUtils.CreateFile(stageFolder1, "testfile2.txt", "This is test file 2.");

			// Simulate delay
			Thread.Sleep(testDelaySeconds * 1000);

			// Act 2: Execute second purge
			purgeController.ExecutePurge();

			// Assert
			if (expectedExecuted)
			{
				Assert.Empty(Directory.GetFiles(stageFolder1));
				Assert.NotEmpty(Directory.GetFiles(stageFolder2));
			}
			else
			{
				Assert.NotEmpty(Directory.GetFiles(stageFolder1));
				Assert.Empty(Directory.GetFiles(stageFolder2));
			}
		}


		[Theory(DisplayName = "Test of the skipping behavior on skip tokens")]
		[InlineData(3, 5, "no-skip.txt", "skip.txt", true)]  // expected no skip
		[InlineData(4, 2, "no-skip.txt", "skip.txt", false)] // expected skip (by time)
		[InlineData(3, 5, "skip.txt", "skip.txt", false)]  // expected skip (by token)
		[InlineData(4, 2, "skip.txt", "skip.txt", false)]  // expected skip (by token)
		public void PurgeSkipTokenTest(int givenSkipSeconds, int testDelaySeconds, string givenSkipFile, string expectedSkipFile, bool expectedExecuted)
		{
			string stageFolder1, stageFolder2;
			Settings testSettings;
			Arrange(givenSkipSeconds, out stageFolder1, out stageFolder2, out testSettings);
			testSettings.OverrideSetting(Settings.Keys.SkipTokenFile, expectedSkipFile);
			//string testFilePath1 = FileUtils.CreateFile(stageFolder1, "testfile1.txt", "This is test file 1.");

			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.ExecutePurge();
			Assert.Empty(Directory.GetFiles(stageFolder1));

			string testFilePath1 = TestFileUtils.CreateFile(stageFolder1, "testfile1.txt", "This is test file 1.");
			string skipFilePath = TestFileUtils.CreateFile(stageFolder1, givenSkipFile, "No content");

			Thread.Sleep(testDelaySeconds * 1000);

			purgeController.ExecutePurge();

			// Assert
			if (expectedExecuted)
			{
				Assert.Empty(Directory.GetFiles(stageFolder1));
				Assert.NotEmpty(Directory.GetFiles(stageFolder2));
				Assert.Contains(Path.GetFileName(skipFilePath), Directory.GetFiles(stageFolder2).Select(Path.GetFileName));
			}
			else
			{
				Assert.Contains(testFilePath1, Directory.GetFiles(stageFolder1));
				Assert.Contains(skipFilePath, Directory.GetFiles(stageFolder1));
				Assert.Empty(Directory.GetFiles(stageFolder2));
			}
		}

		private string Arrange(int givenSkipSeconds, out string stageFolder1, out string stageFolder2, out Settings testSettings)
		{
			string stageRootFolder = fixture.CreateUniqueTempFolder();
			string stageNamePrefix = "purge-temp";
			string stageVersionDelimiter = "_";
			stageFolder1 = Path.Combine(stageRootFolder, stageNamePrefix + stageVersionDelimiter + "1");
			stageFolder2 = Path.Combine(stageRootFolder, stageNamePrefix + stageVersionDelimiter + "2");

			// Create test settings with override
			var overrides = new Dictionary<string, string>
		{
			{"StagingDelaySeconds", givenSkipSeconds.ToString()},
			{"StageRootFolder", stageRootFolder},
			{"StageNamePrefix", stageNamePrefix},
			{"StageVersionDelimiter", stageVersionDelimiter},
		};

			testSettings = TestSettingsProvider.GetSettings(stageRootFolder, overrides);
			return stageRootFolder;
		}
	}
}

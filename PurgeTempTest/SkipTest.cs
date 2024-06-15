using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PurgeTemp.Controller;
using PurgeTemp.Logger;
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

		[Theory]
		[InlineData(1, 2, true)]  // expected no skip
		[InlineData(5, 7, true)]  // expected no skip
		[InlineData(10, 12, true)] // expected no skip
		[InlineData(3, 2, false)]   // expected skip
		[InlineData(8, 5, true)]   // expected skip
		[InlineData(15, 10, true)] // expected skip
		public void PurgeSkipTest(int givenSkipSeconds, int testDelaySeconds, bool expectedExecuted)
		{

			// Arrange
			string stageRootFolder = Path.Combine(fixture.TempFolderPath, "stage-root");
			string stageNamePrefix = "purge-temp";
			string stageVersionDelimiter = "_";
			string stageFolder1 = Path.Combine(stageRootFolder, stageNamePrefix + stageVersionDelimiter + "1");
			string stageFolder2 = Path.Combine(stageRootFolder, stageNamePrefix + stageVersionDelimiter + "2");
			//Directory.CreateDirectory(stageFolder1);
			//Directory.CreateDirectory(stageFolder2);

			// Create test settings with override
			var overrides = new Dictionary<string, string>
		{
			{"StagingDelaySeconds", givenSkipSeconds.ToString()},
			{"StageRootFolder", stageRootFolder},
			{"StageNamePrefix", stageNamePrefix},
			{"StageVersionDelimiter", stageVersionDelimiter},
		};

			Settings testSettings = TestSettingsProvider.GetSettings(fixture.TempFolderPath, overrides);

			// Act 1: Execute initial purge
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);
			purgeController.ExecutePurge();

			// Assert initial folder is empty
			Assert.Empty(Directory.GetFiles(stageFolder1));

			// Write test files into the initial folder
			string testFilePath1 = Path.Combine(stageFolder1, "testfile1.txt");
			string testFilePath2 = Path.Combine(stageFolder1, "testfile2.txt");
			File.WriteAllText(testFilePath1, "This is test file 1.");
			File.WriteAllText(testFilePath2, "This is test file 2.");

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

			// Clean up
			if (File.Exists(testFilePath1)) File.Delete(testFilePath1);
			if (File.Exists(testFilePath2)) File.Delete(testFilePath2);
			if (Directory.Exists(stageFolder1)) Directory.Delete(stageFolder1, true);
			if (Directory.Exists(stageFolder2)) Directory.Delete(stageFolder2, true);
		}
	}

	public class TempFolderFixture : IDisposable
	{
		public string TempFolderPath { get; private set; }

		public TempFolderFixture()
		{
			TempFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			Directory.CreateDirectory(TempFolderPath);
		}

		public void Dispose()
		{
			if (Directory.Exists(TempFolderPath))
			{
				Directory.Delete(TempFolderPath, true);
			}
		}

	}
}

using PurgeTemp.Controller;
using PurgeTempTest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeTempTest
{
	public class PurgeTest : IClassFixture<TempFolderFixture>
	{
		private readonly TempFolderFixture fixture;

		public PurgeTest(TempFolderFixture fixture)
		{
			this.fixture = fixture;
		}

		[Fact(DisplayName = "Test of moving files from stage 1 to stage 2 on purge")]
		public void MoveS1To21Test()
		{
			string stageRootFolder, stageFolder1, stageFolder2, stageFolderLast;
			Dictionary<string, string> overrides = Arrange(out stageRootFolder, out stageFolder1, out stageFolder2, out stageFolderLast);

			// Place two files in stage 1
			string file1Path = FileUtils.CreateFile(stageFolder1, "file1.txt", "Content of file 1");
			string file2Path = FileUtils.CreateFile(stageFolder1, "file2.txt", "Content of file 2");

			// Place one file in stage 2
			string file3Path = FileUtils.CreateFile(stageFolder2, "file3.txt", "Content of file 3");

			Settings testSettings = TestSettingsProvider.GetSettings(stageRootFolder, overrides);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			// Act - Perform the first purge
			Thread.Sleep(3000);
			purgeController.ExecutePurge();

			// Assert
			// Stage 1 should be empty
			Assert.Empty(Directory.GetFiles(stageFolder1));

			// Stage 2 should have the files from stage 1
			var filesInStage2 = Directory.GetFiles(stageFolder2);
			Assert.Contains(Path.Combine(stageFolder2, "file1.txt"), filesInStage2);
			Assert.Contains(Path.Combine(stageFolder2, "file2.txt"), filesInStage2);

			// Last stage should have the file that was in stage 2
			var filesInStageLast = Directory.GetFiles(stageFolderLast);
			Assert.Contains(Path.Combine(stageFolderLast, "file3.txt"), filesInStageLast);
		}

		[Fact(DisplayName = "Test of purging a file after the last stage was reached")]
		public void LastPurgeTest()
		{
			string stageRootFolder, stageFolder1, stageFolder2, stageFolderLast;
			Dictionary<string, string> overrides = Arrange(out stageRootFolder, out stageFolder1, out stageFolder2, out stageFolderLast);

			// Place two files in stage 1
			string file1Path = FileUtils.CreateFile(stageFolder1, "file1.txt", "Content of file 1");

			Settings testSettings = TestSettingsProvider.GetSettings(stageRootFolder, overrides);
			PurgeController purgeController = Program.GetTestEnvironment(testSettings);

			// Assert - preconditions
			// Stage 1 should be contain the file
			var filesInStage1 = Directory.GetFiles(stageFolder1);
			Assert.Contains(Path.Combine(stageFolder1, "file1.txt"), filesInStage1);
			// Stage 2 should be empty
			Assert.Empty(Directory.GetFiles(stageFolder2));
			// Last stage should be empty too
			Assert.Empty(Directory.GetFiles(stageFolderLast));

			// Act 1 - Perform the first purge
			Thread.Sleep(3000);
			purgeController.ExecutePurge();

			// Assert
			// Stage 1 should be empty
			Assert.Empty(Directory.GetFiles(stageFolder1));
			// Stage 2 should have the files from stage 1
			var filesInStage2 = Directory.GetFiles(stageFolder2);
			Assert.Contains(Path.Combine(stageFolder2, "file1.txt"), filesInStage2);
			// Last stage should be empty
			Assert.Empty(Directory.GetFiles(stageFolderLast));

			// Act 2 - Perform the second purge
			Thread.Sleep(3000);
			purgeController.ExecutePurge();

			// Assert
			// Stage 1 should be empty
			Assert.Empty(Directory.GetFiles(stageFolder1));
			// Stage 2 should now be empty too
			Assert.Empty(Directory.GetFiles(stageFolder2));
			// Last stage should be empty
			var filesInLastStage = Directory.GetFiles(stageFolderLast);
			Assert.Contains(Path.Combine(stageFolderLast, "file1.txt"), filesInLastStage);

			// Act 3 - Perform the third purge
			Thread.Sleep(3000);
			purgeController.ExecutePurge();

			// Assert
			// Stage 1 should be empty
			Assert.Empty(Directory.GetFiles(stageFolder1));
			// Stage 2 should now be empty too
			Assert.Empty(Directory.GetFiles(stageFolder2));
			// Last stage should now be empty too
			Assert.Empty(Directory.GetFiles(stageFolderLast));
		}

		private Dictionary<string, string> Arrange(out string stageRootFolder, out string stageFolder1, out string stageFolder2, out string stageFolderLast)
		{
			// Arrange
			stageRootFolder = fixture.CreateUniqueTempFolder();
			string stageNamePrefix = "purge-temp";
			string stageVersionDelimiter = "_";

			stageFolder1 = Path.Combine(stageRootFolder, stageNamePrefix + stageVersionDelimiter + "1");
			stageFolder2 = Path.Combine(stageRootFolder, stageNamePrefix + stageVersionDelimiter + "2");
			stageFolderLast = Path.Combine(stageRootFolder, stageNamePrefix + stageVersionDelimiter + "last");

			// Create test settings with override
			Dictionary<string, string> overrides = new Dictionary<string, string>
		{
			{ Settings.Keys.StageRootFolder, stageRootFolder },
			{ Settings.Keys.StageNamePrefix, stageNamePrefix },
			{ Settings.Keys.StageVersionDelimiter, stageVersionDelimiter },
			{ Settings.Keys.StagingDelaySeconds, "2" },
			{ Settings.Keys.StageVersions, "3" } // Assuming we have 3 stages
        };
			Directory.CreateDirectory(stageFolder1);
			Directory.CreateDirectory(stageFolder2);
			Directory.CreateDirectory(stageFolderLast);
			return overrides;
		}
	}
}

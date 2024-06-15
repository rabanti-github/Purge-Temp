/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2024
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using PurgeTemp.Interface;

namespace PurgeTemp.Utils
{
	/// <summary>
	/// Utils class to handle file operations
	/// </summary>
	public class FileUtils
	{
		private readonly ISettings settings;
		private	readonly IPurgeLogger purgeLogger;

		public FileUtils(ISettings settings, IPurgeLogger purgeLogger)
		{
			this.settings = settings;
			this.purgeLogger = purgeLogger;
		}	

		public void LogFilesToProcess(List<string> folders, string currentFolder)
		{
			// Determine the index of the current folder in the list
			int currentIndex = folders.IndexOf(currentFolder);

			// Determine if the current folder is the last folder
			bool isLastFolder = currentIndex == folders.Count - 1;

			// Determine the threshold for logging
			int fileLogAmountThreshold = settings.FileLogAmountThreshold;

			// Recursively determine all files in the current folder
			List<string> allFiles = Directory.GetFiles(currentFolder, "*", SearchOption.AllDirectories).ToList();

			// Log files based on the threshold and position of the current folder
			int filesLogged = 0;
			while (filesLogged < allFiles.Count)
			{
				// Determine the number of files to log in this batch
				int filesToLog = Math.Min(fileLogAmountThreshold, allFiles.Count - filesLogged);

				// Log each file in the batch
				for (int i = filesLogged; i < filesLogged + filesToLog; i++)
				{
					string file = allFiles[i];
					if (isLastFolder)
					{
						purgeLogger.PurgeInfo(currentFolder, file);
					}
					else
					{
						// Determine the target folder for moving files
						string targetFolder = folders[currentIndex + 1];

						purgeLogger.MoveInfo(currentFolder, targetFolder, file);
					}
				}

				filesLogged += filesToLog;
			}

			// Log a single entry indicating that remaining files were not logged due to threshold exceedance
			int skippedFiles = allFiles.Count - filesLogged;
			if (skippedFiles > 0)
			{
				if (isLastFolder)
				{
					purgeLogger.SkipPurgeInfo(currentFolder, skippedFiles);
				}
				else
				{
					string targetFolder = folders[currentIndex + 1];
					purgeLogger.SkipMoveInfo(currentFolder, targetFolder, skippedFiles);
				}
			}
		}
	}
}

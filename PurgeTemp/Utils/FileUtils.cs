/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2024
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using PurgeTemp.Interface;
using PurgeTemp.Logger;

namespace PurgeTemp.Utils
{
	/// <summary>
	/// Utils class to handle file operations
	/// </summary>
	public class FileUtils
	{
		private readonly ISettings settings;
		private readonly ILoggerFactory loggerFactory;
		private IPurgeLogger? purgeLogger;
		private IPurgeLogger PurgeLogger => purgeLogger ??= loggerFactory.CreatePurgeLogger();

		public FileUtils(ISettings settings, ILoggerFactory loggerFactory)
		{
			this.settings = settings;
			this.loggerFactory = loggerFactory;
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

			int filesLogged = 0;
			foreach (string file in allFiles)
			{
				if (filesLogged >= fileLogAmountThreshold)
				{
					break;
				}
				if (isLastFolder)
				{
					PurgeLogger.PurgeInfo(currentFolder, file.Substring(currentFolder.Length - 1));
				}
				else
				{
					// Determine the target folder for moving files
					string targetFolder = folders[currentIndex + 1];

					PurgeLogger.MoveInfo(currentFolder, targetFolder, file.Substring(targetFolder.Length - 1));
				}
				filesLogged++;
			}

			// Log a single entry indicating that remaining files were not logged due to threshold exceedance
			int skippedFiles = allFiles.Count - filesLogged;
			if (skippedFiles > 0)
			{
				if (isLastFolder)
				{
					if (fileLogAmountThreshold == 0)
					{
						PurgeLogger.SkipPurgeInfo(currentFolder, skippedFiles, true);
					}
					else
					{
						PurgeLogger.SkipPurgeInfo(currentFolder, skippedFiles);
					}
				}
				else
				{
					string targetFolder = folders[currentIndex + 1];
					if (fileLogAmountThreshold == 0)
					{
						PurgeLogger.SkipMoveInfo(currentFolder, targetFolder, skippedFiles, true);
					}
					else
					{
						PurgeLogger.SkipMoveInfo(currentFolder, targetFolder, skippedFiles);
					}
				}
			}
		}
	}
}

/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2024
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using PurgeTemp.Interface;
using PurgeTemp.Utils;

namespace PurgeTemp.Controller
{
	/// <summary>
	/// Class to handle the main logic of the application
	/// </summary>
	public class PurgeController
	{
		public enum ExecutionEvaluation
		{
			CanExecute,
			InvalidArguments,
			TimeSinceLastPurgeTooShort,
			SkippedByToken,
			OtherErrors
		}


		private readonly ISettings settings;
		private readonly IAppLogger appLogger;
		private readonly IPurgeLogger purgeLogger;
		private readonly IDesktopNotification desktopNotification;
		private readonly FileUtils fileUtils;
		private readonly PathUtils pathUtils;

		public PurgeController(IAppLogger appLogger, IPurgeLogger purgeLogger, ISettings settings, IDesktopNotification desktopNotification)
		{
			this.appLogger = appLogger;
			this.purgeLogger = purgeLogger;
			this.settings = settings;
			this.desktopNotification = desktopNotification;
			this.fileUtils =  new FileUtils(settings, purgeLogger);	
			this.pathUtils = new PathUtils(settings, appLogger);
		}

		public void ExecutePurge()
		{
			try
			{
				// Maintain all configured folders
				MaintainAdministrativeFolders();
				// Check if purge can be executed
				ExecutionEvaluation evaluation = CanExecutePurge();
				if (evaluation != ExecutionEvaluation.CanExecute)
				{
					switch (evaluation)
					{	
						case ExecutionEvaluation.InvalidArguments:
							appLogger.Error("The purge execution cannot be performed due to invalid argument(s)");
							break;
						case ExecutionEvaluation.TimeSinceLastPurgeTooShort:
							appLogger.Information("The time since the last purge is too short. The purge was skipped");
							desktopNotification.ShowNotification("Purge not executed", "The time since the last purge is too short. The purge was skipped", IDesktopNotification.Status.SKIP);
							// TODO add desktop notification
							break;
						case ExecutionEvaluation.SkippedByToken:
							appLogger.Information($"The purge was skipped by a token ({settings.SkipTokenFile}) , manually added to the primary purge folder");
							desktopNotification.ShowNotification("Purge not executed", $"The purge was skipped by a token ({settings.SkipTokenFile}) , manually added to the primary purge folder", IDesktopNotification.Status.SKIP);
							// TODO add desktop notification
							break;
						case ExecutionEvaluation.OtherErrors:
							appLogger.Error("The purge execution cannot be performed due to other errors");
							desktopNotification.ShowNotification("Purge not executed", "The purge execution cannot be performed due to other errors", IDesktopNotification.Status.ERROR);
							break;
						default:
							break;
					}
					return;
				}

				// Get the initial stage folder
				string initStageFolder = pathUtils.GetInitStageFolder();

				// Get the list of all stage folders
				List<string> allStageFolders = pathUtils.GetStageFolders();

				// Ensure all intermediate folders exist
				for (int i = 0; i < allStageFolders.Count; i++)
				{
					if (!Directory.Exists(allStageFolders[i]))
					{
						Directory.CreateDirectory(allStageFolders[i]);
					}
				}

				// Get the list of existing stage folders
				List<string> stageFolders = allStageFolders.Where(Directory.Exists).ToList();

				// Log and delete files in the last folder
				if (stageFolders.Count > 0)
				{
					string lastStageFolder = stageFolders.Last();
					fileUtils.LogFilesToProcess(stageFolders, lastStageFolder);
					try
					{
						Directory.Delete(lastStageFolder, true);
					}
					catch (Exception ex)
					{
						appLogger.Error($"Failed to delete the last folder '{lastStageFolder}': {ex.Message}");
						return;
					}
				}

				// Rename folders from the last to the first
				for (int i = stageFolders.Count - 1; i > 0; i--)
				{
					string currentFolder = stageFolders[i - 1];
					string nextFolder = stageFolders[i];

					// Log and move files from current folder to next folder
					fileUtils.LogFilesToProcess(stageFolders, currentFolder);

					// Rename current folder to next folder
					try
					{
						Directory.Move(currentFolder, nextFolder);
					}
					catch (Exception ex)
					{
						appLogger.Error($"Failed to rename folder '{currentFolder}' to '{nextFolder}': {ex.Message}");
						return;
					}
				}
				// Cleanup empty folders
				if (settings.RemoveEmptyStageFolders)
				{
					for (int i = 1; i < stageFolders.Count; i++)
					{
						if (Directory.Exists(stageFolders[i]))
						{
							if (Directory.GetFiles(stageFolders[i]).Length == 0)
							{
								Directory.Delete(stageFolders[i]);
							}

						}
					}
				}
					// Create a new empty first folder
					try
				{
					Directory.CreateDirectory(initStageFolder);
					WriteLastPurgeToken();
					appLogger.Information("Purge was executed successfully");
					desktopNotification.ShowNotification("Purge completed", "Purge was executed successfully", IDesktopNotification.Status.OK);
				}
				catch (Exception ex)
				{
					appLogger.Error($"Failed to create folder '{initStageFolder}': {ex.Message}");
					desktopNotification.ShowNotification("Purge not executed", $"Failed to create folder '{initStageFolder}': {ex.Message}", IDesktopNotification.Status.ERROR);
					return;
				}
			}
			catch (Exception ex)
			{
				appLogger.Error($"An error occurred during purge execution: {ex.Message}");
				desktopNotification.ShowNotification("Purge not executed", $"An error occurred during purge execution: {ex.Message}", IDesktopNotification.Status.ERROR);
				return;
			}
		}

		public void MaintainAdministrativeFolders()
		{
			string configFolder = pathUtils.GetPath(settings.ConfigFolder);
			if (!Directory.Exists(configFolder))
			{
				Directory.CreateDirectory(configFolder);
			}
			string tempFolder = pathUtils.GetPath(settings.TempFolder);
			if (!Directory.Exists(tempFolder))
			{
				Directory.CreateDirectory(tempFolder);
			}
			if (settings.LogEnabled)
			{
				string logBaseFolder = pathUtils.GetPath(settings.LoggingFolder);
				if (!Directory.Exists(logBaseFolder))
				{
					Directory.CreateDirectory(logBaseFolder);
				}
			}
		}

		public ExecutionEvaluation CanExecutePurge()
		{
			try
			{
				string initStageFolder = pathUtils.GetInitStageFolder();
				string skipTokenPath = pathUtils.GetPath(initStageFolder, settings.SkipTokenFile);
				string lastPurgeToken = pathUtils.GetPath(settings.ConfigFolder, settings.StagingTimestampFile);
				int numberOfSecondsBetweenPurge = settings.StagingDelaySeconds;

				// Check if any path is null
				if (skipTokenPath == null || lastPurgeToken == null || initStageFolder == null)
				{
					return ExecutionEvaluation.InvalidArguments;
				}

				// Check if the skip token file exists in the initial temp folder
				if (File.Exists(skipTokenPath))
				{
					appLogger.Information("Purge skipped due to skip token file presence.");
					return ExecutionEvaluation.SkippedByToken;
				}

				// Check if the last purge timestamp file exists
				if (File.Exists(lastPurgeToken))
				{
					// Read the last purge timestamp from the file
					string lastPurgeTimestamp = File.ReadAllText(lastPurgeToken);

					// Parse the timestamp string into a DateTime object
					if (DateTime.TryParseExact(lastPurgeTimestamp, settings.TimeStampFormat,
												null, System.Globalization.DateTimeStyles.None,
												out DateTime lastPurgeTime))
					{
						// Calculate the time elapsed since the last purge
						TimeSpan timeSinceLastPurge = DateTime.Now - lastPurgeTime;

						// Check if the time elapsed since the last purge is greater than the defined delay
						if (timeSinceLastPurge.TotalSeconds < numberOfSecondsBetweenPurge)
						{
							appLogger.Information("Purge skipped due to insufficient time elapsed since last purge.");
							return ExecutionEvaluation.TimeSinceLastPurgeTooShort;
						}
					}
					else
					{
						appLogger.Error("Failed to parse last purge timestamp. Please checkt the settings and manually delete or fix the token, defined at: " + lastPurgeToken);
						return ExecutionEvaluation.InvalidArguments;
					}
				}

				// If all conditions are met, perform the purge
				return ExecutionEvaluation.CanExecute;
			}
			catch (Exception ex) 
			{
				appLogger.Error($"An unknown error occurred: {ex.Message}");
				return ExecutionEvaluation.OtherErrors;
			}

		}

		public void WriteLastPurgeToken()
		{
			try
			{
				// Get the path for the last purge token file
				string lastPurgeTokenPath = pathUtils.GetPath(settings.ConfigFolder, settings.StagingTimestampFile);

				// Get the current timestamp in the specified format
				string currentTimestamp = DateTime.Now.ToString(settings.TimeStampFormat);

				// Write the timestamp to the last purge token file
				File.WriteAllText(lastPurgeTokenPath, currentTimestamp);
			}
			catch (Exception ex)
			{
				appLogger.Error($"Failed to write last purge token: {ex.Message}");
			}
		}

	}
}

/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2024
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using PurgeTemp.Interface;
using PurgeTemp.Logger;
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
		private readonly IDesktopNotification desktopNotification;
		private readonly ILoggerFactory loggerFactory;
		private readonly FileUtils fileUtils;
		private readonly PathUtils pathUtils;
		private IAppLogger? appLogger;

		private IAppLogger AppLogger => appLogger ??= loggerFactory.CreateAppLogger();

		public ExecutionEvaluation ExecutionState { get; set; }

		public PurgeController(ILoggerFactory loggerFactory, ISettings settings)
		{
			this.settings = settings;
			this.loggerFactory = loggerFactory;
			this.fileUtils = new FileUtils(settings, loggerFactory);
			this.pathUtils = new PathUtils(settings, loggerFactory);
			this.desktopNotification = new DesktopNotification(settings, this.pathUtils);
		}

		public int ExecutePurge()
		{
			int errorCode = ErrorCodes.Success;
			try
			{
				Result result = ValidateGeneralSettings();
				if (result.IsNotValid)
				{
					this.ExecutionState = ExecutionEvaluation.InvalidArguments;
					desktopNotification.ShowNotification("Settings validation failed", $"The validation of the settings failed with code {result.ErrorCode}. Enable/see logs for details.", IDesktopNotification.Status.ERROR);
					return result.ErrorCode;
				}
				// Maintain all configured folders
				result = MaintainAdministrativeFolders();
				if (result.IsNotValid)
				{
					this.ExecutionState = ExecutionEvaluation.InvalidArguments;
					desktopNotification.ShowNotification("Preparation failed", $"The administrative preparation failed with code {result.ErrorCode}. Enable/see logs for details.", IDesktopNotification.Status.ERROR);
					return result.ErrorCode;
				}
				result = CheckStageFolders();
				if (result.IsNotValid)
				{
					this.ExecutionState = ExecutionEvaluation.InvalidArguments;
					desktopNotification.ShowNotification("Preparation failed", $"The purge folder definition is invalid and returned code {result.ErrorCode}. Enable/see logs for details.", IDesktopNotification.Status.ERROR);
					return result.ErrorCode;
				}
				// Check if purge can be executed
				this.ExecutionState = CanExecutePurge();
				if (this.ExecutionState != ExecutionEvaluation.CanExecute)
				{
					switch (this.ExecutionState)
					{
						case ExecutionEvaluation.InvalidArguments:
							AppLogger.Error("The purge execution cannot be performed due to invalid argument(s)");
							desktopNotification.ShowNotification("Purge not executed", "The purge execution cannot be performed due to invalid argument(s)", IDesktopNotification.Status.ERROR);
							errorCode = ErrorCodes.InvalidArguments;
							break;
						case ExecutionEvaluation.TimeSinceLastPurgeTooShort:
							AppLogger.Information("The time since the last purge is too short. The purge was skipped");
							desktopNotification.ShowNotification("Purge not executed", "The time since the last purge is too short. The purge was skipped", IDesktopNotification.Status.SKIP);
							errorCode = ErrorCodes.ExecutionTooFrequent;
							break;
						case ExecutionEvaluation.SkippedByToken:
							AppLogger.Information($"The purge was skipped by a token ({settings.SkipTokenFile}) , manually added to the primary purge folder");
							desktopNotification.ShowNotification("Purge not executed", $"The purge was skipped by a token ({settings.SkipTokenFile}) , manually added to the primary purge folder", IDesktopNotification.Status.SKIP);
							errorCode = ErrorCodes.SkipTokenFound;
							break;
						case ExecutionEvaluation.OtherErrors:
							AppLogger.Error("The purge execution cannot be performed due to other errors");
							desktopNotification.ShowNotification("Purge not executed", "The purge execution cannot be performed due to other errors", IDesktopNotification.Status.ERROR);
							errorCode = ErrorCodes.UnknownError;
							break;
						default:
							break;
					}
					return errorCode;
				}

				// Get the list of all stage folders
				Result<List<string>> allStageFolders = pathUtils.GetStageFolders();
				if (allStageFolders.IsNotValid)
				{
					return allStageFolders.ErrorCode;
				}

				// Ensure all intermediate folders exist
				for (int i = 0; i < allStageFolders.Value.Count; i++)
				{
					Result createValidation = pathUtils.CreateFolder(allStageFolders.Value[i]);
					if (createValidation.IsNotValid)
					{
						AppLogger.Error($"Could not create the stage folder '{allStageFolders.Value[i]}' (error code {createValidation.ErrorCode})");
						desktopNotification.ShowNotification("Purge not executed", $"Could not create the stage folder '{allStageFolders.Value[i]}' (error code {createValidation.ErrorCode})", IDesktopNotification.Status.ERROR);
						return errorCode;
					}
				}

				// Get the list of existing stage folders
				List<string> stageFolders = allStageFolders.Value.Where(Directory.Exists).ToList();

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
						AppLogger.Error($"Failed to delete the last folder '{lastStageFolder}': {ex.Message}");
						errorCode = ErrorCodes.CouldNotDeleteLastFolder;
						return errorCode;
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
						AppLogger.Error($"Failed to rename folder '{currentFolder}' to '{nextFolder}': {ex.Message}");
						desktopNotification.ShowNotification("Error during purge", $"Failed to rename folder '{currentFolder}' to '{nextFolder}': {ex.Message}", IDesktopNotification.Status.ERROR);
						errorCode = ErrorCodes.CouldNotRenameStageFolder;
						return errorCode;
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
				Result<string> initStageValidation = pathUtils.GetInitStageFolder();
				if (initStageValidation.IsNotValid)
				{
					return initStageValidation.ErrorCode;
				}
				result = pathUtils.CreateFolder(initStageValidation.Value);
				if (result.IsNotValid)
				{
					AppLogger.Error($"Could not create initial purge folder {initStageValidation.Value} (error code {result.ErrorCode})");
					desktopNotification.ShowNotification("Purge not executed", $"Could not create initial purge folder {initStageValidation.Value} (error code {result.ErrorCode})", IDesktopNotification.Status.ERROR);
					return result.ErrorCode;
				}
				result = WriteLastPurgeToken();
				if (result.IsNotValid)
				{
					AppLogger.Error($"Could not write the last purge token (error code {result.ErrorCode})");
					desktopNotification.ShowNotification("Purge execution incomplete", $"Could not write the last purge token (error code {result.ErrorCode})", IDesktopNotification.Status.ERROR);
					return result.ErrorCode;
				}
				AppLogger.Information("Purge was executed successfully");
				desktopNotification.ShowNotification("Purge completed", "Purge was executed successfully", IDesktopNotification.Status.OK);
			}
			catch (Exception ex)
			{
				AppLogger.Error($"An error occurred during purge execution: {ex.Message}");
				desktopNotification.ShowNotification("Purge not executed", $"An error occurred during purge execution: {ex.Message}", IDesktopNotification.Status.ERROR);
				errorCode = ErrorCodes.UnknownError;
				return errorCode;
			}
			return errorCode;
		}

		public Result ValidateGeneralSettings()
		{
			if (settings.StageVersions < 1)
			{
				AppLogger.Error($"The number of stages cannot be zero or negative");
				return Result<string>.Fail(ErrorCodes.InvalidNumberOfStages);
			}
			if (settings.FileLogAmountThreshold < 0)
			{
				AppLogger.Error($"The number of files to skip on purge logging cannot be negative");
				return Result<string>.Fail(ErrorCodes.InvalidFileLogAmount);
			}
			return Result.Success();
		}

		public Result MaintainAdministrativeFolders()
		{
			string configFolder = pathUtils.GetPath(settings.ConfigFolder);
			Result configFolderValidation = CheckAdministrativeFolder(configFolder);
			if (configFolderValidation.IsNotValid)
			{
				return Result.Fail(configFolderValidation.ErrorCode);
			}
			Result result = pathUtils.CreateFolder(configFolder, true);
			if (result.IsNotValid)
			{
				return Result.Fail(result.ErrorCode);
			}

			string tempFolder = pathUtils.GetPath(settings.TempFolder);
			Result tempFolderValidation = CheckAdministrativeFolder(tempFolder);
			if (tempFolderValidation.IsNotValid)
			{
				return Result.Fail(tempFolderValidation.ErrorCode);
			}
			result = pathUtils.CreateFolder(tempFolder, true);
			if (result.IsNotValid)
			{
				return Result.Fail(result.ErrorCode);
			}
			if (settings.LogEnabled)
			{
				string logBaseFolder = pathUtils.GetPath(settings.LoggingFolder);
				Result logFolderValidation = CheckAdministrativeFolder(logBaseFolder);
				if (logFolderValidation.IsNotValid)
				{
					return Result.Fail(logFolderValidation.ErrorCode);
				}
				result = pathUtils.CreateFolder(logBaseFolder, true);
				if (result.IsNotValid)
				{
					return Result.Fail(result.ErrorCode);
				}
			}
			return Result.Success();
		}

		private Result CheckAdministrativeFolder(string folder)
		{
			Result adminFolderValidation = pathUtils.IsValidFolderName(folder, false);
			if (adminFolderValidation.IsNotValid)
			{
				return Result.Fail(adminFolderValidation.ErrorCode);
			}
			Result<List<string>> allStageFolders = pathUtils.GetStageFolders();
			if (allStageFolders.IsNotValid)
			{
				return Result.Fail(allStageFolders.ErrorCode);
			}
			string normalizedPath = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			// Check if the path matches any of the stage folders
			if (allStageFolders.Value.Contains(normalizedPath))
			{
				AppLogger.Error($"Specified administrative path would lead to a conflict with a defined stage folder: " + normalizedPath);
				return Result.Fail(ErrorCodes.AdministrativePathConflictsWithStagePath);
			}
			return Result.Success();
		}

		private Result CheckStageFolders()
		{
			Result<List<string>> allStageFolders = pathUtils.GetStageFolders();
			if (allStageFolders.IsNotValid)
			{
				return Result.Fail(allStageFolders.ErrorCode);
			}
			foreach (string stageFolder in allStageFolders.Value)
			{
				Result stageValidation = pathUtils.IsValidFolderName(stageFolder, false);
				if (stageValidation.IsNotValid)
				{
					if (stageValidation.ErrorCode == ErrorCodes.PathIsSystemDirectory)
					{
						AppLogger.Error($"Specified stage folder path would lead to a protected system folder: " + stageFolder);
						return Result.Fail(ErrorCodes.StageFolderIsSystemDirectory);
					}
					else if (stageValidation.ErrorCode == ErrorCodes.ReservedNameAsFolderName)
					{
						AppLogger.Error($"Specified stage folder path would lead to a folder with a reserved name: " + stageFolder);
						return Result.Fail(ErrorCodes.StageFolderHasReservedFolderName);
					}
					else
					{
						AppLogger.Error($"Specified stage folder path is invalid:" + stageFolder);
						return Result.Fail(stageValidation.ErrorCode);
					}
				}
			}
			return Result.Success();
		}

		public ExecutionEvaluation CanExecutePurge()
		{
			try
			{
				Result<string> initStageValidation = pathUtils.GetInitStageFolder();
				if (initStageValidation.IsNotValid)
				{
					return ExecutionEvaluation.InvalidArguments;
				}
				string skipTokenPath = pathUtils.GetPath(initStageValidation.Value, settings.SkipTokenFile);
				string lastPurgeToken = pathUtils.GetPath(settings.ConfigFolder, settings.StagingTimestampFile);
				int numberOfSecondsBetweenPurge = settings.StagingDelaySeconds;

				// Check if any path is null
				if (skipTokenPath == null || lastPurgeToken == null || initStageValidation.Value == null)
				{
					AppLogger.Error(string.Format("At least one mandatory argument was not defined:  skipTokenPath:{0}, lastPurgeToken:{1}, initStageFolder:{2}", skipTokenPath, lastPurgeToken, initStageValidation.Value));
					return ExecutionEvaluation.InvalidArguments;
				}

				// Check if the skip token file exists in the initial temp folder
				if (File.Exists(skipTokenPath))
				{
					AppLogger.Information("Purge skipped due to skip token file presence.");
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
						DateTime now = DateTime.Now;
						TimeSpan timeSinceLastPurge = now - lastPurgeTime;

						// Check if the time elapsed since the last purge is greater than the defined delay
						if (timeSinceLastPurge.TotalSeconds < numberOfSecondsBetweenPurge)
						{
							AppLogger.Information("Purge skipped due to insufficient time elapsed since last purge. Last purge: " + lastPurgeTimestamp + ", Now: " + now.ToString(settings.TimeStampFormat) + ", Min. seconds: " + numberOfSecondsBetweenPurge + ", Actual seconds: " + timeSinceLastPurge.TotalSeconds);
							return ExecutionEvaluation.TimeSinceLastPurgeTooShort;
						}
					}
					else
					{
						AppLogger.Error("Failed to parse last purge timestamp. Please checkt the settings and manually delete or fix the token, defined at: " + lastPurgeToken);
						return ExecutionEvaluation.InvalidArguments;
					}
				}

				// If all conditions are met, perform the purge
				return ExecutionEvaluation.CanExecute;
			}
			catch (Exception ex)
			{
				AppLogger.Error($"An unknown error occurred: {ex.Message}");
				return ExecutionEvaluation.OtherErrors;
			}
		}

		public Result WriteLastPurgeToken()
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
				AppLogger.Error($"Failed to write last purge token: {ex.Message}");
				return Result.Fail(ErrorCodes.CouldNotCreateLastPurgeToken);
			}
			return Result.Success();
		}
	}
}

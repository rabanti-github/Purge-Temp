/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2026
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using PurgeTemp.Controller;
using PurgeTemp.Interface;
using System.Reflection;

namespace PurgeTemp.Utils
{
	/// <summary>
	/// Utils class to handle directory or path operations
	/// </summary>
	public class PathUtils
	{
        public const string DEFAULT_DELIMITER = "-";
        public const string DEFAULT_TEMP_FOLDER_NAME = "purge-temp";
        public const string DEFAULT_LAST_FOLDER_NAME_TOKEN = "LAST";
        public static readonly List<string> TestSystemPathTokens = new List<string>()
		{
			"C:\\", "NOT_", "EXISTING_", "PROTECTED_", "SYSTEM_", "PATH_", "FOR_", "TESTING"
		};

		public static readonly string TestSystemPath = String.Join("", TestSystemPathTokens);

		public static readonly HashSet<string> ReservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"CON", "PRN", "AUX", "NUL",
			"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
			"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
		};

		private static HashSet<string> InitializeSystemPaths()
		{
			var systemPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			Environment.GetFolderPath(Environment.SpecialFolder.Windows),
			Environment.GetFolderPath(Environment.SpecialFolder.System),
			Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
			Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
			Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
			Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86),
			Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
			Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
			TestSystemPath
		};

			return systemPaths;
		}

		private static readonly HashSet<string> SystemPaths = InitializeSystemPaths();

		private readonly ISettings settings;
		private readonly ILoggerFactory loggerFactory;
		private IAppLogger? appLogger;
		private IAppLogger AppLogger => appLogger ??= loggerFactory.CreateAppLogger();


		public PathUtils(ISettings settings, ILoggerFactory loggerFactory)//, IAppLogger appLogger)
		{
			this.settings = settings;
			this.loggerFactory = loggerFactory;
		}

		public string GetPath(string token)
		{
			string? currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			return GetPath(currentPath, token);
		}

		public string GetPath(string basePath, string token)
		{
			try
			{
				if (string.IsNullOrEmpty(basePath) && string.IsNullOrEmpty(token))
				{
					return null;
				}

				// Handle the base path if it starts with a relative indicator
				if (!string.IsNullOrEmpty(basePath))
				{
					if (basePath.StartsWith(".\\") || basePath.StartsWith("./") || basePath.StartsWith("../") || basePath.StartsWith("..\\"))
					{
						basePath = GetPath(basePath);
					}
				}

				// If the token is null or empty, return the base path
				if (string.IsNullOrEmpty(token))
				{
					return basePath;
				}

				// Determine if the token is a relative path
				bool isRelative = !Path.IsPathRooted(token) && !token.StartsWith("/") && !token.StartsWith("\\");

				if (isRelative)
				{
					// Treat token as a relative path
					if (token.StartsWith("./") || token.StartsWith(".\\"))
					{
						return CombineRelativePath(basePath, token, ".\\", "./");
					}
					else if (token.StartsWith("../") || token.StartsWith("..\\"))
					{
						DirectoryInfo directoryInfo = new DirectoryInfo(basePath);
						string parentPath = directoryInfo.Parent.FullName;
						return CombineRelativePath(parentPath, token, "../", "..\\");
					}
					else
					{
						// Treat as relative path without any leading indicators
						return CombineRelativePath(basePath, token);
					}
				}
				else
				{
					return token; // Absolute path
				}
			}
			catch (Exception ex)
			{
				AppLogger.Error($"Error occurred while retrieving path: {ex.Message}");
				return null;
			}
		}

		private string CombineRelativePath(string basePath, string token, params string[] pathMarkers)
		{
			string appendix = token;
			foreach (string prefix in pathMarkers)
			{
				if (token.StartsWith(prefix))
				{
					appendix = token.Substring(prefix.Length);
					break;
				}
			}
			return Path.Combine(basePath, appendix);
		}

		public Result CreateFolder(string path, bool isStageFolder = true)
		{
			try
			{
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
			}
			catch (Exception ex)
			{
				AppLogger.Error($"Could not create folder '{path}': {ex.Message}");
				if (isStageFolder)
				{
					return Result.Fail(ErrorCodes.CouldNotCreateNewStageFolder);
				}
				else
				{
					return Result.Fail(ErrorCodes.CouldNotCreateAdministrativeFolder);
				}
			}
			return Result.Success();
		}

		public Result<string> GetInitStageFolder()
		{
			try
			{
				string rootFolder = settings.StageRootFolder;
				string namePrefix = settings.StageNamePrefix;
				bool appendNumberOnFirstStage = settings.AppendNumberOnFirstStage;
				string versionDelimiter = settings.StageVersionDelimiter;
				int stageVersions = settings.StageVersions;

				// Sanitize settings
				(string versionDelimiter, string namePrefix) sanitizedSettings = SanitizeSettings(versionDelimiter, namePrefix);

				string initialFolder;

				// If the number should be appended on the first stage and it's not zero-based
				if (appendNumberOnFirstStage && stageVersions > 0)
				{
					initialFolder = $"{sanitizedSettings.namePrefix}{sanitizedSettings.versionDelimiter}1";
				}
				else
				{
					initialFolder = sanitizedSettings.namePrefix;
				}

				return Result<string>.Success(Path.Combine(rootFolder, initialFolder));
			}
			catch (Exception ex)
			{
				AppLogger.Error($"An error occurred while retrieving initial stage folder: {ex.Message}");
				return Result<string>.Fail(ErrorCodes.UnknownError); ;
			}
		}

		public Result<List<string>> GetStageFolders()
		{
			List<string> stageFolders = new List<string>();

			// Get the initial stage folder
			Result<string> initStageValidation = GetInitStageFolder();
			if (initStageValidation.IsNotValid)
			{
				return Result<List<string>>.Fail(initStageValidation.ErrorCode);
			}
			stageFolders.Add(initStageValidation.Value); // Already validated

			string rootFolder = settings.StageRootFolder;
			string namePrefix = settings.StageNamePrefix;
			string versionDelimiter = settings.StageVersionDelimiter;
			string lastNameSuffix = settings.StageLastNameSuffix;
			int stageVersions = settings.StageVersions;

			// Sanitize settings
			(string versionDelimiter, string namePrefix, string lastNameSuffix) sanitizedSettings = SanitizeSettings(versionDelimiter, namePrefix, lastNameSuffix);


			// If zero or one stage versions are configured, return only the initial stage folder
			if (stageVersions <= 1)
			{
				return Result<List<string>>.Success(stageFolders); // TODO: is this correct / other error code?
			}

			// Add additional stage folders to the list
			for (int i = 2; i <= stageVersions - 1; i++)
			{
				string stageFolder = $"{sanitizedSettings.namePrefix}{sanitizedSettings.versionDelimiter}{i}";
				string absolutePath = Path.Combine(rootFolder, stageFolder);
				stageFolders.Add(absolutePath);
			}

			// Append the last folder with the last name suffix
			string lastStageFolder = $"{sanitizedSettings.namePrefix}{sanitizedSettings.versionDelimiter}{sanitizedSettings.lastNameSuffix}";
			string lastStageFolderPath = Path.Combine(rootFolder, lastStageFolder);
			stageFolders.Add(lastStageFolderPath);

			return Result<List<string>>.Success(stageFolders);
		}

		public (string delimiter, string prefix) SanitizeSettings(string delimiter, string prefix)
		{
			string dummy = DEFAULT_LAST_FOLDER_NAME_TOKEN;
			(string delimiter, string prefix, string suffixForLast) value = SanitizeSettings(delimiter, prefix, dummy);
			return (value.delimiter, value.prefix);
		}

		public (string delimiter, string prefix, string suffixForLast) SanitizeSettings(string delimiter, string prefix, string suffixForLast)
		{
			// Check and sanitize delimiter
			if (string.IsNullOrEmpty(delimiter))
			{
				AppLogger.Information($"Valid, empty delimiter detected. Replacing potential null with empty.");
				delimiter = "";
			}
			else if (delimiter.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
			{
				AppLogger.Warning($"Invalid characters in delimiter '{delimiter}' detected.  Replacing with '" + DEFAULT_DELIMITER +"'.");
				delimiter = DEFAULT_DELIMITER;
			}
			else if (ReservedNames.Contains(delimiter))
			{
				AppLogger.Warning($"Reserved name '{delimiter}' detected as delimiter.  Replacing with '" + DEFAULT_DELIMITER +"'.");
				delimiter = DEFAULT_DELIMITER;
			}

			// Check and sanitize prefix
			Result prefixValidation = IsValidFolderName(prefix, true, "Replacing with '"+ DEFAULT_TEMP_FOLDER_NAME +"'.");
			if (prefixValidation.IsNotValid)
			{
				prefix = DEFAULT_TEMP_FOLDER_NAME;
			}

			// Check and sanitize suffix for last
			Result lastSuffixValidation = IsValidFolderName(suffixForLast, true, "Replacing with '"+ DEFAULT_LAST_FOLDER_NAME_TOKEN+ "'.");
			if (lastSuffixValidation.IsNotValid)
			{
				suffixForLast = DEFAULT_LAST_FOLDER_NAME_TOKEN;
			}

			return (delimiter, prefix, suffixForLast);
		}

		public Result IsValidFolderName(string name, bool isWarning, string appendLogMessage = null)
		{
			try
			{
				if (string.IsNullOrEmpty(name))
				{
					WriteLogMessage($"Specified path is null.", appendLogMessage, isWarning);
					return Result.Fail(ErrorCodes.EmptyFolderName);
				}
				string trimmedName = name.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				if (string.IsNullOrEmpty(trimmedName))
				{
					WriteLogMessage($"Specified path is null.", appendLogMessage, isWarning);
					return Result.Fail(ErrorCodes.EmptyFolderName);
				}
				string lastComponent = Path.GetFileName(trimmedName);
				if (string.IsNullOrEmpty(lastComponent))
				{
					lastComponent = trimmedName;
				}
				if (lastComponent.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
				{
					WriteLogMessage($"Invalid characters in folder name '{name}' detected.", appendLogMessage, isWarning);
					return Result.Fail(ErrorCodes.IllegalCharactersInFolderName);
				}
				else if (ReservedNames.Contains(lastComponent))
				{
					WriteLogMessage($"Reserved name '{lastComponent}' detected as folder name.", appendLogMessage, isWarning);
					return Result.Fail(ErrorCodes.ReservedNameAsFolderName);
				}
				else if (name.EndsWith(" ") || name.EndsWith("."))
				{
					WriteLogMessage($"Folder '{name}' ends with a space or a dot.", appendLogMessage, isWarning);
					return Result.Fail(ErrorCodes.InvalidFolderNameSuffix);
				}
				Result systemFolderValidation = CheckSystemRelevantFolder(name);
				if (systemFolderValidation.IsValid)
				{
					return Result.Success();
				}
				else
				{
					return Result.Fail(systemFolderValidation.ErrorCode);
				}
			}
			catch (Exception ex)
			{
				WriteLogMessage($"An unknown error occurred: " + ex.Message, appendLogMessage, isWarning);
				return Result.Fail(ErrorCodes.UnknownError);
			}
		}

		/// <summary>
		/// Checks whether the provided folder path is a system-relevant folder that should not be touched.
		/// </summary>
		/// <param name="folderPath">The folder path to check.</param>
		/// <returns></returns>
		public Result CheckSystemRelevantFolder(string folderPath, bool logError = true)
		{
			if (string.IsNullOrEmpty(folderPath))
			{
				if (logError)
				{
					WriteLogMessage($"Specified path is null.", null, false);
				}
				return Result.Fail(ErrorCodes.EmptyFolderName);
			}

			// Normalize the path to ensure consistency
			string normalizedPath = Path.GetFullPath(folderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			// Check if the path matches any of the known system-relevant paths
			if (SystemPaths.Contains(normalizedPath))
			{
				if (logError)
				{
					WriteLogMessage($"Specified path is a system relevant path and may not be created or deleted.", ":" + normalizedPath, false);
				}
				return Result.Fail(ErrorCodes.PathIsSystemDirectory);
			}
			return Result.Success();
		}

		private void WriteLogMessage(string message, String suffix, bool isWarning)
		{
			if (isWarning)
			{
				if (suffix != null)
				{
					AppLogger.Warning(message + " " + suffix);
				}
				else
				{
					AppLogger.Warning(message);
				}
			}
			else
			{
				if (suffix != null)
				{
					AppLogger.Error(message + " " + suffix);
				}
				else
				{
					AppLogger.Error(message);
				}
			}
		}

	}
}

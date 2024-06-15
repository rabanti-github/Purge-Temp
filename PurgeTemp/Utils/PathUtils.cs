/*
 * Purge-Temp - Staged temp file clean-up application
 * Copyright Raphael Stoeckli © 2024
 * This library is licensed under the MIT License.
 * You find a copy of the license in project folder or on: http://opensource.org/licenses/MIT
 */

using PurgeTemp.Interface;
using System.Reflection;

namespace PurgeTemp.Utils
{
	/// <summary>
	/// Utils class to handle directory or path operations
	/// </summary>
	public class PathUtils
	{

		private readonly ISettings settings;
		//private readonly IAppLogger appLogger;

		public PathUtils(ISettings settings)//, IAppLogger appLogger)
		{
			this.settings = settings;
			//this.appLogger = appLogger;
		}

		public string GetPath(string token)
		{
			string currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
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
				appLogger.Error($"Error occurred while retrieving path: {ex.Message}");
				return null;
			}
		}


		public string GetPath_old(string basePath, string token)
		{
			if (!string.IsNullOrEmpty(basePath))
			{
				if (basePath.StartsWith(".\\") || basePath.StartsWith("./") || basePath.StartsWith(".\\") || basePath.StartsWith("./"))
				{
					basePath = GetPath(basePath);
				}
			}
			try
			{
				if (string.IsNullOrEmpty(token))
				{
					return basePath;
				}
				if (token.StartsWith("./") || token.StartsWith(".\\"))
				{
					return CombineRelativePath(basePath, token, ".\\", "./");
				}
				else if (token.StartsWith("../") || basePath.StartsWith("..\\"))
				{
					DirectoryInfo directoryInfo = new DirectoryInfo(basePath);
					string parentPath = directoryInfo.Parent.FullName;
					return CombineRelativePath(parentPath, token, "../", "..\\");
				}
				else
				{
					return token; // Absolute path
				}
			}
			catch (Exception ex)
			{
				appLogger.Error($"Error occurred while retrieving path: {ex.Message}");
				return null;
			}
		}

		private string CombineRelativePath(string basePath, string token, params string[] pathMarkers)
		{
			string root = basePath;
			foreach(string prefix in pathMarkers)
			{
				if (basePath.StartsWith(prefix))
				{
					root = basePath.Substring(prefix.Length);
					break;
				}
			}
			string apendix = token;
			foreach (string prefix in pathMarkers)
			{
				if (token.StartsWith(prefix))
				{
					apendix = token.Substring(prefix.Length);
					break;
				}
			}
			return Path.Combine(root, apendix);
		}

		public string GetInitStageFolder()
		{
			try
			{
				string rootFolder = settings.StageRootFolder;
				string namePrefix = settings.StageNamePrefix;
				bool appendNumberOnFirstStage = settings.AppendNumberOnFirstStage;
				string versionDelimiter = settings.StageVersionDelimiter;
				int stageVersions = settings.StageVersions;

				// Sanitize settings
				(versionDelimiter, namePrefix) = SanitizeSettings(versionDelimiter, namePrefix);

				string initialFolder;

				// If the number should be appended on the first stage and it's not zero-based
				if (appendNumberOnFirstStage && stageVersions > 0)
				{
					initialFolder = $"{namePrefix}{versionDelimiter}1";
				}
				else
				{
					initialFolder = namePrefix;
				}

				return Path.Combine(rootFolder, initialFolder);
			}
			catch (Exception ex)
			{
				appLogger.Error($"Error occurred while retrieving initial stage folder: {ex.Message}");
				return null;
			}
		}

		public List<string> GetStageFolders()
		{
			List<string> stageFolders = new List<string>();

			// Get the initial stage folder
			string initStageFolder = GetInitStageFolder();
			stageFolders.Add(initStageFolder);

			// Get the additional stage folders
			//Properties.Settings properties = Properties.Settings.Default;
			string rootFolder = settings.StageRootFolder;
			string namePrefix = settings.StageNamePrefix;
			string versionDelimiter = settings.StageVersionDelimiter;
			string lastNameSuffix = settings.StageLastNameSuffix;
			int stageVersions = settings.StageVersions;

			// Sanitize settings
			(versionDelimiter, namePrefix, lastNameSuffix) = SanitizeSettings(versionDelimiter, namePrefix, lastNameSuffix);


			// If zero or one stage versions are configured, return only the initial stage folder
			if (stageVersions <= 1)
			{
				return stageFolders;
			}

			// Add additional stage folders to the list
			for (int i = 2; i <= stageVersions - 1; i++)
			{
				string stageFolder = $"{namePrefix}{versionDelimiter}{i}";
				string absolutePath = Path.Combine(rootFolder, stageFolder);
				stageFolders.Add(absolutePath);
			}

			// Append the last folder with the last name suffix
			string lastStageFolder = $"{namePrefix}{versionDelimiter}{lastNameSuffix}";
			string lastStageFolderPath = Path.Combine(rootFolder, lastStageFolder);
			stageFolders.Add(lastStageFolderPath);

			return stageFolders;
		}

		public (string delimiter, string prefix) SanitizeSettings(string delimiter, string prefix)
		{
			string dummy = "LAST";
			(string delimiter, string prefix, string suffixForLast) value = SanitizeSettings(delimiter, prefix, dummy);
			return (delimiter, prefix);

		}

		public (string delimiter, string prefix, string suffixForLast) SanitizeSettings(string delimiter, string prefix, string suffixForLast)
		{
			// Check and sanitize delimiter
			if (string.IsNullOrEmpty(delimiter) || delimiter.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
			{
				appLogger.Warning($"Invalid delimiter '{delimiter}' detected. Replacing with '-'.");
				delimiter = "-";
			}

			// Check and sanitize prefix
			if (string.IsNullOrEmpty(prefix) || prefix.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
			{
				appLogger.Warning($"Invalid prefix '{prefix}' detected. Replacing with 'purge-temp'.");
				prefix = "purge-temp";
			}

			// Check and sanitize suffix for last
			if (string.IsNullOrEmpty(suffixForLast) || suffixForLast.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
			{
				appLogger.Warning($"Invalid suffix for last '{suffixForLast}' detected. Replacing with 'LAST'.");
				suffixForLast = "LAST";
			}

			return (delimiter, prefix, suffixForLast);
		}
	}
}

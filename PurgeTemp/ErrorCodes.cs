using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeTemp
{
	/// <summary>
	/// Provides error codes for the application.
	/// </summary>
	public static class ErrorCodes
	{
		// Success
		/// <summary>
		/// Execution was successful.
		/// </summary>
		public const int Success = 0;

		// Valid Values: Execution-related codes (1-99)
		/// <summary>
		/// No execution because the time since the last execution was too short.
		/// </summary>
		public const int ExecutionTooFrequent = 1;

		/// <summary>
		/// No execution because a skip token was found.
		/// </summary>
		public const int SkipTokenFound = 2;

		// Invalid Values: Argument-related codes (100-199)
		/// <summary>
		/// No execution because of general invalid arguments.
		/// </summary>
		public const int InvalidArguments = 100;
		/// <summary>
		/// The specified folder name is invalid because it is empty
		/// </summary>
		public const int EmptyFolderName = 101;
		/// <summary>
		/// The specified folder name is invalid because contains illegal characters
		/// </summary>
		public const int IllegalCharactersInFolderName = 102;
		/// <summary>
		/// The specified folder name is invalid because it uses a reserved file name
		/// </summary>
		public const int ReservedNameAsFolderName = 103;
		/// <summary>
		/// The specified folder name is ends with an invalid character (space or dot)
		/// </summary>
		public const int InvalidFolderNameSuffix = 104;
		/// <summary>
		/// The specified folder is a protected, system relevant folder like 'C:\Windows'
		/// </summary>
		public const int PathIsSystemDirectory = 105;
		/// <summary>
		/// The specified administrative folder is conflicting with a stage folder
		/// </summary>
		public const int AdministrativePathConflictsWithStagePath = 106;
		/// <summary>
		/// The specified stage folder is a protected, system relevant folder like 'C:\Windows'
		/// </summary>
		public const int StageFolderIsSystemDirectory = 107;
		/// <summary>
		/// The specified stage folder is a reserved, invalid folder like 'LPT1' or 'COM2'
		/// </summary>
		public const int StageFolderHasReservedFolderName = 108;
		/// <summary>
		/// The number of stages was below 1
		/// </summary>
		public const int InvalidNumberOfStages = 109;
		/// <summary>
		/// The number of files to log on purge was below 0
		/// </summary>
		public const int InvalidFileLogAmount = 110;

		// Invalid Values: General error codes (200-299)
		/// <summary>
		/// No execution due to an unknown error.
		/// </summary>
		public const int UnknownError = 200;

		/// <summary>
		/// The last folder could not be deleted.
		/// </summary>
		public const int CouldNotDeleteLastFolder = 201;

		/// <summary>
		/// A stage folder could not be renamed to the next stage name.
		/// </summary>
		public const int CouldNotRenameStageFolder = 202;

		/// <summary>
		/// A new stage folder could not be created.
		/// </summary>
		public const int CouldNotCreateNewStageFolder = 203;

		/// <summary>
		/// An administrative folder could not be created.
		/// </summary>
		public const int CouldNotCreateAdministrativeFolder = 204;

		/// <summary>
		/// The last purge token could not be written.
		/// </summary>
		public const int CouldNotCreateLastPurgeToken = 205;

	}
}

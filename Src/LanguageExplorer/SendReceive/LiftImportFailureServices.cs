// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Windows.Forms;
using SIL.Utils;

namespace LanguageExplorer.SendReceive
{
	/// <summary>
	/// This class handles issues related to a FLEx import failures.
	/// </summary>
	internal static class LiftImportFailureServices
	{
		/// <summary>
		/// File name for Lift import failure flag.
		/// </summary>
		public const string FailureFilename = "FLExImportFailure.notice";

		/// <summary>
		/// Get the import failure status.
		/// </summary>
		/// <returns></returns>
		internal static ImportFailureStatus GetFailureStatus(string baseLiftFolderDirectoryName)
		{
			var failurePathname = GetNoticePathname(baseLiftFolderDirectoryName);
			if (!File.Exists(failurePathname))
				return ImportFailureStatus.NoImportNeeded;

			var fileContents = File.ReadAllText(failurePathname);
			return fileContents.Contains(LanguageExplorerResources.kBasicFailureFileContents) ? ImportFailureStatus.BasicImportNeeded : ImportFailureStatus.StandardImportNeeded;
		}

		/// <summary>
		/// Set the Lift import status.
		/// </summary>
		internal static void RegisterStandardImportFailure(string baseLiftFolderDirectoryName)
		{
			// The results (of the FLEx import failure) will be that Lift Bridge will store the fact of the import failure,
			// and then protect the repo from damage by another S/R by Flex
			// by seeing the last import failure, and then requiring the user to re-try the failed import,
			// using the same LIFT file that had failed, before.
			// If that re-try attempt also fails, the user will need to continue re-trying the import,
			// until FLEx is fixed and can do the import.

			// Write out the failure notice.
			var failurePathname = GetNoticePathname(baseLiftFolderDirectoryName);
			File.WriteAllText(failurePathname, LanguageExplorerResources.kStandardFailureFileContents);
		}

		/// <summary>
		/// Show the Lift ikport failure status message, if needed.
		/// </summary>
		internal static void DisplayLiftFailureNoticeIfNecessary(Form parentWindow, string baseLiftFolderDirectory)
		{
			var noticeFilePath = GetNoticePathname(baseLiftFolderDirectory);
			if(File.Exists(noticeFilePath))
			{
				var contents = File.ReadAllText(noticeFilePath);
				if (contents.Contains(LanguageExplorerResources.kStandardFailureFileContents))
				{
					MessageBoxUtils.Show(parentWindow, LanguageExplorerResources.kFlexStandardImportFailureMessage,
										 LanguageExplorerResources.kFlexImportFailureTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				else
				{
					MessageBoxUtils.Show(parentWindow, LanguageExplorerResources.kBasicImportFailureMessage, LanguageExplorerResources.kFlexImportFailureTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
		}

		/// <summary>
		/// Registry a basic import failure.
		/// </summary>
		internal static void RegisterBasicImportFailure(string baseLiftFolderDirectoryName)
		{
			// The results (of the FLEx inital import failure) will be that Lift Bridge will store the fact of the import failure,
			// and then protect the repo from damage by another S/R by Flex
			// by seeing the last import failure, and then requiring the user to re-try the failed import,
			// using the same LIFT file that had failed, before.
			// If that re-try attempt also fails, the user will need to continue re-trying the import,
			// until FLEx is fixed and can do the import.

			// Write out the failure notice.
			var failurePathname = GetNoticePathname(baseLiftFolderDirectoryName);
			File.WriteAllText(failurePathname, LanguageExplorerResources.kBasicFailureFileContents);
		}

		/// <summary>
		/// Clear the previopus import failure status.
		/// </summary>
		internal static void ClearImportFailure(string baseLiftFolderDirectoryName)
		{
			var failurePathname = GetNoticePathname(baseLiftFolderDirectoryName);
			if (File.Exists(failurePathname))
				File.Delete(failurePathname);
		}

		private static string GetNoticePathname(string baseLiftFolderDirectoryName)
		{
			return Path.Combine(baseLiftFolderDirectoryName, FailureFilename);
		}
	}

	/// <summary>
	/// Enumeration of possible Lift import failure status.
	/// </summary>
	internal enum ImportFailureStatus
	{
		BasicImportNeeded,
		StandardImportNeeded,
		NoImportNeeded
	}
}
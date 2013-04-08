using System.IO;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// This class handles issues related to a FLEx import failures.
	/// </summary>
	internal static class LiftImportFailureServices
	{
		public const string FailureFilename = "FLExImportFailure.notice";

		internal static ImportFailureStatus GetFailureStatus(string baseLiftFolderDirectoryName)
		{
			var failurePathname = GetNoticePathname(baseLiftFolderDirectoryName);
			if (!File.Exists(failurePathname))
				return ImportFailureStatus.NoImportNeeded;

			var fileContents = File.ReadAllText(failurePathname);
			return fileContents.Contains(LexEdStrings.kBasicFailureFileContents) ? ImportFailureStatus.BasicImportNeeded : ImportFailureStatus.StandardImportNeeded;
		}

		internal static void RegisterStandardImportFailure(Form parentWindow, string baseLiftFolderDirectoryName)
		{
			// The results (of the FLEx import failure) will be that Lift Bridge will store the fact of the import failure,
			// and then protect the repo from damage by another S/R by Flex
			// by seeing the last import failure, and then requiring the user to re-try the failed import,
			// using the same LIFT file that had failed, before.
			// If that re-try attempt also fails, the user will need to continue re-trying the import,
			// until FLEx is fixed and can do the import.
			MessageBox.Show(parentWindow, LexEdStrings.kFlexStandardImportFailureMessage, LexEdStrings.kFlexImportFailureTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);

			// Write out the failure notice.
			var failurePathname = GetNoticePathname(baseLiftFolderDirectoryName);
			File.WriteAllText(failurePathname, LexEdStrings.kStandardFailureFileContents);
		}

		internal static void RegisterBasicImportFailure(Form parentWindow, string baseLiftFolderDirectoryName)
		{
			// The results (of the FLEx inital import failure) will be that Lift Bridge will store the fact of the import failure,
			// and then protect the repo from damage by another S/R by Flex
			// by seeing the last import failure, and then requiring the user to re-try the failed import,
			// using the same LIFT file that had failed, before.
			// If that re-try attempt also fails, the user will need to continue re-trying the import,
			// until FLEx is fixed and can do the import.
			MessageBox.Show(parentWindow, LexEdStrings.kBasicImportFailureMessage, LexEdStrings.kFlexImportFailureTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);

			// Write out the failure notice.
			var failurePathname = GetNoticePathname(baseLiftFolderDirectoryName);
			File.WriteAllText(failurePathname, LexEdStrings.kBasicFailureFileContents);
		}

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

	internal enum ImportFailureStatus
	{
		BasicImportNeeded,
		StandardImportNeeded,
		NoImportNeeded
	}
}
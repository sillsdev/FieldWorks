// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.Reporting;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>Used to move or copy media files when they are linked to a FieldWorks project</summary>
	public static class MoveOrCopyFilesController
	{
		#region Static methods
		/// <summary>
		/// Checks to see whether the given files are located in the given root directory (or any subfolder of it), and if not, prompts the user to
		/// allow FW to move, copy, or leave the files. If anything unexpected happens, the default is to leave the files where they are.
		/// </summary>
		/// <param name="files">The fully-specified path names of the files.</param>
		/// <param name="sRootDirLinkedFiles">The fully-specified path name of the LinkedFiles root directory.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <returns>The fully specified path names of the files to use, which might be the same as the given path or it could be
		/// in its new location under the LinkedFiles folder if the user elected to move or copy it.</returns>
		public static string[] MoveCopyOrLeaveMediaFiles(string[] files, string sRootDirLinkedFiles, IHelpTopicProvider helpTopicProvider)
		{
			return MoveCopyOrLeaveFiles(files,
				Path.Combine(sRootDirLinkedFiles, LcmFileHelper.ksMediaDir),
				sRootDirLinkedFiles,
				helpTopicProvider);
		}

		/// <summary>
		/// Checks to see whether the given file is located in the given root directory (or any subfolder of it), and if not, prompts the user to
		/// allow FW to move, copy, or leave the file. If anything unexpected happens, the default is to leave the file where it is.
		/// </summary>
		/// <param name="sFile">The fully-specified path name of the file.</param>
		/// <param name="sRootDirLinkedFiles">The fully-specified path name of the LinkedFiles root directory.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <returns>The fully specified path name of the file to use, which might be the same as the given path or it could be
		/// in its new location under the LinkedFiles folder if the user elected to move or copy it.</returns>
		public static string MoveCopyOrLeaveExternalFile(string sFile, string sRootDirLinkedFiles,
			IHelpTopicProvider helpTopicProvider)
		{
			return MoveCopyOrLeaveFiles(new[] {sFile},
				Path.Combine(sRootDirLinkedFiles, LcmFileHelper.ksOtherLinkedFilesDir),
				sRootDirLinkedFiles,
				helpTopicProvider)[0];
		}

		private static string[] MoveCopyOrLeaveFiles(string[] files, string subFolder, string sRootDirExternalLinks,
			IHelpTopicProvider helpTopicProvider)
		{
			try
			{
				if (!Directory.Exists(subFolder))
					Directory.CreateDirectory(subFolder);
			}
			catch (Exception e)
			{
				Logger.WriteEvent(string.Format("Error creating the directory: '{0}'", subFolder));
				Logger.WriteError(e);
				return files;
			}

			// Check whether the file is found within the directory.
			if (files.All(f => IsFileInFolder(f, sRootDirExternalLinks)))
				return files;

			using (var dlg = new MoveOrCopyFilesDlg())
			{
				dlg.Initialize2(subFolder, helpTopicProvider);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					FileLocationChoice choice = dlg.Choice;
					return files.Select(f => PerformMoveCopyOrLeaveFile(f, subFolder, choice)).ToArray();
				}

				return files;
			}
		}

		/// <summary>
		/// Performs the action the user requested: move, copy, or leave the file.
		/// </summary>
		/// <param name="sFile">The fully-specified path name of the file.</param>
		/// <param name="sNewDir">The fully-specified path name of the new target directory.</param>
		/// <param name="action">the action the user chose (copy, move or leave)</param>
		/// <param name="batchMode"><c>false</c> to notify the user of every little problem and return <c>null</c> if anything unexpected happens.
		/// <c>true</c> (default) to interact only in case of conflicts and return the original path if the file cannot be moved or copied.</param>
		/// <param name="sNewName">default is <c>null</c> to keep the same filename in the new location</param>
		/// <returns>The fully-specified path name of the (possibly newly moved or copied) file
		/// (null if batchMode=false and the file could not be moved or copied as specified)</returns>
		internal static string PerformMoveCopyOrLeaveFile(string sFile, string sNewDir, FileLocationChoice action,
			bool batchMode = true, string sNewName = null)
		{
			if (action == FileLocationChoice.Leave)
				return sFile; // use original location.

			var sNewFile = Path.Combine(sNewDir, sNewName ?? Path.GetFileName(sFile));
			if (FileUtils.PathsAreEqual(sFile, sNewFile))
				return sFile;

			if (File.Exists(sNewFile))
			{
				var promptAlreadyExists = string.Format(FwCoreDlgs.ksAlreadyExists, sNewFile);
				if (batchMode)
				{
					promptAlreadyExists = string.Format(FwCoreDlgs.ksClickNoToLeave, promptAlreadyExists, sFile);
				}
				if (MessageBox.Show(promptAlreadyExists, FwCoreDlgs.kstidWarning, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
				{
					return batchMode ? sFile : null;
				}
				try
				{
					File.Delete(sNewFile);
				}
				catch
				{
					// This is probably a picture file that we can't delete because it's open somewhere.
					MessageBox.Show(FwCoreDlgs.ksErrorFileInUse, FwCoreDlgs.ksError);
					return batchMode ? sFile : null;
				}
			}
			try
			{
				switch (action)
				{
					case FileLocationChoice.Move:
						File.Move(sFile, sNewFile);
						break;
					case FileLocationChoice.Copy:
						File.Copy(sFile, sNewFile);
						break;
				}
				return sNewFile;
			}
			catch (Exception e)
			{
				var sAction = (action == FileLocationChoice.Copy ? "copy" : "mov");
				Logger.WriteEvent($"Error {sAction}ing file '{sFile}' to '{sNewFile}'");
				Logger.WriteError(e);
				if (batchMode)
				{
					return sFile;
				}

				MessageBox.Show(string.Format(FwCoreDlgs.ksErrorMovingOrCopyingXtoY, sFile, sNewFile, e), FwCoreDlgs.ksError);
				return null;
			}
		}

		/// <summary>
		/// Determines whether the given file is located in the given root directory (or any subfolder of it).
		/// REVIEW (Hasso) 2023.06: this could be refactored into FileUtils, although it may be too simple to be worth the effort.
		/// </summary>
		/// <param name="sFile">The fully-specified path name of the file.</param>
		/// <param name="sRootDir">The fully-specified path name of the LinkedFiles root directory.</param>
		/// <returns><c>true</c> if the given file is located in the given root directory.</returns>
		internal static bool IsFileInFolder(string sFile, string sRootDir)
		{
			if(sFile.ToLowerInvariant().StartsWith(sRootDir.ToLowerInvariant()))
			{
				var cchDir = sRootDir.Length;
				if(cchDir > 0 && sRootDir[cchDir - 1] == Path.DirectorySeparatorChar)
					return true; // the root directory path ends with '/' or '\'
				if(sFile.Length > cchDir && sFile[cchDir] == Path.DirectorySeparatorChar)
					return true; // the first char in the file's path after the root directory path is '/' or '\'
			}
			return false;
		}
		#endregion

	}
}

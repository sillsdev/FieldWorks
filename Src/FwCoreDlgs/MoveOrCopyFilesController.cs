// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>Used to move or copy media files when they are linked to a FieldWorks project</summary>
	public class MoveOrCopyFilesController
	{
		#region Static methods
		/// <summary>
		/// Checks to see whether the given files are located in the given root directory (or any subfolder of it), and if not, prompts the user to
		/// allow FW to move, copy, or leave the files. If anything unexpected happens, the default is to leave the files where they are.
		/// </summary>
		/// <param name="files">The fully-specified path names of the files.</param>
		/// <param name="sRootDirLinkedFiles">The fully-specified path name of the LinkedFiles root directory.</param>
		/// <param name="isLocal">True if running on the local server: allows file not to be moved or copied</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <returns>The fully specified path names of the files to use, which might be the same as the given path or it could be
		/// in its new location under the LinkedFiles folder if the user elected to move or copy it.</returns>
		public static string[] MoveCopyOrLeaveMediaFiles(string[] files, string sRootDirLinkedFiles, IHelpTopicProvider helpTopicProvider, bool isLocal)
		{
			return MoveCopyOrLeaveFiles(files,
				Path.Combine(sRootDirLinkedFiles, FdoFileHelper.ksMediaDir),
				sRootDirLinkedFiles,
				helpTopicProvider, isLocal);
		}

		/// <summary>
		/// Checks to see whether the given file is located in the given root directory (or any subfolder of it), and if not, prompts the user to
		/// allow FW to move, copy, or leave the file. If anything unexpected happens, the default is to leave the file where it is.
		/// </summary>
		/// <param name="sFile">The fully-specified path name of the file.</param>
		/// <param name="sRootDirLinkedFiles">The fully-specified path name of the LinkedFiles root directory.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="isLocal">True if running on the local server: allows file not to be moved or copied</param>
		/// <returns>The fully specified path name of the file to use, which might be the same as the given path or it could be
		/// in its new location under the LinkedFiles folder if the user elected to move or copy it.</returns>
		public static string MoveCopyOrLeaveExternalFile(string sFile, string sRootDirLinkedFiles,
			IHelpTopicProvider helpTopicProvider, bool isLocal)
		{
			return MoveCopyOrLeaveFiles(new[] {sFile},
				Path.Combine(sRootDirLinkedFiles, FdoFileHelper.ksOtherLinkedFilesDir),
				sRootDirLinkedFiles,
				helpTopicProvider, isLocal).FirstOrDefault();
		}

		private static string[] MoveCopyOrLeaveFiles(string[] files, string subFolder, string sRootDirExternalLinks,
			IHelpTopicProvider helpTopicProvider, bool isLocal)
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
			if (files.All(f => FileIsInExternalLinksFolder(f, sRootDirExternalLinks)))
				return files;

			using (var dlg = new MoveOrCopyFilesDlg())
			{
				dlg.Initialize2(subFolder, helpTopicProvider, isLocal);
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
		/// <param name="sRootDir">The fully-specified path name of the new target directory.</param>
		/// <param name="action">the action the user chose (copy, move or leave)</param>
		/// <returns>The fully-specified path name of the (possibly newly moved or copied) file</returns>
		internal static string PerformMoveCopyOrLeaveFile(string sFile, string sRootDir, FileLocationChoice action)
		{
			if (action == FileLocationChoice.Leave)
				return sFile; // use original location.

			string sNewFile = Path.Combine(sRootDir, Path.GetFileName(sFile));
			if (FileUtils.PathsAreEqual(sFile, sNewFile))
				return sFile;

			if (File.Exists(sNewFile))
			{
				if (MessageBox.Show(string.Format(FwCoreDlgs.ksAlreadyExists, sNewFile),
						FwCoreDlgs.kstidWarning, MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
					== DialogResult.No)
				{
					return sFile;
				}
				try
				{
					File.Delete(sNewFile);
				}
				catch
				{
					// This is probably a picture file that we can't delete because it's open in FieldWorks.
					MessageBox.Show(FwCoreDlgs.ksDeletePictureBeforeReplacingFile, FwCoreDlgs.ksCannotReplaceDisplayedPicture);
					return sNewFile;
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
				Logger.WriteEvent(string.Format("Error {0}ing file '{1}' to '{2}'", sAction, sFile, sNewFile));
				Logger.WriteError(e);
				return sFile;
			}
		}

		/// <summary>
		/// Determines whether the given file is located in the given root directory (or any subfolder of it).
		/// </summary>
		/// <param name="sFile">The fully-specified path name of the file.</param>
		/// <param name="sRootDir">The fully-specified path name of the LinkedFiles root directory.</param>
		/// <returns><c>true</c> if the given file is located in the given root directory.</returns>
		internal static bool FileIsInExternalLinksFolder(string sFile, string sRootDir)
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

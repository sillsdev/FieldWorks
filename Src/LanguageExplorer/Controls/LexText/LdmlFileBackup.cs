// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;

namespace LanguageExplorer.Controls.LexText
{
	internal sealed class LdmlFileBackup
	{
		/// <summary>
		/// Copy a complete directory, including all contents recursively.
		/// Everything in out put will be writeable, even if some input files are read-only.
		/// </summary>
		public static void CopyDirectory(string sourcePath, string targetPath)
		{
			CopyDirectory(new DirectoryInfo(sourcePath), new DirectoryInfo(targetPath));
		}

		private static void CopyDirectory(DirectoryInfo source, DirectoryInfo target)
		{
			// Check if the target directory exists, if not, create it.
			if (Directory.Exists(target.FullName) == false)
			{
				Directory.CreateDirectory(target.FullName);
			}

			// Copy each file into its new directory.
			foreach (var fi in source.GetFiles())
			{
				var destFileName = Path.Combine(target.ToString(), fi.Name);
				fi.CopyTo(destFileName, true);
				File.SetAttributes(destFileName, FileAttributes.Normal); // don't want to copy readonly property.
			}

			// Copy each subdirectory using recursion.
			foreach (var diSourceSubDir in source.GetDirectories())
			{
				var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
				CopyDirectory(diSourceSubDir, nextTargetSubDir);
			}
		}

		/// <summary>
		/// Delete all files in a directory and all subfolders
		/// </summary>
		public static void DeleteDirectory(string sourcePath)
		{
			DeleteDirectory(new DirectoryInfo(sourcePath));
		}

		/// <summary>
		/// Delete all files in a directory and all subfolders
		/// </summary>
		private static void DeleteDirectory(DirectoryInfo source)
		{
			foreach (var diSourceSubDir in source.GetDirectories())
			{
				DeleteDirectory(diSourceSubDir);
			}
			foreach (var fi in source.GetFiles())
			{
				fi.Delete();
			}
		}
	}
}
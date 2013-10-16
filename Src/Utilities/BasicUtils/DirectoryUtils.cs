// --------------------------------------------------------------------------------------------
#region
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DirectoryUtils.cs
// Responsibility:
// --------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Utils
{
	/// <summary></summary>
	public static class DirectoryUtils
	{
		internal static IOrderedEnumerable<string> OrderByAscendingCaseInsensitive(this IEnumerable<string> source)
		{
			return source.OrderBy(path => path.ToLowerInvariant());
		}

		/// <summary>
		/// Returns the names of files (including their paths) in the specified directory, sorted by
		/// case insensitive ascending.
		///
		/// Order of Directory.GetFiles is not guaranteed, so if left unsorted will differ on different platforms.
		/// observered .net3.5 defaulting to case insensitive ascending.
		/// </summary>
		public static string[] GetOrderedFiles(string path)
		{
			return Directory.GetFiles(path).OrderByAscendingCaseInsensitive().ToArray();
		}

		/// <summary>
		/// Returns the names of files (including their paths) that match the specified search pattern in the
		/// specified directory, sorted by case insensitive ascending.
		/// <see cref="M:SIL.Utils.DirectoryUtils.GetOrderedFiles"></see>
		/// </summary>
		public static string[] GetOrderedFiles(string path, string searchPattern)
		{
			return Directory.GetFiles(path, searchPattern).OrderByAscendingCaseInsensitive().ToArray();
		}

		/// <summary>
		/// Returns the names of files (including their paths) that match the specified search pattern in the specified
		/// directory, using a value to determine whether to search subdirectories, sorted by case insensitive ascending.
		/// <see cref="M:SIL.Utils.DirectoryUtils.GetOrderedFiles"></see>
		/// </summary>
		public static string[] GetOrderedFiles(string path, string searchPattern, SearchOption searchOption)
		{
			return Directory.GetFiles(path, searchPattern, searchOption).OrderByAscendingCaseInsensitive().ToArray();
		}

		/// <summary>
		/// Copies the source directory and its files to the destination. Copies subdirectories *only if* <c>copySubDirs</c> is true
		/// </summary>
		/// <param name="sourceDirName">Source dir name.</param>
		/// <param name="destDirName">Destination dir name.</param>
		/// <param name="copySubDirs">If set to <c>true</c> copy sub dirs.</param>
		/// <param name="overwrite">If set to <c>true</c> overwrite existing files.</param>
		public static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs, bool overwrite)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}

			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}

			// Get the files in the directory and copy them to the new location.
			foreach (FileInfo file in dir.GetFiles())
			{
				file.CopyTo(Path.Combine(destDirName, file.Name), overwrite);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dir.GetDirectories())
				{
					CopyDirectory(subdir.FullName, Path.Combine(destDirName, subdir.Name), copySubDirs, overwrite);
				}
			}
		}
	}
}

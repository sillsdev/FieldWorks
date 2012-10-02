// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FileManager.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class FileManager
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Caches the output files that are the output of the given input files with parameter.
		/// </summary>
		/// <param name="handle">The handle, retrieved by a call to <c>GetHash</c></param>
		/// <param name="outFileNames">The output file names.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public CacheFileInfo CacheFile(string handle, params string[] outFileNames)
		{
			return CacheFile(handle, outFileNames, outFileNames);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Caches the output files.
		/// </summary>
		/// <param name="handle">The handle.</param>
		/// <param name="origFileNames">The original output file names.</param>
		/// <param name="outFileNames">The (possibly temporary) output file names.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public CacheFileInfo CacheFile(string handle, string[] origFileNames, string[] outFileNames)
		{
			System.Diagnostics.Debug.Assert(origFileNames.Length == outFileNames.Length);
			int index = 0;
			CachedFile[] cachedFiles = new CachedFile[outFileNames.Length];
			for (int i = 0; i < outFileNames.Length; i++)
			{
				cachedFiles[i] = CopyFile(handle, origFileNames[i], outFileNames[i], ref index);
			}

			return new CacheFileInfo(cachedFiles);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the directories and creates them if they don't exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EnsureDirectories()
		{
			string fileCachePath = Path.Combine(Properties.Settings.Default.FileCachePath,
				"Cache");
			if (!Directory.Exists(fileCachePath))
				Directory.CreateDirectory(fileCachePath);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Clears all files in the file cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		public void Clear()
		{
			string[] allFiles = Directory.GetFiles(Properties.Settings.Default.FileCachePath);
			foreach (string file in allFiles)
				File.Delete(file);
			Directory.Delete(Properties.Settings.Default.FileCachePath, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the files.
		/// </summary>
		/// <param name="cachedFiles">The cached files.</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveFiles(CacheFileInfo cachedFiles)
		{
			foreach (CachedFile file in cachedFiles.Files)
			{
				try
				{
					File.Delete(file.CachedFileName);
				}
				catch (Exception e)
				{
					Console.WriteLine("Got exception: " + e.Message);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the path from file.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns>The path</returns>
		/// ------------------------------------------------------------------------------------
		internal static string GetPathFromFileName(string fileName)
		{
			return Path.Combine(Path.Combine(Properties.Settings.Default.FileCachePath,
				Path.Combine("Cache", Path.Combine(fileName[0].ToString(),
				fileName[1].ToString()))), fileName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of files.
		/// </summary>
		/// <value>The number of files.</value>
		/// ------------------------------------------------------------------------------------
		internal int NumberOfFiles
		{
			get
			{
				return Directory.GetFiles(
					Path.Combine(Properties.Settings.Default.FileCachePath, "Cache"), "*.*",
					SearchOption.AllDirectories).Length;
			}
		}

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Computes the name and path of the file in the cache. It also creates any
		/// subdirectories if necessary.
		/// </summary>
		/// <param name="handle">The handle.</param>
		/// <param name="index">The index.</param>
		/// <returns>Path and name of file</returns>
		/// ------------------------------------------------------------------------------------
		private string GetFileNameFromHandle(string handle, ref int index)
		{
			char[] invalidChars = Path.GetInvalidFileNameChars();
			foreach (char c in invalidChars)
				handle = handle.Replace(c, '_');

			string dir1 = handle[0].ToString();
			string dir2 = handle[1].ToString();
			foreach (char c in Path.GetInvalidPathChars())
			{
				if (dir1[0] == c)
					dir1 = "AA";
				else if (dir2[0] == c)
					dir2 = "AA";
			}
			if (dir1[0] == Path.DirectorySeparatorChar || dir1[0] == Path.AltDirectorySeparatorChar)
				dir1 = "AB";
			if (dir2[0] == Path.DirectorySeparatorChar || dir2[0] == Path.AltDirectorySeparatorChar)
				dir2 = "AB";

			string path = Path.Combine(Properties.Settings.Default.FileCachePath,
				Path.Combine("Cache", Path.Combine(dir1, dir2)));
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			path = Path.Combine(path, handle + "." + index.ToString());
			index++;
			while (File.Exists(path))
			{
				path = Path.ChangeExtension(path, "." + index.ToString());
				index++;
			}

			return path;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the file into the file cache.
		/// </summary>
		/// <param name="handle">The handle.</param>
		/// <param name="origFileName">Name of the original file.</param>
		/// <param name="fileName">Name of the file (might be temporary name).</param>
		/// <param name="index">The index used to produce a unique file name.</param>
		/// <returns>Information about the file in the cache.</returns>
		/// ------------------------------------------------------------------------------------
		private CachedFile CopyFile(string handle, string origFileName, string fileName,
			ref int index)
		{
			string resultFile = GetFileNameFromHandle(handle, ref index);

			File.Copy(fileName, resultFile);

			return new CachedFile(origFileName, resultFile);
		}
		#endregion
	}
}

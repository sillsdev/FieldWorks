// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright file="MyFolders.cs" from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// <author>Greg Trihus</author>
// <email>greg_trihus@sil.org</email>
// Last reviewed:
//
// <remarks>
// Works with folder trees in file system
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SIL.PublishingSolution
{
	/// <summary>
	/// Works with folder trees in file system
	/// </summary>
	public static class MyFolders
	{
		#region bool Copy(string src, string dst, string dirFilter)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy source (src) to destination (dst) using filter (dirFilter)
		/// </summary>
		/// <param name="src">where to get it</param>
		/// <param name="dst">where to put it</param>
		/// <param name="dirFilter">name format</param>
		/// <param name="applicationName">Name of the application.</param>
		/// <returns>true if successfully creating a new copy</returns>
		/// ------------------------------------------------------------------------------------
		public static bool Copy(string src, string dst, string dirFilter, string applicationName)
		{
			Debug.Assert(Directory.Exists(src));
			Debug.Assert(!string.IsNullOrEmpty(dst));
			if (!CreateDirectory(dst, applicationName))
				return false;
			DirectoryInfo di = new DirectoryInfo(src);
			foreach (FileInfo fileInfo in di.GetFiles())
			{
				string dstFullName = Path.Combine(dst, fileInfo.Name);
				File.Copy(fileInfo.FullName, dstFullName, true);
				File.SetAttributes(dstFullName, File.GetAttributes(fileInfo.FullName));
			}

			foreach (DirectoryInfo directoryInfo in di.GetDirectories())
			{
				if (directoryInfo.Name.Substring(0, 1) == ".")
					continue;

				if(directoryInfo.Name == dirFilter)
					continue;

				string dstFullName = Path.Combine(dst, directoryInfo.Name);
				Copy(directoryInfo.FullName, dstFullName, dirFilter, applicationName);
				Directory.SetCreationTime(dstFullName, Directory.GetCreationTime(directoryInfo.FullName));
			}
			return true;
		}
		#endregion

		#region string GetNewName(string filePath, string folderName, string UserFileName)
		/// <summary>
		/// Return the New Folder Name after checking it whether its existing.
		/// </summary>
		/// <param name="directory">Path</param>
		/// <param name="name">name of the subfolder</param>
		/// <returns>resulting folder name</returns>
		public static string GetNewName(string directory, string name)
		{
			Debug.Assert(Directory.Exists(directory));
			Debug.Assert(!string.IsNullOrEmpty(name));
			string filePath = Path.Combine(directory, name);
			Match m = Regex.Match(name, "[0-9]*$");
			if (m.Success)
				name = name.Substring(0, name.Length - m.Value.Length);
			int counter = m.Success ? int.Parse(m.Value) : 0;
			while (Directory.Exists(filePath))
			{
				filePath = Path.Combine(directory, name + ++counter);
			}
			return filePath.Substring(filePath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
		}
		#endregion

		#region bool CreateDirectory(string outPath)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creating the Directory with Checking the Access rights
		/// </summary>
		/// <param name="outPath">Directory to be created</param>
		/// <param name="applicationName">Name of the application.</param>
		/// <returns>True/False based on success</returns>
		/// ------------------------------------------------------------------------------------
		public static bool CreateDirectory(string outPath, string applicationName)
		{
			bool returnValue = true;
			try
			{
				if (!Directory.Exists(outPath))
				{
					Directory.CreateDirectory(outPath);
				}
			}
			catch (UnauthorizedAccessException)
			{
				MessageBox.Show("Sorry! You might not have permission to use this resource.",
					applicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				returnValue = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				returnValue = false;
			}
			return returnValue;
		}
		#endregion
	}
}

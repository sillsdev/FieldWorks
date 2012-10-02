// Tools2.cs
// User: Jean-Marc Giffin at 11:06 A 16/07/2008

using System;
using System.IO;

namespace SIL.FieldWorks.Common.Utils
{
	public class Utils2
	{
		/// <summary>
		/// Get the directory containing a given file.
		/// Ex. "/home/me/hello.txt" -> "/home/me"
		/// </summary>
		/// <param name="filename">
		/// A <see cref="System.String"/> containing the full file path.
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/> containing the directory with the file within.
		/// </returns>
		public static string GetFilesPath(string filename)
		{
			int last = filename.LastIndexOf(Path.DirectorySeparatorChar);
			return filename.Substring(0, last);
		}

		/// <summary>
		/// Get just the filename out of a filepath.
		/// Ex. "/home/me/hello.txt" -> "hello.txt"
		/// </summary>
		/// <param name="filename">
		/// A <see cref="System.String"/> containing the full file path.
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/> containing just the file's name.
		/// </returns>
		public static string GetFileWithoutPath(string filename)
		{
			int last = filename.LastIndexOf(Path.DirectorySeparatorChar);
			return filename.Substring(last + 1, filename.Length - last - 1);
		}
	}
}

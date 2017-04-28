// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Text;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface to allow us to mock out the File class to avoid dependency on the OS.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFileOS
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified file exists.
		/// </summary>
		/// <param name="sPath">The file path.</param>
		/// ------------------------------------------------------------------------------------
		bool FileExists(string sPath);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified directory exists.
		/// </summary>
		/// <param name="sPath">The directory path.</param>
		/// ------------------------------------------------------------------------------------
		bool DirectoryExists(string sPath);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the files in the given directory.
		/// </summary>
		/// <param name="sPath">The directory path.</param>
		/// <returns>list of files</returns>
		/// ------------------------------------------------------------------------------------
		string[] GetFilesInDirectory(string sPath);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the files in the given directory.
		/// </summary>
		/// <param name="sPath">The directory path.</param>
		/// <param name="searchPattern">The search string to match against the names of files in
		/// path. The parameter cannot end in two periods ("..") or contain two periods ("..")
		/// followed by DirectorySeparatorChar or AltDirectorySeparatorChar, nor can it contain
		/// any of the characters in InvalidPathChars.</param>
		/// <returns>list of files</returns>
		/// ------------------------------------------------------------------------------------
		string[] GetFilesInDirectory(string sPath, string searchPattern);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writer.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		Stream OpenStreamForWrite(string filename);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a stream to write the given file in the given mode.
		/// </summary>
		/// <param name="filename">The fully-qualified file name</param>
		/// <param name="mode">The <see>FileMode</see> to use</param>
		/// <returns>A stream with write access</returns>
		/// ------------------------------------------------------------------------------------
		Stream OpenStreamForWrite(string filename, FileMode mode);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a stream to read the given file
		/// </summary>
		/// <param name="filename">The fully-qualified file name</param>
		/// <returns>A stream with read access</returns>
		/// ------------------------------------------------------------------------------------
		Stream OpenStreamForRead(string filename);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writer.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <param name="encoding">The encoding.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		TextWriter GetWriter(string filename, Encoding encoding);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a TextReader for the given file
		/// </summary>
		/// <param name="filename">The fully-qualified file name</param>
		/// <param name="encoding">The encoding to use for interpreting the contents</param>
		/// ------------------------------------------------------------------------------------
		TextReader GetReader(string filename, Encoding encoding);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given file is reable and writeable
		/// </summary>
		/// <param name="filename">The fully-qualified file name</param>
		/// <returns><c>true</c> if the file is reable and writeable; <c>false</c> if the file
		/// does not exist, is locked, is read-only, or has permissions set such that the user
		/// cannot read or write it.</returns>
		/// ------------------------------------------------------------------------------------
		bool IsFileReadableAndWritable(string filename);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the given file
		/// </summary>
		/// <param name="filename">The fully-qualified file name</param>
		/// ------------------------------------------------------------------------------------
		void Delete(string filename);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the specified source.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="destination">The destination.</param>
		/// ------------------------------------------------------------------------------------
		void Move(string source, string destination);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the specified source.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="destination">The destination.</param>
		/// ------------------------------------------------------------------------------------
		void Copy(string source, string destination);

		/// <summary/>
		void CreateDirectory(string directory);

		/// <summary>
		/// Returns the length of the file
		/// </summary>
		/// <param name="filePath"></param>
		long FileLength(string filePath);
	}
}
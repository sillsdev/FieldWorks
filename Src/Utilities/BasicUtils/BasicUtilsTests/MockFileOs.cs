// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2009' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MockFileOS.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Mock version of IFileOS that lets us simulate existing files and directories.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class MockFileOS : IFileOS
	{
		#region FileLockType enum
		internal enum FileLockType
		{
			/// <summary>Not locked. Use this if you just need to see if file exists.</summary>
			None,
			/// <summary>Read lock (can have other read-locks open)</summary>
			Read,
			/// <summary>Write or delete lock (exclusive)</summary>
			Write,
		}
		#endregion

		#region MockFile class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Represents a file in the file system without actually being a file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class MockFile
		{
			public string Contents { get; set; }
			public Encoding Encoding { get; set; }
			private FileLockType m_hardLock = FileLockType.None;
			private List<IDisposable> m_streams;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="MockFile"/> class.
			/// </summary>
			/// <param name="contents">The contents of the file.</param>
			/// <param name="encoding">The file encoding.</param>
			/// --------------------------------------------------------------------------------
			public MockFile(string contents, Encoding encoding)
			{
				Contents = contents;
				Encoding = encoding;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Adds the specified stream to the streams that have been opened for this file.
			/// </summary>
			/// <param name="stream">The stream.</param>
			/// --------------------------------------------------------------------------------
			public void AddStream(IDisposable stream)
			{
				if (stream is Stream || stream is StringReader || stream is StringWriter)
				{
					if (m_streams == null)
						m_streams = new List<IDisposable>();
					m_streams.Add(stream);
				}
				else
					throw new ArgumentException("AddStream can only be called with a Stream, StringReader, or StringWriter.");
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the contents as bytes.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public byte[] ContentsAsBytes
			{
				get
				{
					return Encoding.Convert(Encoding.Unicode, Encoding, Encoding.Unicode.GetBytes(Contents));
				}
				set
				{
					Contents = Encoding.Unicode.GetString(Encoding.Convert(Encoding, Encoding.Unicode, value));
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the lock on this file.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public FileLockType Lock
			{
				get
				{
					if (m_hardLock == FileLockType.Write || m_streams == null)
						return m_hardLock;
					FileLockType worstLock = m_hardLock;
					for (int i = 0; i < m_streams.Count; i++)
					{
						if (m_streams[i] is Stream)
						{
							Stream s = (Stream)m_streams[i];
							if (s.CanWrite)
								return FileLockType.Write;
							if (s.CanRead)
								worstLock = FileLockType.Read;
							else
							{
								m_streams.RemoveAt(i--);
								// A Stream needs to save its binary contents in a string.
								// A C# string is encoded in Unicode, so we need to convert the binary
								// data to Unicode before storing it.
								if (s is MemoryStream &&
									(MiscUtils.IsUnix || (bool)ReflectionHelper.GetField(s, "_exposable")))
									ContentsAsBytes = ((MemoryStream)s).ToArray();
							}
						}
						else if (m_streams[i] is StringReader)
						{
							StringReader sr = (StringReader)m_streams[i];
							try
							{
								sr.Peek();
								worstLock = FileLockType.Read;
							}
							catch (ObjectDisposedException)
							{
								m_streams.RemoveAt(i--);
							}
						}
						else if (m_streams[i] is StringWriter)
						{
							StringWriter sw = (StringWriter)m_streams[i];
							try
							{
								sw.Write((string)null); // Write nothing
								worstLock = FileLockType.Write;
							}
							catch (ObjectDisposedException)
							{
								Contents = sw.GetStringBuilder().ToString();
								m_streams.RemoveAt(i--);
							}
						}
					}
					return worstLock;
				}
				set { m_hardLock = value; }
			}
		}
		#endregion

		/// <summary>Add fake file names to this list to simulate existing files. The value
		/// is the file contents.</summary>
		private readonly Dictionary<string, MockFile> m_existingFiles = new Dictionary<string, MockFile>();
		/// <summary>Add fake folder names to this list to simulate existing folders</summary>
		private readonly List<string> m_existingDirectories = new List<string>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add fake folder names to this list to simulate existing folders
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<string> ExistingDirectories
		{
			get { return m_existingDirectories; }
		}

		#region Public Methods to facilitate testing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the given filename to the collection of files that should be considered to
		/// exist.
		/// </summary>
		/// <param name="filename">The filename (may or may not include path).</param>
		/// ------------------------------------------------------------------------------------
		public void AddExistingFile(string filename)
		{
			AddFile(filename, null, Encoding.UTF8);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new "temp" file (with fully-qualified path and name) to the collection of
		/// files and sets its contents so it can be read. Encoding will be UTF8.
		/// </summary>
		/// <param name="contents">The contents of the file</param>
		/// <returns>The name of the file</returns>
		/// ------------------------------------------------------------------------------------
		public string MakeFile(string contents)
		{
			return MakeFile(contents, Encoding.UTF8);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new "temp" file (with fully-qualified path and name) to the collection of
		/// files and sets its contents so it can be read.
		/// </summary>
		/// <param name="contents">The contents of the file</param>
		/// <returns>The name of the file</returns>
		/// <param name="encoding">File encoding</param>
		/// ------------------------------------------------------------------------------------
		public string MakeFile(string contents, Encoding encoding)
		{
			string filename = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			AddFile(filename, contents, encoding);
			return filename;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a mocked Standard Format file with \id line and file contents as specified.
		/// No BOM. Encoding will be UTF8.
		/// </summary>
		/// <param name="sBookId">The book id (if set, this will cause \id line to be written
		/// as the first line of the fiel)</param>
		/// <param name="lines">Remaining lines of the file</param>
		/// ------------------------------------------------------------------------------------
		public string MakeSfFile(string sBookId, params string[] lines)
		{
			return MakeSfFile(false, sBookId, lines);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a mocked Standard Format file with \id line and file contents as specified.
		/// Encoding will be UTF8.
		/// </summary>
		/// <param name="fIncludeBOM">Indicates whether file contents should include the byte
		/// order mark</param>
		/// <param name="sBookId">The book id (if set, this will cause \id line to be written
		/// as the first line of the fiel)</param>
		/// <param name="lines">Remaining lines of the file</param>
		/// ------------------------------------------------------------------------------------
		public string MakeSfFile(bool fIncludeBOM, string sBookId, params string[] lines)
		{
			return MakeSfFile(Encoding.UTF8, fIncludeBOM, sBookId, lines);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a mocked Standard Format file with \id line and file contents as specified.
		/// </summary>
		/// <param name="encoding">File encoding</param>
		/// <param name="fIncludeBOM">Indicates whether file contents should include the byte
		/// order mark</param>
		/// <param name="sBookId">The book id (if set, this will cause \id line to be written
		/// as the first line of the fiel)</param>
		/// <param name="lines">Remaining lines of the file</param>
		/// ------------------------------------------------------------------------------------
		public string MakeSfFile(Encoding encoding, bool fIncludeBOM, string sBookId,
			params string[] lines)
		{
			return MakeFile(CreateFileContents(fIncludeBOM, sBookId, lines), encoding);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the given filename to the collection of files that should be considered to
		/// exist and set its contents so it can be read.
		/// </summary>
		/// <param name="filename">The filename (may or may not include path).</param>
		/// <param name="contents">The contents of the file</param>
		/// <param name="encoding">File encoding</param>
		/// ------------------------------------------------------------------------------------
		public void AddFile(string filename, string contents, Encoding encoding)
		{
			FileUtils.AssertValidFilePath(filename);
			string dir = Path.GetDirectoryName(filename);
			if (!string.IsNullOrEmpty(dir) && !((IFileOS)this).DirectoryExists(dir))
				ExistingDirectories.Add(dir); // Theoretically, this should add containing folders recursively.
			m_existingFiles[filename] = new MockFile(contents, encoding);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates getting an exclusive (write) file lock on the file having the given filename.
		/// </summary>
		/// <param name="filename">The filename (must have been added previously).</param>
		/// ------------------------------------------------------------------------------------
		public void LockFile(string filename)
		{
			FileUtils.AssertValidFilePath(filename);
			m_existingFiles[filename].Lock = FileLockType.Write;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the simulated file contents, suitable for calling MakeFile or AddFile.
		/// </summary>
		/// <param name="fIncludeBOM">Indicates whether file contents should include the byte
		/// order mark</param>
		/// <param name="sBookId">The book id (if set, this will cause \id line to be written
		/// as the first line of the fiel)</param>
		/// <param name="lines">Remaining lines of the file</param>
		/// ------------------------------------------------------------------------------------
		public static string CreateFileContents(bool fIncludeBOM, string sBookId, params string[] lines)
		{
			StringBuilder bldr = new StringBuilder();
			if (fIncludeBOM)
				bldr.Append("\ufeff");
			if (!String.IsNullOrEmpty(sBookId))
			{
				bldr.Append(@"\id ");
				bldr.AppendLine(sBookId);
			}
			foreach (string sLine in lines)
				bldr.AppendLine(sLine);
			bldr.Length = bldr.Length - Environment.NewLine.Length;

			return bldr.ToString();
		}
		#endregion

		#region IFileOS Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified file exists. Looks in m_existingFiles.Keys without
		/// making any adjustment for case or differences in normalization.
		/// </summary>
		/// <param name="sPath">The file path.</param>
		/// ------------------------------------------------------------------------------------
		bool IFileOS.FileExists(string sPath)
		{
			FileUtils.AssertValidFilePath(sPath);
			if (String.IsNullOrEmpty(sPath))
				return false;
			// Can't use Contains because it takes care of normalization mismatches, but for
			// the purposes of these tests, we want to simulate an Operating System which doesn't
			// (e.g., MS Windows).
			foreach (string sExistingFile in m_existingFiles.Keys)
			{
				if (sExistingFile == sPath)
					return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified directory exists.
		/// </summary>
		/// <param name="sPath">The directory path.</param>
		/// ------------------------------------------------------------------------------------
		bool IFileOS.DirectoryExists(string sPath)
		{
			// Can't use Contains because it takes care of normalization mismatches, but for
			// the purposes of these tests, we want to simulate an Operating System which doesn't
			// (e.g., MS Windows).
			foreach (string sExistingDir in ExistingDirectories)
			{
				if (sExistingDir == sPath)
					return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the files in the given directory.
		/// </summary>
		/// <param name="sPath">The directory path.</param>
		/// <returns>list of files</returns>
		/// ------------------------------------------------------------------------------------
		string[] IFileOS.GetFilesInDirectory(string sPath)
		{
			return ((IFileOS)this).GetFilesInDirectory(sPath, "*");
		}

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
		string[] IFileOS.GetFilesInDirectory(string sPath, string searchPattern)
		{
			FileUtils.AssertValidFilePath(sPath);
			if (searchPattern == null)
				throw new ArgumentNullException("searchPattern");
			// These next two lines look a little strange, but I think we do this to deal with
			// normalization issues.
			int iDir = ExistingDirectories.IndexOf(sPath);
			if (iDir == -1)
			{
				sPath = sPath.TrimEnd(Path.DirectorySeparatorChar);
				iDir = ExistingDirectories.IndexOf(sPath);
			}
			string existingDir = iDir >= 0 ? ExistingDirectories[iDir] : null;

			Regex regex = null;
			if (searchPattern != "*")
			{
				searchPattern = searchPattern.Replace(".", @"\.").Replace("*", ".*").Replace("?", ".");
				searchPattern = searchPattern.Replace("+", @"\+").Replace("$", @"\$").Replace("(", @"\(").Replace(")", @"\)");
				regex = new Regex(searchPattern);
			}

			List<string> files = new List<string>(m_existingFiles.Count);
			foreach (string file in m_existingFiles.Keys)
			{
				if (regex != null)
				{
					string fileName = Path.GetFileName(file);
					Match m = regex.Match(fileName);
					if (m.Value != fileName)
						continue;
				}

				string fileDir = Path.GetDirectoryName(file);
				if (fileDir == String.Empty)
				{
					// Some of our tests just add files with no path and expect them to be
					// treated as existing in any existing directory
					files.Add(Path.Combine(existingDir, file));
				}
				else if (fileDir == sPath)
					files.Add(file);
			}
			if (files.Count == 0 && existingDir == null)
				throw new DirectoryNotFoundException();
			return files.ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writer.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		Stream IFileOS.OpenStreamForWrite(string filename)
		{
			return ((IFileOS)this).OpenStreamForWrite(filename, FileMode.OpenOrCreate);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a stream to write the given file in the given mode.
		/// </summary>
		/// <param name="filename">The fully-qualified file name</param>
		/// <param name="mode">The <see>FileMode</see> to use</param>
		/// <returns>A stream with write access</returns>
		/// ------------------------------------------------------------------------------------
		Stream IFileOS.OpenStreamForWrite(string filename, FileMode mode)
		{
			if (mode != FileMode.Open && mode != FileMode.OpenOrCreate)
				throw new NotImplementedException("Haven't implemented the mocked version of this method for file mode " + mode);
			if (!((IFileOS)this).FileExists(filename))
			{
				if (mode == FileMode.OpenOrCreate)
					AddFile(filename, string.Empty, Encoding.ASCII);
				else
					throw new FileNotFoundException("Could not find file " + filename);
			}
			MockFile finfo = GetFileInfo(filename, FileLockType.Write);
			MemoryStream stream = new MemoryStream(10);
			StreamWriter sw = new StreamWriter(stream, finfo.Encoding);
			sw.Flush();
			stream.Seek(0, SeekOrigin.Begin);
			finfo.AddStream(stream);
			return stream;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a memory stream to read m_FileContents using the encoding (m_FileEncoding)
		/// that was set at the time the file was added.
		/// </summary>
		/// <param name="filename">Not used</param>
		/// <returns>A stream with read access</returns>
		/// ------------------------------------------------------------------------------------
		Stream IFileOS.OpenStreamForRead(string filename)
		{
			MockFile finfo = GetFileInfo(filename, FileLockType.Read);
			//string fileContents = finfo.Contents;
			//byte[] contents = Encoding.Unicode.GetBytes(fileContents);
			MemoryStream stream = new MemoryStream(finfo.ContentsAsBytes, true);
			stream.Seek(0, SeekOrigin.Begin);
			finfo.AddStream(stream);
			return stream;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writer.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <param name="encoding">The encoding.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		TextWriter IFileOS.GetWriter(string filename, Encoding encoding)
		{
			if (!((IFileOS)this).FileExists(filename))
				AddFile(filename, string.Empty, encoding);
			MockFile finfo = GetFileInfo(filename, FileLockType.Write);
			Assert.AreEqual(finfo.Encoding, encoding);
			StringWriter writer = new StringWriter(new StringBuilder());
			finfo.AddStream(writer);
			return writer;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a TextReader on the given file
		/// </summary>
		/// <param name="filename">The fully-qualified filename</param>
		/// <param name="encoding">The encoding to use for interpreting the contents</param>
		/// ------------------------------------------------------------------------------------
		TextReader IFileOS.GetReader(string filename, Encoding encoding)
		{
			MockFile finfo = GetFileInfo(filename, FileLockType.Read);
			// Make sure the encoding of the file is the same as the requested type.
			// However, it is possible that we want an ASCII encoded file opened in the magic
			// code page to turn it into unicode.
			Assert.IsTrue(finfo.Encoding == encoding ||
				((finfo.Encoding == Encoding.ASCII || finfo.Encoding == Encoding.UTF8) &&
				encoding == Encoding.GetEncoding(FileUtils.kMagicCodePage)));
			StringReader reader = new StringReader(finfo.Contents.TrimStart('\ufeff'));
			finfo.AddStream(reader);
			return reader;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given file is reable and writeable.
		/// </summary>
		/// <param name="filename">The fully-qualified file name</param>
		/// <returns><c>true</c> if the file is reable and writeable; <c>false</c> if the file
		/// does not exist, is locked, is read-only, or has permissions set such that the user
		/// cannot read or write it.</returns>
		/// ------------------------------------------------------------------------------------
		bool IFileOS.IsFileReadableAndWritable(string filename)
		{
			return ((IFileOS)this).FileExists(filename) && GetFileInfo(filename).Lock == FileLockType.None;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the given file.
		/// This should probably be made case-insensitive
		/// </summary>
		/// <param name="filename">The fully-qualified file name</param>
		/// ------------------------------------------------------------------------------------
		void IFileOS.Delete(string filename)
		{
			if (((IFileOS)this).FileExists(filename))
			{
				GetFileInfo(filename, FileLockType.Write);
				m_existingFiles.Remove(GetKey(filename));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the specified source.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="destination">The destination.</param>
		/// ------------------------------------------------------------------------------------
		void IFileOS.Move(string source, string destination)
		{
			((IFileOS)this).Copy(source, destination);
			((IFileOS)this).Delete(source);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the specified source.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="destination">The destination.</param>
		/// ------------------------------------------------------------------------------------
		void IFileOS.Copy(string source, string destination)
		{
			m_existingFiles[destination] = GetFileInfo(source, FileLockType.Read);
		}

		/// <summary/>
		void IFileOS.CreateDirectory(string directory)
		{
			ExistingDirectories.Add(directory);
		}

		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file info for the given filename (Using a case-insensitive lookup).
		/// </summary>
		/// <param name="filename">The fully-qualified file name</param>
		/// <returns>The internal file information if the file is found in the list of
		/// existing files</returns>
		/// <exception cref="IOException">File not found</exception>
		/// ------------------------------------------------------------------------------------
		private MockFile GetFileInfo(string filename)
		{
			return GetFileInfo(filename, FileLockType.None);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file info for the given filename (Using a case-insensitive lookup).
		/// </summary>
		/// <param name="filename">The fully-qualified file name</param>
		/// <param name="lockNeeded">Indicates what type of access is needed. If a
		/// permission is needed that is not available, throws an IOException</param>
		/// <returns>The internal file information if the file is found in the list of
		/// existing files</returns>
		/// <exception cref="ArgumentNullException"><c>filename</c> is a null reference</exception>
		/// <exception cref="ArgumentException"><c>filename</c> is an empty string ("") or
		/// contains only white space</exception>
		/// <exception cref="FileNotFoundException">File not found</exception>
		/// <exception cref="IOException">File locked</exception>
		/// ------------------------------------------------------------------------------------
		private MockFile GetFileInfo(string filename, FileLockType lockNeeded)
		{
			if (filename == null)
				throw new ArgumentNullException("filename");
			if (filename.Trim() == string.Empty)
				throw new ArgumentException("Empty filename");

			MockFile finfo = m_existingFiles[GetKey(filename)];
			if (lockNeeded == FileLockType.None)
				return finfo;

			switch (finfo.Lock)
			{
				case FileLockType.Read:
					if (lockNeeded == FileLockType.Write)
						throw new IOException("File " + filename + " is locked (open for read).");
					break;
				case FileLockType.Write:
					throw new IOException("File " + filename + " is locked (open for write).");
			}
			return finfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the key for the file info for the given filename (Using a case-insensitive lookup).
		/// </summary>
		/// <param name="filename">The fully-qualified file name</param>
		/// <returns>The given filename or a correctly cased variation of it to serve as a key
		/// </returns>
		/// <exception cref="FileNotFoundException">File not found</exception>
		/// ------------------------------------------------------------------------------------
		private string GetKey(string filename)
		{
			if (m_existingFiles.ContainsKey(filename))
				return filename;
			string filenameLower = filename.ToLowerInvariant();
			foreach (string fileKey in m_existingFiles.Keys)
			{
				if (fileKey.ToLowerInvariant() == filenameLower)
					return fileKey;
			}
			throw new FileNotFoundException("File " + filename + " not found.");
		}
		#endregion
	}
}

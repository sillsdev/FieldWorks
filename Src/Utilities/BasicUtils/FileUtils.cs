// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FileUtils.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
#if __MonoCS__
using Mono.Unix.Native;
#endif

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Collection of File-related utilities.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class FileUtils
	{
		/// <summary>
		/// Code page 28591 will map all high-bit characters to the same
		/// value with a high-byte of 0 in unicode. For example, character 0x84
		/// will end up as 0x0084.
		/// </summary>
		public const int kMagicCodePage = 28591;

		#region FileUtils Manager class
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Allows setting a different IFileOS adapter (for testing purposes)
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public static class Manager
		{
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Sets the IFileOS adapter.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public static void SetFileAdapter(IFileOS adapter)
			{
				s_fileos = adapter;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Resets the IFileOS adapter to the default adapter which does access the real
			/// file system.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public static void Reset()
			{
				s_fileos = new SystemIOAdapter();
			}
		}
		#endregion

		#region SystemIOAdapter
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Normal implementation of IFileOS that delegates to the System.IO.File and
		/// System.IO.Directory
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		private class SystemIOAdapter : IFileOS
		{
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Determines whether the specified file exists.
			/// </summary>
			/// <param name="sPath">The file path.</param>
			/// ------------------------------------------------------------------------------------
			public bool FileExists(string sPath)
			{
				return File.Exists(sPath);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Determines whether the specified directory exists.
			/// </summary>
			/// <param name="sPath">The directory path.</param>
			/// ------------------------------------------------------------------------------------
			public bool DirectoryExists(string sPath)
			{
				return Directory.Exists(sPath);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the files in the given directory.
			/// </summary>
			/// <param name="sPath">The directory path.</param>
			/// <returns>list of files</returns>
			/// ------------------------------------------------------------------------------------
			public string[] GetFilesInDirectory(string sPath)
			{
				return Directory.GetFiles(sPath);
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
			public string[] GetFilesInDirectory(string sPath, string searchPattern)
			{
				return Directory.GetFiles(sPath, searchPattern);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Opens a stream to read the given file
			/// </summary>
			/// <param name="filename">The fully-qualified file name</param>
			/// <returns>A stream with read access</returns>
			/// ------------------------------------------------------------------------------------
			public Stream OpenStreamForRead(string filename)
			{
				return new FileStream(filename, FileMode.Open, FileAccess.Read);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Opens a stream to write the given file which is created if it doesn't exist
			/// </summary>
			/// <param name="filename">The fully-qualified file name</param>
			/// <returns>A stream with write access</returns>
			/// ------------------------------------------------------------------------------------
			public Stream OpenStreamForWrite(string filename)
			{
				return new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Opens a stream to write the given file in the given mode.
			/// </summary>
			/// <param name="filename">The fully-qualified file name</param>
			/// <param name="mode">The <see>FileMode</see> to use</param>
			/// <returns>A stream with write access</returns>
			/// ------------------------------------------------------------------------------------
			public Stream OpenStreamForWrite(string filename, FileMode mode)
			{
				return new FileStream(filename, mode, FileAccess.Write);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets a TextReader for the given file
			/// </summary>
			/// <param name="filename">The fully-qualified file name</param>
			/// <param name="encoding">The encoding to use for interpreting the contents</param>
			/// ------------------------------------------------------------------------------------
			public TextReader GetReader(string filename, Encoding encoding)
			{
				return new StreamReader(filename, encoding);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets a TextReader for the given file
			/// </summary>
			/// <param name="filename">The fully-qualified file name</param>
			/// <param name="encoding">The encoding.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			public TextWriter GetWriter(string filename, Encoding encoding)
			{
				return new StreamWriter(filename, false, encoding);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Determines whether the given file is reable and writeable
			/// </summary>
			/// <param name="filename">The fully-qualified file name</param>
			/// <returns><c>true</c> if the file is reable and writeable; <c>false</c> if the file
			/// does not exist, is locked, is read-only, or has permissions set such that the user
			/// cannot read or write it.</returns>
			/// ------------------------------------------------------------------------------------
			public bool IsFileReadableAndWritable(string filename)
			{
				bool fRetVal = false;
				try
				{
					using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite))
					{
						fRetVal = stream.CanRead && stream.CanWrite;
						stream.Close();
		}
				}
				catch
				{
					return false;
				}
				return fRetVal;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Deletes the given file
			/// </summary>
			/// <param name="filename">The fully-qualified file name</param>
			/// ------------------------------------------------------------------------------------
			public void Delete(string filename)
			{
				File.Delete(filename);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Moves the specified source.
			/// </summary>
			/// <param name="source">The source.</param>
			/// <param name="destination">The destination.</param>
			/// ------------------------------------------------------------------------------------
			public void Move(string source, string destination)
			{
				File.Move(source, destination);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Copies the specified source.
			/// </summary>
			/// <param name="source">The source.</param>
			/// <param name="destination">The destination.</param>
			/// ------------------------------------------------------------------------------------
			public void Copy(string source, string destination)
			{
				File.Copy(source, destination);
			}
		}
		#endregion

		private static IFileOS s_fileos = new SystemIOAdapter();

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the gievn paths are equal, as defined by the current operating
		/// system. This does not attempt to resolve relative paths or environment variables in
		/// paths. On Windows, it will handle mismatching forward and backward slashes.
		/// </summary>
		/// <param name="file1">The file1.</param>
		/// <param name="file2">The file2.</param>
		/// <returns><c>true</c> if the OS would consider the two paths equal</returns>
		/// ------------------------------------------------------------------------------------
		public static bool PathsAreEqual(string file1, string file2)
		{
			if (MiscUtils.IsUnix)
				return string.Equals(file1, file2, StringComparison.InvariantCulture);
			return string.Equals(file1.Replace('/', '\\'), file2.Replace('/', '\\'),
				StringComparison.InvariantCultureIgnoreCase);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens the two files specified and determines whether or not they are byte-for-byte
		/// identical.
		/// </summary>
		/// <param name="file1"></param>
		/// <param name="file2"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool AreFilesIdentical(string file1, string file2)
		{
			TextReader sr1 = null;
			TextReader sr2 = null;
			try
			{
				sr1 = OpenFileForRead(file1, Encoding.Default);
				sr2 = OpenFileForRead(file2, Encoding.Default);
				string str1 = sr1.ReadToEnd();
				string str2 = sr2.ReadToEnd();

				return str1 == str2;
			}
			catch
			{
				return false;
			}
			finally
			{
				if (sr1 != null)
					sr1.Dispose();
				if (sr2 != null)
					sr2.Dispose();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the file encoding for a Standard Format file
		/// </summary>
		/// <param name="fileName">file name to check</param>
		/// <returns>the file encoding for the file</returns>
		/// <remarks>This method will throw file exceptions if open or read errors
		/// occur</remarks>
		/// ------------------------------------------------------------------------------------
		public static Encoding DetermineSfFileEncoding(string fileName)
		{
			// read the BOM. If one exists, then the task is done.
			Encoding enc = EncodingFromBOM(fileName);
			if (enc != Encoding.ASCII)
				return enc;

			// Read a portion of the file to test
			byte[] buffer = ReadFileData(fileName);
			return ParseBuffer(buffer);
		}

		/// <returns>
		/// Unicode Encoding specified by BOM at the beginning of file,
		/// or ASCII if no BOM was found.
		/// </returns>
		public static Encoding EncodingFromBOM(string path)
		{
			// UTF-32 BOM is 4 bytes
			var beginning = ReadFileData(path, 4);

			var BOMs = new List<Encoding>()
			{
				Encoding.UTF32, // UTF-32 Little Endian
				Encoding.UTF8, // UTF-8
				Encoding.BigEndianUnicode, // UTF-16 Big Endian
				Encoding.Unicode, // UTF-16 Little Endian
			};

			foreach (var bom in BOMs)
				// If file starts with BOM
				if (beginning.IndexOfSubArray(bom.GetPreamble()) == 0)
					return bom;
			return Encoding.ASCII;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the files in the given directory.
		/// </summary>
		/// <param name="sPath">The directory path.</param>
		/// <returns>list of files</returns>
		/// ------------------------------------------------------------------------------------
		public static string[] GetFilesInDirectory(string sPath)
		{
			return s_fileos.GetFilesInDirectory(sPath);
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
		public static string[] GetFilesInDirectory(string sPath, string searchPattern)
		{
			return s_fileos.GetFilesInDirectory(sPath, searchPattern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try to match the given pathname to an existing file, playing whatever games are
		/// necessary with Unicode normalization.  (See LT-8726.)
		/// </summary>
		/// <param name="sPathname">full pathname of a file</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string ActualFilePath(string sPathname)
		{
			if (s_fileos.FileExists(sPathname))
				return sPathname;
			if (s_fileos.FileExists(sPathname.Normalize()))	// NFC
				return sPathname.Normalize();
			if (s_fileos.FileExists(sPathname.Normalize(NormalizationForm.FormD)))	// NFD
				return sPathname.Normalize(NormalizationForm.FormD);

			string sDir = Path.GetDirectoryName(sPathname);
			string sFile = Path.GetFileName(sPathname).Normalize();
			if (!MiscUtils.IsUnix) // Using IsUnix to decide if a file system is case sensitive
			{					// isn't the best way since some Unix file systems are case
								// insensitive, but it works for most cases
				sFile = sFile.ToLowerInvariant();
			}

			if (!s_fileos.DirectoryExists(sDir))
				sDir = sDir.Normalize();
			if (!s_fileos.DirectoryExists(sDir))
				sDir = sDir.Normalize(NormalizationForm.FormD);
			if (s_fileos.DirectoryExists(sDir))
			{
				foreach (string sPath in s_fileos.GetFilesInDirectory(sDir))
				{
					string sName = MiscUtils.IsUnix ? Path.GetFileName(sPath).Normalize() :
						Path.GetFileName(sPath).Normalize().ToLowerInvariant();
					if (sName == sFile)
						return sPath;
				}
			}
			// nothing matches, so return the original pathname
			return sPathname;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Throw an exception if a file path is not well-formed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void AssertValidFilePath(string filename)
		{
			try
			{
				new FileInfo(filename);
			}
			catch (SecurityException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if a file path is well-formed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsFilePathValid(string filename)
		{
			try
			{
				AssertValidFilePath(filename);
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if given URI is a file uri or a path.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsFileUriOrPath(string uri)
		{
			string[] nonFileUris = { Uri.UriSchemeFtp, Uri.UriSchemeGopher, Uri.UriSchemeHttp,
									Uri.UriSchemeHttps, Uri.UriSchemeMailto, Uri.UriSchemeNetPipe,
									Uri.UriSchemeNetTcp, Uri.UriSchemeNews, Uri.UriSchemeNntp,
									"silfw" };

			return (nonFileUris.Where(x => uri.Trim().StartsWith(x + ":")).Count() == 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given file is accessible for reading
		/// </summary>
		/// <param name="filename">The fully-qualified filename</param>
		/// ------------------------------------------------------------------------------------
		public static bool IsFileReadable(string filename)
		{
			try
			{
				using (Stream s = s_fileos.OpenStreamForRead(filename))
				{
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a TextReader on the given file
		/// </summary>
		/// <param name="filename">The fully-qualified filename</param>
		/// <param name="encoding">The encoding to use for interpreting the contents</param>
		/// ------------------------------------------------------------------------------------
		public static TextReader OpenFileForRead(string filename, Encoding encoding)
		{
			return s_fileos.GetReader(filename, encoding);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a file for reading as a binary file.
		/// </summary>
		/// <param name="filename">The filename</param>
		/// <param name="encoding">The file encoding</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static BinaryReader OpenFileForBinaryRead(string filename, Encoding encoding)
		{
			return new BinaryReader(s_fileos.OpenStreamForRead(filename), encoding);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a TextReader on the given file
		/// </summary>
		/// <param name="filename">The fully-qualified filename</param>
		/// <param name="encoding">The encoding.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static TextWriter OpenFileForWrite(string filename, Encoding encoding)
		{
			return s_fileos.GetWriter(filename, encoding);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a file for write as a binary file.
		/// </summary>
		/// <param name="filename">The filename</param>
		/// <param name="encoding">The file encoding</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static BinaryWriter OpenFileForBinaryWrite(string filename, Encoding encoding)
		{
			return new BinaryWriter(s_fileos.OpenStreamForWrite(filename), encoding);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens an existing file for writing.
		/// </summary>
		/// <param name="filename">The filename</param>
		/// <returns>An Stream object on the specified path with Write access.</returns>
		/// ------------------------------------------------------------------------------------
		public static Stream OpenWrite(string filename)
		{
			return s_fileos.OpenStreamForWrite(filename, FileMode.Open);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes the string to file.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <param name="contents">The contents to write to the file.</param>
		/// <param name="encoding">The encoding.</param>
		/// ------------------------------------------------------------------------------------
		public static void WriteStringtoFile(string filename, string contents, Encoding encoding)
		{
			using (TextWriter write = OpenFileForWrite(filename, encoding))
			{
				write.Write(contents);
				write.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given file is reable and writeable
		/// </summary>
		/// <param name="filename">The fully-qualified file name</param>
		/// <returns><c>true</c> if the file is reable and writeable; <c>false</c> if the file
		/// does not exist, is locked, is read-only, or has permissions set such that the user
		/// cannot read or write it.</returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsFileReadableAndWritable(string filename)
		{
			return s_fileos.IsFileReadableAndWritable(filename);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a temporary filename
		/// </summary>
		/// <param name="extension">The extension of the filename or null for '.tmp'.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetTempFile(string extension)
		{
			var fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			return Path.ChangeExtension(fileName, extension ?? ".tmp");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the specified file.
		/// </summary>
		/// <param name="file">The file.</param>
		/// ------------------------------------------------------------------------------------
		public static void Delete(string file)
		{
			s_fileos.Delete(file);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks whether the specified filename exists.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// ------------------------------------------------------------------------------------
		public static bool FileExists(string filename)
		{
			return s_fileos.FileExists(filename);
		}

		/// <summary>
		/// Checks whether the specified file exists. Check if the file exists with diacritics composed or
		/// decomposed. Also check variations for unix/linux.
		/// </summary>
		/// <param name="sPath"></param>
		/// <param name="sCorrectedPath"></param>
		/// <returns></returns>
		public static bool TrySimilarFileExists(string sPath, out string sCorrectedPath)
		{
			sCorrectedPath = ActualFilePath(sPath);
			return s_fileos.FileExists(sCorrectedPath);
		}

		/// <summary>
		/// Checks whether the specified file exists. Check if the file exists with diacritics composed or
		/// decomposed. Also check variations for unix/linux.
		/// </summary>
		/// <param name="sPath"></param>
		/// <returns></returns>
		public static bool SimilarFileExists(string sPath)
		{
			var sCorrectedPath = ActualFilePath(sPath);
			return s_fileos.FileExists(sCorrectedPath);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks whether the specified directory exists.
		/// </summary>
		/// <param name="path">The directory path.</param>
		/// ------------------------------------------------------------------------------------
		public static bool DirectoryExists(string path)
		{
			return s_fileos.DirectoryExists(path);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the given directory exists and contains one or more
		/// files or subdirectories.
		/// </summary>
		/// <param name="path">The path.</param>
		/// ------------------------------------------------------------------------------------
		public static bool NonEmptyDirectoryExists(string path)
		{
			return DirectoryExists(path) &&
				(Directory.GetDirectories(path).Length > 0 ||
				GetFilesInDirectory(path).Length > 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that the backup directory exists. If it doesn't exist, attempt to create it.
		/// </summary>
		/// <param name="directory">directory to check</param>
		/// <returns>true if the current backup directory exists or could be created;
		/// false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool EnsureDirectoryExists(string directory)
		{
			if (DirectoryExists(directory))
				return true;

			// Attempt to create the directoy if it doesn't exist yet.
			try
			{
				Directory.CreateDirectory(directory); // Review Mono (JohnT): do we need this in s_fileos?
				return true;
			}
			catch
			{
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the specified file.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="destination">The destination.</param>
		/// ------------------------------------------------------------------------------------
		public static void Move(string source, string destination)
		{
			s_fileos.Move(source, destination);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the specified file.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="destination">The destination.</param>
		/// ------------------------------------------------------------------------------------
		public static void Copy(string source, string destination)
		{
			s_fileos.Copy(source, destination);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the specified file to the temp directory, in the given subdirectory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void MoveFileToTempDirectory(string filename, string subdir)
		{
			string tempdir = Path.GetTempPath();
			string destdir = subdir == null ? tempdir : Path.Combine(tempdir, subdir);
			if (!Directory.Exists(destdir))
				Directory.CreateDirectory(destdir);
			string destfile = Path.Combine(destdir, Path.GetFileName(filename));
			if (FileExists(destfile))
				Delete(destfile);
			Move(filename, destfile);
		}

		#region FileDialogFilterCaseInsensitiveCombinations
		/// <summary>
		/// Take a FileDialog.Filter and produce a variant with the same description
		/// but new patterns. The new patterns are all the case variations of the old pattern,
		/// allowing them to be used in a way that is effective case insensitive.
		/// This is helpful on platforms with case-sensitive file systems.
		/// For example, passing "CS files (*.cs)|*.cs"
		///           results in "CS files (*.cs)|*.cs; *.cS; *.Cs; *.CS".
		/// Fixes FWNX-320.
		/// Ignored in Windows since unnecessary.
		/// </summary>
		public static string FileDialogFilterCaseInsensitiveCombinations(string filter)
		{
			// Windows is already case insensitive.
			if (!MiscUtils.IsUnix)
				return filter;

			List<string> filterComponents = new List<string>(filter.Split(new char[] {'|'}));

			StringBuilder o = new StringBuilder();
			for (int i = 0; i < filterComponents.Count - 1; i += 2)
			{
				o.AppendFormat("{0}|{1}|", filterComponents[i],
					ProduceCaseInsenstiveCombinationsFromMultipleTokens(filterComponents[i + 1]));
			}

			return o.ToString().Trim(new char[] {'|'});
		}

		/// <summary>
		/// Take a sequence of strings, such as "*.png; *.jpg" and make a sequence of all
		/// possible case combinations, such as "*.png; *.pnG; *.pNg; *.pNG; ...; *.jpg; *.jpG; ...".
		/// </summary>
		private static string ProduceCaseInsenstiveCombinationsFromMultipleTokens(string input)
		{
			List<string> components = new List<string>(input.Split(new string[] {"; ", ";"},
				StringSplitOptions.RemoveEmptyEntries));

			StringBuilder output = new StringBuilder();
			foreach (var component in components)
				output.AppendFormat("{0}; ", ProduceCaseInsenstiveCombinations(component));

			return output.ToString().Trim(new char[] {' ', ';'});
		}

		/// <summary>
		/// Take a string, such as "*.png" and make a sequence of all possible
		/// case combinations, such as "*.png; *.pnG; *.pNg; *.pNG; ...".
		/// </summary>
		private static string ProduceCaseInsenstiveCombinations(string input)
		{
			StringBuilder output = new StringBuilder();

			List<string> combinations = new List<string>();

			// Use binary numbers to represent lower and upper case. For every binary number that
			// could represent a different case combination for input's characters,
			// use it to build up the list of combinations.
			for (int caseSpecification = 0; caseSpecification < Math.Pow(2, input.Length); caseSpecification++)
			{
				string newCombination = ApplyCaseByBinary(input, caseSpecification);

				// Don't duplicate combinations (possible with non-alpha characters)
				if (!combinations.Contains(newCombination))
					combinations.Add(newCombination);
			}

			foreach (var combination in combinations)
			{
				output.AppendFormat("{0}; ", combination);
			}

			return output.ToString().Trim(new char[] {' ', ';'});
		}

		/// <summary>
		/// Apply cases to characters in input from cases specified in caseSpecification.
		/// caseSpecification specifies cases as a binary number where 0 digits mean lower case
		/// and 1 digits mean upper case.
		/// For example, if input is "aaaa" and caseSpecification is binary 0101, then
		/// "aAaA" will be returned.
		/// </summary>
		/// <remarks>
		/// The least-significant digits of caseSpecification are used before more
		/// significant digits. Some significant digits may be unused if caseSpecification
		/// is a larger number than it needs to be for the Length of input.
		/// For example, if input is "aaaa" and caseSpecification is binary 11110101, then
		/// "aAaA" will still be returned, not "AAAA".
		/// </remarks>
		private static string ApplyCaseByBinary(string input, int caseSpecification)
		{
			string output = String.Empty;
			for (int i = input.Length - 1; i >= 0; i--)
			{
				if (caseSpecification % 2 == 0) // binary number ends in 0
					output = new String(input[i], 1).ToLower() + output; // prepend letter as lowercase
				else // binary number ends in 1
					output = new String(input[i], 1).ToUpper() + output; // prepend letter as uppercase
				caseSpecification >>= 1;
			}
			return output;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open a Stream for reading the file. Caller is responsible for closing the Stream.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Stream OpenStreamForRead(string fileName)
		{
			return s_fileos.OpenStreamForRead(fileName);
		}

		/// <summary>
		/// Helper method for multi-platform paths.
		/// In Windows, returns the input string unmodified.
		/// In Linux, converts "..\dir\dir\file.txt" to "../dir/dir/file.txt",
		/// and "C:\dir\dir\file.txt" to "/dir/dir/file.txt".
		/// Paths starting with drive letters other than "C:" are not handled since they
		/// are not as easily interpretable as a Linux path, and an exception will be thrown.
		/// </summary>
		public static string ChangeWindowsPathIfLinux(string windowsPath)
		{
			if (!MiscUtils.IsUnix)
				return windowsPath;
			if (windowsPath == null)
				return windowsPath;
			// Strip off C:
			if (windowsPath.StartsWith("C:", true, null))
				windowsPath = windowsPath.Substring("C:".Length);
			else
			{
				// Other drive letters not allowed
				if (windowsPath.Length >= "X:".Length)
					if (MiscUtils.IsAlpha(windowsPath[0].ToString()) && windowsPath[1] == ':')
						throw new ArgumentException(
							"Drive letters other than C: are not supported by ChangeWindowsPathIfLinux.");
			}
			return windowsPath.Replace('\\', Path.DirectorySeparatorChar);
		}

		/// <summary>
		/// Processes windowsPath using ChangeWindowsPathIfLinux(), while preserving and
		/// not processing prefix at the beginning of windowsPath, if present.
		/// This allows paths to be processed which are prefixed by
		/// FwObjDataTypes.kodtExternalPathName.
		/// </summary>
		public static string ChangeWindowsPathIfLinuxPreservingPrefix(string windowsPath,
			string prefix)
		{
			if (windowsPath == null || prefix == null || !windowsPath.StartsWith(prefix))
				return ChangeWindowsPathIfLinux(windowsPath);
			// Preserve prefix
			windowsPath = windowsPath.Substring(prefix.Length);
			return prefix + ChangeWindowsPathIfLinux(windowsPath);
		}

		/// <summary>
		/// Helper method for multi-platform paths.
		/// In Linux, returns the input string unmodified.
		/// In Windows, converts "../dir/dir/file.txt" to "..\dir\dir\file.txt",
		/// and "/dir/dir/file.txt" to "C:\dir\dir\file.txt".
		/// Paths to removable media such as "/media/cdrom/dir" will simply
		/// be converted to "C:\media\cdrom\dir".
		/// </summary>
		public static string ChangeLinuxPathIfWindows(string linuxPath)
		{
			if (MiscUtils.IsUnix)
				return linuxPath;
			if (String.IsNullOrEmpty(linuxPath))
				return linuxPath;
			// Collapse two or more adjacent slashes
			if (linuxPath.Contains("//"))
				linuxPath = Regex.Replace(linuxPath, "/+", "/");
			// Prepend "C:" for absolute paths)
			if (linuxPath[0] == '/')
				linuxPath = "C:" + linuxPath;
			return linuxPath.Replace('/', Path.DirectorySeparatorChar);
		}

		/// <summary>
		/// Processes linuxPath using ChangeLinuxPathIfWindows(), while preserving and
		/// not processing prefix at the beginning of linuxPath, if present.
		/// This allows paths to be processed which are prefixed by
		/// FwObjDataTypes.kodtExternalPathName.
		/// </summary>
		public static string ChangeLinuxPathIfWindowsPreservingPrefix(string linuxPath,
			string prefix)
		{
			if (linuxPath == null || prefix == null || !linuxPath.StartsWith(prefix))
				return ChangeLinuxPathIfWindows(linuxPath);
			// Preserve prefix
			linuxPath = linuxPath.Substring(prefix.Length);
			return prefix + ChangeLinuxPathIfWindows(linuxPath);
		}

		/// <summary>
		/// Process path using ChangeWindowsPathIfLinux or ChangeLinuxPathIfWindows,
		/// as appropriate, to change the path to the style of path of the current
		/// platform.
		/// For example, on Windows, changes "/dir/file.txt" to "C:\dir\file.txt",
		/// and on Linux, changes "C:\dir\file.txt" to "/dir/file.txt".
		/// </summary>
		public static string ChangePathToPlatform(string path)
		{
			if (MiscUtils.IsUnix)
				return ChangeWindowsPathIfLinux(path);
			return ChangeLinuxPathIfWindows(path);
		}

		/// <summary>
		/// Processes path using ChangeWindowsPathIfLinuxPreservingPrefix or
		/// ChangeLinuxPathIfWindowsPreservingPrefix, as appropriate, to change the path
		/// to the style of path of the current platform, while preserving and
		/// not processing prefix at the beginning of path, if present.
		/// For example, on Windows, changes "/dir/file.txt" to "C:\dir\file.txt",
		/// and on Linux, changes "C:\dir\file.txt" to "/dir/file.txt".
		///
		/// When converting a Windows path on Linux, paths starting with drive letters other
		/// than "C:" are not handled since they are not as easily interpretable as a
		/// Linux path, and an exception will be thrown.
		/// </summary>
		public static string ChangePathToPlatformPreservingPrefix(string path, string prefix)
		{
			if (MiscUtils.IsUnix)
				return ChangeWindowsPathIfLinuxPreservingPrefix(path, prefix);
			return ChangeLinuxPathIfWindowsPreservingPrefix(path, prefix);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a relative path if the specified full path contains any of the specified
		/// folders. If the full path doesn't start with any of the specified folders, then
		/// the full path is returned.
		/// </summary>
		/// <param name="fullPath">The full path.</param>
		/// <param name="relFolderSideEffect">Action to run if the full path starts with one of
		/// specified folders.</param>
		/// <param name="foldersToCheck">The list of folders to check.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetRelativePath(string fullPath, Action<string> relFolderSideEffect,
			params string[] foldersToCheck)
		{
			foreach (string dir in foldersToCheck)
			{
				if (fullPath.StartsWith(dir))
				{
					if (relFolderSideEffect != null)
						relFolderSideEffect(dir); // Notify action that we found a relative folder

					bool dirEndsWithSeparator = dir.EndsWith(Path.DirectorySeparatorChar.ToString());
					return fullPath.Substring(dir.Length + (dirEndsWithSeparator ? 0 : 1));
				}
			}

			return fullPath;
		}

		/// <summary>
		/// Strips file URI prefix from the beginning of a file URI string, and keeps
		/// a beginning slash if in Linux.
		/// eg "file:///C:/Windows" becomes "C:/Windows" in Windows, and
		/// "file:///usr/bin" becomes "/usr/bin" in Linux.
		/// Returns the input unchanged if it does not begin with "file:".
		///
		/// Does not convert the result into a valid path or a path using current platform
		/// path separators.
		/// fileString does not neet to be a valid URI. We would like to treat it as one
		/// but since we import files with file URIs that can be produced by other
		/// tools (eg LIFT) we can't guarantee that they will always be valid.
		///
		/// File URIs, and their conversation to paths, are more complex, with hosts,
		/// forward slashes, and escapes, but just stripping the file URI prefix is
		/// what's currently needed.
		/// Different places in code need "file://', or "file:///" removed.
		///
		/// See uri.LocalPath, http://en.wikipedia.org/wiki/File_URI , and
		/// http://blogs.msdn.com/b/ie/archive/2006/12/06/file-uris-in-windows.aspx .
		///
		/// FWNX-607
		/// </summary>
		public static string StripFilePrefix(string fileString)
		{
			if (fileString == null)
				return fileString;

			string prefix = Uri.UriSchemeFile + ":";

			if (!fileString.StartsWith(prefix))
				return fileString;

			string path = fileString.Substring(prefix.Length);
			// Trim any number of beginning slashes
			path = path.TrimStart('/');
			// Prepend slash on Linux
			if (MiscUtils.IsUnix)
				path = '/' + path;

			return path;
		}

		/// <summary>
		/// Set chmod a+x on a path in Linux. No-op in Windows.
		/// The x mode bit will make a file executable or a directory searchable.
		/// </summary>
		public static void SetExecutable(string path)
		{
			#if __MonoCS__
			if (!FileUtils.FileExists(path) && !FileUtils.DirectoryExists(path))
				throw new FileNotFoundException();

			var fileStat = new Mono.Unix.Native.Stat();
			Mono.Unix.Native.Syscall.stat(path, out fileStat);
			var originalMode = fileStat.st_mode;
			var modeWithExecute = originalMode | FilePermissions.S_IXUSR |
				FilePermissions.S_IXGRP | FilePermissions.S_IXOTH;
			Mono.Unix.Native.Syscall.chmod(path, modeWithExecute);
			#endif
		}

		/// <summary>
		/// Linux utility function. In Linux, returns true if path is chmod +x, else false.
		/// I.e. if path is an executable file or searchable directory.
		/// In Windows, always returns true (or throws if not exist).
		/// </summary>
		public static bool IsExecutable(string path)
		{
			if (!FileUtils.FileExists(path) && !FileUtils.DirectoryExists(path))
				throw new FileNotFoundException();

			#if !__MonoCS__
			return true;
			#else
			return Mono.Unix.Native.Syscall.access(path, Mono.Unix.Native.AccessModes.X_OK) == 0;
			#endif
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse the data buffer to determine an encoding type
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		/// <remarks>We removed support for UTF32 when porting from C++</remarks>
		/// ------------------------------------------------------------------------------------
		private static Encoding ParseBuffer(byte[] buffer)
		{
			// First check for UTF8
			if (IsUTF8String(buffer))
				return Encoding.UTF8;

			// Keep counts of possible UTF16 BE and LE characters seen
			int utf16le = 0;
			int utf16be = 0;
			int backslashCounter = 0;

			for (int pos = 0; pos < buffer.Length; pos++)
			{
				if (buffer[pos] == '\\')
				{
					// check for UTF16
					if (pos % 2 == 0) // even bytes
					{
						if (pos + 1 < buffer.Length && buffer[pos + 1] == 0x00)
							utf16le++;
					}
					else // odd bytes
					{
						if (pos - 1 >= 0 && buffer[pos - 1] == 0x00)
							utf16be++;
					}

					backslashCounter++;
				}
			}
			int total16 = utf16le + utf16be;
			// If more than half of the backslash characters were determined to be
			// UTF16 then assume that it is a UTF16 file
			if (total16 > backslashCounter / 2)
				return (utf16be > utf16le) ? Encoding.BigEndianUnicode : Encoding.Unicode;
			return Encoding.ASCII;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the data is UTF8 encoded
		/// </summary>
		/// <param name="utf8"></param>
		/// <returns></returns>
		/// <remarks>This algorithm uses the pattern described in the Unicode book for
		/// UTF-8 data patterns</remarks>
		/// ------------------------------------------------------------------------------------
		private static bool IsUTF8String(byte[] utf8)
		{
			const byte kLeft1BitMask = 0x80;
			const byte kLeft2BitsMask = 0xC0;
			const byte kLeft3BitsMask = 0xE0;
			const byte kLeft4BitsMask = 0xF0;
			const byte kLeft5BitsMask = 0xF8;

			// If there is no data or too few characters, it is not UTF8
			if (utf8.Length < 10)
				return false;

			int sequenceLen = 1;
			bool multiByteSequenceFound = false;
			// look through the buffer but stop 10 bytes before so we don't run off the
			// end while checking
			for (int i = 0; i < utf8.Length - 10; i += sequenceLen)
			{
				byte by = utf8[i];
				// If the leftmost bit is 0, then this is a 1-byte character
				if ((by & kLeft1BitMask) == 0)
					sequenceLen = 1;
				else if((by & kLeft3BitsMask) == kLeft2BitsMask)
				{
					// If the byte starts with 110, then this will be the first byte
					// of a 2-byte sequence
					sequenceLen = 2;
					// if the second byte does not start with 10 then the sequence is invalid
					if((utf8[i + 1] & kLeft2BitsMask) != 0x80)
						return false;
				}
				else if((by & kLeft4BitsMask) == kLeft3BitsMask)
				{
					// If the byte starts with 1110, then this will be the first byte of
					// a 3-byte sequence
					sequenceLen = 3;
					if ((utf8[i + 1] & kLeft2BitsMask) != 0x80)
						return false;
					if ((utf8[i + 2] & kLeft2BitsMask) != 0x80)
						return false;
				}
				else if((by & kLeft5BitsMask) == kLeft4BitsMask)
				{
					// if the byte starts with 11110, then this will be the first byte of
					// a 4-byte sequence
					sequenceLen = 4;
					if ((utf8[i + 1] & kLeft2BitsMask) != 0x80)
						return false;
					if ((utf8[i + 2] & kLeft2BitsMask) != 0x80)
						return false;
					if ((utf8[i + 3] & kLeft2BitsMask) != 0x80)
						return false;
				}
				else
					return false;

				if (sequenceLen > 1)
					multiByteSequenceFound = true;
			}
			return multiByteSequenceFound;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read a block of data from the file (up to 16K)
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static byte[] ReadFileData(string fileName)
		{
			return ReadFileData(fileName, 16*1024);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read a block of data from the file (up to cMaxBytes)
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="cMaxBytes">Maximum number of bytes to read</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static byte[] ReadFileData(string fileName, int cMaxBytes)
		{
			using (Stream stream = s_fileos.OpenStreamForRead(fileName))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					return reader.ReadBytes(cMaxBytes);
				}
		}
		#endregion
	}
}

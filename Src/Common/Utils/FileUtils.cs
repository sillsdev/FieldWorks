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
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text;

namespace SIL.FieldWorks.Common.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Collection of File-related utilities.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FileUtils
	{
		private static IFileOS s_fileos;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the <see cref="FileUtils"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static FileUtils()
		{
			s_fileos = new SystemIOProxy();
		}

		#region Public methods
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
			StreamReader sr1 = null;
			StreamReader sr2 = null;
			try
			{
				sr1 = new StreamReader(file1);
				sr2 = new StreamReader(file2);
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
					sr1.Close();
				if (sr2 != null)
					sr2.Close();
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
			Encoding enc = ReadBOM(fileName);
			if (enc != Encoding.ASCII)
				return enc;

			// Read a portion of the file to test
			byte[] buffer = ReadFileData(fileName);
			return ParseBuffer(buffer);
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
			string sFile = Path.GetFileName(sPathname).Normalize().ToLowerInvariant();
			if (!s_fileos.DirectoryExists(sDir))
				sDir = sDir.Normalize();
			if (!s_fileos.DirectoryExists(sDir))
				sDir = sDir.Normalize(NormalizationForm.FormD);
			if (s_fileos.DirectoryExists(sDir))
			{
				foreach (string sPath in s_fileos.GetFilesInDirectory(sDir))
				{
					string sName = Path.GetFileName(sPath).Normalize().ToLowerInvariant();
					if (sName == sFile)
						return sPath;
				}
			}
			// nothing matches, so return the original pathname
			return sPathname;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if a file path is well-formed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsPathNameValid(string pathName)
		{
			// Attempt to get the path name and device, if applicable.
			char[] invalidChars = Path.GetInvalidPathChars();
			string strDirectory;
			string strRoot;
			try
			{
				strDirectory = Path.GetDirectoryName(pathName);
				strRoot = Path.GetPathRoot(pathName);
			}
			catch { return false; }

			// If the device is specified, make sure that it is well-formed.
			if (string.IsNullOrEmpty(strRoot) &&
				pathName.Contains(Path.VolumeSeparatorChar.ToString()))
			{
				return false;
			}

			// Make sure the path doesn't contain any
			foreach (char ch in invalidChars)
			{
				if (strDirectory.Contains(ch.ToString()))
					return false;
			}
			return true; // IsFileNameValid(Path.GetFileName(pathName));
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
					int inc = pos % 2;
					if (inc == 0)
					{
						if (pos + 1 <= buffer.Length)
						{
							if (buffer[pos + 1] == 0x00)
								utf16le++;
						}
					}
					else if (inc == 1)
					{
						if (pos - 1 >= 0)
						{
							if (buffer[pos - 1] == 0x00)
								utf16be++;
						}
					}

					backslashCounter++;
				}
			}
			int total16 = utf16le + utf16be;
			// If more than half of the backslash characters were determined to be
			// UTF16 then assume that it is a UTF16 file
			if (total16 > backslashCounter / 2)
			{
				if (utf16be > utf16le)
					return Encoding.BigEndianUnicode;
				else
					return Encoding.Unicode;
			}
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
		/// Look at the start of a file to see if there is a byte order mark.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns>the unicode encoding if there is a BOM, otherwise ASCII</returns>
		/// ------------------------------------------------------------------------------------
		private static Encoding ReadBOM(string fileName)
		{
			byte[] data = ReadFileData(fileName, 3);

			// If there are not enough bytes for a 16-bit BOM, assume ASCII
			if (data == null || data.Length < 2)
				return Encoding.ASCII;

			// Look for a BOM in the various unicode formats
			if (data[0] == 0xfe && data[1] == 0xff)
				return Encoding.BigEndianUnicode;
			if (data[0] == 0xff && data[1] == 0xfe)
				return Encoding.Unicode;

			// If there are not enough bytes for a UTF-8, assume ASCII
			if (data.Length < 3)
				return Encoding.ASCII;

			if (data[0] == 0xef && data[1] == 0xbb && data[2] == 0xbf)
				return Encoding.UTF8;
			return Encoding.ASCII;
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
		/// Read a block of data from the file (up to 16K)
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="cMaxBytes">Maximum number of bytes to read</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static byte[] ReadFileData(string fileName, int cMaxBytes)
		{
			// Open the file and read up to 16K
			using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			{
				using (BinaryReader reader = new BinaryReader(fs))
				{
					return reader.ReadBytes(cMaxBytes);
				}
			}
		}
		#endregion
	}

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
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Normal implementation of IFile that delegates to the System.IO.File and
	/// System.IO.Directory
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SystemIOProxy : IFileOS
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
	}
}

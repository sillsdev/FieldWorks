// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TempSFFileMaker.cs
// Responsibility: TE Team
//
// <remarks>
// Class to manage creation (and retiring) of temp SF files
// </remarks>

using System;
using System.IO;
using System.Text;

using SIL.Utils;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TempSFFileMaker.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TempSFFileMaker
	{
		private static readonly MockFileOS s_fileOs = new MockFileOS();

		static TempSFFileMaker()
		{
			// We never have a real file system for this class
			FileUtils.Manager.SetFileAdapter(s_fileOs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a temp file containing a single book
		/// </summary>
		/// <param name="sSILBookId">Three-letter SIL code for the book represented by the file
		/// being created</param>
		/// <param name="dataLines">Array of lines of SF text to write following the ID line.
		/// This text should not include line-break characters.</param>
		/// <returns>Name of file that was created</returns>
		/// ------------------------------------------------------------------------------------
		public string CreateFile(string sSILBookId, string[] dataLines)
		{
			return CreateFile(sSILBookId, dataLines, Encoding.ASCII, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a temp file containing a single book
		/// </summary>
		/// <param name="sSILBookId">Three-letter SIL code for the book represented by the file
		/// being created</param>
		/// <param name="dataLines">Array of lines of SF text to write following the ID line.
		/// This text should not include line-break characters.</param>
		/// <param name="encoding">The character encoding</param>
		/// <param name="fWriteByteOrderMark">Pass <c>true</c> to have the BOM written at the
		/// beginning of a Unicode file (ignored if encoding is ASCII).</param>
		/// <returns>Name of file that was created</returns>
		/// ------------------------------------------------------------------------------------
		public string CreateFile(string sSILBookId, string[] dataLines, Encoding encoding, bool fWriteByteOrderMark)
		{
			if (sSILBookId == null)
				throw new ArgumentNullException("sSILBookId");

			// Create a temporary file.
			string fileName = FileUtils.GetTempFile("tmp");
			if (!FileUtils.FileExists(fileName))
				s_fileOs.AddFile(fileName, string.Empty, encoding);

			using (var file = FileUtils.OpenFileForBinaryWrite(fileName, encoding))
			{
				// write the byte order marker
				if (fWriteByteOrderMark && encoding != Encoding.ASCII)
					file.Write('\ufeff');

				// Write the id line to the file, IF it's length is > 0
				if (sSILBookId.Length > 0)
					file.Write(EncodeLine(@"\id " + sSILBookId, encoding));

				// write out the file contents
				if (dataLines != null)
				{
					foreach (string sLine in dataLines)
						file.Write(EncodeLine(sLine, encoding));
				}
				file.Close();

				return fileName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a temp file that does not have an ID line.
		/// </summary>
		/// <param name="dataLines">Array of lines of SF text to write.
		/// This text should not include line-break characters.</param>
		/// <returns>Name of file that was created</returns>
		/// ------------------------------------------------------------------------------------
		public string CreateFileNoID(string[] dataLines)
		{
			// Create a temporary file.
			string fileName = FileUtils.GetTempFile("tmp");

			using (var file = FileUtils.OpenFileForBinaryWrite(fileName, Encoding.ASCII))
			{
				if (dataLines != null)
				{
					foreach (string sLine in dataLines)
						file.Write(EncodeLine(sLine, Encoding.ASCII));
				}
				file.Close();

				return fileName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a temp file that does not have an ID line, but does have a specified
		/// extension.
		/// </summary>
		/// <param name="dataLines">Array of lines of SF text to write.
		/// This text should not include line-break characters.</param>
		/// <param name="extension">The extension.</param>
		/// <returns>Name of file that was created</returns>
		/// ------------------------------------------------------------------------------------
		public string CreateFileNoID(string[] dataLines, string extension)
		{
			// Create a temporary file.
			string fileName = FileUtils.GetTempFile(extension);

			using (var file = FileUtils.OpenFileForBinaryWrite(fileName, Encoding.ASCII))
			{
				if (dataLines != null)
				{
					foreach (string sLine in dataLines)
						file.Write(EncodeLine(sLine, Encoding.ASCII));
				}
				file.Close();

				return fileName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Encodes the characters of the given string according to the specified
		/// <see cref="Encoding"/> and returns the results as a byte array, including a final
		/// CR/LF sequence so the line may be written out to a file.
		/// </summary>
		/// <param name="sLine">The Unicode string containing the characters to be encoded
		/// </param>
		/// <param name="encoding">The type of <see cref="Encoding"/> to be done</param>
		/// <returns>The encoded characters as a byte array, including a final CR/LF sequence
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static byte[] EncodeLine(string sLine, Encoding encoding)
		{
			string sIn = sLine + (char)13 + (char)10;
			byte[] bytes = new byte[sIn.Length * 2];
			int i = 0;
			foreach (char ch in sIn)
			{
				bytes[i++] = (byte)(ch & 0xff);
				bytes[i++] = (byte)(ch >> 8);
			}
			return Encoding.Convert(Encoding.Unicode, encoding, bytes);
		}
	}
}

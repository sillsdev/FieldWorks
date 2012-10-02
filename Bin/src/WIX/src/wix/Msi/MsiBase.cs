//-------------------------------------------------------------------------------------------------
// <copyright file="MsiBase.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Wrapper for some base MSI functions.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Msi
{
	using System;
	using System.Text;
	using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

	/// <summary>
	/// MsiBase contains only static functions for performing some general MSI tasts.
	/// </summary>
	public class MsiBase
	{
		/// <summary>
		/// Takes the path to a file and returns a 128-bit hash of that file.
		/// </summary>
		/// <param name="filePath">Path to file that is to be hashed.</param>
		/// <param name="options">The value in this column must be 0. This parameter is reserved for future use.</param>
		/// <param name="hash">Int array that receives the returned file hash information.</param>
		public static void GetFileHash(string filePath, int options, out int[] hash)
		{
			MsiInterop.MSIFILEHASHINFO hashInterop = new MsiInterop.MSIFILEHASHINFO();
			hashInterop.fileHashInfoSize = 20;

			uint er = MsiInterop.MsiGetFileHash(filePath, 0, ref hashInterop);
			if (110 == er)
			{
				throw new System.IO.FileNotFoundException("Failed to find file for hashing.", filePath);
			}
			else if (0 != er)
			{
				throw new ApplicationException(String.Format("Unknown error while getting hash of file: {0}, system error: {1}", filePath, er));   // TODO: come up with a real exception to throw
			}

			hash = new int[4];
			hash[0] = hashInterop.data0;
			hash[1] = hashInterop.data1;
			hash[2] = hashInterop.data2;
			hash[3] = hashInterop.data3;
		}

		/// <summary>
		/// Returns the version string and language string in the format that the installer
		/// expects to find them in the database.  If you just want version information, set
		/// lpLangBuf and pcchLangBuf to zero. If you just want language information, set
		/// lpVersionBuf and pcchVersionBuf to zero.
		/// </summary>
		/// <param name="filePath">Specifies the path to the file.</param>
		/// <param name="version">Returns the file version. Set to 0 for language information only.</param>
		/// <param name="language">Returns the file language. Set to 0 for version information only.</param>
		public static void FileVersion(string filePath, out string version, out string language)
		{
			int versionLength = 20;
			int languageLength = 20;
			StringBuilder versionBuffer = new StringBuilder(versionLength);
			StringBuilder languageBuffer = new StringBuilder(languageLength);

			uint er = MsiInterop.MsiGetFileVersion(filePath, versionBuffer, ref versionLength, languageBuffer, ref languageLength);
			if (234 == er)
			{
				versionBuffer.EnsureCapacity(++versionLength);
				languageBuffer.EnsureCapacity(++languageLength);
				er = MsiInterop.MsiGetFileVersion(filePath, versionBuffer, ref versionLength, languageBuffer, ref languageLength);
			}
			else if (1006 == er)
			{
				er = 0;   // file has no version or language, so no error
			}

			if (0 != er)
			{
				throw new System.Runtime.InteropServices.ExternalException(String.Format("Unknown error while getting version of file: {0}, system error: {1}", filePath, er));
			}

			version = versionBuffer.ToString();
			language = languageBuffer.ToString();
		}
	}
}

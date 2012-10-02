//-------------------------------------------------------------------------------------------------
// <copyright file="CabInterop.cs" company="Microsoft">
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
// Interop class for the winterop.dll.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Cab.Interop
{
	using System;
	using System.Text;
	using System.Runtime.InteropServices;

	/// <summary>
	/// Interop class for the winterop.dll.
	/// </summary>
	internal sealed class CabInterop
	{
		/// <summary>
		/// Private constructor since you can't create this object.
		/// </summary>
		private CabInterop()
		{
		}

		/// <summary>
		/// Starts creating a cabinet.
		/// </summary>
		/// <param name="cabinetName">Name of cabinet to create.</param>
		/// <param name="cabinetDirectory">Directory to create cabinet in.</param>
		/// <param name="maxSize">Maximum size of the cabinet.</param>
		/// <param name="maxThreshold">Maximum threshold in the cabinet.</param>
		/// <param name="compressionType">Type of compression to use in the cabinet.</param>
		/// <param name="contextHandle">Handle to opened cabinet.</param>
		/// <returns>Zero if successfully began cabinet.</returns>
		[DllImport("winterop.dll", EntryPoint="CreateCabBegin", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern int CreateCabBegin(
			string cabinetName,
			string cabinetDirectory,
			uint maxSize,
			uint maxThreshold,
			uint compressionType,
			out IntPtr contextHandle);

		/// <summary>
		/// Adds a file to an open cabinet.
		/// </summary>
		/// <param name="file">Full path to file to add to cabinet.</param>
		/// <param name="token">Name of file in cabinet.</param>
		/// <param name="contextHandle">Handle to open cabinet.</param>
		/// <returns>Zero if successfully added file.</returns>
		[DllImport("winterop.dll", EntryPoint="CreateCabAddFile", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern int CreateCabAddFile(
			string file,
			string token,
			IntPtr contextHandle);

		/// <summary>
		/// Adds an array of files to add to an open cabinet.
		/// </summary>
		/// <param name="files">Array of file paths to add to cabinet.</param>
		/// <param name="tokens">Array matching "files" for names used in cabinet.</param>
		/// <param name="fileCount">Number of file paths in array.</param>
		/// <param name="contextHandle">Handle to open cabinet.</param>
		/// <returns>Zero if successfully added files.</returns>
		[DllImport("winterop.dll", EntryPoint="CreateCabAddFiles", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern int CreateCabAddFiles(
			string[] files,
			string[] tokens,
			uint fileCount,
			IntPtr contextHandle);

		/// <summary>
		/// Closes a cabinet.
		/// </summary>
		/// <param name="contextHandle">Handle to open cabinet to close.</param>
		/// <returns>Zero if successfully closed cabinet.</returns>
		[DllImport("winterop.dll", EntryPoint="CreateCabFinish", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern int CreateCabFinish(
			IntPtr contextHandle);

		/// <summary>
		/// Initializes cabinet extraction.
		/// </summary>
		/// <returns>Zero if cabinets can be extraced.</returns>
		[DllImport("winterop.dll", EntryPoint="ExtractCabBegin", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern int ExtractCabBegin();

		/// <summary>
		/// Extracts files from cabinet.
		/// </summary>
		/// <param name="cabinet">Path to cabinet to extract files from.</param>
		/// <param name="extractDirectory">Directory to extract files to.</param>
		/// <returns>Zero if files were successfully extracted.</returns>
		[DllImport("winterop.dll", EntryPoint="ExtractCab", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
		internal static extern int ExtractCab(
			string cabinet,
			string extractDirectory);

		/// <summary>
		/// Cleans up after cabinet extraction.
		/// </summary>
		[DllImport("winterop.dll", EntryPoint="ExtractCabFinish", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
		internal static extern void ExtractCabFinish();

		/// <summary>
		/// Resets the DACL on an array of files to "empty".
		/// </summary>
		/// <param name="files">Array of file reset ACL to "empty".</param>
		/// <param name="fileCount">Number of file paths in array.</param>
		/// <returns>Zero if successfully reset files.</returns>
		[DllImport("winterop.dll", EntryPoint="ResetAcls", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern int ResetAcls(
			string[] files,
			uint fileCount);
	}
}

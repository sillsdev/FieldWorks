//-------------------------------------------------------------------------------------------------
// <copyright file="FileSystemInterop.cs" company="Microsoft">
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
// Interop for Win32 file system calls.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.FileSystem.Interop
{
	using System;
	using System.Text;
	using System.Runtime.InteropServices;

	/// <summary>
	/// Interop for Win32 file system calls.
	/// </summary>
	internal sealed class FileSystemInterop
	{
		internal const int MaxPath = 255;

		/// <summary>
		/// Cannot instantiate this class.
		/// </summary>
		private FileSystemInterop()
		{
		}

		/// <summary>
		/// Gets the short name for a file.
		/// </summary>
		/// <param name="longPath">Long path to convert to short path.</param>
		/// <param name="shortPath">Short path from long path.</param>
		/// <param name="buffer">Size of short path.</param>
		/// <returns>zero if success.</returns>
		[DllImport("kernel32.dll", EntryPoint="GetShortPathNameW", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
		internal static extern uint GetShortPathName(string longPath, StringBuilder shortPath, [MarshalAs(UnmanagedType.U4)]int buffer);

		/// <summary>
		/// Gets the long name for a file.
		/// </summary>
		/// <param name="shortPath">Short path to convert to short path.</param>
		/// <param name="longPath">Long path from long path.</param>
		/// <param name="buffer">Size of short path.</param>
		/// <returns>zero if success.</returns>
		[DllImport("kernel32.dll", EntryPoint="GetLongPathNameW", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
		internal static extern uint GetLongPathName(string shortPath, StringBuilder longPath, [MarshalAs(UnmanagedType.U4)]int buffer);
	}
}

// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IStorage.cs
// Responsibility: TE Team
//
// <remarks>
// The definitions for these structs and interfaces are copied from a TlbImp generated
// Interop DLL.
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace SIL.Utils
{
	/// <summary></summary>
	[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
	public struct tagRemSNB
	{
		/// <summary></summary>
		public uint ulCntStr;
		/// <summary></summary>
		public uint ulCntChar;
		/// <summary></summary>
		[ComConversionLoss]
		public IntPtr rgString;
	}

	/// <summary></summary>
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct _ULARGE_INTEGER
	{
		/// <summary></summary>
		public ulong QuadPart;
	}

	/// <summary></summary>
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct tagSTATSTG
	{
		/// <summary></summary>
		[MarshalAs(UnmanagedType.LPWStr)]
		public string pwcsName;
		/// <summary></summary>
		public uint type;
		/// <summary></summary>
		public _ULARGE_INTEGER cbSize;
		/// <summary></summary>
		public _FILETIME mtime;
		/// <summary></summary>
		public _FILETIME ctime;
		/// <summary></summary>
		public _FILETIME atime;
		/// <summary></summary>
		public uint grfMode;
		/// <summary></summary>
		public uint grfLocksSupported;
		/// <summary></summary>
		public Guid clsid;
		/// <summary></summary>
		public uint grfStateBits;
		/// <summary></summary>
		public uint reserved;
	}

	/// <summary></summary>
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct _FILETIME
	{
		/// <summary></summary>
		public uint dwLowDateTime;
		/// <summary></summary>
		public uint dwHighDateTime;
	}

	/// <summary></summary>
	[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("0000000D-0000-0000-C000-000000000046")]
	public interface IEnumSTATSTG
	{
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void RemoteNext([In] uint celt, out tagSTATSTG rgelt, out uint pceltFetched);
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Skip([In] uint celt);
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Reset();
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumSTATSTG ppenum);
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// <remarks>Copied from TLBImp generated interop dll</remarks>
	/// ----------------------------------------------------------------------------------------
	[ComImport, Guid("0000000B-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IStorage
	{
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void CreateStream([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName, [In] uint grfMode,
			[In] uint reserved1, [In] uint reserved2, [MarshalAs(UnmanagedType.Interface)] out IStream ppstm);
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
#if !__MonoCS__
		void RemoteOpenStream([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName,
			[In] uint cbReserved1, [In] ref byte reserved1, [In] uint grfMode, [In] uint reserved2,
			[MarshalAs(UnmanagedType.Interface)] out IStream ppstm);
#else
		// The RemoteOpenStream definition above (which was mapping from IStorage.OpenStream)
		// contains different number of parameters than here (we don't have cbReserved1).
		void OpenStream([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName,
			[In] ref byte reserved1, [In] uint grfMode, [In] uint 	reserved2,
			[MarshalAs(UnmanagedType.Interface)] out IStream ppstm);
#endif
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void CreateStorage([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName, [In] uint grfMode,
			[In] uint reserved1, [In] uint reserved2, [MarshalAs(UnmanagedType.Interface)] out IStorage ppstg);
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void OpenStorage([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName,
			[In, MarshalAs(UnmanagedType.Interface)] IStorage pstgPriority, [In] uint grfMode,
			[In] ref tagRemSNB snbExclude, [In] uint reserved,
			[MarshalAs(UnmanagedType.Interface)] out IStorage ppstg);
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void CopyTo([In] uint ciidExclude, [In] ref Guid rgiidExclude,
			[In] ref tagRemSNB snbExclude,
			[In, MarshalAs(UnmanagedType.Interface)] IStorage pstgDest);
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void MoveElementTo([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName,
			[In, MarshalAs(UnmanagedType.Interface)] IStorage pstgDest,
			[In, MarshalAs(UnmanagedType.LPWStr)] string pwcsNewName, [In] uint grfFlags);
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Commit([In] uint grfCommitFlags);
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Revert();
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
#if !__MonoCS__
		void RemoteEnumElements([In] uint reserved1, [In] uint cbReserved2, [In] ref byte reserved2,
			[In] uint reserved3, [MarshalAs(UnmanagedType.Interface)] out IEnumSTATSTG ppenum);
#else
		void EnumElements([In] uint reserved1, [In] ref byte reserved2,
			[In] uint reserved3, [MarshalAs(UnmanagedType.Interface)] out IEnumSTATSTG ppenum);
#endif
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void DestroyElement([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName);
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void RenameElement([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsOldName,
			[In, MarshalAs(UnmanagedType.LPWStr)] string pwcsNewName);
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetElementTimes([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName,
			[In] ref _FILETIME pctime, [In] ref _FILETIME patime, [In] ref _FILETIME pmtime);
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetClass([In] ref Guid clsid);
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetStateBits([In] uint grfStateBits, [In] uint grfMask);
		/// <summary></summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Stat(out tagSTATSTG pstatstg, [In] uint grfStatFlag);
	}
}

// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IPicture.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SIL.Utils.ComTypes
{
	/// <summary>
	/// IPicture interface (as found in stdole)
	/// </summary>
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("7BF80980-BF32-101A-8BBB-00AA00300CAB")]
	public interface IPicture
	{
		/// <summary/>
		int Handle
		{ [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

		/// <summary/>
		int hPal
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			get;
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			set;
		}

		/// <summary/>
		short Type
		{ [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

		/// <summary/>
		int Width
		{ [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

		/// <summary/>
		int Height
		{ [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

		/// <summary/>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Render(IntPtr hdc, int x, int y, int cx, int cy, int xSrc, int ySrc, int cxSrc, int cySrc,
			IntPtr prcWBounds);

		/// <summary/>
		int CurDC
		{ [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

		/// <summary/>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SelectPicture(int hdcIn, out int phdcOut, out int phbmpOut);

		/// <summary/>
		bool KeepOriginalFormat
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			get;
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			set;
		}

		/// <summary/>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void PictureChanged();

		/// <summary/>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SaveAsFile(IntPtr pstm, bool fSaveMemCopy, out int pcbSize);

		/// <summary/>
		int Attributes
		{ [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

		/// <summary/>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SetHdc(int hdc);
	}

	/// <summary>
	/// IPictureDisp interface (as found in stdole)
	/// </summary>
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
	[Guid("7BF80981-BF32-101A-8BBB-00AA00300CAB")]
	public interface IPictureDisp
	{
		/// <summary/>
		int Handle
		{ [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

		/// <summary/>
		int hPal
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			get;
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			set;
		}

		/// <summary/>
		short Type
		{ [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

		/// <summary/>
		int Width
		{ [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

		/// <summary/>
		int Height
		{ [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

		/// <summary/>
		[PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Render(int hdc, int x, int y, int cx, int cy, int xSrc, int ySrc,
			int cxSrc, int cySrc, IntPtr prcWBounds);
	}
}

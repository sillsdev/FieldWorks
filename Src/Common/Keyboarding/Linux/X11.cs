// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
#if __MonoCS__
using System;
using System.Runtime.InteropServices;

namespace X11
{
	/// <summary>
	/// Declarations of unmanaged X11 functions
	/// </summary>
	public static class Unmanaged
	{
		/// <summary/>
		[DllImport("libX11", EntryPoint="XOpenDisplay")]
		public extern static IntPtr XOpenDisplay(IntPtr display);
		/// <summary/>
		[DllImport("libX11", EntryPoint="XCloseDisplay")]
		public extern static int XCloseDisplay(IntPtr display);
	}
}
#endif

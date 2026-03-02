// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Windows USER/GDI handle diagnostics for the current process.
	/// </summary>
	public static class HwndDiagnostics
	{
		private const uint GR_GDIOBJECTS = 0;
		private const uint GR_USEROBJECTS = 1;

		/// <summary>
		/// Gets USER handle count for the current process.
		/// </summary>
		public static int GetCurrentProcessUserHandleCount()
		{
			using (var process = Process.GetCurrentProcess())
			{
				return unchecked((int)GetGuiResources(process.Handle, GR_USEROBJECTS));
			}
		}

		/// <summary>
		/// Gets GDI handle count for the current process.
		/// </summary>
		public static int GetCurrentProcessGdiHandleCount()
		{
			using (var process = Process.GetCurrentProcess())
			{
				return unchecked((int)GetGuiResources(process.Handle, GR_GDIOBJECTS));
			}
		}

		[DllImport("user32.dll", SetLastError = true)]
		private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);
	}
}
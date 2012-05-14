#region Copyright (c) 2003-2005, Luke T. Maxon

/********************************************************************************************************************
'
' Copyright (c) 2003-2005, Luke T. Maxon
' All rights reserved.
'
' Redistribution and use in source and binary forms, with or without modification, are permitted provided
' that the following conditions are met:
'
' * Redistributions of source code must retain the above copyright notice, this list of conditions and the
' 	following disclaimer.
'
' * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and
' 	the following disclaimer in the documentation and/or other materials provided with the distribution.
'
' * Neither the name of the author nor the names of its contributors may be used to endorse or
' 	promote products derived from this software without specific prior written permission.
'
' THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
' WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
' PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
' ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
' LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
' INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
' OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN
' IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
'
'*******************************************************************************************************************/

#endregion

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NUnit.Extensions.Forms
{
	public class Win32
	{
		internal delegate IntPtr CBTCallback(int code, IntPtr wParam, IntPtr lParam);

		internal delegate int WindowEnumProc(IntPtr hwnd, IntPtr lparam);

		[DllImport("user32.dll")]
		internal static extern IntPtr GetDesktopWindow();

		[DllImport("user32.dll")]
		internal static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc func, IntPtr lParam);

		[DllImport("user32.dll")]
		internal static extern int GetClassName(IntPtr handleToWindow, StringBuilder className, int maxClassNameLength);

		[DllImport("user32.dll")]
		internal static extern IntPtr SetWindowsHookEx(int code, CBTCallback callbackFunction, IntPtr handleToInstance,
			int threadID);

		[DllImport("user32.dll")]
		internal static extern bool UnhookWindowsHookEx(IntPtr handleToHook);

		[DllImport("user32.dll")]
		internal static extern IntPtr CallNextHookEx(IntPtr handleToHook, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32", SetLastError = true)]
		public static extern int GetCurrentThreadId();
	}
}
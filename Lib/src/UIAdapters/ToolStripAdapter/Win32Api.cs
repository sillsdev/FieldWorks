// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Win32Api.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SIL.FieldWorks.Common.UIAdapters
{
	#region LOGFONT
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Defines a class for holding info about a logical font.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct LOGFONT
	{
		/// <summary></summary>
		public int lfHeight;
		/// <summary></summary>
		public int lfWidth;
		/// <summary></summary>
		public int lfEscapement;
		/// <summary></summary>
		public int lfOrientation;
		/// <summary></summary>
		public int lfWeight;
		/// <summary></summary>
		public byte lfItalic;
		/// <summary></summary>
		public byte lfUnderline;
		/// <summary></summary>
		public byte lfStrikeOut;
		/// <summary></summary>
		public byte lfCharSet;
		/// <summary></summary>
		public byte lfOutPrecision;
		/// <summary></summary>
		public byte lfClipPrecision;
		/// <summary></summary>
		public byte lfQuality;
		/// <summary></summary>
		public byte lfPitchAndFamily;
		/// <summary></summary>
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string lfFaceName;
	}
	#endregion

	#region NONCLIENTMETRICS
	/// <summary></summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct NONCLIENTMETRICS
	{
		/// <summary></summary>
		public int cbSize;
		/// <summary></summary>
		public int iBorderWidth;
		/// <summary></summary>
		public int iScrollWidth;
		/// <summary></summary>
		public int iScrollHeight;
		/// <summary></summary>
		public int iCaptionWidth;
		/// <summary></summary>
		public int iCaptionHeight;
		/// <summary></summary>
		public LOGFONT lfCaptionFont;
		/// <summary></summary>
		public int iSmCaptionWidth;
		/// <summary></summary>
		public int iSmCaptionHeight;
		/// <summary></summary>
		public LOGFONT lfSmCaptionFont;
		/// <summary></summary>
		public int iMenuWidth;
		/// <summary></summary>
		public int iMenuHeight;
		/// <summary></summary>
		public LOGFONT lfMenuFont;
		/// <summary></summary>
		public LOGFONT lfStatusFont;
		/// <summary></summary>
		public LOGFONT lfMessageFont;
	}
	#endregion

	#region Static class Win32
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Wrappers for Win32 methods
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class Win32Api
	{
		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool SystemParametersInfo(int action, int intParam,
			ref NONCLIENTMETRICS metrics, int update);

		private const int SPI_GETNONCLIENTMETRICS = 41; // const value from Win32API.Text (= 0x0029)

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the non client metrics.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static NONCLIENTMETRICS NonClientMetrics
		{
			get
			{
				NONCLIENTMETRICS metrics = new NONCLIENTMETRICS();
				int size = metrics.cbSize = Marshal.SizeOf(typeof(NONCLIENTMETRICS));
				bool result = SystemParametersInfo(SPI_GETNONCLIENTMETRICS, size, ref metrics, 0);
				Debug.Assert(result);
				return metrics;
			}
		}
	}
	#endregion
}

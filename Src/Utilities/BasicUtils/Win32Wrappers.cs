// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2002' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Win32.cs
//
// <remarks>
// Declaration of wrappers for Win32 methods
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms.VisualStyles;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;

namespace SIL.Utils
{
	internal delegate int BrowseCallBackProc(IntPtr hwnd, int msg, IntPtr lp, IntPtr wp);

	#region Struct Rect
	/// <summary>
	/// Redefine Rect structure.
	/// </summary>
	/// <remarks>We can't simply use Rectangle, because the struct layout
	/// is different (Rect uses left, top, right, bottom, whereas Rectangle uses x, y,
	/// width, height).</remarks>
	public struct Rect
	{
		/// <summary>Specifies the x-coordiante of the upper-left corner of the rectangle</summary>
		public int left;
		/// <summary>Specifies the y-coordiante of the upper-left corner of the rectangle</summary>
		public int top;
		/// <summary>Specifies the x-coordiante of the lower-right corner of the rectangle</summary>
		public int right;
		/// <summary>Specifies the y-coordiante of the lower-right corner of the rectangle</summary>
		public int bottom;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a rectangle with the specified coordinates
		/// </summary>
		/// <param name="l">left</param>
		/// <param name="t">top</param>
		/// <param name="r">right</param>
		/// <param name="b">bottom</param>
		/// ------------------------------------------------------------------------------------
		public Rect(int l, int t, int r, int b)
		{
			left = l; top = t; right = r; bottom = b;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts a Rect struct to a .NET Rectangle
		/// </summary>
		/// <param name="rc">Windows rectangle</param>
		/// <returns>.NET rectangle</returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator Rectangle(Rect rc)
		{
			return Rectangle.FromLTRB(rc.left, rc.top, rc.right, rc.bottom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts a .NET rectangle to a windows rectangle
		/// </summary>
		/// <param name="rc">.NET rectangle</param>
		/// <returns>Windows rectangle</returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator Rect(Rectangle rc)
		{
			return new Rect(rc.Left, rc.Top, rc.Right, rc.Bottom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test whether the rectangle contains the specified point.
		/// </summary>
		/// <param name="pt"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool Contains(Point pt)
		{
			if (pt.X < left)
				return false;
			if (pt.X > right)
				return false;
			if (pt.Y < top)
				return false;
			if (pt.Y > bottom)
				return false;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with this instance.</param>
		/// <returns><c>true</c> if the specified <see cref="T:System.Object"/> is equal to this
		/// instance; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			return (obj is Rect && (Rect)obj == this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data
		/// structures like a hash table.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return top ^ (bottom >> 4) ^ (left >> 8) ^ (right >> 12);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="rc1">The first rectangle.</param>
		/// <param name="rc2">The second rectangle.</param>
		/// <returns>The result of the operator.</returns>
		/// ------------------------------------------------------------------------------------
		public static bool operator == (Rect rc1, Rect rc2)
		{
			return (rc1.top == rc2.top && rc1.bottom == rc2.bottom && rc1.left == rc2.left &&
				rc1.right == rc2.right);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="rc1">The first rectangle.</param>
		/// <param name="rc2">The second rectangle.</param>
		/// <returns>The result of the operator.</returns>
		/// ------------------------------------------------------------------------------------
		public static bool operator != (Rect rc1, Rect rc2)
		{
			return !(rc1 == rc2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return "left=" + left + ", top=" + top +
				", right=" + right + ", bottom=" + bottom;
		}
	}
	#endregion

	#region Static class Win32
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Wrappers for Win32 methods
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class Win32
	{
		/// <summary></summary>
		public const string TOOLBARCLASSNAME = "ToolbarWindow32";
		/// <summary></summary>
		public const string REBARCLASSNAME = "ReBarWindow32";

		/// --------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------
		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			/// <summary></summary>
			public int x;
			/// <summary></summary>
			public int y;
		}

		#region NONCLIENTMETRICS struct
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

		#region LOGFONT struct
		/// <summary></summary>
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
			public string lfFaceSize;
		}

		#endregion

		#region Mouse Buttons
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Defines mouse buttons
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Flags]
		public enum MouseButtons
		{
			/// <summary>Left mouse button</summary>
			MK_LBUTTON = 0x0001,
			/// <summary>Right mouse button</summary>
			MK_RBUTTON = 0x0002,
			/// <summary>Shift key</summary>
			MK_SHIFT = 0x0004,
			/// <summary>Control key</summary>
			MK_CONTROL = 0x0008,
			/// <summary>Middle mouse button</summary>
			MK_MBUTTON = 0x0010,
			/// <summary>First XButton on MS IntelliMouse Explorer</summary>
			MK_XBUTTON1 = 0x0020,
			/// <summary>Second XButton on MS IntelliMouse Explorer</summary>
			MK_XBUTTON2 = 0x0040
		}
		#endregion

		#region MSG struct
		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct MSG
		{
			/// <summary></summary>
			public IntPtr hwnd;
			/// <summary></summary>
			public int message;
			/// <summary></summary>
			public IntPtr wParam;
			/// <summary></summary>
			public IntPtr lParam;
			/// <summary></summary>
			public int time;
			/// <summary></summary>
			public int pt_x;
			/// <summary></summary>
			public int pt_y;
		}
		#endregion

		#region Win32 Windows Message Enumeration
		/// <summary></summary>
		public enum WinMsgs
		{
			/// <summary></summary>
			WM_NULL = 0x0000,
			/// <summary></summary>
			WM_CREATE = 0x0001,
			/// <summary></summary>
			WM_DESTROY = 0x0002,
			/// <summary></summary>
			WM_MOVE = 0x0003,
			/// <summary></summary>
			WM_SIZE = 0x0005,
			/// <summary></summary>
			WM_ACTIVATE = 0x0006,
			/// <summary></summary>
			WM_SETFOCUS = 0x0007,
			/// <summary></summary>
			WM_KILLFOCUS = 0x0008,
			/// <summary></summary>
			WM_ENABLE = 0x000A,
			/// <summary></summary>
			WM_SETREDRAW = 0x000B,
			/// <summary></summary>
			WM_SETTEXT = 0x000C,
			/// <summary></summary>
			WM_GETTEXT = 0x000D,
			/// <summary></summary>
			WM_GETTEXTLENGTH = 0x000E,
			/// <summary></summary>
			WM_PAINT = 0x000F,
			/// <summary></summary>
			WM_CLOSE = 0x0010,
			/// <summary></summary>
			WM_QUERYENDSESSION = 0x0011,
			/// <summary></summary>
			WM_QUIT = 0x0012,
			/// <summary></summary>
			WM_QUERYOPEN = 0x0013,
			/// <summary></summary>
			WM_ERASEBKGND = 0x0014,
			/// <summary></summary>
			WM_SYSCOLORCHANGE = 0x0015,
			/// <summary></summary>
			WM_ENDSESSION = 0x0016,
			/// <summary></summary>
			WM_SHOWWINDOW = 0x0018,
			/// <summary></summary>
			WM_CTLCOLOR = 0x0019,
			/// <summary></summary>
			WM_WININICHANGE = 0x001A,
			/// <summary></summary>
			WM_SETTINGCHANGE = 0x001A,
			/// <summary></summary>
			WM_DEVMODECHANGE = 0x001B,
			/// <summary></summary>
			WM_ACTIVATEAPP = 0x001C,
			/// <summary></summary>
			WM_FONTCHANGE = 0x001D,
			/// <summary></summary>
			WM_TIMECHANGE = 0x001E,
			/// <summary></summary>
			WM_CANCELMODE = 0x001F,
			/// <summary></summary>
			WM_SETCURSOR = 0x0020,
			/// <summary></summary>
			WM_MOUSEACTIVATE = 0x0021,
			/// <summary></summary>
			WM_CHILDACTIVATE = 0x0022,
			/// <summary></summary>
			WM_QUEUESYNC = 0x0023,
			/// <summary></summary>
			WM_GETMINMAXINFO = 0x0024,
			/// <summary></summary>
			WM_PAINTICON = 0x0026,
			/// <summary></summary>
			WM_ICONERASEBKGND = 0x0027,
			/// <summary></summary>
			WM_NEXTDLGCTL = 0x0028,
			/// <summary></summary>
			WM_SPOOLERSTATUS = 0x002A,
			/// <summary></summary>
			WM_DRAWITEM = 0x002B,
			/// <summary></summary>
			WM_MEASUREITEM = 0x002C,
			/// <summary></summary>
			WM_DELETEITEM = 0x002D,
			/// <summary></summary>
			WM_VKEYTOITEM = 0x002E,
			/// <summary></summary>
			WM_CHARTOITEM = 0x002F,
			/// <summary></summary>
			WM_SETFONT = 0x0030,
			/// <summary></summary>
			WM_GETFONT = 0x0031,
			/// <summary></summary>
			WM_SETHOTKEY = 0x0032,
			/// <summary></summary>
			WM_GETHOTKEY = 0x0033,
			/// <summary></summary>
			WM_QUERYDRAGICON = 0x0037,
			/// <summary></summary>
			WM_COMPAREITEM = 0x0039,
			/// <summary></summary>
			WM_GETOBJECT = 0x003D,
			/// <summary></summary>
			WM_COMPACTING = 0x0041,
			/// <summary></summary>
			WM_COMMNOTIFY = 0x0044,
			/// <summary></summary>
			WM_WINDOWPOSCHANGING = 0x0046,
			/// <summary></summary>
			WM_WINDOWPOSCHANGED = 0x0047,
			/// <summary></summary>
			WM_POWER = 0x0048,
			/// <summary></summary>
			WM_COPYDATA = 0x004A,
			/// <summary></summary>
			WM_CANCELJOURNAL = 0x004B,
			/// <summary></summary>
			WM_NOTIFY = 0x004E,
			/// <summary></summary>
			WM_INPUTLANGCHANGEREQUEST = 0x0050,
			/// <summary></summary>
			WM_INPUTLANGCHANGE = 0x0051,
			/// <summary></summary>
			WM_TCARD = 0x0052,
			/// <summary></summary>
			WM_HELP = 0x0053,
			/// <summary></summary>
			WM_USERCHANGED = 0x0054,
			/// <summary></summary>
			WM_NOTIFYFORMAT = 0x0055,
			/// <summary></summary>
			WM_CONTEXTMENU = 0x007B,
			/// <summary></summary>
			WM_STYLECHANGING = 0x007C,
			/// <summary></summary>
			WM_STYLECHANGED = 0x007D,
			/// <summary></summary>
			WM_DISPLAYCHANGE = 0x007E,
			/// <summary></summary>
			WM_GETICON = 0x007F,
			/// <summary></summary>
			WM_SETICON = 0x0080,
			/// <summary></summary>
			WM_NCCREATE = 0x0081,
			/// <summary></summary>
			WM_NCDESTROY = 0x0082,
			/// <summary></summary>
			WM_NCCALCSIZE = 0x0083,
			/// <summary></summary>
			WM_NCHITTEST = 0x0084,
			/// <summary></summary>
			WM_NCPAINT = 0x0085,
			/// <summary></summary>
			WM_NCACTIVATE = 0x0086,
			/// <summary></summary>
			WM_GETDLGCODE = 0x0087,
			/// <summary></summary>
			WM_SYNCPAINT = 0x0088,
			/// <summary></summary>
			WM_NCMOUSEMOVE = 0x00A0,
			/// <summary></summary>
			WM_NCLBUTTONDOWN = 0x00A1,
			/// <summary></summary>
			WM_NCLBUTTONUP = 0x00A2,
			/// <summary></summary>
			WM_NCLBUTTONDBLCLK = 0x00A3,
			/// <summary></summary>
			WM_NCRBUTTONDOWN = 0x00A4,
			/// <summary></summary>
			WM_NCRBUTTONUP = 0x00A5,
			/// <summary></summary>
			WM_NCRBUTTONDBLCLK = 0x00A6,
			/// <summary></summary>
			WM_NCMBUTTONDOWN = 0x00A7,
			/// <summary></summary>
			WM_NCMBUTTONUP = 0x00A8,
			/// <summary></summary>
			WM_NCMBUTTONDBLCLK = 0x00A9,
			/// <summary>
			/// Minimum message value for messages relating to the keyboard.
			/// </summary>
			WM_KEY_Min = 0x0100,
			/// <summary></summary>
			WM_KEYDOWN = 0x0100,
			/// <summary></summary>
			WM_KEYUP = 0x0101,
			/// <summary></summary>
			WM_CHAR = 0x0102,
			/// <summary></summary>
			WM_DEADCHAR = 0x0103,
			/// <summary></summary>
			WM_SYSKEYDOWN = 0x0104,
			/// <summary></summary>
			WM_SYSKEYUP = 0x0105,
			/// <summary></summary>
			WM_SYSCHAR = 0x0106,
			/// <summary></summary>
			WM_SYSDEADCHAR = 0x0107,
			/// <summary></summary>
			WM_KEYLAST = 0x0108,
			/// <summary>
			/// Max message value for messages relating to the keyboard.
			/// </summary>
			WM_KEY_Max = 0x0108,
			/// <summary></summary>
			WM_IME_STARTCOMPOSITION = 0x010D,
			/// <summary></summary>
			WM_IME_ENDCOMPOSITION = 0x010E,
			/// <summary></summary>
			WM_IME_COMPOSITION = 0x010F,
			/// <summary></summary>
			WM_IME_KEYLAST = 0x010F,
			/// <summary></summary>
			WM_INITDIALOG = 0x0110,
			/// <summary></summary>
			WM_COMMAND = 0x0111,
			/// <summary></summary>
			WM_SYSCOMMAND = 0x0112,
			/// <summary></summary>
			WM_TIMER = 0x0113,
			/// <summary></summary>
			WM_HSCROLL = 0x0114,
			/// <summary></summary>
			WM_VSCROLL = 0x0115,
			/// <summary></summary>
			WM_INITMENU = 0x0116,
			/// <summary></summary>
			WM_INITMENUPOPUP = 0x0117,
			/// <summary></summary>
			WM_MENUSELECT = 0x011F,
			/// <summary></summary>
			WM_MENUCHAR = 0x0120,
			/// <summary></summary>
			WM_ENTERIDLE = 0x0121,
			/// <summary></summary>
			WM_MENURBUTTONUP = 0x0122,
			/// <summary></summary>
			WM_MENUDRAG = 0x0123,
			/// <summary></summary>
			WM_MENUGETOBJECT = 0x0124,
			/// <summary></summary>
			WM_UNINITMENUPOPUP = 0x0125,
			/// <summary></summary>
			WM_MENUCOMMAND = 0x0126,
			/// <summary></summary>
			WM_CTLCOLORMSGBOX = 0x0132,
			/// <summary></summary>
			WM_CTLCOLOREDIT = 0x0133,
			/// <summary></summary>
			WM_CTLCOLORLISTBOX = 0x0134,
			/// <summary></summary>
			WM_CTLCOLORBTN = 0x0135,
			/// <summary></summary>
			WM_CTLCOLORDLG = 0x0136,
			/// <summary></summary>
			WM_CTLCOLORSCROLLBAR = 0x0137,
			/// <summary></summary>
			WM_CTLCOLORSTATIC = 0x0138,
			/// <summary>
			/// Minimum message value for messages relating to the mouse.
			/// </summary>
			WM_MOUSE_Min = 0x0200,
			/// <summary></summary>
			WM_MOUSEMOVE = 0x0200,
			/// <summary></summary>
			WM_LBUTTONDOWN = 0x0201,
			/// <summary></summary>
			WM_LBUTTONUP = 0x0202,
			/// <summary></summary>
			WM_LBUTTONDBLCLK = 0x0203,
			/// <summary></summary>
			WM_RBUTTONDOWN = 0x0204,
			/// <summary></summary>
			WM_RBUTTONUP = 0x0205,
			/// <summary></summary>
			WM_RBUTTONDBLCLK = 0x0206,
			/// <summary></summary>
			WM_MBUTTONDOWN = 0x0207,
			/// <summary></summary>
			WM_MBUTTONUP = 0x0208,
			/// <summary></summary>
			WM_MBUTTONDBLCLK = 0x0209,
			/// <summary></summary>
			WM_MOUSEWHEEL = 0x020A,
			/// <summary>
			/// Minimum message value for messages relating to the mouse.
			/// </summary>
			WM_MOUSE_Max = 0x020A,

			/// <summary></summary>
			WM_PARENTNOTIFY = 0x0210,
			/// <summary></summary>
			WM_ENTERMENULOOP = 0x0211,
			/// <summary></summary>
			WM_EXITMENULOOP = 0x0212,
			/// <summary></summary>
			WM_NEXTMENU = 0x0213,
			/// <summary></summary>
			WM_SIZING = 0x0214,
			/// <summary></summary>
			WM_CAPTURECHANGED = 0x0215,
			/// <summary></summary>
			WM_MOVING = 0x0216,
			/// <summary></summary>
			WM_DEVICECHANGE = 0x0219,
			/// <summary></summary>
			WM_MDICREATE = 0x0220,
			/// <summary></summary>
			WM_MDIDESTROY = 0x0221,
			/// <summary></summary>
			WM_MDIACTIVATE = 0x0222,
			/// <summary></summary>
			WM_MDIRESTORE = 0x0223,
			/// <summary></summary>
			WM_MDINEXT = 0x0224,
			/// <summary></summary>
			WM_MDIMAXIMIZE = 0x0225,
			/// <summary></summary>
			WM_MDITILE = 0x0226,
			/// <summary></summary>
			WM_MDICASCADE = 0x0227,
			/// <summary></summary>
			WM_MDIICONARRANGE = 0x0228,
			/// <summary></summary>
			WM_MDIGETACTIVE = 0x0229,
			/// <summary></summary>
			WM_MDISETMENU = 0x0230,
			/// <summary></summary>
			WM_ENTERSIZEMOVE = 0x0231,
			/// <summary></summary>
			WM_EXITSIZEMOVE = 0x0232,
			/// <summary></summary>
			WM_DROPFILES = 0x0233,
			/// <summary></summary>
			WM_MDIREFRESHMENU = 0x0234,
			/// <summary></summary>
			WM_IME_SETCONTEXT = 0x0281,
			/// <summary></summary>
			WM_IME_NOTIFY = 0x0282,
			/// <summary></summary>
			WM_IME_CONTROL = 0x0283,
			/// <summary></summary>
			WM_IME_COMPOSITIONFULL = 0x0284,
			/// <summary></summary>
			WM_IME_SELECT = 0x0285,
			/// <summary></summary>
			WM_IME_CHAR = 0x0286,
			/// <summary></summary>
			WM_IME_REQUEST = 0x0288,
			/// <summary></summary>
			WM_IME_KEYDOWN = 0x0290,
			/// <summary></summary>
			WM_IME_KEYUP = 0x0291,
			/// <summary></summary>
			WM_MOUSEHOVER = 0x02A1,
			/// <summary></summary>
			WM_MOUSELEAVE = 0x02A3,
			/// <summary></summary>
			WM_CUT = 0x0300,
			/// <summary></summary>
			WM_COPY = 0x0301,
			/// <summary></summary>
			WM_PASTE = 0x0302,
			/// <summary></summary>
			WM_CLEAR = 0x0303,
			/// <summary></summary>
			WM_UNDO = 0x0304,
			/// <summary></summary>
			WM_RENDERFORMAT = 0x0305,
			/// <summary></summary>
			WM_RENDERALLFORMATS = 0x0306,
			/// <summary></summary>
			WM_DESTROYCLIPBOARD = 0x0307,
			/// <summary></summary>
			WM_DRAWCLIPBOARD = 0x0308,
			/// <summary></summary>
			WM_PAINTCLIPBOARD = 0x0309,
			/// <summary></summary>
			WM_VSCROLLCLIPBOARD = 0x030A,
			/// <summary></summary>
			WM_SIZECLIPBOARD = 0x030B,
			/// <summary></summary>
			WM_ASKCBFORMATNAME = 0x030C,
			/// <summary></summary>
			WM_CHANGECBCHAIN = 0x030D,
			/// <summary></summary>
			WM_HSCROLLCLIPBOARD = 0x030E,
			/// <summary></summary>
			WM_QUERYNEWPALETTE = 0x030F,
			/// <summary></summary>
			WM_PALETTEISCHANGING = 0x0310,
			/// <summary></summary>
			WM_PALETTECHANGED = 0x0311,
			/// <summary></summary>
			WM_HOTKEY = 0x0312,
			/// <summary></summary>
			WM_PRINT = 0x0317,
			/// <summary></summary>
			WM_PRINTCLIENT = 0x0318,
			/// <summary></summary>
			WM_HANDHELDFIRST = 0x0358,
			/// <summary></summary>
			WM_HANDHELDLAST = 0x035F,
			/// <summary></summary>
			WM_AFXFIRST = 0x0360,
			/// <summary></summary>
			WM_AFXLAST = 0x037F,
			/// <summary></summary>
			WM_PENWINFIRST = 0x0380,
			/// <summary></summary>
			WM_PENWINLAST = 0x038F,
			/// <summary></summary>
			WM_APP = 0x8000,
			/// <summary></summary>
			WM_USER = 0x0400,
			/// <summary></summary>
			WM_REFLECT = WM_USER + 0x1c00
		}

		/// <summary></summary>
		public enum WindowStyles : uint
		{
			/// <summary></summary>
			WS_OVERLAPPED = 0x00000000,
			/// <summary></summary>
			WS_POPUP = 0x80000000,
			/// <summary></summary>
			WS_CHILD = 0x40000000,
			/// <summary></summary>
			WS_MINIMIZE = 0x20000000,
			/// <summary></summary>
			WS_VISIBLE = 0x10000000,
			/// <summary></summary>
			WS_DISABLED = 0x08000000,
			/// <summary></summary>
			WS_CLIPSIBLINGS = 0x04000000,
			/// <summary></summary>
			WS_CLIPCHILDREN = 0x02000000,
			/// <summary></summary>
			WS_MAXIMIZE = 0x01000000,
			/// <summary></summary>
			WS_CAPTION = 0x00C00000,
			/// <summary></summary>
			WS_BORDER = 0x00800000,
			/// <summary></summary>
			WS_DLGFRAME = 0x00400000,
			/// <summary></summary>
			WS_VSCROLL = 0x00200000,
			/// <summary></summary>
			WS_HSCROLL = 0x00100000,
			/// <summary></summary>
			WS_SYSMENU = 0x00080000,
			/// <summary></summary>
			WS_THICKFRAME = 0x00040000,
			/// <summary></summary>
			WS_GROUP = 0x00020000,
			/// <summary></summary>
			WS_TABSTOP = 0x00010000,
			/// <summary></summary>
			WS_MINIMIZEBOX = 0x00020000,
			/// <summary></summary>
			WS_MAXIMIZEBOX = 0x00010000,
			/// <summary></summary>
			WS_TILED = 0x00000000,
			/// <summary></summary>
			WS_ICONIC = 0x20000000,
			/// <summary></summary>
			WS_SIZEBOX = 0x00040000,
			/// <summary></summary>
			WS_POPUPWINDOW = 0x80880000,
			/// <summary></summary>
			WS_OVERLAPPEDWINDOW = 0x00CF0000,
			/// <summary></summary>
			WS_TILEDWINDOW = 0x00CF0000,
			/// <summary></summary>
			WS_CHILDWINDOW = 0x40000000
		}

		/// <summary></summary>
		public enum WindowsHookCodes
		{
			/// <summary></summary>
			WH_MSGFILTER = (-1),
			/// <summary></summary>
			WH_JOURNALRECORD = 0,
			/// <summary></summary>
			WH_JOURNALPLAYBACK = 1,
			/// <summary></summary>
			WH_KEYBOARD = 2,
			/// <summary></summary>
			WH_GETMESSAGE = 3,
			/// <summary></summary>
			WH_CALLWNDPROC = 4,
			/// <summary></summary>
			WH_CBT = 5,
			/// <summary></summary>
			WH_SYSMSGFILTER = 6,
			/// <summary></summary>
			WH_MOUSE = 7,
			/// <summary></summary>
			WH_HARDWARE = 8,
			/// <summary></summary>
			WH_DEBUG = 9,
			/// <summary></summary>
			WH_SHELL = 10,
			/// <summary></summary>
			WH_FOREGROUNDIDLE = 11,
			/// <summary></summary>
			WH_CALLWNDPROCRET = 12,
			/// <summary></summary>
			WH_KEYBOARD_LL = 13,
			/// <summary></summary>
			WH_MOUSE_LL = 14
		}

		/// <summary></summary>
		public enum MenuCharReturnValues
		{
			/// <summary></summary>
			MNC_IGNORE = 0,
			/// <summary></summary>
			MNC_CLOSE = 1,
			/// <summary></summary>
			MNC_EXECUTE = 2,
			/// <summary></summary>
			MNC_SELECT = 3
		}

		/// <summary>
		/// Virtual key codes for WM_KEYDOWN messages
		/// </summary>
		public enum VirtualKeycodes
		{
			/// <summary></summary>
			VK_TAB = 0x09,
			/// <summary></summary>
			VK_CLEAR = 0x0C,
			/// <summary></summary>
			VK_RETURN = 0x0D,
			/// <summary></summary>
			VK_SHIFT = 0x10,
			/// <summary></summary>
			VK_CONTROL = 0x11,
			/// <summary></summary>
			VK_MENU = 0x12,
			/// <summary></summary>
			VK_PAUSE = 0x13,
			/// <summary></summary>
			VK_CAPITAL = 0x14,
			/// <summary></summary>
			VK_ESCAPE = 0x1B,
			/// <summary></summary>
			VK_SPACE = 0x20,
			/// <summary>PGUP</summary>
			VK_PRIOR = 0x21,
			/// <summary>PGDN</summary>
			VK_NEXT = 0x22,
			/// <summary></summary>
			VK_END = 0x23,
			/// <summary></summary>
			VK_HOME = 0x24,
			/// <summary></summary>
			VK_LEFT = 0x25,
			/// <summary></summary>
			VK_UP = 0x26,
			/// <summary></summary>
			VK_RIGHT = 0x27,
			/// <summary></summary>
			VK_DOWN = 0x28,
			/// <summary></summary>
			VK_SELECT = 0x29,
			/// <summary></summary>
			VK_PRINT = 0x2A,
			/// <summary></summary>
			VK_EXECUTE = 0x2B,
			/// <summary></summary>
			VK_SNAPSHOT = 0x2C,
			/// <summary></summary>
			VK_INSERT = 0x2D,
			/// <summary></summary>
			VK_DELETE = 0x2E,
			/// <summary></summary>
			VK_HELP = 0x2F
		}

		/// <summary>
		/// flags for the PeekMessage function
		/// </summary>
		public enum PeekFlags : int
		{
			/// <summary>Do not remove the message from the queue</summary>
			PM_NOREMOVE = 0,
			/// <summary>Remove the message from the queue</summary>
			PM_REMOVE = 1,
			/// <summary>Do not yield while peeking the message</summary>
			PM_NOYIELD = 2
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct TRACKMOUSEEVENTS
		{
			/// <summary></summary>
			public uint cbSize;
			/// <summary></summary>
			public uint dwFlags;
			/// <summary></summary>
			public IntPtr hWnd;
			/// <summary></summary>
			public uint dwHoverTime;
		}

		/// <summary></summary>
		public enum TrackerEventFlags : uint
		{
			/// <summary></summary>
			TME_HOVER = 0x00000001,
			/// <summary></summary>
			TME_LEAVE = 0x00000002,
			/// <summary></summary>
			TME_QUERY = 0x40000000,
			/// <summary></summary>
			TME_CANCEL = 0x80000000
		}

		/// <summary></summary>
		public enum MouseHookFilters
		{
			/// <summary></summary>
			MSGF_DIALOGBOX = 0,
			/// <summary></summary>
			MSGF_MESSAGEBOX = 1,
			/// <summary></summary>
			MSGF_MENU = 2,
			/// <summary></summary>
			MSGF_SCROLLBAR = 5,
			/// <summary></summary>
			MSGF_NEXTWINDOW = 6
		}

		#endregion

		#region Win32 Menu Flags Enumeration
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Menu flags for Add/Check/EnableMenuItem()
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Flags]
		public enum MenuFlags
		{
			/// <summary></summary>
			MF_INSERT = 0x00000000,
			/// <summary></summary>
			MF_CHANGE = 0x00000080,
			/// <summary></summary>
			MF_APPEND = 0x00000100,
			/// <summary></summary>
			MF_DELETE = 0x00000200,
			/// <summary></summary>
			MF_REMOVE = 0x00001000,

			/// <summary></summary>
			MF_BYCOMMAND = 0x00000000,
			/// <summary></summary>
			MF_BYPOSITION = 0x00000400,

			/// <summary></summary>
			MF_SEPARATOR = 0x00000800,

			/// <summary></summary>
			MF_ENABLED = 0x00000000,
			/// <summary></summary>
			MF_GRAYED = 0x00000001,
			/// <summary></summary>
			MF_DISABLED = 0x00000002,

			/// <summary></summary>
			MF_UNCHECKED = 0x00000000,
			/// <summary></summary>
			MF_CHECKED = 0x00000008,
			/// <summary></summary>
			MF_USECHECKBITMAPS = 0x00000200,

			/// <summary></summary>
			MF_STRING = 0x00000000,
			/// <summary></summary>
			MF_BITMAP = 0x00000004,
			/// <summary></summary>
			MF_OWNERDRAW = 0x00000100,

			/// <summary></summary>
			MF_POPUP = 0x00000010,
			/// <summary></summary>
			MF_MENUBARBREAK = 0x00000020,
			/// <summary></summary>
			MF_MENUBREAK = 0x00000040,

			/// <summary></summary>
			MF_UNHILITE = 0x00000000,
			/// <summary></summary>
			MF_HILITE = 0x00000080,

			/// <summary></summary>
			MF_DEFAULT = 0x00001000,
			/// <summary></summary>
			MF_SYSMENU = 0x00002000,
			/// <summary></summary>
			MF_HELP = 0x00004000,
			/// <summary></summary>
			MF_RIGHTJUSTIFY = 0x00004000
		}
		#endregion

		#region Win32 Hit test enumeration
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// WM_NCHITTEST and MOUSEHOOKSTRUCT Mouse Position Codes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum HitTest
		{
			/// <summary></summary>
			HTERROR = (-2),
			/// <summary></summary>
			HTTRANSPARENT = (-1),
			/// <summary></summary>
			HTNOWHERE = 0,
			/// <summary></summary>
			HTCLIENT = 1,
			/// <summary></summary>
			HTCAPTION = 2,
			/// <summary></summary>
			HTSYSMENU = 3,
			/// <summary></summary>
			HTGROWBOX = 4,
			/// <summary></summary>
			HTSIZE = HTGROWBOX,
			/// <summary></summary>
			HTMENU = 5,
			/// <summary></summary>
			HTHSCROLL = 6,
			/// <summary></summary>
			HTVSCROLL = 7,
			/// <summary></summary>
			HTMINBUTTON = 8,
			/// <summary></summary>
			HTMAXBUTTON = 9,
			/// <summary></summary>
			HTLEFT = 10,
			/// <summary></summary>
			HTRIGHT = 11,
			/// <summary></summary>
			HTTOP = 12,
			/// <summary></summary>
			HTTOPLEFT = 13,
			/// <summary></summary>
			HTTOPRIGHT = 14,
			/// <summary></summary>
			HTBOTTOM = 15,
			/// <summary></summary>
			HTBOTTOMLEFT = 16,
			/// <summary></summary>
			HTBOTTOMRIGHT = 17,
			/// <summary></summary>
			HTBORDER = 18,
			/// <summary></summary>
			HTREDUCE = HTMINBUTTON,
			/// <summary></summary>
			HTZOOM = HTMAXBUTTON,
			/// <summary></summary>
			HTSIZEFIRST = HTLEFT,
			/// <summary></summary>
			HTSIZELAST = HTBOTTOMRIGHT,
			/// <summary></summary>
			HTOBJECT = 19,
			/// <summary></summary>
			HTCLOSE = 20,
			/// <summary></summary>
			HTHELP = 21,
		}
		#endregion

		#region User32.dll

		#region Keyboard
		/// <summary>
		/// Options for <see cref="ActivateKeyboardLayout"/>.
		/// </summary>
		[Flags]
		public enum KLF : int
		{
			/// <summary>Pass no options to <see cref="ActivateKeyboardLayout"/></summary>
			None = 0x00000000,
			/// <summary>Undocumented</summary>
			Activate = 0x00000001,
			/// <summary>Undocumented</summary>
			Substitute_Ok = 0x00000002,
			/// <summary><para>If this bit is set, the system's circular list of loaded locale
			/// identifiers is reordered by moving the locale identifier to the head of the
			/// list. If this bit is not set, the list is rotated without a change of order.
			/// </para>
			/// <para>For example, if a user had an English locale identifier active, as well as having
			/// French, German, and Spanish locale identifiers loaded (in that order), then activating
			/// the German locale identifier with the <c>Reorder</c> bit set would produce the
			/// following order: German, English, French, Spanish. Activating the German locale
			/// identifier without the <c>Reorder</c> bit set would produce the following order: German,
			/// Spanish, English, French.</para>
			/// <para>If less than three locale identifiers are loaded, the value of this flag is
			/// irrelevant.</para>
			/// </summary>
			Reorder = 0x00000008,
			/// <summary>Undocumented</summary>
			ReplaceLang = 0x00000010,
			/// <summary>Undocumented</summary>
			NoTellShell = 0x00000080,
			/// <summary>Activates the specified locale identifier for the entire process and sends the
			/// <b>WM_INPUTLANGCHANGE</b> message to the current thread's Focus or Active window.
			/// </summary>
			SetForProcess = 0x00000100,
			/// <summary>This is used with <c>Reset</c>. See <c>Reset</c> for an
			/// explanation.</summary>
			ShiftLock = 0x00010000,
			/// <summary>If set but <c>ShiftLock</c> is not set, the Caps Lock state is turned
			/// off by pressing the Caps Lock key again. If set and <c>ShiftLock</c> is also set,
			/// the Caps Lock state is turned off by pressing either SHIFT key.</summary>
			Reset = 0x40000000
		}
		#endregion

#if __MonoCS__
// TODO-Linux: ensure all methods in this file have XML comments.
#pragma warning disable 1591 // missing XML comment
#endif
		/// <summary></summary>
#if !__MonoCS__
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool SystemParametersInfo(int uiAction, int uiParam,
			ref NONCLIENTMETRICS ncMetrics, int fWinIni);
#else
		// TODO-Linux: Implement if needed
		public static bool SystemParametersInfo(int uiAction, int uiParam,
			ref NONCLIENTMETRICS ncMetrics, int fWinIni)
		{
			return true;
		}
#endif

		/// <summary>
		/// The <c>ActivateKeyboardLayout</c> function sets the input locale identifier
		/// (formerly called the keyboard layout handle) for the calling thread or the current
		/// process. The input locale identifier specifies a locale as well as the physical
		/// layout of the keyboard.
		/// </summary>
		/// <param name="hkl">Input locale identifier to be activated.</param>
		/// <param name="uFlags">Input locale identifier options.</param>
		/// <returns>The return value is an input locale identifier. If the function succeeds,
		/// the return value is the previous input locale identifier. Otherwise, it is zero.
		/// </returns>
#if !__MonoCS__
		[DllImport("user32.dll")]
		extern static public IntPtr ActivateKeyboardLayout(IntPtr hkl, KLF uFlags);
#else
		// TODO-Linux: Implement if needed
		static public IntPtr ActivateKeyboardLayout(IntPtr hkl, KLF uFlags)
		{
			Console.WriteLine("Warning using unimplemented method ActivateKeyboardLayout");
			return IntPtr.Zero;
		}
#endif

		/// <summary></summary>
#if !__MonoCS__
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption,
			uint uType);
#else
		public static int MessageBox(IntPtr hWnd, string lpText, string lpCaption,
			uint uType)
		{
			// TODO-Linux: Do this properly take account of uType. (yes/no dialog ect.)
			System.Windows.Forms.Control temp = System.Windows.Forms.Panel.FromHandle(hWnd);
			System.Windows.Forms.MessageBox.Show(temp, lpText, lpCaption);
			return 0;
		}
#endif

		/// <summary></summary>
#if !__MonoCS__
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool TrackMouseEvent(ref TRACKMOUSEEVENTS tme);
#else
		// TODO-Linux: Implement if needed
		public static bool TrackMouseEvent(ref TRACKMOUSEEVENTS tme)
		{
			Console.WriteLine("Warning using unimplemented method TrackMouseEvent");
			return false;
		}
#endif

		/// <summary></summary>
#if !__MonoCS__
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool GetMenuItemRect(IntPtr hWnd, IntPtr hMenu, uint item, ref RECT rc);
#else
		// TODO-Linux: Implement if needed
		public static bool GetMenuItemRect(IntPtr hWnd, IntPtr hMenu, uint item, ref RECT rc)
		{
			Console.WriteLine("Warning using unimplemented method GetMenuItemRect");
			return false;
		}
#endif

		/// <summary>The all-encompassing SendMessage function.</summary>
#if !__MonoCS__
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool SendMessage(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp);
#else
		// TODO-Linux: Implement if needed
		public static bool SendMessage(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp)
		{
			Console.WriteLine("Warning using unimplemented method SendMessage");
			return false;
		}
#endif

		/// <summary></summary>
#if !__MonoCS__
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
#else
		// TODO-Linux: Implement if needed
		public static int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam)
		{
			Console.WriteLine("Warning using unimplemented method SendMessage");
			return 0;
		}
#endif

		/// <summary></summary>
#if !__MonoCS__
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, WinMsgs msg, int wParam, int lParam);
#else
		// TODO-Linux: Implement if needed
		public static int SendMessage(IntPtr hWnd, WinMsgs msg, int wParam, int lParam)
		{
			Console.WriteLine("Warning using unimplemented method SendMessage");
			return 0;
		}
#endif

		/// <summary></summary>
#if !__MonoCS__
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);
#else
		// TODO-Linux: Implement if needed
		public static IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam)
		{
			Console.WriteLine("Warning using unimplemented method SendMessage");
			return IntPtr.Zero;
		}
#endif

#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern void SendMessage(IntPtr hWnd, int msg, int wParam, ref RECT lParam);
#else
		// TODO-Linux: Implement if needed
		public static void SendMessage(IntPtr hWnd, int msg, int wParam, ref RECT lParam)
		{
			Console.WriteLine("Warning using unimplemented method SendMessage");
		}
#endif

#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, ref POINT lParam);
#else
		// TODO-Linux: Implement if needed
		public static int SendMessage(IntPtr hWnd, int msg, int wParam, ref POINT lParam)
		{
			Console.WriteLine("Warning using unimplemented method SendMessage");
			return 0;
		}
#endif

#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern void SendMessage(IntPtr hWnd, LVMsgs msg, int wParam, ref LVITEM lParam);
#else
		// TODO-Linux: Implement if needed
		public static void SendMessage(IntPtr hWnd, LVMsgs msg, int wParam, ref LVITEM lParam)
		{
			Console.WriteLine("Warning using unimplemented method SendMessage");
		}
#endif

#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, HdrCtrlMsgs msg, int wParam, int lParam);
#else
		// TODO-Linux: Implement if needed
		public static int SendMessage(IntPtr hWnd, HdrCtrlMsgs msg, int wParam, int lParam)
		{
			Console.WriteLine("Warning using unimplemented method SendMessage");
			return 0;
		}
#endif

#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, ref TBBUTTON lParam);
#else
		// TODO-Linux: Implement if needed
		public static int SendMessage(IntPtr hWnd, int msg, int wParam, ref TBBUTTON lParam)
		{
			Console.WriteLine("Warning using unimplemented method SendMessage");
			return 0;
		}
#endif

#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, ref TBBUTTONINFO lParam);
#else
		// TODO-Linux: Implement if needed
		public static int SendMessage(IntPtr hWnd, int msg, int wParam, ref TBBUTTONINFO lParam)
		{
			Console.WriteLine("Warning using unimplemented method SendMessage");
			return 0;
		}
#endif

#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, ref REBARBANDINFO lParam);
#else
		// TODO-Linux: Implement if needed
		public static int SendMessage(IntPtr hWnd, int msg, int wParam, ref REBARBANDINFO lParam)
		{
			Console.WriteLine("Warning using unimplemented method SendMessage");
			return 0;
		}
#endif

#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, ref RBHITTESTINFO lParam);
#else
		// TODO-Linux: Implement if needed
		public static int SendMessage(IntPtr hWnd, int msg, int wParam, ref RBHITTESTINFO lParam)
		{
			Console.WriteLine("Warning using unimplemented method SendMessage");
			return 0;
		}
#endif

#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool PeekMessage(ref MSG msg, IntPtr hWnd, uint wFilterMin, uint wFilterMax, uint wFlag);
#else
		// TODO-Linux: Implement if needed
		public static bool PeekMessage(ref MSG msg, IntPtr hWnd, uint wFilterMin, uint wFilterMax, uint wFlag)
		{
			Console.WriteLine("Warning using unimplemented method PeekMessage");
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool TranslateMessage(ref MSG msg);
#else
		// TODO-Linux: Implement if needed
		public static bool TranslateMessage(ref MSG msg)
		{
			Console.WriteLine("Warning using unimplemented method TranslateMessage");
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool DispatchMessage(ref MSG msg);
#else
		// TODO-Linux: Implement if needed
		public static bool DispatchMessage(ref MSG msg)
		{
			Console.WriteLine("Warning using unimplemented method DispatchMessage");
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool IsDialogMessage(IntPtr hWnd, ref MSG msg);
#else
		// TODO-Linux: Implement if needed
		public static bool IsDialogMessage(IntPtr hWnd, ref MSG msg)
		{
			Console.WriteLine("Warning using unimplemented method IsDialogMessage");
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool PostMessage(IntPtr hWnd, int Msg, uint wParam, uint lParam);
#else
		// TODO-Linux: Implement if needed
		public static bool PostMessage(IntPtr hWnd, int Msg, uint wParam, uint lParam)
		{
			Console.WriteLine("Warning using unimplemented method PostMessage");
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool PostThreadMessage(int idThread, int Msg, uint wParam, uint lParam);
#else
		// TODO-Linux: Implement if needed
		public static bool PostThreadMessage(int idThread, int Msg, uint wParam, uint lParam)
		{
			Console.WriteLine("Warning using unimplemented method PostThreadMessage");
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool PostMessage(IntPtr hWnd, WinMsgs Msg, int wParam, int lParam);
#else
		// TODO-Linux: Implement if needed
		public static bool PostMessage(IntPtr hWnd, WinMsgs Msg, int wParam, int lParam)
		{
			Console.WriteLine("Warning using unimplemented method PostMessage");
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public extern static IntPtr GetDlgItem(IntPtr hDlg, int nControlID);
#else
		// TODO-Linux: Implement if needed
		public static IntPtr GetDlgItem(IntPtr hDlg, int nControlID)
		{
			Console.WriteLine("Warning using unimplemented method GetDlgItem");
			return IntPtr.Zero;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
		public static extern IntPtr GetParent(IntPtr hWnd);
#else
		public static IntPtr GetParent(IntPtr hWnd)
		{
			System.Windows.Forms.Control temp = System.Windows.Forms.Panel.FromHandle(hWnd);
			if (temp != null && temp.Parent != null)
				return temp.Parent.Handle;

			return IntPtr.Zero;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll")]
		public extern static bool SetForegroundWindow(IntPtr hWnd);
#else
		public static bool SetForegroundWindow(IntPtr hWnd)
		{
			System.Windows.Forms.Control temp = System.Windows.Forms.Panel.FromHandle(hWnd);
			if (temp != null)
			{
				temp.BringToFront();
				temp.Focus();
			}

			return false;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll")]
		public extern static bool SetForegroundWindow(int hWnd);
#else
		// TODO-Linux: Implement if needed
		public static bool SetForegroundWindow(int hWnd)
		{
			Console.WriteLine("Warning using unimplemented method SetForegroundWindow");
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll")]
		public extern static bool GetWindowRect(IntPtr hWnd, out Rect rect);
#else
		public static bool GetWindowRect(IntPtr hWnd, out Rect rect)
		{
			System.Windows.Forms.Control temp = System.Windows.Forms.Panel.FromHandle(hWnd);
			if (temp != null)
			{
				rect = new Rect(temp.Location.X, temp.Location.Y, temp.Location.X + temp.Size.Width, temp.Location.Y + temp.Size.Height);
				return true;
			}
			rect = new Rect(0,0,0,0);
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr GetFocus();
#else
		private static Assembly monoWinFormsAssembly;
		private static Type xplatUIX11; // internal mono WinForms type
		private static FieldInfo focusWindow; // internal mono WinForms static instance that traces focus

		public static IntPtr GetFocus()
		{
			if (monoWinFormsAssembly == null)
			{
#pragma warning disable 0612 // Using Obsolete method LoadWithPartialName.
				monoWinFormsAssembly = Assembly.LoadWithPartialName("System.Windows.Forms");
#pragma warning restore 0612
				xplatUIX11 = monoWinFormsAssembly.GetType("System.Windows.Forms.XplatUIX11");
				focusWindow = xplatUIX11.GetField("FocusWindow", System.Reflection.BindingFlags.NonPublic |
System.Reflection.BindingFlags.Static );
			}

			// Get static field to determine Focused Window.
			return (IntPtr)focusWindow.GetValue(null);
		}
#endif

		/// <summary></summary>
		public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SetWindowsHookEx(int hookid, HookProc pfnhook, IntPtr hinst, int threadid);
#else
		// TODO-Linux: Implement if needed
		public static IntPtr SetWindowsHookEx(int hookid, HookProc pfnhook, IntPtr hinst, int threadid)
		{
			Console.WriteLine("Warning using unimplemented method SetWindowsHookEx");
			return IntPtr.Zero;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);
#else
		// TODO-Linux: Implement if needed
		public static bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl)
		{
			Console.WriteLine("Warning using unimplemented method GetWindowPlacement");
			lpwndpl = new WINDOWPLACEMENT();
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lParam);
#else
		// TODO-Linux: Implement if needed
		public static bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lParam)
		{
			Console.WriteLine("Warning using unimplemented method SetWindowPlacement");
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern bool UnhookWindowsHookEx(IntPtr hhook);
#else
		// TODO-Linux: Implement if needed
		public static bool UnhookWindowsHookEx(IntPtr hhook)
		{
			Console.WriteLine("Warning using unimplemented method UnhookWindowsHookEx");
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern IntPtr CallNextHookEx(IntPtr hhook, int code, IntPtr wparam, IntPtr lparam);
#else
		// TODO-Linux: Implement if needed
		public static IntPtr CallNextHookEx(IntPtr hhook, int code, IntPtr wparam, IntPtr lparam)
		{
			Console.WriteLine("Warning using unimplemented method CallNextHookEx");
			return IntPtr.Zero;
		}
#endif
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public extern static IntPtr SetParent(IntPtr hChild, IntPtr hParent);
#else
		// TODO-Linux: Implement if needed
		public static IntPtr SetParent(IntPtr hChild, IntPtr hParent)
		{
			Console.WriteLine("Warning using unimplemented method SetParent");
			return IntPtr.Zero;
		}
#endif
#if !__MonoCS__
		/// <summary>The MenuItemFromPoint function determines which menu item, if any, is at the
		/// specified location.</summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public extern static int MenuItemFromPoint(IntPtr hWnd, IntPtr hMenu, Point ptScreen);
#else
		// TODO-Linux: Implement if needed
		public static int MenuItemFromPoint(IntPtr hWnd, IntPtr hMenu, Point ptScreen)
		{
			Console.WriteLine("Warning using unimplemented method MenuItemFromPoint");
			return 0;
		}
#endif
#if !__MonoCS__
		/// <summary>The EndMenu function ends the calling thread's active menu.</summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public extern static bool EndMenu();
#else
		// TODO-Linux: Implement if needed
		public static bool EndMenu()
		{
			Console.WriteLine("Warning using unimplemented method EndMenu");
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary> go from client to screen</summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public extern static bool ClientToScreen(IntPtr hWnd, ref POINT ptScreen);
#else
		// TODO-Linux: Implement if neededs
		public static bool ClientToScreen(IntPtr hWnd, ref POINT ptScreen)
		{
			Console.WriteLine("Warning using unimplemented method ClientToScreen");
			return false;
		}
#endif
#if !__MonoCS__
		/// <summary> go from screen to client</summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public extern static bool ScreenToClient(IntPtr hWnd, ref POINT ptScreen);
#else
		// TODO-Linux: Implement if needed
		public static bool ScreenToClient(IntPtr hWnd, ref POINT ptScreen)
		{
			Console.WriteLine("Warning using unimplemented method ScreenToClient");
			return false;
		}
#endif

		/// <summary>The RegisterWindowMessage function defines a new window message that is
		/// guaranteed to be unique throughout the system. The message value can be used when
		/// sending or posting messages.</summary>
		/// <param name="name">unique name of a message</param>
		/// <returns>message identifier in the range 0xC000 through 0xFFFF, or 0 if an error
		/// occurs</returns>
#if !__MonoCS__
		[DllImport("user32.dll")]
		extern static public uint RegisterWindowMessage(string name);
#else
		// TODO-Linux: Implement if needed
		static public uint RegisterWindowMessage(string name)
		{
			Console.WriteLine("Warning using unimplemented method RegisterWindowMessage");
			return 0;
		}
#endif

		/// <summary></summary>
		public const int GWL_STYLE = -16;

		/// <summary></summary>
		public const int TVS_NOTOOLTIPS = 0x80;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The GetWindowLongPtr function retrieves information about the specified window.
		/// </summary>
		/// <param name="hWnd">The window handle.</param>
		/// <param name="nIndex">Specifies the zero-based offset to the value to be retrieved.
		/// </param>
		/// <returns>If the function succeeds, the return value is the requested value. If the
		/// function fails, the return value is zero.</returns>
		/// ------------------------------------------------------------------------------------
#if !__MonoCS__
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int GetWindowLong(HandleRef hWnd, int nIndex);
#else
		// TODO-Linux: Implement if needed
		public static int GetWindowLong(HandleRef hWnd, int nIndex)
		{
			Console.WriteLine("Warning using unimplemented method GetWindowLong");
			return 0;
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The SetWindowLongPtr function changes an attribute of the specified window.
		/// </summary>
		/// <param name="hWnd">The window handle.</param>
		/// <param name="nIndex">Specifies the zero-based offset to the value to be set.</param>
		/// <param name="dwNewLong">Specifies the replacement value. </param>
		/// <returns>If the function succeeds, the return value is the previous value of the
		/// specified offset. If the function fails, the return value is zero. </returns>
		/// ------------------------------------------------------------------------------------
#if !__MonoCS__
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SetWindowLong(HandleRef hWnd, int nIndex, int dwNewLong);
#else
		// TODO-Linux: Implement if needed
		public static int SetWindowLong(HandleRef hWnd, int nIndex, int dwNewLong)
		{
			Console.WriteLine("Warning using unimplemented method SetWindowLong");
			return 0;
		}
#endif

		#region MessageBeep
		/// <summary>
		/// Possible arguments to MessageBeep.
		/// </summary>
		public enum BeepType
		{
			/// <summary> </summary>
			SimpleBeep = -1,
			/// <summary> </summary>
			IconAsterisk = 0x00000040,
			/// <summary> </summary>
			IconExclamation = 0x00000030,
			/// <summary> </summary>
			IconHand = 0x00000010,
			/// <summary> </summary>
			IconQuestion = 0x00000020,
			/// <summary> </summary>
			Ok = 0x00000000,
		}
		/// <summary>
		/// Make a noise!
		/// </summary>
		/// <param name="beepType"></param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		public static extern bool MessageBeep(BeepType beepType);
		#endregion

		#region Scrolling
		/// <summary>Identifies a scroll bar.</summary>
		public enum WhichScrollBar : short
		{
			/// <summary>Sets the position of the scroll box in a window's standard horizontal
			/// scroll bar.</summary>
			SB_HORZ = 0,
			/// <summary>Sets the position of the scroll box in a window's standard vertical
			/// scroll bar.</summary>
			SB_VERT = 1,
			/// <summary>Sets the position of the scroll box in a scroll bar control. The
			/// hwnd parameter must be the handle to the scroll bar control.</summary>
			SB_CTL = 2,
			/// <summary></summary>
			SB_BOTH = 3
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The SetScrollPos function sets the position of the scroll box (thumb) in the
		/// specified scroll bar and, if requested, redraws the scroll bar to reflect the
		/// new position of the scroll box.
		/// </summary>
		/// <param name="hWnd">Handle to a scroll bar control or a window with a standard
		/// scroll bar, depending on the value of the nBar parameter. </param>
		/// <param name="nBar">Specifies the scroll bar to be set. </param>
		/// <param name="nPos">Specifies the new position of the scroll box. The position must
		/// be within the scrolling range. </param>
		/// <param name="fRedraw">Specifies whether the scroll bar is redrawn to reflect the
		/// new scroll box position. If this parameter is <c>true</c>, the scroll bar is
		/// redrawn. If it is <c>false</c>, the scroll bar is not redrawn. </param>
		/// <returns>If the function succeeds, the return value is the previous position of
		/// the scroll box. If the function fails, the return value is 0.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public extern static short SetScrollPos(IntPtr hWnd, WhichScrollBar nBar, short nPos, bool fRedraw);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The GetScrollPos function retrieves the current position of the scroll box (thumb)
		/// in the specified scroll bar. The current position is a relative value that depends
		/// on the current scrolling range. For example, if the scrolling range is 0 through
		/// 100 and the scroll box is in the middle of the bar, the current position is 50.
		/// </summary>
		/// <param name="hWnd">Handle to a scroll bar control or a window with a standard
		/// scroll bar, depending on the value of the nBar parameter.</param>
		/// <param name="nBar">Specifies the scroll bar to be examined. </param>
		/// <returns>If the function succeeds, the return value is the current position of
		/// the scroll box. If the function fails, the return value is 0. </returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public extern static short GetScrollPos(IntPtr hWnd, WhichScrollBar nBar);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The SetScrollRange function sets the minimum and maximum scroll box positions for
		/// the specified scroll bar.
		/// </summary>
		/// <param name="hWnd">Handle to a scroll bar control or a window with a standard scroll
		/// bar, depending on the value of the nBar parameter.</param>
		/// <param name="nBar">Specifies the scroll bar to be set.</param>
		/// <param name="nMinPos">Specifies the minimum scrolling position.</param>
		/// <param name="nMaxPos">Specifies the maximum scrolling position.</param>
		/// <param name="fRedraw">Specifies whether the scroll bar is redrawn to reflect the
		/// new scroll box position. If this parameter is <c>true</c>, the scroll bar is
		/// redrawn. If it is <c>false</c>, the scroll bar is not redrawn. </param>
		/// <returns>If the function succeeds, the return value is <c>true</c>. If the function
		/// fails, the return value is <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public extern static bool SetScrollRange(IntPtr hWnd, WhichScrollBar nBar, short nMinPos,
			short nMaxPos, bool fRedraw);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The GetScrollRange function retrieves the current minimum and maximum scroll box
		/// (thumb) positions for the specified scroll bar.
		/// </summary>
		/// <param name="hWnd">Handle to a scroll bar control or a window with a standard scroll
		/// bar, depending on the value of the nBar parameter.</param>
		/// <param name="nBar">Specifies the scroll bar from which the positions are
		/// retrieved.</param>
		/// <param name="nMinPos">Receives the minimum scrolling position.</param>
		/// <param name="nMaxPos">Receives the maximum scrolling position.</param>
		/// <returns>If the function succeeds, the return value is <c>true</c>. If the function
		/// fails, the return value is <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public extern static bool GetScrollRange(IntPtr hWnd, WhichScrollBar nBar,
			out short nMinPos, out short nMaxPos);
		#endregion // Scrolling

		#endregion

		#region Kernel32.dll
		/// <summary>
		/// The <c>MemoryStatus</c> structure contains information about the current
		/// state of both physical and virtual memory.
		/// </summary>
		public struct MemoryStatus
		{
			/// <summary>
			/// Size of the <c>MemoryStatus</c> data structure, in bytes. You do not
			/// need to set this member before calling the <see cref="GlobalMemoryStatus"/>
			/// function; the function sets it.
			/// </summary>
			public uint dwLength;
			/// <summary>See MSDN documentation</summary>
			public uint dwMemoryLoad;
			/// <summary>Total size of physical memory, in bytes.</summary>
			public uint dwTotalPhys;
			/// <summary>Size of physical memory available, in bytes. </summary>
			public uint dwAvailPhys;
			/// <summary>Size of the committed memory limit, in bytes. </summary>
			public uint dwTotalPageFile;
			/// <summary>Size of available memory to commit, in bytes.</summary>
			public uint dwAvailPageFile;
			/// <summary>Total size of the user mode portion of the virtual address space of
			/// the calling process, in bytes.</summary>
			public uint dwTotalVirtual;
			/// <summary>Size of unreserved and uncommitted memory in the user mode portion
			/// of the virtual address space of the calling process, in bytes.</summary>
			public uint dwAvailVirtual;
		};

		/// <summary>
		/// The <c>GlobalMemoryStatus</c> function obtains information about the system's
		/// current usage of both physical and virtual memory.
		/// </summary>
		/// <param name="ms">Pointer to a <see cref="MemoryStatus"/>  structure. The
		/// <c>GlobalMemoryStatus</c> function stores information about current memory
		/// availability into this structure.</param>
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		extern public static void GlobalMemoryStatus(ref MemoryStatus ms);

		/// <summary>
		/// The <c>GetDiskFreeSpace</c> function retrieves information about the specified
		/// disk, including the amount of free space on the disk.
		/// </summary>
		/// <param name="rootPathName">[in] Pointer to a null-terminated string that specifies
		/// the root directory of the disk to return information about. See MSDN for more
		/// information</param>
		/// <param name="sectorsPerCluster">[out] Pointer to a variable for the number of
		/// sectors per cluster.</param>
		/// <param name="bytesPerSector">[out] Pointer to a variable for the number of bytes
		/// per sector.</param>
		/// <param name="numberOfFreeClusters">[out] Pointer to a variable for the total
		/// number of free clusters on the disk that are available to the user associated with
		/// the calling thread. </param>
		/// <param name="totalNumberOfClusters">[out] Pointer to a variable for the total
		/// number of clusters on the disk that are available to the user associated with the
		/// calling thread. </param>
		/// <returns><para>If the function succeeds, the return value is <b>true</b>.</para>
		/// <para>If the function fails, the return value is <b>false</b>. </para></returns>
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		extern public static bool
			GetDiskFreeSpace(string rootPathName, ref uint sectorsPerCluster, ref uint bytesPerSector,
			ref uint numberOfFreeClusters, ref uint totalNumberOfClusters);

		/// <summary></summary>
#if !__MonoCS__
		[DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
		public static extern int GetCurrentThreadId();
#else
		public static int GetCurrentThreadId()
		{
			return System.Threading.Thread.CurrentThread.ManagedThreadId;
		}
#endif
		/// <summary></summary>
#if !__MonoCS__
		[DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
		public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int procID);
#else
		public static int GetWindowThreadProcessId(IntPtr hwnd, out int procID)
		{
			procID = System.Diagnostics.Process.GetCurrentProcess().Id;
			return System.Threading.Thread.CurrentThread.ManagedThreadId;
		}

		[DllImport ("libc")]
		private static extern int readlink(string path, byte[] buffer, int buflen);
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the executable
		/// </summary>
		/// ------------------------------------------------------------------------------------
#if !__MonoCS__
		[DllImport("kernel32.dll", SetLastError = true)]
		[PreserveSig]
		public static extern uint GetModuleFileName(IntPtr hModule, [Out]StringBuilder lpFilename,
			[MarshalAs(UnmanagedType.U4)]int nSize);
#else
		public static uint GetModuleFileName(IntPtr hModule, StringBuilder lpFilename,
			int nSize)
		{
			if (hModule != IntPtr.Zero)
				return 0; // not supported (yet)

			byte[] buf = new byte[nSize];
			int ret = readlink("/proc/self/exe", buf, buf.Length);
			if (ret == -1)
				return 0;
			char[] cbuf = new char[nSize];
			int nChars = Encoding.Default.GetChars(buf, 0, ret, cbuf, 0);
			lpFilename.Append(new String(cbuf, 0, nChars));
			return (uint)nChars;
		}
#endif

		#region Synchronization
		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public struct SecurityAttributes
		{
			/// <summary>length of the structure</summary>
			public UInt32 length;
			/// <summary>security descriptor struct - define this if needed</summary>
			public IntPtr securityDescriptor;
			/// <summary>true to allow the handle to be inherited</summary>
			public bool inheritHandle;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public const UInt32 WAIT_TIMEOUT = 258;
		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public const UInt32 WAIT_OBJECT_0 = 0;

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------

#if !__MonoCS__
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		extern public static IntPtr CreateSemaphore(ref SecurityAttributes securityAttributes,
			int initialCount, int maximumCount, string name);
#else
		// TODO-Linux: Implement if needed
		public static IntPtr CreateSemaphore(ref SecurityAttributes securityAttributes,
			int initialCount, int maximumCount, string name)
		{
			Console.WriteLine("Warning using unimplemented method CreateSemaphore");
			return IntPtr.Zero;
		}
#endif
		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
#if !__MonoCS__
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		extern public static bool ReleaseSemaphore(IntPtr semaphore, int releaseCount,
			out int previousCount);
#else
		// TODO-Linux: Implement if needed
		public static bool ReleaseSemaphore(IntPtr semaphore, int releaseCount,
			out int previousCount)
		{
			Console.WriteLine("Warning using unimplemented method ReleaseSemaphore");
			previousCount = 0;
			return false;
		}
#endif
		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
#if !__MonoCS__
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		extern public static UInt32 WaitForSingleObject(IntPtr handle, UInt32 milliseconds);
#else
		// TODO-Linux: Implement if needed
		public static UInt32 WaitForSingleObject(IntPtr handle, UInt32 milliseconds)
		{
			Console.WriteLine("Warning using unimplemented method WaitForSingleObject");
			return 0;
		}
#endif

#if __MonoCS__
#pragma warning restore 1591 // missing XML comment
#endif
		#endregion


		#endregion

		#region Comctl32.dll
		/// <summary></summary>
		[DllImport("comctl32.dll")]
		public static extern bool InitCommonControlsEx(INITCOMMONCONTROLSEX icc);
		/// <summary></summary>
		[DllImport("comctl32.dll")]
		public static extern bool InitCommonControls();

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct NMREBARCHEVRON
		{
			/// <summary></summary>
			public NMHDR hdr;
			/// <summary></summary>
			public int uBand;
			/// <summary></summary>
			public int wID;
			/// <summary></summary>
			public int lParam;
			/// <summary></summary>
			public RECT rc;
			/// <summary></summary>
			public int lParamNM;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct REBARBANDINFO
		{
			/// <summary></summary>
			public int cbSize;
			/// <summary></summary>
			public int fMask;
			/// <summary></summary>
			public int fStyle;
			/// <summary></summary>
			public int clrFore;
			/// <summary></summary>
			public int clrBack;
			/// <summary></summary>
			public IntPtr lpText;
			/// <summary></summary>
			public int cch;
			/// <summary></summary>
			public int iImage;
			/// <summary></summary>
			public IntPtr hwndChild;
			/// <summary></summary>
			public int cxMinChild;
			/// <summary></summary>
			public int cyMinChild;
			/// <summary></summary>
			public int cx;
			/// <summary></summary>
			public IntPtr hbmBack;
			/// <summary></summary>
			public int wID;
			/// <summary></summary>
			public int cyChild;
			/// <summary></summary>
			public int cyMaxChild;
			/// <summary></summary>
			public int cyIntegral;
			/// <summary></summary>
			public int cxIdeal;
			/// <summary></summary>
			public int lParam;
			/// <summary></summary>
			public int cxHeader;
		}


		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct RBHITTESTINFO
		{
			/// <summary></summary>
			public POINT pt;
			/// <summary></summary>
			public uint flags;
			/// <summary></summary>
			public int iBand;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct WINDOWPLACEMENT
		{
			/// <summary>Window Placement flags</summary>
			public enum WindowPlacementFlags
			{
				/// <summary>Specifies that the coordinates of the minimized window may be specified.
				/// This flag must be specified if the coordinates are set in the ptMinPosition member.</summary>
				WPF_SETMINPOSITION = 0x0001,
				/// <summary>Specifies that the restored window will be maximized, regardless of whether it was maximized
				/// before it was minimized. This setting is only valid the next time the window is restored. It does not
				/// change the default restoration behavior. This flag is only valid when the SW_SHOWMINIMIZED value is
				/// specified for the showCmd member.</summary>
				WPF_RESTORETOMAXIMIZED = 0x0002,
				/// <summary>Windows 2000/XP: If the calling thread and the thread that owns the window are attached to
				/// different input queues, the system posts the request to the thread that owns the window. This prevents
				/// the calling thread from blocking its execution while other threads process the request.</summary>
				WPF_ASYNCWINDOWPLACEMENT = 0x0004,
			}

			/// <summary>Enumeration of commands that specify the current show state of the window.</summary>
			public enum ShowWindowCommands
			{
				/// <summary>Hides the window and activates another window.</summary>
				SW_HIDE = 0,

				/// <summary>Activates and displays the window. If the window is minimized or maximized, the system restores
				/// it to its original size and position. An application should specify this flag when restoring a minimized window.</summary>
				SW_SHOWNORMAL = 1,

				/// <summary>Activates and displays the window. If the window is minimized or maximized, the system restores
				/// it to its original size and position. An application should specify this flag when restoring a minimized window.</summary>
				SW_NORMAL = 1,

				/// <summary>Activates the window and displays it as a minimized window.</summary>
				SW_SHOWMINIMIZED = 2,

				/// <summary>Activates the window and displays it as a maximized window.</summary>
				SW_SHOWMAXIMIZED = 3,

				/// <summary>Maximizes the specified window (actually synonymous with SW_SHOWMAXIMIZED).</summary>
				SW_MAXIMIZE = 3,

				/// <summary>Displays a window in its most recent size and position. This value is similar to SW_SHOWNORMAL,
				/// except the window is not actived.</summary>
				SW_SHOWNOACTIVATE = 4,

				/// <summary>Activates the window and displays it in its current size and position. </summary>
				SW_SHOW = 5,

				/// <summary>Minimizes the specified window and activates the next top-level window in the z-order.</summary>
				SW_MINIMIZE = 6,

				/// <summary>Displays the window as a minimized window. This value is similar to SW_SHOWMINIMIZED,
				/// except the window is not activated.</summary>
				SW_SHOWMINNOACTIVE = 7,

				/// <summary>Displays the window in its current size and position. This value is similar to SW_SHOW,
				/// except the window is not activated.</summary>
				SW_SHOWNA = 8,

				/// <summary>Activates and displays the window. If the window is minimized or maximized,
				/// the system restores it to its original size and position. An application should specify this flag
				/// when restoring a minimized window.</summary>
				SW_RESTORE = 9,

				/// <summary>Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to
				/// the CreateProcess function by the program that started the application. </summary>
				SW_SHOWDEFAULT = 10,

				/// <summary>Windows 2000/XP: Minimizes a window, even if the thread that owns the window is not responding.
				/// This flag should only be used when minimizing windows from a different thread.</summary>
				SW_FORCEMINIMIZE = 11,
			}

			/// <summary></summary>
			public uint length;
			/// <summary></summary>
			public uint flags;
			/// <summary></summary>
			public uint showCmd;
			/// <summary></summary>
			public POINT ptMinPosition;
			/// <summary></summary>
			public POINT ptMaxPosition;
			/// <summary></summary>
			public RECT rcNormalPosition;

			///// --------------------------------------------------------------------------------
			///// <summary>
			///// Initializes a new instance of the <see cref="T:WINDOWPLACEMENT"/> class.
			///// </summary>
			///// <param name="showCmd">Specifies the current show state of the window.</param>
			///// --------------------------------------------------------------------------------
			//public WINDOWPLACEMENT(ShowWindowCommands showCmd)
			//{
			//    length = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINDOWPLACEMENT));
			//    this.showCmd = (uint)showCmd;
			//}
		}

		/// <summary></summary>
		public enum CommonControlInitFlags
		{
			/// <summary></summary>
			ICC_LISTVIEW_CLASSES = 0x00000001,
			/// <summary></summary>
			ICC_TREEVIEW_CLASSES = 0x00000002,
			/// <summary></summary>
			ICC_BAR_CLASSES = 0x00000004,
			/// <summary></summary>
			ICC_TAB_CLASSES = 0x00000008,
			/// <summary></summary>
			ICC_UPDOWN_CLASS = 0x00000010,
			/// <summary></summary>
			ICC_PROGRESS_CLASS = 0x00000020,
			/// <summary></summary>
			ICC_HOTKEY_CLASS = 0x00000040,
			/// <summary></summary>
			ICC_ANIMATE_CLASS = 0x00000080,
			/// <summary></summary>
			ICC_WIN95_CLASSES = 0x000000FF,
			/// <summary></summary>
			ICC_DATE_CLASSES = 0x00000100,
			/// <summary></summary>
			ICC_USEREX_CLASSES = 0x00000200,
			/// <summary></summary>
			ICC_COOL_CLASSES = 0x00000400,
			/// <summary></summary>
			ICC_INTERNET_CLASSES = 0x00000800,
			/// <summary></summary>
			ICC_PAGESCROLLER_CLASS = 0x00001000,
			/// <summary></summary>
			ICC_NATIVEFNTCTL_CLASS = 0x00002000
		}

		/// <summary></summary>
		public enum CommonControlStyles
		{
			/// <summary></summary>
			CCS_TOP = 0x00000001,
			/// <summary></summary>
			CCS_NOMOVEY = 0x00000002,
			/// <summary></summary>
			CCS_BOTTOM = 0x00000003,
			/// <summary></summary>
			CCS_NORESIZE = 0x00000004,
			/// <summary></summary>
			CCS_NOPARENTALIGN = 0x00000008,
			/// <summary></summary>
			CCS_ADJUSTABLE = 0x00000020,
			/// <summary></summary>
			CCS_NODIVIDER = 0x00000040,
			/// <summary></summary>
			CCS_VERT = 0x00000080,
			/// <summary></summary>
			CCS_LEFT = (CCS_VERT | CCS_TOP),
			/// <summary></summary>
			CCS_RIGHT = (CCS_VERT | CCS_BOTTOM),
			/// <summary></summary>
			CCS_NOMOVEX = (CCS_VERT | CCS_NOMOVEY)
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public class INITCOMMONCONTROLSEX
		{
			/// <summary></summary>
			public int dwSize;
			/// <summary></summary>
			public int dwICC;
		}

		/// <summary></summary>
		public enum RebarStyles
		{
			/// <summary></summary>
			RBS_TOOLTIPS = 0x0100,
			/// <summary></summary>
			RBS_VARHEIGHT = 0x0200,
			/// <summary></summary>
			RBS_BANDBORDERS = 0x0400,
			/// <summary></summary>
			RBS_FIXEDORDER = 0x0800,
			/// <summary></summary>
			RBS_REGISTERDROP = 0x1000,
			/// <summary></summary>
			RBS_AUTOSIZE = 0x2000,
			/// <summary></summary>
			RBS_VERTICALGRIPPER = 0x4000,
			/// <summary></summary>
			RBS_DBLCLKTOGGLE = 0x8000,
		}

		/// <summary></summary>
		public enum RebarNotifications
		{
			/// <summary></summary>
			RBN_FIRST = (0 - 831),
			/// <summary></summary>
			RBN_HEIGHTCHANGE = (RBN_FIRST - 0),
			/// <summary></summary>
			RBN_GETOBJECT = (RBN_FIRST - 1),
			/// <summary></summary>
			RBN_LAYOUTCHANGED = (RBN_FIRST - 2),
			/// <summary></summary>
			RBN_AUTOSIZE = (RBN_FIRST - 3),
			/// <summary></summary>
			RBN_BEGINDRAG = (RBN_FIRST - 4),
			/// <summary></summary>
			RBN_ENDDRAG = (RBN_FIRST - 5),
			/// <summary></summary>
			RBN_DELETINGBAND = (RBN_FIRST - 6),
			/// <summary></summary>
			RBN_DELETEDBAND = (RBN_FIRST - 7),
			/// <summary></summary>
			RBN_CHILDSIZE = (RBN_FIRST - 8),
			/// <summary></summary>
			RBN_CHEVRONPUSHED = (RBN_FIRST - 10)
		}

		/// <summary></summary>
		public enum RebarMessages
		{
			/// <summary></summary>
			CCM_FIRST = 0x2000,
			/// <summary></summary>
			RB_INSERTBANDA = (WinMsgs.WM_USER + 1),
			/// <summary></summary>
			RB_DELETEBAND = (WinMsgs.WM_USER + 2),
			/// <summary></summary>
			RB_GETBARINFO = (WinMsgs.WM_USER + 3),
			/// <summary></summary>
			RB_SETBARINFO = (WinMsgs.WM_USER + 4),
			/// <summary></summary>
			RB_GETBANDINFO = (WinMsgs.WM_USER + 5),
			/// <summary></summary>
			RB_SETBANDINFOA = (WinMsgs.WM_USER + 6),
			/// <summary></summary>
			RB_SETPARENT = (WinMsgs.WM_USER + 7),
			/// <summary></summary>
			RB_HITTEST = (WinMsgs.WM_USER + 8),
			/// <summary></summary>
			RB_GETRECT = (WinMsgs.WM_USER + 9),
			/// <summary></summary>
			RB_INSERTBANDW = (WinMsgs.WM_USER + 10),
			/// <summary></summary>
			RB_SETBANDINFOW = (WinMsgs.WM_USER + 11),
			/// <summary></summary>
			RB_GETBANDCOUNT = (WinMsgs.WM_USER + 12),
			/// <summary></summary>
			RB_GETROWCOUNT = (WinMsgs.WM_USER + 13),
			/// <summary></summary>
			RB_GETROWHEIGHT = (WinMsgs.WM_USER + 14),
			/// <summary></summary>
			RB_IDTOINDEX = (WinMsgs.WM_USER + 16),
			/// <summary></summary>
			RB_GETTOOLTIPS = (WinMsgs.WM_USER + 17),
			/// <summary></summary>
			RB_SETTOOLTIPS = (WinMsgs.WM_USER + 18),
			/// <summary></summary>
			RB_SETBKCOLOR = (WinMsgs.WM_USER + 19),
			/// <summary></summary>
			RB_GETBKCOLOR = (WinMsgs.WM_USER + 20),
			/// <summary></summary>
			RB_SETTEXTCOLOR = (WinMsgs.WM_USER + 21),
			/// <summary></summary>
			RB_GETTEXTCOLOR = (WinMsgs.WM_USER + 22),
			/// <summary></summary>
			RB_SIZETORECT = (WinMsgs.WM_USER + 23),
			/// <summary></summary>
			RB_SETCOLORSCHEME = (CCM_FIRST + 2),
			/// <summary></summary>
			RB_GETCOLORSCHEME = (CCM_FIRST + 3),
			/// <summary></summary>
			RB_BEGINDRAG = (WinMsgs.WM_USER + 24),
			/// <summary></summary>
			RB_ENDDRAG = (WinMsgs.WM_USER + 25),
			/// <summary></summary>
			RB_DRAGMOVE = (WinMsgs.WM_USER + 26),
			/// <summary></summary>
			RB_GETBARHEIGHT = (WinMsgs.WM_USER + 27),
			/// <summary></summary>
			RB_GETBANDINFOW = (WinMsgs.WM_USER + 28),
			/// <summary></summary>
			RB_GETBANDINFOA = (WinMsgs.WM_USER + 29),
			/// <summary></summary>
			RB_MINIMIZEBAND = (WinMsgs.WM_USER + 30),
			/// <summary></summary>
			RB_MAXIMIZEBAND = (WinMsgs.WM_USER + 31),
			/// <summary></summary>
			RB_GETDROPTARGET = (CCM_FIRST + 4),
			/// <summary></summary>
			RB_GETBANDBORDERS = (WinMsgs.WM_USER + 34),
			/// <summary></summary>
			RB_SHOWBAND = (WinMsgs.WM_USER + 35),
			/// <summary></summary>
			RB_SETPALETTE = (WinMsgs.WM_USER + 37),
			/// <summary></summary>
			RB_GETPALETTE = (WinMsgs.WM_USER + 38),
			/// <summary></summary>
			RB_MOVEBAND = (WinMsgs.WM_USER + 39),
			/// <summary></summary>
			RB_SETUNICODEFORMAT = (CCM_FIRST + 5),
			/// <summary></summary>
			RB_GETUNICODEFORMAT = (CCM_FIRST + 6)
		}

		/// <summary></summary>
		public enum RebarInfoMask
		{
			/// <summary></summary>
			RBBIM_STYLE = 0x00000001,
			/// <summary></summary>
			RBBIM_COLORS = 0x00000002,
			/// <summary></summary>
			RBBIM_TEXT = 0x00000004,
			/// <summary></summary>
			RBBIM_IMAGE = 0x00000008,
			/// <summary></summary>
			RBBIM_CHILD = 0x00000010,
			/// <summary></summary>
			RBBIM_CHILDSIZE = 0x00000020,
			/// <summary></summary>
			RBBIM_SIZE = 0x00000040,
			/// <summary></summary>
			RBBIM_BACKGROUND = 0x00000080,
			/// <summary></summary>
			RBBIM_ID = 0x00000100,
			/// <summary></summary>
			RBBIM_IDEALSIZE = 0x00000200,
			/// <summary></summary>
			RBBIM_LPARAM = 0x00000400,
			/// <summary></summary>
			BBIM_HEADERSIZE = 0x00000800
		}

		/// <summary></summary>
		public enum RebarStylesEx
		{
			/// <summary></summary>
			RBBS_BREAK = 0x1,
			/// <summary></summary>
			RBBS_FIXEDSIZE = 0x2,
			/// <summary></summary>
			RBBS_CHILDEDGE = 0x4,
			/// <summary></summary>
			RBBS_FIXEDBMP = 0x20,
			/// <summary></summary>
			RBBS_GRIPPERALWAYS = 0x80,
			/// <summary></summary>
			RBBS_NOGRIPPER = 0x100,
			/// <summary></summary>
			RBBS_USECHEVRON = 0x200
		}

		/// <summary></summary>
		public enum RebarHitTestFlags
		{
			/// <summary></summary>
			RBHT_NOWHERE = 0x0001,
			/// <summary></summary>
			RBHT_CAPTION = 0x0002,
			/// <summary></summary>
			RBHT_CLIENT = 0x0003,
			/// <summary></summary>
			RBHT_GRABBER = 0x0004,
			/// <summary></summary>
			RBHT_CHEVRON = 0x0008
		}

		/// <summary></summary>
		public enum ToolBarStyles
		{
			/// <summary></summary>
			TBSTYLE_BUTTON = 0x0000,
			/// <summary></summary>
			TBSTYLE_SEP = 0x0001,
			/// <summary></summary>
			TBSTYLE_CHECK = 0x0002,
			/// <summary></summary>
			TBSTYLE_GROUP = 0x0004,
			/// <summary></summary>
			TBSTYLE_CHECKGROUP = (TBSTYLE_GROUP | TBSTYLE_CHECK),
			/// <summary></summary>
			TBSTYLE_DROPDOWN = 0x0008,
			/// <summary></summary>
			TBSTYLE_AUTOSIZE = 0x0010,
			/// <summary></summary>
			TBSTYLE_NOPREFIX = 0x0020,
			/// <summary></summary>
			TBSTYLE_TOOLTIPS = 0x0100,
			/// <summary></summary>
			TBSTYLE_WRAPABLE = 0x0200,
			/// <summary></summary>
			TBSTYLE_ALTDRAG = 0x0400,
			/// <summary></summary>
			TBSTYLE_FLAT = 0x0800,
			/// <summary></summary>
			TBSTYLE_LIST = 0x1000,
			/// <summary></summary>
			TBSTYLE_CUSTOMERASE = 0x2000,
			/// <summary></summary>
			TBSTYLE_REGISTERDROP = 0x4000,
			/// <summary></summary>
			TBSTYLE_TRANSPARENT = 0x8000,
			/// <summary></summary>
			TBSTYLE_EX_DRAWDDARROWS = 0x00000001
		}

		/// <summary></summary>
		public enum ToolBarExStyles
		{
			/// <summary></summary>
			TBSTYLE_EX_DRAWDDARROWS = 0x1,
			/// <summary></summary>
			TBSTYLE_EX_HIDECLIPPEDBUTTONS = 0x10,
			/// <summary></summary>
			TBSTYLE_EX_DOUBLEBUFFER = 0x80
		}

		/// <summary></summary>
		public enum ToolBarButtonStyles
		{
			/// <summary></summary>
			TBSTYLE_BUTTON = 0x0000,
			/// <summary></summary>
			TBSTYLE_SEP = 0x0001,
			/// <summary></summary>
			TBSTYLE_CHECK = 0x0002,
			/// <summary></summary>
			TBSTYLE_GROUP = 0x0004,
			/// <summary></summary>
			TBSTYLE_CHECKGROUP = (TBSTYLE_GROUP | TBSTYLE_CHECK),
			/// <summary></summary>
			TBSTYLE_DROPDOWN = 0x0008,
			/// <summary></summary>
			TBSTYLE_AUTOSIZE = 0x0010,
			/// <summary></summary>
			TBSTYLE_NOPREFIX = 0x0020,
			/// <summary></summary>
			TBSTYLE_TOOLTIPS = 0x0100,
			/// <summary></summary>
			TBSTYLE_WRAPABLE = 0x0200,
			/// <summary></summary>
			TBSTYLE_ALTDRAG = 0x0400,
			/// <summary></summary>
			TBSTYLE_FLAT = 0x0800,
			/// <summary></summary>
			TBSTYLE_LIST = 0x1000,
			/// <summary></summary>
			TBSTYLE_CUSTOMERASE = 0x2000,
			/// <summary></summary>
			TBSTYLE_REGISTERDROP = 0x4000,
			/// <summary></summary>
			TBSTYLE_TRANSPARENT = 0x8000,
			/// <summary></summary>
			TBSTYLE_EX_DRAWDDARROWS = 0x00000001
		}

		/// <summary></summary>
		public enum ToolBarButtonStates
		{
			/// <summary></summary>
			TBSTATE_CHECKED = 0x01,
			/// <summary></summary>
			TBSTATE_PRESSED = 0x02,
			/// <summary></summary>
			TBSTATE_ENABLED = 0x04,
			/// <summary></summary>
			TBSTATE_HIDDEN = 0x08,
			/// <summary></summary>
			TBSTATE_INDETERMINATE = 0x10,
			/// <summary></summary>
			TBSTATE_WRAP = 0x20,
			/// <summary></summary>
			TBSTATE_ELLIPSES = 0x40,
			/// <summary></summary>
			TBSTATE_MARKED = 0x80
		}

		/// <summary></summary>
		public enum ToolBarMessages
		{
			/// <summary></summary>
			TB_ENABLEBUTTON = (WinMsgs.WM_USER + 1),
			/// <summary></summary>
			TB_CHECKBUTTON = (WinMsgs.WM_USER + 2),
			/// <summary></summary>
			TB_PRESSBUTTON = (WinMsgs.WM_USER + 3),
			/// <summary></summary>
			TB_HIDEBUTTON = (WinMsgs.WM_USER + 4),
			/// <summary></summary>
			TB_INDETERMINATE = (WinMsgs.WM_USER + 5),
			/// <summary></summary>
			TB_MARKBUTTON = (WinMsgs.WM_USER + 6),
			/// <summary></summary>
			TB_ISBUTTONENABLED = (WinMsgs.WM_USER + 9),
			/// <summary></summary>
			TB_ISBUTTONHIDDEN = (WinMsgs.WM_USER + 12),
			/// <summary></summary>
			TB_ISBUTTONINDETERMINATE = (WinMsgs.WM_USER + 13),
			/// <summary></summary>
			TB_ISBUTTONHIGHLIGHTED = (WinMsgs.WM_USER + 14),
			/// <summary></summary>
			TB_SETSTATE = (WinMsgs.WM_USER + 17),
			/// <summary></summary>
			TB_GETSTATE = (WinMsgs.WM_USER + 18),
			/// <summary></summary>
			TB_ADDBITMAP = (WinMsgs.WM_USER + 19),
			/// <summary></summary>
			TB_ADDBUTTONSA = (WinMsgs.WM_USER + 20),
			/// <summary></summary>
			TB_INSERTBUTTONA = (WinMsgs.WM_USER + 21),
			/// <summary></summary>
			TB_ADDBUTTONS = (WinMsgs.WM_USER + 20),
			/// <summary></summary>
			TB_INSERTBUTTON = (WinMsgs.WM_USER + 21),
			/// <summary></summary>
			TB_DELETEBUTTON = (WinMsgs.WM_USER + 22),
			/// <summary></summary>
			TB_GETBUTTON = (WinMsgs.WM_USER + 23),
			/// <summary></summary>
			TB_BUTTONCOUNT = (WinMsgs.WM_USER + 24),
			/// <summary></summary>
			TB_COMMANDTOINDEX = (WinMsgs.WM_USER + 25),
			/// <summary></summary>
			TB_SAVERESTOREA = (WinMsgs.WM_USER + 26),
			/// <summary></summary>
			TB_CUSTOMIZE = (WinMsgs.WM_USER + 27),
			/// <summary></summary>
			TB_ADDSTRINGA = (WinMsgs.WM_USER + 28),
			/// <summary></summary>
			TB_GETITEMRECT = (WinMsgs.WM_USER + 29),
			/// <summary></summary>
			TB_BUTTONSTRUCTSIZE = (WinMsgs.WM_USER + 30),
			/// <summary></summary>
			TB_SETBUTTONSIZE = (WinMsgs.WM_USER + 31),
			/// <summary></summary>
			TB_SETBITMAPSIZE = (WinMsgs.WM_USER + 32),
			/// <summary></summary>
			TB_AUTOSIZE = (WinMsgs.WM_USER + 33),
			/// <summary></summary>
			TB_GETTOOLTIPS = (WinMsgs.WM_USER + 35),
			/// <summary></summary>
			TB_SETTOOLTIPS = (WinMsgs.WM_USER + 36),
			/// <summary></summary>
			TB_SETPARENT = (WinMsgs.WM_USER + 37),
			/// <summary></summary>
			TB_SETROWS = (WinMsgs.WM_USER + 39),
			/// <summary></summary>
			TB_GETROWS = (WinMsgs.WM_USER + 40),
			/// <summary></summary>
			TB_GETBITMAPFLAGS = (WinMsgs.WM_USER + 41),
			/// <summary></summary>
			TB_SETCMDID = (WinMsgs.WM_USER + 42),
			/// <summary></summary>
			TB_CHANGEBITMAP = (WinMsgs.WM_USER + 43),
			/// <summary></summary>
			TB_GETBITMAP = (WinMsgs.WM_USER + 44),
			/// <summary></summary>
			TB_GETBUTTONTEXTA = (WinMsgs.WM_USER + 45),
			/// <summary></summary>
			TB_GETBUTTONTEXTW = (WinMsgs.WM_USER + 75),
			/// <summary></summary>
			TB_REPLACEBITMAP = (WinMsgs.WM_USER + 46),
			/// <summary></summary>
			TB_SETINDENT = (WinMsgs.WM_USER + 47),
			/// <summary></summary>
			TB_SETIMAGELIST = (WinMsgs.WM_USER + 48),
			/// <summary></summary>
			TB_GETIMAGELIST = (WinMsgs.WM_USER + 49),
			/// <summary></summary>
			TB_LOADIMAGES = (WinMsgs.WM_USER + 50),
			/// <summary></summary>
			TB_GETRECT = (WinMsgs.WM_USER + 51),
			/// <summary></summary>
			TB_SETHOTIMAGELIST = (WinMsgs.WM_USER + 52),
			/// <summary></summary>
			TB_GETHOTIMAGELIST = (WinMsgs.WM_USER + 53),
			/// <summary></summary>
			TB_SETDISABLEDIMAGELIST = (WinMsgs.WM_USER + 54),
			/// <summary></summary>
			TB_GETDISABLEDIMAGELIST = (WinMsgs.WM_USER + 55),
			/// <summary></summary>
			TB_SETSTYLE = (WinMsgs.WM_USER + 56),
			/// <summary></summary>
			TB_GETSTYLE = (WinMsgs.WM_USER + 57),
			/// <summary></summary>
			TB_GETBUTTONSIZE = (WinMsgs.WM_USER + 58),
			/// <summary></summary>
			TB_SETBUTTONWIDTH = (WinMsgs.WM_USER + 59),
			/// <summary></summary>
			TB_SETMAXTEXTROWS = (WinMsgs.WM_USER + 60),
			/// <summary></summary>
			TB_GETTEXTROWS = (WinMsgs.WM_USER + 61),
			/// <summary></summary>
			TB_GETOBJECT = (WinMsgs.WM_USER + 62),
			/// <summary></summary>
			TB_GETBUTTONINFOW = (WinMsgs.WM_USER + 63),
			/// <summary></summary>
			TB_SETBUTTONINFOW = (WinMsgs.WM_USER + 64),
			/// <summary></summary>
			TB_GETBUTTONINFOA = (WinMsgs.WM_USER + 65),
			/// <summary></summary>
			TB_SETBUTTONINFOA = (WinMsgs.WM_USER + 66),
			/// <summary></summary>
			TB_INSERTBUTTONW = (WinMsgs.WM_USER + 67),
			/// <summary></summary>
			TB_ADDBUTTONSW = (WinMsgs.WM_USER + 68),
			/// <summary></summary>
			TB_HITTEST = (WinMsgs.WM_USER + 69),
			/// <summary></summary>
			TB_SETDRAWTEXTFLAGS = (WinMsgs.WM_USER + 70),
			/// <summary></summary>
			TB_GETHOTITEM = (WinMsgs.WM_USER + 71),
			/// <summary></summary>
			TB_SETHOTITEM = (WinMsgs.WM_USER + 72),
			/// <summary></summary>
			TB_SETANCHORHIGHLIGHT = (WinMsgs.WM_USER + 73),
			/// <summary></summary>
			TB_GETANCHORHIGHLIGHT = (WinMsgs.WM_USER + 74),
			/// <summary></summary>
			TB_SAVERESTOREW = (WinMsgs.WM_USER + 76),
			/// <summary></summary>
			TB_ADDSTRINGW = (WinMsgs.WM_USER + 77),
			/// <summary></summary>
			TB_MAPACCELERATORA = (WinMsgs.WM_USER + 78),
			/// <summary></summary>
			TB_GETINSERTMARK = (WinMsgs.WM_USER + 79),
			/// <summary></summary>
			TB_SETINSERTMARK = (WinMsgs.WM_USER + 80),
			/// <summary></summary>
			TB_INSERTMARKHITTEST = (WinMsgs.WM_USER + 81),
			/// <summary></summary>
			TB_MOVEBUTTON = (WinMsgs.WM_USER + 82),
			/// <summary></summary>
			TB_GETMAXSIZE = (WinMsgs.WM_USER + 83),
			/// <summary></summary>
			TB_SETEXTENDEDSTYLE = (WinMsgs.WM_USER + 84),
			/// <summary></summary>
			TB_GETEXTENDEDSTYLE = (WinMsgs.WM_USER + 85),
			/// <summary></summary>
			TB_GETPADDING = (WinMsgs.WM_USER + 86),
			/// <summary></summary>
			TB_SETPADDING = (WinMsgs.WM_USER + 87),
			/// <summary></summary>
			TB_SETINSERTMARKCOLOR = (WinMsgs.WM_USER + 88),
			/// <summary></summary>
			TB_GETINSERTMARKCOLOR = (WinMsgs.WM_USER + 89)
		}

		/// <summary></summary>
		public enum ToolBarNotifications
		{
			/// <summary></summary>
			TTN_NEEDTEXTA = ((0 - 520) - 0),
			/// <summary></summary>
			TTN_NEEDTEXTW = ((0 - 520) - 10),
			/// <summary></summary>
			TBN_QUERYINSERT = ((0 - 700) - 6),
			/// <summary></summary>
			TBN_DROPDOWN = ((0 - 700) - 10),
			/// <summary></summary>
			TBN_HOTITEMCHANGE = ((0 - 700) - 13)
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct TBBUTTON
		{
			/// <summary></summary>
			public int iBitmap;
			/// <summary></summary>
			public int idCommand;
			/// <summary></summary>
			public byte fsState;
			/// <summary></summary>
			public byte fsStyle;
			/// <summary></summary>
			public byte bReserved0;
			/// <summary></summary>
			public byte bReserved1;
			/// <summary></summary>
			public int dwData;
			/// <summary></summary>
			public int iString;
		}

		/// <summary></summary>
		public struct NMTOOLBAR
		{
			/// <summary></summary>
			public NMHDR hdr;
			/// <summary></summary>
			public int iItem;
			/// <summary></summary>
			public TBBUTTON tbButton;
			/// <summary></summary>
			public int cchText;
			/// <summary></summary>
			public IntPtr pszText;
			/// <summary></summary>
			public RECT rcButton;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct TBBUTTONINFO
		{
			/// <summary></summary>
			public int cbSize;
			/// <summary></summary>
			public int dwMask;
			/// <summary></summary>
			public int idCommand;
			/// <summary></summary>
			public int iImage;
			/// <summary></summary>
			public byte fsState;
			/// <summary></summary>
			public byte fsStyle;
			/// <summary></summary>
			public short cx;
			/// <summary></summary>
			public IntPtr lParam;
			/// <summary></summary>
			public IntPtr pszText;
			/// <summary></summary>
			public int cchText;
		}

		/// <summary></summary>
		public enum ToolBarButtonInfoFlags : long
		{
			/// <summary></summary>
			TBIF_IMAGE = 0x00000001,
			/// <summary></summary>
			TBIF_TEXT = 0x00000002,
			/// <summary></summary>
			TBIF_STATE = 0x00000004,
			/// <summary></summary>
			TBIF_STYLE = 0x00000008,
			/// <summary></summary>
			TBIF_LPARAM = 0x00000010,
			/// <summary></summary>
			TBIF_COMMAND = 0x00000020,
			/// <summary></summary>
			TBIF_SIZE = 0x00000040,
			//TBIF_BYINDEX           = 0x80000000,
			/// <summary></summary>
			I_IMAGECALLBACK = -1,
			/// <summary></summary>
			I_IMAGENONE = -2
		}

		#endregion

		#region Ole32.dll
		/// <summary>
		/// Carries out the clipboard shutdown sequence. It also releases the <c>IDataObject</c>
		/// pointer that was previously placed on the clipboard.
		/// </summary>
		/// <returns><c>true</c> if the clipboard has been flushed.</returns>
		[DllImport("ole32.dll")]
		public extern static int OleFlushClipboard();

		/// <summary>
		/// Determines whether the data object pointer previously placed on the clipboard is
		/// still on the clipboard.
		/// </summary>
		/// <param name="pDataObject">[in] Pointer to the data object previously copied or cut.</param>
		/// <returns><c>true</c> if object still on the clipboard.</returns>
		[DllImport("ole32.dll")]
		public extern static bool OleIsCurrentClipboard([MarshalAs(UnmanagedType.IUnknown)]object pDataObject);
		#endregion

		#region Shell32.dll

		/// <summary></summary>
		public enum LVNotifications
		{
			/// <summary></summary>
			LVN_FIRST = (0 - 100),
			/// <summary></summary>
			LVN_GETDISPINFOW = (LVN_FIRST - 77),
			/// <summary></summary>
			LVN_SETDISPINFOA = (LVN_FIRST - 51),
			/// <summary></summary>
			LVN_BEGINSCROLL = (LVN_FIRST - 80),
			/// <summary></summary>
			LVN_ENDSCROLL = (LVN_FIRST - 81)
		}

		/// <summary></summary>
		public enum ReflectedMsgs
		{
			/// <summary></summary>
			OCM__BASE = (WinMsgs.WM_USER + 0x1c00),
			/// <summary></summary>
			OCM_COMMAND = (OCM__BASE + WinMsgs.WM_COMMAND),
			/// <summary></summary>
			OCM_CTLCOLORBTN = (OCM__BASE + WinMsgs.WM_CTLCOLORBTN),
			/// <summary></summary>
			OCM_CTLCOLOREDIT = (OCM__BASE + WinMsgs.WM_CTLCOLOREDIT),
			/// <summary></summary>
			OCM_CTLCOLORDLG = (OCM__BASE + WinMsgs.WM_CTLCOLORDLG),
			/// <summary></summary>
			OCM_CTLCOLORLISTBOX = (OCM__BASE + WinMsgs.WM_CTLCOLORLISTBOX),
			/// <summary></summary>
			OCM_CTLCOLORMSGBOX = (OCM__BASE + WinMsgs.WM_CTLCOLORMSGBOX),
			/// <summary></summary>
			OCM_CTLCOLORSCROLLBAR = (OCM__BASE + WinMsgs.WM_CTLCOLORSCROLLBAR),
			/// <summary></summary>
			OCM_CTLCOLORSTATIC = (OCM__BASE + WinMsgs.WM_CTLCOLORSTATIC),
			/// <summary></summary>
			OCM_CTLCOLOR = (OCM__BASE + WinMsgs.WM_CTLCOLOR),
			/// <summary></summary>
			OCM_DRAWITEM = (OCM__BASE + WinMsgs.WM_DRAWITEM),
			/// <summary></summary>
			OCM_MEASUREITEM = (OCM__BASE + WinMsgs.WM_MEASUREITEM),
			/// <summary></summary>
			OCM_DELETEITEM = (OCM__BASE + WinMsgs.WM_DELETEITEM),
			/// <summary></summary>
			OCM_VKEYTOITEM = (OCM__BASE + WinMsgs.WM_VKEYTOITEM),
			/// <summary></summary>
			OCM_CHARTOITEM = (OCM__BASE + WinMsgs.WM_CHARTOITEM),
			/// <summary></summary>
			OCM_COMPAREITEM = (OCM__BASE + WinMsgs.WM_COMPAREITEM),
			/// <summary></summary>
			OCM_HSCROLL = (OCM__BASE + WinMsgs.WM_HSCROLL),
			/// <summary></summary>
			OCM_VSCROLL = (OCM__BASE + WinMsgs.WM_VSCROLL),
			/// <summary></summary>
			OCM_PARENTNOTIFY = (OCM__BASE + WinMsgs.WM_PARENTNOTIFY),
			/// <summary></summary>
			OCM_NOTIFY = (OCM__BASE + WinMsgs.WM_NOTIFY)
		}

		/// <summary></summary>
		public enum HdrCtrlNotifications
		{
			/// <summary></summary>
			HDN_FIRST = -300,
			/// <summary></summary>
			HDN_BEGINTRACK = (HDN_FIRST - 26),
			/// <summary></summary>
			HDN_TRACK = (HDN_FIRST - 28),
			/// <summary></summary>
			HDN_ENDTRACK = (HDN_FIRST - 27),
			/// <summary></summary>
			HDN_ITEMCHANGING = (HDN_FIRST - 20),
			/// <summary></summary>
			HDN_ITEMCHANGED = (HDN_FIRST - 21),
			/// <summary></summary>
			HDN_ITEMCLICK = (HDN_FIRST - 22),
			/// <summary></summary>
			HDN_ITEMDBLCLICK = (HDN_FIRST - 23),
			/// <summary></summary>
			HDN_DIVIDERDBLCLICK = (HDN_FIRST - 25),
			/// <summary></summary>
			HDN_GETDISPINFOW = (HDN_FIRST - 29),
			/// <summary></summary>
			HDN_BEGINDRAG = (HDN_FIRST - 10),
			/// <summary></summary>
			HDN_ENDDRAG = (HDN_FIRST - 11)
		}

		/// <summary></summary>
		public enum HdrCtrlMsgs : int
		{
			/// <summary></summary>
			HDM_FIRST = 0x1200,
			/// <summary></summary>
			HDM_GETITEMRECT = (HDM_FIRST + 7),
			/// <summary></summary>
			HDM_HITTEST = (HDM_FIRST + 6),
			/// <summary></summary>
			HDM_SETIMAGELIST = (HDM_FIRST + 8),
			/// <summary></summary>
			HDM_GETITEMW = (HDM_FIRST + 11),
			/// <summary></summary>
			HDM_ORDERTOINDEX = (HDM_FIRST + 15)
		}

		/// <summary></summary>
		public enum CDReturnFlags
		{
			/// <summary></summary>
			CDRF_DODEFAULT = 0x00000000,
			/// <summary></summary>
			CDRF_NEWFONT = 0x00000002,
			/// <summary></summary>
			CDRF_SKIPDEFAULT = 0x00000004,
			/// <summary></summary>
			CDRF_NOTIFYPOSTPAINT = 0x00000010,
			/// <summary></summary>
			CDRF_NOTIFYITEMDRAW = 0x00000020,
			/// <summary></summary>
			CDRF_NOTIFYSUBITEMDRAW = 0x00000020,
			/// <summary></summary>
			CDRF_NOTIFYPOSTERASE = 0x00000040
		}

		/// <summary></summary>
		public enum CDItemStateFlags : uint
		{
			/// <summary></summary>
			CDIS_SELECTED = 0x0001,
			/// <summary></summary>
			CDIS_GRAYED = 0x0002,
			/// <summary></summary>
			CDIS_DISABLED = 0x0004,
			/// <summary></summary>
			CDIS_CHECKED = 0x0008,
			/// <summary></summary>
			CDIS_FOCUS = 0x0010,
			/// <summary></summary>
			CDIS_DEFAULT = 0x0020,
			/// <summary></summary>
			CDIS_HOT = 0x0040,
			/// <summary></summary>
			CDIS_MARKED = 0x0080,
			/// <summary></summary>
			CDIS_INDETERMINATE = 0x0100
		}

		/// <summary></summary>
		public enum CDStateFlags
		{
			/// <summary></summary>
			CDDS_PREPAINT = 0x00000001,
			/// <summary></summary>
			CDDS_POSTPAINT = 0x00000002,
			/// <summary></summary>
			CDDS_PREERASE = 0x00000003,
			/// <summary></summary>
			CDDS_POSTERASE = 0x00000004,
			/// <summary></summary>
			CDDS_ITEM = 0x00010000,
			/// <summary></summary>
			CDDS_ITEMPREPAINT = (CDDS_ITEM | CDDS_PREPAINT),
			/// <summary></summary>
			CDDS_ITEMPOSTPAINT = (CDDS_ITEM | CDDS_POSTPAINT),
			/// <summary></summary>
			CDDS_ITEMPREERASE = (CDDS_ITEM | CDDS_PREERASE),
			/// <summary></summary>
			CDDS_ITEMPOSTERASE = (CDDS_ITEM | CDDS_POSTERASE),
			/// <summary></summary>
			CDDS_SUBITEM = 0x00020000
		}

		/// <summary></summary>
		public enum SubItemPortion
		{
			/// <summary></summary>
			LVIR_BOUNDS = 0,
			/// <summary></summary>
			LVIR_ICON = 1,
			/// <summary></summary>
			LVIR_LABEL = 2
		}

		/// <summary></summary>
		public enum LVMsgs
		{
			/// <summary></summary>
			LVM_FIRST = 0x1000,
			/// <summary></summary>
			LVM_GETITEMRECT = (LVM_FIRST + 14),
			/// <summary></summary>
			LVM_GETSUBITEMRECT = (LVM_FIRST + 56),
			/// <summary></summary>
			LVM_GETITEMSTATE = (LVM_FIRST + 44),
			/// <summary></summary>
			LVM_GETITEMTEXTW = (LVM_FIRST + 115)
		}

		/// <summary></summary>
		public enum LVItemFlags
		{
			/// <summary></summary>
			LVIF_TEXT = 0x0001,
			/// <summary></summary>
			LVIF_IMAGE = 0x0002,
			/// <summary></summary>
			LVIF_PARAM = 0x0004,
			/// <summary></summary>
			LVIF_STATE = 0x0008,
			/// <summary></summary>
			LVIF_INDENT = 0x0010,
			/// <summary></summary>
			LVIF_NORECOMPUTE = 0x0800
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct RECT
		{
			/// <summary></summary>
			public int left;
			/// <summary></summary>
			public int top;
			/// <summary></summary>
			public int right;
			/// <summary></summary>
			public int bottom;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct NMHDR
		{
			/// <summary></summary>
			public IntPtr hwndFrom;
			/// <summary></summary>
			public int idFrom;
			/// <summary></summary>
			public int code;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct HDITEM
		{
			/// <summary></summary>
			public uint mask;
			/// <summary></summary>
			public int cxy;
			/// <summary></summary>
			public IntPtr pszText;
			/// <summary></summary>
			public IntPtr hbm;
			/// <summary></summary>
			public int cchTextMax;
			/// <summary></summary>
			public int fmt;
			/// <summary></summary>
			public int lParam;
			/// <summary></summary>
			public int iImage;
			/// <summary></summary>
			public int iOrder;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct NMHEADER
		{
			/// <summary></summary>
			public NMHDR hdr;
			/// <summary></summary>
			public int iItem;
			/// <summary></summary>
			public int iButton;
			/// <summary></summary>
			public HDITEM pItem;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct NMCUSTOMDRAW
		{
			/// <summary></summary>
			public NMHDR hdr;
			/// <summary></summary>
			public int dwDrawStage;
			/// <summary></summary>
			public IntPtr hdc;
			/// <summary></summary>
			public RECT rc;
			/// <summary></summary>
			public uint dwItemSpec;
			/// <summary></summary>
			public uint uItemState;
			/// <summary></summary>
			public IntPtr lItemlParam;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct NMLVCUSTOMDRAW
		{
			/// <summary></summary>
			public NMCUSTOMDRAW nmcd;
			/// <summary></summary>
			public uint clrText;
			/// <summary></summary>
			public uint clrTextBk;
			/// <summary></summary>
			public int iSubItem;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct LVITEM
		{
			/// <summary></summary>
			public LVItemFlags mask;
			/// <summary></summary>
			public int iItem;
			/// <summary></summary>
			public int iSubItem;
			/// <summary></summary>
			public uint state;
			/// <summary></summary>
			public uint stateMask;
			/// <summary></summary>
			public IntPtr pszText;
			/// <summary></summary>
			public int cchTextMax;
			/// <summary></summary>
			public int iImage;
			/// <summary></summary>
			public int lParam;
			/// <summary></summary>
			public int iIndent;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct NMTBCUSTOMDRAW
		{
			/// <summary></summary>
			public NMCUSTOMDRAW nmcd;
			/// <summary></summary>
			public IntPtr hbrMonoDither;
			/// <summary></summary>
			public IntPtr hbrLines;
			/// <summary></summary>
			public IntPtr hpenLines;
			/// <summary></summary>
			public int clrText;
			/// <summary></summary>
			public int clrMark;
			/// <summary></summary>
			public int clrTextHighlight;
			/// <summary></summary>
			public int clrBtnFace;
			/// <summary></summary>
			public int clrBtnHighlight;
			/// <summary></summary>
			public int clrHighlightHotTrack;
			/// <summary></summary>
			public RECT rcText;
			/// <summary></summary>
			public int nStringBkMode;
			/// <summary></summary>
			public int nHLStringBkMode;
		}

		/// <summary></summary>
		public struct TOOLTIPTEXTA
		{
			/// <summary></summary>
			public NMHDR hdr;
			/// <summary></summary>
			public IntPtr lpszText;
			/// <summary></summary>
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szText;
			/// <summary></summary>
			public IntPtr hinst;
			/// <summary></summary>
			public int uFlags;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct TOOLTIPTEXT
		{
			/// <summary></summary>
			public NMHDR hdr;
			/// <summary></summary>
			public IntPtr lpszText;
			/// <summary></summary>
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szText;
			/// <summary></summary>
			public IntPtr hinst;
			/// <summary></summary>
			public int uFlags;
		}

		/// <summary></summary>
		public enum ToolTipFlags
		{
			/// <summary></summary>
			TTF_CENTERTIP = 0x0002,
			/// <summary></summary>
			TTF_RTLREADING = 0x0004,
			/// <summary></summary>
			TTF_SUBCLASS = 0x0010,
			/// <summary></summary>
			TTF_TRACK = 0x0020,
			/// <summary></summary>
			TTF_ABSOLUTE = 0x0080,
			/// <summary></summary>
			TTF_TRANSPARENT = 0x0100,
			/// <summary></summary>
			TTF_DI_SETITEM = 0x8000
		}

		/// <summary></summary>
		public enum NotificationMessages
		{
			/// <summary></summary>
			NM_FIRST = (0 - 0),
			/// <summary></summary>
			NM_CUSTOMDRAW = (NM_FIRST - 12),
			/// <summary></summary>
			NM_NCHITTEST = (NM_FIRST - 14),
			/// <summary></summary>
			NM_RELEASEDCAPTURE = (NM_FIRST - 16)
		}

		#endregion

		#region Imm32.dll

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the input context associated with the specified window.
		/// </summary>
		/// <param name="hWnd">The window handle.</param>
		/// <returns>Returns the handle to the input context.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("imm32.dll", CharSet=CharSet.Auto)]
		public static extern IntPtr ImmGetContext(HandleRef hWnd);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the current conversion status.
		/// </summary>
		/// <param name="context">Handle to the input context for which to retrieve information.
		/// </param>
		/// <param name="conversionMode">A combination of conversion mode values.</param>
		/// <param name="sentenceMode">The sentence mode value.</param>
		/// <returns><c>true</c> if the method succeeded, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("imm32.dll", CharSet=CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ImmGetConversionStatus(HandleRef context, out int conversionMode,
			out int sentenceMode);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current conversion status.
		/// </summary>
		/// <param name="context">Handle to the input context for which to retrieve information.
		/// </param>
		/// <param name="conversionMode">A combination of conversion mode values.</param>
		/// <param name="sentenceMode">The sentence mode value.</param>
		/// <returns><c>true</c> if the method succeeded, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("imm32.dll", CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ImmSetConversionStatus(HandleRef context, int conversionMode,
			int sentenceMode);
		#endregion

		/// <summary></summary>
		public const int SPI_GETNONCLIENTMETRICS = 41;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Windows system font.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Font GetCaptionFont()
		{
			NONCLIENTMETRICS ncm = new NONCLIENTMETRICS();
			ncm.cbSize = Marshal.SizeOf(typeof(NONCLIENTMETRICS));
			try
			{
				bool result = SystemParametersInfo(SPI_GETNONCLIENTMETRICS, ncm.cbSize, ref ncm, 0);
				//int lastError = Marshal.GetLastWin32Error();
				return (result ? Font.FromLogFont(ncm.lfCaptionFont) : null);
			}
			catch { }

			return null;
		}

		#region gdi32.dll

		[DllImport("gdi32.dll")]
		internal static extern uint GetFontUnicodeRanges(IntPtr hdc, IntPtr lpgs);

		[DllImport("gdi32.dll")]
		internal static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

		[DllImport("gdi32.dll")]
		internal static extern bool DeleteObject(IntPtr hObject);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public struct FontRange
		{
			/// <summary></summary>
			public UInt16 Low;
			/// <summary></summary>
			public UInt16 High;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static List<FontRange> GetUnicodeRangesForFont(Font font)
		{
			IntPtr hdc, hFont, old, glyphSet;
			List<FontRange> fontRanges;
			using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
			{
				hdc = g.GetHdc();
				hFont = font.ToHfont();
				old = SelectObject(hdc, hFont);
				uint size = GetFontUnicodeRanges(hdc, IntPtr.Zero);
				glyphSet = Marshal.AllocHGlobal((int)size);
				GetFontUnicodeRanges(hdc, glyphSet);
				fontRanges = new List<FontRange>();
				int count = Marshal.ReadInt32(glyphSet, 12);

				for (int i = 0; i < count; i++)
				{
					FontRange range = new FontRange();
					range.Low = (UInt16)Marshal.ReadInt16(glyphSet, 16 + i * 4);
					range.High = (UInt16)(range.Low + Marshal.ReadInt16(glyphSet, 18 + i * 4) - 1);
					fontRanges.Add(range);
				}

				g.ReleaseHdc(hdc);
			}
			SelectObject(hdc, old);
			Marshal.FreeHGlobal(glyphSet);
			DeleteObject(hFont);
			DeleteObject(hdc);

			return fontRanges;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a value indicating whether or not all the characters in the specified
		/// string have glyphs defined in the specified font.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool AreCharGlyphsInFont(string str, Font fnt)
		{
			if (str == null)
				return false;

#if !__MonoCS__ // TODO-Linux FWNX-159: port
			foreach (char chr in str)
			{
				if (!IsCharGlyphInFont(chr, fnt))
					return false;
			}
#endif

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a value indicating whether or not the character represented by the
		/// specified string has a glyph defined in the specified font. It's assumed the
		/// string's length is only one character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsCharGlyphInFont(string str, Font fnt)
		{
			return (string.IsNullOrEmpty(str) || fnt == null ?
				false : IsCharGlyphInFont(str[0], fnt));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a value indicating whether or not the specified character has a glyph
		/// defined in the specified font.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsCharGlyphInFont(char chr, Font fnt)
		{
			if ((int)chr <= 0 || fnt == null)
				return false;

			UInt16 intval = Convert.ToUInt16(chr);
			List<FontRange> ranges = GetUnicodeRangesForFont(fnt);
			bool isChrPresent = false;

			foreach (FontRange range in ranges)
			{
				if (intval >= range.Low && intval <= range.High)
				{
					isChrPresent = true;
					break;
				}
			}

			return isChrPresent;
		}

		#endregion
	}

	#endregion

	#region class LogicalFont
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Defines a class for holding info about a logical font.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public class LogicalFont
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LogicalFont"/> class.
		/// </summary>
		/// <param name="createFrom">The font to use as the basis for the logical font.</param>
		/// <remarks>If it's not obvious why we need this class in order to determine if a font
		/// is a symbol font, see the Dr. Gui article entitled
		/// <see href="http://msdn.microsoft.com/archive/default.asp?url=/archive/en-us/dnaraskdr/html/askgui12302003.asp">
		/// Determining the Character Set Used by a Font</see></remarks>
		/// ------------------------------------------------------------------------------------
		public LogicalFont(Font createFrom)
		{
			createFrom.ToLogFont(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this is a symbol font.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Boolean IsSymbolCharSet
		{
			get { return lfCharSet == (byte)TextMetricsCharacterSet.Symbol; }
		}
	}
	#endregion
}

// ---------------------------------------------------------
// Windows Forms CommandBar Control
// Copyright (C) 2001-2003 Lutz Roeder. All rights reserved.
// http://www.aisto.com/roeder
// roeder@aisto.com
// ---------------------------------------------------------
namespace Reflector.UserInterface
{
	using System;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.Runtime.InteropServices;
	using System.Windows.Forms;

	internal sealed class NativeMethods
	{
		private NativeMethods()
		{
		}

		public const string TOOLBARCLASSNAME = "ToolbarWindow32";

		public const int WS_CHILD = 0x40000000;
		public const int WS_VISIBLE = 0x10000000;
		public const int WS_CLIPCHILDREN = 0x2000000;
		public const int WS_CLIPSIBLINGS = 0x4000000;
		public const int WS_BORDER = 0x800000;

		public const int CCS_NODIVIDER = 0x40;
		public const int CCS_NORESIZE = 0x4;
		public const int CCS_NOPARENTALIGN = 0x8;

		public const int I_IMAGECALLBACK = -1;
		public const int I_IMAGENONE = -2;

		public const int TBSTYLE_TOOLTIPS = 0x100;
		public const int TBSTYLE_FLAT = 0x800;
		public const int TBSTYLE_LIST = 0x1000;
		public const int TBSTYLE_TRANSPARENT = 0x8000;

		public const int TBSTYLE_EX_DRAWDDARROWS = 0x1;
		public const int TBSTYLE_EX_HIDECLIPPEDBUTTONS = 0x10;
		public const int TBSTYLE_EX_DOUBLEBUFFER = 0x80;

		public const int CDRF_DODEFAULT = 0x0;
		public const int CDRF_SKIPDEFAULT = 0x4;
		public const int CDRF_NOTIFYITEMDRAW = 0x20;
		public const int CDDS_PREPAINT = 0x1;
		public const int CDDS_ITEM = 0x10000;
		public const int CDDS_ITEMPREPAINT = CDDS_ITEM | CDDS_PREPAINT;

		public const int CDIS_HOT = 0x40;
		public const int CDIS_SELECTED = 0x1;
		public const int CDIS_DISABLED = 0x4;

		public const int WM_SETREDRAW = 0x000B;
		public const int WM_CANCELMODE = 0x001F;
		public const int WM_NOTIFY = 0x4e;
		public const int WM_KEYDOWN = 0x100;
		public const int WM_KEYUP = 0x101;
		public const int WM_CHAR = 0x0102;
		public const int WM_SYSKEYDOWN = 0x104;
		public const int WM_SYSKEYUP = 0x105;
		public const int WM_COMMAND = 0x111;
		public const int WM_MENUCHAR = 0x120;
		public const int WM_MOUSEMOVE = 0x200;
		public const int WM_LBUTTONDOWN = 0x201;
		public const int WM_MOUSELAST = 0x20a;
		public const int WM_USER = 0x0400;
		public const int WM_REFLECT = WM_USER + 0x1c00;

		public const int NM_CUSTOMDRAW = -12;

		public const int TTN_NEEDTEXTA = ((0 - 520) - 0);
		public const int TTN_NEEDTEXTW = ((0 - 520) - 10);

		public const int TBN_QUERYINSERT = ((0 - 700) - 6);
		public const int TBN_DROPDOWN = ((0 - 700) - 10);
		public const int TBN_HOTITEMCHANGE = ((0 - 700) - 13);

		public const int TBIF_IMAGE = 0x1;
		public const int TBIF_TEXT = 0x2;
		public const int TBIF_STATE = 0x4;
		public const int TBIF_STYLE = 0x8;
		public const int TBIF_COMMAND = 0x20;
		public const int TBIF_SIZE = 0x40;

		public const int MNC_EXECUTE = 2;

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		internal class INITCOMMONCONTROLSEX
		{
			public int Size = 8;
			public int Flags;
		}

		public const int ICC_BAR_CLASSES = 4;
		public const int ICC_COOL_CLASSES = 0x400;

		[DllImport("comctl32.dll")]
		public static extern bool InitCommonControlsEx(INITCOMMONCONTROLSEX icc);

		[StructLayout(LayoutKind.Sequential)]
		internal struct POINT
		{
			public int x;
			public int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct NMHDR
		{
			public IntPtr hwndFrom;
			public int idFrom;
			public int code;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct NMTOOLBAR
		{
			public NMHDR hdr;
			public int iItem;
			public TBBUTTON tbButton;
			public int cchText;
			public IntPtr pszText;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct NMCUSTOMDRAW
		{
			public NMHDR hdr;
			public int dwDrawStage;
			public IntPtr hdc;
			public RECT rc;
			public int dwItemSpec;
			public int uItemState;
			public int lItemlParam;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct LPNMTBCUSTOMDRAW
		{
			public NMCUSTOMDRAW nmcd;
			public IntPtr hbrMonoDither;
			public IntPtr hbrLines;
			public IntPtr hpenLines;
			public int clrText;
			public int clrMark;
			public int clrTextHighlight;
			public int clrBtnFace;
			public int clrBtnHighlight;
			public int clrHighlightHotTrack;
			public RECT rcText;
			public int nStringBkMode;
			public int nHLStringBkMode;
		}

		public const int TTF_RTLREADING = 0x0004;

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		internal struct TOOLTIPTEXT
		{
			public NMHDR hdr;
			public IntPtr lpszText;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=80)]
			public string szText;
			public IntPtr hinst;
			public int uFlags;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		internal struct TOOLTIPTEXTA
		{
			public NMHDR hdr;
			public IntPtr lpszText;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=80)]
			public string szText;
			public IntPtr hinst;
			public int uFlags;
		}

		public const int TB_PRESSBUTTON = WM_USER + 3;
		public const int TB_INSERTBUTTON = WM_USER + 21;
		public const int TB_BUTTONCOUNT = WM_USER + 24;
		public const int TB_GETITEMRECT = WM_USER + 29;
		public const int TB_BUTTONSTRUCTSIZE = WM_USER + 30;
		public const int TB_SETBUTTONSIZE = WM_USER + 32;
		public const int TB_SETIMAGELIST = WM_USER + 48;
		public const int TB_GETRECT = WM_USER + 51;
		public const int TB_SETBUTTONINFO = WM_USER + 64;
		public const int TB_HITTEST = WM_USER + 69;
		public const int TB_GETHOTITEM = WM_USER + 71;
		public const int TB_SETHOTITEM = WM_USER + 72;
		public const int TB_SETEXTENDEDSTYLE = WM_USER + 84;

		public const int TBSTATE_CHECKED = 0x01;
		public const int TBSTATE_ENABLED = 0x04;
		public const int TBSTATE_HIDDEN = 0x08;

		public const int BTNS_BUTTON = 0;
		public const int BTNS_SEP = 0x1;
		public const int BTNS_DROPDOWN = 0x8;
		public const int BTNS_AUTOSIZE = 0x10;
		public const int BTNS_WHOLEDROPDOWN = 0x80;

		[StructLayout(LayoutKind.Sequential, Pack=1)]
		internal struct TBBUTTON
		{
			public int iBitmap;
			public int idCommand;
			public byte fsState;
			public byte fsStyle;
			public byte bReserved0;
			public byte bReserved1;
			public int dwData;
			public int iString;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		internal struct TBBUTTONINFO
		{
			public int cbSize;
			public int dwMask;
			public int idCommand;
			public int iImage;
			public byte fsState;
			public byte fsStyle;
			public short cx;
			public IntPtr lParam;
			public IntPtr pszText;
			public int cchText;
		}

		[DllImport("user32.dll", ExactSpelling=true, CharSet=CharSet.Auto)]
		public static extern IntPtr GetParent(IntPtr hWnd);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern void SendMessage(IntPtr hWnd, int msg, int wParam, ref RECT lParam);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, ref POINT lParam);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern void SendMessage(IntPtr hWnd, int msg, int wParam, ref TBBUTTON lParam);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern void SendMessage(IntPtr hWnd, int msg, int wParam, ref TBBUTTONINFO lParam);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern void SendMessage(IntPtr hWnd, int msg, int wParam, ref REBARBANDINFO lParam);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern IntPtr PostMessage(IntPtr hWnd, int msg, int wParam, int lParam);

		[DllImport("kernel32.dll", ExactSpelling=true, CharSet=CharSet.Auto)]
		public static extern int GetCurrentThreadId();

		internal delegate IntPtr HookProc(int code, IntPtr param1, IntPtr param2);

		public const int WH_MSGFILTER = -1;
		public const int MSGF_MENU = 2;

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern IntPtr SetWindowsHookEx(int hookid, HookProc pfnhook, IntPtr hinst, int threadid);

		[DllImport("user32.dll", CharSet=CharSet.Auto, ExactSpelling=true)]
		public static extern bool UnhookWindowsHookEx(IntPtr hhook);

		[DllImport("user32.dll", CharSet=CharSet.Auto, ExactSpelling=true)]
		public static extern IntPtr CallNextHookEx(IntPtr hhook, int code, IntPtr wparam, IntPtr lparam);

		[StructLayout(LayoutKind.Sequential)]
		internal struct MSG
		{
			public IntPtr hwnd;
			public int message;
			public IntPtr wParam;
			public IntPtr lParam;
			public int time;
			public int pt_x;
			public int pt_y;
		}

		public const string REBARCLASSNAME = "ReBarWindow32";

		public const int RBS_VARHEIGHT = 0x200;
		public const int RBS_BANDBORDERS = 0x400;
		public const int RBS_AUTOSIZE = 0x2000;

		public const int RBN_FIRST = -831;
		public const int RBN_HEIGHTCHANGE = RBN_FIRST - 0;
		public const int RBN_AUTOSIZE = RBN_FIRST - 3;
		public const int RBN_CHEVRONPUSHED = RBN_FIRST - 10;

		public const int RB_SETBANDINFO = WM_USER + 6;
		public const int RB_GETRECT = WM_USER + 9;
		public const int RB_INSERTBAND = WM_USER + 10;
		public const int RB_GETBARHEIGHT = WM_USER + 27;

		[StructLayout(LayoutKind.Sequential)]
		internal struct REBARBANDINFO
		{
			public int cbSize;
			public int fMask;
			public int fStyle;
			public int clrFore;
			public int clrBack;
			public IntPtr lpText;
			public int cch;
			public int iImage;
			public IntPtr hwndChild;
			public int cxMinChild;
			public int cyMinChild;
			public int cx;
			public IntPtr hbmBack;
			public int wID;
			public int cyChild;
			public int cyMaxChild;
			public int cyIntegral;
			public int cxIdeal;
			public int lParam;
			public int cxHeader;
		}

		public const int RBBIM_CHILD = 0x10;
		public const int RBBIM_CHILDSIZE = 0x20;
		public const int RBBIM_STYLE = 0x1;
		public const int RBBIM_ID = 0x100;
		public const int RBBIM_SIZE = 0x40;
		public const int RBBIM_IDEALSIZE = 0x200;
		public const int RBBIM_TEXT = 0x4;

		public const int RBBS_BREAK = 0x1;
		public const int RBBS_CHILDEDGE = 0x4;
		public const int RBBS_FIXEDBMP = 0x20;
		public const int RBBS_GRIPPERALWAYS = 0x80;
		public const int RBBS_USECHEVRON = 0x200;

		[StructLayout(LayoutKind.Sequential)]
		internal struct NMREBARCHEVRON
		{
			public NMHDR hdr;
			public int uBand;
			public int wID;
			public int lParam;
			public RECT rc;
			public int lParamNM;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct IMAGELISTDRAWPARAMS
		{
			public int cbSize;
			public IntPtr himl;
			public int i;
			public IntPtr hdcDst;
			public int x;
			public int y;
			public int cx;
			public int cy;
			public int xBitmap;
			public int yBitmap;
			public int rgbBk;
			public int rgbFg;
			public int fStyle;
			public int dwRop;
			public int fState;
			public int Frame;
			public int crEffect;
		}

		public const int ILD_TRANSPARENT = 0x1;
		public const int ILS_SATURATE = 0x4;

		[DllImport("comctl32.dll", CharSet=CharSet.Auto)]
		public static extern bool ImageList_DrawIndirect(ref IMAGELISTDRAWPARAMS pimldp);

		[StructLayout(LayoutKind.Sequential)]
		internal struct DLLVERSIONINFO
		{
			public int cbSize;
			public int dwMajorVersion;
			public int dwMinorVersion;
			public int dwBuildNumber;
			public int dwPlatformID;
		}

		[DllImport("comctl32.dll")]
		public extern static int DllGetVersion(ref DLLVERSIONINFO dvi);

		public const int SPI_GETFLATMENU = 0x1022;

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public extern static int SystemParametersInfo(int nAction, int nParam, ref int value, int ignore);

		[DllImport("user32.dll", ExactSpelling=true, CharSet=CharSet.Auto)]
		public static extern bool MessageBeep(int type);

		public const int HWND_NOTOPMOST = -2;
		public const int SW_SHOWNORMAL = 1;
		public const int SWP_NOACTIVATE = 0x0010;
		public const int SWP_NOMOVE = 0x0002;
		public const int SWP_NOSIZE = 0x0001;
		public const int SWP_NOZORDER = 0x0004;
		public const int SWP_SHOWWINDOW = 0x0040;

		[DllImport("user32.dll", ExactSpelling=true, CharSet=CharSet.Auto)]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);


		public static void DrawImage(Graphics graphics, Image image, Point point, bool disabled)
		{
			if (!disabled)
			{
				Rectangle destination = new Rectangle(point, image.Size);
				graphics.DrawImage(image, destination, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
			}
			else
			{
				// Painting a disabled gray scale image is done using ILS_SATURATE on WinXP.
				// This code emulates that behaviour if comctl32 version 6 is not availble.
				NativeMethods.DLLVERSIONINFO dvi = new NativeMethods.DLLVERSIONINFO();
				dvi.cbSize = Marshal.SizeOf(typeof(NativeMethods.DLLVERSIONINFO));
				NativeMethods.DllGetVersion(ref dvi);
				if (dvi.dwMajorVersion < 6)
				{
					ImageAttributes attributes = new ImageAttributes();
					Rectangle destination = new Rectangle(point, image.Size);
					float[][] matrix = new float[5][];
					matrix[0] = new float[] { 0.2222f, 0.2222f, 0.2222f, 0.0000f, 0.0000f };
					matrix[1] = new float[] { 0.2222f, 0.2222f, 0.2222f, 0.0000f, 0.0000f };
					matrix[2] = new float[] { 0.2222f, 0.2222f, 0.2222f, 0.0000f, 0.0000f };
					matrix[3] = new float[] { 0.3333f, 0.3333f, 0.3333f, 0.7500f, 0.0000f };
					matrix[4] = new float[] { 0.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f };
					attributes.SetColorMatrix(new ColorMatrix(matrix));
					graphics.DrawImage(image, destination, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
				}
				else
				{
					ImageList imageList = new ImageList();
					imageList.ImageSize = image.Size;
					imageList.ColorDepth = ColorDepth.Depth32Bit;
					imageList.Images.Add(image);

					IntPtr hdc = graphics.GetHdc();
					NativeMethods.IMAGELISTDRAWPARAMS ildp = new NativeMethods.IMAGELISTDRAWPARAMS();
					ildp.cbSize = Marshal.SizeOf(typeof(NativeMethods.IMAGELISTDRAWPARAMS));
					ildp.himl = imageList.Handle;
					ildp.i = 0; // image index
					ildp.hdcDst = hdc;
					ildp.x = point.X;
					ildp.y = point.Y;
					ildp.cx = 0;
					ildp.cy = 0;
					ildp.xBitmap = 0;
					ildp.yBitmap = 0;
					ildp.fStyle = NativeMethods.ILD_TRANSPARENT;
					ildp.fState = NativeMethods.ILS_SATURATE;
					ildp.Frame = -100;
					NativeMethods.ImageList_DrawIndirect(ref ildp);
					graphics.ReleaseHdc(hdc);
				}
			}
		}
	}
}

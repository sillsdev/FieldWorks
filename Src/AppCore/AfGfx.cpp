/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: AfGfx.cpp
Responsibility: Shon Katzenberger
Last reviewed:

	Graphics utilities.
-------------------------------------------------------------------------------*//*:End Ignore*/

#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE


#define DEBUG_AFGDI 1
#undef DEBUG_AFGDI


struct ColorMapEntry
{
	// Use DWORD instead of RGBQUAD so we can compare two RGBQUADs easily.
	DWORD rgbqSrc;
	int clrDst;
};


const ColorMapEntry g_rgcme[] =
{
	// Mapping from color in DIB to system color.
	{ 0x00000000,  COLOR_BTNTEXT },       // Black.
	{ 0x00808080,  COLOR_BTNSHADOW },     // Dark gray.
	{ 0x00C0C0C0,  COLOR_BTNFACE },       // Bright gray.
	{ 0x00FFFFFF,  COLOR_BTNHIGHLIGHT }   // White.
};


/*----------------------------------------------------------------------------------------------
	Load a bitmap from a resource.
----------------------------------------------------------------------------------------------*/
HBITMAP AfGfx::LoadSysColorBitmap(int rid)
{
	// TODO ShonK: Make this throw an exception on failure and handle clean up.

	HMODULE hmod = ModuleEntry::GetModuleHandle();
	int icme;

	// REVIEW ShonK: Should we always create a screen compatible bitmap or does it matter?

	for (icme = 0; icme < SizeOfArray(g_rgcme); icme++)
	{
		if (g_rgcme[icme].rgbqSrc != ::GetSysColor(g_rgcme[icme].clrDst))
			goto LNeedToMap;
	}
	return AfGdi::LoadBitmap(hmod, MAKEINTRESOURCE(rid));

LNeedToMap:
	HRSRC hrsrc = ::FindResource(hmod, MAKEINTRESOURCE(rid), RT_BITMAP);
	if (!hrsrc)
		return NULL;

	HGLOBAL hglb = ::LoadResource(hmod, hrsrc);
	if (!hglb)
		return NULL;

	BITMAPINFOHEADER * pbihRsrc = (BITMAPINFOHEADER *)::LockResource(hglb);
	if (!pbihRsrc)
		return NULL;

	const int kcrgbq = 16;

	if (pbihRsrc->biBitCount != 4)
	{
		// This should be a 4 bit DIB (16 colors).
		return NULL;
	}

	// Make copy of BITMAPINFOHEADER so we can modify the color table.
	int cb = pbihRsrc->biSize + kcrgbq * isizeof(RGBQUAD);
	Vector<byte> vbT;
	vbT.Resize(cb);
	BITMAPINFOHEADER * pbih = (BITMAPINFOHEADER *)vbT.Begin();
	CopyBytes(pbihRsrc, pbih, cb);

	// Color table is in RGBQUAD DIB format.
	DWORD * prgrgbq = (DWORD *)((byte *)pbih + pbih->biSize);
	int irgbq;

	for (irgbq = 0; irgbq < kcrgbq; irgbq++)
	{
		// Look for matching RGBQUAD color in original.
		for (icme = 0; icme < SizeOfArray(g_rgcme); icme++)
		{
			if (prgrgbq[irgbq] == g_rgcme[icme].rgbqSrc)
			{
				// GetSysColor returns an RGB value: 0x00bbggrr.
				// A bitmap contains RGBQUAD values: 0x00rrggbb.
				DWORD clr = ::GetSysColor(g_rgcme[icme].clrDst);
				prgrgbq[irgbq] = (clr & 0x0000FF00) | ((clr & 0x000000FF) << 16) |
					((clr & 0x00FF0000) >> 16);
				break;
			}
		}
	}

	int dxs = (int)pbih->biWidth;
	int dys = (int)pbih->biHeight;
	HDC hdcScrn = ::GetDC(NULL);
	Assert(hdcScrn);
	HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdcScrn, dxs, dys);

	if (hbmp)
	{
		HDC hdcMem = AfGdi::CreateCompatibleDC(hdcScrn);
		HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);

		byte * prgb;
		prgb = (byte *)(pbihRsrc + 1);
		prgb += kcrgbq * isizeof(RGBQUAD);

		::StretchDIBits(hdcMem, 0, 0, dxs, dys, 0, 0, dxs, dys,
			prgb, (BITMAPINFO *)pbih, DIB_RGB_COLORS, SRCCOPY);
		AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);

//		::DeleteObject(hbmp);  Should NOT delete here ... caller is responsible to delete.

		BOOL fSuccess;
		fSuccess = AfGdi::DeleteDC(hdcMem);
		Assert(fSuccess);
	}
	int iSuccess;
	iSuccess = ::ReleaseDC(NULL, hdcScrn);
	Assert(iSuccess);

	return hbmp;
}


/*----------------------------------------------------------------------------------------------
	This method fills a rectangle with the given color, making sure it matches a color in the
	current palette so the color is drawn accurately.
----------------------------------------------------------------------------------------------*/
void AfGfx::FillSolidRect(HDC hdc, const Rect & rc, COLORREF clr, bool fUsePalette)
{
	COLORREF clrOld;
	if (fUsePalette)
	{
		clrOld = ::SetBkColor(hdc,
			PALETTERGB(GetRValue(clr), GetGValue(clr), GetBValue(clr)));
	}
	else
	{
		clrOld = ::SetBkColor(hdc, clr);
	}
	::ExtTextOut(hdc, 0, 0, ETO_OPAQUE, &rc, NULL, 0, NULL);
	::SetBkColor(hdc, clrOld);
}


/*----------------------------------------------------------------------------------------------
	Create a Solid Brush
----------------------------------------------------------------------------------------------*/
HBRUSH AfGfx::CreateSolidBrush(COLORREF clr)
{
	return AfGdi::CreateSolidBrush(PALETTERGB(GetRValue(clr), GetGValue(clr), GetBValue(clr)));
}


/*----------------------------------------------------------------------------------------------
	Provides an easy way to draw a bitmap into a DC
----------------------------------------------------------------------------------------------*/
void AfGfx::DrawBitMap(HDC hdc, HBITMAP hbmp, Rect & rcDst, int xsSrc, int ysSrc,
		DWORD dwRop)
{
	SmartDc sdc;
	sdc.CreateCompat(hdc);

	HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(sdc, hbmp);

	::BitBlt(hdc, rcDst.left, rcDst.top, rcDst.Width(), rcDst.Height(),
		sdc, xsSrc, ysSrc, dwRop);

	AfGdi::SelectObjectBitmap(sdc, hbmpOld, AfGdi::OLD);
}


/*----------------------------------------------------------------------------------------------
	This method checks the top-left and bottom-right points in the rectangle to make sure they
	are both visible. If they are not both visible, it moves the rectangle so that they are
	visible. If the rectangle is too big to fit on the screen, it will be resized.
----------------------------------------------------------------------------------------------*/
bool AfGfx::EnsureVisibleRect(Rect & rc)
{
	// TODO DarrellZ: Call this function whenever the screen resolution changes.
	// We don't have to call this when we restore after minimizing as long as we catch the
	// screen resolution change while we are minimized.

	Rect rcWork;
	if (::GetSystemMetrics(SM_CMONITORS) > 1)
	{
		// Note: (JohnT): in Perforce revision 1 of this file there is code which attempts
		// to use LoadLibrary and GetProcAddress so that this code will run on W95 or WinNT.
		// (Except it won't really, it asserts if it can't get the functions.)
		// MonitorFromPoint and GetMonitorInfo are only implemented on W98 and W2000 and later.
		// This code works in a Debug build but not in a release build. The release build
		// corrupts the stack and leads to a horrible crash with no useful diagnostic info.
		// Since we aren't aiming to support operating systems without this capability anyway,
		// I've elected to just use the functions directly.
		// (KenZ): Actually, the release build seems to work fine on some computers, but not
		// others. For example, it doesn't work on a Dell PWS530 workstation with Intel Xeon
		// processor and dual monitors running WinXP Pro. It crashed on the call to
		// MonitorFromPoint. I eliminated FitRectOnScreen which was using the GetProcAddress
		// approach and used this method instead to get around the problem.

		// See if the top left and bottom right corners of the rectangle are on a monitor.
		Point pt = rc.TopLeft();
		HMONITOR hmonTL = MonitorFromPoint(pt, MONITOR_DEFAULTTONULL);
		pt = rc.BottomRight();
		HMONITOR hmonBR = MonitorFromPoint(pt, MONITOR_DEFAULTTONULL);
		pt.Set(rc.right, rc.top);
		HMONITOR hmonTR = MonitorFromPoint(pt, MONITOR_DEFAULTTONULL);
		pt.Set(rc.left, rc.bottom);
		HMONITOR hmonBL = MonitorFromPoint(pt, MONITOR_DEFAULTTONULL);

		// If both points are on the same monitor, we are done.
		if (hmonTL && hmonBR && hmonTL == hmonBR)
			return true;

		MONITORINFO mi = { isizeof(mi) };

		if (hmonTL)
		{
			// The top left corner of the rectangle is on a monitor.
			if (!GetMonitorInfo(hmonTL, &mi))
				return false;

			// Move the rectangle so that the bottom right corner fits on the monitor.
			int xpRight = NMin(rc.right, mi.rcWork.right);
			int ypBottom = NMin(rc.bottom, mi.rcWork.bottom);
			rc.Offset(xpRight - rc.right, ypBottom - rc.bottom);

			// If the top left point is now off the monitor, set it to the top left
			// coordinate of the monitor.

			// Note: the obvious contraction of this code to
			// HMONITOR hmonT = pfnMonitorFromPoint(rc.TopLeft(), MONITOR_DEFAULTTONULL);
			// Causes a run-time crash with a bad stack corruption. I don't know why separating
			// the parts of the statement fixes things but it does.

			Point pt = rc.TopLeft();
			HMONITOR hmonT = MonitorFromPoint(pt, MONITOR_DEFAULTTONULL);
			if (hmonT != hmonTL)
			{
				rc.top = mi.rcWork.top;
				rc.left = mi.rcWork.left;
			}

			return true;
		}

		if (hmonBR)
		{
			// The bottom right corner of the rectangle is on a monitor.
			if (!GetMonitorInfo(hmonBR, &mi))
				return false;

			// Move the rectangle so that the top left corner fits on the monitor.
			int xpLeft = NMax(rc.left, mi.rcWork.left);
			int ypTop = NMax(rc.top, mi.rcWork.top);
			rc.Offset(xpLeft - rc.left, ypTop - rc.top);

			// If the bottom right point is now off the monitor, set it to the bottom right
			// coordinate of the monitor.
			Point pt = rc.BottomRight();
			HMONITOR hmonT = MonitorFromPoint(pt, MONITOR_DEFAULTTONULL);
			if (hmonT != hmonBR)
			{
				rc.bottom = mi.rcWork.bottom;
				rc.right = mi.rcWork.right;
			}

			return true;
		}

		if (hmonTR)
		{
			// The top right corner of the rectangle is on a monitor.
			if (!GetMonitorInfo(hmonTR, &mi))
				return false;

			// Move the rectangle so that the bottom left corner fits on the monitor.
			int xpLeft = NMax(rc.left, mi.rcWork.left);
			int ypBottom = NMin(rc.bottom, mi.rcWork.bottom);
			rc.Offset(xpLeft - rc.left, ypBottom - rc.bottom);

			// If the top right point is now off the monitor, set it to the top right
			// coordinate of the monitor.

			Point pt(rc.right, rc.top);
			HMONITOR hmonT = MonitorFromPoint(pt, MONITOR_DEFAULTTONULL);
			if (hmonT != hmonTR)
			{
				rc.top = mi.rcWork.top;
				rc.right = mi.rcWork.right;
			}

			return true;
		}

		if (hmonBL)
		{
			// The bottom left corner of the rectangle is on a monitor.
			if (!GetMonitorInfo(hmonBL, &mi))
				return false;

			// Move the rectangle so that the top right corner fits on the monitor.
			int xpRight = NMin(rc.right, mi.rcWork.right);
			int ypTop = NMax(rc.top, mi.rcWork.top);
			rc.Offset(xpRight - rc.right, ypTop - rc.top);

			// If the bottom left point is now off the monitor, set it to the bottom left
			// coordinate of the monitor.
			Point pt(rc.left, rc.bottom);
			HMONITOR hmonT = MonitorFromPoint(pt, MONITOR_DEFAULTTONULL);
			if (hmonT != hmonBL)
			{
				rc.bottom = mi.rcWork.bottom;
				rc.left = mi.rcWork.left;
			}

			return true;
		}

		// No point is on any monitor, so choose a default size on the primary monitor.
		// ENHANCE DarrellZ: Modify this so that the size stays the same if possible.
		// For now, fall through to the next part.
	}

	if (::SystemParametersInfo(SPI_GETWORKAREA, 0, &rcWork, false))
	{
		// Move the rectangle so the bottom right corner is on the monitor.
		int xpRight = NMin(rc.right, rcWork.right);
		int ypBottom = NMin(rc.bottom, rcWork.bottom);
		rc.Offset(xpRight - rc.right, ypBottom - rc.bottom);

		// Move the rectangle so the top left corner is on the monitor.
		int xpLeft = NMax(rc.left, rcWork.left);
		int ypTop = NMax(rc.top, rcWork.top);
		rc.Offset(xpLeft - rc.left, ypTop - rc.top);

		// Resize the rectangle if required to get the bottom right corner on the monitor.
		rc.right = NMin(rc.right , rcWork.right);
		rc.bottom = NMin(rc.bottom, rcWork.bottom);
		return true;
	}

	return false;
}


/***********************************************************************************************
	AfGdi methods
	AfGDI::  device context methods.
	AfGDI::  fonts methods.
	AfGDI::  bitmap methods.
	AfGDI::  image list methods.
***********************************************************************************************/

#ifdef DEBUG_AFGDI
int AfGdi::s_cDCs = 0;
BOOL AfGdi::s_fShowDCs = false;

int AfGdi::s_cFonts = 0;
BOOL AfGdi::s_fShowFonts = false;

int AfGdi::s_cBitmaps = 0;
BOOL AfGdi::s_fShowBitmaps = false;

int AfGdi::s_cBrushes = 0;
BOOL AfGdi::s_fShowBrushes = false;

int AfGdi::s_cImageLists = 0;
BOOL AfGdi::s_fShowImageLists = false;
#endif

/*----------------------------------------------------------------------------------------------
	AfGDI::  device context methods.
----------------------------------------------------------------------------------------------*/

HDC AfGdi::CreateDC(
	LPCTSTR lpszDriver,        // driver name
	LPCTSTR lpszDevice,        // device name
	LPCTSTR lpszOutput,        // not used; should be NULL
	CONST DEVMODE* lpInitData  // optional printer data
)
{
	HDC hdcNew = ::CreateDC(lpszDriver, lpszDevice, lpszOutput, lpInitData);

#ifdef DEBUG_AFGDI
	if (hdcNew)
		s_cDCs++;
	if (s_fShowDCs)
	{
		StrAnsi sta;
		if (hdcNew)
			sta.Format("AfGdi::CreateDC:  #%d:  hdcNew=0x%x.\n", s_cDCs, hdcNew);
		else
			sta.Format("AfGdi::CreateDC:  #%d:  hdcNew=0x%x FAILED.\n", s_cDCs, hdcNew);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return hdcNew;
};


HDC AfGdi::CreateCompatibleDC(HDC hdc)
{
	HDC hdcNew = ::CreateCompatibleDC(hdc);

#ifdef DEBUG_AFGDI
	if (hdcNew)
		s_cDCs++;
	if (s_fShowDCs)
	{
		StrAnsi sta;
		if (hdcNew)
			sta.Format("AfGdi::CreateCompatibleDC:  #%d:  hdc=0x%x; hdcNew=0x%x.\n",
				s_cDCs, hdc, hdcNew);
		else
			sta.Format("AfGdi::CreateCompatibleDC:  #%d:  hdc=0x%x; hdcNew=0x%x FAILED.\n",
				s_cDCs, hdc, hdcNew);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return hdcNew;
};


BOOL AfGdi::DeleteDC(HDC hdc)
{
#ifdef DEBUG_AFGDI
	HPEN         pen;    pen    = (HPEN)        GetCurrentObject(hdc, OBJ_PEN);
	HBRUSH       brush;  brush  = (HBRUSH)      GetCurrentObject(hdc, OBJ_BRUSH);
	HGDIOBJ    dc;       dc       =               GetCurrentObject(hdc, OBJ_DC);
	HGDIOBJ    mdc;      mdc      =               GetCurrentObject(hdc, OBJ_METADC);
	HPALETTE     pal;    pal    = (HPALETTE)    GetCurrentObject(hdc, OBJ_PAL);
	HFONT        font;   font   = (HFONT)       GetCurrentObject(hdc, OBJ_FONT);
	HBITMAP      bitmap; bitmap = (HBITMAP)     GetCurrentObject(hdc, OBJ_BITMAP);
	HRGN         rgn;    rgn    = (HRGN)        GetCurrentObject(hdc, OBJ_REGION);
	HMETAFILE    mf;     mf     = (HMETAFILE)   GetCurrentObject(hdc, OBJ_METAFILE);
	HGDIOBJ    memdc;    memdc    =               GetCurrentObject(hdc, OBJ_MEMDC);
	HGDIOBJ    ep;       ep       =               GetCurrentObject(hdc, OBJ_EXTPEN);
	HGDIOBJ    emdc;     emdc     =               GetCurrentObject(hdc, OBJ_ENHMETADC);
	HENHMETAFILE emf;    emf    = (HENHMETAFILE)GetCurrentObject(hdc, OBJ_ENHMETAFILE);
	HCOLORSPACE  csp;    csp    = (HCOLORSPACE) GetCurrentObject(hdc, OBJ_COLORSPACE);
#endif

#ifdef DEBUG_AFGDI
	if (s_fShowDCs)
	{
		StrAnsi sta;
		sta.Format("AfGdi::DeleteDC:  #%d:  hdc=0x%x", s_cDCs, hdc);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	BOOL fSuccess = ::DeleteDC(hdc);

#ifdef DEBUG_AFGDI
	if (s_fShowDCs)
	{
		StrAnsi sta;
		if (fSuccess)
			sta.Format(".\n");
		else
			sta.Format(" FAILED.\n");
		::OutputDebugStringA(sta.Chars());
	}
	if (fSuccess)
		s_cDCs--;

#endif

	return fSuccess;
};


HDC AfGdi::GetDC(HWND hWnd)
{
	HDC hdcNew = ::GetDC(hWnd);

#ifdef DEBUG_AFGDI
	if (hdcNew)
		s_cDCs++;
	if (s_fShowDCs)
	{
		OutputDC(hdcNew);
		StrAnsi sta;
		if (hdcNew)
			sta.Format("AfGdi::GetDC:  #%d:  hWnd=0x%x; hdcNew=0x%x.\n",
				s_cDCs, hWnd, hdcNew);
		else
			sta.Format("AfGdi::GetDC:  #%d:  hWnd=0x%x; hdcNew=0x%x FAILED.\n",
				s_cDCs, hWnd, hdcNew);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return hdcNew;
};


int AfGdi::ReleaseDC(
	HWND hWnd, // handle to window
	HDC hdc) // handle to DC
{
#ifdef DEBUG_AFGDI
	HPEN         pen;    pen    = (HPEN)        GetCurrentObject(hdc, OBJ_PEN);
	HBRUSH       brush;  brush  = (HBRUSH)      GetCurrentObject(hdc, OBJ_BRUSH);
	HGDIOBJ    dc;       dc       =               GetCurrentObject(hdc, OBJ_DC);
	HGDIOBJ    mdc;      mdc      =               GetCurrentObject(hdc, OBJ_METADC);
	HPALETTE     pal;    pal    = (HPALETTE)    GetCurrentObject(hdc, OBJ_PAL);
	HFONT        font;   font   = (HFONT)       GetCurrentObject(hdc, OBJ_FONT);
	HBITMAP      bitmap; bitmap = (HBITMAP)     GetCurrentObject(hdc, OBJ_BITMAP);
	HRGN         rgn;    rgn    = (HRGN)        GetCurrentObject(hdc, OBJ_REGION);
	HMETAFILE    mf;     mf     = (HMETAFILE)   GetCurrentObject(hdc, OBJ_METAFILE);
	HGDIOBJ    memdc;    memdc    =               GetCurrentObject(hdc, OBJ_MEMDC);
	HGDIOBJ    ep;       ep       =               GetCurrentObject(hdc, OBJ_EXTPEN);
	HGDIOBJ    emdc;     emdc     =               GetCurrentObject(hdc, OBJ_ENHMETADC);
	HENHMETAFILE emf;    emf    = (HENHMETAFILE)GetCurrentObject(hdc, OBJ_ENHMETAFILE);
	HCOLORSPACE  csp;    csp    = (HCOLORSPACE) GetCurrentObject(hdc, OBJ_COLORSPACE);

	TCHAR className[256];
	int iReturn;
	iReturn = GetClassName(hWnd, className, 255);

	DWORD dword;
	dword = GetClassLong(hWnd, GCL_STYLE);
#endif

#ifdef DEBUG_AFGDI
	if (s_fShowDCs)
	{
		StrAnsi sta;
		sta.Format("AfGdi::ReleaseDC:  #%d:  hWnd=0x%x; hdc=0x%x; style=0x%x", s_cDCs, hWnd, hdc, dword);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	int iSuccess = ::ReleaseDC(hWnd, hdc);

#ifdef DEBUG_AFGDI
	if (s_fShowDCs)
	{
		StrAnsi sta;
		if (iSuccess)
			sta.Format(".\n");
		else
			sta.Format(" FAILED.\n");
		::OutputDebugStringA(sta.Chars());
	}
	if (iSuccess)
		s_cDCs--;
#endif

	return iSuccess;
};


void AfGdi::OutputDC(HDC hdc)
{
#ifdef DEBUG_AFGDI
	HPEN         pen;    pen    = (HPEN)        GetCurrentObject(hdc, OBJ_PEN);
	HBRUSH       brush;  brush  = (HBRUSH)      GetCurrentObject(hdc, OBJ_BRUSH);
	HGDIOBJ    dc;       dc       =               GetCurrentObject(hdc, OBJ_DC);
	HGDIOBJ    mdc;      mdc      =               GetCurrentObject(hdc, OBJ_METADC);
	HPALETTE     pal;    pal    = (HPALETTE)    GetCurrentObject(hdc, OBJ_PAL);
	HFONT        font;   font   = (HFONT)       GetCurrentObject(hdc, OBJ_FONT);
	HBITMAP      bitmap; bitmap = (HBITMAP)     GetCurrentObject(hdc, OBJ_BITMAP);
	HRGN         rgn;    rgn    = (HRGN)        GetCurrentObject(hdc, OBJ_REGION);
	HMETAFILE    mf;     mf     = (HMETAFILE)   GetCurrentObject(hdc, OBJ_METAFILE);
	HGDIOBJ    memdc;    memdc    =               GetCurrentObject(hdc, OBJ_MEMDC);
	HGDIOBJ    ep;       ep       =               GetCurrentObject(hdc, OBJ_EXTPEN);
	HGDIOBJ    emdc;     emdc     =               GetCurrentObject(hdc, OBJ_ENHMETADC);
	HENHMETAFILE emf;    emf    = (HENHMETAFILE)GetCurrentObject(hdc, OBJ_ENHMETAFILE);
	HCOLORSPACE  csp;    csp    = (HCOLORSPACE) GetCurrentObject(hdc, OBJ_COLORSPACE);


	StrAnsi sta;
	if (hdc)
	{
		sta.Format("hdc=%u(%x).\n", hdc, hdc);
		::OutputDebugStringA(sta.Chars());
	}
	if (pen)
	{
		sta.Format("pen=%u(%x).\n", pen, pen);
		::OutputDebugStringA(sta.Chars());
	}
	if (brush)
	{
		sta.Format("brush=%u(%x).\n", brush, brush);
		::OutputDebugStringA(sta.Chars());
	}
	if (dc)
	{
		sta.Format("dc=%u(%x).\n", dc, dc);
		::OutputDebugStringA(sta.Chars());
	}
	if (mdc)
	{
		sta.Format("mdc=%u(%x).\n", mdc, mdc);
		::OutputDebugStringA(sta.Chars());
	}
	if (pal)
	{
		sta.Format("pal=%u(%x).\n", pal, pal);
		::OutputDebugStringA(sta.Chars());
	}
	if (font)
	{
		sta.Format("font=%u(%x).\n", font, font);
		::OutputDebugStringA(sta.Chars());
	}
	if (bitmap)
	{
		sta.Format("bitmap=%u(%x).\n", bitmap, bitmap);
		::OutputDebugStringA(sta.Chars());
	}
	if (rgn)
	{
		sta.Format("rgn=%u(%x).\n", rgn, rgn);
		::OutputDebugStringA(sta.Chars());
	}
	if (mf)
	{
		sta.Format("mf=%u(%x).\n", mf, mf);
		::OutputDebugStringA(sta.Chars());
	}
	if (memdc)
	{
		sta.Format("memdc=%u(%x).\n", memdc, memdc);
		::OutputDebugStringA(sta.Chars());
	}
	if (ep)
	{
		sta.Format("ep=%u(%x).\n", ep, ep);
		::OutputDebugStringA(sta.Chars());
	}
	if (emdc)
	{
		sta.Format("emdc=%u(%x).\n", emdc, emdc);
		::OutputDebugStringA(sta.Chars());
	}
	if (emf)
	{
		sta.Format("emf=%u(%x).\n", emf, emf);
		::OutputDebugStringA(sta.Chars());
	}
	if (csp)
	{
		sta.Format("csp=%u(%x).\n", csp, csp);
		::OutputDebugStringA(sta.Chars());
	}
#endif
};


/*----------------------------------------------------------------------------------------------
	AfGDI::  fonts methods.
----------------------------------------------------------------------------------------------*/

HFONT AfGdi::CreateFont(
	int nHeight,               // height of font
	int nWidth,                // average character width
	int nEscapement,           // angle of escapement
	int nOrientation,          // base-line orientation angle
	int fnWeight,              // font weight
	DWORD fdwItalic,           // italic attribute option
	DWORD fdwUnderline,        // underline attribute option
	DWORD fdwStrikeOut,        // strikeout attribute option
	DWORD fdwCharSet,          // character set identifier
	DWORD fdwOutputPrecision,  // output precision
	DWORD fdwClipPrecision,    // clipping precision
	DWORD fdwQuality,          // output quality
	DWORD fdwPitchAndFamily,   // pitch and family
	LPCTSTR lpszFace           // typeface name
)
{
	HFONT font = ::CreateFont(
		nHeight,             // height of font
		nWidth,              // average character width
		nEscapement,         // angle of escapement
		nOrientation,        // base-line orientation angle
		fnWeight,            // font weight
		fdwItalic,           // italic attribute option
		fdwUnderline,        // underline attribute option
		fdwStrikeOut,        // strikeout attribute option
		fdwCharSet,          // character set identifier
		fdwOutputPrecision,  // output precision
		fdwClipPrecision,    // clipping precision
		fdwQuality,          // output quality
		fdwPitchAndFamily,   // pitch and family
		lpszFace             // typeface name
	);

#ifdef DEBUG_AFGDI
	if (font)
		s_cFonts++;
	if (s_fShowFonts)
	{
		StrUni stuModule = ModuleEntry::GetModulePathName();
		StrAnsi sta;
		if (font)
			sta.Format("AfGdi::CreateFont:  #%d:  font=0x%x; %S.\n",
				s_cFonts, font, stuModule.Chars());
		else
			sta.Format("AfGdi::CreateFont:  #%d:  font=0x%x FAILED; %S.\n",
				s_cFonts, font, stuModule.Chars());
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return font;
}


HFONT AfGdi::CreateFontIndirect(const LOGFONT * plf)
{
	HFONT font = ::CreateFontIndirect(plf);

#ifdef DEBUG_AFGDI
	if (font)
		s_cFonts++;
	if (s_fShowFonts)
	{
		StrUni stuModule = ModuleEntry::GetModulePathName();
		StrAnsi sta;
		if (font)
			sta.Format("AfGdi::CreateFontIndirect:  #%d:  font=0x%x; %S.\n",
				s_cFonts, font, stuModule.Chars());
		else
			sta.Format("AfGdi::CreateFontIndirect:  #%d:  font=0x%x FAILED; %S.\n",
				s_cFonts, font, stuModule.Chars());
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return font;
}


HFONT AfGdi::GetStockObjectFont(int iFont)
{
	HFONT font = (HFONT)::GetStockObject(iFont);

#ifdef DEBUG_AFGDI
	if (font)
		s_cFonts++;
	if (s_fShowFonts)
	{
		StrUni stuModule = ModuleEntry::GetModulePathName();
		StrAnsi sta;
		if (font)
			sta.Format("AfGdi::GetStockObjectFont:  #%d:  font=0x%x; %S.\n",
				s_cFonts, font, stuModule.Chars());
		else
			sta.Format("AfGdi::GetStockObjectFont:  #%d:  font=0x%x FAILED; %S.\n",
				s_cFonts, font, stuModule.Chars());
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return font;
}


HFONT AfGdi::SelectObjectFont(HDC hdc, HFONT font, SelType type)
{
	HFONT fontOld = (HFONT)::SelectObject(hdc, font);

#ifdef DEBUG_AFGDI
	if (s_fShowFonts)
	{
		StrUni stuModule = ModuleEntry::GetModulePathName();
		StrAnsi sta;
		sta.Format("AfGdi::SelectObjectFont:  hdc=0x%x; font=0x%x; fontOld=0x%x ",
			hdc, font, fontOld);
		::OutputDebugStringA(sta.Chars());

		if (type == AfGdi::NEW)
			sta.Format("NEW");
		else if (type == AfGdi::OLD)
			sta.Format("OLD");
		else if (type == AfGdi::CLUDGE_OLD)
			sta.Format("CLUDGE_OLD");
		else
			sta.Format("UNDEF");
		::OutputDebugStringA(sta.Chars());

		sta.Format("; %S.\n", stuModule.Chars());
		::OutputDebugStringA(sta.Chars());
	}

	if (type != NEW)
		if (font != (HFONT)0x018A0021)
		{
			//Assert(false);
			return fontOld;
		}
	if (type == NEW)
		if (font == (HFONT)0x018A0029)
		{
			//Assert(false);
			return fontOld;
		}
#endif

	return fontOld;
};


BOOL AfGdi::DeleteObjectFont(HFONT font)
{
#ifdef DEBUG_AFGDI
	if (s_fShowFonts)
	{
		StrAnsi sta;
		sta.Format("AfGdi::DeleteObjectFont:  #%d:  font=0x%x",	s_cFonts, font);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	BOOL fSuccess = ::DeleteObject(font);

#ifdef DEBUG_AFGDI
	if (s_fShowFonts)
	{
		StrUni stuModule = ModuleEntry::GetModulePathName();
		StrAnsi sta;
		if (fSuccess)
			sta.Format("; %S.\n", stuModule.Chars());
		else
			sta.Format(" FAILED; %S.\n", stuModule.Chars());
		::OutputDebugStringA(sta.Chars());
	}
	if (fSuccess)
		s_cFonts--;
#endif

	if (font == (HFONT)0x018A0029)
	{
		//Assert(false);
		return fSuccess;
	}
	return fSuccess;
};


/*----------------------------------------------------------------------------------------------
	AfGDI::  bitmap methods.
----------------------------------------------------------------------------------------------*/

HBITMAP AfGdi::CreateBitmap(
	int nWidth,         // bitmap width, in pixels
	int nHeight,        // bitmap height, in pixels
	UINT cPlanes,       // number of color planes
	UINT cBitsPerPel,   // number of bits to identify color
	CONST VOID *lpvBits // color data array
)
{
	HBITMAP bitmap = ::CreateBitmap(nWidth, nHeight, cPlanes, cBitsPerPel, lpvBits);

#ifdef DEBUG_AFGDI
	if (bitmap)
		s_cBitmaps++;
	if (s_fShowBitmaps)
	{
		StrUni stuModule = ModuleEntry::GetModulePathName();
		StrAnsi sta;
		if (bitmap)
			sta.Format("AfGdi::CreateBitmap:  #%d:  bitmap=0x%x; %S.\n",
				s_cBitmaps, bitmap, stuModule.Chars());
		else
			sta.Format("AfGdi::CreateBitmap:  #%d:  bitmap=0x%x FAILED; %S.\n",
				s_cBitmaps, bitmap, stuModule.Chars());
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return bitmap;
};


HBITMAP AfGdi::CreateCompatibleBitmap(HDC hdc, int width, int height)
{
	HBITMAP bitmap = ::CreateCompatibleBitmap(hdc, width, height);

#ifdef DEBUG_AFGDI
	if (bitmap)
		s_cBitmaps++;
	if (s_fShowBitmaps)
	{
		StrUni stuModule = ModuleEntry::GetModulePathName();
		StrAnsi sta;
		if (bitmap)
			sta.Format("AfGdi::CreateCompatibleBitmap:  #%d:  hdc=0x%x; bitmap=0x%x; %S.\n",
				s_cBitmaps, hdc, bitmap, stuModule.Chars());
		else
			sta.Format("AfGdi::CreateCompatibleBitmap:  #%d:  hdc=0x%x; bitmap=0x%x FAILED; %S.\n",
				s_cBitmaps, hdc, bitmap, stuModule.Chars());
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return bitmap;
};


HBITMAP AfGdi::LoadBitmap(
	HINSTANCE hInstance,  // handle to application instance
	LPCTSTR lpBitmapName  // name of bitmap resource
)
{
	HBITMAP bitmap = ::LoadBitmap(hInstance, lpBitmapName);

#ifdef DEBUG_AFGDI
	if (bitmap)
		s_cBitmaps++;
	if (s_fShowBitmaps)
	{
		StrUni stuModule = ModuleEntry::GetModulePathName();
		StrAnsi sta;
		if (bitmap)
			sta.Format("AfGdi::LoadBitmap:  #%d:  bitmap=0x%x; %S.\n",
				s_cBitmaps, bitmap, stuModule.Chars());
		else
			sta.Format("AfGdi::LoadBitmap:  #%d:  bitmap=0x%x FAILED; %S.\n",
				s_cBitmaps, bitmap, stuModule.Chars());
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return bitmap;
};


HBITMAP AfGdi::LoadImageBitmap(
	HINSTANCE hinst,
	LPCTSTR lpszName,
	UINT uType,
	int cxDesired,
	int cyDesired,
	UINT fuLoad
)
{
	HBITMAP bitmap = (HBITMAP)::LoadImage(hinst, lpszName, uType, cxDesired, cyDesired, fuLoad);

#ifdef DEBUG_AFGDI
	if (bitmap)
		s_cBitmaps++;
	if (s_fShowBitmaps)
	{
		StrUni stuModule = ModuleEntry::GetModulePathName();
		StrAnsi sta;
		if (bitmap)
			sta.Format("AfGdi::LoadImage:  #%d:  bitmap=0x%x; %S.\n",
				s_cBitmaps, bitmap, stuModule.Chars());
		else
			sta.Format("AfGdi::LoadImage:  #%d:  bitmap=0x%x FAILED; %S.\n",
				s_cBitmaps, bitmap, stuModule.Chars());
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return bitmap;
};


HBITMAP AfGdi::SelectObjectBitmap(HDC hdc, HBITMAP bitmap, SelType type)
{
	HBITMAP bitmapOld = (HBITMAP)::SelectObject(hdc, bitmap);

#ifdef DEBUG_AFGDI
	if (s_fShowBitmaps)
	{
		StrUni stuModule = ModuleEntry::GetModulePathName();
		StrAnsi sta;
		sta.Format("AfGdi::SelectObjectBitmap:  hdc=0x%x; bitmap=0x%x; bitmapOld=0x%x ",
			hdc, bitmap, bitmapOld);
		::OutputDebugStringA(sta.Chars());

		if (type == AfGdi::NEW)
			sta.Format("NEW");
		else if (type == AfGdi::OLD)
			sta.Format("OLD");
		else if (type == AfGdi::CLUDGE_OLD)
			sta.Format("CLUDGE_OLD");
		else
			sta.Format("UNDEF");
		::OutputDebugStringA(sta.Chars());

		sta.Format("; %S.\n", stuModule.Chars());
		::OutputDebugStringA(sta.Chars());
	}

	if (type != NEW)
		if (bitmap != (HBITMAP)0x0185000F)
		{
			//Assert(false);
			return bitmapOld;
		}
#endif

	return bitmapOld;
};


BOOL AfGdi::DeleteObjectBitmap(HBITMAP bitmap)
{
#ifdef DEBUG_AFGDI
	if (s_fShowBitmaps)
	{
		StrAnsi sta;
		sta.Format("AfGdi::DeleteObjectBitmap:  #%d:  bitmap=0x%x", s_cBitmaps, bitmap);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	BOOL fSuccess = ::DeleteObject(bitmap);

#ifdef DEBUG_AFGDI
	if (s_fShowBitmaps)
	{
		StrUni stuModule = ModuleEntry::GetModulePathName();
		StrAnsi sta;
		if (fSuccess)
			sta.Format("; %S.\n", stuModule.Chars());
		else
			sta.Format(" FAILED; %S.\n", stuModule.Chars());
		::OutputDebugStringA(sta.Chars());
	}
	if (fSuccess)
		s_cBitmaps--;
#endif

	return fSuccess;
};


/*----------------------------------------------------------------------------------------------
	AfGDI::  brush methods.
----------------------------------------------------------------------------------------------*/

HBRUSH AfGdi::CreateBrushIndirect(CONST LOGBRUSH *lplb)
{
	HBRUSH brush = ::CreateBrushIndirect(lplb);

#ifdef DEBUG_AFGDI
	if (brush)
		s_cBrushes++;
	if (s_fShowBrushes)
	{
		StrAnsi sta;
		if (brush)
			sta.Format("AfGdi::CreateBrushIndirect:  #%d:  brush=0x%x.\n",
				s_cBrushes, brush);
		else
			sta.Format("AfGdi::CreateBrushIndirect:  #%d:  brush=0x%x FAILED.\n",
				s_cBrushes, brush);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return brush;
};


HBRUSH AfGdi::CreatePatternBrush(HBITMAP hbmp)
{
	HBRUSH brush = ::CreatePatternBrush(hbmp);

#ifdef DEBUG_AFGDI
	if (brush)
		s_cBrushes++;
	if (s_fShowBrushes)
	{
		StrAnsi sta;
		if (brush)
			sta.Format("AfGdi::CreatePatternBrush:  #%d:  brush=0x%x.\n",
				s_cBrushes, brush);
		else
			sta.Format("AfGdi::CreatePatternBrush:  #%d:  brush=0x%x FAILED.\n",
				s_cBrushes, brush);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return brush;
};


HBRUSH AfGdi::CreateSolidBrush(COLORREF crColor)
{
	HBRUSH brush = ::CreateSolidBrush(crColor);

#ifdef DEBUG_AFGDI
	if (brush)
		s_cBrushes++;
	if (s_fShowBrushes)
	{
		StrAnsi sta;
		if (brush)
			sta.Format("AfGdi::CreateSolidBrush:  #%d:  brush=0x%x.\n",
				s_cBrushes, brush);
		else
			sta.Format("AfGdi::CreateSolidBrush:  #%d:  brush=0x%x FAILED.\n",
				s_cBrushes, brush);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return brush;
};


HBRUSH AfGdi::SelectObjectBrush(HDC hdc, HBRUSH brush, SelType type)
{
	HBRUSH brushOld = (HBRUSH)::SelectObject(hdc, brush);

#ifdef DEBUG_AFGDI
	if (s_fShowBrushes)
	{
		StrAnsi sta;
		if (brush)
			sta.Format("AfGdi::SelectObjectBrush:  #%d:  brush=0x%x; ",
				s_cBrushes, brush);
		else
			sta.Format("AfGdi::SelectObjectBrush:  #%d:  brush=0x%x FAILED; ",
				s_cBrushes, brush);
		::OutputDebugStringA(sta.Chars());

		if (type == AfGdi::NEW)
			sta.Format("NEW.\n");
		else if (type == AfGdi::OLD)
			sta.Format("OLD.\n");
		else if (type == AfGdi::CLUDGE_OLD)
			sta.Format("CLUDGE_OLD.\n");
		else
			sta.Format("UNDEF.\n");
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return brushOld;
};


BOOL AfGdi::DeleteObjectBrush(HBRUSH brush)
{
#ifdef DEBUG_AFGDI
	if (s_fShowBrushes)
	{
		StrAnsi sta;
		sta.Format("AfGdi::DeleteObjectBrush:  #%d:  brush=0x%x", s_cBrushes, brush);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	BOOL fSuccess = ::DeleteObject(brush);

#ifdef DEBUG_AFGDI
	if (s_fShowBrushes)
	{
		StrAnsi sta;
		if (fSuccess)
			sta.Format(".\n");
		else
			sta.Format(" FAILED.\n");
		::OutputDebugStringA(sta.Chars());
	}
	if (fSuccess)
		s_cBrushes--;
#endif

	return fSuccess;
};


/*----------------------------------------------------------------------------------------------
	AfGDI::  image list methods.
----------------------------------------------------------------------------------------------*/

HIMAGELIST AfGdi::ImageList_Create(int cx, int cy, UINT flags, int cInitial, int cGrow)
{
	HIMAGELIST hil = ::ImageList_Create(cx, cy, flags, cInitial, cGrow);

#ifdef DEBUG_AFGDI
	if (hil)
		s_cImageLists++;
	if (s_fShowImageLists)
	{
		StrAnsi sta;
		if (hil)
			sta.Format("AfGdi::ImageList_Create:  #%d:  hil=0x%x.\n",
				s_cImageLists, hil);
		else
			sta.Format("AfGdi::ImageList_Create:  #%d:  hil=0x%x FAILED.\n",
				s_cImageLists, hil);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return hil;
};


HIMAGELIST AfGdi::TreeView_CreateDragImageZ(HWND hwndTV, HTREEITEM hitem)
{
	HIMAGELIST hil = TreeView_CreateDragImage(hwndTV, hitem);

#ifdef DEBUG_AFGDI
	if (hil)
		s_cImageLists++;
	if (s_fShowImageLists)
	{
		StrAnsi sta;
		if (hil)
			sta.Format("AfGdi::TreeView_CreateDragImage:  #%d:  hil=0x%x.\n",
				s_cImageLists, hil);
		else
			sta.Format("AfGdi::TreeView_CreateDragImage:  #%d:  hil=0x%x FAILED.\n",
				s_cImageLists, hil);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return hil;
};


HIMAGELIST AfGdi::ImageList_LoadImage(
	HINSTANCE hi,
	LPCTSTR lpbmp,
	int cx,
	int cGrow,
	COLORREF crMask,
	UINT uType,
	UINT uFlags
)
{
	HIMAGELIST hil = ::ImageList_LoadImage(hi, lpbmp, cx, cGrow, crMask, uType, uFlags);

#ifdef DEBUG_AFGDI
	if (hil)
		s_cImageLists++;
	if (s_fShowImageLists)
	{
		StrAnsi sta;
		if (hil)
			sta.Format("AfGdi::ImageList_LoadImage:  #%d:  hil=0x%x.\n",
				s_cImageLists, hil);
		else
			sta.Format("AfGdi::ImageList_LoadImage:  #%d:  hil=0x%x FAILED.\n",
				s_cImageLists, hil);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	return hil;
};


BOOL AfGdi::ImageList_Destroy(HIMAGELIST himl)
{
#ifdef DEBUG_AFGDI
	if (s_fShowImageLists)
	{
		StrAnsi sta;
		sta.Format("AfGdi::ImageList_Destroy:  #%d:  himl=0x%x", s_cImageLists, himl);
		::OutputDebugStringA(sta.Chars());
	}
#endif

	BOOL fSuccess = ::ImageList_Destroy(himl);

#ifdef DEBUG_AFGDI
	if (s_fShowImageLists)
	{
		StrAnsi sta;
		if (fSuccess)
			sta.Format(".\n");
		else
			sta.Format(" FAILED.\n");
		::OutputDebugStringA(sta.Chars());
	}
	if (fSuccess)
		s_cImageLists--;
#endif

	return fSuccess;
};


/***********************************************************************************************
	SmartPalette methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
SmartPalette::SmartPalette(HDC hdc)
{
	if (hdc)
	{
		m_hdc = hdc;
		m_hpalOld = g_ct.RealizePalette(hdc);
	}
	else
	{
		m_hdc = NULL;
		m_hpalOld = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
SmartPalette::~SmartPalette()
{
	if (m_hpalOld)
		::SelectPalette(m_hdc, m_hpalOld, false);
}

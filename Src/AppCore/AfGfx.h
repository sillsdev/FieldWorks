/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: AfGfx.h
Responsibility: Shon Katzenberger
Last reviewed:

	Graphics utilities.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AfGfx_H
#define AfGfx_H 1

class AfGdi; // Forward declaraction

/*----------------------------------------------------------------------------------------------
	AfGfx class. This provides a place (working like a namespace) for graphics utility
	functions to live without having to be global functions.
----------------------------------------------------------------------------------------------*/
class AfGfx
{
public:
	static HBITMAP LoadSysColorBitmap(int rid);

	static void FillSolidRect(HDC hdc, const Rect & rc, COLORREF clr, bool fUsePalette = true);

	static void InvertRect(HDC hdc, const Rect & rc)
	{
		::InvertRect(hdc, &rc);
	}

	static HBRUSH CreateSolidBrush(COLORREF clr);
	static COLORREF SetBkColor(HDC hdc, COLORREF clr)
	{
		return ::SetBkColor(hdc, PALETTERGB(GetRValue(clr), GetGValue(clr), GetBValue(clr)));
	}
	static COLORREF SetTextColor(HDC hdc, COLORREF clr)
	{
		return ::SetTextColor(hdc, PALETTERGB(GetRValue(clr), GetGValue(clr), GetBValue(clr)));
	}

	static void DrawBitMap(HDC hdc, HBITMAP hbmp, Rect & rcDst, int xsSrc = 0, int ysSrc = 0,
		DWORD dwRop = SRCCOPY);

	static bool EnsureVisibleRect(Rect & rc);
};


/*----------------------------------------------------------------------------------------------
Class: AfGdi
Description:
----------------------------------------------------------------------------------------------*/
class AfGdi
{
public:

	typedef enum
	{
		UNDEF=0, NEW=1, OLD=2, CLUDGE_OLD=3,
	} SelType;

	//  DEVICE CONTEXT  ------------------------------------------------------------------------

	static int s_cDCs;
	static BOOL s_fShowDCs;

	static HDC CreateDC(
		LPCTSTR lpszDriver,        // driver name
		LPCTSTR lpszDevice,        // device name
		LPCTSTR lpszOutput,        // not used; should be NULL
		CONST DEVMODE* lpInitData  // optional printer data
	);
	static HDC CreateCompatibleDC(HDC hdc);
	static BOOL	DeleteDC(HDC hdc);

	static HDC GetDC(HWND hWnd);
	static int ReleaseDC(
		HWND hWnd, // handle to window
		HDC hdc);  // handle to DC
	static void OutputDC(HDC hdc);


	//  FONTS  ---------------------------------------------------------------------------------

	static int s_cFonts;
	static BOOL s_fShowFonts; // flag to turn on/off logging info for fonts.

	static HFONT CreateFont(
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
	);
	static HFONT CreateFontIndirect(const LOGFONT * plf);
	static HFONT GetStockObjectFont(
		int fnObject=DEFAULT_GUI_FONT   // stock object type
	);
	static HFONT SelectObjectFont(HDC hdc, HFONT font, SelType fNew=NEW);
	static BOOL DeleteObjectFont(HFONT font);


	//  BITMAPS  -------------------------------------------------------------------------------

	static int s_cBitmaps;
	static BOOL s_fShowBitmaps; // flag to turn on/off logging info for bitmaps.

	static HBITMAP CreateBitmap(
		int nWidth,         // bitmap width, in pixels
		int nHeight,        // bitmap height, in pixels
		UINT cPlanes,       // number of color planes
		UINT cBitsPerPel,   // number of bits to identify color
		CONST VOID *lpvBits // color data array
	);
	static HBITMAP CreateCompatibleBitmap(HDC hdc, int width, int height);
	static HBITMAP LoadBitmap(
		HINSTANCE hInstance,  // handle to application instance
		LPCTSTR lpBitmapName  // name of bitmap resource
	);
	static HBITMAP LoadImageBitmap(
		HINSTANCE hinst,
		LPCTSTR lpszName,
		UINT uType,
		int cxDesired,
		int cyDesired,
		UINT fuLoad);
	static HBITMAP SelectObjectBitmap(HDC hdc, HBITMAP bitmap, SelType fNew=NEW);
	static BOOL DeleteObjectBitmap(HBITMAP bitmap);

	//  BRUSHES  -------------------------------------------------------------------------------

	static int s_cBrushes;
	static BOOL s_fShowBrushes; // flag to turn on/off logging info for brushes.

	static HBRUSH CreateBrushIndirect(CONST LOGBRUSH *lplb);
	static HBRUSH CreatePatternBrush(HBITMAP hbmp);
	static HBRUSH CreateSolidBrush(COLORREF crColor);
	static HBRUSH SelectObjectBrush(HDC hdc, HBRUSH brush, SelType fNew=NEW);
	static BOOL DeleteObjectBrush(HBRUSH brush);


	//  IMAGE LISTS  ---------------------------------------------------------------------------

	static int s_cImageLists;
	static BOOL s_fShowImageLists; // flag to turn on/off logging info for image lists.

	static HIMAGELIST ImageList_Create(int cx, int cy, UINT flags, int cInitial, int cGrow);
	static HIMAGELIST TreeView_CreateDragImageZ(HWND hwndTV, HTREEITEM hitem);
	static HIMAGELIST ImageList_LoadImage(
		HINSTANCE hi,
		LPCTSTR lpbmp,
		int cx,
		int cGrow,
		COLORREF crMask,
		UINT uType,
		UINT uFlags);
	static BOOL ImageList_Destroy(HIMAGELIST himl);

private:

};


/*----------------------------------------------------------------------------------------------
	SmartPalette provides a way for us to make sure the colors we have defined in UiColor.h
	show up properly when we draw, regardless of the color depth of the device context.
	Hungarian: spal
----------------------------------------------------------------------------------------------*/
class SmartPalette
{
public:
	SmartPalette(HDC hdc);
	~SmartPalette();

protected:
	HDC m_hdc;
	HPALETTE m_hpalOld;
};


/*----------------------------------------------------------------------------------------------
	SmartDc provides a thin wrapper around an hdc. It understands the several ways of obtaining
	an hdc, and its destructor performs the appropriate kind of cleanup. It also provides
	straightforward ways of performing a few otherwise messy operations.

	REVIEW ShonK(JohnT): is there any reason to create one of these on the heap and support
	reference counting? Are there any more ways to get a DC that we should support?
	Is it worth adding another argument to the constructor and another member variable
	so we can call EndPaint(hwnd, pps) when needed?

	Hungarian: sdc
----------------------------------------------------------------------------------------------*/
typedef enum DcCleanupMode
{
	kdcmNone, // no automatic cleanup required
	kdcmRelease, // call ReleaseDC
	kdcmDelete, // call DeleteDC
	kdcmRestore, // call RestoreDC (to the previous saved state)
} DcCleanupMode;

class SmartDc
{
public:
	// Construct a SmartDc from an existing hdc, specifying the cleanup required.
	// Sample usage:
	// HDC hdc = GetDc(hwnd);
	// SmartDc sdc(hdc, kdcmRelease);
	// ... use the sdc (or the hdc); ReleaseDC(hwnd,hdc) will be called when sdc goes out of
	// scope
	// hwnd is ignored and may be null unless dcm = kdcmRelease.
	SmartDc(void)
	{
		m_hwnd = NULL;
		m_hdc = NULL;
		m_dcm = kdcmNone;
	}
	SmartDc(HDC hdc, DcCleanupMode dcm = kdcmNone, HWND hwnd = NULL)
	{
		Assert(!hwnd || dcm == kdcmRelease);
		m_hwnd = hwnd;
		m_hdc = hdc;
		m_dcm = dcm;
	}
	// Uses GetDc(hwnd) to obtain the DC, ReleaseDC is automatically called when out of scope
	SmartDc(HWND hwnd)
	{
		m_hwnd = NULL;
		m_hdc = NULL;
		m_dcm = kdcmNone;
		Init(hwnd);
	}

	~SmartDc()
	{
		Clear();
	}

	// Clear it out when done. Destroys the hdc unless mode kdcmNone
	void Clear()
	{
		int iSuccess;
		BOOL fSuccess;
		if (m_hdc)
		{
			switch (m_dcm)
			{
			default:
				// It should not be possible for m_dcm to not be one of the standard items, but
				// since this is cleanup code, if it is we will just quitely do nothing.
				Assert(false);
				break;
			case kdcmNone:
				break;
			case kdcmRelease:
				iSuccess = ::ReleaseDC(m_hwnd, m_hdc);
				Assert(iSuccess);
				break;
			case kdcmDelete:
				Assert(!m_hwnd);
				fSuccess = AfGdi::DeleteDC(m_hdc);
				Assert(fSuccess);
				break;
			case kdcmRestore:
				Assert(!m_hwnd);
				fSuccess = ::RestoreDC(m_hdc, -1);
				Assert(fSuccess);
				break;
			}
		}
		m_dcm = kdcmNone;
		m_hwnd = NULL;
		m_hdc = NULL;
	}

	void Init(HDC hdc, DcCleanupMode dcm = kdcmNone, HWND hwnd = NULL)
	{
		Assert(!hwnd || dcm == kdcmRelease);
		Clear();
		m_hwnd = hwnd;
		m_hdc = hdc;
		m_dcm = dcm;
	}

	void Init(HWND hwnd)
	{
		Clear();
		m_dcm = kdcmNone;
		m_hdc = ::GetDC(hwnd);
		if (!m_hdc)
			ThrowHr(WarnHr(E_FAIL));
		m_hwnd = hwnd;
		m_dcm = kdcmRelease;
	}

	void CreateCompat(HDC hdc)
	{
		m_hdc = AfGdi::CreateCompatibleDC(m_hdc);
		if (!m_hdc)
		{
			ThrowHr(WarnHr(E_FAIL), L"CreateCompatibleDC failed");
		}
		m_dcm = kdcmDelete;
	}

	// ENHANCE JohnT: Implement something to wrap a bitmap?

	// Allows a SmartDc to be used just like a plain HDC
	operator HDC() const
	{
		return m_hdc;
	}

protected:
	HWND m_hwnd;
	HDC m_hdc;
	DcCleanupMode m_dcm;
};

/*----------------------------------------------------------------------------------------------
	FontWrap provides a thin wrapper around an HFONT. The constructor takes an HDC and an
	HFONT. It selects the font into the hdc, remembering the previous font. The destructor
	selects the old font back into the HDC and then deletes the font.
	Hungarian: fwr
----------------------------------------------------------------------------------------------*/
class FontWrap
{
public:
	FontWrap(HFONT hfont, HDC hdc)
	{
		Assert(hfont);
		Assert(hdc);
		m_hdc = hdc;
		m_hfontOld = AfGdi::SelectObjectFont(hdc, hfont);
		if (!m_hfontOld)
			ThrowHr(WarnHr(E_FAIL));
		m_hfont = hfont;
	}

	~FontWrap()
	{
		Clear();
	}

	operator HFONT() const
	{
		return m_hfont;
	}

	// Clean up in advance of destruction. The font is deselected and destroyed.
	void Clear()
	{
		if (m_hfontOld)
		{
			AfGdi::SelectObjectFont(m_hdc, m_hfontOld, AfGdi::OLD);
			m_hfontOld = NULL;
		}
		if (m_hfont)
		{
			AfGdi::DeleteObjectFont(m_hfont);
			m_hfont = NULL;
		}
	}

	// Cancel cleanup. The caller resumes responsibility to select the original font
	// (returned by this method) back into the DC and delete the wrapped font.
	HFONT Detach()
	{
		m_hfont = NULL;
		HFONT hfontTemp = m_hfontOld;
		m_hfontOld = NULL;
		return hfontTemp;
	}

protected:
	HDC m_hdc;
	HFONT m_hfont;
	HFONT m_hfontOld;
};

/*----------------------------------------------------------------------------------------------
	BrushWrap provides a thin wrapper around an HBRUSH. The constructor takes an HDC and an
	HBRUSH. It selects the brush into the hdc, remembering the previous brush. The destructor
	selects the old brush back into the HDC and then deletes the brush.
	ENHANCE JohnT: is there one dominant way of creating brushes that would be worth supporting
	with a dedicated constructor that creates the brush as well as wrapping and selecting it?
	Hungarian: bwr
----------------------------------------------------------------------------------------------*/
class BrushWrap
{
public:
	BrushWrap(HBRUSH hbrush, HDC hdc)
	{
		Assert(hbrush);
		Assert(hdc);
		m_hdc = hdc;
		m_hbrushOld = AfGdi::SelectObjectBrush(hdc, hbrush);
		if (!m_hbrushOld)
			ThrowHr(WarnHr(E_FAIL));
		m_hbrush = hbrush;
	}

	~BrushWrap()
	{
		Clear();
	}

	operator HBRUSH() const
	{
		return m_hbrush;
	}

	// Clean up in advance of destruction. The brush is deselected and destroyed.
	void Clear()
	{
		if (m_hbrushOld)
		{
			AfGdi::SelectObjectBrush(m_hdc, m_hbrushOld, AfGdi::OLD);
			m_hbrushOld = NULL;
		}
		if (m_hbrush)
		{
			AfGdi::DeleteObjectBrush(m_hbrush);
			m_hbrush = NULL;
		}
	}

	// Cancel cleanup. The caller resumes responsibility to select the original brush
	// (returned by this method) back into the DC and delete the wrapped brush.
	HBRUSH Detach()
	{
		m_hbrush = NULL;
		HBRUSH hbrushTemp = m_hbrushOld;
		m_hbrushOld = NULL;
		return hbrushTemp;
	}

protected:
	HDC m_hdc;
	HBRUSH m_hbrush;
	HBRUSH m_hbrushOld;
};

/*----------------------------------------------------------------------------------------------
	PenWrap provides a thin wrapper around an HPEN. The constructor takes an HDC and an
	HPEN. It selects the pen into the hdc, remembering the previous pen. The destructor
	selects the old pen back into the HDC and then deletes the pen.
	Hungarian: xpwr
----------------------------------------------------------------------------------------------*/
class PenWrap
{
public:
	PenWrap(HPEN hpen, HDC hdc)
	{
		Assert(hpen);
		Assert(hdc);
		m_hdc = hdc;
		m_hpenOld = (HPEN)::SelectObject(hdc, (HGDIOBJ)hpen);
		if (!m_hpenOld)
			ThrowHr(WarnHr(E_FAIL));
		m_hpen = hpen;
	}

	// Constructs a pen using CreatePen, wraps it and selects it.
	PenWrap(int fnPenStyle, int nWidth, COLORREF crColor, HDC hdc)
	{
		Assert(hdc);
		m_hdc = hdc;
		m_hpen = ::CreatePen(fnPenStyle, nWidth, crColor);
		if (!m_hpen)
			ThrowHr(WarnHr(E_FAIL));
		m_hpenOld = (HPEN)::SelectObject(hdc, (HGDIOBJ)m_hpen);
		if (!m_hpenOld)
		{
			::DeleteObject((HGDIOBJ)m_hpen);
			m_hpen = NULL;
			ThrowHr(WarnHr(E_FAIL));
		}
	}

	// Similarly creates, wraps and selects a pen using CreatePenIndirect
	PenWrap(CONST LOGPEN *plgpn, HDC hdc)
	{
		Assert(hdc);
		m_hdc = hdc;
		m_hpen = ::CreatePenIndirect(plgpn);
		if (!m_hpen)
			ThrowHr(WarnHr(E_FAIL));
		m_hpenOld = (HPEN)::SelectObject(hdc, (HGDIOBJ)m_hpen);
		if (!m_hpenOld)
		{
			::DeleteObject((HGDIOBJ)m_hpen);
			m_hpen = NULL;
			ThrowHr(WarnHr(E_FAIL));
		}
	}

	~PenWrap()
	{
		Clear();
	}

	operator HPEN() const
	{
		return m_hpen;
	}

	// Clean up in advance of destruction. The pen is deselected and destroyed.
	void Clear()
	{
		if (m_hpenOld)
		{
			::SelectObject(m_hdc, (HGDIOBJ)m_hpenOld);
			m_hpenOld = NULL;
		}
		if (m_hpen)
		{
			::DeleteObject((HGDIOBJ)m_hpen);
			m_hpen = NULL;
		}
	}

	// Cancel cleanup. The caller resumes responsibility to select the original pen
	// (returned by this method) back into the DC and delete the wrapped pen.
	HPEN Detach()
	{
		m_hpen = NULL;
		HPEN hpenTemp = m_hpenOld;
		m_hpenOld = NULL;
		return hpenTemp;
	}

protected:
	HDC m_hdc;
	HPEN m_hpen;
	HPEN m_hpenOld;
};

/*----------------------------------------------------------------------------------------------
	RgnWrap provides a thin wrapper around an HRGN. The constructor takes an HDC and an
	HRGN. It selects the rgn into the hdc. The destructor deletes the rgn.
	REVIEW ShonK: is DeleteObject the right way to get rid of a region? The doc does not say.
	Hungarian: rwr
----------------------------------------------------------------------------------------------*/
class RgnWrap
{
public:
	RgnWrap(HRGN hrgn, HDC hdc)
	{
		Assert(hrgn);
		Assert(hdc);
		m_hdc = hdc;
		// NOTE JohnT: For regions all the result tells us is whether we had a simple, complex,
		// or empty region, which we don't currently care about..
		m_hrgnOld = (HRGN)::SelectObject(hdc, (HGDIOBJ)hrgn);
		if (HGDI_ERROR == m_hrgnOld)
			ThrowHr(WarnHr(E_FAIL));
		m_hrgn = hrgn;
	}

	~RgnWrap()
	{
		Clear();
	}

	operator HRGN() const
	{
		return m_hrgn;
	}

	// Clean up in advance of destruction.  m_hdc's clipping rectangle is removed.
	void Clear()
	{
		if (m_hrgn)
		{
			::DeleteObject((HGDIOBJ)m_hrgn);
			m_hrgn = NULL;
		}
	}

	// Cancel cleanup. The caller resumes responsibility to select the original region
	// (returned by this method) back into the DC and delete the wrapped region.
	HRGN Detach()
	{
		m_hrgn = NULL;
		HRGN hrgnTemp = m_hrgnOld;
		m_hrgnOld = NULL;
		return hrgnTemp;
	}

protected:
	HDC m_hdc;
	HRGN m_hrgn;
	HRGN m_hrgnOld;
};


/*----------------------------------------------------------------------------------------------
	ClipRgnWrap provides a thin wrapper around an HRGN. The constructor takes an HDC and an
	HRGN. It selects the rgn into the hdc. The destructor deletes the rgn.
	REVIEW ShonK: is DeleteObject the right way to get rid of a region? The doc does not say.
	Hungarian: crwr
----------------------------------------------------------------------------------------------*/
class ClipRgnWrap
{
public:
	ClipRgnWrap(HRGN hrgn, HDC hdc)
	{
		Assert(hrgn);
		Assert(hdc);
		m_hdc = hdc;
		HRGN hrgnTemp;
		hrgnTemp = ::CreateRectRgn(0, 0, 0, 0);
		m_hrgnOld = hrgnTemp;
		int iResult = ::GetClipRgn(hdc, m_hrgnOld);
//		::DeleteObject((HGDIOBJ)hrgnTemp);  This DeleteObject is not needed.
		if (iResult == -1)
		{
			// An error occurred.
			ThrowHr(WarnHr(E_FAIL));
		}
		else if (!iResult)
		{
			// No current clip region
			::DeleteObject((HGDIOBJ)m_hrgnOld); // trash the temp one

			// The function succeeded and the given device context, has NO clipping region.
			m_hrgnOld = NULL;
		}
		else
		{
			// The function succeeded and the given device context has a clipping region.
			// m_hrgnOld is now set to the previous clipping region.
		}

		int iSuccess = ::SelectClipRgn(hdc, hrgn);
		if ((!iSuccess) || (ERROR == iSuccess))
			ThrowHr(WarnHr(E_FAIL));
		m_hrgn = hrgn;
	}

	~ClipRgnWrap()
	{
		Clear();
	}

	operator HRGN() const
	{
		return m_hrgn;
	}

	// Clean up in advance of destruction.  m_hdc's clipping rectangle is removed.
	void Clear()
	{
		if (m_hrgn)
		{
			// Delete wrapped clipping region.
			::DeleteObject((HGDIOBJ)m_hrgn);
			m_hrgn = NULL;
		}
		if (m_hdc)
		{
			// Restore previous clipping region.
			int iSuccess = ::SelectClipRgn(m_hdc, m_hrgnOld);
			if ((!iSuccess) || (ERROR == iSuccess))
				ThrowHr(WarnHr(E_FAIL));
		}
		if (m_hrgnOld)
		{
			// Delete our copy of previous clipping region.
			::DeleteObject((HGDIOBJ)m_hrgnOld);
			m_hrgnOld = NULL;
		}
	}

	// Cancel cleanup. The caller resumes responsibility to select the original clip region
	// (returned by this method) back into the DC and delete the wrapped region.
	HRGN Detach()
	{
		m_hrgn = NULL;
		HRGN hrgnTemp = m_hrgnOld;
		m_hrgnOld = NULL;
		return hrgnTemp;
	}

protected:
	HDC m_hdc;
	HRGN m_hrgn; // wrapped clipping region.
	HRGN m_hrgnOld; // previous clipping region which is restored at wrapper destruction.
};

#endif // !AfGfx_H

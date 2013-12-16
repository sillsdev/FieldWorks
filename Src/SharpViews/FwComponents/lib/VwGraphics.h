/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwGraphics.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This class encapsulates the host drawing context, in this case (for Windows) an hDC.
	It provides a portable interface for the drawing functions we use to the View and
	Rendering subsystems.

	Before using any other methods, the client should call Initialize(), passing an HDC.
	The VwGraphics class will do a SaveDC on this DC during Initialize, and a RestoreDC
	in its destructor.

	If you do any drawing to the DC not via the VwGraphics, call SaveDC before you start,
	and RestoreDC before making any further calls to the VwGraphics.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWGRAPHICS_INCLUDED
#define VWGRAPHICS_INCLUDED

#if !WIN32
#include "VwGraphicsCairo.h"
#else

// Trick GUID for getting the actual implementation of the VwGraphics object.
// JT: don't know why this next line was not needed until I moved it into TextServe.
class __declspec(uuid("FC1535E1-27C7-11d3-8078-0000C0FB81B5")) VwGraphics;
#define CLID_VWGRAPHICS_IMPL __uuidof(VwGraphics)


/*----------------------------------------------------------------------------------------------
Class: VwGraphics
Description:
Author: John Thomson (February 25, 1999)
----------------------------------------------------------------------------------------------*/
class VwGraphics : public IVwGraphicsWin32
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	VwGraphics();
	virtual ~VwGraphics();

#ifdef BASELINE
	VwGraphics(SilTestSite *psts, bool fDraw, bool fFile);
#endif

	// Member variable access

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// The actual IVwGraphics methods.
	STDMETHOD(InvertRect)(int nTwipsLeft, int nTwipsTop, int nTwipsRight, int nTwipsBottom);
	STDMETHOD(put_ForeColor)(int nRGB);
	STDMETHOD(put_BackColor)(int nRGB);
	STDMETHOD(DrawRectangle)(int nTwipsLeft, int nTwipsTop, int nTwipsRight, int nTwipsBottom);
	STDMETHOD(DrawHorzLine)(int xLeft, int xRight, int y, int dyHeight,
		int cdx, int * prgdx, int * pdxStart);
	STDMETHOD(DrawLine)(int nTwipsXStart, int nTwipsYStart, int nTwipsXEnd, int nTwipsYEnd);
	STDMETHOD(DrawText)(int ixTwips, int iyTwips, int cch, const OLECHAR * prgch, int nStretch);
	STDMETHOD(DrawTextExt)(int x, int y, int cch, const OLECHAR __RPC_FAR * prgchw,
		UINT uOptions, const RECT __RPC_FAR * pRect, int __RPC_FAR * prgdx);
	STDMETHOD(GetTextExtent)(int cch, const OLECHAR * prgch, int *pnTwipsWidth, int *pnTwipsHeight);
	STDMETHOD(GetTextLeadWidth)(int cch, const OLECHAR * prgch, int ich, int dxStretch,
		int * pdx);
	STDMETHOD(GetClipRect)(int * pxLeft, int * pyTop, int * pxRight, int * pyBottom);
	STDMETHOD(GetFontEmSquare)(int * pxyFontEmSquare);
	STDMETHOD(GetGlyphMetrics)(int chw,
		int * psBoundingWidth, int * pyBoundingHeight,
		int * pxBoundingX, int * pyBoundingY, int * pxAdvanceX, int * pyAdvanceY);
	STDMETHOD(GetFontData)(int nTableId, int * pcbTableSz, BSTR * pbstrTableData);
	STDMETHOD(GetFontDataRgch)(int nTableId, int * pcbTableSz, OLECHAR * prgch, int cchMax);
	STDMETHOD(XYFromGlyphPoint)(int chw, int nPoint, int * pxRet, int * pyRet);
	STDMETHOD(get_FontAscent)(int* pdy);
	STDMETHOD(get_FontDescent)(int* pdy);
	STDMETHOD(get_FontCharProperties)(LgCharRenderProps * pchrp);
	STDMETHOD(ReleaseDC)();
	STDMETHOD(get_XUnitsPerInch)(int * pxInch);
	STDMETHOD(put_XUnitsPerInch)(int xInch);
	STDMETHOD(get_YUnitsPerInch)(int * pyInch);
	STDMETHOD(put_YUnitsPerInch)(int yInch);
	STDMETHOD(GetSuperscriptHeightRatio)(int* piNumerator, int* piDenominator);
	STDMETHOD(GetSuperscriptYOffsetRatio)(int* piNumerator, int* piDenominator);
	STDMETHOD(GetSubscriptHeightRatio)(int* piNumerator, int* piDenominator);
	STDMETHOD(GetSubscriptYOffsetRatio)(int* piNumerator, int* piDenominator);
	STDMETHOD(SetupGraphics)(LgCharRenderProps * pchrp);
	STDMETHOD(PushClipRect)(RECT rcClip);
	STDMETHOD(PopClipRect)();

	STDMETHOD(DrawPolygon)(int cVertices, POINT prgvpnt[]);
	STDMETHOD(RenderPicture)(IPicture * ppic, int x, int y, int cx, int cy,
		OLE_XPOS_HIMETRIC xSrc, OLE_YPOS_HIMETRIC ySrc,
		OLE_XSIZE_HIMETRIC cxSrc, OLE_YSIZE_HIMETRIC cySrc,
		LPCRECT prcWBounds);
	STDMETHOD(MakePicture)(byte * pbData, int cbData, IPicture ** pppic);

	// IVwGraphicsWin32 methods
	STDMETHOD(Initialize)(HDC dc);
	STDMETHOD(GetDeviceContext)(HDC * phdc);
	STDMETHOD(SetMeasureDc)(HDC hdc);
	STDMETHOD(SetClipRect)(RECT * prcClip);

	// Other public methods
	HDC DeviceContext()
	{
		return m_hdc;
	}
	int GetXInch();
	int GetYInch();
	void SetFont(HFONT hfont);


protected:
		// Baselining variables
#ifdef BASELINE
	bool m_fFile;
	bool m_fDraw;

	SilTestSite *m_psts;
#endif

	byte * MyGetFontTable(DWORD nTableId);
	byte * MyGetOS2Table();
	int GetFontHeightFromFontTable();

	void MyGetClipRect(RECT * prc);
	// Member variables
	long m_cref;
	int m_rgbForeColor;
	int m_rgbBackColor;
	HDC m_hdc;
	HDC m_hdcMeasure;
	Rect m_rcClip;

	typedef Vector<HRGN> VecHRgn;

	// Stack of clip regions used by PushClipRect and PopClipRect.
	VecHRgn m_vhrgnClipStack;
	HFONT m_hfontOld; // original font to restore into the DC.
	// If we have a distinct measure DC, we save its original font here to restore into it.
	HFONT m_hfontOldMeasure;
	HFONT m_hfont; // current font selected into DC, if any
	LgCharRenderProps m_chrp;

	// Vertical and horizontal resolution. Zero indicates not yet initialized.
	int m_xInch;
	int m_yInch;

	void Init();

	int IntFromFixed(FIXED f)
	{
		if (f.fract >= 0x8000)
			return(f.value + 1);
		else
			return(f.value);
	}

	bool CompareFixed(FIXED f1, FIXED f2)
	{
		if (f1.value == f2.value && f1.fract == f2.fract)
			return true;
		return false;
	}

	void CheckDc();
};

DEFINE_COM_PTR(VwGraphics);
#endif // WIN32

#endif  // VWGRAPHICS_INCLUDED

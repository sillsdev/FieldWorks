/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwGraphics.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This class encapsulates the host drawing context, in this case a Cairo context.
	It provides a portable interface for the drawing functions we use to the View and
	Rendering subsystems.

	Before using any other methods, the client should call one of the Initialize...() methods
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWGRAPHICSCAIRO_INCLUDED
#define VWGRAPHICSCAIRO_INCLUDED

#include "common.h"
#include "VwColor.h"

#include <vector>
#include <cairomm/context.h>
#include <cairomm/surface.h>
#include <pango/pango.h>
//#include <gtkmm.h>
//#include <gdkmm/rectangle.h>

#include "FwKernelTlb.h"

#include <COM.h>
#include <COMInterfaces.h>
#include <WinError.h>

// VwGraphicsCairo is really a VwGraphicsWin32 object (same guid)
#define VwGraphicsCairo VwGraphicsWin32
#define VwGraphicsCairoPtr VwGraphicsWin32Ptr

typedef LgCharRenderProps VwCharRenderProps;

#if SEPARATED_CHAR_RENDER_PROPS

inline bool operator==(const VwFontProps& left, const VwFontProps& right) {
	return memcmp(&left, &right, sizeof(left)) == 0;
}

inline bool operator==(const VwCharRenderProps& left, const VwCharRenderProps& right) {
	return memcmp(&left, &right, sizeof(left)) == 0;
}

#endif

//#include <ft2build.h>
//#include FT_FREETYPE_H

// Trick GUID for getting the actual implementation of the VwGraphics object.
// JT: don't know why this next line was not needed until I moved it into TextServe.
//class __declspec(uuid("FC1535E1-27C7-11d3-8078-0000C0FB81B5")) VwGraphics;
#define CLID_VWGRAPHICS_IMPL __uuidof(VwGraphics)

/*
#ifndef IID_IVwGraphics
	#define IID_IVwGraphics __uuidof(IVwGraphics)
#endif
*/

/*----------------------------------------------------------------------------------------------
Class: VwGraphics
Description:
Author: John Thomson (February 25, 1999) - modifed by TomH

	This uses cairo but implements the same	Interface as VwGraphicsWin32. Also assumes running on mono using mono implementation of swf via monos implementation of gdi+.
----------------------------------------------------------------------------------------------*/
class VwGraphicsCairo : public IVwGraphicsWin32
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	VwGraphicsCairo();
	virtual ~VwGraphicsCairo();

// Implementation of IUnknown
private:
	// Stores the reference count for IUnKnown
	UCOMINT32 m_cref;
public:
	virtual UCOMINT32 AddRef()
	{
	  ::InterlockedIncrement(&m_cref);
		  return m_cref;
	}

		virtual UCOMINT32 Release()
	{
		  UCOMINT32 cref = ::InterlockedDecrement(&m_cref);
		  if (!cref)
		  {
			m_cref = 1;
			delete this;
		  }
		  return cref;
	}

	// Implements IUnknown::QueryInterface for VwGraphicsGTK
		virtual HRESULT QueryInterface(REFIID riid, void** ppv);

#ifdef BASELINE
	VwGraphicsCairo(SilTestSite *psts, bool fDraw, bool fFile);
#endif
	// IVwGraphics methods

	HRESULT(InvertRect)(int nTwipsLeft, int nTwipsTop, int nTwipsRight, int nTwipsBottom);
	HRESULT(put_ForeColor)(int nRGB);
	HRESULT(put_BackColor)(int nRGB);
	HRESULT(DrawRectangle)(int nTwipsLeft, int nTwipsTop, int nTwipsRight, int nTwipsBottom);
	HRESULT(DrawHorzLine)(int xLeft, int xRight, int y, int dyHeight,
		int cdx, int * prgdx, int * pdxStart);
	HRESULT(DrawLine)(int nTwipsXStart, int nTwipsYStart, int nTwipsXEnd, int nTwipsYEnd);
	HRESULT(DrawText)(int ixTwips, int iyTwips, int cch, const OLECHAR * prgch, int unused);
	HRESULT DrawTextExt(int THIS, int  METHOD, int IS, const OLECHAR* NO, UINT LONGER, const RECT* IN, int* USE);
	HRESULT(GetTextExtent)(int cch, const OLECHAR * prgch, int *pnTwipsWidth, int *pnTwipsHeight);
	HRESULT(GetTextLeadWidth)(int cch, const OLECHAR * prgch, int ich, int dxStretch,
		int * pdx);
	HRESULT(GetClipRect)(int * pxLeft, int * pyTop, int * pxRight, int * pyBottom);
	HRESULT(GetFontEmSquare)(int * pxyFontEmSquare);
	HRESULT(GetGlyphMetrics)(int chw,
		int * psBoundingWidth, int * pyBoundingHeight,
		int * pxBoundingX, int * pyBoundingY, int * pxAdvanceX, int * pyAdvanceY);
	HRESULT(GetFontData)(int nTableId, int * pcbTableSz, BSTR * pbstrTableData);
	HRESULT(GetFontDataRgch)(int nTableId, int * pcbTableSz, OLECHAR * prgch, int cchMax);
	HRESULT(XYFromGlyphPoint)(int chw, int nPoint, int * pxRet, int * pyRet);
	HRESULT(get_FontAscent)(int* pdy);
	HRESULT(get_FontDescent)(int* pdy);
	HRESULT(get_FontCharProperties)(VwCharRenderProps * pchrp);
	HRESULT(ReleaseDC)();
	HRESULT(get_XUnitsPerInch)(int * pxInch);
	HRESULT(put_XUnitsPerInch)(int xInch);
	HRESULT(get_YUnitsPerInch)(int * pyInch);
	HRESULT(put_YUnitsPerInch)(int yInch);
	HRESULT(GetSuperscriptHeightRatio)(int* piNumerator, int* piDenominator);
	HRESULT(GetSuperscriptYOffsetRatio)(int* piNumerator, int* piDenominator);
	HRESULT(GetSubscriptHeightRatio)(int* piNumerator, int* piDenominator);
	HRESULT(GetSubscriptYOffsetRatio)(int* piNumerator, int* piDenominator);
	HRESULT(SetupGraphics)(VwCharRenderProps * pchrp);
	HRESULT(PushClipRect)(RECT rcClip);
	HRESULT(PopClipRect)();
	HRESULT(DrawPolygon)(int cVertices, POINT prgvpnt[]);
	HRESULT(RenderPicture)(IPicture * ppic, int x, int y, int cx, int cy,
		OLE_XPOS_HIMETRIC xSrc, OLE_YPOS_HIMETRIC ySrc,
		OLE_XSIZE_HIMETRIC cxSrc, OLE_YSIZE_HIMETRIC cySrc,
		LPCRECT prcWBounds);
	HRESULT(MakePicture)(byte * pbData, int cbData, IPicture ** pppic);

	// IVwGraphicsWin32 methods
	HRESULT(Initialize)(HDC hdc);
	HRESULT(GetDeviceContext)(HDC *phdc);
	HRESULT(SetMeasureDc)(HDC hdc);
	HRESULT(SetClipRect)(RECT * prcClip);
	HRESULT(GetTextStyleContext)(HDC * pContext);

	HDC DeviceContext()
	{
		return m_hdc;
	}

	// Metrics methods

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


#if DEBUG
	FILE * m_loggingFile;
#endif

	/* TODO:
	 * Find out where these vars should come from
	 */
	static const unsigned int kclrTransparent = 0xC0000000;

	byte * MyGetFontTable(DWORD nTableId);
	byte * MyGetOS2Table();
	int GetFontHeightFromFontTable();

	void MyGetClipRect(RECT * prc);

	// Helper function that returns width and height of drawn text, the also returns possible x,y offsets
	bool GetTextExtentHelper(int cch, const OLECHAR * prgch, int * x, int * y, int * pdx, int * pdy);

	// Helper function that creates and configs a PangoLayout if m_layout isn't set
	// caller must NOT call g_object_unref on the layout, as ReleaseDC unrefs m_layout.
	PangoLayout * GetPangoLayoutHelper();

	// Helper function that gets the font Asscent and Descent
	bool FontAscentAndDescent(int * ascent, int * descent);

	// Helper function that gets the fonts Line Height (including spacing)
	int GetFontLineHeight();

	// Colours in use
	VwColor m_foregroundColor;
	VwColor m_backgroundColor;
	VwColor m_textColor;
	VwColor m_textBackColor;

	// This ptr HDC is, when using mono SWF, is actualy a (mono implemented gdi+ Graphics object)
	HDC m_hdc;

	// Surfaces in use
	Cairo::RefPtr<Cairo::Context> m_ctxt;
	/*
	Glib::RefPtr<Gdk::GC> m_gc;
	Glib::RefPtr<Gtk::DrawingArea> m_drawing_area;
	*/

	// Clipping region
	RECT m_rcClip;

	// >0 if drawing is allowed
	int m_enabled;

	/* TODO:
	 * This used to be HRGN's (which are now null pointers)
	 * Whilst HRGN's can store very complex shapes, it appears they are only
	 * being used to store rectangles (it just happens that win32 dc's provide HRGN's)
	 *
	 * Make this vector into a stack
	 */
	typedef std::vector<RECT> VecRect;

	// Stack of clip regions used by PushClipRect and PopClipRect.
	VecRect m_vrectClipStack;
	cairo_scaled_font_t* m_scaledFont;
	VwCharRenderProps m_chrp;

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

	/** Set current drawing color for a Cairo surface
	 */
	void SetCairoColor(Cairo::RefPtr<Cairo::Context> ctxt, VwColor* col) {
		//std::cerr << "SetCairoColor called... ";
		ctxt->set_source_rgba(
		col->m_red,
		col->m_green,
		col->m_blue,
		1.0);
		//std::cerr << "and finished\n";
	}

	void CheckDc();
	bool rectIntersect(RECT* a, RECT* b);

	// Stores the font information for pango.
	PangoFontDescription *m_pangoFontDescription;

	// Cache ascent and descent values - these are cleared when m_pangoFontDescription is changed.
	int m_ascent, m_descent;

	// The PangoFontMap used to initialize the PangoContext used for looking up font info.
	PangoFontMap * m_fontMapForFontContext;
	// The PangoContext used for looking up font info.
	PangoContext * m_fontContext;

	// The PangoFontMap used to initialize the m_context which is used for drawing.
	PangoFontMap * m_fontMap;
	// Each VwGraphics creates its own PangoContext rather than creating one from it's
	// Cairo context. This make things more robust.
	PangoContext * m_context;

	PangoLayout * m_layout;
};

DEFINE_COM_PTR(VwGraphicsCairo);

typedef VwGraphicsCairo VwGraphics;
typedef VwGraphicsCairoPtr VwGraphicsPtr;

#endif  // VWGRAPHICSCAIRO_INCLUDED

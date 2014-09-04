/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwGraphics.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Implements the actual drawing functions we need. Some methods are just used by helper
	classes like ActualTextProperties.
-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE
/***********************************************************************************************
	Forward declarations
***********************************************************************************************/

/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/

/***********************************************************************************************
	Two local classes, copied from AfGfx.h. Maybe we should move them to somewhere they
	can be shared more easily?
***********************************************************************************************/

/***********************************************************************************************
	Constructors/Destructor
***********************************************************************************************/

VwGraphics::VwGraphics()
{
	m_hdc = NULL;
	m_cref = 1;
	m_hfont = NULL;
	m_hfontOld = NULL;
	m_hfontOldMeasure = NULL;
	Init();

	ModuleEntry::ModuleAddRef();
}

#ifdef BASELINE
VwGraphics::VwGraphics(SilTestSite *psts, bool fDraw, bool fFile)
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();

	m_psts = psts;
	m_fDraw = fDraw;
	m_fFile = fFile;
	Init();
}
#endif

VwGraphics::~VwGraphics()
{
	BOOL fSuccess;
	if (m_hdc)
	{
		ReleaseDC();
	}
	Assert(!m_hfontOld);
	if (m_hfontOld)
	{
		fSuccess = AfGdi::DeleteObjectFont(m_hfontOld);
		m_hfontOld = NULL;
	}
	Assert(!m_hfont);
	if (m_hfont)
	{
		fSuccess = AfGdi::DeleteObjectFont(m_hfont);
		m_hfont = NULL;
	}

	if (m_hdc)
	{
		ReleaseDC();
	}
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Initialize a new instance.
----------------------------------------------------------------------------------------------*/
void VwGraphics::Init()
{
#ifdef BASELINE
	m_fDraw = true;
#endif
	// Vars are initialized by using NewObj.
	Assert(m_xInch == 0);
	Assert(m_yInch == 0);
	Assert(m_hdc == NULL);
	Assert(m_hfont == NULL);
	Assert(m_hfontOld == NULL);
	// m_chrp should contain zeros too; don't bother checking.

	// Initialize the clip rectangle to be as big as possible:
	// TODO: decide if we want to do this.
//	m_rcClip.top = 0;
//	m_rcClip.left = 0;
//	m_rcClip.right = INT_MAX;
//	m_rcClip.bottom = INT_MAX;
}

/***********************************************************************************************
	Generic factory stuff to allow creating an instance with CoCreateInstance.
***********************************************************************************************/
static GenericFactory g_fact(
	_T("SIL.Text.VwGraphicsWin32"),
	&CLSID_VwGraphicsWin32,
	_T("SIL Graphics"),
	_T("Apartment"),
	&VwGraphics::CreateCom);


void VwGraphics::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwGraphics> qvg;
	qvg.Attach(NewObj VwGraphics());		// ref count initialy 1
	CheckHr(qvg->QueryInterface(riid, ppv));
}

/***********************************************************************************************
	IUnknown Methods
***********************************************************************************************/
STDMETHODIMP VwGraphics::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (&riid == &CLID_VWGRAPHICS_IMPL)
		*ppv = static_cast<VwGraphics *>(this);
	else if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwGraphics)
		*ppv = static_cast<IVwGraphics *>(this);
	else if (riid == IID_IVwGraphicsWin32)
		*ppv = static_cast<IVwGraphicsWin32 *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<IVwGraphics *>(this),
			IID_IVwGraphics, IID_IVwGraphicsWin32);
		return S_OK;
	}
	// TODO: do we need to add a case for GrGraphics?????????
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

/***********************************************************************************************
	IVwGraphics Interface Methods
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Invert the specified rectangle.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::InvertRect(int xLeft, int yTop, int xRight, int yBottom)
{
	BEGIN_COM_METHOD;

	//return (HRESULT)GrGraphics::InvertRect(xLeft, yTop, xRight, yBottom);

#ifdef BASELINE
	if (m_fFile)
		m_psts->OutputFormat("InvertRect(%d, %d, %d, %d)\n", xLeft, yTop, xRight, yBottom);
	if (m_fDraw)
	{
#endif

	CheckDc();

	RECT rect;
	rect.left = xLeft;
	rect.top = yTop;
	rect.right = xRight;
	rect.bottom = yBottom;

	// This is not just for efficiency; we want to avoid actual drawing
	// involving very large coordinates, because Win-95 mangles them.
	RECT rectClip;
	MyGetClipRect(&rectClip);
	RECT rectIntersect;
	if (!::IntersectRect(&rectIntersect, &rectClip, &rect))
	{
		// no intersection, nothing to do--but we (trivially) succeeded.
		return S_OK;
	}

	if (!::InvertRect(m_hdc, &rect))
		ThrowHr(WarnHr(E_FAIL));

#ifdef BASELINE
	}
#endif

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Set the foreground color used for lines, text
	Arguments:
		nRGB			RGB color value
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::put_ForeColor(int nRGB)
{
	BEGIN_COM_METHOD;

	//return (HRESULT)GrGraphics::put_ForeColor(nRGB);

#ifdef BASELINE
	if (m_fFile)
		m_psts->OutputFormat("put_ForeColor(%d)\n", nRGB);
	if (m_fDraw)
	{
#endif
	m_rgbForeColor = nRGB;
#ifdef BASELINE
	}
#endif

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Background color, used for shape interior, text background
	Arguments:
		nRGB			RGB color value or kclrTransparent
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::put_BackColor(int nRGB)
{
	BEGIN_COM_METHOD;

	//return (HRESULT)GrGraphics::put_BackColor(nRGB);

#ifdef BASELINE
	if (m_fFile)
		m_psts->OutputFormat("put_BackColor(%d)\n", nRGB);
	if (m_fDraw)
	{
#endif
	m_rgbBackColor = nRGB;
#ifdef BASELINE
	}
#endif

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Draw a rectangle, filled with the current background color
	ENHANCE: should we outline it in the foreground color?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::DrawRectangle(int xLeft, int yTop, int xRight, int yBottom)
{
	BEGIN_COM_METHOD;

	//return (HRESULT)GrGraphics::DrawRectangle(xLeft, yTop, xRight, yBottom);

#ifdef BASELINE
	if (m_fFile)
		m_psts->OutputFormat("DrawRectangle(%d, %d, %d, %d)\n", xLeft, yTop, xRight, yBottom);
	if (m_fDraw)
	{
#endif

	CheckDc();

	Rect rect;
	rect.left = xLeft;
	rect.top = yTop;
	rect.right = xRight;
	rect.bottom = yBottom;

	// check whether the rectangle is visible
	RECT rectClip;
	MyGetClipRect(&rectClip);
	RECT rectIntersect;
	if (!::IntersectRect(&rectIntersect, &rectClip, &rect))
	{
		// no intersection, nothing to do--but we (trivially) succeeded.
		return S_OK;
	}

	{
		SmartPalette spal(m_hdc);
		AfGfx::FillSolidRect(m_hdc, rect, m_rgbBackColor);
	}

#ifdef BASELINE
	}
#endif

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Draw a line!
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::DrawLine(int xStart, int yStart, int xEnd, int yEnd)
{
	BEGIN_COM_METHOD;

	//return (HRESULT)GrGraphics::DrawLine(xStart, yStart, xEnd, yEnd);

#ifdef BASELINE
	if (m_fFile)
		m_psts->OutputFormat("DrawLine(%d, %d, %d, %d)\n", xStart, yStart, xEnd, yEnd);
	if (m_fDraw)
	{
#endif

	CheckDc();

	// check whether the rectangle is visible
	RECT rectClip;
	MyGetClipRect(&rectClip);

	// If it is right out of view don't try to draw.
	// We could check horizontally, too, but we should not be getting
	// huge X offsets so it doesn't seem worth it.
	// ENHANCE: do we need to do something about the possibility of a line
	// so long that part is visible but an end point is far enough away
	// for coordinate mangling?
	if (yStart < rectClip.top && yEnd < rectClip.top)
		return S_OK;
	if (yStart > rectClip.bottom && yEnd > rectClip.bottom)
		return S_OK;

	// Typically this is one pixel; something similar on e.g. printer
	int dzpThick = (GetXInch() + GetYInch()) / (96 * 2);
	PenWrap xpwr(PS_SOLID, dzpThick, m_rgbForeColor, m_hdc);

	if (!::MoveToEx(m_hdc,xStart, yStart, NULL))
		ThrowInternalError(E_UNEXPECTED, "Can't do GDI MoveToEx");
	if (!::LineTo(m_hdc,xEnd, yEnd))
		ThrowInternalError(E_UNEXPECTED, "Can't Draw line");

#ifdef BASELINE
	}
#endif

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Draw a horizontal line that may be dotted or dashed.
	Consider first the behavior if *pdxStart is 0.
	If cdx is 0 or, equivalently, cdx is 1 and *prgdx is MAXINT, it draws a regular line
	line DrawLine from (xLeft, y) to (xRight, y).
	If there are multiple dx values, the line is dashed. A segment of length prgdx[0] is
	first drawn, then a gap of length prgdx[1] is left, then a segment of length prgdx[2],
	and so forth until the specified width is reached (the last segment, if drawn, is
	truncated to the specified length).
	If all values in prgdx are used up, we start again at prgdx[0]. An odd number of segments
	means that on and off alternate; thus, for example, a single width may be used to create
	a dashed line with equal gaps and dashes.
	The function then sets *pdxStart to the distance from xRight to the start of the last
	place where prgdx[0] was drawn (not where it was skipped, if there is an odd number).
	If *pdxStart is not initially zero, it gives a distance to skip through prgdx before
	starting. The normal use is to pass the value returned from a previous call, which
	can be used to draw several adjacent segments and have them seem continuous. You can also
	leave a gap in a dashed line by adding its width to pdxStart.
	(Another good way to use pdxStart is to set it to xLeft. This causes all patterns
	to be aligned, as if they were segments of one continuous pattern from the left margin.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::DrawHorzLine(int xLeft, int xRight, int y, int dyHeight,
	int cdx, int * prgdx, int * pdxStart)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgdx, cdx);
	ChkComArgPtr(pdxStart);

	//return (HRESULT)GrGraphics::DrawHorzLine(xLeft, xRight, y, dyHeight, cdx, prgdx,
	//	pdxStart);

#ifdef BASELINE
	if (m_fFile)	{
		StrAnsi stargdx("{");
		for (int i = 0; i < cdx; i++)	{
			if (i == cdx-1)
				stargdx.FormatAppend("%d}", *(prgdx + i));
			else
				stargdx.FormatAppend("%d,", *(prgdx + i));
		}
		m_psts->OutputFormat("DrawHorzLine(%d, %d, %d, %d, %d, \"%s\", ptr)\n", xLeft, xRight,
			y, dyHeight, cdx, stargdx.Chars());
	}
	if (m_fDraw)
	{
#endif

	CheckDc();
	// check whether the line is visible
	RECT rectClip;
	MyGetClipRect(&rectClip);
	if (y < rectClip.top || y > rectClip.bottom)
		return S_OK;

	if (xRight < xLeft)
		return E_INVALIDARG;
	if (xRight == xLeft)
		return S_OK;

	PenWrap xpwr(PS_SOLID, dyHeight, m_rgbForeColor, m_hdc);

	int * pdxLim = prgdx + cdx;
	// Compute total length of pattern
	int dxPattern = 0;
	int * pdx = prgdx;
	for (; pdx < pdxLim; pdx++)
		dxPattern += *pdx;
	int dxStartOffset = *pdxStart - (*pdxStart / dxPattern) * dxPattern;
	// Now do the drawing...
	int xRightSeg;
	pdx = prgdx;
	// The starting point stays here until we get to the first segment we actually
	// want to draw.
	::MoveToEx(m_hdc, xLeft, y, NULL);
	bool fDraw = false;
	for (int x = xLeft - dxStartOffset; x < xRight; x = xRightSeg)
	{
		// Figure the end of the current segment, or the end of the whole line,
		// whichever is smaller.
		xRightSeg = min(x + *pdx, xRight);
		// Advance to next segment (circularly)
		if (++pdx >= pdxLim)
		{
			*pdxStart = xLeft;
			pdx = prgdx;
		}
		// Alternate drawing segments and moving past them
		fDraw = !fDraw;
		// If we're in the range we want to draw...
		if (xRightSeg > xLeft)
		{
			if (fDraw)
			{
				::LineTo(m_hdc, xRightSeg, y);
			}
			else
			{
				::MoveToEx(m_hdc, xRightSeg, y, NULL);
			}
		}

	}

#ifdef BASELINE
	}
#endif

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Draw a piece of text (below and to the right of the specified point)
	OPTIMIZE: for performance, should we make the caller responsible to ensure that only text
	reasonably near the clipping box gets drawn?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::DrawText(int x, int y, int cch, const OLECHAR * prgch, int nStretch)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgch, cch);

	//return (HRESULT)GrGraphics::DrawText(x, y, cch, prgch, nStretch);

#ifdef BASELINE
	if (m_fFile)
	{
		StrAnsi staChar;
		staChar.Assign(prgch, cch);
		m_psts->OutputFormat("DrawText(%d, %d, %d, \"%s\", %d)\n", x, y, cch, staChar.Chars(),
			nStretch);
	}
	if (m_fDraw)
	{
#endif

	CheckDc();

	if (!cch)
		return S_OK;

	// check whether the text is visible, at least vertically
	RECT rectClip;
	MyGetClipRect(&rectClip);
	if (y > rectClip.bottom)
		return S_OK;
	if (y < rectClip.top - MulDiv(500, kdzptInch, GetYInch()))	// ENHANCE: correct if we support 500+ pt fonts
		return S_OK;

	if (nStretch)
	{
		// ENHANCE: when everyone is using NT, we could use GetCharacterPlacement to
		// distribute stretch. Would that work better, especially if the text
		// lacks break characters? For now this is not an option, because
		// Win-95 and Win-98 lack a wide-char version of GetCharacterPlacement.
		TEXTMETRIC tm; // to find out the break character for the font.
		if (!::GetTextMetrics(m_hdcMeasure, &tm))
			ThrowInternalError(E_UNEXPECTED, "GetTextMetrics failed");
		// assume justification only at spaces
		int cbrk = 0;
		const wchar_t * pch = prgch;
		for (int i = 0; i<cch; i++, pch++)
		{
			if (*pch == tm.tmBreakChar)
				cbrk++;
		}
		if (cbrk)
		{
			if (!::SetTextJustification(m_hdc, nStretch, cbrk))
				ThrowInternalError(E_UNEXPECTED, "SetJustification failed");
		}
		// if there are no break characters the extra space will be at the end.
		// Should we do something about not assigning stretch to runs with no
		// break characters?
	}
	// MSDN says that ExtTextOut cannot render more than 8192 characters, so we just cut it off if it is too long
	// ENHANCE: call this multiple times if the string is too long
	BOOL fOK = ::ExtTextOut(m_hdc, x, y,
		0, 0,			// no extra clipping
		prgch, min(cch, 8192),
		0);				// no special character spacing
	if (!fOK)
	{
		int gle = GetLastError();
		// We do this additional check because, if printing is cancelled, it may be that the
		// cancel happens DURING the call to ExtTextOut. It seems that in such a case, Windows
		// does not complete drawing the string which we asked it to, and returns FALSE. But,
		// getting no gle indicates that in fact nothing is wrong.
		if (gle != 0)
			ThrowInternalError(E_UNEXPECTED, "ExtTextOut failed");
	}
	if (nStretch)
	{
		// This defeats Windows' attempt to carry errors forward from one string
		// to another on a line. With this interface, we don't know whether
		// subsequent calls are on the same line. Does this matter?
		::SetTextJustification(m_hdc, 0, 0);
	}

#ifdef BASELINE
	}
//	return S_OK;
#endif

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Draw a list of glyphs at the specified position. The y value corresponds to the baseline of
	the glyphs. Each glyph specifies a vertical and horizontal offset from the original
	position. The glyphs should be in visual order.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::DrawGlyphs(int x, int y, int cgi, const GlyphInfo * prggi)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prggi, cgi);

#ifdef BASELINE
	if (m_fFile)
	{
		StrAnsi stargi;
		for (int i = 0; i < cgi; i++)
		{
			if (i != 0)
				stargdx.Append(", ");
			stargdx.FormatAppend("{%d, %d, %d}", prggi[i].glyphIndex, prggi[i].x, prggi[i].y);
		}
		m_psts->OutputFormat("DrawGlyphs(%d, %d, %d, {%s})\n", x, y, cgi, stargi.Chars());
	}
	if (m_fDraw)
	{
#endif
		CheckDc();

		// check whether the text is visible, at least vertically
		RECT rectClip;
		MyGetClipRect(&rectClip);
		if (y > rectClip.bottom)
			return S_OK;
		if (y < rectClip.top - 10000)	// ENHANCE: correct if we support 500+ pt fonts
			return S_OK;

		// converts the x and y offsets to deltas between glyphs
		OLECHAR* glyphs = new OLECHAR[cgi];
		int* deltas = new int[cgi * 2];
		for (int i = 0; i < cgi; i++)
		{
			glyphs[i] = prggi[i].glyphIndex;
			deltas[i * 2] = i == cgi - 1 ? 0 : prggi[i + 1].x - prggi[i].x;
			deltas[(i * 2) + 1] = i == cgi - 1 ? 0 : prggi[i + 1].y - prggi[i].y;
		}

		// MSDN says that ExtTextOut cannot render more than 8192 characters, so we just cut it off if it is too long
		// ENHANCE: call this multiple times if the string is too long
		BOOL res = ::ExtTextOut(m_hdc, x, y, ETO_GLYPH_INDEX | ETO_PDY, NULL, glyphs, min(cgi, 8192), deltas);

		delete[] glyphs;
		delete[] deltas;

		if (!res)
			ThrowInternalError(E_UNEXPECTED, "ExtTextOut failed");
#ifdef BASELINE
	}
#endif

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Measure the given text.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::GetTextExtent(int cch, const OLECHAR * prgch, int * pdx, int * pdy)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgch, cch);
	ChkComArgPtr(pdx);
	ChkComArgPtr(pdy);

	//return (HRESULT)GrGraphics::GetTextExtent(cch, prgch, pdx, pdy);

#ifdef BASELINE
//	if (m_fFile)
//	{
//		StrAnsi staChar;
//		staChar.Assign(prgch, cch);
//		m_psts->OutputFormat("GetTextExtent(%d, \"%s\", ptr, ptr)\n", cch, staChar.Chars());
//	}
#endif

	CheckDc();

	SIZE size;
	if (cch)
	{
		if (!::GetTextExtentPoint32W(m_hdcMeasure, prgch, cch, &size))
		ThrowInternalError(E_UNEXPECTED, "GetTextExtentPoint32W failed");
	}
	else
	{
		//The windows routine does not work for zero-length strings; it apparently
		//gives a reasonable width of 0, but a completely unreasonable height
		//that is a large negative number.
		TEXTMETRIC tm;
		if (!::GetTextMetrics(m_hdcMeasure, &tm))
			ThrowInternalError(E_UNEXPECTED, "GetTextMetrics failed");
		size.cx = 0;
		size.cy = tm.tmHeight;
	}
	*pdx = size.cx;
	*pdy = size.cy;

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Given that the text indicatd by cch and prgch was drawn with the given stretch, compute
	the width up to (but not including) character ich.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::GetTextLeadWidth(int cch, const OLECHAR * prgch, int ich,
	int dxStretch, int * pdx)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgch, cch);
	ChkComOutPtr(pdx);

	//return (HRESULT)GrGraphics::GetTextLeadWidth(cch, prgch, ich, dxStretch, pdx);

/*#ifdef BASELINE
	if (m_fFile)
	{
		StrAnsi staChar;
		staChar.Assign(prgch, cch);
		m_psts->OutputFormat("GetTextLeadWidth(%d, \"%s\", %d, %d, ptr)\n", cch, staChar.Chars(),
			ich, dxStretch);
	}
#endif*/

	CheckDc();
	if (ich > cch)
		ThrowInternalError(E_INVALIDARG, "ich out of range");
	if (!ich)
		return S_OK;
	SIZE size;
	if (!::GetTextExtentPoint32W(m_hdcMeasure, prgch, ich, &size))
		ThrowInternalError(E_UNEXPECTED, "GetTextExtentPoint32W failed");
	if (dxStretch)
	{
		// position may need to be increased. It depends how much stretch is inserted before
		// the relevant position
		TEXTMETRIC tm; // to find out the break character for the font.
		if (!::GetTextMetrics(m_hdcMeasure, &tm))
			ThrowInternalError(E_UNEXPECTED, "GetTextMetrics failed");
		// assume justification only at break chars
		int cbrk = 0;
		int cbrkPrev = 0;
		const wchar_t * pch = prgch;
		for (int i = 0; i<cch; i++, pch++)
		{
			if (*pch == tm.tmBreakChar)
			{
				cbrk++;
				if (i < ich)
					cbrkPrev++;
			}
		}
		if (!cbrkPrev)
		{
			// justification can't alter things
			*pdx = size.cx;
			return S_OK;
		}
		int dxStretchPrev = dxStretch * cbrkPrev / cbrk;  // even distribution of extra space
		*pdx = size.cx + dxStretchPrev;
		return S_OK;
	} else {
		*pdx = size.cx;
		return S_OK;
	}

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get a rectangle that bounds the area to be drawn. (Some further parts of it may be clipped)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::GetClipRect(int * pxLeft, int * pyTop, int * pxRight, int * pyBottom)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pxLeft);
	ChkComArgPtr(pyTop);
	ChkComArgPtr(pxRight);
	ChkComArgPtr(pyBottom);

	//return (HRESULT)GrGraphics::GetClipRect(pxLeft, pyTop, pxRight, pyBottom);

	CheckDc();

	RECT rect;
	MyGetClipRect(&rect);
	*pxLeft = rect.left;
	*pyTop = rect.top;
	*pxRight = rect.right;
	*pyBottom = rect.bottom;

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get the number of logical units in the currently selected font's em square.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::GetFontEmSquare(int * pxyFontEmSquare)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pxyFontEmSquare);

	//return (HRESULT)GrGraphics::GetFontEmSquare(pxyFontEmSquare);

	TEXTMETRIC tm;
	if (!::GetTextMetrics(m_hdcMeasure, &tm))
		ThrowInternalError(E_UNEXPECTED, "GetTextMetrics failed");
	*pxyFontEmSquare = tm.tmHeight - tm.tmInternalLeading;

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get glyph metrics for a glyph in the currently selected font. chw must be a glyph id.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::GetGlyphMetrics(int chw,
	int * pxBoundingWidth, int * pyBoundingHeight,
	int * pxBoundingX, int * pyBoundingY, int * pxAdvanceX, int * pyAdvanceY)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pxBoundingWidth);
	ChkComArgPtr(pyBoundingHeight);
	ChkComArgPtr(pxBoundingX);
	ChkComArgPtr(pyBoundingY);
	ChkComArgPtr(pxAdvanceX);
	ChkComArgPtr(pyAdvanceY);
	//return (HRESULT)GrGraphics::GetGlyphMetrics(chw, pxBoundingWidth, pyBoundingHeight,
	//	pxBoundingX, pyBoundingY, pxAdvanceX, pyAdvanceY);

#ifdef BASELINE
	if (m_fFile)
		m_psts->OutputFormat("GetGlyphMetrics(%d, ptr...)\n", chw);
#endif

	CheckDc();

	GLYPHMETRICS gm;
	const MAT2 mat2 = {{0,1}, {0,0}, {0,0}, {0,1}};
	if (GDI_ERROR == ::GetGlyphOutline(m_hdcMeasure, chw, GGO_GLYPH_INDEX | GGO_METRICS,
		&gm, 0, NULL, &mat2))
	{
		ThrowInternalError(E_UNEXPECTED, "GetGlyphOutline failed");
	}

	// Note: the results are already in logical units because the LOGFONT has been
	// scaled based on the resolution (DPI).

	*pxBoundingWidth = gm.gmBlackBoxX;
	*pyBoundingHeight = gm.gmBlackBoxY;
	*pxBoundingX = gm.gmptGlyphOrigin.x;
	*pyBoundingY = gm.gmptGlyphOrigin.y;
	*pxAdvanceX = gm.gmCellIncX;
	*pyAdvanceY = gm.gmCellIncY;

	//// Convert device units (pixels) to logical units.
	//int dxLogPerInch = GetXInch();
	//int dyLogPerInch = GetYInch();
	//int dxPixelsPerInch = ::GetDeviceCaps(m_hdcMeasure, LOGPIXELSX);
	//int dyPixelsPerInch = ::GetDeviceCaps(m_hdcMeasure, LOGPIXELSY);
	//*pxBoundingWidth = ::MulDiv(gm.gmBlackBoxX, dxLogPerInch, dxPixelsPerInch);
	//*pyBoundingHeight = ::MulDiv(gm.gmBlackBoxY, dxLogPerInch, dxPixelsPerInch);
	//*pxBoundingX = ::MulDiv(gm.gmptGlyphOrigin.x, dxLogPerInch, dxPixelsPerInch);
	//*pyBoundingY = ::MulDiv(gm.gmptGlyphOrigin.y, dyLogPerInch, dyPixelsPerInch);
	//*pxAdvanceX = ::MulDiv(gm.gmCellIncX, dxLogPerInch, dxPixelsPerInch);
	//*pyAdvanceY = ::MulDiv(gm.gmCellIncY, dyLogPerInch, dyPixelsPerInch);

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get the data from a table in the font.  Return S_FALSE if the table does not exist, setting
	*pcbTableSz to zero.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::GetFontData(int nTableId, int * pcbTableSz, BYTE * prgb)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pcbTableSz);
	ChkComArrayArg(prgb, *pcbTableSz);

	// The nTableID is in native (little-endian) byte order, but the GDI methods expects big-endian.
	DWORD tableName = (nTableId & 0x000000FF) << 24;
	tableName += (nTableId & 0x0000FF00) << 8;
	tableName += (nTableId & 0x00FF0000) >> 8;
	tableName += (nTableId & 0xFF000000) >> 24;

	DWORD cbTableSz = ::GetFontData(m_hdcMeasure, tableName, 0, prgb, *pcbTableSz);
	if (GDI_ERROR == cbTableSz)
	{
		// No such table in the font, or the table is empty.
		*pcbTableSz = 0;
		return S_FALSE;
	}
	*pcbTableSz = cbTableSz;

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get coordinates for a point in a glyph.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::XYFromGlyphPoint(int chw, int nPoint, int * pxRet, int * pyRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pxRet);
	ChkComOutPtr(pyRet);

	//return (HRESULT)GrGraphics::XYFromGlyphPoint(chw, nPoint, pxRet, pyRet);

#ifdef BASELINE
	if (m_fFile)
		m_psts->OutputFormat("XYFromGlyphpoint(%d, %d, ptr, ptr)\n", gid, nPoint);
#endif

	CheckDc();

	GLYPHMETRICS gm;
	const MAT2 mat2 = {{0,1}, {0,0}, {0,0}, {0,1}};
	WORD flag = GGO_GLYPH_INDEX | GGO_NATIVE;

	UINT cbBuf = ::GetGlyphOutline(m_hdcMeasure, chw, flag, &gm, 0, NULL, &mat2);
	if (cbBuf <= 0)
		ThrowInternalError(E_UNEXPECTED, "First GetGlyphOutline in XYFromGlyphPoint failed");
	byte * pGlyphData = NewObj byte[cbBuf];
	if (!pGlyphData)
		ThrowOutOfMemory();
	if (GDI_ERROR == ::GetGlyphOutline(m_hdcMeasure, chw, flag, &gm, cbBuf, pGlyphData,&mat2))
		ThrowInternalError(E_UNEXPECTED, "Second GetGlyphOutline in XYFromGlyphPoint failed");

	// Note: conversion from device to logical units is not necessary, because
	// the font size in the LOGFONT has already been scaled based on the
	// resolution of the device context.
	//int dxLogPerInch = GetXInch();
	//int dyLogPerInch = GetYInch();
	//int dxPixPerInch = ::GetDeviceCaps(m_hdcMeasure, LOGPIXELSX);
	//int dyPixPerInch = ::GetDeviceCaps(m_hdcMeasure, LOGPIXELSY);

	TTPOLYGONHEADER * pPolyHdr;
	TTPOLYCURVE * pPolyCurve;
	pPolyHdr = (TTPOLYGONHEADER *)pGlyphData;
	int nCurPoint;
	nCurPoint = 0;

	// The while loop below cannot handle getting the first point from the polygon header.
	if (nPoint == 0)
	{
		*pxRet = IntFromFixed(pPolyHdr->pfxStart.x);
		*pyRet = IntFromFixed(pPolyHdr->pfxStart.y);
		delete [] pGlyphData;
		return S_OK;
	}

	// If the last curve in the polygon is a spline, the last point in this curve will
	// repeat the point from the the polygon header. This adds an extra point to the data
	// returned by Windows that is not in the TTF font file. Test to set nExtraPt is below.
	int nExtraPt = 0;

	while ((byte *)pPolyHdr < (pGlyphData + cbBuf))
	{
		if (nPoint == nCurPoint)
		{
			*pxRet = IntFromFixed(pPolyCurve->apfx[nPoint - nCurPoint].x);
			*pyRet = IntFromFixed(pPolyCurve->apfx[nPoint - nCurPoint].y);
			delete [] pGlyphData;
			return S_OK;
		}
		nCurPoint++;
		pPolyCurve = (TTPOLYCURVE *)(pPolyHdr + 1);
		if (pPolyHdr->dwType == TT_POLYGON_TYPE)
		{
			while ((byte *)pPolyCurve < (byte *)pPolyHdr + pPolyHdr->cb)
			{
				if (pPolyCurve->wType == TT_PRIM_LINE || pPolyCurve->wType == TT_PRIM_QSPLINE)
				{
					int j = pPolyCurve->cpfx - 1; // index of last point in the curve
					if (pPolyCurve->wType == TT_PRIM_QSPLINE &&
						// test if this is the last curve
						pPolyHdr->cb - (int)((byte *)(&pPolyCurve->apfx[j]) - (byte *)(pPolyHdr))
							== sizeof POINTFX &&
						// and the two points are identical
						CompareFixed(pPolyCurve->apfx[j].x, pPolyHdr->pfxStart.x) &&
						CompareFixed(pPolyCurve->apfx[j].y, pPolyHdr->pfxStart.y))
					{
						nExtraPt = 1;
					}
					else
						nExtraPt = 0;

					if (nPoint < nCurPoint + pPolyCurve->cpfx - nExtraPt)
					{
						*pxRet = IntFromFixed(pPolyCurve->apfx[nPoint - nCurPoint].x);
						*pyRet = IntFromFixed(pPolyCurve->apfx[nPoint - nCurPoint].y);
						delete [] pGlyphData;
						return S_OK;
					}
				}
				else
				{
					delete [] pGlyphData;
					ThrowInternalError(E_UNEXPECTED);
				}

				nCurPoint += pPolyCurve->cpfx - nExtraPt;
				pPolyCurve = (TTPOLYCURVE *)&pPolyCurve->apfx[pPolyCurve->cpfx];
			}
		}
		else if (pPolyHdr->dwType == 0)
		{
			break;
		}
		else
		{
			delete [] pGlyphData;
			ThrowInternalError(E_UNEXPECTED);
		}
		pPolyHdr = (TTPOLYGONHEADER *)((byte *)pPolyHdr + pPolyHdr->cb);
	}

	delete [] pGlyphData;
	ThrowInternalError(E_UNEXPECTED);
	return E_UNEXPECTED;

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get the ascent of the currently selected font
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::get_FontAscent(int * pdy)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pdy);

	//return (HRESULT)GrGraphics::get_FontAscent(pdy);

	CheckDc();

	TEXTMETRIC tm;
	if (!::GetTextMetrics(m_hdcMeasure, &tm))
		ThrowInternalError(E_UNEXPECTED, "GetTextMetrics failed");
	*pdy = tm.tmAscent;

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get the descent of the currently selected font, in logical units.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::get_FontDescent(int * pdyRet)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pdyRet);
	CheckDc();

	//return (HRESULT)GrGraphics::get_FontDescent(pdyRet);

	TEXTMETRIC tm;
	if (!::GetTextMetrics(m_hdcMeasure, &tm))
		ThrowInternalError(E_UNEXPECTED, "GetTextMetrics failed");
	*pdyRet = tm.tmDescent;

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get the descent of the currently selected font, in logical units.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::get_FontCharProperties(LgCharRenderProps * pchrp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pchrp);
	CheckDc();

	//return (HRESULT)GrGraphics::get_FontCharProperties(pchrp);

	memcpy(pchrp, &m_chrp, isizeof(LgCharRenderProps));

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Release the DC and set pointer to null.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::ReleaseDC()
{
	BEGIN_COM_METHOD;

	//return (HRESULT)GrGraphics::ReleaseDC();

	if (m_hdc)
	{
		BOOL fSuccess;
		if (m_hfontOld)
		{
			// We have called SetFont at least once; the most recent font selected
			// is in m_hfont and is also the current one in the DC. Select the
			// original one back into the DC to prevent GDI memory leaks and similar problems.
			HFONT hfontPrev; // Fixed release build.
			hfontPrev = AfGdi::SelectObjectFont(m_hdc, m_hfontOld, AfGdi::OLD);
			fSuccess = AfGdi::DeleteObjectFont(m_hfont);
			m_hfont = 0;
			m_hfontOld = 0;
		}
		Assert(m_hfont == 0);
		if (m_hfontOldMeasure)
		{
			HFONT hfontPrev;
			hfontPrev = AfGdi::SelectObjectFont(m_hdcMeasure, m_hfontOldMeasure, AfGdi::OLD);
			fSuccess = AfGdi::DeleteObjectFont(hfontPrev);
			Assert(fSuccess);
			m_hfontOldMeasure = NULL;
		}
		fSuccess = ::RestoreDC(m_hdc, -1); // -1 means most recently saved
#ifdef DEBUG
		// TODO: We used to have an assert here, but for some reason when running from TE
		// this fails sometimes: hfontPrev has the value it had before we set
		// the font in SetFont(). We couldn't find any reason for that. However,
		// ignoring the assert seems not to break anything.
		// Assert(fSuccess);
		if (!fSuccess)
		{
			DWORD nError = ::GetLastError();
			LPVOID lpMsgBuf;
			::FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM |
				FORMAT_MESSAGE_IGNORE_INSERTS, NULL, nError,
				MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), // Default language
				(LPTSTR) &lpMsgBuf, 0, NULL );
			_CrtDbgReport(_CRT_WARN, NULL, 0, NULL,
				"VwGraphics::ReleaseDC: ::RestoreDC failed: error=%d - %S\n", nError, lpMsgBuf);
			::LocalFree(lpMsgBuf);
		}
#endif
	}
	m_hdc = NULL;
	m_hdcMeasure = NULL;
	m_rcClip.Clear();
	Assert(m_vhrgnClipStack.Size() == 0); // Make sure pushes and pops match.

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get current actual or simulated X resolution
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::get_XUnitsPerInch(int * pxInch)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pxInch);

	//return (HRESULT)GrGraphics::get_XUnitsPerInch(pxInch);

	*pxInch = GetXInch();

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Set (simulated) X resolution. Setting to zero restores actual device resolution.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::put_XUnitsPerInch(int xInch)
{
	BEGIN_COM_METHOD;

	//return (HRESULT)GrGraphics::put_XUnitsPerInch(xInch);

#ifdef BASELINE
	if (m_fFile)
		m_psts->OutputFormat("put_XUnitsPerInch(%d)\n", xInch);
#endif

	m_xInch = xInch;

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get current actual or simulated Y resolution
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::get_YUnitsPerInch(int * pyInch)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pyInch);

	//return (HRESULT)GrGraphics::get_YUnitsPerInch(pyInch);

	*pyInch = GetYInch();

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Set (simulated) Y resolution. Setting to zero restores actual device resolution.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::put_YUnitsPerInch(int yInch)
{
	BEGIN_COM_METHOD;

	//return (HRESULT)GrGraphics::put_YUnitsPerInch(yInch);

	#ifdef BASELINE
	if (m_fFile)
		m_psts->OutputFormat("put_YUnitsPerInch(%d)\n", yInch);
#endif

	m_yInch = yInch;

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}


/*----------------------------------------------------------------------------------------------
	Set up to draw text using the properties specified.
	super/subscript are ignored, as is baseline adjust; client is
	presumed to have handled them. Sets colors and HFONT.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::SetupGraphics(LgCharRenderProps * pchrp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pchrp);

	const int cbFontOffset = offsetof(LgCharRenderProps, ttvBold);
	// if the info related to choosing HFONT is different, make a new HFONT
	if (!m_hfont || memcmp(((byte *)pchrp) + cbFontOffset, ((byte *)&m_chrp) + cbFontOffset,
		isizeof(m_chrp) - cbFontOffset))
	{
		// Remember the font we switch to.
		memcpy(((byte *)&m_chrp) + cbFontOffset, ((byte *)pchrp) + cbFontOffset,
			isizeof(m_chrp) - cbFontOffset);

		// Figure the actual font we need.
		LOGFONT lf;
		lf.lfItalic = pchrp->ttvItalic == kttvOff ? false : true;
		lf.lfWeight = pchrp->ttvBold == kttvOff ? 400 : 700;
		// The minus causes this to be the font height (roughly, from top of ascenders
		// to bottom of descenders). A positive number indicates we want a font with
		// this distance as the total line spacing, which makes them too small.
		// Note that we are also scaling the font size based on the resolution.
		lf.lfHeight = -MulDiv(pchrp->dympHeight, GetYInch(), kdzmpInch);
		lf.lfUnderline = false;
		lf.lfWidth = 0;			// default width, based on height
		lf.lfEscapement = 0;	// no rotation of text (is this how to do slanted?)
		lf.lfOrientation = 0;	// no rotation of character baselines

		lf.lfStrikeOut = 0;		// not strike-out
		lf.lfCharSet = DEFAULT_CHARSET;			// let name determine it; WS should specify valid
		lf.lfOutPrecision = OUT_TT_ONLY_PRECIS;	// only work with TrueType fonts
		lf.lfClipPrecision = CLIP_DEFAULT_PRECIS; // ??
		lf.lfQuality = DRAFT_QUALITY; // I (JohnT) don't think this matters for TrueType fonts.
		lf.lfPitchAndFamily = 0; // must be zero for EnumFontFamiliesEx
		#ifdef UNICODE
				// ENHANCE: test this path if ever needed.
				wcscpy_s(lf.lfFaceName, pchrp->szFaceName);
		#else // not unicode, LOGFONT has 8-bit chars
				WideCharToMultiByte(
					CP_ACP,	0, // dumb; we don't expect non-ascii chars
					pchrp->szFaceName, // string to convert
					-1,		// null-terminated
					lf.lfFaceName, 32,
					NULL, NULL);  // default handling of unconvertibles
		#endif // not unicode
		HFONT hfont;
		hfont = AfGdi::CreateFontIndirect(&lf);
		if (!hfont)
			ThrowHr(WarnHr(E_FAIL));
		SetFont(hfont);
	}


	// Always set the colors.
	// OPTIMIZE JohnT: would it be useful to remember what the hdc is set to?
	{
		SmartPalette spal(m_hdc);

		bool fOK = (AfGfx::SetTextColor(m_hdc, pchrp->clrFore) != CLR_INVALID);
		if (pchrp->clrBack == kclrTransparent)
		{
			// I can't find it documented anywhere, but it seems to be necessary to set
			// the background color to black to make TRANSPARENT mode work--at least on my
			// computer.
			fOK = fOK && (::SetBkColor(m_hdc, RGB(0,0,0)) != CLR_INVALID);
			fOK = fOK && ::SetBkMode(m_hdc, TRANSPARENT);
		} else {
			fOK = fOK && (AfGfx::SetBkColor(m_hdc, pchrp->clrBack)!= CLR_INVALID);
			fOK = fOK && ::SetBkMode(m_hdc, OPAQUE);
		}
	}
#if 0
	// DarrellX reports that this was causing some weird failures on his machine.
	// When he selected the first overlay, fOK turned out to be false, from the
	//     fOK = fOK && (::SetBkColor(m_hdc, RGB(0,0,0)) != CLR_INVALID);
	// line above. But there doesn't seem to be any real problem. Go ahead and igore.
	if (!fOK)
		ThrowHr(WarnHr(E_FAIL));
#endif

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Invoke a new clipping rectangle; the previous clipping state can be
	restored using PopClipRect.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::PushClipRect(RECT rcClip)
{
	BEGIN_COM_METHOD;

	//return (HRESULT)GrGraphics::PushClipRect(rcClip);

	HRGN hrgn;
	HRGN hrgnTemp;
	hrgnTemp = ::CreateRectRgn(0, 0, 0, 0);
	hrgn = hrgnTemp;

	int iResult = ::GetClipRgn(m_hdc, hrgn);
//	::DeleteObject(hrgnTemp);  This DeleteObject is not needed.
	if (iResult == -1)
	{
		// An error occurred.
		ThrowHr(WarnHr(E_FAIL));
	}
	else if (iResult == 0)
	{
		// No current clip region
		::DeleteObject(hrgn); // trash the temp one

		// The function succeeded and the given device context, has NO clipping region.
		hrgn = 0;
	}
	else
	{
		// The function succeeded and the given device context has a clipping region.
		// hrgn is now set to the previous clipping region.
	}

	m_vhrgnClipStack.Push(hrgn);
	::IntersectClipRect(m_hdc, rcClip.left, rcClip.top, rcClip.right, rcClip.bottom);

	END_COM_METHOD(g_fact, IID_IVwGraphicsWin32);
}

/*----------------------------------------------------------------------------------------------
	Restore the previous clipping rectangle.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::PopClipRect()
{
	BEGIN_COM_METHOD;

	//return (HRESULT)GrGraphics::PopClipRect();

	// If *m_vhrgnClipStack.Top() is null, SelectClipRgn() will remove the clipping region.
	int iSuccess = ::SelectClipRgn(m_hdc, *m_vhrgnClipStack.Top());
	if (!iSuccess || ERROR == iSuccess)
		ThrowHr(WarnHr(E_FAIL));

	if (*m_vhrgnClipStack.Top())
	{
		::DeleteObject(*m_vhrgnClipStack.Top());
	}
	m_vhrgnClipStack.Pop();

	END_COM_METHOD(g_fact, IID_IVwGraphicsWin32);
}

/*----------------------------------------------------------------------------------------------
	Draw a polygon.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::DrawPolygon(int cVertices, POINT prgvpnt[])
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgvpnt, cVertices);

#ifdef BASELINE
	if (m_fFile)	{
		StrAnsi points("{");
		for (int i = 0; i < cVertices; i++)
		{
			int x = prgvpnt[i].x;
			int y = prgvpnt[i].y;
			points.FormatAppend("{%d,%d}", x, y);
		}
		points.Append("}");
		m_psts->OutputFormat("DrawPolygon(%d, points)\n", cVertices);
	}
	if (m_fDraw)
	{
#endif
	CheckDc();

	if (cVertices < 2)
		return S_OK;			// trivial

	RECT rectBounds;
	rectBounds.left = prgvpnt[0].x;
	rectBounds.top = prgvpnt[0].y;
	rectBounds.right = rectBounds.left;
	rectBounds.bottom = rectBounds.top;

	// determine a bounding rectangle
	for (int i = 0; i < cVertices; i++)
	{
		int x = prgvpnt[i].x;
		int y = prgvpnt[i].y;
		if (x < rectBounds.left)
			rectBounds.left = x;
		if (x > rectBounds.right)
			rectBounds.right = x;
		if (y < rectBounds.top)
			rectBounds.top = y;
		if (y > rectBounds.bottom)
			rectBounds.bottom = y;
	}

	// check whether the bounding rectangle is visible
	RECT rectClip;
	MyGetClipRect(&rectClip);
	RECT rectIntersect;
	if (!::IntersectRect(&rectIntersect, &rectClip, &rectBounds))
		return S_OK; // no intersection, nothing to do--but we (trivially) succeeded.

	PenWrap xpwr(PS_SOLID, 1, m_rgbForeColor, m_hdc);

	HBRUSH hbrsh = AfGdi::CreateSolidBrush(m_rgbBackColor); //OPTIMIZE JohnT: cache this?
	if (!hbrsh)
		ThrowInternalError(E_UNEXPECTED, "Can't make brush--GDI mem leak?");
	BrushWrap bwr(hbrsh, m_hdc);

	if (!::Polygon(m_hdc, prgvpnt, cVertices))
		ThrowInternalError(E_UNEXPECTED, "Can't draw polygon--GDI mem leak?");

#ifdef BASELINE
	}
#endif

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Render an IPicture using its Render method
	@param ppic The picture to render
	@param x Horizontal position of image in hdc
	@param y Vertical position of image in hdc
	@param cx Horizontal dimension of destination rectangle
	@param cy Vertical dimension of destination rectangle
	@param xSrc Horizontal offset in source picture
	@param ySrc Vertical offset in source picture
	@param cxSrc Amount to copy horizontally in source picture
	@param cySrc Amount to copy vertically in source picture
	@param prcWBounds Pointer to position of destination for a metafile hdc
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::RenderPicture(IPicture * ppic, int x, int y, int cx, int cy,
	OLE_XPOS_HIMETRIC xSrc, OLE_YPOS_HIMETRIC ySrc,
	OLE_XSIZE_HIMETRIC cxSrc, OLE_YSIZE_HIMETRIC cySrc,
	LPCRECT prcWBounds)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ppic);
	return ppic->Render(m_hdc, x, y, cx, cy, xSrc, ySrc, cxSrc, cySrc, prcWBounds);
	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Make an IPicture object from binary data.
	This is currently implemented using OleLoadPicture, which is able to load binary
	data from any of the following:
	Bitmaps (*.bmp); GIF Files (*.gif); JPEG Files (*.jpg); Icons (*.ico);
	Enhanced Metafiles (*.emf); Windows Metafiles (*.wmf)
	Of these JPEG, gif, and bmp are probably the most important, in that order.
	For Mac support, we may decide to reduce the set.
	The file type is automatically detected from the data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::MakePicture(byte * pbData, int cbData, IPicture ** pppic)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pppic);
	ChkComArrayArg(pbData, cbData);

	// Copy the data into an hGlobal so we can use CreateStreamOnHGlobal to make an IStream.
	// OPTIMIZE (JohnT): make a stream class based on ordinary memory.
	HGLOBAL hGlobal = GlobalAlloc(GMEM_MOVEABLE, cbData);
	if (NULL == hGlobal)
		return E_OUTOFMEMORY;

	LPVOID pvData = NULL;
	pvData = GlobalLock(hGlobal);
	if (NULL == pvData)
		ThrowInternalError(E_UNEXPECTED, "GlobalLock failed");

	::memcpy(pvData, pbData, cbData);

	GlobalUnlock(hGlobal);

	LPSTREAM pstm = NULL;
	// create IStream* from global memory
	HRESULT hr;
	IgnoreHr(hr = CreateStreamOnHGlobal(hGlobal, TRUE, &pstm));
	if (FAILED(hr) || !pstm)
		ThrowInternalError(hr, "CreateStreamOnHGlobal failed");

	// Create IPicture from image file
	IgnoreHr(hr = ::OleLoadPicture(pstm, cbData, FALSE, IID_IPicture, (LPVOID *)pppic));
	pstm->Release();
	if (FAILED(hr) || !*pppic)
		ThrowInternalError(hr, "OleLoadPicture failed");
	END_COM_METHOD(g_fact, IID_IVwGraphics);
}



/***********************************************************************************************
	IVwGraphicsWin32 methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Set up the graphics object to use a particular HDC.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::Initialize(HDC hdc)
{
	BEGIN_COM_METHOD;

	//return (HRESULT)GrGraphics::Initialize(hdc);

	m_hdc = hdc;
	m_hdcMeasure = hdc; //  default unless overridden by  SetMeasureDc.
	Assert(m_hfont == 0); // Setting a new DC, we should not have any saved font info.

	BOOL fSuccess;
	fSuccess = ::SaveDC(m_hdc);
	Assert(fSuccess);

	m_hfontOld = 0;
	m_hfontOldMeasure = 0;
	//::SetMapMode(hdc, MM_TEXT);
#if 0 // old code to force it to be twips. May be useful for ActiveX control
	::SetWindowExtEx(hdc, 1440, 1440, NULL); // 72*20 twips per inch
	::SetViewportExtEx(hdc, ::GetDeviceCaps(hdc, LOGPIXELSX),
		::GetDeviceCaps(hdc, LOGPIXELSY), NULL);
#endif

	// For the sake of being definite; rarely makes a difference; I think this is default
	::SetPolyFillMode(hdc, ALTERNATE);

	// Need to set background mode to transparent so Graphite can draw overlapping glyphs
	::SetBkMode(hdc, TRANSPARENT);

	END_COM_METHOD(g_fact, IID_IVwGraphicsWin32);
}

/*----------------------------------------------------------------------------------------------
	Recover the last hDC passed to Initialize.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::GetDeviceContext(HDC * phdc)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(phdc);

	//return (HRESULT)GrGraphics::GetDeviceContext(phdc);

	*phdc = m_hdc;

	END_COM_METHOD(g_fact, IID_IVwGraphicsWin32);
}

/*----------------------------------------------------------------------------------------------
	Set a separate DC to be used for measuring operations (generally whatever is not
	supported in case the output DC is a metafile).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::SetMeasureDc(HDC hdc)
{
	BEGIN_COM_METHOD;

	//return (HRESULT)GrGraphics::SetMeasureDc(hdc);

	m_hdcMeasure = hdc;

	END_COM_METHOD(g_fact, IID_IVwGraphicsWin32);
}

/*----------------------------------------------------------------------------------------------
	Set a clip rectangle, if it is not workable to read it from the HDC (e.g., because
	it is a metafile).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwGraphics::SetClipRect(RECT * prcClip)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prcClip);

	//return (HRESULT)GrGraphics::SetClipRect(prcClip);

	m_rcClip = *prcClip;

	END_COM_METHOD(g_fact, IID_IVwGraphicsWin32);
}

STDMETHODIMP VwGraphics::GetTextStyleContext(HDC * pContext)
{
	BEGIN_COM_METHOD;
	ThrowHr(E_NOTIMPL);
	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/***********************************************************************************************
	Utility methods
***********************************************************************************************/

int VwGraphics::GetXInch()
{
	if (!m_xInch)
	{
		m_xInch = ::GetDeviceCaps(m_hdcMeasure, LOGPIXELSX);
	}
	return m_xInch;
}

int VwGraphics::GetYInch()
{
	if (!m_yInch)
	{
		m_yInch = ::GetDeviceCaps(m_hdcMeasure, LOGPIXELSY);
	}
	return m_yInch;
}

/*----------------------------------------------------------------------------------------------
	Set the requested font. Discard the current one, if any.
----------------------------------------------------------------------------------------------*/
void VwGraphics::SetFont(HFONT hfont)
{
	BOOL fSuccess;
	if (hfont == m_hfont)
		return;
	// Select the new font into the device context
	HFONT hfontPrev = AfGdi::SelectObjectFont(m_hdc, hfont);
	HFONT hfontPrevM = 0; // for measure DC
	if (m_hdc != m_hdcMeasure)
	{
		hfontPrevM = AfGdi::SelectObjectFont(m_hdcMeasure, hfont);
		// If this is the very first font we have selected into the measure DC, we need
		// to remember it and restore it when done. All other fonts in the measure DC
		// are the same ones whose lifetime is being managed for the main DC.
		if (!m_hfontOldMeasure)
			m_hfontOldMeasure = hfontPrevM;
	}
	if (!hfontPrev)
		ThrowHr(WarnHr(E_FAIL));

	if (m_hfontOld)
	{
		// We have previously created a font and now need to delete it.
		// NB this must be done after it is selected out of the DC, or we get a hard-to-find
		// GDI memory leak that causes weird drawing failures on W-98.
		Assert(m_hfont);
		fSuccess = AfGdi::DeleteObjectFont(m_hfont);
	}
	else
	{
		// This is the first font selection we have made into this level; save the old one
		// to eventually select back into the DC before we RestoreDC.
		m_hfontOld = hfontPrev;
	}
	m_hfont = hfont;
}

/*----------------------------------------------------------------------------------------------
	Everything that needs an HDC calls this to check it is present. For now, if it isn't, we
	consider it an internal error on the part of the client.
----------------------------------------------------------------------------------------------*/
void VwGraphics::CheckDc()
{
	if (!m_hdc)
	{
		Assert(false);
		ThrowInternalError(E_INVALIDARG, "Must initialize");
	}
}

/*----------------------------------------------------------------------------------------------
	Everything that needs an HDC calls this to check it is present. For now, if it isn't, we
	consider it an internal error on the part of the client.
----------------------------------------------------------------------------------------------*/
void VwGraphics::MyGetClipRect(RECT * prc)
{
	if (!m_rcClip.IsClear())
	{
		*prc = m_rcClip;
		return;
	}
	if (GetClipBox(m_hdc, prc) == ERROR)
	{

		LPVOID lpMsgBuf;
		DWORD dw = GetLastError();

		FormatMessage(
			FORMAT_MESSAGE_ALLOCATE_BUFFER |
			FORMAT_MESSAGE_FROM_SYSTEM |
			FORMAT_MESSAGE_IGNORE_INSERTS,
			NULL,
			dw,
			MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
			(LPTSTR) &lpMsgBuf,
			0, NULL );

		// Display the error message and exit the process
		StrUni stuMsg;
		stuMsg.Format(L"Could not get clip rectangle: %s", lpMsgBuf);
		LocalFree(lpMsgBuf);

		ThrowInternalError(E_FAIL, stuMsg.Chars());
	}
}

inline unsigned short endian_swap(unsigned short x)
{
	return (x>>8) | (x<<8);
}

inline short endian_swap(short x)
{
	return static_cast<short>(endian_swap(static_cast<unsigned short>(x)));
}

// TT_TAG is Big Endian!
#define MAKE_TT_TAG(x1, x2, x3, x4) (((DWORD)x4 << 24) | ((DWORD)x3 << 16) | ((DWORD)x2 << 8) | (DWORD) x1)

inline byte * VwGraphics::MyGetOS2Table(){
	return MyGetFontTable(MAKE_TT_TAG('O','S','/','2')/*OS/2 table number*/);
}
byte * VwGraphics::MyGetFontTable(DWORD nTableId){
	DWORD cbTableSz = ::GetFontData(m_hdcMeasure, nTableId, 0, NULL, 0);
	if (GDI_ERROR == cbTableSz)
		return NULL; // no such table
	byte * prgb = new byte[cbTableSz];

	if (::GetFontData(m_hdcMeasure, nTableId, 0, (void *)prgb, cbTableSz) == GDI_ERROR)
	{
		delete prgb;
		return NULL;
	}

	return prgb;
}
// Assumes the normal font is in the device context.
STDMETHODIMP VwGraphics::GetSuperscriptHeightRatio(int* piNumerator, int* piDenominator){
	BEGIN_COM_METHOD;
	ChkComArgPtr(piNumerator);
	ChkComArgPtr(piDenominator);

	// First try to get from the font
	byte * pOS2 = VwGraphics::MyGetOS2Table();
	if (pOS2)
	{
		short *piSuperScriptYSize = reinterpret_cast<short*>(&pOS2[20]);
		*piNumerator = endian_swap(*piSuperScriptYSize);
		*piDenominator = GetFontHeightFromFontTable();
		delete pOS2;
	}
	else {
		*piNumerator = 0;
		*piDenominator = 0;
	}

	// If that fails, set the superscript size = 66% of the font size
	if(!*piNumerator || !*piDenominator)
	{
		*piNumerator = 2;
		*piDenominator = 3;
	}

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

// Assumes the normal font is in the device context.
STDMETHODIMP VwGraphics::GetSuperscriptYOffsetRatio(int* piNumerator, int* piDenominator){
	BEGIN_COM_METHOD;
	ChkComArgPtr(piNumerator);
	ChkComArgPtr(piDenominator);

	// First try to get from the font
	byte * pOS2 = VwGraphics::MyGetOS2Table();
	if (pOS2)
	{
		short *piSuperScriptYOffset = reinterpret_cast<short*>(&pOS2[24]);
		*piNumerator = endian_swap(*piSuperScriptYOffset);
		*piDenominator = GetFontHeightFromFontTable();
		delete pOS2;
	}
	else {
		*piNumerator = 0;
		*piDenominator = 0;
	}

	// If that fails, set the superscript offset = font height - superscript font height
	if(!*piNumerator || !*piDenominator)
	{
		int nSuperscriptHeight;
		int nHeight;
		CheckHr(GetSuperscriptHeightRatio(&nSuperscriptHeight, &nHeight));
		*piNumerator = nHeight - nSuperscriptHeight;
		*piDenominator = nHeight;
	}

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

// Assumes the normal font is in the device context.
STDMETHODIMP VwGraphics::GetSubscriptHeightRatio(int* piNumerator, int* piDenominator){
	BEGIN_COM_METHOD;
	ChkComArgPtr(piNumerator);
	ChkComArgPtr(piDenominator);

	// First try to get from the font
	byte * pOS2 = VwGraphics::MyGetOS2Table();
	if (pOS2)
	{
		short *piSubscriptYSize = reinterpret_cast<short*>(&pOS2[12]);
		*piNumerator = endian_swap(*piSubscriptYSize);
		*piDenominator = GetFontHeightFromFontTable();
		delete pOS2;
	}
	else {
		*piNumerator = 0;
		*piDenominator = 0;
	}

	// If that fails, set the superscript size = 66% of the font size
	if(!*piNumerator || !*piDenominator)
	{
		*piNumerator = 2;
		*piDenominator = 3;
	}

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

// Assumes the normal font is in the device context.
STDMETHODIMP VwGraphics::GetSubscriptYOffsetRatio(int* piNumerator, int* piDenominator){
	BEGIN_COM_METHOD;
	ChkComArgPtr(piNumerator);
	ChkComArgPtr(piDenominator);

	// First try to get from the font
	byte * pOS2 = VwGraphics::MyGetOS2Table();
	if (pOS2)
	{
		short *piSubscriptYOffset = reinterpret_cast<short*>(&pOS2[16]);
		*piNumerator = endian_swap(*piSubscriptYOffset);
		*piDenominator = GetFontHeightFromFontTable();
		delete pOS2;
	}
	else {
		*piNumerator = 0;
		*piDenominator = 0;
	}

	// If that fails, set the subscript offset = 1/5 of subscript font height
	if(!*piNumerator || !*piDenominator)
	{
		int nSubscriptHeight;
		int nHeight;
		CheckHr(GetSubscriptHeightRatio(&nSubscriptHeight, &nHeight));
		*piNumerator = nSubscriptHeight;
		*piDenominator = nHeight * 5;
	}

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

int VwGraphics::GetFontHeightFromFontTable()
{
	byte * pHorizontalHeaderTable = MyGetFontTable(MAKE_TT_TAG('h','h','e','a'));
	if(!pHorizontalHeaderTable)
	{
		ThrowInternalError(E_UNEXPECTED, "Can't find required hhea table in Font");
	}
	short *piyAscender = reinterpret_cast<short*>(&pHorizontalHeaderTable[4]);  //yAscender
	short *piyDescender = reinterpret_cast<short*>(&pHorizontalHeaderTable[6]); //yDescender this is a negative value
	short *piyLineGap = reinterpret_cast<short*>(&pHorizontalHeaderTable[8]); //yLineGap
	return endian_swap(*piyAscender) - endian_swap(*piyDescender) + endian_swap(*piyLineGap);
}

#include <vector_i.cpp>
template Vector<HFONT>; // VecHfont;

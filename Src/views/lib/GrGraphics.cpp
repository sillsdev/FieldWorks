/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

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
#include "Main.h"
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

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
GrGraphics::GrGraphics()
{
	m_hdc = NULL;
	m_cref = 1;
	Init();
	m_hfont = NULL;
	m_hfontOld = NULL;
	m_hfontOldMeasure = NULL;
}

/*----------------------------------------------------------------------------------------------
	Initialize a new instance.
----------------------------------------------------------------------------------------------*/
void GrGraphics::Init()
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

//#ifdef BASELINE
//GrGraphics::GrGraphics(SilTestSite *psts, bool fDraw, bool fFile)
//{
//	m_cref = 1;
//	ModuleEntry::ModuleAddRef();
//
//	m_psts = psts;
//	m_fDraw = fDraw;
//	m_fFile = fFile;
//}
//#endif

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
GrGraphics::~GrGraphics()
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

	ModuleEntry::ModuleRelease();
}


/***********************************************************************************************
	IVwGraphics Interface Methods
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Invert the specified rectangle.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::InvertRect(int xLeft, int yTop, int xRight, int yBottom)
{
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
		return kresOk;
	}

	if (!::InvertRect(m_hdc, &rect))
		return WarnHr(kresFail);

#ifdef BASELINE
	}
#endif
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Set the foreground color used for lines, text
	Arguments:
		nRGB			RGB color value
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::put_ForeColor(int nRGB)
{
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
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Background color, used for shape interior, text background
	Arguments:
		nRGB			RGB color value or kclrTransparent
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::put_BackColor(int nRGB)
{
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
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Draw a rectangle, filled with the current background color
	ENHANCE: should we outline it in the foreground color?
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::DrawRectangle(int xLeft, int yTop,	int xRight, int yBottom)
{
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
		return kresOk;
	}

	{
		SmartPalette spal(m_hdc);
		AfGfx::FillSolidRect(m_hdc, rect, m_rgbBackColor);
	}

#ifdef BASELINE
	}
#endif
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Draw a line!
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::DrawLine(int xStart, int yStart, int xEnd, int yEnd)
{
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
		return kresOk;
	if (yStart > rectClip.bottom && yEnd > rectClip.bottom)
		return kresOk;

	// Typically this is one pixel; something similar on e.g. printer
	int dzpThick = (GetXInch() + GetYInch()) / (96 * 2);
	PenWrap xpwr(PS_SOLID, dzpThick, m_rgbForeColor, m_hdc);

	if (!::MoveToEx(m_hdc,xStart, yStart, NULL))
		ThrowInternalError(kresUnexpected, "Can't do GDI MoveToEx");
	if (!::LineTo(m_hdc,xEnd, yEnd))
		ThrowInternalError(kresUnexpected, "Can't Draw line");

#ifdef BASELINE
	}
#endif
	return kresOk;
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
GrResult GrGraphics::DrawHorzLine(int xLeft, int xRight, int y, int dyHeight,
	int cdx, int * prgdx, int * pdxStart)
{
	ChkGrArrayArg(prgdx, cdx);
	ChkGrArgPtr(pdxStart);

	// if cdx is 0, then that is the same as a single segment of length MAXINT
	// so, fix up the segment point
	int dxMaxSegment = MAXINT;
	if (cdx == 0)
	{
		cdx = 1;
		prgdx = &dxMaxSegment;
	}

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
		return kresOk;

	if (xRight < xLeft)
		return kresInvalidArg;
	if (xRight == xLeft)
		return kresOk;

	PenWrap xpwr(PS_SOLID, dyHeight, m_rgbForeColor, m_hdc);

	if (cdx == 0)
		ThrowHr(E_INVALIDARG) ;

	int * pdxLim = prgdx + cdx;
	// Compute total length of pattern
	int dxPattern = 0;
	int * pdx = prgdx;
	for (; pdx < pdxLim; pdx++)
		dxPattern += *pdx;
	int dxStartOffset = 0;
	if (dxPattern != 0)
		dxStartOffset = *pdxStart - (*pdxStart / dxPattern) * dxPattern;
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
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Draw a piece of text (below and to the right of the specified point)
	OPTIMIZE: for performance, should we make the caller responsible to ensure that only text
	reasonably near the clipping box gets drawn?
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::DrawText(int x, int y, int cch, const OLECHAR * prgch, int nStretch)
{
	ChkGrArrayArg(prgch, cch);
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
		return kresOk;

	// check whether the text is visible, at least vertically
	RECT rectClip;
	MyGetClipRect(&rectClip);
	if (y > rectClip.bottom)
		return kresOk;
	if (y < rectClip.top - MulDiv(500, kdzptInch, m_yInch))	// ENHANCE: correct if we support 500+ pt fonts
		return kresOk;

	if (nStretch)
	{
		// ENHANCE: when everyone is using NT, we could use GetCharacterPlacement to
		// distribute stretch. Would that work better, especially if the text
		// lacks break characters? For now this is not an option, because
		// Win-95 and Win-98 lack a wide-char version of GetCharacterPlacement.
		TEXTMETRIC tm; // to find out the break character for the font.
		if (!::GetTextMetrics(m_hdcMeasure, &tm))
			ThrowInternalError(kresUnexpected, "GetTextMetrics failed");
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
				ThrowInternalError(kresUnexpected, "SetJustification failed");
		}
		// if there are no break characters the extra space will be at the end.
		// Should we do something about not assigning stretch to runs with no
		// break characters?
	}
	BOOL fOK = ::ExtTextOutW(m_hdc, x, y,
		0, 0,			// no extra clipping
		prgch, cch,
		0);				// no special character spacing
	if (!fOK)
	{
		int gle = GetLastError();
		// We do this additional check because, if printing is cancelled, it may be that the
		// cancel happens DURING the call to ExtTextOut. It seems that in such a case, Windows
		// does not complete drawing the string which we asked it to, and returns FALSE. But,
		// getting no gle indicates that in fact nothing is wrong.
		if (gle != 0)
			ThrowInternalError(kresUnexpected, "ExtTextOutW failed");
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
//	return kresOk;
#endif
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Draw a list of glyphs that are all positioned vertically the same relative to the baseline,
	given their offsets from each other.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::DrawTextExt(int x, int y, int cgid, const OLECHAR __RPC_FAR * prggid,
	UINT uOptions, const RECT __RPC_FAR * pRect, int __RPC_FAR * prgdx)
{
	ChkGrArrayArg(prggid, cgid);
	ChkGrArgPtrN(pRect);
	if (prgdx == NULL)
	{
		Assert(cgid <= 1);
		if (cgid > 1)
			ThrowInternalError(kresUnexpected);
	}
	else
	{
		ChkGrArrayArg(prgdx, cgid);
	}

#ifdef BASELINE
	if (m_fFile)
	{
		RECT rect;
		StrAnsi staChar;
		staChar.Assign(prggid, cgid);
		StrAnsi stargdx("{");
		for (int i = 0; i < cgid; i++)	{
			if (i == cch-1)
				stargdx.FormatAppend("%d}", *(prgdx + i));
			else
				stargdx.FormatAppend("%d,", *(prgdx + i));
		}
		if (pRect)
			rect = *pRect;
		else	{
			rect.left = 0;
			rect.top = 0;
			rect.right = 0;
			rect.bottom = 0;
		}
		m_psts->OutputFormat("DrawTextExt(%d, %d, %d, \"%s\", %d, {%d, %d, %d, %d}, \"%s\")\n", x, y, cgid,
			staChar.Chars(), uOptions, rect.left, rect.top, rect.right, rect.bottom,
			stargdx.Chars());
	}
	if (m_fDraw)
	{
#endif

	CheckDc();

	// check whether the text is visible, at least vertically
	RECT rectClip;
	MyGetClipRect(&rectClip);
	if (y > rectClip.bottom)
		return kresOk;
	if (y < rectClip.top - 10000)	// ENHANCE: correct if we support 500+ pt fonts
		return kresOk;

	// must explicitly call W API so get wide version on Win 9x
	if (!::ExtTextOutW(m_hdc, x, y, uOptions, pRect, prggid, cgid, prgdx))
		ThrowInternalError(kresUnexpected, "ExtTextOutW failed");

#ifdef BASELINE
	}
#endif
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Measure the given text.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::GetTextExtent(int cch, const OLECHAR * prgch, int * pdx, int * pdy)
{
	ChkGrArrayArg(prgch, cch);
	ChkGrArgPtr(pdx);
	ChkGrArgPtr(pdy);
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
		ThrowInternalError(kresUnexpected, "GetTextExtentPoint32W failed");
	}
	else
	{
		//The windows routine does not work for zero-length strings; it apparently
		//gives a reasonable width of 0, but a completely unreasonable height
		//that is a large negative number.
		TEXTMETRIC tm;
		if (!::GetTextMetrics(m_hdcMeasure, &tm))
			ThrowInternalError(kresUnexpected, "GetTextMetrics failed");
		size.cx = 0;
		size.cy = tm.tmHeight;
	}
	*pdx = size.cx;
	*pdy = size.cy;

	return kresOk;
}
/*----------------------------------------------------------------------------------------------
	Given that the text indicatd by cch and prgch was drawn with the given stretch, compute
	the width up to (but not including) character ich.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::GetTextLeadWidth(int cch, const OLECHAR * prgch, int ich,
	int dxStretch, int * pdx)
{
	ChkGrArrayArg(prgch, cch);
	ChkGrOutPtr(pdx);
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
		ThrowInternalError(kresInvalidArg, "ich out of range");
	if (!ich)
		return kresOk;
	SIZE size;
	if (!::GetTextExtentPoint32W(m_hdcMeasure, prgch, ich, &size))
		ThrowInternalError(kresUnexpected, "GetTextExtentPoint32W failed");
	if (dxStretch)
	{
		// position may need to be increased. It depends how much stretch is inserted before
		// the relevant position
		TEXTMETRIC tm; // to find out the break character for the font.
		if (!::GetTextMetrics(m_hdcMeasure, &tm))
			ThrowInternalError(kresUnexpected, "GetTextMetrics failed");
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
			return kresOk;
		}
		int dxStretchPrev = dxStretch * cbrkPrev / cbrk;  // even distribution of extra space
		*pdx = size.cx + dxStretchPrev;
		return kresOk;
	} else {
		*pdx = size.cx;
		return kresOk;
	}
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Get a rectangle that bounds the area to be drawn. (Some further parts of it may be clipped)
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::GetClipRect(int * pxLeft, int * pyTop, int * pxRight, int * pyBottom)
{
	ChkGrArgPtr(pxLeft);
	ChkGrArgPtr(pyTop);
	ChkGrArgPtr(pxRight);
	ChkGrArgPtr(pyBottom);

	CheckDc();

	RECT rect;
	MyGetClipRect(&rect);
	*pxLeft = rect.left;
	*pyTop = rect.top;
	*pxRight = rect.right;
	*pyBottom = rect.bottom;

	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Get the number of logical units in the currently selected font's em square.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::GetFontEmSquare(int * pxyFontEmSquare)
{
	ChkGrArgPtr(pxyFontEmSquare);

	TEXTMETRIC tm;
	if (!::GetTextMetrics(m_hdcMeasure, &tm))
		ThrowInternalError(kresUnexpected, "GetTextMetrics failed");
	*pxyFontEmSquare = tm.tmHeight - tm.tmInternalLeading;

	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Get glyph metrics for a glyph in the currently selected font. chw must be a glyph id.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::GetGlyphMetrics(int chw,
	int * pxBoundingWidth, int * pyBoundingHeight,
	int * pxBoundingX, int * pyBoundingY, int * pxAdvanceX, int * pyAdvanceY)
{
	ChkGrArgPtr(pxBoundingWidth);
	ChkGrArgPtr(pyBoundingHeight);
	ChkGrArgPtr(pxBoundingX);
	ChkGrArgPtr(pyBoundingY);
	ChkGrArgPtr(pxAdvanceX);
	ChkGrArgPtr(pyAdvanceY);
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
		ThrowInternalError(kresUnexpected, "GetGlyphOutline failed");
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

	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Get a table out of the font. Device independent.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::GetFontData(int nTableId, int * pcbTableSz, byte * prgb, int cbMax)
{
	ChkGrArgPtr(pcbTableSz);
	ChkGrArrayArg(prgb, cbMax);

	*pcbTableSz = -1;

	DWORD cbTableSz = ::GetFontData(m_hdcMeasure, nTableId, 0, NULL, 0);
	if (GDI_ERROR == cbTableSz)
		return kresFail; // no such table
	*pcbTableSz = cbTableSz;
	if (*pcbTableSz > cbMax)
		return kresFalse; // not enough room in buffer
	if (::GetFontData(m_hdcMeasure, nTableId, 0, (void *)prgb, cbTableSz) == GDI_ERROR)
	{
		THROW(kresUnexpected);
	}

	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Get coordinates for a point in a glyph.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::XYFromGlyphPoint(int gid, int nPoint, int * pxRet, int * pyRet)
{
	ChkGrOutPtr(pxRet);
	ChkGrOutPtr(pyRet);

#ifdef BASELINE
	if (m_fFile)
		m_psts->OutputFormat("XYFromGlyphpoint(%d, %d, ptr, ptr)\n", gid, nPoint);
#endif

	CheckDc();

	GLYPHMETRICS gm;
	const MAT2 mat2 = {{0,1}, {0,0}, {0,0}, {0,1}};
	WORD flag = GGO_GLYPH_INDEX | GGO_NATIVE;

	UINT cbBuf = ::GetGlyphOutline(m_hdcMeasure, gid, flag, &gm, 0, NULL, &mat2);
	if (cbBuf <= 0)
		ThrowInternalError(kresUnexpected, "First GetGlyphOutline in XYFromGlyphPoint failed");
	byte * pGlyphData = NewObj byte[cbBuf];
	if (!pGlyphData)
		ThrowOutOfMemory();
	if (GDI_ERROR == ::GetGlyphOutline(m_hdcMeasure, gid, flag, &gm, cbBuf, pGlyphData,&mat2))
		ThrowInternalError(kresUnexpected, "Second GetGlyphOutline in XYFromGlyphPoint failed");

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
		return kresOk;
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
			return kresOk;
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
						return kresOk;
					}
				}
				else
				{
					delete [] pGlyphData;
					ThrowInternalError(kresUnexpected);
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
			ThrowInternalError(kresUnexpected);
		}
		pPolyHdr = (TTPOLYGONHEADER *)((byte *)pPolyHdr + pPolyHdr->cb);
	}

	delete [] pGlyphData;
	ThrowInternalError(kresUnexpected);
	return kresUnexpected;
}

/*----------------------------------------------------------------------------------------------
	Get the ascent of the currently selected font, in logical units.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::get_FontAscent(int* pdy)
{
	ChkGrArgPtr(pdy);
	CheckDc();

	TEXTMETRIC tm;
	if (!::GetTextMetrics(m_hdcMeasure, &tm))
		ThrowInternalError(kresUnexpected, "GetTextMetrics failed");
	*pdy = tm.tmAscent;
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Get the descent of the currently selected font, in logical units.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::get_FontDescent(int * pdyRet)
{
	ChkGrArgPtr(pdyRet);
	CheckDc();

	TEXTMETRIC tm;
	if (!::GetTextMetrics(m_hdcMeasure, &tm))
		ThrowInternalError(kresUnexpected, "GetTextMetrics failed");
	*pdyRet = tm.tmDescent;
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Release the DC and set pointer to null.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::ReleaseDC()
{
	if (m_hdc)
	{
		BOOL fSuccess;
		if (m_hfontOld)
		{
			// We have called SetFont at least once; the most recent font selected
			// is in m_hfont and is also the current one in the DC. Select the
			// original one back into the DC to prevent GDI memory leaks and similar problems.
			HFONT hfontPrev = AfGdi::SelectObjectFont(m_hdc, m_hfontOld, AfGdi::OLD);
#ifdef DEBUG
			// TODO: We used to have an assert here, but for some reason when running from TE
			// this fails sometimes: hfontPrev has the value it had before we set
			// the font in SetFont(). We couldn't find any reason for that. However,
			// ignoring the assert seems not to break anything.
			//Assert(m_hfont == hfontPrev);
			if (m_hfont != hfontPrev)
				_CrtDbgReport(_CRT_WARN, NULL, 0, NULL, "GrGraphics::ReleaseDC: m_hfont != hfontPrev\n");
			// Assert(hfontPrev != m_hfontOld);
			if (hfontPrev == m_hfontOld)
				_CrtDbgReport(_CRT_WARN, NULL, 0, NULL, "GrGraphics::ReleaseDC: hfontPrev == m_hfontOld\n");
#endif
			m_hfont = 0;
			fSuccess = AfGdi::DeleteObjectFont(hfontPrev);
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
				"GrGraphics::ReleaseDC: ::RestoreDC failed: error=%d - %S\n", nError, lpMsgBuf);
			::LocalFree(lpMsgBuf);
		}
#endif
	}
	m_hdc = NULL;
	m_hdcMeasure = NULL;
	Assert(m_vhrgnClipStack.Size() == 0); // Make sure pushes and pops match.

	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Get current actual or simulated X resolution.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::get_XUnitsPerInch(int * pxInch)
{
	ChkGrArgPtr(pxInch);
	*pxInch = GetXInch();
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Set (simulated) X resolution. Setting to zero restores actual device resolution.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::put_XUnitsPerInch(int xInch)
{
#ifdef BASELINE
	if (m_fFile)
		m_psts->OutputFormat("put_XUnitsPerInch(%d)\n", xInch);
#endif

	m_xInch = xInch;
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Get current actual or simulated Y resolution.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::get_YUnitsPerInch(int * pyInch)
{
	ChkGrArgPtr(pyInch);
	*pyInch = GetYInch();
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Set (simulated) Y resolution. Setting to zero restores actual device resolution.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::put_YUnitsPerInch(int yInch)
{
#ifdef BASELINE
	if (m_fFile)
		m_psts->OutputFormat("put_YUnitsPerInch(%d)\n", yInch);
#endif

	m_yInch = yInch;
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Set up to draw text using the properties specified.
	super/subscript are ignored, as is baseline adjust; client is
	presumed to have handled them. Sets colors and HFONT.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::SetupGraphics(LgCharRenderProps * pchrp)
{
	ChkGrArgPtr(pchrp);
#ifdef BASELINE
//	if (m_fFile)
//		m_psts->OutputFormat("SetupGraphics(ptr) fItalic: %d, fBold: %d, dympHeight"
//			": %d, szFaceName: \"%s\", clrFore: %d, clrBack: %d\n", pchrp->fItalic,
//			pchrp->fBold, pchrp->dympHeight, pchrp->szFaceName, pchrp->clrFore, pchrp->clrBack);
#endif

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
				wcscpy(lf.lfFaceName, pchrp->szFaceName);
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
			ThrowHr(WarnHr(kresFail));
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
		ThrowHr(WarnHr(kresFail));
#endif
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Fill in the argument with information about the properties that are selected into
	the device context. Assumes SetupGraphics has been called.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::get_FontCharProperties(LgCharRenderProps * pchrp)
{
	memcpy(pchrp, &m_chrp, isizeof(LgCharRenderProps));
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Set up the graphics object to use a particular HDC.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::Initialize(HDC hdc)
{
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

	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Recover the last hDC passed to Initialize.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::GetDeviceContext(HDC * phdc)
{
	ChkGrArgPtr(phdc);
	*phdc = m_hdc;
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Invoke a new clipping rectangle; the previous clipping state can be
	restored using PopClipRect.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::PushClipRect(RECT rcClip)
{
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
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Restore the previous clipping rectangle.
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::PopClipRect()
{
	// If *m_vhrgnClipStack.Top() is null, SelectClipRgn() will remove the clipping region.
	int iSuccess = ::SelectClipRgn(m_hdc, *m_vhrgnClipStack.Top());
	if (!iSuccess || ERROR == iSuccess)
		ThrowHr(WarnHr(E_FAIL));

	if (*m_vhrgnClipStack.Top())
		::DeleteObject(*m_vhrgnClipStack.Top());
	m_vhrgnClipStack.Pop();
	return kresOk;
}

/*----------------------------------------------------------------------------------------------
	Set a separate DC to be used for measuring operations (generally whatever is not
	supported in case the output DC is a metafile).
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::SetMeasureDc(HDC hdc)
{
	m_hdcMeasure = hdc;
	return kresOk;
}
/*----------------------------------------------------------------------------------------------
	Set a clip rectangle, if it is not workable to read it from the HDC (e.g., because
	it is a metafile).
----------------------------------------------------------------------------------------------*/
GrResult GrGraphics::SetClipRect(RECT * prcClip)
{
	m_rcClip = *prcClip;
	return kresOk;
}

/***********************************************************************************************
	Utility methods
***********************************************************************************************/

int GrGraphics::GetXInch()
{
	if (!m_xInch)
	{
		m_xInch = ::GetDeviceCaps(m_hdcMeasure, LOGPIXELSX);
	}
	return m_xInch;
}

int GrGraphics::GetYInch()
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
void GrGraphics::SetFont(HFONT hfont)
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
		ThrowHr(WarnHr(kresFail));

	if (m_hfontOld)
	{
		// We have previously created a font and now need to delete it.
		// NB this must be done after it is selected out of the DC, or we get a hard-to-find
		// GDI memory leak that causes weird drawing failures on W-98.
		Assert(m_hfont);
#ifdef DEBUG
		// TODO: We used to have an assert here, but for some reason when running from TE
		// this fails sometimes: hfontPrev has the value it had before we set
		// the font in SetFont(). We couldn't find any reason for that. However,
		// ignoring the assert seems not to break anything.
		//Assert(m_hfont == hfontPrev);
		if (m_hfont != hfontPrev)
			_CrtDbgReport(_CRT_WARN, NULL, 0, NULL, "GrGraphics::SetFont: m_hfont != hfontPrev\n");
#endif
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
void GrGraphics::CheckDc()
{
	if (!m_hdc)
	{
		Assert(false);
		ThrowInternalError(kresInvalidArg, "Must initialize");
	}
}

/*----------------------------------------------------------------------------------------------
	Everything that needs an HDC calls this to check it is present. For now, if it isn't, we
	consider it an internal error on the part of the client.
----------------------------------------------------------------------------------------------*/
void GrGraphics::MyGetClipRect(RECT * prc)
{
	if (m_rcClip.Width())
	{
		*prc = m_rcClip;
		return;
	}
	if (GetClipBox(m_hdc, prc) == ERROR)
		ThrowInternalError(E_FAIL, L"Could not get clip rectangle");
}


#include <vector_i.cpp>
template Vector<HRGN>; // VecHRgn;

#include "stdafx.h"
#include "GrClient.h"
#include "GrGraphics.h"

// handle Windows specific way of returning coordinates for glyph points
static int IntFromFixed(FIXED fx)
{
	if (fx.fract >= 0x8000)
		return fx.value + 1;
	else
		return fx.value;
}

GrGraphics::GrGraphics()
{
	m_hdc = 0;
	m_hfont = 0;
	memset (&m_chrp, '\0', sizeof(LgCharRenderProps));

	// Vertical and horizontal resolution. Zero indicates not yet initialized.
	m_yInch = 0;
}

GrGraphics::GrGraphics(const GrGraphics& grfx)
{
	m_hdc = grfx.m_hdc;
	m_hfont = grfx.m_hfont;
	memset (&m_chrp, '\0', sizeof(LgCharRenderProps));
	m_chrp = grfx.m_chrp;
	// Vertical and horizontal resolution. Zero indicates not yet initialized.
	m_yInch = grfx.m_yInch;

	for(int i=0;i<32;i++)
		m_szFaceName[i] = grfx.m_szFaceName[i];
}

GrGraphics::~GrGraphics()
{
	DeleteObject(SelectObject(m_hdc, GetStockObject(ANSI_VAR_FONT)));
}

GrResult GrGraphics::Initialize(HDC hdc)
{
	m_hdc = hdc;
	if (m_hfont != 0) // Setting a new DC, we should not have any saved font info.
		return kresUnexpected;
	SetMapMode(hdc, MM_TEXT);

	// For the sake of being definite; rarely makes a difference; I think this is default
	SetPolyFillMode(hdc, ALTERNATE);

	// Need to set background mode to transparent so Graphite can draw overlapping glyphs
	SetBkMode(hdc, TRANSPARENT);

	int i;
	get_YUnitsPerInch(&i); //initialize m_yInch

	return kresOk;
}

GrResult GrGraphics::SetFont(HFONT hfont)
{
	if (hfont == m_hfont)
		return kresOk;

	HFONT hfontPrev = (HFONT) SelectObject(m_hdc, hfont);
	if (!hfontPrev)
		return kresUnexpected;
	DeleteObject(hfontPrev);

	if (m_hfont)
	{
		DeleteObject(m_hfont);
	}
	m_hfont = hfont;

	//save the font name which will always be used in this obj
	int cbOtm = GetOutlineTextMetrics(m_hdc, 0, NULL);
	OUTLINETEXTMETRIC * pOtm = (OUTLINETEXTMETRIC *)new BYTE[cbOtm];
	if (!GetOutlineTextMetrics(m_hdc, cbOtm, pOtm))
		return kresUnexpected;
	wcscpy(m_szFaceName, (wchar_t*) ((char *)pOtm + (int)pOtm->otmpFaceName));
	delete pOtm;
	return kresOk;
}

GrResult GrGraphics::SetupGraphics(LgCharRenderProps * pchrp)
{
	memcpy(&m_chrp, pchrp, sizeof(LgCharRenderProps));

	// set the font
	LOGFONT lf;
	memset(&lf, '\0', sizeof(LOGFONT));
	lf.lfItalic = pchrp->ttvItalic == kttvOff ? false : true;
	lf.lfWeight = pchrp->ttvBold == kttvOff ? 400 : 700;
	lf.lfHeight = -MulDiv(pchrp->dympHeight, m_yInch, 72000); // convert millipoints to pixels
	lf.lfUnderline = false;
	lf.lfWidth = 0;			// default width, based on height
	lf.lfEscapement = 0;	// no rotation of text (is this how to do slanted?)
	lf.lfOrientation = 0;	// no rotation of character baselines

	lf.lfStrikeOut = 0;		// not strike-out
	lf.lfCharSet = DEFAULT_CHARSET;			// let name determine it; WS should specify valid
	lf.lfOutPrecision = OUT_TT_ONLY_PRECIS;	// only work with TrueType fonts
	lf.lfClipPrecision = CLIP_DEFAULT_PRECIS; // ??
	lf.lfQuality = DRAFT_QUALITY; // don't think this matters for TrueType fonts.
	lf.lfPitchAndFamily = 0; // must be zero for EnumFontFamiliesEx
	wcscpy(lf.lfFaceName, m_szFaceName); // ignore pchrp->szFaceName;

	HFONT hfont;
	hfont = CreateFontIndirect(&lf);
	if (!hfont)
		return kresUnexpected;
	SetFont(hfont);

	//!!! set the colors.
	bool fOK = (SetTextColor(m_hdc, pchrp->clrFore) != CLR_INVALID);

	if(pchrp->clrBack == kclrTransparent)
	{
		// I can't find it documented anywhere, but it seems to be necessary to set
		// the background color to black to make TRANSPARENT mode work--at least on my
		// computer.
		fOK = fOK && (SetBkColor(m_hdc, RGB(0,0,0)) != CLR_INVALID);
		fOK = fOK && SetBkMode(m_hdc, TRANSPARENT);
	} else
	{
		fOK = fOK && (SetBkColor(m_hdc, pchrp->clrBack)!= CLR_INVALID);
		fOK = fOK && SetBkMode(m_hdc, OPAQUE);
	}
	if (!fOK)
		return kresFail;
	else
		return kresOk;
}

GrResult GrGraphics::InvertRect(int xLeft, int yTop, int xRight, int yBottom)
{
	RECT rect;
	rect.left = xLeft;
	rect.top = yTop;
	rect.right = xRight;
	rect.bottom = yBottom;

	if (!::InvertRect(m_hdc, &rect))
		return kresFail;

	return kresOk;
}

GrResult GrGraphics::DrawTextExt(int x, int y, int cch, const OLECHAR * prgchw,
UINT uOptions, const RECT * pRect, int * prgdx)
{
	if (prgdx == NULL)
	{
		if (cch > 1)
			return kresInvalidArg;
	}

	// must explicitly call W API so get wide version on Win 9x
	if (!ExtTextOutW(m_hdc, x, y, uOptions, pRect, prgchw, cch, prgdx))
		return kresUnexpected;

	return kresOk;
}

GrResult GrGraphics::GetFontEmSquare(int * pxyFontEmSquare)
{
	TEXTMETRIC tm;
	if (!GetTextMetrics(m_hdc, &tm))
		return kresUnexpected;
	*pxyFontEmSquare = tm.tmHeight - tm.tmInternalLeading;

	return kresOk;
}

GrResult GrGraphics::GetGlyphMetrics(int chw,
	int * pxBoundingWidth, int * pyBoundingHeight,
	int * pxBoundingX, int * pyBoundingY, int * pxAdvanceX, int * pyAdvanceY)
{
	GLYPHMETRICS gm;
	const MAT2 mat2 = {{0,1}, {0,0}, {0,0}, {0,1}};
	if (GDI_ERROR == ::GetGlyphOutline(m_hdc, chw, GGO_GLYPH_INDEX | GGO_METRICS,
		&gm, 0, NULL, &mat2))
	{
		return kresUnexpected;
	}

	*pxBoundingWidth = gm.gmBlackBoxX;
	*pyBoundingHeight = gm.gmBlackBoxY;
	*pxBoundingX = gm.gmptGlyphOrigin.x;
	*pyBoundingY = gm.gmptGlyphOrigin.y;
	*pxAdvanceX = gm.gmCellIncX;
	*pyAdvanceY = gm.gmCellIncY;

	return kresOk;
}

GrResult GrGraphics::GetFontData(int nTableId, int * pcbTableSz, byte * prgb, int cbMax)
{
	*pcbTableSz = -1;

	DWORD cbTableSz = ::GetFontData(m_hdc, nTableId, 0, NULL, 0);
	if (GDI_ERROR == cbTableSz)
		return kresFail;
	*pcbTableSz = cbTableSz;
	if (*pcbTableSz > cbMax)
		return kresFalse; // not enough room in buffer
	if (GDI_ERROR == ::GetFontData(m_hdc, nTableId, 0, (void *)prgb, cbTableSz))
	{
		return kresUnexpected;
	}

	return kresOk;
}

GrResult GrGraphics::XYFromGlyphPoint(int chw, int nPoint, int * pxRet, int * pyRet)
{
	GLYPHMETRICS gm;
	const MAT2 mat2 = {{0,1}, {0,0}, {0,0}, {0,1}};
	WORD flag = GGO_GLYPH_INDEX | GGO_NATIVE;

	UINT cbBuf = GetGlyphOutline(m_hdc, chw, flag, &gm, 0, NULL, &mat2);
	if (cbBuf <= 0)
		return kresUnexpected;
	BYTE * pGlyphData = new BYTE[cbBuf];
	if (!pGlyphData)
		return kresOutOfMemory;
	if (GDI_ERROR == GetGlyphOutline(m_hdc, chw, flag, &gm, cbBuf, pGlyphData,&mat2))
		return kresUnexpected;

	TTPOLYGONHEADER * pPolyHdr;
	TTPOLYCURVE * pPolyCurve;
	pPolyHdr = (TTPOLYGONHEADER *)pGlyphData;
	int nCurPoint;
	nCurPoint = 0;

	// the while loop below cannot handle getting the first point from the polygon header
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

	while ((BYTE *)pPolyHdr < (pGlyphData + cbBuf))
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
			while ((BYTE *)pPolyCurve < (BYTE *)pPolyHdr + pPolyHdr->cb)
			{
				if (pPolyCurve->wType == TT_PRIM_LINE || pPolyCurve->wType == TT_PRIM_QSPLINE)
				{
					int j = pPolyCurve->cpfx - 1; // index of last point in the curve
					if (pPolyCurve->wType == TT_PRIM_QSPLINE &&
						// test if this is the last curve
						pPolyHdr->cb - (int)((BYTE *)(&pPolyCurve->apfx[j]) - (BYTE *)(pPolyHdr))
							== sizeof POINTFX &&
						IntFromFixed(pPolyCurve->apfx[j].x) == IntFromFixed(pPolyHdr->pfxStart.x) &&
						IntFromFixed(pPolyCurve->apfx[j].y) == IntFromFixed(pPolyHdr->pfxStart.y))
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
					return kresUnexpected;
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
			return kresUnexpected;
		}
		pPolyHdr = (TTPOLYGONHEADER *)((BYTE *)pPolyHdr + pPolyHdr->cb);
	}

	delete [] pGlyphData;
	return kresUnexpected;
}

GrResult GrGraphics::get_FontAscent(int* pdy)
{
	TEXTMETRIC tm;
	if (!GetTextMetrics(m_hdc, &tm))
		return kresUnexpected;
	*pdy = tm.tmAscent;
	return kresOk;
}

GrResult GrGraphics::get_FontDescent(int* pdy)
{
	TEXTMETRIC tm;
	if (!GetTextMetrics(m_hdc, &tm))
		return kresUnexpected;
	*pdy = tm.tmDescent;
	return kresOk;

}

GrResult GrGraphics::get_YUnitsPerInch(int * pyInch)
{
	if (!m_yInch)
	{
		m_yInch = GetDeviceCaps(m_hdc, LOGPIXELSY);
	}
	*pyInch = m_yInch;
	return kresOk;
}


HDC GrGraphics::get_m_hdc(void)
{
	return m_hdc;
}

HFONT GrGraphics::get_m_hfont(void)
{
	return m_hfont;
}

LgCharRenderProps GrGraphics::get_m_chrp(void)
{
	return m_chrp;
}

int GrGraphics::get_m_yInch(void)
{
	return m_yInch;
}

wchar_t* GrGraphics::get_m_szFaceName(void)
{
	return m_szFaceName;
}

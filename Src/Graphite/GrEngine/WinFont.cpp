/*----------------------------------------------------------------------------------------------
Copyright (C) 1999, 2001, 2005 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WinFont.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	A font is an object that knows how to read tables from a (platform-specific) font resource.
----------------------------------------------------------------------------------------------*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

//:End Ignore

//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************


namespace gr
{

//:>********************************************************************************************
//:>	   WinFont methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructors.
	TODO: what happens if the font has no Graphite tables?
----------------------------------------------------------------------------------------------*/
WinFont::WinFont(HDC hdc)
{
	m_hdc  = hdc;

	// Use the name of the font and the bold and italic flags to read the cache.
	m_hfontClient = (HFONT)GetCurrentObject(hdc, OBJ_FONT);
	LOGFONT lf;
	::GetObject(m_hfontClient, sizeof(LOGFONT), &lf);

	Assert(m_hdc);
	Assert(m_hfontClient);

	std::copy(lf.lfFaceName, lf.lfFaceName +
			  sizeof(m_fpropDef.szFaceName) / sizeof(m_fpropDef.szFaceName[0]),
			  m_fpropDef.szFaceName);
	m_fpropDef.pixHeight = (float)lf.lfHeight; // in pixels, possibly negative
	m_fpropDef.fBold = (lf.lfWeight > 550);
	m_fpropDef.fItalic = lf.lfItalic;
	// Since we always set the colors to what we want, don't bother looking up those values,
	// just set these to something reasonable:
	m_fpropDef.clrBack = (unsigned long)kclrTransparent;
	m_fpropDef.clrFore = (unsigned long)kclrBlack;

	// But don't store m_hfont, because we don't really "own" it; the client is
	// responsible for releasing it.
	m_hfont = 0;
	memset(&m_fpropSet, 0, sizeof(m_fpropSet));

	m_pbCmapTbl = NULL;
	m_pbHeadTbl = NULL;
	m_pbHmtxTbl = NULL;
	m_pbLocaTbl = NULL;
	m_pbGlyfTbl = NULL;
	m_pbNameTbl = NULL;
	m_pbSileTbl = NULL;
	m_pbSilfTbl = NULL;
	m_pbFeatTbl = NULL;
	m_pbGlatTbl = NULL;
	m_pbGlocTbl = NULL;
	m_pbSillTbl = NULL;
	m_pbOs2Tbl  = NULL;
}

void WinFont::UniqueCacheInfo(std::wstring & stuFace, bool & fBold, bool & fItalic)
{
	stuFace = m_fpropDef.szFaceName;
	fBold = m_fpropDef.fBold;
	fItalic = m_fpropDef.fItalic;
}

/***
WinFont::WinFont(HDC hdc, std::wstring stuFaceName, int pointSize, bool fBold, bool fItalic)
{
	m_hdc = hdc;

	// Convert points to pixels.

	LOGFONT lf;
	lf.lfItalic = fItalic;
	lf.lfWeight = fBold ? 700 : 400;
	// The minus causes this to be the font height (roughly, from top of ascenders
	// to bottom of descenders). A positive number indicates we want a font with
	// this distance as the total line spacing, which makes them too small.
	// Note that we are also scaling the font size based on the resolution.
	lf.lfHeight = -GrMulDiv(pointSize, getDPIy(), 72); 72 = points per inch
	lf.lfUnderline = false;
	lf.lfWidth = 0;			// default width, based on height
	lf.lfEscapement = 0;	// no rotation of text (is this how to do slanted?)
	lf.lfOrientation = 0;	// no rotation of character baselines
	lf.lfStrikeOut = 0;		// no strike-out
	lf.lfCharSet = DEFAULT_CHARSET;			// let name determine it; WS should specify valid
	lf.lfOutPrecision = OUT_TT_ONLY_PRECIS;	// only work with TrueType fonts
	lf.lfClipPrecision = CLIP_DEFAULT_PRECIS; // ??
	lf.lfQuality = DRAFT_QUALITY;	// JohnT doesn't think this matters for TrueType fonts
	lf.lfPitchAndFamily = 0;		// must be zero for EnumFontFamiliesEx
	#ifdef UNICODE
			// ENHANCE: test this path if ever needed.
			wcscpy(lf.lfFaceName, stuFaceName.data());
	#else // not unicode, LOGFONT has 8-bit chars
			WideCharToMultiByte(
				CP_ACP,	0, // dumb; we don't expect non-ascii chars
				pchrp->szFaceName, // string to convert
				-1,		// null-terminated
				lf.lfFaceName, 32,
				NULL, NULL);  // default handling of unconvertibles
	#endif // not unicode
	HFONT hfont;
	hfont = ::CreateFontIndirect(&lf);
	if (!hfont)
		ThrowHr(WarnHr(kresFail));

	SetFont(hfont); // TODO: fix this
}
***/


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
WinFont::~WinFont()
{
	delete[] m_pbCmapTbl;
	delete[] m_pbHeadTbl;
	delete[] m_pbHmtxTbl;
	delete[] m_pbLocaTbl;
	delete[] m_pbGlyfTbl;
	delete[] m_pbNameTbl;
	delete[] m_pbSileTbl;
	delete[] m_pbSilfTbl;
	delete[] m_pbFeatTbl;
	delete[] m_pbGlatTbl;
	delete[] m_pbGlocTbl;
	if (m_pbSillTbl)
		delete[] m_pbSillTbl;
	delete[] m_pbOs2Tbl;

	static int cDeleteObjectCalls = 0;
	static int cDeleteObjectZero = 0;
	g_fhc.DeleteFont(m_hfont);
	//char ch2[200];
	//if (m_hfont == 0)
	//{
	//	cDeleteObjectZero++;
	//	//sprintf(ch2, "Delete: %d -  %d", cDeleteObjectZero, m_hfont);
	//}
	//else
	//{
	//	cDeleteObjectCalls++;
	//	sprintf(ch2, "Delete: %d -  %d", cDeleteObjectCalls, m_hfont);
	//}
	//Warn(ch2);
}

/*----------------------------------------------------------------------------------------------
	Copy constructor.
----------------------------------------------------------------------------------------------*/
Font * WinFont::copyThis()
{
	WinFont * pfont = new WinFont(*this);
	return pfont;
}

WinFont::WinFont(WinFont & font)
: Font(font)
{
	m_fpropDef = font.m_fpropDef;

	// Since the DC is rather ephemeral, don't bother saving it, so we don't think we can
	// count on using it.
	m_hdc = 0;
	m_hfont = 0;
	m_hfontClient = 0;
	memset(&m_fpropSet, 0, sizeof(FontProps));

	m_pbCmapTbl = NULL;
	m_pbHeadTbl = NULL;
	m_pbHmtxTbl = NULL;
	m_pbLocaTbl = NULL;
	m_pbGlyfTbl = NULL;
	m_pbNameTbl = NULL;
	m_pbSileTbl = NULL;
	m_pbSilfTbl = NULL;
	m_pbFeatTbl = NULL;
	m_pbGlatTbl = NULL;
	m_pbGlocTbl = NULL;
	m_pbSillTbl = NULL;
	m_pbOs2Tbl  = NULL;
}

/*----------------------------------------------------------------------------------------------
	Return true if the font indicated by the argument has an Silf table, indicating it should be
	Graphite-enabled.
----------------------------------------------------------------------------------------------*/
bool WinFont::FontHasGraphiteTables(HDC hdc)
{
	DWORD cbTableSz = ::GetFontData(hdc, kttiSilf, 0, NULL, 0);
	if (cbTableSz == GDI_ERROR)
		return false;
	else if (cbTableSz == 0)
		return false;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Read a table from the font.
----------------------------------------------------------------------------------------------*/
const void * WinFont::getTable(fontTableId32 tableID, size_t * pcbSize)
{
	*pcbSize = 0;

	switch (tableID)
	{
	case kttiCmap:	if (m_pbCmapTbl) return (void *)m_pbCmapTbl; break;
	case kttiHead:	if (m_pbHeadTbl) return (void *)m_pbHeadTbl; break;
	case kttiHmtx:	if (m_pbHmtxTbl) return (void *)m_pbHmtxTbl; break;
	case kttiLoca:	if (m_pbLocaTbl) return (void *)m_pbLocaTbl; break;
	case kttiGlyf:	if (m_pbGlyfTbl) return (void *)m_pbGlyfTbl; break;
	case kttiName:	if (m_pbNameTbl) return (void *)m_pbNameTbl; break;
	case kttiSile:	if (m_pbSileTbl) return (void *)m_pbSileTbl; break;
	case kttiSilf:	if (m_pbSilfTbl) return (void *)m_pbSilfTbl; break;
	case kttiFeat:	if (m_pbFeatTbl) return (void *)m_pbFeatTbl; break;
	case kttiGloc:	if (m_pbGlocTbl) return (void *)m_pbGlocTbl; break;
	case kttiGlat:	if (m_pbGlatTbl) return (void *)m_pbGlatTbl; break;
	case kttiSill:	if (m_pbSillTbl) return (void *)m_pbSillTbl; break;
	case kttiOs2:	if (m_pbOs2Tbl)  return (void *)m_pbOs2Tbl;  break;
	default:
		break; // fall through
	}

	byte * prgbBuffer;
	// The tableID is in native (little-endian) byte order, but the GDI methods expects big-endian.
	fontTableId32 tableIdBIG  = (tableID & 0x000000FF) << 24;
				  tableIdBIG += (tableID & 0x0000FF00) << 8;
				  tableIdBIG += (tableID & 0x00FF0000) >> 8;
				  tableIdBIG += (tableID & 0xFF000000) >> 24;
	DWORD cbTableSz = ::GetFontData(m_hdc, tableIdBIG, 0, NULL, 0);
	if (cbTableSz == GDI_ERROR)
	{
#if 1 // for testing, to avoid popping up exceptions with old fonts that don't have Sill table
		if (tableID == kttiSill)
		{
			m_pbSillTbl = NULL;
			*pcbSize = 0;
			return NULL;
		}
#endif
		THROW(kresUnexpected);
	}
	else if (cbTableSz == 0)
	{
		prgbBuffer = NULL;
	}
	else
	{
		prgbBuffer = new byte[cbTableSz];
		if (::GetFontData(m_hdc, tableIdBIG, 0, (void *)prgbBuffer, cbTableSz) == GDI_ERROR)
		{
			THROW(kresUnexpected);
		}
	}
	*pcbSize = cbTableSz; // allows error checking

	switch (tableID)
	{
	case kttiCmap:	m_pbCmapTbl = prgbBuffer; break;
	case kttiHead:	m_pbHeadTbl = prgbBuffer; break;
	case kttiHmtx:	m_pbHmtxTbl = prgbBuffer; break;
	case kttiLoca:	m_pbLocaTbl = prgbBuffer; break;
	case kttiGlyf:	m_pbGlyfTbl = prgbBuffer; break;
	case kttiName:	m_pbNameTbl = prgbBuffer; break;
	case kttiSile:	m_pbSileTbl = prgbBuffer; break;
	case kttiSilf:	m_pbSilfTbl = prgbBuffer; break;
	case kttiFeat:	m_pbFeatTbl = prgbBuffer; break;
	case kttiGloc:	m_pbGlocTbl = prgbBuffer; break;
	case kttiGlat:	m_pbGlatTbl = prgbBuffer; break;
	case kttiSill:	m_pbSillTbl = prgbBuffer; break;
	case kttiOs2:	m_pbOs2Tbl  = prgbBuffer; break;
	default:
		delete[] prgbBuffer;
		THROW(kresUnexpected);
	}
	return prgbBuffer;
}

/*----------------------------------------------------------------------------------------------
	Return the device resolution.
	TODO: is there any reason to cache these values?
----------------------------------------------------------------------------------------------*/
unsigned int WinFont::getDPIx()
{
	return ::GetDeviceCaps(m_hdc, LOGPIXELSX);
}

unsigned int WinFont::getDPIy()
{
	return ::GetDeviceCaps(m_hdc, LOGPIXELSY);
}

/*----------------------------------------------------------------------------------------------
	Get the metrics of the font in the logical device units.
	Any of the arguments can be NULL.
----------------------------------------------------------------------------------------------*/
void WinFont::getFontMetrics(float * pAscent, float * pDescent, float * pEmSquare)
{
	TEXTMETRIC tm;
	if (!::GetTextMetrics(m_hdc, &tm))
		THROW(kresUnexpected);  // GetTextMetrics failed
	if (pAscent)
		*pAscent = (float)tm.tmAscent;
	if (pDescent)
		*pDescent = (float)tm.tmDescent;
	if (pEmSquare)
		*pEmSquare = (float)(tm.tmHeight - tm.tmInternalLeading);
}

/*----------------------------------------------------------------------------------------------
	Get coordinates for a specific point on a glyph's curve.
----------------------------------------------------------------------------------------------*/
void WinFont::getGlyphPoint(gid16 gid, unsigned int nPoint, gr::Point & xyReturn)
{
	GLYPHMETRICS gm;
	const MAT2 mat2 = {{0,1}, {0,0}, {0,0}, {0,1}};
	WORD flag = GGO_GLYPH_INDEX | GGO_NATIVE;

	UINT cbBuf = ::GetGlyphOutline(m_hdc, gid, flag, &gm, 0, NULL, &mat2);
	if (cbBuf <= 0)
		THROW(kresUnexpected); // first GetGlyphOutline in XYFromGlyphPoint failed
	byte * pGlyphData = new byte[cbBuf];
	if (!pGlyphData)
		THROW(kresOutOfMemory);
	if (GDI_ERROR == ::GetGlyphOutline(m_hdc, gid, flag, &gm, cbBuf, pGlyphData,&mat2))
		THROW(kresUnexpected); // second GetGlyphOutline in XYFromGlyphPoint failed

	// Note: conversion from device to logical units is not necessary, because
	// the font size in the LOGFONT has already been scaled based on the
	// resolution of the device context.

	TTPOLYGONHEADER * pPolyHdr;
	TTPOLYCURVE * pPolyCurve;
	pPolyHdr = (TTPOLYGONHEADER *)pGlyphData;
	int nCurPoint;
	nCurPoint = 0;

	// The while loop below cannot handle getting the first point from the polygon header.
	if (nPoint == 0)
	{
		xyReturn.x = FloatFromFixed(pPolyHdr->pfxStart.x);
		xyReturn.y = FloatFromFixed(pPolyHdr->pfxStart.y);
		delete [] pGlyphData;
		return;
	}

	// If the last curve in the polygon is a spline, the last point in this curve will
	// repeat the point from the the polygon header. This adds an extra point to the data
	// returned by Windows that is not in the TTF font file. Test to set nExtraPt is below.
	int nExtraPt = 0;

	while ((byte *)pPolyHdr < (pGlyphData + cbBuf))
	{
		if (signed(nPoint) == nCurPoint)
		{
			xyReturn.x = FloatFromFixed(pPolyCurve->apfx[nPoint - nCurPoint].x);
			xyReturn.y = FloatFromFixed(pPolyCurve->apfx[nPoint - nCurPoint].y);
			delete [] pGlyphData;
			return;
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

					if (signed(nPoint) < nCurPoint + pPolyCurve->cpfx - nExtraPt)
					{
						xyReturn.x = FloatFromFixed(pPolyCurve->apfx[nPoint - nCurPoint].x);
						xyReturn.y = FloatFromFixed(pPolyCurve->apfx[nPoint - nCurPoint].y);
						delete [] pGlyphData;
						return;
					}
				}
				else
				{
					delete [] pGlyphData;
					THROW(kresUnexpected);
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
			THROW(kresUnexpected);
		}
		pPolyHdr = (TTPOLYGONHEADER *)((byte *)pPolyHdr + pPolyHdr->cb);
	}

	delete [] pGlyphData;
	THROW(kresUnexpected);
}

/*----------------------------------------------------------------------------------------------
	Get glyph metrics for a glyph in the currently selected font. chw must be a glyph id.
----------------------------------------------------------------------------------------------*/
void WinFont::getGlyphMetrics(gid16 chw, gr::Rect & boundingBox, gr::Point & advances)
{
	GLYPHMETRICS gm;
	const MAT2 mat2 = {{0,1}, {0,0}, {0,0}, {0,1}};
	if (GDI_ERROR == ::GetGlyphOutline(m_hdc, chw, GGO_GLYPH_INDEX | GGO_METRICS,
		&gm, 0, NULL, &mat2))
	{
		THROW(kresUnexpected); // GetGlyphOutline failed
	}

	// Note: the results are already in logical units because the LOGFONT has been
	// scaled based on the resolution (DPI).

	boundingBox.left = (float)gm.gmptGlyphOrigin.x;
	boundingBox.top = (float)gm.gmptGlyphOrigin.y;
	boundingBox.right = (float)gm.gmptGlyphOrigin.x + gm.gmBlackBoxX;
	boundingBox.bottom = (float)gm.gmptGlyphOrigin.y - gm.gmBlackBoxY;
	advances.x = gm.gmCellIncX;
	advances.y = gm.gmCellIncY;
}

/*----------------------------------------------------------------------------------------------
	Replace the device context with one that is known to be valid.
	Calls to this method must be balanced by a call to restoreDC.
----------------------------------------------------------------------------------------------*/
void WinFont::replaceDC(HDC hdc)
{
	// Debugging: remember the font that is in hdc.
	//HFONT hfontTemp = (HFONT)GetCurrentObject(hdc, OBJ_FONT);
	//LOGFONT lf;
	//::GetObject(hfontTemp, sizeof(LOGFONT), &lf);
	//if (hfontTemp)
	//	memcpy(m_rgchTemp, lf.lfFaceName, 32 * sizeof(OLECHAR));
	//else
	//	memset(m_rgchTemp, 0, 32 * sizeof(OLECHAR));
	///////////////////////

	if (m_hdc == hdc)
		return;

	m_hdc = hdc;
	SetInternalFont();
}

/*----------------------------------------------------------------------------------------------
	We are finished working with the device context, so set it back to its original state.
	In particular, set the HFONT object to what was there originally (so that when the
	DC is deleted, the HFONT will be destroyed as well).
----------------------------------------------------------------------------------------------*/
void WinFont::restoreDC()
{
	restorePreviousFont();

	// We are finished with this device context.
	m_hdc = 0;
}

/*----------------------------------------------------------------------------------------------
	We are finished working with the font, so set it back to the previous value.
----------------------------------------------------------------------------------------------*/
void WinFont::restorePreviousFont()
{
	if (m_hfontClient)
		// Restore the client's HFONT into the device context.
		::SelectObject(m_hdc, m_hfontClient);
	m_hfontClient = 0;

	// Debugging.
	//HFONT hfontTemp = (HFONT)GetCurrentObject(m_hdc, OBJ_FONT);
	//LOGFONT lf;
	//::GetObject(hfontTemp, sizeof(LOGFONT), &lf);
	//OLECHAR rgchTemp[32];
	//if (hfontTemp)
	//	memcpy(rgchTemp, lf.lfFaceName, 32 * sizeof(OLECHAR));
	//else
	//	memset(rgchTemp, 0, 32 * sizeof(OLECHAR));
	//Assert(wcscmp(rgchTemp, m_rgchTemp) == 0);
	////////////////////////

	// Leave Graphite's HFONT around in case we can use it again later. The destructor is
	// responsible for deleting it.
}

/*----------------------------------------------------------------------------------------------
	Set the low-level internal stuff to match what we want.

	This is used in the following situations:
	- The DC is being replaced in the WinFont, and we need to make its internals match the
		WinFont.
	- The DC is being used for drawing, and we need to make its internals match the Segment.
	- Possibly (eventually): the WinFont is being created, and we need to
		set up the DC to match it
----------------------------------------------------------------------------------------------*/
GrResult WinFont::SetInternalFont()
{
	return SetInternalFont((unsigned long)kclrBlack, (unsigned long)kclrTransparent);
}

GrResult WinFont::SetInternalFont(unsigned long clrFore, unsigned long clrBack)
{
#ifdef BASELINE
//	if (m_fFile)
//		m_psts->OutputFormat("SetInternalFont(ptr) fItalic: %d, fBold: %d, dympHeight"
//			": %d, szFaceName: \"%s\", clrFore: %d, clrBack: %d\n", pchrp->fItalic,
//			pchrp->fBold, pchrp->dympHeight, pchrp->szFaceName, pchrp->clrFore, pchrp->clrBack);
#endif

	// TODO: get the LOGFONT info out of the device context and compare.

	const int cbFontOffset = offsetof(FontProps, fBold);

	if (!m_hfont // no font set up yet or
		// the font we want is different (except we don't care about color)
		|| memcmp(((byte *)&m_fpropDef) + cbFontOffset,
					((byte *)&m_fpropSet) + cbFontOffset,
					isizeof(FontProps) - cbFontOffset))
	{
		// Change the font.
		Assert(!m_hfont || m_hfont == m_hfontClient);

		//wchar rgch[20];
		//_itow((int)pschp->pixHeight, rgch, 10);
		//OutputDebugString(L"font size = ");
		//OutputDebugString(rgch);
		//OutputDebugString(L"\n");

		// Figure the actual font we need.
		LOGFONT lf;
		lf.lfItalic = m_fpropDef.fItalic ;
		lf.lfWeight = m_fpropDef.fBold ? 700 : 400;
		// The -1 causes this to be the font height (roughly, from top of ascenders
		// to bottom of descenders). A positive number indicates we want a font with
		// this distance as the total line spacing, which makes them too small.
		lf.lfHeight = abs(int(m_fpropDef.pixHeight)) * -1; // SCALE this
		lf.lfUnderline = false;
		lf.lfWidth = 0;			// default width, based on height
		lf.lfEscapement = 0;	// no rotation of text (is this how to do slanted?)
		lf.lfOrientation = 0;	// no rotation of character baselines

		lf.lfStrikeOut = 0;						// no strike-out
		lf.lfCharSet = DEFAULT_CHARSET;			// let name determine it; WS should specify valid
		lf.lfOutPrecision = OUT_TT_ONLY_PRECIS;	// only work with TrueType fonts
		lf.lfClipPrecision = CLIP_DEFAULT_PRECIS; // ??
		lf.lfQuality = DRAFT_QUALITY;			// JohnT thinks this doesn't matter for TrueType fonts
		lf.lfPitchAndFamily = 0;				// must be zero for EnumFontFamiliesEx
		#ifdef UNICODE
				// Need to null out the face name so that the hash table will work correctly.
				// Otherwise the extra bytes at the end on the name get treated as a unique name.
				wmemset(lf.lfFaceName, 0, LF_FACESIZE);
				wcscpy(lf.lfFaceName, m_fpropDef.szFaceName);
		#else // not unicode, LOGFONT has 8-bit chars
				WideCharToMultiByte(
					CP_ACP,	0, // dumb; we don't expect non-ascii chars
					m_fpropDef.szFaceName, // string to convert
					-1,		// null-terminated
					lf.lfFaceName, 32,
					NULL, NULL);  // default handling of unconvertibles
		#endif // not unicode

		// Replace it in the device context.

		static int cCreateFontCalls = 0;
		static int cCreateFontZero = 0;
		HFONT hfont = g_fhc.GetFont(lf);
		//char ch1[200];
		//if (hfont == 0)
		//{
		//	cCreateFontZero++;
		//	sprintf(ch1, "Creation: %d -  %d", cCreateFontZero, hfont);
		//}
		//else
		//{
		//	cCreateFontCalls++;
		//	sprintf(ch1, "Create: %d -  %d", cCreateFontCalls, hfont);
		//}
		//Warn(ch1);
		if (!hfont)
			THROW(kresFail);

		HGDIOBJ hfontPrev = ::SelectObject(m_hdc, hfont);
		if (m_hfont && m_hfont != m_hfontClient)
		{
			// Throw away the previous temporary one.
			static int cDeleteObjectCalls = 0;
			static int cDeleteObjectZero = 0;
			g_fhc.DeleteFont(m_hfont);
			//char ch2[200];
			//if (m_hfont == 0)
			//{
			//	cDeleteObjectZero++;
			//	//sprintf(ch2, "Temp del: %d -  %d", cDeleteObjectZero, m_hfont);
			//}
			//else
			//{
			//	cDeleteObjectCalls++;
			//	sprintf(ch2, "Temp del: %d -  %d", cDeleteObjectCalls, m_hfont);
			//}
			//Warn(ch2);
			Assert(m_hfontClient);
		}
		else
		{
			// Save the client's HFONT to restore--this way the client will release that
			// HFONT along with the DC.
			m_hfontClient = (HFONT)hfontPrev;
		}
		m_hfont = hfont;
	}
	else
	{
		// Use the HFONT that we have saved. Save the client's HFONT to restore.
		m_hfontClient = (HFONT)(::SelectObject(m_hdc, m_hfont));
	}

	// Always set the colors.
	// OPTIMIZE JohnT: would it be useful to remember what the hdc is set to? ASK John
	{
		//SmartPalette spal(hdc);

		bool fOK = (SetTextColor(m_hdc, clrFore) != CLR_INVALID);
		if (clrBack == kclrTransparent)
		{
			// I can't find it documented anywhere, but it seems to be necessary to set
			// the background color to black to make TRANSPARENT mode work--at least on my
			// computer.
			fOK = fOK && (::SetBkColor(m_hdc, RGB(0,0,0)) != CLR_INVALID);
			fOK = fOK && ::SetBkMode(m_hdc, TRANSPARENT);
		}
		else
		{
			fOK = fOK && (SetBkColor(m_hdc, clrBack) != CLR_INVALID);
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

	// Remember the font we switch to.

	m_fpropSet = m_fpropDef;
	m_fpropSet.clrFore = clrFore;
	m_fpropSet.clrBack = clrBack;

	return kresOk;
}


//:>********************************************************************************************
//:>	FontHandleCache methods
//:>********************************************************************************************

// cache of font handles accessed by LOGFONTS.
WinFont::FontHandleCache WinFont::g_fhc;

/*----------------------------------------------------------------------------------------------
	Destructor: free all the stored font handles before the internal HashMap itself goes
	away.
----------------------------------------------------------------------------------------------*/
WinFont::FontHandleCache::~FontHandleCache()
{
	ResetFontCache();
}

/*----------------------------------------------------------------------------------------------
	Gets a font from the font cache. If the font doesn't exist in the font cache, it is created,
	otherwise we increment the reference count for the font.

	@param lf LOGFONT value that describes the desired font
	@return Font handle
----------------------------------------------------------------------------------------------*/
HFONT WinFont::FontHandleCache::GetFont(LOGFONT & lf)
{
	FontCacheValue fcv;
	FontHandleHashMap::iterator itFound = m_hmlffcv.find(lf);
	if (itFound !=  m_hmlffcv.end())
	{
		// Update the reference count
		fcv = itFound->second; // value
		fcv.nRefs++;

		m_hmlffcv.erase(itFound);
		m_hmlffcv.insert(std::pair<LogFontWrapper, FontCacheValue>(LogFontWrapper(lf), fcv)); //, true);
	}
	else
	{
		// create font
		fcv.hfont = ::CreateFontIndirect(&lf);
		if (!fcv.hfont)
			THROW(kresFail);

		fcv.nRefs = 1;

		m_hmlffcv.insert(std::pair<LOGFONT, FontCacheValue>(lf, fcv));
	}

	return fcv.hfont;
}

/*----------------------------------------------------------------------------------------------
	Decrements the reference count for the font. If reference count is zero we delete the font
	and remove it from the hash map.

	@param lf Font handle
----------------------------------------------------------------------------------------------*/
void WinFont::FontHandleCache::DeleteFont(HFONT hfont)
{
	if (!hfont)
		return;

	// find the font in the hash map
	FontHandleHashMap::iterator it;
	for (it = m_hmlffcv.begin(); it != m_hmlffcv.end(); ++it)
	{
		if (it->second.hfont == hfont) // second gives the value from the key-value pair
		{
			FontCacheValue fcv = it->second; // value
			fcv.nRefs--;
			if (fcv.nRefs <= 0)
			{
				// delete font
				::DeleteObject(hfont);
				m_hmlffcv.erase(it);
			}
			else
			{
				// Update hash map entry
				LogFontWrapper lfw;
				lfw = it->first;
				m_hmlffcv.erase(it);
				m_hmlffcv.insert(std::pair<LogFontWrapper, FontCacheValue>(lfw, fcv)); // , true);
			}
			return;
		}
	}

	Assert(it != m_hmlffcv.end());
}

/*--------------------------------------------------------------------------------------
	Delete all the stored fonts in the hash map.
--------------------------------------------------------------------------------------*/
void WinFont::FontHandleCache::ResetFontCache()
{
	FontHandleHashMap::iterator it;
	for (it = m_hmlffcv.begin(); it != m_hmlffcv.end(); ++it)
	{
		FontCacheValue fcv = it->second;
		::DeleteObject(fcv.hfont);
	}
	m_hmlffcv.clear();
}


/*--------------------------------------------------------------------------------------
	Hashing function for the LOGFONT which is the key of the hash-map.
--------------------------------------------------------------------------------------*/
size_t WinFont::LogFontHashFuncs::operator() (const WinFont::LogFontWrapper & key) const
{
	size_t nHash = 0;
	byte * pb = (byte *)(&key.m_lf);
	for (int i = 0; i < sizeof(LOGFONT); ++i)
		nHash += (nHash << 4) + *pb++;
	return nHash;
}

/*--------------------------------------------------------------------------------------
	Comparison function for the LOGFONT which is the key of the hash-map.
--------------------------------------------------------------------------------------*/
bool WinFont::LogFontHashFuncs::operator() (const WinFont::LogFontWrapper & key1,
	const WinFont::LogFontWrapper & key2) const
{
	return (key1 == key2);
}

/*--------------------------------------------------------------------------------------
	LogFontHashFuncs calls this method to compare LOGFONT objects.
--------------------------------------------------------------------------------------*/
bool WinFont::LogFontWrapper::operator==(const WinFont::LogFontWrapper & lfw) const
{
	return memcmp(&m_lf, &(lfw.m_lf), sizeof(LOGFONT));
}


} // namespace gr

template stdext::hash_map<
	gr::WinFont::LogFontWrapper,
	gr::WinFont::FontHandleCache::FontCacheValue,
	gr::WinFont::LogFontHashFuncs>;

/*--------------------------------------------------------------------------------------
	Check to see what the state of the DC is.
--------------------------------------------------------------------------------------*/
/**
void WinFont::DebugDC()
{
	HFONT hfontClient = (HFONT)GetCurrentObject(m_hdc, OBJ_FONT);
	LOGFONT lf;
	::GetObject(m_hfontClient, sizeof(LOGFONT), &lf);

	Assert(m_hdc);
	Assert(m_hfontClient);

	FontProps fpropTemp;

	std::copy(lf.lfFaceName, lf.lfFaceName +
			  sizeof(fpropTemp.szFaceName) / sizeof(m_fpropDef.szFaceName[0]),
			  fpropTemp.szFaceName);
	fpropTemp.pixHeight = (float)lf.lfHeight; // in pixels, possibly negative
	fpropTemp.fBold = (lf.lfWeight > 550);
	fpropTemp.fItalic = lf.lfItalic;
}
**/

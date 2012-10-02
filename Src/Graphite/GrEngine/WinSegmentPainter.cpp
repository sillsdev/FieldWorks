/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WinSegmentPainter.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implements the object that handles drawing, mouse clicks, and other UI-related behavior
	for a Graphite segment.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)
#ifndef _WIN32
#include <stdlib.h>
#endif

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************
namespace
{

static bool g_fDrawing;

} // namespace


namespace gr
{

//:>********************************************************************************************
//:>	Methods
//:>********************************************************************************************

//:>********************************************************************************************
//:>	WinSegmentPainter overrides
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
WinSegmentPainter::WinSegmentPainter(Segment * pseg, HDC hdc, float xsOrigin, float ysOrigin)
	: SegmentPainter(pseg, xsOrigin, ysOrigin)
{
	m_hdc = hdc;
	m_prggstrm = NULL;
	m_cgstrm = -1;
	m_prggbb = NULL;
	m_cgbb = -1;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
WinSegmentPainter::~WinSegmentPainter()
{
	delete[] m_prggstrm;
	delete[] m_prggbb;
}

/*----------------------------------------------------------------------------------------------
	Draw the text of the segment at the specified point in the graphics context.
----------------------------------------------------------------------------------------------*/
void WinSegmentPainter::paint()
{
	Font & font = m_pseg->getFont();

	//GrResult res = kresFail;
	try
	{
		WinFont tmpFont(m_hdc);
		m_pseg->SwitchFont(&tmpFont);
		paintAux();
		// switch back to old font. Do this here while tmpFont is still in scope, so that we
		// can delete on it
		m_pseg->SwitchFont(&font);
	}
	catch (...)
	{
		m_pseg->SwitchFont(&font);
		THROW(kresUnexpected);
	}
}

/*----------------------------------------------------------------------------------------------
	Draw the text of the segment at the specified point in the graphics context.
	This is the stuff that we're overriding from SegmentPainter.
----------------------------------------------------------------------------------------------*/
void WinSegmentPainter::paintAux()
{
	SetUpFromSegment();

	if (g_fDrawing)
		return;

	g_fDrawing = true;

	try
	{
		////Assert(m_pseg->m_dxsWidth >= 0);

		int xs = 0;
		//	Top of common-baseline text relative to top of segment:
		int ys = GrEngine::RoundFloat(m_pseg->AscentOffset());

		//	First we draw the background (because if the background color is
		//	anything other than transparent it will clobber nearby glyphs), and then we
		//	draw the glyphs.
		paintBackground(xs, ys);

		//	Now draw the glyphs in the foreground.
		paintForeground(xs, ys);
	}
	catch (...)
	{
		g_fDrawing = false;
		throw;
	}

	g_fDrawing = false;
	return;
}

void WinSegmentPainter::paintForeground(int xs, int ys)
{
	for (int igstrm = 0; igstrm < this->m_cgstrm; igstrm++)
	{
		GlyphStrm & gstrm = this->m_prggstrm[igstrm];
		int xsLeft = xs + gstrm.xsStart;
		int ysTop = ys - gstrm.gsk.ys; // subtract amount above base line, in order to move up

		setForegroundPaintColor(gstrm.gsk);

		int xdLeft = GrEngine::RoundFloat(ScaleXToDest((float)xsLeft));
		int ydTop = GrEngine::RoundFloat(ScaleYToDest((float)ysTop));

		DrawTextExt(xdLeft, ydTop, gstrm.vdxd.size(), &(gstrm.vchwGlyphId[0]),
			ETO_GLYPH_INDEX, NULL, &(gstrm.vdxd[0]));

		RestorePreviousFont();
	}
}

// Set font color for paintForeground...isolated for override in special cases.
void WinSegmentPainter::setForegroundPaintColor(GlyphStrmKey & gsk)
{
	SetFontProps(gsk.clrFore, (unsigned long)kclrTransparent);
}

void WinSegmentPainter::paintBackground(int xs, int ys)
{
	for (int igstrm = 0; igstrm < this->m_cgstrm; igstrm++)
	{
		GlyphStrm & gstrm = this->m_prggstrm[igstrm];
		if (unsigned(gstrm.gsk.clrBack) == kclrTransparent)
			continue;

		int xsLeft = xs + gstrm.xsStart;
		int ysTop = ys - gstrm.gsk.ys; // subtract amount above base line, in order to move up

		SetFontProps(gstrm.gsk.clrFore, gstrm.gsk.clrBack);

		int xdLeft = GrEngine::RoundFloat(ScaleXToDest((float)xsLeft));
		int ydTop = GrEngine::RoundFloat(ScaleYToDest((float)ysTop));

		//	Draw the backgrounds one glyph at a time; otherwise we get background color
		//	filled in in places we don't want it.
		int xd = xdLeft;
		DrawTextExt(xd, ydTop, 1, &gstrm.vchwGlyphId[0],
			ETO_GLYPH_INDEX, NULL, NULL);
		for (size_t i = 1; i < gstrm.vdxd.size(); i++)
		{
			xd += gstrm.vdxd[i-1];
			DrawTextExt(xd, ydTop, 1, &gstrm.vchwGlyphId[i],
				ETO_GLYPH_INDEX, NULL, NULL);
		}
		RestorePreviousFont();
	}
}

/*----------------------------------------------------------------------------------------------
	Draw an insertion point at an appropriate position.

	@param ich			- character position; must be valid
	@param fAssocPrev	- associated with previous character?
	@param bOn			- turning on or off? Caller should alternate, first turning on
							(ignored in this implementation)
	@param dm			- draw mode:
							kdmNormal = draw complete insertion pt (I-beam or split cursor);
							kdmPrimary = only draw primary half of split cursor;
							kdmSecondary = only draw secondary half of split cursor
----------------------------------------------------------------------------------------------*/
void WinSegmentPainter::drawInsertionPoint(
	int ichwIP, bool fAssocPrev,
	bool bOn, bool fForceSplit)
{
	ReplaceDC(m_hdc);

	SegmentPainter::drawInsertionPoint(ichwIP, fAssocPrev, bOn, fForceSplit);

	RestoreDC();
}

/*----------------------------------------------------------------------------------------------
	Highlight a range of text.

	@param ichwAnchor/End	- selected range
	@param ydLineTop/Bottom	- top/bottom of area to highlight if whole line height;
								includes half of inter-line spacing.
	@param bOn				- true if we are turning on (ignored in this implementation)
----------------------------------------------------------------------------------------------*/
bool WinSegmentPainter::drawSelectionRange(int ichwAnchor, int ichwEnd,
	float ydLineTop, float ydLineBottom, bool bOn)
{
	ReplaceDC(m_hdc);

	bool f = SegmentPainter::drawSelectionRange(ichwAnchor, ichwEnd,
		ydLineTop, ydLineBottom, bOn);

	RestoreDC();

	return f;
}

/*----------------------------------------------------------------------------------------------
	When changing the transformation, clear the cache since it stores device units.
----------------------------------------------------------------------------------------------*/
void WinSegmentPainter::setOrigin(float xsOrigin, float ysOrigin)
{
	if (xsOrigin != m_xsOrigin || ysOrigin != m_ysOrigin)
		ClearSegmentCache();
	SegmentPainter::setOrigin(xsOrigin, ysOrigin);
}

void WinSegmentPainter::setPosition(float xdPosition, float ydPosition)
{
	if (xdPosition != m_xdPosition || ydPosition != m_ydPosition)
		ClearSegmentCache();
	SegmentPainter::setPosition(xdPosition, ydPosition);
}

void WinSegmentPainter::setScalingFactors(float xFactor, float yFactor)
{
	if (xFactor != m_xFactor || yFactor != m_yFactor)
		ClearSegmentCache();
	SegmentPainter::setScalingFactors(xFactor, yFactor);
}

//:>********************************************************************************************
//:>	Low-level platform-specific drawing methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Draw a list of glyphs that are all positioned vertically the same relative to the baseline,
	given their offsets from each other.
----------------------------------------------------------------------------------------------*/
void WinSegmentPainter::DrawTextExt(int x, int y,
	int cgid, const OLECHAR __RPC_FAR * prggid,
	UINT uOptions, const RECT __RPC_FAR * pRect, int __RPC_FAR * prgdx)
{
	ChkGrArrayArg(prggid, cgid);
	ChkGrArgPtrN(pRect);
	if (prgdx == NULL)
	{
		Assert(cgid <= 1);
		if (cgid > 1)
			THROW(kresUnexpected);
	}
	else
	{
		ChkGrArrayArg(prgdx, cgid);
	}

	// check whether the text is visible, at least vertically
	RECT rectClip;
	GetMyClipRect(&rectClip);
	if (y > rectClip.bottom)
		return;
	if (y < rectClip.top - 10000)	// ENHANCE: correct if we support 500+ pt fonts
		return;

	// must explicitly call W API so get wide version on Win 9x
	if (!::ExtTextOutW(m_hdc, x, y, uOptions, pRect, prggid, cgid, prgdx))
		THROW(kresUnexpected); //, "ExtTextOutW failed");
}

/*----------------------------------------------------------------------------------------------
	Invert the specified rectangle.
----------------------------------------------------------------------------------------------*/
void WinSegmentPainter::InvertRect(float xLeft, float yTop, float xRight, float yBottom)
{
	// NOTE: We don't want to round the float value here because this causes overlapping of
	// two segments' rectangles sometimes. (TE-4497)
	RECT rect;
	rect.left = GrEngine::RoundFloat(xLeft);
	rect.top = GrEngine::RoundFloat(yTop);
	rect.right = GrEngine::RoundFloat(xRight);
	rect.bottom = GrEngine::RoundFloat(yBottom);

	// This is not just for efficiency; we want to avoid actual drawing
	// involving very large coordinates, because Win-95 mangles them.
	RECT rectClip;
	GetMyClipRect(&rectClip);
	RECT rectIntersect;
	if (!::IntersectRect(&rectIntersect, &rectClip, &rect))
	{
		// no intersection, nothing to do--but we (trivially) succeeded.
		return;
	}

	if (!::InvertRect(m_hdc, &rect))
		WarnHr(kresFail);
}

/*----------------------------------------------------------------------------------------------
	Return the clipping rectangle.
	TODO: cache it.
----------------------------------------------------------------------------------------------*/
void WinSegmentPainter::GetMyClipRect(RECT * prect)
{
	if (::GetClipBox(m_hdc, prect) == ERROR)
		THROW(kresFail); //, L"Could not get clip rectangle");
}

/*----------------------------------------------------------------------------------------------
	Set the device context of the font to the given one, which we know is valid. Set the
	device to display with the character properties as defined by the font.
----------------------------------------------------------------------------------------------*/
void WinSegmentPainter::ReplaceDC(HDC hdc)
{
	Font & font = m_pseg->getFont();
	WinFont * pfontWin = dynamic_cast<WinFont *>(&font);
	pfontWin->replaceDC(hdc);
}

/*----------------------------------------------------------------------------------------------
	Return the device context in the font to its original state (ie, release the HFONT and
	set it back to the original).
----------------------------------------------------------------------------------------------*/
void WinSegmentPainter::RestoreDC()
{
	Font & font = m_pseg->getFont();
	WinFont * pfontWin = dynamic_cast<WinFont *>(&font);
	pfontWin->restoreDC();
}

/*----------------------------------------------------------------------------------------------
	Return the device context in the font to its original state (ie, release the HFONT and
	set it back to the original).
----------------------------------------------------------------------------------------------*/
void WinSegmentPainter::RestorePreviousFont()
{
	Font & font = m_pseg->getFont();
	WinFont * pfontWin = dynamic_cast<WinFont *>(&font);
	pfontWin->restorePreviousFont();
}

//:>********************************************************************************************
//:>	Methods handling the Windows-specific cache
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Set up the data structures that represent the actual rendered glyphs for the new segment.
----------------------------------------------------------------------------------------------*/
void WinSegmentPainter::SetUpFromSegment()
{
	if (m_prggstrm)
	{
		Assert(m_prggbb);
		Assert(m_cgbb >= 0);
		Assert(m_cgstrm >= 0);
		return; // already set up
	}

	//Font & font = m_pseg->getFont();

	//	Iterate over final pass, creating glyph streams corresponding to each distinct
	//	y-coordinate/color combination.
	//	Also count the number of slots needing output and find the lowest x coordinate.
	std::vector<GlyphStrmKey> vgsk;
	m_cgbb = 0;

	ITextSource & tsrc = m_pseg->getText();
	IColorTextSource * pctsrc = dynamic_cast<IColorTextSource *>(&tsrc);

	std::pair<GlyphIterator, GlyphIterator> pairGit = m_pseg->glyphs();
	GlyphIterator gitStart = pairGit.first;
	GlyphIterator gitEnd = pairGit.second;

	for (GlyphIterator git = gitStart ; git != gitEnd ; ++git)
	{
		GlyphInfo ginf = *git;
		int ys = GrEngine::RoundFloat(ginf.yOffset());
		int ich = ginf.firstChar();
		int clrFore;
		int clrBack;
		pctsrc->getColors(ich, &clrFore, &clrBack);

		m_cgbb++;

		size_t igsk;
		for (igsk = 0; igsk < vgsk.size(); igsk++)
		{
			if (ys == vgsk[igsk].ys && clrFore == vgsk[igsk].clrFore &&
				clrBack == vgsk[igsk].clrBack)
			{
				break;
			}
		}
		if (igsk >= vgsk.size())
		{
			//	Didn't find it a glyph stream that matched, so add one.
			GlyphStrmKey gskTmp;
			gskTmp.ys = ys;
			gskTmp.clrFore = clrFore;
			gskTmp.clrBack = clrBack;
			vgsk.push_back(gskTmp);
		}
	}

	//	Sort y offsets based on difference from zero; offset closest to baseline should be
	//	first. Hit testing depends on this sort order to find BB with smallest offset from
	//	baseline.
//	::qsort(vgsk.Begin(), vgsk.Size(), isizeof(int), GlyphKeySort); -- doesn't work with structs

	for (int igsk1 = 0; igsk1 < signed(vgsk.size()) - 1; igsk1++)
	{
		GlyphStrmKey * pgsk1 = &vgsk[igsk1];
		for (size_t igsk2 = igsk1 + 1; igsk2 < vgsk.size(); igsk2++)
		{
			GlyphStrmKey * pgsk2 = &vgsk[igsk2];
			if (pgsk2->LessThan(pgsk1))
			{
				int iTmp;
				int clrTmp;
				iTmp = pgsk1->ys;
				pgsk1->ys = pgsk2->ys;
				pgsk2->ys = iTmp;
				clrTmp = pgsk1->clrFore;
				pgsk1->clrFore = pgsk2->clrFore;
				pgsk2->clrFore = clrTmp;
				clrTmp = pgsk1->clrBack;
				pgsk1->clrBack = pgsk2->clrBack;
				pgsk2->clrBack = clrTmp;
			}
		}
	}

	//	Allocate arrays & set GlyphStrmKey fields.
	m_prggbb = new GlyphBb [m_cgbb];

	m_cgstrm = vgsk.size();
	m_prggstrm = new GlyphStrm [m_cgstrm];

	int igstrm;
	for (igstrm = 0; igstrm < m_cgstrm; igstrm++)
	{
		m_prggstrm[igstrm].gsk.ys = vgsk[igstrm].ys;
		m_prggstrm[igstrm].gsk.clrFore = vgsk[igstrm].clrFore;
		m_prggstrm[igstrm].gsk.clrBack = vgsk[igstrm].clrBack;
	}

	//	Iterate over output slots, filling in GlyphBb array (ig and bounding box fields).
	GlyphIterator git2 = gitStart;
	for (int ig = 0; git2 != gitEnd ; ++git2, ++ig)
	{
		GlyphInfo * pginf = &(*git2);
		GlyphBb & gbb = m_prggbb[ig];
		gbb.ig = ig;
		gbb.pginf = pginf;
	}


	//	Sort GlyphBb array with left x coor primary key and top y coor secondary key.
	//	Hit testing depends on this sort order.
	::qsort(m_prggbb, m_cgbb, isizeof(GlyphBb), GlyphBbSort);

	//	Iterate over GlyphBb array, assigning each to a GlyphStrm.
	int * rgxsLast = new int[m_cgstrm]; // store last x coor for each glyph strm; use ints
										// because this is for calculating the vdxs array
	int * rgxdLast = new int[m_cgstrm];

	int dxdSum = 0;
	int igbb;
	for (igbb = 0; igbb < m_cgbb; igbb++) // left to right processing
	{
		GlyphBb & gbb = m_prggbb[igbb];
		GlyphIterator git3 = gitStart;
		git3 += m_prggbb[igbb].ig;
		GlyphInfo ginf = *git3;

		//	Find which GlyphStrm matches this glyph's y coord and color.
		int ys = GrEngine::RoundFloat(ginf.yOffset());
		int ich = ginf.firstChar();
		int clrFore;
		int clrBack;
		pctsrc->getColors(ich, &clrFore, &clrBack);

		int igstrm;
		for (igstrm = 0; igstrm < m_cgstrm; igstrm++)
		{
			if (m_prggstrm[igstrm].gsk.ys == ys &&
				m_prggstrm[igstrm].gsk.clrFore == clrFore &&
				m_prggstrm[igstrm].gsk.clrBack == clrBack)
			{
				break;
			}
		}
		Assert(igstrm < m_cgstrm);	// found one

		GlyphStrm & gstrm = m_prggstrm[igstrm];
		gstrm.vchwGlyphId.push_back(ginf.glyphID());
		if (gstrm.vchwGlyphId.size() == 1)
		{
			// first glyph id for this GlyphStrm
			gstrm.xsStart = GrEngine::RoundFloat(ginf.origin());
			rgxdLast[igstrm] = GrEngine::RoundFloat(ScaleXToDest(ginf.origin()));
		}
		else
		{
			// subsequent glyph ids - Dx value for last glyph is set later.
			int xdThis = GrEngine::RoundFloat(ScaleXToDest(ginf.origin()));
			int dxd = xdThis - rgxdLast[igstrm];
			gstrm.vdxd.push_back(dxd);
			rgxdLast[igstrm] = xdThis;
			dxdSum += dxd;
		}

		// store mapping values from BB to GlyphStrm and vice versa.
		gbb.igstrm = igstrm;
		gbb.iGlyph = gstrm.vchwGlyphId.size() - 1;
		gstrm.vigbb.push_back(igbb);

		// store mapping value from final pass to BB
		// OutputSlot(pslot->PosPassIndex())->SetGlyphBbIndex(igbb);
		//////pseg->OutputSlot(gbb.islout)->SetGlyphBbIndex(igbb);
	}

	//	Iterate over GlyphStrms adding Dx value (advance width) for last glyphs
	//  needed so GlyphStrm::GetWidth() can find total width of each glyph stream.
	for (igstrm = 0; igstrm < m_cgstrm; igstrm++)
	{
		int cchw = m_prggstrm[igstrm].vchwGlyphId.size();
		for (igbb = 0; igbb < m_cgbb; igbb++)
		{
			if (m_prggbb[igbb].iGlyph == cchw - 1 && m_prggbb[igbb].igstrm == igstrm)
			{
				GlyphIterator git3 = gitStart;
				git3 += m_prggbb[igbb].ig;
				GlyphInfo ginf = *git3;
				float xsAdvWidth = ginf.advanceWidth();
				int dxdTmp = GrEngine::RoundFloat(xsAdvWidth * m_xFactor);
				m_prggstrm[igstrm].vdxd.push_back(dxdTmp);
				dxdSum += dxdTmp;
				break;
			}
		}
	}

	delete [] rgxsLast;
	delete [] rgxdLast;
}

/*----------------------------------------------------------------------------------------------
	Clear the cache that is built by SetUpFromSegment.
----------------------------------------------------------------------------------------------*/
void WinSegmentPainter::ClearSegmentCache()
{
	delete [] m_prggstrm;
	delete [] m_prggbb;

	m_prggstrm = NULL;
	m_cgstrm = -1;
	m_prggbb = NULL;
	m_cgbb = -1;
}

/*----------------------------------------------------------------------------------------------
	Method used by qsort to sort GlyphBb by their upper left corner
	Primary key: left  secondary key: top  tertiary key: index in final pass

	@param p1, p1		- pointers to GlyphBbs to be compared
----------------------------------------------------------------------------------------------*/
int WinSegmentPainter::GlyphBbSort(const void * p1, const void * p2) // static fn
{
	const GlyphBb * pgbb1 = reinterpret_cast<const GlyphBb *> (p1);
	const GlyphBb * pgbb2 = reinterpret_cast<const GlyphBb *> (p2);
	gr::Rect rect1 = pgbb1->pginf->bb();
	gr::Rect rect2 = pgbb2->pginf->bb();

	int xsDiff = GrEngine::RoundFloat(rect1.left - rect2.left);
	if (xsDiff)
		return xsDiff;
	else
		xsDiff = GrEngine::RoundFloat(rect1.top - rect2.top);

	if (xsDiff)
		return xsDiff;
	else
		return (pgbb1->ig - pgbb2->ig);
}

/*----------------------------------------------------------------------------------------------
	Return the left edge of the segment.
----------------------------------------------------------------------------------------------*/
int WinSegmentPainter::LeftEdge()
{
	return GrEngine::RoundFloat(m_prggbb[0].pginf->bb().left);
}

/*----------------------------------------------------------------------------------------------
	Make sure the device context on which the font is set to use the right character properties
	for this segment, including the given colors needed by the text.
----------------------------------------------------------------------------------------------*/
void WinSegmentPainter::SetFontProps(unsigned long clrFore, unsigned long clrBack)
{
	WinFont * pfontWin = dynamic_cast<WinFont *>(&m_pseg->getFont());
	pfontWin->SetInternalFont(clrFore, clrBack);
}


} // namespace gr

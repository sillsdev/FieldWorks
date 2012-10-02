/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrSegmentPlatform.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Platform-specific routines (using platform-specific data structures) of GrSegment.

OBSOLETE - any platform-specific behavior now belongs in a subclass of SegmentPainter.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "main.h"
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

namespace gr
{

//:>********************************************************************************************
//:>	   Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Shift the glyphs physically by the given amount. This happens when a right-to-left
	segment is changed from being end-of-line to not, so we have to account for the fact
	that the trailing white-space was previous invisible but is now visible.
----------------------------------------------------------------------------------------------*/
void GrSegment::ShiftGlyphsPlatform(float dxsShift)
{
	if (dxsShift == 0)
		return;

	for (int igbb = 0; igbb < m_cgbb; igbb++)
	{
		GlyphBb & gbb = m_prggbb[igbb];
		gbb.xsBbLeft += dxsShift;
		gbb.xsBbRight += dxsShift;
		gbb.xsPosLeft += dxsShift;
		gbb.xsPosRight += dxsShift;
	}
	for (int igstrm = 0; igstrm < m_cgstrm; igstrm++)
	{
		GlyphStrm & gstrm = m_prggstrm[igstrm];
		gstrm.xsStart += dxsShift;
		gstrm.xsStartInt += GrEngine::RoundFloat(dxsShift);
	}
}

/*----------------------------------------------------------------------------------------------
	Constructing.
----------------------------------------------------------------------------------------------*/
void GrSegment::InitializePlatform()
{
	m_prggstrm = NULL;
	m_prggbb = NULL;
}

/*----------------------------------------------------------------------------------------------
	Destroying.
----------------------------------------------------------------------------------------------*/
void GrSegment::DestroyContentsPlatform()
{
	if (m_prggstrm)
		delete[] m_prggstrm;
	if (m_prggbb)
		delete[] m_prggbb;
}


//:>********************************************************************************************
//:>	Segment Interface methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Change the direction of the segment. This is needed specifically for white-space-only
	segments, which are initially created to be in the direction of the paragraph, but then
	later are discovered to not be line-end after all, and need to be changed to use the
	directionality of the writing system.

	@return kresFail for segments that do not have weak directionality and therefore
	cannot be changed.
----------------------------------------------------------------------------------------------*/
bool GrSegment::SetDirectionDepthPlatform(int nNewDepth)
{
	if (m_cgbb <= 1)
	{
		//	Only one glyph--no need to move things.
		m_nDirDepth = nNewDepth;
		return true;
	}

	//	Copy the old bounding box information into a new array, reversing the positions
	//	but keeping them in physical (left-to-right) order.
	GlyphBb * prggbbNew = new GlyphBb [m_cgbb];
	int * prgchwGlyph = new int [m_cgbb];
	for (int igbbOld = 0; igbbOld < m_cgbb; igbbOld++)
	{
		int igbbNew = m_cgbb - igbbOld - 1;
		int islout = m_prggbb[igbbOld].islout;
		int iGlyph = m_prggbb[igbbOld].iGlyph;
		int igstrm = m_prggbb[igbbOld].igstrm;

		prggbbNew[igbbNew].igstrm = m_prggbb[igbbOld].igstrm;
		prggbbNew[igbbNew].islout = islout;
		prggbbNew[igbbNew].ysBbBottom = m_prggbb[igbbOld].ysBbBottom;
		prggbbNew[igbbNew].ysBbTop = m_prggbb[igbbOld].ysBbTop;

		prggbbNew[igbbNew].xsBbLeft = m_dxsTotalWidth - m_prggbb[igbbOld].xsBbRight;
		prggbbNew[igbbNew].xsBbRight = m_dxsTotalWidth - m_prggbb[igbbOld].xsBbLeft;
		prggbbNew[igbbNew].xsPosLeft = m_dxsTotalWidth - m_prggbb[igbbOld].xsPosRight;
		prggbbNew[igbbNew].xsPosRight = m_dxsTotalWidth - m_prggbb[igbbOld].xsPosLeft;

		prgchwGlyph[igbbNew] = m_prggstrm[igstrm].vchwGlyphId[iGlyph];

		OutputSlot(islout)->SetGlyphBbIndex(igbbNew);
	}

	//	Regenerate the glyph streams.
	int igstrm;
	for (igstrm = 0; igstrm < m_cgstrm; igstrm++)
	{
		m_prggstrm[igstrm].vchwGlyphId.clear();
		m_prggstrm[igstrm].vdxs.clear();
	}
	float * rgxsLast = new float[m_cgstrm]; // store last x coor for each glyph strm
	int igbb;
	for (igbb = 0; igbb < m_cgbb; igbb++)
	{
		GlyphBb & gbb = prggbbNew[igbb];
		int igstrm = prggbbNew[igbb].igstrm;
		GlyphStrm & gstrm = m_prggstrm[igstrm];
		gstrm.vchwGlyphId.push_back(prgchwGlyph[igbb]);
		if (gstrm.vchwGlyphId.size() == 1)
		{
			gstrm.xsStart = gbb.xsPosLeft;
			gstrm.xsStartInt = GrEngine::RoundFloat(gbb.xsPosLeft);
			rgxsLast[igstrm] = gstrm.xsStart;
		}
		else
		{
			gstrm.vdxs.push_back(GrEngine::RoundFloat(gbb.xsPosLeft - rgxsLast[igstrm]));
			rgxsLast[igstrm] = gbb.xsPosLeft;
		}

		prggbbNew[igbb].iGlyph = gstrm.vchwGlyphId.size() - 1;
	}

	//	Set final advance widths.
	for (igstrm = 0; igstrm < m_cgstrm; igstrm++)
	{
		int cchw = m_prggstrm[igstrm].vchwGlyphId.size();
		for (igbb = 0; igbb < m_cgbb; igbb++)
		{
			if (prggbbNew[igbb].iGlyph == cchw - 1 && prggbbNew[igbb].igstrm == igstrm)
			{
				float xsAdvWidth = prggbbNew[igbb].xsPosRight - prggbbNew[igbb].xsPosLeft;
				m_prggstrm[igstrm].vdxs.push_back(GrEngine::RoundFloat(xsAdvWidth));
				break;
			}
		}
	}

	delete[] rgxsLast;
	delete[] prgchwGlyph;

	delete[] m_prggbb;
	m_prggbb = prggbbNew;

	m_nDirDepth = nNewDepth;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return the rendered glyphs and their x- and y-coordinates, in the order generated by
	the final positioning pass, relative to the top-left of the segment. They are always
	returned in (roughly) left-to-right order. Note: any of the arrays may be null.
----------------------------------------------------------------------------------------------*/
GrResult GrSegment::GetGlyphsAndPositionsPlatform(float xs, float ys,
	Rect rs, Rect rd,
	utf16 * prgchGlyphs, float * prgxd, float * prgyd, float * prgdxdAdv)
{
	for (int igstrm = 0; igstrm < m_cgstrm; igstrm++)
	{
		GlyphStrm  & gstrm = m_prggstrm[igstrm];

		float xsLeft = xs + gstrm.xsStart;
		float ysTop = ys - gstrm.gsk.ys; // subtract amount above base line, in order to move up

		float xdLeft = SegmentPainter::ScaleX(xsLeft, rs, rd);
		float ydTop = SegmentPainter::ScaleY(ysTop, rs, rd);

		float xd = xdLeft;

		for (size_t ich = 0; ich < gstrm.vchwGlyphId.size(); ich++)
		{
			int igbb = gstrm.vigbb[ich];
			Assert(igbb < m_cgbb);

			if (prgchGlyphs)
				prgchGlyphs[igbb] = gstrm.vchwGlyphId[ich];
			prgxd[igbb] = xd;
			if (prgyd)
				prgyd[igbb] = ydTop;

			xd += SegmentPainter::ScaleX((gstrm.vdxs[ich] + xsLeft), rs, rd) - xdLeft;
		}
	}

	if (prgdxdAdv && m_cgbb > 0)
	{
		// Include advance widths, ie, the distance from the origin of each glyph
		// to the origin of the next...
		for (int igbb = 0; igbb < m_cgbb - 1; igbb++)
			prgdxdAdv[igbb] = prgxd[igbb + 1] - prgxd[igbb];
		// ...except for the last glyph, where we use its true advance width.
		GlyphBb & gbbLast = m_prggbb[m_cgbb-1];
		float dxs = gbbLast.xsPosRight - gbbLast.xsPosLeft;
		prgdxdAdv[m_cgbb - 1] = SegmentPainter::ScaleX(dxs, rs, rd) - rd.left;
	}

	return kresOk;

	// Old code: uses the height of each glyph stream, not the actual bounding boxes.
//	for (int igstrm = 0; igstrm < m_cgstrm; igstrm++)
//	{
//		*pdysVisAscent = max(*pdysVisAscent, *pdysFontAscent + m_prggstrm[igstrm].gsk.ys);
//		*pdysNegVisDescent = min(*pdysNegVisDescent, *pdysNegFontDescent + m_prggstrm[igstrm].gsk.ys);
//	}
}

/*----------------------------------------------------------------------------------------------
	Set up the data structures that represent the actual rendered glyphs for the new segment.
----------------------------------------------------------------------------------------------*/
void GrSegment::SetUpOutputArraysPlatform(GrTableManager * ptman, gid16 chwLB, int nDirDepth,
	int islotMin, int cslot)
{
	//	Iterate over final pass, creating glyph streams corresponding to each distinct
	//	y-coordinate/color combination.
	//	Also count the number of slots needing output and find the lowest x coordinate.
	std::vector<GlyphStrmKey> vgsk;
	float xsMin = 0;
	m_cgbb = 0;
	int islot;
	for (islot = islotMin; islot < cslot; islot++)
	{
		GrSlotState * pslot = m_psstrm->SlotAt(islot);

		if (pslot->GlyphID() != chwLB)
			m_cgbb++;
		else
			continue; // skip over line break markers

		xsMin = min(xsMin, pslot->XPosition());

		float ys = pslot->YPosition();
		int clrFore = pslot->ForeColor();
		int clrBack = pslot->BackColor();
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
				float iTmp;
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

	//	Iterate over final pass, filling in GlyphBb array (islout and bounding box fields).
	int igbb = 0;
	for (islot = islotMin; islot < cslot; islot++)
	{
		GrSlotState * pslot = m_psstrm->SlotAt(islot);
		int islout = islot - islotMin;
		if (pslot->GlyphID() == chwLB)
			continue; // skip over linebreak markers
		GlyphBb & gbb = m_prggbb[igbb];
		gbb.islout = islout;
		gbb.xsBbLeft = pslot->XPosition() + pslot->GlyphMetricLogUnits(ptman, kgmetBbLeft);
		gbb.ysBbTop = pslot->YPosition() + pslot->GlyphMetricLogUnits(ptman, kgmetBbTop);
		if (pslot->IsSpace(ptman))
			gbb.xsBbRight =
				pslot->XPosition() + pslot->GlyphMetricLogUnits(ptman, kgmetAdvWidth);
		else
			gbb.xsBbRight =
				pslot->XPosition() + pslot->GlyphMetricLogUnits(ptman, kgmetBbRight);
		gbb.ysBbBottom = pslot->YPosition() + pslot->GlyphMetricLogUnits(ptman, kgmetBbBottom);
		gbb.xsPosLeft = pslot->XPosition();
		gbb.xsPosRight = pslot->XPosition() + pslot->GlyphMetricLogUnits(ptman, kgmetAdvWidth);
		igbb++;
	}

	//	Sort GlyphBb array with left x coor primary key and top y coor secondary key.
	//	Hit testing depends on this sort order.
	::qsort(m_prggbb, m_cgbb, isizeof(GlyphBb), GlyphBbSort);

	//	Iterate over GlyphBb array, assigning each to a GlyphStrm.
	int * rgxsLast = new int[m_cgstrm]; // store last x coor for each glyph strm; use ints
										// because this is for calculating the vdxs array
	for (igbb = 0; igbb < m_cgbb; igbb++)
	{
		GlyphBb & gbb = m_prggbb[igbb];
		GrSlotState * pslot = m_psstrm->SlotAt(gbb.islout + islotMin);

		//	Find which GlyphStrm matches this glyph's y coord and color.
		float ys = pslot->YPosition();
		int clrFore = pslot->ForeColor();
		int clrBack = pslot->BackColor();
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
		gstrm.vchwGlyphId.push_back(pslot->ActualGlyphForOutput(ptman));
		if (gstrm.vchwGlyphId.size() == 1)
		{
			// first glyph id for this GlyphStrm
			gstrm.xsStart = pslot->XPosition();
			gstrm.xsStartInt = GrEngine::RoundFloat(gstrm.xsStart);
			rgxsLast[igstrm] = gstrm.xsStartInt;
		}
		else
		{
			// subsequent glyph ids - Dx value for last glyph is set later.
			int xsThis = GrEngine::RoundFloat(pslot->XPosition());
			gstrm.vdxs.push_back(xsThis - rgxsLast[igstrm]);
			rgxsLast[igstrm] = xsThis;
		}

		// store mapping values from BB to GlyphStrm and vice versa.
		gbb.igstrm = igstrm;
		gbb.iGlyph = gstrm.vchwGlyphId.size() - 1;
		gstrm.vigbb.push_back(igbb);

		// store mapping value from final pass to BB
		// OutputSlot(pslot->PosPassIndex())->SetGlyphBbIndex(igbb);
		OutputSlot(gbb.islout)->SetGlyphBbIndex(igbb);
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
				int islout = m_prggbb[igbb].islout;
				Assert(OutputSlot(islout)->PosPassIndex() == islout);
				int mAdvWidth =	m_psstrm->SlotAt(islout + islotMin)->AdvanceX(ptman);
				float xsAdvWidth = ptman->EmToLogUnits(mAdvWidth);
				m_prggstrm[igstrm].vdxs.push_back(GrEngine::RoundFloat(xsAdvWidth));
				break;
			}
		}
	}

	delete [] rgxsLast;
}

/*----------------------------------------------------------------------------------------------
	Method used by qsort to sort GlyphStrms by their Y coordinate.
	Currently not used.
----------------------------------------------------------------------------------------------*/
float GrSegment::GlyphKeySort(const void * p1, const void * p2) // static fn
{
	const GlyphStrmKey * pgsk1 = reinterpret_cast<const GlyphStrmKey *> (p1);
	const GlyphStrmKey * pgsk2 = reinterpret_cast<const GlyphStrmKey *> (p2);

	return (abs(pgsk1->ys) - abs(pgsk2->ys)); // sort based on diff from zero
}

/*----------------------------------------------------------------------------------------------
	Method used by qsort to sort GlyphBb by their upper left corner
	Primary key: left  secondary key: top  tertiary key: index in final pass

	@param p1, p1		- pointers to GlyphBbs to be compared
----------------------------------------------------------------------------------------------*/
int GrSegment::GlyphBbSort(const void * p1, const void * p2) // static fn
{
	const GlyphBb * pgbb1 = reinterpret_cast<const GlyphBb *> (p1);
	const GlyphBb * pgbb2 = reinterpret_cast<const GlyphBb *> (p2);

	int xsDiff = GrEngine::RoundFloat(pgbb1->xsBbLeft - pgbb2->xsBbLeft);
	if (xsDiff)
		return xsDiff;
	else
		xsDiff = GrEngine::RoundFloat(pgbb1->ysBbTop - pgbb2->ysBbTop);

	if (xsDiff)
		return xsDiff;
	else
		return (pgbb1->islout - pgbb2->islout);
}

//:>********************************************************************************************
//:>	SegmentPainter Interface methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Draw the text of the segment at the specified point in the graphics context.
----------------------------------------------------------------------------------------------*/
GrResult SegmentPainter::DrawSegmentPlatform(float xs, float ys)
{
	//GrResult res = GetFontAscentSourceUnits(pgg, &dysFontAscent);
	//if (ResultFailed(res))
	//	ReturnResult(res);

	GrResult res;

	//	First we draw the background (because if the background color is
	//	anything other than transparent it will clobber nearby glyphs), and then we
	//	draw the glyphs.

	int igstrm;
	for (igstrm = 0; igstrm < m_pseg->m_cgstrm; igstrm++)
	{
		GrSegment::GlyphStrm & gstrm = m_pseg->m_prggstrm[igstrm];
		if (unsigned(gstrm.gsk.clrBack) == kclrTransparent)
			continue;

		float xsLeft = xs + gstrm.xsStart;
		float ysTop = ys - gstrm.gsk.ys; // subtract amount above base line, in order to move up

		SetFontProps(gstrm.gsk.clrFore, gstrm.gsk.clrBack);

		//	Convert source coords to destination coords. Notice that if the source and
		//	destination rectangles are different, the spacing for each glyph will be
		//	adjusted relative to the beginning of the segment.
		float xdLeft = ScaleXToDest(xsLeft);
		float ydTop = ScaleYToDest(ysTop);

		std::vector<int> vdxd;

		if (m_xsOrigin == m_xdPosition && m_xFactor == 1.0)
		{
			vdxd = gstrm.vdxs;
		}
		else
		{
			float xsPrev = xsLeft;
			float xdPrev = xdLeft;
			for (size_t ixs = 0; ixs < gstrm.vdxs.size(); ixs++)
			{
				// This approach accumulated rounding errors:
				//vdxd.push_back(ScaleX((gstrm.vdxs[ixs] + xsLeft), rs, rd) - xdLeft);
				// Instead:
				float xsNext = gstrm.vdxs[ixs] + xsPrev;
				float xdNext = ScaleXToDest(xsNext);
				vdxd.push_back(GrEngine::RoundFloat(xdNext - xdPrev));
				xsPrev = xsNext;
				xdPrev = xdNext;
			}
		}

		//	Draw the backgrounds one glyph at a time; otherwise we get background color
		//	filled in in places we don't want it.
		int xdInt = GrEngine::RoundFloat(xdLeft);
		int ydTopInt = GrEngine::RoundFloat(ydTop);
		DrawTextExt(xdInt, ydTopInt, 1, &gstrm.vchwGlyphId[0],
			ETO_GLYPH_INDEX, NULL, NULL);
		for (size_t i = 1; i < vdxd.size(); i++)
		{
			xdInt += vdxd[i-1];
			DrawTextExt(xdInt, ydTopInt, 1, &gstrm.vchwGlyphId[i],
				ETO_GLYPH_INDEX, NULL, NULL);
		}
	}

	//	Now draw the glyphs in the foreground.
	for (igstrm = 0; igstrm < m_pseg->m_cgstrm; igstrm++)
	{
		GrSegment::GlyphStrm & gstrm = m_pseg->m_prggstrm[igstrm];
		float xsLeft = xs + gstrm.xsStart;
		float ysTop = ys - gstrm.gsk.ys; // subtract amount above base line, in order to move up

		SetFontProps(gstrm.gsk.clrFore, kclrTransparent);

		//	Convert source coords to destination coords. Notice that if the source and
		//	destination rectangles are different, the spacing for each glyph will be
		//	adjusted relative to the beginning of the segment.
		float xdLeft = ScaleXToDest(xsLeft);
		float ydTop = ScaleYToDest(ysTop);

		std::vector<int> vdxd;

		if (m_xsOrigin == m_xdPosition && m_xFactor == 1.0)
		{
			vdxd = gstrm.vdxs;
		}
		else
		{
			// By now we are using integers, so stick with that.
			int xsPrev = GrEngine::RoundFloat(xsLeft);
			int xdPrev = GrEngine::RoundFloat(xdLeft);
			for (size_t ixs = 0; ixs < gstrm.vdxs.size(); ixs++)
			{
				// This approach accumulated rounding errors:
				//vdxd.push_back(ScaleX((gstrm.vdxs[ixs] + xsLeft), rs, rd) - xdLeft);
				// Instead:
				int xsNext = gstrm.vdxs[ixs] + xsPrev;
				int xdNext = ScaleXToDest(xsNext);

				vdxd.push_back(xdNext - xdPrev);
				xsPrev = xsNext;
				xdPrev = xdNext;
			}
		}

		int xdInt = GrEngine::RoundFloat(xdLeft);
		int ydTopInt = GrEngine::RoundFloat(ydTop);
		DrawTextExt(xdInt, ydTopInt, vdxd.size(), &(gstrm.vchwGlyphId[0]),
			ETO_GLYPH_INDEX, NULL, &(vdxd[0]));
	}

	// Experiment for opaque drawing:
	//if (m_pseg->m_prggstrm[0].gsk.clrBack == kclrTransparent)
	//	SetFontProps(kclrWhite, kclrTransparent);
	//else
	//	SetFontProps(m_pseg->m_prggstrm[0].gsk.clrBack, kclrTransparent);
	//int xsLeftTmp = m_pseg->m_prggstrm[0].xsStart;
	//int ysTopTmp = m_pseg->m_prggstrm[0].gsk.ys;
	//int xdLeftTmp = ScaleXToDest(xsLeftTmp);
	//int ydTopTmp = ScaleYToDest(ysTopTmp);
	//gid16 gtmp = 642;
	//DrawTextExt(xdLeftTmp, ydTopTmp, 1, &gtmp, ETO_GLYPH_INDEX, NULL, NULL);
	//SetFontProps(m_pseg->m_prggstrm[0].gsk.clrFore, kclrTransparent);
	//gtmp = 653;
	//DrawTextExt(xdLeftTmp, ydTopTmp, 1, &gtmp, ETO_GLYPH_INDEX, NULL, NULL);

	ReturnResult(kresOk);
}

// REVIEW: should all these methods test the input args?



} // namespace gr

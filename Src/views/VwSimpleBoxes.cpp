/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwSimpleBoxes.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This provides the implementations for the boxes used for ordinary textual and interlinear
	layouts: VwBox, VwGroupBox, VwPileBox, VwDivBox.

-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

//#include <algorithm> //REVIEW JohnT(?): can we use std::sort safely?
#include <limits.h>

using namespace std;

#undef THIS_FILE
DEFINE_THIS_FILE

//#define _DEBUG_SHOW_BOX

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Constructors, Destructors
//:>********************************************************************************************

VwBox::VwBox(VwPropertyStore * pzvps)
	:m_qzvps(pzvps), m_pgboxContainer(0), m_pboxNext(0),
	m_ysTop(0), m_xsLeft(0), m_dxsWidth(0), m_dysHeight(0)
{
}

VwBox::~VwBox()
{
	// Inform any IAccessible implementations that they may no longer point at this box.
#if WIN32
	VwAccessRoot::BoxDeleted(this);
#endif
	VwRootBox * prootb = this->Root();
	if (prootb)
	{
		NotifierVec vpanoteDel;
		prootb->DeleteNotifiersFor(this, -1, vpanoteDel);
		Assert(vpanoteDel.Size() == 0);
	}
}

//:>********************************************************************************************
//:>	VwBox methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Answer true if the recipient is or follows the argument, that is, the recipient is in the
	chain of boxes starting at pbox.
	pbox may be null, in which case, it always answers false
----------------------------------------------------------------------------------------------*/
bool VwBox::IsOrFollows(VwBox * pbox)
{
	while (pbox)
	{
		if (pbox == this)
			return true;
		pbox = pbox->NextOrLazy();
	}
	return false;
}

//certain table classes need to override these
int VwBox::BorderTop()
{
	return m_qzvps->BorderTop();
}

int VwBox::BorderBottom()
{
	return m_qzvps->BorderBottom();

}

int VwBox::BorderLeading()
{
	return m_qzvps->BorderLeading();

}

int VwBox::BorderTrailing()
{
	return m_qzvps->BorderTrailing();

}

//give the ascent of the box. It defaults to the height.
int VwBox::Ascent()
{
	return this->Height();
}

// By default a box does not have to be the end of a line.
bool VwBox::MustEndLine()
{
	return false;
}

// It is an error to try to mark a box as ending a line if it can't handle that state.
void VwBox::SetMustEndLine(bool f)
{
	Assert(!f);
}

// The default just makes height and width 0.
void VwBox::DoLayout(IVwGraphics* pvg, int dxpAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
	m_dxsWidth = 0;
	m_dysHeight = 0;
}

/*----------------------------------------------------------------------------------------------
	Get the next box. Debug build verifies that it is not lazy. This is the original method for
	getting the next box, but new code should generally use NextOrLazy or NextOrExpand. The
	current implementation here basically checks that old code did not need to be updated.
----------------------------------------------------------------------------------------------*/
VwBox * VwBox::Next()
{
	Assert(!dynamic_cast<VwLazyBox *>(m_pboxNext));
	return m_pboxNext;
}

/*----------------------------------------------------------------------------------------------
	When the contents of a box changes, and its size consequently might change, something
	needs to be done about the layout of that box and all its containers.
	The process begins by constructing a FixupMap, which contains the changed box and all
	its parents, each recording (against the box as a key) the invalidate rectangle appropriate
	to the old box layout.
	We then pass this to the Relayout method of the root box.
	By default, any box which finds itself in the map, or which has never been laid out
	(its height is zero), does a full normal layout, and invalidates its old (if any)
	rectangle. It can't invalidate its new rectangle, because at the point where relayout
	is called, the parent box may not have finalized the new position of the child...
	so return true if the parent needs to invalidate the new position.
	Relayout should not be used in cases where the available width may have changed, as this
	could affect the layout of boxes that are not in the map.
	Note that, if the box moves, invalidating its new size at its old position, or vice versa,
	may not do much good. If it moves, the containing box must do appropriate extra
	invalidating.
	Some boxes, notably VwDivBox, may not need to relayout all their children, or even to
	invalidate all their own contents. This can be an important optimization, but must be
	done with care to ensure that what is actually drawn is always correct.
----------------------------------------------------------------------------------------------*/
bool VwBox::Relayout(IVwGraphics * pvg, int dxpAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int dxpAvailOnLine, BoxIntMultiMap * pmmbi)
{
	Rect vrect;
	VwBox * pboxThis = this; // seems to be needed for calling Retrieve...
	bool fGotOldRect = pfixmap->Retrieve(pboxThis, &vrect);
	if (fGotOldRect)
		Root()->InvalidateRect(&vrect); // Invalidate the space it used to occupy.

	// If it is in the map or has never been laid out (height = 0), we need to lay it out.
	if (fGotOldRect || m_dysHeight == 0)
	{
		this->DoLayout(pvg, dxpAvailWidth, dxpAvailOnLine);
		// JohnT: this seems like a good idea, but in fact, if we put a box in the layout
		// map, we put its parent in also, and the parent can invalidate the new position
		// well enough. And the paragraph layout crashes if we put this in, because the
		// paragraph can't position the box until it is laid out, which means that
		// the attempt to figure where the new box is during layout fails (it isn't yet
		// in the list of child boxes of its parent, even). Even Pile and Table containers
		// can't necessarily give a reliable indication of the position of a child while
		// doing the relayout of that child.
		//this->Invalidate();
		return true;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Generally an inner pile can be re-laid-out using the normal algorithm: do nothing
	unless it's in the fix map.
	However, there is one exception:
		If its layout was affected by aligning inner pile contents on a line, then we need to
		do a full layout, to get back to the natural inner box positions, since there may
		now be a different set of other things on the same line.
	Optimize JohnT: in this situation we don't need to relayout the child boxes, so we could
	do something more like DivBox, and redo laying out the inner boxes, doing their layout
	only if they're in the fix map.
----------------------------------------------------------------------------------------------*/
bool VwInnerPileBox::Relayout(IVwGraphics * pvg, int dxpAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int dxpAvailOnLine, BoxIntMultiMap * pmmbi)
{
	bool result = true;
	if (m_fRelayoutRequiresLayout)
		this->DoLayout(pvg, dxpAvailWidth, dxpAvailOnLine);
	else
		result = SuperClass::Relayout(pvg, dxpAvailWidth, prootb, pfixmap, dxpAvailOnLine, pmmbi);
	AdjustBorderedParas(pvg);
	ComputeAscent();
	return result;
}

/*----------------------------------------------------------------------------------------------
	To produce the desired visual effect when we use borders in an inner pile,
	we want every bordered paragraph to have a width equal to the whole inner pile.
----------------------------------------------------------------------------------------------*/
void VwInnerPileBox::AdjustBorderedParas(IVwGraphics * pvg)
{
	int dxpInch;
	CheckHr(pvg->get_XUnitsPerInch(&dxpInch));
	int dxpSurroundWidth = SurroundWidth(dxpInch);
	int dxpInnerWidth = Width() - dxpSurroundWidth;

	// Don't try to do this trick if centering or justifying...maybe one day.
	int tal = Style()->ParaAlign();
	if (tal == ktalJustify || tal == ktalCenter)
		return;

	bool fAnyStretched = false;

	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		fAnyStretched = pbox->StretchToPileWidth(dxpInch, dxpInnerWidth, tal) || fAnyStretched;
	}
	// If anything stretched we need to do a full layout when 're' laying out.
	m_fRelayoutRequiresLayout  |= fAnyStretched;
	AdjustBoxHorizontalPositions(dxpInch);
}

void VwInnerPileBox::DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
	SuperClass::DoLayout(pvg, dxAvailWidth, dxpAvailOnLine, fSyncTops);
	AdjustBorderedParas(pvg);
	ComputeAscent();
}


/*----------------------------------------------------------------------------------------------
	This is part of the algorithm for laying out (possibly nested) inner piles in a paragraph
	so that everything that is supposed to align does.
	We first do a normal layout of the individual boxes on a line of the paragraph.
	We work through the boxes of the inner piles of the line, calling this
	routine for each box. The result is to create a vector, vdyBaselines, of the y offset
	of the baseline of each aligned row of non-inner-pile boxes in the line.
	A box other than a paragraph that has inner piles or an inner pile does the following:
	- computes its own aligned baseline. This is the max of:
		- vdyBaselines[irow], if vdyBaselines has that many items.
		- its current baseline, relative to the top of *pvpboxAligner.
		- dyBottomPrevious + its own ascent.
	- if vdyBaselines[irow] does not exist, it pushes the aligned baseline value.
	- otherwise, if the aligned baseline is greater than vdyBaselines[irow], it adds
		the difference to row [irow] and any subsequent rows.
	- it increments irow
	- it returns its own bottom, relative to pvpboxAligner, after adjustment:
		actually its aligned baseline plus its descent
	- if it concluds that some box's baseline isn't right, it sets fNeedAdjust to true.
----------------------------------------------------------------------------------------------*/
int VwBox::ComputeInnerPileBaselines(VwParagraphBox * pvpboxAligner, IntVec & vdyBaselines,
	int dyBottomPrevious, int & irow, bool & fNeedAdjust)
{
	// Compute the offset from the top of our container to the top of the container we're
	// aligning everything to.
	int dyContainerAligner = 0;
	for (VwGroupBox * pgbox = Container(); pgbox && pgbox != pvpboxAligner; pgbox = pgbox->Container())
		dyContainerAligner += pgbox->Top();

	// The aligned baseline is the max of our current baseline, the baseline required for this
	// row by previous bundles, and the baseline required for this to fit under the previous
	// box in the same column.
	// Enhance JohnT: This does not enforce top and bottom margin constraints on this and the previous box.
	int dyAlignedBaseline = Baseline() + dyContainerAligner;
	if (vdyBaselines.Size() > irow && vdyBaselines[irow] > dyAlignedBaseline)
		dyAlignedBaseline = vdyBaselines[irow];
	dyAlignedBaseline = std::max(dyAlignedBaseline, dyBottomPrevious + Ascent());

	if (dyAlignedBaseline != Baseline() + dyContainerAligner)
		fNeedAdjust = true; // this box needs adjusting

	if (vdyBaselines.Size() > irow)
	{
		int delta = dyAlignedBaseline - vdyBaselines[irow];
		if (delta)
		{
			for (int j = irow; j < vdyBaselines.Size(); j++)
				vdyBaselines[j] += delta;
			fNeedAdjust = true; // previous boxes need adjusting.
		}
	}
	else
	{
		vdyBaselines.Push(dyAlignedBaseline);
	}
	irow++;
	return dyAlignedBaseline + Bottom() - Baseline();
}

/*----------------------------------------------------------------------------------------------
	This is part of the algorithm for laying out (possibly nested) inner piles in a paragraph
	so that everything that is supposed to align does.
	We first do a normal layout of the individual boxes on a line of the paragraph.
	We work through the boxes of the inner piles of the line, calling this
	routine for each box. The result is to create a vector, vdyBaselines, of the y offset
	of the baseline of each aligned row of non-inner-pile boxes in the line.
	For an inner pile, which is therefore a nested inner pile, it calls the routine for
	each of its own inner piles. This results in only the non-inner-pile real bits of
	data counting as rows.
----------------------------------------------------------------------------------------------*/
int VwInnerPileBox::ComputeInnerPileBaselines(VwParagraphBox * pvpboxAligner, IntVec & vdyBaselines,
	int dyBottomPrevious, int & irow, bool & fNeedAdjust)
{
	int adjustedBottom = dyBottomPrevious;
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextRealBox())
		adjustedBottom = pbox->ComputeInnerPileBaselines(pvpboxAligner, vdyBaselines,
			adjustedBottom, irow, fNeedAdjust);
	return adjustedBottom;
}

/*----------------------------------------------------------------------------------------------
	For an inner pile, this is obtained by adding the line values for all children, and taking
	the max of the col values for all children.
----------------------------------------------------------------------------------------------*/
void VwInnerPileBox::CountColumnsAndLines(int * pcCol, int * pcLines)
{
	int cColMax = 1;
	int cLinesTotal = 0;
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		int cCol, cLines;
		pbox->CountColumnsAndLines(&cCol, &cLines);
		if (cCol > cColMax)
			cColMax = cCol;
		cLinesTotal += cLines;
	}
	*pcCol = cColMax;
	*pcLines = cLinesTotal;
}


/*----------------------------------------------------------------------------------------------
	This is part of the algorithm for laying out (possibly nested) inner piles in a paragraph
	so that everything that is supposed to align does.
	We first do a normal layout of the individual boxes on a line of the paragraph.
	We work through the boxes of the inner piles of the line, calling this
	routine for each box. The result is to create a vector, vdyBaselines, of the y offset
	of the baseline of each aligned row of non-inner-pile boxes in the line.
	A paragraph, which is therefore a nested paragraph inside a bundle, if receiving this
	message, is currently required to contain either only inner piles or no inner piles.
	If it contains none, it is treated like a normal box.
	If it contains only inner piles, we call the routine recursively for each inner pile,
	and determine the maximum resulting dyBottomPrevious and irow.
	Pathologically it is possible that such a nested paragraph has multiple lines.
	In this case, the sort of alignment we are trying for can't happen.
	We then do it for the first line of inner piles only.
----------------------------------------------------------------------------------------------*/
int VwParagraphBox::ComputeInnerPileBaselines(VwParagraphBox * pvpboxAligner, IntVec & vdyBaselines,
	int dyBottomPrevious, int & irow, bool & fNeedAdjust)
{
	int irowMax = 0;
	int dyBottomMax = INT_MIN;
	bool fGotInnerPile = false;
	int bottomMax = 0;
	int baseline = 0;
	if (FirstBox() != NULL)
		baseline = FirstBox()->Baseline();
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextRealBox())
	{
		if (pbox->Baseline() != baseline)
			break;
		if (pbox->IsInnerPileBox())
		{
			bottomMax = std::max(bottomMax, pbox->Bottom());
			int irow2 = irow;
			int dyBottom = pbox->ComputeInnerPileBaselines(pvpboxAligner, vdyBaselines,
				dyBottomPrevious, irow2, fNeedAdjust);
			dyBottomMax = std::max(dyBottomMax, dyBottom);
			irowMax = std::max(irowMax, irow2);
			fGotInnerPile = true;
		}
	}
	if (!fGotInnerPile)
	{
		return SuperClass::ComputeInnerPileBaselines(pvpboxAligner, vdyBaselines,
			dyBottomPrevious, irow, fNeedAdjust);
	}
	else
	{
		irow = irowMax;
		// result is the bottom that the worst inner pile will have when adjusted, plus enough
		// space for any margin at the bottom of this box, as indicated by how much its height goes below the
		// bottom of any of its boxes.
		return dyBottomMax + Height() - bottomMax;
	}
}

/*----------------------------------------------------------------------------------------------
	This is part of the algorithm for laying out (possibly nested) inner piles in a paragraph
	so that everything that is supposed to align does.
	Compute the baselines for each row, using ComputeInnerPileBaselines.
	Then we call this routine for each box of each inner pile on the line.
	For a default box, it aligns its own baseline with vdyBaselines[irow] and increments irow.
----------------------------------------------------------------------------------------------*/
void VwBox::AdjustInnerPileBaseline(VwParagraphBox * pvpboxAligner, IntVec & vdyBaselines,
	int & irow)
{
	// Compute the offset from the top of our container to the top of the container we're
	// aligning everything to.
	int dyContainerAligner = 0;
	for (VwGroupBox * pgbox = Container(); pgbox && pgbox != pvpboxAligner; pgbox = pgbox->Container())
		dyContainerAligner += pgbox->Top();

	int delta = vdyBaselines[irow] - (Baseline() + dyContainerAligner); // amount to add to top
	Top(Top() + delta);
	irow++;
}

/*----------------------------------------------------------------------------------------------
	This is part of the algorithm for laying out (possibly nested) inner piles in a paragraph
	so that everything that is supposed to align does.
	Compute the baselines for each row, using ComputeInnerPileBaselines.
	Then we call this routine for each box of each inner pile on the line.
	For an inner pile, it basically just wants to let the embedded leaf boxes adjust
	themselves; except that we do some tricks to ensure that the inner pile still encloses its
	contents.
----------------------------------------------------------------------------------------------*/
void VwInnerPileBox::AdjustInnerPileBaseline(VwParagraphBox * pvpboxAligner, IntVec & vdyBaselines,
	int & irow)
{
	if (!FirstBox())
		return; // no children, nothing to do.
	int topFirst = FirstBox()->Top();
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextRealBox())
	{
		pbox->AdjustInnerPileBaseline(pvpboxAligner, vdyBaselines, irow);
	}
	int delta = FirstBox()->Top() - topFirst; // how much got added to top of first box

	// Move the whole inner pile down that much, and to cancel this out, move the children
	// back up.
	Top(Top() + delta);
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextRealBox())
	{
		pbox->Top(pbox->Top() - delta);
	}

	// Make sure *this is at least high enough to contain all its children.
	int dyRequiredHeight = LastBox()->VisibleBottom();
	if (Height() < dyRequiredHeight)
		_Height(dyRequiredHeight);
}

/*----------------------------------------------------------------------------------------------
	This is part of the algorithm for laying out (possibly nested) inner piles in a paragraph
	so that everything that is supposed to align does.
	Compute the baselines for each row, using ComputeInnerPileBaselines.
	Then we call this routine for each box of each inner pile on the line.
	For an inner pile, it basically just wants to let the embedded leaf boxes adjust
	themselves; except that we do some tricks to ensure that the inner pile still encloses its
	contents.
	Enhance JohnT: this really isn't very smart about top and especially bottom margins.
	The whole algorithm needs adjusting if we're doing complex margins on nested paragraphs.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::AdjustInnerPileBaseline(VwParagraphBox * pvpboxAligner, IntVec & vdyBaselines,
	int & irow)
{
	if (!FirstBox())
		return; // no children, nothing to do.
	int minTop = FirstBox()->Top();
	bool fGotInnerPile = false;
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextRealBox())
	{
		minTop = std::min(minTop, pbox->Top());
		if (pbox->IsInnerPileBox())
			fGotInnerPile = true;
	}
	if (!fGotInnerPile)
	{
		SuperClass::AdjustInnerPileBaseline(pvpboxAligner, vdyBaselines, irow);
		return;
	}

	int irowMax = 0;
	int newMinTop = INT_MAX;
	int newMaxBottom = INT_MIN;
	int firstBaseline = 0;  // only try to align stuff on the first line of nested paragraphs.
	if (FirstBox())
		firstBaseline = FirstBox()->Baseline();
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextRealBox())
	{
		if (pbox->Baseline() == firstBaseline)
		{
			int irow2 = irow;
			pbox->AdjustInnerPileBaseline(pvpboxAligner, vdyBaselines, irow2);
			irowMax = max(irowMax, irow2);
		}
		newMinTop = min(newMinTop, pbox->Top());
		newMaxBottom = max(newMaxBottom, pbox->Bottom());
	}
	int delta = newMinTop - minTop; // how much got added to top of first row

	// Move the whole paragraph down that much, and to cancel this out, move the children
	// back up.
	Top(Top() + delta);
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextRealBox())
	{
		pbox->Top(pbox->Top() - delta);
	}
	newMaxBottom -= delta;

	// Make sure *this is at least high enough to contain all its children.
	if (Height() < newMaxBottom)
		_Height(newMaxBottom);
	irow = irowMax;
}

/*----------------------------------------------------------------------------------------------
	Prepare a FixupMap to handle changes to the size of this box.
	Include 'this' in the map if the caller has not already handled layout and invalidating
	for it (pass fIncludeThis true to include it if this has not been done).
----------------------------------------------------------------------------------------------*/
void VwBox::PrepareFixupMap(FixupMap & fixmap, bool fIncludeThis)
{
	VwBox * pboxContainer = fIncludeThis ? this : this->Container();
	for (; pboxContainer; pboxContainer = pboxContainer->Container())
	{
		Rect vwrect = pboxContainer->GetInvalidateRect();
		fixmap.Insert(pboxContainer, vwrect);
	}
}

/*----------------------------------------------------------------------------------------------
	This may not be used at all anymore, but its theoretically possible if were changing the
	size of a box in a synched view but we're not expanding items and we're not in the process
	of synchronizing. We tried to prove that we can't get here anymore, but it was hopeless.
----------------------------------------------------------------------------------------------*/
void VwBox::FixSync(VwSynchronizer *psync, VwRootBox * prootb)
{
	VwPileBox * pboxp = dynamic_cast<VwPileBox *>(Container());
	VwNotifier * pnote = pboxp->GetLowestNotifier(this);
	if (!pnote)
		return; // can't synchronize
	int dypTopToTopNatural = NaturalTopToTop();
	int dypTopToTopActual = psync->SyncNaturalTopToTop(prootb, pnote->Object(),
		dypTopToTopNatural);
	int dypOrigTop = Top();
	pboxp->SetActualTopToTop(this, dypTopToTopActual);
	int dypAddToTop = Top() - dypOrigTop;
	// Fix the top of the changed box and everything below it.
	for (VwBox * pbox = NextOrLazy(); pbox; pbox = pbox->NextOrLazy())
		pbox->Top(pbox->Top() + dypAddToTop);
}

/*----------------------------------------------------------------------------------------------
	The size of this box changed, which may affect layout of any of its containing boxes
	and require various things to be invalidated. Take care of it.
	(Assumes layout of this box is already OK, and that its old area has been invalidated
	if necessary. Invalidates its new area.)
	Also fix anything that is synchronized with this box.
----------------------------------------------------------------------------------------------*/
void VwBox::SizeChanged()
{
	VwRootBox * prootb = this->Root();
	HoldLayoutGraphics hg(prootb);
	VwSynchronizer * psync = prootb->GetSynchronizer();
	if (psync)
	{
		// A change may affect synchronization of both this and the following box.
		FixSync(psync, prootb);
	}
	FixupMap fixmap;
	PrepareFixupMap(fixmap, false);
	prootb->RelayoutRoot(hg.m_qvg, &fixmap);
	Invalidate();
}

void VwBox::SetActualTopToTop(int dyp)
{
	VwPileBox * pboxp = dynamic_cast<VwPileBox *>(Container());
	if (!pboxp)
		return;
	pboxp->SetActualTopToTop(this, dyp);
}

/*----------------------------------------------------------------------------------------------
	Get the natural distance from the top of the previous box (or top of containing pile, if
	no previous box) to the top of this one (in the absence of synchronization).
	(If not contained in a pile, will return 0.)
----------------------------------------------------------------------------------------------*/
int VwBox::NaturalTopToTop()
{
	VwPileBox * pboxp = dynamic_cast<VwPileBox *>(Container());
	if (!pboxp)
		return 0;
	VwBox * pboxPrev = pboxp->BoxBefore(this);
	int dypInch = Root()->DpiSrc().y;
	if (pboxPrev)
		return pboxp->ComputeTopOfBoxAfter(pboxPrev, dypInch) - pboxPrev->Top();
	else
		return pboxp->FirstBoxTopY(dypInch);
}

/*----------------------------------------------------------------------------------------------
	Get the natural distance from the top of the this box to the top of the next one
	(in the absence of synchronization), or where the next one would go if there were
	one.
	(If not contained in a pile, will return 0.)
----------------------------------------------------------------------------------------------*/
int VwBox::NaturalTopToTopAfter()
{
	VwPileBox * pboxp = dynamic_cast<VwPileBox *>(Container());
	if (!pboxp)
		return 0;
	int dypInch = Root()->DpiSrc().y;
	return pboxp->ComputeTopOfBoxAfter(this, dypInch) - Top();
}

// Answer the last box in the chain linked by Next(). Might be a lazy box.
VwBox * VwBox::EndOfChain()
{
	VwBox * pboxPrev = this;
	for (VwBox* pbox = NextOrLazy(); pbox; pbox = pbox->NextOrLazy())
		pboxPrev = pbox;
	return pboxPrev;
}


void VwBox::Draw(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
	DrawBorder(pvg, rcSrc, rcDst);
	DrawForeground(pvg, rcSrc, rcDst);
}

// The version of the method adds two extra arguments:
// ysTop and dysHeight specify a vertical range within the box that should be drawn.
// Currently the only difference between this and the simpler Draw is passing the
// same arguments on to an override of DrawForeground.
// Currently, these values are always ones found as page boundaries; thus, child box borders
// can be assumed to be entirely on one page or the other and don't need to be restricted
// specially. Thus we don't have a special version of DrawBorders with the extra args.
// ysTop is a distance from the top of this box, in the same resolution as the original
// layout (and hence, the same as the box's own top, height, etc). It may be negative,
// if the top of page is above the top of the box.
// dysHeight is, at the same resolution, the height of the region to draw.
// For inverted views, ysTop is the BOTTOM of the page (think of it more as the start
// position for the boxes on the page), and dysHeight is negative, so that again,
// ysTop + dysHeight gives the end of the stuff we want on this page, in the direction
// lines are advancing.
void VwBox::Draw(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTop, int dysHeight,
				 bool fDisplayPartialLines)
{
	DrawBorder(pvg, rcSrc, rcDst);
	DrawForeground(pvg, rcSrc, rcDst, ysTop, dysHeight, fDisplayPartialLines);
}

void VwBox::DrawBorder(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
	if (BorderTop() || BorderBottom()
		|| BorderLeading() || BorderTrailing()
		|| m_qzvps->BackColor() != kclrTransparent)
	{
		ComBool frtl;
		CheckHr(Style()->get_RightToLeft(&frtl));
		// Margin thicknesses
		int dxsMLeft = MulDiv(m_qzvps->MarginLeading(), rcSrc.Width(), kdzmpInch);
		int dysMTop = MulDiv(m_qzvps->MarginTop(), rcSrc.Height(), kdzmpInch);
		int dxsMRight = MulDiv(m_qzvps->MarginTrailing(), rcSrc.Width(), kdzmpInch);
		int dysMBottom = MulDiv(m_qzvps->MarginBottom(), rcSrc.Height(), kdzmpInch);

		if (frtl)
			SwapVars(dxsMLeft, dxsMRight);

		// outside of border rectangle
		int xdLeftBord = rcSrc.MapXTo(dxsMLeft + m_xsLeft, rcDst);
		int ydTopBord = rcSrc.MapYTo(dysMTop + m_ysTop, rcDst);
		int xdRightBord = rcSrc.MapXTo(m_xsLeft + m_dxsWidth - dxsMRight, rcDst);
		int ydBottomBord = rcSrc.MapYTo(m_ysTop + m_dysHeight - dysMBottom, rcDst);

		// Border thickness in twips.
		int dxsLeftBord = MulDivFixZero(this->BorderLeading(), rcSrc.Width(), kdzmpInch);
		int dysTopBord = MulDivFixZero(this->BorderTop(), rcSrc.Height(), kdzmpInch);
		int dxsRightBord = MulDivFixZero(this->BorderTrailing(), rcSrc.Width(), kdzmpInch);
		int dysBottomBord = MulDivFixZero(this->BorderBottom(), rcSrc.Height(), kdzmpInch);

		if (frtl)
			SwapVars(dxsLeftBord, dxsRightBord);

		// Thickness of border. Measure in dest coords, so that the same source
		// thickness always comes out the same drawing thickness.
		int dxdLeftBord = MulDiv(dxsLeftBord, rcDst.Width(), rcSrc.Width());
		int dydTopBord = MulDiv(dysTopBord, rcDst.Height(), rcSrc.Height());
		int dxdRightBord = MulDiv(dxsRightBord, rcDst.Width(), rcSrc.Width());
		int dydBottomBord = MulDiv(dysBottomBord, rcDst.Height(), rcSrc.Height());

		// Make sure dest border thickness is not zero unless source thickness is.
		if (dxsLeftBord && !dxdLeftBord)
			dxdLeftBord = 1;
		if (dysTopBord && !dydTopBord)
			dydTopBord = 1;
		if (dxsRightBord && !dxdRightBord)
			dxdRightBord = 1;
		if (dysBottomBord && !dydBottomBord)
			dydBottomBord = 1;

		// inside of border rectangle, outside of pad rectangle
		int xdLeftPad = xdLeftBord + dxdLeftBord;
		int ydTopPad = ydTopBord + dydTopBord;
		int xdRightPad = xdRightBord - dxdRightBord;
		int ydBottomPad = ydBottomBord - dydBottomBord;

		// no pad, border, or margin to left of extension box.
		if (IsBoxFromTsString())
			xdLeftPad = xdLeftBord = rcSrc.MapXTo(m_xsLeft, rcDst);

		// no pad, border, or margin to right of box followed by
		// extension
		if (m_pboxNext && m_pboxNext->IsBoxFromTsString())
			xdRightPad = xdRightBord = rcSrc.MapXTo(m_xsLeft + m_dxsWidth, rcDst);

		// Draw background
		if (m_qzvps->BackColor() != kclrTransparent)
		{
			CheckHr(pvg->put_BackColor(m_qzvps->BackColor()));
			CheckHr(pvg->DrawRectangle(xdLeftPad, ydTopPad, xdRightPad, ydBottomPad));
		}

		// Draw border lines. We initially set the background color because we draw the
		// borders using rectangles, and DrawRectangle uses the background color
		CheckHr(pvg->put_BackColor(m_qzvps->BorderColor()));
		if (xdLeftPad != xdLeftBord)
			CheckHr(pvg->DrawRectangle(xdLeftBord, ydTopBord,
					xdLeftPad, ydBottomBord));
		if (ydTopBord != ydTopPad)
			CheckHr(pvg->DrawRectangle(xdLeftBord, ydTopBord,
					xdRightBord, ydTopPad));
		if (xdRightPad != xdRightBord)
			CheckHr(pvg->DrawRectangle(xdRightPad, ydTopBord,
					xdRightBord, ydBottomBord));
		if (ydBottomPad != ydBottomBord)
			CheckHr(pvg->DrawRectangle(xdLeftBord, ydBottomPad,
					xdRightBord, ydBottomBord));
	}
}

void VwBox::DebugDrawBorder(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
	int left = rcSrc.MapXTo(m_xsLeft, rcDst);
	int top = rcSrc.MapYTo(m_ysTop, rcDst);
	int right = rcSrc.MapXTo(m_xsLeft + m_dxsWidth, rcDst);
	int bottom = rcSrc.MapYTo(m_ysTop + m_dysHeight, rcDst);

	VwBox * pboxContainer;
	int iDepth=0;
	for (pboxContainer = this; pboxContainer; pboxContainer = pboxContainer->Container())
	{
		++iDepth;
	}

	switch(iDepth) {
		case 0:
			CheckHr(pvg->put_BackColor(kclrPaleTurquoise));
			break;
		case 1:
			CheckHr(pvg->put_BackColor(kclrLavender));
			break;
		case 2:
			CheckHr(pvg->put_BackColor(kclrPaleBlue));
			break;
		case 3:
			CheckHr(pvg->put_BackColor(kclrPink));
			break;
		case 4:
			CheckHr(pvg->put_BackColor(kclrYellow));
			break;
		case 5:
			CheckHr(pvg->put_BackColor(kclrTurquoise));
			break;
		case 6:
			CheckHr(pvg->put_BackColor(kclrGold));
			break;
		case 7:
			CheckHr(pvg->put_BackColor(kclrSkyBlue));
			break;
		case 8:
			CheckHr(pvg->put_BackColor(kclrLightYellow));
			break;
		case 9:
			CheckHr(pvg->put_BackColor(kclrPaleGreen));
			break;
		case 10:
			CheckHr(pvg->put_BackColor(kclrLightOrange));
			break;
		default:
			// transparent
			break;
	}

	CheckHr(pvg->DrawRectangle(left, top, right, bottom));

	CheckHr(pvg->put_BackColor(kclrBlack));
	// Left
	CheckHr(pvg->DrawRectangle(left, top, left+1, bottom));
	// Right
	if (left != right)
		CheckHr(pvg->DrawRectangle(right-1, top, right, bottom));
	// Top
	CheckHr(pvg->DrawRectangle(left, top, right, top+1));
	// Bottom
	if (top != bottom)
		CheckHr(pvg->DrawRectangle(left, bottom-1, right, bottom));
}


// draw the contents of the box. Assume background has been erased
// and any border has been drawn.
void VwBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
#ifdef _DEBUG
#ifdef _DEBUG_SHOW_BOX
	DebugDrawBorder(pvg, rcSrc, rcDst);
#endif
#endif
	//default is to do nothing.
}

// Check whether this is a key in the mmbi, and if so, notify the relevant layout
// manager that the page is broken. Note: we are counting on the fact that this
// removes the VwPage as it sends the notification. This ensures that no notification
// is sent twice, even if we find two or more reasons that the same page is broken.
void VwBox::CheckBoxMap(BoxIntMultiMap * pmmbi, VwRootBox * prootb)
{
		// This box is modified...if it is a key in pmmbi, the page is broken
	if (pmmbi)
	{
		VwLayoutStream * play = dynamic_cast<VwLayoutStream *>(prootb);
		Assert(play); // pmmbi should only be passed non-null for layout streams.
		BoxIntMultiMap::iterator it, itLim;
		VwBox * pboxKey = this;
		pmmbi->Retrieve(pboxKey, &it, &itLim);
		// loop over the affected pages, marking them broken.
		for (; it != itLim; ++it)
		{
			int hPage = it.GetValue();
			VwPage * ppage = play->FindPage(hPage);
			if (!ppage)
				continue; // page deleted, notification already sent.
			ppage->m_fPageBroken =true;
		}
	}
}

// The version of the method adds two extra arguments:
// ysTop and dysHeight specify a vertical range within the box that should be drawn.
// Currently this only needs to be treated specially by piles and paragraphs, so
// other box classes use their default DrawForeground (without the extra arguments).
// see the description of Draw for details of the extra arguments.
// @param fDisplayPartialLines Set to true to display lines even if they don't fit entirely in
//                             the paragraph box.
void VwBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTop, int dysHeight,
						   bool fDisplayPartialLines)
{
	//default is to ignore the extra arguments.
	DrawForeground(pvg, rcSrc, rcDst);
}

// answer the root box containing this one, if any.
VwRootBox* VwBox::Root()
{
	if (!m_pgboxContainer) return NULL;
	return m_pgboxContainer->Root();
}

void VwBox::Invalidate()
{
	Rect vwrect = GetInvalidateRect();
	Root()->InvalidateRect(&vwrect);
}

// Answer the coordinate transformation used to draw self, given the one used
// to draw the root.
void VwBox::CoordTransFromRoot(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, Rect * prcSrc, Rect *prcDst)
{
	Rect rcSrcCont;
	Rect rcDstCont;
	VwGroupBox * pboxCont = Container();
	if (pboxCont)
	{
		// Get the transform for the container.
		pboxCont->CoordTransFromRoot(pvg, rcSrc, rcDst, &rcSrcCont, &rcDstCont);
		// And hence your own.
		pboxCont->CoordTransForChild(pvg, this, rcSrcCont, rcDstCont, prcSrc, prcDst);
	}
	else
	{
		// This is the root! If arguments are the transformation for the root,
		// they must by definition be right for this
		*prcSrc = rcSrc;
		*prcDst = rcDst;
	}
}


void VwBox::Followers(BoxSet & boxset)
{
	for (VwBox * pbox=NextOrLazy(); pbox; pbox = pbox->NextOrLazy())
		boxset.Insert(pbox);
}

//delete embedded boxes and their notifiers
//default box has none, but VwGroupBox overrides.
//other overrides may be needed later
void VwBox::DeleteContents(VwRootBox * prootb, NotifierVec & vpanoteDel,
	BoxSet * pboxsetDeleted)
{
}

// Deletes this and removes any notifers for this box and deletes the notifiers
void VwBox::DeleteAndCleanupAndDeleteNotifiers()
{
	VwRootBox * prootb = this->Root();
	NotifierVec vpanoteDel;
	DeleteAndCleanup(vpanoteDel); // Deletes *this!!!!!!

	if (prootb)
		prootb->DeleteNotifierVec(vpanoteDel);
#if DEBUG
	else
		Assert(!vpanoteDel.Size()); // got notifiers to delete without a rootbox (BAD!!!)
#endif
}

// Deletes this and removes any notifers for this box
// Returns the list of notifiers to delete
void VwBox::DeleteAndCleanup(NotifierVec &vpanoteDel)
{
	VwRootBox * prootb = this->Root();
	if (prootb)
	{
		prootb->FixSelections(this, NULL); // Just in case a selection is in this box (FWR-1720)
		prootb->DeleteNotifiersFor(this, -1, vpanoteDel);
	}
	delete this;
}

//answer a set of all your containers (not including this)
void VwBox::Containers(BoxSet * pboxset)
{
	for (VwBox * pbox=Container(); pbox; pbox = pbox->Container())
		pboxset->Insert(pbox);
}

// Answer a rectangle, in the coordinate system of the root,
// that this box currently occupies, and which should be invalidated
// if it moves or its contents changes. Subclasses which draw outside their
// natural boxes, such as text with italics, should override.
// In any case, we expand the rectangle a couple of pixels, just to be safe.
// (For example: we could be replacing a whole paragraph, and half of an insertion
// point could be showing outside it.)
Rect VwBox::GetInvalidateRect()
{
	// Get the rectangle that represents this box, relative to the top left of
	// the whole root. We need to add the Left,Top of each containing box to do this.
	Rect rcRet(Left(), Top(), Right(), VisibleBottom());
	for (VwGroupBox * pgbox = Container(); pgbox; pgbox = pgbox->Container())
		rcRet.Offset(pgbox->Left(), pgbox->Top());

	rcRet.Inflate(2, 2); // margin: IP sometimes draws just outside the box.
	return rcRet;
}

// Answer a rectangle, in the coordinate system of the root,
// that this box currently occupies.
Rect VwBox::GetBoundsRect(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
	Rect rcSrc, rcDst;
	CoordTransFromRoot(pvg, rcSrcRoot, rcDstRoot, &rcSrc, &rcDst);
	Rect rcRet;
	rcRet.left = rcSrc.MapXTo(Left(), rcDst);
	rcRet.top = rcSrc.MapYTo(Top(), rcDst);
	rcRet.right = rcSrc.MapXTo(Left() + Width(), rcDst);
	rcRet.bottom = rcSrc.MapYTo(VisibleBottom(), rcDst);
	return rcRet;
}

/*----------------------------------------------------------------------------------------------
	Answer the box the user clicked in (or nearest to). Subclasses which contain nested
	boxes override. The default is to answer this.
	(xd,yd) is the point clicked in the coordinates used to draw the box.
	rcSrc and rcDst are the coordinate transformation used to draw this box, and
	the result rectangles are the coordinate transformation used to draw the chosen box.
----------------------------------------------------------------------------------------------*/
VwBox * VwBox::FindBoxClicked(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
	Rect * prcSrc, Rect * prcDst)
{
#if 99-99
	{
		StrAnsi sta;
		int xMin = Left();
		int x = xd + rcSrc.left;
		int xMax = Right();
		int yMin = Top();
		int y = yd + rcSrc.top;
		int yMax = Bottom();
		char * pszWhere = (xMin <= x && x <= xMax && yMin <= y && y <= yMax) ? "INSIDE " : "OUTSIDE";
		sta.Format("VwBox::FindBoxClicked(}:            %s  %3d %s %3d %s %3d  && %3d %s %3d %s %3d%n",
			pszWhere, xMin, (xMin <= x ? "<=" : "> "), x, (x <= xMax ? "<=" : "< "), xMax,
			yMin, (yMin <= y ? "<=" : "> "), y, (y <= yMax ? "<=" : "< "), yMax);
		::OutputDebugStringA(sta.Chars());
		VwPictureBox * pboxPic = dynamic_cast<VwPictureBox *>(this);
		if (pboxPic != NULL)
		{
			int dypTop = pboxPic->Top();
			int dxpLeft = pboxPic->Left() + pboxPic->AdjustedLeft();
			int ydTop = rcSrc.MapYTo(dypTop + pboxPic->GapTop(rcSrc.Height()), rcDst);
			int xdLeft = rcSrc.MapXTo(dxpLeft + pboxPic->GapLeft(rcSrc.Width()), rcDst);
			int ydBottom = rcSrc.MapYTo(dypTop + pboxPic->Height() - pboxPic->GapBottom(rcSrc.Height()), rcDst);
			int xdRight = rcSrc.MapXTo(dxpLeft + pboxPic->Width() - pboxPic->GapRight(rcSrc.Width()), rcDst);
			sta.Format("Vw(Picture)Box::FindBoxClicked(): dypTop=%d, dxpLeft=%d, ydTop=%d, xdLeft=%d, ydBottom=%d, xdRight=%d%n",
				dypTop, dxpLeft, ydTop, xdLeft, ydBottom, xdRight);
			::OutputDebugStringA(sta.Chars());
		}
	}
#endif
	*prcSrc = rcSrc;
	*prcDst = rcDst;
	return this;
}


/*----------------------------------------------------------------------------------------------
	Return true if the given point (as adjusted by rcSrc) is inside the boundaries of this box.
	(xd,yd) is the point clicked in the coordinates used to draw the box.
	rcSrc and rcDst are the coordinate transformation used to draw this box.
	Note that rcSrc is already adjusted for drawing embedded boxes.

	REVIEW: JohnT should probably verify that this is doing the right check!
----------------------------------------------------------------------------------------------*/
bool VwBox::IsPointInside(int xd, int yd, Rect rcSrc, Rect rcDst)
{
	// Get the point relative to this box
	int xs = rcDst.MapXTo(xd, rcSrc);
	if (xs < Left() || xs > Right())
		return false;

	int ys = rcDst.MapYTo(yd, rcSrc);
	if (ys < Top() || ys > Bottom())
		return false;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Make a selection and install it in the given root box (which is your own).
----------------------------------------------------------------------------------------------*/
MakeSelResult VwBox::MakeSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
		Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, bool forceReadOnly)
{
	VwSelectionPtr qvwsel;
	GetSelection(pvg, prootb, xd, yd, rcSrcRoot, rcDstRoot, rcSrc, rcDst, &qvwsel);
	prootb->SetSelection(qvwsel);
	prootb->ShowSelection();
	return kmsrMadeSel;
}

/*----------------------------------------------------------------------------------------------
	Answer the square of the distance in source coords between this box and the
	given point, which is in the same coordinate system as this box's top, left.
----------------------------------------------------------------------------------------------*/
int VwBox::DsqToPoint(int xs, int ys, Rect rcSrc)
{
	//compute minimum distance from point to
	//border of this
	int dysBorderTop = Top() + MulDiv(Style()->MarginTop(), rcSrc.Height(), kdzmpInch);
	int dxsBorderLeft = Left() + MulDiv(Style()->MarginLeading(), rcSrc.Width(), kdzmpInch);
	int dysBorderBottom = Top() + Height() - MulDiv(Style()->MarginBottom(), rcSrc.Height(), kdzmpInch);
	int dxsBorderRight = Left() + Width() - MulDiv(Style()->MarginTrailing(), rcSrc.Width(), kdzmpInch);
	int dsq;
	if (ys < dysBorderTop)
	{
		if (xs < dxsBorderLeft)
		{//top left corner
			dsq = (ys-dysBorderTop) * (ys-dysBorderTop) +
				(xs-dxsBorderLeft) * (xs-dxsBorderLeft);
		}
		else
		{//above top, right of left
			if (xs <= dxsBorderRight)
			{//center above top
				dsq = (ys-dysBorderTop) * (ys-dysBorderTop);
			}
			else
			{//top right corner
				dsq = (ys-dysBorderTop) * (ys-dysBorderTop) +
					(xs-dxsBorderRight) * (xs-dxsBorderRight);
			}
		}
	}
	else
	{//below top of this
		if (ys > dysBorderBottom)
		{//below bottom
			if (xs < dxsBorderLeft)
			{//bottom left corner
				dsq = (ys-dysBorderBottom) * (ys-dysBorderBottom) +
					(xs-dxsBorderLeft) * (xs-dxsBorderLeft);
			}
			else
			{//below bottom, right of left
				if (xs <= dxsBorderRight)
				{//center below bottom
					dsq = (ys-dysBorderBottom) * (ys-dysBorderBottom);
				}
				else
				{//bottom right corner
					dsq = (ys-dysBorderBottom) * (ys-dysBorderBottom) +
						(xs-dxsBorderRight) * (xs-dxsBorderRight);
				}
			}
		}
		else
		{//within ys range of this
			if (xs < dxsBorderLeft)
			{//center left area
				dsq = (xs-dxsBorderLeft) * (xs-dxsBorderLeft);
			}
			else
			{//center vertically, right of left
				if (xs <= dxsBorderRight)
				{//in this box itself!!
					dsq = 0;
				}
				else
				{//center right area
					dsq = (xs-dxsBorderRight) * (xs-dxsBorderRight);
				}
			}
		}
	}
	return dsq;
}

/*----------------------------------------------------------------------------------------------
	Hilite everything in the box. The default inverts the whole thing.
----------------------------------------------------------------------------------------------*/
void VwBox::HiliteAll(IVwGraphics * pvg, bool fOn, Rect rcSrcRoot, Rect rcDstRoot)
{
	Rect rcSrc, rcDst;
	CoordTransFromRoot(pvg, rcSrcRoot, rcDstRoot, &rcSrc, &rcDst);
	// We will invert the border and pad but not the margin, as that may overlap another box's
	// margin, or even possibly contents.
	int xdLeft = rcSrc.MapXTo(Left() +
		MulDiv(m_qzvps->MarginLeading(), rcSrc.Width(), kdzmpInch), rcDstRoot);
	int xdRight = rcSrc.MapXTo(Left() + Width() -
		MulDiv(m_qzvps->MarginTrailing(), rcSrc.Width(), kdzmpInch), rcDstRoot);
	int ydTop = rcSrc.MapYTo(Top() +
		MulDiv(m_qzvps->MarginTop(), rcSrc.Height(), kdzmpInch), rcDstRoot);
	int ydBottom = rcSrc.MapYTo(Top() + Height() +
		MulDiv(m_qzvps->MarginTop(), rcSrc.Height(), kdzmpInch), rcDstRoot);
	pvg->InvertRect(xdLeft, ydTop, xdRight, ydBottom);
}

/*----------------------------------------------------------------------------------------------
	Get a selection. This is overridden by string boxes (and perhaps one day by other boxes).
	The best the average box can do is to look for a neighboring string box.
----------------------------------------------------------------------------------------------*/
void VwBox::GetSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
	Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, VwSelection ** ppvwsel,
	SearchDirection sdir)
{
	VwBox * pbox;
	int dyMaxSeparation = rcSrcRoot.Height() * 2; // two inches in layout resolution.
	// Try in subsequent boxes, typically within the same paragraph, but sometimes a leaf occurs
	// directly in a pile. In such cases don't look in boxes more than 2 inches away (this is
	// a somewhat arbitrary limit, but in pathological cases it can prevent expanding an
	// outrageous amount of lazy stuff.
	if (sdir & ksdirUp)
	{
		for (pbox = NextRealBox();
			pbox && pbox->Top() - this->Bottom() < dyMaxSeparation;
			pbox = pbox->NextRealBox())
		{
			// It has the same container so should accept the same rcSrc, rcDst.
			pbox->GetSelection(pvg, prootb, xd, yd, rcSrcRoot, rcDstRoot, rcSrc, rcDst, ppvwsel,
				ksdirUp);
			if (*ppvwsel)
				return;
		}
	}

	if (sdir & ksdirDown)
	{
		for (pbox = Container()->RealBoxBefore(this);
			pbox && pbox->Bottom() - this->Top() < dyMaxSeparation;
			pbox = Container()->RealBoxBefore(pbox))
		{
			// It has the same container so should accept the same rcSrc, rcDst.
			pbox->GetSelection(pvg, prootb, xd, yd, rcSrcRoot, rcDstRoot, rcSrc, rcDst,
				ppvwsel, ksdirDown);
			if (*ppvwsel)
				return;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Consider all the boxes of the root as being ordered such that a box
	and all its descendents are before the next box at the same level, and also, a box
	is before its own descendents. Find the next box in this sequence, starting from this.

	Result may be a lazy box unless fReal is true.
	Result may be a child of the recipient if fIncludeChildren is true.
----------------------------------------------------------------------------------------------*/
VwBox * VwBox::NextBoxForSelection(VwBox ** ppStartSearch, bool fReal, bool fIncludeChildren)
{
	VwBox * pboxNext = fReal ? NextRealBox() : NextOrLazy();
	if (pboxNext)
		return pboxNext;

	if (Container())
		return Container()->NextBoxForSelection(ppStartSearch, fReal, false);
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Consider all the boxes of the root as being ordered such that a box
	and all its descendents are before the next box at the same level, and also, a box
	is before its own descendents. Find the next box in this sequence, starting from this.

	Result may be a lazy box unless fReal is true.
	Result may be a child of the recipient if fIncludeChildren is true.
----------------------------------------------------------------------------------------------*/
VwBox * VwGroupBox::NextBoxForSelection(VwBox ** ppStartSearch, bool fReal, bool fIncludeChildren)
{
	if (fIncludeChildren)
	{
		// try to go down
		VwBox * pboxNext = fReal ? FirstRealBox() : FirstBox();
		if (pboxNext)
			return pboxNext;
	}

	return SuperClass::NextBoxForSelection(ppStartSearch, fReal, fIncludeChildren);
}

/*----------------------------------------------------------------------------------------------
	Consider all the boxes of the root as being ordered such that a box
	and all its descendents are before the next box at the same level, and also, a box
	is before its own descendents. Find the next box in this sequence, starting from this.

	Result may be a lazy box unless fReal is true.
	Result may be a child of the recipient if fIncludeChildren is true.
----------------------------------------------------------------------------------------------*/
VwBox * VwBox::NextInRootSeq(bool fReal, IVwSearchKiller * pxserkl, bool fIncludeChildren)
{
	VwBox * pboxNext = NULL;
	VwBox * pbox = this;
	// try to go down
	if (fIncludeChildren)
	{
		VwGroupBox * pgbox = NULL;
		pgbox = dynamic_cast<VwGroupBox *>(pbox);
		if (pgbox)
			pboxNext = fReal ? pgbox->FirstRealBox() : pgbox->FirstBox();
	}
	while (pbox && !pboxNext)
	{
		// we still have a starting point (pbox moves up the chain of containers until
		// it becomes null when the root box has no container). And we haven't found
		// anything yet...the original box wasn't a group or had no children, and
		// we haven't found this or any of its containers to have a next box.

		// First see if the user has asked to abort.
		if (pxserkl)
		{
			ComBool fAbort;
			CheckHr(pxserkl->FlushMessages());
			CheckHr(pxserkl->get_AbortRequest(&fAbort));
			if (fAbort == ComBool(true))
				return NULL;
		}
		// If pbox has a next box it is the answer.
		pboxNext = fReal ? pbox->NextRealBox() : pbox->NextOrLazy();
		// In case Next() didn't find one, move pbox up a level, so the next iteration will
		// see if that has a next box. The container itself is earlier in overall sequence.
		pbox = pbox->Container();
	}
	return pboxNext;
}

/*----------------------------------------------------------------------------------------------
	Return the box logically after this one in the root sequence, not including any of its
	contained boxes. That is, we want the next box after this one, if any; if not, the next
	box after its container, if any, and so on.

	Result may be a lazy box!
----------------------------------------------------------------------------------------------*/
VwBox * VwBox::NextBoxAfter()
{
	VwBox * pbox = this;
	for (; pbox; pbox = pbox->Container())
	{
		if (pbox->NextOrLazy())
			return pbox->NextOrLazy();
	}
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Return the total distance from the top of this box to the top of the whole view.
	This is the sum of its own top and the tops of all containing boxes.
----------------------------------------------------------------------------------------------*/
int VwBox::TopToTopOfDocument()
{
	int result = 0;
	for (VwBox * pbox = this; pbox; pbox = pbox->Container())
		result += pbox->Top();
	return result;
}

/*----------------------------------------------------------------------------------------------
	Return the total distance from the left of this box to the left of the whole view.
	This is the sum of its own top and the tops of all containing boxes.
----------------------------------------------------------------------------------------------*/
int VwBox::LeftToLeftOfDocument()
{
	int result = 0;
	for (VwBox * pbox = this; pbox; pbox = pbox->Container())
		result += pbox->Left();
	return result;
}

/*----------------------------------------------------------------------------------------------
	Return the total top margin of this box...the space we can hide at the top of the page.
	By default this is just its own margin, but various kinds of group boxes consider also
	the margins of their first box (or first row of boxes).
----------------------------------------------------------------------------------------------*/
int VwBox::TotalTopMargin(int dpiY)
{
	return m_qzvps->MswMarginTop() * dpiY / kdzmpInch;
}

/*----------------------------------------------------------------------------------------------
	Return the total bottom margin of this box...the space we can hide at the bottom of the page.
	By default this is just its own margin, but various kinds of group boxes consider also
	the margins of their last box (or last row of boxes).
----------------------------------------------------------------------------------------------*/
int VwBox::TotalBottomMargin(int dpiY)
{
	return MarginBottom() * dpiY / kdzmpInch;
}


/*----------------------------------------------------------------------------------------------
	Consider all the boxes of the root as being ordered such that a box
	and all its descendents are before the previous box at the same level, and also, a box
	is before its own descendents. Find the next box in this sequence, starting from this.
	Note that this is NOT the reverse order to RootSeq; we still process a box before its
	descendents.

	Result may be a lazy box unless fReal is true.
----------------------------------------------------------------------------------------------*/
VwBox * VwBox::NextInReverseRootSeq(bool fReal, IVwSearchKiller * pxserkl)
{
	VwBox * pboxNext = NULL;
	VwBox * pbox = this;
	// try to go down
	VwGroupBox * pgbox = dynamic_cast<VwGroupBox *>(pbox);
	if (pgbox)
		pboxNext = fReal ? pgbox->LastRealBox() : pgbox->LastBox();
	while (pbox && !pboxNext)
	{
		if (pxserkl)
		{
			ComBool fAbort;
			CheckHr(pxserkl->FlushMessages());
			CheckHr(pxserkl->get_AbortRequest(&fAbort));
			if (fAbort == ComBool(true))
				return NULL;
		}
		// If we failed so far try to go horizontal from pbox
		if ((!pboxNext) && pbox->Container())
		{
			VwGroupBox * pgboxCont = pbox->Container();
			pboxNext = fReal ? pgboxCont->RealBoxBefore(pbox) : pgboxCont->BoxBefore(pbox);
		}
		// In case Next() didn't find one, move pbox up a level, and
		// see if that has a next box. The container itself is earlier in overall sequence.
		pbox = pbox->Container();
	}
	return pboxNext;
}

/*----------------------------------------------------------------------------------------------
	Print yourself on pvpi.m_pvg, at the position indicated by the source and dest rects.
	Print starting from ysStart, which is in source coords relative to your container
	(the same coords as your own Top() value), to ysEnd (same coords).
	The ysEnd value will be one obtained by calling FindNiceBreak on yourself,
	unless no break could be found, in which case, print assuming clipping at ysEnd.
----------------------------------------------------------------------------------------------*/
void VwBox::PrintPage(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst, int ysStart, int ysEnd)
{
	// By default, all we can do is draw the whole box.
	Draw(pvpi->m_pvg, rcSrc, rcDst);
}

/*----------------------------------------------------------------------------------------------
	Find the best (last) place to put a "nice" page break in yourself after ysStart.
	The space available for printing is obtained by using the src and dst rects
	to convert ysStart to destination coords, then taking the space from there to
	pvpi.rcDoc.bottom. A "nice" break is one that does not split paragraph lines or
	boxes that don't know how to divide themselves, nor does it violate "keep together"
	or "keep with next".
	rcSrc and rcDst provide a coordinate transformation from source to actual printing coords.
	ysStart and *pysEnd are in the coordinate system of your own Top() value, that is,
	relative to the container's top.

	Return true if any nice break is possible.
	A "nice" break must be within this box (suitable for breaking this box even if
	it has KeepWithNext); the parent box will do something suitable if a break
	at the end of the box is feasible.

	If fDisregardKeeps is true, ignore KeepWithNext and KeepTogether.
----------------------------------------------------------------------------------------------*/
bool VwBox::FindNiceBreak(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst,
	int ysStart, int * pysEnd, bool fDisregardKeeps)
{
	return false; // by default there is no way to break up a box.
}

/*----------------------------------------------------------------------------------------------
	Return the height required to print this box. This excludes its bottom margin.
	ENHANCE JohnT: For DivBox, should we also exclude the bottom margin of the bottom box,
	provided the DivBox has no border or bottom pad? If so make it virtual and override.
	This would have the effect of very occasionally fitting the last paragraph of a doc
	on the last page rather than starting another page.
----------------------------------------------------------------------------------------------*/
int VwBox::PrintHeight(int dysInch)
{
	return Height() - MulDiv(MarginBottom(), dysInch, kdzmpInch);
}

/*----------------------------------------------------------------------------------------------
	Return the height required to display the box in a data entry field. This excludes
	bottom margin, bottom padding unless there is a bottom margin, and recursively any
	similar items on embedded boxes.
----------------------------------------------------------------------------------------------*/
int VwBox::FieldHeight()
{
	int dyInch = Root()->DpiSrc().y;

	if (BorderBottom())
		return Height() + ExtraHeightIfNotFollowedByPara() - MulDiv(MarginBottom(), dyInch, kdzmpInch);
	else
		return Height() + ExtraHeightIfNotFollowedByPara() - MulDiv(MarginBottom(), dyInch, kdzmpInch) -
			MulDiv(m_qzvps->PadBottom(), dyInch, kdzmpInch);
}


//:>********************************************************************************************
//:>	VwGroupBox methods
//:>********************************************************************************************
class DeleteBinder
{
public:
	DeleteBinder()
	{
	}
	void operator() (VwBox * pbox)
	{
		delete pbox;
	}
};

VwGroupBox::~VwGroupBox()
{
	// delete each child box
	// (Note: in general DeleteContents should be called before deleting a box,
	// so this loop will do nothing.
	VwBox * pboxNext;
	for (VwBox * pbox = m_pboxFirst; pbox; pbox = pboxNext)
	{
		pboxNext = pbox->NextOrLazy(); //save before deleting!
		delete pbox;
	}
	m_pboxFirst = m_pboxLast = NULL;

#if 0 // produces incomprehensible compiler error
	DeleteBinder db();
	this->ForEachChild(db);
#endif
}

// Answer the width available for laying out the specified child.
// This is used for relayout type operations, where the container has already been laid out.
// (This is important for table cells, which just use their current width since it never
// depends on the contents.)
// Enhance: stricly VwParagraphBox should override and adjust the answer for the first box
// by any first line indent.
int VwGroupBox::AvailWidthForChild(int dpiX, VwBox * pboxChild)
{
	return Container()->AvailWidthForChild(dpiX, this) - SurroundWidth(dpiX);
}

/*----------------------------------------------------------------------------------------------
	Hilite everything in the box. The default inverts the whole thing.
----------------------------------------------------------------------------------------------*/
void VwGroupBox::HiliteAllChildren(IVwGraphics * pvg, bool fOn, Rect rcSrcRoot, Rect rcDstRoot)
{
	// It's OK to do this to a lazy box because inverting something that's not on the screen
	// will have no effect.
	for (VwBox * pbox = m_pboxFirst; pbox && pbox->Top() != knTruncated;
		pbox = pbox->NextOrLazy())
	{
		pbox->HiliteAll(pvg, fOn, rcSrcRoot, rcDstRoot);
	}
}


void VwGroupBox::Add(VwBox * pbox)
{
	if (m_pboxLast)
		m_pboxLast->SetNext(pbox);
	m_pboxLast = pbox;
	if (!m_pboxFirst)
		m_pboxFirst = pbox;
	pbox->Container(this);
}

// Something has changed inside this group box. pboxPrev is either null or the box before
// the change, and pboxNext is the box immediately after the change, if any.
// By default pboxPrev, pboxNext, and all following boxes are inserted into the fixmap.
// DivBox overrides to stop with the pboxNext itself.
// ParagraphBox doesn't need to insert any of them.
void VwGroupBox::AddPrevAndFollowingToFixMap(FixupMap & fixmap, VwBox * pboxNext, VwBox * pboxPrev)
{
	for (VwBox * pbox = pboxNext; pbox; pbox = pbox->NextOrLazy())
	{
		Rect vwrect = pbox->GetInvalidateRect();
		fixmap.Insert(pbox, vwrect);
		// If the container is a Div box, we can count on it to deal with changes of position
		// of following boxes, and they don't really need to be re-laid-out.
		// OPTIMIZE JohnT: there may be other containers which are, or could be made, smart
		// enough to benefit from this optimization, but Div is by far the most important.
		if (dynamic_cast<VwDivBox *>(pbox->Container()))
			break;
	}

	// If there is a previous box, re-check its layout too. The reason is that
	// the two boxes may be paragaphs, and may have top or bottom borders, and
	// if so, there are interactions between them that may have changed the
	// layout. (For example, if we just inserted a box with the same style
	// as the preceding one, the preceding one loses its border.)
	if (pboxPrev)
	{
		Rect vwrect = pboxPrev->GetInvalidateRect();
		fixmap.Insert(pboxPrev, vwrect);
	}
}

// If the container is a Div box, we can count on it to deal with changes of position
// of following boxes, and they don't really need to be re-laid-out.
void VwDivBox::AddPrevAndFollowingToFixMap(FixupMap & fixmap, VwBox * pboxNext, VwBox * pboxPrev)
{
	if (pboxNext)
	{
		Rect vwrect = pboxNext->GetInvalidateRect();
		fixmap.Insert(pboxNext, vwrect);
	}

	// If there is a previous box, re-check its layout too. The reason is that
	// the two boxes may be paragaphs, and may have top or bottom borders, and
	// if so, there are interactions between them that may have changed the
	// layout. (For example, if we just inserted a box with the same style
	// as the preceding one, the preceding one loses its bottom border, if any.)
	if (pboxPrev)
	{
		Rect vwrect = pboxPrev->GetInvalidateRect();
		fixmap.Insert(pboxPrev, vwrect);
	}
}

class GroupDrawBinder
{
	IVwGraphics * m_pvg;
	Rect m_rcSrc;
	Rect m_rcDst;
public:
	GroupDrawBinder(IVwGraphics* pvg, Rect rcSrc, Rect rcDst)
		:m_pvg(pvg), m_rcSrc(rcSrc), m_rcDst(rcDst)
	{
	}
	void operator() (VwBox * pbox)
	{
		if (pbox->Top() != knTruncated)
			pbox->Draw(m_pvg, m_rcSrc, m_rcDst);
	}
};

// Long form for full argument set. Passes on the extra arguments.
class LongGroupDrawBinder
{
	IVwGraphics * m_pvg;
	Rect m_rcSrc;
	Rect m_rcDst;
	int m_ysTop;
	int m_dysHeight;
	bool m_fDisplayPartialLines;
public:
	LongGroupDrawBinder(IVwGraphics* pvg, Rect rcSrc, Rect rcDst, int ysTop,
		int dysHeight, bool fDisplayPartialLines)
		:m_pvg(pvg), m_rcSrc(rcSrc), m_rcDst(rcDst), m_ysTop(ysTop), m_dysHeight(dysHeight),
		m_fDisplayPartialLines(fDisplayPartialLines)
	{
	}
	void operator() (VwBox * pbox)
	{
		if (pbox->Top() != knTruncated)
			pbox->Draw(m_pvg, m_rcSrc, m_rcDst, m_ysTop, m_dysHeight, m_fDisplayPartialLines);
	}
};
//	The 'foreground' of a group box is the embedded boxes (including their own
//	borders, etc.--maybe this method should be named drawContents?)
void VwGroupBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTop,
		int dysHeight, bool fDisplayPartialLines)
{
#ifdef _DEBUG
#ifdef _DEBUG_SHOW_BOX
	DebugDrawBorder(pvg, rcSrc, rcDst);
#endif
#endif

	rcSrc.Offset(-m_xsLeft, -m_ysTop);
	LongGroupDrawBinder gdb(pvg, rcSrc, rcDst, ysTop, dysHeight, fDisplayPartialLines);
	this->ForEachChild(gdb);
}

//	The 'foreground' of a group box is the embedded boxes (including their own
//	borders, etc.--maybe this method should be named drawContents?)
void VwGroupBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
#ifdef _DEBUG
#ifdef _DEBUG_SHOW_BOX
	DebugDrawBorder(pvg, rcSrc, rcDst);
#endif
#endif

	rcSrc.Offset(-m_xsLeft, -m_ysTop);
	GroupDrawBinder gdb(pvg, rcSrc, rcDst);
	this->ForEachChild(gdb);
}

//	Remove all your boxes and return a pointer to the chain.
//	Caller becomes responsible for storage for boxes (typically will later restore
//	using RestoreBoxes).
VwBox * VwGroupBox::RemoveAllBoxes()
{
	VwBox * pbox = m_pboxFirst;
	m_pboxFirst = m_pboxLast = NULL;
	return pbox;
}

//	Restore boxes temporarily removed so new ones could be generated.
//	This method is private to the regenerate process.
//	ENHANCE: make it a friend of VwEnv.
void VwGroupBox::RestoreBoxes(VwBox * pboxFirst, VwBox * pboxLast)
{
	m_pboxFirst = pboxFirst;
	m_pboxLast = pboxLast;
}

//	Answer true if the recipient contains the first argument.
//	If ppboxSub is not null, set *ppboxSub to point to the sub-box of self
//	that is or contains box. (Make it null if recipient does not contain arg)
bool VwGroupBox::Contains(VwBox * pbox, VwBox ** ppboxSub)
{
	VwBox * pboxSub;
	for (pboxSub = pbox; pboxSub && !(pboxSub->Container() == this); pboxSub = pboxSub->Container());
	if (ppboxSub)
		*ppboxSub = pboxSub;
	return pboxSub != NULL;
}

void VwGroupBox::AddAllChildrenToSet(BoxSet & boxset)
{
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		boxset.Insert(pbox);
		pbox->AddAllChildrenToSet(boxset);
	}
}

// Find a box that contains both the arguments.
VwGroupBox * VwGroupBox::CommonContainer(VwBox * pboxFirst, VwBox * pboxLast)
{
	VwGroupBox * pgboxCommonContainer = pboxFirst->Container();

	while (pgboxCommonContainer && !pgboxCommonContainer->Contains(pboxLast))
		pgboxCommonContainer = pgboxCommonContainer->Container();
	return pgboxCommonContainer;
}


//	Return a vector of the notifiers that cover the specified box, one of your
//	contents. It is unsorted.
void VwGroupBox::GetNotifiers(VwBox * pboxChild, NotifierVec& vpanote)
{
	Assert(pboxChild->Container() == this);
	// Make a set of childBox and all the subsequent boxes in the group.
	// This type should really just be a set (we ignore the value), but we don't
	// have such a class yet.
	BoxSet boxsetFollowers;
	pboxChild->Followers(boxsetFollowers);
	boxsetFollowers.Insert(pboxChild);

	VwRootBox * prootb = this->Root();
	NotifierMap * pmmboxqnote;

	prootb->GetNotifierMap(&pmmboxqnote);

	//	Loop over all the boxes before (and including) childBox.
	for (VwBox * pbox = FirstBox(); pbox != pboxChild->NextOrLazy(); pbox = pbox->NextOrLazy())
	{
		if (!pbox)
		{
			// should normally reach the box after pboxChild, this *this is its parent;
			// Very rarely we call this in the course of laying out a paragraph that is a child
			// of another paragraph that is still being laid out. In this case it's OK
			// not to find the child because we haven't set the child boxes of the paragraph yet.
			// Otherwise, something is wrong!
			Assert(IsParagraphBox() && !FirstBox());
			break; // but if we don't, may aid robustness to just skip looking here.
		}
		//	A notifier is interesting if its first covering box is before (or equal) to
		//	childbox, and its lastBox is after (or equal).

		//	First get the range of notifiers that have the current box
		//	as their first covering box.
		NotifierMap::iterator itboxnote;
		NotifierMap::iterator itboxnoteLim;
		pmmboxqnote->Retrieve(pbox, &itboxnote, &itboxnoteLim);
		// loop over the candidate notifiers.
		for (; itboxnote != itboxnoteLim; ++itboxnote)
		{
			VwAbstractNotifier * pnoteCandidate = itboxnote.GetValue();
			// A notifier which begins AT the interesting box is certainly wanted.
			// One which begins BEFORE it is wanted only if it ends at or after the interesting box.
			VwBox * pboxTemp = pnoteCandidate->LastCoveringBox();
			if (pbox == pboxChild
				|| boxsetFollowers.IsMember(pboxTemp))
			{
				vpanote.Push(pnoteCandidate);
			}
		}
	}
	//	Recursively find notifiers covering this and higher-level boxes.
	if (this->Container())
		this->Container()->GetNotifiers(this, vpanote);
}

// Given a notifier which covers (some part of) pboxChild, answer wheter
// it covers all of it.
//
// Enhance JohnT: this method works well enough for the one place it is used,
// where we always go on to check that the box is the whole contents of the
// notifier. I'm not sure it will do what it is advertised to for all possible
// cases. For example, a notifier which contains some complete boxes, then several
// string properties within a (final) paragraph, could occur and the current
// implementation would not detect that it covers all of any boxes except
// the first.
bool NotifierIncludesAllOfParagraph(VwBox * pboxChild, VwNotifier * pnote)
{
	if (pnote->LastStringIndex() < 0)
		return true; // Notifier that deals with whole boxes only.
	// notifier that deals with strings inside para covers only one box, the paragraph.
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pnote->FirstBox());
	// only check if this is the right box, pvpbox may be null if box is not a paragraph
	if (pvpbox != pboxChild)
		return false;
	// If it doesn't extend to the last string it's no good.
	if (pnote->LastStringIndex() < pvpbox->Source()->CStrings())
		return false;
	// If it starts after the first string it's no good. -1 is OK, indicates that the
	// whole paragraph is part of this object. 0 indicates that the object starts
	// at the first string of the paragraph, which is also OK.
	int itssFirst = pnote->StringIndexes()[0];
	return (itssFirst == 0 || itssFirst == -1);
}

// Given a notifier which covers pboxChild in some sense, answer whether it covers
// anything else
bool NotifierCoversBoxExactly(VwBox * pboxChild, VwNotifier * pnote)
{
	if (pnote->LastStringIndex() >= 0)
		return true; // sub-paragraph notifiers only ever deal with one paragraph.
	return pnote->FirstBox() == pboxChild && pnote->LastBox() == pboxChild;
}

// Get the lowest level real notifier that covers the target box exactly
// (that is, it must include all the contents of the paragraph and nothing else).
VwNotifier * VwGroupBox::GetLowestNotifier(VwBox * pboxChild)
{
	NotifierVec vpanote;
	GetNotifiers(pboxChild, vpanote);
	VwNotifier * pnoteBest = NULL;
	for (int i = 0; i < vpanote.Size(); i++)
	{
		VwNotifier * pnote = dynamic_cast<VwNotifier *>(vpanote[i].Ptr());
		if (!pnote)
			continue; // not a VwNotifier
		if (pboxChild->IsParagraphBox())
		{
			if (!NotifierIncludesAllOfParagraph(pboxChild, pnote))
			{
				// pnote is no good, it doesn't cover the whole paragraph
				continue;
			}
			if (!NotifierCoversBoxExactly(pboxChild, pnote))
			{
				// pnote is no good, it doesn't cover the whole paragraph
				continue;
			}
		}
		else
		{
			// for non-paragraphs, we just want to check that it is the first
			// box of the notifier, and is or contains the last box.
			if (pboxChild != pnote->FirstBox())
				continue; // notifier doesn't start with this
			if (pboxChild != pnote->LastBox())
			{
				VwGroupBox * pgbox = dynamic_cast<VwGroupBox *>(pboxChild);
				if (pgbox == NULL || !pgbox->Contains(pnote->LastBox()))
					continue;
			}
		}
		// If we haven't yet found an acceptable notifier, or the new one
		// has a smaller level, it is the best yet.
		if (pnoteBest == NULL || pnoteBest->Level() > pnote->Level())
		{
			pnoteBest = pnote;
		}
	}
	return pnoteBest;
}

/*----------------------------------------------------------------------------------------------
	Relink your boxes to replace some with others. Caller is responsible to clean up notifiers
	and delete the boxes unlinked. This fixes the container of the added boxes.
	Arguments:
		pboxPrev: box before the first one to replace, or NULL to replace very first box
		pboxLim: box after the last one to replace, or NULL to replace to the very end
		pboxFirst: start of chain of boxes to replace, or NULL for simple deletion
		pboxLast: last box in chain to replace, or NULL for simple deletion
	Return the list of deleted boxes, if any (otherwise null)
----------------------------------------------------------------------------------------------*/
VwBox * VwGroupBox::RelinkBoxes(VwBox * pboxPrev, VwBox * pboxLim, VwBox * pboxFirst,
	VwBox * pboxLast)
{
	Assert(!pboxPrev || pboxPrev->Container() == this);
	Assert(!pboxLim || pboxLim->Container() == this);
	Assert(pboxLast || !pboxFirst);

	VwBox * pboxRet = pboxPrev ? pboxPrev->NextOrLazy() : FirstBox();
	if (pboxRet == pboxLim)
		pboxRet = NULL;
	else if (pboxLim)
	{
		VwBox * pboxLastDel;
		for (pboxLastDel = pboxRet;
			pboxLastDel->NextOrLazy() != pboxLim;
			pboxLastDel = pboxLastDel->NextOrLazy()
		)
			;
		pboxLastDel->SetNext(NULL);
	}

	if (!pboxLim)
	{
		// We will have to update m_pboxLast
		if (pboxLast)
			m_pboxLast = pboxLast; // it is the new last
		else
			m_pboxLast = pboxPrev; // no new last box, it will be the one before first deletion
	}
	// Figure the box that will replace the first deleted box
	VwBox * pboxLink = pboxFirst;
	if (!pboxFirst)
		pboxLink = pboxLim;
	if (pboxPrev)
		pboxPrev->SetNext(pboxLink);
	else
		m_pboxFirst = pboxLink;
	if (pboxLast)
		pboxLast->SetNext(pboxLim);
	for (; pboxFirst != pboxLast; pboxFirst = pboxFirst->NextOrLazy())
		pboxFirst->Container(this);
	if (pboxLast)
		pboxLast->Container(this);
	return pboxRet;
}

//	Delete embedded boxes and their contents (recursively).
void VwGroupBox::DeleteContents(VwRootBox * prootb, NotifierVec & vpanoteDel,
	BoxSet * pboxsetDeleted)
{
	VwBox * pboxNext;
	for (VwBox * pbox = m_pboxFirst; pbox; pbox = pboxNext)
	{
		pbox->DeleteContents(prootb, vpanoteDel, pboxsetDeleted);
		if (prootb)
		{
			prootb->DeleteNotifiersFor(pbox, -1, vpanoteDel);
			prootb->FixSelections(pbox);
		}
		pboxNext = pbox->NextOrLazy(); //save before deleting!
		if (pboxsetDeleted)
			pboxsetDeleted->Insert(pbox);
		delete pbox;
	}
	m_pboxFirst = m_pboxLast = NULL;
}

// Overrides to delete the contents of this group
void VwGroupBox::DeleteAndCleanup(NotifierVec &vpanoteDel)
{
	VwRootBox * prootb = this->Root();
	Assert(prootb);
	DeleteContents(prootb, vpanoteDel);

	SuperClass::DeleteAndCleanup(vpanoteDel);
}

// Return the box immediately before the specified one
// (or null, if it is your first box or this container is empty).
// Raise assert error if not found.
// May return lazy box!
VwBox * VwGroupBox::BoxBefore(VwBox * pboxSub)
{
	// Some temporary boxes aren't fully constructed and may have
	// no first box.
	if (FirstBox() == pboxSub || FirstBox() == NULL)
		return NULL;
	VwBox * pbox;
	for (pbox=FirstBox(); pbox && pbox->NextOrLazy()!=pboxSub; pbox = pbox->NextOrLazy())
		;
	Assert(pbox);
	return pbox;
}

// Gets the paragraph box if this is a paragraph, or if the only thing
// it contains is a paragraph box (e.g. a paragraph box inside of a single
// table cell inside of a single table row inside of a table).
VwParagraphBox * VwGroupBox::GetOnlyContainedPara()
{
	if (FirstBox() && FirstBox() == LastBox())
		return FirstBox()->GetOnlyContainedPara();

	return NULL;
}

// Gets the paragraph box if this is a paragraph, or if one of our child boxes
// contains a paragraph box.
VwParagraphBox * VwGroupBox::GetAnyContainedPara()
{
	if (FirstBox())
		return FirstBox()->GetAnyContainedPara();

	return NULL;
}

// Overrides the GetSelection() method to pass the GetSelection on to the child box
// that should handle it.
void VwGroupBox::GetSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
	Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, VwSelection ** ppvwsel,
	SearchDirection sdir)
{
	Rect rcSrcBox;
	Rect rcDstBox;
	VwBox * pboxClick = FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, &rcSrcBox, &rcDstBox);
	if (!pboxClick)
		return;
	pboxClick->GetSelection(pvg, prootb, xd, yd, rcSrc, rcDst, rcSrcBox, rcDstBox,
		ppvwsel, sdir);
}

VwBox * VwGroupBox::FindBoxClicked(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
	Rect * prcSrc, Rect * prcDst)
{
#if 99-99
	{
		StrAnsi sta;
		int xMin = Left();
		int x = xd + rcSrc.left;
		int xMax = Right();
		int yMin = Top();
		int y = yd + rcSrc.top;
		int yMax = Bottom();
		char * pszWhere = (xMin <= x && x <= xMax && yMin <= y && y <= yMax) ? "INSIDE " : "OUTSIDE";
		sta.Format("VwGroupBox::FindBoxClicked(}:       %s  %3d %s %3d %s %3d  && %3d %s %3d %s %3d%n",
			pszWhere, xMin, (xMin <= x ? "<=" : "> "), x, (x <= xMax ? "<=" : "< "), xMax,
			yMin, (yMin <= y ? "<=" : "> "), y, (y <= yMax ? "<=" : "< "), yMax);
		::OutputDebugStringA(sta.Chars());
	}
#endif
	if (!m_pboxFirst)
		return NULL;
	VwBox * pboxClosest = NULL;
	int dsqMin = INT_MAX; // square of distance from best box so far to target point

	// Adjust rcSrc as usual for drawing embedded boxes.
	rcSrc.Offset(-Left(), -Top());
	// Get the point relative to this box, that is, in the same coords as the (top, left)
	// of each embedded box.
	int xs = rcDst.MapXTo(xd, rcSrc);
	int ys = rcDst.MapYTo(yd, rcSrc);

	for (VwBox * pbox=m_pboxFirst; pbox && pbox->Top() != knTruncated; pbox = pbox->NextOrLazy())
	{
		int dsq = pbox->DsqToPoint(xs,ys, rcSrc);
		if (dsq < dsqMin)
		{
			dsqMin = dsq;
			pboxClosest = pbox;
		}
		else if (dsq == dsqMin)
		{
			//	two are equidistant. Normally we can pick the first arbitrarily;
			//	but if one is an empty string at the border of the other, we
			//	were probably aiming at the empty string.
			if (pbox->Width() == 0)
			{
				pboxClosest = pbox; // no need to change dsq
			}
		}

	}
	return pboxClosest->FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, prcSrc, prcDst);
}

/*----------------------------------------------------------------------------------------------
	Given a coordinate transformation that would be used to draw the recipient,
	figure the one for the specified child.
	By default just adjust rcSrc by the appropriate offset
----------------------------------------------------------------------------------------------*/
void VwGroupBox::CoordTransForChild(IVwGraphics * pvg, VwBox * pboxChild, Rect rcSrc, Rect rcDst,
	Rect * prcSrc, Rect * prcDst)
{
	*prcDst = rcDst;
	rcSrc.Offset(-Left(), -Top());
	*prcSrc = rcSrc;
}

/*----------------------------------------------------------------------------------------------
	Return your count of children (without expanding lazy boxes)
----------------------------------------------------------------------------------------------*/
int VwGroupBox::ChildCount()
{
	int cbox = 0;
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextOrLazy())
		cbox++;
	return cbox;
}
/*----------------------------------------------------------------------------------------------
	Return your nth child (without expanding lazy boxes)
----------------------------------------------------------------------------------------------*/
VwBox * VwGroupBox::ChildAt(int index)
{
	int cbox = 0;
	VwBox * pbox;
	for (pbox = FirstBox(); pbox && cbox < index; pbox = pbox->NextOrLazy())
		cbox++;
	return pbox;
}
VwBox * VwGroupBox::FindBoxContaining(Point ptDst, IVwGraphics * pvg, Rect rcSrcRoot,
	Rect rcDstRoot)
{
	if (!GetBoundsRect(pvg, rcSrcRoot, rcDstRoot).Contains(ptDst))
		return NULL;
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		if (pbox->GetBoundsRect(pvg, rcSrcRoot, rcDstRoot).Contains(ptDst))
			return pbox;
	}
	return this;
}


//:>********************************************************************************************
//:>	VwPileBox methods
//:>********************************************************************************************

// Handle a request for a change to the distance between pbox and the previous box (or the top
// of this pile box, if no previous box) as a result of synchronization.
// Answer whether anything changed.
bool VwPileBox::SetActualTopToTop(VwBox * pboxFix, int dyp)
{
	VwBox * pboxPrev = BoxBefore(pboxFix);
	int dypNewTop = dyp;
	if (pboxPrev)
		dypNewTop = pboxPrev->Top() + dyp;
	 // Amount to add to top of pbox and all subsequent boxes and height of this.
	int dypAddToTop = dypNewTop - pboxFix->Top();
	if (dypAddToTop == 0)
		return false; // No change, nothing to do.
	// ENHANCE (EberhardB): we could improve things by eliminating the following loop
	// and the loop in VwPileBox::SetActualTopToTopAfter since we set the tops of
	// following boxes later on. If we do that we also have to set the height somewhere
	// else.
	// Fix the top of the changed box and everything below it.
	//for (VwBox * pbox = pboxFix; pbox != NULL; pbox = pbox->NextOrLazy())
	//{
		Assert(pboxFix->Top() + dypAddToTop >= (pboxPrev ? pboxPrev->Bottom() : 0));
		pboxFix->Top(pboxFix->Top() + dypAddToTop);
		//pbox->Top(pbox->Top() + dypAddToTop);
		//pboxPrev = pbox;
	//}
	// Now fix our own size and anything that contains this.
	if (dypAddToTop < 0)
	{
		// Shrinking: it's possible nothing comes after this and the container won't
		// know to invalidate after the new size, nor can SizeChanged do it because
		// it doesn't know the old size. So invalidate before we change it.
		Invalidate();
	}

	Assert(m_dysHeight + dypAddToTop >= 0);
	int dyOld2 = Height();
	int dyOld = FieldHeight();
	//m_dysHeight += dypAddToTop;
	if (pboxFix == LastBox())
		m_dysHeight = pboxFix->VisibleBottom() + GapBottom(Root()->DpiSrc().y);

	if (!Root()->GetSynchronizer() || Root()->GetSynchronizer()->OkToNotifyOfSizeChange())
		SizeChanged();
	else
	{
		if (dynamic_cast<VwRootBox*>(this) && (dyOld != FieldHeight() || dyOld2 != Height()))
			CheckHr(Root()->Site()->RootBoxSizeChanged(Root()));
		Invalidate();
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	A pile scans its cells, extracting each of their rows. This is intended for table cells,
	and possibly Div's inside table cells; it should not be used at a level where laziness
	is required, as it will expand everything! We also don't expect it to be used for inner
	piles, because those go in paragraphs, and paragraph does not recurse to its children.
----------------------------------------------------------------------------------------------*/
void VwPileBox::GetPageLines(IVwGraphics * pvg, PageLineVec & vln)
{
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		Assert(!pbox->IsLazyBox());
		pbox->GetPageLines(pvg, vln);
	}
}
// Handle a request for a change to the distance between pboxFix and the following box (or the bottom
// of this pile box, if no following box) as a result of synchronization.
// Answer whether anything changed.
bool VwPileBox::SetActualTopToTopAfter(VwBox * pboxFix, int dyp)
{
	VwBox * pboxNext = pboxFix->NextOrLazy();
	int dypNewTop = pboxFix->Top() + dyp;
	// Amount to add to top of pboxNext and all subsequent boxes and height of this.
	int dypAddToTop;
	if (pboxNext)
		dypAddToTop = dypNewTop - pboxNext->Top();
	else
	{
		// It may alter the height of this pile just the same.
		int dysProperHeight = dypNewTop + GapBottom(Root()->DpiSrc().y);
		dypAddToTop = dysProperHeight - m_dysHeight;
	}

	if (dypAddToTop == 0)
		return false; // No change, nothing to do.

	// ENHANCE (EberhardB): we could improve things by eliminating the following loop
	// and the loop in VwPileBox::SetActualTopToTop since we set the tops of
	// following boxes later on. If we do that we also have to set the height somewhere
	// else.
	// Fix the top of the changed box and everything below it.
	// Any subsequent boxes that don't have notifiers (e.g., bar boxes that separate objects)
	// need to be snugged up against their preceding boxes so we don't get huge nasty gaps.
	if (pboxNext)
	{
		pboxNext->Top(pboxNext->Top() + dypAddToTop);
		VwBox * pboxPrev = pboxNext;
		for (VwBox * pbox = pboxNext->NextOrLazy(); pbox != NULL; pbox = pbox->NextOrLazy())
		{
			if (!GetLowestNotifier(pbox))
				pbox->Top(pboxPrev->VisibleBottom() + pboxPrev->GapBottom(Root()->DpiSrc().y));
			else
				break;
		//		pbox->Top(pbox->Top() + dypAddToTop);
		}
	}

	// Now fix our own size and anything that contains this.
	if (dypAddToTop < 0)
	{
		// Shrinking: it's possible nothing comes after this and the container won't
		// know to invalidate after the new size, nor can SizeChanged do it because
		// it doesn't know the old size. So invalidate before we change it.
		Invalidate();
	}
	//int dyOld2 = Height();
	//int dyOld = FieldHeight();

//	m_dysHeight += dypAddToTop;
	if (pboxFix == LastBox())
	{
		m_dysHeight = pboxFix->VisibleBottom() + GapBottom(Root()->DpiSrc().y);
	}

	if (!Root()->GetSynchronizer() || Root()->GetSynchronizer()->OkToNotifyOfSizeChange())
		SizeChanged();
	else
	{
		// This code was taken out to fix TE-4200. (related to resizing so the scroll bar was
		// no longer visible which caused a layout to start before this one was finished)
		//if (dynamic_cast<VwRootBox*>(this) && (dyOld != FieldHeight() || dyOld2 != Height()))
		//	CheckHr(Root()->Site()->RootBoxSizeChanged(Root()));
		Invalidate();
	}
	return true;
}

class PileLayoutBinder
{
	IVwGraphics * m_pvg;
	int m_dxsWidth;
	int m_clinesMax;
	int m_clines;
	bool m_fSyncTops;
public:
	PileLayoutBinder(IVwGraphics * pvg, int dxsWidth, int clinesMax, bool fSyncTops)
		:m_pvg(pvg), m_dxsWidth(dxsWidth), m_clinesMax(clinesMax), m_clines(0), m_fSyncTops(fSyncTops)
	{
	}
	void operator() (VwBox * pbox)
	{
		// If we have a line limit, stop laying out when we get to it.
		if (m_clines < m_clinesMax)
			pbox->DoLayout(m_pvg, m_dxsWidth, -1, m_fSyncTops);
		if (m_clinesMax < INT_MAX)
			m_clines += pbox->CLines();
	}
};

/*----------------------------------------------------------------------------------------------
	Return the position where the top of the box after pboxPrev should go. (Note that there
	may be no following box, in which case, we just return the bottom of pboxPrev.)

	WARNING! WARNING! WARNING! Unless implementing synchronization, you probably should call
	SyncedFirstBoxTopY instead!
----------------------------------------------------------------------------------------------*/
int VwPileBox::ComputeTopOfBoxAfter(VwBox * pboxPrev, int dypInch)
{
	int ypPos = pboxPrev->VisibleBottom();
	// The height of a box includes its margins. ypPos is therefore
	// now where we would put pboxPrev->Next(), if any, if we added
	// the top and bottom margins.
	// But actually we want something more complex. If MswTopMargin
	// is used, we want that PLUS the current bottom margin,
	// or the max of the two regular margins. (See the discussion of
	// ktptMswTopMargin in TextServe.idh.)
	// Technically, we want the separation to be max (bottom + mswTop, top),
	// where it is now bottom + top.
	VwBox * pboxNext = pboxPrev->NextOrLazy();
	if (pboxNext)
	{
		int dympBottom = pboxPrev->Style()->MarginBottom();
		VwPropertyStore * pzvpsB = pboxNext->Style();
		int dympTop = pzvpsB->MarginTop();
		int dympMswTop = pzvpsB->MswMarginTop();
		int dympCorrectSep = std::max(dympBottom + dympMswTop, dympTop);
		int dympCurrentSep = dympBottom + dympTop;
		ypPos += MulDiv(dympCorrectSep - dympCurrentSep,
			dypInch, kdzmpInch);
	}
	return ypPos;
}

/*----------------------------------------------------------------------------------------------
	Compute the Y position of the first box.
	A first approximation is the GapTop of this box.
	But, in addition to that, we have to make a special adjustment if the first box exists
	and has an MswMarginTop greater than its MarginTop. In that case, it must be moved down
	by the difference between the two.

	WARNING! WARNING! WARNING! Unless implementing synchronization, you probably should call
	SyncedFirstBoxTopY instead!
----------------------------------------------------------------------------------------------*/
int VwPileBox::FirstBoxTopY(int dypInch)
{
	int ypPos = GapTop(dypInch); // top of first box goes here
	if (!m_pboxFirst)
		return ypPos;
	VwPropertyStore * pzvps = m_pboxFirst->Style();
	int dmp = pzvps->MswMarginTop() - pzvps->MarginTop();
	if (dmp <= 0)
		return ypPos;
	ypPos += MulDiv(dmp, dypInch, kdzmpInch);
	return ypPos;
}

// If no synchronization (psync is null), returns the same as ComputeTopOfBoxAfter.
// If synchronized, adjusts result as needed to synchronize.
int VwPileBox::SyncedComputeTopOfBoxAfter(VwBox * pboxCurr, int dypInch,
	VwRootBox * prootb, VwSynchronizer * psync)
{
	int ypPos = ComputeTopOfBoxAfter(pboxCurr, dypInch);
	if (psync)
	{
		VwBox * pboxNext = pboxCurr->NextOrLazy();
		if (pboxNext)
		{
			VwNotifier * pnote = GetLowestNotifier(pboxNext);

			if (pnote)
			{
				int dypTopToTopNatural = ypPos - pboxCurr->Top();
				int dypTopToTopActual = psync->SyncNaturalTopToTop(prootb, pnote->Object(),
					dypTopToTopNatural);
				return pboxCurr->Top() + dypTopToTopActual;
			}
		}
		// If there is no next box, or it doesn't have an exact matching notifier
		// (commonly because it is lazy), try for a sync based on this box.
		VwNotifier * pnoteCurr = GetLowestNotifier(pboxCurr);
		if (pnoteCurr)
		{
			int dypTopToTopNatural = ypPos - pboxCurr->Top();
			int dypTopToTopActual = psync->SyncNaturalTopToTopAfter(prootb, pnoteCurr->Object(),
				dypTopToTopNatural);
			return pboxCurr->Top() + dypTopToTopActual;
		}
	}
	return ypPos;
}

// If not synchronized (psync is null), return FirstBoxTopY. If synced, adjust as needed.
int VwPileBox::SyncedFirstBoxTopY(int dypInch, VwRootBox * prootb, VwSynchronizer * psync)
{
	int ypPos = FirstBoxTopY(dypInch);
	if (psync && FirstBox())
	{
		VwNotifier * pnote = GetLowestNotifier(FirstBox());

		if (pnote)
			ypPos = psync->SyncNaturalTopToTop(prootb, pnote->Object(), ypPos);
	}
	return ypPos;
}

/*----------------------------------------------------------------------------------------------
	Given the position where the left of a child box would go for left alignment, and the
	total width available for laying out child boxes, set the Left of the child box
	appropriately.
	The initial dxLeft proposed is GapLeft().
----------------------------------------------------------------------------------------------*/
void VwPileBox::AdjustLeft(VwBox * pbox, int dxLeft, int dxpInnerWidth)
{
	int tal = pbox->Style()->ParaAlign();
	int xPos = dxLeft;
	switch(tal)
	{
	default: // bizarre, treat as left.
	case ktalJustify: // treat as left for piles
	case ktalLeading: // can't distinguish from left for piles?
		// if pbox is a right-to-left paragraph, then leading is really Right.
		if (pbox->Style()->RightToLeft())
			xPos = dxLeft + max(0, dxpInnerWidth - pbox->Width());
		break;
	case ktalLeft: // nothing to do.
		break;
	case ktalTrailing: // can't distinguish from right for piles?
		// if pbox is a right-to-left paragraph, then trailing is really Left.
		if (pbox->Style()->RightToLeft())
			break;
		// fall-through to ktalRight
	case ktalRight:
		xPos = dxLeft + max(0, dxpInnerWidth - pbox->Width());
		break;
	case ktalCenter:
		xPos += std::max(0, (dxpInnerWidth - pbox->Width())/2);
		break;
	}
	pbox->Left(xPos);
}

/*----------------------------------------------------------------------------------------------
	Adjusts the inner boxes so they are aligned with the sync'd rootboxes. Also adjusts the
	height and the width to reflect the new alignments.
----------------------------------------------------------------------------------------------*/
void VwPileBox::AdjustInnerBoxes(IVwGraphics* pvg, VwSynchronizer * psync, BoxIntMultiMap * pmmbi,
	VwBox * pboxFirstNeedingInvalidate, VwBox * pboxLastNeedingInvalidate, bool fDoInvalidate)
{
	int dxpInch;
	CheckHr(pvg->get_XUnitsPerInch(&dxpInch));
	int dypInch;
	CheckHr(pvg->get_YUnitsPerInch(&dypInch));
	int dxpSurroundWidth = SurroundWidth(dxpInch);
	int clinesMax = m_qzvps->MaxLines();
	int clines = 0;
	int xpPos = GapLeft(dxpInch); // left of all boxes typically goes here
	int dxpInnerWidth = 0;

	VwBox * pboxCurr = m_pboxFirst;
	VwRootBox * prootb = Root();
	int ypPos = SyncedFirstBoxTopY(dypInch, prootb, psync); // top of first box goes here

	for (; pboxCurr && clines < clinesMax; pboxCurr = pboxCurr->NextOrLazy())
		dxpInnerWidth = max(dxpInnerWidth, pboxCurr->Width());

	bool fInInvalidateRange = false;
	for (pboxCurr = m_pboxFirst; pboxCurr && clines < clinesMax; pboxCurr = pboxCurr->NextOrLazy())
	{

		if (pboxCurr == pboxFirstNeedingInvalidate)
			fInInvalidateRange = true;

		bool fNeedInvalidate = fInInvalidateRange; // so far just based on own Relayout
		AdjustLeft(pboxCurr, xpPos, dxpInnerWidth);
		if (pboxCurr->Top() != ypPos)
		{
			if (fDoInvalidate)
			{
				//it moved! need to invalidate
				pboxCurr->Invalidate();
				fNeedInvalidate = true;
			}

			pboxCurr->Top(ypPos);
			pboxCurr->CheckBoxMap(pmmbi, prootb);
		}
		// Now we have the actual position, we can invalidate the new box.
		// Note that if *this is inside another div, then the position of *this
		// may still be wrong. However, if the position of *this changes, the whole
		// new *this will be invalidated, which will include the child we want to
		// invalidate here.
		if (fNeedInvalidate)
			pboxCurr->Invalidate();

		ypPos = SyncedComputeTopOfBoxAfter(pboxCurr, dypInch, prootb, psync);
		clines += pboxCurr->CLines();

		if (pboxCurr == pboxLastNeedingInvalidate)
			fInInvalidateRange = false;
	}
	while (pboxCurr)
	{
		// There are boxes we are not using, because of the max line count. Give them a bizarre
		// Top position.
		pboxCurr->Top(knTruncated);
		pboxCurr = pboxCurr->NextOrLazy();
	}
	// Now ypPos is where we would put the next box (if there was another). But our actual height
	// is greater by the bottom gap
	m_dysHeight = ypPos + GapBottom(dypInch);
	if (psync)
		psync->AdjustSyncedBoxHeights(this, dypInch);
	m_dxsWidth = dxpInnerWidth + dxpSurroundWidth;
}

/*----------------------------------------------------------------------------------------------
	Do the basic layout of the box.
	Note that there is very similar logic in Relayout(), also near the end of VwLazyBox::
	ExpandItems(). Keep all three in sync.
----------------------------------------------------------------------------------------------*/
void VwPileBox::DoLayout(IVwGraphics* pvg, int dxpAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
	int dxpInch;
	CheckHr(pvg->get_XUnitsPerInch(&dxpInch));
	int dxpSurroundWidth = SurroundWidth(dxpInch);
	int dxpInnerAvailWidth = dxpAvailWidth - dxpSurroundWidth;

	//call DoLayout(pvg, dxpInnerAvailWidth) for each child

	PileLayoutBinder plb (pvg, dxpInnerAvailWidth, m_qzvps->MaxLines(), fSyncTops);
	this->ForEachChild(plb);

	AdjustInnerBoxes(pvg, fSyncTops ? Root()->GetSynchronizer() : NULL);
}

// Find the non-pile box within yourself whose bottom (excluding bottom margin) is
// stricly greater than dysPosition.
// (Recurse as necessary.)
// May also return the offset into the final box.
//Note: dysPosition may be between the top and bottom of the box, in which case the offset
//returned is positive (or zero); it may also be above the top of the box, in which case
//the offset is negative.
// Note that the default implementation on VwBox always succeeds, returning itself and the
// position passed. However, paragraph box overrides, because in certain circumstances
// there is white space at the bottom of the paragraph that is not part of the bottom margin
// of the paragraph.
// Note: tests for this function in TestLayoutPage.h
VwBox * VwPileBox::FindNonPileChildAtOffset (int dysPosition, int dpiY, int * pdysOffsetIntoBox)
{
	// In subsequent iterations, becomes the box before pboxChild.
	// Used when pboxChild proves to be a lazy box that needs expanding, to continue
	// the calculation with whatever follows it after expanding (part of) the lazy box.
	// (It is NOT necessarily a real box, it might be a lazy box we chose not to expand.)
	VwBox * pboxPrev = NULL;

	for (VwBox * pboxChild = FirstBox(); pboxChild; )
	{
		if (dysPosition < pboxChild->Bottom() - MulDiv(pboxChild->MarginBottom(), dpiY, kdzmpInch))
		{
			// the draw-able part of the child extends below the target position, so it's a strong candidate.

			// We can't return a lazy box, so one in a promising place must be expanded.
			if (pboxChild->IsLazyBox())
			{
				VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(pboxChild);
				int ihvoExpand = 0;
				int sizeOfPreviousItems = 0;
				while (ihvoExpand < plzbox->CItems() - 1)
				{
					int sizeOfCurrentItem = plzbox->ItemHeight(ihvoExpand);
					if (plzbox->Top() + sizeOfPreviousItems + sizeOfCurrentItem > dysPosition)
						break; // this item should contain the target when expanded.
					sizeOfPreviousItems += sizeOfCurrentItem;
					ihvoExpand++;
				}

				// May destroy plzbox and possibly neighbors
				plzbox->ExpandItems(ihvoExpand, ihvoExpand + 1);

				// Continue the loop after the previous non-lazy box or from the start. Expanding this one may
				// have expanded neighboring preceding boxes, too.
				pboxChild = (pboxPrev == NULL) ? FirstBox() : pboxPrev->NextOrLazy();
				continue;
			}

			VwBox * pboxSubChild = pboxChild->FindNonPileChildAtOffset(dysPosition - pboxChild->Top(),
				dpiY, pdysOffsetIntoBox);
			if (pboxSubChild)
				return pboxSubChild;
			// Didn't find an embedded box that satisfies the condition; probably,
			// we were close to the bottom of pboxChild, in the bottom margin of the
			// last of its children.
			// Continue with the next of our own children, if any.
		}
		pboxPrev = pboxChild;
		pboxChild = pboxChild->NextOrLazy();
	}
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Default page 'lines' of an indivisible box produces a single line containing the box itself,
	it's top relative to the whole document, and its bottom not counting any bottom margin
	(but including anything visible that extends below the 'bottom').
----------------------------------------------------------------------------------------------*/
void VwBox::GetPageLines(IVwGraphics * pvg, PageLineVec & vln)
{
	PageLine ln;
	ln.pboxFirst = this;
	ln.pboxLast = this;
	ln.ypTopOfLine = TopToTopOfDocument();

	int dpiY;
	CheckHr(pvg->get_YUnitsPerInch(&dpiY));

	int ysBottomNoMargins = VisibleBottom() - TotalBottomMargin(dpiY); // relative to top of container
	// Transform it to be relative to the whole root box.
	Rect rcSrc(0,0,1,1); //Identity transform
	Rect rcOutSrc;
	Rect rcOutDst;
	CoordTransFromRoot(pvg, rcSrc, rcSrc, &rcOutSrc, &rcOutDst);
	ln.ypBottomOfLine = rcOutSrc.MapYTo(ysBottomNoMargins, rcOutDst);

	vln.Push(ln);
}


/*----------------------------------------------------------------------------------------------
	When the contents of a box changes, and its size consequently might change, something
	needs to be done about the layout of that box and all its containers.
	The process begins by constructing a FixupMap, which contains the changed box and all
	its parents, each recording (against the box as a key) the invalidate rectangle appropriate
	to the old box layout.
	We then pass this to the Relayout method of the root box.
	By default, any box which finds itself in the map, or which has never been laid out
	(its height is zero), does a full normal layout, and invalidates its old (if any)
	rectangle. It can't invalidate its new rectangle, because at the point where relayout
	is called, the parent box may not have finalized the new position of the child...
	so return true if the parent needs to invalidate the new position.
	Relayout should not be used in cases where the available width may have changed, as this
	could affect the layout of boxes that are not in the map.
	Some boxes, notably VwDivBox, may not need to relayout all their children, or even to
	invalidate all their own contents. This can be an important optimization, but must be
	done with care to ensure that what is actually drawn is always correct.
	This DivBox version always returns false; it handles any invalidating that is needed
	if its position does not change. Hence the caller of rootbox::Relayout does not need
	to worry about invalidating.
----------------------------------------------------------------------------------------------*/
bool VwDivBox::Relayout(IVwGraphics * pvg, int dxpAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int dxpAvailOnLine, BoxIntMultiMap * pmmbi)
{
	// If height of this is 0, we are a new box that has never been laid out,
	// and need to do a full layout.
	if (m_dysHeight == 0)
	{
		this->DoLayout(pvg,dxpAvailWidth);
		this->Invalidate();
		return false;
	}
	// If we are not in the map, we are unchanged, and so are all our children;
	// do nothing.
	Rect vrect;
	VwBox * pboxThis = this; // seems to be needed for calling Retrieve
	if (!pfixmap->Retrieve(pboxThis, &vrect))
		return false;

	// Otherwise, we need to recompute layout. Some of our children may be in the map
	// or otherwise need to be laid out.
	int dxpInch;
	CheckHr(pvg->get_XUnitsPerInch(&dxpInch));
	int dxpSurroundWidth = SurroundWidth(dxpInch);
	int dxpInnerAvailWidth = dxpAvailWidth - dxpSurroundWidth;
	int clinesMax = m_qzvps->MaxLines();
	int clinesTot = 0;
	// if working with line count limit, and #lines changes for some embedded object,
	// must do full layout for every subsequent object (until we hit the line limit).
	bool fLayoutAll = false;
	VwBox * pboxCurr;

	// We'll record the first and last boxes that, according to their Relayout methods,
	// need to be invalidated in their new positions.
	// Typically this is only one box.
	// We'll invalidate all boxes between the two, which may very rarely cause excess
	// redrawing, but saves us building yet more data structures.
	VwBox * pboxFirstNeedingInvalidate = NULL;
	VwBox * pboxLastNeedingInvalidate = NULL;

	for (pboxCurr = m_pboxFirst; pboxCurr && clinesMax > 0; pboxCurr=pboxCurr->NextOrLazy())
	{
		//call relayout(pvg, dxpInnerAvailWidth, prootb, pfixmap) for the child.
		// This will invalidate the old area of any changed child.
		// For various reasons we may need to invalidate the new position. This can't validly
		// be done until its new position is established.
		bool fNeedInvalidate = false;
		if (clinesMax < INT_MAX)
		{
			if (fLayoutAll)
			{
				pboxCurr->DoLayout(pvg, m_dxsWidth);
				fNeedInvalidate = true;
			}
			else if (clinesTot < clinesMax)
			{
				int clines = pboxCurr->CLines();
				fNeedInvalidate = pboxCurr->Relayout(pvg,dxpInnerAvailWidth, prootb,
					pfixmap, dxpAvailOnLine, pmmbi);
				fLayoutAll = clines != pboxCurr->CLines();
				clinesTot += clines;
			}
			// else, we don't even need to do the layout, this box won't be used.
		}
		else
		{
			fNeedInvalidate = pboxCurr->Relayout(pvg, dxpInnerAvailWidth, prootb,
				pfixmap, dxpAvailOnLine, pmmbi);
		}
		if (fNeedInvalidate)
		{
			pboxLastNeedingInvalidate = pboxCurr;
			if (pboxFirstNeedingInvalidate == NULL)
				pboxFirstNeedingInvalidate = pboxCurr;
		}
		//if (fNeedInvalidate)
		//{
		//	pboxCurr->Invalidate();
		//	pboxCurr->Top(-1); // forces final loop to invalidate new position.
		//}
	}


	AdjustInnerBoxes(pvg, prootb->GetSynchronizer(), pmmbi, pboxFirstNeedingInvalidate,
		pboxLastNeedingInvalidate, true);

	// The container does NOT necessarily need to invalidate this box; assuming our
	// top position doesn't change, we've invalidated everything necessary. The
	// container, being necessarily a div box itself, will know to invalidate this
	// if its top position HAS changed.
	// It's also important that we don't depend on the caller to invalidate us,
	// because the root box is a subclass of VwDivBox
	return false;
}

VwBox * VwPileBox::FindBoxClicked(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
	Rect * prcSrc, Rect * prcDst)
{
#if 99-99
	{
		StrAnsi sta;
		int xMin = Left();
		int x = xd + rcSrc.left;
		int xMax = Right();
		int yMin = Top();
		int y = yd + rcSrc.top;
		int yMax = Bottom();
		char * pszWhere = (xMin <= x && x <= xMax && yMin <= y && y <= yMax) ? "INSIDE " : "OUTSIDE";
		sta.Format("VwPileBox::FindBoxClicked(}:        %s  %3d %s %3d %s %3d  && %3d %s %3d %s %3d%n",
			pszWhere, xMin, (xMin <= x ? "<=" : "> "), x, (x <= xMax ? "<=" : "< "), xMax,
			yMin, (yMin <= y ? "<=" : "> "), y, (y <= yMax ? "<=" : "< "), yMax);
		::OutputDebugStringA(sta.Chars());
	}
#endif
	VwBox * pboxPrev = NULL;
	VwBox * pbox;
	// Get the coords
	rcSrc.Offset(-Left(), -Top());

	// Get the y coord relative to this box, that is, in the same coords as the (top, left)
	// of each embedded box.
	int ys = rcDst.MapYTo(yd, rcSrc);

	for (pbox = m_pboxFirst;
		pbox && pbox->Top() != knTruncated && IsVerticallyAfter(ys, pbox);
		pbox = pbox->NextOrLazy())
	{
		pboxPrev = pbox;
	}
	if (!(pbox) || pbox->Top() == knTruncated)
	{
		//below the last box; if no box at all return null
		if (!pboxPrev)
			return NULL;
		//otherwise let the last box find it
		return pboxPrev->FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, prcSrc, prcDst);
	}

	//if before the end of the first box, the first box is it
	if (!pboxPrev)
		return pbox->FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, prcSrc, prcDst);

	//use the closest box. First find a distance half way between them, then if ys is after that
	//use the second, otherwise the first.
	int ysSplit = (LeadingEdge(pbox) + TrailingEdge(pboxPrev))/2;
	if (!IsVerticallyAfter(ysSplit, ys))
		return pbox->FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, prcSrc, prcDst);
	else
		return pboxPrev->FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, prcSrc, prcDst);
}

/*----------------------------------------------------------------------------------------------
	Return the height required to display the box in a data entry field. This excludes
	bottom margin, bottom padding unless there is a bottom margin, and recursively any
	similar items on embedded boxes.
----------------------------------------------------------------------------------------------*/
int VwPileBox::FieldHeight()
{
	int dxsResult = SuperClass::FieldHeight();
	// Find last non-truncated box.
	VwBox * pbox;
	VwBox * pboxLast = NULL;
	for (pbox = m_pboxFirst; pbox && pbox->Top() != knTruncated; pbox = pbox->NextOrLazy())
	{
		pboxLast = pbox;
	}
	if (pboxLast)
		dxsResult -= pboxLast->Height() - pboxLast->FieldHeight()
			- pboxLast->ExtraHeightIfNotFollowedByPara();
	return dxsResult;
}


//:>********************************************************************************************
//:>	VwDivBox methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Expand all lazy boxes (recursively)
----------------------------------------------------------------------------------------------*/
void VwDivBox::ExpandFully()
{
	// the last non-lazy box we encountered, or null if we haven't found any.
	VwBox * pboxLastReal = NULL;
	for (VwBox * pbox = FirstBox(); pbox; )
	{
		VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(pbox);
		if (plzbox)
		{
			plzbox->ExpandFully();
			pbox = pboxLastReal ? pboxLastReal->NextOrLazy() : FirstBox();
			continue;
		}
		pboxLastReal = pbox; // got a real one.
		VwDivBox * pdbox = dynamic_cast<VwDivBox *>(pbox);
		if (pdbox)
			pdbox->ExpandFully();
		pbox = pbox->NextOrLazy();
	}
}

/*----------------------------------------------------------------------------------------------
	DivBoxes can be very large, so we only want to draw what is visible. Also, we don't want to
	expand lazy boxes in the course of searching for the right stuff to draw, so we use
	NextOrLazy instead of calling the Next() method. Otherwise, this is
	the same as the GroupBox version.
----------------------------------------------------------------------------------------------*/
void VwPileBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
	// Call the extended version of the method, with default values of the extra args.
	// The arguments that control clipping child boxes for printing are passed with values that will never cause
	// clipping, that is, a top just above the top of this box and a bottom just below its bottom (hence the -1 and +2).
	// This method is not called during printing.
	DrawForeground(pvg, rcSrc, rcDst, ChooseSecondIfInverted(-1, Height() + 2), ChooseSecondIfInverted(Height() + 2, -Height() - 2));
}

// Answer the top of the box following pbox, or if nothing follows it, its own (visible) bottom
int TopOfNextBox(VwBox * pbox)
{
	if (pbox->NextOrLazy() && pbox->NextOrLazy()->Top() != knTruncated)
		return pbox->NextOrLazy()->Top();
	else
		return pbox->VisibleBottom();
}

void VwPileBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTopOfPage,
	int dysPageHeight, bool fDisplayPartialLines)
{
#ifdef _DEBUG
#ifdef _DEBUG_SHOW_BOX
	DebugDrawBorder(pvg, rcSrc, rcDst);
#endif
#endif

	int left, top, right, bottom;
	CheckHr(pvg->GetClipRect(&left, &top, &right, &bottom));
	rcSrc.Offset(-m_xsLeft, -m_ysTop);

	// We won't bother drawing anything above the top or below the bottom of the clip
	// rectangle...except we allow an extra quarter inch, just in case some font draws
	// a bit outside its proper rectangle.
	//bottom -= rcDst.top;
	//top -= rcDst.top;
	int dydInch = rcDst.Height();
	bottom += dydInch / 4;
	top -= dydInch / 4;

	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		if (pbox->Top() == knTruncated)
			return;
		// is this box before the top of the page we want to draw? If not don't draw it
		// Is the top of the NEXT box above the top of the clip rectangle? If so, don't draw
		// this one. (We consider the top of the NEXT box rather than the bottom of this,
		// because some boxes draw borders in the gap between the two boxes, so it's possible
		// if the top of the following box is visible that some of the border of this box is,
		// even though none of this box itself is.)
		if (rcSrc.MapYTo(TopOfNextBox(pbox), rcDst) > top && pbox->VisibleBottom() > ysTopOfPage)
		{
			// Adjust the top of page to be relative to this box. It's okay for this to go
			// negative because it represents the distance from the top of the box to the
			// top of the page and the top of the page could be above the top of this box.
			pbox->Draw(pvg, rcSrc, rcDst, ysTopOfPage - pbox->Top(), dysPageHeight, fDisplayPartialLines);
		}

		// is this box after the bottom of the page we want to draw? If so don't draw any more.
		// This is important to prevent fragments of the next page's top line at the bottom of
		// a page. It must be precise and in source coords.

		// If this box extends below the bottom of the clip rect we can be sure we don't
		// need to draw any more boxes. Checking (before the draw) whether this box's top is
		// below the bottom is less reliable, because occasionally boxes (e.g., adjacent
		// paragraphs with borders and the same properties) draw in the space between them.
		// It's important to check this because, in a non-print-preview view, we have no known page
		// boundaries, so it is important to observe the clip rectangle or we will draw
		// everything, with bad consequences for performance.
		// Note that we don't use VisibleBottom() here. If pbox is a one-line drop-cap paragraph,
		// it's visible bottom (the bottom of the drop cap) may be below the bottom of the page/clip
		// rectangle, but we still want to draw the following paragraph, which overlaps the bottom
		// of the drop cap.
// TODO-Linux: This breaks backtranslation-draftview -
// Remove when we no longer use RomRender (this isn't neccessary the problem, but it could be)
#if WIN32
		if (rcSrc.MapYTo(pbox->Bottom(), rcDst) > bottom ||
			pbox->Bottom() >= ysTopOfPage + dysPageHeight)
		{
			return;
		}
#else
		if (/*rcSrc.MapYTo(pbox->Bottom(), rcDst) > bottom  ||*/ //pbox->Bottom() > bottom ||
			pbox->Bottom() >= ysTopOfPage + dysPageHeight)
		{
			return;
		}

#endif



	}
}

// Performance Note:  gathered from JetBrains dotTrace 2.0
// 9.57 % PrepareToDraw* - 5798 ms - 8 calls - SIL.FieldWorks.Common.COMInterfaces._VwRootBoxClass.PrepareToDraw(IVwGraphics, Rect, Rect)
// This method is taking a large amount of time and could be a place for future performance
//   refactoring.
/*----------------------------------------------------------------------------------------------
	Prepare to draw: make sure that no lazy boxes intersect the draw rectangle. If they do,
	expand them as needed.

	Return true if expanding forced a scroll of the parent window.
----------------------------------------------------------------------------------------------*/
VwPrepDrawResult VwDivBox::PrepareToDraw(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
	int xdLeftClip, ydTopClip, xdRightClip, ydBottomClip;
	CheckHr(pvg->GetClipRect(&xdLeftClip, &ydTopClip, &xdRightClip, &ydBottomClip));
	rcSrc.Offset(-m_xsLeft, -m_ysTop);
	VwPrepDrawResult xpdr = kxpdrNormal;
	AssertObj(this);

	// This box is the place where we will resume our loop if we have to expand a lazy box.
	// It is initially NULL, later it becomes the most recently encountered real box.
	// Note that we have to save the last real box, not just the previous box, because
	// it is possible that expanding one lazy box results in the preceding lazy boxes
	// getting expanded also.
	VwBox * pboxResume = NULL;

	for (VwBox * pbox = FirstBox(); pbox; )
	{
		AssertObj(pbox);
		if (pbox->Top() == knTruncated)
			return xpdr;
		int ydTopBox = rcSrc.MapYTo(pbox->Top(), rcDst);
		if (ydTopBox > ydBottomClip)
			return xpdr;
		int ydBottomBox = rcSrc.MapYTo(pbox->VisibleBottom(), rcDst);
		if (ydBottomBox > ydTopClip)
		{
			// It's a box we need to draw! If it's a lazybox, expand it
			VwLazyBox * plzb = dynamic_cast<VwLazyBox *>(pbox);
			if (plzb)
			{
				AssertObj(plzb);
				Rect rcSrcChild = rcSrc;

				int ihvoMin;
				int ihvoLim;
				plzb->GetItemsToExpand(ydTopClip, ydBottomClip, rcSrcChild, rcDst, &ihvoMin, &ihvoLim);

				bool fForce;
				plzb->ExpandItems(ihvoMin, ihvoLim, &fForce);
				if (fForce)
					xpdr = kxpdrInvalidate;
				else if (xpdr == kxpdrNormal)
					xpdr = kxpdrAdjust;
				AssertObj(this);
				// Go on with the loop, but do NOT advance pbox. Several situations need to be
				// dealt with which are all covered by going back to the last real box we
				// encountered:
				// 1. We expanded somewhere in the middle of the lazy box. In this case,
				// pbox is unchanged (still the lazy box), but it now contains fewer items
				// and should fit. Going back over it verifies this. Then we continue with the
				// boxes produced by the expansion, if any.
				// 2. We expanded at the start of the lazy box. We need to go back to a place
				// before pbox in order to call PrepareToDraw for the boxes resulting from
				// the expansion, and in case some of them are lazy boxes for a deeper level
				// of object.
				// 3. We expanded at the start and produced no boxes for these items. pbox
				// is still the lazy one, but we need to try expanding it some more.
				// 4. pbox was completely expanded and no longer exists.
				// 5. A lazy box BEFORE pbox got expanded as a side effect of expanding pbox.
				// (This can happen when dealing with borders and numbered paragraphs.)
				// This last one is the case that forces us to go all the way back to a
				// box that was previously encountered as a real box, to be sure we don't
				// try to use a pointer to a deleted box.
				pbox = pboxResume ? pboxResume : FirstBox();
				continue;
			}
			// We get here only if pbox is a non-lazy box which intersects the clip rectangle.
			// Since it might still contain lazy boxes, we have to ask it to prepare also.
			// Note: apparently max is a macro, not an inline function. If we don't have this
			// temp var, but put the call inside the macro, it gets called twice...which is
			// not only wasteful! The second time it never needs more adjusting, so we always
			// get back kxpdNormal, and may miss the need for an adjustment.
			VwPrepDrawResult xpdrT = pbox->PrepareToDraw(pvg, rcSrc, rcDst);
			xpdr = (VwPrepDrawResult)max(xpdr, xpdrT);
		}
		// If we didn't deliberately do go backwards following an expand,
		// we now need to advance to the next box.
		AssertObjN(pbox->NextOrLazy());
		pboxResume = pbox;
		pbox = pbox->NextOrLazy();
	}
	return xpdr;
}


/*----------------------------------------------------------------------------------------------
	Find the best (last) place to put a "nice" page break in yourself after ysStart.

	The break must come before (or at) pvpi->m_rcDoc.bottom (in dest coords).

	A "nice" break is one that does not split paragraph lines or
	boxes that don't know how to divide themselves, nor does it violate "keep together"
	or "keep with next", unless fDisregardKeeps is true.

	rcSrc and rcDst provide a coordinate transformation from source to actual printing coords,
	as passed to Draw() (that is, they are the pair used to interpret this box's Top()).

	ysStart and *pysEnd are in the coordinate system of your own Top() value, that is,
	relative to the container's top.

	Return true if any nice break is possible.
----------------------------------------------------------------------------------------------*/
bool VwPileBox::FindNiceBreak(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst,
	int ysStart, int * pysEnd, bool fDisregardKeeps)
{
	if ((!fDisregardKeeps) && Style()->KeepTogether())
		return false; // Can't nicely divide a keeptogether box.

	// Skip boxes we have already printed entirely on a previous page
	// TODO 1324(JohnT): If we are converting boxes on earlier pages back into lazy boxes,
	// we may need a change here.
	// Get the start position in the coordinate system of child box Top() and Bottom().
	int ysStartChild = ysStart - Top();
	VwBox * pbox;
	for (pbox = FirstRealBox();
		pbox && pbox->Top() != knTruncated && !IsVerticallySameOrAfter(TrailingPrintEdge(pbox, rcSrc.Height()), ysStartChild); // pbox->Bottom() <= ysStartChild;
		pbox = pbox->NextRealBox())
	{
	}
	// OK, the nice break will be in or after or at the end of pbox, if found at all.
	bool fResult = false;
	Rect rcSrcChild = rcSrc;
	rcSrcChild.Offset(-m_xsLeft, -m_ysTop);
	for (; pbox; pbox = pbox->NextRealBox())
	{
		// If this is an invisible, truncated box, or if it starts after the page break
		// position, we can stop.
		if (pbox->Top() == knTruncated ||
			!IsVerticallySameOrAfter(TrailingEdge(pvpi->m_rcDoc), rcSrcChild.MapYTo(LeadingPrintEdge(pbox, rcSrc.Height()), rcDst)))
			//rcSrcChild.MapYTo(pbox->Top(), rcDst) >= pvpi->m_rcDoc.bottom)
		{
			break; // don't consider breaks in pbox or after.
		}
		// If the box does not have the KeepWithNext property, and fits, and there
		// is another box after it, its end is a possible break.
		// Note: from one point of view, there's also a possible break after it if there
		// is NO box after it. However, that break is effectively after the end of the
		// whole pile. So we don't want to return that as a possible place to break up
		// the pile. Doing so might violate a keep-with-next on the pile as a whole.
		if (pbox->NextRealBox() && (fDisregardKeeps || !pbox->KeepWithNext()))
		{
			int ydEnd = rcSrcChild.MapYTo(TrailingPrintEdge(pbox, rcSrc.Height()), rcDst);
			if (!IsVerticallySameOrAfter(ydEnd, TrailingEdge(pvpi->m_rcDoc) )) //ydEnd <= pvpi->m_rcDoc.bottom)
			{
				// If we use this break, we will consider ourselves to have fitted
				// on the page everything up to the top of the next box.
				*pysEnd = LeadingPrintEdge(pbox->NextRealBox(), rcSrc.Height()) + Top();
				// That counts as success only if the box extends into this page; if it's
				// entirely above the top we haven't found a new break.
				fResult = (*pysEnd > ysStart);
				continue;
			}
		}
		// Otherwise this box gives us a break only if we can find one within it.
		if (pbox->FindNiceBreak(pvpi, rcSrcChild, rcDst, ysStartChild, pysEnd, fDisregardKeeps))
		{
			*pysEnd += Top();
			fResult = (*pysEnd > ysStart);
		}
	}
	return fResult;
}

void VwDivBox::PrintPage(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst,
	int ysStart, int ysEnd)
{
	Rect rcSrcChild = rcSrc;
	rcSrcChild.Offset(-m_xsLeft, -m_ysTop);

	DrawBorder(pvpi->m_pvg, rcSrc, rcDst);

	VwBox * pbox;
	// Skip boxes we have already printed entirely on a previous page
	// We have to subtract m_ysTop from ysStart & ysEnd to put them into a coordinate system
	// that matches our embedded boxes' Top() and Bottom() coords.
	ysStart -= m_ysTop;
	ysEnd -= m_ysTop;
	int ysStartChild = ysStart - Top();
	for (pbox = FirstRealBox();
		pbox && pbox->Top() != knTruncated && !IsVerticallyAfter(TrailingPrintEdge(pbox, rcSrc.Height()), ysStartChild); // pbox->Bottom() < ysStartChild;
		pbox = pbox->NextRealBox())
	{
	}

	for (; pbox && pbox->Top() != knTruncated && IsVerticallyAfter(ysEnd, LeadingPrintEdge(pbox, rcSrc.Height())); //pbox->Top() < ysEnd;
		pbox = pbox->NextRealBox())
	{
		pbox->PrintPage(pvpi, rcSrcChild, rcDst, ysStart, ysEnd);
	}
}

/*----------------------------------------------------------------------------------------------
	Return the first real box in the division (if any), by expanding any initial lazy boxes.
----------------------------------------------------------------------------------------------*/
VwBox * VwDivBox::FirstRealBox()
{
	while (m_pboxFirst && m_pboxFirst->Expand())
		;
	return m_pboxFirst;
}

/*----------------------------------------------------------------------------------------------
	Return the last real box in the division (if any), by expanding any final lazy boxes.
----------------------------------------------------------------------------------------------*/
VwBox * VwDivBox::LastRealBox()
{
	VwLazyBox * plzb;
	while ((plzb = dynamic_cast<VwLazyBox *>(m_pboxLast)) != NULL)
		plzb->ExpandItems(plzb->CItems() - 1, plzb->CItems());
	return m_pboxLast;
}

/*----------------------------------------------------------------------------------------------
	Return the first real box in the division (if any) before the argument. Return NULL if the
	argument is your first box.
----------------------------------------------------------------------------------------------*/
VwBox * VwDivBox::RealBoxBefore(VwBox * pboxSub)
{
	// Normally we do one iteration, but if that finds a lazy box we expand it at the end
	// and try again.
	for (;;)
	{
		if (FirstBox() == pboxSub)
			return NULL;
		VwBox * pbox;
		for (pbox=FirstBox(); pbox && pbox->NextOrLazy()!=pboxSub; pbox = pbox->NextOrLazy())
			;
		Assert(pbox);
		VwLazyBox * plzb = dynamic_cast<VwLazyBox *>(pbox);
		if (!plzb)
			return pbox;
		// Do the mimimum expansion of the lazy box (at the end).
		int citems = plzb->CItems();
		plzb->ExpandItems(citems - 1, citems, NULL);
	}
}



//:>********************************************************************************************
//:>	VwLeafBox methods (none at present)
//:>********************************************************************************************

//:>********************************************************************************************
//:>	VwSeparatorBox methods
//:>********************************************************************************************

VwSeparatorBox::~VwSeparatorBox()
{
}

//make a gray bar, 2 points wide and the current font height. Add one point on either side.
void VwSeparatorBox::DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
	int dxpInch;
	CheckHr(pvg->get_XUnitsPerInch(&dxpInch));
	int dypInch;
	CheckHr(pvg->get_YUnitsPerInch(&dypInch));

	OLECHAR dummyChar = 'A';
	CheckHr(pvg->GetTextExtent(1, &dummyChar, &m_dxsWidth, &m_dysHeight));
	m_dysHeight += SurroundHeight(dypInch);
	m_dxsWidth = 6 * dxpInch / 72 + SurroundWidth(dxpInch);
	CheckHr(pvg->get_FontAscent(&m_dysAscent));
	m_dysAscent += GapTop(dypInch);
}

void VwSeparatorBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
#ifdef _DEBUG
#ifdef _DEBUG_SHOW_BOX
	DebugDrawBorder(pvg, rcSrc, rcDst);
#endif
#endif

	int top = rcSrc.MapYTo(Top() + GapTop(rcSrc.Height()), rcDst);
	int left = rcSrc.MapXTo(Left() + GapLeft(rcSrc.Width()), rcDst);
	int bottom = rcSrc.MapYTo(Top() + GapTop(rcSrc.Height()) + Height(), rcDst);
	int right = rcSrc.MapXTo(Left() + GapLeft(rcSrc.Width()) + Width(), rcDst);
	int inset = (right - left) / 3;
	left += inset;
	right -= inset;
#if WIN32
	CheckHr(pvg->put_BackColor(::GetSysColor(COLOR_3DFACE)));
#else //WIN32
	// TODO-Linux: implement better.
	// set to default grey RGB color
	CheckHr(pvg->put_BackColor(RGB(128,128,128)));
#endif //WIN32
	CheckHr(pvg->DrawRectangle(left, top, right, bottom));
}

//:>********************************************************************************************
//:>	VwBarBox methods
//:>********************************************************************************************

VwBarBox::~VwBarBox()
{
}

/*----------------------------------------------------------------------------------------------
	Make the box big enough for a bar of the specified height and width, plus borders if any,
	and figure the actual ascent that will position it correctly.
----------------------------------------------------------------------------------------------*/
void VwBarBox::DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
	int dxpInch;
	CheckHr(pvg->get_XUnitsPerInch(&dxpInch));
	int dypInch;
	CheckHr(pvg->get_YUnitsPerInch(&dypInch));

	m_dysHeight = MulDiv(m_dmpHeight, dypInch, kdzmpInch) + SurroundHeight(dypInch);

	if (m_dmpWidth == -1)
	{
		// Magic value, means use all available width on line. If not in a paragraph,
		// just use all the available width.
		m_dxsWidth = dxpAvailOnLine == -1 ? dxAvailWidth : dxpAvailOnLine;
	}
	else
	{
		m_dxsWidth = MulDiv(m_dmpWidth, dxpInch, kdzmpInch) + SurroundWidth(dxpInch);
	}
	m_dysAscent =  GapTop(dypInch) + MulDiv(m_dmpBaselineOffset, dypInch, kdzmpInch)
		+ MulDiv(m_dmpHeight, dypInch, kdzmpInch);
}

/*----------------------------------------------------------------------------------------------
	Draw the actual rectangle.
----------------------------------------------------------------------------------------------*/
void VwBarBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
#ifdef _DEBUG
#ifdef _DEBUG_SHOW_BOX
	DebugDrawBorder(pvg, rcSrc, rcDst);
#endif
#endif

	if (m_rgbColor == kclrTransparent)
		return; // nothing to do.
	int top = rcSrc.MapYTo(Top() + GapTop(rcSrc.Height()), rcDst);
	int left = rcSrc.MapXTo(Left() + GapLeft(rcSrc.Width()), rcDst);
	int bottom = rcSrc.MapYTo(Top() + GapTop(rcSrc.Height()) + Height(), rcDst);
	int right = rcSrc.MapXTo(Left() + GapLeft(rcSrc.Width()) + Width(), rcDst);
	CheckHr(pvg->put_BackColor(m_rgbColor));
	CheckHr(pvg->DrawRectangle(left, top, right, bottom));
}

//:>********************************************************************************************
//:>	VwPictureBox methods
//:>********************************************************************************************

VwPictureBox::~VwPictureBox()
{
#if !WIN32
	// On Linux we use a managed IPicture implementation that also implements IDisposable,
	// so we should call Dispose()
	if (m_qpic)
	{
		// If m_qpic implements IComDisposable we can call Dispose()
		// but only if we are the last Native reference to the IPicture ccw.
		IComDisposablePtr qdisp;
		HRESULT hr = m_qpic->QueryInterface(IID_IComDisposable, (void **)&qdisp);
		if (!(SUCCEEDED(hr) && qdisp))
			return;

		IPicture * ppic = m_qpic.Detach();
		int refcount = ppic->Release();
		// if the only refcount is qdisp.
		if (refcount == 1)
			qdisp->Dispose();
	}
#endif
}
#define HIMETRIC_INCH 2540
/*----------------------------------------------------------------------------------------------
	Compute the picture's size and just use it, for now. Eventually we may implement
	cropping and scaling. Make the ascent equal to the GapTop plus the actual picture
	size, to align the bottom of the picture with the baseline, as Word typically does
	for inline pictures.
----------------------------------------------------------------------------------------------*/
void VwPictureBox::DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
	int dxpInch;
	CheckHr(pvg->get_XUnitsPerInch(&dxpInch));
	int dypInch;
	CheckHr(pvg->get_YUnitsPerInch(&dypInch));

	// get width and height of picture
	COMINT32 hmWidth;
	COMINT32 hmHeight;
	m_qpic->get_Width(&hmWidth);
	m_qpic->get_Height(&hmHeight);

	// Convert himetric to pixels. (Adjust for margins etc. later.)
	// These used to be calculated using MulDiv but that returned truncated results. We
	// want rounded results. -- DavidO
	m_dxsWidth = ((dxpInch * hmWidth + HIMETRIC_INCH / 2) / HIMETRIC_INCH);
	m_dysHeight = ((dypInch * hmHeight + HIMETRIC_INCH / 2) / HIMETRIC_INCH);
	float aspect = 1.0;
	if (m_dxsWidth != 0)
		aspect = (float)m_dysHeight / m_dxsWidth;

	int dxsWidth = MulDiv(m_dxmpWidth, dxpInch, kdzmpInch);
	int dysHeight = MulDiv(m_dympHeight, dypInch, kdzmpInch);

	if (dxsWidth < 0)
	{
		m_dxsWidth = dxsWidth;
		if (dysHeight < 0)
		{
			// Both absolute
			m_dysHeight = dysHeight;
		}
		else
		{
			// Compute height to suit width.
			m_dysHeight = (int)(m_dxsWidth * aspect);
		}
	}
	else if (dysHeight < 0)
	{
		// Compute width to suit height
		m_dxsWidth = (int)(m_dysHeight / aspect);
	}
	else // both >= 0, treat as max or unspecified.
	{
		// convert zeros to true max
		int dysMaxHeight = dysHeight;
		if (dysMaxHeight == 0)
			dysMaxHeight = INT_MAX;
		int dxsMaxWidth = dxsWidth;
		if (dxsMaxWidth == 0 || dxsMaxWidth > dxAvailWidth)
			dxsMaxWidth = dxAvailWidth;

		if (m_dxsWidth > dxsMaxWidth)
		{
			m_dysHeight = (int)(dxsMaxWidth * aspect);
			m_dxsWidth = dxsMaxWidth;
		}
		if (m_dysHeight > dysMaxHeight)
		{
			m_dxsWidth = (int)(dysMaxHeight / aspect);
			m_dysHeight = dysMaxHeight;
		}
	}

	// Make the ascent right. This aligns the bottom of the picture with the text baseline,
	// adjusted for any offset.
	int dympOffset;
	CheckHr(m_qzvps->get_FontSuperscript(&dympOffset));
	m_dysAscent = m_dysHeight + GapTop(dypInch) + MulDiv(dympOffset, dypInch, kdzmpInch);
	// Then adjust for margins etc.
	m_dysHeight += SurroundHeight(dypInch);
	m_dxsWidth += SurroundWidth(dxpInch);
}

/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
void VwPictureBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
#ifdef _DEBUG
#ifdef _DEBUG_SHOW_BOX
	DebugDrawBorder(pvg, rcSrc, rcDst);
#endif
#endif

	int dypTop = Top();
	int dxpLeft = Left() + AdjustedLeft();
	int ydTop = rcSrc.MapYTo(dypTop + GapTop(rcSrc.Height()), rcDst);
	int xdLeft = rcSrc.MapXTo(dxpLeft + GapLeft(rcSrc.Width()), rcDst);
	int ydBottom = rcSrc.MapYTo(dypTop + Height() - GapBottom(rcSrc.Height()), rcDst);
	int xdRight = rcSrc.MapXTo(dxpLeft + Width() - GapRight(rcSrc.Width()), rcDst);
	Rect rd(xdLeft, ydTop, xdRight, ydBottom);
	COMINT32 hmWidth;
	COMINT32 hmHeight;
	CheckHr(m_qpic->get_Width(&hmWidth));
	CheckHr(m_qpic->get_Height(&hmHeight));
	pvg->RenderPicture(m_qpic, xdLeft, ydTop, xdRight - xdLeft, ydBottom - ydTop, 0, hmHeight, hmWidth, -hmHeight, &rd);
}
/*----------------------------------------------------------------------------------------------
Overridden to check for a limit that is a picture selection
----------------------------------------------------------------------------------------------*/
void VwPictureBox::Search(VwPattern * ppat, IVwSearchKiller * pxserkl)
{
	VwPictureSelectionPtr qpselLimit;
	IVwSelection * pselLimitT = ppat->Limit();
	if (pselLimitT)
	{
		try{
			CheckHr(pselLimitT->QueryInterface(CLSID_VwPictureSelection, (void **) & qpselLimit));
		}
		catch(Throwable& thr){
			if (thr.Result() != E_NOINTERFACE) {
				throw thr;
			}
		}
		if (qpselLimit && this == qpselLimit->LeafBox())
			CheckHr(ppat->put_StoppedAtLimit(true));
	}
}

//:>********************************************************************************************
//:>	VwIndepPictureBox methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Get a selection.
	Review DavidO: Does this method need to do any more?
----------------------------------------------------------------------------------------------*/
void VwPictureBox::GetSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
	Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, VwSelection ** ppvwsel,
	SearchDirection sdir)
{
	VwParagraphBox * pvpboxContainer = dynamic_cast<VwParagraphBox *>(Container());
	if (pvpboxContainer == NULL)
	{
		*ppvwsel = NewObj VwPictureSelection(this, 1, 1, true); // JohnT: this weird (old) behavior seems to work for pictures in moveable piles, at least.
		return;
	}
	int ichAnchor = pvpboxContainer->Source()->RenToLog(GetCharPosition());
	*ppvwsel = NewObj VwPictureSelection(this, ichAnchor, ichAnchor + 1, true);
}

/*----------------------------------------------------------------------------------------------
	If the picture is embedded in a paragraph box, return the corresponding character position
	in rendered characters, relative to entire paragraph.
----------------------------------------------------------------------------------------------*/
int VwLeafBox::GetCharPosition()
{
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(Container());
	Assert(pvpbox); // Don't call this unless sure container is paragraph.
	int cchPrev = 0;
	for (VwBox * pbox = pvpbox->FirstBox(); pbox != this; pbox = pbox->NextOrLazy())
	{
		Assert(pbox); // we'd better find it!
		if (pbox->IsStringBox())
		{
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
			int dichLim;
			CheckHr(psbox->Segment()->get_Lim(psbox->IchMin(), &dichLim));
			cchPrev = psbox->IchMin() + dichLim;
		}
		else
		{
			cchPrev++;
		}
	}
	return cchPrev;
}

/*----------------------------------------------------------------------------------------------
	Detects hot picture and activates the link.
----------------------------------------------------------------------------------------------*/
void VwPictureBox::DoHotLink(VwPictureSelection * pPicSel)
{
	AssertPtr(pPicSel);

	// 9 = 1 byte for the type + 8 bytes for the GUID. Technically, this must be > 9 because the
	// picture data (which follows the GUID) is also in the string, but for our purposes, we
	// really don't care about that
	if (!m_sbstrObjData || m_sbstrObjData.Length() < 9 || !m_pvpbox)
		return; // Must not be a 'hot' picture

	int ichObjStart, ichObjEnd;
	CheckHr(pPicSel->get_ParagraphOffset(false, &ichObjStart));
	CheckHr(pPicSel->get_ParagraphOffset(false, &ichObjEnd));
	HVO hvoOwner;
	PropTag tag;
	int ichMinEditProp, ichLimEditProp, fragEdit, iprop, itssProp;
	IVwViewConstructorPtr qvvc; // The view constructor, if any, responsible for the edited property.
	VwAbstractNotifierPtr qanote;
	VwNoteProps vnp;
	ITsStringPtr qtssProp;
	if (m_pvpbox->EditableSubstringAt(ichObjStart, ichObjEnd, false, &hvoOwner,
		&tag, &ichMinEditProp, &ichLimEditProp, &qvvc, &fragEdit,
		&qanote, &iprop, &vnp, &itssProp, &qtssProp) != kvepvNone)
	{
		if (!qvvc)
		{
			// The common case, the property was added normally using AddStringProp.
			// Find the higher level view constructor
			VwBox * pboxFirstProp;
			int itssFirstProp;
			Assert(qanote->Parent());
			VwNotifier * pnote = dynamic_cast<VwNotifier *>(qanote.Ptr());
			if (pnote)
			{
				qanote->Parent()->GetPropForSubNotifier(pnote, &pboxFirstProp,
					&itssFirstProp, &tag, &iprop);
				qvvc = qanote->Parent()->Constructors()[iprop];
			}
		}
		IVwRootBoxPtr qrootb;
		CheckHr(pPicSel->get_RootBox(&qrootb));
		ISilDataAccess * psda = NULL;
		if (qrootb)
		{
			VwRootBox * proot = dynamic_cast<VwRootBox *>(qrootb.Ptr());
			psda = proot->GetDataAccess();
		}
		if (qvvc)
			CheckHr(qvvc->DoHotLinkAction(m_sbstrObjData, psda));
	}
}

/*----------------------------------------------------------------------------------------------
	Check for links to dependent objects. For each such object, add it's GUID to the vector.
----------------------------------------------------------------------------------------------*/
void VwPictureBox::GetDependentObjects(Vector<GUID> & vguid)
{
	// 9 = 1 byte for the type + 8 bytes for the GUID. Technically, this must be > 9 because the
	// picture data (which follows the GUID) is also in the string, but for our purposes, we
	// really don't care about that
	if (!m_sbstrObjData || m_sbstrObjData.Length() < 9)
		return; // Must not be a 'hot' picture

	// It's one of the guids we care about.
	GUID guid;
	memcpy(&guid, m_sbstrObjData.Chars() + 1, 16);
	vguid.Push(guid);
}

/*----------------------------------------------------------------------------------------------
	Compute the picture's size and just use it, for now. Eventually we may implement
	cropping and scaling. Make the ascent equal to the GapTop plus the actual picture
	size, to align the bottom of the picture with the baseline, as Word typically does
	for inline pictures.
----------------------------------------------------------------------------------------------*/
void VwIndepPictureBox::DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
	SuperClass::DoLayout(pvg, dxAvailWidth, dxpAvailOnLine, fSyncTops);

	// At this point, after the parent class has done it's work, all this method does
	// is figure out where, within the available width, the box is to be displayed.
	// m_dxsOffset will be added to Left() when the box actually sent to the output
	// device.

	// Ignore any explicit alignment if we're not in a pile box.
	if (!dynamic_cast<VwPileBox *>(Container()))
		return;

	// Do nothing if the available width isn't adaquate or the picture aligns to the left.
	if (m_dxsWidth > dxAvailWidth || Style()->ParaAlign() == ktalLeft)
		return;

	// Center box within available width.
	if (Style()->ParaAlign() == ktalCenter)
	{
		m_dxsOffset = (dxAvailWidth - m_dxsWidth) / 2;
		return;
	}

	// Find out whether old writing system is RTL or LTR.
	ComBool frtl;
	CheckHr(Style()->get_RightToLeft(&frtl));

	// Right-align the box if its alignment is explicitly set to right. Otherwise use the
	// old writing system's direction in conjunction with the trailing/leading properties to
	// determine whether it's right-aligned.
	if (Style()->ParaAlign() == ktalRight ||
		(!frtl && Style()->ParaAlign() == ktalTrailing) ||
		(frtl && Style()->ParaAlign() == ktalLeading))
	{
		m_dxsOffset = dxAvailWidth - m_dxsWidth;
	}
}

/*----------------------------------------------------------------------------------------------
	Make a selection and install it in the given root box (which is your own).
	The default method is overridden to detect hot text and activate the link instead of
	making the selection.

	@return True if the selection was set, false if selection failed due to the insertion point
		being read-only.  If forceReadOnly is true then the selection will not fail.
----------------------------------------------------------------------------------------------*/
MakeSelResult VwIntegerPictureBox::MakeSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd,
	int yd, Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, bool forceReadOnly)
{
	if (m_nValMin == m_nValMax)
		return SuperClass::MakeSelection(pvg, prootb, xd, yd, rcSrcRoot, rcDstRoot,
			rcSrc, rcDst, forceReadOnly);
	ISilDataAccess * psda = Root()->GetDataAccess();

	// Try to find out whether the property is virtual and read-only.  If so, don't do anything!
	// (This fixes bug LT-491.)
	IVwCacheDaPtr qvcd;
	HRESULT hr = psda->QueryInterface(IID_IVwCacheDa, (void **)&qvcd);
	if (SUCCEEDED(hr) && qvcd)
	{
		IVwVirtualHandlerPtr qvh;
		hr = qvcd->GetVirtualHandlerId(m_tag, &qvh);
		if (SUCCEEDED(hr) && qvh)
		{
			ComBool fWriteable = FALSE;
			CheckHr(qvh->get_Writeable(&fWriteable));
			if (!fWriteable)
				return kmsrAction;		// Don't actually do anything -- it's read-only!
		}
	}

	// Get the field name from the MetaDataCache
	SmartBstr sbstrFieldName;
	IFwMetaDataCachePtr qmdc;
	CheckHr(psda->get_MetaDataCache(&qmdc));
	IgnoreHr(hr = qmdc->GetFieldName(m_tag, &sbstrFieldName));
	if (FAILED(hr))
	{
		if (m_tag < 0)
		{
			// We don't expect these virtual fields to have names stored in the meta data
			// cache, so we'll create one for them.
			StrUni stu;
			stu.Format(L"VirtualField%d", m_tag);
			stu.GetBstr(&sbstrFieldName);
		}
		else
		{
			// On the other hand, we shouldn't get here, so crash if we do!
			CheckHr(hr);
		}
	}

	// Start an undo task
	StrUni stuFmtUndo(kstidUndoChangeField);
	StrUni stuFmtRedo(kstidRedoChangeField);
	StrUni stuUndo;
	StrUni stuRedo;
	stuUndo.Format(stuFmtUndo.Chars(), sbstrFieldName.Chars());
	stuRedo.Format(stuFmtRedo.Chars(), sbstrFieldName.Chars());
	CheckHr(psda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));

	int nVal;
	CheckHr(psda->get_IntProp(m_hvo, m_tag, &nVal));
	nVal++;
	if (nVal > m_nValMax)
		nVal = m_nValMin;
	CheckHr(psda->SetInt(m_hvo, m_tag, nVal));
	// End the undo task we started
	CheckHr(psda->EndUndoTask());
	// Enhance JohnT: really we want to return something that means, leave the current selection
	// alone. This at least tells the caller we didn't make one here.
	return kmsrAction;
}

/*----------------------------------------------------------------------------------------------
	See the comment on VwParagraphBox::StretchToPileWidth.
	An inner pile wants to stretch if any of its contained boxes does (or if it has a border
	itself).
----------------------------------------------------------------------------------------------*/
bool VwInnerPileBox::StretchToPileWidth(int dxpInch, int dxpTargetWidth, int tal)
{
	int dxpSurroundWidth = SurroundWidth(dxpInch);
	int dxpInnerWidth = dxpTargetWidth - dxpSurroundWidth;

	// If the pile is centered, justified, or aligned differently from
	// the containing pile, we won't try this trick. (Maybe one day...)
	VwPropertyStore * pvps = Style();
	if (pvps->ParaAlign() != tal)
		return false;

	// Give all our children a chance to stretch. We want to if any of them want to.
	bool fInnerStretch = false;
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		// Order is important here! We want to call StretchToInnerPile even if fInnerStretch
		// is already true!
		fInnerStretch = pbox->StretchToPileWidth(dxpInch, dxpInnerWidth, tal) || fInnerStretch;
	}
	// If we don't have a border or background color, and nothing inside wants to stretch,
	// no reason to stretch at all.
	if ((!fInnerStretch) && pvps->BorderTop() == 0 &&  pvps->BorderBottom() == 0
		&& pvps->BorderLeading() == 0 && pvps->BorderTrailing() == 0
		&& pvps->BackColor() == kclrTransparent)
	{
		return false;
	}
	//int delta = dxpTargetWidth - Width();
	_Width(dxpTargetWidth);
	AdjustBoxHorizontalPositions(dxpInch);
	return true;
}
void VwInnerPileBox::AdjustBoxHorizontalPositions(int dxpInch)
{
	int tal = Style()->ParaAlign();
	// If the pile is aligned right--same as leading for piles--we need to move the box over
	// as well as adjusting its width.
	if (tal == ktalRight || tal == ktalTrailing)
	{
		// Need to recompute the left's of embedded boxes (any that didn't stretch
		// probably need to move right).
		int dxpGapLeft = GapLeft(dxpInch); // left of all boxes typically goes here
		int dxpGapRight = GapRight(dxpInch);
		int clinesMax = m_qzvps->MaxLines();
		int clines = 0;
		// Want the real inner width now we're adjusted, not the one we originally
		// compute by taking the max of all the children.
		int dxpInnerWidth = Width() - dxpGapRight - dxpGapLeft;
		VwBox * pboxCurr;

		for (pboxCurr = m_pboxFirst; pboxCurr && clines < clinesMax; pboxCurr = pboxCurr->NextOrLazy())
		{
			AdjustLeft(pboxCurr, dxpGapLeft, dxpInnerWidth);
		}
	}
}

int VwInnerPileBox::SyncedComputeTopOfBoxAfter(VwBox * pboxCurr, int dypInch,
		VwRootBox * prootb, VwSynchronizer * psync)
{
	return VwPileBox::SyncedComputeTopOfBoxAfter(pboxCurr, dypInch, prootb, NULL);
}

int VwInnerPileBox::SyncedFirstBoxTopY(int dypInch, VwRootBox * prootb, VwSynchronizer * psync)
{
	return VwPileBox::SyncedFirstBoxTopY(dypInch, prootb, NULL);
}

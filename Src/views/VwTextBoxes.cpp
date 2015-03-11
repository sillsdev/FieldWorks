/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TextBoxes.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Contains the classes VwAbstractStringBox (and subclasses) which handle displaying strings,
	plus the VwParagraphBox class which lays them (and other stuff) out.

-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

using namespace std;

#undef THIS_FILE
DEFINE_THIS_FILE

// #define _DEBUG_SHOW_BOX

/***********************************************************************************************
  Summary of line breaking.

  When the paragraph box is laying out lines and comes to a string, it analyzes the string
  into runs, and determines the actual text properties, including old writing system, to use for
  each.  For each old writing system run (WS run), consisting of possibly several adjacent runs
  that differ in various stylistic ways that don't affect old writing system, it makes an
  initial segment (and VwStringBox) containing as much as will fit on the current line.  It then
  makes, if necessary, additional VwStringBoxes containing the rest of the run.

  When a segment is initially created, it has its end of line flag true.  Its width therefore
  does not include trailing space.  For the last segment of a WS run, if there are more boxes in
  the paragraph, we then change it to have end-of-line false.  If it still fits, we try to put
  more stuff after it.  If we can't put anything after it on the same line, we have to set
  end-of-line true again!

  After creating the segments that will go on any given line, we examine the direction
  embedding of the sequence of segments, and figure out how to reorder them.  This will involve
  adjusting the end-of-line flags of any segments that are now on a line boundary (or that
  previously were and are no longer).  That in turn may mean that the segments no longer all fit
  -- for example, if contextual forms in the new end-of-line configuration take more space
  than before.

  If the rearranged segments don't fit, we have to ask the last of them to make an earlier
  break.  If it can't, we remove it from the line, and adjust the end-of-line flag of another
  segment.  If even that won't fit, we ask it to make an earlier break, and so forth..

  If we have a line break in the middle of a segment, all is well, because the writing
  system itself decided that break is appropriate.  If a old writing system boundary coincides
  with a line boundary, we have a harder problem.  If the white space is at the start of
  the following segment, we will just make a trivial zero-width segment which gets stuck
  at the end of the line.  However, if the white space is at the end of the first segment,
  we need to know that we can make a good break at the end of that segment.  Therefore,
  we need a way to tell what kind of break is produced by putting a line break
  at its end (assuming the following segment can't break immediately).  This is done by
  using the line breaking properties of the characters directly.

  White space following (logically) a segment that is logically at the end of the line
  disappears into the margin.  If the logical and physical ends of the line coincide, this
  is not a problem: anything that actually displays the space (such as highlighting a range)
  just highlights the appropriate distance beyond the physical end of the segment.  If they are
  not adjacent, it is necessary to create a special white space segment, which takes no space
  in the paragraph layout, but is physically put at the end of the line.
***********************************************************************************************/


//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	VwStringBox Methods
//:>********************************************************************************************

// This appears to do nothing, but there are two smart pointers that need to get destructed...
VwStringBox::~VwStringBox()
{
}

// Lay the text box out. We assume that the containing paragraph did the interesting
// work; we just copy the relevant dimensions from the segment.
void VwStringBox::DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
	if (!m_qlseg)
	{
		// There is a quite complex process here that the paragraph is also interested in.
		// We need to scan the string, work out the old writing system for each run,
		// determine groups of runs that have the same WS, and attempt to make a segment
		// for each of those--subject to what fits on a line. Then for each ows, we make
		// a segment. However, even if we are not wrapping, we may have to make multiple
		// text boxes to accommodate multiple segments. I think the paragraph had better
		// handle it all--so calling DoLayout before we have a segment is an error.
		// Note: this says text boxes can only occur within paragraphs.
		Warn("laying out text box with no segment");
		ThrowHr(WarnHr(E_UNEXPECTED));
	}

	CheckHr(m_qlseg->get_Width(IchMin(), pvg, &m_dxsWidth));
	CheckHr(m_qlseg->get_Height(IchMin(), pvg, &m_dysHeight));
	CheckHr(m_qlseg->get_Ascent(IchMin(), pvg, &m_dysAscent));
	int dxpInch;
	CheckHr(pvg->get_XUnitsPerInch(&dxpInch));
	int dxpSurroundWidth = SurroundWidth(dxpInch);
	m_dxsWidth += dxpSurroundWidth;
	int dypInch;
	CheckHr(pvg->get_YUnitsPerInch(&dypInch));
	int dysGapTop = GapTop(dypInch);
	m_dysHeight += dysGapTop + GapBottom(dypInch);
	m_dysAscent += dysGapTop;
}

// VwStringBox just passes this on to its segment.
// VwDropCapStringBox corrects the bottom.
Rect VwStringBox::DrawRange(IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int ichMin, int ichLim, int ydLineTop, int ydLineBottom, ComBool fOn,
	Rect rcSrcRoot, Rect rcDstRoot, bool fIsLastParaOfSelection)
{
	int ichOurMin = IchMin();
	int ichLimSeg;
	CheckHr(Segment()->get_Lim(ichOurMin, &ichLimSeg));
	if (ichMin <= ichOurMin && (ichOurMin + ichLimSeg) <= ichLim)
	{
		VwBox* pboxNext = Next();
		if (pboxNext && pboxNext->Baseline() == Baseline())
		{
			// The whole string box is selected. We invert the whole stringbox ourselves.
			// We do this to eliminate rounding errors - when we layout in printer pixels
			// but draw in screen pixels we might end up with a font size of e.g. 12.7 and
			// that's what we use to calculate the width of the string box. However, when
			// we actually draw the text (and the selection) the Windows API really draws
			// with 13 points since it can't handle fractions. When we let the Segment
			// draw the selection it gets the width of the segment from Windows API method
			// ::ScriptCPtoX which calculates it with the rounded value, so we're slightly
			// off. It doesn't matter for the start or end of a selection, but if it is in
			// the middle of a selection we get overlapping/separated inverted rectangles
			// which leave white bars hanging around (TE-4497).
			Rect rcSrc(rcSrc1);
			int xdLeft = rcSrc.MapXTo(0, rcDst1);
			int xdRight = rcSrc.MapXTo(Width(), rcDst1);
			CheckHr(pvg->InvertRect(xdLeft, ydLineTop, xdRight, ydLineBottom));
			return Rect(xdLeft, ydLineTop, xdRight, ydLineBottom);;
		}
		// if we are the last box on the line we let the segment deal with it because
		// there might be trailing spaces outside of our box so we wouldn't select them.
	}
	// Not handled directly by us

	// Only part of the string box is selected. Let the segment deal with inverting
	// the selection.
	Rect rcBounds;
	CheckHr(Segment()->DrawRange(ichOurMin, pvg, rcSrc1, rcDst1, ichMin,
		ichLim, ydLineTop, ydLineBottom, fOn, fIsLastParaOfSelection, &rcBounds));
	return rcBounds;
}
int VwDropCapStringBox::AdjustSelBottom(IVwGraphics * pvg,
	int ydLineBottom, Rect rcSrcRoot, Rect rcDstRoot)
{
	// Drop cap bottom is much below main bottom of line, so need to adjust. We want to use
	// the bottom of the SECOND line.
	VwBox * pbox = NextOrLazy();
	while (pbox && pbox->Ascent() + pbox->Top() == Top() + Ascent())
		pbox = pbox->NextOrLazy();
	if (pbox)
	{
		// There's a second line, use its bottom
		int dum1;
		dynamic_cast<VwParagraphBox *>(Container())->
			GetLineTopAndBottom(pvg, pbox, &dum1, &ydLineBottom, rcSrcRoot, rcDstRoot);
	}
	else
	{
		// No next line in the paragraph, use the extra height
		VwParagraphBox* pvpbox = dynamic_cast<VwParagraphBox *>(Container());
		int dum1, ydFirstLineBottom;
		pvpbox->GetLineTopAndBottom(pvg, this, &dum1, &ydFirstLineBottom, rcSrcRoot, rcDstRoot);
		int ydExtraHeight = pvpbox->ExtraHeightIfNotFollowedByPara(true);
		ydLineBottom = ydFirstLineBottom + MulDiv(ydExtraHeight, rcDstRoot.Height(), rcSrcRoot.Height());
	}
	return ydLineBottom;
}

// VwStringBox just passes this on to its segment.
// VwDropCapStringBox corrects the bottom.
Rect VwDropCapStringBox::DrawRange(IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int ichMin, int ichLim, int ydLineTop, int ydLineBottom, ComBool fOn,
	Rect rcSrcRoot, Rect rcDstRoot, bool fIsLastParaOfSelection)
{
	ydLineBottom = AdjustSelBottom(pvg, ydLineBottom, rcSrcRoot, rcDstRoot);
	return SuperClass::DrawRange(pvg, rcSrc1, rcDst1, ichMin, ichLim, ydLineTop, ydLineBottom, fOn,
		rcSrcRoot, rcDstRoot, fIsLastParaOfSelection);
}

void VwStringBox::PositionOfRange(IVwGraphics* pvg,
	RECT rcSrc1, RECT rcDst1, int ichMin, int ichLim, int ydTop, int ydBottom,
	RECT * prsBounds, ComBool * pfAnythingToDraw, Rect rcSrcRoot, Rect rcDstRoot,
	bool fIsLastParaOfSelection)
{
	Segment()->PositionOfRange(IchMin(), pvg,
		rcSrc1, rcDst1, ichMin, ichLim, ydTop, ydBottom, fIsLastParaOfSelection,
		prsBounds, pfAnythingToDraw);
}

void VwDropCapStringBox::PositionOfRange(IVwGraphics* pvg,
	RECT rcSrc1, RECT rcDst1, int ichMin, int ichLim, int ydTop, int ydBottom,
	RECT * prsBounds, ComBool * pfAnythingToDraw, Rect rcSrcRoot, Rect rcDstRoot,
	bool fIsLastParaOfSelection)
{
	ydBottom = AdjustSelBottom(pvg, ydBottom, rcSrcRoot, rcDstRoot);
	Segment()->PositionOfRange(IchMin(), pvg,
		rcSrc1, rcDst1, ichMin, ichLim, ydTop, ydBottom, fIsLastParaOfSelection,
		prsBounds, pfAnythingToDraw);
}

// Overrides to not worry about the notifiers
void VwStringBox::DeleteAndCleanup(NotifierVec &vpanoteDel)
{
	delete this;
}

// Check for links to dependent objects. For each such object, add it's GUID to the vector.
// Note that the logic here is very similar to VwTextSelection::GetSelectionString1.
// We were not able to figure a clean way to factor it out, however.
void VwStringBox::GetDependentObjects(Vector<GUID> & vguid)
{
	VwTxtSrc * pts = dynamic_cast<VwParagraphBox *>(Container())->Source();
	int ichMin = pts->RenToLog(IchMin()); // offset into logical chars of paragraph.
	int dichLim; // Length of rendered characters in this box's segment
	CheckHr(m_qlseg->get_Lim(IchMin(), &dichLim));
	// end of string box in logical chars relative to para;
	int ichLim = pts->RenToLog(IchMin() + dichLim);
	ITsStringPtr qtss;
	int ichMinTss; // Start of current TsString in paragraph (logical)
	int ichLimTss; // End of current TsString in paragraph (logical)
	int itss; // indexes strings in paragraph.
	VwPropertyStorePtr qzvps;
	pts->StringFromIch(ichMin, false, &qtss, &ichMinTss, &ichLimTss, &qzvps, &itss);
	int ichNew;

	// Loop over the runs of the string(s).
	for (int ich = ichMin; ich < ichLim; ich = ichNew)
	{
		// ich is logical chars, relative to the paragraph, as are ichMinTss and ichLim
		if (qtss) // otherwise embedded picture, or similar.
		{
			ITsTextPropsPtr qttp;
			TsRunInfo tri;
			CheckHr(qtss->FetchRunInfoAt(ich - ichMinTss, &tri, &qttp));
			if (tri.ichLim - tri.ichMin == 1)
			{
				// Run of length 1: it might be an ORC. Get it and see.
				OLECHAR ch;
				qtss->FetchChars(ich - ichMinTss, ich - ichMinTss + 1, &ch);
				if (ch == L'\xfffc')
				{
					// it's an ORC! See if it has objData.
					SmartBstr sbstrObjData;
					CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstrObjData));
					if (sbstrObjData.Length() == 9 && (sbstrObjData.Chars()[0] == kodtOwnNameGuidHot
						|| sbstrObjData.Chars()[0] == kodtNameGuidHot))
					{
						// It's one of the guids we care about.
						GUID guid;
						memcpy(&guid, sbstrObjData.Chars() + 1, 16);
						vguid.Push(guid);
					}
				}
			}
			ichNew = ichMinTss + tri.ichLim;
		}
		else // no qtss here; advance past the missing string's character position.
		{
			ichNew = ichLimTss;
		}
		// See if we need the next Tss.
		if (ichNew >= ichLimTss && ichLim > ichLimTss)
		{
			// We need the next string
			Assert(ichNew == ichLimTss);  // we should have used this string up
			pts->StringAtIndex(++itss, &qtss);
			ichMinTss = ichLimTss; // This string begins where the old one ended.
			int cch; // Length of new current string.
			if (qtss)
				CheckHr(qtss->get_Length(&cch));
			else
				cch = 1; // embedded non-string boxes always occupy one char position.
			ichLimTss += cch;
		}
		else
		{
			// If we didn't move to the next string, we should have advanced the
			// character position. (If we did move to the next string, we might
			// NOT advance the character position, because the string might be empty.)
			Assert(ichNew > ich);
		}
	}
}


// This will probably never be called, as we want to use a smarter algorithm
// to adjust positions based on actual drawing width in dest coords.
void VwStringBox::DrawForeground(IVwGraphics *pvg, Rect rcSrc, Rect rcDst)
{
#ifdef _DEBUG
#ifdef _DEBUG_SHOW_BOX
	DebugDrawBorder(pvg, rcSrc, rcDst);
#endif
#endif

	if (!m_qlseg)
		ThrowHr(WarnHr(E_UNEXPECTED));
	rcSrc.Offset(-m_xsLeft, -m_ysTop);

	int dxdWidth;
	CheckHr(m_qlseg->DrawText(IchMin(), pvg, rcSrc, rcDst, &dxdWidth));
}

/*----------------------------------------------------------------------------------------------
	This differs from the overridden GetInvalidateRect by invalidating 10 points either side of
	the box's rectangle, as a kludgy way of dealing with overhang.
	ENHANCE: if the segment supports overhang info, use it to determine exactly what to
	invalidate.
	NOTE: in the current implementation of paragraph layout, this function is not used during
	typing.
----------------------------------------------------------------------------------------------*/
Rect VwStringBox::GetInvalidateRect()
{
	Rect rcRet = VwLeafBox::GetInvalidateRect();
	// For now we guess a max overhang of 10 points.
	int dxdMaxOverhang = MulDiv(10, Root()->DpiSrc().x, 72);
	rcRet.left -= dxdMaxOverhang;
	rcRet.right += dxdMaxOverhang;
	int dxHalfLine = rcRet.Height() >> 1;
	int dxQuarterLine = dxHalfLine >> 1;
	rcRet.Inflate(dxQuarterLine, dxHalfLine); // be generous with the amount we erase
	return rcRet;
}

/*----------------------------------------------------------------------------------------------
	This differs from the overridden GetInvalidateRect by invalidating some extra space to help
	with characters that draw outside their segments.
	ENHANCE: if the segment supports overhang info, use it to determine exactly what to
	invalidate.
----------------------------------------------------------------------------------------------*/
Rect VwParagraphBox::GetInvalidateRect()
{
	VwRootBox * prootb = Root();
	int dpiY = prootb->DpiSrc().y;
	Rect rcRet = SuperClass::GetInvalidateRect();
	BoxVec vbox;
	int ysTop, ysBottom;
	if (m_pboxFirst)
		GetALineTB(m_pboxFirst, vbox, dpiY, &ysTop, &ysBottom);
	else
	{
		ysTop = 0; // arbitrary average size line for a little overlap.
		ysBottom = 20;
	}
	// Inflate by half a line or 10 points, whichever is greater
	int dysLine = ysBottom - ysTop;
	int dys20pt = 20 * dpiY / 72;
	if (dysLine < dys20pt)
		dysLine = dys20pt;
	int dxsLine = dysLine * prootb->DpiSrc().x / dpiY;
	rcRet.Inflate(dxsLine / 2, dysLine / 2);
	OLECHAR boundaryMarkChar = VwParagraphBox::GetBoundaryMarkChar();
	if (boundaryMarkChar)
	{
		// we have a paragraph marker. Need to increase width.
		// We don't care if it is outside of the visible area
		if (!m_dxdBoundaryMark)
		{
			// need to calculate width of boundary mark
			IVwRootSitePtr qvrs;
			CheckHr(Root()->get_Site(&qvrs));
			IVwGraphicsPtr qvg;
			Rect rcSrcRoot, rcDestRoot;
			CheckHr(qvrs->GetGraphics(Root(), &qvg, &rcSrcRoot, &rcDestRoot));
			CheckHr(qvg->SetupGraphics(m_qzvps->Chrp()));
			int dydMark;
			CheckHr(qvg->GetTextExtent(1, &boundaryMarkChar, &m_dxdBoundaryMark, &dydMark));
			CheckHr(qvrs->ReleaseGraphics(Root(), qvg));
		}
		if (m_fParaRtl)
			rcRet.left -= m_dxdBoundaryMark; // boundary mark on the left side of text line for RTL para
		else
			rcRet.right += m_dxdBoundaryMark; // boundary mark on the right side of text line otherwise

	}
	return rcRet;
}

/*----------------------------------------------------------------------------------------------
	This defaults to the same as GetBoundsRect() unless the paragraph is enclosed by a
	VwTableCellBox, in which case we call GetBoundsRect() on the enclosing VwTableCellBox.
	This method is used in VwTextSelection::FindClosestEditableIP() since all cells on the
	same row should be "close enough" to being on the same line.
----------------------------------------------------------------------------------------------*/
Rect VwParagraphBox::GetOuterBoundsRect(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
	VwTableCellBox * pcellbox = dynamic_cast<VwTableCellBox *>(Container());
	if (pcellbox)
		return pcellbox->GetBoundsRect(pvg, rcSrcRoot, rcDstRoot);
	else
		return GetBoundsRect(pvg, rcSrcRoot, rcDstRoot);
}


#ifdef DEBUG
/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool VwParagraphBox::IsValid() const
{
	VwBox * pboxLast = NULL;
	bool fSkipBoxChecks = false; // set true when we find a knTruncated
	for (VwBox * pbox = m_pboxFirst; pbox; pbox = pbox->Next())
	{
		pboxLast = pbox;
		AssertPtr(pbox);
		if (pbox->Top() == knTruncated)
			fSkipBoxChecks = true;
		if (!fSkipBoxChecks)
		{
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
			if (psbox && !psbox->Segment())
				return false;
		}
	}
	return pboxLast == m_pboxLast;
}
#endif

/*----------------------------------------------------------------------------------------------
	Return the ascent.
----------------------------------------------------------------------------------------------*/
int VwStringBox::Ascent()
{
	return m_dysAscent;
}

int VwDropCapStringBox::Ascent()
{
	return SuperClass::Ascent() - m_dysSubFromAscent;
}
/*----------------------------------------------------------------------------------------------
	Get a selection.
----------------------------------------------------------------------------------------------*/
void VwStringBox::GetSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
	Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, VwSelection ** ppvwsel,
	SearchDirection sdir)
{
	int ich;
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(Container());
	Assert(pvpbox);
	bool fAssocPrevious;
	GetPointOffset(pvg, xd, yd, rcSrc, rcDst, &ich, &fAssocPrevious);
	// Find out where the selection is best located: fAssocPrevious may be wrong!
	Rect rdPrim;
	Rect rdPrim2;
	RECT rdSec;
	ComBool fSplit;
	pvpbox->LocOfSelection(pvg, ich, ich, fAssocPrevious, rcSrcRoot, rcDstRoot,
		&rdPrim, &rdSec, &fSplit, true);
	pvpbox->LocOfSelection(pvg, ich, ich, !fAssocPrevious, rcSrcRoot, rcDstRoot,
		&rdPrim2, &rdSec, &fSplit, true);
	if (rdPrim != rdPrim2)
	{
		// Compare the relative distances to the two possibilities (don't need actual figure,
		// so we can skip the SQRT step in the computations).
		int dx1 = xd - (rdPrim.left + rdPrim.right) / 2;
		int dy1 = yd - (rdPrim.top + rdPrim.bottom) / 2;
		int nDist1 = dx1 * dx1 + dy1 * dy1;
		int dx2 = xd - (rdPrim2.left + rdPrim2.right) / 2;
		int dy2 = yd - (rdPrim2.top + rdPrim2.bottom) / 2;
		int nDist2 = dx2 * dx2 + dy2 * dy2;
		if (nDist2 < nDist1)
			fAssocPrevious = !fAssocPrevious;
	}
	*ppvwsel = NewObj VwTextSelection(pvpbox, ich, ich, fAssocPrevious);
}

/*----------------------------------------------------------------------------------------------
	Make a selection and install it in the given root box (which is your own).

	@return True if the selection was set, false if selection failed due to the insertion point
		being read-only.  If forceReadOnly is true then the selection will not fail.
----------------------------------------------------------------------------------------------*/
MakeSelResult VwStringBox::MakeSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
		Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, bool forceReadOnly)
{
	VwSelectionPtr qvwsel;

	GetSelection(pvg, prootb, xd, yd, rcSrcRoot, rcDstRoot, rcSrc, rcDst, &qvwsel);

	// Selections must be in editable text.
	VwTextSelection * psel = dynamic_cast<VwTextSelection *>(qvwsel.Ptr());
	if (!psel || psel->IsEditable(psel->m_ichEnd, psel->m_pvpbox, psel->m_fAssocPrevious)
		|| forceReadOnly)
	{
		// Make selection in the ordinary way.
		prootb->SetSelection(qvwsel);
		prootb->ShowSelection();
		return kmsrMadeSel;
	}
	return kmsrNoSel;
}

/*----------------------------------------------------------------------------------------------
	Detect hot text and activate the link.
----------------------------------------------------------------------------------------------*/
void VwStringBox::DoHotLink(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
		Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst)
{
	ITsTextPropsPtr qttp;
	// Variables to receive info about selection context, and others. All vars must be first
	// because we use gotos.
	HVO hvoOwner;
	PropTag tag;
	int ichMinEditProp;
	int ichLimEditProp;
	// The view constructor, if any, responsible for the edited property.
	IVwViewConstructorPtr qvvc;
	int fragEdit; // The fragment identifier the VC needs for the edited property.
	VwAbstractNotifierPtr qanote; // The notifier for the property.
	int iprop; // the index of the property within that notifier.
	VwNoteProps vnp; // Notifier Property attributes.
	int itssProp; // index of edited string in list for the para box
	ITsStringPtr qtssProp;
	SmartBstr sbstr;
	VwSelectionPtr qvwsel;
	Rect rcBounds = GetBoundsRect(pvg, rcSrcRoot, rcDstRoot);
	POINT pt = {xd, yd};

	GetSelection(pvg, prootb, xd, yd, rcSrcRoot, rcDstRoot, rcSrc, rcDst, &qvwsel);
	if (!qvwsel)
		return;

	// If the user clicked outside our own box, he didn't actually click on any hot link,
	// so don't treat it as a hot click. This is especially useful in allowing the user
	// to click after a hot link at the very end of a paragraph.
	if (!rcBounds.Contains(pt))
		return;

	VwTextSelection * psel = dynamic_cast<VwTextSelection *>(qvwsel.Ptr());
	Assert(psel);

	// This uses notifier information to determine the substring we want to edit
	// and what property etc. it belongs to.
	int ichObj;
	ichObj = psel->m_ichAnchor;
	if (ichObj && psel->m_fAssocPrevious)
		ichObj--;
	if (psel->m_pvpbox->EditableSubstringAt(ichObj, ichObj + 1, false, &hvoOwner,
		&tag, &ichMinEditProp, &ichLimEditProp, &qvvc, &fragEdit,
		&qanote, &iprop, &vnp, &itssProp, &qtssProp) == kvepvNone)
		return;
	if (!qtssProp)
		return;
	int cchProp;
	CheckHr(qtssProp->get_Length(&cchProp));
	if (ichObj - ichMinEditProp > cchProp)
		// Most likely an insertion point in an empty string, so we can't find a text prop
		// that covers this character position, so we wound up with a higher level one.
		// In any case, we can't locate a particular character that might be hot.
		return;
	CheckHr(qtssProp->get_PropertiesAt(ichObj - ichMinEditProp, &qttp));
	// see if the selected character has the objdata property
	CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstr));
	// OK, activate the hot link
	if (sbstr.Length() > 0)
	{
		wchar chFirst = sbstr.Chars()[0];
		if (chFirst == kodtNameGuidHot || chFirst == kodtExternalPathName
			|| chFirst == kodtOwnNameGuidHot)
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
				else
					return;
			}
			Assert(qvvc);
			// Then perform the special action.
			CheckHr(qvvc->DoHotLinkAction(sbstr, prootb->GetDataAccess()));
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Given a click at a particular position in a string box, determine the character offset in
	the containing paragraph box, and whether
	the selection is most associated with the previous or following character.
	Return result in logical chars
----------------------------------------------------------------------------------------------*/
void VwStringBox::GetPointOffset(IVwGraphics * pvg, int xd, int yd,
	Rect rcSrc, Rect rcDst, int * pich, bool * pfAssocPrevious)
{
	rcSrc.Offset(-Left() - GapLeft(rcSrc.Width()), -Top() - GapTop(rcSrc.Height()));
	POINT pt;
	pt.x = xd;
	pt.y = yd;
	if (!m_qlseg.Ptr())
		ThrowHr(WarnHr(E_UNEXPECTED));
	ComBool fT;
	int ich;
	CheckHr(m_qlseg->PointToChar(IchMin(), pvg, rcSrc, rcDst, pt, &ich, &fT));
	*pfAssocPrevious = (bool)fT;
	VwTxtSrc * pts = dynamic_cast<VwParagraphBox *>(Container())->Source();
	*pich = pts->RenToLog(ich);
	// Now reverse the transformation. If we get a different result, the user clicked within
	// a substitution, in which case, since the character position we return is that of the
	// object character, we have to force the association to be with that character.
	int ichRen = pts->LogToRen(*pich);
	if (ichRen != ich)
		*pfAssocPrevious = false;
}

/*----------------------------------------------------------------------------------------------
	Given that we have a selection anchored at ichAnchor relative to pvpboxAnchor, and the user
	has shift-clicked or dragged to (xd, yd) relative to the current box,
	determine what the end offset of the selection ought now to be, relative to pvpbox.
	char indexes are logical.
----------------------------------------------------------------------------------------------*/
void VwStringBox::GetExtendedClickOffset(
	IVwGraphics * pvg,
	int xd, int yd,
	Rect rcSrc, Rect rcDst,
	VwParagraphBox * pvpboxAnchor, int ichAnchor,
	int * pich)
{
	rcSrc.Offset(-Left() - GapLeft(rcSrc.Width()), -Top() - GapTop(rcSrc.Height()));

	VwParagraphBox * pvpboxThis = dynamic_cast<VwParagraphBox *>(Container());
	if (pvpboxThis != pvpboxAnchor)
	{
		// The box clicked or dragged to is not part of the same string. Select as far as
		// possible in the appropriate direction.
		// ENHANCE JohnT: this logic is also applicable to clicking in a non-string box; figure
		// how to isolate it.
		// ENHANCE JohnT: this logic works only if the alternate string is in the same pile!
		// ENHANCE JohnT: This whole routine may go away if we start allowing larger selections.
		VwBox * pbox;
		for (pbox = pvpboxAnchor; pbox && pbox != pvpboxThis; pbox = pbox->NextRealBox())
			;
		if (pbox)
		{
			// clicked box logically follows anchor; select to logical end of paragraph box
			*pich = pvpboxAnchor->Source()->Cch();
			return;
		}
		else
		{
			// clicked box before anchor; select to logical start of string
			*pich = 0;
			return;
		}
	}
	// otherwise, we want an offset within this box's segment
	POINT pt;
	pt.x = xd;
	pt.y = yd;
	ComBool fAssocPrevious;
	// REVIEW JohnT(?): arguably, fAssocPrevious is relevant in the special case that we are
	// switching back to an IP. Should we try to use it?
	int ichren;
	m_qlseg->PointToChar(IchMin(), pvg, rcSrc, rcDst, pt, &ichren, &fAssocPrevious);
	*pich = dynamic_cast<VwParagraphBox *>(Container())->Source()->RenToLog(ichren);
}

#ifdef _DEBUG
/*----------------------------------------------------------------------------------------------
	Return a useful string for the debugger to show for a VwAbstractStringBox variable.
	This routine needs to be very careful not to cause any kind of protection violation,
	even if passed a badly corrupted string (e.g, *this points to uninitialized memory).
	If this routine fails badly, it will crash Visual Studio.
	To reduce the likelihood of such crashes, we initialize rgchDebugStr[0] in the contstructor
	and check it here.
----------------------------------------------------------------------------------------------*/
OLECHAR * VwStringBox::DebugStr()
{
#if WIN32
	if (!::_CrtIsValidPointer(this, isizeof(this), TRUE))
	{
		static OleStringLiteral str(L"A bad string box pointer");
		return str;
	}
#endif
	if (rgchDebugStr[0] != 0xffff)
	{
		static OleStringLiteral str(L"A corrupted string box");
		return str;
	}
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(Container());
	if (!pvpbox)
	{
		static OleStringLiteral str(L"Lost string box");
		return str;
	}
	if (!pvpbox->Source())
	{
		static OleStringLiteral str(L"String box w/o source");
		return str;
	}
	if (!m_qlseg)
	{
		static OleStringLiteral str(L"String box w/o segment");
		return str;
	}
	int dich;
	if (FAILED(m_qlseg->get_Lim(IchMin(), &dich)))
	{
		static OleStringLiteral str(L"String box--get_Lim failed");
		return str;
	}
	int ichLimOrig = IchMin() + dich;

	int ichLim = std::min(ichLimOrig, IchMin() + MAX_DEBUG_STR_LEN);
	pvpbox->Source()->Fetch(IchMin(),ichLim, rgchDebugStr + 1);
	return rgchDebugStr + 1;
}
#endif /* _DEBUG */

/*----------------------------------------------------------------------------------------------
	Get the box ready to be re-used in a new layout.
----------------------------------------------------------------------------------------------*/
void VwStringBox::Reset()
{
	m_qlseg = NULL;
}

StrUni VwStringBox::Description()
{
	int cch;
	CheckHr(m_qlseg->get_Lim(IchMin(), &cch));

	StrUni stuResult;
	OLECHAR * prgch;
	stuResult.SetSize(cch, &prgch);
	CheckHr(dynamic_cast<VwParagraphBox *>(Container())->Source()->
		Fetch(IchMin(), IchMin() + cch, prgch));
	return stuResult;
}


//:>********************************************************************************************
//:>	Class ParaBuilder
//:>********************************************************************************************

// These are the 'states' of the paragraph builder
typedef enum
{
	kzpbsAddUnknownBox,		// ready to process a new box, don't know anything about it
	kzpbsAddNonStringBox,	// try to add to line a box we know is not a string
	kzpbsAddWsRun,			// try to add to line a run of a string (or part of it)
	kzpbsFinalizeLine,		// line is full, clean up and make sure everything really fits
	kspbsFinalizeNoBack,	// line is full, we know backtracking is impossible so don't
	kzpbsOutputLine,		// line is OK, finish up adding to para and set up for next line
	kzpbsBackTrack,			// stuff we thought would fit does not, remove something
	kzpbsQuit,				// we've regenerated as much as we need to
	kzpbsAbort,				// We couldn't get anything on a line and must give up (probably zero width)
} ParaBuilderState; // Hungarian zpbs (the z prevents the initial p being taken as pointer)

/*----------------------------------------------------------------------------------------------
	This is a private class designed to be instantiated on-stack as part of the process
	of laying out a paragraph. It allows the algorithm to be broken into multiple methods
	without requiring variables for the state of the computation to be kept permanently
	in the paragraph or passed around as huge lists of arguments.
----------------------------------------------------------------------------------------------*/
class ParaBuilder
{
public:  // we can make anything public since the whole class is private to this file
	ParaBuilderState m_zpbs;	// state of the builder, controls next step in MainLoop()

	VwParagraphBox * m_pvpbox;	// the thing we are laying out.
	VwTxtSrc * m_pts;			// main text source for paragraph
	int m_itss;					// index of next item in m_pts to add
	IVwGraphics * m_pvg;
	ILgWritingSystemFactoryPtr m_qwsf;

	int m_dxAvailWidth;			// the available width in which we were asked to lay out

	int m_dxSurroundWidth;		// width of gaps either side of this para
	int m_dxInnerAvailWidth;	// width available to lay out para contents, inside padding

	int m_clines;		// number of lines in paragraph (so far)
	int m_clinesMax;	// Max number of lines allowed in para.

	int m_iline;		// line being relaid-out

	// The edge of the space occupied by the previous line. Previously called m_ysLineTop, this is typically the
	// 'top' of the current line; but it is more precise to consider it the top of the space available for
	// the current line, since line spacing may cause the actual top of the current line to be either lower
	// or even (with exact line spacing) higher. Moreover, when laying out an inverted paragraph, it is actually
	// the bottom of the space available for the current line.
	int m_ysEdgeOfSpaceForThisLine;
	// The offset from m_ysEdgeOfSpaceForThisLine at which the baseline of the previous line is found.
	// This is positive in the normal case when the previous baseline is above this line, negative when
	// laying out an inverted paragraph with the previous one below, and zero when laying out the first
	// line.
	int m_dysPrevBaselineOffset;
//	int m_yBaseLine;		// baseline of line being laid out
	int m_dypLineHeight;	// requested line height (distance between baselines)
	int m_dxLead;			// empty space on leading edge
	int m_dxTrail;			// empty space on trailing edge
	int m_dxWidthRemaining;	// available space for putting more boxes on line
	int m_dxInnerWidth;		// width of stuff on current line, including leading margin (but not trailing).
	int m_dxLeadSubseqLines;	// leading offset for lines other than first

	bool m_fExactLineHeight;
	int m_dyExactAscent;

	// Logically first box of the whole paragraph
	VwBox * m_pboxFirst;
	// Chain of embedded boxes we will add when we get to the right index in m_pts
	VwBox * m_pboxNextNonStr;
	// Logically first box on current line. Keep in sync with m_pboxEndLine so they always delimit the line being built.
	VwBox * m_pboxStartLine;
	// Logically last box on current line.  Keep in sync with m_pboxStartLine so they always delimit the line being built
	VwBox * m_pboxEndLine;
	// The real last box on the line, if we added a white space segment box;
	// Leave null unless we do so. See FinalizeLine() and BackTrack().
	VwStringBox * m_psboxLastReal;
	// End of previous line; new boxes linked here.
	VwBox * m_pboxEndPrevLine;

	// Logically first box of line following the one being relaid-out
	VwBox * m_pboxNextLine;

	// the first box that was originally in the para before we started.
	VwBox * m_pboxOriginalFirst;

	// Range of differing characters when doing a partial layout
	int m_ichMinDiff;
	int m_ichLimDiff;
	int m_cchLenDiff;
	// Set true when we regenerated one line more than expected, to handle cross-line
	// contextualization.
	bool m_fDidOneExtraLine;

	// chain of string boxes currently available for reuse
	VwStringBox * m_psboxReusable;

	// chain of VwMoveablePiles we may be able to reuse
	VwMoveablePileBox * m_pmpboxReusable;

	// chain of VwPictureBoxes we may be able to reuse
	VwPictureBox * m_ppboxReusable;

	// Highest level of direction embedding for line
	int m_nMaxDirectionDepth;

	// Handling of trailing white space: ktwshAll for top-level segments, alternates between
	// ktwshNoWs and ktwshOnlyWs for embedded segments.
	LgTrailingWsHandling m_twsh;
	// White-space state of the last box created.
	LgTrailingWsHandling m_twshLastBox;
	// paragraph direction
	bool m_fParaRtl;

	// Char range bounds we are currently trying to put in paragraph. -1 indicates we are ready
	// to take a new string (or box) from m_pts. These store an index relative to rendered
	// characters (where this is different from logical, i.e., with a mapped text source).
	int m_ichMinString;
	int m_ichLimString;

	// Range of characters for last successfully made segment
	int m_ichMinLastSeg;
	int m_ichLimLastSeg;

	VwPropertyStore * m_pzvps;			// properties of the para we are constructing.
	VwPropertyStore * m_pzvpsString;	// properties of the string we are processing

	IRenderEnginePtr m_qre;		// rendering engine for ich last passed to
	LgCharRenderProps m_chrp;	// props of ich last passed to

	IVwJustifierPtr m_qvjus;		// justification agent

	LgLineBreak m_lbNormalBreak;  // hyphenating or word breaking

	// Used only if we have to change box order for bidi:
	// the boxes of the line, initially in logical order, then changed to physical.
	BoxVec m_vbox;

	// How each segment finished; stack must be equal in size to number of boxes in current
	// line.
	Vector<LgEndSegmentType> m_vest;

	ILgSegment * m_psegPrevContext;

	int m_dyTagAbove;		// extra height to insert above lines for opening tags
	int m_dyTagBelow;		// extra height to insert below lines for closing tags
	bool m_fSemiTagging;	// displaying styles on text itself but not the tags
	IVwOverlay * m_pxvo;

	// Picture box we are trying to fit into line
	VwPictureBox * m_pboxpic;
	// Char index for picture box we have made but not yet used (or -1 if none)
	int m_ichSavePic;

	// Minimum ascents and descents for bullets and numbers
	int m_dyMinDescent;
	int m_dyMinAscent;

	// Layout resolutions
	int m_dxpInch;
	int m_dypInch;

	// Vector of 'moveable piles' that have been created but not yet inserted into the
	// paragraph. Will be added (as extra lines) when we next finalize a line.
	Vector<VwMoveablePileBox *> m_vmpbox;

	int m_fIsNested; // 0 = unknown, 1 = nested, 2 = not nested.

	// Answer true if any container of the paragraph we're laying out is a paragraph.
	// Since this doesn't change we compute it the first time (if any) that it is needed.
	bool IsNested()
	{
		// See if any container is a paragraph
		if (m_fIsNested == 0) // not yet computed
		{
			m_fIsNested = 2; // default, not nested, use this if loop ends without finding paragraph.
			for (VwGroupBox * pgbox = m_pvpbox->Container(); pgbox; pgbox = pgbox->Container())
			{
				if (pgbox->IsParagraphBox())
				{
					m_fIsNested = 1;
					break;
				}
			}
		}
		return m_fIsNested == 1;
	}

	// Handle any initialization related to bullets and numbers, and return true if there
	// are any.
	bool InitBulletsAndNumbers(int dxsFirstIndent)
	{
		COLORREF clrUnder;
		int unt;
		StrUni stuBulNum = m_pvpbox->GetBulNumString(m_pvg, &clrUnder, &unt);
		if (!stuBulNum.Length())
			return false;

		int dxsBulNum, dysBulNum;
		CheckHr(m_pvg->GetTextExtent(stuBulNum.Length(),
			const_cast<OLECHAR *>(stuBulNum.Chars()), &dxsBulNum, &dysBulNum));
		// If it does not end with a space insert an arbitrary three pixels to give
		// minimal separation.
		OLECHAR chLast = stuBulNum.Chars()[stuBulNum.Length() - 1];
		if (chLast != ' ')
			dxsBulNum += 3 * m_dxpInch / 72;
		CheckHr(m_pvg->get_FontAscent(&m_dyMinAscent));
		m_dyMinDescent = dysBulNum - m_dyMinAscent;
		if (dxsFirstIndent < 0 && -dxsFirstIndent > dxsBulNum)
		{
			// Number fits in hanging indent: align rest of first line with main para
			m_dxWidthRemaining = m_dxInnerAvailWidth;
			m_dxLead = m_pvpbox->GapLeading(m_dxpInch);
		}
		else
		{
			m_dxWidthRemaining -= dxsBulNum;
			m_dxLead += dxsBulNum;
			if (m_dxWidthRemaining < 0)
			{
				// Number on a line by itself
				m_dxWidthRemaining = m_dxInnerAvailWidth;
				Forward(m_ysEdgeOfSpaceForThisLine, dysBulNum);
				m_dyMinAscent = m_dyMinDescent = 0;
				m_dxLead = m_pvpbox->GapLeading(m_dxpInch);
			}
		}
		return true;
	}

	// Alter val by distance in the 'forward' direction. By default this is downwards, that is, distance is added.
	// In an inverted paragraph it is subtracted to move up.
	virtual void Forward(int & val, int distance)
	{
		val += distance;
	}

	// Handle special initialization if we have drop caps. Actually creates the drop caps
	// segment, if this paragraph needs it (and then returns true, though this is currently
	// not used). May also adjust indents to wrap around DC on previous one-line DC para.
	bool InitDropCaps()
	{
		ITsStringPtr qtssFirst;
		if (m_pts->CStrings() == 0)
			return false; // not supposed to be possible, but has occurred (see LTB-704) so be defensive.
		m_pts->StringAtIndex(0, &qtssFirst);
		if (!qtssFirst)
			return false; // para starts with picture or something-- can't make a Drop Cap.
		ITsTextPropsPtr qttpFirst;
		TsRunInfo tri;
		CheckHr(qtssFirst->FetchRunInfo(0, &tri, &qttpFirst));
		// Check the first run to see whether it specifies drop caps.
		VwPropertyStorePtr qzvpsFirstRun;
		int tal = m_pvpbox->Style()->ParaAlign();
		bool fWantDropCapHere = tri.ichLim != 0 && tal != ktalTrailing; // don't want a drop cap here if para is empty.
		if (fWantDropCapHere)
		{
			m_pzvps->ComputedPropertiesForTtp(qttpFirst, &qzvpsFirstRun);
			fWantDropCapHere = qzvpsFirstRun->DropCaps();
		}
		if (!fWantDropCapHere)
		{
			// This paragraph doesn't have its own drop cap. However, if the previous
			// paragraph has a DC and only has one line, we need to adjust the indent
			// of this one.
			VwParagraphBox * pvpboxPrev =
				dynamic_cast<VwParagraphBox *>(m_pvpbox->Container()->BoxBefore(m_pvpbox));
			if (!pvpboxPrev)
				return false; // no previous paragraph, don't adjust this for DC
			if (pvpboxPrev->ExtraHeightIfNotFollowedByPara(true) <= 0)
				return false; // paragraph has no space to wrap into.
			if (pvpboxPrev->Style()->ParaAlign() == ktalTrailing)
				return false; // Don't worry about it if the DC is in a trailing paragraph since it won't look right at all
			// Previous para is one-line DC, adjust first line indent of this by width of DC.
			// This should not be null if the two heights are different!
			VwDropCapStringBox * pdcsbDropCap = dynamic_cast<VwDropCapStringBox *>(pvpboxPrev->FirstBox());
			// Enhance TE Team (JohnT): do we want to ADD the width of the DC to the first line
			// indent, as implemented here, or do we want to change the FLI to the MAX of its normal
			// value and what it needs to be to avoid the Drop Cap?
			int delta = pdcsbDropCap->Width();
			if (tal != ktalTrailing && (m_pvpbox->RightToLeft() || tal != ktalRight))
				m_dxLead += delta;
			m_dxWidthRemaining -= delta;
			return false; //
		}

		// Make a segment up to as many characters as have these same properties.
		int cchDrop = m_pts->RenToLog(tri.ichLim);
		ILgSegmentPtr qlseg;
		int dxWidth;
		int dichLimSeg;
		LgEndSegmentType est;

		AddUnknownItem();
		GetRendererForChar(0);
		GetSegment(0, cchDrop, cchDrop,
			false, // OK to end the segment at cchDrop even though no space follows.
			true, // segment logically starts line
			m_dxWidthRemaining,
			m_lbNormalBreak, // If entire drop cap run doesn't fit, try for 'nice' break
			klbClipBreak, // If no nice break is possible, make any kind of break
			&qlseg,
			&dichLimSeg, &dxWidth, &est,
			NULL);
		if (!qlseg)
		{
			// If we can't make a drop cap segment for some reason, fall back to
			// normal processing.
			Warn("drop cap processing failed");
			return false;
		}

		VwDropCapStringBox * psbox;
		psbox = NewObj VwDropCapStringBox(m_pzvpsString, 0);
		psbox->SetSegment(qlseg);
		psbox->DoLayout(m_pvg, m_dxInnerAvailWidth, m_dxWidthRemaining);
		m_twshLastBox = ktwshAll;
		// Suppress first line indent.
		m_dxLead = m_pvpbox->GapLeading(m_dxpInch);
		// If, as usual, we fit all the drop caps text, but there is more text in the paragraph,
		// we need to change kestNoMore to kestOkayBreak, so the system knows it can break the
		// line and go on in the unlikely event that only the drop cap fits on the line.
		if (est == kestNoMore && cchDrop < m_pts->CchRen())
			est = kestOkayBreak;
		AddBoxToLine(psbox, ktwshAll, est);
		m_ichMinString = dichLimSeg;

		// finagle ascent of psbox to align it relative to the first line.
		// Move it down exactly one line to align with the second line of text
		// (unless something forces extra line spacing).
		psbox->m_dysSubFromAscent = MulDiv(m_pzvps->AdjustedLineHeight(qzvpsFirstRun), m_dypInch, kdzmpInch);
		m_zpbs = kzpbsAddWsRun; // We're all set to add the rest of the first string.
		if (m_ichMinString == m_ichLimString)
		{
			// Except if the only thing in the paragraph was the drop cap!
			// Then we set up (as in case kestNoMore: in AddWsRun) to indicate
			// no more text.
			m_zpbs = kzpbsAddUnknownBox;
			m_ichMinString = -1; // indicates no more text to add.
		}

		m_dxWidthRemaining = m_dxInnerAvailWidth - psbox->Width();

		return true;
	}

	///*------------------------------------------------------------------------------------------
	//	Determine the baseline for a string of "default paragraph charcaters" for this
	//	paragraph in the given writing system.
	// Basically this sets its baseline to what we would get for the
	// default font and size for this writing system for the paragraph.

	//	@param ws	true if we want to do a complete relayout, false if we
	//------------------------------------------------------------------------------------------*/
	//int DefaultParagraphBaseline(int ws)
	//{
	//	ITsPropsBldrPtr qtpb; // This is used to make a TsTextProps that just has the WS.
	//	qtpb.CreateInstance(CLSID_TsPropsBldr);
	//	qtpb->SetIntPropValues(ktptWs, ktpvDefault, ws);
	//	ITsTextPropsPtr qttpSimplified; // Just has ws of first run.
	//	qtpb->GetTextProps(&qttpSimplified);
	//	LgCharRenderProps chrp;
	//	m_pzvps->get_ChrpFor(qttpSimplified, &chrp);
	//	CheckHr(m_pvg->SetupGraphics(&chrp));
	//	int dysAscent;
	//	m_pvg->get_FontAscent(&dysAscent);
	//	int dysGapTop = psbox->GapTop(m_dypInch);
	//	return dysGapTop + dysAscent;
	//}

	// Adjust the indent of a line that needs to wrap around a drop cap.
	void WrapAroundDropCap(VwBox * pboxFirst)
	{
		m_dxLead += pboxFirst->Width();
		m_dxWidthRemaining -= pboxFirst->Width();
	}


	/*------------------------------------------------------------------------------------------
		Initialize the ParaBuilder.

		@param fComplete			true if we want to do a complete relayout, false if we
										want to do it line-by-line
		@param pboxStartLayout		the box to start redoing the layout; guaranteed to be
										the first box on the line in the old layout;
										if NULL, start at the beginning
		@param cLinesToSkip			number of lines we are not redoing at the beginning of
										the paragraph
		@param dyTop				top of first line to regenerate
		@param dyPrevDescent		max descent of previous line
		@param ichStart				character to start on within the TextSource; -1 if we
										start at the beginning of the current string
	------------------------------------------------------------------------------------------*/
	void Initialize(IVwGraphics * pvg, VwPropertyStore * pzvps,
		int dxAvailWidth, VwParagraphBox * pvpbox, VwTxtSrc * pts, bool fComplete,
		VwBox * pboxStartLayout1, int cLinesToSkip1, int dyTop1, int dyPrevDescent1,
		int ichMinDiff, int ichLimDiff, int cchLenDiff,
		int ichStart1)
	{
		AssertPtr(pvpbox);
		int cLinesToSkip = cLinesToSkip1;
		VwBox * pboxStartLayout = pboxStartLayout1;
		int ichStart = ichStart1;
		int dyTop = dyTop1;
		int dyPrevDescent = dyPrevDescent1;
		if (ichStart == -1 || pboxStartLayout == NULL || cLinesToSkip == 0)
		{
			// Any of these indicates we need to do a complete layout. In case they aren't passed
			// consistently...there's at least one special case where ichStart is -1 even when we
			// wanted a partial layout, because there's a non-string box at the start of the line...
			// force everything to full-layout mode.
			pboxStartLayout = NULL;
			cLinesToSkip = 0;
			ichStart = -1;
			dyTop = pvpbox->InitialBoundary();
			dyPrevDescent = 0;
		}
		Assert(!(fComplete && pboxStartLayout));
		m_pvpbox = pvpbox;
		m_pboxOriginalFirst = m_pvpbox->FirstBox();
		// Need to set the first box of the paragraph to null. This is needed since we can call
		// EditableSubStringAt() while laying out the paragraph (possibly trying to get an
		// ORC view constructor) which loops through the boxes inside of m_pvpbox. If the
		// boxes have been deleted, but the first box is still pointing to the deleted box,
		// bad things happen. :). Also guards against any possible re-entrancy.
		m_pvpbox->_SetFirstBox(NULL);
		m_pvpbox->m_pboxLast = NULL; // just for consistency.

		if (cLinesToSkip == 0)
		{
			// We're laying out the whole thing anyway; drop cap handling (at least)
			// works best if we treat it as a complete layout.
			fComplete = true;
			pboxStartLayout = NULL;
			ichStart = -1;
		}

		m_fIsNested = 0;
		m_pboxpic = NULL;
		m_ichSavePic = -1;
		m_clinesMax = pvpbox->MaxLines();
		m_pvg = pvg;
		m_pzvps = pzvps;
		m_dxAvailWidth = dxAvailWidth;
		m_pboxFirst = NULL; // none inserted yet

		m_fParaRtl = RightToLeftPara(pzvps, pts);
		m_pvpbox->SetRightToLeft(m_fParaRtl);
		m_nMaxDirectionDepth = 0;

		CheckHr(pvg->get_XUnitsPerInch(&m_dxpInch));
		CheckHr(pvg->get_YUnitsPerInch(&m_dypInch));
		int dxpSurroundWidth = pvpbox->SurroundWidth(m_dxpInch);
		m_dxSurroundWidth = dxpSurroundWidth;
		m_dxInnerAvailWidth = m_dxAvailWidth - m_dxSurroundWidth;

		// If we somehow have no space at all to lay it out, take one point's worth anyway.
		// Things start failing if available width is negative before there is anything
		// on the line.
		if (m_dxInnerAvailWidth <= 0)
		{
			Warn("laying out para with no avail width");
			m_dxInnerAvailWidth = 1;
		}
		m_clines = cLinesToSkip;
		m_ysEdgeOfSpaceForThisLine = dyTop;
		if (m_clines == 0)
			AdjustEdgeOfSpaceForInitialGap();

		int dxsFirstIndent = MulDiv(m_pzvps->FirstIndent(), m_dxpInch, kdzmpInch);
		m_dxLead = pvpbox->GapLeading(m_dxpInch);
		m_dxWidthRemaining = m_dxInnerAvailWidth;
		if (m_clines == 0)
		{
			m_dxLead += dxsFirstIndent;
			m_dxWidthRemaining -= dxsFirstIndent;
		}
		else if (m_clines == 1 && m_pboxOriginalFirst &&
			m_pboxOriginalFirst->IsDropCapBox() &&
			pboxStartLayout != m_pboxOriginalFirst->NextOrLazy())
		{
			// We're reusing a first line that has a drop cap, and restarting
			// in the second line, and there is some text on the first line other
			// than the drop cap. Fiddle the leading indent to wrap the line
			// around the drop cap.
			WrapAroundDropCap(m_pboxOriginalFirst);
		}
		m_dxTrail = pvpbox->GapTrailing(m_dxpInch);
		if (m_fParaRtl)
			pvpbox->SetRightEdge(m_dxAvailWidth - pvpbox->GapLeading(m_dxpInch) +
				std::max(0, -dxsFirstIndent));
		else
			// Irrelevant, but might as well set it to something vaguely meaningful.
			pvpbox->SetRightEdge(m_dxAvailWidth - pvpbox->GapRight(m_dxpInch));

		m_pts = pts;

		m_dxInnerWidth = 0;
		m_pboxStartLine = NULL; // nothing in current line yet
		m_vest.Clear();
		AssertEndSegTypeVectorSize();
		m_dysPrevBaselineOffset = dyPrevDescent;

		m_ichMinString = ichStart;
		// Initially set m_itss to the string that contains our start character; later in init we adjust this
		// to the end of the range of contiguous strings including that one.
		m_itss = 0;
		if (ichStart >= 0)
		{
			ITsStringPtr qtssT;
			VwPropertyStorePtr qzvpsT;
			int ichMinPropT, ichLimPropT;
			m_pts->StringFromIch(m_pts->RenToLog(m_ichMinString), false, &qtssT, &ichMinPropT, &ichLimPropT, &qzvpsT, &m_itss);
		}

		m_ichMinDiff = ichMinDiff;
		m_ichLimDiff = ichLimDiff;
		m_cchLenDiff = cchLenDiff;
		if (m_dxWidthRemaining <= 0)
			m_dxWidthRemaining = 1; // even first line needs some space

		m_dyMinAscent = m_dyMinDescent = 0;
		bool fGotBulletsOrNumbers = false;
		// Handle various special cases for the first line.
		if (m_clines == 0)
		{
			// Adjust width remaining for any bullets or numbers.
			fGotBulletsOrNumbers = InitBulletsAndNumbers(dxsFirstIndent);
		}


		// If we're doing a full layout (or, which is virtually equivalent,
		// starting with the first line), we want to be in the 'unknown' state
		// expected by InitDropCaps().
		if (ichStart == -1 || m_clines == 0)
			m_zpbs = kzpbsAddUnknownBox;
		else
		{
			ITsStringPtr qtss;
			m_pts->StringAtIndex(m_itss, &qtss);
			Assert(qtss);
			// Get ready to add a string.
			VwPropertyStorePtr qzvps;
			m_pts->StyleAtIndex(m_itss, &qzvps);
			m_pzvpsString = qzvps;

			// Extract the contiguous run of characters we will make string boxes for.
			// It starts at the current index and continues until we have a non-string
			// item in m_pts, or have included all the strings. Update m_itss to point
			// to the first non-included item.
			m_itss++;
			while (m_itss < m_pts->CStrings())
			{
				m_pts->StringAtIndex(m_itss, &qtss);
				if (!qtss)
					break;
				m_itss++;
			}
			m_ichLimString = m_pts->LogToRen(m_pts->IchStartString(m_itss));

			m_zpbs = kzpbsAddWsRun;
		}

		m_psboxReusable = NULL;
		m_pmpboxReusable = NULL;
		m_ppboxReusable = NULL;
		m_psboxLastReal = NULL;
		m_pboxEndPrevLine = NULL;
		m_pboxEndLine = NULL;
		m_psegPrevContext = NULL;

		ISilDataAccessPtr qsda;
		if (pvpbox && pvpbox->Root())
			qsda = pvpbox->Root()->GetDataAccess();
		if (!qsda)
			ThrowHr(WarnHr(E_FAIL));
		CheckHr(qsda->get_WritingSystemFactory(&m_qwsf));
		Assert(m_qwsf);
		if (m_pts)
			m_pts->SetWritingSystemFactory(m_qwsf);

		// ENHANCE JohnT (v2): set to klbHyphenBreak if para style allows hyphenation:
		// Normally we want to find word breaks if any, but for the last line we have room for,
		// we want all the letters we can fit.
		m_lbNormalBreak = m_clinesMax > 1 ? klbGoodBreak : klbLetterBreak;

		m_dxLeadSubseqLines = pvpbox->GapLeading(m_dxpInch);
		m_dypLineHeight = MulDiv(m_pzvps->LineHeight(), m_dypInch, kdzmpInch);
		m_fExactLineHeight = m_pzvps->ExactLineHeight();
		if (fComplete)
			pvpbox->SetExactAscent(-1);
		int dympExactAscent = pvpbox->ExactAscent();
		m_dyExactAscent = dympExactAscent < 0 ? dympExactAscent :
			MulDiv(dympExactAscent, m_dypInch, kdzmpInch);
		m_fDidOneExtraLine = false;

		//VwStringBox * psbox = dynamic_cast<VwStringBox *>(pboxStartLayout);

		DiscardLines(fComplete, true, pboxStartLayout);

		m_pxvo = m_pvpbox->Source()->Overlay();
		IVwRootSitePtr qvrs;
		m_fSemiTagging = false;
		if (m_pvpbox->Root())
		{
			CheckHr(m_pvpbox->Root()->get_Site(&qvrs));
			ComBool f;
			if (qvrs)
			{
				CheckHr(qvrs->get_SemiTagging(m_pvpbox->Root(), &f));
				m_fSemiTagging = (bool)f;
			}
		}
		m_pvpbox->SetSemiTagging(m_fSemiTagging);
		pvpbox->ComputeTagHeights(pvg, m_pxvo, m_dypInch, m_dyTagAbove, m_dyTagBelow);

		m_qvjus = NULL;
		if (m_clines == 0 && !fGotBulletsOrNumbers)
			InitDropCaps();
	}
	// Adjust m_ysEdgeOfSpaceForThisLine (which is at the top or bottom of the whole box)
	// by the appropriate gap in the appropriate direction. The default adds the top gap.
	// For an inverted box subtract the bottom gap.
	virtual void AdjustEdgeOfSpaceForInitialGap()
	{
		m_ysEdgeOfSpaceForThisLine += m_pvpbox->GapTop(m_dypInch);
	}

	/*------------------------------------------------------------------------------------------
		Discard some or all layout boxes.

		@param fComplete		if true, discard the rest of the paragraph;
									if false, just do one more line
		@param fInit			true if we are initializing the ParaBuilder;
									false if we are in the process of doing the layout
		@param pboxStart		box to begin discarding, guaranteed to be first on the line;
									NULL if we want to start at the beginning of the para
	------------------------------------------------------------------------------------------*/
	void DiscardLines(bool fComplete, bool fInit, VwBox * pboxStart)
	{
		// Determine the first box the paragraph that we want to keep.
		Vector<VwBox *> vpboxOneLine;
		if (fComplete)
		{
			m_pboxNextLine = NULL;
		}
		else if (pboxStart)
		{
			m_pboxNextLine = m_pvpbox->GetALine(pboxStart, vpboxOneLine);
		}
		else
		{
			m_pboxNextLine = m_pvpbox->GetALine(m_pboxOriginalFirst, vpboxOneLine);
		}

		// When discarding, put string boxes in reuse list, discard other layout boxes,
		// and put embedded boxes in a list starting at m_pboxNextNonStr.
		VwBox * pboxPrev = NULL; // previous box in sequence before pboxStart, if we're looping over those.
		VwBox * pboxLastNonString = NULL; // last box placed into m_pboxNextNonStr linked list.
		// JohnT: The use of these two variables is subtle and non-obvious.
		// What one might think of as the 'typical' case is that we start pbox at
		// pboxStart, which is the first box we want to 'toss' (delete, recycle, whatever);
		// or at the beginning of the paragraph, and are starting to toss there.
		// However, there is a special case when fInit is true (we're starting up the
		// whole layout algorithm), but not starting at the beginning of the paragraph.
		// (pboxStart is non-NULL). In this case, we start the loop at the beginning
		// of the paragraph rather than at pboxStart, because we need to have a valid
		// pboxPrev when we process pboxStart, so that we can break the 'Next' chain
		// at that point. To achieve this, we set fStartTossing to false, so that
		// we don't toss boxes until we get to pboxStart.
		bool fStartTossing = (pboxStart == NULL);
		VwBox * pbox;
		if (fInit)
		{
			pbox = m_pboxOriginalFirst;
			m_pboxNextNonStr = NULL; // in case we don't find any
		}
		else
		{
			pbox = pboxStart;
			// In case we discard more lines before we've inserted all the non-string
			// boxes we previously found, we MUST not lose any of them.
			if (m_pboxNextNonStr)
				pboxLastNonString = m_pboxNextNonStr->EndOfChain();
		}

		VwBox * pboxNext;
		// Loop until we run out of boxes or reach m_pboxNextLine.
		for (; pbox; pbox = pboxNext)
		{
			pboxNext = pbox->Next(); // in case pbox gets reset or deleted

			if (pbox == m_pboxNextLine)
			{
				// Hit the beginning of the next line: stop.
				Assert(m_pboxNextLine);
				Assert(fStartTossing); // We should have found pboxStart!
				break;
			}

			if (!fStartTossing && pbox == pboxStart)
			{
				fStartTossing = true;
				// If we have a pboxPrev at this point, it is because we started the loop
				// at m_pboxFirst in order to find pboxPrev, so we can break the box
				// link from the last box we're keeping to the first one we're tossing.
				if (pboxPrev)
					pboxPrev->SetNext(NULL);
				pboxPrev = NULL;
			}

			if (!fStartTossing)
			{
				// We're processing boxes before pboxStart, mainly in order to set pboxPrev
				// for use in the previous block of code, also, the last iteration
				// where fStartTossing is false establishes m_pboxEndPrevLine.
				if (fInit)
				{
					// Copy into the ParaBuilder's chain.
					if (pboxPrev)
					{
						Assert(pboxPrev->Next() == pbox);
						pboxPrev->SetNext(pbox); // just in case
					}
					else
					{
						m_pboxFirst = pbox;
					}
					m_pboxEndPrevLine = pbox;
				}
				pboxPrev = pbox;
				if (pbox->IsStringBox())
				{
					VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
					m_psegPrevContext = psbox->Segment();
				}
			}
			else
			{
				// 'Normal' case, tossing pbox.
				if (pbox->IsBoxFromTsString())
				{
					// Simplest not to reuse drop caps string box...might get put elsewhere in para.
					if (pbox->IsStringBox() && !pbox->IsDropCapBox())
					{
						// reuse it, but discard contents
						VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
						psbox->Reset();
						psbox->SetNext(m_psboxReusable);
						m_psboxReusable = psbox;
					}
					else if (pbox->IsMoveablePile())
					{
						// Hope to reuse entirely (keep contents)
						VwMoveablePileBox * pmpbox = dynamic_cast<VwMoveablePileBox *>(pbox);
						pmpbox->SetNext(m_pmpboxReusable);
						m_pmpboxReusable = pmpbox;
					}
					else if (pbox->IsAnchor())
					{
						// Need to do something about making sure we continue the layout process
						// at least as far as the box it is anchored to.
						// The following is not especially efficient, nor adequate if the
						// mpbox has moved to another paragraph, but will work for now.
						m_pboxNextLine = NULL; // Toss everything remaining.
						delete pbox;
					}
					else
					{
						VwPictureBox * ppbox = dynamic_cast<VwPictureBox *>(pbox);
						if (!ppbox)
						{
							// eg, a drop cap box: discard it; Nothing should be
							// pointing to it
							delete pbox;
						}
						else
						{
							// Try to reuse the picture box
							ppbox->SetNext(m_ppboxReusable);
							m_ppboxReusable = ppbox;
						}
					}
				}
				else
				{
					// Embedded box that cannot be regenerated using the strings in the
					// text source: keep it and use it when we hit the place where we
					// need it; see AddNonStringBox.
					if (pboxLastNonString)
						pboxLastNonString->SetNext(pbox);
					else
						m_pboxNextNonStr = pbox;
					pboxLastNonString = pbox;
				}
			}
		}
		if (pboxPrev)
		{
			pboxPrev->SetNext(NULL); // clean up any following text box from previous layout.
		}
		if (pboxLastNonString)
			pboxLastNonString->SetNext(NULL);
	}

	/*------------------------------------------------------------------------------------------
		Discard one more line, if any.
	------------------------------------------------------------------------------------------*/
	void DiscardOneMoreLine()
	{
		if (m_pboxNextLine)
			DiscardLines(false, false, m_pboxNextLine);
	}

	/*------------------------------------------------------------------------------------------
		Destructor
	------------------------------------------------------------------------------------------*/
	virtual ~ParaBuilder()
	{
		Assert(m_vmpbox.Size() == 0);
		// Delete any discarded string boxes that have not been reused
		while (m_psboxReusable)
		{
			VwStringBox * psboxNext = static_cast<VwStringBox *>(m_psboxReusable->Next());
			delete m_psboxReusable;
			m_psboxReusable = psboxNext;
		}
		// Delete any leftover picture boxes that have not been reused
		while (m_ppboxReusable)
		{
			VwPictureBox * ppboxNext = static_cast<VwPictureBox *>(m_ppboxReusable->Next());
			// It's possible that a picture selection may be pointing to this box, so
			// make sure that situation is handled.
			VwRootBox * prootb = m_pvpbox->Root();
			if (prootb)
				prootb->FixSelections(m_ppboxReusable, NULL);
			delete m_ppboxReusable;
			m_ppboxReusable = ppboxNext;
		}
		// Delete any leftover moveable piles (and their contents and notifiers).
		while (m_pmpboxReusable)
		{
			VwMoveablePileBox * pmpboxNext = static_cast<VwMoveablePileBox *>(m_pmpboxReusable->NextOrLazy());
			m_pmpboxReusable->DeleteAndCleanupAndDeleteNotifiers();
			m_pmpboxReusable = pmpboxNext;
		}
		if (m_pboxpic)
			delete m_pboxpic;

		// m_qvjus is smart pointer; don't need to delete it
	}

	/*------------------------------------------------------------------------------------------
		Determine if the overall paragraph direction should be right-to-left.
	------------------------------------------------------------------------------------------*/
	bool RightToLeftPara(VwPropertyStore * pzvps, VwTxtSrc * pts)
	{
		ComBool f;
		CheckHr(pzvps->get_RightToLeft(&f));
		return (bool)f;
	}

	/*------------------------------------------------------------------------------------------
		Return the top direction depth of the paragraph.
	------------------------------------------------------------------------------------------*/
	int TopDepth()
	{
		if (m_fParaRtl)
			return 1;
		else
			return 0;
	}

	/*------------------------------------------------------------------------------------------
		Change the end-of-line status of the specified box to the specified value,
		and adjust m_dxWidthRemaining accordingly.
	------------------------------------------------------------------------------------------*/
	void BoxEndsLine(VwStringBox * psbox, bool f)
	{
		int dxWidthOld = psbox->Width();
		CheckHr(psbox->Segment()->put_EndLine(psbox->IchMin(), m_pvg, f));
		// Segment may have changed size, recompute
		psbox->DoLayout(m_pvg, m_dxInnerAvailWidth, m_dxWidthRemaining);
		m_dxWidthRemaining += dxWidthOld - psbox->Width();
	}

	/*------------------------------------------------------------------------------------------
		If there is a string box at the end of the line, change its end-of-line
		to the specified value, and adjust m_dxWidthRemaining accordingly.
	------------------------------------------------------------------------------------------*/
	void LastBoxEndsLine(bool f)
	{
		if (m_pboxEndLine)
		{
			if (m_pboxEndLine->IsStringBox())
				BoxEndsLine(dynamic_cast<VwStringBox *>(m_pboxEndLine), f);
		}
	}

	/*------------------------------------------------------------------------------------------
		Add a box to the line currently being built, and update the space available.
	------------------------------------------------------------------------------------------*/
	bool AddBoxToLine(VwBox * pbox, LgTrailingWsHandling twsh, LgEndSegmentType est)
	{
		if (twsh == ktwshOnlyWs)
			m_psboxLastReal = dynamic_cast<VwStringBox *>(m_pboxEndLine);
		else
			m_psboxLastReal = NULL;

		if (m_pboxEndLine)
		{
			m_pboxEndLine->SetNext(pbox);
		}
		else
		{
			if (m_pboxEndPrevLine)
				m_pboxEndPrevLine->SetNext(pbox);
			else
				m_pboxFirst = pbox;
			m_pboxStartLine = pbox;
		}
		m_pboxEndLine = pbox;
		m_dxWidthRemaining -= pbox->Width();
		pbox->Container(m_pvpbox);
		m_vest.Push(est);
		AssertEndSegTypeVectorSize();
		return false;
	}

	/*------------------------------------------------------------------------------------------
		Check that the size of the LgEndSegmentType vector matches the number of boxes on
		the line.
	------------------------------------------------------------------------------------------*/
	void AssertEndSegTypeVectorSize()
	{
#ifdef _DEBUG
		Assert((m_pboxStartLine == NULL) == (m_vest.Size() == 0));
		int cbox = 0;
		VwBox * pbox;
		for (pbox = m_pboxStartLine; pbox && pbox != m_pboxEndLine; pbox = pbox->Next())
			cbox++;
		if (m_pboxStartLine && m_pboxEndLine)
			cbox++;
		Assert(m_vest.Size() == cbox);
		Assert(m_vbox.Size() == 0 || m_vbox.Size() == m_vest.Size());
#endif // _DEBUG
	}

	/*------------------------------------------------------------------------------------------
		Get the renderer to use for the specified run. Also sets m_chrp.
		ich is in rendered chars.
	------------------------------------------------------------------------------------------*/
	void GetRendererForChar(int ich)
	{
		int ichMin;
		int ichLim;
		CheckHr(m_pts->GetCharProps(ich, &m_chrp, &ichMin, &ichLim));
		m_pts->SetWritingSystemFactory(m_qwsf);		// Just to be safe.
		CheckHr(m_pvg->SetupGraphics(&m_chrp));
		CheckHr(m_qwsf->get_Renderer(m_chrp.ws, m_pvg, &m_qre));
		Assert(m_qre.Ptr());
	}

	/*------------------------------------------------------------------------------------------
		Make a string box for the given segment.
		If there is an old string box we can reuse, do so; otherwise, allocate a new one.
		In any case lay out the new box.
	------------------------------------------------------------------------------------------*/
	void MakeStringBox(ILgSegment * plseg, VwStringBox ** ppsbox, int ichBase,
		LgTrailingWsHandling twsh)
	{
		Assert(plseg);
		if (m_psboxReusable)
		{
			*ppsbox = m_psboxReusable;
			m_psboxReusable = static_cast<VwStringBox *>(m_psboxReusable->Next());
			(*ppsbox)->SetNext(NULL);
			(*ppsbox)->_SetIchMin(ichBase);
			(*ppsbox)->m_qzvps = m_pzvpsString; // in case reused from different span.
		}
		else
		{
			*ppsbox = NewObj VwStringBox(m_pzvpsString, ichBase);
		}
		(*ppsbox)->SetSegment(plseg);
		(*ppsbox)->DoLayout(m_pvg, m_dxInnerAvailWidth, m_dxWidthRemaining);
//		(*ppsbox)->SetTrWsHandling(twsh);
		// Remember the white-space state for the most recently created box.
		m_twshLastBox = twsh;
	}

	/*------------------------------------------------------------------------------------------
		Find the box before the argument, in the current line we are building.
	------------------------------------------------------------------------------------------*/
	VwBox * BoxBeforeInCurrentLine(VwBox * pbox)
	{
		if (pbox == m_pboxStartLine)
			return NULL;
		VwBox * pboxPrev;
		for (pboxPrev = m_pboxStartLine;
			pboxPrev->Next() != pbox;
			pboxPrev = pboxPrev->Next())
		{
			Assert(pboxPrev->Next());
		}
		return pboxPrev;
	}

	/*------------------------------------------------------------------------------------------
		Fix the start and end flags on segments in text boxes from pboxStartLine
		to pboxEndLine (inclusive). This happens when we have reordered things due to
		bidirectionality. All flags should become false except for the left end of pboxdLeft
		and the right end of pboxRight.
		Adjust m_dxWidthRemaining by any change in box width.
	------------------------------------------------------------------------------------------*/
	void FixStartAndEndFlags(VwBox * pboxdLeft, VwBox * pboxRight)
	{
		//	ReverseUpstreamBoxes may have reordered things, and the new order is in
		//	the box vector. If we haven't reordered things, fill in the vector with the
		//	boxes in the original order.
		if (m_vbox.Size() == 0)
		{
			for (VwBox* pbox = m_pboxStartLine; pbox != m_pboxEndLine; pbox = pbox->Next())
				m_vbox.Push(pbox);
			m_vbox.Push(m_pboxEndLine);
		}

		Assert(m_vbox.Size() == m_vest.Size());
		for (int ibox = 0; ibox < m_vbox.Size(); ibox++)
		{
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(m_vbox[ibox]);
			if (psbox)
			{
				int dxWidth = psbox->Width();
				ComBool fRtoL;
				bool fEndLeft = (psbox == pboxdLeft);
				bool fEndRight = (psbox == pboxRight);
				bool fEndLine = fEndRight; // OK if LtoR seg or same
				bool fStartLine = fEndLeft;
				ILgSegmentPtr qlseg = psbox->Segment();
				AssertPtr(qlseg.Ptr());
				if (fEndLeft != fEndRight)
				{
					//if (psbox == m_pboxEndLine && m_psboxLastReal && psbox != m_psboxLastReal)
					// trailing white-space segment (Note that m_twsh is not necessarily
					// = to ktwshOnlyWs, because we may have tried to get something else
					// and failed.)
					//	fRtoL = false;
					//else

					if (qlseg)
						CheckHr(qlseg->get_RightToLeft(psbox->IchMin(), &fRtoL));
					if (fRtoL)
					{
						fEndLine = fEndLeft;
						fStartLine = fEndRight;
					}
				}

				// OPTIMIZE: possibly first retrieve old state, and update width only
				// if it changed--might be more efficient for some segment designs
				if (qlseg)
				{
					CheckHr(qlseg->put_EndLine(psbox->IchMin(), m_pvg, fEndLine));
					CheckHr(qlseg->put_StartLine(psbox->IchMin(), m_pvg,
						fStartLine));
				}
				psbox->DoLayout(m_pvg, m_dxInnerAvailWidth, m_dxWidthRemaining);
				// width remaining is increased by the old width, decreased by the new
				m_dxWidthRemaining += dxWidth - psbox->Width();
			}
		}
	}

	// Get the direction depth of a box and whether it is weak
	void GetDirectionDepth(VwBox * pbox, int * pnDepth, ComBool * pf)
	{
		VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
		if (psbox)
		{
			int ichMin = psbox->IchMin();
			CheckHr(psbox->Segment()->get_DirectionDepth(ichMin, pnDepth, pf));
			if (*pf && ichMin > 0)
			{
				// We think it's weak...but don't treat it so if it is
				// surrounded by strong direction markers.
				int cch;
				CheckHr(psbox->Segment()->get_Lim(ichMin, &cch));
				if (ichMin + cch < m_pts->CchRen())
				{
					OLECHAR chmarker = m_fParaRtl ? 0x200f : 0x200e; // RLM : LRM
					OLECHAR ch;
					CheckHr(m_pts->Fetch(ichMin - 1, ichMin, &ch));
					if (ch == chmarker)
					{
						CheckHr(m_pts->Fetch(ichMin + cch, ichMin + cch + 1, &ch));
						if (ch == chmarker)
						{
							*pf = false;
							*pnDepth = TopDepth(); // the override chars we're looking for are to force it to paragraph direction.
						}
					}
				}
			}
		}
		else
		{
			// non-string boxes (see SetDirectionDepth) are considered strong at
			// top depth
			*pnDepth = TopDepth();
			*pf = false;
		}
	}

	/*------------------------------------------------------------------------------------------
		Find in m_vbox any sequences of boxes whose direction depth is nDepth or more.
		Reverse the boxes in each such sequence.
	------------------------------------------------------------------------------------------*/
	void ReverseUpstreamBoxes(int nDepth, VwBox * pboxLogicalLast)
	{
		for (int iboxMin = 0; iboxMin < m_vbox.Size(); ++iboxMin)
		{
			int nDepthBox;
			ComBool f;
			GetDirectionDepth(m_vbox[iboxMin], &nDepthBox, &f);
			int iboxLim = iboxMin + 1; // limit of sequence to reverse
			if (nDepthBox >= nDepth)
			{
				// We got the first box of sequence to reverse!
				// Now find the index after the last to reverse.
				while (iboxLim < m_vbox.Size())
				{
					GetDirectionDepth(m_vbox[iboxLim], &nDepthBox, &f);
					if (nDepthBox < nDepth)
						break;
					//if (psbox == pboxLogicalLast && m_twshLastBox == ktwshOnlyWs)
					// trailing white-space segment--don't reorder
					//	break;
					iboxLim++;
				}

				// reverse the lists
				for (int ibox = 0; ibox < (iboxLim - iboxMin) / 2; ibox++)
				{
					VwBox * pboxTemp = m_vbox[iboxMin + ibox];
					m_vbox[iboxMin + ibox] = m_vbox[iboxLim - 1 - ibox];
					m_vbox[iboxLim - 1 - ibox] = pboxTemp;
				}

				// Advance start of search to box after this range.
				// Note that iboxLim is already checked, if there are that many.
				// Note that iboxLim is always at least one more than iboxMin, so we
				// do progress.
				iboxMin = iboxLim - 1;
			}
		}
	}

	/*------------------------------------------------------------------------------------------
		Determine the directionality for any boxes that have weak direction, ie,
		white-space-only boxes.
	------------------------------------------------------------------------------------------*/
	void SetWeakDirections(int nTopDepth)
	{
		// Figure out which boxes have weak directionality and set the direction depths of
		// all the boxes.
		Vector<int> vnDepth;
		Vector<bool> vfWeak;
		int ibox;
		for (ibox = 0; ibox < m_vbox.Size(); ++ibox)
		{
			if (m_vbox[ibox]->IsStringBox())
			{
				VwStringBox * psbox = dynamic_cast<VwStringBox *>(m_vbox[ibox]);
				int nDepth;
				ComBool fWeak;
				CheckHr(psbox->Segment()->get_DirectionDepth(psbox->IchMin(),
					&nDepth, &fWeak));
				vnDepth.Push(nDepth);
				vfWeak.Push(fWeak);
			}
			else
			{
				vnDepth.Push(nTopDepth);
				vfWeak.Push(false);
			}
		}

		for (ibox = 0; ibox < m_vbox.Size(); ++ibox)
		{
			if (!vfWeak[ibox])
				continue;

			VwStringBox * psbox = dynamic_cast<VwStringBox *>(m_vbox[ibox]);
			// Set the depth of the weak box to the shallowest of the adjacent boxes.
			int nDepthPrev = 100;
			for (int ibox2 = ibox; --ibox2 >= 0; )
			{
				if (!vfWeak[ibox2])
				{
					nDepthPrev = std::min(nDepthPrev, vnDepth[ibox2]);
					break;
				}
			}
			if (nDepthPrev == 100) // leading edge of para line
				nDepthPrev = nTopDepth;
			int nDepthNext = 100;
			for (int ibox2 = ibox + 1; ibox2 < m_vbox.Size(); ++ibox2)
			{
				if (!vfWeak[ibox2])
				{
					nDepthNext = std::min(nDepthNext, vnDepth[ibox2]);
					break;
				}
			}
			if (nDepthNext == 100) // trailing edge of para line
				nDepthNext = nTopDepth;

			int nNewDepth = std::min(nDepthPrev, nDepthNext);
			CheckHr(psbox->Segment()->SetDirectionDepth(psbox->IchMin(), nNewDepth));
		}
	}

	/*------------------------------------------------------------------------------------------
		Add something to the paragraph; decide what kind of thing it is.
		If it is text, set things up to do the text-processing routines; otherwise,
		do the non-text routines.
	------------------------------------------------------------------------------------------*/
	void AddUnknownItem()
	{
		// Before adding an unknown item, we should have processed all characters of any
		// previous string box.
		Assert(m_ichMinString < 0);
		// To add a box to this line, there has to be room with the old last box
		// NOT in the end-line-true state. So switch it now. If it does not fit
		// at all like that, go straight to finalizing the line.
		// Also do that if there is nothing more to add.
		SmartBstr sbstr;
		LastBoxEndsLine(false);
		if (m_dxWidthRemaining < 0 || (m_itss >= m_pts->CStrings()))
		{
			// No more room on line, or nothing more to add.
			m_zpbs = kzpbsFinalizeLine;
			LastBoxEndsLine(true);
			return;
		}

		ITsStringPtr qtss;
		m_pts->StringAtIndex(m_itss, &qtss);
		LgEndSegmentType est;
		if (qtss)
		{
			// Add a string.
			VwPropertyStorePtr qzvps;
			m_pts->StyleAtIndex(m_itss, &qzvps);
			m_pzvpsString = qzvps;

			// Extract the contiguous run of characters we will make string boxes for.
			// It starts at the current index and continues until we have a non-string
			// item in m_pts, or have included all the strings. Update m_itss to point
			// to the first non-included item.
			m_ichMinString = m_pts->LogToRen(m_pts->IchStartString(m_itss));
			m_itss++;
			while (m_itss < m_pts->CStrings())
			{
				m_pts->StringAtIndex(m_itss, &qtss);
				if (!qtss)
					break;
				m_itss++;
			}
			m_ichLimString = m_pts->LogToRen(m_pts->IchStartString(m_itss));
			if (m_ichMinString == m_ichLimString)
			{
				// Handle case of empty string:
				// just make a single zero-width string box, add to line, continue
				// with next unknown box
				GetRendererForChar(m_ichMinString);
				int nSegDirDepth = m_chrp.nDirDepth;
				if (nSegDirDepth < TopDepth())
					nSegDirDepth += 2;
				if (nSegDirDepth > m_nMaxDirectionDepth)
					m_nMaxDirectionDepth = nSegDirDepth;
				ILgSegmentPtr qlseg;
				int nDum1, nDum2;
				m_twsh = ktwshAll;
				GetSegment(m_ichMinString, m_ichMinString, m_ichMinString,
					false,
					m_pboxStartLine == NULL,	// start of line if nothing there already
					m_dxInnerAvailWidth,
					klbClipBreak, klbClipBreak,	// accept any break for empty seg
					&qlseg,
					&nDum1, &nDum2, &est,
					NULL);

				VwStringBox * psbox;
				MakeStringBox(qlseg, &psbox, m_ichMinString, ktwshAll);
				// The avail width passed here is pretty arbitary; string box does not
				// use it.
				psbox->DoLayout(m_pvg, m_dxInnerAvailWidth, m_dxWidthRemaining);
				AddBoxToLine(psbox, ktwshAll, est);
				// OK, we have completely processed this string item,
				// ready to do the next in same mode.
				// Note: this assumes that an empty string never has non-zero width
				// (unless there's an applicable margin, padding or border),
				// nor does appending an empty box change the end-of-line property
				// of the previous box.
				int dxpInch;
				CheckHr(m_pvg->get_XUnitsPerInch(&dxpInch));
				Assert(psbox->Width() == psbox->SurroundWidth(dxpInch));
				Assert(m_dxInnerAvailWidth >= 0);
				// In case nothing more gets added and we move on to FinalizeLine,
				// this variable needs to be zero so we don't try to look for
				// char line break properties on the next-to-last character.
				m_ichLimLastSeg = 0;
				m_ichMinString = -1; // shows current string entirely handled.
				return;
			} // if empty string

			// If non-empty string, switch to kpbAddWsRun and continue.
			m_zpbs = kzpbsAddWsRun;
			return;
		} // string box
		else
		{
			// Otherwise, we can save a switch execution by going ahead with the non-string box.
			m_zpbs = kzpbsAddUnknownBox;
			m_ichMinString = -1; // indicates all string-related vars are invalid
			AddNonStringBox();
			// non-string boxes are considered to have depth TopDepth().
			if (TopDepth() > m_nMaxDirectionDepth)
					m_nMaxDirectionDepth = TopDepth();
		}
	}

	// This is the one thing we do differently in Relayout, which is therefore overridden
	// in ParaRebuilder.
	virtual void LayoutNonString()
	{
		m_pboxNextNonStr->DoLayout(m_pvg, m_dxInnerAvailWidth, m_dxWidthRemaining);
	}

	/*------------------------------------------------------------------------------------------
		Preconditions:
		m_pts[m_itss] indicates a non-string.
		There should be a corresponding non-string box in the list pointed to by
		m_pboxNextNonStr. Any string box at the current end of line is in the end-of-line
		false state.
	------------------------------------------------------------------------------------------*/
	void AddNonStringBox()
	{
		// If there is not a non-string box handy, we may need to undo more of the layout.
		while (!m_pboxNextNonStr && m_pboxNextLine)
			DiscardOneMoreLine();

		Assert(m_ichMinString < 0);
		Assert(m_pboxNextNonStr && !dynamic_cast<VwStringBox *>(m_pboxNextNonStr));

		// A non-string box needs to be laid out. It is allowed to use all the space,
		// not just what is on this line; if need be we will put it on the next line.
		LayoutNonString();
		// If the next box will fit in the available space, add to line,
		// advance to next box, switch to kpbAddUnknownBox.
		// If there is nothing on the line, put this box there anyway.
		if (m_pboxNextNonStr->Width() < m_dxWidthRemaining || !m_pboxStartLine)
		{
			m_zpbs = kzpbsAddUnknownBox;
			m_itss++;
			AddBoxToLine(m_pboxNextNonStr, ktwshAll, kestOkayBreak);
			m_pboxNextNonStr = m_pboxNextNonStr->Next();
			return;
		}
		// Otherwise, line is full: finalize it.
		m_zpbs = kzpbsFinalizeLine;
	}

	// Return the View constructor applicable to the specified character.
	// Note that this does NOT give a ref count on the notifier or the ViewConstructor.
	void VcForIchLog(int ich, IVwViewConstructor ** ppvvc, VwNotifier ** ppnote,
		int * piprop, int * pws, int * pichMinEditProp)
	{
		HVO hvo;
		PropTag tag;
		int ichLimEditProp;
		IVwViewConstructorPtr qvvcEdit;
		int fragEdit;
		VwAbstractNotifierPtr qanote;
		int itssProp;
		ITsStringPtr qtssPropFirst;
		VwNoteProps vnp;
		m_pvpbox->EditableSubstringAt(ich, ich + 1, false, &hvo, &tag,
			pichMinEditProp, &ichLimEditProp, ppvvc, &fragEdit, &qanote, piprop, &vnp,
			&itssProp, &qtssPropFirst);
		VwNotifier * pnote = dynamic_cast<VwNotifier *>(qanote.Ptr());
		*ppnote = pnote;
		if (vnp == kvnpStringAltMember || vnp == kvnpUnicodeProp)
			*pws = fragEdit;
		else
			*pws = 0;
		if (!*ppvvc)
		{
			// The common case, the property was added normally using AddStringProp.
			// Find the higher level view constructor
			VwBox * pboxFirstProp;
			int itssFirstProp;
			int iprop;
			Assert(qanote->Parent());
			if (pnote)
			{
				qanote->Parent()->GetPropForSubNotifier(pnote, &pboxFirstProp,
					&itssFirstProp, &tag, &iprop);
				*ppvvc = qanote->Parent()->Constructors()[iprop];
			}
		}

	}

	/*------------------------------------------------------------------------------------------
		See if there is an available picture box we can reuse for the specified paragraph box
		and object data
	------------------------------------------------------------------------------------------*/
	VwPictureBox * FindPictureBoxToReuse(VwParagraphBox * pvpbox, SmartBstr sbstrObjData)
	{
		VwBox * pboxPrev = NULL;
		for (VwBox * pbox = m_ppboxReusable; pbox; pbox = pbox->Next())
		{
			VwPictureBox * ppbox = dynamic_cast<VwPictureBox *>(pbox);
			if (pvpbox == ppbox->Paragraph() && sbstrObjData == ppbox->ObjectData())
			{
				// Found picture that is owned by the paragraph and contains same data.
				if (pboxPrev == NULL)
					m_ppboxReusable = dynamic_cast<VwPictureBox *>(ppbox->Next());
				else
					pboxPrev->SetNext(ppbox->Next());
				ppbox->SetNext(NULL);
				return ppbox;
			}
			pboxPrev = pbox;
		}
		return NULL;
	}

	/*------------------------------------------------------------------------------------------
		See if there is an available MP box we can reuse for the specified guid, VC, and parent
	------------------------------------------------------------------------------------------*/
	VwMoveablePileBox * FindMpToReuse(GUID * pguid, IVwViewConstructor * pvc,
		VwNotifier * pnote)
	{
		VwBox * pboxPrev = NULL;
		for (VwBox * pbox = m_pmpboxReusable; pbox; pbox = pbox->NextOrLazy())
		{
			VwMoveablePileBox * pmpbox = dynamic_cast<VwMoveablePileBox *>(pbox);
			if (*pguid == *(pmpbox->Guid()) && pvc == pmpbox->Vc() && pnote == pmpbox->Notifier())
			{
				if (pboxPrev == NULL)
					m_pmpboxReusable = dynamic_cast<VwMoveablePileBox *>(pmpbox->NextOrLazy());
				else
					pboxPrev->SetNext(pmpbox->NextOrLazy());
				pmpbox->SetNext(NULL);
				return pmpbox;
			}
			pboxPrev = pbox;
		}
		return NULL;
	}

	/*------------------------------------------------------------------------------------------
		Deal with the process of making a VwMoveablePileBox for a specified GUID. Push the
		result into m_vmpbox. Todo: reuse from previous layout.
	------------------------------------------------------------------------------------------*/
	void GetMoveablePile(const OLECHAR * pchGuid)
	{
		// Locate the view constructor that will interpret the Guid.
		IVwViewConstructorPtr qvc;
		VwNotifier * pnote;
		int iprop;
		int ws;
		int ichMinEditProp;
		int ichLog = m_pvpbox->Source()->RenToLog(m_ichMinString);
		VcForIchLog(ichLog, &qvc, &pnote, &iprop, &ws, &ichMinEditProp);
		if (!qvc)
			return; // No way to make one without a VC.

		// Get the Guid itself.
		GUID guid;
		::memcpy(&guid, pchGuid, isizeof(guid));

		// See if we can reuse a box for this VC and GUID from a previous layout.
		VwMoveablePileBox * pmpbox = FindMpToReuse(&guid, qvc, pnote);

		if (!pmpbox)
		{
			// Didn't find one, go ahead and make it.
			HVO hvoEmbedded;
			CheckHr(qvc->GetIdFromGuid(m_pvpbox->Root()->GetDataAccess(), &guid, &hvoEmbedded));
			if (!hvoEmbedded)
				return; // Guid doesn't represent a current object, ignore it.

			// Set up a VwEnv and make a moveable pile to represent the object.
			pmpbox = NewObj VwMoveablePileBox(m_pvpbox->Style());
			pmpbox->Container(m_pvpbox);
			VwEnvPtr qvwenv;
			qvwenv.Attach(m_pvpbox->Root()->MakeEnv());
			qvwenv->InitEmbedded(m_pvg, pmpbox);
			qvwenv->OpenObject(hvoEmbedded);
			CheckHr(qvc->DisplayEmbeddedObject(qvwenv, hvoEmbedded));
			qvwenv->CloseObject();
			pmpbox->SetVc(qvc);
			pmpbox->SetGuid(&guid);
			pmpbox->SetOriginalContainer(m_pvpbox);
			if (pmpbox->FirstBox() == NULL)
			{
				// DisplayEmbeddedObject didn't put anything in the box. Discard it.
				delete pmpbox;
				qvc.Detach(); // VcForIchLog did not add ref to VwViewConstructor
				return;
			}
			qvwenv->SetParentOfTopNotifiers(pnote);
			pmpbox->SetNotifier(pnote);
		}

		pmpbox->SetParentPropIndex(iprop);
		pmpbox->SetAlternative(ws);
		pmpbox->SetCharIndex(ichLog - ichMinEditProp);
		// Save the resulting box to be inserted into the paragraph later.
		m_vmpbox.Push(pmpbox);
		// Insert an anchor box to help us keep track of where it 'belongs'.
		VwAnchorBox * pabox = NewObj VwAnchorBox(m_pvpbox->Style(), pmpbox);
		LgEndSegmentType est;
		if (m_vest.Size() == 0)
			est = kestOkayBreak; // start of line break is OK, I guess.
		else
			est = *(m_vest.Top()); // OK break if previous segment was.
		AddBoxToLine(pabox, ktwshAll, est);

		qvc.Detach(); // VcForIchLog did not add ref to VwViewConstructor
	}

	/*------------------------------------------------------------------------------------------
		Check for an object character. If found, insert the appropriate thing into the
		box stream.

		At the start, m_spbs is kzpbsAddWsRun. Leave it so unless we detect end of WS run or
		end of input; in those cases, switch to kzpbsAddUnknownBox or kzpbsFinalizeLine
		as appropriate.
	------------------------------------------------------------------------------------------*/
	bool CheckForObject()
	{
		if (m_ichLimString <= m_ichMinString)
			return false;
		OLECHAR ch;
		ITsTextPropsPtr qttp;
		m_pts->CharAndPropsAt(m_pts->RenToLog(m_ichMinString), &ch, &qttp);
		Assert(qttp);
		if (ch != 0xfffc || !qttp) // object replacement character
			return false;

		// It should have this property. If not, ignore it.
		SmartBstr sbstr;
		CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstr));
		if (sbstr.Length() >= 1)
		{
			const OLECHAR * pchData = sbstr.Chars();
			// typically the data is binary
			// Note: Mac (and any big-endian) version will need to flip bytes for binary data.
			switch(*pchData)
			{
			case kodtPictOddHot:
			case kodtPictEvenHot:
				return HandlePicture(sbstr);
			case kodtGuidMoveableObjDisp:
				GetMoveablePile(sbstr.Chars() + 1);
				// Enhance FWR-2366 JohnT: we could also display an anchor symbol as part of this line.
				m_ichMinString++; // Whether successful or not, we used up this character.
				return true;
			case kodtOwnNameGuidHot:
			case kodtNameGuidHot:
				{
					// These are not handled here; normally the text source expands them.
					// However, if a segment happens to end for some other reason at the start of
					// the ORC (likely, because the ORC can expand to a lot of text),
					// this method may get called anyway for that ich.
					// We want to answer false ('there is no ORC here') because for para layout
					// purposes there isn't: there is just more text (which the text source translates
					// the ORC into). Otherwise, things go wrong wrapping paragraphs with links.
					int ichMin, ichLim, isbt, irun = 0;
					VwPropertyStorePtr qzvps;
					m_pts->GetCharPropInfo(m_ichMinString, &ichMin, &ichLim, &isbt, &irun, &qttp, &qzvps);
					SmartBstr sbstrObjData;
					CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstrObjData));

					return (sbstrObjData.Length() >= 1 && HandlePicture(sbstrObjData));
				}
			default:
				break;
			}
		}
		// Default (both for empty ktptObjData and for unrecognized obj data subtype)
		// is to skip the character. May be data from some later version
		// of FW, or just corrupted. In any case continue as best we can.
		Warn("Unexpected object data type");
		m_ichMinString++;
		return true;
	}

	/*------------------------------------------------------------------------------------------
		Trys handling a picture ORC at the current location in the paragraph and with the
		object data as specified in the given string.
	------------------------------------------------------------------------------------------*/
	bool HandlePicture(SmartBstr sbstrObjData)
	{
		int cchData = sbstrObjData.Length();
		const OLECHAR * pchData = sbstrObjData.Chars();
		// typically the data is binary
		// Note: Mac (and any big-endian) version will need to flip bytes for
		// binary data.
		int cbData = (cchData - 1) * 2;
		if (*pchData != kodtPictOddHot && *pchData != kodtPictEvenHot)
			return false;
		if (*pchData == kodtPictOddHot)
			cbData--;

		// A picture suitable for OleLoadPicture
		if (m_ichMinString == m_ichSavePic)
		{
			// We tried to put this picture on a previous line and failed.
			// Try again.
			m_ichSavePic = -1; // Nothing saved
		}
		else
		{
			// Make a new picture box
			if (m_pboxpic)
			{
				delete m_pboxpic;
				m_ichSavePic = -1;
				m_pboxpic = NULL;
			}

			m_pboxpic = FindPictureBoxToReuse(m_pvpbox, sbstrObjData);
			if (!m_pboxpic)
			{
				// No picture box found that can be reused. Create a new one.
				IPicturePtr qpic;
				try
				{
					// 9 = 1 for the picture type plus 8 for the object guid
					byte * pbData = (byte *)(const_cast<OLECHAR *>(pchData + 9));
					CheckHr(m_pvg->MakePicture(pbData, cbData, &qpic));
				}
				catch (Throwable& thr)
				{
					m_ichMinString++; // skip char we can't use
					WarnHr(thr.Error());
					return true; // Just don't insert any picture.
				}
				// Give the picture box the reset properties to prevent it
				// inheriting margins etc. from the paragraph.
				VwPropertyStorePtr qzvpsReset;
				CheckHr(m_pzvps->ComputedPropertiesForEmbedding(&qzvpsReset));
				m_pboxpic = NewObj VwPictureBox(qzvpsReset, qpic, 0, 0, m_pvpbox, sbstrObjData);
			}

			m_pboxpic->DoLayout(m_pvg, m_dxInnerAvailWidth, m_dxWidthRemaining);
		}
		if (m_pboxpic->Width() <= m_dxWidthRemaining)
		{
			AddBoxToLine(m_pboxpic, ktwshAll, kestOkayBreak);
			m_pboxpic = NULL; // So it does not get deleted by destructor
			m_ichMinString++;
		}
		else
		{
			// We will try to finalize the line. We can pretty easily get
			// into some messy backtracking here, because a single picture
			// could exceed the whole view width. If so, output it anyway.
			if (m_dxWidthRemaining == m_dxInnerAvailWidth)
			{
				// It's the first thing on the line, output it anyway.
				AddBoxToLine(m_pboxpic, ktwshAll, kestOkayBreak);
				m_pboxpic = NULL; // So it does not get deleted by destructor
				m_ichMinString++;
				// certainly nothing more will fit, go ahead and finalize.
				m_zpbs = kspbsFinalizeNoBack;
			}
			else
			{
				// Otherwise try to finalize the line without the picture.
				m_zpbs = kzpbsFinalizeLine;
				// But save the picture box for next try
				m_ichSavePic = m_ichMinString;
				return true;
			}
		}
		// If we get here, we inserted a picture.
		// Now, if the picture is the last thing in the paragraph, or
		// immediately followed by another picture, we need to insert an
		// empty segment.
		bool fNeedEmpty = false;
		if (m_ichMinString == m_pts->CchRen())
			fNeedEmpty = true;
		else
		{
			ITsTextPropsPtr qttp;
			OLECHAR ch;
			m_pts->CharAndPropsAt(m_pts->RenToLog(m_ichMinString), &ch, &qttp);
			fNeedEmpty = (ch == 0xfffc); // object replacement character
		}
		if (fNeedEmpty)
		{
			// Empty string.
			ILgSegmentPtr qlseg;
			LgEndSegmentType est;
			int dxWidth;
			int dichLimSeg;
			GetSegment(m_ichMinString, m_ichMinString, m_ichMinString,
				false, // end is a valid break point
				m_pboxStartLine == NULL,
				m_dxWidthRemaining,
				klbClipBreak, klbClipBreak,
				&qlseg,
				&dichLimSeg, &dxWidth, &est,
				NULL);
			// seg must not have negative # characters!
			Assert(qlseg && dichLimSeg >= 0);

			// Make the segment into a box and add to the current line.
			// (If there is an available string box, reuse it--especially if
			// the main one).
			VwStringBox * psbox;
			MakeStringBox(qlseg, &psbox, m_ichMinString, ktwshAll);
			// Whatever the segment says we will consider this a valid place
			// to line break.
			est = kestNoMore;
			AddBoxToLine(psbox, ktwshAll, est);
			// If that was the last character in the ows run, indicate that
			// the ows run is over.
			if (m_ichMinString == m_ichLimString)
			{
				m_zpbs = kzpbsAddUnknownBox; // Need to get a new TsString next
				m_ichMinString = -1;
			}
		}
		return true;
	}

	void StartNewString()
	{
		m_zpbs = kzpbsAddUnknownBox;
		m_ichMinString = -1; // indicates no more text to add.
	}

	/*------------------------------------------------------------------------------------------
		Add a run of text to the paragraph.
		Preconditions:
		m_ichMinString, m_ichLimString indicate the limits of the text to be added, and
		that range is not empty.
		If the last box already on the line is a string one, it is in the end-of-line false
		state.
	------------------------------------------------------------------------------------------*/
	void AddWsRun()
	{
		// Make a segment holding as much as possible in a single writing system.
		GetRendererForChar(m_ichMinString);
		int nSegDirDepth = m_chrp.nDirDepth;
		if (nSegDirDepth < TopDepth())
			nSegDirDepth += 2;
		if (nSegDirDepth == TopDepth())
			m_twsh = ktwshAll;
		else
			// Toggle between visible and white-space segments.
			// If we were previously building ktwshAll segments, we want to start by looking for
			// a leading all-white-space segment.
			m_twsh = (m_twsh == ktwshOnlyWs) ? ktwshNoWs : ktwshOnlyWs;

		ILgSegmentPtr qlseg;
		int dxWidth;
		int dichLimSeg;
		LgEndSegmentType est;

		GetSegment(m_ichMinString, m_ichLimString, m_ichLimString,
			false, // end is a valid break point
			m_pboxStartLine == NULL,
			m_dxWidthRemaining,
			m_lbNormalBreak,
			(m_pboxStartLine == NULL ? klbClipBreak : m_lbNormalBreak),
			&qlseg,
			&dichLimSeg, &dxWidth, &est,
			m_psegPrevContext);

		if (!qlseg && m_twsh != ktwshAll)
		{
			// If we are separating out visible text and white-space, and we didn't get
			// any of what we were expecting, try the other.
			m_twsh = (m_twsh == ktwshNoWs) ? ktwshOnlyWs : ktwshNoWs;
			GetSegment(m_ichMinString, m_ichLimString, m_ichLimString,
				false, // end is a valid break point
				m_pboxStartLine == NULL,
				m_dxWidthRemaining,
				m_lbNormalBreak,
				(m_pboxStartLine == NULL ? klbClipBreak : m_lbNormalBreak),
				&qlseg,
				&dichLimSeg, &dxWidth, &est,
				m_psegPrevContext);
		}
		Assert(!qlseg || dichLimSeg >= 0); // seg must not have negative # characters!

		// If we could not make a seg that fit, finalize and output the line
		if (!qlseg)
		{
			if (CheckForObject())
				return;

			OLECHAR ch;
			ITsTextPropsPtr qttp;
			m_pts->CharAndPropsAt(m_pts->RenToLog(m_ichMinString), &ch, &qttp);
			if (ch == kchwHardLineBreak)
				m_ichMinString++; // skip hard line break

			if (!m_pboxStartLine)
			{
				// If nothing fits and we have nothing on the line, probably we were
				// so unlucky as to find a break character (e.g., newline) exactly
				// at the end of the previous line. We need to stop the paragraph
				// layout here.
				m_zpbs = kzpbsAbort;
				return;
			}
			LastBoxEndsLine(true);
			m_zpbs = kzpbsFinalizeLine;
			return;
		}
		// If we got an empty segment check whether it is because of an initial object
		// character. If it is, discard the empty segment--except at the start of the
		// para, where we want an empty segment to handle the insertion point.
		// (Note that we specifically don't want to even call CheckForObject
		// if we keep the empty segment. We need to get it inserted first. The next
		// iteration will try this position again.)
		if ((!dichLimSeg) && (!m_pboxFirst) && CheckForObject())
			return;

		// Make the segment into a box and add to the current line.
		// (If there is an available string box, reuse it--especially if the main one)
		VwStringBox * psbox;
		MakeStringBox(qlseg, &psbox, m_ichMinString, m_twsh);
		AddBoxToLine(psbox, m_twsh, est);
		if (nSegDirDepth > m_nMaxDirectionDepth)
			m_nMaxDirectionDepth = nSegDirDepth;

		m_ichMinString += dichLimSeg; // for next seg if any

		// Only the first segment on the line needs context from the previous line.
		// Here was assume that successive segments on the same line need no context.
		m_psegPrevContext = NULL;

		switch (est)
		{
		case kestMoreLines:
			// we need more segs for this run, so can fit no more stuff on this line.
			m_zpbs = kzpbsFinalizeLine;
			return;
		case kestHardBreak:
			{ // block
				if (m_ichMinString == m_ichLimString)
				{
					// We turned ALL the available text in this string into segments, but there is another
					// string (or embedded box). In this case the spec for IRenderEngine is ambiguous, and
					// unfortunately, Graphite sets est to kestHardBreak, while UniscribeEngine sets it to
					// kestNoMore. We want to handle it the same either way.
					StartNewString();
					return;
				}
				// ENHANCE JohnT: handle hard break that is tab
				if (CheckForObject())
				{
					// We inserted (or will soon try to insert) more stuff on this line.
					// So the segment we just made no longer ends the line, and must be adjusted.
					// This may cause backtracking, if psbox ends with white space and gets wider.
					BoxEndsLine(psbox, false); // Not LastBoxEndsLine, psbox may no longer be the last box
					break;
				}
				// ENHANCE JohnT: should we have code here to skip a LF, if we found a CR?
				m_zpbs = kzpbsFinalizeLine;
				OLECHAR ch;
				ITsTextPropsPtr qttp;
//-				m_pts->CharAndPropsAt(m_pts->LogToRen(m_ichMinString), &ch, &qttp);
				m_pts->CharAndPropsAt(m_pts->RenToLog(m_ichMinString), &ch, &qttp);
				if (ch == kchwHardLineBreak)
					m_ichMinString++; // skip hard line break
				else
					m_ichMinString = -1; // indicates no more text to add.
			}
			break;
		case kestNoMore:
			StartNewString();
			return;
		case kestBadBreak:
		case kestMoreWhtsp:
		case kestOkayBreak:
		case kestWsBreak:
			// Some text fit, but we want to try putting more on the line--unless
			// there is a hard-break.
			OLECHAR ch;
			ITsTextPropsPtr qttp;
			m_pts->CharAndPropsAt(m_pts->RenToLog(m_ichMinString), &ch, &qttp);
			if (ch == kchwHardLineBreak)
			{
				m_ichMinString++; // skip hard line break
				m_vest.Pop();
				m_vest.Push(kestOkayBreak);
				m_zpbs = kzpbsFinalizeLine;
				return;
			}

			LastBoxEndsLine(false);
			if (m_dxWidthRemaining < 0)
			{
				// It did not fit once end line is set false! Don't try to put any more.
				if (est == kestBadBreak || est == kestWsBreak)
				{
					// Moreover, we stopped for some reason, maybe change of chrp,
					// at a place that is not a good line break. Backtrack!
					// Todo JohnT: figure what to do here. For now go ahead and break...
				}
				// OK to line break here; tell it to be the end of the line as originally
				// proposed, and finalize the line.
				m_zpbs = kzpbsFinalizeLine;
				LastBoxEndsLine(true);
				return;
			}
			// OK, last box still fits with end line false.
			// See what more we can put.
			Assert(m_ichMinString < m_ichLimString);
			// Stay in this mode, try to add more.
		}
	}

	/*------------------------------------------------------------------------------------------
		Get the next segment, if anything will fit.
	------------------------------------------------------------------------------------------*/
	void GetSegment(
		int ichwMin, int ichwLim, int ichwLimBacktrack,
		ComBool fNeedFinalBreak,
		ComBool fStartLine,
		int dxMaxWidth,
		LgLineBreak lbPref, LgLineBreak lbMax,
		ILgSegment ** ppsegRet,
		int * pdichwLimSeg, int * pdxWidth, LgEndSegmentType * pest,
		ILgSegment * psegPrev)
	{
		try{
			CheckHr(m_qre->FindBreakPoint(
				m_pvg, m_pts, m_qvjus, ichwMin, ichwLim, ichwLimBacktrack,
				fNeedFinalBreak,
				fStartLine,
				dxMaxWidth,
				lbPref, lbMax,
				m_twsh, m_fParaRtl,
				ppsegRet,
				pdichwLimSeg, pdxWidth, pest,
				psegPrev));
			if (*ppsegRet != NULL && m_twsh == ktwshNoWs)
			{
				// An upstream segment may need to be truncated if it embeds downstream white space.
				OLECHAR chmarker = m_fParaRtl ? 0x200f : 0x200e; // RLM : LRM
				OLECHAR * rgch = NewObj OLECHAR[*pdichwLimSeg];
				CheckHr(m_pts->Fetch(ichwMin, ichwMin + *pdichwLimSeg, rgch));
				ILgCharacterPropertyEnginePtr qcpe;
				qcpe.CreateInstance(CLSID_LgIcuCharPropEngine);

				// We're looking for a sequence of the marker character, some white space, and another marker.
				// this loop can find a marker that is the last character of the segment with no following white
				// space, but using that won't shorten the segment so we don't.
				int ichfirstMarker = -1;
				for (int i = 0; i < *pdichwLimSeg; i++)
				{
					if (rgch[i] == chmarker)
					{
						if (ichfirstMarker == -1)
							ichfirstMarker = i;
						else
							break; // got the pattern.
					}
					else
					{
						ComBool fIsSep;
						CheckHr(qcpe->get_IsSeparator(rgch[i], &fIsSep));
						if (!fIsSep)
						{
							ichfirstMarker = -1; // reset the search
						}
					}
				}
				delete[] rgch;
				if (ichfirstMarker != -1 && ichfirstMarker + 1 < *pdichwLimSeg) // got one at a position that will shorten the segment
				{
					(*ppsegRet)->Release(); // discard the segment we got.
					// truncate segment, by making a new one as if we were backtracking and the first marker
					// was the last character we are allowed to include.
					int lim = ichwMin + ichfirstMarker + 1;
					CheckHr(m_qre->FindBreakPoint(
						m_pvg, m_pts, m_qvjus, ichwMin, lim, lim,
						fNeedFinalBreak,
						fStartLine,
						dxMaxWidth,
						lbPref, lbMax,
						m_twsh, m_fParaRtl,
						ppsegRet,
						pdichwLimSeg, pdxWidth, pest,
						psegPrev));
					// This is right before a space, so it should definitely be a good break!
					*pest = kestOkayBreak;
					// ...unless it's a non-breaking space :-<
					OLECHAR ch;
					CheckHr(m_pts->Fetch(lim, lim + 1, &ch)); // we shortened it, so there must be at least one more
					if (ch == L'\x00a0' || ch == L'\x202F' || ch == L'\xfeff' || ch == L'\x2060')
						*pest = kestBadBreak;
				}
			}
		}
		catch(Throwable& thr){
			if (thr.Result() == E_FAIL)
				m_pvpbox->Root()->SetSegmentError(thr.Result(), thr.Message());
			else
				throw thr;
		}
	}

	/*------------------------------------------------------------------------------------------
		Finalize a line. We come here after something would not fit, or we run out of things
		to add. The following are the possible initial states and outcomes. In all cases,
		the variables relating to where to place the line are significant, also m_pboxStartLine
		and m_pboxEndLine, which indicate what material we hope to put on the line.

		One possible outcome (the most common, hopefully) is that after rearranging the line
		if necessary and adjusting end-of-line flags on text segments, everything fits,
		and we output the line. Another option is usually that things don't fit, and (without
		change of state) we move to backtrack mode. If the last box is a text box, and bidi
		reordering moves it so it is not physically at the end of the line, another possible
		outcome is that we add a white space segment to the line before one of the other
		outcomes.

		1. We just added a non-string box. m_pboxNextNonStr indicates the next box to put
		in the paragraph. m_ichMinString is -1. All the string-related variables are not
		significant.

		2. We just added a string segment which filled the line. m_itss indicates
		what item of m_pts to add after we do the string. m_ichMinString gives the next
		char to add, and m_ichLimSeg indicates how much more text needs to be added.

		m_zpbs may be kspbsFinalizeNoBack to indicate that no backtracking is possible
	------------------------------------------------------------------------------------------*/
	void FinalizeLine()
	{
		// Every line should have something in it! But we've had several crashes where it did not.
		// I (JohnT) cannot see how this is possible. Most likely it is some bizarre case where
		// the window is very narrow. Instead of crashing, abort the layout, and hope it gets
		// done again more successfully at a larger width before the user sees it. At worst,
		// a display with something strange may give us more clue than a crash.
		if (!m_pboxStartLine || !m_pboxEndLine)
		{
			Assert(false); // Should have a line of boxes to output.
			m_zpbs = kzpbsAbort;
			return;
		}

		// If the line ends with something that knows it can't be a line end,
		// switch to kpbBackTrack and continue.
		if (m_zpbs != kspbsFinalizeNoBack && m_pboxEndLine->CannotEndLine())
		{
			m_zpbs = kzpbsBackTrack;
			return;
		}
		// If we can deduce it can't be a line end, backtrack.
		// if last box on line is a string box, and we broke it at a point that is not a
		// valid line break, backtrack.
		AssertEndSegTypeVectorSize();
		VwStringBox * psbox = dynamic_cast<VwStringBox *>(m_pboxEndLine);
		LgEndSegmentType est = *(m_vest.Top());
		if (psbox)
		{
			if (est == kestBadBreak)
			{
				m_zpbs = kzpbsBackTrack;
				return;
			}
			if (est == kestWsBreak)
			{
				// Typically, we assume this is a bad break, too. But if it is right after
				// white space, definitely allow the break.
				int ichMinBox = psbox->IchMin();
				int cchSeg;
				CheckHr(psbox->Segment()->get_Lim(ichMinBox, &cchSeg));
				int ichLastBox = ichMinBox + cchSeg - 1;
				if (ichLastBox < 0)
				{
					// Don't think this can happen, but be defensive. If we can't establish white space,
					// treat as bad break.
					m_zpbs = kzpbsBackTrack;
					return;
				}
				OLECHAR ch;
				CheckHr(m_pts->Fetch(ichLastBox, ichLastBox + 1, &ch));
				ILgCharacterPropertyEnginePtr qchprpeng;
				qchprpeng.CreateInstance(CLSID_LgIcuCharPropEngine);
				byte lbp;
				CheckHr(qchprpeng->GetLineBreakProps(&ch, 1, &lbp));
				lbp &= 0x1f; // strip 'is it a space' high bit
				// If it's a space (or other character which provides a break opportunity after),
				// go ahead and break. Otherwise treat as bad break.
				if (lbp != klbpSP && lbp != klbpBA && lbp != klbpB2)
				{
					m_zpbs = kzpbsBackTrack;
					return;
				}
			}
		}

		// If this is the last line to output and we didn't fit everything, check
		// there is room for the final "..."
		// Note this test differs from the one for the main loop: we don't test m_pboxStartLine.
		// That test is needed to determine whether we need more steps in the loop (e.g., a
		// call to outputline), but does not indicate whether there is actually more material
		// to add. There is more material iff there are are more strings or more characters of
		// this string.
		if (m_clines == m_clinesMax - 1 &&
			(m_itss < m_pts->CStrings() || m_ichMinString >= 0))
		{
			static OleStringLiteral rgchEllipsis(L"..."); // Review JohnT: use Unicode one-char ellipsis?
			const int cchEllipsis = 3;
			CheckHr(m_pvg->SetupGraphics(m_pzvps->Chrp()));
			// Review JohnT: should we use a single Unicode character that represents "..."?
			int dxdEllipsis, dydEllipsis;
			CheckHr(m_pvg->GetTextExtent(cchEllipsis, rgchEllipsis, &dxdEllipsis,
				&dydEllipsis));
			// If there is not room for the ellipsis, we probably want to backtrack;
			// except that if the paragraph is very narrow we will just leave it out.
			if (m_zpbs != kspbsFinalizeNoBack && m_dxWidthRemaining < dxdEllipsis &&
				m_dxInnerAvailWidth > 3 * dxdEllipsis)
			{
				m_zpbs = kzpbsBackTrack;
				return;
			}
			// This ensures that the boxes on the line are correctly placed if right aligned.
			m_dxWidthRemaining -= dxdEllipsis;
		}

		if (!FinalizeLineOrder())
			return;

		// If all is well, go ahead and output the line.
		OutputLine();
	}

	/*------------------------------------------------------------------------------------------
		Fully justify the segments.
	------------------------------------------------------------------------------------------*/
	bool Justify()
	{
		if (*(m_vest.Top()) == kestNoMore) // don't justify last line
			return false;

		VwBox * pbox;
		int ibox;
		Vector<int> vdxsWidths;
		Vector<int> vdxsDiffs;

		int dxsWidthUsed = 0;
		for (pbox = m_pboxStartLine; pbox; pbox = pbox->Next())
		{
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
			if (psbox)
			{
				int dxsWidth = psbox->Width();
				vdxsWidths.Push(dxsWidth);
				dxsWidthUsed += dxsWidth;
			}
			if (pbox == m_pboxEndLine)
				break;
		}

		// DEBUG
		if (m_dxWidthRemaining < 0)
		{
			// Shrinking
			char rgch[20];
			StrApp strTmp(L"shrinking line ");
			_itoa_s(m_clines + 1, rgch, sizeof(rgch), 10);
			strTmp += rgch;
			strTmp += L" by ";
			_itoa_s(m_dxWidthRemaining * -1, rgch, sizeof(rgch), 10);
			strTmp += rgch;
			strTmp += L" units\n";
			OutputDebugString(strTmp.Chars());
		}

		// Divide the extra width among the segments, in a way that is proportional
		// to the widths of the segments.
		int dxsWidthUsedLp = dxsWidthUsed;
		int dxsDiffLp = m_dxWidthRemaining;
		for (ibox = 0; ibox < vdxsWidths.Size(); ibox++)
		{
			int dxsThis =
				(dxsWidthUsedLp <= 0) ? 0 : ((dxsDiffLp  * vdxsWidths[ibox]) / dxsWidthUsedLp);
			vdxsDiffs.Push(dxsThis);
			// Doing it this way avoids having a left-over amount due to rounding error.
			dxsWidthUsedLp -= vdxsWidths[ibox];
			dxsDiffLp -= dxsThis;
		}

		ibox = 0;
		for (pbox = m_pboxStartLine; pbox; pbox = pbox->Next())
		{
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
			if (psbox)
			{
				int xsOld = pbox->Width();
				psbox->Segment()->put_Stretch(psbox->IchMin(), vdxsDiffs[ibox]);
				psbox->DoLayout(m_pvg, m_dxInnerAvailWidth, m_dxWidthRemaining);
				int xsNew = pbox->Width();
				m_dxWidthRemaining -= (xsNew - xsOld);
				ibox++;
			}
			if (pbox == m_pboxEndLine)
				break;
		}
		return true;
	}

	/*------------------------------------------------------------------------------------------
		Do any necessary reordering before outputting the line.
		Return true if this does not cause the line to overflow the available space.
	------------------------------------------------------------------------------------------*/
	bool FinalizeLineOrder()
	{

//		Assert(!m_fParaRtl || m_nMaxDirectionDepth > 0);
		if (m_fParaRtl || m_nMaxDirectionDepth > 0)
		{
			// We may need to reorder boxes on line.
			// First make a vector of them.
			m_vbox.Clear();
			for (VwBox* pbox = m_pboxStartLine; pbox != m_pboxEndLine; pbox = pbox->Next())
				m_vbox.Push(pbox);
			m_vbox.Push(m_pboxEndLine);
			Assert(m_vbox.Size() == m_vest.Size());

			SetWeakDirections(TopDepth());
			// Do the actual reordering.
			for (int nDepth = 1; nDepth <= m_nMaxDirectionDepth; nDepth++)
				ReverseUpstreamBoxes(nDepth, m_pboxEndLine);
			// Fix all start and end flags and adjust width accordingly.
			FixStartAndEndFlags(m_vbox[0], m_vbox[m_vbox.Size() - 1]);

			// Does it all still fit?
			if (m_zpbs != kspbsFinalizeNoBack && m_dxWidthRemaining < 0)
			{
				m_zpbs = kzpbsBackTrack;
				return false;
			}
		}
		return true;
	}

	/*------------------------------------------------------------------------------------------
		Compute the baseline for the current line, given the max ascent and descent of the
		boxes on the line, and the information saved about the previous line if any.
	------------------------------------------------------------------------------------------*/
	virtual int BaseLine(int dyMaxDescent, int dyMaxAscent)
	{
		int yBaseLine;
		if (m_fExactLineHeight)
		{
			CalculateExactAscent();

			// If we're about to scrunch the line down to fit in less space than it would
			// naturally occupy, we should only do so if we're dealing with string boxes.
			// Picture boxes (etc.) should not be scrunched. Instead they should be expanded,
			// if necessary to be an even multiple of the exact linespacing.
			if (SomethingOtherThanAStringThatIsTooBigForAnExactSpaceLine(dyMaxAscent + dyMaxDescent))
			{
				int dyTotalLineHeight = dyMaxAscent + dyMaxDescent;
				int nRoundUp = (dyTotalLineHeight % m_dypLineHeight == 0) ? 0 : 1;
				int yBottomOfLine = m_ysEdgeOfSpaceForThisLine + (dyTotalLineHeight / m_dypLineHeight + nRoundUp) * m_dypLineHeight;
				// Move up to the exact expected descent above where we decided we want the expanded line to end.
				int dyExactDescent = m_dypLineHeight - m_dyExactAscent;
				yBaseLine = yBottomOfLine - dyExactDescent;
			}
			else
				yBaseLine = m_ysEdgeOfSpaceForThisLine + m_dyExactAscent;
			// Some kinds of boxes (such as string boxes) will just ignore this, but if this is a picture box or pile
			// box or something that lacks a sensible default, we can force the ascent to be what we want.
			m_pboxStartLine->ForceAscentForExactLineSpacing(yBaseLine - m_ysEdgeOfSpaceForThisLine);

//			if (m_pboxStartLine == m_pboxFirst)
//				yBaseLine = m_ysEdgeOfSpaceForThisLine + m_dyExactAscent;
//			else
//				yBaseLine = m_yBaseLine + m_dypLineHeight;
		}
		else if (m_pboxStartLine == m_pboxFirst)
		{
			// We are on the first line; baseline is just the ascent of the segments
			// plus any GapTop already stored in m_ysEdgeOfSpaceForThisLine.
			yBaseLine = m_ysEdgeOfSpaceForThisLine + dyMaxAscent;
		}
		else
		{
			// Set the baseline the preferred distance from the previous baseline
			// (Note LineHeight is in 1000/pt, distances are in twips)
			yBaseLine = m_ysEdgeOfSpaceForThisLine - m_dysPrevBaselineOffset + m_dypLineHeight;
			// If that will make the lines overlap, increase the spacing.
			if (yBaseLine - m_ysEdgeOfSpaceForThisLine < dyMaxAscent)
				yBaseLine = m_ysEdgeOfSpaceForThisLine + dyMaxAscent;
		}
		return yBaseLine + m_dyTagAbove;
	}

	bool WontFitInFixedLineSpace(VwBox * pbox)
	{
		if (pbox->Ascent() > m_dyExactAscent)
			return true;
		// We could consider whether it will extend too far down, but the current client doesn't
		// care: it is going to put the baseline the standard descent above the bottom of the line.
		return false;
	}
	/*------------------------------------------------------------------------------------------
		When using exact line spacing, figure out if we're building something other than a
		string box (e.e.g, a picture box) that is too big for the line.
	------------------------------------------------------------------------------------------*/
	bool SomethingOtherThanAStringThatIsTooBigForAnExactSpaceLine(int dyLineHeight)
	{
		if (dyLineHeight <= m_dypLineHeight)
			return false;

		VwBox * pbox;
		if (m_nMaxDirectionDepth > 0)
		{
			for (int i = 0; i < m_vbox.Size(); ++i)
			{
				pbox = m_vbox[i];
				if (!pbox->IsStringBox() && WontFitInFixedLineSpace(pbox))
					return true;
			}
		}
		else // no vector, loop through linked list
		{
			for (pbox = m_pboxStartLine; pbox; pbox = pbox->Next())
			{
				if (!pbox->IsStringBox() && WontFitInFixedLineSpace(pbox))
					return true;
			}
		}
		return false;
	}

	/*------------------------------------------------------------------------------------------
		When using exact line spacing, figure out what the ascent should be.
	------------------------------------------------------------------------------------------*/
	void CalculateExactAscent()
	{
		if (m_dyExactAscent > -1)
			return;

		// Loop through all the boxes that have been generated so far (guaranteed to be at
		// least one line) and calculate the proportion of their ascent to their total height.
		// Keep track of how many characters use each proportion.
		Assert(m_pboxFirst);
		HashMap<int, int> hmnc;
		int nBestAscent = -1;	// percentage
		int cchBest = -1; // so even an empty segment will beat it
		for (VwBox * pbox = m_pboxFirst; pbox; pbox = pbox->Next())
		{
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
			if (psbox)
			{
				int cchSeg;
				CheckHr(psbox->Segment()->get_Lim(0, &cchSeg));
#if WIN32
				int nAscentSeg = (psbox->Ascent() * 100) / psbox->Height();
#else
				// TODO-Linux: psbox->Height() should NOT return 0 - FIXME.
				int nAscentSeg = psbox->Height() ? (psbox->Ascent() * 100) / psbox->Height() : 0;
#endif
				int cchAscent = 0;
				hmnc.Retrieve(nAscentSeg, &cchAscent);
				cchAscent += cchSeg;
				hmnc.Insert(nAscentSeg, cchAscent, true);
				if (cchAscent > cchBest)
				{
					// This is the best ascent proportion so far.
					cchBest = cchAscent;
					nBestAscent = nAscentSeg;
				}
			}
		}

		// The exact ascent is the most popular proportion applied to the
		// specified exact line height.
		m_dyExactAscent = (nBestAscent * m_dypLineHeight) / 100;
	}

	/*------------------------------------------------------------------------------------------
		Add the recently created line of boxes to the paragraph.
	------------------------------------------------------------------------------------------*/
	void OutputLine()
	{
		VwBox * pbox; // loop variables

		LgEndSegmentType est = *(m_vest.Top());

		// If there are inner piles in the line, align their corresponding components
		IntVec vdyBaselines; // item is position of nth baseline
		bool fNeedAdjust = false;
		for (pbox = m_pboxStartLine; ; pbox = pbox->Next())
		{
			if (pbox->IsInnerPileBox())
			{
				int irow = 0;
				pbox->ComputeInnerPileBaselines(m_pvpbox, vdyBaselines, 0, irow, fNeedAdjust);
			}
			if (pbox == m_pboxEndLine)
				break;
		}
		if (fNeedAdjust)
		{
			// Loop again, adjusting all boxes to the computed baselines
			for (pbox = m_pboxStartLine; ; pbox = pbox->Next())
			{
				if (pbox->IsInnerPileBox())
				{
					int irow = 0;
					pbox->AdjustInnerPileBaseline(m_pvpbox, vdyBaselines, irow);
				}
				if (pbox == m_pboxEndLine)
					break;
			}
		}

		// Figure the X and Y positions of each box on the line
		int dyMaxDescent = m_dyMinDescent;
		int dyMaxAscent = m_dyMinAscent;
		int yBaseLine;
		m_dyMinAscent = m_dyMinDescent = 0; // No special minimum on subsequent lines
		int xPos = 0;
		int xSpaceUsed = 0;
		int tal = m_pvpbox->Style()->ParaAlign();
		bool fJustify;

		// This is the amount we add in various places to achieve right or (half of it) center
		// alignment. Typically we use the remaining available white space in order to
		// move stuff over that far. However, if we're a nested paragraph (inside an inner pile),
		// we must not do this, or each pile will be the full width and end up on a line by itself.
		int dxAlignMentAdjust = m_dxWidthRemaining;
		if (IsNested())
			dxAlignMentAdjust = 0;
		else
			dxAlignMentAdjust = dxAlignMentAdjust; // for breakpoint!
		int dxExtraWidthRight = 0; // extra space needed on right for leading/trailing margin.

		switch(tal)
		{
		case ktalJustify:
			fJustify = Justify(); // false on last line
			// For right-to-left paragraphs, the last line is right-justified.
			// Note that we must not use dxAlignMentAdjust here, because that was computed
			// using the old value of m_dxWidthRemaining; but calling Justify() changes that.
			// Usually, alignment adjust will be zero except for the last line; but just in case
			// we were unable to fully justify, we might need it anyway.
			// It doesn't make much sense to try to justify a nested paragraph, but just in case,
			// we won't adjust those.
			if (m_pvpbox->RightToLeft())
			{
				dxAlignMentAdjust = m_dxWidthRemaining; // usually zero except for last line
				if (IsNested())
					dxAlignMentAdjust = 0;
				xPos = m_dxTrail + dxAlignMentAdjust;
			}
			else {
				xPos = m_dxLead;
			}
			xSpaceUsed = xPos;
			dxExtraWidthRight = m_dxTrail;
			break;
		case ktalLeft:
			if (m_pvpbox->RightToLeft()) {
				xPos = m_dxTrail;
			}
			else {
				xPos = m_dxLead;
			}
			xSpaceUsed = xPos;
			dxExtraWidthRight = m_dxTrail;
			break;
		case ktalRight:
			if (m_pvpbox->RightToLeft()) {
				xPos = m_dxTrail + dxAlignMentAdjust;
			}
			else {
				xPos = m_dxLead + dxAlignMentAdjust;
			}
			xSpaceUsed = xPos;
			dxExtraWidthRight = m_dxLead;
			break;
		case ktalCenter:
			if (m_pvpbox->RightToLeft()) {
				xPos = m_dxTrail + (dxAlignMentAdjust / 2);
				xSpaceUsed = m_dxTrail + dxAlignMentAdjust;
			}
			else {
				xPos = m_dxLead + (dxAlignMentAdjust / 2);
				xSpaceUsed = m_dxLead + dxAlignMentAdjust;
			}
			dxExtraWidthRight = m_dxTrail;
			break;
		case ktalLeading:
			if (m_pvpbox->RightToLeft()) {
				xPos = m_dxTrail + dxAlignMentAdjust;	// right-align
				dxExtraWidthRight = m_dxLead;
			}
			else {
				xPos = m_dxLead;	// left-align
				dxExtraWidthRight = m_dxTrail;
			}
			xSpaceUsed = xPos;
			break;
		case ktalTrailing:
			if (m_pvpbox->RightToLeft()) {
				xPos = m_dxTrail;	// left-align
				dxExtraWidthRight = m_dxTrail;
			}
			else {
				xPos = m_dxLead + dxAlignMentAdjust;	// right-align
				dxExtraWidthRight = m_dxLead;
			}
			xSpaceUsed = xPos;
			break;
		default:
			break;
		}
		int i;  // local loop variable
		bool fAloneOnLine; // true if only one box on line.
		// Working through the list gets done one of two ways, depending on whether we
		// rearranged the boxes for bidi or not. What gets done for each box in the
		// list should be exactly the same in each branch.
		if (m_nMaxDirectionDepth > 0)
		{
			fAloneOnLine = m_vbox.Size() == 1;
			for (i = 0; i < m_vbox.Size(); ++i)
			{
				pbox = m_vbox[i];
				// Next 2 lines duplicated below
				dyMaxDescent = std::max(dyMaxDescent, pbox->LineSepDescent(fAloneOnLine));
				dyMaxAscent = std::max(dyMaxAscent, pbox->Ascent());
			}
			yBaseLine = BaseLine(dyMaxDescent, dyMaxAscent);
			for (i = 0; i < m_vbox.Size(); ++i)
			{
				pbox = m_vbox[i];
				// Next 4 lines duplicated below
				pbox->Top(yBaseLine - pbox->Ascent());
				pbox->Left(xPos);
				xPos += pbox->Width();
				xSpaceUsed += pbox->Width();
			}
			m_vbox.Clear();  // ready for next line
			m_nMaxDirectionDepth = 0; // no embedding yet on next line
		}
		else // no vector, loop through linked list
		{
			fAloneOnLine = m_pboxStartLine == m_pboxEndLine;
			for (pbox = m_pboxStartLine; ; pbox = pbox->Next())
			{
				// Next 2 lines duplicated above
				dyMaxDescent = std::max(dyMaxDescent, pbox->LineSepDescent(fAloneOnLine));
				dyMaxAscent = std::max(dyMaxAscent, pbox->Ascent());
				if (pbox == m_pboxEndLine)
					break;
			}
			yBaseLine = BaseLine(dyMaxDescent, dyMaxAscent);
			for (pbox = m_pboxStartLine; ; pbox = pbox->Next())
			{
				// Next 4 lines duplicated above
				pbox->Top(yBaseLine - pbox->Ascent());
				pbox->Left(xPos);
				xPos += pbox->Width();
				xSpaceUsed += pbox->Width();
				if (pbox == m_pboxEndLine)
					break;
			}
		}
		if (m_pxvo && !m_fSemiTagging && !m_fExactLineHeight)
		{
			// Make sure descent is at least big enough for double underlining.
			// Review PM (JohnT): should we check space for double underline even if no overlay?
			int dyInch;
			CheckHr(m_pvg->get_YUnitsPerInch(&dyInch));
			int dydMinDescent = 4 * dyInch / 96;
			dyMaxDescent = std::max(dyMaxDescent, dydMinDescent);
		}
		// Update line count, top of line position, etc.
		m_dxLead = m_dxLeadSubseqLines;  // cancel first-line indent for lines other than 1
		m_clines++;
		// Normally we want to find word breaks if any, but for the last line we have room for,
		// we want all the letters we can fit.
		if (m_clines == m_clinesMax - 1)
			m_lbNormalBreak = klbLetterBreak;

		SetupNextLine(yBaseLine, dyMaxDescent, dyMaxAscent);

		if (xSpaceUsed + dxExtraWidthRight > m_dxInnerWidth)
			m_dxInnerWidth = xSpaceUsed + dxExtraWidthRight;
		m_dxWidthRemaining = m_dxInnerAvailWidth; // no space used on next line
		// If the next line is one of the ones that needs to wrap around a drop cap
		// (currently only the second line), and there was room for something else
		// on the line with the drop cap, then adjust the leading indent on the
		// next line by the width of the drop cap.
		if (m_clines == 1 && m_pboxFirst->IsDropCapBox() && !fAloneOnLine)
		{
			WrapAroundDropCap(m_pboxFirst);
		}

		m_pboxEndPrevLine = m_pboxEndLine; // save for relinking boxes
		if (m_pboxEndPrevLine->IsStringBox())
		{
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(m_pboxEndPrevLine);
			m_psegPrevContext = psbox->Segment();
		}
		m_pboxStartLine = NULL;  // nothing on new next line
		m_pboxEndLine = NULL;
		m_psboxLastReal = NULL; // no white space segment on this line yet
		m_vest.Clear();
		m_vbox.Clear();
		AssertEndSegTypeVectorSize();

		int dympExactAscent = m_dyExactAscent < 0 ? m_dyExactAscent :
			MulDiv(m_dyExactAscent, kdzmpInch, m_dypInch);
		m_pvpbox->SetExactAscent(dympExactAscent);

		if (m_vmpbox.Size() > 0)
		{
			VwBox * pboxT = m_vmpbox[0];
			// Remove the first pending box from the list.
			m_vmpbox.Replace(0, 1, NULL, 0);
			pboxT->SetNext(NULL);
			// Lay it out in the currently available width.
			pboxT->DoLayout(m_pvg, m_dxInnerAvailWidth);
			// Put it on the next line.
			AddBoxToLine(pboxT, ktwshAll, kestOkayBreak);
			// And that makes a complete line all by itself, so back to finalize mode.
			m_zpbs = kzpbsFinalizeLine;
			return; // Back to main loop to process the new line we just made.
		}

		// Determine the proper next mode, depending on whether we were in the middle
		// of adding a string or not, and switch to it.
		if (est != kestNoMore && m_ichMinString != -1)
		{
			// There's more stuff to put into the paragraph. If we're doing a partial layout
			// we may be able to resynchronize at this point, concluding that the rest of the
			// lines are not affected.
			VwStringBox * psboxNextLine = (m_pboxNextLine) ?
				dynamic_cast<VwStringBox *>(m_pboxNextLine) :
				NULL;
			if (psboxNextLine && m_ichMinString >= m_ichLimDiff &&
				psboxNextLine->IchMin() + m_cchLenDiff == m_ichMinString &&
				m_fDidOneExtraLine)
			{
				// Keep the rest of the lines, adjusting their counters and positions as
				// necessary.
				m_pboxEndPrevLine->SetNext(m_pboxNextLine);
				// we are going to set kzpbsQuit, which stops the loop, so it is not dangerous
				// as it usually would be to set this variable without setting m_pboxEndLine.
				// However, for the quit to take place, we must do one more iteration of the main
				// loop, and that might not happen unless we make this variable non-null.
				// The value we set it to is arbitrary.
				m_pboxStartLine = m_pboxNextLine;
				int yOldBaseline = m_pboxNextLine->Baseline();
				int yNewBaseline = NextBaseLine(m_pboxNextLine);
				for (pbox = m_pboxNextLine; pbox; pbox = pbox->Next())
				{
					VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
					if (psbox)
						psbox->_IncIchMin(m_cchLenDiff);
					pbox->Top(pbox->Top() + yNewBaseline - yOldBaseline);
				}
				// Calculate the total number of lines and the final m_ysEdgeOfSpaceForThisLine.
				Vector<VwBox *> vpboxOneLine;
				VwBox * pboxNextLine = m_pboxNextLine;
				VwBox * pboxLastLine = m_pboxNextLine;
				while (pboxNextLine)
				{
					pboxLastLine = pboxNextLine;
					pboxNextLine = m_pvpbox->GetALine(pboxNextLine, vpboxOneLine);
					m_clines++;
				}
				yBaseLine = pboxLastLine->Baseline();
				dyMaxDescent = 0;
				dyMaxAscent = 0;
				for (pbox = pboxLastLine; pbox; pbox = pbox->Next())
				{
					dyMaxDescent = std::max(dyMaxDescent, pbox->Descent());
					dyMaxAscent = std::max(dyMaxAscent, pbox->Ascent());
				}

				SetupNextLine(yBaseLine, dyMaxDescent, dyMaxAscent);

				m_pboxNextLine = NULL; // no more unused lines.
				m_zpbs = kzpbsQuit;
				return;
			}

			// Can't resynchronize at this point (or doing a full layout), discard one more
			// line of the old layout if any and proceed to make another line.
			if (m_ichMinString >= m_ichLimDiff)
				m_fDidOneExtraLine = true;
			DiscardOneMoreLine();
			m_zpbs = kzpbsAddWsRun;
		}
		else
		{
			// there's no more text to add in the current group of TsStrings (kest is kestNoMore).
			// m_ichMinString should be -1 to be consistent with this.
			// Typically we're at the end of the paragraph. But we could also be stopped just before
			// a non-string (e.g., picture or inner pile) box. So we set up to add something more
			// to the paragraph, if there is anything more.

			// Ideally we should be able to do better than this; in some cases we shouldn't
			// have to discard a line if the next non-string boxes match up. But non-string
			// boxes are unusual enough that this will do for now.
			DiscardOneMoreLine();
			m_zpbs = kzpbsAddUnknownBox;
			Assert(m_ichMinString == -1);
		}
	}

	// Figure m_ysEdgeOfSpaceForThisLine and m_dysPrevBaselineOffset for the next line,
	// given the baseline of the current one and its max ascent and descent.
	// Top of next line is at the baseline for the current line, plus the max descent,
	// plus extra space for tagging if needed...unless we want exact spacing, in
	// which case we use the exact value.
	// Overridden for inverted layout.
	virtual void SetupNextLine(int yBaseLine, int dyMaxDescent, int dyMaxAscent)
	{

		if (m_fExactLineHeight)
		{
			m_ysEdgeOfSpaceForThisLine = yBaseLine + (m_dypLineHeight - m_dyExactAscent) + m_dyTagBelow;
			m_dysPrevBaselineOffset = m_dypLineHeight - m_dyExactAscent;
		}
		else
		{
			m_ysEdgeOfSpaceForThisLine = yBaseLine + dyMaxDescent + m_dyTagBelow;
			m_dysPrevBaselineOffset = dyMaxDescent;
		}
	}


	/*------------------------------------------------------------------------------------------
		Calculate the desired baseline for the line following the one which just got
		laid out. This following line has already been laid out as well,
		but may need its y-position adjusted
	------------------------------------------------------------------------------------------*/
	int NextBaseLine(VwBox * pboxStartLine)
	{
		int dyMaxDescent = 0;
		int dyMaxAscent = 0;
		Vector<VwBox *> vpboxOneLine;
		VwBox * pboxLineBelowThat = m_pvpbox->GetALine(pboxStartLine, vpboxOneLine);
		for (VwBox * pbox = pboxStartLine; pbox != pboxLineBelowThat; pbox = pbox->Next())
		{
			dyMaxDescent = std::max(dyMaxDescent, pbox->Descent());
			dyMaxAscent = std::max(dyMaxAscent, pbox->Ascent());
		}
		int yBaseLine = BaseLine(dyMaxDescent, dyMaxAscent);
		if (m_pxvo && !m_fSemiTagging && !m_fExactLineHeight)
		{
			// Make sure descent is at least big enough for double underlining.
			// Review PM (JohnT): should we check space for double underline even if no overlay?
			int dyInch;
			CheckHr(m_pvg->get_YUnitsPerInch(&dyInch));
			int dydMinDescent = 4 * dyInch / 96;
			dyMaxDescent = std::max(dyMaxDescent, dydMinDescent);
		}
		return yBaseLine;
	}

	/*------------------------------------------------------------------------------------------
		Add a box to the reusable list; reset it.
	------------------------------------------------------------------------------------------*/
	void MakeReusable(VwBox * pbox)
	{
		VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
		psbox->SetNext(m_psboxReusable);
		m_psboxReusable = psbox;
		psbox->Reset();
	}

	/*------------------------------------------------------------------------------------------
		Stuff proposed for current line does not fit.
		Possible initial states are as for FinalizeLine, with the possibility that a white
		space segment has been added to the line. An additional distinction of importance
		for this method is that there may be one or many boxes on the line.
		The valid outcomes of this method are that we are once again in a valid state for
		FinalizeLine, but have removed something from the line. Alternatively, if there is
		only one box on the line and we cannot shorten it, we go direct to output line.
	------------------------------------------------------------------------------------------*/
	void BackTrack()
	{
		// If we made a white space segment discard it.
		if (m_psboxLastReal)
		{
			MakeReusable(m_pboxEndLine);
			m_pboxEndLine = m_psboxLastReal;
			m_psboxLastReal = NULL;
			m_vest.Pop();
		}
		AssertEndSegTypeVectorSize();

		// If the last thing on the line is a text box,
		// try to make an earlier break in its segment.
		VwStringBox * psboxLast = dynamic_cast<VwStringBox *>(m_pboxEndLine);
		if (psboxLast)
		{
			// Make a replacement segment starting at the same place but ending at least
			// one character sooner, and for sure at a good line break.
			int ichMinStrSave = m_ichMinString;
			int dxWidthRemainingSave = m_dxWidthRemaining;
			m_ichMinString = psboxLast->IchMin();
			int ichLimBacktrack;
			CheckHr(psboxLast->Segment()->get_Lim(m_ichMinString, &ichLimBacktrack));
			ichLimBacktrack += m_ichMinString; // lim is relative.
			int dxWidthLast;
			CheckHr(psboxLast->Segment()->get_Width(psboxLast->IchMin(), m_pvg, &dxWidthLast));
			m_dxWidthRemaining += dxWidthLast;
			m_psegPrevContext = NULL;
			bool fStartLine = false;
			if (psboxLast == m_pboxStartLine)
			{
				// The last box on the previous line has our context, if any.
				fStartLine = true;
				// Ideally we should assert that psboxLast was created with startLine = true,
				// but there is no way to ask a segment for that flag! :-(
				VwStringBox * psboxLastOnPrev = dynamic_cast<VwStringBox *>(m_pboxEndPrevLine);
				if (psboxLastOnPrev)
					m_psegPrevContext = psboxLastOnPrev->Segment();
			}

			ichLimBacktrack--;
			// Find a valid surrogate boundary:
			if (ichLimBacktrack > m_ichMinString)
			{
				OLECHAR rgchwTmp[2];
				m_pts->Fetch(ichLimBacktrack-1, ichLimBacktrack+1, rgchwTmp);
				if ((rgchwTmp[0] >= 0xD800) && (rgchwTmp[0] <= 0xDBFF))
				{
					Assert((rgchwTmp[1] >= 0xDC00) && (rgchwTmp[1] <= 0xDFFF));
					ichLimBacktrack--;
				}
			}

			if (ichLimBacktrack > m_ichMinString)
			{

				// We can try for a shorter segment starting at the same place.
				GetRendererForChar(m_ichMinString);
				int nSegDirDepth = m_chrp.nDirDepth;
				if (nSegDirDepth < TopDepth())
					nSegDirDepth += 2;
				if (nSegDirDepth == TopDepth())
					m_twsh = ktwshAll;
				else
					m_twsh = ktwshNoWs;
				ILgSegmentPtr qlseg;
				int dxWidth, dichLimSeg;
				LgEndSegmentType est;
				// Review (SharonC): Why would we ever want to backtrack and find a bad break?
				LgLineBreak lbWorst = m_lbNormalBreak;
//				LgEndSegmentType estLast = *(m_vest.Top());
//				if (m_pboxStartLine == psboxLast)
//					// First box on line: be more flexible about the worst break allowed.
//					lbWorst = (estLast == kestWsBreak) ? klbHyphenBreak : klbClipBreak;

				GetSegment(m_ichMinString, m_ichLimString, ichLimBacktrack,
					true, // must have a valid line break to finish seg at ichLimString
					fStartLine,
					m_dxWidthRemaining,
					m_lbNormalBreak, lbWorst,
					&qlseg,
					&dichLimSeg, &dxWidth, &est,
					m_psegPrevContext);
				if (qlseg)
				{
					// Make sure it really did make a shorter segment. Otherwise we may loop
					// forever.
					Assert(dichLimSeg + m_ichMinString <= ichLimBacktrack);
					// We succeeded in making a shorter segment for our last box.
					psboxLast->SetSegment(qlseg);
					psboxLast->DoLayout(m_pvg, m_dxInnerAvailWidth, m_dxWidthRemaining);
					m_ichMinString += dichLimSeg; // for next seg if any
					m_twshLastBox = m_twsh;
					m_dxWidthRemaining -= dxWidth;
					m_vest.Pop();
					m_vest.Push(est);
					m_vbox.Clear();

					if (m_twsh == ktwshNoWs && m_ichMinString < ichLimBacktrack)
					{
						// If we are separating visible and white-space, try to get a
						// trailing white-space segment.
						ILgSegmentPtr qlsegWs;
						m_twsh = ktwshOnlyWs;
						GetSegment(m_ichMinString, m_ichLimString, ichLimBacktrack,
							true, fStartLine,
							m_dxWidthRemaining,
							m_lbNormalBreak, lbWorst,
							&qlsegWs,
							&dichLimSeg, &dxWidth, &est,
							m_psegPrevContext);
						if (qlsegWs)
						{
							VwStringBox * psboxWs;
							MakeStringBox(qlsegWs, &psboxWs, m_ichMinString, m_twsh);
							AddBoxToLine(psboxWs, m_twsh, est);
							m_ichMinString += dichLimSeg; // for next seg if any
						}
					}

					AssertEndSegTypeVectorSize();

					// Try to finalize again with this shorter segment.
					m_zpbs = kzpbsFinalizeLine;
					return;
				}
			}
			// Either the segment got shortened to zero characters, or there was no viable
			// earlier break point.
			// It is almost impossible psboxLast is the first on its line, since if so
			// we passed arguments allowing even a break that splits characters. But what if
			// we backtrack in a state where only one character fit on the line? If so,
			// a segment with that one character is the whole line, and we go straight to
			// outputting it.
			if (psboxLast == m_pboxStartLine)
			{
				m_ichMinString = ichMinStrSave;
				m_dxWidthRemaining = dxWidthRemainingSave;
				FinalizeLineOrder();
				m_zpbs = kzpbsOutputLine;
				return;
			}
		} // if last box is a string one
		// JohnT: this could in theory be an else-if, as the positive branch above should return
		// one way or another if m_pboxEndLine == m_pboxStartLine. But the following code
		// won't do anything sensible if we only have one box on the line, so it seems worth
		// checking here.
		if (m_pboxEndLine == m_pboxStartLine)
		{
			// We have a single non-string box alone on the line. All we can do is output it.
			m_zpbs = kzpbsOutputLine;
			return;
		}

		// Discard the current last box and try an earlier one.
		VwBox * pboxPrev = BoxBeforeInCurrentLine(m_pboxEndLine);
		// If the old last one is a broken string one we could not shorten dispose of it
		psboxLast = dynamic_cast<VwStringBox *>(m_pboxEndLine);
		if (psboxLast)
		{
			MakeReusable(psboxLast);
			for (int i = 0; i < m_vbox.Size(); ++i)
			{
				if (m_vbox[i] == psboxLast)
				{
					m_vbox.Delete(i);
				break;
				}
			}
		}
		else
		{
			VwPictureBox * pboxpicLast = dynamic_cast<VwPictureBox *>(m_pboxEndLine);
			if (pboxpicLast)
			{
				// First see if it is the only thing on the line. If so output it; we
				// can't shorten it. (ENHANCE PM(JohnT): should we scale it? Clip it?)
				if (pboxpicLast == m_pboxStartLine)
				{
					m_zpbs = kzpbsOutputLine;
					return;
				}
				// We'd like to keep this to put it on the next line. Try finalizing without it
				m_zpbs = kzpbsFinalizeLine;
				if (m_ichSavePic >= 0)
				{
					// We had saved yet another picture! drop it
					delete m_pboxpic;
				}
				// Move m_ichMinString back to point at the object character
				if (m_ichMinString < 0)
					m_ichMinString = m_ichLimString - 1;
				else
					m_ichMinString--;
				// But save the picture box for next try
				m_ichSavePic = m_ichMinString;
				m_pboxpic = pboxpicLast;
			}
			else
			{
				// Currently string and picture boxes are the only possible bits of a string.
				// If we introduce other kinds we will need to add code for them.
				Assert(!m_pboxEndLine->IsBoxFromTsString());
				// Put it back in the list of boxes we have to add
				m_pboxEndLine->SetNext(m_pboxNextNonStr);
				m_pboxNextNonStr = m_pboxEndLine;
				m_itss--; // causes us to process it again later
			}
		}
		m_pboxEndLine = pboxPrev;
		m_vest.Pop();
		// It just might wind up being the end of the whole paragraph, if we are working
		// with a limited number of lines! Don't leave it linked to the box we just
		// freed up for deletion or re-use.
		m_pboxEndLine->SetNext(NULL);
		FixStartAndEndFlags(m_pboxStartLine, m_pboxEndLine); // also fixes width remaining
		AssertEndSegTypeVectorSize();

		m_zpbs = kzpbsFinalizeLine;
		// If it is a string box we can apply better criteria
		psboxLast = dynamic_cast<VwStringBox *>(m_pboxEndLine);
		if (psboxLast)
		{
			// Review (SharonC): is this still a necessary test, now that we are recording
			// the est for each box?
			LgLineBreak lb;
			CheckHr(psboxLast->Segment()->get_EndBreakWeight(psboxLast->IchMin(), m_pvg, &lb));
			if (lb > klbHyphenBreak)
			{
				// It was not a reasonable place to break, try Backtrack again.
				m_zpbs = kzpbsBackTrack;
			}
		}
	} // end of BackTrack method

	/*------------------------------------------------------------------------------------------
		Loop through the contents to build a paragraph.
	------------------------------------------------------------------------------------------*/
	void MainLoop()
	{
		// Loop as long as we have more boxes to process, or more characters
		// to process from the current string, or an incomplete line of boxes we
		// have not finalized; unless we reach the max line limit.
		while (
			m_clines < m_clinesMax &&
			(m_itss < m_pts->CStrings() || m_ichMinString >= 0 || m_pboxStartLine))
		{
			switch (m_zpbs)
			{
			case kzpbsAddUnknownBox:
				AddUnknownItem();
				break;
			case kzpbsAddNonStringBox:
				AddNonStringBox();
				break;
			case kzpbsAddWsRun:
				AddWsRun();
				break;
			case kzpbsFinalizeLine:
			case kspbsFinalizeNoBack:
				FinalizeLine();
				break;
			case kzpbsOutputLine:
				OutputLine();
				break;
			case kzpbsBackTrack:
				BackTrack();
				break;
			case kzpbsQuit:
				return;
			case kzpbsAbort:
				// simulate having output all the lines we're allowed.
				// This terminates the loop, but allows other stuff (like saving non-string boxes
				// that are left over) to continue.
				m_clinesMax = m_clines;
				break;
			}
		}
		// If we have more boxes (we ran out of line count), add them in so we don't lose them.
		// Give the first a negative Top() so we know not to draw them.
		if (m_pboxNextNonStr)
		{
			// This should only happen if we ran out of lines.
			Assert(m_clines == m_clinesMax);
			// Much like AddBoxToLine
			if (m_pboxEndLine)
			{
				m_pboxEndLine->SetNext(m_pboxNextNonStr);
			}
			else
			{
				if (m_pboxEndPrevLine)
					m_pboxEndPrevLine->SetNext(m_pboxNextNonStr);
				else
					m_pboxFirst = m_pboxNextNonStr;
			}
			m_pboxNextNonStr->Top(knTruncated);
		}
		// If there are more lines that were left over after we laid out the whole content of
		// the paragraph, get rid of them (otherwise they become memory leaks).
		DiscardLines(true, false, m_pboxNextLine); // Review JohnT: is this right??
		// Rather than increasing the Height of this box if it is a one-line drop-cap, we make
		// the pile layout routine consider the ExtraHeightIfNotFollowedByPara value, which is
		// adjusted for one-line paragraphs.
		//// If there is only one line (and, eventually, if we're not going to wrap another
		//// paragraph into that space), we need to boost it to leave foom for the DC.
		//if (m_clines == 1 && m_pboxFirst->IsDropCapBox())
		//{
		//	// Enhance JohnT: this doesn't handle tagged drop caps! Surely we don't need to?
		//	m_ysEdgeOfSpaceForThisLine = max (m_ysEdgeOfSpaceForThisLine, m_pboxFirst->Bottom());
		//}
	}

};

class InvertedParaBuilder : public ParaBuilder
{
public:
	virtual ~InvertedParaBuilder() { }
protected:
	// Alter val by distance in the 'forward' direction. By default this is downwards, that is, distance is added.
	// In an inverted paragraph it is subtracted to move up.
	virtual void Forward(int & val, int distance)
	{
		val -= distance;
	}
	// Adjust m_ysEdgeOfSpaceForThisLine (which is at the top or bottom of the whole box)
	// by the appropriate gap in the appropriate direction. The default adds the top gap.
	// For an inverted box subtract the bottom gap.
	virtual void AdjustEdgeOfSpaceForInitialGap()
	{
		m_ysEdgeOfSpaceForThisLine -= m_pvpbox->GapBottom(m_dypInch);
	}

	/*------------------------------------------------------------------------------------------
	Compute the baseline for the current line, given the max ascent and descent of the
	boxes on the line, and the information saved about the previous line if any.
	In this inverted variant, m_ysEdgeOfSpaceForThisLine is the BOTTOM of the space
	available for the current line, and m_dysPrevBaselineOffset is typically negative,
	indicating that the previous baseline is below that.
	------------------------------------------------------------------------------------------*/
	virtual int BaseLine(int dyMaxDescent, int dyMaxAscent)
	{
		int yBaseLine;
		if (m_fExactLineHeight)
		{

			CalculateExactAscent();

			// If we're about to scrunch the line down to fit in less space than it would
			// naturally occupy, we should only do so if we're dealing with string boxes.
			// Picture boxes (etc.) should not be scrunched. Instead they should be expanded,
			// if necessary to be an even multiple of the exact linespacing.
			if (SomethingOtherThanAStringThatIsTooBigForAnExactSpaceLine(dyMaxAscent + dyMaxDescent))
			{
				int dyTotalLineHeight = dyMaxAscent + dyMaxDescent;
				int nRoundUp = (dyTotalLineHeight % m_dypLineHeight == 0) ? 0 : 1;
				int yTopOfLine = m_ysEdgeOfSpaceForThisLine - (dyTotalLineHeight / m_dypLineHeight + nRoundUp) * m_dypLineHeight;
				// Set the baseline the exact distance below where we want the top of the line.
				// Enhance JohnT: this is a first crude approximation, a mirror image of the normal algorithm.
				// But in fact, we probably want pictures to extend 'above' the baseline in inverted text, too.
				// (We also need a way to reverse the rotation effect for pictures in vertical text.)
				// (Remember to change the call to ForceAscentForExactLineSpacing() to be consistent)
				yBaseLine = yTopOfLine + m_dyExactAscent;
			}
			else
			{
				// otherwise our new baseline is the exact desired DESCENT ABOVE the bottom.
				int dyExactDescent = m_dypLineHeight - m_dyExactAscent;
				yBaseLine = m_ysEdgeOfSpaceForThisLine - dyExactDescent;
			}
			// Some kinds of boxes (such as string boxes) will just ignore this, but if this is a picture box or pile
			// box or something that lacks a sensible default, we can force the ascent to be what we want.
			m_pboxStartLine->ForceAscentForExactLineSpacing(m_dyExactAscent);

//			if (m_pboxStartLine == m_pboxFirst)
//				yBaseLine = m_ysEdgeOfSpaceForThisLine + m_dyExactAscent;
//			else
//				yBaseLine = m_yBaseLine + m_dypLineHeight;
		}
		else if (m_pboxStartLine == m_pboxFirst)
		{
			// We are on the first line; baseline is just the descent of the segments
			// plus any GapBottom already stored in m_ysEdgeOfSpaceForThisLine.
			yBaseLine = m_ysEdgeOfSpaceForThisLine - dyMaxDescent;
		}
		else
		{
			// The first argument computes the baseline as the preferred distance from the previous baseline
			// (Note LineHeight is in 1000/pt, distances are in twips; m_dysPrevBaselineOffset is typically
			// minus the ascent of the previous line, subtracting it gives a larger number, putting the
			// previous baseline below the top of the previous line.)
			// The second puts it just far enough above the bottom to fit the descenders.
			// Min makes it the higher of the two, that is, the larger separation.
			yBaseLine = min(m_ysEdgeOfSpaceForThisLine - m_dysPrevBaselineOffset - m_dypLineHeight,
				m_ysEdgeOfSpaceForThisLine - dyMaxDescent);
		}
		return yBaseLine - m_dyTagAbove;
	}

	// Figure m_ysEdgeOfSpaceForThisLine and m_dysPrevBaselineOffset for the next line,
	// given the baseline of the current one and its max ascent and descent.
	// Bottom of next line is at the baseline for the current line, minus the max ascent,
	// plus extra space for tagging if needed...unless we want exact spacing, in
	// which case we use the exact value.
	// Overridden for inverted layout.
	virtual void SetupNextLine(int yBaseLine, int dyMaxDescent, int dyMaxAscent)
	{

		if (m_fExactLineHeight)
		{
			m_ysEdgeOfSpaceForThisLine = yBaseLine - m_dyExactAscent - m_dyTagAbove;
			m_dysPrevBaselineOffset = (-m_dyExactAscent);
		}
		else
		{
			m_ysEdgeOfSpaceForThisLine = yBaseLine - dyMaxAscent - m_dyTagAbove;
			m_dysPrevBaselineOffset = (-dyMaxAscent);
		}
	}
};


//:>********************************************************************************************
//:>	VwParagraphBox Methods
//:>********************************************************************************************
VwParagraphBox::VwParagraphBox(VwPropertyStore * pzvps, VwSourceType vst)
	:VwGroupBox(pzvps)
{
	switch (vst)
	{
	case kvstNormal:
		m_qts.Attach(NewObj VwSimpleTxtSrc);
		break;
	case kvstTagged:
		m_qts.Attach(NewObj VwOverlayTxtSrc);
		break;
	case kvstMapped:
		m_qts.Attach(NewObj VwMappedTxtSrc);
		break;
	case kvstMappedTagged:
		m_qts.Attach(NewObj VwMappedOverlayTxtSrc);
		break;
	case kvstConc:
		m_qts.Attach(NewObj VwConcTxtSrc);
		break;
	case kvstOverride:
		{
			// Important: we have to attach the MappedOverlayTxtSrc to a smart pointer
			// instead of creating it directly. That ends up with an extra reference
			// to MappedOverlayTxtSrc and results in ugly memory leaks.
			//m_qts.Attach(NewObj VwOverrideTxtSrc(NewObj VwMappedOverlayTxtSrc));
			VwTxtSrcPtr qtsTmp;
			qtsTmp.Attach(NewObj VwMappedOverlayTxtSrc);
			m_qts.Attach(NewObj VwOverrideTxtSrc(qtsTmp));
			break;
		}
	default:
		Assert(false);
		break;
	}
	m_dympExactAscent = -1;
}

VwParagraphBox::~VwParagraphBox()
{
	VwRootBox * prootb = Root();
	if (prootb)
	{
#ifdef ENABLE_TSF
		prootb->ClearSelectedAnchorPointerTo(this);
#endif /*ENABLE_TSF*/
		Assert(prootb->m_pvpboxNextSpellCheck != this);
	}
}

/*----------------------------------------------------------------------------------------------
	Hilite everything in the box. For paragraphs it generally looks better to highlight the
	lines individually, I think. This gives a ragged right consistend with the look when
	just part of a paragraph is inverted.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::HiliteAll(IVwGraphics * pvg, bool fOn, Rect rcSrcRoot, Rect rcDstRoot)
{
	HiliteAllChildren(pvg, fOn, rcSrcRoot, rcDstRoot);
}

/*----------------------------------------------------------------------------------------------
	Answer true if it is not possible to draw a selection as far through the paragraph as
	ich, because of truncation of the display of the paragraph. Ich is a renderer offset.
----------------------------------------------------------------------------------------------*/
bool VwParagraphBox::IsSelectionTruncated(int ich)
{
	if (!FirstBox() && MaxLines())
	{
		// we haven't laid out the paragraph yet, so it's not truncated. It's just hidden altogether.
		// (this case is needed only for tests)
		return false;
	}
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->Next())
	{
		if (pbox->Top() == knTruncated)
			return true;
		VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
		if (!psbox)
			continue; // can't draw there, but maybe later....
		int dichLim;
		CheckHr(psbox->Segment()->get_Lim(psbox->IchMin(), &dichLim));
		if (ich <= psbox->IchMin() + dichLim)
			return false; // no problem, we have a good segment to draw in
	}
	return true; // If we don't find a box that can draw it, assume we can't.
}

/*----------------------------------------------------------------------------------------------
	Functor for DoAllStringBoxesMethod which draws the selection
----------------------------------------------------------------------------------------------*/
class DrawSelectionBinder
{
private:
	bool m_fOn;
	Rect m_rcBounds; // keeps track of the bounding rectangle of the selection we drew.
	int m_ysTop;
	int m_dysHeight;
	bool m_fDisplayPartialLines;
public:

	DrawSelectionBinder(bool fOn, int ysTop, int dysHeight, bool fDisplayPartialLines)
	{
		m_fOn = fOn;
		m_ysTop = ysTop;
		m_dysHeight = dysHeight;
		m_fDisplayPartialLines = fDisplayPartialLines;
	}

	// True if we want to draw on this line, that is, if the line falls within the
	// range of lines appropriate to this segment.
	// Any rcSrc will do, we only want the layout resolution.
	bool DrawOnThisLine(VwStringBox * psbox, IVwGraphics * pvg, Rect & rcSrc)
	{
		VwParagraphBox* pvpboxParent = dynamic_cast<VwParagraphBox*>(psbox->Container());
		int ysLineTop;
		int ysLineBottom;
		pvpboxParent->GetLineTopAndBottom(pvg, psbox, &ysLineTop, &ysLineBottom,
			rcSrc, rcSrc, false);

		// We want to display the selection if it is inside of the visible range.
		// We make it relative to the rootbox
		int ysParentTop = pvpboxParent->TopToTopOfDocument();
		ysLineTop += ysParentTop;
		ysLineBottom += ysParentTop;
		if (!m_fDisplayPartialLines && (ysLineBottom <= m_ysTop || ysLineBottom > m_ysTop + m_dysHeight))
			return false;
		else if (m_fDisplayPartialLines && ysLineTop >= m_ysTop + m_dysHeight)
			return false;
		return true;
	}

	// Operator to deal with a range
	void operator() (VwStringBox* psbox, IVwGraphics * pvg, Rect & rcSrc1, Rect & rcDst1,
		int & ichMin, int & ichLim, int & ydLineTop, int & ydLineBottom,
		const Rect & rcSrcRoot, const Rect & rcDstRoot, bool fIsLastParaOfSelection)
	{
		if (DrawOnThisLine(psbox, pvg, rcSrc1))
		{
			m_rcBounds.Sum(psbox->DrawRange(pvg, rcSrc1, rcDst1, ichMin,
				ichLim, ydLineTop, ydLineBottom, m_fOn, rcSrcRoot, rcDstRoot, fIsLastParaOfSelection));
		}
	}

	// Operator to deal with empty range at start of non-empty paragraph
	void operator() (VwStringBox* psbox, IVwGraphics * pvg, RECT & rcSrcSeg, RECT & rcDstSBox,
		int & ydLineTop, int & ydLineBottom)
	{
	}

	// Operator to deal with Insertion point
	void operator() (VwStringBox* psbox, IVwGraphics * pvg, Rect & rcSrcSeg, Rect & rcDstSBox,
		int & ichMin, bool & fAssocPrev, LgIPDrawMode & dm)
	{
		if (DrawOnThisLine(psbox, pvg, rcSrcSeg))
		{
			CheckHr(psbox->Segment()->DrawInsertionPoint(psbox->IchMin(), pvg, rcSrcSeg, rcDstSBox,
				ichMin, fAssocPrev, m_fOn, dm));
		}
	}

	Rect GetResults()
	{
		return m_rcBounds;
	}
};

/*----------------------------------------------------------------------------------------------
	Functor for DoAllStringBoxesMethod to determine the location of the selection
----------------------------------------------------------------------------------------------*/
class LocOfSelBinder
{
private:
	Rect m_rdPrimary;
	Rect m_rdSecondary;
	bool m_fSplit;
	bool m_fGotSomething; // Note if we actually get a result.
	Rect m_rdRange; // accumulate range here
	bool m_fFirstRange; // true until we get one range rectangle from some seg

public:
	LocOfSelBinder()
	{
		m_fSplit = false;
		m_fGotSomething = false;
		m_fFirstRange = true;
	}

	// Operator for dealing with a range
	void operator() (VwStringBox* psbox, IVwGraphics * pvg, RECT & rcSrcSeg, RECT & rcDstSBox,
		int & ichMin, int & ichLim, int & ydLineTop, int & ydLineBottom,
		const Rect & rcSrcRoot, const Rect & rcDstRoot, bool fIsLastParaOfSelection)
	{
		Rect rd;
		ComBool fAnything;
		psbox->PositionOfRange(pvg, rcSrcSeg, rcDstSBox,
			ichMin, ichLim, ydLineTop, ydLineBottom, &rd, &fAnything, rcSrcRoot, rcDstRoot,
			fIsLastParaOfSelection);
		if (fAnything)
		{
			if (m_fFirstRange)
			{
				m_fFirstRange = false;
				m_rdRange = rd;
			}
			else
			{
				m_rdRange.Sum(rd);
			}
		}
	}

	// Operator to deal with empty range at start of a non-empty paragraph. This is ok,
	// so set the m_fGotSomething flag.
	void operator() (VwStringBox* psbox, IVwGraphics * pvg, RECT & rcSrcSeg, RECT & rcDstSBox,
		int & ydLineTop, int & ydLineBottom)
	{
		m_rdPrimary = Rect(0, ydLineTop, 0, ydLineBottom);
		m_fGotSomething = true;
	}

	// Operator to deal with Insertion point
	void operator() (VwStringBox* psbox, IVwGraphics *pvg, Rect & rcSrcSeg, Rect & rcDstSBox,
		int & ichMin, bool & fAssocPrev, LgIPDrawMode & dm)
	{
		RECT rdPrim;
		RECT rdSec;
		ComBool fPrim;
		ComBool fSec;
		CheckHr(psbox->Segment()->PositionsOfIP(psbox->IchMin(), pvg, rcSrcSeg, rcDstSBox,
			ichMin, fAssocPrev, dm, &rdPrim, &rdSec, &fPrim, &fSec));
		if (fPrim)
		{
			m_rdPrimary = rdPrim;
			m_fGotSomething = true;
		}
		if (fSec)
		{
			m_rdSecondary = rdSec;
			m_fSplit = true;
			// Does not count as getting something; we need a primary rectangle.
		}
	}

	void GetResults(RECT * prdPrimary, RECT * prdSecondary,  ComBool * pfSplit,
		bool & fGotSomething)
	{
		*prdPrimary = m_rdPrimary;
		*prdSecondary = m_rdSecondary;
		*pfSplit = m_fSplit;
		fGotSomething = m_fGotSomething;

		if (!m_fFirstRange)
		{
			*prdPrimary = m_rdRange;
			fGotSomething = true;
		}
		// else, it was set when we processed the relevant section as an IP.
	}
};

/*----------------------------------------------------------------------------------------------
	Method object that works with functor to draw the selection or get the location of
	a selection.
----------------------------------------------------------------------------------------------*/
class DoAllStringBoxesMethod
{
private:
	VwParagraphBox * m_pvpbox;
	IVwGraphics * m_pvg;
	int & m_ichlogMin;
	int & m_ichlogLim;
	bool & m_fAssocPrev1;
	Rect & m_rcSrcRoot;
	Rect & m_rcDstRoot;
	bool m_fIsInsertionPoint;
	bool m_fIsLastParaOfSelection;

	// Deal with an insertion point. Returns the value of fAssocPrev
	bool HandleIp(VwStringBox* psbox, VwStringBox * psboxPrev, int ichMin, int ichLim,
		LgIPDrawMode dmPrev, LgIPDrawMode & dm)
	{
		// Draw an insertion point
		bool fAssocPrev = m_fAssocPrev1;
		if (ichMin && ichMin == psbox->IchMin() && fAssocPrev)
		{
			// The IP is right at the start of this segment.
			if (psboxPrev)
			{
				// In a case like the segment following a hard line break, we have a
				// psboxPrev, but it is (a) on the previous line, so it shouldn't
				// draw the IP; and (b)
				// it won't, because its range limit is strictly less than ichMin.
				// So we need to force this segment to draw it.
				int dich;
				CheckHr(psboxPrev->Segment()->get_Lim(psboxPrev->IchMin(), &dich));
				// If the previous segment's limit is strictly less than ichMin,
				// there is a missing character (typically the hard line break) that
				// is not represented by any box. We need to draw the IP.
				if (psboxPrev->IchMin() + dich < ichMin)
					fAssocPrev = false;
			}
			else
			{
				// We are trying to associate the IP with the previous character, but
				// there isn't a string box to represent that character, either because
				// we're at the start of the segment, or because there is something
				// like a picture before this string box. Since there is nothing right
				// before us that can draw the IP, we'd better draw it.
				fAssocPrev = false;
			}
		}
		if ((!dynamic_cast<VwStringBox *>(psbox->Next())) && (!m_fAssocPrev1))
		{
			// Next box is embedded and sel is oriented towards it--is it at boundary?
			int dichLim;
			CheckHr(psbox->Segment()->get_Lim(psbox->IchMin(), &dichLim));
			if (ichMin == psbox->IchMin() + dichLim)
				fAssocPrev = true; // override to draw in this seg.
		}
		// Did we figure a special mode at the end of the previous segment?
		// If so, use the complementary mode for the start of this.
		if (dmPrev != kdmNormal)
		{
			dm = (dmPrev == kdmSplitPrimary ? kdmSplitSecondary : kdmSplitPrimary);
			return fAssocPrev;
		}
		// are we at the end of a segment?
		int ichLimSeg;
		HRESULT hr;
		IgnoreHr(hr = psbox->Segment()->get_Lim(psbox->IchMin(), &ichLimSeg));
		if (FAILED(hr))
			return fAssocPrev; // If anything goes wrong just draw normal I-beam
		ichLimSeg += psbox->IchMin();
		if (ichMin != ichLimSeg)
			return fAssocPrev;  // not a segment boundary, can't be WS boundary

		{ // BLOCK
			// OK, we are at the end of the segment for psbox
			// Is it at the corresponding physical boundary?
			ComBool fRtoL;
			IgnoreHr(hr = psbox->Segment()->get_RightToLeft(psbox->IchMin(), &fRtoL));
			if (FAILED(hr))
				return fAssocPrev;
			ComBool fCoincide;
			// arg 3 says asking about logical end of segment; arg 4 says asking about the
			// physical end that would normally correspond with it
			IgnoreHr(hr = psbox->Segment()->DoBoundariesCoincide(psbox->IchMin(), m_pvg, true,
				fRtoL, &fCoincide));
			if (FAILED(hr))
				return fAssocPrev;
			if (fCoincide)
			{
				// they are the same in this WS run. What about the next one?
				VwStringBox * psboxNext;
				psboxNext = dynamic_cast<VwStringBox *>(psbox->Next());
				if (!psboxNext)
					return fAssocPrev; // no following segment, no split cursor
				ComBool fRtoLNext;
				IgnoreHr(hr = psboxNext->Segment()->get_RightToLeft(psboxNext->IchMin(),
					&fRtoLNext));
				if (FAILED(hr))
					return fAssocPrev;
				if (fRtoLNext == fRtoL)
				{
					// segments in the same direction. All may be well if the second
					// segment's logical start coincides with its physical one.
					// REVIEW SharonC (JohnT): what if the two segments are in the same direction, but due
					// to embedding they are ordered unexpectedly?
					IgnoreHr(hr = psboxNext->Segment()->DoBoundariesCoincide(
						psboxNext->IchMin(), m_pvg, false, fRtoL, &fCoincide));
					if (FAILED(hr))
						return fAssocPrev;
					if (fCoincide)
						return fAssocPrev;  // normal IP at the boundary
				}
			}
		}
		// If we get here we need a cursor split between the two.
		// We are in the preceding segment. It will get the primary cursor if the IP is
		// associated with the preceding character.
		dm = fAssocPrev ? kdmSplitPrimary : kdmSplitSecondary;
		return fAssocPrev;
	}

public:
	DoAllStringBoxesMethod(VwParagraphBox* pvpBox, IVwGraphics * pvg, int & ichlogMin,
		int & ichlogLim, bool & fAssocPrev1, Rect & rcSrcRoot, Rect & rcDstRoot,
		bool fIsInsertionPoint, bool fIsLastParaOfSelection):
			m_pvpbox(pvpBox), m_pvg(pvg), m_ichlogMin(ichlogMin), m_ichlogLim(ichlogLim),
			m_fAssocPrev1(fAssocPrev1), m_rcSrcRoot(rcSrcRoot), m_rcDstRoot(rcDstRoot),
			m_fIsInsertionPoint(fIsInsertionPoint), m_fIsLastParaOfSelection(fIsLastParaOfSelection)
	{
	}

	template<class Op> void Run(Op & f)
	{
		int ichMin = m_pvpbox->Source()->LogToRen(m_ichlogMin);
		int ichLim = m_pvpbox->Source()->LogToRen(m_ichlogLim);
		ITsTextPropsPtr qttpAfter;
		ITsTextPropsPtr qttpBefore;
		// Loop over all your string boxes. This ensures we hit all the boxes whose segments
		// might be involved in drawing the selection.
		// OPTIMIZE JohnT: could we safely stop sooner?
		// For messing with IPs at WS boundaries, it is useful to know the mode in which
		// we drew the IP for the previous segment. For the first segment we can behave as
		// if nothing special happened in the previous one.
		LgIPDrawMode dmPrev = kdmNormal;
		VwStringBox * psbox;
		VwStringBox * psboxPrev = NULL;
		for (VwBox * pbox = m_pvpbox->FirstBox(); pbox; psboxPrev = psbox, pbox = pbox->Next())
		{
			psbox = dynamic_cast<VwStringBox *>(pbox);
			if (!psbox)
				continue;
			Rect rcSrcSeg; // rect to pass to segments
			Rect rcDstSBox;
			psbox->CoordTransFromRoot(m_pvg, m_rcSrcRoot, m_rcDstRoot, &rcSrcSeg, &rcDstSBox);
			rcSrcSeg.Offset(-psbox->Left() - psbox->GapLeft(m_rcSrcRoot.Width()),
				-psbox->Top() - psbox->GapTop(m_rcSrcRoot.Height()));
			if (ichMin == ichLim && m_fIsInsertionPoint)
			{
				LgIPDrawMode dm = kdmNormal;
				bool fAssocPrev = HandleIp(psbox, psboxPrev, ichMin, ichLim, dmPrev, dm);
				f(psbox, m_pvg, rcSrcSeg, rcDstSBox, ichMin, fAssocPrev, dm);
				dmPrev = dm;
			}
			else
			{
				int ydLineTop;
				int ydLineBottom;
				m_pvpbox->GetLineTopAndBottom(m_pvg, psbox, &ydLineTop, &ydLineBottom,
					m_rcSrcRoot, m_rcDstRoot);

				// Draw a range, unless it's an empty range at the start of a non-empty paragraph.
				// (We want to draw a range at the start of an empty paragraph, since that's how we
				// get a little blob to show that the empty paragraph is included in the selection.)
				if (ichMin == 0 && ichLim == 0 && m_pvpbox->Source()->Cch() != 0)
				{	// empty range at start of non-empty paragraph is ok
					f(psbox, m_pvg, rcSrcSeg, rcDstSBox, ydLineTop, ydLineBottom);
					return;
				}
				else
				{
					f(psbox, m_pvg, rcSrcSeg, rcDstSBox, ichMin, ichLim, ydLineTop, ydLineBottom,
						m_rcSrcRoot, m_rcDstRoot, m_fIsLastParaOfSelection);
				}
			}
		}
	}
};

/*----------------------------------------------------------------------------------------------
	Show the selection on the screen (in the indicated graphics context). The selection runs
	from ichMin to ichLim, and if the two are the same, it is associated with the logically
	preceding character if fAssocPrev is true, otherwise with the following one. The selection
	is turning on if fOn is true and off otherwise; implementations may also ignore this, and
	just invert each time this method is called. The character offsets are relative to this
	box.
	The isInsertionPoint flag allows the code to tell whether we are drawing an insertion point.
	This may not be true, even if the paragraph is empty and ichLogLim == ichLogMin, if the
	selection extends into other paragraphs.
	ysTop and dysHeight specify a vertical range that should be drawn.
	ysTop is a distance from the top of the rootbox, in the same resolution as the original
	layout (and hence, the same as the box's own top, height, etc).
	dysHeight is, at the same resolution, the height of the region to draw.

	DrawSelection returns the bounds of the drawn selection.
----------------------------------------------------------------------------------------------*/
Rect VwParagraphBox::DrawSelection(IVwGraphics * pvg, int ichlogMin, int ichlogLim,
	bool fAssocPrev1, bool fOn, Rect rcSrcRoot, Rect rcDstRoot, bool fIsInsertionPoint,
	bool fIsLastParaOfSelection, int ysTop, int dysHeight, bool fDisplayPartialLines)
{
	DrawSelectionBinder drawSelectionBinder(fOn, ysTop, dysHeight, fDisplayPartialLines);
	DoAllStringBoxesMethod doAllStringBoxes(this, pvg, ichlogMin, ichlogLim,
		fAssocPrev1, rcSrcRoot, rcDstRoot, fIsInsertionPoint, fIsLastParaOfSelection);
	doAllStringBoxes.Run(drawSelectionBinder);

	return drawSelectionBinder.GetResults();
}

/*----------------------------------------------------------------------------------------------
	Figure the location on the screen where the selection will be drawn. Parameters are similar
	to DrawSelection, except that fOn is omitted (we don't care); we add the three arguments in
	which the result is returned (see View.idh::VwSelection::Location). fIsLastParaOfSelection
	is false if the selection continues in the next paragraph. In that case we want to draw
	an indicator at the end of the paragraph.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::LocOfSelection(IVwGraphics * pvg, int ichlogMin, int ichlogLim,
	bool fAssocPrev1, Rect rcSrcRoot, Rect rcDstRoot, RECT * prdPrimary, RECT * prdSecondary,
	ComBool * pfSplit, bool fIsInsertionPoint, bool fIsLastParaOfSelection)
{

	LocOfSelBinder locOfSelBinder;
	DoAllStringBoxesMethod doAllStringBoxes(this, pvg, ichlogMin, ichlogLim,
		fAssocPrev1, rcSrcRoot, rcDstRoot, fIsInsertionPoint, fIsLastParaOfSelection);
	doAllStringBoxes.Run(locOfSelBinder);

	bool fGotSomething;
	locOfSelBinder.GetResults(prdPrimary, prdSecondary, pfSplit, fGotSomething);

	// We should have found a location. If we didn't, the whole rectangle for this box is some
	// sort of answer. This would tend to indicate a defect in the rendering engine.
	if (!fGotSomething)
	{
		// We get this many times for no good reason. Warn("Finding selection location failed");
		*prdPrimary = GetBoundsRect(pvg, rcSrcRoot, rcDstRoot);
	}
}

/*----------------------------------------------------------------------------------------------
	Given a character position and an indication of whether it is most associated with the
	previous or following character, determine which property of which object to edit,
	and return the object, the property, and the range of characters within this para box.
	Also return the view constructor and fragment identifier that should be used to parse
	the edited string, if any, and the actual notifier and property index.
	Return an indication of what you found. It should be an editable property if possible,
	otherwise any property that covers the given position.
	Caller gets a ref count on panote, ptssProp, and pvvc.
	All char indexes here are logical.
----------------------------------------------------------------------------------------------*/
VwEditPropVal VwParagraphBox::EditableSubstringAt(int ichMin, int ichLim, bool fAssocBefore,
	HVO * phvo, int * ptag, int * pichMin, int * pichLim,
	IVwViewConstructor ** ppvvc, int * pfrag, VwAbstractNotifier ** ppanote, int * piprop,
	VwNoteProps * pvnp, int * pitssProp, ITsString ** pptssProp)
{
	// Get all the notifiers which have anything to do with this para box.
	NotifierVec vpanote;
	Container()->GetNotifiers(this, vpanote);
	// Of those notifiers, some don't cover position ich at all.
	// Of the ones that do, some cover it, but that range is a complex property,
	// so there is something more local that covers it.
	// If it is in the middle of a range, there should be only one basic
	// property that covers it.
	// If it is at a boundary, there could be several: there could be multiple
	// empty basic and complex properties at that offset.
	// If fAssocBefore is true, we want the logically first of those basic properties
	// (excluding kNotAnAttr items); otherwise, we want the logically last.
	// To further complicate things, the candidate properties could be in different
	// notifiers (if it is at an object boundary, as well as a property one), and
	// conceivably they could be different types of notifiers, so we have to handle
	// it with a method on notifier.
	// The solution is that each notifier knows how to find the best option within
	// itself; this routine then selects the logically first of those if fAssocPrev is
	// true, otherwise the logically last
	VwAbstractNotifierPtr qanote;
	VwAbstractNotifierPtr qanoteBest;
	int tag;
	int ichMinProp;
	int ichLimProp;
	IVwViewConstructorPtr qvvc;
	int frag;
	HVO hvo;
	int iprop; // index of property in newly tried notifier that has substring
	VwNoteProps vnp;
	int itssProp;
	ITsStringPtr qtssProp;
	int ipropBest = 0; // same index in current best notifier
	VwEditPropVal vepvBest = kvepvNone;

	// See what our properties have to say about editability for ich and, if possible, ich-1
	ITsStringPtr qtssT;
	VwPropertyStorePtr qzvpsT;
	int itssT;
	Source()->StringFromIch(ichMin, false, &qtssT, &ichMinProp, &ichLimProp, &qzvpsT, &itssT);

	for (int i = 0; i < vpanote.Size(); i++)
	{
		qanote = vpanote[i];
		VwParagraphBox * pvpboxThis = this;
		VwEditPropVal vepv;
		vepv = qanote->EditableSubstringAt(pvpboxThis, ichMin, ichLim, fAssocBefore,
			&hvo, &tag, &ichMinProp, &ichLimProp, &qvvc, &frag, &iprop, &vnp,
			&itssProp, &qtssProp);
		// If the notifier doesn't cover this property at all skip it.
		if (vepv == kvepvNone)
			continue;

		// The notifier knows _something_ about this property. Is it preferable to what we
		// already have, if anything?
		if (qanoteBest)
		{
			// Figure how they are related.
			int ipropChild; // if one notifier is an ancestor of the other
			VwNoteRel vnr = qanoteBest->HowRelated(qanote, &ipropChild);
			switch(vnr)
			{
			default:
				ThrowHr(WarnHr(E_UNEXPECTED));
			case kvnrBefore:
				// Unless one of them is an object level property, we should only get two
				// matches if we're at the boundary of both of them.
				Assert(vepv == kvepvNonStringProp || vepvBest == kvepvNonStringProp
					|| ichMin == ichMinProp || ichLim == ichLimProp);
				// the new notifier is before the current best.
				// If one is more editable, prefer it.
				if (vepvBest > vepv)
					continue; // keep current editable one
				if (vepvBest < vepv)
					break; // prefer new editable one
				// Same degree of editability, result determined by preferred direction.
				if (!fAssocBefore)
				{
					// we want the last, so keep the current best
					continue;
				}
				break;
			case kvnrAfter:
				Assert(vepv == kvepvNonStringProp || vepvBest == kvepvNonStringProp
					|| ichMin == ichMinProp || ichLim == ichLimProp);
				// the new notifier is after the current best
				// If one is more editable, prefer it.
				if (vepvBest > vepv)
					continue; // keep current editable one
				if (vepvBest < vepv)
					break; // prefer new editable one
				if (fAssocBefore)
				{
					// we want the first, so keep the current best
					continue;
				}
				break;
			case kvnrDescendant:
				// The new node is a descendant of the current best, in prop ipropChild of the
				// current best.
				// The most common case is that ipropChild == iprop, the property of the
				// ancestor that was our previous best. In this case, we want the descendant,
				// the more specific notifier that hopefully has an actual editable string property.
				// However, it's possible that we have a string that is part of an embedded object
				// (the descendant) adjacent to an ordinary string property (the ancestor).
				// Since the IP is right at the boundary (that's why they both offer at least
				// some editability), we prefer the one that fAssocBefore indicates.
				// ipropChild is the index of the property of qanoteBest that contains the
				// descendant, and ipropBest is the property (typically adjacent) of ipropBest
				// that is editable. The most desirable one is therefore determined by
				// fAssocBefore as indicated here.
				// an example of where this matters is the footnote marker in TE footnotes.
				// If one of them only matched the box, prefer the other.
				if (vepvBest > kvepvNonStringProp && vepv <= kvepvNonStringProp)
					continue; // keep current (more editable) one
				if (vepvBest <= kvepvNonStringProp && vepv > kvepvNonStringProp)
					break; // prefer new (more editable) one
				// If they are both non-string, prefer the more specific
				if (vepvBest == kvepvNonStringProp && vepv == kvepvNonStringProp)
					break; // replace ancestor with new descendant.
				if ((fAssocBefore && ipropChild > ipropBest) ||
					(!fAssocBefore && ipropChild < ipropBest))
				{
					continue;
				}
				break;
			case kvnrAncestor:
				// The new notifier is an ancestor of the current best,
				// which is in prop ipropChild of the new one
				// the new property is after the previous one if iprop is greater than
				// ipropChild
				// Note that in THIS case, if iprop == ipropChild, we need to continue,
				// so as NOT to replace the child in qanoteBest with its ancestor in qanote.
				// If one of them only matched the box, prefer the other.
				if (vepvBest > kvepvNonStringProp && vepv <= kvepvNonStringProp)
					continue; // keep current (more editable) one
				if (vepvBest <= kvepvNonStringProp && vepv > kvepvNonStringProp)
					break; // prefer new (more editable) one
				// If they are both non-string, prefer the more specific
				if (vepvBest == kvepvNonStringProp && vepv == kvepvNonStringProp)
					continue; // keep current descendant.
				if ((fAssocBefore && iprop >= ipropChild) ||
					(!fAssocBefore && iprop <= ipropChild))
				{
					continue;
				}
				break;
			}
		}

		// If we get here, we did not continue, so we got a new notifier which is either the
		// first option we found, or better than any previous one
		qanoteBest = qanote;
		ipropBest = iprop;
		vepvBest = vepv;

		// Assign all the parameters.
		*ptag = tag;
		*pichMin = ichMinProp;
		*pichLim = ichLimProp;
		*pfrag = frag;
		*piprop = ipropBest;
		*phvo = hvo;
		ReleaseObj(*ppvvc);
		*ppvvc = qvvc.Detach();
		ReleaseObj(*ppanote);
		*ppanote = qanoteBest;
		AddRefObj(*ppanote);
		*pvnp = vnp;
		*pitssProp = itssProp;
		ReleaseObj(*pptssProp);
		*pptssProp = qtssProp.Detach();
	} // loop considering all notifiers

	return vepvBest;
}

/*----------------------------------------------------------------------------------------------
	Replace the strings from itssMin to itssLim (relative to the list of strings associated
	with this para) with (all of) the strings in pvpboxRep.
	Make all relevant changes to the display (but not to affected notifiers--
	caller is responsible for that!).
	Note that itssLim may be -1 to indicate the last string associated with this.
	Also deletes any boxes associated with the deleted strings, after cleaning up notifiers
	associated with them. Boxes deleted (other than simple string layout boxes) are
	returned in pboxsetDeleted. If it is known that no non-layout boxes will be deleted,
	the last two arguments may be omitted (or passed as null).

	@param itssMin/Lim	range of strings changed (in the original text source)
	@param pvpboxRep	dummy box containing a list of replacement boxes, and the
							new text source
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::ReplaceStrings(IVwGraphics * pvg, int itssMin, int itssLim,
	VwParagraphBox * pvpboxRep, VwNotifier * pnote, NotifierVec * pvpanoteDel,
	BoxSet * pboxsetDeleted)
{
	int cLinesToSave = 0;
	bool fComplete = true; // Do we need to do a complete relayout? Assume so unless special conditions apply.
	bool fForceComplete = false;
	VwBox * pboxStartReplace = NULL;
	VwBox * pboxStartReplacePrev;
	int dyStartReplace;
	int dyStartReplacePrev;
	int dyPrevDescent;
	int dyPrevDescentPrev;
	int cchOld, cchNew;
	VwOverrideTxtSrc * pctsOverride = dynamic_cast<VwOverrideTxtSrc *>(Source());
	if (pctsOverride != NULL)
	{
		// We can't safely update an override text source; the overrides might not be
		// in the right places any more! Switch back to the original. Hopefully something
		// (e.g., fresh spelling check) will reinstate it if needed.
		m_qts = pctsOverride->EmbeddedSrc();
		Root()->ResetSpellCheck(); // make sure we redo this, even if 'nothing' changes.
		Invalidate(); // Changing the source may change spelling appearance, force paint.
		fForceComplete = true;
	}
	int ichRenLim, ichRenMin;
	CheckHr(Source()->LogToRen(Source()->IchStartString(itssLim), &ichRenLim));
	CheckHr(Source()->LogToRen(Source()->IchStartString(itssMin), &ichRenMin));
	cchOld = ichRenLim - ichRenMin;
	pvpboxRep->Source()->get_Length(&cchNew);
	int ichMinDiff = 0;
	int ichLimDiff = cchNew;
	int cchLenDiff = cchNew - cchOld;
	int ctssOld = Source()->CStrings();
	int ctssNew = pvpboxRep->Source()->CStrings();
#ifdef ENABLE_TSF
	IViewInputMgr * pvim = Root()->InputManager();
#endif /*ENABLE_TSF*/
	VwBox * pboxFirstSubRep = pvpboxRep->FirstBox();

	ITsStringPtr qtssOld;
	ITsStringPtr qtssNew;
	if (ctssOld > itssMin)
		Source()->StringAtIndex(itssMin, &qtssOld);
	if (ctssNew > 0)
		pvpboxRep->Source()->StringAtIndex(0, &qtssNew);
	bool fCurrentSelectionChanged = Root()->FixSelectionsForStringReplacement(Source(), itssMin, itssLim, pvpboxRep->Source());

	if (!fForceComplete && // Don't HAVE to do complete layout because we already changed the TxtSource.
		ctssNew == 1 &&	// exactly one new string
		itssMin + 1 == itssLim &&	// and exactly one replaced
		qtssOld.Ptr() && qtssNew.Ptr())	// and both the old and new versions are non-null (no embedded boxes involved)
	{
		CompareSourceStrings(Source(), pvpboxRep->Source(), itssMin,
			&ichMinDiff, &ichLimDiff);

		if (ichMinDiff == -1 && ichMinDiff == -1)
		{
			// Do the actual change anyway. This is not expensive and it protects against any
			// subtle thing that CompareSourceStrings may have missed. One I (JohnT) know it
			// misses is replacing one empty string with another that has different properties.
			// We need to really do that change because it affects the properties the user will
			// type with. Another case is replacing a source that marks spelling errors with
			// one that does not (yet). That can happen after adding a word to the spelling
			// dictionary. It requires a paint.
			Source()->ReplaceContents(itssMin, itssLim, pvpboxRep->Source());
			Assert(cchNew == cchOld);
			return;	// no change needed
		}

		// Diff positions are relative to the string itssMin in the text source. We need the
		// positions relative to the entire text source.
		ichMinDiff += ichRenMin;
		ichLimDiff += ichRenMin;

		fComplete = (ichMinDiff == 0 && ichLimDiff == cchNew);
		cLinesToSave = FindFirstBoxOnLine(pvg, ichMinDiff,
			&pboxStartReplace, &dyStartReplace, &dyPrevDescent,
			&pboxStartReplacePrev, &dyStartReplacePrev, &dyPrevDescentPrev);
		if (!pboxStartReplace)
		{
			// This can happen, for example, when the change is after the limit of visible
			// lines in the browse view (the user must have edited in some other view).
			// It signifies that the change is not visible, and we just need to update the
			// string collection.
			Source()->ReplaceContents(itssMin, itssLim, pvpboxRep->Source());
#ifdef ENABLE_TSF
			if (pvim)
				CheckHr(pvim->OnTextChange());
#endif /*ENABLE_TSF*/
			return;
		}
		// We may need to regenerate one line earlier, to handle white space and cross-line
		// contextualization. But if there is already white space on the line before the
		// change, assume just starting at the current line is fine.
		if (pboxStartReplacePrev &&
			!WsNearStartOfLine(Source(), pboxStartReplace, ichMinDiff))
		{
			pboxStartReplace = pboxStartReplacePrev; // regenerate starting at previous line
			dyStartReplace = dyStartReplacePrev;
			dyPrevDescent = dyPrevDescentPrev;
			cLinesToSave--;
		}
	}

	// Remember the locations of this box, all its containers, and
	// all following boxes.
	// ENHANCE JohnT: this code more-or-less duplicates the code for the same purpose in
	// VwNotifier::ReplaceBoxes. Can we factor it out? Also, in rare case both will
	// actually get executed, leading to duplication of effort; can we safely prevent that?
	// Two fixmaps are useful.
	// The first records this box and all its containers. These boxes need to have
	// their own layout redone, though embedded boxes.
	FixupMap fixmap;
	VwBox * pboxContainer;
	for (pboxContainer = this; pboxContainer; pboxContainer = pboxContainer->Container())
	{
		if (pboxContainer == this && !fComplete)
		{
			// We're doing a partial relayout; don't put this box in the fixmap.
		}
		else
		{
			Rect vwrect = pboxContainer->GetInvalidateRect();
			fixmap.Insert(pboxContainer, vwrect);
		}
	}

	if (fComplete)
	{
		// Complete re-layout.

		// Get rid of any extra boxes produced by layout. Get back to one box per empty slot
		// in the text source.
		DiscardLayoutBoxes();
		// Figure out which of those boxes will be replaced.
		int cboxesBefore = 0;
		int ibox;
		for (ibox = 0; ibox < itssMin; ibox++)
		{
			ITsStringPtr qtss;
			Source()->StringAtIndex(ibox, &qtss);
			if (!qtss)
				cboxesBefore++;
		}
		int cboxesRep = 0; // number of boxes to replace (in the deleted range)
		for (; ibox < itssLim; ibox++)
		{
			ITsStringPtr qtss;
			Source()->StringAtIndex(ibox, &qtss);
			if (!qtss)
				cboxesRep++;
		}
		VwBox * pboxPrev = NULL;
		if (cboxesBefore)
		{
			pboxPrev = FirstBox();
			// Advance by cboxesBefore - 1 boxes
			while (--cboxesBefore > 0)
				pboxPrev = pboxPrev->Next();
		}
		VwBox * pboxLim = NULL;
		// To delete no boxes at all (is cboxesRep is 0), the limit should be the very first
		// embedded box (if pboxPrev is null) or the one right after it, if any.
		// To delete more boxes than that, we must follow that many Next() pointers.
		if (pboxPrev)
			pboxLim = pboxPrev->Next();
		else
			pboxLim = FirstBox(); // may also be null.
		while (cboxesRep > 0)
		{
			pboxLim = pboxLim->Next();
			cboxesRep--;
		}
		VwBox * pboxDel = NULL;
		if (FirstBox() || pvpboxRep->FirstBox())
		{
			// We mention the superclass explicitly because RelinkBoxes is not normally used for
			// paragraph boxes, and has an override with Assert(false).
			pboxDel = VwGroupBox::RelinkBoxes(pboxPrev, pboxLim, pvpboxRep->FirstBox(),
				pvpboxRep->LastBox());
		}
		// Detach the new boxes from the old paragraph, so they don't get deleted with it.
		pvpboxRep->RemoveAllBoxes();

		// Now do the replacement in the text source
		Source()->ReplaceContents(itssMin, itssLim, pvpboxRep->Source());

		// Delete the replaced sub-boxes, if any, together with their notifiers.
		// Fix any higher-level notifiers that point at the first of them.
		// Invalidate or fix any selections that point at them.
		// It's important to do this BEFORE RelayoutRoot, as that notifies the client
		// of the change of size, and the client might try to do something with an invalid
		// selection.
		if (pboxDel && pnote)
			pnote->DeleteBoxes(pboxDel, pboxDel->EndOfChain(), pnote->Level()+1, pboxFirstSubRep, *pvpanoteDel, pboxsetDeleted);

		// Force layout to recompute size of this box; also replaces all VwStringBoxes, so we
		// don't have to worry about having messed up their offsets etc.
		m_dysHeight = 0;
		VwRootBox * prootb = Root();
		prootb->RelayoutRoot(pvg, &fixmap);

#ifdef ENABLE_TSF
		if (pvim)
			CheckHr(pvim->OnTextChange());
#endif /*ENABLE_TSF*/
		if (fCurrentSelectionChanged && Root()->Selection())
			Root()->Selection()->CommitAndNotify(ksctSamePara, Root());
		return;
	}
	else
	{
		VwRootBox * prootb = Root();
		Rect vwrectOrig = GetInvalidateRect();
		// This might look as if it is duplicated down below, but we need both because the
		// rectangles may be different sizes as a result of the layout, and either might be
		// larger. If we really want to do only one invalidate we should compute the union of
		// the before and after rectangles.
		prootb->InvalidateRect(&vwrectOrig);
		int dysHeight = m_dysHeight;
		int dxsWidth = m_dxsWidth;

		Source()->ReplaceContents(itssMin, itssLim, pvpboxRep->Source());

		DoPartialLayout(pvg, pboxStartReplace, cLinesToSave, dyStartReplace, dyPrevDescent,
			ichMinDiff, ichLimDiff, cchLenDiff);

		// If height and width didn't change, no need to recompute containers.
		if (NoSignificantSizeChange(dysHeight, dxsWidth))
		{
			if (dxsWidth < m_dxsWidth)
			{
				// But if the width increased, need to invalidate the new rectangle.
				Rect vwrectNew = GetInvalidateRect();
				prootb->InvalidateRect(&vwrectNew);
			}
#ifdef ENABLE_TSF
			if (pvim)
				CheckHr(pvim->OnTextChange());
#endif /*ENABLE_TSF*/
			if (fCurrentSelectionChanged && Root()->Selection())
				Root()->Selection()->CommitAndNotify(ksctSamePara, Root());
			// Since the size didn't change, we only need to fix pages where this box is a
			// boundary.
			prootb->SendPageNotifications(this);
			return;
		}

		// We want it in the fixmap so as to detect, in Relayout(), that this box is the
		// changed one, and inform any consequently broken pages. But we don't want it
		// to really do a layout, since that is already done. The empty rectangle serves
		// as a flag.
		VwBox * pboxKey = this;
		Rect rectEmpty(0,0,0,0);
		fixmap.Insert(pboxKey, rectEmpty);

		prootb->RelayoutRoot(pvg, &fixmap);

		// If you get a crash here verify that you don't have a recursive call to the layout
		// code (e.g. set a breakpoint in SimpleRootSite.SizeChanged). The call to RelayoutRoot
		// should not destroy any paragraph boxes - if it does it is very likely that something
		// else is going wrong (c.f. TE-4889).
		Rect vwrectNew = GetInvalidateRect();
		Root()->InvalidateRect(&vwrectNew);

#ifdef ENABLE_TSF
		if (pvim)
			CheckHr(pvim->OnTextChange());
#endif /*ENABLE_TSF*/
		if (fCurrentSelectionChanged && Root()->Selection())
			Root()->Selection()->CommitAndNotify(ksctSamePara, Root());
		return;
	}
}

// Return true if the box has not changed its size significantly since when it was
// dysHeight by dysWidth.
// A change of width is not significant if all containers are piles.
bool VwParagraphBox::NoSignificantSizeChange(int dysHeight, int dxsWidth)
{
	if (dysHeight != m_dysHeight)
		return false;
	if (dxsWidth == m_dxsWidth)
		return true;
	for (VwGroupBox * pvgbox = Container(); pvgbox; pvgbox = pvgbox->Container())
	{
		if (!pvgbox->IsPileBox())
			return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return true if there is white space at the beginning of the line before where the
	change was made.
----------------------------------------------------------------------------------------------*/
bool VwParagraphBox::WsNearStartOfLine(IVwTextSource * pts, VwBox * pboxStartOfLine,
	int ichChange)
{
	VwStringBox * psbox = dynamic_cast<VwStringBox *>(pboxStartOfLine);
	if (!psbox)
		return true;

	// NOTE: This depends only on the Unicode general category, not on the language.
	ILgCharacterPropertyEnginePtr qchprpeng;
	qchprpeng.CreateInstance(CLSID_LgIcuCharPropEngine);

	int ichMin = psbox->IchMin();
	ComBool fIsSep;

	OLECHAR * rgch = NewObj OLECHAR[ichChange - ichMin];
	CheckHr(pts->Fetch(ichMin, ichChange, rgch));
	for (int ich = 0; ich < ichChange - ichMin; ich++)
	{
		CheckHr(qchprpeng->get_IsSeparator(rgch[ich], &fIsSep));
		if (fIsSep)
		{
			delete[] rgch;
			return true;
		}
	}
	delete[] rgch;
	return false;
}

/*----------------------------------------------------------------------------------------------
	Answer the ascent of the paragraph, currently the ascent of its first line.

	ENHANCE: This is a rather arbitrary definition, we may want better for some sorts of
	alignment.
----------------------------------------------------------------------------------------------*/
int VwParagraphBox::Ascent()
{
	// we can use the first box as indicative of the ascent of the
	// entire first line because of the way the lines are laid out
	if (m_pboxFirst)
		return (m_pboxFirst->Ascent() + m_pboxFirst->Top());
	else
		return VwGroupBox::Ascent();
}

/*----------------------------------------------------------------------------------------------
	Answer the maximum number of lines this paragraph can occupy. Takes account of previous
	paragraphs in the same container, if the container has a limit specified.
----------------------------------------------------------------------------------------------*/
int VwParagraphBox::MaxLines()
{
	int nlinesMax = m_qzvps->MaxLines();
	if (nlinesMax == INT_MAX)
		return nlinesMax;
	if (!Container())
		return nlinesMax;
	int nlinesMaxCont = Container()->Style()->MaxLines();
	if (nlinesMaxCont == INT_MAX)
		return nlinesMax; // use our own limit unmodified if container does not have one
	for (VwBox * pbox = Container()->FirstRealBox(); pbox != this; pbox=pbox->NextRealBox())
	{
		Assert(pbox);
		nlinesMaxCont -= pbox->CLines();
		if (nlinesMaxCont <= 0)
			return 0;
	}
	// Max is either our own inherent max, or what's left of the container's limit, whichever
	// is less.
	return std::min(nlinesMax, nlinesMaxCont);
}

/*----------------------------------------------------------------------------------------------
	Answer the number of lines this paragraph occupies.
----------------------------------------------------------------------------------------------*/
int VwParagraphBox::CLines()
{
	int ysBaseline = -1; // above any possible real baseline for first box
	VwBox * pbox;
	int clines = 0;
	for (pbox = m_pboxFirst; pbox; pbox = pbox->Next())
	{
		int ysBaselineThis = pbox->Top() + pbox->Ascent();
		if (ysBaselineThis < 0)
		{
			// Extra box not to be drawn (from truncated paragraph): stop here
			break;
		}
		if (ysBaselineThis != ysBaseline)
		{
			// Start of new (or first) line
			ysBaseline = ysBaselineThis;
			clines++;
		}
	}
	return clines;
}

/*----------------------------------------------------------------------------------------------
	Compare the contents of two strings. Return the range (in the second) that is different.
	Both return values will be -1 if the source strings are the same.
	If both strings are empty, but have different properties, return 0 and 0.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::CompareSourceStrings(VwTxtSrc * pts1, VwTxtSrc * pts2, int itss,
	int * pichwMinDiff, int * pichwLimDiff)
{
	Assert(pts2->CStrings() == 1);

	CachedProps * pchrp1;
	CachedProps * pchrp2;
	int ichwRunMin2;
	int ichwRunMin1Orig;
	int cchwTotalLim1;
	int ichwLim1, ichwLim2;

	// Set the start offsets in each string where comparing begins.
	CheckHr(pts1->LogToRen(pts1->IchStartString(itss), &ichwRunMin1Orig));
	ichwRunMin2 = 0;

	// Set the end offsets in each string where comparing stops.
	CheckHr(pts1->LogToRen(pts1->IchStartString(itss + 1), &ichwLim1));
	CheckHr(pts1->get_Length(&cchwTotalLim1));
	CheckHr(pts2->get_Length(&ichwLim2)); // The lim is the length

	wchar * rgchw1 = NewObj wchar[cchwTotalLim1];
	wchar * rgchw2 = NewObj wchar[ichwLim2]; // The lim is the length

	*pichwMinDiff = -1; // not set
	*pichwLimDiff = -1;

	// dummy variables for results GetCharPropInfo wants to return
	int irun1, irun2, isbt1, isbt2;
	int ichwRunLim1, ichwRunLim2;
	ITsTextPropsPtr qttp1, qttp2;
	VwPropertyStorePtr qzvps1, qzvps2;
	int cchwStrLim1 = ichwLim1 - ichwRunMin1Orig;
	int ichwRunMin1 = ichwRunMin1Orig;

	// Search from the beginning of the strings, searching for a difference and keeping
	// track of the first offset where a difference is found (either a difference in
	// properties or a difference in characters).
	while (ichwRunMin1 < ichwLim1 && ichwRunMin2 < ichwLim2)
	{
		irun1 = irun2 = 0;
		pchrp1 = pts1->GetCharPropInfo(ichwRunMin1, &ichwRunMin1, &ichwRunLim1, &isbt1, &irun1, &qttp1, &qzvps1);
		pchrp2 = pts2->GetCharPropInfo(ichwRunMin2, &ichwRunMin2, &ichwRunLim2, &isbt2, &irun2, &qttp2, &qzvps2);

		// Make sure we don't go beyond the characters we care about
		ichwRunLim1 = min(ichwRunLim1, cchwTotalLim1);

		if (pchrp1 != pchrp2)
		{
			// Found a difference in run properties, so the beginning of the run for the
			// text source is where the difference starts.
			Assert(ichwRunMin1 - ichwRunMin1Orig == ichwRunMin2);
			*pichwMinDiff = ichwRunMin2;
			break;
		}
		CheckHr(pts1->Fetch(ichwRunMin1, ichwRunLim1, rgchw1));
		CheckHr(pts2->Fetch(ichwRunMin2, ichwRunLim2, rgchw2));

		int cchwRun1 = ichwRunLim1 - ichwRunMin1;
		int cchwRun2 = ichwRunLim2 - ichwRunMin2;

		// Search through the run text to compare the characters one-by-one.
		int ichw;
		for (ichw = 0; ichw < min(cchwRun1, cchwRun2); ichw++)

		{
			if (rgchw1[ichw] != rgchw2[ichw])
			{
				// Found a difference in the run text. The difference is at our current ich.
				*pichwMinDiff = ichwRunMin2 + ichw;
				break;
			}
		}
		if (*pichwMinDiff != -1)
			break; // We found a difference

		if (cchwRun1 != cchwRun2)
		{
			// The runs don't contain the same number of characters, but the
			// characters are the same, so the difference is at the end of the
			// shorter text.
			*pichwMinDiff = ichwRunMin2 + ichw;
			break;
		}

		ichwRunMin1 = ichwRunLim1;
		ichwRunMin2 = ichwRunLim2;
	}

	if (*pichwMinDiff == -1)
	{
		// Didn't find any differences in the text or properties that we searched.
		// This will mose likely happen if the length of one of the strings is zero.
		delete[] rgchw1;
		delete[] rgchw2;
		if (cchwStrLim1 < ichwLim2)
		{
			// More characters in second text source string
			*pichwMinDiff = cchwStrLim1;
			*pichwLimDiff = ichwLim2;
		}
		else if (cchwStrLim1 > ichwLim2)
		{
			// More characters in the first text source string
			*pichwMinDiff = ichwLim2;
			*pichwLimDiff = ichwLim2;
		}
		else if (cchwStrLim1 == 0 && ichwLim2 == 0)
		{
			// No differences...unless both strings empty with different props
			irun1 = irun2 = 0;
			pts1->GetCharPropInfo(ichwRunMin1, &ichwRunMin1, &ichwRunLim1, &isbt1, &irun1, &qttp1, &qzvps1);
			pts2->GetCharPropInfo(ichwRunMin2, &ichwRunMin2, &ichwRunLim2, &isbt2, &irun2, &qttp2, &qzvps2);
			if (qttp1.Ptr() != qttp2.Ptr())
			{
				// Properties were different for the empty strings
				*pichwMinDiff = 0;
				*pichwLimDiff = 0;
			}
		}
		// else no diffs at all...leave both values -1.
		return;
	}

	if (*pichwMinDiff >= cchwStrLim1 && cchwStrLim1 <= ichwLim2)
	{
		// The text of the second string was longer, but when comparing the runs, the last
		// run of the second string was longer as well. This means that there is no more text
		// to compare so we can safely assume that the difference spans the rest of the text.
		*pichwLimDiff = ichwLim2;
		delete[] rgchw1;
		delete[] rgchw2;
		return;
	}

	// We know we have a difference now. Search backwards looking for the end of the difference
	ichwRunMin1 = ichwLim1 - 1;
	ichwRunMin2 = ichwLim2 - 1;
	while (ichwRunMin1 >= ichwRunMin1Orig || ichwRunMin2 >= 0)
	{
		if (ichwRunMin2 >= 0)
		{
			irun2 = 0;
			pchrp2 = pts2->GetCharPropInfo(ichwRunMin2, &ichwRunMin2, &ichwRunLim2, &isbt2, &irun2, &qttp2, &qzvps2);
		}
		else
		{
			// The second string contained the end of the the first string, but was shorter
			// than the first.
			*pichwLimDiff = 0;
			break;
		}
		if (ichwRunMin1 >= ichwRunMin1Orig)
		{
			irun1 = 0;
			pchrp1 = pts1->GetCharPropInfo(ichwRunMin1, &ichwRunMin1, &ichwRunLim1, &isbt1, &irun1, &qttp1, &qzvps1);
		}
		else
		{
			// The first string contained the end of the second string, but was shorter
			// than the second.
			*pichwLimDiff = ichwRunLim2;
			break;
		}

		ichwRunMin1 = max(ichwRunMin1, ichwRunMin1Orig);

		int cchwRun1 = ichwRunLim1 - ichwRunMin1;
		int cchwRun2 = ichwRunLim2 - ichwRunMin2;

		if (pchrp1 != pchrp2)
		{
			// Found a difference in run properties, so the end of the run for the
			// text source is where the difference ends.
			*pichwLimDiff = ichwRunLim2;
			break;
		}

		CheckHr(pts1->Fetch(ichwRunMin1, ichwRunLim1, rgchw1));
		CheckHr(pts2->Fetch(ichwRunMin2, ichwRunLim2, rgchw2));
		int ichw1, ichw2;
		for (ichw1 = cchwRun1, ichw2 = cchwRun2; ichw1 > 0 && ichw2 > 0; )
		{
			ichw1--;
			ichw2--;
			if (rgchw1[ichw1] != rgchw2[ichw2])
			{
				// Found a difference in the run text. The difference is at the ich of the
				// second string.
				*pichwLimDiff = ichwRunMin2 + ichw2 + 1;
				break;
			}
		}
		if (*pichwLimDiff != -1)
			break; // We found the end of the difference

		if (cchwRun1 != cchwRun2)
		{
			// The runs don't contain the same number of characters, but the
			// characters are the same, so the difference is at the beginning of the
			// shorter text.
			*pichwLimDiff = ichwRunMin2 + ichw2;
			break;
		}

		ichwRunMin1 = ichwRunMin1 - 1;
		ichwRunMin2 = ichwRunMin2 - 1;
	}

	Assert(*pichwLimDiff != -1);

	if (cchwStrLim1 < ichwLim2 && *pichwLimDiff - *pichwMinDiff < ichwLim2 - cchwStrLim1)
	{
		//	The length of stuff changed must be at least as big as the difference in size.
		*pichwLimDiff = *pichwMinDiff + (ichwLim2 - cchwStrLim1);
	}

	delete[] rgchw1;
	delete[] rgchw2;
}

/*----------------------------------------------------------------------------------------------
	Computes (by being called for each box on a line in turn) the boundary of the current
	line (the starting point for laying out the following line) as well as the offset of this
	line from the previous one. These values are used to initialize partial paragraph
	layout. In a normal paragraph, they are basically the bottom and descent of the boxes
	on the current line. In an inverted paragraph, they are the top and minus the ascent.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::AdjustLineBoundary(bool fExactLineHeight, int & dyBoundaryCurr,
	int & dyBaselineOffsetCurr, VwBox * pboxTmp, int dypLineHeight, int dypInch)
{
	if (fExactLineHeight)
	{
		int dyExactAscent = MulDiv(m_dympExactAscent, dypInch, kdzmpInch);
		dyBoundaryCurr = pboxTmp->Baseline() + (dypLineHeight - dyExactAscent);
		dyBaselineOffsetCurr = dypLineHeight - dyExactAscent;
	}
	else
	{
		// This logic for 'aloneOnLine' is only valid for the first box,
		// but the drop caps box which cares about it always is.
		VwBox * pboxNext = pboxTmp->NextOrLazy();
		int dypDescent = pboxTmp->LineSepDescent(pboxNext
			&& pboxNext->Baseline() != pboxTmp->Baseline());
		dyBoundaryCurr = max(dyBoundaryCurr, pboxTmp->Baseline() + dypDescent);
		dyBaselineOffsetCurr = max(dyBaselineOffsetCurr, dypDescent);
	}
}

/*----------------------------------------------------------------------------------------------
	Return a pointer to the first box on the line where the given character (from the
	text source) is displayed. While we're at it, also return the first box on the line
	before that, because we don't know for sure whether we might have to back up one line.

	Also return the number of lines before the one containing the character,
	and the tops of the lines containing the first boxes. "Top" in this case signifies the
	bottom of the previous line (its baseline plus the largest descender) plus the height
	of any overlay tags that follow the line...the value with which to initialize the
	paragraph builder for laying out a replacement for the line containing *ppboxStartLine.
	(In an inverted paragraph, it is actually the bottom of the line.)
----------------------------------------------------------------------------------------------*/
int VwParagraphBox::FindFirstBoxOnLine(IVwGraphics * pvg, int ichw,
	VwBox ** ppboxStartLine, int * pdyTop, int * pdyPrevDescent,
	VwBox ** ppboxPrevLine, int * pdyPrevTop, int * pdyPrevPrevDescent)
{
	int cLines = -1;
	int cLinesRet = 0;
	// Zero initializes for max function in normal version of AdjustLineBoundary;
	// Height() initializes for min() in inverted version.
	int dyBottomCurr = InitialBoundary();
	int dyBottomPrev = 0;
	int dyDescentCurr = 0;
	int dyDescentPrev = 0;
	int dyDescentPrevPrev = 0;
	*pdyTop = 0;
	*pdyPrevTop = 0;
	*pdyPrevDescent = 0;
	*pdyPrevPrevDescent = 0;
	VwBox * pboxCurrFirst = m_pboxFirst;
	VwBox * pboxNextFirst;
	*ppboxStartLine = pboxCurrFirst;
	*ppboxPrevLine = NULL;
	Vector<VwBox *> vpboxOneLine;

	int dysTagAbove, dysTagBelow;
	ComputeTagHeights(pvg, Source()->Overlay(), Root()->DpiSrc().y, dysTagAbove,
		dysTagBelow);

	// For exact line spacing:
	int dypInch;
	CheckHr(pvg->get_YUnitsPerInch(&dypInch));
	int dypLineHeight = MulDiv(m_qzvps->LineHeight(), dypInch, kdzmpInch);
	bool fExactLineHeight = m_qzvps->ExactLineHeight();

	while (pboxCurrFirst)
	{
		pboxNextFirst = GetALine(pboxCurrFirst, vpboxOneLine);
		cLines++;
		dyBottomPrev = dyBottomCurr;
		dyBottomCurr = InitialBoundary();
		dyDescentPrevPrev = dyDescentPrev;
		dyDescentPrev = dyDescentCurr;
		dyDescentCurr = 0;
		bool fAtStartOfLine = true;
		for (int ibox = 0; ibox < vpboxOneLine.Size(); ibox++)
		{
			VwBox * pboxTmp = vpboxOneLine[ibox];
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(pboxTmp);
			if (psbox)
			{
				if (psbox->IchMin() >= ichw)
					return cLinesRet;

				if (fAtStartOfLine)
				{
					// Remember stuff to return.

					// Save the start of this line, which is the bottom of the previous line.
					*ppboxPrevLine = (*ppboxStartLine == pboxCurrFirst) ? NULL : *ppboxStartLine;
					*ppboxStartLine = pboxCurrFirst;
					cLinesRet = cLines;
					*pdyPrevTop = *pdyTop;
					// Need to allow room for tagging, but only if there actually is a previous line.
					// Enhnace JohnT (Inverted): need some fiddling here if we ever do inverted views
					// with tagging. Roughly we need to subtract dysTagAbove instead of adding
					// dysTagBelow, but it may also affect the descent/ascent value.
					*pdyTop = dyBottomPrev + (cLines ? dysTagBelow : 0);
					*pdyPrevDescent = dyDescentPrev;
					*pdyPrevPrevDescent = dyDescentPrevPrev;
					fAtStartOfLine = false;
				}
			}

			// Adjust the variables that keep track of the bottom (or top) of the current line and its maximum descent
			// (or ascent).
			AdjustLineBoundary(fExactLineHeight, dyBottomCurr, dyDescentCurr, pboxTmp,
				dypLineHeight, dypInch);
		}
		pboxCurrFirst = pboxNextFirst;
	}
	return cLinesRet;
}

int ComputeIchStart(VwBox * pboxStart)
{
	VwStringBox * psbox = dynamic_cast<VwStringBox *>(pboxStart);
	if (psbox)
		return psbox->IchMin();
	else
		return -1;
}

	// ParaBuilder::Initialize expects the outer width. This fixes TE-2787.
int VwParagraphBox::ComputeOuterWidth()
{
	return Container()->AvailWidthForChild(Root()->DpiSrc().x, this);
}

/*----------------------------------------------------------------------------------------------
	Lay out part of the paragraph. Changes here should typically be reflected in
	VwInvertedParaBox::DoPartialLayout.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::DoPartialLayout(IVwGraphics * pvg, VwBox * pboxStart, int cLinesToSave,
	int dyStart, int dyPrevDescent,
	int ichMinDiff, int ichLimDiff, int cchLenDiff)
{
	ParaBuilder zpb;
	zpb.Initialize(pvg, m_qzvps, ComputeOuterWidth(), this, m_qts,
		false, pboxStart, cLinesToSave, dyStart, dyPrevDescent,
		ichMinDiff, ichLimDiff, cchLenDiff, ComputeIchStart(pboxStart));

	DoLayoutAux(pvg, (void *)&zpb);
	AssertValid();
}

/*----------------------------------------------------------------------------------------------
	Lay out part of the paragraph (using an InvertedParaBuilder).
----------------------------------------------------------------------------------------------*/
void VwInvertedParaBox::DoPartialLayout(IVwGraphics * pvg, VwBox * pboxStart, int cLinesToSave,
	int dyStart, int dyPrevDescent,
	int ichMinDiff, int ichLimDiff, int cchLenDiff)
{
	InvertedParaBuilder zpb;
	// ParaBuilder::Initialize expects the outer width. This fixes TE-2787.
	zpb.Initialize(pvg, m_qzvps, ComputeOuterWidth(), this, m_qts,
		false, pboxStart, cLinesToSave, dyStart, dyPrevDescent,
		ichMinDiff, ichLimDiff, cchLenDiff, ComputeIchStart(pboxStart));

	DoLayoutAux(pvg, (void *)&zpb);
	AssertValid();
}

bool VwParagraphBox::AssertValid(void)
{
	bool fTruncated = false;
	for (VwBox * pbox = m_pboxFirst; pbox; pbox = pbox->NextOrLazy())
	{
		// Check basic linkage of boxes.
		Assert(pbox->Container() == this);
		if (!pbox->NextOrLazy())
		{
			bool f;
			f = (pbox == m_pboxLast);
			Assert(f);
		}
		if (pbox->Top() == knTruncated)
			fTruncated = true; // pretend no more boxes
		if (fTruncated)
			continue;
		// Check all string boxes have segments
		VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
		if (psbox)
		{
			bool f;
			f = (psbox->Segment());
			// Because of the complexities of painting windows, etc, just bringing up
			// the dialog informing us about the assertion below gets things in a bad
			// state. (This happens when there has been an error in Graphite--but it
			// will try to recover.) So instead, just output a warning.
			//Assert(f);
#if 0
			// There are thousands of these warnings in debug and is v e r y s l o w. .
			Warn("No segment in the paragraph");
#endif
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	The minimum chunk of code we can override to get an InvertedParaBuilder when we want one.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::RunParaBuilder(IVwGraphics * pvg, int dxAvailWidth, int cch)
{
	ParaBuilder zpb;
	zpb.Initialize(pvg, m_qzvps, dxAvailWidth, this, m_qts, true, NULL, 0, 0, 0,
		0, cch, cch, -1);
	DoLayoutAux(pvg, (void *)&zpb);
}

void VwInvertedParaBox::RunParaBuilder(IVwGraphics * pvg, int dxAvailWidth, int cch)
{
	InvertedParaBuilder zpb;
	zpb.Initialize(pvg, m_qzvps, dxAvailWidth, this, m_qts, true, NULL, 0, InitialBoundary(), 0,
		0, cch, cch, -1);
	DoLayoutAux(pvg, (void *)&zpb);
}

/*----------------------------------------------------------------------------------------------
	The value that the 'boundary of previous space' is set to (before calling
	AdjustEdgeOfSpaceForInitialGap) for a complete layout.
----------------------------------------------------------------------------------------------*/
int VwParagraphBox::InitialBoundary()
{
	return 0;
}

int VwInvertedParaBox::InitialBoundary()
{
	return Height();
}
/*----------------------------------------------------------------------------------------------
	Lay out the paragraph.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::DoLayout(IVwGraphics * pvg, int dxAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
	Assert(Source()->CStrings() > 0);

	// better recalculate the width of our boundary mark - things like font size might have
	// changed.
	m_dxdBoundaryMark = 0;
	int cch;
	CheckHr(m_qts->get_Length(&cch));
	RunParaBuilder(pvg, dxAvailWidth, cch);
}

/*----------------------------------------------------------------------------------------------
	This method gives a box an opportunity to stretch its own width to match the final
	determined width of a containing box. This is initiated by inner piles to allow
	contained items that have borders or background colors to extend the full width of the
	pile. (Normally, when not using inner piles, such paragraphs occupy the full available
	width. But this would cause every inner pile to occupy a full paragraph line. So inside
	an inner pile, paragraphs take on their minimum width even if they have a border or
	background. This adjustment allows them to take the full width of the outermost relevant
	inner pile.)
	The idea is that dxpTargetWidth is the width that the receiving box should have
	(measuring to the outside of its own margins) in order to occupy the full available
	width inside the container.
----------------------------------------------------------------------------------------------*/
bool VwParagraphBox::StretchToPileWidth(int dxpInch, int dxpTargetWidth, int tal)
{
	int delta = dxpTargetWidth - Width();
	if (delta <= 0)
		return false; // don't ever shrink, and let client know if no change needed.
	// If the paragraph is centered, justified, or aligned differently from
	// the pile, we won't try this trick. (Maybe one day...)
	VwPropertyStore * pvps = Style();
	if (pvps->ParaAlign() != tal)
		return false;

	bool fInnerStretch = false;
	if (LastBox())
	{
		VwInnerPileBox * pipbox = dynamic_cast<VwInnerPileBox *>(LastBox());
		if (pipbox)
		{
			// See whether IT wants to stretch! Its new possible width
			// is most easily computed as its current width plus the
			// available growth space.
			int dxpInnerTargetWidth = pipbox->Width() + delta;
			fInnerStretch = pipbox->StretchToPileWidth(dxpInch, dxpInnerTargetWidth, tal);
		}
	}

	// If we don't have a border or background color, and nothing inside wants to stretch,
	// no reason to stretch at all.
	if ((!fInnerStretch) && pvps->BorderTop() == 0 &&  pvps->BorderBottom() == 0
		&& pvps->BorderLeading() == 0 && pvps->BorderTrailing() == 0
		&& pvps->BackColor() == kclrTransparent)
	{
		return false;
	}

	_Width(dxpTargetWidth);
	// If the pile is aligned right--same as leading for piles--we need to move the box over
	// as well as adjusting its width.
	if (tal == ktalRight || tal == ktalTrailing)
	{
		// Also, any preceding boxes need to move over by delta, to keep them
		// right of the expanded inner pile.
		for (VwBox * pbox = FirstBox(); pbox != LastBox(); pbox = pbox->NextOrLazy())
		{
			pbox->Left(pbox->Left() + delta);
		}
		// If we didn't stretch the last box, it too should be adjusted.
		if (!fInnerStretch && LastBox() != NULL)
			LastBox()->Left(LastBox()->Left() + delta);
	}
	return true;
}
// Subclass used by Relayout
class ParaReBuilder : public ParaBuilder
{
public:
	VwRootBox * m_prootb;
	FixupMap * m_pfixmap;

	virtual void LayoutNonString()
	{
		m_pboxNextNonStr->Relayout(m_pvg, m_dxInnerAvailWidth, m_prootb, m_pfixmap,
			m_dxWidthRemaining);
	}
};

// Subclass used by Relayout for inverted paragraphs
class InvertedParaReBuilder : public InvertedParaBuilder
{
public:
	VwRootBox * m_prootb;
	FixupMap * m_pfixmap;

	virtual void LayoutNonString()
	{
		m_pboxNextNonStr->Relayout(m_pvg, m_dxInnerAvailWidth, m_prootb, m_pfixmap,
			m_dxWidthRemaining);
	}
};

// Much like DoLayout, but we don't necessarily need to lay out non-string child boxes.
// So we use a subclass of ParaBuilder which does that one thing differently.
bool VwParagraphBox::Relayout(IVwGraphics * pvg, int dxAvailWidth, VwRootBox * prootb,
	FixupMap * pfixmap, int dxpAvailOnLine, BoxIntMultiMap * pmmbi)
{
	// better recalculate the width of our boundary mark - things like font size might have
	// changed.
	m_dxdBoundaryMark = 0;

	if (m_dysHeight == 0)
	{ // typically new box, needs complete layout.
		this->DoLayout(pvg, dxAvailWidth, dxpAvailOnLine);
		// But, may not really be new. If in the fixmap, the contract calls for us to
		// invalidate the old rectangle. (Not doing this causes the following problem:
		// in WorldPad, make a document consisting of a single paragraph, make
		// it all very large, make it small again: not all the large text is erased.)
		Rect vrect;
		VwBox * pboxThis = this; // seems to be needed for calling Retrieve...
		if (pfixmap->Retrieve(pboxThis, &vrect))
		{
			Root()->InvalidateRect(&vrect);
		}
		this->CheckBoxMap(pmmbi, prootb);
		return true;
	}
	Rect vrect;
	VwBox * pbox = this; // Retrieve requires non-const VwBox argument
	if (!pfixmap->Retrieve(pbox, &vrect))
		return false; //unmodified, needs nothing.

	this->CheckBoxMap(pmmbi, prootb);
	// We put an all-zeros rectangle in the fixmap when we need to CheckBoxMap but
	// do NOT want to relayout.
	if (vrect.left == vrect.right && vrect.top == vrect.bottom && vrect.top == 0)
		return false;
	int cch;
	CheckHr(m_qts->get_Length(&cch));
	RunParaReBuilder(pvg, dxAvailWidth, prootb, pfixmap, cch);
	return true; // Invalidate the whole paragraph.
}

// The smallest chunk of code we can conveniently wrap around the appropriate class of ParaReBuilder,
// so this can be overridden for VwInvertedParaBox.
void VwParagraphBox::RunParaReBuilder(IVwGraphics * pvg, int dxAvailWidth, VwRootBox * prootb,
	FixupMap * pfixmap, int cch)
{
	ParaReBuilder zpb;
	zpb.Initialize(pvg, m_qzvps, dxAvailWidth, this, m_qts, true, NULL, 0, 0, 0,
		0, cch, cch, -1);
	zpb.m_prootb = prootb;
	zpb.m_pfixmap = pfixmap;

	DoLayoutAux(pvg, (void *)&zpb);
}

void VwInvertedParaBox::RunParaReBuilder(IVwGraphics * pvg, int dxAvailWidth, VwRootBox * prootb,
	FixupMap * pfixmap, int cch)
{
	InvertedParaReBuilder zpb;
	zpb.Initialize(pvg, m_qzvps, dxAvailWidth, this, m_qts, true, NULL, 0, InitialBoundary(), 0,
		0, cch, cch, -1);
	zpb.m_prootb = prootb;
	zpb.m_pfixmap = pfixmap;

	DoLayoutAux(pvg, (void *)&zpb);
}

/*--------------------------------------------------------------------------------------------*/
void VwParagraphBox::DoLayoutAux(IVwGraphics * pvg, void * pv)
{
	if (Source()->CStrings() == 0)
		ThrowInternalError(E_UNEXPECTED, L"Laying out a paragraph with no content - connect report to LT-9233");
	ParaBuilder * pzpb = reinterpret_cast<ParaBuilder *>(pv);

	// run the main loop assigning boxes to lines and breaking strings.
	pzpb->MainLoop();

	m_pboxFirst = pzpb->m_pboxFirst;
	if (m_pboxFirst) // May be empty (e.g., because 0 lines allowed)
		m_pboxLast = m_pboxFirst->EndOfChain();
	else
		m_pboxLast = NULL; // Make sure it is set.
	SetHeightAndAdjustChildren(pvg, pzpb);

	m_dxsWidth = pzpb->m_dxInnerWidth + pzpb->m_dxTrail;
	// To make borders and background color cover the full width of the page as expected,
	// we generally make all paragraphs occupy all the available space.
	// If it has more than one line it is definitely going to use all the space.
	// If it has only one line, there are a couple of important exceptions:
	// 1. A paragraph that is nested somewhere inside another paragraph (as
	// in an interlinear bundle) must not use all available width or there will
	// only be one bundle per line.
	// 2. If we are wanting to find out how wide something is (by giving an
	// unlimited available width), this purpose is defeated if the paragraph
	// just automatically takes on the available width.
	// Case 1 is handled by not increasing the width if any container is a
	// paragraph.
	// Case 2 is handled by only increasing the width if we have a non-transparent
	// background color or some border: in other words, don't do it unless really
	// necessary.
	if (pzpb->m_clines > 1)
		m_dxsWidth = pzpb->m_dxAvailWidth;
	else if (m_qzvps->BackColor() != kclrTransparent || m_qzvps->HasAnyBorder())
	{
		// See if any container is a paragraph; if not, use all available width.
		if (!pzpb->IsNested())
			m_dxsWidth = pzpb->m_dxAvailWidth;
	}
#if 0 // ENHANCE: reinstate and update when we do window box
	VwWindowBox* lastWindBox = dynamic_cast<VwWindowBox*>(m_lastBox);
	if (lastWindBox && lastWindBox->justifyRight())
	{
		m_width = availWidth; //right justifying, so width must be maximal
		int delta = availWidth - m_lastBox->right();
		m_lastBox->left(m_lastBox->left()+delta);
	}
#endif
#ifdef DEBUG
	// We could just do AssertObj(this), but I want to be able to break on the Assert
	// and try the layout again.
	if (!IsValid())
		Assert(false);
#endif
#ifdef ENABLE_TSF
	if (Root()->InputManager())
		CheckHr(Root()->InputManager()->OnLayoutChange());
#endif /*ENABLE_TSF*/
}

// This is called after running the parabuilder algorithm. It determines the height of the overall box.
// In the inverted case, if the height changes, child positions need to be adjusted.
void VwParagraphBox::SetHeightAndAdjustChildren(IVwGraphics * pvg, ParaBuilder * pzpb)
{
	// At this point, pzpb->m_ysEdgeOfSpaceForThisLine is far enough below (or above) the previous line to allow for its
	// descenders (or ascenders), possibly a double underline, and any additional tag below (or above) that line.
	// To produce a proper space between paragraphs when extra inter-line spacing is used,
	// we need to produce an extra gap that is equivalent to the extra space typically
	// added between lines of the same paragraph. Also of course add the bottom gap for
	// our own pad, margin, etc.

	// Unfortunately, the ideal amount of space to add depends on the ascent of the next
	// line, which we don't know. What's more, if we tried to take that into account
	// somehow, we would have to deal with the possibility that the two paragraphs have
	// different line spacing. Instead, we just assume the ascent of the next line
	// is the ascent appropriate for the last character of this paragraph.
	// Next baseline, not allowing for tagging or font height.
	// Very similar to the algorithm in Baseline().
	// I think we could do this in less steps, but this is clearer.
	int dySpaceExtra;
	if (pzpb->m_fExactLineHeight)
	{
		dySpaceExtra = 0;
	}
	else
	{
		int dyAscent;
		int ich = max(Source()->CchRen() - 1, 0);
		LgCharRenderProps chrp;
		int ichMinRun, ichLimRun; // dummies
		CheckHr(Source()->GetCharProps(ich, &chrp, &ichMinRun, &ichLimRun));
		CheckHr(pvg->SetupGraphics(&chrp));
		CheckHr(pvg->get_FontAscent(&dyAscent));
		//CheckHr(pvg->get_FontDescent(&dyDescent)); not needed for BaseLine algorithm in normal case when not exact line height.
		// This would be the baseline of the next line if there were one.
		// Enhance JohnT: the descent is not actually needed in the normal case. In the inverted case, where
		// it is, should we include the minimum space for double-underline?
		int yNextBaseLine = pzpb->BaseLine(0, dyAscent);
		// This is the space we would have added in order to achieve minimum line spacing
		// (if there were another line):
		dySpaceExtra = max(yNextBaseLine - pzpb->m_ysEdgeOfSpaceForThisLine - dyAscent, 0);
	}

	int dypInch;
	CheckHr(pvg->get_YUnitsPerInch(&dypInch));
	m_dysHeight = pzpb->m_ysEdgeOfSpaceForThisLine + GapBottom(dypInch) + dySpaceExtra;
}

void VwInvertedParaBox::SetHeightAndAdjustChildren(IVwGraphics * pvg, ParaBuilder * pzpb)
{
	// At this point, pzpb->m_ysEdgeOfSpaceForThisLine is far enough below (or above) the previous line to allow for its
	// descenders (or ascenders), possibly a double underline, and any additional tag below (or above) that line.
	// To produce a proper space between paragraphs when extra inter-line spacing is used,
	// we need to produce an extra gap that is equivalent to the extra space typically
	// added between lines of the same paragraph. Also of course add the bottom gap for
	// our own pad, margin, etc.

	// Unfortunately, the ideal amount of space to add depends on the ascent of the next
	// line, which we don't know. What's more, if we tried to take that into account
	// somehow, we would have to deal with the possibility that the two paragraphs have
	// different line spacing. Instead, we just assume the ascent of the next line
	// is the ascent appropriate for the last character of this paragraph.
	// Next baseline, not allowing for tagging or font height.
	// Very similar to the algorithm in Baseline().
	// I think we could do this in less steps, but this is clearer.
	if (!pzpb->m_fExactLineHeight)
	{
		int dyDescent;
		int ich = max(Source()->CchRen() - 1, 0);
		LgCharRenderProps chrp;
		int ichMinRun, ichLimRun; // dummies
		CheckHr(Source()->GetCharProps(ich, &chrp, &ichMinRun, &ichLimRun));
		CheckHr(pvg->SetupGraphics(&chrp));
		//CheckHr(pvg->get_FontAscent(&dyAscent)); not needed for baseline in inverted non-exact case.
		CheckHr(pvg->get_FontDescent(&dyDescent));
		// This would be the baseline of the next line if there were one.
		// Enhance JohnT: the descent is not actually needed in the normal case. In the inverted case, where
		// it is, should we include the minimum space for double-underline?
		int yNextBaseLine = pzpb->BaseLine(dyDescent, 0);
		// Pretend the last line ended where it did, or enough higher to put the next para where it should be for
		// the line spaccing.
		pzpb->m_ysEdgeOfSpaceForThisLine = min(pzpb->m_ysEdgeOfSpaceForThisLine, yNextBaseLine - dyDescent);
	}

	// At this point, m_ysEdgeOfSpaceForThisLine is above the current bottom of the paragraph by enough distance
	// to hold all its lines and the bottom gap.

	int dypInch;
	CheckHr(pvg->get_YUnitsPerInch(&dypInch));
	int oldHeight = m_dysHeight;
	m_dysHeight = Height() - pzpb->m_ysEdgeOfSpaceForThisLine + GapTop(dypInch);
	if (m_dysHeight != oldHeight)
	{
		// adjust all the (non-truncated) boxes.
		int delta = m_dysHeight - oldHeight; // all need to move down this much
		for (VwBox * pbox = FirstBox(); pbox && pbox->Top() != knTruncated; pbox = pbox->NextOrLazy())
			pbox->Top(pbox->Top() + delta);
		Invalidate(); // evverything moved.
	}
}

/*----------------------------------------------------------------------------------------------
	Discard your layout boxes, which will be regenerated by the string in the text source.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::DiscardLayoutBoxes()
{
	VwBox * pboxPrev = NULL;
	VwBox * pboxNext = NULL;
	VwBox * pbox = FirstBox();
	m_pboxFirst = NULL; // no boxes at all unless we find one to keep.
	for (; pbox; pbox = pboxNext)
	{
		pboxNext = pbox->Next(); // in case pbox gets reset or deleted
		if (pbox->IsBoxFromTsString())
		{
			// discard it; as a layout extension nothing should be pointing to it
			pbox->DeleteAndCleanupAndDeleteNotifiers();
		}
		else
		{
			// Box containing embedded stuff, not generated from the strings in the text
			// source: keep it and use it when we hit the place
			// where we need it; see AddNonStringBox.
			if (pboxPrev)
				pboxPrev->SetNext(pbox);
			else
				// Our first non-extension box
				m_pboxFirst = pbox;
			pboxPrev = pbox; // link future ones after this
		}
	}
	m_pboxLast = pboxPrev;
	if (m_pboxLast)
		m_pboxLast->SetNext(NULL);
}

/*----------------------------------------------------------------------------------------------
	Obtain one line of boxes, starting with pboxFirst, in the vector vbox, ordered by x coord
	of top left. Also answer the first box of the subsequent line, if any. Boxes on a line are
	identified by having the same baseline.
	NOTE: GetALine can be called (from ParaBuilder) when *this is not guaranteed to be properly
	liked with *pboxFirst
----------------------------------------------------------------------------------------------*/
VwBox * VwParagraphBox::GetALine(VwBox * pboxFirst, BoxVec & vbox)
{
	vbox.Clear();
	int ysBaseline = pboxFirst->Top() + pboxFirst->Ascent();
	VwBox * pbox;
	for (pbox = pboxFirst; pbox; pbox = pbox->Next())
	{
		if (pbox->Top() == knTruncated)
			return NULL; // pretend no more boxes
		int ysBaselineThis = pbox->Top() + pbox->Ascent();
		if (ysBaselineThis != ysBaseline)
			break;
		// OPTIMIZE JohnT: is it worth coding a binary search for the place to insert?
		int xsLeft = pbox->Left();
		int ibox;
		for (ibox = 0; ibox < vbox.Size(); ++ibox)
		{
			if (m_fParaRtl)
			{
				// Boxes with equal Left(), logical first comes last (rightmost among equals).
				if (vbox[ibox]->Left() >= xsLeft)
					break;
			}
			else
			{
				// Boxes with equal Left(), logical first comes first (leftmost among equals).
				if (vbox[ibox]->Left() > xsLeft)
					break;
			}
		}
		vbox.Insert(ibox, pbox);
	}
	// Exit when we find a different baseline or run out of boxes.
	// In either case, pbox is the value to return.
	return pbox;
}

/*----------------------------------------------------------------------------------------------
	Obtain one line of boxes, starting with pboxFirst, in the vector vbox, ordered by x coord
	of top left. Also answer the first box of the subsequent line, if any. Boxes on a line are
	identified by having the same baseline. Also compute the top and bottom of the line
	in source coords (relative to top left of paragraph).
	Note: the bottom is the layout bottom (the top of the next line); it is possible that
	a drop cap on the current line extends below it.
----------------------------------------------------------------------------------------------*/
VwBox * VwParagraphBox::GetALineTB(VwBox * pboxFirst, BoxVec & vbox, int dpiY,
	int * pysTop, int * pysBottom)
{
	vbox.Clear();
	int ysTop = pboxFirst->Top();
	int ysBottom = ysTop;
	int ysBaseline = ysTop + pboxFirst->Ascent();
	VwBox * pbox;
	for (pbox = pboxFirst; pbox; pbox = pbox->Next())
	{
		if (pbox->Top() == knTruncated)
		{
			pbox = NULL;
			break;
		}
		int ysTopThis = pbox->Top();
		int ysBaselineThis = ysTopThis + pbox->Ascent();
		if (ysBaselineThis != ysBaseline)
			break;
		// OPTIMIZE JohnT: is it worth coding a binary search for the place to insert?
		int xsLeft = pbox->Left();
		// Review JohnT: is <= correct here? It amounts to saying that two boxes with
		// the same X coord will be inserted in logical order from left to right.
		// Maybe we should use < in a right-to-left para?
		int ibox;
		for (ibox = 0; ibox < vbox.Size() && vbox[ibox]->Left() <= xsLeft; ibox++)
			;
		vbox.Insert(ibox, pbox);
		if (ysTopThis < ysTop)
			ysTop = ysTopThis;
		// pbox == m_pvpboxLast is not a very robust way to determine whether the box
		// is 'alone on a line' but it only matters for a drop caps box which is always
		// the first box in the paragraph
		VwBox * pboxNext = pbox->NextOrLazy();
		int ysBottomThis = pbox->Top() + pbox->Ascent() +
			pbox->LineSepDescent(pboxNext && pboxNext->Baseline() != pbox->Baseline());
		if (ysBottomThis > ysBottom)
			ysBottom = ysBottomThis;
	}
	if (Source()->Overlay())
	{
		// Force descent to leave room for double underline.
		int dysMinDescent = 4 * dpiY / 96;
		ysBottom = std::max(ysBottom, dysMinDescent + ysBaseline);
	}
	// Exit when we find a different baseline or run out of boxes.
	// In either case, pbox is the value to return.
	*pysTop = ysTop;
	*pysBottom = ysBottom;
	return pbox;
}

/*----------------------------------------------------------------------------------------------
	Return a vector of boxes in physical order.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::GetBoxesInPhysicalOrder(BoxVec & vbox)
{
	VwBox * pbox = m_pboxFirst;
	while (pbox)
	{
		BoxVec vboxOneLine;
		pbox = GetALine(pbox, vboxOneLine);

		// Sort the boxes based on their physical layout and the paragraph direction.
		int ivMin = vbox.Size();
		for (int ibox = 0; ibox < vboxOneLine.Size(); ++ibox)
		{
			// Get the position of this box in the sorted list (each line is sorted separately).
			int iv;
			int ivLim;
			VwBox * pboxNew = vboxOneLine[ibox];
			for (iv = ivMin, ivLim = vbox.Size(); iv < ivLim; )
			{
				int ivMid = (iv + ivLim) / 2;
				VwBox * pboxTmp = vbox[ivMid];
				if ((m_fParaRtl && pboxNew->Left() < pboxTmp->Left()) ||
					(!m_fParaRtl && pboxNew->Left() > pboxTmp->Left()))
				{
					iv = ivMid + 1;
				}
				else
				{
					ivLim = ivMid;
				}
			}
			vbox.Insert(iv, vboxOneLine[ibox]);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Return the box that follows the given box in physical order (based on the paragraph
	direction).
----------------------------------------------------------------------------------------------*/
VwBox * VwParagraphBox::GetNextPhysicalBox(VwBox * pbox)
{
	BoxVec vbox;
	GetBoxesInPhysicalOrder(vbox);
	return GetAdjPhysicalBox(pbox, vbox, 1);
}

/*----------------------------------------------------------------------------------------------
	Return the box that preceeds the given box in physical order (based on the paragraph
	direction).
----------------------------------------------------------------------------------------------*/
VwBox * VwParagraphBox::GetPrevPhysicalBox(VwBox * pbox)
{
	BoxVec vbox;
	GetBoxesInPhysicalOrder(vbox);
	return GetAdjPhysicalBox(pbox, vbox, -1);
}

/*----------------------------------------------------------------------------------------------
	Return the extra height that the box should have if it is the last thing in its pile or if
	the next box is not a paragraph box.
	Currently this is non-zero only for one-line drop-cap paragraphs.
	If fIgnoreNextBox is false (the default), it returns non-zero only if it is indeed not
	followed by a paragraph. If it is true, it will not make that test.
----------------------------------------------------------------------------------------------*/
int VwParagraphBox::ExtraHeightIfNotFollowedByPara(bool fIgnoreNextBox)
{
	VwDropCapStringBox * pdcsbFirst = dynamic_cast<VwDropCapStringBox *>(this->FirstBox());
	if (!pdcsbFirst)
		return 0; // not a DC para.
	// Do we have at least as many lines as what the height of the drop cap covers?
	int yFirstBaseline = pdcsbFirst->Ascent() + pdcsbFirst->Top();
	for (VwBox * pbox = pdcsbFirst->NextOrLazy(); pbox; pbox = pbox->NextOrLazy())
	{
		// REVIEW (EberhardB): I think this doesn't work if the drop cap is more than 2 lines high
		if (pbox->Ascent() + pbox->Top() != yFirstBaseline)
			return 0; // more than one line, height covers DC.
	}

	if (!fIgnoreNextBox)
	{
		VwParagraphBox * pvpboxNext = dynamic_cast<VwParagraphBox *>(NextOrLazy());
		if (pvpboxNext)
		{
			if (!dynamic_cast<VwDropCapStringBox *>(pvpboxNext->FirstBox()))
			{
				// Next box exists and is a paragraph and does NOT have a drop cap of its own,
				// so it can wrap around our DC. Therefore we don't need extra space.
				return 0;
			}
		}
	}

	// We want an adjusted height that includes the drop cap
	int height = Height(); // doesn't include height of drop cap

	int dyInch = Root()->DpiSrc().y;
	int lineHeight = MulDiv(m_qzvps->LineHeight(), dyInch, kdzmpInch);

	// If there is a line height set, we return that - drop cap covers two lines. The paragraph
	// currently consists only of one line (which Height() returned), so we have to add a
	// extra line height
	if (lineHeight > 0)
		return lineHeight;

	// Height based on the drop cap is its own height plus our top and bottom total gaps.
	int ourDcHeight = pdcsbFirst->Bottom() + GapBottom(dyInch);
	return max(0, ourDcHeight - height); // don't return less than zero, may have picture or larger text.
}

/*----------------------------------------------------------------------------------------------
	 True if it is undesirable to put a page break between this box and the next.
	 Currently this just depends on the style by default, except that VwParagraphBox
	 overrides to prevent breaks between a one-line drop-cap paragraph and the
	 following, overlapping paragraph.
----------------------------------------------------------------------------------------------*/
bool VwParagraphBox::KeepWithNext()
{
	if (SuperClass::KeepWithNext())
		return true;

	if (!NextOrLazy())
		return false;

	// Check if we consist of multiple lines. If we do then a drop-cap doesn't matter and we
	// should go with the setting of the style, i.e. return false here.
	// If we consist of only one line and the first box is a drop-cap box, then we want to
	// return true to keep this paragraph together with the first line of the next paragraph.
	// The following code dupliactes code in ExtraHeightIfNotFollowedByPara(), but unfortunately
	// that method doesn't tell us the reason why we don't need extra height.
	VwDropCapStringBox * pdcsbFirst = dynamic_cast<VwDropCapStringBox *>(this->FirstBox());
	if (!pdcsbFirst)
		return false; // para doesn't start with a drop-cap

	// Determine if we have one or multiple lines in this paragraph
	int yFirstBaseline = pdcsbFirst->Ascent() + pdcsbFirst->Top();
	for (VwBox * pbox = pdcsbFirst->NextOrLazy(); pbox; pbox = pbox->NextOrLazy())
	{
		if (pbox->Ascent() + pbox->Top() != yFirstBaseline)
			return false; // more than one line
	}

	// current para consists of just one line and starts with drop-cap. Now what about
	// the next paragraph?
	VwParagraphBox * pvpboxNext = dynamic_cast<VwParagraphBox *>(NextOrLazy());
	if (pvpboxNext)
	{
		if (!dynamic_cast<VwDropCapStringBox *>(pvpboxNext->FirstBox()))
		{
			// The next paragraph doesn't have a drop cap, so we want to keep together with
			// that paragraph
			return true;
		}
	}

	// Next box is either not a paragraph or has its own drop cap
	return false;
}

/*----------------------------------------------------------------------------------------------
	 True if it is undesirable to put a page break in the middle of this paragraph.
----------------------------------------------------------------------------------------------*/
bool VwParagraphBox::KeepTogether()
{
	return Style()->KeepTogether();
}

/*----------------------------------------------------------------------------------------------
	 True if this paragraph has the WidowOrphanControl attribute set, i.e. there the first line
	 of the paragraph should not be on its own at the bottom of the page (orphan), nor the last
	 line of the paragraph on its own at the top of the page (widow).
----------------------------------------------------------------------------------------------*/
bool VwParagraphBox::ControlWidowsAndOrphans()
{
	return Style()->WidowOrphanControl();
}

/*----------------------------------------------------------------------------------------------
	Return the box that follows or preceeds the given box in physical order
	(based on the paragraph direction).
	@param vbox			- vector containing boxes in physical order
	@param nDir			- 1 if we want the following box, -1 if we want the previous
----------------------------------------------------------------------------------------------*/
VwBox * VwParagraphBox::GetAdjPhysicalBox(VwBox * pbox, BoxVec & vbox, int nDir)
{
	for (int ibox = 0; ibox < vbox.Size(); ibox++)
	{
		if (vbox[ibox] == pbox)
		{
			if (nDir > 0 && ibox >= vbox.Size() - 1)
				return NULL;
			else if (nDir < 0 && ibox == 0)
				return NULL;
			else
				return vbox[ibox + nDir];
		}
	}
	Assert(false);
	return NULL;
}


const int kMaxUnderlineSegs = 1000;

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
bool VwParagraphBox::FindNiceBreak(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst,
	int ysStart, int * pysEnd, bool fDisregardKeeps)
{
	if ((!fDisregardKeeps) && Style()->KeepTogether())
		return false; // Can't nicely divide a keeptogether box.

	BoxVec vbox;
	int ysTop, ysBottom;
	int clines = 0;
	int dysTagAbove, dysTagBelow;
	ComputeTagHeights(pvpi->m_pvg, Source()->Overlay(), rcSrc.Height(), dysTagAbove,
		dysTagBelow);
	Rect rcSrcChild = rcSrc; // rect in which to interpret child box coords.
	rcSrcChild.Offset(-m_xsLeft, -m_ysTop);

	for (VwBox * pboxLine = m_pboxFirst; pboxLine && pboxLine->Top() != knTruncated; )
	{
		pboxLine = GetALineTB(pboxLine, vbox, rcSrc.Height(), &ysTop, &ysBottom);
		clines++;
		int ydEnd = rcSrcChild.MapYTo(ChooseSecondIfInverted(ysBottom + dysTagBelow, ysTop - dysTagAbove), rcDst);
		if (IsVerticallyAfter(ydEnd, TrailingEdge(pvpi->m_rcDoc)))
		{
			// This line won't fit. Break right before it, or not at all.
			if (clines <= 1)
				return false; // Can't split para unless at least 1 line fit.
			if (clines == 2 && m_pboxFirst->IsDropCapBox())
				return false; // Can't split between 1st two lines of DC para.
			// Start next page at the leading edge of the first line that didn't fit, adjusted to
			// container coordinates.
			*pysEnd = ChooseSecondIfInverted(ysTop - dysTagAbove, ysBottom + dysTagBelow) + Top();
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Print the page.
	ysStart  and ysEnd are relative to the container's top.
	(We could truncate lines beyond the page boundary, but so far have not bothered. They'll
	get clipped.)
	(for inverted views: ysEnd is less then ysStart; ysStart is the 'bottom' of the page)
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::PrintPage(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst,
	int ysStart, int ysEnd)
{
	Rect rcSrcChild = rcSrc;
	rcSrcChild.Offset(-m_xsLeft, -m_ysTop);
	DrawBorder(pvpi->m_pvg, rcSrc, rcDst);
	DrawForeground(pvpi->m_pvg, rcSrc, rcDst, ysStart - Top(), ysEnd - ysStart);
}

// This array also needs to be kept in sync with FwBulletsTab.cs
// Removal or ediing of bullets would require a data migration. Adding additional items shouldn't need a data migration.
static const OLECHAR s_rgszBulletOptions[] = {
	0x00B7, 0x2022, 0x25CF, 0x274D, 0x25AA, 0x25A0, 0x25AB, 0x25A1, 0x2751, 0x2752,
	0x2B27, 0x29EB, 0x25C6, 0x2756, 0x2318, 0x261E, 0x271E, 0x271E, 0x2730, 0x27A2,
	0x27B2, 0x2794, 0x2794, 0x21E8, 0x2713
};

/*----------------------------------------------------------------------------------------------
	Generate the paragraph number that should be applied to this paragraph. This should only
	be called if there is a style that requires numbering; it assumes that the current para
	is in fact numbered.

	(Note: we use INT_MIN rather than -1 for our exception value because it is just conceivable
	the user might want a sequence of negative paragraph numbers including -1. For example,
	I've seen a sequence of "steps towards Christianity" list that starts at -7 and proceeds
	up to 3.)

	Change by JohnT, 18 June: we can't assume that if this paragraph has a start-at, we should
	use that value absolutely. Problem is that "start at" can be specified in a style, and
	the style applied to several paragraphs, and that should not produce a sequence all
	numbered the same. For explicit numbering, I fixed this by applying the "start at" value
	to only the first paragraph (this is still in place, though I think no longer needed);
	but for a style there is no corresponding trick to use.

	So, the logic I've chosen is that if a paragraph has the same numbering style and start-at
	value as the previous paragraph, its number is one greater than the previous.

	To spell it out more precisely, suppose we know the number of the previous paragraph
	(nPrev, initially zero), together with the start-at and vbn (style) values for this and
	the previous paragraph. (For the first para we use a vbn-previous that makes the 'previous'
	paragraph not numbered at all.) Then the number of the current paragraph is computed on the
	following rules:

	1. If this vbn is different from the previous one,
		1.1 if this paragraph has a start-at, use it
		1.2 otherwise this paragraph is number 1.
	2. If this vbn is the same as the previous para,
		2.1 if this para has no start-at its number is nPrev + 1
		2.2 otherwise, if this para has the same start-at as the previous para it is nPrev + 1.
		2.3 otherwise use the paragraph's own start-at.
----------------------------------------------------------------------------------------------*/
int VwParagraphBox::ParaNumber()
{
	VwGroupBox * pgboxCont = Container();
	if (!pgboxCont)
		return 1;

	// This is the number of the paragraph we are processing. When that is *this, it is the
	// return value.
	int nRet;
	// Set this to the last lazy box that has to be expanded in order to determine
	// our number. A lazy box needs to be expanded if we don't find a subsequent
	// paragraph that has a different vbn or start-at.
	VwLazyBox * plzbProblem = NULL;
	// First time round we start here. If we have to iterate because of lazy box
	// expansion, we may be able to start at a better place.
	VwBox * pboxStartAt = pgboxCont->FirstBox();

	// Note what we need to layout if we have to expand a lazy box.
	VwBox * pboxFirstLayout = NULL;
	VwBox * pboxLimLayout = NULL;

	// This remembers the position of the closure last expanded. That's good enough
	// to use for the Adjust, because it only wants to know whether the top left
	// is above or below the scroll position, and everything we expand here is above.
	Rect rcThisOld;
	VwRootBox * prootb = Root();

	// Normally the first iteration of this loop is all there is. We repeat if
	// we have had to expand a lazy box to find a starting point for the number
	// sequence.
	for (;;)
	{
		// Treat the previous paragraph as being number 0, but with no relevant vbn or
		// start-at.
		int vbnPrev = 0;
		int nStartAtPrev = INT_MIN;
		nRet = 0;
		for (VwBox * pbox = pboxStartAt; ; pbox = pbox->NextOrLazy())
		{
			Assert(pbox); // we should reach *this!
			VwLazyBox * plzb = dynamic_cast<VwLazyBox *>(pbox);
			if (plzb)
			{
				// We don't care about its style, because it may expand to other boxes
				// with different styles. It will become a problem one unless we later
				// find a definite start position.
				plzbProblem = plzb;
				Assert(plzb->NextOrLazy()); // we expect *this to follow somewhere
				// Set the 'prev' vbn to match the next box, and the previous start-at to
				// undefined. This will prevent us from deciding we don't need to know
				// about the box before the next one, when it is current.
				vbnPrev = plzb->NextOrLazy()->Style()->BulNumScheme();
				nStartAtPrev = INT_MIN;
				continue; // can't be *this, it's a lazy box
			}
			int vbnCur = pbox->Style()->BulNumScheme();
			int nStartAtCur = pbox->Style()->NumStartAt();
			// Implement the logic in the method header
			if (vbnCur != vbnPrev)
			{
				nRet = nStartAtCur == INT_MIN ? 1 : nStartAtCur;
				// This is a definite starting point, there has been a prevous box with
				// different properties.
				plzbProblem = NULL;
				// Don't need to look at earlier boxes again, if we loop to expand lazy box.
				pboxStartAt = pbox;
			}
			else
			{
				if (nStartAtCur == INT_MIN || nStartAtCur == nStartAtPrev)
					nRet++; //Makes it one, if no previous box at all.
				else
				{
					// Again, this signifies a restart position, where the value does
					// not depend on the previous box or anything before it, since this
					// box has a start-at and the previous one had a different one or none.
					nRet = nStartAtCur == INT_MIN ? 1 : nStartAtCur;
					plzbProblem = NULL;
					pboxStartAt = pbox;
				}
			}
			vbnPrev = vbnCur;
			nStartAtPrev = nStartAtCur;

			// If we found the box we're looking for, stop and see if we need to deal
			// with a lazy box.
			if (pbox == this)
				break;
		}
		// If there is no problem lazy box we are done; use the value we just computed
		if (!plzbProblem)
			break;
		// If there is a lazy box do the minimum expansion at the end of it. Hope this
		// allows us to figure our number, and try again.
		// Don't do layout of the boxes expanded until we are done, because that could
		// lead to recursive calls to this function, which confuses things.
		HoldGraphics hg(prootb);
		rcThisOld = plzbProblem->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
		int citems = plzbProblem->CItems();

		// If we expand multiple lazy boxes, or expand the same one multiple times,
		// we have to figure an overall list of boxes to re-layout. First get the ones
		// that need it just because of expanding this one.
		// The interesting cases are:
		// 1. First expansion. Just remember all the values we get from the expansion.
		// 2. Expanding one we already partly expanded. In this case, pboxFirstLayout1
		// is the box we're expanding, and we need to change pboxFirstLayout to the new value.
		// 3. Expanding one that was produced by a previous expansion (nested lazy boxes).
		// In this case we don't want to change anything, unless the box we're expanding is
		// pboxFirstLayout, and goes away, in which case we want to change it to the
		// first box that replaced it.
		// 4. Expanding one before one we previously expanded (which has been completely
		// expanded). In this case, like 2, we want to keep the new pboxFirstLayout.
		// How do we distinguish cases 3 and 4? In case 3, plzbProblem either is or
		// follows pboxFirstLayout; in cases 2 and 4 (which we want to treat the same),
		// plzbProblem is before pboxFirstLayout, since that came either from expanding
		// part of plzbProblem or a later lazy box.
		bool fCase3 = plzbProblem->IsOrFollows(pboxFirstLayout);
		VwBox * pboxFirstLayout1;
		VwBox * pboxLimLayout1;
		VwBox * pboxExpand = plzbProblem->ExpandItemsNoLayout(citems - 1, citems,
			&pboxFirstLayout1, &pboxLimLayout1);
		if (pboxStartAt == plzbProblem)
		{
			// problem lazy box is our first one. Start over with new first box.
			pboxStartAt = pgboxCont->FirstBox();
		}
		// Now figure how the new list of boxes needing layout combines with the old,
		// if any.
		// We begin by expanding at the end of the last lazybox, so the limit will not
		// change after the first time; it will be the box after that last lazy box.
		if (!pboxLimLayout)
			pboxLimLayout = pboxLimLayout1;
		if (fCase3)
		{
			if (pboxFirstLayout == plzbProblem)
				pboxFirstLayout = pboxExpand;
		}
		else
		{
			// cases 2 and 4, update to the new value.
			pboxFirstLayout = pboxFirstLayout1;
		}
	}

	// For sync'd views the call to ExpandItemsNoLayout already layed out and adjusted
	// the boxes, so there is no need to call it again.
	if (pboxFirstLayout && !prootb->GetSynchronizer())
	{
		// Need to make sure the expanded boxes get laid out, and any consequent adjustments
		// in scroll position get made. The safest thing is to lay out everything from
		// the sta
		VwDivBox * pdboxContainer = dynamic_cast<VwDivBox *>(pboxFirstLayout->Container());

		// We need to get the size of our rootbox before we layout anything.
		Rect rcRootOld;
		{ // block for HoldGraphics
			HoldGraphics hg(prootb);
			rcRootOld = prootb->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
		}
		prootb->AdjustBoxPositions(rcRootOld, pboxFirstLayout, pboxLimLayout,
			rcThisOld, pdboxContainer, NULL, NULL, true);
	}

	if (nRet > 14999)
		nRet = ((nRet - 1) % 14999) + 1;
	return nRet;
}

static const wchar_t * s_rgszDig0[] = {
	L"", L"i", L"ii", L"iii", L"iv", L"v", L"vi", L"vii", L"viii", L"ix" };
static const wchar_t * s_rgszDig10[] = {
	L"", L"x", L"xx", L"xxx", L"xl", L"l", L"lx", L"lxx", L"lxxx", L"xc" };
static const wchar_t * s_rgszDig100[] = {
	L"", L"c", L"cc", L"ccc", L"cd", L"d", L"dc", L"dcc", L"dccc", L"cm" };
static const wchar_t * s_rgszDig1000[] = {
	L"", L"m", L"mm", L"mmm", L"mmmm", L"mmmmm", L"mmmmmm", L"mmmmmmm", L"mmmmmmmm",
		L"mmmmmmmmm"};
static const wchar_t * s_rgszDig10000[] = {
	L"ten", L"mmmmmmmmmm", L"mmmmmmmmmmmmmmmmmmmm", L"mmmmmmmmmmmmmmmmmmmmmmmmmmmmmm",
		L"mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm"};

// TODO: remove pragma and resolve issues
#pragma warning(disable: 4996)

/*----------------------------------------------------------------------------------------------
	Generate the string and font that should be used to display the bullet/number string
	at the start of the paragraph.
	Set the VwGraphics to the proper state for drawing the string.
	Review (SharonC): Do we need to handle font variations here?
----------------------------------------------------------------------------------------------*/
StrUni VwParagraphBox::GetBulNumString(IVwGraphics * pvg, COLORREF * pclrUnder, int * punt)
{
	// Set the ws factory in the property store just in case it hasn't been set yet.
	ILgWritingSystemFactoryPtr qwsf;
	GetWritingSystemFactory(&qwsf);
	m_qzvps->putref_WritingSystemFactory(qwsf);

	int vbn = m_qzvps->BulNumScheme();
	OLECHAR rgchNum[100];  // Holds representation of number
	OLECHAR * pch = rgchNum; // several branches use this
	OLECHAR * pchLim = rgchNum + isizeof(rgchNum)/isizeof(OLECHAR);
	int nVal = 0;
	if (vbn == kvbnNone)
		return StrUni();
	// Otherwise we need to set the appropriate font etc. Default is to use para props.
	CachedProps chrp = *(m_qzvps->Chrp());
	StrUni stuFontProps = m_qzvps->NumFont();

	*pclrUnder = chrp.m_clrUnder;
	*punt = chrp.m_unt;
	const OLECHAR * pchProps = stuFontProps.Chars();
	const OLECHAR * pchPropsLim = pchProps + stuFontProps.Length();

	while (pchProps < pchPropsLim)
	{
		int tpt = *pchProps++;
		if (tpt == ktptFontFamily)
		{
			u_strcpy(chrp.szFaceName, pchProps);
			break; // no more properties
		}
		// It must be a numeric property
		nVal = *pchProps + ((*(pchProps + 1)) << 16);
		pchProps += 2;
		switch(tpt)
		{
		case ktptItalic:
			chrp.ttvItalic = (nVal == 2);
			if (nVal == kttvOff)
				chrp.ttvItalic = kttvOff;
			else if (nVal == kttvForceOn)
				chrp.ttvItalic = kttvForceOn;
			else if (nVal == kttvInvert)
				chrp.ttvItalic = kttvInvert;
			break;
		case ktptBold:
				if (nVal == kttvOff)
					chrp.ttvBold = kttvOff;
				else if (nVal == kttvForceOn)
					chrp.ttvBold = kttvForceOn;
				else if (nVal == kttvInvert)
					chrp.ttvBold = kttvInvert;
				else
					chrp.ttvBold = (nVal >= 550) ? kttvForceOn : kttvOff;
			break;
		case ktptSuperscript:
			chrp.ssv = (unsigned char) nVal;
			break;
		case ktptFontSize:
			chrp.dympHeight = nVal;
			break;
		case ktptOffset:
			chrp.dympOffset = nVal;
			break;
		case ktptForeColor:
			chrp.clrFore = nVal;
			break;
		case ktptBackColor:
			chrp.clrBack = nVal;
			break;

		case ktptUnderline:
			*punt = nVal;
			break;
		case ktptUnderColor:
			*pclrUnder = nVal;
			break;
		}
	}

	switch(vbn)
	{
	default:
		if (vbn >= kvbnBulletBase &&
			vbn < kvbnBulletBase + isizeof(s_rgszBulletOptions) / isizeof(OLECHAR))
		{
			// bullet
			static OleStringLiteral fontName(L"Quivira");
			u_strcpy(chrp.szFaceName, fontName); // only font that works for bullets
			CheckHr(pvg->SetupGraphics(&chrp));
			StrUni stuText;
			stuText.Format(L"%c", s_rgszBulletOptions[vbn - kvbnBulletBase]);
			return stuText;
		}
		// Otherwise it is invalid
		Assert(false);
		ThrowHr(E_UNEXPECTED);
		break;
	case kvbnNone:
		return StrUni(); // don't bother to setup graphics
	case kvbnArabic:
		nVal = ParaNumber();
LArabic:
		_itow_s(nVal,pch,pchLim-pch,10);
		pch += wcslen(pch);
		//*pch++ = ' ';
		*pch = 0;
		break;
	case kvbnArabic01:
		nVal = ParaNumber();
		if (nVal < 10)
			*pch++ = '0';
		goto LArabic;
	case kvbnRomanUpper:
	case kvbnRomanLower:
		nVal = ParaNumber();
		if (nVal > 14999)
			goto LArabic;
		wcscpy(pch, StrUni(s_rgszDig1000[nVal / 1000]).Chars());
		pch += wcslen(pch);
		wcscpy(pch, StrUni(s_rgszDig100[(nVal / 100) % 10]).Chars());
		pch += wcslen(pch);
		wcscpy(pch, StrUni(s_rgszDig10[(nVal / 10) % 10]).Chars());
		pch += wcslen(pch);
		wcscpy(pch, StrUni(s_rgszDig0[nVal % 10]).Chars());
		if (vbn == kvbnRomanUpper)
		{
			for (pch = rgchNum; *pch; pch++)
				*pch -= 'a' - 'A';
		}
		else
		{
			pch += wcslen(pch);
		}
		//*pch++ = ' '; // after the conversion; we don't want to convert the space
		*pch = 0;
		break;
	case kvbnLetterUpper:
	case kvbnLetterLower:
		nVal = ParaNumber();
		StrAnsi sta;

		// Limit Length to a count of 780
		if (nVal > 780)
			nVal = ((nVal - 1) % 780) + 1;

		sta.Format(((vbn == kvbnLetterUpper) ? "%O" : "%o"), nVal);
		for (int ich = 0; ich < sta.Length(); ich++)
			*pch++ = sta[ich];
		*pch = 0;
		break;
	}
	StrUni stuFaceName(chrp.szFaceName);
	stuFaceName = FwStyledText::FontMarkupToFontName(stuFaceName);
	u_strcpy(chrp.szFaceName, stuFaceName.Chars());
	// If we drop out of the switch, as opposed to returning, we have a number in
	// rgchNum. Now we need to combine it with following and preceding text, if any.
	CheckHr(pvg->SetupGraphics(&chrp));
	StrUniBufSmall stubNum(rgchNum);
	StrUni stuText;
	stuText.Format(L"%s%s%s", m_qzvps->NumTxtBefore().Chars(), stubNum.Chars(),
		m_qzvps->NumTxtAfter().Chars());
	return stuText;
}

// TODO: remove pragma after issues are resolved
#pragma warning(default: 4996)

/*----------------------------------------------------------------------------------------------
	Overridden to ensure that if dest coords are different, we use correct cumulative widths
	in dest coords to position boxes on a line. Also handles drawing underlines if requested.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
	DrawForeground(pvg, rcSrc, rcDst, ChooseSecondIfInverted(-1, Height() + 1), ChooseSecondIfInverted(INT_MAX, INT_MIN));
}

// Info that varies from one line to another.
struct LineInfo
{
	BoxVec vbox;
	// These two variables become the actual top and bottom of the line, that is, the top of the highest
	// box and the bottom of the lowest. They are used for things like deciding where to draw overlay tags.
	int ysTop;
	int ysBottom;
};

class DrawForegroundMethod
{
public:
	IVwGraphics * pvg;
	Rect rcSrc;
	Rect rcDst;
	int ysTopOfPage;
	int dysPageHeight;
	bool fDisplayPartialLines;
	VwParagraphBox * pvpbox;
	ILgWritingSystemFactoryPtr qwsf;
	int xdLeftClip, ydTopClip, xdRightClip, ydBottomClip;
	Rect rcSrcOrig;

	DrawForegroundMethod(IVwGraphics * pvg1, Rect rcSrc1, Rect rcDst1, int ysTopOfPage1,
	int dysPageHeight1, bool fDisplayPartialLines1, VwParagraphBox * pvpbox1)
	{
		pvg = pvg1;
		rcSrc = rcSrcOrig = rcSrc1;
		rcDst = rcDst1;
		ysTopOfPage = ysTopOfPage1;
		dysPageHeight = dysPageHeight1;
		fDisplayPartialLines = fDisplayPartialLines1;
		pvpbox = pvpbox1;
	}

	int Left()
	{
		return pvpbox->Left();
	}

	int Top()
	{
		return pvpbox->Top();
	}

	VwPropertyStore * Style()
	{
		return pvpbox->m_qzvps;
	}

	int ChooseSecondIfInverted(int first, int second)
	{
		return pvpbox->ChooseSecondIfInverted(first, second);
	}

	bool IsVerticallySameOrAfter(int first, int second)
	{
		return pvpbox->IsVerticallySameOrAfter(first, second);
	}

	bool IsVerticallyAfter(int first, int second)
	{
		return pvpbox->IsVerticallyAfter(first, second);
	}

	VwTxtSrc * Source()
	{
		return pvpbox->Source();
	}

	bool RightToLeft()
	{
		return pvpbox->RightToLeft();
	}

	VwBox * FirstBox()
	{
		return pvpbox->FirstBox();
	}

	void Run()
	{
	#ifdef _DEBUG
	#ifdef _DEBUG_SHOW_BOX
		pvpbox->DebugDrawBorder(pvg, rcSrc, rcDst);
	#endif
	#endif


		rcSrc.Offset(-Left(), -Top());

		// end of stuff to draw in internal coordinate system.
		int ysEnd = ysTopOfPage + dysPageHeight;
		int ysStart = ysTopOfPage;
		LineInfo line1, line2;
		LineInfo * plineCurrent = &line1;
		LineInfo * plinePrevious = NULL;
		LineInfo *plineLastLine = plineCurrent; // last processed line
		if (!FirstBox())
			return;

		// Set the ws factory in the property store just in case it hasn't been set yet.
		pvpbox->GetWritingSystemFactory(&qwsf);
		Style()->putref_WritingSystemFactory(qwsf);

		CheckHr(pvg->GetClipRect(&xdLeftClip, &ydTopClip, &xdRightClip, &ydBottomClip));

		int clines = 0;
		for (VwBox * pboxLine = FirstBox(); pboxLine && pboxLine->Top() != knTruncated; )
		{
			clines++;
			pboxLine = pvpbox->GetALineTB(pboxLine, plineCurrent->vbox, rcSrc.Height(), &(plineCurrent->ysTop), &(plineCurrent->ysBottom));
			plineLastLine = plineCurrent;

			// Need to calculate the top and bottom slightly differently for determining whether line is on page.
			// All the code that deals with page breaks uses GetLineTopAndBottom, which can give a different
			// answer by including some of the space between adjacent lines (roughly evenly divided).
			// We also need to use these values for ysTop and ysBottom if doing exact line spacing.
			// Enhance JohnT: it is possible we could avoid the need to do this additional calculation
			// if we aren't close to the boundary; for example, if ysTop > ysStart and ysBottom < ysEnd,
			// the box is certainly on the page. It's harder to detect that it is certainly off the page,
			// though, and it makes for complex additional paths through the method for an optimization of
			// doubtful significance.
			// (It IS important to be very exact about which lines to draw, though. Otherwise, in page layout,
			// it's easy to get the line of text drawn on one page and the selection on another!)
			int ysTopForOnPage = plineCurrent->ysTop;
			int ysBottomForOnPage = plineCurrent->ysBottom;
			if (plineCurrent->vbox.Size()) // probably always true, but for robustness...
				pvpbox->GetLineTopAndBottom(pvg, plineCurrent->vbox[0], &ysTopForOnPage, &ysBottomForOnPage, rcDst, rcDst, false);
			if (Style()->ExactLineHeight())
			{
				// The adjusted values are more reliable in this case; lines may be overlapping, hopefully not by much!
				plineCurrent->ysTop = ysTopForOnPage;
				plineCurrent->ysBottom = ysBottomForOnPage;
			}

			int leadingEdge = ChooseSecondIfInverted(ysTopForOnPage, ysBottomForOnPage);
			int trailingEdge = ChooseSecondIfInverted(ysBottomForOnPage, ysTopForOnPage);

			if (!fDisplayPartialLines && IsVerticallyAfter(trailingEdge, ysEnd))
				return;
			else if (fDisplayPartialLines && !IsVerticallySameOrAfter(ysEnd, leadingEdge))
				return;

			int leadingClip = ChooseSecondIfInverted(ydTopClip - 5, ydBottomClip + 5);
			int trailingClip = ChooseSecondIfInverted(ydBottomClip + 5, ydTopClip - 5);

			if (IsVerticallySameOrAfter(ysStart, trailingEdge))
				continue; // line is before start of page
			if (IsVerticallyAfter(rcSrc.MapYTo(leadingEdge, rcDst), trailingClip))
				return; // line comes after end of clip.
			if (IsVerticallyAfter(leadingClip, rcSrc.MapYTo(trailingEdge, rcDst)))
				continue; // line is before clip rect.

			//if (ysBottomForOnPage <= ysStart)
			//	continue;
			//// Truncate more conservatively for clipping
			//if (rcSrc.MapYTo(ysTopForOnPage, rcDst) > ydBottomClip + 5)
			//	return;
			//if (rcSrc.MapYTo(ysBottomForOnPage, rcDst) < ydTopClip - 5)
			//	continue;
			DrawOneLine(clines, plineCurrent->vbox, plineCurrent->ysTop, plineCurrent->ysBottom, false);

			// Any colored background in this line?
			bool fAnyColoredBackground = false;
			for (int ibox = 0; ibox < plineCurrent->vbox.Size() && !fAnyColoredBackground; ibox++)
			{
				VwStringBox * psbox = dynamic_cast<VwStringBox *>(plineCurrent->vbox[ibox]);
				if (psbox == NULL)
					continue;
				int ich = psbox->IchMin();
				int dichLim;
				CheckHr(psbox->Segment()->get_Lim(psbox->IchMin(),&dichLim));
				int ichLim = ich + dichLim;
				while (ich < ichLim)
				{
					LgCharRenderProps chrp;
					int ichMinRun, ichLimRun;
					CheckHr(Source()->GetCharProps(ich, &chrp, &ichMinRun, &ichLimRun));
					if (chrp.clrBack != (COLORREF)kclrTransparent)
					{
						fAnyColoredBackground = true;
						break;
					}
					ich = ichLimRun;
				}
			}
			if (fAnyColoredBackground)
			{
				// The line we just drew had colored background. We want to redraw the previous line, if possible.
				if (plinePrevious)
				{
					DrawOneLine(clines, plinePrevious->vbox, plinePrevious->ysTop, plinePrevious->ysBottom, true);
				}
				else if (clines == 1)
				{
					VwParagraphBox * pvpboxPrev = dynamic_cast<VwParagraphBox *>(pvpbox->Container()->BoxBefore(pvpbox));
					if (pvpboxPrev)
						pvpboxPrev->DrawLastLine(pvg, rcSrcOrig, rcDst, ysTopOfPage, dysPageHeight, fDisplayPartialLines);
				}
			}
			// Swap current and previous.
			plinePrevious = plineCurrent;
			plineLastLine = plinePrevious;
			if (plineCurrent == &line1)
				plineCurrent = &line2;
			else
				plineCurrent = &line1;
		}
		if (plinePrevious)
			DoFinalLine(clines, plinePrevious);
		else
			plinePrevious = plineCurrent;
		if (clines == pvpbox->MaxLines())
		{
			// We may need to draw an ellipsis. Did we have to truncate?
			// We find the last string box, get its limit, and then add the number of
			// following non-string boxes. This is the total number of characters
			// accounted for. If it is less than the length of the text source, we truncated.
			VwStringBox * psboxLast = NULL;
			for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->Next())
			{
				VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
				if (psbox)
					psboxLast = psbox;
			}
			int cchShown = 0; // Winds up being the number of characters displayed.
			// The first non-string box after psboxLast. This default is appropriate
			// if psboxLast is null.
			VwBox * pboxTrailer = FirstBox();
			if (psboxLast)
			{
				int dichLim;
				CheckHr(psboxLast->Segment()->get_Lim(psboxLast->IchMin(),&dichLim));
				cchShown = psboxLast->IchMin() + dichLim;
				pboxTrailer = psboxLast->Next();
			}
			// Now we have cchShown as the character count up to the end of the last text
			// segment. If there are subsequent non-text boxes, they count one each.
			for (; pboxTrailer; pboxTrailer = pboxTrailer->Next())
				cchShown++;
			if (cchShown < Source()->CchRen() && plinePrevious->vbox.Size())
			{
				// We will draw an ellipsis. Find the physically right-most box on the line.
				// TODO: deal with right-to-left
				VwBox * pboxLastOnLine = *(plinePrevious->vbox).Top();
#if WIN32
				OLECHAR rgchEllipsis[] = L"..."; // Review JohnT: use Unicode one-char ellipsis?
#else // !WIN32
				static const OleStringLiteral literalEllipsis = L"...";
				OLECHAR * rgchEllipsis = literalEllipsis;
#endif // !WIN32
				const int cchEllipsis = 3;
				CheckHr(pvg->SetupGraphics(Style()->Chrp()));
				int dxdEllipsis, dydEllipsis;
				CheckHr(pvg->GetTextExtent(cchEllipsis, rgchEllipsis, &dxdEllipsis, &dydEllipsis));
				int dydAscentEllipsis;
				CheckHr(pvg->get_FontAscent(&dydAscentEllipsis));
				int dysBaseline = pboxLastOnLine->Baseline();
				// rcDst winds up being just right for the very last box drawn, which is
				// pboxLastOnLine.
				int ydBaseline = rcSrc.MapYTo(dysBaseline, rcDst);
				int ydTop = ydBaseline - dydAscentEllipsis;
				int xdLeft = rcSrc.MapXTo(pboxLastOnLine->Right(), rcDst);
				CheckHr(pvg->DrawText(xdLeft, ydTop, cchEllipsis, rgchEllipsis, 0));
			}
		}

		OLECHAR boundaryMarkChar = pvpbox->GetBoundaryMarkChar();
		if (boundaryMarkChar)
		{
			// We will draw a boundary mark. Find the physically last box on the line.
			// We don't care if it is outside of the visible area
			VwBox* pboxLastOnLine;

			if (RightToLeft())
				pboxLastOnLine = *(plineLastLine->vbox).Bottom();
			else
				pboxLastOnLine = *(plineLastLine->vbox).Top();

			CheckHr(pvg->SetupGraphics(Style()->Chrp()));

			int dxdMark, dydMark;
			CheckHr(pvg->GetTextExtent(1, &boundaryMarkChar, &dxdMark, &dydMark));
			int dydAscentMark;
			CheckHr(pvg->get_FontAscent(&dydAscentMark));
			int dysBaseline = pboxLastOnLine->Baseline();
			// rcDst winds up being just right for the very last box drawn, which is
			// pboxLastOnLine.
			int ydBaseline = rcSrc.MapYTo(dysBaseline, rcDst);
			int ydTop = ydBaseline - dydAscentMark;
			int xdLeft;
			if (RightToLeft())
				xdLeft = rcSrc.MapXTo(pboxLastOnLine->Left() - dxdMark, rcDst);
			else
				xdLeft = rcSrc.MapXTo(pboxLastOnLine->Right(), rcDst);
			// if specified, use a highlight color.
			// ENHANCE: For now, the highlight color is hardcoded. Set it with a callback.
			if (pvpbox->m_BoundaryMark == endOfParagraphHighlighted || pvpbox->m_BoundaryMark == endofSectionHighlighted)
			{
				CheckHr(pvg->put_BackColor(RGB(255,255,0)));
				// Draw the rectangle.
				CheckHr(pvg->DrawRectangle(xdLeft, ydTop, xdLeft + dxdMark, ydTop + dydMark));
			}
			CheckHr(pvg->DrawText(xdLeft, ydTop, 1, &boundaryMarkChar, 0));
		}
	}

	virtual void DoFinalLine(int clines, LineInfo * pline)
	{
	}

	virtual void DrawOneLine(int clines, BoxVec & vbox, int ysTop, int ysBottom, bool fSuppressBackground)
	{
		int rgxdLefts[kMaxUnderlineSegs];
		int rgxdRights[kMaxUnderlineSegs];
		int rgydTops[kMaxUnderlineSegs];
		// Handle bullets and numbers.
		if (clines == 1)
		{
			// Draw number if any.
			COLORREF clrUnder;
			int unt;
			StrUni stuBulNum = pvpbox->GetBulNumString(pvg, &clrUnder, &unt);
			if (stuBulNum.Length())
			{
				int dypAscent;
				CheckHr(pvg->get_FontAscent(&dypAscent));
				int dxdWidth, dydHeight;
				CheckHr(pvg->GetTextExtent(stuBulNum.Length(),
					const_cast<OLECHAR *>(stuBulNum.Chars()), &dxdWidth, &dydHeight));
				int ysTopNum = FirstBox()->Top() + FirstBox()->Ascent() - dypAscent;
				// Special case: check for number on line by itself.
				if (ysTop != pvpbox->GapTop(rcSrc.Height()))
				{
					int dyTagAbove, dyTagBelow;
					pvpbox->ComputeTagHeights(pvg, Source()->Overlay(), rcSrc.Height(), dyTagAbove,
						dyTagBelow);
					// The messes up the font selection we need to do the drawing. Safest is
					// to compute it all again. this is an unusual case.
					pvpbox->GetBulNumString(pvg, &clrUnder, &unt);
					if (pvpbox->GapTop(rcSrc.Height()) + dyTagAbove != ysTop)
					{
						// line by itself: put at very top
						ysTopNum = pvpbox->GapTop(rcSrc.Height());
					}
				}
				int ydTopNum = rcSrc.MapYTo(ysTopNum, rcDst);
				int xdNum;
				if (RightToLeft())
				{
					// m_dxsRightEdge is the right edge of the paragraph, including the
					// leading indent, but not including any first line indent.
					xdNum = rcSrc.MapXTo(pvpbox->m_dxsRightEdge, rcDst) -
						std::max(0, MulDiv(Style()->FirstIndent(), rcSrc.Width(), kdzmpInch)) -
						dxdWidth;
				}
				else
				{
					xdNum = rcSrc.MapXTo(
						pvpbox->GapLeading(rcSrc.Width()) +
							MulDiv(Style()->FirstIndent(), rcSrc.Width(), kdzmpInch),
						rcDst);
				}
				CheckHr(pvg->DrawText(xdNum, ydTopNum, stuBulNum.Length(),
					const_cast<OLECHAR *>(stuBulNum.Chars()), 0));
				if (unt != kuntNone)
				{
					if (clrUnder == (unsigned long) kclrTransparent)
						clrUnder = Style()->Chrp()->clrFore;

					// top of underline 1 pixel below baseline
					int ydDrawAt = ydTopNum + dypAscent + rcDst.Height() / 96;
					int dydOffset = rcDst.Height() / 96;
					// underline is drawn at most one offset above ydDrawAt and at most 2 offsets below.
					// Skip the work if it is clipped.
					if (ydDrawAt - dydOffset < ydBottomClip + 1 && ydDrawAt + dydOffset * 2 > ydTopClip - 1)
					{
						pvpbox->DrawUnderline(pvg, xdNum, xdNum + dxdWidth, ydDrawAt,
								rcDst.Width() / 96, dydOffset,
								clrUnder, unt, rcDst.left);
					}
				}

			}
		}
		// Draw the boxes from left to right, adjusting for their widths.
		// ENHANCE JohnT (v2?): if paragraph is right-aligned, draw from right to left.
		for (int ibox = 0; ibox < vbox.Size(); ibox++)
		{
			VwBox * pbox = vbox[ibox];
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
			if (psbox)
			{
	#ifdef _DEBUG
	#ifdef _DEBUG_SHOW_BOX
	psbox->DebugDrawBorder(pvg, rcSrc, rcDst);
	#endif
	#endif

				if (!psbox->Segment())
				{
					WarnHr(E_UNEXPECTED); // error in rendering
					continue;
				}
				Rect rcSrcChild(rcSrc);
				rcSrcChild.Offset(-psbox->Left() - psbox->GapLeft(rcSrc.Width()),
					-psbox->Top() - psbox->GapTop(rcSrc.Height()));
				int dxdWidth;
				int ichMin = psbox->IchMin();
				ILgSegment * pseg = psbox->Segment();

				// Do underlining in segment, if any (before drawing text! text on top!)
				int dichLim;
				int cxd;
				CheckHr(psbox->Segment()->get_Lim(ichMin, &dichLim));
				int ichLim = ichMin + dichLim;
				int unt;
				COLORREF clrUnder;
				int ichMinRun = ichMin;
				int ichLimRun;
				int dydOffset = rcDst.Height() / 96; // distance between double underline, also up and down for squiggle.
				for (; ichMinRun < ichLim; ichMinRun = ichLimRun)
				{
					Source()->GetUnderlineInfo(ichMinRun, &unt, &clrUnder, &ichLimRun);
					ichLimRun = std::min(ichLimRun, ichLim);
					Assert(ichLimRun > ichMinRun);
					if (unt == kuntNone)
						continue;
					// Get info about where to draw underlines for this run
					try
					{
						int ydApproxUnderline = rcSrcChild.MapYTo(psbox->Ascent(), rcDst);
						// GetCharPlacement seems to be the really expensive part of underlining; don't do it
						// if the underline is nowhere near the clip rectangle. Times 2 and times 3 are both one more multiple
						// than typically needed.
						if (ydApproxUnderline - dydOffset * 2 < ydTopClip - 1 || ydApproxUnderline + dydOffset * 3 > ydBottomClip + 1)
							continue;
						CheckHr(psbox->Segment()->GetCharPlacement(ichMin, pvg, ichMinRun,
							ichLimRun, rcSrcChild, rcDst, true, kMaxUnderlineSegs, &cxd,
							rgxdLefts, rgxdRights, rgydTops));
					}
					catch (Throwable& thr)
					{
						WarnHr(thr.Error());
						continue; // ignore any problems here, just don't draw underlines.
					}
					for (int ixd = 0; ixd < cxd; ixd++)
					{
						// top of underline 1 pixel below baseline
						int ydDrawAt = rgydTops[ixd];
						// underline is drawn at most one offset above ydDrawAt and at most 2 offsets below.
						// Skip the work if it is clipped.
						if (ydDrawAt - dydOffset < ydBottomClip + 1 && ydDrawAt + dydOffset * 2 > ydTopClip - 1)
						{
							int xLeft = max(rgxdLefts[ixd], xdLeftClip - 1);
							int xRight = min(rgxdRights[ixd], xdRightClip + 1);
							pvpbox->DrawUnderline(pvg, xLeft, xRight, ydDrawAt,
								rcDst.Width() / 96, dydOffset,
								clrUnder, unt, rcDst.left);
						}
					}
				}
				if (fSuppressBackground)
					CheckHr(pseg->DrawTextNoBackground(ichMin, pvg, rcSrcChild, rcDst, &dxdWidth));
				else
					CheckHr(pseg->DrawText(ichMin, pvg, rcSrcChild, rcDst, &dxdWidth));

				// Handle overlays for the segment, if required.
				IVwOverlay * pvo = Source()->Overlay();
				if (pvo)
				{
					VwParagraphBox::FobData fobd(vbox, ibox, rcSrc, rcDst);
					VwOverlayFlags vof;
					CheckHr(pvo->get_Flags(&vof));
					if (vof & kgrfofTagAnywhere)
					{
						pvpbox->DrawOverlayTags(pvg, rcSrcChild, rcDst, psbox, vof,
							rcSrc.MapYTo(ysTop, rcDst), rcSrc.MapYTo(ysBottom, rcDst), NULL,
							fobd);
					}
				}
			}
			else
			{
				pbox->Draw(pvg, rcSrc, rcDst);
			}
		}
	}
};

// This class is used to implement drawing just the last line of a paragraph, in transparent background mode.
// To this end, it overrides DrawOneLine so that all normal drawing does nothing.
// It then overrides DoFinalLine to call the base class DrawOneLine on just the final line.
class DrawLastLineMethod: public DrawForegroundMethod
{
public:
	DrawLastLineMethod(IVwGraphics * pvg1, Rect rcSrc1, Rect rcDst1, int ysTopOfPage1,
		int dysPageHeight1, bool fDisplayPartialLines1, VwParagraphBox * pvpbox1)
	: DrawForegroundMethod(pvg1, rcSrc1, rcDst1, ysTopOfPage1, dysPageHeight1, fDisplayPartialLines1, pvpbox1)
	{
	}

	virtual void DoFinalLine(int clines, LineInfo * pline)
	{
		DrawForegroundMethod::DrawOneLine(clines, pline->vbox, pline->ysTop, pline->ysBottom, true);
	}

	virtual void DrawOneLine(int clines, BoxVec & vbox, int ysTop, int ysBottom, bool fSuppressBackground)
	{
	}
};

/*----------------------------------------------------------------------------------------------
	Draw, but no lines that start at or after ysEnd or end before ysStart.
	Note: ysTopOfPage is relative to the top of this box, and may well therefore be negative,
	if the box is somewhere in the middle of the page (or if we called it from the version
	with fewer arguments that draws everything).
	@param fDisplayPartialLines Set to true to display lines even if they don't fit entirely in
								the paragraph box.
	Note: char indexes in here are in rendered characters.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTopOfPage,
	int dysPageHeight, bool fDisplayPartialLines)
{
	DrawForegroundMethod dfm(pvg, rcSrc, rcDst, ysTopOfPage, dysPageHeight,
		fDisplayPartialLines, this);
	dfm.Run();
}

/*----------------------------------------------------------------------------------------------
	like draw, but only the last line gets drawn, and in transparent background mode.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::DrawLastLine(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTopOfPage,
	int dysPageHeight, bool fDisplayPartialLines)
{
	DrawLastLineMethod dfm(pvg, rcSrc, rcDst, ysTopOfPage, dysPageHeight,
		fDisplayPartialLines, this);
	dfm.Run();
}

/*----------------------------------------------------------------------------------------------
	Return the unicode character represented by the VwBoundaryMark enumeration value.
----------------------------------------------------------------------------------------------*/
OLECHAR VwParagraphBox::GetBoundaryMarkChar()
{
	switch (m_BoundaryMark)
	{
	case endOfParagraph:
	case endOfParagraphHighlighted:
		return (0x00B6); // ""
	case endOfSection:
	case endofSectionHighlighted:
		return (0x00A7); // ""
	case none:
	default:
		return 0x0000;
	}
}

/*----------------------------------------------------------------------------------------------
	The box at position ibox in vbox wants to draw an overlay tag that extends beyond itself.
	The fOpening flag indicates whether it is drawing an opening or closing tag, and
	fRightToLeft indicates whether the box in question is internally drawn right-to-left.
	Determine the limit of how far it can draw in the X direction, in dest coordinates.
	rcSrc is set up so that the top left of the paragraph box is 0,0. (Beware: this is not
	the rcSrc passed to DrawOverlayTags!).
----------------------------------------------------------------------------------------------*/
int VwParagraphBox::FindOverlayBoundary(BoxVec vbox, int ibox, bool fOpening, bool fRightToLeft,
	Rect rcSrc, Rect rcDst, IVwGraphics * pvg)
{
	int dxdInch;
	CheckHr(pvg->get_XUnitsPerInch(&dxdInch));

	// Determine whether the box needs to draw a tag to the right or the left of itself.
	bool fOnRight = fRightToLeft ? (!fOpening) : fOpening;

	int xdLim;
	int iboxLim = vbox.Size();
	if (fOnRight)
	{
		// Loop over boxes after ibox looking for relevant boundaries
		for (int ibox2 = ibox + 1; ibox2 < iboxLim; ibox2++)
		{
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(vbox[ibox2]);
			if (!psbox)
				continue; // picture or similar, we can draw anywhere above or below it.
			// If this box has a different internal direction from the starting box,
			// it is too confusing to allow overlap.
			ComBool fRightToLeft2;
			CheckHr(psbox->Segment()->get_RightToLeft(psbox->IchMin(), &fRightToLeft2));
			if (fRightToLeft != fRightToLeft2)
			{
				// Opposite direction from our box, treat its left edge as limit
				return rcSrc.MapXTo(psbox->Left(), rcDst);
			}

			// Figure the position of the any tags that will show in this box
			if (FindOverlayBoundary(psbox, fOpening, fRightToLeft, &xdLim, rcSrc, rcDst, pvg))
				return xdLim;
		}
		// If we drop out of this loop there are no more tags to draw right of our box,
		// so we can take the paragraph boundary as our limit.
		return rcSrc.MapXTo(Width(), rcDst) - GapTrailing(dxdInch);
	}
	else // !fOnRight
	{
		// Loop over boxes before ibox looking for relevant boundaries
		for (int ibox2 = ibox; --ibox2 >= 0; )
		{
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(vbox[ibox2]);
			if (!psbox)
				continue; // picture or similar, we can draw anywhere above or below it.
			// If this box has a different internal direction from the starting box,
			// it is too confusing to allow overlap.
			ComBool fRightToLeft2;
			CheckHr(psbox->Segment()->get_RightToLeft(psbox->IchMin(), &fRightToLeft2));
			if (fRightToLeft != fRightToLeft2)
			{
				// Opposite direction from our box, treat its right edge as limit
				return rcSrc.MapXTo(psbox->Right(), rcDst);
			}

			// Figure the position of the any tags that will show in this box
			if (FindOverlayBoundary(psbox, fOpening, fRightToLeft, &xdLim, rcSrc, rcDst, pvg))
				return xdLim;
		}
		// If we drop out of this loop there are no more tags to draw left of our box,
		// so we can take the paragraph boundary as our limit.
		return rcSrc.MapXTo(0, rcDst) + GapLeading(dxdInch);
	}
}

/*----------------------------------------------------------------------------------------------
	Find the left-most (if fRightToLeft is false) or rightmost (if fRightToLeft is true)
	tag boundary of the type indicated by fOpening in pbox. If there is no tag to draw within
	the box, return false.
	The conversion rectangles treat the top left of the paragraph as (0,0).
----------------------------------------------------------------------------------------------*/
bool VwParagraphBox::FindOverlayBoundary(VwStringBox * psbox, bool fOpening, bool fRightToLeft,
	int * pxd, Rect rcSrc, Rect rcDst, IVwGraphics * pvg)
{
	// Determine whether we want the first boundary or the last one.
	bool fFirst = (fOpening && !fRightToLeft) || (fRightToLeft && !fOpening);

	int ichMin = psbox->IchMin();
	int ichBoundary = ichMin; // char position of relevant boundary.
	int dichLim;
	CheckHr(psbox->Segment()->get_Lim(ichMin, &dichLim));
	// If it is an empty segment it can't show any tags.
	if (!dichLim)
		return false;
	int ichLimSeg = ichMin + dichLim;

	// Two sets of result variables for GetCharPropInfo.
	int ichMinRun;
	int ichLimRun;
	int isbt;
	int irun = 0;
	ITsTextPropsPtr qttp;
	int ichMinRun2;
	int ichLimRun2;
	int isbt2;
	int irun2;
	ITsTextPropsPtr qttp2;

	IVwOverlay * pvo = Source()->Overlay();
	VecTagInfo vti;

	if (fFirst)
	{
		// Get the properties of the first character of the segment.
		irun = 0;
		Source()->GetCharPropInfo(ichMin, &ichMinRun, &ichLimRun, &isbt, &irun, &qttp, NULL);
		if (fOpening)
		{
			// See if we draw a boundary right at the start. (We never draw a closing tag
			// there.)
			if (ichMin == 0)
			{
				// Compare props of char 0 with no tags at all.
				FindGuidDiffs(qttp, NULL, pvo, vti, NULL);
			}
			else if (ichMin == ichMinRun)
			{
				// start of segment is start of a run; see if it is a boundary by comparing
				// with props of previous run
				irun2 = 0;
				Source()->GetCharPropInfo(ichMin - 1, &ichMinRun2, &ichLimRun2, &isbt2,
					&irun2, &qttp2, NULL);
				FindGuidDiffs(qttp, qttp2, pvo, vti, NULL);
			}
			if (vti.Size())
			{
				goto LGotBoundary;
			}
		}
		// OK, ichMin is not a boundary...see if we can find one.
		while (ichLimRun < ichLimSeg)
		{
			// We have the properties of the previous run in qttp.
			// There is nothing to be drawn up to ichLimRun, but there might be there.
			ichBoundary = ichLimRun;
			// Get the properties of the run after ichBoundary;
			irun = 0;
			Source()->GetCharPropInfo(ichBoundary, &ichMinRun, &ichLimRun, &isbt, &irun,
				&qttp2, NULL);

			// Compute the relevant type of difference between the two property sets.
			if (fOpening)
				FindGuidDiffs(qttp2, qttp, pvo, vti, NULL);
			else
				FindGuidDiffs(qttp, qttp2, pvo, vti, NULL);
			qttp = qttp2; // qttp is always for the current characters.
			if (vti.Size())
				goto LGotBoundary;
		}
		// Nothing up to the end...possibly a closing tag right at the segment boundary?
		if (!fOpening)
		{
			ichBoundary = ichLimSeg;
			if (ichLimRun == ichLimSeg)
			{
				if (ichLimSeg == Source()->CchRen())
				{
					// Last character of string: see if we need closing tag
					FindGuidDiffs(qttp, NULL, pvo, vti, NULL);
				}
				else
				{
					// Ok to get next character
					irun2 = 0;
					Source()->GetCharPropInfo(ichLimSeg, &ichMinRun2, &ichLimRun2, &isbt2,
						&irun2, &qttp2, NULL);
					FindGuidDiffs(qttp, qttp2, pvo, vti, NULL);
				}
				if (vti.Size())
				{
					goto LGotBoundary;
				}
			}
		}
	}
	else // !fFirst
	{
		ichBoundary = ichLimSeg;
		// Get the properties of the last character of the segment.
		irun = 0;
		Source()->GetCharPropInfo(ichLimSeg - 1, &ichMinRun, &ichLimRun, &isbt, &irun, &qttp, NULL);
		if (!fOpening)
		{
			// See if we draw a boundary right at the end. (We never draw an opening tag there.)
			if (ichLimSeg == Source()->CchRen())
			{
				// Compare props of last char with no tags at all.
				FindGuidDiffs(qttp, NULL, pvo, vti, NULL);
			}
			else if (ichLimSeg == ichLimRun)
			{
				// end of segment is end of a run; see if it is a boundary by comparing
				// with props of following run
				irun2 = 0;
				Source()->GetCharPropInfo(ichLimSeg, &ichMinRun2, &ichLimRun2, &isbt2,
					&irun2, &qttp2, NULL);
				FindGuidDiffs(qttp, qttp2, pvo, vti, NULL);
			}
			if (vti.Size())
			{
				goto LGotBoundary;
			}
		}
		// OK, ichLimSeg is not a boundary...see if we can find one.
		while (ichMinRun > ichMin)
		{
			// We have the properties of the following run in qttp.
			// There is nothing to be drawn after ichMinRun, but there might be there.
			ichBoundary = ichMinRun;
			// Get the properties of the run before ichBoundary;
			irun = 0;
			Source()->GetCharPropInfo(ichBoundary - 1, &ichMinRun, &ichLimRun, &isbt, &irun,
				&qttp2, NULL);

			// Compute the relevant type of difference between the two property sets.
			if (fOpening)
				FindGuidDiffs(qttp, qttp2, pvo, vti, NULL);
			else
				FindGuidDiffs(qttp2, qttp, pvo, vti, NULL);
			qttp = qttp2; // qttp is always for the current characters.
			if (vti.Size())
				goto LGotBoundary;
		}
		// Nothing before the start...possibly an opening tag right at the segment boundary?
		if (!fOpening)
		{
			ichBoundary = ichMin;
			if (ichMin == ichMinRun)
			{
				if (ichMin == 0)
				{
					// First character of string: see if we need opening tag
					FindGuidDiffs(qttp, NULL, pvo, vti, NULL);
				}
				else
				{
					// Ok to get previous character
					irun2 = 0;
					Source()->GetCharPropInfo(ichMin - 1, &ichMinRun2, &ichLimRun2, &isbt2,
						&irun2, &qttp2, NULL);
					FindGuidDiffs(qttp, qttp2, pvo, vti, NULL);
				}
				if (vti.Size())
				{
					goto LGotBoundary;
				}
			}
		}
	}
	// If we get here we didn't find a boundary at all.
	return false;

LGotBoundary:
	// If we get here the limit is at ichBoundary. Now figure where that is in xd coordinates.
	// If it is somewhere inside the segment we use underlining info, if available.
	if (ichBoundary > ichMin && ichBoundary < ichLimSeg)
	{
		Rect rcSrcString(rcSrc);
		rcSrcString.Offset(-psbox->Left(), -psbox->Top());
		int rgxdLefts[10]; // 10 is ridiculously generous for underlining one character!
		int rgxdRights[10];
		int rgydTops[10];
		int cxd;
		HRESULT hr;
		IgnoreHr(hr = psbox->Segment()->GetCharPlacement(ichMin, pvg, ichBoundary,
			ichBoundary + 1, rcSrcString, rcDst, false, // don't skip white space
			10, &cxd, rgxdLefts, rgxdRights, rgydTops));
		if (SUCCEEDED(hr) && cxd)
		{
			// Use the first underline segment
			*pxd = fRightToLeft ? rgxdRights[0] : rgxdLefts[0];
			return true;
		}
		// couldn't get underline info, keep the tag clear of the box
		ichBoundary = fRightToLeft ? ichLimSeg : ichMin;
	}
	// By now it's at one of the boundaries.
	if (ichBoundary == ichMin)
	{
		*pxd = rcSrc.MapXTo((fRightToLeft ? psbox->Right() : psbox->Left()), rcDst);
	}
	else
	{
		*pxd = rcSrc.MapXTo((fRightToLeft ? psbox->Left() : psbox->Right()), rcDst);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Determine whether or not the specified point (xd, yd) corresponds to an open or close
	overlay tag (the abbreviation drawn above or below the text when overlays are showing).
	The function returns true if it found an overlay. In this case, the following parameters
	will contain information about the overlay tag(s):
		piGuid - This will be set to the index of the actual tag corresponding to the point.
				This is used when several tags are opened/closed at one position and are
				grouped together.
		pbstrGuids - This will be set to a string containg the GUIDs of all the overlays
				in the group. Each GUID is made up of kcchGuidRepLength (8) characters.
		prc - This will be set to the rectangle containing the specified tag.
		prcAllTags - The rectangle containing the entire list of tags.
		pfOpeningTag - True if the tag is above the line and false if below the line.

	TODO: Should this be enhanced to return any of the following:
		1) The rectangle corresponding to the entire group of tags.
		2) A selection (IVwSelection) containing the text that is tagged with the
			specified tag.
		3) Whether or not the tag is an open tag or a close tag.
----------------------------------------------------------------------------------------------*/
bool VwParagraphBox::FindOverlayTagAt(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int xd,
	int yd, int * piGuid, BSTR * pbstrGuids, RECT * prc, RECT * prcAllTags,
	ComBool * pfOpeningTag)
{
	AssertPtr(piGuid);
	AssertPtr(pbstrGuids);
	AssertPtr(prc);
	AssertPtr(prcAllTags);
	AssertPtr(pfOpeningTag);
	*piGuid = 0;
	*pbstrGuids = NULL;
	memset(prc, 0, isizeof(RECT));
	memset(prcAllTags, 0, isizeof(RECT));
	*pfOpeningTag = false;

	IVwOverlay * pvo = Source()->Overlay();
	if (!pvo)
		return false;

	VwOverlayFlags vof;
	CheckHr(pvo->get_Flags(&vof));
	Rect rcSrcBox;
	Rect rcDstBox;
	VwBox * pboxClick = FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, &rcSrcBox, &rcDstBox);
	if (!pboxClick)
		return false;
	VwStringBox * psbox = dynamic_cast<VwStringBox *>(pboxClick);
	if (!psbox)
		return false;

	BoxVec vbox;
	int ysTop;
	int ysBottom;
	GetALineTB(psbox, vbox, rcSrc.Height(), &ysTop, &ysBottom);

	FobData fobd(vbox, 0, rcSrc, rcDst);
	TagSelectInfo tsi;
	tsi.xd = xd - psbox->Left() - psbox->GapLeft(rcSrc.Width());
	// If the commented out code is added back, adjust the ::OffsetRect call below.
	tsi.yd = yd; //  - psbox->Top() - psbox->GapTop(rcSrc.Height());
	tsi.fOpening = false;
	tsi.fTagClicked = false;
	tsi.ich = 0;
	tsi.iguid = 0;

	DrawOverlayTags(pvg, rcSrc, rcDst, psbox, vof, ysTop, ysBottom, &tsi, fobd);

	if (tsi.fTagClicked)
	{
		Assert(tsi.vguid.Size() > 0);
		SmartBstr sbstr;
		sbstr.Assign((OLECHAR *)(tsi.vguid.Begin()), tsi.vguid.Size() * kcchGuidRepLength);
		*pbstrGuids = sbstr.Detach();
		*piGuid = tsi.iguid;
		::SetRect(prc, tsi.rc.left, tsi.rc.top, tsi.rc.right, tsi.rc.bottom);
		::OffsetRect(prc, psbox->Left() + psbox->GapLeft(rcSrc.Width()), 0);
		::SetRect(prcAllTags, tsi.rcAllTags.left, tsi.rcAllTags.top,
			tsi.rcAllTags.right, tsi.rcAllTags.bottom);
		::OffsetRect(prcAllTags, psbox->Left() + psbox->GapLeft(rcSrc.Width()), 0);
		*pfOpeningTag = tsi.fOpening;

		//::OffsetRect(prc, psbox->Left() + psbox->GapLeft(rcSrc.Width()),
		//	psbox->Top() + psbox->GapTop(rcSrc.Height()));
	}

	return tsi.fTagClicked;
}


/*----------------------------------------------------------------------------------------------
	Draw the overlay tags for a string box.
	ydTop and ydBottom are the top and bottom of the string boxes on the line. (Draw opening
	tags just above ydTop and closing just below ydBottom.)

	Special case: if ptsi is non-null, we draw nothing. Instead, we test whether the point
	in ptsi is within some tag we are displaying, and if so, fill in the rest of the struct
	to indicate which tag was clicked.
	Char indexes are in rendered chars.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::DrawOverlayTags(IVwGraphics * pvg, Rect rcSrc, Rect rcDst,
	VwStringBox * psbox, VwOverlayFlags vof, int ydTop, int ydBottom, TagSelectInfo * ptsi,
	FobData & fobd)
{
	bool fAbove = vof & kgrfofTagAbove;
	bool fBelow = vof & kgrfofTagBelow;
	// We should not have been called if neither is wanted.
	if (!fAbove && !fBelow)
		return;

	ILgSegment * pseg = psbox->Segment();
	int ichMin = psbox->IchMin();
	int dichLim;
	CheckHr(pseg->get_Lim(psbox->IchMin(), &dichLim));
	int ichLimSeg = ichMin + dichLim;

	// All variables in this routine are rendered char indexes, not logical ones.
	int ichMinRun;
	int ichLimRun;
	int isbt;
	int irun = 0;
	ITsTextPropsPtr qttp;
	int ichMinRun2;
	int ichLimRun2;
	int isbt2;
	int irun2;
	ITsTextPropsPtr qttp2;
	IVwOverlay * pvo = dynamic_cast<VwParagraphBox *>(psbox->Container())->Source()->Overlay();

	// This vector stores info that we need to display about the beginnings of tags. When
	// we reach the end of the segment or another place where we need to display opening
	// info, that gives us the available space, and we then display it, clear it, and
	// use it for the next.
	VecTagInfo vtiPending;
	// This character position indicates where we last drew close-of-tag info, for the
	// purpose of knowing how much space we have available for drawing the next.
	// Initially, we have available everything from the start of the segment.
	int ichLastClose = ichMin;
	// This indicates the character position where the pending tags need to be drawn.
	int ichPending = ichMin;

	// We are interested in boundaries between tagging. If this segment starts at 0, we begin by
	// marking a start label of all unhidden tags for the first character. Otherwise, we
	// initialize with the tagging properties of the character before the first of interest, and
	// ichMin get tags only if different from the previous character.
	// This code sets qttp to the properties of the first character, and vtiPending to the
	// opening tags, if any, that need to be drawn at the very start of the segment.
	if (ichMin == 0)
	{
		// We need this info anyway as it may be used to compute end tags for the
		// next run.
		Source()->GetCharPropInfo(0, &ichMinRun, &ichLimRun, &isbt, &irun, &qttp, NULL);
		if (fAbove)
		{
			// The info we really want from this call is the qttp for the char offset.
			// Maybe someday make a more efficient routine that just gets that?
			irun = 0;
			FindGuidDiffs(qttp, NULL, pvo, vtiPending, ptsi);
		}
	}
	else
	{
		irun = 0;
		Source()->GetCharPropInfo(ichMin, &ichMinRun, &ichLimRun, &isbt, &irun, &qttp, NULL);
		if (ichMinRun == ichMin && fAbove)
		{
			// It is possible that we need opening tag info at the start of the segment.
			// Get the props of the previous character and see.
			irun2 = 0;
			Source()->GetCharPropInfo(ichMin - 1, &ichMinRun2, &ichLimRun2, &isbt2,
				&irun2, &qttp2, NULL);
			FindGuidDiffs(qttp, qttp2, pvo, vtiPending, ptsi);
		}
	}

	// Now, loop while we have more runs...
	while (ichLimRun < ichLimSeg)
	{
		// Get info about next run.
		irun = 0;
		Source()->GetCharPropInfo(ichLimRun, &ichMinRun, &ichLimRun, &isbt, &irun, &qttp2, NULL);

		// Figure the opening and closing tags we need to show at this boundary, if any.
		// Make sure these are inside the loop so they get cleared each time.
		VecTagInfo vtiOpening;
		VecTagInfo vtiClosing;
		if (fAbove)
			FindGuidDiffs(qttp2, qttp, pvo, vtiOpening, ptsi);
		if (fBelow)
			FindGuidDiffs(qttp, qttp2, pvo, vtiClosing, ptsi);
		qttp = qttp2; // qttp is always for the current characters.
		// If we need to display opening tags here, for now display the previous set.
		// We don't yet know how much space we have to display the new ones.
		if (vtiOpening.Size())
		{
			DrawTags(pvg, true, vof, vtiPending, ichPending, ichMinRun, rcSrc, rcDst, psbox,
				ydTop, ydBottom, ptsi, fobd);
			vtiPending = vtiOpening;
			ichPending = ichMinRun;
		}
		if (vtiClosing.Size())
		{
			DrawTags(pvg, false, vof, vtiClosing, ichLastClose, ichMinRun, rcSrc, rcDst, psbox,
				ydTop, ydBottom, ptsi, fobd);
			ichLastClose = ichMinRun;
		}
	}
	// After the last run do any pending open
	if (fAbove)
		DrawTags(pvg, true, vof, vtiPending, ichPending, ichLimSeg, rcSrc, rcDst, psbox,
		ydTop, ydBottom, ptsi, fobd);
	// Figure any closing tag for the end of the segment. If there is nothing more in the
	// string, close any remaining. If there are more characters, test the next.
	if (fBelow)
	{
		if (ichLimSeg < Source()->CchRen())
		{
			irun = 0;
			Source()->GetCharPropInfo(ichLimSeg, &ichMinRun, &ichLimRun, &isbt, &irun, &qttp2, NULL);
		}
		else
			qttp2.Clear();
		VecTagInfo vtiFinalClosing;
		FindGuidDiffs(qttp, qttp2, pvo, vtiFinalClosing, ptsi);
		if (vtiFinalClosing.Size())
		{
			DrawTags(pvg, false, vof, vtiFinalClosing, ichLastClose, Source()->CchRen(), rcSrc,
				rcDst, psbox, ydTop, ydBottom, ptsi, fobd);
		}
	}
}

const int kcxdMax = 1000; // Don't bother with more than 1000 underline line segments
/*----------------------------------------------------------------------------------------------
	Draw the opening (fOpening true) or closing (fOpening false) tags and brackets
	(if requested by user, as specified by vof). The actual tag info is in vti,
	and it should be drawn in the space betwee ichMin and ichLim, which are relative to
	your own Source(). rcSrc and rcDst signify where to draw in the usual way (they are
	adjusted for the exact place where psbox's segment is to be drawn). The overlays
	are relative to psbox, and it can be assumed that ichMin and ichLim are within the
	range for that segment.

	The basic idea is to use the underlining positions code to determine where we can draw.
	We ask for underlining info for the specified range of characters. The segments returned
	are supposed to be in logical order. We ask the segment its overall direction. We then
	concatenate any underline segments that are contiguous horizontally at the appropriate
	end of the range, and use that space.

	ydTopText and ydBottomText give the top and bottom of the line of text. Opening tags go
	above ydTopText, closing below ydBottomText.

	Special case: if ptsi is non-null, we draw nothing. Instead, we test whether the
	point in *ptsi is within one of the tags, or the ... where we truncated. The fTagClicked
	field is set if so, and the other fields filled in to indicate what was clicked.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::DrawTags(IVwGraphics * pvg, bool fOpening, VwOverlayFlags vof,
	VecTagInfo vti, int ichMin, int ichLim, Rect rcSrc, Rect rcDst, VwStringBox * psbox,
	int ydTopText, int ydBottomText, TagSelectInfo * ptsi, FobData & fobd)
{
	// If there are no differences bewteen the two runs, do nothing.
	if (vti.Size() == 0)
		return;

	if (m_fSemiTagging)
		return;

	IVwOverlay * pvo = Source()->Overlay();
	ComBool fRightToLeft;
	CheckHr(psbox->Segment()->get_RightToLeft(psbox->IchMin(), &fRightToLeft));

	int cxd; // #line segs
	int rgxdLefts[kcxdMax];
	int rgxdRights[kcxdMax];
	int rgydTops[kcxdMax];
	HRESULT hr = psbox->Segment()->GetCharPlacement(psbox->IchMin(), pvg, ichMin, ichLim,
		rcSrc, rcDst, false, // don't skip white space
		kcxdMax, &cxd, rgxdLefts, rgxdRights,
		rgydTops);
	if (!cxd && ichMin == 0 && ichLim == 0)
	{
		// No characters, but we can guess based on where the insertion point would go in the
		// empty string.
		RECT rectIP, rectSec;
		ComBool fPrimHere, fSecHere;
		IgnoreHr(hr = psbox->Segment()->PositionsOfIP(psbox->IchMin(), pvg,
			rcSrc, rcDst, 0, false, kdmSplitPrimary, &rectIP, &rectSec, &fPrimHere, &fSecHere));
		if (hr == S_OK && fPrimHere)
		{
			rgxdLefts[0] = rectIP.left;
			rgxdRights[0] = rectIP.left + 1;
			cxd = 1;
		}
	}
	if (FAILED(hr) || !cxd)
	{
		// Recover from this--maybe an experimental rendering engine, or the text is
		// somehow too complex to figure char placements?
		Warn("Could not draw tags for lack of char placement info");
		return;
	}

	// Decide whether to draw on the right or on the left. For left-to-right text, we draw
	// opening on the left, closing on the right; for right-to-left, reverse these.
	bool fDrawRight = !fRightToLeft; // draw closing on the right unless right-to-left
	if (fOpening)
		fDrawRight = (bool)fRightToLeft; // draw opening on the left unless right-to-left

	// Get a first approximation to available space based on underlining information for
	// the specified character range. If this space is not enough, we consider other options
	// later.
	int xdLeft;
	int xdRight;
	if (fOpening)
	{
		// Use the logically first line segment, and any contiguous ones.
		xdLeft = rgxdLefts[0];
		xdRight = rgxdRights[0];
		// If there are more segment and they are physically contiguous in the X direction
		// (maybe not in y), use them too.
		for (int ixd = 1; ixd < cxd; ixd++)
		{
			if (fRightToLeft)
			{
				// subsequent segments are OK if to the left of the current one
				if (xdLeft != rgxdRights[ixd])
					break;
				xdLeft = rgxdLefts[ixd];
			}
			else
			{
				// subsequent segments are OK if to the right
				if (xdRight != rgxdLefts[ixd])
					break;
				xdRight = rgxdRights[ixd];
			}
		}
	}
	else
	{
		// Use the logically last line segment, and any contiguous ones.
		xdLeft = rgxdLefts[cxd - 1];
		xdRight = rgxdRights[cxd - 1];
		// If there are more segment and they are physically contiguous in the X direction
		// (maybe not in y), use them too. (Note that we start this loop at cxd-2, the
		// next-to-last segment.)
		for (int ixd = cxd - 1; --ixd >= 0; )
		{
			if (fRightToLeft)
			{
				// previous segments are OK if to the right of the current one
				if (xdRight != rgxdLefts[ixd])
					break;
				xdRight = rgxdRights[ixd];
			}
			else
			{
				// previous segments are OK if to the left
				if (xdLeft != rgxdRights[ixd])
					break;
				xdLeft = rgxdLefts[ixd];
			}
		}
	}

	// OK, we  have available space from xdLeft to xdRight for drawing tags (and the bracket,
	// if requested). Figure what text will fit, and truncate if necessary.
	int dxdAvail = xdRight - xdLeft;
	if (vof & (fOpening ? kfofLeadBracket : kfofTrailBracket))
	{
		// Leave what on screen at normal magnification will be two pixels of space (2/96 inch).
		// That is one pixel for the line, one for a gap.
		dxdAvail -= MulDiv(2, rcDst.Width(), 96);
	}
	// Create a chrp that describes how we will draw. Some of these values may get overwritten
	// if kfofTagsUseAttribs is on.
	LgCharRenderProps chrp;
	chrp.clrFore = kclrBlack;
	chrp.clrBack = (COLORREF) kclrTransparent;
	chrp.dympOffset = 0;
	// Ignore ws, and other fields that don't affect actual font selection & drawing
	chrp.ttvBold = kttvOff;
	chrp.ttvItalic = kttvOff;
	CheckHr(pvo->get_FontSize(&chrp.dympHeight));
	CheckHr(pvo->FontNameRgch(chrp.szFaceName));

	int dxdTotalWidth = 0; // width of tags computed so far
	int dxdSpacer; // width of comma between tags (compute if > 1)
	int dxdMissing; // width of "..." (or as many periods as will fit)
	int dydHeight;
	static OleStringLiteral rgchMissing(L"...");
	int cchMissing = 0; // changed to as many as will fit if missing string needed.
	// We can use this for all measurements; color should not affect them.
	CheckHr(pvg->SetupGraphics(&chrp));
	// Review JohnT: should we use a single Unicode character that represents "..."?
	// OPTIMIZE Johnt: could we avoid ever computing this if everything fits?
	CheckHr(pvg->GetTextExtent(3, rgchMissing, &dxdMissing, &dydHeight));
	if (vti.Size() > 1)
	{
		OLECHAR comma = ',';
		CheckHr(pvg->GetTextExtent(1, &comma, &dxdSpacer, &dydHeight));
	}
	int ctagsDisp;
	CheckHr(pvo->get_MaxShowTags(&ctagsDisp));
	ctagsDisp = std::min(ctagsDisp, vti.Size()); // Number we will display...assume all
	int cchFirst = vti[0].stuAbbr.Length(); // number of chars we can display of first label
	int iti;
	bool fExtended = false; // Set true if we have tried to extend the available space already.
	for (iti = 0; iti < vti.Size(); iti++)
	{
		int dxdWidth;
		CheckHr(pvg->GetTextExtent(vti[iti].stuAbbr.Length(), vti[iti].stuAbbr.Chars(),
			&dxdWidth, &dydHeight));
		// We will add this tag to the list provided
		//  (1) it is the last tag and it fits; or
		//  (2) it fits, followed by ...
		int dxdRequired;
		if (iti == vti.Size() - 1)
		{
			dxdRequired = dxdTotalWidth + dxdWidth;
		}
		else
		{
			// Make sure we can fit the "..." if the next tag does not fit.
			dxdRequired = dxdTotalWidth + dxdWidth + dxdMissing;
		}
		if (iti)
			dxdRequired += dxdSpacer; // between previous and this
		if (dxdRequired > dxdAvail && !fExtended)
		{
			fExtended = true;
			// Since we now know we are short of space, investigate possibilities for
			// more space.
			if (fOpening)
			{
				// We want more space after the segment. Only possible if our limit is at the
				// end of it.
				int dichLim;
				CheckHr(psbox->Segment()->get_Lim(psbox->IchMin(), &dichLim));
				if (ichLim < psbox->IchMin() + dichLim)
					goto LFixedSpace;
			}
			else // !fOpening
			{
				// Want more before it. Only possible if our min is the start.
				if (ichMin > psbox->IchMin())
					goto LFixedSpace;
			}
			// Original limit was the segment boundary. We may be able to do better.
			// Search the array of boxes on this line to see what is after us.
			int xdLim;
			xdLim = VwParagraphBox::FindOverlayBoundary(fobd.vboxLine, fobd.ibox,
				fOpening, fRightToLeft, fobd.rcSrcPara, fobd.rcDst, pvg);
			if ((fOpening && !fRightToLeft) || (fRightToLeft && ! fOpening))
			{
				// New limit is on the right
				dxdAvail += xdLim - xdRight;
				xdRight = xdLim;
			}
			else
			{
				dxdAvail += xdLeft - xdLim;
				xdLeft = xdLim;
			}
			// Various text measurements done during FindOverlayBoundary may have put
			// the graphics object in a different state. Restore it.
			CheckHr(pvg->SetupGraphics(&chrp));
		}
LFixedSpace:
		if (dxdRequired > dxdAvail)
		{
			// That didn't help...truncate somehow.
			ctagsDisp = iti;
			// We won't display this tag...unless it is the first, then we might display some.
			cchMissing = 3; // Draw all 3 dots...unless it is the first, then may need less
			if (iti == 0)
			{
				// Not even the first string fits, at least not with following ...
				if (dxdMissing > dxdAvail)
				{
					// We can't even fit ...! Try fewer dots...
					// Note: it is pathologically possible that dxdAvail is actually negative
					// (e.g., the thing tagged is a diacritic of zero width, and we have
					// subtracted two for the bracket), so we could get down to zero chars
					// without dxdMissing falling to equal dxdAvail.
					for (; cchMissing > 0 && dxdMissing > dxdAvail; )
					{
						--cchMissing;
						CheckHr(pvg->GetTextExtent(cchMissing, rgchMissing, &dxdMissing,
							&dydHeight));
					}
					// No chars of even the first string, so no width for it.
					cchFirst = 0;
					dxdWidth = 0;
					dxdTotalWidth = dxdMissing;
				}
				else
				{
					// The missing string fits. See how much of tag 1 will also fit. We know
					// the whole of it won't fit, so start by trying one fewer characters.
					for (; dxdWidth + dxdMissing > dxdAvail; )
					{
						Assert (cchFirst > 0);
						--cchFirst;
						CheckHr(pvg->GetTextExtent(cchFirst, vti[0].stuAbbr.Chars(), &dxdWidth,
							&dydHeight));
					}
					if (cchFirst)
						ctagsDisp = 1; // otherwise leave it 0
				}
				dxdTotalWidth = dxdMissing + dxdWidth;
			}
			else
			{
				// We already have at least one tag, and we checked when we added it that we
				// had room for the missing dots. Just make sure they're included
				dxdTotalWidth += dxdMissing;
			}
			break;
		} // if it didn't fit...
		dxdTotalWidth += dxdWidth;
		if (iti != 0)
			dxdTotalWidth += dxdSpacer;
	}
	// OK, dxdTotalWidth is the space required to draw the tag list (truncated as necessary...as
	// indicated by ctagsDisp, cchFirst, and cchMissing. xdPos is the left of where we will
	// draw. xdPos + dxdTotalWidth is therefore the right boundary.
	int xdPos = xdLeft;
	if (fDrawRight)
	{
		xdPos = xdRight - dxdTotalWidth;
		if (vof & (fOpening ? kfofLeadBracket : kfofTrailBracket))
		{
			// Leave what on screen at normal magnification will be two pixels of space (2/96
			// inch).   That is one pixel for the line, one for a gap.
			xdPos -= MulDiv(2, rcDst.Width(), 96);
		}
	}
	int ydTopTag; // top  of tag text

	int dydAscent;
	CheckHr(pvg->get_FontAscent(&dydAscent));
	// Figure the minimum descent to allow for (possibly double) underlines.
	// We have dydHeight from any number of measurements above.
	int dydMinDescent = 4 * rcDst.Height() / 96;
	if (dydHeight < dydMinDescent + dydAscent)
		dydHeight = dydMinDescent + dydAscent;
	if (fOpening)
	{
		// Draw above the segment.
		ydTopTag = ydTopText - dydHeight;
		if (ptsi)
		{
			// We now know enough to stop altogether if the point is out of the tag rectangle.
			/*if (ptsi->yd < ydTopTag || ptsi->yd > ydTopTag + dydHeight ||
				ptsi->xd < xdPos || ptsi->xd > xdPos + dxdTotalWidth)
			{
				return;
			}*/
			int xdT = rcDst.MapXTo(ptsi->xd, rcSrc) - Left();
			int ydT = rcDst.MapYTo(ptsi->yd, rcSrc) - Top();
			if (ydT < ydTopTag || ydT > ydTopTag + dydHeight ||
				xdT < xdPos || xdT > xdPos + dxdTotalWidth)
			{
				return;
			}
		}
		else
		{
			if (vof & kfofLeadBracket)
			{
				int dxdThick = std::max(rcDst.Width() / 96, 1);
				int dydThick = std::max(rcDst.Height() / 96, 1);
				// Vertical line aligned with xdPos, from two pixels above tag to bottom of tag,
				// plus a horizontal line three pixels long (not counting the vertical).
				// It is most precise to draw it with a rectangle, however.
				COLORREF clrLine = kclrBlack;
				// ENHANCE JohnT: if we want the bracket to match the tag we can reinstate this.
				//if (ctagsDisp == 1 && (vof & kfofTagsUseAttribs))
				//	clrLine = vti[0].clrFore;
				CheckHr(pvg->put_BackColor(clrLine));
				if (fDrawRight)
				{
					CheckHr(pvg->DrawRectangle(xdRight - dxdThick, ydTopTag - dydThick * 2,
						xdRight, ydTopTag + dydHeight));
					CheckHr(pvg->DrawRectangle(xdRight - dxdThick * 4, ydTopTag - dydThick * 2,
						xdRight - dxdThick, ydTopTag - dydThick));
					xdRight -= dxdThick * 2;
				}
				else
				{
					CheckHr(pvg->DrawRectangle(xdPos, ydTopTag - dydThick * 2,
						xdPos + dxdThick, ydTopTag + dydHeight));
					CheckHr(pvg->DrawRectangle(xdPos + dxdThick, ydTopTag - dydThick * 2,
						xdPos + dxdThick * 4, ydTopTag - dydThick));
					xdPos += dxdThick * 2;
				}
			}
		}
	}
	else
	{
		// Draw below the segment.
		ydTopTag = ydBottomText;
		if (ptsi)
		{
			int xdT = rcDst.MapXTo(ptsi->xd, rcSrc) - Left();
			int ydT = rcDst.MapYTo(ptsi->yd, rcSrc) - Top();
			// We now know enough to stop altogether if the point is out of the tag rectangle.
			if (ydT < ydTopTag || ydT > ydTopTag + dydHeight ||
				xdT < xdPos || xdT > xdPos + dxdTotalWidth)
			{
				return;
			}
		}
		else
		{
			// normal drawing.
			if (vof & kfofTrailBracket)
			{
				int dxdThick = std::max(rcDst.Width() / 96, 1);
				int dydThick = std::max(rcDst.Height() / 96, 1);
				// Vertical line aligned with xdPos, from baseline of upper tag to bottom of
				// bottom tag.  It is most precise to draw it with a rectangle, however.
				COLORREF clrLine = kclrBlack;
				//if (ctagsDisp == 1 && (vof & kfofTagsUseAttribs))
				//	clrLine = vti[0].clrFore;
				CheckHr(pvg->put_BackColor(clrLine));
				int ydTopBracket = ydTopTag;
				int ydBottomBracket = ydTopTag + dydHeight;
				if (fDrawRight)
				{
					CheckHr(pvg->DrawRectangle(xdRight - dxdThick, ydTopBracket,
						xdRight, ydBottomBracket));
					CheckHr(pvg->DrawRectangle(xdRight - dxdThick * 4,
						ydBottomBracket - dydThick, xdRight - dxdThick, ydBottomBracket));
					xdRight -= dxdThick * 2;
				}
				else
				{
					CheckHr(pvg->DrawRectangle(xdPos, ydTopBracket, xdPos + dxdThick,
						ydBottomBracket));
					CheckHr(pvg->DrawRectangle(xdPos + dxdThick, ydBottomBracket - dydThick,
						xdPos + dxdThick * 4, ydBottomBracket));
					xdPos += dxdThick * 2;
				}
			}
		}
	}
	// All that is left is to draw the tags themselves--if requested.
	if (fOpening && !(vof & kfofLeadTag))
		return;
	if ((!fOpening) && !(vof & kfofTrailTag))
		return;
	// a pixel below baseline, typically
	int ydUnder = ydTopTag + dydAscent + rcDst.Height() / 96;
	if (ptsi && ptsi->xd < xdPos)
	{
		// Point is not in the tag area. We have nothing to do.
		return;
	}
	for (iti = 0; iti < ctagsDisp; iti++)
	{
		if (iti)
		{
			// Use props of previous string. Not done on first iteration!
			if (!ptsi)
			{
				// We only want to draw the separator if we draw the text.
				OLECHAR comma = ',';
				CheckHr(pvg->DrawText(xdPos, ydTopTag, 1, &comma, 0));
			}
			xdPos += dxdSpacer;
		}

		if (vof & kfofTagsUseAttribs)
		{
			chrp.clrFore = vti[iti].clrFore;
			chrp.clrBack = vti[iti].clrBack;
			CheckHr(pvg->SetupGraphics(&chrp));
		}
		int dxdWidth;
		int cch = iti ? vti[iti].stuAbbr.Length() : cchFirst;
		CheckHr(pvg->GetTextExtent(cch, vti[iti].stuAbbr.Chars(), &dxdWidth, &dydHeight));
		if (ptsi)
		{
			// OLD COMMENT:
			//   Just test whether the point is in range. We would have stopped earlier if
			//   it is less than xdPos
			// We can't do this Assert here because of the spacer (comma) between two adjacent
			// tags. If the cursor happens to be in between two tags, this Assert was failing.
			// Instead of the Assert, we need to add it to the 'if' condition.
			//Assert(ptsi->xd >= xdPos);
			//if (ptsi->xd <= xdPos + dxdWidth)
			// If we don't have enough room to show all the tags, we still want to say
			// we're over a tag if we're over the ... at the end. In this case, iguid
			// will be set to -1.
			bool fInTag = (ptsi->xd >= xdPos && ptsi->xd <= xdPos + dxdWidth);
			if (!fInTag && iti < ctagsDisp - 1)
			{
				// If we're in between tags, count the comma as belonging to the previous tag.
				fInTag = ptsi->xd <= xdPos + dxdWidth + dxdSpacer;
			}
			if (fInTag || (cchMissing > 0 && iti == ctagsDisp - 1))
			{
				// The point is in this tag! Done.
				ptsi->fOpening = fOpening;
				ptsi->fTagClicked = true;
				ptsi->ich = fOpening ? ichMin : ichLim;
				// This is a temporary fix to get the correct tags that are in this group.
				// The previous vmi member variable contained all open and close tags for
				// the entire line (or paragraph, I'm not sure), and iti did not necessarily
				// have any correlation to that vector, so this way we at least get the GUIDs.
				// From the GUIDs, we can get all the information we need later.
				ptsi->vguid.Resize(vti.Size());
				for (int iguid = vti.Size(); --iguid >= 0; )
				{
					memcpy(&(ptsi->vguid[iguid]), &(vti[iguid].uid),
						kcchGuidRepLength * isizeof(OLECHAR));
				}
				if (fInTag)
				{
					ptsi->iguid = iti;
					::SetRect(&ptsi->rc, xdPos, ydTopTag, xdPos + dxdWidth, ydTopTag + dydHeight);
				}
				else
				{
					ptsi->iguid = -1;
					::SetRect(&ptsi->rc, xdPos + dxdWidth, ydTopTag, xdPos + dxdWidth,
						ydTopTag + dydHeight);
					if (cchMissing)
					{
						// Only add the missing width if there are characters that are missing>
						ptsi->rc.right += dxdMissing;
				}
				}
				int x = rcSrc.MapXTo(ptsi->rc.left, rcDst);
				int y = rcSrc.MapYTo(ptsi->rc.top, rcDst);
				::OffsetRect(&ptsi->rc, x - ptsi->rc.left + Left(), y - ptsi->rc.top + Top());

				// Get the rectangle containing all the tags.
				if (fDrawRight)
				{
					int xdT = xdRight;
					if (vof & (fOpening ? kfofLeadBracket : kfofTrailBracket))
					{
						// Leave what on screen at normal magnification will be two pixels of space (2/96
						// inch).   That is one pixel for the line, one for a gap.
						xdT -= MulDiv(2, rcDst.Width(), 96);
					}
					::SetRect(&ptsi->rcAllTags, xdT - dxdTotalWidth, ydTopTag, xdT,
						ydTopTag + dydHeight);
				}
				else
				{
					::SetRect(&ptsi->rcAllTags, xdLeft, ydTopTag, xdLeft + dxdTotalWidth,
						ydTopTag + dydHeight);
				}
				x = rcSrc.MapXTo(ptsi->rcAllTags.left, rcDst);
				y = rcSrc.MapYTo(ptsi->rcAllTags.top, rcDst);
				::OffsetRect(&ptsi->rcAllTags, x - ptsi->rcAllTags.left + Left(),
					y - ptsi->rcAllTags.top + Top());
				return;
			}
		}
		else
		{
			// Normal: draw text and possibly underline.
			if (cch)
				CheckHr(pvg->DrawText(xdPos, ydTopTag, cch, vti[iti].stuAbbr.Chars(), 0));
			if ((vof & kfofTagsUseAttribs) && vti[iti].unt != kuntNone)
			{
				DrawUnderline(pvg, xdPos, xdPos + dxdWidth, ydUnder, rcDst.Width() / 96,
					rcDst.Height() / 96, vti[iti].clrUnder, vti[iti].unt, rcDst.left);
			}
		}
		xdPos += dxdWidth;
	}
	// Draw missing string...use props of previous real string if applicable.
	if (cchMissing && !ptsi)
	{
		// We don't want to draw the ... if we're just measuring.
		CheckHr(pvg->DrawText(xdPos, ydTopTag, cchMissing, rgchMissing, 0));
	}
}

// trivial class until we get a real one. Arbitrarily considers all words of length 8 to be problems.
//class Dict
//{
//public:
//	bool check (const std::string & utf8word)
//	{
//		return utf8word.length() != 8;
//	}
//};

class SpellCheckMethod
{
	VwParagraphBox * m_pvpbox;
	int m_ich; // position in main text source
	int m_cch; // total count of characters in source.
	VwTxtSrc * m_psrc; // original source (from para box)
	StrUni m_text; // current text to check (contents of one entire text source)
	int m_ichLimRun; // limit of characters with same text props.
	VwSpellingOverrideTxtSrcPtr m_qsotsOverride;
	PropOverrideVec m_vdp;
	ITsTextPropsPtr m_qttpSquiggle;
	StrUni m_stuDictId; // last ID requested.
	ICheckWordPtr m_qcw; // last dict obtained.
	ILgWritingSystemFactoryPtr m_qwsf;
	int m_ws; // ws to which m_qcpe applies.
	ILgCharacterPropertyEnginePtr m_qcpe; // valid for chars from m_ich to m_ichLimRun
public:
	SpellCheckMethod(VwParagraphBox * pvpbox)
	{
		m_pvpbox = pvpbox;
		m_psrc = m_pvpbox->Source();
		m_qsotsOverride = dynamic_cast<VwSpellingOverrideTxtSrc *>(m_psrc);
		// If we already have an override text source, we want to go inside it.
		// This prevents us from seeing spurious text runs from a previous spell check.
		if (m_qsotsOverride)
			m_psrc = m_qsotsOverride->EmbeddedSrc();
		m_ich = 0;
		m_cch = m_psrc->CchRen();
		m_pvpbox->GetWritingSystemFactory(&m_qwsf);
	}

	~SpellCheckMethod()
	{
	}

	void EnsureRightCpe()
	{
		if (m_ich < m_ichLimRun)
			return;
		LgCharRenderProps chrp;
		int ichMin; // dummy for return from GetCharProps.
		CheckHr(m_psrc->GetCharProps(m_ich, &chrp, &ichMin, &m_ichLimRun));
		if (chrp.ws == m_ws)
			return; // new run, but same WS.
		m_ws = chrp.ws;
		ILgWritingSystemPtr qws;
		CheckHr(m_qwsf->get_CharPropEngine(m_ws, &m_qcpe));
	}


	// Check one run, in the sense of one complete text source.
	void CheckRun()
	{
		int ichMinWord = 0;
		m_ichLimRun = m_ws = 0; // forces immediate retrieve char props.
		bool fInWord = false;
		for (m_ich = 0; m_ich < m_cch; m_ich++)
		{
			EnsureRightCpe();
			ComBool isWordForming;
			OLECHAR ch = m_text[m_ich];
			CheckHr(m_qcpe->get_IsWordForming(ch, &isWordForming));
			// For consistency with double-click, and so we can detect embedded verse numbers, we
			// also consider numeric characters word-forming here. Often they are eliminated
			// because a style marks them as do-not-check. We also include the special character
			// that gets inserted before footnote callers, and ORC itself...this appears in the
			// sequence for embedded pictures, but the renderer does something different.
			if (!isWordForming)
			{
				if (ch == L'\xFEFF' || ch == L'\xFFFC')
					isWordForming = true;
				else
					CheckHr(m_qcpe->get_IsNumber(ch, &isWordForming));
			}
			if (isWordForming)
			{
				if (!fInWord)
				{
					fInWord = true;
					ichMinWord = m_ich;
				}
			}
			else
			{
				if (fInWord)
				{
					CheckWord(ichMinWord, m_ich);
					fInWord = false;
				}
			}
		}
		if (fInWord)
			CheckWord(ichMinWord, m_cch);
	}

	void GetDictionary(const OLECHAR * pchId)
	{
		if (wcscmp(m_stuDictId.Chars(), pchId) == 0)
			return;
		m_stuDictId.Assign(pchId);
		m_pvpbox->Root()->GetDictionary(pchId, &m_qcw);
	}

	// Answer true if the character at ich should be spell-checked, at least in so far as this is controlled
	// by the spell check property and read-only property. (Other reasons for treating it as read-only and
	// therefore not checking it are not considered here.) Also indicate a range of characters that have the
	// same properties.
	bool IsRunSpellChecked(int ich, int * pichMinRun, int * pichLimRun, int * pws)
	{
		int isbt;
		int irun = 0;
		ITsTextPropsPtr qttp;
		VwPropertyStorePtr qzvps;
		m_psrc->GetCharPropInfo(ich, pichMinRun, pichLimRun, &isbt,
			&irun, &qttp, &qzvps);
		*pws = qzvps->Chrp()->ws;
		if (qzvps->SpellingMode() == ksmDoNotCheck)
			return false;
		// This doesn't catch a lot of non-editable stuff, but it's quick.
		// The full test of editability is relatively expensive, so we only do it
		// if we find a mis-spelling.
		if (qzvps->SpellingMode() != ksmForceCheck && !qzvps->Editable())
			return false;
		return true;
	}

	// We want to check, and if erroneous appropriately underline, the sequence of contiguous word-forming
	// characters from ichMinWord to ichLimWord (rendering offsets) in the text source for the paragraph.
	// First, we remove runs that are not spell-checked at the start and end. If nothing remains, there
	// is no problem; quit.
	// We then have a range that begins and ends with stuff we want to check.
	// If there is embedded stuff we don't want to check, we have a type-2 problem.
	// If the remaining range contains more than one writing system, we have a type-2 problem.
	// Otherwise, check the remaining text and report a type-1 problem if it's not in the spell dictionary.
	void CheckWord(int ichMinWord1, int ichLimWord1)
	{
		int ichMinWord = ichMinWord1; // copies we may modify.
		int ichLimWord = ichLimWord1;
		// reduce range to eliminate anything at the boundaries that is not spell-checked.
		int ichMinRun;
		int ichLimRun;
		int ws;
		while (ichLimWord > ichMinWord && !IsRunSpellChecked(ichMinWord, &ichMinRun, &ichLimRun, &ws))
			ichMinWord = ichLimRun;
		while (ichLimWord > ichMinWord && !IsRunSpellChecked(ichLimWord - 1, &ichMinRun, &ichLimRun, &ws))
			ichLimWord = ichMinRun;
		if (ichLimWord <= ichMinWord)
			return;  //nothing requires checking.
		// Look for type-2 problems ('word' not uniform enough to check meaningfully)
		for(int ich = ichMinWord; ich < ichLimWord; )
		{
			int wsRun;
			// An embedded ORC that hasn't been replaced yet is probably invisible and should be ingnored.
			if (m_text[ich] == L'\xFFFC')
			{
				ich++;
				continue;
			}
			if (!IsRunSpellChecked(ich, &ichMinRun, &ichLimRun, &wsRun) || wsRun != ws)
			{
				// Since we know both ends of the run are spell-checked, and the WS of the last run,
				// if we find any run that is NOT spell-checked, or any run in a different WS,
				// we have a type-2 problem.
				AddDispPropOverrides(ichMinWord, ichLimWord, kclrBlue);
				return;
			}
			Assert(ichLimRun > ich);
			ich = ichLimRun;
		}

		// Actually check the spelling of the word at the current boundaries; it's all in one writing system.
		StrUni word = m_text.Mid(ichMinWord, ichLimWord - ichMinWord);
		UnicodeString ucInput(word.Chars(), word.Length());
		UnicodeString ucOutput;
		UErrorCode uerr = U_ZERO_ERROR;
		Normalizer::normalize(ucInput, (UNormalizationMode)knmNFC, 0, ucOutput, uerr);
		if (U_FAILURE(uerr)) // may get warnings, like not terminated.
			return; // give up if we can't normalize.
		word.Assign(ucOutput.getBuffer(), ucOutput.length());

		SmartBstr sbstrWsId;
		ILgWritingSystemPtr qwse;
		CheckHr(m_qwsf->get_EngineOrNull(ws, &qwse));
		if (!qwse)
			return;
		CheckHr(qwse->get_SpellCheckingId(&sbstrWsId));
		if (sbstrWsId.Length() == 0)
			return;
		GetDictionary(sbstrWsId.Chars());
		if (!m_qcw)
			return; // can't check this language.
		ComBool fOk;
		CheckHr(m_qcw->Check(const_cast<OLECHAR *>(word.Chars()), &fOk));
		if (fOk)
			return; // all is well
		int isbt;
		int irun = 0;
		ITsTextPropsPtr qttp;
		VwPropertyStorePtr qzvps;
		m_psrc->GetCharPropInfo(ichMinWord, &ichMinRun, &ichLimRun, &isbt,
			&irun, &qttp, &qzvps);
		// Enhance JohnT: should we do something different if only PART of the word has forceSpellCheck set?
		// I don't think there's any way that CAN happen right now.
		if (qzvps->SpellingMode() != ksmForceCheck)
		{
			// Check whether it is really editable.
			int ichMinWordLog;
			CheckHr(m_psrc->RenToLog(ichMinWord, &ichMinWordLog));
			int ichLimWordLog;
			CheckHr(m_psrc->RenToLog(ichLimWord, &ichLimWordLog));
			if (!IsReallyEditable(ichMinWordLog, ichLimWordLog))
				return;
		}

		AddDispPropOverrides(ichMinWord, ichLimWord, kclrRed);
	}

	// Set up the overrides we need to give the specified range of characters a squiggle underline of the
	// specified color.
	void AddDispPropOverrides(int ichMinWord, int ichLimWord, int clr)
	{
		// Need a separate override for each run in the word
		for (int ich = ichMinWord; ich < ichLimWord; )
		{
			int ichMinRun, ichLimRun, isbt;
			int irun = 0;
			ITsTextPropsPtr qttp;
			CachedProps * pchrp = m_psrc->GetCharPropInfo(ich, &ichMinRun, &ichLimRun, &isbt,
				&irun, &qttp, NULL);

			DispPropOverride dpo;
			dpo.ichMin = ich;
			dpo.ichLim = Min(ichLimRun, ichLimWord);
			dpo.chrp = *pchrp;
			dpo.chrp.clrUnder = clr;
			dpo.chrp.unt = kuntSquiggle;
			m_vdp.Push(dpo);

			Assert(ichLimRun > ich);
			ich = ichLimRun;
		}
	}

	bool IsReallyEditable(int ichMin, int ichLim)
	{
		HVO hvoEdit;
		int tagEdit;
		int ichMinEditProp;
		int ichLimEditProp;
		IVwViewConstructorPtr qvvcEdit;
		int fragEdit;
		VwAbstractNotifierPtr qanote;
		int iprop;
		VwNoteProps vnp;
		int itssProp;
		ITsStringPtr qtssProp;
		// This uses notifier information to determine the editable property following ichIP.
		VwEditPropVal vepv = m_pvpbox->EditableSubstringAt(ichMin, ichLim, false,
			&hvoEdit, &tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit,
			&qanote, &iprop, &vnp, &itssProp, &qtssProp);
		return vepv == kvepvEditable;
	}

	// Core wrapper for whole algorithm
	void Run()
	{
		// If source is an override but NOT a spelling one, skip.
		// Typically this would mean we're in a TE diff view or an active IME paragraph.
		if (dynamic_cast<VwOverrideTxtSrc *>(m_psrc))
			return;
		OLECHAR * pch;
		m_text.SetSize(m_cch, &pch);
		CheckHr(m_psrc->Fetch(0, m_cch, pch));
		CheckRun();
		bool fChanged = false;
		if (!m_qsotsOverride && m_vdp.Size() > 0)
		{
			m_qsotsOverride.Attach(NewObj VwSpellingOverrideTxtSrc(m_psrc));
			m_pvpbox->SetSource(m_qsotsOverride);
		}
		else if (m_qsotsOverride && m_vdp.Size() == 0)
		{
			m_pvpbox->SetSource(m_qsotsOverride->EmbeddedSrc());
			fChanged = true;
		}
		if (m_vdp.Size() > 0)
			fChanged = m_qsotsOverride->UpdateOverrides(m_vdp);
		if (fChanged)
			UpdateDisplay();
	}

	void UpdateDisplay()
	{
		// JohnT: underline doesn't affect the layout at all, so should just be able to redraw.
		m_pvpbox->Invalidate();
		// Enhance JohnT: this could be optimized; typically the size of the paragraph
		// does not change.
		//VwRootBox * prootb = m_pvpbox->Root();
		//HoldLayoutGraphics hg(prootb);
		//FixupMap fixmap;
		//VwBox * pboxContainer;
		//for (pboxContainer = m_pvpbox; pboxContainer; pboxContainer = pboxContainer->Container())
		//{
		//	Rect vwrect = pboxContainer->GetInvalidateRect();
		//	fixmap.Insert(pboxContainer, vwrect);
		//}
		//m_pvpbox->_Height(0); // forces it to be fully laid out.
		//prootb->RelayoutRoot(hg.m_qvg, &fixmap);
	}
};

/*----------------------------------------------------------------------------------------------
	Main driver routine for spell checking. Scan your text for mis-spelled words, and if any
	are found, insert an overlay text source to squiggle them.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::SpellCheck()
{
	SpellCheckMethod method(this);
	method.Run();
}


/*----------------------------------------------------------------------------------------------
	Draw an underline from xdLeft to xdRight at ydTop, given the specified screen resolution,
	the desired colur and underline type, and (for aligning squiggles) the offset in the
	destination drawing rectangle.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::DrawUnderline(IVwGraphics * pvg, int xdLeft, int xdRight, int ydTop,
	int dxScreenPix, int dyScreenPix, int clrUnder, int unt, int xOffset)
{
	int rgdx[2];
	int cdx = 1;
	CheckHr(pvg->put_ForeColor(clrUnder));
	int xStartPattern;
	rgdx[0] = INT_MAX; // one long continuous segment, used for single and double
	switch(unt)
	{
	case kuntSquiggle:
		{ // BLOCK for var decls
			// ENHANCE JohnT: should we do some trick to make it look continuous
			// even if drawn in multiple chunks?
			// Note: going up as well as down from ydTop makes the squiggle
			// actually touch the bottom of typical letters. This is consistent
			// with Word; FrontPage puts the squiggle one pixel clear. If we want
			// the latter effect, just use ydTop + dyScreenPix.
			int dxdSeg = max(1, dxScreenPix * 2);
			int xdStartFromTrueLeft = ((xdLeft - xOffset) / dxdSeg) * dxdSeg; // aligns it to multiple of dxdSeg
			int xdStart = xdStartFromTrueLeft + xOffset; // back in drawing coords
			int dydStart = -dyScreenPix; // toggle for up/down segs
			// Initial value is determined by whether xdStart is an odd or even multiple
			// of dxdSeg.
			if (xdStartFromTrueLeft % (dxdSeg * 2) != 0)
				dydStart = -dydStart;
			while (xdStart < xdRight)
			{
				int xdEnd = xdStart + dxdSeg;
				CheckHr(pvg->DrawLine(xdStart, ydTop + dydStart,
					xdEnd, ydTop - dydStart));
				dydStart = -dydStart;
				xdStart = xdEnd;
			}
		}
		// This uses diagonal lines so don't break and draw a straight one, return
		return;
	case kuntDotted:
		rgdx[0] = dxScreenPix * 2;
		rgdx[1] = dxScreenPix * 2;
		cdx = 2;
		break;
	case kuntDashed:
		rgdx[0] = dxScreenPix * 6;
		rgdx[1] = dxScreenPix * 3;
		cdx = 2;
		break;
	case kuntStrikethrough:
		{
			int dydAscent;
			CheckHr(pvg->get_FontAscent(&dydAscent));
			ydTop = ydTop - dydAscent / 3;
			break;
		}
	case kuntDouble:
		xStartPattern = xdLeft;
		pvg->DrawHorzLine(xdLeft, xdRight, ydTop + dyScreenPix * 2, dyScreenPix,
			cdx, rgdx, &xStartPattern);
		// FALL THROUGH -- to draw the upper line as well.
	case kuntSingle:
		// For (some) forwards compatibility, treat any unrecognized underline
		// type as single.
	default:
		break;
	}
	xStartPattern = xdLeft;
	pvg->DrawHorzLine(xdLeft, xdRight, ydTop, dyScreenPix,
		cdx, rgdx, &xStartPattern);
}

/*----------------------------------------------------------------------------------------------
	Assume that prgchGuidsThis and prgchGuidsOther both contain lists of Guids represented as
	kcchGuidRepLength OLECHARs each. Further assume the lists are in standard order, as
	determined by using CompareGuids, with later items being less. Produce a vector of TagInfo
	for tags that are in this and not in other, and are not hidden. pttpOther may be null.
	If ptsi is non-null, fill in its vmi vector with the full list of menu infos,
	without reference to the limit on how many are displayed.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::FindGuidDiffs(ITsTextProps * pttpThis, ITsTextProps * pttpOther,
	IVwOverlay * pvo, VecTagInfo & vtiDiffs, TagSelectInfo * ptsi)
{
	AssertPtr(pttpThis);
	AssertPtrN(pttpOther);

	int cchMaxName = 0; // Don't need name usually...
	if (ptsi)
		cchMaxName = 256; // Unless getting ptsi info!

	// Get the lists of guids from the two ttps.
	SmartBstr sbstrGuidsThis;
	CheckHr(pttpThis->GetStrPropValue(ktptTags, &sbstrGuidsThis));
	OLECHAR * prgchGuidsThis = const_cast<OLECHAR *>(sbstrGuidsThis.Chars());
	int cguidThis = BstrLen(sbstrGuidsThis) / kcchGuidRepLength;

	SmartBstr sbstrGuidsOther;
	if (pttpOther)
		CheckHr(pttpOther->GetStrPropValue(ktptTags, &sbstrGuidsOther));
	OLECHAR * prgchGuidsOther = const_cast<OLECHAR *>(sbstrGuidsOther.Chars());
	int cguidOther = BstrLen(sbstrGuidsOther) / kcchGuidRepLength;

	// Loop over them looking for unhidden differences.
	OLECHAR * pchThis = prgchGuidsThis;
	OLECHAR * pchOther = prgchGuidsOther;
	OLECHAR * pchLimThis = pchThis + kcchGuidRepLength * cguidThis;
	OLECHAR * pchLimOther = pchOther + kcchGuidRepLength * cguidOther;
	for (; pchThis < pchLimThis; )
	{
		// See if the GUID at pchThis is >, =, or < the one at pchOther.
		// If pchOther has run out, treat as >, meaning the current item is in this only.
		// In the following, nComp < 0 means *pchThis is > *pchOther. This is backwards.
		// The reason is that this code was originally written for ascending order, but
		// the guids wound up stored in descending order. So we just reverse the polarity
		// of the comparison function.
		int nComp = -1;
		if (pchOther < pchLimOther)
			nComp = -CompareGuids(pchThis, pchOther);
		// If they are the same we have nothing to do
		if (nComp == 0)
		{
			pchThis += kcchGuidRepLength;
			pchOther += kcchGuidRepLength;
			continue;
		}
		else if (nComp < 0)
		{
			// If the one in this is <, it is in this and not in other. See if it is visible,
			// and get its abbreviation.
			TagInfo ti;
			int cchAbbr;
			int cchName;
			OLECHAR rgchAbbr[256];
			OLECHAR rgchName[256];
			ComBool fHidden;
			HRESULT hr;
			IgnoreHr(hr = pvo->GetDispTagInfo(pchThis, &fHidden, &ti.clrFore, &ti.clrBack,
				&ti.clrUnder, &ti.unt, rgchAbbr, 256, &cchAbbr, rgchName, cchMaxName, &cchName));
			if (SUCCEEDED(hr) && !fHidden)
			{
				// If somehow we can't get the tag info (maybe its abbr is too long??) skip it.
				// If all is well and it is not a hidden tag push it into the vector.
				// If it is a hidden tag, ignore it.
				memcpy(&ti.uid, pchThis, kcchGuidRepLength * isizeof(OLECHAR));
				ti.stuAbbr.Assign(rgchAbbr, cchAbbr);
				vtiDiffs.Push(ti);
			}
			/*if (ptsi)
			{
				MenuInfo mi;
				memcpy(mi.rgchGuid, pchThis, kcchGuidRepLength * isizeof(OLECHAR));
				mi.stuAbbr = ti.stuAbbr;
				mi.stuName.Assign(rgchName, cchName);
				ptsi->vmi.Push(mi);
			}*/
			pchThis += kcchGuidRepLength;
			// DON'T increment pchOther, there may be more tags that don't match before
			// we get to that.
		}
		else
		{
			// If the one in this is >, then the one in other is not in this. This makes a
			// difference, but not one we want to display at this point.
			pchOther += kcchGuidRepLength;
		}
	}
	int cguidMaxDiff = 1; // Will work unless we have more than 1. If we do look it up.
	// Sort vtiDiffs so it is alphabetical by abbr, then chop off if we have
	// too many. I'm presuming that having, and even more displaying, lots will be unusual,
	// so an insertion sort (which can be cut off at the specified number) seems ideal.
	ILgCollatingEnginePtr qcoleng;
	if (vtiDiffs.Size() > 1)
	{
		// It's worth looking up how many we can display.
		CheckHr(pvo->get_MaxShowTags(&cguidMaxDiff));
		// Also we need a collating engine.
		qcoleng.CreateInstance(CLSID_LgUnicodeCollater);
		ILgWritingSystemFactoryPtr qwsf;
		GetWritingSystemFactory(&qwsf);
		CheckHr(qcoleng->putref_WritingSystemFactory(qwsf));
	}
	int cguidKeep = std::min(cguidMaxDiff, vtiDiffs.Size());
	// Normally the following loop just runs until we have the number we want alphabetically
	// first. However, if we are also generating menuinfo, we need to build the full list.
	int cguidLoop = cguidKeep;
	if (ptsi)
		cguidLoop = vtiDiffs.Size();

	for (int iti = 0; iti < cguidLoop; iti++)
	{
		int itiSmall = iti;
		// Find the smallest item at index iti or greater, and move it to position iti
		for (int itiTry = iti + 1; itiTry < vtiDiffs.Size(); itiTry++)
		{
			// See if [itiTry] is less than [itiSmall]. If so, set itiSmall to itiTry.
			// OPTIMIZE JohnT: if performance is a real issue and we are often dealing with longer
			// lists, it may be worth using the SortKey function, and keeping the sort key
			// of itiSmall.
			int nComp;
			CheckHr(qcoleng->Compare(vtiDiffs[itiSmall].stuAbbr.Bstr(),
				vtiDiffs[itiTry].stuAbbr.Bstr(), fcoDefault, &nComp));
			if (nComp > 0)
				itiSmall = itiTry;
		}
		// Move the item
		if (itiSmall != iti)
		{
			// Swap items at iti and itiSmall
			TagInfo tiTemp = vtiDiffs[iti];
			vtiDiffs[iti] = vtiDiffs[itiSmall];
			vtiDiffs[itiSmall] = tiTemp;
		}
		/*if (ptsi)
		{
			MenuInfo miTemp = ptsi->vmi[iti];
			ptsi->vmi[iti] = ptsi->vmi[itiSmall];
			ptsi->vmi[itiSmall] = miTemp;
		}*/
	}
	vtiDiffs.Delete(cguidKeep, vtiDiffs.Size());
}

/*----------------------------------------------------------------------------------------------
	Given a coordinate transformation that would be used to draw the recipient,
	figure the one for the specified child.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::CoordTransForChild(IVwGraphics * pvg, VwBox * pboxChild, Rect rcSrc,
	Rect rcDst, Rect * prcSrc, Rect * prcDst)
{
	Assert(m_pboxFirst); // otherwise how can we be asked about a child?
	Assert(pboxChild->Container() == this);
	Assert(pboxChild->Top() != knTruncated); // Should not be working with these.

	rcSrc.Offset(-Left(), -Top());
	*prcSrc = rcSrc; // nothing fancy about this.
	*prcDst = rcDst;
}


/*----------------------------------------------------------------------------------------------
	This is overridden for two reasons: first, to duplicate the adjustment for destination
	coord actual box widths, which is done in DrawForeground; also, to insist that a click
	far below or above the box is considered to be at the start or end of the paragraph,
	not the closest position on the bottom or top line.
----------------------------------------------------------------------------------------------*/
VwBox * VwParagraphBox::FindBoxClicked(IVwGraphics * pvg, int xd, int yd,
	Rect rcSrc, Rect rcDst, Rect * prcSrc, Rect * prcDst)
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
		sta.Format("VwParagraphBox::FindBoxClicked(}:   %s  %3d %s %3d %s %3d  && %3d %s %3d %s %3d%n",
			pszWhere, xMin, (xMin <= x ? "<=" : "> "), x, (x <= xMax ? "<=" : "< "), xMax,
			yMin, (yMin <= y ? "<=" : "> "), y, (y <= yMax ? "<=" : "< "), yMax);
		::OutputDebugStringA(sta.Chars());
	}
#endif
	rcSrc.Offset(-Left(), -Top());

	// Do a loop very like
	// the GroupBox implementation, except that we do the adjustment of rcDst for the
	// actual widths of the boxes.
	BoxVec vbox;
	if (!m_pboxFirst)
		return NULL;
	int xs = rcDst.MapXTo(xd, rcSrc);
	VwBox * pboxClosest = NULL;
	int dsqMin = INT_MAX; // square of distance from best box so far to target point
	int dyMin = INT_MAX;
	Rect rcDstClosest; // appropriate rcDst for the closest box.
	int ys = rcDst.MapYTo(yd, rcSrc);

	VwBox * pboxLine = m_pboxFirst;
	if (m_pboxFirst->Top() == knTruncated)
		pboxLine = NULL;

	int dysTagAbove = 0;
	int dysTagBelow = 0;

	for (; pboxLine; )
	{
		int ysTop, ysBottom;
		pboxLine = GetALineTB(pboxLine, vbox, rcSrc.Height(), &ysTop, &ysBottom);
		// If the click is beyond the end (typically below the bottom) of this line, move on to the next.

		if (IsVerticallyAfter(ys, VerticallyLast(ysBottom, ysTop)))
		{
			// We also need to check if it's part of the close tag section below
			// the line of text. If so, we want the selection to be on this line.
			if (dysTagBelow == 0)
			{
				ComputeTagHeights(pvg, Source()->Overlay(), rcSrc.Height(), dysTagAbove,
					dysTagBelow);
			}
			if (IsVerticallyAfter(ys, VerticallyLast(ysBottom + dysTagBelow, ysTop - dysTagAbove)))
				continue;
		}
		// Draw the boxes from left to right, adjusting for their widths.
		// ENHANCE JohnT (v2?): if paragraph is right-aligned, draw from right to left.
		for (int ibox = 0; ibox < vbox.Size(); ibox++)
		{
			VwBox * pbox = vbox[ibox];
			bool fInside = pbox->IsPointInside(xd, yd, rcSrc, rcDst);
			if (fInside)
			{
				// The algorithm that uses the DsqToPoint() to find the closest box can
				// actually choose a neighboring box in preference to one that contains
				// the point!  See LT-5203.
#if 99-99
				StrAnsi sta;
				sta.Format("VwParagraphBox::FindBoxClicked(): using surrounding pbox instead of pboxClosest%n");
				::OutputDebugStringA(sta.Chars());
#endif
				return pbox->FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, prcSrc, prcDst);
			}
			// This bit is just like the GroupBox version of this routine
			int dsq = pbox->DsqToPoint(xs, ys, rcSrc);
			int dy = pbox->Top() - ys;
			if (dy < 0)
			{
				dy = ys - pbox->Bottom();
				if (dy < 0)
					dy = 0;
			}
			// Now, which box do we want?
			// Basically, the closest; but, if the user clicks beyond the end of a line, we want
			// it to be a click on that line, even if it is closer to a box on a (longer) line
			// above.  This suggests using dsq iff dy == 0; but it is just pathologically
			// possible that there is a large box at the start of a line followed by a small
			// one, and the user clicks somewhere that is in the y range of the large box but
			// not the small one. In that case, we want the click to be treated as an in-line
			// box, but to go to the closer one.
			// How this works out is that,
			//  1. If the current box is on the same baseline as the current best box,
			//		we simply use distance.
			//  2. Otherwise, if the point is not in either y range, we simply use distance.
			//  3. Otherwise, we keep the box in whose y range the point occurs.
			bool fBetter = !pboxClosest; // certainly better first time round
			if (pboxClosest && (pboxClosest->Baseline() == pbox->Baseline() ||
				(dy > 0 && dyMin > 0)))
			{
				// Use total distance
				if (dsq < dsqMin)
				{
					fBetter = true;
				}
				else if (dsq == dsqMin)
				{
					//	two are equidistant. Normally we can pick the first arbitrarily;
					//	but if one is an empty string at the border of the other, we
					//	were probably aiming at the empty string.
					fBetter = pbox->Width() == 0;
				}
			}
			else
			{
				fBetter = dy < dyMin;
			}
			if (fBetter)
			{
				dsqMin = dsq;
				pboxClosest = pbox;
				rcDstClosest = rcDst;
				dyMin = dy;
			}
		}
		// Let the closest box on the line that contains the point handle it.
		return pboxClosest->FindBoxClicked(pvg, xd, yd, rcSrc, rcDstClosest, prcSrc, prcDst);
	}
	// if click is below last line, act like a click off to the right of the last box.
	VwBox * pboxLast = m_pboxLast->EndOfChain(); // very last box, even if an extension
	// Make like a click at the right of that box
	// Todo JohnT (v2): if box is right-to-left, make like a click at the left.
	int xdFix = rcSrc.MapXTo(pboxLast->Left() + pboxLast->Width(), rcDst);
	pboxLast->FindBoxClicked(pvg, xdFix, yd, rcSrc, rcDst, prcSrc, prcDst);
	return pboxLast->FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, prcSrc, prcDst);
}

/*----------------------------------------------------------------------------------------------
	Get the top and bottom of the space set aside for the 'line' containing pboxTarget
	@param pvg The graphics object. It is assumed that get_YUnitsPerInch equals rcDstRoot.Height().
	@param fRelativeToRoot True to get the location of the box to be relative to the whole
						   view, false to get the location relative to the container
	@param ppboxLastOnLine if non-null, returns pointer to last box on line.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::GetLineTopAndBottom(IVwGraphics * pvg, VwBox * pboxTarget, int * pydTop,
	int * pydBottom, Rect rcSrcRoot, Rect rcDstRoot, bool fRelativeToRoot, VwBox ** ppboxLastOnLine)
{
	if (ppboxLastOnLine)
		*ppboxLastOnLine = NULL; // for robustness, and to help detect if we fail to set it.
	// baseline of the current line, relative to this box.
	int ysBaseLine = -1; // not equal to baseline of first box, so we treat it as start line
	VwBox * pbox;
	int ysLineTop = 0; // of the line we are processing (arbitrary inits)
	int ysLineBottom = 0;
	int ysPrevLineBottom = 0; // of the line we processed last; init to top of para
	int ysLineAboveBottom = 0; // line above the one containing the target
	int ysTargetLineTop = 0;  // line containing the target box
	int ysTargetLineBottom = 0;
	int ysNextLineTop; // following line (after target) if any
	bool fFoundTarget = false;
	bool fInNextLine = false; // true when we have started to process the line after the target

	// For exact line spacing:
	int dypInch;
	CheckHr(pvg->get_YUnitsPerInch(&dypInch));
	int dpiSrc = MulDiv(dypInch, rcSrcRoot.Height(), rcDstRoot.Height());
	int dypLineHeight = MulDiv(m_qzvps->LineHeight(), dpiSrc, kdzmpInch);
	bool fExactLineHeight = m_qzvps->ExactLineHeight();
	// Used in exact algorithm.
	int dysExactDescent = dypLineHeight - MulDiv(m_dympExactAscent, dpiSrc, kdzmpInch);

	Rect rcSrc(rcSrcRoot);
	Rect rcDst(rcDstRoot);
	if (fRelativeToRoot)
	{
		CoordTransFromRoot(pvg, rcSrcRoot, rcDstRoot, &rcSrc, &rcDst);
		rcSrc.Offset(-Left(), -Top()); // adjust for internal positions
	}
	int dysTagAbove, dysTagBelow;
	ComputeTagHeights(pvg, Source()->Overlay(), rcSrc.Height(), dysTagAbove, dysTagBelow);

	// These intial values are used only in the inverted algorithm. Therefore a useful value for the
	// top of the line before the first is the bottom of the content region of the box.
	// Since the real line tops we compute don't include any TagAbove, we subtract that
	// at the end of the algorithm; so add it in here to cancel that out if the value
	// here is used.
	int ysPrevLineTop, ysPrevPrevLineTop;
	ysLineTop = ysPrevLineTop = ysPrevPrevLineTop = Height() - GapBottom(dypInch) + dysTagAbove;

	VwBox * pboxPrev = NULL;
	for (pbox = m_pboxFirst; pbox; pbox = pbox->Next())
	{
		if (pbox->Baseline() != ysBaseLine)
		{
			// starting a new line. If the previous line was the one after the target, we are
			// done
			if (fInNextLine)
			{
				break;
			}
			// If last line was the target, save info about it.
			if (fFoundTarget)
			{
				fInNextLine = true; // we will now process the line after the target
				ysTargetLineTop = ysLineTop;
				ysTargetLineBottom = ysLineBottom;
				if (ppboxLastOnLine)
					*ppboxLastOnLine = pboxPrev;
			}
			// Save the old bottom, and set the new
			// ones to those for this box; subsequent boxes may modify this
			ysPrevLineBottom = ysLineBottom;
			ysPrevPrevLineTop = ysPrevLineTop;
			ysPrevLineTop = ysLineTop;
			int ysNewBaseLine = pbox->Baseline();
			if (fExactLineHeight)
			{
				// We have to be tricky here because for pictures and similar things we
				// make a line be a multiple of the line height. We can't be sure how
				// far above or below the baseline particular boxes go. However, we
				// always put the baseline exactly (line height - exact ascent) above
				// the bottom of the line. So, we can get the bottom by going down from the
				// current baseline, and the top by going down from the previous baseline.
				// For first line, regard previous baseline as being the
				// 'exact descent' above the bottom of the top gap.
				int ysPrevBaseline;
				if (ysBaseLine < 0)
					ysPrevBaseline = GapTop(dpiSrc) - dysExactDescent;
				else
					ysPrevBaseline = ysBaseLine;
				ysLineTop = ysPrevBaseline + dysExactDescent;
				ysLineBottom = ysNewBaseLine + dysExactDescent;
			}
			else
			{
				ysLineTop = pbox->Top();
				ysLineBottom = pbox->Bottom();
			}
			ysBaseLine = ysNewBaseLine;
		}
		else if (fExactLineHeight)
		{
			Assert(m_dympExactAscent > -1);
		}
		else
		{
			ysLineTop = min(ysLineTop, pbox->Top());
			ysLineBottom = max(ysLineBottom, pbox->Bottom());
		}
		if (pbox == pboxTarget)
		{
			// found the one we are looking for. Save the end of the previous line.
			ysLineAboveBottom = ysPrevLineBottom;
			fFoundTarget = true;
		}
		pboxPrev = pbox;
	}
	Assert(fFoundTarget);  // very bad usage if we don't get to it!


	// We exit the loop either because we were about to start the line two after the target,
	// or because we ran out of boxes. If we ever found a box in a line after the target,
	// the current top of line is the top of that line. Otherwise, there is no subsequent
	// line, so consider the bottom of the target line to be the top of the next
	if (fInNextLine)
	{
		ysNextLineTop = ysLineTop;
		// ysTargetLineTop and Bottom are already set
	}
	else
	{
		// no more lines, pboxTarget is part of the last line.
		if (ppboxLastOnLine)
			*ppboxLastOnLine = pboxPrev; // the very last box, we never found any subsequent line.
		ysTargetLineTop = ysLineTop;
		ysTargetLineBottom = ysLineBottom;
		ysPrevPrevLineTop = ysPrevLineTop; // we didn't go on to the next line, so only need to go one back.
		// The top position for the next line is normally the top of the line,
		// excluding its tag above, if any. Therefore, below, we subtract
		// dysTagAbove to get the place where the next line really starts.
		// In this case we have the "real" place the next line starts, so cancel
		// out the correction.
		ysNextLineTop = Height() - GapBottom(rcSrcRoot.Height()) + dysTagAbove;
	}

	// Split the difference in dest coords, so invert rectangles on adjacent lines meet exactly.
	int ydTargetLineTop = rcSrc.MapYTo(ysTargetLineTop - dysTagAbove, rcDst);
	//int ydLineAboveBottom = rcSrc.MapYTo(ysLineAboveBottom, rcDst);
	int ydNextLineTop = rcSrc.MapYTo(ChooseSecondIfInverted(ysNextLineTop, ysPrevPrevLineTop) - dysTagAbove, rcDst);
	//int ydTargetLineBottom = rcSrc.MapYTo(ysTargetLineBottom, rcDst);

	*pydTop = ydTargetLineTop;
	*pydBottom = ydNextLineTop;
	// We should determine the last box on the line if it is asked for.
	Assert(!ppboxLastOnLine || *ppboxLastOnLine);
}

/*----------------------------------------------------------------------------------------------
	Compute the space needed above and below each line in the paragraph for drawing tags.
	This depends on the current overlay settings, if any, as well as the current drawing
	mode and related stuff.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::ComputeTagHeights(IVwGraphics * pvg, IVwOverlay * pxvo, int dyInch,
	int & dyTagAbove, int & dyTagBelow)
{
	dyTagAbove = 0;
	dyTagBelow = 0;
	if (pxvo && !m_fSemiTagging)
	{
		VwOverlayFlags vof;
		CheckHr(pxvo->get_Flags(&vof));
		int dyTagHeight;
		if (vof & kgrfofTagAnywhere)
		{
			// figure the height we need for showing a tag
			LgCharRenderProps chrp;
			memset(&chrp, 0, sizeof(chrp));
			SmartBstr sbstrFont;
			CheckHr(pxvo->get_FontName(&sbstrFont));
			OLECHAR * prgchSrc = const_cast<OLECHAR *>(sbstrFont.Chars());
			wcsncpy_s(chrp.szFaceName, 32, prgchSrc, min(32, BstrLen(sbstrFont) + 1));
			chrp.szFaceName[31] = 0; // ensure null termination even for long path
			CheckHr(pxvo->get_FontSize(&chrp.dympHeight));
			chrp.dympOffset = 0;
			chrp.ssv = kssvOff;
			chrp.ttvBold = kttvOff;
			chrp.ttvItalic = kttvOff;
			// writing system, ows, etc. don't matter for now
			CheckHr(pvg->SetupGraphics(&chrp));
			int dx; // dummy
			CheckHr(pvg->GetTextExtent(0, NULL, &dx, &dyTagHeight));
			// Make sure the descent is at least 4/96 inch, for (possibly double) underlining
			int dyAscent;
			CheckHr(pvg->get_FontAscent(&dyAscent));
			int dydMinDescent = 4 * dyInch / 96;
			if (dyTagHeight - dyAscent < dydMinDescent)
				dyTagHeight = dyAscent + dydMinDescent;

			// Figure specifically where needed.
			if (vof & kgrfofTagAbove)
			{
				dyTagAbove = dyTagHeight;
				// We add two pixels to the above height, but not the below, because the
				// upper bracket goes two pixels above the top of the text.
				// On the other hand, we allow the below bracket to overlap with the second
				// line of double underline, which we already made room for.
				if (vof & kfofLeadBracket)
					dyTagAbove += 2 * dyInch / 96; // two pixels on a standard screen.;
			}
			if (vof & kgrfofTagBelow)
				dyTagBelow = dyTagHeight;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Relink your boxes to replace some with others. Caller is responsible to clean up notifiers
	and delete the boxes unlinked.  Paragraph box overrides to adjust char offsets in later
	boxes.
	Arguments:
		pboxPrev: box before the first one to replace, or NULL to replace very first box
		pboxLim: box after the last one to replace, or NULL to replace to the very end
		pboxFirst: start of chain of boxes to replace, or NULL for simple deletion
		pboxLast: last box in chain to replace, or NULL for simple deletion
----------------------------------------------------------------------------------------------*/
VwBox * VwParagraphBox::RelinkBoxes(VwBox * pboxPrev, VwBox * pboxLim, VwBox * pboxFirst,
	VwBox * pboxLast)
{
	Assert(false); // paragraphs should use a different approach to updating.
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Add a child box. In a paragraph it must have a dummy item in the text source as well.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::Add(VwBox * pbox)
{
	VwGroupBox::Add(pbox);
	// Put a dummy record to stand for it in the text source.
	m_qts->Vpst().Push(VpsTssRec(pbox->Style(), NULL));
}

/*----------------------------------------------------------------------------------------------
	Override PadLeading so that it includes any negative FirstIndent().
	This has the effect that the padding is taken as to the left-most of the first line
	and the other lines.
----------------------------------------------------------------------------------------------*/
int VwParagraphBox::PadLeading()
{
	return m_qzvps->PadLeading() + std::max(-m_qzvps->FirstIndent(), 0);
}

/*----------------------------------------------------------------------------------------------
	The spec says, "If two paragraphs are adjacent and are of the same style (either a global
	style or direct formatting), then there is no border between them." Therefore if the
	border width is not zero, and the previous box's style is the same as this, we adjust
	to zero. (There may be cases PM intended which this does not catch. We'll see how they
	react.)
----------------------------------------------------------------------------------------------*/
int VwParagraphBox::BorderTop()
{
	int dysMargin = SuperClass::BorderTop();
	if (dysMargin)
	{
		VwGroupBox * pvgboxCont = Container();
		if (!pvgboxCont)
			return dysMargin;
		// (TimS/EberhardB) This code was changed to fix assertions/crashes that were happening
		// as a result of expanding the next box when the expansion resulted in a box that
		// wasn't contained by the same container of this box. (TE-4141)
		// We can't think of any way this could cause problems with lazy boxes as the user
		// shouldn't ever see a lazy box (and thus not be able to tell that we think a border
		// needs to be drawn when there shouldn't be one). When the user scrolls the lazy box
		// gets expanded and we come here again and "fix" the border.
		//VwBox * pboxPrev = pvgboxCont->RealBoxBefore(this);
		//if ((!pboxPrev) || pboxPrev->Style() != Style())
		//	return dysMargin;

		VwBox * pboxPrev = pvgboxCont->BoxBefore(this);
		if (!pboxPrev || pboxPrev->Style() != Style())
			return dysMargin;
	}
	return 0;
}
int VwParagraphBox::BorderBottom()
{
	int dysMargin = SuperClass::BorderBottom();
	if (dysMargin)
	{
		// (TimS/EberhardB) This code was changed to fix assertions/crashes that were happening
		// as a result of expanding the next box when the expansion resulted in a box that
		// wasn't contained by the same container of this box. (TE-4141)
		// We can't think of any way this could cause problems with lazy boxes as the user
		// shouldn't ever see a lazy box (and thus not be able to tell that we think a border
		// needs to be drawn when there shouldn't be one). When the user scrolls the lazy box
		// gets expanded and we come here again and "fix" the border.
		//VwBox * pboxNext = NextRealBox();
		//if ((!pboxNext) || pboxNext->Style() != Style())
		//	return dysMargin;

		if (!NextOrLazy() || NextOrLazy()->Style() != Style())
			return dysMargin;
	}
	return 0;
}

// qsort function for sorting an array of pointers to integers by the magnitude of the
// integers pointed to.
int compareIntPtrs(const void * ppv1, const void * ppv2)
{
	return **((int **)ppv1) - **((int **)ppv2);
}

/*----------------------------------------------------------------------------------------------
	Normalize each string in your text source to Nfd (limitation: there may possibly be
	non-normalized sequences crossing string boundaries). Fix as well as possible any selections
	known to your root box which are affected.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::MakeSourceNfd()
{
	// Get the selections that might be affected by the normalization.
	SelVec & vselInUse = Root()->ActiveSelections();
	// Build a vector of pointers to some of the selections' member variables: specifically,
	// offsets into this paragraph.
	Vector<int *> vpint;
	for (int isel = 0; isel < vselInUse.Size(); isel++)
	{
		VwTextSelection * psel = dynamic_cast<VwTextSelection *>(vselInUse[isel]);
		if ((!psel) || !psel->m_qrootb)
			continue;
		VwParagraphBox * pvpboxEnd = psel->m_pvpboxEnd;
		if (psel->m_pvpboxEnd == NULL)
			pvpboxEnd = psel->m_pvpbox;
		if (pvpboxEnd == this)
			vpint.Push(&psel->m_ichEnd);
		if (psel->m_pvpbox == this)
			vpint.Push(&psel->m_ichAnchor);
	}
	// Sort the vector by paragraph offset.
	qsort(vpint.Begin(), vpint.Size(), isizeof(int *), compareIntPtrs);

	// This is the vector of objects that have the strings we need to normalize.
	VpsTssVec & vpst = Source()->Vpst();
	int ichMinOld = 0; // min offset covered by current string before normalization.
	int ichMinNew = 0; // min offset covered by current string after normalization.
	int ipiMinOffsetToFix = 0; // index into vpint of first item to fix, in this string.
	bool fChanged = false;

	for (int ipst = 0; ipst < vpst.Size(); ipst++)
	{
		int cchOld = 1; // count of characters in current string before normalization. (1 if no string)
		VpsTssRec vps = vpst[ipst];
		// In AfDeFeTags fields, clicking find next button with text in the quick find
		// box will cause qtms to be 0. (This occurs anywhere there is a non-string box
		// such as pictures or interlinear bundles.)
		if (vps.qtms)
			CheckHr(vps.qtms->get_Length(&cchOld));
		int ichLimOld = ichMinOld + cchOld;
		int ipiLimOffsetToFix = ipiMinOffsetToFix; // limit of items in vpint that point to current string.
		// Find lim of range that point to current original string.
		while(ipiLimOffsetToFix < vpint.Size() && *vpint[ipiLimOffsetToFix] <= ichLimOld)
			ipiLimOffsetToFix++;
		// The ones for this string need to be temporarily made relative to the string,
		// for the call to NfdAndFixOffsets.
		for (int ipi = ipiMinOffsetToFix; ipi < ipiLimOffsetToFix; ipi++)
			*(vpint[ipi]) -= ichMinOld;

		int cchNew = 1; // count of characters in current string after normalization. 1 if no string.
		if (vps.qtms)
		{
			ITsStringPtr qtssRep;
			CheckHr(vps.qtms->NfdAndFixOffsets(&qtssRep, vpint.Begin() + ipiMinOffsetToFix,
				ipiLimOffsetToFix - ipiMinOffsetToFix));
			if (qtssRep.Ptr() != vps.qtms.Ptr())
			{
				// Normalization changed something...replace the string in the source
				// and note we need to layout.
				vps.qtms.Attach(qtssRep.Detach());
				vpst[ipst] = vps;
				fChanged = true;
			}
			CheckHr(vps.qtms->get_Length(&cchNew));
		}

		// Make them paragraph-relative again, adjusting by the length of previous
		// NORMALIZED strings.
		for (int ipi = ipiMinOffsetToFix; ipi < ipiLimOffsetToFix; ipi++)
			*(vpint[ipi]) += ichMinNew;

		ichMinNew = ichMinNew + cchNew; // for next iteration.
		ichMinOld = ichLimOld; // for next iteration.
		ipiMinOffsetToFix = ipiLimOffsetToFix;
	}

	// Need to redo the layout of the box if anything changed.
	if (fChanged)
	{
		HoldLayoutGraphics hg(Root());
		// We expect it to be the same size, so lay out in current width, which is usually the
		// full width really available anyway.
		DoLayout(hg.m_qvg, Width());
	}
}

/*----------------------------------------------------------------------------------------------
	Search for a match to the specified pattern within your contents. If the pattern specifies
	a start position by means of a selection, use it.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::Search(VwPattern * ppat, IVwSearchKiller * pxserkl)
{
	ComBool fAbort;
	if (pxserkl)
	{
		CheckHr(pxserkl->FlushMessages());
		CheckHr(pxserkl->get_AbortRequest(&fAbort));
		if (fAbort == ComBool(true))
			return;
	}
	// Do this before retrieving info from any selections, they might get modified.
	MakeSourceNfd();

	VwTextSelection * psel = ppat->Selection();
	int cchSearchLog = Source()->Cch();
	bool fForward = ppat->Forward();
	int ichStartLog = fForward ? 0 : cchSearchLog;
	ppat->SetFound(false);
	// If we have a selection use it as a starting point. Search from the appropriate
	// boundary.
	VwParagraphBox * pvpboxDummy;
	if (psel)
	{
		psel->GetLimit(fForward, &pvpboxDummy, &ichStartLog); // logical position!
	}

	// Determine the end of the range to search.
	VwTextSelectionPtr qselLimit;
	IVwSelection * pselLimitT = ppat->Limit();
	if (pselLimitT)
	{
		try{
			CheckHr(pselLimitT->QueryInterface(CLSID_VwTextSelection, (void **) & qselLimit));
		}
		catch(Throwable& thr)
		{
			if (thr.Result() != E_NOINTERFACE)
				throw thr;  // Only acceptable error is another type.
		}
	}
	int ichEndLog = 0;
	if (fForward)
		ichEndLog = cchSearchLog;
	VwParagraphBox * pvpboxLim = NULL;
	int ichLimitLog;
	if (qselLimit)
		qselLimit->GetLimit(fForward, &pvpboxLim, &ichLimitLog);
	if (pvpboxLim == this)
	{
		if (ichLimitLog == ichStartLog)
		{
			// Searching at limit. This can happen if we wrapped around to a limit
			// at the very start or end of the document, or if we found a match
			// exactly before the limit.
			CheckHr(ppat->put_StoppedAtLimit(true));
			return;
		}
		// Now, if searching towards the limit, use it, otherwise, ignore it.
		if ((fForward && ichLimitLog > ichStartLog) || (!fForward && ichLimitLog < ichStartLog))
			ichEndLog = ichLimitLog;
		else
			pvpboxLim = NULL; // if we fail it's not because of the limit.
	}

	int ichMinLog, ichLimLog;
	CheckHr(ppat->FindIn(Source(), ichStartLog, ichEndLog, fForward, &ichMinLog, &ichLimLog,
		pxserkl));
	if (ichMinLog >= 0)
	{
		// We got a match, set it up as a new selection in the pattern.
		// Make sure at least part of it is visible in the sense of not being cut off by
		// a line limit.
		VwBox * pbox = FirstBox();
		VwStringBox * psbox = NULL;
		bool fVisible = false;
		for (; pbox; pbox = pbox->NextRealBox())
		{
			psbox = dynamic_cast<VwStringBox *>(pbox);
			if (psbox && psbox->IchMin() > ichMinLog)
			{
				fVisible = true; // a segment starts after the beginning of our match
				break;
			}
		}
		if (psbox && !fVisible)
		{
			// Final box starts after match...how does it finish?
			int dichLim;
			CheckHr(psbox->Segment()->get_Lim(psbox->IchMin(), &dichLim));
			if (psbox->IchMin() + dichLim > ichMinLog)
				fVisible = true;
		}
		if (fVisible)
		{
			// We have a useable match.
			VwTextSelectionPtr qsel;
			qsel.Attach (NewObj VwTextSelection(this, ichMinLog, ichLimLog, false));
			ppat->SetSelection(qsel);
			ppat->SetFound(true);
			return;
		}
	}
	// No useable match in this box.
	// Decide whether to set StoppedAtLimit.
	// ENHANCE JohnT: would it be useful to try to figure out whether the limit
	// is actually also the end of the view?
	if (pvpboxLim == this)
	{
		CheckHr(ppat->put_StoppedAtLimit(true));
	}
}

/*----------------------------------------------------------------------------------------------
	Write the contents of a paragraph box to the stream in WorldPad XML format.

	@param pstrm Pointer to an IStream object for output.
----------------------------------------------------------------------------------------------*/
// ENHANCE: check for internal structure, such as pictures or interlinear text.
void VwParagraphBox::WriteWpxText(IStream * pstrm)
{
	AssertPtr(pstrm);
	AssertPtr(m_qts);

	ILgWritingSystemFactoryPtr qwsf;
	GetWritingSystemFactory(&qwsf);

	FormatToStream(pstrm, "  <StTxtPara>%n");
	FormatToStream(pstrm, "    <StyleRules1002>%n");
	ITsTextPropsPtr qttp;
	CheckHr(m_qzvps->get_TextProps(&qttp));
	qttp->WriteAsXml(pstrm, qwsf, 6);
	FormatToStream(pstrm, "    </StyleRules1002>%n");
	FormatToStream(pstrm, "    <Contents1003>%n");
	FormatToStream(pstrm, "      <Str>");

	LgCharRenderProps chrpPara;
	int nVal;
	int nVar;
	chrpPara.clrBack = 0;
	chrpPara.clrFore = 0;
	chrpPara.ttvBold = 0;
	chrpPara.ttvItalic = 0;
	chrpPara.ssv = 0;
	chrpPara.fWsRtl = 0;
	chrpPara.nDirDepth = 0;
	chrpPara.dympHeight = 0;
	chrpPara.dympOffset = 0;
	HRESULT hr = qttp->GetIntPropValues(ktptWs, &nVar, &nVal);
	if (hr == S_OK)
	{
		chrpPara.ws = nVal;
	}
	hr = qttp->GetIntPropValues(ktptBackColor, &nVar, &nVal);
	if (hr == S_OK)
		chrpPara.clrBack = nVal;
	hr = qttp->GetIntPropValues(ktptForeColor, &nVar, &nVal);
	if (hr == S_OK)
		chrpPara.clrFore = nVal;
	hr = qttp->GetIntPropValues(ktptBold, &nVar, &nVal);
	if (hr == S_OK)
		chrpPara.ttvBold = nVal;
	hr = qttp->GetIntPropValues(ktptItalic, &nVar, &nVal);
	if (hr == S_OK)
		chrpPara.ttvItalic = nVal;
	hr = qttp->GetIntPropValues(ktptSuperscript, &nVar, &nVal);
	if (hr == S_OK)
		chrpPara.ssv = (byte)nVal;
	hr = qttp->GetIntPropValues(ktptRightToLeft, &nVar, &nVal);
	if (hr == S_OK)
		chrpPara.fWsRtl = (byte)nVal;
	hr = qttp->GetIntPropValues(ktptDirectionDepth, &nVar, &nVal);
	if (hr == S_OK)
		chrpPara.nDirDepth = nVal;
	hr = qttp->GetIntPropValues(ktptFontSize, &nVar, &nVal);
	if (hr == S_OK)
		chrpPara.dympHeight = nVal;
	hr = qttp->GetIntPropValues(ktptOffset, &nVar, &nVal);
	if (hr == S_OK)
		chrpPara.dympOffset = nVal;
	StrUniBuf stubFont;
	SmartBstr sbstr;
	hr = qttp->GetStrPropValue(ktptFontFamily, &sbstr);
	if (hr == S_OK)
		stubFont.Assign(sbstr.Chars(), sbstr.Length());
	int cchSeg;
	CheckHr(m_qts->get_Length(&cchSeg));
	Vector<OLECHAR> vch;
	vch.Resize(cchSeg);
	CheckHr(m_qts->Fetch(0, cchSeg, vch.Begin()));
	int ich;
	int ichMinChar;
	int ichLimChar;
	LgCharRenderProps chrp;
	StrUniBuf stub;
	for (ich = 0; ich < cchSeg; ich = ichLimChar)
	{
		CheckHr(m_qts->GetCharProps(ich, &chrp, &ichMinChar, &ichLimChar));
		Assert(ichMinChar <= ich);
		Assert(ichLimChar > ich);
		FormatToStream(pstrm, "<Run");
		FwXml::WriteIntTextProp(pstrm, qwsf, ktptWs, ktpvDefault, chrp.ws);
		if (chrp.clrBack != chrpPara.clrBack)
			FwXml::WriteIntTextProp(pstrm, qwsf, ktptBackColor, 0, chrp.clrBack);
		if (chrp.clrFore != chrpPara.clrFore)
			FwXml::WriteIntTextProp(pstrm, qwsf, ktptForeColor, 0, chrp.clrFore);
		if (chrp.ttvBold != chrpPara.ttvBold)
			FwXml::WriteIntTextProp(pstrm, qwsf, ktptBold, 0, chrp.ttvBold);
		if (chrp.ttvItalic != chrpPara.ttvItalic)
			FwXml::WriteIntTextProp(pstrm, qwsf, ktptItalic, 0, chrp.ttvItalic);
		if (chrp.ssv != chrpPara.ssv)
			FwXml::WriteIntTextProp(pstrm, qwsf, ktptSuperscript, 0, chrp.ssv);
		if (chrp.fWsRtl != chrpPara.fWsRtl)
			FwXml::WriteIntTextProp(pstrm, qwsf, ktptRightToLeft, 0, chrp.fWsRtl ? 1 : 0);
		if (chrp.nDirDepth != chrpPara.nDirDepth)
			FwXml::WriteIntTextProp(pstrm, qwsf, ktptDirectionDepth, 0, chrp.nDirDepth);
		if (chrp.dympHeight != chrpPara.dympHeight)
			FwXml::WriteIntTextProp(pstrm, qwsf, ktptFontSize, ktpvMilliPoint, chrp.dympHeight);
		if (chrp.dympOffset != chrpPara.dympOffset)
			FwXml::WriteIntTextProp(pstrm, qwsf, ktptOffset, ktpvMilliPoint, chrp.dympOffset);
		stub.Assign(chrp.szFaceName);
		if (stub != stubFont)
			FwXml::WriteStrTextProp(pstrm, ktptFontFamily, stub.Bstr());
		FormatToStream(pstrm, "/>");
		WriteXmlUnicode(pstrm, vch.Begin() + ich, ichLimChar - ich);
	}

	FormatToStream(pstrm, "</Str>%n");
	FormatToStream(pstrm, "    </Contents1003>%n");
	FormatToStream(pstrm, "  </StTxtPara>%n");
}


/*----------------------------------------------------------------------------------------------
	Draw the borders, and fill the interior with the background color. Paragraphs override
	to produce the special MS-Word behavior of filling the space between paragraphs if they
	have identical properties.
	If we ever have a non-MS-Word mode this special behavior should be disabled in that mode.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::DrawBorder(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{

	if (BorderTop() || BorderBottom() ||
		BorderLeading() || BorderTrailing() ||
		m_qzvps->BackColor() != kclrTransparent)
	{
		// Margin thicknesses
		int dysMTop = MulDiv(m_qzvps->MarginTop(), rcSrc.Height(), kdzmpInch);
		int dysMBottom = MulDiv(m_qzvps->MarginBottom(), rcSrc.Height(), kdzmpInch);
		int dxsMLeft = MulDiv(m_qzvps->MarginLeading(), rcSrc.Width(), kdzmpInch);
		int dxsMRight = MulDiv(m_qzvps->MarginTrailing(), rcSrc.Width(), kdzmpInch);
		if (m_fParaRtl)
		{
			int t = dxsMLeft;
			dxsMLeft = dxsMRight;
			dxsMRight = t;
		}

		// outside of border rectangle
		int ydTopBord = rcSrc.MapYTo(dysMTop + m_ysTop, rcDst);
		int ydBottomBord = rcSrc.MapYTo(m_ysTop + m_dysHeight - dysMBottom, rcDst);
		int xdLeftBord = rcSrc.MapXTo(dxsMLeft + m_xsLeft, rcDst);
		int xdRightBord = rcSrc.MapXTo(m_xsLeft + m_dxsWidth - dxsMRight, rcDst);

		// Border thickness in twips.
		int dysTopBord = MulDivFixZero(this->BorderTop(), rcSrc.Height(), kdzmpInch);
		int dysBottomBord = MulDivFixZero(this->BorderBottom(), rcSrc.Height(), kdzmpInch);
		int dxsLeftBord = MulDivFixZero(this->BorderLeading(), rcSrc.Width(), kdzmpInch);
		int dxsRightBord = MulDivFixZero(this->BorderTrailing(), rcSrc.Width(), kdzmpInch);
		if (m_fParaRtl)
		{
			int t = dxsLeftBord;
			dxsLeftBord = dxsRightBord;
			dxsRightBord = t;
		}

		// Thickness of border. Measure in dest coords, so that the same source
		// thickness always comes out the same drawing thickness.
		int dydTopBord = MulDiv(dysTopBord, rcDst.Height(), rcSrc.Height());
		int dydBottomBord = MulDiv(dysBottomBord, rcDst.Height(), rcSrc.Height());
		int dxdLeftBord = MulDiv(dxsLeftBord, rcDst.Width(), rcSrc.Width());
		int dxdRightBord = MulDiv(dxsRightBord, rcDst.Width(), rcSrc.Width());

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

		// This is the special bit for paragraph boxes (a special bit mainly for string boxes
		// has been omitted). It adjusts things to make the side borders and background meet
		// up if two adjacent paragraphs have the same style.
		VwGroupBox * pvgboxCont = Container();
		if (pvgboxCont && (dxsLeftBord || dxsRightBord ||
			m_qzvps->BackColor() != kclrTransparent))
		{
			VwBox * pboxPrev = pvgboxCont->RealBoxBefore(this);
			if (pboxPrev && pboxPrev->Style() == Style())
			{
				// This is tricky. We want to draw a border right up to the bottom of the
				// previous box. Our own top includes our MarginTop, but may not include
				// all of our MswMarginTop...The most reliable way to make it meet up
				// exactly with the previous box is to compute the bottom of that box.
				Rect rcSrcPrev = rcSrc;
				ydTopBord = rcSrcPrev.MapYTo(pboxPrev->Top() + pboxPrev->Height(), rcDst);
				ydTopPad = ydTopBord; // no border width in this special case.
			}
			VwBox * pboxNext = NextRealBox();
			if (pboxNext && pboxNext->Style() == Style())
			{
				// Change things affected by dysMBottom as if it were zero.
				ydBottomBord = rcSrc.MapYTo(m_ysTop + m_dysHeight, rcDst);
				ydBottomPad = ydBottomBord - dydBottomBord;
			}
		}

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

StrUni VwParagraphBox::Description()
{
	int cch;
	CheckHr(m_qts->get_Length(&cch));
	StrUni stuResult;
	OLECHAR * prgch;
	stuResult.SetSize(cch, &prgch);
	CheckHr(m_qts->Fetch(0, cch, prgch));
	return stuResult;
}

/*----------------------------------------------------------------------------------------------
	Find a writing system factory if you can.  You better can!!
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::GetWritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	AssertPtr(ppwsf);
	*ppwsf = NULL;
	// First possible source: the VwTxtSrc object.
	if (m_qts)
		m_qts->GetWritingSystemFactory(ppwsf);
	if (!*ppwsf)
	{
		if (Root())
		{
			ISilDataAccessPtr qsda;
			qsda = Root()->GetDataAccess();
			if (qsda)
				CheckHr(qsda->get_WritingSystemFactory(ppwsf));
		}
	}
	AssertPtr(*ppwsf);
}

/*----------------------------------------------------------------------------------------------
	Find the child box that corresponds to the specified string index. This is doable
	if there is no string for that index: it is the nth child box that is not 'from TsString',
	where n is the number of items in our text source with null tss.
	If there IS a string at that index, return NULL.
----------------------------------------------------------------------------------------------*/
VwBox * VwParagraphBox::ChildAtStringIndex(int itssTarget)
{
	ITsStringPtr qtssT;
	Source()->StringAtIndex(itssTarget, &qtssT);
	if (qtssT)
	{
		return NULL; // there isn't an independent child at that index.
	}
	// Count how many previous text source items are not strings.
	int cNonTssPrev = 0;
	for (int itss = 0; itss < itssTarget; ++itss)
	{
		Source()->StringAtIndex(itss, &qtssT);
		if (!qtssT)
			cNonTssPrev ++;
	}
	int itss2 = 0;
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		if (pbox->IsBoxFromTsString())
			continue;
		if (itss2 == itssTarget)
			return pbox;
		itss2++;
	}
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	For a paragraph, if there are no inner piles answer (1,1). If there are inner piles we
	want the line count to be the max of all their line counts. The column count should be
	the sum of all the inner pile column counts, plus one for each non-empty range between
	inner piles.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::CountColumnsAndLines(int * pcCol, int * pcLines)
{
	int cLineMax = 1;
	int cColTotal = 0;
	VwInnerPileBox * pipboxPrev = NULL;
	for (int itss = 0; itss < Source()->CStrings(); itss++)
	{
		VwInnerPileBox * pipbox = dynamic_cast<VwInnerPileBox *>(ChildAtStringIndex(itss));
		if (pipbox != NULL)
		{
			int cCol, cLine;
			pipbox->CountColumnsAndLines(&cCol, &cLine);
			if (cLine > cLineMax)
				cLineMax = cLine;
			cColTotal += cCol;
		}
		else
		{
			// count the very first non-inner pile, and any that immediately follow inner piles.
			if (pipboxPrev != NULL || cColTotal == 0)
				cColTotal++;
		}
		pipboxPrev = pipbox;
	}
	*pcCol = cColTotal;
	*pcLines = cLineMax;
}

/*----------------------------------------------------------------------------------------------
	Given a child (non-string) box, find the corresponding string index.
----------------------------------------------------------------------------------------------*/
int VwParagraphBox::StringIndexOfChildBox(VwBox * pboxTarget)
{
	// count how many previous boxes are not generated from TsStrings.
	int cprevEmbedded = 0;
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		if (pbox->IsBoxFromTsString())
			continue;
		if (pbox == pboxTarget)
			break;
		cprevEmbedded++;
	}

	int citem = Source()->CStrings();
	int cNonTssPrev = 0;
	for (int itss = 0; itss < citem; ++itss)
	{
		ITsStringPtr qtssT;
		Source()->StringAtIndex(itss, &qtssT);
		if (!qtssT)
		{
			if (cNonTssPrev == cprevEmbedded)
				return itss;
			cNonTssPrev++;
		}
	}
	Assert(false);
	return citem;
}

/*----------------------------------------------------------------------------------------------
	Paragraph produces its lines.
----------------------------------------------------------------------------------------------*/
void VwParagraphBox::GetPageLines(IVwGraphics * pvg, PageLineVec & vln)
{
	VwBox * pbox = FirstBox();

	// Cycle through the lines (pbox is always the first box on a line)
	while (pbox)
	{
		PageLine ln;
		ln.pboxFirst = pbox;
		Rect rcSrc(0,0,1,1); //Identity transform
		// This will get the top and bottom y position based up the box passed in.
		GetLineTopAndBottom(pvg, pbox, &ln.ypTopOfLine, &ln.ypBottomOfLine, rcSrc, rcSrc, true, &ln.pboxLast);

		// For the first line we want to include the top margin of the whole paragaph.
		// This helps to keep things in differnt columns aligned.
		if (pbox == m_pboxFirst)
		{
			int dpiY;
			CheckHr(pvg->get_YUnitsPerInch(&dpiY));
			ln.ypTopOfLine -= TotalTopMargin(dpiY);
		}
		vln.Push(ln);

		// Advance to start of next line if any.
		pbox = ln.pboxLast->NextOrLazy();
	}
}


/*----------------------------------------------------------------------------------------------
	This is part of the logic of VwPileBbox::FindNonPileChildAtOffset, which tries to find its first
	non-pile box which extends beyond dysPosition (not counting its bottom margin). This
	routine is called when a paragraph crosses the dysPosition passed to its container.
	Usually, it does the same as the VwBox routine: answer itself and dysPosition. But in
	certain cases, for example, when a child box has a bottom margin or when there is a
	large line spacing, it may be that there is white space between the lowest box in the
	paragraph and the top of its own bottom margin. In such cases, this routine may be called
	with dysPostion >= the bottom of the lowest contained box. If this happens it must
	return null, indicating that it doesn't really extend beyond the specified position.
	(This is typically used for page breaking...the idea is that if this paragraph will
	fit on the page which ends at dysPosition, it should return null.)
----------------------------------------------------------------------------------------------*/
VwBox * VwParagraphBox::FindNonPileChildAtOffset (int dysPosition, int dpiY, int * pdysOffsetIntoBox)
{
		int ysBottomNoMargins = INT_MIN;
		for (VwBox * pbox = FirstBox();
			pbox && ysBottomNoMargins <= dysPosition;
			pbox = pbox->NextRealBox())
		{
			int ysBoxBottomNoMargins = pbox->Bottom() - MulDiv(pbox->MarginBottom(), dpiY, kdzmpInch);
			ysBottomNoMargins = std::max(ysBottomNoMargins, ysBoxBottomNoMargins);
		}
		if (dysPosition >= ysBottomNoMargins)
			return NULL;
		*pdysOffsetIntoBox = dysPosition;
		return this;
}

/*----------------------------------------------------------------------------------------------
	Initialize the member variables. Pass needed info on to text source.
----------------------------------------------------------------------------------------------*/
void VwConcParaBox::Init(int ichMinItem, int ichLimItem, int dmpAlign, VwConcParaOpts cpo)
{
	m_ichMinItem = ichMinItem;
	m_ichLimItem = ichLimItem;
	m_cpo = cpo;
	m_dmpAlign = dmpAlign;

	bool fBold = m_cpo & kcpoBold;
	VwConcTxtSrc * pcts = dynamic_cast<VwConcTxtSrc *>(m_qts.Ptr());
	Assert(pcts);
	pcts->Init(ichMinItem, ichLimItem, fBold);
}

/*----------------------------------------------------------------------------------------------
	Lay out the paragraph.
----------------------------------------------------------------------------------------------*/
void VwConcParaBox::DoLayout(IVwGraphics * pvg, int dxAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
	// Lay it out as if in unlimited width (but use half of INT_MAX to guard against overflow)
	SuperClass::DoLayout(pvg, INT_MAX / 2, INT_MAX / 2, fSyncTops);
	DoSpecialAlignment(pvg);
}

/*----------------------------------------------------------------------------------------------
	Lay out part of the paragraph. VwConcParaBox can't do this; convert to full layout.
----------------------------------------------------------------------------------------------*/
void VwConcParaBox::DoPartialLayout(IVwGraphics * pvg, VwBox * pboxStart, int cLinesToSave,
	int dyStart, int dyPrevDescent,
	int ichMinDiff, int ichLimDiff, int cchLenDiff)
{
	DoLayout(pvg, INT_MAX / 2, INT_MAX / 2);
}

/*----------------------------------------------------------------------------------------------
	Redo the layout of the paragraph.
----------------------------------------------------------------------------------------------*/
bool VwConcParaBox::Relayout(IVwGraphics * pvg, int dxAvailWidth, VwRootBox * prootb,
	FixupMap * pfixmap, int dxpAvailOnLine, BoxIntMultiMap * pmmbi)
{
	bool fRet = SuperClass::Relayout(pvg, INT_MAX / 2, prootb, pfixmap, INT_MAX / 2, pmmbi);
	if (fRet)
		DoSpecialAlignment(pvg);
	return fRet;
}

/*----------------------------------------------------------------------------------------------
	Do some special alignment calculations for the unlimited width.
----------------------------------------------------------------------------------------------*/
void VwConcParaBox::DoSpecialAlignment(IVwGraphics * pvg)
{
	if (m_cpo & kcpoAlign)
	{
		// For now we can only do align left.
		// The trick otherwise is not just to do our alignment, but to undo what the
		// superclass Layout did.
		Assert(m_qzvps->ParaAlign() == ktalLeft || m_qzvps->ParaAlign() == ktalLeading);
		// Figure out where the key word is naturally displayed.
		// First find the box it is part of.
		VwStringBox * psbox = NULL;
		VwConcTxtSrc * pcts = dynamic_cast<VwConcTxtSrc *>(Source());
		int ichMinItem = m_ichMinItem - pcts->m_cchDiscardInitial;
		int ichLimItem = m_ichLimItem - pcts->m_cchDiscardInitial;

		for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->Next())
		{
			VwStringBox * psbox2 = dynamic_cast<VwStringBox *>(pbox);
			if (!psbox2) continue;
			if (psbox2->IchMin() > ichMinItem)
			{
				// the key word starts in the previous string box...stop searching
				break;
			}
			psbox = psbox2; // we want to find the last string box in the right range
		}
		if (!psbox)
			return; // can't adjust, can't match index with string box
		int dichLim;
		int ichMinBox = psbox->IchMin();
		CheckHr(psbox->Segment()->get_Lim(ichMinBox, &dichLim));
		if (ichMinBox + dichLim <= ichMinItem)
			return; // item isn't in a string box, can't align

		// Figure item position, adjust all boxes to align.
		int rgxdLefts[kMaxUnderlineSegs];
		int rgxdRights[kMaxUnderlineSegs];
		int rgydTops[kMaxUnderlineSegs];
		int cxd; // # segments of underlining
		int dxsInch, dysInch;
		pvg->get_XUnitsPerInch(&dxsInch);
		pvg->get_YUnitsPerInch(&dysInch);
		Rect rc(0, 0, dxsInch, dysInch);
		try
		{
			CheckHr(psbox->Segment()->GetCharPlacement(ichMinBox, pvg, ichMinItem,
			min(ichLimItem, ichMinBox + dichLim),
			rc, rc, true, kMaxUnderlineSegs, &cxd,
			rgxdLefts, rgxdRights, rgydTops));
		}
		catch (Throwable& thr)
		{
			WarnHr(thr.Error());
			return; // ignore any problems here, just don't align.
		}
		if (cxd < 1)
		{
			Warn("Can't align in DoSpecialAlignment.");
			return; // ignore any problems here, just don't align.
		}

		int dxsAlign = rgxdLefts[0] + psbox->Left();
		int dxsGoalAlign = MulDiv(m_dmpAlign, dxsInch, kdzmpInch);
		int dxsAdjust = dxsGoalAlign - dxsAlign;
		// Now adjust all the box lefts by this amount
		for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->Next())
		{
			pbox->Left(pbox->Left() + dxsAdjust);
		}
	}
}

#include "Vector_i.cpp"
template class Vector<VwParagraphBox::TagInfo>;
template class Vector<int *>;
//template Vector<VwParagraphBox::MenuInfo>;

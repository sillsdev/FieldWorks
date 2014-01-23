/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwInvertedRootBox.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description: Implementations of override methods for inverted views.

-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Methods
//:>********************************************************************************************

VwInvertedRootBox::VwInvertedRootBox(VwPropertyStore * pzvps)
	: VwRootBox(pzvps)
{
}

// Protected default constructor used for CreateCom
VwInvertedRootBox::VwInvertedRootBox() : VwRootBox()
{
}


VwInvertedRootBox::~VwInvertedRootBox()
{
}

static GenericFactory g_fact(
	_T("SIL.Views.VwInvertedRootBox"),
	&CLSID_VwInvertedRootBox,
	_T("SIL Inverted Root Box"),
	_T("Apartment"),
	&VwInvertedRootBox::CreateCom);

void VwInvertedRootBox::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwInvertedRootBox> qrootb;
	qrootb.Attach(NewObj VwInvertedRootBox());		// ref count initialy 1
	CheckHr(qrootb->QueryInterface(riid, ppv));
}

/*----------------------------------------------------------------------------------------------
	Construct the right sort of VwEnv. Typical usage is
	VwEnvPtr qvwenv;
	qvwenv->Attach(MakeEnv());
----------------------------------------------------------------------------------------------*/
VwEnv * VwInvertedRootBox::MakeEnv()
{
	return NewObj(VwInvertedEnv);
}


// Dummy factory for END_COM_METHOD macro.
static DummyFactory dfactEnv(_T("Sil.Views.VwInvertedEnv"));


VwInvertedEnv::VwInvertedEnv() : VwEnv()
{
}

VwInvertedEnv::~VwInvertedEnv()
{
}

/*----------------------------------------------------------------------------------------------
	warn of use of currently unsupported types of box.
----------------------------------------------------------------------------------------------*/

STDMETHODIMP VwInvertedEnv::OpenMappedPara()
{
	BEGIN_COM_METHOD;
	Assert(false); // "This type of box is not yet supported in inverted views");
	ThrowHr(WarnHr(E_NOTIMPL));
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}
STDMETHODIMP VwInvertedEnv::OpenConcPara(int ichMinItem, int ichLimItem, VwConcParaOpts cpoFlags,
								int dmpAlign)
{
	BEGIN_COM_METHOD;
	Assert(false); // "This type of box is not yet supported in inverted views");
	ThrowHr(WarnHr(E_NOTIMPL));
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}
STDMETHODIMP VwInvertedEnv::OpenOverridePara(int cOverrideProperties,
									 DispPropOverride *prgOverrideProperties)
{
	BEGIN_COM_METHOD;
	Assert(false); // "This type of box is not yet supported in inverted views");
	ThrowHr(WarnHr(E_NOTIMPL));
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}
STDMETHODIMP VwInvertedEnv::OpenInnerPile()
{
	BEGIN_COM_METHOD;
	Assert(false); // "This type of box is not yet supported in inverted views");
	ThrowHr(WarnHr(E_NOTIMPL));
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}
STDMETHODIMP VwInvertedEnv::OpenTable(int ccolm, VwLength vlenWidth, int mpBorder,
	VwAlignment vwalign, VwFramePosition frmpos, VwRule vwrule,
	int mpSpacing, int mpPadding, ComBool fSelectOneCol)
{
	BEGIN_COM_METHOD;
	Assert(false); // "This type of box is not yet supported in inverted views");
	ThrowHr(WarnHr(E_NOTIMPL));
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}
STDMETHODIMP VwInvertedEnv::AddLazyVecItems(int tag, IVwViewConstructor * pvvc, int frag)
{
	BEGIN_COM_METHOD;
	Assert(false); // "This type of box is not yet supported in inverted views");
	ThrowHr(WarnHr(E_NOTIMPL));
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

VwParagraphBox * VwInvertedEnv::MakeParagraphBox(VwSourceType vst)
{
	return NewObj VwInvertedParaBox(m_qzvps, vst);
}

VwPropertyStore * VwInvertedEnv::MakePropertyStore()
{
	return NewObj VwInvertedPropertyStore();
}

VwInvertedPropertyStore::VwInvertedPropertyStore() : VwPropertyStore()
{
}

VwInvertedPropertyStore::~VwInvertedPropertyStore()
{
}
/*----------------------------------------------------------------------------------------------
	Make a div box (overridden in VwInvertedEnv to make an inverted one)
----------------------------------------------------------------------------------------------*/
VwDivBox * VwInvertedEnv::MakeDivBox()
{
	return NewObj VwInvertedDivBox(m_qzvps);
}
/*----------------------------------------------------------------------------------------------
	constructor for inverted para box.
----------------------------------------------------------------------------------------------*/
VwInvertedParaBox::VwInvertedParaBox(VwPropertyStore * pzvps, VwSourceType vst)
: VwParagraphBox(pzvps, vst)
{
}

/*----------------------------------------------------------------------------------------------
	destructor.
----------------------------------------------------------------------------------------------*/
VwInvertedParaBox::~VwInvertedParaBox()
{
}

/*----------------------------------------------------------------------------------------------
	Computes (by being called for each box on a line in turn) the boundary of the current
	line (the starting point for laying out the following line) as well as the offset of this
	line from the previous one. These values are used to initialize partial paragraph
	layout. In a normal paragraph, they are basically the bottom and descent of the boxes
	on the current line. In an inverted paragraph, they are the top and minus the ascent.
----------------------------------------------------------------------------------------------*/
void VwInvertedParaBox::AdjustLineBoundary(bool fExactLineHeight, int & dyBoundaryCurr,
	int & dyBaselineOffsetCurr, VwBox * pboxTmp, int dypLineHeight, int dypInch)
{
	dyBoundaryCurr = std::min(dyBoundaryCurr, pboxTmp->Top());
	dyBaselineOffsetCurr = dyBoundaryCurr - (pboxTmp->Top() + pboxTmp->Ascent());
}

/*----------------------------------------------------------------------------------------------
	constructor for inverted div box.
----------------------------------------------------------------------------------------------*/
VwInvertedDivBox::VwInvertedDivBox(VwPropertyStore * pzvps)
: VwDivBox(pzvps)
{
}

/*----------------------------------------------------------------------------------------------
	destructor.
----------------------------------------------------------------------------------------------*/
VwInvertedDivBox::~VwInvertedDivBox()
{
}

/*----------------------------------------------------------------------------------------------
	Compute where the top of the box after pboxPrev (if any) should go, assuming pboxPrev to
	be in its correct place. In an inverted pile, it goes ABOVE the previos box.

	The height of a box includes its margins. Therefore, proxPrev->Top() - pboxCurr->Height()
	gives the position we would put it if the space between the boxes were to be
	proxPrev->TopMargin() + pboxCurr->BottomMargin().

	But that's wrong. The natural gap between boxes in a pile is the max of the two margins.
	So we compute

	dympGapNatural = max(pboxPrev->TopMargin(), proxCurr->BottomMargin()).

	And then, there's the complication of the alternative MSW top margin. In the inverted
	pile, that's reinterpreted as a bottom margin, giving an alternative minimum separation

	dympGapMsw = pboxPrev->TopMargin() + pboxCurr->MswMarginTop().

	Then we want the max of those two gaps, and then, we have to adjust for the margins
	currently wrongly included.

	If there's no next box, we basically want the top of pboxPrev.
----------------------------------------------------------------------------------------------*/
int VwInvertedDivMethods::ComputeTopOfBoxAfter(VwBox * pboxPrev, int dypInch)
{
	//int dympBottomPrev = pzvpsPrev->MarginBottom();
	VwBox * pboxCurr = pboxPrev->NextOrLazy();
	if (pboxCurr)
	{
		// Figure the correction to make the gap between the boxes correct
		VwPropertyStore * pzvpsPrev = pboxPrev->Style();
		int dympTopPrev = pzvpsPrev->MarginTop();
		VwPropertyStore * pzvpsCurr = pboxCurr->Style();
		int dympMswTopCurr = pzvpsCurr->MswMarginTop();
		int dympBottomCurr = pzvpsCurr->MarginBottom();
		int dympGapNatural = std::max(dympTopPrev, dympBottomCurr);
		int dympGapMsw = dympTopPrev + dympMswTopCurr;
		int dympGapDesired = std::max(dympGapMsw, dympGapNatural);
		int dympGapDefault = dympTopPrev + dympBottomCurr;

		return pboxPrev->Top() - pboxCurr->Height() + MulDiv(dympGapDefault - dympGapDesired, dypInch, kdzmpInch);
	}
	else
	{
		return pboxPrev->Top();
	}
}
/*----------------------------------------------------------------------------------------------
	Compute the Y position of the first box, based on the current height of this one.
	A first approximation is the GapBottom of this box above its height (that is, its bottom
	relative to its top).
	If there's a first box at all, reduce this by its height.
	But, in addition to that, we have to make a special adjustment if the first box exists
	and has an MswMarginTop greater than its MarginBottom. In that case, it must be moved up
	by the difference between the two.
----------------------------------------------------------------------------------------------*/
int VwInvertedDivMethods::FirstBoxTopY(int dypInch)
{
	int ypPos = m_pdbox->Height() - m_pdbox->GapBottom(dypInch);
	if (!m_pdbox->m_pboxFirst)
		return ypPos;
	ypPos -= m_pdbox->m_pboxFirst->Height();
	VwPropertyStore * pzvps = m_pdbox->m_pboxFirst->Style();
	int dmpMswTop = pzvps->MswMarginTop();
	int dmpBottom = pzvps->MarginBottom();
	if (dmpBottom >= dmpMswTop)
		return ypPos;
	return ypPos - MulDiv(dmpMswTop - dmpBottom, dypInch, kdzmpInch);
}

VwInvertedDivMethods::VwInvertedDivMethods(VwDivBox * pdbox)
{
	m_pdbox = pdbox;
}
/*----------------------------------------------------------------------------------------------
	Adjusts the inner box positions and the size of the container.
----------------------------------------------------------------------------------------------*/
void VwInvertedDivBox::AdjustInnerBoxes(IVwGraphics* pvg, VwSynchronizer * psync, BoxIntMultiMap * pmmbi,
	VwBox * pboxFirstNeedingInvalidate, VwBox * pboxLastNeedingInvalidate, bool fDoInvalidate)
{
	VwInvertedDivMethods idm(this);
	idm.AdjustInnerBoxes(pvg, psync, pmmbi, pboxFirstNeedingInvalidate, pboxLastNeedingInvalidate, fDoInvalidate);
}

void VwInvertedRootBox::AdjustInnerBoxes(IVwGraphics* pvg, VwSynchronizer * psync, BoxIntMultiMap * pmmbi,
	VwBox * pboxFirstNeedingInvalidate, VwBox * pboxLastNeedingInvalidate, bool fDoInvalidate)
{
	VwInvertedDivMethods idm(this);
	idm.AdjustInnerBoxes(pvg, psync, pmmbi, pboxFirstNeedingInvalidate, pboxLastNeedingInvalidate, fDoInvalidate);
}

void VwInvertedDivMethods::AdjustInnerBoxes(IVwGraphics* pvg, VwSynchronizer * psync, BoxIntMultiMap * pmmbi,
	VwBox * pboxFirstNeedingInvalidate, VwBox * pboxLastNeedingInvalidate, bool fDoInvalidate)
{
	Assert(psync == NULL); // sync'd not yet supported.
	int dxpInch;
	CheckHr(pvg->get_XUnitsPerInch(&dxpInch));
	int dypInch;
	CheckHr(pvg->get_YUnitsPerInch(&dypInch));
	int dxpSurroundWidth = m_pdbox->SurroundWidth(dxpInch);
	int clinesMax = m_pdbox->m_qzvps->MaxLines();
	int clines = 0;
	int xpPos = m_pdbox->GapLeft(dxpInch); // left of all boxes typically goes here
	int dxpInnerWidth = 0;

	VwBox * pboxCurr = m_pdbox->m_pboxFirst;
	VwRootBox * prootb = m_pdbox->Root();
	int ypPos = FirstBoxTopY(dypInch); // top of first box goes here

	for (; pboxCurr && clines < clinesMax; pboxCurr = pboxCurr->NextOrLazy())
		dxpInnerWidth = std::max(dxpInnerWidth, pboxCurr->Width());

	bool fInInvalidateRange = false;
	for (pboxCurr = m_pdbox->m_pboxFirst; pboxCurr && clines < clinesMax; pboxCurr = pboxCurr->NextOrLazy())
	{
		if (pboxCurr == pboxFirstNeedingInvalidate)
			fInInvalidateRange = true;

		bool fNeedInvalidate = fInInvalidateRange; // so far just based on own Relayout
		m_pdbox->AdjustLeft(pboxCurr, xpPos, dxpInnerWidth);
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

		ypPos = ComputeTopOfBoxAfter(pboxCurr, dypInch);
		clines += pboxCurr->CLines();

		if (pboxCurr == pboxLastNeedingInvalidate)
			fInInvalidateRange = false;
	}
	VwBox * pboxFirstTruncated = pboxCurr; // typically null
	while (pboxCurr)
	{
		// There are boxes we are not using, because of the max line count. Give them a bizarre
		// Top position.
		pboxCurr->Top(knTruncated);
		pboxCurr = pboxCurr->NextOrLazy();
	}
	// Now ypPos is where we would put the next box (if there was another). But our actual height
	// is greater by the top gap
	int dysHeight = m_pdbox->Height() - ypPos + m_pdbox->GapTop(dypInch);
	m_pdbox->m_dxsWidth = dxpInnerWidth + dxpSurroundWidth;
	if (dysHeight != m_pdbox->m_dysHeight)
	{
		// Our height changed; everything moves again! Also everything needs painting.
		m_pdbox->Invalidate(); // old size and position.
		int dysMove = dysHeight - m_pdbox->m_dysHeight;
		for (pboxCurr = m_pdbox->m_pboxFirst; pboxCurr != pboxFirstTruncated; pboxCurr = pboxCurr->NextOrLazy())
			pboxCurr->Top(pboxCurr->Top() + dysMove);
		m_pdbox->m_dysHeight = dysHeight;
		m_pdbox->Invalidate(); // still old position, but new size; container will invalidate new position if moved.
	}
}

// Answer the bottom of the box following pbox, or if nothing follows it, its own top.
// Makes sense only for inverted piles, where the next box is above this one.
int BottomOfNextBox(VwBox * pbox)
{
	if (pbox->NextOrLazy())
		return pbox->NextOrLazy()->Bottom();
	else
		return pbox->Top();
}

void VwInvertedDivBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTopOfPage,
	int dysPageHeight, bool fDisplayPartialLines)
{
	VwInvertedDivMethods idm(this);
	idm.DrawForeground(pvg, rcSrc, rcDst, ysTopOfPage, dysPageHeight, fDisplayPartialLines);
}
void VwInvertedRootBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTopOfPage,
	int dysPageHeight, bool fDisplayPartialLines)
{
	VwInvertedDivMethods idm(this);
	idm.DrawForeground(pvg, rcSrc, rcDst, ysTopOfPage, dysPageHeight, fDisplayPartialLines);
}

void VwInvertedDivMethods::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTopOfPage,
	int dysPageHeight, bool fDisplayPartialLines)
{
#ifdef _DEBUG
#ifdef _DEBUG_SHOW_BOX
	DebugDrawBorder(pvg, rcSrc, rcDst);
#endif
#endif

	int left, top, right, bottom;
	CheckHr(pvg->GetClipRect(&left, &top, &right, &bottom));
	rcSrc.Offset(-m_pdbox->m_xsLeft, -m_pdbox->m_ysTop);

	// We won't bother drawing anything above the top or below the bottom of the clip
	// rectangle...except we allow an extra quarter inch, just in case some font draws
	// a bit outside its proper rectangle.
	//bottom -= rcDst.top;
	//top -= rcDst.top;
	int dydInch = rcDst.Height();
	bottom += dydInch / 4;
	top -= dydInch / 4;

	for (VwBox * pbox = m_pdbox->FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		if (pbox->Top() == knTruncated)
			return;
		// This test is designed to skip boxes before the ones we need to draw. Since we're
		// going up,
		// is this box before the bottom of the page we want to draw? If not don't draw it
		// Is the bottom of the NEXT box below the bottom of the clip rectangle? If so, don't draw
		// this one. (We consider the bottom of the NEXT box rather than the bottom of this,
		// because some boxes draw borders in the gap between the two boxes, so it's possible
		// if the bottom of the following box is visible that some of the border of this box is,
		// even though none of this box itself is.)
		if (rcSrc.MapYTo(BottomOfNextBox(pbox), rcDst) < bottom && pbox->Top() < ysTopOfPage)
		{
			// Adjust the top of page to be relative to this box. It's okay for this to go
			// negative because it represents the distance from the top of the box to the
			// top of the page and the top of the page could be above the top of this box.
			pbox->Draw(pvg, rcSrc, rcDst, ysTopOfPage - pbox->Top(), dysPageHeight, fDisplayPartialLines);
		}
		// This test truncates the loop when we're beyond what we're interested in drawing.

		// is this box after the top of the page we want to draw? If so don't draw any more.
		// This is important to prevent fragments of the next page's top line at the bottom of
		// a page. It must be precise and in source coords.

		// If this box extends above the top of the clip rect we can be sure we don't
		// need to draw any more boxes. Checking (before the draw) whether this box's bottom is
		// above the top is less reliable, because occasionally boxes (e.g., adjacent
		// paragraphs with borders and the same properties) draw in the space between them.
		// It's important to check this because, in a non-print-preview view, we have no known page
		// boundaries, so it is important to observe the clip rectangle or we will draw
		// everything, with bad consequences for performance.
		// Note that we don't use VisibleBottom() here. If pbox is a one-line drop-cap paragraph,
		// it's visible bottom (the bottom of the drop cap) may be below the bottom of the page/clip
		// rectangle, but we still want to draw the following paragraph, which overlaps the bottom
		// of the drop cap.
		if (rcSrc.MapYTo(pbox->Top(), rcDst) < top ||
			pbox->Top() <= ysTopOfPage + dysPageHeight)
		{
			return;
		}
	}
}
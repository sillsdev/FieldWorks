/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwSynchronizer.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This class synchronizes two views which contain the same sequence of objects by making
	the display of each object the same size in each view.
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

VwSynchronizer::VwSynchronizer()
{
	m_cref = 1;
	m_fStartedExpanding = false;
	m_fAlreadyExpandingItems = false;
	m_fSyncingTops = false;
	ModuleEntry::ModuleAddRef();
}

VwSynchronizer::~VwSynchronizer()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP VwSynchronizer::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwSynchronizer)
		*ppv = static_cast<IVwSynchronizer *>(this);
	else if (&riid == &CLSID_VwSynchronizer)
		*ppv = static_cast<VwSynchronizer *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<IVwSynchronizer *>(this),
			IID_IVwSynchronizer, IID_IVwNotifyChange);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Views.VwSynchronizer"),
	&CLSID_VwSynchronizer,
	_T("SIL View Synchronizer"),
	_T("Apartment"),
	&VwSynchronizer::CreateCom);


void VwSynchronizer::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwSynchronizer> qsync;
	qsync.Attach(NewObj VwSynchronizer());		// ref count initialy 1
	CheckHr(qsync->QueryInterface(riid, ppv));
}

//:>********************************************************************************************
//:>	IVwSynchronizer methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Add a root box to the synchronization set.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSynchronizer::AddRoot(IVwRootBox * prootb)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prootb);
	VwRootBoxPtr qrootb;

	// if we have expanded stuff then you can't add another rootbox
#if WIN32
	if (m_fStartedExpanding)
	{
		ThrowHr(E_UNEXPECTED);
	}
#else
	// TODO-Linux: Workaround - proberbly caused by datagridview events being wrong.
#endif

	CheckHr(prootb->QueryInterface(CLSID_VwRootBox, (void **)&qrootb));
	m_vrootb.Push(qrootb);
	qrootb->SetSynchronizer(this);
	END_COM_METHOD(g_fact, IID_IVwSynchronizer);
}

STDMETHODIMP VwSynchronizer::get_IsExpandingLazyItems(ComBool * fAlreadyExpandingItems)
{
	BEGIN_COM_METHOD;
	*fAlreadyExpandingItems = IsExpandingLazyItems();
	END_COM_METHOD(g_fact, IID_IVwSynchronizer);
}

/*----------------------------------------------------------------------------------------------
	Informs other synchronized roots that the source one has determined a new natural
	top-to-top for one of the objects it is displaying. dypTopToTopNatural distance from the
	top of the display of hvoObj to the top of whatever comes before it in the pile in the
	absence of synchronization. Returns the dypTopToTop that the display of the object should
	actually use in view of synchronization. Also informs the other boxes of the new actual
	dypTopToTop.
	@param prootbSrc The root box that originated the change.
	@param hvoObj the object that changed. Usually the client will call this twice for a
		change, once for the actual changed object, and once for the next one, as the change
		might affect either separation. (For example, if MarginTop changed, the position
		of hvoObj will move; if MarginBottom or the size of hvoObj changed, it will affect
		the following object.)
	@param dypTopToTopNatural The distance from the top of the display of hvoObj to the
		top of the previous box in the same pile, or to the top of the whole pile if it is
		the first thing in the pile. (The latter can be non-zero because marginTop is not
		considered part of the box.)
----------------------------------------------------------------------------------------------*/
int VwSynchronizer::SyncNaturalTopToTop(VwRootBox * prootbSrc, HVO hvoObj,
	int dypTopToTopNatural)
{
	//StrAnsi sta;
	//sta.Format("SyncNaturalTopToTop for %d\n", hvoObj);
	//Warn(sta.Chars());

	Assert(!m_fSyncingTops);
	m_fSyncingTops = true;
	int dypTopToTopActual = dypTopToTopNatural; // will become actual as we max with others.

	// The actual dypTopToTop is the max of the natural ones for all the roots. Since we already
	// know the natural value for prootbSrc, we save a little time (and perhaps some risk that
	// it is in an intermediate state where this call won't work) by using the value we were
	// passed for that box instead of calling NaturalTopToTop().
	// Note that we are retrieving the Natural values of each box; its current Actual layout
	// might be larger as a result of previous synchronizations.
	for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
	{
		if (dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr()) != prootbSrc)
		{
			dypTopToTopActual = std::max(dypTopToTopActual,
				dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr())->NaturalTopToTop(hvoObj));
		}
	}
	// Now notify each box (except prootbSrc, which will get the information via
	// dypTopToTopActual and may well be in the middle of another layout operation)
	// of the required actual TopToTop. Note that we don't try to suppress this notification
	// if it hasn't changed, mainly because we don't know here what the old actual TTT is.
	for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
	{
		if (dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr()) != prootbSrc)
		{
			dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr())->SetActualTopToTop(hvoObj, dypTopToTopActual);
		}
	}
	m_fSyncingTops = false;

	return dypTopToTopActual;
}

/*----------------------------------------------------------------------------------------------
	Informs other synchronized roots that the source one has determined a new natural
	top-to-top for one of the objects it is displaying. dypTopToTopNatural is the distance from
	the top of the display of hvoObj to the top of whatever comes AFTER it in the pile in the
	absence of synchronization. Returns the dypTopToTop that the display of the object should
	actually use in view of synchronization. Also informs the other boxes of the new actual
	dypTopToTop.
	@param prootbSrc The root box that originated the change.
	@param hvoObj the object that changed. Usually the client will call this twice for a
		change, once for the actual changed object, and once for the next one, as the change
		might affect either separation. (For example, if MarginTop changed, the position
		of hvoObj will move; if MarginBottom or the size of hvoObj changed, it will affect
		the following object.)
	@param dypTopToTopNatural The distance from the top of the display of hvoObj to the
		top of the following box in the same pile, or to where the following thing would go
		if there were one.
----------------------------------------------------------------------------------------------*/
int VwSynchronizer::SyncNaturalTopToTopAfter(VwRootBox * prootbSrc, HVO hvoObj,
	int dypTopToTopNatural)
{
	Assert(!m_fSyncingTops);
	m_fSyncingTops = true;
	int dypTopToTopActual = dypTopToTopNatural; // will become actual as we max with others.

	// The actual dypTopToTop is the max of the natural ones for all the roots. Since we already
	// know the natural value for prootbSrc, we save a little time (and perhaps some risk that
	// it is in an intermediate state where this call won't work) by using the value we were
	// passed for that box instead of calling NaturalTopToTop().
	// Note that we are retrieving the Natural values of each box; its current Actual layout
	// might be larger as a result of previous synchronizations.
	for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
	{
		if (dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr()) != prootbSrc)
		{
			dypTopToTopActual = std::max(dypTopToTopActual,
				dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr())->NaturalTopToTopAfter(hvoObj));
		}
	}
	// Now notify each box (except prootbSrc, which will get the information via
	// dypTopToTopActual and may well be in the middle of another layout operation)
	// of the required actual TopToTop. Note that we don't try to suppress this notification
	// if it hasn't changed, mainly because we don't know here what the old actual TTT is.
	for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
	{
		if (dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr()) != prootbSrc)
		{
			dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr())->SetActualTopToTopAfter(hvoObj, dypTopToTopActual);
		}
	}
	m_fSyncingTops = false;
	return dypTopToTopActual;
}

/*----------------------------------------------------------------------------------------------
	Verify that the tops of all other boxes that should be synchronized with hvoObj at position
	ypTop in root prootbSrc actually are.
----------------------------------------------------------------------------------------------*/
bool VwSynchronizer::VerifyCorrespondence(VwRootBox * prootbSrc, HVO hvoObj,
	VwBox * pboxSrc)
{
	bool fRetVal = true;
#ifdef _DEBUG
	StrAnsi sta;
	int ypTop = pboxSrc->Top();
	for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
	{
		if (dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr()) != prootbSrc)
		{
			VwRootBox * prootb = dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr());
			VwBox * pbox = prootb->GetBoxDisplaying(hvoObj);
			if (!pbox)
			{
				fRetVal = false; // GetBoxDisplaying reports error if appropriate
				continue;
			}
			if (pbox->Top() != ypTop)
			{
				sta.Format("box for %d has wrong top (%d instead of %d)\n", hvoObj,
					pbox->Top(), ypTop);
				Warn(sta.Chars());
				fRetVal = false;
			}
			if (pbox->NextOrLazy() == NULL)
			{
				if (pboxSrc->NextOrLazy() != NULL)
				{
					sta.Format("box for %d has following box but corresponding box doesn't\n", hvoObj,
						pbox->Top(), ypTop);
					Warn(sta.Chars());
					fRetVal = false;
				}
				continue;
			}
			if (pboxSrc->NextOrLazy() == NULL)
			{
				sta.Format("box for %d has following box but corresponding box doesn't\n", hvoObj,
					pbox->Top(), ypTop);
				Warn(sta.Chars());
				fRetVal = false;
				continue;
			}
			if (pbox->NextOrLazy()->Top() != pboxSrc->NextOrLazy()->Top())
			{
				sta.Format("box for %d next box has wrong top (%d instead of %d)\n", hvoObj,
					pbox->NextOrLazy()->Top(), pboxSrc->NextOrLazy()->Top());
				Warn(sta.Chars());
				fRetVal = false;
			}
		}
	}
#endif
	return fRetVal;
}

/*----------------------------------------------------------------------------------------------
	Returns true if a box can notify the view that its size has changed, false otherwise
----------------------------------------------------------------------------------------------*/
bool VwSynchronizer::OkToNotifyOfSizeChange()
{
	return !m_fSyncingTops && m_vLazyItemsInfo.Size() == 0;
}

/*----------------------------------------------------------------------------------------------
	Adjust any corresponding box heights to reflect their correct height based on the positions
	of their last boxes, which hopefully by now are in their correct synched locations.
----------------------------------------------------------------------------------------------*/
void VwSynchronizer::AdjustSyncedBoxHeights(VwBox * pbox, int dypInch)
{
	VwGroupBox * pboxContainer = pbox->Container();
	if (pboxContainer)
	{
		VwNotifier * pnote = pboxContainer->GetLowestNotifier(pbox);
		if (!pnote)
		{
			static OleStringLiteral name(L"<not set>");
			StrAnsi sta;
			sta.Format("Didn't find notifier for box: %x (%S)", pbox,
				pbox->Root()->m_stuAccessibleName ? pbox->Root()->m_stuAccessibleName.Chars() : name);
			Warn(sta.Chars());
			return;
		}

		HVO hvoSyncObj = pnote->Object();
		for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
		{
			// We want to set the height on ALL rootboxes, otherwise we might end up
			// with large portions of white space in one of the rootboxes.
			VwRootBox * prootb = dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr());
			VwPileBox* pboxDisplayingHvoSyncObj = dynamic_cast<VwPileBox *>(prootb->GetBoxDisplaying(hvoSyncObj));
			if (pboxDisplayingHvoSyncObj && pboxDisplayingHvoSyncObj->LastBox())
			{
				int newHeight = pboxDisplayingHvoSyncObj->LastBox()->VisibleBottom() +
					pboxDisplayingHvoSyncObj->GapBottom(dypInch);
				pboxDisplayingHvoSyncObj->_Height(newHeight);
			}
		}
	}
	else
	{
		for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
		{
			VwRootBox * prootb = dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr());
			if (prootb->LastBox())
				prootb->_Height(prootb->LastBox()->VisibleBottom() + prootb->GapBottom(dypInch));
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Informs other synchronized roots that the source one expanded items from ihvoMin to ihvoLim
	in a lazy box which was displaying property tag of object hvoContext as the iprop'th
	property.
----------------------------------------------------------------------------------------------*/
VwBox* VwSynchronizer::ExpandLazyItems(VwRootBox * prootbSrc, HVO hvoContext, int tag,
	int iprop, int ihvoMin, int ihvoLim, bool * pfForcedScroll, VwBox** ppboxFirstLayout,
	VwBox** ppboxLimLayout)
{
	// Can't add another rootbox after we started expanding
	m_fStartedExpanding = true;

	// Expanding the lazy box may cause another lazy box to get expanded, which results in a
	// recursive call here in a sync'd view. We put it in the list and deal with it after
	// the first expansion is done.
	m_vLazyItemsInfo.Push(ExpandLazyItemsInfo(prootbSrc, hvoContext, tag, iprop, ihvoMin,
		ihvoLim));

	VwBox* pRet = NULL;

	// If we are already expanding boxes, we just expand this one box, but don't lay it out
	// and don't sync with the other root boxes. We do that later when the original expansion
	// is finished.
	if (m_fAlreadyExpandingItems)
	{
		StrAnsi sta;
		sta.Format("Recursive ExpandLazyItems for rootbox %x, iprop %d, ihvoMin %d, ihvoLim %d", prootbSrc,
			iprop, ihvoMin, ihvoLim);
		Warn(sta.Chars());

		// Find index of source rootbox in vector
		int irootbSrc = -1;
		for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
		{
			if (dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr()) == prootbSrc)
				irootbSrc = irootb;
		}
		Assert(irootbSrc > -1);

		// Expand the lazy box for the source view only so that we can finish the pending
		// expansion (e.g. while calculating the margin when we have borders). The lay out
		// and adjusting boxes will be done later when the pending expansion is finished.
		pRet = ExpandLazyItemsNoLayoutOnRootb(prootbSrc, hvoContext, tag, iprop, ihvoMin,
			ihvoLim, irootbSrc, ppboxFirstLayout, ppboxLimLayout);
		return pRet;
	}
	m_fAlreadyExpandingItems = true;

	// We need to get the size of our rootbox before we lay out anything.
	Rect rcRootOld;
	{ // block for HoldGraphics
		HoldGraphics hg(prootbSrc);
		rcRootOld = prootbSrc->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
	}

	for (int i = 0; i < m_vrootb.Size(); i++)
	{
		m_vboxFirstLayout.Push(NULL);
		m_vboxLimLayout.Push(NULL);
		m_vboxContainer.Push(NULL);
		m_vTopBottomExpandedBoxes.Push(Rect());
	}

	int irootbSrc = -1;
	// Expand boxes and lay them out while there's something to do
	while (m_vLazyItemsInfo.Size() > 0)
	{
		// Expand lazy items while we have something to expand
		while (m_vLazyItemsInfo.Size() > 0)
		{
			ExpandLazyItemsInfo elii = m_vLazyItemsInfo[0];
			for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
			{
				VwRootBox* prootb = dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr());
				// Now expand the boxes for the root boxes we didn't already do in the if above
				// (and for the first time we come in here)
				if (prootb != elii.m_prootbSrc || m_vboxFirstLayout[irootb] == NULL)
				{
					//StrAnsi sta;
					//sta.Format("ExpandLazyItems for rootbox %x, iprop %d, ihvoMin %d, ihvoLim %d", dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr()),
					//	elii.m_iprop, elii.m_ihvoMin, elii.m_ihvoLim);
					//Warn(sta.Chars());

					VwBox* pboxFirstLayout;
					VwBox* pboxLimLayout;
					VwBox* pRetTmp = ExpandLazyItemsNoLayoutOnRootb(prootb,
						elii.m_hvoContext, elii.m_tag, elii.m_iprop, elii.m_ihvoMin, elii.m_ihvoLim,
						irootb, &pboxFirstLayout, &pboxLimLayout);

					if (!pRet && prootb == prootbSrc)
					{
						// We want to return the values for the box we got started with
						pRet = pRetTmp;
						*ppboxFirstLayout = pboxFirstLayout;
						*ppboxLimLayout = pboxLimLayout;
					}
				}
			}
			m_vLazyItemsInfo.Replace(0, 1, NULL, 0);
		}

		for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
		{
			if (dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr()) == prootbSrc)
				irootbSrc = irootb;
			else
			{
				// Lay out the secondary views - this might expand some more boxes (and add
				// something to m_vLazyItemsInfo)
				VwLazyBox::LayoutExpandedItems(m_vboxFirstLayout[irootb], m_vboxLimLayout[irootb],
					m_vboxContainer[irootb]);
			}
		}

		VwLazyBox::LayoutExpandedItems(m_vboxFirstLayout[irootbSrc], m_vboxLimLayout[irootbSrc],
			m_vboxContainer[irootbSrc], true);
	}
	m_fAlreadyExpandingItems = false;

	Assert(irootbSrc > -1);

	// Lay out the source view and adjust positions of all boxes in all views
	prootbSrc->AdjustBoxPositions(rcRootOld, m_vboxFirstLayout[irootbSrc],
		m_vboxLimLayout[irootbSrc], m_rcAllExpandedBoxes, m_vboxContainer[irootbSrc],
		pfForcedScroll, this, false);

	Assert(m_vLazyItemsInfo.Size() == 0);
	m_vLazyItemsInfo.Clear();
	m_vboxContainer.Clear();
	m_vboxFirstLayout.Clear();
	m_vboxLimLayout.Clear();
	m_rcAllExpandedBoxes.Clear();
	m_vTopBottomExpandedBoxes.Clear();

	return pRet;
}

/*----------------------------------------------------------------------------------------------
	Call ExpandLazyItemsNoLayout on the root box. Also update the first and lim boxes.
----------------------------------------------------------------------------------------------*/
VwBox * VwSynchronizer::ExpandLazyItemsNoLayoutOnRootb(VwRootBox * prootb, HVO hvoContext, int tag,
	int iprop, int ihvoMin, int ihvoLim, int irootb, VwBox** ppboxFirstLayout, VwBox** ppboxLimLayout)
{
	Rect rcLazyBoxOld;
	VwDivBox* pdboxContainer;

	VwBox* pRet = prootb->ExpandItemsNoLayout(hvoContext, tag, iprop, ihvoMin, ihvoLim,
		&rcLazyBoxOld, ppboxFirstLayout, ppboxLimLayout, &pdboxContainer);

	// If this asserts, then ExpandItemsNoLayout() expanded some items with a different container
	// then the box we're expanding (e.g. we had to expand the previous/next section because of borders).
	Assert(m_vboxContainer[irootb] == NULL || m_vboxContainer[irootb] == pdboxContainer);
	Assert(pdboxContainer || (!*ppboxFirstLayout && !*ppboxLimLayout));
	if (m_vTopBottomExpandedBoxes[irootb].IsClear())
	{
		m_vTopBottomExpandedBoxes[irootb] = Rect(0, ihvoMin, 0, ihvoLim);
		m_vboxFirstLayout[irootb] = *ppboxFirstLayout;
		m_vboxLimLayout[irootb] = *ppboxLimLayout;
	}
	else if (ihvoMin == m_vTopBottomExpandedBoxes[irootb].bottom)
	{	// we expanded the box below our previous expanded box
		m_vboxLimLayout[irootb] = *ppboxLimLayout;
		m_vTopBottomExpandedBoxes[irootb].bottom = ihvoLim;
	}
	else if (ihvoLim == m_vTopBottomExpandedBoxes[irootb].top)
	{	// we expanded the box above our previous expanded box
		m_vboxFirstLayout[irootb] = *ppboxFirstLayout;
		m_vTopBottomExpandedBoxes[irootb].top = ihvoMin;
	}
	else
	{
		// we expanded something non-continous
		Assert(false);
	}
	m_vboxContainer[irootb] = pdboxContainer;
	if (m_rcAllExpandedBoxes.IsClear())
		m_rcAllExpandedBoxes = rcLazyBoxOld;
	else
		m_rcAllExpandedBoxes.Union(rcLazyBoxOld);

	return pRet;
}

/*----------------------------------------------------------------------------------------------
	Informs other synchronized roots that the source one contracted items from ihvoMin to ihvoLim
	to a lazy box which represents property tag of object hvoContext as the iprop'th
	property.
----------------------------------------------------------------------------------------------*/
void VwSynchronizer::ContractLazyItems(VwRootBox * prootbSrc, HVO hvoContext, int tag,
	int iprop, int ihvoMin, int ihvoLim)
{
	for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
	{
		if (dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr()) != prootbSrc)
			dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr())->
				ContractLazyItems(hvoContext, tag, iprop, ihvoMin, ihvoLim);
	}
}

/*----------------------------------------------------------------------------------------------
	Answer true if any synchronized root box other than prootbSrc has a selection.
----------------------------------------------------------------------------------------------*/
bool VwSynchronizer::AnotherRootHasSelection(VwRootBox * prootbSrc)
{
	for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
	{
		if (dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr()) != prootbSrc)
		{
			if (dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr())->ActiveSelections().Size() > 0)
				return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Reconstruct every root box in the set, but in a way that preserves the sync. This means
	that we must do the Construct() phase for everyone before the Layout() phase for everyone.
----------------------------------------------------------------------------------------------*/
void VwSynchronizer::Reconstruct()
{
	// Save the information we need to determine which views changed size.
	IntVec vHeight;
	IntVec vFieldHeight;
	IntVec vWidth;

	// First save all the current sizes.
	for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
	{
		VwRootBox * prootb = dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr());
		// Root boxes don't currently get removed from the sync list when closed.
		// but, trying to do some of this stuff to them when closed will Assert.
		// And some weird combination of circs produces a Reconstruct while
		// shutting down.
		if (prootb->Site() == NULL)
			continue;

		vHeight.Push(prootb->Height());
		int dyOld;
		if (prootb->m_fConstructed)
			dyOld = prootb->FieldHeight();
		else
			dyOld = 0;
		vFieldHeight.Push(dyOld);

		vWidth.Push(prootb->Width());

		Rect vwrect = prootb->GetInvalidateRect();
		prootb->InvalidateRect(&vwrect); //old
	}

	// Now we reconstruct everything. No layout until all are back in default state,
	// with nothing lazy expanded.
	for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
	{
		VwRootBox * prootb = dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr());
		if (prootb->Site() == NULL)
			continue;
		CheckHr(prootb->DestroySelection());
		prootb->ClearNotifiers();
		NotifierVec vpanoteDelDummy; // required argument, but all gone already.
		prootb->DeleteContents(prootb, vpanoteDelDummy);
		int dxAvailWidth;
		CheckHr(prootb->Site()->GetAvailWidth(prootb, &dxAvailWidth));
		HoldLayoutGraphics hg(prootb);
		prootb->Construct(hg.m_qvg, dxAvailWidth);
	}

	// Now we lay them ALL out...
	for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
	{
		VwRootBox * prootb = dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr());
		if (prootb->Site() == NULL)
			continue;
		int dxAvailWidth;
		CheckHr(prootb->Site()->GetAvailWidth(prootb, &dxAvailWidth));
		HoldLayoutGraphics hg(prootb);
		prootb->Layout(hg.m_qvg, dxAvailWidth);
	}

	// And then, when they've all had a chance to affect each other, we figure size
	// changes..
	for (int irootb = 0; irootb < m_vrootb.Size(); irootb++)
	{
		VwRootBox * prootb = dynamic_cast<VwRootBox *>(m_vrootb[irootb].Ptr());
		if (prootb->Site() == NULL)
			continue;
		if (vHeight[irootb] != prootb->Height()
			|| vFieldHeight[irootb] != prootb->FieldHeight()
			|| vWidth[irootb] != prootb->Width())
		{
			CheckHr(prootb->Site()->RootBoxSizeChanged(prootb));
		}
		prootb->Invalidate(); // new
	}
}

// Explicit instantiation
#include "Vector_i.cpp"
template class ComVector<IVwRootBox>; // RootBoxVec;
template class Vector<ExpandLazyItemsInfo>; // LazyItemsInfoVec;

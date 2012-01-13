/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwLayoutStream.cpp
Responsibility: John Thomson

Description:

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

//:>********************************************************************************************
//:>	VwLayoutStream Methods
//:>********************************************************************************************

VwLayoutStream::VwLayoutStream(VwPropertyStore * pzvps)
	: VwRootBox(pzvps)
{
}

// Protected default constructor used for CreateCom
VwLayoutStream::VwLayoutStream() : VwRootBox()
{
}


VwLayoutStream::~VwLayoutStream()
{
}

/*----------------------------------------------------------------------------------------------
	Ensure that the view is constructed and laid out at our specified width (TODO: and DPI).
----------------------------------------------------------------------------------------------*/
void VwLayoutStream::ConstructAndLayout(IVwGraphics * pvg, int dxsAvailWidth)
{
	// Todo: Layout should delete all pages if the width changed.
	if (!m_fConstructed)
		Construct(pvg, dxsAvailWidth); // Does NOT lay out to this width.
	// Todo: also save and check the dpi of the VwGraphics. Layout if changed.
	if (m_dxsLayoutWidth != dxsAvailWidth)
		Layout(pvg, dxsAvailWidth);
}

/*----------------------------------------------------------------------------------------------
	Find the page that has the specified handle. Answer null if there is none.
	(This does NOT give a reference count on the object...assign to a smart pointer if you
	want to keep it beyond when it is removed from the hashmap. This should be rare.)
----------------------------------------------------------------------------------------------*/
VwPage * VwLayoutStream::FindPage(int hPage)
{
	VwPagePtr qpage;
	if (m_hmhpagePages.Retrieve(hPage, qpage))
		return qpage;
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Add the page to our data structures.
----------------------------------------------------------------------------------------------*/
void VwLayoutStream::AddPage(VwPage * ppage)
{
	VwPagePtr qpage = ppage;
	m_hmhpagePages.Insert(ppage->m_hPage, qpage, true);
}

/*----------------------------------------------------------------------------------------------
	Create a new page with the specified handle and add it to our data structures.
----------------------------------------------------------------------------------------------*/
VwPage * VwLayoutStream::CreatePage(int hPage)
{
	VwPagePtr qpage;
	qpage.Attach(NewObj VwPage(hPage));
	AddPage(qpage);
	return qpage;
}

//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP VwLayoutStream::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid != IID_IVwLayoutStream)
		return SuperClass::QueryInterface(riid, ppv);
	*ppv = static_cast<IVwLayoutStream *>(this);

	SuperClass::AddRef(); // prevents confusion with AddRef supposedly inherited through interface
	return NOERROR;
}


//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Views.VwLayoutStream"),
	&CLSID_VwLayoutStream,
	_T("SIL layout stream"),
	_T("Apartment"),
	&VwLayoutStream::CreateCom);

void VwLayoutStream::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwLayoutStream> qlay;
	qlay.Attach(NewObj VwLayoutStream());		// ref count initialy 1
	CheckHr(qlay->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	IVwLayoutStream methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
 Set the Manager that callbacks will be made to during the layout process.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwLayoutStream::SetManager(IVwLayoutManager * plm)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(plm);
	m_qlm = plm;
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
 A (dependent) stream is requested by this method to lay out the specified object
 for display using the specified graphics object on the specified page.
 The object to be expanded is indicated using the ihvoRoot, cvsli, and prgvsli,
 in the same manner as MakeTextSelInObj indicates the first object to be selected.
 Typically ihvoRoot is 0, and there is just one cvsli giving the property and index
 of the desired object. But the design makes it possible to have more structure.
 This call also makes hPage an 'active' page; editing on the page will result
 in callbacks if something changes that might require the page boundary to move.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwLayoutStream::LayoutObj(IVwGraphics * pvg, int dxsAvailWidth,
	int ihvoRoot, int cvsli, VwSelLevInfo * prgvsli, int hPage)
{
	BEGIN_COM_METHOD;
	// If not already constructed and laid out at this width, do so now.
	ConstructAndLayout(pvg, dxsAvailWidth);
	// Find or create the page object.
	VwPage * ppage = FindPage(hPage);
	if (ppage == NULL)
	{
		ppage = CreatePage(hPage);
		m_pageRollBack = *ppage; // Any rollback on this new page will return it to empty.
	}

	int dpiY;
	CheckHr(pvg->get_YUnitsPerInch(&dpiY));

	VwNotifier * pnote = NotifierForSliArray(ihvoRoot, cvsli, prgvsli);
	Assert(pnote);	// streams are inconsistent if we can't find notifier for selection
	// If there's nothing on the page, locate the top of this object and set the start vars.
	if (ppage->m_pboxStart == NULL)
	{
		VwBox *pboxStart = pnote->FirstBox();
		// Verify that it's a start-of-line box.
		// Enhance: possibly we could allow something like a table cell that is the start of
		// its row.
		// Enhance: possibly we need to drill down to a non-pile if pboxStart is a pile.
		for (VwGroupBox * pgbox = pboxStart->Container(); pgbox; pgbox = pgbox->Container())
			if (!pgbox->IsPileBox())
				ThrowHr(WarnHr(E_INVALIDARG));
		//int itssStart = pnote->StringIndexes()[0];
		//// Enhance: eventually need to support starting within paragraph...
		//// ensure it is start of line, probably requires change of layout.
		//if (itssStart > 0)
		//	ThrowHr(WarnHr(E_INVALIDARG));
		ppage->m_pboxStart = pboxStart;
		//ppage->m_ichMin = 0; // Review: would it be better just to store the itsstring?
		// Now figure the top of pboxStart, plus its top margin, plus top margins of
		// anything embedded.
		// Enhance: possibly we should also add top margins of boxes within pboxStart.
		// However, we currently never put margins on string boxes, and doing it requires
		// us to create a messy collection of virtual methods because it depends on how
		// pboxStart lays out its contents. YAGNI.
		ppage->m_dysStart = 0; // Used to try to "eat" top margin, but TE-5807 suggests that this was a bad idea: pboxStart->TotalTopMargin(dpiY);
		// Enhance: adjust m_dysStart if not starting at the top of the box.
	}
	// Locate the bottom of the object and set the end vars.
	VwBox * pboxLast = pnote->LastBox();
	// Verify that it's a start-of-line box.
	// Enhance: possibly we could allow something like a table cell that is the end of
	// its row.
	// Enhance: possibly we need to drill down to a non-pile if pboxStart is a pile.
	for (VwGroupBox * pgbox = pboxLast->Container(); pgbox; pgbox = pgbox->Container())
		if (!pgbox->IsPileBox())
			ThrowHr(WarnHr(E_INVALIDARG));
	ppage->m_pboxEnd = pboxLast;
	// Enhance: eventually support notifier ending within paragraph.
	//int itssLast = pnote->LastStringIndex();
	//if (itssLast >= 0)
	//{
	//	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pboxLast);
	//	if (itssLast < pvpbox->Source()->CStrings() - 1)
	//	{
	//		// Enhance: figure the character offset. pvpbox->Source()->IchStartString(itssLast) + the length of the string.
	//		Assert(false);
	//	}
	//	else
	//		ppage->m_ichLim = pvpbox->Source()->Cch();
	//}
	ppage->m_dysEnd = pboxLast->TopToTopOfDocument() + pboxLast->Height() -
		pboxLast->TotalBottomMargin(dpiY) - ppage->m_pboxStart->TopToTopOfDocument();

	// Todo eventually: if the object boundaries are not line boundaries, adjust paragraph
	// layout so they are. If the old end object boundary was a forced line boundary,
	// remove it.
	// Possibly make a CommitPage method and have it make sure there is a line break after the last object.

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Perform the layout operation on a primary root site.
	The root is to be laid out for drawing using the specified Graphics object.

	pysStartThisPageBoundary indicates where the page is to start. If this is not a valid
		page break position (e.g., in the middle of a line or text or table), it will
		be adjusted (by decreasing) until it is.
	dxsAvailWidth indicates the width available for layout.
	dysAvailHeight indicates the height available on the page.
	pdysUsedHeight returns the amount of space used by the material fitted on the page
	pysStartNextPageBoundary returns a y position indicating where to start the next page.
		If there is no more content in the stream, returns *pysStartNextPageBoundary zero.
	hPage is a 'handle' to the page, passed to various other methods.
		It can be any arbitrary value, but the same value should not be used for two
		pages at the same time.
	nColumns is the number of columns that this page will be layed out with.

	This call also makes hPage an 'active' page; editing on the page will result
	in callbacks if something changes that might require the page boundary to move.
	This may result in actual internal layout, if some material on the page has
	not been laid out already (e.g., because of laziness).
	It also results in call-backs being made using the hPage for all object refs
	on the page.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwLayoutStream::LayoutPage(IVwGraphics * pvg, int dxsAvailWidth,
	int dysAvailHeight, int * pysStartThisPageBoundary, int hPage, int nColumns,
	int * pdysUsedHeight, int * pysStartNextPageBoundary)
{
	BEGIN_COM_METHOD;
	m_vColumnHeights.Clear();
	m_vColumnOverlaps.Clear();
	LayoutPageMethod(this, pvg, dxsAvailWidth, dysAvailHeight, pysStartThisPageBoundary, hPage,
		nColumns, &m_vColumnHeights, &m_vColumnOverlaps, pdysUsedHeight, pysStartNextPageBoundary).Run();
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
 Notifies the Layout Stream that notifications are no longer required for changes
 affecting the specified page.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwLayoutStream::DiscardPage(int hPage)
{
	BEGIN_COM_METHOD;
	m_hmhpagePages.Delete(hPage);
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
 Return an IVwSelection that indicates the position of one of the ends of the specified
 page.
 If the page break is within a paragraph, the end of the previous page and start of
 the next differ only by fAssocPrevious (always points to a character on the page).
 If the page break is at a paragraph boundary, the selection will be at the end of
 the last para on the page or the start of the first.
 If the boundary is at higher-level break, the IP is at the end of the last, lowest
 level paragraph at the end of the page, or the start of the first.
 Will fail if called for a page that is 'broken' and has not been redone.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwLayoutStream::PageBoundary(int hPage, ComBool fEnd, IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
 Return the total height of the material on the specified page.
 The page may have been established for this root site either by calling LayoutPage,
 or by one or more calls to LayoutObj. In the latter case, the height is everything
 from the first object laid out on this page to the last. (Anything not already
 laid out between the two will automatically be laid out during the call.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwLayoutStream::PageHeight(int hPage, int * pdysHeight)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pdysHeight);
	VwPage * ppage = FindPage(hPage);
	if (ppage)
		*pdysHeight = ppage->m_dysEnd - ppage->m_dysStart;
	// If not found, indicate by answering zero (*pdysHeght already set by ChkComOutPtr).
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Return the top of the part of the data in this stream that is on the specified page.
	This is a distance from the top of the document to the top of what is displayed on the page
	(inside any top margins). Note that subsequent expansion of lazy boxes above
	this could change this position.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwLayoutStream::PagePostion(int hPage, int * pysPosition)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pysPosition);
	VwPage * ppage = FindPage(hPage);
	if (ppage)
		*pysPosition = ppage->m_pboxStart->TopToTopOfDocument() + ppage->m_dysStart;
	// If not found, indicate by answering zero (*pysPosition already set by ChkComOutPtr).
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
 Return the height of column iColumn.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwLayoutStream::ColumnHeight(int iColumn, int * pdysHeight)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pdysHeight);

	if (!m_vColumnHeights.Size() || iColumn >= m_vColumnHeights.Size())
		return E_FAIL;

	*pdysHeight = m_vColumnHeights[iColumn];

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
 Return the overlap of column iColumn.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwLayoutStream::ColumnOverlapWithPrevious(int iColumn, int * pdysHeight)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pdysHeight);

	if (!m_vColumnOverlaps.Size() || iColumn >= m_vColumnOverlaps.Size())
		return E_FAIL;

	*pdysHeight = m_vColumnOverlaps[iColumn];

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
 Return whether the specified point is in a line in a page above the boundary.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwLayoutStream::IsInPageAbove(int xs, int ys, int ysBottomOfPage,
	IVwGraphics * pvg, int * pxsLeft, int * pxsRight, ComBool * pfInLineAbove)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfInLineAbove); // makes default result false
	ChkComOutPtr(pxsLeft); // makes default result zero
	ChkComOutPtr(pxsRight); // makes default result zero

	// If the target point is beyond the page boundary it can't possibly be on a previous page.
	// The routine would not normally be called in that case, but play safe.
	if (ys > ysBottomOfPage)
		return S_OK;

	// Find the box that is around the target point.
	int dysOffsetIntoBox;
	int dpiY, dpiX;
	CheckHr(pvg->get_YUnitsPerInch(&dpiY));
	CheckHr(pvg->get_XUnitsPerInch(&dpiX));
	VwBox *pboxTarget = FindNonPileChildAtOffset(ys, dpiY, &dysOffsetIntoBox);

	// If we didn't get a box, or it's a lazy box, we can't split it into lines. The point is considered on the
	// previous page if it's above the boundary at all. And we already know it is that.
	if (!pboxTarget || pboxTarget->IsLazyBox())
	{
		*pfInLineAbove = true;
		return S_OK;
	}
	Rect rcNull(0, 0, dpiX, dpiY);

	// If the main box it's in is above the relevant point, succeed trivially.
	int ysTopToTop = pboxTarget->TopToTopOfDocument();
	if (ysTopToTop + pboxTarget->VisibleBottom() - pboxTarget->Top()
		- pboxTarget->TotalBottomMargin(dpiY) <= ysBottomOfPage)
	{
		*pfInLineAbove = true;
		Rect rcSrc, rcDst;
		pboxTarget->CoordTransFromRoot(pvg, rcNull, rcNull, &rcSrc, &rcDst);
		*pxsLeft = rcSrc.MapXTo(pboxTarget->Left(), rcDst);
		*pxsRight = rcSrc.MapXTo(pboxTarget->Left() + pboxTarget->Width(), rcDst);

		return S_OK;
	}

	Vector<PageLine> vln;
	pboxTarget->GetPageLines(pvg, vln);
	for (int iln = 0; iln < vln.Size(); iln++)
	{
		// This line (and all subsequent ones) are on pages after the boundary,
		// and can't prove that our target coordinate is on the previous page.
		if (vln[iln].ypBottomOfLine > ysBottomOfPage)
			return S_OK;
		if (vln[iln].ypBottomOfLine < ys)
			continue; // line is entirely above target point, can't prove anything.
		// OK, this line is on the previous page. Is our x value inside its range?
		// If so, the point is on the previous page.
		VwBox * pbox = vln[iln].pboxFirst;
		int left = INT_MAX;
		int right = INT_MIN;
		VwBox * pboxLim = vln[iln].pboxLast->NextOrLazy();
		for (; pbox != pboxLim; pbox = pbox->NextOrLazy())
		{
			Rect rcSrc, rcDst;
			pbox->CoordTransFromRoot(pvg, rcNull, rcNull, &rcSrc, &rcDst);
			left = std::min(left, rcSrc.MapXTo(pbox->Left(), rcDst));
			right = std::max(right, rcSrc.MapXTo(pbox->Left() + pbox->Width(), rcDst));
		}
		if (left <= xs && xs <= right)
		{
			*pfInLineAbove = true;
			*pxsLeft = left;
			*pxsRight = right;
			return S_OK;
		}
		// We found a line just above the bottom of the page, but our x coord was NOT
		// in it. The last such line we found determines the left/right results if
		// our point is NOT on the previous page.
		*pxsLeft = left;
		*pxsRight = right;
	}
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}


/*----------------------------------------------------------------------------------------------
	Revert the collections of objects on the given page to its previously-committed state.
	If nothing on this page has yet been committed the page may be discarded.
	After adding pages with LayoutObj, a page should either be rolled back or committed.
	In the meantime the objects are treated as part of the page; in particular,
	the PageHeight DOES include them.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwLayoutStream::RollbackLayoutObjects(int hPage)
{
	BEGIN_COM_METHOD;
	if (hPage != m_pageRollBack.m_hPage)
		return S_OK;
	VwPage * ppage = FindPage(hPage);
	if (!ppage)
		return S_OK;
	*ppage = m_pageRollBack;
	if (m_pageRollBack.m_pboxStart == NULL)
		DiscardPage(hPage);
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}
/*----------------------------------------------------------------------------------------------
	Confirm the objects added to the page with LayoutObj. Marks a position that
	RollbackLayoutObjects will return to after subsequent LayoutObj calls until
	the next Commit.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwLayoutStream::CommitLayoutObjects(int hPage)
{
	BEGIN_COM_METHOD;
	VwPage * ppage = FindPage(hPage);
	if (!ppage)
		return S_OK;
	m_pageRollBack = *ppage;
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Lay the box out in the available width. Must be called before Draw, Height, or Width. In
	this sub-class, we store the width, so that later we can check to see if we need to re-
	layout, if the width changes.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwLayoutStream::Layout(IVwGraphics * pvg, int dxAvailWidth)
{
	m_dxsLayoutWidth = dxAvailWidth;
	return SuperClass::Layout(pvg, dxAvailWidth);
}

// Insert a box and all its containers except the root box into the map.
void InsertBoxAndContainers(BoxIntMultiMap * pmmbi, VwBox * pbox, int hPage, VwRootBox * prootb)
{
	Assert(pbox);
	if (pbox)
	{
		pmmbi->Insert(pbox, hPage);
		for (VwGroupBox * pgbox = pbox->Container();
			pgbox && pgbox != prootb;
			pgbox = pgbox->Container())
		{
			VwBox * pboxKey = pgbox;
			pmmbi->Insert(pboxKey, hPage);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Clean out everything and rebuild the view from scratch. Selections are lost. This is a last
	resort if some property changed and we are not sure what the consequences should be.
	VwLayoutStream overrides to send page broken notifications for all existing pages and
	remove them.

	@param fCheckForSync True to do synchronization if it is needed; false to force the
						reconstruct only on this one VwRootBox.
----------------------------------------------------------------------------------------------*/
void VwLayoutStream::Reconstruct(bool fCheckForSync)
{
	SuperClass::Reconstruct(fCheckForSync);

	// Send PageBroken notifications for all our pages, and delete them.
	// We build the vector so that all the old pages are actually gone before we start
	// making PageBroken calls. This helps robustness, by ensuring that nothing that
	// PageBroken() might do can possibly attempt to use one of the old VwPages, which
	// contain dangerous dangling pointers to boxes that the superclass Reconstruct()
	// has destroyed. It also ensures that the Clear() does not destroy any new pages
	// that might get created as a result of whatever PageBroken does.
	Vector<int> vpage;
	GpHashMap<int, VwPage>::iterator itLim = m_hmhpagePages.End();
	for (GpHashMap<int, VwPage>::iterator it = m_hmhpagePages.Begin(); it != itLim; ++it)
		vpage.Push(it.GetKey());
	m_hmhpagePages.Clear();
	for (int i = 0; i < vpage.Size(); i++)
		CheckHr(m_qlm->PageBroken(this,  vpage[i]));
}

/*----------------------------------------------------------------------------------------------
	This is overridden to generate the BoxIntMultiMap that is really used for re-laying out
	a root that is a layout stream. The incoming argument is always NULL.
	pboxsetDeleted is a set of boxes that were deleted as a result of the change that forced
	the relayout. If it is not null, check each page to see whether it's start or end box
	is in the set, and if so, that page is certainly broken, and we must not use those boxes.
----------------------------------------------------------------------------------------------*/
bool VwLayoutStream::RelayoutCore(IVwGraphics * pvg, int dxpAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int dxpAvailOnLine, BoxIntMultiMap * pmmbi, BoxSet * pboxsetDeleted)
{
	Assert(pmmbi == NULL); // We should not be called by anything that supplies an mmbi.
	BoxIntMultiMap mmbi;

	// Create a list of all of the last boxes in the rootbox so we can see if they changed later
	Vector<VwBox *> vlastBoxes;
	VwGroupBox * pgrpBox = dynamic_cast<VwGroupBox *>(m_pboxLast);
	while (pgrpBox)
	{
		vlastBoxes.Push(pgrpBox);
		pgrpBox = dynamic_cast<VwGroupBox *>(pgrpBox->LastBox());
	}

	// Insert the first and last box of each page into the map, and also their containers.
	GpHashMap<int, VwPage>::iterator itLim = m_hmhpagePages.End();
	for (GpHashMap<int, VwPage>::iterator it = m_hmhpagePages.Begin(); it != itLim; ++it)
	{
		VwPage * ppage = it.GetValue();
		if (pboxsetDeleted)
		{
			if (pboxsetDeleted->IsMember(ppage->m_pboxStart) ||
				pboxsetDeleted->IsMember(ppage->m_pboxEnd))
			{
				ppage->m_fPageBroken = true;
				ppage->m_pboxStart = ppage->m_pboxEnd = NULL; // make sure we don't use them!
				continue; // In particular don't put them in the map or look for their containers.
			}
		}
		InsertBoxAndContainers(&mmbi, ppage->m_pboxStart, ppage->m_hPage, prootb);
		InsertBoxAndContainers(&mmbi, ppage->m_pboxEnd, ppage->m_hPage, prootb);
	}

	bool fResult = SuperClass::Relayout(pvg, dxpAvailWidth, prootb, pfixmap, dxpAvailOnLine, &mmbi);

	// Check to see if any of the rootbox last boxes changed
	pgrpBox = dynamic_cast<VwGroupBox *>(m_pboxLast);
	int iLastBox = 0;
	bool fLastBoxChanged = false;
	while (pgrpBox)
	{
		VwBox * poldBox = vlastBoxes[iLastBox++];
		pgrpBox = dynamic_cast<VwGroupBox *>(pgrpBox->LastBox());
		if (poldBox != pgrpBox)
			fLastBoxChanged = true;
	}

	// If one of the last boxes changed, make sure we break any pages that contained any
	// of the last boxes. (TE-6383)
	if (fLastBoxChanged)
	{
		pgrpBox = dynamic_cast<VwGroupBox *>(m_pboxLast);
		while (pgrpBox)
		{
			pgrpBox = dynamic_cast<VwGroupBox *>(pgrpBox->LastBox());
			VwBox * pbox = pgrpBox;
			if (!pboxsetDeleted || !pboxsetDeleted->IsMember(pbox))
				pgrpBox->CheckBoxMap(&mmbi, prootb);
		}
	}

	// Build a vector of pages to delete, so we don't mess up the iterator by deleting as we go.
	// Enhance: by passing a struct containing the vector and map to Relayout, we could build the
	// vector as we go.
	Vector<int> vpage;
	itLim = m_hmhpagePages.End();
	for (GpHashMap<int, VwPage>::iterator it = m_hmhpagePages.Begin(); it != itLim; ++it)
	{
		VwPage * ppage = it.GetValue();
		if (ppage->m_fPageBroken)
		{
			vpage.Push(ppage->m_hPage);
		}
	}
	for (int i = 0; i < vpage.Size(); i++)
	{
		int hPage = vpage[i];
		if (FindPage(hPage))
		{
			CheckHr(m_qlm->PageBroken(this, hPage));
			CheckHr(DiscardPage(hPage));
		}
		// Otherwise it was in the list twice or more, and has already been done.
	}
	return fResult;
}

/*----------------------------------------------------------------------------------------------
	This is called from VwParagraphBox when it changes but determines (because its size did
	not change) that a RelayoutRoot is not needed. Any other box class that we ever support
	internal page breaks in should do the same if relevant. It reports a page broken if it
	starts or ends in the argument box.
	Optimize: since the box's size didn't change...otherwise we would have called
	RelayoutRoot...if it is entirely on one page or the other, we could skip the notification.
----------------------------------------------------------------------------------------------*/
void VwLayoutStream::SendPageNotifications(VwBox * pbox)
{
	GpHashMap<int, VwPage>::iterator itLim = m_hmhpagePages.End();
	for (GpHashMap<int, VwPage>::iterator it = m_hmhpagePages.Begin(); it != itLim; ++it)
	{
		VwPage * ppage = it.GetValue();
		if (ppage->m_pboxStart == pbox || ppage->m_pboxEnd == pbox)
		{
			CheckHr(m_qlm->PageBroken(this, ppage->m_hPage));
			CheckHr(DiscardPage(ppage->m_hPage));
		}
	}
}


//:>********************************************************************************************
//:>	VwPage methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Construct a page with the specified handle (and initially no contents).
----------------------------------------------------------------------------------------------*/
VwPage::VwPage(int hPage)
{
	m_hPage = hPage;
	m_fPageBroken = false;
	m_pboxStart = m_pboxEnd = NULL;
	m_dysStart = m_dysEnd = -1;
}


//:>********************************************************************************************
//:>	LayoutPageMethod methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Construct the LayoutPageMethod, which is a "method class" that provides the implementation
	of VwLayoutStream::LayoutPage
----------------------------------------------------------------------------------------------*/
LayoutPageMethod::LayoutPageMethod(VwLayoutStream * play, IVwGraphics * pvg,
	int dxsAvailWidth, int dysAvailHeight, int * pysStartThisPageBoundary, int hPage, int nColumns,
	Vector<int> * pvColHeights, Vector<int> * pvColOverlaps, int * pdysUsedHeight, int * pysStartNextPageBoundary)
{
	m_play = play;
	m_pvg = pvg;
	m_pvg->get_YUnitsPerInch(&m_dpiY);
	m_dxsAvailWidth = dxsAvailWidth;
	m_dysAvailHeight = dysAvailHeight;
	m_pysStartPageBoundary = pysStartThisPageBoundary;
	m_hPage = hPage;
	m_nColumns = nColumns;
	m_pvColHeights = pvColHeights;
	m_pvColOverlaps = pvColOverlaps;
	m_pdysUsedHeight = pdysUsedHeight;
	m_pysStartNextPageBoundary = pysStartNextPageBoundary;
}

/*----------------------------------------------------------------------------------------------
	OLD:Finds a line at or after the given position in the whole view. To do this, we look for the first
	line whose bottom (excluding any bottom margins) is greater than the given position. If we
	find such a line, we return the first (string) box on that line, the top of the highest
	(string) box on that line, and the bottom (excluding any bottom margins) of the lowest
	(string) box on that line. If we don't find any more data, this method returns a NULL box.

	Initialize m_pboxBeingAdded, m_vlnLinesOfBoxBeingAdded, and m_ilnBeingConsideredMin,
	and m_ilnBeingConsideredLim
	such that they represent the first (group of) line(s) to be placed on a page beginning at approximately
	ysPosition (from the top of the whole rootbox).

	To do this we first dig into the top-level piles (divs and tables) until we find a non-pile
	that matches the coordinate. We may have to expand lazy boxes to do this; we want a real box.
	Then we ask that box to fill in the line vector, and decide which of them to start at.
	We want the first line whose bottom is strictly greater than the specified position, that
	is, the first line that does not fit on a previous page ending at ysPosition.
----------------------------------------------------------------------------------------------*/
void LayoutPageMethod::FindFirstLineOnPage(int ysPosition)
{
	int dysOffsetIntoBox;
	m_pboxBeingAdded = m_play->FindNonPileChildAtOffset(ysPosition, m_dpiY, &dysOffsetIntoBox);
	m_vlnLinesOfBoxBeingAdded.Clear();
	m_ilnBeingConsideredMin = m_ilnBeingConsideredLim = 0;
	if (m_pboxBeingAdded != NULL)
	{
		m_pboxBeingAdded->GetPageLines(m_pvg, m_vlnLinesOfBoxBeingAdded);
		for (; m_ilnBeingConsideredMin < m_vlnLinesOfBoxBeingAdded.Size(); m_ilnBeingConsideredMin++)
		{
			if (m_vlnLinesOfBoxBeingAdded[m_ilnBeingConsideredMin].ypBottomOfLine > ysPosition)
				break; // The specified line won't fit on the previous page, so becomes the first of this.

		}
		// pathologically, there may not be a valid line that ends after the target coordinate.
		// for example, the coordinate might be in the bottom margin.
		// If so, go on to the next valid line, which must be after the coordinate because
		// it is after the whole target box.
		if (m_ilnBeingConsideredMin >= m_vlnLinesOfBoxBeingAdded.Size())
			AdvanceToNextLine();
	}
	if (m_ilnBeingConsideredMin < m_vlnLinesOfBoxBeingAdded.Size())
		SetLineGroupLimit();
	// TODO: See whether this line is really a valid page break. If not, back up.
	// However, this is dangerous: what if we took an emergency, invalid page break on the page
	// before because otherwise nothing fit? We really need to know whether the previous page
	// break was exact.
}

// Just for testing, I'm afraid.
void LayoutPageMethod::FindFirstLineOnPage(int dysPosition, VwBox ** ppboxFirst, VwBox ** ppboxLast,
	int * pysTopOfLine, int * pysBottomOfLine)
{
	FindFirstLineOnPage(dysPosition);
	if (m_vlnLinesOfBoxBeingAdded.Size() == 0)
	{
		*ppboxFirst = *ppboxLast = NULL;
		*pysTopOfLine = *pysBottomOfLine = 0;
		return;
	}
	PageLine ln = m_vlnLinesOfBoxBeingAdded[m_ilnBeingConsideredMin];
	*ppboxFirst = ln.pboxFirst;
	*ppboxLast = ln.pboxLast;
	*pysTopOfLine = TopOfCurrentLine();
	*pysBottomOfLine = ln.ypBottomOfLine;
}

///*----------------------------------------------------------------------------------------------
//	Finds a line at or after target position. If no line break can be found
//	(i.e., we are presumably at the very end of the data for this rootbox), this method returns
//	NULL.
//----------------------------------------------------------------------------------------------*/
//void LayoutPageMethod::FindPageBreakNear(int dysPosition, VwBox ** ppboxFirst,
//	int * pysTopOfLine, int * pysBottomOfLine)
//{
//	int dysOffsetIntoBox;
//	FindLine(dysPosition, &pboxFirst, &ysTopOfLine, &ysBottomOfLine);
//	//brk.m_ysTopOfPage = dysPosition - dysOffsetIntoBox;
//// TODO: Currently finds top of box at or before target position. We want this method to return
//// the line break at or after the given position.
//// REVIEW: Can we really make one method handle both caes: finding the top of the current page
//// and finding the next possible break? In one case we need the top of the line. In the other,
//// we need the bottom of the line.
//}

/*----------------------------------------------------------------------------------------------
	Return the extra space required for any of the lines in the current group.
----------------------------------------------------------------------------------------------*/
int LayoutPageMethod::GetExtraSpace()
{
	int extraSpace = 0;
	for (int iln = m_ilnBeingConsideredMin; iln < m_ilnBeingConsideredLim; iln++)
	{
		VwBox * pboxLast = m_vlnLinesOfBoxBeingAdded[iln].pboxLast;
		if (!pboxLast)
			continue; // defensive, I don't think this can happen.
		if (pboxLast->NextOrLazy() != NULL)
			continue; // not the last line, no question currently of extra space in this sense.
		if (!pboxLast->Container())
			continue;
		extraSpace = max(extraSpace, pboxLast->Container()->ExtraHeightIfNotFollowedByPara(true));
	}
	return extraSpace;
}

// Functor for adding dependent objects to a layout stream
class AddDependentObjectsMethod
{
private:
	VwLayoutStream * m_play;
	IVwGraphics * m_pvg;
	int m_hPage;

public:
	AddDependentObjectsMethod(VwLayoutStream * play, IVwGraphics * pvg, int hPage)
		: m_play(play), m_pvg(pvg), m_hPage(hPage)
	{
	}

	// Check all boxes from pboxFirst to pboxLast (inclusive) for ORC characters which are
	// owning links to dependent objects. For each such object, add it's GUID to the vector.
	void GetDependentObjectsInChunk(VwBox * pboxFirst, VwBox * pboxLast, Vector<GUID> & vguid)
	{
		// This gets the next box that is strictly AFTER (not contained within) the last box in
		// the current chunk. Using this as a limit allows the NextInRootSeq in the loop
		// to dig down inside boxes in the sequence, including the last one (which is important
		// for interlinear text).
		// Note that pboxFirst and pboxLast might belong to different paragraphs (e.g., because
		// of widow/orphan control), in which case, the second paragraph box will get a
		// GetDependentObjects call; but that method does NOT recurse, so we won't process
		// the whole paragraph, we just skip that and continue the root sequence to the relevant
		// ones of its children.
		VwBox * pboxLim = pboxLast->NextInRootSeq(true, NULL, false);
		for (VwBox * pbox = pboxFirst; pbox != pboxLim; pbox = pbox->NextInRootSeq(true, NULL, true))
		{
			pbox->GetDependentObjects(vguid);
		}
	}

	// Retrieve all dependent objects between pboxFirst and pboxLast and add them to the
	// layout stream.
	bool operator() (bool fNothingInColumnYet, PageLineVec & vln, int ilnMin, int ilnLim, int * pdysAvailHeight)
	{
		// Add any dependent objects on the current line, and adjust our available space accordingly.
		ComBool fFailed;
		Vector<GUID> vguid;
		for (int iln = ilnMin; iln < ilnLim; iln++)
			GetDependentObjectsInChunk(vln[iln].pboxFirst, vln[iln].pboxLast, vguid);
		if (vguid.Size() > 0)
		{
			CheckHr(m_play->m_qlm->AddDependentObjects(m_play, m_pvg, m_hPage,
				vguid.Size(), vguid.Begin(), !fNothingInColumnYet, &fFailed, pdysAvailHeight));
			if (fFailed && !fNothingInColumnYet)
				return false;
			Assert(!fFailed); // very bad if we can't fit even one full line & dependents on page!
		}
		return true;
	}
};

// Functor for balancing columns
class BalanceColumnsMethod
{
public:
	bool operator() (bool fNothingInColumnYet, PageLineVec & vln, int ilnMin, int ilnLim, int * pdysAvailHeight)
	{
		// nothing to do when balancing columns!
		return true;
	}
};

/*----------------------------------------------------------------------------------------------
	This is the main method for laying out a single page.
----------------------------------------------------------------------------------------------*/
void LayoutPageMethod::Run()
{
	// Make sure the stream is constructed and laid out at the correct width.
	m_play->ConstructAndLayout(m_pvg, m_dxsAvailWidth);

	// Find the paragraph or table (top-level thing below div) that contains *pysStartThisPageBoundary.
	// (Todo for laziness support: finding this para may involve expanding lazy stuff,
	// which may require an adjustment to ysStartThisPage.)
	// (Todo for table support: if it's a table, the whole thing must fit. Later, allow for
	// just a row to fit.)
	// If it's a paragraph, figure which line contains ysStartThisPage.
	// Figure the closest valid page break at or after the top of that line.
	VwBox * pboxFirst; // first string box on page
	VwBox * pboxLast; // last string box on page

	// Layout the page. While doing this we have to adjust for any dependent objects like
	// footnotes that have an influence on the available height. If we have multiple columns we
	// pretend we have one long narrow page with a single column. The height we can use is the
	// available height times the number of columns. Adjusting the columns is a separate second
	// step.
	int ysBottomOfLastLineThatFit;
	int dysAvailHeight = m_dysAvailHeight * m_nColumns;
	int nLines = LayoutFullPage(1, dysAvailHeight, 0,
		AddDependentObjectsMethod(m_play, m_pvg, m_hPage), &pboxFirst, &pboxLast,
		&ysBottomOfLastLineThatFit, &dysAvailHeight);

	if (!nLines)
		return;	// empty page, so we can't do anything

	if (m_nColumns > 1)
	{
		if (nLines > 1)
		{
			// Balance the columns
			// Split the used height into columns
			int dysInitialCalcHeightPerColumn = *m_pdysUsedHeight / m_nColumns;
			// remaining available height below columns
			int dysExtraAvailHeight = dysAvailHeight / m_nColumns;
			// It is very possible that dysInitialCalcHeightPerColumn is in the middle of a line.
			// We need to loop through all the lines again and re-adjust the used height and the
			// start of the next page to make everything fit in the columns.
			LayoutFullPage(m_nColumns, dysInitialCalcHeightPerColumn, dysExtraAvailHeight,
				BalanceColumnsMethod(), &pboxFirst, &pboxLast, &ysBottomOfLastLineThatFit,
				&dysAvailHeight);
		}
		else
		{
			// need to set height of further columns to 0 to prevent crash
			for (int iColumn = 1; iColumn < m_nColumns; iColumn++)
			{
				m_pvColHeights->Push(0);
				m_pvColOverlaps->Push(0);
			}
		}
	}

	// Create our own page object, so we can keep track of page position and send
	// notifications if we break it.
	VwPagePtr qpage;
	qpage.Attach(NewObj VwPage(m_hPage));
	qpage->m_pboxStart = pboxFirst;
	qpage->m_pboxEnd = pboxLast;
	qpage->m_dysStart = *m_pysStartPageBoundary - qpage->m_pboxStart->TopToTopOfDocument();
	// Yes, we really want this to be the distance from the START box!
	qpage->m_dysEnd = ysBottomOfLastLineThatFit - qpage->m_pboxStart->TopToTopOfDocument();
	Assert(!qpage->m_pboxStart->IsStringBox());
	Assert(!qpage->m_pboxEnd->IsStringBox());
	m_play->AddPage(qpage);
}

// Answer true if there is more material available to add to this page. That is, we got a box
// that we should put (perhaps partly) on this page, and it has more lines that we haven't put
// on a previous page or already put on this one.
bool LayoutPageMethod::MoreStuffToAdd()
{
	return m_pboxBeingAdded != NULL || m_ilnBeingConsideredMin < m_vlnLinesOfBoxBeingAdded.Size();
}

// Top of current line is min of all the box sequences that make up the complete 'line'.
int LayoutPageMethod::TopOfCurrentLine()
{
	int result = m_vlnLinesOfBoxBeingAdded[m_ilnBeingConsideredMin].ypTopOfLine;
	for (int iln = m_ilnBeingConsideredMin + 1; iln < m_ilnBeingConsideredLim; iln++)
		result = min(result, m_vlnLinesOfBoxBeingAdded[iln].ypTopOfLine);
	return result;
}

/*----------------------------------------------------------------------------------------------
	Layout one full page. We do this by looping over all the columns, adding one line after the
	other as long as they fit in dysInitialAvailableHeightPerColumn (plus
	dysAdditionalAvailableHeight). While adding the line we also add the dependent objects for
	that line.
	This method gets called twice: For the initial layout we call it and pretend we have one
	long column. This adds all dependent objects and calculates how many lines really fit on the
	page. In a second pass we call this method again to balance the columns. The initial height
	we use per column is the calculated height of the first pass divided by the number of
	columns. While we process this second pass we might find that a line would fit at the bottom
	of the column if we increase this initial height slightly, but only if it fits in
	dysAdditionalAvailableHeight.

	@param nColumns Number of columns (for initial layout this will be 1, for balancing columns
					it is the real number of columns).
	@param dysInitialAvailableHeightPerColumn The initial estimated height per column.
	@param dysAdditionalAvailableHeight Additional available space. This will be 0 for the
					initial layout of columns. When balancing columns this is the extra space
					from the first pass that is left over below the last line.
	@param f		Functor. For the first pass this is a method that adds the dependent
					objects. For balancing columns this simply does nothing.
	@param ppboxFirst Pointer to the first box (in the first column) at least partly on the page.
	@param ppboxLastThatFit Pointer to the last box (in the last column) that fit at least partly on the page.
	@param pysBottomOfLastLine The bottom of the last line (in the last column) that fit on
					the page.
	@param pdysMinAvailHeight The height available below the tallest column after we did a
					layout.

	@return Returns the number of lines on this page
----------------------------------------------------------------------------------------------*/
template<class Op> int LayoutPageMethod::LayoutFullPage(int nColumns,
	int dysInitialAvailableHeightPerColumn, int dysAdditionalAvailableHeight, Op f,
	VwBox** ppboxFirst, VwBox** ppboxLastThatFit, int * pysBottomOfLastLineThatFit,
	int * pdysMinAvailHeight)
{
	// REVIEW (EberhardB): Can we somehow merge this method with VwBox::FindBreak()?

	m_pvColHeights->Clear();
	m_pvColOverlaps->Clear();
	int nLines = 0;
	*pdysMinAvailHeight = dysInitialAvailableHeightPerColumn;
	*m_pdysUsedHeight = 0;
	*ppboxFirst = *ppboxLastThatFit = NULL;
	*pysBottomOfLastLineThatFit = 0;

	// Initialize: find the first box (and possibly line of that box) we will put on the page.
	FindFirstLineOnPage(*m_pysStartPageBoundary);
	if (!MoreStuffToAdd())
	{
		return 0;
	}
	// The page boundary is ideally the 'bottom' of the previous line.
	// If there isn't a previous line in the current box, the top of the current line works,
	// since containing boxes don't (currently) overlap. This works better than the current box,
	// because we don't need to fit on the page any margins etc. above the first line.
	if (m_ilnBeingConsideredMin > 0)
		*m_pysStartPageBoundary = m_vlnLinesOfBoxBeingAdded[m_ilnBeingConsideredMin - 1].ypBottomOfLine;
	else
		*m_pysStartPageBoundary = TopOfCurrentLine();
	*ppboxFirst = m_pboxBeingAdded;

	// First column starts at start of page.
	int ysTopBoundaryThisColumn = *m_pysStartPageBoundary;
	// for each column, add as much as fits (but always something).
	for (int iColumn = 0; iColumn < nColumns; iColumn++)
	{
		if (!MoreStuffToAdd())
		{
			// balancing failed; we have nothing to put in this column
			m_pvColHeights->Push(0);
			m_pvColOverlaps->Push(0);
			continue;
		}
		int ysTopOfThisColumn = min(TopOfCurrentLine(), ysTopBoundaryThisColumn);
		// This would be an unfortunate way to leave it, since it isn't a line bottom;
		// but we will always add at least one line to the column, since we know we have
		// more stuff to add.
		*pysBottomOfLastLineThatFit = ysTopOfThisColumn;

		bool fNothingInColumnYet = true;
		// Height available for more material to be added to this column;
		// does not include pending lines--lines we have considered but (because of Keep-with next etc)
		// not committed to including by adjusting *pysBottomOfLastLineThatFit.
		int dysAvailHeight = dysInitialAvailableHeightPerColumn;

		// Total extra height needed for dependent objects of pending lines. Gets adjusted to include
		// dependent objects of current line.
		// Does NOT include dep object height of lines already committed to including.
		int dysExtraHeightForDepObjects = 0;

		// number of lines on page including ones that we have determined will fit, but which are
		// pending being definitely added until we find a valid break point after one that fits.
		int nLinesWithPending = nLines;

		// When valid, the bottom of a previous, pending line that fit but has not been fully added.
		int ysBottomPreviousPending = -1; // invalid
		VwBox * pboxLastPending = NULL; // m_pboxBeingAdded of that previous, pending line.

		// Iterate forward, one possible page break at a time, expanding lazy stuff as we go.
		// For each possible break, identify embedded object characters of relevant types, and call
		// AddDependentObjects. Continue until AddDependentObjects fails, or the next chunk
		// won't fit even without calling AddDependentObjects. Note that the usual exit
		// is a break, hit when we conclude that the current line won't really fit.
		for (; MoreStuffToAdd() && dysAvailHeight > 0; AdvanceToNextLine())
		{
			// The lowest point on the 'line' that actually displays anything; must fit in available space.
			int ysBottomOfThisLine = m_vlnLinesOfBoxBeingAdded[m_ilnBeingConsideredMin].ypBottomOfLine;
			// Additional space by which the current line actually extends below ysBottomOfThisLine
			// (typically zero; the exception is a one-line paragraph with a drop cap).
			int dysExtraVisibleHeight = GetExtraSpace();
			// Distance bottom of this line to bottom of last line that fit
			int dysThisLineFromLast = ysBottomOfThisLine - *pysBottomOfLastLineThatFit;
			// Calculate the available height if we include this line (and any previous ones
			// since setting *pysBottomOfLastLineThatFit).
			int dysAvailHeightBeyondThisLine = dysAvailHeight - dysExtraHeightForDepObjects
				- dysThisLineFromLast - dysExtraVisibleHeight;

			// If the line MUST fit (because it is the first), or if it fits itself (without considering
			// dependent objects), determine the effect of dependent objects. This potentially adjusts
			// dysAvailHeightBeyondThisLine and dysExtraHeightForDepObjects.
			if (fNothingInColumnYet || dysAvailHeightBeyondThisLine > 0)
			{
				int dysAvailHeightBeforeDepObjects = dysAvailHeightBeyondThisLine;
				if (!f(fNothingInColumnYet, m_vlnLinesOfBoxBeingAdded, m_ilnBeingConsideredMin,
					m_ilnBeingConsideredLim, &dysAvailHeightBeyondThisLine))
				{
					// Couldn't fit the dependent objects; set dysAvailHeightBeyondThisLine very negative
					// so we will NOT add this (group of) lines.
					Assert(!fNothingInColumnYet); // Functor not allowed to return false when nothing on page.
					dysAvailHeightBeyondThisLine = INT_MIN;
				}
				else
				{
					// If the functor call (AddDependentObjects) reduced the available height,
					// we have to use the reduced available height for our further calculations.
					// However, we don't want reduce dysAvailHeight yet since we don't know if
					// this line (or following lines) really fit.
					dysExtraHeightForDepObjects +=
						dysAvailHeightBeforeDepObjects - dysAvailHeightBeyondThisLine;
				}
			}

			// If the line doesn't fit in the optimal (balanced column) space, see if it will fit by using
			// the space we reserved in order to balance the columns. (Except if there's nothing on the page,
			// we will include it no matter what, so skip this step.)
			if (dysAvailHeightBeyondThisLine <= 0 && !fNothingInColumnYet) // we want at least one line!
			{
				// would this line fit if we use the reserved column-balancing space?
				if (dysAvailHeightBeyondThisLine + dysAdditionalAvailableHeight < 0)
				{
					// No, it would not fit even if we allow the column to be unbalanced.

					// Is there ANYTHING that definitely fit in the column before the current pending line(s)?
					if (dysAvailHeight == dysInitialAvailableHeightPerColumn)
					{
						// No, we haven't definitely put anything in the column yet. But, we do have SOMETHING
						// in the column, according to fNothingInColumnYet; so it must be a previous pending line.
						// So that should not be invalid!
						Assert(pboxLastPending != NULL);
						// It's better to break the keep-together rules than
						// to have an empty (series of) columns or overlapping or clipped text,
						// so back up to the previous line that fit completely and make it the
						// column boundary (even though it's presumably keep-with-next).
						// ENHANCE: even if we ignore the keep rules, we still should try to obey
						// the widow/orphan control rules. That would make it look nicer.
						*pysBottomOfLastLineThatFit = ysBottomPreviousPending;
						*ppboxLastThatFit = pboxLastPending;
						nLines = nLinesWithPending;
						break; // out of the loop adding lines to this column; the current line will go in next column or page
					}
					else
					{
						// Yes, there is already something in the column, ending at a valid break point.
						// The pending material will go in a subsequent page or column.
						break;
					}
					// Currently it's not possible to get here. Doing so would have the effect of going ahead
					// and adding the current line to the column...another possible choice when nothing fits in
					// the column.
				}
				else
				{
					// It didn't fit in the balanced-column space, but does if we don't worry about balancing,
					// so go ahead and put it in...let the columns be slightly unbalanced.
				}
			}

			// Getting here signifies that the current line fits, or that for some other reason, including it in the
			// current column is better than any other available option.
			nLinesWithPending++; // So we have one more line (pending for now)
			fNothingInColumnYet = false;
			ysBottomPreviousPending = ysBottomOfThisLine + dysExtraVisibleHeight;
			pboxLastPending = m_pboxBeingAdded;

			// Normally the line we just added should actually be put on the page. However,
			// if it is the last line of a paragraph that should be kept with the next paragraph,
			// we only want to put it on the page if part of that next paragraph also fits.
			// In that case, we postpone putting it on the page, by not updating the variables
			// that indicate what we've actually put on the page.
			// This depends on KeepWithNext() never being true for the very last box!
			// This probably requires more attention if we ever implement KeepWithNext for
			// non-paragraph boxes.
			// Note that this would be the place to implement KeepTogether to prevent a whole
			// paragraph breaking, if we want to. (Or, we could treat a keep-together paragraph as
			// a single line.)
			if (CanBreakAfterCurrentGroup())
			{
				*pysBottomOfLastLineThatFit = ysBottomPreviousPending;
				*ppboxLastThatFit = pboxLastPending;
				// For adding MORE material, we don't need to consider how much the current line
				// extends beyond its official bottom.
				dysAvailHeight = dysAvailHeightBeyondThisLine - dysExtraVisibleHeight;
				dysExtraHeightForDepObjects = 0;
				nLines = nLinesWithPending;
				pboxLastPending = NULL; // no previous pending line, all previous are included.
			}
		}

		// TODO (TE-5438): This code doesn't take into account the possibility of a dependent
		// object no longer needing to be on the page after we adjust the start of the next
		// page.

		// Set pysStartNextPageBoundary to the beginning of the line that didn't fit.
		if (MoreStuffToAdd())
		{
			// ENHANCE: Might want to bump this up past any margins (top of line that didn't fit)
			// Might really be the start of the next column, at this point.
			*m_pysStartNextPageBoundary = *pysBottomOfLastLineThatFit;
		}
		else
			*m_pysStartNextPageBoundary = 0; // no more pages.

		int dysActualHeightThisColumn = *pysBottomOfLastLineThatFit - ysTopBoundaryThisColumn;
		// No absolute reason not to let it go negative...that would amount to allowing white space to
		// disappear in the page break...but it might mess up old tests or be an unexpected change of
		// behavior.
		int dysOverlapThisColumn = max(ysTopBoundaryThisColumn - ysTopOfThisColumn, 0);

		// Set pdysUsedHeight to something possibly a little less than *m_pysStartNextPageBoundary;
		// i.e., the end of the last line that did fit, without bottom margin, minus ysStartThisPage.
		// REVIEW: we may find that more sophisticated layout manager algorithms
		// require the space required both with AND without the bottom margin of the
		// last line.
		*m_pdysUsedHeight = max(*m_pdysUsedHeight, dysActualHeightThisColumn + dysOverlapThisColumn);
		m_pvColHeights->Push(dysActualHeightThisColumn);
		m_pvColOverlaps->Push(dysOverlapThisColumn);

		*pdysMinAvailHeight = min(*pdysMinAvailHeight, dysAvailHeight);

		// we continue with the first line in next column. Pending lines and the current one carry over.
		ysTopBoundaryThisColumn = *m_pysStartNextPageBoundary;
	}
	return nLines;
}

/*----------------------------------------------------------------------------------------------
	Return true if we can break the page or column after the current line group. It has to be OK for
	all the lines in the group.
----------------------------------------------------------------------------------------------*/
bool LayoutPageMethod::CanBreakAfterCurrentGroup()
{
	for (int iln = m_ilnBeingConsideredMin; iln < m_ilnBeingConsideredLim; iln++)
	{
		if (!CanBreakAfter(m_vlnLinesOfBoxBeingAdded[iln].pboxLast))
			return false;
	}
	return true;
}
//Return true if we can break the page or column after pboxEndOfLine.
bool LayoutPageMethod::CanBreakAfter(VwBox * pboxEndOfLine)
{
	if (pboxEndOfLine == NULL)
		return true; // should never happen

	VwGroupBox * pGroupBox = pboxEndOfLine->Container();
	if (pGroupBox == NULL)
		return true; // should never happen
	VwParagraphBox * pParagraph = dynamic_cast<VwParagraphBox*>(pGroupBox);
	if (pParagraph == NULL)
		return true; // should never happen

	bool fKeepAllowsBreaking = (pboxEndOfLine->NextOrLazy() || !pParagraph->KeepWithNext()) &&
		(!pboxEndOfLine->NextOrLazy() || !pParagraph->KeepTogether());
	if (!fKeepAllowsBreaking)
		return false;

	if (pParagraph->ControlWidowsAndOrphans() && pboxEndOfLine->NextOrLazy())
	{
		// If we break here, would that leave a widowed line, i.e. is the next line the last
		// line of the paragraph which would then possibly end up at the top of a page/column
		// on its own?
		VwBox * pBoxNextLineLast;
		VwBox * pBoxNextLine = StartOfNextLine(pboxEndOfLine);
		PrintableBottomOfLine(pBoxNextLine, &pBoxNextLineLast);

		bool fNextLineIsWidow = !pBoxNextLineLast || !pBoxNextLineLast->NextOrLazy();
		if (fNextLineIsWidow)
			return false; // don't break if this would result in a widowed next line

		// Figure out if a break after this line would result in a orphaned line.
		// To do this we have to know how many lines there are in the current paragraph above
		// this line. Unfortunately there is no method that would tell us if we are the first
		// line of a paragraph.
		Rect rcSrc(0,0,1,1); //Identity transform
		int yTopThisLine, yBottomThisLine;
		pParagraph->GetLineTopAndBottom(m_pvg, pboxEndOfLine, &yTopThisLine, &yBottomThisLine,
			rcSrc, rcSrc);

		int yBottomPrevLine = 0;
		int nPrevLines = 0;
		VwBox* pbox;
		for (pbox = pParagraph->FirstBox(); pbox && pbox != pboxEndOfLine; pbox = pbox->Next())
		{
			int yTop, yBottom;
			pParagraph->GetLineTopAndBottom(m_pvg, pbox, &yTop, &yBottom, rcSrc, rcSrc);
			if (yTop >= yTopThisLine)
				break;
			if (yTop > yBottomPrevLine)
			{
				yBottomPrevLine = yBottom;
				nPrevLines++;
			}
		}

		// Currently we want to have at least two lines of a paragraph together. This could
		// easily be changed here if we want to have three or more lines!
		bool fIsOrphan = (nPrevLines < 1);

		return !fIsOrphan; // don't break if this would leave current line orphaned.
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Adjust m_pboxBeingAdded, m_vlnLinesOfBoxBeingAdded, and m_ilnBeingConsideredMin, and
	m_ilnBeingConsideredMin so they indicate the next line group that needs to be added.
	Specifically:
	- if there are more lines in the current box advance m_ilnBeingConsideredMin to the next
	- otherwise advance m_pboxBeingAdded to the next available non-pile box,
		and set the other variables to indicate its first line (which might be the box itself).
	- then set m_ilnBeingConsideredLim to indicate the end of the group of lines having the
		exact same bottom as m_ilnBeingConsideredMin.
----------------------------------------------------------------------------------------------*/
void LayoutPageMethod::AdvanceToNextLine()
{
	m_ilnBeingConsideredMin = m_ilnBeingConsideredLim;
	if (m_ilnBeingConsideredMin < m_vlnLinesOfBoxBeingAdded.Size())
	{
		SetLineGroupLimit();
		return;
	}
	m_ilnBeingConsideredMin = m_ilnBeingConsideredMin = 0;
	m_vlnLinesOfBoxBeingAdded.Clear();
	// Find the logically next box that is not a pile. The last false in the initializer
	// is there to make sure that we do NOT look inside the box we just finished
	// processing. On the other hand, the root sequence may
	// well take us up to piles that we MUST look inside, so the increment line
	// passes the last argument true.
	for (m_pboxBeingAdded = m_pboxBeingAdded->NextInRootSeq(true, NULL, false);
		m_pboxBeingAdded;
		m_pboxBeingAdded = m_pboxBeingAdded->NextInRootSeq(true, NULL, true))
	{
		if (dynamic_cast<VwPileBox *>(m_pboxBeingAdded))
			continue; // only interested in non-piles
		m_pboxBeingAdded->GetPageLines(m_pvg, m_vlnLinesOfBoxBeingAdded);
		if (m_vlnLinesOfBoxBeingAdded.Size() > 0)
		{
			SetLineGroupLimit();
			return; // got more lines.
		}
	}
	// If we drop out of the loop there is nothing more to put on the page, and we have
	// properly set m_pboxBeingAdded to null and cleared m_vlnLinesOfBoxBeingAdded to indicate this.
}

// Given that m_vlnLinesOfBoxBeingAdded and m_ilnBeingConsideredMin indicate the start of a
// line group, set the end of it. That is, set the limit so that the range is a group of
// consecutive sub-lines with the same bottom.
void LayoutPageMethod::SetLineGroupLimit()
{
	m_ilnBeingConsideredLim = m_ilnBeingConsideredMin + 1;
	while (m_ilnBeingConsideredLim < m_vlnLinesOfBoxBeingAdded.Size() &&
		m_vlnLinesOfBoxBeingAdded[m_ilnBeingConsideredLim].ypBottomOfLine ==
			m_vlnLinesOfBoxBeingAdded[m_ilnBeingConsideredMin].ypBottomOfLine)
	{
		m_ilnBeingConsideredLim++;
	}
}

/*----------------------------------------------------------------------------------------------
	Return the next start of line box, given that pbox is an end-of-line box.
	Specifically:
	- if pbox->NextRealBox() is non-null and pbox->Container() is a paragraph,
		we want that next box.
	- otherwise, iterate through the NextRealBox sequence from pbox
	until we find one (pboxNonPile) that is not a pile.
	- if pboxNonPile isn't a paragraph answer it.
	- if it is a paragraph answer its first box.

	Note that this handles some special cases:
	- if pbox is a picture in a pile and its next box is a paragraph, we have to dig into the paragraph.
	- if pbox is a table row and has a next box we just want that next box.
	- if pbox is in a paragraph, we must not dig inside its next box, even if it is a pile.
----------------------------------------------------------------------------------------------*/
VwBox * LayoutPageMethod::StartOfNextLine(VwBox * pboxEndOfPrevLine)
{
	VwBox * pboxNext = pboxEndOfPrevLine->NextRealBox();
	if (pboxNext && dynamic_cast<VwParagraphBox *>(pboxEndOfPrevLine->Container()))
		return pboxNext;
	VwBox * pbox;
	// Find the logically next box that is not a pile. The last false in the initializer
	// is there to make sure that we do NOT look inside the last box on the previous
	// line (where we might find all kinds of complex boxes including piles and more
	// paragraphs, in an interlinear text). On the other hand, the root sequence may
	// well take us up to piles that we MUST look inside, so the increment line
	// passes the last argument true.
	for (pbox = pboxEndOfPrevLine->NextInRootSeq(true, NULL, false);
		dynamic_cast<VwPileBox *>(pbox);
		pbox = pbox->NextInRootSeq(true, NULL, true))
	{
	}
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pbox);
	if (pvpbox)
		return pvpbox->FirstBox();
	return pbox;

	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Given a start of line box, determine the bottom of what is printable on that line
	(lowest bottom - bottom margin for any box on line) and the end box of the line.
	(If the container is a paragraph, iterate over the boxes with the same baseline; otherwise,
	just answer pboxStartLine and its bottom - margin.)
	The returned value is relative to the top of the rootbox.
----------------------------------------------------------------------------------------------*/
int LayoutPageMethod::PrintableBottomOfLine(VwBox * pboxStartLine, VwBox ** ppboxEndLine)
{
	Assert(pboxStartLine); // Must be valid coming in.
	Assert(ppboxEndLine); // Must be valid coming in.

	*ppboxEndLine = pboxStartLine; // initially the same thing

	Rect rcSrc(0,0,1,1); //Identity transform

	// We want to make sure we are working with a VwParagraph box.
	VwParagraphBox * pParaBox = dynamic_cast<VwParagraphBox *>(pboxStartLine->Container());
	if (!pParaBox)
	{
		Rect rcOutSrc;
		Rect rcOutDst;
		pboxStartLine->CoordTransFromRoot(m_pvg, rcSrc, rcSrc, &rcOutSrc, &rcOutDst);

		// Find the bottom of the printable line without margins
		int ysBottomNoMargins = pboxStartLine->VisibleBottom()
			- MulDiv(pboxStartLine->MarginBottom(), m_dpiY, kdzmpInch);
		return rcOutSrc.MapYTo(ysBottomNoMargins, rcOutDst);
	}

	int yTop, yBottom;
	pParaBox->GetLineTopAndBottom(m_pvg, pboxStartLine, &yTop, &yBottom, rcSrc, rcSrc);

	int yPrevBottom = yBottom;

	// Cycle through as long as we can get the next (non-lazy) box
	// Optimize JohnT: GetALineTopAndBottom already iterates over boxes in line,
	// could very easily return last box in line, then would not need a loop here at all.
	for (VwBox * pbox = pboxStartLine; pbox; pbox = pbox->NextRealBox()){
		// This will get the top and bottom y position based up the box passed in.
		pParaBox->GetLineTopAndBottom(m_pvg, pbox, &yTop, &yBottom, rcSrc, rcSrc);

		// Change the first box on the line if our previous top or bottom has changed
		if(yBottom != yPrevBottom)
			break;

		*ppboxEndLine = pbox; // change the last box on the line to our now current box.

		yPrevBottom = yBottom;
	}

	return yPrevBottom;
}

/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeTags.cpp
Responsibility: Ken Zook
Last reviewed: never

Implements
	TagsVc: The view constructor that determines how the tags view appears.
	AfDeFeTags: A field editor to display one or more lists of CmPossibility references.
	AfDeFeTags::AfDeSelListener: Keeps track of the selection.
	AfDeFeTags::DftButton: Displays and processes the button that calls up the list chooser.
	AfDeTagsWnd: A window allowing the tags to be edited.

Note: This was initially designed to allow references from multiple lists to be shown in a
single property.
	Tags:	OCM Fishing | Fishing Gear |
			Tst Test A | Test B |
as well as references to a single list
	Locations: river | forest |
The design specs have changed so that we currently have no need for showing multiple lists
in a single property. So far we haven't removed this capability (for when analysts change
their minds (^o^)). However, at some point we will probably want to show multiple encodings
for lists. At that point, we will need to replace the capability of showing multiple lists
with the capability of handling multiple encodings.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

#undef DEBUG_THIS_FILE
//#define DEBUG_THIS_FILE 1

// First available dummy ID. We reserve 64K ids at the end of the range for dummies.
const uint kidDummy = 0xffff0000;
//const uint kidNew = kidDummy + 1;

/*----------------------------------------------------------------------------------------------
	Since we want to disable formatting commands, we need to have our own command map in
	order to override and disable the buttons/comboboxes.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(AfDeTagsWnd)
	ON_CID_CHILD(kcidEditCut,              &AfDeTagsWnd::CmdEdit,  &SuperClass::CmsEditCut)
	ON_CID_CHILD(kcidEditPaste,            &AfDeTagsWnd::CmdEdit,  &SuperClass::CmsEditPaste)
	ON_CID_CHILD(kcidEditUndo,             &AfDeTagsWnd::CmdEditUndo, &AfDeTagsWnd::CmsEditUndo)
	ON_CID_CHILD(kcidEditRedo,             &AfDeTagsWnd::CmdEditRedo, &AfDeTagsWnd::CmsEditRedo)

	ON_CID_CHILD(kcidFmtFnt,               &AfVwWnd::CmdFmtFnt,    &AfDeTagsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmtStyles,            &AfVwWnd::CmdFmtStyles, &AfDeTagsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbStyle,           &AfVwWnd::CmdCharFmt,   &AfDeTagsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidApplyNormalStyle,     &AfVwWnd::CmdApplyNormalStyle, NULL)
	ON_CID_CHILD(kcidFmttbWrtgSys,         &AfVwWnd::CmdCharFmt,   &AfDeTagsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFnt,             &AfVwWnd::CmdCharFmt,   &AfDeTagsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFntSize,         &AfVwWnd::CmdCharFmt,   &AfDeTagsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbBold,            &AfVwWnd::CmdCharFmt,   &AfDeTagsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbItal,            &AfVwWnd::CmdCharFmt,   &AfDeTagsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyBgrndColor, &AfVwWnd::CmdCharFmt,   &AfDeTagsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyFgrndColor, &AfVwWnd::CmdCharFmt,   &AfDeTagsWnd::CmsCharFmt)
END_CMD_MAP_NIL()


//:>********************************************************************************************
//:> TagsVc methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
	@param pszSty Pointer to a Unicode string giving the name of the style to use for displays.
	@param clrBkg The background color for the field.
----------------------------------------------------------------------------------------------*/
TagsVc::TagsVc(LPCOLESTR pszSty, COLORREF clrBkg)
{
	AssertPsz(pszSty);
	m_stuSty = pszSty;
	m_clrBkg = (clrBkg == -1 ? ::GetSysColor(COLOR_WINDOW) : clrBkg);
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
TagsVc::~TagsVc()
{
}

static DummyFactory g_fact1(_T("SIL.AppCore.TagsVc"));

/*----------------------------------------------------------------------------------------------
	This is the method for displaying the contents of the tags field. If the possibilities come
	from a single list, they are simply displayed in one paragraph. If the possibilities come
	from more than one list, the items are sorted by list, and the view uses a table to display
	each list as a row in the table. The left cell in each row shows the list abbreviation,
	and the right cell shows the paragraph of possibility names/abbreviations. Multiple items
	in a list are separated by vertical gray bars (field separators).
	@param pvwenv Pointer to the view environment.
	@param hvo The id of the object we are displaying.
	@param frag Identifies the part of the view we are currently displaying.
	@return HRESULT indicating success (S_OK), or failure (E_FAIL).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TagsVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwenv);

	StrUni stu;
	switch (frag)
	{
	case kfrMultiList:
		{
			// Display a sequence of paragraphs, each showing the items from one possibility
			// list, tagged with the list abbreviations. Currently to get the paragraph
			// tagging exactly right we need a table.  Display the list of items for hvo as a
			// paragraph with vertical gray bars (field separators), inside a table that shows
			// the abbr.
			CheckHr(pvwenv->put_IntProperty(ktptParaColor, ktpvDefault, m_clrBkg));
			CheckHr(pvwenv->put_StringProperty(kspNamedStyle, m_stuSty.Bstr()));

			VwLength vlTab = {10000, kunPercent100}; // Table uses 100% of available width.
			// Extra tag column for subitem uses 1/3 inch
			// ENHANCE KenZ: We should probably calculate the width needed based on abbr.
			VwLength vlColAbbr = {kdzmpInch / 3, kunPoint1000};
			// The second column is relative, using the remaining space.
			VwLength vlColMain = {1, kunRelative};

			CheckHr(pvwenv->OpenTable(2, // Columns.
				vlTab,
				0, // Border thickness.
				kvaLeft, // Default alignment.
				kvfpVoid, // No border.
				kvrlNone, // No rules between cells.
				0, // No forced space between cells.
				0, // No padding inside cells.
				false));
			// Specify column widths. The first argument is # cols, not col index.
			// The abbreviation column only occurs at all if its width is non-zero.
			CheckHr(pvwenv->MakeColumns(1, vlColAbbr));
			CheckHr(pvwenv->MakeColumns(1, vlColMain));

			CheckHr(pvwenv->OpenTableBody());
			// Add the rows.
			CheckHr(pvwenv->AddObjVecItems(kflidPsslIds, this, kfrListInTable));
			CheckHr(pvwenv->CloseTableBody());

			CheckHr(pvwenv->CloseTable());
			break;
		}

	case kfrSingleList:
		// Display a paragraph of items with vertical gray bars (field separators).
		// Used when items come from a single list.
		CheckHr(pvwenv->put_IntProperty(ktptParaColor, ktpvDefault, m_clrBkg));
		CheckHr(pvwenv->put_StringProperty(kspNamedStyle, m_stuSty.Bstr()));
		CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptSemiEditable));
		if (m_fRTL)
			CheckHr(pvwenv->put_IntProperty(ktptLeadingIndent, ktpvMilliPoint, kdzmpInch / 4));
		else
			CheckHr(pvwenv->put_IntProperty(ktptTrailingIndent, ktpvMilliPoint, kdzmpInch / 4));
		CheckHr(pvwenv->put_IntProperty(ktptRightToLeft, ktpvEnum, m_fRTL));
		CheckHr(pvwenv->OpenParagraph());
		CheckHr(pvwenv->AddObjVecItems(kflidPsslIds, this, kfrList));
		CheckHr(pvwenv->CloseParagraph());
		break;

	case kfrList:
		// Display the list of items for hvo as a paragraph with vertical gray bars (field
		// separators).
		// This is currently the top level but will eventually move down one.
		// ENHANCE: figure out how much indent is needed to make room for button.
		// If it helps, you can have the view measure a string--see use of get_StringWidth
		// in CustDocVc.
		CheckHr(pvwenv->AddObjVec(kflidPssIds, this, kfrListVec));
		break;

	case kfrListInTable:
		// Display a single row in a table, with abbr and a paragraph of items.
		CheckHr(pvwenv->OpenTableRow());

		CheckHr(pvwenv->OpenTableCell(1,1));
		CheckHr(pvwenv->put_IntProperty(ktptSuperscript, ktpvEnum, kssvSuper));
		CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
		CheckHr(pvwenv->AddStringProp(kflidPsslAbbr, this));
		CheckHr(pvwenv->CloseTableCell());

		CheckHr(pvwenv->OpenTableCell(1,1));
		CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptSemiEditable));
		if (m_fRTL)
			CheckHr(pvwenv->put_IntProperty(ktptLeadingIndent, ktpvMilliPoint, kdzmpInch / 4));
		else
			CheckHr(pvwenv->put_IntProperty(ktptTrailingIndent, ktpvMilliPoint, kdzmpInch / 4));
		CheckHr(pvwenv->put_IntProperty(ktptRightToLeft, ktpvEnum, m_fRTL));
		CheckHr(pvwenv->OpenParagraph());
		CheckHr(pvwenv->AddObjVec(kflidPssIds, this, kfrListVec));
		CheckHr(pvwenv->CloseParagraph());
		CheckHr(pvwenv->CloseTableCell());

		CheckHr(pvwenv->CloseTableRow());
		break;

	case kfrItem:
		// Display one item by showing its name
		CheckHr(pvwenv->AddStringProp(kflidPssName, this));
		break;
	}
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	Display a vector of possibility items.
	@param pvwenv Pointer to the view environment.
	@param hvo The id of the object we are displaying.
	@param tag The field id holding the items.
	@param frag Identifies the part of the view we are currently displaying (unused here).
	@return HRESULT indicating success (S_OK), or failure (E_FAIL).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TagsVc::DisplayVec(IVwEnv * pvwenv, HVO hvo, int tag, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwenv);

	// We handle this one as a sequence so we can insert field separators.
	// Ignore frag as this constructor only has one sequence.
	ISilDataAccessPtr qsda;
	CheckHr(pvwenv->get_DataAccess(&qsda));
	int chvo;
	CheckHr(qsda->get_VecSize(hvo, tag, &chvo));
	Assert(chvo);  // We always add a dummy one, so zero should not occur.
	// For each item in the vector, add a field separator and the item.
	// Note, the final field separator allows the user to click in an empty location to add a
	// new item.
	for (int ihvo = 0; ihvo < chvo; ihvo++)
	{
		if (ihvo != 0)
			CheckHr(pvwenv->AddSeparatorBar());
		HVO hvoItem;
		CheckHr(qsda->get_VecItem(hvo, tag, ihvo, &hvoItem));
		CheckHr(pvwenv->AddObj(hvoItem, this, kfrItem));
	}
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	Set the writing system information for displaying the tags.

	@param ws code of Writing System used to display this field.
	@param pwsf pointer to the relevant writing system factory.
----------------------------------------------------------------------------------------------*/
void TagsVc::SetWritingSystemInfo(int ws, ILgWritingSystemFactory * pwsf)
{
	AssertPtr(pwsf);

	m_ws = ws;
	m_qwsf = pwsf;
	IWritingSystemPtr qws;
	m_fRTL = FALSE;
	CheckHr(pwsf->get_EngineOrNull(ws, &qws));
	if (qws)
		CheckHr(qws->get_RightToLeft(&m_fRTL));
}


//:>********************************************************************************************
//:>	AfDeFeTags methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDeFeTags::AfDeFeTags()
{
	m_kidNew = kidDummy + 1;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeFeTags::~AfDeFeTags()
{
	Assert(m_vpssl.Size() >= 0);
	if (m_vpssl.Size() > 0)
		m_vpssl.Clear();
}

static DummyFactory g_fact2(_T("SIL.AppCore.AfDeFeTags"));

/*----------------------------------------------------------------------------------------------
	Complete initialization of the property by storing the appropriate items, names, and lists.
	@param vpssl Vector of ids of possibility lists (we display one list per table row).
	@param fMultiList True to use a table to show list abbr, and list contents.
	@param fHier True if item names include parent items in the hierarchy.
	@param pnt Indicates whether we show name, abbr, or both for each item.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::Init(Vector<HVO> & vpssl, bool fMultiList, bool fHier, PossNameType pnt)
{
	Assert(m_flid); // Don't call this until Initialize has been called.

	int cpssl = vpssl.Size();
	Assert(cpssl); // We need at least one list.
	Assert(fMultiList || cpssl == 1); // For fSingleList, we can only have one pssl.
	Assert((uint)pnt < (uint)kpntLim);

	int ipssl;
	// Save the list ids and register ourself with the list
	for (ipssl = 0; ipssl < cpssl; ++ipssl)
	{
		m_vpssl.Push(vpssl[ipssl]);
		PossListInfoPtr qpli;
		GetLpInfo()->LoadPossList(vpssl[ipssl], m_wsMagic, &qpli);
		AssertPtr(qpli);
		qpli->AddNotify(this);
	}
	m_fMultiList = fMultiList;
	m_fHier = fHier;
	m_pnt = pnt;

	// Create the display cache.
	m_qvcd.CreateInstance(CLSID_VwCacheDa);
	// Create the view constructor.
	m_qtgvc.Attach(NewObj TagsVc(m_qfsp->m_stuSty.Chars(), m_chrp.clrBack));

	// Set the writing system info (especially for Right-To-Left handling).
	ILgWritingSystemFactoryPtr qwsf;
	GetLpInfo()->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	m_qtgvc->SetWritingSystemInfo(m_ws, qwsf);

	// Add information to the display cache.
	FillDisplayCache();
	m_fSuggestedText = false;
	m_fIgnoreNext_3 = false;
	m_fTypeAhead = false;

#ifdef DEBUG_THIS_FILE
	StrAnsi sta;
	sta.Format("AfDeFeTags::Init:  m_fSuggestedText set to false.\n");
	OutputDebugString(sta.Chars());
#endif
}


/*----------------------------------------------------------------------------------------------
	Relase all smart pointers.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::OnReleasePtr()
{
	for (int ipssl = m_vpssl.Size(); --ipssl >= 0; )
	{
		PossListInfoPtr qpli;
		if (GetLpInfo()->GetPossList(m_vpssl[ipssl], m_wsMagic, &qpli))
		{
			AssertPtr(qpli);
			qpli->RemoveNotify(this);
		}
	}
	SuperClass::OnReleasePtr();
}


/*----------------------------------------------------------------------------------------------
	Fill the display cache. Fake properties are used in the display cache to store the data as
	follows (The "vector" key is to the left of |->):

	This property stores a list of the possibility lists currently being displayed.
	1529 | kflidPsslIds |-> 186 | 978

	The following two properties store the items in the vector for each list above.
	Each list has a blank at the end for new insertions.
	186 | kflidPssIds |-> 302 | 303 | kidDummy
	978 | kflidPssIds |-> 979 | 980 | 983 | kidDummy

	This property stores names for all of the possibility items stored above.
	302 | kflidPssName |-> Fishing
	303 | kflidPssName |-> Fishing Gear
	979 | kflidPssName |-> Test A
	980 | kflidPssName |-> Test B
	983 | kflidPssName |-> Test E
	kidDummy | kflidPssName |-> ""

	This property stores abbreviations for all of the possibility lists stored above.
	186 | kflidPsslAbbr |-> OCM
	979 | kflidPsslAbbr |-> TST
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::FillDisplayCache()
{
	int cpssl = m_vpssl.Size();
	int chvoItems; // Count of items in prghvoPss and strings in vtssNames.

	// Get the list of items we are referencing.
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	CheckHr(qcvd->get_VecSize(m_hvoObj, m_flid, &chvoItems));

	// Save the list ids in a fake property of the root.
	m_qvcd->CacheVecProp(m_hvoObj, kflidPsslIds, &m_vpssl[0], cpssl);

	if (!m_fMultiList)
	{
		// For single lists, get the items and store them directly into prghvoPss.
		HVO * prghvoPss = NewObj HVO[chvoItems + 1];
		int ihvo;
		for (ihvo = 0; ihvo < chvoItems; ++ihvo)
			CheckHr(qcvd->get_VecItem(m_hvoObj, m_flid, ihvo, &prghvoPss[ihvo]));
		prghvoPss[ihvo] = kidDummy;
		// Store the main list of items as a fake property of the single list
		m_qvcd->CacheVecProp(m_vpssl[0], kflidPssIds, prghvoPss, chvoItems + 1);
		delete[] prghvoPss;
	}
	else
	{
		// For multi-lists, we need to load more info from the poss cache.
		StoreItemsByList();
	}

	// Add the names for each list.
	for (int ipssl = 0; ipssl < cpssl; ++ipssl)
		AddNames(m_vpssl[ipssl]); // Add names.

	// Add a dummy name to the end. This provides a space for the user to add a new item.
	ITsStringPtr qtss;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	CheckHr(qtsf->MakeStringRgch(L"", 0, m_ws, &qtss));
	m_qvcd->CacheStringProp(kidDummy, kflidPssName, qtss);
}


/*----------------------------------------------------------------------------------------------
	Cache the possibility names/abbreviations for the items for the specified possibility list.
	@param hvoPssl Id of the possibility list.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::AddNames(HVO hvoPssl)
{
	StrUni stu;
	ITsStringPtr qtss;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	PossListInfoPtr qpli;
	PossItemInfo * ppii = NULL;
	int cpss;
	ISilDataAccessPtr qsdaTemp;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));

	// Get the possibility list and cache the abbreviation.
	AfLpInfo * plpi = GetLpInfo();
	AssertPtr(plpi);
	plpi->LoadPossList(hvoPssl, m_wsMagic, &qpli);
	AssertPtr(qpli);
	stu = qpli->GetAbbr();
	CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_ws, &qtss));
	m_qvcd->CacheStringProp(hvoPssl, kflidPsslAbbr, qtss);

	// Get the items for the list and cache their names and/or abbreviations.
	CheckHr(qsdaTemp->get_VecSize(hvoPssl, kflidPssIds, &cpss));
	--cpss; // Skip the dummy at the end.
	for (int ipssl = 0; ipssl < cpss; ++ipssl)
	{
		HVO hvoPss;
		CheckHr(qsdaTemp->get_VecItem(hvoPssl, kflidPssIds, ipssl, &hvoPss));
		// Ignore new items that haven't been added to the list yet.
		if ((uint)hvoPss >= kidDummy)
			continue;
		int ipss = qpli->GetIndexFromId(hvoPss);
		// Ignore any items that are not in this list.
		if (ipss < 0)
			continue;
		ppii = qpli->GetPssFromIndex(ipss);
		if (!ppii)
			continue;
		if (m_fHier)
			ppii->GetHierName(qpli, stu, m_pnt);
		else
			ppii->GetName(stu, m_pnt);
		int ws;
		ws = ppii->GetWs();
		CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, &qtss));
		m_qvcd->CacheStringProp(hvoPss, kflidPssName, qtss);
	}
}


/*----------------------------------------------------------------------------------------------
	Query the poss list cache to get a list of possibility items and corresponding possibility
	lists sorted by list.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::StoreItemsByList()
{
	int cpssl = m_vpssl.Size(); // The number of lists.
	int cpss; // The number of items in the list (may be from mutliple lists).
	CustViewDaPtr qcvd;
	AfLpInfo * plpi = GetLpInfo();
	AssertPtr(plpi);
	plpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	CheckHr(qcvd->get_VecSize(m_hvoObj, m_flid, &cpss));

	if (!cpss)
	{
		// If the property is empty, cache an empty item for each list.
		HVO hvo = kidDummy;
		for (int i = 0; i < cpssl; ++i)
			m_qvcd->CacheVecProp(m_vpssl[i], kflidPssIds, &hvo, 1);
		return;
	}

	PossListInfoPtr qpli;
	PossItemInfo * ppii = NULL;
	int ipss;
	int ipssl;

	// Make sure possibility lists are cached.
	for (ipssl = 0; ipssl < cpssl; ++ipssl)
	{
		plpi->LoadPossList(m_vpssl[ipssl], m_wsMagic, &qpli);
		AssertPtr(qpli);
	}

	// Add each item to the cache for its possiblity list.
	HVO pss;
	// Go through each possibility list.
	for (ipssl = 0; ipssl < cpssl; ++ipssl)
	{
		Vector<HVO> vhvo; // List of items for the current list.
		HVO pssl = m_vpssl[ipssl];
		// Go through each item in the property.
		for (ipss = 0; ipss < cpss; ++ipss)
		{
			CheckHr(qcvd->get_VecItem(m_hvoObj, m_flid, ipss, &pss));
			if (plpi->GetPossListAndItem(pss, m_wsMagic, &ppii, &qpli))
			{
				if (qpli->GetPsslId() != pssl)
					continue; // The item isn't part of the current list.
				vhvo.Push(pss);
			}
		}
		vhvo.Push(kidDummy); // Add dummy entry to end.
		// Cache the list of items for the current list.
		m_qvcd->CacheVecProp(pssl, kflidPssIds, vhvo.Begin(), vhvo.Size());
	}
}


/*----------------------------------------------------------------------------------------------
	Refresh the field by reloading data from the poss cache and forcing it to display.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::UpdateField()
{
	ISilDataAccessPtr qsdaTemp;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));

	// Get the data from the cache.
	FillDisplayCache();
	// Force display updates by going through each list "property" and marking the property
	// as changed.
	int cpssl = m_vpssl.Size();
	for (int ipssl = 0; ipssl < cpssl; ++ipssl)
	{
		int cpss;
		CheckHr(qsdaTemp->get_VecSize(m_vpssl[ipssl], kflidPssIds, &cpss));
		qsdaTemp->PropChanged(m_qrootb, kpctNotifyAll, m_vpssl[ipssl], kflidPssIds, 0,
				cpss, cpss);
	}
}


/*----------------------------------------------------------------------------------------------
	Make a rootbox, initialize it, and return a pointer to it.
	@param pvg Graphics information (unused by this method).
	@param pprootb Pointer that receives the newly created RootBox.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf, IVwRootBox ** pprootb)
{
	AssertPtrN(pwsf);
	AssertPtr(pprootb);
	AssertPtr(m_qtgvc); // Init should have been called before calling this.

	*pprootb = NULL;

	// Create the RootBox.
	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this)); // The field editor is the root site.

	ISilDataAccessPtr qsdaTemp;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));

	// Finish setting it up.
	int frag = !m_fMultiList ? kfrSingleList : kfrMultiList; // Top-level fragment
	IVwViewConstructor * pvvc = m_qtgvc;
	if (pwsf)
		CheckHr(qsdaTemp->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qsdaTemp)); // The RootBox uses the display cache.
	CheckHr(qrootb->SetRootObjects(&m_hvoObj, &pvvc, &frag,
		GetLpInfo()->GetAfStylesheet(), 1));

	// Return the RootBox pointer.
	*pprootb = qrootb;
	AddRefObj(*pprootb);

	// Register the RootBox if we have a main window.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (prmw)
		prmw->RegisterRootBox(qrootb);
}


/*----------------------------------------------------------------------------------------------
	Check whether the content of this edit field has changed.
	@return True if the field changed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeTags::IsDirty()
{
	return m_fDirty;
}


/*----------------------------------------------------------------------------------------------
	Make an edit box to allow editing. hwnd is the parent hwnd. rc is the size of the child
	window. Store the hwnd and return true.
	See ${AfDeFieldEditor#BeginEdit} for parameter and return descriptions.
----------------------------------------------------------------------------------------------*/
bool AfDeFeTags::BeginEdit(HWND hwnd, Rect &rc, int dxpCursor, bool fTopCursor)
{
	if (!SuperClass::BeginEdit(hwnd, rc, dxpCursor, fTopCursor))
		return false;

	// Add a listener to notify us whenever the selection changes.
	m_qadsl = NewObj AfDeSelListener();
	CheckHr(m_qrootb->AddSelChngListener(m_qadsl));
	m_qadsl->m_pdft = this;

	// If we display the button here, we can't determine where it should occur since the
	// click doesn't get fully evaluated until after we exit this method. Instead, we
	// let the listener insert the button when the selection is first established.

	m_fDirty = false;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Saves changes but keeps the editing window open.
	@return True if edit saved successfully.
----------------------------------------------------------------------------------------------*/
bool AfDeFeTags::SaveEdit()
{
	EndTempEdit();
#ifdef DEBUG_THIS_FILE
	StrAnsi sta;
	sta.Format("AfDeFeTags::SaveEdit: this=%d, flid=%d\n", this, m_flid);
	OutputDebugString(sta.Chars());
#endif

	// Commit any edits so there isn't a transaction open.
	IVwSelectionPtr qsel;
	CheckHr(m_qrootb->get_Selection(&qsel));
	ComBool fOk;
	if (qsel)
		CheckHr(qsel->Commit(&fOk));

	// Since we should never be in an invalid state, we don't need IsOkToClose() first.
	Vector<HVO> vpssNew; // The new vector of possibility ids for the property.
	// Remove any duplicates from the list.
	int cpssl = m_vpssl.Size();
	AfLpInfo * plpi = GetLpInfo(); // pointer to the current language project info

	ISilDataAccessPtr qsdaTemp;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));

	// Process each possibility list.
	for (int ipssl = 0; ipssl < cpssl; ++ipssl)
	{
		HVO hvopssl = m_vpssl[ipssl];
		int cpss;
		CheckHr(qsdaTemp->get_VecSize(hvopssl, kflidPssIds, &cpss));
		HVO * prghvoUniq = NewObj HVO[cpss]; // Array of unique possibility ids.
		int ihvoUniq = 0;
		try
		{
			// Process each possibility in the list, copying unique values to array.
			for (int ipss = 0; ipss < cpss; ++ipss)
			{
				HVO pss;
				int ipssT = 0;
				CheckHr(qsdaTemp->get_VecItem(hvopssl, kflidPssIds, ipss, &pss));

				while (ipssT < ihvoUniq && pss != prghvoUniq[ipssT])
					++ipssT;
				if (ipssT == ihvoUniq)
				{
					bool fOkToInsert = false;

					// TODO TimP:  What should be done with pss = 0?
					if (pss >= 0)
					{
						// We have a unique value, so copy it.
						prghvoUniq[ihvoUniq++] = pss;
						// Save real values in a list for update test.
						fOkToInsert = true;
					}
					else if (pss > kidDummy) // if ((kidDummy < pss) && (pss < 0))
					{
						// this is a new one (see m_kidNew)
						ITsStringPtr qtss;
						CheckHr(qsdaTemp->get_StringProp(pss, kflidPssName, &qtss));

						OLECHAR * pchBuf;
						int cch;
						qtss->get_Length(&cch);
						if (cch == 0)
							continue;  // skip empty items
						StrUni stuTyped;
						stuTyped.SetSize(cch, &pchBuf);
						qtss->FetchChars(0, cch, pchBuf);

						if (! m_fHier)
						{
							// Colon is invalid when hierarchy is not being displayed.
							int ich = stuTyped.FindStr(L":");
							while (ich > 0)
							{
								stuTyped.Replace(ich,ich+1,"-");
								ich = stuTyped.FindStr(L":");
							}
						}

						// add new item to list
						AfLpInfo * plpi = GetLpInfo();
						AssertPtr(plpi);
						PossListInfoPtr qpli;
						plpi->LoadPossList(hvopssl, m_wsMagic, &qpli);
						int ipssTemp;
						bool bExists = !qpli->PossUniqueName(-1, stuTyped, m_pnt, ipssTemp);
						if (bExists)
						{
							// The Name is in the Poss List
							// Find the REAL pss to add to the cache
							PossItemInfo * pPiiTemp = qpli->GetPssFromIndex(ipssTemp);
							pss = pPiiTemp->GetPssId();
							// This code has found that the poss name is already in the poss list
							// and has replaced a m_kidNew pss with a REAL pss

							//
							int ipssTemp2 = 0;
							while (ipssTemp2 < ihvoUniq && pss != prghvoUniq[ipssTemp2])
								++ipssTemp2;
							if (ipssTemp2 == ihvoUniq)
							{
								// add it to the unique list.
								prghvoUniq[ihvoUniq++] = pss;
							}
							else
							{
								// The REAL pss is already in the cached "list"
								// so do not insert it.
								fOkToInsert = false;
							}
						}
						else
						{
							// The user created a new item, so add it to the possibility list.
							m_fSaving = true; // Disable updates while adding a new item.
							pss = CreatePss(qpli->GetPsslId(), pchBuf, m_pnt, m_fHier);
							m_fSaving = false;

							int itss; // Separate line to prevent error in release build.
							itss = qpli->GetIndexFromId(pss);

							Assert(itss >= 0); // What should we do if we have an error?
							// We have a new value, so add it to the unique list.
							prghvoUniq[ihvoUniq++] = pss;
							fOkToInsert = true;
						}
					}
					else
					{
						ihvoUniq++; // pss == kidDummy
					}
					if ((pss != kidDummy) && fOkToInsert)
					{
						// TODO TimP:  Should GetPossListAndItem() be called with a fourth argument?
						// If someone deleted an item in the list editor that we are
						// currently showing, we don't want to save this value any more.
						PossItemInfo * ppii;
						if (plpi->GetPossListAndItem(pss, m_wsMagic, &ppii))
							vpssNew.Push(pss); // It is in the poss list so add it
					}
				}
			}
			// If we deleted anything, cache the new list.
			if (cpss != ihvoUniq)
			{
				m_qvcd->CacheVecProp(hvopssl, kflidPssIds, &prghvoUniq[0], ihvoUniq);
				qsdaTemp->PropChanged(m_qrootb, kpctNotifyMeThenAll, hvopssl, kflidPssIds, 0,
					cpss, ihvoUniq);
			}
			delete[] prghvoUniq;
			prghvoUniq = NULL;
		}
		catch (...)
		{
			delete[] prghvoUniq;
			Assert(false);
			return false;
		}

	}

	// Check to see if we've made any changes to the property.
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	int cpssOrig;
	bool fNeedUpdate = false;
	CheckHr(qcvd->get_VecSize(m_hvoObj, m_flid, &cpssOrig));
	if (cpssOrig == vpssNew.Size())
		for (int ipss = 0; ipss < cpssOrig; ++ipss)
		{
			HVO hvo;
			CheckHr(qcvd->get_VecItem(m_hvoObj, m_flid, ipss, &hvo));
			if (hvo != vpssNew[ipss])
			{
				fNeedUpdate = true; // We made a change.
				break;
			}
		}
	else
		fNeedUpdate = true; // We made a change.

	// If we've changed anything, update the real cache.
	if (fNeedUpdate)
	{
		// Check if the record has been edited by someone else since we first loaded the data.
		HRESULT hrTemp;
		if ((hrTemp = qcvd->CheckTimeStamp(m_hvoObj)) != S_OK)
		{
			// If it was changed and the user does not want to overwrite it, perform a refresh
			// so the displayed field will revert to its original value.
			m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
			qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1);
			m_fDirty = false;
			return true;
		}

		// Update the value in the cache and refresh views.
		BeginChangesToLabel();
		if (vpssNew.Size())
		{
			CheckHr(qcvd->Replace(m_hvoObj, m_flid, 0, cpssOrig, vpssNew.Begin(), vpssNew.Size()));
		}
		else
		{
			CheckHr(qcvd->Replace(m_hvoObj, m_flid, 0, cpssOrig, NULL, 0));
		}

		CheckHr(qcvd->EndUndoTask());

		// Fake having replaced everything, since we don't know anything more exact.
		qcvd->PropChanged(m_qrootb, kpctNotifyAllButMe, m_hvoObj, m_flid, 0,
					cpssOrig, vpssNew.Size());

		// Notify other windows of the change.
		m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
		// NOTE:  Cannot pass GetOwner(), GetOwnerFlid() to UpdateAllDEWindows() because
		//			AfDeSplitChild::UpdateField() must be passed the roled participant.
	}
	m_fDirty = false;

	// We need to leave in a state that cancels undo actions since this may be called
	// without actually closing the edit box.
	BeginTempEdit();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Close the current editor, saving changes that were made.
	@param fForce True if we want to force the editor closed without making any
		validity checks or saving any changes.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::EndEdit(bool fForce)
{
	if (!fForce)
	{
		if (!SaveEdit())
		{
			Assert(false); // Something went wrong.
		}
	}

	CheckHr(m_qrootb->DelSelChngListener(m_qadsl));
	SuperClass::EndEdit(fForce);
	m_hwndButton = 0;
}


/*----------------------------------------------------------------------------------------------
	Move/resize/redraw the edit and button windows.
	@param rcClip The new location rect in AfDeSplitChild coordinates.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::MoveWnd(const Rect & rcClip)
{
	SuperClass::MoveWnd(rcClip);
	PlaceButton();
}


/*----------------------------------------------------------------------------------------------
	Creates a button if missing. Otherwise it makes sure the button is at the right of the,
	on the top line of the current possibility list. It also sets the current list so the
	correct chooser will be called.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::PlaceButton()
{
	// Get the selection to find out where we need to place the button.
	const int dxpButWid = 16;
	const int dxpButHeight = 15;
	IVwSelectionPtr qvwsel;
	Rect rcPara; // Paragraph location.
	Rect rcClient;
	::GetClientRect(m_hwnd, &rcClient);

	Rect rcBut(rcClient.right - dxpButWid,
		rcClient.top,
		rcClient.right,
		rcClient.top + Min((int)rcClient.bottom - (int)rcClient.top, (int)dxpButHeight));
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (qvwsel)
	{
		// We have a selection, so figure out which list it is in.
		CheckHr(qvwsel->GetParaLocation(&rcPara));
		rcBut.top = rcPara.top;
		rcBut.bottom = rcPara.top +
			Min((int)rcPara.bottom - (int)rcPara.top, (int)dxpButHeight);
		// Set the current possibility list.
		int cvsli;
		CheckHr(qvwsel->CLevels(false, &cvsli));
		// CLevels includes the string property itself, but AllTextSelInfo does not need it
		cvsli--;
		Assert((uint)cvsli < 3);
		VwSelLevInfo rgvsli[2];
		// Get selection information to determine where the user is typing.
		int ihvoObj;
		PropTag tagTextProp;
		int cpropPrevious;
		int ichAnchor;
		int ichEnd;
		int ws;
		ComBool fAssocPrev;
		int ihvoEnd;
		// ichAnchor (which should = ichEnd) gives an index to the cursor from the beginning of
		// the item. AllTextSelInfo returns an array of one or two VwSelLevInfo objects.
		// If on the list abbr, it gives one item. In a tag it gives two: rgvsli[0].ihvo
		// indicates which item in the list, and rgvsli[1].ihvo indicates which list.
		CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
			&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
		m_hvoPssl = m_vpssl[rgvsli[cvsli - 1].ihvo];
	}
	else
	{
		// We should never get here unless something really bizzare happens. We should always
		// have a selection in the field if the editor is active.
		Assert(false);
		// Set m_hvoPssl properly?
		// adjust rcBut better somehow?
		rcBut.top = rcClient.top;
		rcBut.bottom = rcClient.top +
			Min((int)rcClient.bottom - (int)rcClient.top, (int)dxpButHeight);
	}

	// Create the button if it isn't present.
	if (!m_hwndButton)
	{
		WndCreateStruct wcsButton;
		wcsButton.InitChild(_T("BUTTON"), m_hwnd, 1);
		wcsButton.style |= WS_VISIBLE | BS_OWNERDRAW;
		wcsButton.SetRect(rcBut);

		DftButtonPtr qdftb;
		qdftb.Create();
		qdftb->CreateAndSubclassHwnd(wcsButton);
		qdftb->m_pdft = this;
		m_hwndButton = qdftb->Hwnd();
	}
	else
	{
		// Otherwise, move the button.
		::MoveWindow(m_hwndButton, rcBut.left, rcBut.top, rcBut.Width(), rcBut.Height(), true);
	}
}


/*----------------------------------------------------------------------------------------------
	Saves the current cursor location in m_ichCurs.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::SaveCursor(int iFrom)
{
	if ((iFrom == 3) && m_fIgnoreNext_3)
	{
		m_fIgnoreNext_3 = false;

#ifdef DEBUG_THIS_FILE
		StrAnsi sta;
		sta.Format("AfDeFeTags::SaveCursor:  ignoring nCode equal to 3.\n");
		OutputDebugString(sta.Chars());
#endif
		return;
	}

	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	// what if nothing is selected ?
	Assert(qvwsel);

	// Commit the selection so that we can access the data from the cache.
	ComBool fOk;
	CheckHr(qvwsel->Commit(&fOk));
	CheckHr(m_qrootb->get_Selection(&qvwsel));

	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string property itself, but AllTextSelInfo does not need it
	Assert((uint)cvsli < 3);
	VwSelLevInfo rgvsli[2];
	// Get selection information to determine where the user is typing.
	int ihvoObj;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;
	// ichAnchor (which should = ichEnd) gives an index to the cursor from the beginning of
	// the item.
	CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

	// ichEnd can be wrong here (13 for example when "Unknown|Unknown" then "delete" last "n")
	// because "n" was deleted internally from both "Unknown" strings leaving "Unknow|Unknow".

#ifdef DEBUG_THIS_FILE
	StrAnsi sta;
	sta.Format("SaveCursor:  m_ichCurs=%d",
							m_ichCurs);
	OutputDebugString(sta.Chars());
#endif
	if (iFrom == 1)
	{
		m_ichCurs = ichAnchor;
#ifdef DEBUG_THIS_FILE
		sta.Format("=>%d", m_ichCurs);
		OutputDebugString(sta.Chars());
#endif
	}
	else if ((! m_fIgnoreNext_3) && (iFrom == 3))
	{
		m_ichCurs = ichAnchor;
#ifdef DEBUG_THIS_FILE
		sta.Format("=>%d", m_ichCurs);
		OutputDebugString(sta.Chars());
		sta.Format("; m_fSuggestedText set to false");
		OutputDebugString(sta.Chars());
#endif
		m_fSuggestedText = false;
	}
#ifdef DEBUG_THIS_FILE
	sta.Format("; ichAnchor=%d; ichEnd=%d; iFrom=%d.\n",
				ichAnchor,    ichEnd,    iFrom);
	OutputDebugString(sta.Chars());
#endif
}


/*----------------------------------------------------------------------------------------------
	Fire up the chooser with the original items selected. If the user makes a change,
	update the display cache appropriately, update the display and reset the insertion point.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::ProcessChooser()
{
	int cpssOld; // Number of items for this list prior to calling the chooser.
	int ipss;

	ISilDataAccessPtr qsdaTemp;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));

	CheckHr(qsdaTemp->get_VecSize(m_hvoPssl, kflidPssIds, &cpssOld));
	cpssOld--; // The last one is a blank, so ignore it.
	// Copy the original items into vpssOld.
	m_vpssOld.Clear();
	for (ipss = 0; ipss < cpssOld; ++ipss)
	{
		HVO pss;
		CheckHr(qsdaTemp->get_VecItem(m_hvoPssl, kflidPssIds, ipss, &pss));
		m_vpssOld.Push(pss);
	}

	// Open up the list chooser.
	PossChsrDlgPtr qplc;
	qplc.Create();
	qplc->SetDialogValues(m_hvoPssl, m_wsMagic, m_vpssOld);
	// Since this field editor may be deleted while the list chooser is up (e.g., sync), we
	// launch the chooser here and request it to call us back with ChooserApplied to finish
	// the processing. Nothing of any significance should be done in this method after
	// launching the dialog.
	qplc->SetFromEditor(true);
	qplc->DoModal(m_hwnd);
}


/*----------------------------------------------------------------------------------------------
	The Ok button in the list chooser has been hit. Process the results from the list chooser.
	@param pplc Pointer to the dialog box being closed.
	----------------------------------------------------------------------------------------------*/
void AfDeFeTags::ChooserApplied(PossChsrDlg * pplc)
{
	Vector<HVO> vpss; // Vector of items chosen by the user.
	int cpss; // Number of items chosen by the user.
	int cpssOld = m_vpssOld.Size();
	ISilDataAccessPtr qsdaTemp;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));

	// Process the items chosen by the user.
	pplc->GetDialogValues(vpss);
	// We want to set the dirty flag if the user has made any changes. If it is already set,
	// we can skip this test since we know the property is already dirty.
	if (!m_fDirty)
	{
		if (vpss.Size() != m_vpssOld.Size())
		{
			// The user made a change if the lists are of different size.
			m_fDirty = true;
		}
		else
		{
			int ipssOld;
			bool fFound;
			cpss = vpss.Size();
			// Go through the new items and make sure we have a corresponding
			// item in the old list for each one. If not, the user made a change.
			for (int ipss = 0; ipss < cpss; ++ipss)
			{
				fFound = false;
				for (ipssOld = 0; ipssOld < cpssOld; ++ipssOld)
				{
					if (vpss[ipss] == m_vpssOld[ipssOld])
					{
						fFound = true;
						break;
					}
				}
				if (!fFound)
				{
					m_fDirty = true;
					break;
				}
			}
		}
	}
	vpss.Push(kidDummy); // Add a blank one at the end.

	// Our previous cursor point may have been deleted, so always move the cursor
	// to the last dummy item to make sure we have a valid cursor.
	IVwSelectionPtr qvwsel;
	// If we rename an item from a chooser, in the process we have already killed this editor
	// and another editor has been opened. So we should just exit from this carcass here.
	if (!m_qrootb)
		return;
	CheckHr(m_qrootb->get_Selection(&qvwsel));

	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string prop itself, but AllTextSelInfo does not need it.
	Assert((uint)cvsli < 3);
	VwSelLevInfo rgvsli[2];
	// Get selection information to determine where the user is typing.
	int ihvoObj;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;
	// ichAnchor (which should = ichEnd) gives an index to the cursor from the beginning of
	// the item.
	CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
	if (tagTextProp == kflidPsslAbbr)
	{
		// We are in a pssl abbreviation, so move to the first tag.
		ichAnchor = 0;
		tagTextProp = kflidPssName;
		rgvsli[1].ihvo = rgvsli[0].ihvo;
		rgvsli[0].tag = kflidPssIds;
		rgvsli[1].tag = kflidPsslIds;
		rgvsli[1].cpropPrevious = kflidPssIds;
		cvsli++;
	}
	rgvsli[0].ihvo = vpss.Size() - 1;

	// Save the new ids in the display cache, then get new names/abbreviations.
	// Do this AFTER getting the selection info, as it may destroy the selection.
	// Do it BEFORE figuring the new selection, because that code depends on the
	// new value in m_qvcd.
	m_qvcd->CacheVecProp(m_hvoPssl, kflidPssIds, vpss.Begin(), vpss.Size());
	AddNames(m_hvoPssl);

	qsdaTemp->PropChanged(m_qrootb, kpctNotifyMeThenAll, m_hvoPssl, kflidPssIds, 0, vpss.Size(),
		cpssOld + 1);

	// I (KenZ) don't fully understand this. But if a user enters the chooser, and opens a
	// list editor from there and adds a new item, closes the list editor, checks the new
	// item in the chooser, selects OK, then moves to the next record without moving from
	// the field, the added item is lost. We get ksyncPossList and ksyncAddPss sync messages
	// from the list editor, but for some reason we are getting an extra ksyncPossList
	// message after this method completes. That extra message is calling ListChanged which
	// calls UpdateField, which reloads our temporary cache from the main cache and wipes out
	// the change we just made. So until we can do something better, we'll save the changes
	// here to make sure the UpdateField doesn't wipe out our change.
	SaveEdit();

	// Set to beginning of the last string.
	ichAnchor = 0;
	ichEnd = 0;

	// Now make the new selection. Do this AFTER updating the property, because it
	// depends on the display containing the new data so we can make a selection in
	// the right place.
	CheckHr(m_qrootb->MakeTextSelection(ihvoObj, cvsli, rgvsli, tagTextProp, cpropPrevious,
		ichAnchor, ichEnd, ws, true, ihvoEnd, NULL, true, NULL));
}

/*----------------------------------------------------------------------------------------------
	The user has modified an item in a list. Take what has been typed so far and try to find
	an appropriate item in the possibility list. If found, update the display cache to reflect
	the change and return selection indexes for the remainder of the word.
	If not found, underline the item with a red squiggly.
	@param ihvoPss Index for the possibility that was modified.
	@param ihvoPssl Index for the possibility list that owns ihvoPss.
	@param nChar The last key typed.
	@param pichAnchor Pointer to receive the beginning character index for the new selection.
	@param pichEnd Pointer to receive the ending character index for the new selection.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::FindAndSetPss(int ihvoPss, int ihvoPssl, uint nChar, int * pichAnchor,
	int * pichEnd)
{
	m_fDirty = true;
	ISilDataAccessPtr qsdaTemp;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));

	AssertPtr(pichAnchor);
	AssertPtr(pichEnd);
	Assert(*pichAnchor >= 0);
	Assert((uint)ihvoPssl < (uint)m_vpssl.Size());

	// The value passed is MORE reliable than the saved value, since it was immediately
	// obtained from the live selection. The saved value can be wrong, if the user's typing
	// caused something earlier in the paragraph to change, as when the same string occurs
	// twice in the paragraph (e.g., the same possibility shows up twice).

#ifdef DEBUG_THIS_FILE
	StrAnsi sta;
	sta.Format("FindAndSetPss:  ========== ==========\n");
	OutputDebugString(sta.Chars());
	if (m_fSuggestedText)
	{
		sta.Format("FindAndSetPss:  suggested text.\n");
		OutputDebugString(sta.Chars());
	}
	sta.Format("FindAndSetPss:  m_ichCurs=%d; *pichAnchor=%d; *pichEnd=%d; nChar=%d.\n",
								m_ichCurs,    *pichAnchor,    *pichEnd,    nChar);
	OutputDebugString(sta.Chars());
#endif
	HVO hvoPssl = m_vpssl[ihvoPssl];
	int chvoPss = 0; // The starting number of items.
	CheckHr(qsdaTemp->get_VecSize(hvoPssl, kflidPssIds, &chvoPss));
	// We should always have an empty slot at the end, so chvoPss should never be zero.
	Assert(chvoPss > 0);
	Assert((uint)ihvoPss < (uint)chvoPss);
	int chvoPssNew = chvoPss; // The final number of items after this edit.

	ITsStringPtr qtss;
	StrUni stuFound;
	// Use StrUni here so that user can paste in something huge without crashing.
	// Performance is not that critical here.
	StrUni stuTyped;
	HVO hvoPss;

	// Get the current item and its name as modified by the user
	CheckHr(qsdaTemp->get_VecItem(hvoPssl, kflidPssIds, ihvoPss, &hvoPss));
	CheckHr(qsdaTemp->get_StringProp(hvoPss, kflidPssName, &qtss));

	int cchTyped; // number of characters in the typed string
	// TODO TimP: does stuTyped contain the string after a backspace or delete key has been pressed?
	qtss->get_Length(&cchTyped);

	// Only allow the max number of charaters for a tag
	if (cchTyped > kcchPossNameAbbrMax)
	{
		// Truncate the item to the max characters allowed
		int iTmp = cchTyped - kcchPossNameAbbrMax;
		Assert(m_ichCurs > iTmp);
		if (m_ichCurs > iTmp)
		{
			m_ichCurs -= iTmp;
		}
		cchTyped = kcchPossNameAbbrMax;

		StrUni stuSel;
		OLECHAR * pchBuf;
		stuSel.SetSize(kcchPossNameAbbrMax, &pchBuf);
		qtss->FetchChars(0, kcchPossNameAbbrMax, pchBuf);

		// Get the writing system to use.
		int wsT;
		ITsTextPropsPtr qttp;
		qtss->get_PropertiesAt(0, &qttp);
		int nVar;
		CheckHr(qttp->GetIntPropValues(ktptWs, &nVar, &wsT));
		if (!wsT || wsT == -1)
			wsT = UserWs();

		ITsStrFactoryPtr qtsf;
		ITsStrBldrPtr qtsb;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		CheckHr(qtsf->GetBldr(&qtsb));
		ITsStringPtr qtssTrunc;
		CheckHr(qtsf->MakeStringRgch(stuSel.Chars(), stuSel.Length(), wsT, &qtssTrunc));

		HVO hvoT = kidDummy;
		CheckHr(qsdaTemp->Replace(hvoPssl, kflidPssIds, ihvoPss, ihvoPss + 1, &hvoT, 1));
		m_qvcd->CacheStringProp(hvoT, kflidPssName, qtssTrunc);
	}

	OLECHAR * pchBuf;
	stuTyped.SetSize(cchTyped, &pchBuf);
	qtss->FetchChars(0, cchTyped, pchBuf);

#ifdef DEBUG_THIS_FILE
	sta.Format("FindAndSetPss:  m_ichCurs=%d; cchTyped=%d; nChar=%d.\n",
								m_ichCurs,    cchTyped,    nChar);
	OutputDebugString(sta.Chars());
#endif

	bool fNeedCompare = true; // look for match except when deleting characters at end of current item
	// !! always compare ... in all cases

	bool fTypeAhead = false; // allow type ahead only when adding characters at end of current item
							// or backspacing at end of current item.

	if (nChar == 0) // (see kcidEditPaste special code)
	{
		// If we pasted something, force a compare.
//June5		fNeedCompare = true;
	}
	else if (nChar == 127)
	{
		fTypeAhead = false;
		if (*pichAnchor == cchTyped)
			fNeedCompare = false; // deleting character(s) at end of current item
		else
			fNeedCompare = true;
	}
//	else if (cchTyped == m_ichCurs && nChar != VK_DELETE && nChar != 127)
//  the delete key is 46 which is VK_DELETE (2E)
	else if ((cchTyped == m_ichCurs) && (nChar != kscDelForward))
	{
		// Need to compare if we typed a character and we are at the end of the item
		// Need to compare if we delete the last character in the non-type-ahead string
		fNeedCompare = true;
		fTypeAhead = true;
	}

	// Now we need to restore the string to its unedited form since it may occur multiple
	// times in the list, and when we are deleting, we could leave the duplicate copies
	// in a bad state.
	PossListInfoPtr qpli;
	PossItemInfo * ppii = NULL;
	AfLpInfo * plpi = GetLpInfo();
	AssertPtr(plpi);
	plpi->LoadPossList(hvoPssl, m_wsMagic, &qpli);
	AssertPtr(qpli);
	int ws = m_ws;
	if (hvoPss >= 0)
	{
		// not kidDummy nor a m_kidNew one
		int ipss = qpli->GetIndexFromId(hvoPss);
		ppii = qpli->GetPssFromIndex(ipss);
		AssertPtr(ppii);
		if (m_fHier)
			ppii->GetHierName(qpli, stuFound, m_pnt);
		else
			ppii->GetName(stuFound, m_pnt);
		ws = ppii->GetWs();
	}
	else
		stuFound = L"";
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	CheckHr(qtsf->MakeStringRgch(stuFound.Chars(), stuFound.Length(), ws, &qtss));
	m_qvcd->CacheStringProp(hvoPss, kflidPssName, qtss);

	int cch; // number of characters in ?
	cch = cchTyped;

#ifdef DEBUG_THIS_FILE
	sta.Format("FindAndSetPss:  m_ichCurs=%d; *pichAnchor=%d; cch=%d.\n",
								m_ichCurs,    *pichAnchor,    cch);
	OutputDebugString(sta.Chars());
#endif

	// Since the selection is deleted on backspace, we need to use
	// some extra logic to make backspace actually move the selection back.
	if (nChar == VK_BACK)
	{
		// JohnT: it's possible to have m_fSuggestedText true, not because we did a type-ahead fill-in
		// (and therefore had a range selection before the backspace), but just because the user
		// typed a letter previously that made an exact match, even though he may have typed
		// it in the middle of the word. For example, type a valid item, then put the cursor
		// in the middle, and delete a character. Without the second check, it moves disconcertingly
		// to one position short of the end.
		if (m_fSuggestedText && cch == m_ichCurs)
		{
			if (cch > 0)
			{
				--cch;
#ifdef DEBUG_THIS_FILE
				sta.Format("FindAndSetPss:  m_fSuggestedText ... decrement cch to %d.\n", cch);
				OutputDebugString(sta.Chars());
#endif
			}
		}
		else
		{
			// must work for "Case buy cas" delete "u"
			cch = m_ichCurs;
#ifdef DEBUG_THIS_FILE
			sta.Format("FindAndSetPss:  m_fSuggestedText ... set cch to m_ichCurs (%d).\n", cch);
			OutputDebugString(sta.Chars());
#endif
		}
	}
	else
	{
		cch = m_ichCurs;
#ifdef DEBUG_THIS_FILE
		sta.Format("FindAndSetPss:  6 - back NOT\n");
		OutputDebugString(sta.Chars());
#endif
	}

	fNeedCompare = true;
	ComBool fExactMatch = false;
	if (cchTyped == 0)
	{
		// If nothing to match, get the first item in the possibility list. If we are
		// already at that item, remove the item. If we are at the dummy item, do nothing.
		if (hvoPss == kidDummy)
			return;
		int cItems = qpli->GetCount();
		if (!cItems)
			return;
		ppii = qpli->GetPssFromIndex(0);
		if (ppii->GetPssId() == hvoPss)
		{
			CheckHr(qsdaTemp->Replace(hvoPssl, kflidPssIds, ihvoPss, ihvoPss + 1, NULL, 0));
			qsdaTemp->PropChanged(m_qrootb, kpctNotifyMeThenAll, hvoPssl, kflidPssIds, 0,
				chvoPssNew - 1, chvoPss);
			*pichAnchor = 0;
			*pichEnd = 0;
			return;
		}
	}
	else if (fNeedCompare)
	{
		// Try to find an item that matches what the user typed in the possibility list.
		StrUni stuMatch(stuTyped);
/////		stuMatch.Replace(cch, stuMatch.Length(), L"");// Delete chars to right of cursor.
		Locale loc = GetLpInfo()->GetLocale(m_ws);
		if (m_fHier)
		{
			ppii = qpli->FindPssHier(stuMatch.Chars(), loc, m_pnt, fExactMatch);
		}
		else
		{
			ppii = qpli->FindPss(stuMatch.Chars(), loc, m_pnt);
		}

		if (ppii)
		{
			// found a match that starts with stuTyped
			int ipssTemp;

			// TODO TimP:  check for hierarchy.  If stuTyped contains hierarchy,

			// Was the match exact (rather than just starting with stuTyped) ?
			if (fExactMatch) // May have matched in the FindPssHier() call above.
			{
#ifdef DEBUG_THIS_FILE
				sta.Format("FindAndSetPss:  Exact match (hier).\n");
				OutputDebugString(sta.Chars());
#endif
			}
			else
			{
				fExactMatch = !qpli->PossUniqueName(-1, stuTyped, m_pnt, ipssTemp);
				if (fExactMatch)
				{
#ifdef DEBUG_THIS_FILE
					sta.Format("FindAndSetPss:  Exact match.\n");
					OutputDebugString(sta.Chars());
#endif
					// in case FindPss() above matches "ABC" but "AB" is also in list.
					ppii = qpli->GetPssFromIndex(ipssTemp);
				}
				else
				{
#ifdef DEBUG_THIS_FILE
					sta.Format("FindAndSetPss:  Not exact match.\n");
					OutputDebugString(sta.Chars());
#endif
				}
			}
		}
	}
	else
		ppii = NULL;

#ifdef DEBUG_THIS_FILE
	if (fTypeAhead)
		sta.Format("FindAndSetPss:  Type Ahead.\n");
	else
		sta.Format("FindAndSetPss:  Not Type Ahead.\n");
	OutputDebugString(sta.Chars());
#endif

	int cchCurrent = 0; // number of characters in current item within the list.
	bool fNewItem = false;

	// added && fTypeAhead to cause "Case by Ce" delete last "e" to not replace with
	// stuFound below but does not work for "Case by cse" type "a" between "c" and "s";
	// added "|| ExactMatch" to try to fix this.
	if (ppii && (fTypeAhead || fExactMatch))
	{
#ifdef DEBUG_THIS_FILE
		sta.Format("FindAndSetPss:  Found\n");
		OutputDebugString(sta.Chars());
#endif

		// If found, store the new item.
		HVO hvoNew = ppii->GetPssId();
		CheckHr(qsdaTemp->Replace(hvoPssl, kflidPssIds, ihvoPss, ihvoPss + 1, &hvoNew, 1));
		if (m_fHier)
			ppii->GetHierName(qpli, stuFound, m_pnt);
		else
			ppii->GetName(stuFound, m_pnt);

		int ws;
		ws = ppii->GetWs();

		// If the last character was a delimiter (field separator), we need to set cch
		// accordingly.
		if (m_fHier && nChar == kchHierDelim)
		{
			// Need to set cch to the position of the last delimiter.
			cch = stuFound.FindCh(kchHierDelim, cch - 1) + 1;
		}

		// Note: there isn't any method to delete the old string, so it just hangs
		// around until the editor is closed. We could accumulate a fair number of these
		// since we'll likely get a different name for each keystroke. But still, we
		// probably aren't talking about more than a few dozen at the most.
		// Store the new item name.
		CheckHr(qtsf->MakeStringRgch(stuFound.Chars(), stuFound.Length(), ws, &qtss));
		m_qvcd->CacheStringProp(hvoNew, kflidPssName, qtss);

		// If we just added a new item, add another dummy to the end.
		if (ihvoPss == chvoPss - 1)
		{
			HVO hvoT = kidDummy;
			CheckHr(qsdaTemp->Replace(hvoPssl, kflidPssIds, chvoPss, chvoPss, &hvoT, 1));
			CheckHr(qtsf->MakeStringRgch(L"", 0, m_ws, &qtss));
			m_qvcd->CacheStringProp(hvoT, kflidPssName, qtss);
			chvoPssNew++;
		}
		cchCurrent = stuFound.Length();

#ifdef DEBUG_THIS_FILE
		sta.Format("FindAndSetPss:  cchCurrent=%d; stuTyped.Length()=%d; m_ichCurs=%d.\n",
									cchCurrent,    stuTyped.Length(),    m_ichCurs);
		OutputDebugString(sta.Chars());
#endif

		if (cchCurrent > stuTyped.Length())
		{
			// typed string is shorter than (but contained in) found string
			m_fSuggestedText = true;
			m_fIgnoreNext_3 = true;
#ifdef DEBUG_THIS_FILE
			sta.Format("FindAndSetPss:  1 - m_fSuggestedText set to true.\n");
			OutputDebugString(sta.Chars());
#endif
		}
		else if (m_ichCurs != stuTyped.Length())
		{
			m_fSuggestedText = true;
			m_fIgnoreNext_3 = true;
#ifdef DEBUG_THIS_FILE
			sta.Format("FindAndSetPss:  2 - m_fSuggestedText set to true.\n");
			OutputDebugString(sta.Chars());
#endif
		}
		else
		{
			m_fSuggestedText = false;
#ifdef DEBUG_THIS_FILE
			sta.Format("FindAndSetPss:  m_fSuggestedText set to false.\n");
			OutputDebugString(sta.Chars());
#endif
		}
	}
	else
	{
#ifdef DEBUG_THIS_FILE
		sta.Format("FindAndSetPss:  not found; ");
		OutputDebugString(sta.Chars());
		sta.Format("m_fSuggestedText set to false.\n");
		OutputDebugString(sta.Chars());
#endif

		m_fSuggestedText = false;
		cchCurrent = cch;
		// Something was typed that was not found; assume a new item is being added.
		fNewItem = true;
		if (m_ichCurs + 1 == cch && nChar != VK_BACK)
			::MessageBeep(MB_ICONEXCLAMATION); // Beep on the first unmatched character.
		// Underline the string with a red squiggly.
		ITsIncStrBldrPtr qtisb;
		qtisb.CreateInstance(CLSID_TsIncStrBldr);
		qtisb->SetIntPropValues(ktptWs, ktpvDefault, m_ws);
		CheckHr(qtisb->SetIntPropValues(ktptUnderColor, ktpvDefault, kclrRed));
		CheckHr(qtisb->SetIntPropValues(ktptUnderline, ktpvEnum, kuntSquiggle));
		qtisb->AppendRgch(stuTyped.Chars(), stuTyped.Length());
		ITsStringPtr qtss;
		qtisb->GetString(&qtss);
		HVO hvoT = m_kidNew++;

		m_qvcd->CacheStringProp(hvoT, kflidPssName, qtss);

		// Replace the original item with the new item.
		CheckHr(qsdaTemp->Replace(hvoPssl, kflidPssIds, ihvoPss, ihvoPss + 1, &hvoT, 1));

		// If we just added a new item, add another dummy to the end.
		if (ihvoPss == chvoPss - 1)
		{
			HVO hvoT = kidDummy;
			CheckHr(qsdaTemp->Replace(hvoPssl, kflidPssIds, chvoPss, chvoPss, &hvoT, 1));
			CheckHr(qtsf->MakeStringRgch(L"", 0, m_ws, &qtss));
			m_qvcd->CacheStringProp(hvoT, kflidPssName, qtss);
			chvoPssNew++;
		}
	}

	// Highlight word from cursor to end of word.

	*pichAnchor = cch;
	if (fTypeAhead)
		*pichEnd = cchCurrent;
	else
		*pichEnd = *pichAnchor;

#ifdef DEBUG_THIS_FILE
	sta.Format("FindAndSetPss:  m_ichCurs=%d; *pichAnchor=%d; *pichEnd=%d; cchPrev=%d; cch=%d.\n",
								m_ichCurs,    *pichAnchor,    *pichEnd,    cchPrev,    cch);
	OutputDebugString(sta.Chars());
#endif

	if (*pichAnchor > *pichEnd)
	{
		// This will happen when multiple characters are handled at once as a result of
		// the user typing too quickly.
//		*pichAnchor = *pichEnd;
		// This happens when a new character is added to the end of a string
		// ex old string ("Cz"); new string "Cza"
		*pichEnd = *pichAnchor;
	}

	qsdaTemp->PropChanged(m_qrootb, kpctNotifyMeThenAll, hvoPssl, kflidPssIds, 0, chvoPssNew,
		chvoPss);

	// If we modified the last dummy item, we need to refresh any other lists to make
	// them "forget" the old dummy string which was changed, and remember the new null string.
	if (ihvoPss == chvoPss - 1)
	{
		for (int i = 0; i < m_vpssl.Size(); ++i)
		{
			if (m_vpssl[i] != hvoPssl)
			{
				int chvo = 0;
				CheckHr(qsdaTemp->get_VecSize(m_vpssl[i], kflidPssIds, &chvo));
				qsdaTemp->PropChanged(m_qrootb, kpctNotifyMeThenAll, m_vpssl[i], kflidPssIds,
					chvo - 1, 1, 1);
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Create a new window for editing the contents.
	@param hwndParent The hwnd for the parent window.
	@param rcBounds The position of the new window relative to the parent window.
	@return A pointer to the new AfDeVwWnd window. The caller obtains one (and initially only)
		reference count to the window.
----------------------------------------------------------------------------------------------*/
AfDeVwWnd * AfDeFeTags::CreateEditWnd(HWND hwndParent, Rect & rcBounds)
{
	AfDeTagsWndPtr qdtw;
	qdtw.Attach(NewObj AfDeTagsWnd);

	// ENHANCE JohnT: could some or all of this be moved into BeginEdit so subclasses
	// don't have to mess with it? Maybe we could pass in wcs instead of rcBounds,
	// and have this method just call InitChild with appropriate parameters.
	WndCreateStruct wcs;
	wcs.InitChild(_T("AfVwWnd"), hwndParent, 0);
	wcs.style |=  WS_VISIBLE;
	wcs.SetRect(rcBounds);

	qdtw->m_pdfv = this; // It needs to know its field editor

	qdtw->CreateHwnd(wcs);
	return qdtw.Detach(); // Give the caller the initial ref count.
}


/*----------------------------------------------------------------------------------------------
	Move the cursor to the next item, if not at the end.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::NextItem()
{
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));

	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string prop itself, but AllTextSelInfo does not need it.
	Assert((uint)cvsli < 3);
	VwSelLevInfo rgvsli[2];
	// Get selection information to determine where the user is typing.
	int ihvoObj;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;
	// ichAnchor (which should = ichEnd) gives an index to the cursor from the beginning of
	// the item (either the abbreviation or the tags).
	CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

	if (tagTextProp == kflidPsslAbbr)
	{
		// We are in a pssl abbreviation, so move to the first tag.
		ichAnchor = 0;
		tagTextProp = kflidPssName;
		rgvsli[1].ihvo = rgvsli[0].ihvo;
		rgvsli[0].ihvo = 0;
		rgvsli[0].tag = kflidPssIds;
		rgvsli[1].tag = kflidPsslIds;
		rgvsli[1].cpropPrevious = kflidPssIds;
		cvsli++;
	}
	else
	{
		ISilDataAccessPtr qsdaTemp;
		CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));
		// We are in a normal tag. rgvsli[0].ihvo indicates which item in the list, and
		// rgvsli[1].ihvo indicates which list.
		int ipss = rgvsli[0].ihvo;
		int ipssl = rgvsli[1].ihvo;
		int chvoPss;
		CheckHr(qsdaTemp->get_VecSize(m_hvoPssl, kflidPssIds, &chvoPss));
		if (ipss < chvoPss - 1)
			rgvsli[0].ihvo++;
		else if (ipssl < m_vpssl.Size() - 1)
		{
			rgvsli[0].ihvo = 0;
			rgvsli[1].ihvo++;
			m_hvoPssl = m_vpssl[ipssl + 1];
		}

		// Set to the beginning of the current string.
		ichAnchor = 0;
	}
	ichEnd = 0;
	fAssocPrev = false; // False at beginning of para and true elsewhere.
	// Now make the new selection.
	CheckHr(m_qrootb->MakeTextSelection(ihvoObj, cvsli, rgvsli, tagTextProp, cpropPrevious,
		ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, NULL, true, NULL));
}


/*----------------------------------------------------------------------------------------------
	Move the cursor to the previous item, if not at the beginning.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::PrevItem()
{
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));

	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string property itself, but AllTextSelInfo does not need it
	Assert((uint)cvsli < 3);
	VwSelLevInfo rgvsli[2];
	// Get selection information to determine where the user is typing.
	int ihvoObj;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;
	ISilDataAccessPtr qsdaTemp;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));
	// ichAnchor (which should = ichEnd) gives an index to the cursor from the beginning of
	// the item.
	CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

	if (tagTextProp == kflidPsslAbbr)
	{
		// We are in a pssl abbreviation. Pretend we are at the beginning of the first
		// tag for that pssl and take the normal action.
		ichAnchor = 0;
		tagTextProp = kflidPssName;
		rgvsli[1].ihvo = rgvsli[0].ihvo;
		rgvsli[0].ihvo = 0;
		rgvsli[0].tag = kflidPssIds;
		rgvsli[1].tag = kflidPsslIds;
		rgvsli[1].cpropPrevious = kflidPssIds;
		cvsli++;
	}

	int ipss = rgvsli[0].ihvo;
	int ipssl = rgvsli[1].ihvo;
	int chvoPss;
	if (ipss > 0)
		rgvsli[0].ihvo--;
	else if (ipssl > 0)
	{
		rgvsli[1].ihvo--;
		m_hvoPssl = m_vpssl[--ipssl];
		CheckHr(qsdaTemp->get_VecSize(m_hvoPssl, kflidPssIds, &chvoPss));
		rgvsli[0].ihvo = chvoPss - 1;
	}

	// Get the offset for the beginning of the current string.
	ichAnchor = 0;
	ichEnd = 0;
	fAssocPrev = false; // False at beginning of item.
	// Now make the new selection.
	CheckHr(m_qrootb->MakeTextSelection(ihvoObj, cvsli, rgvsli, tagTextProp, cpropPrevious,
		ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, NULL, true, NULL));
}


/*----------------------------------------------------------------------------------------------
	Retrieve the current item and list indexes.
	@param pipss Pointer to receive the item index.
	@param pipssl Pointer to receive the list index.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::GetCurrentItem(int * pipss, int * pipssl)
{
	AssertPtr(pipss);
	AssertPtr(pipssl);

	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));

	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string prop itself, but AllTextSelInfo does not need it.
	Assert((uint)cvsli < 3);
	VwSelLevInfo rgvsli[2];
	// Get selection information to determine where the user is typing.
	int ihvoObj;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;
	// ichAnchor (which should = ichEnd) gives an index to the cursor from the beginning of
	// the item (either the abbreviation or the tags).
	CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
	if (tagTextProp == kflidPsslAbbr)
	{
		// We are in a pssl abbreviation.
		*pipss = -1;
		*pipssl = rgvsli[0].ihvo;
	}
	else
	{
		// We are in a normal tag.
		*pipss = rgvsli[0].ihvo;
		*pipssl = rgvsli[1].ihvo;
	}
}


/*----------------------------------------------------------------------------------------------
	When the selection is changed, it propagates this to its site.
	TODO JohnT: when VwRootBox can be changed, make it do both kinds of change notification
	in MakeSimpleSelAt; then this is (probably) not necessary.
	@param pvwselNew Pointer to the new selection.
	@return S_OK indicating success.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeTags::SelectionChanged(IVwRootBox * prootb, IVwSelection * pvwselNew)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwselNew);

	m_fSuggestedText = false;

#ifdef DEBUG_THIS_FILE
	StrAnsi sta;
	sta.Format("AfDeFeTags::SelectionChanged:   m_fSuggestedText set to false.\n");
	OutputDebugString(sta.Chars());
#endif

	PlaceButton();
	SaveCursor(3);
	return S_OK;

	END_COM_METHOD(g_fact2, IID_IVwRootSite);
}


//:>********************************************************************************************
//:>	AfDeFeTags::AfDeSelListener methods.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Do a standard COM query interface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeTags::AfDeSelListener::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IEventListener)
		*ppv = static_cast<IEventListener *>(this);
	else
		return E_NOINTERFACE;

	AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	This gets called when the selection changes. It updates the cursor and button.
	@param nCode Indicates the change that was made:
		1 if it moved within the same paragraph.
		2 if the selection moved to a different paragraph,
		3 if a completely new selection was installed (may be same para or different)
		4 if the selection was destroyed (may be followed by another notification)
	@param nArg2 (unused here).
	@return S_OK indicating success.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeTags::AfDeSelListener::Notify(int nCode, int nArg2)
{
	BEGIN_COM_METHOD
// TODO TimP:  Should 1 and 4 below be replaced with constants?

#ifdef DEBUG_THIS_FILE
	StrAnsi sta;
	sta.Format("Notify:  nCode=%d; nArg2=%d.\n",
						nCode,    nArg2);
	OutputDebugString(sta.Chars());
#endif

	if (nCode < 4)
	{
		m_pdft->PlaceButton();
		m_pdft->SaveCursor(nCode);
	}

	return S_OK;

	END_COM_METHOD(g_fact2, IID_IVwRootSite);
}


//:>********************************************************************************************
//:>	AfDeFeTags::DftButton methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Handle window painting (WM_PAINT). We use owner-draw on this button to get the desired ...
	@param pdis Pointer to a struct giving information needed for owner-draw.
	@return True indicating it was processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeTags::DftButton::OnDrawThisItem(DRAWITEMSTRUCT * pdis)
{
	// Note: Using a Rectangle over an exisitng background didn't work on one monitor.
	// Also, using a standard button didn't work properly when it was clicked.
	AssertObj(this);
	AssertPtr(pdis);
	HDC hdc = pdis->hDC;
	// Draw the button.
	Rect rc(pdis->rcItem);
	Rect rcDot;
	Rect rcT;
	if (pdis->itemState & ODS_SELECTED)
	{
		AfGfx::FillSolidRect(hdc, rc, ::GetSysColor(COLOR_3DFACE));
		::DrawEdge(hdc, &rc, EDGE_SUNKEN, BF_RECT);
		rcDot.left = rc.Width() / 2 - 4;
		rcDot.top = rc.bottom - 5;
	}
	else
	{
		AfGfx::FillSolidRect(hdc, rc, ::GetSysColor(COLOR_3DFACE));
		::DrawEdge(hdc, &rc, EDGE_RAISED, BF_RECT);
		rcDot.left = rc.Width() / 2 - 5;
		rcDot.top = rc.bottom - 6;
	}

	// Draw the dots.
	const int kclrText = ::GetSysColor(COLOR_BTNTEXT);
	rcDot.right = rcDot.left + 2;
	rcDot.bottom = rcDot.top + 2;
	AfGfx::FillSolidRect(hdc, rcDot, kclrText);
	rcDot.Offset(4, 0);
	AfGfx::FillSolidRect(hdc, rcDot, kclrText);
	rcDot.Offset(4, 0);
	AfGfx::FillSolidRect(hdc, rcDot, kclrText);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Return the what's-this help string for the Chooser Dialog ellipsis button.

	@param pt not used.
	@param pptss Address of a pointer to an ITsString COM object for returning the help string.

	@return true.
----------------------------------------------------------------------------------------------*/
bool AfDeFeTags::DftButton::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	StrApp str;
	str.Load(kstidEllipsisButtonWhatsThisHelp); // No context help available
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(str);
	CheckHr(qtsf->MakeString(stu.Bstr(), m_pdft->UserWs(), pptss));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Check the requirments of the FldSpec, and verify that data in the field meets the
	requirement. It returns:
		kFTReqNotReq if the all requirements are met.
		kFTReqWs if data is missing, but it is encouraged.
		kFTReqReq if data is missing, but it is required.
----------------------------------------------------------------------------------------------*/
FldReq AfDeFeTags::HasRequiredData()
{
	if (m_qfsp->m_fRequired == kFTReqNotReq)
		return kFTReqNotReq;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	// We need to check the temporary cache for items rather than the main cache since we may
	// not have changed the items yet.
	ISilDataAccessPtr qsdaTemp;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));
	int cpssl = m_vpssl.Size();
	bool fEmpty = true;
	// Process each possibility list.
	for (int ipssl = 0; ipssl < cpssl; ++ipssl)
	{
		HVO hvopssl = m_vpssl[ipssl];
		int cpss;
		CheckHr(qsdaTemp->get_VecSize(hvopssl, kflidPssIds, &cpss));
		if (cpss > 1) // We always have a dummy.
		{
			fEmpty = false;
			break;
		}
	}
	if (fEmpty)
		return m_qfsp->m_fRequired;
	else
		return kFTReqNotReq;
}


/*----------------------------------------------------------------------------------------------
	Something has changed in the possibility list.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::ListChanged(int nAction, HVO hvoPssl, HVO hvoSrc, HVO hvoDst, int ipssSrc,
	int ipssDst)
{
	// We don't want to update the field while we are in the middle of saving.
	if (!m_fSaving)
		UpdateField();
}


/*----------------------------------------------------------------------------------------------
	This method saves the current cursor information in RecMainWnd. Normally it just
	stores the cursor index in RecMainWnd::m_ichCur. For structured texts, however,
	it also inserts the appropriate hvos and flids for the StText classes in
	m_vhvoPath and m_vflidPath. Other editors may need to do other things.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::SaveCursorInfo()
{
	// Store the current record/subrecord and field info.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (!prmw)
		return;
	try
	{
		// Get the selection information.
		IVwSelectionPtr qvwsel;
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
			return; // No selection.
		int csli;
		CheckHr(qvwsel->CLevels(false, &csli));
		if (!csli)
			return; // Some strange selection, perhaps a literal string, can't handle as yet.
		int ichAnchor;
		int ichEnd;
		CheckHr(qvwsel->get_ParagraphOffset(0, &ichAnchor));
		CheckHr(qvwsel->get_ParagraphOffset(1, &ichEnd));
		// We don't want the superclass method here because it gets dummy internal structure.
		// If we ever start using multiple lists again, we'll need to fix the index for lists
		// beyond the first one.
		prmw->SetCursorIndex(Min(ichAnchor, ichEnd));
	}
	catch (...)
	{
	}
}


/*----------------------------------------------------------------------------------------------
	This attempts to place the cursor as defined in RecMainWnd m_vhvoPath, m_vflidPath,
	and m_ichCur.
	@param vhvo Vector of ids inside the field.
	@param vflid Vector of flids inside the field.
	@param ichCur Character offset in the final field for the cursor.
----------------------------------------------------------------------------------------------*/
void AfDeFeTags::RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur)
{
	if (m_qrootb)
	{
		// We need to determine which item ichCur is in to get the index set up correctly
		// in rgvsli. This needs to be smarter if we return to allowing multiple lists.
		ISilDataAccessPtr qsdaTemp;
		int cchTotal = 0; // Accumulated position.
		int cpss;
		HVO hvopssl = m_vpssl[0]; // Assume the first list until we start using more.
		CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));
		CheckHr(qsdaTemp->get_VecSize(hvopssl, kflidPssIds, &cpss));
		int ipss = 0;
		for (; ipss < cpss; ++ipss)
		{
			ITsStringPtr qtss;
			int cch;
			HVO hvoPss;
			CheckHr(qsdaTemp->get_VecItem(hvopssl, kflidPssIds, ipss, &hvoPss));
			CheckHr(qsdaTemp->get_StringProp(hvoPss, kflidPssName, &qtss));
			CheckHr(qtss->get_Length(&cch));
			cchTotal += cch;
			if (ichCur <= cchTotal)
				break;
			++cchTotal; // Add one for the vertical gray bar (field separator).
		}

		VwSelLevInfo rgvsli[2];
		rgvsli[0].tag = kflidPssIds; // Set up the index for the ids.
		rgvsli[0].cpropPrevious = 0;
		rgvsli[0].ihvo = ipss;
		// Use the first possibility list for now. Need to fix if we allow multiple lists.
		rgvsli[1].tag = kflidPsslIds;
		rgvsli[1].cpropPrevious = 0;
		rgvsli[1].ihvo = 0;
		IVwSelectionPtr qsel;
		if (vflid.Size())
		{
			m_qrootb->MakeTextSelection(
				0, // int ihvoRoot
				2, // int cvlsi,
				rgvsli, // Skip the first one -- VwSelLevInfo * prgvsli
				kflidPssName, // int tagTextProp,
				0, // int cpropPrevious,
				cchTotal - ichCur, // int ichAnchor in current item,
				cchTotal - ichCur, // int ichEnd in current item,
				0, // int ws,
				true, // ComBool fAssocPrev,
				-1, // int ihvoEnd,
				NULL, // ITsTextProps * pttpIns,
				true, // ComBool fInstall,
				&qsel); // IVwSelection ** ppsel
		}
		// If we didn't get a text selection, try getting a selection somewhere close.
		if (!qsel)
		{
			m_qrootb->MakeTextSelInObj(
				0,  // index of the one and only root object in this view
				2, // the object we want is one level down
				rgvsli, // and here's how to find it there
				0,
				NULL, // don't worry about the endpoint
				true, // select at the start of it
				true, // Find an editable field
				false, // and don't select a range.
				// Making this true, allows the whole record to scroll into view when we launch
				// a new window by clicking on a reference to an entry, but we don't get an insertion
				// point. Using false gives an insertion point, but the top of the record is typically
				// at the bottom of the screen, which isn't good.
				false, // don't select the whole object
				true, // but do install it as the current selection
				NULL); // and don't bother returning it to here. */
		}
	}
}


//:>********************************************************************************************
//:>	AfDeTagsWnd methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Process commands. We just want to intercept BN_CLICKED to pass it to AfDeFeTags.
	See ${AfWnd#OnCommand} for parameter and return descriptions.
----------------------------------------------------------------------------------------------*/
bool AfDeTagsWnd::OnCommand(int cid, int nc, HWND hctl)
{
#ifdef DEBUG_THIS_FILE
	StrAnsi sta;
	sta.Format("AfDeTagsWnd::OnCommand:  cid=%d; nc=%d.\n",
										cid,    nc);
	OutputDebugString(sta.Chars());
#endif

	// Process a button click by passing it on to AfDeFeTags
	if (nc == BN_CLICKED)
	{
		AfDeFeTags * pdft = dynamic_cast<AfDeFeTags *>(m_pdfv);
		if (pdft)
			pdft->ProcessChooser();
		return true;
	}

	return SuperClass::OnCommand(cid, nc, hctl);
}


/*----------------------------------------------------------------------------------------------
	Process character being typed. We need to do a name lookup for each character typed.
	See ${AfVwRootSite#OnChar} for parameter descriptions.
	@param nChar Character code of the key pressed.
	@param nRepCnt Not used locally; passed on to SuperClass::OnChar() method.
	@param nFlags Not used locally; passed on to SuperClass::OnChar() method.
----------------------------------------------------------------------------------------------*/
void AfDeTagsWnd::OnChar(UINT nChar, UINT nRepCnt, UINT nFlags)
{
#ifdef DEBUG_THIS_FILE
	StrAnsi sta;
	sta.Format("AfDeTagsWnd::OnChar:  nChar=%d; nRepCnt=%d; nFlags=%d.\n",
									nChar,    nRepCnt,    nFlags);
	OutputDebugString(sta.Chars());
#endif

	// Tab is handled in FWndProc but still comes through here when we don't move to
	// another field. So we never want to process Tab here. We also should ignore Escape, and
	// any other control characters, except BS.
	if (nChar != VK_BACK && nChar < VK_SPACE)
		return;

	// If Backspace or Delete and our selection spans one or more whole items, we want to delete
	// the items.
	if ((nChar == VK_BACK) || (nChar == kscDelForward))
	{
		if (OnCut())
			return; // Item(s) deleted.
	}
	else
	{
		// For normal characters, we delete any multiple objects, and then go ahead with typing.
		OnCut();
	}

	SuperClass::OnChar(nChar, nRepCnt, nFlags);

	// Commit the selection, and get the results.
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));

	// Commit the selection so that we can access the data from the cache.
	ComBool fOk;
	CheckHr(qvwsel->Commit(&fOk));
	CheckHr(m_qrootb->get_Selection(&qvwsel));

	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string prop itself, but AllTextSelInfo does not need it.
	Assert((uint)cvsli < 3);
	VwSelLevInfo rgvsli[2];

	// Get selection information to determine where the user is typing.
	int ihvoObj;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;
	// ichAnchor (which should = ichEnd) gives an index to the cursor from the beginning of
	// the item.
	CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

	// If we are in the abbreviation, ignore the typing.
	if (tagTextProp == kflidPsslAbbr)
		return;

	// Go find and set a possibility item based on what the user typed.
	AfDeFeTags * pdft = dynamic_cast<AfDeFeTags *>(m_pdfv);
	AssertPtr(pdft);
	Assert(ichAnchor == ichEnd); // I don't think this should ever be different.
	// We need to destroy the selection because otherwise, when we destroy it, it commits
	// pending changes which sets the name of the item back to the thing we last typed,
	// defeating the changes FindAndSetPss is trying to make.
	CheckHr(m_qrootb->DestroySelection());
	pdft->FindAndSetPss(rgvsli[0].ihvo, rgvsli[1].ihvo, nChar, &ichAnchor, &ichEnd);
	fAssocPrev = ichAnchor; // False at beginning of paragraph and true elsewhere.
	// Now make the new selection.
	CheckHr(m_qrootb->MakeTextSelection(ihvoObj, cvsli, rgvsli, tagTextProp, cpropPrevious,
		ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, NULL, true, NULL));
}

/*----------------------------------------------------------------------------------------------
	When selecting over a range of tags, extend the selection to include the entire range
	of the strings.
----------------------------------------------------------------------------------------------*/
void AfDeTagsWnd::CallMouseUp(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot)
{
	SuperClass::CallMouseUp(xp, yp, rcSrcRoot, rcDstRoot);

	// Extend the selection?

	IVwSelectionPtr qsel;
	m_qrootb->get_Selection(&qsel);
	if (!qsel)
		return;

	ComBool fBogus;
	IVwPropertyStorePtr qvpsBogus;

	ITsStringPtr qtssAnchor;
	HVO hvoAnchor;
	PropTag tagAnchor;
	int ihvoAnchor, cpropPrevAnchor;
//	CheckHr(qsel->TextSelInfo(false, &qtssAnchor, &ichAnchor, &fBogus,
//		&hvoAnchor, &tagAnchor, &encAnchor));
	CheckHr(qsel->PropInfo(false, 1, &hvoAnchor, &tagAnchor, &ihvoAnchor, &cpropPrevAnchor,
		&qvpsBogus));

	ITsStringPtr qtssEnd;
	HVO hvoEnd;
	PropTag tagEnd;
	int ihvoEnd, cpropPrevEnd;
//	CheckHr(qsel->TextSelInfo(true, &qtssEnd, &ichEnd, &fBogus,
//		&hvoEnd, &tagEnd, &encEnd));
	CheckHr(qsel->PropInfo(true, 1, &hvoEnd, &tagEnd, &ihvoEnd, &cpropPrevEnd,
		&qvpsBogus));

	if (hvoAnchor != hvoEnd)
	{
		Assert(false);
		return;
	}
	if (ihvoAnchor == ihvoEnd)
		return;	// selection within a single string: no change

	// Now adjust the selection.
	CheckHr(qsel->ExtendToStringBoundaries());
}

/*----------------------------------------------------------------------------------------------
	Process window messages. We intercept Alt+Down to open the chooser and Tab with other
	keys to move the selection or move to a new field.
	See ${AfWnd#FWndProc} for parameter and return descriptions.
----------------------------------------------------------------------------------------------*/
bool AfDeTagsWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
#ifdef DEBUG_THIS_FILE
	StrAnsi sta;
	sta.Format("AfDeTagsWnd::FWndProc:  wm=%d; wp=%d; lp=%d; lnRet=%d.\n",
										wm,    wp,    lp,    lnRet);
	OutputDebugString(sta.Chars());
#endif

	switch (wm)
	{
	case WM_SYSKEYDOWN:
		// Alt+Up and Alt+Down opens chooser.
		if (wp == VK_DOWN || wp == VK_UP)
		{
			AfDeFeTags * pdft = dynamic_cast<AfDeFeTags *>(m_pdfv);
			AssertPtr(pdft);
			pdft->ProcessChooser();
			return true;
		}

	case WM_KEYDOWN:
		{
			switch (wp)
			{
			case VK_LEFT:
			case VK_UP:
			case VK_RIGHT:
			case VK_DOWN:
				{
					AfDeFeTags * pdft = dynamic_cast<AfDeFeTags *>(m_pdfv);
					AssertPtr(pdft);
					pdft->SetSuggestedTextFlag(false);
#ifdef DEBUG_THIS_FILE
					StrAnsi sta;
					sta.Format("FWndProc:  m_fSuggestedText set to false.\n");
					OutputDebugString(sta.Chars());
#endif
					break;
				}
			case VK_TAB:
				{
					AfDeSplitChild * padsc = dynamic_cast<AfDeSplitChild *>(
						AfWnd::GetAfWnd(::GetParent(m_hwnd)));
					AssertPtr(padsc);
					int nShift = ::GetKeyState(VK_SHIFT);
					if (::GetKeyState(VK_CONTROL) < 0)
					{
						AfDeFeTags * pdft = dynamic_cast<AfDeFeTags *>(m_pdfv);
						AssertPtr(pdft);
						int ipss0;
						int ipssl0;
						int ipss;
						int ipssl;
						pdft->GetCurrentItem(&ipss0, &ipssl0);
						if (nShift < 0)
							pdft->PrevItem(); // Move back one item.
						else
							pdft->NextItem(); // Move forward one item.
						pdft->GetCurrentItem(&ipss, &ipssl);
						if (ipss == ipss0 && ipssl == ipssl0)
						{
							if (nShift < 0)
								padsc->OpenPreviousEditor(); // move to previous editor.
							else
								padsc->OpenNextEditor(); // move to next editor.
						}
					}
					else if (nShift < 0)
						padsc->OpenPreviousEditor(); // Shift Tab to previous editor.
					else
						padsc->OpenNextEditor(); // Tab to next editor.
					return true;
				}

			default:
				break;
			}
		}

	default:
		break;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Enable/Disable Format toolbar buttons (and the format font command).
	All these are disabled in a tags field; there is nothing here we should be editing, except
	pseudo-editing to select something.
	@param cms The command state object.
	@return True indicating it was handled.
----------------------------------------------------------------------------------------------*/
bool AfDeTagsWnd::CmsCharFmt(CmdState & cms)
{
	if (cms.Cid() == kcidFmtStyles)
		cms.Enable(true);
	else
		cms.Enable(false);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Process the Edit...Cut/Paste commands.
	At some future point we may want to handle the capability to paste a text containing a
	sequence of items, but at this point we only paste the contents into a single item and
	try to parse it from there.
	@return true indicating it was handled or false indicating it wasn't handled.
----------------------------------------------------------------------------------------------*/
bool AfDeTagsWnd::CmdEdit(Cmd * pcmd)
{
#ifdef DEBUG_THIS_FILE
	StrAnsi sta;
	sta.Format("AfDeTagsWnd::CmdEdit:  pcmd->m_cid=%d.\n",
									pcmd->m_cid);
	OutputDebugString(sta.Chars());
#endif

	switch (pcmd->m_cid)
	{
	case kcidEditCut:
		{
			SuperClass::CmdEditCopy(pcmd);
			if (OnCut())
				return true; // Deleted whole items.
			SuperClass::CmdEditCut(pcmd); // Use normal processing.

			// Commit the selection, and get the results.
			IVwSelectionPtr qvwsel;
			ComBool fOk;
			CheckHr(m_qrootb->get_Selection(&qvwsel));

			// Commit the selection so that we can access the data from the cache.
			CheckHr(qvwsel->Commit(&fOk));
			CheckHr(m_qrootb->get_Selection(&qvwsel));

			int cvsli;
			CheckHr(qvwsel->CLevels(false, &cvsli));
			// CLevels includes the string prop itself, but AllTextSelInfo does not need it.
			cvsli--;
			Assert((uint)cvsli < 3);
			VwSelLevInfo rgvsli[2];
			// Get selection information to determine where the user is typing.
			int ihvoObj;
			PropTag tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			ComBool fAssocPrev;
			int ihvoEnd;
			// ichAnchor (which should = ichEnd) gives an index to the cursor from the
			// beginning of the item.
			CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp,
				&cpropPrevious, &ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

			// If we are in the abbreviation, ignore the typing.
			if (tagTextProp == kflidPsslAbbr)
				return true;

			// Go find and set a possibility item based on what the user typed.
			AfDeFeTags * pdft = dynamic_cast<AfDeFeTags *>(m_pdfv);
			AssertPtr(pdft);
			Assert(ichAnchor == ichEnd); // I don't think this should ever be different.
			// We need to destroy the selection because otherwise, when we destroy it, it
			// commits pending changes which sets the name of the item back to the thing we
			// last typed, defeating the changes FindAndSetPss is trying to make.
			CheckHr(m_qrootb->DestroySelection());
			pdft->FindAndSetPss(rgvsli[0].ihvo, rgvsli[1].ihvo, 0, &ichAnchor, &ichEnd);
			fAssocPrev = ichAnchor; // False at beginning of paragraph and true elsewhere.
			// Now make the new selection.
			CheckHr(m_qrootb->MakeTextSelection(ihvoObj, cvsli, rgvsli, tagTextProp,
				cpropPrevious, ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, NULL, true, NULL));
			return true;
		}

	case kcidEditPaste:
		{
			SuperClass::CmdEditPaste(pcmd);
			// Commit the selection, and get the results.
			IVwSelectionPtr qvwsel;
			ComBool fOk;
			CheckHr(m_qrootb->get_Selection(&qvwsel));

			// Commit the selection so that we can access the data from the cache.
			CheckHr(qvwsel->Commit(&fOk));
			CheckHr(m_qrootb->get_Selection(&qvwsel));

			int cvsli;
			CheckHr(qvwsel->CLevels(false, &cvsli));
			// CLevels includes the string prop itself, but AllTextSelInfo does not need it.
			cvsli--;
			Assert((uint)cvsli < 3);
			VwSelLevInfo rgvsli[2];
			// Get selection information to determine where the user is typing.
			int ihvoObj;
			PropTag tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			ComBool fAssocPrev;
			int ihvoEnd;
			// ichAnchor (which should = ichEnd) gives an index to the cursor from the
			// beginning of the paragraph.
			CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp,
				&cpropPrevious, &ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

			// If we are in the abbreviation, ignore the typing.
			if (tagTextProp == kflidPsslAbbr)
				return true;

			// Go find and set a possibility item based on what the user typed.
			AfDeFeTags * pdft = dynamic_cast<AfDeFeTags *>(m_pdfv);
			AssertPtr(pdft);
			Assert(ichAnchor == ichEnd); // I don't think this should ever be different.
			// We need to destroy the selection because otherwise, when we destroy it, it
			// commits pending changes which sets the name of the item back to the thing we
			// last typed, defeating the changes FindAndSetPss is trying to make.
			CheckHr(m_qrootb->DestroySelection());
			pdft->FindAndSetPss(rgvsli[0].ihvo, rgvsli[1].ihvo, 0, &ichAnchor, &ichEnd);
			fAssocPrev = ichAnchor; // False at beginning of paragraph and true elsewhere.
			// Now make the new selection.
			CheckHr(m_qrootb->MakeTextSelection(ihvoObj, cvsli, rgvsli, tagTextProp,
				cpropPrevious, ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, NULL, true, NULL));
			return true;
		}

	default:
		Assert(false); // We shouldn't get here.
		return false;
	}
}


/*----------------------------------------------------------------------------------------------
	Process Cut/Bsp/Del when selection covers one or more whole items. If the selection does
	not cover whole item(s), do nothing.
	@return true if we processed the deletion. false if nothing was done.
----------------------------------------------------------------------------------------------*/
bool AfDeTagsWnd::OnCut()
{
#ifdef DEBUG_THIS_FILE
	StrAnsi sta;
	sta.Format("AfDeTagsWnd::OnCut.\n");
	OutputDebugString(sta.Chars());
#endif

	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	ComBool fIsRange;
	CheckHr(qvwsel->get_IsRange(&fIsRange));
	if (!fIsRange)
		return false; // Use default processing.

	ITsStringPtr qtssSel;
	ITsStringPtr qtssAnchor;
	ITsStringPtr qtssEnd;
	int ichAnchor;
	int ichEnd;
	HVO hvoAnchor;
	HVO hvoEnd;
	ComBool fAssocPrev;
	PropTag tagAnchor;
	PropTag tagEnd;
	int encAnchor;
	int encEnd;

	SmartBstr sbstr = L"; ";
	CheckHr(qvwsel->GetSelectionString(&qtssSel, sbstr));
	CheckHr(qvwsel->TextSelInfo(false, &qtssAnchor, &ichAnchor, &fAssocPrev, &hvoAnchor,
		&tagAnchor, &encAnchor));
	CheckHr(qvwsel->TextSelInfo(true, &qtssEnd, &ichEnd, &fAssocPrev, &hvoEnd,
		&tagEnd, &encEnd));

	HVO hvoOfList;
	PropTag tagOfList;
	int ihvoAnchor;
	int ihvoEnd;
	int cpropPrevAnchor;
	int cpropPrevEnd;
	IVwPropertyStorePtr qvpsBogus;

	CheckHr(qvwsel->PropInfo(false, 1, &hvoOfList, &tagOfList, &ihvoAnchor, &cpropPrevAnchor,
		&qvpsBogus));
	CheckHr(qvwsel->PropInfo(true, 1, &hvoOfList, &tagOfList, &ihvoEnd, &cpropPrevEnd,
		&qvpsBogus));
	// This is more reliable than later testing ichAnchor and ichEnd, which may be relative
	// to different properties.
	ComBool fEndBeforeAnchor;
	CheckHr(qvwsel->get_EndBeforeAnchor(&fEndBeforeAnchor));

	// Do not use hvoAnchor and hvoEnd to determine whether sel is within a single item
	// because an hvo may appear more than once in the list.

	AfDeFeTags * pdft = dynamic_cast<AfDeFeTags *>(m_pdfv);
	AssertPtr(pdft);
	IVwCacheDaPtr qvcd = pdft->m_qvcd;
	Assert(qvcd);
	ISilDataAccessPtr qsdaTemp;
	CheckHr(qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));
	HVO hvoPssl = pdft->m_hvoPssl;
	if (ihvoEnd == ihvoAnchor) // test BEFORE modifying below!
	{
		// A single item...but is the whole of it selected?
		if (min(ichAnchor, ichEnd) != 0)
			return false; // not whole items, do normal typing
		// One of them starts at the beginning of the property...
		int cchAnchorProp;
		CheckHr(qtssAnchor->get_Length(&cchAnchorProp));
		// If the other one is not at the end, not whole item, do normal typing.
		if (cchAnchorProp != max(ichAnchor, ichEnd))
			return false;
	}

	// If either end of the selection is the dummy object at the end, exclude it.
	// Also exclude an object at the end if the selection ends at the start of the object.
	if (hvoAnchor == kidDummy)
	{
		if (hvoEnd == kidDummy)
		{
			// Even if we selected all of the dummy item, we won't delete it, so
			// process normally.
			return false;
		}
		ihvoAnchor--;
	}
	else if (fEndBeforeAnchor && ichAnchor == 0)
	{
		// selection to the start of next item; don't delete that.
		ihvoAnchor--;
	}

	if (hvoEnd == kidDummy) // highlighting left to right
		ihvoEnd--;
	else if (!fEndBeforeAnchor && ichEnd == 0 && ihvoEnd > ihvoAnchor)
		ihvoEnd--; // selection to start of item the other way.

	// We need to swap values if we have a right-to-left selection.
	int ihvo;
	if (ihvoAnchor > ihvoEnd)
	{
		ihvo = ihvoAnchor;
		ihvoAnchor = ihvoEnd;
		ihvoEnd = ihvo;
	}

	// At this point we know the selection covers one or more whole items (other than dummy),
	// so we want to delete these items. We need to delete from end of vector,
	// keeping the dummy.
	int chvoPss; // The starting number of items.
	CheckHr(qsdaTemp->get_VecSize(hvoPssl, kflidPssIds, &chvoPss));
	int chvoPssNew = chvoPss;
	for (ihvo = ihvoEnd; ihvo >= ihvoAnchor; --ihvo)
	{
		// Remove the hvo and name at this location.
		CheckHr(qsdaTemp->Replace(hvoPssl, kflidPssIds, ihvo, ihvo + 1, NULL, 0));
		--chvoPssNew;
	}
	// Update the display.
	qsdaTemp->PropChanged(m_qrootb, kpctNotifyMeThenAll, hvoPssl, kflidPssIds, 0,
		chvoPssNew, chvoPss);

	// Commit the selection so that we can access the data from the cache.
	ComBool fOk;
	CheckHr(qvwsel->Commit(&fOk));
	CheckHr(m_qrootb->get_Selection(&qvwsel));

	//int cvsli;
	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string prop itself, but AllTextSelInfo does not need it.
	Assert((uint)cvsli < 3);
	VwSelLevInfo rgvsli[2];
	// Get selection information to determine where the user is typing.
	int ihvoObj;
	PropTag tagTextProp;
	int cpropPrevious;
	int ws;
	// ichAnchor (which should = ichEnd now after the deleted selection) gives an index to
	// the cursor from the beginning of the paragraph.
	CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

	fAssocPrev = ichAnchor; // False at beginning of paragraph and true elsewhere.
	// Now make the new selection.
	CheckHr(m_qrootb->MakeTextSelection(ihvoObj, cvsli, rgvsli, tagTextProp, cpropPrevious,
		ichAnchor, ichAnchor, ws, fAssocPrev, -1, NULL, true, NULL));

	return true;
}

/*----------------------------------------------------------------------------------------------
	Enable/disable the undo menu item. Because of complexities in undo/redoing these changes,
	for now we disable undo/redo if any typing has been done in this field.
	@param cms menu command state
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfDeTagsWnd::CmsEditUndo(CmdState & cms)
{
	AfDeFeTags * pdft = dynamic_cast<AfDeFeTags *>(m_pdfv);
	if (pdft->IsDirty())
	{
		StrApp staLabel(kstidRedoFieldDisabled);
		cms.SetText(staLabel, staLabel.Length());
		cms.Enable(false);
		return true;
	}
	else
	{
		RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(MainWindow());
		Assert(pwnd);
		return pwnd->CmsEditUndo(cms);
	}
}


/*----------------------------------------------------------------------------------------------
	Enable/disable the redo menu item. Because of complexities in undo/redoing these changes,
	for now we disable undo/redo if any typing has been done in this field.
	@param cms menu command state
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfDeTagsWnd::CmsEditRedo(CmdState & cms)
{
	AfDeFeTags * pdft = dynamic_cast<AfDeFeTags *>(m_pdfv);
	if (pdft->IsDirty())
	{
		StrApp staLabel(kstidRedoFieldDisabled);
		cms.SetText(staLabel, staLabel.Length());
		cms.Enable(false);
		return true;
	}
	else
	{
		RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(MainWindow());
		Assert(pwnd);
		return pwnd->CmsEditRedo(cms);
	}
}


/*----------------------------------------------------------------------------------------------
	Handle the undo command by passing it on to the main window.
	@param pcmd menu command
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfDeTagsWnd::CmdEditUndo(Cmd * pcmd)
{
	RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(pwnd);
	pwnd->CmdEditUndo(pcmd);
	// Note, due to FullRefresh, this field editor is deleted at this point.
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle the redo command by passing it on to the main window.
	@param pcmd menu command
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfDeTagsWnd::CmdEditRedo(Cmd * pcmd)
{
	RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(pwnd);
	pwnd->CmdEditRedo(pcmd);
	// Note, due to FullRefresh, this field editor is deleted at this point.
	return true;
}

/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeRefs.cpp
Responsibility: Ken Zook
Last reviewed: never

Implements
	RefsVc: The view constructor that determines how the reference view appears.
	AfDeFeRefs: A field editor to display atomic or sequence references.
	AfDeFeRefs::AfDeSelListener: Keeps track of the selection.
	AfDeFeRefs::DfrButton: Displays and processes the button that calls up the ref chooser.
	AfDeRefsWnd: A window allowing references to be added and deleted.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

// First available dummy ID. We reserver 64K ids at the end of the range for dummies.
const uint kidDummy = 0xffff0000;

/*----------------------------------------------------------------------------------------------
	Since we want to disable formatting commands, we need to have our own command map in
	order to override and disable the buttons/comboboxes.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(AfDeRefsWnd)
	ON_CID_CHILD(kcidEditCut, &AfDeRefsWnd::CmdEditCut, &AfDeRefsWnd::CmsEdit)
	ON_CID_CHILD(kcidEditCopy, &AfDeRefsWnd::CmdEditCopy, &AfDeRefsWnd::CmsEdit)
	ON_CID_CHILD(kcidEditPaste, &AfDeRefsWnd::CmdEditPaste, &AfDeRefsWnd::CmsEdit)
	ON_CID_CHILD(kcidFmtFnt, &AfVwWnd::CmdFmtFnt, &AfDeRefsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmtStyles, &AfVwWnd::CmdFmtStyles, &AfDeRefsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbStyle, &AfVwWnd::CmdCharFmt, &AfDeRefsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidApplyNormalStyle, &AfVwWnd::CmdApplyNormalStyle, NULL)
	ON_CID_CHILD(kcidFmttbWrtgSys, &AfVwWnd::CmdCharFmt, &AfDeRefsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFnt, &AfVwWnd::CmdCharFmt, &AfDeRefsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFntSize, &AfVwWnd::CmdCharFmt, &AfDeRefsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbBold, &AfVwWnd::CmdCharFmt, &AfDeRefsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbItal, &AfVwWnd::CmdCharFmt, &AfDeRefsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyBgrndColor, &AfVwWnd::CmdCharFmt, &AfDeRefsWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyFgrndColor, &AfVwWnd::CmdCharFmt, &AfDeRefsWnd::CmsCharFmt)
END_CMD_MAP_NIL()


//:>********************************************************************************************
//:> ObjVc methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
	@param fLoadData True if the VC needs to load any data it uses.
----------------------------------------------------------------------------------------------*/
ObjVc::ObjVc(bool fLoadData)
{
	m_fLoadData = fLoadData;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
ObjVc::~ObjVc()
{
}

static DummyFactory g_fact1(_T("SIL.AppCore.ObjVc"));

/*----------------------------------------------------------------------------------------------
	Load the data needed to display this view. In this case, we need to load the class
	for the HVO. If the class is already in the cache, don't reload it.
	@param pvwenv Pointer to the view environment.
	@param hvo The id of the object we are displaying.
	@param frag Identifies the part of the view we are currently displaying.
	@return HRESULT indicating success (S_OK), or failure (E_FAIL).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ObjVc::LoadDataFor(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwenv);

	if (!hvo)
		ThrowHr(E_INVALIDARG);

	IDbColSpecPtr qdcs;
	StrUni stuSql;
	ISilDataAccessPtr qsda;
	int clid;
	CheckHr(pvwenv->get_DataAccess(&qsda));
	CheckHr(qsda->get_IntProp(hvo, kflidCmObject_Class, &clid));

	if (!clid)
	{
		// If the class is missing from the cache, load it.
		IVwOleDbDaPtr qda;
		CheckHr(qsda->QueryInterface(IID_IVwOleDbDa, (void**)&qda));
		stuSql.Format(L"select id, Class$ from CmObject "
			L"where id = %d", hvo);
		qdcs.CreateInstance(CLSID_DbColSpec);
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctInt, 1, kflidCmObject_Class, 0));

		// Execute the query and store results in the cache.
		CheckHr(qda->Load(stuSql.Bstr(), qdcs, hvo, 0, NULL, NULL));
	}
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	This is the method for displaying the name of a single reference. This default view just
	shows ClassName:HVO for each item. "RnEvent:1233"
	@param pvwenv Pointer to the view environment.
	@param hvo The id of the object we are displaying.
	@param frag Identifies the part of the view we are currently displaying.
	@return HRESULT indicating success (S_OK), or failure (E_FAIL).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ObjVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwenv);

	switch (frag)
	{
	case kfrRefName:
	case kfrListName:
		{
			StrUni stu;
			int clid;
			SmartBstr sbstr = L"UnLoaded";
			AssertPtr(m_qdbi);
			if (!m_qdbi)
				ThrowHr(WarnHr(E_UNEXPECTED));

			// Make sure data is loaded.
			LoadDataFor(pvwenv, hvo, frag);
			ISilDataAccessPtr qsda;
			CheckHr(pvwenv->get_DataAccess(&qsda));
			AssertPtr(qsda);
			CheckHr(qsda->get_IntProp(hvo, kflidCmObject_Class, &clid));

			if (clid)
			{
				IFwMetaDataCachePtr qmdc;
				m_qdbi->GetFwMetaDataCache(&qmdc);
				AssertPtr(qmdc);
				qmdc->GetClassName(clid, &sbstr);
			}

			stu.Format(L"%s:%d", sbstr.Chars(), hvo);
			ITsStringPtr qtss;
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_qdbi->UserWs(), &qtss));
			CheckHr(pvwenv->AddString(qtss));
			break;
		}
	}
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwViewConstructor);
}


//:>********************************************************************************************
//:> RefsVc methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
	@param flid The id of the field we are displaying
	@param povc Pointer to a VC that is used to display a each item.
	@param pszSty Pointer to a Unicode string giving the name of the style to use for displays.
	@param clrBkg The background color for the field.
----------------------------------------------------------------------------------------------*/
RefsVc::RefsVc(int flid, ObjVc * povc, LPCOLESTR pszSty, COLORREF clrBkg)
{
	AssertPsz(pszSty);
	AssertPtr(povc);
	Assert(flid);
	m_stuSty = pszSty;
	m_clrBkg = (clrBkg == -1 ? ::GetSysColor(COLOR_WINDOW) : clrBkg);
	m_flid = flid;
	m_qovc = povc;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
RefsVc::~RefsVc()
{
}

static DummyFactory g_fact2(_T("SIL.AppCore.RefsVc"));

/*----------------------------------------------------------------------------------------------
	This is the method for displaying the contents of the reference field. References are
	displayed in a paragraph separated by vertical gray bars. If the list is a sequence,
	and at least one item is present, a gray bar is always appended to the end, providing
	feedback to the user that this is a sequence.
	@param pvwenv Pointer to the view environment.
	@param hvo The id of the object we are displaying.
	@param frag Identifies the part of the view we are currently displaying.
	@return HRESULT indicating success (S_OK), or failure (E_FAIL).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RefsVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwenv);

	StrUni stu;
	switch (frag)
	{
	case kfrMultiRefs:
		{
			// Display a paragraph of items with separators.
			CheckHr(pvwenv->put_IntProperty(ktptParaColor, ktpvDefault, m_clrBkg));
			CheckHr(pvwenv->put_StringProperty(kspNamedStyle, m_stuSty.Bstr()));
			CheckHr(pvwenv->put_IntProperty(ktptTrailingIndent, ktpvMilliPoint, kdzmpInch / 4));
			CheckHr(pvwenv->OpenMappedPara());
			CheckHr(pvwenv->AddObjVec(m_flid, this, 0));
			CheckHr(pvwenv->CloseParagraph());
			break;
		}

	case kfrSingleRef:
		{
			// Display a paragraph with a single item.
			CheckHr(pvwenv->put_IntProperty(ktptParaColor, ktpvDefault, m_clrBkg));
			CheckHr(pvwenv->put_StringProperty(kspNamedStyle, m_stuSty.Bstr()));
			CheckHr(pvwenv->put_IntProperty(ktptTrailingIndent, ktpvMilliPoint, kdzmpInch / 4));
			CheckHr(pvwenv->OpenMappedPara());
			CheckHr(pvwenv->AddObjProp(m_flid, this, kfrObjName));
			CheckHr(pvwenv->CloseParagraph());
			break;
		}

	case kfrObjName:
		// Display one reference.
		{
			CheckHr(pvwenv->put_StringProperty(kspNamedStyle, m_stuSty.Bstr()));
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
			AssertPtr(m_qdbi);
			if (!m_qdbi)
				ThrowHr(WarnHr(E_UNEXPECTED));
			if (hvo)
			{
				CheckHr(m_qovc->Display(pvwenv, hvo, kfrRefName));
			}
			else
			{
				// Need to add a dummy string to allow an insertion point.
				ITsStringPtr qtss;
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				CheckHr(qtsf->MakeStringRgch(L"", 0, m_qdbi->UserWs(), &qtss));
				CheckHr(pvwenv->AddString(qtss));
			}
			break;
		}
	}
	return S_OK;

	END_COM_METHOD(g_fact2, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	Display a vector of reference items.
	@param pvwenv Pointer to the view environment.
	@param hvo The id of the object we are displaying.
	@param tag The field id holding the items.
	@param frag Identifies the part of the view we are currently displaying (unused here).
	@return HRESULT indicating success (S_OK), or failure (E_FAIL).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RefsVc::DisplayVec(IVwEnv * pvwenv, HVO hvo, int tag, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwenv);

	// We handle this one as a sequence so we can insert separators.
	// Ignore frag as this constructor only has one sequence.
	ISilDataAccessPtr qsda;
	CheckHr(pvwenv->get_DataAccess(&qsda));
	int chvo;
	CheckHr(qsda->get_VecSize(hvo, tag, &chvo));
	if (chvo)
	{
		// For each item in the vector, add the item and a separator. The final separator
		// indicates to the user that we have a sequence.
		for (int ihvo = 0; ihvo < chvo; ihvo++)
		{
			HVO hvoItem;
			CheckHr(qsda->get_VecItem(hvo, tag, ihvo, &hvoItem));
			CheckHr(pvwenv->AddObj(hvoItem, this, kfrObjName));
			CheckHr(pvwenv->AddSeparatorBar());
		}
	}
	else
	{
		// Need to add a dummy string to allow an insertion point.
		ITsStringPtr qtss;
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		AssertPtr(m_qdbi);
		if (!m_qdbi)
			ThrowHr(WarnHr(E_UNEXPECTED));
		CheckHr(qtsf->MakeStringRgch(L"", 0, m_qdbi->UserWs(), &qtss));
		CheckHr(pvwenv->AddString(qtss));
	}
	return S_OK;

	END_COM_METHOD(g_fact2, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	Return the text string that gets shown to the user when this object needs to be displayed.

	@param pguid Pointer to a database object's assigned GUID.
	@param pptss Address of a pointer to an ITsString COM object used for returning the text
					string.

	@return S_OK, E_POINTER, or E_FAIL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RefsVc::GetStrForGuid(BSTR bstrGuid, ITsString ** pptss)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pptss);
	ChkComBstrArg(bstrGuid);

	AssertPtr(m_qovc);
	return m_qovc->GetStrForGuid(bstrGuid, pptss);

	END_COM_METHOD(g_fact2, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	The user clicked on the object.

	@param pguid Pointer to a database object's assigned GUID.
	@param hvoOwner The database ID of the object.
	@param tag Identifier used to select one particular property of the object.
	@param ptss Pointer to an ITsString COM object containing a string that embeds a link to the
					object.
	@param ichObj Offset in the string to the pseudo-character that represents the object link.

	@return S_OK, E_POINTER, E_INVALIDARG, or E_FAIL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RefsVc::DoHotLinkAction(BSTR bstrData, HVO hvoOwner, PropTag tag,
	ITsString * ptss, int ichObj)
{
	BEGIN_COM_METHOD
	ChkComBstrArg(bstrData);
	ChkComArgPtr(ptss);
	if (!hvoOwner)
		ThrowHr(E_INVALIDARG);

	if (BstrLen(bstrData) == 9 && bstrData[0] == kodtNameGuidHot)
	{
		// The mouse is over a ref, so we want to perform a jump.
		// TODO KenZ (DarrellZ)
		return S_OK;
	}
	return SuperClass::DoHotLinkAction(bstrData, hvoOwner, tag, ptss, ichObj);

	END_COM_METHOD(g_fact2, IID_IVwViewConstructor);
}

//:>********************************************************************************************
//:>	AfDeFeRefs methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDeFeRefs::AfDeFeRefs()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeFeRefs::~AfDeFeRefs()
{
}


/*----------------------------------------------------------------------------------------------
	Complete initialization of the property by storing the appropriate items, names, and lists.
	@param fMultiList True to use a table to show list abbr, and list contents.
	@param fHier True if item names include parent items in the hierarchy.
	@param pnt Indicates whether we show name, abbr, or both for each item.
----------------------------------------------------------------------------------------------*/
void AfDeFeRefs::Init(ObjVc * povc, bool fMultiRefs)
{
	Assert(m_flid); // Don't call this until Initialize has been called.
	AssertPtr(povc);

	m_fMultiRefs = fMultiRefs;

	// Create the view constructor.
	m_qrvc.Attach(NewObj RefsVc(m_flid, povc, m_qfsp->m_stuSty.Chars(), m_chrp.clrBack));

	AfLpInfo * plpi = GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo *pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	m_qrvc->SetDbInfo(pdbi);	// also sets m_qdbi for povc.
}


/*----------------------------------------------------------------------------------------------
	Make a rootbox, initialize it, and return a pointer to it.
	@param pvg Graphics information (unused by this method).
	@param pprootb Pointer that receives the newly created RootBox.
----------------------------------------------------------------------------------------------*/
void AfDeFeRefs::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
	IVwRootBox ** pprootb)
{
	AssertPtr(pprootb);
	AssertPtrN(pwsf);
	AssertPtr(m_qrvc); // Init should have been called before calling this.

	*pprootb = NULL;

	// Create the RootBox.
	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this)); // The field editor is the root site.

	// Set the RootBox data cache.
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	if (pwsf)
		CheckHr(qcvd->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qcvd));

	// Finish initializing the RootBox.
	IVwViewConstructor * pvvc = m_qrvc;
	int frag = m_fMultiRefs ? kfrMultiRefs : kfrSingleRef;
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
	Make an edit box to allow editing. hwnd is the parent hwnd. rc is the size of the child
	window. Store the hwnd and return true.
	See ${AfDeFieldEditor#BeginEdit} for parameter and return descriptions.
----------------------------------------------------------------------------------------------*/
bool AfDeFeRefs::BeginEdit(HWND hwnd, Rect &rc, int dxpCursor, bool fTopCursor)
{
	if (!SuperClass::BeginEdit(hwnd, rc, dxpCursor, fTopCursor))
		return false;

	// Add a listener to notify us whenever the selection changes.
	m_qadsl = NewObj AfDeSelListener();
	CheckHr(m_qrootb->AddSelChngListener(m_qadsl));
	m_qadsl->m_pdfr = this;

	// If we display the button here, we can't determine where it should occur since the
	// click doesn't get fully evaluated until after we exit this method. Instead, we
	// let the listener insert the button when the selection is first established.

	// Note that no selection is successfully established by the superclass call, because
	// there is nowhere in this type of field that we can edit in the normal sense.
	// Make a selection at the beginning or end of the field.
	CheckHr(m_qrootb->MakeSimpleSel(fTopCursor, false, false, true, NULL));

	m_fDirty = false;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Saves changes but keeps the editing window open.
	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool AfDeFeRefs::SaveEdit()
{
	return true;
}


/*----------------------------------------------------------------------------------------------
	Close the current editor, saving changes that were made.
	@param fForce True if we want to force the editor closed without making any
		validity checks or saving any changes.
----------------------------------------------------------------------------------------------*/
void AfDeFeRefs::EndEdit(bool fForce)
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
void AfDeFeRefs::MoveWnd(const Rect & rcClip)
{
	SuperClass::MoveWnd(rcClip);
	PlaceButton();
}


/*----------------------------------------------------------------------------------------------
	Creates a button if missing. Otherwise it makes sure the button is at the right of the,
	on the top line of the current possibility list. It also sets the current list so the
	correct chooser will be called.
----------------------------------------------------------------------------------------------*/
void AfDeFeRefs::PlaceButton()
{
	const int dxpButWid = 16;
	const int dxpButHeight = 15;
	Rect rcClient;
	::GetClientRect(m_hwnd, &rcClient);
	Rect rcBut(rcClient.right - dxpButWid,
		rcClient.top,
		rcClient.right,
		rcClient.top + Min((int)rcClient.bottom, (int)dxpButHeight));

	// Create the button if it isn't present.
	if (!m_hwndButton)
	{
		WndCreateStruct wcsButton;
		wcsButton.InitChild(_T("BUTTON"), m_hwnd, 1);
		wcsButton.style |= WS_VISIBLE | BS_OWNERDRAW;
		wcsButton.SetRect(rcBut);

		DfrButtonPtr qdfrb;
		qdfrb.Create();
		qdfrb->CreateAndSubclassHwnd(wcsButton);
		qdfrb->m_pdfr = this;
		m_hwndButton = qdfrb->Hwnd();
	}
	else
	{
		// Otherwise, move the button.
		::MoveWindow(m_hwndButton, rcBut.left, rcBut.top, rcBut.Width(), rcBut.Height(), true);
	}
}


/*----------------------------------------------------------------------------------------------
	Fire up the chooser with the original items selected. If the user makes a change,
	update the display cache appropriately, update the display and reset the insertion point.
----------------------------------------------------------------------------------------------*/
void AfDeFeRefs::ProcessChooser()
{
	HVO hvo;
	hvo = m_qadsc->LaunchRefChooser(this);
	// ENHANCE KenZ: Add hvo to the property once an appropriate dialog is developed to
	// to return something useful.
}


/*----------------------------------------------------------------------------------------------
	Create a new window for editing the contents.
	@param hwndParent The hwnd for the parent window.
	@param rcBounds The position of the new window relative to the parent window.
	@return A pointer to the new AfDeVwWnd window. The caller obtains one (and initially only)
		reference count to the window.
----------------------------------------------------------------------------------------------*/
AfDeVwWnd * AfDeFeRefs::CreateEditWnd(HWND hwndParent, Rect & rcBounds)
{
	AfDeRefsWndPtr qdtw;
	qdtw.Attach(NewObj AfDeRefsWnd);

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
void AfDeFeRefs::NextItem()
{
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));

	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string prop itself, but AllTextSelInfo does not need it.
	Assert((uint)cvsli < 2);
	VwSelLevInfo rgvsli;
	// Get selection information to determine where the user is typing.
	int ihvoObj;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;
	CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, &rgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
	Assert(rgvsli.tag == m_flid);

	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	int chvo;
	CheckHr(qcvd->get_VecSize(m_hvoObj, m_flid, &chvo));
	if (rgvsli.ihvo < chvo - 1)
	{
		rgvsli.ihvo++;
		CheckHr(m_qrootb->MakeTextSelInObj(ihvoObj, cvsli, &rgvsli, 0, NULL, true,
			false, false, false, true, NULL));
	}
	else
		m_qadsc->OpenNextEditor();
}


/*----------------------------------------------------------------------------------------------
	Move the cursor to the previous item, if not at the beginning.
----------------------------------------------------------------------------------------------*/
void AfDeFeRefs::PrevItem()
{
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));

	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string property itself, but AllTextSelInfo does not need it
	Assert((uint)cvsli < 2);
	VwSelLevInfo rgvsli;
	// Get selection information to determine where the user is typing.
	int ihvoObj;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;
	CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, &rgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
	Assert(rgvsli.tag == m_flid);

	if (rgvsli.ihvo > 0)
	{
		rgvsli.ihvo--;
		CheckHr(m_qrootb->MakeTextSelInObj(ihvoObj, cvsli, &rgvsli, 0, NULL, true,
			false, false, false, true, NULL));
	}
	else
		m_qadsc->OpenPreviousEditor();
}


static DummyFactory g_fact3(_T("SIL.AppCore.AfDeFeRefs"));

/*----------------------------------------------------------------------------------------------
	When the selection is changed, it propagates this to its site.
	TODO JohnT: when VwRootBox can be changed, make it do both kinds of change notification
	in MakeSimpleSelAt; then this is (probably) not necessary.
	@param pvwselNew Pointer to the new selection.
	@return S_OK indicating success.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeRefs::SelectionChanged(IVwRootBox * prootb, IVwSelection * pvwselNew)
{
	BEGIN_COM_METHOD

	PlaceButton();
	return S_OK;

	END_COM_METHOD(g_fact3, IID_IVwRootSite);
}


/*----------------------------------------------------------------------------------------------
	Process mouse movements when the editor isn't active.
	See ${AfDeFieldEditor#OnMouseMove} for parameter descriptions.
----------------------------------------------------------------------------------------------*/
/*TODO TimP
bool AfDeFeRefs::OnMouseMove(uint grfmk, int xp, int yp)
{
	// See if the mouse is over text.
	RECT rcSrcRoot;
	RECT rcDstRoot;

	InitGraphics();
	GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
	UninitGraphics();

	ComBool fInText;
	CheckHr(m_qrootb->get_IsClickInText(xp, yp, rcSrcRoot, rcDstRoot, &fInText));

	// Change the cursor to a pointing finger if over a ref.
	if (fInText)
		::SetCursor(::LoadCursor(NULL, IDC_HAND));
	else
		::SetCursor(::LoadCursor(NULL, IDC_ARROW));

	return true;
}
*/

/*----------------------------------------------------------------------------------------------
	Complete a drag drop operation by storing hvo in the property.
	@param hvo The object we are dropping.
	@param clid The class of the object we are dropping.
	@param pt The mouse location in long screen coordinates.
----------------------------------------------------------------------------------------------*/
void AfDeFeRefs::DropObject(HVO hvo, int clid, POINTL pt)
{
	HVO hvoT = hvo;
	IFwMetaDataCachePtr qmdc;
	AfLpInfo * plpi = GetLpInfo();
	AssertPtr(plpi);
	plpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
	AssertPtr(qmdc);
	ComBool fValid;
	qmdc->get_IsValidClass(m_flid, clid, &fValid);
	if (!fValid)
		return; // Not over a valid reference field for the class we are dragging.

	CustViewDaPtr qcvd;
	plpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	int chvo = 0;
	HVO hvoOld;
	// Ignore drop if item is already in the property.
	if (m_fMultiRefs)
	{
		CheckHr(qcvd->get_VecSize(m_hvoObj, m_flid, &chvo));
		for (int ihvo = 0; ihvo < chvo; ++ihvo)
		{
			CheckHr(qcvd->get_VecItem(m_hvoObj, m_flid, ihvo, &hvoOld));
			if (hvo == hvoOld)
				return;
		}
	}
	else
	{
		CheckHr(qcvd->get_ObjectProp(m_hvoObj, m_flid, &hvoOld));
		if (hvoOld == hvo)
			return;
	}

	// ENHANCE KenZ: Need to add it in the appropriate place depending on mouse location
	// For now, add the new object at the end of the list.
	// Start undo action (e.g., "Undo Drop Reference in See Also").
	SmartBstr sbstrLabel;
	CheckHr(m_qtssLabel->get_Text(&sbstrLabel));
	StrUni stuUndoFmt;
	StrUni stuRedoFmt;
	StrUtil::MakeUndoRedoLabels(kstidUndoRefDropTo, &stuUndoFmt, &stuRedoFmt);
	StrUni stuUndo;
	StrUni stuRedo;
	stuUndo.Format(stuUndoFmt.Chars(), sbstrLabel.Chars());
	stuRedo.Format(stuRedoFmt.Chars(), sbstrLabel.Chars());
	CheckHr(qcvd->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
	if (m_fMultiRefs)
		CheckHr(qcvd->Replace(m_hvoObj, m_flid, chvo, chvo, &hvoT, 1));
	else
		CheckHr(qcvd->SetObjProp(m_hvoObj, m_flid, hvoT));
	CheckHr(qcvd->EndUndoTask());
	qcvd->PropChanged(m_qrootb, kpctNotifyAll, m_hvoObj, m_flid, chvo, 1, 0);
}


//:>********************************************************************************************
//:>	AfDeFeRefs::AfDeSelListener methods.
//:>********************************************************************************************

static DummyFactory g_fact4(_T("SIL.AppCore.AfDeSelListener"));

/*----------------------------------------------------------------------------------------------
	Do a standard COM query interface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeRefs::AfDeSelListener::QueryInterface(REFIID riid, void **ppv)
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
STDMETHODIMP AfDeFeRefs::AfDeSelListener::Notify(int nCode, int nArg2)
{
	BEGIN_COM_METHOD

	if (nCode < 4)
		m_pdfr->PlaceButton();
	return S_OK;

	END_COM_METHOD(g_fact4, IID_IEventListener);
}


/*----------------------------------------------------------------------------------------------
	Check the requirments of the FldSpec, and verify that data in the field meets the
	requirement. It returns:
		kFTReqNotReq if the all requirements are met.
		kFTReqWs if data is missing, but it is encouraged.
		kFTReqReq if data is missing, but it is required.
----------------------------------------------------------------------------------------------*/
FldReq AfDeFeRefs::HasRequiredData()
{
	if (m_qfsp->m_fRequired == kFTReqNotReq)
		return kFTReqNotReq;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	int cobj;
	if (m_fMultiRefs)
		CheckHr(qcvd->get_VecSize(m_hvoObj, m_flid, &cobj));
	else
	{
		HVO hvo;
		CheckHr(qcvd->get_ObjectProp(m_hvoObj, m_flid, &hvo));
		cobj = hvo;
	}
	if (!cobj)
		return m_qfsp->m_fRequired;
	else
		return kFTReqNotReq;
}


//:>********************************************************************************************
//:>	AfDeFeRefs::DfrButton methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Handle window painting (WM_PAINT). We use owner-draw on this button to get the desired ...
	@param pdis Pointer to a struct giving information needed for owner-draw.
	@return True indicating it was processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeRefs::DfrButton::OnDrawThisItem(DRAWITEMSTRUCT * pdis)
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
	Return the what's-this help string for the Cross Reference ellipsis button.

	@param pt not used.
	@param pptss Address of a pointer to an ITsString COM object for returning the help string.

	@return true.
----------------------------------------------------------------------------------------------*/
bool AfDeFeRefs::DfrButton::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	StrApp str;
	str.Load(kstidEllipsisCrossRefWhatsThisHelp); // No context help available
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(str);
	CheckHr(qtsf->MakeString(stu.Bstr(), m_pdfr->UserWs(), pptss));
	return true;
}


//:>********************************************************************************************
//:>	AfDeRefsWnd methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Process commands. We just want to intercept BN_CLICKED to pass it to AfDeFeRefs.
	See ${AfWnd#OnCommand} for parameter and return descriptions.
----------------------------------------------------------------------------------------------*/
bool AfDeRefsWnd::OnCommand(int cid, int nc, HWND hctl)
{
	// Process a button click by passing it on to AfDeFeRefs
	if (nc == BN_CLICKED)
	{
		AfDeFeRefs * pdfr = dynamic_cast<AfDeFeRefs *>(m_pdfv);
		if (pdfr)
			pdfr->ProcessChooser();
		return true;
	}

	return SuperClass::OnCommand(cid, nc, hctl);
}


/*----------------------------------------------------------------------------------------------
	Process a mouse click. If it is on a ref, jump/launch to that ref. Otherwise set an
	insertion point at the end closest to the click.
----------------------------------------------------------------------------------------------*/
void AfDeRefsWnd::CallMouseUp(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot)
{
	SuperClass::CallMouseUp(xp, yp, rcSrcRoot, rcDstRoot);

	m_wsPending = -1;

	// TODO KenZ(DarrellZ): Uncomment this code to use the proper hot link mechanism.
	/*ComBool fInObject; // The mouse is over text.
	int odt;
	CheckHr(m_qrootb->get_IsClickInObject(xp, yp, rcSrcRoot, rcDstRoot, &odt, &fInObject));
	if (fInObject && (odt == kodtNameGuidHot || odt == kodtExternalPathName))
	{
		SuperClass::CallMouseDown(xp, yp, rcSrcRoot, rcDstRoot);
		return;
	}*/

	ComBool fInText; // The mouse is over text.
	CheckHr(m_qrootb->get_IsClickInText(xp, yp, rcSrcRoot, rcDstRoot, &fInText));

	// Get the tentative selection at the mouse location regardless of editability.
	IVwSelectionPtr qsel;
	m_qrootb->MakeSelAt(xp, yp, rcSrcRoot, rcDstRoot, false, &qsel);
	// If we can't make a selection for some reason just do nothing.
	if (!qsel)
	{
		Warn("Could not make selection in AfDeRefsWnd");
		return;
	}

	// Get information on the selection.
	int cvsli;
	CheckHr(qsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string prop itself, but AllTextSelInfo does not need it.
	Assert((uint)cvsli < 3);
	VwSelLevInfo rgvsli[2];
	// Get selection information to determine where the mouse is located.
	int ihvoObj;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;
	CheckHr(qsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
	//sta.Format("AfDeRefsWnd::CallMouseUp:  ihvoObj=%d; tagTextProp=%d; cpropPrevious=%d; ichAnchor=%d; ichEnd=%d; ws=%d; fAssocPrev=%d; ihvoEnd=%d.\n",
	//                                       ihvoObj,    tagTextProp,    cpropPrevious,    ichAnchor,    ichEnd,    ws,    (int)fAssocPrev, ihvoEnd);
	//OutputDebugString(sta.Chars());

	// qvwsel
	IVwSelectionPtr qvwsel;
	ComBool fOk;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	// Commit the selection so that we can access the data from the cache.
	CheckHr(qvwsel->Commit(&fOk));
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
	//sta.Format("AfDeRefsWnd::CallMouseUp:  qvwsel  ihvoObj=%d; tagTextProp=%d; cpropPrevious=%d; ichAnchor=%d; ichEnd=%d; ws=%d; fAssocPrev=%d; ihvoEnd=%d.\n",
	//                                               ihvoObj,    tagTextProp,    cpropPrevious,    ichAnchor,    ichEnd,    ws,    (int)fAssocPrev, ihvoEnd);
	//OutputDebugString(sta.Chars());

	if (fInText)
	{
		if (ichAnchor != ichEnd)
			return; // Do not open in (highlighted) internal link.

		// Set the selection at the mouse location.
		m_qrootb->MakeSelAt(xp, yp, rcSrcRoot, rcDstRoot, true, &qsel);
		if (!qsel)
			ThrowHr(WarnHr(E_FAIL));
		CheckHr(qsel->AllTextSelInfo(&ihvoObj, 1, rgvsli, &tagTextProp, &cpropPrevious,
			&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

		// The mouse is over a ref, so we want to perform a jump.
		// Get the id for the object of interest.
		HVO hvo; // The reference we are over.
		HVO hvoOwn = m_pdfv->GetOwner();
		int flid = m_pdfv->GetOwnerFlid();
		int ihvo = rgvsli[0].ihvo;
		ISilDataAccessPtr qsda;
		CheckHr(m_qrootb->get_DataAccess(&qsda));
		AfDeFeRefs * pdfr = dynamic_cast<AfDeFeRefs *>(m_pdfv);
		AssertPtr(pdfr);
		if (pdfr->m_fMultiRefs)
			CheckHr(qsda->get_VecItem(hvoOwn, flid, ihvo, &hvo));
		else
			CheckHr(qsda->get_ObjectProp(hvoOwn, flid, &hvo));

		// Show the selected object.
		if (::GetKeyState(VK_SHIFT) < 0)
			pdfr->GetDeWnd()->LaunchItem(hvo); // Open hvo in a new window.
		else
			pdfr->GetDeWnd()->JumpItem(hvo); // Open hvo in the current window.
	}
	else
	{
		// The mouse is outside of the text, so we want to place the IP at the selection
		// location. To determine whether we are at the beginning or end of a ref, make
		// a fake selection at the beginning and compare to see if we are at the same
		// location.
		CheckHr(m_qrootb->MakeTextSelInObj(ihvoObj, cvsli, rgvsli, 0, NULL, true, false, false,
			false, false, &qsel));
		int ichAnchorT;
		CheckHr(qsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
			&ichAnchorT, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
		// Make an IP at the beginning or end of the ref.
		CheckHr(m_qrootb->MakeTextSelInObj(ihvoObj, cvsli, rgvsli, 0, NULL, ichAnchorT == ichAnchor,
			false, false, false, true, NULL));
	}

	return;
}


/*----------------------------------------------------------------------------------------------
	Process keyboard keys.
	See ${AfVwWnd#OnKeyDown} for parameter descriptions.
----------------------------------------------------------------------------------------------*/
bool AfDeRefsWnd::OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
{
	if (nChar != VK_RETURN && nChar != VK_LEFT && nChar != VK_RIGHT && nChar != VK_BACK
		&& nChar != VK_DELETE)
	{
		return SuperClass::OnKeyDown(nChar, nRepCnt, nFlags);
	}

	IVwSelectionPtr qsel;
	IVwSelectionPtr qselT;
	bool fShifted = ::GetKeyState(VK_SHIFT) < 0;
	CheckHr(m_qrootb->get_Selection(&qsel));
	// Get information on the selection.
	int cvsli;
	CheckHr(qsel->CLevels(false, &cvsli));
	// cvsli == 0 or 1 when field is empty. Arrow keys can be handled by superclass.
	if (cvsli < 2)
		return SuperClass::OnKeyDown(nChar, nRepCnt, nFlags);
	cvsli--; // CLevels includes the string prop itself, but AllTextSelInfo does not need it.
	Assert((uint)cvsli < 2); // If the view is more complex, we probably need changes below.
	VwSelLevInfo rgvsli[1];
	// Get selection information to determine the cursor position.
	int ihvoObj;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;
	CheckHr(qsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

	switch (nChar)
	{
	case VK_RIGHT:
	{
		if (fShifted && ichEnd != ichAnchor)
			return true; // For now don't extend selection beyond one item.
		// To determine if we are at the end of an object ref, we need to make a dummy
		// selection on the end of that object ref.
		CheckHr(m_qrootb->MakeTextSelInObj(ihvoObj, cvsli, rgvsli, 0, NULL, false, false, false,
			false, false, &qselT));
		int ichEndT;
		CheckHr(qselT->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
			&ichAnchor, &ichEndT, &ws, &fAssocPrev, &ihvoEnd, NULL));
		if (ichEnd != ichEndT)
		{
			// The IP is at the beginning of the ref, so move to the end. Make a
			// range selection if it is Shift+right arrow.
			CheckHr(m_qrootb->MakeTextSelInObj(ihvoObj, cvsli, rgvsli, 0, NULL, false, false,
				false, fShifted, true, NULL));
			return true;
		}
		// The IP (or range) is at the end of the ref, so we need to move to the
		// beginning of the next ref, if there is one.
		HVO hvo = m_pdfv->GetObj();
		ISilDataAccessPtr qsda;
		CheckHr(m_qrootb->get_DataAccess(&qsda));
		int chvo; // Number of items in the list.
		CheckHr(qsda->get_VecSize(hvo, rgvsli[0].tag, &chvo));
		if (chvo - 1 > rgvsli[0].ihvo)
		{
			// We have another ref, so place the cursor at the beginning of the next ref.
			++rgvsli[0].ihvo;
			CheckHr(m_qrootb->MakeTextSelInObj(ihvoObj, cvsli, rgvsli, 0, NULL, true, false,
				false, false, true, NULL));
			return true;
		}
		// We don't have another ref, so go to the next field. The SuperClass will
		// do this without special code here.
		break;
	}

	case VK_LEFT:
	{
		if (fShifted && ichEnd != ichAnchor)
			return true; // For now don't extend selection beyond one item.
		// To determine if we are at the beginning of an object ref, we need to make a dummy
		// selection on the beginning of that object ref.
		CheckHr(m_qrootb->MakeTextSelInObj(ihvoObj, cvsli, rgvsli, 0, NULL, true, false, false,
			false, false, &qselT));
		int ichAnchorT;
		CheckHr(qselT->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
			&ichAnchorT, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
		if (ichAnchor != ichAnchorT)
		{
			// The IP is at the end of the ref, so move to the beginning. Make a
			// range selection if it is Shift+left arrow.
			CheckHr(m_qrootb->MakeTextSelInObj(ihvoObj, cvsli, rgvsli, 0, NULL, true, false,
				false, fShifted, true, NULL));
			return true;
		}
		// The IP (or range) is at the beginning of the ref, so we need to move to the
		// end of the previous ref, if there is one.
		if (rgvsli[0].ihvo)
		{
			// We have another ref, so place the cursor at the end of the previous ref.
			--rgvsli[0].ihvo;
			CheckHr(m_qrootb->MakeTextSelInObj(ihvoObj, cvsli, rgvsli, 0, NULL, false, false,
				false, false, true, NULL));
			return true;
		}
		// We don't have another ref, so go to the previous field. The SuperClass will
		// do this without special code here.
		break;
	}

	case VK_RETURN:
	{
		if (ichAnchor == ichEnd)
			return true; // Ignore Enter if we don't have a range selection.

		// Get the id for the object of interest.
		HVO hvo; // The reference we are over.
		HVO hvoOwn = m_pdfv->GetOwner();
		int flid = m_pdfv->GetOwnerFlid();
		int ihvo = rgvsli[0].ihvo;
		ISilDataAccessPtr qsda;
		CheckHr(m_qrootb->get_DataAccess(&qsda));
		AfDeFeRefs * pdfr = dynamic_cast<AfDeFeRefs *>(m_pdfv);
		AssertPtr(pdfr);
		if (pdfr->m_fMultiRefs)
			CheckHr(qsda->get_VecItem(hvoOwn, flid, ihvo, &hvo));
		else
			CheckHr(qsda->get_ObjectProp(hvoOwn, flid, &hvo));

		// Show the selected object.
		if (fShifted)
			pdfr->GetDeWnd()->LaunchItem(hvo); // Open hvo in a new window.
		else
			pdfr->GetDeWnd()->JumpItem(hvo); // Open hvo in the current window.
		break;
	}

	case VK_DELETE:
	case VK_BACK:
	{
		int ihvoLeft;
		int ihvoRight;
		// If no range, we need to find out whether the IP is at the beginning or end of ref.
		if (ichEnd == ichAnchor)
		{
			// To determine if we are at the end of an object ref, we need to make a dummy
			// selection on the end of that object ref.
			CheckHr(m_qrootb->MakeTextSelInObj(ihvoObj, cvsli, rgvsli, 0, NULL,
				false, false, false, false, false, &qselT));
			int ichEndT;
			CheckHr(qselT->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp,
				&cpropPrevious, &ichAnchor, &ichEndT, &ws, &fAssocPrev, &ihvoEnd, NULL));
			if (ichEnd != ichEndT && nChar == VK_BACK ||
				ichEnd == ichEndT && nChar == VK_DELETE)
			{
				return true; // Bksp at beginning or Del at end of ref is ignored.
			}
		}

		IVwSelectionPtr qvwsel;
		CheckHr(m_qrootb->get_Selection(&qvwsel));

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

		int ichLeft;
		int ichRight;
		if (ichAnchor > ichEnd)
		{
			ihvoLeft = ihvoEnd;
			ihvoRight = ihvoAnchor;
			ichLeft = ichEnd;
			ichRight = ichAnchor;
		}
		else
		{
			ihvoLeft = ihvoAnchor;
			ihvoRight = ihvoEnd;
			ichLeft = ichAnchor;
			ichRight = ichEnd;
		}
		// Delete the current reference.
		HVO hvoOwn = m_pdfv->GetObj();
		int flid = m_pdfv->GetFlid();
		int ihvo = rgvsli[0].ihvo;
		ISilDataAccessPtr qsda;
		CheckHr(m_qrootb->get_DataAccess(&qsda));
		AfDeFeRefs * pdfr = dynamic_cast<AfDeFeRefs *>(m_pdfv);
		AssertPtr(pdfr);
		// Start undo action (e.g., "Undo Delete Reference in See Also").
		SmartBstr sbstrLabel;
		ITsStringPtr qtss;
		pdfr->GetLabel(&qtss);
		CheckHr(qtss->get_Text(&sbstrLabel));
		StrUni stuUndoFmt;
		StrUni stuRedoFmt;
		StrUtil::MakeUndoRedoLabels(kstidUndoRefDel, &stuUndoFmt, &stuRedoFmt);
		StrUni stuUndo;
		StrUni stuRedo;
		stuUndo.Format(stuUndoFmt.Chars(), sbstrLabel.Chars());
		stuRedo.Format(stuRedoFmt.Chars(), sbstrLabel.Chars());
		CheckHr(qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
		if (pdfr->m_fMultiRefs)
		{
			int iAdd = 0;
			if (((ichRight / 2) * 2) < ichRight)
				iAdd = 1;
			CheckHr(qsda->Replace(hvoOwn, flid, ihvoLeft, ihvoRight + iAdd, NULL, 0));
		}
		else
			CheckHr(qsda->SetObjProp(hvoOwn, flid, 0));
		CheckHr(qsda->EndUndoTask());
		qsda->PropChanged(m_qrootb, kpctNotifyMeThenAll, hvoOwn, flid, ihvo, 0, 1);
		return true;
	}

	default:
		break;
	}
	return SuperClass::OnKeyDown(nChar, nRepCnt, nFlags);
}


/*----------------------------------------------------------------------------------------------
	Process window messages. We intercept Alt+Down to open the chooser and Tab with other
	keys to move the selection or move to a new field.
	See ${AfWnd#FWndProc} for parameter and return descriptions.
----------------------------------------------------------------------------------------------*/
bool AfDeRefsWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_SYSKEYDOWN:
		// Alt+Up and Alt+Down opens chooser.
		if (wp == VK_DOWN || wp == VK_UP)
		{
			AfDeFeRefs * pdfr = dynamic_cast<AfDeFeRefs *>(m_pdfv);
			AssertPtr(pdfr);
			pdfr->ProcessChooser();
			return true;
		}
		if (wp == VK_MENU) // aka "Alt key"
			break;  // let the alt key fall through to SuperClass::FWndProc()

	case WM_KEYDOWN:
		{
			switch (wp)
			{
			case VK_TAB:
				{
					AfDeSplitChild * padsc = dynamic_cast<AfDeSplitChild *>(
						AfWnd::GetAfWnd(::GetParent(m_hwnd)));
					AssertPtr(padsc);
					int nShift = ::GetKeyState(VK_SHIFT);
					if (::GetKeyState(VK_CONTROL) < 0)
					{
						AfDeFeRefs * pdfr = dynamic_cast<AfDeFeRefs *>(m_pdfv);
						AssertPtr(pdfr);
						if (nShift < 0)
							pdfr->PrevItem(); // Move back one item.
						else
							pdfr->NextItem(); // Move forward one item.
					}
					else if (nShift < 0)
						padsc->OpenPreviousEditor(); // Shift Tab to previous editor.
					else
						padsc->OpenNextEditor(); // Tab to next editor.
					return true;
				}

			default:
				// We need to bypass AfVwRootSite::FwndProc to handle Del properly.
				return OnKeyDown(wp, LOWORD(lp), HIWORD(lp));
			}
		}

	case WM_CHAR:
		return true; // All keystrokes are handled in WM_KEYDOWN, so ignore WM_CHAR.

	default:
		break;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Enable/Disable Format toolbar buttons (and the format font command).
	All these are disabled in a reference field; there is nothing here we should be editing, except
	pseudo-editing to select something.
	@param cms The command state object.
	@return True indicating it was handled.
----------------------------------------------------------------------------------------------*/
bool AfDeRefsWnd::CmsCharFmt(CmdState & cms)
{
	if (cms.Cid() == kcidFmtStyles)
		cms.Enable(true);
	else
		cms.Enable(false);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Process the Edit...Cut command.
	@return True indicating it was handled.
----------------------------------------------------------------------------------------------*/
bool AfDeRefsWnd::CmdEditCut(Cmd * pcmd)
{
	// TODO KenZ: This needs to be implemented, but not sure when.
	return false;
}


/*----------------------------------------------------------------------------------------------
	Process the Edit...Copy command.
	@return True indicating it was handled.
----------------------------------------------------------------------------------------------*/
bool AfDeRefsWnd::CmdEditCopy(Cmd * pcmd)
{
	// TODO KenZ: This needs to be implemented, but not sure when.
	return false;
}


/*----------------------------------------------------------------------------------------------
	Process the Edit...Paste command.
	@return True indicating it was handled.
----------------------------------------------------------------------------------------------*/
bool AfDeRefsWnd::CmdEditPaste(Cmd * pcmd)
{
	// TODO KenZ: This needs to be implemented, but not sure when.
	return false;
}


/*----------------------------------------------------------------------------------------------
	Enable/Disable Edit toolbar buttons and menu.
	@param cms The command state object.
	@return True indicating it was handled.
----------------------------------------------------------------------------------------------*/
bool AfDeRefsWnd::CmsEdit(CmdState & cms)
{
	// Once this functionality is implemented, we can use SuperClass enabling.
	cms.Enable(false);
	return true;
}

/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: CleTlsOptDlg.cpp
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Implementation of the Tools Options Dialog class for the list editor.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#include "Set_i.cpp"
#include "Vector_i.cpp"
#include "GpHashMap_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>Main Dialog Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
CleTlsOptDlg::CleTlsOptDlg(void)
{
	m_cTabs = 5;
}


/*----------------------------------------------------------------------------------------------
	Save changes made in the dialog box.
	@return true if Successful
----------------------------------------------------------------------------------------------*/
void CleTlsOptDlg::SaveDialogValues()
{
	WaitCursor wc;

	AfApp::Papp()->SetMsrSys(m_nMsrSys);  // save TlsOptGen copy of value into AfApp

	SaveCustFlds();
	SaveViewValues(kidlgViews);
	SaveFilterValues(CleMainWnd::kimagFilterSimple, CleMainWnd::kimagFilterFull, kidlgFilters);
	SaveSortValues(CleMainWnd::kimagSort, kidlgSortMethods);
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true if Successful
----------------------------------------------------------------------------------------------*/
bool CleTlsOptDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
	AssertPtr(pcmw);
	AfMdiClientWndPtr qmdic = pcmw->GetMdiClientWnd();
	AssertPtr(qmdic);
	AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(qmdic->GetCurChild());
	AssertPtr(qafcrw);
	// Cancel the dialog if we aren't allowed to make changes.
	if (!qafcrw->IsOkToChange())
	{
		SuperClass::OnCancel();
		return true;
	}

	Assert(m_cTabs == 5); // Ensure that the number of dialogs is what we expect.
	m_hwndTab = GetDlgItem(m_hwnd, kcidTlsOptDlgTab);

	AfLpInfo * plpi = pcmw->GetLpInfo();
	AssertPtr(plpi);

	// Setup m_vuvs
	plpi->GetDbInfo()->GetCopyUserViewSpecs(&m_vuvs);
	SetVuvsCopy();

	m_qrmw = pcmw;
	AfViewBarShell * pvwbrs = pcmw->GetViewBarShell();
	AssertPtr(pvwbrs);

	// CAUTION! The order these are inserted is important since we use constant indexes to
	// access them (e.g., kidlgGeneral).
	AfDialogViewPtr qadv;
	qadv.Attach(NewObj TlsOptDlgCst(this));
	m_vdlgv.Push(qadv);
	qadv.Attach(NewObj TlsOptDlgVw(this));
	m_vdlgv.Push(qadv);
	qadv.Attach(NewObj FwFilterDlg(this));
	m_vdlgv.Push(qadv);
	qadv.Attach(NewObj TlsOptDlgSort(this));
	m_vdlgv.Push(qadv);
	qadv.Attach(NewObj TlsOptDlgGen(this));
	m_vdlgv.Push(qadv);

	// Initialize the object classes that we can create in this dialog.
	TlsObject to;
	to.m_nLevel = 0;
	if (!m_tgv.clsid)
		m_tgv.clsid = pcmw->GetRecordClid();
	switch (m_tgv.clsid)
	{
	case kclidCmPossibility:
		{
			to.m_clsid = kclidCmPossibility;
			to.m_strName.Load(kstidTlsOptPossibility);
			m_vto.Push(to);
			to.m_strClsName = "CmPossibility";
			m_vcdi.Push(to);
			break;
		}
	default:
		{
			// All other classes get special treatment for custom stuff.
			switch (m_tgv.clsid)
			{
			default:
				Assert(false);	// Unknown class.
				break;
			case kclidCmPerson:
				to.m_clsid = kclidCmPerson;
				to.m_strName.Load(kstidTlsOptPerson);
				m_vto.Push(to);
				to.m_strClsName = "CmPerson";
				m_vcdi.Push(to);
				to.m_clsid = kclidCmPossibility;
				to.m_nLevel = 1000; // Level 1000 means ALL possibilities
				to.m_strName.Load(kstidTlsOptCstAllPss);
				to.m_strClsName = "CmPossibility";
				m_vcdi.Push(to);
				break;
			case kclidCmLocation:
				to.m_clsid = kclidCmLocation;
				to.m_strName.Load(kstidTlsOptLocation);
				m_vto.Push(to);
				to.m_strClsName = "CmLocation";
				m_vcdi.Push(to);
				to.m_clsid = kclidCmPossibility;
				to.m_nLevel = 1000; // Level 1000 means ALL possibilities
				to.m_strName.Load(kstidTlsOptCstAllPss);
				to.m_strClsName = "CmPossibility";
				m_vcdi.Push(to);
				break;
			case kclidCmAnthroItem:
				to.m_clsid = kclidCmAnthroItem;
				to.m_strName.Load(kstidTlsOptAnthroItem);
				m_vto.Push(to);
				to.m_strClsName = "CmAnthroItem";
				m_vcdi.Push(to);
				to.m_clsid = kclidCmPossibility;
				to.m_nLevel = 1000; // Level 1000 means ALL possibilities
				to.m_strName.Load(kstidTlsOptCstAllPss);
				to.m_strClsName = "CmPossibility";
				m_vcdi.Push(to);
				break;
			case kclidCmCustomItem:
				to.m_clsid = kclidCmCustomItem;
				to.m_strName.Load(kstidTlsOptCustomItem);
				m_vto.Push(to);
				to.m_strClsName = "CmCustomItem";
				m_vcdi.Push(to);
				to.m_clsid = kclidCmPossibility;
				to.m_nLevel = 1000; // Level 1000 means ALL possibilities
				to.m_strName.Load(kstidTlsOptCstAllPss);
				to.m_strClsName = "CmPossibility";
				m_vcdi.Push(to);
				break;
			case kclidLexEntryType:
				to.m_clsid = kclidLexEntryType;
				to.m_strName.Load(kstidTlsOptLexEntryType);
				m_vto.Push(to);
				to.m_strClsName = "LexEntryType";
				m_vcdi.Push(to);
				to.m_clsid = kclidCmPossibility;
				to.m_nLevel = 1000; // Level 1000 means ALL possibilities
				to.m_strName.Load(kstidTlsOptCstAllPss);
				to.m_strClsName = "CmPossibility";
				m_vcdi.Push(to);
				break;
			case kclidMoMorphType:
				to.m_clsid = kclidMoMorphType;
				to.m_strName.Load(kstidTlsOptMoMorphType);
				m_vto.Push(to);
				to.m_strClsName = "MoMorphType";
				m_vcdi.Push(to);
				to.m_clsid = kclidCmPossibility;
				to.m_nLevel = 1000; // Level 1000 means ALL possibilities
				to.m_strName.Load(kstidTlsOptCstAllPss);
				to.m_strClsName = "CmPossibility";
				m_vcdi.Push(to);
				break;
			case kclidPartOfSpeech:
				to.m_clsid = kclidPartOfSpeech;
				to.m_strName.Load(kstidTlsOptPartOfSpeech);
				m_vto.Push(to);
				to.m_strClsName = "PartOfSpeech";
				m_vcdi.Push(to);
				to.m_clsid = kclidCmPossibility;
				to.m_nLevel = 1000; // Level 1000 means ALL possibilities
				to.m_strName.Load(kstidTlsOptCstAllPss);
				to.m_strClsName = "CmPossibility";
				m_vcdi.Push(to);
				// ENHANCE RandyR: Add other things for Wordworks Stage 2.
				break;
			}
			break;
		}
	}

	// Initialize the Custom Define In vector for all classes, but CmPossibility.
	if (m_tgv.clsid != kclidCmPossibility)
	{
	}

	// If we don't get a reasonable values from GetCurClsLevel, we just default to 0 for the
	// index.
	m_ivto = 0;
	for (int ivto = 0; ivto < m_vto.Size(); ++ivto)
	{
		if (m_vto[ivto].m_clsid == m_tgv.clsid
			&& m_vto[ivto].m_nLevel == m_tgv.nLevel)
		{
			m_ivto = ivto;
			break;
		}
	}
	// set the default custom Define In to match the Field In since the combo's will be hidden
	m_iDefaultCstDfn = m_ivto;

	// Initialize the master view types supported by this dialog.
	TlsView tv;
	tv.m_vwt = kvwtDE;
	tv.m_fMaster = true;
	m_vtv.Push(tv);
	tv.m_vwt = kvwtDoc;
	tv.m_fMaster = false;
	m_vtv.Push(tv);

	// Update the General tab.
	TlsOptDlgGen * ptodg = dynamic_cast<TlsOptDlgGen *>(m_vdlgv[kidlgGeneral].Ptr());
	AssertPtr(ptodg);
	ptodg->SetDialogValues(m_vuvs, &m_siwndClientDel);

	int iv1;

	// Update the Views tab.
	TlsOptDlgVw * ptodv = dynamic_cast<TlsOptDlgVw *>(m_vdlgv[kidlgViews].Ptr());
	AssertPtr(ptodv);
	if (m_tgv.itabInitial == kidlgViews && m_tgv.iv1 >= 0)
	{
		iv1 = m_tgv.iv1;
	}
	else
	{
		// Use the current view that is selected.
		Set<int> sisel;
		pvwbrs->GetSelection(m_qrmw->GetViewbarListIndex(kvbltView), sisel);
		iv1 = *sisel.Begin();
	}
	ptodv->SetDialogValues(m_vuvs, &m_siwndClientDel, iv1);

	// Update the Custom Fields tab.
	TlsOptDlgCst * ptodc = dynamic_cast<TlsOptDlgCst *>(m_vdlgv[kidlgCustom].Ptr());
	AssertPtr(ptodc);
	ptodc->SetDialogValues(m_vuvs, &m_siwndClientDel, &m_siCustFldDel);

	// Update the Filters tab.
	FwFilterDlg * pfltdlg = dynamic_cast<FwFilterDlg *>(m_vdlgv[kidlgFilters].Ptr());
	AssertPtr(pfltdlg);
	if (m_tgv.itabInitial == kidlgFilters && m_tgv.iv1 >= 0)
	{
		iv1 = m_tgv.iv1;
	}
	else
	{
		// Use the current filter that is selected.
		Set<int> sisel;
		pvwbrs->GetSelection(m_qrmw->GetViewbarListIndex(kvbltFilter), sisel);
		iv1 = *sisel.Begin() - 1; // Subtract one for the No Filter item.
	}
	pfltdlg->SetDialogValues(pcmw, Max(0, iv1));

	// Update the Sort Methods tab.
	TlsOptDlgSort * psrtdlg = dynamic_cast<TlsOptDlgSort *>(m_vdlgv[kidlgSortMethods].Ptr());
	AssertPtr(psrtdlg);
	if (m_tgv.itabInitial == kidlgSortMethods && m_tgv.iv1 >= 0)
	{
		iv1 = m_tgv.iv1;
	}
	else
	{
		// Use the current sort method that is selected.
		Set<int> sisel;
		pvwbrs->GetSelection(m_qrmw->GetViewbarListIndex(kvbltSort), sisel);
	}
	psrtdlg->SetDialogValues(pcmw, Max(0, iv1));

	// WARNING: If this ever gets changed to anything but a fixed length buffer, make sure
	// ti.pszText is set after loading each string, since the memory pointed to by strb
	// could be different each time.
	StrAppBuf strb;
	TCITEM ti;
	ti.mask = TCIF_TEXT;
	ti.pszText = const_cast<achar *>(strb.Chars());

	// Add a tab to the tab control for each dialog view.
	strb.Load(kstidTlsOptCust);
	TabCtrl_InsertItem(m_hwndTab, kidlgCustom, &ti);
	strb.Load(kstidTlsOptView);
	TabCtrl_InsertItem(m_hwndTab, kidlgViews, &ti);
	strb.Load(kstidTlsOptFltr);
	TabCtrl_InsertItem(m_hwndTab, kidlgFilters, &ti);
	strb.Load(kstidTlsOptSort);
	TabCtrl_InsertItem(m_hwndTab, kidlgSortMethods, &ti);
	strb.Load(kstidTlsOptGen);
	TabCtrl_InsertItem(m_hwndTab, kidlgGeneral, &ti);

	// This section must be after at least one tab gets added to the tab control.
	RECT rcTab;
	::GetWindowRect(m_hwndTab, &rcTab);
	TabCtrl_AdjustRect(m_hwndTab, false, &rcTab);
	POINT pt = { rcTab.left, rcTab.top };
	::ScreenToClient(m_hwnd, &pt);
	m_dxsClient = pt.x;
	m_dysClient = pt.y;

	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	ShowChildDlg(m_tgv.itabInitial);
	m_siwndClientDel.Clear();
	m_siCustFldDel.Clear();

	AfApp::Papp()->EnableMainWindows(false);

	SetFocus(::GetDlgItem(m_hwnd, kcidTlsOptDlgTab));
	return AfDialog::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Returns a RecordSpec containing the BlockSpec pointers for the given
	UserViewSpec (ivuvs) and the given record type (vrt).

	@param vuvs UserViewSpec that contains all the views
	@param ivuvs Index to the view of interest
	@param vrt The record type of interest. (Normally kvrtEvent, kvrtAnal, kvrtSubEvent, etc)
		But in our use, this is always 0 because we are only showing one type of item, even
		though there are different RecordSpecs for each subclass of CmPossibility.
	@param pprsp Out Record Spec to be returned.
----------------------------------------------------------------------------------------------*/
void CleTlsOptDlg::GetBlockVec(UserViewSpecVec & vuvs, int ivuvs, int vrt,
	RecordSpec ** pprsp)
{
	AssertPtr(pprsp);
	ClsLevel clevKey;

	if (vuvs[ivuvs]->m_vwt == kvwtBrowse)
	{
		clevKey.m_clsid = 0;
		clevKey.m_nLevel = 0;
	}
	else
		// switch (vrt)	// We need its clsid, not the actual index,
		// since we only have one item in m_vto.
		clevKey.m_nLevel = 0;
		switch (m_vto[vrt].m_clsid)
		{
		// Fall through for all known cases, since the clid is in m_vto[x].m_clsid.
		case kclidCmPossibility:		// kvrtCmPossibility:
		case kclidCmPerson:				// kvrtCmPerson:
		case kclidCmLocation:			// kvrtCmLocation:
		case kclidCmAnthroItem:			// kvrtCmAnthroItem:
		case kclidCmCustomItem:			// kvrtCmCustomItem:
		case kclidMoMorphType:			// kvrtMoMorphType:
		case kclidLexEntryType:			// kvrtLexEntryType:
		case kclidPartOfSpeech:			// kvrtPartOfSpeech:
			clevKey.m_clsid = m_vto[vrt].m_clsid;
			break;
		default:
			Assert(false);	// Unsupported class.
			break;
		}

	RecordSpecPtr qrsp;
	vuvs[ivuvs]->m_hmclevrsp.Retrieve(clevKey, qrsp);
	*pprsp = qrsp;
	qrsp.Detach(); // Caller gets reference count.
}


/*----------------------------------------------------------------------------------------------
	Returns the visibility that should be used for a new custom field that is created for a
	given view Type.

	@param vwt View type
	@param nrt The record type of interest. (kvrtEvent, kvrtAnal, etc)
	@return field visibility for that ype of record in that type of view.
----------------------------------------------------------------------------------------------*/
FldVis CleTlsOptDlg::GetCustVis(int vwt, int nrt)
{
	switch (vwt)
	{
	case kvwtDoc:
	case kvwtBrowse:
			return kFTVisIfData;
	case kvwtDE:
			return kFTVisAlways;
	default:
		Assert(false);
		break;
	}
	return kFTVisIfData;
}


/*----------------------------------------------------------------------------------------------
	Checks to see if a field should be included in the required listview on the General tab.

	@param flid Flid of the field in question
	@return True if field should be in the list.
----------------------------------------------------------------------------------------------*/
bool CleTlsOptDlg::CheckReqList(int flid)
{
	switch (flid)
	{
		case kflidCmPossibility_DateCreated:
		case kflidCmPossibility_DateModified:
			return false;
		default:
			break;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Gets the label to be used in the Sort Method tab of TlsOpt.

	@param strb label to be used.
	@return true showing a sting was returned.
----------------------------------------------------------------------------------------------*/
StrApp CleTlsOptDlg::GetIncludeLabel()
{
	StrApp strLbl;
	strLbl.Load(kstidSortIncLabel);
	return strLbl;
}


/*----------------------------------------------------------------------------------------------
	Override to return more appropriate index.

	@param cid Command Id.
----------------------------------------------------------------------------------------------*/
int CleTlsOptDlg::GetInitialTabIndex(int cid)
{
	int nIdx;
	switch (cid)
	{
	default:
		nIdx = SuperClass::GetInitialTabIndex(cid);
		break;
	case kcidViewViewsConfig:
		nIdx = kidlgViews;
		break;
	case kcidViewFltrsConfig:
		nIdx = kidlgFilters;
		break;
	case kcidViewSortsConfig:
		nIdx = kidlgSortMethods;
		break;
	}
	return nIdx;
}

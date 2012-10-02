/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: RnTlsOptDlg.cpp
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Implementation of the Tools Options Dialog class for the Data Notebook.
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
RnTlsOptDlg::RnTlsOptDlg(void)
{
	m_cTabs = 6;
}

/*----------------------------------------------------------------------------------------------
	Save changes made in the dialog box.
	@return true if Successful
----------------------------------------------------------------------------------------------*/
void RnTlsOptDlg::SaveDialogValues()
{
	WaitCursor wc;
	AfApp::Papp()->SetMsrSys(m_nMsrSys);  // save TlsOptGen copy of value into AfApp

	SaveCustFlds();
	SaveViewValues(kidlgViews);
	SaveFilterValues(RnMainWnd::kimagFilterSimple, RnMainWnd::kimagFilterFull, kidlgFilters);
	SaveSortValues(RnMainWnd::kimagSort, kidlgSortMethods);
	SaveOverlayValues(RnMainWnd::kimagOverlay, kidlgOverlays);
	if (m_fCustFldDirty)
	{
		// save everything to the Db to make sure that there are not any Locks that will cause
		// problems for other users or apps.
		m_qrmw->SaveData();
		m_fCustFldDirty = false;
	}
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
bool RnTlsOptDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	m_qrmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AfMdiClientWndPtr qmdic = m_qrmw->GetMdiClientWnd();
	AssertPtr(qmdic);
	// Cancel the dialog if we aren't allowed to make changes.
	AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(qmdic->GetCurChild());
	// This might not exist yet if the filter doesn't match anything during startup.
	if (qafcrw && !qafcrw->IsOkToChange())
	{
		SuperClass::OnCancel();
		return true;
	}

	Assert(m_cTabs == 6); // Ensure that the number of dialogs is what we expect.
	m_hwndTab = GetDlgItem(m_hwnd, kcidTlsOptDlgTab);

	AfLpInfo * plpi = m_qrmw->GetLpInfo();
	AssertPtr(plpi);

	// Setup m_vuvs
	plpi->GetDbInfo()->GetCopyUserViewSpecs(&m_vuvs);
	SetVuvsCopy();

	AfViewBarShell * pvwbrs = m_qrmw->GetViewBarShell();
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
	qadv.Attach(NewObj TlsOptDlgOvr(this));
	m_vdlgv.Push(qadv);
	qadv.Attach(NewObj TlsOptDlgGen(this));
	m_vdlgv.Push(qadv);

	// Initialize the object classes that we can create in this dialog.
	TlsObject to;
	to.m_clsid = kclidRnEvent;
	to.m_nLevel = 0;
	to.m_strName.Load(kstidEventEntry);
	m_vto.Push(to);
	to.m_clsid = kclidRnAnalysis;
	to.m_nLevel = 0;
	to.m_strName.Load(kstidAnalEntry);
	m_vto.Push(to);

	// Initialize the Custom Define In vector.
	to.m_clsid = kclidRnEvent;
	to.m_nLevel = 0;
	to.m_strName.Load(kstidEventEntry);
	to.m_strClsName = "RnEvent";
	m_vcdi.Push(to);
	to.m_clsid = kclidRnAnalysis;
	to.m_nLevel = 0;
	to.m_strName.Load(kstidAnalEntry);
	to.m_strClsName = "RnAnalysis";
	m_vcdi.Push(to);
	to.m_clsid = kclidRnGenericRec;
	to.m_nLevel = 1000; // Level 1000 means ALL Entries
	to.m_strName.Load(kstidTlsOptCstAllEnt);
	to.m_strClsName = "RnGenericRec";
	m_vcdi.Push(to);
	m_iDefaultCstDfn = m_vcdi.Size() - 1;

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

	// Initialize the master view types supported by this dialog.
	TlsView tv;
	tv.m_vwt = kvwtBrowse;
	tv.m_fMaster = false;
	m_vtv.Push(tv);
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

	// Update the Custom Fields tab.
	TlsOptDlgCst * ptodc = dynamic_cast<TlsOptDlgCst *>(m_vdlgv[kidlgCustom].Ptr());
	AssertPtr(ptodc);
	ptodc->SetDialogValues(m_vuvs, &m_siwndClientDel, &m_siCustFldDel);

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
		if (sisel.Size())
			iv1 = *sisel.Begin();
		else
		{
			iv1 = -1;
			Assert(!qafcrw);
		}
	}
	if (iv1 >= 0)
		ptodv->SetDialogValues(m_vuvs, &m_siwndClientDel, iv1);

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
		if (sisel.Size())
			iv1 = *sisel.Begin() - 1; // Subtract one for the No Filter item.
		else
		{
			iv1 = -1;
			Assert(!qafcrw);
		}
	}
	pfltdlg->SetDialogValues(m_qrmw, Max(0, iv1));

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
		if (sisel.Size())
			iv1 = *sisel.Begin() - 1; // Subtract one for the No sort item.
		else
		{
			iv1 = -1;
			Assert(!qafcrw);
		}
	}
	psrtdlg->SetDialogValues(m_qrmw, Max(0, iv1));

	// Update the Overlays tab.
	TlsOptDlgOvr * ptodo = dynamic_cast<TlsOptDlgOvr *>(m_vdlgv[kidlgOverlays].Ptr());
	AssertPtr(ptodo);
	if (m_tgv.itabInitial == kidlgOverlays && m_tgv.iv1 >= 0)
	{
		iv1 = m_tgv.iv1;
	}
	else
	{
		// Use the first overlay that is selected.
		Set<int> sisel;
		pvwbrs->GetSelection(m_qrmw->GetViewbarListIndex(kvbltOverlay), sisel);
		if (sisel.Size())
			iv1 = *sisel.Begin() - 1; // Subtract one for the No Overlay item.
		else
		{
			iv1 = -1;
			Assert(!qafcrw);
		}
	}
	ptodo->SetDialogValues(plpi, Max(0, iv1),
		(m_tgv.itabInitial == kidlgOverlays && m_tgv.iv2 >= 0) ? m_tgv.iv2 : 0);

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
	strb.Load(kstidTlsOptOvr);
	TabCtrl_InsertItem(m_hwndTab, kidlgOverlays, &ti);
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
	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Returns a RecordSpec containing the BlockSpec pointers for the given
	UserViewSpec (ivuvs) and the given record type (vrt).

	@param vuvs UserViewSpec that contains all the views
	@param ivuvs Index to the view of interest
	@param vrt The record type of interest. (kvrtEvent, kvrtAnal, etc)
	@param pprsp Out Record Spec to be returned.
----------------------------------------------------------------------------------------------*/
void RnTlsOptDlg::GetBlockVec(UserViewSpecVec & vuvs, int ivuvs, int vrt,
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
		switch (vrt)
		{
		case kvrtEvent:
			clevKey.m_clsid = kclidRnEvent;
			clevKey.m_nLevel = 0;
			break;
		case kvrtAnal:
			clevKey.m_clsid = kclidRnAnalysis;
			clevKey.m_nLevel = 0;
			break;
		default:
			Assert(false);
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
FldVis RnTlsOptDlg::GetCustVis(int vwt, int nrt)
{
	switch (vwt)
	{
	case kvwtDoc:
	case kvwtBrowse:
		switch (nrt)
		{
		case kvrtEvent:
			return kFTVisIfData;
		case kvrtAnal:
			return kFTVisIfData;
		default:
			Assert(false);
			break;
		}
	case kvwtDE:
		switch (nrt)
		{
		case kvrtEvent:
			return kFTVisAlways;
		case kvrtAnal:
			return kFTVisAlways;
		default:
			Assert(false);
			break;
		}
	default:
		Assert(false);
		break;
	}
	return kFTVisIfData;
}


/*----------------------------------------------------------------------------------------------
	Process a browse UserViewSpec. They require extra work since we have a dummy RecordSpec
	with the information we need and this needs to be put into appropriate RecordSpecs for
	each object that is being loaded.
----------------------------------------------------------------------------------------------*/
void RnTlsOptDlg::ProcessBrowseSpec(UserViewSpec * puvs, AfLpInfo * plpi)
{
	ClevRspMap::iterator ithmclevrspLim = puvs->m_hmclevrsp.End();
	for (ClevRspMap::iterator it = puvs->m_hmclevrsp.Begin(); it != ithmclevrspLim; ++it)
	{
		ClsLevel clev = it.GetKey();
		// Delete all RecordSpecs except our dummy and the RnRoledPartic.
		if (clev.m_clsid && clev.m_clsid != kclidRnRoledPartic)
			puvs->m_hmclevrsp.Delete(clev);
	}
	// Now create new record specs as needed.
	CompleteBrowseRecordSpec(puvs, plpi);
}


/*----------------------------------------------------------------------------------------------
	Checks to see if a field should be included in the required listview on the General tab.

	@param flid Flid of the field in question
	@return True if field should be in the list.
----------------------------------------------------------------------------------------------*/
bool RnTlsOptDlg::CheckReqList(int flid)
{
	switch (flid)
	{
		case kflidRnGenericRec_DateCreated:
		case kflidRnGenericRec_DateModified:
			return false;
		default:
			break;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Override to return more appropriate index.

	@param cid Command Id.
----------------------------------------------------------------------------------------------*/
int RnTlsOptDlg::GetInitialTabIndex(int cid)
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
	case kcidViewOlaysConfig:
		nIdx = kidlgOverlays;
		break;
	}
	return nIdx;
}

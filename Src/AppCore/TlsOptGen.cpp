/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TlsOptDlg.cpp
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Implementation of the Tools Options Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

BEGIN_CMD_MAP(TlsOptDlgGen)
	ON_CID_GEN(kcidTlsOptGenModNReq, &TlsOptDlgGen::CmdMod, NULL)
	ON_CID_GEN(kcidTlsOptGenModEnc, &TlsOptDlgGen::CmdMod, NULL)
	ON_CID_GEN(kcidTlsOptGenModReq, &TlsOptDlgGen::CmdMod, NULL)
END_CMD_MAP_NIL()


//:>********************************************************************************************
//:>General Dialog Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
	@param ppsd pointer to the main TlsOptDlg.
----------------------------------------------------------------------------------------------*/
TlsOptDlgGen::TlsOptDlgGen(TlsOptDlg * ptod)
{
	m_rid = kridTlsOptDlgGen;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Options/Options_General_tab.htm");
	m_ptod = ptod;
	m_wsUser = ptod->MainWnd()->UserWs();
}


/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
TlsOptDlgGen::~TlsOptDlgGen()
{
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgGen::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Load Measurement combo
	StrAppBuf strb;
	HWND hwndMsr = ::GetDlgItem(m_hwnd, kcidTlsOptDlgGenMsr);
	strb.Load(kstidInchesTxt);
	::SendMessage(hwndMsr, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidMmTxt);
	::SendMessage(hwndMsr, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidCmTxt);
	::SendMessage(hwndMsr, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	//strb.Load(kstidPtTxt);
	//::SendMessage(hwndMsr, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

	m_ptod->SetMsrSys(AfApp::Papp()->GetMsrSys());
	::SendMessage(hwndMsr, CB_SETCURSEL, (int)m_ptod->GetMsrSys(), 0);

	m_hwndGenFlds = ::GetDlgItem(m_hwnd, kcidTlsOptDlgGenFlds);

	// Subclass the fields listview control as a tsslistview
	TssListViewPtr qtlv;
	qtlv.Create();
	qtlv->SubclassListView(::GetDlgItem(m_hwnd, kcidTlsOptDlgGenFlds), m_wsUser);
	m_hwndGenFlds = qtlv->Hwnd();
	Assert(m_hwndGenFlds == GetDlgItem(m_hwnd, kcidTlsOptDlgGenFlds));
	ListView_SetExtendedListViewStyle(m_hwndGenFlds,LVS_EX_FULLROWSELECT);

	// Read in and setup the Required tss text
	AfUtil::GetResourceTss(kstidTlosOptNotRequired, m_wsUser, &m_rgqFldReq[kFTReqNotReq]);
	AfUtil::GetResourceTss(kstidTlosOptEncouraged, m_wsUser, &m_rgqFldReq[kFTReqWs]);
	AfUtil::GetResourceTss(kstidTlosOptRequired, m_wsUser, &m_rgqFldReq[kFTReqReq]);

	// Load "Fields in:" Combo Box
	HWND hwndFIn = ::GetDlgItem(m_hwnd, kcidTlsOptDlgGenFIn);
	Vector<TlsObject> & vto = m_ptod->ObjectVec();
	for (int i = 0; i < vto.Size(); ++i)
	{
		::SendMessage(hwndFIn, CB_ADDSTRING, 0, (LPARAM)vto[i].m_strName.Chars());
	}

	// Only show the Fields In combo if we should.
	if (!m_ptod->GetfShowFInCbo())
	{
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgGenFInL), SW_HIDE);
		::ShowWindow(hwndFIn, SW_HIDE);
		Rect rcCbo;
		::GetWindowRect(hwndFIn, &rcCbo);
		::MapWindowPoints(NULL, m_hwnd, (POINT *)&rcCbo, 2);

		Rect rcLst;
		::GetWindowRect(::GetDlgItem(m_hwnd, kcidTlsOptDlgGenFlds), &rcLst);
		::MapWindowPoints(NULL, m_hwnd, (POINT *)&rcLst, 2);

		::MoveWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgGenFlds), rcLst.left, rcCbo.top,
			rcLst.Width(), rcLst.Height() + (rcLst.top - rcCbo.top), true);
	}


	// Setup columns in fields list box
	FW_LVCOLUMN flvc;
	flvc.mask = LVCF_TEXT | LVCF_WIDTH;
	flvc.cx = 100;
	AfUtil::GetResourceTss(kstidTlsOptFld, m_wsUser, &flvc.qtss);
	Fw_ListView_InsertColumn(m_hwndGenFlds, 0, &flvc);
	flvc.cx = 95;
	AfUtil::GetResourceTss(kstidTlsOptData, m_wsUser, &flvc.qtss);
	Fw_ListView_InsertColumn(m_hwndGenFlds, 1, &flvc);

	// select the first item in listview control
	FW_LVITEM fwlvi;
	fwlvi.mask = LVIF_STATE;
	fwlvi.iItem = 0;
	fwlvi.iSubItem = 0;
	fwlvi.state = LVIS_SELECTED | LVIS_FOCUSED;
	fwlvi.stateMask = LVIS_SELECTED | LVIS_FOCUSED;
	Fw_ListView_SetItem(m_hwndGenFlds, &fwlvi);
	ListView_EnsureVisible(m_hwndGenFlds, 0, false);
	::SetFocus(m_hwndGenFlds);

	return AfDialog::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.
	@param vuvs The UserViewSpec to be used in this dialog.
	@param psiwndClientDel The delete list that will be updated with any changes.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgGen::SetDialogValues(UserViewSpecVec & vuvs, Set<int> * psiwndClientDel)
{
	AssertPtr(psiwndClientDel);
	m_psiwndClientDel = psiwndClientDel;
}


/*----------------------------------------------------------------------------------------------
	Bring up the Modify Field Required Settings dialog.

	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgGen::ModReq()
{
	int iItem = ListView_GetNextItem(m_hwndGenFlds, -1, LVNI_SELECTED);
	RecordSpecPtr qrsp;

	UserViewSpecVec & vuvs = m_ptod->ViewSpecVec();
	int cuvs = vuvs.Size();
	int iuvs;
	for (iuvs = 0; iuvs < cuvs; ++iuvs)
	{
		if (vuvs[iuvs]->m_vwt == kvwtDE)
			break;
	}
	m_ptod->GetBlockVec(vuvs, iuvs, m_ptod->CurObjVecIndex(), &qrsp);
	BlockVec & vpbsp = qrsp->m_vqbsp;

	// Get the lParam, because it is the index to the fields of the block spec.
	FW_LVITEM fwlvi;
	fwlvi.mask = LVIF_PARAM;
	fwlvi.iItem = iItem;
	fwlvi.iSubItem = 0;
	Fw_ListView_GetItem(m_hwndGenFlds, &fwlvi);
	int ilParam = fwlvi.lParam;
	ITsStringPtr qtssFldName = vpbsp[ilParam]->m_qtssLabel;

	iItem = ilParam;
	FldReq nreq = vpbsp[ilParam]->m_fRequired;

	ModReqDlgPtr qmrd;
	qmrd.Create();
	qmrd->SetDialogValues(nreq);
	if (qmrd->DoModal(m_hwnd) != kctidOk)
		return true;

	qmrd->GetDialogValues(&nreq);

	SaveReqChange(nreq);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Modify Required data field command.

	@param pcmd Ptr to menu command
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgGen::CmdMod(Cmd * pcmd)
{
	AssertObj(pcmd);
	switch (pcmd->m_cid)
	{
	case kcidTlsOptGenModNReq: // Not Required.
		SaveReqChange(kFTReqNotReq);
		break;

	case kcidTlsOptGenModEnc: // Encouraged.
		SaveReqChange(kFTReqWs);
		break;

	case kcidTlsOptGenModReq: // Required.
		SaveReqChange(kFTReqReq);
		break;

	default:
		Assert(false);
		return true;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Updates the vuvs to reflect the new state of required.
	@param newReq state the required flag is to be changed to.

	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgGen::SaveReqChange(FldReq nreq)
{
	int iItem = ListView_GetNextItem(m_hwndGenFlds, -1, LVNI_SELECTED);
	RecordSpecPtr qrsp;
	UserViewSpecVec & vuvs = m_ptod->ViewSpecVec();
	int cuvs = vuvs.Size();
	int iuvs;
	for (iuvs = 0; iuvs < cuvs; ++iuvs)
	{
		if (vuvs[iuvs]->m_vwt == kvwtDE)
			break;
	}

	m_ptod->GetBlockVec(vuvs, iuvs, m_ptod->CurObjVecIndex(), &qrsp);
	BlockVec & vpbsp = qrsp->m_vqbsp;

	// Get the lParam, because it is the index to the fields of the block spec.
	FW_LVITEM fwlvi;
	fwlvi.mask = LVIF_PARAM;
	fwlvi.iItem = iItem;
	fwlvi.iSubItem = 0;
	Fw_ListView_GetItem(m_hwndGenFlds, &fwlvi);
	int ilParam = fwlvi.lParam;
	ITsStringPtr qtssFldName = vpbsp[ilParam]->m_qtssLabel;

	if (nreq != kFTReqReq)
	{
		fwlvi.mask = LVIF_TEXT;
		fwlvi.iSubItem = 1;
		fwlvi.qtss = m_rgqFldReq[(int)nreq];
		Fw_ListView_SetItem(m_hwndGenFlds, &fwlvi);
	}
	bool fShowReqDlg = true;
	for (iuvs = 0; iuvs < cuvs; ++iuvs)
	{
		if (vuvs[iuvs]->m_vwt == kvwtDE)
		{
			m_ptod->GetBlockVec(vuvs, iuvs, m_ptod->CurObjVecIndex(), &qrsp);
			BlockVec & vpbsp = qrsp->m_vqbsp;

			if ((vpbsp[ilParam]->m_fRequired != kFTReqReq) && (nreq == kFTReqReq) && fShowReqDlg)
			{
				if (!CheckAlwaysVisible(qtssFldName))
				{
					// the field has just been changed to Required from another setting
					ModFldSetNtcDlgPtr qmfsnd;
					qmfsnd.Create();
					if (qmfsnd->DoModal(m_hwnd) != kctidOk)
						return true;
					NowRequired(qtssFldName); // This adds windows to the deleted list.
				}
				fwlvi.mask = LVIF_TEXT;
				fwlvi.iSubItem = 1;
				fwlvi.qtss = m_rgqFldReq[(int)nreq];
				Fw_ListView_SetItem(m_hwndGenFlds, &fwlvi);
				fShowReqDlg = false;
			}

			for (int ibsp = 0; ibsp < vpbsp.Size(); ++ibsp)
			{
				ComBool fEquals;
				CheckHr(qtssFldName->Equals(vpbsp[ibsp]->m_qtssLabel, &fEquals));
				if (fEquals)
				{
					vpbsp[ibsp]->m_fRequired = nreq;
					qrsp->m_fDirty = true;
					// Add the window to the delete list which will update it when OK
					// is pressed.
					int iwndClient = vuvs[iuvs]->m_iwndClient;
					if (iwndClient >= 0)
						m_psiwndClientDel->Insert(iwndClient);
					break;
				}
			}
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Called when the dialog becomes active.

	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgGen::SetActive()
{
	HWND hwndFIn = ::GetDlgItem(m_hwnd, kcidTlsOptDlgGenFIn);
	::SendMessage(hwndFIn, CB_SETCURSEL, m_ptod->CurObjVecIndex(), 0);
	UpdateFlds();
	return true;
}

/*----------------------------------------------------------------------------------------------
	Checks to see if this field is always visible in all data entry views.

	@param ptssFldName Name of field that has just changed.
	@return true if field is always visible in all views.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgGen::CheckAlwaysVisible(ITsString * ptssFldName)
{
	int iuvs;
	UserViewSpecVec & vuvs = m_ptod->ViewSpecVec();
	int cuvs = vuvs.Size();
	for (iuvs = 0; iuvs < cuvs; ++iuvs)
	{
		if (vuvs[iuvs]->m_vwt != kvwtDE)
			continue;

		// Process each data entry RecordSpec.
		RecordSpecPtr qrsp;
		ClevRspMap::iterator ithmclevrspLim = vuvs[iuvs]->m_hmclevrsp.End();
		for (ClevRspMap::iterator it = vuvs[iuvs]->m_hmclevrsp.Begin(); it != ithmclevrspLim;
			++it)
		{
			ClsLevel clev = it.GetKey();
			vuvs[iuvs]->m_hmclevrsp.Retrieve(clev, qrsp);
			AssertPtr(qrsp);
			BlockVec & vpbsp = qrsp->m_vqbsp;
			for (int ifld = 0; ifld < vpbsp.Size(); ++ifld)
			{
				ComBool fEqual;
				vpbsp[ifld]->m_qtssLabel->Equals(ptssFldName, &fEqual);
				if (fEqual)
				{
					if (vpbsp[ifld]->m_eVisibility != kFTVisAlways)
						return false;
				}
			}
		}
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	The required field has just been changed to required, so this field in all data entry field
	views must be changed to always visable.

	@param ptssFldName Name of field that has just changed.
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgGen::NowRequired(ITsString * ptssFldName)
{
	int iuvs;
	UserViewSpecVec & vuvs = m_ptod->ViewSpecVec();
	int cuvs = vuvs.Size();
	for (iuvs = 0; iuvs < cuvs; ++iuvs)
	{
		if (vuvs[iuvs]->m_vwt != kvwtDE)
			continue;

		// Process each data entry RecordSpec.
		RecordSpecPtr qrsp;
		ClevRspMap::iterator ithmclevrspLim = vuvs[iuvs]->m_hmclevrsp.End();
		for (ClevRspMap::iterator it = vuvs[iuvs]->m_hmclevrsp.Begin(); it != ithmclevrspLim;
			++it)
		{
			ClsLevel clev = it.GetKey();
			vuvs[iuvs]->m_hmclevrsp.Retrieve(clev, qrsp);
			AssertPtr(qrsp);
			BlockVec & vpbsp = qrsp->m_vqbsp;
			for (int ifld = 0; ifld < vpbsp.Size(); ++ifld)
			{
				ComBool fEqual;
				vpbsp[ifld]->m_qtssLabel->Equals(ptssFldName, &fEqual);
				if (fEqual)
				{
					// Force it to always visible
					vpbsp[ifld]->m_eVisibility = kFTVisAlways;

					qrsp->m_fDirty = true;

					// Add the window to the delete list which will update it when OK
					// is pressed.
					int iwndClient = vuvs[iuvs]->m_iwndClient;
					if (iwndClient >= 0)
						m_psiwndClientDel->Insert(iwndClient);
				}
			}
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Update Fields listView Control.  It removes all items from the listview, then finds any
	Data Entry View and adds in all fields that are in this view.  (All Data Entry Views have
	all fields in them)

	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgGen::UpdateFlds()
{
	::SendMessage(m_hwndGenFlds, LVM_DELETEALLITEMS, 0, 0);

	UserViewSpecVec & vuvs = m_ptod->ViewSpecVec();
	int cuvs = vuvs.Size();
	int iuvs;
	if (!cuvs)
		return true;

	FW_LVITEM fwlvi;
	RecordSpecPtr qrsp;
	for (iuvs = 0; iuvs < cuvs; ++iuvs)
	{
		if (vuvs[iuvs]->m_vwt == kvwtDE)
		{
			m_ptod->GetBlockVec(vuvs, iuvs, m_ptod->CurObjVecIndex(), &qrsp);
			BlockVec & vpbsp = qrsp->m_vqbsp;

			int iLvItem = 0;
			int ifld;
			int inew;
			for (ifld = 0; ifld < vpbsp.Size(); ++ifld)
			{
				const OLECHAR * prgwch;
				int cch;
				CheckHr(vpbsp[ifld]->m_qtssLabel->LockText(&prgwch, &cch));
				StrUni stu(prgwch);
				vpbsp[ifld]->m_qtssLabel->UnlockText(prgwch);
				StrUni stuRs(kstidTlosOptSubentries);
				if (stu.EqualsCI(stuRs))
					continue;

				if (m_ptod->CheckReqList(vpbsp[ifld]->m_flid))
				{
					fwlvi.mask = LVIF_TEXT | LVIF_PARAM;
					fwlvi.iItem = iLvItem;
					fwlvi.iSubItem = 0;
					fwlvi.qtss = vpbsp[ifld]->m_qtssLabel;
					fwlvi.lParam = (LPARAM) ifld;
					inew = Fw_ListView_InsertItem(m_hwndGenFlds, &fwlvi);
					fwlvi.mask = LVIF_TEXT;
					fwlvi.iSubItem = 1;
					fwlvi.iItem = inew;
					fwlvi.qtss = m_rgqFldReq[vpbsp[ifld]->m_fRequired];
					Fw_ListView_SetItem(m_hwndGenFlds, &fwlvi);
					iLvItem++;
				}
			}
			if (vpbsp.Size())
			{
				// select what was selected before.
				fwlvi.mask = LVIF_STATE;
				fwlvi.iItem = 0;
				fwlvi.iSubItem = 0;
				fwlvi.state = LVIS_SELECTED | LVIS_FOCUSED;
				fwlvi.stateMask = LVIS_SELECTED | LVIS_FOCUSED;
				Fw_ListView_SetItem(m_hwndGenFlds, &fwlvi);
				ListView_EnsureVisible(m_hwndGenFlds, 0, false);
			}
			return true;
		}
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle window messages.

	@param wm windows message
	@param wp WPARAM
	@param lp LPARAM
	@param lnRet Value to be returned to the windows.
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgGen::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_ERASEBKGND:
		// this is required to prvent the listview from not painting when selected then covered
		// by another window, then uncovered.  without this the listview will not repaint.
		RedrawWindow(m_hwndGenFlds, NULL , NULL, RDW_ERASE | RDW_FRAME | RDW_INTERNALPAINT |
			RDW_INVALIDATE);
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Process notifications for this dialog from some event on a control.  This method is called
	by the framework.

	@param ctid Id of the control that issued the windows command.
	@param pnmh Windows command that is being passed.
	@param lnRet return value to be returned to the windows command.
	@return true if command is handled.
	See ${AfWnd#OnNotifyChild}
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgGen::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	int iItem;
	Rect rc;
	FW_LVITEM fwlvi;
	StrAnsi sta;
	switch(pnmh->code)
	{
	case CBN_SELCHANGE: // Combo box item changed.
		switch (pnmh->idFrom)
		{
		case kcidTlsOptDlgGenFIn:
			{
				int icb = ::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0);
				m_ptod->SetCurObjVecIndex(icb);
				UpdateFlds();
				return true;
			}
		case kcidTlsOptDlgGenMsr:
			{
				//AfApp::Papp()->SetMsrSys((MsrSysType)::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0));
				m_ptod->SetMsrSys((MsrSysType)::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0));

				return true;
			}
		}
		break;
	case NM_RCLICK:
		if (pnmh->idFrom == kcidTlsOptDlgGenFlds)
		{
			iItem = ListView_GetNextItem(m_hwndGenFlds, -1, LVNI_SELECTED);
			ListView_GetSubItemRect(m_hwndGenFlds, iItem, 1, LVIR_BOUNDS, &rc);
			::ClientToScreen(m_hwndGenFlds,(POINT *)&rc);

			HMENU hmenu = ::LoadMenu(ModuleEntry::GetModuleHandle(),
				MAKEINTRESOURCE(kcidTlsOptGenModMnu));
			HMENU hmenuPopup = ::GetSubMenu(hmenu, 0);
			::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, rc.left, rc.top,
				0, m_hwnd, NULL);
			::DestroyMenu(hmenu);
		}
		break;
	case BN_CLICKED:
		switch (pnmh->idFrom)
		{
		case kcidTlsOptDlgGenMod:
			ModReq();
			return true;
		}
		break;
	}
	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	The hwnd has been attached.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgGen::PostAttach(void)
{
	AfApp::Papp()->AddCmdHandler(this,1);
	return;
}


/*----------------------------------------------------------------------------------------------
	Need to clean up.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgGen::OnReleasePtr()
{
	SuperClass::OnReleasePtr();
	AfApp::Papp()->RemoveCmdHandler(this, 1);
}


//:>********************************************************************************************
//:>Modify Field Settings Notice Dialog Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ModFldSetNtcDlg::ModFldSetNtcDlg(void)
{
	m_rid = kridModFldNtcDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Options/Options_General_tab.htm");
}

/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool ModFldSetNtcDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Subclass the help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	// Add the icon:
	HICON hicon = ::LoadIcon(NULL, IDI_QUESTION);
	if (hicon)
	{
		HWND hwnd = ::GetDlgItem(m_hwnd, kcidTlsOptDlgIcon);
		::SendMessage(hwnd, STM_SETICON, (WPARAM)hicon, (LPARAM)0);
	}

	return AfDialog::OnInitDlg(hwndCtrl, lp);
}


//:>********************************************************************************************
//:>Modify Required Field Dialog Methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ModReqDlg::ModReqDlg(void)
{
	m_rid = kridModReqDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Options/Options_General_tab.htm");
}

/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param req enum showing if the field is required, encourged, or not required.
----------------------------------------------------------------------------------------------*/
void ModReqDlg::SetDialogValues(FldReq req)
{
	m_nReq = req;
	return;
}

/*----------------------------------------------------------------------------------------------
	Gets the values from the dialog.

	@param preq Out enum showing if the field is required, encourged, or not required.
----------------------------------------------------------------------------------------------*/
void ModReqDlg::GetDialogValues(FldReq * preq)
{
	AssertPtr(preq);

	* preq = m_nReq;
	return;
}
/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool ModReqDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	StrAppBuf strb;
	m_fDisableEnChange = false;

	// Subclass the help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	SetReq(m_nReq);
	return AfDialog::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Process notifications for this dialog from some event on a control.  This method is called
	by the framework.

	@param ctid Id of the control that issued the windows command.
	@param pnmh Windows command that is being passed.
	@param lnRet return value to be returned to the windows command.
	@return true if command is handled.
	See ${AfWnd#OnNotifyChild}
----------------------------------------------------------------------------------------------*/
bool ModReqDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	if (m_fDisableEnChange == false)
	{
		switch(pnmh->code)
		{
		case BN_CLICKED:
			switch (pnmh->idFrom)
			{
			case kcidTlsOptDlgModReq:
				SetReq(kFTReqReq);
				break;
			case kcidTlsOptDlgModEn:
				SetReq(kFTReqWs);
				break;
			case kcidTlsOptDlgModNReq:
				SetReq(kFTReqNotReq);
				break;
			}
		}
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Sets the required setting

	@param nreq enum showing if the field is required, encourged, or not required.
----------------------------------------------------------------------------------------------*/
void ModReqDlg::SetReq(FldReq nreq)
{
	// set disable flag to prevent endless loop
	m_fDisableEnChange = true;

		// Set the new type.
	Assert(kFTReqReq == nreq || kFTReqWs == nreq || kFTReqNotReq == nreq);

	m_nReq = nreq;

	// Determine current status.
	bool fReq = (kFTReqReq == m_nReq);
	bool fEn = (kFTReqWs == m_nReq);
	bool fNReq = (kFTReqNotReq == m_nReq);

	// Set the correct radio button and clear the others.
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModReq), BM_SETCHECK,
		fReq ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModEn), BM_SETCHECK,
		fEn ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModNReq), BM_SETCHECK,
		fNReq ? BST_CHECKED : BST_UNCHECKED, 0);

	m_fDisableEnChange = false;
}

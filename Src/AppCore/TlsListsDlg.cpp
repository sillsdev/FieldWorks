/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TlsListsDlg.cpp
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Implementation of the List of lists (Tools Lists) Dialog class.

	TlsListsPrTab : AfWnd - This class is used to manage the tab control on the PossChsrDlg
		window. It is needed to keep the tab control from flickering when it is resized.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Methods
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
TlsListsDlg::TlsListsDlg(void)
{
	m_rid = kridTlsListsDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Topics_Lists.htm");
	m_fPreventCancel = false;
}

/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.
----------------------------------------------------------------------------------------------*/
void TlsListsDlg::SetDialogValues(HVO hvopsslSel, int ws, bool ffromCustFldDlg,
	Vector<HVO> & vpsslId)
{
	m_hvopsslSel = hvopsslSel;
	m_wsMagic = kwsAnals;
	m_ffromCustFldDlg = ffromCustFldDlg;
	m_vfactId = vpsslId;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the writing system from the db for this Possibility List.

	@param hvopssl HVO of the Possibility List that you want the writing systm for.
	@return Writing system for this list.
----------------------------------------------------------------------------------------------*/
int TlsListsDlg::GetWs(HVO hvoPssl)
{
	Assert(hvoPssl);
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);


	StrUni stuCmd;
	ComBool fRealIsNull = TRUE;
	ComBool fMagicIsNull = TRUE;
	int wsReal = 0;
	int wsMagic = 0;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	pdbi->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));
	stuCmd.Format(L"select WritingSystem, WsSelector from CmPossibilityList"
		L" where id = %d", hvoPssl);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&wsReal), sizeof(wsReal),
			&cbSpaceTaken, &fRealIsNull, 0));
		CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&wsMagic), sizeof(wsMagic),
			&cbSpaceTaken, &fMagicIsNull, 0));
	}
	if (!fMagicIsNull)
	{
		Assert(wsMagic);
		return wsMagic;
	}
	else
	{
		Assert(wsReal);
		return wsReal;
	}
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog.
----------------------------------------------------------------------------------------------*/
void TlsListsDlg::GetDialogValues(HVO * hvopsslSel)
{
	AssertPtr(hvopsslSel);
	*hvopsslSel = m_hvopsslSel;
	return;
}

/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)  This is also called to update the spin controls in the dialog.
----------------------------------------------------------------------------------------------*/
bool TlsListsDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	m_hwndList = ::GetDlgItem(m_hwnd, kcidTlsListsLst);

	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetDbAccess(&m_qode);

	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	// Setup the List Box
	LVCOLUMN lvc = { LVCF_TEXT | LVCF_WIDTH };
	Rect rc;
	::GetClientRect(m_hwndList, &rc);
	lvc.cx = rc.Width();
	ListView_InsertColumn(m_hwndList, 0, &lvc);

	LoadList();
	if (m_ffromCustFldDlg)
	{
		StrApp str(kstidOk);
		HWND hwndEdit = GetDlgItem(m_hwnd, kctidOk);
		::SendMessage(hwndEdit, WM_SETTEXT, 0, (long)str.Chars());
	}
	else
	{
		StrApp str(kstidCls);
		HWND hwndEdit = GetDlgItem(m_hwnd, kctidOk);
		::SendMessage(hwndEdit, WM_SETTEXT, 0, (long)str.Chars());
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Loads the Listbox list.
----------------------------------------------------------------------------------------------*/
bool TlsListsDlg::LoadList()
{
	StrApp str;
	int i;
	StrUni stuIds;
	int cv = m_vfactId.Size();
	Assert(cv);

	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	IOleDbCommandPtr qodc;

	// Load the name of a possibility list from the database and put in ListBox.
	const int kcchBuffer = MAX_PATH;
	OLECHAR rgchName[kcchBuffer];
	HVO hvo;
	StrUni stu;
	::SendMessage(m_hwndList, WM_SETREDRAW, false, 0);
	for (i = 0; i < cv; ++i)
	{
		hvo = m_vfactId[i];
		stu.Format(L"exec GetOrderedMultiTxt '%d', %d",
			hvo, kflidCmMajorObject_Name);
		CheckHr(m_qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		Assert(fMoreRows); // This proc should always return something.
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchName),
			kcchBuffer * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
		str = rgchName;
		LVITEM lvi = { LVIF_TEXT | LVIF_PARAM };
		lvi.iItem = ListView_GetItemCount(m_hwndList);
		lvi.pszText = const_cast<achar *>(str.Chars());
		lvi.lParam = hvo;
		ListView_InsertItem(m_hwndList, &lvi);
	}
	LVFINDINFO lvfi = { LVFI_PARAM};
	lvfi.lParam = m_hvopsslSel;
	int icursel = ListView_FindItem(m_hwndList, -1, &lvfi);
	if (icursel == -1)
		icursel = 0;

	LVITEM lvi = { LVIF_STATE };
	lvi.mask = LVIF_STATE;
	lvi.iItem = icursel;
	lvi.iSubItem = 0;
	lvi.state = LVIS_SELECTED | LVIS_FOCUSED;
	lvi.stateMask = LVIS_SELECTED | LVIS_FOCUSED;
	ListView_SetItem(m_hwndList, &lvi);
	ListView_EnsureVisible(m_hwndList, icursel, false);

	::SendMessage(m_hwndList, WM_SETREDRAW, true, 0);
	::InvalidateRect(m_hwndList, NULL, true);

	UpdateCtrls();
	return true;
}

/*----------------------------------------------------------------------------------------------
	Update Diaglog changes.
----------------------------------------------------------------------------------------------*/
void TlsListsDlg::UpdateCtrls()
{
	// Get the selected item.
	int icursel = ListView_GetNextItem(m_hwndList, -1, LVNI_SELECTED);
	Assert(icursel != LB_ERR); // something should always be selected.
	LVITEM lvi;
	lvi.mask = LVIF_PARAM;
	lvi.iItem = icursel;
	lvi.iSubItem = 0;
	ListView_GetItem(m_hwndList, &lvi);
	HVO hvoCur = lvi.lParam;

	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	IOleDbCommandPtr qodc;

	StrUni stuQuery;

	stuQuery.Format(L"select Owner$ from CmObject where Class$ =  %d and "
		L"ID = %d" ,kclidCmPossibilityList, hvoCur);

	CheckHr(m_qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));

	HVO hvoOwner;
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoOwner),
			isizeof(hvoOwner), &cbSpaceTaken, &fIsNull, 0));
		if (hvoOwner)
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsListsDel), 0);
		else
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsListsDel), 1);
	}
	else
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsListsDel), 1);
}


/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool TlsListsDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	NMLISTVIEW * pnmlv = (NMLISTVIEW *)pnmh;
	StrAppBuf strb;
	StrAppBuf strb2;

	switch(pnmh->code)
	{
	case BN_CLICKED:
		{
			if	(m_fPreventCancel)
				return true;

			switch (pnmh->idFrom)
			{
			case kcidTlsListsAdd:
				m_fCustFldDirty = true;
				OnAddList(false);
				OnProperty();
				return true;

			case kcidTlsListsCopy:
				m_fCustFldDirty = true;
				OnAddList(true);
				OnProperty();
				return true;

			case kcidTlsListsMod:
				OnModify();
				return false;

			case kcidTlsListsDel:
				OnDelList();
				m_fCustFldDirty = true;
				return true;

			case kcidTlsListsProp:
				OnProperty();
				m_fCustFldDirty = true;
				return true;

			default:
				return AfWnd::OnNotifyChild(ctid, pnmh, lnRet);
			}
		}
		break;

	case LVN_ITEMCHANGED:
		if (pnmh->idFrom == kcidTlsListsLst)
		{
			if (pnmlv->uNewState & LVIS_SELECTED)
			{
				m_hvopsslSel = pnmlv->lParam;
				UpdateCtrls();
			}
		}
		break;

	case LVN_ITEMCHANGING:
		{
			// If the user clicked on an empty part of the list view, keep the selection
			// on the current item.
			NMLISTVIEW * pnmlv = (NMLISTVIEW *)pnmh;
			if (pnmlv->uChanged & LVIF_STATE && !(pnmlv->uNewState & LVIS_SELECTED))
			{
				// NOTE: This can also be called when the keyboard is used to select a different
				// item. In this case, we don't want to cancel the new selection.
				if (::GetKeyState(VK_LBUTTON) < 0 || ::GetKeyState(VK_RBUTTON) < 0)
				{
					LVHITTESTINFO lvhti;
					::GetCursorPos(&lvhti.pt);
					::ScreenToClient(pnmh->hwndFrom, &lvhti.pt);
					if (ListView_HitTest(pnmh->hwndFrom, &lvhti) == -1)
					{
						lnRet = true;
						return true;
					}
				}
			}
		}
		break;
	}

	return AfWnd::OnNotifyChild(ctid, pnmh, lnRet);
}

/*----------------------------------------------------------------------------------------------
	This method shows the properties dialog.
	@return true
----------------------------------------------------------------------------------------------*/
void TlsListsDlg::OnProperty()
{
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
	AssertPtr(pdapp);

	int nResponse;
	int wsMagic = GetWs(m_hvopsslSel);
	nResponse = pdapp->TopicsListProperties(plpi, m_hvopsslSel, wsMagic, m_hwnd);
	if (nResponse == kctidOk)
	{
		SyncInfo sync(ksyncFullRefresh, 0, 0);
		plpi->StoreAndSync(sync);
	}
}


/*----------------------------------------------------------------------------------------------
	Synchronize all windows in this application with any changes made in the database.
	@param sync -> The information describing a given change.
----------------------------------------------------------------------------------------------*/
bool TlsListsDlg::Synchronize(SyncInfo & sync)
{
	if (sync.msg == ksyncPossList)
	{
		ListView_DeleteAllItems(m_hwndList);
		LoadList();
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	This method notifies the user that it must save the all changes, then launches the List
	Editor.
	@return true
----------------------------------------------------------------------------------------------*/
void TlsListsDlg::OnModify()
{
	// Start the list Editor
	IFwToolPtr qtool;
	try
	{
		MSG message;
		if (::PeekMessage(&message, NULL, WM_PAINT, WM_PAINT, PM_REMOVE))
			::DispatchMessage(&message);
		WaitCursor wc;
		CLSID clsid;
		StrUni stu(kpszCleProgId);
		CheckHr(::CLSIDFromProgID(stu.Chars(), &clsid));
		qtool.CreateInstance(clsid);
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
		AssertPtr(prmw);
		AfLpInfo * plpi = prmw->GetLpInfo();
		AssertPtr(plpi);
		AfDbInfo * pdbi = plpi->GetDbInfo();
		AssertPtr(pdbi);

		// Always save the database prior to opening the list editor to avoid locks. Locks can
		// happen even when the user doesn't intentionally modify the database (e.g., UserViews
		// are written the first time a ListEditor is opened on a newly created database.)
		prmw->SaveData();
		m_fCustFldDirty = false;

		long htool;
		int nPid;
		Vector<HVO> vhvo;
		// We don't want to pass in a flid array, but if we try to pass in a null
		// vector, the marshalling process complains. So we need to use this kludge
		// to get it to work.
		int flidKludge;
		int nView = -1; // Default to data entry.

		vhvo.Push(m_hvopsslSel);

		PossListInfoPtr qpli;

		int ws = GetWs(m_hvopsslSel);
		plpi->LoadPossList(m_hvopsslSel, ws, &qpli);
		Assert(qpli);
		qtool->NewMainWndWithSel((wchar *)pdbi->ServerName(), (wchar *)pdbi->DbName(),
			plpi->GetLpId(), qpli->GetPsslId(), qpli->GetWs(), 0, 0,
			vhvo.Begin(), vhvo.Size(), &flidKludge, 0, 0, nView,
			&nPid,
			&htool); // value you can pass to CloseMainWnd if you want.

			// Bring the new window to the top.
			::SetForegroundWindow((HWND)htool);

		/*
		// Wait for the new list editor to close.
		HANDLE hproc = ::OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, nPid);
		qtool.Clear();	// THIS IS CRUCIAL FOR ::GetExitCodeProcess() BELOW TO WORK!
		if (hproc)
		{
			DWORD nCode;
			while (::GetExitCodeProcess(hproc, &nCode))
			{
				if (nCode != STILL_ACTIVE)
					break;
				if (::PeekMessage(&message, NULL, WM_PAINT, WM_PAINT, PM_REMOVE))
					::DispatchMessage(&message);
			}
			::CloseHandle(hproc);
		}
		else
		{
			while (::IsWindow((HWND)htool))
			{
				if (::PeekMessage(&message, NULL, WM_PAINT, WM_PAINT, PM_REMOVE))
					::DispatchMessage(&message);
			}
		}

		// Now we need to redisplay the possibility list, with the newly modified information!
		// (This may not be the most efficient way to do this, but so what?)
/*
		// Reload the tree control.  (This may not be the most efficient way to do
		// this, but so what?)
		HWND hwndTree = m_rghwndTree[kiChoiceList];
		::SendMessage(hwndTree, WM_SETREDRAW, false, 0);
		s_fIgnoreSelChange = true;
		TreeView_DeleteAllItems(hwndTree);
		s_fIgnoreSelChange = false;
		AddPossibilities();
		::SendMessage(hwndTree, WM_SETREDRAW, true, 0);

		UpdateCurrentChoices();
		::SendMessage(hwndTree, WM_SETREDRAW, true, 0);
*/
	}
	catch (...)
	{
		StrApp str(kstidCannotLaunchListEditor);
		::MessageBox(m_hwnd, str.Chars(), NULL, MB_OK | MB_ICONSTOP);
		return;
	}

}


/*----------------------------------------------------------------------------------------------
	Delete a List.

	Note: much of this code is duplicated in CleOpenProjDlg::CmdRemoveList(), so any bugs here
	may need to be fixed there as well.

	@return true
----------------------------------------------------------------------------------------------*/
bool TlsListsDlg::OnDelList()
{
	if	(m_fPreventCancel)
			return false;
	m_fPreventCancel = true;
	try
	{
		HVO hvoCur;
		int icursel = ListView_GetNextItem(m_hwndList, -1, LVNI_SELECTED);
		Assert(icursel != LB_ERR); // something should always be selected.
		LVITEM lvi;
		lvi.mask = LVIF_PARAM;
		lvi.iItem = icursel;
		lvi.iSubItem = 0;
		ListView_GetItem(m_hwndList, &lvi);
		hvoCur = lvi.lParam;

		AfMainWnd * pafw = MainWindow();
		Assert(pafw); // Should be able to find it by PostAttach().
		AfLpInfo * plpi = pafw->GetLpInfo();
		AssertPtr(plpi);
		UserViewSpecVec vuvs = plpi->GetDbInfo()->GetUserViewSpecs();
		Vector<TlsObject> vto; // List of objects.

		// Find a DataEntry View and see if the list is referenced by any fields
		for (int iuv = 0; iuv < vuvs.Size(); ++iuv)
		{
			if (vuvs[iuv]->m_vwt != kvwtDE)
				continue;

			// Process each data entry RecordSpec.
			RecordSpecPtr qrsp;
			ClevRspMap::iterator ithmclevrspLim = vuvs[iuv]->m_hmclevrsp.End();
			for (ClevRspMap::iterator it = vuvs[iuv]->m_hmclevrsp.Begin(); it != ithmclevrspLim;
				++it)
			{
				ClsLevel clev = it.GetKey();
				vuvs[iuv]->m_hmclevrsp.Retrieve(clev, qrsp);
				AssertPtr(qrsp);
				for (int ifld = 0; ifld < qrsp->m_vqbsp.Size(); ++ifld)
				{
					if (qrsp->m_vqbsp[ifld]->m_hvoPssl != hvoCur)
						continue;
					// This list is used by a custom field so notify the user then exit.
					const achar * pszHelpUrl;
					pszHelpUrl = m_pszHelpUrl;

					m_pszHelpUrl = _T("DeletingAViewFilterSortMethodO.htm");
					StrApp strLab(kstidTlsListsNotDel);
					StrApp strM(kstidTlsListsNotDelMsg);
					StrApp strMsg;

					StrApp strCust;
					const OLECHAR * prgwch;
					int cch;
					qrsp->m_vqbsp[ifld]->m_qtssLabel->LockText(&prgwch, &cch);
					strCust.Assign(prgwch, cch);
					qrsp->m_vqbsp[ifld]->m_qtssLabel->UnlockText(prgwch);

					strMsg.Format(strM.Chars(),strCust.Chars());
					::MessageBox(m_hwnd, strMsg.Chars(), strLab.Chars(), MB_HELP |
						MB_OK | MB_DEFBUTTON2 | MB_ICONINFORMATION);
					m_pszHelpUrl = pszHelpUrl;
					m_fPreventCancel = false;
					return true;
				}
			}
		}

		// This list is NOT used by a custom field so notify the user then we can delete it.
		StrApp strTitle(kstidTlsListsDel);
		StrApp strPrompt(kstidTlsListsDMsg);

		const achar * pszHelpUrl;
		pszHelpUrl = m_pszHelpUrl;
		m_pszHelpUrl = _T("User_Interface/Menus/Tools/Delete_A_Topics_List.htm");

		ConfirmDeleteDlgPtr qcdd;
		qcdd.Create();
		qcdd->SetTitle(strTitle.Chars());
		qcdd->SetPrompt(strPrompt.Chars());
		qcdd->SetHelpUrl(m_pszHelpUrl);
		// Make sure the user really wants to delete the list.
		if (qcdd->DoModal(m_hwnd) != kctidOk)
		{
			m_pszHelpUrl = pszHelpUrl;
			m_fPreventCancel = false;
			return true;
		}
		m_pszHelpUrl = pszHelpUrl;

		// It's essential that we not allow partial updates or we can damage the database to where
		// a user can't get started again.
		try
		{
			m_qode->BeginTrans();
			IOleDbCommandPtr qodc;
			StrUni stuSql;
			WaitCursor wc;
			CheckHr(m_qode->CreateCommand(&qodc));
			stuSql.Format(L"EXEC DeleteObjects '%d';", hvoCur);
			CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

			ListView_DeleteItem(m_hwndList, icursel);
			if (icursel == ListView_GetItemCount(m_hwndList))
				--icursel;
			lvi.mask = LVIF_STATE;
			lvi.iItem = icursel;
			lvi.iSubItem = 0;
			lvi.state = LVIS_SELECTED | LVIS_FOCUSED;
			lvi.stateMask = LVIS_SELECTED | LVIS_FOCUSED;
			ListView_SetItem(m_hwndList, &lvi);
			ListView_EnsureVisible(m_hwndList, icursel, false);

			int ihvo;
			for (ihvo = 0; ihvo < m_vfactId.Size(); ++ihvo)
			{
				if (m_vfactId[ihvo] == hvoCur)
				{
					m_vfactId.Delete (ihvo);
					break;
				}
			}

			Vector<HVO> & vhvo = plpi->GetPsslIds();
			for (ihvo = 0; ihvo < vhvo.Size(); ++ihvo)
			{
				if (vhvo[ihvo] == hvoCur)
				{
					vhvo.Delete (ihvo);
					break;
				}
			}
			m_qode->CommitTrans();
			UpdateCtrls();
		}
		catch(...)
		{
			m_qode->RollbackTrans();
			throw;	// For now we have nothing to add, so pass it on up.
		}
	}
	catch(...)
	{
	}
	m_fPreventCancel = false;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Add or Copy a List.

	@param fcopyList True if the Copy button was pressed, or false if Add was pressed.
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsListsDlg::OnAddList(bool fcopyList)
{
	if	(m_fPreventCancel)
			return false;
	m_fPreventCancel = true;
	try
	{
		HVO hvoCur;
		StrApp str;

		if (fcopyList)
		{
			// Get the selected item.
			int icursel = ListView_GetNextItem(m_hwndList, -1, LVNI_SELECTED);
			Assert(icursel != LB_ERR); // something should always be selected.
			achar rgch[MAX_PATH];
			LVITEM lvi;
			lvi.mask = LVIF_PARAM | LVIF_TEXT;
			lvi.pszText = rgch;
			lvi.cchTextMax = MAX_PATH;
			lvi.iItem = icursel;
			lvi.iSubItem = 0;
			ListView_GetItem(m_hwndList, &lvi);
			hvoCur = lvi.lParam;

			str.Load(kstidTlsListsCpyLst);
			str.Append(lvi.pszText);
		}
		else
		{
			str.Load(kstidTlsListsAddLst);
		}

		StrApp str1;
		StrApp str2;

		str1 = str;
		// Check the field name to see if it is already in the list. If it is then make a name
		// with a number appended.Keep trying until we get a name that is not in the list.
		int ncnt = 0;
		bool found = true;
		do
		{
			found = false;
			LVFINDINFO lvfi = {LVFI_STRING};
			lvfi.psz = str.Chars();
			int icursel = ListView_FindItem(m_hwndList, -1, &lvfi);
			if (icursel > -1)
			{
				// The field name is already in the list: make a name with a number appended.
				ncnt ++;
				str = str1;
				str2.Format(_T("%d"), ncnt);
				str.Append(str2.Chars(), str2.Length());
				found = true;
			}
		} while (found == true);

		AfMainWnd * pafw = MainWindow();
		AssertPtr(pafw);
		AfLpInfo * plpi = pafw->GetLpInfo();
		AssertPtr(plpi);
		int ws = plpi->ActualWs(m_wsMagic);

		StrUni stuName(str.Chars());
		AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
		AssertPtr(pdapp);
		HVO hvo = pdapp->NewTopicsList(m_qode, stuName, ws, m_wsMagic, fcopyList ? hvoCur : -1);

		if (hvo < 0)
		{
			m_fPreventCancel = false;
			return false;
		}

		LVITEM lvi = { LVIF_TEXT | LVIF_PARAM };
		lvi.iItem = ListView_GetItemCount(m_hwndList);
		lvi.pszText = const_cast<achar *>(str.Chars());
		lvi.lParam = hvo;
		ListView_InsertItem(m_hwndList, &lvi);

		LVFINDINFO lvfi = { LVFI_PARAM};
		lvfi.lParam = hvo;
		int icursel = ListView_FindItem(m_hwndList, -1, &lvfi);

		lvi.mask = LVIF_STATE;
		lvi.iItem = icursel;
		lvi.iSubItem = 0;
		lvi.state = LVIS_SELECTED | LVIS_FOCUSED;
		lvi.stateMask = LVIS_SELECTED | LVIS_FOCUSED;
		ListView_SetItem(m_hwndList, &lvi);
		ListView_EnsureVisible(m_hwndList, icursel, false);

		m_vfactId.Push(hvo);
		plpi->AddPsslId(hvo);
		UpdateCtrls();
	}
	catch(...)
	{
	}
	m_fPreventCancel = false;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool TlsListsDlg::OnApply(bool fClose)
{
	if (m_fPreventCancel)
		return false;
	if (m_fCustFldDirty)
	{
		// save everything to the Db to make sure that there are not any Locks that will cause
		// problems for other users or apps.
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
		AssertObj(prmw);
		prmw->SaveData();
		m_fCustFldDirty = false;
	}
	return AfDialog::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool TlsListsDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}
/*----------------------------------------------------------------------------------------------
	Handle the Cancel button.
----------------------------------------------------------------------------------------------*/
bool TlsListsDlg::OnCancel()
{
	if (m_fPreventCancel)
		return false;
	return SuperClass::OnCancel();
}

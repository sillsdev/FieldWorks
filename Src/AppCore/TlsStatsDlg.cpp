	/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
	Copyright (C) 2000, 2001, 2003 SIL International. All rights reserved.

	Distributable under the terms of either the Common Public License or the
	GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TlsStatsDlg.cpp
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Implementation of the List of Reports (Tools Reports) Dialog class.

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#include "HashMap_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Methods
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
TlsStatsDlg::TlsStatsDlg(void)
{
	m_rid = kridTlsStatsDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/List_Statistics.htm");
}

/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.
----------------------------------------------------------------------------------------------*/
void TlsStatsDlg::SetDialogValues(int ws, Vector<HVO> & vpsslId,
	Vector<bool> & vfDisplaySettings)
{
	m_wsMagic = kwsAnals;
	Assert(vpsslId.Size());
	m_vfactId = vpsslId;
	m_vfDisplaySettings = vfDisplaySettings;
}


/*----------------------------------------------------------------------------------------------
	Changing size.
----------------------------------------------------------------------------------------------*/
bool TlsStatsDlg::OnSize(int wst, int dxp, int dyp)
{
#ifdef DEBUG_TlsSTATSDLG
	StrAnsi staTemp = "TlsStatsDlg::OnSize:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif

	uint grfnMove = SWP_NOZORDER | SWP_NOSIZE;
	uint grfnSize = SWP_NOZORDER | SWP_NOMOVE;

	// Resize group box
	HWND hwndGroupBox;
	Rect rcGroupBox;
	hwndGroupBox = ::GetDlgItem(m_hwnd, kcidTlsStatGB);
	::GetWindowRect(hwndGroupBox, &rcGroupBox);
	::SetWindowPos(hwndGroupBox, NULL, 0, 0, dxp - m_dxGroupBox, dyp - m_dyGroupBox, grfnSize);
	::InvalidateRect(hwndGroupBox, NULL, true);

	// Resize stats list "button" control
	HWND hwndStatList;
	Rect rcStatList;
	hwndStatList = ::GetDlgItem(m_hwnd, kcidTlsStatList);
	::GetWindowRect(hwndStatList, &rcStatList);
	::SetWindowPos(hwndStatList, NULL, 0, 0, dxp - m_dxStatList, dyp - m_dyStatList, grfnSize);
	::InvalidateRect(hwndStatList, NULL, true);

	// Get the client size (in screen coordinates).
	Rect rcClient;
	GetClientRect(rcClient);
	::MapWindowPoints(m_hwnd, NULL, (POINT *)&rcClient, 2);

	// Move the four buttons (at the bottom of the dialog).
	HWND hwndButton;
	Rect rcButton;
	int xInClient;

	hwndButton = ::GetDlgItem(m_hwnd, kcidTlsStatCopy);
	::GetWindowRect(hwndButton, &rcButton);
	xInClient = rcButton.left - rcClient.left;
	::SetWindowPos(hwndButton, NULL, xInClient, dyp - m_yButtons, 0, 0, grfnMove);
	::InvalidateRect(hwndButton, NULL, true);

	hwndButton = ::GetDlgItem(m_hwnd, kcidTlsStatPrint);
	::GetWindowRect(hwndButton, &rcButton);
	xInClient = rcButton.left - rcClient.left;
	::SetWindowPos(hwndButton, NULL, xInClient, dyp - m_yButtons, 0, 0, grfnMove);
	::InvalidateRect(hwndButton, NULL, true);

	hwndButton = ::GetDlgItem(m_hwnd, kcidTlsStatClose);
	::GetWindowRect(hwndButton, &rcButton);
	xInClient = rcButton.left - rcClient.left;
	::SetWindowPos(hwndButton, NULL, xInClient, dyp - m_yButtons, 0, 0, grfnMove);
	::InvalidateRect(hwndButton, NULL, true);

	hwndButton = ::GetDlgItem(m_hwnd, kctidHelp);
	::GetWindowRect(hwndButton, &rcButton);
	xInClient = rcButton.left - rcClient.left;
	::SetWindowPos(hwndButton, NULL, xInClient, dyp - m_yButtons, 0, 0, grfnMove);
	::InvalidateRect(hwndButton, NULL, true);

	// Move the gripper to the bottom right.
	Rect rc;
	::GetWindowRect(m_hwndGrip, &rc);
	::MoveWindow(m_hwndGrip, dxp - rc.Width(), dyp - rc.Height(), rc.Width(), rc.Height(),
		true);
	::InvalidateRect(m_hwndGrip, NULL, true);

	return SuperClass::OnSize(wst, dxp, dyp);
}


/*----------------------------------------------------------------------------------------------
	Do not allow the dialog to be resized smaller than when it was initialized.
----------------------------------------------------------------------------------------------*/
bool TlsStatsDlg::OnSizing(int wse, RECT * prc)
{
#ifdef DEBUG_TLSSTATSDLG
	StrAnsi staTemp = "TlsStatsDlg::OnSizing:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	AssertPtr(prc);

	if (prc->right - prc->left < m_xMin)
	{
		if (wse == WMSZ_TOPLEFT || wse == WMSZ_LEFT || wse == WMSZ_BOTTOMLEFT)
			prc->left = prc->right - m_xMin;
		else
			prc->right = prc->left + m_xMin;
	}

	if (prc->bottom - prc->top < m_yMin)
	{
		if (wse == WMSZ_TOPLEFT || wse == WMSZ_TOP || wse == WMSZ_TOPRIGHT)
			prc->top = prc->bottom - m_yMin;
		else
			prc->bottom = prc->top + m_yMin;
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool TlsStatsDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
#ifdef DEBUG_TLSSTATSRDLG
	StrAnsi staTemp = "TlsStatsDlg::FWndProc:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	switch (wm)
	{
	case WM_SIZING:
		return OnSizing(wp, (RECT *)lp);
	// default: nothing special.
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Synchronize all windows in this application with any changes made in the database.
	@param sync -> The information describing a given change.
----------------------------------------------------------------------------------------------*/
bool TlsStatsDlg::Synchronize(SyncInfo & sync)
{
	if (sync.msg == ksyncPossList)
	{
		ClearMap();
		GetCounts();
		m_qtsl->Redraw();
//		ListView_DeleteAllItems(m_hwndList);
//		LoadList();
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Get the counts from the database and load it into the list box.

	@param hvopssl HVO of the Possibility List that you want the writing systm for.
	@return Writing system for this list.
----------------------------------------------------------------------------------------------*/
void TlsStatsDlg::GetCounts()
{
	HWND hwnd;
	bool fChecked;

	hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatAbbr);
	fChecked = (::SendMessage(hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED);
	m_qtsl->SetShowAbbr(fChecked);

	hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatZero);
	fChecked = (::SendMessage(hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED);
	m_qtsl->SetShowZero(! fChecked);

	hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatSub);
	fChecked = (::SendMessage(hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED);
	m_qtsl->SetIncludeSubitems(fChecked);

	hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatListCb);
	int iSel = ::SendMessage(hwnd, CB_GETCURSEL, 0, 0);
	Assert(iSel != -1);
	if (iSel == -1)
		return;
	HVO hvoPssl = ::SendMessage(hwnd, CB_GETITEMDATA, (WPARAM)iSel, 0);
	Assert(hvoPssl);
	if (hvoPssl == -1)
		return;
	m_hvopsslSel = hvoPssl;

	hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatAsc);
	fChecked = (::SendMessage(hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED);
	m_qtsl->SetStatSortAsc(fChecked);

	PossListInfoPtr qpli;
	int ws = GetWs(hvoPssl);
	m_plpi->LoadPossList(hvoPssl, ws, &qpli);
	m_wsMagic = ws;
	Assert(qpli);

	AfDbInfo * pdbi = m_plpi->GetDbInfo();
	AssertPtr(pdbi);
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	pdbi->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));

	Vector<HVO> & vPsslIds = m_plpi->GetPsslIds();
	int iFoundPos = -1;
	for (int i = 0; i < vPsslIds.Size(); i++)
	{
		if (vPsslIds[i] == hvoPssl)
		{
			iFoundPos = i;
			break;
		}
	}

	Assert(iFoundPos != -1);

	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	StrUni stuCmd;
	pafw->GetStatsQuery(iFoundPos, &stuCmd);

	Assert(stuCmd.Length());

	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	ComBool fMoreRows;
	CheckHr(qodc->NextRow(&fMoreRows));
	ComBool fIsNull;
	fIsNull = true;

	StrUni stuOutput;
	HVO hvoItem;
	HVO hvoOld = NULL;
	int cItems = 0;

	ULONG cbSpaceTaken;
	HVO hvoParent;
	// pass one - get counts for all items in our query
	while (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoItem), sizeof(hvoItem),
			&cbSpaceTaken, &fIsNull, 0));
		if (fIsNull)
		{
			CheckHr(qodc->NextRow(&fMoreRows));
			continue;
		}
		if (hvoOld != hvoItem)
		{
			if (hvoOld != NULL)
			{
				StItemInfo *stTemp = new StItemInfo;
				stTemp->cItems = cItems;
				stTemp->cChildren = 0;
				stTemp->hvoParent = hvoParent;

				m_mapCount[hvoOld] = stTemp;	// .insert(std::make_pair(hvoOld, stTemp));
			}

			hvoParent = qpli->GetOwnerIdFromId(hvoItem);

			if (hvoParent == -1)
			{
				CheckHr(qodc->NextRow(&fMoreRows));
				hvoOld = hvoItem = NULL;
				continue;
			}

			int ipss;
			PossListInfoPtr qpliTemp;
			m_plpi->GetPossListAndItem(hvoItem, ws, &ipss, &qpliTemp);
			AssertPtr(qpliTemp);

			cItems = 1;
		}
		else
		{
			cItems++;
		}
		hvoOld = hvoItem;

		CheckHr(qodc->NextRow(&fMoreRows));
	}

	if (hvoItem)
	{
		StItemInfo *stTemp = new StItemInfo;
		stTemp->cItems = cItems;
		stTemp->cChildren = 0;
		stTemp->hvoParent = hvoParent;
		m_mapCount.insert(std::make_pair(hvoOld, stTemp));
	}

	// Now loop again over all items in the recordset and add counts for children and all
	// of the parents up the chain.

	std::map<HVO, StItemInfo*>::iterator mapIt;
	std::map<HVO, StItemInfo*> mapAddedParents;

	//
	// End of pass one: now have list items with their parents and counts.
	//
	int cLoop = 0;
	int numNodesVisited = 0;
	for (mapIt = m_mapCount.begin(); mapIt != m_mapCount.end(); ++mapIt)
	{
		cLoop++;
		numNodesVisited++;
		HVO hvoKey = mapIt->first;			// gets key value from hashmap
		StItemInfo *curInfo = mapIt->second;	// get the info for this key
		int cRefs = curInfo->cItems;
		HVO hvoParent = curInfo->hvoParent;

		while (hvoParent > 0 && hvoParent != hvoKey)
		{
			numNodesVisited++;
			// got the parent
			std::map<HVO, StItemInfo*>::iterator foundIt = m_mapCount.find(hvoParent);
			if (foundIt != m_mapCount.end())
			{
				foundIt->second->cChildren += cRefs;
				hvoParent = foundIt->second->hvoParent;
			}
			else
			{
				// look in the non-list nodes for the parent
				foundIt = mapAddedParents.find(hvoParent);
				if (foundIt != mapAddedParents.end())
				{
					foundIt->second->cChildren += cRefs;
					hvoParent = foundIt->second->hvoParent;
				}
				else
				{
					HVO hvoCurrent = hvoParent;
					hvoParent = qpli->GetOwnerIdFromId(hvoCurrent);
					StItemInfo *stTemp = new StItemInfo;
					stTemp->cItems = 0;
					stTemp->cChildren = cRefs;
					stTemp->hvoParent = hvoParent;

					mapAddedParents[hvoCurrent] = stTemp;
				}
			}
		}
	}

	// add the 'not in list elements' to the others
	StItemInfo *curInfo;
	for (mapIt = mapAddedParents.begin(); mapIt != mapAddedParents.end(); ++mapIt)
	{
		HVO item = mapIt->first;
		curInfo = mapIt->second;	// get the info for this key
		m_mapCount[item] = curInfo;
	}
	mapAddedParents.clear();
}

/*----------------------------------------------------------------------------------------------
	Clear the map of all items in the list.
----------------------------------------------------------------------------------------------*/
void TlsStatsDlg::ClearMap()
{
	std::map<HVO, StItemInfo*>::iterator mapIt;
	// release the structures
	for (mapIt = m_mapCount.begin(); mapIt != m_mapCount.end(); ++mapIt)
	{
		StItemInfo * curInfo = mapIt->second;	// get the info for this key
		delete curInfo;
	}
	m_mapCount.clear();
}

/*----------------------------------------------------------------------------------------------
	Retrieve the writing system from the db for this Possibility List.

	@param hvopssl HVO of the Possibility List that you want the writing systm for.
	@return Writing system for this list.
----------------------------------------------------------------------------------------------*/
int TlsStatsDlg::GetWs(HVO hvoPssl)
{
	Assert(hvoPssl);

	AssertPtr(m_plpi);
	AfDbInfo * pdbi = m_plpi->GetDbInfo();
	AssertPtr(pdbi);
	IOleDbEncapPtr qode;
	pdbi->GetDbAccess(&qode);
	IOleDbCommandPtr qodc;
	CheckHr(qode->CreateCommand(&qodc));
	StrUni stuCmd;
	stuCmd.Format(L"select WsSelector from CmPossibilityList where id = %d",
		hvoPssl);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	ComBool fMoreRows;
	CheckHr(qodc->NextRow(&fMoreRows));
	int ws = 0;
	ULONG cbSpaceTaken;
	ComBool fIsNull = true;
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&ws), sizeof(ws),
			&cbSpaceTaken, &fIsNull, 0));
	}
	return ws;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog.
----------------------------------------------------------------------------------------------*/
void TlsStatsDlg::GetDialogValues(HVO * hvopsslSel)
{
	AssertPtr(hvopsslSel);
	*hvopsslSel = m_hvopsslSel;
	return;
}

/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)
----------------------------------------------------------------------------------------------*/
bool TlsStatsDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
#ifdef DEBUG_TLSSTATSDLG
	StrAnsi staTemp = "TlsStatsDlg::OnInitDlg:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	m_plpi = plpi;

	AfDbInfo * pdbi = m_plpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetDbAccess(&m_qode);
	m_wsUser = pdbi->UserWs();

	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	LoadListCombo();

	// Make a trivial data access object.
	m_qvcd.CreateInstance(CLSID_VwCacheDa);
	Assert(m_qvcd);

	// Find the dimensions of the controls.
	Rect rcClient;  // size of the dialog
	GetClientRect(rcClient);
	Rect rcWindow(rcClient);
	::MapWindowPoints(m_hwnd, NULL, (POINT *)&rcWindow, 2);

	Rect rcDialog;  // bigger than rcWindow
	::GetWindowRect(m_hwnd, &rcDialog);
	m_yMin = rcDialog.bottom - rcDialog.top;
	m_xMin = rcDialog.right - rcDialog.left;

	Rect rcHelp;
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidHelp), &rcHelp);
	m_yButtons = rcWindow.bottom - rcHelp.top;

	Rect rcGroupBox;
	::GetWindowRect(::GetDlgItem(m_hwnd, kcidTlsStatGB), &rcGroupBox);
	m_dxGroupBox = (rcGroupBox.left - rcWindow.left) + (rcWindow.right - rcGroupBox.right);
	m_dyGroupBox = (rcWindow.bottom - rcGroupBox.bottom) + (rcGroupBox.top - rcWindow.top);

	Rect rcStatList;
	::GetWindowRect(::GetDlgItem(m_hwnd, kcidTlsStatList), &rcStatList);
	m_dxStatList = (rcStatList.left - rcWindow.left) + (rcWindow.right - rcStatList.right);
	m_dyStatList = (rcWindow.bottom - rcStatList.bottom) + (rcStatList.top - rcWindow.top);

	m_qtsl.Attach(NewObj TlsStatsList);
	m_qtsl->InitValues(0, 0, this);

	m_qtsl->Create(m_hwnd, kcidTlsStatList, m_qvcd, m_wsUser);

	HWND hwnd;
	m_qtsl->SetShowAbbr(true);
	hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatAbbr);
	::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);

	m_qtsl->SetShowZero(true);
	hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatZero);
	::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);

	m_qtsl->SetIncludeSubitems(false);
	hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatSub);
	::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);

	m_qtsl->SetStatSortAsc(true);
	hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatAsc);
	::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
	hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatDesc);
	::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);

	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatAbbr), false);
	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatZero), false);
	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatSub), false);

	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatOrderBy), false);
	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatAsc), false);
	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatDesc), false);

	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatShow), false);
	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatCopy), false);
	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatPrint), false);

	// Cause resize gripper to be displayed.
	GetClientRect(rcClient);
	OnSize(kwstRestored, rcClient.Width(), rcClient.Height());

	::SetFocus(::GetDlgItem(m_hwnd, kcidTlsStatListCb));
	return false; // causes focus to not go to the default button
}

/*----------------------------------------------------------------------------------------------
	Loads the List combo box.
----------------------------------------------------------------------------------------------*/
bool TlsStatsDlg::LoadListCombo()
{
	HWND hwndListCb = ::GetDlgItem(m_hwnd, kcidTlsStatListCb);
	StrApp str;
	int i;
	StrUni stuIds;
	int cv = m_vfactId.Size();
	Assert(cv);

	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	IOleDbCommandPtr qodc;

	// Load the name of a possibility list from the database and put in combo Box.
	const int kcchBuffer = MAX_PATH;
	OLECHAR rgchName[kcchBuffer];
	HVO hvo;
	StrUni stu;
	::SendMessage(hwndListCb, WM_SETREDRAW, false, 0);
	int iCur;
	for (i = 0; i < cv; ++i)
	{
		hvo = m_vfactId[i];
		if (hvo == 0)
			continue;
		stu.Format(L"exec GetOrderedMultiTxt '%d', %d", hvo, kflidCmMajorObject_Name);
		CheckHr(m_qode->CreateCommand(&qodc));

		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		Assert(fMoreRows); // This proc should always return something.
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchName),
			kcchBuffer * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
		str = rgchName;

		iCur = ::SendMessage(hwndListCb, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		::SendMessage(hwndListCb, CB_SETITEMDATA, (WPARAM)iCur, (LPARAM)hvo);

		m_vfDisplaySettings2.Insert(iCur, m_vfDisplaySettings[i]);
	}
	::SendMessage(hwndListCb, WM_SETREDRAW, true, 0);
	::InvalidateRect(hwndListCb, NULL, true);

	stu = L" Choose list...";	// ensure first in sorted list
	m_fDummyItemDeleted = false;
	int iSelListPos = ::SendMessage(hwndListCb, CB_ADDSTRING, 0, (LPARAM)stu.Chars());
	Assert(iSelListPos == 0);
	::SendMessage(hwndListCb, CB_SETCURSEL, iSelListPos, 0);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Loads the "Order By" combo box.
----------------------------------------------------------------------------------------------*/
bool TlsStatsDlg::LoadOrderByCombo()
{
	HWND hwndCb = ::GetDlgItem(m_hwnd, kcidTlsStatOrderBy);
	::SendMessage(hwndCb, WM_SETREDRAW, false, 0);
	int iCur;
	StrApp strDefault(kstidTlsStatDefault);
	StrApp strAbbr(kstidTlsStatHeaderAbbr);
	StrApp strName(kstidTlsStatHeaderName);
	StrApp strCount(kstidTlsStatHeaderCount);
	::SendMessage(hwndCb, CB_RESETCONTENT, 0, 0);
	if (m_qtsl->GetShowAbbr())
	{
		iCur = ::SendMessage(hwndCb, CB_INSERTSTRING, 0, (LPARAM)strAbbr.Chars());
		::SendMessage(hwndCb, CB_SETITEMDATA, (WPARAM)iCur, (LPARAM)ksobAbbr);
	}

	iCur = ::SendMessage(hwndCb, CB_ADDSTRING, 0, (LPARAM)strName.Chars());
	::SendMessage(hwndCb, CB_SETITEMDATA, (WPARAM)iCur, (LPARAM)ksobName);
	iCur = ::SendMessage(hwndCb, CB_ADDSTRING, 0, (LPARAM)strCount.Chars());
	::SendMessage(hwndCb, CB_SETITEMDATA, (WPARAM)iCur, (LPARAM)ksobCount);

	HWND hwndListCb = ::GetDlgItem(m_hwnd, kcidTlsStatListCb);
	int iSelListPos = ::SendMessage(hwndListCb, CB_GETCURSEL, 0, 0);
	if (m_vfDisplaySettings2[iSelListPos])
	{
		iCur = ::SendMessage(hwndCb, CB_ADDSTRING, 0, (LPARAM)strDefault.Chars());
		::SendMessage(hwndCb, CB_SETITEMDATA, (WPARAM)iCur, (LPARAM)ksobDefault);
		::SendMessage(hwndCb, WM_SETREDRAW, true, 0);
		::InvalidateRect(hwndCb, NULL, true);
		if (m_qtsl->GetShowAbbr())
			::SendMessage(hwndCb, CB_SETCURSEL, 3, 0);
		else
			::SendMessage(hwndCb, CB_SETCURSEL, 2, 0);
		m_sobStatOrderBy = ksobDefault;
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatAsc), false);
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatDesc), false);
	}
	else
	{
		::SendMessage(hwndCb, WM_SETREDRAW, true, 0);
		::InvalidateRect(hwndCb, NULL, true);
		if (m_qtsl->GetShowAbbr())
			::SendMessage(hwndCb, CB_SETCURSEL, 1, 0);	// Pre-select 1st item if Abbr is 0th.
		else
			::SendMessage(hwndCb, CB_SETCURSEL, 0, 0);	// Pre-select 0th item if Name is 0th.
		m_sobStatOrderBy = ksobName;
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatAsc), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatDesc), true);
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool TlsStatsDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	int iSelListPos;
	HWND hwnd;

	switch(pnmh->code)
	{
	case CBN_SELCHANGE: // Combo box item changed.
		switch (pnmh->idFrom)
		{
		case kcidTlsStatListCb:
			{
				hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatListCb);
				iSelListPos = ::SendMessage(hwnd, CB_GETCURSEL, 0, 0);
				HVO hvoPssl = ::SendMessage(hwnd, CB_GETITEMDATA, (WPARAM)iSelListPos, 0);
				if (hvoPssl == 0)
					return true;
				m_hvopsslSel = hvoPssl;

				if (! m_fDummyItemDeleted)
				{
					if (iSelListPos <= 0)
						return true;

					// user has selected something other than the dummy item
					// ok to delete the dummy item
					HWND hwndListCb = ::GetDlgItem(m_hwnd, kcidTlsStatListCb);
					::SendMessage(hwndListCb, CB_DELETESTRING, 0, 0);
					m_fDummyItemDeleted = true;

					iSelListPos--; // Because the zero item is now deleted

					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatAbbr), true);
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatZero), true);
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatSub), true);

					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatOrderBy), true);
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatAsc), true);
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatDesc), true);

					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatShow), true);
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatCopy), true);
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatPrint), true);
				}
				AssertPtr(m_plpi);
				PossListInfoPtr qpli;

				int ws = GetWs(m_hvopsslSel);
				m_plpi->LoadPossList(m_hvopsslSel, ws, &qpli);
				Assert(qpli);
				int depth = qpli->GetDepth();
				if (depth <= 1)
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatSub), false);
				else
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatSub), true);

				// Set Display Options to defaults based on type of list
				if (m_vfDisplaySettings2[iSelListPos])
				{
					m_qtsl->SetShowAbbr(true);
					hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatAbbr);
					::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);

					m_qtsl->SetShowZero(true);
					hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatZero);
					::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);

					m_qtsl->SetIncludeSubitems(true);
					hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatSub);
					::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
				}
				else
				{
					m_qtsl->SetShowAbbr(false);
					hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatAbbr);
					::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);

					m_qtsl->SetShowZero(false);
					hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatZero);
					::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);

					if (depth <= 1)
					{
						m_qtsl->SetIncludeSubitems(false);
						hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatSub);
						::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);
					}
					else
					{
						m_qtsl->SetIncludeSubitems(true);
						hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatSub);
						::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
					}
				}
				m_qtsl->SetStatSortAsc(true);
				hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatAsc);
				::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
				hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatDesc);
				::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);
				LoadOrderByCombo();
				ClearMap();
				GetCounts();
				m_qtsl->Redraw();
				return true;
			}
		case kcidTlsStatOrderBy:
			{
				hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatOrderBy);
				int iOrderByPos = ::SendMessage(hwnd, CB_GETCURSEL, 0, 0);
				m_sobStatOrderBy = (StatsOrderBy) ::SendMessage(hwnd, CB_GETITEMDATA, iOrderByPos, 0);
				if(m_sobStatOrderBy == ksobDefault)
				{
					HWND hwndAsc = ::GetDlgItem(m_hwnd, kcidTlsStatAsc);
					HWND hwndDec = ::GetDlgItem(m_hwnd, kcidTlsStatDesc);
					::EnableWindow(hwndAsc, false);
					::EnableWindow(hwndDec, false);
					::SendMessage(hwndAsc, BM_SETCHECK, BST_CHECKED, 0);
					::SendMessage(hwndDec, BM_SETCHECK, BST_UNCHECKED, 0);
				}
				else
				{
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatAsc), true);
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatDesc), true);
				}
				return true;
			}
		}
		break;
	case CBN_CLOSEUP: // Combo box text changed.
		switch (pnmh->idFrom)
		{
		case kcidTlsStatListCb:
			{
				hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatListCb);
				iSelListPos = ::SendMessage(hwnd, CB_GETCURSEL, 0, 0);
				HVO hvoPssl = ::SendMessage(hwnd, CB_GETITEMDATA, (WPARAM)iSelListPos, 0);
				if (hvoPssl == 0)
					return true;
				m_hvopsslSel = hvoPssl;

				if (! m_fDummyItemDeleted)
				{
					if (iSelListPos <= 0)
						return true;

					// user has selected something other than the dummy item
					// ok to delete the dummy item
					HWND hwndListCb = ::GetDlgItem(m_hwnd, kcidTlsStatListCb);
					::SendMessage(hwndListCb, CB_DELETESTRING, 0, 0);
					m_fDummyItemDeleted = true;

					iSelListPos--; // Because the zero item is now deleted

					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatAbbr), true);
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatZero), true);
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatSub), true);

					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatOrderBy), true);
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatAsc), true);
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatDesc), true);

					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatShow), true);
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatCopy), true);
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatPrint), true);
				}
				AssertPtr(m_plpi);
				PossListInfoPtr qpli;

				int ws = GetWs(m_hvopsslSel);
				m_plpi->LoadPossList(m_hvopsslSel, ws, &qpli);
				Assert(qpli);
				int depth = qpli->GetDepth();
				if (depth <= 1)
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatSub), false);
				else
					::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsStatSub), true);

				// Set Display Options to defaults based on type of list
				if (m_vfDisplaySettings2[iSelListPos])
				{
					m_qtsl->SetShowAbbr(true);
					hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatAbbr);
					::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);

					m_qtsl->SetShowZero(true);
					hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatZero);
					::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);

					m_qtsl->SetIncludeSubitems(true);
					hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatSub);
					::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
				}
				else
				{
					m_qtsl->SetShowAbbr(false);
					hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatAbbr);
					::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);

					m_qtsl->SetShowZero(false);
					hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatZero);
					::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);

					m_qtsl->SetIncludeSubitems(false);
					hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatSub);
					::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);
				}
				m_qtsl->SetStatSortAsc(true);
				hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatAsc);
				::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
				hwnd = ::GetDlgItem(m_hwnd, kcidTlsStatDesc);
				::SendMessage(hwnd, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);
				LoadOrderByCombo();
				ClearMap();
				GetCounts();
				m_qtsl->Redraw();
				return true;
			}
		}
		break;
	case CBN_DROPDOWN: // Combo box item changed.
		break;
	case BN_CLICKED:
		switch (pnmh->idFrom)
		{
		case kcidTlsStatAbbr:
			{
				if (m_fDummyItemDeleted)
				{
					m_qtsl->SetShowAbbr(! m_qtsl->GetShowAbbr());
					LoadOrderByCombo();
					return true;
				}
			}
			break;
		case kcidTlsStatShow:
			{
				if (m_fDummyItemDeleted)
				{
					////LoadOrderByCombo();
					ClearMap();
					GetCounts();
					m_qtsl->Redraw();
					return true;
				}
			}
			break;
		case kcidTlsStatCopy:
			{
				// Copy the displayed list "table" to the clipboard.
				IVwSelectionPtr qselStart;
				IVwSelectionPtr qselEnd;
				IVwSelectionPtr qselAll;
				IVwRootBoxPtr qrootb;
				CheckHr(m_qtsl->get_RootBox(&qrootb));
				// start, not editable, not range, don't install.
				CheckHr(qrootb->MakeSimpleSel(true, false, false, false, &qselStart));
				// end, not editable, not range, don't install.
				CheckHr(qrootb->MakeSimpleSel(false, false, false, false, &qselEnd));
				CheckHr(qrootb->MakeRangeSelection(qselStart, qselEnd, false, &qselAll));
				ITsStringPtr qtssText;
				StrUni stuSep(" ");
				CheckHr(qselAll->GetSelectionString(&qtssText, stuSep.Bstr()));
				const OLECHAR * pchText;
				int cch;
				CheckHr(qtssText->LockText(&pchText, &cch));
				IDataObjectPtr qdobj;
				StringDataObject::Create(const_cast<OLECHAR *>(pchText), &qdobj);
				CheckHr(qtssText->UnlockText(pchText));
				if (::OleSetClipboard(qdobj) == S_OK)
				{
					ModuleEntry::SetClipboard(qdobj);
				}
			}
			break;
		case kcidTlsStatPrint:
			{
				Cmd cmd;
				m_qtsl->CmdFilePrint1(&cmd);
			}
			break;
		case kcidTlsStatClose:
			{
				::PostMessage(m_hwnd, WM_CLOSE, 0, 0);
			}
			break;
		}
	}
	return AfWnd::OnNotifyChild(ctid, pnmh, lnRet);
}
/*----------------------------------------------------------------------------------------------
	Process draw messages.
----------------------------------------------------------------------------------------------*/
bool TlsStatsDlg::OnDrawChildItem(DRAWITEMSTRUCT * pdis)
{
	if (pdis->CtlID == kcidTlsStatList)
	{
		UpdatePreview(pdis);
		return true;
	}
	return SuperClass::OnDrawChildItem(pdis);
}

/*----------------------------------------------------------------------------------------------
	Takes care of painting the button behind the preview window. This window exists only to
	create the border and allow the preview position to be specified by the resource editor.
----------------------------------------------------------------------------------------------*/
void TlsStatsDlg::UpdatePreview(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);
	HDC hdc = pdis->hDC;
	DrawEdge(hdc, &pdis->rcItem, EDGE_SUNKEN, BF_RECT);
}

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
TlsStatsList::TlsStatsList(void)
{
	m_fColumnsModified = false;
	m_qvcd = NULL;
}


/*----------------------------------------------------------------------------------------------
	Create the view window.

	@param hwndPar Handle to the parent window.
	@param wid Child window identifier to use for the view window.
	@param pvcd Pointer to the data cache containing the filter information.
----------------------------------------------------------------------------------------------*/
void TlsStatsList::Create(HWND hwndPar, int wid, IVwCacheDa * pvcd, int wsUser)
{
	AssertPtr(pvcd);

	m_qvcd = pvcd;

	ITsStrFactoryPtr qtsf;
	ITsStringPtr qtss;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(kstidTagTextDemo);
	CheckHr(qtsf->MakeString(stu.Bstr(), wsUser, &qtss));

	WndCreateStruct wcs;
	wcs.InitChild(_T("AfVwWnd"), hwndPar, wid);
	wcs.style |= WS_VISIBLE | WS_TABSTOP;
	wcs.dwExStyle = WS_EX_CLIENTEDGE;
	// Since the preview is placed inside the owner draw button, it must not clip siblings or
	// it won't show up at all.
	wcs.style &= ~WS_CLIPSIBLINGS;
	Rect rcBounds;
	HWND hwndTemp = ::GetDlgItem(hwndPar, wid);
	::GetWindowRect(hwndTemp, &rcBounds);
	::DestroyWindow(hwndTemp);

	// Get rectangle for child window, in pixels relative to parent
	Rect rcMyBounds;
	::GetWindowRect(hwndPar, &rcMyBounds);
	rcBounds.Offset(-rcMyBounds.left, -rcMyBounds.top);
	// Reduce the size of the view to exclude the border and make it fit.
	SIZE sizeMargins = { 0, ::GetSystemMetrics(SM_CYCAPTION) };
	rcBounds.top -= sizeMargins.cy;
	rcBounds.bottom -= sizeMargins.cy;
	rcBounds.right -= 2 * ::GetSystemMetrics(SM_CXEDGE) + 1;
	rcBounds.bottom -= 2 * ::GetSystemMetrics(SM_CYEDGE) + 1;
	wcs.SetRect(rcBounds);
	CreateHwnd(wcs);
}

/*----------------------------------------------------------------------------------------------
	Set the flag for showing Abbr.
----------------------------------------------------------------------------------------------*/
void TlsStatsList::SetShowAbbr(bool f)
{
	m_fShowAbbr = f;
}
/*----------------------------------------------------------------------------------------------
	Get the flag for showing Abbr.
----------------------------------------------------------------------------------------------*/
bool TlsStatsList::GetShowAbbr()
{
	return m_fShowAbbr;
}

/*----------------------------------------------------------------------------------------------
	Set the flag for showing items with zero count
----------------------------------------------------------------------------------------------*/
void TlsStatsList::SetShowZero(bool f)
{
	m_fDoNotShowZero = !f;
}
/*----------------------------------------------------------------------------------------------
	Get the flag for showing items with zero count
----------------------------------------------------------------------------------------------*/
bool TlsStatsList::GetShowZero()
{
	return ! m_fDoNotShowZero;
}

/*----------------------------------------------------------------------------------------------
	Set the flag for including list subitems in count
----------------------------------------------------------------------------------------------*/
void TlsStatsList::SetIncludeSubitems(bool f)
{
	m_fIncludeSubitems = f;
}
/*----------------------------------------------------------------------------------------------
	Get the flag for including list subitems in count
----------------------------------------------------------------------------------------------*/
bool TlsStatsList::GetIncludeSubitems()
{
	return m_fIncludeSubitems;
}


/*----------------------------------------------------------------------------------------------
	Set the flag for sort direction
----------------------------------------------------------------------------------------------*/
void TlsStatsList::SetStatSortAsc(bool f)
{
	m_fStatSortAsc = f;
}
/*----------------------------------------------------------------------------------------------
	Get the flag for showing Abbr.
----------------------------------------------------------------------------------------------*/
bool TlsStatsList::GetStatSortAsc()
{
	return m_fStatSortAsc;
}


/*----------------------------------------------------------------------------------------------
	Redraw the window
----------------------------------------------------------------------------------------------*/
void TlsStatsList::Redraw()
{
	m_qrootb->Reconstruct();
}


/*----------------------------------------------------------------------------------------------
	Make the root box.
----------------------------------------------------------------------------------------------*/
void TlsStatsList::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
	IVwRootBox ** pprootb)
{
	AssertPtrN(pwsf);
	*pprootb = NULL;

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this));

	// Make an arbitrary ID for a dummy root object
	HVO hvo = 1;
	int frag = 0;

	// Set up a new view constructor.
	m_qtslvc.Attach(NewObj TlsStatsListVc());
	m_qtslvc->InitValues(m_pttpFirst, m_pttpOther, m_ptsd);

	ISilDataAccessPtr qsdaTemp;
	HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);
	if (pwsf)
		CheckHr(qsdaTemp->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qsdaTemp));

	IVwViewConstructor * pvvc = m_qtslvc;
	CheckHr(qrootb->SetRootObjects(&hvo, &pvvc, &frag, NULL, 1));
	*pprootb = qrootb;
	(*pprootb)->AddRef();
}

int TlsStatsListVc::ItemsFor (PossItemInfo * ppii, TlsStatsDlg * ptsd)
{
	std::map<HVO, TlsStatsDlg::StItemInfo*>::iterator mapIt;
	mapIt = ptsd->m_mapCount.find(ppii->GetPssId());

	// is item in hash map?
	if (mapIt == ptsd->m_mapCount.end())
	{
		// not found
		return 0;
	}
	else
	{
		// found
		TlsStatsDlg::StItemInfo * curInfo = mapIt->second;	// get the info for this key
		if (!ptsd->m_qtsl->GetIncludeSubitems())
			return curInfo->cItems;
		else
			return curInfo->cItems + curInfo->cChildren;
	}
	return 0;
}

class PossItemComparer
{
public:

	StatsOrderBy m_sob; // how we want to sort
	bool m_fAscending;
	TlsStatsDlg * m_ptsd;
	Collator * m_pcoll;

	PossItemComparer(StatsOrderBy sob, bool fAscending, TlsStatsDlg * ptsd, Collator * pcoll)
	{
		m_sob=sob;
		m_fAscending = fAscending;
		m_ptsd = ptsd;
		m_pcoll = pcoll;
	}

	const bool compareStrings(const OLECHAR * pchLeft, const OLECHAR * pchRight)
	{
		// Ignore the error code...just use the best result it can give.
		UErrorCode status = U_ZERO_ERROR;
		return m_pcoll->compare(pchLeft, wcslen(pchLeft), pchRight, wcslen(pchRight), status)
			== UCOL_LESS;
	}

	bool operator()( const PossItemInfo * ppiiLhs, const PossItemInfo * ppiiRhs ) const
	{
	   //return( lhs < rhs? true : false );
		PossItemInfo * ppiiFirst = const_cast<PossItemInfo *>(ppiiRhs);
		PossItemInfo * ppiiSecond = const_cast<PossItemInfo *>(ppiiLhs);
		if (m_fAscending)
		{
			ppiiFirst = const_cast<PossItemInfo *>(ppiiLhs);
			ppiiSecond = const_cast<PossItemInfo *>(ppiiRhs);
		}
		StrUni stuLeft, stuRight;
		switch(m_sob)
		{
		case ksobAbbr:
			ppiiFirst->GetName(stuLeft, kpntAbbreviation);
			ppiiSecond->GetName(stuRight, kpntAbbreviation);
			return const_cast<PossItemComparer *>(this)->compareStrings(stuLeft.Chars(), stuRight.Chars());
			break;
		case ksobName:
			ppiiFirst->GetName(stuLeft, kpntName);
			ppiiSecond->GetName(stuRight, kpntName);
			return const_cast<PossItemComparer *>(this)->compareStrings(stuLeft.Chars(), stuRight.Chars());
			break;
		case ksobCount:
			return TlsStatsListVc::ItemsFor(ppiiFirst, m_ptsd) < TlsStatsListVc::ItemsFor(ppiiSecond, m_ptsd);
			break;
		default:
			Assert(false);
		}
		return false; // unreachable, but makes compiler happy.
	}
};

static DummyFactory g_fact(_T("SIL.AppCore.TlsStatsListVc"));

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them. Construct
	the complete contents of the preview.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TlsStatsListVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwenv);

	// Constant fragments
	if ((frag == 0) && (m_ptsd->m_hvopsslSel))
	{
		int dxpTable = 21;
		Rect rc;
		int ccols = 2;
		if (m_ptsd->m_qtsl->GetShowAbbr())
			ccols++;
		// TODO RecordSpecPtr ? m_qrsp ?
		for (int icol = 0; icol < ccols; icol++)
			dxpTable += 100; //m_qrsp->m_vqbsp[icol]->m_dxpColumn;

		VwLength vlTab = { 10000, kunPercent100 };

		CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));

		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		ITsStringPtr qtss;

		// Database name - above the table
		SmartBstr sbstrDatabase;
		CheckHr(m_ptsd->m_qode->get_Database(&sbstrDatabase));
		StrUni stuDatabase(sbstrDatabase.Chars());

		CheckHr(pvwenv->put_IntProperty(ktptAlign, ktpvEnum, ktalCenter));
		CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
		CheckHr(pvwenv->OpenParagraph());
		CheckHr(qtsf->MakeStringRgch(sbstrDatabase.Chars(), sbstrDatabase.Length(),
			m_ptsd->m_plpi->AnalWs(), &qtss));
		CheckHr(pvwenv->AddString(qtss));
		CheckHr(pvwenv->CloseParagraph());

		// We add one extra column for the left fixed column
		CheckHr(pvwenv->OpenTable(ccols,
			vlTab,
			72000/96, // border thickness about a pixel
			kvaLeft, // default alignment
			kvfpVoid, // border below table only
			kvrlNone, // no visible separator lines
			0, // no forced space between cells
			72000 * 2 / 96, // 2 pixels padding inside cells
			false));
		// Specify column widths. The first argument is #cols, not col index.
		// The tag column only occurs at all if its width is non-zero.

		if (m_ptsd->m_qtsl->GetShowAbbr())
		{
			// Abbreviations
			VwLength vl = {1, kunRelative};
			CheckHr(pvwenv->MakeColumns(1, vl));
		}

		VwLength v2 = {4, kunRelative};
		CheckHr(pvwenv->MakeColumns(1, v2));

		VwLength v3 = {30000, kunPoint1000};
		CheckHr(pvwenv->MakeColumns(1, v3));

		CheckHr(pvwenv->OpenTableBody());

		// column headings
		CheckHr(pvwenv->OpenTableRow());
		if (m_ptsd->m_qtsl->GetShowAbbr())
		{
			CheckHr(pvwenv->OpenTableCell(1,1));
			CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
			CheckHr(pvwenv->OpenParagraph());
			AfUtil::GetResourceTss(kstidTlsStatHeaderAbbr, m_ptsd->m_wsUser, &qtss);
			CheckHr(pvwenv->AddString(qtss));
			CheckHr(pvwenv->CloseParagraph());
			CheckHr(pvwenv->CloseTableCell());
		}

		CheckHr(pvwenv->OpenTableCell(1,1));
		CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
		CheckHr(pvwenv->OpenParagraph());
		AfUtil::GetResourceTss(kstidTlsStatHeaderName, m_ptsd->m_wsUser, &qtss);
		CheckHr(pvwenv->AddString(qtss));
		CheckHr(pvwenv->CloseParagraph());
		CheckHr(pvwenv->CloseTableCell());

		CheckHr(pvwenv->OpenTableCell(1,1));
		CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
		CheckHr(pvwenv->OpenParagraph());
		AfUtil::GetResourceTss(kstidTlsStatHeaderCount, m_ptsd->m_wsUser, &qtss);
		CheckHr(pvwenv->AddString(qtss));
		CheckHr(pvwenv->CloseParagraph());
		CheckHr(pvwenv->CloseTableCell());
		CheckHr(pvwenv->CloseTableRow());

		PossListInfoPtr qpli;
		bool fRet;
		fRet = m_ptsd->m_plpi->LoadPossList(m_ptsd->m_hvopsslSel, m_ptsd->m_wsMagic, &qpli);
		Assert(qpli);

		Vector<PossItemInfo *> vppii;

		int ipss;
		PossItemInfo * ppii;
		for (ipss=0; ipss< qpli->GetCount(); ipss++)
		{
			ppii = qpli->GetPssFromIndex(ipss);
			AssertPtr(ppii);
			int cCount = TlsStatsListVc::ItemsFor(ppii, m_ptsd);
			if (cCount == 0 && !m_ptsd->m_qtsl->GetShowZero())
				continue;
			vppii.Push(ppii);
		}

		if(m_ptsd->m_sobStatOrderBy != ksobDefault)
		{
			Collator * pcoll = NULL;
			try
			{
				// Sort the vector.

				if (m_ptsd->m_sobStatOrderBy != ksobCount)
				{
					// First figure the Collator we will use to compare strings.
					UErrorCode status = U_ZERO_ERROR;
					StrUtil::InitIcuDataDir();
					Locale loc = m_ptsd->m_plpi->GetLocale(m_ptsd->m_plpi->ActualWs(qpli->GetWs()));
					pcoll = Collator::createInstance(loc, status);
					if (pcoll == NULL || !U_SUCCESS(status))
						ThrowHr(E_FAIL);
				}

				PossItemComparer pic(m_ptsd->m_sobStatOrderBy, m_ptsd->m_qtsl->GetStatSortAsc(), m_ptsd,
					pcoll);
				std::stable_sort(vppii.Begin(), vppii.Begin() + vppii.Size(), pic);
			}
			catch(...)
			{
				if (pcoll)
				{
					delete pcoll;
					pcoll = NULL;
					throw;
				}
			}
			if (pcoll)
			{
				delete pcoll;
				pcoll = NULL;
			}

		}

		// Now insert the vector items into the table.
		for (ipss=0; ipss< vppii.Size(); ipss++)
		{
			ppii = vppii[ipss];

			CheckHr(pvwenv->OpenTableRow());

			// Display Abbr.
			if (m_ptsd->m_qtsl->GetShowAbbr())
			{
				StrUni stuAbbr;
				ppii->GetName(stuAbbr, kpntAbbreviation);
				CheckHr(pvwenv->OpenTableCell(1,1));
				CheckHr(pvwenv->OpenParagraph());
				CheckHr(qtsf->MakeStringRgch(stuAbbr.Chars(), stuAbbr.Length(),
					ppii->GetWs(), &qtss));
				CheckHr(pvwenv->AddString(qtss));
				CheckHr(pvwenv->CloseParagraph());
				CheckHr(pvwenv->CloseTableCell());
			}

			// Display Name
			StrUni stuName;
			ppii->GetName(stuName, kpntName);
			CheckHr(pvwenv->OpenTableCell(1,1));
			CheckHr(pvwenv->OpenParagraph());
			CheckHr(qtsf->MakeStringRgch(stuName.Chars(), stuName.Length(),
				ppii->GetWs(), &qtss));
			CheckHr(pvwenv->AddString(qtss));
			CheckHr(pvwenv->CloseParagraph());
			CheckHr(pvwenv->CloseTableCell());

			// Display Count
			int cCount = TlsStatsListVc::ItemsFor(ppii, m_ptsd);
			StrUni stu;
			stu.Format(L"%d", cCount);
			CheckHr(pvwenv->OpenTableCell(1,1));
			CheckHr(pvwenv->OpenParagraph());
			CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(),
				ppii->GetWs(), &qtss));
			CheckHr(pvwenv->AddString(qtss));
			CheckHr(pvwenv->CloseParagraph());
			CheckHr(pvwenv->CloseTableCell());

			CheckHr(pvwenv->CloseTableRow());

			// Put the full version in the clipboard, with extra info.
			IDataObjectPtr qdobj;
			StringDataObject::Create(const_cast<OLECHAR *>(stu.Chars()), &qdobj);
			if (::OleSetClipboard(qdobj) == S_OK)
			{
				ModuleEntry::SetClipboard(qdobj);
			}
		}
		CheckHr(pvwenv->CloseTableBody());
		CheckHr(pvwenv->CloseTable());
	}

	return S_OK;
	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}

// Explicit instantiation

#include "vector_i.cpp"
template Vector<PossItemInfo *>;

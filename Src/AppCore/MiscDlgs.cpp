/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: MiscDlgs.cpp
Responsibility: Rand Burgett
Last reviewed:
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE
//:End Ignore

//:>********************************************************************************************
//:>	PossChsrMrgDlg methods. (Poss Chooser Merge dialog)
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
PossChsrMrg::PossChsrMrg(void)
{
	m_rid = kridChsrMergDlg;
	m_pszHelpUrl = _T("Beginning_Tasks/Referencing_Topics_Lists/Merge_list_items.htm");
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog variables, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param pszOldName Name of the Existing item
	@param pszOldAbbr Abbr of the Existing item
	@param pszNewName Name of the Renamed item
	@param pszNewAbbr Abbr of the Renamed item
----------------------------------------------------------------------------------------------*/
void PossChsrMrg::SetDialogValues(PossListInfo * ppli,HVO hvoSel)
{
	m_qpli = ppli;
	m_hvoSel = hvoSel;
}


/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool PossChsrMrg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case TVN_GETDISPINFO:
		return OnGetDispInfo((NMTVDISPINFO *)pnmh);
	case TVN_SELCHANGED:
		{
		int iSelItem = ((NMTREEVIEW *)pnmh)->itemNew.lParam;
		m_hvoDst = m_qpli->GetPssFromIndex(iSelItem)->GetPssId();
		break;
		}
	}
	return false;
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
bool PossChsrMrg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	StrUni stu;
	int ipss = m_qpli->GetIndexFromId(m_hvoSel);
	PossItemInfo * piiOrg = m_qpli->GetPssFromIndex(ipss);
	AssertPtr(piiOrg);
	piiOrg->GetName(stu, kpntNameAndAbbrev);

	HWND hwndLab = ::GetDlgItem(m_hwnd, kctidChsrMergLabel);
	StrApp str(kstidChsrMergText);
	StrApp strText;
	StrApp strMerg(stu.Chars());
	strText.Format(str.Chars(),strMerg.Chars());
	::SendMessage(hwndLab, WM_SETTEXT, (WPARAM)0, (LPARAM)strText.Chars());

	HWND hwndTree = ::GetDlgItem(m_hwnd, kctidMergeTree);

	// Add the possibilities to the tree view.
	WaitCursor wc;

	if (!m_qpli)
		return false;

	// Add each item to the tree.
	TVINSERTSTRUCT tvis = { TVI_ROOT, TVI_LAST };
	tvis.item.mask = TVIF_PARAM | TVIF_TEXT;
	tvis.item.pszText = LPSTR_TEXTCALLBACK;

	Vector<HTREEITEM> vhti;
	vhti.Resize(8);
	vhti[0] = TVI_ROOT;

	::SendMessage(hwndTree, WM_SETREDRAW, false, 0);

	int cpii = m_qpli->GetCount();
	if (cpii)
	{
		// Add the possibility items to the list.
		PossItemInfo * pii = m_qpli->GetPssFromIndex(0);
		int ilevel = 1;
		int ilevelNext = 1;
		for (int ipii = 0; ipii < cpii; ipii++)
		{
			AssertPtr(pii);

			// If the next item has a greater level, it is a child of this item, so set the
			// children flag.
			if (ipii < cpii - 1)
				ilevelNext = pii[1].GetHierLevel();
			if (ilevelNext > ilevel)
			{
				vhti.Resize(ilevelNext + 1);
				tvis.item.cChildren = 1;
			}
			else
			{
				tvis.item.cChildren = 0;
			}

			tvis.hParent = vhti[ilevel - 1];
			tvis.item.lParam = ipii;
			vhti[ilevel] = TreeView_InsertItem(hwndTree, &tvis);

			pii++;
			ilevel = ilevelNext;
		}
	}

	::SendMessage(hwndTree, WM_SETREDRAW, true, 0);

	// Select the first item in the tree.
	HTREEITEM htiSel = TreeView_GetRoot(hwndTree);
	TreeView_SelectItem(hwndTree, htiSel);
	TreeView_EnsureVisible(hwndTree, htiSel);
	m_hvoDst = m_qpli->GetPssFromIndex(0)->GetPssId();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Show the appropriate text for an item based on the view of item selection.
	This gets called every time an item needs to be drawn.
----------------------------------------------------------------------------------------------*/
bool PossChsrMrg::OnGetDispInfo(NMTVDISPINFO * pntdi)
{
	AssertPtr(pntdi);
	Assert(pntdi->item.mask == TVIF_TEXT);
	AssertObj(m_qpli);

	PossItemInfo * pii = m_qpli->GetPssFromIndex(pntdi->item.lParam);
	AssertPtr(pii);
	StrUni stu;
	pii->GetName(stu, kpntNameAndAbbrev);
	StrApp str(stu);
	lstrcpy(pntdi->item.pszText, str.Chars());
	return true;
}


/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the OK or the Apply Now button.
	When the framework calls this method, changes are made in the dialog are accepted if the
	return value is true.

	@param fClose not used here
	@return true if Successful
----------------------------------------------------------------------------------------------*/
bool PossChsrMrg::OnApply(bool fClose)
{
	if (m_qpli->IsFirstHvoAncestor(m_hvoSel, m_hvoDst))
	{
		const achar * pszHelpUrl;
		pszHelpUrl = m_pszHelpUrl;
		m_pszHelpUrl = _T("Beginning_Tasks/Referencing_Topics_Lists/Merge_results.htm");
		StrApp strT(kstidInvalidMergeT);
		StrApp strM(kstidInvalidMergeMsg);
		::MessageBox(m_hwnd, strM.Chars(), strT.Chars(), MB_OK | MB_HELP | MB_ICONINFORMATION);
		m_pszHelpUrl = pszHelpUrl;
		return false;
	}
	if(!AfApp::ConfirmUndoableAction())
		return false;

	return AfDialog::OnApply(fClose);
}


//:>********************************************************************************************
//:>	MssngDt methods. (Missing Data dialog)
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
MssngDt::MssngDt(void)
{
	m_rid = kridMssngDtDlg;
	m_pszHelpUrl = _T("Beginning_Tasks/Entering_Data/Missing_Required_Data.htm");
}


/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool MssngDt::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		if (ctidFrom == kctidMssngDtYes)
		{
			m_hvoButton = 0;
		}
		else if (ctidFrom == kctidMssngDtNo)
		{
			m_hvoButton = 1;
		}
		else
		{
			m_hvoButton = 2;
		}
	}
	::EndDialog(m_hwnd, kctidOk);

	return false;
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
bool MssngDt::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	::SetWindowText(m_hwnd, m_strTitle.Chars());
	::SendMessage(::GetDlgItem(m_hwnd, kstidEncouragedMsg), WM_SETTEXT, (WPARAM)0,
		(LPARAM)m_strText.Chars());
	// Default to "No"
	m_hvoButton = 1;
	return true;
}


//:>********************************************************************************************
//:>	DeleteDlg methods. (Delete dialog)
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
DeleteDlg::DeleteDlg(void)
{
	m_rid = kridDeleteDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/Edit/Delete.htm");
}

/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool DeleteDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	switch (pnmh->code)
	{
	case BN_CLICKED:
		if (ctidFrom == kctidDelDlgSelTxt)
		{
			m_nSel = 0;
		}

		if (ctidFrom == kctidDelDlgObj)
		{
			m_nSel = 1;
		}
	}
	return SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet);
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
bool DeleteDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{

	::SendMessage(::GetDlgItem(m_hwnd, kctidDelDlgObj), WM_SETTEXT, (WPARAM)0,
		(LPARAM)m_str.Chars());
	::CheckDlgButton(m_hwnd, kctidDelDlgSelTxt, BST_CHECKED);
	return true;
}



//:>********************************************************************************************
//:>	DeleteObjDlg methods. (Delete Object dialog)
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
DeleteObjDlg::DeleteObjDlg(void)
{
	m_rid = kridDeleteObjDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/Edit/Delete_an_Entry_or_Subentry.htm");
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog variables, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param ptss Title of the object to be deleted.
	@param stuObject name of the type of the object it is.
	@param stuSubObject what its subobjects are called.
	@param clid class id of the object
----------------------------------------------------------------------------------------------*/
void DeleteObjDlg::SetDialogValues(HVO hvoObj, ITsString * ptss, StrApp strObject, StrApp strSubObject,
		int clid, AfDbInfoPtr qdbi)
{
	m_hvoObj = hvoObj;
	m_qtss = ptss;
	m_strObject = strObject;
	m_strSubObject = strSubObject;
	m_clid = clid;
	m_qdbi = qdbi;
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
bool DeleteObjDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	StrApp str(kstidDelObjDel);
	str.Append(m_strObject);

	HICON hicon = ::LoadIcon(NULL, IDI_EXCLAMATION);
	::SendMessage(::GetDlgItem(m_hwnd, kctidDelAndChgStylesIcon), STM_SETICON, (WPARAM)hicon, 0);

	::SendMessage(m_hwnd, WM_SETTEXT, (WPARAM)0, (LPARAM)str.Chars());
	str.Load(kstidDelObjTxt);
	StrApp strFmt;

	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	m_qdbi->GetDbAccess(&qode);

	// Get all linked REFERENCES.
	StrUni stuSql;
	stuSql.Assign(
		L"create table [#ObjInfoTbl$]("
		L"[ObjId] int not null,"
		L"[ObjClass] int null,"
		L"[InheritDepth] int null default(0),"
		L"[OwnerDepth] int null default(0),"
		L"[RelObjId] int null,"
		L"[RelObjClass] int null,"
		L"[RelObjField] int null,"
		L"[RelOrder] int null,"
		L"[RelType] int null,"
		L"[OrdKey] varbinary(250) null default(0))");

	CheckHr(qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	int nRef = 0;
	stuSql.Format(L"SELECT COUNT(*) FROM dbo.fnGetRefsToObj(%d, NULL) fn", m_hvoObj);
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&nRef),
			isizeof(int), &cbSpaceTaken, &fIsNull, 0));
	}

	// Get all Owned Objects.
	stuSql.Format(L"GetLinkedObjs$ '%d', %d, 0, 1, 1, 0, %d", m_hvoObj, kgrfcptOwning,
		m_clid);
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	int nSubObj = 0;
	stuSql.Assign(L"select count (*) from [#ObjInfoTbl$] where RelObjClass is not NULL and [InheritDepth] > -1");
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&nSubObj),
			isizeof(int), &cbSpaceTaken, &fIsNull, 0));
	}

	stuSql.Format(L"drop table [#ObjInfoTbl$]");
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	strFmt.Format(str, nSubObj, m_strSubObject.Chars(), nRef, m_strObject.Chars());
	::SendMessage(::GetDlgItem(m_hwnd, kctidDelObjTxt), WM_SETTEXT, (WPARAM)0,
		(LPARAM)strFmt.Chars());

	// Set the string to Bold.
	ITsStrBldrPtr qtsb;
	CheckHr(m_qtss->GetBldr(&qtsb));
	int cch;
	CheckHr(qtsb->get_Length(&cch));
	CheckHr(qtsb->SetIntPropValues(0, cch, ktptBold, ktpvEnum, kttvForceOn));
	CheckHr(qtsb->GetString(&m_qtss));

	TssEditPtr qte;
	qte.Create();
	ILgWritingSystemFactoryPtr qwsf;
	m_qdbi->GetLgWritingSystemFactory(&qwsf);
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));
	qte->SubclassEdit(m_hwnd, kctidDelObjBox, qwsf, wsUser, 0);
	::SendMessage(qte->Hwnd(), EM_SETMARGINS, EC_RIGHTMARGIN | EC_LEFTMARGIN, MAKELPARAM(0, 0));
	SendMessage(qte->Hwnd(), FW_EM_SETTEXT, 0, (LPARAM)m_qtss.Ptr());
	SendMessage(qte->Hwnd(), EM_SETREADONLY , (WPARAM)true, 0);

	StrUni stu("");
	::SendMessage(qte->Hwnd(), FW_EM_SETSTYLE, (WPARAM)::GetSysColor(COLOR_3DFACE), (LPARAM)&stu);
	DWORD style = ::GetWindowLong(qte->Hwnd(),GWL_STYLE);
	style |= WS_DISABLED;
	::SetWindowLong(qte->Hwnd(),GWL_STYLE, style);

	::SetFocus(::GetDlgItem(m_hwnd, kctidCancel));
	return false;
}

//:>********************************************************************************************
//:>	ConfirmDeleteDlg methods. (Confirm Delete dialog)
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ConfirmDeleteDlg::ConfirmDeleteDlg(void)
{
	m_rid = kridConfirmDeleteDlg;
	m_pszHelpUrl = _T("DialogConfirmDelete.htm");
}

/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool ConfirmDeleteDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

//	return SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet);
//	SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet);
	return false;
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
bool ConfirmDeleteDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	StrApp strFmt;
	StrApp strText;
	HWND hwnd;

	::SetWindowText(m_hwnd, m_strTitle.Chars());

	HICON hicon = ::LoadIcon(NULL, IDI_WARNING);
	if (hicon)
	{
		hwnd = ::GetDlgItem(m_hwnd, kridConfirmDeleteIcon);
		::SendMessage(hwnd, STM_SETICON, (WPARAM)hicon, (LPARAM)0);
	}

	hwnd = ::GetDlgItem(m_hwnd, kcidConfirmDeleteMsg);
	strText = m_strPrompt;
	::SetWindowText(hwnd, strText.Chars());

	::SetFocus(::GetDlgItem(m_hwnd, kctidCancel));

///////////////	SuperClass::OnInitDlg(hwndCtrl, lp);
	return false;
}


bool ConfirmDeleteDlg::OnActivate(bool fActivating, LPARAM lp)
{
	AfDialog * pdlg;

	pdlg = dynamic_cast<AfDialog *>(AfWnd::GetAfWnd(::GetParent(m_hwnd)));
	if (pdlg)
		::SendMessage(pdlg->Hwnd(), DM_SETDEFID, ::GetDlgCtrlID(m_hwnd), 0);

	return false;
}

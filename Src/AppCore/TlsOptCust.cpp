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

//:>********************************************************************************************
//:>Custom Dialog Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
	@param ppsd pointer to the main TlsOptDlg.
----------------------------------------------------------------------------------------------*/
TlsOptDlgCst::TlsOptDlgCst(TlsOptDlg * ptod)
{
	AssertPtr(ptod);

	m_rid = kridTlsOptDlgCst;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Options/Options_Fields_tab.htm");
	m_ptod = ptod;
	m_wsUser = ptod->MainWnd()->UserWs();
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param vuvs user views to be used in the dialog
	@param psiwndClientDel delete list that will be updated to hold all deleted or
		modified views.
	@param psiCustFldDel custom fields that will be deleted from the Db whe OK is Pressed.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgCst::SetDialogValues(UserViewSpecVec & vuvs, Set<int> * psiwndClientDel,
	Set<int> * psiCustFldDel)
{
	AssertPtr(psiwndClientDel);
	m_psiwndClientDel = psiwndClientDel;
	m_psiCustFldDel = psiCustFldDel;
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
bool TlsOptDlgCst::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfoPtr qdbi = plpi->GetDbInfo();
	AssertPtr(qdbi);
	qdbi->GetDbAccess(&m_qode);

	StrAppBuf strb;

	m_hwndCstDfn = ::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDfn);
	m_hwndCstFld = ::GetDlgItem(m_hwnd, kcidTlsOptDlgCstFld);
	m_hwndCstDesc = ::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDes);
	m_hwndCstType = ::GetDlgItem(m_hwnd, kcidTlsOptDlgCstTyp);
	m_hwndCstLimit = ::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLimit);
	m_hwndCstLists = ::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLstcbo);
	m_hwndCstWS = ::GetDlgItem(m_hwnd, kcidTlsOptDlgCstWS);

	// Load "Define In" Combo Box
	Vector<TlsObject> & vcdi = m_ptod->CustDefInVec();
	for (int i = 0; i < vcdi.Size(); ++i)
	{
		int iItem = ::SendMessage(m_hwndCstDfn, CB_ADDSTRING, 0,
			(LPARAM)vcdi[i].m_strName.Chars());
		::SendMessage(m_hwndCstDfn, CB_SETITEMDATA, (WPARAM)iItem, (LPARAM)i);
	}
	::SendMessage(m_hwndCstDfn, CB_SETCURSEL, m_ptod->DefaultCstDfnIdx(), 0);

	// Load "Type" Combo Box
	strb.Load(kstidTlsOptCTSingle);
	::SendMessage(m_hwndCstType, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidTlsOptCTTxt);
	::SendMessage(m_hwndCstType, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidTlsOptCTLst);
	::SendMessage(m_hwndCstType, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidTlsOptCTDate);
	::SendMessage(m_hwndCstType, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidTlsOptCTInt);
	::SendMessage(m_hwndCstType, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	::SendMessage(m_hwndCstType, CB_SETCURSEL, 0, 0);

/*	strb.Load(kstidTlsOptCTTxt); Multi-Line Text
	::SendMessage(m_hwndCstType, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidTlsOptCTLst); List Reference
	::SendMessage(m_hwndCstType, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidTlsOptCTMulti); Multi-Single Line Text
	::SendMessage(m_hwndCstType, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidTlsOptCTMono); Single Line Text
	::SendMessage(m_hwndCstType, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidTlsOptCTDate);
	::SendMessage(m_hwndCstType, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidTlsOptCTInt);
	::SendMessage(m_hwndCstType, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	::SendMessage(m_hwndCstType, CB_SETCURSEL, 0, 0);
*/
	// Load "Limit" Combo Box
	strb.Load(kstidTlsOptCuOne);
	::SendMessage(m_hwndCstLimit, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidTlsOptCuMul);
	::SendMessage(m_hwndCstLimit, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	::SendMessage(m_hwndCstLimit, CB_SETCURSEL, (WPARAM)kftMulti, 0);

	// Setup the Custom Fields List Box
	LVCOLUMN lvc = { LVCF_TEXT | LVCF_WIDTH };
	Rect rc;
	::GetClientRect(m_hwndCstFld, &rc);
	lvc.cx = rc.Width();
	ListView_InsertColumn(m_hwndCstFld, 0, &lvc);

	LoadListsCbo();

	// go through all fields of all views and mark them as old fields.
	Vector<TlsObject> & vto = m_ptod->ObjectVec();
	int cuvs = m_ptod->m_vuvs.Size();
	for (int iuvs = 0; iuvs < cuvs; ++iuvs)
	{
		for (int ito = 0; ito < vto.Size(); ++ito)
		{
			RecordSpecPtr qrsp;
			m_ptod->GetBlockVec(m_ptod->m_vuvs, iuvs, ito, &qrsp);
			for (int ifld = 0; ifld < qrsp->m_vqbsp.Size(); ++ifld)
			{
				qrsp->m_vqbsp[ifld]->m_fNewFld = false;
			}
		}
	}

	// Only show the Define In combo if we should be.
	if (!m_ptod->GetfShowCstDfnInCbo())
	{
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDfnCap), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDfn), SW_HIDE);
		Rect rcDfn;
		::GetWindowRect(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDfnCap), &rcDfn);
		::MapWindowPoints(NULL, m_hwnd, (POINT *)&rcDfn, 2);

		Rect rcLst;
		::GetWindowRect(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstFldCap), &rcLst);
		::MapWindowPoints(NULL, m_hwnd, (POINT *)&rcLst, 2);

		::MoveWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstFldCap), rcLst.left, rcDfn.top,
			rcLst.Width(), rcLst.Height() + (rcLst.top - rcDfn.top), true);

		::GetWindowRect(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDfn), &rcDfn);
		::MapWindowPoints(NULL, m_hwnd, (POINT *)&rcDfn, 2);

		::GetWindowRect(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstFld), &rcLst);
		::MapWindowPoints(NULL, m_hwnd, (POINT *)&rcLst, 2);

		::MoveWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstFld), rcLst.left, rcDfn.top,
			rcLst.Width(), rcLst.Height() + (rcLst.top - rcDfn.top), true);
	}

//	Vector<TlsObject> & vto = m_ptod->ObjectVec();
//	if (vto.Size() < 2)
//	{
//
//	}
	OnDefChange();
	return AfDialog::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Loads the Lists combo box with all lists.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgCst::LoadListsCbo()
{
	::SendMessage(m_hwndCstLists, CB_RESETCONTENT, 0, 0);

	AfMainWnd * pafw = MainWindow();
	AssertObj(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);

	Vector<HVO> & vhvo = plpi->GetPsslIds();
//		qftlstd->SetDialogValues(m_hvoPssl, plpi->AnalWs(), vhvo);

	StrApp str;
	int i;
	StrUni stuQuery;
	int cv = vhvo.Size();
	Assert(cv);

	// The following code was adapted from TlsStatsDlg::LoadListCombo()
	// to address a crash after creating a custom field and choosing a possibility list
	// in this combo (DN-844).
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	IOleDbCommandPtr qodc;

	// Load the name of a possibility list from the database and put in combo Box.
	const int kcchBuffer = MAX_PATH;
	OLECHAR rgchName[kcchBuffer];
	HVO hvo;
	StrUni stu;
	for (i = 0; i < cv; ++i)
	{
		hvo = vhvo[i];
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
		int icbo;
		icbo = ::SendMessage(m_hwndCstLists, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		::SendMessage(m_hwndCstLists, CB_SETITEMDATA, (WPARAM)icbo, (LPARAM)hvo);
		if (m_hvoPssl == hvo)
			::SendMessage(m_hwndCstLists, CB_SETCURSEL, (WPARAM)icbo, 0);
	}
}


/*----------------------------------------------------------------------------------------------
	"Define In" Combo box item changed.

	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgCst::OnDefChange()
{
	int iDefIn = SendMessage(m_hwndCstDfn, CB_GETCURSEL, 0, 0);
	int iCustDefIn = ::SendMessage(m_hwndCstDfn, CB_GETITEMDATA, (WPARAM)iDefIn, 0);

	Vector<TlsObject> & vcdi = m_ptod->CustDefInVec();

	::SendMessage(m_hwndCstFld, WM_SETREDRAW, false, 0);
	ListView_DeleteAllItems(m_hwndCstFld);

	ITsStringPtr qtss;
	int cuvs = m_ptod->m_vuvs.Size();
	for (m_iuvs = 0; m_iuvs < cuvs; ++m_iuvs)
	{
		if (m_ptod->m_vuvs[m_iuvs]->m_vwt == kvwtDE)
			break;
	}

	int ivcdi;
	if (vcdi[iCustDefIn].m_nLevel == 1000)
		ivcdi = 0; // It is All Entries, so just get the first one.
	else
		ivcdi = iCustDefIn;

	Vector<TlsObject> & vto = m_ptod->ObjectVec();
	for (m_ito = 0; m_ito < vto.Size(); ++m_ito)
	{
		if (vto[m_ito].m_clsid == vcdi[ivcdi].m_clsid)
			break;
	}

	// Find out if it is the "All Entries" item
	bool fHasAllEntries = false;
	for (int icdi = 0; icdi < vcdi.Size(); ++icdi)
	{
		if (vcdi[icdi].m_nLevel == 1000)
		{
			fHasAllEntries = true;
			break;
		}
	}

	// Get the m_vpbsp of the Define In Class.
	RecordSpecPtr qrsp;
	m_ptod->GetBlockVec(m_ptod->m_vuvs, m_iuvs, m_ito, &qrsp);
	m_vpbsp = qrsp->m_vqbsp;

	// Go through all fields and look for custom fields.
	int ifld;
	for (ifld = 0; ifld < m_vpbsp.Size(); ++ifld)
	{
		qtss = m_vpbsp[ifld]->m_qtssLabel;

		// if the first item has a m_Level of 1000 than it is a "ALL Entries"
		// item so we need to see if this field is in all RecordSpecs.
		if (fHasAllEntries)
		{
			// Go through all Entries and see if this field is in all entries.
			ComBool fFound = false;
			for (int ix = 0; ix < vto.Size(); ++ix)
			{
				if (!vto[ix].m_nLevel)
				{
					RecordSpecPtr qrsp;
					m_ptod->GetBlockVec(m_ptod->m_vuvs, m_iuvs, ix, &qrsp);
					BlockVec & vpbsp = qrsp->m_vqbsp;

					for (int ifld = 0; ifld < vpbsp.Size(); ++ifld)
					{
						qtss->Equals(vpbsp[ifld]->m_qtssLabel, &fFound);
						if (fFound)
							break;
					}
					if (!fFound)
						break;
				}
			}
			// fFound is true if it is in all entries, and false if not in only some.

			if (vcdi[iCustDefIn].m_nLevel == 1000)
			{
				// All Entries is selected.
				if (!fFound)
					// It is NOT in all Entries so skip it.
					continue;
			}
			else
			{
				if (fFound && (vto.Size() > 1))
					// It is in all Entries so skip it.
					continue;
			}
		}

		// This is a custom field that is to be added in the listview.
		StrApp str;
		const OLECHAR * pwrgch;
		int cch;
		LVITEM lvi = { LVIF_TEXT | LVIF_PARAM };

		lvi.iItem = ListView_GetItemCount(m_hwndCstFld);
		qtss->LockText(&pwrgch, &cch);
		str.Assign(pwrgch, cch);
		qtss->UnlockText(pwrgch);

		lvi.pszText = const_cast<achar *>(str.Chars());
		lvi.lParam = ifld;
		ListView_InsertItem(m_hwndCstFld, &lvi);
	}
	SendMessage(m_hwndCstFld, LB_SETCURSEL, (WPARAM)0, 0);
	::SendMessage(m_hwndCstFld, WM_SETREDRAW, true, 0);
	::InvalidateRect(m_hwndCstFld, NULL, true);

	UpdateCtrls();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Delete a custom field.

	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgCst::OnDelFld()
{
	// Get the selected  item then delete it.
	int iItem = ListView_GetNextItem(m_hwndCstFld, -1, LVNI_SELECTED);
	if (iItem < 0)
	{
		// Do nothing if there is no Field selected.
		return true;
	}

	LVITEM lvi;
	lvi.mask = LVIF_PARAM;
	lvi.iItem = iItem;
	lvi.iSubItem = 0;
	ListView_GetItem(m_hwndCstFld, &lvi);
	int iDelitem = lvi.lParam;

	if (!m_vpbsp[iDelitem]->m_fNewFld)
	{
		StrApp strTitle(kstidTlsOptCuDel);
		StrApp strPrompt(kstidTlsOptCuDMsg);

		const achar * pszHelpUrl = m_pszHelpUrl;
		m_pszHelpUrl = _T("Advanced_Tasks/Customizing_Fields/Delete_a_field.htm");

		ConfirmDeleteDlgPtr qcdd;
		qcdd.Create();
		qcdd->SetTitle(strTitle.Chars());
		qcdd->SetPrompt(strPrompt.Chars());
		qcdd->SetHelpUrl(m_pszHelpUrl);
		// Make sure the user really wants to delete the custom field.
		if (qcdd->DoModal(m_hwnd) != kctidOk)
		{
			m_pszHelpUrl = pszHelpUrl;
			return true;
		}

		m_pszHelpUrl = pszHelpUrl;

		IOleDbCommandPtr qodc;
		ComBool fMoreRows;
		ComBool fIsNull;
		ULONG cbSpaceTaken;

		StrUni stuFldName;
		StrUni stuClsName;
		stuFldName = m_vpbsp[iDelitem]->m_stuFldName;
		stuClsName = m_vpbsp[iDelitem]->m_stuClsName;
		StrUni stuQuery;
		switch (m_vpbsp[iDelitem]->m_ft)
		{
			// Note: the unusual nchar(0xA0) etc represent non-printing characters which don't,
			//       on their own, prevent a field from being deleted.
			case kftString:
				stuQuery.Format(L"if exists (select * from %s where %s", stuClsName.Chars(),
					stuFldName.Chars());
				stuQuery.Append(L" like N'%[^'+"
					L"nchar(0xA0)+nchar(0x2000)+'-'+nchar(0x200B)+nchar(0x2028)+"
					L"nchar(0x2029)+nchar(0x3000)+' ]%' ) "
					L"select cast(1 as tinyint) fDataExists "
					L"else select cast(0 as tinyint) fDataExists");
				break;
			case kftMsa:
			case kftRefSeq:
				stuQuery.Format(L"if exists (select * from %s_%s) ", stuClsName.Chars(),
					stuFldName.Chars());
				stuQuery.Append(L"select cast(1 as tinyint) fDataExists "
					L"else select cast(0 as tinyint) fDataExists");
				break;
			case kftGenDate:
			case kftInteger:
				stuQuery.Format(L"if exists (select * from %s where %s is not NULL and %s <> 0)"
					L"select cast(1 as tinyint) fDataExists "
					L"else select cast(0 as tinyint) fDataExists"
					, stuClsName.Chars(), stuFldName.Chars(), stuFldName.Chars());
				break;
			case kftStText:
				stuQuery.Format(L"if exists (select * from %s_%s cf ", stuClsName.Chars(),
					stuFldName.Chars());
				stuQuery.Append(L"join StText_Paragraphs sp on sp.src = cf.dst "
				L"join StTxtPara stp on stp.id = sp.dst "
				L"where stp.contents like N'%[^'+nchar(0xA0)+nchar(0x2000)+'-'+nchar(0x200B)+"
				L"nchar(0x2028)+nchar(0x2029)+nchar(0x3000)+' ]%' ) "
				L"select cast(1 as tinyint) fDataExists "
				L"else select cast(0 as tinyint) fDataExists");
				break;
			case kftRefAtomic:
				stuQuery.Format(L"if exists (select * from %s where %s is not NULL)"
					L"select cast(1 as tinyint) fDataExists "
					L"else select cast(0 as tinyint) fDataExists",
					stuClsName.Chars(), stuFldName.Chars());
				break;
		}
		CheckHr(m_qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));

		bool fFound;
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&fFound),
			isizeof(fFound), &cbSpaceTaken, &fIsNull, 0));
		Assert(cbSpaceTaken == isizeof(fFound));
		if (fFound)
		{
			StrApp strA(kstidTlsOptCuDelDataA);
			StrApp strB(kstidTlsOptCuDelDataB);
			::MessageBox(m_hwnd, strA.Chars(), strB.Chars(), MB_OK | MB_ICONINFORMATION);
			return true;
		}
	}

	m_psiCustFldDel->Insert(m_vpbsp[iDelitem]->m_flid);

	m_iCurSel = ListView_GetNextItem(m_hwndCstFld, -1, LVNI_SELECTED);
	UpdateProperties(true);

	// Update the MetaCache
	AfMainWnd * pafw = MainWindow();
	AssertObj(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfoPtr qdbi = plpi->GetDbInfo();
	IFwMetaDataCachePtr qmdc;
	plpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
	qmdc->Init(m_qode);

	OnDefChange();

	// Select the next Item
	if (iItem == ListView_GetItemCount(m_hwndCstFld))
		iItem --;

	if (iItem >= 0)
	{
		lvi.mask = LVIF_STATE;
		lvi.iItem = iItem;
		lvi.iSubItem = 0;
		lvi.state = LVIS_SELECTED | LVIS_FOCUSED;
		lvi.stateMask = LVIS_SELECTED | LVIS_FOCUSED;
		ListView_SetItem(m_hwndCstFld, &lvi);
		ListView_EnsureVisible(m_hwndCstFld, iItem, false);

		lvi.mask = LVIF_PARAM;
		ListView_GetItem(m_hwndCstFld, &lvi);
	}

	// remove the default buton properties from the delete button if not enabled
	HWND hwndDelBtn = ::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDel);
	if (!::IsWindowEnabled(hwndDelBtn))
	{
		LONG style = ::GetWindowLong(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDel), GWL_STYLE);
		if (style & BS_DEFPUSHBUTTON)
		{
			// turn off default button property
			style = style ^ BS_DEFPUSHBUTTON;
			::SetWindowLong(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDel), GWL_STYLE, style);
			::SetFocus(m_hwndCstFld);
		}
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Add a new custom field.

	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgCst::OnAddFld(bool fCopyFld)
{
	RecordSpecPtr qrsp;
	StrApp str;
	StrApp str1;
	const OLECHAR * pwrgch;
	int cch;

	str.Load(kstidTlsOptNewFld);
	StrApp str2;
	StrApp str3;
	str1 = str;

	bool found;
	int ncnt = 0;
	// Check the user field name to see if it is already in the list.  If it is then make a name
	// with a number appended.  Keep trying until we get a name that is not in the list.
	do
	{
		found = false;
		Vector<TlsObject> & vto = m_ptod->ObjectVec();
		for (int ito = 0; ito < vto.Size(); ++ito)
		{
			m_ptod->GetBlockVec(m_ptod->m_vuvs, m_iuvs, ito, &qrsp);
			BlockVec & vpbsp = qrsp->m_vqbsp;

			if (vpbsp.Size() == 0)
				continue;

			for (int i = 0; i < vpbsp.Size(); ++i)
			{
				vpbsp[i]->m_qtssLabel->LockText(&pwrgch, &cch);
				str3.Assign(pwrgch, cch);
				vpbsp[i]->m_qtssLabel->UnlockText(pwrgch);
				if (str == str3)
				{
					// The field name is already in the list:  append a number to the name.
					ncnt ++;
					str = str1;
					str2.Format(_T("%d"), ncnt);
					str.Append(str2.Chars(),str2.Length());
					found = true;
					break;
				}
			}
			if (found == true)
				break;
		}
	} while (found == true);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu = str.Chars();
	ITsStringPtr qtss;
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &qtss);

	int ivcdi = ::SendMessage(m_hwndCstDfn, CB_GETCURSEL, 0, 0);
	Vector<TlsObject> & vcdi = m_ptod->CustDefInVec();

	// Fill a vector with the classId of the field to be added along with all
	// of it's subclasses.
	Vector<HVO> vClsId;
	vClsId.Push(vcdi[ivcdi].m_clsid);
	IOleDbCommandPtr qodc;
	ComBool fMoreRows;
	ComBool fIsNull;
	ULONG cbSpaceTaken;
	StrUni stuQuery;
	stuQuery.Format(L"select Id from class$ where base = %d",
		vcdi[ivcdi].m_clsid);
	CheckHr(m_qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		HVO clsId;
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&clsId),
			isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
		vClsId.Push(clsId);
		CheckHr(qodc->NextRow(&fMoreRows));
	}

	BlockSpecPtr qbspOld;
	if (fCopyFld)
	{
		int iItem = ListView_GetNextItem(m_hwndCstFld, -1, LVNI_SELECTED);
		LVITEM lviOld;
		lviOld.mask = LVIF_PARAM;
		lviOld.iItem = iItem;
		lviOld.iSubItem = 0;
		ListView_GetItem(m_hwndCstFld, &lviOld);
		qbspOld = m_vpbsp[lviOld.lParam];
	}

	// go through all views and add the new field to all record specs that have
	// a ClassId in the vClsId vector.
	// Use a flid of 0 as a flag that this needs to be added to the Db when OK is Pressed.
	int flid = 0;
	Vector<TlsObject> & vto = m_ptod->ObjectVec();
	int cuvs = m_ptod->m_vuvs.Size();
	for (int iuvs = 0; iuvs < cuvs; ++iuvs)
	{
		// Do not add them to the Browse view.
		if (m_ptod->m_vuvs[iuvs]->m_vwt == kvwtBrowse)
			continue;

		for (int ito = 0; ito < vto.Size(); ++ito)
		{
			RecordSpecPtr qrsp;
			m_ptod->GetBlockVec(m_ptod->m_vuvs, iuvs, ito, &qrsp);

			// Only change Recordspecs that match the DefineIn combo
			if (vcdi[ivcdi].m_nLevel < 1000)
			{
				bool fMatched = false;
				for (int icls = 0; icls < vClsId.Size(); ++icls)
				{
					if (vClsId[icls] == qrsp->m_clsid)
					{
						fMatched = true;
						break;
					}
				}

				if (!fMatched)
				{
					// skip this record spec.
					continue;
				}
			}
			BlockSpecPtr qbsp;
			FldVis fldVis;
			fldVis = m_ptod->GetCustVis(m_ptod->m_vuvs[iuvs]->m_vwt, ito);

			if (fCopyFld)
			{
				qbsp.Attach(NewObj BlockSpec(qtss, qbspOld->m_qtssHelp,	flid,
					qbspOld->m_ft, qbspOld->m_eVisibility, qbspOld->m_fRequired,
					qbspOld->m_stuSty.Chars(), qbspOld->m_ws, true, qbspOld->m_hvoPssl));
			}
			else
			{
				qbsp.Attach(NewObj BlockSpec(qtss, qtss, flid, kftString, fldVis, kFTReqNotReq,
					L"", kwsAnal, true, NULL));
				qbsp->m_hvoPssl = 0;
			}
			qbsp->m_fNewFld = true;
			qbsp->m_fCustFld = true;
			// Temporarily store the clasid in the m_hvo until the OK button is pressed.
			qbsp->m_hvo = vcdi[ivcdi].m_clsid;
			qrsp->m_vqbsp.Push(qbsp);
		}
	}

	LVITEM lvi = { LVIF_TEXT | LVIF_PARAM };
	int iNewItem = ListView_GetItemCount(m_hwndCstFld);
	lvi.iItem = iNewItem;
	lvi.pszText = const_cast<achar *>(str.Chars());
	lvi.lParam = m_vpbsp.Size();
	ListView_InsertItem(m_hwndCstFld, &lvi);

	LVFINDINFO lvfi = { LVFI_STRING };
	lvfi.psz = const_cast<achar *>(str.Chars());
	int iitem = ListView_FindItem(m_hwndCstFld, -1, &lvfi);
	Assert(iitem != -1);

	ListView_SetItemState(m_hwndCstFld, iitem, LVIS_FOCUSED | LVIS_SELECTED,
	LVIS_FOCUSED | LVIS_SELECTED);
	ListView_EnsureVisible(m_hwndCstFld, iitem, false);
	::SetFocus(m_hwndCstFld);
	ListView_EditLabel(m_hwndCstFld, iitem);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Get the index (of the requested writing system) into the dialog box.

	@return index into dialog box
----------------------------------------------------------------------------------------------*/
int TlsOptDlgCst::getWsIndex(int ws)
{
	int iWS = -1;
	int cWS = m_vCboWS.Size();
	for (int iWStemp=0; iWStemp < cWS; iWStemp++)
	{
		if (m_vCboWS[iWStemp] == ws)
		{
			iWS = iWStemp;
		}
	}
	if (iWS != -1)
		return iWS;
	else
		return 0;
}


/*----------------------------------------------------------------------------------------------
	Update field controls in this window.

	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgCst::UpdateCtrls()
{
	RecordSpecPtr qrsp;
	m_ptod->GetBlockVec(m_ptod->m_vuvs, m_iuvs, m_ito, &qrsp);
	m_vpbsp = qrsp->m_vqbsp;

	if (ListView_GetItemCount(m_hwndCstFld) == 0)
	{
		// Disable or hide controls if there are no fields.
		SendMessage(m_hwndCstDesc, WM_SETTEXT, 0, (LPARAM)_T(""));
		SendMessage(m_hwndCstType, CB_SETCURSEL, 0, 0);
		SendMessage(m_hwndCstLimit, CB_SETCURSEL, 0, 0);
		SendMessage(m_hwndCstWS, CB_SETCURSEL, 0, 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDel), 0);

		::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstDes), 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstTyp), 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstCap), 0);

		::ShowWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstDes), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstTyp), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstCap), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstWS), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLst), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLstcbo), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDes), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstTyp), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLimit), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstWS), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstLstCap), SW_HIDE);
	}
	else
	{
		// There are fields so update and enable the controls.

		::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstWS), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstCap), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstTyp), true);

		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstWS), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLimit), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLstcbo), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLst), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstLstCap), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstTyp), true);

		// Get the selected item.
		int iItem = ListView_GetNextItem(m_hwndCstFld, -1, LVNI_SELECTED);
		if (iItem < 0)
		{
			iItem = 0;
			ListView_SetItemState(m_hwndCstFld, iItem, LVIS_FOCUSED | LVIS_SELECTED,
			LVIS_FOCUSED | LVIS_SELECTED);
			ListView_EnsureVisible(m_hwndCstFld, iItem, false);
		}

		LVITEM lvi;
		lvi.mask = LVIF_PARAM;
		lvi.iItem = iItem;
		lvi.iSubItem = 0;
		ListView_GetItem(m_hwndCstFld, &lvi);
		int ivbsp = lvi.lParam;

		m_hvoPssl = qrsp->m_vqbsp[ivbsp]->m_hvoPssl;

		StrApp str;
		const OLECHAR * pwrgch;
		int cch;
		qrsp->m_vqbsp[ivbsp]->m_qtssHelp->LockText(&pwrgch, &cch);
		str.Assign(pwrgch, cch);
		qrsp->m_vqbsp[ivbsp]->m_qtssHelp->UnlockText(pwrgch);
		SendMessage(m_hwndCstDesc, WM_SETTEXT, 0, (LPARAM)str.Chars());

		// Show/Enable or Disable controls
		::ShowWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstDes), SW_SHOW);
		::ShowWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstTyp), SW_SHOW);
		::ShowWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstCap), SW_SHOW);
		::ShowWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstWS), SW_SHOW);

		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDes), SW_SHOW);
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstTyp), SW_SHOW);
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLimit), SW_SHOW);
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLst), SW_SHOW);
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLstcbo), SW_SHOW);
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstWS), SW_SHOW);
		::ShowWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstLstCap), SW_SHOW);

		m_vCboWS.Clear();

		StrAppBuf strb;
		if (qrsp->m_vqbsp[ivbsp]->m_ft == kftMsa)
		{
			SendMessage(m_hwndCstLists, CB_SETCURSEL, (WPARAM)-1, 0);
			SendMessage(m_hwndCstType, CB_SETCURSEL, (WPARAM)kcftSingle, 0);
			SendMessage(m_hwndCstLimit, CB_SETCURSEL, (WPARAM)kftOne, 0);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLimit), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLst), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLstcbo), false);

			// Load "Writing System" Combo Box
			::SendMessage(m_hwndCstWS, CB_RESETCONTENT, 0, 0);
			m_vCboWS.Push(kwsAnal);
			strb.Load(kstidWSAnalWs);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
			m_vCboWS.Push(kwsAnals);
			strb.Load(kstidWSAnalWss);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

			m_vCboWS.Push(kwsVern);
			strb.Load(kstidWSVernWs);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
			m_vCboWS.Push(kwsVerns);
			strb.Load(kstidWSVernWss);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

			m_vCboWS.Push(kwsAnalVerns);
			strb.Load(kstidWSAnalVernWss);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
			m_vCboWS.Push(kwsVernAnals);
			strb.Load(kstidWSVernAnalWss);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

			// display the appropriate choice in the dialog box
			int ws = qrsp->m_vqbsp[ivbsp]->m_ws;
			int iWS = getWsIndex(ws);
			::SendMessage(m_hwndCstWS, CB_SETCURSEL, (WPARAM)iWS, 0);
		}
		if (qrsp->m_vqbsp[ivbsp]->m_ft == kftString)
		{
			SendMessage(m_hwndCstLists, CB_SETCURSEL, (WPARAM)-1, 0);
			SendMessage(m_hwndCstType, CB_SETCURSEL, (WPARAM)kcftSingle, 0);
			SendMessage(m_hwndCstLimit, CB_SETCURSEL, (WPARAM)kftOne, 0);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLimit), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLst), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLstcbo), false);

			// Load "Writing System" Combo Box
			::SendMessage(m_hwndCstWS, CB_RESETCONTENT, 0, 0);
			m_vCboWS.Push(kwsAnal);
			strb.Load(kstidWSAnalWs);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
			m_vCboWS.Push(kwsAnals);
			strb.Load(kstidWSAnalWss);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

			m_vCboWS.Push(kwsVern);
			strb.Load(kstidWSVernWs);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
			m_vCboWS.Push(kwsVerns);
			strb.Load(kstidWSVernWss);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

			m_vCboWS.Push(kwsAnalVerns);
			strb.Load(kstidWSAnalVernWss);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
			m_vCboWS.Push(kwsVernAnals);
			strb.Load(kstidWSVernAnalWss);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

			// display the appropriate choice in the dialog box
			int ws = qrsp->m_vqbsp[ivbsp]->m_ws;
			int iWS = getWsIndex(ws);
			::SendMessage(m_hwndCstWS, CB_SETCURSEL, (WPARAM)iWS, 0);
		}
		if (qrsp->m_vqbsp[ivbsp]->m_ft == kftStText)
		{
			SendMessage(m_hwndCstLists, CB_SETCURSEL, (WPARAM)-1, 0);
			SendMessage(m_hwndCstType, CB_SETCURSEL, (WPARAM)kcftTxt, 0);
			SendMessage(m_hwndCstLimit, CB_SETCURSEL, (WPARAM)kftOne, 0);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLimit), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLst), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLstcbo), false);

			// Load "Writing System" Combo Box
			::SendMessage(m_hwndCstWS, CB_RESETCONTENT, 0, 0);
			m_vCboWS.Push(kwsAnal);
			strb.Load(kstidWSAnalWs);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
			m_vCboWS.Push(kwsVern);
			strb.Load(kstidWSVernWs);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

			// display the appropriate choice in the dialog box
			int ws = qrsp->m_vqbsp[ivbsp]->m_ws;
			int iWS = getWsIndex(ws);
			::SendMessage(m_hwndCstWS, CB_SETCURSEL, (WPARAM)iWS, 0);
		}
		if (qrsp->m_vqbsp[ivbsp]->m_ft == kftRefSeq)
		{
			SendMessage(m_hwndCstType, CB_SETCURSEL, (WPARAM)kcftList, 0);
			SendMessage(m_hwndCstLimit, CB_SETCURSEL, (WPARAM)kftMulti, 0);

			int cLst = SendMessage(m_hwndCstLists, CB_GETCOUNT, 0, 0);
			bool fFound = false;
			for (int icbo = 0; icbo < cLst; ++icbo)
			{
				if (m_hvoPssl == SendMessage(m_hwndCstLists, CB_GETITEMDATA, (WPARAM)icbo, 0))
				{
					SendMessage(m_hwndCstLists, CB_SETCURSEL, (WPARAM)icbo, 0);
					fFound = true;
					break;
				}
			}

			// if not a valid list then force it to the first one in the list.
			if (!fFound)
				SendMessage(m_hwndCstLists, CB_SETCURSEL, (WPARAM)0, 0);

			// Load "Writing System" Combo Box
			::SendMessage(m_hwndCstWS, CB_RESETCONTENT, 0, 0);
			m_vCboWS.Push(kwsAnal);
			strb.Load(kstidWSAnalWs);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
			m_vCboWS.Push(kwsVern);
			strb.Load(kstidWSVernWs);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

			m_vCboWS.Push(kwsAnalVerns);
			strb.Load(kstidWSAnalVernWss);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
			m_vCboWS.Push(kwsVernAnals);
			strb.Load(kstidWSVernAnalWss);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

			// display the appropriate choice in the dialog box
			int ws = qrsp->m_vqbsp[ivbsp]->m_ws;
			int iWS = getWsIndex(ws);
			::SendMessage(m_hwndCstWS, CB_SETCURSEL, (WPARAM)iWS, 0);
		}
		if (qrsp->m_vqbsp[ivbsp]->m_ft == kftRefAtomic)
		{
			SendMessage(m_hwndCstType, CB_SETCURSEL, (WPARAM)kcftList, 0);
			SendMessage(m_hwndCstLimit, CB_SETCURSEL, (WPARAM)kftOne, 0);

			int cLst = SendMessage(m_hwndCstLists, CB_GETCOUNT, 0, 0);
			bool fFound = false;
			for (int icbo = 0; icbo < cLst; ++icbo)
			{
				if (m_hvoPssl == SendMessage(m_hwndCstLists, CB_GETITEMDATA, (WPARAM)icbo, 0))
				{
					SendMessage(m_hwndCstLists, CB_SETCURSEL, (WPARAM)icbo, 0);
					fFound = true;
					break;
				}
			}

			// if not a valid list then force it to the first one in the list.
			if (!fFound)
				SendMessage(m_hwndCstLists, CB_SETCURSEL, (WPARAM)0, 0);

			// Load "Writing System" Combo Box
			::SendMessage(m_hwndCstWS, CB_RESETCONTENT, 0, 0);
			m_vCboWS.Push(kwsAnal);
			strb.Load(kstidWSAnalWs);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
			m_vCboWS.Push(kwsVern);
			strb.Load(kstidWSVernWs);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

			m_vCboWS.Push(kwsAnalVerns);
			strb.Load(kstidWSAnalVernWss);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
			m_vCboWS.Push(kwsVernAnals);
			strb.Load(kstidWSVernAnalWss);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

			// display the appropriate choice in the dialog box
			int ws = qrsp->m_vqbsp[ivbsp]->m_ws;
			int iWS = getWsIndex(ws);
			::SendMessage(m_hwndCstWS, CB_SETCURSEL, (WPARAM)iWS, 0);
		}
		if (qrsp->m_vqbsp[ivbsp]->m_ft == kftGenDate)
		{
			SendMessage(m_hwndCstLists, CB_SETCURSEL, (WPARAM)-1, 0);
			SendMessage(m_hwndCstType, CB_SETCURSEL, (WPARAM)kcftDate, 0);
			SendMessage(m_hwndCstLimit, CB_SETCURSEL, (WPARAM)kftOne, 0);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLimit), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLst), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLstcbo), false);

			// Load "Writing System" Combo Box
			::SendMessage(m_hwndCstWS, CB_RESETCONTENT, 0, 0);
			m_vCboWS.Push(kwsAnal);
			strb.Load(kstidWSAnalWs);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
			::SendMessage(m_hwndCstWS, CB_SETCURSEL, (WPARAM)0, 0);
			::EnableWindow(m_hwndCstWS, false);
		}
		if (qrsp->m_vqbsp[ivbsp]->m_ft == kftInteger)
		{
			SendMessage(m_hwndCstLists, CB_SETCURSEL, (WPARAM)-1, 0);
			SendMessage(m_hwndCstType, CB_SETCURSEL, (WPARAM)kcftInt, 0);
			SendMessage(m_hwndCstLimit, CB_SETCURSEL, (WPARAM)kftOne, 0);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLimit), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLst), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLstcbo), false);

			// Load "Writing System" Combo Box
			::SendMessage(m_hwndCstWS, CB_RESETCONTENT, 0, 0);
			m_vCboWS.Push(kwsAnal);
			strb.Load(kstidWSAnalWs);
			::SendMessage(m_hwndCstWS, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
			::SendMessage(m_hwndCstWS, CB_SETCURSEL, (WPARAM)0, 0);
			::EnableWindow(m_hwndCstWS, false);
		}

		::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstCap), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDel), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstDes), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDes), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstTyp), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstWS), true);

		if (!qrsp->m_vqbsp[ivbsp]->m_fNewFld)
		{
			::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstWS), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstCap), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstTyp), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstLstCap), false);

			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstWS), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLimit), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLst), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstLstcbo), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstTyp), false);
		}
		if (!qrsp->m_vqbsp[ivbsp]->m_fCustFld)
		{
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDel), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kstidTlsOptDlgCstDes), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstDes), false);
		}
		switch (qrsp->m_vqbsp[ivbsp]->m_ft)
		{
		case kftDateRO:
		case kftObjRefSeq:
		case kftSubItems:
		case kftRefCombo:
		case kftExpandable:
		case kftUnicode:
		case kftMta:
		case kftEnum:
		case kftTtp:
		case kftDummy:
		case kftLimEmbedLabel:
		case kftGroup:
		case kftGroupOnePerLine:
		case kftTitleGroup:
		case kftDate:
		case kftObjRefAtomic:
		case kftBackRefAtomic:
		case kftObjOwnSeq:
		case kftObjOwnCol:
		case kftGuid:
		case kftStTextParas:
			{
				LONG style = ::GetWindowLong(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstCopy), GWL_STYLE);
				if (style & BS_DEFPUSHBUTTON)
				{
					// turn off default button property
					style = style ^ BS_DEFPUSHBUTTON;
					::SetWindowLong(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstCopy), GWL_STYLE, style);
				}
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstCopy), false);
				SendMessage(m_hwndCstLists, CB_SETCURSEL, (WPARAM)-1, 0);
				SendMessage(m_hwndCstType, CB_SETCURSEL, (WPARAM)-1, 0);
				SendMessage(m_hwndCstLimit, CB_SETCURSEL, (WPARAM)-1, 0);
				SendMessage(m_hwndCstWS, CB_SETCURSEL, (WPARAM)-1, 0);
			}
			break;
		default:
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgCstCopy), true);
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
bool TlsOptDlgCst::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_ERASEBKGND)
	{
		// this is required to prevent the listview from not painting when selected then covered
		// by another window, then uncovered.  without this the listview will not repaint.
		RedrawWindow(m_hwndCstFld, NULL, NULL, RDW_ERASE | RDW_FRAME | RDW_INTERNALPAINT |
			RDW_INVALIDATE);
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Goes through all views in a userviewspec and changes the properties of any fields that have
	the specified fieldname.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgCst::UpdateProperties(bool fDelete)
{
	int iItem = ListView_GetNextItem(m_hwndCstFld, -1, LVNI_SELECTED);
	LVITEM lvi;
	lvi.mask = LVIF_PARAM;
	lvi.iItem = iItem;
	lvi.iSubItem = 0;
	ListView_GetItem(m_hwndCstFld, &lvi);
	ITsStringPtr qtssLabel = m_vpbsp[lvi.lParam]->m_qtssLabel;

	m_iCboTyp = SendMessage(m_hwndCstType, CB_GETCURSEL, 0, 0);
	m_nCboLimit = SendMessage(m_hwndCstLimit, CB_GETCURSEL, 0, 0);
	m_nCboWS = SendMessage(m_hwndCstWS, CB_GETCURSEL, 0, 0);

	// go through all views and change the string
	Vector<TlsObject> & vto = m_ptod->ObjectVec();
	int cuvs = m_ptod->m_vuvs.Size();
	for (int iuvs = 0; iuvs < cuvs; ++iuvs)
	{
		for (int ito = 0; ito < vto.Size(); ++ito)
		{
			RecordSpecPtr qrsp;
			m_ptod->GetBlockVec(m_ptod->m_vuvs, iuvs, ito, &qrsp);
			BlockVec vpbsp = qrsp->m_vqbsp;

			// We now have a vpbsp so go through all Entries and change name.

			// Go through all fields and look for a field with that name.
			int ifld;
			for (ifld = 0; ifld < qrsp->m_vqbsp.Size(); ++ifld)
			{
				ComBool fEquals = false;
				qtssLabel->Equals(qrsp->m_vqbsp[ifld]->m_qtssLabel, &fEquals);
				if (fEquals)
				{
					if (fDelete)
					{
						// We found one that needs to be Deleted.
						qrsp->m_vqbsp.Delete(ifld, ifld + 1);
					}
					else
					{
						// We found one that needs to be changed.
						qrsp->m_vqbsp[ifld]->m_qtssHelp = m_qtssNewDesc;

						int ws = m_vCboWS[m_nCboWS];
						qrsp->m_vqbsp[ifld]->m_ws = ws;

						switch (m_iCboTyp)
						{
						case kcftSingle:
							if (ws == kwsAnal || ws == kwsVern)
								qrsp->m_vqbsp[ifld]->m_ft = kftString;
							else
								qrsp->m_vqbsp[ifld]->m_ft = kftMsa;
							break;
						case kcftTxt:
							qrsp->m_vqbsp[ifld]->m_ft = kftStText;
							break;
						case kcftList:
							qrsp->m_vqbsp[ifld]->m_hvoPssl = m_hvoPssl;

							if (m_nCboLimit == kftOne)
								qrsp->m_vqbsp[ifld]->m_ft = kftRefAtomic;
							else
								qrsp->m_vqbsp[ifld]->m_ft = kftRefSeq;
							break;
						case kcftDate:
							qrsp->m_vqbsp[ifld]->m_ft = kftGenDate;
							break;
						case kcftInt:
							qrsp->m_vqbsp[ifld]->m_ft = kftInteger;
							break;
						}
					}

					qrsp->m_fDirty = true;
					// Add the window to the delete list which will update it when OK is pressed.
					int iwndClient = m_ptod->m_vuvs[iuvs]->m_iwndClient;
					if (iwndClient >= 0)
						m_psiwndClientDel->Insert(iwndClient);
				}
			}
		}
	}
	return true;
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
bool TlsOptDlgCst::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	NMLISTVIEW * pnmlv = (NMLISTVIEW *)pnmh;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	ITsStringPtr qtss;

	switch (pnmh->code)
	{
	case CBN_SELCHANGE: // This is also LBN_SELCHANGE. Combo box or list box item changed.
		{
		m_ptod->SetCustFldDirty(true);
		int iItem = ListView_GetNextItem(m_hwndCstFld, -1, LVNI_SELECTED);
		LVITEM lvi;
		lvi.mask = LVIF_PARAM;
		lvi.iItem = iItem;
		lvi.iSubItem = 0;
		ListView_GetItem(m_hwndCstFld, &lvi);

		switch (pnmh->idFrom)
			{
			case kcidTlsOptDlgCstDfn: // Define In Combo box item changed.
				return OnDefChange();

			case kcidTlsOptDlgCstLstcbo: // Lists Combo box item changed.
				{
				int icbo = SendMessage(m_hwndCstLists, CB_GETCURSEL, 0, 0);
				m_hvoPssl = SendMessage(m_hwndCstLists, CB_GETITEMDATA, (WPARAM)icbo, 0);
				}
			case kcidTlsOptDlgCstTyp: // Type Combo box item changed.
			case kcidTlsOptDlgCstLimit: // Limit Combo box item changed.
			case kcidTlsOptDlgCstWS: // Limit Combo box item changed.
				StrAppBufHuge strbh;
				int cch = ::SendDlgItemMessage(m_hwnd, kcidTlsOptDlgCstDes, WM_GETTEXT,
					strbh.kcchMaxStr, (LPARAM)strbh.Chars());
				strbh.SetLength(cch);
				ITsStringPtr qtssNew;
				StrUni stu;
				stu = strbh.Chars();
				qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser,
					&m_qtssNewDesc);
				m_iCurSel = ListView_GetNextItem(m_hwndCstFld, -1, LVNI_SELECTED);

				if (kcftList == SendMessage(m_hwndCstType, CB_GETCURSEL, 0, 0))
				{
					if (m_hvoPssl == 0)
					{
						SendMessage(m_hwndCstLimit, CB_SETCURSEL, kftMulti, 0);
						SendMessage(m_hwndCstLists, CB_SETCURSEL, 0, 0);
						m_hvoPssl = SendMessage(m_hwndCstLists, CB_GETITEMDATA, (WPARAM)0, 0);
					}
				}
				else
				{
					m_hvoPssl = 0;
					SendMessage(m_hwndCstLists, CB_SETCURSEL, 0, 0);
				}

				UpdateProperties(false);
				UpdateCtrls();
				break;
			}

		return true;
		break;
		}
	case BN_CLICKED:
		m_ptod->SetCustFldDirty(true);
		switch (pnmh->idFrom)
		{
		case kcidTlsOptDlgCstAdd:
			return OnAddFld(false);

		case kcidTlsOptDlgCstCopy:
			return OnAddFld(true);

		case kcidTlsOptDlgCstDel:
			return OnDelFld();

		case kcidTlsOptDlgCstLst:
			{
			TlsListsDlgPtr qftlstd;
			qftlstd.Create();

			// Use our window title as default header.
			achar rgchName[MAX_PATH];
			::SendMessage(m_hwnd, WM_GETTEXT, MAX_PATH, (LPARAM)rgchName);

			AfMainWnd * pafw = MainWindow();
			AssertObj(pafw);
			AfLpInfo * plpi = pafw->GetLpInfo();
			AssertPtr(plpi);

			Vector<HVO> & vhvo = plpi->GetPsslIds();
			qftlstd->SetDialogValues(m_hvoPssl, plpi->AnalWs(), true, vhvo);

			// Run the dialog.
			if (qftlstd->DoModal(Hwnd()) == kctidOk)
			{
				qftlstd->GetDialogValues(&m_hvoPssl);
				LoadListsCbo();
			}
			UpdateProperties(false);
			return true;
			}
		}
		break;
	case EN_CHANGE:
		{
		switch (pnmh->idFrom)
			{
			case kcidTlsOptDlgCstDes:
				{
					// This is needed because there is no other way to see what the edited text
					// is when someone finishes editing then clicks another field in the
					// listview control.  For some reason the listview updates and reloads the
					// edit box before  the "EN_KILLFOCUS" message is received.
					StrAppBufHuge strbh;
					int cch = ::SendDlgItemMessage(m_hwnd, kcidTlsOptDlgCstDes, WM_GETTEXT,
						strbh.kcchMaxStr, (LPARAM)strbh.Chars());
					strbh.SetLength(cch);
					ITsStringPtr qtssNew;
					StrUni stu;
					stu = strbh.Chars();
					qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser,
						&m_qtssNewDesc);
					m_iCurSel = ListView_GetNextItem(m_hwndCstFld, -1, LVNI_SELECTED);
					m_ptod->SetCustFldDirty(true);
				}
			}
		}
		break;
	case EN_KILLFOCUS:
		{
		if (pnmh->idFrom == kcidTlsOptDlgCstDes)
			UpdateProperties(false);
		break;
		}
	case LVN_ITEMCHANGED:
		if (pnmh->idFrom == kcidTlsOptDlgCstFld && pnmlv->uNewState & LVIS_SELECTED)
		{
			UpdateCtrls();
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

	case LVN_KEYDOWN:
		{
			NMLVKEYDOWN * pnmlvkd = (NMLVKEYDOWN *)pnmh;
			if (pnmlvkd->wVKey == VK_F2)
			{
				int iitem = ListView_GetNextItem(pnmh->hwndFrom, -1, LVNI_SELECTED);
				if (iitem != -1)
					ListView_EditLabel(pnmh->hwndFrom, iitem);
			}
			break;
		}

	case LVN_BEGINLABELEDIT:
		if (pnmh->idFrom == kcidTlsOptDlgCstFld)
		{
			int iItem = ListView_GetNextItem(m_hwndCstFld, -1, LVNI_SELECTED);
			LVITEM lvi;
			lvi.mask = LVIF_PARAM;
			lvi.iItem = iItem;
			lvi.iSubItem = 0;
			ListView_GetItem(m_hwndCstFld, &lvi);

			if (!m_vpbsp[lvi.lParam]->m_fCustFld)
			{
				lnRet = true;
				return true;	// prevent the user from editing the label
			}
			m_iEditlParam = lvi.lParam;
		}
		break;

	case LVN_ENDLABELEDIT:
		if (pnmh->idFrom == kcidTlsOptDlgCstFld)
		{
			NMLVDISPINFO * plvdi = (NMLVDISPINFO *)pnmh;
			if (!plvdi->item.pszText)
			{
				m_dirty = true;
				m_ptod->SetCustFldDirty(true);
				lnRet = false;
				return true;
			}

			// Strip off blank characters at the front and end of the name.
			StrApp strT;
			StrUtil::TrimWhiteSpace(plvdi->item.pszText, strT);

			int iItem = plvdi->item.iItem;

			StrApp strOld;
			ITsStringPtr qtssOld = m_vpbsp[m_iEditlParam]->m_qtssLabel;
			const OLECHAR * pwrgchLbl;
			int cchLbl;
			qtssOld->LockText(&pwrgchLbl, &cchLbl);
			strOld.Assign(pwrgchLbl, cchLbl);
			qtssOld->UnlockText(pwrgchLbl);

			if (!strT.Length())
				strT = strOld;
			if (strT == strOld)
			{
				// if nothing was changed just return false and the old value will be reloaded.
				m_dirty = true;
				m_ptod->SetCustFldDirty(true);
				lnRet = false;
				return true;
			}

			// Check all fields to see if this is a duplicate label
			// Go through each item in "Define In combo"
			int cType = m_ptod->ObjectVec().Size();
			for (int iType = 0; iType < cType; ++iType)
			{
				RecordSpecPtr qrsp;
				m_ptod->GetBlockVec(m_ptod->m_vuvs, m_iuvs, iType, &qrsp);
				BlockVec vpbsp = qrsp->m_vqbsp;
				for (int i = 0; i < vpbsp.Size(); ++i)
				{
					vpbsp[i]->m_qtssLabel->LockText(&pwrgchLbl, &cchLbl);
					StrApp strLbl;
					strLbl.Assign(pwrgchLbl, cchLbl);
					vpbsp[i]->m_qtssLabel->UnlockText(pwrgchLbl);
					if (strT.EqualsCI(strLbl))
					{
						// There is already a standard field with this label.
						StrApp strMsg(kstidTlsOptCuDifLab);
						StrApp strTit(kstidTlsOptCuDifLabCap);
						::MessageBox(m_hwnd, strMsg.Chars(), strTit.Chars(), MB_OK);
						::SetFocus(m_hwndCstFld);
						ListView_EditLabel(m_hwndCstFld, iItem);
						lnRet = false;
						return true;
					}
				}
			}

			// create a tss string for the new string
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			StrUni stu = strT;
			ITsStringPtr qtssNew;
			qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &qtssNew);

			// go through all views and change the string
			Vector<TlsObject> & vto = m_ptod->ObjectVec();
			int cuvs = m_ptod->m_vuvs.Size();
			for (int iuvs = 0; iuvs < cuvs; ++iuvs)
			{
				for (int ito = 0; ito < vto.Size(); ++ito)
				{
					RecordSpecPtr qrsp;
					m_ptod->GetBlockVec(m_ptod->m_vuvs, iuvs, ito, &qrsp);
					BlockVec vpbsp = qrsp->m_vqbsp;

					// We now have a vpbsp so go through all Entries and change name.

					// Go through all fields and look for a field with that name.
					int ifld;
					for (ifld = 0; ifld < vpbsp.Size(); ++ifld)
					{
						ComBool fEquals = false;
						qtssOld->Equals(vpbsp[ifld]->m_qtssLabel, &fEquals);
						if (fEquals)
						{
							// We found one that needs to be changed.
							vpbsp[ifld]->m_qtssLabel = qtssNew;
							break;
						}
					}
				}
			}

			m_dirty = true;
			StrAppBufHuge strbh;
			int cch2 = ::SendDlgItemMessage(m_hwnd, kcidTlsOptDlgCstDes, WM_GETTEXT,
				strbh.kcchMaxStr, (LPARAM)strbh.Chars());
			strbh.SetLength(cch2);
			stu = strbh.Chars();
			qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &m_qtssNewDesc);
			m_ptod->SetCustFldDirty(true);
			lnRet = true;
			UpdateProperties(false);
			return true;
		}
	}
	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}

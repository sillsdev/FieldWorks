/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ListsPropDlg.cpp
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Implementation of the Lists Properties Dialog class.

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	ListsPropDlg Implementation
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
ListsPropDlg::ListsPropDlg()
{
	m_rid = kridLangProjPropDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/File/Properties/Topics_List_Properties_General_tab.htm");
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
ListsPropDlg::~ListsPropDlg()
{
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
bool ListsPropDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	GeneralPropDlgTabPtr qgenp;
	qgenp.Attach(NewObj GeneralPropDlgTab(this, m_ctidName));
	qgenp->EnableLocation(true);
	qgenp->EnableSize(true);
	qgenp->EnableModified(true);
	qgenp->EnableDescription(true);
	AfDialogViewPtr qdlgv;
	qdlgv = qgenp;
	m_vqdlgv.Push(qdlgv);
	qdlgv.Attach(NewObj DetailsPropDlgTab(this));
	m_vqdlgv.Push(qdlgv);

	m_hwndTab = ::GetDlgItem(m_hwnd, kcidLangProjPropDlgTab);

	// WARNING: If this ever gets changed to anything but a fixed length buffer, make sure
	// ti.pszText is set after loading each string, since the memory pointed to by strb
	// could be different each time.
	StrAppBuf strb;
	TCITEM ti;
	ti.mask = TCIF_TEXT;
	ti.pszText = const_cast<achar *>(strb.Chars());

	// Add a tab to the tab control for each dialog view.
	strb.Load(kstidGeneralPropTab);
	TabCtrl_InsertItem(m_hwndTab, kidlgGeneral, &ti);
	strb.Load(kstidListsDetails);
	TabCtrl_InsertItem(m_hwndTab, kridDetailsPropDlg, &ti);

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

	ShowChildDlg(m_itabInitial);

	AfApp::Papp()->EnableMainWindows(false);

	::SetFocus(m_hwndTab);
	::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)m_strWndCaption.Chars());

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	This method checks for duplicates in list names.  If the given name exists then return true.

	@param pszName name to check to see if it exists.
	@return True if name already exists.
----------------------------------------------------------------------------------------------*/
bool ListsPropDlg::CheckName(const achar * pszName)
{
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	Vector<HVO> & vhvo = plpi->GetPsslIds();

	AfDbInfoPtr qdbi = plpi->GetDbInfo();
	AssertPtr(qdbi);
	IOleDbEncapPtr qode;
	qdbi->GetDbAccess(&qode);

	StrAnsi sta;
	int i;
	StrUni stuIds;
	int cv = vhvo.Size();
	Assert(cv);

	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	IOleDbCommandPtr qodc;

	const int kcchBuffer = MAX_PATH;
	OLECHAR rgchName[kcchBuffer];
	HVO hvo;
	StrUni stu;
	for (i = 0; i < cv; ++i)
	{
		hvo = vhvo[i];
		stu.Format(L"exec GetOrderedMultiTxt '%d', %d",
			hvo, kflidCmMajorObject_Name);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		Assert(fMoreRows); // This proc should always return something.
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchName),
			kcchBuffer * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
		sta = rgchName;
		if (hvo != m_hvoObj && sta.Equals(pszName))
			return false;
	}
	return true;
}
/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the OK or the Apply Now button.
	When the framework calls this method, changes made in the dialog are accepted.
	The default OnApply closes the dialog.

	@param fClose not used here

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool ListsPropDlg::OnApply(bool fClose)
{
	SetFocus(::GetDlgItem(m_hwnd, kctidOk));
	AfApp::Papp()->EnableMainWindows(true);
	return AfDialog::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Pass the message on to the current sub dialog.

	@return true
----------------------------------------------------------------------------------------------*/
bool ListsPropDlg::OnHelp()
{
	if(m_itabCurrent == 0)
		m_pszHelpUrl = _T("User_Interface/Menus/File/Properties/Topics_List_Properties_General_tab.htm");
	else
		m_pszHelpUrl = _T("User_Interface/Menus/File/Properties/Topics_List_Properties_Details_tab.htm");
	AfUtil::ShowHelpFile(AfApp::Papp()->GetHelpFile(), m_pszHelpUrl);
	return true;
}


//:>********************************************************************************************
//:>	DetailsPropDlgTab Implementation
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
DetailsPropDlgTab::DetailsPropDlgTab(PropertiesDlg * ppropd)
{
	m_rid = kridDetailsPropDlg;
	m_ppropd = ppropd;
	m_hfontLarge = NULL;
	m_fInitialized = false;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
DetailsPropDlgTab::~DetailsPropDlgTab()
{
	if (m_hfontLarge)
	{
		AfGdi::DeleteObjectFont(m_hfontLarge);
		m_hfontLarge = NULL;
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
bool DetailsPropDlgTab::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	HICON hicon = m_ppropd->GetIconHandle();
	if (hicon)
	{
		::SendMessage(::GetDlgItem(m_hwnd, kridDetailsPropTabObjIcon), STM_SETICON,
			(WPARAM)hicon, (LPARAM)0);
	}

	m_hfontLarge = AfGdi::CreateFont(16, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
	{
		::SendMessage(::GetDlgItem(m_hwnd, kridDetailsPropTabBigName), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	}
	::SetWindowText(::GetDlgItem(m_hwnd, kridDetailsPropTabBigName), m_ppropd->GetName());

	::SetWindowText(::GetDlgItem(m_hwnd, kctidDetailsPropTabAbbr), m_ppropd->GetAbbr());
	::SetWindowText(::GetDlgItem(m_hwnd, kctidDetailsPropTabHelpF), m_ppropd->GetHelpFile());

	::SendDlgItemMessage(m_hwnd, kctidDetailsPropTabSort, BM_SETCHECK, m_ppropd->GetSorted(), 0);
	::SendDlgItemMessage(m_hwnd, kctidDetailsPropTabDup, BM_SETCHECK, m_ppropd->GetDuplicates(), 0);
	if (m_ppropd->GetDepth() > 1)
		::SendDlgItemMessage(m_hwnd, kctidDetailsPropTabHeir, BM_SETCHECK, BST_CHECKED, 0);
	else
		::SendDlgItemMessage(m_hwnd, kctidDetailsPropTabHeir, BM_SETCHECK, BST_UNCHECKED, 0);

	// Initialize values for the Display combo box.
	HWND hwndCbo = ::GetDlgItem(m_hwnd, kctidDetailsPropTabDisp);
	StrAppBuf strb;
	strb.Load(kstidListsDetailsName);
	::SendMessage(hwndCbo, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidListsDetailsAN);
	::SendMessage(hwndCbo, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidListsDetailsAbbr);
	::SendMessage(hwndCbo, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	::SendMessage(hwndCbo, CB_SETCURSEL , (WPARAM)m_ppropd->GetDispOpt(), 0);

	// Load "Writing System" Combo Box
	hwndCbo = ::GetDlgItem(m_hwnd, kctidDetailsPropTabWS);
	strb.Load(kstidWSAnals);
	::SendMessage(hwndCbo, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidWSVerns);
	::SendMessage(hwndCbo, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidWSAnalVerns);
	::SendMessage(hwndCbo, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidWSVernAnals);
	::SendMessage(hwndCbo, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	int iWSCboSel;
	int ws = m_ppropd->GetWs();
		switch (ws)
		{
		case kwsAnals:
			iWSCboSel = 0;
			break;
		case kwsVerns:
			iWSCboSel = 1;
			break;
		case kwsAnalVerns:
			iWSCboSel = 2;
			break;
		case kwsVernAnals:
			iWSCboSel = 3;
			break;
		}
	::SendMessage(hwndCbo, CB_SETCURSEL , (WPARAM)iWSCboSel, 0);

	m_fInitialized = true;

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool DetailsPropDlgTab::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	if (m_fInitialized)
	{
		switch (pnmh->code)
		{
		case BN_CLICKED:
			{
			switch (pnmh->idFrom)
				{
				case kctidDetailsPropTabSort:
					{
					if (IsDlgButtonChecked(m_hwnd, kctidDetailsPropTabSort) == BST_CHECKED)
						m_ppropd->SetSorted(true);
					else
						m_ppropd->SetSorted(false);
					break;
					}
				case kctidDetailsPropTabDup:
					{
					if (IsDlgButtonChecked(m_hwnd, kctidDetailsPropTabDup) == BST_CHECKED)
						m_ppropd->SetDuplicates(true);
					else
						m_ppropd->SetDuplicates(false);
					break;
					}
				case kctidDetailsPropTabHeir:
					{
					if (IsDlgButtonChecked(m_hwnd, kctidDetailsPropTabHeir) == BST_CHECKED)
						m_ppropd->SetDepth(127);
					else
						m_ppropd->SetDepth(1);
					break;
					}
				case kctidDetailsPropTabBrws:
					{
					OnBrws();
					break;
					}
				}
			break;
			}
		case EN_KILLFOCUS:
			{
			switch (pnmh->idFrom)
				{
				case kctidDetailsPropTabAbbr:
					{
						StrAppBufHuge strbh;
						int cch = ::SendDlgItemMessage(m_hwnd, kctidDetailsPropTabAbbr,
							WM_GETTEXT, strbh.kcchMaxStr, (LPARAM)strbh.Chars());
						strbh.SetLength(cch);
						if (cch)
							m_ppropd->SetAbbr(strbh.Chars());
						else
							// List must have an abbreviation.
							::SetWindowText(::GetDlgItem(m_hwnd, kctidDetailsPropTabAbbr),
								m_ppropd->GetAbbr());
						break;
					}
				case kctidDetailsPropTabHelpF:
					{
						StrAppBufHuge strbh;
						int cch = ::SendDlgItemMessage(m_hwnd, kctidDetailsPropTabHelpF,
							WM_GETTEXT, strbh.kcchMaxStr, (LPARAM)strbh.Chars());
						strbh.SetLength(cch);
						m_ppropd->SetHelpFile(strbh.Chars());
						break;
					}
				}
			break;
			}
		case CBN_SELCHANGE:
			{
			switch (pnmh->idFrom)
				{
				case kctidDetailsPropTabDisp:
					{
						int icbo = ::SendDlgItemMessage(m_hwnd, kctidDetailsPropTabDisp, CB_GETCURSEL, 0, 0);
						m_ppropd->SetDispOpt(icbo);
						break;
					}
				case kctidDetailsPropTabWS:
					{
						int icbo = ::SendDlgItemMessage(m_hwnd, kctidDetailsPropTabWS, CB_GETCURSEL, 0, 0);
						switch (icbo)
						{
						case 0:
							m_ppropd->SetWs(kwsAnals);
							break;
						case 1:
							m_ppropd->SetWs(kwsVerns);
							break;
						case 2:
							m_ppropd->SetWs(kwsAnalVerns);
							break;
						case 3:
							m_ppropd->SetWs(kwsVernAnals);
							break;
						}
						break;
					}
				}
			break;
			}
		}
	}
	return AfWnd::OnNotifyChild(ctid, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	This method is called when the user presses Browse button.  It opens a dialog to browse for
	a help file, then if OK is pressed it puts that filename and path into the help edit box.
----------------------------------------------------------------------------------------------*/
void DetailsPropDlgTab::OnBrws()
{
	// Open file dialog.
	achar szFile[MAX_PATH];
	::ZeroMemory(szFile, MAX_PATH);
	OPENFILENAME ofn;
	::ZeroMemory(&ofn, sizeof(OPENFILENAME));
	// the constant below is required for compatibility with Windows 95/98 (and maybe NT4)
	ofn.lStructSize = OPENFILENAME_SIZE_VERSION_400;
	ofn.Flags		= OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_HIDEREADONLY;
	ofn.hwndOwner	= m_hwnd;
	ofn.lpstrFilter	= _T("Compiled HTML Help Files (*.chm)\0*.chm\0");
	StrApp str(kstidOpenHelp);
	ofn.lpstrTitle	= str.Chars();

	StrApp strHelpPath = AfApp::Papp()->GetFwCodePath().Chars();
	strHelpPath.Append(_T("\\Helps\\"));

	ofn.lpstrInitialDir = strHelpPath.Chars();
	ofn.lpstrFile	= szFile;
	ofn.nMaxFile	= MAX_PATH;
	if (IDOK != ::GetOpenFileName(&ofn))
		return; // We do not save the results
	::SetWindowText(::GetDlgItem(m_hwnd, kctidDetailsPropTabHelpF), szFile);
	m_ppropd->SetHelpFile(szFile);
}


/*----------------------------------------------------------------------------------------------
	This method is called when the user selects OK or Apply from the parent dialog, or the
	parent dialog wants to persist the changes in this dialog.

	@return true.
----------------------------------------------------------------------------------------------*/
bool DetailsPropDlgTab::Apply()
{
	return true;
}

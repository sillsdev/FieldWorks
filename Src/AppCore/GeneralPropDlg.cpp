/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GeneralPropDlg.cpp
Responsibility: Steve McConnel (Sharon Correll?)
Last reviewed: Not yet.

Description:
	Implementation of the File / Language Project Properties dialog classes.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE
//:End Ignore

//:>********************************************************************************************
//:>	PropertiesDlg Implementation
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Process notifications for this dialog from some event on a control.  This method is called
	by the framework.

	@param ctid Id of the control that issued the windows command.
	@param pnmh Windows command that is being passed.
	@param lnRet return value to be returned to the windows command.
	@return true if command is handled.
	See ${AfWnd#OnNotifyChild}
----------------------------------------------------------------------------------------------*/
bool PropertiesDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	int itab;

	switch (pnmh->code)
	{
	case TCN_SELCHANGE:
		// Make sure we can move to the current tab.
		itab = TabCtrl_GetCurSel(m_hwndTab);
		Assert((uint)itab < (uint)m_vqdlgv.Size());
		ShowChildDlg(itab);
		return true;

	case TCN_SELCHANGING:
		// Make sure that we can move off of the current tab.
		itab = TabCtrl_GetCurSel(m_hwndTab);
		Assert((uint)itab < (uint)m_vqdlgv.Size());
		lnRet = !m_vqdlgv[itab]->QueryClose(AfDialogView::kqctChange);
		return true;
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Switch to a different dialog tab.

	@param itab index of the dialog to swich to.
	@return true
----------------------------------------------------------------------------------------------*/
bool PropertiesDlg::ShowChildDlg(int itab)
{
	Assert((uint)itab < (uint)m_vqdlgv.Size());
	AssertPtr(m_vqdlgv[itab]);

	if (m_itabCurrent == itab)
	{
		// We already have the tab selected, so we can return without doing anything.
		return true;
	}

	if (!m_vqdlgv[itab]->Hwnd())
	{
		HWND hwndFocus = ::GetFocus();

		// This is the first time this tab has been selected, and the dialog has not
		// been created yet, so create it now.
		m_vqdlgv[itab]->DoModeless(m_hwnd);

		// This is needed so the new dialog has the correct z-order in the parent dialog.
		::SetWindowPos(m_vqdlgv[itab]->Hwnd(), NULL, m_dxsClient, m_dysClient, 0, 0,
			SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

		// If the focus was on the tab control, Windows moves the focus to the
		// new dialog, so set it back to the tab control.
		if (hwndFocus == m_hwndTab)
			::SetFocus(m_hwndTab);
	}

	bool fRet = m_vqdlgv[itab]->SetActive();
	if (fRet)
	{
		// Show the new dialog view and hide the old one.
		::ShowWindow(m_vqdlgv[itab]->Hwnd(), SW_SHOW);
		if (m_itabCurrent != -1)
			::ShowWindow(m_vqdlgv[m_itabCurrent]->Hwnd(), SW_HIDE);

		m_itabCurrent = itab;
	}

	TabCtrl_SetCurSel(m_hwndTab, m_itabCurrent);
	return fRet;

}

/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the OK or the Apply Now button.
	When the framework calls this method, changes are made in the dialog are accepted if the
	return value is true.

	@param fClose not used here
	@return true if Successful
----------------------------------------------------------------------------------------------*/
bool PropertiesDlg::OnApply(bool fClose)
{
	for (int idlgv = 0; idlgv < m_vqdlgv.Size(); idlgv++)
	{
		Assert(m_vqdlgv[idlgv]);
		if (m_vqdlgv[idlgv]->Hwnd() && !m_vqdlgv[idlgv]->Apply())
			return false;
	}

	AfApp::Papp()->EnableMainWindows(true);
	return AfDialog::OnApply(fClose);
}


/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the Cancel button.
	When the framework calls this method, changes made in the dialog are not accepted.

	@return true if Successful
----------------------------------------------------------------------------------------------*/
bool PropertiesDlg::OnCancel()
{
	if (m_vqdlgv.Size())
	{
		for (int idlgv = 0; idlgv < m_vqdlgv.Size(); idlgv++)
		{
			if (m_vqdlgv[idlgv]->Hwnd())
				m_vqdlgv[idlgv]->Cancel();
		}
	}

	AfApp::Papp()->EnableMainWindows(true);
	return AfDialog::OnCancel();
}



//:>********************************************************************************************
//:>	FwPropDlg Implementation
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwPropDlg::FwPropDlg()
{
	m_rid = kridLangProjPropDlg;
	m_pszHelpUrl = NULL;
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
bool FwPropDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	StrApp str;
	str.Format(_T("%r %r"), AfApp::Papp()->GetAppPropNameId(), kstidPropProperties);
	::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)str.Chars());

	m_pszHelpUrl = m_strHelpF.Chars();
	GeneralPropDlgTabPtr qgenp;
	qgenp.Attach(NewObj GeneralPropDlgTab(this, m_ctidName));
	qgenp->EnableLocation(true);
	qgenp->EnableSize(true);
	qgenp->EnableModified(true);
	qgenp->EnableDescription(true);
	AfDialogViewPtr qdlgv;
	qdlgv = qgenp;
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
	TabCtrl_InsertItem(m_hwndTab, 0, &ti);

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

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}



//:>********************************************************************************************
//:>	GeneralPropDlgTab Implementation
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
GeneralPropDlgTab::GeneralPropDlgTab(PropertiesDlg * ppropd, unsigned int ctidName)
{
	m_rid = kridGeneralPropTabDlg;
	m_ppropd = ppropd;
	m_hfontLarge = NULL;
	m_fInitialized = false;
	m_ctidName = ctidName;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
GeneralPropDlgTab::~GeneralPropDlgTab()
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
bool GeneralPropDlgTab::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	HICON hicon = m_ppropd->GetIconHandle();
	if (hicon)
	{
		::SendMessage(::GetDlgItem(m_hwnd, kridGeneralPropTabObjIcon), STM_SETICON,
			(WPARAM)hicon, (LPARAM)0);
	}

	m_hfontLarge = AfGdi::CreateFont(16, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
	{
		::SendMessage(::GetDlgItem(m_hwnd, kridGeneralPropTabBigName), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	}
	::SetWindowText(::GetDlgItem(m_hwnd, kridGeneralPropTabBigName), m_ppropd->GetName());

	// Replace the kctidGeneralPropTabName editbox with a bespoke version, so that the
	// "what's this" help will work differently for the different circumstances:
	HWND hwndEdit = ::GetDlgItem(m_hwnd, kctidGeneralPropTabName);
	Rect rc;
	// Get the current edit box's size, font, previous sibling, style and extended style:
	::GetWindowRect(hwndEdit, &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);
	HFONT hfontEdit = (HFONT)(::SendMessage(hwndEdit, WM_GETFONT, 0, 0));
	HWND hwndPrev = ::GetWindow(hwndEdit, GW_HWNDPREV);
	DWORD dwStyle = ::GetWindowLong(hwndEdit, GWL_STYLE);
	DWORD dwExStyle =  ::GetWindowLong(hwndEdit, GWL_EXSTYLE);
	// Get rid of original edit box:
	::DestroyWindow(hwndEdit);
	// Put in an identical one, but with the id that we want:
	hwndEdit = ::CreateWindowEx(dwExStyle, _T("EDIT"), _T(""), dwStyle, 0, 0, 0, 0, m_hwnd,
		(HMENU)m_ctidName, NULL, NULL);
	::SetWindowPos(hwndEdit, hwndPrev, rc.left, rc.top, rc.Width(), rc.Height(), 0);
	::SendMessage(hwndEdit, WM_SETFONT, (WPARAM)hfontEdit, 0);

	::SetWindowText(hwndEdit, m_ppropd->GetName());

	const achar * pszType = m_ppropd->GetType();
	::SetWindowText(::GetDlgItem(m_hwnd, kridGeneralPropTabType), pszType);

	if (m_fLocationEnb)
	{
		const achar * pszLoc = m_ppropd->GetLocation();
		::SetWindowText(::GetDlgItem(m_hwnd, kridGeneralPropTabLocation), pszLoc);
	}
	else
	{
		::ShowWindow(::GetDlgItem(m_hwnd, kridGeneralPropTabLocationLabel), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kridGeneralPropTabLocation), SW_HIDE);
	}

	if (m_fSizeEnb)
	{
		const achar * pszSize = m_ppropd->GetSizeString();
		::SetWindowText(::GetDlgItem(m_hwnd, kridGeneralPropTabSize), pszSize);
	}
	else
	{
		::ShowWindow(::GetDlgItem(m_hwnd, kridGeneralPropTabSizeLabel), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kridGeneralPropTabSize), SW_HIDE);
	}

	StrAppBuf strb;
	GetDateString(m_ppropd->GetDateCreatedFlid(), strb);
	::SetWindowText(::GetDlgItem(m_hwnd, kridGeneralPropTabCreated), strb.Chars());

	if (m_fModifiedEnb)
	{
		GetDateString(m_ppropd->GetDateModifiedFlid(), strb);
		::SetWindowText(::GetDlgItem(m_hwnd, kridGeneralPropTabModified), strb.Chars());
	}
	else
	{
		::ShowWindow(::GetDlgItem(m_hwnd, kridGeneralPropTabModifiedLabel), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kridGeneralPropTabModified), SW_HIDE);
	}

	if (m_fDescriptionEnb)
	{
		const achar * pszDesc = m_ppropd->GetDescription();
		::SetWindowText(::GetDlgItem(m_hwnd, kctidGeneralPropTabDescription), pszDesc);
	}
	else
	{
		::ShowWindow(::GetDlgItem(m_hwnd, kridGeneralPropTabDescriptionLabel), SW_HIDE);
		::ShowWindow(::GetDlgItem(m_hwnd, kctidGeneralPropTabDescription), SW_HIDE);
	}

	m_fInitialized = true;

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Fill in the string with the given "Date Created" or "Date Modified" value.

	@param flid Field id for either "DateCreated" or "DateModified".
	@param strb Reference to the output string.
----------------------------------------------------------------------------------------------*/
void GeneralPropDlgTab::GetDateString(int flid, StrAppBuf & strb)
{
	strb.Clear();
	try
	{
		AfLpInfo * plpi = m_ppropd->GetLangProjInfo();
		AssertPtr(plpi);
		CustViewDaPtr qcvd;
		plpi->GetDataAccess(&qcvd);
		AssertPtr(qcvd);
		int64 ntim = 0;
		SilTime tim;
		SYSTEMTIME stim;
		achar rgchDate[50]; // Tuesday, August 15, 2000		mardi 15 août 2000
		achar rgchTime[50]; // 10:17:09 PM					22:20:08
		int cch;
		HVO hvo = m_ppropd->GetObjId();
		HRESULT hr;
		CheckHr(hr = qcvd->get_TimeProp(hvo, flid, &ntim));
		if (hr == S_FALSE && !ntim)
		{
			int clid = MAKECLIDFROMFLID(flid);
			AfDbInfo * pdbi = plpi->GetDbInfo();
			AssertPtr(pdbi);
			IOleDbEncapPtr qode;
			pdbi->GetDbAccess(&qode);
			AssertPtr(qode);
			IFwMetaDataCachePtr qmdc;
			pdbi->GetFwMetaDataCache(&qmdc);
			AssertPtr(qmdc);
			SmartBstr sbstrField;
			CheckHr(qmdc->GetFieldName(flid, &sbstrField));
			IOleDbCommandPtr qodc;
			CheckHr(qode->CreateCommand(&qodc));
			StrUni stu;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;

			SmartBstr sbstrClass;
			CheckHr(qmdc->GetClassName(clid, &sbstrClass));
			// Note that we need the view, not just the class table proper, in case the
			// attribute is defined on a superclass (such as CmMajorObject).
			stu.Format(L"select [%b] from [%b_] where [Id] = %d",
				sbstrField.Bstr(), sbstrClass.Bstr(), hvo);
			CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (fMoreRows)
			{
				DBTIMESTAMP dbtim;
				CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&dbtim),
					sizeof(DBTIMESTAMP), &cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
				{
					stim.wYear = (unsigned short)dbtim.year;
					stim.wMonth = (unsigned short)dbtim.month;
					stim.wDayOfWeek = 0;
					stim.wDay = (unsigned short)dbtim.day;
					stim.wHour = (unsigned short)dbtim.hour;
					stim.wMinute = (unsigned short)dbtim.minute;
					stim.wSecond = (unsigned short)dbtim.second;
					stim.wMilliseconds = (unsigned short)(dbtim.fraction/1000000);
					cch = ::GetDateFormat(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &stim, NULL,
						rgchDate, 50);
					rgchDate[cch] = 0;
					cch = ::GetTimeFormat(LOCALE_USER_DEFAULT, NULL, &stim, NULL,
						rgchTime, 50);
					rgchTime[cch] = 0;
					strb.Format(_T("%s %s"), rgchDate, rgchTime);
				}
			}

		}
		else if (ntim)
		{
			tim = ntim;
			// Convert the date to a system date.
			// Then format it to a time based on the current user locale.
			stim.wYear = (unsigned short)tim.Year();
			stim.wMonth = (unsigned short)tim.Month();
			stim.wDayOfWeek = (unsigned short)tim.WeekDay();
			stim.wDay = (unsigned short)tim.Date();
			stim.wHour = (unsigned short)tim.Hour();
			stim.wMinute = (unsigned short)tim.Minute();
			stim.wSecond = (unsigned short)tim.Second();
			stim.wMilliseconds = (unsigned short)tim.MilliSecond();
			cch = ::GetDateFormat(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &stim, NULL, rgchDate,
				50);
			rgchDate[cch] = 0;
			cch = ::GetTimeFormat(LOCALE_USER_DEFAULT, NULL, &stim, NULL, rgchTime, 50);
			rgchTime[cch] = 0;
			strb.Format(_T("%s %s"), rgchDate, rgchTime);
		}
	}
	catch (...)	// Was empty.
	{
		throw;	// For now we have nothing to add, so pass it on up.
	}
}

/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool GeneralPropDlgTab::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	if (m_fInitialized)
	{
		switch (pnmh->code)
		{
		case EN_KILLFOCUS:
			{
				if (pnmh->idFrom == m_ctidName)
				{
					StrAppBufHuge strbh;
					int cch = ::SendDlgItemMessage(m_hwnd, m_ctidName, WM_GETTEXT,
						strbh.kcchMaxStr, (LPARAM)strbh.Chars());
					strbh.SetLength(cch);
					if (cch)
					{
						FixString(strbh);
						::SetWindowText(::GetDlgItem(m_hwnd, m_ctidName), strbh.Chars());
						if (!m_ppropd->CheckName(strbh.Chars()))
						{
							StrApp strA(kstidTlsLstsNameExist);
							StrApp strB(kstidTlsLstsNameExistC);
							::MessageBox(m_hwnd, strA.Chars(), strB.Chars(),
								MB_OK | MB_ICONINFORMATION);
							::SetFocus(::GetDlgItem(m_hwnd, m_ctidName));
							return false;
						}
						m_ppropd->SetName(strbh.Chars());
					}
					else
						// List must have a name.
						::SetWindowText(::GetDlgItem(m_hwnd, m_ctidName),
							m_ppropd->GetName());
				}
				else if (pnmh->idFrom == kctidGeneralPropTabDescription)
				{
					StrAppBufHuge strbh;
					int cch = ::SendDlgItemMessage(m_hwnd, kctidGeneralPropTabDescription,
						WM_GETTEXT, strbh.kcchMaxStr, (LPARAM)strbh.Chars());
					strbh.SetLength(cch);
					FixString(strbh);
					::SetWindowText(::GetDlgItem(m_hwnd, kctidGeneralPropTabDescription),
						strbh.Chars());
					m_ppropd->SetDescription(strbh.Chars());
				}
			}
			break;
		}
	}
	return AfWnd::OnNotifyChild(ctid, pnmh, lnRet);
}
/*----------------------------------------------------------------------------------------------
	Replaces any carriage returns, linefeeds, or tabs with a space, then trim leading and
	trailing spaces.
----------------------------------------------------------------------------------------------*/
void GeneralPropDlgTab::FixString(StrAppBufHuge & strbh)
{
	int ich = strbh.FindStr(_T("\r\n"));
	while (ich > -1)
	{
		strbh.Replace(ich, ich + 2, " ");
		ich = strbh.FindStr(_T("\r\n"));
	}

	ich = strbh.FindStr(_T("\n"));
	while (ich > -1)
	{
		strbh.Replace(ich, ich + 1, " ");
		ich = strbh.FindStr(_T("\n"));
	}

	ich = strbh.FindStr(_T("\t"));
	while (ich > -1)
	{
		strbh.Replace(ich, ich + 1, " ");
		ich = strbh.FindStr(_T("\t"));
	}

	while (strbh.Left(1) == _T(" "))
	{
		strbh = strbh.Right(strbh.Length() - 1);
	}

	while (strbh.Right(1) == _T(" "))
	{
		strbh = strbh.Left(strbh.Length() - 1);
	}
}

/*----------------------------------------------------------------------------------------------
	This method is called when the user selects OK or Apply from the parent dialog, or the
	parent dialog wants to persist the changes in this dialog.

	@return true.
----------------------------------------------------------------------------------------------*/
bool GeneralPropDlgTab::Apply()
{
	if (!m_fInitialized)
		return true;

	Vector<achar> vch;
	HWND hwnd;
	int cch;

	// Get the name and store it.  It must not be empty.
	hwnd = ::GetDlgItem(m_hwnd, m_ctidName);
	cch = ::GetWindowTextLength(hwnd);
	if (cch)
	{
		vch.Resize(cch + 1);
		::GetWindowText(hwnd, vch.Begin(), cch + 1);
		m_ppropd->SetName(vch.Begin());
	}
	else
	{
		return false;		// The project must have a name!
	}

	// Get the description and store it.  It may be empty.
	hwnd = ::GetDlgItem(m_hwnd, kctidGeneralPropTabDescription);
	cch = ::GetWindowTextLength(hwnd);
	if (cch)
	{
		vch.Resize(cch + 1);
		::GetWindowText(hwnd, vch.Begin(), cch + 1);
		m_ppropd->SetDescription(vch.Begin());
	}

	return true;
}

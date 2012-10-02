/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpOptionsDlg.cpp
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Implements the behavior of the options dialog.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:End Ignore

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
WpOptionsDlg::WpOptionsDlg()
{
	m_rid = kridOptionsDlg;
	m_pszHelpUrl = _T("User_Interface\\Menus\\Tools\\Options\\Options_overview.htm");
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
WpOptionsDlg::~WpOptionsDlg()
{
}

/*----------------------------------------------------------------------------------------------
	Initialize dialog, and its controls.
----------------------------------------------------------------------------------------------*/
bool WpOptionsDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Subclass the Help button to show the standard icon.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);
	WpApp * pwpapp = dynamic_cast<WpApp *>(AfApp::Papp());
	Assert(pwpapp);

	m_fLogicalArrow = pwpapp->LogicalArrow();
//	m_fLogicalShiftArrow = pwpapp->LogicalShiftArrow();
//	m_fLogicalHomeEnd = pwpapp->LogicalHomeEnd();
	m_fGraphiteLog = pwpapp->GraphiteLogging();

	HWND hwndVis = ::GetDlgItem(m_hwnd, kctidArrVis);
	HWND hwndLog = ::GetDlgItem(m_hwnd, kctidArrLog);
	if (m_fLogicalArrow)
	{
		::SendMessage(hwndVis, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);
		::SendMessage(hwndLog, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
	}
	else
	{
		::SendMessage(hwndVis, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
		::SendMessage(hwndLog, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);
	}

#if 0
	hwndVis = ::GetDlgItem(m_hwnd, kctidShArrVis);
	hwndLog = ::GetDlgItem(m_hwnd, kctidShArrLog);
	if (m_fLogicalShiftArrow)
	{
		::SendMessage(hwndVis, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);
		::SendMessage(hwndLog, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
	}
	else
	{
		::SendMessage(hwndVis, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
		::SendMessage(hwndLog, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);
	}

	hwndVis = ::GetDlgItem(m_hwnd, kctidHomeVis);
	hwndLog = ::GetDlgItem(m_hwnd, kctidHomeLog);
	if (m_fLogicalHomeEnd)
	{
		::SendMessage(hwndVis, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);
		::SendMessage(hwndLog, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
	}
	else
	{
		::SendMessage(hwndVis, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
		::SendMessage(hwndLog, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);
	}
#endif

	HWND hwndGrLog = ::GetDlgItem(m_hwnd, kctidGraphiteLog);
	if (m_fGraphiteLog)
		::SendMessage(hwndGrLog, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
	else
		::SendMessage(hwndGrLog, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle the OK button.
----------------------------------------------------------------------------------------------*/
bool WpOptionsDlg::OnApply(bool fClose)
{
	return SuperClass::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Handle the Cancel button.
----------------------------------------------------------------------------------------------*/
bool WpOptionsDlg::OnCancel()
{
	return SuperClass::OnCancel();
}

/*----------------------------------------------------------------------------------------------
	Handle the behavior of various controls.
----------------------------------------------------------------------------------------------*/
bool WpOptionsDlg::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	HWND hwndVis;
	HWND hwndLog;
	HWND hwndGrLog = ::GetDlgItem(m_hwnd, kctidGraphiteLog);

	int nVis, nLog, nChecked;

	switch (id)
	{
	case kctidArrVis:
	case kctidArrLog:
		if (pnmh->code == BN_CLICKED)
		{
			m_fLogicalArrow = (id == kctidArrLog);
			hwndVis = ::GetDlgItem(m_hwnd, kctidArrVis);
			hwndLog = ::GetDlgItem(m_hwnd, kctidArrLog);
			nVis = (id == kctidArrVis) ? BST_CHECKED : BST_UNCHECKED;
			nLog = (id == kctidArrLog) ? BST_CHECKED : BST_UNCHECKED;
			::SendMessage(hwndVis, BM_SETCHECK, (WPARAM)nVis, 0);
			::SendMessage(hwndLog, BM_SETCHECK, (WPARAM)nLog, 0);
		}
		break;

#if 0
	case kctidShArrVis:
	case kctidShArrLog:
		if (pnmh->code == BN_CLICKED)
		{
			m_fLogicalShiftArrow = (id == kctidShArrLog);
			hwndVis = ::GetDlgItem(m_hwnd, kctidShArrVis);
			hwndLog = ::GetDlgItem(m_hwnd, kctidShArrLog);
			nVis = (id == kctidShArrVis) ? BST_CHECKED : BST_UNCHECKED;
			nLog = (id == kctidShArrLog) ? BST_CHECKED : BST_UNCHECKED;
			::SendMessage(hwndVis, BM_SETCHECK, (WPARAM)nVis, 0);
			::SendMessage(hwndLog, BM_SETCHECK, (WPARAM)nLog, 0);
		}
		break;

	case kctidHomeVis:
	case kctidHomeLog:
		if (pnmh->code == BN_CLICKED)
		{
			m_fLogicalHomeEnd = (id == kctidHomeLog);
			hwndVis = ::GetDlgItem(m_hwnd, kctidHomeVis);
			hwndLog = ::GetDlgItem(m_hwnd, kctidHomeLog);
			nVis = (id == kctidHomeVis) ? BST_CHECKED : BST_UNCHECKED;
			nLog = (id == kctidHomeLog) ? BST_CHECKED : BST_UNCHECKED;
			::SendMessage(hwndVis, BM_SETCHECK, (WPARAM)nVis, 0);
			::SendMessage(hwndLog, BM_SETCHECK, (WPARAM)nLog, 0);
		}
		break;
#endif

	case kctidGraphiteLog:
		if (pnmh->code == BN_CLICKED)
		{
			nChecked = ::SendMessage(hwndGrLog, BM_GETCHECK, 0, 0);
			m_fGraphiteLog = (nChecked == BST_CHECKED);
		}
		break;

	default:
		break;
	}

	return SuperClass::OnNotifyChild(id, pnmh, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Update the options flags in the application.
----------------------------------------------------------------------------------------------*/
void WpOptionsDlg::ModifyAppFlags(WpApp * pwpapp)
{
	pwpapp->SetLogicalArrow(m_fLogicalArrow);
	pwpapp->SetLogicalShiftArrow(m_fLogicalArrow);		// was m_fLogicalShiftArrow
	pwpapp->SetLogicalHomeEnd(m_fLogicalArrow);			// was m_fLogicalHomeEnd
	pwpapp->SetGraphiteLogging(m_fGraphiteLog);
}

/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpDocDlg.cpp
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Implements the behavior of the document dialog.
	THIS CLASS IS CURRENTLY NOT BEING USED.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:End Ignore

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
WpDocDlg::WpDocDlg()
{
	m_rid = kridDocDlg;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
WpDocDlg::~WpDocDlg()
{
}

/*----------------------------------------------------------------------------------------------
	Initialize dialog, and its controls.
----------------------------------------------------------------------------------------------*/
bool WpDocDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	int nRtl;
	CheckHr(m_qda->get_IntProp(khvoText, kflidStText_RightToLeft, &nRtl));
	m_fRtl = (bool)nRtl;

	HWND hwndLtr = ::GetDlgItem(m_hwnd, kctidDocLtr);
	HWND hwndRtl = ::GetDlgItem(m_hwnd, kctidDocRtl);
	if (m_fRtl)
	{
		::SendMessage(hwndLtr, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);
		::SendMessage(hwndRtl, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
	}
	else
	{
		::SendMessage(hwndLtr, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
		::SendMessage(hwndRtl, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);
	}

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle the OK button.
----------------------------------------------------------------------------------------------*/
bool WpDocDlg::OnApply(bool fClose)
{
	StrUni stuUndo = L"Undo Document Direction";
	StrUni stuRedo = L"Redo Document Direction";
	CheckHr(m_qda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));

	CheckHr(m_qda->SetInt(khvoText, kflidStText_RightToLeft, (int)m_fRtl));

	CheckHr(m_qda->EndUndoTask());

	return SuperClass::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Handle the Cancel button.
----------------------------------------------------------------------------------------------*/
bool WpDocDlg::OnCancel()
{
	return SuperClass::OnCancel();
}

/*----------------------------------------------------------------------------------------------
	Handle the behavior of various controls.
----------------------------------------------------------------------------------------------*/
bool WpDocDlg::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	HWND hwndLtr = ::GetDlgItem(m_hwnd, kctidDocLtr);
	HWND hwndRtl = ::GetDlgItem(m_hwnd, kctidDocRtl);

	int nLtr, nRtl;

	switch (id)
	{
	case kctidDocLtr:
	case kctidDocRtl:
		if (pnmh->code == BN_CLICKED)
		{
			m_fRtl = (id == kctidDocRtl);
			nLtr = (id == kctidDocLtr) ? BST_CHECKED : BST_UNCHECKED;
			nRtl = (id == kctidDocRtl) ? BST_CHECKED : BST_UNCHECKED;
			::SendMessage(hwndLtr, BM_SETCHECK, (WPARAM)nLtr, 0);
			::SendMessage(hwndRtl, BM_SETCHECK, (WPARAM)nRtl, 0);
		}
		break;

	default:
		break;
	}

	return SuperClass::OnNotifyChild(id, pnmh, lnRet);
}

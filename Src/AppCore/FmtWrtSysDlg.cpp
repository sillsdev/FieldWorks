/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FmtWrtSysDlg.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implements the Format / Writing System Dialog class.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE
//:End Ignore

//:>********************************************************************************************
//:>	Format - Writing Systems dialog
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FmtWrtSysDlg::FmtWrtSysDlg()
{
	m_rid = kridFmtWrtSysDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/Format/Writing_System.htm");
	m_wsInit = -1;
	m_iwsInit = -1;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FmtWrtSysDlg::~FmtWrtSysDlg()
{
}

/*----------------------------------------------------------------------------------------------
	Initialize dialog, and its controls.
----------------------------------------------------------------------------------------------*/
bool FmtWrtSysDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	InitEncList();

	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	HWND hwndEncs = ::GetDlgItem(m_hwnd, kctidFmtWritingSystems);
	// Fill the list up with writing system strings.
	achar rgch[50];
	for (int iws = 0; iws < m_vws.Size(); ++iws)
	{
		_tcsncpy_s(rgch, m_vstr[iws].Chars(), 50);
		::SendMessage(hwndEncs, LB_ADDSTRING, 0, (LPARAM)rgch);
		if (m_vws[iws] == m_wsInit)
			m_iwsInit = iws;
	}

	::SendMessage(hwndEncs, LB_SETCURSEL, (WPARAM)m_iwsInit, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle the OK (Apply) button.
----------------------------------------------------------------------------------------------*/
bool FmtWrtSysDlg::OnApply(bool fClose)
{
	HWND hwndEncs = ::GetDlgItem(m_hwnd, kctidFmtWritingSystems);
	int iwsSel = ::SendMessage(hwndEncs, LB_GETCURSEL, 0, 0);
	if (iwsSel >= 0)
		m_wsSel = m_vws[iwsSel];
	else
		m_wsSel = -1;
	return SuperClass::OnApply(fClose);

}

/*----------------------------------------------------------------------------------------------
	Handle the Cancel button.
----------------------------------------------------------------------------------------------*/
bool FmtWrtSysDlg::OnCancel()
{
	return SuperClass::OnCancel();
}

/*----------------------------------------------------------------------------------------------
	Handle the behavior of various controls.
	Currently nothing special to do.
----------------------------------------------------------------------------------------------*/
#if 0
bool FmtWrtSysDlg::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	HWND hwndEncs = ::GetDlgItem(m_hwnd, kctidFmtWritingSystems);
	HWND hwndOk = ::GetDlgItem(m_hwnd, kctidOk);
	HWND hwndCancel = ::GetDlgItem(m_hwnd, kctidCancel);
	int iwsSel = ::SendMessage(hwndEncs, LB_GETCURSEL, 0, 0);

	switch (id)
	{
	case kctidFmtWritingSystems:
		switch (pnmh->code)
		{
		case LBN_SELCHANGE:
			break;
		default:
			break;
		}
		break;

	case kctidCancel:
		break;

	default:
		break;
	}

	return SuperClass::OnNotifyChild(id, pnmh, lnRet);
}
#endif


/*----------------------------------------------------------------------------------------------
	Initialize the list of encodings.
----------------------------------------------------------------------------------------------*/
void FmtWrtSysDlg::InitEncList()
{
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	IVwRootBoxPtr qrootb = pafw->GetActiveRootBox();
	ISilDataAccessPtr qsda;
	if (qrootb)
		qrootb->get_DataAccess(&qsda);
	int rgenc[30];
	int cws = 0;
	if (qsda)
		qsda->get_WritingSystemsOfInterest(30, rgenc, &cws);

	// Get the appropropriate writing system factory.
	ILgWritingSystemFactoryPtr qwsf;
	pafw->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);

	for (int iws = 0; iws < cws; ++iws)
	{
		int ws = rgenc[iws];
		IWritingSystemPtr qws;
		CheckHr(qwsf->get_EngineOrNull(ws, &qws));
		if (!qws)
			continue;
		//	Generate the name to use for the writing system.
		SmartBstr sbstr;
		CheckHr(qws->get_UiName(pafw->UserWs(), &sbstr));
		if (!sbstr)
			continue;
		StrApp str(sbstr.Chars());
		// Add this writing system sorted by name.
		int iv;
		int ivLim;
		for (iv = 0, ivLim = m_vstr.Size(); iv < ivLim; )
		{
			int ivMid = (iv + ivLim) / 2;
			if (m_vstr[ivMid] < str)
				iv = ivMid + 1;
			else
				ivLim = ivMid;
		}
		m_vws.Insert(iv, ws);
		m_vstr.Insert(iv, str);
		Assert(m_vws.Size() == m_vstr.Size());
	}
}

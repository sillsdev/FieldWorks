/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpWrSysDlg.cpp
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Implements the behavior of the dialog to allow selecting of a old writing system and
	applying it to the current text.

	TODO: Delete this file, as it has been made obsolete by moving the functionality to
	FmtWrtSysDlg.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:End Ignore


#if 0 // replaced by FmtWrtSysDlg in AppCore

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
WpFormatWsDlg::WpFormatWsDlg()
{
	m_rid = kridWpFormatWsDlg;
	m_wsInit = -1;
	m_iwsInit = -1;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
WpFormatWsDlg::~WpFormatWsDlg()
{
}

/*----------------------------------------------------------------------------------------------
	Initialize dialog, and its controls.
----------------------------------------------------------------------------------------------*/
bool WpFormatWsDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	InitEncList();

	HWND hwndEncs = ::GetDlgItem(m_hwnd, kctidWritingSystems);
	// Fill the list up with writing system strings.
	for (int iws = 0; iws < m_vws.Size(); ++iws)
		::SendMessage(hwndEncs, LB_ADDSTRING, 0, (LPARAM)m_vstr[iws].Chars());

	::SendMessage(hwndEncs, LB_SETCURSEL, (WPARAM)m_iwsInit, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle the OK (Apply) button.
----------------------------------------------------------------------------------------------*/
bool WpFormatWsDlg::OnApply(bool fClose)
{
	HWND hwndEncs = ::GetDlgItem(m_hwnd, kctidWritingSystems);
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
bool WpFormatWsDlg::OnCancel()
{
	return SuperClass::OnCancel();
}

/*----------------------------------------------------------------------------------------------
	Handle the behavior of various controls.
	Currently nothing special to do.
----------------------------------------------------------------------------------------------*/
#if 0
bool WpFormatWsDlg::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	HWND hwndEncs = ::GetDlgItem(m_hwnd, kctidWritingSystems);
	HWND hwndOk = ::GetDlgItem(m_hwnd, kctidOk);
	HWND hwndCancel = ::GetDlgItem(m_hwnd, kctidCancel);
	int iwsSel = ::SendMessage(hwndEncs, LB_GETCURSEL, 0, 0);

	switch (id)
	{
	case kctidWritingSystems:
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
	Initialize the list of encodings from the writing system factory.
----------------------------------------------------------------------------------------------*/
void WpFormatWsDlg::InitEncList()
{
	int cws;
	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
	CheckHr(qwsf->get_NumberOfWs(&cws));
	int * prgenc = NewObj int[cws];
	CheckHr(qwsf->GetWritingSystems(prgenc));
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));

	for (int iws = 0; iws < cws; iws++)
	{
		if (prgenc[iws] == 0)
			continue;

		IWritingSystemPtr qws;
		CheckHr(qwsf->get_Engine(prgenc[iws], &qws));
		if (!qws)
			continue;

		//	Generate the name to use for the writing system.
		SmartBstr sbstr;
		CheckHr(qws->get_UiName(wsUser, &sbstr));
		if (!sbstr)
			continue;
		StrUni stu(sbstr.Chars());
		StrApp str = stu;

		m_vws.Push(prgenc[iws]);
		m_vstr.Push(str);
	}
	Assert(m_vws.Size() == m_vstr.Size());

	//	Sort the encodings by name.
	for (iws = 0; iws < m_vws.Size() - 1; iws++)
	{
		for (int iws2 = iws + 1; iws2 < m_vws.Size(); iws2++)
		{
			if (m_vstr[iws] > m_vstr[iws2])
			{
				StrApp strTmp = m_vstr[iws];
				int encTmp = m_vws[iws];
				m_vstr[iws] = m_vstr[iws2];
				m_vws[iws] = m_vws[iws2];
				m_vstr[iws2] = strTmp;
				m_vws[iws2] = encTmp;
			}
		}
		if (m_vws[iws] == m_wsInit)
			m_iwsInit = iws;
	}

	delete[] prgenc;
}

#endif // 0

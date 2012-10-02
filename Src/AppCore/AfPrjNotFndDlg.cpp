/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfPrjNotFndDlg.cpp
Responsibility: John Landon
Last reviewed: Not yet.

Description:
	Implementation of the AfPrjNotFndDlg class.
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
AfPrjNotFndDlg::AfPrjNotFndDlg()
{
	m_rid = kridAfPrjNotFndDlg;
//	m_pszHelpUrl = "";
	m_hfontLarge = NULL;
}


/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfPrjNotFndDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Display the "Information" icon.
	HICON hicon = ::LoadIcon(NULL, MAKEINTRESOURCE(IDI_WARNING));
	if (hicon)
	{
		::SendMessage(::GetDlgItem(m_hwnd, kridPrjNotFndIcon), STM_SETICON, (WPARAM)hicon,
			(LPARAM)0);
	}

	// Set the font for the header, and display the header.
	m_hfontLarge = AfGdi::CreateFont(16, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
		::SendMessage(::GetDlgItem(m_hwnd, kridPrjNotFndHeader), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	StrApp strFmt(kstidPrjNotFndHeaderFmt);
	StrApp str;
	str.Format(strFmt.Chars(), m_strProj.Chars());
	::SetWindowText(::GetDlgItem(m_hwnd, kridPrjNotFndHeader), str.Chars());

	// Temporarily disable the New button.
//	::EnableWindow(::GetDlgItem(m_hwnd, kctidPrjNotFndNew), FALSE);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool AfPrjNotFndDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctidFrom)
		{
		case kctidPrjNotFndOpen:
		case kctidPrjNotFndNew:
		case kctidPrjNotFndRestore:
		case kctidPrjNotFndExit:
			::EndDialog(m_hwnd, ctidFrom);
			return true;
		}
	}
	return false;
}

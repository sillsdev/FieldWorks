/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2003 by SIL International. All rights reserved.

File: RnAnthroListDlg.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Implement the Dialog class for choosing an anthropology list for a new project.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE


//:>********************************************************************************************
//:>	RnAnthroListDlg Implementation.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnAnthroListDlg::RnAnthroListDlg()
{
	SetResourceId(kridRnAnthroCodes);
	SetHelpUrl(_T("User_Interface/Menus/File/Choose_a_List_of_Anthropology_Categories.htm"));
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
RnAnthroListDlg::~RnAnthroListDlg()
{
}

/*----------------------------------------------------------------------------------------------
	Set the basic values for displaying the dialog.
----------------------------------------------------------------------------------------------*/
void RnAnthroListDlg::SetValues(bool fHaveOCM, bool fHaveFRAME,
	const Vector<StrApp> & vstrXmlFiles, const achar * pszHelpFilename)
{
	m_fHaveOCM = fHaveOCM;
	m_fHaveFRAME = fHaveFRAME;
	m_strHelpFilename = pszHelpFilename;

	for (int istr = 0; istr < vstrXmlFiles.Size(); ++istr)
		m_vstrXmlFiles.Push(vstrXmlFiles[istr]);
}

/*----------------------------------------------------------------------------------------------
	Set the description for the dialog.
----------------------------------------------------------------------------------------------*/
void RnAnthroListDlg::SetDescription(BSTR bstrDescription)
{
	m_strDescription = bstrDescription;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return true.
----------------------------------------------------------------------------------------------*/
bool RnAnthroListDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	if (m_strDescription.Length())
		::SetDlgItemText(m_hwnd, kctidRnAnthroDescription, m_strDescription.Chars());

	int ctidSel = kctidRnAnthroBlank;
	m_nChoice = kralUserDef;
	if (!m_fHaveOCM)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidRnAnthroOCM), FALSE);
	}
	else
	{
		ctidSel = kctidRnAnthroOCM;
		m_nChoice = kralOCM;
	}
	if (!m_fHaveFRAME)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidRnAnthroOCMFrame), FALSE);
	}
	else
	{
		ctidSel = kctidRnAnthroOCMFrame;
		m_nChoice = kralFRAME;
	}

	// Fill in the combo box with the nonstandard initialization file basenames.
	HWND hwndOther = ::GetDlgItem(m_hwnd, kctidRnAnthroOther);
	Assert(hwndOther != NULL);
	int ie;
	for (ie = 0; ie < m_vstrXmlFiles.Size(); ++ie)
		::SendMessage(hwndOther, CB_ADDSTRING, 0, (LPARAM)m_vstrXmlFiles[ie].Chars());
	::EnableWindow(hwndOther, FALSE);

	// Set the initial choice among the radio buttons.
	::CheckRadioButton(m_hwnd, kctidRnAnthroOCMFrame, kctidRnAnthroUseOther, ctidSel);

	// If the combobox is empty, hide it, and hide the corresponding radio button.
	if (!m_vstrXmlFiles.Size())
	{
		HWND hwndUse = ::GetDlgItem(m_hwnd, kctidRnAnthroUseOther);
		::EnableWindow(hwndUse, FALSE);
		::ShowWindow(hwndUse, SW_HIDE);
		::ShowWindow(hwndOther, SW_HIDE);
	}
	else
	{
		::SendMessage(hwndOther, CB_SETCURSEL, 0, 0);
	}

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnAnthroListDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctidFrom)
		{
		case kctidRnAnthroOCMFrame:
			if (::IsDlgButtonChecked(m_hwnd, ctidFrom) == BST_CHECKED)
			{
				m_nChoice = kralFRAME;
				::EnableWindow(::GetDlgItem(m_hwnd, kctidRnAnthroOther), FALSE);
			}
			break;
		case kctidRnAnthroOCM:
			if (::IsDlgButtonChecked(m_hwnd, ctidFrom) == BST_CHECKED)
			{
				m_nChoice = kralOCM;
				::EnableWindow(::GetDlgItem(m_hwnd, kctidRnAnthroOther), FALSE);
			}
			break;
		case kctidRnAnthroBlank:
			if (::IsDlgButtonChecked(m_hwnd, ctidFrom) == BST_CHECKED)
			{
				m_nChoice = kralUserDef;
				::EnableWindow(::GetDlgItem(m_hwnd, kctidRnAnthroOther), FALSE);
			}
			break;
		case kctidRnAnthroUseOther:
			if (::IsDlgButtonChecked(m_hwnd, ctidFrom) == BST_CHECKED)
			{
				::EnableWindow(::GetDlgItem(m_hwnd, kctidRnAnthroOther), TRUE);
				int isel = ::SendMessage(::GetDlgItem(m_hwnd, kctidRnAnthroOther),
					CB_GETCURSEL, 0, 0);
				if ((unsigned)isel < (unsigned)m_vstrXmlFiles.Size())
					m_nChoice = isel;
			}
			break;
		}
		break;
	case CBN_SELCHANGE:
		if (ctidFrom == kctidRnAnthroOther)
		{
			int isel = ::SendMessage(::GetDlgItem(m_hwnd, kctidRnAnthroOther),
				CB_GETCURSEL, 0, 0);
			if ((unsigned)isel < (unsigned)m_vstrXmlFiles.Size())
				m_nChoice = isel;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	The default OnCancel closes the dialog.  This, on the other hand, does nothing!  By design,
	we don't have a Cancel button, and we want to ignore other sources of the IDCANCEL command.
----------------------------------------------------------------------------------------------*/
bool RnAnthroListDlg::OnCancel()
{
	return false;
}

// more virtual methods: implement as needed.
// void RnAnthroListDlg::HandleDlgMessages(HWND hwndDlg, WPARAM wp, LPARAM lp);
// bool RnAnthroListDlg::Synchronize(SyncInfo & sync);
// bool RnAnthroListDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
// bool RnAnthroListDlg::OnHelpInfo(HELPINFO * phi);
// bool RnAnthroListDlg::OnCommand(int cid, int nc, HWND hctl);
// bool RnAnthroListDlg::OnApply(bool fClose);

/*----------------------------------------------------------------------------------------------
	OnHelp shows the help page for the dialog (if there is one).
----------------------------------------------------------------------------------------------*/
bool RnAnthroListDlg::OnHelp()
{
	AssertPsz(m_pszHelpUrl);
	if (m_strHelpFilename.Length())
	{
		StrAppBufPath strbpHelp;
		strbpHelp.Format(_T("%s::/%s"), m_strHelpFilename.Chars(), m_pszHelpUrl);
		HtmlHelp(::GetDesktopWindow(), strbpHelp.Chars(), HH_DISPLAY_TOPIC, NULL);
		return true;
	}
	return false;
}

// bool RnAnthroListDlg::OnActivate(bool fActivating, LPARAM lp);
// void RnAnthroListDlg::DoDataExchange(AfDataExchange * padx);

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)

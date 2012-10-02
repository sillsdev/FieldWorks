/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TeStyleDlg.cpp
Responsibility: Eberhard Beilharz
Last reviewed: never

Description:
	Implementation of TE specific Style Dialog
-------------------------------------------------------------------------------*//*:End Ignore*/

#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

//:>*******************************************************************************************
//:> TeStylesDlg implementation
//:>*******************************************************************************************

/*----------------------------------------------------------------------------------------------
	Override to do special initalizations

	See ${AfStylesDlg#OnInitDlg}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool TeStylesDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	m_hwndList = ::GetDlgItem(m_hwnd, kctidTesdCbList);

	// Turn off visual styles for these controls until all the controls on the dialog
	// can handle them properly (e.g. custom controls).
	HMODULE hmod = ::LoadLibrary(L"uxtheme.dll");
	if (hmod != NULL)
	{
		typedef void (__stdcall *SetWindowThemeProc)(HWND, LPTSTR, LPTSTR);
		SetWindowThemeProc pfn = (SetWindowThemeProc)::GetProcAddress(hmod, "SetWindowTheme");
		if (pfn != NULL)
			(pfn)(m_hwndList, L"", L"");

		::FreeLibrary(hmod);
	}

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Override of ${AfStylesDialog#SetupTabs} to insert special "General" page
----------------------------------------------------------------------------------------------*/
void TeStylesDlg::SetupTabs()
{
	SuperClass::SetupTabs();

	m_rgdlgv[0].Attach(NewObj TeFmtGenDlg(this, m_nMsrSys));
}

/*----------------------------------------------------------------------------------------------
	Process notifications for this dialog from some event on a control.  This method is called
	by the framework.

	@param ctid Id of the control that issued the windows command.
	@param pnmh Windows command that is being passed.
	@param lnRet return value to be returned to the windows command.
	@return true if command is handled.
	See ${AfStylesDlg#OnNotifyChild}
----------------------------------------------------------------------------------------------*/
bool TeStylesDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	switch(pnmh->code)
	{
	case CBN_SELCHANGE:
		{	// User changes combo box to show filtered list or all styles
			// First, save the values. This is done easiest by calling UpdateTabCtrl
			UpdateTabCtrl(-1, -1);

			// force styles listbox to be refilled
			UpdateStyleList();
			// check if selected style is still shown, otherwise select first visible
			int iListItem; // Index in the listview.
			if (SkipStyle(m_istyiSelected))
			{
				// select first item in the styles list
				iListItem = 0;
			}
			else
			{
				// re-select old item
				LVFINDINFO plvfi;
				plvfi.lParam = m_istyiSelected;
				plvfi.flags = LVFI_PARAM;
				iListItem = ListView_FindItem(m_hwndStylesList, -1, &plvfi);
			}

			// select an item
			DWORD dwT = LVIS_SELECTED | LVIS_FOCUSED;
			ListView_SetItemState(m_hwndStylesList, iListItem, dwT, dwT);

			// Make sure that it is visible
			ListView_EnsureVisible(m_hwndStylesList, iListItem, false);
			lnRet = 0;
			return true;
		}
	default:
		break;
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Returns true if style should not be shown in dialog
----------------------------------------------------------------------------------------------*/
bool TeStylesDlg::SkipStyle(int iStyle) const
{
	// get current value of the list combobox
	StyleTextTypes testt = static_cast<StyleTextTypes>(
		::SendMessage(m_hwndList, CB_GETCURSEL, 0, 0));

	switch (testt)
	{
	case ksttBasic:
		if (m_vstyi[iStyle].m_nUserLevel > 0)
			return true;
		break;

	case ksttAll:
		return false;

	case ksttCustom:
		if (m_vstyi[iStyle].m_nUserLevel > m_nCustomStyleLevel)
			return true;
		break;
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Set the values for the dialog controls based
----------------------------------------------------------------------------------------------*/
void TeStylesDlg::SetDialogValues()
{
	// Fill list combobox.
	StrApp staTemp;
	::SendMessage(m_hwndList, CB_RESETCONTENT, 0, 0);
	staTemp.Load(kstidTesdListBasic);
	::SendMessage(m_hwndList, CB_ADDSTRING, 0, (LPARAM)staTemp.Chars()); // 0
	staTemp.Load(kstidTesdListAll);
	::SendMessage(m_hwndList, CB_ADDSTRING, 0, (LPARAM)staTemp.Chars()); // 1
	staTemp.Load(kstidTesdListCustom);
	::SendMessage(m_hwndList, CB_ADDSTRING, 0, (LPARAM)staTemp.Chars()); // 2

	// Select the value in the combo box that represents the selected level.
	int nSelect;
	if (m_nCustomStyleLevel == 0)
		nSelect = ksttBasic;
	else if (m_nCustomStyleLevel > 10)
		nSelect = ksttAll;
	else
		nSelect = ksttCustom;

	::SendMessage(m_hwndList, CB_SETCURSEL, nSelect, 0);

	SuperClass::SetDialogValues();
}

/*----------------------------------------------------------------------------------------------
	Deletes the selected style. Overwritten so that we can select a style that is shown.

	@return true
----------------------------------------------------------------------------------------------*/
bool TeStylesDlg::CmdDel()
{
	bool fRet = SuperClass::CmdDel();

	if (SkipStyle(m_istyiSelected))
	{
		// select first item in the styles list because selected (BasedOn) style is not visible
		DWORD dwT = LVIS_SELECTED | LVIS_FOCUSED;
		ListView_SetItemState(m_hwndStylesList, 0, dwT, dwT);

		// Make sure that it is visible
		ListView_EnsureVisible(m_hwndStylesList, 0, false);
	}

	return fRet;
}

//:>********************************************************************************************
//:>	Set methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Set the Usage for this paragraph style to be stuNewUsage, if it is different from the
	current usage and set m_fDirty to true. Return true.

	@param styi The style to be changed.
	@param stuNewUsage New usage description
	@return true
----------------------------------------------------------------------------------------------*/
bool TeStylesDlg::SetUsage(StyleInfo & styi, const StrUni & stuNewUsage)
{
	if (!styi.m_stuUsage.Equals(stuNewUsage))
	{
		styi.m_stuUsage.Assign(stuNewUsage);
		styi.m_fDirty = true;
		styi.m_fModified = true;
	}
	return true;
}

//:>*******************************************************************************************
//:> TeFmtGenDlg implementation
//:>*******************************************************************************************

/*----------------------------------------------------------------------------------------------
	The app framework calls this to initialize the dialog. All one-time initialization should
	be done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)
----------------------------------------------------------------------------------------------*/
bool TeFmtGenDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Get the window handles for our controls.
	m_hwndUsage = ::GetDlgItem(m_hwnd, kctidTeFgEdUsage); // Usage edit control
	m_hwndShortcut = ::GetDlgItem(m_hwnd, kctidFgEdShortcut); // Shortcut edit control.

	// Fix static controls that contain shortcuts so the shortcuts won't do anything if
	// the controls are disabled.
	AfStaticText::FixEnabling(m_hwnd, kctidTeFgEdUsage);
	AfStaticText::FixEnabling(m_hwnd, kctidFgEdShortcut);

	// Turn off visual styles for these controls until all the controls on the dialog
	// can handle them properly (e.g. custom controls).
	HMODULE hmod = ::LoadLibrary(L"uxtheme.dll");
	if (hmod != NULL)
	{
		typedef bool (__stdcall *themeProc)();
		typedef void (__stdcall *SetWindowThemeProc)(HWND, LPTSTR, LPTSTR);
		themeProc pfnb = (themeProc)::GetProcAddress(hmod, "IsAppThemed");
		bool fAppthemed = (pfnb != NULL ? (pfnb)() : false);
		pfnb = (themeProc)::GetProcAddress(hmod, "IsThemeActive");
		bool fThemeActive = (pfnb != NULL ? (pfnb)() : false);
		SetWindowThemeProc pfn = (SetWindowThemeProc)::GetProcAddress(hmod, "SetWindowTheme");

		if (fAppthemed && fThemeActive && pfn != NULL)
			(pfn)(m_hwndUsage, L"", L"");

		::FreeLibrary(hmod);
	}

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Set the values for the dialog controls based on the style styi.
----------------------------------------------------------------------------------------------*/
void TeFmtGenDlg::SetDialogValues(StyleInfo & styi, Vector<int> & vwsProj)
{
	SuperClass::SetDialogValues(styi, vwsProj);
	StrApp strTemp;
	// TeStylesDlg * ptesd = dynamic_cast<TeStylesDlg*>(m_pafsd);

	// Set Usage field
	::SetWindowText(m_hwndUsage, styi.m_stuUsage);
	::EnableWindow(m_hwndUsage, true);
}

/*----------------------------------------------------------------------------------------------
	Checks if a style can be used as a follow-on sytle for a given style.
----------------------------------------------------------------------------------------------*/
bool CompatibleNextStyle(StyleInfo& styleBase, StyleInfo& styleCheckForNext)
{
	// If the styles are the same style, then it is ok to follow yourself
	if (styleBase.m_hvoStyle == styleCheckForNext.m_hvoStyle)
		return true;

	// force the addition of styles that are the "next" style for a built-in style
	if (styleBase.m_fBuiltIn && styleBase.m_hvoNext == styleCheckForNext.m_hvoStyle)
		return true;

	// Make sure the styles are the same type.
	if (styleBase.m_st != styleCheckForNext.m_st)
		return false;

	// make sure the context of the styles are the same.
	if (styleBase.m_nContext != styleCheckForNext.m_nContext)
		return false;

	// If the "based on" structure is a "body" then the "next" structure can
	// not be "head".
	// REVIEW: We could not find definitions for these values in C++. If you can, please
	// use them instead.
	const int STRUCTURE_HEAD = 1;
	const int STRUCTURE_BODY = 2;

	if (styleBase.m_nStructure == STRUCTURE_BODY && styleCheckForNext.m_nStructure == STRUCTURE_HEAD)
		return false;

	// ok, it must be compatible.
	return true;
}

/*----------------------------------------------------------------------------------------------
	Overrides the LoadNextStyleCombobox in the FmtGenDlg.cpp file so that we can limit the
	styles by context.
----------------------------------------------------------------------------------------------*/
void TeFmtGenDlg::LoadNextStyleCombobox(StyleInfo & styi)
{
	TeStylesDlg * ptesd = dynamic_cast<TeStylesDlg*>(m_pafsd);
	StrApp strTemp;

	// We have to refill the Next combo box to show only styles with compatible text styles.
	// "Next" styles are compatible if they have the same Context and a compatible Structure.
	::SendMessage(m_hwndNext, CB_RESETCONTENT, 0, 0);
	for (int istyi = 0; istyi < ptesd->m_vstyi.Size(); istyi++)
	{
		StyleInfo& styiTemp = ptesd->m_vstyi[istyi];
		if (styiTemp.m_fDeleted)
			continue;
		strTemp = styiTemp.m_stuName;
			if (CompatibleNextStyle(styi, styiTemp))
				::SendMessage(m_hwndNext, CB_ADDSTRING, 0, (LPARAM)strTemp.Chars());
	}
}

/*----------------------------------------------------------------------------------------------
	Get the final values for the dialog controls, after the dialog has been closed.
----------------------------------------------------------------------------------------------*/
void TeFmtGenDlg::GetDialogValues(StyleInfo & styi, bool & fBasedOnChanged)
{
	TeStylesDlg * ptesd = dynamic_cast<TeStylesDlg*>(m_pafsd);

	TCHAR rgcha[1024];
	::GetWindowText(m_hwndUsage, rgcha, 1024);
	StrUni stuUsage = rgcha;
	ptesd->SetUsage(styi, stuUsage);

	SuperClass::GetDialogValues(styi, fBasedOnChanged);
}
/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FmtGenDlg.h
Responsibility: Larry Waswick
Last reviewed: Not yet.

Description:
Header file for the Format General Dialog class.

Sample method for presetting values, calling dialog, and getting results back.
(See FmtParaDlg.h header for example of Paragraph dialog).
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FMT_GEN_DLG_H
#define FMT_GEN_DLG_H 1

class AfStylesDlg;
class StyleInfo;

/*----------------------------------------------------------------------------------------------
	This class provides the functionality particular to the General tab for the
	Format/Styles Dialog.

	@h3{Hungarian: fgen}
----------------------------------------------------------------------------------------------*/
class FmtGenDlg : public AfDialogView
{
public:
	/*------------------------------------------------------------------------------------------
		Constructor for creating the General tab for the given AfStylesDlg.
	------------------------------------------------------------------------------------------*/
	FmtGenDlg(AfStylesDlg * pafsd, MsrSysType nMsrSys)
	{
		m_rid = kridFmtGenDlg;
		m_pszHelpUrl = _T("User_Interface/Menus/Format/Style/Style_General_tab.htm");
		m_pafsd = pafsd;
		// Dialog may set focus to another control before this dialog is initialized. If so,
		// we don't want to react to losing focus because it'll be ugly.
		m_fSuppressLossOfFocus = true;
		m_nMsrSys = nMsrSys;
	}

	bool SetActive(); // Called when the dialog becomes active.

	// Set initial values for the dialog controls, prior to displaying the dialog.
	virtual void SetDialogValues(StyleInfo & styi, Vector<int> & vwsProj);
	// Called by AfStylesDlg to change the style name.
	void SetName(StrApp & strName);
	// Get the final values for the dialog controls, and set fBasedOnChanged to say whether
	// "Based On" was changed.
	virtual void GetDialogValues(StyleInfo & styi, bool & fBasedOnChanged);
	// Retrieve the name in the editbox.
	StrApp GetName();

protected:
	// Attributes.

	// Gives the current (possibly edited) value of each property for each writing system.
	WsStyleVec m_vesi;
	MsrSysType m_nMsrSys;

#if 0
	int m_ivhvoStyleBasedOn; // The StStyle this is based on.
	int m_ivhvoStyleFollowing; // The StStyle to use for the following paragraph.
	StrApp m_strName; // Style name.
	StrApp m_strShortcut; // Shortcut key.
	StyleType m_stStyleType; // Style type (Paragraph or Character).
#endif

	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	// Process notifications from the user.
	bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	// Handle a change in a combo box.
	virtual bool OnComboChange(NMHDR * pnmh, long & lnRet);
	// Process draw messages.
	bool OnDrawChildItem(DRAWITEMSTRUCT * pdis);
	// Handle What's This? help.
	virtual bool OnHelpInfo(HELPINFO * phi);
	// update the list of styles in the "next" combobox
	virtual void LoadNextStyleCombobox(StyleInfo & styi);
	void SetNextStyleComboboxValue(StyleInfo & styi);
	// update the list of styles in the "Based On" combobox
	virtual void LoadBasedOnStyleCombobox(StyleInfo & styi);
	void SetBasedOnStyleComboboxValue(StyleInfo & styi);

	AfStylesDlg * m_pafsd; // The parent Format/Styles dialog.

	HWND m_hwndName; // Window handle for the Name edit control.
	HWND m_hwndType; // Window handle for the Type text control.
	HWND m_hwndBasedOn; // Window handle for the Based On combobox.
	HWND m_hwndNext; // Window handle for the Next combobox.
	HWND m_hwndShortcut; // Window handle for the Shortcut edit control.
	HWND m_hwndDescription; // Window handle for the Description text control.

	Vector<int> m_vwsProj;
private:
	// Replace strOldName with strNewName in the BasedOn and Next comboboxes.
	void UpdateComboboxes(StrApp & strOldName, StrApp & strNewName);
	bool m_fSuppressLossOfFocus; // suppress normal loss-of-focus handling during SetDialogValues
};

typedef GenSmartPtr<FmtGenDlg> FmtGenDlgPtr;

#endif // !FMT_GEN_DLG_H

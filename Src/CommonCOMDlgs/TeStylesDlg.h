/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TeStyleDlg.h
Responsibility: Eberhard Beilharz
Last reviewed: never

Description:
	Definition of TE specific Styles Dialog. This dialog overrides the general Style Dialog
	and adds some additional controls.

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TESTYLEDLG_INCLUDED
#define TESTYLEDLG_INCLUDED 1

/*----------------------------------------------------------------------------------------------
	The list of styles can be filtered to show basic (and in-use), all, or a custom list.
	@h3{Hungarian: testt}
----------------------------------------------------------------------------------------------*/
enum StyleTextTypes
{
	ksttBasic = 0,
	ksttAll,
	ksttCustom,
	ksttLim
};

namespace TestCmnFwDlgs
{
	class TestFwStylesDlg;
};

/*----------------------------------------------------------------------------------------------
	TE specific Format Styles dialog shell class.

	@h3{Hungarian: tesd}
----------------------------------------------------------------------------------------------*/
class TeStylesDlg: public AfStylesDlg
{
private:
	typedef AfStylesDlg SuperClass;

	friend class TeFmtGenDlg;

public:
	// C'tor
	// @param fShowAll - if true show all styles in listview instead of selecting according
	//		to text type of style
	TeStylesDlg(bool fShowAll = false)
		: m_hwndList(NULL), m_fShowAll(fShowAll)
	{
		m_rid = kridTeStyleDlg;
	}

	//:>****************************************************************************************
	//:>	Use these additional methods to set StyleInfo member variables whenever the m_fDirty
	//:>	variable needs to be set.
	//:>****************************************************************************************

	// Set the Usage for this paragraph style to be stuNewUsage and return true;
	// set m_fDirty to true.
	bool SetUsage(StyleInfo & styi, const StrUni & stuNewUsage);

protected:
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	// create the sub dialogs
	virtual void SetupTabs();
	// Process notifications from the user.
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	// Returns true if style should not be shown in dialog
	// The default implementation always returns false and so shows all styles.
	virtual bool SkipStyle(int iStyle) const;
	// Set initial values for the dialog controls, prior to displaying the dialog.
	virtual void SetDialogValues();
	// Delete the selected style in response to the Delete button being pressed.
	virtual bool CmdDel();

	HWND m_hwndList; // Handle to the list combo box
	bool m_fShowAll; // if true initially show all styles instead of selecting from text type

	friend class TestCmnFwDlgs::TestFwStylesDlg;
};

/*----------------------------------------------------------------------------------------------
	This class provides the functionality particular to the General tab for the
	TE Format/Styles Dialog.

	@h3{Hungarian: tefgen}
----------------------------------------------------------------------------------------------*/
class TeFmtGenDlg : public FmtGenDlg
{
private:
	typedef FmtGenDlg SuperClass;

public:
	/*------------------------------------------------------------------------------------------
		Constructor for creating the General tab for the given AfStylesDlg.
	------------------------------------------------------------------------------------------*/
	TeFmtGenDlg(AfStylesDlg * pafsd, MsrSysType nMsrSys)
		: FmtGenDlg(pafsd, nMsrSys), m_hwndUsage(NULL)
	{
		m_rid = kridTeFmtGenDlg;
		m_pszHelpUrl = _T("TEDialogStyleGeneralTab.htm");
	}

	virtual void LoadNextStyleCombobox(StyleInfo & styi);
	// Set initial values for the dialog controls, prior to displaying the dialog.
	virtual void SetDialogValues(StyleInfo & styi, Vector<int> & vwsProj);
	// Get the final values for the dialog controls, and set fBasedOnChanged to say whether
	// "Based On" was changed.
	virtual void GetDialogValues(StyleInfo & styi, bool & fBasedOnChanged);

protected:
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);

	HWND m_hwndUsage; // Window handle for the Usage edit control.
};

#endif // TESTYLEDLG_INCLUDED

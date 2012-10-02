/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfFindDlg.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
Header file for the Format Paragraph Dialog class.

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFFIND_DLG_H
#define AFFIND_DLG_H 1

class AfFindRepDlg;

/*----------------------------------------------------------------------------------------------
This class is used in place of TssEdit so we can receive notifications of changes to the text
in the control.
Hungarian: frdlg.
----------------------------------------------------------------------------------------------*/

class FindTssEdit : public TssEdit
{
	typedef TssEdit SuperClass;
public:
	FindTssEdit()
	{
		m_nEditable = ktptIsEditable;
		m_fShowTags = true;
	}

	void SetParent(AfFindRepDlg * pfrdlg);
	virtual bool OnChange();
	virtual bool OnSetFocus(HWND hwndOld, bool fTbControl = false);
	void GetOverlay(IVwOverlay ** ppvo);
	virtual void HandleSelectionChange(IVwSelection * pvwsel);

protected:
	AfFindRepDlg * m_pfrdlg;

	virtual void AdjustForOverlays(IVwOverlay * pvo);

};
typedef GenSmartPtr<FindTssEdit> FindTssEditPtr;
typedef Vector<VwChangeInfo> VecCi;

/*----------------------------------------------------------------------------------------------
This class provides the functionality of the Find/Replace Dialog.
Hungarian: frdlg.
----------------------------------------------------------------------------------------------*/
class AfFindRepDlg : public AfDialog
{
	typedef AfDialog SuperClass;
public:
	AfFindRepDlg();

	// Store the objects for accessing style and writing system information.
	void SetFormatSources(ISilDataAccess * psda, IVwStylesheet * pasts)
	{
		m_qsda = psda;
		m_qasts = pasts;
	}
	void SetMessageIDs(int stidNoMatches, int stidReplaceN)
	{
		m_stidNoMatches = stidNoMatches;
		m_stidReplaceN = stidReplaceN;
	}

	// Sets initial values for the dialog controls, prior to displaying the dialog.
	void SetDialogValues(IVwPattern * pxpat, AfVwRootSite * pvrs, bool fReplace, bool fOverlays);
	virtual void EditBoxChanged(FindTssEdit * pfte);
	virtual void EditBoxFocus(FindTssEdit * pfte);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	void ToggleOverlays(IVwOverlay * pvo);

//	virtual bool OnSetFocus()
//	{
		// DON'T turn on the default keyboard. We have editable fields in this dialog
		// that handle setting it to what they want. Return true so that the superclass
		// doesn't try to handle it.
//		return true;
//	}

protected:
	// Member variables

	ISilDataAccessPtr m_qsda;
	IVwStylesheetPtr m_qasts;
	IVwOverlayPtr m_qvo;

	// The control window for the find what box.
	FindTssEditPtr m_qteFindWhat;
	// The control window for the replace with box.
	FindTssEditPtr m_qteReplaceWith;
	// Whichever of them was last in focus
	FindTssEditPtr m_qteLastFocus;

	// And for the More/Less button.
	AfButtonPtr m_qbtnMoreLess;

	HWND m_hwndTab; // Handle to the tab control.

	// The thing we edit; source of most initial state. Also used to perform the Find.
	IVwPatternPtr m_qxpat;

	// True if we are showing "more" controls:
	bool m_fShowMoreControls;
	int m_dypMoreControls;  // additional size of dialog if we are showing "more" controls

	// True if the edit boxes need to be expanded to show overlay tags:
	bool m_fShowTags;
	int m_dypFind;		// additional space needed for Find box
	int m_dypReplace;	// additional space needed for Replace box

	Vector<int> m_vws;				// Writing systems being displayed in the old writing system submenu
	Vector<StrUni> m_vstuStyles;	// Styles being displayed in the styles submenu

	// Rectangle where the More/Less button appears in the resource file,
	// relative to the dialog as a whole
	Rect m_rcMoreLess;
	// Rectangle where the Replace All button appears in the resource file,
	// relative to the dialog as a whole. If Replace and ReplaceAll are hidden,
	// the More/Less button movees here.
	Rect m_rcMoreLessFind;

	// These represent the current state of the dialog.
	ITsStringPtr m_qtssFindWhat; // String to search for.
	ITsStringPtr m_qtssReplaceWith; // The string to replace with.
	typedef ComVector<ITsString> VecTss; // Hungarian vtss
	// temporarily make this static even though it will probably cause a memory leak so we
	// can test building the list before we do persistence of the list.
	static VecTss m_vtssFindWhatItems; // Items in the Find What Combo box (max of 12).
	bool m_fListResults; // True if we want the "list results" behavior
	bool m_fAllEntries; // True to search all entries
	bool m_fFieldCheck; // True to limit search to specified fields.
	typedef Vector<StrUni> VecStrUni; // Hungarian vstu
	VecStrUni m_vstuFields; // Items in the Fields Combo box.
	bool m_fMatchWholeWord; // True to force finding whole words.
	bool m_fMatchCase; // True to force exact case.
	bool m_fMatchDiacritics; // True to force exact diacritic match.
	bool m_fMatchOldWritingSystem; // True to force exact old writing system match.

	// Control enabling
	bool m_fCanWhichEntries;
	bool m_fCanSelectField;
	bool m_fReplaceTab;
	bool m_fOverlays;  // true if they might tag their search string with an overlay to look for

	// Control behavior
	// This is true if we should close the window when executing "Find Now/Next".
	// True in Find, false in Replace currently.
	bool m_fCloseOnFindNow;

	// This is set true when we first succeed in copying data from member variables
	// into the actual controls. Otherwise, the typical sequence is that SetDialogValues
	// initializes the member variables, but can't copy to the controls because they don't
	// yet have hwnds; then when a control is first created (but before FillCtls runs again)
	// something calls GetCtrls (e.g., an Enable method) and we lose the values.
	bool m_fOkToGetCtls;

	int m_atid; // identifies the special accelerator table loaded to handle edit commands.

	// Which version of the messages to give. These are currently not used by the dialog itself,
	// but it seemed like a reasonable place to store them.
	int m_stidNoMatches;
	int m_stidReplaceN;

	// The original root site where the search is going to take place. We cache it here,
	// because the system needs to treat the current edit box (in the Find dialog itself)
	// as the active root box--this is so that overlay palettes will communicate with them.
	AfVwRootSitePtr m_qvrsLast;

	//:> Protected methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnInitDlg1(HWND hwndCtrl, LPARAM lp);
	void SetReplace(bool f);
	bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnCancel();
	bool OnComboChange(NMHDR * pnmh, long & lnRet);
	virtual bool OnButtonClick(NMHDR * pnmh, long & lnRet);
	bool OnDrawChildItem(DRAWITEMSTRUCT * pdis);
	virtual void FillCtls(void);
	virtual void GetCtls(void);
	void OnFindNow();
	void OnFindMoreLess();
	void Toggle(bool & fVal, int cid);
	virtual void EnableMoreControls();
	bool CmdFindFmtWsItems(Cmd * pcmd);
	bool CmdFindFmtStyleItems(Cmd * pcmd);
	bool CmsFindFmtWsItems(CmdState & cms);
	bool CmsFindFmtStyleItems(CmdState & cms);
	bool CmdRestoreFocus(Cmd * pcmd);
	void StoreWsAndStyleInString(FindTssEdit * pte, ITsString * ptss, ITsString ** pptssFixed);
	void SelectedWsAndStyle(int * pws, StrUni * pstuStyle);
	void LoadWindowPosition();
	void SaveWindowPosition();

	virtual void OnReleasePtr();
	virtual void SetDialogValues1(IVwPattern * pxpat, AfVwRootSite * pvrs);
	virtual void EnableDynamicControls();
	void OnFindReplace();
	void OnFindReplaceAll();
	void BeginUndoTask(ISilDataAccess * psda, int stid);
	bool IsReplacePossible();
	void EnableReplaceButton();

	void DoReplacement(IVwSelection * psel, ITsString * ptssRepl,
		bool fUseWs, bool fEmptySearch);
	void GetTagsToRemove(ITsString * ptssFound, bool * pfUseTags,
		Vector<StrUni> & vstuTagsToRemove);
	void GetTagsToInclude(ITsString * ptssFound, Vector<StrUni> & vstuTagsToRemove,
		Vector<StrUni> & vstuTagsToInclude);
	StrUni AddReplacementTags(Vector<StrUni> & vstuOrig, SmartBstr sbstrRepl);

	AfVwRootSite * RootSite();
	AfMainWnd * MainWindow();

	IVwSearchKillerPtr m_qxserkl;
	bool m_fBusy;

	friend class DisplayBusy;
	class DisplayBusy
	{
	public:
		DisplayBusy(AfFindRepDlg * frdlg);
		~DisplayBusy();
	protected:
		AfFindRepDlg * m_frdlg;
	};

	CMD_MAP_DEC(AfFindRepDlg);
};
typedef GenSmartPtr<AfFindRepDlg> AfFindRepDlgPtr;


/*----------------------------------------------------------------------------------------------
	This class is the dialog that is used when they replace an empty string.
----------------------------------------------------------------------------------------------*/
class ConfirmEmptyReplaceDlg : public AfDialog
{
public:
	ConfirmEmptyReplaceDlg();

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnActivate(bool fActivating, LPARAM lp);
};

typedef GenSmartPtr<ConfirmEmptyReplaceDlg> ConfirmEmptyReplaceDlgPtr;


#endif // !AFFIND_DLG_H

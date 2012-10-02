/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDialog.h
Responsibility: Darrell Zook
Last reviewed:

Description:
	This file contains class declarations for the following classes:
		AfDialog : AfWnd - This is the base class for all dialogs (both modal or modeless).
		AfDialogView : AfDialog - This is the base class for dialogs that should be contained
			within another dialog. This is mainly used for dialogs that show up on tab controls,
			where the user can switch between dialogs. This class doesn't actually do anything
			but provide virtual methods that should be overridden to take the appropriate
			action when called. These dialogs are always modeless.
		HelpAboutDlg : AfDialog - This is a generic Help dialog for an application.
		AfButton : AfWnd - This class provides our special button functionality, including
			an icon to the left of the text or a down arrow to the right of the text used for
			popup menus.
		AfStaticText : AfWnd - Supports disabling shortcuts in static text items when the
			corresponding edit box is disabled.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDIALOG_H
#define AFDIALOG_H 1


// Uncomment the next line to enable tooltip help when hovering over a control on a dialog.
// NOTE: If this is done, the AfDialog::EnumChildProc method should be changed to use the
// proper string for each control.
//#define INCLUDE_TOOL_TIPS 1


class AfDialog;
class AfDialogView;
class AfDialogShell;
class AfDataExchange;
class HelpAboutDlg;
class AfButton;
typedef GenSmartPtr<AfDialog> AfDialogPtr;
typedef GenSmartPtr<AfDialogView> AfDialogViewPtr;
typedef GenSmartPtr<AfDialogShell> AfDialogShellPtr;
typedef GenSmartPtr<HelpAboutDlg> HelpAboutDlgPtr;
typedef GenSmartPtr<AfButton> AfButtonPtr;

typedef enum BtnType
{
	kbtImage = 0,	//Image is passed in to go on button
	kbtHelp,		//Use default help Bmp
	kbtFont,		//Use default font Bmp
	kbtPopMenu,		//Creats a popup menu icon
	kbtMore,		//Creates a double down arrow
	kbtLess,		//Creates a double up arrow
};

/*----------------------------------------------------------------------------------------------
	Dialog class.
	Hungarian: dlg.
----------------------------------------------------------------------------------------------*/
class AfDialog : public AfWnd
{
	typedef AfWnd SuperClass;
public:
	AfDialog(void);
	~AfDialog(void);

	int DoModal(HWND hwndPar, int rid = 0, void * pv = NULL);
	void DoModeless(HWND hwndPar, int rid = 0, void * pv = NULL);

	void SetResourceId(int rid)
		{ m_rid = rid; }
	int GetResourceId()
		{ return m_rid; }
	void SetHelpUrl(const achar * pszHelpUrl)
		{ m_pszHelpUrl = pszHelpUrl; }
	const achar * GetHelpUrl()
		{ return m_pszHelpUrl; }
	void SetHelpFile(const achar * pszHelpFile)
		{ m_pszHelpFile = pszHelpFile; }
	void CenterInWindow(HWND hwndPar);
	AfDialog * GetTopDialog();

	// virtual function called for WM_ACTIVATE from DlgProc()
	virtual void HandleDlgMessages(HWND hwndDlg, WPARAM wp, LPARAM lp)
	{
		HandleDlgMessages(hwndDlg, LOWORD(wp) != WA_INACTIVE);
	}

	static void HandleDlgMessages(HWND hwndDlg, bool fActivate);
	virtual bool Synchronize(SyncInfo & sync);

protected:
	/*------------------------------------------------------------------------------------------
		Static dialog proc.
	------------------------------------------------------------------------------------------*/
	static BOOL CALLBACK DlgProc(HWND hwndDlg, uint msg, WPARAM wParam, LPARAM lParam);

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnHelpInfo(HELPINFO * phi);
	virtual bool OnCommand(int cid, int nc, HWND hctl);
	virtual bool OnApply(bool fClose);
	virtual bool OnCancel();
	virtual bool OnHelp();
	virtual bool OnActivate(bool fActivating, LPARAM lp);
	//virtual bool OnPaint(HDC hdc);
	//virtual bool OnSetFocus();
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);

	// Functions used by dialog subclasses to derive old and current values for property tpt.
	static void MergeIntProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
		int varExpected, int & nValOld, int & nValCur, bool fFirst);
	static void MergeMvIntProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
		int & nValOld, int & nValCur, int & nVarOld, int & nVarCur, bool fFirst);
	static void MergeInvertingProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
		int varExpected, int & nOldRet, int & nCurRet, bool fFirst);
	static void MergeStringProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
		StrUni & stuValOld, StrUni & stuValCur, bool fFirst, OLECHAR * pszConflict);

	static void MergeFmtDlgIntProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
		int tpv, int & nHard, int & nSoft, bool fFirst,
		int & xChrpi, bool fHard, bool fInverting = false);
	static void MergeFmtDlgIntProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
		int tpv, COLORREF & clrHard, COLORREF & clrSoft, bool fFirst,
		int & xChrpi, bool fHard);
	static void MergeFmtDlgIntProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
		int tpv, int & nHard, int & nSoft, int & nVarHard, int & nVarSoft, bool fFirst,
		int & xChrpi, bool fHard, bool fInverting = false, bool fMv = false);
	static void MergeFmtDlgStrProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
		StrUni & stuHard, StrUni & stuSoft, bool fFirst, int & xChrpi, bool fHard);

	bool UpdateData(bool fSave = true);
	// Override this method and add DDX_ calls for each variable that is attached to a control.
	virtual void DoDataExchange(AfDataExchange * padx) // Does nothing.
		{ AssertPtr(padx); }

	void TurnOnDefaultKeyboard();

	/*------------------------------------------------------------------------------------------
		Setting simple text control values.
	------------------------------------------------------------------------------------------*/
	void DDX_Text(AfDataExchange * padx, int cid, BYTE & b);
	void DDX_Text(AfDataExchange * padx, int cid, short & sn);
	void DDX_Text(AfDataExchange * padx, int cid, int & n);
	void DDX_Text(AfDataExchange * padx, int cid, uint & u);
	void DDX_Text(AfDataExchange * padx, int cid, long & ln);
	void DDX_Text(AfDataExchange * padx, int cid, StrUni & stu);
	//void DDX_Text(AfDataExchange * padx, int cid, ITsString ** pptss);
	void DDX_Text(AfDataExchange * padx, int cid, float & flt);
	void DDX_Text(AfDataExchange * padx, int cid, double & dbl);

	/*------------------------------------------------------------------------------------------
		Setting common Windows control types.
	------------------------------------------------------------------------------------------*/
	void DDX_Check(AfDataExchange * padx, int cid, int & n);
	void DDX_Radio(AfDataExchange * padx, int cid, int & n);
	void DDX_LBIndex(AfDataExchange * padx, int cid, int & n);
	void DDX_CBIndex(AfDataExchange * padx, int cid, int & n);
	void DDX_Scroll(AfDataExchange * padx, int cid, int & n);
	void DDX_Slider(AfDataExchange * padx, int cid, int & n);

	// for getting access to the actual controls
	void DDX_Control(AfDataExchange * padx, int cid, AfWnd ** ppwnd);

	bool SimpleScanf(Psz pszText, Pcsz pszFormat, va_list pData);
	void DDX_TextWithFormat(AfDataExchange * padx, int cid, LPCTSTR lpszFormat,
		UINT ridPrompt, ...);

	int m_rid;

	const achar * m_pszHelpUrl;
	const achar * m_pszHelpFile;

	static IHelpTopicProviderPtr s_qhtprovHelpUrls;

	bool m_fModeless;

	static HWND s_hwndCurModelessDlg;
	static HHOOK s_hhook;
	static LRESULT CALLBACK GetMsgProc(int code, WPARAM wp, LPARAM lp);

#ifdef INCLUDE_TOOL_TIPS
	// When enabled, this pops up control tool tips when mouse hovers over control.
	// To enable it in a dialog, in the OnInitDlg method, add the following line:
	//    InitializeToolTip(x);
	// where the optional parameter is the width of the pop-up windows.
	void InitializeToolTip(int dxpMax = -1);

	static BOOL EnumChildProc(HWND hwndCtrl, LPARAM lp);
	static LRESULT CALLBACK GetMsgProcTip(int nCode, WPARAM wp, LPARAM lp);

	HWND m_hwndToolTip;
	HHOOK m_hhook;

	enum { kdxpDefTipWidth = 200 };
#endif INCLUDE_TOOL_TIPS
};


/*----------------------------------------------------------------------------------------------
	Dialog view class. This class should be used for individual tabs on a tabbed dialog.
	It should also be used for dialogs that can be reused in multiple places, like the paragraph
	formatting dialog (it is embedded in the Format/Styles dialog and is used as a top level
	dialog with the Format/Paragraph menu command).

	NOTE: The dialog resource for a dialog view should not have a caption or any borders. Also,
	dialog views should always be modeless, not modal.

	Hungarian: dlgv.
----------------------------------------------------------------------------------------------*/
class AfDialogView : public AfDialog
{
public:
	AfDialogView()
	{
		m_fCancelInProgress = false;
	}

	// This enum is used to specify the reason behind calling QueryClose.
	typedef enum
	{
		kqctOk,     // The OK button on the parent dialog was clicked and it is closing.
		kqctCancel, // The Cancel button on the parent dialog was clicked and it is closing.
		kqctChange, // We are switching to another dialog view (i.e. to a different tab).
	} QueryCloseType;

	// Apply will be called when the user selects OK or Apply from the parent dialog, or
	// the parent dialog wants to persist the changes in this dialog.
	virtual bool Apply()
		{ return true; }
	// Cancel will be called when the user selects Cancel from the parent dialog.
	virtual void Cancel()
		{ }
	// SetActive will be called whenever this dialog view gets the focus. This could either
	// be because it just got created, or because the user just switched to this tab (assuming
	// this dialog view is in a tabbed dialog). One-time initialization stuff should go into
	// OnInitDialog. Any other initialization stuff should go here.
	// Return false here to keep the dialog view from gaining the focus and becoming visible.
	virtual bool SetActive()
		{ return true; }
	// QueryClose will be called whenever this dialog view loses the focus. Look at the
	// QueryCloseType enum to see the different ways this is called.
	// Return false here to keep the dialog view from losing the focus.
	virtual bool QueryClose(QueryCloseType qct)
		{ return true; }

	bool Help()
	{
		return OnHelp();
	}
	void SetCancelInProgress()
	{
		m_fCancelInProgress = true;
	}
protected:
	bool m_fCancelInProgress;
};


/*----------------------------------------------------------------------------------------------
	Dialog shell class. This class should be used to host AfDialogView classes when they should
	act like top level dialogs (like the dialog that comes up when you select Format/Paragraph).

	This dialog provides OK, Cancel, and and (optional) Help buttons. It will automatically
	resize to fit the dialog view that it embeds.

	Hungarian: dlgs.
----------------------------------------------------------------------------------------------*/
class AfDialogShell : public AfDialog
{
public:
	int CreateDlgShell(AfDialogView * pdlgv, Pcsz pszTitle, HWND hwndPar, void * pv = NULL);
	int CreateNoHelpDlgShell(AfDialogView * pdlgv, Pcsz pszTitle, HWND hwndPar,
		void * pv = NULL);

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);

	virtual bool OnApply(bool fClose);
	virtual bool OnCancel();
	virtual bool OnHelp();

	AfDialogViewPtr m_qdlgv;
	Pcsz m_pszTitle;
};


class AfUserException : public std::exception
{
};

// REVIEW SteveMc: Should these be moved to an RC file sometime?
// DDV parse errors
#define DDX_PARSE_INT               0xF110
#define DDX_PARSE_REAL              0xF111
#define DDX_PARSE_INT_RANGE         0xF112
#define DDX_PARSE_REAL_RANGE        0xF113
#define DDX_PARSE_STRING_SIZE       0xF114
#define DDX_PARSE_RADIO_BUTTON      0xF115
#define DDX_PARSE_BYTE              0xF116
#define DDX_PARSE_UINT              0xF117
#define DDX_PARSE_DATETIME          0xF118
#define DDX_PARSE_CURRENCY          0xF119

/*
	// Parsing error prompts
	DDX_PARSE_INT,              "Please enter an integer."
	DDX_PARSE_REAL,             "Please enter a number."
	DDX_PARSE_INT_RANGE,        "Please enter an integer between %1 and %2."
	DDX_PARSE_REAL_RANGE,       "Please enter a number between %1 and %2."
	DDX_PARSE_STRING_SIZE,      "Please enter no more than %1 characters."
	DDX_PARSE_RADIO_BUTTON,     "Please select a button."
	DDX_PARSE_BYTE,             "Please enter an integer between 0 and 255."
	DDX_PARSE_UINT,             "Please enter a positive integer."
	DDX_PARSE_DATETIME,         "Please enter a date and/or time."
	DDX_PARSE_CURRENCY,         "Please enter a currency."
*/


/*----------------------------------------------------------------------------------------------
	Class to exchange data between member variables and controls in a dialog.
	Hungarian: adx.
----------------------------------------------------------------------------------------------*/
class AfDataExchange
{
public:
	AfDataExchange(AfDialog * pdlg, bool fSave);

	HWND PrepareCtrl(int nID); // return HWND of control
	HWND PrepareEditCtrl(int nID); // return HWND of control
	void Fail(); // will throw exception

	bool m_fSave; // true => save and validate data
	AfDialogPtr m_qdlg; // dialog container

protected:
	HWND m_hwndLastControl; // last control used (for validation)
	bool m_fEditLastControl; // last control was an edit item
};


#if 0

/*----------------------------------------------------------------------------------------------
	Standard Dialog Data Validation routines.
----------------------------------------------------------------------------------------------*/
// range - value must be >= minVal and <= maxVal
// NOTE: you will require casts for 'minVal' and 'maxVal' to use the
//   UINT, DWORD or float types
void DDV_MinMaxByte(AfDataExchange * padx, BYTE value, BYTE minVal, BYTE maxVal);
void DDV_MinMaxShort(AfDataExchange * padx, short value, short minVal, short maxVal);
void DDV_MinMaxInt(AfDataExchange * padx, int value, int minVal, int maxVal);
void DDV_MinMaxLong(AfDataExchange * padx, long value, long minVal, long maxVal);
void DDV_MinMaxUInt(AfDataExchange * padx, UINT value, UINT minVal, UINT maxVal);
void DDV_MinMaxDWord(AfDataExchange * padx, DWORD value, DWORD minVal, DWORD maxVal);
void DDV_MinMaxFloat(AfDataExchange * padx, float const & value, float minVal, float maxVal);
void DDV_MinMaxDouble(AfDataExchange * padx, double const & value, double minVal, double maxVal);

// special control types
void DDV_MinMaxSlider(AfDataExchange * padx, DWORD value, DWORD minVal, DWORD maxVal);

// number of characters
void DDV_MaxChars(AfDataExchange * padx, StrUni const & value, int cch);

#endif // if 0


/*----------------------------------------------------------------------------------------------
	Application About dialog.
----------------------------------------------------------------------------------------------*/
class HelpAboutDlg : public AfDialog
{
public:
	HelpAboutDlg();
	~HelpAboutDlg();

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);

protected:
	virtual bool OnHelp()
	{
		// Don't do anything here, because there isn't Help info for this dialog.
		return true;
	}

	HFONT m_hfontAppName;
	HFONT m_hfontSuiteName;
};


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of a normal Windows button. It also adds the
	following functionality:
	1) Displaying an icon from an imagelist to the left of the button, or
	2) Displaying a down wedge to the right of the button for popup menus.

	Hungarian: btn
----------------------------------------------------------------------------------------------*/
class AfButton : public AfWnd
{
typedef AfWnd SuperClass;

public:
	AfButton();
	~AfButton();

	void Create(HWND hwndPar, int wid, const achar * pszCaption, BtnType btntyp,
		HIMAGELIST himl, int imag, DWORD dwStyle = WS_CHILD | WS_VISIBLE | WS_TABSTOP);
	void SubclassButton(HWND hwndDlg, int wid, BtnType btntyp, HIMAGELIST himl, int imag);
	void SetArrowType(BtnType btntyp);

protected:
	virtual bool OnDrawThisItem(DRAWITEMSTRUCT * pdis);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	virtual void GetCaption(HDC hdc, const Rect & rc, StrApp & strCaption)
	{
		strCaption = m_strCaption;
	}

	StrApp m_strCaption;
	HIMAGELIST m_himl;
	bool m_fimlCreated;
	int m_imag;
	bool m_fPopupMenu;
	int m_widDef;
	BtnType m_btntyp;
};

/*----------------------------------------------------------------------------------------------
	This class subclasses a static text control in a dialog. The reason is that if the static
	text contains a shortcut and the user types it, Windows activates the next enabled
	non-static control. That isn't usually what we want. We want to activate the next control,
	if it is enabled, otherwise do nothing. This control overrides the standard behavior
	only to the extent that if the following control is disabled, it returns an empty string
	when processing WM_GETTEXT. This is the recommended way to disable the standard behavior.
	See the MSDN article, "HOWTO: Disable the Mnemonic on a Disabled Static Text Control."

	Note: a common manifestation of this problem is that the dialog closes when the user
	issues a shortcut for a disabled control. This happens when the OK or Cancel button
	happens to be the next enabled control.

	Usage: if hwndDlg is the window handle of the dialog, and wid is the id of the edit box
	FOLLOWING the static text item that needs fixing, inserting this one line in your
	OnInitDlg method will fix the problem:

	AfStaticText::FixEnabling(hwndDlg, wid);

	Hungarian: ast
----------------------------------------------------------------------------------------------*/
class AfStaticText : public AfWnd
{
typedef AfWnd SuperClass;

public:
	AfStaticText(HWND hwndDlg, int wid);
	~AfStaticText()
	{}

	static void FixEnabling(HWND hwndDlg, int wid);

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	HWND m_hwndDlg;
	TCHAR m_nonMnemnonicText[300];
};

typedef GenSmartPtr<AfStaticText> AfStaticTextPtr;

#endif // !AFDIALOG_H

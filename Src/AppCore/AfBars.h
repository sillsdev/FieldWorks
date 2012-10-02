/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfBars.h
Responsibility: Steve McConnel (was Darrell Zook)
Last reviewed:

Description:
	This file contains class declarations for the following classes:
		AfToolBarCombo : TssComboEx - This class implements an extended combobox that can be
			placed on a toolbar.
		AfToolBarEdit : TssEdit - An edit field that can be placed on a toolbar.
		AfToolBarToolTip : AfWnd - This class is used by a toolbar to subclass the tooltip
			belonging to the toolbar. It is needed so that tooltips will be shown on child
			windows (AfToolBarCombo windows).
		AfToolBar : AfWnd - This class implements a toolbar.
		AfReBar : AfWnd - This class implements a rebar, which can contain multiple toolbars.
			This class allows toolbars to be resized and reordered within the rebar frame.
		AfStatusBar : AfWnd - This class implements a status bar.
		AfProgressBar : AfWnd - This class implements a progress bar.
		AfMenuBar : AfToolBar - This class implements a specialized toolbar that can show
			a menu. This should only be used for top-level menus in an application that
			show up in the rebar for the application.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef AFTOOLBAR_H
#define AFTOOLBAR_H 1


/*----------------------------------------------------------------------------------------------
	Forward declarations.
-------------------------------------------------------------------------------*//*:End Ignore*/
class AfToolBarCombo;
class AfToolBarEdit;
class AfToolBarToolTip;
class AfToolBar;
class AfReBar;
class AfStatusBar;
class AfProgressBar;
class AfMenuBar;
typedef GenSmartPtr<AfToolBarCombo> AfToolBarComboPtr;
typedef GenSmartPtr<AfToolBarEdit> AfToolBarEditPtr;
typedef GenSmartPtr<AfToolBarToolTip> AfToolBarToolTipPtr;
typedef GenSmartPtr<AfToolBar> AfToolBarPtr;
typedef GenSmartPtr<AfReBar> AfReBarPtr;
typedef GenSmartPtr<AfStatusBar> AfStatusBarPtr;
typedef GenSmartPtr<AfProgressBar> AfProgressBarPtr;
typedef GenSmartPtr<AfMenuBar> AfMenuBarPtr;


/*----------------------------------------------------------------------------------------------
	This class contains variables that are shared by the AfToolbar and AfToolBarCombo classes.
	These are used for drag-and-drop operations.

	Hungarian: tbg
----------------------------------------------------------------------------------------------*/
class ToolBarGlobals
{
	friend AfToolBarCombo;
	friend AfToolBar;

public:
	ToolBarGlobals();
	~ToolBarGlobals();

protected:
	bool m_fDragging;		// Flag whether we are currently dragging.
	HWND m_hwndOld;			// Handle to the original AfToolbar's window.
	HWND m_hwndChild;		// Handle to the AfToolBarCombo window where the dragging began.
	int m_iSrcButton;		// The button we were over when dragging began.
	int m_iDstButton;		// The button we are over when dragging ends.
};

/*----------------------------------------------------------------------------------------------
	Extended combo box window for placing on a toolbar.

	Hungarian: tbc.
----------------------------------------------------------------------------------------------*/
class AfToolBarCombo : public TssComboEx
{
	typedef TssComboEx SuperClass;

public:

	AfToolBarCombo();

	virtual void Create(HWND hwndPar, DWORD dwStyle, int wid, int iButton, Rect & rc,
		HWND hwndToolTip, bool fTypeAhead);

	virtual bool OnCharEnter(int ctid, NMHDR * pnmh, long & lnRet);
	virtual bool OnCharTab(int ctid, NMHDR * pnmh, long & lnRet);
	virtual bool OnCharEscape(int ctid, NMHDR * pnmh, long & lnRet);

	// Return the button index for this toolbar child.
	int GetButtonIndex()
		{ return m_iButton; }
	// Set the button index for this toolbar child.
	void SetButtonIndex(int iButton)
		{ m_iButton = iButton; }

	virtual bool GetHelpStrFromPt(Point pt, ITsString ** pptss);

	void SetWs(int ws)
	{
		Assert(ws);
		m_ws = ws;
	}

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnSelEndOK(int nID, HWND hwndCombo);

	int m_iButton;		// Button index for this toolbar child.

	// Global flag whether the ComboBoxEx class common control has been initialized.
	static bool s_fInitialized;
};


/*----------------------------------------------------------------------------------------------
	Edit box for placing on a toolbar.
	NOTE: This class has not been implemented in any useful way.

	Hungarian: tbe.
----------------------------------------------------------------------------------------------*/
class AfToolBarEdit : public TssEdit
{
	typedef TssEdit SuperClass;
public:
	AfToolBarEdit();
	virtual void Create(HWND hwndPar, DWORD dwStyle, int wid, int iButton, Rect & rc,
		HWND hwndToolTip, bool fTypeAhead, ILgWritingSystemFactory * pwsf, int ws);

	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnChange();
	virtual bool OnSetFocus(HWND hwndOld, bool fTbControl = false);

	virtual bool GetHelpStrFromPt(Point pt, ITsString ** pptss);

	virtual bool DoesSemiTagging()
	{
		// A tool-bar edit box shows only the text colored and styles according to the
		// active tags, but does not show the tags themselves.
		return true;
	}

	void SetUserWs(int wsUser)
	{
		Assert(wsUser);
		m_wsUser = wsUser;
	}

protected:
	AfToolBar * m_ptlbr;
	int m_iButton;
	int m_wsUser;		// user interface writing system id.

	virtual bool OnCharEnter();
	virtual bool OnCharTab();
	virtual bool OnCharEscape();
};


/*----------------------------------------------------------------------------------------------
	This class handles the TTM_WINDOWFROMPOINT message to allow tooltips on the edit box within
	a combo box on the toolbar.

	Hungarian: tbtt.
----------------------------------------------------------------------------------------------*/
class AfToolBarToolTip : public AfWnd
{
	typedef AfWnd SuperClass;

public:
	/*------------------------------------------------------------------------------------------
		Attach the given window handle to this object, and attach this object's WndProc handler
		to the window handle.  Also set the toolbar window handle for this object.

		@param hwnd Handle to the combobox window.
		@param hwndToolBar Handle to the combobox's enclosing toolbar window.
	------------------------------------------------------------------------------------------*/
	void Subclass(HWND hwnd, HWND hwndToolBar)
	{
		SubclassHwnd(hwnd);
		m_hwndToolBar = hwndToolBar;
	}

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	HWND m_hwndToolBar;		// Handle to the enclosing toolbar window.
};


/*----------------------------------------------------------------------------------------------
	This class implements the basic functionality of a toolbar for this application framework.

	Hungarian: tlbr.
----------------------------------------------------------------------------------------------*/
class AfToolBar : public AfWnd
{
	typedef AfWnd SuperClass;

public:
	AfToolBar(void);
	~AfToolBar(void);

	virtual void Initialize(int wid, int rid, const achar * pszName);
	virtual void Create(AfMainWnd * pafw);
	void Load();

	void SetWritingSystemFactory(ILgWritingSystemFactory * pwsf)
	{
		AssertPtr(pwsf);
		m_qwsf = pwsf;
		CheckHr(pwsf->get_UserWs(&m_wsUser));
	}

	// Return the toolbar style set for this toolbar.
	uint GetStyle(void)
	{
		Assert(m_hwnd);
		return ::SendMessage(m_hwnd, TB_GETSTYLE, 0, 0);
	}

	// Return the hwnd of this toolbar.
	HWND GetHwnd()
	{
		Assert(m_hwnd);
		return m_hwnd;
	}


	// Return the window ID assigned to this toolbar.
	int GetWindowId()
		{ return m_wid; }

	// Return the name assigned to this toolbar.
	const achar * GetName()
		{ return m_str.Chars(); }

	void SetupComboControl(AfToolBarCombo ** pptbc, int cid, int nWidth, int dypDropdown,
		bool fTypeAhead);
	void SetupEditControl(AfToolBarEdit ** pptbe, int cid, int nWidth);

	virtual bool GetHelpStrFromPt(Point pt, ITsString ** pptss);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	void UpdateIconColor(int cid, COLORREF clr);
	void UpdateIconImage(int widButton, HIMAGELIST himlSource, int iimage);

	enum
	{
		kdxpBmp = 16,
		kdypBmp = 15,
	};

	AfMainWnd * MainWindow()
	{
		return m_qafw;
	}

protected:
	int m_wid;				// Window ID for this toolbar.
	int m_rid;				// Resource ID for this toolbar.
	StrApp m_str;			// Name of this toolbar.
	ILgWritingSystemFactoryPtr m_qwsf;	// writing system factory for the app's main window.
	int m_wsUser;			// user interface writing system id.
	AfMainWndPtr m_qafw;	// Application frame window to which this toolbar is attached.

	void Save();
	void SetButtons(ushort * prgcid, int ccid);
	void UpdateToolBar();

	virtual bool OnDragBegin(Point pt);
	virtual bool OnDragOver(Point pt);
	virtual bool OnDragEnd(Point pt);
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);

	// Release smart pointers in response to a WM_NCDESTROY message.
	virtual void OnReleasePtr()
	{
		m_qafw.Clear();
	}
};


/*----------------------------------------------------------------------------------------------
	This class wraps the Windows "rebar" facility for this application framework.

	Hungarian: rebr.
----------------------------------------------------------------------------------------------*/
class AfReBar : public AfWnd
{
	typedef AfWnd SuperClass;

public:
	AfReBar(void);
	~AfReBar(void);

	void CreateHwnd(HWND hwndPar, int wid);
};


/*----------------------------------------------------------------------------------------------
	This class implements a "status bar" facility for this application framework.  The status
	bar is divided into five standard panes.

	Hungarian: stbr.
----------------------------------------------------------------------------------------------*/
class AfStatusBar : public AfWnd, public IAdvInd
{
	typedef AfWnd SuperClass;

public:
	AfStatusBar(void);
	~AfStatusBar(void);

	//:> IAdvInd methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);
	STDMETHOD(Step)(int nStepAmt);

	void CreateHwnd(HWND hwndPar, int wid);

	void InitializePanes();
	void StoreHelpText(const achar * pszHelp);
	void DisplayHelpText();
	void RestoreStatusText();
	void SetRecordInfo(const OLECHAR * pszDatestamp, const OLECHAR *pszTitle,
		const achar * pszToolTip);
	void SetSortingStatus(bool fSorting, const achar * pszToolTipMethod,
		const OLECHAR * pszSortKey, const achar * pszToolTipKey);
	void SetFilteringStatus(bool fFiltering, const achar * pszToolTip);
	void SetLocationStatus(int irec, int crec);

	void StartProgressBar(const achar * pszMessage, int nLowLim = 0, int nHighLim = 100,
		int nStep = 10);
	void StepProgressBar(int nIncrement = 0);
	void EndProgressBar();
	bool IsProgressBarActive() {return m_qprbr;}

	virtual bool OnDrawThisItem(DRAWITEMSTRUCT * pdis);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	bool GetHelpStrFromPt(Point pt, ITsString ** pptss);

	enum { kcStatusPanes = 5,
			kiRecord = 0, kiSortKey = 1, kiSorted = 2, kiFiltered = 3, kiLocation = 4,
			kiHelp = kiRecord, kiProgress = kiSortKey
	};
	enum { knMenuTimer = 100 };

	void SetUserWs(int wsUser)
	{
		Assert(wsUser);
		m_wsUser = wsUser;
	}

protected:
	int m_rgnPaneEdges[kcStatusPanes];	// Array of right edge locations for the panes.
	HWND m_hwndToolTip;		// Handle to the tooltip window for the status bar.
	int m_wsUser;			// user interface writing system id.

	// First pane: menu help / general help / current record's datestamp and title.
	const StrApp m_strGeneralHelp;	// Fixed status bar message (kstidIdle).
	const StrApp m_strHelpMsgTip;	// Fixed tooltip help message (kstidStBarHelpMsg).
	StrApp m_strHelpMsg;			// Contains help message to display in the first pane.
	StrUni m_stuDatestamp;			// Contains record datestamp to display in the first pane.
	StrUni m_stuTitle;				// Contains record title to display in the first pane.
	StrApp m_strRecordToolTip;		// Contains tooltip string for the first pane.

	// Second pane: current record's sort key value (if sorting) / progress indicator.
	StrUni m_stuSortKey;			// Contains sort key string to display in the second pane.
	StrApp m_strSortKeyToolTip;		// Contains tooltip string for the second pane.
	AfProgressBarPtr m_qprbr;		// Progress bar to display in the second pane instead.

	// Third pane: sorting flag.
	bool m_fSorted;					// Flag whether or not this pane is empty.
	const StrApp m_strSorted;		// Fixed status bar message (kstidStBarSorted).
	const StrApp m_strNoSortTip;	// Fixed tooltip help message (kstidStBarDefaultSort).
	StrApp m_strSortedToolTip;		// Contains tooltip string for the third pane.

	// Fourth pane: filtering flag.
	bool m_fFiltered;				// Flag whether or not this pane is empty.
	const StrApp m_strFiltered;		// Fixed status bar message (kstidStBarFiltered).
	const StrApp m_strNoFilterTip;	// Fixed tooltip help message (kstidNoFilter).
	StrApp m_strFilteredToolTip;	// Contains tooltip string for the fourth pane.

	// Fifth pane: record index / count.
	StrApp m_strLocation;				// Contains string to display in the fifth pane.
	const StrApp m_strLocationToolTip;	// Contains tooltip string for the fifth pane.
};


/*----------------------------------------------------------------------------------------------
	This class implements a progress bar which is typically mapped to the second pane of the
	status bar.

	Hungarian: prbr.
----------------------------------------------------------------------------------------------*/
class AfProgressBar : public AfWnd
{
	typedef AfWnd SuperClass;

public:
	AfProgressBar(void);
	~AfProgressBar(void);

	void CreateHwnd(HWND hwndPar, int wid);

	void SetColors(COLORREF clrBar, COLORREF clrBk);	// PBM_SETBARCOLOR and PBM_SETBKCOLOR
	void SetRange(int nLowLim, int nHighLim);			// PBM_SETRANGE32
	void StepIt(int nIncrement = 0);					// PBM_DELTAPOS or PBM_STEPIT
	void SetStep(int nStepInc);							// PBM_SETSTEP
	void SetPos(int nNewPos);							// PBM_SETPOS

protected:
	int m_nLowLim;		// The lower limit of the progress bar values.
	int m_nHighLim;		// The upper limit of the progress bar values.
	int m_nStep;		// The default amount by which to increment the progress bar.
	int m_nCurrent;		// The current progress bar value.
};


/*----------------------------------------------------------------------------------------------
	This class implements a toolbar window that simulates a menubar.

	Hungarian: mnbr
----------------------------------------------------------------------------------------------*/
class AfMenuBar : public AfToolBar
{
	typedef AfToolBar SuperClass;

public:
	AfMenuBar();
	~AfMenuBar();

	virtual void Initialize(HWND hwndMain, int wid, int rid, const achar * pszName);
	virtual void Create(AfMainWnd * pafw);

	void ShowChevronPopup(int ibtn, HWND hwnd, Point & pt);

	virtual bool ShowMenu(int nId);
	virtual bool GetHelpStrFromPt(Point pt, ITsString ** pptss);

protected:
	HMENU m_hmenu;		// Handle to the menu associated with this object.
	int m_nIdOld;		// Menu id of the menu being displayed by ShowMenu.
	HWND m_hwndMain;	// Handle to the application's main window. (?)

	// s_wmShowMenu exists because I (DarrellZ) could not get a second popup menu to show right
	// after closing the first one when the user moves off one top level menu item to another
	// item.  It always closed the second menu with the first one. To get around this problem,
	// I post our user-defined message to the queue. By the time it receives the message, the
	// first popup menu has already been closed, so it is safe to create the second popup menu.
	static uint s_wmShowMenu;
	// Handle to the current hook returned by SetWindowsHookEx (used when a popup menu is
	// currently showing).
	static HHOOK s_hhook;
	// Pointer to the menu bar that s_hhook belongs to (used when a popup menu is currently
	// showing).
	static AfMenuBarPtr s_qmnbrCur;
	// Handle to the most recent popup menu displayed by ShowMenu.
	static HMENU s_hmenuLastPopup;
	// Handle to the menu most recently displayed by ShowMenu or used by WM_MENUSELECT.
	static HMENU s_hmenuOld;
	// Flag that the current menu selection can open a submenu.
	static bool s_fOverSubMenu;
	// Flag that a menu accelerator key has been pressed.
	static bool s_fIgnoreMouseMove;

	static LRESULT CALLBACK MsgHook(int code, WPARAM wParam, LPARAM lParam);

	virtual bool OnNotifyThis(int id, NMHDR * pnmh, long & lnRet);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
};


#endif // !AFTOOLBAR_H

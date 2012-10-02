/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfMainWnd.h
Responsibility: Darrell Zook
Last reviewed:

Description:
	This file contains class declarations for the following classes:

	This file contains the declaration of a frame window class that supports a status bar,
	multiple toolbars, menu help, and "What's this" help.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFMAINWND_H
#define AFMAINWND_H 1

// Border positions used in the format border combo.
// The numbers used for each correspond to positions in Appcore\res\FmtBdrComboData.

enum
{
	kbpAll,
	kbpSingles,  // Start of the 4 buttons that represent a single side.
	kbpTop = kbpSingles,
	kbpBottom,
	kbpLeft,
	kbpRight,
	kbpNone,
	kbpLim,
};

//class AfLpInfo;	// Forward reference.

class AfMainWnd;
typedef GenSmartPtr<AfMainWnd> AfMainWndPtr;


/*----------------------------------------------------------------------------------------------
	Main frame window class. Creates a status bar, toolbar, and menu bar.

	Hungarian: afw.
----------------------------------------------------------------------------------------------*/
class AfMainWnd : public AfWnd
{
public:
	typedef AfWnd SuperClass;

	AfMainWnd(void);
	~AfMainWnd(void);

	virtual void GetStatsQuery(int iList, StrUni * pstuQuery)
	{
		*pstuQuery = L"";
	}
	virtual AfToolBar * GetToolBar(int wid);
	virtual int GetBandFromHwnd(HWND hwnd);
	void ShowToolBar(int wid, bool fShow = true);
	bool LoadToolbars(FwSettings * pfws, const achar * pszRoot, DWORD dwBarFlags);
	void SaveToolbars(FwSettings * pfws, const achar * pszRoot, const achar * pszKey);
	virtual void FixMenu(HMENU hmenu);
	void SetFullHelpUrl(const achar * pszFullHelpUrl);
	void ClearFullHelpUrl();

	AfStatusBar * GetStatusBarWnd()
	{
		return m_qstbr;
	}
	AfMenuMgr * GetMenuMgr(void)
	{
		return &m_mum;
	}

	virtual AfStylesheet * GetStylesheet()
	{
		return NULL;
	}
	virtual bool OnStyleDropDown(HWND hctl);

	virtual void OnStylesheetChange();

	static void ToggleHelpMode();
	static bool InHelpMode() {return s_hhook != NULL;}

	// Overrides.
	virtual void GetClientRect(Rect & rc);
	virtual bool GetHelpStrFromPt(Point pt, ITsString ** pptss);
	virtual void OnIdle();
	virtual void OnActivate(bool fActivating, HWND hwnd);

	// This is the first thing that happens when a frame window is activated or deactivated.
	// It is called before click actions take place. OnActivate() happens after click actions
	// are processed.
	// @param fActivating True if we are activating this window, false if we are deactivating.
	virtual void OnPreActivate(bool fActivating)
	{}
	virtual bool OnClose()
	{
		// Having the destructor get it seems to be too late, maybe its DLL is already unloaded?
		m_qxpat.Clear();

		// Return false to close the window.
		SaveSettings(NULL, true);
		return false;
	}
	virtual int GetMinHeight()
	{
		return kdypMin;
	}
	virtual int GetMinWidth()
	{
		return 0;
	}

	void GetColors(COLORREF * pclrFore, COLORREF * pclrBack);
	int GetBdrBtnPos() {return m_bpBorderPos;}
	virtual void LoadSettings(const achar * pszRoot, bool fRecursive);
	virtual void SaveSettings(const achar * pszRoot, bool fRecursive);
	void UpdateToolBarIcon(int idToolBar, int idButton, COLORREF clr);
	virtual void OnToolBarButtonAdded(AfToolBar * ptlbr, int ibtn, int cid);

	virtual void PrepareNewWindowLocation();

	virtual void RegisterRootBox(IVwRootBox * prootb);
	void UnregisterRootBox(IVwRootBox * prootb);
	IVwRootBox * GetActiveRootBox(bool fTbControlOkay = true)
	{
		if (!fTbControlOkay && m_prootbPrev)
			// We aren't interested in a toolbar control. Return the stored
			// box that is the main editing area.
			return m_prootbPrev;
		else
			return m_prootbActive;
	}
	void SetActiveRootBox(IVwRootBox * prootb, bool fTbControl = false);
	bool GetActiveViewWindow(AfVwRootSite ** ppvwnd, int * pgrfvfs = NULL,
		bool fTbControlOkay = true);

	virtual void SetCurrentOverlay(IVwOverlay * pvo);

	void GetPageSetupInfo(POrientType * ppot, PgSizeType * ppst,
		int * pdxmpLeftMargin, int * pdxmpRightMargin,
		int * pdympTopMargin, int * pdympBottomMargin,
		int * pdympHeaderMargin, int * pdympFooterMargin,
		int * pdxmpPageWidth, int * pdympPageHeight,
		ITsString ** pptssHeader, ITsString ** pptssFooter, bool * pfHeaderOnFirstPage);

	virtual void EnableWindow(bool fEnable)
	{
		::EnableWindow(m_hwnd, fEnable);
	}

	virtual void RefreshToolbars();
	// Override if you use overlays; turn them on if true, off if false.
	virtual void ShowAllOverlays(bool fShow, bool fRemerge = false) {};

	virtual void RenameAndDeleteStyles(Vector<StrUni> & vstuOldNames,
		Vector<StrUni> & vstuNewNames,
		Vector<StrUni> & vstuDeletedNames) = 0;

	virtual void UpdateToolBarWrtSysControl() = 0;

	// This enum is used for GetActiveViewWindow. They can be |'ed together, so if anymore
	// are added, make sure their numbers are correct (8, 16, 32, ...).
	typedef enum
	{
		kvfsPara = 1,
		kvfsChar = 2,
		kvfsOverlay = 4,

		kvfsNormal = kvfsPara | kvfsChar,
		kvfsAll = kvfsPara | kvfsChar | kvfsOverlay,
	} ViewFormatState;

	/*------------------------------------------------------------------------------------------
		This should be overridden to return a hierarchical FilterMenuNodeVec that contains the
		information needed by the Filter dialogs to construct the popup menu for choosing
		fields.

		@return Pointer to a hierarchical FilterMenuNodeVec used to create a popup menu, or
						NULL.
	------------------------------------------------------------------------------------------*/
	virtual FilterMenuNodeVec * GetFilterMenuNodes(AfLpInfo * plpi)
	{
		return NULL;
	}

	/*------------------------------------------------------------------------------------------
		This returns a reference to a flattened out vector of FilterMenuNodes which is used by
		the filter code.
	------------------------------------------------------------------------------------------*/
	FilterMenuNodeVec & FlatFilterMenuNodeVec()
	{
		return m_vfmnFlat;
	}

	/*------------------------------------------------------------------------------------------
		This clears the filter menu, forcing it to be rebuilt the next time it is needed.
	------------------------------------------------------------------------------------------*/
	void ClearFilterMenuNodes()
	{
		m_vfmn.Clear();
		m_vfmnFlat.Clear();
	}

	/*------------------------------------------------------------------------------------------
		This should be overridden to return a SortMenuNodeVec that contains the information
		needed by the Sort dialogs to construct the combobox list of fields.

		@return Pointer to a SortMenuNodeVec used to populate a combobox, or NULL.
	------------------------------------------------------------------------------------------*/
	virtual SortMenuNodeVec * GetSortMenuNodes(AfLpInfo * plpi)
	{
		return NULL;
	}

	/*------------------------------------------------------------------------------------------
		This returns a reference to a flattened out vector of SortMenuNodes which is used by
		the sort methods code.
	------------------------------------------------------------------------------------------*/
	SortMenuNodeVec & FlatSortMenuNodeVec()
	{
		return m_vsmnFlat;
	}

	/*------------------------------------------------------------------------------------------
		This clears the sort method menu, forcing it to be rebuilt the next time it is needed.
	------------------------------------------------------------------------------------------*/
	void ClearSortMenuNodes()
	{
		m_vsmn.Clear();
		m_vsmnFlat.Clear();
	}

	/*------------------------------------------------------------------------------------------
		Return a pointer to the default sort method.  This must be called after
		GetSortMenuNodes() has been called at least once, since that other method may actually
		fill in the information for the default sort method.
	------------------------------------------------------------------------------------------*/
	AppSortInfo * GetDefaultSortMethod()
	{
		return &m_asiDefault;
	}

	virtual void SetWindowMode(bool fFullWindow);

	// We want to call this message handler from other windows, such as AfDeSplitChild.
	virtual bool OnMenuSelect(int cid, uint grfmf, HMENU hMenu);

	StrApp UndoRedoText(bool fRedo);

	// Save the window and point where a context menu was initiated.
	void SetContextInfo(AfWnd * pwnd, Point pt)
	{
		AssertPtrN(pwnd);
		m_qwndContext = pwnd;
		m_pt = pt;
	}

	// When a context menu is launched, we store a pointer to the window and point where it
	// was launched. This returns the window pointer and optionally returns the point.
	AfWnd * GetContextInfo(Point * ppt = NULL)
	{
		AssertPtrN(ppt);
		if (ppt)
			*ppt = m_pt;
		return m_qwndContext;
	}
	// Process Insert External Link menu item.
	virtual bool CmdExternalLink(Cmd * pcmd);
	void MakeExternalLinkAtSel(IVwSelection * pvwsel, StrUni stuFile, ISilDataAccess * psda,
		AfVwRootSite * pvwnd);

	// These make AfMainWnd a pure virtual class.
	virtual int UserWs() = 0;
	virtual void GetLgWritingSystemFactory(ILgWritingSystemFactory ** ppwsf) = 0;

	// Needed to make DLLs happy, when there is no ModuleEntry::s_tid (?????????)
	virtual AfLpInfo * GetLpInfo()
	{
		return NULL;
	}

	virtual void SaveData()
	{
	}

	/*------------------------------------------------------------------------------------------
		Get the Find pattern that is used for this application. This is just a convenient place
		to cache it.

		@return Pointer to the internally stored Find pattern.
	------------------------------------------------------------------------------------------*/
	IVwPattern * GetFindPattern()
	{
		if (!m_qxpat)
			m_qxpat.CreateInstance(CLSID_VwPattern);
		return m_qxpat;
	}

protected:
	// ENHANCE SteveMc(ShonK): If we go to a multi-threaded app, make m_mum a TLS thing so each
	// thread has its own.
	AfMenuMgr m_mum;

	AfReBarPtr m_qrebr;
	AfStatusBarPtr m_qstbr;
	Vector<AfToolBarPtr> m_vqtlbr;
	ComVector<IVwRootBox> m_vqrootb;

	// A find pattern that represents the current search.  Since it deals with TsStrings, it has
	// to be related to a particular writing system factory, hence a particular database.  This
	// allows that association, and also allows each main window to have its very own search.
	IVwPatternPtr m_qxpat;

	// Page setup variables.
	int m_dxmpLeftMargin;
	int m_dxmpRightMargin;
	int m_dympTopMargin;
	int m_dympBottomMargin;
	int m_dympHeaderMargin;
	int m_dympFooterMargin;
	POrientType m_nOrient;
	PgSizeType m_sPgSize;
	int m_dxmpPageWidth;
	int m_dympPageHeight;
	ITsStringPtr m_qtssHeader;
	StrUni m_stuHeaderDefault;
	ITsStringPtr m_qtssFooter;
	bool m_fHeaderOnFirstPage;
	StrApp m_strFullHelpUrl;	// For showing app-independent help.
	Point m_pt; // Point where context menu was initiated (screen coordinates)
	// Window initiating context menu (only non-NULL during context menus).
	AfWndPtr m_qwndContext;

	// Override this if something needs to be done after running page setup to save
	// the new settings. If you don't want the default initial page setup, you
	// need to arrange that also.
	virtual void SavePageSetup()
	{
	}

	virtual void CreateToolBar(AfToolBar * ptlbr, bool fShow, bool fBreak, int cx);

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void PostAttach(void);

	static LRESULT CALLBACK HelpMsgHook(int code, WPARAM wParam, LPARAM lParam);
	static HHOOK s_hhook;
	static LRESULT CALLBACK HelpMsgFilterHook(int code, WPARAM wParam, LPARAM lParam);
	static HHOOK s_hhookFilter;
	static bool s_fInMenu;
	// TODO DarrellZ: These next four lines need to be moved out of here into another class.
	// They are really variables for the formatting tool bar: the current color shown in
	// the formatting toolbar for foreground and background, and the image list used to
	// draw the format border combo, and the current item in that list.
	COLORREF m_clrFore;
	COLORREF m_clrBack;
	HIMAGELIST m_himlBorderCombo;
	int m_bpBorderPos;

	// The active root box. A window containing a root box and gaining focus should set this.
	// A window containing a root box being destroyed should clear it, if the window in
	// question has that root box. Do NOT clear when losing focus. This is used to activate
	// and deactivate the root box as the window activates and deactivates.
	// Note that this is NOT reference-counted. This pointer does NOT prevent deletion of the
	// root box; rather, deleting the root box should cause it to be cleared.
	IVwRootBox * m_prootbActive;
	bool m_fActiveWindow;

	// When the active root box is an item in the tool bar, remember the "real" main
	// root box.
	IVwRootBox * m_prootbPrev;

	FilterMenuNodeVec m_vfmn;		// Contain structures needed by the filters.
	FilterMenuNodeVec m_vfmnFlat;
	SortMenuNodeVec m_vsmn;			// Contains data needed by sorting.
	SortMenuNodeVec m_vsmnFlat;		// Contains data needed by sorting.
	AppSortInfo m_asiDefault;		// Definition of the default sort method.

	int m_wsUser;		// user interface writing system id.

	void _CopyMenuNode(FilterMenuNode ** ppfmnDst, FilterMenuNode * pfmnSrc);
	void _CopyMenuNodeVector(FilterMenuNodeVec & vfmnDst, FilterMenuNodeVec & vfmnSrc);
	void _AssignFieldTypes(IFwMetaDataCache * pmdc, FilterMenuNode * pfmn);
	void _CopyMenuNode(SortMenuNode ** ppfmnDst, SortMenuNode * pfmnSrc);
	void _CopyMenuNodeVector(SortMenuNodeVec & vfmnDst, SortMenuNodeVec & vfmnSrc);
	void _AssignFieldTypes(IFwMetaDataCache * pmdc, SortMenuNode * pfmn);

	// These are used to determine what the previous state of the toolbars was
	// if we're in Full Window mode.
	bool m_fFullWindow;
	DWORD m_dwOldToolbarFlags;

	virtual IActionHandler * GetActionHandler()
	{
		return NULL;
	}

	// Override this to load different default toolbar settings.
	virtual void LoadDefaultToolbarFlags(Vector<DWORD> & vflag, DWORD & dwBarFlags);

	/*******************************************************************************************
		Message handlers.
	*******************************************************************************************/
	virtual bool OnSize(int wst, int dxs, int dys);
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnContextMenu(HWND hwnd, Point pt);
	virtual bool OnChevronPushed(NMREBARCHEVRON * pnrc);

	virtual void OnReleasePtr();

	/*******************************************************************************************
		Command functions.
	*******************************************************************************************/
	virtual bool CmdFilePageSetup(Cmd * pcmd);
	virtual bool CmdFileImport(Cmd * pcmd);
	virtual bool CmsFileImport(CmdState & cms);
	virtual bool CmdTbToggle(Cmd * pcmd);
	virtual bool CmsTbUpdate(CmdState & cms);
	virtual bool CmdSbToggle(Cmd * pcmd);
	virtual bool CmsSbUpdate(CmdState & cms);
	virtual bool CmdSettingChange(Cmd * pcmd);

	// Process Insert External Link menu item.
	virtual bool ExternalLink(Cmd * pcmd, AfLpInfo * plpi);
	virtual bool CmsInsertLink(CmdState & cms);

	CMD_MAP_DEC(AfMainWnd);
};


#endif // !AFMAINWND_H

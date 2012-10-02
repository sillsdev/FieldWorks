/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfTagOverlay.h
Responsibility: Darrell Zook
Last reviewed: Not yet.

Description:
	This file contains class declarations for the following classes:
		AfTagOverlayGlobals - This class contains generic overlay stuff that can be called
			from any of the overlay window classes.
		AfTagOverlayPalette : AfWnd - This window draws a button for each tag in an overlay. It
			allows the user to scroll up and down in the list. When the user clicks on a
			button, the corresponding tag is applied to the current selection in the current
			view window, if there is one.
		AfTagOverlayTool : AfWnd - This window is the top level overlay tool window. It embeds
			an AfTagOverlayPalette and an AfTagTypeAheadEdit.
		AfTagTypeAheadEdit : AfWnd - This class is used to provide type-ahead functionality
			for the edit box at the top of the AfTagOverlayTool window. As the user types in
			this box, the palette and tree scrolls as necessary to keep the current tag visible.
		TlsOptDlgOvr : AfDialogView - This dialog view implements the tab in the Options/Tools
			dialog that allows the user to insert, delete, and modify overlays. It also allows
			the user to modify global/advanced overlay settings.
		NewOverlayDlg : AfDialog - This modal dialog allows the user to specify the name of a
			new overlay and the possibility list the tags will come from.
		AdvOverlayDlg : AfDialog - This modal dialog allows the user to modify global/advanced
			overlay settings.
		OvrPreviewWnd : AfVwScrollWnd - This window is a child of the AdvOverlayDlg and shows a
			preview of the global overlay flags.
		OvrPreviewVc : VwBaseVc - This is the view constructor for the OvrPreviewWnd class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TAG_OVERLAY_H
#define TAG_OVERLAY_H 1

class OverlayChsrTab;
class AfTagOverlayTree;
class AfTagOverlayPalette;
class AfTagOverlayTool;
class AfTagTypeAheadEdit;
class TlsOptDlg;
class TlsOptDlgOvr;
class NewOverlayDlg;
class AdvOverlayDlg;
class AfOverlayListBar;
class OvrPreviewWnd;
class OvrPreviewVc;
typedef GenSmartPtr<OverlayChsrTab> OverlayChsrTabPtr;
typedef GenSmartPtr<AfTagOverlayTree> AfTagOverlayTreePtr;
typedef GenSmartPtr<AfTagOverlayPalette> AfTagOverlayPalettePtr;
typedef GenSmartPtr<AfTagOverlayTool> AfTagOverlayToolPtr;
typedef GenSmartPtr<AfTagTypeAheadEdit> AfTagTypeAheadEditPtr;
typedef GenSmartPtr<TlsOptDlgOvr> TlsOptDlgOvrPtr;
typedef GenSmartPtr<NewOverlayDlg> NewOverlayDlgPtr;
typedef GenSmartPtr<AdvOverlayDlg> AdvOverlayDlgPtr;
typedef GenSmartPtr<OvrPreviewWnd> OvrPreviewWndPtr;
typedef GenSmartPtr<OvrPreviewVc> OvrPreviewVcPtr;


const int kcRecentOverlayTag = 6;

const int khvoPreview = 1;
const int kfragPreview = 0;
const int ktagPreview = 2;

class AfVwWnd;
class AfVwRootSite;
class UiColorCombo;
class AfLpInfo;
class PossListInfo;
class AfDbInfo;
class PossWebEvent;

/*----------------------------------------------------------------------------------------------
	This class provides global resources for the AfTagOverlay classes.
	Hungarian: tog
----------------------------------------------------------------------------------------------*/
class AfTagOverlayGlobals
{
public:
	AfTagOverlayGlobals();
	~AfTagOverlayGlobals();

	HIMAGELIST GetImageList();

	bool SaveOverlay(AfLpInfo * pdbi, IVwOverlay * pvo);

	void SetGlobalOverlayValues(int fof, wchar * pszFont, int dympFont, int ctagMax);
	void GetGlobalOverlayValues(int & fof, StrUni & stuFont, int & dympFont, int & ctagMax);

	enum
	{
		kmsgGetSelection = 1,
		kmsgApplyTag,
		kmsgRemoveTag,
	};

protected:
	HIMAGELIST m_himl;
	int m_fof; // VwOverlayFlags
	StrUni m_stuFont;
	int m_dympFont;
	int m_ctagMax;
	bool m_fLoadedGlobals;
};

extern AfTagOverlayGlobals g_tog;


/*----------------------------------------------------------------------------------------------
	This class is the list window inside of the top-level tool window. It contains buttons for
	each of the tags in an overlay.
	Hungarian: topl
----------------------------------------------------------------------------------------------*/
class AfTagOverlayPalette : public AfWnd
{
typedef AfWnd SuperClass;

public:
	AfTagOverlayPalette();
	~AfTagOverlayPalette();

	void Create(AfTagOverlayTool * ptot, int wid);

	void ChangeDisplayOption(PossNameType pnt);
	void AddPossibilities();

	void SetShowRecent(bool fShow);
	bool GetShowRecent()
		{ return m_fShowRecent; }
	void GetRecentTags(int * prgitag, int ctag)
	{
		AssertArray(prgitag, ctag);
		CopyItems(m_rgipssRecentSrt, prgitag, Min(ctag, kcRecentOverlayTag));
	}
	void SetRecentTags(int * prgitag, int ctag)
	{
		AssertArray(prgitag, ctag);
		CopyItems(prgitag, m_rgipssRecent, Min(ctag, kcRecentOverlayTag));
		CopyItems(prgitag, m_rgipssRecentSrt, Min(ctag, kcRecentOverlayTag));
	}

	void OnClickPss(int ipss);
	void OnSelectPss(int ipss);

	int ButtonFromPss(int ipss);

	void UpdateScrollbar();

	int GetMinWidth();

	void Reset()
	{
		m_vipssButton.Clear();
	}

protected:
	virtual void PostAttach();
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnSize(int wst, int dxp, int dyp);
	virtual bool OnPaint(HDC hdcDef);
	virtual bool OnMouseMove(uint grfmk, int xp, int yp);
	virtual bool OnLButtonUp(uint grfmk, int xp, int yp);
	virtual bool OnLButtonDown(uint grfmk, int xp, int yp);
	virtual bool OnContextMenu(HWND hwnd, Point pt);
	virtual bool OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags);
	virtual bool OnVScroll(int nSBCode, int nPos, HWND hwndSbar);

	int GetPssFromCoord(int xp, int yp);
	void DrawButtons(HDC hdc, Rect & rc);
	void DrawRecentButtons(HDC hdc, Rect & rc);
	void DrawButton(HDC hdc, int ibtn, Rect & rc);

	int GetButtonCountPerRow(int dxpRow, int * pdxpButton = NULL);

	enum
	{
		kdxpButton = 43,
		kdypButton = 25,

		kdzpBorder = 2,
		kdzpMargin = 4,
	};

	AfTagOverlayTool * m_ptot;
	PossNameType m_pnt;

	bool m_fShowRecent;
	int m_ibtnTop;
	int m_ibtnTopMax;
	int m_ibtnPressed;
	int m_ibtnSelected;
	bool m_fInMostRecent;
	int m_ipssHover;
	HWND m_hwndToolTip;
	HFONT m_hfont;
	int m_rgipssRecent[kcRecentOverlayTag];
	int m_rgipssRecentSrt[kcRecentOverlayTag];
	int m_dxpButton;
	int m_dypButton;
	int m_dypOffset;
	int m_crowVisible;
	Vector<int> m_vipssButton;
};


/*----------------------------------------------------------------------------------------------
	This class is the tree window inside of the top-level tool window. It contains an item for
	each of the tags in an overlay.
	Hungarian: totr
----------------------------------------------------------------------------------------------*/
class AfTagOverlayTree : public AfWnd
{
typedef AfWnd SuperClass;
public:
	//AfTagOverlayTree() : m_pplddDragDrop(NULL) { }
	AfTagOverlayTree();
	~AfTagOverlayTree();

	void Create(AfTagOverlayTool * ptot, int wid);
	void ChangeDisplayOption(PossNameType pnt);

	//void SetDragDropHandler(PossListDragDrop * pldd) { m_pplddDragDrop = pldd; }
	bool AddPossibilities();
	void GetExpandedItems(HTREEITEM hti, Vector<int> & vipss);
	bool SetExpandedItems(HTREEITEM hti, Vector<int> & vipss);

	HTREEITEM FindHvo(HTREEITEM hti, HVO hvoPss);
	HTREEITEM FindPss(int ipss)
		{ return FindPssHelper(TreeView_GetRoot(m_hwnd), ipss);	};

	void OnClickPss(int ipss);
	void OnSelectPss(int ipss);

	void Reset()
	{
		// Nothing to do here yet.
	}

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnPaint(HDC hdcDef);
	virtual bool OnNotifyThis(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnGetDispInfo(NMTVDISPINFO * pntdi);

	HTREEITEM FindPssHelper(HTREEITEM hti, int ipss);

	enum
	{
		// These numbers should not be changed, because code in OnNotifyThis assumes
		// these specific numbers.
		kstatUnchecked = 1,
		kstatDisabled = 2,
		kstatChecked = 3,
	};

	//PossListDragDrop * m_pplddDragDrop;
	AfTagOverlayTool * m_ptot;
	PossNameType m_pnt;
	HIMAGELIST m_himlStates;
	bool m_fUpdating;
};


/*----------------------------------------------------------------------------------------------
	This class is the top-level tool window.
	Hungarian: tot
----------------------------------------------------------------------------------------------*/
class AfTagOverlayTool : public AfDialog, public PossWebEventNotify, public PossListNotify
{
typedef AfDialog SuperClass;
friend AfTagOverlayTree;

public:
	AfTagOverlayTool();
	~AfTagOverlayTool();

	void Create(AfOverlayListBar * polb, HWND hwndPar, AfLpInfo * plpi, IVwOverlay * pvo);
	void LoadOverlay();

	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	virtual void SaveSettings(const achar * pszRoot, bool fRecursive = true);

	int GetOverlayIndex()
		{ return m_qlpi->GetOverlayIndex(this); }
	AfTagOverlayPalette * GetOverlayList()
		{ return m_qtopl.Ptr(); }

	void SelectTab(int itab);

	// The subclass should override these.
	virtual bool OnConfigureTag(int iovr, int itag)
		{ return false; } // Do nothing.
	virtual bool OnChangeOverlay(int iovr)
		{ return false; } // Do nothing.

	void EnableWindow(bool fEnable);
	bool IsEnabled()
		{ return m_fEnabled; }
	void HideExcludedTags(bool fHideExcluded);

	int GetVisiblePssCount();
	PossItemInfo * GetVisiblePssInfo(int ipssVisible, int * pipss = NULL);

	bool IsPssInSel(int ipss);
	bool IsTagInSel(int itag);
	int TagIndexFromPss(int ipss);
	int TagIndexFromHvo(HVO hvoPss);

	PossListInfo * GetPossListInfo()
		{ return m_qpli.Ptr(); }

	int GetSelectedPss()
		{ return m_ipssSelected; }
	void SetSelectedPss(int ipss, bool fUpdateHelp = true);
	void ClickPss(int ipss);

	void ModifyPss(int ipss, bool fInsert, bool fRecursive);

	void UpdateEditText(int ipss);

	void ShowHelp(bool fShow, bool fForceShow = false);
	void UpdateHelpWindow();
	void ChangeDisplayOption(PossNameType pnt);
	virtual HRESULT UpdateTree(wchar * pszUrl);
	void UpdateStatus(bool fForceUpdate = false);
	void SelectPss(HWND hwndEdit, const wchar * prgch);

	virtual void ListChanged(int nAction, HVO hvoPssl, HVO hvoSrc, HVO hvoDst, int ipssSrc,
		int ipssDst);

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnSize(int wst, int dxp, int dyp);
	virtual bool OnSizing(int wst, RECT * prc);
	virtual bool OnCommand(int cid, int nc, HWND hctl);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool OnTimer(int nId);
	virtual void OnReleasePtr();
	virtual bool OnHelpInfo(HELPINFO * phi);
	virtual bool OnApply(bool fClose)
	{
		// This needs to be overridden so the dialog doesn't close
		// when the Enter key is pressed.
		return true;
	}
	virtual bool OnCancel();

	// Command handlers.
	virtual bool CmdChangeDisplay(Cmd * pcmd);
	virtual bool CmsChangeDisplay(CmdState & cms);
	virtual bool CmdHideExclude(Cmd * pcmd);
	virtual bool CmsHideExclude(CmdState & cms);
	virtual bool CmdConfigure(Cmd * pcmd);
	virtual bool CmdShowHelp(Cmd * pcmd);
	virtual bool CmdShowRecent(Cmd * pcmd);
	virtual bool CmsShowRecent(CmdState & cms);
	virtual bool CmdModifyTag(Cmd * pcmd);
	virtual bool CmsModifyTag(CmdState & cms);

	enum
	{
		kxpEdit = 40,
		kdypEdit = 20,

		kdxpBtnOptions = 65,
		kdxpBtnTagTest = 100,
		kdypButton = 24,

		kdypDefHeight = 300,

		kdzpBorder = 7,

		kitabTree = 0,
		kitabPalette = 1,

		kmaskCurTabSel = 0x1,
		kmaskShowHelp = 0x2,
		kmaskShowRecent = 0x4,
		kmaskHideExcluded = 0x8,
	};

	AfTagOverlayTreePtr m_qtotr;
	AfTagOverlayPalettePtr m_qtopl;
	AfButtonPtr m_qbtnOptions;
	GenSmartPtr<AfLpInfo> m_qlpi;
	GenSmartPtr<AfOverlayListBar> m_qolb;
	IVwOverlayPtr m_qvo;
	PossListInfoPtr m_qpli;
	int m_pssl;
	GUID m_guid; // GUID that identifies the list more precisely.
	PossNameType m_pnt;
	HWND m_hwndTab;
	HWND m_hwndGrip;
	bool m_fShowHelp;
	int m_dxpHelp;
	int m_dxpOldLeft;
	int m_iCurTabSel; // 0 = Tree, 1 = Palette
	HWND m_hwndTool;
	HWND m_hwndHelp;
	ComSmartPtr<PossWebEvent> m_qpwe;
	IWebBrowser2Ptr m_qweb2;
	HIMAGELIST m_himlCold;
	HIMAGELIST m_himlHot;
	Vector<bool> m_vfTagInSel;
	Vector<HVO> m_vhvoTag;
	Vector<int> m_vipssVisible;
	bool m_fEnabled;
	int m_ipssSelected;
	bool m_fHideExcluded;
	bool m_fUpdating;
	HICON m_hiconHideExcluded;

	CMD_MAP_DEC(AfTagOverlayTool);
};


/*----------------------------------------------------------------------------------------------
	This class paints the tab control without flicker.
	Hungarian: octab
----------------------------------------------------------------------------------------------*/
class OverlayChsrTab : public AfWnd
{
typedef AfWnd SuperClass;
public:
	void SubclassTab(HWND hwnd)
	{
		SubclassHwnd(hwnd);
	}

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnPaint(HDC hdcDef);
};


/*----------------------------------------------------------------------------------------------
	This class is used to trap the backspace used in the edit box.
	Hungarian: ttae
----------------------------------------------------------------------------------------------*/
class AfTagTypeAheadEdit : public AfWnd
{
public:
	void SubclassEdit(HWND hwnd)
		{ SubclassHwnd(hwnd); }

	static bool s_fExtraBackspace;

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
};


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Overlay dialog pane in the Tools Option dialog.
	Hungarian: tod
----------------------------------------------------------------------------------------------*/
class TlsOptDlgOvr : public AfDialogView
{
typedef AfDialogView SuperClass;

public:
	TlsOptDlgOvr(TlsOptDlg * ptod);
	~TlsOptDlgOvr();

	typedef enum
	{
		kosNormal = 0,
		kosModified,
		kosInserted,
		kosDeleted,
	} OverlayState;

	typedef struct
	{
		HVO m_hvoPss;
		COLORREF m_clrFore;
		COLORREF m_clrBack;
		COLORREF m_clrUnder;
		int m_unt;
		OverlayState m_os;
	} OverlayTagInfo;

	typedef struct
	{
		StrUni m_stuName;
		HVO m_hvo;
		HVO m_hvoPssl;
		Vector<OverlayTagInfo> m_voti;
		OverlayState m_os;
		GenSmartPtr<PossListInfo> m_qpli;
	} OverlayInfo;

	void SetDialogValues(AfLpInfo * plpi, int iovr = 0, int itag = 0);
	void GetDialogValues(Vector<OverlayInfo> & voi);
	bool WasModified()
		{ return m_fModified; }

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool Apply();
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnEndLabelEdit(NMLVDISPINFO * plvdi, long & lnRet);
	virtual bool OnOverlayChange(int iovr);
	virtual bool OnTagChange();
	virtual bool OnGetDispInfo(NMLVDISPINFO * pnmdi);
	virtual bool OnMeasureChildItem(MEASUREITEMSTRUCT * pmis);
	virtual bool OnDrawChildItem(DRAWITEMSTRUCT * pdis);
	virtual bool OnCustomDrawTagList(NMLVCUSTOMDRAW * pncd, long & lnRet);
	virtual void OnReleasePtr()
	{
		m_qccmbFore.Clear();
		m_qccmbBack.Clear();
		m_qccmbUnder.Clear();
	}
	void OnComboSelEnd(int ctid);
	virtual bool OnTagEditUpdate();

	void UpdateOverlayList();
	void AddOverlay();
	void CopyOverlay();
	void DeleteOverlay();
	void ChooseTags();
	void UpdateTagList();

	GenSmartPtr<UiColorCombo> m_qccmbFore;
	GenSmartPtr<UiColorCombo> m_qccmbBack;
	GenSmartPtr<UiColorCombo> m_qccmbUnder;
	COLORREF m_clrTagFore;
	COLORREF m_clrTagBack;
	COLORREF m_clrTagUnder;
	GenSmartPtr<AfLpInfo> m_qlpi;
	TlsOptDlg * m_ptod;

	int m_iovrInitial;
	int m_iovrCurrent;
	int m_itagInitial;
	bool m_fUpdating;
	bool m_fModified;
	// This gives the text height of one item in the tag listview.
	int m_dypText;
	// This is the font that is used to draw the tags in the tag listview.
	HFONT m_hfont;
	HIMAGELIST m_himlTag;

	Vector<OverlayInfo> m_voi;
};


/*----------------------------------------------------------------------------------------------
	This class allows the user to create a new overlay and base it on a choices list.
	Hungarian: nod
----------------------------------------------------------------------------------------------*/
class NewOverlayDlg : public AfDialog
{
typedef AfDialog SuperClass;

public:
	NewOverlayDlg();
	~NewOverlayDlg();

	// Sets initial values for the dialog controls, prior to displaying the dialog.
	void SetDialogValues(AfLpInfo * plpi);

	// Retrieve values.
	void GetDialogValues(StrApp & strName, HVO & hvoPssl, bool & fIncludeAll);

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);

	StrApp m_strName;
	HVO m_hvoPssl;
	bool m_fIncludeAll;
	GenSmartPtr<AfLpInfo> m_qlpi;
};


/*----------------------------------------------------------------------------------------------
	This class allows the user to change advanced settings for overlays.
	Hungarian: aod
----------------------------------------------------------------------------------------------*/
class AdvOverlayDlg : public AfDialog
{
typedef AfDialog SuperClass;

public:
	AdvOverlayDlg();

	void SetUserWs(int wsUser)
	{
		Assert(wsUser);
		m_wsUser = wsUser;
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);
	/*bool OnMeasureChildItem(MEASUREITEMSTRUCT * pmis);
	bool OnDrawChildItem(DRAWITEMSTRUCT * pdis);
	void DrawPreview(HDC hdc, HWND hwndItem, RECT rcItem);*/
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);

	OvrPreviewWndPtr m_qopw;
	IVwCacheDaPtr m_qvcd;
	IVwOverlayPtr m_qvo;

	bool CreateOverlay();
	int GetOverlayFlags();
	int GetFontSize();
	void GetFontName(StrUni & stuName);
	int GetMaxTags();

	GUID m_uid[3]; // Contains dummy GUIDs for the overlay tags.

	int m_wsUser;	// user interface writing system id.
};


/*----------------------------------------------------------------------------------------------
	This class shows a preview of the overlay flags. It's a child of the AdvOverlayDlg dialog.
	Hungarian:  opw
----------------------------------------------------------------------------------------------*/
class OvrPreviewWnd : public AfVwScrollWnd
{
typedef AfVwScrollWnd SuperClass;

public:
	void Create(HWND hwndPar, int wid, IVwCacheDa * pvcd, IVwOverlay * pvo, int wsUser);

	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb);

	void UpdateOverlay()
	{
		CheckHr(m_qrootb->putref_Overlay(m_qvo));
	}

protected:
	OvrPreviewVcPtr m_qopvc;
	IVwCacheDaPtr m_qvcd;
	IVwOverlayPtr m_qvo;
};


/*----------------------------------------------------------------------------------------------
	This class implements the main view constructor for the overlay preview window.

	Hungarian: opvc.
----------------------------------------------------------------------------------------------*/
class OvrPreviewVc : public VwBaseVc
{
typedef VwBaseVc SuperClass;

public:
	//:> IVwViewConstructor methods.
	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
};


#endif // !TAG_OVERLAY_H

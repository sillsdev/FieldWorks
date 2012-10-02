/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: PossChsrDlg.h
Responsibility: Darrell Zook
Last reviewed: Not yet.

Description:
	This file contains class declarations for the following classes:
		PossChsrDlg : AfDialog - This class creates and manages the main chooser dialog. It
			contains windows of the three following classes.
		PossChsrTree : AfWnd - This class is used to manage the two tree controls on the
			PossChsrDlg window. It is needed to get a context menu and to keep the tree view
			from flickering when it is resized.
		PossChsrTab : AfWnd - This class is used to manage the tab control on the PossChsrDlg
			window. It is needed to keep the tab control from flickering when it is resized.
		PossChsrComboEdit : AfWnd - This class is used to manage the edit boxes within the two
			combo boxes on the PossChsrDlg window. It is needed to handle type-ahead correctly.
		PossWebEvent : IDispatch - This class is used to monitor events raised by the
			IWebBrowser2 control contained on the PossChsrDlg window.
		PossWebEventNotify - This is an abstract class that contains common WebBrowser event
			notification. It allows us to reuse the PossWebEvent class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef POSS_CHSR_DLG_H
#define POSS_CHSR_DLG_H 1

#pragma warning(disable: 4244)
#pragma warning(disable: 4189)
#define _ATL_APARTMENT_THREADED
#undef _ATL_FREE_THREADED
#include <atlcom.h>
#include <exdispid.h>
#pragma warning(default: 4244)
#pragma warning(default: 4189)


class PossChsrDlg;
class PossChsrTree;
class PossChsrTab;
class PossChsrComboEdit;
class PossWebEvent;
typedef GenSmartPtr<PossChsrDlg> PossChsrDlgPtr;
typedef GenSmartPtr<PossChsrTree> PossChsrTreePtr;
typedef GenSmartPtr<PossChsrTab> PossChsrTabPtr;
typedef GenSmartPtr<PossChsrComboEdit> PossChsrComboEditPtr;
typedef ComSmartPtr<PossWebEvent> PossWebEventPtr;
typedef ComSmartPtr<DWebBrowserEvents2> DWebBrowserEvents2Ptr;


#define kchtiHistory 12


/*----------------------------------------------------------------------------------------------
	This class is used to handle the WebEvent callback when the current page changes.
	Hungarian: pwe
----------------------------------------------------------------------------------------------*/
class PossWebEventNotify
{
public:
	virtual HRESULT UpdateTree(wchar * pszUrl) = 0;
};


/*----------------------------------------------------------------------------------------------
	Implements the view constructor for the choices window
	@h3{Hungarian: pccvc}
----------------------------------------------------------------------------------------------*/
class PossChsrChoicesVc : public VwBaseVc
{
public:
	void InitValues(PossChsrDlg * ppcd)
	{
		AssertPtr(ppcd);
		m_ppcd = ppcd;
	}
	STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag);
protected:
	PossChsrDlg * m_ppcd; // Access to PossChsrDlg data
//	static int ItemsFor(PossItemInfo * ppii, TlsStatsDlg * ptsd);
};

DEFINE_COM_PTR(PossChsrChoicesVc);

/*----------------------------------------------------------------------------------------------
	@h3{Hungarian: tsl}
----------------------------------------------------------------------------------------------*/
class  PossChsrChoices : public AfVwScrollWnd
{
	friend class PossChsrChoicesVc;
public:
	PossChsrChoices(void);
	void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf, IVwRootBox ** pprootb);
	void InitValues(PossChsrDlg * ppcd)
	{
		AssertPtr(ppcd);
		m_ppcd = ppcd;
	}
	void Create(HWND hwndPar, int wid, IVwCacheDa * pvcd, int wsUser);
	void Redraw();
	virtual COLORREF GetWindowColor()
	{
		return ::GetSysColor(COLOR_BTNFACE);
	}
protected:
	IVwCacheDaPtr m_qvcd;
	PossChsrChoicesVcPtr m_qpccvc;
	PossChsrDlg * m_ppcd;
};

DEFINE_COM_PTR(PossChsrChoices);

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Possibilities List Chooser Dialog.
	Hungarian: plc.
----------------------------------------------------------------------------------------------*/
class PossChsrDlg : public AfDialog, public PossWebEventNotify, public PossListNotify
{
typedef AfDialog SuperClass;
friend PossChsrTree;

public:
	PossChsrDlg();
	~PossChsrDlg();

	// Sets initial values for the dialog controls, prior to displaying the dialog.
	void SetDialogValues(HVO psslId, int ws, HVO pssIdSel, AppOverlayInfo * paoi = NULL);
	void SetDialogValues(HVO psslId, int ws, Vector<HVO> & vpssIdSel,
		StrApp * pstrOverlayName = NULL);

	// Retrieve values.
	void GetDialogValues(Vector<HVO> & vpssIdSel);
	void GetDialogValues(HVO & pssIdSel);

	void SelectTab(int itab);
	void ShowHelp(bool fShow);
	void ChangeDisplayOption(PossNameType pnt);
	void ShowCurrentChoices(bool fShow);

	virtual HRESULT UpdateTree(wchar * pszUrl);

	void SetContextIndex(int ipss)
		{ m_ipssContext = ipss; }
	virtual void ListChanged(int nAction, HVO hvoPssl, HVO hvoSrc, HVO hvoDst, int ipssSrc,
		int ipssDst);
	void SetFromEditor(bool fFromEditor)
	{
		m_fFromEditor = fFromEditor;
	}
	bool GetShowCurrent(void)
	{
		return m_fShowCurrent;
	}
	HWND GetHwndTreeChoiceList(void)
	{
		return m_rghwndTree[kiChoiceList];
	}
	void GetCheckedItems(HWND hwndTree, HTREEITEM hti, Vector<int> & vipss);
	PossListInfoPtr GetPossListInfo(void)
	{
		return m_qpli;
	}
	PossNameType GetPossNameType(void)
	{
		return m_pnt;
	}

protected:
	enum
	{
		kCheckStateChange = WM_APP + 1, // Message sent after treeview checkbox is toggled.
	};
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);
	virtual bool OnCancel();
	virtual bool OnComboChange(NMHDR * pnmh, long & lnRet);
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnSize(int wst, int dxp, int dyp);
	virtual bool OnSizing(int wse, RECT * prc);
	virtual bool OnGetDispInfo(NMTVDISPINFO * pntdi);
	virtual bool OnSelChanged(int ipss);
	virtual bool OnEditUpdate(NMHDR * pnmh);
	virtual bool OnClick(NMHDR * pnmh);
	virtual bool ContextMenu(HWND hwnd, Point pt, TPMPARAMS * ptpm);
	virtual bool OnHelpInfo(HELPINFO * phi);

	bool AddPossibilities(HVO hvoSel = NULL);
	bool AddKeywords(achar * pszKeyword);
	HTREEITEM FindString(HWND hwndTree, HTREEITEM hti, achar * psz, int & cchMatched);
	void PerformComboAction();

	void UpdateCurrentChoices();
	void UpdateSelectedItems(HWND hwndTree, HTREEITEM hti);
	void ResetCheckBoxes(HWND hwndTree, HTREEITEM htiCandidate, HTREEITEM htiProtect);
	void UpdateCheckBoxes(HWND hwndTree, HTREEITEM hti);
	HTREEITEM FindHvo(HWND hwndTree, HTREEITEM hti, HVO hvoPss);
	HTREEITEM FindPss(HWND hwndTree, HTREEITEM hti, int ipss);
	void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	void SaveSettings(const achar * pszRoot, bool fRecursive = true);

	void CheckItem(HTREEITEM hti, Vector<int> & vipssChanged, bool fInsert, bool fRecursive);

	/*******************************************************************************************
		Command functions.
	*******************************************************************************************/
	virtual bool CmdChangeDisplay(Cmd * pcmd);
	virtual bool CmsChangeDisplay(CmdState & cms);
	virtual bool CmdModify(Cmd * pcmd);
	virtual bool CmdRename(Cmd * pcmd);
	virtual bool CmdMerge(Cmd * pcmd);
	virtual bool CmdInsert(Cmd * pcmd);
	virtual bool CmdDelete(Cmd * pcmd);
	virtual bool CmsRequireItem(CmdState & cms);
	virtual bool CmsNotRequireItem(CmdState & cms);
	virtual bool CmdModifyTag(Cmd * pcmd);
	virtual bool CmsModifyTag(CmdState & cms);
	virtual bool CmdViewRefresh(Cmd * pcmd);

	// These are indexes into the m_rghwndTree and m_rghwndHistCombo arrays.
	enum
	{
		kiChoiceList = 0,
		kiKeywordSearch = 1,
	};

	enum
	{
		kmaskNameType = 0x3,
		kmaskCurTabSel = 0x4,
		kmaskShowHelp = 0x8,
		kmaskShowCurrent = 0x10,
	};

	// Member variables.
	IVwCacheDaPtr m_qvcd;
	int m_wsUser;
	PossListInfoPtr m_qpli;
	Vector<HVO> m_vpssId; // The IDs of the selected items.
	HVO m_psslId; // The Id of the list we are showing.
	int m_ws;
	bool m_fAtomic;
	bool m_fOverlay;
	StrApp * m_pstrOverlayName; // This will be NULL unless m_fOverlay is true.

	PossChsrTreePtr m_qpctCh;
	PossChsrChoicesPtr m_qpcc; // Actual choices window.
	HWND m_hwndChoices;
	HWND m_hwndTab;
	HWND m_rghwndTree[2]; // 0 = Choices List, 1 = Keyword Search
	HWND m_rghwndHistCombo[2]; // 0 = Choices List, 1 = Keyword Search
	HWND m_hwndGrip;
	HWND m_hwndTool;
	HIMAGELIST m_himlCold;
	HIMAGELIST m_himlHot;
	HWND m_hwndHelp;
	IWebBrowser2Ptr m_qweb2;
	int m_iCurTabSel; // 0 = Choices List, 1 = Keyword Search
	bool m_fShowHelp;
	bool m_fShowCurrent;
	PossNameType m_pnt;
	int m_cHistBack;
	int m_cHistForward;
	PossWebEventPtr m_qpwe;
	int m_ipssContext;

	int m_dypTab;
	int m_dypTree;
	int m_ypOk;
	int m_dxpOk;
	int m_dxpButtonSep;
	int m_dxpHelp;
	int m_dypHelp;
	int m_dypCurSel;
	int m_dxpMin;

	PossListDragDrop m_plddDragDrop;
	AppOverlayInfo * m_paoi;
	bool m_fFromEditor;
	HVO m_hvoTarget; // Used to select correct item in tree view after a drag/insert.

	bool m_fIgnoreSelChange;

	CMD_MAP_DEC(PossChsrDlg);
};


/*----------------------------------------------------------------------------------------------
	This class paints the tree without flicker.
	Hungarian: pct
----------------------------------------------------------------------------------------------*/
class PossChsrTree : public TssTreeView //AfWnd
{
typedef TssTreeView SuperClass;
public:
	PossChsrTree() : m_pplddDragDrop(NULL) { }
	void SetDragDropHandler(PossListDragDrop * pldd) { m_pplddDragDrop = pldd; }

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnPaint(HDC hdcDef);

	PossListDragDrop * m_pplddDragDrop;
};


/*----------------------------------------------------------------------------------------------
	This class paints the tab control without flicker.
	Hungarian: pctab
----------------------------------------------------------------------------------------------*/
class PossChsrTab : public AfWnd
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
	This class is used to trap the backspace used in combo boxes.
	Hungarian: pcce
----------------------------------------------------------------------------------------------*/
class PossChsrComboEdit : public AfWnd
{
public:
	void SubclassEdit(HWND hwnd)
		{ SubclassHwnd(hwnd); }

	static bool s_fExtraBackspace;

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
};


/*----------------------------------------------------------------------------------------------
	This class is used as a callback for the HTML control to find out when the control has
	finished showing an HTML page and to update the toolbar buttons.
	Hungarian: pwe
----------------------------------------------------------------------------------------------*/
class PossWebEvent : public IDispatch // DWebBrowserEvents2
{
public:
	PossWebEvent(PossWebEventNotify * ppwen, HWND hwndTool, AfLpInfo * plpi);

	void SetWebBrowser(IWebBrowser2 * pweb2)
	{
		AssertPtr(pweb2);
		m_qweb2 = pweb2;
	}

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// IDispatch methods.
	STDMETHOD(GetTypeInfoCount)(UINT * pctinfo);
	STDMETHOD(GetTypeInfo)(UINT iTInfo, LCID lcid, ITypeInfo ** ppTInfo);
	STDMETHOD(GetIDsOfNames)(REFIID riid, LPOLESTR * rgszNames, UINT cNames, LCID lcid,
		DISPID * rgDispId);
	STDMETHOD(Invoke)(DISPID dispIdMember, REFIID riid, LCID lcid, WORD wFlags,
		DISPPARAMS * pDispParams, VARIANT * pVarResult, EXCEPINFO * pExcepInfo,
		UINT * puArgErr);

	// Helper methods.
	void UpdateHelp(PossListInfo * pppli, HVO hvoPssId);
	void UpdateDefaultHelp(PossListInfo * ppli, HVO hvoPssId);

protected:
	long m_cref; // COM reference count.
	PossWebEventNotify * m_ppwen;
	HWND m_hwndTool;
	AfLpInfoPtr m_qlpi;
	IWebBrowser2Ptr m_qweb2;
	PossListInfoPtr m_qpli;
	HVO m_hvoPssId;
};


/*----------------------------------------------------------------------------------------------
	This class is the Poss Chsr Insert and Rename dialog.
----------------------------------------------------------------------------------------------*/
class PossChsrIns : public AfDialogView
{
	typedef AfDialogView SuperClass;
public:
	PossChsrIns();

	void SetDialogValues(HVO psslId, int ws, int ipss, bool fInsert, LPCOLESTR pszName,
		LPCOLESTR pszAbbr, LPCOLESTR pszDesc);
	void GetDialogValues(StrUni & stuName, StrUni & stuAbbr, StrUni & stuDesc);
	virtual bool OnApply(bool fClose);

	class PssTssEdit;
	typedef GenSmartPtr<PssTssEdit> PssTssEditPtr;
	/*------------------------------------------------------------------------------------------
	This class is used in place of TssEdit so we can receive notifications of changes to the
	text in the control and so tabbing will work between edit boxes.
	------------------------------------------------------------------------------------------*/
	class PssTssEdit : public TssEdit
	{
		typedef TssEdit SuperClass;
	public:
		PssTssEdit()
		{
			m_nEditable = ktptIsEditable;
		}
	protected:
		virtual bool OnChange();
		virtual bool OnKillFocus(HWND hwndNew);
		virtual bool OnSetFocus(HWND hwndOld, bool fTbControl = false);
	};

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	void SetAbbr();
	void SetName();

	// Member variables.
	PossListInfoPtr m_qpli;
	HVO m_psslId;
	int m_ws;
	int m_ipss;
	bool m_fInsert;
	StrUni m_stuName;
	StrUni m_stuAbbr;
	StrUni m_stuDesc;
	PssTssEditPtr m_qteName;
	PssTssEditPtr m_qteAbbr;
	PssTssEditPtr m_qteDesc;
	int m_iMerge;	// -1 for no merge, otherwise this is the index of where to merge into.
	int m_atid; // Identifies the special accelerator table loaded to handle edit commands.
};

typedef GenSmartPtr<PossChsrIns> PossChsrInsPtr;

#endif // !POSS_CHSR_DLG_H

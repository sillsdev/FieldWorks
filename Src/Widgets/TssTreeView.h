/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TssTreeView.h
Responsibility: Rand Burgett
Last reviewed:

	This is the base Sdk class of a TreeView designed for TsStrings.
	This class is used for Sdk applications, and is also the base for an ActiveX control.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TSSTREEVIEW_H
#define TSSTREEVIEW_H 1

typedef struct FW_TVITEM{
	UINT      mask;
	HTREEITEM hItem;
	UINT      state;
	UINT      stateMask;
	ITsStringPtr qtss;
	int       iImage;
	int       iSelectedImage;
	int       cChildren;
	LPARAM    lParam;
	FW_TVITEM::FW_TVITEM()
	{
		// Init these because TssTreeView::SetItem()'s callers often do not init them.
		iImage = 0;
		iSelectedImage = 0;
	}
} FW_TVITEM, FAR *LPFW_TVITEM;

typedef struct FW_TVITEMEX{
	UINT      mask;
	HTREEITEM hItem;
	UINT      state;
	UINT      stateMask;
	ITsStringPtr qtss;
	int       iImage;
	int       iSelectedImage;
	int       cChildren;
	LPARAM    lParam;
	int       iIntegral;
} FW_TVITEMEX, FAR *LPFW_TVITEMEX;

typedef struct FW_NMTREEVIEW {
	NMHDR       hdr;
	UINT        action;
	FW_TVITEM    itemOld;
	FW_TVITEM    itemNew;
	POINT       ptDrag;
} FW_NMTREEVIEW, FAR *LPFW_NMTREEVIEW;

typedef struct FW_NMTVKEYDOWN {
	NMHDR hdr;
	WORD wVKey;
	UINT flags;
} FW_NMTVKEYDOWN, FAR *LPFW_NMTVKEYDOWN;

typedef struct FW_NMTVDISPINFO {
	NMHDR hdr;
	FW_TVITEM item;
} FW_NMTVDISPINFO, FAR *LPFW_NMTVDISPINFO;

typedef struct FW_TVINSERTSTRUCT {
	HTREEITEM hParent;
	HTREEITEM hInsertAfter;
	FW_TVITEMEX itemex;
} FW_TVINSERTSTRUCT, FAR *LPFW_TVINSERTSTRUCT;

typedef struct FwTreeItem
{
	ITsStringPtr qtss;
	LPARAM lParam;
	int dxp;
} FwTreeItem;

// The reason we are using WM_APP here instead of WM_USER is because on some machines, using
// WM_USER as the base caused messages to be converted to another message somehow. I
// (DarrellZ) don't understand how the messages were getting converted, but using WM_APP
// seems to work.
enum
{
	FW_TVM_GETITEM = WM_APP +1,
	FW_TVM_SETITEM,
//	FW_TVM_TVINSERTSTRUCT,
	FW_TVN_BEGINDRAG,
	FW_TVN_BEGINLABELEDIT,
	FW_TVN_BEGINRDRAG,
	FW_TVN_DELETEITEM,
	FW_TVN_ENDLABELEDIT,
	FW_TVN_GETDISPINFO,
	FW_TVN_ITEMEXPANDED,
	FW_TVN_ITEMEXPANDING,
	FW_TVN_KEYDOWN,
	FW_TVN_SELCHANGED,
	FW_TVN_SELCHANGING,
	FW_TVN_SETDISPINFO,
	FW_TVN_SINGLEEXPAND,
};

/*----------------------------------------------------------------------------------------------
	Macros for list view messages that we have overridden:
	Fw_TreeView_GetItem
	Fw_TreeView_SetItem
	Fw_TreeView_InsertItem
----------------------------------------------------------------------------------------------*/
#define Fw_TreeView_GetItem(hwnd, pitem) \
	(BOOL)SendMessage((hwnd), FW_TVM_GETITEM, 0, (LPARAM)(FW_TVITEM *)(pitem))

#define Fw_TreeView_SetItem(hwnd, pitem) \
	(BOOL)SendMessage((hwnd), FW_TVM_SETITEM, 0, (LPARAM)(const FW_TVITEM *)(pitem))

#define Fw_TreeView_InsertItem(hwnd, ptis) \
	(HTREEITEM)SendMessage((hwnd), TVM_INSERTITEM, 0, (LPARAM)(FW_TVINSERTSTRUCT *)(ptis))

class AfToolTipWnd;
DEFINE_COM_PTR(AfToolTipWnd);


/*----------------------------------------------------------------------------------------------
	The main view constructor for the context help window.
	Hungarian: ttvvc.
----------------------------------------------------------------------------------------------*/
class TssTreeViewVc : public VwBaseVc
{
	typedef VwBaseVc SuperClass;

public:
	// IVwViewConstructor methods.
	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
};

typedef GenSmartPtr<TssTreeViewVc> TssTreeViewVcPtr;

/*----------------------------------------------------------------------------------------------
	This class supports the TsString Tree View. It uses a reflector outside window
	to catch and process Windows messages.
	Hungarian: ttvw
----------------------------------------------------------------------------------------------*/
class TssTreeView : public AfVwWnd
{
typedef AfVwWnd SuperClass;

public:
	// Constructor and destructor.
	TssTreeView();

	virtual void SubclassTreeView(HWND hwnd);

/***********************************************************************************************
	Methods
***********************************************************************************************/
	bool GetItemRect(HTREEITEM hItem, RECT * pRect, bool fTextOnly);
	bool GetItemRect(bool fTextOnly, RECT * pRect);
	UINT GetCount();
	UINT GetIndent();
	void SetIndent(UINT nIndent);
	HIMAGELIST GetImageList(UINT nImageList);
	HIMAGELIST SetImageList(int nImageListType, HIMAGELIST hImageList);
	HTREEITEM GetNextItem(UINT nCode, HTREEITEM hItem);
	HTREEITEM GetChildItem(HTREEITEM hItem);
	HTREEITEM GetNextSiblingItem(HTREEITEM hItem);
	HTREEITEM GetPrevSiblingItem(HTREEITEM hItem);
	HTREEITEM GetParentItem(HTREEITEM hItem);
	HTREEITEM GetFirstVisibleItem();
	HTREEITEM GetNextVisibleItem(HTREEITEM hItem);
	HTREEITEM GetPrevVisibleItem(HTREEITEM hItem);
	HTREEITEM GetSelectedItem();
	HTREEITEM GetDropHilightItem();
	HTREEITEM GetRootItem();
//	bool GetItem(TVITEM* pItem);
	bool GetItem(FW_TVITEM* pItem);
	ITsString * GetItemText(HTREEITEM hItem);
	bool GetItemImage(HTREEITEM hItem, int& nImage, int& nSelectedImage);
	UINT GetItemState(HTREEITEM hItem, UINT nStateMask);
	DWORD GetItemData(HTREEITEM hItem);
//	bool SetItem(TVITEM* pItem);
	bool SetItem(FW_TVITEM* pItem);
//	bool SetItem(HTREEITEM hItem, UINT nMask, LPCTSTR lpszItem, int nImage,
//		int nSelectedImage, UINT nState, UINT nStateMask, LPARAM lParam);
	bool SetItem( HTREEITEM hItem, UINT nMask, ITsString * ptss, int iImage,
		int iSelectedImage, UINT nState, UINT nStateMask, LPARAM lParam );
	bool SetItemText(HTREEITEM hItem, LPCTSTR lpszItem);
	bool SetItemImage(HTREEITEM hItem, int nImage, int nSelectedImage);
	bool SetItemState(HTREEITEM hItem, UINT nState, UINT nStateMask);
	bool SetItemData(HTREEITEM hItem, DWORD dwData);
	bool ItemHasChildren(HTREEITEM hItem);
	// ENHANCE: Change this to VwEdit * instead of HWND once it gets finished.
	HWND GetEditControl();
	UINT GetVisibleCount();
	HWND GetToolTips();
	HWND SetToolTips(HWND hWndTooltip);
	bool SetUnicodeFormat(bool fUnicode);
	COLORREF GetBkColor();
	COLORREF SetBkColor(COLORREF clr);
	SHORT GetItemHeight();
	SHORT SetItemHeight(SHORT cyHeight);
	UINT SetScrollTime(UINT uScrollTime);
	COLORREF GetTextColor();
	COLORREF SetTextColor(COLORREF clr);
	bool SetInsertMark(HTREEITEM hItem, bool fAfter = TRUE);
	bool GetCheck(HTREEITEM hItem);
	bool SetCheck(HTREEITEM hItem, bool fCheck = TRUE);
	COLORREF GetInsertMarkColor();
	COLORREF SetInsertMarkColor(COLORREF clrNew);
	UINT GetISearchString(LPSTR lpsz);
	UINT GetScrollTime();
	UINT GetUnicodeFormat();
// Operations
	HTREEITEM InsertItem(LPFW_TVINSERTSTRUCT lpInsertStruct);
	HTREEITEM InsertItem(ITsString * ptss, HTREEITEM hParent, HTREEITEM hInsertAfter);
	HTREEITEM InsertItem(UINT nMask, ITsString * ptss, int iImage,
		int iSelectedImage, int cChildren, int nIntegral, UINT nState, UINT nStateMask,
		LPARAM lParam, HTREEITEM hParent, HTREEITEM hInsertAfter);
	HTREEITEM InsertItem(ITsString * ptss, int iImage, int iSelectedImage, HTREEITEM hParent,
		HTREEITEM hInsertAfter);

//	HTREEITEM InsertItem(LPCTSTR lpszItem, HTREEITEM hParent = TVI_ROOT,
//		HTREEITEM hInsertAfter = TVI_LAST);
//	HTREEITEM InsertItem(LPCTSTR lpszItem, int nImage, int nSelectedImage,
//		HTREEITEM hParent = TVI_ROOT, HTREEITEM hInsertAfter = TVI_LAST);
	bool DeleteItem(HTREEITEM hItem);
	bool DeleteAllItems();
	bool Expand(HTREEITEM hItem, UINT nCode);
	bool Select(HTREEITEM hItem, UINT nCode);
	bool SelectItem(WPARAM flag, HTREEITEM hItem);
	bool SelectDropTarget(HTREEITEM hItem);
	bool SelectSetFirstVisible(HTREEITEM hItem);
	// ENHANCE: Change this to VwEdit * instead of HWND once it gets finished.
	HWND EditLabel(HTREEITEM hItem);
	bool EndEditLabelNow(bool fCancel);
	HTREEITEM HitTest(POINT pt, UINT * pFlags = NULL);
	HTREEITEM HitTest(TVHITTESTINFO * pHitTestInfo);
	HIMAGELIST CreateDragImage(HTREEITEM hItem);
	bool SortChildren(HTREEITEM hItem);
	bool EnsureVisible(HTREEITEM hItem);
	bool SortChildrenCB(bool fRecurse, LPTVSORTCB pSort);
	static int CALLBACK TssTreeView::PossListCompareFunc(LPARAM lParam1, LPARAM lParam2,
		LPARAM lParamSort);

/***********************************************************************************************
	Notification message handlers.
***********************************************************************************************/


	// Events. Override these in the derived class.
	virtual bool OnClick(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnDblClk(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnKillFocus(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnRClick(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnRDblClk(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnReturn(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnSetCursor(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnSetFocus(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnBeginDrag(FW_NMTREEVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnBeginLabelEdit(FW_NMTVDISPINFO * pfnmdi, long & lnRet)
		{ return false; }
	virtual bool OnBeginRDrag(FW_NMTREEVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnDeleteItem(FW_NMTREEVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnEndLabelEdit(FW_NMTVDISPINFO * pfnmdi, long & lnRet)
		{ return false; }
	virtual bool OnGetDispInfo(FW_NMTVDISPINFO * pfnmdi, long & lnRet)
		{ return false; }
	// ENHANCE: Fix this once we make our tooltip widget.
	/*virtual bool OnGetInfoTip(FW_NMTVGETINFOTIP * pfnmgit, long & lnRet)
		{ return false; }*/
	virtual bool OnItemExpanded(FW_NMTREEVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnItemExpanding(FW_NMTREEVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnKeyDown(FW_NMTVKEYDOWN * pfnmkd, long & lnRet)
		{ return false; }
	virtual bool OnSelChanged(FW_NMTREEVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnSelChanging(FW_NMTREEVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnSetDispInfo(FW_NMTREEVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnSingleExpand(FW_NMTREEVIEW * pfnmv, long & lnRet)
		{ return false; }

	virtual bool OnSize(int nId, int dxp, int dyp);
	virtual bool OnPaint(HDC hdcDef);

	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb);

	enum
	{
		khvoItemText = 1000,
		kflidItemText = 1001,

		kfrItemText = 0,
	};

	// Set the writing system factory (and the user interface writing system as a side-effect).
	void SetWritingSystemFactory(ILgWritingSystemFactory * pwsf)
	{
		AssertPtr(pwsf);

		m_qwsf = pwsf;
		CheckHr(pwsf->get_UserWs(&m_wsUser));
		Assert(m_wsUser);
	}

protected:
	enum
	{
		kcchMaxText = 1024,
	};
	static achar s_rgchBuffer[kcchMaxText];

	// These are used for look-ahead typing.
//	OLECHAR m_rgchLookAhead[10];
//	int m_cchLookAhead;

	void PreCreateHwnd(CREATESTRUCT & cs);
	virtual void SubclassHwnd(HWND hwnd);

	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	bool OnNotifyThis(int id, NMHDR * pnmh, long & lnRet);
	bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);

	bool _OnCustomDraw(NMTVCUSTOMDRAW * pnmcd, long & lnRet);
	bool _OnBeginDrag(NMTREEVIEW * pnmv, long & lnRet);
	bool _OnBeginLabelEdit(NMTVDISPINFO * pnmdi, long & lnRet);
	bool _OnBeginRDrag(NMTREEVIEW * pnmv, long & lnRet);
	bool _OnDeleteItem(NMTREEVIEW * pnmv, long & lnRet);
	bool _OnEndLabelEdit(NMTVDISPINFO * pnmdi, long & lnRet);
	bool _OnGetDispInfo(NMTVDISPINFO * pnmdi, long & lnRet);
	// ENHANCE: Fix this once we make our tooltip widget.
	/*bool _OnGetInfoTip(NMTVGETINFOTIP * pnmgit, long & lnRet);*/
	bool _OnItemExpanded(NMTREEVIEW * pnmv, long & lnRet);
	bool _OnItemExpanding(NMTREEVIEW * pnmv, long & lnRet);
	bool _OnKeyDown(NMTVKEYDOWN * pnmkd, long & lnRet);
	bool _OnSelChanged(NMTREEVIEW * pnmv, long & lnRet);
	bool _OnSelChanging(NMTREEVIEW * pnmv, long & lnRet);
	bool _OnSetDispInfo(NMTREEVIEW * pnmv, long & lnRet);
	bool _OnSingleExpand(NMTREEVIEW * pnmv, long & lnRet);

	void _CopyItem(const TVITEM & tvi, FW_TVITEM & ftvi);
	void _CopyItem(const FW_TVITEM & ftvi, TVITEM & tvi);

	IVwCacheDaPtr m_qvcd;
	TssTreeViewVcPtr m_qttvvc;
	AfToolTipWndPtr m_qttw;

	COLORREF m_clrDrawingBack;
	int m_dxpSpace;
	achar m_rgSpaces[2000];

	ILgWritingSystemFactoryPtr m_qwsf;
	int m_wsUser;		// user interface writing system id.
};

typedef GenSmartPtr<TssTreeView> TssTreeViewPtr;

#endif //!TSSTREEVIEW_H

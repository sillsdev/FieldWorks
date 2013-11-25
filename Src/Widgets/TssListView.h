/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TssListView.h
Responsibility: Darrell Zook
Last reviewed:

	This is the base Sdk class of a listview designed for TsStrings.
	This class is used for Sdk applications, and is also the base for an ActiveX control.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TSSLISTVIEW_H
#define TSSLISTVIEW_H 1

typedef struct
{
	uint flags;
	ITsStringPtr qtss;
	LPARAM lParam;
	POINT pt;
	uint vkDirection;
} FW_LVFINDINFO;

typedef struct
{
	uint mask;
	int fmt;
	int cx;
	ITsStringPtr qtss;
	int iSubItem;
	int iImage;
	int iOrder;
} FW_LVCOLUMN;

typedef struct FW_LVITEM
{
	FW_LVITEM(uint _mask = 0, int _iItem = 0, int _iSubItem = 0)
	{
		mask = _mask;
		iItem = _iItem;
		iSubItem = _iSubItem;
		state = stateMask = 0;
		iImage = iIndent = 0;
		lParam = 0;
	}
	uint mask;
	int iItem;
	int iSubItem;
	uint state;
	uint stateMask;
	ITsStringPtr qtss;
	int iImage;
	LPARAM lParam;
	int iIndent;
} FW_LVITEM;

typedef struct
{
	NMHDR hdr;
	FW_LVITEM item;
} FW_NMLVDISPINFO;


// The reason we are using WM_APP here instead of WM_USER is because on some machines, using
// WM_USER as the base caused messages to be converted to another message somehow. I
// (DarrellZ) don't understand how the messages were getting converted, but using WM_APP
// seems to work.
enum
{
	FW_LVM_FINDITEM = WM_APP + 1,
	FW_LVM_GETCOLUMN,
	FW_LVM_GETEDITCONTROL,
	FW_LVM_GETISEARCHSTRING,
	FW_LVM_GETITEM,
	FW_LVM_GETITEMTEXT,
	FW_LVM_GETSTRINGWIDTH,
	FW_LVM_INSERTCOLUMN,
	FW_LVM_INSERTITEM,
	FW_LVM_SETCOLUMN,
	FW_LVM_SETITEM,
	FW_LVM_SETITEMSTATE,
	FW_LVM_SETITEMTEXT,
};


/*----------------------------------------------------------------------------------------------
	Macros for list view messages that we have overridden:
		Fw_ListView_FindItem
		Fw_ListView_GetColumn
		Fw_ListView_GetEditControl
		Fw_ListView_GetISearchString
		Fw_ListView_GetItem
		Fw_ListView_GetItemText
		Fw_ListView_GetStringWidth
		Fw_ListView_InsertColumn
		Fw_ListView_InsertItem
		Fw_ListView_SetColumn
		Fw_ListView_SetItem
		Fw_ListView_SetItemState
		Fw_ListView_SetItemText
----------------------------------------------------------------------------------------------*/

#define Fw_ListView_FindItem(hwnd, iStart, pflvfi) \
	(int)SendMessage((hwnd), FW_LVM_FINDITEM, (WPARAM)(int)(iStart), \
		(LPARAM)(const FW_LVFINDINFO *)(pflvfi))

#define Fw_ListView_GetColumn(hwnd, iCol, pflvc) \
	(bool)SendMessage((hwnd), FW_LVM_GETCOLUMN, (WPARAM)(int)(iCol), \
		(LPARAM)(FW_LVCOLUMN *)(pflvc))

// ENHANCE: Change this to TssEdit * once it gets finished.
#define Fw_ListView_GetEditControl(hwnd) \
	(AfWnd *)SendMessage((hwnd), FW_LVM_GETEDITCONTROL, 0, 0)

#define Fw_ListView_GetISearchString(hwnd, ptss) \
	(bool)SendMessage((hwnd), FW_LVM_GETISEARCHSTRING, 0, (LPARAM)(ITsString *)(ptss))

#define Fw_ListView_GetItem(hwnd, pflvi) \
	(bool)SendMessage((hwnd), FW_LVM_GETITEM, 0, (LPARAM)(FW_LVITEM *)(pflvi))

#define Fw_ListView_GetItemText(hwnd, iItem, iSItem, pptss) \
{ \
	AssertPtr(pptss); \
	Assert(!*pptss); \
	FW_LVITEM flvi; \
	flvi.iSubItem = iSItem; \
	SendMessage((hwnd), FW_LVM_GETITEMTEXT, (WPARAM)iItem, (LPARAM)(FW_LVITEM *)&flvi); \
	*pptss = flvi.qtss.Detach(); \
}

#define Fw_ListView_GetStringWidth(hwnd, pptss) \
	(int)SendMessage((hwnd), FW_LVM_GETSTRINGWIDTH, 0, (LPARAM)(ITsString **)(pptss))

#define Fw_ListView_InsertColumn(hwnd, iCol, pflvc) \
	(int)SendMessage((hwnd), FW_LVM_INSERTCOLUMN, (WPARAM)(int)(iCol), \
		(LPARAM)(const FW_LVCOLUMN *)(pflvc))

#define Fw_ListView_InsertItem(hwnd, pflvi)   \
	(int)SendMessage((hwnd), FW_LVM_INSERTITEM, 0, (LPARAM)(const FW_LVITEM *)(pflvi))

#define Fw_ListView_SetColumn(hwnd, iCol, pflvc) \
	(bool)SendMessage((hwnd), FW_LVM_SETCOLUMN, (WPARAM)(int)(iCol), \
		(LPARAM)(const FW_LVCOLUMN *)(pflvc))

#define Fw_ListView_SetItem(hwnd, pflvi) \
	(bool)SendMessage((hwnd), FW_LVM_SETITEM, 0, (LPARAM)(const FW_LVITEM *)(pflvi))

#define Fw_ListView_SetItemState(hwnd, iItem, data, mask) \
{ \
	FW_LVITEM flvi; \
	flvi.stateMask = mask; \
	flvi.state = data; \
	SendMessage((hwnd), FW_LVM_SETITEMSTATE, (WPARAM)iItem, (LPARAM)(FW_LVITEM *)&flvi); \
}

#define Fw_ListView_SetItemText(hwnd, iItem, iSubItem, ptss) \
{ \
	FW_LVITEM flvi; \
	flvi.iSubItem = iSubItem; \
	flvi.qtss = ptss; \
	SendMessage((hwnd), FW_LVM_SETITEMTEXT, (WPARAM)iItem, (LPARAM)(FW_LVITEM *)&flvi); \
}


/*----------------------------------------------------------------------------------------------
	This class supports the TsString Tree View.
	Hungarian: tlv
----------------------------------------------------------------------------------------------*/
class TssListView : public AfWnd
{
typedef AfWnd SuperClass;

public:
	TssListView();

	virtual void SubclassListView(HWND hwnd, int wsUser);

/***********************************************************************************************
	MFC-like methods
***********************************************************************************************/
// Attributes
	SIZE ApproximateViewRect(int iCount = -1, int cx = -1, int cy = -1);
	bool Arrange(int code);
	HWND CreateDragImage(int iItem, POINT * pptUpLeft);
	bool DeleteAllItems();
	bool DeleteColumn(int iCol);
	bool DeleteItem(int iItem);
	HWND EditLabel(int iItem);
	bool EnsureVisible(int i, bool fPartialOK);
	int FindItem(int iStart, FW_LVFINDINFO * plvfi);
	COLORREF GetBkColor();
	bool GetBkImage(LVBKIMAGE * plvbki);
	uint GetCallbackMask();
	bool GetColumn(int iCol, FW_LVCOLUMN * plvc);
	bool GetColumnOrderArray(int cCol, int * prgiArray);
	int GetColumnWidth(int iCol);
	int GetCountPerPage();
	// ENHANCE: Change this to TssEdit once it is done.
	AfWnd * GetEditControl();
	DWORD GetExtendedListViewStyle();
	HWND GetHeaderCtrl();
	HCURSOR GetHotCursor();
	int GetHotItem();
	DWORD GetHoverTime();
	HIMAGELIST GetImageList(int iImageList);
	uint GetISearchString(StrUni & stu);
	uint GetISearchString(ITsString ** pptss);
	bool GetItem(FW_LVITEM * plvi);
	int GetItemCount();
	bool GetItemPosition(int i, POINT * ppt);
	bool GetItemRect(int i, RECT * prc);
	uint GetItemSpacing(bool fSmall);
	uint GetItemState(int i, uint mask);
	bool SetItemState(int iItem, uint nState, uint nMask);
	int GetItemText(int iItem, FW_LVITEM * plvi);
	int GetItemTextA(int iItem, LVITEM * plvi);
	int GetNextItem(int iStart, uint flags);
	uint GetNumberOfWorkAreas();
	bool GetOrigin(POINT * pptOrg);
	uint GetSelectedCount();
	int GetSelectionMark();
	int GetStringWidth(StrUni & stu);
	int GetStringWidth(ITsString * ptss);
	bool GetSubItemRect(int iItem, RECT * prc);
	COLORREF GetTextBkColor();
	COLORREF GetTextColor();
	HWND GetToolTips();
	int GetTopIndex();
	uint GetUnicodeFormat();
	bool GetViewRect(RECT * prc);
	void GetWorkAreas(int nWorkAreas, RECT * prc);
	int HitTest(LVHITTESTINFO * phti);
	int InsertColumn(int iCol, const FW_LVCOLUMN * plvc);
	int InsertItem(const FW_LVITEM * plvi);
	bool RedrawItems(int iFirst, int iLast);
	bool Scroll(int dxp, int dyp);
	bool SetBkColor(COLORREF clrBk);
	bool SetBkImage(HBITMAP hbmp, bool fTile = true, int xOffsetPercent = 0,
		int yOffsetPercent = 0);
	bool SetBkImage(LVBKIMAGE * plvbki);
	bool SetCallbackMask(uint mask);
	bool SetColumn(int iCol, const FW_LVCOLUMN * plvc);
	bool SetColumnOrderArray(int cCol, int * prgCol);
	bool SetColumnWidth(int iCol, int dxp);
	DWORD SetExtendedListViewStyle(DWORD dwExMask, DWORD dwExStyle);
	HCURSOR SetHotCursor(HCURSOR hCursor);
	int SetHotItem(int iItem);
	DWORD SetHoverTime(DWORD dwHoverTime = (DWORD)-1);
	SIZE SetIconSpacing(int dxp, int dyp);
	HIMAGELIST SetImageList(int iImageList, HIMAGELIST himl);
	bool SetItem(const FW_LVITEM * plvi);
	bool SetItemCount(int cItems, int dwFlags);
	bool SetItemPosition(int iItem, int xp, int yp);
	void SetItemPosition32(int iItem, POINT * pptNewPos);
	bool SetItemState(int i, FW_LVITEM * plvi);
	bool SetItemText(int i, FW_LVITEM * plvi);
	int SetSelectionMark(int iIndex);
	bool SetTextBkColor(COLORREF clrText);
	bool SetTextColor(COLORREF clrText);
	HWND SetToolTips(HWND hwndToolTip);
	uint SetUnicodeFormat(bool fUnicode);
	void SetWorkAreas(int nWorkAreas, RECT * prc);
	bool SortItems(LPARAM lParamSort, PFNLVCOMPARE pfnCompare);
	int SubItemHitTest(LVHITTESTINFO * phti);
	bool Update(int iItem);

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
	virtual bool OnSetFocus(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool CustomDraw(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnHover(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnReleasedCapture(NMHDR * pnmh, long & lnRet)
		{ return false; }

	virtual bool OnBeginDrag(NMLISTVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnBeginLabelEdit(FW_NMLVDISPINFO * pfnmdi, long & lnRet)
		{ return false; }
	virtual bool OnBeginRDrag(NMLISTVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnColumnClick(NMLISTVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnDeleteAllItems(NMLISTVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnDeleteItem(NMLISTVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnEndLabelEdit(FW_NMLVDISPINFO * pfnmdi, long & lnRet)
		{ return false; }
	virtual bool OnGetDispInfo(FW_NMLVDISPINFO * pfnmdi, long & lnRet)
		{ return false; }
	virtual bool OnInsertItem(NMLISTVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnHotTrack(NMLISTVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnItemActivate(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnItemChanged(NMLISTVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnItemChanging(NMLISTVIEW * pfnmv, long & lnRet)
		{ return false; }
	virtual bool OnKeyDown(NMLVKEYDOWN * pnkd, long & lnRet)
		{ return false; }
	virtual bool OnMarqueeBegin(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnODStateChanged(NMLVODSTATECHANGE * pStateChange, long & lnRet)
		{ return false; }
	virtual bool OnSetDispInfo(FW_NMLVDISPINFO * pfnmdi, long & lnRet)
		{ return false; }

protected:
	enum
	{
		kcchMaxText = 1024,
	};
	static achar s_rgchBuffer[kcchMaxText];

	int m_wsUser;		// user interface writing system id.

	void PreCreateHwnd(CREATESTRUCT & cs);

	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	bool OnNotifyThis(int id, NMHDR * pnmh, long & lnRet);

	bool _OnBeginLabelEdit(NMLVDISPINFO * pnmdi, long & lnRet);
	bool _OnEndLabelEdit(NMLVDISPINFO * pnmdi, long & lnRet);
	bool _OnGetDispInfo(NMLVDISPINFO * pnmdi, long & lnRet);
	bool _OnItemChanged(NMLISTVIEW * pnmv, long & lnRet);
	bool _OnItemChanging(NMLISTVIEW * pnmv, long & lnRet);
	bool _OnSetDispInfo(NMLVDISPINFO * pnmdi, long & lnRet);

	void _CopyItem(const LVITEM & lvi, FW_LVITEM & flvi);
	void _CopyItem(const FW_LVITEM & flvi, LVITEM & lvi);
	void _CopyColumn(const LVCOLUMN & lvc, FW_LVCOLUMN & flvc);
	void _CopyColumn(const FW_LVCOLUMN & flvc, LVCOLUMN & lvc);
};

typedef GenSmartPtr<TssListView> TssListViewPtr;

#endif //!TSSLISTVIEW_H

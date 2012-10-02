/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TssListView.cpp
Responsibility: Darrell Zook
Last reviewed:

	Implementation of TssListView.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


achar TssListView::s_rgchBuffer[kcchMaxText];

/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
TssListView::TssListView()
{
	Assert(_WIN32_IE >= 0x0300);
}


/*----------------------------------------------------------------------------------------------
// This function must be called before the listview control has been initialized with any data.
// (i.e. before any items are inserted into it.)
----------------------------------------------------------------------------------------------*/
void TssListView::SubclassListView(HWND hwnd, int wsUser)
{
	SubclassHwnd(hwnd);
	Assert(GetItemCount() == 0);

	Assert(wsUser);
	m_wsUser = wsUser;
}


void TssListView::PreCreateHwnd(CREATESTRUCT & cs)
{
	cs.style |= WS_CHILD;
}


/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
bool TssListView::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
//	case LVM_FINDITEM:
	case LVM_INSERTCOLUMN:
	case LVM_INSERTITEM:
		// We don't support these methods. In most cases, there are appropriate
		// TsString versions available.
		Assert(false);
		lnRet = -1;
		return true;

	// These were initially combined into one Assert, but automatic testing is failing and we
	// want to find out which message it is using, so for now, we are duplicating code
	// for each item.
	case LVM_GETCOLUMN:
		Assert(false);
		lnRet = false;
		return true;
	case LVM_GETISEARCHSTRING:
		Assert(false);
		lnRet = false;
		return true;
	case LVM_GETITEM:
		Assert(false);
		lnRet = false;
		return true;
	case LVM_GETSTRINGWIDTH:
		Assert(false);
		lnRet = false;
		return true;
	case LVM_SETCOLUMN:
		Assert(false);
		lnRet = false;
		return true;
	case LVM_SETITEM:
		Assert(false);
		lnRet = false;
		return true;
	case LVM_SETITEMSTATE:
		Assert(false);
		lnRet = false;
		return true;
	case LVM_SETITEMTEXT:
		Assert(false);
		lnRet = false;
		return true;
	case LVM_GETEDITCONTROL:
		// We don't support these methods. In most cases, there are appropriate
		// TsString versions available.
		Assert(false);
		lnRet = false;
		return true;

	case LVM_GETITEMTEXT:
		lnRet = GetItemTextA(wp, (LVITEM *)lp);
		return true;

	case FW_LVM_FINDITEM:
		lnRet = FindItem((int)wp, (FW_LVFINDINFO *)lp);
		return true;

	case FW_LVM_GETCOLUMN:
		lnRet = GetColumn((int)wp, (FW_LVCOLUMN *)lp);
		return true;

	case FW_LVM_GETEDITCONTROL:
		lnRet = (long)GetEditControl();
		return true;

	case FW_LVM_GETISEARCHSTRING:
		lnRet = GetISearchString((ITsString **)lp);
		return true;

	case FW_LVM_GETITEM:
		lnRet = GetItem((FW_LVITEM *)lp);
		return true;

	case FW_LVM_GETITEMTEXT:
		lnRet = GetItemText((int)wp, (FW_LVITEM *)lp);
		return true;

	case FW_LVM_GETSTRINGWIDTH:
		lnRet = GetStringWidth((ITsString *)lp);
		return true;

	case FW_LVM_INSERTCOLUMN:
		lnRet = InsertColumn((int)wp, (const FW_LVCOLUMN *)lp);
		return true;

	case FW_LVM_INSERTITEM:
		lnRet = InsertItem((const FW_LVITEM *)lp);
		return true;

	case FW_LVM_SETCOLUMN:
		lnRet = SetColumn((int)wp, (const FW_LVCOLUMN *)lp);
		return true;

	case FW_LVM_SETITEM:
		lnRet = SetItem((const FW_LVITEM *)lp);
		return true;

	case FW_LVM_SETITEMSTATE:
		lnRet = SetItemState((int)wp, (FW_LVITEM *)lp);
		return true;

	case FW_LVM_SETITEMTEXT:
		lnRet = SetItemText((int)wp, (FW_LVITEM *)lp);
		return true;


	case LVM_APPROXIMATEVIEWRECT:
		{
			SIZE size = ApproximateViewRect((int)wp, LOWORD(lp), HIWORD(lp));
			lnRet = MAKELONG(size.cx, size.cx);
			return true;
		}

	case LVM_ARRANGE:
		lnRet = Arrange((int)wp);
		return true;

	case LVM_CREATEDRAGIMAGE:
		lnRet = (long)CreateDragImage((int)wp, (POINT *)lp);
		return true;

	case LVM_DELETEALLITEMS:
		lnRet = DeleteAllItems();
		return true;

	case LVM_DELETECOLUMN:
		lnRet = DeleteColumn((int)wp);
		return true;

	case LVM_DELETEITEM:
		lnRet = DeleteItem((int)wp);
		return true;

	case LVM_EDITLABEL:
		lnRet = (long)EditLabel((int)wp);
		return true;

	case LVM_ENSUREVISIBLE:
		lnRet = EnsureVisible((int)wp, (bool)lp);
		return true;

	case LVM_GETBKCOLOR:
		lnRet = GetBkColor();
		return true;

	case LVM_GETBKIMAGE:
		lnRet = GetBkImage((LVBKIMAGE *)lp);
		return true;

	case LVM_GETCALLBACKMASK:
		lnRet = GetCallbackMask();
		return true;

	case LVM_GETCOLUMNORDERARRAY:
		lnRet = GetColumnOrderArray((int)wp, (int *)lp);
		return true;

	case LVM_GETCOLUMNWIDTH:
		lnRet = GetColumnWidth((int)wp);
		return true;

	case LVM_GETCOUNTPERPAGE:
		lnRet = GetCountPerPage();
		return true;

	case LVM_GETEXTENDEDLISTVIEWSTYLE:
		lnRet = GetExtendedListViewStyle();
		return true;

	case LVM_GETHEADER:
		lnRet = (long)GetHeaderCtrl();
		return true;

	case LVM_GETHOTCURSOR:
		lnRet = (long)GetHotCursor();
		return true;

	case LVM_GETHOTITEM:
		lnRet = GetHotItem();
		return true;

	case LVM_GETHOVERTIME:
		lnRet = GetHoverTime();
		return true;

	case LVM_GETIMAGELIST:
		lnRet = (long)GetImageList((int)wp);
		return true;

	case LVM_GETITEMCOUNT:
		lnRet = GetItemCount();
		return true;

	case LVM_GETITEMPOSITION:
		lnRet = GetItemPosition((int)wp, (POINT *)lp);
		return true;

	case LVM_GETITEMRECT:
		lnRet = GetItemRect((int)wp, (RECT *)lp);
		return true;

	case LVM_GETITEMSPACING:
		lnRet = GetItemSpacing((bool)wp);
		return true;

	case LVM_GETITEMSTATE:
		lnRet = GetItemState((int)wp, (uint)lp);
		return true;

	case LVM_GETNEXTITEM:
		lnRet = GetNextItem((int)wp, (uint)lp);
		return true;

	case LVM_GETNUMBEROFWORKAREAS:
		AssertPtr((uint *)lp);
		*((uint *)lp) = GetNumberOfWorkAreas();
		return true;

	case LVM_GETORIGIN:
		lnRet = GetOrigin((POINT *)lp);
		return true;

	case LVM_GETSELECTEDCOUNT:
		lnRet = GetSelectedCount();
		return true;

	case LVM_GETSELECTIONMARK:
		lnRet = GetSelectionMark();
		return true;

	case LVM_GETSUBITEMRECT:
		lnRet = GetSubItemRect((int)wp, (RECT *)lp);
		return true;

	case LVM_GETTEXTBKCOLOR:
		lnRet = GetTextBkColor();
		return true;

	case LVM_GETTEXTCOLOR:
		lnRet = GetTextColor();
		return true;

	case LVM_GETTOOLTIPS:
		lnRet = (long)GetToolTips();
		return true;

	case LVM_GETTOPINDEX:
		lnRet = GetTopIndex();
		return true;

	case LVM_GETUNICODEFORMAT:
		lnRet = GetUnicodeFormat();
		return true;

	case LVM_GETVIEWRECT:
		lnRet = GetViewRect((RECT *)lp);
		return true;

	case LVM_GETWORKAREAS:
		GetWorkAreas((int)wp, (RECT *)lp);
		return true;

	case LVM_HITTEST:
		lnRet = HitTest((LVHITTESTINFO *)lp);
		return true;

	case LVM_REDRAWITEMS:
		lnRet = RedrawItems((int)wp, (int)lp);
		return true;

	case LVM_SCROLL:
		lnRet = Scroll((int)wp, (int)lp);
		return true;

	case LVM_SETBKCOLOR:
		lnRet = SetBkColor((COLORREF)lp);
		return true;

	case LVM_SETBKIMAGE:
		lnRet = SetBkImage((LVBKIMAGE *)lp);
		return true;

	case LVM_SETCALLBACKMASK:
		lnRet = SetCallbackMask(wp);
		return true;

	case LVM_SETCOLUMNORDERARRAY:
		lnRet = SetColumnOrderArray((int)wp, (int *)lp);
		return true;

	case LVM_SETCOLUMNWIDTH:
		lnRet = SetColumnWidth((int)wp, LOWORD(lp));
		return true;

	case LVM_SETEXTENDEDLISTVIEWSTYLE:
		lnRet = SetExtendedListViewStyle(wp, lp);
		return true;

	case LVM_SETHOTCURSOR:
		lnRet = (long)SetHotCursor((HCURSOR)lp);
		return true;

	case LVM_SETHOTITEM:
		lnRet = SetHotItem((int)wp);
		return true;

	case LVM_SETHOVERTIME:
		lnRet = SetHoverTime((DWORD)lp);
		return true;

	case LVM_SETICONSPACING:
		{
			SIZE size = SetIconSpacing(LOWORD(lp), HIWORD(lp));
			lnRet = MAKELONG(size.cx, size.cy);
			return true;
		}

	case LVM_SETIMAGELIST:
		lnRet = (long)SetImageList((int)wp, (HIMAGELIST)lp);
		return true;

	case LVM_SETITEMCOUNT:
		lnRet = SetItemCount((int)wp, lp);
		return true;

	case LVM_SETITEMPOSITION:
		lnRet = SetItemPosition((int)wp, LOWORD(lp), HIWORD(lp));
		return true;

	case LVM_SETITEMPOSITION32:
		SetItemPosition32((int)wp, (POINT *)lp);
		return true;

	case LVM_SETSELECTIONMARK:
		lnRet = SetSelectionMark((int)lp);
		return true;

	case LVM_SETTEXTBKCOLOR:
		lnRet = SetTextBkColor((COLORREF)lp);
		return true;

	case LVM_SETTEXTCOLOR:
		lnRet = SetTextColor((COLORREF)lp);
		return true;

	case LVM_SETTOOLTIPS:
		lnRet = (long)SetToolTips((HWND)lp);
		return true;

	case LVM_SETUNICODEFORMAT:
		lnRet = SetUnicodeFormat((bool)wp);
		return true;

	case LVM_SETWORKAREAS:
		SetWorkAreas((int)wp, (RECT *)lp);
		return true;

	case LVM_SORTITEMS:
		lnRet = SortItems(wp, (PFNLVCOMPARE)lp);
		return true;

	case LVM_SUBITEMHITTEST:
		lnRet = SubItemHitTest((LVHITTESTINFO *)lp);
		return true;

	case LVM_UPDATE:
		lnRet = Update(wp);
		return true;

	default:
		return false;
	}
}

/***********************************************************************************************
	TssListView message handlers
***********************************************************************************************/


/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
bool TssListView::OnNotifyThis(int id, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	Assert(pnmh->hwndFrom == m_hwnd);

	switch (pnmh->code)
	{
	case LVN_GETINFOTIP:
	case LVN_ODCACHEHINT:
	case LVN_ODFINDITEM:
		Assert(false);
		return true;

	case LVN_BEGINLABELEDIT:
		return _OnBeginLabelEdit((NMLVDISPINFO *)pnmh, lnRet);

	case LVN_ENDLABELEDIT:
		return _OnEndLabelEdit((NMLVDISPINFO *)pnmh, lnRet);

	case LVN_GETDISPINFO:
		return _OnGetDispInfo((NMLVDISPINFO *)pnmh, lnRet);

	case LVN_ITEMCHANGED:
		return _OnItemChanged((NMLISTVIEW *)pnmh, lnRet);

	case LVN_ITEMCHANGING:
		return _OnItemChanging((NMLISTVIEW *)pnmh, lnRet);

	case LVN_SETDISPINFO :
		return _OnSetDispInfo((NMLVDISPINFO *)pnmh, lnRet);


	case LVN_BEGINDRAG:
		return OnBeginDrag((NMLISTVIEW *)pnmh, lnRet);

	case LVN_BEGINRDRAG:
		return OnBeginRDrag((NMLISTVIEW *)pnmh, lnRet);

	case LVN_COLUMNCLICK:
		return OnColumnClick((NMLISTVIEW *)pnmh, lnRet);

	case LVN_DELETEALLITEMS:
		return OnDeleteAllItems((NMLISTVIEW *)pnmh, lnRet);

	case LVN_DELETEITEM:
		return OnDeleteItem((NMLISTVIEW *)pnmh, lnRet);

	case LVN_INSERTITEM:
		return OnInsertItem((NMLISTVIEW *)pnmh, lnRet);

	case LVN_HOTTRACK:
		return OnHotTrack((NMLISTVIEW *)pnmh, lnRet);

	case LVN_ITEMACTIVATE:
		return OnItemActivate(pnmh, lnRet);

	case LVN_KEYDOWN:
		return OnKeyDown((NMLVKEYDOWN *)pnmh, lnRet);

	case LVN_MARQUEEBEGIN:
		return OnMarqueeBegin(pnmh, lnRet);

	case NM_CLICK:
		return OnClick(pnmh, lnRet);

	case NM_CUSTOMDRAW:
		// TODO
		//return OnCustomDraw(pnmh);
		return false;

	case NM_DBLCLK:
		return OnDblClk(pnmh, lnRet);

	case NM_HOVER:
		return OnHover(pnmh, lnRet);

	case NM_KILLFOCUS:
		return OnKillFocus(pnmh, lnRet);

	case NM_RCLICK:
		return OnRClick(pnmh, lnRet);

	case NM_RDBLCLK:
		return OnRDblClk(pnmh, lnRet);

	case NM_RELEASEDCAPTURE:
		return OnReleasedCapture(pnmh, lnRet);

	case NM_RETURN:
		return OnReturn(pnmh, lnRet);

	case NM_SETFOCUS:
		return OnSetFocus(pnmh, lnRet);

	default:
		return false;
	}
}

/***********************************************************************************************
	MFC methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Calculates the approximate width and height required to display a given number of items.
----------------------------------------------------------------------------------------------*/
SIZE TssListView::ApproximateViewRect(int iCount, int cx, int cy)
{
	DWORD dwT = SuperClass::DefWndProc(LVM_APPROXIMATEVIEWRECT, (WPARAM)iCount,
		MAKELPARAM(cx, cy));
	SIZE size = { LOWORD(dwT), HIWORD(dwT) };
	return size;
}


/*----------------------------------------------------------------------------------------------
	Arranges items in icon view
----------------------------------------------------------------------------------------------*/
bool TssListView::Arrange(int code)
{
	return (HIMAGELIST)SuperClass::DefWndProc(LVM_ARRANGE, (WPARAM)code, 0);
}


/*----------------------------------------------------------------------------------------------
	Creates a drag image list for the specified item.
----------------------------------------------------------------------------------------------*/
HWND TssListView::CreateDragImage(int iItem, POINT * pptUpLeft)
{
	AssertPtr(pptUpLeft);

	return (HWND)SuperClass::DefWndProc(LVM_CREATEDRAGIMAGE, iItem, (LPARAM)pptUpLeft);
}


/*----------------------------------------------------------------------------------------------
	Removes all items from a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::DeleteAllItems()
{
	return SuperClass::DefWndProc(LVM_DELETEALLITEMS, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Removes a column from a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::DeleteColumn(int iCol)
{
	return SuperClass::DefWndProc(LVM_DELETECOLUMN, iCol, 0);
}


/*----------------------------------------------------------------------------------------------
	Removes an item from a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::DeleteItem(int iItem)
{
	return SuperClass::DefWndProc(LVM_DELETEITEM, iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	Begins in-place editing of the specified list view item's text. The message implicitly
	selects and focuses the specified item.
----------------------------------------------------------------------------------------------*/
HWND TssListView::EditLabel(int iItem)
{
	return (HWND)SuperClass::DefWndProc(LVM_EDITLABEL, iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	Ensures that a list view item is either entirely or partially visible, scrolling the list
	view control if necessary.
----------------------------------------------------------------------------------------------*/
bool TssListView::EnsureVisible(int i, bool fPartialOK)
{
	return SuperClass::DefWndProc(LVM_ENSUREVISIBLE, i, fPartialOK);
}


/*----------------------------------------------------------------------------------------------
	Searches for a list view item with the specified characteristics.
----------------------------------------------------------------------------------------------*/
int TssListView::FindItem(int iStart, FW_LVFINDINFO * pfwlvfi)
{
	AssertPtr(pfwlvfi);

	StrApp str;
	const OLECHAR * pwrgch;
	int cch;

	HRESULT hr;
	IgnoreHr(hr = pfwlvfi->qtss->LockText(&pwrgch, &cch));
	if (FAILED(hr))
		return NULL;
	str.Assign(pwrgch, cch);
	pfwlvfi->qtss->UnlockText(pwrgch);

	LVFINDINFO lvfi;
	lvfi.psz = str.Chars();
	lvfi.flags = pfwlvfi->flags;
	lvfi.lParam = pfwlvfi->lParam;
	lvfi.pt = pfwlvfi->pt;
	lvfi.vkDirection = pfwlvfi->vkDirection;
	int nT = SuperClass::DefWndProc(LVM_FINDITEM, iStart, (LPARAM)&lvfi);
	return nT;


	// TODO
	//return SuperClass::DefWndProc(LVM_FINDITEM, iStart, (LPARAM)plvfi);
//	return -1;
}


/*----------------------------------------------------------------------------------------------
	Retrieves the background color of a list view control.
----------------------------------------------------------------------------------------------*/
COLORREF TssListView::GetBkColor()
{
	return SuperClass::DefWndProc(LVM_GETBKCOLOR, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the background image in a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::GetBkImage(LVBKIMAGE * plvbki)
{
	AssertPtr(plvbki);

	return SuperClass::DefWndProc(LVM_GETBKIMAGE, 0, (LPARAM)plvbki);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the callback mask for a list view control.
----------------------------------------------------------------------------------------------*/
uint TssListView::GetCallbackMask()
{
	return SuperClass::DefWndProc(LVM_GETCALLBACKMASK, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the attributes of a list view control's column.
----------------------------------------------------------------------------------------------*/
bool TssListView::GetColumn(int iCol, FW_LVCOLUMN * plvc)
{
	AssertPtr(plvc);

	LVCOLUMN lvc = { plvc->mask };
	lvc.iSubItem = plvc->iSubItem;
	lvc.pszText = s_rgchBuffer;
	lvc.cchTextMax = kcchMaxText;
	return SuperClass::DefWndProc(LVM_GETCOLUMN, iCol, (LPARAM)plvc);
	_CopyColumn(lvc, *plvc);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the current left-to-right order of columns in a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::GetColumnOrderArray(int cCol, int * prgCol)
{
	AssertArray(prgCol, cCol);

	return SuperClass::DefWndProc(LVM_GETCOLUMNORDERARRAY, cCol, (LPARAM)prgCol);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the width of a column in report or list view.
----------------------------------------------------------------------------------------------*/
int TssListView::GetColumnWidth(int iCol)
{
	return SuperClass::DefWndProc(LVM_GETCOLUMNWIDTH, iCol, 0);
}


/*----------------------------------------------------------------------------------------------
	Calculates the number of items that can fit vertically in the visible area of a list view
	control when in list or report view. Only fully visible items are counted.
----------------------------------------------------------------------------------------------*/
int TssListView::GetCountPerPage()
{
	return SuperClass::DefWndProc(LVM_GETCOUNTPERPAGE, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the handle to the edit control being used to edit a list view item's text.
----------------------------------------------------------------------------------------------*/
AfWnd * TssListView::GetEditControl()
{
	HWND hwndEdit = (HWND)SuperClass::DefWndProc(LVM_GETEDITCONTROL, 0, 0);
	if (!hwndEdit)
		return NULL;
	// TODO
	/*TssEditPtr qedit;
	qedit.Create();
	qedit.SubclassEdit(hwndEdit);
	return qedit;*/
	return NULL;
}


/*----------------------------------------------------------------------------------------------
	Retrieves the extended styles that are currently in use for a given list view control.
----------------------------------------------------------------------------------------------*/
DWORD TssListView::GetExtendedListViewStyle()
{
	return SuperClass::DefWndProc(LVM_GETEXTENDEDLISTVIEWSTYLE, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the handle to the header control used by the list view control.
----------------------------------------------------------------------------------------------*/
HWND TssListView::GetHeaderCtrl()
{
	return (HWND)SuperClass::DefWndProc(LVM_GETHEADER, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the HCURSOR value used when the pointer is over an item while hot tracking is
	enabled.
----------------------------------------------------------------------------------------------*/
HCURSOR TssListView::GetHotCursor()
{
	return (HCURSOR)SuperClass::DefWndProc(LVM_GETHOTCURSOR, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the index of the hot item.
----------------------------------------------------------------------------------------------*/
int TssListView::GetHotItem()
{
	return SuperClass::DefWndProc(LVM_GETHOTITEM, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the amount of time that the mouse cursor must hover over an item before it is
	selected.
----------------------------------------------------------------------------------------------*/
DWORD TssListView::GetHoverTime()
{
	return SuperClass::DefWndProc(LVM_GETHOVERTIME, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the handle to an image list used for drawing list view items.
----------------------------------------------------------------------------------------------*/
HIMAGELIST TssListView::GetImageList(int iImageList)
{
	return (HIMAGELIST)SuperClass::DefWndProc(LVM_GETIMAGELIST, iImageList, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the incremental search string of a list view control.
----------------------------------------------------------------------------------------------*/
uint TssListView::GetISearchString(StrUni & stu)
{
	// TODO
	//return SuperClass::DefWndProc(LVM_GETISEARCHSTRING, 0, (LPARAM)lpsz);
	return 0;
}


uint TssListView::GetISearchString(ITsString ** pptss)
{
	AssertPtr(pptss);
	Assert(!*pptss);

	// TODO
	//return SuperClass::DefWndProc(LVM_GETISEARCHSTRING, 0, (LPARAM)lpsz);
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Retrieves some or all of a list view item's attributes.
----------------------------------------------------------------------------------------------*/
bool TssListView::GetItem(FW_LVITEM * plvi)
{
	AssertPtr(plvi);

	LVITEM lvi = { plvi->mask, plvi->iItem, plvi->iSubItem };
	lvi.pszText = s_rgchBuffer;
	lvi.cchTextMax = kcchMaxText;
	bool fT = SuperClass::DefWndProc(LVM_GETITEM, 0, (LPARAM)&lvi);
	_CopyItem(lvi, *plvi);
	return fT;
}


/*----------------------------------------------------------------------------------------------
	Retrieves the number of items in a list view control.
----------------------------------------------------------------------------------------------*/
int TssListView::GetItemCount()
{
	return SuperClass::DefWndProc(LVM_GETITEMCOUNT, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the position of a list view item.
----------------------------------------------------------------------------------------------*/
bool TssListView::GetItemPosition(int i, POINT * ppt)
{
	AssertPtr(ppt);

	return SuperClass::DefWndProc(LVM_GETITEMPOSITION, (WPARAM)i, (LPARAM)ppt);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the bounding rectangle for all or part of an item in the current view.
----------------------------------------------------------------------------------------------*/
bool TssListView::GetItemRect(int i, RECT * prc)
{
	AssertPtr(prc);

	return SuperClass::DefWndProc(LVM_GETITEMRECT, i, (LPARAM)prc);
}


/*----------------------------------------------------------------------------------------------
	Determines the spacing between items in a list view control.
----------------------------------------------------------------------------------------------*/
uint TssListView::GetItemSpacing(bool fSmall)
{
	return SuperClass::DefWndProc(LVM_GETITEMSPACING, fSmall, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the state of a list view item.
----------------------------------------------------------------------------------------------*/
uint TssListView::GetItemState(int i, uint mask)
{
	return SuperClass::DefWndProc(LVM_GETITEMSTATE, i, mask);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the text of a list view item or subitem.
----------------------------------------------------------------------------------------------*/
int TssListView::GetItemText(int iItem, FW_LVITEM * plvi)
{
	AssertPtr(plvi);

	LVITEM lvi = { plvi->mask, plvi->iItem, plvi->iSubItem };
	lvi.pszText = s_rgchBuffer;
	lvi.cchTextMax = kcchMaxText;
	int nT = SuperClass::DefWndProc(LVM_GETITEMTEXT, iItem, (LPARAM)&lvi);
	_CopyItem(lvi, *plvi);
	return nT;
}


/*----------------------------------------------------------------------------------------------
	Retrieves the text of a list view item or subitem, converting it to Ansi
	Under normal circumstances this causes an Assert because it indicates a programming
	error. We should be calling an Ansi version when we are dealing with Unicode. However,
	automatic testing software isn't smart enough to know about our special methods to
	handle TsStrings, so if a FwAutoTest environment varible is set, this will go ahead and
	convert the TsString to ANSI and return it.
----------------------------------------------------------------------------------------------*/
int TssListView::GetItemTextA(int iItem, LVITEM * plvi)
{
	AssertPtr(plvi);

	size_t requiredSize;
	getenv_s( &requiredSize, NULL, 0, "FwAutoTest" );
	if (!requiredSize)
	{
		// No environment variable, so Assert. Our program should not be calling this.
		Assert(false);
		return false;
	}

	// We are being called from an automatic testing program, so return an ANSI string
	FW_LVITEM fwlvi;
	fwlvi.mask = LVIF_TEXT | LVIF_PARAM;
	fwlvi.iItem = iItem;
	fwlvi.iSubItem = 0;
	int nT = GetItemText(iItem, &fwlvi);
	_CopyItem(fwlvi, *plvi);
	return nT;
}


/*----------------------------------------------------------------------------------------------
	Searches for a list view item that has the specified properties and bears the specified
	relationship to a specified item.
----------------------------------------------------------------------------------------------*/
int TssListView::GetNextItem(int iStart, uint flags)
{
	return SuperClass::DefWndProc(LVM_GETNEXTITEM, iStart, flags);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the number of working areas in a list view control.
----------------------------------------------------------------------------------------------*/
uint TssListView::GetNumberOfWorkAreas()
{
	uint cWorkAreas;
	SuperClass::DefWndProc(LVM_GETNUMBEROFWORKAREAS, 0, (LPARAM)&cWorkAreas);
	return cWorkAreas;
}


/*----------------------------------------------------------------------------------------------
	Retrieves the current view origin for a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::GetOrigin(POINT * pptOrg)
{
	AssertPtr(pptOrg);

	return SuperClass::DefWndProc(LVM_GETORIGIN, 0, (LPARAM)pptOrg);
}


/*----------------------------------------------------------------------------------------------
	Determines the number of selected items in a list view control.
----------------------------------------------------------------------------------------------*/
uint TssListView::GetSelectedCount()
{
	return SuperClass::DefWndProc(LVM_GETSELECTEDCOUNT, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the selection mark from a list view control.
----------------------------------------------------------------------------------------------*/
int TssListView::GetSelectionMark()
{
	return SuperClass::DefWndProc(LVM_GETSELECTIONMARK, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Determines the width of a specified string using the specified list view control's current
	font.
----------------------------------------------------------------------------------------------*/
int TssListView::GetStringWidth(StrUni & stu)
{
	StrAnsi sta = stu;
	return SuperClass::DefWndProc(LVM_GETSTRINGWIDTH, (WPARAM)sta.Chars(), 0);
}

int TssListView::GetStringWidth(ITsString * ptss)
{
	AssertPtr(ptss);

	const OLECHAR * prgch;
	int cch;
	HRESULT hr;
	IgnoreHr(hr = ptss->LockText(&prgch, &cch));
	if (FAILED(hr))
		return 0;
	StrAnsi sta;
	try
	{
		sta.Assign(prgch, cch);
	}
	catch (...)
	{
		ptss->UnlockText(prgch);
		return 0;
	}
	ptss->UnlockText(prgch);

	return SuperClass::DefWndProc(LVM_GETSTRINGWIDTH, (WPARAM)sta.Chars(), 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves information about the bounding rectangle for a subitem in a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::GetSubItemRect(int iItem, RECT * prc)
{
	AssertPtr(prc);

	return SuperClass::DefWndProc(LVM_GETSUBITEMRECT, iItem, (LPARAM)prc);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the text background color of a list view control.
----------------------------------------------------------------------------------------------*/
COLORREF TssListView::GetTextBkColor()
{
	return SuperClass::DefWndProc(LVM_GETTEXTBKCOLOR, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the text color of a list view control.
----------------------------------------------------------------------------------------------*/
COLORREF TssListView::GetTextColor()
{
	return SuperClass::DefWndProc(LVM_GETTEXTCOLOR, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the tooltip control that the list view control uses to display tooltips.
----------------------------------------------------------------------------------------------*/
HWND TssListView::GetToolTips()
{
	return (HWND)SuperClass::DefWndProc(LVM_GETTOOLTIPS, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the index of the topmost visible item when in list or report view.
----------------------------------------------------------------------------------------------*/
int TssListView::GetTopIndex()
{
	return SuperClass::DefWndProc(LVM_GETTOPINDEX, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the UNICODE character format flag for the control.
----------------------------------------------------------------------------------------------*/
uint TssListView::GetUnicodeFormat()
{
	return SuperClass::DefWndProc(LVM_GETUNICODEFORMAT, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the bounding rectangle of all items in the list view control. The list view must
	be in icon or small icon view.
----------------------------------------------------------------------------------------------*/
bool TssListView::GetViewRect(RECT * prc)
{
	AssertPtr(prc);

	return SuperClass::DefWndProc(LVM_GETVIEWRECT, 0, (LPARAM)prc);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the working areas from a list view control.
----------------------------------------------------------------------------------------------*/
void TssListView::GetWorkAreas(int cWorkAreas, RECT * prgrc)
{
	AssertArray(prgrc, cWorkAreas);

	SuperClass::DefWndProc(LVM_GETWORKAREAS, cWorkAreas, (LPARAM)prgrc);
}


/*----------------------------------------------------------------------------------------------
	Determines which list view item, if any, is at a specified position.
----------------------------------------------------------------------------------------------*/
int TssListView::HitTest(LVHITTESTINFO * phti)
{
	AssertPtr(phti);

	return SuperClass::DefWndProc(LVM_HITTEST, 0, (LPARAM)phti);
}


/*----------------------------------------------------------------------------------------------
	Inserts a new column in a list view control.
----------------------------------------------------------------------------------------------*/
int TssListView::InsertColumn(int iCol, const FW_LVCOLUMN * plvc)
{
	AssertPtr(plvc);

	LVCOLUMN lvc;
	lvc.pszText = s_rgchBuffer;
	lvc.cchTextMax = kcchMaxText;
	_CopyColumn(*plvc, lvc);
	return SuperClass::DefWndProc(LVM_INSERTCOLUMN, iCol, (LPARAM)&lvc);
}


/*----------------------------------------------------------------------------------------------
	Inserts a new item in a list view control.
----------------------------------------------------------------------------------------------*/
int TssListView::InsertItem(const FW_LVITEM * plvi)
{
	AssertPtr(plvi);

	LVITEM lvi;
	lvi.pszText = s_rgchBuffer;
	lvi.cchTextMax = kcchMaxText;
	_CopyItem(*plvi, lvi);
	return SuperClass::DefWndProc(LVM_INSERTITEM, 0, (LPARAM)&lvi);
}


/*----------------------------------------------------------------------------------------------
	Forces a list view control to redraw a range of items.
----------------------------------------------------------------------------------------------*/
bool TssListView::RedrawItems(int iFirst, int iLast)
{
	return SuperClass::DefWndProc(LVM_REDRAWITEMS, iFirst, iLast);
}


/*----------------------------------------------------------------------------------------------
	Scrolls the content of a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::Scroll(int dxp, int dyp)
{
	return SuperClass::DefWndProc(LVM_SCROLL, dxp, dyp);
}


/*----------------------------------------------------------------------------------------------
	Sets the background color of a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::SetBkColor(COLORREF clrBk)
{
	return SuperClass::DefWndProc(LVM_SETBKCOLOR, 0, clrBk);
}


/*----------------------------------------------------------------------------------------------
	Sets the background image in a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::SetBkImage(LVBKIMAGE * plvbki)
{
	AssertPtr(plvbki);

	return SuperClass::DefWndProc(LVM_SETBKIMAGE, 0, (LPARAM)plvbki);
}


/*----------------------------------------------------------------------------------------------
	Changes the callback mask for a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::SetCallbackMask(uint mask)
{
	return SuperClass::DefWndProc(LVM_SETCALLBACKMASK, mask, 0);
}


/*----------------------------------------------------------------------------------------------
	Sets the attributes of a list view column.
----------------------------------------------------------------------------------------------*/
bool TssListView::SetColumn(int iCol, const FW_LVCOLUMN * plvc)
{
	AssertPtr(plvc);

	LVCOLUMN lvc;
	lvc.pszText = s_rgchBuffer;
	lvc.cchTextMax = kcchMaxText;
	_CopyColumn(*plvc, lvc);
	return SuperClass::DefWndProc(LVM_SETCOLUMN, iCol, (LPARAM)&lvc);
}


/*----------------------------------------------------------------------------------------------
	Sets the left-to-right order of columns in a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::SetColumnOrderArray(int cCol, int * prgCol)
{
	AssertArray(prgCol, cCol);

	return SuperClass::DefWndProc(LVM_SETCOLUMNORDERARRAY, cCol, (LPARAM)prgCol);
}


/*----------------------------------------------------------------------------------------------
	Changes the width of a column in report or list view.
----------------------------------------------------------------------------------------------*/
bool TssListView::SetColumnWidth(int iCol, int dxp)
{
	return SuperClass::DefWndProc(LVM_SETCOLUMNWIDTH, iCol, dxp);
}


/*----------------------------------------------------------------------------------------------
	Sets extended styles in list view controls.
----------------------------------------------------------------------------------------------*/
DWORD TssListView::SetExtendedListViewStyle(DWORD dwExMask, DWORD dwExStyle)
{
	Assert(!(dwExMask & LVS_EX_INFOTIP));

	return SuperClass::DefWndProc(LVM_SETEXTENDEDLISTVIEWSTYLE, dwExMask, dwExStyle);
}


/*----------------------------------------------------------------------------------------------
	Sets the HCURSOR value that the list view control uses when the pointer is over an item while
	hot tracking is enabled.
----------------------------------------------------------------------------------------------*/
HCURSOR TssListView::SetHotCursor(HCURSOR hCursor)
{
	return (HCURSOR)SuperClass::DefWndProc(LVM_SETHOTCURSOR, 0, (LPARAM)hCursor);
}


/*----------------------------------------------------------------------------------------------
	Sets the hot item for a list view control.
----------------------------------------------------------------------------------------------*/
int TssListView::SetHotItem(int iItem)
{
	return SuperClass::DefWndProc(LVM_SETHOTITEM, iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	Sets the amount of time which the mouse cursor must hover over an item before it is selected.
----------------------------------------------------------------------------------------------*/
DWORD TssListView::SetHoverTime(DWORD dwHoverTime)
{
	return SuperClass::DefWndProc(LVM_SETHOVERTIME, 0, dwHoverTime);
}


/*----------------------------------------------------------------------------------------------
	Sets the spacing between icons in list view controls that have the LVS_ICON style.
----------------------------------------------------------------------------------------------*/
SIZE TssListView::SetIconSpacing(int dxp, int dyp)
{
	DWORD dwT = SuperClass::DefWndProc(LVM_SETICONSPACING, 0, MAKELPARAM(dxp, dyp));
	SIZE size = { LOWORD(dwT), HIWORD(dwT) };
	return size;
}


/*----------------------------------------------------------------------------------------------
	Assigns an image list to a list view control.
----------------------------------------------------------------------------------------------*/
HIMAGELIST TssListView::SetImageList(int iImageList, HIMAGELIST himl)
{
	return (HIMAGELIST)SuperClass::DefWndProc(LVM_SETIMAGELIST, iImageList, (LPARAM)himl);
}


/*----------------------------------------------------------------------------------------------
Sets some or all of a list view item's attributes.
----------------------------------------------------------------------------------------------*/
bool TssListView::SetItem(const FW_LVITEM * plvi)
{
	AssertPtr(plvi);

	LVITEM lvi;
	lvi.pszText = s_rgchBuffer;
	lvi.cchTextMax = kcchMaxText;
	_CopyItem(*plvi, lvi);
	return SuperClass::DefWndProc(LVM_SETITEM, 0, (LPARAM)&lvi);
}


/*----------------------------------------------------------------------------------------------
	Causes the list view control to allocate memory for the specified number of items or sets the
	virtual number of items in a virtual list view control. This depends on how the list view
	control was created.
----------------------------------------------------------------------------------------------*/
bool TssListView::SetItemCount(int cItems, int dwFlags)
{
	return SuperClass::DefWndProc(LVM_SETITEMCOUNT, cItems, dwFlags);
}


/*----------------------------------------------------------------------------------------------
	Moves an item to a specified position in a list view control (must be in icon or small icon
	view).
----------------------------------------------------------------------------------------------*/
bool TssListView::SetItemPosition(int iItem, int xp, int yp)
{
	return SuperClass::DefWndProc(LVM_SETITEMPOSITION, iItem, MAKELPARAM(xp, yp));
}


/*----------------------------------------------------------------------------------------------
	Moves an item to a specified position in a list view control (must be in icon or small icon
	view). This message differs from the LVM_SETITEMPOSITION message in that it uses 32-bit
	coordinates.
----------------------------------------------------------------------------------------------*/
void TssListView::SetItemPosition32(int iItem, POINT * pptNewPos)
{
	AssertPtr(pptNewPos);

	SuperClass::DefWndProc(LVM_SETITEMPOSITION32, iItem, (LPARAM)pptNewPos);
}


/*----------------------------------------------------------------------------------------------
	Changes the state of an item in a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::SetItemState(int i, FW_LVITEM * plvi)
{
	AssertPtr(plvi);

	LVITEM lvi;
	lvi.pszText = s_rgchBuffer;
	lvi.cchTextMax = kcchMaxText;
	_CopyItem(lvi, *plvi);
	return SuperClass::DefWndProc(LVM_SETITEMSTATE, i, (LPARAM)&lvi);
}


/*----------------------------------------------------------------------------------------------
	Changes the text of a list view item or subitem.
----------------------------------------------------------------------------------------------*/
bool TssListView::SetItemText(int i, FW_LVITEM * plvi)
{
	AssertPtr(plvi);

	LVITEM lvi;
	lvi.pszText = s_rgchBuffer;
	lvi.cchTextMax = kcchMaxText;
	plvi->mask = LVIF_TEXT;           // causes _CopyItem to copy the tss into s_rgchBuffer
	_CopyItem(*plvi, lvi);
	return SuperClass::DefWndProc(LVM_SETITEMTEXT, i, (LPARAM)&lvi);
}


/*----------------------------------------------------------------------------------------------
	Sets the selection mark in a list view control.
----------------------------------------------------------------------------------------------*/
int TssListView::SetSelectionMark(int iIndex)
{
	return SuperClass::DefWndProc(LVM_SETSELECTIONMARK, 0, iIndex);
}


/*----------------------------------------------------------------------------------------------
	Sets the background color of text in a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::SetTextBkColor(COLORREF clrText)
{
	return SuperClass::DefWndProc(LVM_SETTEXTBKCOLOR, 0, clrText);
}


/*----------------------------------------------------------------------------------------------
	Sets the text color of a list view control.
----------------------------------------------------------------------------------------------*/
bool TssListView::SetTextColor(COLORREF clrText)
{
	return SuperClass::DefWndProc(LVM_SETTEXTCOLOR, 0, clrText);
}


/*----------------------------------------------------------------------------------------------
	Sets the tooltip control that the list view control will use to display tooltips.
----------------------------------------------------------------------------------------------*/
HWND TssListView::SetToolTips(HWND hwndToolTip)
{
	return (HWND)SuperClass::DefWndProc(LVM_SETTOOLTIPS, 0, (LPARAM)hwndToolTip);
}


/*----------------------------------------------------------------------------------------------
	Sets the UNICODE character format flag for the control. This message allows you to change
	the character set used by the control at run time rather than having to re-create the
	control.
----------------------------------------------------------------------------------------------*/
uint TssListView::SetUnicodeFormat(bool fUnicode)
{
	return SuperClass::DefWndProc(LVM_SETUNICODEFORMAT, fUnicode, 0);
}


/*----------------------------------------------------------------------------------------------
	Sets the working areas within a list view control.
----------------------------------------------------------------------------------------------*/
void TssListView::SetWorkAreas(int cWorkAreas, RECT * prgrc)
{
	AssertArray(prgrc, cWorkAreas);

	SuperClass::DefWndProc(LVM_SETWORKAREAS, cWorkAreas, (LPARAM)prgrc);
}


/*----------------------------------------------------------------------------------------------
	Uses an application-defined comparison function to sort the items of a list view control. The
	index of each item changes to reflect the new sequence.
----------------------------------------------------------------------------------------------*/
bool TssListView::SortItems(LPARAM lParamSort, PFNLVCOMPARE pfnCompare)
{
	AssertPfn(pfnCompare);

	return SuperClass::DefWndProc(LVM_SORTITEMS, lParamSort, (LPARAM)pfnCompare);
}


/*----------------------------------------------------------------------------------------------
	Determines which list view item or subitem is at a given position.
----------------------------------------------------------------------------------------------*/
int TssListView::SubItemHitTest(LVHITTESTINFO * phti)
{
	AssertPtr(phti);

	return SuperClass::DefWndProc(LVM_SUBITEMHITTEST, 0, (LPARAM)phti);
}


/*----------------------------------------------------------------------------------------------
	Updates a list view item. If the list view control has the LVS_AUTOARRANGE style, this
	causes the list view control to be arranged.
----------------------------------------------------------------------------------------------*/
bool TssListView::Update(int iItem)
{
	return SuperClass::DefWndProc(LVM_UPDATE, iItem, 0);
}



/***********************************************************************************************
	Notification message handlers.
***********************************************************************************************/
bool TssListView::_OnBeginLabelEdit(NMLVDISPINFO * pnmdi, long & lnRet)
{
	AssertPtr(pnmdi);

	FW_NMLVDISPINFO nmdi;
	nmdi.hdr = pnmdi->hdr;
	_CopyItem(pnmdi->item, nmdi.item);

	return OnBeginLabelEdit(&nmdi, lnRet);
}


bool TssListView::_OnEndLabelEdit(NMLVDISPINFO * pnmdi, long & lnRet)
{
	AssertPtr(pnmdi);

	FW_NMLVDISPINFO nmdi;
	nmdi.hdr = pnmdi->hdr;
	_CopyItem(pnmdi->item, nmdi.item);

	return OnEndLabelEdit(&nmdi, lnRet);
}


bool TssListView::_OnGetDispInfo(NMLVDISPINFO * pnmdi, long & lnRet)
{
	AssertPtr(pnmdi);

	FW_NMLVDISPINFO nmdi;
	nmdi.hdr = pnmdi->hdr;
	_CopyItem(pnmdi->item, nmdi.item);

	return OnGetDispInfo(&nmdi, lnRet);
}


bool TssListView::_OnItemChanged(NMLISTVIEW * pnmv, long & lnRet)
{
	AssertPtr(pnmv);

	if (pnmv->uChanged & LVIF_PARAM)
	{
		// ENHANCE: Once we start using the lParam value to store addition info for each item,
		// this has to be changed. For now, we do nothing.
	}

	return OnItemChanged(pnmv, lnRet);
}


bool TssListView::_OnItemChanging(NMLISTVIEW * pnmv, long & lnRet)
{
	AssertPtr(pnmv);

	if (pnmv->uChanged & LVIF_PARAM)
	{
		// ENHANCE: Once we start using the lParam value to store addition info for each item,
		// this has to be changed. For now, we do nothing.
	}

	return OnItemChanging(pnmv, lnRet);
}


bool TssListView::_OnSetDispInfo(NMLVDISPINFO * pnmdi, long & lnRet)
{
	AssertPtr(pnmdi);

	FW_NMLVDISPINFO nmdi;
	nmdi.hdr = pnmdi->hdr;
	_CopyItem(pnmdi->item, nmdi.item);

	return OnSetDispInfo(&nmdi, lnRet);
}


void TssListView::_CopyItem(const LVITEM & lvi, FW_LVITEM & flvi)
{
	int mask = lvi.mask;

	flvi.mask = mask;
	flvi.iItem = lvi.iItem;
	flvi.iSubItem = lvi.iSubItem;

	if (mask & LVIF_TEXT)
	{
		// ENHANCE: Once we start storing TsStrings, this has to be changed.
		Assert(lvi.pszText != LPSTR_TEXTCALLBACK);

		ITsStringPtr qtss;
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);

		StrUni stu = lvi.pszText;
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &qtss);
		flvi.qtss = qtss;
	}

	if (mask & LVIF_IMAGE)
		flvi.iImage = lvi.iImage;

	if (mask & LVIF_INDENT)
		flvi.iIndent = lvi.iIndent;

	if (mask & LVIF_PARAM)
	{
		// ENHANCE: Once we start using the lParam value, this has to be changed.
		flvi.lParam = lvi.lParam;
	}

	if (mask & LVIF_STATE)
	{
		flvi.state = lvi.state;
		flvi.stateMask = lvi.stateMask;
	}

}


void TssListView::_CopyItem(const FW_LVITEM & flvi, LVITEM & lvi)
{
	int mask = flvi.mask;

	lvi.mask = mask;
	lvi.iItem = flvi.iItem;
	lvi.iSubItem = flvi.iSubItem;

	if (mask & LVIF_TEXT)
	{
		// ENHANCE: Once we start storing TsStrings, this has to be changed.
		AssertArray(lvi.pszText, lvi.cchTextMax);

		const OLECHAR * prgch;
		int cch;
		if (flvi.qtss && SUCCEEDED(flvi.qtss->LockText(&prgch, &cch)))
		{
			StrApp str;
			try
			{
				str.Assign(prgch, cch);
			}
			catch (...)
			{
			}
			flvi.qtss->UnlockText(prgch);
			_tcsncpy_s(lvi.pszText, lvi.cchTextMax, str.Chars(), lvi.cchTextMax);
		}
	}

	if (mask & LVIF_IMAGE)
		lvi.iImage = flvi.iImage;

	if (mask & LVIF_INDENT)
		lvi.iIndent = flvi.iIndent;

	if (mask & LVIF_PARAM)
	{
		// ENHANCE: Once we start using the lParam value, this has to be changed.
		lvi.lParam = flvi.lParam;
	}

	if (mask & LVIF_STATE)
	{
		lvi.state = flvi.state;
		lvi.stateMask = flvi.stateMask;
	}

}


void TssListView::_CopyColumn(const LVCOLUMN & lvc, FW_LVCOLUMN & flvc)
{
	int mask = lvc.mask;

	flvc.mask = mask;

	if (mask & LVCF_FMT)
		flvc.fmt = lvc.fmt;

	if (mask & LVCF_IMAGE)
		flvc.iImage = lvc.iImage;

	if (mask & LVCF_ORDER)
		flvc.iOrder = lvc.iOrder;

	if (mask & LVCF_SUBITEM)
		flvc.iSubItem = lvc.iSubItem;

	if (mask & LVCF_WIDTH)
		flvc.cx = lvc.cx;

	if (mask & LVCF_TEXT)
	{
		// ENHANCE: Once we start storing TsStrings, this has to be changed.
		Assert(lvc.pszText != LPSTR_TEXTCALLBACK);

		ITsStringPtr qtss;
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);

		StrUni stu = lvc.pszText;
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &qtss);
		flvc.qtss = qtss;
	}
}


void TssListView::_CopyColumn(const FW_LVCOLUMN & flvc, LVCOLUMN & lvc)
{
	int mask = flvc.mask;

	lvc.mask = mask;

	if (mask & LVCF_FMT)
		lvc.fmt = flvc.fmt;

	if (mask & LVCF_IMAGE)
		lvc.iImage = flvc.iImage;

	if (mask & LVCF_ORDER)
		lvc.iOrder = flvc.iOrder;

	if (mask & LVCF_SUBITEM)
		lvc.iSubItem = flvc.iSubItem;

	if (mask & LVCF_WIDTH)
		lvc.cx = flvc.cx;

	if (mask & LVCF_TEXT)
	{
		// ENHANCE: Once we start storing TsStrings, this has to be changed.
		AssertArray(lvc.pszText, lvc.cchTextMax);

		const OLECHAR * prgch;
		int cch;
		if (flvc.qtss && SUCCEEDED(flvc.qtss->LockText(&prgch, &cch)))
		{
			StrApp str;
			try
			{
				str.Assign(prgch, cch);
			}
			catch (...)
			{
			}
			flvc.qtss->UnlockText(prgch);
			_tcsncpy_s(lvc.pszText, lvc.cchTextMax, str.Chars(), lvc.cchTextMax);
		}
	}
}

/*LRESULT TssListView::_OnCustomDraw(NMTVCUSTOMDRAW * pnmcd)
{
	AssertPtr(pnmcd);
	FW_NMLISTVIEW fntv;
	fntv.hdr = pnmv->hdr;
	fntv.itemNew.hItem = pnmv->itemNew.hItem;
	fntv.itemNew.state = pnmv->itemNew.state;
	FwTreeItem * pfti = (FwTreeItem *)pnmv->itemNew.lParam;
	AssertPtr(pfti);
	fntv.itemNew.lParam = pfti->lParam;
	fntv.ptDrag = pnmv->ptDrag;
	fntv.hdr.code = FW_TVN_BEGINRDRAG;
	if (m_fCreatedNew)
		return SendMessage(m_hwndParent, WM_NOTIFY, nID, (LPARAM)&fntv);
	return OnBeginRDrag(&fntv);
};*/

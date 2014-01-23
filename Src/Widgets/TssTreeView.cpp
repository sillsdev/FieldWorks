/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TssTreeView.cpp
Responsibility: Rand Burgett
Last reviewed:

	Implementation of TssTreeView.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


achar TssTreeView::s_rgchBuffer[kcchMaxText];

// ENHANCE: Remove this for next versions (supporting Unicode/Non-Roman).
#define FW_VERSION_1 1
#undef FW_VERSION_1

/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
TssTreeView::TssTreeView()
{
	Assert(_WIN32_IE >= 0x0300);
}

/*----------------------------------------------------------------------------------------------
// This function must be called before the treeview control has been initialized with any data.
// (i.e. before any items are inserted into it.)
----------------------------------------------------------------------------------------------*/
void TssTreeView::SubclassTreeView(HWND hwnd)
{
	SubclassHwnd(hwnd);
	Assert(GetCount() == 0);
}

void TssTreeView::PreCreateHwnd(CREATESTRUCT & cs)
{
	cs.style |= WS_CHILD;

	m_qvcd.CreateInstance(CLSID_VwCacheDa);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void TssTreeView::SubclassHwnd(HWND hwnd)
{
	SuperClass::SubclassHwnd(hwnd);

	CREATESTRUCT cs;
	SuperClass::OnCreate(&cs);

	HWND hwndToolTip = (HWND)::SendMessage(m_hwnd, TVM_GETTOOLTIPS, 0, 0);
	m_qttw.Attach(NewObj AfToolTipWnd);
	m_qttw->SubclassToolTip(hwndToolTip);
}


/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
bool TssTreeView::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case TVM_CREATEDRAGIMAGE:
		lnRet = (long)CreateDragImage((HTREEITEM)lp);
		return true;

	case TVM_DELETEITEM:
		lnRet = DeleteItem((HTREEITEM)lp);
		return true;

	case TVM_EDITLABEL:
		lnRet = (long)EditLabel((HTREEITEM)lp);
		return true;

	case TVM_ENDEDITLABELNOW:
		lnRet = EndEditLabelNow(wp);
		return true;

	case TVM_ENSUREVISIBLE:
		lnRet = EnsureVisible((HTREEITEM)lp);
		return true;

	case TVM_EXPAND:
		lnRet = Expand((HTREEITEM)wp, lp);
		::InvalidateRect(m_hwnd, NULL, FALSE);
		::UpdateWindow(m_hwnd);
		return true;

	case TVM_GETBKCOLOR:
		lnRet = GetBkColor();
		return true;

	case TVM_GETCOUNT:
		lnRet = GetCount();
		return true;

	case TVM_GETEDITCONTROL:
		lnRet = (long)GetEditControl();
		return true;

	case TVM_GETIMAGELIST:
		lnRet = (long)GetImageList(wp);
		return true;

	case TVM_GETINDENT:
		lnRet = GetIndent();
		return true;

	case TVM_GETINSERTMARKCOLOR:
		lnRet = GetInsertMarkColor();
		return true;

	case TVM_GETISEARCHSTRING:
		lnRet = GetISearchString((LPSTR)lp);
		return true;

	case TVM_GETITEM:
		lnRet = GetItem((LPFW_TVITEM)lp);
		return true;

	case TVM_GETITEMHEIGHT:
		lnRet = GetItemHeight();
		return true;

	case TVM_GETITEMRECT:
		lnRet = GetItemRect((BOOL)wp, (LPRECT)lp);
		return true;

	case TVM_GETNEXTITEM:
		lnRet = (long)GetNextItem(wp, (HTREEITEM)lp);
		return true;

	case TVM_GETSCROLLTIME:
		lnRet = GetScrollTime();
		return true;

	case TVM_GETTEXTCOLOR:
		lnRet = GetTextColor();
		return true;

	case TVM_GETTOOLTIPS:
		lnRet = (long)GetToolTips();
		return true;

	case TVM_GETUNICODEFORMAT:
		lnRet = GetUnicodeFormat();
		return true;

	case TVM_GETVISIBLECOUNT:
		lnRet = GetVisibleCount();
		return true;

	case TVM_HITTEST:
		lnRet = (long)HitTest((LPTVHITTESTINFO) lp);
		return true;

	case TVM_INSERTITEM:
		lnRet = (long)InsertItem((LPFW_TVINSERTSTRUCT) lp);
		return true;

	case TVM_SELECTITEM:
		lnRet = SelectItem(wp, (HTREEITEM)lp);
		::InvalidateRect(m_hwnd, NULL, FALSE);
		::UpdateWindow(m_hwnd);
		return true;

	case TVM_SETBKCOLOR:
		lnRet = SetBkColor(lp);
		return true;

	case TVM_SETIMAGELIST:
		lnRet = (long)SetImageList(wp, (HIMAGELIST) lp);
		return true;

	case TVM_SETINDENT:
		SetIndent(wp);
		return true;

	case TVM_SETINSERTMARK:
		lnRet = SetInsertMark((HTREEITEM)wp, lp);
		return true;

	case TVM_SETINSERTMARKCOLOR:
		lnRet = SetInsertMarkColor(lp);
		return true;

	case TVM_SETITEM:
		lnRet = SetItem((const LPFW_TVITEM)lp);
		return true;

	case TVM_SETITEMHEIGHT:
		lnRet = SetItemHeight((SHORT)wp);
		return true;

	case TVM_SETSCROLLTIME:
		lnRet = SetScrollTime((UINT)wp);
		return true;

	case TVM_SETTEXTCOLOR:
		lnRet = SetTextColor(lp);
		return true;

	case TVM_SETTOOLTIPS:
		lnRet = (long)SetToolTips((HWND)wp);
		return true;

	case TVM_SETUNICODEFORMAT:
		lnRet = SetUnicodeFormat(wp);
		return true;

	case TVM_SORTCHILDREN:
		lnRet = SortChildren((HTREEITEM)lp);
		return true;

	case TVM_SORTCHILDRENCB:
		lnRet = SortChildrenCB((BOOL)wp, (LPTVSORTCB)lp);
		return true;

/*	case WM_DESTROY:
		lnRet = OnDestroy();
		return true;

	case WM_SIZE:
		MoveWindow(pttv->m_hwnd, 0, 0, LOWORD(lp), HIWORD(lp), true);
		return true;
*/

	case WM_DESTROY:
		DeleteAllItems();
		return false;

	default:
		return false;
	}
}

/***********************************************************************************************
	TssTreeView message handlers
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
bool TssTreeView::OnNotifyThis(int id, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	Assert(pnmh->hwndFrom == m_hwnd);

	switch (pnmh->code)
	{
#ifdef FW_VERSION_1
	case TVN_GETINFOTIP:
		Assert(false);
		return true;
#endif // FW_VERSION_1

	case TVN_BEGINDRAG:
		return _OnBeginDrag((NMTREEVIEW *)pnmh, lnRet);

	case TVN_BEGINLABELEDIT:
		return _OnBeginLabelEdit((NMTVDISPINFO *)pnmh, lnRet);

	case TVN_BEGINRDRAG:
		return _OnBeginRDrag((NMTREEVIEW *)pnmh, lnRet);

	// REVIEW DarrellZ: For some reason, Windows 98 is sending the Unicode version
	// of this notification. Figure out why.
	case TVN_DELETEITEMA:
	case TVN_DELETEITEMW:
		return _OnDeleteItem((NMTREEVIEW *)pnmh, lnRet);

	case TVN_ENDLABELEDIT:
		return _OnEndLabelEdit((NMTVDISPINFO *)pnmh, lnRet);

	case TVN_GETDISPINFO:
		return _OnGetDispInfo((NMTVDISPINFO *)pnmh, lnRet);

	case TVN_ITEMEXPANDED:
		{
			bool rt = _OnItemExpanded((NMTREEVIEW *)pnmh, lnRet);
			// The UpdateWindow must be called twice in order for it to work when
			// different items use different fonts.
			::UpdateWindow(m_hwnd);
			::InvalidateRect(m_hwnd, NULL, FALSE);
			::UpdateWindow(m_hwnd);

			return rt;
		}
	case TVN_ITEMEXPANDINGA:
	case TVN_ITEMEXPANDINGW:
		return _OnItemExpanding((NMTREEVIEW *)pnmh, lnRet);

	case TVN_KEYDOWN:
		return _OnKeyDown((NMTVKEYDOWN *)pnmh, lnRet);

	case TVN_SELCHANGED:
		{
			bool rt = _OnSelChanged((NMTREEVIEW *)pnmh, lnRet);
			::InvalidateRect(m_hwnd, NULL, FALSE);
			::UpdateWindow(m_hwnd);
			return rt;
		}

	case TVN_SELCHANGINGA:
	case TVN_SELCHANGINGW:
		return _OnSelChanging((NMTREEVIEW *)pnmh, lnRet);

	case TVN_SETDISPINFO:
		return _OnSetDispInfo((NMTREEVIEW *)pnmh, lnRet);

	case TVN_SINGLEEXPAND:
		{
			bool rt = _OnSingleExpand((NMTREEVIEW *)pnmh, lnRet);
			::InvalidateRect(m_hwnd, NULL, FALSE);
			::UpdateWindow(m_hwnd);
			return rt;
		}

	case NM_CLICK:
		return OnClick(pnmh, lnRet);

	case NM_CUSTOMDRAW:
		return _OnCustomDraw((NMTVCUSTOMDRAW *)pnmh, lnRet);

	case NM_DBLCLK:
		return OnDblClk(pnmh, lnRet);

	case NM_KILLFOCUS:
		return OnKillFocus(pnmh, lnRet);

	case NM_RCLICK:
		return OnRClick(pnmh, lnRet);

	case NM_RDBLCLK:
		return OnRDblClk(pnmh, lnRet);

	case NM_RETURN:
		return OnReturn(pnmh, lnRet);

	case NM_SETCURSOR:
		return OnSetCursor(pnmh, lnRet);

	case NM_SETFOCUS:
		return OnSetFocus(pnmh, lnRet);

	default:
		return false;
	}
}


/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	if (pnmh->code == TTN_SHOW)
	{
		// Find out which treeview item we're currently over, and set the text
		// of the tooltip to the correct string.
		TVHITTESTINFO thti;
		::GetCursorPos(&thti.pt);
		::ScreenToClient(m_hwnd, &thti.pt);
		HTREEITEM hti = (HTREEITEM)::SendMessage(m_hwnd, TVM_HITTEST, 0, (LPARAM)&thti);
		if (hti && (thti.flags & TVHT_ONITEM))
		{
			TVITEM tvi = { TVIF_PARAM, hti };
			if (SuperClass::DefWndProc(TVM_GETITEM, 0, (LPARAM)&tvi))
			{
				// Update the tooltip text.
				FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
				AssertPtr(pfti);
				m_qttw->UpdateText(pfti->qtss);

				// Make sure the tooltip is wide/tall enough for the string to be displayed.
				Rect rc;
				*(HTREEITEM*)&rc = hti;
				::SendMessage(m_hwnd, TVM_GETITEMRECT, true, (LPARAM)&rc);
				::SendMessage(m_qttw->Hwnd(), TTM_ADJUSTRECT, true, (LPARAM)&rc);
				::SetWindowPos(m_qttw->Hwnd(), NULL, 0, 0, rc.Width(), rc.Height(),
					SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOMOVE);
			}
		}
		// Return false here to show the tooltip in the default location.
		return false;
	}

	return SuperClass::OnNotifyChild(id, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	This is necessary to get the CustomDraw notifications.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::OnPaint(HDC hdcDef)
{
	Assert(!hdcDef);

	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps);
	Assert(hdc);
	Rect rc = ps.rcPaint;

	// Create the virtual screen in memory.
	HDC hdcMem = AfGdi::CreateCompatibleDC(hdc);
	Assert(hdcMem);
	HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, rc.Width(), rc.Height());
	Assert(hbmp);
	HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
	::SetViewportOrgEx(hdcMem, -rc.left, -rc.top, NULL);
	AfGfx::FillSolidRect(hdcMem, rc, ::GetSysColor(COLOR_WINDOW));

	// Draw the tree view in memory.
	DefWndProc(WM_PAINT, (WPARAM)hdcMem, 0);

	// Copy image to the screen.
	::BitBlt(hdc, rc.left, rc.top, rc.Width(), rc.Height(), hdcMem, rc.left, rc.top, SRCCOPY);

	// Clean up.
	HBITMAP hbmpDebug;
	hbmpDebug = AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);
	Assert(hbmpDebug && hbmpDebug != HGDI_ERROR);
	Assert(hbmpDebug == hbmp);

	BOOL fSuccess;
	fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
	Assert(fSuccess);

	fSuccess = AfGdi::DeleteDC(hdcMem);
	Assert(fSuccess);

	::EndPaint(m_hwnd, &ps);

	return true;
}


/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
bool TssTreeView::OnSize(int nId, int dxp, int dyp)
{
	// Make sure the tree view knows that the size has changed.
	DefWndProc(WM_SIZE, nId, MAKELPARAM(dxp, dyp));
	// NOTE: We specifically don't call the SuperClass::OnSize method here because the rootbox
	// is layed out in the _OnCustomDraw method, so it's just extra work to do it here.
	return true;
}


/***********************************************************************************************
	methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Call this function to create a dragging bitmap for the given item in a tree view control,
	create an image list for the bitmap, and add the bitmap to the image list. An application
	uses the image-list functions to display the image when the item is being dragged.
----------------------------------------------------------------------------------------------*/
HIMAGELIST TssTreeView::CreateDragImage(HTREEITEM hItem)
{
	return (HIMAGELIST)SuperClass::DefWndProc(TVM_CREATEDRAGIMAGE, 0, (LPARAM)hItem);
}

/*----------------------------------------------------------------------------------------------
Call this function to delete an item from the tree view control.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::DeleteItem(HTREEITEM hItem)
{
	return SuperClass::DefWndProc(TVM_DELETEITEM, 0, (LPARAM)hItem);
}


/*----------------------------------------------------------------------------------------------
	Call this function to delete all items from the tree view control.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::DeleteAllItems()
{
	return SuperClass::DefWndProc(TVM_DELETEITEM, 0, (LPARAM)TVI_ROOT);
}


/*----------------------------------------------------------------------------------------------
	Call this function to begin in-place editing of the specified item’s text. The editing is
	accomplished by replacing the text of the item with a single-line edit control containing
	the text.
----------------------------------------------------------------------------------------------*/
// ENHANCE: Change this to VwEdit * instead of HWND once it gets finished.
HWND TssTreeView::EditLabel(HTREEITEM hItem)
{
	return (HWND)SuperClass::DefWndProc(TVM_EDITLABEL, 0, (LPARAM)hItem);
}

/*----------------------------------------------------------------------------------------------
Ends the editing of a tree view item's label.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::EndEditLabelNow(bool fCancel)
{
	return SuperClass::DefWndProc(TVM_ENDEDITLABELNOW, (WPARAM)fCancel, 0);
}

/*----------------------------------------------------------------------------------------------
	Call this function to ensure that a tree view item is visible. If necessary, the function
	expands the parent item or scrolls the tree view control so that the item is visible.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::EnsureVisible(HTREEITEM hItem)
{
	return SuperClass::DefWndProc(TVM_ENSUREVISIBLE,0, (LPARAM)hItem);
}

/*----------------------------------------------------------------------------------------------
	Call this function to expand or collapse the list of child items, if any, associated with
	the given parent item.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::Expand(HTREEITEM hItem, UINT nCode)
{
	return SuperClass::DefWndProc(TVM_EXPAND, (WPARAM)hItem, (LPARAM)nCode);
}

/*----------------------------------------------------------------------------------------------
	This member function implements the behavior of the Win32 messageTVM_GETBKCOLOR, as
	described in the Platform SDK.
----------------------------------------------------------------------------------------------*/
COLORREF TssTreeView::GetBkColor()
{
	return SuperClass::DefWndProc(TVM_GETBKCOLOR, 0, 0);
}

/*----------------------------------------------------------------------------------------------
Call this function to retrieve a count of the items in a tree view control.
----------------------------------------------------------------------------------------------*/
UINT TssTreeView::GetCount()
{
	return SuperClass::DefWndProc(TVM_GETCOUNT, 0, 0);
}

/*----------------------------------------------------------------------------------------------
	Call this function to retrieve the handle of the edit control being used to edit a tree
	view item’s text.
----------------------------------------------------------------------------------------------*/
// ENHANCE: Change this to VwEdit * instead of HWND once it gets finished.
HWND TssTreeView::GetEditControl( )
{
	return (HWND)SuperClass::DefWndProc(TVM_GETEDITCONTROL, 0, 0);
}

/*----------------------------------------------------------------------------------------------
	Call this function to retrieve the handle of the normal or state image list associated with
	the tree view control. Each item in a tree view control can have a pair of bitmapped images
	associated with it. One image is displayed when the item is selected, and the other is
	displayed when the item is not selected. For example, an item might display an open folder
	when it is selected and a closed folder when it is not selected.
----------------------------------------------------------------------------------------------*/
HIMAGELIST TssTreeView::GetImageList(UINT nImage)
{
	return (HIMAGELIST)SuperClass::DefWndProc(TVM_GETIMAGELIST, (WPARAM) nImage, 0);
}

/*----------------------------------------------------------------------------------------------
	Call this function to retrieve the amount, in pixels, that child items are indented relative
	to their parent items.
----------------------------------------------------------------------------------------------*/
UINT TssTreeView::GetIndent()
{
	return SuperClass::DefWndProc(TVM_GETINDENT, 0, 0);
}

/*----------------------------------------------------------------------------------------------
	This member function implements the behavior of the Win32 message TVM_GETINSERTMARKCOLOR, as
	described in the Platform SDK.
----------------------------------------------------------------------------------------------*/
COLORREF TssTreeView::GetInsertMarkColor( )
{
	return SuperClass::DefWndProc(TVM_GETINSERTMARKCOLOR, 0, 0);
}

/*----------------------------------------------------------------------------------------------
	Retrieves the incremental search string for a tree view control. The tree view control uses
	the incremental search string to select an item based on characters typed by the user.
----------------------------------------------------------------------------------------------*/
UINT TssTreeView::GetISearchString(LPSTR lpsz)
{
	return SuperClass::DefWndProc(TVM_GETISEARCHSTRING, 0, (LPARAM)lpsz);
}


/*----------------------------------------------------------------------------------------------
	Call this function to retrieve the attributes of the specified tree view item.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::GetItem(FW_TVITEM * pItem)
{
	AssertPtr(pItem);

	TVITEM tvi = { pItem->mask, pItem->hItem};
	tvi.pszText = s_rgchBuffer;
	tvi.cchTextMax = kcchMaxText;
	bool fT = SuperClass::DefWndProc(TVM_GETITEM, 0, (LPARAM)&tvi);
	if (fT)
		_CopyItem(tvi, *pItem);
	return fT;
}

/*----------------------------------------------------------------------------------------------
	This member function implements the behavior of the Win32 message TVM_GETITEMHEIGHT, as
	described in the Platform SDK.
----------------------------------------------------------------------------------------------*/
SHORT TssTreeView::GetItemHeight()
{
	return (SHORT)SuperClass::DefWndProc(TVM_GETITEMHEIGHT, 0, 0);
}

/*----------------------------------------------------------------------------------------------
	Call this function to retrieve the bounding rectangle for hItem and determine whether it is
	visible or not.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::GetItemRect(HTREEITEM hItem, RECT * pRect, bool fTextOnly)
{
	AssertPtr(pRect);
	*(HTREEITEM *)pRect = hItem;
	return SuperClass::DefWndProc(TVM_GETITEMRECT, fTextOnly, (LPARAM)pRect);
}

/*----------------------------------------------------------------------------------------------
	Call this function to retrieve the bounding rectangle for hItem and determine whether it is
	visible or not.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::GetItemRect(bool fTextOnly, RECT * pRect)
{
	AssertPtr(pRect);
	return SuperClass::DefWndProc(TVM_GETITEMRECT, fTextOnly, (LPARAM)pRect);
}

/*----------------------------------------------------------------------------------------------
	Call this function to retrieve the tree view item that has the specified relationship,
	indicated by the nCode parameter, to hItem.
----------------------------------------------------------------------------------------------*/
HTREEITEM TssTreeView::GetNextItem(UINT nCode, HTREEITEM hItem)
{
	return (HTREEITEM)SuperClass::DefWndProc(TVM_GETNEXTITEM, (WPARAM)nCode, (LPARAM)hItem);
}

/*----------------------------------------------------------------------------------------------
Retrieves the maximum scroll time for the tree view control.
----------------------------------------------------------------------------------------------*/
UINT TssTreeView::GetScrollTime()
{
	return SuperClass::DefWndProc(TVM_GETSCROLLTIME, 0, 0);
}

/*----------------------------------------------------------------------------------------------
	This member function implements the behavior of the Win32 messageTVM_GETTEXTCOLOR, as
	described in the Platform SDK.
----------------------------------------------------------------------------------------------*/
COLORREF TssTreeView::GetTextColor()
{
	return SuperClass::DefWndProc(TVM_GETTEXTCOLOR, 0, 0);
}

/*----------------------------------------------------------------------------------------------
	This member function implements the behavior of the Win32 messageTVM_GETTOOLTIPS, as
	described in the Platform SDK.
----------------------------------------------------------------------------------------------*/
HWND TssTreeView::GetToolTips()
{
	return (HWND)SuperClass::DefWndProc(TVM_GETTOOLTIPS, 0, 0);
}

/*----------------------------------------------------------------------------------------------
	Retrieves the UNICODE character format flag for the control.
----------------------------------------------------------------------------------------------*/
uint TssTreeView::GetUnicodeFormat()
{
	return SuperClass::DefWndProc(TVM_GETUNICODEFORMAT, 0, 0);
}

/*----------------------------------------------------------------------------------------------
	Call this function to retrieve a count of the visible items in a tree view control.
----------------------------------------------------------------------------------------------*/
UINT TssTreeView::GetVisibleCount()
{
	return SuperClass::DefWndProc(TVM_GETVISIBLECOUNT, 0, 0);
}

/*----------------------------------------------------------------------------------------------
	Call this function to determine the location of the specified point relative to the client
	area of a tree view control. When this function is called, the pt parameter specifies the
	coordinates of the point to test. The function returns the handle of the item at the
	specified point or NULL if no item occupies the point. In addition, the pFlags parameter
	contains a value that indicates the location of the specified point.
----------------------------------------------------------------------------------------------*/
HTREEITEM TssTreeView::HitTest(POINT pt, UINT * pFlags)
{
	AssertPtr(pFlags);
	TVHITTESTINFO thti;
	thti.pt = pt;
	HTREEITEM hItem = (HTREEITEM)SuperClass::DefWndProc(TVM_HITTEST, 0, (LPARAM)&thti);
	*pFlags = thti.flags;
	return hItem;
}

HTREEITEM TssTreeView::HitTest(TVHITTESTINFO * pHitTestInfo)
{
	return (HTREEITEM)SuperClass::DefWndProc(TVM_HITTEST, 0, (LPARAM)pHitTestInfo);
}

/*----------------------------------------------------------------------------------------------
	Call this function to insert a new item in a tree view control.
----------------------------------------------------------------------------------------------*/
HTREEITEM TssTreeView::InsertItem(FW_TVINSERTSTRUCT * pftis)
{
	AssertPtr(pftis);
	return InsertItem(pftis->itemex.mask, pftis->itemex.qtss, pftis->itemex.iImage,
		pftis->itemex.iSelectedImage, pftis->itemex.cChildren, pftis->itemex.iIntegral,
		pftis->itemex.state, pftis->itemex.stateMask, pftis->itemex.lParam, pftis->hParent,
		pftis->hInsertAfter);
}

HTREEITEM TssTreeView::InsertItem(UINT nMask, ITsString * ptss, int iImage, int iSelectedImage,
	int cChildren, int nIntegral, UINT nState, UINT nStateMask, LPARAM lParam,
	HTREEITEM hParent, HTREEITEM hInsertAfter)
{
	AssertPtr(ptss);
	TVINSERTSTRUCT tis;
	tis.hInsertAfter = hInsertAfter;
	tis.hParent = hParent;
	tis.itemex.mask = nMask | TVIF_PARAM;
	FwTreeItem * pfti = NewObj FwTreeItem;
	if (!pfti)
		return NULL;
	pfti->qtss = ptss;
	pfti->lParam = lParam;
#ifdef FW_VERSION_1
	StrApp str;
	const OLECHAR * pwrgch;
	int cch;
	HRESULT hr;
	IgnoreHr(hr = ptss->LockText(&pwrgch, &cch));
	if (FAILED(hr))
		return NULL;
	str.Assign(pwrgch, cch);
	ptss->UnlockText(pwrgch);
//	if (sta.Error())
//		return NULL;
	tis.itemex.pszText = const_cast<achar *>(str.Chars());
	tis.itemex.cchTextMax = cch;
#else // !FW_VERSION_1
	//tis.itemex.pszText = NULL;
	tis.item.pszText = (LPTSTR)LPSTR_TEXTCALLBACK;
#endif // !FW_VERSION_1
	tis.itemex.iImage = iImage;
	tis.itemex.iSelectedImage = iSelectedImage;
	tis.itemex.iIntegral = nIntegral;
	tis.itemex.state = nState;
	tis.itemex.stateMask = nStateMask;
	tis.itemex.lParam = (LPARAM)pfti;
	return (HTREEITEM) SuperClass::DefWndProc(TVM_INSERTITEM, 0, (LPARAM)&tis);
}

HTREEITEM TssTreeView::InsertItem(ITsString * ptss, HTREEITEM hParent, HTREEITEM hInsertAfter)
{
	AssertPtr(ptss);
	return (HTREEITEM) InsertItem(TVIF_TEXT, ptss, 0, 0, 0, 1, 0, 0, 0, hParent, hInsertAfter);
}

HTREEITEM TssTreeView::InsertItem(ITsString * ptss, int iImage, int iSelectedImage, HTREEITEM hParent,
	HTREEITEM hInsertAfter)
{
	AssertPtr(ptss);
	return InsertItem(TVIF_TEXT | TVIF_IMAGE | TVIF_SELECTEDIMAGE, ptss, iImage,
		iSelectedImage, 0, 1, 0, 0, 0, hParent, hInsertAfter);
}

/*----------------------------------------------------------------------------------------------
	Call this function to select the given tree view item. If hItem is NULL, then this function
	selects no item.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::SelectItem(WPARAM flag, HTREEITEM hItem)
{
	return SuperClass::DefWndProc(TVM_SELECTITEM, flag, (LPARAM)hItem);
}

/*----------------------------------------------------------------------------------------------
	This member function implements the behavior of the Win32 messageTVM_SETBKCOLOR, as
	described in the Platform SDK.
----------------------------------------------------------------------------------------------*/
COLORREF TssTreeView::SetBkColor(COLORREF clr)
{
	return SuperClass::DefWndProc(TVM_SETBKCOLOR, 0, (LPARAM)clr);
}

/*----------------------------------------------------------------------------------------------
	Call this function to set the normal or state image list for a tree view control and redraw
	the control using the new images.
----------------------------------------------------------------------------------------------*/
HIMAGELIST TssTreeView::SetImageList( int nImageListType, HIMAGELIST hImageList)
{
	return (HIMAGELIST)SuperClass::DefWndProc(TVM_SETIMAGELIST, (WPARAM)nImageListType, (LPARAM)hImageList);
}

/*----------------------------------------------------------------------------------------------
	Call this function to set the width of indentation for a tree view control and redraw the
	control to reflect the new width.
----------------------------------------------------------------------------------------------*/
void TssTreeView::SetIndent(UINT nIndent)
{
	SuperClass::DefWndProc(TVM_SETINDENT, (WPARAM)nIndent, 0);
}

/*----------------------------------------------------------------------------------------------
	This member function implements the behavior of the Win32 messageTVM_SETINSERTMARK, as
	described in the Platform SDK.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::SetInsertMark(HTREEITEM hItem, bool fAfter)
{
	return SuperClass::DefWndProc(TVM_SETINSERTMARK, (WPARAM)fAfter, (LPARAM)hItem);
}

/*----------------------------------------------------------------------------------------------
	This member function implements the behavior of the Win32 messageTVM_SETINSERTMARKCOLOR, as
	described in the Platform SDK.
----------------------------------------------------------------------------------------------*/
COLORREF TssTreeView::SetInsertMarkColor(COLORREF clrNew)
{
	return SuperClass::DefWndProc(TVM_SETINSERTMARKCOLOR, 0, (LPARAM)clrNew);
}

/*----------------------------------------------------------------------------------------------
	Call this function to set the attributes of the specified tree view item.  In the TVITEM
	structure, the hItem member identifies the item, and the mask member specifies which
	attributes to set.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::SetItem(FW_TVITEM * pfsi)
{
	AssertPtr(pfsi);

	// Because the input is a FW_TVITEM, the lParam data member should already be a pointer to
	// an FwTreeItem, if TVIF_PARAM is in the mask, so get the "real" lParam value:
	LPARAM lparam = 0;
	if ((pfsi->mask & TVIF_PARAM) && pfsi->lParam)
		lparam = ((FwTreeItem *)pfsi->lParam)->lParam;

	return SetItem(pfsi->hItem, pfsi->mask, pfsi->qtss, pfsi->iImage,
		pfsi->iSelectedImage, pfsi->state, pfsi->stateMask, lparam);
}

bool TssTreeView::SetItem(HTREEITEM hItem, UINT nMask, ITsString * ptss, int iImage,
	int iSelectedImage, UINT nState, UINT nStateMask, LPARAM lParam )
{
	TVITEM tvi;

	tvi.mask = nMask;
	// Check if the lParam value needs to be set:
	if (nMask & TVIF_PARAM)
	{
		// We have to incorproate the lParam value in an FwTreeItem:
		FwTreeItem * pfti = NewObj FwTreeItem;
		if (ptss)
			pfti->qtss = ptss;
		else
		{
			// We need an lParam value, but there is no TsString, so make up a blank one for the
			// FwTreeItem:
			// REVIEW: Is this a good idea?
			ITsStringPtr qtss;
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			Assert(m_wsUser);
			qtsf->MakeStringRgch(L"", 0, m_wsUser, &qtss);
			pfti->qtss = qtss;
		}
		pfti->lParam = lParam;
		tvi.lParam = (LPARAM)pfti;
	}
	if (nMask & TVIF_TEXT)
	{
#ifdef FW_VERSION_1
		if (!ptss)
		{
			tvi.pszText = _T("");
			tvi.cchTextMax = 0;
		}
		else
		{
			StrApp str;
			const OLECHAR * pwrgch;
			int cch;
			HRESULT hr;
			IgnoreHr(hr = ptss->LockText(&pwrgch, &cch));
			if (FAILED(hr))
				return NULL;
			str.Assign(pwrgch, cch);
			ptss->UnlockText(pwrgch);
//			if (sta.Error())
//				return NULL;
			tvi.pszText = const_cast<achar *>(str.Chars());
			tvi.cchTextMax = cch;
		}
#else // !FW_VERSION_1
		//tvi.pszText = NULL;
		tvi.pszText = (LPTSTR)LPSTR_TEXTCALLBACK;
#endif // !FW_VERSION_1
	}
	tvi.hItem = hItem;
	tvi.state = nState;
	tvi.stateMask = nStateMask;
	tvi.iImage = iImage;
	tvi.iSelectedImage = iSelectedImage;
	return SuperClass::DefWndProc(TVM_SETITEM, 0, (LPARAM)&tvi);
}

/*----------------------------------------------------------------------------------------------
	This member function implements the behavior of the Win32 message TVM_SETITEMHEIGHT, as
	described in the Platform SDK.
----------------------------------------------------------------------------------------------*/
SHORT TssTreeView::SetItemHeight(SHORT cyHeight)
{
	return (SHORT)SuperClass::DefWndProc(TVM_SETITEMHEIGHT, (WPARAM)cyHeight, 0);
}

/*----------------------------------------------------------------------------------------------
	Sets the maximum scroll time for the tree view control. Returns the previous maximum scroll
	time, in milliseconds.
----------------------------------------------------------------------------------------------*/
UINT TssTreeView::SetScrollTime(UINT uScrollTime)
{
	return SuperClass::DefWndProc(TVM_SETSCROLLTIME, (WPARAM)uScrollTime, 0);
}

/*----------------------------------------------------------------------------------------------
	This member function implements the behavior of the Win32 messageTVM_SETTEXTCOLOR, as
	described in the Platform SDK.
----------------------------------------------------------------------------------------------*/
COLORREF TssTreeView::SetTextColor(COLORREF clr)
{
	return SuperClass::DefWndProc(TVM_SETTEXTCOLOR, 0, (LPARAM)clr);
}

/*----------------------------------------------------------------------------------------------
	Sets a tree view control's child tooltip control.
----------------------------------------------------------------------------------------------*/
HWND TssTreeView::SetToolTips(HWND hWndTooltip)
{
	return (HWND)SuperClass::DefWndProc(TVM_SETTOOLTIPS, (WPARAM)hWndTooltip, 0);
}

/*----------------------------------------------------------------------------------------------
	Sets the UNICODE character format flag for the control. This message allows you to change
	the character set used by the control at run time rather than having to re-create the
	control.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::SetUnicodeFormat(bool fUnicode)
{
	return SuperClass::DefWndProc(TVM_SETUNICODEFORMAT, (WPARAM)fUnicode, 0);
}

/*----------------------------------------------------------------------------------------------
	Call this function to sort the child items of the given parent item in a tree view control.
	SortChildren will not recurse through the tree; only the immediate children of hItem will
	be sorted.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::SortChildren(HTREEITEM hItem)
{
	return SuperClass::DefWndProc(TVM_SORTCHILDREN, 0, (LPARAM)hItem);
}

/*----------------------------------------------------------------------------------------------
	Call this function to sort tree view items using an application-defined callback function
	that compares the items.  The structure's comparison function, lpfnCompare, must return a
	negative value if the first item should precede the second, a positive value if the first
	item should follow the second, or zero if the two items are equivalent.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::SortChildrenCB(bool fRecurse, LPTVSORTCB psort)
{
	// MSDN help ms-help://MS.VSCC/MS.MSDNVS/shellcc/platform/CommCtls/TreeView/Macros/TreeView_SortChildrenCB.htm
	// says that fRecurse is reserved and must be zero. Windows does not currently implement
	// the recursion that we need, so if fRecurse is non-zero, we will intercept this and handle
	// the recursion ourselves:
	if (!fRecurse)
		return SuperClass::DefWndProc(TVM_SORTCHILDRENCB, (WPARAM)fRecurse, (LPARAM)psort);

	SuperClass::DefWndProc(TVM_SORTCHILDRENCB, 0, (LPARAM)psort);

	if (!psort->hParent)
		psort->hParent = TreeView_GetRoot(m_hwnd);
	else
		psort->hParent = TreeView_GetChild(m_hwnd, psort->hParent);

	if (!psort->hParent)
		return true;

	do
	{
		HTREEITEM htiChild = TreeView_GetChild(m_hwnd, psort->hParent);
		if (htiChild)
		{
			TVSORTCB sort;
			sort.hParent = psort->hParent;
			sort.lParam = psort->lParam;
			sort.lpfnCompare = psort->lpfnCompare;
			SortChildrenCB(true, &sort);
		}
	} while ((psort->hParent = TreeView_GetNextSibling(m_hwnd, psort->hParent)) != NULL);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Callback method used in sorting TreeView members when they come from Possibility lists.
----------------------------------------------------------------------------------------------*/
int CALLBACK TssTreeView::PossListCompareFunc(LPARAM lParam1, LPARAM lParam2, LPARAM lParamSort)
{
	FwTreeItem * pfti1 = (FwTreeItem *)lParam1;
	AssertPtr(pfti1);
	FwTreeItem * pfti2 = (FwTreeItem *)lParam2;
	AssertPtr(pfti2);
	PossListInfo * ppli = (PossListInfo *)lParamSort;
	AssertPtr(ppli);
	AfLpInfo * plpi = ppli->GetLpInfoPtr();
	AssertPtr(plpi);

	const OLECHAR * pwrgch1;
	const OLECHAR * pwrgch2;
	int cch1, cch2;
	HRESULT hr;
	IgnoreHr(hr = pfti1->qtss->LockText(&pwrgch1, &cch1));
	if (FAILED(hr))
		return 1;
	IgnoreHr(hr = pfti2->qtss->LockText(&pwrgch2, &cch2));
	if (FAILED(hr))
		return 1;

	SmartBstr sbstr1(pwrgch1);
	SmartBstr sbstr2(pwrgch2);

	// Get a collater, which will be able to compare names for us, even in foreign encodings:
	ILgWritingSystemFactoryPtr qwsf;
	ILgCollatingEnginePtr qcoleng;
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetLgWritingSystemFactory(&qwsf);
	int ws = plpi->ActualWs(ppli->GetWs());
	CheckHr(hr = qwsf->get_DefaultCollater(ws, &qcoleng));

	int nResult;
	CheckHr(hr = qcoleng->Compare(sbstr1, sbstr2, fcoDefault, &nResult));

	pfti1->qtss->UnlockText(pwrgch1);
	pfti2->qtss->UnlockText(pwrgch2);

	return nResult;
}


/*----------------------------------------------------------------------------------------------
	Make the root box.
----------------------------------------------------------------------------------------------*/
void TssTreeView::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
	IVwRootBox ** pprootb)
{
	AssertPtr(pvg);
	AssertPtrN(pwsf);
	AssertPtr(pprootb);

	*pprootb = NULL;

	if (!m_qwsf)
		SetWritingSystemFactory(pwsf);
	Assert(m_wsUser);

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this));
	HVO hvo = khvoItemText;
	int frag = kfrItemText;

	// Set up a new view constructor.
	m_qttvvc.Attach(NewObj TssTreeViewVc);

	ISilDataAccessPtr qsdaTemp;
	HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);
	if (pwsf)
		CheckHr(qsdaTemp->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qsdaTemp));

	ITsStringPtr qtss;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(L"Dummy String");
	CheckHr(qtsf->MakeString(stu.Bstr(), m_wsUser, &qtss));

	IVwViewConstructor * pvvc = m_qttvvc;
	// REVIEW: This could eventually be changed to SetRootString, but as of now,
	// it doesn't appear to be fully implemented yet.
	CheckHr(qrootb->SetRootObjects(&hvo, &pvvc, &frag, NULL, 1));
	//CheckHr(qrootb->SetRootString(qtss, NULL, pvvc, kfrItemText));
	*pprootb = qrootb.Detach();
}


/***********************************************************************************************
	Notification message handlers.
***********************************************************************************************/

bool TssTreeView::_OnCustomDraw(NMTVCUSTOMDRAW * pnmcd, long & lnRet)
{
	switch (pnmcd->nmcd.dwDrawStage)
	{
	case CDDS_PREPAINT:
		// Request pre-paint and post-paint notifications for each item.
		lnRet = CDRF_NOTIFYITEMDRAW | CDRF_NOTIFYPOSTPAINT;
		return true;

	case CDDS_ITEMPREPAINT:
		lnRet = CDRF_NOTIFYPOSTPAINT;

		{
			// This section is used to determine to create a string with the correct
			// color/underline attributes and select it into the cache.
			// It also measures the width of each item the first time it's drawn.

			m_clrDrawingBack = ::GetSysColor(COLOR_WINDOW);

			TVITEM tvi = { TVIF_PARAM, (HTREEITEM)pnmcd->nmcd.dwItemSpec};
			if (SuperClass::DefWndProc(TVM_GETITEM, 0, (LPARAM)&tvi))
			{
				FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
				if (pfti == NULL)
					return false;
				AssertPtr(pfti);

				// Figure out the foreground, background, and underline colors.
				ITsStrBldrPtr qtsb;
				CheckHr(pfti->qtss->GetBldr(&qtsb));
				int cch;
				CheckHr(qtsb->get_Length(&cch));
				COLORREF clrFore, clrBack, clrUnder;
				int unt = ((pnmcd->nmcd.uItemState & CDIS_HOT) == CDIS_HOT) ? kuntSingle : kuntNone;
				if ((pnmcd->nmcd.uItemState & CDIS_SELECTED) == CDIS_SELECTED)
				{
					clrFore = ::GetSysColor(COLOR_HIGHLIGHTTEXT);
					clrBack = ::GetSysColor(COLOR_HIGHLIGHT);
					if (unt == kuntSingle)
						clrUnder = ::GetSysColor(COLOR_HIGHLIGHTTEXT);
				}
				else
				{
					clrBack = ::GetSysColor(COLOR_WINDOW);
					if (unt != kuntSingle)
					{
						clrFore = ::GetSysColor(COLOR_WINDOWTEXT);
					}
					else
					{
						clrFore = ::GetSysColor(COLOR_HOTLIGHT);
						clrUnder = ::GetSysColor(COLOR_HOTLIGHT);
					}
				}
				qtsb->SetIntPropValues(0, cch, ktptUnderline, ktpvEnum, unt);
				qtsb->SetIntPropValues(0, cch, ktptForeColor, ktpvDefault, clrFore);
				qtsb->SetIntPropValues(0, cch, ktptBackColor, ktpvDefault, clrBack);
				if (unt != kuntNone)
					qtsb->SetIntPropValues(0, cch, ktptUnderColor, ktpvDefault, clrUnder);

				// Update the string in the cache and tell the rootbox that it's changed.
				ITsStringPtr qtss;
				CheckHr(qtsb->GetString(&qtss));
				m_qvcd->CacheStringProp(khvoItemText, kflidItemText, qtss);
				ISilDataAccessPtr qsda;
				CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda));
				qsda->PropChanged(m_qrootb, kpctNotifyAll, khvoItemText, kflidItemText, 0, 0, 0);

				// Save the background color for the post-paint notification.
				m_clrDrawingBack = clrBack;

				// Since we're not calling the SuperClass::OnSize in our OnSize method,
				// we need to set the layout width here.
				if (m_dxdLayoutWidth < 0)
					m_dxdLayoutWidth = 999999;
				// For some reason, this is necessary for long items or they get cut off.
				{
					HoldGraphics hg(this);
					CheckHr(m_qrootb->Layout(hg.m_qvg, m_dxdLayoutWidth));
				}

				if (pfti->dxp == 0)
				{
					// This is the first time this item is being drawn, so figure out the
					// width. This is the only way I could figure out how to get the actual
					// width of the string. Everything else I tried returned the layout width,
					// or the width of the window, neither of which is correct.
					IVwSelectionPtr qsel;
					RECT rc;
					CheckHr(m_qrootb->MakeSimpleSel(true, false, true, false, &qsel));
					CheckHr(qsel->GetParaLocation(&rc));
					pfti->dxp = rc.right - rc.left;

					// Adjust the height of each item if required.
					int dypLine = max(rc.bottom - rc.top, 16);
					if (::SendMessage(m_hwnd, TVM_GETITEMHEIGHT, 0, 0) != dypLine)
						::SendMessage(m_hwnd, TVM_SETITEMHEIGHT, dypLine, 0);
				}
			}
		}
		return true;

	case CDDS_ITEMPOSTPAINT:
		{
			// This section is used to actually paint the item over whatever the treeview
			// already painted for this item (currently a bunch of spaces).
			FW_TVITEM ftvi;
			ftvi.mask = TVIF_HANDLE | TVIF_TEXT;
			ftvi.hItem = (HTREEITEM )pnmcd->nmcd.dwItemSpec;
			if (TreeView_GetItem(m_hwnd, &ftvi))
			{
				// Fill the background with the correct color.
				POINT pt;
				RECT rcItem;
				TreeView_GetItemRect(m_hwnd, ftvi.hItem, &rcItem, true);
				AfGfx::FillSolidRect(pnmcd->nmcd.hdc, rcItem, m_clrDrawingBack);

				// This is required so we draw the item in the correct spot in the window.
				::SetWindowOrgEx(pnmcd->nmcd.hdc, -rcItem.left, -rcItem.top, &pt);
				Rect rcClip(rcItem);
				rcClip.Offset(-rcClip.left, -rcClip.top - 1);
				// Draw the item.
				Draw(pnmcd->nmcd.hdc, rcClip);
				// Reset the window origin.
				::SetWindowOrgEx(pnmcd->nmcd.hdc, pt.x, pt.y, NULL);
			}
		}
		return true;

	case CDDS_POSTPAINT:
		// We need to tell the view window that it doesn't need to paint anything else.
		::ValidateRect(m_hwnd, NULL);
		return true;
	}

	return false;
}

bool TssTreeView::_OnBeginDrag(NMTREEVIEW * pnmv, long & lnRet)
{
	AssertPtr(pnmv);
	FW_NMTREEVIEW fntv;
	fntv.hdr = pnmv->hdr;
	fntv.itemNew.hItem = pnmv->itemNew.hItem;
	fntv.itemNew.state = pnmv->itemNew.state;
	FwTreeItem * pfti = (FwTreeItem *)pnmv->itemNew.lParam;
	AssertPtr(pfti);
	fntv.itemNew.lParam = pfti->lParam;
	fntv.ptDrag = pnmv->ptDrag;
	fntv.hdr.code = FW_TVN_BEGINDRAG;
	return OnBeginDrag(&fntv, lnRet);
}

bool TssTreeView::_OnBeginLabelEdit(NMTVDISPINFO * pnmdi, long & lnRet)
{
	AssertPtr(pnmdi);
	FW_NMTVDISPINFO fndi;
	fndi.hdr = pnmdi->hdr;
	fndi.item.hItem = pnmdi->item.hItem;
	fndi.item.state = pnmdi->item.state;
	FwTreeItem * pfti = (FwTreeItem *)pnmdi->item.lParam;
	AssertPtr(pfti);
	fndi.item.lParam = pfti->lParam;
	fndi.item.qtss = pfti->qtss;
	// ENHANCE RandB: Uncomment this once we draw the strings ourselves.
	//Assert(!pnmdi->item.pszText);
	fndi.hdr.code = FW_TVN_BEGINLABELEDIT;
	return OnBeginLabelEdit(&fndi, lnRet);
}

bool TssTreeView::_OnBeginRDrag(NMTREEVIEW * pnmv, long & lnRet)
{
	AssertPtr(pnmv);
	FW_NMTREEVIEW fntv;
	fntv.hdr = pnmv->hdr;
	fntv.itemNew.hItem = pnmv->itemNew.hItem;
	fntv.itemNew.state = pnmv->itemNew.state;
	FwTreeItem * pfti = (FwTreeItem *)pnmv->itemNew.lParam;
	AssertPtr(pfti);
	fntv.itemNew.lParam = pfti->lParam;
	fntv.ptDrag = pnmv->ptDrag;
	fntv.hdr.code = FW_TVN_BEGINRDRAG;
	return OnBeginRDrag(&fntv, lnRet);
}

bool TssTreeView::_OnDeleteItem(NMTREEVIEW * pnmv, long & lnRet)
{
	AssertPtr(pnmv);
	FW_NMTREEVIEW fntv;
	fntv.hdr = pnmv->hdr;
	fntv.itemOld.hItem = pnmv->itemOld.hItem;
	FwTreeItem * pfti = (FwTreeItem *)pnmv->itemOld.lParam;
	AssertPtr(pfti);
	fntv.itemOld.lParam = pfti->lParam;
	delete pfti;
	fntv.hdr.code = FW_TVN_DELETEITEM;
	/*
	REVIEW: This formerly returned the value returned from OnDeleteItem, namely false,
	unless some subclass has been defined to return true. Unfortunately, when this method is
	called as part of a TreeView_DeleteAllItems macro, returning false results in a second
	TVN_DELETEITEM message being issued, even though the item is deleted here already, and
	thus it crashes.
	Here is Darrell's full explanation:
	1) TssTreeView::FWndProc gets a TVM_DELETEITEM message (from the
	TreeView_DeleteAllItems macro).
	2) This calls TssTreeView::DeleteItem, which calls the following method:
		SuperClass::DefWndProc(TVM_DELETEITEM, 0, (LPARAM)hItem);
	This method generates a TVN_DELETEITEM notification, which gets handled in
	TssTreeView::OnNotifyThis.
	3) TssTreeView::OnNotifyThis calls TssTreeView::_OnDeleteItem, which deletes
	the FwTreeItem object attached to the soon-to-be-deleted item.
	4) The TssTreeView::_OnDeleteItem function returns false, which usually
	means it hasn't done anything. Actually, technically, it calls the virtual
	OnDeleteItem function, which by default returns false.
	5) This false value eventually gets passed back to the following line in
	AfWnd::WndProc:
		fRet = qwnd->FWndProcPre(wm, wp, lp, lnRet);
	This means fRet is false, even though we did handle the message.
	6) The next line in AfWnd::WndProc calls FWndProc, which doesn't do anything
	we care about.
	7) The WndProcPost method is then called, which ends up calling
	AfWnd::DefWndProc, which calls the ::DefWindowProc API function. This in
	effect jumps back to step #2 and generates another TVN_DELETEITEM
	notification. This is why we get that notification twice for each item.
	*/
	OnDeleteItem(&fntv, lnRet);
	return true;
}

bool TssTreeView::_OnEndLabelEdit(NMTVDISPINFO * pnmdi, long & lnRet)
{
	AssertPtr(pnmdi);
	FW_NMTVDISPINFO fndi;
	fndi.hdr = pnmdi->hdr;
	fndi.item.hItem = pnmdi->item.hItem;
	FwTreeItem * pfti = (FwTreeItem *)pnmdi->item.lParam;
	AssertPtr(pfti);
	fndi.item.lParam = pfti->lParam;
	fndi.item.qtss = pfti->qtss;
//	Assert(!pnmdi->item.pszText);
	fndi.hdr.code = FW_TVN_ENDLABELEDIT;
	return OnEndLabelEdit(&fndi, lnRet);
}


/*----------------------------------------------------------------------------------------------
	This notification is sent for every item in the treeview when it needs to be drawn.
----------------------------------------------------------------------------------------------*/
bool TssTreeView::_OnGetDispInfo(NMTVDISPINFO * pnmdi, long & lnRet)
{
// TODO: This method does not do what is required i.e. fill in the data members according to the
// the type of information required, specified in mask, hItem, state, and lParam data members.
	AssertPtr(pnmdi);
	FW_NMTVDISPINFO fndi;
	fndi.hdr = pnmdi->hdr;
	fndi.item.hItem = pnmdi->item.hItem;
	fndi.item.state = pnmdi->item.state;
	fndi.item.mask = pnmdi->item.mask;
	FwTreeItem * pfti = (FwTreeItem *)pnmdi->item.lParam;
	AssertPtr(pfti);
	fndi.item.lParam = pfti->lParam;
	fndi.hdr.code = FW_TVN_GETDISPINFO;

	if (pnmdi->item.mask & TVIF_TEXT)
	{
		// NOTE: This section right here does not return the actual string that is
		// attached to this item. It instead returns a string of spaces whose width is
		// greater than or equal to the width of the string attached to the item.
		// This was the only thing I could think of to force the treeview to draw the items
		// in such a way that the horizontal scrollbar works correctly. This also causes
		// the treeview to send us a notification message when a tooltip needs to be displayed
		// (e.g. when the window is too small to show the entire string).

		// If you need to get access to the string, you can copy the following 5 lines.
		TVITEM tvi = { TVIF_PARAM, pnmdi->item.hItem};
		if (!SuperClass::DefWndProc(TVM_GETITEM, 0, (LPARAM)&tvi))
			return false;
		FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
		AssertPtr(pfti);

		if (m_dxpSpace == 0)
		{
			// This is only called once to figure out the width of a space in the
			// default GUI font (which is the font used by the tree view). This
			// also initializes the m_rgSpaces array.
			SIZE size;
			HDC hdc = ::GetDC(m_hwnd);
			HFONT hfontOld = (HFONT)::SelectObject(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
			::GetTextExtentPoint32(hdc, _T(" "), 1, &size);
			::SelectObject(hdc, hfontOld);
			::ReleaseDC(m_hwnd, hdc);
			m_dxpSpace = size.cx;
			wmemset(m_rgSpaces, ' ', sizeof(m_rgSpaces) / sizeof(achar) - 1);
			m_rgSpaces[sizeof(m_rgSpaces) / sizeof(achar) - 1] = 0;
		}
		int cch = (int)((double)pfti->dxp / m_dxpSpace + 1.5);
		if (cch > pnmdi->item.cchTextMax)
			cch = pnmdi->item.cchTextMax;
		// ENHANCE: This might cause problems with strings that are wider than 780 pixels.
		// 780 comes from 3 * 260
		//   3   = width of a space in the default GUI font
		//   260 = default value of pnmdi->item.cchTextMax
		// If this becomes a problem, one possible solution is to set pnmdi->item.pszText to
		// point to the array of spaces. If you do this, though, I believe you'll need to
		// have at least three arrays. Read the MSDN documentation on the NMTVDISPINFO structure
		// for more information:
		StrCpyN(pnmdi->item.pszText, m_rgSpaces, cch);
	}
	return true;
}

bool TssTreeView::_OnItemExpanded(NMTREEVIEW * pnmv, long & lnRet)
{
	AssertPtr(pnmv);
	FW_NMTREEVIEW fntv;
	fntv.hdr = pnmv->hdr;
	fntv.itemNew.hItem = pnmv->itemNew.hItem;
	fntv.itemNew.state = pnmv->itemNew.state;
	FwTreeItem * pfti = (FwTreeItem *)pnmv->itemNew.lParam;
	AssertPtr(pfti);
	fntv.itemNew.lParam = pfti->lParam;
	fntv.action = pnmv->action;
	fntv.hdr.code = FW_TVN_ITEMEXPANDED;
	return OnItemExpanded(&fntv, lnRet);
}

bool TssTreeView::_OnItemExpanding(NMTREEVIEW * pnmv, long & lnRet)
{
	AssertPtr(pnmv);
	FW_NMTREEVIEW fntv;
	fntv.hdr = pnmv->hdr;
	fntv.itemNew.hItem = pnmv->itemNew.hItem;
	fntv.itemNew.state = pnmv->itemNew.state;
	FwTreeItem * pfti = (FwTreeItem *)pnmv->itemNew.lParam;
	AssertPtr(pfti);
	fntv.itemNew.lParam = pfti->lParam;
	fntv.action = pnmv->action;
	fntv.hdr.code = FW_TVN_ITEMEXPANDING;
	return OnItemExpanding(&fntv, lnRet);
}

bool TssTreeView::_OnKeyDown(NMTVKEYDOWN * pnmkd, long & lnRet)
{
	AssertPtr(pnmkd);
	FW_NMTVKEYDOWN fnkd;
	fnkd.hdr = pnmkd->hdr;
	fnkd.wVKey = pnmkd->wVKey;
	fnkd.flags = pnmkd->flags;
	fnkd.hdr.code = FW_TVN_KEYDOWN;
	return OnKeyDown(&fnkd, lnRet);
}
bool TssTreeView::_OnSelChanged(NMTREEVIEW * pnmv, long & lnRet)
{
	AssertPtr(pnmv);
	FW_NMTREEVIEW fntv;
	fntv.hdr = pnmv->hdr;
	fntv.itemNew.hItem = pnmv->itemNew.hItem;
	if (pnmv->itemNew.hItem)
	{
		fntv.itemNew.mask = pnmv->itemNew.mask;
		fntv.itemNew.state = pnmv->itemNew.state;
		FwTreeItem * pfti = (FwTreeItem *)pnmv->itemNew.lParam;
		AssertPtr(pfti);
		fntv.itemNew.lParam = pfti->lParam;
	}

	fntv.itemOld.hItem = pnmv->itemOld.hItem;
	if (pnmv->itemOld.hItem)
	{
		fntv.itemOld.mask = pnmv->itemOld.mask;
		fntv.itemOld.state = pnmv->itemOld.state;
		FwTreeItem * pfti = (FwTreeItem *)pnmv->itemOld.lParam;
		AssertPtr(pfti);
		fntv.itemOld.lParam = pfti->lParam;
	}
	fntv.action = pnmv->action;
	fntv.ptDrag = pnmv->ptDrag;
	fntv.hdr.code = FW_TVN_SELCHANGED;
	return OnSelChanged(&fntv, lnRet);
}


bool TssTreeView::_OnSelChanging(NMTREEVIEW * pnmv, long & lnRet)
{
	AssertPtr(pnmv);
	FW_NMTREEVIEW fntv;
	fntv.hdr = pnmv->hdr;

	fntv.itemNew.hItem = pnmv->itemNew.hItem;
	if (pnmv->itemNew.hItem)
	{
		fntv.itemNew.mask = pnmv->itemNew.mask;
		fntv.itemNew.state = pnmv->itemNew.state;
		fntv.itemNew.stateMask = pnmv->itemNew.stateMask;
		fntv.itemNew.iImage = pnmv->itemNew.iImage;
		fntv.itemNew.iSelectedImage = pnmv->itemNew.iSelectedImage;
		fntv.itemNew.cChildren = pnmv->itemNew.cChildren;
		FwTreeItem * pfti = (FwTreeItem *)pnmv->itemNew.lParam;
		AssertPtr(pfti);
		fntv.itemNew.qtss = pfti->qtss;
		fntv.itemNew.lParam = pfti->lParam;
	}

	fntv.itemOld.hItem = pnmv->itemOld.hItem;
	if (pnmv->itemOld.hItem)
	{
		fntv.itemOld.mask = pnmv->itemOld.mask;
		fntv.itemOld.state = pnmv->itemOld.state;
		fntv.itemOld.stateMask = pnmv->itemOld.stateMask;
		fntv.itemOld.iImage = pnmv->itemOld.iImage;
		fntv.itemOld.iSelectedImage = pnmv->itemOld.iSelectedImage;
		fntv.itemOld.cChildren = pnmv->itemOld.cChildren;
		FwTreeItem * pfti = (FwTreeItem *)pnmv->itemOld.lParam;
		AssertPtr(pfti);
		fntv.itemOld.qtss = pfti->qtss;
		fntv.itemOld.lParam = pfti->lParam;
		fntv.action = pnmv->action;
		fntv.hdr.code = FW_TVN_SELCHANGING;
	}
	return OnSelChanging(&fntv, lnRet);
}

bool TssTreeView::_OnSetDispInfo(NMTREEVIEW * pnmv, long & lnRet)
{
	AssertPtr(pnmv);
	FW_NMTREEVIEW fntv;
	fntv.hdr = pnmv->hdr;
	fntv.itemNew.mask = pnmv->itemNew.mask;
	fntv.itemNew.hItem = pnmv->itemNew.hItem;
	fntv.itemNew.state = pnmv->itemNew.state;
	fntv.itemNew.stateMask = pnmv->itemNew.stateMask;
	fntv.itemNew.iImage = pnmv->itemNew.iImage;
	fntv.itemNew.iSelectedImage = pnmv->itemNew.iSelectedImage;
	fntv.itemNew.cChildren = pnmv->itemNew.cChildren;
	FwTreeItem * pfti = (FwTreeItem *)pnmv->itemNew.lParam;
	AssertPtr(pfti);
	fntv.itemNew.qtss = pfti->qtss;
	fntv.itemNew.lParam = pfti->lParam;

	fntv.itemOld.mask = pnmv->itemOld.mask;
	fntv.itemOld.hItem = pnmv->itemOld.hItem;
	fntv.itemOld.state = pnmv->itemOld.state;
	fntv.itemOld.stateMask = pnmv->itemOld.stateMask;
	fntv.itemOld.iImage = pnmv->itemOld.iImage;
	fntv.itemOld.iSelectedImage = pnmv->itemOld.iSelectedImage;
	fntv.itemOld.cChildren = pnmv->itemOld.cChildren;
	pfti = (FwTreeItem *)pnmv->itemOld.lParam;
	AssertPtr(pfti);
	fntv.itemOld.qtss = pfti->qtss;
	fntv.itemOld.lParam = pfti->lParam;
	fntv.action = pnmv->action;
	fntv.hdr.code = FW_TVN_SETDISPINFO;
	return OnSetDispInfo(&fntv, lnRet);
}

bool TssTreeView::_OnSingleExpand(NMTREEVIEW * pnmv, long & lnRet)
{
	AssertPtr(pnmv);
	FW_NMTREEVIEW fntv;
	fntv.hdr = pnmv->hdr;
	fntv.itemNew.mask = pnmv->itemNew.mask;
	fntv.itemNew.hItem = pnmv->itemNew.hItem;
	fntv.itemNew.state = pnmv->itemNew.state;
	fntv.itemNew.stateMask = pnmv->itemNew.stateMask;
	fntv.itemNew.iImage = pnmv->itemNew.iImage;
	fntv.itemNew.iSelectedImage = pnmv->itemNew.iSelectedImage;
	fntv.itemNew.cChildren = pnmv->itemNew.cChildren;
	FwTreeItem * pfti = (FwTreeItem *)pnmv->itemNew.lParam;
	AssertPtr(pfti);
	fntv.itemNew.qtss = pfti->qtss;
	fntv.itemNew.lParam = pfti->lParam;

	fntv.itemOld.mask = pnmv->itemOld.mask;
	fntv.itemOld.hItem = pnmv->itemOld.hItem;
	fntv.itemOld.state = pnmv->itemOld.state;
	fntv.itemOld.stateMask = pnmv->itemOld.stateMask;
	fntv.itemOld.iImage = pnmv->itemOld.iImage;
	fntv.itemOld.iSelectedImage = pnmv->itemOld.iSelectedImage;
	fntv.itemOld.cChildren = pnmv->itemOld.cChildren;
	pfti = (FwTreeItem *)pnmv->itemOld.lParam;
	AssertPtr(pfti);
	fntv.itemOld.qtss = pfti->qtss;
	fntv.itemOld.lParam = pfti->lParam;
	fntv.action = pnmv->action;
	fntv.ptDrag = pnmv->ptDrag;
	fntv.hdr.code = FW_TVN_SINGLEEXPAND;
	return OnSingleExpand(&fntv, lnRet);
}
void TssTreeView::_CopyItem(const TVITEM & tvi, FW_TVITEM & ftvi)
{
	int mask = tvi.mask;

	ftvi.mask = mask;
	ftvi.hItem = tvi.hItem;

	if (mask & TVIF_CHILDREN)
		ftvi.cChildren = tvi.cChildren;

	if (mask & TVIF_IMAGE)
		ftvi.iImage = tvi.iImage;

	if (mask & TVIF_PARAM)
		ftvi.lParam = tvi.lParam;

	if (mask & TVIF_SELECTEDIMAGE)
		ftvi.iSelectedImage = tvi.iSelectedImage;

	if (mask & TVIF_STATE)
	{
		ftvi.state = tvi.state;
		ftvi.stateMask = tvi.stateMask;
	}

	if (mask & TVIF_TEXT)
	{
		FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
		if (pfti)
		{
			AssertPtr(pfti);
			ftvi.qtss = pfti->qtss;
		}
		else
		{
			Assert(tvi.pszText != LPSTR_TEXTCALLBACK);
			ITsStringPtr qtss;
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);

			StrUni stu;
			stu = tvi.pszText;
			Assert(m_wsUser);
			qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &qtss);
			ftvi.qtss = qtss;
		}
	}
}


void TssTreeView::_CopyItem(const FW_TVITEM & ftvi, TVITEM & tvi)
{
	int mask = ftvi.mask;

	tvi.mask = mask;
	tvi.hItem = ftvi.hItem;

	if (mask & TVIF_CHILDREN)
		tvi.cChildren = ftvi.cChildren;

	if (mask & TVIF_IMAGE)
		tvi.iImage = ftvi.iImage;

	if (mask & TVIF_PARAM)
		tvi.lParam = ftvi.lParam;

	if (mask & TVIF_SELECTEDIMAGE)
		tvi.iSelectedImage = ftvi.iSelectedImage;

	if (mask & TVIF_STATE)
	{
		tvi.state = ftvi.state;
		tvi.stateMask = ftvi.stateMask;
	}

	if (mask & LVIF_TEXT)
	{
		AssertArray(tvi.pszText, tvi.cchTextMax);

		const OLECHAR * prgch;
		int cch;
		if (ftvi.qtss && SUCCEEDED(ftvi.qtss->LockText(&prgch, &cch)))
		{
			StrApp str;
			try
			{
				str.Assign(prgch, cch);
			}
			catch (...)
			{
			}
			ftvi.qtss->UnlockText(prgch);
			_tcsncpy_s(tvi.pszText, tvi.cchTextMax, str.Chars(), tvi.cchTextMax);
		}
	}
}


/***********************************************************************************************
	TssTreeViewVc methods.
***********************************************************************************************/

static DummyFactory g_fact(_T("SIL.AppCore.TssTreeViewVc"));

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.
	The treeview item only consists of a single TsString.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TssTreeViewVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);
	Assert(frag == TssTreeView::kfrItemText);

	CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));

	// Everything is set up now... Add the string.
	CheckHr(pvwenv->OpenParagraph());
	CheckHr(pvwenv->OpenInnerPile());
	CheckHr(pvwenv->OpenParagraph());
	CheckHr(pvwenv->AddStringProp(TssTreeView::kflidItemText, this));
	CheckHr(pvwenv->CloseParagraph());
	CheckHr(pvwenv->CloseInnerPile());
	CheckHr(pvwenv->CloseParagraph());

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}

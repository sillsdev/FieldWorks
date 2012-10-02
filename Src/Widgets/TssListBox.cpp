/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TssListBox.cpp
Responsibility: Darrell Zook
Last reviewed:

	Implementation of TssListBox.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

const int ktmrResetSearch = 1;

/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
TssListBox::TssListBox()
{
	m_cchLookAhead = 0;
}


/*----------------------------------------------------------------------------------------------
// This function must be called before the ListBox control has been initialized with any data.
// (i.e. before any items are inserted into it.)
----------------------------------------------------------------------------------------------*/
void TssListBox::SubclassListBox(HWND hwnd)
{
	Assert(::SendMessage(hwnd, LB_GETCOUNT, 0, 0) == 0);

	// We have to create a new window because the owner-draw style cannot be turned on for a
	// window that has already been created.
	DWORD style = ::GetWindowLong(hwnd, GWL_STYLE);
	style &= ~LBS_HASSTRINGS;
	style |= LBS_OWNERDRAWVARIABLE | LBS_NOTIFY | WS_VSCROLL;
	DWORD styleEx = ::GetWindowLong(hwnd, GWL_EXSTYLE);
	Rect rc;
	::GetWindowRect(hwnd, &rc);
	HWND hwndPar = ::GetParent(hwnd);
	::MapWindowPoints(NULL, hwndPar, (POINT *)&rc, 2);

	WndCreateStruct wcs;
	wcs.InitChild(_T("LISTBOX"), hwndPar, ::GetDlgCtrlID(hwnd));
	wcs.SetRect(rc);
	wcs.style = style;
	wcs.dwExStyle = styleEx;
	AfWnd::CreateAndSubclassHwnd(wcs);

	::DestroyWindow(hwnd);
}

void TssListBox::PreCreateHwnd(CREATESTRUCT & cs)
{
	cs.style &= ~LBS_HASSTRINGS;
	cs.style |= LBS_OWNERDRAWVARIABLE | LBS_NOTIFY | WS_VSCROLL | WS_CHILD;
}

bool TssListBox::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case LB_ADDSTRING:
	case LB_INSERTSTRING:
	case LB_GETTEXT:
	case LB_SELECTSTRING:
	case LB_DIR:
	case LB_FINDSTRING:
	case LB_ADDFILE:
	case LB_SETITEMHEIGHT:
	case LB_FINDSTRINGEXACT:
	case LB_SETLOCALE:
	case LB_GETLOCALE:
	case LB_SETCOUNT:
		// We don't support these methods. In most cases, there are appropriate
		// TsString versions available.
		Assert(false);
		lnRet = LB_ERR;
		return true;

	case LB_SELITEMRANGEEX:
	case LB_SELITEMRANGE:
		return false; // Forward these to the default handler.

	case FW_LB_ADDSTRING:
		lnRet = AddString((ITsString *)lp);
		return true;

	case FW_LB_INSERTSTRING:
		lnRet = InsertString(wp, (ITsString *)lp);
		return true;

	case FW_LB_GETTEXT:
		lnRet = GetText(wp, (ITsString **)lp);
		return true;

	case FW_LB_SELECTSTRING:
		lnRet = SelectString(wp, (ITsString *)lp);
		return true;

	case FW_LB_FINDSTRING:
		lnRet = FindString(wp, (ITsString *)lp);
		return true;

	case FW_LB_FINDSTRINGEXACT:
		lnRet = FindStringExact(wp, (ITsString *)lp);
		return true;

	case LB_DELETESTRING:
		lnRet = DeleteString(wp);
		return true;

	case LB_RESETCONTENT:
		ResetContent();
		return true;

	case LB_SETSEL:
		lnRet = SetSel(lp, wp);
		return true;

	case LB_SETCURSEL:
		lnRet = SetCurSel(wp);
		return true;

	case LB_GETSEL:
		lnRet = GetSel(wp);
		return true;

	case LB_GETCURSEL:
		lnRet = GetCurSel();
		return true;

	case LB_GETTEXTLEN:
		lnRet = GetTextLen(wp);
		return true;

	case LB_GETCOUNT:
		lnRet = GetCount();
		return true;

	case LB_GETTOPINDEX:
		lnRet = GetTopIndex();
		return true;

	case LB_GETSELCOUNT:
		lnRet = GetSelCount();
		return true;

	case LB_GETSELITEMS:
		lnRet = GetSelItems(wp, (int *)lp);
		return true;

	case LB_SETTABSTOPS:
		lnRet = SetTabStops(wp, (int *)lp);
		return true;

	case LB_SETCOLUMNWIDTH:
		SetColumnWidth(wp);
		return true;

	case LB_SETTOPINDEX:
		lnRet = SetTopIndex(wp);
		return true;

	case LB_GETITEMRECT:
		lnRet = GetItemRect(wp, (RECT *)lp);
		return true;

	case LB_GETITEMDATA:
		lnRet = GetItemData(wp);
		return true;

	case LB_SETITEMDATA:
		lnRet = SetItemData(wp, lp);
		return true;

	case LB_SETANCHORINDEX:
		SetAnchorIndex(wp);
		return true;

	case LB_GETANCHORINDEX:
		lnRet = GetAnchorIndex();
		return true;

	case LB_SETCARETINDEX:
		lnRet = SetCaretIndex(wp, lp);
		return true;

	case LB_GETCARETINDEX:
		lnRet = GetCaretIndex();
		return true;

	case LB_GETITEMHEIGHT:
		lnRet = GetItemHeight(wp);
		return true;

	case LB_INITSTORAGE:
		lnRet = InitStorage(wp, lp);
		return true;

	case LB_ITEMFROMPOINT:
		lnRet = ItemFromPoint(Point(LOWORD(lp), HIWORD(lp)));
		return true;

	case WM_CHAR:
		lnRet = OnChar(wp);
		return true;

	case WM_TIMER:
		lnRet = OnTimer(wp);
		return true;

	default:
		return false;
	}
}

/***********************************************************************************************
	MFC methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Retrieves the number of items in the list box.
----------------------------------------------------------------------------------------------*/
int TssListBox::GetCount()
{
	return AfWnd::DefWndProc(LB_GETCOUNT, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the index of the first visible item in a list box. Initially the item with
	index 0 is at the top of the list box, but if the list box contents have been scrolled
	another item may be at the top.
----------------------------------------------------------------------------------------------*/
int TssListBox::GetTopIndex()
{
	return AfWnd::DefWndProc(LB_GETTOPINDEX, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	The system scrolls the list box contents so that either the specified item appears at the
	top of the list box or the maximum scroll range has been reached.
----------------------------------------------------------------------------------------------*/
int TssListBox::SetTopIndex(int iItem)
{
	return AfWnd::DefWndProc(LB_SETTOPINDEX, (WPARAM)iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	Allocate space for items. Since we are storing pointers to TsStrings, this simply allocates
	enough vector space for cItems. cBytes is ignored, since the actual TsStrings are stored
	elsewhere.
----------------------------------------------------------------------------------------------*/
int TssListBox::InitStorage(int cItems, uint cBytes)
{
	try
	{
		m_vItems.EnsureSpace(cItems);
	}
	catch (...)
	{
		return LB_ERRSPACE;
	}
	return cItems;
}


/*----------------------------------------------------------------------------------------------
	Retrieves the zero-based index of the item nearest the specified point in a list box.
	The return value contains the index of the nearest item in the low-order word. The
	high-order word is zero if the specified point is in the client area of the list box,
	or one if it is outside the client area.
----------------------------------------------------------------------------------------------*/
uint TssListBox::ItemFromPoint(POINT pt)
{
	return AfWnd::DefWndProc(LB_ITEMFROMPOINT, 0, MAKELPARAM(pt.x, pt.y));
}


/*----------------------------------------------------------------------------------------------
	Retrieves the index of the currently selected item, if any, in a single-selection list box.
----------------------------------------------------------------------------------------------*/
int TssListBox::GetCurSel()
{
	return AfWnd::DefWndProc(LB_GETCURSEL, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Selects a string and scroll it into view, if necessary. When the new string is selected,
	the list box removes the highlight from the previously selected string.
----------------------------------------------------------------------------------------------*/
int TssListBox::SetCurSel(int iSelect)
{
	DWORD dwStyle = ::GetWindowLong(m_hwnd, GWL_STYLE);
	if (dwStyle & LBS_NOSEL)
		return LB_ERR;
	else if (dwStyle & LBS_EXTENDEDSEL || dwStyle & LBS_MULTIPLESEL)
	{
		SetSel(-1, false);
		return SetSel(iSelect);
	}
	else
		return AfWnd::DefWndProc(LB_SETCURSEL, iSelect, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the selection state of an item. If an item is selected, the return value is
	greater than zero; otherwise, it is zero. If an error occurs, the return value is LB_ERR.
----------------------------------------------------------------------------------------------*/
int TssListBox::GetSel(int iItem) // also works for single-selection
{
	return AfWnd::DefWndProc(LB_GETSEL, iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	Selects a string in a multiple-selection list box. If the fSelect is true, the string is
	selected and highlighted; if fSelect is false, the highlight is removed and the string is
	no longer selected. iItem specifies the zero-based index of the string to set. If index is
	-1, the selection is added to or removed from all strings, depending on the value of
	fSelect.
----------------------------------------------------------------------------------------------*/
int TssListBox::SetSel(int iItem, bool fSelect)
{
	return AfWnd::DefWndProc(LB_SETSEL, (WPARAM)fSelect, iItem);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
int TssListBox::GetSelCount()
{
	return AfWnd::DefWndProc(LB_GETSELCOUNT, 0, 0);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
int TssListBox::GetSelItems(int cItems, int * prgnIndex)
{
	AssertArray(prgnIndex, cItems);

	return AfWnd::DefWndProc(LB_GETSELITEMS, cItems, (LPARAM)prgnIndex);
}


/*----------------------------------------------------------------------------------------------
	Sets the anchor item--that is, the item from which a multiple selection starts. A multiple
	selection spans all items from the anchor item to the caret item.
----------------------------------------------------------------------------------------------*/
void TssListBox::SetAnchorIndex(int iItem)
{
	Assert((uint)iItem < (uint)m_vItems.Size());

	AfWnd::DefWndProc(LB_SETANCHORINDEX, iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the index of the anchor item--that is, the item from which a multiple selection
	starts. A multiple selection spans all items from the anchor item to the caret item.
----------------------------------------------------------------------------------------------*/
int TssListBox::GetAnchorIndex()
{
	return AfWnd::DefWndProc(LB_GETITEMDATA, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the application-defined value associated with the specified list box item.
----------------------------------------------------------------------------------------------*/
DWORD TssListBox::GetItemData(int iItem)
{
	Assert((uint)iItem < (uint)m_vItems.Size());

	return AfWnd::DefWndProc(LB_GETITEMDATA, iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	Sets a value associated with the specified item in a list box.
----------------------------------------------------------------------------------------------*/
int TssListBox::SetItemData(int iItem, DWORD dwItemData)
{
	Assert((uint)iItem < (uint)m_vItems.Size());

	return AfWnd::DefWndProc(LB_GETITEMDATA, iItem, dwItemData);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the dimensions of the rectangle that bounds a list box item as it is currently
	displayed in the list box.
----------------------------------------------------------------------------------------------*/
int TssListBox::GetItemRect(int iItem, RECT * lpRect)
{
	return AfWnd::DefWndProc(LB_GETITEMRECT, iItem, (LPARAM)lpRect);
}


/*----------------------------------------------------------------------------------------------
	Get the text for the specified item. Return the length, or LB_ERR if something went wrong.
----------------------------------------------------------------------------------------------*/
int TssListBox::GetText(int iItem, ITsString ** pptss)
{
	AssertPtr(pptss);

	*pptss = NULL;
	if ((uint)iItem >= (uint)m_vItems.Size())
	{
		Assert(false);
		return LB_ERR;
	}

	int cch;
	ITsStringPtr qtss = m_vItems[iItem];
	if (!qtss)
		return LB_ERR;
	HRESULT hr;
	IgnoreHr(hr = qtss->get_Length(&cch));
	if (FAILED(hr))
		return LB_ERR;
	*pptss = qtss.Detach();
	return cch;
}


/*----------------------------------------------------------------------------------------------
	Retrieves the length (characters) of a string in a list box.
----------------------------------------------------------------------------------------------*/
int TssListBox::GetTextLen(int iItem)
{
	if ((uint)iItem >= (uint)m_vItems.Size())
	{
		Assert(false);
		return LB_ERR;
	}

	int cch;
	HRESULT hr;
	IgnoreHr(hr = m_vItems[iItem]->get_Length(&cch));
	if (FAILED(hr))
		return LB_ERR;
	return cch;
}


/*----------------------------------------------------------------------------------------------
	Sets the width, in pixels, of all columns in the multiple comlumn list box.
----------------------------------------------------------------------------------------------*/
void TssListBox::SetColumnWidth(int cxWidth)
{
	AfWnd::DefWndProc(LB_SETCOLUMNWIDTH, cxWidth, 0);
}


/*----------------------------------------------------------------------------------------------
	Sets the tab-stop positions in a list box. nTabStops is the number of tab stops. rgTabStops
	is a pointer to the first member of an array of integers containing the tab stops. The
	integers represent the number of quarters of the average character width for the font that
	is selected into the list box. For example, a tab stop of 4 is placed at 1.0 character
	units, and a tab stop of 6 is placed at 1.5 average character units. However, if the list
	box is part of a dialog box, the integers are in dialog template units. The tab stops must
	be sorted in ascending order; backward tabs are not allowed.
----------------------------------------------------------------------------------------------*/
bool TssListBox::SetTabStops(int cTabStops, int * prgnTabStops)
{
	AssertArray(prgnTabStops, cTabStops);

	return AfWnd::DefWndProc(LB_SETTABSTOPS, cTabStops, (LPARAM)prgnTabStops);
}


/*----------------------------------------------------------------------------------------------
	Sets tab stops to the default size of 32 dialog units.
----------------------------------------------------------------------------------------------*/
void TssListBox::SetTabStops()
{
	AfWnd::DefWndProc(LB_SETTABSTOPS, 0, NULL);
}


/*----------------------------------------------------------------------------------------------
	Specifies that tab stops are to be set at every cxEachStop dialog units.
----------------------------------------------------------------------------------------------*/
bool TssListBox::SetTabStops(const int & duEachStop) // takes an 'int'
{
	return AfWnd::DefWndProc(LB_SETTABSTOPS, 1, duEachStop);
}


/*----------------------------------------------------------------------------------------------
	Retrieves the height of items in a list box.
----------------------------------------------------------------------------------------------*/
int TssListBox::GetItemHeight(int iItem)
{
	Assert((uint)iItem < (uint)m_vItems.Size());

	return AfWnd::DefWndProc(LB_GETITEMHEIGHT, iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	Determines the index of the item that has the focus rectangle in a multiple-selection
	list box. The item may or may not be selected.
----------------------------------------------------------------------------------------------*/
int TssListBox::GetCaretIndex()
{
	return AfWnd::DefWndProc(LB_GETCARETINDEX, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Sets the focus rectangle to the item at the specified index in a multiple-selection list
	box. If the item is not visible, it is scrolled into view.
----------------------------------------------------------------------------------------------*/
int TssListBox::SetCaretIndex(int iItem, bool fScroll)
{
	Assert((uint)iItem < (uint)m_vItems.Size());

	return AfWnd::DefWndProc(LB_SETCARETINDEX, iItem, fScroll);
}


/*----------------------------------------------------------------------------------------------
	Adds a string to a list box. If the list box does not have the LBS_SORT style, the string
	is added to the end of the list. Otherwise, the string is inserted into the list and the
	list is sorted. The return value is the zero-based index of the string in the list box.
	If an error occurs, the return value is LB_ERR. If there is insufficient space to store
	the new string, the return value is LB_ERRSPACE.
----------------------------------------------------------------------------------------------*/
int TssListBox::AddString(ITsString * ptss)
{
	AssertPtr(ptss);

	int iNewItem = m_vItems.Size();
	if (::GetWindowLong(m_hwnd, GWL_STYLE) & LBS_SORT)
	{
		// Find out where the string should be inserted.
		const OLECHAR * prgwch;
		const OLECHAR * prgwchT;
		int cch;
		int cchT;
		int ivMin;
		int ivLim;
		HRESULT hr;
		IgnoreHr(hr = ptss->LockText(&prgwch, &cch));
		if (FAILED(hr))
			return LB_ERR;

		for (ivMin = 0, ivLim = m_vItems.Size(); ivMin < ivLim; )
		{
			int ivMid = (ivMin + ivLim) / 2;
			IgnoreHr(hr = m_vItems[ivMid]->LockText(&prgwchT, &cchT));
			if (FAILED(hr))
			{
				ptss->UnlockText(prgwch);
				return 0;
			}
			if (wcscmp(prgwch, prgwchT) > 0)
				ivMin = ivMid + 1;
			else
				ivLim = ivMid;
			m_vItems[ivMid]->UnlockText(prgwchT);
		}
		Assert(ivMin <= m_vItems.Size());

		ptss->UnlockText(prgwch);
		iNewItem = ivMin;
	}

	try
	{
		m_vItems.Insert(iNewItem, ptss);
	}
	catch (...)
	{
		return LB_ERRSPACE;
	}

	// Insert the empty item at the end of the list (probably the fastest place to insert it).
	return AfWnd::DefWndProc(LB_INSERTSTRING, m_vItems.Size() - 1, 0);
}


/*----------------------------------------------------------------------------------------------
	Deletes a string (at index iItem) from the listbox. The return value is a count of the
	strings remaining in the list. The return value is LB_ERR if the index parameter specifies
	an index greater than the number of items in the list.
----------------------------------------------------------------------------------------------*/
int TssListBox::DeleteString(uint iItem)
{
	Assert(iItem < (uint)m_vItems.Size());

	if (iItem >= (uint)m_vItems.Size())
		return LB_ERR;

	m_vItems.Delete(iItem, iItem + 1);

	// Delete the empty item at the end of the list (probably the fastest place to delete it).
	return AfWnd::DefWndProc(LB_DELETESTRING, m_vItems.Size() - 1, 0);
}


/*----------------------------------------------------------------------------------------------
	Inserts a string into a list box at location iItem. Unlike AddString, this does not cause
	a list with the LBS_SORT style to be sorted.
----------------------------------------------------------------------------------------------*/
int TssListBox::InsertString(int iItem, ITsString * ptss)
{
	AssertPtr(ptss);
	Assert((uint)iItem <= (uint)m_vItems.Size());
	// If the list is sorted, call AddString, not InsertString.
	Assert(!(::GetWindowLong(m_hwnd, GWL_STYLE) & LBS_SORT));

	AfWnd::DefWndProc(LB_INSERTSTRING, iItem, 0);
	try
	{
		m_vItems.Insert(iItem, ptss);
	}
	catch (...)
	{
		return LB_ERR;
	}
	return iItem;
}


/*----------------------------------------------------------------------------------------------
	Removes all items from a list box.
----------------------------------------------------------------------------------------------*/
void TssListBox::ResetContent()
{
	m_vItems.Clear();
	AfWnd::DefWndProc(LB_RESETCONTENT, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Finds the first string in a list box that contains the specified prefix without changing
	the list-box selection.
----------------------------------------------------------------------------------------------*/
int TssListBox::FindString(int iStartAfter, ITsString * ptss)
{
	AssertPtr(ptss);
	Assert(iStartAfter == -1 || (uint)iStartAfter < (uint)m_vItems.Size());

	int cItems = m_vItems.Size();
	const OLECHAR * prgwch1;
	const OLECHAR * prgwch2;
	int cch1;
	int cch2;
	HRESULT hr;
	IgnoreHr(hr = ptss->LockText(&prgwch1, &cch1));
	if (FAILED(hr))
		return LB_ERR;

	for (int iItem = iStartAfter + 1; iItem < cItems; iItem++)
	{
		// ENHANCE: Use a comparison operator between two TsStrings when there is one.
		// For now, just use the characters.
		IgnoreHr(hr = m_vItems[iItem]->LockText(&prgwch2, &cch2));
		if (FAILED(hr))
		{
			ptss->UnlockText(prgwch1);
			return LB_ERR;
		}
		if (cch1 <= cch2 &&
			_wcsnicmp(prgwch1, prgwch2, cch1) == 0) // case doesn't matter
		{
			ptss->UnlockText(prgwch1);
			m_vItems[iItem]->UnlockText(prgwch2);
			return iItem;
		}
		m_vItems[iItem]->UnlockText(prgwch2);
	}
	for (int iItem = 0; iItem <= iStartAfter; iItem++)
	{
		// ENHANCE: Use a comparison operator between two TsStrings when there is one.
		// For now, just use the characters.
		IgnoreHr(hr = m_vItems[iItem]->LockText(&prgwch2, &cch2));
		if (FAILED(hr))
		{
			ptss->UnlockText(prgwch1);
			return LB_ERR;
		}
		if (cch1 <= cch2 &&
			_wcsnicmp(prgwch1, prgwch2, cch1) == 0) // case doesn't matter
		{
			ptss->UnlockText(prgwch1);
			m_vItems[iItem]->UnlockText(prgwch2);
			return iItem;
		}
		m_vItems[iItem]->UnlockText(prgwch2);
	}
	ptss->UnlockText(prgwch1);
	return LB_ERR;
}


/*----------------------------------------------------------------------------------------------
	Finds the first list box string that matches the specified string.
----------------------------------------------------------------------------------------------*/
int TssListBox::FindStringExact(int iStartAfter, ITsString * ptss)
{
	AssertPtr(ptss);
	Assert(iStartAfter == -1 || (uint)iStartAfter < (uint)m_vItems.Size());

	int cItems = m_vItems.Size();
	const OLECHAR * prgwch1;
	const OLECHAR * prgwch2;
	int cch1;
	int cch2;
	HRESULT hr;
	IgnoreHr(hr = ptss->LockText(&prgwch1, &cch1));
	if (FAILED(hr))
		return LB_ERR;

	for (int iItem = iStartAfter + 1; iItem < cItems; iItem++)
	{
		// ENHANCE: Use a comparison operator between two TsStrings when there is one.
		// For now, just use the characters.
		IgnoreHr(hr = m_vItems[iItem]->LockText(&prgwch2, &cch2));
		if (FAILED(hr))
		{
			ptss->UnlockText(prgwch1);
			return LB_ERR;
		}
		if (cch1 == cch2 &&
			_wcsicmp(prgwch1, prgwch2) == 0) // case doesn't matter
		{
			ptss->UnlockText(prgwch1);
			m_vItems[iItem]->UnlockText(prgwch2);
			return iItem;
		}
		m_vItems[iItem]->UnlockText(prgwch2);
	}
	for (int iItem = 0; iItem <= iStartAfter; iItem++)
	{
		// ENHANCE: Use a comparison operator between two TsStrings when there is one.
		// For now, just use the characters.
		IgnoreHr(hr = m_vItems[iItem]->LockText(&prgwch2, &cch2));
		if (FAILED(hr))
		{
			ptss->UnlockText(prgwch1);
			return LB_ERR;
		}
		if (cch1 == cch2 &&
			_wcsicmp(prgwch1, prgwch2) == 0) // case doesn't matter
		{
			ptss->UnlockText(prgwch1);
			m_vItems[iItem]->UnlockText(prgwch2);
			return iItem;
		}
		m_vItems[iItem]->UnlockText(prgwch2);
	}
	ptss->UnlockText(prgwch1);
	return LB_ERR;
}


/*----------------------------------------------------------------------------------------------
	Searches a list box for an item that begins with the characters in a specified string.
	If a matching item is found, the item is selected.
----------------------------------------------------------------------------------------------*/
int TssListBox::SelectString(int iStartAfter, ITsString * ptss)
{
	AssertPtr(ptss);
	Assert(iStartAfter == -1 || (uint)iStartAfter < (uint)m_vItems.Size());

	if (::GetWindowLong(m_hwnd, GWL_STYLE) & LBS_NOSEL)
		return LB_ERR;

	int iItem = FindString(iStartAfter, ptss);
	if (iItem != LB_ERR)
		SetCurSel(iItem);
	return iItem;
}


/*----------------------------------------------------------------------------------------------
	If bSelect is TRUE, the range of strings are selected and highlighted; if FALSE, the
	highlight is removed and the strings are no longer selected.
----------------------------------------------------------------------------------------------*/
int TssListBox::SelItemRange(bool fSelect, int iFirstItem, int iLastItem)
{
	if (fSelect)
		return AfWnd::DefWndProc(LB_SELITEMRANGEEX, iFirstItem, iLastItem);
	else
		return AfWnd::DefWndProc(LB_SELITEMRANGEEX, iLastItem, iFirstItem);
}


/***********************************************************************************************
	End of MFC
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
int TssListBox::OnChar(int ch)
{
	// TODO: What should the behavior of this be?
	// Currently, it just searches based on the character that is typed. If the
	// currently selected item starts with the typed character, it finds the next
	// item (wrapping around to the top if necessary) that starts with the character.

	ITsStrFactoryPtr qtsf;
	try
	{
		qtsf.CreateInstance(CLSID_TsStrFactory);
	}
	catch (...)
	{
		return -1;
	}

	/*if (m_cchLookAhead >= 10) // Too many characters have been typed.
		return -1;

	// Come up with a new TsString to search for.
	m_rgchLookAhead[m_cchLookAhead++] = (OLECHAR)wParam;*/
	m_rgchLookAhead[0] = (OLECHAR)ch;

	ITsStringPtr qtss;
	// TODO DarrellZ: What is the proper writing system for this?
	int ws = MainWindow()->UserWs();	// MakeStringRgch will crash and burn!!
	Assert(ws);
	HRESULT hr;
	IgnoreHr(hr = qtsf->MakeStringRgch(m_rgchLookAhead, 1, ws, &qtss));
	if (FAILED(hr))
	{
		//m_cchLookAhead--;
		return -1;
	}

	/*if (m_cchLookAhead > 1)
		KillTimer(m_hwnd, ktmrResetSearch);
	SetTimer(m_hwnd, ktmrResetSearch, 500, NULL);*/
	//int iItem = FindString(min(GetCurSel(), 0) - 1, qtss);
	int iSel;
	iSel = GetCurSel();
	int iItem = FindString(max(GetCurSel(), 0), qtss);
	if (iItem == LB_ERR)
	{
		/*m_cchLookAhead = 0;
		KillTimer(m_hwnd, ktmrResetSearch);*/
		return -1;
	}
	SetCurSel(iItem);
	// Send notification to parent window that the selection changed.
	::PostMessage(::GetParent(m_hwnd), WM_COMMAND, MAKEWPARAM(::GetDlgCtrlID(m_hwnd),
		LBN_SELCHANGE), (LPARAM)m_hwnd);
	return -1;
}

int TssListBox::OnTimer(UINT nIDEvent)
{
	if (nIDEvent == ktmrResetSearch)
	{
		::KillTimer(m_hwnd, nIDEvent);
		m_cchLookAhead = 0;
		return 1;
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssListBox::OnDrawThisItem(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);
	// ENHANCE: Change this to use the view subsystem.

	if (pdis->itemID == -1)
	{
		// This is the "default" height of an empty item.
		pdis->rcItem.bottom = 13;
		if (pdis->itemState & ODS_FOCUS)
			::DrawFocusRect(pdis->hDC, &pdis->rcItem);
		else
			::ExtTextOut(pdis->hDC, 0, 0, ETO_OPAQUE, &pdis->rcItem, NULL, 0, NULL);
	}
	else
	{
		if ((uint)pdis->itemID >= (uint)m_vItems.Size())
		{
			Assert(false);
			return false;
		}

		COLORREF clrOldText = ::GetTextColor(pdis->hDC);
		COLORREF clrOldBack = ::GetBkColor(pdis->hDC);
		if (pdis->itemState & ODS_SELECTED)
		{
			::SetTextColor(pdis->hDC, GetSysColor(COLOR_HIGHLIGHTTEXT));
			::SetBkColor(pdis->hDC, GetSysColor(COLOR_HIGHLIGHT));
		}

		const OLECHAR * prgwch;
		int cch;
		HRESULT hr;
		IgnoreHr(hr = m_vItems[pdis->itemID]->LockText(&prgwch, &cch));
		if (FAILED(hr))
			return false;
		HFONT hfontOld = AfGdi::SelectObjectFont(pdis->hDC, GetStockObject(DEFAULT_GUI_FONT));
		::ExtTextOutW(pdis->hDC, pdis->rcItem.left + 2, pdis->rcItem.top, ETO_OPAQUE,
			&pdis->rcItem, prgwch, cch, NULL);
		AfGdi::SelectObjectFont(pdis->hDC, hfontOld, AfGdi::OLD);
		m_vItems[pdis->itemID]->UnlockText(prgwch);

		// Restore old colors
		::SetTextColor(pdis->hDC, clrOldText);
		::SetBkColor(pdis->hDC, clrOldBack);

		if (pdis->itemState & ODS_FOCUS)
			::DrawFocusRect(pdis->hDC, &pdis->rcItem);
	}
	return true;
}

bool TssListBox::OnMeasureThisItem(MEASUREITEMSTRUCT * pmis)
{
	AssertPtr(pmis);

	Assert((uint)pmis->itemID < (uint)m_vItems.Size());

	// ENHANCE: Use the view subsystem to figure out the dimensions.

	const OLECHAR * prgwch;
	int cch;
	ITsStringPtr qtss = m_vItems[pmis->itemID];
	HRESULT hr;
	IgnoreHr(hr = qtss->LockText(&prgwch, &cch));
	if (FAILED(hr))
		return false;

	// ENHANCE: Somehow get the width and height of the string from the view stuff.
	// For now, use the default font of the desktop, which is too big, but it's easy to get.
	SIZE size;
	HDC hdc = ::GetDC(NULL);
	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, GetStockObject(DEFAULT_GUI_FONT));
	bool fSuccess = ::GetTextExtentPoint32W(hdc, prgwch, cch, &size);
	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	int iSuccess;
	iSuccess = ::ReleaseDC(NULL, hdc);
	Assert(iSuccess);
	qtss->UnlockText(prgwch);

	pmis->itemWidth = size.cx;
	pmis->itemHeight = size.cy;
	return fSuccess;
}


bool TssListBox::OnNotifyThis(int id, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	Assert(pnmh->hwndFrom == m_hwnd);

	switch (pnmh->code)
	{
	case LBN_SELCHANGE:
		return OnSelChange(pnmh->idFrom, pnmh->hwndFrom);
	case LBN_SELCANCEL:
		return OnSelCancel(pnmh->idFrom, pnmh->hwndFrom);
	case LBN_DBLCLK:
		return OnDblClick(pnmh->idFrom, pnmh->hwndFrom);
	default:
		return false;
	}
}

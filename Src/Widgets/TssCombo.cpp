/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999-2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TssCombo.cpp
Responsibility: Rand Burgett
Last reviewed:

	Implementation of TssComboEx.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

const int ktmrResetSearch = 1;

const int knToolTipTimer = 7;

bool TssComboEx::s_fInitialized;

achar TssComboEx::s_rgchBuffer[kcchMaxText];

#define kcidComboTypeAhead				29005 // not used in resource, trick in TssComboEdit

BEGIN_CMD_MAP(TssComboEdit)
	ON_CID_CHILD(kcidComboTypeAhead, &TssComboEdit::CmdTypeAhead, NULL)
END_CMD_MAP_NIL()

//:>********************************************************************************************
//:>	TssComboEx methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
TssComboEx::TssComboEx()
{
	m_hwndToolTip = NULL;
	m_fTypeAhead = false;
	if (!s_fInitialized)
	{
		s_fInitialized = true;
		INITCOMMONCONTROLSEX iccex = { isizeof(iccex), ICC_USEREX_CLASSES };
		::InitCommonControlsEx(&iccex);
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize with parameters.
----------------------------------------------------------------------------------------------*/
void TssComboEx::Create(HWND hwndParent, int cid, HWND hwndToolTip, bool fTypeAhead)
{
	m_hwndParent = hwndParent;
	m_cid = cid;
	m_hwndToolTip = hwndToolTip;
	m_fTypeAhead = fTypeAhead;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void TssComboEx::CreateAndSubclassHwnd(WndCreateStruct & wcs)
{
	SuperClass::CreateAndSubclassHwnd(wcs);

	// Make it look like a standard combo box.
	::SendMessage(Hwnd(), CB_SETITEMHEIGHT, (WPARAM)-1, 15);
	::SendMessage(Hwnd(), WM_SETFONT, (WPARAM)::GetStockObject(DEFAULT_GUI_FONT), true);

	// Subclass the combo box inside the extended combo box wrapper.
	HWND hwndCombo = GetComboControl();
	TssComboPtr qtc;
	qtc.Create();
	qtc->Subclass(hwndCombo, m_hwndParent);

	if (HasToolTip())
	{
		// Add the combo information to the tooltip.
		TOOLINFO ti = { isizeof(ti), TTF_IDISHWND };
#ifdef DEBUG
		static StrApp s_str;
		s_str.Format(_T("Missing a tooltip for combo control with ID %d"), m_cid);
		ti.lpszText = const_cast<achar *>(s_str.Chars());
#else // !DEBUG
		ti.lpszText = _T("Dummy text");
#endif // !DEBUG

		ti.hwnd = hwndCombo;
		ti.uId = (uint)ti.hwnd;
		::GetClientRect(hwndCombo, &ti.rect);
		::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);
	}

	DWORD dwStyle = ::GetWindowLong(hwndCombo, GWL_STYLE);
	dwStyle &= ~CBS_HASSTRINGS;
	::SetWindowLong(hwndCombo, GWL_STYLE, dwStyle);

	// Subclass the edit box inside the combo box.
	TssComboEditPtr qtce;
	qtce.Create();
	qtce->Subclass(GetEditControl(), m_hwndToolTip);
	Assert(::GetParent(GetEditControl()) == hwndCombo);
}

/*----------------------------------------------------------------------------------------------
	This function must be called before the Combo box has been initialized with any data.
	(i.e. before any items are inserted into it).
----------------------------------------------------------------------------------------------*/
void TssComboEx::SubclassCombo(HWND hwnd)
{
	SubclassHwnd(hwnd);
	Assert(GetCount() == 0);
}

void TssComboEx::SubclassCombo(HWND hwndDlg, int cid, DWORD dwStyleExtra, bool fTypeAhead)
{
	Create(hwndDlg, cid, NULL, fTypeAhead);

	// Set margins to leave room for sunken border effect, plus one pixel white space.
	// Yet one more pixel on the leading edge keeps the IP clear of the border.
//	SIZE sizeMargins = { ::GetSystemMetrics(SM_CXEDGE), ::GetSystemMetrics(SM_CYEDGE) };
//	m_dxpMarginLeft = sizeMargins.cx + 2;
//	m_dxpMarginRight = sizeMargins.cx + 1;
//	m_dypMarginTop = sizeMargins.cy + 1;

	HWND hwndOld = ::GetDlgItem(hwndDlg, cid);

	// Get window coordinates relative to the dialog.
	Rect rc;
//	::GetWindowRect(hwndOld, &rc);
	// NOTE: We can't call GetWindowRect here because what we get is the size of
	// the editbox part of it, not the dropdown part. This essentially sets the
	// height of the dropdown to 0, causing all sorts of weird problems.
	::SendMessage(hwndOld, CB_GETDROPPEDCONTROLRECT, 0, (LPARAM)&rc);
	::MapWindowPoints(NULL, hwndDlg, (POINT *)&rc, 2);

//	const int kcchMax = 2048;
//	achar rgch[kcchMax];
//	::GetDlgItemText(hwndDlg, wid, rgch, kcchMax);

	// Get information on old window.
	HWND hwndPrev = ::GetWindow(hwndOld, GW_HWNDPREV);
	DWORD dwStyleEx = ::GetWindowLong(hwndOld, GWL_EXSTYLE);
	DWORD dwStyle = ::GetWindowLong(hwndOld, GWL_STYLE);
	::DestroyWindow(hwndOld);

	// Create the new window and set the styles appropriately.
	WndCreateStruct wcs;
	wcs.InitChild(WC_COMBOBOXEX, hwndDlg, cid);
	wcs.style |= dwStyle;
	wcs.SetRect(rc);

	CreateAndSubclassHwnd(wcs);

	dwStyleEx &= ~WS_EX_NOPARENTNOTIFY;

	::SetWindowLong(m_hwnd, GWL_EXSTYLE, dwStyleEx | dwStyleExtra);
	::SetWindowPos(m_hwnd, hwndPrev, rc.left, rc.top, rc.Width(), rc.Height(), 0);
}

/*----------------------------------------------------------------------------------------------
	Make sure the child style is added to the combo box.
	The style must be a drop-down that allows typing.
----------------------------------------------------------------------------------------------*/
void TssComboEx::PreCreateHwnd(CREATESTRUCT & cs)
{
	cs.style &= ~CBS_SIMPLE;
	cs.style &= ~CBS_DROPDOWNLIST;
	cs.style &= ~CBS_HASSTRINGS;
	cs.style |= WS_CHILD | CBS_OWNERDRAWFIXED | CBS_DROPDOWN;
	cs.style &= ~CBS_OWNERDRAWVARIABLE;
}

/*----------------------------------------------------------------------------------------------
	This processes Windows messages on the reflector window. In general, it normally calls the
	appropriate method on the Combo class.
----------------------------------------------------------------------------------------------*/
bool TssComboEx::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case CB_ADDSTRING:
	case CB_DIR:
	case CB_FINDSTRING:
	case CB_FINDSTRINGEXACT:
	case CB_GETLBTEXT:
	case CB_INSERTSTRING:
	case CB_SELECTSTRING:
	case CB_GETLOCALE:
	case CB_SETLOCALE:
	case CBEM_GETITEM:
	case CBEM_INSERTITEM:
	case CBEM_SETITEM:
	//case WM_DRAWITEM:
	case WM_MEASUREITEM:
	case WM_COMPAREITEM:
		// We don't support these methods. In most cases, there are appropriate
		// TsString versions available.
		Assert(false);
		lnRet = CB_ERR;
		return true;

	case FW_CB_GETTEXT:
		GetText((ITsString **)lp);
		return true;

	case FW_CB_ADDSTRING:
		AddString((ITsString *)lp);
		return true;

	case FW_CB_FINDSTRING:
		lnRet = FindString(wp, (ITsString *)lp);
		return true;

	case FW_CB_FINDSTRINGEXACT:
		lnRet = FindStringExact(wp, (ITsString *)lp);
		return true;

	case FW_CB_GETLBTEXT:
		GetLBText(wp, (ITsString **)lp);
		return true;

	case FW_CB_INSERTSTRING:
		InsertString(wp, (ITsString *)lp);
		return true;

	case FW_CB_SELECTSTRING:
		SelectString(wp, (ITsString *)lp);
		return true;

	// This is defined as CB_DELETESTRING.
	case CBEM_DELETEITEM:
		lnRet = DeleteItem(wp);
		return true;

	case CB_GETCOUNT:
		GetCount();
		return true;

	case CB_GETCURSEL:
		lnRet = GetCurSel();
		return true;

	case CB_GETDROPPEDCONTROLRECT:
		GetDroppedControlRect((RECT *)lp);
		return true;

	case CB_GETDROPPEDSTATE:
		lnRet = GetDroppedState();
		return true;

	case CB_GETDROPPEDWIDTH :
		lnRet = GetDroppedWidth();
		return true;

	case CB_GETEDITSEL:
		lnRet = GetEditSel((uint *)wp, (uint *)lp);
		return true;

	case CB_GETEXTENDEDUI:
		lnRet = GetExtendedUI();
		return true;

	case CB_GETHORIZONTALEXTENT:
		lnRet = GetHorizontalExtent();
		return true;

	case CB_GETITEMDATA:
		lnRet = GetItemData(wp);
		return true;

	case CB_GETITEMHEIGHT:
		lnRet = GetItemHeight(wp);
		return true;

	case CB_GETLBTEXTLEN:
		lnRet = GetLBTextLen(wp);
		return true;

	case CB_GETTOPINDEX:
		GetTopIndex();
		return true;

	case CB_INITSTORAGE:
		lnRet = InitStorage((int)wp, (DWORD) lp);
		return true;

	case CB_LIMITTEXT:
		lnRet = LimitText(wp);
		return true;

	case CB_RESETCONTENT:
		ResetContent();
		return true;

	case CB_SETCURSEL:
		lnRet = SetCurSel(wp);
		return true;

	case CB_SETDROPPEDWIDTH:
		lnRet = SetDroppedWidth (wp);
		return true;

	case CB_SETEDITSEL:
		lnRet = SetEditSel(LOWORD(lp), HIWORD(lp));
		return true;

	case CB_SETEXTENDEDUI:
		lnRet = SetExtendedUI(wp);
		return true;

	case CB_SETHORIZONTALEXTENT:
		SetHorizontalExtent(wp);
		return true;

	case CB_SETITEMDATA:
		lnRet = SetItemData(wp, (DWORD)lp);
		return true;

	case CB_SETITEMHEIGHT:
		lnRet = SetItemHeight(wp, (int)lp);
		return true;

	case CB_SETTOPINDEX:
		lnRet = SetTopIndex(wp);
		return true;

	case CB_SHOWDROPDOWN:
		ShowDropDown((bool) wp);
		return true;

	case CBEM_GETCOMBOCONTROL:
		lnRet = (long)GetComboControl();
		return true;

	case CBEM_GETEDITCONTROL:
		lnRet = (long)GetEditControl();
		return true;

	case CBEM_GETEXTENDEDSTYLE:
		lnRet = GetExtendedStyle();
		return true;

	case CBEM_GETIMAGELIST:
		lnRet = (long)GetImageList();
		return true;

	case FW_CBEM_GETITEM:
		lnRet = GetItem((FW_COMBOBOXEXITEM *)lp);
		return true;

	case CBEM_GETUNICODEFORMAT:
		lnRet = GetUnicodeFormat();
		return true;

	case CBEM_HASEDITCHANGED:
		lnRet = HasEditChanged();
		return true;

	case FW_CBEM_INSERTITEM:
		lnRet = InsertItem((FW_COMBOBOXEXITEM *)lp);
		return true;

	case CBEM_SETEXTENDEDSTYLE:
		lnRet = (long)SetExtendedStyle(wp, lp);
		return true;

	case CBEM_SETIMAGELIST:
		lnRet = (long)SetImageList((HIMAGELIST)lp);
		return true;

	case FW_CBEM_SETITEM:
		lnRet = SetItem((FW_COMBOBOXEXITEM *)lp);
		return true;

	case CBEM_SETUNICODEFORMAT:
		lnRet = SetUnicodeFormat(wp);
		return true;

	case WM_COMMAND:
		return false;

	case WM_SETFOCUS:
		TurnOnDefaultKeyboard();
		// pass focus down to embedded combo control; our wrapper window never can have focus
		// (RAID #2267)
		::SetFocus(GetComboControl());
		return true;

	case WM_COPY:
		Copy();
		return true;

	case WM_CUT:
		Cut();
		return true;

	case WM_PASTE:
		Paste();
		return true;

	case WM_CTLCOLOREDIT: // ie, the internal edit box
	case WM_CTLCOLORSTATIC:
	case WM_CTLCOLORBTN:
	case WM_CTLCOLORLISTBOX:
		if (::IsWindowEnabled(GetComboControl()))
		{	// Send message to parent only if window is enabled, otherwise parent draws
			// the disabled window white!
			::SendMessage(::GetParent(m_hwnd), wm, wp, (LPARAM)m_hwnd);
			return true;
		}
		else
			return false;

	default:
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle window notifications.
----------------------------------------------------------------------------------------------*/
bool TssComboEx::OnNotifyThis(int id, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	Assert(pnmh->hwndFrom == m_hwnd);

	switch (pnmh->code)
	{
	case CBEN_BEGINEDIT:
		TurnOnDefaultKeyboard();
		return OnBeginEdit(pnmh, lnRet);

	case CBEN_DELETEITEM:
		return _OnDeleteItem((NMCOMBOBOXEX *)pnmh, lnRet);

	case CBEN_DRAGBEGIN:
		return _OnDragBegin((NMCBEDRAGBEGIN *)pnmh, lnRet);

	case CBEN_ENDEDIT:
		return _OnEndEdit((NMCBEENDEDIT *)pnmh, lnRet);

	case CBEN_GETDISPINFO:
		return _OnGetDispInfo((NMCOMBOBOXEX *)pnmh, lnRet);

	case CBEN_INSERTITEM:
		return _OnInsertItem((NMCOMBOBOXEX *)pnmh, lnRet);

	case NM_SETCURSOR:
		return OnSetCursor((NMMOUSE *)pnmh, lnRet);

	default:
		break;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	This has to be handled so that it doesn't add the command message to the message queue in
	AfWnd.
----------------------------------------------------------------------------------------------*/
bool TssComboEx::OnCommand(int cid, int nc, HWND hctl)
{
	bool fHandled = false;

	switch (nc)
	{
	case CBN_CLOSEUP:
		fHandled = OnCloseUp(cid, hctl);
		break;

	case CBN_DBLCLK:
		fHandled = OnDblClk(cid, hctl);
		break;

	case CBN_DROPDOWN:
		fHandled = OnDropDown(cid, hctl);
		break;

	case CBN_EDITCHANGE:
		fHandled = OnEditChange(cid, hctl);
		break;

	case CBN_EDITUPDATE:
		fHandled = EditUpDate(cid, hctl);
		break;

	case CBN_ERRSPACE:
		fHandled = OnErrSpace(cid, hctl);
		break;

	case CBN_KILLFOCUS:
		fHandled = OnKillFocus(cid, hctl);
		break;

	case CBN_SELCHANGE:
		fHandled = OnSelChange(cid, hctl);
		break;

	case CBN_SELENDCANCEL:
		fHandled = OnSelEndCancel(cid, hctl);
		break;

	case CBN_SELENDOK:
		fHandled = OnSelEndOK(cid, hctl);
		break;

	case CBN_SETFOCUS:
		fHandled = OnSetFocus(cid, hctl);
		break;
	}

	if (fHandled)
		return true;

	if (hctl)
	{
		// Convert to a notify message.
		NMHDR nmh;
		nmh.hwndFrom = m_hwnd;
		nmh.idFrom = cid;
		nmh.code = nc;
		AssertPtr(Parent());
		::SendMessage(Parent()->Hwnd(), WM_NOTIFY, cid, (LPARAM)&nmh);
		return false;
	}

	// NOTE: Do not call SuperClass::OnCommand here because we do not want this message
	// to be added to the message queue.
	return false;
}


/*----------------------------------------------------------------------------------------------
	An application sends a CBEM_DELETEITEM message to delete a string in the list
	box of a combo box.
----------------------------------------------------------------------------------------------*/
int TssComboEx::DeleteItem(uint iItem)
{
	return AfWnd::DefWndProc(CBEM_DELETEITEM, iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_GETCOUNT message to retrieve the number of items in the list box
	of a combo box.
----------------------------------------------------------------------------------------------*/
int TssComboEx::GetCount()
{
	return AfWnd::DefWndProc(CB_GETCOUNT, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_GETCURSEL message to retrieve the index of the currently selected
	item, if any, in the list box of a combo box.
----------------------------------------------------------------------------------------------*/
int TssComboEx::GetCurSel()
{
	return AfWnd::DefWndProc(CB_GETCURSEL, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_GETDROPPEDCONTROLRECT message to retrieve the screen coordinates
	of the drop-down list box of a combo box.
----------------------------------------------------------------------------------------------*/
int TssComboEx::GetDroppedControlRect(RECT * prc)
{
	AssertPtr(prc);
	return AfWnd::DefWndProc(CB_GETDROPPEDCONTROLRECT, 0, (LPARAM)prc);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_GETDROPPEDSTATE message to determine whether the list box of a
	combo box is dropped down.
----------------------------------------------------------------------------------------------*/
bool TssComboEx::GetDroppedState()
{
	return AfWnd::DefWndProc(CB_GETDROPPEDSTATE, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends the CB_GETDROPPEDWIDTH message to retrieve the minimum allowable width,
	in pixels, of the list box of a combo box with the CBS_DROPDOWN or CBS_DROPDOWNLIST style.

	NOTE (DarrellZ): In MSDN, it didn't say that this message is forwarded by the extended combo
	box, so it might not work properly.
	NOTE (SteveMc): this function never seems to be called.
----------------------------------------------------------------------------------------------*/
int TssComboEx::GetDroppedWidth()
{
	return AfWnd::DefWndProc(CB_GETDROPPEDWIDTH, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends the CB_GETHORIZONTALEXTENT message to retrieve from a combo box the
	width, in pixels, by which the list box can be scrolled horizontally (the scrollable
	width). This is applicable only if the list box has a horizontal scroll bar.

	NOTE (DarrellZ): In MSDN, it didn't say that this message is forwarded by the extended combo
	box, so it might not work properly.
	NOTE (SteveMc): this function never seems to be called.
----------------------------------------------------------------------------------------------*/
UINT TssComboEx::GetHorizontalExtent()
{
	return AfWnd::DefWndProc(CB_GETHORIZONTALEXTENT, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_GETEDITSEL message to determine whether a combo box has the
	default user interface or the extended user interface.

	NOTE (DarrellZ): In MSDN, it didn't say that this message is forwarded by the extended combo
	box, so it might not work properly.
	NOTE (SteveMc): this function never seems to be called.
----------------------------------------------------------------------------------------------*/
uint TssComboEx::GetEditSel(uint * pichStart, uint * pichEnd)
{
	AssertPtrN(pichStart);
	AssertPtrN(pichEnd);
	return AfWnd::DefWndProc(CB_GETEDITSEL, (WPARAM)pichStart, (LPARAM)pichEnd);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_GETEXTENDEDUI message to determine whether a combo box has the
	default user interface or the extended user interface.
----------------------------------------------------------------------------------------------*/
bool TssComboEx::GetExtendedUI()
{
	return AfWnd::DefWndProc(CB_GETEXTENDEDUI, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_GETITEMDATA message to a combo box to retrieve the
	application-supplied 32-bit value associated with the specified item in the combo box.
----------------------------------------------------------------------------------------------*/
DWORD TssComboEx::GetItemData(int iItem)
{
	return AfWnd::DefWndProc(CB_GETITEMDATA, (WPARAM)iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_GETITEMHEIGHT message to determine the height of list items or the
	selection field in a combo box.
----------------------------------------------------------------------------------------------*/
int TssComboEx::GetItemHeight(int iItem)
{
	return AfWnd::DefWndProc(CB_GETITEMHEIGHT, (WPARAM)iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_GETLBTEXTLEN message to retrieve the length, in characters, of a
	string in the list of a combo box.
----------------------------------------------------------------------------------------------*/
int TssComboEx::GetLBTextLen(int iItem)
{
	return AfWnd::DefWndProc(CB_GETLBTEXTLEN, (WPARAM)iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends the CB_GETTOPINDEX message to retrieve the zero-based index of the
	first visible item in the list box portion of a combo box. Initially the item with index 0
	is at the top of the list box, but if the list box contents have been scrolled, another
	item may be at the top.

	NOTE (DarrellZ): In MSDN, it didn't say that this message is forwarded by the extended combo
	box, so it might not work properly.
	NOTE (SteveMc): this function never seems to be called.
----------------------------------------------------------------------------------------------*/
int TssComboEx::GetTopIndex()
{
	return AfWnd::DefWndProc(CB_GETTOPINDEX, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends the CB_INITSTORAGE message before adding a large number of items to the
	list box portion of a combo box. This message allocates memory for storing list box items.

	NOTE (DarrellZ): In MSDN, it didn't say that this message is forwarded by the extended combo
	box, so it might not work properly.
	NOTE (SteveMc): this function never seems to be called.
----------------------------------------------------------------------------------------------*/
int TssComboEx::InitStorage(int cItems, uint cb)
{
	return AfWnd::DefWndProc(CB_INITSTORAGE, (WPARAM)cItems, (LPARAM)cb);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_LIMITTEXT message to limit the length of the text the user may
	type into the edit control of a combo box.
----------------------------------------------------------------------------------------------*/
bool TssComboEx::LimitText(int cchMax)
{
	return AfWnd::DefWndProc(CB_LIMITTEXT, (WPARAM)cchMax, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_RESETCONTENT message to remove all items from the list box and
	edit control of a combo box.
----------------------------------------------------------------------------------------------*/
int TssComboEx::ResetContent()
{
	return AfWnd::DefWndProc(CB_RESETCONTENT, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_SETCURSEL message to select a string in the list of a combo box.
	If necessary, the list scrolls the string into view. The text in the edit control of the
	combo box changes to reflect the new selection, and any previous selection in the list is
	removed.
----------------------------------------------------------------------------------------------*/
int TssComboEx::SetCurSel(int iItem)
{
	return AfWnd::DefWndProc(CB_SETCURSEL, (WPARAM)iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends the CB_SETDROPPEDWIDTH message to set the maximum allowable width, in
	pixels, of the list box of a combo box with the CBS_DROPDOWN or CBS_DROPDOWNLIST style.
----------------------------------------------------------------------------------------------*/
int TssComboEx::SetDroppedWidth(uint dxp)
{
	return AfWnd::DefWndProc(CB_SETDROPPEDWIDTH, (WPARAM)dxp, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_SETEDITSEL message to select characters in the edit control of a
	combo box.

	NOTE (DarrellZ): In MSDN, it didn't say that this message is forwarded by the extended combo
	box, so it might not work properly.
	NOTE (SteveMc): this function never seems to be called.
----------------------------------------------------------------------------------------------*/
bool TssComboEx::SetEditSel(int ichStart, int ichEnd)
{
	return AfWnd::DefWndProc(CB_SETEDITSEL, 0, MAKELPARAM(ichStart, ichEnd));
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_SETEXTENDEDUI message to select either the default user interface
	or the extended user interface for a combo box that has the CBS_DROPDOWN or
	CBS_DROPDOWNLIST style.
----------------------------------------------------------------------------------------------*/
int TssComboEx::SetExtendedUI(bool fExtended)
{
	return AfWnd::DefWndProc(CB_SETEXTENDEDUI, (WPARAM)fExtended, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends the CB_SETHORIZONTALEXTENT message to set the width, in pixels, by
	which a list box can be scrolled horizontally (the scrollable width). If the width of the
	list box is smaller than this value, the horizontal scroll bar horizontally scrolls items
	in the list box. If the width of the list box is equal to or greater than this value, the
	horizontal scroll bar is hidden or, if the combo box has the CBS_DISABLENOSCROLL style,
	disabled.

	NOTE (DarrellZ): In MSDN, it didn't say that this message is forwarded by the extended combo
	box, so it might not work properly.
	NOTE (SteveMc): this function never seems to be called.
----------------------------------------------------------------------------------------------*/
void TssComboEx::SetHorizontalExtent(uint dxpExtent)
{
	AfWnd::DefWndProc(CB_SETHORIZONTALEXTENT, (WPARAM)dxpExtent, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_SETITEMDATA message to set the 32-bit value associated with the
	specified item in a combo box.
----------------------------------------------------------------------------------------------*/
int TssComboEx::SetItemData(int iItem, DWORD dwItemData)
{
	return AfWnd::DefWndProc(CB_SETITEMDATA, (WPARAM)iItem, (LPARAM)dwItemData);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_SETITEMHEIGHT message to set the height of list items or the
	selection field in a combo box.
----------------------------------------------------------------------------------------------*/
int TssComboEx::SetItemHeight(int iItem, uint dypItem)
{
	return AfWnd::DefWndProc(CB_SETITEMHEIGHT, (WPARAM)iItem, (LPARAM)dypItem);
}


/*----------------------------------------------------------------------------------------------
	An application sends the CB_SETTOPINDEX message to ensure that a particular item is visible
	in the list box of a combo box. The system scrolls the list box contents so that either the
	specified item appears at the top of the list box or the maximum scroll range has been
	reached.

	NOTE (DarrellZ): In MSDN, it didn't say that this message is forwarded by the extended combo
	box, so it might not work properly.
	NOTE (SteveMc): this function never seems to be called.
----------------------------------------------------------------------------------------------*/
int TssComboEx::SetTopIndex(int iItem)
{
	return AfWnd::DefWndProc(CB_SETTOPINDEX, (WPARAM)iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_SHOWDROPDOWN message to show or hide the list box of a combo box
	that has the CBS_DROPDOWN or CBS_DROPDOWNLIST style.
----------------------------------------------------------------------------------------------*/
bool TssComboEx::ShowDropDown(bool fShowIt)
{
	return AfWnd::DefWndProc(CB_SHOWDROPDOWN, fShowIt, 0);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void TssComboEx::Copy()
{
	AfWnd::DefWndProc(WM_COPY, 0, 0);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void TssComboEx::Cut()
{
	AfWnd::DefWndProc(WM_CUT, 0, 0);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void TssComboEx::Paste()
{
	AfWnd::DefWndProc(WM_PASTE, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a FW_CB_GETTEXT message to copy the text that corresponds to a window
	into a buffer provided by the caller.
----------------------------------------------------------------------------------------------*/
int TssComboEx::GetText(ITsString ** pptss)
{
	AssertPtr(pptss);
	Assert(!*pptss);

	int cch = AfWnd::DefWndProc(WM_GETTEXTLENGTH, 0, 0) + 1;
	achar * prgch = NewObj achar[cch + 1];
	if (!prgch)
		return 0;

	cch = AfWnd::DefWndProc(WM_GETTEXT, cch, (LPARAM)prgch);

	ITsStrFactoryPtr qtsf;
	StrUni stu;
	try
	{
		qtsf.CreateInstance(CLSID_TsStrFactory);
		stu = prgch;
		delete prgch;
	}
	catch (...)
	{
		if (prgch)
			delete prgch;
		return -1;
	}

	HRESULT hr;
	IgnoreHr(hr = qtsf->MakeStringRgch(stu.Chars(), stu.Length(), WritingSystem(), pptss));
	if (FAILED(hr))
		return 0;
	return cch;
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_ADDSTRING message to add a string to the list box of a combo box.
	If the combo box does not have the CBS_SORT style, the string is added to the end of the
	list. Otherwise, the string is inserted into the list, and the list is sorted.
----------------------------------------------------------------------------------------------*/
int TssComboEx::AddString(ITsString * ptss)
{
	AssertPtr(ptss);

	COMBOBOXEXITEM cbi = { CBEIF_TEXT, -1 };
	StrApp str;
	if (QtssToStr(ptss, str) == false)
		return CB_ERR;
	cbi.pszText = const_cast<achar *>(str.Chars());
	return AfWnd::DefWndProc(CBEM_INSERTITEM, 0, (LPARAM)&cbi);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_FINDSTRING message to search the list box of a combo box for an
	item beginning with the characters in a specified string.
----------------------------------------------------------------------------------------------*/
int TssComboEx::FindString(int iItemAfter, ITsString * ptss)
{
	AssertPtr(ptss);

#if 0 // not needed by working code
	StrApp str;
	if (QtssToStr(ptss, str) == false)
		return CB_ERR;
	// This always returns zero.
	// return AfWnd::DefWndProc(CB_FINDSTRING, (WPARAM)iItemStart, (LPARAM)str.Chars());
	// This always returns CB_ERR (indicates not found).
	// return ::SendMessage(::GetWindow(m_hwnd, GW_CHILD), CB_FINDSTRING, (WPARAM)iItemStart,
	//	(LPARAM)str.Chars());
#endif

	int citem = GetCount();
	int citemsToGo = citem;
	int iitem = iItemAfter;

	SmartBstr sbstr1;
	HRESULT hr;
	IgnoreHr(hr = ptss->get_Text(&sbstr1));
	if (FAILED(hr))
		return CB_ERR;
	ILgCharacterPropertyEnginePtr qcpe;
	qcpe.CreateInstance(CLSID_LgIcuCharPropEngine);
	CheckHr(qcpe->put_Locale(0x409));	// TODO: Don't assume US English!
	SmartBstr sbstrU1;
	IgnoreHr(hr = qcpe->ToUpper(sbstr1, &sbstrU1));
	if (FAILED(hr))
		return CB_ERR;

	FW_COMBOBOXEXITEM fcbi;
	fcbi.mask = CBEIF_TEXT;
	while (citemsToGo--)
	{
		if (++iitem == citem)
			iitem = 0;
		fcbi.iItem = iitem;
		if (!GetItem(&fcbi))
			break;
		SmartBstr sbstr2;
		IgnoreHr(hr = fcbi.qtss->get_Text(&sbstr2));
		if (FAILED(hr))
			return CB_ERR;
		SmartBstr sbstrU2;
		IgnoreHr(hr = qcpe->ToUpper(sbstr2, &sbstrU2));
		if (FAILED(hr))
			return CB_ERR;
		if (sbstrU1.Length() <= sbstrU2.Length() &&
			wcsncmp(sbstrU1.Chars(), sbstrU2.Chars(), sbstrU1.Length()) == 0)
		{
			return iitem;
		}
	}

	return CB_ERR;
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_FINDSTRINGEXACT message to find the first list box string in a
	combo box that matches the string specified in the ptss parameter.
----------------------------------------------------------------------------------------------*/
int TssComboEx::FindStringExact(int iStartAfter, ITsString * ptss)
{
	AssertPtr(ptss);

	int citem = GetCount();
	int citemsToGo = citem;
	int iitem = iStartAfter;
	int iitemMatch = CB_ERR;

	const OLECHAR * pwrgch1;
	const OLECHAR * pwrgch2;
	int cch1;
	int cch2;
	HRESULT hr;
	IgnoreHr(hr = ptss->LockText(&pwrgch1, &cch1));
	if (FAILED(hr))
		return CB_ERR;

	//ComBool fEquals;
	FW_COMBOBOXEXITEM fcbi;
	fcbi.mask = CBEIF_TEXT;
	while (citemsToGo--)
	{
		if (++iitem == citem)
			iitem = 0;
		fcbi.iItem = iitem;
		if (!GetItem(&fcbi))
			break;
#if 0
		// REVIEW DarrellZ: Figure out why Equals doesn't work here.
		CheckHr(ptss->Equals(fcbi.qtss, &fEquals));
		if (fEquals)
		{
			iitemMatch = iitem;
			break;
		}
#else // !0
		IgnoreHr(hr = fcbi.qtss->LockText(&pwrgch2, &cch2));
		if (FAILED(hr))
			break;
		if (cch1 == cch2 && wcscmp(pwrgch1, pwrgch2) == 0)
		{
			fcbi.qtss->UnlockText(pwrgch2);
			iitemMatch = iitem;
			break;
		}
		fcbi.qtss->UnlockText(pwrgch2);
#endif // !0
	}

	ptss->UnlockText(pwrgch1);
	return iitemMatch;
}


/*----------------------------------------------------------------------------------------------
	An application sends a FW_CB_GETLBTEXT message to retrieve a string from the list of a
	combobox.
----------------------------------------------------------------------------------------------*/
int TssComboEx::GetLBText(int iItem, ITsString ** pptss)
{
	AssertPtr(pptss);
	Assert(!*pptss);

	FW_COMBOBOXEXITEM fcbei;
	fcbei.mask = CBEIF_TEXT;
	fcbei.iItem = iItem;
	GetItem(&fcbei);
	*pptss = fcbei.qtss;
	AddRefObj(*pptss);

	return AfWnd::DefWndProc(CB_GETLBTEXTLEN, (WPARAM)iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_INSERTSTRING message to insert a string into the list box of a
	combo box. Unlike the CB_ADDSTRING message, the CB_INSERTSTRING message does not cause a
	list with the CBS_SORT style to be sorted.
----------------------------------------------------------------------------------------------*/
int TssComboEx::InsertString(int iItem, ITsString * ptss)
{
	AssertPtr(ptss);

	COMBOBOXEXITEM cbi = { CBEIF_TEXT, iItem };
	StrApp str;
	if (QtssToStr(ptss, str) == false)
		return CB_ERR;
	cbi.pszText = const_cast<achar *>(str.Chars());
	return AfWnd::DefWndProc(CBEM_INSERTITEM, 0, (LPARAM)&cbi);
}


/*----------------------------------------------------------------------------------------------
	An application sends a CB_SELECTSTRING message to search the list of a combo box for an
	item that begins with the characters in a specified string. If a matching item is found,
	it is selected and copied to the edit control.

	NOTE (DarrellZ): In MSDN, it didn't say that this message is forwarded by the extended combo
	box, so it might not work properly.
	NOTE (SteveMc): this function never seems to be called.
----------------------------------------------------------------------------------------------*/
int TssComboEx::SelectString(int iItemStart, ITsString * ptss)
{
	AssertPtr(ptss);

	StrApp str;
	if (QtssToStr(ptss, str) == false)
		return CB_ERR;
	return ::SendMessage(GetComboControl(), CB_SELECTSTRING, iItemStart, (LPARAM)str.Chars());
	//return AfWnd::DefWndProc(CB_SELECTSTRING, (WPARAM)iItemStart, (LPARAM)str.Chars());
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssComboEx::QtssToStr(ITsString * ptss, StrApp & str)
{
	AssertPtr(ptss);

	const OLECHAR * pwrgch = NULL;
	int cch;

	try
	{
		ITsTextPropsPtr qttp;
		ptss->get_PropertiesAt(0, &qttp); // We may have an empty string.
		int var;
		if (qttp)
			qttp->GetIntPropValues(ktptWs, &var, &m_ws);

		HRESULT hr;
		IgnoreHr(hr = ptss->LockText(&pwrgch, &cch));
		if (FAILED(hr))
			return false;
		str.Assign(pwrgch, cch);
		ptss->UnlockText(pwrgch);
	}
	catch (...)
	{
		if (pwrgch)
			ptss->UnlockText(pwrgch);
		return false;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
HWND TssComboEx::GetComboControl()
{
	return (HWND)AfWnd::DefWndProc(CBEM_GETCOMBOCONTROL, 0, 0);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
HWND TssComboEx::GetEditControl()
{
	return (HWND)AfWnd::DefWndProc(CBEM_GETEDITCONTROL, 0, 0);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
uint TssComboEx::GetExtendedStyle()
{
	return AfWnd::DefWndProc(CBEM_GETEXTENDEDSTYLE, 0, 0);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
HIMAGELIST TssComboEx::GetImageList()
{
	return (HIMAGELIST)AfWnd::DefWndProc(CBEM_GETIMAGELIST, 0, 0);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssComboEx::GetItem(FW_COMBOBOXEXITEM * pfcbi)
{
	AssertPtr(pfcbi);

	COMBOBOXEXITEM cbi;
	memset(&cbi, 0, sizeof(cbi));
	cbi.mask = pfcbi->mask;
	cbi.iItem = pfcbi->iItem;
	cbi.pszText = s_rgchBuffer;
	cbi.cchTextMax = kcchMaxText;
	bool fRet = AfWnd::DefWndProc(CBEM_GETITEM, 0, (LPARAM)&cbi);

	if (!_CopyItem(cbi, *pfcbi))
		return false;
	return fRet;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssComboEx::GetUnicodeFormat()
{
	return AfWnd::DefWndProc(CBEM_GETUNICODEFORMAT, 0, 0);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssComboEx::HasEditChanged()
{
	return AfWnd::DefWndProc(CBEM_HASEDITCHANGED, 0, 0);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
int TssComboEx::InsertItem(FW_COMBOBOXEXITEM * pfcbi)
{
	AssertPtr(pfcbi);
	Assert(!(pfcbi->mask & CBEIF_TEXT) || pfcbi->qtss);

	COMBOBOXEXITEM cbi;
	if (!_CopyItem(*pfcbi, cbi))
		return -1;

	return AfWnd::DefWndProc(CBEM_INSERTITEM, 0, (LPARAM)&cbi);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
uint TssComboEx::SetExtendedStyle(uint nExMask, uint nExStyle)
{
	return AfWnd::DefWndProc(CBEM_SETEXTENDEDSTYLE, nExMask, nExStyle);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
HIMAGELIST TssComboEx::SetImageList(HIMAGELIST himl)
{
	return (HIMAGELIST)AfWnd::DefWndProc(CBEM_SETIMAGELIST, 0, (LPARAM)himl);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssComboEx::SetItem(FW_COMBOBOXEXITEM * pfcbi)
{
	AssertPtr(pfcbi);
	Assert(!(pfcbi->mask & CBEIF_TEXT) || pfcbi->qtss);

	COMBOBOXEXITEM cbi;
	if (!_CopyItem(*pfcbi, cbi))
		return false;

	return AfWnd::DefWndProc(CBEM_SETITEM, 0, (LPARAM)&cbi);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssComboEx::SetUnicodeFormat(bool fUnicode)
{
	return AfWnd::DefWndProc(CBEM_SETUNICODEFORMAT, fUnicode, 0);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssComboEx::_OnDeleteItem(NMCOMBOBOXEX * pnmcb, long & lnRet)
{
	AssertPtr(pnmcb);

	FW_NMCOMBOBOXEX fnmcb;
	fnmcb.hdr = pnmcb->hdr;
	if (!_CopyItem(pnmcb->ceItem, fnmcb.ceItem))
		return false;
	return OnDeleteItem(&fnmcb, lnRet);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssComboEx::_OnDragBegin(NMCBEDRAGBEGIN * pnmdb, long & lnRet)
{
	AssertPtr(pnmdb);

	FW_NMCBEDRAGBEGIN fnmdb;
	fnmdb.hdr = pnmdb->hdr;
	fnmdb.iItemid = pnmdb->iItemid;

	ITsStrFactoryPtr qtsf;
	try
	{
		qtsf.CreateInstance(CLSID_TsStrFactory);
		StrUni stu(pnmdb->szText);
		HRESULT hr;
		IgnoreHr(hr = qtsf->MakeStringRgch(stu.Chars(), stu.Length(), WritingSystem(), &fnmdb.qtss));
		if (FAILED(hr))
			return false;
	}
	catch (...)
	{
		return false;
	}

	return OnDragBegin(&fnmdb, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Answer m_ws if it is non-zero. If it is zero, try to figure the default UI writing system.
	Currently creates a writing system factory using CreateInstance if it doesn't already
	have one, which is OK for WorldPad. DN and other apps need to set at least the wsf somehow.
	(Probably each time they change view...or at least database...).
	Todo SteveMc(JohnT): all clients should set m_qwsf to something appropriate.
----------------------------------------------------------------------------------------------*/
int TssComboEx::WritingSystem()
{
	if (m_ws == 0)
	{
		if (!m_qwsf)
			m_qwsf.CreateInstance(CLSID_LgWritingSystemFactory); // valid only for WorldPad!
		CheckHr(m_qwsf->get_UserWs(&m_ws));
	}
	return m_ws;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssComboEx::_OnEndEdit(NMCBEENDEDIT * pnmee, long & lnRet)
{
	AssertPtr(pnmee);

	FW_NMCBEENDEDIT fnmee;
	fnmee.hdr = pnmee->hdr;
	fnmee.fChanged = pnmee->fChanged;
	fnmee.iNewSelection = pnmee->iNewSelection;
	fnmee.iWhy = pnmee->iWhy;

	ITsStrFactoryPtr qtsf;
	try
	{
		qtsf.CreateInstance(CLSID_TsStrFactory);
		StrUni stu(pnmee->szText);
		HRESULT hr;
		IgnoreHr(hr = qtsf->MakeStringRgch(stu.Chars(), stu.Length(), WritingSystem(), &fnmee.qtss));
		if (FAILED(hr))
			return false;
	}
	catch (...)
	{
		return false;
	}

	return OnEndEdit(&fnmee, lnRet);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssComboEx::_OnGetDispInfo(NMCOMBOBOXEX * pnmcb, long & lnRet)
{
	AssertPtr(pnmcb);

	FW_NMCOMBOBOXEX fnmcb;
	fnmcb.hdr = pnmcb->hdr;
	if (!_CopyItem(pnmcb->ceItem, fnmcb.ceItem))
		return false;
	if (OnGetDispInfo(&fnmcb, lnRet))
	{
		if (!_CopyItem(fnmcb.ceItem, pnmcb->ceItem))
			return false;
		return true;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssComboEx::_OnInsertItem(NMCOMBOBOXEX * pnmcb, long & lnRet)
{
	AssertPtr(pnmcb);

	FW_NMCOMBOBOXEX fnmcb;
	fnmcb.hdr = pnmcb->hdr;
	if (!_CopyItem(pnmcb->ceItem, fnmcb.ceItem))
		return false;
	return OnInsertItem(&fnmcb, lnRet);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssComboEx::_CopyItem(const COMBOBOXEXITEM & cbi, FW_COMBOBOXEXITEM & fcbi)
{
	fcbi.iImage = cbi.iImage;
	fcbi.iIndent = cbi.iIndent;
	fcbi.iItem = cbi.iItem;
	fcbi.iOverlay = cbi.iOverlay;
	fcbi.iSelectedImage = cbi.iSelectedImage;
	fcbi.lParam = cbi.lParam;
	fcbi.mask = cbi.mask;

	/*  If you place a breakpoint in TssComboEx::InsertItem and start Notebook, before returning
		the final DefWndProc, you'll see that cbi contains an 8-bit pszText string. However, by
		the time this reaches TssComboEx::_CopyItem, the string in cbi has changed to a 16-bit
		string. This messes up _CopyItem because it expects an 8-bit string. The UNICODE and
		_UNICODE flags are definitly not on, but yet something in Windows is converting the
		COMBOBOXEXITEM to a COMBOBOXEXITEMW. Converting COMBOBOXEXITEM to COMBOBOXEXITEMA and
		NMCOMBOBOXEX to NMCOMBOBOXEXA still does not affect this internal translation. The
		conversion apparently takes place in user32.dll!CharLowerBufW()+0x98 which shows up in
		the debug stack. I (KenZ) don't know how to get around this problem. Fortunately, at
		least so far, this doesn't seem to affect what the end-user sees. */

	if (cbi.mask & CBEIF_TEXT)
	{
		ITsStrFactoryPtr qtsf;
		try
		{
			qtsf.CreateInstance(CLSID_TsStrFactory);
			StrUni stu(cbi.pszText);
			HRESULT hr;
			IgnoreHr(hr = qtsf->MakeStringRgch(stu.Chars(), stu.Length(), WritingSystem(), &fcbi.qtss));
			if (FAILED(hr))
				return false;
		}
		catch (...)
		{
			return false;
		}
	}
	return true;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool TssComboEx::_CopyItem(const FW_COMBOBOXEXITEM & fcbi, COMBOBOXEXITEM & cbi)
{
	cbi.mask = fcbi.mask;
	cbi.iItem = fcbi.iItem;
	cbi.iImage = fcbi.iImage;
	cbi.iIndent = fcbi.iIndent;
	cbi.iOverlay = fcbi.iOverlay;
	cbi.iSelectedImage = fcbi.iSelectedImage;
	cbi.lParam = fcbi.lParam;

	if (fcbi.mask & CBEIF_TEXT)
	{
		StrApp str;
		if (!QtssToStr(fcbi.qtss, str))
			return false;
		int cch = Min(kcchMaxText - 1, str.Length());
		memmove(s_rgchBuffer, str.Chars(), cch * isizeof(achar));
		s_rgchBuffer[cch] = 0;
		cbi.pszText = s_rgchBuffer;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	When setting focus to the control, turn on the default keyboard.
----------------------------------------------------------------------------------------------*/
void TssComboEx::TurnOnDefaultKeyboard()
{
	// For comparison, we want only the LANGID portion of the HKL.
	HKL hklCurr = reinterpret_cast<HKL>(LANGIDFROMLCID(::GetKeyboardLayout(0)));
	LCID lcidDefault = AfApp::GetDefaultKeyboard();
	// For keyboard selection, we want only the LANGID portion of the LCID.
	HKL hklDefault = reinterpret_cast<HKL>(LANGIDFROMLCID(lcidDefault));
	if (hklCurr != hklDefault)
	{
#if 99
		StrAnsi sta;
		sta.Format("TssComboEx::TurnOnDefaultKeyboard() -"
			" ::ActivateKeyboardLayout(%x, KLF_SETFORPROCESS);\n",
			hklDefault);
		::OutputDebugStringA(sta.Chars());
#endif
		::ActivateKeyboardLayout(hklDefault, KLF_SETFORPROCESS);
	}
}

//:>********************************************************************************************
//:>	TssComboEdit methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Return true if the combo-box does type-ahead.
----------------------------------------------------------------------------------------------*/
bool TssComboEdit::DoesTypeAhead()
{
	HWND hwndCombo = ::GetParent(m_hwnd);
	TssComboPtr qtc = dynamic_cast<TssCombo *>(AfWnd::GetAfWnd(hwndCombo));
	return qtc->DoesTypeAhead();

}

/*----------------------------------------------------------------------------------------------
	Return true if the combo-box has a tool-tip.
----------------------------------------------------------------------------------------------*/
bool TssComboEdit::HasToolTip()
{
	HWND hwndCombo = ::GetParent(m_hwnd);
	TssComboPtr qtc = dynamic_cast<TssCombo *>(AfWnd::GetAfWnd(hwndCombo));
	return qtc->HasToolTip();
}

/*----------------------------------------------------------------------------------------------
	Return the main tool bar or dialog that contains the combo box.
----------------------------------------------------------------------------------------------*/
HWND TssComboEdit::MainParent()
{
	HWND hwndCombo = ::GetParent(m_hwnd);
	TssComboPtr qtc = dynamic_cast<TssCombo *>(AfWnd::GetAfWnd(hwndCombo));
	return qtc->MainParent();
}

/*----------------------------------------------------------------------------------------------
	Return the control ID for the combo box.
----------------------------------------------------------------------------------------------*/
int TssComboEdit::Cid()
{
	HWND hwndCombo = ::GetParent(m_hwnd);
	TssComboPtr qtc = dynamic_cast<TssCombo *>(AfWnd::GetAfWnd(hwndCombo));
	return qtc->Cid();
}

/*----------------------------------------------------------------------------------------------
	Handle type ahead. This has to be in a separate message because if we try to do it as
	part of handling WM_CHAR, we don't see the change in the combo box. I don't know why.
----------------------------------------------------------------------------------------------*/
bool TssComboEdit::CmdTypeAhead(Cmd * pcmd)
{
	Assert(DoesTypeAhead());

	HWND hwndCombo = ::GetParent(::GetParent(m_hwnd));
#ifdef JohnT_Aug_1_01_StandardCombo
	// There are two versions of type-ahead here. Neither works, for different reasons.

	// This is the code we would need (not debugged) for a regular combo box.
	// It doesn't work because we have a TssCombo which understands different text messages.
	StrAppBuf strb;
	int cch = ::SendMessage(hwndCombo, WM_GETTEXT, strb.kcchMaxStr, (LPARAM)strb.Chars());
	strb.SetLength(cch);
	// See if there is an item beginning with that.
	int iitem = ::SendMessage(hwndCombo, CB_FINDSTRING, (uint)-1, (LPARAM)strb.Chars());
	if (iitem != CB_ERR)
	{
		// Retrieve the text of the complete item.
		achar rgch[MAX_PATH];
		Vector<achar> vch;
		achar * pszT;
		int cchFull = ::SendMessage(hwndCombo, CB_GETLBTEXTLEN, iitem, (long)0);
		if (cchFull < MAX_PATH)
		{
			pszT = rgch;
		}
		else
		{
			vch.Resize(cchFull + 1);
			pszT = vch.Begin();
		}
		cchFull = ::SendMessage(hwndCombo, CB_GETLBTEXT, iitem, (long)pszT);
		if (cchFull < 0)
			pszT = _T("");
		// Set it as the text of the edit box.
		::SendMessage(hwndCombo, WM_SETTEXT, 0, (LPARAM)pszT);
		// And select the part the user hasn't typed so his next keystroke will type over it.
		::SendMessage(hwndCombo, CB_SETEDITSEL, 0, MAKELONG(cch, -1));
	}
#endif

	// ---------------
	// Old comment apparently obsolete as of 29 Nov 2001:
	// This is for a TssCombo. I haven't time to figure why it doesn't work, but it seems
	// harmless, because of the third problem.
	// There are several problems:
	// 1. FW_CB_FINDSTRING always seems to return zero, whatever the input.
	// 2. FW_CB_GETLBTEXT returns unpredictable things. It seems to select the zeroth item
	// from any of the combo boxes in the toolbar. I haven't figured out the pattern.
	// For a while it seemed to like the last one (font size), but I've seen others, even
	// the right answer several times.
	// 3. FW_EM_SETTEXT does nothing at all, as far as I can determine.
	// 4. Since the text hasn't been changed, I can't tell whether CB_SETEDITSEL does anything.

	// Then, FW_CB_FINDSTRING doesn't seem to work; at least, it found "default paragraph
	// characters" when looking for "Normal".
	// Finally, trying to set the replacement string and selection seems to have no effect.
	// It's as though the system remembers what should have been produced by typing, but
	// delays actually doing it. Perhaps the typing produces queued messages to update
	// the box contents?
	// ---------------

	ITsStringPtr qtss;
	::SendMessage(hwndCombo, FW_CB_GETTEXT, 0, (LPARAM)(&qtss));
	int cch;
	CheckHr(qtss->get_Length(&cch));
	int cchMatch;

	// See if there is an item beginning with that.
	bool fStripFirstTwo = false; // true to strip off para mark
	int iitem = ::SendMessage(hwndCombo, FW_CB_FINDSTRING, (uint)-1, (long) qtss.Ptr());
	ITsStringPtr qtssStyle;
	if (iitem == CB_ERR)
	{
		// This part is currently only used for the Style combobox on the toolbar.
		HWND hwndStyleComboBox = ::GetDlgItem(::GetParent(hwndCombo), kcidFmttbStyle);
		if (hwndStyleComboBox)
		{
			AfMainWnd * pafw = MainWindow();
			if (!pafw)
				return true;
			// Prepare for it by updating the item list.
			pafw->OnStyleDropDown(hwndStyleComboBox);
			fStripFirstTwo = true;
			// For style combo, try prepending paragraph/character mark
			ITsStrBldrPtr qtpb;
			CheckHr(qtss->GetBldr(&qtpb));
			CheckHr(qtpb->ReplaceRgch(0, 0, L"\xb6 ", 2, NULL));
			CheckHr(qtpb->GetString(&qtssStyle));
			iitem = ::SendMessage(hwndCombo, FW_CB_FINDSTRING, (uint)-1, (long) qtssStyle.Ptr());
			if (iitem == CB_ERR)
			{
				CheckHr(qtpb->ReplaceRgch(0, 1, L"\xaa", 1, NULL));
				CheckHr(qtpb->GetString(&qtssStyle));
				iitem = ::SendMessage(hwndCombo, FW_CB_FINDSTRING, (uint)-1, (long) qtssStyle.Ptr());
			}
		}
	}
	if (iitem == CB_ERR)
	{
		int cchLastLength = 0;
		if (m_qtssLastText)
			CheckHr(m_qtssLastText->get_Length(&cchLastLength));
		// There's no point looking for the previous string in the list if it was empty,
		// so we just set the combobox to empty and return here.
		if (cchLastLength == 0)
		{
			::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)_T(""));
			return true;
		}
		// New text did not match. Try reverting to previous text.
		fStripFirstTwo = false;
		iitem = ::SendMessage(hwndCombo, FW_CB_FINDSTRINGEXACT, (uint)-1,
			(long)m_qtssLastText.Ptr());
		if (iitem == CB_ERR)
		{
			// For style combo, try prepending paragraph/character mark.
			fStripFirstTwo = true;
			ITsStrBldrPtr qtpb;
			if (m_qtssLastText)
				CheckHr(m_qtssLastText->GetBldr(&qtpb));
			else
				qtpb.CreateInstance(CLSID_TsStrBldr);
			CheckHr(qtpb->ReplaceRgch(0, 0, L"\xb6 ", 2, NULL));
			CheckHr(qtpb->GetString(&qtssStyle));
			iitem = ::SendMessage(hwndCombo, FW_CB_FINDSTRINGEXACT, (uint)-1,
				(long) qtssStyle.Ptr());
			if (iitem == CB_ERR)
			{
				CheckHr(qtpb->ReplaceRgch(0, 1, L"\xaa", 1, NULL));
				CheckHr(qtpb->GetString(&qtssStyle));
				iitem = ::SendMessage(hwndCombo, FW_CB_FINDSTRINGEXACT, (uint)-1,
					(long) qtssStyle.Ptr());
			}
			// The previous text should match!
			Assert(iitem != CB_ERR);
		}
		// Figure out how much of the part of the previous string matches the new.
		// That will be the part that will NOT be selected.
		cchMatch = 0;
		OLECHAR rgchThis[200];
		OLECHAR rgchLast[200];
		CheckHr(qtss->FetchChars(0, min(cch, 200), rgchThis));
		CheckHr(m_qtssLastText->FetchChars(0, min(cchLastLength, 200), rgchLast));
		while (cchMatch < cch && cchMatch < cchLastLength &&
			rgchThis[cchMatch] == rgchLast[cchMatch])
		{
			cchMatch++;
		}
	}
	else
	{
		cchMatch = cch;
	}

	// Retrieve the text of the complete item.
	::SendMessage(hwndCombo, FW_CB_GETLBTEXT, iitem, (LPARAM)&qtss);
	// Select it in the list, so that if the user types a down arrow he sees it. (Do this
	// before setting the text and selection).
	::SendMessage(hwndCombo, CB_SETCURSEL, iitem, 0);
	// Set it as the text of the edit box (that's this window), minus any extra characters
	// for the style.
	// Note that it's not a TssEdit, just a regular edit box, for now.
	SmartBstr sbstr;
	CheckHr(qtss->get_Text(&sbstr));
	StrAppBuf strb = sbstr.Chars() + (fStripFirstTwo ? 2 : 0);
	::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)strb.Chars());
	// And select the part the user hasn't typed so his next keystroke will type over it.
	::SendMessage(m_hwnd, EM_SETSEL, cchMatch, -1);

	TssComboExPtr qtce = dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwndCombo));
	Assert(qtce);
	qtce->OnSelChange(Cid(), hwndCombo);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle window messages.

	@param wm Windows message identifier.
	@param wp First message parameter.
	@param lp Second message parameter.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the message has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool TssComboEdit::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_KEYUP && wp == VK_MENU)
	{
		// This removes the character that is produced by an Alt+??? combination. The main
		// problem this fixes is when the user hits Alt+Up to open/close the combobox.
		// This normally translates into a backspace WM_CHAR message, which clears out the
		// contents of the combobox. This is bad. So to solve it, we get rid of the WM_CHAR
		// message.
		MSG msg;
		::PeekMessage(&msg, NULL, WM_CHAR, WM_CHAR, PM_REMOVE);
	}

//	HWND hwndCombo = ::GetParent(m_hwnd);
//	TssComboPtr qtce= dynamic_cast<TssCombo *>(AfWnd::GetAfWnd(hwndCombo));
	bool fTypeAhead = DoesTypeAhead();
	bool fToolTip = HasToolTip();

	if (fTypeAhead && (wm == WM_RBUTTONDOWN))
	{
		lnRet = true;
		return true;
	}

	if (fToolTip &&
		(wm == WM_LBUTTONDOWN || wm == WM_LBUTTONUP ||
			wm == WM_MBUTTONDOWN || wm == WM_MBUTTONUP || wm == WM_MOUSEMOVE ||
			wm == WM_RBUTTONDOWN || wm == WM_RBUTTONUP))
	{
		// Notify the tooltip belonging to the parent toolbar of the mouse message.
		Assert(m_hwndToolTip);
		MSG msg;
		msg.hwnd = ::GetParent(m_hwnd);
		msg.message = wm;
		msg.wParam = wp;
		msg.lParam = lp;
		::SendMessage(m_hwndToolTip, TTM_RELAYEVENT, 0, (LPARAM)&msg);
	}
	else if (wm == WM_SETFOCUS)
	{
		// We're getting the focus. Remember what was showing.
		HWND hwndCombo = ::GetParent(::GetParent(m_hwnd));
		TssComboEx * ptce = dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwndCombo));
		Assert(ptce);
		ptce->GetText(&m_qtssFocusText);
	}
#ifdef JohnT_Aug_1_01_ApplyOnLoseFocus
	// Analysts currently say they don't want anything the user typed applied unless he hits CR.
	// So there is nothing special to do on loss of focus.
	else if (wm == WM_KILLFOCUS)
	{
		// We're about to lose the focus, so apply the settings in the combo box,
		// if anything changed.
		ITsStringPtr qtssText;
		HWND hwndCombo = ::GetParent(::GetParent(m_hwnd));
		TssComboEx * ptce = dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwndCombo));
		Assert(ptce);
		ptce->GetText(&qtssText);
		// Treat as no change unless we have a prior string to compare with.
		ComBool fEqual = true;
		if (m_qtssFocusText)
			CheckHr(qtssText->Equals(m_qtssFocusText, &fEqual));
		if (!fEqual)
		{
			TssComboExPtr qtce = dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwndCombo));
			Assert(qtce && hwndCombo == qtce->Hwnd());
			HWND hwndToolBar = ::GetParent(hwndCombo);
			AfMainWnd * pafw = MainWindow();
			AssertPtr(pafw);
			AfVwRootSitePtr qvwnd;
			int grfvfs = kvfsNormal;
			// Review (SharonC): Do we want to add a false argument to prevent getting
			// any toolbar controls?
			if (pafw->GetActiveViewWindow(&qvwnd, &grfvfs) && grfvfs == kvfsNormal)
			{
				TBBUTTON tbb;
				::SendMessage(hwndToolBar, TB_GETBUTTON, qtce->GetButtonIndex(), (long)&tbb);
				qvwnd->ApplyFormatting(tbb.idCommand, hwndCombo);
			}
		}
	}
#endif
	else if (fTypeAhead && (wm == WM_KEYDOWN))
	{
		if (wp == VK_DELETE)
		{
			// The delete key shouldn't change what's selected at all.
			lnRet = true;
			return true;
		}
		// Save what was in the the combo box in m_qtssLastText
		HWND hwndCombo = ::GetParent(::GetParent(m_hwnd));
		::SendMessage(hwndCombo, FW_CB_GETTEXT, 0, (LPARAM)(&m_qtssLastText));
		if (wp == VK_LEFT || wp == VK_RIGHT)
		{
			lnRet = true;  // left or right arrow; ignore
			return true;
		}
	}
	else if (wm == WM_CHAR)
	{
		HWND hwndComboEx = ::GetParent(::GetParent(m_hwnd));
		TssComboExPtr qtce = dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwndComboEx));
		Assert(qtce && hwndComboEx == qtce->Hwnd());
//		HWND hwndParent = ::GetParent(hwndComboEx);

		NMHDR nmh;
		nmh.hwndFrom = hwndComboEx;
		nmh.idFrom = qtce->m_cid;
		nmh.code = WM_CHAR;
		if (wp == VK_TAB)  // '\t'
		{
			return qtce->OnCharTab(qtce->m_cid, &nmh, lnRet);
		}
		else if (wp == VK_RETURN) // '\r'
		{
			return qtce->OnCharEnter(qtce->m_cid, &nmh, lnRet);
		}
		else if (wp == VK_ESCAPE) // '\33'
		{
			return qtce->OnCharEscape(qtce->m_cid, &nmh, lnRet);
		}

		if (fTypeAhead && (wp == VK_BACK)) // '\010'
		{
			// For type-ahead we need to delete the extra stuff and one more char, if possible.
			DWORD ichMin, ichLim;
			::SendMessage(m_hwnd, EM_GETSEL, (WPARAM)(&ichMin), (LPARAM)(&ichLim));
			// The EM_SETSEL and EM_REPLACESEL messages were replaced by WM_GETTEXT and
			// WM_SETTEXT, because the latter pair seemed to work better with the
			// typeahead mechanism. --Sharon
			if (ichMin > 0)
			{
				ichMin--;
//					::SendMessage(m_hwnd, EM_SETSEL, ichMin, ichLim);
			}
//				// (FALSE prevents UNDO, L"0" works for both wide and narrow.)
//				::SendMessage(m_hwnd, EM_REPLACESEL, FALSE, (LPARAM)_T(""));
			achar rgch[MAX_PATH];
			::SendMessage(m_hwnd, WM_GETTEXT, MAX_PATH, (LPARAM)rgch);
			rgch[ichMin] = 0; // zero-terminate
			::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)rgch);
			::PostMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(kcidComboTypeAhead, 0), NULL);
			//return true;
		}
		else if (fTypeAhead)
		{
			// Some other keystroke, do type-ahead. First insert the character
			// the user typed.
			::PostMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(kcidComboTypeAhead, 0), NULL);
		}
	}

#ifdef JohnT_Aug_1_01_CTRL_DOWN_OPEN
	// PM no longer wants this, Windows 2000 uses ALT-down-arrow to open combo.
	else if (wm == WM_KEYDOWN && wp == VK_DOWN && ::GetKeyState(VK_CONTROL) < 0)
	{
		// For some reason we don't get a WM_CHAR here.
		HWND hwndCombo = ::GetParent(::GetParent(m_hwnd));
		::SendMessage(hwndCombo, CB_SHOWDROPDOWN, true, 0);

	}
#endif

	// If this is a type ahead combo, we don't want a mouse click or double-click to
	// change the selection from anything but the whole text in the combo box.
	if (fTypeAhead)
	{
		if (wm == WM_LBUTTONDOWN || wm == WM_LBUTTONDBLCLK ||
			wm == WM_MBUTTONDOWN || wm == WM_MBUTTONDBLCLK ||
			wm == WM_RBUTTONDOWN || wm == WM_RBUTTONDBLCLK)
		{
			::PostMessage(m_hwnd, EM_SETSEL, 0, (LPARAM) -1);
		}
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


//:>********************************************************************************************
//:>	TssCombo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Return true if the combo does type-ahead.
----------------------------------------------------------------------------------------------*/
bool TssCombo::DoesTypeAhead()
{
	HWND hwndComboEx = ::GetParent(m_hwnd);
	TssComboExPtr qtc = dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwndComboEx));
	return qtc->DoesTypeAhead();
}

/*----------------------------------------------------------------------------------------------
	Return true if the combo has a tool tip.
----------------------------------------------------------------------------------------------*/
bool TssCombo::HasToolTip()
{
	HWND hwndComboEx = ::GetParent(m_hwnd);
	TssComboExPtr qtc = dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwndComboEx));
	return qtc->HasToolTip();
}

/*----------------------------------------------------------------------------------------------
	Return the main tool bar or dialog that contains the combo box.
----------------------------------------------------------------------------------------------*/
HWND TssCombo::MainParent()
{
	HWND hwndComboEx = ::GetParent(m_hwnd);
	TssComboExPtr qtc = dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwndComboEx));
	return qtc->m_hwndParent;
}

/*----------------------------------------------------------------------------------------------
	Return the control ID for the combo box.
----------------------------------------------------------------------------------------------*/
int TssCombo::Cid()
{
	HWND hwndComboEx = ::GetParent(m_hwnd);
	TssComboExPtr qtc = dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwndComboEx));
	return qtc->Cid();
}

/*----------------------------------------------------------------------------------------------
	Handle notifications.

	@param ctid Identifier of the common control sending the message.
	@param pnmh Pointer to an NMHDR structure containing notification code and additional info.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool TssCombo::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(id, pnmh, lnRet))
		return true;

	bool fToolTip = HasToolTip();

	if (fToolTip && pnmh->code == TTN_POP)
	{
		// Wait 1/2 second after the tooltip disappears before resetting the text on the
		// status bar.
		::SetTimer(MainParent(), knToolTipTimer, 500, NULL);
		return true;
	}
	else if (fToolTip && pnmh->code == TTN_SHOW)
	{
		// This flag keeps the tooltip from recursively appearing and crashing the program.
		static bool s_fIgnore = false;
		if (!s_fIgnore)
		{
			// If another tooltip shows up in the 1/2 second time interval set above, cancel
			// the timer, so the status bar doesn't get changed back to the idle string.
			::KillTimer(MainParent(), knToolTipTimer);

			// Create a new notification message and forward it to the parent in order to get
			// the default response for a normal tooltip (which is currently defined in
			// AfMainWnd::OnNotifyChild).
			NMTTDISPINFO nmtdi;
			nmtdi.hdr.hwndFrom = (HWND)id;
			nmtdi.hdr.code = TTN_GETDISPINFO;
			nmtdi.hdr.idFrom = ::GetDlgCtrlID((HWND)id);
			*nmtdi.szText = 0;
			::SendMessage(::GetParent(m_hwnd), WM_NOTIFY, nmtdi.hdr.idFrom, (LPARAM)&nmtdi);

			// Update the status bar here rather than above after ::KillTimer() so that the
			// string for the new command is already set.
			AfMainWnd * pafw = MainWindow();
			AssertPtr(pafw);
			AfStatusBar * pstat = pafw->GetStatusBarWnd();
			if (pstat)
				pstat->DisplayHelpText();

			if (*nmtdi.szText)
			{
				// Now we have the text for the control, so update the text in the tooltip.
				TOOLINFO ti = { isizeof(ti) };
				ti.hwnd = (HWND)id;
				ti.uId = (uint)ti.hwnd;
				ti.lpszText = nmtdi.szText;
				::SendMessage(pnmh->hwndFrom, TTM_UPDATETIPTEXT, 0, (LPARAM)&ti);

				// This is required so the tooltip gets resized properly.
				s_fIgnore = true;
				::SendMessage(pnmh->hwndFrom, TTM_UPDATE, 0, 0);
				s_fIgnore = false;
				return true;
			}
		}
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle window messages.

	@param wm Windows message identifier.
	@param wp First message parameter.
	@param lp Second message parameter.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the message has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool TssCombo::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	bool fToolTip = HasToolTip();

	if (fToolTip &&
		(wm == WM_LBUTTONDOWN || wm == WM_LBUTTONUP ||
			wm == WM_MBUTTONDOWN || wm == WM_MBUTTONUP || wm == WM_MOUSEMOVE ||
			wm == WM_RBUTTONDOWN || wm == WM_RBUTTONUP))
	{
		// Notify the tooltip belonging to the parent toolbar of the mouse message.
		MSG msg;
		msg.hwnd = m_hwnd;
		msg.message = wm;
		msg.wParam = wp;
		msg.lParam = lp;
		HWND hwndToolTip = (HWND)::SendMessage(MainParent(), TB_GETTOOLTIPS, 0, 0);
		::SendMessage(hwndToolTip, TTM_RELAYEVENT, 0, (LPARAM)&msg);
	}
	else if (wm == WM_CHAR && wp == VK_TAB)
	{
		// Handle Tab key. This is called when Tab is pressed while the list is dropped down.
		HWND hwndComboEx = ::GetParent(m_hwnd);
		TssComboExPtr qtce = dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwndComboEx));
		Assert(qtce && hwndComboEx == qtce->Hwnd());
//		HWND hwndParent = ::GetParent(hwndComboEx);

		NMHDR nmh;
		nmh.hwndFrom = hwndComboEx;
		nmh.idFrom = qtce->m_cid;
		nmh.code = WM_CHAR;
		return qtce->OnCharTab(qtce->m_cid, &nmh, lnRet);
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

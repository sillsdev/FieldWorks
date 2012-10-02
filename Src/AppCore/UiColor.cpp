/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UiColor.cpp
Responsibility: Darrell Zook
Last reviewed: Not yet.

Description:
	This file contains class definitions for the following classes:
		UiColorCombo : AfWnd
		UiColorPopup : AfWnd
		UiToolTip : AfWnd
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


/***********************************************************************************************
	Implementation of class UiColorCombo
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
UiColorCombo::UiColorCombo()
{
	m_pclr = NULL;
	m_fPushed = false;
	m_fShowText = false;
	m_clrLabel = ::GetSysColor(COLOR_WINDOWTEXT);
}


/*----------------------------------------------------------------------------------------------
	Subclass an existing button window.
----------------------------------------------------------------------------------------------*/
void UiColorCombo::SubclassButton(HWND hwnd, COLORREF * pclr, bool fShowText)
{
	AssertPtr(pclr);
#ifdef DEBUG
	{ // Make sure the window is really a button.
		achar rgch[10];
		::GetClassName(hwnd, rgch, SizeOfArray(rgch));
		Assert(StrCmpI(rgch, _T("BUTTON")) == 0);
	}
#endif

	::SetWindowLong(hwnd, GWL_STYLE, ::GetWindowLong(hwnd, GWL_STYLE) | BS_OWNERDRAW);

	SuperClass::SubclassHwnd(hwnd);

	m_wid = ::GetDlgCtrlID(hwnd);
	Assert(m_wid);

	m_fShowText = fShowText;
	m_pclr = pclr;
	SetColor(*pclr);
	SetWindowSize();
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool UiColorCombo::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_GETDLGCODE)
	{
		lnRet = DLGC_BUTTON | DLGC_WANTARROWS;
		return true;
	}
	if (wm == WM_LBUTTONDOWN ||
		((wm == WM_KEYDOWN || wm == WM_SYSKEYDOWN) && (wp == VK_DOWN || wp == VK_UP)))
	{
		// Show the popup color chooser.
		RECT rc;
		::GetWindowRect(m_hwnd, &rc);

		::SetFocus(m_hwnd);

		POINT pt = { rc.left, rc.bottom };
		WndCreateStruct wcs;
		wcs.InitChild(_T("STATIC"), m_hwnd, m_wid);
		/*
		StrApp str;
		str.Format("m_hwnd=%x, rc.top=%d, rc.bottom=%d, rc.left=%d, rc.right=%d, m_wid=%d,
			cy=%d, cx=%d, y=%d, x=%d", m_hwnd, rc.top, rc.bottom, rc.left, rc.right, m_wid,
			wcs.cy, wcs.cx, wcs.y, wcs.x);
		::MessageBox(NULL, str.Chars(), "UiColorCombo::FWndProc", MB_OK); */
		::SendMessage(GetParent(m_hwnd), WM_COMMAND, MAKEWPARAM(m_wid, CBN_DROPDOWN),
			(LPARAM)m_hwnd);
		m_fPushed = true;
		UiColorPopupPtr qcop;
		qcop.Create();
		qcop->DoPopup(wcs, m_pclr, pt);
		m_fPushed = false;
	}
	if (wm == WM_KILLFOCUS)
	{
		::SendMessage(::GetParent(m_hwnd), WM_COMMAND, MAKEWPARAM(m_wid, CBN_KILLFOCUS),
			(LPARAM)m_hwnd);
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Convert notifications from the UiColorPopup into notifications for the parent of the
	UiColorCombo.
----------------------------------------------------------------------------------------------*/
bool UiColorCombo::OnCommand(int cid, int nc, HWND hctl)
{
	switch (nc)
	{
	case CBN_SELENDOK:
		return OnSelEndOK();
	case CBN_SELENDCANCEL:
		return OnSelEndCancel();
	case CBN_SELCHANGE:
		return OnSelChange();
	case CBN_KILLFOCUS:
		return OnKillFocus();
	default:
		return SuperClass::OnCommand(cid, nc, hctl);
	}
}


/*----------------------------------------------------------------------------------------------
	The CBN_SELENDOK notification message is sent when the user selects a list item, or selects
	an item and then closes the list. It indicates that the user's selection is to be processed.
----------------------------------------------------------------------------------------------*/
bool UiColorCombo::OnSelEndOK()
{
	HWND hwndParent = ::GetParent(m_hwnd);
	::SendMessage(hwndParent, WM_COMMAND, MAKEWPARAM(m_wid, CBN_CLOSEUP), (LPARAM)m_hwnd);
	::SendMessage(hwndParent, WM_COMMAND, MAKEWPARAM(m_wid, CBN_SELENDOK), (LPARAM)m_hwnd);
	return true;
}


/*----------------------------------------------------------------------------------------------
	The CBN_SELENDCANCEL notification message is sent when the user selects an item, but then
	selects another control or closes the dialog box. It indicates the user's initial
	selection is to be ignored.
----------------------------------------------------------------------------------------------*/
bool UiColorCombo::OnSelEndCancel()
{
	HWND hwndParent = ::GetParent(m_hwnd);
	::SendMessage(hwndParent, WM_COMMAND, MAKEWPARAM(m_wid, CBN_CLOSEUP), (LPARAM)m_hwnd);
	::SendMessage(hwndParent, WM_COMMAND, MAKEWPARAM(m_wid, CBN_SELENDCANCEL), (LPARAM)m_hwnd);
	return true;
}


/*----------------------------------------------------------------------------------------------
	The CBN_SELCHANGE notification message is sent when the user changes the current selection
	in the list box of a combo box. The user can change the selection by clicking in the list
	box or by using the arrow keys.
----------------------------------------------------------------------------------------------*/
bool UiColorCombo::OnSelChange()
{
	::SendMessage(::GetParent(m_hwnd), WM_COMMAND, MAKEWPARAM(m_wid, CBN_SELCHANGE),
		(LPARAM)m_hwnd);
	return true;
}


/*----------------------------------------------------------------------------------------------
	The CBN_KILLFOCUS notification message is sent when the combo-box loses focus.
----------------------------------------------------------------------------------------------*/
bool UiColorCombo::OnKillFocus()
{
	::SendMessage(::GetParent(m_hwnd), WM_COMMAND, MAKEWPARAM(m_wid, CBN_KILLFOCUS),
		(LPARAM)m_hwnd);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Draw the combo control.
----------------------------------------------------------------------------------------------*/
bool UiColorCombo::OnDrawThisItem(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);
	AssertPtr(m_pclr);

	HDC hdc = pdis->hDC;
	Rect rc(pdis->rcItem);
	UINT state = pdis->itemState;
	COLORREF clr = *m_pclr;

	SIZE sizeMargins = { ::GetSystemMetrics(SM_CXEDGE), ::GetSystemMetrics(SM_CYEDGE) };

	// Draw the down arrow.
	::DrawFrameControl(hdc, &m_rcArrowButton, DFC_SCROLL, DFCS_SCROLLDOWN |
		(m_fPushed || (state & ODS_SELECTED) ? DFCS_PUSHED : 0) |
		((state & ODS_DISABLED) ? DFCS_INACTIVE : 0));

	::DrawEdge(hdc, &rc, EDGE_SUNKEN, BF_RECT);

	// Must reduce the size of the "client" area of the button due to edge thickness.
	rc.Inflate(-sizeMargins.cx, -sizeMargins.cy);

	// Select and realize the palette.
	HPALETTE hpalOld = g_ct.RealizePalette(hdc);

	// Fill background.
	rc.right -= m_rcArrowButton.right - m_rcArrowButton.left;
	HPEN hpenOld = (HPEN)::SelectObject(hdc, ::GetStockObject(NULL_PEN));

	HBRUSH hbrBackground = AfGdi::CreateSolidBrush(((state & ODS_DISABLED) ||
		clr == CLR_DEFAULT) ? ::GetSysColor(COLOR_3DFACE) : ::GetSysColor(COLOR_WINDOW));

	HBRUSH hbrOld = AfGdi::SelectObjectBrush(hdc, hbrBackground);
	::Rectangle(hdc, rc.left, rc.top, rc.right + 1, rc.bottom + 1);

	// If knNinch or conflicting then leave control blank.
	if ((clr != (COLORREF)knNinch) && (clr != (COLORREF)knNinch + 1))
	{
		// Fill a small box with color.
		int nBoxHeight = rc.bottom - rc.top - 4;
		RECT rcBox = { rc.left + 2, rc.top + 2, rc.left + 2 + nBoxHeight, rc.bottom - 2 };
		if (!m_fShowText)
			::SetRect(&rcBox, rc.left + 2, rc.top + 2, rc.right - 2, rc.bottom - 2);

		COLORREF clr2 = clr;
		if (clr2 == kclrTransparent)
			clr2 = ::GetSysColor(COLOR_WINDOW);

		HBRUSH hbr = AfGdi::CreateSolidBrush(((state & ODS_DISABLED) ||
			clr2 == CLR_DEFAULT) ? ::GetSysColor(COLOR_3DFACE) :
				PALETTERGB(GetRValue(clr2), GetGValue(clr2), GetBValue(clr2)));

		HBRUSH hbrT = AfGdi::SelectObjectBrush(hdc, hbr);
		::Rectangle(hdc, rcBox.left, rcBox.top, rcBox.right, rcBox.bottom);
		AfGdi::SelectObjectBrush(hdc, hbrT);
		::FrameRect(hdc, &rcBox, (HBRUSH)::GetStockObject(BLACK_BRUSH));

		AfGdi::DeleteObjectBrush(hbr);

		if (hpalOld)
			::SelectPalette(hdc, hpalOld, false);

		if (m_fShowText)
		{
			// Write out the text to the right of the box
			COLORREF clrOldFore = ::SetTextColor(hdc, m_clrLabel);
			COLORREF clrOldBk = ::SetBkColor(hdc, ::GetSysColor(COLOR_WINDOW));

			if (state & ODS_DISABLED)
				::SetBkColor(hdc, GetSysColor(COLOR_3DFACE));

			Rect rcT(rcBox.right + 2, rcBox.top - 1, rc.right - 1, rcBox.bottom + 1);
			HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
			StrApp str(g_ct.GetColorRid(g_ct.GetIndexFromColor(clr)));
			::DrawText(hdc, str.Chars(), -1, &rcT, DT_LEFT);
			AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
			::SetBkColor(hdc, clrOldBk);
			::SetTextColor(hdc, clrOldFore);
		}

	}
	AfGdi::SelectObjectBrush(hdc, hbrOld, AfGdi::OLD);
	AfGdi::DeleteObjectBrush(hbrBackground);
	if (hpenOld)
		::SelectObject(hdc, hpenOld);

	// Draw focus rect
	if (state & ODS_FOCUS)
	{
		Rect rcT;
		rcT.Set(rc.left + 1, rc.top + 1, rc.right - 1, rc.bottom - 1);
		::DrawFocusRect(hdc, &rcT);
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Calculate the window's size.
----------------------------------------------------------------------------------------------*/
void UiColorCombo::SetWindowSize()
{
	// Get size dimensions of edges.
	SIZE sizeMargin = { ::GetSystemMetrics(SM_CXEDGE), ::GetSystemMetrics(SM_CYEDGE) };

	// Get size of dropdown arrow.
	// -- No, we want these buttons to be a constant size, so that they match the other
	// buttons in the dialog or whatever.
//	int dxsArrow = max(::GetSystemMetrics(SM_CXHTHUMB), 5 * sizeMargin.cx);
//	int dysArrow = max(::GetSystemMetrics(SM_CYVTHUMB), 5 * sizeMargin.cy);
	int dxsArrow = 16;
	int dysArrow = 16;
	int dzsArrow = max(dxsArrow, dysArrow);
	SIZE sizeArrow = { dzsArrow, dzsArrow };

	// Get the System default text height.
	TEXTMETRIC tm;
	HDC hdc = ::GetDC(m_hwnd);
	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
	::GetTextMetrics(hdc, &tm);
	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	int iSuccess;
	iSuccess = ::ReleaseDC(m_hwnd, hdc);
	Assert(iSuccess);

	// Get window size.
	Rect rc;
	::GetWindowRect(m_hwnd, &rc);
	::MapWindowPoints(NULL, ::GetParent(m_hwnd), (POINT *)&rc, 2);

	// Set window size at least as wide as 2 arrows, and as high as arrow + 2X margins
	// JT: add one pixel to font height to clear descenders and make it match standard
	// combo boxes, at least with standard fonts.
	int dxs = Max(rc.right - rc.left, 2 * sizeArrow.cx + 2 * sizeMargin.cx);
	int dys = Max(sizeArrow.cy, tm.tmHeight) + 1 + 2 * sizeMargin.cy;
	// Retain vertical position by adding the dys to the bottom of current rectangle.
	::MoveWindow(m_hwnd, rc.left, rc.bottom - dys, dxs, dys, true);

	// Get the new coords of this window
	::GetWindowRect(m_hwnd, &rc);
	::ScreenToClient(m_hwnd, (POINT *)&rc);
	::ScreenToClient(m_hwnd, ((POINT *)&rc) + 1);

	// Get the rect where the arrow goes, and convert to client coords.
	::SetRect(&m_rcArrowButton, rc.right - sizeArrow.cx - sizeMargin.cx,
		rc.top + sizeMargin.cy, rc.right - sizeMargin.cx, rc.bottom - sizeMargin.cy);
}


/***********************************************************************************************
	UiColorPopup class implementation.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
UiColorPopup::UiColorPopup()
{
	// Make sure that constants for the number of rows and columns match the color table size.
	Assert(krowMax * kcolMax - kcolMax + 1 == g_ct.Size());

	// Various initializations
	m_dzsBorder = ::GetSystemMetrics(SM_CXEDGE);
	m_iclrHot = -1;
	m_iclrOld = 0;
	m_pclr = NULL;
	m_clrOld = RGB(0, 0, 0);
	m_pt.x = m_pt.y = 0;
	m_fIgnoreButtonUp = true;
	m_dysNinchRow = 0;
	m_fCanceled = true;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
UiColorPopup::~UiColorPopup()
{
#ifdef TimP_2002_10_Invalid
	if (m_hwndToolTip)
	{
		::DestroyWindow(m_hwndToolTip);
		m_hwndToolTip = NULL;
	}
#endif
}


/*----------------------------------------------------------------------------------------------
	Show the popup (using a modal loop) at the given location. pclr will contain the selected
	color when this method returns. If the color selection is cancelled, pclr will contain
	the previous selected color.
	DoPopup returns false if the user cancels the color selection.
----------------------------------------------------------------------------------------------*/
bool UiColorPopup::DoPopup(WndCreateStruct & wcs, COLORREF * pclr, POINT pt)
{
	AssertPtr(pclr);

	m_pclr = pclr;
	m_pt = pt;
	SuperClass::CreateAndSubclassHwnd(wcs);

	m_clrOld = *m_pclr;

	UiToolTipPtr qwnd;
	qwnd.Create();
	m_hwndToolTip = qwnd->Create(m_hwnd);

	Rect rc;
	TOOLINFO ti = { isizeof(ti) };
	ti.hwnd = m_hwnd;
	ti.hinst = ModuleEntry::GetModuleHandle();
	for (int i = 0; i < g_ct.Size(); i++)
	{
		GetCellRect(i, rc);
		ti.lpszText = reinterpret_cast<achar *>(g_ct.GetColorRid(i));
		ti.rect = rc;
		::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);
	}

	// Find which cell corresponds to the initial color.
	m_iclrOld = g_ct.GetIndexFromColor(*m_pclr);
	m_fMouseDown = true;

	::SetCapture(m_hwnd);
	MSG msg;
	bool fContinue = true;
	// Start a modal message loop.
	while (fContinue && ::GetMessage(&msg, NULL, 0, 0))
	{
		if (msg.hwnd == m_hwnd)
		{
			::SendMessage(m_hwndToolTip, TTM_RELAYEVENT, 0, (LPARAM)&msg);
			switch (msg.message)
			{
			case WM_LBUTTONDOWN:
				m_fMouseDown = true;
				::InvalidateRect(m_hwnd, NULL, true);
				break;
			case WM_LBUTTONUP:
				fContinue = OnLButtonUp(msg.wParam, MakePoint(msg.lParam));
				break;
			case WM_PAINT:
				OnPaint();
				break;
			case WM_MOUSEMOVE:
				OnMouseMove(msg.wParam, LOWORD(msg.lParam), HIWORD(msg.lParam));
				break;
			case WM_KEYDOWN:
				fContinue = OnKeyDown(msg.wParam, LOWORD(msg.lParam));
				break;
			case WM_KILLFOCUS:
				fContinue = OnKillFocus((HWND)msg.wParam);
				break;
			default:
				DefWndProc(msg.message, msg.wParam, msg.lParam);
				break;
			}
		}
		else
		{
			// Dispatch the message the normal way.
			::TranslateMessage(&msg);
			::DispatchMessage(&msg);
		}
	}
	::ReleaseCapture();

	return !m_fCanceled;
}


/*----------------------------------------------------------------------------------------------
	Initialize member variables before the window is created.
----------------------------------------------------------------------------------------------*/
void UiColorPopup::PreCreateHwnd(CREATESTRUCT & cs)
{
	SuperClass::PreCreateHwnd(cs);

	// Retrieve the menu font's height and calculate the height of the row to display it.
	TEXTMETRIC tm;
	HDC hdc = ::GetDC(NULL);
	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
	GetTextMetrics(hdc, &tm);
	m_dysNinchRow = tm.tmHeight + 5 * m_dzsBorder;
	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	int iSuccess;
	iSuccess = ::ReleaseDC(NULL, hdc);
	Assert(iSuccess);

	// Calculate the popup window's width and height from subtracting the UpperLeft cells origin
	// from the LowerRight + 1 origin..
	Point ptUpperLeft, ptLowerRight;
	GetPtFromRowCol(ptUpperLeft, 0, 0);
	GetPtFromRowCol(ptLowerRight, krowMax, kcolMax);
	ptLowerRight.Offset(2 * m_dzsBorder, 2 * m_dzsBorder); // Leave room for the border
	ptLowerRight -= ptUpperLeft;
	cs.cx = ptLowerRight.x;
	cs.cy = ptLowerRight.y;

	cs.style &= ~WS_CHILD;
	cs.style |= WS_VISIBLE | WS_POPUP;
	m_wid = (UINT)cs.hMenu;
	Assert(m_wid);
	cs.hMenu = 0;
	// We store the requested parent for this popup, because it seems that Windows sets the
	// parent of a popup window to a top-level window, which means that messages could get
	// sent to the wrong window if we don't store it.
	m_hwndParent = cs.hwndParent;

	// Make sure the popup window will be visible on the work area.
	Rect rc(m_pt.x, m_pt.y, m_pt.x + cs.cx, m_pt.y + cs.cy);
	AfGfx::EnsureVisibleRect(rc);
	cs.x = rc.left;
	cs.y = rc.top;
}


/*----------------------------------------------------------------------------------------------
	Given an index into the color table, returns the 0-based row that the color is on
	in the popup menu.
----------------------------------------------------------------------------------------------*/
int UiColorPopup::GetRowFromColor(int iclr) const
{
	Assert(0 <= iclr && iclr < g_ct.Size());
	return (iclr + kcolMax - 1) / kcolMax;
}


/*----------------------------------------------------------------------------------------------
	Given an index into the color table, returns the 0-based column that the color is on
	in the popup menu.
----------------------------------------------------------------------------------------------*/
int UiColorPopup::GetColFromColor(int iclr) const
{
	Assert(0 <= iclr && iclr < g_ct.Size());
	if (iclr <= 0)
		return 0;
	return (iclr - 1) % kcolMax;
}


/*----------------------------------------------------------------------------------------------
	Give a point, determine what row of the popup would contain it.
----------------------------------------------------------------------------------------------*/
int UiColorPopup::GetRowFromPt(const Point & pt)
{
	long y;
	// Translate points to be relative raised window edge.
	y = pt.y - m_dzsBorder;
	if (y < m_dysNinchRow)
		return 0;

	return (y - m_dysNinchRow) / kdzsButton + 1; // Add one to account for NinchRow.
}


/*----------------------------------------------------------------------------------------------
	Given a point, determine what column of the popup would contain it.
----------------------------------------------------------------------------------------------*/
int UiColorPopup::GetColFromPt(const Point & pt)
{
	// Translate points to be relative raised window edge.
	return (pt.x - m_dzsBorder) / kdzsButton;
}


/*----------------------------------------------------------------------------------------------
	Calculate the upper left hand corner of the cell specified by row, col. This method does not
	restrict the value of row and column to allow for finding the point of a cell adjacent to
	a valid cell, (ie. finding the row + 1 and col + 1 point would be 1 greater then the lower
	right corner of the row, col cell which is the cells bounding rectangle.
----------------------------------------------------------------------------------------------*/
void UiColorPopup::GetPtFromRowCol(Point & pt, int row, int col)
{
	pt.Set(0,0);
	pt.Offset(m_dzsBorder, m_dzsBorder);
	if (row)
		pt.y += (m_dysNinchRow + (row - 1) * kdzsButton);

	pt.x += col * kdzsButton;
}


/*----------------------------------------------------------------------------------------------
	Given a row, column pair, returns the 0-based index into the color table.
----------------------------------------------------------------------------------------------*/
int UiColorPopup::GetTableIndexFromRowCol(int row, int col) const
{
	if (row < 0 || col < 0 || row >= krowMax || col >= kcolMax)
		return -1;

	if (row == 0)
		return 0;

	int iclr = (row - 1) * kcolMax + col + 1;
	if (iclr >= g_ct.Size())
		return -1;
	return iclr;
}


/*----------------------------------------------------------------------------------------------
	If an arrow key is pressed, then move the selection.
----------------------------------------------------------------------------------------------*/
bool UiColorPopup::OnKeyDown(int vk, int cact)
{
	switch (vk)
	{
	case VK_DOWN:
	case VK_UP:
	case VK_RIGHT:
	case VK_LEFT:
		if (m_iclrHot < 0)
		{
			ChangeSelection(0);
			return true;
		}
		break;

	case VK_ESCAPE:
		*m_pclr = m_clrOld;
		EndSelection(CBN_SELENDCANCEL);
		return false;

	case VK_RETURN:
	case VK_SPACE:
		EndSelection(CBN_SELENDOK);
		return false;

	default:
		return true;
	}

	int row = GetRowFromColor(m_iclrHot);
	int col = GetColFromColor(m_iclrHot);

	switch (vk)
	{
	case VK_DOWN:
		if (++row >= krowMax)
			row = 0;
		break;

	case VK_UP:
		if (--row < 0)
			row = krowMax - 1;
		if (row == 0)
			col = 0;
		break;

	case VK_RIGHT:
		if (row == 0)
			return true;
		if (++col >= kcolMax)
			col = 0;
		break;

	case VK_LEFT:
		if (row == 0)
			return true;
		if (--col < 0)
			col = kcolMax - 1;
		break;
	}

	ChangeSelection(GetTableIndexFromRowCol(row, col));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Draw the popup table
----------------------------------------------------------------------------------------------*/
void UiColorPopup::OnPaint()
{
	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps); // Get device context for painting.

	// Draw Color cells.
	for (int iclr = 0; iclr < g_ct.Size(); iclr++)
		DrawCell(hdc, iclr, iclr == m_iclrHot, iclr == m_iclrOld);

	// Draw raised window edge (ex-window style WS_EX_WINDOWEDGE is supposed to do this,
	// but for some reason it doesn't.
	Rect rc;
	GetClientRect(rc);
	::DrawEdge(hdc, &rc, EDGE_RAISED, BF_RECT);

	::EndPaint(m_hwnd, &ps);
}


/*----------------------------------------------------------------------------------------------
	Draw and individual color button cell
----------------------------------------------------------------------------------------------*/
void UiColorPopup::DrawCell(HDC hdc, int iclr, bool fHot, bool fOld)
{
	Rect rc;

	GetCellRect(iclr, rc);

	// Select and realize the color table's palette.
	HPALETTE hpalOld = g_ct.RealizePalette(hdc);

	COLORREF clrBackBtn;
	switch (((uint)fHot << 1) | (uint)fOld)
	{
	case 1: // old
		// REVIEW (JeffG) ShonK: This is not the right color. There must be some inbetween shade
		// of gray that is darker then COLOR_3DHIGHLIGHT
		clrBackBtn = ::GetSysColor(COLOR_3DLIGHT);
		if (clrBackBtn == ::GetSysColor(COLOR_3DFACE))
			clrBackBtn = ::GetSysColor(COLOR_3DHIGHLIGHT);
		AfGfx::FillSolidRect(hdc, rc, clrBackBtn, false);
		::DrawEdge(hdc, &rc, EDGE_SUNKEN, BF_RECT);
		break;
	case 2: // hot
		clrBackBtn = ::GetSysColor(COLOR_3DFACE);
		AfGfx::FillSolidRect(hdc, rc, clrBackBtn);
		::DrawEdge(hdc, &rc, m_fMouseDown ? EDGE_SUNKEN : EDGE_RAISED, BF_RECT);
		break;
	case 3: // hot and old
		clrBackBtn = ::GetSysColor(COLOR_3DFACE);
		AfGfx::FillSolidRect(hdc, rc, clrBackBtn);
		::DrawEdge(hdc, &rc, EDGE_SUNKEN, BF_RECT);
		break;
	default: // neither
		clrBackBtn = ::GetSysColor(COLOR_3DFACE);
		AfGfx::FillSolidRect(hdc, rc, clrBackBtn);
		break;
	}

	rc.Inflate(-m_dzsBorder - 1, -m_dzsBorder - 1);

	HBRUSH hbrOld;
	HPEN hpenOld = (HPEN)::SelectObject(hdc,
		::CreatePen(PS_SOLID, 1, ::GetSysColor(COLOR_3DSHADOW)));
	if (iclr == 0)
	{
		// Draw a rectangle
		hbrOld = AfGdi::SelectObjectBrush(hdc, AfGfx::CreateSolidBrush(clrBackBtn));
		::Rectangle(hdc, rc.left, rc.top, rc.right, rc.bottom);

		// Draw "None" text.
		StrAppBuf strbButtonText(kstidUnspecified);
		rc.Inflate(-1, -1);
		::SetBkMode(hdc, TRANSPARENT);
		HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
		::DrawText(hdc, strbButtonText.Chars(), -1, &rc, DT_SINGLELINE | DT_VCENTER | DT_CENTER);
		AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	}
	else
	{
		hbrOld = AfGdi::SelectObjectBrush(hdc,
			AfGdi::CreateSolidBrush(g_ct.GetColor(iclr) | 0x02000000));

		// Draw the cell Color
		::Rectangle(hdc, rc.left, rc.top, rc.right, rc.bottom);
	}

	// Restore DC and cleanup
	if (hpenOld)
		::DeleteObject(::SelectObject(hdc, hpenOld));
	if (hbrOld)
		AfGdi::DeleteObjectBrush(AfGdi::SelectObjectBrush(hdc, hbrOld, AfGdi::OLD));
	if (hpalOld)
		::SelectPalette(hdc, hpalOld, false);
}


/*----------------------------------------------------------------------------------------------
	Change selection as the mouse moves to a new color cell (button)
----------------------------------------------------------------------------------------------*/
void UiColorPopup::OnMouseMove(UINT nFlags, int xp, int yp)
{
	Point pt(xp, yp);

	if (m_fIgnoreButtonUp)
	{
		Point ptT(pt);
		Rect rc;
		::ClientToScreen(m_hwnd, &ptT);
		::GetWindowRect(m_hwnd, &rc);
		if (rc.Contains(ptT))
			m_fIgnoreButtonUp = false;
	}

	// Get the row and column from the point (GetTableIndexFromRowCol returns -1 if the mouse
	// if not over a button.
	int iclrNew = GetTableIndexFromRowCol(GetRowFromPt(pt), GetColFromPt(pt));

	// In range? If not, default and exit
	if (iclrNew < 0)
		return;

	// OK - we have the row and column of the current selection
	// Has the row/col selection changed? If yes, then redraw old and new cells.
	if (iclrNew != m_iclrHot)
		ChangeSelection(iclrNew);
}


/*----------------------------------------------------------------------------------------------
	End selection on LButtonUp
----------------------------------------------------------------------------------------------*/
bool UiColorPopup::OnLButtonUp(UINT nFlags, POINT pt)
{
	m_fMouseDown = false;

	if (m_fIgnoreButtonUp)
	{
		m_fIgnoreButtonUp = false;
		return true;
	}

	::ClientToScreen(m_hwnd, &pt);

	RECT rc;
	::GetWindowRect(m_hwnd, &rc);
	EndSelection(PtInRect(&rc, pt) ? CBN_SELENDOK : CBN_SELENDCANCEL);
	return false;
}


/*----------------------------------------------------------------------------------------------
	Gets the coordinates of the color cell given by (row,col)
----------------------------------------------------------------------------------------------*/
void UiColorPopup::GetCellRect(int iclr, Rect & rc)
{
	Assert(0 <= iclr && iclr < g_ct.Size());

	Point ptUpperLeft, ptLowerRight;
	int row = GetRowFromColor(iclr);
	int col = GetColFromColor(iclr);

	GetPtFromRowCol(ptUpperLeft, row, col);
	if (!row)
		GetPtFromRowCol(ptLowerRight, row + 1, kcolMax);
	else
		GetPtFromRowCol(ptLowerRight, row + 1, col + 1);

	//ptLowerRight.Offset(-1, -1); // shrink by 1 the point of the next row and col cell.
	rc.Set(ptUpperLeft.x, ptUpperLeft.y, ptLowerRight.x, ptLowerRight.y);
}


/*----------------------------------------------------------------------------------------------
	Change to a new cell, redrawing both old and new to indicate the change.
----------------------------------------------------------------------------------------------*/
void UiColorPopup::ChangeSelection(int iclr)
{
	Assert(0 <= iclr && iclr < g_ct.Size());

	HDC hdc = ::GetDC(m_hwnd);

	if (iclr == m_iclrHot)
		return;

	// Draw the old selection (that we're about to change).
	if (m_iclrHot >= 0)
		DrawCell(hdc, m_iclrHot, false, m_iclrOld == m_iclrHot);

	// Set the current selection as row/col and draw as selected.
	m_iclrHot = iclr;
	DrawCell(hdc, m_iclrHot, true, m_iclrOld == m_iclrHot);

	// Store the current Color
	*m_pclr = g_ct.GetColor(m_iclrHot);
	::SendMessage(m_hwndParent, WM_COMMAND, MAKEWPARAM(m_wid, CBN_SELCHANGE), (LPARAM)m_hwnd);
	int iSuccess;
	iSuccess = ::ReleaseDC(m_hwnd, hdc);
	Assert(iSuccess);
}


/*----------------------------------------------------------------------------------------------
	Process the end of the popup window.
----------------------------------------------------------------------------------------------*/
void UiColorPopup::EndSelection(int nMessage)
{
	if (nMessage == CBN_SELENDCANCEL)
		*m_pclr = m_clrOld;
	else
		m_fCanceled = false;

	::SendMessage(m_hwndParent, WM_COMMAND, MAKEWPARAM(m_wid, nMessage), (LPARAM)m_hwnd);

	::PostMessage(m_hwnd, WM_CLOSE, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Redraw if the palette has changed.
----------------------------------------------------------------------------------------------*/
bool UiColorPopup::OnQueryNewPalette()
{
	::InvalidateRect(m_hwnd, NULL, TRUE);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Redraw if the palette has changed.
----------------------------------------------------------------------------------------------*/
void UiColorPopup::OnPaletteChanged(HWND hwndFocus)
{
	if (hwndFocus == m_hwnd)
		::InvalidateRect(m_hwnd, NULL, TRUE);
}


/*----------------------------------------------------------------------------------------------
	Return the mouse to normal operation now that the window is disappearing.
----------------------------------------------------------------------------------------------*/
bool UiColorPopup::OnKillFocus(HWND hwndNew)
{
	::ReleaseCapture();
	return false;
}


/***********************************************************************************************
	UiToolTip methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Create the tooltip control.
----------------------------------------------------------------------------------------------*/
HWND UiToolTip::Create(HWND hwndParent)
{
	// Create and subclass the tooltip control.
	HWND hwnd = ::CreateWindowEx(0, TOOLTIPS_CLASS, NULL, WS_POPUP | TTS_NOPREFIX |
		TTS_ALWAYSTIP, 0, 0, 0, 0, hwndParent, NULL, 0, NULL);
	if (!hwnd)
		ThrowHr(WarnHr(E_FAIL));
	SubclassHwnd(hwnd);

	m_hwndParent = hwndParent;
	return hwnd;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool UiToolTip::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == TTM_WINDOWFROMPOINT)
	{
		lnRet = (long)m_hwndParent;
		return true;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: IconCombo.cpp
Responsibility: Darrell Zook
Last reviewed: Not yet.

Description:
	This file contains class definitions for the following classes:
		IconComboCombo : AfWnd
		IconComboPopup : AfWnd
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


/***********************************************************************************************
	Implementation of class IconComboCombo
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
IconComboCombo::IconComboCombo()
{
	m_pival = NULL;
	m_fPushed = false;
	m_fShowText = false;
}


/*----------------------------------------------------------------------------------------------
	Subclass an existing button window.
----------------------------------------------------------------------------------------------*/
void IconComboCombo::SubclassButton(HWND hwnd, int * pval, bool fShowText)
{
	AssertPtr(pval);
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
	m_pival = pval;
	SetVal(*pval);
	SetWindowSize();
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool IconComboCombo::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_GETDLGCODE)
	{
		lnRet = DLGC_BUTTON | DLGC_WANTARROWS;
		return true;
	}
	if (wm == WM_LBUTTONDOWN ||
		((wm == WM_KEYDOWN || wm == WM_SYSKEYDOWN) && (wp == VK_DOWN || wp == VK_UP)))
	{
		// Show the popup icon chooser.
		RECT rc;
		::GetWindowRect(m_hwnd, &rc);

		::SetFocus(m_hwnd);

		//POINT pt = { rc.left, rc.bottom };
		WndCreateStruct wcs;
		wcs.InitChild(_T("STATIC"), m_hwnd, m_wid);
		::SendMessage(GetParent(m_hwnd), WM_COMMAND, MAKEWPARAM(m_wid, CBN_DROPDOWN),
			(LPARAM)m_hwnd);

		m_fPushed = true;
		IconComboPopupPtr qcop;
		qcop.Create();
// Need more args if we implement this fully.
//		qcop->DoPopup(wcs, m_pival, pt);
		m_fPushed = false;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Convert notifications from the IconComboPopup into notifications for the parent of the
	IconComboCombo.
----------------------------------------------------------------------------------------------*/
bool IconComboCombo::OnCommand(int cid, int nc, HWND hctl)
{
	switch (nc)
	{
	case CBN_SELENDOK:
		return OnSelEndOK();
	case CBN_SELENDCANCEL:
		return OnSelEndCancel();
	case CBN_SELCHANGE:
		return OnSelChange();
	default:
		return SuperClass::OnCommand(cid, nc, hctl);
	}
}


/*----------------------------------------------------------------------------------------------
	The CBN_SELENDOK notification message is sent when the user selects a list item, or selects
	an item and then closes the list. It indicates that the user's selection is to be processed.
----------------------------------------------------------------------------------------------*/
bool IconComboCombo::OnSelEndOK()
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
bool IconComboCombo::OnSelEndCancel()
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
bool IconComboCombo::OnSelChange()
{
	::SendMessage(::GetParent(m_hwnd), WM_COMMAND, MAKEWPARAM(m_wid, CBN_SELCHANGE),
		(LPARAM)m_hwnd);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Draw the combo control.
----------------------------------------------------------------------------------------------*/
bool IconComboCombo::OnDrawThisItem(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);
	AssertPtr(m_pival);

#ifdef JT_4_3_01_IMPLEMENTED
	HDC hdc = pdis->hDC;
	Rect rc(pdis->rcItem);
	UINT state = pdis->itemState;
	int ival = *m_pival;

	SIZE sizeMargins = { ::GetSystemMetrics(SM_CXEDGE), ::GetSystemMetrics(SM_CYEDGE) };

	// Draw the down arrow.
	::DrawFrameControl(hdc, &m_rcArrowButton, DFC_SCROLL, DFCS_SCROLLDOWN |
		(m_fPushed || (state & ODS_SELECTED) ? DFCS_PUSHED : 0) |
		((state & ODS_DISABLED) ? DFCS_INACTIVE : 0));

	::DrawEdge(hdc, &rc, EDGE_SUNKEN, BF_RECT);

	// Must reduce the size of the "client" area of the button due to edge thickness.
	rc.Inflate(-sizeMargins.cx, -sizeMargins.cy);

// This is old code from UiColor.cpp. It may be some help if we get around to implementing
// this control fully.

	// Select and realize the palette.
	HPALETTE hpalOld = g_ct.RealizePalette(hdc);

	// Fill background.
	rc.right -= m_rcArrowButton.right - m_rcArrowButton.left;
	HPEN hpenOld = (HPEN)::SelectObject(hdc, ::GetStockObject(NULL_PEN));

//	HBRUSH hbrBackground = AfGdi::CreateSolidBrush(((state & ODS_DISABLED) ||
//		clr == CLR_DEFAULT) ? ::GetSysColor(COLOR_3DFACE) : RGB(255, 255, 255));
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
			//			clr2 = kclrWhite;
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
			//COLORREF clrOldBk = ::SetBkColor(hdc, RGB(255,255,255));
			COLORREF clrOldBk = ::SetBkColor(hdc, ::GetSysColor(COLOR_WINDOW));
			Rect rcT(rcBox.right + 2, rcBox.top - 1, rc.right - 1, rcBox.bottom + 1);
			HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
			StrAnsi sta(g_ct.GetColorRid(g_ct.GetIndexFromColor(clr)));
			::DrawText(hdc, sta.Chars(), -1, &rcT, DT_LEFT);
			AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
			::SetBkColor(hdc, clrOldBk);
		}

	}
	if (hpenOld)
		::SelectObject(hdc, hpenOld);
	AfGdi::SelectObjectBrush(hdc, hbrOld, AfGdi::OLD);
	AfGdi::DeleteObjectBrush(hbrBackground);

	// Draw focus rect
	if (state & ODS_FOCUS)
	{
		Rect rcT;
		rcT.Set(rc.left + 1, rc.top + 1, rc.right - 1, rc.bottom - 1);
		::DrawFocusRect(hdc, &rcT);
	}

#endif JT_4_3_01_IMPLEMENTED
	return true;
}


/*----------------------------------------------------------------------------------------------
	Calculate the window's size.
----------------------------------------------------------------------------------------------*/
void IconComboCombo::SetWindowSize()
{
	// Get size dimensions of edges.
	SIZE sizeMargin = { ::GetSystemMetrics(SM_CXEDGE), ::GetSystemMetrics(SM_CYEDGE) };

	// Get size of dropdown arrow.
	int dxsArrow = max(::GetSystemMetrics(SM_CXHTHUMB), 5 * sizeMargin.cx);
	int dysArrow = max(::GetSystemMetrics(SM_CYVTHUMB), 5 * sizeMargin.cy);
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
	IconComboPopup class implementation.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
IconComboPopup::IconComboPopup()
{
	// Make sure that constants for the number of rows and columns match the color table size.

	// Various initializations
	m_dzsBorder = ::GetSystemMetrics(SM_CXEDGE) + 1;
	m_dxsButton = kdxsIcon + (m_dzsBorder * 2) + 1;
	m_dysButton = kdysIcon + (m_dzsBorder * 2) + 1;
	m_ivalHot = -1;
	m_ivalOld = 0;
	m_pival = NULL;
	m_pt.x = m_pt.y = 0;
	m_fIgnoreButtonUp = true;
	m_fCanceled = true;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
IconComboPopup::~IconComboPopup()
{
	if (m_hwndToolTip)
		::DestroyWindow(m_hwndToolTip);
}


/*----------------------------------------------------------------------------------------------
	Show the popup (using a modal loop) at the given location. *pival will contain the selected
	icon index when this method returns. If the icon selection is cancelled, pival will contain
	the previous selected index (the initial value of *pival).
	DoPopup returns false if the user cancels the color selection.
	@param cval number of buttons
	@param rid resource id of bitmap containing buttons. They are assumed to be 16 pixels wide
	and 15 high, as in a toolbar. Pink pixels are to be masked out.
	It is further assumed that rids from rid + 1 to rid + cval identify tooltip help for the
	buttons.
	@param prgfPressed is the button pressed
----------------------------------------------------------------------------------------------*/
bool IconComboPopup::DoPopup(WndCreateStruct & wcs, int * pival, POINT pt, int cval, int rid,
	int cColumns, bool * prgfPressed, HIMAGELIST himl)
{
	AssertPtr(pival);

	m_cvals = cval;
	m_pival = pival;
	m_pt = pt;
	m_cColumns = cColumns;
	m_cRows = (cval + cColumns - 1) / cColumns;
	m_himl = himl;
	m_prgfPressed = prgfPressed;

	SuperClass::CreateAndSubclassHwnd(wcs);

	m_ivalOld = *m_pival;

	// Tooltip stuff
	UiToolTipPtr qwnd;
	qwnd.Create();
	m_hwndToolTip = qwnd->Create(m_hwnd);

	Rect rc;
	TOOLINFO ti = { isizeof(ti) };
	ti.hwnd = m_hwnd;
	ti.hinst = ModuleEntry::GetModuleHandle();
	for (int i = 0; i < cval; i++)
	{
		GetCellRect(i, rc);
		ti.lpszText = reinterpret_cast<achar *>(rid + i + 1);
		ti.rect = rc;
		::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);
	}

	// Find which cell corresponds to the initial color.
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
void IconComboPopup::PreCreateHwnd(CREATESTRUCT & cs)
{
	SuperClass::PreCreateHwnd(cs);

	// Calculate the popup window's width and height
	Point ptUpperLeft, ptLowerRight;
	GetPtFromRowCol(ptUpperLeft, 0, 0);
	GetPtFromRowCol(ptLowerRight, m_cRows, m_cColumns);
	ptLowerRight.Offset(2 * m_dzsBorder, 2 * m_dzsBorder); // Leave room for the border
	ptLowerRight -= ptUpperLeft;  // Convert the point into a height and width.
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
	Given an index into the icon list, returns the 0-based row that the icon is on
	in the popup menu.
----------------------------------------------------------------------------------------------*/
int IconComboPopup::GetRowFromColor(int ival) const
{
	Assert(0 <= ival && ival < m_cvals);
	return ival / m_cColumns;
}


/*----------------------------------------------------------------------------------------------
	Given an index into the icon list, returns the 0-based column that the icon is on
	in the popup menu.
----------------------------------------------------------------------------------------------*/
int IconComboPopup::GetColFromColor(int ival) const
{
	Assert(0 <= ival && ival < m_cvals);
	if (ival <= 0)
		return 0;
	return (ival) % m_cColumns;
}


/*----------------------------------------------------------------------------------------------
	Give a point, determine what row of the popup would contain it.
----------------------------------------------------------------------------------------------*/
int IconComboPopup::GetRowFromPt(const Point & pt)
{
	long y;
	// Translate points to be relative raised window edge.
	y = pt.y - m_dzsBorder;
	if (y < 0)
		return 0;

	return y / m_dysButton;
}


/*----------------------------------------------------------------------------------------------
	Given a point, determine what column of the popup would contain it.
----------------------------------------------------------------------------------------------*/
int IconComboPopup::GetColFromPt(const Point & pt)
{
	// Translate points to be relative raised window edge.
	return (pt.x - m_dzsBorder) / m_dxsButton;
}


/*----------------------------------------------------------------------------------------------
	Calculate the upper left hand corner of the cell specified by row, col. This method does not
	restrict the value of row and column to allow for finding the point of a cell adjacent to
	a valid cell, (ie. finding the row + 1 and col + 1 point would be 1 greater then the lower
	right corner of the row, col cell which is the cells bounding rectangle.
----------------------------------------------------------------------------------------------*/
void IconComboPopup::GetPtFromRowCol(Point & pt, int row, int col)
{
	pt.Set(0,0);
	pt.Offset(m_dzsBorder, m_dzsBorder);
	if (row)
		pt.y += ((row) * m_dysButton);

	pt.x += col * m_dxsButton;
}


/*----------------------------------------------------------------------------------------------
	Given a row, column pair, returns the 0-based index into the icon list.
----------------------------------------------------------------------------------------------*/
int IconComboPopup::GetTableIndexFromRowCol(int row, int col) const
{
	if (row < 0 || col < 0 || row >= m_cRows || col >= m_cColumns)
		return -1;

	int ival = (row) * m_cColumns + col;
	if (ival >= m_cvals)
		return -1;
	return ival;
}


/*----------------------------------------------------------------------------------------------
	If an arrow key is pressed, then move the selection.
----------------------------------------------------------------------------------------------*/
bool IconComboPopup::OnKeyDown(int vk, int cact)
{
	switch (vk)
	{
	case VK_DOWN:
	case VK_UP:
	case VK_RIGHT:
	case VK_LEFT:
		if (m_ivalHot < 0)
		{
			ChangeSelection(0);
			return true;
		}
		break;

	case VK_ESCAPE:
		*m_pival = m_ivalOld;
		EndSelection(CBN_SELENDCANCEL);
		return false;

	case VK_RETURN:
	case VK_SPACE:
		EndSelection(CBN_SELENDOK);
		return false;

	default:
		return true;
	}

	int row = GetRowFromColor(m_ivalHot);
	int col = GetColFromColor(m_ivalHot);

	switch (vk)
	{
	case VK_DOWN:
		if (++row >= m_cRows)
			row = 0;
		break;

	case VK_UP:
		if (--row < 0)
			row = m_cRows - 1;
		if (row == 0)
			col = 0;
		break;

	case VK_RIGHT:
		if (row == 0)
			return true;
		if (++col >= m_cColumns)
			col = 0;
		break;

	case VK_LEFT:
		if (row == 0)
			return true;
		if (--col < 0)
			col = m_cColumns - 1;
		break;
	}

	ChangeSelection(GetTableIndexFromRowCol(row, col));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Draw the popup table
----------------------------------------------------------------------------------------------*/
void IconComboPopup::OnPaint()
{
	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps); // Get device context for painting.

	// Draw Color cells.
	for (int ival = 0; ival < m_cvals; ival++)
		DrawCell(hdc, ival, ival == m_ivalHot, m_prgfPressed[ival]);

	// Draw raised window edge (ex-window style WS_EX_WINDOWEDGE is supposed to do this,
	// but for some reason it doesn't.
	Rect rc;
	GetClientRect(rc);
	::DrawEdge(hdc, &rc, EDGE_RAISED, BF_RECT);

	::EndPaint(m_hwnd, &ps);
}


/*----------------------------------------------------------------------------------------------
	Draw an individualbutton cell
----------------------------------------------------------------------------------------------*/
void IconComboPopup::DrawCell(HDC hdc, int ival, bool fHot, bool fOld)
{
	Rect rc;

	GetCellRect(ival, rc);

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
		::DrawEdge(hdc, &rc, EDGE_ETCHED, BF_RECT);
		break;
	case 2: // hot
		clrBackBtn = ::GetSysColor(COLOR_3DFACE);
		AfGfx::FillSolidRect(hdc, rc, clrBackBtn);
		::DrawEdge(hdc, &rc, m_fMouseDown ? EDGE_SUNKEN : EDGE_RAISED, BF_RECT);
		break;
	case 3: // hot and old
		clrBackBtn = ::GetSysColor(COLOR_3DFACE);
		AfGfx::FillSolidRect(hdc, rc, clrBackBtn);
		::DrawEdge(hdc, &rc, EDGE_ETCHED, BF_RECT);
		break;
	default: // neither
		clrBackBtn = ::GetSysColor(COLOR_3DFACE);
		AfGfx::FillSolidRect(hdc, rc, clrBackBtn);
		break;
	}

	rc.Inflate(-m_dzsBorder - 1, -m_dzsBorder - 1);

	::ImageList_Draw(m_himl, ival, hdc, rc.left, rc.top, ILD_TRANSPARENT);
}


/*----------------------------------------------------------------------------------------------
	Change selection as the mouse moves to a new color cell (button)
----------------------------------------------------------------------------------------------*/
void IconComboPopup::OnMouseMove(UINT nFlags, int xp, int yp)
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
	int ivalNew = GetTableIndexFromRowCol(GetRowFromPt(pt), GetColFromPt(pt));

	// In range? If not, default and exit
	if (ivalNew < 0)
		return;

	// OK - we have the row and column of the current selection
	// Has the row/col selection changed? If yes, then redraw old and new cells.
	if (ivalNew != m_ivalHot)
		ChangeSelection(ivalNew);
}


/*----------------------------------------------------------------------------------------------
	End selection on LButtonUp
----------------------------------------------------------------------------------------------*/
bool IconComboPopup::OnLButtonUp(UINT nFlags, POINT pt)
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
	Gets the coordinates of the icon cell given by (row,col)
----------------------------------------------------------------------------------------------*/
void IconComboPopup::GetCellRect(int ival, Rect & rc)
{
	Assert(0 <= ival && ival < m_cvals);

	Point ptUpperLeft, ptLowerRight;
	int row = GetRowFromColor(ival);
	int col = GetColFromColor(ival);

	GetPtFromRowCol(ptUpperLeft, row, col);
	// millers: I don't know why 2 needs to be added to the row and col below, but that's
	// the way it works. Otherwise there is not enough border to the bottom and right of
	// of the buttons.
	GetPtFromRowCol(ptLowerRight, row + 1, col + 1);

	//ptLowerRight.Offset(-1, -1); // shrink by 1 the point of the next row and col cell.
	rc.Set(ptUpperLeft.x, ptUpperLeft.y, ptLowerRight.x, ptLowerRight.y);
}


/*----------------------------------------------------------------------------------------------
	Change to a new cell, redrawing both old and new to indicate the change.
----------------------------------------------------------------------------------------------*/
void IconComboPopup::ChangeSelection(int ival)
{
	Assert(0 <= ival && ival < m_cvals);
	if (ival == m_ivalHot)
		return;

	HDC hdc = ::GetDC(m_hwnd);

	// Draw the old selection (that we're about to change).
	if (m_ivalHot >= 0)
		DrawCell(hdc, m_ivalHot, false, m_prgfPressed[m_ivalHot]);

	// Set the current selection as row/col and draw as selected.
	m_ivalHot = ival;
	DrawCell(hdc, m_ivalHot, true, m_prgfPressed[m_ivalHot]);

	// Store the current Color
	*m_pival = m_ivalHot;
	::SendMessage(m_hwndParent, WM_COMMAND, MAKEWPARAM(m_wid, CBN_SELCHANGE), (LPARAM)m_hwnd);
	int iSuccess;
	iSuccess = ::ReleaseDC(m_hwnd, hdc);
	Assert(iSuccess);
}


/*----------------------------------------------------------------------------------------------
	Process the end of the popup window.
----------------------------------------------------------------------------------------------*/
void IconComboPopup::EndSelection(int nMessage)
{
	if (nMessage == CBN_SELENDCANCEL)
		*m_pival = m_ivalOld;
	else
		m_fCanceled = false;

	::SendMessage(m_hwndParent, WM_COMMAND, MAKEWPARAM(m_wid, nMessage), (LPARAM)m_hwnd);

	::PostMessage(m_hwnd, WM_CLOSE, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Redraw if the palette has changed.
----------------------------------------------------------------------------------------------*/
bool IconComboPopup::OnQueryNewPalette()
{
	::InvalidateRect(m_hwnd, NULL, TRUE);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Redraw if the palette has changed.
----------------------------------------------------------------------------------------------*/
void IconComboPopup::OnPaletteChanged(HWND hwndFocus)
{
	if (hwndFocus == m_hwnd)
		::InvalidateRect(m_hwnd, NULL, TRUE);
}


/*----------------------------------------------------------------------------------------------
	Return the mouse to normal operation now that the window is disappearing.
----------------------------------------------------------------------------------------------*/
bool IconComboPopup::OnKillFocus(HWND hwndNew)
{
	::ReleaseCapture();
	return false;
}

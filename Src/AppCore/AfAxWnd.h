/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfAxWnd.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This file creates classes which support window classes that conform to both the AfWnd
	conventions and those of windows created using ATL 3.0. We do this by creating a new
	base class to replace CWindow (and ATL class) for each AfWnd subclass we want to use
	as the top-level window in an ActiveX control. For example, to make a control based
	on AfWnd itself, we use the declaration in this file:

	class AfAxWnd : public AfAxWndTempl<AfWnd>;

	Inheritance from AfAxWndTempl basically generates code almost identical to ATL 3.0
	CWindow, so that all the ATL classes inheriting from AfAxWnd will have all the
	expected methods; but it also inherits from AfWnd so that AfWnd-based code finds
	what it expects, too.

	You should then make a declaration of a class which will function in place of
	CComControl as a base class for your control, for example:

	template <class TBase>
	class AfAtlControl : public CComControl<TBase, CWindowImpl<TBase, AfAxWnd> >

	You then substitute this class for the Wizard-generated CComControl in the
	actual declaration of your control, for example:

	class ATL_NO_VTABLE CDeView :
		...
		public MyAtlControl<CDeView>, // replaces CComControl(CDeView)
		...

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFAXWND_INCLUDED
#define AFAXWND_INCLUDED 1

template <class AfWndBase>
class AfAxWndTempl : public AfWndBase
{
public:
	static RECT rcDefault;
	// ATL subclasses (inheriting from AfAxWnd) insist on inheriting a variable
	// m_hWnd. It is the same as our m_hWnd (inherited from AfWnd), but unfortunately
	// we need both.
	// ENHANCE JohnT: could we just violate our convention and change the spelling in AfWnd?
	HWND m_hWnd;

	AfAxWndTempl(HWND hWnd = NULL)
		: AfWndBase()
	{
		m_hWnd = hWnd;
	}

	AfAxWndTempl& operator=(HWND hWnd)
	{
		m_hWnd = hWnd;
		m_hwnd = hWnd; // just to be safe
		return *this;
	}

	static LPCTSTR GetWndClassName()
	{
		return NULL;
	}

	void Attach(HWND hWndNew)
	{
		ATLASSERT(::IsWindow(hWndNew));
		m_hWnd = hWndNew;
		m_hwnd = hWndNew; // for safety
	}

	HWND Detach()
	{
		HWND hWnd = m_hWnd;
		m_hWnd = NULL;
		DetachHwnd(hWnd); // AfWnd equivalent
		return hWnd;
	}

	HWND Create(LPCTSTR lpstrWndClass, HWND hWndParent, RECT& rcPos, LPCTSTR szWindowName = NULL,
			DWORD dwStyle = 0, DWORD dwExStyle = 0,
			UINT nID = 0, LPVOID lpCreateParam = NULL)
	{
		m_hWnd = ::CreateWindowEx(dwExStyle, lpstrWndClass, szWindowName,
			dwStyle, rcPos.left, rcPos.top, rcPos.right - rcPos.left,
			rcPos.bottom - rcPos.top, hWndParent, (HMENU)nID,
			_Module.GetModuleInstance(), lpCreateParam);
		// m_hwnd should get set as a side effect of WM_CREATE
		return m_hWnd;
	}

	HWND Create(LPCTSTR lpstrWndClass, HWND hWndParent, LPRECT lpRect = NULL, LPCTSTR szWindowName = NULL,
			DWORD dwStyle = 0, DWORD dwExStyle = 0,
			HMENU hMenu = NULL, LPVOID lpCreateParam = NULL)
	{
		if (lpRect == NULL)
			lpRect = &rcDefault;
		m_hWnd = ::CreateWindowEx(dwExStyle, lpstrWndClass, szWindowName,
			dwStyle, lpRect->left, lpRect->top, lpRect->right - lpRect->left,
			lpRect->bottom - lpRect->top, hWndParent, hMenu,
			_Module.GetModuleInstance(), lpCreateParam);
		return m_hWnd;
	}

	BOOL DestroyWindow()
	{
		ATLASSERT(::IsWindow(m_hWnd));

		if (!::DestroyWindow(m_hWnd))
			return FALSE;

		m_hWnd = NULL;
		return TRUE;
	}

// Attributes

	operator HWND() const { return m_hWnd; }

	DWORD GetStyle() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return (DWORD)::GetWindowLong(m_hWnd, GWL_STYLE);
	}

	DWORD GetExStyle() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return (DWORD)::GetWindowLong(m_hWnd, GWL_EXSTYLE);
	}

	LONG GetWindowLong(int nIndex) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetWindowLong(m_hWnd, nIndex);
	}

	LONG SetWindowLong(int nIndex, LONG dwNewLong)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetWindowLong(m_hWnd, nIndex, dwNewLong);
	}

	WORD GetWindowWord(int nIndex) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetWindowWord(m_hWnd, nIndex);
	}

	WORD SetWindowWord(int nIndex, WORD wNewWord)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetWindowWord(m_hWnd, nIndex, wNewWord);
	}

// Message Functions

	LRESULT SendMessage(UINT message, WPARAM wParam = 0, LPARAM lParam = 0)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SendMessage(m_hWnd,message,wParam,lParam);
	}

	BOOL PostMessage(UINT message, WPARAM wParam = 0, LPARAM lParam = 0)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::PostMessage(m_hWnd,message,wParam,lParam);
	}

	BOOL SendNotifyMessage(UINT message, WPARAM wParam = 0, LPARAM lParam = 0)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SendNotifyMessage(m_hWnd, message, wParam, lParam);
	}

	// support for C style macros
	static LRESULT SendMessage(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
	{
		ATLASSERT(::IsWindow(hWnd));
		return ::SendMessage(hWnd, message, wParam, lParam);
	}

// Window Text Functions

	BOOL SetWindowText(LPCTSTR lpszString)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetWindowText(m_hWnd, lpszString);
	}

	int GetWindowText(LPTSTR lpszStringBuf, int nMaxCount) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetWindowText(m_hWnd, lpszStringBuf, nMaxCount);
	}

	int GetWindowTextLength() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetWindowTextLength(m_hWnd);
	}

// Font Functions

	void SetFont(HFONT hFont, BOOL bRedraw = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		::SendMessage(m_hWnd, WM_SETFONT, (WPARAM)hFont, MAKELPARAM(bRedraw, 0));
	}

	HFONT GetFont() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return (HFONT)::SendMessage(m_hWnd, WM_GETFONT, 0, 0);
	}

// Menu Functions (non-child windows only)

	HMENU GetMenu() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetMenu(m_hWnd);
	}

	BOOL SetMenu(HMENU hMenu)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetMenu(m_hWnd, hMenu);
	}

	BOOL DrawMenuBar()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::DrawMenuBar(m_hWnd);
	}

	HMENU GetSystemMenu(BOOL bRevert) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetSystemMenu(m_hWnd, bRevert);
	}

	BOOL HiliteMenuItem(HMENU hMenu, UINT uItemHilite, UINT uHilite)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::HiliteMenuItem(m_hWnd, hMenu, uItemHilite, uHilite);
	}

// Window Size and Position Functions

	BOOL IsIconic() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::IsIconic(m_hWnd);
	}

	BOOL IsZoomed() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::IsZoomed(m_hWnd);
	}

	BOOL MoveWindow(int x, int y, int nWidth, int nHeight, BOOL bRepaint = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::MoveWindow(m_hWnd, x, y, nWidth, nHeight, bRepaint);
	}

	BOOL MoveWindow(LPCRECT lpRect, BOOL bRepaint = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::MoveWindow(m_hWnd, lpRect->left, lpRect->top, lpRect->right - lpRect->left, lpRect->bottom - lpRect->top, bRepaint);
	}

	BOOL SetWindowPos(HWND hWndInsertAfter, int x, int y, int cx, int cy, UINT nFlags)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetWindowPos(m_hWnd, hWndInsertAfter, x, y, cx, cy, nFlags);
	}

	BOOL SetWindowPos(HWND hWndInsertAfter, LPCRECT lpRect, UINT nFlags)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetWindowPos(m_hWnd, hWndInsertAfter, lpRect->left, lpRect->top, lpRect->right - lpRect->left, lpRect->bottom - lpRect->top, nFlags);
	}

	UINT ArrangeIconicWindows()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ArrangeIconicWindows(m_hWnd);
	}

	BOOL BringWindowToTop()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::BringWindowToTop(m_hWnd);
	}

	BOOL GetWindowRect(LPRECT lpRect) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetWindowRect(m_hWnd, lpRect);
	}

	BOOL GetClientRect(LPRECT lpRect) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetClientRect(m_hWnd, lpRect);
	}

	BOOL GetWindowPlacement(WINDOWPLACEMENT FAR* lpwndpl) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetWindowPlacement(m_hWnd, lpwndpl);
	}

	BOOL SetWindowPlacement(const WINDOWPLACEMENT FAR* lpwndpl)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetWindowPlacement(m_hWnd, lpwndpl);
	}

// Coordinate Mapping Functions

	BOOL ClientToScreen(LPPOINT lpPoint) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ClientToScreen(m_hWnd, lpPoint);
	}

	BOOL ClientToScreen(LPRECT lpRect) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		if (!::ClientToScreen(m_hWnd, (LPPOINT)lpRect))
			return FALSE;
		return ::ClientToScreen(m_hWnd, ((LPPOINT)lpRect)+1);
	}

	BOOL ScreenToClient(LPPOINT lpPoint) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ScreenToClient(m_hWnd, lpPoint);
	}

	BOOL ScreenToClient(LPRECT lpRect) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		if (!::ScreenToClient(m_hWnd, (LPPOINT)lpRect))
			return FALSE;
		return ::ScreenToClient(m_hWnd, ((LPPOINT)lpRect)+1);
	}

	int MapWindowPoints(HWND hWndTo, LPPOINT lpPoint, UINT nCount) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::MapWindowPoints(m_hWnd, hWndTo, lpPoint, nCount);
	}

	int MapWindowPoints(HWND hWndTo, LPRECT lpRect) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::MapWindowPoints(m_hWnd, hWndTo, (LPPOINT)lpRect, 2);
	}

// Update and Painting Functions

	HDC BeginPaint(LPPAINTSTRUCT lpPaint)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::BeginPaint(m_hWnd, lpPaint);
	}

	void EndPaint(LPPAINTSTRUCT lpPaint)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		::EndPaint(m_hWnd, lpPaint);
	}

	HDC GetDC()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetDC(m_hWnd);
	}

	HDC GetWindowDC()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetWindowDC(m_hWnd);
	}

	int ReleaseDC(HDC hDC)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ReleaseDC(m_hWnd, hDC);
	}

	void Print(HDC hDC, DWORD dwFlags) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		::SendMessage(m_hWnd, WM_PRINT, (WPARAM)hDC, dwFlags);
	}

	void PrintClient(HDC hDC, DWORD dwFlags) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		::SendMessage(m_hWnd, WM_PRINTCLIENT, (WPARAM)hDC, dwFlags);
	}

	BOOL UpdateWindow()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::UpdateWindow(m_hWnd);
	}

	void SetRedraw(BOOL bRedraw = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		::SendMessage(m_hWnd, WM_SETREDRAW, (WPARAM)bRedraw, 0);
	}

	BOOL GetUpdateRect(LPRECT lpRect, BOOL bErase = FALSE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetUpdateRect(m_hWnd, lpRect, bErase);
	}

	int GetUpdateRgn(HRGN hRgn, BOOL bErase = FALSE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetUpdateRgn(m_hWnd, hRgn, bErase);
	}

	BOOL Invalidate(BOOL bErase = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::InvalidateRect(m_hWnd, NULL, bErase);
	}

	BOOL InvalidateRect(LPCRECT lpRect, BOOL bErase = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::InvalidateRect(m_hWnd, lpRect, bErase);
	}

	BOOL ValidateRect(LPCRECT lpRect)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ValidateRect(m_hWnd, lpRect);
	}

	void InvalidateRgn(HRGN hRgn, BOOL bErase = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		::InvalidateRgn(m_hWnd, hRgn, bErase);
	}

	BOOL ValidateRgn(HRGN hRgn)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ValidateRgn(m_hWnd, hRgn);
	}

	BOOL ShowWindow(int nCmdShow)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ShowWindow(m_hWnd, nCmdShow);
	}

	BOOL IsWindowVisible() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::IsWindowVisible(m_hWnd);
	}

	BOOL ShowOwnedPopups(BOOL bShow = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ShowOwnedPopups(m_hWnd, bShow);
	}

	HDC GetDCEx(HRGN hRgnClip, DWORD flags)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetDCEx(m_hWnd, hRgnClip, flags);
	}

	BOOL LockWindowUpdate(BOOL bLock = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::LockWindowUpdate(bLock ? m_hWnd : NULL);
	}

	BOOL RedrawWindow(LPCRECT lpRectUpdate = NULL, HRGN hRgnUpdate = NULL, UINT flags = RDW_INVALIDATE | RDW_UPDATENOW | RDW_ERASE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::RedrawWindow(m_hWnd, lpRectUpdate, hRgnUpdate, flags);
	}

// Timer Functions

	UINT SetTimer(UINT nIDEvent, UINT nElapse)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetTimer(m_hWnd, nIDEvent, nElapse, NULL);
	}

	BOOL KillTimer(UINT nIDEvent)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::KillTimer(m_hWnd, nIDEvent);
	}

// Window State Functions

	BOOL IsWindowEnabled() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::IsWindowEnabled(m_hWnd);
	}

	BOOL EnableWindow(BOOL bEnable = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::EnableWindow(m_hWnd, bEnable);
	}

	HWND SetActiveWindow()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetActiveWindow(m_hWnd);
	}

	HWND SetCapture()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetCapture(m_hWnd);
	}

	HWND SetFocus()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetFocus(m_hWnd);
	}

// Dialog-Box Item Functions

	BOOL CheckDlgButton(int nIDButton, UINT nCheck)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::CheckDlgButton(m_hWnd, nIDButton, nCheck);
	}

	BOOL CheckRadioButton(int nIDFirstButton, int nIDLastButton, int nIDCheckButton)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::CheckRadioButton(m_hWnd, nIDFirstButton, nIDLastButton, nIDCheckButton);
	}

	int DlgDirList(LPTSTR lpPathSpec, int nIDListBox, int nIDStaticPath, UINT nFileType)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::DlgDirList(m_hWnd, lpPathSpec, nIDListBox, nIDStaticPath, nFileType);
	}

	int DlgDirListComboBox(LPTSTR lpPathSpec, int nIDComboBox, int nIDStaticPath, UINT nFileType)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::DlgDirListComboBox(m_hWnd, lpPathSpec, nIDComboBox, nIDStaticPath, nFileType);
	}

	BOOL DlgDirSelect(LPTSTR lpString, int nCount, int nIDListBox)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::DlgDirSelectEx(m_hWnd, lpString, nCount, nIDListBox);
	}

	BOOL DlgDirSelectComboBox(LPTSTR lpString, int nCount, int nIDComboBox)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::DlgDirSelectComboBoxEx(m_hWnd, lpString, nCount, nIDComboBox);
	}

	UINT GetDlgItemInt(int nID, BOOL* lpTrans = NULL, BOOL bSigned = TRUE) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetDlgItemInt(m_hWnd, nID, lpTrans, bSigned);
	}

	UINT GetDlgItemText(int nID, LPTSTR lpStr, int nMaxCount) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetDlgItemText(m_hWnd, nID, lpStr, nMaxCount);
	}
	BOOL GetDlgItemText(int nID, BSTR& bstrText) const
	{
		ATLASSERT(::IsWindow(m_hWnd));

		HWND hWndCtl = GetDlgItem(nID);
		if (hWndCtl == NULL)
			return FALSE;

		return AfAxWnd(hWndCtl).GetWindowText(bstrText);
	}
	HWND GetNextDlgGroupItem(HWND hWndCtl, BOOL bPrevious = FALSE) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetNextDlgGroupItem(m_hWnd, hWndCtl, bPrevious);
	}

	HWND GetNextDlgTabItem(HWND hWndCtl, BOOL bPrevious = FALSE) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetNextDlgTabItem(m_hWnd, hWndCtl, bPrevious);
	}

	UINT IsDlgButtonChecked(int nIDButton) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::IsDlgButtonChecked(m_hWnd, nIDButton);
	}

	LRESULT SendDlgItemMessage(int nID, UINT message, WPARAM wParam = 0, LPARAM lParam = 0)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SendDlgItemMessage(m_hWnd, nID, message, wParam, lParam);
	}

	BOOL SetDlgItemInt(int nID, UINT nValue, BOOL bSigned = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetDlgItemInt(m_hWnd, nID, nValue, bSigned);
	}

	BOOL SetDlgItemText(int nID, LPCTSTR lpszString)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetDlgItemText(m_hWnd, nID, lpszString);
	}

#ifndef _ATL_NO_HOSTING
	HRESULT GetDlgControl(int nID, REFIID iid, void** ppUnk)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		ATLASSERT(ppUnk != NULL);
		HRESULT hr = E_FAIL;
		HWND hWndCtrl = GetDlgItem(nID);
		if (hWndCtrl != NULL)
		{
			*ppUnk = NULL;
			CComPtr<IUnknown> spUnk;
			hr = AtlAxGetControl(hWndCtrl, &spUnk);
			if (SUCCEEDED(hr))
				hr = spUnk->QueryInterface(iid, ppUnk);
		}
		return hr;
	}
#endif //!_ATL_NO_HOSTING

// Scrolling Functions

	int GetScrollPos(int nBar) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetScrollPos(m_hWnd, nBar);
	}

	BOOL GetScrollRange(int nBar, LPINT lpMinPos, LPINT lpMaxPos) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetScrollRange(m_hWnd, nBar, lpMinPos, lpMaxPos);
	}

	BOOL ScrollWindow(int xAmount, int yAmount, LPCRECT lpRect = NULL, LPCRECT lpClipRect = NULL)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ScrollWindow(m_hWnd, xAmount, yAmount, lpRect, lpClipRect);
	}

	int ScrollWindowEx(int dx, int dy, LPCRECT lpRectScroll, LPCRECT lpRectClip, HRGN hRgnUpdate, LPRECT lpRectUpdate, UINT uFlags)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ScrollWindowEx(m_hWnd, dx, dy, lpRectScroll, lpRectClip, hRgnUpdate, lpRectUpdate, uFlags);
	}

	int ScrollWindowEx(int dx, int dy, UINT uFlags, LPCRECT lpRectScroll = NULL, LPCRECT lpRectClip = NULL, HRGN hRgnUpdate = NULL, LPRECT lpRectUpdate = NULL)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ScrollWindowEx(m_hWnd, dx, dy, lpRectScroll, lpRectClip, hRgnUpdate, lpRectUpdate, uFlags);
	}

	int SetScrollPos(int nBar, int nPos, BOOL bRedraw = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetScrollPos(m_hWnd, nBar, nPos, bRedraw);
	}

	BOOL SetScrollRange(int nBar, int nMinPos, int nMaxPos, BOOL bRedraw = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetScrollRange(m_hWnd, nBar, nMinPos, nMaxPos, bRedraw);
	}

	BOOL ShowScrollBar(UINT nBar, BOOL bShow = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ShowScrollBar(m_hWnd, nBar, bShow);
	}

	BOOL EnableScrollBar(UINT uSBFlags, UINT uArrowFlags = ESB_ENABLE_BOTH)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::EnableScrollBar(m_hWnd, uSBFlags, uArrowFlags);
	}

// Window Access Functions

	HWND ChildWindowFromPoint(POINT point) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ChildWindowFromPoint(m_hWnd, point);
	}

	HWND ChildWindowFromPointEx(POINT point, UINT uFlags) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ChildWindowFromPointEx(m_hWnd, point, uFlags);
	}

	HWND GetTopWindow() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetTopWindow(m_hWnd);
	}

	HWND GetWindow(UINT nCmd) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetWindow(m_hWnd, nCmd);
	}

	HWND GetLastActivePopup() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetLastActivePopup(m_hWnd);
	}

	BOOL IsChild(HWND hWnd) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::IsChild(m_hWnd, hWnd);
	}

	HWND GetParent() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetParent(m_hWnd);
	}

	HWND SetParent(HWND hWndNewParent)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetParent(m_hWnd, hWndNewParent);
	}

// Window Tree Access

	int GetDlgCtrlID() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetDlgCtrlID(m_hWnd);
	}

	int SetDlgCtrlID(int nID)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return (int)::SetWindowLong(m_hWnd, GWL_ID, nID);
	}

	HWND GetDlgItem(int nID) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetDlgItem(m_hWnd, nID);
	}

// Alert Functions

	BOOL FlashWindow(BOOL bInvert)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::FlashWindow(m_hWnd, bInvert);
	}

	int MessageBox(LPCTSTR lpszText, LPCTSTR lpszCaption = _T(""), UINT nType = MB_OK)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::MessageBox(m_hWnd, lpszText, lpszCaption, nType);
	}

// Clipboard Functions

	BOOL ChangeClipboardChain(HWND hWndNewNext)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ChangeClipboardChain(m_hWnd, hWndNewNext);
	}

	HWND SetClipboardViewer()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetClipboardViewer(m_hWnd);
	}

	BOOL OpenClipboard()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::OpenClipboard(m_hWnd);
	}

// Caret Functions

	BOOL CreateCaret(HBITMAP hBitmap)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::CreateCaret(m_hWnd, hBitmap, 0, 0);
	}

	BOOL CreateSolidCaret(int nWidth, int nHeight)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::CreateCaret(m_hWnd, (HBITMAP)0, nWidth, nHeight);
	}

	BOOL CreateGrayCaret(int nWidth, int nHeight)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::CreateCaret(m_hWnd, (HBITMAP)1, nWidth, nHeight);
	}

	BOOL HideCaret()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::HideCaret(m_hWnd);
	}

	BOOL ShowCaret()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ShowCaret(m_hWnd);
	}

#ifdef _INC_SHELLAPI
// Drag-Drop Functions
	void DragAcceptFiles(BOOL bAccept = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd)); ::DragAcceptFiles(m_hWnd, bAccept);
	}
#endif

// Icon Functions

	HICON SetIcon(HICON hIcon, BOOL bBigIcon = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return (HICON)::SendMessage(m_hWnd, WM_SETICON, bBigIcon, (LPARAM)hIcon);
	}

	HICON GetIcon(BOOL bBigIcon = TRUE) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return (HICON)::SendMessage(m_hWnd, WM_GETICON, bBigIcon, 0);
	}

// Help Functions

	BOOL WinHelp(LPCTSTR lpszHelp, UINT nCmd = HELP_CONTEXT, DWORD dwData = 0)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::WinHelp(m_hWnd, lpszHelp, nCmd, dwData);
	}

	BOOL SetWindowContextHelpId(DWORD dwContextHelpId)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetWindowContextHelpId(m_hWnd, dwContextHelpId);
	}

	DWORD GetWindowContextHelpId() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetWindowContextHelpId(m_hWnd);
	}

// Hot Key Functions

	int SetHotKey(WORD wVirtualKeyCode, WORD wModifiers)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return (int)::SendMessage(m_hWnd, WM_SETHOTKEY, MAKEWORD(wVirtualKeyCode, wModifiers), 0);
	}

	DWORD GetHotKey() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SendMessage(m_hWnd, WM_GETHOTKEY, 0, 0);
	}

// Misc. Operations

//N new
#if 0
	// Conflicts with defn on AfWnd, which does almost exactly the same thing.
	BOOL GetScrollInfo(int nBar, LPSCROLLINFO lpScrollInfo)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetScrollInfo(m_hWnd, nBar, lpScrollInfo);
	}
#endif
	BOOL SetScrollInfo(int nBar, LPSCROLLINFO lpScrollInfo, BOOL bRedraw = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetScrollInfo(m_hWnd, nBar, lpScrollInfo, bRedraw);
	}
	BOOL IsDialogMessage(LPMSG lpMsg)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::IsDialogMessage(m_hWnd, lpMsg);
	}

	void NextDlgCtrl() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		::SendMessage(m_hWnd, WM_NEXTDLGCTL, 0, 0L);
	}
	void PrevDlgCtrl() const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		::SendMessage(m_hWnd, WM_NEXTDLGCTL, 1, 0L);
	}
	void GotoDlgCtrl(HWND hWndCtrl) const
	{
		ATLASSERT(::IsWindow(m_hWnd));
		::SendMessage(m_hWnd, WM_NEXTDLGCTL, (WPARAM)hWndCtrl, 1L);
	}

	BOOL ResizeClient(int nWidth, int nHeight, BOOL bRedraw = TRUE)
	{
		ATLASSERT(::IsWindow(m_hWnd));

		RECT rcWnd;
		if (!GetClientRect(&rcWnd))
			return FALSE;

		if (nWidth != -1)
			rcWnd.right = nWidth;
		if (nHeight != -1)
			rcWnd.bottom = nHeight;

		if (!::AdjustWindowRectEx(&rcWnd, GetStyle(), (!(GetStyle() & WS_CHILD) && (GetMenu() != NULL)), GetExStyle()))
			return FALSE;

		UINT uFlags = SWP_NOZORDER | SWP_NOMOVE;
		if (!bRedraw)
			uFlags |= SWP_NOREDRAW;

		return SetWindowPos(NULL, 0, 0, rcWnd.right - rcWnd.left, rcWnd.bottom - rcWnd.top, uFlags);
	}

	int GetWindowRgn(HRGN hRgn)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetWindowRgn(m_hWnd, hRgn);
	}
	int SetWindowRgn(HRGN hRgn, BOOL bRedraw = FALSE)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::SetWindowRgn(m_hWnd, hRgn, bRedraw);
	}
	HDWP DeferWindowPos(HDWP hWinPosInfo, HWND hWndInsertAfter, int x, int y, int cx, int cy, UINT uFlags)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::DeferWindowPos(hWinPosInfo, m_hWnd, hWndInsertAfter, x, y, cx, cy, uFlags);
	}
	DWORD GetWindowThreadID()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::GetWindowThreadProcessId(m_hWnd, NULL);
	}
	DWORD GetWindowProcessID()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		DWORD dwProcessID;
		::GetWindowThreadProcessId(m_hWnd, &dwProcessID);
		return dwProcessID;
	}
	BOOL IsWindow()
	{
		return ::IsWindow(m_hWnd);
	}
	BOOL IsWindowUnicode()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::IsWindowUnicode(m_hWnd);
	}
	BOOL IsParentDialog()
	{
		ATLASSERT(::IsWindow(m_hWnd));
		TCHAR szBuf[8]; // "#32770" + NUL character
		GetClassName(GetParent(), szBuf, sizeof(szBuf)/sizeof(TCHAR));
		return lstrcmp(szBuf, _T("#32770")) == 0;
	}
	BOOL ShowWindowAsync(int nCmdShow)
	{
		ATLASSERT(::IsWindow(m_hWnd));
		return ::ShowWindowAsync(m_hWnd, nCmdShow);
	}

	HWND GetDescendantWindow(int nID) const
	{
		ATLASSERT(::IsWindow(m_hWnd));

		// GetDlgItem recursive (return first found)
		// breadth-first for 1 level, then depth-first for next level

		// use GetDlgItem since it is a fast USER function
		HWND hWndChild, hWndTmp;
		AfAxWnd wnd;
		if ((hWndChild = ::GetDlgItem(m_hWnd, nID)) != NULL)
		{
			if (::GetTopWindow(hWndChild) != NULL)
			{
				// children with the same ID as their parent have priority
				wnd.Attach(hWndChild);
				hWndTmp = wnd.GetDescendantWindow(nID);
				if (hWndTmp != NULL)
					return hWndTmp;
			}
			return hWndChild;
		}

		// walk each child
		for (hWndChild = ::GetTopWindow(m_hWnd); hWndChild != NULL;
			hWndChild = ::GetNextWindow(hWndChild, GW_HWNDNEXT))
		{
			wnd.Attach(hWndChild);
			hWndTmp = wnd.GetDescendantWindow(nID);
			if (hWndTmp != NULL)
				return hWndTmp;
		}

		return NULL;    // not found
	}

	void SendMessageToDescendants(UINT message, WPARAM wParam = 0, LPARAM lParam = 0, BOOL bDeep = TRUE)
	{
		AfAxWnd wnd;
		for (HWND hWndChild = ::GetTopWindow(m_hWnd); hWndChild != NULL;
			hWndChild = ::GetNextWindow(hWndChild, GW_HWNDNEXT))
		{
			::SendMessage(hWndChild, message, wParam, lParam);

			if (bDeep && ::GetTopWindow(hWndChild) != NULL)
			{
				// send to child windows after parent
				wnd.Attach(hWndChild);
				wnd.SendMessageToDescendants(message, wParam, lParam, bDeep);
			}
		}
	}

	BOOL CenterWindow(HWND hWndCenter = NULL)
	{
		ATLASSERT(::IsWindow(m_hWnd));

		// determine owner window to center against
		DWORD dwStyle = GetStyle();
		if (hWndCenter == NULL)
		{
			if (dwStyle & WS_CHILD)
				hWndCenter = ::GetParent(m_hWnd);
			else
				hWndCenter = ::GetWindow(m_hWnd, GW_OWNER);
		}

		// get coordinates of the window relative to its parent
		RECT rcDlg;
		::GetWindowRect(m_hWnd, &rcDlg);
		RECT rcArea;
		RECT rcCenter;
		HWND hWndParent;
		if (!(dwStyle & WS_CHILD))
		{
			// don't center against invisible or minimized windows
			if (hWndCenter != NULL)
			{
				DWORD dwStyle = ::GetWindowLong(hWndCenter, GWL_STYLE);
				if (!(dwStyle & WS_VISIBLE) || (dwStyle & WS_MINIMIZE))
					hWndCenter = NULL;
			}

			// center within screen coordinates
			::SystemParametersInfo(SPI_GETWORKAREA, NULL, &rcArea, NULL);
			if (hWndCenter == NULL)
				rcCenter = rcArea;
			else
				::GetWindowRect(hWndCenter, &rcCenter);
		}
		else
		{
			// center within parent client coordinates
			hWndParent = ::GetParent(m_hWnd);
			ATLASSERT(::IsWindow(hWndParent));

			::GetClientRect(hWndParent, &rcArea);
			ATLASSERT(::IsWindow(hWndCenter));
			::GetClientRect(hWndCenter, &rcCenter);
			::MapWindowPoints(hWndCenter, hWndParent, (POINT*)&rcCenter, 2);
		}

		int DlgWidth = rcDlg.right - rcDlg.left;
		int DlgHeight = rcDlg.bottom - rcDlg.top;

		// find dialog's upper left based on rcCenter
		int xLeft = (rcCenter.left + rcCenter.right) / 2 - DlgWidth / 2;
		int yTop = (rcCenter.top + rcCenter.bottom) / 2 - DlgHeight / 2;

		// if the dialog is outside the screen, move it inside
		if (xLeft < rcArea.left)
			xLeft = rcArea.left;
		else if (xLeft + DlgWidth > rcArea.right)
			xLeft = rcArea.right - DlgWidth;

		if (yTop < rcArea.top)
			yTop = rcArea.top;
		else if (yTop + DlgHeight > rcArea.bottom)
			yTop = rcArea.bottom - DlgHeight;

		// map screen coordinates to child coordinates
		return ::SetWindowPos(m_hWnd, NULL, xLeft, yTop, -1, -1,
			SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
	}

	BOOL ModifyStyle(DWORD dwRemove, DWORD dwAdd, UINT nFlags = 0)
	{
		ATLASSERT(::IsWindow(m_hWnd));

		DWORD dwStyle = ::GetWindowLong(m_hWnd, GWL_STYLE);
		DWORD dwNewStyle = (dwStyle & ~dwRemove) | dwAdd;
		if (dwStyle == dwNewStyle)
			return FALSE;

		::SetWindowLong(m_hWnd, GWL_STYLE, dwNewStyle);
		if (nFlags != 0)
		{
			::SetWindowPos(m_hWnd, NULL, 0, 0, 0, 0,
				SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE | nFlags);
		}

		return TRUE;
	}

	BOOL ModifyStyleEx(DWORD dwRemove, DWORD dwAdd, UINT nFlags = 0)
	{
		ATLASSERT(::IsWindow(m_hWnd));

		DWORD dwStyle = ::GetWindowLong(m_hWnd, GWL_EXSTYLE);
		DWORD dwNewStyle = (dwStyle & ~dwRemove) | dwAdd;
		if (dwStyle == dwNewStyle)
			return FALSE;

		::SetWindowLong(m_hWnd, GWL_EXSTYLE, dwNewStyle);
		if (nFlags != 0)
		{
			::SetWindowPos(m_hWnd, NULL, 0, 0, 0, 0,
				SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE | nFlags);
		}

		return TRUE;
	}

	BOOL GetWindowText(BSTR* pbstrText)
	{
		return GetWindowText(*pbstrText);
	}
	BOOL GetWindowText(BSTR& bstrText)
	{
		USES_CONVERSION;
		ATLASSERT(::IsWindow(m_hWnd));
		if (bstrText != NULL)
		{
			SysFreeString(bstrText);
			bstrText = NULL;
		}

		int nLen = ::GetWindowTextLength(m_hWnd);
		if (nLen == 0)
		{
			bstrText = ::SysAllocString(OLESTR(""));
			return (bstrText != NULL) ? TRUE : FALSE;
		}

		LPTSTR lpszText = (LPTSTR)_alloca((nLen+1)*sizeof(TCHAR));

		if (!::GetWindowText(m_hWnd, lpszText, nLen+1))
			return FALSE;

		bstrText = ::SysAllocString(T2OLE(lpszText));
		return (bstrText != NULL) ? TRUE : FALSE;
	}
	HWND GetTopLevelParent() const
	{
		ATLASSERT(::IsWindow(m_hWnd));

		HWND hWndParent = m_hWnd;
		HWND hWndTmp;
		while ((hWndTmp = ::GetParent(hWndParent)) != NULL)
			hWndParent = hWndTmp;

		return hWndParent;
	}

	HWND GetTopLevelWindow() const
	{
		ATLASSERT(::IsWindow(m_hWnd));

		HWND hWndParent;
		HWND hWndTmp = m_hWnd;

		do
		{
			hWndParent = hWndTmp;
			hWndTmp = (::GetWindowLong(hWndParent, GWL_STYLE) & WS_CHILD) ? ::GetParent(hWndParent) : ::GetWindow(hWndParent, GW_OWNER);
		}
		while (hWndTmp != NULL);

		return hWndParent;
	}


	/*------------------------------------------------------------------------------------------
		At the start of the message map of any AfWnd-based ATL control, insert a call like this:
		// Allow the AfWnd message framework to handle any messages it wants to.
		if (DoAfWndMessageProc(hWnd, uMsg, wParam, lParam, lResult))
			return TRUE;
		Note that we can't usefully put a message map on this class because it doesn't inherit
		from a message map handler class. ENHANCE JohnT: should we rearrange the class hierarchy
		so that it does?
		The method returns true if something in the AfWnd framework handled the message.
		Otherwise, the message is allowed to pass through the usual ATL message map mechanism.
		The code is much like AfWnd::WndProc, but is a member function, and does not handle
		calling the default window procedure...we let ATL handle that. Also, unlike our message
		post-processor, we don't assume we have an AfApp object that can handle menu init.
	------------------------------------------------------------------------------------------*/
	bool DoAfWndMessageProc(HWND hwnd, UINT uMsg, WPARAM wp, LPARAM lp, LRESULT& lResult)
	{
		Assert(hwnd);

		AfWndPtr qwnd = this;

		// AfWnd normally does this on WM_CREATE, but here we need to do it on the very
		// first message the window gets, because the thunk sends even that here, and we
		// don't want to fire the Assert below. WM_NCCREATE seems to be the very first.
		// Note JohnT: we could just do it on the first message, whatever it is?
		// Say, detecting by hwnd == 0? That may be necessary if we discover an exception
		// to the rule about WM_NCCREATE being first, but the Assert would be lost.
		if (uMsg == WM_NCCREATE)
		{
			try
			{
				qwnd->AttachHwnd(hwnd);
			}
			catch (...)
			{
				Warn("AttachHwnd failed.");
				return false;
			}
		}

		Assert(qwnd->Hwnd() == hwnd);

		bool fRet = false; // if an exception occurs we didn't handle it.
		lResult = 0; // our FWndProcs assume this as a default.

		try
		{
			// Call the pre non-virtual WndProc.
			fRet = qwnd->FWndProcPre(uMsg, wp, lp, lResult);

			// Call the virtual window proc.
			if (!fRet)
				fRet = qwnd->FWndProc(uMsg, wp, lp, lResult);

			// qwnd->WndProcPost(fRet, hwnd, uMsg, wp, lp, lResult);
		}
		catch (...)
		{
			Warn("Exception caught in AfWnd::WindowProc");
			return 0;
		}

		if (uMsg == WM_NCDESTROY)
		{
			try
			{
				// This is a bit of a kludge. ATL has its own way of managing the object lifetime
				// (I haven't figured exactly what it is), and will destroy our object eventually.
				// If we don't do anyting special, we get one ref count when we AttachHwnd(), and
				// lose it (and destroy the object) in DetachHwnd(). That destroys the object
				// before ATL is finished with it.
				// ENHANCE JohnT: do we need to do more to merge the two approaches to managing
				// object lifetime?
				AfWnd::AddRef();
				OnReleasePtr();
				DetachHwnd(hwnd);
				// Do NOT claim to have handled it...ATL may still do more stuff.
			}
			catch (...)
			{
				Warn("Handling WM_NCDESTROY failed.");
				return false;
			}
		}

		return fRet;
	}
};

/*----------------------------------------------------------------------------------------------
	A base class to help us define AfAtlControl.
	See the file header for more explanation.
----------------------------------------------------------------------------------------------*/
class AfAxWnd : public AfAxWndTempl<AfWnd>
{
};

/*----------------------------------------------------------------------------------------------
	A base class to replace CComControl for ATL ActiveX controls also based on AfWnd.
----------------------------------------------------------------------------------------------*/
template <class TBase>
class AfAtlControl : public CComControl<TBase, CWindowImpl<TBase, AfAxWnd> >
{
	typedef CComControl<TBase, CWindowImpl<TBase, AfAxWnd> > SuperClass;
	BEGIN_MSG_MAP(AfAtlControl<TBase>)
		// Allow the AfWnd message framework to handle any messages it wants to.
		if (DoAfWndMessageProc(hWnd, uMsg, wParam, lParam, lResult))
			return TRUE;
		CHAIN_MSG_MAP(SuperClass)
		DEFAULT_REFLECTION_HANDLER()
	END_MSG_MAP()
};

/*----------------------------------------------------------------------------------------------
	A base class to help us define AfAtlSplitControl.
	See the file header for more explanation.
----------------------------------------------------------------------------------------------*/
class AfAxSplitWnd : public AfAxWndTempl<AfSplitFrame>
{
};

/*----------------------------------------------------------------------------------------------
	A base class to replace CComControl for ATL ActiveX controls also based on AfWnd.
	Generate the control using the Wizard, then replace CComControl<YourClass> with
	AfAtlSplitControl<YourClass>.
----------------------------------------------------------------------------------------------*/
template <class TBase>
class AfAtlSplitControl : public CComControl<TBase, CWindowImpl<TBase, AfAxSplitWnd> >
{
	typedef CComControl<TBase, CWindowImpl<TBase, AfAxSplitWnd> > SuperClass;
	BEGIN_MSG_MAP(AfAtlSplitControl<TBase>)
		// Allow the AfWnd message framework to handle any messages it wants to.
		if (DoAfWndMessageProc(hWnd, uMsg, wParam, lParam, lResult))
			return TRUE;
		CHAIN_MSG_MAP(SuperClass)
		DEFAULT_REFLECTION_HANDLER()
	END_MSG_MAP()
};



_declspec(selectany) RECT AfAxWnd::rcDefault = { CW_USEDEFAULT, CW_USEDEFAULT, 0, 0 };
_declspec(selectany) RECT AfAxSplitWnd::rcDefault = { CW_USEDEFAULT, CW_USEDEFAULT, 0, 0 };

#endif
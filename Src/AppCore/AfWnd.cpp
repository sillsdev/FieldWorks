/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfWnd.cpp
Responsibility: Steve McConnel (was Darrell Zook)
Last reviewed:
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#include <typeinfo.h>
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE
//:End Ignore

//:>********************************************************************************************
//:>	AfWnd methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Register this window class.  This is a static method.

	@param pszName The window class name.
	@param grfwcs Window class style flags (CS_HREDRAW, CS_NOCLOSE, ...)
	@param ridCrs Resource ID for the window class cursor, or 0.
	@param ridMenu Resource ID for the window class menu, or 0.
	@param clrBack Background color for the window class.
	@param ridIcon Resource ID for the window class icon, or 0.
	@param ridIconSmall Resource ID for the window class small icon, or 0.
----------------------------------------------------------------------------------------------*/
void AfWnd::RegisterClass(Pcsz pszName, int grfwcs, int ridCrs, int ridMenu, int clrBack,
	int ridIcon, int ridIconSmall)
{
	AssertPsz(pszName);
	Assert(!(ridCrs & 0xFFFF0000));
	Assert(!(ridMenu & 0xFFFF0000));
	Assert(clrBack == -1 || !(clrBack & 0xFFFF0000));
	Assert(!(ridIcon & 0xFFFF0000));
	Assert(!(ridIconSmall & 0xFFFF0000));

	WNDCLASSEX wcx;
	HINSTANCE hinst = ModuleEntry::GetModuleHandle();

	ClearItems(&wcx, 1);
	wcx.cbSize = isizeof(wcx);
	wcx.style = grfwcs;
	wcx.lpfnWndProc = &AfWnd::WndProc;
	wcx.cbClsExtra = 0;

	// REVIEW ShonK: For dialogs this needs to be DLGWINDOWEXTRA. Will we ever register
	// dialog classes? Probably not.
	wcx.cbWndExtra = 0;

	wcx.hInstance = hinst;

	// REVIEW ShonK: Determine whether to pass NULL or hinst in LoadIcon calls.
	wcx.hIcon = ridIcon ? ::LoadIcon(hinst, MAKEINTRESOURCE(ridIcon)) : NULL;
	wcx.hCursor = ridCrs ? ::LoadCursor(NULL, MAKEINTRESOURCE(ridCrs)) : NULL;
	// REVIEW ShonK: Do we not want to disallow a brush to be passed in clrBack?
	// hbrBackground in WNDCLASSEX allows a brush if it is not one a fixed number of
	// system colors. For context help we want COLOR_INFOBK which is not listed as
	// one of the valid colors. However, this along with the assert above, fails if
	// we try to pass a brush with COLOR_INFOBK, although this should be acceptable to
	// WNDCLASSEX. It ends up that we can pass COLOR_INFOBK to this method directly, and
	// it works OK. Will any color work using this approach, or should we allow a brush
	// to be consistent with WNDCLASSEX?
	// wcx.hbrBackground = (uint)clrBack < (uint)30 ? (HBRUSH)(clrBack + 1) : (HBRUSH)clrBack;
	wcx.hbrBackground = clrBack >= 0 ? (HBRUSH)(clrBack + 1) : NULL;
	wcx.lpszMenuName = MAKEINTRESOURCE(ridMenu);
	wcx.lpszClassName = pszName;
	wcx.hIconSm = ridIconSmall ? ::LoadIcon(hinst, MAKEINTRESOURCE(ridIconSmall)) : NULL;

	if (!::RegisterClassEx(&wcx))
	{
		// we try to avoid this, but it can happen with opening/closing multiple projects and
		// multiple dialogs.
		DWORD dw = ::GetLastError();
		if (dw == ERROR_CLASS_ALREADY_EXISTS)
			return;
		AssertMsg(false, "Registering window class failed.");
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
}


/*----------------------------------------------------------------------------------------------
	Get the AfWnd object pointer from a window handle.  This is a static method.

	@param hwnd Handle to a window (or 0--returns null).

	@return Pointer to the associated AfWnd object, or NULL if there is none or hwnd is invalid.
----------------------------------------------------------------------------------------------*/
AfWnd * AfWnd::GetAfWnd(HWND hwnd)
{
	if (!hwnd)
		return NULL;
	AfWnd * pwnd = (AfWnd *)GetWindowLong(hwnd, GWL_USERDATA);
	if (!pwnd)
		return NULL;

	try
	{
		if (pwnd->m_nMagic != knMagicWnd)
			return NULL;
	}
	catch (...)
	{
		return NULL;
	}

	return pwnd;
}


/*----------------------------------------------------------------------------------------------
	Constructor.

	Note: for the ATL support subclass, it is important that this constructor zero variables
	where required, that is, don't assume memory is allocated with NewObj or a similar zeroing
	allocator.
----------------------------------------------------------------------------------------------*/
AfWnd::AfWnd()
{
	m_hwnd = 0;
	m_wnpDefWndProc = 0;
	m_nMagic = knMagicWnd;
	m_pmum = NULL;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfWnd::~AfWnd()
{
	AssertObj(this);
	Assert(!m_hwnd);
	m_nMagic = 0;
	if (m_pmum)
	{
		delete m_pmum;
		m_pmum = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Create the HWND associated with this AfWnd object.

	@param wcs Pointer to the object containing the values for calling ::CreateWindowEx.
----------------------------------------------------------------------------------------------*/
void AfWnd::CreateHwnd(WndCreateStruct & wcs)
{
	AssertObj(this);
	Assert(!m_hwnd);

	PreCreateHwnd(wcs);

	AfWndCreate afwc = { this, wcs.lpCreateParams };

	HWND hwnd = ::CreateWindowEx(wcs.dwExStyle, wcs.lpszClass, wcs.lpszName, wcs.style,
		wcs.x, wcs.y, wcs.cx, wcs.cy, wcs.hwndParent, wcs.hMenu, wcs.hInstance, &afwc);

	Assert(m_hwnd == hwnd);
	if (!hwnd)
	{
#if 0
		// Popup DEBUG explanation of failure.
		LPVOID lpMsgBuf;
		::FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM |
			FORMAT_MESSAGE_IGNORE_INSERTS,
			NULL, ::GetLastError(),
			MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR) &lpMsgBuf, 0, NULL);
		::MessageBox(NULL, (LPCTSTR)lpMsgBuf, _T("DEBUG"), MB_OK);
		::LocalFree(lpMsgBuf);
#endif
		ThrowHr(WarnHr(E_FAIL));
	}
}


/*----------------------------------------------------------------------------------------------
	Create and subclass the HWND associated with this AfWnd object.

	@param wcs Pointer to the object containing the values for calling ::CreateWindowEx.
----------------------------------------------------------------------------------------------*/
void AfWnd::CreateAndSubclassHwnd(WndCreateStruct & wcs)
{
	AssertObj(this);
	Assert(!m_hwnd);

	PreCreateHwnd(wcs);

	HWND hwnd = ::CreateWindowEx(wcs.dwExStyle, wcs.lpszClass, wcs.lpszName, wcs.style,
		wcs.x, wcs.y, wcs.cx, wcs.cy, wcs.hwndParent, wcs.hMenu, wcs.hInstance,
		wcs.lpCreateParams);

	Assert(!m_hwnd);
	if (!hwnd)
		ThrowHr(WarnHr(E_FAIL));

	try
	{
		SubclassHwnd(hwnd);
	}
	catch (...)
	{
		::DestroyWindow(hwnd);
		throw;
	}
}


/*----------------------------------------------------------------------------------------------
	Set the underlying HWND's show state by calling ::ShowWindow.

	@param nShow Specifies how the window is to be shown: SW_FORCEMINIMIZE, SW_HIDE, ...
----------------------------------------------------------------------------------------------*/
void AfWnd::Show(int nShow)
{
	AssertObj(this);
	Assert(m_hwnd);

	::ShowWindow(m_hwnd, nShow);
}


/*----------------------------------------------------------------------------------------------
	Destroy the underlying HWND by calling ::DestroyWindow.
----------------------------------------------------------------------------------------------*/
void AfWnd::DestroyHwnd(void)
{
	AssertObj(this);
	if (m_hwnd)
		::DestroyWindow(m_hwnd);
	Assert(!m_hwnd);
}


/*----------------------------------------------------------------------------------------------
	Attach the given hwnd to this AfWnd.  This means that we save hwnd as m_hwns, and store a
	pointer to this AfWnd object as the user data associated with the hwnd.  This is normally
	called when WM_CREATE is processed, but may be called at a later time, especially for
	controls in dialogs.

	@param hwnd Handle to a window.
----------------------------------------------------------------------------------------------*/
void AfWnd::AttachHwnd(HWND hwnd)
{
	AssertObj(this);
	Assert(hwnd != NULL);

	if (m_hwnd)
	{
		if (m_hwnd == hwnd)
			return;
		AssertMsg(false, "AfWnd already attached to a different HWND.");
		ThrowHr(E_FAIL);
	}

	AfWnd * pwnd = reinterpret_cast<AfWnd *>(::GetWindowLong(hwnd, GWL_USERDATA));
	if (pwnd)
	{
		AssertMsg(false, "Hwnd already attached to an AfWnd");
		ThrowHr(E_FAIL);
	}

	::SetWindowLong(hwnd, GWL_USERDATA, (long)this);
	AddRef();
	m_hwnd = hwnd;

	PostAttach();
}


/*----------------------------------------------------------------------------------------------
	Detach the given hwnd from this AfWnd.  This is normally called when WM_NCDESTROY is
	processed, but may be called earlier.

	@param hwnd Handle to a window.
----------------------------------------------------------------------------------------------*/
void AfWnd::DetachHwnd(HWND hwnd)
{
	AssertObj(this);
	Assert(hwnd != NULL);

	if (!m_hwnd)
	{
		AssertMsg(false, "AfWnd not attached to the HWND.");
		ThrowHr(E_FAIL);
	}

	AfWnd * pwnd = reinterpret_cast<AfWnd *>(::GetWindowLong(hwnd, GWL_USERDATA));
	if (pwnd != this)
	{
		AssertMsg(false, "Hwnd not attached to this AfWnd");
		ThrowHr(E_FAIL);
	}

	::SetWindowLong(hwnd, GWL_USERDATA, 0);
	if (m_wnpDefWndProc)
	{
		::SetWindowLong(hwnd, GWL_WNDPROC, (long)m_wnpDefWndProc);
		m_wnpDefWndProc = NULL;
	}

	m_hwnd = NULL;
	Release();
}


/*----------------------------------------------------------------------------------------------
	Attach the given hwnd to this AfWnd, and subclass it by setting the window procedure for the
	hwnd to AfWnd::WndProc.

	@param hwnd Handle to a window.
----------------------------------------------------------------------------------------------*/
void AfWnd::SubclassHwnd(HWND hwnd)
{
	AssertObj(this);
	Assert(hwnd != NULL);
	Assert(!m_hwnd);
	Assert(!m_wnpDefWndProc);

	AttachHwnd(hwnd);
	SetLastError(0);
	m_wnpDefWndProc = (WNDPROC)::SetWindowLong(hwnd, GWL_WNDPROC, (long)&AfWnd::WndProc);
	Assert(m_wnpDefWndProc);
}


/*----------------------------------------------------------------------------------------------
	Call the appropriate default window procedure.  This is a virtual method.

	@param wm Window message identifier.
	@param wp First message parameter.
	@param lp Second message parameter.

	@return Result of the message processing -- depends on the message.
----------------------------------------------------------------------------------------------*/
LRESULT AfWnd::DefWndProc(uint wm, WPARAM wp, LPARAM lp)
{
	AssertObj(this);
	Assert(m_hwnd != NULL);

	if (m_wnpDefWndProc)
		return ::CallWindowProc(m_wnpDefWndProc, m_hwnd, wm, wp, lp);
	else
		return ::DefWindowProc(m_hwnd, wm, wp, lp);
}

#ifdef _DEBUG
static int DebugWindowMessages; // set non-zero to display trace info on all windows messages.
#endif
/*----------------------------------------------------------------------------------------------
	This static method provides the basic window procedure used in this application framework.

	@param hwnd Handle of the window to which the message is directed.
	@param wm Window message identifier.
	@param wp First message parameter -- depends on the message.
	@param lp Second message parameter -- depends on the message.

	@return Result of the message processing -- depends on the message.
----------------------------------------------------------------------------------------------*/
LRESULT CALLBACK AfWnd::WndProc(HWND hwnd, uint wm, WPARAM wp, LPARAM lp)
{
	Assert(hwnd);

	AfWndPtr qwnd = reinterpret_cast<AfWnd *>(::GetWindowLong(hwnd, GWL_USERDATA));
	AssertPtrN(qwnd);
#ifdef _DEBUG
	if (DebugWindowMessages)
	{
		const type_info & myTypeInfo (typeid(*(qwnd.Ptr())));
		StrAnsi staT;
		staT.Format("Message %x sent to %s (%x)\n", wm, myTypeInfo.name(), hwnd);
		::OutputDebugStringA(staT.Chars());
	}
#endif

	if (!qwnd)
	{
		if (wm != WM_CREATE)
			return ::DefWindowProc(hwnd, wm, wp, lp);

		CREATESTRUCT * pcs = reinterpret_cast<CREATESTRUCT *>(lp);
		if (!pcs || !pcs->lpCreateParams)
			return ::DefWindowProc(hwnd, wm, wp, lp);

		AssertPtr(pcs);
		AfWndCreate * pafwc = reinterpret_cast<AfWndCreate *>(pcs->lpCreateParams);
		AssertPtr(pafwc);
		qwnd = pafwc->pwnd;
		AssertPtr(qwnd);

		try
		{
			qwnd->AttachHwnd(hwnd);
		}
		catch (...)
		{
			Warn("AttachHwnd failed.");
			return -1;
		}

		// Put the user-defined value back into pcs so that the subclass will have it.
		pcs->lpCreateParams = pafwc->pv;
	}

	Assert(qwnd->m_hwnd == hwnd);

	bool fRet;
	long lnRet = 0;

	BEGIN_TOP_LEVEL_ACTION
		// Call the pre non-virtual WndProc.
		fRet = qwnd->FWndProcPre(wm, wp, lp, lnRet);

		// Call the virtual window proc.
		if (!fRet)
			fRet = qwnd->FWndProc(wm, wp, lp, lnRet);

		qwnd->WndProcPost(fRet, hwnd, wm, wp, lp, lnRet);
	END_TOP_LEVEL_ACTION

	// Assert(_CrtCheckMemory()); //Not a bad place to insert this if having memory troubles.
	return lnRet;
}


/*----------------------------------------------------------------------------------------------
	Non-virtual window proc to call standard message handlers.  All handlers should be virtual.
	WARNING: This method is also called for dialogs.

	@param wm Windows message identifier.
	@param wp First message parameter.
	@param lp Second message parameter.
	@param lnRet Value to be returned to the system.  (return value for window procedure)

	@return true to prevent the message from being sent to other windows.
----------------------------------------------------------------------------------------------*/
bool AfWnd::FWndProcPre(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);

	bool fRet;

	switch (wm)
	{
	case WM_SYSCOLORCHANGE:
		// In an ActiveX control we may have no Papp().
		// It's a bit unusual to be handling a command in that situation, but it can happen.
		if (!AfApp::Papp())
			return false;
		// Push a color change command and enqueue a command indicating that we're done
		// processing color change messages.
		// NOTE: We get one of these per top level window.
		AfApp::Papp()->PushCid(kcidColorChange, this, wp, lp);
		AfApp::Papp()->EnqueueCid(kcidEndColorChange, this, wp, lp);
		return false;

	case WM_SETTINGCHANGE:
		// In an ActiveX control we may have no Papp().
		// It's a bit unusual to be handling a command in that situation, but it can happen.
		if (!AfApp::Papp())
			return false;
		// Push a color setting command and enqueue a command indicating that we're done
		// processing setting change messages.
		// NOTE: We may get many of these per top level window.
		AfApp::Papp()->PushCid(kcidSettingChange, this, wp, lp);
		AfApp::Papp()->EnqueueCid(kcidEndSettingChange, this, wp, lp);
		return false;

	case WM_MEASUREITEM:
		// Handle menu items.
		if (!wp && AfApp::GetMenuMgr(&m_pmum)->OnMeasureItem((MEASUREITEMSTRUCT *)lp))
			return true;
		return OnMeasureChildItem((MEASUREITEMSTRUCT *)lp);

	case WM_DRAWITEM:
		// Handle menu items.
		if (!wp && AfApp::GetMenuMgr(&m_pmum)->OnDrawItem((DRAWITEMSTRUCT *)lp))
			return true;
		return OnDrawChildItem((DRAWITEMSTRUCT *)lp);

	case WM_INITMENUPOPUP:
		// LOWORD(lp) specifies the zero-based relative position of the menu item that opens the
		// drop-down menu or submenu.
		// HIWORD(lp) If the menu is the window menu, this is TRUE; otherwise, it is FALSE.
		if (HIWORD(lp) == 0) // Ignore system menus.
		{
			AfApp::GetMenuMgr(&m_pmum)->ExpandMenuItems((HMENU)wp, LOWORD(lp));
		}
		return OnInitMenuPopup((HMENU)wp, LOWORD(lp), HIWORD(lp) != 0);

	case WM_MENUSELECT:
		if (!lp && HIWORD(wp) == 0xFFFF)
		{
			// Menu was closed if it had been open.
			AfApp::GetMenuMgr(&m_pmum)->OnMenuClose();
			AfMainWnd * pafw = MainWindow();
			if (pafw)
				pafw->SetContextInfo(NULL, MakePoint(0));
		}
		return OnMenuSelect((int)LOWORD(wp), (UINT)HIWORD(wp), (HMENU)lp);

	case WM_MENUCHAR:
		lnRet = AfApp::GetMenuMgr(&m_pmum)->OnMenuChar((achar)LOWORD(wp), (HMENU)lp);
		return lnRet != 0;

	case WM_PAINT:
		return OnPaint((HDC)wp);

	case WM_SIZE:
		fRet = OnSize(wp, LOWORD(lp), HIWORD(lp));
		OnClientSize();
		return fRet;

	case WM_COMMAND:
		return OnCommand(LOWORD(wp), HIWORD(wp), (HWND)lp);

	case WM_NOTIFY:
		return OnNotifyChild(wp, (NMHDR *)lp, lnRet);

	case WM_SETFOCUS:
		return OnSetFocus();

	case WM_CONTEXTMENU:
		return OnContextMenu((HWND)wp, MakePoint(lp));
	}

	// Keep passing the message.
	return false;
}


/*----------------------------------------------------------------------------------------------
	Non-virtual window proc to call standard message handlers. All handlers should be virtual.
	WARNING: This method is also called for dialogs. Dialogs will always pass true for fRet.

	@param fRet Flag not to call the default window procedure.
	@param hwnd Handle of the window to which the message is directed.
	@param wm Windows message identifier.
	@param wp First message parameter.
	@param lp Second message parameter.
	@param lnRet Value to be returned to the system.  (return value for window procedure)
----------------------------------------------------------------------------------------------*/
void AfWnd::WndProcPost(int fRet, HWND hwnd, uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(hwnd != NULL);

	switch (wm)
	{
	// Menu management.
	case WM_INITMENUPOPUP:
		// Ignore system menus.
		if (!HIWORD(lp))
			AfApp::GetMenuMgr(&m_pmum)->OnInitMenuPopup((HMENU)wp, (int)LOWORD(lp));
		break;
	}

	// Call the default window proc.
	if (!fRet)
	{
		if (hwnd != m_hwnd)
			lnRet = ::DefWindowProc(hwnd, wm, wp, lp);
		else
			lnRet = DefWndProc(wm, wp, lp);
	}

	switch (wm)
	{
	// AfWnd management.
	case WM_NCDESTROY:
		if (hwnd == m_hwnd)
		{
			OnReleasePtr();
			DetachHwnd(hwnd);
		}
		break;
	}
}


/*----------------------------------------------------------------------------------------------
	Process a WM_COMMAND message by creating a command object and dispatching it.

	@param cid The identifier of the menu item, control, or accelerator.  (LOWORD(wParam) from
					the window procedure)
	@param nc The notification code if the message is from a control.  If the message is from
			an accelerator, this value is 1.  If the message is from a menu, this value is zero.
			(HIWORD(wParam) from the window procedure)
	@param hctl Handle to the control.  (lParam from the window procedure)

	@return True if the message is handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfWnd::OnCommand(int cid, int nc, HWND hctl)
{
	//StrApp str;
	//str.Format("AfWnd::OnCommand, cid=%d, nc=%d, hctl=%d\n", cid, nc, hctl);
	//OutputDebugString(str.Chars());

	AssertObj(this);
	CmdPtr qcmd;

	if (cid == 0)
		return false;

	qcmd.Attach(NewObj Cmd);
	qcmd->m_qcmh = this;
	qcmd->m_cid = cid;
	qcmd->m_rgn[0] = nc;
	qcmd->m_rgn[1] = (int)hctl;
	// In an ActiveX control we may have no Papp().
	// It's a bit unusual to be handling a command in that situation, but it can happen.
	if (AfApp::Papp())
		return AfApp::Papp()->FDispatchCmd(qcmd);
	else
		return false;
}


/*----------------------------------------------------------------------------------------------
	Process a WM_INITMENUPOPUP message by calling the menu manager to update the menu items.

	@param hmenu Handle to the menu.  (lParam from the window procedure)
	@param ihmenu Index of the menu item that opens the drop-down menu or submenu.
					(LOWORD(lParam) from the window procedure)
	@param fSysMenu Flag whether this is the window menu.  (HIWORD(lParam) from the window
					procedure)

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfWnd::OnInitMenuPopup(HMENU hmenu, int ihmenu, bool fSysMenu)
{
	AssertObj(this);

	if (!fSysMenu)
		AfApp::GetMenuMgr(&m_pmum)->SetMenuStates(this, hmenu, ihmenu, fSysMenu);

	return true;
}

/*----------------------------------------------------------------------------------------------
	This allows a child window of the current window to handle owner draw itself when its
	parent receives a WM_MEASUREITEM message.

	@param pmis Pointer to a data structure for returning the dimensions of an owner-drawn
					control or menu item.  (lParam from window procedure)

	@return True if the message is handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfWnd::OnMeasureChildItem(MEASUREITEMSTRUCT * pmis)
{
	AssertObj(this);
	AssertPtr(pmis);

	HWND hwnd = ::GetDlgItem(m_hwnd, pmis->CtlID);
	if (!hwnd)
		return false;

	AfWnd * pwnd = AfWnd::GetAfWnd(hwnd);
	if (!pwnd)
		return false;
	AssertObj(pwnd);

	return pwnd->OnMeasureThisItem(pmis);
}

/*----------------------------------------------------------------------------------------------
	This allows a child window of the current window to handle owner draw itself when its
	parent receives a WM_DRAWITEM message.

	@param pdis Pointer to a data structure containing information needed to paint an
					owner-drawn control or menu item.  (lParam from window procedure)

	@return True if the message is handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfWnd::OnDrawChildItem(DRAWITEMSTRUCT * pdis)
{
	AssertObj(this);
	AssertPtr(pdis);

	HWND hwnd = ::GetDlgItem(m_hwnd, pdis->CtlID);
	if (!hwnd)
		return false;

	AfWnd * pwnd = AfWnd::GetAfWnd(hwnd);
	if (!pwnd)
		return false;
	AssertObj(pwnd);

	return pwnd->OnDrawThisItem(pdis);
}


/*----------------------------------------------------------------------------------------------
	Process a WM_NOTIFY message.

	@param id Identifier of the common control sending the message.  (wParam from the window
					procedure)
	@param pnmh Pointer to an NMHDR structure containing notification code and additional info.
					(lParam from the window procedure)
	@param lnRet Value to be returned to the system.  (return value for the window procedure)

	@return True if the message is handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfWnd::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	AssertObj(this);
	AssertPtr(pnmh);
	Assert(pnmh->hwndFrom);

	AfWnd * pwnd = AfWnd::GetAfWnd(pnmh->hwndFrom);
	if (!pwnd || pwnd == this)
		return false;
	AssertObj(pwnd);

	return pwnd->OnNotifyThis(id, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	This static callback method calls either SaveSettings or LoadSettings for all the children
	in a specific window.

	@param hwnd Handle to the parent window.
	@param lParam Pointer to the setting information, which include whether to load or save,
					and the registry root string of the application.

	@return TRUE.
----------------------------------------------------------------------------------------------*/
BOOL CALLBACK AfWnd::EnumChildProc(HWND hwnd, LPARAM lParam)
{
	SettingInfo * psi = (SettingInfo *)lParam;
	AssertPtr(psi);

	AfWnd * pwnd = AfWnd::GetAfWnd(hwnd);
	if (pwnd)
	{
		if (psi->fSave)
			pwnd->SaveSettings(psi->pszRoot);
		else
			pwnd->LoadSettings(psi->pszRoot);
	}
	else
		::EnumChildWindows(hwnd, &AfWnd::EnumChildProc, (LPARAM)psi);

	return TRUE;
}


/*----------------------------------------------------------------------------------------------
	If fRecursive is true, every child window will be asked to load their settings.

	@param pszRoot Pointer to the registry root string of the application.
	@param fRecursive Flag whether to load recursively for child windows.
----------------------------------------------------------------------------------------------*/
void AfWnd::LoadSettings(const achar * pszRoot, bool fRecursive)
{
	// REVIEW SteveMc: This looks like it doesn't really do anything yet, other than the
	// recursion.
	if (fRecursive)
	{
		SettingInfo si = { const_cast<achar *>(pszRoot), false };
		::EnumChildWindows(m_hwnd, &AfWnd::EnumChildProc, (LPARAM)&si);
	}
}


/*----------------------------------------------------------------------------------------------
	If fRecursive is true, every child window will be asked to save their settings.

	@param pszRoot Pointer to the registry root string of the application.
	@param fRecursive Flag whether to load recursively for child windows.
----------------------------------------------------------------------------------------------*/
void AfWnd::SaveSettings(const achar * pszRoot, bool fRecursive)
{
	// REVIEW SteveMc: This looks like it doesn't really do anything yet, other than the
	// recursion.
	if (fRecursive)
	{
		SettingInfo si = { const_cast<achar *>(pszRoot), true };
		::EnumChildWindows(m_hwnd, &AfWnd::EnumChildProc, (LPARAM)&si);
	}
}


/*----------------------------------------------------------------------------------------------
	Load the size and position of the window.

	@param pszRoot Registry subkey name for the application.
	@param pszValue Registry key value name.
----------------------------------------------------------------------------------------------*/
void AfWnd::LoadWindowPosition(const achar * pszRoot, const achar * pszValue)
{
	Assert(::IsWindow(m_hwnd)); // Make sure the window handle is valid.

	Rect rc;
	FwSettings * pfws = AfApp::GetSettings();
	if (pfws->GetBinary(pszRoot, pszValue, (BYTE *)&rc, isizeof(RECT)))
	{
		AfGfx::EnsureVisibleRect(rc);
		WINDOWPLACEMENT wp = { isizeof(wp) };
		wp.rcNormalPosition = rc;
		::SetWindowPlacement(m_hwnd, &wp);
	}
}


/*----------------------------------------------------------------------------------------------
	Save the size and position of the window.

	@param pszRoot Registry subkey name for the application.
	@param pszValue Registry key value name.
----------------------------------------------------------------------------------------------*/
void AfWnd::SaveWindowPosition(const achar * pszRoot, const achar * pszValue)
{
	Assert(::IsWindow(m_hwnd)); // Make sure the window handle is valid.

	WINDOWPLACEMENT wp = { isizeof(wp) };
	::GetWindowPlacement(m_hwnd, &wp);

	FwSettings * pfws = AfApp::GetSettings();
	pfws->SetBinary(pszRoot, pszValue, (BYTE *)&wp.rcNormalPosition, isizeof(RECT));
}


/*----------------------------------------------------------------------------------------------
	Find the parent window (or NULL, if no parent or parent is not an AfWnd)
	(This is almost too trivial to make a method out of, but I got sick of looking up how to
	do it.)

	@return Pointer to the AfWnd object of the parent window, or NULL.
----------------------------------------------------------------------------------------------*/
AfWnd * AfWnd::Parent()
{
	return AfWnd::GetAfWnd(::GetParent(m_hwnd));
}

/*----------------------------------------------------------------------------------------------
	Find your main window, or NULL,
	if no ancestor or owner is an AfMainWnd.

	@return Pointer to the ancestral AfMainWnd object, or NULL.
----------------------------------------------------------------------------------------------*/
AfMainWnd * AfWnd::MainWindow()
{
	HWND hwnd = m_hwnd;
	AfMainWnd * pafw = NULL;
	while (hwnd && (pafw = dynamic_cast<AfMainWnd *>(AfWnd::GetAfWnd(hwnd))) == NULL)
		hwnd = ::GetParent(hwnd);
	return pafw;
}

/*----------------------------------------------------------------------------------------------
	Notifies the window that a style definition has changed.
	Overrides should call the superclass method to get inherited behavior.
	This default implementation does nothing.
----------------------------------------------------------------------------------------------*/
void AfWnd::OnStylesheetChange()
{
}


/*----------------------------------------------------------------------------------------------
	Refresh the contents of the window (including child windows).
	If fReloadData is true, windows which depend on data like a database which could be
	independently modified should reload from the database. Otherwise it may be assumed that
	the main data (typicaly in a CustViewDa) is already valid, and just make the display
	conform.
	This default implementation invalidates the window and propagates the message down to
	children.

	Enhance JohnT: this looked like a nice idea, but I hadn't gotten far with it when I
	discovered that Rand had implemented AfApp::UpdateAllWindows. This serves too similar a
	purpose to duplicate the code, but I like RefreshAll better as an approach, though there
	may be a need to enhance it with an option to indicate only updating windows related to
	a particular database. I didn't want to discard what I'd done with RefreshAll, but I also
	didn't want to try to change the architecture of Rand's work this close to release.

	So RefreshAll remains a stub that is not fully implemented (no window class has a
	non-trivial implementation) and not used.

	WLC/TE also has the beginnings of a Refresh architecture that should be merged into the
	final solution.

	Note from KenZ: I considered using this approach instead of FullRefresh(), which I
	implemented instead. The problem I saw with this approach was that we need more control
	over the order classes get refreshed. This method simply gives a list of window handles
	in somewhat random order (around 177 for RnMainWnd). Also, it works much better for us
	to process things on a class basis rather than an HWND basis.
----------------------------------------------------------------------------------------------*/
void AfWnd::RefreshAll(bool fReloadData)
{
	LPARAM lparam = fReloadData ? 1 : 0;
	::EnumChildWindows(m_hwnd, &AfWnd::EnumChildRefreshAll, lparam);
	::InvalidateRect(m_hwnd, NULL, true);
}

/*----------------------------------------------------------------------------------------------
	This static callback method calls OnStylesheetChange() for all the children
	in a specific window. It is invoked from AfMainWnd::OnStylesheetChange.

	@param hwnd Handle to the parent window.
	@param lParam not used.

	@return TRUE.
----------------------------------------------------------------------------------------------*/
BOOL CALLBACK AfWnd::EnumChildSsChange(HWND hwnd, LPARAM lParam)
{
	AfWnd * pwnd = AfWnd::GetAfWnd(hwnd);
	if (pwnd)
		pwnd->OnStylesheetChange();
	return TRUE;
}


/*----------------------------------------------------------------------------------------------
	This static callback method calls Synchronize on child AfDialogs.
	It is invoked from RecMainWnd::Synchronize.
	@param hwnd Handle to the parent window.
	@param lParam address of SyncInfo.
	@return TRUE.
----------------------------------------------------------------------------------------------*/
BOOL CALLBACK AfWnd::EnumChildSyncDialogs(HWND hwnd, LPARAM lParam)
{
	AfDialog * pdlg = dynamic_cast<AfDialog *>(AfWnd::GetAfWnd(hwnd));
	if (pdlg)
	{
		SyncInfo * psync = reinterpret_cast<SyncInfo *>(lParam);
		pdlg->Synchronize(*psync);
	}
	return TRUE;
}


/*----------------------------------------------------------------------------------------------
	This static callback method calls RefreshAll() for all the children
	in a specific window. It is invoked from AfMainWnd::OnStylesheetChange.
	(This will get called around 177 times when RefreshAll is performed on RnMainWnd.)
	@param hwnd Handle to the parent window.
	@param lParam non-zero to pass 'true' for the fReloadData parameter of RefreshAll.

	@return TRUE.
----------------------------------------------------------------------------------------------*/
BOOL CALLBACK AfWnd::EnumChildRefreshAll(HWND hwnd, LPARAM lParam)
{
	AfWnd * pwnd = AfWnd::GetAfWnd(hwnd);
	if (pwnd)
	{
		pwnd->RefreshAll(lParam != 0);
	}

	return TRUE;
}

/*----------------------------------------------------------------------------------------------

	This method works like TrackPopupMenu (supplying m_hwnd as the menu argument, and the
	required nulls for the last two arguments), except that if we are in what's this help mode,
	it instead tries to find the 'what's this' help for the selected item and displays it.  If
	it does this, it also calls ToggleHelpMode when done.  For it to work, the cid must identify
	a string containing multi-part names from which the krstWhatsThisEnabled option can be
	retrieved.

	@param hMenu handle to shortcut menu
	@param uFlags options
	@param x horizontal position
	@param y vertical position
	@param wsUser user interface writing system id.
	@param ptpm contains the area not to overlap (defaults to NULL)
----------------------------------------------------------------------------------------------*/
BOOL AfWnd::TrackPopupWithHelp(HMENU hMenu, UINT uFlags, int x, int y, int wsUser,
	TPMPARAMS * ptpm)
{
	if (AfMainWnd::InHelpMode())
	{
		// Trackit without notifing the parent and get the command id.
		bool fReturnCmd = uFlags & TPM_RETURNCMD;
		uFlags |= TPM_NONOTIFY | TPM_RETURNCMD;
		// Do this BEFORE we show the menu, so it shows normally.
		AfMainWnd::ToggleHelpMode();
		// This is dirty trick played by Windows that works because BOOL is really a short.
		BOOL cid = ::TrackPopupMenuEx(hMenu, uFlags, x, y, m_hwnd, ptpm);
		StrApp staMsg;
		AfUtil::GetResourceStr(krstWhatsThisEnabled, cid, staMsg);
		StrUni stuMsg = staMsg;
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		ITsStringPtr qtssMsg;
		qtsf->MakeStringRgch(stuMsg.Chars(), stuMsg.Length(), wsUser, &qtssMsg);
		AfContextHelpWndPtr qchw;
		qchw.Attach(NewObj AfContextHelpWnd);
		Point pt (x, y);
		qchw->Create(m_hwnd, qtssMsg, pt);
		if (fReturnCmd)
			return cid;
		else
			return cid ? TRUE : 0;
	}
	else
	{
		return ::TrackPopupMenuEx(hMenu, uFlags, x, y, m_hwnd, ptpm);
	}
}

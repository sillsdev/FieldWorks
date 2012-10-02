/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfWnd.h
Responsibility: Steve McConnel (was Darrell Zook)
Last reviewed:

When an application is shut down, the following process occurs.

	1. WM_CLOSE is sent to windows to tell them to begin the close process. By default,
	WM_CLOSE then sends DestroyWindow() to actually destroy the window and its children.

	2. When a window is destroyed, the window is removed from the screen, then WM_DESTROY is
	sent to the window. WM_DESTROY is then sent to each child window as it is being
	destroyed. Thus, child windows are still intact at the point it receives this call.

	3. WM_NCDESTROY is sent to each window to destroy the nonclient area. AfWnd catches this
	and sends OnReleasePtr() and DetachHwnd() to the window being closed. By this time
	client windows have been destroyed. OnReleasePtr() should clear any smart pointers to
	other AfWnd classes (to avoid circular references) and any COM smart pointers (to make
	sure they are cleared before CoUninitialize).

	4. AfApp::Cleanup() is called.

	5. CoUninitialize() is called from ModuleEntry::WinMain(). By this time there should not
	be any smart pointers referencing COM objects since this call releases the memory on
	some machines, thus causing a crash if the smart pointer tries to release its pointer later.

	6. The AfWnd class destructors are called and global smart pointers are released.
	Note: ComSmartPointers should have already been cleared prior to this.

	ENHANCE: Add more comments.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFWND_H
#define AFWND_H 1

class AfMainWnd;

/*----------------------------------------------------------------------------------------------
	This class wraps the standard MS Windows CREATESTRUCT data structure, adding a number of
	methods used to create a window.

	Hungarian: wcs
----------------------------------------------------------------------------------------------*/
class WndCreateStruct : public CREATESTRUCT
{
public:
	// Constructor.
	WndCreateStruct()
	{
		Init();
	}

	/*------------------------------------------------------------------------------------------
		Initialize the member variables of the CREATESTRUCT data structure.

		@param pszClassT Pointer to the window class string.
	------------------------------------------------------------------------------------------*/
	void Init(Pcsz pszClassT = NULL)
	{
		ClearItems(static_cast<CREATESTRUCT *>(this), 1);

		lpszClass = pszClassT;
		x = CW_USEDEFAULT;
		y = CW_USEDEFAULT;
		cx = CW_USEDEFAULT;
		cy = CW_USEDEFAULT;
		hInstance = ModuleEntry::GetModuleHandle();
	}

	/*------------------------------------------------------------------------------------------
		Initialize this window as an application main window.

		@param pszClassT Pointer to the window class string.
	------------------------------------------------------------------------------------------*/
	void InitMain(Pcsz pszClassT)
	{
		Init(pszClassT);

		dwExStyle = WS_EX_APPWINDOW;
		style = WS_OVERLAPPEDWINDOW | WS_CLIPCHILDREN;
	}

	/*------------------------------------------------------------------------------------------
		Initialize this window as a child window.

		@param pszClassT Pointer to the window class string.
		@param hwndParT Handle to the parent window.
		@param Window identifier for the child window.
	------------------------------------------------------------------------------------------*/
	void InitChild(Pcsz pszClassT, HWND hwndParT, int widT)
	{
		Init(pszClassT);

		style = WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS;
		hwndParent = hwndParT;
		hMenu = (HMENU)widT;
	}

	/*------------------------------------------------------------------------------------------
		Set the coordinates for this window.

		@param rcT Coordinates of the rectangular window corners.
	------------------------------------------------------------------------------------------*/
	void SetRect(const Rect & rcT)
	{
		x = rcT.left;
		y = rcT.top;
		cx = rcT.Width();
		cy = rcT.Height();
	}

	/*------------------------------------------------------------------------------------------
		Set the window identifier for this window.

		@param widT Window identifier.
	------------------------------------------------------------------------------------------*/
	void SetWid(int widT)
	{
		hMenu = (HMENU)widT;
	}

	/*------------------------------------------------------------------------------------------
		Load the indicated menu for this window.

		@param ridT Menu resource identifier.
	------------------------------------------------------------------------------------------*/
	void LoadMenu(int ridT)
	{
		hMenu = ::LoadMenu(ModuleEntry::GetModuleHandle(), MAKEINTRESOURCE(ridT));
		if (!hMenu)
			ThrowHr(WarnHr(E_FAIL));
	}
};


/*----------------------------------------------------------------------------------------------
	These are option flags for RegisterClass.
----------------------------------------------------------------------------------------------*/
enum WndClsStyle
{
	kfwcsNil = 0,
	kfwcsClassDc = CS_CLASSDC,
	kfwcsOwnDc = CS_OWNDC,
	kfwcsDblClicks = CS_DBLCLKS,
	kfwcsNoClose = CS_NOCLOSE,
	kfwcsHorzRedraw = CS_HREDRAW,
	kfwcsVertRedraw = CS_VREDRAW,
	kfwcsRedraw = CS_HREDRAW | CS_VREDRAW,

	kgrfwcsDef = kfwcsDblClicks
};


/*----------------------------------------------------------------------------------------------
	WM_SIZE action type.
----------------------------------------------------------------------------------------------*/
enum WndSizeType
{
	kwstMaximized = SIZE_MAXIMIZED,		// We've been maximized.
	kwstMinimized = SIZE_MINIMIZED,		// We've been minimized.
	kwstRestored = SIZE_RESTORED,		// We're neither maximized nor minimized.
	kwstMaxHide = SIZE_MAXHIDE,			// Someone else has been maximized so we're hidden.
	kwstMaxShow = SIZE_MAXSHOW,			// Someone else has been restored from being maximized.
};


/*----------------------------------------------------------------------------------------------
	Event IDs for OnChildEvent.
----------------------------------------------------------------------------------------------*/
enum ChildEventID
{
	kceidStartDraggingBorder = 0,
	kceidDraggingBorder = 1,
};


/*----------------------------------------------------------------------------------------------
	This is base class for all window objects in this application framework.

	Hungarian: wnd.
----------------------------------------------------------------------------------------------*/
class AfWnd : public CmdHandler
{
public:
	AfWnd(void);
	virtual ~AfWnd(void);

	//:> Public static methods.
	static void RegisterClass(Pcsz pszName, int grfwcs = kgrfwcsDef, int ridCrs = 0,
		int ridMenu = 0, int clrBack = -1, int ridIcon = 0, int ridIconSmall = 0);
	static AfWnd * GetAfWnd(HWND hwnd);

	virtual void CreateHwnd(WndCreateStruct & wcs);
	virtual void CreateAndSubclassHwnd(WndCreateStruct & wcs);

	virtual void Show(int nShow);
	virtual void DestroyHwnd(void);

	/*------------------------------------------------------------------------------------------
		Retrieve the coordinates of this window's client area.  This calls the Windows user
		interface function of the same name.

		@param rc Reference to a data structure for returning the coordinates.
	------------------------------------------------------------------------------------------*/
	virtual void GetClientRect(Rect & rc)
	{
		::GetClientRect(m_hwnd, &rc);
	}

	/*------------------------------------------------------------------------------------------
		Return a string containing the help string for the window at the specified point.
		This is called in response to a WM_LBUTTONDOWN message intercepted by
		AfMainWnd::HelpMsgHook.

		@param pt Screen location of a left mouse click relative to the screen.
		@param pptss Address of a pointer to an ITsString COM object for returning the help
						string.

		@return True if successful, false if no help string is available for the given screen
						location.
	------------------------------------------------------------------------------------------*/
	virtual bool GetHelpStrFromPt(Point pt, ITsString ** pptss)
	{
		return false;
	}

	//:> Override these two methods to store and load user-customizable settings
	//:> specific to the subclassed window.
	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	virtual void SaveSettings(const achar * pszRoot, bool fRecursive = true);

	void LoadWindowPosition(const achar * pszRoot, const achar * pszValue);
	void SaveWindowPosition(const achar * pszRoot, const achar * pszValue);

	/*------------------------------------------------------------------------------------------
		Process a WM_MEASUREITEM message.

		@param pmis Pointer to a data structure for returning the dimensions of an owner-drawn
						control or menu item.  (lParam from the window procedure)

		@return True if the message is handled, otherwise false.
	------------------------------------------------------------------------------------------*/
	virtual bool OnMeasureThisItem(MEASUREITEMSTRUCT * pmis)
	{
		return false;
	}
	/*------------------------------------------------------------------------------------------
		Process a WM_DRAWITEM message.

		@param pdis Pointer to a data structure containing information needed to paint an
						owner-drawn control or menu item.  (lParam from the window procedure)

		@return True if the message is handled, otherwise false.
	------------------------------------------------------------------------------------------*/
	virtual bool OnDrawThisItem(DRAWITEMSTRUCT * pdis)
	{
		return false;
	}
	/*------------------------------------------------------------------------------------------
		Process a WM_NOTIFY message.

		@param id Identifier of the common control sending the message.  (wParam from the window
						procedure)
		@param pnmh Pointer to an NMHDR structure containing the notification code and
						additional information.  (lParam from the window procedure)
		@param lnRet Value to be returned to the system.  (return value for the window
						procedure)

		@return True if the message is handled, otherwise false.
	------------------------------------------------------------------------------------------*/
	virtual bool OnNotifyThis(int id, NMHDR * pnmh, long & lnRet)
	{
		return false;
	}
	/*------------------------------------------------------------------------------------------
		Process a WM_SETFOCUS message.

		@return True if the message is handled, otherwise false.
	------------------------------------------------------------------------------------------*/
	virtual bool OnSetFocus()
	{
		return false;
	}
	/*------------------------------------------------------------------------------------------
		Processes events from child windows. This is sort of like a message handler but child
		windows have to explicitly call their parent's OnChildEvent virtual function to pass
		events -- e.g. (AfWnd *)Parent()->OnChildEvent(...). Events can actually be window
		messages received by a child window which the child wants to pass on to his parent.
		However they don't have to be. Events passed to a parent can be	anything the user can
		think of. Event ids should be added to the ChildEventID	enumeration list. An example of
		how this virtual function is used can be found in AfCaptionBar, AfHeaderWnd. When a
		subclass of AfHeaderWnd that contains a caption bar needs to be sized by dragging the
		top of the caption bar, the caption bar	needs to inform it's AfHeaderWnd that the user
		is hovering over the top border and when he holds down the primary mouse button. The
		AfHeaderWnd, in turn, needs to inform it's parent window that it's being sized. The
		parent has the option of changing the sizes and positions of siblings of the
		AfHeaderWnd.

		@param ceid	An event id defined in the ChildEventID enumeration in AfWnd.h
		@param pAfWnd A pointer to the child window sending the event.
		@param lpInfo A pointer to any extra data the child wishes to send its parent.

		@return True by default. The importance of what's returned is up to the child
				sending the event and the parent receiving it.
	------------------------------------------------------------------------------------------*/
	virtual bool OnChildEvent(int ceid, AfWnd *pAfWnd, void *lpInfo = NULL)
	{
		return true;
	}
	/*------------------------------------------------------------------------------------------
		Retrieve the parameters of a scroll bar, including the minimum and maximum scrolling
		positions, the page size, and the position of the scroll box.  This calls the Windows
		user interface function of the same name.

		@param nBar Scroll bar type (SB_CTL, SB_HORZ, or SB_VERT).
		@param psi Pointer to a data structure for returning the scroll bar information.

		@return True if successful, otherwise false.
	------------------------------------------------------------------------------------------*/
	virtual bool GetScrollInfo(int nBar, SCROLLINFO * psi)
	{
		AssertPtr(psi);
		return ::GetScrollInfo(m_hwnd, nBar, psi);
	}
	/*------------------------------------------------------------------------------------------
		Set the parameters of a scroll bar, including the minimum and maximum scrolling
		positions, the page size, and the position of the scroll box.  This calls the Windows
		user interface function of the same name.

		@param nBar Scroll bar type (SB_CTL, SB_HORZ, or SB_VERT).
		@param psi Pointer to a data structure containing the scroll bar information.
		@param fRedraw Flag whether the scroll bar is to be redrawn to reflect the new settings.

		@return The current position of the scroll box.
	------------------------------------------------------------------------------------------*/
	virtual int SetScrollInfo(int nBar, SCROLLINFO * psi, bool fRedraw)
	{
		AssertPtr(psi);
		return ::SetScrollInfo(m_hwnd, nBar, psi, fRedraw);
	}

	//:> Find the parent window (or NULL, if no parent or parent is not an AfWnd)
	AfWnd * Parent();
	//:> Find the MainWindow class in your chain of parents (or null)
	AfMainWnd * MainWindow();
	// Refresh everything.
	virtual void RefreshAll(bool fReloadData);
	static BOOL CALLBACK EnumChildRefreshAll(HWND hwnd, LPARAM lParam);
	// Reload data and redraw views and child dialogs. Override on subclasses.
	virtual bool FullRefresh()
	{
		return true;
	}


#ifdef DEBUG
	/*------------------------------------------------------------------------------------------
		Perform some basic sanity checks.  This is used in Assert, and typically consists of a
		a series of Asserts.

		@return True if everything seems okay, otherwise false.
	------------------------------------------------------------------------------------------*/
	bool AssertValid(void)
	{
		AssertPtr(this);
		Assert(m_nMagic == knMagicWnd);
		Assert(!m_hwnd || ::IsWindow(m_hwnd));
		Assert(!m_wnpDefWndProc || m_hwnd != NULL);
		return true;
	}
#endif // DEBUG

	//:>****************************************************************************************
	//:> These methods need to be public for the benefit of the ATL control class
	//:>****************************************************************************************

	//:> This is where we put calls to standard handlers. Note that this is NOT virtual.
	//:> The handlers should all be virtual.
	bool FWndProcPre(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	/*------------------------------------------------------------------------------------------
		Main window procedure.  Override this to get window messages.  Note that if a standard
		handler returns true (eg, OnSize), this is not called. Return false to call the default
		window procedure.  Note this return is not the value returned to Windows as specified by
		the platform SDK.  That value should be stored in lnRet prior to returning.

		@param wm Windows message id.
		@param wp First message parameter -- usage depends on message.
		@param lp Second message parameter -- usage depends on message.
		@param lnRet Value to be returned to the system.  (return value for window procedure)

		@return True if the message is handled, otherwise false.
	------------------------------------------------------------------------------------------*/
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
	{
		return false;
	}

	virtual void AttachHwnd(HWND hwnd);

	BOOL TrackPopupWithHelp(HMENU hMenu, UINT uFlags, int x, int y, int wsUser,
		TPMPARAMS * ptpm = NULL);

protected:
	/*------------------------------------------------------------------------------------------
		This stores the split and scroll information for a window.

		Hungarian: wndset
	------------------------------------------------------------------------------------------*/
	struct WndSettings
	{
		StrUni vstuName;
		int viTreeW;
		int viTopP;
		SCROLLINFO siT;
		SCROLLINFO siB;

		// Constructor.
		WndSettings()
		{
			ClearBytes(&siT, isizeof(siT));
			ClearBytes(&siB, isizeof(siB));
			siT.cbSize = isizeof(siT);
			siT.fMask = SIF_POS;
			siB.cbSize = isizeof(siB);
			siB.fMask = SIF_POS;
		}
	};

	/*------------------------------------------------------------------------------------------
		This contains data used to initialize windows by calling Windows functions such as
		::CreateWindowEx (LPVOID lpParam) or ::CreateDialogParam (LPARAM dwInitParam).

		Hungarian: afwc
	------------------------------------------------------------------------------------------*/
	struct AfWndCreate
	{
		AfWnd * pwnd;	// Pointer to the AfWnd (or subclass) object (ie, set to "this").
		void * pv;		// User-defined data.
	};

	/*------------------------------------------------------------------------------------------
		This contains data passed to ::EnumChildWindows by either SaveSettings or LoadSettings
		in order to save or load the settings associated with every child window.

		Hungarian: si
	------------------------------------------------------------------------------------------*/
	struct SettingInfo
	{
		// The string this application uses as the root for all registry entries.
		achar * pszRoot;
		// Flag whether to call SaveSettings or LoadSettings.
		bool fSave;
	};

	enum { knMagicWnd = 'AWnd' };
	int m_nMagic;				// Used to assert on and in GetAfWnd.
	WNDPROC m_wnpDefWndProc;	// Address of the default window procedure.

	AfMenuMgr * m_pmum;			// Needed when no AfApp / AfMainWindow exists.

	//:> Protected static methods.

	//:> Main window proc.
	static LRESULT CALLBACK WndProc(HWND hwnd, uint wm, WPARAM wp, LPARAM lp);
	//:> Callback proc for enumerating the child windows.
	static BOOL CALLBACK EnumChildProc(HWND hwnd, LPARAM lParam);

	//:> This should just call the appropriate default window proc (DefWindowProc, DefFrameProc,
	//:> DefMDIChildProc or the subclassed window proc).
	virtual LRESULT DefWndProc(uint wm, WPARAM wp, LPARAM lp);

	//:> This is called after FWndProcPre and FWndProc have been called.
	//:> Note that it's NOT virtual. This handles calling the default window proc (if
	//:> appropriate) and detaching HWNDs on WM_NCDESTROY messages.
	void AfWnd::WndProcPost(int fRet, HWND hwnd, uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	//:>****************************************************************************************
	//:>	Attaching and detaching an HWND to the AfWnd.
	//:>****************************************************************************************

	virtual void SubclassHwnd(HWND hwnd);
	virtual void DetachHwnd(HWND hwnd);

	/*------------------------------------------------------------------------------------------
		Finish initializing the window immediately after it is created before other events
		happen.
	------------------------------------------------------------------------------------------*/
	virtual void PostAttach(void)
	{
		AssertObj(this);
		Assert(m_hwnd != NULL);
	}

	/*------------------------------------------------------------------------------------------
		Finish initializing before the window is actually created, possibly by adjusting the
		window style or dimensions stored in cs.

		@param cs Pointer to the data used in creating the window.
	------------------------------------------------------------------------------------------*/
	virtual void PreCreateHwnd(CREATESTRUCT & cs)
	{
		AssertObj(this);
		AssertPsz(cs.lpszClass);
	}


	//:>****************************************************************************************
	//:>	Message handlers.
	//:>****************************************************************************************

	//:> Default handler creates a Cmd and dispatches it.
	virtual bool OnCommand(int cid, int nc, HWND hctl);
	//:> Default handler calls the command dispatcher to set menu state.
	virtual bool OnInitMenuPopup(HMENU hmenu, int ihmenu, bool fSysMenu);

	/*------------------------------------------------------------------------------------------
		This is called when the WM_NCDESTROY message is handled.  The class should clear any
		smart pointers to other AfWnd classes to avoid circular references, and if it is a
		window that closes when the application closes, it must also clear any member variable
		COM smart pointers to make sure they are cleared before CoUninitialize.
	------------------------------------------------------------------------------------------*/
	virtual void OnReleasePtr()
	{
		//:> See more details on closing windows in the file header above.
	}
	/*------------------------------------------------------------------------------------------
		Process the WM_PAINT message to paint part of a window.

		@param hdc NULL with WM_PAINT, but otherwise it could be a DC to use for painting.

		@return True if the message is processed and should not be passed on.
	------------------------------------------------------------------------------------------*/
	virtual bool OnPaint(HDC hdc)
	{
		return false;
	}
	/*------------------------------------------------------------------------------------------
		Process the WM_SIZE message when the window size has changed.

		@param wst Flag specifying the type of resizing requested.
		@param dxp New width of the client area in pixesl.
		@param dyp New height of the client area in pixels.

		@return True if the message is processed and should not be passed on.
	------------------------------------------------------------------------------------------*/
	virtual bool OnSize(int wst, int dxp, int dyp)
	{
		return false;
	}
	/*------------------------------------------------------------------------------------------
		Resize / reposition the client area.  This is also called to help handle to WM_SIZE
		message.

		@return True if the message is processed and should not be passed on.
	------------------------------------------------------------------------------------------*/
	virtual bool OnClientSize(void)
	{
		return false;
	}
	/*------------------------------------------------------------------------------------------
		Process a WM_CONTEXTMENU message or an NM_RCLICK notification.

		@param hwnd Handle to the menu's owner's window.
		@param pt Screen location to position the menu at.

		@return True if the message is handled, otherwise false.
	------------------------------------------------------------------------------------------*/
	virtual bool OnContextMenu(HWND hwnd, Point pt)
	{
		return false;
	}

	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnMeasureChildItem(MEASUREITEMSTRUCT * pmis);
	virtual bool OnDrawChildItem(DRAWITEMSTRUCT * pdis);

	/*------------------------------------------------------------------------------------------
		Process a WM_MENUSELECT message.

		@param cid Menu item identifier or submenu index.
		@param grfmf Menu flag bits: MF_BITMAP, MF_CHECKED, MF_DISABLED, ...
		@param hMenu Handle to the menu that was clicked.

		@return True if the message is handled, otherwise false.
	------------------------------------------------------------------------------------------*/
	virtual bool OnMenuSelect(int cid, uint grfmf, HMENU hMenu)
	{
		return false;
	}
	/*------------------------------------------------------------------------------------------
		Process a WM_HELP message.

		@param phi Pointer to a data structure that contains information about the menu item,
						control, dialog box, or window for which help is wanted.

		@return True if the message is handled, otherwise false.
	------------------------------------------------------------------------------------------*/
	virtual bool OnHelpInfo(HELPINFO * phi)
	{
		return false;
	}

	virtual void OnStylesheetChange();
	static BOOL CALLBACK EnumChildSsChange(HWND hwnd, LPARAM lParam);
	static BOOL CALLBACK EnumChildSyncDialogs(HWND hwnd, LPARAM lParam);
};

typedef GenSmartPtr<AfWnd> AfWndPtr;


#endif // !AFWND_H

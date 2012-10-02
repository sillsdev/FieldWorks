/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfBars.cpp
Responsibility: Steve McConnel (was Darrell Zook)
Last reviewed:

Description:
	This file contains class definitions for the following classes:
		AfToolBarCombo : TssComboEx
		AfToolBarEdit : TssEdit
		AfToolBarToolTip : AfWnd
		AfToolBar : AfWnd
		AfReBar : AfWnd
		AfStatusBar : AfWnd
		AfProgressBar : AfWnd
		AfMenuBar : AfToolBar
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE
//:End Ignore

#define CAN_MOVE_BUTTONS 0

const int knToolTipTimer = 7;

ToolBarGlobals g_tbg;

uint AfMenuBar::s_wmShowMenu = ::RegisterWindowMessage(_T("AfMenuBar ShowMenu"));
HHOOK AfMenuBar::s_hhook;
AfMenuBarPtr AfMenuBar::s_qmnbrCur;
HMENU AfMenuBar::s_hmenuLastPopup;
HMENU AfMenuBar::s_hmenuOld;
bool AfMenuBar::s_fOverSubMenu;
bool AfMenuBar::s_fIgnoreMouseMove;

//#define kcidComboTypeAhead				29005 // not used in resource, trick in AfBars.

//:>********************************************************************************************
//:>	AfToolBarGlobals methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
ToolBarGlobals::ToolBarGlobals()
{
	m_fDragging = false;
	m_hwndOld = NULL;
	m_hwndChild = NULL;
	m_iSrcButton = -1;
	m_iDstButton = -1;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
ToolBarGlobals::~ToolBarGlobals()
{
}


//:>********************************************************************************************
//:>	AfToolBar methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfToolBar::AfToolBar()
{
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfToolBar::~AfToolBar()
{
}

/*----------------------------------------------------------------------------------------------
	Initialize the tool bar window with its window ID, resource ID, and name.

	@param wid Window ID for the toolbar.
	@param rid Resource ID for the toolbar.
	@param pszName Name of the toolbar.
----------------------------------------------------------------------------------------------*/
void AfToolBar::Initialize(int wid, int rid, const achar * pszName)
{
	AssertPsz(pszName);

	m_wid = wid;
	m_rid = rid;
	m_str = pszName;
}

/*----------------------------------------------------------------------------------------------
	Create the tool bar window.

	@param pafw Pointer to the application's main window, which is the toolbar's parent.
----------------------------------------------------------------------------------------------*/
void AfToolBar::Create(AfMainWnd * pafw)
{
	AssertObj(this);
	AssertPtr(pafw);
	Assert(!m_hwnd);

	INITCOMMONCONTROLSEX iccex = { sizeof(iccex), ICC_BAR_CLASSES };
	InitCommonControlsEx(&iccex);

	WndCreateStruct wcs;

	wcs.InitChild(TOOLBARCLASSNAME, pafw->Hwnd(), m_wid);
	wcs.style |= TBSTYLE_FLAT | TBSTYLE_TOOLTIPS | CCS_NODIVIDER | CCS_NOPARENTALIGN |
		CCS_NORESIZE;

	CreateAndSubclassHwnd(wcs);
	Assert(m_hwnd != NULL);

	DWORD style = ::GetWindowLong(m_hwnd, GWL_STYLE);
	::SetWindowLong(m_hwnd, GWL_STYLE, style & ~TBSTYLE_TRANSPARENT);
	::ShowWindow(m_hwnd, SW_SHOW);

	m_qafw = pafw;
	::SendMessage(m_hwnd, TB_BUTTONSTRUCTSIZE, isizeof(TBBUTTON), 0);
	::SendMessage(m_hwnd, TB_SETEXTENDEDSTYLE, 0, TBSTYLE_EX_DRAWDDARROWS);

	::SendMessage(m_hwnd, WM_SETFONT, (WPARAM)::GetStockObject(DEFAULT_GUI_FONT), true);

	Load();
}

/*----------------------------------------------------------------------------------------------
	Load the toolbar settings.
----------------------------------------------------------------------------------------------*/
void AfToolBar::Load()
{
	AssertObj(this);
	Assert(m_hwnd);
	Assert(m_rid);

#if 0
	FwSettings * pfs;
	pfs = AfApp::GetSettings(); // Temp separated from above so Release build will work.
	AssertPtr(pfs);
	StrAppBufPath strbp;
	strbp.Format(_T("Toolbar-%d"), m_rid);

	StrApp str;
	// TODO DarrellZ: Remove this once we want to be able to load customizable toolbars.
	// Currently, it saves the toolbar state out, and this will load it back in. I commented
	// this out for now, though, because new buttons added to existing toolbars in the
	// resource files would not show up at runtime and it might be kind of confusing.
	if (pfs->GetString(NULL, strbp.Chars(), str))
	{
		Vector<ushort> vcid;
		const achar * pch = str.Chars();
		while (pch[0] && pch[1])
		{
			ushort nT = (ushort)strtol(pch, &pch, 0);
			vcid.Push(nT);
		}
		SetButtons(vcid.Begin(), vcid.Size());
	}
	else
#endif
	{
		HINSTANCE hinst = ModuleEntry::GetModuleHandle();
		HRSRC hrsrc;
		ToolBarData * ptbd;

		// Load toolbar resource.
		if (NULL == (hrsrc = ::FindResource(hinst, MAKEINTRESOURCE(m_rid), RT_TOOLBAR)) ||
			NULL == (ptbd = (ToolBarData *)LoadResource(hinst, hrsrc)))
		{
			Warn("Can't load toolbar!");
			ThrowHr(WarnHr(E_FAIL));
		}
		Assert(ptbd->suVer == 1);
		Assert(ptbd->dxsBmp == kdxpBmp);
		Assert(ptbd->dysBmp == kdypBmp);
		SetButtons(ptbd->rgcid, ptbd->ccid);
	}
	// Set new sizes of the buttons.
	::SendMessage(m_hwnd, TB_SETBITMAPSIZE, 0, MAKELONG(kdxpBmp, kdypBmp));
	::SendMessage(m_hwnd, TB_SETBUTTONSIZE, 0, MAKELONG(kdxpBmp + 7, kdypBmp + 7));

	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	::SendMessage(m_hwnd, TB_SETIMAGELIST, 0, (LPARAM)pafw->GetMenuMgr()->GetImageList());
}

/*----------------------------------------------------------------------------------------------
	Save the toolbar settings.
----------------------------------------------------------------------------------------------*/
void AfToolBar::Save()
{
	FwSettings * pfs = AfApp::GetSettings();
	AssertPtr(pfs);

	StrAppBufPath strbp;
	strbp.Format(_T("Toolbar-%d"), m_rid);

	// Create a string that contains the command IDs of each of the buttons located on the
	// toolbar separated by spaces.
	StrApp str;
	int cbtn = ::SendMessage(m_hwnd, TB_BUTTONCOUNT, 0, 0);
	for (int ibtn = 0; ibtn < cbtn; ibtn++)
	{
		TBBUTTONINFO tbbi = { isizeof(tbbi), TBIF_BYINDEX | TBIF_COMMAND };
		if (-1 == ::SendMessage(m_hwnd, TB_GETBUTTONINFO, ibtn, (long)&tbbi))
			ThrowHr(WarnHr(E_FAIL));

		str.FormatAppend(_T("%d "), tbbi.idCommand);
	}
	pfs->SetString(NULL, strbp.Chars(), str);
}

/*----------------------------------------------------------------------------------------------
	Set the buttons for the toolbar.

	@param prgcid Pointer to the array of button command IDs.
	@param ccid Number of button command IDs in the array.
----------------------------------------------------------------------------------------------*/
void AfToolBar::SetButtons(ushort * prgcid, int ccid)
{
	AssertObj(this);
	Assert(m_hwnd);
	Assert(ccid > 0);
	AssertArray(prgcid, ccid);

	Vector<TBBUTTON> vtbtn;
	vtbtn.Resize(ccid);
	ClearItems(vtbtn.Begin(), ccid);

	// Add new buttons.
	int icid;
	int dxsSep = (GetStyle() & TBSTYLE_FLAT) ? 6 : 8;

	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfMenuMgr * pmum = pafw->GetMenuMgr();
	AssertPtr(pmum);

	for (icid = 0; icid < ccid; icid++)
	{
		TBBUTTON & tbtn = vtbtn[icid];

		tbtn.iString = -1;
		tbtn.fsState = TBSTATE_ENABLED;
		tbtn.idCommand = prgcid[icid];

		if (!tbtn.idCommand)
		{
			// Separator.
			tbtn.fsStyle = TBSTYLE_SEP;
			tbtn.iBitmap = dxsSep;
		}
		else
		{
			// A command button with image.
			tbtn.fsStyle = TBSTYLE_BUTTON;
			tbtn.iBitmap = pmum->GetImagFromCid(tbtn.idCommand);
			Assert(tbtn.iBitmap != -1);
		}
	}

	// REVIEW ShonK: Should we check for failure?
	::SendMessage(m_hwnd, TB_ADDBUTTONS, ccid, (LPARAM)vtbtn.Begin());

	for (icid = 0; icid < ccid; icid++)
		pafw->OnToolBarButtonAdded(this, icid, vtbtn[icid].idCommand);
}

/*----------------------------------------------------------------------------------------------
	Create a combo box and attach it to the button with the given ID on a toolbar.

	@param pptbc Address of a pointer to the created toolbar child.
	@param cid Command id for the combo box control.
	@param nWidth Width of the combo box button in pixels.
	@param dypDropdown Height of the dropdown list for the combo box.
	@param fTypeAhead Is this a type ahead combo box?
----------------------------------------------------------------------------------------------*/
void AfToolBar::SetupComboControl(AfToolBarCombo ** pptbc, int cid, int nWidth,
	int dypDropdown, bool fTypeAhead)
{
	AssertPtrN(pptbc);

	// Make the button that is to be the combo-control a separator, and resize it.
	int iButton = ::SendMessage(m_hwnd, TB_COMMANDTOINDEX, cid, 0);
	Assert(-1 != iButton);
	TBBUTTONINFO tbbi = { isizeof(tbbi), TBIF_STYLE | TBIF_SIZE };
	tbbi.fsStyle = TBSTYLE_SEP;
	tbbi.cx = (WORD)nWidth;
	::SendMessage(m_hwnd, TB_SETBUTTONINFO, cid, (long)&tbbi);

	// Get a handle to the tooltip control for the toolbar.
	HWND hwndTool = (HWND)::SendMessage(m_hwnd, TB_GETTOOLTIPS, 0, 0);
	AfToolBarToolTipPtr qtbtt = dynamic_cast<AfToolBarToolTip *>(AfWnd::GetAfWnd(hwndTool));
	if (!qtbtt)
	{
		qtbtt.Create();
		qtbtt->Subclass(hwndTool, m_hwnd);
	}

	// Create the combo-control itself, reposition it in the client area and show it.
	Rect rc;
	::SendMessage(m_hwnd, TB_GETITEMRECT, iButton, (long)&rc);
	rc.bottom = ++rc.top + dypDropdown;
	rc.left += 2;
	rc.right += 2;
	DWORD dwStyle = CBS_DROPDOWN;
	AfToolBarComboPtr qtbc;
	qtbc.Create();
	qtbc->Create(m_hwnd, dwStyle, cid, iButton, rc, hwndTool, true);
	qtbc->SetWs(m_wsUser);

	if (pptbc)
		*pptbc = qtbc.Detach();
}

/*----------------------------------------------------------------------------------------------
	Create an edit box and attach it to the button with the given ID on a toolbar.

	NOTE: This method does not work!

	@param pptbe	Address of a pointer to the created toolbar child.
	@param cid		Command id for the combo box control.
----------------------------------------------------------------------------------------------*/
void AfToolBar::SetupEditControl(AfToolBarEdit ** pptbe, int cid, int nWidth)
{
	AssertPtrN(pptbe);

	// Make the button that is to be the edit-box a separator, and resize it.
	int iButton = ::SendMessage(m_hwnd, TB_COMMANDTOINDEX, cid, 0);
	Assert(-1 != iButton);
	TBBUTTONINFO tbbi = { isizeof(tbbi), TBIF_STYLE | TBIF_SIZE };
	tbbi.fsStyle = TBSTYLE_SEP;
	tbbi.cx = (WORD)nWidth;
	::SendMessage(m_hwnd, TB_SETBUTTONINFO, cid, (long)&tbbi);

	// Get a handle to the tooltip control for the toolbar.
	HWND hwndTool = (HWND)::SendMessage(m_hwnd, TB_GETTOOLTIPS, 0, 0);
	AfToolBarToolTipPtr qtbtt = dynamic_cast<AfToolBarToolTip *>(AfWnd::GetAfWnd(hwndTool));
	if (!qtbtt)
	{
		qtbtt.Create();
		qtbtt->Subclass(hwndTool, m_hwnd);
	}

	// Create the edit-control itself, reposition it in the client area and show it.
	Rect rc;
	::SendMessage(m_hwnd, TB_GETITEMRECT, iButton, (long)&rc);
	DWORD dwStyle = WS_CHILD | ES_LEFT | ES_AUTOHSCROLL | ES_NOHIDESEL;
	AfToolBarEditPtr qtbe;
	qtbe.Create();
	// For now the only edit box that shows tags is the quick find edit box.
	qtbe->SetShowTags(cid == kcidEditSrchQuick);

	Assert(m_wsUser);
	qtbe->Create(m_hwnd, dwStyle, cid, iButton, rc, hwndTool, true, m_qwsf, m_wsUser);
	qtbe->SetUserWs(m_wsUser);

	if (pptbe)
		*pptbe = qtbe.Detach();
}

/*----------------------------------------------------------------------------------------------
	Make sure any children are in the right position.
----------------------------------------------------------------------------------------------*/
void AfToolBar::UpdateToolBar()
{
#if CAN_MOVE_BUTTONS
	Assert(g_tbg.m_fDragging);
	Assert(g_tbg.m_hwndOld);

	if (g_tbg.m_hwndChild)
	{
		Rect rc;
		::SendMessage(g_tbg.m_hwndOld, TB_GETITEMRECT, g_tbg.m_iDstButton, (long)&rc);
		Rect rcChild;
		::GetWindowRect(g_tbg.m_hwndChild, &rcChild);

		::SetParent(g_tbg.m_hwndChild, g_tbg.m_hwndOld);
		::MoveWindow(g_tbg.m_hwndChild, rc.left, rc.top, rcChild.Width(), rcChild.Height(),
			true);

		AfToolBarComboPtr qtbc;
		qtbc = dynamic_cast<AfToolBarCombo *>(AfWnd::GetAfWnd(g_tbg.m_hwndChild));
		AssertObj(qtbc);
		qtbc->SetButtonIndex(g_tbg.m_iDstButton);
	}

	// Update the position of the child windows in the toolbar that was dragged from.
	// Then update the position of the child windows in the toolbar that was dropped on.
	HWND hwnd = ::GetWindow(m_hwnd, GW_CHILD);
	HWND hwndTool = m_hwnd;
	for (int i = 0; i < 2; i++)
	{
		while (hwnd)
		{
			if (hwnd != g_tbg.m_hwndChild)
			{
				AfToolBarComboPtr qtbc = dynamic_cast<AfToolBarCombo *>(AfWnd::GetAfWnd(hwnd));
				AssertObj(qtbc);

				int iButton = qtbc->GetButtonIndex();
				if (g_tbg.m_iSrcButton <= iButton)
					qtbc->SetButtonIndex(i == 0 ? --iButton : ++iButton);
				if (m_hwnd == g_tbg.m_hwndOld)
				{
					if (g_tbg.m_iDstButton <= iButton)
						qtbc->SetButtonIndex(++iButton);
				}

				Rect rc;
				::SendMessage(hwndTool, TB_GETITEMRECT, iButton, (long)&rc);

				Rect rcChild;
				::GetWindowRect(hwnd, &rcChild);
				::MoveWindow(hwnd, rc.left, rc.top, rcChild.Width(), rcChild.Height(), true);
			}
			hwnd = ::GetWindow(hwnd, GW_HWNDNEXT);
		}

		if (m_hwnd == g_tbg.m_hwndOld)
			break;
		hwndTool = g_tbg.m_hwndOld;
		hwnd = ::GetWindow(hwndTool, GW_CHILD);
	}
#endif
}

/*----------------------------------------------------------------------------------------------
	Return a string containing the help string for the button at the specified point.

	@param pt Screen location of a mouse click.
	@param pptss Address of a pointer to an ITsString COM object for returning the help string.

	@return True if successful, false if no help string is available for the given screen
					location.
----------------------------------------------------------------------------------------------*/
bool AfToolBar::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	Point ptClient(pt);
	::ScreenToClient(m_hwnd, &ptClient);

	int iButton = ::SendMessage(m_hwnd, TB_HITTEST, 0, (LPARAM)&ptClient);
	if (iButton >= 0)
	{
		// We have hit one of the buttons on the toolbar.
		TBBUTTON tbb;
		::SendMessage(m_hwnd, TB_GETBUTTON, iButton, (LPARAM)&tbb);
		StrApp str;
		if (!AfUtil::GetResourceStr(krstWhatsThisEnabled, tbb.idCommand, str))
			str.Load(kstidNoHelpError); // No context help available
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		StrUni stu(str);
		Assert(m_wsUser);
		CheckHr(qtsf->MakeString(stu.Bstr(), m_wsUser, pptss));
		return true;
	}
	else
	{
		// See if we are on a child window and if so, ask it for its help string.
		// NOTE: This only looks one level deep. It does not find grandchildren
		// (like the edit box inside a combo box.
		// WARNING: It is very important to check to make sure that hwnd isn't the
		// current toolbar window. Otherwise you'll get infinite recursion.
		HWND hwnd = ::ChildWindowFromPoint(m_hwnd, ptClient);
		if (hwnd != m_hwnd)
		{
			AfWnd * pwnd = AfWnd::GetAfWnd(hwnd);
			if (pwnd)
			{
				AssertObj(pwnd);
				if (pwnd->GetHelpStrFromPt(pt, pptss))
					return true;
			}
		}
	}

	return SuperClass::GetHelpStrFromPt(pt, pptss);
}

/*----------------------------------------------------------------------------------------------
	Begin a button drag/drop operation.

	@param pt Screen location where the mouse button was first pushed.

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfToolBar::OnDragBegin(Point pt)
{
#if CAN_MOVE_BUTTONS
	g_tbg.m_fDragging = true;

	// Find the current button we are over.
	g_tbg.m_iSrcButton = abs(::SendMessage(m_hwnd, TB_HITTEST, 0, (LPARAM)&pt));

	::SetFocus(m_hwnd);
	::SetCapture(m_hwnd);
#endif
	return true;
}

/*----------------------------------------------------------------------------------------------
	Determine which toolbar window (if any) the mouse is over, and show the insertion mark at
	the appropriate place.

	@param pt Screen location where the mouse cursor currently is displayed.

	@return True if the mouse cursor is over a toolbar and an insertion mark is displayed,
					otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfToolBar::OnDragOver(Point pt)
{
#if CAN_MOVE_BUTTONS
	::ClientToScreen(m_hwnd, &pt);

	HWND hwnd = ::WindowFromPoint(pt);
	AfToolBarPtr qtlb;
	while (hwnd)
	{
		qtlb = dynamic_cast<AfToolBar *>(AfWnd::GetAfWnd(hwnd));
		if (qtlb)
			break;
		hwnd = ::GetParent(hwnd);
	}

	// If we are over a new toolbar, remove the insertion mark from the old toolbar.
	if (hwnd != g_tbg.m_hwndOld)
	{
		TBINSERTMARK tbim = { -1 };
		::SendMessage(g_tbg.m_hwndOld, TB_SETINSERTMARK, 0, (LPARAM)&tbim);
		g_tbg.m_hwndOld = hwnd;
	}

	if (qtlb)
	{
		TBINSERTMARK tbim = { 0 };
		::ScreenToClient(hwnd, &pt);

		// ENHANCE DarrellZ: Add this if we want to support copying buttons.
		/*::SetCursor(::LoadCursor(ModuleEntry::GetModuleHandle(),
			MAKEINTRESOURCE(AfApp::GrfmstCur() & kfmstCtrl ? kridTBarCopy : kridTBarMove)));*/
		::SetCursor(::LoadCursor(ModuleEntry::GetModuleHandle(),
			MAKEINTRESOURCE(kridTBarMove)));

		// Find the current button we are over.
		g_tbg.m_iDstButton = ::SendMessage(hwnd, TB_HITTEST, 0, (LPARAM)&pt);
		if (g_tbg.m_iDstButton < 0)
			g_tbg.m_iDstButton = -g_tbg.m_iDstButton - 1;

		// If we are past the end of the toolbar, set tbim.iButton to the index
		// of the last button.
		int cButtons = ::SendMessage(hwnd, TB_BUTTONCOUNT, 0, 0);
		if (g_tbg.m_iDstButton >= cButtons)
		{
			tbim.iButton = cButtons - 1;
			tbim.dwFlags = TBIMHT_AFTER;
		}
		else
		{
			Rect rc;
			::SendMessage(hwnd, TB_GETITEMRECT, g_tbg.m_iDstButton, (LPARAM)&rc);

			tbim.iButton = g_tbg.m_iDstButton;
			if (pt.x > rc.left + rc.Width() / 2)
			{
				tbim.dwFlags = TBIMHT_AFTER;
				g_tbg.m_iDstButton++;
			}
		}
		::SendMessage(hwnd, TB_SETINSERTMARK, 0, (LPARAM)&tbim);
		return true;
	}
	::SetCursor(::LoadCursor(ModuleEntry::GetModuleHandle(), MAKEINTRESOURCE(kridTBarNoDrop)));

#endif
	return false;
}

/*----------------------------------------------------------------------------------------------
	If the cursor is not over a toolbar, delete the button.
	If the cursor is over a toolbar, move or copy the button from the old toolbar to the new
	one.

	@param pt Screen location where the mouse button is released.

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfToolBar::OnDragEnd(Point pt)
{
#if CAN_MOVE_BUTTONS
	::ReleaseCapture();

	if (g_tbg.m_hwndOld)
	{
		TBINSERTMARK tbim = { -1, 0 };
		::SendMessage(g_tbg.m_hwndOld, TB_SETINSERTMARK, 0, (LPARAM)&tbim);

		// Get button information from the original toolbar.
		TBBUTTON tbb;
		::SendMessage(m_hwnd, TB_GETBUTTON, g_tbg.m_iSrcButton, (LPARAM)&tbb);

		if (g_tbg.m_hwndChild)
		{
			Rect rc;
			::SendMessage(m_hwnd, TB_GETITEMRECT, g_tbg.m_iSrcButton, (long)&rc);
			tbb.iBitmap = rc.Width();
		}

		if (m_hwnd == g_tbg.m_hwndOld)
		{
			if (g_tbg.m_iDstButton > g_tbg.m_iSrcButton)
				g_tbg.m_iDstButton--;
			::SendMessage(m_hwnd, TB_MOVEBUTTON, g_tbg.m_iSrcButton, g_tbg.m_iDstButton);
		}
		else
		{
			// Insert the new button at the final destination on the new toolbar.
			::SendMessage(g_tbg.m_hwndOld, TB_INSERTBUTTON, g_tbg.m_iDstButton, (LPARAM)&tbb);

			// Delete the old button if the Ctrl key is not down.
			// ENHANCE DarrellZ: Add this if we want to support copying buttons.
			//if (!(AfApp::GrfmstCur() & kfmstCtrl))
			{
				::SendMessage(m_hwnd, TB_DELETEBUTTON, g_tbg.m_iSrcButton, 0);
			}
		}

		// Make sure any child windows get moved if they need to be.
		UpdateToolBar();

		g_tbg.m_hwndOld = NULL;
	}
	else
	{
		// Delete the button from the current toolbar.
		::SendMessage(m_hwnd, TB_DELETEBUTTON, g_tbg.m_iSrcButton, 0);
	}

	g_tbg.m_fDragging = false;
#endif

	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle notifications.

	@param ctid Identifier of the common control sending the message.
	@param pnmh Pointer to an NMHDR structure containing notification code and additional info.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfToolBar::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctid, pnmh, lnRet))
		return true;

	// Wait 1/2 second after the tooltip disappears before resetting the text on the status bar.
	// If another tooltip shows up in that time interval, cancel the timer so the status bar
	// doesn't get changed back to the idle string.
	if (pnmh->code == TTN_SHOW)
	{
		::KillTimer(m_hwnd, knToolTipTimer);
		AfStatusBarPtr qstbr = m_qafw->GetStatusBarWnd();
		qstbr->DisplayHelpText();
	}
	else if (pnmh->code == TTN_POP)
	{
		::SetTimer(m_hwnd, knToolTipTimer, 500, NULL);
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
bool AfToolBar::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
#if CAN_MOVE_BUTTONS
	if (wm == WM_LBUTTONDOWN && (AfApp::GrfmstCur() & kfmstAlt))
		return OnDragBegin(MakePoint(lp));

	if (g_tbg.m_fDragging)
	{
		if (WM_MOUSEMOVE == wm)
			return OnDragOver(MakePoint(lp));

		if (wm == WM_LBUTTONUP)
			return OnDragEnd(MakePoint(lp));

		if (wm == WM_KEYDOWN && wp == VK_ESCAPE)
		{
			// End the drag operation.
			g_tbg.m_fDragging = false;
			::ReleaseCapture();

			if (g_tbg.m_hwndOld)
			{
				TBINSERTMARK tbim = { -1, 0 };
				::SendMessage(g_tbg.m_hwndOld, TB_SETINSERTMARK, 0, (LPARAM)&tbim);
				g_tbg.m_hwndOld = NULL;
			}
			return true;
		}
	}
#endif

	if (wm == WM_TIMER)
	{
		AfStatusBarPtr qstbr = m_qafw->GetStatusBarWnd();
		qstbr->RestoreStatusText();
		::KillTimer(m_hwnd, knToolTipTimer);
	}
	else if (wm == WM_PAINT)
	{
		// This draws toolbar buttons using our palette so they look better on low-color
		// monitors.
		bool fT;
		HDC hdc = AfGdi::GetDC(m_hwnd);
		{ // Block to deselect palette before the HDC is released.
			SmartPalette spal(hdc);
			Assert(!wp);
			fT = SuperClass::FWndProc(wm, (WPARAM)hdc, lp, lnRet);
		}
		int iSuccess;
		iSuccess = AfGdi::ReleaseDC(m_hwnd, hdc);
		Assert(iSuccess);
		return fT;
	}
	else if ((wm == WM_CHAR && wp == VK_ESCAPE) || (wm == WM_SYSKEYUP && wp == VK_MENU))
	{
		// A toolbar should only be able to get the focus if it is a menubar. In this case,
		// the user is currently in the menu, so hitting any of these keys should cause
		// the focus to go back to the main window (i.e. out of the menu loop).
		Assert(dynamic_cast<AfMenuBar *>(this) != NULL);
		AfMainWnd * pafw = MainWindow();
		AssertPtr(pafw);
		::SetFocus(pafw->Hwnd());
	}
	else if (wm == WM_DESTROY)
	{
		Save();
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Set the background color of the toolbar icon. This is used for toolbar buttons that
	represent color, such as foreground/background colors. The lower 3 pixels of the button
	specified by widButton will be changed to reflect the color that is passed in and the
	toolbar will be updated with the new icon.

	@param widButton Command ID of the button.
	@param clr Background color to use for the button.
----------------------------------------------------------------------------------------------*/
void AfToolBar::UpdateIconColor(int widButton, COLORREF clr)
{
	if (!m_hwnd || !m_qafw)
		return;

	HIMAGELIST himl = (HIMAGELIST)::SendMessage(Hwnd(), TB_GETIMAGELIST, 0, 0);
	if (!himl)
		return;
	HWND hwndOwner = m_qafw->Hwnd();

	TBBUTTONINFO tbbi = { isizeof(tbbi) };
	tbbi.dwMask = TBIF_IMAGE;
	::SendMessage(Hwnd(), TB_GETBUTTONINFO, widButton, (long)&tbbi);

	IMAGEINFO ii;
	ImageList_GetImageInfo(himl, tbbi.iImage, &ii);
	Assert(ii.hbmImage);

	Rect rc(0, 0, ii.rcImage.right - ii.rcImage.left, ii.rcImage.bottom - ii.rcImage.top);
	rc.top = rc.bottom - 3;

	// Update the image.
	{
		BOOL fSuccess;
		HDC hdc = AfGdi::GetDC(hwndOwner);
		HDC hdcMem = AfGdi::CreateCompatibleDC(hdc);
		HBITMAP hbmpImage = AfGdi::CreateCompatibleBitmap(hdc, rc.right, rc.bottom);

		{ // Block to deselect palette before the HDC is deleted.
			SmartPalette spal(hdcMem);

			// Draw the selected bitmap into hbmpImage.
			HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmpImage);
			fSuccess = ::PatBlt(hdcMem, 0, 0, rc.right, rc.bottom, WHITENESS);
			Assert(fSuccess);
			ImageList_Draw(himl, tbbi.iImage, hdcMem, 0, 0, ILD_NORMAL);
			if (clr == ::GetSysColor(COLOR_3DFACE))
			{
				// If the new color is the same as the color used for the background of the toolbar,
				// draw a border, so the bar along the bottom will still be visible.
				fSuccess = ::Rectangle(hdcMem, rc.left, rc.top, rc.right, rc.bottom);
				Assert(fSuccess);
				rc.Inflate(-1, -1);
			}
			AfGfx::FillSolidRect(hdcMem, rc, clr);
			AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);
		}
		fSuccess = AfGdi::DeleteDC(hdcMem);
		Assert(fSuccess);

		ImageList_Replace(himl, tbbi.iImage, hbmpImage, 0);
		fSuccess = AfGdi::DeleteObjectBitmap(hbmpImage);
		Assert(fSuccess);

		int iSuccess;
		iSuccess = AfGdi::ReleaseDC(hwndOwner, hdc);
		Assert(iSuccess);
	}
	::SendMessage(Hwnd(), TB_SETIMAGELIST, 0, (long)himl);

	Assert(tbbi.iImage != I_IMAGECALLBACK);
	::SendMessage(Hwnd(), TB_CHANGEBITMAP, (WPARAM)widButton, MAKELPARAM(tbbi.iImage, 0));
}

/*----------------------------------------------------------------------------------------------
	Replace the image for the specified button with the iimageth image in himlSource.
	This is used for things like the border combo box where we replace the original image
	with one of the ones from the combo list.

	@param widButton Command ID of the button.
----------------------------------------------------------------------------------------------*/
void AfToolBar::UpdateIconImage(int widButton, HIMAGELIST himlSource, int iimage)
{
	if (!m_hwnd || !m_qafw)
		return;

	HIMAGELIST himl = (HIMAGELIST)::SendMessage(Hwnd(), TB_GETIMAGELIST, 0, 0);
	if (!himl)
		return;

	TBBUTTONINFO tbbi = { isizeof(tbbi) };
	tbbi.dwMask = TBIF_IMAGE;
	::SendMessage(Hwnd(), TB_GETBUTTONINFO, widButton, (long)&tbbi);

	HICON hicon = ::ImageList_ExtractIcon(0, himlSource, iimage);
	::ImageList_ReplaceIcon(himl, tbbi.iImage, hicon);
	::DestroyIcon(hicon);

	::SendMessage(Hwnd(), TB_SETIMAGELIST, 0, (long)himl);

	Assert(tbbi.iImage != I_IMAGECALLBACK);
	::SendMessage(Hwnd(), TB_CHANGEBITMAP, (WPARAM)widButton, MAKELPARAM(tbbi.iImage, 0));
}


//:>********************************************************************************************
//:>	AfToolBarToolTip methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Handle window messages.

	@param wm Windows message identifier.
	@param wp First message parameter.
	@param lp Second message parameter.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the message has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfToolBarToolTip::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == TTM_WINDOWFROMPOINT)
	{
		// This returns the extended combobox within the toolbar if the point corresponds to
		// one. If this is not done, tooltips do not appear for toolbar child windows.
		POINT pt = *((POINT *)lp);
		::ScreenToClient(m_hwndToolBar, &pt);
		HWND hwnd = ::ChildWindowFromPoint(m_hwndToolBar, pt);
		AfToolBarCombo * ptbc = dynamic_cast<AfToolBarCombo *>(AfWnd::GetAfWnd(hwnd));
		if (ptbc)
			lnRet = (long)ptbc->GetComboControl();
		else
			lnRet = (long)hwnd;
		return true;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


//:>********************************************************************************************
//:>	AfToolBarCombo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfToolBarCombo::AfToolBarCombo() : TssComboEx()
{
	m_iButton = -1;
}


/*----------------------------------------------------------------------------------------------
	Create the child HWND.

	@param hwndPar Handle to the parent toolbar window.
	@param dwStyle Extended window style flag bits.
	@param wid Command ID for the toolbar child button.
	@param iButton Index of the child button within the toolbar.
	@param rc Dimensions of the toolbar child button.
----------------------------------------------------------------------------------------------*/
void AfToolBarCombo::Create(HWND hwndPar, DWORD dwStyle, int cid, int iButton, Rect & rc,
	HWND hwndToolTip, bool fTypeAhead)
{
	Assert(cid);

	SuperClass::Create(hwndPar, cid, hwndToolTip, fTypeAhead);

	WndCreateStruct wcs;
	wcs.InitChild(WC_COMBOBOXEX, hwndPar, cid);
	wcs.style |= dwStyle;
	wcs.SetRect(rc);
	CreateAndSubclassHwnd(wcs);

	::ShowWindow(m_hwnd, SW_SHOW);

	m_iButton = iButton;
}


/*----------------------------------------------------------------------------------------------
	Return the what's-this help string for the toolbar child at the given point.

	@param pt Screen location of a mouse click.
	@param pptss Address of a pointer to an ITsString COM object for returning the help string.

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfToolBarCombo::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	StrApp str;
	if (!AfUtil::GetResourceStr(krstWhatsThisEnabled, ::GetDlgCtrlID(m_hwnd), str))
		str.Load(kstidNoHelpError); // No context help available
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(str);
	Assert(m_ws);
	CheckHr(qtsf->MakeString(stu.Bstr(), m_ws, pptss));
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
bool AfToolBarCombo::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
#if CAN_MOVE_BUTTONS
	bool fStartButtonDrag = false;

	if (WM_COMMAND == wm && HIWORD(wp) == EN_SETFOCUS)
		fStartButtonDrag = true;

	if (WM_LBUTTONDOWN == wm)
		fStartButtonDrag = true;

	if (fStartButtonDrag && AfApp::GrfmstCur() & kfmstAlt)
	{
		HWND hwndParent = ::GetParent(m_hwnd);
		AfToolBarPtr qtlbr = (AfToolBar *)AfWnd::GetAfWnd(hwndParent);
		AssertObj(qtlbr);
		g_tbg.m_fDragging = true;
		g_tbg.m_iSrcButton = m_iButton;
		g_tbg.m_hwndChild = m_hwnd;
		::SetFocus(qtlbr->Hwnd());
		::SetCapture(qtlbr->Hwnd());
		return true;
	}
#endif

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Handle notifications.

	@param ctid Identifier of the common control sending the message.
	@param pnmh Pointer to an NMHDR structure containing notification code and additional info.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfToolBarCombo::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(id, pnmh, lnRet))
		return true;

	// Pass the notification up to the parent window.
	if (pnmh->code == TTN_GETDISPINFO)
		lnRet = ::SendMessage(::GetParent(m_hwnd), WM_NOTIFY, id, (LPARAM)pnmh);

	return false;
}


/*----------------------------------------------------------------------------------------------
	Handle the combobox closing.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfToolBarCombo::OnSelEndOK(int nID, HWND hwndCombo)
{
	// This notification can happen in several circumstances. The special case is when the
	// Alt key is down. When the user hits Alt+Up or Alt+Down to close the combobox, we don't
	// want the action to actually take place. We just want to close the dropdown list. So
	// we return true here, which keeps the TssComboEx from passing the notification on.
	if (::GetKeyState(VK_MENU) < 0)
		return true;
	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle a press of the Enter key. Specifically, apply the contents of the combo-box to
	the selection of the active window.

	@param ctid Identifier of the common control sending the message.
	@param pnmh Pointer to an NMHDR structure containing notification code and additional info.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfToolBarCombo::OnCharEnter(int ctid, NMHDR * pnmh, long & lnRet)
{
	Assert(pnmh->code == WM_CHAR);

	HWND hwndCombo = pnmh->hwndFrom;
	AfToolBarComboPtr qtbc = dynamic_cast<AfToolBarCombo *>(AfWnd::GetAfWnd(hwndCombo));
	Assert(qtbc && hwndCombo == qtbc->Hwnd());
	HWND hwndToolBar = ::GetParent(hwndCombo);

	AfMainWnd * pafw = qtbc->MainWindow();
	AssertPtr(pafw);
	AfVwRootSitePtr qvwnd;
	int grfvfs = kvfsNormal;
	// DarrellZ moved the (grfvfs == kvfsNormal) check down to the case for when
	// the Enter key is pressed. As long as none of the other keys actually apply
	// formatting, this should be OK.
	// Review (SharonC): Do we want to add a false argument to prevent getting any toolbar
	// controls?
	if (pafw->GetActiveViewWindow(&qvwnd, &grfvfs))
	{
		TBBUTTON tbb;
		// Apply the effects of the combo-box to the current window.

		::SendMessage(hwndToolBar, TB_GETBUTTON, qtbc->GetButtonIndex(), (long)&tbb);
		// Move focus back to the target window after first applying the formatting
		// information.
		if (qvwnd->ApplyFormatting(tbb.idCommand, hwndCombo))
		{
			AssertPtr(qvwnd->Window());
			::SendMessage(hwndCombo, CB_SHOWDROPDOWN, false, 0);
			::SetFocus(qvwnd->Window()->Hwnd());
		}
		else
		{
			HWND hwndEdit = qtbc->GetEditControl();
			::SetFocus(hwndEdit);
			::SendMessage(hwndEdit, EM_SETSEL, 0, -1);
		}
		return true; // suppress DefWndProc, which beeps.
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle a press of the Tab key. Specifically, move the focus to the next or previous
	control on the toolbar.

	@param ctid Identifier of the common control sending the message.
	@param pnmh Pointer to an NMHDR structure containing notification code and additional info.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfToolBarCombo::OnCharTab(int ctid, NMHDR * pnmh, long & lnRet)
{
	Assert(pnmh->code == WM_CHAR);

	HWND hwndCombo = pnmh->hwndFrom;
	AfToolBarComboPtr qtbc = dynamic_cast<AfToolBarCombo *>(AfWnd::GetAfWnd(hwndCombo));
	Assert(qtbc && hwndCombo == qtbc->Hwnd());
	HWND hwndToolBar = ::GetParent(hwndCombo);

	AfMainWnd * pafw = qtbc->MainWindow();
	AssertPtr(pafw);
	AfVwRootSitePtr qvwnd;
	int grfvfs = kvfsNormal;
	// DarrellZ moved the (grfvfs == kvfsNormal) check down to the case for when
	// the Enter key is pressed. As long as none of the other keys actually apply
	// formatting, this should be OK.
	// Review (SharonC): Do we want to add a false argument to prevent getting any toolbar
	// controls?
	if (pafw->GetActiveViewWindow(&qvwnd, &grfvfs))
	{
		TBBUTTON tbb;
		::SendMessage(hwndToolBar, TB_GETBUTTON, qtbc->GetButtonIndex(), (long)&tbb);
		HWND hwndNext;
		if (::GetKeyState(VK_SHIFT) < 0)
		{
			hwndNext = ::GetWindow(hwndCombo, GW_HWNDPREV);
			if (!hwndNext)
				hwndNext = ::GetWindow(hwndCombo, GW_HWNDLAST);
		}
		else
		{
			hwndNext = ::GetWindow(hwndCombo, GW_HWNDNEXT);
			if (!hwndNext)
				hwndNext = ::GetWindow(hwndCombo, GW_HWNDFIRST);
		}
		if (hwndNext)
		{
			HWND hwndEdit;
#ifdef JohnT_Aug_1_01_ApplyOnLoseFocus
			// Analysts currrently say tab should not apply.
			// NOTE: If this is ever reinstated, you will need to check to see if the
			// active view window supports the type of formatting (paragraph/character)
			// that the combobox is applying.
			if (qvwnd->ApplyFormatting(tbb.idCommand, hwndCombo))
				hwndEdit = ::GetWindow(::GetWindow(hwndNext, GW_CHILD), GW_CHILD);
			else
				hwndEdit = ::GetWindow(::GetWindow(hwndCombo, GW_CHILD), GW_CHILD);
#else
			hwndEdit = ::GetWindow(::GetWindow(hwndNext, GW_CHILD), GW_CHILD);

#endif

			::SetFocus(hwndEdit);
			::SendMessage(hwndEdit, EM_SETSEL, 0, -1);
			return true; // suppress DefWndProc, which beeps.
		}
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle a press of the Escape key. Specifically, move the focus back to the main window
	without applying the contents of the combo-box.

	@param ctid Identifier of the common control sending the message.
	@param pnmh Pointer to an NMHDR structure containing notification code and additional info.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfToolBarCombo::OnCharEscape(int ctid, NMHDR * pnmh, long & lnRet)
{
	Assert(pnmh->code == WM_CHAR);

	HWND hwndCombo = pnmh->hwndFrom;
	AfToolBarComboPtr qtbc = dynamic_cast<AfToolBarCombo *>(AfWnd::GetAfWnd(hwndCombo));
	Assert(qtbc && hwndCombo == qtbc->Hwnd());
//	HWND hwndToolBar = ::GetParent(hwndCombo);

	AfMainWnd * pafw = qtbc->MainWindow();
	AssertPtr(pafw);
	AfVwRootSitePtr qvwnd;
	int grfvfs = kvfsNormal;

	// Review (SharonC): Do we want to add a false argument to prevent getting any toolbar
	// controls?
	if (pafw->GetActiveViewWindow(&qvwnd, &grfvfs))
	{
		AssertPtr(qvwnd->Window());
		::SetFocus(qvwnd->Window()->Hwnd());
		return true;
	}
	return false;
}

//:>********************************************************************************************
//:>	AfToolBarEdit methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfToolBarEdit::AfToolBarEdit() : TssEdit()
{
	m_iButton = -1;
}


/*----------------------------------------------------------------------------------------------
	Create the child HWND.

	@param hwndPar Handle to the parent toolbar window.
	@param dwStyle Extended window style flag bits.
	@param cid Command ID for the toolbar child button.
	@param iButton Index of the child button within the toolbar.
	@param rc Dimensions of the toolbar child button.
----------------------------------------------------------------------------------------------*/
void AfToolBarEdit::Create(HWND hwndPar, DWORD dwStyle, int cid, int iButton, Rect & rc,
	HWND hwndToolTip, bool fTypeAhead, ILgWritingSystemFactory * pwsf, int ws)
{
	Assert(cid);

	SuperClass::Create(hwndPar, cid, dwStyle, hwndToolTip, _T(""), pwsf, ws, NULL);
	DWORD dwStyleEx = ::GetWindowLong(m_hwnd, GWL_EXSTYLE);
	::SetWindowLong(m_hwnd, GWL_EXSTYLE, dwStyleEx | WS_EX_CLIENTEDGE);
	::MoveWindow(m_hwnd, rc.left, rc.top, rc.Width(), rc.Height(), false);
	::EnableWindow(m_hwnd, true);
	DWORD dwT = ::GetWindowLong(m_hwnd, GWL_STYLE);
	::SetWindowLong(m_hwnd, GWL_STYLE, dwT);
	::ShowWindow(m_hwnd, SW_SHOW);
	m_iButton = iButton;
}


/*----------------------------------------------------------------------------------------------
	Handle notifications.

	@param ctid Identifier of the common control sending the message.
	@param pnmh Pointer to an NMHDR structure containing notification code and additional info.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfToolBarEdit::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(id, pnmh, lnRet))
		return true;

	// Pass the notification up to the parent window.
	if (pnmh->code == TTN_GETDISPINFO)
		lnRet = ::SendMessage(::GetParent(m_hwnd), WM_NOTIFY, id, (LPARAM)pnmh);

	return false;
}

/*----------------------------------------------------------------------------------------------
	When the copy of the string changes, update the application's search pattern to match.
----------------------------------------------------------------------------------------------*/
bool AfToolBarEdit::OnChange()
{
	HWND hwndToolBar = ::GetParent(m_hwnd);
	if (hwndToolBar == 0)
		return false;
	if (m_cid == kcidEditSrchQuick)
	{
		ITsStringPtr qtssNew;
		::SendMessage(m_hwnd, FW_EM_GETTEXT, 0, (LPARAM)&qtssNew);

//		AfMainWnd * pafw = m_ptlbr->MainWindow();
		AfMainWnd * pafw = MainWindow();
		AssertPtr(pafw);
		IVwPattern * pxpat = pafw->GetFindPattern();
		ITsStringPtr qtssFindWhat;
		CheckHr(pxpat->get_Pattern(&qtssFindWhat));

		ComBool fEq = false;
		if (qtssFindWhat)
			CheckHr(qtssFindWhat->Equals(qtssNew, &fEq));
		if (!fEq)
			CheckHr(pxpat->putref_Pattern(qtssNew));
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Pass along the flag to indicate that this is a toolbar control, in which case we need to
	remember the real window in which the viewing/editing is taking place.
----------------------------------------------------------------------------------------------*/
bool AfToolBarEdit::OnSetFocus(HWND hwndOld, bool fTbControl)
{
	return SuperClass::OnSetFocus(hwndOld, true);
}

/*----------------------------------------------------------------------------------------------
	Handle the Enter key: execute the Quick Search.
----------------------------------------------------------------------------------------------*/
bool AfToolBarEdit::OnCharEnter()
{
	AfMainWndPtr qafw = AfApp::Papp()->GetCurMainWnd();
	Assert(qafw);
	AfVwRootSitePtr qvrs;
	qafw->GetActiveViewWindow(&qvrs, NULL, false);
	if (Cid() == kcidEditSrchQuick)
	{
		// Set the focus to the window in which the search is happening so the selection will
		// show up.
		::SetFocus(qvrs->Window()->Hwnd());
		qvrs->CmdEditSrchQuick1(NULL);
		// Now set the focus back to the edit box control so another Enter key will
		// repeat the search.
		::SetFocus(Hwnd());
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle the tab key: move to the next control.
	This method doesn't really work. TODO: Fix when we really want it.
----------------------------------------------------------------------------------------------*/
bool AfToolBarEdit::OnCharTab()
{
	return false;

//	Assert(pnmh->code == WM_CHAR);

//	HWND hwndCombo = pnmh->hwndFrom;
//	AfToolBarComboPtr qtbc = dynamic_cast<AfToolBarCombo *>(AfWnd::GetAfWnd(hwndCombo));
//	Assert(qtbc && hwndCombo == qtbc->Hwnd());

	HWND hwndToolBar = ::GetParent(Hwnd());

	AfMainWnd * pafw = AfApp::Papp()->GetCurMainWnd();
	AssertPtr(pafw);
	AfVwRootSitePtr qvwnd;
	int grfvfs = kvfsNormal;
	if (pafw->GetActiveViewWindow(&qvwnd, &grfvfs, false))
	{
		TBBUTTON tbb;
		::SendMessage(hwndToolBar, TB_GETBUTTON, m_iButton, (long)&tbb);
		HWND hwndNext;
		if (::GetKeyState(VK_SHIFT) < 0)
		{
			hwndNext = ::GetWindow(Hwnd(), GW_HWNDPREV);
			if (!hwndNext)
				hwndNext = ::GetWindow(Hwnd(), GW_HWNDLAST);
		}
		else
		{
			hwndNext = ::GetWindow(Hwnd(), GW_HWNDNEXT);
			if (!hwndNext)
				hwndNext = ::GetWindow(Hwnd(), GW_HWNDFIRST);
		}
		if (hwndNext)
		{
			HWND hwndEdit;
			hwndEdit = ::GetWindow(::GetWindow(hwndNext, GW_CHILD), GW_CHILD);
			::SetFocus(hwndEdit);
			::SendMessage(hwndEdit, EM_SETSEL, 0, -1);
			return true; // suppress DefWndProc, which beeps.
		}
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle escape key: return to the focus to the main window.
----------------------------------------------------------------------------------------------*/
bool AfToolBarEdit::OnCharEscape()
{
	AfMainWndPtr qafw = AfApp::Papp()->GetCurMainWnd();
	Assert(qafw);
	AfVwRootSitePtr qvrs;
	qafw->GetActiveViewWindow(&qvrs, NULL, false);
	::SetFocus(qvrs->Window()->Hwnd());
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return the what's-this help string for the toolbar child at the given point.

	@param pt Screen location of a mouse click.
	@param pptss Address of a pointer to an ITsString COM object for returning the help string.

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfToolBarEdit::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	StrApp str;
	if (!AfUtil::GetResourceStr(krstWhatsThisEnabled, ::GetDlgCtrlID(m_hwnd), str))
		str.Load(kstidNoHelpError); // No context help available
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(str);
	CheckHr(qtsf->MakeString(stu.Bstr(), m_wsUser, pptss));
	return true;
}


//:>********************************************************************************************
//:>	AfReBar methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfReBar::AfReBar()
{
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfReBar::~AfReBar()
{
}

/*----------------------------------------------------------------------------------------------
	Create the ReBar HWND.

	@param hwndPar Handle to the parent window.
	@param wid Child window identifier.
----------------------------------------------------------------------------------------------*/
void AfReBar::CreateHwnd(HWND hwndPar, int wid)
{
	Assert(hwndPar);

	INITCOMMONCONTROLSEX iccex = { sizeof(iccex), ICC_COOL_CLASSES };
	::InitCommonControlsEx(&iccex);

	WndCreateStruct wcs;

	wcs.InitChild(REBARCLASSNAME, hwndPar, wid);
	wcs.dwExStyle = WS_EX_TOOLWINDOW;
	wcs.style |= RBS_VARHEIGHT | RBS_BANDBORDERS | RBS_DBLCLKTOGGLE | RBS_AUTOSIZE |
		CCS_TOP | CCS_NODIVIDER | WS_VISIBLE;

	CreateAndSubclassHwnd(wcs);
}


//:>********************************************************************************************
//:>	AfStatusBar methods.
//:>********************************************************************************************

static DummyFactory g_fact(_T("SIL.AppCore.AfStatusBar"));

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfStatusBar::AfStatusBar():
	m_strGeneralHelp(kstidIdle),
	m_strFiltered(kstidStBarFiltered),
	m_strSorted(kstidStBarSorted),
	m_strNoFilterTip(kstidNoFilter),
	m_strNoSortTip(kstidStBarDefaultSort),
	m_strHelpMsgTip(kstidStBarHelpMsg),
	m_strLocationToolTip(kstidLocationToolTip)
{
	m_fSorted = false;
	m_fFiltered = false;
	memset(m_rgnPaneEdges, 0, isizeof(m_rgnPaneEdges));
	m_hwndToolTip = NULL;
	m_strRecordToolTip = m_strHelpMsgTip;
	m_strSortedToolTip = m_strNoSortTip;
	m_strFilteredToolTip = m_strNoFilterTip;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfStatusBar::~AfStatusBar()
{
#ifdef JohnT_1_21_2002_Invalid
	// It appears that Windows destroys child tooltip windows automatically.
	// At least, BoundsChecker complains that m_hwndToolTip is invalid.
	if (m_hwndToolTip)
	{
		::DestroyWindow(m_hwndToolTip);
		m_hwndToolTip = NULL;
	}
#endif
}

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  IUnknown, and IAdvInd are supported.

	@param riid Reference to the desired interface GUID.
	@param ppv Address of a pointer for returning the desired interface pointer.

	@return S_OK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStatusBar::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IAdvInd *>(this));
	else if (riid == IID_IAdvInd)
		*ppv = static_cast<IAdvInd *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(
			static_cast<IUnknown *>(static_cast<IAdvInd *>(this)), IID_IAdvInd);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Increment the reference count. (We use the inherited GenRefObj reference count, but COM
	expects AddRef and release to return the reference count.

	@return The updated reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) AfStatusBar::AddRef()
{
	return SuperClass::AddRef();
}

/*----------------------------------------------------------------------------------------------
	Decrement the reference count. (See notes on AddRef.)

	@return The updated reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) AfStatusBar::Release()
{
	return SuperClass::Release();
}

/*----------------------------------------------------------------------------------------------
	Advance the progress bar indicator by calling StepProgressBar with the given amount.

	@param nStepAmt Amount by which to advance the progress bar display, relative to the defined
					end points of the progress bar.

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStatusBar::Step(int nStepAmt)
{
	BEGIN_COM_METHOD;

	StepProgressBar(nStepAmt);

	END_COM_METHOD(g_fact, IID_IAdvInd);
}

/*----------------------------------------------------------------------------------------------
	Create a status bar HWND and connect it to this.

	@param hwndPar Handle to the parent window.
	@param wid Child window identifier.
----------------------------------------------------------------------------------------------*/
void AfStatusBar::CreateHwnd(HWND hwndPar, int wid)
{
	Assert(hwndPar);

	INITCOMMONCONTROLSEX iccex = { sizeof(iccex), ICC_BAR_CLASSES };
	InitCommonControlsEx(&iccex);

	WndCreateStruct wcs;
	wcs.InitChild(STATUSCLASSNAME, hwndPar, wid);
	wcs.style |= SBARS_SIZEGRIP | SBT_TOOLTIPS | CCS_BOTTOM | CCS_NODIVIDER | WS_VISIBLE;
	CreateAndSubclassHwnd(wcs);
}

/*----------------------------------------------------------------------------------------------
	Finish initializing the status bar by splitting it into five panes.
----------------------------------------------------------------------------------------------*/
void AfStatusBar::InitializePanes()
{
	Assert(m_hwnd);
	// Get the total width we have to work with.
	RECT rcStatusBar;
	::GetClientRect(m_hwnd, &rcStatusBar);
	int nWidthTotal = rcStatusBar.right - rcStatusBar.left;
	int nFlagWidth = nWidthTotal / 10;
	if (nFlagWidth > 50)
		nFlagWidth = 50;
	// Divide it into five section appropriately.
	m_rgnPaneEdges[kiLocation] = rcStatusBar.right - 20;
	m_rgnPaneEdges[kiFiltered] = m_rgnPaneEdges[kiLocation] - 2 * nFlagWidth;
	m_rgnPaneEdges[kiSorted] = m_rgnPaneEdges[kiFiltered] - nFlagWidth;
	m_rgnPaneEdges[kiSortKey] = m_rgnPaneEdges[kiSorted] - nFlagWidth;
	int nWidth = m_rgnPaneEdges[kiSortKey] - rcStatusBar.left;
	m_rgnPaneEdges[kiRecord] = rcStatusBar.left + 3 * nWidth / 5;	// 60% of remaining area.
	// Tell the status bar to create the window parts.
	::SendMessage(m_hwnd, SB_SETPARTS, (WPARAM)kcStatusPanes, (LPARAM)&m_rgnPaneEdges[0]);

	TOOLINFO ti = {
		isizeof(ti), 0, m_hwnd, 0,
		rcStatusBar.left, rcStatusBar.top, rcStatusBar.right, rcStatusBar.bottom,
		ModuleEntry::GetModuleHandle(), NULL
	};
	// Create the tooltip windows if needed, or remove existing tools.
	if (!m_hwndToolTip)
	{
		m_hwndToolTip = ::CreateWindow(TOOLTIPS_CLASS, NULL,
			TTS_ALWAYSTIP, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,
			m_hwnd, NULL, ModuleEntry::GetModuleHandle(), NULL);
		if (!m_hwndToolTip)
			ThrowHr(E_FAIL);
	}
	else
	{
		int ctool = ::SendMessage(m_hwndToolTip, TTM_GETTOOLCOUNT, 0, 0);
		for (int itool = 0; itool < ctool; itool++)
		{
			ti.uId = itool;
			::SendMessage(m_hwndToolTip, TTM_DELTOOL, 0, (LPARAM)&ti);
		}
	}

	// Add the new tools.
	ti.uId = kiRecord;
	ti.lpszText = const_cast<achar *>(m_strRecordToolTip.Chars());
	ti.rect.left = 0;
	ti.rect.right = m_rgnPaneEdges[kiRecord] - rcStatusBar.left;
	::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);

	ti.uId = kiSortKey;
	ti.lpszText = const_cast<achar *>(m_strSortKeyToolTip.Chars());
	ti.rect.left = m_rgnPaneEdges[kiRecord] - rcStatusBar.left;
	ti.rect.right = m_rgnPaneEdges[kiSortKey] - rcStatusBar.left;
	::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);

	ti.uId = kiSorted;
	ti.lpszText = const_cast<achar *>(m_strSortedToolTip.Chars());
	ti.rect.left = m_rgnPaneEdges[kiSortKey] - rcStatusBar.left;
	ti.rect.right = m_rgnPaneEdges[kiSorted] - rcStatusBar.left;
	::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);

	ti.uId = kiFiltered;
	ti.lpszText = const_cast<achar *>(m_strFilteredToolTip.Chars());
	ti.rect.left = m_rgnPaneEdges[kiSorted] - rcStatusBar.left;
	ti.rect.right = m_rgnPaneEdges[kiFiltered] - rcStatusBar.left;
	::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);

	ti.uId = kiLocation;
	ti.lpszText = const_cast<achar *>(m_strLocationToolTip.Chars());
	ti.rect.left = m_rgnPaneEdges[kiFiltered] - rcStatusBar.left;
	ti.rect.right = m_rgnPaneEdges[kiLocation] - rcStatusBar.left;
	::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);
}

/*----------------------------------------------------------------------------------------------
	Store a menu help message to display in the first pane of the status bar.

	@param pszHelp Menu help message string.
----------------------------------------------------------------------------------------------*/
void AfStatusBar::StoreHelpText(const achar * pszHelp)
{
	if (pszHelp)
		m_strHelpMsg = pszHelp;
	else
		m_strHelpMsg.Clear();
}

/*----------------------------------------------------------------------------------------------
	Write the stored menu help message to the first pane of the status bar.
----------------------------------------------------------------------------------------------*/
void AfStatusBar::DisplayHelpText()
{
	if (m_strHelpMsg.Length())
	{
		bool fResult;
		fResult = ::SendMessage(m_hwnd, SB_SETTEXT, (WPARAM)kiHelp,
			(LPARAM)m_strHelpMsg.Chars());
		::InvalidateRect(m_hwnd, NULL, false);
		TOOLINFO ti = {
			isizeof(ti), 0, m_hwnd, 0, 0,0,0,0, ModuleEntry::GetModuleHandle(), NULL };
		ti.uId = kiRecord;
		ti.lpszText = const_cast<achar *>(m_strHelpMsgTip.Chars());
		::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, (WPARAM)0, (LPARAM)&ti);
		Assert(fResult);
	}
}

/*----------------------------------------------------------------------------------------------
	Restore whatever was in the first pane before writing a menu help message there.
----------------------------------------------------------------------------------------------*/
void AfStatusBar::RestoreStatusText()
{
	bool fResult;
	TOOLINFO ti = { isizeof(ti), 0, m_hwnd, 0, 0,0,0,0, ModuleEntry::GetModuleHandle(), NULL };
	if (m_stuDatestamp.Length() || m_stuTitle.Length())
	{
		fResult = ::SendMessage(m_hwnd, SB_SETTEXT, (WPARAM)SBT_OWNERDRAW | kiRecord,
			(LPARAM)this);
		ti.uId = kiRecord;
		ti.lpszText = const_cast<achar *>(m_strRecordToolTip.Chars());
		::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, (WPARAM)0, (LPARAM)&ti);
	}
	else
	{
		fResult = ::SendMessage(m_hwnd, SB_SETTEXT, (WPARAM)kiRecord,
			(LPARAM)m_strGeneralHelp.Chars());
		ti.uId = kiRecord;
		ti.lpszText = const_cast<achar *>(m_strHelpMsgTip.Chars());
		LRESULT lr;
		lr = ::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, (WPARAM)0, (LPARAM)&ti);
	}
	::InvalidateRect(m_hwnd, NULL, false);
	// Clear the previously displayed help message.
	m_strHelpMsg.Clear();
	Assert(fResult);
}

/*----------------------------------------------------------------------------------------------
	Write information specific to the current record to the first pane of the status bar.

	@param pszDatestamp Short record datestamp string.
	@param pszTitle Record title string.
	@param pszToolTip Explanatory tooltip string (something like "Date: title").
----------------------------------------------------------------------------------------------*/
void AfStatusBar::SetRecordInfo(const OLECHAR * pszDatestamp, const OLECHAR * pszTitle,
	const achar * pszToolTip)
{
	bool fResult;
	m_stuDatestamp = pszDatestamp;
	m_stuTitle = pszTitle;
	m_strRecordToolTip = pszToolTip;
	TOOLINFO ti = { isizeof(ti), 0, m_hwnd, 0, 0,0,0,0, ModuleEntry::GetModuleHandle(), NULL };
	if (m_stuDatestamp.Length() || m_stuTitle.Length())
	{
		fResult = ::SendMessage(m_hwnd, SB_SETTEXT, (WPARAM)SBT_OWNERDRAW | kiRecord,
			(LPARAM)this);
		ti.uId = kiRecord;
		ti.lpszText = const_cast<achar *>(m_strRecordToolTip.Chars());
		::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, (WPARAM)0, (LPARAM)&ti);
	}
	else
	{
		fResult = ::SendMessage(m_hwnd, SB_SETTEXT, (WPARAM)kiRecord,
			(LPARAM)m_strGeneralHelp.Chars());
		ti.uId = kiRecord;
		ti.lpszText = const_cast<achar *>(m_strHelpMsgTip.Chars());
		::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, (WPARAM)0, (LPARAM)&ti);
	}
	::InvalidateRect(m_hwnd, NULL, false);
	Assert(fResult);
}

/*----------------------------------------------------------------------------------------------
	Write sorting information to the second and third panes of the status bar.

	@param fSorting Flag whether the records have been sorted specially.
	@param pszToolTipMethod Tooltip string giving the name of the sorting method.
	@param pszSortKey String containing the current record's sort key value.
	@param pszToolTipKey Explanatory tooltip string for the sort key.
----------------------------------------------------------------------------------------------*/
void AfStatusBar::SetSortingStatus(bool fSorting, const achar * pszToolTipMethod,
	const OLECHAR * pszSortKey, const achar * pszToolTipKey)
{
	m_fSorted = fSorting;
	m_stuSortKey = pszSortKey;
	m_strSortedToolTip = pszToolTipMethod;
	m_strSortKeyToolTip = pszToolTipKey;
	if (m_rgnPaneEdges[0])
	{
		TOOLINFO ti = {
			isizeof(ti), 0, m_hwnd, 0, 0,0,0,0, ModuleEntry::GetModuleHandle(), NULL
		};
		bool fResult;
		if (fSorting)
		{
			fResult = ::SendMessage(m_hwnd, SB_SETTEXT, (WPARAM)SBT_OWNERDRAW | kiSortKey,
				(LPARAM)pszSortKey);
			fResult &= ::SendMessage(m_hwnd, SB_SETTEXT, (WPARAM)SBT_OWNERDRAW | kiSorted,
				(LPARAM)m_strSorted.Chars()) ? true : false;
			ti.uId = kiSorted;
			ti.lpszText = const_cast<achar *>(m_strSortedToolTip.Chars());
			::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, (WPARAM)0, (LPARAM)&ti);
			ti.uId = kiSortKey;
			ti.lpszText = const_cast<achar *>(m_strSortKeyToolTip.Chars());
			::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, (WPARAM)0, (LPARAM)&ti);
		}
		else
		{
			fResult = ::SendMessage(m_hwnd, SB_SETTEXT, (WPARAM)kiSortKey, (LPARAM)_T(""));
			fResult &= ::SendMessage(m_hwnd, SB_SETTEXT, (WPARAM)kiSorted, (LPARAM)_T("")) ?
				true : false;
			ti.uId = kiSorted;
			ti.lpszText = const_cast<achar *>(m_strNoSortTip.Chars());
			::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, (WPARAM)0, (LPARAM)&ti);
			ti.uId = kiSortKey;
			ti.lpszText = const_cast<achar *>(m_strNoSortTip.Chars());
			::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, (WPARAM)0, (LPARAM)&ti);
		}
		::InvalidateRect(m_hwnd, NULL, false);
		Assert(fResult);
	}
}

/*----------------------------------------------------------------------------------------------
	Write filtering information to the fourth pane of the status bar.

	@param fFiltering Flag whether the filters have been filtered.
	@param pszToolTip Tooltip string containing the name of the current filter, or something
					like "No Filter".
----------------------------------------------------------------------------------------------*/
void AfStatusBar::SetFilteringStatus(bool fFiltering, const achar * pszToolTip)
{
	m_fFiltered = fFiltering;
	m_strFilteredToolTip = pszToolTip;
	if (m_rgnPaneEdges[0])
	{
		TOOLINFO ti = {
			isizeof(ti), 0, m_hwnd, 0, 0,0,0,0, ModuleEntry::GetModuleHandle(), NULL
		};
		bool fResult;
		if (fFiltering)
		{
			fResult = ::SendMessage(m_hwnd, SB_SETTEXT, (WPARAM)SBT_OWNERDRAW | kiFiltered,
				(LPARAM)m_strFiltered.Chars());
			ti.uId = kiFiltered;
			ti.lpszText = const_cast<achar *>(m_strFilteredToolTip.Chars());
			::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, (WPARAM)0, (LPARAM)&ti);
		}
		else
		{
			fResult = ::SendMessage(m_hwnd, SB_SETTEXT, (WPARAM)kiFiltered, (LPARAM)_T(""));
			ti.uId = kiFiltered;
			ti.lpszText = const_cast<achar *>(m_strNoFilterTip.Chars());
			::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, (WPARAM)0, (LPARAM)&ti);
		}
		::InvalidateRect(m_hwnd, NULL, false);
		Assert(fResult);
	}
}

/*----------------------------------------------------------------------------------------------
	Write record location information to the fifth pane of the status bar.

	@param irec Index of the current record relative to the number available.
	@param crec Total number of records, or number of records in filtered set.
----------------------------------------------------------------------------------------------*/
void AfStatusBar::SetLocationStatus(int irec, int crec)
{
	if (crec > 0)
		m_strLocation.Format(_T(" %d / %d "), irec + 1, crec);
	else if (crec == 0 && irec == 0)
		m_strLocation.Format(_T(" 0 / 0 "));		// filtered everything out?
	else
		m_strLocation.Clear();
	if (m_rgnPaneEdges[0])
	{
		bool fResult;
		fResult = ::SendMessage(m_hwnd, SB_SETTEXT, (WPARAM)kiLocation,
			(LPARAM)m_strLocation.Chars());
		::InvalidateRect(m_hwnd, NULL, false);
		Assert(fResult);
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize a progress bar in the second pane of the status bar, and display a message in
	the first pane to explain the progress bar.

	@param pszMessage Short message describing the current operation.
	@param nLowLim Lower limit of possible progress bar values.
	@param nHighLim Upper limit of possible progress bar values.
	@param nStep Default amount by which to advance the progress bar.
----------------------------------------------------------------------------------------------*/
#define ID_PROGRESSBAR 1002
void AfStatusBar::StartProgressBar(const achar * pszMessage, int nLowLim, int nHighLim,
	int nStep)
{
	::SendMessage(m_hwnd, SB_SETTEXT, (WPARAM)kiHelp, (LPARAM)pszMessage);
	::InvalidateRect(m_hwnd, NULL, false);
	TOOLINFO ti = { isizeof(ti), 0, m_hwnd, 0, 0,0,0,0, ModuleEntry::GetModuleHandle(), NULL };
	ti.uId = kiRecord;
	::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, (WPARAM)0, (LPARAM)&ti);

	Assert(m_hwnd);
	if (!m_rgnPaneEdges[0])
		InitializePanes();
	if (m_qprbr)
	{
		m_qprbr->DestroyHwnd();
		m_qprbr.Clear();
	}
	m_qprbr.Create();
	m_qprbr->CreateHwnd(m_hwnd, ID_PROGRESSBAR);
	m_qprbr->SetRange(nLowLim, nHighLim);
	m_qprbr->SetStep(nStep);
}

/*----------------------------------------------------------------------------------------------
	Grow the progress bar by the given increment.  (zero => default size step)

	@param nIncrement Amount by which to advance the progress bar.
----------------------------------------------------------------------------------------------*/
void AfStatusBar::StepProgressBar(int nIncrement)
{
	if (m_qprbr)
		m_qprbr->StepIt(nIncrement);
}

/*----------------------------------------------------------------------------------------------
	Clear the progress bar.
----------------------------------------------------------------------------------------------*/
void AfStatusBar::EndProgressBar()
{
	if (m_qprbr)
	{
		m_qprbr->DestroyHwnd();
		m_qprbr.Clear();
	}
}

/*----------------------------------------------------------------------------------------------
	Handle WM_DRAWITEM window messages sent to the parent window.

	@param pdis Pointer to the information needed to paint an owner-drawn control or menu item.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool AfStatusBar::OnDrawThisItem(DRAWITEMSTRUCT * pdis)
{
	COLORREF clr;
	if (pdis->itemID == kiRecord)
	{
		::SetBkColor(pdis->hDC, ::GetSysColor(COLOR_MENU));
#ifdef DEBUG
		AfStatusBar * pstbr = reinterpret_cast<AfStatusBar *>(pdis->itemData);
		Assert(pstbr == this);
#endif
		int nLeft = 3;
		if (m_stuDatestamp.Length())
		{
			::TextOutW(pdis->hDC, pdis->rcItem.left + nLeft, pdis->rcItem.top + 2,
				m_stuDatestamp.Chars(), m_stuDatestamp.Length());
			SIZE size;
			if (::GetTextExtentPoint32W(pdis->hDC, m_stuDatestamp.Chars(),
				m_stuDatestamp.Length(), &size))
			{
				nLeft += size.cx;
				if (::GetTextExtentPoint32W(pdis->hDC, L"  ", 2, &size))
					nLeft += size.cx;
				else
					nLeft += 8;
			}
			else
			{
				nLeft += 100;
			}
		}
		if (m_stuTitle)
		{
			HFONT hfont = (HFONT)::GetCurrentObject(pdis->hDC, OBJ_FONT);
			LOGFONT lf;
			::GetObject(hfont, isizeof(lf), &lf);
			lf.lfWeight = FW_BOLD;
			HFONT hfntBold = AfGdi::CreateFontIndirect(&lf);
			HFONT hfntDefault = AfGdi::SelectObjectFont(pdis->hDC, hfntBold);
			::TextOutW(pdis->hDC, pdis->rcItem.left + nLeft, pdis->rcItem.top + 2,
				m_stuTitle.Chars(), m_stuTitle.Length());
			AfGdi::SelectObjectFont(pdis->hDC, hfntDefault, AfGdi::OLD);
			AfGdi::DeleteObjectFont(hfntBold);
		}
		return true;
	}
	else if (pdis->itemID == kiSortKey)
	{
		::SetBkColor(pdis->hDC, ::GetSysColor(COLOR_MENU));
		::TextOutW(pdis->hDC, pdis->rcItem.left + 3, pdis->rcItem.top + 2,
			m_stuSortKey.Chars(), m_stuSortKey.Length());
		return true;
	}
	else if (pdis->itemID == kiSorted)
	{
		clr = RGB(0,255,0);			// Green.
	}
	else if (pdis->itemID == kiFiltered)
	{
		clr = RGB(255,255,0);		// Yellow.
	}
	else
	{
		return false;
	}
	HBRUSH hbrush = AfGdi::CreateSolidBrush(clr);
	if (!hbrush)
		return false;

	int iSuccess;
	iSuccess = ::FillRect(pdis->hDC, &pdis->rcItem, hbrush);
	Assert(iSuccess);
	::SetBkColor(pdis->hDC, clr);
	StrAppBuf strb = reinterpret_cast<achar *>(pdis->itemData);
	::TextOut(pdis->hDC, pdis->rcItem.left + 3, pdis->rcItem.top + 2,
		strb.Chars(), strb.Length());
	BOOL fSuccess;
	fSuccess = AfGdi::DeleteObjectBrush(hbrush);
	Assert(fSuccess);
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
bool AfStatusBar::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_LBUTTONDOWN ||
		wm == WM_LBUTTONUP ||
		wm == WM_MBUTTONDOWN ||
		wm == WM_MBUTTONUP ||
		wm == WM_MOUSEMOVE ||
		wm == WM_RBUTTONDOWN ||
		wm == WM_RBUTTONUP)
	{
		// Show the tool tips for the panes in the status bar.
		if (m_hwndToolTip)
		{
			MSG msg;
			msg.hwnd = m_hwnd;
			msg.message = wm;
			msg.wParam = wp;
			msg.lParam = lp;
			::SendMessage(m_hwndToolTip, TTM_RELAYEVENT, 0, (LPARAM)&msg);
		}
	}
	else if (wm == WM_WINDOWPOSCHANGED)
	{
		// Resize the panes in the status bar.
		InitializePanes();
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Return the what's-this help string for the status bar child at the given point.

	@param pt Screen location of a mouse click.
	@param pptss Address of a pointer to an ITsString COM object for returning the help string.

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfStatusBar::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	StrApp str;
	if (!AfUtil::GetResourceStr(krstWhatsThisEnabled, ::GetDlgCtrlID(m_hwnd), str))
		str.Load(kstidStBar_WhatsThisHelp); // No context help available
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(str);
	CheckHr(qtsf->MakeString(stu.Bstr(), m_wsUser, pptss));
	return true;
}


//:>********************************************************************************************
//:>	AfProgressBar methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfProgressBar::AfProgressBar()
{
	m_nLowLim = 0;
	m_nHighLim = 0;
	m_nStep = 0;
	m_nCurrent = 0;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfProgressBar::~AfProgressBar()
{
}

/*----------------------------------------------------------------------------------------------
	Create a progress bar HWND and connect it to this.

	@param hwndPar Handle to the parent window.
	@param wid Child window identifier.
----------------------------------------------------------------------------------------------*/
void AfProgressBar::CreateHwnd(HWND hwndPar, int wid)
{
	Assert(hwndPar);

	INITCOMMONCONTROLSEX iccex = { sizeof(iccex), ICC_PROGRESS_CLASS };
	::InitCommonControlsEx(&iccex);

	WndCreateStruct wcs;
	wcs.InitChild(PROGRESS_CLASS, hwndPar, wid);
	wcs.style |= WS_CHILD | WS_VISIBLE | PBS_SMOOTH;
	RECT rc;
	::SendMessage(hwndPar, SB_GETRECT, (WPARAM)AfStatusBar::kiProgress, (LPARAM)&rc);
	wcs.SetRect(rc);
	CreateAndSubclassHwnd(wcs);
}

/*----------------------------------------------------------------------------------------------
	Send the PBM_SETBARCOLOR and PBM_SETBKCOLOR messages to the progress bar window.

	@param clrBar Color of the progress bar.
	@param clrBk Background color of the pane in which the progress bar is displayed.
----------------------------------------------------------------------------------------------*/
void AfProgressBar::SetColors(COLORREF clrBar, COLORREF clrBk)
{
	Assert(m_hwnd);
	::SendMessage(m_hwnd, PBM_SETBARCOLOR, 0, (LPARAM)clrBar);
	::SendMessage(m_hwnd, PBM_SETBKCOLOR, 0, (LPARAM)clrBk);
}

/*----------------------------------------------------------------------------------------------
	Send the PBM_SETRANGE32 message to the progress bar window.

	@param nLowLim Lower limit of possible values for the progress bar (typically 0).
	@param nHighLim Upper limit of possible values for the progress bar.
----------------------------------------------------------------------------------------------*/
void AfProgressBar::SetRange(int nLowLim, int nHighLim)
{
	Assert(m_hwnd);
	Assert(nLowLim < nHighLim);
	::SendMessage(m_hwnd, PBM_SETRANGE32, nLowLim, nHighLim);
	m_nLowLim = nLowLim;
	m_nHighLim = nHighLim;
	m_nCurrent = nLowLim;
}

/*----------------------------------------------------------------------------------------------
	Send either the PBM_STEPIT or the PBM_DELTAPOS message to the progress bar window.

	@param nIncrement Amount by which to advance the progress bar.
----------------------------------------------------------------------------------------------*/
void AfProgressBar::StepIt(int nIncrement)
{
	Assert(m_hwnd);
	if (nIncrement)
		m_nCurrent += nIncrement;
	else
		m_nCurrent += m_nStep;
	if (m_nCurrent > m_nHighLim)
		m_nCurrent = m_nLowLim + m_nCurrent % (m_nHighLim - m_nLowLim);
	::SendMessage(m_hwnd, PBM_SETPOS, m_nCurrent, 0);
}

/*----------------------------------------------------------------------------------------------
	Send the PBM_SETSTEP message to the progress bar window.

	@param nStepInc Default amount by which to advance the progress bar.
----------------------------------------------------------------------------------------------*/
void AfProgressBar::SetStep(int nStepInc)
{
	Assert(m_hwnd);
	::SendMessage(m_hwnd, PBM_SETSTEP, nStepInc, 0);
	m_nStep = nStepInc;
}

/*----------------------------------------------------------------------------------------------
	Send the PBM_SETPOS message to the progress bar window.

	@param nNewPos New position for the progress bar.
----------------------------------------------------------------------------------------------*/
void AfProgressBar::SetPos(int nNewPos)
{
	Assert(m_hwnd);
	::SendMessage(m_hwnd, PBM_SETPOS, nNewPos, 0);
	m_nCurrent = nNewPos;
}


//:>********************************************************************************************
//:>	AfMenuBar methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfMenuBar::AfMenuBar()
{
	Assert(!m_hwndMain);
	Assert(!m_hmenu);
	m_nIdOld = -1;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfMenuBar::~AfMenuBar()
{
	if (m_hmenu)
	{
		::DestroyMenu(m_hmenu);
		m_hmenu = NULL;
	}
}

/*----------------------------------------------------------------------------------------------
	Load the menu.

	@param hwndMain Handle to the main window to which the menubar is attached.
	@param wid Window id of the menu bar.
	@param rid Resource id of the menu bar.
	@param pszName Name of the menu bar.
----------------------------------------------------------------------------------------------*/
void AfMenuBar::Initialize(HWND hwndMain, int wid, int rid, const achar * pszName)
{

	SuperClass::Initialize(wid, rid, pszName);

	m_hwndMain = hwndMain;
	Assert(m_hmenu == NULL);
	m_hmenu = ::LoadMenu(ModuleEntry::GetModuleHandle(), MAKEINTRESOURCE(m_rid));
	if (!m_hmenu)
		ThrowHr(WarnHr(E_FAIL), L"Could not create the menu.");
}

/*----------------------------------------------------------------------------------------------
	Create a menu bar based on the menu resource id passed in to Initialize.

	@param pafw Pointer to the application's frame window object with which the menubar is
					associated.
----------------------------------------------------------------------------------------------*/
void AfMenuBar::Create(AfMainWnd * pafw)
{
	AssertObj(this);
	AssertObj(pafw);
	AssertPtr(pafw);
	Assert(!m_hwnd);
	Assert(m_rid);
	Assert(m_hmenu);

	INITCOMMONCONTROLSEX iccex = { sizeof(iccex), ICC_BAR_CLASSES };
	::InitCommonControlsEx(&iccex);

	WndCreateStruct wcs;

	m_qafw = pafw;
	wcs.InitChild(TOOLBARCLASSNAME, pafw->Hwnd(), m_wid);
	wcs.style |= TBSTYLE_FLAT | TBSTYLE_LIST | CCS_NODIVIDER | CCS_NOPARENTALIGN | CCS_NORESIZE;

	CreateAndSubclassHwnd(wcs);

	DWORD style = ::GetWindowLong(m_hwnd, GWL_STYLE);
	::SetWindowLong(m_hwnd, GWL_STYLE, style & ~TBSTYLE_TRANSPARENT);
	::ShowWindow(m_hwnd, SW_SHOW);

	// Set the bitmap size to 0 since we're not showing bitmaps on the menu buttons.
	Assert(m_hwnd != NULL);
	::SendMessage(m_hwnd, TB_BUTTONSTRUCTSIZE, isizeof(TBBUTTON), 0);
	::SendMessage(m_hwnd, TB_SETBITMAPSIZE, 0, 0);

	::SendMessage(m_hwnd, WM_SETFONT, (WPARAM)pafw->GetMenuMgr()->GetMenuFont(), true);

	achar rgch[MAX_PATH];
	TBBUTTON tbb;
	tbb.iBitmap = -1;
	tbb.fsState = TBSTATE_ENABLED;

	// Loop through all the top level menu items to see if we need to add keyboard
	// accelerators for any of them.
	Vector<ACCEL> vaccel;
	ACCEL accel;
	int cmenu = ::GetMenuItemCount(m_hmenu);
	for (int imenu = 0; imenu < cmenu; imenu++)
	{
		if (::GetMenuItemID(m_hmenu, imenu) == 0)
		{
			tbb.fsStyle = TBSTYLE_SEP;
			tbb.idCommand = -1;
		}
		else
		{
			tbb.fsStyle = TBSTYLE_AUTOSIZE | TBSTYLE_BUTTON | TBSTYLE_DROPDOWN;
			tbb.idCommand = m_wid + imenu;
		}
		::GetMenuString(m_hmenu, imenu, rgch, MAX_PATH, MF_BYPOSITION);

		// See if the menu item has an accelerator defined for it.
		achar * pchAccel = _tcschr(rgch, '&');
		if (pchAccel && isalpha(pchAccel[1]))
		{
			accel.cmd = (WORD)tbb.idCommand;
			accel.fVirt = FALT | FVIRTKEY;
			accel.key = (BYTE)::VkKeyScan(pchAccel[1]);
			vaccel.Push(accel);
		}

		// Set the string of the button to correspond with the menu item.
		rgch[_tcslen(rgch) + 1] = 0;
		tbb.iString = ::SendMessage(m_hwnd, TB_ADDSTRING, NULL, (LPARAM)rgch);
		::SendMessage(m_hwnd, TB_INSERTBUTTON, imenu, (LPARAM)&tbb);
	}

	// Load the accelerator table for the top level menu items.
	if (vaccel.Size())
	{
		HACCEL hact = ::CreateAcceleratorTable(vaccel.Begin(), vaccel.Size());
		AfApp::Papp()->AddAccelTable(hact, -1, m_hwnd);
	}
}

/*----------------------------------------------------------------------------------------------
	Make a popup menu showing the menu items starting from ibtn to the end of the menu.

	@param ibtn Index of the starting menu item in the menubar's menu.
	@param hwndPar Handle to the parent of the popup menu.
	@param pt Screen location where to display the popup menu.
----------------------------------------------------------------------------------------------*/
void AfMenuBar::ShowChevronPopup(int ibtn, HWND hwndPar, Point & pt)
{
	int cbtn = ::GetMenuItemCount(m_hmenu);
	Assert((uint)ibtn < (uint)cbtn);

	achar rgch[MAX_PATH];
	HMENU hmenuPopup = ::CreatePopupMenu();
	while (ibtn < cbtn)
	{
		// Separators will have an ID of 0.
		if (::GetMenuItemID(m_hmenu, ibtn) == 0)
		{
			::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
		}
		else
		{
			::GetMenuString(m_hmenu, ibtn, rgch, MAX_PATH, MF_BYPOSITION);
			::AppendMenu(hmenuPopup, MF_POPUP, (uint)::GetSubMenu(m_hmenu, ibtn), rgch);
		}
		ibtn++;
	}

	::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y, 0, hwndPar, NULL);

	// NOTE: It is very important to remove the menu items before destroying the menu. If this
	// is not done, the submenus will be destroyed as well, which means the top-level menu
	// will no longer work correctly.
	int cmenu = ::GetMenuItemCount(hmenuPopup);
	while (cmenu--)
		::RemoveMenu(hmenuPopup, 0, MF_BYPOSITION);

	::DestroyMenu(hmenuPopup);
}

/*----------------------------------------------------------------------------------------------
	Handle notifications.

	@param id Identifier of the common control sending the message.
	@param pnmh Pointer to an NMHDR structure containing notification code and additional info.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfMenuBar::OnNotifyThis(int id, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (TBN_DROPDOWN == pnmh->code)
	{
		if (::GetFocus() == m_hwnd)
		{
			AfMainWnd * pafw = MainWindow();
			AssertPtr(pafw);
			::SetFocus(pafw->Hwnd());
		}
		return ShowMenu(((NMTOOLBAR *)pnmh)->iItem);
	}

	return SuperClass::OnNotifyThis(id, pnmh, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Handle window messages.

	@param wm Windows message identifier.
	@param wp First message parameter.
	@param lp Second message parameter.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the message has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfMenuBar::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == s_wmShowMenu)
		return ShowMenu(lp);

	if (wm == WM_COMMAND)
	{
		if ((uint)LOWORD(wp) >= (uint)m_wid && (uint)LOWORD(wp) < (uint)(m_wid +
			::SendMessage(m_hwnd, TB_BUTTONCOUNT, 0, 0)))
		{
			// A menu accelerator key was pressed, so show the appropriate menu.
			s_fIgnoreMouseMove = true;
			ShowMenu(LOWORD(wp));
		}
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Show the submenu with the given id.

	@param nId Menu item command identifier.
----------------------------------------------------------------------------------------------*/
bool AfMenuBar::ShowMenu(int nId)
{
	if (m_nIdOld == nId)
		return false;
	m_nIdOld = nId;

	int iButton = ::SendMessage(m_hwnd, TB_COMMANDTOINDEX, nId, 0);
	HMENU hmenuSub = ::GetSubMenu(m_hmenu, iButton);
	s_hmenuLastPopup = s_hmenuOld = hmenuSub;
	s_fOverSubMenu = false;

	// Determine where to show the popup menu.
	Rect rc;
	::SendMessage(m_hwnd, TB_GETITEMRECT, iButton, (LPARAM)&rc);
	::MapWindowPoints(m_hwnd, NULL, (POINT *)&rc, 2);
	::SendMessage(m_hwnd, TB_SETSTATE, nId, TBSTATE_PRESSED | TBSTATE_ENABLED);

	// Create a hook to monitor messages while the menu is showing.
	Assert(!s_hhook);
	s_hhook = ::SetWindowsHookEx(WH_MSGFILTER, &AfMenuBar::MsgHook, 0, GetCurrentThreadId());
	s_qmnbrCur = this;

	// Create and show the menu.
	TPMPARAMS tpm = { isizeof(tpm) };
	tpm.rcExclude = rc;
	// When a menu is moved both horizontally and vertically, we need the next line so that
	// the menu doesn't show up directly to the left of the toolbar button. This can happen
	// when the window is at the bottom, right side of a screen, and a menu is shown.
	// In this case, the menu shows to the left and above the toolbar button.
	tpm.rcExclude.left--;
	::TrackPopupMenuEx(hmenuSub, TPM_LEFTALIGN | TPM_TOPALIGN | TPM_LEFTBUTTON | TPM_RIGHTBUTTON | TPM_VERTICAL,
		rc.left, rc.bottom, m_hwndMain, &tpm);

	// Clean up after the menu disappears.
	s_qmnbrCur.Clear();
	if (s_hhook)
	{
		::UnhookWindowsHookEx(s_hhook);
		s_hhook = NULL;
	}
	m_nIdOld = -1;

	::SendMessage(m_hwnd, TB_SETSTATE, nId, TBSTATE_ENABLED);

	return true;
}

/*----------------------------------------------------------------------------------------------
	The menubar hook procedure.  Basically all this does is detect when the mouse moves over a
	new button on the menu toolbar.  When it does, it sends messages to close the current menu
	and show the menu for the new button.

	See MessageProc in MSDN for more information.

	@param code Hook code.
	@param wParam Not used.
	@param lParam Pointer to the message data.
----------------------------------------------------------------------------------------------*/
LRESULT CALLBACK AfMenuBar::MsgHook(int code, WPARAM wParam, LPARAM lParam)
{
	MSG * pmsg = (MSG *)lParam;
	AssertObj(s_qmnbrCur);
	AssertPtr(pmsg);

	Assert(code >= 0);
	switch (pmsg->message)
	{
	case WM_MOUSEMOVE:
		if (s_fIgnoreMouseMove)
		{
			// Skip the first move message that comes when the menu first opens.
			s_fIgnoreMouseMove = false;
		}
		else
		{
			HWND hwnd = s_qmnbrCur->Hwnd();

			Point pt = MakePoint(pmsg->lParam);

			Rect rc;
			::GetWindowRect(hwnd, &rc);
			if (PtInRect(&rc, pt))
			{
				// The mouse is inside the menu toolbar.
				::ScreenToClient(hwnd, &pt);

				int iButton = ::SendMessage(hwnd, TB_HITTEST, 0, (LPARAM)&pt);
				if (iButton >= 0)
				{
					// The mouse is over a button.
					TBBUTTON tbb;
					::SendMessage(hwnd, TB_GETBUTTON, iButton, (LPARAM)&tbb);

					if (s_qmnbrCur->m_nIdOld != tbb.idCommand)
					{
						// The mouse is over a new button, so show the new submenu after
						// closing the current menu.
						Assert(s_hhook);
						::UnhookWindowsHookEx(s_hhook);
						s_hhook = NULL;

						::SendMessage(s_qmnbrCur->m_hwndMain, WM_CANCELMODE, 0, 0);
						::PostMessage(hwnd, s_wmShowMenu, 0, tbb.idCommand);
						return 0;
					}
				}
			}
		}
		break;

	case WM_LBUTTONUP:
		{
			int cid = 0;
			int iItem = -1;
			Point pt = MakePoint(pmsg->lParam);
			if (s_hmenuOld)
			{
				iItem = ::MenuItemFromPoint(pmsg->hwnd, s_hmenuOld, pt);
				if (iItem != -1)
					cid = ::GetMenuItemID(s_hmenuOld, iItem);
			}
			// The ID for items that open a submenu is -1, so we need to check for that case,
			// and ignore it if they clicked on an item that opens a submenu.
			if (cid > 0)
			{
				// Check to make sure the menu item can be selected.
				int nState = ::GetMenuState(s_hmenuOld, iItem, MF_BYPOSITION);
				if (!(nState & (MF_DISABLED | MF_GRAYED | MF_POPUP)))
				{
					// Perform the command.
					Assert(s_hhook);
					::UnhookWindowsHookEx(s_hhook);
					s_hhook = NULL;

					::SendMessage(s_qmnbrCur->m_hwndMain, WM_CANCELMODE, 0, 0);
					::PostMessage(s_qmnbrCur->m_hwndMain, WM_COMMAND, cid, NULL);
					return 0;
				}
			}
		}
		break;

	case WM_KEYDOWN:
		// If we are over an item that will open up a submenu, perform the default action.
		if (pmsg->wParam == VK_RIGHT && s_fOverSubMenu)
		{
			s_fOverSubMenu = false;
			return ::CallNextHookEx(s_hhook, code, wParam, lParam);
		}

		// If we are not over a top-level popup, perform the default action.
		if (pmsg->wParam == VK_LEFT && s_hmenuOld != s_hmenuLastPopup)
			return ::CallNextHookEx(s_hhook, code, wParam, lParam);

		// Move to the next menu in the sequence.
		if (pmsg->wParam == VK_LEFT || pmsg->wParam == VK_RIGHT)
		{
			HWND hwndTool = s_qmnbrCur->Hwnd();
			int iButton = ::SendMessage(hwndTool, TB_COMMANDTOINDEX, s_qmnbrCur->m_nIdOld, 0);
			int cButtons = ::SendMessage(hwndTool, TB_BUTTONCOUNT, 0, 0);
			TBBUTTON tbb;

			do
			{
				if (pmsg->wParam == VK_LEFT)
					iButton--;
				if (pmsg->wParam == VK_RIGHT)
					iButton++;

				// Wrap around the menu bar if needed.
				if (iButton < 0)
					iButton = cButtons - 1;
				if (iButton >= cButtons)
					iButton = 0;
				::SendMessage(hwndTool, TB_GETBUTTON, iButton, (LPARAM)&tbb);
			} while (tbb.idCommand == -1);

			if (s_qmnbrCur->m_nIdOld != tbb.idCommand)
			{
				Assert(s_hhook);
				::UnhookWindowsHookEx(s_hhook);
				s_hhook = NULL;

				s_fIgnoreMouseMove = true;
				::SendMessage(s_qmnbrCur->m_hwndMain, WM_CANCELMODE, 0, 0);
				::PostMessage(hwndTool, s_wmShowMenu, 0, tbb.idCommand);
				return 0;
			}
		}
		break;

	case WM_MENUSELECT:
		s_hmenuOld = (HMENU)pmsg->lParam;
		s_fOverSubMenu = (HIWORD(pmsg->wParam) & MF_POPUP) != 0;
		break;
	}

	Assert(s_hhook);
	return ::CallNextHookEx(s_hhook, code, wParam, lParam);
}

/*----------------------------------------------------------------------------------------------
	Show the popup menu for the button under the mouse point.

	@param pt Screen location of a mouse click.
	@param pptss Address of a pointer to an ITsString COM object for returning the help string.

	@return True if successful, false if no help string is available for the given screen
					location.
----------------------------------------------------------------------------------------------*/
bool AfMenuBar::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	Point ptClient(pt);
	::ScreenToClient(m_hwnd, &ptClient);

	int iButton = ::SendMessage(m_hwnd, TB_HITTEST, 0, (LPARAM)&ptClient);
	if (iButton >= 0)
	{
		// Instead of giving help for the menu button, show the popup menu instead.
		TBBUTTON tbb;
		::SendMessage(m_hwnd, TB_GETBUTTON, iButton, (LPARAM)&tbb);
		ShowMenu(tbb.idCommand);
		return false;
	}

	return SuperClass::GetHelpStrFromPt(pt, pptss);
}

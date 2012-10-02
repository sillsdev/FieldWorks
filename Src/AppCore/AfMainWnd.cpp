/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfMainWnd.cpp
Responsibility: Darrell Zook
Last reviewed:

Description:
	This file contains the class definition for the following class:
		AfMainWnd : AfWnd
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

// Window ids for standard child windows.
#define kctidRebar     1000
#define kctidStatusBar 1001

// These static variables are used for "What's This" help.
// This captures messages before they are processed by the main message loop.
HHOOK AfMainWnd::s_hhook;
// This captures menu messages.
HHOOK AfMainWnd::s_hhookFilter;
// This is true when a menu is open.
bool AfMainWnd::s_fInMenu;
// This is the flags received with the last WM_MENUSELECT message. It is used to determine
// whether a menu item is enabled or not.
uint s_mnuFlag = 0;
// This is the last menu item we were over. It is used when the user hits the Enter key
// when they have a menu item selected.
uint s_mnuLastID = 0;
// This is the handle to the latest menu. It is used to determine whether or not the
// user clicked on a menu item or an another window when a menu is open.
static HMENU s_hmenuCurrent;

uint s_wmShowHelp = ::RegisterWindowMessage(_T("AfMainWnd ShowHelp"));
uint s_wmActivate = ::RegisterWindowMessage(_T("AfMainWnd Activate"));
uint s_wm_kmselectlang = ::RegisterWindowMessage(_T("WM_KMSELECTLANG"));
uint s_wm_kmkbchange = ::RegisterWindowMessage(_T("WM_KMKBCHANGE"));

extern const achar * kpszWndPositionValue;


/*----------------------------------------------------------------------------------------------
	The command map for a generic frame window.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(AfMainWnd)
	ON_CID_GEN(kcidColorChange, &AfMainWnd::CmdSettingChange, NULL)
	ON_CID_GEN(kcidEndColorChange, &AfMainWnd::CmdSettingChange, NULL)
	ON_CID_GEN(kcidSettingChange, &AfMainWnd::CmdSettingChange, NULL)
	ON_CID_GEN(kcidEndSettingChange, &AfMainWnd::CmdSettingChange, NULL)
	ON_CID_ME(kcidFilePageSetup, &AfMainWnd::CmdFilePageSetup, NULL)

	ON_CID_GEN(kcidExpToolbars, &AfMainWnd::CmdTbToggle, &AfMainWnd::CmsTbUpdate)
END_CMD_MAP_NIL()


/***********************************************************************************************
	AfMainWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfMainWnd::AfMainWnd() : m_mum(this)
{
	m_himlBorderCombo = NULL;

	m_clrFore = kclrRed;
	m_clrBack = kclrYellow;
	m_bpBorderPos = kbpAll;
	m_fActiveWindow = false;
	m_prootbActive = NULL;

	// Printing-related initializers
	m_dxmpLeftMargin = 90000;
	m_dxmpRightMargin = 90000;
	m_dympTopMargin = 72000;
	m_dympBottomMargin = 72000;
	m_dympHeaderMargin = 36000;
	m_dympFooterMargin = 36000;
	m_nOrient = kPort;
	m_sPgSize = kSzLtr;
	m_dxmpPageWidth = kdzmpInch * 17/2;
	m_dympPageHeight = kdzmpInch * 11;
	m_fHeaderOnFirstPage = false;
	ModuleEntry::ModuleAddRef();
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfMainWnd::~AfMainWnd()
{
	if (m_himlBorderCombo)
	{
		AfGdi::ImageList_Destroy(m_himlBorderCombo);
		m_himlBorderCombo = NULL;
	}
	// This will post a quit when the last window closes, unless the explorer still has
	// an IFwTools object for this application. (Or, eventually, some other app has
	// some other COM object we implement, or has our class factory locked.)
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	The window has just been attached. Create the child windows.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::PostAttach(void)
{
	AssertObj(this);
	Assert(m_hwnd != NULL);
	Assert(!m_qstbr);

	SuperClass::PostAttach();

	AfApp::Papp()->AddWindow(this);
	AfApp::Papp()->SetCurrentWindow(this);

	m_qrebr.Create();
	m_qrebr->CreateHwnd(m_hwnd, kctidRebar);
	m_qstbr.Create();
	m_qstbr->CreateHwnd(m_hwnd, kctidStatusBar);
	// This is nice, but it doesn't work because a typical client calls SuperClass::PostAttach
	// BEFORE creating the toolbar that needs updating. Leave here as a reminder that if we
	// do what we probably should and move the creation of the standard toolbars here, this will
	// be a good place to do it.
	AfToolBar * ptlbr = GetToolBar(kridTBarFmtg);
	if (ptlbr)
		ptlbr->UpdateIconImage(kcidFmttbApplyBdr, m_himlBorderCombo, m_bpBorderPos);

	StrUni stuFooter = L"&[page],&[date]";
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	CheckHr(qtsf->MakeStringRgch(stuFooter.Chars(), stuFooter.Length(), UserWs(),
		&m_qtssFooter));
}


/*----------------------------------------------------------------------------------------------
	Load UI settings specific to this window.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::LoadSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);

	SuperClass::LoadSettings(pszRoot, fRecursive);
	DWORD dwT;

	FwSettings * pfws = AfApp::GetSettings();

	// TODO DarrellZ: These next lines need to be moved out of here into another class.
	// Get the foreground and background colors.
	if (pfws->GetDword(pszRoot, _T("Fore Color"), &dwT))
		m_clrFore = (COLORREF)dwT;
	if (pfws->GetDword(pszRoot, _T("Back Color"), &dwT))
		m_clrBack = (COLORREF)dwT;
	if (pfws->GetDword(pszRoot, _T("Border"), &dwT) && dwT >= 0 && dwT < kbpLim)
		m_bpBorderPos = dwT;
}


/*----------------------------------------------------------------------------------------------
	Save UI settings specific to this window.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::SaveSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);

	SuperClass::SaveSettings(pszRoot);

	FwSettings * pfws = AfApp::GetSettings();

	// TODO DarrellZ: These next lines need to be moved out of here into another class.
	// Store the foreground and background colors.
	pfws->SetDword(pszRoot, _T("Fore Color"), m_clrFore);
	pfws->SetDword(pszRoot, _T("Back Color"), m_clrBack);
	pfws->SetDword(pszRoot, _T("Border"), m_bpBorderPos);
}


/*----------------------------------------------------------------------------------------------
	Calculate a suitable position and size for a new window, so as to look obviously like a new
	window, rather than sneakily lying on top of an existing window.

	A hack is used to write the new location to the registry so that the new window will be
	moved to the correct location before it becomes visible. The LoadSettings method makes the
	window visible, so we can't just move it after a call to qwnd->CreateHwnd.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::PrepareNewWindowLocation()
{
	WINDOWPLACEMENT wp = { isizeof(wp) };
	::GetWindowPlacement(m_hwnd, &wp);
	Rect rc(wp.rcNormalPosition);
	int dypCaption = ::GetSystemMetrics(SM_CYCAPTION) + ::GetSystemMetrics(SM_CYSIZEFRAME);
	Rect rcOriginal(rc);
	rc.Offset(dypCaption, dypCaption);
	AfGfx::EnsureVisibleRect(rc);
	// Check if new window will sit virtually on top of original window:
	if (abs(rcOriginal.left - rc.left) < dypCaption
		&& abs(rcOriginal.top - rc.top) < dypCaption)
	{
		if (rcOriginal.left >= dypCaption || rcOriginal.top >= dypCaption)
		{
			// Move so that top left of window is at top left of display:
			rc.left = rc.top = 0;
			rc.right = rcOriginal.Width();
			rc.bottom = rcOriginal.Height();
		}
	}
	FwSettings * pfws = AfApp::GetSettings();
	pfws->SetBinary(NULL, kpszWndPositionValue, (BYTE *)&rc, isizeof(RECT));
}

/*----------------------------------------------------------------------------------------------
	Add the root box to the vector of root boxes. Also set the overlay for the new root box to
	the current overlay.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::RegisterRootBox(IVwRootBox * prootb)
{
	AssertPtr(prootb);
	m_vqrootb.Push(prootb);
}


/*----------------------------------------------------------------------------------------------
	Remove the root box from the vector of root boxes.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::UnregisterRootBox(IVwRootBox * prootb)
{
	AssertPtr(prootb);

	int crootb = m_vqrootb.Size();
	for (int irootb = 0; irootb < crootb; irootb++)
	{
		if (SameObject(m_vqrootb[irootb], prootb))
		{
			m_vqrootb.Delete(irootb);
			return;
		}
	}

	// REVIEW DarrellZ: Should we assert if this happens?
	// Assert(false); // This should never happen.
}


/*----------------------------------------------------------------------------------------------
	Store the active rootbox for access later. This takes care of adding and removing the
	view window associated with the rootbox to and from the list of command handlers.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::SetActiveRootBox(IVwRootBox * prootb, bool fTbControl)
{
	AssertPtrN(prootb);
	AfVwRootSitePtr qvrs;

	// Remove the previous view window (if there is one) from the list of command handlers.
	if (GetActiveViewWindow(&qvrs))
		AfApp::Papp()->RemoveCmdHandler(qvrs->Window(), 1);

	if (fTbControl)
	{
		// The new active box is a toolbar control. Remember the main root box also.
		if (m_prootbPrev == NULL)
			m_prootbPrev = m_prootbActive;
	}
	else
		m_prootbPrev = NULL;

	m_prootbActive = prootb;

	// Add the active view window (if there is one) to the list of command handlers.
	if (GetActiveViewWindow(&qvrs))
		AfApp::Papp()->AddCmdHandler(qvrs->Window(), 1, kgrfcmmAll);
}


/*----------------------------------------------------------------------------------------------
	Return true if there is an active view window. Set *ppvwnd to the active root site if there
	is one. When the function is called, if pgrfvfs is not NULL, it should contain one or more
	values in the ViewFormatState enum. For every value in pgrfvfs, the selection will be
	checked to see if that type of formatting is available. When the function returns, pgrfvfs
	will contain one or more of the original values in pgrfvfs, depending on the current
	selection. For example, if *pgrfvfs = kvfsPara | kvfsChar when this function is called, and
	the current selection supports paragraph, but not character, formatting, pgrfvfs will be
	set to kvfsPara when the function returns. In this example, overlay formatting will not
	even be checked to see if it is possible, since it was not contained in pgrfvfs when the
	function was called.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::GetActiveViewWindow(AfVwRootSite ** ppvwnd, int * pgrfvfs, bool fTbControlOkay)
{
	AssertPtr(ppvwnd);
	AssertPtrN(pgrfvfs);

	if (!m_prootbActive)
		return false;

	IVwRootBoxPtr qrootb = GetActiveRootBox(fTbControlOkay);

	IVwRootSitePtr qvrs;
	CheckHr(qrootb->get_Site(&qvrs));
	*ppvwnd = dynamic_cast<AfVwRootSite *>(qvrs.Ptr());
	if (!*ppvwnd)
		return false;
	AddRefObj(*ppvwnd);

	if (pgrfvfs)
	{
		IVwSelectionPtr qvwsel;
		CheckHr(qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
		{
			*pgrfvfs = 0;
			return true;
		}

		ComBool fCanFormat;
		int grfvfs = 0;
		int vfsMask = *pgrfvfs;

		IVwRootSitePtr qvrs;
		m_prootbActive->get_Site(&qvrs);
		AfVwRootSitePtr qavrs = dynamic_cast<AfVwRootSite *>(qvrs.Ptr());
		if (qavrs && !qavrs->SelectionInOneField())
		{} // selection spans multiple fields: can't edit
		else
		{
			if (vfsMask & kvfsPara)
			{
				CheckHr(qvwsel->get_CanFormatPara(&fCanFormat));
				if (fCanFormat)
					grfvfs |= kvfsPara;
			}
			if (vfsMask & kvfsChar)
			{

				CheckHr(qvwsel->get_CanFormatChar(&fCanFormat));
				if (fCanFormat)
					grfvfs |= kvfsChar;
			}
			if (vfsMask & kvfsOverlay)
			{
				CheckHr(qvwsel->get_CanFormatOverlay(&fCanFormat));
				if (fCanFormat)
					grfvfs |= kvfsOverlay;
			}
		}
		*pgrfvfs = grfvfs;
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Step through the register root boxes and inform them of the overlay change. pvo can be NULL
	to remove all overlays.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::SetCurrentOverlay(IVwOverlay * pvo)
{
	AssertPtrN(pvo);

	int crootb = m_vqrootb.Size();
	for (int irootb = 0; irootb < crootb; irootb++)
	{
		// Try to save some scroll position info
		AfVwSelInfo avsi;
		bool fGotSelInfo = avsi.LoadVisible(m_vqrootb[irootb]);

		// Make the actual change.
		CheckHr(m_vqrootb[irootb]->putref_Overlay(pvo));

		// Try to restore position
		if (fGotSelInfo)
		{
			IVwSelectionPtr qsel;
			// MakeVisible will fail if we don't have a selection.
			m_vqrootb[irootb]->get_Selection(&qsel);
			if (qsel)
				avsi.MakeVisible(m_vqrootb[irootb], false, NULL);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(!lnRet);

	if (wm == s_wmShowHelp)
	{
		// wp contains the menu flag from the last WM_MENUSELECT message.
		// lp contains the ID of the last menu item.
		Assert(lp);

		StrApp str;
		ResourceStringType rst;
		if (wp & MF_DISABLED)
			rst = krstWhatsThisDisabled;
		else
			rst = krstWhatsThisEnabled;
		if (lp >= kcidMenuItemDynMin && lp < kcidMenuItemDynLim)
		{
			// Call the handler that will return the expanded menu item help text.
			// REVIEW DarrellZ: Right now this uses the text that is shown in the status bar for
			// the item help text. If we need a different string, define a new constant in
			// AfMenuMgr and use it instead of kmaGetStatusText below. This means that every
			// place expandable menus are used (in the main window) should handle the new
			// constant.
			HMENU hmenu;
			int idDummy;
			GetMenuMgr()->GetLastExpMenuInfo(&hmenu, &idDummy);
			CmdPtr qcmd;
			qcmd.Attach(NewObj Cmd);
			qcmd->m_cid = idDummy;
			qcmd->m_rgn[0] = AfMenuMgr::kmaGetStatusText;
			qcmd->m_rgn[1] = lp - kcidMenuItemDynMin;
			qcmd->m_rgn[2] = (int)&str;
			qcmd->m_qcmh = m_qwndContext; // Needed for context help.
			AfApp::Papp()->FDispatchCmd(qcmd);
		}
		else
		{
			AfUtil::GetResourceStr(rst, lp, str);
		}
		if (str.Length() == 0)
		{
			str.Load(kstidNoHelpError);
		}

		Point pt;
		::GetCursorPos(&pt);

		AfContextHelpWndPtr qchw;
		qchw.Attach(NewObj AfContextHelpWnd);
		ITsStringPtr qtss;
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		StrUni stu(str);
		CheckHr(qtsf->MakeString(stu.Bstr(), UserWs(), &qtss));
		qchw->Create(m_hwnd, qtss, pt);
		return true;
	}

	// Catch the activation message we posted from WM_ACTIVATE. If any changes are made
	// here, make sure you can properly switch between two windows, both with overlay
	// tools open. Also, make sure you don't send excessive SetFocus messages when
	// activating/deactivating windows and switching between fields.

	// Note (JohnT): When the window is closed, we don't get an s_wmActivate, I think because
	// the window is destroyed altogether before it gets to the head of the event queue.
	// So don't count on it too much. It's only really good for closing palette windows,
	// which is safe because Windows does it anyway when closing the owning window.
	if (wm == s_wmActivate)
	{
		bool fActivate = wp;
		//StrAppBuf strb;
		//strb.Format("Into s_wmActivate with fActivate=%d, m_fActiveWindow=%d, m_hwnd=%x\n",fActivate, m_fActiveWindow, m_hwnd);
		//OutputDebugString(strb.Chars());

		// Handle rootbox activation and open/close overlay windows associated with
		// this frame (in the data notebook, etc.)
		OnActivate(fActivate, (HWND)lp);

		// If the focus window is not in my ownership, and we are activating the window,
		// then set the focus.
		HWND hwndFocus = ::GetFocus();
		HWND hwnd = 0;
		if (hwndFocus)
			hwnd = ::GetAncestor(hwndFocus, GA_ROOT);
		if (hwnd != m_hwnd && fActivate)
		{
			// This is needed when an overlay tool is open and we click on a notebook window
			// from another notebook window. Without forcing a focus switch, the focus actually
			// ends up in the overlay tool. However, we don't normally want to
			// force a focus switch because it does unnecessary work and can cause
			// problems such as moving the focus out of the size combobox on the toolbar.
			//OutputDebugString("s_wmActivate in AfMainWnd -> SetFocus to frame\n");
			::SetFocus(m_hwnd);
		}
		return true;
	}

	// These non-const messages need to be forwarded to the active root box if any.
	// Can't be in the switch because non-constant.
	if (wm == s_wm_kmselectlang)
	{
		AfVwRootSitePtr qvrs;
		if (GetActiveViewWindow(&qvrs))
		{
			lnRet = ::SendMessage(qvrs->Window()->Hwnd(), wm, wp, lp);
			return true;
		}
	}

	switch (wm)
	{
	case WM_ACTIVATE:
		{
			//StrAppBuf strb;
			//strb.Format("Into WM_ACTIVATE with wp=%x, m_fActiveWindow=%d, m_hwnd=%x, lp=%x\n",wp, m_fActiveWindow, m_hwnd, lp);
			//OutputDebugString(strb.Chars());
			// We don't want to activate/deactive the main window when we are starting up
			// a child window such as a floating overlay tool.
			// Therefore, if the window to be activated/deactivated is not my child and
			// the activation needs to change.
			if ((!lp) || ::GetWindow((HWND)lp, GW_OWNER) != m_hwnd)
			{
				if (LOWORD(wp) != WA_INACTIVE && !m_fActiveWindow)
				{
					// We are currently inactive, so activate the window.
					//OutputDebugString("Post s_wmActivate on from WM_ACTIVATE\n");
					OnPreActivate(true);
					::PostMessage(m_hwnd, s_wmActivate, 1, 0);
				}
				else if (LOWORD(wp) == WA_INACTIVE && m_fActiveWindow)
				{
					// We are currently active, so deactivate the window.
					//OutputDebugString("Post s_wmActivate off from WM_ACTIVATE\n");
					OnPreActivate(false);
					::PostMessage(m_hwnd, s_wmActivate, 0, 0);
				}
				else
				{
					//OutputDebugString("WM_ACTIVATE ignored(1)\n");
				}
			}
			else
			{
				//OutputDebugString("WM_ACTIVATE ignored(2)\n");
			}
			break;
		}

	case WM_SYSCOMMAND:
		// If the user hits the Alt key without any other key, set the focus
		// to the menu bar. If the menu bar already has the focus, set the focus
		// back to the main window.
		// Note: This assumes the only toolbar that can be a menu is the first
		// toolbar.
		if (wp == SC_KEYMENU && lp == 0 && (m_vqtlbr.Size() > 0))
		{
			AfMenuBar * pmenu = dynamic_cast<AfMenuBar *>(m_vqtlbr[0].Ptr());
			if (pmenu)
			{
				::SendMessage(m_hwnd, WM_CANCELMODE, 0, 0);
				::SetFocus(pmenu->Hwnd());
				return true;
			}
		}
		break;

	case WM_DESTROY:
		AfApp::Papp()->RemoveWindow(this);
		return false;

	case WM_SETFOCUS:
		AfApp::Papp()->SetCurrentWindow(this);
		break;

	case WM_CLOSE:
		return OnClose();

	// Wait 1/2 second after a menu is closed before resetting the text on the status bar.
	// If another menu shows up in that time interval, cancel the timer.
	case WM_ENTERMENULOOP:
		::KillTimer(m_hwnd, AfStatusBar::knMenuTimer);
		break;
	case WM_EXITMENULOOP:
		::SetTimer(m_hwnd, AfStatusBar::knMenuTimer, 500, NULL);
		break;
	case WM_TIMER:
		// Reset the status bar text.
		m_qstbr->RestoreStatusText();
		::KillTimer(m_hwnd, AfStatusBar::knMenuTimer);
		break;

	case WM_SIZING:
		{
			// Keep the main window from resizing below a certain point.
			Rect * prc = (Rect *)lp;
			AssertPtr(prc);

			int dypMin = GetMinHeight();
			if (prc->bottom - prc->top < dypMin)
			{
				if (wp == WMSZ_TOPLEFT || wp == WMSZ_TOP || wp == WMSZ_TOPRIGHT)
					prc->top = prc->bottom - dypMin;
				else
					prc->bottom = prc->top + dypMin;
			}

			int dxpMin = GetMinWidth();
			if (prc->right - prc->left < dxpMin)
			{
				if (wp == WMSZ_TOPLEFT || wp == WMSZ_LEFT || wp == WMSZ_BOTTOMLEFT)
					prc->left = prc->right - dxpMin;
				else
					prc->right = prc->left + dxpMin;
			}
		}
		break;

	case WM_HELP:
		// Handle application-independent help (e.g. from DbAccess).
		if (m_strFullHelpUrl)
		{
			HtmlHelp(m_hwnd, m_strFullHelpUrl, HH_DISPLAY_TOPIC, NULL);
		}
		return true;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Set the full help URL for display of application-independent help.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::SetFullHelpUrl(const achar * pszFullHelpUrl)
{
	m_strFullHelpUrl.Assign(pszFullHelpUrl);
}


/*----------------------------------------------------------------------------------------------
	Clear the full help URL.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::ClearFullHelpUrl()
{
	m_strFullHelpUrl.Clear();
}

/*----------------------------------------------------------------------------------------------
	Create a new toolbar and add it to the rebar.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::CreateToolBar(AfToolBar * ptlbr, bool fShow, bool fBreak, int cx)
{
	AssertPtr(ptlbr);
	Assert(!ptlbr->Hwnd());

	ptlbr->Create(this);

	REBARBANDINFO rbbi = { isizeof(rbbi) };
	rbbi.fMask = RBBIM_STYLE | RBBIM_CHILD | RBBIM_CHILDSIZE | RBBIM_SIZE | RBBIM_ID;
	rbbi.fStyle = RBBS_CHILDEDGE | RBBS_GRIPPERALWAYS;
	if (dynamic_cast<AfMenuBar *>(ptlbr) != NULL)
		rbbi.fStyle |= RBBS_USECHEVRON; // Turn chevrons on for menu toolbars.

	HWND hwndToolbar = ptlbr->Hwnd();
	rbbi.fMask |= RBBIM_IDEALSIZE;

	// Since TB_GETMAXSIZE doesn't really return the right value, find the width of the
	// toolbar.
	Rect rc;
	int cxIdeal = 0;
	int cbtn = ::SendMessage(hwndToolbar, TB_BUTTONCOUNT, 0, 0);
	for (int ibtn = 0; ibtn < cbtn; ibtn++)
	{
		::SendMessage(hwndToolbar, TB_GETITEMRECT, ibtn, (LPARAM)&rc);
		cxIdeal += rc.Width();
	}
	rbbi.cxIdeal = cxIdeal;

	if (!fShow)
		rbbi.fStyle |= RBBS_HIDDEN;
	if (fBreak)
		rbbi.fStyle |= RBBS_BREAK;
	rbbi.wID = ptlbr->GetWindowId();

	// Get the height of the toolbar.
	DWORD dwBtnSize = ::SendMessage(ptlbr->Hwnd(), TB_GETBUTTONSIZE, 0, 0);

	rbbi.hwndChild = ptlbr->Hwnd();
	rbbi.cxMinChild = 0;
	rbbi.cyMinChild = HIWORD(dwBtnSize);
	rbbi.cx = cx;
	rbbi.cyChild = HIWORD(dwBtnSize);
	::SendMessage(m_qrebr->Hwnd(), RB_INSERTBAND, (WPARAM)-1, (LPARAM)&rbbi);
}


/*----------------------------------------------------------------------------------------------
	This returns the toolbar given its id.
----------------------------------------------------------------------------------------------*/
AfToolBar * AfMainWnd::GetToolBar(int wid)
{
	AssertObj(this);

	for (int itlbr = 0; itlbr < m_vqtlbr.Size(); itlbr++)
	{
		if (m_vqtlbr[itlbr]->GetWindowId() == wid)
			return m_vqtlbr[itlbr];
	}

	return NULL;
}


/*----------------------------------------------------------------------------------------------
	Find the band that owns the given hwnd. Returns -1 if it's not found.
----------------------------------------------------------------------------------------------*/
int AfMainWnd::GetBandFromHwnd(HWND hwnd)
{
	AssertObj(this);
	Assert(hwnd);

	REBARBANDINFO rbbi = { isizeof(rbbi) };
	HWND hwndRebr = m_qrebr->Hwnd();

	int cband = ::SendMessage(hwndRebr, RB_GETBANDCOUNT, 0, 0);
	for (int iband = 0; iband < cband; iband++)
	{
		rbbi.fMask = RBBIM_CHILD;
		::SendMessage(hwndRebr, RB_GETBANDINFO, iband, (LPARAM)&rbbi);
		if (rbbi.hwndChild == hwnd)
			return iband;
	}

	return -1;
}


/*----------------------------------------------------------------------------------------------
	Shows or hides a toolbar given its ID. If the toolbar has not yet been created
	and fShow is true, it will be created and added to the bottom of the rebar.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::ShowToolBar(int wid, bool fShow)
{
	AfToolBar * ptlbr = GetToolBar(wid);

	if (ptlbr)
	{
		Assert(ptlbr->Hwnd());
		int iband = GetBandFromHwnd(ptlbr->Hwnd());

		REBARBANDINFO rbbi = { isizeof(rbbi), RBBIM_STYLE };
		REBARBANDINFO rbbiNext = { isizeof(rbbiNext), RBBIM_STYLE };
		if (!fShow)
		{
			// If the band given by wid has the RBBS_BREAK style on it and the next band does
			// not, add this style to the next band so that the line doesn't disappear.
			if (::SendMessage(m_qrebr->Hwnd(), RB_GETBANDINFO, iband, (LPARAM)&rbbi) &&
				::SendMessage(m_qrebr->Hwnd(), RB_GETBANDINFO, iband + 1, (LPARAM)&rbbiNext) &&
				(rbbi.fStyle & RBBS_BREAK) &&
				!(rbbiNext.fStyle & RBBS_BREAK))
			{
				rbbiNext.fStyle |= RBBS_BREAK;
				::SendMessage(m_qrebr->Hwnd(), RB_SETBANDINFO, iband + 1, (LPARAM)&rbbiNext);
			}
		}

		::SendMessage(m_qrebr->Hwnd(), RB_SHOWBAND, iband, fShow);
		if (!fShow && ::IsChild(m_qrebr->Hwnd(), ::GetFocus()))
			::SetFocus(m_hwnd);
	}
	else
	{
		Warn("Could not find the requested toolbar.\n");
	}
}


/*----------------------------------------------------------------------------------------------
	Load and create toolbars from user-defined settings.
	Parameters:
		dwBarFlags - bit mask specifying which toolbars should be visible. A toolbar
		will be visible if its bit (based on the index in m_vqtlbr) is set in dwBarFlags.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::LoadToolbars(FwSettings * pfws, const achar * pszRoot, DWORD dwBarFlags)
{
	AssertPtr(pfws);
	AssertPszN(pszRoot);

	StrApp str;
	int ctlbr = m_vqtlbr.Size();
	Assert(ctlbr < 0x8000);
	Vector<DWORD> vflag;
	vflag.Resize(ctlbr);
	int itlbr;
	for (itlbr = 0; itlbr < ctlbr; itlbr++)
	{
		str.Format(_T("Band-%d"), itlbr);
		if (!pfws->GetDword(pszRoot, str.Chars(), &vflag[itlbr]))
			break;
	}
	if (itlbr != ctlbr)
	{
		// The settings could not be loaded successfully, so use default values.
		LoadDefaultToolbarFlags(vflag, dwBarFlags);
	}

	// Everything was loaded from the settings successfully.
	for (int iband = 0; iband < ctlbr; iband++)
	{
		const int kmask = 0x8000;
		DWORD dwT = vflag[iband];
		int itlbr = HIWORD(dwT) & ~kmask;
		Assert((uint)itlbr < (uint)ctlbr);
		bool fBreak = HIWORD(dwT) & kmask;
		int dxpBar = LOWORD(dwT);
		CreateToolBar(m_vqtlbr[itlbr], dwBarFlags & (1 << itlbr), fBreak, dxpBar);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Load the default toolbar setup, including each toolbar's width and
	whether or not it should be on a separate line.
	Each band has the following format for the flag:
		LOWORD(vflag[iband])           = width of the band
		HIWORD(vflag[iband]) & 0x8000  = 1 if the next toolbar should be on a new line
		HIWORD(vflag[iband]) & ~0x8000 = toolbar index in m_vqtlbr
	Subclasses should override this in order to use different default settings.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::LoadDefaultToolbarFlags(Vector<DWORD> & vflag, DWORD & dwBarFlags)
{
	int cband = m_vqtlbr.Size();
	for (int iband = 0; iband < cband; iband++)
	{
		// The next three lines are the only lines that should be modified.
		// The fourth line should stay the same in subclasses.
		int itlbr = iband;
		bool fBreak = true;
		int dxpBar = 50;
		vflag[iband] = MAKELPARAM(dxpBar, itlbr | (fBreak << 15));
	}
}


/*----------------------------------------------------------------------------------------------
	Save toolbar settings.
	This assumes that the mask values for the toolbars are as follows:
		0x0001 = first toolbar,
		0x0002 = second toolbar,
		0x0004 = third toolbar,
		etc.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::SaveToolbars(FwSettings * pfws, const achar * pszRoot,
	const achar * pszKey)
{
	AssertPtr(pfws);
	AssertPszN(pszRoot);
	AssertPsz(pszKey);
	Assert(m_vqtlbr.Size() == ::SendMessage(m_qrebr->Hwnd(), RB_GETBANDCOUNT, 0, 0));
	if (m_fFullWindow)
		return;

	// Save the band order, size, and important style information.
	// Also save the toolbar visibility information.
	StrApp str;
	HWND hwndRebar = m_qrebr->Hwnd();
	int cband = m_vqtlbr.Size();
	REBARBANDINFO rbbi = { isizeof(rbbi), RBBIM_CHILD | RBBIM_STYLE | RBBIM_SIZE };
	DWORD dwToolbarFlags = 0;
	for (int iband = 0; iband < cband; iband++)
	{
		::SendMessage(hwndRebar, RB_GETBANDINFO, iband, (LPARAM)&rbbi);
		int itlbr;
		for (itlbr = 0; itlbr < cband; itlbr++)
		{
			AssertPtr(m_vqtlbr[itlbr]);
			if (m_vqtlbr[itlbr]->Hwnd() == rbbi.hwndChild)
				break;
		}
		Assert(itlbr < cband);
		str.Format(_T("Band-%d"), iband);
		// If this Assert ever fires, we're going to have to adjust how the toolbar info is put
		// into one DWORD. The index could probably use less bits if it needed to.
		Assert((rbbi.cx & 0xFFFF0000) == 0);
		DWORD dwT = MAKELONG(rbbi.cx, itlbr);
		if (rbbi.fStyle & RBBS_BREAK)
			dwT |= 0x80000000;
		pfws->SetDword(pszRoot, str.Chars(), dwT);

		if (!(rbbi.fStyle & RBBS_HIDDEN))
			dwToolbarFlags |= (1 << itlbr);
	}
	pfws->SetDword(pszRoot, pszKey, dwToolbarFlags);
}


/*----------------------------------------------------------------------------------------------
	Initialize special buttons on the toolbars.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::OnToolBarButtonAdded(AfToolBar * ptlbr, int ibtn, int cid)
{
	AssertPtr(ptlbr);

	switch (cid)
	{
	case kcidFmttbApplyBdr:
		{
			// Get the icons.
			if (!m_himlBorderCombo)
				m_himlBorderCombo = AfGdi::ImageList_Create(16, 15, ILC_COLORDDB | ILC_MASK,
					0, 0);
			if (!m_himlBorderCombo)
				ThrowHr(WarnHr(E_FAIL));
			HBITMAP hbmp = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(),
				MAKEINTRESOURCE(kridFmtBorderBtns));
			if (!hbmp)
				ThrowHr(WarnHr(E_FAIL));
			if (::ImageList_AddMasked(m_himlBorderCombo, hbmp, kclrPink) == -1)
				ThrowHr(WarnHr(E_FAIL));
			AfGdi::DeleteObjectBitmap(hbmp);
			// Leave this here just in case, but it currently doesn't work because
			// the toolbar's images have not yet been loaded.
			ptlbr->UpdateIconImage(kcidFmttbApplyBdr, m_himlBorderCombo, m_bpBorderPos);
		}

		// FALL THROUGH
//	case kcidEditUndo:	// Review: do we want drop-down buttons on the Undo/Redo widgets?
//	case kcidEditRedo:
	case kcidFmttbApplyBgrndColor:
	case kcidFmttbApplyFgrndColor:
		{
			// Add the drop down style to the button.
			TBBUTTONINFO tbbi = { isizeof(tbbi), TBIF_STYLE };
			::SendMessage(ptlbr->Hwnd(), TB_GETBUTTONINFO, cid, (long)&tbbi);
			tbbi.fsStyle |= TBSTYLE_DROPDOWN;
			::SendMessage(ptlbr->Hwnd(), TB_SETBUTTONINFO, cid, (long)&tbbi);
		}
		break;
	case kcidFmttbBold:
	case kcidFmttbItal:
		{
			// Set the style of the button to checked/unchecked.
			TBBUTTONINFO tbbi = { isizeof(tbbi), TBIF_STYLE };
			::SendMessage(ptlbr->Hwnd(), TB_GETBUTTONINFO, cid, (long)&tbbi);
			tbbi.fsStyle |= TBSTYLE_CHECK;
			::SendMessage(ptlbr->Hwnd(), TB_SETBUTTONINFO, cid, (long)&tbbi);
		}
		break;
	case kcidFmttbAlignLeft:
	case kcidFmttbAlignCntr:
	case kcidFmttbAlignRight:
		{
			// Set the style of the button to a checked/unchecked group.
			TBBUTTONINFO tbbi = { isizeof(tbbi), TBIF_STYLE };
			::SendMessage(ptlbr->Hwnd(), TB_GETBUTTONINFO, cid, (long)&tbbi);
			tbbi.fsStyle |= TBSTYLE_CHECKGROUP;
			::SendMessage(ptlbr->Hwnd(), TB_SETBUTTONINFO, cid, (long)&tbbi);
		}
		break;
	}
}


/*----------------------------------------------------------------------------------------------
	Ask the root boxes to redraw based on stylesheet changes. Also send a notification to
	all child windows.

	Note (JohnT): the notification of all child windows was added later. Now we have it, we
	could possibly override OnStylesheetChange on windows that hold root boxes. However, we
	need the list of root boxes for some other things, so it is simpler not to change what is
	already working.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::OnStylesheetChange()
{
	SuperClass::OnStylesheetChange();
	int crootb = m_vqrootb.Size();
	for (int irootb = 0; irootb < crootb; irootb++)
		CheckHr(m_vqrootb[irootb]->OnStylesheetChange());
	::EnumChildWindows(m_hwnd, &AfWnd::EnumChildSsChange, (LPARAM)0);
}


/*----------------------------------------------------------------------------------------------
	Get the bounding rectangle of the client part that isn't occupied by toolbars, etc.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::GetClientRect(Rect & rc)
{
	Rect rcT;

	::GetClientRect(m_hwnd, &rc);
	// JohnT: I'm not entirely sure how we can have m_qrebr and it not have an HWND, but
	// it occurs somewhere during startup and BoundsChecker complains, so I put in an
	// extra check.
	if (m_qrebr && m_qrebr->Hwnd() && ::IsWindowVisible(m_qrebr->Hwnd()))
	{
		::GetClientRect(m_qrebr->Hwnd(), &rcT);
		rc.top += rcT.Height() + 1;
	}
	if (m_qstbr && ::IsWindowVisible(m_qstbr->Hwnd()))
	{
		::GetClientRect(m_qstbr->Hwnd(), &rcT);
		rc.bottom -= rcT.Height();
	}
}


/*----------------------------------------------------------------------------------------------
	Reposition our child windows - status bar, etc.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::OnSize(int wst, int dxs, int dys)
{
	SuperClass::OnSize(wst, dxs, dys);

	// After several days, I (KenZ) tracked down a very elusive bug that may only happen on
	// release builds running outside the debugger with the main window on a secondary monitor
	// when running XP and when another process, such as backing up the database takes more
	// than several seconds. But there are also other places that apparently run into similar
	// problems. Apparently what happens in the first case, is XP thinks the main window is
	// no longer responding, so it inactivates the main window and places a new ghost window
	// in the same Z-order (as long as the debugger is not running) where the original window
	// was located. Then at a certain point in the backup, it sends a WM_WINDOWPOSCHANGED
	// message to the original main window (this window) while the window is still inactive
	// (WS_VISIBLE is not on). The default handler translates this into WM_SIZE and WM_MOVE
	// messages. Without the following test, we can get a messed up client window which takes
	// up the full main window because various OnSize, OnClientSize, and GetClientRect methods
	// use IsWindowVisible to determine if the rebar, viewbar, caption bar, and status bar
	// are visible. As long as the main window is deemed not visible, all of these others
	// return false as well, resulting in invalid sizing of the client window. So to get around
	// the problem, we simply abort the OnSize method here without calling any of the other
	// methods. A corresponding test is also performed in RecMainWnd::OnClientSize. An alternate,
	// but more extensive solution to this problem would be to stop using IsWindowVisible to
	// determine whether various bars are enabled and keep track of this information in member
	// variables.
	if (!::IsWindowVisible(m_hwnd))
		return true;

	// REVIEW ShonK: should we filter out kwstShowMax and kwstHideMax?
	// Send bogus WM_SIZE messages to the bars so they know to resize themselves.
	if (m_qrebr)
		::SendMessage(m_qrebr->Hwnd(), WM_SIZE, kwstRestored, 0);
	if (m_qstbr)
		::SendMessage(m_qstbr->Hwnd(), WM_SIZE, kwstRestored, 0);

	return false;
}


/*----------------------------------------------------------------------------------------------
	Handle window notification messages.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(id, pnmh, lnRet))
		return true;

	if (pnmh->code == CBN_DROPDOWN && id == kcidFmttbStyle)
		return OnStyleDropDown(pnmh->hwndFrom);

	switch (pnmh->code)
	{
	case RBN_AUTOSIZE:
		// If the rebar autosized, our client area was resized.
		OnClientSize();
		return false;

	case RBN_CHEVRONPUSHED:
		return OnChevronPushed((NMREBARCHEVRON *)pnmh);

	case TTN_GETDISPINFO:
		{
			// Get the tooltip string for the given control.
			NMTTDISPINFO * pnmtdi = (NMTTDISPINFO *)pnmh;
			StrApp strHelp;
			bool f;
			if (id == kcidEditUndo || id == kcidEditRedo)
			{
				// The tool tips for Undo and Redo use the label from the task list.
				strHelp = UndoRedoText(id == kcidEditRedo);
				f = true;
			}
			else
				f = AfUtil::GetResourceStr(krstHoverEnabled, pnmtdi->hdr.idFrom, strHelp);
			if (f)
			{
				StrApp strAccel;
				GetMenuMgr()->FFindAccelKeyName(pnmtdi->hdr.idFrom, strAccel);
				if (strAccel.Length() > 0)
					strHelp.FormatAppend(_T(" (%s)"), strAccel.Chars());
				_tcscpy_s(pnmtdi->szText, strHelp.Chars());
			}
#ifdef DEBUG
			else
			{
				StrAnsi sta;
				sta.Format("Missing a tooltip for button with ID %d", pnmtdi->hdr.idFrom);
				Warn(sta.Chars());
			}
#endif
			if (AfUtil::GetResourceStr(krstStatusEnabled, pnmtdi->hdr.idFrom, strHelp))
				m_qstbr->StoreHelpText(strHelp.Chars());
			return false;
		}
	}

	if (id == kridTBarFmtg && pnmh->code == TBN_DROPDOWN)
	{
		int cid = ((NMTOOLBAR *)pnmh)->iItem;

		if (cid == kcidFmttbApplyBdr)
		{
			// The border to apply needs to be changed.
			Rect rc;
			SendMessage(pnmh->hwndFrom, TB_GETRECT, cid, (long)&rc);

			Point pt(rc.left, rc.bottom);
			::ClientToScreen(pnmh->hwndFrom, &pt);

			WndCreateStruct wcs;
			wcs.InitChild(_T("STATIC"), m_hwnd, 1);

			IconComboPopupPtr qxpop;
			qxpop.Create();
			HWND hwndFocus = ::GetFocus();
			AfVwRootSite * pavrs = dynamic_cast<AfVwRootSite * >(AfWnd::GetAfWnd(hwndFocus));
			bool rgfPressed[kbpLim];
			if ((!pavrs) || !pavrs->GetFmtBdrPressed(rgfPressed))
			{
				for (int i = 0; i < kbpLim; i++)
					// Another option for the following would be:
					// rgfPressed[i] = (i == m_bpBorderPos);
					rgfPressed[i] = false;
			}

			if (qxpop->DoPopup(wcs, &m_bpBorderPos, pt, kbpLim, kridFmtBorderBtns, kbpLim,
				rgfPressed, m_himlBorderCombo))
			{
				// Recreate the toolbar icon.
				AfToolBar * ptlbr = GetToolBar(kridTBarFmtg);
				ptlbr->UpdateIconImage(kcidFmttbApplyBdr, m_himlBorderCombo, m_bpBorderPos);

				// Pretend like the button was clicked. When I (DarrellZ) tried to use SendMessage
				// here, the message never got trapped for some reason, so that's why I'm using
				// PostMessage.
				::PostMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(cid, 0), (LPARAM)pnmh->hwndFrom);
			}

			lnRet = TBDDRET_DEFAULT;
			return true;
		}

		// Otherwise the foreground or background color needs to be changed.
		Assert(cid == kcidFmttbApplyFgrndColor || cid == kcidFmttbApplyBgrndColor);

		COLORREF * pclr = (cid == kcidFmttbApplyFgrndColor) ? &m_clrFore : &m_clrBack;

		Rect rc;
		SendMessage(pnmh->hwndFrom, TB_GETRECT, cid, (long)&rc);

		Point pt(rc.left, rc.bottom);
		::ClientToScreen(pnmh->hwndFrom, &pt);

		WndCreateStruct wcs;
		wcs.InitChild(_T("STATIC"), m_hwnd, 1);

		UiColorPopupPtr qcop;
		qcop.Create();
		if (qcop->DoPopup(wcs, pclr, pt))
		{
			// Recreate the toolbar icon.
			COLORREF clrNew = *pclr;
			UpdateToolBarIcon(kridTBarFmtg, cid, clrNew);

			// Pretend like the button was clicked. When I (DarrellZ) tried to use SendMessage
			// here, the message never got trapped for some reason, so that's why I'm using
			// PostMessage.
			::PostMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(cid, 0), (LPARAM)pnmh->hwndFrom);
		}

		lnRet = TBDDRET_DEFAULT;
		return true;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Show the context menu for the toolbars.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::OnContextMenu(HWND hwnd, Point pt)
{
	HWND hwndRebar = m_qrebr->Hwnd();
	int ctlbr = m_vqtlbr.Size();
	if (hwnd == hwndRebar || ::IsChild(hwndRebar, hwnd) && ctlbr > 1)
	{
		// Show a popup menu to toggle toolbar states.
		HMENU hmenuPopup = ::CreatePopupMenu();
		::AppendMenu(hmenuPopup, MF_STRING, kcidExpToolbars, NULL);
		::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y, 0, m_hwnd,
			NULL);
		::DestroyMenu(hmenuPopup);
		return true;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Modify the icon for a toolbar icon that shows a color bar along the bottom, or an icon
	from an image list.
	Note: currently it uses the clr passed for colors, but its own known value for the
	border position.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::UpdateToolBarIcon(int widToolBar, int widButton, COLORREF clr)
{
	AfToolBar * ptlbr = GetToolBar(widToolBar);
	if (!ptlbr)
		return;

	if (widButton == kcidFmttbApplyBdr)
	{
		ptlbr->UpdateIconImage(kcidFmttbApplyBdr, m_himlBorderCombo, m_bpBorderPos);
		return;
	}

	if (clr == kclrTransparent)
	{
		if (widButton == kcidFmttbApplyFgrndColor)
			clr = kclrBlack;
		else
			clr = kclrWhite;
	}
	ptlbr->UpdateIconColor(widButton, clr);
}


/*----------------------------------------------------------------------------------------------
	Returns the foreground and background colors. Either parameter can be NULL if the value is
	not needed.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::GetColors(COLORREF * pclrFore, COLORREF * pclrBack)
{
	AssertPtrN(pclrFore);
	AssertPtrN(pclrBack);
	if (pclrFore)
		*pclrFore = m_clrFore;
	if (pclrBack)
		*pclrBack = m_clrBack;
}


/*----------------------------------------------------------------------------------------------
	Display help string in status bar for a menu item.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::OnMenuSelect(int cid, uint grfmf, HMENU hMenu)
{
//	StrApp str;
//	str.Format("AfMainWnd::OnMenuSelect, cid=%d, grfmf=%d, hMenu=%d.\n", cid, grfmf, hMenu);
//	OutputDebugString(str.Chars());

	AssertObj(this);
	::KillTimer(m_hwnd, AfStatusBar::knMenuTimer);

	m_qstbr->StoreHelpText(NULL);
	if (!hMenu && grfmf == 0xFFFF)	// Closing the menu.
	{
		m_qstbr->RestoreStatusText();
		return false;
	}
	// Clear the field.
	if (cid == 0 ||  grfmf & MF_POPUP || grfmf & MF_SYSMENU)
	{
		m_qstbr->RestoreStatusText();
		return false;
	}
	// The default behavior is to display a string in the status bar in the far most left pane.
	StrApp strHelpStr;
	if (cid >= kcidMenuItemDynMin && cid < kcidMenuItemDynLim)
	{
		// Call the handler that will return the expanded menu item text.
		GetMenuMgr()->SaveActiveMenu(hMenu);
		HMENU hmenuT;
		int idDummy;
		GetMenuMgr()->GetLastExpMenuInfo(&hmenuT, &idDummy);
		Assert(hMenu == hmenuT);
//		if (!GetMenuMgr()->GetLastExpMenuInfo(&hmenu, &idDummy))
//		{
//			m_qstbr->RestoreStatusText();
//			return false;
//		}
		CmdPtr qcmd;
		qcmd.Attach(NewObj Cmd);
		qcmd->m_cid = idDummy;
		qcmd->m_rgn[0] = AfMenuMgr::kmaGetStatusText;
		qcmd->m_rgn[1] = cid - kcidMenuItemDynMin;
		qcmd->m_rgn[2] = (int)&strHelpStr;
		qcmd->m_qcmh = m_qwndContext; // Needed for context help.
		if (!AfApp::Papp()->FDispatchCmd(qcmd))
		{
			m_qstbr->RestoreStatusText();
			return false;
		}
	}
	else
	{
		// The string is retrieved from the resource file using the command id from the menu.
		if (!AfUtil::GetResourceStr(krstStatusEnabled, cid, strHelpStr))
		{
			m_qstbr->RestoreStatusText();
			return false;
		}
	}
	m_qstbr->StoreHelpText(strHelpStr.Chars());
	m_qstbr->DisplayHelpText();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Show a popup menu containing all the items that didn't fit on the menu bar.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::OnChevronPushed(NMREBARCHEVRON * pnrc)
{
	AssertPtr(pnrc);

	HWND hwndRebar = pnrc->hdr.hwndFrom;
	REBARBANDINFO rbbi = { isizeof(rbbi), RBBIM_CHILD };
	::SendMessage(hwndRebar, RB_GETBANDINFO, pnrc->uBand, (LPARAM)&rbbi);

	HWND hwndToolbar = rbbi.hwndChild;
	AfMenuBar * pmenu = dynamic_cast<AfMenuBar *>(AfWnd::GetAfWnd(hwndToolbar));
	AssertPtr(pmenu); // This should only be reached from a menu toolbar.

	Rect rcToolbar;
	::GetClientRect(hwndToolbar, &rcToolbar);
	Rect rcBtn;
	Rect rc;
	int cbtn = ::SendMessage(hwndToolbar, TB_BUTTONCOUNT, 0, 0);
	int ibtn;
	for (ibtn = 0; ibtn < cbtn; ibtn++)
	{
		::SendMessage(hwndToolbar, TB_GETITEMRECT, ibtn, (LPARAM)&rcBtn);
		::IntersectRect(&rc, &rcToolbar, &rcBtn);
		if (!::EqualRect(&rc, &rcBtn))
			break;
	}

	Point pt(pnrc->rc.left, pnrc->rc.bottom);
	::ClientToScreen(hwndRebar, &pt);
	pmenu->ShowChevronPopup(ibtn, m_hwnd, pt);

	return true;
}


/*----------------------------------------------------------------------------------------------
	If the click happened over a menu, release the mouse capture and simulate a mouse click,
	which will cause Windows to open the menu that was clicked on.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	// This is not used for applications that have an AfMenuBar for the menu.
	// The AfMenuBar class takes care of What's This help automatically.
	// This doesn't hurt, however, because the AfMenuBar does not return HTMENU
	// when WM_NCHITTEST is sent to it.
	int nT = ::SendMessage(m_hwnd, WM_NCHITTEST, 0, MAKELPARAM(pt.x, pt.y));
	if (nT == HTMENU)
	{
		::ReleaseCapture();
		// This simulates a hardware mouse down, so if the buttons are reversed, we need to
		// simulate a right click instead.
		::mouse_event(
			(GetSystemMetrics(SM_SWAPBUTTON) ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_LEFTDOWN),
			0, 0, 0, 0);
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Update the state of the visible toolbar icons.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::OnIdle()
{
	CmdState cms;
	CmdExec * pcex = AfApp::GetCmdExec();
	AssertPtr(pcex);
	TBBUTTON tbb;

	// Use the same mechanism on the toolbar buttons that is used on menu items.
	int ctlbr = m_vqtlbr.Size();
	for (int itlbr = 0; itlbr < ctlbr; itlbr++)
	{
		AfToolBar * ptlbr = m_vqtlbr[itlbr];
		AssertObj(ptlbr);

		// Skip enabling/disabling if the toolbar is really a menu.
		if (dynamic_cast<AfMenuBar *>(ptlbr) != NULL)
			continue;

		HWND hwndTool = ptlbr->Hwnd();
		if (::IsWindowVisible(hwndTool))
		{
			int cbtn = ::SendMessage(hwndTool, TB_BUTTONCOUNT, 0, 0);
			for (int ibtn = 0; ibtn < cbtn; ibtn++)
			{
				::SendMessage(hwndTool, TB_GETBUTTON, ibtn, (LPARAM)&tbb);
				if (!tbb.idCommand) // Skip separators.
					continue;

				cms.Init(tbb.idCommand, this, NULL, hwndTool);
				pcex->FSetCmdState(cms);
				int grfces = cms.Grfces();

				int nState = tbb.fsState;
				if (grfces & kfcesEnable)
					nState |= TBSTATE_ENABLED;
				else if (grfces & kfcesDisable)
					nState &= ~TBSTATE_ENABLED;
				if (grfces & kfcesCheck || grfces & kfcesBullet)
					nState |= TBSTATE_CHECKED;
				else if (grfces & kfcesUncheck)
					nState &= ~TBSTATE_CHECKED;

				// This triggers a BoundsChecker warning that the command is invalid.
				// However this seems to be a case where we are deliberately passing an invalid
				// command id in the cases where this item is not a combo. Just tell BC to ignore.
				HWND hwndCombo = ::GetDlgItem(hwndTool, tbb.idCommand);
				if (hwndCombo)
				{
					BOOL fEnabled = (nState & TBSTATE_ENABLED) == TBSTATE_ENABLED;
					if (fEnabled || ::IsWindowEnabled(hwndCombo))
						::EnableWindow(hwndCombo, fEnabled);
				}
				else
					::SendMessage(hwndTool, TB_SETSTATE, tbb.idCommand, nState);
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Release smart pointers. This is called from the WM_NCDESTROY message.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::OnReleasePtr()
{
	SuperClass::OnReleasePtr();

	m_qrebr.Clear();
	m_qstbr.Clear();
	for (int itlbr = m_vqtlbr.Size(); --itlbr >= 0; )
		m_vqtlbr[itlbr].Clear();
	m_vqtlbr.Clear();
}

/*----------------------------------------------------------------------------------------------
	This has to be handled so that it doesn't add the command message to the message queue in
	AfWnd. Update the list of styles in the combobox.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::OnStyleDropDown(HWND hctl)
{
	ComBool fCanFmtP = true;
	if (m_prootbActive)
	{
		IVwSelectionPtr qvwsel;
		CheckHr(m_prootbActive->get_Selection(&qvwsel));
		qvwsel->get_CanFormatPara(&fCanFmtP);
	}

	// Get the list of styles from the stylesheet in case it has changed.
	AfToolBarCombo * ptbc = dynamic_cast<AfToolBarCombo *>(AfWnd::GetAfWnd(hctl));
	AssertPtr(ptbc);
	::SendMessage(hctl, CB_RESETCONTENT, 0, 0);
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);

	FW_COMBOBOXEXITEM fcbi;
	memset(&fcbi, 0, isizeof(fcbi));
	fcbi.mask = CBEIF_TEXT;

	if (!GetStylesheet())
		return true; // By default there will be nothing in the combo.
	HvoClsidVec & vhcStyles = GetStylesheet()->GetStyles();
	ISilDataAccessPtr qsda;
	GetStylesheet()->get_DataAccess(&qsda);
	Vector<SmartBstr> vsbstrStyles;
	vsbstrStyles.EnsureSpace(vhcStyles.Size());

	int nType = -1; // Since values for nType begin with 0, initialize nType to -1.
	// Set this flag to true if we find Default Paragraph Chars explicitly.
	bool fGotDefChars = false;
	int cstyles = vhcStyles.Size();

	SmartBstr sbstrName;
	StrUni stuDefParaChars = L""; // Dummy style name for "no character style at all"
	stuDefParaChars.Load(kstidDefParaChars);

	int icbo = -1;
	for (int iStyle = 0; iStyle <  cstyles + 1; iStyle++)
	{
		if (iStyle == cstyles)	// Last time through
		{
			// Insert "Default Paragraph Characters" if not already found
			if (fGotDefChars)
				break;
			SetBstr(&sbstrName, stuDefParaChars);
			nType = kstCharacter; // it's a character style
		}
		else // For each real style
		{
			// Skip this style if we can't use it in the current selection.
			CheckHr(qsda->get_IntProp(vhcStyles[iStyle].hvo, kflidStStyle_Type, & nType));
			if (!fCanFmtP && nType == kstParagraph)
				continue;

			// Get the style name.
			CheckHr(qsda->get_UnicodeProp(vhcStyles[iStyle].hvo, kflidStStyle_Name,
				&sbstrName));
			if (!wcscmp(sbstrName, stuDefParaChars))
				fGotDefChars = true;
		}

		icbo++;

		// Add the type char. to show in the combo box.
		SmartBstr sbstrN;
		if (nType == kstParagraph)
			sbstrN = L"\xb6 ";
		else
			sbstrN = L"\xaa ";
		sbstrN.Append(sbstrName.Chars(),sbstrName.Length());

		qtsf->MakeStringRgch(sbstrN.Chars(), sbstrN.Length(), UserWs(), &fcbi.qtss);
		// Get the position of the new style in the sorted list.
//		for (int iv = 0, ivLim = iStyle; iv < ivLim; )
		int iv, ivLim;
		for (iv = 0, ivLim = icbo; iv < ivLim; )
		{
			int ivMid = (iv + ivLim) / 2;
			if (wcscmp(vsbstrStyles[ivMid].Chars(), sbstrName.Chars()) < 0)
				iv = ivMid + 1;
			else
				ivLim = ivMid;
		}
		fcbi.iItem = iv;
		ptbc->InsertItem(&fcbi);
		vsbstrStyles.Insert(iv, sbstrName);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Switch between What's This help mode and normal mode.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::ToggleHelpMode()
{
	if (s_hhook)
	{
		Assert(s_hhookFilter);
		::UnhookWindowsHookEx(s_hhook);
		::UnhookWindowsHookEx(s_hhookFilter);
		s_hhook = NULL;
		s_hhookFilter = NULL;
		s_fInMenu = false;
		s_mnuLastID = 0;
		s_hmenuCurrent = NULL;
		//::SendMessage(AfApp::Papp()->GetCurMainWnd()->Hwnd(), WM_CANCELMODE, 0, 0);
		// Simulate mouse movement so the cursor gets set correctly.
		// ENHANCE DarrellZ: Figure out how to get the actual cursor for the window the mouse is
		// over when the help mode is cancelled. For now, just show a default arrow.
		::SetCursor(::LoadCursor(NULL, IDC_ARROW));
		// I thought the following line would work, but it doesn't seem to.
		//::mouse_event(MOUSEEVENTF_MOVE, 0, 0, 0, 0);
	}
	else
	{
		DWORD dwThreadId = GetCurrentThreadId();
		s_hhook = ::SetWindowsHookEx(WH_GETMESSAGE, &HelpMsgHook, 0, dwThreadId);
		s_hhookFilter = ::SetWindowsHookEx(WH_MSGFILTER, &HelpMsgFilterHook, 0, dwThreadId);
	}
}


/*----------------------------------------------------------------------------------------------
	A hook procedure while we are in "What's This" help mode. This traps messages before
	they are passed to the window procedure of the destination window.
----------------------------------------------------------------------------------------------*/
LRESULT CALLBACK AfMainWnd::HelpMsgHook(int code, WPARAM wParam, LPARAM lParam)
{
	MSG * pmsg = (MSG *)lParam;
	AssertPtr(pmsg);
	Assert(s_hhook);

	if (code < 0)
		return ::CallNextHookEx(s_hhook, code, wParam, lParam);

	::SetCursor(::LoadCursor(NULL, IDC_HELP));

	// The main message loop calls PeekMessage with the PM_NOREMOVE flag before it calls
	// GetMessage. We want to ignore everything that is not removed from the message loop.
	// Otherwise, we could end up processing everything twice. And in the case where
	// ToggleHelpMode is called, we will miss the message the second time through
	// (from GetMessage) because the hook has already been released.
	if (!(wParam & PM_REMOVE))
	{
		pmsg->message = 0; // Don't pass the message on.
		return 0;
	}

	// If a menu is showing, we don't want to process any messages except for the
	// ones in the line below.
	if (s_fInMenu &&  pmsg->message != WM_LBUTTONDOWN &&
		pmsg->message != WM_KEYDOWN && pmsg->message != WM_LBUTTONUP)
	{
		return ::CallNextHookEx(s_hhook, code, wParam, lParam);
	}

	switch (pmsg->message)
	{
	case WM_MOUSEMOVE:
	case WM_NCMOUSEMOVE:
		{
			// This first dispatches the message to get mouse-over effects such as flat toolbar
			// buttons. Then it sets the cursor to the help cursor in case the window the mouse
			// is over has changed it to something else.
			::DispatchMessage(pmsg); // Pass the message on to be handled.
			::SetCursor(::LoadCursor(NULL, IDC_HELP));
			pmsg->message = 0; // Don't pass the message on.
			return 0;
		}
		break;

	case WM_NCLBUTTONDOWN:
		{
			ToggleHelpMode();
			pmsg->message = 0; // Don't pass the message on.

			int cid = 0;
			switch (pmsg->wParam)
			{
			case HTBOTTOM:
			case HTBOTTOMLEFT:
			case HTBOTTOMRIGHT:
			case HTLEFT:
			case HTRIGHT:
			case HTTOP:
			case HTTOPLEFT:
			case HTTOPRIGHT:
				// REVIEW DarrellZ: should the next one be here?
			case HTGROWBOX:
				cid = kstidSizeBorder; break;
			case HTCAPTION:
				cid = kstidTitleBar; break;
			case HTCLOSE:
				cid = kstidClose; break;
			case HTHELP:
				cid = kstidHelp; break;
			case HTHSCROLL:
				cid = kstidHScroll; break;
			case HTMAXBUTTON:
				cid = kstidMaximize; break;
			case HTMINBUTTON:
				cid = kstidMinimize; break;
			case HTVSCROLL:
				cid = kstidVScroll; break;
			}

			if (cid)
				::PostMessage(pmsg->hwnd, s_wmShowHelp, 0, cid);
			return 0;
		}

	case WM_LBUTTONDOWN:
		{
			pmsg->message = 0; // Don't pass the message on.

			Point pt = MakePoint(pmsg->lParam);
			HWND hwnd = pmsg->hwnd;

			if (s_fInMenu)
			{
				// The user just clicked. At this point we don't really know if they've clicked
				// on a menu item or on another window.
				// See if the user clicked on a menu item or not.
				int iItem = -1;
				if (s_hmenuCurrent)
					iItem = ::MenuItemFromPoint(pmsg->hwnd, s_hmenuCurrent, pt);
				if (iItem != -1)
				{
					// The user clicked on a menu item, so find its ID. The ID for items
					// that open a submenu is -1, so we need to check for that case, and
					// ignore it if they clicked on an item that opens a submenu.
					if (s_mnuLastID > 0)
					{
						// Show the help string for the selected menu item.
						// s_mnuFlag contains a flag (MF_DISABLED) that determines whether
						// or not the selected item is enabled.
						//::PostMessage(pmsg->hwnd, s_wmShowHelp, s_mnuFlag, cid);
						::PostMessage(pmsg->hwnd, s_wmShowHelp, s_mnuFlag, s_mnuLastID);
						ToggleHelpMode();
					}
					return 0;
				}
				else
				{
					// Find the most embedded child window at the current mouse position.
					// This is necessary because pmsg->hwnd points to the window with the
					// menu (AfMainWnd) instead of the actual window that was clicked on.
					while (hwnd)
					{
						Point ptClient(pt);
						::ScreenToClient(hwnd, &ptClient);
						HWND hwndT = ::ChildWindowFromPoint(hwnd, ptClient);
						if (hwndT && hwndT != hwnd)
							hwnd = hwndT;
						else
							break;
					}
					// Fall-through to the non-menu code.
				}
			}
			else
			{
				// If we're not in a menu, pt is in client coordinates, so change
				// it to screen coordinates.
				::ClientToScreen(pmsg->hwnd, &pt);
			}

			// Starting at the window that was clicked on, check the hierarchy up
			// to the top (frame) window to see if there should be any What's This help
			// for that window.
			AfWndPtr qwnd;
			while (hwnd)
			{
				qwnd = AfWnd::GetAfWnd(hwnd);
				if (qwnd)
				{
					AssertObj(qwnd);

					ITsStringPtr qtss;
					if (qwnd->GetHelpStrFromPt(pt, &qtss))
					{
						AfContextHelpWndPtr qchw;
						qchw.Attach(NewObj AfContextHelpWnd);
						qchw->Create(pmsg->hwnd, qtss, pt);
						ToggleHelpMode();
						return 0;
					}
				}
				hwnd = ::GetParent(hwnd);
			}
		}
		break;

	case WM_LBUTTONUP:
		pmsg->message = 0; // Don't pass the message on.
		if (s_fInMenu && s_hmenuCurrent)
		{
			// The user just let go of the mouse button. At this point we don't really
			// know if they've clicked on a menu item or on another window.
			// See if the user clicked on a menu item or not.
			Point pt = MakePoint(pmsg->lParam);
			int iItem = ::MenuItemFromPoint(pmsg->hwnd, s_hmenuCurrent, pt);
			if (iItem != -1)
			{
				// The user clicked on a menu item, so find its ID. The ID for items
				// that open a submenu is -1, so we need to check for that case, and
				// ignore it if they clicked on an item that opens a submenu.
				if (s_mnuLastID > 0)
				{
					// Show the help string for the selected menu item.
					// s_mnuFlag contains a flag (MF_DISABLED) that determines whether
					// or not the selected item is enabled.
					//::PostMessage(pmsg->hwnd, s_wmShowHelp, s_mnuFlag, cid);
					::PostMessage(pmsg->hwnd, s_wmShowHelp, s_mnuFlag, s_mnuLastID);
					ToggleHelpMode();
				}
				return 0;
			}
		}
		break;

	case WM_KEYDOWN:
		if (s_fInMenu)
		{
			// If we're in a menu, hitting the Enter key will show What's This help
			// for the selected menu item. Any other key should be ignored.
			if (s_mnuLastID > 0 && pmsg->wParam == VK_RETURN)
			{
				pmsg->message = 0;
				HWND hwndFrameWnd = AfApp::Papp()->GetCurMainWnd()->Hwnd();
				::PostMessage(hwndFrameWnd, s_wmShowHelp, s_mnuFlag, s_mnuLastID);
				ToggleHelpMode();
				return 0;
			}
		}
		else
		{
			// If we're not in a menu, any key should break out of the help mode.
			ToggleHelpMode();
		}
		break;
	}

	return ::CallNextHookEx(s_hhook, code, wParam, lParam);
}


/*----------------------------------------------------------------------------------------------
	A hook procedure while we are in "What's This" help mode. This traps menu messages before
	they are passed to the window procedure of the destination window.
----------------------------------------------------------------------------------------------*/
LRESULT CALLBACK AfMainWnd::HelpMsgFilterHook(int code, WPARAM wParam, LPARAM lParam)
{
	MSG * pmsg = (MSG *)lParam;
	AssertPtr(pmsg);
	Assert(s_hhookFilter);
	if (code < 0)
		return ::CallNextHookEx(s_hhookFilter, code, wParam, lParam);

	s_fInMenu = true;
	if (pmsg->message == WM_MENUSELECT)
	{
		s_hmenuCurrent = (HMENU)pmsg->lParam;
		s_mnuFlag = 0;
		s_mnuLastID = 0;

		if (!pmsg->lParam && HIWORD(pmsg->wParam) == 0xFFFF)
			s_fInMenu = false; // Menu was closed.
		else if (!(HIWORD(pmsg->wParam) & MF_POPUP))
		{
			// Store menu item information for later.
			s_mnuLastID = LOWORD(pmsg->wParam);
			s_mnuFlag = HIWORD(pmsg->wParam);
		}
	}

	return ::CallNextHookEx(s_hhookFilter, code, wParam, lParam);
}


/*----------------------------------------------------------------------------------------------
	The user has changed Window's settings.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::CmdSettingChange(Cmd * pcmd)
{
	AssertObj(this);
	AssertObj(pcmd);

	switch (pcmd->m_cid)
	{
	case kcidColorChange:
	case kcidSettingChange:
		m_mum.Refresh();
		break;
	case kcidEndColorChange:
	case kcidEndSettingChange:
		m_mum.ResumeRefresh();
		break;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Handle the file / import command.  This must be overridden by the specific application main
	window class.

	@param pcmd Pointer to the menu command.  (not used by this function)

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::CmdFileImport(Cmd * pcmd)
{
	return false;
}

/*----------------------------------------------------------------------------------------------
	Disable the File / Import command.  This must be overridden by the specific application
	main window class.

	@param cms The menu command state.

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::CmsFileImport(CmdState & cms)
{
	cms.Enable(false);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Toggle whether or not a toolbar is visible.

	This method is used for three purposes.
	1)	If pcmd->m_rgn[0] == AfMenuMgr::kmaExpandItem, it is being called to expand the dummy
		item by adding new items.
	2)	If pcmd->m_rgn[0] == AfMenuMgr::kmaGetStatusText, it is being called to get the status
		bar string for an expanded item.
	3)	If pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand, it is being called because the user
		selected an expandable menu item.

	Expanding items:
		pcmd->m_rgn[1] -> Contains the handle to the menu (HMENU) to add items to.
		pcmd->m_rgn[2] -> Contains the index in the menu where you should start inserting items.
		pcmd->m_rgn[3] -> This value must be set to the number of items that you inserted.
	The expanded items will automatically be deleted when the menu is closed. The dummy
	menu item will be deleted for you, so don't do anything with it here.

	Getting the status bar text:
		pcmd->m_rgn[1] -> Contains the index of the expanded/inserted item to get text for.
		pcmd->m_rgn[2] -> Contains a pointer (StrApp *) to the text for the inserted item.
	If the menu item does not have any text to show on the status bar, return false.

	Performing the command:
		pcmd->m_rgn[1] -> Contains the handle of the menu (HMENU) containing the expanded items.
		pcmd->m_rgn[2] -> Contains the index of the expanded/inserted item to get text for.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::CmdTbToggle(Cmd * pcmd)
{
	AssertObj(pcmd);
	Assert(pcmd->m_cid == kcidExpToolbars);

	int ma = pcmd->m_rgn[0];
	if (ma == AfMenuMgr::kmaExpandItem)
	{
		// We need to expand the dummy menu item.
		HMENU hmenu = (HMENU)pcmd->m_rgn[1];
		int imni = pcmd->m_rgn[2];
		int & cmniAdded = pcmd->m_rgn[3];
		cmniAdded = 0;

		StrApp str;
		int ctlbr = m_vqtlbr.Size();
		for (int itlbr = 0; itlbr < ctlbr; itlbr++)
		{
			// Skip menu bars.
			if (dynamic_cast<AfMenuBar *>(m_vqtlbr[itlbr].Ptr()))
				continue;
//			str.Format("%s Toolbar", m_vqtlbr[itlbr]->GetName()); -- no, don't append "Toolbar"
			str = m_vqtlbr[itlbr]->GetName();
			::InsertMenu(hmenu, imni + cmniAdded++, MF_BYPOSITION, kcidMenuItemDynMin + itlbr,
				str.Chars());
		}
		return true;
	}
	else if (ma == AfMenuMgr::kmaGetStatusText)
	{
		// We need to return the text for the expanded menu item.
		//    m_rgn[1] holds the index of the selected item.
		//    m_rgn[2] holds a pointer to the string to set
		int ibar = pcmd->m_rgn[1];
		Assert((uint)ibar < (uint)m_vqtlbr.Size());
		StrApp * pstr = (StrApp *)pcmd->m_rgn[2];
		AssertPtr(pstr);
		StrApp strSH(kstidShowHide);
		StrApp strName(m_vqtlbr[ibar]->GetName());

		// Remove the prefix character in the name if there is one.
		int ichPrefix = strName.FindCh('&');
		if (ichPrefix >= 0)
			strName.Replace(ichPrefix, ichPrefix + 1, "", 0);

		pstr->Format(strSH.Chars(), strName.Chars());
		return true;
	}
	else if (ma == AfMenuMgr::kmaDoCommand)
	{
		int ibar = pcmd->m_rgn[2];
		Assert((uint)ibar < (uint)m_vqtlbr.Size());
		int wid = m_vqtlbr[ibar]->GetWindowId();

		REBARBANDINFO rbbi = { isizeof(rbbi), RBBIM_STYLE };
		int iband = ::SendMessage(m_qrebr->Hwnd(), RB_IDTOINDEX, wid, 0);
		::SendMessage(m_qrebr->Hwnd(), RB_GETBANDINFO, iband, (LPARAM)&rbbi);
		ShowToolBar(wid, rbbi.fStyle & RBBS_HIDDEN);
		return true;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Return the state of whether or not a toolbar is visible.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::CmsTbUpdate(CmdState & cms)
{
	Assert(cms.Cid() == kcidExpToolbars);
	int ibar = cms.GetExpMenuItemIndex();
	Assert((uint)ibar < (uint)m_vqtlbr.Size());

	// Disable if we're in full window mode.
	cms.Enable(!m_fFullWindow);
	if (m_fFullWindow)
	{
		cms.SetCheck(m_dwOldToolbarFlags & (1 << ibar));
	}
	else
	{
		int wid = m_vqtlbr[ibar]->GetWindowId();

		REBARBANDINFO rbbi = { isizeof(rbbi), RBBIM_STYLE };
		int iband = ::SendMessage(m_qrebr->Hwnd(), RB_IDTOINDEX, wid, 0);
		::SendMessage(m_qrebr->Hwnd(), RB_GETBANDINFO, iband, (LPARAM)&rbbi);
		cms.SetCheck(!(rbbi.fStyle & RBBS_HIDDEN));
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Toggle whether or not the status bar is visible.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::CmdSbToggle(Cmd * pcmd)
{
	AssertObj(pcmd);
	AssertPtr(m_qstbr);

	ShowWindow(m_qstbr->Hwnd(), !::IsWindowVisible(m_qstbr->Hwnd()));
	::SendMessage(m_hwnd, WM_SIZE, kwstRestored, 0);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Return the state of whether or not the status bar is visible.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::CmsSbUpdate(CmdState & cms)
{
	AssertPtr(m_qstbr);

	cms.SetCheck(::IsWindowVisible(m_qstbr->Hwnd()));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Bring up the Page Setup dialog, run it, save the results.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::CmdFilePageSetup(Cmd * pcmd)

{
	AssertObj(pcmd);

	FilPgSetDlgPtr qfpsd;
	qfpsd.Create();

	ITsStringPtr qtssTitle;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	qtsf->MakeStringRgch(m_stuHeaderDefault.Chars(), m_stuHeaderDefault.Length(), UserWs(),
		&qtssTitle);

	qfpsd->SetDialogValues(m_dxmpLeftMargin, m_dxmpRightMargin, m_dympTopMargin,
		m_dympBottomMargin, m_dympHeaderMargin, m_dympFooterMargin, m_nOrient,
		m_qtssHeader, m_qtssFooter, m_fHeaderOnFirstPage, m_dympPageHeight,
		m_dxmpPageWidth, m_sPgSize,
		AfApp::Papp()->GetMsrSys(), qtssTitle);

	// Run the dialog.
	if (qfpsd->DoModal(Hwnd()) == kctidOk)
	{
		// Get the output values.
		qfpsd->GetDialogValues(&m_dxmpLeftMargin, &m_dxmpRightMargin, &m_dympTopMargin,
			&m_dympBottomMargin, &m_dympHeaderMargin, &m_dympFooterMargin, &m_nOrient,
			&m_qtssHeader, &m_qtssFooter, &m_fHeaderOnFirstPage, &m_dympPageHeight,
			&m_dxmpPageWidth, &m_sPgSize);
		SavePageSetup();
		// Notify the database that we've made a change and update all our windows.
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(this);
		if (prmw)
		{
			SyncInfo sync(ksyncPageSetup, prmw->GetRootObj(), 0);
			prmw->GetLpInfo()->StoreAndSync(sync);
		}
	}
	return true;
}

void AfMainWnd::GetPageSetupInfo(POrientType * ppot,
	PgSizeType * ppst, int * pdxmpLeftMargin, int * pdxmpRightMargin,
	int * pdympTopMargin, int * pdympBottomMargin,
	int * pdympHeaderMargin, int * pdympFooterMargin,
	int * pdxmpPageWidth, int * pdympPageHeight,
	ITsString ** pptssHeader, ITsString ** pptssFooter, bool * pfHeaderOnFirstPage)
{
	*ppot = m_nOrient;
	*ppst = m_sPgSize;
	*pdxmpLeftMargin = m_dxmpLeftMargin;
	*pdxmpRightMargin = m_dxmpRightMargin;
	*pdympTopMargin = m_dympTopMargin;
	*pdympBottomMargin = m_dympBottomMargin;
	*pdympHeaderMargin = m_dympHeaderMargin;
	*pdympFooterMargin = m_dympFooterMargin;
	*pdxmpPageWidth = m_dxmpPageWidth;
	*pdympPageHeight = m_dympPageHeight;
	*pptssHeader = m_qtssHeader;
	AddRefObj(*pptssHeader);
	*pptssFooter = m_qtssFooter;
	AddRefObj(*pptssFooter);
	*pfHeaderOnFirstPage = m_fHeaderOnFirstPage;
}


/*----------------------------------------------------------------------------------------------
	Get the latest imagelist from the menu and update all the toolbars.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::RefreshToolbars()
{
	HIMAGELIST himl = m_mum.GetImageList();
	HFONT hfont = m_mum.GetMenuFont();
	int ctlbr = m_vqtlbr.Size();
	for (int itlbr = 0; itlbr < ctlbr; itlbr++)
	{
		HWND hwndTool = m_vqtlbr[itlbr]->Hwnd();
		if (dynamic_cast<AfMenuBar *>(m_vqtlbr[itlbr].Ptr()))
			::SendMessage(hwndTool, WM_SETFONT, (WPARAM)hfont, true);
		else
			::SendMessage(hwndTool, TB_SETIMAGELIST, 0, (LPARAM)himl);
	}
}


/*----------------------------------------------------------------------------------------------
	Activate or deactivate the window. Unlike the parallel Windows message, this is NOT called
	if we are just switching between our frame window and some floating toolbar that belongs
	to it. Also, this is called after click actions are processed.
	The default behavior is to correspondingly activate the root box, if it has a range
	selection. If it has an insertion point, it gets activated only if it has focus.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::OnActivate(bool fActivating, HWND hwnd)
{
	// If we're leaving this window and we're currently in the middle of a What's This help
	// loop, cancel it.
	if (s_hhook && !fActivating)
		ToggleHelpMode();

	m_fActiveWindow = fActivating;

	if (!m_prootbActive)
		return;
	try
	{
		IVwSelectionPtr qsel;
		CheckHr(m_prootbActive->get_Selection(&qsel));
		if (!qsel)
			return;
		ComBool fRange;
		CheckHr(qsel->get_IsRange(&fRange));
		if (!fRange)
			return; // don't do anything; IP handled by focus change
		CheckHr(m_prootbActive->Activate(fActivating ? vssEnabled : vssDisabled));
	}
	catch (...)
	{ // Nothing we can do...
	}
}


/*----------------------------------------------------------------------------------------------
	This is called when any menu item is expanded, and allows the app to modify the menu.

	@param hmenu Handle to the menu that is being expanded right now.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::FixMenu(HMENU hmenu)
{
}


/*----------------------------------------------------------------------------------------------
	This creates a copy of pfmnSrc and puts it in *ppfmnDst.

	@param ppfmnDst
	@param pfmnSrc
----------------------------------------------------------------------------------------------*/
void AfMainWnd::_CopyMenuNode(FilterMenuNode ** ppfmnDst, FilterMenuNode * pfmnSrc)
{
	AssertPtr(pfmnSrc);
	AssertPtr(ppfmnDst);
	Assert(!*ppfmnDst);

	FilterMenuNodePtr qfmn;
	qfmn.Create();
	qfmn->m_stuText = pfmnSrc->m_stuText;
	qfmn->m_fmnt = pfmnSrc->m_fmnt;
	qfmn->m_flid = pfmnSrc->m_flid;
	qfmn->m_proptype = pfmnSrc->m_proptype;
	qfmn->m_hvo = pfmnSrc->m_hvo;
	if (pfmnSrc->m_vfmnSubItems.Size())
		_CopyMenuNodeVector(qfmn->m_vfmnSubItems, pfmnSrc->m_vfmnSubItems);
	*ppfmnDst = qfmn.Detach();
}


/*----------------------------------------------------------------------------------------------
	This creates a copy of vfmnSrc and puts it in vfmnDst.

	@param vfmnDst
	@param vfmnSrc
----------------------------------------------------------------------------------------------*/
void AfMainWnd::_CopyMenuNodeVector(FilterMenuNodeVec & vfmnDst, FilterMenuNodeVec & vfmnSrc)
{
	FilterMenuNodePtr qfmn;
	int cfmn = vfmnSrc.Size();
	for (int ifmn = 0; ifmn < cfmn; ifmn++)
	{
		_CopyMenuNode(&qfmn, vfmnSrc[ifmn]);
		vfmnDst.Push(qfmn);
	}
}


/*----------------------------------------------------------------------------------------------
	This recursively assigns default field types to any nodes that do not already have assigned
	field types in the hierarchical list of filter menu nodes.

	@param pmdc
	@param pfmn
----------------------------------------------------------------------------------------------*/
void AfMainWnd::_AssignFieldTypes(IFwMetaDataCache * pmdc, FilterMenuNode * pfmn)
{
	AssertPtr(pmdc);
	AssertPtr(pfmn);

	if (pfmn->m_fmnt == kfmntLeaf)
	{
		if (pfmn->m_proptype == 0 && pfmn->m_flid != 0)	// separator line has both set to zero
			CheckHr(pmdc->GetFieldType(pfmn->m_flid, &pfmn->m_proptype));
	}
	else
	{
		// Recurse into the subitems.
		int cfmn = pfmn->m_vfmnSubItems.Size();
		for (int ifmn = 0; ifmn < cfmn; ifmn++)
			_AssignFieldTypes(pmdc, pfmn->m_vfmnSubItems[ifmn]);
	}
}


/*----------------------------------------------------------------------------------------------
	This creates a copy of psmnSrc and puts it in *ppsmnDst.

	@param ppsmnDst
	@param psmnSrc
----------------------------------------------------------------------------------------------*/
void AfMainWnd::_CopyMenuNode(SortMenuNode ** ppsmnDst, SortMenuNode * psmnSrc)
{
	AssertPtr(psmnSrc);
	AssertPtr(ppsmnDst);
	Assert(!*ppsmnDst);

	SortMenuNodePtr qsmn;
	qsmn.Create();
	qsmn->m_stuText = psmnSrc->m_stuText;
	qsmn->m_smnt = psmnSrc->m_smnt;
	qsmn->m_flid = psmnSrc->m_flid;
	qsmn->m_proptype = psmnSrc->m_proptype;
	qsmn->m_hvo = psmnSrc->m_hvo;
	if (psmnSrc->m_vsmnSubItems.Size())
		_CopyMenuNodeVector(qsmn->m_vsmnSubItems, psmnSrc->m_vsmnSubItems);
	*ppsmnDst = qsmn.Detach();
}


/*----------------------------------------------------------------------------------------------
	This creates a copy of vsmnSrc and puts it in vsmnDst.

	@param vsmnDst
	@param vsmnSrc
----------------------------------------------------------------------------------------------*/
void AfMainWnd::_CopyMenuNodeVector(SortMenuNodeVec & vsmnDst, SortMenuNodeVec & vsmnSrc)
{
	SortMenuNodePtr qsmn;
	int csmn = vsmnSrc.Size();
	for (int ismn = 0; ismn < csmn; ismn++)
	{
		_CopyMenuNode(&qsmn, vsmnSrc[ismn]);
		vsmnDst.Push(qsmn);
	}
}


/*----------------------------------------------------------------------------------------------
	This recursively assigns default field types to any nodes that do not already have assigned
	field types in the hierarchical list of sort menu nodes.

	@param pmdc
	@param psmn
----------------------------------------------------------------------------------------------*/
void AfMainWnd::_AssignFieldTypes(IFwMetaDataCache * pmdc, SortMenuNode * psmn)
{
	AssertPtr(pmdc);
	AssertPtr(psmn);

	if (psmn->m_smnt == ksmntLeaf)
	{
		ULONG luFlid = psmn->m_flid;
		if (psmn->m_proptype == 0 && luFlid != 0)
			CheckHr(pmdc->GetFieldType(luFlid, &psmn->m_proptype));
	}
	else
	{
		// Recurse into the subitems.
		int csmn = psmn->m_vsmnSubItems.Size();
		for (int ismn = 0; ismn < csmn; ismn++)
			_AssignFieldTypes(pmdc, psmn->m_vsmnSubItems[ismn]);
	}
}

/*----------------------------------------------------------------------------------------------
	If fFullWindow is true, temporarily hide the toolbars.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::SetWindowMode(bool fFullWindow)
{
	m_fFullWindow = fFullWindow;

	HWND hwndRebar = m_qrebr->Hwnd();
	int cband = m_vqtlbr.Size();
	REBARBANDINFO rbbi = { isizeof(rbbi), RBBIM_CHILD | RBBIM_STYLE };

	::SendMessage(m_hwnd, WM_SETREDRAW, false, 0);
	if (fFullWindow)
	{
		// Store the current state of the toolbars.
		m_dwOldToolbarFlags = 0;
		for (int iband = 0; iband < cband; iband++)
		{
			::SendMessage(hwndRebar, RB_GETBANDINFO, iband, (LPARAM)&rbbi);
			int itlbr;
			for (itlbr = 0; itlbr < cband; itlbr++)
			{
				AssertPtr(m_vqtlbr[itlbr]);
				if (m_vqtlbr[itlbr]->Hwnd() == rbbi.hwndChild)
					break;
			}
			Assert(itlbr < cband);
			if (dynamic_cast<AfMenuBar *>(((AfToolBarPtr)m_vqtlbr[itlbr]).Ptr()) == NULL)
			{
				if (!(rbbi.fStyle & RBBS_HIDDEN))
				{
					m_dwOldToolbarFlags |= (1 << itlbr);
					// Hide the toolbar.
					rbbi.fStyle |= RBBS_HIDDEN;
					::SendMessage(hwndRebar, RB_SETBANDINFO, iband, (LPARAM)&rbbi);
				}
			}
			else
			{
				m_dwOldToolbarFlags |= (1 << itlbr);
			}
		}
	}
	else
	{
		// Restore the old state of the toolbars.
		for (int iband = 0; iband < cband; iband++)
		{
			::SendMessage(hwndRebar, RB_GETBANDINFO, iband, (LPARAM)&rbbi);
			int itlbr;
			for (itlbr = 0; itlbr < cband; itlbr++)
			{
				AssertPtr(m_vqtlbr[itlbr]);
				if (m_vqtlbr[itlbr]->Hwnd() == rbbi.hwndChild)
					break;
			}
			Assert(itlbr < cband);
			if (m_dwOldToolbarFlags & (1 << itlbr))
				rbbi.fStyle &= ~RBBS_HIDDEN;
			else
				rbbi.fStyle |= RBBS_HIDDEN;
			::SendMessage(hwndRebar, RB_SETBANDINFO, iband, (LPARAM)&rbbi);
		}
	}
	::SendMessage(m_hwnd, WM_SETREDRAW, true, 0);

	Rect rc;
	GetClientRect(rc);
	OnSize(kwstRestored, rc.Width(), rc.Height());
	::RedrawWindow(m_hwnd, NULL, NULL, RDW_INVALIDATE | RDW_ERASE | RDW_UPDATENOW | RDW_ALLCHILDREN);
}

/*----------------------------------------------------------------------------------------------
	Return the Undo or Redo text that should be put in the Edit menu and used for the
	button tooltip.
----------------------------------------------------------------------------------------------*/
StrApp AfMainWnd::UndoRedoText(bool fRedo)
{
	StrApp strUndo = L"";
	IActionHandlerPtr qacth = GetActionHandler();
	if (qacth)
	{
		SmartBstr sbstr;
		if (fRedo)
			qacth->GetRedoText(&sbstr);
		else
			qacth->GetUndoText(&sbstr);
		StrUni stu(sbstr.Chars());
		strUndo = stu;
	}
	if (strUndo.Length() == 0)
	{
		StrUni stuUndo, stuRedo;
		StrUtil::MakeUndoRedoLabels(kstidUndoUnknown, &stuUndo, &stuRedo);
		strUndo = (fRedo) ? stuRedo : stuUndo;
	}
	return strUndo;
}

/*----------------------------------------------------------------------------------------------
	Process External Link menu items (Open, Open With, External Link, Remove Link).

	@param pcmd Ptr to menu command

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::CmdExternalLink(Cmd * pcmd)
{
	AssertObj(pcmd);
	return ExternalLink(pcmd, NULL);
}


/*----------------------------------------------------------------------------------------------
	Process External Link menu items (Open, Open With, External Link, Remove Link).

	@param pcmd Pointer to menu command
	@param plpi Pointer to the AfLpInfo for the language project

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::ExternalLink(Cmd * pcmd, AfLpInfo * plpi)
{
	if (!m_prootbActive)
		return false;

	ISilDataAccessPtr qsda;
	if (plpi)
	{
		CustViewDaPtr qda;
		plpi->GetDataAccess(&qda);
		qsda = dynamic_cast<ISilDataAccess *>(qda.Ptr());
	}
	else
		m_prootbActive->get_DataAccess(&qsda);

	IVwRootSitePtr qvrs;
	CheckHr(m_prootbActive->get_Site(&qvrs));
	AfVwRootSite * pvwnd = dynamic_cast<AfVwRootSite *>(qvrs.Ptr());
	if (!pvwnd)
		return false;

	bool fFoundLinkStyle;
	IVwSelectionPtr qvwsel;
	StrAppBuf strbFile;

	if (!pvwnd->GetExternalLinkSel(fFoundLinkStyle, &qvwsel, strbFile))
	{
		StrApp strMsg(kstidNoExtLink);
		StrApp strTitle(kstidExtLinkTitle);
		::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(), MB_OK | MB_ICONEXCLAMATION);
		return false; // Invalid selection or other problem.
	}

	if (fFoundLinkStyle)
		CheckHr(qvwsel->Install());
	else if (!qvwsel)
		CheckHr(m_prootbActive->get_Selection(&qvwsel));
	if (!qvwsel)
		return false;

	if (plpi) plpi->MapExternalLink(strbFile);

	ITsStringPtr qtssLink;

	switch (pcmd->m_cid)
	{
	case kcidExtLink:
		{
			bool fSetFromFileOnly = false;
			// Default to the External Link root directory if there isn't a filename.
			StrAppBuf strbInitDir;

			// File name without directory path:
			StrAppBuf strbFileOnly;  // must be declared before ofn since ofn may hold a pointer to this.

			// Get the filename from the user.
			OPENFILENAME ofn = { OPENFILENAME_SIZE_VERSION_400 };
			ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_HIDEREADONLY;
			// The constant below is required for compatibility with Win 95/98 (and maybe NT4)
			ofn.lStructSize = OPENFILENAME_SIZE_VERSION_400;
			ofn.hwndOwner = m_hwnd;
			ofn.lpstrFilter = _T("All Files (*.*)\0*.*\0\0");
			ofn.lpstrTitle = _T("Create External Link");
			ofn.lpstrFile = const_cast<achar *>(strbFile.Chars());
			ofn.nMaxFile = kcchMaxBufDef;
			if (plpi && (strbFile.Length() == 0))
			{
				// Get the initial directory to start from. This should be the external
				// link directory on the machine the database is on.
				strbInitDir = plpi->GetExtLinkRoot();
				plpi->MapExternalLink(strbInitDir);
				ofn.lpstrInitialDir = strbInitDir.Chars();
			}
			else if (plpi && (strbFile.Length() > 0))
			{
				// See if the current path name is still valid. If not, we will truncate it to
				// make the "nearest" folder available:
				WIN32_FIND_DATA wfd;
				HANDLE hFind = ::FindFirstFile(strbFile.Chars(), &wfd);
				if (hFind != INVALID_HANDLE_VALUE)
					::FindClose(hFind);
				else
				{
					// Current path does not exist. Find the longest sub-path which does exist:
					int iMarker = strbFile.Length();
					bool fFoundValid = false;
					while (!fFoundValid)
					{
						StrAppBuf strbSlash("\\");
						iMarker = strbFile.ReverseFindCh(strbSlash[0], iMarker);
						if (iMarker <= 0)
							break;

						if (!strbFileOnly.Length())
						{
							strbFileOnly =
								strbFile.Right(strbFile.Length() - iMarker - 1).Chars();
						}

						StrApp strAttempt = strbFile.Left(iMarker).Chars();
						DWORD nFlags = GetFileAttributes(strAttempt.Chars());
						if (nFlags != -1 && (nFlags & FILE_ATTRIBUTE_DIRECTORY))
						{
							fFoundValid = true;
							break;
						}
						iMarker--;
					}
					if (fFoundValid)
						strbInitDir = strbFile.Left(iMarker);
					else
					{
						// We have a broken link that is way off course!
						// Get the initial directory to start from. This should be the external
						// link directory on the machine the database is on.
						strbInitDir = plpi->GetExtLinkRoot();
					}
					plpi->MapExternalLink(strbInitDir);
					ofn.lpstrInitialDir = strbInitDir.Chars();
					ofn.lpstrFile = const_cast<achar *>(strbFileOnly.Chars());
					fSetFromFileOnly = true;
				}
			}
			if (IDOK != GetOpenFileName(&ofn))
				return false;

			// At this point, ofn.lpstrFile is either pointing to strbFile or strbFileOnly
			// AND the string within strbFile or strbFileOnly could have changed by the
			// GetOpenFileName() method call.
			// We need to set the length here to the length of the file the user chose.
			// ofn.lpstrFile is used instead of strbFile.Chars() because the Chars method
			// asserts if the length of the string has changed.
			if (fSetFromFileOnly)
				strbFile = ofn.lpstrFile;  // ofn.lpstrFile was pointing to strbFileOnly
			strbFile.SetLength(StrLen(ofn.lpstrFile));

			bool fLocal = true;  // assume local for WorldPad

			if (plpi)
			{
				// Allow local links if we are looking at a local database.
				AfDbApp * papp = dynamic_cast<AfDbApp *>(AfApp::Papp());
				StrUni stuLocalServer(papp->GetLocalServer());

				AfDbInfo * pdbi = plpi->GetDbInfo();
				StrUni stuServerName(pdbi->ServerName());
				fLocal = stuServerName.EqualsCI(stuLocalServer); // Is the db on the local machine ?
			}

			if (! fLocal)
			{
				// The db is not on the local machine

				// Is the selected file on the local machine?
				int ich = strbFile.FindCh('\\');
				if (ich != 0)
				{
					// The selected file IS on the local machine.
					// Tell the user a remote file is needed required.
					StrApp strTitle(kstidHLInvalidTitle);
					StrApp strPrompt(kstidHLInvalidMsg);

					// enable display of a help page from a non-dialog context
					StrApp strHelpUrl(AfApp::Papp()->GetHelpFile());   // path
					strHelpUrl.Append(_T("::/"));
					StrApp strPath2 = _T("DialogInvalidExternalLink.htm"); // filename
					strHelpUrl.Append(strPath2);
					AfMainWndPtr qafwTop = AfApp::Papp()->GetCurMainWnd();
					qafwTop->SetFullHelpUrl(strHelpUrl.Chars());

					::MessageBox(m_hwnd, strPrompt.Chars(), strTitle.Chars(), MB_OK | MB_HELP);

					qafwTop->ClearFullHelpUrl();

					return false;
				}
				else
				{
					// The selected file IS on a remote machine.
					// Modify the remote computer filespec to a local computer file spec
					plpi->UnmapExternalLink(strbFile);
				}
			}

			StrUni stuFile(strbFile.Chars());

			MakeExternalLinkAtSel(qvwsel, stuFile, qsda, pvwnd);
		}
		return true;
	case kcidExtLinkUrl:
		{
			IDataObjectPtr qdobj;
			CheckHr(::OleGetClipboard(&qdobj));
			FORMATETC format;
			STGMEDIUM medium;
			// For now we require that the clipboard can supply a Unicode string.
			// May eventually find reason to support other formats (such as 8-bit string).
			format.cfFormat = CF_UNICODETEXT;
			format.ptd = NULL;
			format.dwAspect = DVASPECT_CONTENT;
			format.lindex = -1;
			format.tymed = TYMED_HGLOBAL;
			StrUni stuClip;
			HRESULT hr;
			CheckHr(hr = qdobj->GetData(&format, &medium));
			if (hr == S_OK)
			{
				if (medium.tymed == TYMED_HGLOBAL && medium.hGlobal)
				{
					// Convert the global memory string to a TsString without any formatting.
					const wchar * pwszClip = (const wchar *)::GlobalLock(medium.hGlobal);
					stuClip = pwszClip;
					::GlobalUnlock(medium.hGlobal);
				}
				ReleaseStgMedium(&medium);
			}
			else
				return false; // Can't do it if nothing in clipboard; warn user?
			if (!::UrlIs(stuClip.Chars(), URLIS_URL))
			{
				// Enhance: disable menu item or give error message here.
				return false;
			}
			// stuClip should now contain the URL.
			// Enhance: should we validate it somehow?
			MakeExternalLinkAtSel(qvwsel, stuClip, qsda, pvwnd);
		}
		return true;

	case kcidExtLinkOpen:
		AfApp::LaunchHL(NULL, _T("open"), strbFile.Chars(), NULL, NULL, SW_SHOWNORMAL);
		break;

	case kcidExtLinkOpenWith:
		{
			StrApp strParam;
			strParam.Format(_T("shell32.dll,OpenAs_RunDLL %s"), strbFile.Chars());
			AfApp::LaunchHL(NULL, _T("open"), _T("rundll32.exe"), strParam.Chars(), NULL,
				SW_SHOWNORMAL);
		}
		break;

	case kcidExtLinkRemove:
		{
			StrApp strTitle(kstidExtLinkTitle);
			StrApp strPrompt(kstidExtLinkRemovePrompt);
			int nRes = ::MessageBox(m_hwnd, strPrompt.Chars(), strTitle.Chars(),
				MB_ICONQUESTION | MB_YESNO);
			if (nRes == IDYES)
			{
				// Remove the ExternalLink style and the ObjData property from the string.
				int cch;
				ITsStrBldrPtr qtsb;
				SmartBstr sbstr = L"";
				CheckHr(qvwsel->GetSelectionString(&qtssLink, sbstr));
				CheckHr(qtssLink->get_Length(&cch));
				CheckHr(qtssLink->GetBldr(&qtsb));
				CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle, NULL));
				CheckHr(qtsb->SetStrPropValue(0, cch, ktptObjData, NULL));
				CheckHr(qtsb->GetString(&qtssLink));

				StrUni stuUndo, stuRedo;
				StrUtil::MakeUndoRedoLabels(kstidUndoExtLink, &stuUndo, &stuRedo);
				CheckHr(qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
				//	Record an action that will handle replacing the selection on undo.
				pvwnd->SetupUndoSelection(qsda, true);

				// Replace the selection with the new string.
				CheckHr(qvwsel->ReplaceWithTsString(qtssLink));

				pvwnd->SetupUndoSelection(qsda, false);
				CheckHr(qsda->EndUndoTask());

				return true;
			}
		}
		return true;

	default:
		return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Insert an external link at the given selection. The 'file' may also be a URL.
----------------------------------------------------------------------------------------------*/
void AfMainWnd::MakeExternalLinkAtSel(IVwSelection * pvwsel, StrUni stuFile,
	ISilDataAccess * psda, AfVwRootSite * pvwnd)
{
	ITsStringPtr qtssLink;
	// If we have a range selection, use the selection text as the external link text, otherwise
	// use the filename.
	ComBool fRange;
	CheckHr(pvwsel->get_IsRange(&fRange));
	if (fRange)
	{
		SmartBstr sbstr = L"";
		CheckHr(pvwsel->GetSelectionString(&qtssLink, sbstr));
	}
	else
	{
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		CheckHr(qtsf->MakeString(stuFile.Bstr(), UserWs(), &qtssLink));
	}

	// Add the ExternalLink style to the string.
	int cch;
	ITsStrBldrPtr qtsb;
	StrUni stuStyle(L"External Link");
	StrUni stuPropValue;
	stuPropValue.Format(L"%c%s", kodtExternalPathName, stuFile.Chars());
	CheckHr(qtssLink->get_Length(&cch));
	CheckHr(qtssLink->GetBldr(&qtsb));
	CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle, stuStyle.Bstr()));
	CheckHr(qtsb->SetStrPropValue(0, cch, ktptObjData, stuPropValue.Bstr()));
	CheckHr(qtsb->GetString(&qtssLink));

	StrUni stuUndo, stuRedo;
	StrUtil::MakeUndoRedoLabels(kstidUndoExtLink, &stuUndo, &stuRedo);
	CheckHr(psda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
	//	Record an action that will handle replacing the selection on undo.
	pvwnd->SetupUndoSelection(psda, true);

	IVwRootBoxPtr qrootb; // get this before we possibly invalidate selection.
	CheckHr(pvwnd->get_RootBox(&qrootb));

	// Replace the selection with the new string.
	CheckHr(pvwsel->ReplaceWithTsString(qtssLink));

	pvwnd->SetupUndoSelection(psda, false);

	// Arrange that immediate further typing won't extend link.

	CheckHr(psda->EndUndoTask());
	IVwSelectionPtr qsel;
	CheckHr(qrootb->get_Selection(&qsel)); // may just possibly have changed.
	if (!qsel)
		return;
	ITsPropsBldrPtr qtpb;
	ITsTextPropsPtr qttp;
	CheckHr(qtssLink->get_PropertiesAt(0, &qttp));
	CheckHr(qttp->GetBldr(&qtpb));
	CheckHr(qtpb->SetStrPropValue(ktptObjData, NULL));
	CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, NULL));
	CheckHr(qtpb->GetTextProps(&qttp));
	CheckHr(qsel->SetIpTypingProps(qttp));
}

/*----------------------------------------------------------------------------------------------
	Enable the Insert Link menu item(s).
----------------------------------------------------------------------------------------------*/
bool AfMainWnd::CmsInsertLink(CmdState & cms)
{
	IVwSelectionPtr qsel;
	if (m_prootbActive)
	{
		IVwRootSitePtr qvrs;
		m_prootbActive->get_Site(&qvrs);
		AfVwRootSitePtr qavrs = dynamic_cast<AfVwRootSite *>(qvrs.Ptr());
		if (qavrs && !qavrs->SelectionInOneField())
		{
			cms.Enable(false);
			return true;
		}
		CheckHr(m_prootbActive->get_Selection(&qsel));
	}
	if (qsel)
	{
		ComBool fCanFormat;
		CheckHr(qsel->get_CanFormatChar(&fCanFormat));
		cms.Enable((bool)fCanFormat);
	}
	else
		cms.Enable(false);

	return true;
}

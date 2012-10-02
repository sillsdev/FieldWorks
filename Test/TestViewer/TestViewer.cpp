/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: TestViewer.cpp
Responsibility: Luke Ulrich
Last reviewed: nunca

Description:
	This class provides the base for TestViewer functions.
----------------------------------------------------------------------------------------------*/
#include "main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

// Create one global instance. It has to exist before WinMain is called.
static WpApp g_app;

const int kdxpMin = 300; // Minimum width for window.
const int kdypMin = 200; // Minimum height for window.

const int knStatusTimer = 5;
const char * kpszMainSettingsKey = "Settings";

BEGIN_CMD_MAP(WpApp)
//	ON_CID_ALL(kcidFileOpen, &WpApp::CmdFileOpenTest, NULL)
//	ON_CID_ALL(kcidFileSave, &WpApp::CmdFileDone, NULL)
//	ON_CID_ALL(kcidFileSaveAs, &WpApp::CmdFileSaveAs, NULL)
	ON_CID_ALL(kcidFileExit, &WpApp::CmdFileExit, NULL)
//	ON_CID_ALL(kcidHelpAbout, &WpApp::CmdHelpAbout, NULL)
//	ON_CID_ALL(kcidWndCascad, &WpApp::CmdWndCascade, NULL)
//	ON_CID_ALL(kcidWndTile, &WpApp::CmdWndTileHoriz, NULL)
//	ON_CID_ALL(kcidWndSideBy, &WpApp::CmdWndTileVert, NULL)
#if 0 // ifdef DEBUG
//	ON_CID_ALL(kcidDbgOpnPrj, &WpApp::CmdDbgOpnPrj, NULL)
//	ON_CID_ALL(kcidPossChsr, &WpApp::CmdPossChsr, NULL)
#endif
END_CMD_MAP_NIL()


/*----------------------------------------------------------------------------------------------
	The Research Notebook command map for the main window.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(WpMainWnd)
//	ON_CID_GEN(kcidViewTBarsStd, &WpMainWnd::CmdTbToggle, &WpMainWnd::CmsTbUpdate)
//	ON_CID_GEN(kcidViewTBarsFmtg, &WpMainWnd::CmdTbToggle, &WpMainWnd::CmsTbUpdate)
//	ON_CID_GEN(kcidViewTBarsIns, &WpMainWnd::CmdTbToggle, &WpMainWnd::CmsTbUpdate)
//	ON_CID_GEN(kcidViewTBarsWnd, &WpMainWnd::CmdTbToggle, &WpMainWnd::CmsTbUpdate)
//	ON_CID_GEN(kcidViewStatBar, &WpMainWnd::CmdSbToggle, &WpMainWnd::CmsSbUpdate)
//	ON_CID_ME(kcidFilePageSetup, &WpMainWnd::CmdFilePageSetup, NULL)
//	ON_CID_ME(kcidToolsOpts, &WpMainWnd::CmdToolsOpts, NULL)
//	ON_CID_ME(kcidFmtStyles, &WpMainWnd::CmdFmtStyles, NULL)
//	ON_CID_ME(kcidHelpWhatsThis, &WpMainWnd::CmdHelpMode, NULL)
//	ON_CID_ME(kcidHelpConts, &WpMainWnd::CmdHelpContents, NULL)
//	ON_CID_ME(kcidWndNew, &WpMainWnd::CmdWndNewTest, NULL)
	ON_CID_ME(kcidScript, &WpMainWnd::CmdWndFileScript, &WpMainWnd::ScriptEnable)
END_CMD_MAP_NIL()

/*----------------------------------------------------------------------------------------------
	Constructor for WpApp.
----------------------------------------------------------------------------------------------*/
WpApp::WpApp()
{
	s_fws.SetRoot("TestViewer");
}

/*----------------------------------------------------------------------------------------------
	Initialize the application.
----------------------------------------------------------------------------------------------*/
void WpApp::Init(void)
{
	AfApp::Init();
	AfWnd::RegisterClass("WpMainWnd", 0, 0, 0, COLOR_3DFACE, (int)kridTestViewerIcon);
	WndCreateStruct wcs;
	wcs.InitMain("WpMainWnd");

	// Commented lines below are specific to graphite content
	// not necessary for this program
/*	Vector<StrApp> vstr;
	bool f = GrUtil::GetAllGraphiteFonts(vstr);
	for (int i = 0; i < vstr.Size(); i++)
	{
		StrApp strTmp = vstr[i];
	}
	Assert(f);
	//	For now, just take the first registered font.
	OLECHAR * rgchFontName;
	StrUni stuFirstFont;
	if (vstr.Size() == 0)
		rgchFontName = L"Arial";
	else
	{
		stuFirstFont = vstr[0];
		rgchFontName = const_cast<OLECHAR *>(stuFirstFont.Chars());
	}

	//	Create the encoding, writing system, and renderer.

	ILgEncodingPtr qenc;
	qenc.CreateInstance(CLSID_LgEncoding);
	qenc->put_Encoding(100);	// TODO: come up with a better number.
	ILgWritingSystemPtr qws;
	qws.CreateInstance(CLSID_LgWritingSystem);
	CheckHr(qenc->SetDefaultWs(qws));

	ILgEncodingFactoryPtr qencf;
	qencf.CreateInstance(CLSID_LgEncodingFactory);
	CheckHr(qencf->AddEngine(qenc));

	//	Create a Graphite engine.
	IRenderEnginePtr qgreng;
	qgreng.CreateInstance(CLSID_GrEngine);
	CheckHr(qws->SetRenderer(qgreng));

	ISimpleInit * pInit;
	CheckHr(qgreng->QueryInterface(IID_ISimpleInit, (void **)&pInit));
	CheckHr(pInit->InitNew((BYTE *)rgchFontName, wcslen(rgchFontName) * 2));
	pInit->Release();

	//	Determine script direction.
	int grfsdc;
	bool fRtl;
	CheckHr(qgreng->GetScriptDirection(&grfsdc));
	if (grfsdc & kfsdcHorizLtr != 0)
		fRtl = false;
	else if (grfsdc & kfsdcHorizRtl != 0)
		fRtl = true;
	else
		fRtl = false;
	CheckHr(qws->put_RightToLeft(fRtl));

	//	Turn on the transduction log
	ITraceControl * pTraceCtrl;
	CheckHr(qgreng->QueryInterface(IID_ITraceControl, (void **)&pTraceCtrl));
	CheckHr(pTraceCtrl->SetTracing(1));
	pTraceCtrl->Release();*/

	WpMainWndPtr qwnd;
	qwnd.Create();
	qwnd->CreateHwnd(wcs);

	// Size and position main window appropiately
	MoveWindow(qwnd->Hwnd(), 350, 100, 600, 350, true);

	qwnd->Show(m_nShow);
	AddWindow(qwnd);
	::SendMessage(qwnd->Hwnd(), WM_SETTEXT, 0, (LPARAM)"TestViewer");
}

/*----------------------------------------------------------------------------------------------
	Pass the idle message on to the main window.
----------------------------------------------------------------------------------------------*/
void WpApp::OnIdle()
{
	// REVIEW DarrellZ: Does this need to call OnIdle for all the top-level windows
	// or just for the one that has the focus?
	AssertObj(m_qwndCur);

	m_qwndCur->OnIdle();
}


/*----------------------------------------------------------------------------------------------
	Handle the File/Open command.
----------------------------------------------------------------------------------------------*/
bool WpApp::CmdFileOpenTest(Cmd * pcmd)
{
/*
	AfFrameWnd * pwnd = Papp()->GetCurMainWnd();
	WpMainWnd * pwpwnd = dynamic_cast<WpMainWnd *>(pwnd);
	WpSplitWnd * pwsw = pwpwnd->SplitWnd();
	WpChildWnd * pwcw = pwsw->ChildWnd();
*/
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the Test-Done command.
----------------------------------------------------------------------------------------------*/
bool WpApp::CmdFileDone(Cmd * pcmd)
{
/*	AfFrameWnd * pwnd = Papp()->GetCurMainWnd();
	WpMainWnd * pwpwnd = dynamic_cast<WpMainWnd *>(pwnd);
	WpSplitWnd * pwsw = pwpwnd->SplitWnd();
	WpChildWnd * pwcw = pwsw->ChildWnd();*/

	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the File-Save As command.
----------------------------------------------------------------------------------------------*/
bool WpApp::CmdFileSaveAs(Cmd * pcmd)
{
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the exit command.
----------------------------------------------------------------------------------------------*/
bool WpApp::CmdFileExit(Cmd * pcmd)
{
#if 0
	// Todo DarrelZ(JohnT): Somehow we need to get the main windows to close properly.
	// They should have a chance to check whether we need to save, and their OnReleasePtr
	// methods need to be called.

	while (AfApp::Papp()->GetMainWndCount())
		::PostMessage(AfApp::Papp()->GetCurMainWnd()->Hwnd(), WM_CLOSE, 0, 0);
#endif

	Quit();
	return true;
}

/*----------------------------------------------------------------------------------------------
	CleanUp the application.
----------------------------------------------------------------------------------------------*/
void WpApp::CleanUp(void)
{
	// Currently nothing to do
	// Todo DarrelZ (JohnT): This should probably go into AfApp...
	m_qwndCur.Clear();
	// NOTE: This has to go backwards, because the window will remove itself from this
	// vector when it handles the clse message.
	for (int iwnd = m_vqwnd.Size(); --iwnd >= 0; )
		::SendMessage(m_vqwnd[iwnd]->Hwnd(), WM_CLOSE, 0, 0);
}

/***********************************************************************************************
	WpMainWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
WpMainWnd::WpMainWnd()
{
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
WpMainWnd::~WpMainWnd()
{
}

/*----------------------------------------------------------------------------------------------
	Load settings specific to this window.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::LoadSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPsz(pszRoot);

	SuperClass::LoadSettings(pszRoot, fRecursive);

	// Get window position.
	LoadWindowPosition(pszRoot, "Position");

	FwSettings * pfws = AfApp::GetSettings();

	// Read the toolbar settings from storage. If the settings aren't there, use
	// default settings.
	DWORD dwToolbarFlags;
	if (!pfws->GetDword(pszRoot, "Toolbar Flags", &dwToolbarFlags))
	{
		dwToolbarFlags = kmskShowMenuBar |
			kmskShowStandardToolBar |
			kmskShowFormatToolBar;
	}
	int rgShowMask[] = {
		kmskShowMenuBar,
		kmskShowStandardToolBar,
		kmskShowFormatToolBar,
		kmskShowWindowToolBar };
	LoadToolbars(pfws, pszRoot, dwToolbarFlags, rgShowMask);
#if 0
	// TODO SharonC(DarrellZ): If you want this this toolbar, add an item for it in
	// rgShowMask above.
	if (dwBarFlags & kmskShowInsertToolBar)
		CreateToolBar(m_vqtlbr[4]);
#endif
}

/*----------------------------------------------------------------------------------------------
	Save settings specific to this window.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::SaveSettings(const achar * pszRoot, bool fRecursive)
{
/*	AssertPsz(pszRoot);

	SuperClass::SaveSettings(pszRoot);

	SaveWindowPosition(pszRoot, "Position");

	FwSettings * pfws = AfApp::GetSettings();

	// Store the visibility settings for the view bar and the toolbars.
	SaveToolbars(pfws, pszRoot, "Toolbar Flags");*/
}

/*----------------------------------------------------------------------------------------------
	The hwnd has been attached.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::PostAttach(void)
{

	SuperClass::PostAttach();

	// Review DarrellZ: in RnMainWnd, this is not necessary, because somewhere in the middle
	// of this method (before LoadSettings, which needs it) our window gets a SetFocus
	// message and records itself as the main window. Why there and not here?
	AfApp::Papp()->SetCurrentWindow(this);

	// Create the main data window and attach it
	m_qwsw.Attach(NewObj WpSplitWnd);
	m_qwsw->Init(this);
	WndCreateStruct wcs;
	wcs.style |= WS_VISIBLE;
	wcs.InitChild("AfClientWnd", m_hwnd, 1000);
	m_qwsw->CreateHwnd(wcs);
	::ShowWindow(m_qwsw->Hwnd(), SW_SHOW);

	// Create the toolbars.
	const int rgrid[] =
	{
		kridTBarStd,
		kridRnTBarFmtg,
//		kridRnTBarIns,
		kridRnTBarWnd,
		kridTBarInvisible,
	};

	GetMenuMgr()->LoadToolBars(rgrid, SizeOfArray(rgrid));
	GetMenuMgr()->LoadAccelTable(kridAccelStd, 0, m_hwnd);

	AfMenuBarPtr qmnbr;
	AfToolBarPtr qtlbr;

	qmnbr.Create();
	qmnbr->Initialize(m_hwnd, kridAppMenu, kridAppMenu, "Menu Bar");
	m_vqtlbr.Push(qmnbr.Ptr());

	qtlbr.Attach(NewObj RnStdToolBar);
	qtlbr->Initialize(kridTBarStd, kridTBarStd, "Standard Toolbar");
	m_vqtlbr.Push(qtlbr);

	qtlbr.Attach(NewObj RnFmtToolBar);
	qtlbr->Initialize(kridRnTBarFmtg, kridRnTBarFmtg, "Formatting Toolbar");
	m_vqtlbr.Push(qtlbr);

#if 0
	qtlbr.Create();
	qtlbr->Initialize(kridRnTBarIns, kridRnTBarIns, "Insert Toolbar");
	m_vqtlbr.Push(qtlbr);
#endif

	qtlbr.Create();
	qtlbr->Initialize(kridRnTBarWnd, kridRnTBarWnd, "Window Toolbar");
	m_vqtlbr.Push(qtlbr);

	LoadSettings(kpszMainSettingsKey, false);

	g_app.AddCmdHandler(this, 1);

	// Update the icons for the formatting toolbar drop-down buttons.
	// TODO JeffG(ShonK): Do the real thing for these.
	UpdateToolBarIcon(kridRnTBarFmtg, kcidFmttbApplyBgrndColor, RGB(255, 255, 255));
	UpdateToolBarIcon(kridRnTBarFmtg, kcidFmttbApplyFgrndColor, RGB(0, 0, 0));

	// Set the status bar to a safe default state.
	StrApp str(kstidIdle);
	m_qstbr->SetPaneText(0, str.Chars());
}

bool WpMainWnd::OnSize(int wst, int dxp, int dyp)
{
	::MoveWindow(m_qwsw->Hwnd(), 0, 0, dxp, dyp, true);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Reposition our client window.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::OnClientSize(void)
{
	SuperClass::OnClientSize();

	Rect rc;
	SuperClass::GetClientRect(rc);

	if (m_qwsw)
		::MoveWindow(m_qwsw->Hwnd(), rc.left, rc.top, rc.Width(), rc.Height(), true);

	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(!lnRet);

	switch (wm)
	{
	case WM_DESTROY:
		AfApp::Papp()->RemoveWindow(this);
		return false;
	case WM_SETFOCUS:
		AfApp::Papp()->SetCurrentWindow(this);
		if (m_qwsw && m_qwsw->Hwnd())
			SetFocus(m_qwsw->Hwnd());
		break;
	case WM_CLOSE:
		// Strange - but without the PostQuitMessage(0) statement hitting the X button will
		// not close down the program completely
		PostQuitMessage(0);
		return OnClose();
	case WM_SIZING:
		{
			// Keep the main window from resizing below a certain point.
			Rect * prc = (Rect *)lp;
			AssertPtr(prc);

			if (prc->bottom - prc->top < kdypMin)
			{
				if (wp == WMSZ_TOPLEFT || wp == WMSZ_TOP || wp == WMSZ_TOPRIGHT)
					prc->top = prc->bottom - kdypMin;
				else
					prc->bottom = prc->top + kdypMin;
			}
			if (prc->right - prc->left < kdxpMin)
			{
				if (wp == WMSZ_TOPLEFT || wp == WMSZ_LEFT || wp == WMSZ_BOTTOMLEFT)
					prc->left = prc->right - kdxpMin;
				else
					prc->right = prc->left + kdxpMin;
			}
			break;
		}
	// TODO DarrellZ: This almost takes care of the Alt key properly, but there's
	// still some problems with it.
	/*case WM_ENTERMENULOOP:
		if (!wp)
		{
			::SendMessage(m_hwnd, WM_CANCELMODE, 0, 0);
			HWND hwndMenu = m_vqtlbr[0]->Hwnd();
			::SetFocus(hwndMenu);
			return true;
		}
		break;*/
	// Wait 1/2 second after a menu is closed before resetting the text on the status bar.
	// If another menu shows up in that time interval, cancel the timer.
	case WM_ENTERMENULOOP:
		::KillTimer(m_hwnd, knStatusTimer);
		break;
	case WM_EXITMENULOOP:
		::SetTimer(m_hwnd, knStatusTimer, 500, NULL);
		break;
	case WM_TIMER:
		// Reset the status bar text.
		StrApp str(kstidIdle);
		m_qstbr->SetPaneText(0, str.Chars());
		::KillTimer(m_hwnd, knStatusTimer);
		break;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Show the context menu for the toolbars.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::OnContextMenu(HWND hwnd, Point pt)
{
	HWND hwndRebar = m_qrebr->Hwnd();
	if (hwnd == hwndRebar || ::IsChild(hwndRebar, hwnd))
	{
		// Show a popup menu to toggle toolbar states.
		HMENU hmenuPopup = ::CreatePopupMenu();
		if (!hmenuPopup)
			ThrowHr(WarnHr(E_FAIL));
		int ctlbr = m_vqtlbr.Size();
		for (int itlbr = 1; itlbr < ctlbr; itlbr++)
		{
			::AppendMenu(hmenuPopup, MF_STRING, kcidViewTBarsStd - 1 + itlbr,
				m_vqtlbr[itlbr]->GetName());
		}
		::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y, 0, m_hwnd,
			NULL);
		::DestroyMenu(hmenuPopup);
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Update the state of the visible toolbar icons.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::OnIdle()
{
	int ctlbr = m_vqtlbr.Size();
	AfToolBarPtr qtlbr;
	CmdState cms;
	CmdExecPtr qcex = AfApp::GetCmdExec();
	TBBUTTON tbb;
	HWND hwndTool;

	// Use the same mechanism on the toolbar buttons that is used on menu items.
	for (int itlbr = 0; itlbr < ctlbr; itlbr++)
	{
		qtlbr = m_vqtlbr[itlbr];
		AssertObj(qtlbr);

		if (dynamic_cast<AfMenuBar *>(qtlbr.Ptr()) != NULL)
			continue;

		hwndTool = qtlbr->Hwnd();
		if (::IsWindowVisible(hwndTool))
		{
			int cbtn = ::SendMessage(hwndTool, TB_BUTTONCOUNT, 0, 0);
			for (int ibtn = 0; ibtn < cbtn; ibtn++)
			{
				::SendMessage(hwndTool, TB_GETBUTTON, ibtn, (LPARAM)&tbb);
				if (tbb.idCommand)
				{
					cms.Init(tbb.idCommand, this, NULL);

					qcex->FSetCmdState(cms);
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
					::SendMessage(hwndTool, TB_SETSTATE, tbb.idCommand, nState);
				}
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Write settings out to the registry.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::OnClose()
{
	// Review SharonC (JohnT): is this where we need to check about saving the document?
	SaveSettings(kpszMainSettingsKey);

	return false;
}

/*----------------------------------------------------------------------------------------------
	As it finally goes away, make doubly sure all pointers get cleared. This helps break cycles.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::OnReleasePtr()
{
	// By contract we must clear all our own smart pointers.
	m_qwsw.Clear();
	// To prevent spurious memory leaks shut down the encoding factory, releasing
	// cached encodings, writing systems, etc. But only if we are the last window!
	if (AfApp::Papp()->GetMainWndCount() == 1)
	{
		ILgEncodingFactoryPtr qencf;
		qencf.CreateInstance(CLSID_LgEncodingFactory);
		qencf->Shutdown();
	}
	SuperClass::OnReleasePtr();
}

/*----------------------------------------------------------------------------------------------
	Bring up the Page Setup dialog.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdFilePageSetup(Cmd * pcmd)
{
	return true;
}

/*----------------------------------------------------------------------------------------------
	Setup a new test - here we retrieve a filename from the user to log events to
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdWndNewTest(Cmd * pcmd)
{
/*	WpSplitWnd * pwsw = SplitWnd();
	WpChildWnd * pwcw = pwsw->ChildWnd();
*/
	return true;
}
/*----------------------------------------------------------------------------------------------
	This opens the non-modal test script dialog box with a test.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdWndFileScript(Cmd * pcmd)
{
	WpSplitWnd * pwsw = SplitWnd();
	WpChildWnd * pwcw = pwsw->ChildWnd();

	pwcw->ShowScriptDialog();
	return true;
}
/*----------------------------------------------------------------------------------------------
	Cascade all the top-level windows.
----------------------------------------------------------------------------------------------*/
bool WpApp::CmdWndCascade(Cmd * pcmd)
{
	return true;
}

/*----------------------------------------------------------------------------------------------
	Tile all the top-level windows so that they all have the same height and their width is the
	width of the screen.
----------------------------------------------------------------------------------------------*/
bool WpApp::CmdWndTileHoriz(Cmd * pcmd)
{
	return true;
}

/*----------------------------------------------------------------------------------------------
	Tile all the top-level windows so that they all have the same width and their height is the
	height of the screen.
----------------------------------------------------------------------------------------------*/
bool WpApp::CmdWndTileVert(Cmd * pcmd)
{
	return true;
}

/*----------------------------------------------------------------------------------------------
	Bring up the format styles dialog.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdFmtStyles(Cmd * pcmd)
{
	return true;
}

/*----------------------------------------------------------------------------------------------
	Enter What's This help mode.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdHelpMode(Cmd * pcmd)
{
	return true;
}

/*----------------------------------------------------------------------------------------------
	Open the main HTML Help file.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdHelpContents(Cmd * pcmd)
{
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return the toolbar resource ID given a menu ID.
----------------------------------------------------------------------------------------------*/
int WpMainWnd::ToolIdFromMenuId(int rid)
{
	switch (rid)
	{
	case kcidViewTBarsStd:
		return kridTBarStd;
	case kcidViewTBarsFmtg:
		return kridRnTBarFmtg;
#if 0
	case kcidViewTBarsIns:
		return kridRnTBarIns;
#endif
	case kcidViewTBarsWnd:
		return kridRnTBarWnd;
	default:
		Assert(false);
		return -1;
	}
}

/*----------------------------------------------------------------------------------------------
	Toggle whether or not a toolbar is visible.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdTbToggle(Cmd * pcmd)
{
	AssertObj(pcmd);

	int wid = ToolIdFromMenuId(pcmd->m_cid);
	AfToolBarPtr qtlb = GetToolBar(wid);
	ShowToolBar(wid, !qtlb || !::IsWindowVisible(qtlb->Hwnd()));
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return the state of whether or not a toolbar is visible.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmsTbUpdate(CmdState & cms)
{
	AfToolBarPtr qtlb = GetToolBar(ToolIdFromMenuId(cms.Cid()));
	cms.SetCheck(qtlb && ::IsWindowVisible(qtlb->Hwnd()));
	return true;
}

/*----------------------------------------------------------------------------------------------
	Toggle whether or not the status bar is visible.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdSbToggle(Cmd * pcmd)
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
bool WpMainWnd::CmsSbUpdate(CmdState & cms)
{
	AssertPtr(m_qstbr);

	cms.SetCheck(::IsWindowVisible(m_qstbr->Hwnd()));
	return true;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void WpMainWnd::UpdateToolBarIcon(int widToolBar, int widButton, COLORREF clr)
{
	AfToolBarPtr qtlbr = GetToolBar(widToolBar);
	if (!qtlbr)
		return;

	HIMAGELIST himl = (HIMAGELIST)::SendMessage(qtlbr->Hwnd(), TB_GETIMAGELIST, 0, 0);
	if (!himl)
		return;

	TBBUTTONINFO tbbi = { isizeof(tbbi) };
	tbbi.dwMask = TBIF_IMAGE;
	::SendMessage(qtlbr->Hwnd(), TB_GETBUTTONINFO, widButton, (long)&tbbi);

	IMAGEINFO ii;
	ImageList_GetImageInfo(himl, tbbi.iImage, &ii);
	Assert(ii.hbmImage);

	Rect rc(0, 0, ii.rcImage.right - ii.rcImage.left, ii.rcImage.bottom - ii.rcImage.top);
	rc.top = rc.bottom - 3;

	// Update the image.
	HDC hdc = GetDC(m_hwnd);
	HDC hdcMem = CreateCompatibleDC(hdc);
	HBITMAP hbmpImage = CreateCompatibleBitmap(hdc, rc.right, rc.bottom);

	// Draw the selected bitmap into hbmpImage.
	HBITMAP hbmpOld = (HBITMAP)SelectObject(hdcMem, hbmpImage);
	PatBlt(hdcMem, 0, 0, rc.right, rc.bottom, WHITENESS);
	ImageList_Draw(himl, tbbi.iImage, hdcMem, 0, 0, ILD_NORMAL);
	AfGfx::FillSolidRect(hdcMem, rc, clr);
	if (clr == kclrLightGrey)
		::Rectangle(hdcMem, rc.left, rc.top, rc.right, rc.bottom);
	SelectObject(hdcMem, hbmpOld);
	DeleteObject(hdcMem);

	ImageList_Replace(himl, tbbi.iImage, hbmpImage, 0);
	DeleteObject(hbmpImage);

	ReleaseDC(m_hwnd, hdc);
	::SendMessage(qtlbr->Hwnd(), TB_SETIMAGELIST, 0, (long)himl);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool WpApp::CmdHelpAbout(Cmd * pcmd)
{
	return true;
}

/*----------------------------------------------------------------------------------------------
	Bring up another top-level window, initialized from the given file, or empty.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::NewWindow(char * szFileName)
{
	return true;
}


// Enable/disable the menu items under the test menu header
bool WpMainWnd::ScriptEnable()
{
	// For now they are always enabled
	return true;
}
/***********************************************************************************************
	Toolbar initialization stuff.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Change undo/redo buttons to dropdowns when creating the standard toolbar.
----------------------------------------------------------------------------------------------*/
void RnStdToolBar::Create(AfFrameWnd * pwnd)
{
	AfToolBar::Create(pwnd);

	// Set the undo and redo buttons to drop down.
	TBBUTTONINFO tbbi;
	tbbi.cbSize = isizeof(tbbi);
	tbbi.dwMask = TBIF_STYLE;
	// Undo/redo buttons.
	::SendMessage(m_hwnd, TB_GETBUTTONINFO, kcidEditUndo, (long)&tbbi);
	tbbi.fsStyle |= TBSTYLE_DROPDOWN;
	::SendMessage(m_hwnd, TB_SETBUTTONINFO, kcidEditUndo, (long)&tbbi);
	::SendMessage(m_hwnd, TB_SETBUTTONINFO, kcidEditRedo, (long)&tbbi);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void RnFmtToolBar::Create(AfFrameWnd * pwnd)
{
	AfToolBar::Create(pwnd);

	// Set the color buttons and border button to drop down.
	TBBUTTONINFO tbbi;
	tbbi.cbSize = isizeof(tbbi);
	tbbi.dwMask = TBIF_STYLE;
	// Border button.
	::SendMessage(m_hwnd, TB_GETBUTTONINFO, kcidFmttbApplyBdr, (long)&tbbi);
	tbbi.fsStyle |= TBSTYLE_DROPDOWN;
	::SendMessage(m_hwnd, TB_SETBUTTONINFO, kcidFmttbApplyBdr, (long)&tbbi);
	// Color buttons.
	::SendMessage(m_hwnd, TB_GETBUTTONINFO, kcidFmttbApplyBgrndColor, (long)&tbbi);
	tbbi.fsStyle |= TBSTYLE_DROPDOWN;
	::SendMessage(m_hwnd, TB_SETBUTTONINFO, kcidFmttbApplyBgrndColor, (long)&tbbi);
	::SendMessage(m_hwnd, TB_SETBUTTONINFO, kcidFmttbApplyFgrndColor, (long)&tbbi);

	// Set up the combo boxes on the formatting toolbar.
	AfToolBarChildPtr qtbc;
	SetupComboControl(&qtbc, kcidFmttbStyle, 100, 200);
	::SetWindowText(qtbc->Hwnd(), "Normal");
	SetupComboControl(&qtbc, kcidFmttbWrtgSys, 80, 200);
	::SetWindowText(qtbc->Hwnd(), "English");
	SetupComboControl(&qtbc, kcidFmttbFnt, 120, 200);
	::SetWindowText(qtbc->Hwnd(), "Times New Roman");
	SetupComboControl(&qtbc, kcidFmttbFntSize, 40, 200);
	::SetWindowText(qtbc->Hwnd(), "12");
}


/***********************************************************************************************
	WpSplitWnd stuff.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
WpSplitWnd::WpSplitWnd()
{
	m_qwcw.Attach(NewObj WpChildWnd);
}

void WpSplitWnd::OnReleasePtr()
{
	m_qwcw.Clear();
}

void WpSplitWnd::CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew)
{
	Assert(!psplcCopy); // Todo JohnT: create second child using same root box
	WndCreateStruct wcs;

	wcs.InitChild("AfVwWnd", m_hwnd, 0);
	wcs.style |=  WS_VISIBLE;

	*psplcNew = m_qwcw;
	m_qwcw->Init(MainWnd()->FileName());
	m_qwcw->CreateHwnd(wcs);

	AddRefObj(*psplcNew);
}

/***********************************************************************************************
	WpChildWnd stuff.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Make the root box.
----------------------------------------------------------------------------------------------*/
void WpChildWnd::MakeRoot(IVwGraphics * pvg, IVwRootBox ** pprootb)
{
	*pprootb = NULL;

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	// SetSite takes an IVwRootSite, which this class implements.
	CheckHr(qrootb->SetSite(this));
	// The root StText is always ID 1
	HVO  hvoDoc = 1;
	int frag = kfrText;
	// We need a pointer to the pointer, and we can't use &m_qvc because that clears the
	// pointer!!
	IVwViewConstructor * pvvc = m_qvc;

	CheckHr(qrootb->putref_DataAccess(m_qda));

	// Todo JohnT: obtain stylesheet and pass instead of NULL
	CheckHr(qrootb->SetRootObjects(&hvoDoc, &pvvc, &frag, NULL, 1));

	*pprootb = qrootb.Detach();
}

/*----------------------------------------------------------------------------------------------
	Initialize the window, using the next from the given file.
----------------------------------------------------------------------------------------------*/
void WpChildWnd::Init(StrAnsi staFileName)
{
	m_qvc.Attach(NewObj StVc);
	m_qda.Attach(NewObj WpDa);
	m_qda->InitNew(staFileName);

	// Allocate memory for the viewclass object m_tvr and set the rootbox in OnCreate function
	m_tvr = NewObj TestVwRoot(true, true);
	// Assign the viewclass SIL TestSite pointer (that was just created) and assign its address
	// to WpChildWnd's member for initializing the graphics with. The VwGraphics constructor
	// takes an SilTestSite * as an argument, in order that it can baseline various functions
	m_psts = m_tvr->GetSTSPtr();
}

/*----------------------------------------------------------------------------------------------
	Override to make an initial selection, which can't be done until the superclass
	method creates the root box.
----------------------------------------------------------------------------------------------*/
int WpChildWnd::OnCreate(CREATESTRUCT * pcs)
{
	int result = SuperClass::OnCreate(pcs);
	Assert(m_qrootb);

	// Provide a valid rootbox object to the TestVwRoot viewclass object
	m_tvr->SetRootBox(m_qrootb);

	CheckHr(m_qrootb->MakeSimpleSel(true, true, false));
	ShowScriptDialog();
	return result;
}
/*----------------------------------------------------------------------------------------------
	Commit the window's selection.
----------------------------------------------------------------------------------------------*/
bool WpChildWnd::CommitSelection()
{
/*	IVwSelectionPtr qsel;
	CheckHr(m_qrootb->get_Selection(&qsel));
	if (!qsel)
		return true;
	ComBool fOkay;
	CheckHr(qsel->Commit(&fOkay));*/
	return true;
}
//---------------------------------------------------------------------------------------------
// This function overrides the virtual void InitGraphics function in AfVwRoot
// Override here so we can instantiate VwGraphics object that logs function call to a file
void WpChildWnd::InitGraphics()
{
	if (m_cactInitGraphics == 0)
	{
		// We are asking for a VwGraphics but haven't been given a DC. Make one.
		HDC hdc = ::GetDC(Window()->Hwnd());
		if (!m_qvg)
		{
			// First Arg => Pointer to SilTestSite for baselining various graphics operations
			//				Initialized in the WpChildWnd::Init function
			// Second Arg => whether to have graphic functions draw or not
			// Third Arg => whether to log to a file
			m_qvg.Attach(NewObj VwGraphics(m_psts, true, true));
		}
		m_qvg->Initialize(hdc); // puts the DC in the right state
	}
	m_cactInitGraphics++;
}
//---------------------------------------------------------------------------------------------
void WpChildWnd::OnReleasePtr()
{
	if (m_tvr)
		delete m_tvr;
	SuperClass::OnReleasePtr();
}
//---------------------------------------------------------------------------------------------
//---------------------------------------------------------------------------------------------
void WpChildWnd::ShowScriptDialog()
{
	// If the TstScriptDlg already exists, give it the focus and return
	if (m_qtsd)
	{
		::SetFocus(m_qtsd->Hwnd());
		return;
	}

	m_qtsd.Create();
	// After creating the dialog pass two pointers: First - WpChildWnd, and VwGraphics * for
	// Initializing/Uninitializing the graphics system
	m_qtsd->SetDialogValues(this, m_qvg);
	// Assign the ViewClass object pointer
	m_qtsd->SetTestObj(m_tvr);
	// This dialog box will remain open alongside the Main window
	m_qtsd->DoModeless(this->Hwnd());
	// Adjust its location relative to the main window
	MoveWindow(m_qtsd->Hwnd(), 40, 100, 285, 260, true);
}
//---------------------------------------------------------------------------------------------
// Utility function for converting rectangular coordinates to string format
// Specifically when formatting a string of various function calls. For example, if the
// CallMouseDown function was called, this simplifies the extracting of numbers for the rcSrc
// and rcDst rectangles
//---------------------------------------------------------------------------------------------
StrAnsi WpChildWnd::GetRectChar(RECT &rcRect)
{
	StrAnsi str;
	str.FormatAppend("%d %d %d %d", rcRect.left, rcRect.top, rcRect.right, rcRect.bottom);
	return str;
}
//---------------------------------------------------------------------------------------------
/*
						"WRAPPER" functions that record action to file.
						Writing functions used in this app, reading functions found in the
						testview files themselves (e.g. TestVwRoot)
*/
//---------------------------------------------------------------------------------------------
//---------------------------------------------------------------------------------------------
//---------------------------------------------------------------------------------------------
//---------------------------------------------------------------------------------------------

void WpChildWnd::CallOnTyping(VwGraphicsPtr qvg, SmartBstr _bstr, int cchBackspace, int cchDelForward, OLECHAR oleChar,
								RECT rcSrc, RECT rcDst)
{
	// If a valid TestScriptDlg pointer exists
	if (m_qtsd && m_qtsd->IsRecording())
	{
		// Write test action to the modeless dialog
		StrAnsi ans = _bstr.Chars();
		ans.Append("_");
		outstr << "OnTyping " << 6 << " " << ans.Chars() << " " << cchBackspace << " " <<
			cchDelForward << " " << oleChar << " " << GetRectChar(rcSrc).Chars() <<
			" " << GetRectChar(rcDst).Chars();
		m_qtsd->RecordString(outstr.str());
		// Clear the outstr for next usage
		outstr.str("");
	}
	m_tvr->DoOnTyping(qvg, _bstr, cchBackspace, cchDelForward, oleChar, rcSrc, rcDst);
}
/*
void WpChildWnd::testOnChar(int chw)
{
	AddTstFunc("OnChar", 7);
	WriteInt(chw);
	EndTstFunc();

	m_psts->OutputFormat("  FUNCTION: OnChar(%d)\n", chw);
	CheckHr(m_qrootb->OnChar(chw));
}
*/
void WpChildWnd::CallOnSysChar(int chw)
{
	if (m_qtsd && m_qtsd->IsRecording())
	{
		outstr << "OnSysChar " << 8 << " " << chw;
		m_qtsd->RecordString(outstr.str());
		outstr.str("");
	}
	m_tvr->DoOnSysChar(chw);
}

void WpChildWnd::CallOnExtendedKey(int chw, VwShiftStatus ss)
{
	if (m_qtsd && m_qtsd->IsRecording())
	{
		outstr << "OnExtendedKey " << 9 << " " << chw << " " << ss;
		m_qtsd->RecordString(outstr.str());
		outstr.str("");
	}
	m_tvr->DoOnExtendedKey(chw, ss);
}

void WpChildWnd::CallMouseDown(int xd, int yd, RECT rcSrc, RECT rcDst)
{
	if (m_qtsd && m_qtsd->IsRecording())
	{
		outstr << "MouseDown " << 11 << " " << xd << " " << yd << " "
			<< GetRectChar(rcSrc).Chars() << " " << GetRectChar(rcDst).Chars();
		m_qtsd->RecordString(outstr.str());
		outstr.str("");
	}
	m_tvr->DoMouseDown(xd, yd, rcSrc, rcDst);
}

void WpChildWnd::CallMouseMoveDrag(int xd, int yd, RECT rcSrc, RECT rcDst)
{
	if (m_qtsd && m_qtsd->IsRecording())
	{
		outstr << "MouseMoveDrag " << 12 << " " << xd << " " << yd << " "
			<< GetRectChar(rcSrc).Chars() << " " << GetRectChar(rcDst).Chars();
		m_qtsd->RecordString(outstr.str());
		outstr.str("");
	}
	m_tvr->DoMouseMoveDrag(xd, yd, rcSrc, rcDst);
}

void WpChildWnd::CallMouseDownExtended(int xd, int yd, RECT rcSrc, RECT rcDst)
{
	if (m_qtsd && m_qtsd->IsRecording())
	{
		outstr << "MouseDownExtended " << 13 << " " << xd << " " << yd << " "
			<< GetRectChar(rcSrc).Chars() << " " << GetRectChar(rcDst).Chars();
		m_qtsd->RecordString(outstr.str());
		outstr.str("");
	}
	m_tvr->DoMouseDownExtended(xd, yd, rcSrc, rcDst);
}

void WpChildWnd::CallMouseUp(int xd, int yd, RECT rcSrc, RECT rcDst)
{
	if (m_qtsd && m_qtsd->IsRecording())
	{
		outstr << "MouseUp " << 14 << " " << xd << " " << yd << " "
			<< GetRectChar(rcSrc).Chars() << " " << GetRectChar(rcDst).Chars();
		m_qtsd->RecordString(outstr.str());
		outstr.str("");
	}
	m_tvr->DoMouseUp(xd, yd, rcSrc, rcDst);
}

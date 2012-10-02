/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: HelloWorld.cpp
Responsibility: Darrell Zook
Last reviewed: never

Description:
	This file contains the base classes for Hello World.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

// Create one global instance. It has to exist before WinMain is called.
HwApp g_app;

BEGIN_CMD_MAP(HwApp)
	ON_CID_ALL(kcidFileExit, &AfApp::CmdFileExit, NULL)
END_CMD_MAP_NIL()


/***********************************************************************************************
	HwApp methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
HwApp::HwApp()
{
	s_fws.SetRoot("Hello World Toolbar"); //"Software\\SIL\\FieldWorks\\Hello World Toolbar";
}


/*----------------------------------------------------------------------------------------------
	Initialize the application.
----------------------------------------------------------------------------------------------*/
void HwApp::Init(void)
{
	SuperClass::Init();

	AfWnd::RegisterClass("HwMainWnd", 0, 0, 0, COLOR_3DFACE, (int)kridHelloWorldIcon);
	AfWnd::RegisterClass("HwClientWnd", kfwcsHorzRedraw | kfwcsVertRedraw, (int)IDC_ARROW, 0,
		COLOR_WINDOW);

	// Open initial window
	WndCreateStruct wcs;
	wcs.InitMain("HwMainWnd");
	HwMainWndPtr qwnd;
	qwnd.Create();

	qwnd->CreateHwnd(wcs);
	qwnd->Show(m_nShow);
}


/***********************************************************************************************
	HwMainWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Load settings specific to this window.
----------------------------------------------------------------------------------------------*/
void HwMainWnd::LoadSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);

	SuperClass::LoadSettings(pszRoot, fRecursive);

	FwSettings * pfws;
	pfws = AfApp::GetSettings();

	// TODO: Use methods defined on pfws to load settings.

	// Read the toolbar settings. If the settings aren't there, use default values.
	DWORD dwToolbarFlags;
	if (!pfws->GetDword(pszRoot, "Toolbar Flags", &dwToolbarFlags))
		dwToolbarFlags = (DWORD)-1; // Show all toolbars.
	LoadToolbars(pfws, pszRoot, dwToolbarFlags);

	// Get window position.
	LoadWindowPosition(pszRoot, "Position");

	::ShowWindow(m_hwnd, SW_SHOW);
	OnIdle();
	::UpdateWindow(m_hwnd);
}


/*----------------------------------------------------------------------------------------------
	Save settings specific to this window.
----------------------------------------------------------------------------------------------*/
void HwMainWnd::SaveSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);

	SuperClass::SaveSettings(pszRoot, fRecursive);

	SaveWindowPosition(pszRoot, "Position");

	FwSettings * pfws;
	pfws = AfApp::GetSettings();

	// TODO: Use methods defined on pfws to save settings.

	// Store the settings for the toolbars.
	SaveToolbars(pfws, pszRoot, "Toolbar Flags");
}


/*----------------------------------------------------------------------------------------------
	The hwnd has been attached.
----------------------------------------------------------------------------------------------*/
void HwMainWnd::PostAttach(void)
{
	StrAppBuf strbT; // Holds temp string

	// Set the default caption text.
	strbT.Load(kstidHelloWorld);
	::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)strbT.Chars());

	// This creates the main frame window and sets it as the current window. It also
	// creates the rebar and status bar.
	SuperClass::PostAttach();

	const int rgrid[] =
	{
		kridTBarStd,
		kridHwTBarIns,
		kridHwTBarTools,
		kridHwTBarWnd,
	};

	GetMenuMgr()->LoadToolBars(rgrid, SizeOfArray(rgrid));

	// Create the menu bar.
	AfMenuBarPtr qmnbr;
	qmnbr.Create();
	qmnbr->Initialize(m_hwnd, kridAppMenu, kridAppMenu, "Menu Bar");
	m_vqtlbr.Push(qmnbr.Ptr());

	// Create the toolbars.
	AfToolBarPtr qtlbr;

	qtlbr.Create();
	qtlbr->Initialize(kridTBarStd, kridTBarStd, "Standard");
	m_vqtlbr.Push(qtlbr);

	qtlbr.Create();
	qtlbr->Initialize(kridHwTBarIns, kridHwTBarIns, "Insert");
	m_vqtlbr.Push(qtlbr);

	qtlbr.Create();
	qtlbr->Initialize(kridHwTBarTools, kridHwTBarTools, "Tools");
	m_vqtlbr.Push(qtlbr);

	qtlbr.Create();
	qtlbr->Initialize(kridHwTBarWnd, kridHwTBarWnd, "Window");
	m_vqtlbr.Push(qtlbr);

	// Load window settings.
	LoadSettings(NULL, false);

	g_app.AddCmdHandler(this, 1);
	m_qstbr->RestoreStatusText();

	// Create the client window.
	const int kwidChild = 1000;
	WndCreateStruct wcs;
	wcs.InitChild("HwClientWnd", m_hwnd, kwidChild);
	wcs.dwExStyle |= WS_EX_CLIENTEDGE;
	m_qhwcw.Create();
	m_qhwcw->CreateHwnd(wcs);
	::ShowWindow(m_qhwcw->Hwnd(), SW_SHOW);
}


/*----------------------------------------------------------------------------------------------
	Reposition our child windows.
----------------------------------------------------------------------------------------------*/
bool HwMainWnd::OnClientSize(void)
{
	SuperClass::OnClientSize();

	Rect rc;
	SuperClass::GetClientRect(rc);
	if (m_qhwcw)
		::MoveWindow(m_qhwcw->Hwnd(), rc.left, rc.top, rc.Width(), rc.Height(), true);

	return false;
}


/*----------------------------------------------------------------------------------------------
	As it finally goes away, make doubly sure all pointers get cleared. This helps break cycles.
----------------------------------------------------------------------------------------------*/
void HwMainWnd::OnReleasePtr()
{
	m_qhwcw.Clear();
	g_app.RemoveCmdHandler(this, 1);
	SuperClass::OnReleasePtr();
}


/***********************************************************************************************
	HwClientWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Paint Hello World in the middle of the client area.
----------------------------------------------------------------------------------------------*/
bool HwClientWnd::OnPaint(HDC hdcDef)
{
	Assert(!hdcDef);

	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps);

	Rect rc;
	GetClientRect(rc);

	::DrawText(hdc, _T("Hello World!"), -1, &rc, DT_SINGLELINE | DT_VCENTER | DT_CENTER);

	::EndPaint(m_hwnd, &ps);
	return true;
}
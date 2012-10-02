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

CComModule _Module;

BEGIN_CMD_MAP(HwApp)
	ON_CID_ALL(kcidFileExit, &AfApp::CmdFileExit, NULL)
END_CMD_MAP_NIL()


BEGIN_CMD_MAP(HwMainWnd)
	ON_CID_ALL(kcidFileAbout, &HwMainWnd::CmdFileAbout, NULL)
END_CMD_MAP_NIL()


/***********************************************************************************************
	HwApp methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
HwApp::HwApp()
{
	s_fws.SetRoot("Hello World ActiveX"); //"Software\\SIL\\FieldWorks\\Hello World ActiveX";
	_Module.Init(NULL, ModuleEntry::GetModuleHandle(), &LIBID_SACONTROLLib);
}


/*----------------------------------------------------------------------------------------------
	Initialize the application.
----------------------------------------------------------------------------------------------*/
void HwApp::Init(void)
{
	SuperClass::Init();

	AfWnd::RegisterClass("HwMainWnd", 0, 0, 0, COLOR_3DFACE, (int)kridHelloWorldIcon);

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
		dwToolbarFlags = (DWORD)-1; // Show all toolbars
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
	wcs.InitChild("STATIC", m_hwnd, kwidChild);
	m_qwndHost.Create();
	m_qwndHost->CreateAndSubclassHwnd(wcs);

	Assert(memcmp(&CLSID_SaCtrlEvent, &DIID__DSAEvents, isizeof(GUID)) == 0);

	CComCoClass<SaCtrlEvent>::CreateInstance(&m_qsce);
	IUnknownPtr qunk;
	CheckHr(AtlAxCreateControlEx(L"SA.SACtrl.1", m_qwndHost->Hwnd(), NULL, NULL, &qunk,
		DIID__DSAEvents, (IUnknown *)m_qsce.Ptr()));
	CheckHr(qunk->QueryInterface(DIID__DSA, (void **)&m_qctrl));
	m_qsce->Init(this, m_qctrl);
	StrUni stu(L"baluchi.wav");
	CheckHr(m_qctrl->put_WaveFile(stu.Bstr()));
	CheckHr(m_qctrl->raw_SetGraphs(GRAPH_MAGNITUDE, GRAPH_MAGNITUDE));
	CheckHr(m_qctrl->put_ShowContextMenu(false));
	CheckHr(m_qctrl->put_ShowCursorContextMenu(false));

	::ShowWindow(m_qwndHost->Hwnd(), SW_SHOW);

	g_app.AddCmdHandler(this, 1);
}


/*----------------------------------------------------------------------------------------------
	Reposition our child windows.
----------------------------------------------------------------------------------------------*/
bool HwMainWnd::OnClientSize(void)
{
	SuperClass::OnClientSize();

	Rect rc;
	SuperClass::GetClientRect(rc);
	if (m_qwndHost)
		::MoveWindow(m_qwndHost->Hwnd(), rc.left, rc.top, rc.Width(), rc.Height(), true);

	return false;
}


/*----------------------------------------------------------------------------------------------
	Show the help dialog for the ActiveX control.
----------------------------------------------------------------------------------------------*/
bool HwMainWnd::CmdFileAbout(Cmd * pcmd)
{
	/*HRESULT hr;
	hr = m_qctrl->raw_AboutBox();
	return true;*/
	return false;
}


/*----------------------------------------------------------------------------------------------
	As it finally goes away, make doubly sure all pointers get cleared. This helps break cycles.
----------------------------------------------------------------------------------------------*/
void HwMainWnd::OnReleasePtr()
{
	m_qwndHost.Clear();
	m_qsce.Clear();
	m_qctrl.Clear();
	g_app.RemoveCmdHandler(this, 1);
	SuperClass::OnReleasePtr();
}


/***********************************************************************************************
	SaCtrlEvent methods.
***********************************************************************************************/

void __stdcall SaCtrlEvent::OnCursorMoved(short nID, long dwPosition)
{
	AfStatusBar * pstat = m_qwndMain->GetStatusBarWnd();
	AssertPtr(pstat);
	long csamp;
	m_qctrl->get_NumberOfSamples(&csamp);
	pstat->SetLocationStatus(dwPosition, csamp);
}


void __stdcall SaCtrlEvent::OnCursorContextMenu(long x, long y, short nID)
{
	HMENU hmenuPopup = ::CreatePopupMenu();

	::AppendMenu(hmenuPopup, MF_STRING, 1, "Change color...");
	::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
	::AppendMenu(hmenuPopup, MF_STRING, 2, "Delete");

	::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, x, y, 0, m_qwndMain->Hwnd(),
		NULL);
	::DestroyMenu(hmenuPopup);
}


void __stdcall SaCtrlEvent::OnContextMenu(long x, long y)
{
	HMENU hmenuPopup = ::CreatePopupMenu();

	::AppendMenu(hmenuPopup, MF_STRING, 1, "Open file...");
	::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
	::AppendMenu(hmenuPopup, MF_STRING, 2, "Play");

	::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, x, y, 0, m_qwndMain->Hwnd(),
		NULL);
	::DestroyMenu(hmenuPopup);
}
/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: DllClient.cpp
Responsibility: Darrell Zook
Last reviewed: never

Description:
	This file contains the base classes for Dll Client application.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

// Create one global instance. It has to exist before WinMain is called.
DcApp g_app;

BEGIN_CMD_MAP(DcApp)
	ON_CID_ALL(kcidFileExit, &AfApp::CmdFileExit, NULL)
END_CMD_MAP_NIL()


/***********************************************************************************************
	DcApp methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
DcApp::DcApp()
{
	s_fws.SetRoot("Dll Client application"); //"Software\\SIL\\FieldWorks\\Hello World";
}


/*----------------------------------------------------------------------------------------------
	Initialize the application.
----------------------------------------------------------------------------------------------*/
void DcApp::Init(void)
{
	SuperClass::Init();

	AfWnd::RegisterClass("DcMainWnd", 0, 0, 0, COLOR_3DFACE, (int)kridDllClientIcon);
	AfWnd::RegisterClass("DcClientWnd", kfwcsHorzRedraw | kfwcsVertRedraw, (int)IDC_ARROW, 0,
		COLOR_WINDOW);

	// Open initial window
	WndCreateStruct wcs;
	wcs.InitMain("DcMainWnd");
	DcMainWndPtr qwnd;
	qwnd.Create();

	qwnd->CreateHwnd(wcs);
	qwnd->Show(m_nShow);
}


/***********************************************************************************************
	DcMainWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Load settings specific to this window.
----------------------------------------------------------------------------------------------*/
void DcMainWnd::LoadSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);

	SuperClass::LoadSettings(pszRoot, fRecursive);

	FwSettings * pfws;
	pfws = AfApp::GetSettings();

	// TODO: Use methods defined on pfws to load settings.

	// Get window position.
	LoadWindowPosition(pszRoot, "Position");

	::ShowWindow(m_hwnd, SW_SHOW);
	OnIdle();
	::UpdateWindow(m_hwnd);
}


/*----------------------------------------------------------------------------------------------
	Save settings specific to this window.
----------------------------------------------------------------------------------------------*/
void DcMainWnd::SaveSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);

	SuperClass::SaveSettings(pszRoot, fRecursive);

	SaveWindowPosition(pszRoot, "Position");

	FwSettings * pfws;
	pfws = AfApp::GetSettings();

	// TODO: Use methods defined on pfws to save settings.
}


/*----------------------------------------------------------------------------------------------
	The hwnd has been attached.
----------------------------------------------------------------------------------------------*/
void DcMainWnd::PostAttach(void)
{
	StrAppBuf strbT; // Holds temp string

	// Set the default caption text.
	strbT.Load(kstidDllClient);
	::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)strbT.Chars());

	// This creates the main frame window and sets it as the current window. It also
	// creates the rebar and status bar.
	SuperClass::PostAttach();

	// Create the menu bar.
	AfMenuBarPtr qmnbr;
	qmnbr.Create();
	qmnbr->Initialize(m_hwnd, kridAppMenu, kridAppMenu, "Menu Bar");
	m_vqtlbr.Push(qmnbr.Ptr());
	CreateToolBar(qmnbr, true, true, 200);

	// Load window settings.
	LoadSettings(NULL, false);

	g_app.AddCmdHandler(this, 1);
	m_qstbr->RestoreStatusText();

	// Create the client window.
	const int kwidChild = 1000;
	WndCreateStruct wcs;
	wcs.InitChild("DcClientWnd", m_hwnd, kwidChild);
	wcs.dwExStyle |= WS_EX_CLIENTEDGE;
	m_dccw.Create();
	m_dccw->CreateHwnd(wcs);
	::ShowWindow(m_dccw->Hwnd(), SW_SHOW);
}


/*----------------------------------------------------------------------------------------------
	Resize the child window.
----------------------------------------------------------------------------------------------*/
bool DcMainWnd::OnSize(int wst, int dxp, int dyp)
{
	if (m_dccw)
	{
		Rect rc;
		GetClientRect(rc);
		::MoveWindow(m_dccw->Hwnd(), rc.left, rc.top, rc.Width(), rc.Height(), true);
	}
	return SuperClass::OnSize(wst, dxp, dyp);
}


/*----------------------------------------------------------------------------------------------
	As it finally goes away, make doubly sure all pointers get cleared. This helps break cycles.
----------------------------------------------------------------------------------------------*/
void DcMainWnd::OnReleasePtr()
{
	m_dccw.Clear();
	g_app.RemoveCmdHandler(this, 1);
	SuperClass::OnReleasePtr();
}


/***********************************************************************************************
	DcClientWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Paint Dll Client application in the middle of the client area.
----------------------------------------------------------------------------------------------*/
bool DcClientWnd::OnPaint(HDC hdcDef)
{
	Assert(!hdcDef);

	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps);

	Rect rc;
	GetClientRect(rc);

	try
	{

		ISampleInterfacePtr qsi;
		qsi.CreateInstance(CLSID_SampleInterface);

		SmartBstr bstrText;
		CheckHr(qsi->get_HelloWorldString(&bstrText));
		StrApp staText(bstrText.Chars());

		::DrawText(hdc, staText.Chars(), -1, &rc, DT_SINGLELINE | DT_VCENTER | DT_CENTER);
	}
	catch (...)
	{
		// Nothing much we can do...try this
		::DrawText(hdc, _T("Drawing failed!"), -1, &rc, DT_SINGLELINE | DT_VCENTER | DT_CENTER);
	}

	::EndPaint(m_hwnd, &ps);
	return true;
}

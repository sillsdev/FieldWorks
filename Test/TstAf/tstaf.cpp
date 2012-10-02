/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: tstaf.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a basic test for the AfApp core functions.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

// Create one global instance. It has to exist before WinMain is called.
static TstAf g_app;

/*----------------------------------------------------------------------------------------------
	The test command map.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(TstMainWnd)
	ON_CID_ME(FORMAT_BORDER, &TstMainWnd::CmdFmtBorder, NULL)
	ON_CID_GEN(VIEW_TOOLBARS_STANDARD, &TstMainWnd::OnToolBarsStandard, NULL)
	ON_CID_GEN(VIEW_TOOLBARS_FORMATTING, &TstMainWnd::OnToolBarsFormatting, NULL)
	ON_CID_GEN(VIEW_TOOLBARS_DATA, &TstMainWnd::OnToolBarsData, NULL)
	ON_CID_GEN(VIEW_TOOLBARS_VIEW, &TstMainWnd::OnToolBarsView, NULL)
	ON_CID_GEN(VIEW_TOOLBARS_INSERT, &TstMainWnd::OnToolBarsInsert, NULL)
	ON_CID_GEN(VIEW_TOOLBARS_TOOLS, &TstMainWnd::OnToolBarsTools, NULL)
	ON_CID_GEN(VIEW_TOOLBARS_WINDOW, &TstMainWnd::OnToolBarsWindow, NULL)
END_CMD_MAP_NIL()


/*----------------------------------------------------------------------------------------------
	Initialize the application.
----------------------------------------------------------------------------------------------*/
void TstAf::Init(void)
{
	TstMainWndPtr qwnd;

	AfWnd::RegisterClass("TstAfMain", 0, 0, IDR_APPNMENU, COLOR_WINDOW,
		(int)IDI_APPLICATION);
	AfWnd::RegisterClass("TstAfClient", 0, (int)IDC_ARROW, 0, COLOR_WINDOW, 0);

	const int rgrid[] =
	{
		IDR_TOOLBAR_STANDARD,
		IDR_TOOLBAR_FORMATTING,
		IDR_TOOLBAR_DATA,
		IDR_TOOLBAR_VIEW,
		IDR_TOOLBAR_INSERT,
		IDR_TOOLBAR_TOOLS,
		IDR_TOOLBAR_WINDOW,
	};

	AfApp::GetMenuMgr()->LoadToolBars(rgrid, SizeOfArray(rgrid));

	WndCreateStruct wcs;

	wcs.InitMain("TstAfMain");

	qwnd.Create();
	qwnd->CreateHwnd(wcs);
	qwnd->Show(m_nShow);

	AfApp::GetMenuMgr()->LoadAccelTable(IDR_ACCELERATOR1, 0, qwnd->Hwnd());
}


/*----------------------------------------------------------------------------------------------
	The hwnd has been attached.
----------------------------------------------------------------------------------------------*/
void TstMainWnd::PostAttach(void)
{
	SuperClass::PostAttach();

	WndCreateStruct wcs;

	CreateToolBar(IDR_TOOLBAR_STANDARD, IDR_TOOLBAR_STANDARD);
	HWND hwnd = CreateToolBar(IDR_TOOLBAR_FORMATTING, IDR_TOOLBAR_FORMATTING)->Hwnd();
	CreateToolBar(IDR_TOOLBAR_DATA, IDR_TOOLBAR_DATA);

	// Set the color buttons to drop down.
	::SendMessage(hwnd, TB_SETEXTENDEDSTYLE, 0, TBSTYLE_EX_DRAWDDARROWS);

	TBBUTTONINFO tbbi;
	tbbi.cbSize = isizeof(tbbi);
	tbbi.dwMask = TBIF_STYLE;

	::SendMessage(hwnd, TB_GETBUTTONINFO, FORMATTB_APPLY_BACKGROUND_COLOR, (long)&tbbi);
	tbbi.fsStyle |= TBSTYLE_DROPDOWN;
	::SendMessage(hwnd, TB_SETBUTTONINFO, FORMATTB_APPLY_BACKGROUND_COLOR, (long)&tbbi);
	::SendMessage(hwnd, TB_GETBUTTONINFO, FORMATTB_APPLY_FOREGROUND_COLOR, (long)&tbbi);
	tbbi.fsStyle |= TBSTYLE_DROPDOWN;
	::SendMessage(hwnd, TB_SETBUTTONINFO, FORMATTB_APPLY_FOREGROUND_COLOR, (long)&tbbi);

	wcs.InitChild("TstAfClient", m_hwnd, kwidClient);
	wcs.style |= WS_VISIBLE;

	m_qtcw.Create();
	m_qtcw->CreateHwnd(wcs);

	g_app.AddCmdHandler(this, 1);
}


/*----------------------------------------------------------------------------------------------
	Reposition our client window.
----------------------------------------------------------------------------------------------*/
bool TstMainWnd::OnClientSize(void)
{
	SuperClass::OnClientSize();

	if (m_qtcw)
	{
		Rect rc;

		GetClientRect(rc);
		::MoveWindow(m_qtcw->Hwnd(), rc.left, rc.top, rc.Width(), rc.Height(), true);
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool TstMainWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(!lnRet);

	switch (wm)
	{
	case WM_DESTROY:
		g_app.Quit();
		return false;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Bring up the format border dialog.
----------------------------------------------------------------------------------------------*/
bool TstMainWnd::CmdFmtBorder(Cmd * pcmd)
{
	FmtBorderParaDlgPtr qbrdp;
	qbrdp.Create();
	qbrdp->DoModal(IDD_BP, m_hwnd);

	return true;
}


/*----------------------------------------------------------------------------------------------
	When resized, redraw.
----------------------------------------------------------------------------------------------*/
bool TstClientWnd::OnSize(int wst, int dxs, int dys)
{
	HDC hdc = GetDC(m_hwnd);

	OnPaint(hdc);

	ReleaseDC(m_hwnd, hdc);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool TstClientWnd::OnPaint(HDC hdcDef)
{
	AssertObj(this);

	HDC hdc;
	PAINTSTRUCT ps;
	Rect rc;
	Rect rcT;

	// Don't use the passed in hdc. Use the one returned from BeginPaint.
	if (hdcDef)
		hdc = hdcDef;
	else
		hdc = BeginPaint(m_hwnd, &ps);

	GetClientRect(rc);

	rcT = rc;
	rcT.bottom = 20;
	AfGfx::FillSolidRect(hdc, rcT, RGB(255, 255, 255));

	rcT = rc;
	rcT.right = 20;
	AfGfx::FillSolidRect(hdc, rcT, RGB(255, 255, 255));

	rcT = rc;
	rcT.left = rcT.right - 20;
	AfGfx::FillSolidRect(hdc, rcT, RGB(255, 255, 255));

	rcT = rc;
	rcT.top = rcT.bottom - 20;
	AfGfx::FillSolidRect(hdc, rcT, RGB(255, 255, 255));

	rc.Inflate(-20, -20);
	AfGfx::FillSolidRect(hdc, rc, ::GetSysColor(COLOR_3DLIGHT));
	::DrawText(hdc, "Hello, Win32!", -1, &rc, DT_SINGLELINE | DT_CENTER | DT_VCENTER);

	if (!hdcDef)
		EndPaint(m_hwnd, &ps);

	return true;
}


bool TstMainWnd::OnToolBarsStandard(Cmd * pcmd)
{
	ShowToolBar(IDR_TOOLBAR_STANDARD, false);
	return true;
}

bool TstMainWnd::OnToolBarsFormatting(Cmd * pcmd)
{
	ShowToolBar(IDR_TOOLBAR_FORMATTING, false);
	return true;
}

bool TstMainWnd::OnToolBarsData(Cmd * pcmd)
{
	ShowToolBar(IDR_TOOLBAR_DATA, true);
	return true;
}

bool TstMainWnd::OnToolBarsView(Cmd * pcmd)
{
	ShowToolBar(IDR_TOOLBAR_VIEW, true);
	return true;
}

bool TstMainWnd::OnToolBarsInsert(Cmd * pcmd)
{
	ShowToolBar(IDR_TOOLBAR_INSERT, true);
	return true;
}

bool TstMainWnd::OnToolBarsTools(Cmd * pcmd)
{
	ShowToolBar(IDR_TOOLBAR_TOOLS, true);
	return true;
}

bool TstMainWnd::OnToolBarsWindow(Cmd * pcmd)
{
	ShowToolBar(IDR_TOOLBAR_WINDOW, true);
	return true;
}

bool TstMainWnd::OnNotify(int id, NMHDR * pnmh)
{
	if (id == IDR_TOOLBAR_FORMATTING && pnmh->code == TBN_DROPDOWN)
	{
		MessageBox(NULL, "Drop down color", NULL, MB_OK);
		return true;
	}
	return false;
}

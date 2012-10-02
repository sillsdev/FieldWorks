/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: HelloView.cpp
Responsibility: Darrell Zook
Last reviewed: never

Description:
	This file contains the base classes for Hello View.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

// Create one global instance. It has to exist before WinMain is called.
HvApp g_app;

BEGIN_CMD_MAP(HvApp)
	ON_CID_ALL(kcidFileExit, &AfApp::CmdFileExit, NULL)
END_CMD_MAP_NIL()


/***********************************************************************************************
	HvApp methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
HvApp::HvApp()
{
	s_fws.SetRoot("Hello View"); //"Software\\SIL\\FieldWorks\\Hello World";
}


/*----------------------------------------------------------------------------------------------
	Initialize the application.
----------------------------------------------------------------------------------------------*/
void HvApp::Init(void)
{
	SuperClass::Init();

	AfWnd::RegisterClass("HvMainWnd", 0, 0, 0, COLOR_3DFACE, (int)kridHelloViewIcon);
	AfWnd::RegisterClass("HvClientWnd", kfwcsHorzRedraw | kfwcsVertRedraw, (int)IDC_ARROW, 0,
		COLOR_WINDOW);

	// Open initial window
	WndCreateStruct wcs;
	wcs.InitMain("HvMainWnd");
	HvMainWndPtr qwnd;
	qwnd.Create();

	qwnd->CreateHwnd(wcs);
	qwnd->Show(m_nShow);
}


/***********************************************************************************************
	HvMainWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Load settings specific to this window.
----------------------------------------------------------------------------------------------*/
void HvMainWnd::LoadSettings(const achar * pszRoot, bool fRecursive)
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
void HvMainWnd::SaveSettings(const achar * pszRoot, bool fRecursive)
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
void HvMainWnd::PostAttach(void)
{
	StrAppBuf strbT; // Holds temp string

	// Set the default caption text.
	strbT.Load(kstidHelloView);
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
	wcs.InitChild("HvClientWnd", m_hwnd, kwidChild);
	wcs.dwExStyle |= WS_EX_CLIENTEDGE;
	m_hvcw.Create();
	m_hvcw->CreateHwnd(wcs);
	::ShowWindow(m_hvcw->Hwnd(), SW_SHOW);
}


/*----------------------------------------------------------------------------------------------
	Resize the child window.
----------------------------------------------------------------------------------------------*/
bool HvMainWnd::OnSize(int wst, int dxp, int dyp)
{
	if (m_hvcw)
	{
		Rect rc;
		GetClientRect(rc);
		::MoveWindow(m_hvcw->Hwnd(), rc.left, rc.top, rc.Width(), rc.Height(), true);
	}
	return SuperClass::OnSize(wst, dxp, dyp);
}


/*----------------------------------------------------------------------------------------------
	As it finally goes away, make doubly sure all pointers get cleared. This helps break cycles.
----------------------------------------------------------------------------------------------*/
void HvMainWnd::OnReleasePtr()
{
	m_hvcw.Clear();
	g_app.RemoveCmdHandler(this, 1);
	SuperClass::OnReleasePtr();
}


/***********************************************************************************************
	HvClientWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Make the root box.
----------------------------------------------------------------------------------------------*/
void HvClientWnd::MakeRoot(IVwGraphics * pvg, ILgEncodingFactory * pencf, IVwRootBox ** pprootb)
{
	AssertPtrN(pencf);
	*pprootb = NULL;

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	// SetSite takes an IVwRootSite, which this class implements.
	CheckHr(qrootb->SetSite(this));

	// We're going to simulate an "object" by giving it a dummy handle of 1
	HVO  hvoRoot = 1;
	// Which fragment of the root object we're going to display. Needs to be a variable
	// so we can simulate an array using &.
	int frag = kfrText;

	// We need our special view constructor
	HvVcPtr qhvvc;
	qhvvc.Attach(NewObj HvVc());
	// We need a pointer to the pointer, and we can't use &qhvvc because that clears the
	// pointer!!
	IVwViewConstructor * pvvc = qhvvc;

	// We need a data source. We can use the in-memory VwCacheDa.
	VwCacheDaPtr qcda;
	qcda.Attach(NewObj VwCacheDa());

	if (pencf)
		CheckHr(qcda->putref_EncodingFactory(pencf));
	else
	{
		ILgEncodingFactoryPtr qencf;
		qencf.CreateInstance(CLSID_LgEncodingFactory);	// Get the registry-based factory.

		CheckHr(qcda->putref_EncodingFactory(qencf));
	}

	CheckHr(qrootb->putref_DataAccess(qcda));

	// Put some data in it. Make a TsString. To do so we need a string factory.
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	ITsStringPtr qtss;
	OLECHAR * rgchHello = L"Hello World! This is a view";
	int encEng = StrUtil::ParseEnc("ENG");
	CheckHr(qtsf->MakeStringRgch(rgchHello, wcslen(rgchHello), encEng, &qtss));
	// Put the string in the data source as the value of ktagProp.
	CheckHr(qcda->CacheStringProp(hvoRoot, ktagProp, qtss));

	CheckHr(qrootb->SetRootObjects(&hvoRoot, &pvvc, &frag, NULL, 1));
	*pprootb = qrootb.Detach();
}

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP HvVc::Display(IVwEnv* pvwenv, HVO hvo, int frag)
{
	switch(frag)
	{
	default:
		Assert(false);
		break;
	case kfrText:
		CheckHr(pvwenv->AddStringProp(ktagProp, this));
		break;
	}
	return S_OK;
}
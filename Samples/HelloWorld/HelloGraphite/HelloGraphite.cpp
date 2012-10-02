/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: HelloGraphite.cpp
Responsibility: Sharon Correll
Last reviewed: never

Description:
	This file contains the base classes for Hello Graphite.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

// Create one global instance. It has to exist before WinMain is called.
HgApp g_app;

BEGIN_CMD_MAP(HgApp)
	ON_CID_ALL(kcidFileExit, &AfApp::CmdFileExit, NULL)
END_CMD_MAP_NIL()


/***********************************************************************************************
	HvApp methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
HgApp::HgApp()
{
	s_fws.SetRoot("Hello Graphite"); //"Software\\SIL\\FieldWorks\\Hello World";
}


/*----------------------------------------------------------------------------------------------
	Initialize the application.
----------------------------------------------------------------------------------------------*/
void HgApp::Init(void)
{
	SuperClass::Init();

	AfWnd::RegisterClass("HgMainWnd", 0, 0, 0, COLOR_3DFACE, (int)kridHelloGraphiteIcon);
	AfWnd::RegisterClass("HgClientWnd", kfwcsHorzRedraw | kfwcsVertRedraw, (int)IDC_ARROW, 0,
		COLOR_WINDOW);

	// Open initial window
	WndCreateStruct wcs;
	wcs.InitMain("HgMainWnd");
	HgMainWndPtr qwnd;
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
void HgMainWnd::LoadSettings(const achar * pszRoot, bool fRecursive)
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
void HgMainWnd::SaveSettings(const achar * pszRoot, bool fRecursive)
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
void HgMainWnd::PostAttach(void)
{
	StrAppBuf strbT; // Holds temp string

	// Set the default caption text.
	strbT.Load(kstidHelloGraphite);
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
	wcs.InitChild("HgClientWnd", m_hwnd, kwidChild);
	wcs.dwExStyle |= WS_EX_CLIENTEDGE;
	m_hgcw.Create();
	m_hgcw->CreateHwnd(wcs);
	::ShowWindow(m_hgcw->Hwnd(), SW_SHOW);
}


/*----------------------------------------------------------------------------------------------
	Resize the child window.
----------------------------------------------------------------------------------------------*/
bool HgMainWnd::OnSize(int wst, int dxp, int dyp)
{
	if (m_hgcw)
	{
		Rect rc;
		GetClientRect(rc);
		::MoveWindow(m_hgcw->Hwnd(), rc.left, rc.top, rc.Width(), rc.Height(), true);
	}
	return SuperClass::OnSize(wst, dxp, dyp);
}


/*----------------------------------------------------------------------------------------------
	As it finally goes away, make doubly sure all pointers get cleared. This helps break cycles.
----------------------------------------------------------------------------------------------*/
void HgMainWnd::OnReleasePtr()
{
	m_hgcw.Clear();
	g_app.RemoveCmdHandler(this, 1);

	// Get all this stuff deleted, particularly the Graphite engine, before the DLLs are
	// unloaded.
	ILgEncodingFactoryPtr qencf;
	qencf.CreateInstance(CLSID_LgEncodingFactory);
	qencf->Shutdown();

	SuperClass::OnReleasePtr();
}


/***********************************************************************************************
	HgClientWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Make the root box.
----------------------------------------------------------------------------------------------*/
void HgClientWnd::MakeRoot(IVwGraphics * pvg, IVwRootBox ** pprootb)
{
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
	HgVcPtr qhgvc;
	qhgvc.Attach(NewObj HgVc());
	// We need a pointer to the pointer, and we can't use &qhvvc because that clears the
	// pointer!!
	IVwViewConstructor * pvvc = qhgvc;

	// We need a data source. We can use the in-memory VwCacheDa.
	VwCacheDaPtr qcda;
	qcda.Attach(NewObj VwCacheDa());
	CheckHr(qrootb->putref_DataAccess(qcda));

	ITsStringPtr qtss;
	ReadInitFile(&qtss);

	qhgvc->SetFontSize(m_nSize * 1000);

	// Put the string in the data source as the value of ktagProp.
	CheckHr(qcda->CacheStringProp(hvoRoot, ktagProp, qtss));

	CheckHr(qrootb->SetRootObjects(&hvoRoot, &pvvc, &frag, NULL, 1));
	*pprootb = qrootb.Detach();
}

/*----------------------------------------------------------------------------------------------
	Read the initialization file which contains the string to render and the font to use.
----------------------------------------------------------------------------------------------*/
void HgClientWnd::ReadInitFile(ITsString ** pptss)
{
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	ITsStringPtr qtss;

	FILE * f = fopen("hginit.txt", "rb");
	OLECHAR rgchwHello[1000];
	int nSize = 14;
	OLECHAR rgchwFont[64];
	int enc;
	if (!f)
	{
		OLECHAR * rgchwTmp = L"Hello World! We're not using Graphite because we couldn't read the hginit.txt file.";
		memcpy(rgchwHello, rgchwTmp, 85 * 2);
		enc = StrUtil::ParseEnc("ENG");
	}
	else
	{
		//	Get length of file.
		int cbFileLen;
		fseek(f, 0, SEEK_END);
		cbFileLen = ftell(f);
		fseek(f, 0, SEEK_SET);

		cbFileLen++; // null termination

		OLECHAR * pswzData = NewObj OLECHAR[cbFileLen];
		memset(pswzData, 0, cbFileLen);
		char * pszData = NewObj char[cbFileLen];
		memset(pszData, 0, cbFileLen);

		fread(pszData, 1, cbFileLen, f);
		fclose(f);

		if (*pszData == 0xFFFFFFFF && *(pszData + 1) == 0xFFFFFFFE)
		{
			// UTF-16, little-endian
			memcpy(pswzData, pszData, cbFileLen);
		}
		else if (*pszData == 0xFFFFFFFE && *(pszData + 1) == 0xFFFFFFFF)
		{
			// UTF-16, big-endian--reverse byte order.
			char * pch = pszData + 2;
			OLECHAR * pchw = pswzData;
			while (pch < pszData + cbFileLen && (*pch != 0 || *(pch + 1) != 0))
			{
				OLECHAR chwTmp1 = (OLECHAR)((*pch) << 8);
				OLECHAR chwTmp2 = (OLECHAR)(*(pch + 1) & 0x00FF);
				*pchw = (OLECHAR)(chwTmp1 | chwTmp2);
				pch += 2;
				pchw++;
			}
		}
		else
		{
			// UTF-8
			if (*pszData == 0xFFFFFFEF && *(pszData + 1) == 0xFFFFFFBB &&
				*(pszData + 2) == 0xFFFFFFBF)
			{
				// Skip UTF-8 markers
				SetUtf16FromUtf8(pswzData, cbFileLen - 3, pszData + 3);
			}
			else
				SetUtf16FromUtf8(pswzData, cbFileLen, pszData);
		}

		// Read the name of the font.
		OLECHAR * pchw = pswzData;
		OLECHAR * pchwFont = rgchwFont;
		while (*pchw != 0 && *pchw != ';' && *pchw != 10 && *pchw != 13)
			*pchwFont++ = *pchw++;
		*pchwFont = 0;	// zero termination
		pchw++;
		while (*pchw == 10 || *pchw == 13)
			pchw++;


		// TODO: read the Graphite feature string.
		OLECHAR rgchwGrFeat[1];
		rgchwGrFeat[0] = 0;

		// Read the size.
		char rgchSize[10];
		char * pchSize = rgchSize;
		while (*pchw != 0 && *pchw != ';' && *pchw != 10 && *pchw != 13)
			*pchSize++ =  (char)*pchw++;
		*pchSize = 0;	// zero termination
		pchw++;
		nSize = atoi(rgchSize);
		while (*pchw == 10 || *pchw == 13)
			pchw++;

		// Read the string to be rendered.
		OLECHAR * pchwStr = rgchwHello;
		while (*pchw != 0 && *pchw != 10 && *pchw != 13)
			*pchwStr++ = *pchw++;
		*pchwStr = 0;	// zero termination

		// Create a writing system that uses the given font.
		ILgEncodingFactoryPtr qencf;
		qencf.CreateInstance(CLSID_LgEncodingFactory);
		ILgEncodingPtr qenc;
		qenc.CreateInstance(CLSID_LgEncoding);
		enc = StrUtil::ParseEnc("HGR99");
		qenc->put_Encoding(enc);
		ILgWritingSystemPtr qws;
		qws.CreateInstance(CLSID_LgWritingSystem);
		CheckHr(qenc->SetDefaultWs(qws));
		CheckHr(qencf->AddEngine(qenc));
		SmartBstr sbstrWsName(L"Hello Graphite");
		CheckHr(qws->put_Name(enc, sbstrWsName));
		ITsStringPtr qtssDescr;
		SmartBstr sbstrDescr(L"Default writing system for Hello Graphite");
		CheckHr(qtsf->MakeString(sbstrDescr, 0, &qtssDescr));
		CheckHr(qws->put_Description(enc, qtssDescr));

		//	Create a Graphite renderer
		IRenderEnginePtr qreneng;
		qreneng.CreateInstance(CLSID_GrEngine);
		CheckHr(qws->SetRenderer(qreneng));
		//	Turn on the transduction log
		ITraceControl * pTraceCtrl;
		CheckHr(qreneng->QueryInterface(IID_ITraceControl, (void **)&pTraceCtrl));
		CheckHr(pTraceCtrl->SetTracing(true));
		pTraceCtrl->Release();
		// Set the default font.
		SmartBstr sbstrFont(rgchwFont);
		CheckHr(qws->put_DefaultSerif(sbstrFont));
		CheckHr(qws->put_DefaultSansSerif(sbstrFont));
		CheckHr(qws->put_DefaultMonospace(sbstrFont));
		// Initialize the renderer.
		StrUni stuInit(rgchwFont);
		StrUni stuFontVar(rgchwGrFeat);
		stuInit += L":";
		stuInit += stuFontVar;
		ISimpleInit * pInit;
		CheckHr(qreneng->QueryInterface(IID_ISimpleInit, (void **)&pInit));
		CheckHr(pInit->InitNew((BYTE *)stuInit.Chars(),
			(stuInit.Length() * isizeof(OLECHAR))));
		pInit->Release();

		delete[] pswzData;
		delete[] pszData;
	}

	CheckHr(qtsf->MakeStringRgch(rgchwHello, wcslen(rgchwHello), enc, pptss));

	m_nSize = nSize;
}

/***********************************************************************************************
	HgVc methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP HgVc::Display(IVwEnv* pvwenv, HVO hvo, int frag)
{
	switch(frag)
	{
	default:
		Assert(false);
		break;
	case kfrText:
		CheckHr(pvwenv->put_IntProperty(ktptFontSize, ktpvMilliPoint, m_mpSize));
		CheckHr(pvwenv->AddStringProp(ktagProp));
		break;
	}
	return S_OK;
}
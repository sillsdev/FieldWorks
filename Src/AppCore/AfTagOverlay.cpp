/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfTagOverlay.cpp
Responsibility: Darrell Zook
Last reviewed: Not yet.

Description:
	This file contains class definitions for the following classes:
		AfTagOverlayGlobals
		AfTagOverlayPalette : AfWnd
		AfTagOverlayTool : AfWnd
		AfTagTypeAheadEdit : AfWnd
		TlsOptDlgOvr : AfDialogView
		NewOverlayDlg : AfDialog
		AdvOverlayDlg : AfDialog
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

// AtlAxWinInit is implemented in Atl.dll
#pragma comment(lib, "atl.lib")
#define _ATL_APARTMENT_THREADED
#undef _ATL_FREE_THREADED
#include <atldef.h>
#define _ATL_DLL_IMPL
#include <atliface.h>
#include <mshtml.h>
#include <windowsx.h>

#undef THIS_FILE
DEFINE_THIS_FILE

const achar * kpszTagOverlaySubKey = _T("Overlays");
const int kidOverlayIdleTimer = 15;
const int knOverlayIdle = 250; // 1/4 second.
const int kidOverlayTagSettings = 16;
const int knOverlayTagChange = 100; // 1/10 second.

bool AfTagTypeAheadEdit::s_fExtraBackspace;

extern uint s_wmActivate;

BEGIN_CMD_MAP(AfTagOverlayTool)
	ON_CID_ME(kcidOvlyDispName, &AfTagOverlayTool::CmdChangeDisplay,
		&AfTagOverlayTool::CmsChangeDisplay)
	ON_CID_ME(kcidOvlyDispAbbrev, &AfTagOverlayTool::CmdChangeDisplay,
		&AfTagOverlayTool::CmsChangeDisplay)
	ON_CID_ME(kcidOvlyDispBoth, &AfTagOverlayTool::CmdChangeDisplay,
		&AfTagOverlayTool::CmsChangeDisplay)
	ON_CID_ME(kcidOvlyShowRecent, &AfTagOverlayTool::CmdShowRecent,
		&AfTagOverlayTool::CmsShowRecent)
	ON_CID_ME(kcidOvlyExcludeTag, &AfTagOverlayTool::CmdModifyTag,
		&AfTagOverlayTool::CmsModifyTag)
	ON_CID_ME(kcidOvlyIncludeSubtags, &AfTagOverlayTool::CmdModifyTag,
		&AfTagOverlayTool::CmsModifyTag)
	ON_CID_ME(kcidOvlyExcludeSubtags, &AfTagOverlayTool::CmdModifyTag,
		&AfTagOverlayTool::CmsModifyTag)
	ON_CID_ME(kcidOvlyHideExclude, &AfTagOverlayTool::CmdHideExclude,
		&AfTagOverlayTool::CmsHideExclude)
	// TODO: Change this to kcidViewOlaysConfig.
	ON_CID_ME(kcidOvlyConfigure, &AfTagOverlayTool::CmdConfigure, NULL)
	ON_CID_ME(kcidOvlyHelp, &AfTagOverlayTool::CmdShowHelp, NULL)
END_CMD_MAP_NIL()

AfTagOverlayGlobals g_tog;

static const int s_rgdyptSize[] =
{
	8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72
};

typedef enum
{
	//:> This number is used as an index, so it cannot change unless you change
	//:> the kridOverlayToolMenu menu in AfTagOverlay.rc.
	kopmOptions = 0,
} OverlayPopupMenu;


/***********************************************************************************************
	AfTagOverlayGlobals methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfTagOverlayGlobals::AfTagOverlayGlobals()
{
	m_himl = NULL;
	m_fof = kgrfofDefault;
	m_stuFont = L"Arial Narrow";
	m_dympFont = 9000;
	m_ctagMax = 1000;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfTagOverlayGlobals::~AfTagOverlayGlobals()
{
	if (m_himl)
	{
		AfGdi::ImageList_Destroy(m_himl);
		m_himl = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	This allows the same imagelist to be used for all the different overlay dialog boxes and
	windows.
----------------------------------------------------------------------------------------------*/
HIMAGELIST AfTagOverlayGlobals::GetImageList()
{
	if (m_himl)
		return m_himl;

	m_himl = AfGdi::ImageList_Create(16, 16, ILC_COLORDDB | ILC_MASK, 0, 0);
	HBITMAP hbmp = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(),
		MAKEINTRESOURCE(kridTagBtnImages));
	if (!hbmp)
		ThrowHr(WarnHr(E_FAIL));

	// WARNING: Do not change the color in the next line.
	if (::ImageList_AddMasked(m_himl, hbmp, kclrPink) == -1)
		ThrowHr(WarnHr(E_FAIL));
	AfGdi::DeleteObjectBitmap(hbmp);

	return m_himl;
}


/*----------------------------------------------------------------------------------------------
	This method creates a new overlay in the database based on pvo. Then it adds the
	information into the database for the overlay and each tag belonging to the overlay.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayGlobals::SaveOverlay(AfLpInfo * plpi, IVwOverlay * pvo)
{
	AssertPtr(plpi);
	AssertPtr(pvo);

	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);

	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stuQuery;
	ComBool fIsNull;
	ComBool fMoreRows;

	try
	{
		pdbi->GetDbAccess(&qode);
		CheckHr(qode->CreateCommand(&qodc));

		SmartBstr sbstrName;
		GUID uid;
		HVO psslId;
		CheckHr(WarnHr(pvo->get_Name(&sbstrName)));
		CheckHr(WarnHr(pvo->get_Guid((OLECHAR *)&uid)));
		CheckHr(WarnHr(pvo->get_PossListId(&psslId)));

		// Create a temporary cache for saving to the database.
		IVwOleDbDaPtr qda;
		qda.CreateInstance(CLSID_VwOleDbDa);
		// Gather up relevant interfaces needed here.
		ISetupVwOleDbDaPtr qods;
		CheckHr(qda->QueryInterface(IID_ISetupVwOleDbDa, (void**)&qods));
		Assert(qods);
		ISilDataAccessPtr qsda;
		CheckHr(qda->QueryInterface(IID_ISilDataAccess, (void**)&qsda));
		Assert(qsda);

		IOleDbEncapPtr qode;
		IFwMetaDataCachePtr qmdc;
		ILgWritingSystemFactoryPtr qwsf;
		plpi->GetDbInfo()->GetDbAccess(&qode);
		plpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
		plpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
		qods->Init(qode, qmdc, qwsf, NULL);

		// Create a new CmOverlay.
		HVO hvoOverlay;
		qsda->MakeNewObject(kclidCmOverlay, plpi->GetLpId(), kflidLangProject_Overlays,
			-1, &hvoOverlay);
		stuQuery.Format(L"update CmOverlay set Name = ?, PossList = %d where id = %d",
			psslId, hvoOverlay);
		StrUni stuName(sbstrName.Chars());
		StrUtil::NormalizeStrUni(stuName, UNORM_NFD);
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)stuName.Chars(), stuName.Length() * 2));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
		stuQuery.Clear();
		qodc.Clear();

		// Save the tags in the overlay.
		int ctag;
		CheckHr(WarnHr(pvo->get_CTags(&ctag)));
		if (ctag)
		{
			HVO hvoPss;
			COLORREF clrFore;
			COLORREF clrBack;
			COLORREF clrUnder;
			int unt;
			ComBool fHidden;
			for (int itag = 0; itag < ctag; itag++)
			{
				CheckHr(WarnHr(pvo->GetDbTagInfo(itag, &hvoPss, &clrFore, &clrBack, &clrUnder,
					&unt, &fHidden, (OLECHAR *)&uid)));
				stuQuery.FormatAppend(
					L"INSERT INTO CmOverlay_PossItems (Src,Dst) VALUES (%d,%d);%n",
					hvoOverlay, hvoPss);
			}
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
		}
	}
	catch (...)
	{
		return false;
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Update the global overlay values. These don't actually change the existing overlays, but
	when the overlay dialog closes after changing these, all the existing overlay windows will
	be destroyed and recreated, so at that point, the new values will be used.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayGlobals::SetGlobalOverlayValues(int fof, wchar * pszFont, int dympFont,
	int ctagMax)
{
	AssertPsz(pszFont);
	m_fof = fof;
	m_stuFont = pszFont;
	m_dympFont = dympFont;
	m_ctagMax = ctagMax;

	// Save global overlay settings.
	FwSettings * pfs = AfApp::GetSettings();
	AssertPtr(pfs);

	pfs->SetDword(kpszTagOverlaySubKey, _T("View Flags"), m_fof);
	StrApp str(m_stuFont);
	pfs->SetString(kpszTagOverlaySubKey, _T("Font"), str);
	pfs->SetDword(kpszTagOverlaySubKey, _T("Font Size"), m_dympFont);
	pfs->SetDword(kpszTagOverlaySubKey, _T("Max Tags"), m_ctagMax);
}


/*----------------------------------------------------------------------------------------------
	Put the existing global overlay values into the overlay.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayGlobals::GetGlobalOverlayValues(int & fof, StrUni & stuFont, int & dympFont,
	int & ctagMax)
{
	if (!m_fLoadedGlobals)
	{
		// Load global overlay settings.
		FwSettings * pfs = AfApp::GetSettings();
		AssertPtr(pfs);

		DWORD dwT;
		if (pfs->GetDword(kpszTagOverlaySubKey, _T("View Flags"), &dwT))
			m_fof = (int)dwT;
		StrApp str;
		if (pfs->GetString(kpszTagOverlaySubKey, _T("Font"), str))
			m_stuFont = str;
		if (pfs->GetDword(kpszTagOverlaySubKey, _T("Font Size"), &dwT))
			m_dympFont = (int)dwT;
		if (pfs->GetDword(kpszTagOverlaySubKey, _T("Max Tags"), &dwT))
			m_ctagMax = (int)dwT;

		m_fLoadedGlobals = true;
	}

	fof = m_fof;
	stuFont = m_stuFont;
	dympFont = m_dympFont;
	ctagMax = m_ctagMax;
}


/***********************************************************************************************
	AfTagOverlayTool methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
AfTagOverlayTool::AfTagOverlayTool()
{
	m_rid = kridOverlayChsrDlg;
	m_pszHelpUrl = _T("Advanced_Tasks/Tagging_Text/Tag_text.htm");

	m_hwndTab = NULL;
	m_hwndGrip = NULL;
	m_hwndTool = NULL;
	m_hwndHelp = NULL;

	m_himlCold = NULL;
	m_himlHot = NULL;
	m_hiconHideExcluded = NULL;
	m_iCurTabSel = kitabTree;
	m_fShowHelp = false;
	m_pnt = kpntName;
	m_dxpHelp = 0;
	m_fEnabled = true;
	m_fHideExcluded = false;
	m_fUpdating = false;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfTagOverlayTool::~AfTagOverlayTool()
{
	m_hwndTab = NULL;
	if (m_hwndHelp)
	{
		::DestroyWindow(m_hwndHelp);
		m_hwndHelp = NULL;
	}
	// Child windows do not need to be destroyed:  m_hwndGrip, m_hwndTool
	m_hwndGrip = NULL;
	m_hwndTool = NULL;

	if (m_hiconHideExcluded)
	{
		::DestroyIcon(m_hiconHideExcluded);
		m_hiconHideExcluded = NULL;
	}
	if (m_himlCold)
	{
		AfGdi::ImageList_Destroy(m_himlCold);
		m_himlCold = NULL;
	}
	if (m_himlHot)
	{
		AfGdi::ImageList_Destroy(m_himlHot);
		m_himlHot = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Create the main tool window.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::Create(AfOverlayListBar * polb, HWND hwndPar, AfLpInfo * plpi,
	IVwOverlay * pvo)
{
	Assert(polb);
	Assert(hwndPar);
	AssertPtr(plpi);
	AssertPtr(pvo);

	m_qolb = polb;
	m_qlpi = plpi;
	m_qvo = pvo;

	DoModeless(hwndPar);
}


/*----------------------------------------------------------------------------------------------
	Create the child windows after the main tool window has been created.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	SmartBstr sbstr;
	CheckHr(m_qvo->get_Name(&sbstr));
	sbstr.Append(L" ");
	StrApp str(sbstr.Chars());
	str.AppendLoad(kstidTagOverlay);
	::SetWindowText(m_hwnd, str.Chars());

	// Create the gripper control.
	Assert(m_hwndGrip == NULL);
	m_hwndGrip = ::CreateWindow(_T("SCROLLBAR"), NULL, WS_CHILD | WS_VISIBLE | SBS_SIZEGRIP |
		SBS_SIZEBOX | SBS_SIZEBOXBOTTOMRIGHTALIGN, 0, 0, 0, 0,
		m_hwnd, NULL, NULL, NULL);

	m_hwndTab = ::GetDlgItem(m_hwnd, kctidOverlayTab);
	DWORD dwStyle = ::GetWindowLong(m_hwndTab, GWL_STYLE);
	::SetWindowLong(m_hwndTab, GWL_STYLE, dwStyle | WS_CLIPCHILDREN);
	OverlayChsrTabPtr qoctab;
	qoctab.Create();
	qoctab->SubclassTab(m_hwndTab);

	AfTagTypeAheadEditPtr qttae;
	qttae.Create();
	qttae->SubclassEdit(::GetDlgItem(m_hwnd, kctidEditTag));

	TCITEM tci = { TCIF_TEXT };
	StrApp strTab(kstidTagList);
	tci.pszText = const_cast<achar *>(strTab.Chars());
	TabCtrl_InsertItem(m_hwndTab, 0, &tci);
	strTab.Load(kstidTagPalette);
	tci.pszText = const_cast<achar *>(strTab.Chars());
	TabCtrl_InsertItem(m_hwndTab, 1, &tci);

	m_qbtnOptions.Create();
	m_qbtnOptions->SubclassButton(m_hwnd, kctidOverlayOptions, kbtPopMenu, NULL, 0);

	// Set the icon for the Show Excluded Tags button.
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidHideExcludedTags);
	if (m_hiconHideExcluded)
		::DestroyIcon(m_hiconHideExcluded);
	m_hiconHideExcluded = ImageList_GetIcon(g_tog.GetImageList(), 4, ILD_NORMAL);
	::SendMessage(hwnd, BM_SETIMAGE, IMAGE_ICON, (LPARAM)m_hiconHideExcluded);

	m_qtotr.Create();
	m_qtotr->Create(this, kctidOverlayTree);

	m_qtopl.Create();
	m_qtopl->Create(this, kctidToolTagList);
	::SetWindowPos(m_qtopl->Hwnd(), ::GetDlgItem(m_hwnd, kctidEditTag), 0, 0, 0, 0,
		SWP_NOSIZE | SWP_NOMOVE);

	// Choose a default position in the middle of the parent window.
	// This sizes the window to the smallest possible size.
	Rect rc(0);
	OnSizing(0, &rc);
	Rect rcParent;
	::GetWindowRect(::GetParent(m_hwnd), &rcParent);
	::MoveWindow(m_hwnd, rcParent.left + (rcParent.Width() - rc.Width()) / 2,
		rcParent.top + (rcParent.Height() - rc.Height()) / 2, rc.Width(), rc.Height(), true);

	LoadSettings(kpszTagOverlaySubKey);

	/*GetClientRect(rc);
	::CreateWindow("SCROLLBAR", "", WS_CHILD | WS_VISIBLE | SBS_SIZEBOX | SBS_SIZEGRIP |
		SBS_SIZEBOXBOTTOMRIGHTALIGN, 0, 0, rc.Width(), rc.Height(), m_hwnd,
		(HMENU)kctidGripper, NULL, NULL);*/

	TabCtrl_SetCurSel(m_hwndTab, m_iCurTabSel);
	SelectTab(m_iCurTabSel);
	// This has to be after the window has first been sized.
	UpdateStatus(true);

	::ShowWindow(m_hwnd, SW_SHOW);
	::UpdateWindow(m_hwnd);

	return false; // We just set the focus, so the system shouldn't do it.
}


/*----------------------------------------------------------------------------------------------
	Reload the possibility and overlay information.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::LoadOverlay()
{
	if (m_qpli)
	{
		// Stop the timer while we're reloading everything.
		::KillTimer(m_hwnd, kidOverlayIdleTimer);
		// Remove ourselves from the current possibility list notification in case it changes.
		m_qpli->RemoveNotify(this);
	}

	int ivo = m_qlpi->GetOverlayIndex(this);
	m_qlpi->GetOverlay(ivo, &m_qvo);

	HVO psslId;
	CheckHr(m_qvo->get_PossListId(&psslId));
	m_qlpi->LoadPossList(psslId, m_qlpi->GetPsslWsFromDb(psslId), &m_qpli);

	m_qpli->AddNotify(this);

	ChangeDisplayOption(m_qpli->GetDisplayOption());

	// Get the vector of the possibility tags in the overlay.
	int ctag;
	COLORREF clrFore;
	COLORREF clrBack;
	COLORREF clrUnder;
	int unt;
	ComBool fHidden;
	GUID uid;
	CheckHr(m_qvo->get_CTags(&ctag));
	m_vhvoTag.Resize(ctag);
	for (int itag = 0; itag < ctag; itag++)
	{
		CheckHr(m_qvo->GetDbTagInfo(itag, &m_vhvoTag[itag], &clrFore, &clrBack, &clrUnder,
			&unt, &fHidden, (OLECHAR *)&uid));
	}

	// Populate the corresponding vector of indexes into the possibility list.
	m_vipssVisible.Resize(ctag);
	int cpii = m_qpli->GetCount();
	int itag, ipii;
	for (ipii = 0, itag = 0; ipii < cpii && itag < ctag; ipii++)
	{
		PossItemInfo * ppii = m_qpli->GetPssFromIndex(ipii);
		AssertPtr(ppii);
		if (TagIndexFromHvo(ppii->GetPssId()) >= 0)
			m_vipssVisible[itag++] = ipii;
		else if (m_fHideExcluded && ipii == m_ipssSelected)
			m_ipssSelected = 0;
	}
	Assert(itag == ctag);

	m_vfTagInSel.Resize(ctag);

	// Reset any caches that the individual controls are holding on to so we don't
	// have problems trying to draw a previous item that doesn't exist anymore.
	m_qtotr->Reset();
	m_qtopl->Reset();

	// Load the possibilities items into the tree and the palette.
	m_qtotr->AddPossibilities();
	m_qtopl->AddPossibilities();

	::SetTimer(m_hwnd, kidOverlayIdleTimer, knOverlayIdle, NULL);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_ACTIVATE:
		{
			HWND hwndNew = (HWND)lp;
			HWND hwndOwner = ::GetWindow(m_hwnd, GW_OWNER);
			HWND hwndNewOwner = ::GetWindow(hwndNew, GW_OWNER);
			if (hwndOwner != hwndNew &&
				(!hwndNew || (hwndOwner != hwndNewOwner && m_hwnd != hwndNewOwner)))
			{
				// This makes sure that:
				//   1) a new window is getting focus,
				//   2) the new window is not the owner of the tool window, and
				//   3) the owner of the new window is not the owner of the tool window.
				//   4) the owner of the new window is not the tool window.
				// Case 3 would be if two overlay tool windows were open, and the
				// user clicked from one tool window to the other tool window.
				AfMainWnd * pafw = dynamic_cast<AfMainWnd *>(AfWnd::GetAfWnd(hwndOwner));
				if (pafw && (LOWORD(wp) == WA_INACTIVE || HIWORD(wp)))
					::PostMessage(pafw->Hwnd(), s_wmActivate, 0, (LPARAM)hwndNew);
			}
		}
		break;

	case WM_DESTROY:
		SaveSettings(kpszTagOverlaySubKey);
		m_qweb2.Clear();
		::DestroyWindow(m_hwndHelp);
		m_qpwe.Clear();
		break;

	case WM_SETFOCUS:
		::SetFocus(::GetDlgItem(m_hwnd, kctidEditTag));
		break;

	case WM_TIMER:
		return OnTimer(wp);

	case WM_SIZING:
		return OnSizing(wp, (RECT *)lp);
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Adjust the sizes of all the child windows.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::OnSize(int wst, int dxp, int dyp)
{
	if (m_dxpOldLeft == 0)
		m_dxpOldLeft = dxp - m_dxpHelp;

	Rect rc;
	uint grfnMove = SWP_NOZORDER | SWP_NOSIZE;
	uint grfnSize = SWP_NOZORDER | SWP_NOMOVE;

	int dxpLeft = dxp;
	if (m_fShowHelp)
	{
		m_dxpHelp = dxp - m_dxpOldLeft;
		dxpLeft -= m_dxpHelp;
	}
	else
	{
		m_dxpOldLeft = dxp;
	}

	// Get the client size (in screen coordinates).
	Rect rcClient;
	GetClientRect(rcClient);
	::MapWindowPoints(m_hwnd, NULL, (POINT *)&rcClient, 2);

	// Get the height of the Options button.
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidOverlayOptions), &rc);
	int dypOptions = rc.Height();

	::GetWindowRect(m_hwndTab, &rc);
	int dzpGap = rc.left - rcClient.left;

	// Move the help and toolbar windows. These need to be moved before the
	// tab is resized because of a weird refresh bug when the tab overlaps part
	// of the help window before the help window is moved over.
	if (m_fShowHelp)
	{
		const int kypToolbar = 5;
		const int kdypToolbar = 46;
		::MoveWindow(m_hwndTool, dxp - m_dxpHelp, kypToolbar, m_dxpHelp,
			kdypToolbar - kypToolbar, true);
		::MoveWindow(m_hwndHelp, dxp - m_dxpHelp, kdypToolbar, m_dxpHelp,
			dyp - (dzpGap * 2) - kdypToolbar - 20, true);
	}

	// Resize the tab control.
	::SetWindowPos(m_hwndTab, NULL, 0, 0, dxpLeft - (dzpGap * 2),
		dyp - dypOptions - (dzpGap * 4), grfnSize);

	// Resize the tree and palette controls.
	Rect rcTree;
	::GetClientRect(m_hwndTab, &rcTree);
	TabCtrl_AdjustRect(m_hwndTab, false, &rcTree);
	::MapWindowPoints(m_hwndTab, m_hwnd, (POINT *)&rcTree, 2);
	rcTree.top += 28;
	rcTree.bottom -= 2;
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidOverlayTree);
	::SetWindowPos(hwnd, NULL, rcTree.left, rcTree.top, rcTree.Width(), rcTree.Height(),
		SWP_NOZORDER);
	if (m_qtopl)
	{
		::SetWindowPos(m_qtopl->Hwnd(), NULL, rcTree.left, rcTree.top, rcTree.Width(),
			rcTree.Height(), SWP_NOZORDER);
	}

	// Resize the editbox control.
	hwnd = ::GetDlgItem(m_hwnd, kctidEditTag);
	::GetWindowRect(hwnd, &rc);
	int xp = rcTree.right - (rc.left - rcClient.left);
	::SetWindowPos(hwnd, NULL, 0, 0, xp, rc.Height(), grfnSize);

	// Move the three buttons.
	hwnd = ::GetDlgItem(m_hwnd, kctidOverlayOptions);
	::GetWindowRect(hwnd, &rc);
	::SetWindowPos(hwnd, NULL, dzpGap, dyp - dzpGap - rc.Height(), 0, 0, grfnMove);
	::InvalidateRect(hwnd, NULL, true);

	hwnd = ::GetDlgItem(m_hwnd, kctidHideExcludedTags);
	::SetWindowPos(hwnd, NULL, dzpGap * 2 + rc.Width(),
		dyp - dzpGap - rc.Height(), 0, 0, grfnMove);
	::InvalidateRect(hwnd, NULL, true);

	hwnd = ::GetDlgItem(m_hwnd, kctidOverlayToggleHelp);
	::GetWindowRect(hwnd, &rc);
	::SetWindowPos(hwnd, NULL, dxpLeft - dzpGap - rc.Width(),
		dyp - (dzpGap * 2) - rc.Height() - 20, 0, 0, grfnMove);
	::InvalidateRect(hwnd, NULL, true);

	// Move the gripper to the bottom right.
	::GetWindowRect(m_hwndGrip, &rc);
	::MoveWindow(m_hwndGrip, dxp - rc.Width(), dyp - rc.Height(), rc.Width(),
		rc.Height(), true);
	::InvalidateRect(m_hwndGrip, NULL, true);

	return SuperClass::OnSize(wst, dxp, dyp);
}


/*----------------------------------------------------------------------------------------------
	Make sure we don't resize the window too small.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::OnSizing(int wst, RECT * prc)
{
	AssertPtr(prc);

	if (!m_qtopl)
		return false;

	Rect rcClient;
	GetClientRect(rcClient);
	int dxpLeft = prc->right - prc->left;
	int dxpMin = 0;

	// Figure out the minimum size of the palette window.
	int dxpPaletteMin = m_qtopl->GetMinWidth();

	// Change the client coordinates to screen coordinates. This is used to
	// find the horizontal tree offset from the left side of the dialog.
	::MapWindowPoints(m_hwnd, NULL, (POINT *)&rcClient, 2);
	Rect rc;
	::GetWindowRect(m_hwndTab, &rc);
	int dzpGap = rc.left - rcClient.left;

	rc.Set(0, 0, dxpPaletteMin, 0);
	TabCtrl_AdjustRect(m_hwndTab, true, &rc);
	// After the next line, dxpMin will be equal to the tab width.
	dxpMin = rc.Width() + ::GetSystemMetrics(SM_CXEDGE) * 2;
	// After the next line, dxpMin will be equal to the client width of the tool.
	dxpMin += (dzpGap * 2);
	// After the next line, dxpMin will be equal to the window width of the tool.
	dxpMin += ::GetSystemMetrics(SM_CXSIZEFRAME) * 2;

	if (dxpLeft < dxpMin)
		dxpLeft = dxpMin;
	int dxpTool = dxpLeft;

	if (m_fShowHelp)
	{
		int dxpLeftOld = dxpLeft - m_dxpHelp;
		if (dxpLeftOld < dxpMin)
		{
			// We need to modify either the width of the left side or the help window.
			if (wst == 0)
			{
				// This is the special case where this method gets called from code to
				// force all the windows to layout properly.
				// In this case, we want to adjust the width of the left side without
				// modifying the width of the help window.
				m_dxpOldLeft += (dxpMin - dxpLeftOld); // Adjust the width of the left side.
				dxpTool += (dxpMin - dxpLeftOld); // Adjust the total width.
			}
			else
			{
				m_dxpHelp -= (dxpMin - dxpLeftOld); // Adjust the width of the help window.
			}
		}
		if (dxpTool < dxpMin + 50)
			dxpTool = dxpMin + 50;
	}

	// Adjust the window as needed.
	if (wst == WMSZ_TOPLEFT || wst == WMSZ_LEFT || wst == WMSZ_BOTTOMLEFT)
		prc->left = prc->right - dxpTool;
	else
		prc->right = prc->left + dxpTool;
	if (prc->bottom - prc->top < kdypDefHeight)
	{
		if (wst == WMSZ_TOPLEFT || wst == WMSZ_TOP || wst == WMSZ_TOPRIGHT)
			prc->top = prc->bottom - kdypDefHeight;
		else
			prc->bottom = prc->top + kdypDefHeight;
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::OnCommand(int cid, int nc, HWND hctl)
{
	switch (cid)
	{
	case kctidEditTag:
		if (nc == EN_UPDATE)
		{
			// m_fUpdating is required because the FindTag call will update the
			// edit control, and we don't want to get into an infinite loop here.
			if (!m_fUpdating)
			{
				// Let the overlay list window know that the user typed a key in the
				// look-ahead edit box.
				m_fUpdating = true;
				Assert(cid == kctidEditTag);
				AssertPtr(m_qtopl);
				achar rgchBuffer[MAX_PATH];
				::SendMessage(hctl, WM_GETTEXT, MAX_PATH, (LPARAM)rgchBuffer);
				if (AfTagTypeAheadEdit::s_fExtraBackspace && *rgchBuffer)
					rgchBuffer[StrLen(rgchBuffer) - 1] = 0;
				StrUni stu(rgchBuffer);
				stu.ToLower();
				SelectPss(hctl, stu.Chars());
				m_fUpdating = false;
				return true;
			}
		}
		break;

	case kctidOverlayOptions:
		if (nc == BN_CLICKED)
		{
			// Show the Options popup menu.
			Rect rc;
			::GetWindowRect(m_qbtnOptions->Hwnd(), &rc);
			TPMPARAMS tpm = { isizeof(tpm) };
			tpm.rcExclude = rc;
			HMENU hmenuPopup = ::LoadMenu(ModuleEntry::GetModuleHandle(),
				MAKEINTRESOURCE(kridOverlayToolMenu));
			::TrackPopupMenuEx(::GetSubMenu(hmenuPopup, kopmOptions),
				TPM_LEFTALIGN | TPM_TOPALIGN | TPM_VERTICAL | TPM_RIGHTBUTTON,
				rc.left, rc.bottom, m_hwnd, &tpm);
			::DestroyMenu(hmenuPopup);
		}
		break;

	default:
		break;
	}

	return SuperClass::OnCommand(cid, nc, hctl);
}


/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case TCN_SELCHANGE:
		SelectTab(TabCtrl_GetCurSel(pnmh->hwndFrom));
		return true;

	/*case NM_RCLICK:
		{
			Point pt;
			::GetCursorPos(&pt);

			TVHITTESTINFO tvhti;
			::GetCursorPos(&tvhti.pt);
			::ScreenToClient(m_rghwndTree[kiChoiceList], &tvhti.pt);

			if (TreeView_HitTest(m_rghwndTree[kiChoiceList], &tvhti))
			{
				if (tvhti.hItem)
				{
					// Select the item that was right clicked on
					TreeView_SelectItem(m_rghwndTree[kiChoiceList], tvhti.hItem);
				}
			}
			ContextMenu(m_hwnd, pt);
			return true;
		}*/

	/*case TVN_BEGINDRAG:
		if (pnmh->hwndFrom == m_rghwndTree[kiChoiceList])
			return m_plddDragDrop.BeginDrag(pnmh);
		break;*/

	case BN_CLICKED:
		switch (pnmh->idFrom)
		{
		case kctidOverlayToggleHelp:
			ShowHelp(!m_fShowHelp);
			return true;

		case kctidHideExcludedTags:
			HideExcludedTags(!m_fHideExcluded);
			return true;

		case kcidPossBack:
			CheckHr(m_qweb2->GoBack());
			break;

		case kcidPossForward:
			CheckHr(m_qweb2->GoForward());
			break;

		case kcidPossPrint:
			{
				// Print contents of WebBrowser control.
				IDispatchPtr qdisp;
				ComSmartPtr<IOleCommandTarget> qoct;
				CheckHr(m_qweb2->get_Document(&qdisp));
				CheckHr(qdisp->QueryInterface(IID_IOleCommandTarget, (void **)&qoct));
				qoct->Exec(NULL, OLECMDID_PRINT, OLECMDEXECOPT_DODEFAULT, NULL, NULL);
			}
			return true;
		}
		break;

	default:
		break;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Handle timer messages.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::OnTimer(int nId)
{
	AfApp::Papp()->SuppressIdle();

	// If the window is not visible, don't do any further processing.
	if (!::IsWindowVisible(m_hwnd))
		return true;

	if (nId == kidOverlayIdleTimer)
		UpdateStatus();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Clear our smart window pointers.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::OnReleasePtr()
{
	m_qpli->RemoveNotify(this);

	m_qtotr.Clear();
	m_qtopl.Clear();
	m_qbtnOptions.Clear();
	m_qlpi.Clear();
	m_qolb.Clear();
	m_qvo.Clear();
	m_qpli.Clear();
	m_qpwe.Clear();
	m_qweb2.Clear();
}


/*----------------------------------------------------------------------------------------------
	Show help information for the requested control.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::OnHelpInfo(HELPINFO * phi)
{
	AssertPtr(phi);

	if (phi->iCtrlId == kctidOverlayTab)
	{
		// See which tab the user clicked on.
		TCHITTESTINFO thti;
		thti.pt = phi->MousePos;
		::ScreenToClient(m_hwndTab, &thti.pt);
		int itab = TabCtrl_HitTest(m_hwndTab, &thti);
		if (itab == -1)
			itab = m_iCurTabSel; // Use the current tab if the user didn't click on a tab.
		if (itab == kitabTree)
			phi->iCtrlId = kctidOverlayTabList;
		else if (itab == kitabPalette)
			phi->iCtrlId = kctidOverlayTabPalette;
		else
			return false;
	}

	return SuperClass::OnHelpInfo(phi);
}


/*----------------------------------------------------------------------------------------------
	The Cancel button was pushed.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::OnCancel()
{
	if (m_qolb)
		m_qolb->HideOverlay(this);
	return SuperClass::OnCancel();
}


/*----------------------------------------------------------------------------------------------
	This method updates the tag that is currently selected and redraws the child controls as
	nneded.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::SetSelectedPss(int ipss, bool fUpdateHelp)
{
	if ((uint)ipss >= (uint)m_qpli->GetCount())
		return;

	m_ipssSelected = ipss;

	// Update the text in the type-ahead edit box.
	UpdateEditText(ipss);

	m_qtopl->OnSelectPss(ipss);
	m_qtotr->OnSelectPss(ipss);

	if (fUpdateHelp)
		UpdateHelpWindow();
}


/*----------------------------------------------------------------------------------------------
	Update the type-ahead edit box with the abbreviation and text of the selected tag.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::UpdateEditText(int ipss)
{
	AssertPtr(m_qpli.Ptr());
	StrApp str;
	if (m_qpli->GetCount() > 0)
	{
		PossItemInfo * ppii = m_qpli->GetPssFromIndex(ipss);
		AssertPtr(ppii);
		StrUni stu;
		ppii->GetName(stu, m_pnt);
		str = stu;
	}
	m_fUpdating = true;
	HWND hwndEdit = ::GetDlgItem(m_hwnd, kctidEditTag);
	::SendMessage(hwndEdit, WM_SETTEXT, 0, (LPARAM)str.Chars());
	m_fUpdating = false;
	::SendMessage(hwndEdit, EM_SETSEL, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	This method applies the tag to the selection of the current view window.
	If ipss is -1, the currently selected tag is applied.
	Nothing will happen if the specified tag is not in the overlay.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::ClickPss(int ipss)
{
	if (ipss == -1)
		ipss = m_ipssSelected;
	int itag = TagIndexFromPss(ipss);
	if (itag < 0)
		return;

	m_vfTagInSel[itag] = !m_vfTagInSel[itag];

	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfVwRootSitePtr qvwnd;
	int grfvfs = AfMainWnd::kvfsOverlay;
	if (pafw->GetActiveViewWindow(&qvwnd, &grfvfs) && grfvfs == AfMainWnd::kvfsOverlay)
		qvwnd->ModifyOverlay(m_vfTagInSel[itag], m_qvo, itag);

	// Update the text in the type-ahead edit box.
	UpdateEditText(ipss);

	m_qtotr->OnClickPss(ipss);
	m_qtopl->OnClickPss(ipss);
}


/*----------------------------------------------------------------------------------------------
	If fInsert is true, add the possibility to the overlay (if it doesn't already exist).
	If fInsert is false, remove the possibility from the overlay (if it already exists).
	If fRecursive is true, all subtags of the possibility will also be added or removed.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::ModifyPss(int ipss, bool fInsert, bool fRecursive)
{
	// Add/remove the new items to/from the database.
	AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(GetOverlayIndex());
	HVO hvoPss;
	int itag;

	// Possibilities are added to vhvoPss in the following cases:
	// 1) We're inserting a possibility and it isn't already in the overlay.
	// 2) We're deleting a possibility and it is already in the overlay.
	Vector<HVO> vhvoPss;

	try
	{
		PossItemInfo * ppii = m_qpli->GetPssFromIndex(ipss);
		AssertPtr(ppii);
		hvoPss = ppii->GetPssId();
		itag = TagIndexFromHvo(hvoPss);
		if ((fInsert && itag < 0) || (!fInsert && itag >= 0))
			vhvoPss.Push(hvoPss);
		if (fRecursive)
		{
			// Find the next possibility item that is either at the same level or higher
			// than the initial possibility item. Everything up to (but not including) this
			// item should be added/removed to/from the overlay.
			int nHierLevel = ppii->GetHierLevel();
			PossItemInfo * ppiiLim = m_qpli->GetPssFromIndex(0) + m_qpli->GetCount();
			for (++ppii; ppii < ppiiLim; ppii++)
			{
				if (ppii->GetHierLevel() <= nHierLevel)
					break;

				hvoPss = ppii->GetPssId();
				itag = TagIndexFromHvo(hvoPss);
				if ((fInsert && itag < 0) || (!fInsert && itag >= 0))
					vhvoPss.Push(hvoPss);
			}
		}

		if (vhvoPss.Size() == 0)
		{
			// There's nothing to do, so quit.
			return;
		}

		StrUni stuQuery;
		for (int ipss = vhvoPss.Size(); --ipss >= 0; )
		{
			if (fInsert)
			{
				stuQuery.FormatAppend(
					L"insert into CmOverlay_PossItems (Src, Dst) values (%d, %d)\n",
					aoi.m_hvo, vhvoPss[ipss]);
			}
			else
			{
				stuQuery.FormatAppend(L",%d", vhvoPss[ipss]);
			}
		}

		if (!fInsert)
		{
			// stuQuery starts with an extra ',', so we add 1 to skip it here.
			StrUni stuT;
			stuT.Format(
				L"delete from CmOverlay_PossItems where Src = %d and Dst in (%s)",
				aoi.m_hvo, stuQuery.Chars() + 1);
			stuQuery = stuT;
		}

		AfDbInfo * pdbi = m_qlpi->GetDbInfo();
		AssertPtr(pdbi);
		IOleDbEncapPtr qode;
		pdbi->GetDbAccess(&qode);
		IOleDbCommandPtr qodc;
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
	}
	catch (...)
	{
		// REVIEW DarrellZ: Is there anything we should do here?
	}

	// Force the overlay to reload so the tag order is correct.
	aoi.m_qvo.Clear();
	m_qlpi->GetOverlay(GetOverlayIndex(), &m_qvo);
	LoadOverlay();

	// Notify the main window of the change to the overlay.
	OnChangeOverlay(GetOverlayIndex());
}


/*----------------------------------------------------------------------------------------------
	Find the tag whose name matches closest with prgch. If there aren't any matches, use
	the first tag. Then update the text in hwndEdit to reflect the abbreviation and name
	of the tag. Also, select the non-matched text in hwndEdit. Finally, make sure that the
	selected button is currently showing.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::SelectPss(HWND hwndEdit, const wchar * prgch)
{
	Assert(hwndEdit);
	AssertPsz(prgch);

	int cchMatched = 0;
	StrUni stu;
	int ipssBestMatch = 0;
	int cchPattern = StrLen(prgch);
	if (!cchPattern)
		return;

#ifdef NEW_STUFF_USING_ICU
	Locale loc = m_qlpi->GetLocale(m_qlpi->ActualWs(m_qpli->GetWs()));

	UErrorCode uerr = U_ZERO_ERROR;
	UnicodeString ust(prgch, cchPattern);
	UnicodeString ustPattern;
	Normalizer::normalize(ust, UNORM_NFD, 0, ustPattern, uerr);
	Assert(U_SUCCESS(uerr));
	UCharCharacterIterator itch(prgch, 1); // Temporarily set for next step.
	StringSearch * pss = new StringSearch(ustPattern, itch, loc, NULL, uerr);
	Assert(U_SUCCESS(uerr));
	RuleBasedCollator * rbc = pss->getCollator();
	rbc->setStrength(Collator::SECONDARY); // We want a caseless search.
	pss->setCollator(rbc, uerr);
	Assert(U_SUCCESS(uerr));

	int cpssVisible = GetVisiblePssCount();
	for (int ipssVisible = 0; ipssVisible < cpssVisible; ipssVisible++)
	{
		int ipss;
		PossItemInfo * ppii = GetVisiblePssInfo(ipssVisible, &ipss);
		AssertPtr(ppii);
		ppii->GetName(stu, m_pnt);

		itch.setText(stu.Chars(), stu.Length());
		pss->setText(itch, uerr);
		int ichMatch = pss->first(uerr);
		if (ichMatch == 0)
		{
			ipssBestMatch = ipss;
			cchMatched = cchPattern;
			break;
		}
	}
#else
	if (*prgch != 0)
	{
		int cchFind = StrLen(prgch);
		int cpssVisible = GetVisiblePssCount();
		for (int ipssVisible = 0; ipssVisible < cpssVisible; ipssVisible++)
		{
			int ipss;
			PossItemInfo * ppii = GetVisiblePssInfo(ipssVisible, &ipss);
			AssertPtr(ppii);
			ppii->GetName(stu, m_pnt);

			wchar * prgchFind = (wchar *)prgch - 1;
			wchar * prgchTag = (wchar *)stu.Chars() - 1;
			wchar * prgchFindStop = prgchFind + cchFind + 1;
			while (prgchFind < prgchFindStop && *++prgchFind && *++prgchTag)
			{
				if (*prgchFind != towlower(*prgchTag))
					break;
			}

			int cch = prgchFind - prgch;
			if (cch > cchMatched)
			{
				// This is a better match than what we already had, so remember it.
				ipssBestMatch = ipss;
				cchMatched = prgchFind - prgch;
			}

			if (cch == cchFind)
			{
				// We've found an exact match, so break out of the loop.
				break;
			}
		}
	}
#endif

	SetSelectedPss(ipssBestMatch);
	::SendMessage(hwndEdit, EM_SETSEL, 999, cchMatched);
}


/*----------------------------------------------------------------------------------------------
	Something has changed in the possibility list.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::ListChanged(int nAction, HVO hvoPssl, HVO hvoSrc, HVO hvoDst,
	int ipssSrc, int ipssDst)
{
	switch (nAction)
	{
	case kplnaInsert:
	case kplnaDelete:
	case kplnaModify:
	case kplnaReload:
		LoadOverlay();
		break;

	case kplnaMerged:
		break;

	case kplnaDisplayOption:
		ChangeDisplayOption(m_qpli->GetDisplayOption());
		break;
	}
}


/*----------------------------------------------------------------------------------------------
	Determine what text to show for each button.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::CmdChangeDisplay(Cmd * pcmd)
{
	AssertPtr(pcmd);

	PossNameType pnt = m_pnt;
	if (pcmd->m_cid == kcidOvlyDispName)
		pnt = kpntName;
	else if (pcmd->m_cid == kcidOvlyDispBoth)
		pnt = kpntNameAndAbbrev;
	else if (pcmd->m_cid == kcidOvlyDispAbbrev)
		pnt = kpntAbbreviation;

	if (pnt != m_pnt)
		m_qpli->SetDisplayOption(pnt); // This will update eveything.
	return true;
}


/*----------------------------------------------------------------------------------------------
	Check the current option that says what text is shown for each button.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::CmsChangeDisplay(CmdState & cms)
{
	if (cms.Cid() == kcidOvlyDispName)
		cms.SetCheck(m_pnt == kpntName);
	else if (cms.Cid() == kcidOvlyDispBoth)
		cms.SetCheck(m_pnt == kpntNameAndAbbrev);
	else if (cms.Cid() == kcidOvlyDispAbbrev)
		cms.SetCheck(m_pnt == kpntAbbreviation);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Hide excluded tags.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::CmdHideExclude(Cmd * pcmd)
{
	AssertPtr(pcmd);
	HideExcludedTags(!m_fHideExcluded);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Show the recent tags.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::CmsHideExclude(CmdState & cms)
{
	cms.SetCheck(m_fHideExcluded);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Configure the selected overlay.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::CmdConfigure(Cmd * pcmd)
{
	Assert(pcmd);

	int itag = TagIndexFromPss(m_ipssSelected);
	if (itag < 0)
		itag = 0;
	return OnConfigureTag(GetOverlayIndex(), itag);
}


/*----------------------------------------------------------------------------------------------
	Show help for the dialog.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::CmdShowHelp(Cmd * pcmd)
{
	AssertPtr(pcmd);
	OnHelp();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Toggle the show recent option for this overlay.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::CmdShowRecent(Cmd * pcmd)
{
	Assert(pcmd);
	m_qtopl->SetShowRecent(!m_qtopl->GetShowRecent());
	return true;
}


/*----------------------------------------------------------------------------------------------
	Update the Show Recent command on the popup menu.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::CmsShowRecent(CmdState & cms)
{
	cms.SetCheck(m_qtopl->GetShowRecent());
	cms.Enable(m_iCurTabSel == kitabPalette);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Add or remove the selected tag (and possibly its child tags as wel).
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::CmdModifyTag(Cmd * pcmd)
{
	AssertPtr(pcmd);
	bool fInsert;
	if (pcmd->m_cid == kcidOvlyExcludeTag)
	{
		// If the possibility is currently in the overlay, remove it.
		// Otherwise we want to insert it into the overlay.
		fInsert = (TagIndexFromPss(m_ipssSelected) < 0);
	}
	else
	{
		fInsert = (pcmd->m_cid == kcidOvlyIncludeSubtags);
	}
	bool fRecursive = pcmd->m_cid != kcidOvlyExcludeTag;
	ModifyPss(m_ipssSelected, fInsert, fRecursive);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Update the Include Tag in Overlay command on the popup menu.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::CmsModifyTag(CmdState & cms)
{
	if (m_qpli->GetCount() == 0)
		cms.Enable(false);
	if (cms.Cid() == kcidOvlyExcludeTag)
		cms.SetCheck(TagIndexFromPss(m_ipssSelected) < 0);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Load user interface settings.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::LoadSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);
	Assert(m_hwnd); // The dialog must already exist when this is called.
	AssertPtr(m_qvo);

	HVO pssl;
	CheckHr(WarnHr(m_qvo->get_PossListId(&pssl)));

	StrApp strSubKey(pszRoot);
	// Use GUID instead of HVO for registry key to ensure uniqueness.
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	// Can't Assert directly or it fails in release build.
	bool f;
	f = pdbi->GetGuidFromId(pssl, m_guid);
	Assert(f);
	strSubKey.FormatAppend(_T("\\%g"), &m_guid);

	FwSettings * pfs = AfApp::GetSettings();
	AssertPtr(pfs);
	DWORD dwT = 0;
	if (pfs->GetDword(strSubKey.Chars(), _T("Settings"), &dwT))
	{
		AssertPtr(m_qtopl);
		m_iCurTabSel = dwT & kmaskCurTabSel ? 1 : 0;
		m_fShowHelp = dwT & kmaskShowHelp;
		m_qtopl->SetShowRecent(dwT & kmaskShowRecent);
		HideExcludedTags((dwT & kmaskHideExcluded) != 0);
		// This needs to be after HideExcludedTags, so that the overlay is loaded
		// by the time ShowHelp gets called.
		ShowHelp(m_fShowHelp, true);
	}
	else
	{
		m_qtopl->SetShowRecent(false);
		HideExcludedTags(false);
	}

	// Reset this back to 0 so it gets set for real the next time the window is resized.
	dwT = m_dxpOldLeft;
	m_dxpOldLeft = 0;
	dwT = m_dxpHelp;
	if (pfs->GetDword(strSubKey.Chars(), _T("Help Width"), &dwT))
	{
		m_dxpHelp = dwT;
	}
	else
	{
		Rect rc;
		GetClientRect(rc);
		m_dxpHelp = rc.Width();
	}
	dwT = m_dxpHelp;

	LoadWindowPosition(strSubKey.Chars(), _T("Size"));

	// Load the most recent tags.
	int rgitag[kcRecentOverlayTag];
	for (int itag = 0; itag < kcRecentOverlayTag; itag++)
	{
		DWORD dwT;
		StrAppBufSmall strbs;
		strbs.Format(_T("Tag%d"), itag);
		if (!pfs->GetDword(strSubKey.Chars(), strbs.Chars(), &dwT))
			rgitag[itag] = -1;
		else
			rgitag[itag] = (int)dwT;
	}
	m_qtopl->SetRecentTags(rgitag, isizeof(rgitag) / isizeof(int));

	SuperClass::LoadSettings(pszRoot, fRecursive);
}


/*----------------------------------------------------------------------------------------------
	Save user interface settings.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::SaveSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);
	Assert(m_hwnd); // The dialog must still exist when this is called.
	AssertPtr(m_qvo);

	StrApp strSubKey(pszRoot);
	// Use GUID instead of HVO for registry key to ensure uniqueness.
	// JohnT: we store it in a member variable (set in LoadSettings) rather than computing it
	// here from m_qvo because by the time we're shutting down this window (if shutting down
	// the whole application) we don't have a dbinfo object to retrieve the GUID from.
	strSubKey.FormatAppend(_T("\\%g"), &m_guid);

	FwSettings * pfs = AfApp::GetSettings();
	AssertPtr(pfs);
	AssertPtr(m_qtopl);
	Assert(m_iCurTabSel < 2); // If this ever changes, we need more bits in mask.
	DWORD dwT = 0;
	if (m_iCurTabSel)
		dwT |= kmaskCurTabSel;
	if (m_fShowHelp)
		dwT |= kmaskShowHelp;
	if (m_qtopl->GetShowRecent())
		dwT |= kmaskShowRecent;
	if (m_fHideExcluded)
		dwT |= kmaskHideExcluded;
	pfs->SetDword(strSubKey.Chars(), _T("Settings"), dwT);

	// Save the most recent tags.
	int rgitag[kcRecentOverlayTag];
	m_qtopl->GetRecentTags(rgitag, isizeof(rgitag) / isizeof(int));
	for (int itag = 0; itag < kcRecentOverlayTag && rgitag[itag] != -1; itag++)
	{
		StrAppBufSmall strbs;
		strbs.Format(_T("Tag%d"), itag);
		pfs->SetDword(strSubKey.Chars(), strbs.Chars(), rgitag[itag]);
	}

	pfs->SetDword(strSubKey.Chars(), _T("Help Width"), m_dxpHelp);

	SaveWindowPosition(strSubKey.Chars(), _T("Size"));

	SuperClass::SaveSettings(pszRoot, fRecursive);
}


/*----------------------------------------------------------------------------------------------
	Enable/disable the controls on the top level window.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::EnableWindow(bool fEnable)
{
	::EnableWindow(::GetDlgItem(m_hwnd, kctidOvrTagLabel), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidEditTag), fEnable);

	if (fEnable)
		TreeView_SetBkColor(m_qtotr->Hwnd(), ::GetSysColor(COLOR_WINDOW));
	else
		TreeView_SetBkColor(m_qtotr->Hwnd(), ::GetSysColor(COLOR_3DFACE));
}


/*----------------------------------------------------------------------------------------------
	If fShow is true, all tags in the possibility list will show up in the tree and palette.
	If fShow is false, only the tags in the overlay will show up  in the tree and palette.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::HideExcludedTags(bool fHideExcluded)
{
	m_fHideExcluded = fHideExcluded;

	HWND hwndButton = ::GetDlgItem(m_hwnd, kctidHideExcludedTags);
	::SendMessage(hwndButton, BM_SETCHECK, m_fHideExcluded, 0);

	::SetCursor(::LoadCursor(NULL, IDC_WAIT));
	LoadOverlay();
	::SetCursor(::LoadCursor(NULL, IDC_ARROW));
}


/*----------------------------------------------------------------------------------------------
	Return the number of visible tags that should show up in the tree or palette.
----------------------------------------------------------------------------------------------*/
int AfTagOverlayTool::GetVisiblePssCount()
{
	if (!m_qpli)
		return 0;
	return m_fHideExcluded ? m_vipssVisible.Size() : m_qpli->GetCount();
}


/*----------------------------------------------------------------------------------------------
	Return the PossItemInfo corresponding to the specified tag in the tree or palette.
----------------------------------------------------------------------------------------------*/
PossItemInfo * AfTagOverlayTool::GetVisiblePssInfo(int ipssVisible, int * pipss)
{
	AssertPtr(m_qpli);
	AssertPtrN(pipss);
	int ipss = m_fHideExcluded ? m_vipssVisible[ipssVisible] : ipssVisible;
	if (pipss)
		*pipss = ipss;
	return m_qpli->GetPssFromIndex(ipss);
}


/*----------------------------------------------------------------------------------------------
	Return true if the possibility is applied to the selection in the current view window.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::IsPssInSel(int ipss)
{
	return IsTagInSel(TagIndexFromPss(ipss));
}


/*----------------------------------------------------------------------------------------------
	Return true if the tag is applied to the selection in the current view window.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTool::IsTagInSel(int itag)
{
	if ((uint)itag >= (uint)m_vfTagInSel.Size())
		return false;
	return m_vfTagInSel[itag];
}


/*----------------------------------------------------------------------------------------------
	Return the tag index in the overlay from the specified possibility index.
	If the specified tag is excluded from the overlay, return -1.
----------------------------------------------------------------------------------------------*/
int AfTagOverlayTool::TagIndexFromPss(int ipss)
{
	if ((uint)ipss >= (uint)m_qpli->GetCount())
		return -1;
	PossItemInfo * ppii = m_qpli->GetPssFromIndex(ipss);
	AssertPtr(ppii);
	return TagIndexFromHvo(ppii->GetPssId());
}


/*----------------------------------------------------------------------------------------------
	Return the tag index in the overlay from the specified possibility index.
	If the specified tag is excluded from the overlay, return -1.
----------------------------------------------------------------------------------------------*/
int AfTagOverlayTool::TagIndexFromHvo(HVO hvoPss)
{
	// Perform a binary search to find the HVO that matches this possibility item.
	for (int iMin = 0, iLim = m_vhvoTag.Size(); iMin < iLim; )
	{
		int iMid = (iMin + iLim) >> 1;
		HVO hvo = m_vhvoTag[iMid];
		if (hvo == hvoPss)
			return iMid;
		else if (hvo < hvoPss)
			iMin = iMid + 1;
		else
			iLim = iMid;
	}
	return -1;
}


/*----------------------------------------------------------------------------------------------
	Show the child windows (tree and combo) for the new tab and hide the old ones.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::SelectTab(int itab)
{
	if (itab == kitabTree)
	{
		::ShowWindow(m_qtotr->Hwnd(), SW_SHOW);
		::ShowWindow(m_qtopl->Hwnd(), SW_HIDE);
	}
	else
	{
		::ShowWindow(m_qtopl->Hwnd(), SW_SHOW);
		::ShowWindow(m_qtotr->Hwnd(), SW_HIDE);
	}

	m_iCurTabSel = itab;
}


/*----------------------------------------------------------------------------------------------
	Select the corresponding item in the tree to the left. Returns E_FAIL if the web browser
	should not show the requested page.
----------------------------------------------------------------------------------------------*/
HRESULT AfTagOverlayTool::UpdateTree(wchar * pszUrl)
{
	AssertPsz(pszUrl);

	// If this is one of our fake pages, return.
	if (_wcsnicmp(pszUrl, L"about", 5) == 0)
		return S_OK;

	StrUni stu(pszUrl);
	int ichLim = stu.ReverseFindCh('.');
	int ichMin = stu.ReverseFindCh('/') + 1;
	Assert((uint)ichLim > (uint)ichMin);
	stu = stu.Mid(ichMin, ichLim - ichMin);

	HVO hvoPss = m_qpli->GetIdFromHelpId(stu.Chars());
	if (hvoPss)
	{
		int ipss = m_qpli->GetIndexFromId(hvoPss);
		if (ipss >= 0)
			SetSelectedPss(ipss, false);
	}
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Update the status of items in the palette and the tree. Also check to see if the whole
	dialog should be disabled or enabled.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::UpdateStatus(bool fForceUpdate)
{
	// If we're not visible, we don't need to update anything.
	if (!fForceUpdate && !::IsWindowVisible(m_hwnd))
		return;

	bool fEnable = false;
	int ctag = m_vfTagInSel.Size();
	if (ctag > 0)
	{
		// Remove the pressed flag for all the buttons.
		memset(&m_vfTagInSel[0], 0, ctag * sizeof(bool));

		AfMainWnd * pafw = MainWindow();
		AssertPtr(pafw);
		AfVwRootSitePtr qvwnd;
		int grfvfs = AfMainWnd::kvfsOverlay;
		if (pafw->GetActiveViewWindow(&qvwnd, &grfvfs) && grfvfs == AfMainWnd::kvfsOverlay)
		{
			// Get the selection from the current view window.
			IVwRootBoxPtr qrootb;
			IVwSelectionPtr qvwsel;
			qvwnd->get_RootBox(&qrootb);

			TtpVec vqttp;
			VwPropsVec vqvps;

			if (AfVwRootSite::GetCharacterProps(qrootb, &qvwsel, vqttp, vqvps))
			{
				int cttp = vqttp.Size();
				fEnable = true;

				// Now that we have a pointer to the selection of the current view window,
				// go through each textprops to see which tag buttons should be pressed.
				Vector<SmartBstr> vsbstr;
				vsbstr.Resize(cttp);
				Vector<int> vcuid;
				vcuid.Resize(cttp);
				for (int ittp = 0; ittp < cttp; ittp++)
				{
					CheckHr(vqttp[ittp]->GetStrPropValue(ktptTags, &(vsbstr[ittp])));
					vcuid[ittp] = vsbstr[ittp].Length() * isizeof(wchar) / isizeof(GUID);
				}
				for (int itag = 0; itag < ctag; itag++)
				{
					// Loop through each tag and get its guid.
					HVO hvo;
					COLORREF clrFore;
					COLORREF clrBack;
					COLORREF clrUnder;
					int unt;
					ComBool fHidden;
					GUID uid;
					CheckHr(m_qvo->GetDbTagInfo(itag, &hvo, &clrFore, &clrBack, &clrUnder,
						&unt, &fHidden, (OLECHAR *)&uid));

					int cuidFound = 0;
					int ittp;
					for (ittp = 0; cuidFound == ittp && ittp < cttp; ittp++)
					{
						// Loop through each textprop. The only time we want to set a button
						// as pressed is if every textprop contains the guid for that
						// button. So if cuidFound != ittp, we can stop looping through the
						// textprops.
						int cuid = vcuid[ittp];
						GUID * puid = (GUID *)vsbstr[ittp].Chars();
						int nRes = -1;
						for (int iuid = 0; nRes < 0 && iuid < cuid; iuid++)
						{
							// Loop through the guids for the textprop and compare them
							// to the button guid to see if there is a match. The guids in
							// the textprop are sorted (as strings), so we can quit if
							// nRes < 0.
							nRes = CompareGuids((OLECHAR *)&uid, (OLECHAR *)(puid + iuid));
							if (nRes == 0)
								cuidFound++;
						}
					}
					// If every textprop had the guid for the button, set the pressed flag.
					m_vfTagInSel[itag] = cuidFound == ittp;
				}
			}
		}
	}

	if (m_fEnabled != fEnable)
	{
		m_fEnabled = fEnable;
		EnableWindow(fEnable);
	}

	HWND hwnd = (m_iCurTabSel == kitabTree) ? m_qtotr->Hwnd() : m_qtopl->Hwnd();
	::InvalidateRect(hwnd, NULL, false);
	::UpdateWindow(hwnd);
}


/*----------------------------------------------------------------------------------------------
	Show or hide the help pane.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::ShowHelp(bool fShow, bool fForceShow)
{
	if (m_fShowHelp == fShow && !fForceShow)
		return;
	m_fShowHelp = fShow;

	Rect rc;
	::GetWindowRect(m_hwnd, &rc);

	if (fShow)
	{
		if (m_hwndHelp == NULL)
		{
			// This is the first time the help window is being shown, so we need to create
			// and position the webbrowser and toolbar controls.
			WaitCursor wc;

			// Create the toolbar window for the help pane.
			Assert(m_hwndTool == NULL);
			m_hwndTool = ::CreateWindow(TOOLBARCLASSNAME, NULL, WS_CHILD | TBSTYLE_FLAT |
				CCS_NOPARENTALIGN | CCS_NORESIZE | CCS_NODIVIDER, 0, 0, 0, 0,
				m_hwnd, NULL, NULL, NULL);
			::SendMessage(m_hwndTool, TB_BUTTONSTRUCTSIZE, isizeof(TBBUTTON), 0);
			if (!m_himlCold)
				m_himlCold = AfGdi::ImageList_LoadImage(ModuleEntry::GetModuleHandle(),
					MAKEINTRESOURCE(kridPossToolbarCold), 20, 0, CLR_DEFAULT, IMAGE_BITMAP,
					LR_DEFAULTCOLOR);
			::SendMessage(m_hwndTool, TB_SETIMAGELIST, 0, (LPARAM)m_himlCold);
			if (!m_himlHot)
				m_himlHot = AfGdi::ImageList_LoadImage(ModuleEntry::GetModuleHandle(),
					MAKEINTRESOURCE(kridPossToolbarHot), 20, 0, CLR_DEFAULT, IMAGE_BITMAP,
					LR_DEFAULTCOLOR);
			::SendMessage(m_hwndTool, TB_SETHOTIMAGELIST, 0, (LPARAM)m_himlHot);

			// Set the tool strings for the help toolbar.
			achar rgchBuf[MAX_PATH];
			StrAppBufPath strbpTool(kstidPossTbBack);
			memcpy(rgchBuf, strbpTool.Chars(), strbpTool.Length() * isizeof(achar));
			int ich = strbpTool.Length();
			rgchBuf[ich++] = 0;
			strbpTool.Load(kstidPossTbForward);
			memcpy(rgchBuf + ich, strbpTool.Chars(), strbpTool.Length() * isizeof(achar));
			ich += strbpTool.Length();
			rgchBuf[ich++] = 0;
			strbpTool.Load(kstidPossTbPrint);
			memcpy(rgchBuf + ich, strbpTool.Chars(), strbpTool.Length() * isizeof(achar));
			ich += strbpTool.Length();
			rgchBuf[ich++] = 0;
			rgchBuf[ich] = 0;
			::SendMessage(m_hwndTool, TB_ADDSTRING, NULL, (WPARAM)rgchBuf);
			TBBUTTON rgtbb[] = {
				{ 0, kcidPossBack,    TBSTATE_ENABLED, TBSTYLE_BUTTON },
				{ 1, kcidPossForward, TBSTATE_ENABLED, TBSTYLE_BUTTON },
				{ 2, kcidPossPrint,   TBSTATE_ENABLED, TBSTYLE_BUTTON },
			};
			rgtbb[0].iString = 0;
			rgtbb[1].iString = 1;
			rgtbb[2].iString = 2;
			::SendMessage(m_hwndTool, TB_ADDBUTTONS, 3, (LPARAM)rgtbb);

			m_qpwe.Attach(NewObj PossWebEvent(this, m_hwndTool, m_qlpi));
			// Create the HTML window for the help pane.
			// First initialize ATL control containment code.
			AtlAxWinInit();
			Assert(m_hwndHelp == NULL);
			m_hwndHelp = ::CreateWindowEx(WS_EX_CLIENTEDGE, _T(ATLAXWIN_CLASS), _T(""),
				WS_CHILD | WS_VSCROLL, 0, 0, 0, 0, m_hwnd, NULL,
				AfApp::Papp()->GetInstance(), NULL);
			IUnknownPtr qunk;
			CheckHr(AtlAxCreateControlEx(L"about:blank", m_hwndHelp, NULL, NULL, &qunk,
				IID_IDispatch, static_cast<IUnknown *>(m_qpwe)));
			CheckHr(qunk->QueryInterface(IID_IWebBrowser2, (void **)&m_qweb2));
			m_qpwe->SetWebBrowser(m_qweb2);
		}

		rc.right += m_dxpHelp;
		AfGfx::EnsureVisibleRect(rc);
		::MoveWindow(m_hwnd, rc.left, rc.top, rc.Width(), rc.Height(), true);

		UpdateHelpWindow();

		::ShowWindow(m_hwndHelp, SW_SHOW);
		::ShowWindow(m_hwndTool, SW_SHOW);
	}
	else
	{
		if (m_hwndHelp)
			::ShowWindow(m_hwndHelp, SW_HIDE);
		if (m_hwndTool)
			::ShowWindow(m_hwndTool, SW_HIDE);

		::SetWindowPos(m_hwnd, NULL, 0, 0, rc.Width() - m_dxpHelp, rc.Height(),
			SWP_NOZORDER | SWP_NOMOVE);
	}

	StrApp str(fShow ? "<<" : ">>");
	::SetWindowText(::GetDlgItem(m_hwnd, kctidOverlayToggleHelp), str.Chars());
}


/*----------------------------------------------------------------------------------------------
	Update the information in the help window.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::UpdateHelpWindow()
{
	if (!m_qpwe || !m_fShowHelp)
		return;

	PossItemInfo * ppii = m_qpli->GetPssFromIndex(m_ipssSelected);
	AssertPtr(ppii);
	m_qpwe->UpdateHelp(m_qpli, ppii->GetPssId());
}


/*----------------------------------------------------------------------------------------------
	Change how the tag items are displayed.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTool::ChangeDisplayOption(PossNameType pnt)
{
	if (m_pnt == pnt)
		return;
	m_pnt = pnt;

	m_qtotr->ChangeDisplayOption(pnt);
	m_qtopl->ChangeDisplayOption(pnt);

	UpdateEditText(m_ipssSelected);

	// See if we need to make the tool wider to accommodate the new display option.
	Rect rc;
	::GetWindowRect(m_hwnd, &rc);
	OnSizing(0, &rc);
	::MoveWindow(m_hwnd, rc.left, rc.top, rc.Width(), rc.Height(), true);
}


/***********************************************************************************************
	AfTagOverlayTree methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfTagOverlayTree::AfTagOverlayTree()
{
	m_fUpdating = false;
	m_himlStates = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfTagOverlayTree::~AfTagOverlayTree()
{
	if (m_himlStates)
	{
		AfGdi::ImageList_Destroy(m_himlStates);
		m_himlStates = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Create the list that contains the tags.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTree::Create(AfTagOverlayTool * ptot, int wid)
{
	AssertPtr(ptot);

	m_ptot = ptot;
	SubclassHwnd(::GetDlgItem(m_ptot->Hwnd(), wid));

	if (!m_himlStates)
		m_himlStates = AfGdi::ImageList_LoadImage(ModuleEntry::GetModuleHandle(),
			MAKEINTRESOURCE(kridTagStateImages), 16, 0, CLR_DEFAULT, IMAGE_BITMAP, 0);
	HIMAGELIST himlProjOld = TreeView_SetImageList(m_hwnd, m_himlStates, TVSIL_NORMAL);
	if (himlProjOld)
		AfGdi::ImageList_Destroy(himlProjOld);
}


/*----------------------------------------------------------------------------------------------
	Change how the tag items are displayed.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTree::ChangeDisplayOption(PossNameType pnt)
{
	if (m_pnt == pnt)
		return;
	m_pnt = pnt;

	::InvalidateRect(m_hwnd, NULL, false);
	::UpdateWindow(m_hwnd);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTree::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	// Don't erase the background (to reduce flicker).
	if (wm == WM_ERASEBKGND)
		return true;

	if (wm == WM_CHAR && wp == VK_SPACE)
	{
		// Pretend like the current item in the tree was clicked.
		m_ptot->ClickPss(-1);
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Draw everything to memory so it doesn't flicker.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTree::OnPaint(HDC hdcDef)
{
	Assert(!hdcDef);

	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps);
	Rect rc = ps.rcPaint;

	// Create the virtual screen in memory.
	HDC hdcMem = AfGdi::CreateCompatibleDC(hdc);
	HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, rc.Width(), rc.Height());
	HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
	::SetViewportOrgEx(hdcMem, -rc.left, -rc.top, NULL);
	AfGfx::FillSolidRect(hdcMem, rc, ::GetSysColor(COLOR_3DFACE));

	// Draw the tree view in memory:
	DefWndProc(WM_PAINT, (WPARAM)hdcMem, 0);

	// TODO
	/*if (m_pplddDragDrop)
		m_pplddDragDrop->Paint(hdcMem);*/

	// Copy image to the screen.
	::BitBlt(hdc, rc.left, rc.top, rc.Width(), rc.Height(), hdcMem, rc.left, rc.top, SRCCOPY);

	// Clean up.
	AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);

	BOOL fSuccess;
	fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
	Assert(fSuccess);

	fSuccess = AfGdi::DeleteDC(hdcMem);
	Assert(fSuccess);

	::EndPaint(m_hwnd, &ps);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle notifications.

	@param id Identifier of the common control sending the message.
	@param pnmh Pointer to an NMHDR structure containing notification code and additional info.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTree::OnNotifyThis(int id, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	switch (pnmh->code)
	{
	case TVN_GETDISPINFO:
		return OnGetDispInfo((NMTVDISPINFO *)pnmh);

	case NM_CLICK:
		{
			Point pt;
			::GetCursorPos(&pt);

			TVHITTESTINFO tvhti;
			::GetCursorPos(&tvhti.pt);
			::ScreenToClient(m_hwnd, &tvhti.pt);

			if (TreeView_HitTest(m_hwnd, &tvhti) && (tvhti.flags & TVHT_ONITEMICON))
			{
				// See if the state of this item can be changed.
				TVITEM tvi = { TVIF_IMAGE | TVIF_SELECTEDIMAGE, tvhti.hItem };
				if (TreeView_GetItem(m_hwnd, &tvi))
				{
					m_ptot->SetSelectedPss(tvi.lParam);
					if (tvi.iImage == kstatChecked || tvi.iImage == kstatUnchecked)
						m_ptot->ClickPss(-1);
					return true;
				}
			}
		}
		break;

	case NM_RCLICK:
		{
			Point pt;
			::GetCursorPos(&pt);

			TVHITTESTINFO tvhti;
			tvhti.pt = pt;
			::ScreenToClient(m_hwnd, &tvhti.pt);

			HTREEITEM hti = TreeView_HitTest(m_hwnd, &tvhti);
			if (hti)
				TreeView_SelectItem(m_hwnd, hti);

			HMENU hmenuPopup = ::LoadMenu(ModuleEntry::GetModuleHandle(),
				MAKEINTRESOURCE(kridOverlayToolMenu));
			::TrackPopupMenu(::GetSubMenu(hmenuPopup, kopmOptions),
				TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y, 0, m_ptot->Hwnd(), NULL);
			::DestroyMenu(hmenuPopup);
			return true;
		}
		break;

	case TVN_SELCHANGED:
		{
			// m_fUpdating is required because the SetSelectedPss call will update the
			// selection of the tree, and we don't want to get into an infinite loop here.
			if (!m_fUpdating)
			{
				NMTREEVIEW * pnmtv = (NMTREEVIEW *)pnmh;
				if (pnmtv->itemNew.hItem != NULL)
				{
					m_fUpdating = true;
					m_ptot->SetSelectedPss(pnmtv->itemNew.lParam);
					m_fUpdating = false;
					return true;
				}
			}
		}
		break;
	}

	return SuperClass::OnNotifyThis(id, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Show the appropriate text for an item based on the view of item selection.
	This gets called every time an item needs to be drawn.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTree::OnGetDispInfo(NMTVDISPINFO * pntdi)
{
	AssertPtr(pntdi);

	if (pntdi->item.mask & TVIF_TEXT)
	{
		PossListInfo * ppli = m_ptot->GetPossListInfo();
		AssertPtr(ppli);
		PossItemInfo * ppii = ppli->GetPssFromIndex(pntdi->item.lParam);
		AssertPtr(ppii);
		StrUni stu;
		ppii->GetName(stu, m_pnt);
		StrApp str(stu);
		lstrcpy(pntdi->item.pszText, str.Chars());
	}

	if (pntdi->item.mask & TVIF_IMAGE)
	{
		if (!m_ptot->IsEnabled() || m_ptot->TagIndexFromPss(pntdi->item.lParam) < 0)
			pntdi->item.iImage = kstatDisabled;
		else if (m_ptot->IsPssInSel(pntdi->item.lParam))
			pntdi->item.iImage = kstatChecked;
		else
			pntdi->item.iImage = kstatUnchecked;
	}

	if (pntdi->item.mask & TVIF_SELECTEDIMAGE)
	{
		if (!m_ptot->IsEnabled() || m_ptot->TagIndexFromPss(pntdi->item.lParam) < 0)
			pntdi->item.iSelectedImage = kstatDisabled;
		else if (m_ptot->IsPssInSel(pntdi->item.lParam))
			pntdi->item.iSelectedImage = kstatChecked;
		else
			pntdi->item.iSelectedImage = kstatUnchecked;
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Add all the possibilities in the list to the Choices treeview control.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTree::AddPossibilities()
{
	WaitCursor wc;

	// Figure out which items we need to add to the tree. To do this, we want to loop through
	// all the possibilities in reverse order. If the specified possibility is in the overlay,
	// we need to add it and all its parents to the tree.
	PossListInfo * ppli = m_ptot->GetPossListInfo();
	AssertPtr(ppli);
	if (ppli->GetCount() == 0)
		return true;

	Vector<int> vipss;
	vipss.Resize(ppli->GetCount());
	PossItemInfo * ppiiMin = ppli->GetPssFromIndex(0);
	AssertPtr(ppiiMin);
	int ipssTree = 0;
	PossItemInfo * ppiiLast = NULL;
	for (int ipssVisible = m_ptot->GetVisiblePssCount() - 1; ipssVisible >= 0; ipssVisible--)
	{
		PossItemInfo * ppiiTag = m_ptot->GetVisiblePssInfo(ipssVisible);
		AssertPtr(ppiiTag);

		if (ppiiLast > ppiiMin)
		{
			int nHierLevel = (ppiiLast--)->GetHierLevel();
			for (; ppiiLast > ppiiTag; ppiiLast--)
			{
				if (ppiiLast->GetHierLevel() < nHierLevel)
				{
					nHierLevel = ppiiLast->GetHierLevel();
					vipss[ipssTree++] = ppiiLast - ppiiMin;
				}
			}
		}

		vipss[ipssTree++] = ppiiTag - ppiiMin;
		ppiiLast = ppiiTag;
	}
	if (ppiiLast > ppiiMin)
	{
		int nHierLevel = (ppiiLast--)->GetHierLevel();
		for (; ppiiLast >= ppiiMin; ppiiLast--)
		{
			if (ppiiLast->GetHierLevel() < nHierLevel)
			{
				nHierLevel = ppiiLast->GetHierLevel();
				vipss[ipssTree++] = ppiiLast - ppiiMin;
			}
		}
	}
	vipss.Resize(ipssTree);

	// Add each item to the tree.
	TVINSERTSTRUCT tvis = { TVI_ROOT, TVI_LAST };
	tvis.item.mask = TVIF_PARAM | TVIF_TEXT | TVIF_IMAGE | TVIF_SELECTEDIMAGE;
	tvis.item.pszText = LPSTR_TEXTCALLBACK;
	tvis.item.iImage = I_IMAGECALLBACK;
	tvis.item.iSelectedImage = I_IMAGECALLBACK;

	Vector<HTREEITEM> vhti;
	vhti.Resize(8);
	vhti[0] = TVI_ROOT;

	// Keep track of which items were expanded.
	Vector<int> vipssExpanded;
	GetExpandedItems(TreeView_GetRoot(m_hwnd), vipssExpanded);

	::SendMessage(m_hwnd, WM_SETREDRAW, false, 0);
	m_fUpdating = true;
	TreeView_DeleteAllItems(m_hwnd);
	m_fUpdating = false;

	int ctag = vipss.Size();
	if (ctag)
	{
		// Turn scrollbars back on for the tree view.
		DWORD dwT = ::GetWindowLong(m_hwnd, GWL_STYLE);
		::SetWindowLong(m_hwnd, GWL_STYLE, dwT & ~TVS_NOSCROLL);

		// Add the possibility items to the tree.
		PossItemInfo * ppii = ppli->GetPssFromIndex(vipss[ctag - 1]);
		AssertPtr(ppii);
		PossItemInfo * ppiiNext;
		int ilevel = 1;
		int ilevelNext = 1;
		for (int itag = ctag - 1; itag >= 0; itag--)
		{
			// If the next item has a greater level, it is a child of this item, so set the
			// children flag.
			if (itag > 0)
			{
				ppiiNext = ppli->GetPssFromIndex(vipss[itag - 1]);
				AssertPtr(ppiiNext);
				ilevelNext = ppiiNext->GetHierLevel();
			}
			else
			{
				ppiiNext = NULL;
			}
			if (ilevelNext > ilevel)
			{
				vhti.Resize(ilevelNext + 1);
				tvis.item.cChildren = 1;
			}
			else
			{
				tvis.item.cChildren = 0;
			}

			// Add the item.
			tvis.hParent = vhti[ilevel - 1];
			tvis.item.lParam = vipss[itag];
			vhti[ilevel] = TreeView_InsertItem(m_hwnd, &tvis);

			ppii = ppiiNext;
			ilevel = ilevelNext;
		}
	}

	if (vipssExpanded.Size() > 0)
	{
		// Expand all the possibilities that were expanded before.
		SetExpandedItems(TreeView_GetRoot(m_hwnd), vipssExpanded);
	}

	OnSelectPss(m_ptot->GetSelectedPss());

	::SendMessage(m_hwnd, WM_SETREDRAW, true, 0);

	// This is necessary because of a bug in the tree view that does not scroll properly
	// when the redraw flag is off. Normally the OnSelectPss method will scroll to make
	// sure the item is visible.
	TreeView_EnsureVisible(m_hwnd, TreeView_GetSelection(m_hwnd));

	return true;
}


/*----------------------------------------------------------------------------------------------
	Go recursively through the tree adding the expanded items to a vector.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTree::GetExpandedItems(HTREEITEM hti, Vector<int> & vipss)
{
	if (!hti)
		return;

	TVITEM tvi = { TVIF_HANDLE | TVIF_STATE | TVIF_PARAM };
	tvi.stateMask = TVIS_EXPANDED;
	while (hti)
	{
		tvi.hItem = hti;
		if (TreeView_GetItem(m_hwnd, &tvi))
		{
			if ((tvi.state & TVIS_EXPANDED) == TVIS_EXPANDED)
				vipss.Push(tvi.lParam);
		}
		HTREEITEM htiT = TreeView_GetChild(m_hwnd, hti);
		if (htiT)
			GetExpandedItems(htiT, vipss);
		hti = TreeView_GetNextSibling(m_hwnd, hti);
	}
}


/*----------------------------------------------------------------------------------------------
	Go recursively through the tree setting the expanded items from a vector.
	When this method returns, vipss will contain items that couldn't be found in the tree.
	This method returns true when all items have been expanded; otherwise it returns false.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayTree::SetExpandedItems(HTREEITEM hti, Vector<int> & vipss)
{
	if (!hti)
		return false;

	TVITEM tvi = { TVIF_HANDLE | TVIF_PARAM };
	while (hti)
	{
		tvi.hItem = hti;
		if (TreeView_GetItem(m_hwnd, &tvi))
		{
			int ipssInTree = tvi.lParam;
			for (int ipss = 0, cpss = vipss.Size(); ipss < cpss; ipss++)
			{
				if (vipss[ipss] == ipssInTree)
				{
					TreeView_SetItemState(m_hwnd, hti, TVIS_EXPANDED, TVIS_EXPANDED);
					vipss.Delete(ipss);
					if (vipss.Size() == 0)
						return true;
					break;
				}
			}
		}
		HTREEITEM htiT = TreeView_GetChild(m_hwnd, hti);
		if (htiT)
		{
			if (SetExpandedItems(htiT, vipss))
				return true;
		}
		hti = TreeView_GetNextSibling(m_hwnd, hti);
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	This method recursively searches through the tree to find the item that corresponds to the
	given hvoPss.
----------------------------------------------------------------------------------------------*/
HTREEITEM AfTagOverlayTree::FindHvo(HTREEITEM hti, HVO hvoPss)
{
	AssertPtr(m_ptot);
	PossListInfo * ppli = m_ptot->GetPossListInfo();
	AssertPtr(ppli);

	TVITEM tvi = { TVIF_HANDLE | TVIF_PARAM };
	while (hti)
	{
		tvi.hItem = hti;
		if (TreeView_GetItem(m_hwnd, &tvi))
		{
			PossItemInfo * ppii = ppli->GetPssFromIndex(tvi.lParam);
			AssertPtr(ppii);
			if (ppii->GetPssId() == hvoPss)
			{
				// We have found a match.
				return hti;
			}
		}
		HTREEITEM htiT = TreeView_GetChild(m_hwnd, hti);
		if (htiT)
		{
			htiT = FindHvo(htiT, hvoPss);
			if (htiT)
				return htiT;
		}
		hti = TreeView_GetNextSibling(m_hwnd, hti);
	}
	return NULL;
}


/*----------------------------------------------------------------------------------------------
	This method recursively searches through the tree to find the item at the requested index.
----------------------------------------------------------------------------------------------*/
HTREEITEM AfTagOverlayTree::FindPssHelper(HTREEITEM hti, int ipss)
{
	while (hti)
	{
		TVITEM tvi = { TVIF_HANDLE | TVIF_PARAM, hti };
		if (TreeView_GetItem(m_hwnd, &tvi) && tvi.lParam == ipss)
		{
			// We have found a match.
			return hti;
		}
		HTREEITEM htiT = TreeView_GetChild(m_hwnd, hti);
		if (htiT)
		{
			htiT = FindPssHelper(htiT, ipss);
			if (htiT)
				return htiT;
		}
		hti = TreeView_GetNextSibling(m_hwnd, hti);
	}
	return NULL;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void AfTagOverlayTree::OnClickPss(int ipss)
{
	// TODO: Do we need to do anything here?
}


/*----------------------------------------------------------------------------------------------
	Select the specified tag and make sure it's visible.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayTree::OnSelectPss(int ipss)
{
	HTREEITEM hti = FindPss(ipss);
	if (hti)
	{
		// This prevents infinite recursion since we change the current item in this method.
		static bool s_fUpdating = false;
		if (!s_fUpdating)
		{
			s_fUpdating = true;
			TreeView_SelectItem(m_hwnd, hti);
			TreeView_EnsureVisible(m_hwnd, hti);
			s_fUpdating = false;
		}
	}
}


/***********************************************************************************************
	OverlayChsrTab methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool OverlayChsrTab::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	// Don't erase the background (to reduce flicker).
	if (wm == WM_ERASEBKGND)
		return true;

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Draw everything to memory so it doesn't flicker.
----------------------------------------------------------------------------------------------*/
bool OverlayChsrTab::OnPaint(HDC hdcDef)
{
	Assert(!hdcDef);

	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps);
	Rect rc = ps.rcPaint;

	// Create the virtual screen in memory.
	HDC hdcMem = AfGdi::CreateCompatibleDC(hdc);
	HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, rc.Width(), rc.Height());
	HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
	::SetViewportOrgEx(hdcMem, -rc.left, -rc.top, NULL);
	AfGfx::FillSolidRect(hdcMem, rc, ::GetSysColor(COLOR_3DFACE));

	// TODO:  TimP
	HFONT hfontMemOld = (HFONT)::GetCurrentObject(hdcMem, OBJ_FONT);
	// DefWndProc() below is sometimes selecting a non-default font into hdcMem.


	// Draw the tab view in memory.
	DefWndProc(WM_PAINT, (WPARAM)hdcMem, 0); // This sometimes changes the font for hdcMem!

	// Copy image to the screen.
	::BitBlt(hdc, rc.left, rc.top, rc.Width(), rc.Height(), hdcMem, rc.left, rc.top, SRCCOPY);


	// DefWndProc() above is sometimes selecting a non-default font into hdcMem.
	HFONT hfontMemOld2 = AfGdi::SelectObjectFont(hdcMem, hfontMemOld, AfGdi::CLUDGE_OLD);
	if (hfontMemOld != hfontMemOld2)
	{
//		BOOL fSuccess;
//		fSuccess = AfGdi::DeleteObjectFont(hfontMemOld2);  // Do NOT delete hfontMemOld2!
//		Assert(fSuccess);
	}


	// Clean up.
	HBITMAP hbmpDebug;
	hbmpDebug = AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);
	Assert(hbmpDebug && hbmpDebug != HGDI_ERROR);
	Assert(hbmpDebug == hbmp);

	BOOL fSuccess;
	if (hbmpOld != hbmp)
	{
		fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
		Assert(fSuccess);
	}

	fSuccess = AfGdi::DeleteDC(hdcMem);
	Assert(fSuccess);

	::EndPaint(m_hwnd, &ps);

	return true;
}


/***********************************************************************************************
	AfTagOverlayPalette methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfTagOverlayPalette::AfTagOverlayPalette()
{
	m_fShowRecent = false;
	m_ibtnTop = 0;
	m_ibtnSelected = -1;
	m_ipssHover = -1;
	m_hfont = NULL;
	for (int itag = 0; itag < kcRecentOverlayTag; itag++)
	{
		m_rgipssRecent[itag] = -1;
		m_rgipssRecentSrt[itag] = -1;
	}
	m_dxpButton = 0;
	m_dypButton = 0;
	m_dypOffset = 0;
	m_crowVisible = 0;
	m_fInMostRecent = false;
	m_hwndToolTip = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfTagOverlayPalette::~AfTagOverlayPalette()
{
#ifdef TimP_2002_12_Invalid
	// It appears tooltip "windows" should not be "DestroyWindow"ed.
	// The DestroyWindow call below causes an error that GetLastError reports as "1400".
	if (m_hwndToolTip)
	{
		BOOL flag  = ::DestroyWindow(m_hwndToolTip);
		if (!flag)
		{
			CHAR szBuf[80];
			DWORD dw = GetLastError();
			sprintf(szBuf, "%s failed: GetLastError returned %u.\n",
				"DestroyWindow", dw);
			MessageBox(NULL, szBuf, "Error", MB_OK);
		}
		m_hwndToolTip = NULL;
	}
#endif
}


/*----------------------------------------------------------------------------------------------
	Create the list that contains the tags.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayPalette::Create(AfTagOverlayTool * ptot, int wid)
{
	AssertPtr(ptot);

	static bool s_fIsRegistered = false;
	if (!s_fIsRegistered)
	{
		AfWnd::RegisterClass(_T("AfTagOverlayPalette"), 0, 0, 0, COLOR_3DFACE);
		s_fIsRegistered = true;
	}

	m_ptot = ptot;

	WndCreateStruct wcs;
	wcs.style = WS_CHILD | WS_VISIBLE | WS_BORDER | WS_TABSTOP | WS_VSCROLL;
	wcs.dwExStyle = WS_EX_CLIENTEDGE;
	wcs.lpszClass = _T("AfTagOverlayPalette");
	wcs.hwndParent = m_ptot->Hwnd();
	wcs.SetWid(wid);
	CreateHwnd(wcs);
}


/*----------------------------------------------------------------------------------------------
	Change how the tag items are displayed.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayPalette::ChangeDisplayOption(PossNameType pnt)
{
	if (m_pnt == pnt)
		return;
	m_pnt = pnt;

	UpdateScrollbar();
}


/*----------------------------------------------------------------------------------------------
	Add all the possibilities in the list to the Choices treeview control.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayPalette::AddPossibilities()
{
	AssertPtr(m_ptot);
	int cpssVisible = m_ptot->GetVisiblePssCount();
	m_vipssButton.Resize(cpssVisible);
	for (int ipssVisible = 0; ipssVisible < cpssVisible; ipssVisible++)
		m_ptot->GetVisiblePssInfo(ipssVisible, &m_vipssButton[ipssVisible]);

	// Force the palette to lay itself out.
	UpdateScrollbar();

	OnSelectPss(m_ptot->GetSelectedPss());
}


/*----------------------------------------------------------------------------------------------
	Change whether or not recent tags are shown.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayPalette::SetShowRecent(bool fShow)
{
	m_fShowRecent = fShow;
	UpdateScrollbar();
}


/*----------------------------------------------------------------------------------------------
	Create the child windows after the main tool window has been created.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayPalette::PostAttach()
{
	// Create the tooltip control.
	Assert(m_hwndToolTip == NULL);
	m_hwndToolTip = ::CreateWindow(TOOLTIPS_CLASS, NULL, TTS_ALWAYSTIP, 0, 0, 0, 0, m_hwnd,
		0, ModuleEntry::GetModuleHandle(), NULL);
	if (!m_hwndToolTip)
		ThrowHr(E_FAIL);

	TOOLINFO ti = { isizeof(ti), TTF_IDISHWND | TTF_SUBCLASS };
	ti.hwnd = m_hwnd;
	ti.uId = (uint)m_hwnd;
	ti.lpszText = _T("");
	::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);

	// Create the font for the tags and find out how big each button needs to be so that it
	// can show at least 'MMM...'.
	m_hfont = (HFONT)::GetStockObject(DEFAULT_GUI_FONT);
	HDC hdc = ::GetDC(m_hwnd);
	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, m_hfont);
	SIZE size;
	::GetTextExtentPoint32(hdc, _T("MMM..."), 6, &size);
	m_dxpButton = size.cx + (kdzpBorder + kdzpMargin) * 2;
	m_dypButton = size.cy + (kdzpBorder + kdzpMargin) * 2;
	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	int iSuccess;
	iSuccess = ::ReleaseDC(m_hwnd, hdc);
	Assert(iSuccess);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayPalette::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_GETDLGCODE:
		lnRet = DLGC_WANTALLKEYS;
		return true;

	case WM_KEYDOWN:
		return OnKeyDown(wp, LOWORD(lp), HIWORD(lp));

	case WM_LBUTTONDOWN:
		return OnLButtonDown(wp, (short)LOWORD(lp), (short)HIWORD(lp));

	case WM_LBUTTONUP:
		return OnLButtonUp(wp, (short)LOWORD(lp), (short)HIWORD(lp));

	case WM_MOUSEMOVE:
		return OnMouseMove(wp, (short)LOWORD(lp), (short)HIWORD(lp));

	case WM_VSCROLL:
		return OnVScroll(LOWORD(wp), HIWORD(wp), (HWND)lp);

	// Force a redraw so we add or get rid of the focus rectangle.
	case WM_SETFOCUS: // Fall through.
	case WM_KILLFOCUS:
		::InvalidateRect(m_hwnd, NULL, false);
		break;

	case WM_ERASEBKGND:
		// Don't erase the background because the paint procedure takes care of it.
		return true;

	default:
		break;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Recalculate the top button that should be shown and redraw the list when it is sized.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayPalette::OnSize(int wst, int dxp, int dyp)
{
	UpdateScrollbar();

	::InvalidateRect(m_hwnd, NULL, false);
	::UpdateWindow(m_hwnd);

	return SuperClass::OnSize(wst, dxp, dyp);
}


/*----------------------------------------------------------------------------------------------
	Return the number of buttons that will fit in the given width.
	If pdxpButton is not null, it will be set to the width of each button.
----------------------------------------------------------------------------------------------*/
int AfTagOverlayPalette::GetButtonCountPerRow(int dxpRow, int * pdxpButton)
{
	AssertPtrN(pdxpButton);

	int dxpButton = m_dxpButton;
	if (m_pnt == kpntName)
		dxpButton *= 2;
	else if (m_pnt == kpntNameAndAbbrev)
		dxpButton *= 3;
	if (pdxpButton)
		*pdxpButton = dxpButton;
	return (dxpRow - kdzpBorder) / (dxpButton + kdzpBorder);
}


/*----------------------------------------------------------------------------------------------
	Update the Most Recent Tag area.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayPalette::OnClickPss(int ipss)
{
	if (!m_fInMostRecent)
	{
		// We need to insert the new button at the proper place in the array of recent tags.
		// We keep two arrays; one sorted and one non-sorted. The sorted array is the one that
		// determines what the user sees in the tool window. The non-sorted array is the one
		// that determines which button (the oldest one) gets removed from the most recent
		// group when a new button is pressed.
		int ipssRemove = m_rgipssRecent[kcRecentOverlayTag - 1];

		// See if the button is already in the non-sorted array.
		int ipssRecent;
		for (ipssRecent = 0; ipssRecent < kcRecentOverlayTag - 1; ipssRecent++)
		{
			if (m_rgipssRecent[ipssRecent] == ipss)
				break;
		}
		// Move any existing indexes down one slot.
		memmove(m_rgipssRecent + 1, m_rgipssRecent, ipssRecent * isizeof(int));
		// Insert the new index at the beginning.
		m_rgipssRecent[0] = ipss;

		// If the button is not already in the most recently used list, add it now.
		if (ipssRecent == kcRecentOverlayTag - 1 && ipssRemove != ipss)
		{
			if (ipssRemove != -1)
			{
				int iv;
				for (iv = 0; iv < kcRecentOverlayTag; iv++)
				{
					if (m_rgipssRecentSrt[iv] == ipssRemove)
						break;
				}
				memmove(m_rgipssRecentSrt + iv, m_rgipssRecentSrt + iv + 1,
					(kcRecentOverlayTag - iv - 1) * isizeof(int));
				m_rgipssRecentSrt[kcRecentOverlayTag - 1] = -1;
			}

			// Insert the new item in the correct sorted order.
			StrUni stuAbbrev;
			StrUni stuAbbrevNew;
			PossListInfo * ppli = m_ptot->GetPossListInfo();
			AssertPtr(ppli);
			PossItemInfo * ppii = ppli->GetPssFromIndex(ipss);
			AssertPtr(ppii);
			ppii->GetName(stuAbbrev, kpntAbbreviation);
			// Find where the item should be inserted. The rest of the items should still be
			// sorted correctly.
			int iv;
			for (iv = 0; iv < kcRecentOverlayTag; iv++)
			{
				if (m_rgipssRecentSrt[iv] == -1)
					break;
				ppii = ppli->GetPssFromIndex(m_rgipssRecentSrt[iv]);
				AssertPtr(ppii);
				ppii->GetName(stuAbbrevNew, kpntAbbreviation);
				if (_wcsicmp(stuAbbrevNew.Chars(), stuAbbrev.Chars()) < 0)
					break;
			}
			Assert((uint)iv < (uint)kcRecentOverlayTag);
			// We need to insert it before the current button.
			if (m_rgipssRecentSrt[iv] != ipss)
			{
				memmove(m_rgipssRecentSrt + iv + 1, m_rgipssRecentSrt + iv,
					(kcRecentOverlayTag - iv - 1) * isizeof(int));
				m_rgipssRecentSrt[iv] = ipss;
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Make sure the specified possibility is visible.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayPalette::OnSelectPss(int ipss)
{
	Rect rc;
	GetClientRect(rc);
	int cbtnPerRow = GetButtonCountPerRow(rc.Width());
	if (!cbtnPerRow)
		return;

	// This might not find the possibility in the case where the tree is showing an excluded
	// item that is needed for a child item. Since the excluded item is only shown in the tree
	// and not in the palette, m_ibtnSelected will be set to -1.
	m_ibtnSelected = ButtonFromPss(ipss);
	if (m_ibtnSelected == -1)
		return;

	int ibtnLastRow = m_ibtnSelected / cbtnPerRow;
	int ibtnTopRow = -1;
	if (m_ibtnSelected < m_ibtnTop)
		ibtnTopRow = ibtnLastRow;
	else if (m_ibtnSelected >= m_ibtnTop + m_crowVisible * cbtnPerRow)
		ibtnTopRow = Max(0, ibtnLastRow - m_crowVisible + 1);
	if (ibtnTopRow != -1)
		m_ibtnTop = ibtnTopRow * cbtnPerRow;
	Assert((uint)m_ibtnTop < (uint)m_vipssButton.Size());

	::SetScrollPos(m_hwnd, SB_VERT, m_ibtnTop / cbtnPerRow, true);

	::InvalidateRect(m_hwnd, NULL, false);
	::UpdateWindow(m_hwnd);
}


/*----------------------------------------------------------------------------------------------
	Return the index of the button that corresponds to the specified possibility.
	Return -1 if the possibility cannot be found.
----------------------------------------------------------------------------------------------*/
int AfTagOverlayPalette::ButtonFromPss(int ipss)
{
	for (int ibtn = 0, cbtn = m_vipssButton.Size(); ibtn < cbtn; ibtn++)
	{
		if (m_vipssButton[ibtn] == ipss)
			return ibtn;
	}
	return -1;
}


/*----------------------------------------------------------------------------------------------
	Update the vertical scrollbar settings.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayPalette::UpdateScrollbar()
{
	Rect rc;
	GetClientRect(rc);
	int cbtnPerRow = GetButtonCountPerRow(rc.Width());
	if (!cbtnPerRow)
		return;

	SCROLLINFO si = { isizeof(si),  SIF_PAGE | SIF_POS | SIF_RANGE };

	int dyp = rc.Height();
	int cbtn = m_vipssButton.Size();
	// The second line here is checking to see if the scrollbar is currently showing.
	// If the window is not currently visible, we don't care about the scrollbar.
	// If the window is visible, and the scrollbar is showing, we need to account
	// for the Show Recent area.
	if (m_fShowRecent && m_rgipssRecent[0] != -1 && cbtn > kcRecentOverlayTag &&
		(!::IsWindowVisible(m_hwnd) || (::GetScrollInfo(m_hwnd, SB_VERT, &si) && si.nMax > 0)))
	{
		dyp -= (kdzpBorder + m_dypButton); // Border above the recent buttons.
		int ibtnInRow = -1;
		for (int ibtn = 0; ibtn < kcRecentOverlayTag && m_rgipssRecentSrt[ibtn] != -1; ibtn++)
		{
			if (++ibtnInRow == cbtnPerRow)
			{
				ibtnInRow = 0;
				dyp -= (m_dypButton + kdzpBorder); // Button height plus spacing.
			}
		}
		dyp -= (kdzpBorder * 3 + 2);
	}

	int ibtn = m_ibtnTop;
	int yp = 0;
	m_crowVisible = 0;
	while (yp < dyp && ibtn < cbtn)
	{
		yp += kdzpBorder + m_dypButton;
		ibtn += cbtnPerRow;
		if (ibtn > cbtn)
			ibtn = cbtn;
		m_crowVisible++;
	}
	if (yp > dyp)
		m_crowVisible--;

	int cRows = (dyp - kdzpBorder) / (m_dypButton + kdzpBorder);
	m_ibtnTop = (m_ibtnTop / cbtnPerRow) * cbtnPerRow;

	// See if the top button needs to be adjusted to get rid of empty space at the bottom.
	int ibtnTopMax = ((cbtn - ((cRows - 1) * cbtnPerRow) - 1) / cbtnPerRow) * cbtnPerRow;
	if (ibtnTopMax < 0)
		ibtnTopMax = 0;
	if (m_ibtnTop > ibtnTopMax)
		m_ibtnTop = ibtnTopMax;
	m_ibtnTopMax = ibtnTopMax;

	// Fix the vertical scrollbar.
	si.nMin = 0;
	si.nMax = m_ibtnTopMax / cbtnPerRow;
	si.nPage = 1;
	si.nPos = m_ibtnTop / cbtnPerRow;
	::SetScrollInfo(m_hwnd, SB_VERT, &si, true);
}


/*----------------------------------------------------------------------------------------------
	Return the minimum width of the palette.
----------------------------------------------------------------------------------------------*/
int AfTagOverlayPalette::GetMinWidth()
{
	int dxp = kdzpBorder * 2;
	if (m_pnt == kpntNameAndAbbrev)
		dxp += m_dxpButton * 3;
	else if (m_pnt == kpntName)
		dxp += m_dxpButton * 2;
	else
		dxp += (m_dxpButton * 2) + kdzpBorder;
	// Add the width of the vertical scrollbar.
	dxp += ::GetSystemMetrics(SM_CXVSCROLL);
	// Add the width of the borders.
	dxp += ::GetSystemMetrics(SM_CXEDGE) * 2;
	return dxp;
}


/*----------------------------------------------------------------------------------------------
	Paint the list.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayPalette::OnPaint(HDC hdcDef)
{
	Assert(!hdcDef);

	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps);
	Rect rc;
	GetClientRect(rc);

	HDC hdcMem = AfGdi::CreateCompatibleDC(hdc);
	HBITMAP hbmp;
	HBITMAP hbmpOld;
	{
		SmartPalette spal(hdc);
		SmartPalette spalMem(hdcMem);
		hbmp = AfGdi::CreateCompatibleBitmap(hdc, rc.Width(), rc.Height());
		hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
		AfGfx::FillSolidRect(hdcMem, rc, ::GetSysColor(COLOR_3DFACE));

		// Draw the buttons for the visible tags.
		::SetBkMode(hdcMem, TRANSPARENT);

		HFONT hfontOld = AfGdi::SelectObjectFont(hdcMem, m_hfont);
		DrawButtons(hdcMem, rc);
		AfGdi::SelectObjectFont(hdcMem, hfontOld, AfGdi::OLD);

		// Copy the image from memory onto the screen.
		::BitBlt(hdc, 0, 0, rc.Width(), rc.Height(), hdcMem, 0, 0, SRCCOPY);
	}

	// Clean up.
	HBITMAP hbmpDebug;
	hbmpDebug = AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);
	Assert(hbmpDebug && hbmpDebug != HGDI_ERROR);
	Assert(hbmpDebug == hbmp);

	BOOL fSuccess;
	fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
	Assert(fSuccess);

	fSuccess = AfGdi::DeleteDC(hdcMem);
	Assert(fSuccess);

	::EndPaint(m_hwnd, &ps);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle when the user moves the mouse over the list window.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayPalette::OnMouseMove(uint grfmk, int xp, int yp)
{
	::SetCursor(::LoadCursor(NULL, IDC_ARROW));

	TOOLINFO ti = { isizeof(ti) };
	ti.hwnd = m_hwnd;
	ti.uId = (uint)m_hwnd;

	m_ipssHover = -1;
	int ipssHover = GetPssFromCoord(xp, yp);
	if (ipssHover != m_ipssHover)
	{
		StrApp strName;
		if (ipssHover != -1)
		{
			// Get the abbreviation and name for the tooltip.
			PossListInfo * ppli = m_ptot->GetPossListInfo();
			AssertPtr(ppli);
			PossItemInfo * ppii = ppli->GetPssFromIndex(ipssHover);
			AssertPtr(ppii);
			StrUni stuName;
			ppii->GetName(stuName);
			strName = stuName;
		}
		ti.lpszText = const_cast<achar *>(strName.Chars());

		// Update the tooltip text.
		::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, 0, (LPARAM)&ti);

		m_ipssHover = ipssHover;
	}
	if (m_ipssHover == -1)
	{
		// Hide the tooltip if it is showing.
		::SendMessage(m_hwndToolTip, TTM_POP, 0, (LPARAM)&ti);
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	The user has released the left mouse button.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayPalette::OnLButtonUp(uint grfmk, int xp, int yp)
{
	// If the button is let up over the same item it was pressed down on, click it.
	int ipss = m_ptot->GetSelectedPss();
	if (ipss != -1 && GetPssFromCoord(xp, yp) == ipss)
		m_ptot->ClickPss(ipss);

	::InvalidateRect(m_hwnd, NULL, false);
	::UpdateWindow(m_hwnd);

	return true;
}


/*----------------------------------------------------------------------------------------------
	The user has pressed the left mouse button.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayPalette::OnLButtonDown(uint grfmk, int xp, int yp)
{
	::SetFocus(m_hwnd);

	// Find out which button was pressed.
	int ipss = GetPssFromCoord(xp, yp);
	if (ipss < 0)
		return false;

	m_ptot->SetSelectedPss(ipss);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Show a context menu for the overlay palette control.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayPalette::OnContextMenu(HWND hwnd, Point pt)
{
	Point ptClient;
	if (pt.x == -1 && pt.y == -1)
	{
		// Coordinates at -1,-1 is the indication that the user triggered the menu with the
		// keyboard, and not the mouse:
		ptClient.Set(0, 0);
		::ClientToScreen(m_hwnd, &pt);
	}
	else
	{
		ptClient = pt; // Point in AfDeSplitChild coordinates.
		::ScreenToClient(m_hwnd, &ptClient);
	}
	int ipss = GetPssFromCoord(ptClient.x, ptClient.y);
	if (ipss >= 0)
		m_ptot->SetSelectedPss(ipss);

	HMENU hmenuPopup = ::LoadMenu(ModuleEntry::GetModuleHandle(),
		MAKEINTRESOURCE(kridOverlayToolMenu));
	::TrackPopupMenu(::GetSubMenu(hmenuPopup, kopmOptions),
		TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y, 0, m_ptot->Hwnd(), NULL);
	::DestroyMenu(hmenuPopup);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle keyboard messages. This moves the current button.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayPalette::OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
{
	if (nChar == VK_TAB)
	{
		// Since we are capturing all the keystrokes (which was necessary to get the <Enter>
		// key), hitting <Tab> will not move the focus to the next sibling window. We
		// are trapping that case here and doing it manually.
		::SetFocus(::GetNextDlgTabItem(::GetParent(m_hwnd), m_hwnd,
			::GetKeyState(VK_SHIFT) < 0));
		return true;
	}
	if (nChar == VK_ESCAPE)
	{
		// Since we are capturing all the keystrokes (which was necessary to get the <Enter>
		// key), hitting <Escape> will not send a WM_CLOSE message to the dialog window. We
		// are trapping that case here and doing it manually.
		::SendMessage(::GetParent(m_hwnd), WM_CLOSE, 0, 0);
		return true;
	}

	int ibtn = m_ibtnSelected;
	Rect rc;
	GetClientRect(rc);
	int cbtnPerRow = GetButtonCountPerRow(rc.Width());

	switch (nChar)
	{
	case VK_DOWN:
		ibtn += cbtnPerRow;
		break;
	case VK_UP:
		ibtn -= cbtnPerRow;
		break;
	case VK_RIGHT:
		ibtn++;
		break;
	case VK_LEFT:
		ibtn--;
		break;
	case VK_HOME:
		ibtn = 0;
		break;
	case VK_END:
		ibtn = m_vipssButton.Size() - 1;
		break;
	case VK_PRIOR:
		ibtn -= cbtnPerRow * m_crowVisible;
		break;
	case VK_NEXT:
		ibtn += cbtnPerRow * m_crowVisible;
		break;
	case VK_SPACE: // Fall through.
	case VK_RETURN:
		m_ptot->ClickPss(-1);
		return true;
	default:
		return false;
	}

	ibtn = Max(0, Min(m_vipssButton.Size() - 1, ibtn));
	int ipss = m_vipssButton[ibtn];
	if (ipss != m_ptot->GetSelectedPss())
		m_ptot->SetSelectedPss(ipss);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Scroll the tags in the list.
----------------------------------------------------------------------------------------------*/
bool AfTagOverlayPalette::OnVScroll(int nSBCode, int nPos, HWND hwndSbar)
{
	Rect rc;
	GetClientRect(rc);
	int cbtnPerRow = GetButtonCountPerRow(rc.Width());
	int cRows = (rc.Height() - kdzpBorder) / (m_dypButton + kdzpBorder);
	int ibtnTopNew = m_ibtnTop;

	switch (nSBCode)
	{
	case SB_BOTTOM:
		ibtnTopNew = m_ibtnTopMax;
		break;
	case SB_LINEDOWN:
		ibtnTopNew += cbtnPerRow;
		break;
	case SB_LINEUP:
		ibtnTopNew -= cbtnPerRow;
		break;
	case SB_PAGEDOWN:
		ibtnTopNew += (cRows * cbtnPerRow);
		break;
	case SB_PAGEUP:
		ibtnTopNew -= (cRows * cbtnPerRow);
		break;
	case SB_THUMBPOSITION:
	case SB_THUMBTRACK:
		ibtnTopNew = nPos * cbtnPerRow;
		break;
	case SB_TOP:
		ibtnTopNew = 0;
		break;
	}
	if (ibtnTopNew < 0)
		ibtnTopNew = 0;
	else if (ibtnTopNew > m_ibtnTopMax)
		ibtnTopNew = m_ibtnTopMax;

	m_ibtnTop = ibtnTopNew;
	::SetScrollPos(m_hwnd, SB_VERT, m_ibtnTop / cbtnPerRow, true);
	::InvalidateRect(m_hwnd, NULL, false);
	::UpdateWindow(m_hwnd);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Returns -1 if the coordinate is not within a button. Otherwise it returns the index of the
	button in the overlay.
----------------------------------------------------------------------------------------------*/
int AfTagOverlayPalette::GetPssFromCoord(int xp, int yp)
{
	Rect rc;
	GetClientRect(rc);
	int dxpButton;
	int cbtnPerRow = GetButtonCountPerRow(rc.Width(), &dxpButton);

	m_fInMostRecent = false;
	if (m_dypOffset)
	{
		if (yp >= m_dypOffset)
			yp -= m_dypOffset;
		else
			m_fInMostRecent = true;
	}

	// Make sure the mouse click was on a row and not in the space between rows.
	int irow = (yp - kdzpBorder) / (m_dypButton + kdzpBorder);
	if (yp > kdzpBorder + irow * (m_dypButton + kdzpBorder) + m_dypButton)
		return -1;

	// Make sure the mouse click was on a column and not in the space between columns.
	int icol = (xp - kdzpBorder) / (dxpButton + kdzpBorder);
	if (icol >= cbtnPerRow)
		return -1;
	if (xp > kdzpBorder + icol * (dxpButton + kdzpBorder) + dxpButton)
		return -1;

	if (m_fInMostRecent)
	{
		int ibtn = irow * cbtnPerRow + icol;
		if (ibtn >= kcRecentOverlayTag)
			return -1;
		return m_rgipssRecentSrt[ibtn];
	}

	int ibtn = m_ibtnTop + irow * cbtnPerRow + icol;
	if (ibtn >= m_vipssButton.Size())
		return -1;
	return m_vipssButton[ibtn];
}


/*----------------------------------------------------------------------------------------------
	Draw all the buttons in the list. If hdc is NULL, no buttons are drawn, but m_crowVisible
	is updated.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayPalette::DrawButtons(HDC hdc, Rect & rc)
{
	Rect rcPalette(rc);
	DrawRecentButtons(hdc, rcPalette);
	// rcPalette.top has now been adjusted by the height of the recent buttons.
	m_dypOffset = rcPalette.top - rc.top;

	int dxpButton;
	int cbtnPerRow = GetButtonCountPerRow(rcPalette.Width(), &dxpButton);
	int ibtn = m_ibtnTop;
	int cbtn = m_vipssButton.Size();

	Rect rcRow(rcPalette);
	rcRow.Inflate(-kdzpBorder, 0);

	// Keep drawing rows until we run out of room or we run out of buttons.
	m_crowVisible = 0;
	while (rcRow.top < rcPalette.bottom && ibtn < cbtn)
	{
		rcRow.top += kdzpBorder;
		rcRow.bottom = rcRow.top + m_dypButton;

		for (int ibtnInRow = 0; ibtnInRow < cbtnPerRow && ibtn < cbtn; ibtnInRow++)
		{
			Rect rcButton(rcRow);
			rcButton.Offset(ibtnInRow * (dxpButton + kdzpBorder), 0);
			rcButton.right = rcButton.left + dxpButton;
			DrawButton(hdc, ibtn, rcButton);
			ibtn++;
		}

		rcRow.top = rcRow.bottom;
		m_crowVisible++;
	}
	if (rcRow.top > rc.bottom)
		m_crowVisible--;
}


/*----------------------------------------------------------------------------------------------
	Draw the Recent Buttons section of the list.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayPalette::DrawRecentButtons(HDC hdc, Rect & rc)
{
	// Return if:
	//    we are not showing the most recent tags, or
	//    no tags have been selected yet, or
	//    the total number of tags in the overlay is <= kcRecentOverlayTag, or
	//    all the tags in the overlay are visible without scrolling.
	if (!m_fShowRecent || m_rgipssRecent[0] == -1)
		return;
	if (m_vipssButton.Size() <= kcRecentOverlayTag)
		return;
	SCROLLINFO si = { isizeof(si), SIF_RANGE };
	if (::GetScrollInfo(m_hwnd, SB_VERT, &si) && si.nMax == 0)
		return;

	int dxpButton;
	int cbtnPerRow = GetButtonCountPerRow(rc.Width(), &dxpButton);

	Rect rcButton(rc.left + kdzpBorder, rc.top + kdzpBorder);

	int ibtnInRow = 0;
	for (int ibtnInRecent = 0;
		ibtnInRecent < kcRecentOverlayTag && m_rgipssRecentSrt[ibtnInRecent] != -1;
		ibtnInRecent++)
	{
		int ibtn = ButtonFromPss(m_rgipssRecentSrt[ibtnInRecent]);
		if (ibtn > -1)
		{
			if (ibtnInRow++ == cbtnPerRow)
			{
				ibtnInRow = 1;
				rcButton.left = rc.left + kdzpBorder;
				rcButton.top = rcButton.bottom + kdzpBorder;
			}
			rcButton.right = rcButton.left + dxpButton;
			rcButton.bottom = rcButton.top + m_dypButton;
			DrawButton(hdc, ibtn, rcButton);
			rcButton.left = rcButton.right + kdzpBorder;
		}
	}

	Rect rcEdge(rc.left + kdzpBorder, rcButton.bottom + (kdzpBorder * 2),
		rc.right - kdzpBorder, 0);
	rcEdge.bottom = rcEdge.top + 2;
	::DrawEdge(hdc, &rcEdge, BDR_SUNKENOUTER, BF_RECT);

	rc.top = rcEdge.bottom + kdzpBorder;
}


/*----------------------------------------------------------------------------------------------
	Draw a single button.
----------------------------------------------------------------------------------------------*/
void AfTagOverlayPalette::DrawButton(HDC hdc, int ibtn, Rect & rc)
{
	int ipss = m_vipssButton[ibtn];
	PossListInfo * ppli = m_ptot->GetPossListInfo();
	AssertPtr(ppli);
	PossItemInfo * ppii = ppli->GetPssFromIndex(ipss);
	AssertPtr(ppii);

	StrUni stuText;
	ppii->GetName(stuText, m_pnt);
	StrApp strText(stuText);

	Rect rcBack(rc);
	rcBack.Inflate(-kdzpMargin, -kdzpMargin);

	bool fEnabled = m_ptot->IsEnabled() && m_ptot->TagIndexFromPss(ipss) >= 0;
	if (!fEnabled)
	{
		::DrawEdge(hdc, &rc, BDR_RAISEDINNER, BF_RECT);

		// Draw the text of the button.
		Rect rcText(rcBack);
		rcText.bottom -= 2;
		TEXTMETRIC tm;
		::GetTextMetrics(hdc, &tm);
		Rect rcT(rcText);
		::DrawText(hdc, strText.Chars(), strText.Length(), &rcText, DT_END_ELLIPSIS | DT_LEFT |
			DT_NOPREFIX | DT_SINGLELINE | DT_VCENTER | DT_CALCRECT);
		//rcText.Offset((rcT.Width() - rcText.Width()) / 2, (rcT.Height() - rcText.Height()) / 2);
		rcText.Offset(2, (rcT.Height() - rcText.Height()) / 2);
		Rect rcShadow(rcText);
		rcShadow.Offset(1, 1);
		AfGfx::SetTextColor(hdc, ::GetSysColor(COLOR_3DHIGHLIGHT));
		::DrawText(hdc, strText.Chars(), strText.Length(), &rcShadow, DT_END_ELLIPSIS | DT_LEFT |
			DT_NOPREFIX | DT_SINGLELINE);
		AfGfx::SetTextColor(hdc, ::GetSysColor(COLOR_3DSHADOW));
		::DrawText(hdc, strText.Chars(), strText.Length(), &rcText, DT_END_ELLIPSIS | DT_LEFT |
			DT_NOPREFIX | DT_SINGLELINE);
	}
	else
	{
		bool fPressed = ipss >= 0 && m_ptot->IsPssInSel(ipss);
		if (fPressed)
			AfGfx::FillSolidRect(hdc, rc, ::GetSysColor(COLOR_3DHIGHLIGHT));

		// Draw the edge of the button, along with the black square if this is the default
		// button.
		::DrawEdge(hdc, &rc, fPressed ? BDR_SUNKENOUTER : BDR_RAISEDINNER, BF_RECT);

		COLORREF clrFore = ppii->GetForeColor();
		COLORREF clrBack = ppii->GetBackColor();
		COLORREF clrUnder = ppii->GetUnderColor();
		int unt = ppii->GetUnderlineType();

		// Draw the background of the button.
		AfGfx::SetTextColor(hdc, clrFore);
		if (fPressed)
			rcBack.Offset(1, 1);
		if (clrBack != (COLORREF)kclrTransparent)
		{
			AfGfx::SetBkColor(hdc, clrBack);
			::ExtTextOut(hdc, 0, 0, ETO_OPAQUE, &rcBack, NULL, 0, NULL);
		}

		// Draw the text of the button.
		Rect rcText(rcBack);
		//rcText.bottom -= 2;
		TEXTMETRIC tm;
		::GetTextMetrics(hdc, &tm);
		// DT_CENTER does not center the text horizontally for some reason; maybe because of
		// the DT_END_ELLIPSIS flag, so we have to manually adjust the rcText rectangle.
		Rect rcT(rcText);
		::DrawText(hdc, strText.Chars(), strText.Length(), &rcText, DT_END_ELLIPSIS | DT_LEFT |
			DT_NOPREFIX | DT_SINGLELINE | DT_VCENTER | DT_CALCRECT);
		//rcText.Offset((rcT.Width() - rcText.Width()) / 2, (rcT.Height() - rcText.Height()) / 2);
		rcText.Offset(2, (rcT.Height() - rcText.Height()) / 2);
		::DrawText(hdc, strText.Chars(), strText.Length(), &rcText, DT_END_ELLIPSIS | DT_LEFT |
			DT_NOPREFIX | DT_SINGLELINE);

		// Draw the underline of the button.
		if (unt != kuntNone)
		{
			int yp = rcText.top + tm.tmAscent + 1;
			for (int xp = rcText.left; xp < rcText.right; xp++)
			{
				switch (unt)
				{
				case kuntDotted:
					::SetPixel(hdc, xp, yp, clrUnder);
					xp += 2;
					break;
				case kuntDashed:
					::SetPixel(hdc, xp, yp, clrUnder);
					if ((xp - rcText.left + 2) % 6 == 0)
						xp += 2;
					break;
				case kuntDouble:
					::SetPixel(hdc, xp, yp + 2, clrUnder);
					// Fall through.
				case kuntSingle:
					::SetPixel(hdc, xp, yp, clrUnder);
					break;
				}
			}
		}
	}

	if (ipss == m_ptot->GetSelectedPss())
	{
		PenWrap xpwr(PS_SOLID, 0, 0, hdc);
		::MoveToEx(hdc, rc.left - 1, rc.top - 1, NULL);
		::LineTo(hdc, rc.right, rc.top - 1);
		::LineTo(hdc, rc.right, rc.bottom);
		::LineTo(hdc, rc.left - 1, rc.bottom);
		::LineTo(hdc, rc.left - 1, rc.top - 1);

		if (::GetFocus() == m_hwnd)
		{
			Rect rcFocus(rcBack);
			rcFocus.Inflate(2, 2);
			AfGfx::SetTextColor(hdc, ::GetSysColor(COLOR_BTNTEXT));
			AfGfx::SetBkColor(hdc, ::GetSysColor(COLOR_3DFACE));
			::DrawFocusRect(hdc, &rcFocus);
		}
	}
}


/***********************************************************************************************
	AfTagTypeAheadEdit methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfTagTypeAheadEdit::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_GETDLGCODE)
	{
		lnRet = DefWndProc(wm, wp, lp) | DLGC_WANTALLKEYS;
		return true;
	}
	if (wm == WM_KEYDOWN)
	{
		if (wp == VK_TAB)
		{
			// Since we are capturing all the keystrokes (which was necessary to get the <Enter>
			// key), hitting <Tab> will not move the focus to the next sibling window. We
			// are trapping that case here and doing it manually.
			::SetFocus(::GetNextDlgTabItem(::GetParent(m_hwnd), m_hwnd,
				::GetKeyState(VK_SHIFT) < 0));
			return true;
		}
		if (wp == VK_ESCAPE)
		{
			// Since we are capturing all the keystrokes (which was necessary to get the <Enter>
			// key), hitting <Escape> will not send a WM_CLOSE message to the dialog window. We
			// are trapping that case here and doing it manually.
			::SendMessage(::GetParent(m_hwnd), WM_CLOSE, 0, 0);
			return true;
		}
		DWORD ichStart;
		DWORD ichStop;
		::SendMessage(m_hwnd, EM_GETSEL, (WPARAM)&ichStart, (LPARAM)&ichStop);
		s_fExtraBackspace = ichStart != ichStop && wp == VK_BACK;
	}
	else if (wm == WM_CHAR && wp == VK_RETURN)
	{
		HWND hwndParent = ::GetParent(m_hwnd);
		AfWnd * pwnd = AfWnd::GetAfWnd(hwndParent);
		AssertPtr(pwnd);
		AfTagOverlayTool * ptot = dynamic_cast<AfTagOverlayTool *>(pwnd);
		if (ptot)
			ptot->ClickPss(-1);
	}
	else if (wm == WM_CHAR && (wp == VK_TAB || wp == VK_ESCAPE || wp == VK_RETURN))
	{
		// This is necessary to avoid a beep, since tab/escape/return is an
		// invalid character in the edit box.
		return true;
	}
	else if (wm == WM_SETFOCUS)
	{
		// For some reason, the EM_SETSEL message has to be posted to the window.
		// You can't use SendMessage.
		::PostMessage(m_hwnd, EM_SETSEL, 0, -1);
	}

	return AfWnd::FWndProc(wm, wp, lp, lnRet);
}


/***********************************************************************************************
	TlsOptDlgOvr methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
TlsOptDlgOvr::TlsOptDlgOvr(TlsOptDlg * ptod)
{
	AssertPtr(ptod);
	m_ptod = ptod;
	m_rid = kridTlsOptDlgOvr;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Options/Options_Overlays_tab.htm");
	m_clrTagFore = (COLORREF)kclrTransparent;
	m_clrTagBack = (COLORREF)kclrTransparent;
	m_clrTagUnder = (COLORREF)kclrTransparent;
	m_iovrInitial = 0;
	m_iovrCurrent = -1;
	m_itagInitial = 0;
	m_fUpdating = false;
	m_fModified = false;
	m_hfont = NULL;
	m_himlTag = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
TlsOptDlgOvr::~TlsOptDlgOvr()
{
	if (m_hfont)
	{
		AfGdi::DeleteObjectFont(m_hfont);
		m_hfont = NULL;
	}
	if (m_himlTag)
	{
		AfGdi::ImageList_Destroy(m_himlTag);
		m_himlTag = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal. This method will
	result in a dialog allowing only a single selection.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgOvr::SetDialogValues(AfLpInfo * plpi, int iovr, int itag)
{
	AssertPtr(plpi);

	m_qlpi = plpi;
	m_iovrInitial = iovr;
	m_itagInitial = itag;
}


/*----------------------------------------------------------------------------------------------
	Gets the final values for the dialog controls, after the dialog has been closed.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgOvr::GetDialogValues(Vector<TlsOptDlgOvr::OverlayInfo> & voi)
{
	voi = m_voi;
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgOvr::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Subclass the tag foreground color combo box.
	m_qccmbFore.Create();
	m_qccmbFore->SubclassButton(::GetDlgItem(m_hwnd, kctidTagFore), &m_clrTagFore, true);

	// Subclass the tag background color combo box.
	m_qccmbBack.Create();
	m_qccmbBack->SubclassButton(::GetDlgItem(m_hwnd, kctidTagBack), &m_clrTagBack, true);

	// Subclass the tag underline color combo box.
	m_qccmbUnder.Create();
	m_qccmbUnder->SubclassButton(::GetDlgItem(m_hwnd, kctidTagUnderColor), &m_clrTagUnder,
		true);

	// Subclass the type-ahead edit control.
	AfTagTypeAheadEditPtr qttae;
	qttae.Create();
	qttae->SubclassEdit(::GetDlgItem(m_hwnd, kctidTagEdit));

	// Replace the two buttons with icons on them with AfButton windows.
	HIMAGELIST himl = g_tog.GetImageList();
	AfButtonPtr qbtn;

	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidTagChoose, kbtImage, himl, 2);

	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidTagAdvanced, kbtImage, himl, 1);

	// Since the Underline style combobox is ownerdraw, we just insert 5 empty items here.
	HWND hwndCombo = ::GetDlgItem(m_hwnd, kctidTagUnderStyle);
	for (int i = 0; i < 5; i++)
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)_T(""));

	LVCOLUMN lvc = { LVCF_TEXT | LVCF_WIDTH };
	HWND hwndList = ::GetDlgItem(m_hwnd, kctidTagList);
	Rect rc;
	::GetClientRect(hwndList, &rc);
	lvc.cx = 50;
	ListView_InsertColumn(hwndList, 0, &lvc);
	lvc.cx = rc.Width() - lvc.cx;
	ListView_InsertColumn(hwndList, 1, &lvc);
	ListView_SetExtendedListViewStyle(hwndList, LVS_EX_FULLROWSELECT);
	UpdateTagList();

	hwndList = ::GetDlgItem(m_hwnd, kctidOverlayList);
	::GetClientRect(hwndList, &rc);
	lvc.cx = rc.Width();
	ListView_InsertColumn(hwndList, 0, &lvc);
	HIMAGELIST himlOld = ListView_SetImageList(hwndList, g_tog.GetImageList(), LVSIL_SMALL);
	if (himlOld)
		if (himlOld != g_tog.GetImageList())
			AfGdi::ImageList_Destroy(himlOld);

	// Load the overlays into the list view.
	int covr = m_qlpi->GetOverlayCount();
	for (int iovr = 0; iovr < covr; iovr++)
	{
		AppOverlayInfo aoi = m_qlpi->GetOverlayInfo(iovr);
		OverlayInfo oi;
		oi.m_stuName = aoi.m_stuName;
		oi.m_hvo = aoi.m_hvo;
		oi.m_hvoPssl = aoi.m_hvoPssl;
		oi.m_os = kosNormal;
		m_voi.Push(oi);
	}
	m_iovrCurrent = m_iovrInitial;
	UpdateOverlayList();

	return true;
}


/*----------------------------------------------------------------------------------------------
	Apply the changes to the overlays that were made in the dialog.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgOvr::Apply()
{
	if (!m_hwnd)
		return true;

	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stuQuery;

	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);

	pdbi->GetDbAccess(&qode);
	try
	{
		qode->BeginTrans();
		CheckHr(qode->CreateCommand(&qodc));

		// Create a temporary cache for saving to the database.
		IVwOleDbDaPtr qda;
		qda.CreateInstance(CLSID_VwOleDbDa);
		// Gather up relevant interfaces needed here.
		ISetupVwOleDbDaPtr qods;
		CheckHr(qda->QueryInterface(IID_ISetupVwOleDbDa, (void**)&qods));
		Assert(qods);
		ISilDataAccessPtr qsda;
		CheckHr(qda->QueryInterface(IID_ISilDataAccess, (void**)&qsda));
		Assert(qsda);

		IFwMetaDataCachePtr qmdc;
		ILgWritingSystemFactoryPtr qwsf;
		pdbi->GetFwMetaDataCache(&qmdc);
		pdbi->GetLgWritingSystemFactory(&qwsf);
		qods->Init(qode, qmdc, qwsf, NULL);

		// Save all the modified settings to the database.
		int covr = m_voi.Size();
		for (int iovr = 0; iovr < covr; iovr++)
		{
			OverlayInfo & oi = m_voi[iovr];

			if (oi.m_os == kosDeleted)
			{
				// oi.m_hvo might be NULL if the user deleted an overlay that was just created.
				if (oi.m_hvo)
				{
					// The overlay and all its tags should be deleted.
					pdbi->DeleteObject(oi.m_hvo);
				}
				m_fModified = true;
				continue;
			}

			if (oi.m_os == kosInserted)
			{
				// Create a new overlay.
				Assert(oi.m_hvo == NULL);
				qsda->MakeNewObject(kclidCmOverlay, m_qlpi->GetLpId(), kflidLangProject_Overlays,
					-1, &(oi.m_hvo));
			}

			if (oi.m_os != kosNormal)
			{
				// The overlay has been changed, so update the overlay information as well as the
				// overlay tag information. This includes new, modified, and deleted tags.
				stuQuery.Format(L"update CmOverlay set Name = ?, PossList = %d "
					L"where id = %d\n", oi.m_hvoPssl, oi.m_hvo);
				StrUtil::NormalizeStrUni(oi.m_stuName, UNORM_NFD);
				CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
					(ULONG *)(oi.m_stuName.Chars()), oi.m_stuName.Length() * 2));
				CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
				m_fModified = true;

				StrUni stuDelete;
				StrUni stuModify;
				stuDelete.Format(L"DELETE FROM CmOverlay_PossItems WHERE Src = %d and Dst IN (",
					oi.m_hvo);
				int cotiDel = 0;

				int coti = oi.m_voti.Size();
				for (int ioti = 0; ioti < coti; ioti++)
				{
					OverlayTagInfo & oti = oi.m_voti[ioti];
					if (oti.m_os == kosDeleted)
					{
						if (++cotiDel == 1)
							stuDelete.FormatAppend(L"%d", oti.m_hvoPss);
						else
							stuDelete.FormatAppend(L", %d", oti.m_hvoPss);
					}
					else if (oti.m_os != kosNormal)
					{
						Assert(oti.m_os == kosModified || oti.m_os == kosInserted);
						if (oti.m_os == kosInserted)
						{
							stuModify.FormatAppend(
								L"INSERT INTO CmOverlay_PossItems (Src,Dst) VALUES (%d,%d);%n"
								L"UPDATE CmPossibility SET%n"
								L"    ForeColor = %d, BackColor = %d,%n"
								L"    UnderColor = %d, UnderStyle = %d, Hidden = %d%n"
								L"WHERE [id] = %d;%n",
								oi.m_hvo, oti.m_hvoPss, oti.m_clrFore, oti.m_clrBack,
								oti.m_clrUnder, oti.m_unt, 0, oti.m_hvoPss);
						}
						else
						{
							stuModify.FormatAppend(
								L"UPDATE CmPossibility SET%n"
								L"    ForeColor = %d, BackColor = %d,%n"
								L"    UnderColor = %d, UnderStyle = %d, Hidden = %d%n"
								L"WHERE [id] = %d;%n",
								oti.m_clrFore, oti.m_clrBack, oti.m_clrUnder, oti.m_unt,
								0, oti.m_hvoPss);
						}
					}
				}
				if (cotiDel > 0)
				{
					stuDelete.Append(L")");
					CheckHr(qodc->ExecCommand(stuDelete.Bstr(), knSqlStmtNoResults));
				}
				if (stuModify.Length() > 0)
					CheckHr(qodc->ExecCommand(stuModify.Bstr(), knSqlStmtNoResults));

				// Force the list to be reloaded from the database.
				PossListInfoPtr qpli;
				m_qlpi->LoadPossList(oi.m_hvoPssl, m_qlpi->GetPsslWsFromDb(oi.m_hvoPssl), &qpli);
				qpli->FullRefresh();
			}
		}
		qode->CommitTrans();
	}
	catch(...)
	{
		qode->RollbackTrans();
		throw;	// For now we have nothing to add, so pass it on up.
	}

	qodc.Clear();
	qode.Clear();

	// Fix the sort order of the internal vector of overlay information. Make sure the old
	// values in m_voi get saved to the database before we update it with the new sort order.
	LVITEM lvi = { LVIF_PARAM };
	Vector<OverlayInfo> voi;
	HWND hwndList = ::GetDlgItem(m_hwnd, kctidOverlayList);
	int citem = ListView_GetItemCount(hwndList);
	for (int iitem = 0; iitem < citem; iitem++)
	{
		lvi.iItem = iitem;
		if (!ListView_GetItem(hwndList, &lvi))
			ThrowHr(E_UNEXPECTED);
		Assert((uint)lvi.lParam < (uint)m_voi.Size());
		voi.Push(m_voi[lvi.lParam]);
	}
	Assert(voi.Size() == citem);
	m_voi = voi;

	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgOvr::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_ERASEBKGND)
	{
		// This is needed because of a bug in the list view control that causes
		// it not to be redrawn sometimes.
		::RedrawWindow(::GetDlgItem(m_hwnd, kctidOverlayList), NULL, NULL,
			RDW_ERASE | RDW_FRAME | RDW_INVALIDATE);
		::RedrawWindow(::GetDlgItem(m_hwnd, kctidTagList), NULL, NULL,
			RDW_ERASE | RDW_FRAME | RDW_INVALIDATE);
	}
	else if (wm == WM_TIMER)
	{
		if (wp == kidOverlayTagSettings)
		{
			::KillTimer(m_hwnd, kidOverlayTagSettings);
			return OnTagChange();
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgOvr::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case LVN_ITEMCHANGED:
		{
			NMLISTVIEW * pnmlv = (NMLISTVIEW *)pnmh;
			if (ctidFrom == kctidOverlayList && (pnmlv->uChanged & LVIF_STATE) &&
				(pnmlv->uNewState & LVIS_SELECTED))
			{
				return OnOverlayChange(pnmlv->lParam);
			}
			if (ctidFrom == kctidTagList && (pnmlv->uChanged & LVIF_STATE))
			{
				// Since this happens for every change to every tag, we don't want to
				// update the tag properties section right away. Instead, we create a
				// timer, so the update only happens once, after the state of all the
				// tags have been changed.
				::SetTimer(m_hwnd, kidOverlayTagSettings, knOverlayTagChange, NULL);
			}
			break;
		}

	case LVN_MARQUEEBEGIN:
		lnRet = 1; // Cancel the bounding box selection.
		return true;

	case LVN_GETDISPINFO:
		return OnGetDispInfo((NMLVDISPINFO *)pnmh);

	case LVN_KEYDOWN:
		{
			NMLVKEYDOWN * pnmlvkd = (NMLVKEYDOWN *)pnmh;
			if (pnmlvkd->wVKey == VK_DELETE)
			{
				if (pnmh->idFrom == kctidOverlayList)
				{
					DeleteOverlay();
					return true;
				}
			}
			else if (pnmlvkd->wVKey == VK_F2)
			{
				int iitem = ListView_GetNextItem(pnmh->hwndFrom, -1, LVNI_SELECTED);
				if (iitem != -1)
					ListView_EditLabel(pnmh->hwndFrom, iitem);
			}
			break;
		}

	case LVN_ENDLABELEDIT:
		Assert(pnmh->idFrom == kctidOverlayList);
		return OnEndLabelEdit((NMLVDISPINFO *)pnmh, lnRet);

	case NM_CLICK: // Fall-through.
	case NM_DBLCLK:
		{
			// If the user clicked somewhere so that the listview no longer has any selected
			// items, select the item that currently has the focus.
			if (ListView_GetSelectedCount(pnmh->hwndFrom) == 0)
			{
				int itag = ListView_GetNextItem(pnmh->hwndFrom, -1, LVNI_FOCUSED);
				ListView_SetItemState(pnmh->hwndFrom, itag, LVIS_SELECTED, LVIS_SELECTED);
			}
			break;
		}

	case NM_CUSTOMDRAW:
		if (ctidFrom == kctidTagList)
			return OnCustomDrawTagList((NMLVCUSTOMDRAW *)pnmh, lnRet);
		break;

	case CBN_SELENDOK:
		::InvalidateRect(::GetDlgItem(m_hwnd, kctidTagUnderStyle), NULL, true);
		OnComboSelEnd(ctidFrom);
		return true;

	case BN_CLICKED:
		switch (ctidFrom)
		{
		case kctidTagChoose:
			ChooseTags();
			return true;

		case kctidTagAdvanced:
			{
				AdvOverlayDlgPtr qaod;
				qaod.Create();
				AfDbInfo * pdbi = m_qlpi->GetDbInfo();
				AssertPtr(pdbi);
				qaod->SetUserWs(pdbi->UserWs());
				if (qaod->DoModal(m_hwnd) == kctidOk)
				{
					m_fModified = true;
					UpdateTagList();
				}
			}
			return true;

		case kctidOvrAdd:
			AddOverlay();
			return true;

		case kctidOvrCopy:
			CopyOverlay();
			return true;

		case kctidOvrDelete:
			DeleteOverlay();
			return true;
		}
		break;

	case EN_UPDATE:
		if (!m_fUpdating)
		{
			if (ctidFrom == kctidTagEdit)
				return OnTagEditUpdate();
		}
		break;

	default:
		break;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Update the overlay name.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgOvr::OnEndLabelEdit(NMLVDISPINFO * plvdi, long & lnRet)
{
	AssertPtr(plvdi);

	if (plvdi->item.pszText)
	{
		AssertPsz(plvdi->item.pszText);

		// Trim leading and trailing space characters.
		StrApp strLabel;
		StrUtil::TrimWhiteSpace(plvdi->item.pszText, strLabel);

		if (strLabel.Length() == 0)
		{
			// The item is empty, so show a message complaining about it.
			StrApp strMessage(kstidOvrRenEmptyMsg);
			StrApp strOverlay(kstidTlsOptOvr);
			::MessageBox(m_hwnd, strMessage.Chars(), strOverlay.Chars(),
				MB_OK | MB_ICONINFORMATION);
			::PostMessage(plvdi->hdr.hwndFrom, LVM_EDITLABEL, plvdi->item.iItem, 0);
			return true;
		}

		HWND hwndList = ::GetDlgItem(m_hwnd, kctidOverlayList);

		// See if there is already an item with the same name.
		LVFINDINFO lvfi = { LVFI_STRING };
		lvfi.psz = strLabel.Chars();
		int iitem = ListView_FindItem(hwndList, -1, &lvfi);
		int iovr = -1;
		if (iitem != -1)
		{
			LVITEM lvi = { LVIF_PARAM, iitem };
			ListView_GetItem(hwndList, &lvi);
			iovr = lvi.lParam;
		}
		// If they didn't change the name, we're done.
		if (iovr == m_iovrCurrent)
			return true;
		if (iovr != -1)
		{
			StrApp strMessage(kstidOvrRenOverlayMsg);
			StrApp strOverlay(kstidTlsOptOvr);
			::MessageBox(m_hwnd, strMessage.Chars(), strOverlay.Chars(),
				MB_OK | MB_ICONINFORMATION);
			::PostMessage(plvdi->hdr.hwndFrom, LVM_EDITLABEL, plvdi->item.iItem, 0);
			return true;
		}

		// Update the name of the selected overlay.
		OverlayInfo & oi = m_voi[m_iovrCurrent];
		Assert(oi.m_os != kosDeleted);
		oi.m_stuName = strLabel;
		if (oi.m_os == kosNormal)
			oi.m_os = kosModified;

		UpdateOverlayList();
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	The user selected a new overlay, so update the list of tags.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgOvr::OnOverlayChange(int iovr)
{
	Assert((uint)iovr < (uint)m_voi.Size());
	WaitCursor wc;

	m_iovrCurrent = iovr;

	OverlayInfo & oi = m_voi[iovr];
	if (!oi.m_qpli)
	{
		Assert(oi.m_hvo != 0);
		Assert(oi.m_voti.Size() == 0);
		Assert(oi.m_os == kosNormal);

		// This is the first time that this overlay has been looked at, so load it now.
		// Also, add all the tags in the overlay to m_voti;
		IVwOverlayPtr qvo;
		int ctag;
		HVO hvo;
		ComBool fHidden;
		GUID uid;
		int covr = m_qlpi->GetOverlayCount();
		for (int iovrT = 0; iovrT < covr; iovrT++)
		{
			AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(iovrT);
			if (aoi.m_hvo == oi.m_hvo)
			{
				m_qlpi->GetOverlay(iovrT, &qvo);
				break;
			}
		}
		AssertPtr(qvo);
		CheckHr(qvo->get_CTags(&ctag));
		oi.m_voti.Resize(ctag);
		for (int itag = 0; itag < ctag; itag++)
		{
			OverlayTagInfo & oti = oi.m_voti[itag];
			CheckHr(qvo->GetDbTagInfo(itag, &hvo, &oti.m_clrFore, &oti.m_clrBack,
				&oti.m_clrUnder, &oti.m_unt, &fHidden, (OLECHAR *)&uid));
			oti.m_hvoPss = hvo;
			oti.m_os = kosNormal;
		}

		// Cache a pointer to the possibility list that the tags come from.
		HVO hvoPssl;
		CheckHr(qvo->get_PossListId(&hvoPssl));
		m_qlpi->LoadPossList(hvoPssl, m_qlpi->GetPsslWsFromDb(hvoPssl), &oi.m_qpli);
	}

	int ctag = oi.m_voti.Size();

	HWND hwndList = ::GetDlgItem(m_hwnd, kctidTagList);
	::SendMessage(hwndList, WM_SETREDRAW, false, 0);
	ListView_DeleteAllItems(hwndList);

	// Clear out any existing name in the tag type-ahead edit box.
	::SetWindowText(::GetDlgItem(m_hwnd, kctidTagEdit), _T(""));

	if (ctag > 0)
	{
		LVITEM lvi = { LVIF_TEXT | LVIF_PARAM };
		lvi.pszText = LPSTR_TEXTCALLBACK;
		lvi.iItem = 0;

		for (int itag = 0; itag < ctag; itag++)
		{
			if (oi.m_voti[itag].m_os == kosDeleted)
				continue;

			lvi.lParam = itag;

			lvi.iSubItem = 0;
			ListView_InsertItem(hwndList, &lvi);

			lvi.iSubItem = 1;
			ListView_SetItem(hwndList, &lvi);

			lvi.iItem++;
		}

		if (lvi.iItem > 0)
		{
			Assert((uint)m_itagInitial < (uint)lvi.iItem);
			DWORD dwT = LVIS_SELECTED | LVIS_FOCUSED;
			ListView_SetItemState(hwndList, m_itagInitial, dwT, dwT);
			ListView_EnsureVisible(hwndList, m_itagInitial, false);
		}
	}

	// It seems that hiding a window still allows it to receive accelerator keys, so whenever
	// we hide a window, we have to disable it as well.
	int nCmdShow = ctag > 0 ? SW_SHOW : SW_HIDE;
	::ShowWindow(::GetDlgItem(m_hwnd, kstidOvrForeLabel), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidTagFore), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kstidOvrBackLabel), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidTagBack), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kstidOvrUnderLabel), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kstidOvrStyleLabel), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidTagUnderStyle), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kstidOvrColorLabel), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidTagUnderColor), nCmdShow);
	bool fEnable = ctag > 0;
	::EnableWindow(::GetDlgItem(m_hwnd, kstidOvrForeLabel), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidTagFore), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kstidOvrBackLabel), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidTagBack), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kstidOvrUnderLabel), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kstidOvrStyleLabel), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidTagUnderStyle), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kstidOvrColorLabel), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidTagUnderColor), fEnable);

	::SendMessage(hwndList, WM_SETREDRAW, true, 0);
	::InvalidateRect(hwndList, NULL, true);

	m_itagInitial = 0;

	return false;
}


/*----------------------------------------------------------------------------------------------
	The user selected a new tag, so update the controls that show the tag attributes.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgOvr::OnTagChange()
{
	COLORREF clrFore;
	COLORREF clrBack;
	COLORREF clrUnder;
	int unt;

	HWND hwndList = ::GetDlgItem(m_hwnd, kctidTagList);
	int itag = ListView_GetNextItem(hwndList, -1, LVNI_SELECTED);
	if (itag == -1)
		itag = ListView_GetNextItem(hwndList, -1, LVNI_FOCUSED);
	if (itag == -1)
		return false;

	OverlayInfo & oi = m_voi[m_iovrCurrent];
	int ipssFirst = 0; // Gives the possibility index of the first selected tag.
	int ctags = 0;
	while (itag != -1)
	{
		LVITEM lvi = { LVIF_PARAM, itag };
		if (!ListView_GetItem(hwndList, &lvi))
			return false;

		Assert((uint)lvi.lParam < (uint)oi.m_voti.Size());
		OverlayTagInfo & oti = oi.m_voti[lvi.lParam];

		if (ctags == 0)
		{
			clrFore = oti.m_clrFore;
			clrBack = oti.m_clrBack;
			clrUnder = oti.m_clrUnder;
			unt = oti.m_unt;
			ipssFirst = lvi.lParam;
		}
		else
		{
			if (clrFore != oti.m_clrFore)
				clrFore = (COLORREF)knConflicting;
			if (clrBack != oti.m_clrBack)
				clrBack = (COLORREF)knConflicting;
			if (clrUnder != oti.m_clrUnder)
				clrUnder = (COLORREF)knConflicting;
			unt = knConflicting;
		}

		ctags++;
		itag = ListView_GetNextItem(hwndList, itag, LVNI_SELECTED);
	}
	Assert(ctags > 0);

	m_qccmbFore->SetColor(clrFore);
	m_qccmbBack->SetColor(clrBack);
	m_qccmbUnder->SetColor(clrUnder);
	if (unt == knConflicting)
		::SendMessage(::GetDlgItem(m_hwnd, kctidTagUnderStyle), CB_SETCURSEL, (WPARAM)-1, 0);
	else
		::SendMessage(::GetDlgItem(m_hwnd, kctidTagUnderStyle), CB_SETCURSEL, unt, 0);

	StrApp str;
	if (ctags == 1)
	{
		OverlayTagInfo & oti = oi.m_voti[ipssFirst];
		PossListInfo * ppli = NULL;
		int ipss = oi.m_qpli->GetIndexFromId(oti.m_hvoPss, &ppli);
		if (ppli)
			oi.m_qpli = ppli;
		PossItemInfo * ppii = oi.m_qpli->GetPssFromIndex(ipss);
		AssertPtr(ppii);
		StrUni stu;
		ppii->GetName(stu);
		str = stu;
	}
	else
	{
		str.Load(kstidOvlyMultipleSel);
	}

	// Don't change the edit box test if it matches the new text. This is necessary
	// for the type-ahead to work properly.
	HWND hwndEdit = ::GetDlgItem(m_hwnd, kctidTagEdit);
	achar rgch[MAX_PATH];
	::SendMessage(hwndEdit, WM_GETTEXT, MAX_PATH, (LPARAM)rgch);
	if (!str.Equals(rgch))
	{
		m_fUpdating = true;
		::SendMessage(hwndEdit, WM_SETTEXT, 0, (LPARAM)str.Chars());
		m_fUpdating = false;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Return the information needed to draw the list view item in the tags list view.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgOvr::OnGetDispInfo(NMLVDISPINFO * pnmdi)
{
	AssertPtr(pnmdi);

	if (pnmdi->item.mask & LVIF_TEXT)
	{
		// This callback happens every time an item in the tag list view is drawn.
		LVITEM lvi = { LVIF_PARAM };
		lvi.iItem = pnmdi->item.iItem;
		ListView_GetItem(::GetDlgItem(m_hwnd, kctidTagList), &lvi);
		int ioti = lvi.lParam;

		OverlayInfo & oi = m_voi[m_iovrCurrent];
		Assert((uint)ioti < (uint)oi.m_voti.Size());
		OverlayTagInfo & oti = oi.m_voti[ioti];

		PossListInfo * ppli = NULL;
		int ipss = oi.m_qpli->GetIndexFromId(oti.m_hvoPss, &ppli);
		// If a possibility item is deleted from the list chooser, this method gets called
		// to update the list box before we even get back from the chooser. But by now, the
		// item has been deleted from the list. In this case, we ignore this call to update
		// an item. The list box will be properly updated as soon as the dialog box is closed,
		// so the user will not see any problem.
		if (ipss < 0)
			return false;
		if (ppli)
			oi.m_qpli = ppli;
		PossItemInfo * ppii = oi.m_qpli->GetPssFromIndex(ipss);
		AssertPtr(ppii);
		StrUni stu;

		// The first column is the tag abbreviation; the second column is the tag name.
		if (pnmdi->item.iSubItem == 0)
		{
			ppii->GetName(stu, kpntAbbreviation);
			StrApp str(stu);
			_tcsncpy_s(pnmdi->item.pszText, pnmdi->item.cchTextMax, str.Chars(), str.Length()+1);
		}
		else
		{
			Assert(pnmdi->item.iSubItem == 1);
			ppii->GetName(stu, kpntName);
			StrApp str(stu);
			_tcsncpy_s(pnmdi->item.pszText, pnmdi->item.cchTextMax, str.Chars(), str.Length()+1);
		}
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Return the size of the owner-draw underline style combo box.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgOvr::OnMeasureChildItem(MEASUREITEMSTRUCT * pmis)
{
	AssertPtr(pmis);

	// TODO DarrellZ: Figure out why this method isn't ever getting called.
	if (pmis->CtlID == kctidTagUnderStyle)
	{
		Rect rc;
		::GetWindowRect(::GetDlgItem(m_hwnd, kctidTagUnderStyle), &rc);

		int dypMargin = ::GetSystemMetrics(SM_CYEDGE);
		int dypArrow = Max(::GetSystemMetrics(SM_CYVTHUMB), 5 * dypMargin);

		pmis->itemWidth = rc.Width();
		pmis->itemHeight = dypArrow + 2 * dypMargin;
	}

	return SuperClass::OnMeasureChildItem(pmis);
}


/*----------------------------------------------------------------------------------------------
	Draw the appropriate underline style.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgOvr::OnDrawChildItem(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);

	if (pdis->CtlID == kctidTagUnderStyle)
	{
		int unt = pdis->itemID;

		COLORREF clr = kclrWhite;
		Rect rcBack(pdis->rcItem);
		rcBack.Inflate(-1, -1);
		if (unt >= kuntMin && m_clrTagBack != kclrTransparent && unt != kuntNone)
			clr = m_clrTagBack;
		AfGfx::FillSolidRect(pdis->hDC, rcBack, clr);

		if (unt == kuntNone)
		{
			Rect rcT(pdis->rcItem);
			rcT.left += 3;
			StrApp str(kstidOverlayNone);
			::DrawText(pdis->hDC, str.Chars(), -1, &rcT, DT_SINGLELINE | DT_VCENTER);
		}
		else if (unt > kuntNone)
		{
			int yp = pdis->rcItem.top + (pdis->rcItem.bottom - pdis->rcItem.top) / 2;
			int xpStart = pdis->rcItem.left + 3;
			int xpStop = pdis->rcItem.right - 3;
			for (int xp = xpStart; xp < xpStop; xp++)
			{
				switch (unt)
				{
				case kuntDotted:
					::SetPixel(pdis->hDC, xp, yp, m_clrTagUnder);
					xp += 2;
					break;
				case kuntDashed:
					::SetPixel(pdis->hDC, xp, yp, m_clrTagUnder);
					if ((xp - xpStart + 2) % 6 == 0)
						xp += 2;
					break;
				case kuntSingle:
					::SetPixel(pdis->hDC, xp, yp, m_clrTagUnder);
					break;
				case kuntDouble:
					::SetPixel(pdis->hDC, xp, yp - 1, m_clrTagUnder);
					::SetPixel(pdis->hDC, xp, yp + 1, m_clrTagUnder);
					break;
				}
			}
		}

		if (pdis->itemState & ODS_FOCUS)
			::DrawFocusRect(pdis->hDC, &rcBack);
	}

	return SuperClass::OnDrawChildItem(pdis);
}


/*----------------------------------------------------------------------------------------------
	This shows the foreground and background of each tag in the tag list view.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgOvr::OnCustomDrawTagList(NMLVCUSTOMDRAW * pncd, long & lnRet)
{
	AssertPtr(pncd);

	if (pncd->nmcd.dwDrawStage == CDDS_PREPAINT)
	{
		lnRet = CDRF_NOTIFYITEMDRAW;
		return true;
	}

	if (pncd->nmcd.dwDrawStage == CDDS_ITEMPREPAINT)
	{
		lnRet = CDRF_NOTIFYSUBITEMDRAW;
		return true;
	}


	if (pncd->nmcd.dwDrawStage == (CDDS_SUBITEM | CDDS_ITEMPREPAINT))
	{
		if (pncd->iSubItem == 0)
		{
			HWND hwndItem = ::GetDlgItem(m_hwnd, kctidTagList);
			HDC hdc = pncd->nmcd.hdc;
			int ioti = pncd->nmcd.lItemlParam;
			OverlayInfo & oi = m_voi[m_iovrCurrent];
			Assert((uint)ioti < (uint)oi.m_voti.Size());
			OverlayTagInfo & oti = oi.m_voti[ioti];
			Rect rcLabel;
			if (ListView_GetItemRect(hwndItem, pncd->nmcd.dwItemSpec, &rcLabel, LVIR_LABEL))
			{
				int x = rcLabel.left + 3; // Offset text three pixels.
				int y = rcLabel.top + 2; // 2 pixels above, 1 below the text.
				if (oti.m_unt == kuntDouble)
				{
					// A double-underline takes up two extra pixels, so subtract one from the
					// top.
					y--;
				}
				else if (oti.m_unt == kuntNone)
				{
					// No underline gives us two extra pixels, so add one to the top.
					y++;
				}

				achar rgchText[256];
				ListView_GetItemText(hwndItem, ioti, 0, rgchText, 256);
				StrApp str(rgchText);

				// Set the font and text and background colors.
				COLORREF clrTextOld;
				COLORREF clrBkOld;
				HFONT hfontOld = AfGdi::SelectObjectFont(hdc, m_hfont);
				if (oti.m_clrFore == kclrTransparent)
					clrTextOld = ::SetTextColor(hdc, ::GetSysColor(COLOR_WINDOWTEXT));
				else
					clrTextOld = ::SetTextColor(hdc, oti.m_clrFore);
				if (oti.m_clrBack == kclrTransparent)
					clrBkOld = ::SetBkColor(hdc, ::GetSysColor(COLOR_WINDOW));
				else
					clrBkOld = ::SetBkColor(hdc, oti.m_clrBack);

				// Calculate the size of the shaded rectangle.
				SIZE size;
				::GetTextExtentPoint32(hdc, str.Chars(), str.Length(), &size);
				// The shaded rectangle should cover three pixels to the left and right of the
				// text, one pixel above, and one pixel below the underline (or the text)
				// if there isn't any underline.
				Rect rcColor(x - 3, y, x + size.cx + 3, y + size.cy);
				if (oti.m_unt == kuntDouble)
					rcColor.bottom += 4;
				else if (oti.m_unt != kuntNone)
					rcColor.bottom += 2;

				// Draw the text.
				int nAlignOld = ::SetTextAlign(hdc, TA_LEFT | TA_TOP | TA_NOUPDATECP);
				::ExtTextOut(hdc, x, y, ETO_OPAQUE, &rcColor, str.Chars(), str.Length(), NULL);
				::SetTextAlign(hdc, nAlignOld);

				// Restore the old font and colors.
				::SetTextColor(hdc, clrTextOld);
				::SetBkColor(hdc, clrBkOld);
				AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);

				if (oti.m_unt != kuntNone)
				{
					// Draw the underlining of the text. (Underline is 1 pixel below baseline.)
					y += m_dypText + 1;
					int x1;
					int x2;
					int xEnd = x + size.cx;
					// Create a pen of the proper color, and select it.
					// (pen is 1 pixel wide.)
					HPEN hpen = ::CreatePen(PS_SOLID, 0, oti.m_clrUnder);
					HPEN hpenOld = (HPEN)::SelectObject(hdc, hpen);
					switch (oti.m_unt)
					{
					case kuntSingle:
						::MoveToEx(hdc, x, y, NULL);
						::LineTo(hdc, xEnd, y);
						break;
					case kuntDouble:
						::MoveToEx(hdc, x, y, NULL);
						::LineTo(hdc, xEnd, y);
						::MoveToEx(hdc, x, y + 2, NULL);
						::LineTo(hdc, xEnd, y + 2);
						break;
					case kuntDashed:
						for (x1 = x; x1 < xEnd; x1 += 9)
						{
							::MoveToEx(hdc, x1, y, NULL);
							x2 = x1 + 6;
							if (x2 > xEnd)
								x2 = xEnd;
							::LineTo(hdc, x2, y);
						}
						break;
					case kuntDotted:
						for (x1 = x; x1 < xEnd; x1 += 4)
						{
							::MoveToEx(hdc, x1, y, NULL);
							x2 = x1 + 2;
							if (x2 > xEnd)
								x2 = xEnd;
							::LineTo(hdc, x2, y);
						}
						break;
					}
					// Clean up the pen, restoring the original value.
					::SelectObject(hdc, hpenOld);
					::DeleteObject(hpen);
				}
			}
			lnRet = CDRF_SKIPDEFAULT;
		}
		else
		{
			// Let Windows draw the second column.
			lnRet = CDRF_DODEFAULT;
		}
		return true;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	The user changes an attribute of the current tag in the current overlay.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgOvr::OnComboSelEnd(int ctid)
{
	OverlayInfo & oi = m_voi[m_iovrCurrent];
	HWND hwndList = ::GetDlgItem(m_hwnd, kctidTagList);
	LVITEM lvi = { LVIF_PARAM };
	int unt = ::SendMessage(::GetDlgItem(m_hwnd, kctidTagUnderStyle), CB_GETCURSEL, 0, 0);
	int & itag = lvi.iItem = ListView_GetNextItem(hwndList, -1, LVNI_SELECTED);
	while (itag != -1)
	{
		if (ListView_GetItem(hwndList, &lvi))
		{
			int ioti = lvi.lParam;
			Assert((uint)ioti < (uint)oi.m_voti.Size());
			OverlayTagInfo & oti = oi.m_voti[ioti];

			switch (ctid)
			{
			case kctidTagFore:
				oti.m_clrFore = m_clrTagFore;
				break;

			case kctidTagBack:
				oti.m_clrBack = m_clrTagBack;
				break;

			case kctidTagUnderStyle:
				oti.m_unt = unt;
				break;

			case kctidTagUnderColor:
				oti.m_clrUnder = m_clrTagUnder;
				break;

			default:
				Assert(false); // This should never happen.
				break;
			}
			Assert(oti.m_os != kosDeleted);
			if (oti.m_os == kosNormal)
			{
				oti.m_os = kosModified;
				oi.m_os = kosModified;
			}
		}

		// Update the current listview item with the tag changes.
		ListView_RedrawItems(hwndList, itag, itag);

		itag = ListView_GetNextItem(hwndList, itag, LVNI_SELECTED);
	}
}


/*----------------------------------------------------------------------------------------------
	Update the listview with the current overlays.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgOvr::UpdateOverlayList()
{
	HWND hwndList = ::GetDlgItem(m_hwnd, kctidOverlayList);

	int iitemOld = 0;
	if (m_iovrCurrent != -1)
	{
		LVFINDINFO lvfi = { LVFI_PARAM };
		lvfi.lParam = m_iovrCurrent;
		iitemOld = ListView_FindItem(hwndList, -1, &lvfi);
	}

	::SendMessage(hwndList, WM_SETREDRAW, false, 0);
	ListView_DeleteAllItems(hwndList);

	LVITEM lvi = { LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM };
	lvi.iImage = 1;
	int covr = m_voi.Size();
	for (int iovr = 0; iovr < covr; iovr++)
	{
		lvi.iItem = iovr;
		OverlayInfo & oi = m_voi[iovr];
		if (oi.m_os == kosDeleted)
			continue;
		StrApp str(oi.m_stuName.Chars());
		lvi.pszText = const_cast<achar *>(str.Chars());
		lvi.lParam = iovr;
		ListView_InsertItem(hwndList, &lvi);
	}

	// It seems that hiding a window still allows it to receive accelerator keys, so whenever
	// we hide a window, we have to disable it as well.
	int citem = ListView_GetItemCount(hwndList);
	int nCmdShow = citem > 0 ? SW_SHOW : SW_HIDE;
	::ShowWindow(::GetDlgItem(m_hwnd, kctidTagEdit), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidTagList), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidTagChoose), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kstidOvrTagLabel), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kstidOvrForeLabel), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidTagFore), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kstidOvrBackLabel), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidTagBack), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kstidOvrUnderLabel), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kstidOvrStyleLabel), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidTagUnderStyle), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kstidOvrColorLabel), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidTagUnderColor), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidTagAdvanced), nCmdShow);
	bool fEnable = citem > 0;
	::EnableWindow(::GetDlgItem(m_hwnd, kctidOvrCopy), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidOvrDelete), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidSettingsLabel), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidTagEdit), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidTagList), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidTagChoose), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kstidOvrTagLabel), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kstidOvrForeLabel), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidTagFore), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kstidOvrBackLabel), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidTagBack), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kstidOvrUnderLabel), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kstidOvrStyleLabel), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidTagUnderStyle), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kstidOvrColorLabel), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidTagUnderColor), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidTagAdvanced), fEnable);

	::SendMessage(hwndList, WM_SETREDRAW, true, 0);
	::InvalidateRect(hwndList, NULL, true);

	if (m_iovrCurrent != -1)
	{
		LVFINDINFO lvfi = { LVFI_PARAM };
		lvfi.lParam = m_iovrCurrent;
		int iitemNew = ListView_FindItem(hwndList, -1, &lvfi);

		if (iitemNew == -1)
		{
			iitemNew = iitemOld;

			// The old current selection is not in the list, so determine which item to select.
			int citem = ListView_GetItemCount(hwndList);
			if ((uint)iitemNew >= (uint)citem)
				iitemNew = citem - 1;
		}
		ListView_SetItemState(hwndList, iitemNew, LVIS_FOCUSED | LVIS_SELECTED,
			LVIS_FOCUSED | LVIS_SELECTED);
		ListView_EnsureVisible(hwndList, iitemNew, false);
	}
}


/*----------------------------------------------------------------------------------------------
	Add a new overlay to the list view. Open up a dialog to get the name and
	possibility list it's a part of.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgOvr::AddOverlay()
{
	NewOverlayDlgPtr qnod;
	qnod.Create();
	qnod->SetDialogValues(m_qlpi);
	if (qnod->DoModal(m_hwnd) != kctidOk)
		return;

	HWND hwndList = ::GetDlgItem(m_hwnd, kctidOverlayList);

	StrApp strName;
	HVO hvoPssl;
	bool fIncludeAll;
	qnod->GetDialogValues(strName, hvoPssl, fIncludeAll);
	m_ptod->FixName(strName, hwndList, false);

	OverlayInfo oi;
	oi.m_os = kosInserted;
	oi.m_stuName = strName;
	oi.m_hvoPssl = hvoPssl;
	oi.m_hvo = NULL;
	m_qlpi->LoadPossList(hvoPssl, m_qlpi->GetPsslWsFromDb(hvoPssl), &oi.m_qpli);

	if (fIncludeAll)
	{
		PossListInfo * ppli = oi.m_qpli;
		AssertPtr(ppli);
		int cpss = ppli->GetCount();
		oi.m_voti.Resize(cpss);
		PossItemInfo * ppii = ppli->GetPssFromIndex(0);
		// Add all the items in the possibility to the overlay.
		for (int ipss = 0; ipss < cpss; ipss++, ppii++)
		{
			AssertPtr(ppii);
			OverlayTagInfo & oti = oi.m_voti[ipss];
			oti.m_os = kosInserted;
			oti.m_hvoPss = ppii->GetPssId();
			oti.m_clrFore = ppii->GetForeColor();
			oti.m_clrBack = ppii->GetBackColor();
			oti.m_clrUnder = ppii->GetUnderColor();
			oti.m_unt = ppii->GetUnderlineType();
		}
	}

	m_voi.Push(oi);
	UpdateOverlayList();

	LVFINDINFO lvfi = { LVFI_PARAM };
	lvfi.lParam = m_voi.Size() - 1;
	int iitem = ListView_FindItem(hwndList, -1, &lvfi);
	Assert(iitem != -1);
	ListView_SetItemState(hwndList, iitem, LVIS_FOCUSED | LVIS_SELECTED,
		LVIS_FOCUSED | LVIS_SELECTED);
	ListView_EnsureVisible(hwndList, iitem, false);

	m_fModified = true;
}


/*----------------------------------------------------------------------------------------------
	Add a copy of an existing overlay to the list view.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgOvr::CopyOverlay()
{
	HWND hwndList = ::GetDlgItem(m_hwnd, kctidOverlayList);
	Assert((uint)m_iovrCurrent < (uint)m_voi.Size());

	OverlayInfo & oiOld = m_voi[m_iovrCurrent];
	StrApp strName(oiOld.m_stuName);
	m_ptod->FixName(strName, hwndList, true);

	OverlayInfo oi;
	oi.m_os = kosInserted;
	oi.m_stuName = strName;
	oi.m_qpli = oiOld.m_qpli;
	oi.m_voti = oiOld.m_voti;
	int coti = oi.m_voti.Size();
	for (int ioti = 0; ioti < coti; ioti++)
		oi.m_voti[ioti].m_os = oiOld.m_voti[ioti].m_os == kosDeleted ? kosDeleted : kosInserted;
	oi.m_hvo = NULL;
	oi.m_hvoPssl = oiOld.m_hvoPssl;

	m_voi.Push(oi);
	UpdateOverlayList();

	LVFINDINFO lvfi = { LVFI_PARAM };
	lvfi.lParam = m_voi.Size() - 1;
	int iitem = ListView_FindItem(hwndList, -1, &lvfi);
	Assert(iitem != -1);
	ListView_SetItemState(hwndList, iitem, LVIS_FOCUSED | LVIS_SELECTED,
		LVIS_FOCUSED | LVIS_SELECTED);
	ListView_EnsureVisible(hwndList, iitem, false);
}


/*----------------------------------------------------------------------------------------------
	Delete the currently selected overlay in the list view.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgOvr::DeleteOverlay()
{
	Assert((uint)m_iovrCurrent < (uint)m_voi.Size());

	StrApp strTitle(kstidDeleteOverlay);
	StrApp strPrompt(kstidDeleteOverlayMsg);

	const achar * pszHelpUrl = m_pszHelpUrl;
	m_pszHelpUrl = _T("Advanced_Tasks/Tagging_Text/Delete_an_overlay.htm");

	ConfirmDeleteDlgPtr qcdd;
	qcdd.Create();
	qcdd->SetTitle(strTitle.Chars());
	qcdd->SetPrompt(strPrompt.Chars());
	qcdd->SetHelpUrl(m_pszHelpUrl);
	// Make sure the user really wants to delete the overlay.
	if (qcdd->DoModal(m_hwnd) == kctidOk)
	{
		WaitCursor wc;

		OverlayInfo & oi = m_voi[m_iovrCurrent];
		oi.m_os = kosDeleted;

		UpdateOverlayList();
	}
	m_pszHelpUrl = pszHelpUrl;
}


/*----------------------------------------------------------------------------------------------
	Open up a Choices Chooser with the tags in the current overlay already selected so the user
	can modify the items that are in the current overlay.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgOvr::ChooseTags()
{
	Assert((uint)m_iovrCurrent < (uint)m_voi.Size());
	OverlayInfo & oi = m_voi[m_iovrCurrent];
	int psslId = oi.m_qpli->GetPsslId();

	// Add all the tags in the current overlay to the vector to pass to the Choices Chooser.
	Vector<HVO> vpssIdSel;
	int coti = oi.m_voti.Size();
	vpssIdSel.Resize(coti);
	int itag = 0;
	for (int ioti = 0; ioti < coti; ioti++)
	{
		if (oi.m_voti[ioti].m_os != kosDeleted)
			vpssIdSel[itag++] = oi.m_voti[ioti].m_hvoPss;
	}
	vpssIdSel.Resize(itag);

	StrApp staName(oi.m_stuName);
	// We need the list's preferred writing system (may be selector value).
	int wsList = m_qlpi->GetPsslWsFromDb(psslId);
	PossChsrDlgPtr qplc;
	qplc.Create();
	qplc->SetDialogValues(psslId, wsList, vpssIdSel, &staName);
	qplc->SetHelpUrl(_T("Advanced_Tasks/Tagging_Text/Overlay_Tag_Chooser.htm"));
	if (qplc->DoModal(m_hwnd) != kctidOk)
		return;

	Vector<HVO> vpssIdSelNew;
	qplc->GetDialogValues(vpssIdSelNew);

	// Go through the vector of old tags and decide what needs to be done with each one.
	// Insert new tags in their proper location if needed.
	int ioti = 0;
	int ctagNew = vpssIdSelNew.Size();
	for (int itagNew = 0; itagNew < ctagNew; itagNew++, ioti++)
	{
		HVO hvoNew = vpssIdSelNew[itagNew];
		// The size of oi.m_voti can change, so we have to check every time.
		if (ioti < oi.m_voti.Size())
		{
			OverlayTagInfo & oti = oi.m_voti[ioti];
			if (oti.m_hvoPss == hvoNew)
			{
				// The tags match. Make sure the tag's state is not deleted.
				if (oti.m_os == kosDeleted)
				{
					oti.m_os = kosModified;
					oi.m_os = kosModified;
				}
				continue;
			}

			// The tags do not match.

			// First look through the new IDs to see if we need to delete this tag.
			int itagT;
			for (itagT = itagNew + 1; itagT < ctagNew; itagT++)
			{
				if (oti.m_hvoPss == vpssIdSelNew[itagT])
					break;
			}
			if (itagT == ctagNew)
			{
				// The old tag is not in the new list, so set its state to deleted.
				// If the tag was not in the original list but has been inserted by the
				// user, remove it from the vector, because it is not in the database yet.
				if (oi.m_os == kosNormal)
					oi.m_os = kosModified;
				if (oti.m_os == kosInserted)
				{
					coti--;
					oi.m_voti.Delete(ioti--);
				}
				else
				{
					oti.m_os = kosDeleted;
				}
				itagNew--;
				continue;
			}

#ifdef DEBUG
			// Since both lists should be sorted in the same way, we should be able to say
			// now that the new item must be inserted at the current location (ioti) in oi.
			// Just to be paranoid, check to make sure the new item is not already in the
			// old list.
			{
				int cotiT = vpssIdSel.Size();
				for (int iotiT = 0; iotiT < cotiT; iotiT++)
				{
					if (oi.m_voti[ioti].m_hvoPss == hvoNew)
						Assert(false);
				}
			}
#endif // DEBUG
		}

		// We need to insert the new tag here.
		int ipss = oi.m_qpli->GetIndexFromId(hvoNew);
		PossItemInfo * ppii = oi.m_qpli->GetPssFromIndex(ipss);
		AssertPtr(ppii);
		OverlayTagInfo otiNew = { hvoNew };
		otiNew.m_clrFore = ppii->GetForeColor();
		otiNew.m_clrBack = ppii->GetBackColor();
		otiNew.m_clrUnder = ppii->GetUnderColor();
		otiNew.m_unt = ppii->GetUnderlineType();
		otiNew.m_os = kosInserted;
		oi.m_voti.Insert(ioti, otiNew);
		if (oi.m_os == kosNormal)
			oi.m_os = kosModified;
		coti++;
	}

	// Any items left over in the old list should be deleted.
	if (ioti < coti)
	{
		if (oi.m_os == kosNormal)
			oi.m_os = kosModified;
		while (ioti < coti)
		{
			if (oi.m_voti[ioti].m_os == kosInserted)
			{
				oi.m_voti.Delete(ioti);
				coti--;
			}
			else
			{
				oi.m_voti[ioti++].m_os = kosDeleted;
			}
		}
	}

	// Reload the list of tags.
	OnOverlayChange(m_iovrCurrent);
}


/*----------------------------------------------------------------------------------------------
	This method creates a new font based on the global overlay information. It also creates
	a new imagelist for the tag listview so that the items will have the proper vertical
	spacing.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgOvr::UpdateTagList()
{
	// Create the new font for the overlay tags and determine the height of a character.
	// This height is used in creating the imagelist for the tag listview.
	int fof;
	StrUni stuFont;
	int dympFont;
	int ctagMax;
	g_tog.GetGlobalOverlayValues(fof, stuFont, dympFont, ctagMax);
	HDC hdc = ::GetDC(m_hwnd);
	StrApp strFont(stuFont);
	if (m_hfont)
		AfGdi::DeleteObjectFont(m_hfont);
	m_hfont = AfGdi::CreateFont(-MulDiv(dympFont / 1000, ::GetDeviceCaps(hdc, LOGPIXELSY), 72),
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, strFont.Chars());
	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, m_hfont);
	TEXTMETRIC tm;
	::GetTextMetrics(hdc, &tm);
	// For some reason, the listview looks best when we subtract 1 from the height, even
	// with tags that have characters that hang below the baseline.
	m_dypText = tm.tmHeight - 1;
	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	int iSuccess;
	iSuccess = ::ReleaseDC(m_hwnd, hdc);
	Assert(iSuccess);

	// Create an empty imagelist to space out the items in the tag listview.
	// To figure out the vertical spacing, we take the height of the font and
	// add 5 (1 for the space above, 1 for the space below, and 3 for the double
	// underline). This is the maximum height of any of the list items.
	HWND hwndList = ::GetDlgItem(m_hwnd, kctidTagList);
//	HIMAGELIST himlEmpty = AfGdi::ImageList_Create(1, m_dypText + 5, ILC_COLOR, 0, 0);
//	HIMAGELIST himlOld = ListView_SetImageList(hwndList, himlEmpty, LVSIL_SMALL);
//	if (himlOld)
//		if (himlOld != himlEmpty)
//			AfGdi::ImageList_Destroy(himlOld);
	if (m_himlTag)
		AfGdi::ImageList_Destroy(m_himlTag);
	m_himlTag = AfGdi::ImageList_Create(1, m_dypText + 5, ILC_COLOR, 0, 0);
	HIMAGELIST himlOld = ListView_SetImageList(hwndList, m_himlTag, LVSIL_SMALL);
	if (himlOld)
		if (himlOld != m_himlTag)
			AfGdi::ImageList_Destroy(himlOld);

	// Force the imagelist to redraw itself. Just changing the imagelist makes it redraw,
	// but the scrollbars were messed up if the font size changed dramatically. I figured
	// the easiest way was to just reload all the items in the list.
	if (m_iovrCurrent >= 0)
	{
		// Save the current selection so we can select it after we repopulate the list.
		int iSel = ListView_GetNextItem(hwndList, -1, LVNI_SELECTED);
		OnOverlayChange(m_iovrCurrent);
		ListView_SetItemState(hwndList, iSel, LVIS_SELECTED | LVIS_FOCUSED,
			LVIS_SELECTED | LVIS_FOCUSED);
		ListView_EnsureVisible(hwndList, iSel, false);
	}
}


/*----------------------------------------------------------------------------------------------
	This method searches through the overlay for the current edit box text. The search is
	not case-specific. The edit box will be set to the nearest match.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgOvr::OnTagEditUpdate()
{
	HWND hwndEdit = ::GetDlgItem(m_hwnd, kctidTagEdit);
	achar rgch[MAX_PATH];
	::SendMessage(hwndEdit, WM_GETTEXT, MAX_PATH, (LPARAM)rgch);
	if (AfTagTypeAheadEdit::s_fExtraBackspace && *rgch)
		rgch[StrLen(rgch) - 1] = 0;

	HWND hwndList = ::GetDlgItem(m_hwnd, kctidTagList);
	LVITEM lvi = { LVIF_TEXT };
	lvi.pszText = rgch;
	lvi.cchTextMax = MAX_PATH;
	int cchPattern = StrLen(rgch);

	if (!cchPattern)
		return true;

	PossListInfoPtr qpli = m_voi[m_iovrCurrent].m_qpli;
	Locale loc = m_qlpi->GetLocale(m_qlpi->ActualWs(qpli->GetWs()));

	UErrorCode uerr = U_ZERO_ERROR;
	UnicodeString ust(rgch, cchPattern);
	UnicodeString ustPattern;
	Normalizer::normalize(ust, UNORM_NFD, 0, ustPattern, uerr);
	Assert(U_SUCCESS(uerr));
	UCharCharacterIterator itch(rgch, 1); // Temporarily set for next step.
	StringSearch * pss = new StringSearch(ustPattern, itch, loc, NULL, uerr);
	Assert(U_SUCCESS(uerr));
	RuleBasedCollator * rbc = pss->getCollator();
	rbc->setStrength(Collator::SECONDARY); // We want a caseless search.
	pss->setCollator(rbc, uerr);
	Assert(U_SUCCESS(uerr));

	// Find the item that best matches the search string and how many characters in that item
	// match the search string.
	int cchMatched = 0;
	int itagBestMatch = -1;
	int ctag = ListView_GetItemCount(hwndList);
	for (int itag = 0; itag < ctag; itag++)
	{
		lvi.iItem = itag;
		lvi.iSubItem = 0;
		if (!ListView_GetItem(hwndList, &lvi))
			ThrowHr(E_FAIL);
		PossItemInfo * ppii = qpli->GetPssFromIndex(lvi.iItem);
		StrUni stu;
		ppii->GetName(stu);

		itch.setText(stu.Chars(), stu.Length());
		pss->setText(itch, uerr);
		int ichMatch = pss->first(uerr);
		if (ichMatch == 0)
		{
			itagBestMatch = itag;
			cchMatched = cchPattern;
			break;
		}
	}
	delete pss;
	if (itagBestMatch == -1)
		itagBestMatch = 0;
	lvi.iItem = itagBestMatch;
	lvi.iSubItem = 0;
	ListView_GetItem(hwndList, &lvi);
	StrApp strMatch(rgch);
	lvi.iSubItem = 1;
	ListView_GetItem(hwndList, &lvi);
	strMatch.FormatAppend(_T(" - %s"), rgch);

	// Update the list and edit box.
	// Remove the selection from all items.
	ListView_SetItemState(hwndList, -1, 0, LVIS_SELECTED | LVIS_FOCUSED);
	// Select the matched tag.
	ListView_SetItemState(hwndList, itagBestMatch, LVIS_SELECTED | LVIS_FOCUSED,
		LVIS_SELECTED | LVIS_FOCUSED);
	ListView_EnsureVisible(hwndList, itagBestMatch, false);

	m_fUpdating = true;
	::SendMessage(hwndEdit, WM_SETTEXT, 0, (LPARAM)strMatch.Chars());
	m_fUpdating = false;
	::SendMessage(hwndEdit, EM_SETSEL, strMatch.Length(), cchMatched);

	return true;
}


/***********************************************************************************************
	NewOverlayDlg methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
NewOverlayDlg::NewOverlayDlg()
{
	m_rid = kridNewOverlayDlg;
	m_pszHelpUrl = _T("Advanced_Tasks/Tagging_Text/Add_an_overlay.htm");
	m_hvoPssl = -1;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
NewOverlayDlg::~NewOverlayDlg()
{
}


/*----------------------------------------------------------------------------------------------
	Store initial dialog values.
----------------------------------------------------------------------------------------------*/
void NewOverlayDlg::SetDialogValues(AfLpInfo * plpi)
{
	AssertPtr(plpi);
	m_qlpi = plpi;
}


/*----------------------------------------------------------------------------------------------
	Retrieve dialog values after the user has closed the dialog.
----------------------------------------------------------------------------------------------*/
void NewOverlayDlg::GetDialogValues(StrApp & strName, HVO & hvoPssl, bool & fIncludeAll)
{
	Assert(m_hvoPssl != -1);
	strName = m_strName;
	hvoPssl = m_hvoPssl;
	fIncludeAll = m_fIncludeAll;
}


/*----------------------------------------------------------------------------------------------
	The dialog is being created.
----------------------------------------------------------------------------------------------*/
bool NewOverlayDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Add the choices lists to the list box.
	HWND hwndList = ::GetDlgItem(m_hwnd, kctidListName);
	LVCOLUMN lvc = { LVCF_TEXT | LVCF_WIDTH };
	Rect rc;
	::GetClientRect(hwndList, &rc);
	lvc.cx = rc.Width();
	ListView_InsertColumn(hwndList, 0, &lvc);
	ListView_SetExtendedListViewStyle(hwndList, LVS_EX_FULLROWSELECT);

	try
	{
		IOleDbEncapPtr qode;
		IOleDbCommandPtr qodc;
		StrUni stuQuery;
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;

		//  Obtain pointer to IOleDbEncap interface.
		AfDbInfo * pdbi = m_qlpi->GetDbInfo();
		AssertPtr(pdbi);
		pdbi->GetDbAccess(&qode);

		CheckHr(qode->CreateCommand(&qodc));

		// Query the database to find the names of the Choices Lists for the Research Notebook.
		wchar rgchName[MAX_PATH];
		int psslId;
		Vector<HVO> & vhvo = m_qlpi->GetPsslIds();
		stuQuery.Format(L"select mo.obj, mo.txt from CmPossibilityList cp "
			L"join CmMajorObject_Name mo on cp.id = mo.obj "
			L"where mo.ws = %d and cp.id in (%d", pdbi->UserWs(), vhvo[0]);
		int chvo = vhvo.Size();
		for (int ihvo = 1; ihvo < chvo; ihvo++)
			stuQuery.FormatAppend(L", %d", vhvo[ihvo]);
		stuQuery.Append(L") order by mo.txt");
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		LVITEM lvi = { LVIF_TEXT | LVIF_PARAM };
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&psslId),
				isizeof(psslId), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(rgchName),
				isizeof(rgchName), &cbSpaceTaken, &fIsNull, 2));

			StrApp strName(rgchName);
			lvi.pszText = const_cast<achar *>(strName.Chars());
			lvi.lParam = psslId;
			ListView_InsertItem(hwndList, &lvi);
			lvi.iItem++;

			CheckHr(qodc->NextRow(&fMoreRows));
		}
	}
	catch (...)
	{
		::EndDialog(m_hwnd, 1);
		StrApp strM(kstidChoicesFail);
		StrApp strT(kstidNewOverlay);
		MessageBox(m_hwnd, strM.Chars(), strT.Chars(), MB_OK | MB_ICONWARNING);
	}

	// Select the first item in the list.
	DWORD dwT = LVIS_SELECTED | LVIS_FOCUSED;
	ListView_SetItemState(hwndList, 0, dwT, dwT);

	// Set up the edit box containing the overlay name.
	StrApp str(kstidNewOverlay);
	::SetDlgItemText(m_hwnd, kctidEditName, str.Chars());
	::SendMessage(::GetDlgItem(m_hwnd, kctidEditName), EM_SETSEL, 0, -1);

	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	::SendMessage(::GetDlgItem(m_hwnd, kctidOvrIncludeAll), BM_SETCHECK, BST_CHECKED, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Store the user choices in member variables and close the dialog.
----------------------------------------------------------------------------------------------*/
bool NewOverlayDlg::OnApply(bool fClose)
{
	achar szName[MAX_PATH];
	::SendMessage(::GetDlgItem(m_hwnd, kctidEditName), WM_GETTEXT, MAX_PATH, (LPARAM)szName);

	// Strip off spaces at the beginning and end of the string.
	StrApp strName;
	StrUtil::TrimWhiteSpace(szName, strName);

	if (!strName.Length())
	{
		// The item is empty, so show a message complaining about it.
		StrApp strMessage(kstidOvrRenEmptyMsg);
		StrApp strOverlay(kstidTlsOptOvr);
		::MessageBox(m_hwnd, strMessage.Chars(), strOverlay.Chars(),
			MB_OK | MB_ICONINFORMATION);
		::SetFocus(::GetDlgItem(m_hwnd, kctidEditName));
		return false;
	}

	HWND hwndList = ::GetDlgItem(m_hwnd, kctidListName);
	int iitem = ListView_GetNextItem(hwndList, -1, LVNI_SELECTED);
	LVITEM lvi = { LVIF_PARAM, iitem };
	ListView_GetItem(hwndList, &lvi);
	m_hvoPssl = lvi.lParam;

	m_strName = strName;

	m_fIncludeAll = ::IsDlgButtonChecked(m_hwnd, kctidOvrIncludeAll) == BST_CHECKED;

	return SuperClass::OnApply(fClose);
}


/***********************************************************************************************
	AdvOverlayDlg methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AdvOverlayDlg::AdvOverlayDlg()
{
	m_rid = kridAdvOverlayDlg;
	m_pszHelpUrl =
		_T("User_Interface/Menus/Tools/Options/Advanced_Global_Overlay_Settings.htm");
}


/*----------------------------------------------------------------------------------------------
	The dialog is being created.
----------------------------------------------------------------------------------------------*/
bool AdvOverlayDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	int fof;
	StrUni stuFont;
	int dympFont;
	int ctagMax;
	g_tog.GetGlobalOverlayValues(fof, stuFont, dympFont, ctagMax);

	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	// Get the currently available fonts via the LgFontManager.
	ILgFontManagerPtr qfm;
	qfm.CreateInstance(CLSID_LgFontManager);

	SmartBstr bstrNames;
	CheckHr(qfm->AvailableFonts(&bstrNames));

	StrApp strNameList;
	strNameList.Assign(bstrNames.Bstr(), BstrLen(bstrNames.Bstr())); // Convert BSTR to StrApp.
	int cch = strNameList.Length();
	StrApp strName; // Individual font name.
	int ichMin = 0; // Index of the beginning of a font name.
	int ichLim = 0; // Index that is one past the end of a font name.

	// Add each font name to the combo box.
	HWND hwndCombo = ::GetDlgItem(m_hwnd, kctidFontCombo);
	while (ichLim < cch)
	{
		ichLim = strNameList.FindCh(L',', ichMin);
		if (ichLim == -1) // i.e., if not found.
			ichLim = cch;
		strName.Assign(strNameList.Chars() + ichMin, ichLim - ichMin);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)strName.Chars());
		ichMin = ichLim + 1;
	}

	// Select the current font.
	StrApp str(stuFont);
	::SendMessage(hwndCombo, CB_SELECTSTRING, (WPARAM)-1, (LPARAM)str.Chars());

	// Add the font sizes to the combo box.
	hwndCombo = ::GetDlgItem(m_hwnd, kctidSizeCombo);
	int cdypt = isizeof(s_rgdyptSize) / isizeof(int);
	StrAppBufSmall strbs;
	for (int idypt = 0; idypt < cdypt; idypt++)
	{
		strbs.Format(_T("%d"), s_rgdyptSize[idypt]);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)strbs.Chars());
	}

	// Select the current font size.
	strbs.Format(_T("%d"), dympFont / 1000);
	if (CB_ERR == ::SendMessage(hwndCombo, CB_SELECTSTRING, (WPARAM)-1, (LPARAM)strbs.Chars()))
		::SetWindowText(hwndCombo, strbs.Chars());

	if (fof & kfofLeadTag)
		::CheckDlgButton(m_hwnd, kctidOpenTag, BST_CHECKED);
	if (fof & kfofTrailTag)
		::CheckDlgButton(m_hwnd, kctidCloseTag, BST_CHECKED);
	if (fof & kfofLeadBracket)
		::CheckDlgButton(m_hwnd, kctidOpenBracket, BST_CHECKED);
	if (fof & kfofTrailBracket)
		::CheckDlgButton(m_hwnd, kctidCloseBracket, BST_CHECKED);
	if (fof & kfofTagsUseAttribs)
		::CheckDlgButton(m_hwnd, kctidShowFormat, BST_CHECKED);

	// Set max tags choices to the tags combo box. These are related to ctagMax as follows:
	// ctagmax < 1: "none"; = 1: "1"; ...; = 5: "5"; > 5: "all".
	hwndCombo = ::GetDlgItem(m_hwnd, kctidTagsCombo);
	str.Load(kstidOvrTagNone);
	::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
	int i;
	for (i = 1; i < 6; ++i)
	{
		strbs.Format(_T("%d"), i);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)strbs.Chars());
	}
	str.Load(kstidOvrTagAll);
	::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());

	// Set the current number of max tags.
	i = ctagMax;
	if (i > 5)
		i = 6;
	::SendMessage(hwndCombo, CB_SETCURSEL, (WPARAM)i, 0);

	// Create a private dummy cache.
	m_qvcd.CreateInstance(CLSID_VwCacheDa);
	Assert(m_qvcd);

	CreateOverlay();

	m_qopw.Attach(NewObj OvrPreviewWnd);
	m_qopw->Create(m_hwnd, kctidOvrPreview, m_qvcd, m_qvo, m_wsUser);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	The OK button was pushed.
----------------------------------------------------------------------------------------------*/
bool AdvOverlayDlg::OnApply(bool fClose)
{
	// REVIEW JeffG (LarryW): Do we need to be concerned with knConflicting here?
	int dypt = GetFontSize();
	if (!IsValidFontSize(dypt)) // && dypt != knConflicting)
	{
		StrApp strMessage(kstidFfdRange);
		MessageBox(m_hwnd, strMessage.Chars(), NULL, 0);

		// REVIEW JeffG (LarryW): Are we keeping some old font size somewhere?
		// m_chrpCur.dympHeight = m_chrpOld.dympHeight;

		StrAppBuf strbFontSize;
		// m_chrpOld.dympHeight / 1000); // See default above.
		strbFontSize.Format(_T("%d"), 12);
		::SendDlgItemMessage(m_hwnd, kctidSizeCombo, WM_SETTEXT, 0,
			(LPARAM)strbFontSize.Chars());

		// Set focus back to font size combobox.
		::SetFocus(::GetDlgItem(m_hwnd, kctidSizeCombo));
		return false;
	}

	StrUni stuFont;
	GetFontName(stuFont);

	g_tog.SetGlobalOverlayValues(GetOverlayFlags(), (wchar *)stuFont.Chars(), dypt * 1000,
		GetMaxTags());

	return SuperClass::OnApply(fClose);
}


/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool AdvOverlayDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case CBN_SELENDOK:
		::PostMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(ctidFrom, CBN_CLOSEUP),
			(LPARAM)pnmh->hwndFrom);
		break;

	case CBN_CLOSEUP:
		if (ctidFrom == kctidFontCombo)
		{
			StrUni stuFont;
			GetFontName(stuFont);
			CheckHr(m_qvo->put_FontName(stuFont.Bstr()));
			m_qopw->UpdateOverlay();
		}
		else if (ctidFrom == kctidSizeCombo)
		{
			int dypt = GetFontSize();
			if (IsValidFontSize(dypt))
			{
				CheckHr(m_qvo->put_FontSize(dypt * 1000));
				m_qopw->UpdateOverlay();
			}
		}
		else if (ctidFrom == kctidTagsCombo)
		{
			CheckHr(m_qvo->put_MaxShowTags(GetMaxTags()));
			m_qopw->UpdateOverlay();
		}
		return true;

	case BN_CLICKED:
		switch (ctidFrom)
		{
		case kctidOpenTag:
		case kctidCloseTag:
		case kctidOpenBracket:
		case kctidCloseBracket:
		case kctidShowFormat:
			CheckHr(m_qvo->put_Flags((VwOverlayFlags)GetOverlayFlags()));
			m_qopw->UpdateOverlay();
		}
		break;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Returns the selected font size.
----------------------------------------------------------------------------------------------*/
int AdvOverlayDlg::GetFontSize()
{
	int cch;
	StrAppBuf strbFontSize;

	// Get the text from the Font Size combobox and convert it to a number.
	strbFontSize.SetLength(strbFontSize.kcchMaxStr);
	cch = ::SendDlgItemMessage(m_hwnd, kctidSizeCombo, WM_GETTEXT, strbFontSize.kcchMaxStr,
		(LPARAM)strbFontSize.Chars());
	if (cch > strbFontSize.kcchMaxStr)
		cch = strbFontSize.kcchMaxStr;
	strbFontSize.SetLength(cch);

	if (cch == 0)
		return 12;
	return StrUtil::ParseInt(strbFontSize.Chars());
}


/*----------------------------------------------------------------------------------------------
	Returns the selected font name.
----------------------------------------------------------------------------------------------*/
void AdvOverlayDlg::GetFontName(StrUni & stuName)
{
	int cch;
	StrAppBuf strb;
	cch = ::GetDlgItemText(m_hwnd, kctidFontCombo, &strb[0], strb.kcchMaxStr);
	strb.SetLength(cch);
	stuName = strb.Chars();
}


/*----------------------------------------------------------------------------------------------
	Returns the current selection for max tags.
----------------------------------------------------------------------------------------------*/
int AdvOverlayDlg::GetMaxTags()
{
	int ctagMax = ::SendDlgItemMessage(m_hwnd, kctidTagsCombo, CB_GETCURSEL, 0, 0);
	if (ctagMax == CB_ERR || ctagMax == 6)
		ctagMax = 1000;	// Set to 1000 if nothing or "all" selected.
	return ctagMax;
}


/*----------------------------------------------------------------------------------------------
	Returns the selected overlay flags.
----------------------------------------------------------------------------------------------*/
int AdvOverlayDlg::GetOverlayFlags()
{
	int fof = 0;
	if (::IsDlgButtonChecked(m_hwnd, kctidOpenTag) == BST_CHECKED)
		fof |= kfofLeadTag;
	if (::IsDlgButtonChecked(m_hwnd, kctidCloseTag) == BST_CHECKED)
		fof |= kfofTrailTag;
	if (::IsDlgButtonChecked(m_hwnd, kctidOpenBracket) == BST_CHECKED)
		fof |= kfofLeadBracket;
	if (::IsDlgButtonChecked(m_hwnd, kctidCloseBracket) == BST_CHECKED)
		fof |= kfofTrailBracket;
	if (::IsDlgButtonChecked(m_hwnd, kctidShowFormat) == BST_CHECKED)
		fof |= kfofTagsUseAttribs;
	return fof;
}


/*----------------------------------------------------------------------------------------------
	Create a sample overlay to use for the preview.
	Returns true if the overlay was created successfully.
----------------------------------------------------------------------------------------------*/
bool AdvOverlayDlg::CreateOverlay()
{
	try
	{
		//  Create an instance of a VwOverlay and initialize.
		m_qvo.CreateInstance(CLSID_VwOverlay);

		StrUni stuTag1(L"Tag1");
		StrUni stuTag2(L"Tag2");
		StrUni stuTag3(L"Tag3");
		for (int i = 0; i < 3; i++)
			memset(&m_uid[i], i + 1, isizeof(GUID));

		CheckHr(m_qvo->SetTagInfo((OLECHAR *)&m_uid[0], 1, kosmAll, stuTag1.Bstr(),
			stuTag1.Bstr(), RGB(255, 0, 0), (COLORREF)kclrTransparent, 0, kuntNone, false));
		CheckHr(m_qvo->SetTagInfo((OLECHAR *)&m_uid[1], 2, kosmAll, stuTag2.Bstr(),
			stuTag2.Bstr(), 0, RGB(0, 255, 255), 0, kuntNone, false));
		CheckHr(m_qvo->SetTagInfo((OLECHAR *)&m_uid[2], 3, kosmAll, stuTag3.Bstr(),
			stuTag3.Bstr(), 0, (COLORREF)kclrTransparent, 0, kuntSingle, false));

		StrUni stuFont;
		GetFontName(stuFont);
		CheckHr(m_qvo->put_FontName(stuFont.Bstr()));
		int dypt = GetFontSize();
		if (IsValidFontSize(dypt))
			CheckHr(m_qvo->put_FontSize(dypt * 1000));
		CheckHr(m_qvo->put_MaxShowTags(GetMaxTags()));
		CheckHr(m_qvo->put_Flags((VwOverlayFlags)GetOverlayFlags()));
	}
	catch (...)
	{
		m_qvo.Clear();
		return false;
	}
	return true;
}


/***********************************************************************************************
	OvrPreviewWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Create the view window.

	@param hwndPar Handle to the parent window.
	@param wid Child window identifier to use for the view window.
	@param pcda Pointer to the data cache containing the filter information.
----------------------------------------------------------------------------------------------*/
void OvrPreviewWnd::Create(HWND hwndPar, int wid, IVwCacheDa * pvcd, IVwOverlay * pvo,
	int wsUser)
{
	AssertPtr(pvcd);
	AssertPtr(pvo);

	m_qvcd = pvcd;
	m_qvo = pvo;

	ITsStrFactoryPtr qtsf;
	ITsStringPtr qtss;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(kstidTagTextDemo);
	CheckHr(qtsf->MakeString(stu.Bstr(), wsUser, &qtss));

	m_qvcd->CacheStringProp(khvoPreview, ktagPreview, qtss);

	HWND hwndTemp = ::GetDlgItem(hwndPar, wid);
	Rect rc;
	::GetWindowRect(hwndTemp, &rc);
	::MapWindowPoints(NULL, hwndPar, (POINT *)&rc, 2);
	::DestroyWindow(hwndTemp);

	WndCreateStruct wcs;
	wcs.InitChild(_T("AfVwWnd"), hwndPar, wid);
	wcs.style |= WS_VISIBLE | WS_TABSTOP;
	wcs.dwExStyle = WS_EX_CLIENTEDGE;
	wcs.SetRect(rc);
	CreateHwnd(wcs);

	CheckHr(m_qrootb->putref_Overlay(m_qvo));

	CheckHr(m_qrootb->MakeTextSelection(0, 0, NULL, ktagPreview, 0, 0, 24, 0, false, -1, NULL,
		true, NULL));
	ModifyOverlay(true, m_qvo, 2);
	CheckHr(m_qrootb->MakeTextSelection(0, 0, NULL, ktagPreview, 0, 8, 24, 0, false, -1, NULL,
		true, NULL));
	ModifyOverlay(true, m_qvo, 1);
	CheckHr(m_qrootb->MakeTextSelection(0, 0, NULL, ktagPreview, 0, 13, 24, 0, false, -1, NULL,
		true, NULL));
	ModifyOverlay(true, m_qvo, 0);
	CheckHr(m_qrootb->DestroySelection());
}


/*----------------------------------------------------------------------------------------------
	Make the root box.

	@param pvg Pointer to an IVwGraphics COM object: not used by this function.
	@param pprootb Address of a pointer to the root box, used to return the newly created root
					box.
----------------------------------------------------------------------------------------------*/
void OvrPreviewWnd::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
	IVwRootBox ** pprootb)
{
	AssertPtrN(pwsf);
	*pprootb = NULL;

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this));
	HVO hvo = khvoPreview;
	int frag = kfragPreview;

	// Set up a new view constructor.
	m_qopvc.Attach(NewObj OvrPreviewVc);

	ISilDataAccessPtr qsdaTemp;
	HRESULT hr;
	IgnoreHr(hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);
	if (pwsf)
		CheckHr(qsdaTemp->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qsdaTemp));

	IVwViewConstructor * pvvc = m_qopvc;
	CheckHr(qrootb->SetRootObjects(&hvo, &pvvc, &frag, NULL, 1));
	*pprootb = qrootb;
	(*pprootb)->AddRef();
}


/***********************************************************************************************
	OvrPreviewVc methods.
***********************************************************************************************/

static DummyFactory g_fact(_T("SIL.AppCore.OvrPreviewVc"));

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.

	@param pvwenv Pointer to a view environment COM object, used to access the filter data.
	@param hvo Database/cache ID for the filter fragment.
	@param frag Selects what fragment of a filter to display.

	@return S_OK, E_FAIL, or another appropriate error COM error value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OvrPreviewVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwenv);

	if (frag == kfragPreview)
	{
		CheckHr(pvwenv->put_IntProperty(ktptMarginTop, ktpvMilliPoint, 2000));
		CheckHr(pvwenv->put_IntProperty(ktptAlign, ktpvEnum, ktalCenter));
		CheckHr(pvwenv->put_IntProperty(ktptFontSize, ktpvMilliPoint, 11000));
		CheckHr(pvwenv->OpenTaggedPara());
		CheckHr(pvwenv->AddStringProp(ktagPreview, this));
		CheckHr(pvwenv->CloseParagraph());
	}

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}

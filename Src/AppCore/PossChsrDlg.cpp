/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: PossChsrDlg.cpp
Responsibility: Darrell Zook
Last reviewed: Not yet.

Description:
	This file contains code for the Possibilities List Chooser. It contains the following
	classes:
		PossChsrDlg : AfDialog - This class creates and manages the main chooser dialog. It
			contains windows of the three following classes.
		PossChsrTree : AfWnd - This class is used to manage the two tree controls on the
			PossChsrDlg window. It is needed to get a context menu and to keep the tree view
			from flickering when it is resized.
		PossChsrTab : AfWnd - This class is used to manage the tab control on the PossChsrDlg
			window. It is needed to keep the tab control from flickering when it is resized.
		PossChsrComboEdit : AfWnd - This class is used to manage the edit boxes within the two
			combo boxes on the PossChsrDlg window. It is needed to handle type-ahead correctly.
		PossWebEvent : IDispatch - This class is used to monitor events raised by the
			IWebBrowser2 control contained on the PossChsrDlg window.
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

#undef DEBUG_POSSCHSRDLG

const achar * kpszPossChsrSubKey = _T("List Chooser");

bool PossChsrComboEdit::s_fExtraBackspace;


/*----------------------------------------------------------------------------------------------
	The command map for the main chooser dialog.
----------------------------------------------------------------------------------------------*/

BEGIN_CMD_MAP(PossChsrDlg)
	ON_CID_ME(kcidPossDispName, &PossChsrDlg::CmdChangeDisplay, &PossChsrDlg::CmsChangeDisplay)
	ON_CID_ME(kcidPossDispNameAbbrev, &PossChsrDlg::CmdChangeDisplay,
		&PossChsrDlg::CmsChangeDisplay)
	ON_CID_ME(kcidPossDispAbbrev, &PossChsrDlg::CmdChangeDisplay,
		&PossChsrDlg::CmsChangeDisplay)
	ON_CID_ME(kcidPossTrEditList, &PossChsrDlg::CmdModify, &PossChsrDlg::CmsNotRequireItem)
	ON_CID_ME(kcidPossTrInsert, &PossChsrDlg::CmdInsert, &PossChsrDlg::CmsNotRequireItem)
	ON_CID_ME(kcidPossTrInsertBef, &PossChsrDlg::CmdInsert, &PossChsrDlg::CmsRequireItem)
	ON_CID_ME(kcidPossTrInsertAft, &PossChsrDlg::CmdInsert, &PossChsrDlg::CmsRequireItem)
	ON_CID_ME(kcidPossTrInsertSub, &PossChsrDlg::CmdInsert, &PossChsrDlg::CmsRequireItem)
	ON_CID_ME(kcidPossTrRename, &PossChsrDlg::CmdRename, &PossChsrDlg::CmsRequireItem)
	ON_CID_ME(kcidPossTrMerge, &PossChsrDlg::CmdMerge, &PossChsrDlg::CmsRequireItem)
	ON_CID_ME(kcidPossTrDelete, &PossChsrDlg::CmdDelete, &PossChsrDlg::CmsRequireItem)
	ON_CID_ME(kcidPossExcludeTag, &PossChsrDlg::CmdModifyTag, &PossChsrDlg::CmsModifyTag)
	ON_CID_ME(kcidPossIncludeSubtags, &PossChsrDlg::CmdModifyTag, &PossChsrDlg::CmsModifyTag)
	ON_CID_ME(kcidPossExcludeSubtags, &PossChsrDlg::CmdModifyTag, &PossChsrDlg::CmsModifyTag)
	ON_CID_ME(kcidViewRefresh, &PossChsrDlg::CmdViewRefresh, NULL)
END_CMD_MAP_NIL()


/***********************************************************************************************
	PossChsrDlg methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
PossChsrDlg::PossChsrDlg()
{
	m_rid = kridPossChsrDlg;
	m_pszHelpUrl = _T("Beginning_Tasks/Referencing_Topics_Lists/Chooser.htm");

	m_hwndTab = NULL;
	m_rghwndTree[0] = m_rghwndTree[1] = NULL;
	m_rghwndHistCombo[0] = m_rghwndHistCombo[1] = NULL;
	m_hwndTool = NULL;
	m_himlCold = NULL;
	m_himlHot = NULL;
	m_hwndHelp = NULL;
	m_hwndGrip = NULL;
	m_iCurTabSel = kiChoiceList;
	m_fShowHelp = false;
	m_fShowCurrent = false;
	m_pnt = kpntName;
	m_fAtomic = false;
	m_ypOk = m_dxpOk = m_dypTab = m_dypTree = 0;
	m_dxpButtonSep = 0;
	m_dxpHelp = m_dypHelp = 0;
	m_dypCurSel = 0;
	m_dxpMin = 0;
	m_cHistBack = m_cHistForward = -1;
	m_ipssContext = -1;
	m_fIgnoreSelChange = false;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
PossChsrDlg::~PossChsrDlg()
{
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

	// Child windows do not need to be destroyed:  m_hwndHelp, m_hwndGrip, m_hwndTool, m_hwndTab

	m_qpli->RemoveNotify(this);
	BOOL fSuccess;
	fSuccess = ::UnregisterClass(_T(ATLAXWIN_CLASS), ::GetModuleHandle(NULL));
	Assert(fSuccess);
	m_fIgnoreSelChange = false;

	// Assert(m_vpssId.Size() == 0); == 1 when hit OK immediately after opening dialog!
	Assert(m_vpssId.Size() >= 0);
	if (m_vpssId.Size() > 0)
		m_vpssId.Clear();
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal. This method will
	result in a dialog allowing only a single selection. If pssIdSel is not NULL, it will
	initially be selected.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::SetDialogValues(HVO psslId, int ws, HVO pssIdSel, AppOverlayInfo * paoi)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::SetDialogValues 1:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	m_psslId = psslId;
	m_ws = ws;
	m_fAtomic = true;
	if (pssIdSel)
		m_vpssId.Push(pssIdSel);
	m_fOverlay = false;
	m_pstrOverlayName = NULL;
	m_paoi = paoi;
	if (m_paoi)
		m_fOverlay = true;
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal. This method will
	result in a dialog allowing multiple selections. If vpssIdSel is not empty, each item in
	the vector will initially be selected.
	fOverlay should be true if the dialog is being used to modify the possibilities
	in an overlay.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::SetDialogValues(HVO psslId, int ws, Vector<HVO> & vpssIdSel,
	StrApp * pstrOverlayName)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::SetDialogValues 2:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	m_psslId = psslId;
	m_ws = ws;
	m_fAtomic = false;
	m_vpssId = vpssIdSel;
	m_pstrOverlayName = pstrOverlayName;
	if (m_pstrOverlayName)
	{
		m_pszHelpUrl = _T("DialogOverlayTagChooser.htm");
		m_fOverlay = true;
	}
	else
	{
		m_fOverlay = false;
	}
	m_paoi = NULL;
}


/*----------------------------------------------------------------------------------------------
	Gets the final values for the dialog controls, after the dialog has been closed.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::GetDialogValues(Vector<HVO> & vpssIdSel)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::GetDialogValues 1:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	vpssIdSel = m_vpssId;
}


/*----------------------------------------------------------------------------------------------
	Gets the final values for the dialog controls, after the dialog has been closed.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::GetDialogValues(HVO & pssIdSel)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::GetDialogValues 2:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	Assert(m_fAtomic); // This method should only be used for atomic dialogs.
	Assert(m_vpssId.Size() == 1 || m_vpssId.Size() == 0);

	if (m_vpssId.Size())
		pssIdSel = m_vpssId[0];
	else
		pssIdSel = 0;
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::OnInitDlg:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	DWORD dwStyle;
	// Get the LpInfo, DbInfo, and writing system factory.
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	ILgWritingSystemFactoryPtr qwsf;
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	m_wsUser = pdbi->UserWs();
	pdbi->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);

	// Make a trivial data access object.
	m_qvcd.CreateInstance(CLSID_VwCacheDa);
	Assert(m_qvcd);

	m_qpcc.Attach(NewObj PossChsrChoices);
	m_qpcc->InitValues(this);

	// Give the kcidPossChsrChoices button the WS_CLIPCHILDREN style.
	// This prevents it drawing over its child.
	//dwStyle = ::GetWindowLong(::GetDlgItem(m_hwnd, kcidPossChsrChoices), GWL_STYLE);
	//::SetWindowLong(::GetDlgItem(m_hwnd, kcidPossChsrChoices), GWL_STYLE, dwStyle | WS_CLIPCHILDREN);

	// Create the Selected Choices window (child of kcidPossChsrChoices).
	m_qpcc->Create(m_hwnd, kcidPossChsrChoices, m_qvcd, m_wsUser);

	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);
	// Create the gripper control.
	m_hwndGrip = ::CreateWindow(_T("SCROLLBAR"), NULL,
		WS_CHILD | WS_VISIBLE | SBS_SIZEGRIP | SBS_SIZEBOX | SBS_SIZEBOXBOTTOMRIGHTALIGN,
		0, 0, 0, 0, m_hwnd, NULL, NULL, NULL);

	m_hwndTab = ::GetDlgItem(m_hwnd, kctidTab);
	dwStyle = ::GetWindowLong(m_hwndTab, GWL_STYLE);
	::SetWindowLong(m_hwndTab, GWL_STYLE, dwStyle | WS_CLIPCHILDREN);
	PossChsrTabPtr qpctab;
	qpctab.Create();
	qpctab->SubclassTab(m_hwndTab);

	Rect rc;
	HWND hwndTemp = ::GetDlgItem(m_hwnd, kctidKeywordTree);
	::GetWindowRect(hwndTemp, &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);

	// Create the keyword search tree.
	WndCreateStruct wcs;
	wcs.InitChild(WC_TREEVIEW, m_hwnd, kctidKeywordTree);
	wcs.SetRect(rc);
	wcs.style = ::GetWindowLong(hwndTemp, GWL_STYLE) | WS_VISIBLE | TVS_NOSCROLL;
	wcs.dwExStyle = ::GetWindowLong(hwndTemp, GWL_EXSTYLE);

	HWND hwndPrev = ::GetNextWindow(hwndTemp, GW_HWNDPREV);
	BOOL fSuccess;
	fSuccess = ::DestroyWindow(hwndTemp);
	Assert(fSuccess);

	wcs.style |= TVS_CHECKBOXES;


	PossChsrTreePtr qpct;
	qpct.Create();
	qpct->CreateAndSubclassHwnd(wcs);
	m_rghwndTree[kiKeywordSearch] = qpct->Hwnd();
	qpct->SetWritingSystemFactory(qwsf);
	::SetWindowPos(qpct->Hwnd(), hwndPrev, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);

	// Create the choices tree.
	wcs.hMenu = (HMENU)kctidChoiceTree;
	wcs.style |= TVS_HASBUTTONS | TVS_LINESATROOT;
	m_qpctCh.Create();
	m_qpctCh->CreateAndSubclassHwnd(wcs);
	m_rghwndTree[kiChoiceList] = m_qpctCh->Hwnd();
	m_qpctCh->SetWritingSystemFactory(qwsf);
	::SetWindowPos(m_qpctCh->Hwnd(), hwndPrev, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);

	m_rghwndHistCombo[kiChoiceList] = ::GetDlgItem(m_hwnd, kctidChoiceHist);
	m_rghwndHistCombo[kiKeywordSearch] = ::GetDlgItem(m_hwnd, kctidKeywordHist);

	// Subclass the edit box within the combo box controls.
	PossChsrComboEditPtr qpcce;
	qpcce.Create();
	qpcce->SubclassEdit(::GetWindow(m_rghwndHistCombo[kiChoiceList], GW_CHILD));
	qpcce.Create();
	qpcce->SubclassEdit(::GetWindow(m_rghwndHistCombo[kiKeywordSearch], GW_CHILD));

	TCITEM tci = { TCIF_TEXT };
	StrApp strTab(kstidTopicsLst);
	tci.pszText = const_cast<achar *>(strTab.Chars());
	TabCtrl_InsertItem(m_hwndTab, 0, &tci);
	strTab.Load(kstidKeywordSrch);
	tci.pszText = const_cast<achar *>(strTab.Chars());
	TabCtrl_InsertItem(m_hwndTab, 1, &tci);

	Rect rcClient;
	GetClientRect(rcClient);
	Rect rcWindow(rcClient);
	::MapWindowPoints(m_hwnd, NULL, (POINT *)&rcWindow, 2);

	// Find the dimensions of the controls.
	Rect rcT;
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidHelp), &rcT);
	m_ypOk = rcWindow.bottom - rcT.top;
	m_dypHelp = rcWindow.bottom - rcT.bottom;
	int xpLeft = rcT.left;
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidCancel), &rcT);
	m_dxpButtonSep = xpLeft - rcT.right;
	m_dxpOk = rcT.Width();
	::GetWindowRect(m_hwndTab, &rcT);
	m_dypTab = rcWindow.bottom - rcT.bottom + (rcT.top - rcWindow.top);
	::GetWindowRect(m_rghwndTree[0], &rcT);
	m_dypTree = rcWindow.bottom - rcT.bottom + (rcT.top - rcWindow.top);
	m_dxpMin = m_dxpOk * 4 + m_dxpButtonSep * 5;
	HWND hwndChoicesButton = ::GetDlgItem(m_hwnd, kcidPossChsrChoices);
	::GetWindowRect(hwndChoicesButton, &rcT);
	m_dypCurSel = rcT.Height();

	// Give the button a sunken appearance.
	dwStyle = ::GetWindowLong(hwndChoicesButton, GWL_EXSTYLE);
	::SetWindowLong(hwndChoicesButton, GWL_EXSTYLE, dwStyle | WS_EX_CLIENTEDGE);

	LoadSettings(kpszPossChsrSubKey);

	// Load the possibility list from the database, if not already cached.
	plpi->LoadPossList(m_psslId, m_ws, &m_qpli);
	StrApp str;
	if (m_paoi)
	{
		str.Assign(m_paoi->m_stuName);
		str.AppendLoad(kstidOverlayChooser);
	}
	else if (m_fOverlay)
	{
		str.Format(_T("%s%r"), m_pstrOverlayName->Chars(), kstidOverlayChooser);
	}
	else
	{
		str = m_qpli->GetName();
		str.AppendLoad(kstidChooser);
	}
	::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)str.Chars());

	m_qpli->AddNotify(this);

	m_plddDragDrop.Init(m_qpli, false, m_rghwndTree[kiChoiceList], m_hwnd);
	m_qpctCh->SetDragDropHandler(&m_plddDragDrop);

	if (m_fShowHelp)
	{
		StrApp str(kstidLess);
		::SetWindowText(::GetDlgItem(m_hwnd, kctidToggleHelp), str.Chars());
	}

	AfButtonPtr qbtn2;
	qbtn2.Create();
	qbtn2->SubclassButton(m_hwnd, kctidModifyLst, kbtPopMenu, NULL, 0);
	if (m_fOverlay)
	{
		StrApp str(kstidModifyLstOptions);
		BOOL fSuccess;
		fSuccess = ::SetDlgItemText(m_hwnd, kctidModifyLst, str);
		Assert(fSuccess);
	}

	// Create the toolbar window for the help pane.
	m_hwndTool = ::CreateWindow(TOOLBARCLASSNAME, NULL,
		WS_CHILD | TBSTYLE_FLAT | CCS_NOPARENTALIGN | CCS_NORESIZE | CCS_NODIVIDER,
		0, 0, 0, 0, m_hwnd, NULL, NULL, NULL);
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

	m_qpwe.Attach(NewObj PossWebEvent(this, m_hwndTool, plpi));
	// Create the HTML window for the help pane.
	// First initialize ATL control containment code.
	AtlAxWinInit();
#define CRASH_WIN98
#ifdef CRASH_WIN98
// This still crashes on Win98.
	Assert(m_hwndHelp == 0);
	m_hwndHelp = ::CreateWindowEx(WS_EX_CLIENTEDGE, _T(ATLAXWIN_CLASS), _T(""),
		WS_CHILD | WS_VSCROLL, 0, 0, 0, 0, m_hwnd, NULL, AfApp::Papp()->m_hinst, NULL);
	DWORD dwErr = 0;
	if (!m_hwndHelp)
	{
		dwErr = ::GetLastError();
		LPVOID lpMsgBuf;
		if (::FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER|
				FORMAT_MESSAGE_FROM_SYSTEM|FORMAT_MESSAGE_IGNORE_INSERTS,
				NULL, dwErr, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
				(LPTSTR) &lpMsgBuf,
				0,
				NULL ))
		{
			// Display the string.
			::MessageBox(NULL, (LPCTSTR)lpMsgBuf, _T("Error"), MB_OK | MB_ICONINFORMATION);
			// Free the buffer.
			::LocalFree( lpMsgBuf );
		}
	}
	Assert(m_hwndHelp);
	IUnknownPtr qunk;
	CheckHr(AtlAxCreateControlEx(L"about:blank", m_hwndHelp, NULL, NULL, &qunk,
		IID_IDispatch, static_cast<IUnknown *>(m_qpwe)));
	CheckHr(qunk->QueryInterface(IID_IWebBrowser2, (void **)&m_qweb2));
	m_qpwe->SetWebBrowser(m_qweb2);
#endif

	if (m_fShowHelp)
	{
		::ShowWindow(m_hwndHelp, SW_SHOW);
		::ShowWindow(m_hwndTool, SW_SHOW);
	}

	// Note: The order of the calls in the rest of this method appear to be quite critical
	// due to a tree view bug relating to check boxes. The problem shows up when the
	// keyword list is the default list. Checked items in the choices list "lose" their
	// checkmarks when the user first switches to the choices tab. If you make any changes
	// to the following order, make sure this bug does not resurface.

	ShowCurrentChoices(m_fShowCurrent);
	ChangeDisplayOption(m_qpli->GetDisplayOption());

	TabCtrl_SetCurSel(m_hwndTab, m_iCurTabSel);

	::ShowWindow(m_hwnd, SW_SHOW);
	::UpdateWindow(m_hwnd);

	// Add the possibilities to the choices tree view.
	if (!AddPossibilities())
		return false;

	GetClientRect(rcClient);
	OnSize(kwstRestored, rcClient.Width(), rcClient.Height());

	m_qpcc->Redraw();

	SelectTab(m_iCurTabSel);

	::SetFocus(m_rghwndHistCombo[m_iCurTabSel]);

	AfApp::Papp()->EnableMainWindows(false);

	int cpss = m_qpli->GetCount();
	if (cpss && m_paoi && m_paoi->m_qvo)
	{
		int ctags;
		m_paoi->m_qvo->get_CTags(&ctags);
		Assert(ctags <= cpss);
		cpss = ctags;
	}

	// The UpdateWindow must be called twice in order for it to work when
	// different items use different fonts.
	::UpdateWindow(m_hwnd);
	::InvalidateRect(m_hwnd, NULL, FALSE);
	::UpdateWindow(m_hwnd);

	return false; // We just set the focus, so the system shouldn't do it.
}


/*----------------------------------------------------------------------------------------------
	Since there doesn't seem to be any way to trap the Enter key in a combo box and keep it
	from closing the dialog, this checks to see if one of the combo boxes currently has the
	focus. If it does, it adds the string to the drop down list and performs the
	appropriate action for that combo box.
	I (DarrellZ) first tried subclassing the edit box to handle the WM_KEYDOWN, WM_CHAR, and
	WM_KEYUP messages, but I never got any of these for the Enter key for some reason.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::OnApply(bool fClose)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::OnApply:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	HWND hwnd = ::GetFocus();
	if (::IsChild(m_rghwndHistCombo[0], hwnd) || ::IsChild(m_rghwndHistCombo[1], hwnd))
	{
		// The user hit the <Enter> key in a combobox.
		PerformComboAction();
		return true;
	}

	// Update m_vpssId.
	m_vpssId.Clear();
	UpdateSelectedItems(m_rghwndTree[kiChoiceList], TreeView_GetRoot(m_rghwndTree[kiChoiceList]));

	if (m_fAtomic && m_vpssId.Size() > 1)
	{
		// If this happens, we should just use the first one.
		// TODO (DarrellZ): This should ideally be handled elsewhere to ensure this case
		// never happens.
		m_vpssId.Resize(1);
	}

	bool fT = SuperClass::OnApply(fClose);
	if (m_fFromEditor)
	{
		// We've been called from a field editor. Since it is possible for a field editor
		// to be deleted and a new one to be created while this dialog is open (e.g.,
		// synchronization) we need to use this callback method to make sure we are using
		// the latest active field editor instead of the old carcass.
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
		AssertPtr(prmw);
		AfDeSplitChild * padsc = prmw->CurrentDeWnd();
		if (padsc)
		{
			AfDeFieldEditor * pdfe = padsc->GetActiveFieldEditor();
			if (pdfe)
				pdfe->ChooserApplied(this);
		}
	}
	return fT;
}


/*----------------------------------------------------------------------------------------------
	The dialog is being closed.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::OnCancel()
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::OnCancel:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	m_vpssId.Clear();
	return SuperClass::OnCancel();
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::FWndProc:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	switch (wm)
	{
	case WM_SIZING:
		return OnSizing(wp, (RECT *)lp);
	case WM_DESTROY:
		{
			HIMAGELIST himl;
			himl = (HIMAGELIST)::SendMessage(m_rghwndTree[kiChoiceList], TVM_GETIMAGELIST,
				TVSIL_STATE, 0);
			if (himl)
				ImageList_Destroy(himl);
			himl = (HIMAGELIST)::SendMessage(m_rghwndTree[kiKeywordSearch], TVM_GETIMAGELIST,
				TVSIL_STATE, 0);
			if (himl)
				ImageList_Destroy(himl);

			AfApp::Papp()->EnableMainWindows(true);
			SaveSettings(kpszPossChsrSubKey, false);
	#ifdef CRASH_WIN98
			m_qweb2.Clear();
	#endif
			m_fIgnoreSelChange = true;

			BOOL fSuccess;
			fSuccess = ::DestroyWindow(m_hwndHelp);
			Assert(fSuccess);
			m_hwndHelp = NULL;

			fSuccess = ::DestroyWindow(m_hwndGrip);
			Assert(fSuccess);
			m_hwndGrip = NULL;

			fSuccess = ::DestroyWindow(m_hwndTool);
			Assert(fSuccess);
			m_hwndTool = NULL;

			m_qpwe.Clear();
		}
		break;
	case WM_MOUSEMOVE:
		if (m_plddDragDrop.IsDragging())
		{
			m_plddDragDrop.MouseMove(LOWORD(lp), HIWORD(lp));
			return true;
		}
		break;
	case WM_LBUTTONUP:
		if (m_plddDragDrop.IsDragging())
		{
			if (m_plddDragDrop.EndDrag(LOWORD(lp), HIWORD(lp)))
			{
				// Update all lists now that we've changed.
				AfMainWnd * pafw = MainWindow();
				Assert(pafw);
				AfLpInfo * plpi = pafw->GetLpInfo();
				AssertPtr(plpi);
				m_hvoTarget = m_plddDragDrop.GetSourceHvo();
				SyncInfo sync(ksyncPossList, m_psslId, m_hvoTarget);
				return plpi->StoreAndSync(sync);
			}
			return true;
		}
		break;
	case WM_WINDOWPOSCHANGING:
		m_plddDragDrop.KillDrag();
		return false;
	case kCheckStateChange: // User-defined message
		UpdateCheckBoxes((HWND)wp, (HTREEITEM)lp);
		break;
	// default: nothing special.
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Resize/move all the controls on the dialog.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::OnSize(int wst, int dxp, int dyp)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::OnSize:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	// REVIEW DarrellZ: This might need to become non-static if it causes conflicts in
	// dialogs for different possibility lists.
	static int s_dxpOldLeft = dxp - m_dxpHelp;

	Rect rc;
	uint grfnMove = SWP_NOZORDER | SWP_NOSIZE;
	uint grfnSize = SWP_NOZORDER | SWP_NOMOVE;

	int dxpLeft = dxp;
	if (m_fShowHelp)
	{
		m_dxpHelp = dxp - s_dxpOldLeft;
		dxpLeft -= m_dxpHelp;
	}
	else
	{
		s_dxpOldLeft = dxp;
	}

	// Get the client size (in screen coordinates).
	Rect rcClient;
	GetClientRect(rcClient);
	::MapWindowPoints(m_hwnd, NULL, (POINT *)&rcClient, 2);

	// Resize the tab control.
	::GetWindowRect(m_hwndTab, &rc);
	int dzpGap = rc.left - rcClient.left;
	::SetWindowPos(m_hwndTab, NULL, 0, 0, dxpLeft - (dzpGap * 2), dyp - m_dypTab, grfnSize);

	int dypCurSel = 0;
	if (m_fShowCurrent)
		dypCurSel = m_dypCurSel + dzpGap;

	// Resize the two tree controls.
	Rect rcTree;
	::GetWindowRect(m_rghwndTree[0], &rcTree);
	int xp = rcTree.left - rcClient.left;
	int dypTree = dyp - m_dypTree - dypCurSel;
	::SetWindowPos(m_rghwndTree[0], NULL, 0, 0, dxpLeft - (xp * 2), dypTree, grfnSize);
	::SetWindowPos(m_rghwndTree[1], NULL, 0, 0, dxpLeft - (xp * 2), dypTree, grfnSize);

	// Move the owner-draw button which contains the current choices window...
	::SetWindowPos(::GetDlgItem(m_hwnd, kcidPossChsrChoices), HWND_TOP, xp,
		(rcTree.top - rcClient.top) + dypTree + dzpGap,
		dxpLeft - (xp * 2), m_dypCurSel, 0);
	// ...and the views window which is inside it moves automatically.
	::InvalidateRect(m_qpcc->Hwnd(), NULL, true);

	// Move the check box below the tree.
	int ypT = dyp - m_dypTree + (dzpGap * 2) + rcTree.top - rcClient.top;
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidShowChoices), NULL, xp, ypT,
		0, 0, grfnMove);
	::InvalidateRect(::GetDlgItem(m_hwnd, kctidShowChoices), NULL, true);

	// Resize the two combo box controls.
	::GetWindowRect(m_rghwndHistCombo[0], &rc);
	xp += rc.left - rcClient.left;
	::SetWindowPos(m_rghwndHistCombo[0], NULL, 0, 0, dxpLeft - xp, rc.Height(), grfnSize);
	::SetWindowPos(m_rghwndHistCombo[1], NULL, 0, 0, dxpLeft - xp, rc.Height(), grfnSize);

	// Move the five buttons.
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidHelp), NULL, dxpLeft - m_dxpOk - dzpGap,
		dyp - m_ypOk, 0, 0, grfnMove);
	::InvalidateRect(::GetDlgItem(m_hwnd, kctidHelp), NULL, true);

	::SetWindowPos(::GetDlgItem(m_hwnd, kctidCancel), NULL,
		dxpLeft - dzpGap - (m_dxpOk * 2) - m_dxpButtonSep, dyp - m_ypOk, 0, 0, grfnMove);
	::InvalidateRect(::GetDlgItem(m_hwnd, kctidCancel), NULL, true);

	::SetWindowPos(::GetDlgItem(m_hwnd, kctidOk), NULL,
		dxpLeft - dzpGap - (m_dxpOk * 3) - (m_dxpButtonSep * 2), dyp - m_ypOk, 0, 0, grfnMove);
	::InvalidateRect(::GetDlgItem(m_hwnd, kctidOk), NULL, true);

	::SetWindowPos(::GetDlgItem(m_hwnd, kctidModifyLst), NULL,
		dxpLeft - dzpGap - (m_dxpOk * 4) - (m_dxpButtonSep * 3), dyp - m_ypOk, 0, 0, grfnMove);
	::InvalidateRect(::GetDlgItem(m_hwnd, kctidModifyLst), NULL, true);

	::GetWindowRect(::GetDlgItem(m_hwnd, kctidToggleHelp), &rc);
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidToggleHelp), NULL, dxpLeft - dzpGap - rc.Width(),
		dyp - m_ypOk - (dzpGap * 2) - rc.Height(), 0, 0, grfnMove);
	::InvalidateRect(::GetDlgItem(m_hwnd, kctidToggleHelp), NULL, true);

	// Move the gripper to the bottom right.
	::GetWindowRect(m_hwndGrip, &rc);
	::MoveWindow(m_hwndGrip, dxp - rc.Width(), dyp - rc.Height(), rc.Width(),
		rc.Height(), true);
	::InvalidateRect(m_hwndGrip, NULL, true);

	// Move the help and toolbar windows.
	if (m_fShowHelp)
	{
		const int kypToolbar = 5;
		const int kdypToolbar = 46;
		if (m_hwndTool)
			::MoveWindow(m_hwndTool, dxp - m_dxpHelp, kypToolbar, m_dxpHelp,
				kdypToolbar - kypToolbar, true);
		if (m_hwndHelp)
			::MoveWindow(m_hwndHelp, dxp - m_dxpHelp, kdypToolbar, m_dxpHelp,
				dyp - m_dypHelp - kdypToolbar, true);
	}

	return SuperClass::OnSize(wst, dxp, dyp);
}


/*----------------------------------------------------------------------------------------------
	Make sure the dialog doesn't get resized smaller than a minimum size.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::OnSizing(int wse, RECT * prc)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::OnSizing:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	AssertPtr(prc);

	// TODO DarrellZ: Figure out what this number should really be.
	int dypMin = 215;
	if (m_fShowCurrent)
		dypMin += m_dypCurSel;
	if (prc->bottom - prc->top < dypMin)
	{
		if (wse == WMSZ_TOPLEFT || wse == WMSZ_TOP || wse == WMSZ_TOPRIGHT)
			prc->top = prc->bottom - dypMin;
		else
			prc->bottom = prc->top + dypMin;
	}

	int dxpMin = m_dxpMin;
	if (m_fShowHelp)
	{
		Rect rc;
		GetClientRect(rc);
		dxpMin = rc.Width() - m_dxpHelp + 50;
	}
	if (prc->right - prc->left < dxpMin)
	{
		if (wse == WMSZ_TOPLEFT || wse == WMSZ_LEFT || wse == WMSZ_BOTTOMLEFT)
			prc->left = prc->right - dxpMin;
		else
			prc->right = prc->left + dxpMin;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Add all the possibilities in the list to the Choices treeview control.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::AddPossibilities(HVO hvoSel)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::AddPossibilities:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	WaitCursor wc;

	if (!m_qpli)
		return false;

	// Add each item to the tree.
	FW_TVINSERTSTRUCT tvis;
	// memset needed because TssTreeView::InsertItem() is accessing one or more members
	// that are not initialized.
	memset(&tvis, 0, isizeof(tvis));
	tvis.hParent = TVI_ROOT;
	tvis.hInsertAfter = TVI_LAST;
	tvis.itemex.mask = TVIF_PARAM | TVIF_TEXT;

	Vector<HTREEITEM> vhti;
	vhti.Resize(8);
	vhti[0] = TVI_ROOT;
	HWND hwndTree = m_rghwndTree[kiChoiceList];

	::SendMessage(hwndTree, WM_SETREDRAW, false, 0);

	Vector<HTREEITEM> vhtiSel;
	Vector<HVO> vhvoPss = m_vpssId;
	int cpss = vhvoPss.Size();

	// Keep track of which item we're going to select initially.
	HTREEITEM htiSel = NULL;
	Set<HVO> setOverlay;
	if (m_paoi && m_paoi->m_qvo)
	{
		int ctag;
		m_paoi->m_qvo->get_CTags(&ctag);
		HVO hvo; // record ID in database of CmPossibility
		COLORREF clrFore;
		COLORREF clrBack;
		COLORREF clrUnder;
		int unt;
		ComBool fHidden;
		OLECHAR rgchGuid[isizeof(GUID)];
		for (int itag = 0; itag < ctag; ++itag)
		{
			CheckHr(m_paoi->m_qvo->GetDbTagInfo(itag, &hvo, &clrFore, &clrBack, &clrUnder, &unt,
				&fHidden, rgchGuid));
			setOverlay.Insert(hvo);
		}
	}

	int cpii = m_qpli->GetCount();
	if (cpii)
	{
		// Turn scrollbars back on for the Choices List tree view.
		DWORD dwT = ::GetWindowLong(m_rghwndTree[kiChoiceList], GWL_STYLE);
		::SetWindowLong(m_rghwndTree[kiChoiceList], GWL_STYLE, dwT & ~TVS_NOSCROLL);

		// Add the possibility items to the list.
		// DANGEROUS: this knows about Possibility list's internal storage of items!
		PossItemInfo * rgppii = m_qpli->GetPssFromIndex(0);
		AssertPtr(rgppii);
		int ilevel = 1;
		int ilevelNext = 1;
		int ipii;
		int ipiiNext;
		for (ipii = 0; ipii < cpii; ipii = ipiiNext)
		{
			HVO hvoPss = rgppii[ipii].GetPssId();
			ipiiNext = ipii + 1;
			if (m_paoi && setOverlay.IsMember(hvoPss))
			{
				while (ipiiNext < cpii)
				{
					HVO hvoPssNext = rgppii[ipiiNext].GetPssId();
					if (setOverlay.IsMember(hvoPssNext))
						break;
					++ipiiNext;
				}
			}
			if (!m_paoi || setOverlay.IsMember(hvoPss))
			{
				// If the next item has a greater level, it is a child of this item, so set the
				// children flag.
				if (ipiiNext < cpii)
					ilevelNext = rgppii[ipiiNext].GetHierLevel();
				if (ilevelNext > ilevel)
				{
					vhti.Resize(ilevelNext + 1);
					tvis.itemex.cChildren = 1;
				}
				else
				{
					tvis.itemex.cChildren = 0;
				}

				ITsStringPtr qtss;
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);

				StrUni stu;
				rgppii[ipii].GetName(stu, (PossNameType)m_pnt);
				qtsf->MakeStringRgch(stu.Chars(), stu.Length(), rgppii[ipii].GetWs(), &qtss);
				tvis.itemex.qtss = qtss;

				// Add the item.
				tvis.hParent = vhti[ilevel - 1];
				tvis.itemex.lParam = ipii;
				vhti[ilevel] = TreeView_InsertItem(hwndTree, &tvis);

				// See if this item should be checked.
				if (hvoPss == hvoSel)
					htiSel = vhti[ilevel];
				for (int ipss = 0; ipss < cpss; ipss++)
				{
					if (vhvoPss[ipss] == hvoPss)
					{
						cpss--;
						Assert(cpss >= 0);
						vhvoPss.Delete(ipss);
						// Keep track of this item for later.
						vhtiSel.Push(vhti[ilevel]);
						break;
					}
				}
				ilevel = ilevelNext;
			}
		}
	}

	int csel = vhtiSel.Size();
	Assert(!m_fAtomic || (uint)csel <= 1);

	// Set the checkbox for all the initial selected item(s).
	FW_TVITEM tvi;
	tvi.mask = TVIF_HANDLE | TVIF_STATE;
	tvi.stateMask = TVIS_STATEIMAGEMASK;
	tvi.state = INDEXTOSTATEIMAGEMASK(2);
	for (int isel = csel; --isel >= 0; )
	{
		// Expand necessary items to make this item visible.
		TreeView_EnsureVisible(hwndTree, vhtiSel[isel]);
		tvi.hItem = vhtiSel[isel];
		TreeView_SetItem(hwndTree, &tvi);
	}

	if (m_qpli->GetIsSorted())
	{
		TVSORTCB sortcb;
		sortcb.hParent = NULL;
		sortcb.lpfnCompare = TssTreeView::PossListCompareFunc;
		sortcb.lParam = (LPARAM)m_qpli.Ptr();
		TreeView_SortChildrenCB(hwndTree, &sortcb, true);
	}

	::SendMessage(hwndTree, WM_SETREDRAW, true, 0);

	// Select an initial item in the tree. This defaults to the top item in the tree.
	// NOTE: This has to be done after the treeview has been redrawn; otherwise it doesn't
	// always work correctly.
	if (!hvoSel && csel)
	{
		// Select the first checked item if any items are checked.
		htiSel = vhtiSel[0];
	}
	if (!htiSel)
		htiSel = TreeView_GetRoot(hwndTree);
	TreeView_SelectItem(hwndTree, htiSel);
	TreeView_EnsureVisible(hwndTree, htiSel);

	::UpdateWindow(hwndTree);
	::InvalidateRect(hwndTree, NULL, FALSE);
	::UpdateWindow(hwndTree);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Add all the possibilities that match the keyword to the Keywords treeview control.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::AddKeywords(achar * pszKeyword)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::AddKeywords:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	AssertPsz(pszKeyword);
	StrUni stuKeyWord(pszKeyword);
	AssertObj(m_qpli);

	WaitCursor wc;
	try
	{
		HTREEITEM hti;
		StrUni stu;
		Vector<HTREEITEM> vhtiSel;
		FW_TVINSERTSTRUCT tvis;
		// memset needed because TssTreeView::InsertItem() is accessing one or more members
		// that are not initialized.
		memset(&tvis, 0, isizeof(tvis));
		tvis.hParent = TVI_ROOT;
		tvis.hInsertAfter = TVI_LAST;
		tvis.itemex.mask = TVIF_PARAM | TVIF_TEXT;

		HWND hwndTree = m_rghwndTree[kiKeywordSearch];
		::SendMessage(hwndTree, WM_SETREDRAW, false, 0);

		m_fIgnoreSelChange = true;
		TreeView_DeleteAllItems(hwndTree);
		m_fIgnoreSelChange = false;
		int citems = m_qpli->GetCount();

		// Find out which items are checked.
		Vector<int> vipss;
		GetCheckedItems(m_rghwndTree[kiChoiceList],
			TreeView_GetRoot(m_rghwndTree[kiChoiceList]), vipss);
		int cpss = vipss.Size();
		Vector<HVO> vpssId;
		for (int ipss = 0; ipss < cpss; ipss++)
		{
			PossItemInfo * ppii = m_qpli->GetPssFromIndex(vipss[ipss]);
			AssertPtr(ppii);
			vpssId.Push(ppii->GetPssId());
		}

		OLECHAR * prgch = pszKeyword;
		StrUni stuT;
		int cch = StrLen(prgch);
		AfMainWnd * pafw = MainWindow();
		AssertPtr(pafw);
		AfLpInfo * plpi = pafw->GetLpInfo();
		AssertPtr(plpi);
		Locale loc = plpi->GetLocale(plpi->ActualWs(m_qpli->GetWs()));
		UnicodeString ust(prgch, cch);
		UnicodeString ustPattern;
		UErrorCode uerr = U_ZERO_ERROR;
		Normalizer::normalize(ust, UNORM_NFD, 0, ustPattern, uerr);
		Assert(U_SUCCESS(uerr));
		for (int iitem = 0; iitem < citems; iitem++)
		{
			PossItemInfo * ppii = m_qpli->GetPssFromIndex(iitem);
			AssertPtr(ppii);
			ppii->GetName(stuT, m_pnt);
			uerr = U_ZERO_ERROR;
			UnicodeString ustTarget(stuT.Chars());
			StringSearch * pss = new StringSearch(ustPattern, ustTarget, loc, NULL, uerr);
			Assert(U_SUCCESS(uerr));
			RuleBasedCollator * rbc = pss->getCollator();
			rbc->setStrength(Collator::SECONDARY); // We want a caseless search.
			uerr = U_ZERO_ERROR;
			pss->setCollator(rbc, uerr);
			Assert(U_SUCCESS(uerr));

			uerr = U_ZERO_ERROR;
			bool fFound = False;
			for (int pos = pss->first(uerr); pos != USEARCH_DONE;
				pos = pss->next(uerr))
			{
				Assert(U_SUCCESS(uerr));
				fFound = True;
				uerr = U_ZERO_ERROR;
			}
			Assert(U_SUCCESS(uerr));
			if (fFound)
			{
				// Add an item to the tree.
				tvis.itemex.lParam = iitem;
				ITsStringPtr qtss;
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);

				qtsf->MakeStringRgch(stuT.Chars(), stuT.Length(), ppii->GetWs(), &qtss);
				tvis.itemex.qtss = qtss;
				hti = TreeView_InsertItem(hwndTree, &tvis);

				// See if this item should be checked.
				for (int ipss = 0; ipss < cpss; ipss++)
				{
					if (vpssId[ipss] == ppii->GetPssId())
					{
						cpss--;
						Assert(cpss >= 0);
						vpssId.Delete(ipss);
						vhtiSel.Push(hti);
					}
				}
			}
			delete pss;
		}

		// Check all the initial selected items.
		FW_TVITEM tvi;
		tvi.mask = TVIF_HANDLE | TVIF_STATE;
		tvi.stateMask = TVIS_STATEIMAGEMASK;
		tvi.state = INDEXTOSTATEIMAGEMASK(2);
		cpss = vhtiSel.Size();
		for (int ipss = 0; ipss < cpss; ipss++)
		{
			tvi.hItem = vhtiSel[ipss];
			TreeView_SetItem(hwndTree, &tvi);
		}

		::SendMessage(hwndTree, WM_SETREDRAW, true, 0);

		// Turn scrollbars on if there are any items in the Keyword Search tree view.
		// Otherwise turn them off.
		DWORD dwT = ::GetWindowLong(m_rghwndTree[kiKeywordSearch], GWL_STYLE) & ~TVS_NOSCROLL;
		if (TreeView_GetCount(m_rghwndTree[kiKeywordSearch]) == 0)
			dwT |= TVS_NOSCROLL;
		::SetWindowLong(m_rghwndTree[kiKeywordSearch], GWL_STYLE, dwT);
	}
	catch (...)
	{
		return false;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle a change in a combo box.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::OnComboChange(NMHDR * pnmh, long & lnRet)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::OnComboChange:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	AssertPtr(pnmh);

	// Get the current index and text from the combo box.
	int icb = ::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0);
	if (icb == CB_ERR)
		return false;
	achar rgch[MAX_PATH];
	Vector<achar> vch;
	achar * pszT;
	int cch = ::SendMessage(pnmh->hwndFrom, CB_GETLBTEXTLEN, icb, (LPARAM)0);
	if (cch < MAX_PATH)
	{
		pszT = rgch;
	}
	else
	{
		vch.Resize(cch + 1);
		pszT = vch.Begin();
	}
	cch = ::SendMessage(pnmh->hwndFrom, CB_GETLBTEXT, icb, (LPARAM)pszT);
	if (cch < 0)
		pszT = _T("");

	switch (pnmh->idFrom)
	{
	case kctidChoiceHist:
		// Scroll to the desired item and select it.
		Assert(icb < kchtiHistory);
		if (icb)
		{
			// Update the keyword list to reflect the changed item in Most Recently Used order.
			::SendMessage(pnmh->hwndFrom, CB_DELETESTRING, icb, 0);
			int icbNew = ::SendMessage(pnmh->hwndFrom, CB_INSERTSTRING, 0, (LPARAM)pszT);
			::SendMessage(pnmh->hwndFrom, CB_SETCURSEL, icbNew, 0);
		}
		OnEditUpdate(pnmh);
		break;
	case kctidKeywordHist:
		Assert(icb < kchtiHistory);
		AddKeywords(pszT);
		if (icb)
		{
			// Update the keyword list to reflect the changed item in Most Recently Used order.
			::SendMessage(pnmh->hwndFrom, CB_DELETESTRING, icb, 0);
			int icbNew = ::SendMessage(pnmh->hwndFrom, CB_INSERTSTRING, 0, (LPARAM)pszT);
			::SendMessage(pnmh->hwndFrom, CB_SETCURSEL, icbNew, 0);
		}
		break;
	default:
		Assert(false);	// We shouldn't get here.
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Find the tree item starting with the text that has been typed so far.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::OnEditUpdate(NMHDR * pnmh)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::OnEditUpdate:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	// This should only work on the Choices tree view.
	if (m_iCurTabSel != kiChoiceList)
		return false;

	achar rgchBuffer[MAX_PATH];
	::SendMessage(pnmh->hwndFrom, WM_GETTEXT, MAX_PATH, (LPARAM)rgchBuffer);

	HWND hwndTree = m_rghwndTree[m_iCurTabSel];
	int cchMatched;
	HTREEITEM hti = NULL;
	int cch = StrLen(rgchBuffer);
	if (cch)
	{
		if (PossChsrComboEdit::s_fExtraBackspace)
			rgchBuffer[--cch] = 0;

		// REVIEW DarrellZ: Should the search start at the current selection or the top of the
		// tree. If it starts at the current selection, hitting backspace will not change the
		// selection. If it starts at the top of the tree, hitting backspace could change the
		// selection.
		/*HTREEITEM htiStart = TreeView_GetSelection(hwndTree);
		if (!htiStart)
			htiStart = TreeView_GetRoot(hwndTree);*/
		HTREEITEM htiStart = TreeView_GetRoot(hwndTree);

		hti = FindString(hwndTree, htiStart, rgchBuffer, cchMatched);
	}

	if (hti)
	{
		TreeView_SelectItem(hwndTree, hti);

		FW_TVITEM tvi;
		tvi.mask = TVIF_PARAM;
		tvi.hItem = hti;
		if (TreeView_GetItem(hwndTree, &tvi))
		{
			FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
			AssertPtr(pfti);
			PossItemInfo * ppii = m_qpli->GetPssFromIndex(pfti->lParam);
			AssertPtr(ppii);

			StrUni stu;
			ppii->GetName(stu, m_pnt);
			StrApp str(stu.Chars());

			::SendMessage(pnmh->hwndFrom, WM_SETTEXT, 0, (LPARAM)str.Chars());
			HWND hwndEdit = ::GetWindow(pnmh->hwndFrom, GW_CHILD);
			::SendMessage(hwndEdit, EM_SETSEL, str.Length(), cchMatched);

			// If the user typed an invalid key, beep.
			if (cchMatched < StrLen(rgchBuffer))
				::MessageBeep(MB_ICONEXCLAMATION);
		}
	}
	else
	{
		TreeView_SelectItem(hwndTree, NULL);
		::SendMessage(pnmh->hwndFrom, WM_SETTEXT, 0, (LPARAM)_T(""));
		if (cch > 0)
			::MessageBeep(MB_ICONEXCLAMATION);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	If the user clicked on the check box, select the item and if atomic, deselect all others.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::OnClick(NMHDR * pnmh)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::OnClick:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	AssertPtr(pnmh);

	if (pnmh->hwndFrom == m_rghwndTree[kiChoiceList] ||
		pnmh->hwndFrom == m_rghwndTree[kiKeywordSearch])
	{
		// Determine which item has been clicked on:
		TVHITTESTINFO tvhti = {0};
		DWORD nPos = ::GetMessagePos();
		tvhti.pt.x = GET_X_LPARAM(nPos);
		tvhti.pt.y = GET_Y_LPARAM(nPos);
		::MapWindowPoints(HWND_DESKTOP, pnmh->hwndFrom, &tvhti.pt, 1);
		TreeView_HitTest(pnmh->hwndFrom, &tvhti);

		if (TVHT_ONITEMSTATEICON & tvhti.flags)
		{
			// The state does not get set by Windows until after this message is processed,
			// (see ms-help://MS.VSCC/MS.MSDNVS/kbwin32/Source/win32sdk/q261289.htm)
			// so send ourselves another message ready to continue with this action:
			Assert(tvhti.hItem);
			PostMessage(m_hwnd, kCheckStateChange, (WPARAM)pnmh->hwndFrom, (LPARAM)tvhti.hItem);
#if 0
			HTREEITEM hti = TreeView_GetSelection(pnmh->hwndFrom);
			if (hti == tvhti.hItem)
			{
				// Show an edit box to allow the user to edit the selected list item.
				Rect rc;
				TreeView_GetItemRect(pnmh->hwndFrom, hti, &rc, true);

				// ENHANCE DarrellZ: Figure out which part of the item the user clicked on.
				if (m_pnt != kpntName)
				{
				}
				return false;
			}
#endif // 0
			// Select the item, so its web window help will appear:
			TreeView_SelectItem(pnmh->hwndFrom, tvhti.hItem);
			return true;
		}
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Show the child windows (tree and combo) for the new tab and hide the old ones.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::SelectTab(int itab)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::SelectTab:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	::ShowWindow(m_rghwndTree[1 - itab], SW_HIDE);
	::ShowWindow(m_rghwndHistCombo[1 - itab], SW_HIDE);

	::ShowWindow(m_rghwndTree[itab], SW_SHOW);
	::ShowWindow(m_rghwndHistCombo[itab], SW_SHOW);

	StrApp str(itab == 0 ? kstidPssItem : kstidKeyword);
	BOOL fSuccess;
	fSuccess = ::SetDlgItemText(m_hwnd, kctidTreeEditLabel, str.Chars());
	Assert(fSuccess);

	m_iCurTabSel = itab;

	FW_TVITEM tvi;
	tvi.mask = TVIF_PARAM;
	tvi.hItem = TreeView_GetSelection(m_rghwndTree[itab]);
	if (TreeView_GetItem(m_rghwndTree[itab], &tvi))
	{
		FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
		AssertPtr(pfti);
		OnSelChanged(pfti->lParam);
	}
	::SetFocus(m_rghwndHistCombo[itab]);
	::SendMessage(m_rghwndHistCombo[itab], CB_SETEDITSEL, 0, MAKELPARAM(0, -1));
}


/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::OnNotifyChild:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case CBN_EDITUPDATE:
		return OnEditUpdate(pnmh);

	case CBN_SELENDOK:
		return OnComboChange(pnmh, lnRet);

	case CBN_KILLFOCUS:
		if (m_iCurTabSel == kiChoiceList && pnmh->hwndFrom == m_rghwndHistCombo[kiChoiceList])
			PerformComboAction();				// Save this in the list.
		break;

	case CBN_SELENDCANCEL:
		if (pnmh->hwndFrom == m_rghwndHistCombo[kiChoiceList])
		{
			FW_TVITEM tvi;
			tvi.mask = TVIF_TEXT;
			tvi.hItem = TreeView_GetSelection(m_rghwndTree[kiChoiceList]);

			// Make sure the item it's looking for is found in the tree.
			// This caused a problem when the tree was empty.
			if (TreeView_GetItem(m_rghwndTree[kiChoiceList], &tvi))
			{
				const OLECHAR * pwrgch;
				int cch;
				CheckHr(tvi.qtss->LockText(&pwrgch, &cch));
				StrApp str;
				str.Assign(pwrgch, cch);
				tvi.qtss->UnlockText(pwrgch);
				::SetWindowText(pnmh->hwndFrom, str.Chars());
			}
			return true;
		}
		break;

	case TCN_SELCHANGE:
		SelectTab(TabCtrl_GetCurSel(pnmh->hwndFrom));
		return true;

	case NM_RCLICK:
		{
			Point pt;
			::GetCursorPos(&pt);

			TVHITTESTINFO tvhti;
			::GetCursorPos(&tvhti.pt);
			::ScreenToClient(m_rghwndTree[m_iCurTabSel], &tvhti.pt);

			if (TreeView_HitTest(m_rghwndTree[m_iCurTabSel], &tvhti))
			{
				if (tvhti.hItem)
				{
					// Select the item that was right clicked on
					TreeView_SelectItem(m_rghwndTree[m_iCurTabSel], tvhti.hItem);
				}
			}
			ContextMenu(m_hwnd, pt, NULL);
			return true;
		}

	case TVN_GETDISPINFO:
		return OnGetDispInfo((NMTVDISPINFO *)pnmh);

	case TVN_SELCHANGED:
		if (((NMTREEVIEW *)pnmh)->itemNew.hItem != NULL)
		{
			FwTreeItem * pfti = (FwTreeItem *)(((NMTREEVIEW *)pnmh)->itemNew.lParam);
			AssertPtr(pfti);
			return OnSelChanged(pfti->lParam);
		}
		break;

	case TVN_BEGINDRAG:
		if (pnmh->hwndFrom == m_rghwndTree[kiChoiceList])
			return m_plddDragDrop.BeginDrag(pnmh);
		break;

	case NM_CLICK:
		return OnClick(pnmh);

	case BN_CLICKED:
		switch (pnmh->idFrom)
		{

		case kctidModifyLst:
			{
				Rect rc;
				::GetWindowRect(pnmh->hwndFrom, &rc);
				TPMPARAMS tpm = { isizeof(tpm) };
				tpm.rcExclude = rc;
				ContextMenu(pnmh->hwndFrom, Point(rc.left, rc.bottom), &tpm);
				return true;
			}

		case kctidToggleHelp:
			ShowHelp(!m_fShowHelp);
			return true;

		case kctidShowChoices:
			ShowCurrentChoices(!m_fShowCurrent);
			return true;

		case kcidPossBack:
#ifdef CRASH_WIN98
			CheckHr(m_qweb2->GoBack());
#endif
			break;

		case kcidPossForward:
#ifdef CRASH_WIN98
			CheckHr(m_qweb2->GoForward());
#endif
			break;

		case kcidPossPrint:
#ifdef CRASH_WIN98
			{
				// Print contents of WebBrowser control.
				IDispatchPtr qdisp;
				ComSmartPtr<IOleCommandTarget> qoct;
				CheckHr(m_qweb2->get_Document(&qdisp));
				CheckHr(qdisp->QueryInterface(IID_IOleCommandTarget, (void **)&qoct));
				qoct->Exec(NULL, OLECMDID_PRINT, OLECMDEXECOPT_DODEFAULT, NULL, NULL);
			}
#endif
			return true;
		}
		break;

	default:
		break;
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Create and show the context menu for Right Click and Modify.

	@param hwnd handle to menu owner window
	@param pt point to position the menu at

	@return true
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::ContextMenu(HWND hwnd, Point pt, TPMPARAMS * ptpm)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::ContextMenu:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	HMENU hmenuPopup = ::CreatePopupMenu();

	StrApp str;
	AfUtil::GetResourceStr(krstItem, kcidPossDispName, str);
	::AppendMenu(hmenuPopup, MF_STRING, kcidPossDispName, str.Chars());
	AfUtil::GetResourceStr(krstItem, kcidPossDispAbbrev, str);
	::AppendMenu(hmenuPopup, MF_STRING, kcidPossDispAbbrev, str.Chars());
	AfUtil::GetResourceStr(krstItem, kcidPossDispNameAbbrev, str);
	::AppendMenu(hmenuPopup, MF_STRING, kcidPossDispNameAbbrev, str.Chars());
	::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);

	if (m_fOverlay)
	{
		// If m_paoi is true, we don't want to do any of this since we're really
		// only showing items that are included in the overlay. We shouldn't be
		// modifying the overlay at all.
		if (!m_paoi)
		{
			str.Load(kcidPossExcludeTag);
			::AppendMenu(hmenuPopup, MF_STRING, kcidPossExcludeTag, str.Chars());
			str.Load(kcidPossExcludeSubtags);
			::AppendMenu(hmenuPopup, MF_STRING, kcidPossExcludeSubtags, str.Chars());
			str.Load(kcidPossIncludeSubtags);
			::AppendMenu(hmenuPopup, MF_STRING, kcidPossIncludeSubtags, str.Chars());

			::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
		}
	}
	else if (m_iCurTabSel == kiChoiceList)
	{
		if (m_qpli->GetCount())
		{
			if (m_qpli->GetIsSorted())
			{
				AfUtil::GetResourceStr(krstItem, kcidPossTrInsert, str);
				::AppendMenu(hmenuPopup, MF_STRING, kcidPossTrInsert, str.Chars());
			}
			else
			{
				AfUtil::GetResourceStr(krstItem, kcidPossTrInsertBef, str);
				::AppendMenu(hmenuPopup, MF_STRING, kcidPossTrInsertBef, str.Chars());
				AfUtil::GetResourceStr(krstItem, kcidPossTrInsertAft, str);
				::AppendMenu(hmenuPopup, MF_STRING, kcidPossTrInsertAft, str.Chars());
			}

			if (m_qpli->GetDepth() > 1)
			{
				AfUtil::GetResourceStr(krstItem, kcidPossTrInsertSub, str);
				::AppendMenu(hmenuPopup, MF_STRING, kcidPossTrInsertSub, str.Chars());
			}
		}
		else
		{
			AfUtil::GetResourceStr(krstItem, kcidPossTrInsert, str);
			::AppendMenu(hmenuPopup, MF_STRING, kcidPossTrInsert, str.Chars());
		}

		AfUtil::GetResourceStr(krstItem, kcidPossTrRename, str);
		::AppendMenu(hmenuPopup, MF_STRING, kcidPossTrRename, str.Chars());
		AfUtil::GetResourceStr(krstItem, kcidPossTrMerge, str);
		::AppendMenu(hmenuPopup, MF_STRING, kcidPossTrMerge, str.Chars());
		AfUtil::GetResourceStr(krstItem, kcidPossTrDelete, str);
		::AppendMenu(hmenuPopup, MF_STRING, kcidPossTrDelete, str.Chars());
		::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
	}

	AfUtil::GetResourceStr(krstItem, kcidPossTrEditList, str);
	::AppendMenu(hmenuPopup, MF_STRING, kcidPossTrEditList, str.Chars());

	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	int wsUser = pafw->UserWs();
	Assert(wsUser);

	TrackPopupWithHelp(hmenuPopup, TPM_LEFTALIGN | TPM_TOPALIGN | TPM_VERTICAL |
		TPM_RIGHTBUTTON, pt.x, pt.y, wsUser, ptpm);

	::DestroyMenu(hmenuPopup);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Show help information for the requested control.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::OnHelpInfo(HELPINFO * phi)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::OnHelpInfo:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	AssertPtr(phi);
	if (m_fOverlay)
	{
		switch (phi->iCtrlId)
		{
		case kctidTreeEditLabel: // fall through
		case kctidChoiceHist: phi->iCtrlId = kctidOvlyChoiceHist; break;
		case kctidKeywordHist: phi->iCtrlId = kctidOvlyKeywordHist; break;
		case kctidChoiceTree: phi->iCtrlId = kctidOvlyChoiceTree; break;
		case kctidKeywordTree: phi->iCtrlId = kctidOvlyKeywordTree; break;
		case kctidShowChoices: phi->iCtrlId = kctidOvlyShowChoices; break;
		case kctidToggleHelp: phi->iCtrlId = kctidOvlyToggleHelp; break;
		case kcidPossChsrChoices: phi->iCtrlId = kctidOvlyCurrentChoices; break;
		case kctidModifyLst: phi->iCtrlId = kctidOvlyModifyLst; break;
		case kctidOk: phi->iCtrlId = kctidOvlyOK; break;

		case kctidTab:
			{
				// See which tab the user clicked on.
				TCHITTESTINFO thti;
				thti.pt = phi->MousePos;
				::ScreenToClient(m_hwndTab, &thti.pt);
				int itab = TabCtrl_HitTest(m_hwndTab, &thti);
				if (itab == -1)
					itab = m_iCurTabSel; // Use the current tab if the user didn't click on a tab.
				if (itab == kiChoiceList)
					phi->iCtrlId = kctidOvlyTabChoice;
				else if (itab == kiKeywordSearch)
					phi->iCtrlId = kctidOvlyTabKeyword;
				else
					return false;
			}
			break;
		}
	}

	return SuperClass::OnHelpInfo(phi);
}


/*----------------------------------------------------------------------------------------------
	Show the appropriate text for an item based on the view of item selection.
	This gets called every time an item needs to be drawn.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::OnGetDispInfo(NMTVDISPINFO * pntdi)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::OnGetDispInfo:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	// This routine should not be needed any more (for text retrieval)
	Assert(false);
/*
	AssertPtr(pntdi);
	Assert(pntdi->item.mask == TVIF_TEXT);
	AssertObj(m_qpli);

	FwTreeItem * pfti = (FwTreeItem *)(pntdi->item.lParam);
	AssertPtr(pfti);
	PossItemInfo * ppii = m_qpli->GetPssFromIndex(pfti->lParam);
	AssertPtr(ppii);
	StrUni stu;
	ppii->GetName(stu, m_pnt);

	ITsStringPtr qtss;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);

	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_qlpi->GetDbInfo()->UserWs(), &pfti->qtss);

	StrApp str(stu);
	lstrcpy(pntdi->item.pszText, str.Chars());
*/
	return true;
}


/*----------------------------------------------------------------------------------------------
	Show the help information for the new item if needed.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::OnSelChanged(int ipss)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::OnSelChanged:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	// This keeps us from getting in an infinite recursive loop.
	if (m_fIgnoreSelChange)
		return true;

	// We can have a bum index here if the first item was selected at the time, but was
	// deleted. In that case, we should set the context to -1 (no value selected) and
	// then ignore this request.
	if (ipss >= m_qpli->GetCount()) {
		m_ipssContext = -1;
		return true;
	}

	m_ipssContext = ipss;
	PossItemInfo * ppii = m_qpli->GetPssFromIndex(ipss);
	AssertPtr(ppii);
	StrUni stu;

	// If the choices tab is active, update the text of the combo box to match
	// the new selection.
	if (m_iCurTabSel == kiChoiceList)
	{
		ppii->GetName(stu, m_pnt);
		StrApp str(stu);
		::SetWindowText(m_rghwndHistCombo[kiChoiceList], str.Chars());
	}

	// Select the same item in the other tree control.
	HWND hwndTree = m_rghwndTree[1 - m_iCurTabSel];
	HTREEITEM hti = FindPss(hwndTree, TreeView_GetRoot(hwndTree), ipss);
	if (hti)
	{
		TreeView_SelectItem(hwndTree, hti);
		TreeView_EnsureVisible(hwndTree, hti);
	}

	// If we're not showing the help page, we're done.
	if (!m_fShowHelp)
		return true;

	// Navigating to a different URL will cause this method be called again, so set
	// m_fIgnoreSelChange to true before calling UpdateHelp.
	m_fIgnoreSelChange = true;
	m_qpwe->UpdateHelp(m_qpli, ppii->GetPssId());
	m_fIgnoreSelChange = false;

	return true;
}

/*----------------------------------------------------------------------------------------------
	Recursively add every checked item to m_vpssId.
	@param hwndTree Tree view window.
	@param hti Tree item whose checkbox is currently under consideration.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::UpdateSelectedItems(HWND hwndTree, HTREEITEM hti)
{
	if (!hti)
		return;

	FW_TVITEM tvi;
	tvi.mask = TVIF_PARAM;
	if (TreeView_GetCheckState(hwndTree, hti))
	{
		tvi.hItem = hti;
		if (TreeView_GetItem(hwndTree, &tvi))
		{
			FwTreeItem * pfti = (FwTreeItem *)(tvi.lParam);
			AssertPtr(pfti);
			PossItemInfo * ppii = m_qpli->GetPssFromIndex(pfti->lParam);
			AssertPtr(ppii);
			m_vpssId.Push(ppii->GetPssId());
		}
	}

	UpdateSelectedItems(hwndTree, TreeView_GetChild(hwndTree, hti));
	UpdateSelectedItems(hwndTree, TreeView_GetNextSibling(hwndTree, hti));
}


/*----------------------------------------------------------------------------------------------
	Recursively clear check boxes of all Tree items, except the specified item.
	@param hwndTree Tree view window.
	@param htiCandidate Tree item whose checkbox is currently under consideration.
	@param htiProtect Tree item that must not be altered.
	The base case is when htiCandidate is NULL.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::ResetCheckBoxes(HWND hwndTree, HTREEITEM htiCandidate, HTREEITEM htiProtect)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::ResetCheckBoxes:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	if (!htiCandidate)
		return; // Recursion base case

	if (htiCandidate != htiProtect)
		TreeView_SetCheckState(hwndTree, htiCandidate, false);

	ResetCheckBoxes(hwndTree, TreeView_GetChild(hwndTree, htiCandidate), htiProtect);
	ResetCheckBoxes(hwndTree, TreeView_GetNextSibling(hwndTree, htiCandidate), htiProtect);
}


/*----------------------------------------------------------------------------------------------
	Maintain integrity of TreeView check boxes.
	@param hwndTree Tree view window that owns the item that has just been changed.
	@param hti Tree item that has just been changed.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::UpdateCheckBoxes(HWND hwndTree, HTREEITEM hti)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::UpdateCheckBoxes:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	Assert(hwndTree);
	Assert(hti);

	bool fChecked = TreeView_GetCheckState(hwndTree, hti);

	FW_TVITEM tvi;
	tvi.mask = TVIF_PARAM;
	tvi.hItem = hti;
	TreeView_GetItem(hwndTree, &tvi);

	if (fChecked && m_fAtomic)
	{
		// Make sure no other item is selected in the tree:
		HTREEITEM htiRoot = TreeView_GetRoot(hwndTree);
		ResetCheckBoxes(hwndTree, htiRoot, hti);
	}

	// Select/Deselect the same item in the other tree if it is there.
	HWND hwndOtherTree;
	if (hwndTree == m_rghwndTree[kiChoiceList])
		hwndOtherTree = m_rghwndTree[kiKeywordSearch];
	else
		hwndOtherTree = m_rghwndTree[kiChoiceList];

	FwTreeItem * pfti = (FwTreeItem *)(tvi.lParam);
	AssertPtr(pfti);
	PossItemInfo * ppii = m_qpli->GetPssFromIndex(pfti->lParam);
	AssertPtr(ppii);
	StrUni stu;
	ppii->GetName(stu, m_pnt);
	StrApp str(stu);

	int cchMatched;
	hti = FindString(hwndOtherTree, TreeView_GetRoot(hwndOtherTree),
		const_cast<achar *>(str.Chars()), cchMatched);
	if (hti && cchMatched == str.Length())
		TreeView_SetCheckState(hwndOtherTree, hti, fChecked);

	if (m_fAtomic && fChecked)
	{
		// Make sure no other item is selected in the tree:
		HTREEITEM htiRoot = TreeView_GetRoot(hwndOtherTree);
		ResetCheckBoxes(hwndOtherTree, htiRoot, (cchMatched == str.Length()) ? hti : NULL);
	}
	m_qpcc->Redraw();
}


/*----------------------------------------------------------------------------------------------
	Select the corresponding item in the tree to the left. Returns E_FAIL if the web browser
	should not show the requested page.
----------------------------------------------------------------------------------------------*/
HRESULT PossChsrDlg::UpdateTree(wchar * pszUrl)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::UpdateTree:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	AssertPsz(pszUrl);

	if (m_fIgnoreSelChange)
		return S_OK;

	HWND hwndTree = m_rghwndTree[kiChoiceList];

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
		HTREEITEM hti = FindHvo(hwndTree, TreeView_GetRoot(hwndTree), hvoPss);
		// Select the new item in both trees, starting with the choice list first.
		m_fIgnoreSelChange = true;
		if (hti)
		{
			TreeView_SelectItem(hwndTree, hti);

			// Update the text in the Choice List combo box.
			FW_TVITEM tvi;
			tvi.mask = TVIF_TEXT;
			tvi.hItem = hti;
			TreeView_GetItem(hwndTree, &tvi);
			DWORD ichSelStart;
			DWORD ichSelStop;
			::SendMessage(m_rghwndHistCombo[kiChoiceList], CB_GETEDITSEL,
				(WPARAM)&ichSelStart, (LPARAM)&ichSelStop);

			const OLECHAR * pwrgch;
			int cch;
			CheckHr(tvi.qtss->LockText(&pwrgch, &cch));
			StrApp str;
			str.Assign(pwrgch, cch);
			tvi.qtss->UnlockText(pwrgch);

			::SetWindowText(m_rghwndHistCombo[kiChoiceList], str.Chars()/*rgch*/);
			::SendMessage(m_rghwndHistCombo[kiChoiceList], CB_SETEDITSEL,
				0, MAKELPARAM(ichSelStart, ichSelStop));
		}
		hwndTree = m_rghwndTree[kiKeywordSearch];
		hti = FindHvo(hwndTree, TreeView_GetRoot(hwndTree), hvoPss);
		if (hti)
			TreeView_SelectItem(hwndTree, hti);
		m_fIgnoreSelChange = false;
	}

	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Show or hide the help pane.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::ShowHelp(bool fShow)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::ShowHelp:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	if (m_fShowHelp == fShow)
		return;
	m_fShowHelp = fShow;

	StrApp str(fShow ? kstidLess : kstidMore);
	::SetWindowText(::GetDlgItem(m_hwnd, kctidToggleHelp), str.Chars());

	Rect rc;
	::GetWindowRect(m_hwnd, &rc);

	if (fShow)
	{
		rc.right += m_dxpHelp;
		AfGfx::EnsureVisibleRect(rc);
		::MoveWindow(m_hwnd, rc.left, rc.top, rc.Width(), rc.Height(), true);

		// Update the contents of the help window.
		HWND hwndTree = m_rghwndTree[m_iCurTabSel];
		FW_TVITEM tvi;
		tvi.mask = TVIF_PARAM;
		tvi.hItem = TreeView_GetSelection(hwndTree);
		if (TreeView_GetItem(hwndTree, &tvi))
		{
			FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
			AssertPtr(pfti);
			OnSelChanged(pfti->lParam);
		}

		::ShowWindow(m_hwndHelp, SW_SHOW);
		::ShowWindow(m_hwndTool, SW_SHOW);
	}
	else
	{
		::ShowWindow(m_hwndHelp, SW_HIDE);
		::ShowWindow(m_hwndTool, SW_HIDE);

		::SetWindowPos(m_hwnd, NULL, 0, 0, rc.Width() - m_dxpHelp, rc.Height(),
			SWP_NOZORDER | SWP_NOMOVE);
	}
}


/*----------------------------------------------------------------------------------------------
	Change how the tree draws each of its items.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::ChangeDisplayOption(PossNameType pnt)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::ChangeDisplayOption:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	if (m_pnt == pnt)
		return;
	m_pnt = pnt;

	// Force a refresh so the text of each item is drawn with the new display option.
	::InvalidateRect(m_rghwndTree[m_iCurTabSel], NULL, true);

	// REVIEW DarrellZ: Do we really want to reset the content of the choices combobox when
	// we switch the display option? This was initially done because of the confusion the user
	// might have in seeing different settings (i.e. name only) in the strings the combobox
	// had saved.
	::SendMessage(m_rghwndHistCombo[kiChoiceList], CB_RESETCONTENT, 0, 0);

	m_qpcc->Redraw();
}


/*----------------------------------------------------------------------------------------------
	Show or hide the window that shows the currently selected choices.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::ShowCurrentChoices(bool fShow)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::ShowCurrentChoices:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	m_fShowCurrent = fShow;
	::SendMessage(::GetDlgItem(m_hwnd, kctidShowChoices), BM_SETCHECK,
		fShow ? BST_CHECKED : BST_UNCHECKED, 0);

	if (fShow)
		m_qpcc->Redraw();

	// The window is hidden before we resize so we don't see it move down right before it
	// disappears.
	if (!fShow)
		::ShowWindow(m_qpcc->Hwnd(), SW_HIDE);

	// Force a resize.
	Rect rc;
	GetClientRect(rc);
	OnSize(kwstRestored, rc.Width(), rc.Height());

	// We wait to show the window so that it is moved to the correct position before it
	// becomes visible.
	if (fShow)
		::ShowWindow(m_qpcc->Hwnd(), SW_SHOW);
}


/*----------------------------------------------------------------------------------------------
	Find an item in the tree that matches the given string starting at the given item.
	cchMatched will contain the length of the tree item name that matches most with the string.
----------------------------------------------------------------------------------------------*/
HTREEITEM PossChsrDlg::FindString(HWND hwndTree, HTREEITEM hti, achar * psz, int & cchMatched)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::FindString:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	AssertPsz(psz);

	cchMatched = 0;
	if (!hti)
		return NULL;

	FW_TVITEM tvi;
	tvi.mask = TVIF_PARAM;
	int cchFind = StrLen(psz);
	int chti = TreeView_GetCount(hwndTree);

	// Convert the search string to lowercase.
	StrUni stuFind(psz);
	stuFind.ToLower();

	HTREEITEM htiBestMatch = NULL;

	for (int ihti = 0; ihti < chti; ihti++)
	{
		tvi.hItem = hti;
		if (!TreeView_GetItem(hwndTree, &tvi))
			return NULL;
		FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
		AssertPtr(pfti);
		PossItemInfo * ppii = m_qpli->GetPssFromIndex(pfti->lParam);
		AssertPtr(ppii);

		// See how many characters in the two strings match.
		StrUni stu;
		ppii->GetName(stu, m_pnt);
		OLECHAR * prgchFind = (OLECHAR *)stuFind.Chars() - 1;
		OLECHAR * prgch = (OLECHAR *)stu.Chars() - 1;
		OLECHAR * prgchFindStop = prgchFind + cchFind + 1;
		AssertPsz(prgchFind + 1);
		AssertPsz(prgch + 1);
		while (prgchFind < prgchFindStop && *++prgchFind && *++prgch)
		{
			if (*prgchFind != towlower(*prgch))
				break;
		}
		int cch = prgchFind - stuFind.Chars();
		if (cch > cchMatched)
		{
			cchMatched = cch;
			htiBestMatch = hti;
		}
		if (cch == cchFind)
			return hti;

		// Look at the next item in the tree.
		HTREEITEM htiT = TreeView_GetChild(hwndTree, hti);
		if (!htiT)
		{
			while ((htiT = TreeView_GetNextSibling(hwndTree, hti)) == NULL)
			{
				if ((hti = TreeView_GetParent(hwndTree, hti)) == NULL)
				{
					// Start over from the top of the tree.
					htiT = TreeView_GetRoot(hwndTree);
					break;
				}
			}
			if (!htiT)
				htiT = hti;
		}
		hti = htiT;
		Assert(hti);
	}

	// There wasn't a perfect match, so return the handle of the item that matched the most
	// characters.
	return htiBestMatch;
}


/*----------------------------------------------------------------------------------------------
	Adds a new item to the combo list at the top of the list (deleting the item if it was
	already in the list. If there are too many items in the list, the last one is thrown out.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::PerformComboAction()
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::PerformComboAction:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	achar rgchT[MAX_PATH];
	HWND hwndCombo = m_rghwndHistCombo[m_iCurTabSel];
	::SendMessage(hwndCombo, WM_GETTEXT, MAX_PATH, (LPARAM)rgchT);

	if (rgchT[0])
	{
		int iitem = ::SendMessage(hwndCombo, CB_FINDSTRINGEXACT, (WPARAM)-1, (LPARAM)rgchT);
		if (iitem != CB_ERR)
			::SendMessage(hwndCombo, CB_DELETESTRING, iitem, 0);
		int iitemNew = ::SendMessage(hwndCombo, CB_INSERTSTRING, 0, (LPARAM)rgchT);
		::SendMessage(hwndCombo, CB_SETCURSEL, iitemNew, 0);
		// If we went past the history limit, delete the oldest item.
		if (::SendMessage(hwndCombo, CB_GETCOUNT , 0, 0) > kchtiHistory)
			::SendMessage(hwndCombo, CB_DELETESTRING, kchtiHistory, 0);
		if (m_iCurTabSel == kiKeywordSearch)
			AddKeywords(rgchT);
	}

//	::SetFocus(m_rghwndTree[m_iCurTabSel]);
}


/*----------------------------------------------------------------------------------------------
	Go recursively through the tree adding the checked items to a vector.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::GetCheckedItems(HWND hwndTree, HTREEITEM hti, Vector<int> & vipss)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::GetCheckedItems:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	if (!hti)
		return;

	FW_TVITEM tvi;
	tvi.mask = TVIF_HANDLE | TVIF_STATE | TVIF_PARAM;
	tvi.stateMask = TVIS_STATEIMAGEMASK;
	while (hti)
	{
		tvi.hItem = hti;
		if (TreeView_GetItem(hwndTree, &tvi))
		{
			if ((tvi.state >> 12) - 1)
			{
				FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
				AssertPtr(pfti);
				vipss.Push(pfti->lParam);
			}
		}
		HTREEITEM htiT = TreeView_GetChild(hwndTree, hti);
		if (htiT)
			GetCheckedItems(hwndTree, htiT, vipss);
		hti = TreeView_GetNextSibling(hwndTree, hti);
	}
}


/*----------------------------------------------------------------------------------------------
	This method recursively searches through the tree to find the item that corresponds to the
	given hvoPss. This is used to determine which item to select when the user jumps to a new
	page within the HTML help window.
----------------------------------------------------------------------------------------------*/
HTREEITEM PossChsrDlg::FindHvo(HWND hwndTree, HTREEITEM hti, HVO hvoPss)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::FindHvo:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	FW_TVITEM tvi;
	tvi.mask = TVIF_HANDLE | TVIF_PARAM;
	while (hti)
	{
		tvi.hItem = hti;
		if (TreeView_GetItem(hwndTree, &tvi))
		{
			FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
			AssertPtr(pfti);
			PossItemInfo * ppii = m_qpli->GetPssFromIndex(pfti->lParam);
			AssertPtr(ppii);
			if (ppii->GetPssId() == hvoPss)
			{
				// We have found a match.
				return hti;
			}
		}
		HTREEITEM htiT = TreeView_GetChild(hwndTree, hti);
		if (htiT)
		{
			htiT = FindHvo(hwndTree, htiT, hvoPss);
			if (htiT)
				return htiT;
		}
		hti = TreeView_GetNextSibling(hwndTree, hti);
	}
	return NULL;
}


/*----------------------------------------------------------------------------------------------
	This method recursively searches through the tree to find the item that corresponds to the
	given choices list item. This is used to determine which item to select in a tree when the
	user selects an item in the other tree.
----------------------------------------------------------------------------------------------*/
HTREEITEM PossChsrDlg::FindPss(HWND hwndTree, HTREEITEM hti, int ipss)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::FindPss:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	FW_TVITEM tvi;
	tvi.mask = TVIF_HANDLE | TVIF_PARAM;
	while (hti)
	{
		tvi.hItem = hti;
		if (TreeView_GetItem(hwndTree, &tvi))
		{
			FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
			AssertPtr(pfti);
			if (pfti->lParam == ipss)
				return hti; // We have found a match.
		}

		HTREEITEM htiT = TreeView_GetChild(hwndTree, hti);
		if (htiT)
		{
			htiT = FindPss(hwndTree, htiT, ipss);
			if (htiT)
				return htiT;
		}
		hti = TreeView_GetNextSibling(hwndTree, hti);
	}
	return NULL;
}


/*----------------------------------------------------------------------------------------------
	Read user interface settings.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::LoadSettings(const achar * pszRoot, bool fRecursive)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::LoadSettings:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	Assert(pszRoot);
	Assert(m_hwnd); // The dialog must have already been created before this can be called.

	SuperClass::LoadSettings(pszRoot, fRecursive);

	StrApp strSubKey(pszRoot);
	// Use GUID instead of HVO for registry key to ensure uniqueness.
	GUID guid;
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	// Can't Assert directly or it fails in release build.
	bool f;
	f = pdbi->GetGuidFromId(m_psslId, guid);
	Assert(f);
	strSubKey.FormatAppend(_T("\\%g"), &guid);

	FwSettings * pfs = AfApp::GetSettings();
	DWORD dwT;
	Rect rc;
	StrApp str;

	if (pfs->GetDword(strSubKey.Chars(), _T("Settings"), &dwT))
	{
		m_iCurTabSel = dwT & kmaskCurTabSel ? 1 : 0;
		m_fShowHelp = dwT & kmaskShowHelp;
		m_fShowCurrent = dwT & kmaskShowCurrent;
	}

	// Load the saved choice list history.
	HWND hwndCombo = m_rghwndHistCombo[kiChoiceList];
	if (pfs->GetString(strSubKey.Chars(), _T("Choices History"), str))
	{
		int ichMin = 0;
		int ichLim;
		while ((ichLim = str.FindCh('\n', ichMin)) > 0)
		{
			StrApp strT = str.Mid(ichMin, ichLim - ichMin);
			if (strT.Length())
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)strT.Chars());
			ichMin = ichLim + 1;
		}
	}

	// Load the saved keyword search history.
	hwndCombo = m_rghwndHistCombo[kiKeywordSearch];
	if (pfs->GetString(strSubKey.Chars(), _T("Keyword History"), str))
	{
		int ichMin = 0;
		int ichLim;
		while ((ichLim = str.FindCh('\n', ichMin)) > 0)
		{
			StrApp strT = str.Mid(ichMin, ichLim - ichMin);
			if (strT.Length())
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)strT.Chars());
			ichMin = ichLim + 1;
		}
	}

	if (pfs->GetDword(strSubKey.Chars(), _T("Help Width"), &dwT))
	{
		m_dxpHelp = dwT;
	}
	else
	{
		GetClientRect(rc);
		m_dxpHelp = rc.Width();
	}

	LoadWindowPosition(strSubKey.Chars(), _T("Size"));
}


/*----------------------------------------------------------------------------------------------
	Save user interface settings.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::SaveSettings(const achar * pszRoot, bool fRecursive)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::SaveSettings:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	Assert(pszRoot);
	Assert(m_hwnd); // The dialog must still exist when this is called.

	SuperClass::SaveSettings(pszRoot, fRecursive);

	StrApp strSubKey(pszRoot);
	// Use GUID instead of HVO for registry key to ensure uniqueness.
	GUID guid;
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	// Can't Assert directly or it fails in release build.
	bool f;
	f = pdbi->GetGuidFromId(m_psslId, guid);
	Assert(f);
	strSubKey.FormatAppend(_T("\\%g"), &guid);

	FwSettings * pfs = AfApp::GetSettings();
	Assert(m_iCurTabSel < 2); // If this ever changes, we need more bits in mask.
	DWORD dwT = 0;
	if (m_iCurTabSel)
		dwT |= kmaskCurTabSel;
	if (m_fShowHelp)
		dwT |= kmaskShowHelp;
	if (m_fShowCurrent)
		dwT |= kmaskShowCurrent;
	pfs->SetDword(strSubKey.Chars(), _T("Settings"), dwT);

	// Store the saved choice list history.
	StrApp str;
	achar rgch[MAX_PATH];
	HWND hwndCombo = m_rghwndHistCombo[kiChoiceList];
	int citems = ::SendMessage(hwndCombo, CB_GETCOUNT, 0, 0);
	for (int iitem = 0; iitem < citems; iitem++)
	{
		if (::SendMessage(hwndCombo, CB_GETLBTEXTLEN, 0, 0) < MAX_PATH)
		{
			::SendMessage(hwndCombo, CB_GETLBTEXT, iitem, (LPARAM)rgch);
			str.FormatAppend(_T("%s\n"), rgch);
		}
	}
	pfs->SetString(strSubKey.Chars(), _T("Choices History"), str);

	// Store the saved keyword search history.
	hwndCombo = m_rghwndHistCombo[kiKeywordSearch];
	str.Clear();
	citems = ::SendMessage(hwndCombo, CB_GETCOUNT, 0, 0);
	for (int iitem = 0; iitem < citems; iitem++)
	{
		if (::SendMessage(hwndCombo, CB_GETLBTEXTLEN, 0, 0) < MAX_PATH)
		{
			::SendMessage(hwndCombo, CB_GETLBTEXT, iitem, (LPARAM)rgch);
			str.FormatAppend(_T("%s\n"), rgch);
		}
	}
	pfs->SetString(strSubKey.Chars(), _T("Keyword History"), str);

	pfs->SetDword(strSubKey.Chars(), _T("Help Width"), m_dxpHelp);

	SaveWindowPosition(strSubKey.Chars(), _T("Size"));
}


/*----------------------------------------------------------------------------------------------
	The user chose to display the tree items in a different way.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::CmdChangeDisplay(Cmd * pcmd)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::CmdChangeDisplay:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	AssertPtr(pcmd);

	PossNameType pnt = m_pnt;
	if (pcmd->m_cid == kcidPossDispName)
		pnt = kpntName;
	else if (pcmd->m_cid == kcidPossDispNameAbbrev)
		pnt = kpntNameAndAbbrev;
	else if (pcmd->m_cid == kcidPossDispAbbrev)
		pnt = kpntAbbreviation;

	if (pnt != m_pnt)
	{
		//ChangeDisplayOption(pnt);
		m_qpli->SetDisplayOption(pnt); // This will update everything.
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Show a check mark next to the way the tree items are currently being shown.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::CmsChangeDisplay(CmdState & cms)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::CmsChangeDisplay:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	if (cms.Cid() == kcidPossDispName)
		cms.SetCheck(m_pnt == kpntName);
	else if (cms.Cid() == kcidPossDispNameAbbrev)
		cms.SetCheck(m_pnt == kpntNameAndAbbrev);
	else if (cms.Cid() == kcidPossDispAbbrev)
		cms.SetCheck(m_pnt == kpntAbbreviation);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Disable the menu item if there isn't a currently selected item.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::CmsRequireItem(CmdState & cms)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::CmsRequireItem:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	cms.Enable(m_ipssContext != -1);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Disable the menu item if there isn't a currently selected item.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::CmsNotRequireItem(CmdState & cms)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::CmsNotRequireItem:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	cms.Enable(true);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Open up the List Editor on the current list and scroll down to the selected item.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::CmdModify(Cmd * pcmd)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::CmdModify:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	IFwToolPtr qft;
	try
	{
		MSG message;
		if (::PeekMessage(&message, NULL, WM_PAINT, WM_PAINT, PM_REMOVE))
			::DispatchMessage(&message);
		WaitCursor wc;
		CLSID clsid;
		StrUni stu(kpszCleProgId);
		CheckHr(::CLSIDFromProgID(stu.Chars(), &clsid));
		// See if already running.
		IRunningObjectTablePtr qrot;
		CheckHr(::GetRunningObjectTable(0, &qrot));
		IMonikerPtr qmnk;
		CheckHr(::CreateClassMoniker(clsid, &qmnk));
		IUnknownPtr qunk;
		if (SUCCEEDED(qrot->GetObject(qmnk, &qunk)))
			qunk->QueryInterface(IID_IFwTool, (void **)&qft);
		// If not start it up.
		if (!qft)
			qft.CreateInstance(clsid);
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
		AssertPtr(prmw);
		AfLpInfo * plpi = prmw->GetLpInfo();
		AssertPtr(plpi);
		AfDbInfo * pdbi = plpi->GetDbInfo();
		AssertPtr(pdbi);

		// Always save the database prior to opening the list editor to avoid locks. Locks can
		// happen even when the user doesn't intentionally modify the database (e.g., UserViews
		// are written the first time a ListEditor is opened on a newly created database.)
		prmw->SaveData();

		long htool;
		int nPid;
		Vector<HVO> vhvo;
		// We don't want to pass in a flid array, but if we try to pass in a null
		// vector, the marshalling process complains. So we need to use this kludge
		// to get it to work.
		int flidKludge;
		int nView = -1; // Default to data entry.

		if (m_ipssContext > -1)
				vhvo.Push(m_qpli->GetPssFromIndex(m_ipssContext)->GetPssId());
		else
			vhvo.Push(-1);

		CheckHr(qft->NewMainWndWithSel((wchar *)pdbi->ServerName(), (wchar *)pdbi->DbName(),
			plpi->GetLpId(), m_qpli->GetPsslId(), m_qpli->GetWs(), 0, 0,
			vhvo.Begin(), vhvo.Size(), &flidKludge, 0, 0, nView,
			&nPid,
			&htool)); // value you can pass to CloseMainWnd if you want.
		// Note that on Windows 2000, the list editor CANNOT do this for itself. If it isn't
		// already open, it comes to the front automatically, but if it is already running,
		// NOTHING it can do will fix things except for the current foreground application
		// to do this.
		::SetForegroundWindow((HWND)htool);
	}
	catch (...)
	{
		StrApp str(kstidCannotLaunchListEditor);
		::MessageBox(m_hwnd, str.Chars(), NULL, MB_OK | MB_ICONSTOP);
		return true;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Changes the Name, Abbr, or Description of the selected item.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::CmdRename(Cmd * pcmd)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::CmdRename:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	AfMainWnd * pafw = MainWindow();
	Assert(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	StrUni stuQuery;
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	plpi->GetDbInfo()->GetDbAccess(&qode);
	AssertPtr(qode);
	CheckHr(qode->CreateCommand(&qodc));
	PossChsrInsPtr qpci;
	qpci.Create();

	HWND hwndTree = m_rghwndTree[kiChoiceList];
	FW_TVITEM tvi;
	tvi.mask = TVIF_PARAM;
	tvi.hItem = TreeView_GetSelection(hwndTree);
	if (!TreeView_GetItem(hwndTree, &tvi))
		return true;
	FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
	AssertPtr(pfti);
	int ipss = pfti->lParam;
	PossItemInfo * ppii = m_qpli->GetPssFromIndex(ipss);
	AssertPtr(ppii);
	HVO hvoItem = ppii->GetPssId();

	StrUni stuName;
	StrUni stuAbbr;
	StrUni stuDesc("");
	StrUni stuNameOrig;
	StrUni stuAbbrOrig;
	StrUni stuDescOrig;
	bool bNameChanged = false;	// Set if Name or Abbreviation is changed in the dialog.

	ppii->GetName(stuName, kpntName);
	ppii->GetName(stuAbbr, kpntAbbreviation);

	// Ehance: Since we are storing this is a string, it is capable of having formatting
	// and embedded encodings. To support that here and in the UI we need to use TsStrings and
	// use a TssEditBox for the description in the dialog, plus a means for setting
	// formatting in the dialog.
	stuQuery.Format(L"select txt from MultiBigStr$ where obj = %d "
		L"and flid = %d and ws = %d", hvoItem, kflidCmPossibility_Description, ppii->GetWs());
	CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	ComBool fIsNull;
	CheckHr(qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		wchar rgchText[8200];
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(rgchText),
			isizeof(rgchText), &cbSpaceTaken, &fIsNull, 2));
		stuDesc = rgchText;
		if (isizeof(rgchText) < cbSpaceTaken)
		{
			// Text is too big for buffer, so load it into a bigger buffer.
			wchar * prgch = NewObj wchar[(cbSpaceTaken)/isizeof(wchar)];
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(prgch),
				cbSpaceTaken, &cbSpaceTaken, &fIsNull, 2));
			stuDesc = prgch;
			delete[] prgch;
		}
	}

	stuNameOrig = stuName;
	stuAbbrOrig = stuAbbr;
	stuDescOrig = stuDesc;	// Save original values for comparison.
	qpci->SetDialogValues(m_psslId, ppii->GetWs(), ipss, false, stuName.Chars(),
		stuAbbr.Chars(), stuDesc.Chars());
	qpci->SetHelpUrl(_T("Beginning_Tasks/Referencing_Topics_Lists/Chooser_Modify_options.htm"));
	if (qpci->DoModal(m_hwnd) != kctidOk)
		return true;
	qpci->GetDialogValues(stuName, stuAbbr, stuDesc);
	StrUtil::NormalizeStrUni(stuName, UNORM_NFD);
	StrUtil::NormalizeStrUni(stuAbbr, UNORM_NFD);
	StrUtil::NormalizeStrUni(stuDesc, UNORM_NFD);

	// Update the db

	if (stuName != stuNameOrig)
	{
		bNameChanged = true;
		stuQuery.Format(L"exec SetMultiTxt$ %d, %d, %d, ?",
			kflidCmPossibility_Name, hvoItem, ppii->GetWs());
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)stuName.Chars(), stuName.Length() * sizeof(OLECHAR)));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
	}

	if (stuAbbr != stuAbbrOrig)
	{
		bNameChanged = true;
		stuQuery.Format(L"exec SetMultiTxt$ %d, %d, %d, ?",
			kflidCmPossibility_Abbreviation, hvoItem, ppii->GetWs());
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)stuAbbr.Chars(), stuAbbr.Length() * sizeof(OLECHAR)));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
	}

	if (stuDesc != stuDescOrig)
	{
		stuQuery.Format(L"exec SetMultiBigStr$ %d, %d, %d, ?, ?",
			kflidCmPossibility_Description, hvoItem, ppii->GetWs());
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)stuDesc.Chars(), stuDesc.Length() * sizeof(OLECHAR)));
		ITsStringPtr qtss;
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		CheckHr(qtsf->MakeString(stuDesc.Bstr(), plpi->ActualWs(ppii->GetWs()), &qtss));

		const int kcbFmtBufMax = 1024;
		int cbFmtBufSize = kcbFmtBufMax;
		int cbFmtSpaceTaken;
		HRESULT hr;
		byte * rgbFmt = NewObj byte[kcbFmtBufMax];

		hr = qtss->SerializeFmtRgb(rgbFmt, cbFmtBufSize, &cbFmtSpaceTaken);
		if (hr != S_OK)
		{
			if (hr == S_FALSE)
			{
				//  If the supplied buffer is too small, try it again with
				//  the value that cbFmtSpaceTaken was set to.  If this
				//   fails, throw error.
				delete[] rgbFmt;
				rgbFmt = NewObj byte[cbFmtSpaceTaken];
				cbFmtBufSize = cbFmtSpaceTaken;
				CheckHr(qtss->SerializeFmtRgb(rgbFmt, cbFmtBufSize, &cbFmtSpaceTaken));
			}
			else
			{
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
		}

		CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
			reinterpret_cast<ULONG *>(rgbFmt), cbFmtSpaceTaken));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
		delete[] rgbFmt;
	}

	// Update all interested parties of change. Note this invalidates ppii.
	if (bNameChanged)
	{
		SyncInfo sync(ksyncPossList, m_psslId, 0);
		plpi->StoreAndSync(sync);
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Merges the currently selected item into another item.  This Opens the merge dialog and
	allows the user to select an item to merge into.  It then does the merge.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::CmdMerge(Cmd * pcmd)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::CmdMerge:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	RecMainWnd * prmw;
	// Abort if the any editor can't be changed.  Because we don't currently have a way force a
	// close without an assert, and I want to keep the assert there to catch programming errors.
	// We could add a boolean argument to EndEdit to override the assert, but this hardly seems
	// worth it, especially considering above comments. So for now, we force users to produce
	// valid data prior to deleting a record. Also, since updating other windows may cause their
	// editors to close, we also need to check that they are legal.
	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	int cafw = vqafw.Size();
	int iafw;
	for (iafw = 0; iafw < cafw; iafw++)
	{
		prmw = dynamic_cast<RecMainWnd *>(vqafw[iafw].Ptr());
		AssertPtr(prmw);
		if (!prmw->IsOkToChange())
		{
			// Bring the bad window to the top.
			::SetForegroundWindow(prmw->Hwnd());
			return false;
		}
	}
	prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(prmw);
	AfMdiClientWndPtr qmdic = prmw->GetMdiClientWnd();
	AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(qmdic->GetCurChild());
	HWND hwndTree = m_rghwndTree[kiChoiceList];
	FW_TVITEM tvi;
	tvi.mask = TVIF_STATE | TVIF_PARAM;
	tvi.stateMask = TVIS_STATEIMAGEMASK;
	tvi.hItem = TreeView_GetSelection(hwndTree);
	if (!TreeView_GetItem(hwndTree, &tvi))
		return true;
	bool fchecked = false;
	if ((tvi.state >> 12) - 1)
		fchecked = true;

	FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
	AssertPtr(pfti);
	int ipss = pfti->lParam;
	PossItemInfo * ppii = m_qpli->GetPssFromIndex(ipss);
	AssertPtr(ppii);
	HVO hvoSrc = ppii->GetPssId();

	// Can't merge an item if its IsProtected flag is set.
	if (!prmw->IsPossibilityDeletable(hvoSrc, kstidCantMergeItem))
		return true;

	PossChsrMrgPtr qpcm;
	qpcm.Create();
	qpcm->SetDialogValues(m_qpli, hvoSrc);
	qpcm->SetHelpUrl(_T("Beginning_Tasks/Referencing_Topics_Lists/Merge_list_items.htm"));
	if (qpcm->DoModal(m_hwnd) != kctidOk)
		return true;
	HVO hvoDst = qpcm->GetSelHvo();

	// do a merge of two list items
	WaitCursor wc;
	m_qpli->MergeItem(hvoSrc, hvoDst);

	// Update all interested parties of change. Note this invalidates ppii.
	SyncInfo sync(ksyncMergePss, m_psslId, 0);
	prmw->GetLpInfo()->StoreAndSync(sync);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Add a new item to the Poss list.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::CmdInsert(Cmd * pcmd)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::CmdInsert:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	try
	{
		PossChsrInsPtr qpci;
		qpci.Create();
		StrUni stuName("");
		StrUni stuAbbr("");
		StrUni stuDesc("");

		qpci->SetDialogValues(m_psslId, m_ws, -1, true, stuName.Chars(), stuAbbr.Chars(),
			stuDesc.Chars());
		qpci->SetHelpUrl(
			_T("Beginning_Tasks/Referencing_Topics_Lists/Chooser_Modify_options.htm"));
		if (qpci->DoModal(m_hwnd) != kctidOk)
			return true;
		qpci->GetDialogValues(stuName, stuAbbr, stuDesc);
		if (!stuName.Length())
			stuName.Assign(stuAbbr);
		if (!stuAbbr.Length())
			stuAbbr.Assign(stuName);
		StrUtil::NormalizeStrUni(stuName, UNORM_NFD);
		StrUtil::NormalizeStrUni(stuAbbr, UNORM_NFD);
		StrUtil::NormalizeStrUni(stuDesc, UNORM_NFD);

		int ipssNew;
		HWND hwndTree = m_rghwndTree[kiChoiceList];
		FW_TVITEM tvi;
		tvi.mask = TVIF_PARAM;
		tvi.hItem = TreeView_GetSelection(hwndTree);

		if (tvi.hItem)
		{
			if (!TreeView_GetItem(hwndTree, &tvi))
				return true;
			FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
			AssertPtr(pfti);
			ipssNew = pfti->lParam;
			if (ipssNew == -1)
				ipssNew = m_qpli->GetCount() - 1;
		}
		else
			ipssNew = -1;

		bool fOK;
		switch (pcmd->m_cid)
		{
		case kcidPossTrInsert:
		case kcidPossTrInsertBef:
			fOK = m_qpli->InsertPss(ipssNew, stuAbbr.Chars(), stuName.Chars(), kpilBefore,
				&ipssNew);
			break;
		case kcidPossTrInsertAft:
			fOK = m_qpli->InsertPss(ipssNew, stuAbbr.Chars(), stuName.Chars(), kpilAfter,
				&ipssNew);
			break;
		case kcidPossTrInsertSub:
			fOK = m_qpli->InsertPss(ipssNew, stuAbbr.Chars(), stuName.Chars(), kpilUnder,
				&ipssNew);
			break;
		}

		if (!fOK)
		{
			StrApp str(kstidCannotInsertListItem);
			::MessageBox(m_hwnd, str.Chars(), NULL, MB_OK | MB_ICONSTOP);
			return true;
		}

		//XXX MY NEW CODE XXX
		OnSelChanged(ipssNew);

		HVO hvoNew = m_qpli->GetPssFromIndex(ipssNew)->GetPssId();
		StrUni stuQuery;
		IOleDbEncapPtr qode;
		IOleDbCommandPtr qodc;
		AfMainWnd * pafw = MainWindow();
		Assert(pafw);
		AfLpInfo * plpi = pafw->GetLpInfo();
		AssertPtr(plpi);
		plpi->GetDbInfo()->GetDbAccess(&qode);
		AssertPtr(qode);
		CheckHr(qode->CreateCommand(&qodc));
		ComBool fMoreRows;
		ComBool fIsNull;

		// Update the db
		stuQuery.Format(L"exec SetMultiBigStr$ %d, %d, %d, ?, ?",
			kflidCmPossibility_Description, hvoNew, plpi->ActualWs(m_ws));
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)stuDesc.Chars(), stuDesc.Length() * sizeof(OLECHAR)));
		ITsStringPtr qtss;
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		CheckHr(qtsf->MakeString(stuDesc.Bstr(), plpi->ActualWs(m_ws), &qtss));

		const int kcbFmtBufMax = 1024;
		int cbFmtBufSize = kcbFmtBufMax;
		int cbFmtSpaceTaken;
		HRESULT hr;
		byte * rgbFmt = NewObj byte[kcbFmtBufMax];

		hr = qtss->SerializeFmtRgb(rgbFmt, cbFmtBufSize, &cbFmtSpaceTaken);
		if (hr != S_OK)
		{
			if (hr == S_FALSE)
			{
				//  If the supplied buffer is too small, try it again with
				//  the value that cbFmtSpaceTaken was set to.  If this
				//   fails, throw error.
				delete[] rgbFmt;
				rgbFmt = NewObj byte[cbFmtSpaceTaken];
				cbFmtBufSize = cbFmtSpaceTaken;
				CheckHr(qtss->SerializeFmtRgb(rgbFmt, cbFmtBufSize, &cbFmtSpaceTaken));
			}
			else
			{
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
		}

		CheckHr(qodc->SetParameter(2,
				DBPARAMFLAGS_ISINPUT,
				NULL,
				DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(rgbFmt),
				cbFmtSpaceTaken));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
		delete[] rgbFmt;

		// Update all interested parties of change.
		m_hvoTarget = hvoNew; // Select the new item.
		SyncInfo sync(ksyncAddPss, m_psslId, hvoNew);
		plpi->StoreAndSync(sync);
	}
	catch (...)
	{
		StrApp str(kstidCannotLaunchListEditor);
		::MessageBox(m_hwnd, str.Chars(), NULL, MB_OK | MB_ICONSTOP);
		return true;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Remove the selected item from the possibility list.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::CmdDelete(Cmd * pcmd)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::CmdDelete:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	if (!m_qpli->GetCount())
		return true;
	MSG message;
	if (::PeekMessage(&message, NULL, WM_PAINT, WM_PAINT, PM_REMOVE))
		::DispatchMessage(&message);
	WaitCursor wc;

	RecMainWndPtr qrmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(qrmw);
	AfMdiClientWndPtr qmdic = qrmw->GetMdiClientWnd();
	AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(qmdic->GetCurChild());
	AfLpInfoPtr qlpi = qrmw->GetLpInfo();
	AssertPtr(qlpi);
	CustViewDaPtr qcvd = qrmw->MainDa();

	int clid = m_qpli->GetItemClsid();
	HVO hvo = m_qpli->GetPssFromIndex(m_ipssContext)->GetPssId();

	// Can't delete an item if its IsProtected flag is set.
	if (!qrmw->IsPossibilityDeletable(hvo, kstidCantDeleteItem))
		return true;

	StrUni stuName;
	m_qpli->GetPssFromIndex(m_ipssContext)->GetName(stuName, kpntNameAndAbbrev);
	StrApp strType;
	StrApp strSub;
	int flid;
	CheckHr(qcvd->get_ObjOwnFlid(hvo, &flid));
	strType.Load(flid == kflidCmPossibility_SubPossibilities ? kstidListSubitem : kstidListItem);
	strSub.Load(kstidListSubitems);
	StrUni stu;
	stu.Load(flid == kflidCmPossibility_SubPossibilities ? kstidListSubitem : kstidListItem);
	stu.Append(L"  ' ");
	stu.Append(stuName.Chars());
	stu.Append(L" '");
	ITsStringPtr qtss;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	CheckHr(qtsf->MakeString(stu.Bstr(), qlpi->ActualWs(m_ws), &qtss));

	// put up the delete object dialog
	DeleteObjDlgPtr qdo;
	qdo.Create();
	qdo->SetDialogValues(hvo, qtss, strType, strSub, clid, qlpi->GetDbInfo());
	qdo->SetHelpUrl(_T("Beginning_Tasks/Referencing_Topics_Lists/Chooser_Modify_options.htm"));
	wc.RestoreCursor();
	if (kctidOk != qdo->DoModal(m_hwnd))
		return true;

	WaitCursor wc2;

	// Delete the selected item.
	if (!m_qpli->DeletePss(hvo))
	{
		StrApp str(kstidCannotDeleteListItem);
		::MessageBox(m_hwnd, str.Chars(), NULL, MB_OK | MB_ICONSTOP);
		return true;
	}
	// Update all interested parties of change.
	/*SyncInfo sync(ksyncDelPss, m_psslId, hvo);
	qlpi->StoreAndSync(sync);*/


	FW_TVITEM tvi;
	tvi.mask = TVIF_PARAM;
	tvi.hItem = TreeView_GetSelection(m_rghwndTree[m_iCurTabSel]);
	//try to get selected item in the current tab and load it into tvi
	//if we succeed call OnSelectChange with the selected value
	if (TreeView_GetItem(m_rghwndTree[m_iCurTabSel], &tvi))
	{
		FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
		AssertPtr(pfti);
		OnSelChanged(pfti->lParam);
	//if we don't succeed, call OnSelectChange with the index to the first item (i.e. 0)
	}
	else
	{
		OnSelChanged(0);
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Add or remove the selected tag (and possibly its child tags as wel).
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::CmdModifyTag(Cmd * pcmd)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::CmdModifyTag:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	AssertPtr(pcmd);
	bool fInsert;
	if (pcmd->m_cid == kcidPossExcludeTag)
	{
		// If the possibility is currently in the overlay, remove it.
		// Otherwise we want to insert it into the overlay.
		HWND hwndTree = m_rghwndTree[m_iCurTabSel];
		fInsert = TreeView_GetCheckState(hwndTree, TreeView_GetSelection(hwndTree)) == 0;
	}
	else
	{
		fInsert = (pcmd->m_cid == kcidPossIncludeSubtags);
	}
	bool fRecursive = pcmd->m_cid != kcidPossExcludeTag;

	HWND hwndTree = m_rghwndTree[kiChoiceList];
	HTREEITEM htiStart;
	if (m_iCurTabSel == kiChoiceList)
		htiStart = TreeView_GetSelection(hwndTree);
	else
		htiStart = FindPss(hwndTree, TreeView_GetRoot(hwndTree), m_ipssContext);

	Vector<int> vipssChanged;
	CheckItem(htiStart, vipssChanged, fInsert, fRecursive);

	if (vipssChanged.Size() > 0)
	{
		// If any of the modified possibility items are in the Keyword Search tree,
		// update their check state as well.
		hwndTree = m_rghwndTree[kiKeywordSearch];
		FW_TVITEM tvi;
		tvi.mask = TVIF_HANDLE | TVIF_PARAM;
		tvi.hItem = TreeView_GetRoot(hwndTree);
		HTREEITEM & hti = tvi.hItem;
		while (hti)
		{
			if (TreeView_GetItem(hwndTree, &tvi))
			{
				FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
				AssertPtr(pfti);
				// Look through all the items that changed to see if there's a match.
				for (int ipss = 0; ipss < vipssChanged.Size(); ipss++)
				{
					if (vipssChanged[ipss] == pfti->lParam)
					{
						// We found a match, so update the check state of the tree item.
						TreeView_SetCheckState(hwndTree, hti, fInsert);
						// Remove it from the vector so we don't waste time looking for
						// this possibility index again.
						vipssChanged.Delete(ipss);
						break;
					}
				}
			}
			hti = TreeView_GetNextSibling(hwndTree, hti);
		}
		m_qpcc->Redraw();
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Update the Include/Exclude commands on the popup menu.
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::CmsModifyTag(CmdState & cms)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::CmsModifyTag:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	cms.Enable(m_ipssContext != -1);
	if (cms.Cid() == kcidPossExcludeTag)
	{
		HWND hwndTree = m_rghwndTree[m_iCurTabSel];
		cms.SetCheck(TreeView_GetCheckState(hwndTree, TreeView_GetSelection(hwndTree)) == 0);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	If fInsert is true, add the possibility to the overlay (if it doesn't already exist).
	If fInsert is false, remove the possibility from the overlay (if it already exists).
	If fRecursive is true, all subtags of the possibility will also be added or removed.
	When this method returns, vipssChanged will contain the index of all the possibility
	items that have been modified.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::CheckItem(HTREEITEM hti, Vector<int> & vipssChanged, bool fInsert,
	bool fRecursive)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::CheckItem:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	if (!hti)
		return;

	HWND hwndTree = m_rghwndTree[kiChoiceList];
	FW_TVITEM tvi;
	tvi.mask = TVIF_HANDLE | TVIF_PARAM;
	tvi.hItem = hti;
	if (TreeView_GetItem(hwndTree, &tvi))
	{
		TreeView_SetCheckState(hwndTree, hti, fInsert);

		FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
		AssertPtr(pfti);
		vipssChanged.Push(pfti->lParam);
	}

	if (fRecursive)
	{
		hti = TreeView_GetChild(hwndTree, hti);
		while (hti)
		{
			CheckItem(hti, vipssChanged, fInsert, fRecursive);
			hti = TreeView_GetNextSibling(hwndTree, hti);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Something has changed in the possibility list.
----------------------------------------------------------------------------------------------*/
void PossChsrDlg::ListChanged(int nAction, HVO hvoPssl, HVO hvoSrc, HVO hvoDst, int ipssSrc,
	int ipssDst)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::ListChanged:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	switch (nAction)
	{
	case kplnaInsert:
	case kplnaDelete:
	case kplnaModify:
	case kplnaReload:
	case kplnaMerged:
	case kplnaDisplayOption:
		{
			// Save the current selection in the tree.
			HWND hwndTree = m_rghwndTree[kiChoiceList];
			FW_TVITEM tvi;
			tvi.mask = TVIF_HANDLE | TVIF_PARAM;
			tvi.hItem = TreeView_GetSelection(hwndTree);
			HVO hvoSel = 0;
			if (TreeView_GetItem(hwndTree, &tvi))
			{
				// If we deleted the last item (i.e. the one at the end) and it was selected,
				// we need to get back to a real item.
				FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
				AssertPtr(pfti);
				int ihvo = pfti->lParam;
				if (ihvo >= m_qpli->GetCount())
					ihvo = m_qpli->GetCount() - 1;
				if (ihvo < 0 && (m_iCurTabSel == kiChoiceList))
				{
					// The list is now empty.
					StrApp str;
					::SetWindowText(m_rghwndHistCombo[kiChoiceList], str.Chars());
				}
				else
				{
					PossItemInfo * ppii = m_qpli->GetPssFromIndex(ihvo);
					AssertPtr(ppii);
					if (m_hvoTarget)
					{
						hvoSel = m_hvoTarget;
						m_hvoTarget = 0;
					}
					else
						hvoSel = ppii->GetPssId();
					if (m_iCurTabSel == kiChoiceList)
					{
						StrUni stu;
						ppii->GetName(stu, m_pnt);
						StrApp str(stu);
						::SetWindowText(m_rghwndHistCombo[kiChoiceList], str.Chars());
					}
				}
			}

			// Repopulate tree with current items.
			::SendMessage(hwndTree, WM_SETREDRAW, false, 0); // AddPossibilities restores.

			// The DeleteAllItems causes a bunch of calls to OnSelChanged, so we want to
			// ignore any sel changes until all the items have been deleted.
			// We want to wait until the possibilities have been added back in before
			// resetting the flag so that the HTML pane isn't changed at all.
			m_fIgnoreSelChange = true;
			TreeView_DeleteAllItems(hwndTree);
			// Reset in case changes were made.
			m_pnt = m_qpli->GetDisplayOption();
			m_ws = m_qpli->GetWs();
			bool fOk = AddPossibilities(hvoSel);
			m_fIgnoreSelChange = false;
			if (!fOk)
				return;

			// And make the list of selected items in the other window match.
			m_qpcc->Redraw();
			// Enhance JohnT: possibly also update the search results in the other window,
			// if it is visible?
			if (m_iCurTabSel == kiKeywordSearch)
			{
				achar rgchT[MAX_PATH];
				HWND hwndCombo = m_rghwndHistCombo[m_iCurTabSel];
				::SendMessage(hwndCombo, WM_GETTEXT, MAX_PATH, (LPARAM)rgchT);
				if (rgchT[0])
					AddKeywords(rgchT);
			}
			// If we're not showing the help page, we're done.
			if (m_fShowHelp)
			{
				// Update the help window.
				m_qpwe->UpdateHelp(m_qpli, hvoSel);
			}
		}
		break;
	default:
		break;
	}
}


/*----------------------------------------------------------------------------------------------
	Run the Refresh command.
	(The pcmd argument is unused and therefore has a default, but it is present because this
	method much match the pattern for command implementations.)
----------------------------------------------------------------------------------------------*/
bool PossChsrDlg::CmdViewRefresh(Cmd * pcmd)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrDlg::CmdViewRefresh:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	WaitCursor wc;
	// Don't test the results here. Under some circumstances CleDeFeString::IsOkToClose might
	// return false, because the cache wasn't loaded yet when it was checking things out.
	// We don't want to return false or we get make extra unnecessary calls when multiple
	// windows are open.
	if (!AfApp::Papp())
	{
		// For now give up. Maybe later we will have our own member variable and fall back on
		// that? This only happens when used as an ActiveX control. Or maybe we will implement
		// a COM interface for the ActiveX control to still access an Application interface?
		return true;
	}

	AfDbApp * papp = dynamic_cast<AfDbApp *>(AfApp::Papp());
	if (papp)
	{
		RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(papp->GetCurMainWnd());
		if (pwnd && !papp->FullRefresh(pwnd->GetLpInfo()))
			return false;
	}
	else
		return false;
	return true; // Processed command.
}


/***********************************************************************************************
	PossChsrTree methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool PossChsrTree::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrTree::FWndProc:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	// Don't erase the background (to reduce flicker).
	if (wm == WM_ERASEBKGND)
		return true;

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Draw everything to memory so it doesn't flicker.
----------------------------------------------------------------------------------------------*/
bool PossChsrTree::OnPaint(HDC hdcDef)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrTree::OnPaint:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	Assert(!hdcDef);

	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps);
	Assert(hdc);
	Rect rc = ps.rcPaint;

	// Create the virtual screen in memory.
	HDC hdcMem = AfGdi::CreateCompatibleDC(hdc);
	Assert(hdcMem);
	HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, rc.Width(), rc.Height());
	Assert(hbmp);
	HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
	::SetViewportOrgEx(hdcMem, -rc.left, -rc.top, NULL);
	AfGfx::FillSolidRect(hdcMem, rc, ::GetSysColor(COLOR_3DFACE));

// TODO:  TimP added Wednesday, December 4, 2002
//	HFONT hfontOld = (HFONT)::GetCurrentObject(hdcMem, OBJ_FONT); // code below is selecting a
																// non-default font

	// Draw the tree view in memory.
	DefWndProc(WM_PAINT, (WPARAM)hdcMem, 0);

	if (m_pplddDragDrop)
		m_pplddDragDrop->Paint(hdcMem);

	// Copy image to the screen.
	::BitBlt(hdc, rc.left, rc.top, rc.Width(), rc.Height(), hdcMem, rc.left, rc.top, SRCCOPY);

//	AfGdi::SelectObjectFont(hdcMem, hfontOld, AfGdi::CLUDGE_OLD); // code above is selecting a
																// non-default font
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


/***********************************************************************************************
	PossChsrTab methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool PossChsrTab::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrTab::FWndProc:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	// Don't erase the background (to reduce flicker).
	if (wm == WM_ERASEBKGND)
		return true;

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Draw everything to memory so it doesn't flicker.
----------------------------------------------------------------------------------------------*/
bool PossChsrTab::OnPaint(HDC hdcDef)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrTab::OnPaint:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
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
	DefWndProc(WM_PAINT, (WPARAM)hdcMem, 0); // This changes the font for hdcMem!

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
	fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
	Assert(fSuccess);

	fSuccess = AfGdi::DeleteDC(hdcMem);
	Assert(fSuccess);

	::EndPaint(m_hwnd, &ps);

	return true;
}


/***********************************************************************************************
	PossChsrComboEdit methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool PossChsrComboEdit::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
#ifdef DEBUG_POSSCHSRDLG
	StrAnsi staTemp = "PossChsrComboEdit::FWndProc:  IN.\n";
	::OutputDebugStringA(staTemp.Chars());
#endif
	if (wm == WM_KEYDOWN)
	{
		DWORD ichStart;
		DWORD ichStop;
		::SendMessage(m_hwnd, EM_GETSEL, (WPARAM)&ichStart, (LPARAM)&ichStop);
		s_fExtraBackspace = ichStart != ichStop && wp == VK_BACK;
	}
	return AfWnd::FWndProc(wm, wp, lp, lnRet);
}


/***********************************************************************************************
	PossWebEvent methods.
***********************************************************************************************/
static DummyFactory g_factpwe(_T("SIL.AppCore.PossWebEvent"));

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
PossWebEvent::PossWebEvent(PossWebEventNotify * ppwen, HWND hwndTool, AfLpInfo * plpi)
{
	AssertPtr(ppwen);
	Assert(hwndTool);
	AssertPtr(plpi);
	m_cref = 1;
	m_ppwen = ppwen;
	m_hwndTool = hwndTool;
	m_qlpi = plpi;
}


/*----------------------------------------------------------------------------------------------
	Standard IUnknown method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP PossWebEvent::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IDispatch)
		*ppv = static_cast<IDispatch *>(this);
	else
		return E_NOINTERFACE;

	AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Standard IUnknown method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) PossWebEvent::AddRef()
{
	return InterlockedIncrement(&m_cref);
}

/*----------------------------------------------------------------------------------------------
	Standard IUnknown method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) PossWebEvent::Release()
{
	long cref = InterlockedDecrement(&m_cref);
	if (cref == 0)
	{
		m_cref = 1;
		delete this;
	}
	return cref;
}


/*----------------------------------------------------------------------------------------------
	This method does not need to be implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP PossWebEvent::GetTypeInfoCount(UINT * pctinfo)
{
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	This method does not need to be implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP PossWebEvent::GetTypeInfo(UINT iTInfo, LCID lcid, ITypeInfo ** ppTInfo)
{
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	This method does not need to be implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP PossWebEvent::GetIDsOfNames(REFIID riid, LPOLESTR * rgszNames, UINT cNames,
	LCID lcid, DISPID * rgDispId)
{
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	This method gets called when events happen in the HTML control.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP PossWebEvent::Invoke(DISPID dispIdMember, REFIID riid, LCID lcid, WORD wFlags,
	DISPPARAMS * pDispParams, VARIANT * pVarResult, EXCEPINFO * pExcepInfo,
	UINT * puArgErr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pDispParams);
	ChkComArgPtrN(pVarResult);
	ChkComArgPtrN(pExcepInfo);
	ChkComArgPtrN(puArgErr);

	if (DISPID_BEFORENAVIGATE2 == dispIdMember)
	{
		// Whenever we open a new URL, update the current selection in the tree.
		const int kiUrl = 5;
		Assert(pDispParams->cArgs > kiUrl);
		Assert(pDispParams->rgvarg[kiUrl].vt == (VT_BYREF | VT_VARIANT));
		Assert(pDispParams->rgvarg[kiUrl].pvarVal->vt == VT_BSTR);
		AssertPtr(m_ppwen);
		OLECHAR * pszUrl = pDispParams->rgvarg[kiUrl].pvarVal->bstrVal;
		const int kiCancel = 0; // 5th (and last, which is why it's 0) parameter.

		if (_wcsnicmp(pszUrl, L"http://", 7) == 0 || FAILED(m_ppwen->UpdateTree(pszUrl)))
		{
			Assert(pDispParams->rgvarg[kiCancel].vt == (VT_BYREF | VT_BOOL));
			*pDispParams->rgvarg[kiCancel].pboolVal = VARIANT_TRUE;
		}
		return S_OK;
	}
	else if (DISPID_NAVIGATEERROR == dispIdMember)
	{
		// The requested HTML page was not found, so show default help information.
		const int kiCancel = 0; // 5th (and last, which is why it's 0) parameter.
		UpdateDefaultHelp(m_qpli, m_hvoPssId);

		Assert(pDispParams->rgvarg[kiCancel].vt == (VT_BYREF | VT_BOOL));
		*pDispParams->rgvarg[kiCancel].pboolVal = VARIANT_TRUE;
		return S_OK;
	}
	else if (DISPID_COMMANDSTATECHANGE == dispIdMember)
	{
		// Update the toolbar buttons.
		Assert(pDispParams->cArgs == 2);
		Assert(pDispParams->rgvarg[0].vt == VT_BOOL);
		Assert(pDispParams->rgvarg[1].vt == VT_I4);
		int cid = 0;
		int nT = pDispParams->rgvarg[1].intVal;
		if (nT == CSC_NAVIGATEFORWARD)
			cid = kcidPossForward;
		else if (nT == CSC_NAVIGATEBACK)
			cid = kcidPossBack;
		if (cid != 0)
			::SendMessage(m_hwndTool, TB_ENABLEBUTTON, cid, pDispParams->rgvarg[0].boolVal);
		return S_OK;
	}
	return E_NOTIMPL;
	END_COM_METHOD(g_factpwe, IID_IDocHostUIHandlerDispatch);
}

/*----------------------------------------------------------------------------------------------
	Show help for the given possibility in the webbrowser.
----------------------------------------------------------------------------------------------*/
void PossWebEvent::UpdateHelp(PossListInfo * ppli, HVO hvoPssId)
{
	AssertPtr(m_qlpi);
	AssertPtr(ppli);

	// Store these for later to generate default help information if the
	// requested HTML page is not found in the help file.
	m_qpli = ppli;
	m_hvoPssId = hvoPssId;

	const OLECHAR * pwszHelpFile = ppli->GetHelpFile();
	AssertPszN(pwszHelpFile);

	// Try to find the URL to open.
	StrUni stuUrl;
	if (StrLen(pwszHelpFile) > 0)
	{
		wchar rgchUrl[MAX_PATH];
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
			AssertPtr(qode);

			CheckHr(qode->CreateCommand(&qodc));

			stuQuery.Format(L"select helpid from CmPossibility where id = %d",
				hvoPssId);
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (fMoreRows)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(rgchUrl),
					isizeof(rgchUrl), &cbSpaceTaken, &fIsNull, 2));
				// Compose the URL if we got a valid helpid.
				if (cbSpaceTaken)
				{
					// If the Help File contains an absolute path, then we don't want to alter
					// that. However, if it contains a relative path, or no path, then we want
					// to prepend that with the FieldWorks Help path:
					StrUni stuColon = L":";
					StrUni stuBackslash = L"\\";
					StrUni stuHelpFile(pwszHelpFile);
					bool fFullPath = false;
					if (stuHelpFile[0] == stuBackslash[0])
						fFullPath = true;
					else if (stuHelpFile[1] == stuColon[0])
						fFullPath = true;
					if (fFullPath)
						stuUrl.Format(L"its:%s::/%s.htm", pwszHelpFile, rgchUrl);
					else
					{
						// Stick the FieldWorks Help path in:
						StrUni stuPath(AfApp::Papp()->GetFwCodePath().Chars());
						stuUrl.Format(L"its:%s\\Helps\\%s::/%s.htm", stuPath.Chars(),
							pwszHelpFile, rgchUrl);
					}
				}
			}
		}
		catch (...)
		{
			// Do nothing here so we drop down to the default help.
		}
	}

	// If we couldn't get a URL, create a default help HTML page.
	if (stuUrl.Length() == 0)
		UpdateDefaultHelp(ppli, hvoPssId);
	else
		CheckHr(m_qweb2->Navigate(stuUrl.Bstr(), NULL, NULL, NULL, NULL));
}


/*----------------------------------------------------------------------------------------------
	If pssId is -1, return an error string.
----------------------------------------------------------------------------------------------*/
void PossWebEvent::UpdateDefaultHelp(PossListInfo * ppli, HVO hvoPssId)
{
	AssertPtr(m_qlpi);
	AssertPtr(ppli);

	ILgWritingSystemFactoryPtr qwsf;
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);
	IWritingSystemPtr qws;

	StrUni stuTitle(kstidTitle);
	StrUni stuDescription(kstidNoDesc);
	StrUni stuHeader(kstidShortDescription);
	int ws = pdbi->UserWs();
	StrUni stuTitleFont;
	StrUni stuHeaderFont;
	StrUni stuDescFont;

	CheckHr(qwsf->get_EngineOrNull(ws, &qws));
	AssertPtr(qws);
	SmartBstr sbstr;
	CheckHr(qws->get_DefaultSerif(&sbstr));
	if (sbstr)
		stuHeaderFont.Assign(sbstr.Chars());

	StrUni stuHTML;
	// hvoPssId can be 0 after deleting the last item. We'll treat that the same as
	// -1 as an error and simply display a page with a blank title and description.
	if (hvoPssId > 0)
	{
		int ipss = ppli->GetIndexFromId(hvoPssId, &ppli);
		PossItemInfo * ppii = ppli->GetPssFromIndex(ipss);
		AssertPtr(ppii);
		ppii->GetName(stuTitle, kpntNameAndAbbrev);
		int ws = ppii->GetWs();
		CheckHr(qwsf->get_EngineOrNull(ws, &qws));
		AssertPtr(qws);
		SmartBstr sbstr;
		CheckHr(qws->get_DefaultSerif(&sbstr));
		if (sbstr)
			stuTitleFont.Assign(sbstr.Chars());
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;
		IOleDbEncapPtr qode;
		IOleDbCommandPtr qodc;

		//  Obtain pointer to IOleDbEncap interface.
		AfDbInfo * pdbi = m_qlpi->GetDbInfo();
		AssertPtr(pdbi);
		pdbi->GetDbAccess(&qode);
		AssertPtr(qode);

		try
		{
			StrUni stuQuery;
			stuQuery.Format(L"exec GetOrderedMultiTxt '%d', %d", hvoPssId,
				kflidCmPossibility_Description);

			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (fMoreRows)
			{
				wchar rgchText[8200];
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(rgchText),
					isizeof(rgchText), &cbSpaceTaken, &fIsNull, 2));
				CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&ws), isizeof(ws),
					&cbSpaceTaken, &fIsNull, 0));

				stuDescription = rgchText;
				if (stuDescription.Equals(L"***"))
					stuDescription.Load(kstidNoDesc);

				CheckHr(qwsf->get_EngineOrNull(ws, &qws));
				if (qws)
				{
					SmartBstr sbstr;
					CheckHr(qws->get_DefaultSerif(&sbstr));
					if (sbstr)
						stuDescFont.Assign(sbstr.Chars());
				}
			}
		}
		catch (...)	// Was empty.
		{
			throw;	// For now we have nothing to add, so pass it on up.
		}
	}

	//stuHTML.Format(L"<html><head><title>%s</title>", stuTitle.Chars());
	//stuHTML.FormatAppend(L"<link rel=stylesheet type=\"text/css\" href=\"linglink.css\" "
	//	L"title=\"original\"></head><body><h2>%s</h2>", stuTitle.Chars());
	//stuHTML.FormatAppend(L"<h3 class=SubmoduleHeading>%s</h3>", stuHeader.Chars());
	//stuHTML.FormatAppend(L"<div class=SubmoduleBody><p>%s</p></div>", stuDescription.Chars());

	stuHTML.Format(L"<html>%n"
		L"<head>%n"
		L"<title>%s</title>%n"
		L"<link rel=stylesheet type=\"text/css\" href=\"linglink.css\" title=\"original\">%n"
		L"</head>%n"
		L"<body>%n", stuTitle.Chars());
	if (stuTitleFont.Length())
		stuHTML.FormatAppend(L"<font face=\"%s\">", stuTitleFont.Chars());
	stuHTML.FormatAppend(L"<h2>%s</h2>", stuTitle.Chars());
	if (stuTitleFont.Length())
		stuHTML.FormatAppend(L"</font>");
	stuHTML.FormatAppend(L"%n");
	if (stuHeaderFont.Length())
		stuHTML.FormatAppend(L"<font face=\"%s\">", stuHeaderFont.Chars());
	stuHTML.FormatAppend(L"<h3 class=\"SubmoduleHeading\">%s</h3>", stuHeader.Chars());
	if (stuHeaderFont.Length())
		stuHTML.FormatAppend(L"</font>");
	stuHTML.FormatAppend(L"%n");
	if (stuDescFont.Length())
		stuHTML.FormatAppend(L"<font face=\"%s\">", stuDescFont.Chars());
	stuHTML.FormatAppend(L"<div class=\"SubmoduleBody\"><p>%s</p></div>",
		stuDescription.Chars());
	if (stuDescFont.Length())
		stuHTML.FormatAppend(L"</font>");
	stuHTML.FormatAppend(L"%n"
		L"</body>%n"
		L"</html>%n");

	// To display this help, we can no longer just pass the text as a URL, because a string
	// that is too long will be ignored. So instead, we will write a new HTML page:
	try
	{
		IDispatchPtr qdisp;
		// Get an interface to the current document:
		HRESULT hr;
		CheckHr(hr = m_qweb2->get_Document(&qdisp));
		if (!qdisp)
		{
			// REVIEW DarrellZ: Try to figure out why this can happen and see what we can
			// do to recover intelligently.
			return;
		}
		DEFINE_COM_PTR(IHTMLDocument2);
		IHTMLDocument2Ptr qdoc;
		CheckHr(hr = qdisp->QueryInterface(IID_IHTMLDocument2, (void **)&qdoc));
		// Clear the document of its contents.
		// (See ms-help://MS.VSCC/MS.MSDNVS/ProgIE/workshop/browser/mshtml/reference/IFaces/Document2/clear.htm)
		CheckHr(hr = qdoc->write(NULL));
		CheckHr(hr = qdoc->close());

		// See (ms-help://MS.VSCC/MS.MSDNVS/ProgIE/workshop/browser/mshtml/reference/IFaces/Document2/write.htm)
		// Set up the text we want to write in a SAFEARRAY:
		VARIANT * pvar;
		SAFEARRAY * psa;
		SmartBstr sbstr = stuHTML.Bstr();

		// Create a new one-dimensional array:
		psa = SafeArrayCreateVector(VT_VARIANT, 0, 1);
		if (psa != NULL)
		{
			CheckHr(hr = SafeArrayAccessData(psa, (void **)&pvar));
			pvar->vt = VT_BSTR;
			pvar->bstrVal = sbstr;
			CheckHr(hr = SafeArrayUnaccessData(psa));
			CheckHr(hr = qdoc->write(psa));
			SafeArrayDestroy(psa);
		}
	}
	catch (...)
	{
		// Do nothing.
	}
}

//:>********************************************************************************************
//:>PossChsrInsDlg methods. (Poss Chooser Insert and rename dialog)
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
PossChsrIns::PossChsrIns(void)
{
	m_rid = kridChsrInsertDlg;
	m_iMerge = -1;
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param fInsert If true then it is a Insert operation otherwise it is a Rename.
	@param pszName Initial Name of item
	@param pszAbbr Initial Abbr of item
	@param pszDesc Initial Description of item
----------------------------------------------------------------------------------------------*/
void PossChsrIns::SetDialogValues(HVO psslId, int ws, int ipss, bool fInsert,
		LPCOLESTR pszName, LPCOLESTR pszAbbr, LPCOLESTR pszDesc)
{
	if (fInsert)
		m_pszHelpUrl = _T("DialogInsertNewListItem.htm");
	else
		m_pszHelpUrl = _T("DialogRenameItem.htm");

	m_ipss = ipss;
	m_psslId = psslId;
	m_ws = ws;
	m_fInsert = fInsert;
	m_stuName = pszName;
	m_stuAbbr = pszAbbr;
	m_stuDesc = pszDesc;
}


/*----------------------------------------------------------------------------------------------
	Gets the values from the dialog.

	@param pvis Out Visibility of the field.
	@param stuSty Text style
----------------------------------------------------------------------------------------------*/
void PossChsrIns::GetDialogValues(StrUni & stuName, StrUni & stuAbbr, StrUni & stuDesc)
{
	stuName = m_stuName;
	stuAbbr = m_stuAbbr;
	stuDesc = m_stuDesc;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	Only WM_ACTIVATE is processed, and even then the message is
	passed on to the superclass's FWndProc method.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True or false: whatever the superclass's FWndProc method returns.
----------------------------------------------------------------------------------------------*/
bool PossChsrIns::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_ACTIVATE)
	{
		if (LOWORD(wp) == WA_INACTIVE)
		{
			// Remove our special accelerator table.
			AfApp::Papp()->RemoveAccelTable(m_atid);
		}
		else
		{
			// We load this basic accelerator table so that these commands can be directed to
			// this window.  This allows the embedded Views to see the commands. Otherwise, if
			// they are translated by the main window, the main window is the 'target', and the
			// command handlers on AfVwRootSite don't work, because the root site is not a child
			// window of the main one.
			// I'm creating and destroying in Activate/Deactivate partly because I copied the
			// code from AfFindDialog, but also just to make sure this accel table can't be
			// accidentally used for other windows.
			m_atid = AfApp::Papp()->LoadAccelTable(kridAccelBasic, 0, m_hwnd);
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool PossChsrIns::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	plpi->LoadPossList(m_psslId, m_ws, &m_qpli);

	if (m_fInsert)
	{
		StrApp str(kcidPossInsert);
		::SendMessage(m_hwnd, WM_SETTEXT, (WPARAM)0, (LPARAM)str.Chars());
	}
	else
	{
		StrApp str(kcidPossRename);
		::SendMessage(m_hwnd, WM_SETTEXT, (WPARAM)0, (LPARAM)str.Chars());
	}

	// Subclass the edit boxes for TsStrings.
	ILgWritingSystemFactoryPtr qwsf;
	plpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);
	m_qteName.Create();
	int wsActual = plpi->ActualWs(m_ws);
	m_qteName->SubclassEdit(m_hwnd, kctidChsrInsName, qwsf, wsActual, WS_EX_CLIENTEDGE);
	m_qteAbbr.Create();
	m_qteAbbr->SubclassEdit(m_hwnd, kctidChsrInsAbbr, qwsf, wsActual, WS_EX_CLIENTEDGE);
	m_qteDesc.Create();
	m_qteDesc->SubclassEdit(m_hwnd, kctidChsrInsDesc, qwsf, wsActual, WS_EX_CLIENTEDGE);

	// Put the initial information in the edit boxes.
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	ITsStringPtr qtss;
	qtsf->MakeStringRgch(m_stuName.Chars(), m_stuName.Length(), wsActual, &qtss);
	m_qteName->SetText(qtss);
	qtsf->MakeStringRgch(m_stuAbbr.Chars(), m_stuAbbr.Length(), wsActual, &qtss);
	m_qteAbbr->SetText(qtss);
	qtsf->MakeStringRgch(m_stuDesc.Chars(), m_stuDesc.Length(), wsActual, &qtss);
	m_qteDesc->SetText(qtss);

	if (!m_stuName.Length() && !m_stuAbbr.Length())
		::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), false);

	::SendMessage(::GetDlgItem(m_hwnd, kctidChsrInsName), EM_LIMITTEXT,
		(WPARAM)kcchPossNameAbbrMax, (LPARAM)0);
	::SendMessage(::GetDlgItem(m_hwnd, kctidChsrInsAbbr), EM_LIMITTEXT,
		(WPARAM)kcchPossNameAbbrMax, (LPARAM)0);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Process notifications for this dialog from some event on a control.  This method is called
	by the framework.

	@param ctid Id of the control that issued the windows command.
	@param pnmh Windows command that is being passed.
	@param lnRet return value to be returned to the windows command.
	@return true if command is handled.
	See ${AfWnd#OnNotifyChild}
----------------------------------------------------------------------------------------------*/
bool PossChsrIns::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	int iMatch;
	ITsStringPtr qtss;
	const OLECHAR * prgchText;
	int cchText;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	int wsActual = m_qpli->GetLpInfoPtr()->ActualWs(m_ws);

	switch (pnmh->code)
	{
	case EN_CHANGE:
		{
			// This is called when window is first created, so we may not have other windows.
			int cchName = m_qteName ? m_qteName->GetTextLength() : 0;
			int cchAbbr = m_qteAbbr ? m_qteAbbr->GetTextLength() : 0;
			if (!cchName && !cchAbbr)
				::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), false);
			else
				::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), true);
			break;
		}

	case EN_KILLFOCUS: // Edit control modified.
		{
			switch (pnmh->idFrom)
			{
			case kctidChsrInsDesc:
				m_qteDesc->GetText(&qtss);
				CheckHr(qtss->LockText(&prgchText, &cchText));
				m_stuDesc.Assign(prgchText, cchText);
				CheckHr(qtss->UnlockText(prgchText));
				break;
			case kctidChsrInsName:
				{
					// Get the name from the edit box.
					m_qteName->GetText(&qtss);
					CheckHr(qtss->LockText(&prgchText, &cchText));
					m_stuName.Assign(prgchText, cchText);
					CheckHr(qtss->UnlockText(prgchText));

					// Trim leading and trailing space characters.
					StrUtil::TrimWhiteSpace(m_stuName.Chars(), m_stuName);

					qtsf->MakeStringRgch(m_stuName.Chars(), m_stuName.Length(), wsActual,
						&qtss);
					m_qteName->SetText(qtss);

					// Allow a blank name at this point, it will be prohibited in "OnApply".
					if (!m_stuName.Length())
						return true;

					if (m_qpli->GetAllowDup())
					{
						SetAbbr();
						return true;
					}

					// return if the help button or the Cancel button was pressed
					HWND hwnd = ::GetFocus();
					if (hwnd != ::GetDlgItem(m_hwnd, kctidChsrInsAbbr) &&
							hwnd != ::GetDlgItem(m_hwnd, kctidChsrInsDesc) &&
							hwnd != ::GetDlgItem(m_hwnd, kctidOk))
					{
						SetAbbr();
						return true;
					}

					if (m_qpli->PossUniqueName(m_ipss, m_stuName, kpntName, iMatch))
					{
						SetAbbr();
						return true;
					}
					StrApp strMsg(kstidChsrInsDup);
					StrApp strHead(kstidChsrInsDupHead);
					::MessageBox(m_hwnd, strMsg.Chars(), strHead.Chars(), MB_OK |
						MB_ICONINFORMATION);
					::SetFocus(pnmh->hwndFrom);
					return true;
				}
			case kctidChsrInsAbbr:
				{
					// Get the abbreviation from the edit box.
					m_qteAbbr->GetText(&qtss);
					CheckHr(qtss->LockText(&prgchText, &cchText));
					m_stuAbbr.Assign(prgchText, cchText);
					CheckHr(qtss->UnlockText(prgchText));

					// Trim leading and trailing space characters.
					StrUtil::TrimWhiteSpace(m_stuAbbr.Chars(), m_stuAbbr);

					qtsf->MakeStringRgch(m_stuAbbr.Chars(), m_stuAbbr.Length(),
						wsActual, &qtss);
					m_qteAbbr->SetText(qtss);

					// Allow a blank abbreviation at this point, it will be prohibited in "OnApply".
					if (!m_stuAbbr.Length())
						return true;

					if (m_qpli->GetAllowDup())
					{
						SetName();
						return true;
					}

					// return if the help button or the Cancel button was pressed
					HWND hwnd = ::GetFocus();
					if (hwnd != ::GetDlgItem(m_hwnd, kctidChsrInsName) &&
							hwnd != ::GetDlgItem(m_hwnd, kctidChsrInsDesc) &&
							hwnd != ::GetDlgItem(m_hwnd, kctidOk))
					{
						SetName();
						return true;
					}

					if (m_qpli->PossUniqueName(m_ipss, m_stuAbbr, kpntAbbreviation, iMatch))
					{
						SetName();
						return true;
					}
					StrApp strMsg(kstidChsrInsDup);
					StrApp strHead(kstidChsrInsDupHead);
					::MessageBox(m_hwnd, strMsg.Chars(), strHead.Chars(), MB_OK |
						MB_ICONINFORMATION);
					::SetFocus(pnmh->hwndFrom);
					return true;
				}
			}
		}
	}

	return AfWnd::OnNotifyChild(ctid, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	If there is a name but no Abbr. then it makes the abbreviation by stripping all spaces
	from the name and limit it to 8 Chrs., then makes m_strAbbr unique, then writes it back to
	the Abbreviation textbox
----------------------------------------------------------------------------------------------*/
void PossChsrIns::SetAbbr()
{
	if (m_stuAbbr.Length() || !m_stuName.Length())
		return;
	// Make the abbreviation by stripping all spaces from the name
	// and limit it to 8 Chrs.
	int ich = 0;
	UnicodeString us(m_stuName.Chars());

	bool fSurr;
	while ((ich < m_stuName.Length()) && (m_stuAbbr.Length() < 8))
	{
		fSurr = false;
		UChar32 ch = us.charAt(ich);
		if (U16_IS_SURROGATE(ch))
		{
			fSurr = true;
			ch = us.char32At(ich);
		}
		if (! u_isUWhiteSpace(ch))
			m_stuAbbr.Append(m_stuName.Mid(ich, 1).Chars());
		ich++;
		if (fSurr)
			ich++;
	}

	// make m_strAbbr unique
	// I don't care about the stuName so I just use "z9z9"
	StrUni stuName("z9z9");
	StrUni stuAbbr(m_stuAbbr);
	m_qpli->ValidPossName(-1, stuAbbr, stuName);
	m_stuAbbr.Assign(stuAbbr);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	ITsStringPtr qtss;
	qtsf->MakeStringRgch(m_stuAbbr.Chars(), m_stuAbbr.Length(),
		m_qpli->GetLpInfoPtr()->ActualWs(m_ws), &qtss);
	m_qteAbbr->SetText(qtss);

	HWND hwnd = ::GetFocus();
	if (hwnd == ::GetDlgItem(m_hwnd, kctidChsrInsAbbr))
		::PostMessage(::GetDlgItem(m_hwnd, kctidChsrInsAbbr),
			EM_SETSEL, (WPARAM)0, (LPARAM)-1);
}


/*----------------------------------------------------------------------------------------------
	If there is a abbr but no name then it copies then it takes the name and makes it an unique
	name, then writes it to the name textbox
----------------------------------------------------------------------------------------------*/
void PossChsrIns::SetName()
{
	if (!m_stuAbbr.Length() || m_stuName.Length())
		return;
	m_stuName.Assign(m_stuAbbr.Chars());
	// make m_strName unique
	// I don't care about the stuAbbr so I just use "z9z9"
	StrUni stuAbbr("z9z9");
	StrUni stuName(m_stuName);
	m_qpli->ValidPossName(-1, stuAbbr, stuName);
	m_stuName.Assign(stuName);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	ITsStringPtr qtss;
	qtsf->MakeStringRgch(m_stuName.Chars(), m_stuName.Length(),
		m_qpli->GetLpInfoPtr()->ActualWs(m_ws), &qtss);
	m_qteName->SetText(qtss);

	HWND hwnd = ::GetFocus();
	if (hwnd == ::GetDlgItem(m_hwnd, kctidChsrInsName))
		::PostMessage(::GetDlgItem(m_hwnd, kctidChsrInsName),
			EM_SETSEL, (WPARAM)0, (LPARAM)-1);
}


/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the OK or the Apply Now button.
	When the framework calls this method, changes are made in the dialog are accepted if the
	return value is true.

	@param fClose not used here
	@return true if Successful
----------------------------------------------------------------------------------------------*/
bool PossChsrIns::OnApply(bool fClose)
{
	ITsStringPtr qtss;
	const OLECHAR * prgchText;
	int cchText;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);

	// Get the name from the edit box.
	m_qteName->GetText(&qtss);
	CheckHr(qtss->LockText(&prgchText, &cchText));
	m_stuName.Assign(prgchText, cchText);
	CheckHr(qtss->UnlockText(prgchText));

	// Get the abbreviation from the edit box.
	m_qteAbbr->GetText(&qtss);
	CheckHr(qtss->LockText(&prgchText, &cchText));
	m_stuAbbr.Assign(prgchText, cchText);
	CheckHr(qtss->UnlockText(prgchText));

	// Make sure Name does not have a ":" or a " - " in the string
	int ich = m_stuName.FindStr(L":");
	bool fFixed = false;
	while (ich > 0)
	{
		m_stuName.Replace(ich,ich + 1,"-");
		fFixed = true;
		ich = m_stuName.FindStr(L":");
	}
	ich = m_stuName.FindStr(L" - ");
	while (ich > 0)
	{
		m_stuName.Replace(ich,ich + 3,"-");
		fFixed = true;
		ich = m_stuName.FindStr(L" - ");
	}

	// Make sure Abbr does not have a ":" or a " - " in the string
	ich = m_stuAbbr.FindStr(L":");
	while (ich > 0)
	{
		m_stuAbbr.Replace(ich,ich + 1,"-");
		fFixed = true;
		ich = m_stuAbbr.FindStr(L":");
	}
	ich = m_stuAbbr.FindStr(L" - ");
	while (ich > 0)
	{
		m_stuAbbr.Replace(ich,ich + 3,"-");
		fFixed = true;
		ich = m_stuAbbr.FindStr(L" - ");
	}

	if (fFixed)
	{
		int wsActual = m_qpli->GetLpInfoPtr()->ActualWs(m_ws);
		qtsf->MakeStringRgch(m_stuName.Chars(), m_stuName.Length(), wsActual, &qtss);
		m_qteName->SetText(qtss);
		qtsf->MakeStringRgch(m_stuAbbr.Chars(), m_stuAbbr.Length(), wsActual, &qtss);
		m_qteAbbr->SetText(qtss);

		StrApp strMsg(kstidFixedStr);
		StrApp strTitle(kstidFixedStrTitle);
		::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(), MB_OK | MB_ICONINFORMATION);
		return false;
	}

	if (!m_qpli->GetAllowDup())
	{
		if (!m_stuName.Length() && !m_stuAbbr.Length())
			return false;

		int iMatch;
		if (!m_qpli->PossUniqueName(m_ipss, m_stuName, kpntName, iMatch) ||
			!m_qpli->PossUniqueName(m_ipss, m_stuAbbr, kpntAbbreviation, iMatch))
		{
			StrApp strMsg(kstidChsrInsDup);
			StrApp strHead(kstidChsrInsDupHead);
			::MessageBox(m_hwnd, strMsg.Chars(), strHead.Chars(), MB_OK |
				MB_ICONINFORMATION);
			return false;
		}
	}
	return AfDialog::OnApply(fClose);
}


/*----------------------------------------------------------------------------------------------
	Something has changed.
----------------------------------------------------------------------------------------------*/
bool PossChsrIns::PssTssEdit::OnChange()
{
	::SendMessage(::GetParent(m_hwnd), WM_COMMAND,
		MAKEWPARAM(::GetDlgCtrlID(m_hwnd), EN_CHANGE), (LPARAM)m_hwnd);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Setting focus on the edit box.
	@param hwndOld The handle of the window losing focus.
	@param fTbControl ?
	@return superclass' OnSetFocus success state
----------------------------------------------------------------------------------------------*/
bool PossChsrIns::PssTssEdit::OnSetFocus(HWND hwndOld, bool fTbControl)
{
	// Using SuperClass below is ambiguous.
	return ScrollSuperClass::OnSetFocus(hwndOld, fTbControl);
}


/*----------------------------------------------------------------------------------------------
	Clearing focus on the edit box
	@param hwndNew The handle of the window gaining focus.
----------------------------------------------------------------------------------------------*/
bool PossChsrIns::PssTssEdit::OnKillFocus(HWND hwndNew)
{
	m_qrootb->DestroySelection(); // Clear selection when we tab to next edit box.
	ScrollSuperClass::OnKillFocus(hwndNew);
	::SendMessage(::GetParent(m_hwnd), WM_COMMAND,
		MAKEWPARAM(::GetDlgCtrlID(m_hwnd), EN_KILLFOCUS), (LPARAM)m_hwnd);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Constructor for PossChsrChoices
----------------------------------------------------------------------------------------------*/
PossChsrChoices::PossChsrChoices(void)
{
	m_qvcd = NULL;
}


/*----------------------------------------------------------------------------------------------
	Create the view window.

	@param hwndPar Handle to the parent window.
	@param wid Child window identifier to use for the view window.
	@param pvcd Pointer to the data cache containing the filter information.
----------------------------------------------------------------------------------------------*/
void PossChsrChoices::Create(HWND hwndPar, int wid, IVwCacheDa * pvcd, int wsUser)
{
	AssertPtr(pvcd);

	m_qvcd = pvcd;

	WndCreateStruct wcs;
	wcs.InitChild(_T("AfVwWnd"), hwndPar, wid);
	wcs.style |= WS_VISIBLE | WS_TABSTOP| WS_VSCROLL;
	//wcs.dwExStyle = WS_EX_CLIENTEDGE;
	// Since the choices list is placed inside the owner draw button, it must not clip siblings or
	// it won't show up at all.
	wcs.style &= ~WS_CLIPSIBLINGS;
	Rect rcBounds;
	HWND hwndTemp = ::GetDlgItem(hwndPar, wid);
	::GetWindowRect(hwndTemp, &rcBounds);
	::DestroyWindow(hwndTemp);

	// Get rectangle for child window, in pixels relative to parent
	Rect rcMyBounds;
	::GetWindowRect(hwndPar, &rcMyBounds);
	rcBounds.Offset(-rcMyBounds.left, -rcMyBounds.top);
	// Reduce the size of the view to exclude the border and make it fit.
	SIZE sizeMargins = { 0, ::GetSystemMetrics(SM_CYCAPTION) };
	rcBounds.top -= sizeMargins.cy;
	rcBounds.bottom -= sizeMargins.cy;
	rcBounds.left += ::GetSystemMetrics(SM_CXEDGE);
	rcBounds.right -= 2 * ::GetSystemMetrics(SM_CXEDGE) + 1;
	rcBounds.bottom -= 2 * ::GetSystemMetrics(SM_CYEDGE) + 1;
	wcs.SetRect(rcBounds);
	CreateHwnd(wcs);
}

/*----------------------------------------------------------------------------------------------
	Redraw the window
----------------------------------------------------------------------------------------------*/
void PossChsrChoices::Redraw()
{
	m_qrootb->Reconstruct();
}

/*----------------------------------------------------------------------------------------------
	Make the root box.
----------------------------------------------------------------------------------------------*/
void PossChsrChoices::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
	IVwRootBox ** pprootb)
{
	AssertPtrN(pwsf);
	*pprootb = NULL;

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this));

	// Make an arbitrary ID for a dummy root object
	HVO hvo = 1;
	int frag = 0;

	// Set up a new view constructor.
	m_qpccvc.Attach(NewObj PossChsrChoicesVc());
	m_qpccvc->InitValues(m_ppcd);

	ISilDataAccessPtr qsdaTemp;
	HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);
	if (pwsf)
		CheckHr(qsdaTemp->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qsdaTemp));

	IVwViewConstructor * pvvc = m_qpccvc;
	CheckHr(qrootb->SetRootObjects(&hvo, &pvvc, &frag, NULL, 1));
	*pprootb = qrootb;
	(*pprootb)->AddRef();
}

static DummyFactory g_fact(_T("SIL.AppCore.PossChsrChoicesVc"));

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them. Construct
	the complete contents of the preview.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP PossChsrChoicesVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwenv);

	// If we are not supposed to be showing the choices window then return.
	if (!(m_ppcd->GetShowCurrent()))
		return S_OK;

	// Constant fragments.
	if (frag == 0)
	{
		CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);

		HWND hwndTree = m_ppcd->GetHwndTreeChoiceList();
		Vector<int> vipss;
		m_ppcd->GetCheckedItems(hwndTree, TreeView_GetRoot(hwndTree), vipss);
		ITsStringPtr qtssChoice;
		ITsIncStrBldrPtr qtisb;
		qtisb.CreateInstance(CLSID_TsIncStrBldr);
		StrUni stuPss;

		// Display each item as a separate paragraph.
		int citem = vipss.Size();
		int ws;
		for (int iitem = 0; iitem < citem; iitem++)
		{
			CheckHr(pvwenv->OpenParagraph());
			PossItemInfo * ppii = m_ppcd->GetPossListInfo()->GetPssFromIndex(vipss[iitem]);
			AssertPtr(ppii);
			ppii->GetName(stuPss, m_ppcd->GetPossNameType());
			ws = ppii->GetWs();
			qtisb->Clear();
			CheckHr(qtisb->SetIntPropValues(ktptWs, 0, ws));
			CheckHr(qtisb->AppendRgch(stuPss.Chars(), stuPss.Length()));
			CheckHr(qtisb->GetString(&qtssChoice));
			CheckHr(pvwenv->AddString(qtssChoice));
			CheckHr(pvwenv->CloseParagraph());
		}
	}
	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}

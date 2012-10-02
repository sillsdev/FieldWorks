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
	ON_CID_ALL(kcidWndCascad, &AfApp::CmdWndCascade, NULL)
	ON_CID_ALL(kcidWndTile, &AfApp::CmdWndTileHoriz, NULL)
	ON_CID_ALL(kcidWndSideBy, &AfApp::CmdWndTileVert, NULL)
END_CMD_MAP_NIL()


BEGIN_CMD_MAP(HwMainWnd)
	ON_CID_GEN(kcidViewStatBar, &AfMainWnd::CmdSbToggle, &AfMainWnd::CmsSbUpdate)

	ON_CID_ME(kcidWndNew, &HwMainWnd::CmdWndNew, NULL)
	ON_CID_ME(kcidWndSplit, &HwMainWnd::CmdWndSplit, &HwMainWnd::CmsWndSplit)
	ON_CID_ME(kcidHelpWhatsThis, &HwMainWnd::CmdHelpMode, NULL)

	ON_CID_ME(kcidViewVbar, &HwMainWnd::CmdVbToggle, &HwMainWnd::CmsVbUpdate)
	ON_CID_ME(kcidViewSbar, &HwMainWnd::CmdSbToggle, &AfMainWnd::CmsSbUpdate)

	ON_CID_CHILD(kcidExpViews, &HwMainWnd::CmdViewExpMenu, &HwMainWnd::CmsViewExpMenu)
	ON_CID_CHILD(kcidExpGroup0, &HwMainWnd::CmdViewExpMenu, &HwMainWnd::CmsViewExpMenu)
	ON_CID_CHILD(kcidExpGroup1, &HwMainWnd::CmdViewExpMenu, &HwMainWnd::CmsViewExpMenu)
END_CMD_MAP_NIL()


typedef struct
{
	achar * pszName;
	int imag;
} DummyListBarItem;

DummyListBarItem s_rgViews[] = {
	{ _T("View Left"),   HwMainWnd::kimagView0 },
	{ _T("View Top"),    HwMainWnd::kimagView1 },
	{ _T("View Right"),  HwMainWnd::kimagView2 },
	{ _T("View Bottom"), HwMainWnd::kimagView3 },
};

DummyListBarItem s_rgGroup0[] = {
	{ _T("G0 Item 0"), HwMainWnd::kimagGroup0 },
	{ _T("G0 Item 1"), HwMainWnd::kimagGroup0 },
	{ _T("G0 Item 2"), HwMainWnd::kimagGroup0 },
};

DummyListBarItem s_rgGroup1[] = {
	{ _T("G1 Item 0"), HwMainWnd::kimagGroup1 },
	{ _T("G1 Item 1"), HwMainWnd::kimagGroup1 },
	{ _T("G1 Item 2"), HwMainWnd::kimagGroup1 },
	{ _T("G1 Item 3"), HwMainWnd::kimagGroup1 },
	{ _T("G1 Item 4"), HwMainWnd::kimagGroup1 },
};

/***********************************************************************************************
	HwApp methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
HwApp::HwApp()
{
	s_fws.SetRoot("Hello World Viewbar"); //"Software\\SIL\\FieldWorks\\Hello World Viewbar";
}


/*----------------------------------------------------------------------------------------------
	Initialize the application.
----------------------------------------------------------------------------------------------*/
void HwApp::Init(void)
{
	SuperClass::Init();

	AfWnd::RegisterClass("HwMainWnd", 0, 0, 0, COLOR_3DFACE, (int)kridHelloWorldIcon);
	AfWnd::RegisterClass("HwSplitChild", kfwcsHorzRedraw | kfwcsVertRedraw, (int)IDC_ARROW, 0,
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
	Destructor.
----------------------------------------------------------------------------------------------*/
HwMainWnd::~HwMainWnd()
{
	if (m_rghiml[0])
		ImageList_Destroy(m_rghiml[0]);
	if (m_rghiml[1])
		ImageList_Destroy(m_rghiml[1]);
}


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

	// Load the width of the view bar.
	DWORD dwT;
	if (pfws->GetDword(pszRoot, "Viewbar Width", &dwT))
		m_dxpLeft = dwT;

	// Read the viewbar settings. If the settings aren't there, use default values.
	DWORD dwViewbarFlags;
	if (!pfws->GetDword(pszRoot, "Viewbar Flags", &dwViewbarFlags))
	{
		dwViewbarFlags = kmskShowLargeGroup0Icons |
			kmskShowLargeGroup1Icons |
			kmskShowViewBar;
	}

	m_qvwbrs->ChangeIconSize(kiViewsList, dwViewbarFlags & kmskShowLargeGroup0Icons);
	m_qvwbrs->ChangeIconSize(kiGroup0List, dwViewbarFlags & kmskShowLargeGroup0Icons);
	m_qvwbrs->ChangeIconSize(kiGroup1List, dwViewbarFlags & kmskShowLargeGroup1Icons);

	if (!pfws->GetDword(pszRoot, "LastViewBarGroup", &dwT))
		dwT = kiViewsList;
	m_qvwbrs->SetCurrentList(Min((uint)dwT, (uint)(kiListMax - 1)));

	m_qvwbrs->ShowViewBar(dwViewbarFlags & kmskShowViewBar);

	DWORD dwStatusbarFlags;
	if (!pfws->GetDword(pszRoot, "Statusbar Flags", &dwStatusbarFlags))
		dwStatusbarFlags = 1;
	::ShowWindow(m_qstbr->Hwnd(), dwStatusbarFlags ? SW_SHOW : SW_HIDE);

	// Get window position.
	LoadWindowPosition(pszRoot, "Position");

	::ShowWindow(m_hwnd, SW_SHOW);
	OnIdle();
	::UpdateWindow(m_hwnd);

	Set<int> sisel;
	int isel;

	int cv = SizeOfArray(s_rgViews);
	if (!pfws->GetDword(pszRoot, "LastView", &dwT) || (uint)dwT >= (uint)cv)
		dwT = 0;
	sisel.Clear();
	isel = (int)dwT;
	sisel.Insert(isel);
	m_qvwbrs->SetSelection(kiViewsList, sisel);

	cv = SizeOfArray(s_rgGroup0);
	if (!pfws->GetDword(pszRoot, "LastGroup0", &dwT) || (uint)dwT >= (uint)cv)
		dwT = 0;
	sisel.Clear();
	isel = (int)dwT;
	sisel.Insert(isel);
	m_qvwbrs->SetSelection(kiGroup0List, sisel);

	cv = SizeOfArray(s_rgGroup1);
	if (!pfws->GetDword(pszRoot, "LastGroup1", &dwT))
		dwT = 0;
	sisel.Clear();
	for (int iv = 0; iv < cv; iv++)
	{
		if (dwT & (1 << (iv + 1)))
			sisel.Insert(iv);
	}
	m_qvwbrs->SetSelection(kiGroup1List, sisel);
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

	// Store the settings for the viewbar.
	AssertObj(m_qvwbrs);
	DWORD dwViewbarFlags = 0;
	if (m_qvwbrs->IsShowingLargeIcons(kiGroup0List))
		dwViewbarFlags |= kmskShowLargeGroup0Icons;
	if (m_qvwbrs->IsShowingLargeIcons(kiGroup1List))
		dwViewbarFlags |= kmskShowLargeGroup1Icons;
	if (::IsWindowVisible(m_qvwbrs->Hwnd()))
		dwViewbarFlags |= kmskShowViewBar;
	pfws->SetDword(pszRoot, "Viewbar Flags", dwViewbarFlags);

	// Store the settings for the status bar.
	pfws->SetDword(pszRoot, "Statusbar Flags", ::IsWindowVisible(m_qstbr->Hwnd()));

	// Store the Views item that is currently selected.
	Set<int> sisel;
	m_qvwbrs->GetSelection(kiViewsList, sisel);
	Assert(sisel.Size() == 1);
	pfws->SetDword(pszRoot, "LastView", *sisel.Begin());

	// Store the Group 0 item that is currently selected.
	m_qvwbrs->GetSelection(kiGroup0List, sisel);
	Assert(sisel.Size() == 1);
	pfws->SetDword(pszRoot, "LastGroup0", *sisel.Begin());

	// Store the Group 1 item that is currently selected.
	DWORD dwGroup1 = 0;
	m_qvwbrs->GetSelection(kiGroup1List, sisel);
	Assert(sisel.Size() >= 0);
	Set<int>::iterator sit = sisel.Begin();
	Set<int>::iterator sitStop = sisel.End();
	while (sit != sitStop)
	{
		dwGroup1 |= 1 << (*sit + 1);
		++sit;
	}
	pfws->SetDword(pszRoot, "LastGroup1", dwGroup1);

	// Store the width of the view bar.
	pfws->SetDword(pszRoot, "Viewbar Width", m_dxpLeft);

	// Store which view bar group is currently selected.
	pfws->SetDword(pszRoot, "LastViewBarGroup", m_qvwbrs->GetCurrentList());
}


/*----------------------------------------------------------------------------------------------
	The user has selected a different item in the view bar.
----------------------------------------------------------------------------------------------*/
bool HwMainWnd::OnViewBarChange(int ilist, Set<int> & siselOld, Set<int> & siselNew)
{
	AfCaptionBar * pcpbr = m_qmdic->GetCaptionBar();
	AssertPtr(pcpbr);

	switch (ilist)
	{
	case kiViewsList:
		{
			Assert(siselNew.Size() == 1);
			int iv = *siselNew.Begin();
			m_qmdic->SetCurChildFromIndex(iv);

			// Update the caption bar.
			AfClientWnd * pafcw = m_qmdic->GetCurChild();
			AssertPtr(pafcw);
			pcpbr->SetCaptionText(pafcw->GetViewName());
			pcpbr->SetIconImage(kiViewsList, pafcw->GetImageIndex());
		}
		break;
	case kiGroup0List:
		{
			// Update the caption bar. This is needed to refresh the tooltip.
			pcpbr->SetIconImage(kiGroup0List, kimagGroup0);

			Assert(siselNew.Size() == 1);
			int iv = *siselNew.Begin();
			StrApp strMsg;
			strMsg.Format(_T("Group 0: The following item is selected: %d."), iv);
			::MessageBox(m_hwnd, strMsg.Chars(), "HelloWorld", MB_OK);
		}
		break;
	case kiGroup1List:
		{
			// Update the caption bar. This is needed to refresh the tooltip.
			pcpbr->SetIconImage(kiGroup1List, kimagGroup1);

			StrApp strMsg;
			if (siselNew.Size() == 0)
			{
				strMsg = _T("Group 1: No items are selected.");
			}
			else if (siselNew.Size() == 1)
			{
				strMsg.Format(_T("Group 1: The following item is selected: %d."),
					*siselNew.Begin());
			}
			else
			{
				Assert(siselNew.Size() > 1);
				Set<int>::iterator sit = siselNew.Begin();
				strMsg.Format(_T("Group 1: The following items are selected: %d"), *sit);
				while (++sit != siselNew.End())
					strMsg.FormatAppend(_T(", %d"), *sit);
				strMsg.Append(_T("."));
			}
			::MessageBox(m_hwnd, strMsg.Chars(), "HelloWorld", MB_OK);
		}
		break;
	}
	return true;
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

	// Insert the Group 0 group into the viewbar.
	m_qvwbrs->AddList("Views", m_rghiml[0], m_rghiml[1], false);
	int cv = SizeOfArray(s_rgViews);
	for (int iv = 0; iv < cv; iv++)
		m_qvwbrs->AddListItem(kiViewsList, s_rgViews[iv].pszName, s_rgViews[iv].imag);

	m_qvwbrs->AddList("Group 0", m_rghiml[0], m_rghiml[1], false);
	cv = SizeOfArray(s_rgGroup0);
	for (iv = 0; iv < cv; iv++)
		m_qvwbrs->AddListItem(kiGroup0List, s_rgGroup0[iv].pszName, s_rgGroup0[iv].imag);

	// Note that this list can have multiple items selected at once.
	m_qvwbrs->AddList("Group 1", m_rghiml[0], m_rghiml[1], true);
	cv = SizeOfArray(s_rgGroup1);
	for (iv = 0; iv < cv; iv++)
		m_qvwbrs->AddListItem(kiGroup1List, s_rgGroup1[iv].pszName, s_rgGroup1[iv].imag);

	// Load window settings.
	LoadSettings(NULL, false);

	g_app.AddCmdHandler(this, 1);
	m_qstbr->RestoreStatusText();
}


/*----------------------------------------------------------------------------------------------
	Create the client windows and attach them to the MDI client.
----------------------------------------------------------------------------------------------*/
void HwMainWnd::InitMdiClient()
{
	// Initialize the image lists
	m_rghiml[0] = ::ImageList_Create(32, 32, ILC_COLORDDB | ILC_MASK, 0, 0);
	if (!m_rghiml[0])
		ThrowHr(WarnHr(E_FAIL));
	HBITMAP hbmp = ::LoadBitmap(ModuleEntry::GetModuleHandle(),
		MAKEINTRESOURCE(kridHwVBarLarge));
	if (!hbmp)
		ThrowHr(WarnHr(E_FAIL));
	::ImageList_AddMasked(m_rghiml[0], hbmp, kclrPink);
	::DeleteObject(hbmp);

	m_rghiml[1] = ::ImageList_Create(16, 16, ILC_COLORDDB | ILC_MASK, 0, 0);
	if (!m_rghiml[1])
		ThrowHr(WarnHr(E_FAIL));
	hbmp = ::LoadBitmap(ModuleEntry::GetModuleHandle(), MAKEINTRESOURCE(kridHwVBarSmall));
	if (!hbmp)
		ThrowHr(WarnHr(E_FAIL));
	::ImageList_AddMasked(m_rghiml[1], hbmp, kclrPink);
	::DeleteObject(hbmp);

	// Create and attach our caption bar.
	HwCaptionBarPtr qhcb;
	qhcb.Attach(NewObj HwCaptionBar(this));
	m_qmdic->SetCaptionBar(qhcb);

	// Create the client child window.
	AfClientWndPtr qafcw;
	int wid = kwidChildBase;

	qafcw.Attach(NewObj HwClientWnd0);
	qafcw->Create(s_rgViews[0].pszName, kimagView0, wid++);
	m_qmdic->AddChild(qafcw);

	qafcw.Attach(NewObj HwClientWnd1);
	qafcw->Create(s_rgViews[1].pszName, kimagView1, wid++);
	m_qmdic->AddChild(qafcw);

	qafcw.Attach(NewObj HwClientWnd2);
	qafcw->Create(s_rgViews[2].pszName, kimagView2, wid++);
	m_qmdic->AddChild(qafcw);

	qafcw.Attach(NewObj HwClientWnd3);
	qafcw->Create(s_rgViews[3].pszName, kimagView3, wid++);
	m_qmdic->AddChild(qafcw);

	m_qmdic->SetClientIndexLim(wid);
}


/*----------------------------------------------------------------------------------------------
	Bring up another top-level window with the same view.
----------------------------------------------------------------------------------------------*/
bool HwMainWnd::CmdWndNew(Cmd * pcmd)
{
	SaveSettings(NULL);

	WndCreateStruct wcs;
	wcs.InitMain("HwMainWnd");

	HwMainWndPtr qwnd;
	qwnd.Create();
	qwnd->CreateHwnd(wcs);

	Rect rc;
	::GetWindowRect(m_hwnd, &rc);
	int dypCaption = ::GetSystemMetrics(SM_CYCAPTION) + ::GetSystemMetrics(SM_CYSIZEFRAME);
	rc.Offset(dypCaption, dypCaption);
	AfGfx::EnsureVisibleRect(rc);
	::MoveWindow(qwnd->Hwnd(), rc.left, rc.top, rc.Width(), rc.Height(), true);
	qwnd->Show(SW_SHOW);

	return true;
}


/*----------------------------------------------------------------------------------------------
	If the current client window is already split, unsplit it. Otherwise split it in half
	horizontally.
----------------------------------------------------------------------------------------------*/
bool HwMainWnd::CmdWndSplit(Cmd * pcmd)
{
	AssertPtr(pcmd);
	AfSplitFrame * psplf = dynamic_cast<AfSplitFrame *>(GetMdiClientWnd()->GetCurChild());
	AssertPtr(psplf);
	if (psplf->GetPane(1) == NULL)
		psplf->SplitWindow(-1);
	else
		psplf->UnsplitWindow(true);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Set the state of the split window toolbar/menu item. It should be checked when the window
	is split.
----------------------------------------------------------------------------------------------*/
bool HwMainWnd::CmsWndSplit(CmdState & cms)
{
	AfSplitFrame * psplf = dynamic_cast<AfSplitFrame *>(GetMdiClientWnd()->GetCurChild());
	// This can be NULL when the main window is first getting created because the
	// child windows have not been created yet.
	AssertPtrN(psplf);
	if (psplf)
		cms.SetCheck(psplf->GetPane(1));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Enter What's This help mode.
----------------------------------------------------------------------------------------------*/
bool HwMainWnd::CmdHelpMode(Cmd * pcmd)
{
	ToggleHelpMode();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Toggle the visibility of the view bar.
----------------------------------------------------------------------------------------------*/
bool HwMainWnd::CmdVbToggle(Cmd * pcmd)
{
	AssertObj(pcmd);

	m_qvwbrs->ShowViewBar(!::IsWindowVisible(m_qvwbrs->Hwnd()));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Set the state of the view bar toolbar/menu item.
----------------------------------------------------------------------------------------------*/
bool HwMainWnd::CmsVbUpdate(CmdState & cms)
{
	cms.SetCheck(::IsWindowVisible(m_qvwbrs->Hwnd()));
	return true;
}


/*----------------------------------------------------------------------------------------------
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
bool HwMainWnd::CmdViewExpMenu(Cmd * pcmd)
{
	AssertPtr(pcmd);

	int cid = pcmd->m_cid;
	int ma = pcmd->m_rgn[0];
	if (ma == AfMenuMgr::kmaExpandItem)
	{
		// We need to expand the dummy menu item.
		HMENU hmenu = (HMENU)pcmd->m_rgn[1];
		int imni = pcmd->m_rgn[2];
		int & cmniAdded = pcmd->m_rgn[3];

		int cv;
		if (cid == kcidExpViews)
		{
			cv = SizeOfArray(s_rgViews);
			for (int iv = 0; iv < cv; iv++)
			{
				::InsertMenu(hmenu, imni + iv, MF_BYPOSITION, kcidMenuItemDynMin + iv,
					s_rgViews[iv].pszName);
			}
		}
		else if (cid == kcidExpGroup0)
		{
			cv = SizeOfArray(s_rgGroup0);
			for (int iv = 0; iv < cv; iv++)
			{
				::InsertMenu(hmenu, imni + iv, MF_BYPOSITION, kcidMenuItemDynMin + iv,
					s_rgGroup0[iv].pszName);
			}
		}
		else if (cid == kcidExpGroup1)
		{
			cv = SizeOfArray(s_rgGroup1);
			for (int iv = 0; iv < cv; iv++)
			{
				::InsertMenu(hmenu, imni + iv, MF_BYPOSITION, kcidMenuItemDynMin + iv,
					s_rgGroup1[iv].pszName);
			}
		}
		else
		{
			Assert(false); // This should never happen.
		}
		cmniAdded = cv;
		return true;
	}
	else if (ma == AfMenuMgr::kmaGetStatusText)
	{
		// We need to return the text for the expanded menu item.
		//    m_rgn[1] holds the index of the selected item.
		//    m_rgn[2] holds a pointer to the string to set

		int imni = pcmd->m_rgn[1];
		StrApp * pstr = (StrApp *)pcmd->m_rgn[2];
		AssertPtr(pstr);

		if (cid == kcidExpViews)
			pstr->Format(_T("Selects the %s view"), s_rgViews[imni].pszName);
		else if (cid == kcidExpGroup0)
			pstr->Format(_T("Selects the %s item"), s_rgGroup0[imni].pszName);
		else if (cid == kcidExpGroup1)
			pstr->Format(_T("Selects the %s item"), s_rgGroup1[imni].pszName);
		else
			Assert(false); // This should never happen.
		return true;
	}
	else if (ma == AfMenuMgr::kmaDoCommand)
	{
		// The user selected an expanded menu item, so perform the command now.
		//    m_rgn[1] holds the menu handle.
		//    m_rgn[2] holds the index of the selected item.

		Set<int> sisel;
		int iitem = pcmd->m_rgn[2];
		sisel.Insert(iitem);

		if (cid == kcidExpViews)
			m_qvwbrs->SetSelection(kiViewsList, sisel);
		else if (cid == kcidExpGroup0)
			m_qvwbrs->SetSelection(kiGroup0List, sisel);
		else if (cid == kcidExpGroup1)
			m_qvwbrs->SetSelection(kiGroup1List, sisel);
		else
			Assert(false); // This should never happen.
		return true;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Set the state for an expanded menu item.
	   cms.GetExpMenuItemIndex() returns the index of the item to set the state for.
	To get the menu handle and the old ID of the dummy item that was replaced, call
	AfMenuMgr::GetLastExpMenuInfo.
----------------------------------------------------------------------------------------------*/
bool HwMainWnd::CmsViewExpMenu(CmdState & cms)
{
	Set<int> sisel;
	int iitem = cms.GetExpMenuItemIndex();

	switch (cms.Cid())
	{
	case kcidExpViews:
		m_qvwbrs->GetSelection(kiViewsList, sisel);
		break;
	case kcidExpGroup0:
		m_qvwbrs->GetSelection(kiGroup0List, sisel);
		break;
	case kcidExpGroup1:
		m_qvwbrs->GetSelection(kiGroup1List, sisel);
		break;
	}

	cms.SetCheck(sisel.IsMember(iitem));

	return true;
}


/*----------------------------------------------------------------------------------------------
	As it finally goes away, make doubly sure all pointers get cleared. This helps break cycles.
----------------------------------------------------------------------------------------------*/
void HwMainWnd::OnReleasePtr()
{
	g_app.RemoveCmdHandler(this, 1);
	SuperClass::OnReleasePtr();
}


/***********************************************************************************************
	HwClientWnd[0-3] methods.
***********************************************************************************************/

void HwClientWnd0::CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew)
{
	AssertPtr(psplcNew);
	AssertPtrN(psplcCopy);

	WndCreateStruct wcs;
	wcs.InitChild("HwSplitChild", m_hwnd, 0);
	wcs.style |= WS_VISIBLE;

	HwSplitChildPtr qhwsc;
	qhwsc.Attach(NewObj HwSplitChild(0));
	*psplcNew = qhwsc;

	qhwsc->CreateHwnd(wcs);

	AddRefObj(*psplcNew);
}


void HwClientWnd1::CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew)
{
	AssertPtr(psplcNew);
	AssertPtrN(psplcCopy);

	WndCreateStruct wcs;
	wcs.InitChild("HwSplitChild", m_hwnd, 0);
	wcs.style |= WS_VISIBLE;

	HwSplitChildPtr qhwsc;
	qhwsc.Attach(NewObj HwSplitChild(1));
	*psplcNew = qhwsc;

	qhwsc->CreateHwnd(wcs);

	AddRefObj(*psplcNew);
}


void HwClientWnd2::CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew)
{
	AssertPtr(psplcNew);
	AssertPtrN(psplcCopy);

	WndCreateStruct wcs;
	wcs.InitChild("HwSplitChild", m_hwnd, 0);
	wcs.style |= WS_VISIBLE;

	HwSplitChildPtr qhwsc;
	qhwsc.Attach(NewObj HwSplitChild(2));
	*psplcNew = qhwsc;

	qhwsc->CreateHwnd(wcs);

	AddRefObj(*psplcNew);
}


void HwClientWnd3::CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew)
{
	AssertPtr(psplcNew);
	AssertPtrN(psplcCopy);

	WndCreateStruct wcs;
	wcs.InitChild("HwSplitChild", m_hwnd, 0);
	wcs.style |= WS_VISIBLE;

	HwSplitChildPtr qhwsc;
	qhwsc.Attach(NewObj HwSplitChild(3));
	*psplcNew = qhwsc;

	qhwsc->CreateHwnd(wcs);

	AddRefObj(*psplcNew);
}


/***********************************************************************************************
	HwSplitChild methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Paint Hello World in the client area.
----------------------------------------------------------------------------------------------*/
bool HwSplitChild::OnPaint(HDC hdcDef)
{
	Assert(!hdcDef);

	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps);

	Rect rc;
	GetClientRect(rc);

	int dtFlags = DT_SINGLELINE;
	if (m_itype == 0) // left.
		dtFlags |= DT_LEFT | DT_VCENTER;
	else if (m_itype == 1) // top.
		dtFlags |= DT_CENTER | DT_TOP;
	else if (m_itype == 2) // right.
		dtFlags |= DT_RIGHT | DT_VCENTER;
	else if (m_itype == 3) // bottom.
		dtFlags |= DT_CENTER | DT_BOTTOM;
	::DrawText(hdc, _T("Hello World!"), -1, &rc, dtFlags);

	::EndPaint(m_hwnd, &ps);
	return true;
}


/***********************************************************************************************
	HwCaptionBar methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Create one of our caption bars that has an icon for each item in the view bar.
----------------------------------------------------------------------------------------------*/
void HwCaptionBar::Create(HWND hwndPar, int wid, HIMAGELIST himl)
{
	Assert(hwndPar);
	Assert(himl);
	SuperClass::Create(hwndPar, wid, himl);

	// Add icons for each group.
	AddIcon(HwMainWnd::kimagView0);
	AddIcon(HwMainWnd::kimagGroup0);
	AddIcon(HwMainWnd::kimagGroup1);
}


/*----------------------------------------------------------------------------------------------
	Show the appropriate context menu based on the icon that was right-clicked on.
----------------------------------------------------------------------------------------------*/
void HwCaptionBar::ShowContextMenu(int ibtn, Point pt)
{
	HMENU hmenuPopup = ::CreatePopupMenu();

	switch (ibtn)
	{
	case HwMainWnd::kiViewsList:
		::AppendMenu(hmenuPopup, MF_STRING, kcidExpViews, NULL);
		break;
	case HwMainWnd::kiGroup0List:
		::AppendMenu(hmenuPopup, MF_STRING, kcidExpGroup0, NULL);
		break;
	case HwMainWnd::kiGroup1List:
		::AppendMenu(hmenuPopup, MF_STRING, kcidExpGroup1, NULL);
		break;
	default:
		Assert(false); // Should never happen.
		break;
	}

	AssertPtr(m_pwndMain);
	::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y, 0,
		m_pwndMain->Hwnd(), NULL);

	::DestroyMenu(hmenuPopup);
}


/*----------------------------------------------------------------------------------------------
	Get the text that represents this icon. It will be shown in a tooltip.
----------------------------------------------------------------------------------------------*/
void HwCaptionBar::GetIconName(int ibtn, StrApp & str)
{
	str.Clear();

	AfViewBarShell * pvwbrs = m_pwndMain->GetViewBarShell();
	AssertPtr(pvwbrs);
	Set<int> sisel;
	pvwbrs->GetSelection(ibtn, sisel);

	switch (ibtn)
	{
	case HwMainWnd::kiViewsList:
		{
			Assert(sisel.Size() == 1);
			int isel = *sisel.Begin();
			Assert((uint)isel < (uint)SizeOfArray(s_rgViews));
			str = s_rgViews[isel].pszName;
		}
		break;
	case HwMainWnd::kiGroup0List:
		{
			Assert(sisel.Size() == 1);
			int isel = *sisel.Begin();
			Assert((uint)isel < (uint)SizeOfArray(s_rgGroup0));
			str = s_rgGroup0[isel].pszName;
		}
		break;
	case HwMainWnd::kiGroup1List:
		{
			Assert(sisel.Size() >= 0);
			if (sisel.Size() == 0)
			{
				str = "<None>";
			}
			else
			{
				Set<int>::iterator sit = sisel.Begin();
				str = s_rgGroup1[*sit].pszName;
				while (++sit != sisel.End())
					str.FormatAppend(_T(", %s"), s_rgGroup1[*sit].pszName);
			}
		}
		break;
	default:
		Assert(false); // Should never happen.
		break;
	}
}
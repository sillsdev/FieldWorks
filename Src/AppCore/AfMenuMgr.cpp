/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfMenuMgr.cpp.cpp
Responsibility: Shon Katzenberger
Last reviewed:

	Menu manager code. This is intended to be embedded in the frame window.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

using namespace AfMenuMgrUtils;


/*----------------------------------------------------------------------------------------------
	Constants.
----------------------------------------------------------------------------------------------*/

// Drawing constants.
const int kdxsMarg = 1; // Margin on either side of the entire menu item.
const int kdxsGap = 0; // Num pixels between button and text.
const int kdxsTxtMarg = 2; // Num pixels after hilite to start text.
const int kdxsBtnMarg = 2; // Num pixels wider button is than bitmap.
const int kdysBtnMarg = 2; // Ditto for height.

// DrawText flags.
const int kgrfdtDef = DT_SINGLELINE | DT_LEFT | DT_VCENTER;


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfMenuMgr::AfMenuMgr(AfMainWnd * pafw)
{
	AssertObj(this);
//	AssertPtr(pafw);
	m_pafwFrame = pafw;	// Note that this may be NULL if there is currently no frame window.
	m_hfontMenu = NULL;
	m_himl = NULL;
	m_hbmpCheck = NULL;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfMenuMgr::~AfMenuMgr(void)
{
	AssertObj(this);
	if (m_hfontMenu)
	{
		AfGdi::DeleteObjectFont(m_hfontMenu);
		m_hfontMenu = NULL;
	}
	if (m_himl)
	{
		AfGdi::ImageList_Destroy(m_himl);
		m_himl = NULL;
	}
	if (m_hbmpCheck)
	{
		AfGdi::DeleteObjectBitmap(m_hbmpCheck);
		m_hbmpCheck = NULL;
	}
	m_vridTlbr.Clear();
	m_vati.Clear();
	m_vcti.Clear();
	m_vhmenu.Clear();
}


/*----------------------------------------------------------------------------------------------
	Set the menu item states by calling the command dispatcher on each item.
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::SetMenuStates(CmdHandler * pcmh, HMENU hmenu, int ihmenu, bool fSysMenu)
{
	// If this menu is an expanded menu, update m_emLastExp.
	int cmenuExp = m_vemMenus.Size();
	for (int imenuExp = 0; imenuExp < cmenuExp; imenuExp++)
	{
		if (m_vemMenus[imenuExp].m_hmenu == hmenu)
		{
			m_emLastExp = m_vemMenus[imenuExp];
			break;
		}
	}

	if (!::IsMenu(hmenu))
		return;

	CmdState cms;
	CmdExecPtr qcex;
	if (AfApp::Papp())
		qcex = AfApp::GetCmdExec();
	CmdHandlerPtr qcmh = pcmh; // Keep a ref count.
	int cmni = ::GetMenuItemCount(hmenu);
	int imni;
	uint grfces;

	for (imni = 0; imni < cmni; imni++)
	{
		// Constructor sets up dwTypeData to point to a string buffer.
		MenuItemInfo mii;

		mii.fMask = MIIM_DATA | MIIM_ID | MIIM_TYPE | MIIM_STATE | MIIM_SUBMENU;
		::GetMenuItemInfo(hmenu, imni, true, &mii);

		if (mii.fType & MFT_SEPARATOR || mii.hSubMenu)
			continue;

		MenuItemData * pmid = MenuItemData::GetData(mii.dwItemData);

		if (mii.wID >= kcidMenuItemDynMin && mii.wID < kcidMenuItemDynLim)
		{
			// Use the ID of the dummy item that was expanded.
			cms.Init(m_emLastExp.m_idDummy, pcmh,
				pmid ? pmid->m_strTxt.Chars() : mii.dwTypeData);
			cms.m_iitemExp = mii.wID - kcidMenuItemDynMin;
		}
		else
		{
			cms.Init(mii.wID, pcmh, pmid ? pmid->m_strTxt.Chars() : mii.dwTypeData);
		}

		if (qcex)
			qcex->FSetCmdState(cms);
		grfces = cms.Grfces();

		if (!grfces)
		{
			// Don't change the item at all.
			continue;
		}

		mii.fMask = MIIM_STATE;

		if (grfces & kfcesEnable)
			mii.fState &= ~MFS_GRAYED;
		else if (grfces & kfcesDisable)
			mii.fState |= MFS_GRAYED;

		if (grfces & kfcesCheck)
			mii.fState |= MFS_CHECKED;
		else if (grfces & kfcesBullet)
		{
			mii.fState |= MFS_CHECKED;
		}
		else if (grfces & kfcesUncheck)
			mii.fState &= ~MFS_CHECKED;

		if (grfces & kfcesText)
		{
			if (pmid)
			{
				pmid->m_strTxt = cms.Text();
				if (grfces == kfcesText)
				{
					// Don't call SetMenuItemInfo in this case.
					continue;
				}
			}
			else
			{
				mii.fMask |= MIIM_TYPE;
				mii.dwTypeData = const_cast<achar *>(cms.Text());
				mii.cch = StrLen(mii.dwTypeData);
			}
		}

		::SetMenuItemInfo(hmenu, imni, true, &mii);
	}
}


/*----------------------------------------------------------------------------------------------
	Some system settings have changed. Nuke the menu font, rebuild the image list and nuke
	the check mark bitmap.
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::Refresh(void)
{
	if (m_fIgnoreRefresh)
		return;

	if (m_hfontMenu)
	{
		AfGdi::DeleteObjectFont(m_hfontMenu);
		m_hfontMenu = NULL;
	}
	if (m_hbmpCheck)
	{
		AfGdi::DeleteObjectBitmap(m_hbmpCheck);
		m_hbmpCheck = NULL;
	}

	int imagLim = m_himl ? ImageList_GetImageCount(m_himl) : 0;
	if (!imagLim)
		return;

	int irid;
	int rid;
	HIMAGELIST himl = m_himl;
	m_himl = NULL;

	try
	{
		for (irid = 0; irid < m_vridTlbr.Size(); irid++)
		{
			rid = m_vridTlbr[irid];
			LoadImages(rid);
		}
		int imag = m_himl ? ImageList_GetImageCount(m_himl) : 0;

		if (imag != imagLim)
		{
			Warn("Refreshing the image list produced a different number of images!");
			if (m_himl)
				AfGdi::ImageList_Destroy(m_himl);
			m_himl = himl;
		}
		else if (himl)
			AfGdi::ImageList_Destroy(himl);
	}
	catch (...)
	{
		Warn("Refreshing the image list failed!");
		if (m_himl)
			AfGdi::ImageList_Destroy(m_himl);
		m_himl = himl;
		return;
	}

	// Free all cached icons.
	int icti;

	for (icti = 0; icti < m_vcti.Size(); icti++)
	{
		if (m_vcti[icti].m_hicon)
		{
			::DestroyIcon(m_vcti[icti].m_hicon);
			m_vcti[icti].m_hicon = NULL;
		}
	}

	// Make sure the toolbars are using the new imagelist.
	if (m_pafwFrame)
		m_pafwFrame->RefreshToolbars();

	// Ignore future Refresh calls until we're told to resume honoring them.
	m_fIgnoreRefresh = true;
}


/*----------------------------------------------------------------------------------------------
	This method returns the handle of the last menu that was expanded, along with the original
	ID of the dummy item that was deleted when the new items were inserted.
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::GetLastExpMenuInfo(HMENU * phmenu, int * pidDummy)
{
	AssertPtr(phmenu);
	AssertPtr(pidDummy);

	*phmenu = m_emLastExp.m_hmenu;
	*pidDummy = m_emLastExp.m_idDummy;
}


/*----------------------------------------------------------------------------------------------
	When a menu changes, save the last expansion information, if there is one.
	@param hmenu Handle to the menu that is currently active.
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::SaveActiveMenu(HMENU hmenu)
{
	// See if we have a corresponding expanded menu.
	for (int imenuExp = m_vemMenus.Size(); --imenuExp >= 0; )
	{
		if (m_vemMenus[imenuExp].m_hmenu == hmenu)
			m_emLastExp = m_vemMenus[imenuExp]; // Found one, so update the saved value.
	}
}


/*----------------------------------------------------------------------------------------------
	Sets the icon that shows when a menu item with an ID of cidNew is visible to the icon that
	shows when a menu item with an ID of cidOriginal is visible. This returns true if it was
	successful.
----------------------------------------------------------------------------------------------*/
bool AfMenuMgr::UseIdenticalIcon(int cidOriginal, int cidNew)
{
	Assert(cidOriginal != cidNew);
	int ictiNew;
	if (FFindCidToImag(cidNew, &ictiNew))
		return true;
	int icti;
	if (!FFindCidToImag(cidOriginal, &icti))
		return false;

	CidToImag cti;
	cti.m_cid = cidNew;
	cti.m_imag = m_vcti[icti].m_imag;
	m_vcti.Insert(ictiNew, cti);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Load toolbars given an array of resource ids.
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::LoadToolBars(const int * prgrid, int crid)
{
	AssertObj(this);
	AssertArray(prgrid, crid);

	int irid;

	for (irid = 0; irid < crid; irid++)
		LoadToolBar(prgrid[irid]);
}


/*----------------------------------------------------------------------------------------------
	Load a single toolbar given its resource id.
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::LoadToolBar(int rid)
{
	AssertObj(this);

	HINSTANCE hinst = ModuleEntry::GetModuleHandle();
	HRSRC hrsrc;
	ToolBarData * ptbd;

	// Load toolbar resource.
	if (NULL == (hrsrc = ::FindResource(hinst, MAKEINTRESOURCE(rid), RT_TOOLBAR)) ||
		NULL == (ptbd = (ToolBarData *)LoadResource(hinst, hrsrc)))
	{
		Warn("Can't load toolbar!");
		ThrowHr(WarnHr(E_FAIL));
	}
	Assert(ptbd->suVer == 1);

	if (!m_dxsBmp)
	{
		Assert(!m_dysBmp);
		m_dxsBmp = ptbd->dxsBmp;
		m_dysBmp = ptbd->dysBmp;
		Assert(m_dxsBmp > 0);
		Assert(m_dysBmp > 0);
		m_dxsBtn = m_dxsBmp + 2 * kdxsBtnMarg;
		m_dysBtn = m_dysBmp + 2 * kdysBtnMarg;
	}
	else
	{
		Assert(m_dxsBmp > 0);
		Assert(m_dysBmp > 0);
		if (m_dxsBmp != ptbd->dxsBmp || m_dysBmp != ptbd->dysBmp)
		{
			Warn("ToolBar button size wrong!");
			ThrowHr(WarnHr(E_FAIL));
		}
	}

	int imag = m_himl ? ImageList_GetImageCount(m_himl) : 0;

	LoadImages(rid);

	int imagLim = ImageList_GetImageCount(m_himl);
	int icid;
	int cid;
	int icti;
	CidToImag cti;

	for (icid = 0; icid < ptbd->ccid; icid++)
	{
		cid = ptbd->rgcid[icid];
		if (cid > 0)
		{
			if (imag >= imagLim)
				AssertMsg(false, "Not enough images in the image list.");
			else if (!FFindCidToImag(cid, &icti))
			{
				cti.m_cid = cid;
				cti.m_imag = imag;
				m_vcti.Insert(icti, cti);
			}
			imag++;
		}
	}

	m_vridTlbr.Push(rid);
}


/*----------------------------------------------------------------------------------------------
	Load the images for the toolbar.
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::LoadImages(int rid)
{
	AssertObj(this);

	Assert(m_dxsBmp > 0);
	Assert(m_dysBmp > 0);

	if (!m_himl)
	{
		m_himl = AfGdi::ImageList_Create(m_dxsBmp, m_dysBmp, ILC_COLORDDB | ILC_MASK, 10, 10);
		if (!m_himl)
		{
			Warn("Creating the image list failed!");
			ThrowHr(WarnHr(E_FAIL));
		}
	}

	HBITMAP hbmpTlbr = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(), MAKEINTRESOURCE(rid));
	if (!hbmpTlbr)
	{
		Warn("Can't load bitmap for toolbar!");
		ThrowHr(WarnHr(E_FAIL));
	}

	// Add bitmaps to the image list and each command id to the map.
	int imag = m_himl ? ImageList_GetImageCount(m_himl) : 0;

	// WARNING: Do not change the color in the next line.
	int imagRet = ImageList_AddMasked(m_himl, hbmpTlbr, kclrPink);

	AfGdi::DeleteObjectBitmap(hbmpTlbr);
	hbmpTlbr = NULL;

	if (imagRet != imag)
	{
		// Something went wrong here.
		if (imagRet < 0)
		{
			Warn("Adding to the image list failed!");
			ThrowHr(WarnHr(E_FAIL));
		}
		AssertMsg(false, "Why is imag != imagRet?");
		ThrowHr(WarnHr(E_FAIL));
	}
}


/*----------------------------------------------------------------------------------------------
	Handle dropping down the menu.
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::OnInitMenuPopup(HMENU hmenu, int imnu)
{
	AssertObj(this);
	ConvertMenu(hmenu, imnu, true);
}


/*----------------------------------------------------------------------------------------------
	This method gets called before a menu is shown. It goes through each menu item and looks to
	see if it is within the range of expandable menu items. If it is, it then uses our command
	routing mechanism to insert the expanded items in place of the dummy item.
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::ExpandMenuItems(HMENU hmenu, int imnu)
{
	AssertObj(this);
	Assert(hmenu);

	if (!::IsMenu(hmenu))
		return;

	// FixMenu is used to modify the menu for this App if needed.
	if (AfApp::Papp())
	{
		AfMainWnd * pafw = AfApp::Papp()->GetCurMainWnd();
		if (pafw)
			pafw->FixMenu(hmenu);
	}
	int cmni = ::GetMenuItemCount(hmenu);
	int imni;

	for (imni = 0; imni < cmni; imni++)
	{
		// Constructor sets up dwTypeData to point to a string buffer.
		MenuItemInfo mii;

		mii.fMask = MIIM_DATA | MIIM_ID | MIIM_TYPE;
		::GetMenuItemInfo(hmenu, imni, true, &mii);

		if (mii.wID >= (uint)kcidMenuItemExpMin && mii.wID < (uint)kcidMenuItemExpLim)
		{
			// Call the handler that will modify the menu.
			CmdPtr qcmd;
			qcmd.Attach(NewObj Cmd);
			qcmd->m_cid = mii.wID;
			qcmd->m_rgn[0] = kmaExpandItem;
			qcmd->m_rgn[1] = (int)hmenu;
			qcmd->m_rgn[2] = imni + 1;
			qcmd->m_rgn[3] = -1;
			// We need to give it a pointer to the window, or the command handler can't tell
			// which window has the popup menu.
			// If no window, hope that we don't need it!
			if (m_pafwFrame)
			{
				qcmd->m_qcmh = m_pafwFrame->GetContextInfo();
				if (AfApp::Papp()->FDispatchCmd(qcmd))
				{
					Assert(qcmd->m_rgn[3] != -1);

					// Remove the temporary place-holder menu item.
					::DeleteMenu(hmenu, imni, MF_BYPOSITION);

					m_emLastExp.m_hmenu = hmenu;
					m_emLastExp.m_cmniAdded = qcmd->m_rgn[3];
					m_emLastExp.m_imni = imni;
					m_emLastExp.m_idDummy = mii.wID;
					m_vemMenus.Push(m_emLastExp);

					// Make sure the new menu items get expanded as well.
					cmni = ::GetMenuItemCount(hmenu);
					imni--;
					continue;
				}
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	This method allows a class to set up a handler for any menu items that are within the range
	kcidMenuItemDynMin - kcidMenuItemDynLim. Currently it only allows one handler at a time.
	As soon as another window calls this method or a menu is encountered with expandable items,
	the handler will be lost.
	This method should only be used for a case when an unknown number of menu items (< 2000)
	need to be added for a temporary popup menu.
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::SetMenuHandler(int cid)
{
	Assert(m_vemMenus.Size() == 0);
	m_emLastExp.m_idDummy = cid;
}


/*----------------------------------------------------------------------------------------------
	Convert the menu items to or from owner draw.

	NOTE:  one place where is is used is in the possibility choose dialog when the modify
			button is pressed.  This code generates the menu when the modify button is pressed.

	@param hmenu
	@param imnu
	@param fShowBtns
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::ConvertMenu(HMENU hmenu, int imnu, bool fShowBtns)
{
	AssertObj(this);
	Assert(hmenu);

	if (::IsMenu(hmenu))
	{
		int cmni = ::GetMenuItemCount(hmenu);
		int imni;

		for (imni = 0; imni < cmni; imni++)
		{
			// Constructor sets up dwTypeData to point to a string buffer.
			MenuItemInfo mii;

			mii.fMask = MIIM_DATA | MIIM_ID | MIIM_TYPE | MIIM_SUBMENU;
			::GetMenuItemInfo(hmenu, imni, true, &mii);

			MenuItemData * pmid = NULL;

			if (mii.dwItemData)
			{
				pmid = MenuItemData::GetData(mii.dwItemData);
				if (NULL == pmid)
				{
					// Some one else is using the dwItemData.
					Warn("Couldn't take over item");
					continue;
				}
			}

			mii.fMask = 0;

			if (fShowBtns)
			{
				// Convert to owner draw.
				if (!pmid)
				{
					pmid = NewObj MenuItemData;
					pmid->m_strTxt = mii.m_sz;
					pmid->m_uType = mii.fType;
					pmid->m_fSubMenu = mii.hSubMenu != NULL;

					if (!(mii.fType & MFT_SEPARATOR) && pmid->m_strTxt.FindCh('\t') < 0)
						FFindAccelKeyName(mii.wID, pmid->m_strAccel);

					mii.dwItemData = (ulong)pmid;
					mii.fMask |= MIIM_DATA;
				}
				if (!(mii.fType & MFT_OWNERDRAW))
				{
					mii.fType |= MFT_OWNERDRAW;
					mii.fMask |= MIIM_TYPE;
				}
				if (mii.fMask)
					::SetMenuItemInfo(hmenu, imni, true, &mii);
			}
			else if (pmid)
			{
				// Convert back.
				if (mii.fType != pmid->m_uType)
				{
					mii.fType = pmid->m_uType;
					mii.fMask |= MIIM_TYPE;
					mii.dwTypeData = const_cast<achar *>(pmid->m_strTxt.Chars());
					mii.cch = pmid->m_strTxt.Length();
				}
				mii.dwItemData = 0;
				mii.fMask |= MIIM_DATA;
				if (mii.fMask)
					::SetMenuItemInfo(hmenu, imni, true, &mii);

				delete pmid;
			}
		}
	}

	int ihmenu;

	if (fShowBtns && !FFindHmenu(hmenu, &ihmenu))
		m_vhmenu.Insert(ihmenu, hmenu);
}


/*----------------------------------------------------------------------------------------------
	Handle the menu characters.
----------------------------------------------------------------------------------------------*/
long AfMenuMgr::OnMenuChar(achar ch, HMENU hmenu)
{
	MenuItemData * pmid;
	int imni;
	int cmni = ::GetMenuItemCount(hmenu);
	int imniSel = -1;
	int imniRet = -1;
	bool fGray = false;
	bool fMulti = false;

	ch = ::ToUpper(ch);

	for (imni = 0; imni < cmni; imni++)
	{
		// Constructor sets up dwItemData to point to a string buffer.
		MenuItemInfo mii;

		mii.fMask = MIIM_DATA | MIIM_TYPE | MIIM_STATE;
		::GetMenuItemInfo(hmenu, imni, true, &mii);

		if (!(mii.fType & MFT_OWNERDRAW) ||
			NULL == (pmid = MenuItemData::GetData(mii.dwItemData)))
		{
			continue;
		}

		if (mii.fState & MFS_HILITE)
			imniSel = imni;

		StrApp & str = pmid->m_strTxt;
		int ich = str.FindCh('&');

		if (ich < 0 || ::ToUpper(str[ich + 1]) != ch)
			continue;

		if (imniRet < 0)
		{
			Assert(!fMulti);
			imniRet = imni;
			fGray = (mii.fState & MFS_GRAYED) != 0;
			continue;
		}
		if (mii.fState & MFS_GRAYED)
			continue;
		if (fGray)
		{
			// Pretend the first one doesn't exist, since it is grayed.
			imniRet = imni;
			fGray = false;
		}
		else
		{
			fMulti = true;
			if (imniRet <= imniSel && imniSel < imni)
				imniRet = imni;
		}
	}

	if (imniRet < 0)
		return 0;

	if (!fMulti)
		return MAKELONG(imniRet, MNC_EXECUTE);

	return MAKELONG(imniRet, MNC_SELECT);
}


/*----------------------------------------------------------------------------------------------
	Converts all menus back to non-ownerdraw. It also replaces dummy menu items that were
	replaced with an expanded list, after it deletes the expanded menu items.
	This is called when the entire menu is closed--not when a cascaded menu closes.
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::OnMenuClose(void)
{
	AssertObj(this);

	HMENU hmenu;
	while (m_vhmenu.Pop(&hmenu))
		ConvertMenu(hmenu, 0, false);

	ExpandedMenu em;
	while (m_vemMenus.Pop(&em))
	{
		// Delete the inserted items from the menu.
		int imniMin = em.m_imni;
		int imniLim = imniMin + em.m_cmniAdded;
		for (int i = imniLim; --i >= imniMin; ) // Delete the items backwards.
			::DeleteMenu(em.m_hmenu, i, MF_BYPOSITION);

		// Insert the dummy item again.
		::InsertMenu(em.m_hmenu, imniMin, MF_BYPOSITION, em.m_idDummy, _T(""));
	}
}


/*----------------------------------------------------------------------------------------------
	Get the menu font. This returns m_hfontMenu, creating it if it's currently NULL.
----------------------------------------------------------------------------------------------*/
HFONT AfMenuMgr::GetMenuFont(void)
{
	AssertObj(this);

	if (!m_hfontMenu)
	{
		NONCLIENTMETRICS ncm;
		ncm.cbSize = isizeof(ncm);

		SystemParametersInfo(SPI_GETNONCLIENTMETRICS, isizeof(ncm), &ncm, 0);
		m_hfontMenu = AfGdi::CreateFontIndirect(&ncm.lfMenuFont);
		if (!m_hfontMenu)
		{
			Warn("Couldn't create menu font");
			ThrowHr(WarnHr(E_FAIL));
		}
	}

	return m_hfontMenu;
}


/*----------------------------------------------------------------------------------------------
	Called by window procs.
----------------------------------------------------------------------------------------------*/
bool AfMenuMgr::OnMeasureItem(MEASUREITEMSTRUCT * pmis)
{
	AssertPtr(pmis);

	MenuItemData * pmid;

	if (pmis->CtlType != ODT_MENU || NULL == (pmid = MenuItemData::GetData(pmis->itemData)))
		return false;

	if (pmid->m_uType & MFT_SEPARATOR)
	{
		// Separator: use half system height and zero width.
		pmis->itemHeight = ::GetSystemMetrics(SM_CYMENU) >> 1;
		pmis->itemWidth = 0;
		return true;
	}

	// Compute size of text: use DrawText with DT_CALCRECT.
	Rect rcText;
	int dxs;
	int dys = ::GetSystemMetrics(SM_CYMENU);

	// Get the screen DC, but don't draw on it!
	HDC hdc = ::GetDC(NULL);

	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, GetMenuFont());
	Assert(hfontOld);

	rcText.Clear();
	::DrawText(hdc, pmid->m_strTxt.Chars(), pmid->m_strTxt.Length(), &rcText,
		kgrfdtDef | DT_CALCRECT);

	dys = ::Max(dys, rcText.Height());
	dxs = rcText.Width();

	if (pmid->m_strAccel.Length())
	{
		Assert(pmid->m_strTxt.FindCh('\t') < 0);

		// Measure the accelerator text.
		rcText.Clear();
		::DrawText(hdc, pmid->m_strAccel.Chars(), pmid->m_strAccel.Length(), &rcText,
			kgrfdtDef | DT_CALCRECT);
		dys = ::Max(dys, rcText.Height());
		dxs += rcText.Width();

		// Measure the tab character (for consistency).
		rcText.Clear();
		::DrawText(hdc, _T("\t"), 1, &rcText, kgrfdtDef | DT_CALCRECT);
		dys = ::Max(dys, rcText.Height());
		dxs += rcText.Width();
	}

	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	int iSuccess;
	iSuccess = ::ReleaseDC(NULL, hdc);

	dxs += (kdxsTxtMarg + kdxsMarg) * 2;
	dxs += kdxsGap;
	dxs += m_dxsBtn * 2;

	// Whatever width we return Windows will add the width of a menu checkmark, so we
	// must subtract it here.
	dxs -= ::GetSystemMetrics(SM_CXMENUCHECK) - 1;
	pmis->itemWidth = dxs;
	pmis->itemHeight = dys;

	return true;
}


/*----------------------------------------------------------------------------------------------
	Draw the menu item.
----------------------------------------------------------------------------------------------*/
bool AfMenuMgr::OnDrawItem(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);

	MenuItemData * pmid;

	if (pdis->CtlType != ODT_MENU || NULL == (pmid = MenuItemData::GetData(pdis->itemData)))
		return false;

	Assert(pdis->itemAction != ODA_FOCUS);
	Assert(pdis->hDC);

	Rect rcT;
	HDC hdc = pdis->hDC;
	Rect rcItem = pdis->rcItem;
	SmartPalette spal(hdc);

	rcItem.Inflate(-kdxsMarg, 0);

	if (pmid->m_uType & MFT_SEPARATOR)
	{
		// Draw separator.
		rcT = rcItem;
		rcT.top += rcT.Height() >> 1;
		::DrawEdge(hdc, &rcT, EDGE_ETCHED, BF_TOP);
		return true;
	}

	// Not a separator.
	bool fDisabled = (pdis->itemState & ODS_GRAYED) != 0;
	bool fSelected = (pdis->itemState & ODS_SELECTED) != 0;
	bool fChecked  = (pdis->itemState & ODS_CHECKED) != 0;
	bool fHaveBtn = false;

	COLORREF clrMenu = ::GetSysColor(COLOR_MENU);
	COLORREF clrBackBtn = clrMenu;

	if (fChecked && !fSelected)
	{
		clrBackBtn = ::GetSysColor(COLOR_3DLIGHT);
		if (clrBackBtn == clrMenu)
			clrBackBtn = ::GetSysColor(COLOR_3DHIGHLIGHT);
	}

	// Paint button.
	int ysTop = rcItem.top + (rcItem.Height() - m_dysBtn) / 2;
	Rect rcBtn(rcItem.left, ysTop, rcItem.left + m_dxsBtn, ysTop + m_dysBtn);

	int icti;

	if (!pmid->m_fSubMenu)
	{
		if (FFindCidToImag(pdis->itemID, &icti))
		{
			// This item has a button!
			CidToImag & cti = m_vcti[icti];

			fHaveBtn = true;

			// Compute point to start drawing.
			int xsBmp = rcBtn.left + (m_dxsBtn - m_dxsBmp) / 2;
			int ysBmp = rcBtn.top + (m_dysBtn - m_dysBmp) / 2;

			// Draw disabled or normal.
			if (!fDisabled)
			{
				// Normal: fill background depending on state.
				AfGfx::FillSolidRect(hdc, rcBtn, clrBackBtn);

				// Draw pushed-in or popped-out edge.
				if (fChecked || fSelected)
					::DrawEdge(hdc, &rcBtn, fChecked ? BDR_SUNKENOUTER : BDR_RAISEDINNER, BF_RECT);

				// Draw the button!
				::ImageList_Draw(m_himl, cti.m_imag, hdc, xsBmp, ysBmp, ILD_TRANSPARENT);
			}
			else
			{
				HICON hicon = cti.GetIcon(m_himl);
				if (hicon)
				{
					::DrawState(hdc, NULL, NULL, (LPARAM)hicon, 0, xsBmp, ysBmp, 0, 0,
						DST_ICON | DSS_DISABLED);
				}
			}
		}
		else if (fChecked || pmid->m_hbmpUnchecked)
		{
			AfGfx::FillSolidRect(hdc, rcBtn, clrBackBtn);
			DrawCheckmark(hdc, rcBtn, clrBackBtn,
				fChecked ? pmid->m_hbmpChecked : pmid->m_hbmpUnchecked);
			if (fChecked)
				::DrawEdge(hdc, &rcBtn, BDR_SUNKENOUTER, BF_RECT);
			fHaveBtn = true;
		}
	}

	// Done with button, now paint text. First do background if needed.
	COLORREF clrBack = ::GetSysColor(fSelected ? COLOR_HIGHLIGHT : COLOR_MENU);

	if (fSelected || pdis->itemAction == ODA_SELECT)
	{
		// Selected or selection state changed: paint text background.
		rcT = rcItem;
		if (fHaveBtn)
			rcT.left += m_dxsBtn + kdxsGap;
		AfGfx::FillSolidRect(hdc, rcT, clrBack);
	}

	// Compute text rectangle and colors.
	Rect rcText = rcItem;
	rcText.left += m_dxsBtn + kdxsGap + kdxsTxtMarg;
	rcText.right -= m_dxsBtn;
	::SetBkMode(hdc, TRANSPARENT);
	COLORREF clrText = ::GetSysColor(fDisabled ? COLOR_GRAYTEXT :
		fSelected ? COLOR_HIGHLIGHTTEXT : COLOR_MENUTEXT);

	// Now paint menu item text. No need to select font, because Windows sets it up before
	// sending WM_DRAWITEM.
	if (fDisabled && (!fSelected || clrText == clrBack))
	{
		// Disabled: draw hilite text shifted southeast 1 pixel for embossed
		// look. Don't do it if item is selected, though--unless text color same
		// as menu highlight color.
		rcT = rcText + Point(1, 1);
		DrawMenuText(hdc, rcT, pmid, GetSysColor(COLOR_3DHILIGHT));
	}
	DrawMenuText(hdc, rcText, pmid, clrText);

	//  Set timer to restore the status bar information if we've left the menu.
	if (!fSelected && pdis->itemAction == ODA_SELECT)
	{
//		AssertPtr(m_pafwFrame);
		if (m_pafwFrame)
			::SetTimer(m_pafwFrame->Hwnd(), AfStatusBar::knMenuTimer, 100, NULL);
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Draw the main menu text and the accelerator information. If the main menu text contains
	a tab it is assumed that the accelerator information follows the tab. Otherwise
	pmid->m_strAccel is used for the accelerator information.
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::DrawMenuText(HDC hdc, Rect & rc, const MenuItemData * pmid, COLORREF clr)
{
	AssertPtr(pmid);

	int ich;
	StrApp str = pmid->m_strTxt;

	// Look for a tab character.
	ich = str.FindCh('\t');
	if (ich < 0)
		ich = str.Length();

	::SetTextColor(hdc, clr);
	::DrawText(hdc, str.Chars(), ich, &rc, kgrfdtDef);

	// Draw the accelerator text.
	if (ich == str.Length())
	{
		// Accelerator is not embedded in the main text.
		str = pmid->m_strAccel;
		ich = 0;
	}
	else
	{
		// Accelerator is embedded in the main text.
		Assert(pmid->m_strAccel.Length() == 0);
		ich++;
	}

	if (ich < str.Length())
		::DrawText(hdc, str.Chars() + ich, str.Length() - ich, &rc, kgrfdtDef | DT_RIGHT);
}


#ifndef OBM_CHECK
#define OBM_CHECK 32760 // from winuser.h
#endif


/*----------------------------------------------------------------------------------------------
	Draw a check mark bitmap and frame it with the inset.
	TODO ShonK: Make this match VS?
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::DrawCheckmark(HDC hdc, const Rect & rc, COLORREF clrBack, HBITMAP hbmp)
{
	// Get checkmark bitmap if none, use Windows standard.
	if (!hbmp)
	{
		if (!m_hbmpCheck)
		{
			m_hbmpCheck = AfGdi::LoadBitmap(NULL, MAKEINTRESOURCE(OBM_CHECK));
			if (!m_hbmpCheck)
			{
				AssertMsg(false, "Couldn't load check mark bitmap");
				return;
			}
		}
		hbmp = m_hbmpCheck;
	}

	// Center bitmap in caller's rectangle
	BITMAP bmp;
	::GetObject(hbmp, isizeof(bmp), &bmp);

	Rect rcDst(0, 0, bmp.bmWidth, bmp.bmHeight);

	rcDst.Center(rc);

	HDC hdcMem = AfGdi::CreateCompatibleDC(hdc);
	if (!hdcMem)
	{
		Warn("CreateCompatibleDC failed");
		return;
	}

	HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
	COLORREF clrOld = ::SetBkColor(hdc, clrBack);

	::BitBlt(hdc,
		Max<int>(0, rcDst.left), Max<int>(0, rcDst.top),
		Min<int>(rcDst.Width(), rcDst.right), Min<int>(rcDst.Height(), rcDst.bottom),
		hdcMem,
		Min<int>(0, rcDst.left), Min<int>(0, rcDst.top), SRCCOPY);
	::SetBkColor(hdc, clrOld);
	AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);

	BOOL fSuccess;
	fSuccess = AfGdi::DeleteDC(hdcMem);
	Assert(fSuccess);
}


/*----------------------------------------------------------------------------------------------
	Find the CidToImag item for this command.
----------------------------------------------------------------------------------------------*/
bool AfMenuMgr::FFindCidToImag(int cid, int * picti)
{
	AssertPtr(picti);

	int ivMin, ivLim;

	for (ivMin = 0, ivLim = m_vcti.Size(); ivMin < ivLim; )
	{
		int ivMid = (ivMin + ivLim) / 2;
		if (m_vcti[ivMid].m_cid < cid)
			ivMin = ivMid + 1;
		else
			ivLim = ivMid;
	}

	*picti = ivMin;
	return ivMin < m_vcti.Size() && m_vcti[ivMin].m_cid == cid;
}


/*----------------------------------------------------------------------------------------------
	Return the index of the bitmap used by the given command ID.
----------------------------------------------------------------------------------------------*/
int AfMenuMgr::GetImagFromCid(int cid)
{
	int icti;
	if (!FFindCidToImag(cid, &icti))
		return -1;
	return m_vcti[icti].m_imag;
}


/*----------------------------------------------------------------------------------------------
	Look for the hmenu in m_vhmenu.
----------------------------------------------------------------------------------------------*/
bool AfMenuMgr::FFindHmenu(HMENU hmenu, int * pihmenu)
{
	AssertPtr(pihmenu);

	int ivMin, ivLim;

	for (ivMin = 0, ivLim = m_vhmenu.Size(); ivMin < ivLim; )
	{
		int ivMid = (ivMin + ivLim) / 2;
		if (m_vhmenu[ivMid] < hmenu)
			ivMin = ivMid + 1;
		else
			ivLim = ivMid;
	}

	*pihmenu = ivMin;
	return ivMin < m_vhmenu.Size() && m_vhmenu[ivMin] == hmenu;
}


/*----------------------------------------------------------------------------------------------
	Get the icon for the image.
----------------------------------------------------------------------------------------------*/
HICON CidToImag::GetIcon(HIMAGELIST himl)
{
	if (!m_hicon)
		m_hicon = ::ImageList_ExtractIcon(0, himl, m_imag);
	return m_hicon;
}


/***********************************************************************************************
	Accelerator key handling.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Add a new accelerator key table to the list. Returns an id (atid) which can be used in
	future calls (e.g., to RemoveAccelTable or SetAccelHandle.)
----------------------------------------------------------------------------------------------*/
int AfMenuMgr::AddAccelTable(HACCEL hact, int apl, HWND hwnd)
{
	int iati, ivLim;

	for (iati = 0, ivLim = m_vati.Size(); iati < ivLim; )
	{
		int ivMid = (iati + ivLim) / 2;
		if (m_vati[ivMid].m_apl < apl)
			iati = ivMid + 1;
		else
			ivLim = ivMid;
	}

	m_vati.Insert(iati, AccelTableInfo());

	AccelTableInfo & ati = m_vati[iati];
	ati.m_hact = hact;
	ati.m_apl = apl;
	ati.m_hwnd = hwnd;
	ati.m_atid = m_atidNext++;

	int cacc = CopyAcceleratorTable(hact, NULL, 0);

	if (!cacc)
		return ati.m_atid;

	int iaki;
	int iacc;
	Vector<ACCEL> vacc;
	AccelKeyInfo aki;

	vacc.Resize(cacc);
	CopyAcceleratorTable(hact, vacc.Begin(), cacc);
	ati.m_vaki.EnsureSpace(cacc);

	for (iacc = 0; iacc < cacc; iacc++)
	{
		*static_cast<ACCEL *>(&aki) = vacc[iacc];
		if (!ati.FFindAccelKeyInfo(aki.cmd, &iaki))
			ati.m_vaki.Insert(iaki, aki);
	}

	return ati.m_atid;
}


/*----------------------------------------------------------------------------------------------
	Load a new accelerator handle from a resource.
----------------------------------------------------------------------------------------------*/
int AfMenuMgr::LoadAccelTable(int ridAccel, int apl, HWND hwnd)
{
	HACCEL hact = ::LoadAccelerators(ModuleEntry::GetModuleHandle(), MAKEINTRESOURCE(ridAccel));
	if (!hact)
		ThrowHr(WarnHr(E_FAIL));
	return AddAccelTable(hact, apl, hwnd);
}


/*----------------------------------------------------------------------------------------------
	Remove an accelerator table. (Note: if it will be wanted again, consider instead disabling
	with SetAccelHwnd(atid, NULL)).
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::RemoveAccelTable(int atid)
{
	int iati;

	for (iati = m_vati.Size(); --iati >= 0; )
	{
		if (m_vati[iati].m_atid == atid)
		{
			m_vati.Delete(iati);
			return;
		}
	}
	Warn("Accelerator table not found");
}


/*----------------------------------------------------------------------------------------------
	Specifies which hwnd the commands should be dispatched to. If hwnd is set to NULL, it
	disables the accelerator table.
----------------------------------------------------------------------------------------------*/
void AfMenuMgr::SetAccelHwnd(int atid, HWND hwnd)
{
	int iati;

	for (iati = m_vati.Size(); --iati >= 0; )
	{
		if (m_vati[iati].m_atid == atid)
		{
			m_vati[iati].m_hwnd = hwnd;
			return;
		}
	}
	Warn("Accelerator table not found");
}


/*----------------------------------------------------------------------------------------------
	Translate accelerators. Return true iff the message should be dispatched.
----------------------------------------------------------------------------------------------*/
bool AfMenuMgr::FTransAccel(MSG * pmsg)
{
	int iati;

	for (iati = 0; iati < m_vati.Size(); iati++)
	{
		// If m_hwnd is NULL the accelerator table is disabled.
		if (m_vati[iati].m_hwnd &&
			::TranslateAccelerator(m_vati[iati].m_hwnd, m_vati[iati].m_hact, pmsg))
		{
			return false;
		}
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Find the first active accelerator for the given command id and return its string
	equivalent, eg, "Ctrl+Z".
----------------------------------------------------------------------------------------------*/
bool AfMenuMgr::FFindAccelKeyName(int cid, StrApp & str)
{
	int iati, iaki;

	for (iati = 0; iati < m_vati.Size(); iati++)
	{
		// If m_hwnd is NULL the accelerator table is disabled.
		if (m_vati[iati].m_hwnd && m_vati[iati].FFindAccelKeyInfo(cid, &iaki))
		{
			m_vati[iati].m_vaki[iaki].GetName(str);
			return true;
		}
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Look for the given cid in the accelerator tables key info.
----------------------------------------------------------------------------------------------*/
bool AccelTableInfo::FFindAccelKeyInfo(int cid, int * piaki)
{
	AssertPtr(piaki);

	int ivMin, ivLim;

	for (ivMin = 0, ivLim = m_vaki.Size(); ivMin < ivLim; )
	{
		int ivMid = (ivMin + ivLim) / 2;
		if (m_vaki[ivMid].cmd < cid)
			ivMin = ivMid + 1;
		else
			ivLim = ivMid;
	}

	*piaki = ivMin;
	return ivMin < m_vaki.Size() && m_vaki[ivMin].cmd == cid;
}


/*----------------------------------------------------------------------------------------------
	Get the name of the accelerator key.
----------------------------------------------------------------------------------------------*/
void AccelKeyInfo::GetName(StrApp & str)
{
	achar sz[kcchMaxMenu + 1];

	if (!m_strKey.Length())
	{
		if (fVirt & FCONTROL)
		{
			StrApp str(kstidKeyCtrl);
			m_strKey.Append(str);
		}
		if (fVirt & FALT)
		{
			StrApp str(kstidKeyAlt);
			m_strKey.Append(str);
		}
		if (fVirt & FSHIFT)
		{
			StrApp str(kstidKeyShift);
			m_strKey.Append(str);
		}
		if (fVirt & FVIRTKEY)
		{
			if (key == VK_DELETE)
			{
				StrApp str(kstidKeyDelete);
				_tcscpy_s(sz, str.Chars());
			}
			else
			{
				::GetKeyNameText(::MapVirtualKey(key, 0) << 16, sz, kcchMaxMenu);
				sz[kcchMaxMenu] = 0;
			}
		}
		else
		{
			sz[0] = (achar)key;
			sz[1] = 0;
		}
		m_strKey += sz;
	}

	str = m_strKey;
}

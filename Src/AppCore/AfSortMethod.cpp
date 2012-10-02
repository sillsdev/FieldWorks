/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfSortMethod.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Implementation of the Sort Method tab in the Tools/Options dialog.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

BEGIN_CMD_MAP(TlsOptDlgSort)
	ON_CID_CHILD(kcidSortByFirstPopupMenu, &TlsOptDlgSort::CmdSortByFirstPopup, NULL)
	ON_CID_CHILD(kcidSortBySecondPopupMenu, &TlsOptDlgSort::CmdSortBySecondPopup, NULL)
	ON_CID_CHILD(kcidSortByThirdPopupMenu, &TlsOptDlgSort::CmdSortByThirdPopup, NULL)
END_CMD_MAP_NIL()

/***********************************************************************************************
	TlsOptDlgSort methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
TlsOptDlgSort::TlsOptDlgSort(TlsOptDlg * ptod)
{
	AssertPtr(ptod);
	m_ptod = ptod;
	m_rid = kridTlsOptDlgSort;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Options/Options_Sort_Methods_tab.htm");
	m_prmwMain = NULL;
	m_isrtInitial = 0;
	m_isrtCurrent = -1;
	m_fModified = false;
	m_hmenuPopup = NULL;
	m_hmenuPopup2 = NULL;
	m_hwndToolTip = NULL;
	m_hwndToolTip2 = NULL;
	m_hwndToolTip3 = NULL;
	m_himl = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
TlsOptDlgSort::~TlsOptDlgSort()
{
	if (m_himl)
	{
		AfGdi::ImageList_Destroy(m_himl);
		m_himl = NULL;
	}
	if (m_hmenuPopup)
	{
		::DestroyMenu(m_hmenuPopup);
		m_hmenuPopup = NULL;
	}
	if (m_hmenuPopup2)
	{
		::DestroyMenu(m_hmenuPopup2);
		m_hmenuPopup2 = NULL;
	}
#ifdef TimP_2002_10_Invalid
	// It appears tooltip "windows" should not be "DestroyWindow"ed.
	// These DestroyWindow calls cause an error that GetLastError reports as "1400".
	if (m_hwndToolTip)
	{
		::DestroyWindow(m_hwndToolTip);
		m_hwndToolTip = NULL;
	}
	if (m_hwndToolTip2)
	{
		::DestroyWindow(m_hwndToolTip2);
		m_hwndToolTip2 = NULL;
	}
	if (m_hwndToolTip3)
	{
		::DestroyWindow(m_hwndToolTip3);
		m_hwndToolTip3 = NULL;
	}
#endif
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal. This method will
	result in a dialog allowing only a single selection.

	@param prmwMain Pointer to the application's main window.
	@param isrtInitial Index into the application's table of sorting specifications for choosing
					an initial sorting specification to edit.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgSort::SetDialogValues(RecMainWnd * prmwMain, int isrtInitial)
{
	AssertPtr(prmwMain);

	m_prmwMain = prmwMain;
	m_isrtInitial = isrtInitial;
}


/*----------------------------------------------------------------------------------------------
	Gets the final values for the dialog controls, after the dialog has been closed.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgSort::GetDialogValues(Vector<SortMethodInfo> & vsmi)
{
	vsmi.Clear();
	for (int ismi = 0; ismi < m_vsmi.Size(); ++ismi)
	{
		// Skip deleted methods.
		if (m_vsmi[ismi].m_sms != ksmsDeleted)
			vsmi.Push(m_vsmi[ismi]);
	}
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgSort::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_ERASEBKGND)
	{
		// this is required to prevent the listview from not painting when selected then covered
		// by another window, then uncovered.  without this the listview will not repaint.
		HWND hwndList = ::GetDlgItem(m_hwnd, kctidSortList);
		RedrawWindow(hwndList, NULL , NULL, RDW_ERASE | RDW_FRAME | RDW_INTERNALPAINT |
			RDW_INVALIDATE);
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgSort::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	StrApp strLbl = m_ptod->GetIncludeLabel();
	if (strLbl.Length())
		::SendMessage(::GetDlgItem(m_hwnd, kctidSortIncludeSubrecords),
			WM_SETTEXT, 0, (LPARAM)strLbl.Chars());

	AfLpInfoPtr qlpi = m_prmwMain->GetLpInfo();
	AssertPtr(qlpi);
	AfDbInfoPtr qdbi = qlpi->GetDbInfo();
	AssertPtr(qdbi);

	StrApp strTip;
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidSortByFirst, kbtPopMenu, NULL, 0);
	m_hwndToolTip = ::CreateWindowEx(0, TOOLTIPS_CLASS, NULL, TTS_ALWAYSTIP,
		0, 0, 0, 0, m_hwnd, 0, ModuleEntry::GetModuleHandle(), NULL);
	TOOLINFO ti = { isizeof(ti), TTF_SUBCLASS | TTF_IDISHWND };
	ti.hwnd = m_hwnd;
	ti.uId = (uint)qbtn->Hwnd();
	strTip.Load(kstidSortByFirstTip);
	ti.lpszText = const_cast<achar *>(strTip.Chars());
	::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);

	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidSortBySecond, kbtPopMenu, NULL, 0);
	m_hwndToolTip2 = ::CreateWindowEx(0, TOOLTIPS_CLASS, NULL, TTS_ALWAYSTIP,
		0, 0, 0, 0, m_hwnd, 0, ModuleEntry::GetModuleHandle(), NULL);
	ti.hwnd = m_hwnd;
	ti.uId = (uint)qbtn->Hwnd();
	strTip.Load(kstidSortBySecondTip);
	ti.lpszText = const_cast<achar *>(strTip.Chars());
	::SendMessage(m_hwndToolTip2, TTM_ADDTOOL, 0, (LPARAM)&ti);

	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidSortByThird, kbtPopMenu, NULL, 0);
	m_hwndToolTip3 = ::CreateWindowEx(0, TOOLTIPS_CLASS, NULL, TTS_ALWAYSTIP,
		0, 0, 0, 0, m_hwnd, 0, ModuleEntry::GetModuleHandle(), NULL);
	ti.hwnd = m_hwnd;
	ti.uId = (uint)qbtn->Hwnd();
	strTip.Load(kstidSortByThirdTip);
	ti.lpszText = const_cast<achar *>(strTip.Chars());
	::SendMessage(m_hwndToolTip3, TTM_ADDTOOL, 0, (LPARAM)&ti);

	// Create a temporary cache for loading/saving from/to the database.
	m_qodde.CreateInstance(CLSID_VwOleDbDa);
	IOleDbEncapPtr qode;
	IFwMetaDataCachePtr qmdc;
	qdbi->GetDbAccess(&qode);
	qdbi->GetFwMetaDataCache(&qmdc);
	qdbi->GetLgWritingSystemFactory(&m_qwsf);
	ISetupVwOleDbDaPtr qsodde;
	CheckHr(m_qodde->QueryInterface(IID_ISetupVwOleDbDa, (void**)&qsodde));
	Assert(qsodde);
	qsodde->Init(qode, qmdc, m_qwsf, NULL);

	// Create a private dummy cache.
	m_qvcd.CreateInstance(CLSID_VwCacheDa);

	// Create the popup menus used to select fields.
	Assert(m_hmenuPopup == NULL);
	m_hmenuPopup = CreatePopupMenu(qlpi, m_prmwMain);
	// The second popup menu is the same, except that we add a "NONE" entry and a separator line
	// at the top of the menu.
	Assert(m_hmenuPopup2 == NULL);
	m_hmenuPopup2 = CreatePopupMenu(qlpi, m_prmwMain);
	SortMenuNodeVec & vsmnFlat = m_prmwMain->FlatSortMenuNodeVec();
	StrApp str(kstidSortNone);
	MENUITEMINFO mii = { sizeof(MENUITEMINFO), MIIM_ID|MIIM_STRING, MFT_STRING, MFS_ENABLED,
		kcidMenuItemDynMin + vsmnFlat.Size(), NULL, NULL, NULL,
		reinterpret_cast<ULONG_PTR>(str.Chars()), const_cast<achar *>(str.Chars()),
		str.Length(), NULL };
	::InsertMenuItem(m_hmenuPopup2, 0, TRUE, &mii);
	mii.fMask = MIIM_TYPE;
	mii.fType = MFT_SEPARATOR;
	mii.dwItemData = NULL;
	mii.dwTypeData = NULL;
	mii.cch = 0;
	::InsertMenuItem(m_hmenuPopup2, 1, TRUE, &mii);

	// Load the sort methods into our internal sort method vector.
	int clidRec = m_prmwMain->GetRecordClid();
	int csrt = qdbi->GetSortCount();
	for (int isrt = 0; isrt < csrt; isrt++)
	{
		AppSortInfo asi = qdbi->GetSortInfo(isrt);
		if (asi.m_clidRec == clidRec)
		{
			SortMethodInfo smi;

			smi.m_stuName = asi.m_stuName;
			smi.m_fIncludeSubfields = asi.m_fIncludeSubfields;
			smi.m_hvoOld = asi.m_hvo;
			smi.m_hvo = 0;

			SortMethodUtil::ParseFieldPath(asi.m_stuPrimaryField,
				smi.m_rgski[kiskiPrimary].m_vflid);
			smi.m_rgski[kiskiPrimary].m_ws = asi.m_wsPrimary;
			smi.m_rgski[kiskiPrimary].m_coll =
				GetCollationIndex(asi.m_wsPrimary, asi.m_collPrimary);
			smi.m_rgski[kiskiPrimary].m_fReverse = asi.m_fPrimaryReverse;

			SortMethodUtil::ParseFieldPath(asi.m_stuSecondaryField,
				smi.m_rgski[kiskiSecondary].m_vflid);
			smi.m_rgski[kiskiSecondary].m_ws = asi.m_wsSecondary;
			smi.m_rgski[kiskiSecondary].m_coll =
				GetCollationIndex(asi.m_wsSecondary, asi.m_collSecondary);
			smi.m_rgski[kiskiSecondary].m_fReverse = asi.m_fSecondaryReverse;

			SortMethodUtil::ParseFieldPath(asi.m_stuTertiaryField,
				smi.m_rgski[kiskiTertiary].m_vflid);
			smi.m_rgski[kiskiTertiary].m_ws = asi.m_wsTertiary;
			smi.m_rgski[kiskiTertiary].m_coll =
				GetCollationIndex(asi.m_wsTertiary, asi.m_collTertiary);
			smi.m_rgski[kiskiTertiary].m_fReverse = asi.m_fTertiaryReverse;

			m_vsmi.Push(smi);
		}
	}
	if (m_isrtInitial < 0)
		m_isrtInitial = 0;
	if (m_vsmi.Size())
	{
		// If a sort is activated on a custom field, and it is the last one in the list and
		// the user deletes the custom field, we get into a state where m_isrtInitial ends up
		// equaling m_vsmi.Size(). So in this case, we can just set it to zero since the
		// viewbar also sets it to no sort. I'm (KenZ) not sure this is the correct answer,
		// but it does seem to work without any ill effects and the chance of it happening
		// is extremely rare.
		if (m_isrtInitial >= m_vsmi.Size())
			m_isrtInitial = 0;
		Assert((uint)m_isrtInitial < (uint)m_vsmi.Size());
		InitializeWithSortMethod(m_isrtInitial);
	}

	HWND hwndList = ::GetDlgItem(m_hwnd, kctidSortList);
	HIMAGELIST himlOld = ListView_SetImageList(hwndList, GetImageList(), LVSIL_SMALL);
	if (himlOld)
		if (himlOld != GetImageList())
			AfGdi::ImageList_Destroy(himlOld);

	// Insert a column in the list view control.
	LVCOLUMN lvc = { LVCF_TEXT | LVCF_WIDTH };
	Rect rc;
	::GetClientRect(hwndList, &rc);
	lvc.cx = rc.Width();
	ListView_InsertColumn(hwndList, 0, &lvc);

	UpdateSortMethodList();

	return true;
}


/*----------------------------------------------------------------------------------------------
	Apply changes to the dialog. This is the point where all sort method changes get copied from
	our dummy cache back to the temporary cache (which writes it out to the database). This
	methods also reorders the vector of sort method information so that it is sorted by the sort
	method name.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgSort::Apply()
{
	Assert(m_hwnd);

	WaitCursor wc;

	AfLpInfoPtr qlpi = m_prmwMain->GetLpInfo();
	AssertPtr(qlpi);
	AfDbInfoPtr qdbi = qlpi->GetDbInfo();
	AssertPtr(qdbi);

	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	qdbi->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));

	// Get interfaces needed in this method.
	ISilDataAccessPtr qsda_oddeTemp;
	CheckHr(m_qodde->QueryInterface(IID_ISilDataAccess, (void **)&qsda_oddeTemp));
	IVwCacheDaPtr qvcd_oddeTemp;
	CheckHr(m_qodde->QueryInterface(IID_IVwCacheDa, (void**)&qvcd_oddeTemp));

	HVO hvoLpId = qlpi->GetLpId();

	int isrt;
	int csrt = m_vsmi.Size();
	// Check for any sort method not actually being defined.
	StrUni stuUndefined;
	int csrtUndef = 0;
	for (isrt = 0; isrt < csrt; ++isrt)
	{
		SortMethodInfo & smi = m_vsmi[isrt];
		if (smi.m_sms == ksmsInserted && !smi.m_rgski[kiskiPrimary].m_vflid.Size())
		{
			if (stuUndefined.Length())
				stuUndefined.Append(L", ");
			stuUndefined.Append(smi.m_stuName);
			++csrtUndef;
		}
	}
	if (csrtUndef)
	{
		// Alert the user, ask what to do.
		StrApp strUndefined(stuUndefined);
		StrApp strFmt;
		if (csrtUndef == 1)
			strFmt.Load(kstidSortUndefinedMethod);
		else
			strFmt.Load(kstidSortUndefinedMethods);
		StrApp str;
		str.Format(strFmt.Chars(), strUndefined.Chars());
		StrApp strTitle(kstidSortCannotCreate);
		if (::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(),
			MB_OKCANCEL | MB_ICONQUESTION | MB_APPLMODAL) == IDCANCEL)
		{
			return false;
		}
	}

	// For all the sort methods that were updated, copy their information into the temporary
	// cache, which will update the database.
	for (isrt = 0; isrt < csrt; ++isrt)
	{
		SortMethodInfo & smi = m_vsmi[isrt];
		if (smi.m_sms == ksmsDeleted)
		{
			// If smi.m_hvoOld is NULL, the sort method hasn't been put into the database yet,
			// so we don't have to delete it.
			if (smi.m_hvoOld)
			{
				m_fModified = true;
				CheckHr(qsda_oddeTemp->DeleteObjOwner(hvoLpId, smi.m_hvoOld,
					kflidLangProject_SortSpecs, -1));
			}
		}

		if (smi.m_sms == ksmsInserted && !smi.m_rgski[kiskiPrimary].m_vflid.Size())
			continue;

		if (smi.m_sms == ksmsInserted || smi.m_sms == ksmsModified)
		{
			m_fModified = true;
			if (smi.m_hvoOld)
			{
				Assert(smi.m_sms == ksmsModified);
			}
			else
			{
				Assert(smi.m_sms == ksmsInserted);
				// Create the new sort method.
				qsda_oddeTemp->MakeNewObject(kclidCmSortSpec, hvoLpId,
					kflidLangProject_SortSpecs, -1, &(smi.m_hvoOld));
			}

			// Convert the writing system ids and collation indexes into actual collation
			// database ids (or "null").
			StrUni stuPrimaryWs;
			StrUni stuSecondaryWs;
			StrUni stuTertiaryWs;
			StrUni stuPrimaryColl;
			StrUni stuSecondaryColl;
			StrUni stuTertiaryColl;
			GetUpdateIdStrings(smi.m_rgski[kiskiPrimary], stuPrimaryWs, stuPrimaryColl);
			GetUpdateIdStrings(smi.m_rgski[kiskiSecondary], stuSecondaryWs, stuSecondaryColl);
			GetUpdateIdStrings(smi.m_rgski[kiskiTertiary], stuTertiaryWs, stuTertiaryColl);
			// Generate the field path strings to store in the database.
			StrUni stuPrimaryFieldPath;
			StrUni stuSecondaryFieldPath;
			StrUni stuTertiaryFieldPath;
			SortMethodUtil::CreateFieldPath(smi.m_rgski[kiskiPrimary].m_vflid,
				stuPrimaryFieldPath);
			SortMethodUtil::CreateFieldPath(smi.m_rgski[kiskiSecondary].m_vflid,
				stuSecondaryFieldPath);
			SortMethodUtil::CreateFieldPath(smi.m_rgski[kiskiTertiary].m_vflid,
				stuTertiaryFieldPath);
			StrUni stuQuery;
			const CLSID * pclsid = AfApp::Papp()->GetAppClsid();
			stuQuery.Format(L"UPDATE CmSortSpec%n"
				L" SET [Name]=?, App='%g', ClassId=%d, IncludeSubentries=%d,%n"
				L" PrimaryField=?, PrimaryWs=%s, PrimaryCollation=%s,"
				L" PrimaryReverse=%d,%n"
				L" SecondaryField=%s, SecondaryWs=%s, SecondaryCollation=%s,"
				L" SecondaryReverse=%d,%n"
				L" TertiaryField=%s, TertiaryWs=%s, TertiaryCollation=%s,"
				L" TertiaryReverse=%d%n"
				L" WHERE [id] = %d",
				pclsid, m_prmwMain->GetRecordClid(),
				smi.m_fIncludeSubfields,
				stuPrimaryWs.Chars(), stuPrimaryColl.Chars(),
				smi.m_rgski[kiskiPrimary].m_fReverse,
				stuSecondaryFieldPath.Length() ? L"?" : L"null",
				stuSecondaryWs.Chars(), stuSecondaryColl.Chars(),
				smi.m_rgski[kiskiSecondary].m_fReverse,
				stuTertiaryFieldPath.Length() ? L"?" : L"null",
				stuTertiaryWs.Chars(), stuTertiaryColl.Chars(),
				smi.m_rgski[kiskiTertiary].m_fReverse,
				smi.m_hvoOld);
			StrUtil::NormalizeStrUni(smi.m_stuName, UNORM_NFD);
			StrUtil::NormalizeStrUni(stuPrimaryFieldPath, UNORM_NFD);
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)(smi.m_stuName.Chars()), smi.m_stuName.Length() * 2));
			CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)(stuPrimaryFieldPath.Chars()), stuPrimaryFieldPath.Length() * 2));
			if (stuSecondaryFieldPath.Length())
			{
				StrUtil::NormalizeStrUni(stuSecondaryFieldPath, UNORM_NFD);
				CheckHr(qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
					(ULONG *)(stuSecondaryFieldPath.Chars()),
					stuSecondaryFieldPath.Length() * 2));
			}
			if (stuTertiaryFieldPath.Length())
			{
				Assert(stuSecondaryFieldPath.Length());
				StrUtil::NormalizeStrUni(stuTertiaryFieldPath, UNORM_NFD);
				CheckHr(qodc->SetParameter(4, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
					(ULONG *)(stuTertiaryFieldPath.Chars()),
					stuTertiaryFieldPath.Length() * 2));
			}
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
		}
	}

	// Reorder the sort methods in m_vsmi by name, and remove deleted (or undefined) sort
	// methods.
	Vector<SortMethodInfo> vsmi;
	for (isrt = 0; isrt < csrt; ++isrt)
	{
		SortMethodInfo & smi = m_vsmi[isrt];
		if (smi.m_sms == ksmsDeleted ||
			(smi.m_sms == ksmsInserted && !smi.m_rgski[kiskiPrimary].m_vflid.Size()))
		{
			continue;
		}
		int iv;
		int ivLim;
		for (iv = 0, ivLim = vsmi.Size(); iv < ivLim; )
		{
			int ivMid = (iv + ivLim) / 2;
			if (vsmi[ivMid].m_stuName < smi.m_stuName)
				iv = ivMid + 1;
			else
				ivLim = ivMid;
		}
		Assert(iv <= vsmi.Size());
		vsmi.Insert(iv, smi);
	}
	m_vsmi = vsmi;

	return true;
}


/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgSort::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case LVN_ITEMCHANGED:
		{
			NMLISTVIEW * pnmlv = reinterpret_cast<NMLISTVIEW *>(pnmh);
			if (pnmlv->uChanged & LVIF_STATE)
			{
				Assert(ctidFrom == kctidSortList);
				if (pnmlv->uNewState & LVIS_SELECTED)
				{
					// Show the new selected sort method on the right side of the dialog.
					InitializeWithSortMethod(pnmlv->lParam);
					::SetFocus(pnmh->hwndFrom);
					return true;
				}
			}
			break;
		}
		break;

	case LVN_ITEMCHANGING:
		{
			// If the user clicked on an empty part of the list view, keep the selection
			// on the current item.
			NMLISTVIEW * pnmlv = reinterpret_cast<NMLISTVIEW *>(pnmh);
			if (pnmlv->uChanged & LVIF_STATE && !(pnmlv->uNewState & LVIS_SELECTED))
			{
				// NOTE: This can also be called when the keyboard is used to select a different
				// item. In this case, we don't want to cancel the new selection.
				if (::GetKeyState(VK_LBUTTON) < 0 || ::GetKeyState(VK_RBUTTON) < 0)
				{
					LVHITTESTINFO lvhti;
					::GetCursorPos(&lvhti.pt);
					::ScreenToClient(pnmh->hwndFrom, &lvhti.pt);
					if (ListView_HitTest(pnmh->hwndFrom, &lvhti) == -1)
					{
						lnRet = true;
						return true;
					}
				}
			}
		}
		break;

	case LVN_KEYDOWN:
		{
			NMLVKEYDOWN * pnmlvkd = reinterpret_cast<NMLVKEYDOWN *>(pnmh);
			if (pnmlvkd->wVKey == VK_DELETE)
				DeleteSortMethod();
			else if (pnmlvkd->wVKey == VK_F2)
			{
				int iitem = ListView_GetNextItem(pnmh->hwndFrom, -1, LVNI_SELECTED);
				if (iitem != -1)
					ListView_EditLabel(pnmh->hwndFrom, iitem);
			}
			break;
		}
		break;

	case LVN_ENDLABELEDIT:
		return OnEndLabelEdit(reinterpret_cast<NMLVDISPINFO *>(pnmh), lnRet);

	case BN_CLICKED:
		switch (ctidFrom)
		{
		case kctidAddSort:
			{
				StrUniBufPath stubp;
				stubp.Load(kstidSortNewMethod);
				InsertSortMethod(stubp.Chars(), NULL);
			}
			return true;
		case kctidCopySort:
			{
				Assert((uint)m_isrtCurrent < (uint)m_vsmi.Size());
				SortMethodInfo & smiOld = m_vsmi[m_isrtCurrent];
				InsertSortMethod(smiOld.m_stuName.Chars(), &smiOld);
			}
			return true;
		case kctidDeleteSort:
			DeleteSortMethod();
			return true;
		case kctidSortIncludeSubrecords:
			{
				SortMethodInfo & smi = m_vsmi[m_isrtCurrent];
				smi.m_fIncludeSubfields = !smi.m_fIncludeSubfields;
				if (smi.m_fIncludeSubfields)
					::CheckDlgButton(m_hwnd, kctidSortIncludeSubrecords, BST_CHECKED);
				else
					::CheckDlgButton(m_hwnd, kctidSortIncludeSubrecords, BST_UNCHECKED);
				if (smi.m_sms == ksmsNormal)
					smi.m_sms = ksmsModified;
			}
			return true;
		case kctidSortByFirst:
			{
				// Show the popup-menu
				Rect rc;
				::GetWindowRect(::GetDlgItem(m_hwnd, kctidSortByFirst), &rc);
				AfApp::GetMenuMgr()->SetMenuHandler(kcidSortByFirstPopupMenu);
				::TrackPopupMenu(m_hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, rc.left,
					rc.bottom, 0, m_hwnd, NULL);
				// The actual work is carried out by CmdSortByFirst().
			}
			return true;
		case kctidSortBySecond:
			{
				// Show the popup-menu
				Rect rc;
				::GetWindowRect(::GetDlgItem(m_hwnd, kctidSortBySecond), &rc);
				AfApp::GetMenuMgr()->SetMenuHandler(kcidSortBySecondPopupMenu);
				::TrackPopupMenu(m_hmenuPopup2, TPM_LEFTALIGN | TPM_RIGHTBUTTON, rc.left,
					rc.bottom, 0, m_hwnd, NULL);
				// The actual work is carried out by CmdSortBySecond().
			}
			return true;
		case kctidSortByThird:
			{
				// Show the popup-menu
				Rect rc;
				::GetWindowRect(::GetDlgItem(m_hwnd, kctidSortByThird), &rc);
				AfApp::GetMenuMgr()->SetMenuHandler(kcidSortByThirdPopupMenu);
				::TrackPopupMenu(m_hmenuPopup2, TPM_LEFTALIGN | TPM_RIGHTBUTTON, rc.left,
					rc.bottom, 0, m_hwnd, NULL);
				// The actual work is carried out by CmdSortByThird().
			}
			return true;
		case kctidSortByFirstReversed:
			{
				SortMethodInfo & smi = m_vsmi[m_isrtCurrent];
				smi.m_rgski[kiskiPrimary].m_fReverse = !smi.m_rgski[kiskiPrimary].m_fReverse;
				if (smi.m_rgski[kiskiPrimary].m_fReverse)
					::CheckDlgButton(m_hwnd, kctidSortByFirstReversed, BST_CHECKED);
				else
					::CheckDlgButton(m_hwnd, kctidSortByFirstReversed, BST_UNCHECKED);
				if (smi.m_sms == ksmsNormal)
					smi.m_sms = ksmsModified;
			}
			return true;
		case kctidSortBySecondReversed:
			{
				SortMethodInfo & smi = m_vsmi[m_isrtCurrent];
				smi.m_rgski[kiskiSecondary].m_fReverse =
					!smi.m_rgski[kiskiSecondary].m_fReverse;
				if (smi.m_rgski[kiskiSecondary].m_fReverse)
					::CheckDlgButton(m_hwnd, kctidSortBySecondReversed, BST_CHECKED);
				else
					::CheckDlgButton(m_hwnd, kctidSortBySecondReversed, BST_UNCHECKED);
				if (smi.m_sms == ksmsNormal)
					smi.m_sms = ksmsModified;
			}
			return true;
		case kctidSortByThirdReversed:
			{
				SortMethodInfo & smi = m_vsmi[m_isrtCurrent];
				smi.m_rgski[kiskiTertiary].m_fReverse = !smi.m_rgski[kiskiTertiary].m_fReverse;
				if (smi.m_rgski[kiskiTertiary].m_fReverse)
					::CheckDlgButton(m_hwnd, kctidSortByThirdReversed, BST_CHECKED);
				else
					::CheckDlgButton(m_hwnd, kctidSortByThirdReversed, BST_UNCHECKED);
				if (smi.m_sms == ksmsNormal)
					smi.m_sms = ksmsModified;
			}
			return true;
		}
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle an LVN_ENDLABELEDIT notification message by changing the name of the item if the new
	name is a unique, non-empty string.

	@param plvdi Pointer to the data for an LVN_ENDLABELEDIT notification message.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgSort::OnEndLabelEdit(NMLVDISPINFO * plvdi, long & lnRet)
{
	AssertPtr(plvdi);

	if (plvdi->item.pszText)
	{
		AssertPsz(plvdi->item.pszText);

		// Strip off blank characters at the front and end of the name.
		StrApp strLabel;
		StrUtil::TrimWhiteSpace(plvdi->item.pszText, strLabel);

		if (strLabel.Length() == 0)
		{
			// The item is empty, so show a message complaining about it.
			StrApp strMessage(kstidSortRenEmptyMsg);
			StrApp strSortMethod(kstidTlsOptSort);
			::MessageBox(m_hwnd, strMessage.Chars(), strSortMethod.Chars(),
				MB_OK | MB_ICONINFORMATION | MB_SYSTEMMODAL);
			::PostMessage(plvdi->hdr.hwndFrom, LVM_EDITLABEL, plvdi->item.iItem, 0);
			return true;
		}

		HWND hwndList = ::GetDlgItem(m_hwnd, kctidSortList);

		// See if there is already an item with the same name.
		LVFINDINFO lvfi = { LVFI_STRING };
		lvfi.psz = strLabel.Chars();
		int iitem = ListView_FindItem(hwndList, -1, &lvfi);
		int isrt = -1;
		if (iitem != -1)
		{
			LVITEM lvi = { LVIF_PARAM, iitem };
			ListView_GetItem(hwndList, &lvi);
			isrt = lvi.lParam;
		}
		// If they didn't change the name, we're done.
		if (isrt == m_isrtCurrent)
			return true;
		if (isrt != -1)
		{
			StrApp strMessage(kstidSortRenMethodMsg);
			StrApp strSortMethod(kstidTlsOptSort);
			::MessageBox(m_hwnd, strMessage.Chars(), strSortMethod.Chars(),
				MB_OK | MB_ICONINFORMATION | MB_SYSTEMMODAL);
			::PostMessage(plvdi->hdr.hwndFrom, LVM_EDITLABEL, plvdi->item.iItem, 0);
			return true;
		}

		// Update the name of the selected sort method.
		SortMethodInfo & smi = m_vsmi[m_isrtCurrent];
		Assert(smi.m_sms != ksmsDeleted);
		smi.m_stuName = strLabel;
		if (smi.m_sms == ksmsNormal)
			smi.m_sms = ksmsModified;

		// This is necessary to reorder the list if needed due to the name change.
		UpdateSortMethodList();
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	This method is usually used for three purposes, but in this case only the last case will
	ever get called.
@line	1) If pcmd->m_rgn[0] == AfMenuMgr::kmaExpandItem, it is being called to expand the dummy
			item by adding new items.
@line	2) If pcmd->m_rgn[0] == AfMenuMgr::kmaGetStatusText, it is being called to get the
			status bar string for an expanded item.
@line	3) If pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand, it is being called because the user
			selected an expandable menu item.

	Performing the command:
@line   pcmd->m_rgn[1] -> Contains the handle of the menu (HMENU) containing the expanded items.
@line   pcmd->m_rgn[2] -> Contains the index of the expanded/inserted item to get text for.

	We are using this method to take advantage of the expandable menus functionality.
	See ${CreatePopupMenu} for more information on why we're doing it this way.

	@param pcmd Pointer to the command information.

	@return True.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgSort::CmdSortByFirstPopup(Cmd * pcmd)
{
	AssertPtr(pcmd);
	Assert(pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand);

	SortMethodInfo & smi = m_vsmi[m_isrtCurrent];
	Vector<int> vflidOrig = smi.m_rgski[kiskiPrimary].m_vflid;
	int encOrig = smi.m_rgski[kiskiPrimary].m_ws;
	int collOrig = smi.m_rgski[kiskiPrimary].m_coll;

	// The user selected an expanded menu item, so perform the command now.
	//    m_rgn[1] holds the menu handle.
	//    m_rgn[2] holds the index of the selected item.
	SortMenuNodeVec vsmn;
	SortMenuNodeVec & vsmnFlat = MainWindow()->FlatSortMenuNodeVec();
	BuildColumnVector(vsmnFlat, pcmd->m_rgn[2], vsmn);

	// Update the information in the sort method structure.
	UpdateSortKeyInfo(vsmn, smi.m_rgski[kiskiPrimary]);
	if (smi.m_sms == ksmsNormal)
	{
		if (vflidOrig.Size() != smi.m_rgski[kiskiPrimary].m_vflid.Size() ||
			encOrig != smi.m_rgski[kiskiPrimary].m_ws ||
			collOrig != smi.m_rgski[kiskiPrimary].m_coll)
		{
			smi.m_sms = ksmsModified;
		}
		else
		{
			for (int iflid = 0; iflid < vflidOrig.Size(); ++iflid)
			{
				if (vflidOrig[iflid] != smi.m_rgski[kiskiPrimary].m_vflid[iflid])
				{
					smi.m_sms = ksmsModified;
					break;
				}
			}
		}
	}

	// Update the button text.
	StrApp str;
	GetColumnName(vsmn, ksmntLeaf, ksmntLeaf, str);
	::SetDlgItemText(m_hwnd, kctidSortByFirst, str.Chars());

	GetColumnName(vsmn, ksmntClass, ksmntLeaf, str);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByFirstField), str.Chars());
	::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByFirstReversed), TRUE);

	::EnableWindow(::GetDlgItem(m_hwnd, kctidSortBySecond), TRUE);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidSortThenBySecond), TRUE);

	GetColumnName(vsmn, ksmntWs, ksmntColl, str);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByFirstLanguage), str.Chars());

	return true;
}


/*----------------------------------------------------------------------------------------------
	This method is usually used for three purposes, but in this case only the last case will
	ever get called.
@line	1) If pcmd->m_rgn[0] == AfMenuMgr::kmaExpandItem, it is being called to expand the dummy
			item by adding new items.
@line	2) If pcmd->m_rgn[0] == AfMenuMgr::kmaGetStatusText, it is being called to get the
			status bar string for an expanded item.
@line	3) If pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand, it is being called because the user
			selected an expandable menu item.

	Performing the command:
@line   pcmd->m_rgn[1] -> Contains the handle of the menu (HMENU) containing the expanded items.
@line   pcmd->m_rgn[2] -> Contains the index of the expanded/inserted item to get text for.

	We are using this method to take advantage of the expandable menus functionality.
	See ${CreatePopupMenu} for more information on why we're doing it this way.

	@param pcmd Pointer to the command information.

	@return True.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgSort::CmdSortBySecondPopup(Cmd * pcmd)
{
	AssertPtr(pcmd);
	Assert(pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand);

	SortMethodInfo & smi = m_vsmi[m_isrtCurrent];
	Vector<int> vflidOrig = smi.m_rgski[kiskiSecondary].m_vflid;
	int encOrig = smi.m_rgski[kiskiSecondary].m_ws;
	int collOrig = smi.m_rgski[kiskiSecondary].m_coll;

	// The user selected an expanded menu item, so perform the command now.
	//    m_rgn[1] holds the menu handle.
	//    m_rgn[2] holds the index of the selected item.
	SortMenuNodeVec vsmn;
	SortMenuNodeVec & vsmnFlat = MainWindow()->FlatSortMenuNodeVec();
	if (pcmd->m_rgn[2] == vsmnFlat.Size())
	{
		StrApp str(kstidSortChoose);
		::SetDlgItemText(m_hwnd, kctidSortBySecond, str.Chars());
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortBySecondReversed), FALSE);
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortBySecondField), _T(""));
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortBySecondLanguage), _T(""));

		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByThird), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortThenByThird), FALSE);
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByThirdField), _T(""));
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByThirdLanguage), _T(""));
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByThirdReversed), FALSE);

		if (smi.m_sms == ksmsNormal)
		{
			if (smi.m_rgski[kiskiSecondary].m_vflid.Size() ||
				smi.m_rgski[kiskiSecondary].m_ws ||
				smi.m_rgski[kiskiSecondary].m_coll ||
				smi.m_rgski[kiskiTertiary].m_vflid.Size() ||
				smi.m_rgski[kiskiTertiary].m_ws ||
				smi.m_rgski[kiskiTertiary].m_coll)
			{
				smi.m_sms = ksmsModified;
			}
		}
		smi.m_rgski[kiskiSecondary].m_vflid.Clear();
		smi.m_rgski[kiskiSecondary].m_ws = 0;
		smi.m_rgski[kiskiSecondary].m_coll = 0;
		smi.m_rgski[kiskiSecondary].m_fReverse = false;
		smi.m_rgski[kiskiTertiary].m_vflid.Clear();
		smi.m_rgski[kiskiTertiary].m_ws = 0;
		smi.m_rgski[kiskiTertiary].m_coll = 0;
		smi.m_rgski[kiskiTertiary].m_fReverse = false;

		return true;
	}
	BuildColumnVector(vsmnFlat, pcmd->m_rgn[2], vsmn);

	// Update the information in the sort method structure.
	UpdateSortKeyInfo(vsmn, m_vsmi[m_isrtCurrent].m_rgski[kiskiSecondary]);
	if (smi.m_sms == ksmsNormal)
	{
		if (vflidOrig.Size() != smi.m_rgski[kiskiSecondary].m_vflid.Size() ||
			encOrig != smi.m_rgski[kiskiSecondary].m_ws ||
			collOrig != smi.m_rgski[kiskiSecondary].m_coll)
		{
			smi.m_sms = ksmsModified;
		}
		else
		{
			for (int iflid = 0; iflid < vflidOrig.Size(); ++iflid)
			{
				if (vflidOrig[iflid] != smi.m_rgski[kiskiSecondary].m_vflid[iflid])
				{
					smi.m_sms = ksmsModified;
					break;
				}
			}
		}
	}

	// Update the button text.
	StrApp str;
	GetColumnName(vsmn, ksmntLeaf, ksmntLeaf, str);
	::SetDlgItemText(m_hwnd, kctidSortBySecond, str.Chars());

	GetColumnName(vsmn, ksmntClass, ksmntLeaf, str);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidSortBySecondField), str.Chars());
	::EnableWindow(::GetDlgItem(m_hwnd, kctidSortBySecondReversed), TRUE);

	::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByThird), TRUE);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidSortThenByThird), TRUE);

	GetColumnName(vsmn, ksmntWs, ksmntColl, str);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidSortBySecondLanguage), str.Chars());

	return true;
}


/*----------------------------------------------------------------------------------------------
	This method is usually used for three purposes, but in this case only the last case will
	ever get called.
@line	1) If pcmd->m_rgn[0] == AfMenuMgr::kmaExpandItem, it is being called to expand the dummy
			item by adding new items.
@line	2) If pcmd->m_rgn[0] == AfMenuMgr::kmaGetStatusText, it is being called to get the
			status bar string for an expanded item.
@line	3) If pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand, it is being called because the user
			selected an expandable menu item.

	Performing the command:
@line   pcmd->m_rgn[1] -> Contains the handle of the menu (HMENU) containing the expanded items.
@line   pcmd->m_rgn[2] -> Contains the index of the expanded/inserted item to get text for.

	We are using this method to take advantage of the expandable menus functionality.
	See ${CreatePopupMenu} for more information on why we're doing it this way.

	@param pcmd Pointer to the command information.

	@return True.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgSort::CmdSortByThirdPopup(Cmd * pcmd)
{
	AssertPtr(pcmd);
	Assert(pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand);

	SortMethodInfo & smi = m_vsmi[m_isrtCurrent];
	Vector<int> vflidOrig = smi.m_rgski[kiskiTertiary].m_vflid;
	int encOrig = smi.m_rgski[kiskiTertiary].m_ws;
	int collOrig = smi.m_rgski[kiskiTertiary].m_coll;

	// The user selected an expanded menu item, so perform the command now.
	//    m_rgn[1] holds the menu handle.
	//    m_rgn[2] holds the index of the selected item.
	SortMenuNodeVec vsmn;
	SortMenuNodeVec & vsmnFlat = MainWindow()->FlatSortMenuNodeVec();
	if (pcmd->m_rgn[2] == vsmnFlat.Size())
	{
		StrApp str(kstidSortChoose);
		::SetDlgItemText(m_hwnd, kctidSortByThird, str.Chars());
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByThirdReversed), FALSE);
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByThirdField), _T(""));
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByThirdLanguage), _T(""));

		if (smi.m_sms == ksmsNormal)
		{
			if (smi.m_rgski[kiskiTertiary].m_vflid.Size() || smi.m_rgski[kiskiTertiary].m_ws ||
				smi.m_rgski[kiskiTertiary].m_coll)
			{
				smi.m_sms = ksmsModified;
			}
		}
		smi.m_rgski[kiskiTertiary].m_vflid.Clear();
		smi.m_rgski[kiskiTertiary].m_ws = 0;
		smi.m_rgski[kiskiTertiary].m_coll = 0;
		smi.m_rgski[kiskiTertiary].m_fReverse = false;
		return true;
	}
	BuildColumnVector(vsmnFlat, pcmd->m_rgn[2], vsmn);

	// Update the information in the sort method structure.
	UpdateSortKeyInfo(vsmn, m_vsmi[m_isrtCurrent].m_rgski[kiskiTertiary]);
	if (smi.m_sms == ksmsNormal)
	{
		if (vflidOrig.Size() != smi.m_rgski[kiskiTertiary].m_vflid.Size() ||
			encOrig != smi.m_rgski[kiskiTertiary].m_ws ||
			collOrig != smi.m_rgski[kiskiTertiary].m_coll)
		{
			smi.m_sms = ksmsModified;
		}
		else
		{
			for (int iflid = 0; iflid < vflidOrig.Size(); ++iflid)
			{
				if (vflidOrig[iflid] != smi.m_rgski[kiskiTertiary].m_vflid[iflid])
				{
					smi.m_sms = ksmsModified;
					break;
				}
			}
		}
	}

	// Update the button text.
	StrApp str;
	GetColumnName(vsmn, ksmntLeaf, ksmntLeaf, str);
	::SetDlgItemText(m_hwnd, kctidSortByThird, str.Chars());

	GetColumnName(vsmn, ksmntClass, ksmntLeaf, str);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByThirdField), str.Chars());
	::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByThirdReversed), TRUE);

	GetColumnName(vsmn, ksmntWs, ksmntColl, str);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByThirdLanguage), str.Chars());

	return true;
}


/*----------------------------------------------------------------------------------------------
	Return the handle to the image list that holds icons used by the sort method dialog.
----------------------------------------------------------------------------------------------*/
HIMAGELIST TlsOptDlgSort::GetImageList()
{
	if (!m_himl)
	{
		// The image list hasn't been created yet, so create it now.
		m_himl = AfGdi::ImageList_Create(16, 16, ILC_COLORDDB | ILC_MASK, 0, 0);
		HBITMAP hbmp = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(),
			MAKEINTRESOURCE(kridSortMethodImages));
		if (!hbmp)
			ThrowHr(WarnHr(E_FAIL));
		if (::ImageList_AddMasked(m_himl, hbmp, kclrPink) == -1)
			ThrowHr(WarnHr(E_FAIL));
		AfGdi::DeleteObjectBitmap(hbmp);
	}
	return m_himl;
}


/*----------------------------------------------------------------------------------------------
	Create a hierarchical popup menu based on the sort menu node structure returned
	from the application.
	Since this menu is generated at run-time, we don't know how many items will be in the menu.
	So we start adding menu items in the range used by expandable menus. Since we also store a
	flat list of all possible nodes, we can use these IDs as indexes into the flat list. So
	when a user chooses an item from this menu, we can easily get back to the node the menu
	item corresponds to. This 'trick' seemed to be the most efficient way to create a
	hierarchical menu at runtime and still be able to figure out what the user clicked on.

	@param plpi Pointer to the application language project information.

	@return Handle to the newly created hierarchical popup menu.
----------------------------------------------------------------------------------------------*/
HMENU TlsOptDlgSort::CreatePopupMenu(AfLpInfo * plpi, AfMainWnd * pafw)
{
	AssertPtr(plpi);

	HMENU hmenuPopup = ::CreatePopupMenu();
	if (!hmenuPopup)
		ThrowHr(E_FAIL);
	int cid = kcidMenuItemDynMin;
	AssertPtr(pafw);
	SortMenuNodeVec * pvsmn = pafw->GetSortMenuNodes(plpi);
	AssertPtr(pvsmn);
	SortMenuNodeVec & vsmnFlat = pafw->FlatSortMenuNodeVec();
	bool fPushNodes = vsmnFlat.Size() == 0;
	int csmn = pvsmn->Size();
	if (csmn == 1)
	{
		// We need this top menu node internally: just don't show it to users because they
		// would consider it repetitively redundant.
		if (fPushNodes)
			vsmnFlat.Push((*pvsmn)[0]);
		++cid;
		pvsmn = &(*pvsmn)[0]->m_vsmnSubItems;
		csmn = pvsmn->Size();
	}
	for (int ismn = 0; ismn < csmn; ismn++)
		InsertMenuNode(hmenuPopup, (*pvsmn)[ismn], cid, vsmnFlat, fPushNodes);
	return hmenuPopup;
}

/*----------------------------------------------------------------------------------------------
	This recursive method first gets called from CreatePopupMenu and adds one node to the menu.
	It calls itself recursively if the node pops up another submenu. It is used to generate
	a hierarchical popup menu based on the sort method menu node structure returned from the
	application.

	@param hmenu Handle to a popup menu.
	@param psmn Pointer to a sort method menu node.
	@param cid Command id for the next new menu node to add to hmenu.
	@param fPushNodes Flag whether to push psmn onto the class's flat list of sort method menu
					nodes.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgSort::InsertMenuNode(HMENU hmenu, SortMenuNode * psmn, int & cid,
	SortMenuNodeVec & vsmnFlat, bool fPushNodes)
{
	AssertPtr(psmn);
	StrApp str(psmn->m_stuText);
	if (fPushNodes)
		vsmnFlat.Push(psmn);

	if (!psmn->m_vsmnSubItems.Size())
	{
		Assert(psmn->m_smnt == ksmntLeaf || psmn->m_smnt == ksmntColl
			|| psmn->m_smnt == ksmntWs);

		if (psmn->m_flid == 0 && psmn->m_proptype == kcptNil && !str.Length())
			::AppendMenu(hmenu, MF_SEPARATOR, cid++, NULL);
		else
			::AppendMenu(hmenu, MF_STRING, cid++, str.Chars());
	}
	else
	{
		// Create a popup menu and insert the nodes for it.
		cid++;
		HMENU hmenuPopup = ::CreatePopupMenu();
		int csmn = psmn->m_vsmnSubItems.Size();
		for (int ismn = 0; ismn < csmn; ismn++)
			InsertMenuNode(hmenuPopup, psmn->m_vsmnSubItems[ismn], cid, vsmnFlat, fPushNodes);
		BOOL fSuccess = ::AppendMenu(hmenu, MF_POPUP, (uint)hmenuPopup, str.Chars());
		if (!fSuccess)
		{
			Assert(fSuccess);
		}
		::DestroyMenu(hmenuPopup);
	}
}


/*----------------------------------------------------------------------------------------------
	This recursive method creates a vector of ownership SortMenuNode structures based on the
	index of a selected SortMenuNode in the flat list. It is used to generate the vector when
	the user selects an item from the popup menu generated using CreatePopupMenu.
	When this returns, the first item in vsmn will be the root node. The last item will be the
	node given by ismnFlat.

	@param ismnFlat Index into the class's flat list of sort menu nodes.
	@param vsmn Reference to the output column of sort menu nodes.
	@param fClear Flag whether to clear vsmn at the beginning of the function.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgSort::BuildColumnVector(SortMenuNodeVec & vsmnFlat, int ismnFlat,
	SortMenuNodeVec & vsmn, bool fClear)
{
	SortMenuNodePtr qsmn = vsmnFlat[ismnFlat];
	AssertPtr(qsmn);

	// fClear should only be true the first time this is called.
	if (fClear)
		vsmn.Clear();

	vsmn.Insert(0, qsmn);

	// Look through the flat list to find out which item points to the one given by ismnFlat.
	// It must be before the index given by ismnFlat.
	for (int ismn = 0; ismn < ismnFlat; ismn++)
	{
		SortMenuNodeVec & vsmnSub = vsmnFlat[ismn]->m_vsmnSubItems;
		int csmnSub = vsmnSub.Size();
		for (int ismnSub = 0; ismnSub < csmnSub; ismnSub++)
		{
			if (vsmnSub[ismnSub] == qsmn)
			{
				// We've found the parent so call this method again passing the parent index.
				BuildColumnVector(vsmnFlat, ismn, vsmn, false);
				return;
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	This method returns the column name of a column given the vector of SortMenuNodes that
	represent the column.  The string has the form: "<node>: <node>: <node>".

	If both smntMin and smntMax are equal to ksmntLeaf, and the leaf's flid is equal to either
	kflidCmPossibility_Name or kflidCmPossibility_Abbreviation, the the preceding field node's
	name is used in stead of "Name" or "Abbreviation".

	@param vsmn Reference to the input column of sort menu nodes.
	@param smntMin Type of the first node to show.
	@param smntMax Type of the last node to show.
	@param str Reference to the output column name string.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgSort::GetColumnName(SortMenuNodeVec & vsmn, SortMenuNodeType smntMin,
	SortMenuNodeType smntMax, StrApp & str)
{
	str.Clear();

	StrApp strT;
	int csmn = vsmn.Size();
	Assert(csmn > 0);
	bool fUse = false;
	for (int ismn = 0; ismn < csmn; ismn++)
	{
		if (vsmn[ismn]->m_smnt == smntMin)
			fUse = true;
		if (fUse)
		{
			if (smntMin == smntMax && smntMin == ksmntLeaf)
			{
				if (vsmn[ismn]->m_flid == kflidCmPossibility_Abbreviation ||
					vsmn[ismn]->m_flid == kflidCmPossibility_Name)
				{
					Assert(ismn);
					str = vsmn[ismn - 1]->m_stuText;
					break;
				}
			}
			if (str.Length())
				str.Append(": ");
			strT = vsmn[ismn]->m_stuText;
			str.Append(strT.Chars());
		}
		if (vsmn[ismn]->m_smnt == smntMax)
			break;
	}
}

/*----------------------------------------------------------------------------------------------
	Return a pointer to the menu node in pvsmn given by the path in rgflidPath.  If the path
	does not lead to a menu node, return NULL.

	@param pvsmn
	@param rgflidPath
	@param cflid
----------------------------------------------------------------------------------------------*/
SortMenuNode * TlsOptDlgSort::FindMenuNode(SortMenuNodeVec * pvsmn, int * rgflidPath, int cflid)
{
	AssertPtr(pvsmn);
	AssertArray(rgflidPath, cflid);
	AssertPtr(rgflidPath);
	Assert(cflid);

	int csmn = pvsmn->Size();
	int ismn;
	for (ismn = 0; ismn < csmn; ++ismn)
	{
		if ((*pvsmn)[ismn]->m_flid == rgflidPath[0])
		{
			if (cflid == 1)
				return (*pvsmn)[ismn];
			else
				return FindMenuNode(&(*pvsmn)[ismn]->m_vsmnSubItems, rgflidPath + 1, cflid - 1);
		}
	}
	return NULL;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void TlsOptDlgSort::InitializeWithSortMethod(int isrt)
{
	m_isrtCurrent = isrt;

	::EnableWindow(::GetDlgItem(m_hwnd, kctidSortIncludeSubrecords), TRUE);
	if (m_vsmi[isrt].m_fIncludeSubfields)
		::CheckDlgButton(m_hwnd, kctidSortIncludeSubrecords, BST_CHECKED);
	else
		::CheckDlgButton(m_hwnd, kctidSortIncludeSubrecords, BST_UNCHECKED);

	StrApp strField;
	StrApp strPath;
	StrApp strWsColl;
	StrApp strChoose(kstidSortChoose);

	::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByFirst), TRUE);
	if (m_vsmi[isrt].m_rgski[kiskiPrimary].m_vflid.Size())
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByFirstReversed), TRUE);
		if (m_vsmi[isrt].m_rgski[kiskiPrimary].m_fReverse)
			::CheckDlgButton(m_hwnd, kctidSortByFirstReversed, BST_CHECKED);
		else
			::CheckDlgButton(m_hwnd, kctidSortByFirstReversed, BST_UNCHECKED);
		BuildPathNames(m_vsmi[isrt].m_rgski[kiskiPrimary], strField, strPath, strWsColl);
		::SetDlgItemText(m_hwnd, kctidSortByFirst, strField.Chars());
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByFirstField), strPath.Chars());
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByFirstLanguage), strWsColl.Chars());
		// Enable the secondary sort selection.
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortBySecond), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortThenBySecond), TRUE);
	}
	else
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByFirstReversed), FALSE);
		::CheckDlgButton(m_hwnd, kctidSortByFirstReversed, BST_UNCHECKED);
		::SetDlgItemText(m_hwnd, kctidSortByFirst, strChoose.Chars());
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByFirstField), _T(""));
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByFirstLanguage), _T(""));
		// Disable the secondary sort selection: we don't have even a primary key yet!
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortBySecond), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortThenBySecond), FALSE);
		Assert(!m_vsmi[isrt].m_rgski[kiskiSecondary].m_vflid.Size());
	}

	if (m_vsmi[isrt].m_rgski[kiskiSecondary].m_vflid.Size())
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortBySecondReversed), TRUE);
		if (m_vsmi[isrt].m_rgski[kiskiSecondary].m_fReverse)
			::CheckDlgButton(m_hwnd, kctidSortBySecondReversed, BST_CHECKED);
		else
			::CheckDlgButton(m_hwnd, kctidSortBySecondReversed, BST_UNCHECKED);
		BuildPathNames(m_vsmi[isrt].m_rgski[kiskiSecondary], strField, strPath, strWsColl);
		::SetDlgItemText(m_hwnd, kctidSortBySecond, strField.Chars());
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortBySecondField), strPath.Chars());
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortBySecondLanguage), strWsColl.Chars());
		// Enable the tertiary sort selection.
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByThird), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortThenByThird), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByThirdReversed), TRUE);
	}
	else
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortBySecondReversed), FALSE);
		::CheckDlgButton(m_hwnd, kctidSortBySecondReversed, BST_UNCHECKED);
		::SetDlgItemText(m_hwnd, kctidSortBySecond, strChoose.Chars());
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortBySecondField), _T(""));
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortBySecondLanguage), _T(""));
		// Disable the tertiary sort selection: we don't have even a secondary key yet!
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByThird), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortThenByThird), FALSE);
		Assert(!m_vsmi[isrt].m_rgski[kiskiTertiary].m_vflid.Size());
	}

	if (m_vsmi[isrt].m_rgski[kiskiTertiary].m_vflid.Size())
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByThirdReversed), TRUE);
		if (m_vsmi[isrt].m_rgski[kiskiTertiary].m_fReverse)
			::CheckDlgButton(m_hwnd, kctidSortByThirdReversed, BST_CHECKED);
		else
			::CheckDlgButton(m_hwnd, kctidSortByThirdReversed, BST_UNCHECKED);
		BuildPathNames(m_vsmi[isrt].m_rgski[kiskiTertiary], strField, strPath, strWsColl);
		::SetDlgItemText(m_hwnd, kctidSortByThird, strField.Chars());
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByThirdField), strPath.Chars());
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByThirdLanguage), strWsColl.Chars());
	}
	else
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByThirdReversed), FALSE);
		::CheckDlgButton(m_hwnd, kctidSortByThirdReversed, BST_UNCHECKED);
		::SetDlgItemText(m_hwnd, kctidSortByThird, strChoose.Chars());
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByThirdField), _T(""));
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByThirdLanguage), _T(""));
	}
}

/*----------------------------------------------------------------------------------------------

	@param ski
	@param strField
	@param strPath
	@param strWsColl
----------------------------------------------------------------------------------------------*/
void TlsOptDlgSort::BuildPathNames(SortKeyInfo & ski, StrApp & strField, StrApp & strPath,
	StrApp & strWsColl)
{
	AfLpInfoPtr qlpi = m_prmwMain->GetLpInfo();
	AssertPtr(qlpi);
	SortMenuNodeVec * pvsmn = m_prmwMain->GetSortMenuNodes(qlpi);
	AssertPtr(pvsmn);

	strField.Clear();
	strPath.Clear();
	strWsColl.Clear();

	StrApp str;
	int iflid;
	int ismn;
	for (iflid = 0, ismn = 0; ismn < pvsmn->Size(); ++ismn)
	{
		if ((*pvsmn)[ismn]->m_flid == ski.m_vflid[iflid])
		{
			if (strPath.Length())
				strPath.Append(_T(": "));
			str.Assign((*pvsmn)[ismn]->m_stuText);
			strPath.Append(str);
			++iflid;
			if (iflid == ski.m_vflid.Size())
			{
				Assert((*pvsmn)[ismn]->m_smnt == ksmntLeaf);
				strField.Assign(str);
				break;
			}
			// Fudge the loop variables to go one level down into the cascading menus.
			pvsmn = &(*pvsmn)[ismn]->m_vsmnSubItems;
			ismn = -1;
		}
	}
	// Convert the collation information to the writing system/old writing system/collation
	// string.
	if (ski.m_ws)
	{
		Assert((*pvsmn)[ismn]->m_vsmnSubItems.Size());
		// Point to writing system node.
		IWritingSystemPtr qws;
		CheckHr(m_qwsf->get_EngineOrNull(ski.m_ws, &qws));
		if (qws)
		{
			StrUni stuEncColl;
			SmartBstr sbstr;
			int wsUser;
			CheckHr(m_qwsf->get_UserWs(&wsUser));
			CheckHr(qws->get_Name(wsUser, &sbstr));
			stuEncColl.Assign(sbstr.Chars());
			int ccoll;
			CheckHr(qws->get_CollationCount(&ccoll));
			if (ccoll)
			{
				ICollationPtr qcoll;
				Assert((unsigned)ski.m_coll < (unsigned)ccoll);
				CheckHr(qws->get_Collation(ski.m_coll, &qcoll));
				if (!qcoll)
				{
					ski.m_coll = 0;			// Use the default collation.
					CheckHr(qws->get_Collation(0, &qcoll));
				}
				AssertPtr(qcoll);
				CheckHr(qcoll->get_Name(wsUser, &sbstr));
				stuEncColl.FormatAppend(L": %s", sbstr.Chars());
			}
			strWsColl.Assign(stuEncColl);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Update the list view with the sort method information.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgSort::UpdateSortMethodList()
{
	HWND hwndList = ::GetDlgItem(m_hwnd, kctidSortList);

	int iitemOld = 0;
	if (m_isrtCurrent != -1)
	{
		LVFINDINFO lvfi = { LVFI_PARAM };
		lvfi.lParam = m_isrtCurrent;
		iitemOld = ListView_FindItem(hwndList, -1, &lvfi);
	}

	::SendMessage(hwndList, WM_SETREDRAW, false, 0);
	ListView_DeleteAllItems(hwndList);

	// Reorder the names of the sort methods in m_vsmi.  (This is a virtual reordering only, to
	// ensure sorted names in the list box.)
	Vector<int> visrt;
	int isrt;
	int csrt = m_vsmi.Size();
	for (isrt = 0; isrt < csrt; ++isrt)
	{
		int iv;
		int ivLim;
		for (iv = 0, ivLim = visrt.Size(); iv < ivLim; )
		{
			int ivMid = (iv + ivLim) / 2;
			if (m_vsmi[visrt[ivMid]].m_stuName < m_vsmi[isrt].m_stuName)
				iv = ivMid + 1;
			else
				ivLim = ivMid;
		}
		Assert(iv <= visrt.Size());
		visrt.Insert(iv, isrt);
	}

	// Insert items in the list view for all non-deleted sort methods.
	LVITEM lvi = { LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM };
	for (isrt = 0; isrt < csrt; isrt++)
	{
		lvi.iItem = isrt;
		SortMethodInfo & smi = m_vsmi[visrt[isrt]];
		if (smi.m_sms == ksmsDeleted)
			continue;
		StrApp str(smi.m_stuName.Chars());
		lvi.pszText = const_cast<achar *>(str.Chars());
		lvi.iImage = 0;
		lvi.lParam = visrt[isrt];
		ListView_InsertItem(hwndList, &lvi);
	}

	if (m_isrtCurrent != -1)
	{
		// Find the index of the item that was previously selected.
		LVFINDINFO lvfi = { LVFI_PARAM };
		lvfi.lParam = m_isrtCurrent;
		int iitemNew = ListView_FindItem(hwndList, -1, &lvfi);

		if (iitemNew == -1)
		{
			iitemNew = iitemOld;

			// The old current selection is not in the list, so determine which item to select.
			int citem = ListView_GetItemCount(hwndList);
			if ((uint)iitemNew >= (uint)citem)
				iitemNew = citem - 1;
		}
		Assert(iitemNew != -1 || ListView_GetItemCount(hwndList) == 0);
		if (iitemNew != -1)
		{
			ListView_SetItemState(hwndList, iitemNew, LVIS_FOCUSED | LVIS_SELECTED,
				LVIS_FOCUSED | LVIS_SELECTED);
			ListView_EnsureVisible(hwndList, iitemNew, false);
		}
	}

	if (ListView_GetItemCount(hwndList) > 0)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidDeleteSort), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidCopySort), TRUE);
	}
	else
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidDeleteSort), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidCopySort), FALSE);
		// Totally disable and clear the dialog fields when there aren't any sort methods.
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortIncludeSubrecords), FALSE);
		::CheckDlgButton(m_hwnd, kctidSortIncludeSubrecords, BST_UNCHECKED);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByFirst), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByFirstReversed), FALSE);
		::CheckDlgButton(m_hwnd, kctidSortByFirstReversed, BST_UNCHECKED);
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByFirstField), _T(""));
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByFirstLanguage), _T(""));
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortBySecond), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortThenBySecond), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortBySecondReversed), FALSE);
		::CheckDlgButton(m_hwnd, kctidSortBySecondReversed, BST_UNCHECKED);
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortBySecondField), _T(""));
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortBySecondLanguage), _T(""));
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByThird), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortThenByThird), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidSortByThirdReversed), FALSE);
		::CheckDlgButton(m_hwnd, kctidSortByThirdReversed, BST_UNCHECKED);
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByThirdField), _T(""));
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortByThirdLanguage), _T(""));
	}

	::SendMessage(hwndList, WM_SETREDRAW, true, 0);
	::InvalidateRect(hwndList, NULL, true);
}

/*----------------------------------------------------------------------------------------------
	Create a new sort method to be added to the database.  If psmi is not NULL, the new sort
	method will be copied from it.

	@param pszName Initial name of the new sort method.
	@param psmi Pointer to an existing sort method to copy (may be NULL).
----------------------------------------------------------------------------------------------*/
void TlsOptDlgSort::InsertSortMethod(const wchar * pszName, SortMethodInfo * psmi)
{
	AssertPsz(pszName);
	AssertPtrN(psmi);

	HWND hwndList = ::GetDlgItem(m_hwnd, kctidSortList);

	StrApp strName(pszName);
	m_ptod->FixName(strName, hwndList, psmi != NULL);
	StrUni stuName(strName);

	SortMethodInfo smi;
	smi.m_sms = ksmsInserted;
	ISilDataAccessPtr qsda;
	CheckHr(m_qodde->QueryInterface(IID_ISilDataAccess, (void **)&qsda));
	qsda->MakeNewObject(kclidCmSortSpec, m_prmwMain->GetLpInfo()->GetLpId(),
		kflidLangProject_SortSpecs, -1, &(smi.m_hvo));
	smi.m_stuName = stuName;

	if (psmi)
	{
		smi.m_fIncludeSubfields = psmi->m_fIncludeSubfields;
		for (int iski = 0; iski < kcski; ++iski)
		{
			smi.m_rgski[iski].m_vflid = psmi->m_rgski[iski].m_vflid;
			smi.m_rgski[iski].m_ws = psmi->m_rgski[iski].m_ws;
			smi.m_rgski[iski].m_coll = psmi->m_rgski[iski].m_coll;
			smi.m_rgski[iski].m_fReverse = psmi->m_rgski[iski].m_fReverse;
		}
	}
	else
	{
		smi.m_fIncludeSubfields = false;
		for (int iski = 0; iski < kcski; ++iski)
		{
			smi.m_rgski[iski].m_ws = 0;
			smi.m_rgski[iski].m_coll = 0;
			smi.m_rgski[iski].m_fReverse = false;
		}
	}

	m_vsmi.Push(smi);
	m_isrtCurrent = -1;
	UpdateSortMethodList();
	m_isrtCurrent = m_vsmi.Size() - 1;

	// Select the new sort method in the list.
	LVFINDINFO lvfi = { LVFI_PARAM };
	lvfi.lParam = m_isrtCurrent;
	int iitem = ListView_FindItem(hwndList, -1, &lvfi);
	Assert(iitem != -1);
	ListView_SetItemState(hwndList, iitem, LVIS_FOCUSED | LVIS_SELECTED,
		LVIS_FOCUSED | LVIS_SELECTED);
	ListView_EnsureVisible(hwndList, iitem, false);
	::SendMessage(m_hwnd, WM_NEXTDLGCTL, (WPARAM)hwndList, true);
	ListView_EditLabel(hwndList, iitem);
	InitializeWithSortMethod(m_isrtCurrent);
}

/*----------------------------------------------------------------------------------------------
	Delete the currently selected sort method in the list view.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgSort::DeleteSortMethod()
{
	Assert((uint)m_isrtCurrent < (uint)m_vsmi.Size());

	StrApp strTitle(kstidDeleteSortMethod);
	StrApp strPrompt(kstidSortDelMethodMsg);

	const achar * pszHelpUrl = m_pszHelpUrl;
	m_pszHelpUrl = _T("Basic_Tasks/Sorting/Delete_a_sort_method.htm");

	ConfirmDeleteDlgPtr qcdd;
	qcdd.Create();
	qcdd->SetTitle(strTitle.Chars());
	qcdd->SetPrompt(strPrompt.Chars());
	qcdd->SetHelpUrl(m_pszHelpUrl);
	// Make sure the user really wants to delete the sort method.
	if (qcdd->DoModal(m_hwnd) == kctidOk)
	{
		WaitCursor wc;

		SortMethodInfo & smi = m_vsmi[m_isrtCurrent];
		Assert(smi.m_sms != ksmsDeleted);
		smi.m_sms = ksmsDeleted;

		UpdateSortMethodList();
	}
	m_pszHelpUrl = pszHelpUrl;
}

/*----------------------------------------------------------------------------------------------
	Set the sort key info according to the menu path stored in vsmn.

	@param vsmn Reference to the input column of sort menu nodes.
	@param ski Reference to the output sort key information structure.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgSort::UpdateSortKeyInfo(SortMenuNodeVec & vsmn, SortKeyInfo & ski)
{
	ski.m_vflid.Clear();
	ski.m_ws = 0;
	ski.m_coll = 0;
	int csmn = vsmn.Size();
	Assert(csmn > 0);
	for (int ismn = 0; ismn < csmn; ismn++)
	{
		switch (vsmn[ismn]->m_smnt)
		{
		case ksmntClass:
			ski.m_vflid.Push(vsmn[ismn]->m_clid);
			break;
		case ksmntField:
		case ksmntLeaf:
			ski.m_vflid.Push(vsmn[ismn]->m_flid);
			break;
		case ksmntWs:
			ski.m_ws = vsmn[ismn]->m_ws;
			break;
		case ksmntColl:
			ski.m_coll = vsmn[ismn]->m_coll;
			break;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Convert the ws and coll index into a string containing either the collation's database id,
	or the word "null".
----------------------------------------------------------------------------------------------*/
void TlsOptDlgSort::GetUpdateIdStrings(SortKeyInfo ski, StrUni & stuWs, StrUni & stuColl)
{
	if (ski.m_ws)
	{
		stuWs.Format(L"%d", ski.m_ws);
		int hvoColl;
		IWritingSystemPtr qws;
		ICollationPtr qcoll;
		CheckHr(m_qwsf->get_EngineOrNull(ski.m_ws, &qws));
		AssertPtr(qws);
		CheckHr(qws->get_Collation(ski.m_coll, &qcoll));
		AssertPtr(qcoll);
		CheckHr(qcoll->get_Hvo(&hvoColl));
		Assert(hvoColl);
		stuColl.Format(L"%d", hvoColl);
	}
	else
	{
		stuWs.Assign(L"null");
		stuColl.Assign(L"null");
	}
}

/*----------------------------------------------------------------------------------------------
	Convert the ws and coll database id into the collation's index for the writing system.
----------------------------------------------------------------------------------------------*/
int TlsOptDlgSort::GetCollationIndex(int ws, int hvoColl)
{

	if (ws && hvoColl)
	{
		IWritingSystemPtr qws;
		ICollationPtr qcoll;
		CheckHr(m_qwsf->get_EngineOrNull(ws, &qws));
		AssertPtr(qws);
		int ccoll;
		CheckHr(qws->get_CollationCount(&ccoll));
		int icoll;
		for (icoll = 0; icoll < ccoll; ++icoll)
		{
			int hvo;
			CheckHr(qws->get_Collation(icoll, &qcoll));
			AssertPtr(qcoll);
			CheckHr(qcoll->get_Hvo(&hvo));
			Assert(hvo);
			if (hvo == hvoColl)
				return icoll;
		}
	}
	// If all else fails, assume first (default) collation.
	return 0;
}



/*----------------------------------------------------------------------------------------------
	Load all sort method information from the database for this application.

	@param pdbi Pointer to the application database information.
	@param pguidApp Pointer to application's GUID.

	@return True if successful, otherwise false.
----------------------------------------------------------------------------------------------*/
bool SortMethodUtil::LoadSortMethods(AfDbInfo * pdbi, const GUID * pguidApp)
{
	AssertPtr(pdbi);
	AssertPtr(pguidApp);

	// If we've already loaded the sort methods from the database, don't do it again.
	if (pdbi->GetSortCount() != 0)
		return true;

	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stuQuery;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;

	//  Obtain pointer to IOleDbEncap interface.
	pdbi->GetDbAccess(&qode);

	try
	{
		CheckHr(qode->CreateCommand(&qodc));
		stuQuery.Format(
			L"SELECT id, ClassId, Name, IncludeSubentries,%n"
			L"    PrimaryField, PrimaryWs, PrimaryCollation, PrimaryReverse,%n"
			L"    SecondaryField, SecondaryWs, SecondaryCollation, SecondaryReverse,%n"
			L"    TertiaryField, TertiaryWs, TertiaryCollation, TertiaryReverse%n"
			L"FROM CmSortSpec WHERE App = '%g' ORDER BY Name",
			pguidApp);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));

		// Read the sort method information from the CmSortSpec table in the database.
		Vector<wchar> vch;
		vch.Resize(MAX_PATH);
		int cch;
		AppSortInfo asi;
		asi.m_fIncludeSubfields = false;	// No constructor to initialize these properly.
		asi.m_fPrimaryReverse = false;
		asi.m_fSecondaryReverse = false;
		asi.m_fTertiaryReverse = false;
		// We cannot read boolean values directly from the database.  For some reason,
		// trying to do so results in the boolean being set to false every time.  So we
		// use an int (fT) as an intermediate value.
		int fT = 0;
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&asi.m_hvo),
				isizeof(asi.m_hvo), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&asi.m_clidRec),
				isizeof(asi.m_clidRec), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(vch.Begin()),
				vch.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				cch = cbSpaceTaken / isizeof(wchar);
				if (cch > vch.Size())
				{
					vch.Resize(cch);
					CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(vch.Begin()),
						vch.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
				}
				asi.m_stuName.Assign(vch.Begin(), cch);
			}
			else
			{
				asi.m_stuName.Clear();
			}
			CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&fT), isizeof(fT),
				&cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
				asi.m_fIncludeSubfields = fT & 0xFFFF;
			CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(vch.Begin()),
				vch.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				cch = cbSpaceTaken / isizeof(wchar);
				if (cch > vch.Size())
				{
					vch.Resize(cch);
					CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(vch.Begin()),
						vch.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
				}
				asi.m_stuPrimaryField.Assign(vch.Begin(), cch);
			}
			else
			{
				asi.m_stuPrimaryField.Clear();
			}
			CheckHr(qodc->GetColValue(6, reinterpret_cast<BYTE *>(&asi.m_wsPrimary),
				isizeof(asi.m_wsPrimary), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(7, reinterpret_cast<BYTE *>(&asi.m_collPrimary),
				isizeof(asi.m_collPrimary), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(8, reinterpret_cast<BYTE *>(&fT), isizeof(fT),
				&cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
				asi.m_fPrimaryReverse = fT & 0xFFFF;
			CheckHr(qodc->GetColValue(9, reinterpret_cast<BYTE *>(vch.Begin()),
				vch.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				cch = cbSpaceTaken / isizeof(wchar);
				if (cch > vch.Size())
				{
					vch.Resize(cch);
					CheckHr(qodc->GetColValue(9, reinterpret_cast<BYTE *>(vch.Begin()),
						vch.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
				}
				asi.m_stuSecondaryField.Assign(vch.Begin(), cch);
			}
			else
			{
				asi.m_stuSecondaryField.Clear();
			}
			CheckHr(qodc->GetColValue(10, reinterpret_cast<BYTE *>(&asi.m_wsSecondary),
				isizeof(asi.m_wsSecondary), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(11, reinterpret_cast<BYTE *>(&asi.m_collSecondary),
				isizeof(asi.m_collSecondary), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(12, reinterpret_cast<BYTE *>(&fT), isizeof(fT),
				&cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
				asi.m_fSecondaryReverse = fT & 0xFFFF;
			CheckHr(qodc->GetColValue(13, reinterpret_cast<BYTE *>(vch.Begin()),
				vch.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				cch = cbSpaceTaken / isizeof(wchar);
				if (cch > vch.Size())
				{
					vch.Resize(cch);
					CheckHr(qodc->GetColValue(13, reinterpret_cast<BYTE *>(vch.Begin()),
						vch.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
				}
				asi.m_stuTertiaryField.Assign(vch.Begin(), cch);
			}
			else
			{
				asi.m_stuTertiaryField.Clear();
			}
			CheckHr(qodc->GetColValue(14, reinterpret_cast<BYTE *>(&asi.m_wsTertiary),
				isizeof(asi.m_wsTertiary), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(15, reinterpret_cast<BYTE *>(&asi.m_collTertiary),
				isizeof(asi.m_collTertiary), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(16, reinterpret_cast<BYTE *>(&fT), isizeof(fT),
				&cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
				asi.m_fTertiaryReverse = fT & 0xFFFF;
			if (asi.m_stuName.Length() && asi.m_stuPrimaryField.Length())
			{
				SortMethodUtil::CheckMultiOutput(pdbi, asi);
				pdbi->AddSort(asi);
			}

			CheckHr(qodc->NextRow(&fMoreRows));
		}
	}
	catch (...)
	{
		return false;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Parse the field path string to fill in the field path vector.

	@param stuField Input string containing a comma delimited list of field ids (first item is
					class id).
	@param vflid Output vector containing the list of field ids.
----------------------------------------------------------------------------------------------*/
void SortMethodUtil::ParseFieldPath(StrUni & stuFieldPath, Vector<int> & vflidPath)
{
	StrAnsi staFieldPath(stuFieldPath);
	vflidPath.Clear();

	const char * pszPath = staFieldPath.Chars();
	if (!pszPath || !*pszPath)
		return;						// Empty string is okay: no path to parse.

	char * psz;
	int nT = (int)strtol(pszPath, &psz, 10);
	if (nT <= 0)
		ThrowHr(E_UNEXPECTED);
	vflidPath.Push(nT);
	while (psz && *psz == ',')
	{
		nT = (int)strtol(psz+1, &psz, 10);
		vflidPath.Push(nT);
	}
}

/*----------------------------------------------------------------------------------------------
	Fill in the field path string from the field path vector.

	@param vflidPath Input vector containing a list of field ids (first item is a class id).
	@param stuFieldPath Output string containing the list of field ids, separated by commas.
----------------------------------------------------------------------------------------------*/
void SortMethodUtil::CreateFieldPath(Vector<int> & vflidPath, StrUni & stuFieldPath)
{
	stuFieldPath.Clear();
	for (int iflid = 0; iflid < vflidPath.Size(); ++iflid)
	{
		if (stuFieldPath.Length())
			stuFieldPath.Append(L",");
		stuFieldPath.FormatAppend(L"%d", vflidPath[iflid]);
	}
}

/*----------------------------------------------------------------------------------------------
	Build a join statement from the vector of class/field ids in vflidPath, where the object id
	matches the value given by pszId.

	@param pasi Pointer to the sort method definition.
	@param pszId Input string containing the id value to join on.
	@param pmdc Pointer to the FieldWorks meta-data cache.
	@param pwsf Pointer to an writing system factory (used to obtain collation values).
	@param pasiXref Pointer to the additional sort method information used by cross reference
					fields.
	@param stuTable Input/Output string containing a table declaration.  Cleared if the temp
					table is not needed.  Contains only the declarations of mandatory columns
					on input.
	@param stuAddSel Input/Output string containing additions to the SELECT list of columns.
					Appended to only as needed.
	@param stuJoin Output string containing the SQL "JOIN" statement(s) segment.
	@param stuOrder Input/Output string containing the SQL "ORDER BY" statement segment
					referring to the appropriate fields in the tables joined in stuJoin.
					Cleared if the temp table is not needed.  Contains the beginning of a
					SELECT statement with the mandatory columns listed on input.
----------------------------------------------------------------------------------------------*/
void SortMethodUtil::BuildSqlPieces(AppSortInfo * pasi, const wchar * pszId,
	IFwMetaDataCache * pmdc, ILgWritingSystemFactory * pwsf, AppSortInfo * pasiXref,
	AppSortInfo * pasiDef, StrUni & stuTable, StrUni & stuAddSel, StrUni & stuJoin,
	StrUni & stuOrder)
{
	AssertPtr(pasi);
	AssertPsz(pszId);
	AssertPtr(pmdc);
	AssertPtrN(pasiXref);
	AssertPtrN(pasiDef);

	stuJoin.Clear();
	stuAddSel.Clear();

/*
	PRELIMINARY EXAMPLE SKETCHED BY Steve Miller
	--------------------------------------------
	-- Create the table variable, necessary because of the collate clause:

	declare @Sort table (
		objid int,
		TxtAnal nvarchar(4000) collate Latin1_General_CS_AS_KS_WS,
		TxtVern nvarchar(4000),
		TxtVernKey varbinary(whatever)
		)

	-- Load the table variable. Since the collation has been set
	-- for the analysis language, the data will be loaded for it:

	insert into @Sort
		select m1.obj, m1.Txt, m2.Txt,
			xp_GetSortKey('fr_CAN', m2.txt)		-- stored proc to get ICU key
		from MultiTxt$ m1
			join MultiTxt$ m2 on m2.Obj = m1.obj and m2.Flid = m1.flid
			where m1.Ws = @EncAnal and m2.Ws = @EncVern

	-- Now issue the select, with our primary order deteremined by
	-- the collation, and the secondary order determined by our
	-- call to a stored proc, which in turn called the ICU stuff.
	-- "Order by 1, 3" says to order by the first and third columns:

	select objid, TxtAnal, TxtVern from @Sort order by 1, 3
*/

	// Parse the sort method info into the separate sort keys.
	Vector<SortKeyInfo> vski;
	int iski;
	bool fUsesColl = false;
	// Handle sorting by a field that doesn't occur in all records (for example, sorting by an
	// RnEvent field that doesn't exist in RnAnalysis records).  Entries with no value set sort
	// to the top, but entries with nonexistent values should sort to the bottom.
#if 1
	Vector<bool> vfUsesSubtype;
	Vector<int> vclidSubtype;
	Vector<bool> vfRedundant;
#else
	bool rgfUsesSubtype[3] = { false, false, false };
	int rgclidSubtype[3] = { 0, 0, 0};
	int rgfRedundant[3] = { false, false, false };
#endif
	if (pasi->m_stuPrimaryField.Length())
	{
		vski.Resize(1);
		SortMethodUtil::ParseFieldPath(pasi->m_stuPrimaryField, vski[0].m_vflid);
		vski[0].m_ws = pasi->m_wsPrimary;
		vski[0].m_coll = pasi->m_collPrimary;
		vski[0].m_fReverse = pasi->m_fPrimaryReverse;
	}
	if (pasi->m_stuSecondaryField.Length())
	{
		vski.Resize(2);
		SortMethodUtil::ParseFieldPath(pasi->m_stuSecondaryField, vski[1].m_vflid);
		vski[1].m_ws = pasi->m_wsSecondary;
		vski[1].m_coll = pasi->m_collSecondary;
		vski[1].m_fReverse = pasi->m_fSecondaryReverse;
	}
	if (pasi->m_stuTertiaryField.Length())
	{
		vski.Resize(3);
		SortMethodUtil::ParseFieldPath(pasi->m_stuTertiaryField, vski[2].m_vflid);
		vski[2].m_ws = pasi->m_wsTertiary;
		vski[2].m_coll = pasi->m_collTertiary;
		vski[2].m_fReverse = pasi->m_fTertiaryReverse;
	}
	if (pasiXref)
	{
		// Check whether we need to fix the sort method to handle a cross reference field.
		AdjustSortMethodForCrossReferences(pasiXref, pmdc, vski);
	}
	if (pasiDef && pasiDef != pasi && !pasi->m_stuSecondaryField.Length() &&
		pasi->m_stuPrimaryField != pasiDef->m_stuPrimaryField)
	{
		// Add the default secondary key (the default sort's primary key).
		SortKeyInfo ski;
		SortMethodUtil::ParseFieldPath(pasiDef->m_stuPrimaryField, ski.m_vflid);
		ski.m_ws = pasiDef->m_wsPrimary;
		ski.m_coll = pasiDef->m_collPrimary;
		ski.m_fReverse = pasiDef->m_fPrimaryReverse;
		vski.Insert(1, ski);
	}
#if 1
	vfUsesSubtype.Resize(vski.Size());
	vclidSubtype.Resize(vski.Size());
	vfRedundant.Resize(vski.Size());
#endif
	for (iski = 0; iski < vski.Size(); ++iski)
	{
		SortKeyInfo & ski = vski[iski];
		if (ski.m_ws)
			fUsesColl = true;
#if 1
		if (pasi->m_clidRec != ski.m_vflid[0])
			vfUsesSubtype[iski] = true;
		vclidSubtype[iski] = ski.m_vflid[0];
#else
		if (pasi->m_clidRec != ski.m_vflid[0])
			rgfUsesSubtype[iski] = true;
		rgclidSubtype[iski] = ski.m_vflid[0];
#endif
		// Check whether this sort field essentially repeats a previous sort field.
		if (iski)
		{
			for (int iski2 = iski; iski2; )
			{
				--iski2;
				SortKeyInfo & ski2 = vski[iski2];
				bool fSame = (ski.m_ws == ski2.m_ws &&
					ski.m_coll == ski2.m_coll);
				if (fSame)
				{
					if (ski.m_vflid.Size() != ski2.m_vflid.Size())
					{
						fSame = false;
					}
					else
					{
						for (int iflid = 0; iflid < ski.m_vflid.Size(); ++iflid)
						{
							if (ski.m_vflid[iflid] != ski2.m_vflid[iflid])
							{
								fSame = false;
								break;
							}
						}
					}
				}
				if (fSame)
				{
#if 1
					vfRedundant[iski] = true;
#else
					rgfRedundant[iski] = true;
#endif
					break;
				}
			}
		}
	}

	bool fUseTable = false;
	StrUni stuOrdSel;
	Set<StrUni, HashStrUni, EqlStrUni> setTabId;
#if 1
	if (fUsesColl || vfUsesSubtype[0] || vfUsesSubtype[1] || vfUsesSubtype[2])
#else
	if (fUsesColl || rgfUsesSubtype[0] || rgfUsesSubtype[1] || rgfUsesSubtype[2])
#endif
	{
		StrUni stuTab;
		stuTab.Format(L"DECLARE @SrtTemp TABLE (%n%s", stuTable.Chars());
		stuTable.Assign(stuTab);
		stuOrdSel.Assign(stuOrder);		// L"SELECT ObjId, ClsId, OwnId"
		stuOrder.Format(L"%nFROM @SrtTemp ORDER BY ");
		fUseTable = true;
	}
	else
	{
		// Don't need stuTable if not collating, and stuOrder must start empty.
		stuTable.Clear();
		stuOrder.Clear();
	}

	StrUni stuId;
	StrUni stuT;
	StrUni stuTabOrd;		// ORDER BY clause when using temp table for collation.

	HashMapStrUni<StrUni> hmstuJoinstuAlias;
	for (iski = 0; iski < vski.Size(); ++iski)
	{
#if 1
		if (vfRedundant[iski])
#else
		if (rgfRedundant[iski])
#endif
			continue;
		SortKeyInfo & ski = vski[iski];
		stuId.Assign(pszId);
#if 1
		if (vfUsesSubtype[iski])
#else
		if (rgfUsesSubtype[iski])
#endif
		{
			if (stuTabOrd.Length())
				stuTabOrd.FormatAppend(L", Type%d ASC", iski);
			else
				stuTabOrd.Format(L"Type%d ASC", iski);
		}
		// Skip the class id at the beginning of field path vector.
		for (int iflid = 1; iflid < ski.m_vflid.Size(); ++iflid)
		{
			int flid = ski.m_vflid[iflid];
			SmartBstr sbstrField;
			SmartBstr sbstrClass;
			SmartBstr sbstrDstCls;
			int nType;
			StrUni stuAlias;		// alias for the current table being joined.
			StrUni stuAlias2;		// alias for the second table being joined for this field.
			StrUni stuTabName;		// temp table entry name for current sort field.
			bool fLeaf = (iflid == ski.m_vflid.Size() - 1);
			CheckHr(pmdc->GetFieldName(flid, &sbstrField));
			CheckHr(pmdc->GetOwnClsName(flid, &sbstrClass));
			CheckHr(pmdc->GetFieldType(flid, &nType));
			switch (nType)
			{
			case kcptBoolean:
			case kcptInteger:
			case kcptNumeric:
			case kcptFloat:
			case kcptTime:
			case kcptGuid:
			case kcptGenDate:
			case kcptBinary:
			case kcptUnicode:
			case kcptString:
				// Simple value.
				Assert(fLeaf);		// This must be the leaf (final) node of the path.
				Assert(nType != kcptString || ski.m_ws);
				stuT.Format(L"%s id %s", sbstrClass.Chars(), stuId.Chars());
				if (!hmstuJoinstuAlias.Retrieve(stuT, &stuAlias))
				{
					stuAlias.Format(L"s%df%d", iski + 1, iflid);
					stuJoin.FormatAppend(L"LEFT OUTER JOIN %s %s ON %s.id = %s%n",
						sbstrClass.Chars(), stuAlias.Chars(), stuAlias.Chars(), stuId.Chars());
					hmstuJoinstuAlias.Insert(stuT, stuAlias);
				}
				break;

			case kcptMultiString:
				// Multilingual formatted string.
				Assert(fLeaf);		// This must be the leaf (final) node of the path.
				Assert(ski.m_ws);
				stuT.Format(L"MultiStr$ %s %d %d", stuId.Chars(), flid, ski.m_ws);
				if (!hmstuJoinstuAlias.Retrieve(stuT, &stuAlias))
				{
					stuAlias.Format(L"s%df%d", iski + 1, iflid);
					stuJoin.FormatAppend(L"LEFT OUTER JOIN MultiStr$ %s ON %s.Obj = %s"
						L" AND %s.Flid = %d AND %s.Ws = %d%n",
						stuAlias.Chars(), stuAlias.Chars(), stuId.Chars(),
						stuAlias.Chars(), flid, stuAlias.Chars(), ski.m_ws);
					hmstuJoinstuAlias.Insert(stuT, stuAlias);
				}
				break;

			case kcptMultiUnicode:
				// Multilingual simple string.
				Assert(fLeaf);		// This must be the leaf (final) node of the path.
				Assert(ski.m_ws);
				// Class and Field names have been declared above
				stuT.Format(L"%s_%s %s %d %d", sbstrClass.Chars(), sbstrField.Chars(),
					stuId.Chars(), flid, ski.m_ws);

				if (!hmstuJoinstuAlias.Retrieve(stuT, &stuAlias))
				{
					stuAlias.Format(L"s%df%d", iski + 1, iflid);
					stuJoin.FormatAppend(L"LEFT OUTER JOIN %s_%s %s"
						L" ON %s.Obj = %s AND %s.Ws = %d%n",
						sbstrClass.Chars(), sbstrField.Chars(), stuAlias.Chars(),
						stuAlias.Chars(), stuId.Chars(), stuAlias.Chars(), ski.m_ws);
				}
				break;

			case kcptImage:
			case kcptBigString:
			case kcptMultiBigString:
			case kcptBigUnicode:
			case kcptMultiBigUnicode:
				// Cannot sort on these: implemented with 'image' and 'ntext' data types.
				Assert(false);			// THIS SHOULD NEVER HAPPEN!
				Assert(fLeaf);			// This must be the leaf (final) node of the path.
				stuTable.Clear();
				stuAddSel.Clear();
				stuJoin.Clear();
				stuOrder.Clear();
				return;

			case kcptOwningAtom:
			case kcptReferenceAtom:
				// Atomic reference.
				Assert(!fLeaf);		// This must not be the leaf (final) node of the path.
				stuT.Format(L"%s id %s", sbstrClass.Chars(), stuId.Chars());
				if (!hmstuJoinstuAlias.Retrieve(stuT, &stuAlias))
				{
					stuAlias.Format(L"s%df%d", iski + 1, iflid);
					stuJoin.FormatAppend(L"LEFT OUTER JOIN %s %s ON %s.id = %s%n",
						sbstrClass.Chars(), stuAlias.Chars(), stuAlias.Chars(), stuId.Chars());
					hmstuJoinstuAlias.Insert(stuT, stuAlias);
				}
				stuId.Format(L"%s.%s", stuAlias.Chars(), sbstrField.Chars());
				break;

			case kcptOwningCollection:
			case kcptReferenceCollection:
			case kcptOwningSequence:
			case kcptReferenceSequence:
				Assert(!fLeaf);
				stuT.Format(L"%s_%s src %s",
					sbstrClass.Chars(), sbstrField.Chars(), stuId.Chars());
				if (!hmstuJoinstuAlias.Retrieve(stuT, &stuAlias))
				{
					stuAlias.Format(L"s%df%d", iski + 1, iflid);
					stuJoin.FormatAppend(L"LEFT OUTER JOIN %s_%s %s ON %s.Src = %s%n",
						sbstrClass.Chars(), sbstrField.Chars(), stuAlias.Chars(),
						stuAlias.Chars(), stuId.Chars());
					hmstuJoinstuAlias.Insert(stuT, stuAlias);
				}
				stuId.Format(L"%s.Dst", stuAlias.Chars());
				break;
			default:
				Assert(false);		// THIS SHOULD NEVER HAPPEN!!
				break;
			}
			if (fLeaf)
			{
				if (fUseTable)
				{
					const wchar * pszType;
					// Define the type for a table entry.
					switch (nType)
					{
					case kcptBoolean:
						pszType = L"bit";
						break;
					case kcptInteger:
						pszType = L"int";
						break;
					case kcptNumeric:
						pszType = L"decimal(28,4)";
						break;
					case kcptFloat:
						pszType = L"float";
						break;
					case kcptTime:
						pszType = L"datetime";
						break;
					case kcptGuid:
						pszType = L"uniqueidentifier";
						break;
					case kcptGenDate:
						pszType = L"int";
						break;
					case kcptBinary:
						pszType = L"varbinary(8000)";
						break;
					case kcptUnicode:
					case kcptString:
					case kcptMultiString:
					case kcptMultiUnicode:
						pszType = L"nvarchar(4000)";
						break;
					default:
						Assert(false);		// THIS SHOULD NEVER HAPPEN!
						pszType = L"";
						break;
					}
					stuTabName.Format(L"%s_%s", stuAlias.Chars(), sbstrField.Chars());
					stuTable.FormatAppend(L",%n    %s %s", stuTabName.Chars(), pszType);
					if (nType == kcptString ||
						nType == kcptMultiString ||
						nType == kcptMultiUnicode)
					{
						IWritingSystemPtr qws;
						CheckHr(pwsf->get_EngineOrNull(ski.m_ws, &qws));
						if (qws)
						{
							ICollationPtr qcoll;
							int ccoll;
							CheckHr(qws->get_CollationCount(&ccoll));
							if (ski.m_coll < ccoll)
								CheckHr(qws->get_Collation(ski.m_coll, &qcoll));
//-							CheckHr(qws->GetCollationForCode(ski.m_coll, &qcoll));
							if (!qcoll && !ski.m_coll)
								CheckHr(qws->get_Collation(0, &qcoll));
							if (qcoll)
							{
								SmartBstr sbstr;
								CheckHr(qcoll->get_WinCollation(&sbstr));
								if (sbstr.Length())
								{
									stuTable.FormatAppend(L" COLLATE %s", sbstr.Chars());
								}
								else
								{
									// TODO SteveMc: HANDLE ICU COLLATION STUFF????
								}
							}
						}
					}
					stuT.Format(L"%s_Id", stuAlias.Chars());
					bool fIdInTable = setTabId.IsMember(stuT);
					if (!fIdInTable)
					{
						stuTable.FormatAppend(L",%n    %s int", stuT.Chars());
						setTabId.Insert(stuT);
					}
					stuOrdSel.FormatAppend(L", %s", stuT.Chars());
					if (nType == kcptMultiString || nType == kcptMultiUnicode)
					{
						stuAddSel.FormatAppend(L", %s.Txt", stuAlias.Chars());
						if (!fIdInTable)
							stuAddSel.FormatAppend(L",%s.Obj", stuAlias.Chars());
					}
					else
					{
						stuAddSel.FormatAppend(L", %s.%s",
							stuAlias.Chars(), sbstrField.Chars());
						if (!fIdInTable)
							stuAddSel.FormatAppend(L",%s.Id", stuAlias.Chars());
					}
					stuT.Format(L"%s %s",
						stuTabName.Chars(), ski.m_fReverse ? L"DESC" : L"ASC");
					if (stuTabOrd.Length())
						stuTabOrd.FormatAppend(L", %s", stuT.Chars());
					else
						stuTabOrd.Assign(stuT.Chars());
				}
				else
				{
					stuT.Format(L"%s.%s %s", stuAlias.Chars(), sbstrField.Chars(),
						ski.m_fReverse ? L"DESC" : L"ASC");
					if (stuOrder.Length())
						stuOrder.FormatAppend(L", %s", stuT.Chars());
					else
						stuOrder.Format(L" ORDER BY %s", stuT.Chars());
					stuAddSel.FormatAppend(L", %s.Id, %s.%s",
						stuAlias.Chars(), stuAlias.Chars(), sbstrField.Chars());
				}
			}
		}
#if 1
		if (vfUsesSubtype[iski])
#else
		if (rgfUsesSubtype[iski])
#endif
		{
			Assert(stuTable.Length());
			Assert(stuTabOrd.Length());
			Assert(stuOrder.Length());
			stuTable.FormatAppend(L",%n    Type%d int", iski);
#if 1
			stuAddSel.FormatAppend(L", ABS(t1.Class$ - %d)", vclidSubtype[iski]);
#else
			stuAddSel.FormatAppend(L", ABS(t1.Class$ - %d)", rgclidSubtype[iski]);
#endif
		}
	}

	if (stuTable.Length())
	{
		Assert(stuTabOrd.Length());
		Assert(stuOrder.Length());
		stuTable.FormatAppend(L"%n)%nINSERT INTO @SrtTemp%n");
	}
	if (stuTabOrd.Length())
	{
		Assert(stuTable.Length());
		Assert(stuOrder.Length());
		stuOrder.Replace(0, 0, stuOrdSel);
		stuOrder.Append(stuTabOrd.Chars());
	}
}

/*----------------------------------------------------------------------------------------------

	@param plpi
	@param pasi
	@param hvoTopLevel
	@param flidTop
	@param flidSub
	@param pasiXref
	@param stuQuery
----------------------------------------------------------------------------------------------*/
void SortMethodUtil::GetSortQuery(AfLpInfo * plpi, AppSortInfo * pasi, HVO hvoTopLevel,
	int flidTop, int flidSub, AppSortInfo * pasiXref, AppSortInfo * pasiDef, StrUni & stuQuery)
{
	AssertPtr(plpi);
	AssertPtr(pasi);
	Assert(pasi->m_stuPrimaryField.Length());
	Assert(hvoTopLevel);
	Assert(flidTop);

	stuQuery.Clear();

	AfDbInfoPtr qdbi = plpi->GetDbInfo();
	AssertPtr(qdbi);
	IFwMetaDataCachePtr qmdc;
	qdbi->GetFwMetaDataCache(&qmdc);
	AssertPtr(qmdc);
	ILgWritingSystemFactoryPtr qwsf;
	qdbi->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);
	SmartBstr sbstrClass;
	SmartBstr sbstrField;
	CheckHr(qmdc->GetOwnClsName(flidTop, &sbstrClass));
	CheckHr(qmdc->GetFieldName(flidTop, &sbstrField));

	StrUni stuTable;
	StrUni stuAddSel;
	StrUni stuFrom;
	StrUni stuJoin;
	StrUni stuWhere;
	StrUni stuOrder;

	stuTable.Format(
		L"    ObjId int,%n"
		L"    ClsId int,%n"
		L"    OwnId int");
	stuOrder.Format(L"SELECT ObjId, ClsId, OwnId");
	SortMethodUtil::BuildSqlPieces(pasi, L"t1.id", qmdc, qwsf, pasiXref, pasiDef,
		stuTable, stuAddSel, stuJoin, stuOrder);
#ifdef DEBUG
	if (stuTable.Length())
	{
		Assert(stuAddSel.Length());
		Assert(stuJoin.Length());
		Assert(stuOrder.Length());
	}
#endif
	if (pasi->m_fIncludeSubfields && flidSub)
	{
		stuQuery.Format(L"DECLARE @uid AS uniqueidentifier;%n"
			L"exec GetSubObjects$ @uid output, %d, %d;%n"
			L"exec GetSubObjects$ @uid output, null, %d;%n",
			hvoTopLevel, flidTop, flidSub);
		SmartBstr sbstrTarget;
		CheckHr(qmdc->GetDstClsName(flidTop, &sbstrTarget));
		stuFrom.Format(L"(SELECT * FROM %s_ a WHERE EXISTS (%n"
			L"        SELECT * FROM [ObjInfoTbl$] b%n"
			L"        WHERE a.id = b.objid AND b.uid = @uid)) t1%n",
			sbstrTarget.Chars());
		stuWhere.Clear();
		stuOrder.FormatAppend(L";%nexec CleanObjInfoTbl$ @uid");
	}
	else
	{
		stuFrom.Format(L"%s_%s cf%n"
			L"JOIN CmObject t1 ON t1.id = cf.dst%n",
			sbstrClass.Chars(), sbstrField.Chars());
		stuWhere.Format(L"WHERE cf.Src = %d%n", hvoTopLevel);


	}
	stuQuery.Append(stuTable);
	stuQuery.Append(L"SELECT DISTINCT t1.Id, t1.Class$, t1.Owner$");
	stuQuery.Append(stuAddSel);
	stuQuery.FormatAppend(L"%nFROM %s", stuFrom.Chars());
	stuQuery.Append(stuJoin);
	stuQuery.Append(stuWhere);
	stuQuery.Append(stuOrder);
}

/*----------------------------------------------------------------------------------------------
	Extract the desired enumeration name from the given enumeration string.

	@param stid Resource number of the enumeration string.
	@param iselEnum Index of the desired enumeration.
	@param stu Reference to the output string.
----------------------------------------------------------------------------------------------*/
static void SelectEnumString(int stid, int iselEnum, StrUni & stu)
{
	if (stid)
	{
		StrUni stuEnum(stid);
		Assert(stuEnum.Length());
		Vector<StrUni> vstuEnum;
		const wchar * pszEnum = stuEnum.Chars();
		const wchar * pszEnumTotalLim = stuEnum.Chars() + stuEnum.Length();
		StrUni stuT;
		while (pszEnum < pszEnumTotalLim)
		{
			const wchar * pszEnumLim = wcschr(pszEnum, '\n');
			if (!pszEnumLim)
				pszEnumLim = pszEnumTotalLim;
			stuT.Assign(pszEnum, pszEnumLim - pszEnum);
			vstuEnum.Push(stuT);
			if (iselEnum < vstuEnum.Size())
			{
				stu = vstuEnum[iselEnum];
				return;
			}
			pszEnum = pszEnumLim + 1;
		}
	}
	stu.Format(L"%d", iselEnum);
}

/*----------------------------------------------------------------------------------------------
	Build the current sort key value (and sort key field name) strings for use by the status
	bar display routines.

	@param pasi Pointer to the current sort method.
	@param skh Reference to the current sort key HVO vector.
	@param vsmn Reference to the SortMenuNode tree.
	@param pcvd Pointer to the data cache.
	@param plpi Pointer to the language project info.
	@param pasiXref Pointer to the additional sort info needed for cross references.
	@param stuKeyValue Reference to the output sort key value string.
	@param stuKeyName Reference to the output sort key field name string.
----------------------------------------------------------------------------------------------*/
void SortMethodUtil::GenerateStatusStrings(AppSortInfo * pasi, SortKeyHvos & skh,
	SortMenuNodeVec & vsmn, IVwCacheDa * pcvd, AfLpInfo * plpi, AppSortInfo * pasiXref,
	StrUni & stuKeyValue, StrUni & stuKeyName)
{
	AssertPtr(pasi);
	Assert(pasi->m_stuPrimaryField.Length());
	AssertPtr(pcvd);
	AssertPtr(plpi);

	AfDbInfoPtr qdbi = plpi->GetDbInfo();
	AssertPtr(qdbi);
	IFwMetaDataCachePtr qmdc;
	qdbi->GetFwMetaDataCache(&qmdc);
	AssertPtr(qmdc);
	ISilDataAccessPtr qsda;
	CheckHr(pcvd->QueryInterface(IID_ISilDataAccess, (void **)&qsda));

	Vector<SortKeyInfo> vski;
	Vector<HVO> vhvoKey;
	vski.Resize(1);
	ParseFieldPath(pasi->m_stuPrimaryField, vski[0].m_vflid);
	vski[0].m_ws = pasi->m_wsPrimary;
	vhvoKey.Push(skh.m_hvoPrimary);
	if (pasi->m_stuSecondaryField.Length())
	{
		vski.Resize(2);
		ParseFieldPath(pasi->m_stuSecondaryField, vski[1].m_vflid);
		vhvoKey.Push(skh.m_hvoSecondary);
		vski[1].m_ws = pasi->m_wsSecondary;
	}
	if (pasi->m_stuTertiaryField.Length())
	{
		vski.Resize(3);
		ParseFieldPath(pasi->m_stuTertiaryField, vski[2].m_vflid);
		vhvoKey.Push(skh.m_hvoTertiary);
		vski[2].m_ws = pasi->m_wsTertiary;
	}

	int iski;
	stuKeyName.Clear();
	stuKeyValue.Clear();
	StrUni stuNull(kstidSortNoValue);
	for (iski = 0; iski < vski.Size(); ++iski)
	{
		StrUni stuPath;
		int iflid;
		int ismn;
		if (iski)
		{
			stuKeyName.Append(L" / ");
			stuKeyValue.Append(L" / ");
		}
		SortMenuNodeVec * pvsmn = &vsmn;
		for (iflid = 0, ismn = 0; ismn < pvsmn->Size(); ++ismn)
		{
			int flid = (*pvsmn)[ismn]->m_flid;
			if (flid == vski[iski].m_vflid[iflid])
			{
				if (stuPath.Length())
					stuPath.Append(L": ");
				stuPath.Append((*pvsmn)[ismn]->m_stuText);
				++iflid;
				if (iflid == vski[iski].m_vflid.Size())
				{
					Assert((*pvsmn)[ismn]->m_smnt == ksmntLeaf);
					// Get the value?
					if (vhvoKey[iski])
					{
						int nType;
						int nT;
						__int64 lnT;
						SilTime stim;
						GUID guid;
						Vector<byte> vb;
						int cb;
						SmartBstr sbstr;
						ITsStringPtr qtss;
						CheckHr(qmdc->GetFieldType(flid, &nType));
						if (flid == kflidCmPossibility_Name ||
							flid == kflidCmPossibility_Abbreviation)
						{
							PossItemInfo * ppii;
							PossListInfoPtr qpli;
							if (plpi->GetPossListAndItem(vhvoKey[iski], vski[iski].m_ws,
								&ppii, &qpli))
							{
								StrUni stu;
								PossNameType pnt;
								// pnt = qpli->GetDisplayOption();
								if (flid == kflidCmPossibility_Name)
									pnt = kpntName;
								else
									pnt = kpntAbbreviation;
								ppii->GetName(stu, pnt);
								stuKeyValue.Append(stu);
							}
							else
							{
								stuKeyValue.FormatAppend(
									L"(poss list item (hvo = %d, ws = %d)",
									vhvoKey[iski], vski[iski].m_ws);
							}
						}
						else
						{
						HRESULT hr;
							switch (nType)
							{
							case kcptBoolean:
							case kcptInteger:
								CheckHr(hr = qsda->get_IntProp(vhvoKey[iski], flid, &nT));
								if (hr == S_OK)
								{
									int stid = (*pvsmn)[ismn]->m_stid;
									switch (flid)
									{
									case kflidCmPerson_Gender:
										SelectEnumString(stid, nT, stuKeyValue);
										break;
									case kflidCmPerson_IsResearcher:
										SelectEnumString(stid, nT, stuKeyValue);
										break;
									default:
										if (nType == kcptBoolean)
											SelectEnumString(stid, nT, stuKeyValue);
										else
											stuKeyValue.FormatAppend(L"%d", nT);
										break;
									}
								}
								break;
							case kcptNumeric:
							case kcptFloat:
								CheckHr(hr = qsda->get_Int64Prop(vhvoKey[iski], flid, &lnT));
								if (hr == S_OK)
									stuKeyValue.FormatAppend(L"%08x%08x", lnT);
								break;
							case kcptTime:
								CheckHr(hr = qsda->get_TimeProp(vhvoKey[iski], flid, (__int64 *)&stim));
								if (hr == S_OK)
									stuKeyValue.FormatAppend(L"%T", &stim);
								break;
							case kcptGuid:
								CheckHr(hr = qsda->get_GuidProp(vhvoKey[iski], flid, &guid));
								if (hr == S_OK)
									stuKeyValue.FormatAppend(L"%g", &guid);
								break;
							case kcptGenDate:
								CheckHr(hr = qsda->get_IntProp(vhvoKey[iski], flid, &nT));
								if (hr == S_OK)
									stuKeyValue.FormatAppend(L"%D", nT);
								break;
							case kcptBinary:
								vb.Resize(8000);
								CheckHr(hr = qsda->BinaryPropRgb(vhvoKey[iski], flid, vb.Begin(),
									vb.Size(), &cb));
								if (hr == S_OK && cb)
								{
									int ib;
									for (ib = 0; ib < 8; ++ib)
									{
										if (ib > cb)
											break;
										stuKeyValue.FormatAppend(L"%02x", vb[ib]);
									}
									if (ib < cb)
										stuKeyValue.Append(L"...");
								}
								else
								{
									stuKeyValue.Append(stuNull.Chars());
								}
								break;
							case kcptUnicode:
								CheckHr(qsda->get_UnicodeProp(vhvoKey[iski], flid, &sbstr));
								stuKeyValue.FormatAppend(L"%b", sbstr);
								break;
							case kcptString:
								CheckHr(qsda->get_StringProp(vhvoKey[iski], flid, &qtss));
								CheckHr(qtss->get_Text(&sbstr));
								stuKeyValue.FormatAppend(L"%b", sbstr);
								break;
							case kcptMultiString:
							case kcptMultiUnicode:
								CheckHr(qsda->get_MultiStringAlt(vhvoKey[iski], flid,
									vski[iski].m_ws, &qtss));
								CheckHr(qtss->get_Text(&sbstr));
								stuKeyValue.FormatAppend(L"%b", sbstr);
								break;
							case kcptReferenceAtom:
							case kcptReferenceCollection:
							case kcptReferenceSequence:
								if (pasiXref)
								{
									// Must be cross reference.
									SortKeyInfo rgskiXref[3];
									int cskiXref = 0;
									if (pasiXref->m_stuPrimaryField.Length())
									{
										SortMethodUtil::ParseFieldPath(
											pasiXref->m_stuPrimaryField, rgskiXref[0].m_vflid);
										Assert(rgskiXref[0].m_vflid.Size() == 1);
										rgskiXref[0].m_ws = pasiXref->m_wsPrimary;
										rgskiXref[0].m_coll = pasiXref->m_collPrimary;
										rgskiXref[0].m_fReverse = pasiXref->m_fPrimaryReverse;
										++cskiXref;
									}
									if (pasiXref->m_stuSecondaryField.Length())
									{
										SortMethodUtil::ParseFieldPath(
											pasiXref->m_stuSecondaryField,
											rgskiXref[1].m_vflid);
										Assert(rgskiXref[1].m_vflid.Size() == 1);
										rgskiXref[1].m_ws = pasiXref->m_wsSecondary;
										rgskiXref[1].m_coll = pasiXref->m_collSecondary;
										rgskiXref[1].m_fReverse = pasiXref->m_fSecondaryReverse;
										++cskiXref;
									}
									if (pasiXref->m_stuTertiaryField.Length())
									{
										SortMethodUtil::ParseFieldPath(
											pasiXref->m_stuTertiaryField, rgskiXref[2].m_vflid);
										Assert(rgskiXref[2].m_vflid.Size() == 1);
										rgskiXref[2].m_ws = pasiXref->m_wsTertiary;
										rgskiXref[2].m_coll = pasiXref->m_collTertiary;
										rgskiXref[2].m_fReverse = pasiXref->m_fTertiaryReverse;
										++cskiXref;
									}
									int isx;
									wchar * pszSep = L"";
									for (isx = 0; isx < cskiXref; ++isx)
									{
										int flidX = rgskiXref[isx].m_vflid[0];
										int nTypeX;
										qmdc->GetFieldType(flidX, &nTypeX);
										switch (nTypeX)
										{
										case kcptBoolean:
										case kcptInteger:
											CheckHr(hr = qsda->get_IntProp(vhvoKey[iski], flidX, &nT));
											if (hr == S_OK)
											{
												stuKeyValue.FormatAppend(L"%s%d", pszSep, nT);
												pszSep = L" - ";
											}
											break;
										case kcptNumeric:
										case kcptFloat:
											CheckHr(hr = qsda->get_Int64Prop(vhvoKey[iski],
												flidX, &lnT));
											if (hr == S_OK)
											{
												stuKeyValue.FormatAppend(L"%s%08x%08x",
													pszSep, lnT);
												pszSep = L" - ";
											}
											break;
										case kcptTime:
											CheckHr(hr = qsda->get_TimeProp(vhvoKey[iski], flidX,
												(__int64 *)&stim));
											if (hr == S_OK)
											{
												stuKeyValue.FormatAppend(L"%s%T",
													pszSep, &stim);
												pszSep = L" - ";
											}
											break;
										case kcptGuid:
											CheckHr(hr = qsda->get_GuidProp(vhvoKey[iski], flidX,
												&guid));
											if (hr == S_OK)
											{
												stuKeyValue.FormatAppend(L"%s%g",
													pszSep, &guid);
												pszSep = L" - ";
											}
											break;
										case kcptGenDate:
											CheckHr(hr = qsda->get_IntProp(vhvoKey[iski], flidX, &nT));
											if (hr == S_OK)
											{
												stuKeyValue.FormatAppend(L"%s%D", pszSep, nT);
												pszSep = L" - ";
											}
											break;
										case kcptBinary:
											vb.Resize(8000);
											CheckHr(hr = qsda->BinaryPropRgb(vhvoKey[iski], flidX,
												vb.Begin(), vb.Size(), &cb));
											if (hr == S_OK && cb)
											{
												stuKeyValue.FormatAppend(L"%s", pszSep);
												int ib;
												for (ib = 0; ib < 8; ++ib)
												{
													if (ib > cb)
														break;
													stuKeyValue.FormatAppend(L"%02x", vb[ib]);
												}
												if (ib < cb)
													stuKeyValue.Append(L"...");
											}
											else
											{
												stuKeyValue.FormatAppend(L"%s%s",
													pszSep, stuNull.Chars());
											}
											pszSep = L" - ";
											break;
										case kcptUnicode:
											CheckHr(hr = qsda->get_UnicodeProp(vhvoKey[iski], flidX,
												&sbstr));
											if (hr == S_OK)
											{
												stuKeyValue.FormatAppend(L"%s%b",
													pszSep, sbstr);
												pszSep = L" - ";
											}
											break;
										case kcptString:
											CheckHr(hr = qsda->get_StringProp(vhvoKey[iski], flidX,
												&qtss));
											if (hr == S_OK)
												CheckHr(hr = qtss->get_Text(&sbstr));
											if (hr == S_OK)
											{
												stuKeyValue.FormatAppend(L"%s%b",
													pszSep, sbstr);
												pszSep = L" - ";
											}
											break;
										case kcptMultiString:
										case kcptMultiUnicode:
											CheckHr(hr = qsda->get_MultiStringAlt(vhvoKey[iski], flidX,
												rgskiXref[isx].m_ws, &qtss));
											if (hr == S_OK)
												CheckHr(hr = qtss->get_Text(&sbstr));
											if (hr == S_OK)
											{
												stuKeyValue.FormatAppend(L"%s%b",
													pszSep, sbstr);
												pszSep = L" - ";
											}
											break;
										}
									}
								}
								break;
							default:
								Assert(false);		// THIS SHOULD NEVER HAPPEN!
								break;
							}
						}
					}
					else
					{
						stuKeyValue.Append(stuNull.Chars());
					}
					break;
				}
				// Fudge the loop variables to go one level down into the cascading menus.
				pvsmn = &(*pvsmn)[ismn]->m_vsmnSubItems;
				ismn = -1;
			}
		}
		stuKeyName.Append(stuPath);
	}
}

/*----------------------------------------------------------------------------------------------
	Adjust the sort method stored in vski for any cross reference nodes within.  Also adjust the
	vector of HVOs for the key if needed.

	@param pasiXref Pointer to the additional sort info needed for cross references (may be
					NULL).
	@param vski Reference to the vector of "sort key info" objects which define the sort method.
----------------------------------------------------------------------------------------------*/
void SortMethodUtil::AdjustSortMethodForCrossReferences(AppSortInfo * pasiXref,
	IFwMetaDataCache * pmdc, Vector<SortKeyInfo> & vski)
{
	if (!pasiXref)
		return;

	SortKeyInfo rgskiXref[3];
	int cskiXref = 0;
	if (pasiXref->m_stuPrimaryField.Length())
	{
		SortMethodUtil::ParseFieldPath(pasiXref->m_stuPrimaryField,
			rgskiXref[0].m_vflid);
		rgskiXref[0].m_ws = pasiXref->m_wsPrimary;
		rgskiXref[0].m_coll = pasiXref->m_collPrimary;
		rgskiXref[0].m_fReverse = pasiXref->m_fPrimaryReverse;
		++cskiXref;
	}
	if (pasiXref->m_stuSecondaryField.Length())
	{
		SortMethodUtil::ParseFieldPath(pasiXref->m_stuSecondaryField,
			rgskiXref[1].m_vflid);
		rgskiXref[1].m_ws = pasiXref->m_wsSecondary;
		rgskiXref[1].m_coll = pasiXref->m_collSecondary;
		rgskiXref[1].m_fReverse = pasiXref->m_fSecondaryReverse;
		++cskiXref;
	}
	if (pasiXref->m_stuTertiaryField.Length())
	{
		SortMethodUtil::ParseFieldPath(pasiXref->m_stuTertiaryField,
			rgskiXref[2].m_vflid);
		rgskiXref[2].m_ws = pasiXref->m_wsTertiary;
		rgskiXref[2].m_coll = pasiXref->m_collTertiary;
		rgskiXref[2].m_fReverse = pasiXref->m_fTertiaryReverse;
		++cskiXref;
	}
	int iski;
	for (iski = 0; iski < vski.Size(); ++iski)
	{
		SortKeyInfo & ski = vski[iski];
		int iflid = ski.m_vflid.Size() - 1;
		int flid = ski.m_vflid[iflid];
		int nType;
		CheckHr(pmdc->GetFieldType(flid, &nType));
		if (nType == kcptReferenceAtom || nType == kcptReferenceCollection ||
			nType == kcptReferenceSequence)
		{
			// Must be a cross reference field: merge the additional information,
			// possibly adding one or two new entries to the sort info array.
			if (cskiXref > 1)
				vski.Insert(iski+1, ski);
			if (cskiXref > 2)
				vski.Insert(iski+1, ski);
			for (int isx = 0; isx < cskiXref; ++isx)
			{
				vski[iski+isx].m_vflid.InsertMulti(vski[iski+isx].m_vflid.Size(),
					rgskiXref[isx].m_vflid.Size(), rgskiXref[isx].m_vflid.Begin());
				vski[iski+isx].m_ws = rgskiXref[isx].m_ws;
				vski[iski+isx].m_coll = rgskiXref[isx].m_coll;
			}
			iski += cskiXref - 1;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Set asi.m_fMultiOutput appropriately for the sort method defined by asi.  (TRUE if each
	entry/item can produce multiple items in the sorted list.)

	@param pdbi Pointer to the database info.
	@param asi Reference to the sort method info.
----------------------------------------------------------------------------------------------*/
void SortMethodUtil::CheckMultiOutput(AfDbInfo * pdbi, AppSortInfo & asi)
{
	AssertPtr(pdbi);
	IFwMetaDataCachePtr qmdc;
	pdbi->GetFwMetaDataCache(&qmdc);
	AssertPtr(qmdc);

	asi.m_fMultiOutput = false;
	Vector<int> vflid;
	SortMethodUtil::ParseFieldPath(asi.m_stuPrimaryField, vflid);
	int proptype;
	int iflid;
	for (iflid = 1; iflid < vflid.Size(); ++iflid)
	{
		qmdc->GetFieldType(vflid[iflid], &proptype);
		if (proptype == kcptOwningCollection ||
			proptype == kcptReferenceCollection ||
			proptype == kcptOwningSequence ||
			proptype == kcptReferenceSequence)
		{
			asi.m_fMultiOutput = true;
			return;
		}
	}
	if (!asi.m_fMultiOutput && asi.m_stuSecondaryField.Length())
	{
		SortMethodUtil::ParseFieldPath(asi.m_stuSecondaryField, vflid);
		for (iflid = 1; iflid < vflid.Size(); ++iflid)
		{
			qmdc->GetFieldType(vflid[iflid], &proptype);
			if (proptype == kcptOwningCollection ||
				proptype == kcptReferenceCollection ||
				proptype == kcptOwningSequence ||
				proptype == kcptReferenceSequence)
			{
				asi.m_fMultiOutput = true;
				return;
			}
		}
	}
	if (!asi.m_fMultiOutput && asi.m_stuTertiaryField.Length())
	{
		SortMethodUtil::ParseFieldPath(asi.m_stuTertiaryField, vflid);
		for (iflid = 1; iflid < vflid.Size(); ++iflid)
		{
			qmdc->GetFieldType(vflid[iflid], &proptype);
			if (proptype == kcptOwningCollection ||
				proptype == kcptReferenceCollection ||
				proptype == kcptOwningSequence ||
				proptype == kcptReferenceSequence)
			{
				asi.m_fMultiOutput = true;
				return;
			}
		}
	}
}


//:>********************************************************************************************
//:>	AfSortMethodTurnOffDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfSortMethodTurnOffDlg::AfSortMethodTurnOffDlg()
{
	m_rid = kridSortMethodTurnOffDlg;
}


/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.

	@param hwndCtrl Handle passed on to the superclass method.
	@param lp Long parameter passed on the superclass method.

	@return True or false: whatever the superclass's OnInitDlg method returns.
----------------------------------------------------------------------------------------------*/
bool AfSortMethodTurnOffDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the icon to the system information icon.
	HICON hicon = ::LoadIcon(NULL, IDI_EXCLAMATION);
	::SendMessage(::GetDlgItem(m_hwnd, kctidSortInfoIcon), STM_SETICON, (WPARAM)hicon, 0);

	StrApp str;
	str.Load(kstidSortTurnOffInfo);
	if (str.Length())
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortTurnOffInfo), str.Chars());
	str.Clear();
	str.Load(kstidSortTurnOffQuestion);
	if (str.Length())
		::SetWindowText(::GetDlgItem(m_hwnd, kcidSortTurnOffQuestion), str.Chars());

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\MkCustomNb.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#include "Vector_i.cpp"
#include "HashMap_i.cpp"

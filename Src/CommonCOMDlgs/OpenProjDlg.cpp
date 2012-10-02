/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: OpenProjDlg.cpp
Responsibility: Steve McConnel (was John Wimbish)
Last reviewed: Not yet.

Description:
	Implementation of the File Open dialog class.

Note: Requires including winsock2.h and linking with ws2_32.lib.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma hdrstop
#include "main.h"
#undef THIS_FILE
DEFINE_THIS_FILE

static const int kMax = 256;

//:>********************************************************************************************
//:>	IMPLEMENTATION OF EMBEDDED CLASS OpenProjDlg::SubitemInfo
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor, sets values into the items we are interested in in this class.

	@param hvo
	@param pszName
	@param hvoOwner
----------------------------------------------------------------------------------------------*/
OpenProjDlg::SubitemInfo::SubitemInfo(HVO hvo, OLECHAR * pszName, HVO hvoOwner)
{
	m_hvo = hvo;
	m_stuName = pszName;
	m_hvoOwner = hvoOwner;
}

/*----------------------------------------------------------------------------------------------
	Copy Constructor, required for use of this in a vector.

	@param liSource
----------------------------------------------------------------------------------------------*/
OpenProjDlg::SubitemInfo::SubitemInfo(const SubitemInfo & liSource)
{
	m_hvo = liSource.m_hvo;
	m_stuName = liSource.m_stuName;
	m_hvoOwner = liSource.m_hvoOwner;
}

//:>********************************************************************************************
//:>	IMPLEMENTATION OF OpenProjDlg.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
OpenProjDlg::OpenProjDlg()
{
	m_szLocalMachineName[0] = '\0';
	m_hwndProjList = NULL;
	m_hwndListList = NULL;
	m_himlProj = NULL;
	m_himlSubItem = NULL;
}

/*----------------------------------------------------------------------------------------------
	Initialize the various features and values of the dialog.
----------------------------------------------------------------------------------------------*/
void OpenProjDlg::Init(IStream * fist, BSTR bstrCurrentServer, BSTR bstrLocalServer,
	BSTR bstrUserWs, ComBool fAllowMenu, int clidSubitem, BSTR bstrHelpFullUrl, int rid,
	ILgWritingSystemFactory * pwsf)
{
	m_stuUserWs = bstrUserWs;
	m_rid = rid;
	m_qfist = fist;
	m_stuCurrentServer = bstrCurrentServer;
	m_stuLocalServer = bstrLocalServer;
	m_strHelpFullUrl = bstrHelpFullUrl;
	m_pszHelpUrl = m_strHelpFullUrl.Chars();
	m_fAllowMenu = (bool)fAllowMenu;
	m_clidSubitem = clidSubitem;
	m_qwsf = pwsf;

	m_szLocalMachineName[0] = '\0';
	m_hwndProjList = NULL;
	m_hwndListList = NULL;
}


/*----------------------------------------------------------------------------------------------
	Returns the vector index of the item in the list view that is selected, or -1 if no
	list item is selected.

	@param hwndList
----------------------------------------------------------------------------------------------*/
static int GetListSelection(HWND hwndList)
{
	HTREEITEM htri = TreeView_GetSelection(hwndList);
	if (!htri)
		return -1;
	TVITEM item;
	item.hItem = (HTREEITEM)htri;
	item.mask = TVIF_PARAM;
	TreeView_GetItem(hwndList, &item);
	return item.lParam;
}

/*----------------------------------------------------------------------------------------------
	Insert an item into the tree view.

	@param hwndList
	@param iItem
	@param pszText
	int iImage
----------------------------------------------------------------------------------------------*/
static void InsertItemIntoTreeViewCtrl(HWND hwndList, int iItem, const achar * pszText,
	int iImage = -1)
{
	TV_INSERTSTRUCT is;
	is.hParent = TVI_ROOT;
	is.item.lParam = iItem;
	is.hInsertAfter = TVI_LAST;
	is.item.mask = TVIF_TEXT | TVIF_PARAM;
	if (-1 != iImage)
		is.item.mask |= (TVIF_IMAGE | TVIF_SELECTEDIMAGE);
	is.item.pszText = const_cast<achar *>(pszText);
	is.item.cchTextMax = _tcslen(is.item.pszText);
	is.item.iImage = iImage;
	is.item.iSelectedImage = iImage;
	HTREEITEM hti;
	hti = TreeView_InsertItem(hwndList, &is);
	Assert(hti);
}


/*----------------------------------------------------------------------------------------------
	Two-variable variant for when we don't need the version number...
----------------------------------------------------------------------------------------------*/
OLECHAR * OpenProjDlg::GetWsColumnNameAndUserWs(IOleDbEncap * pode, int * pwsUser)
{
	int nVer;
	return GetWsColumnNameAndUserWs(pode, pwsUser, &nVer);
}

static void FixForUpdate(SmartBstr & sbstr, StrUni & stu)
{
	if (sbstr.Length() == 0)
		stu.Assign(L"NULL");
	else
		stu.Format(L"N'%s'", sbstr.Chars());
}


/*----------------------------------------------------------------------------------------------
	We changed the encoding model at version 100003, so in order to see older
	versions, we need to use a different string for the name of what is now the ws column.

	We also changed it at version 150001, so older versions will have a magic number
	representing English as the user interface ws value, and newer versions look up the number
	from the database.
----------------------------------------------------------------------------------------------*/
OLECHAR * OpenProjDlg::GetWsColumnNameAndUserWs(IOleDbEncap * pode, int * pwsUser, int * pnVer)
{
	AssertPtr(pode);
	AssertPtr(pwsUser);
	AssertPtr(pnVer);
	IOleDbCommandPtr qodc;
	ULONG cbSpaceTaken;
	ComBool fMoreRows, fIsNull;
	StrUni stuCommand = L"select DbVer from version$";
	CheckHr(pode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuCommand.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	// Get the version number
	CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(pnVer), sizeof(int),
		&cbSpaceTaken, &fIsNull, 0));
	if (*pnVer < 150001)
	{
		*pwsUser = 740664001;	// "ENG" parsed in our old convoluted algorithm.
	}
	else
	{
		// Believe it or not...
		stuCommand.Format(
			L"SELECT [Id] FROM LgWritingSystem WHERE ICULocale = N'%s'",
			m_stuUserWs.Chars());
		CheckHr(qodc->ExecCommand(stuCommand.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(pwsUser), sizeof(*pwsUser),
				&cbSpaceTaken, &fIsNull, 0));
		}
		else
		{
			// If the current user writing system doesn't exist in the new project, try to
			// create it.  See LT-8659.
			stuCommand.Format(
				L"DECLARE @ws INT; DECLARE @guid UNIQUEIDENTIFIER;%n"
				L"EXEC CreateObject$ %<0>d, @ws OUTPUT, @guid OUTPUT;%n",
				kclidLgWritingSystem);
			IWritingSystemPtr qwsUserOld;
			if (m_qwsf)
			{
				IgnoreHr(m_qwsf->get_Engine(m_stuUserWs.Bstr(), &qwsUserOld));
			}
			if (!qwsUserOld)
			{
				if (m_stuUserWs.Equals(L"en"))
				{
					*pwsUser = 0;		// botched database (!?)
					return *pnVer >= 100003 ? L"ws" : L"enc";
				}
				else
				{
					m_stuUserWs.Assign(L"en");
					return GetWsColumnNameAndUserWs(pode, pwsUser, pnVer);
				}
			}
			int nLocale;
			SmartBstr sbstr;
			StrUni stuMono;
			StrUni stuSansSerif;
			StrUni stuSerif;
			StrUni stuBody;
			//StrUni stuSpellDict;
			StrUni stuBodyFontFeatures;
			StrUni stuFontVariation;
			//StrUni stuKeyboardType;
			StrUni stuKeymanKeyboard;
			StrUni stuLegacyMapping;
			StrUni stuSansFontVariation;
			SmartBstr sbstrValidChars;
			ComBool fRightToLeft;
			CheckHr(qwsUserOld->get_Locale(&nLocale));
			CheckHr(qwsUserOld->get_DefaultMonospace(&sbstr));
			FixForUpdate(sbstr, stuMono);
			CheckHr(qwsUserOld->get_DefaultSansSerif(&sbstr));
			FixForUpdate(sbstr, stuSansSerif);
			CheckHr(qwsUserOld->get_DefaultSerif(&sbstr));
			FixForUpdate(sbstr, stuSerif);
			CheckHr(qwsUserOld->get_DefaultBodyFont(&sbstr));
			FixForUpdate(sbstr, stuBody);
			//CheckHr(qwsUserOld->get_SpellCheckDictionary(&sbstr));
			//FixForUpdate(sbstr, stuSpellDict);
			CheckHr(qwsUserOld->get_BodyFontFeatures(&sbstr));
			FixForUpdate(sbstr, stuBodyFontFeatures);
			CheckHr(qwsUserOld->get_FontVariation(&sbstr));
			FixForUpdate(sbstr, stuFontVariation);
			//CheckHr(qwsUserOld->get_KeyboardType(&sbstr));
			//FixForUpdate(sbstr, stuKeyboardType);
			CheckHr(qwsUserOld->get_KeymanKbdName(&sbstr));
			FixForUpdate(sbstr, stuKeymanKeyboard);
			CheckHr(qwsUserOld->get_LegacyMapping(&sbstr));
			FixForUpdate(sbstr, stuLegacyMapping);
			CheckHr(qwsUserOld->get_SansFontVariation(&sbstr));
			FixForUpdate(sbstr, stuSansFontVariation);
			CheckHr(qwsUserOld->get_ValidChars(&sbstrValidChars));
			CheckHr(qwsUserOld->get_RightToLeft(&fRightToLeft));
			stuCommand.FormatAppend(
				L"UPDATE LgWritingSystem%nSET"
				L" Locale=%<0>d,DefaultMonospace=%<1>s,DefaultSansSerif=%<2>s,"
				L" DefaultSerif=%<3>s,FontVariation=%<4>s,RightToLeft=%<5>d,ICULocale=N'%<6>s',"
				L" KeymanKeyboard=%<7>s,LegacyMapping=%<8>s,SansFontVariation=%<9>s,"
				L" DefaultBodyFont=%<10>s,BodyFontFeatures=%<11>s,ValidChars=%<12>s"
				L" WHERE Id=@ws",
				nLocale, stuMono.Chars(), stuSansSerif.Chars(), stuSerif.Chars(),
				stuFontVariation.Chars(), fRightToLeft ? 1 : 0, m_stuUserWs.Chars(),
				stuKeymanKeyboard.Chars(), stuLegacyMapping.Chars(),
				stuSansFontVariation.Chars(), stuBody.Chars(), stuBodyFontFeatures.Chars(),
				sbstrValidChars.Length() > 0 ? L"?" : L"NULL");
			if (sbstrValidChars.Length() > 0)
			{
				CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
					(ULONG *)sbstrValidChars.Chars(), sbstrValidChars.Length() * 2));
			}
			CheckHr(qodc->ExecCommand(stuCommand.Bstr(), knSqlStmtNoResults));
			qodc.Clear();
			CheckHr(pode->CreateCommand(&qodc));

			// Try again to get the user ws id.
			stuCommand.Format(L"SELECT [Id] FROM LgWritingSystem WHERE ICULocale = N'%s'",
				m_stuUserWs.Chars());
			CheckHr(qodc->ExecCommand(stuCommand.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (fMoreRows)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(pwsUser),
					sizeof(*pwsUser), &cbSpaceTaken, &fIsNull, 0));
			}
			else
			{
				*pwsUser = 0;		// botched database (!?)
				return NULL;
			}
		}
	}
	return *pnVer >= 100003 ? L"ws" : L"enc";
}


/*----------------------------------------------------------------------------------------------
	Fill the list control containing the list of projects stored on the currently selected
	server.

	@param hItem
----------------------------------------------------------------------------------------------*/
void OpenProjDlg::FillListControl(HTREEITEM hItem)
{
	Assert(NULL != hItem);

	IOleDbEncapPtr qode;				// Current connection. Declare before qodc.
	IOleDbCommandPtr qodc;				// Currently-executing command.
	ComBool fMoreRows;					// True while there are more rows in the rowset.
	ComBool fIsNull;					// True if the value sought by GetColValue is NULL.
	ULONG cbSpaceTaken;					// Size of data returned by GetColValue.
	StrApp strTemp;
	StrUni stuTemp;

	// Turn on the hourglass (wait) cursor. This will automatically dissappear
	// when this function is exited (via the objects' destructors).
	WaitCursor wc;
	RECT rect;
	::GetWindowRect(::GetDlgItem(Hwnd(), kctidHelp), &rect);
	POINT pt;
	pt.x = rect.left;
	pt.y = rect.bottom;
	::ScreenToClient(Hwnd(), &pt);
	// Turn on the searching flashlight animation.
	AfAnimateCtrl anim(Hwnd(), kridOpenProjAviSearching, pt.x+3, pt.y+20);

	// Note this vector must be cleared first or we get multiple redraws of sublist when we
	// delete an item from CLE.
	m_vqstuDatabases.Clear();
	// Delete everything out of the list box
	TreeView_DeleteAllItems(m_hwndProjList);
	// Delete everything out of the subitem list if it exists.
	if (m_rid == kridOpenProjSubitemDlg)
	{
		TreeView_DeleteAllItems(m_hwndListList);
		m_vsi.Clear();

		::InvalidateRect(m_hwndListList, NULL, true);
		::UpdateWindow(m_hwndListList);
	}

	// Retrieve the name of the server from the tree control. If we don't have a server
	// selected in the tree control, then there is nothing left to do.
	TVITEM tv;
	tv.hItem  = hItem;
	tv.pszText = m_szServerName;
	tv.cchTextMax = sizeof(m_szServerName) / sizeof(achar);
	tv.mask = TVIF_TEXT | TVIF_IMAGE;
	if (FALSE == TreeView_GetItem(m_qntv->Hwnd(), &tv))
		return;
	if (kridImageComputer != tv.iImage)
		return;

	// Redraw the listbox immediately, so the user doesn't continue looking at the old list of
	// stuff.
	strTemp.Load(kstidOpenProjStatusSearching);
	InsertItemIntoTreeViewCtrl(m_hwndProjList, 0, strTemp.Chars(), kridOpenProjSearch);
	::InvalidateRect(m_hwndProjList, NULL, true);
	::UpdateWindow(m_hwndProjList);

	// If the local machine, use the instance string as the server name. Then, we'll open
	// without having to go across the network. (This is especially required for Windows 9x.)
	if (0 == _tcsicmp(m_szServerName, m_szLocalMachineName))
	{
		StrApp str(m_stuLocalServer);
		_tcscpy_s(m_szServerName, str.Chars());
	}

	// Attempt to open "master" on the target machine. If it doesn't open, then sql is either
	// not installed or it isn't running. (We will want to display an appropriate message in
	// the list control; then we're done; we use m_fConnected to do that.)
#ifdef UNICODE
	m_stuServerName.Assign(m_szServerName);
#else
	wchar wszServerName[kMax];
	mbstowcs(wszServerName, m_szServerName, kMax);
	m_stuServerName = wszServerName;
#endif
	qode.CreateInstance(CLSID_OleDbEncap);
	m_fConnected = false;
	if (!m_stuServerName.EqualsCI(m_stuLocalServer))
		m_stuServerName.Append(L"\\SILFW");
	StrUni stuDatabase(L"master");
	try
	{
		CheckHr(qode->Init(m_stuServerName.Bstr(), stuDatabase.Bstr(), m_qfist, koltMsgBox,
			koltvForever));
	}
	catch (Throwable& thr)
	{
		// qode->Init() failed;
		if (FAILED(thr.Result()))
		{
			TreeView_DeleteAllItems(m_hwndProjList);
			StrApp strFmt(kstidOpenProjServerNotRunningFmt);
			StrApp strMachine(m_stuServerName);
			strTemp.Format(strFmt.Chars(), strMachine.Chars());
			InsertItemIntoTreeViewCtrl(m_hwndProjList, 0, strTemp.Chars(), kridOpenProjSearch);
			return;
		}
	}
	m_fConnected = true;
	StrUni stuCommand;
	CheckHr(qode->CreateCommand(&qodc));
	// Retrieve a list of potential FieldWorks databases, place them in a vector for future
	// use.
	stuCommand = L"exec master..sp_GetFWDBs";
	CheckHr(qodc->ExecCommand(stuCommand.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	wchar wszDatabaseName[kMax];
	while (fMoreRows)
	{
		// Get the database name
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(wszDatabaseName),
			sizeof(wszDatabaseName), &cbSpaceTaken, &fIsNull, 2));

		// Place it into the vector
		StrUni stu(wszDatabaseName);
		m_vqstuDatabases.Push(stu);

		// Get the next row
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	qodc.Clear();
	qode.Clear();


	// Place the project names (full name if appropriate) into the list control.
	TreeView_DeleteAllItems(m_hwndProjList);
	if (m_vqstuDatabases.Size())
	{
		for (int i = 0; i < m_vqstuDatabases.Size(); i++)
		{
			stuTemp.Assign(m_vqstuDatabases[i].Bstr());
			strTemp.Assign(stuTemp);
			InsertItemIntoTreeViewCtrl(m_hwndProjList, i, strTemp.Chars(),
				kridOpenProjFileDrawerClosed);
		}
		// Fill the subitem list if it exists.
		if (m_rid == kridOpenProjSubitemDlg)
		{
			int isel = -1;
			// Set the selection to the current project, if there is one.
			AfMainWnd * pafw = MainWindow();
			if (pafw)
			{
				AfLpInfo * plpi = pafw->GetLpInfo();
				AssertPtr(plpi);
				AfDbInfo * pdbi = plpi->GetDbInfo();
				AssertPtr(pdbi);
				StrUni stuCurrentDb(pdbi->DbName());
				for (int i = 0; i < m_vqstuDatabases.Size(); i++)
				{
					if (m_vqstuDatabases[i] == stuCurrentDb)
					{
						isel = i;
						break;
					}
				}
			}
			if (isel == -1)
				TreeView_SelectItem(m_hwndProjList, TreeView_GetRoot(m_hwndProjList));
			else
			{
				HTREEITEM hitem = TreeView_GetRoot(m_hwndProjList);
				while (hitem)
				{
					TVITEM item;
					item.hItem = (HTREEITEM)hitem;
					item.mask = TVIF_PARAM;
					TreeView_GetItem(m_hwndProjList, &item);
					if (item.lParam == isel)
						break;
					hitem = TreeView_GetNextItem(m_hwndProjList, hitem, TVGN_NEXT);
				};

				if (!hitem)
					hitem = TreeView_GetRoot(m_hwndProjList);

				TreeView_SelectItem(m_hwndProjList, hitem);
			}
			if ((GetListSelection(m_hwndProjList) != -1) && m_vqstuDatabases.Size() &&
					(GetListSelection(m_hwndListList) != -1) && m_vsi.Size())
				::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), 1);
		}
		else
		{
			if ((GetListSelection(m_hwndProjList) != -1) && m_vqstuDatabases.Size())
				::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), 1);
		}
	}
	else
	{
		// If the vector was empty, indicate that we have an empty list.
		strTemp.Load(kstidOpenProjNoItemsInTheList);
		InsertItemIntoTreeViewCtrl(m_hwndProjList, 0, strTemp.Chars(), kridOpenProjSearch);
		StrApp strName;
		if (m_rid == kridOpenProjSubitemDlg)
		{
			FillSubitemList(NULL, NULL);
		}
	}
}

void OpenProjDlg::InsertSubitem(SubitemInfo & siT)
{
	int iv;
	int ivLim;
	StrUni stuNew(siT.GetName());
	for (iv = 0, ivLim = m_vsi.Size(); iv < ivLim; )
	{
		int ivMid = (iv + ivLim) / 2;
		StrUni stu(m_vsi[ivMid].GetName());
		if (wcscmp(stu.Chars(), stuNew.Chars()) < 0)
			iv = ivMid + 1;
		else
			ivLim = ivMid;
	}
	m_vsi.Insert(iv, siT);
}

/*----------------------------------------------------------------------------------------------
	Fill the list control containing the subitems in the currently selected project.

	@param bstrServer
	@param bstrDatabase
	@param hvoProj
----------------------------------------------------------------------------------------------*/
void OpenProjDlg::FillSubitemList(BSTR bstrServer, BSTR bstrDatabase)
{
	TreeView_DeleteAllItems(m_hwndListList);
	m_vsi.Clear();
	// Redraw the listbox immediately, so the user doesn't continue looking at the old list of
	// stuff.
	StrApp strTemp;
	StrUni stuTemp;
	if (bstrServer && bstrDatabase)
	{
		strTemp.Load(kstidOpenProjStatusSearching);
		InsertItemIntoTreeViewCtrl(m_hwndListList, 0, strTemp.Chars(), kridOpenProjSearch);
	}
	::InvalidateRect(m_hwndListList, NULL, true);
	::UpdateWindow(m_hwndListList);
	if (!bstrServer || !bstrDatabase)
		return;
	Assert(m_clidSubitem);

	IOleDbEncapPtr qode;				// Current connection. Declare before qodc.
	IOleDbCommandPtr qodc;				// Currently-executing command.
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(bstrServer, bstrDatabase, m_qfist, koltMsgBox, koltvForever));
	CheckHr(qode->CreateCommand(&qodc));
	int ili;
	Vector<HVO> vhvoOwners;
	StrUni stuCommand;
	ComBool fMoreRows;
	ComBool fIsNull;					// True if the value sought by GetColValue is NULL.
	ULONG cbSpaceTaken;					// Size of data returned by GetColValue.

	int hvoProj;
	// 0. Get project id
	stuCommand = L"SELECT TOP(1) id FROM CmProject";
	CheckHr(qodc->ExecCommand(stuCommand.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoProj), sizeof(hvoProj),
		&cbSpaceTaken, &fIsNull, 0));
	if (fIsNull)
		return;

	// 1. Build vector of additional list owner ids.
	stuCommand.Format(L"SELECT Owner$ FROM CmObject WHERE "
		L"Class$ = %d AND Owner$ <> %d",
		m_clidSubitem, hvoProj);
	CheckHr(qodc->ExecCommand(stuCommand.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	Vector<HVO> vhvoT;
	HVO hvo;
	CheckHr(qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), sizeof(hvo),
			&cbSpaceTaken, &fIsNull, 0));
		if (!fIsNull)
			vhvoT.Push(hvo);
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	// 2. Select those list owners which are themselves eventually owned by the project.
	int ihvo;
	ULONG chvo;
	for (ihvo = 0; ihvo < vhvoT.Size(); ++ihvo)
	{
		stuCommand.Format(L"select COUNT(ObjId)"
			L" FROM fnGetOwnershipPath$(%d, null, -1,1,0) where (ObjId = %d)",
			vhvoT[ihvo], hvoProj);
		CheckHr(qodc->ExecCommand(stuCommand.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&chvo), isizeof(chvo), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull && chvo == 1)
				vhvoOwners.Push(vhvoT[ihvo]);
		}
	}
	// 3. Get the object id and name for all relevant lists in the database.
	int wsUser;
	StrUni stuWsColName = GetWsColumnNameAndUserWs(qode, &wsUser);
	Assert(wsUser);
	/* stuCommand.Format(L"SELECT co.Id, mt.Txt, co.Owner$%n"
		L"FROM CmObject co%n"
		L"JOIN CmMajorObject_Name mt on mt.Obj = co.Id AND mt.%s = %d%n"
		L"WHERE co.Class$ = %d AND (co.Owner$ in (%d",
		stuWsColName.Chars(), wsUser, m_clidSubitem, hvoProj);
	for (ihvo = 0; ihvo < vhvoOwners.Size(); ++ihvo)
		stuCommand.FormatAppend(L",%d", vhvoOwners[ihvo]);
	stuCommand.Append(L") OR co.Owner$ is NULL)");		// Include Custom Lists (unowned) */

	// For now, limit lists to ones we can display in the list editor.
	// Note that this retrieves all WSs of the major object name and may therefore retrieve the same object twice
	stuCommand.Format(L"select cpl.id, mt.txt, cpl.owner$, mt.%s%n"
		L"from CmPossibilityList_ cpl%n"
		L"join CmMajorObject_Name mt on mt.obj = cpl.id%n"
		L"where cpl.ItemClsid <= %d%n"
		L"order by cpl.id",
		stuWsColName.Chars(), kclidCmCustomItem);

	CheckHr(qodc->ExecCommand(stuCommand.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	Vector<OLECHAR> vchName;
	vchName.Resize(kMax);
	ULONG cbName = vchName.Size() * isizeof(OLECHAR);
	HVO hvoOwner;
	CheckHr(qodc->NextRow(&fMoreRows));
	int hvoPrev = 0; // hvo of previous iteration
	int hvoPending = 0; // hvo of item saved in siPrev IF it still needs to be inserted.
	SubitemInfo siPending(0, L"", 0);
	while (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), isizeof(hvo),
			&cbSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(vchName.Begin()),
			cbName, &cbSpaceTaken, &fIsNull, 2));
		if (cbName <= cbSpaceTaken)
		{
			vchName.Resize(cbSpaceTaken / isizeof(OLECHAR) + 1);
			cbName = vchName.Size() * isizeof(OLECHAR);
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(vchName.Begin()),
				cbName, &cbSpaceTaken, &fIsNull, 2));
		}
		CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&hvoOwner), isizeof(hvoOwner),
			&cbSpaceTaken, &fIsNull, 0));
		if (fIsNull)
			hvoOwner = 0;
		int ws;
		CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&ws), isizeof(ws),
			&cbSpaceTaken, &fIsNull, 0));
		if (fIsNull)
			ws = 0;
		SubitemInfo siT(hvo, vchName.Begin(), hvoOwner);
		// If we have a pending item to insert and have now moved to a different item,
		// insert the pending one.
		if (hvo != hvoPrev && hvoPending != 0)
		{
			InsertSubitem(siPending);
			hvoPending = 0;
		}
		// If the current item has the preferred WS, just insert it.
		if (ws == wsUser)
		{
			InsertSubitem(siT);
			hvoPending = 0; // forget any pending item.
		}
		// If this is the first ws for the current item and NOT the preferred one, make it pending.
		if (hvo != hvoPrev && ws != wsUser)
		{
			hvoPending = hvo;
			siPending = siT;
		}
		hvoPrev = hvo;
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	// If we have a left-over pending item insert it.
	if (hvoPending != 0)
		InsertSubitem(siPending);
	TreeView_DeleteAllItems(m_hwndListList);
	if (m_vsi.Size())
	{
		for (ili = 0; ili < m_vsi.Size(); ++ili)
		{
			stuTemp.Assign(m_vsi[ili].GetName().Bstr());
			strTemp.Assign(stuTemp);
			InsertItemIntoTreeViewCtrl(m_hwndListList, ili, strTemp.Chars(),
				kridOpenProjSubitem);
		}
	}
	else
	{
		strTemp.Load(kstidOpenProjNoItemsInTheList);
		InsertItemIntoTreeViewCtrl(m_hwndListList, 0, strTemp.Chars(),
			kridOpenProjSearch);
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool OpenProjDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Get handle to the controls
	m_hwndProjList = ::GetDlgItem(Hwnd(), kctidOpenProjChooseProject);
	Assert(m_hwndProjList);
	m_hwndListList = NULL;
	if (m_rid == kridOpenProjSubitemDlg)
	{
		m_hwndListList = ::GetDlgItem(Hwnd(), kctidOpenProjChooseSubitem);
		Assert(m_hwndListList);
	}

	// Subclass the Windows tree view to use our special Network treeview.
	HWND hwndNetworkTree = ::GetDlgItem(Hwnd(), kctidOpenProjChooseComputer);
	Assert(NULL != hwndNetworkTree);
	m_qntv.Create();
	m_qntv->SubclassTreeView(hwndNetworkTree);

	// Get the local machine name.
	NetworkTreeView::GetLocalMachineName(m_szLocalMachineName, isizeof(m_szLocalMachineName));

	// Initialize image list into the list control. Note that the image list has to be there
	// when the tree view is actually displayed, so we can't destroy it again straight away,
	// after setting it into the tree view. Hence need for a member variable. Destroyed by
	// destructor.
	Assert(m_himlProj == NULL);
	m_himlProj = AfGdi::ImageList_Create(17, 17, ILC_COLORDDB | ILC_MASK, 5, 5);
	HBITMAP hbmpImages = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(),
		MAKEINTRESOURCE(kridOpenProjImagesSmall));
	int nRet;
	nRet = ImageList_AddMasked(m_himlProj, hbmpImages, RGB(255,255,255));
	HIMAGELIST himlProjOld = TreeView_SetImageList(m_hwndProjList, m_himlProj, TVSIL_NORMAL);
	if (himlProjOld)
		AfGdi::ImageList_Destroy(himlProjOld);
	AfGdi::DeleteObjectBitmap(hbmpImages);
	if (m_rid == kridOpenProjSubitemDlg)
	{
		Assert(m_himlSubItem == NULL);
		m_himlSubItem = AfGdi::ImageList_Create(17, 17, ILC_COLORDDB | ILC_MASK, 5, 5);
		hbmpImages = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(),
			MAKEINTRESOURCE(kridOpenProjImagesSmall));
		nRet = ImageList_AddMasked(m_himlSubItem, hbmpImages, RGB(255,255,255));
		himlProjOld = TreeView_SetImageList(m_hwndListList, m_himlSubItem, TVSIL_NORMAL);
		if (himlProjOld)
			AfGdi::ImageList_Destroy(himlProjOld);
		AfGdi::DeleteObjectBitmap(hbmpImages);
		StrApp strCaption(kstidOpenProjSubitemCaption);
		if (strCaption.Length())
			::SetWindowText(m_hwnd, strCaption.Chars());
		StrApp strChoose(kstidOpenProjChooseSubitemText);
		if (strChoose.Length())
			::SetWindowText(::GetDlgItem(m_hwnd, kcidOpenProjChooseSubitemText),
				strChoose.Chars());
	}

	StrUni stuLocalMachine(m_szLocalMachineName);
	StrUni stuCurrentMachine;

	if (m_stuCurrentServer.Length())
		stuCurrentMachine = m_stuCurrentServer;
	else
	{
		AfMainWnd * pafw = MainWindow();
		if (pafw)
		{
			AfLpInfo * plpi = pafw->GetLpInfo();
			AssertPtr(plpi);
			AfDbInfo * pdbi = plpi->GetDbInfo();
			AssertPtr(pdbi);
			stuCurrentMachine.Assign(pdbi->ServerName());
		}
		else
		{
			stuCurrentMachine = stuLocalMachine;
		}
	}

	Assert(stuCurrentMachine.Length());

	// Hack off the... whatever that undesirable stuff is at the end of the server name.
	int ich = stuCurrentMachine.FindStrCI(L"\\SILFW");
	if (ich >= 0)
		stuCurrentMachine.Replace(ich, stuCurrentMachine.Length(), L"");

	HTREEITEM hitem = TreeView_GetFirstVisible(hwndNetworkTree);	// Local Computer.
	if (!stuLocalMachine.EqualsCI(stuCurrentMachine))
	{
		achar rgchText[kMax + 1];
		rgchText[kMax] = 0;
		TVITEM item;
		item.mask = TVIF_TEXT;
		item.pszText = rgchText;
		item.cchTextMax = kMax;
		// Get the network neighborhood, and expand it.
		HTREEITEM htiRoot;
		HTREEITEM htiEntire;
		htiRoot = TreeView_GetNextSibling(hwndNetworkTree, hitem);		// Network Neighborhood.
		TreeView_Expand(hwndNetworkTree, htiRoot, TVE_EXPAND);
		htiEntire = TreeView_GetChild(hwndNetworkTree, htiRoot);		// Entire Network.
		HTREEITEM hti = htiEntire;
		bool fFound = false;
		// First, search the network neighborhood.
		for (;;)
		{
			item.hItem = TreeView_GetNextSibling(hwndNetworkTree, hti);
			if (!item.hItem)
				break;			// Out of nodes in the tree.
			TreeView_GetItem(hwndNetworkTree, &item);
			StrUni stuMachine(rgchText);
			if (stuMachine.EqualsCI(stuCurrentMachine))
			{
				fFound = true;
				hitem = item.hItem;
				break;
			}
			hti = item.hItem;
		}
		// If not found, search recursively through the entire network: this could be SLOW!
		if (!fFound && htiEntire)
		{
			hti = htiEntire;
			for (;;)
			{
				TreeView_Expand(hwndNetworkTree, hti, TVE_EXPAND);
				item.hItem = TreeView_GetChild(hwndNetworkTree, hti);
				if (!item.hItem)
					item.hItem = TreeView_GetNextSibling(hwndNetworkTree, hti);
				if (!item.hItem)
				{
					HTREEITEM hti2 = TreeView_GetParent(hwndNetworkTree, hti);
					if (hti2)
					{
						TreeView_Expand(hwndNetworkTree, hti2, TVE_COLLAPSE);
						if (hti2 == htiEntire)
							break;
						item.hItem = TreeView_GetNextSibling(hwndNetworkTree, hti2);
					}
				}
				if (!item.hItem)
					break;			// Out of nodes in the tree.
				TreeView_GetItem(hwndNetworkTree, &item);
				StrUni stuMachine(rgchText);
				if (stuMachine.EqualsCI(stuCurrentMachine))
				{
					fFound = true;
					hitem = item.hItem;
					break;
				}
				hti = item.hItem;
			}
			if (!fFound)
				TreeView_Expand(hwndNetworkTree, htiRoot, TVE_COLLAPSE);
		}
	}
	TreeView_SelectItem(hwndNetworkTree, hitem);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Process notifications from user.

	@param ctidFrom
	@param pnmh
	@param lnRet
----------------------------------------------------------------------------------------------*/
bool OpenProjDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	switch (pnmh->code)
	{
	// The user has selected an item in the tree. Place the appropriate corresponding contents
	// into the list control.
	case TVN_SELCHANGED:
		switch (ctidFrom)
		{
		case kctidOpenProjChooseComputer:
			{
				::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), 0);
				NMTREEVIEW * pnmtv = reinterpret_cast<NMTREEVIEW *>(pnmh);
				FillListControl(pnmtv->itemNew.hItem);
				return true;
			}
		case kctidOpenProjChooseProject:
			{
				::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), 0);
				// Turn on the hourglass (wait) cursor. This will automatically dissappear
				// when this function is exited (via the objects' destructors).
				WaitCursor wc;
				RECT rect;
				::GetWindowRect(::GetDlgItem(Hwnd(), kctidHelp), &rect);
				POINT pt;
				pt.x = rect.left;
				pt.y = rect.bottom;
				::ScreenToClient(Hwnd(), &pt);
				// Turn on the searching flashlight animation.
				AfAnimateCtrl anim(Hwnd(), kridOpenProjAviSearching, pt.x+3, pt.y+20);

				NMTREEVIEW * pnmtv = reinterpret_cast<NMTREEVIEW *>(pnmh);
				if ((m_rid == kridOpenProjSubitemDlg) && m_vqstuDatabases.Size())
				{
					FillSubitemList(m_stuServerName.Bstr(),
						m_vqstuDatabases[(int)pnmtv->itemNew.lParam].Bstr());
					TreeView_SelectItem(m_hwndListList, TreeView_GetRoot(m_hwndListList));

					if ((GetListSelection(m_hwndProjList) != -1) && m_vqstuDatabases.Size() &&
							(GetListSelection(m_hwndListList) != -1) && m_vsi.Size())
						::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), 1);
				}
				if (m_rid != kridOpenProjSubitemDlg)
				{
					if ((GetListSelection(m_hwndProjList) != -1) && m_vqstuDatabases.Size())
						::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), 1);
				}
				return true;
			}
		case kctidOpenProjChooseSubitem:
			{
				NMTREEVIEW * pnmtv = reinterpret_cast<NMTREEVIEW *>(pnmh);
				if ((int)pnmtv->itemNew.lParam >= m_vsi.Size())
				{
					Assert(!pnmtv->itemNew.lParam && !m_vsi.Size());
				}
				return true;
			}
		}
		break;

	case NM_CLICK:
		switch (ctidFrom)
		{
		case kcidOpenProjDelete:
			{
				OnRemoveProject();
				return true;
			}
		}
		break;

	case NM_RCLICK:
		if (!m_fAllowMenu)
			return true;
		switch (ctidFrom)
		{
		case kctidOpenProjChooseProject:
			{
				Point pt;
				::GetCursorPos(&pt);

				TVHITTESTINFO tvhti;
				tvhti.pt = pt;
				::ScreenToClient(m_hwndProjList, &tvhti.pt);

				if (TreeView_HitTest(m_hwndProjList, &tvhti))
				{
					// If right-clicked on an item, select the item then show the popup menu.
					if (tvhti.hItem)
					{
						// Select the item that was right clicked on
						TreeView_SelectItem(m_hwndProjList, tvhti.hItem);
						m_iProj = GetListSelection(m_hwndProjList);
						HMENU hmenu = ::LoadMenu(ModuleEntry::GetModuleHandle(),
							MAKEINTRESOURCE(kridOpenProjPopup));
						SetRemoveProjectState(hmenu);
						HMENU hmenuPopup = ::GetSubMenu(hmenu, 0);
						int menuitem;
						menuitem = ::TrackPopupMenu(hmenuPopup, TPM_NONOTIFY | TPM_RETURNCMD |
							TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y, 0,	m_hwnd, NULL);
						::DestroyMenu(hmenu);
						if (menuitem)
							OnRemoveProject();
						// Make sure Windows doesn't pass this on. Without this, if you left
						// click on the title bar after activating the right+click menu, you
						// get the right+click system popup menu.
						lnRet = 1;
					}
				}
				break;
			}
		case kctidOpenProjChooseSubitem:
			{
				Point pt;
				TVHITTESTINFO tvhti;
				::GetCursorPos(&pt);
				::GetCursorPos(&tvhti.pt);
				::ScreenToClient(m_hwndListList, &tvhti.pt);
				if (TreeView_HitTest(m_hwndListList, &tvhti))
				{
					// If right-clicked on an item, select the item then show the popup menu.
					if (tvhti.hItem)
					{
						// Select the item that was right clicked on
						m_iProj = GetListSelection(m_hwndProjList);
						TreeView_SelectItem(m_hwndListList, tvhti.hItem);
						m_iSubitem = GetListSelection(m_hwndListList);
						if (!m_vsi[m_iSubitem].GetOwner())
						{
							// Only show menu if it does not have a owner, which means it is not
							// a factory list.
							HMENU hmenu = ::LoadMenu(ModuleEntry::GetModuleHandle(),
								MAKEINTRESOURCE(kridOpenProjSubitemPopup));
							if (hmenu)
							{
								HMENU hmenuPopup = ::GetSubMenu(hmenu, 0);
								Point pt;
								::GetCursorPos(&pt);
								int menuitem;
								menuitem = ::TrackPopupMenu(hmenuPopup, TPM_NONOTIFY | TPM_RETURNCMD |
									TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y, 0, m_hwnd, NULL);
								::DestroyMenu(hmenu);
								if (menuitem)
									OnRemoveList();
								// Make sure Windows doesn't pass this on. Without this, if you left
								// click on the title bar after activating the right+click menu, you
								// get the right+click system popup menu.
								lnRet = 1;
							}
						}
					}
				}
				break;
			}
		}
		return true;

	case NM_DBLCLK:
		if (ctidFrom == kctidOpenProjChooseProject)
		{
			if (GetListSelection(m_hwndProjList) != -1 &&  m_vqstuDatabases.Size())
			{
				if (m_rid == kridOpenProjDlg)
				{
					// same as clicking on [Open]
					::PostMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(kctidOk, BN_CLICKED),
						(LPARAM)::GetDlgItem(m_hwnd, kctidOk));
					return true;
				}
				else if (GetListSelection(m_hwndListList) != -1 &&  m_vsi.Size())
				{
					// same as clicking on [Open]
					::PostMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(kctidOk, BN_CLICKED),
						(LPARAM)::GetDlgItem(m_hwnd, kctidOk));
					return true;
				}
			}
		}
		else if (ctidFrom == kctidOpenProjChooseSubitem)
		{
			if ((GetListSelection(m_hwndProjList) != -1) &&  m_vqstuDatabases.Size() &&
				(GetListSelection(m_hwndListList) != -1) &&  m_vsi.Size())
			{
				// same as clicking on [Open] ?
				::PostMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(kctidOk, BN_CLICKED),
					(LPARAM)::GetDlgItem(m_hwnd, kctidOk));
			}
			return true;
		}
		break;

	// Default is do nothing.
	default:
		break;
	}

	return AfWnd::OnNotifyChild(ctidFrom, pnmh, lnRet);
}

/*----------------------------------------------------------------------------------------------

	@param hvoProj
	@param stuProject
	@param stuDatabase
	@param stuMachine
	@param guid
----------------------------------------------------------------------------------------------*/
bool OpenProjDlg::GetSelectedProject(int & hvoProj, StrUni & stuProject,
		StrUni & stuDatabase, StrUni & stuMachine, GUID * guid)
{
	if (false == m_fProjectIsSelected)
		return false;

	StrUni stuCommand;
	IOleDbEncapPtr qode;				// Current connection. Declare before qodc.
	IOleDbCommandPtr qodc;				// Currently-executing command.
	ComBool fMoreRows;					// True while there are more rows in the rowset.
	ComBool fIsNull;					// True if the value sought by GetColValue is NULL.
	ULONG cbSpaceTaken;					// Size of data returned by GetColValue.
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(m_stuServerName.Bstr(), m_stuDatabase.Bstr(), m_qfist, koltMsgBox,
		koltvForever));
	CheckHr(qode->CreateCommand(&qodc));
	// Get the needed information on the selected project.
	try
	{
		// Open the database
		try
		{
			CheckHr(qode->Init(m_stuServerName.Bstr(), m_stuDatabase.Bstr(), m_qfist,
				koltMsgBox, koltvForever));
		}
		catch (Throwable& thr)
		{
			// qode->Init() failed;
			if (FAILED(thr.Result()))
				return false;
		}

		int wsUser;
		int nVer;
		StrUni stuWsColName = GetWsColumnNameAndUserWs(qode, &wsUser, &nVer);
		if (!wsUser)
		{
			// We have a seriously unusable database: ignore it.
			return false;
		}

		// Execute a command to retrieve the language project within the database
		stuCommand.Format(L"SELECT TOP(1) lp.id, mt.txt, cm.guid$"
			L" FROM %s lp"
			L" LEFT OUTER JOIN CmProject_Name mt on mt.obj=lp.id"
			L"    and mt.%s = %d"
			L" join CmObject cm on cm.id=lp.id ",
			(nVer <= 200202 ? L"LanguageProject" : L"LangProject"), stuWsColName.Chars(), wsUser);

		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuCommand.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));

		// Compile a list of all of the language projects
		wchar wszProjectName[kMax];
		CheckHr(qodc->NextRow(&fMoreRows));
		// Get the project id
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&m_hvoProj), sizeof(ulong), &cbSpaceTaken,
			&fIsNull, 0));

		// Get the project name
		CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(wszProjectName),
			sizeof(wszProjectName), &cbSpaceTaken, &fIsNull, 2));
		m_stuProject.Assign(wszProjectName);

		// Get the guid
		CheckHr(qodc->GetColValue(3, reinterpret_cast <BYTE *>(&m_guid),
			sizeof(guid), &cbSpaceTaken, &fIsNull, 0));
	}
	catch (...)
	{
		return false;
	}
	qodc.Clear();
	qode.Clear();


	hvoProj	= m_hvoProj;
	stuDatabase = m_stuDatabase;
	stuProject	= m_stuProject;
	stuMachine	= m_stuMachine;
	*guid		= m_guid;
	return true;
}

/*----------------------------------------------------------------------------------------------

	@param hvo
	@param stuName
----------------------------------------------------------------------------------------------*/
bool OpenProjDlg::GetSelectedSubitem(HVO & hvo, StrUni & stuName)
{
	if (m_hvoSubitem)
	{
		hvo = m_hvoSubitem;
		stuName = m_stuSubitemName;
		return true;
	}
	else
	{
		return false;
	}
}


/*----------------------------------------------------------------------------------------------

	@param padx
----------------------------------------------------------------------------------------------*/
void OpenProjDlg::DoDataExchange(AfDataExchange * padx)
{
	AssertPtr(padx);
	if (!padx->m_fSave)
		return;

	// Default to nothing selected.
	m_hvoProj = 0;
	m_stuDatabase = L"";
	m_stuProject = L"";
	m_stuMachine = m_stuLocalServer;
	m_fProjectIsSelected = false;
	m_hvoSubitem = 0;
	m_stuSubitemName = L"";

	// Is there anything selected in the list box(es)? Return if not.
	int iSelectedItem = GetListSelection(m_hwndProjList);
	if (iSelectedItem == -1)
		return;
	int iSelectedSubitem = 0;
	if (m_rid == kridOpenProjSubitemDlg)
		iSelectedSubitem = GetListSelection(m_hwndListList);
	if (iSelectedSubitem == -1)
		return;

	// Get the project list's information
	TVITEM item;
	achar szName[kMax + 1];
	item.hItem = TreeView_GetSelection(m_hwndProjList);
	item.mask = TVIF_TEXT | TVIF_IMAGE;
	item.pszText = szName;
	item.cchTextMax = kMax;
	TreeView_GetItem(m_hwndProjList, &item);

	// If the image is of our searching icon, then we don't have a project.
	if (kridOpenProjSearch == item.iImage)
		return;
	// Get the subitem list's information.
	achar szSubitemName[kMax+1];
	if (m_rid == kridOpenProjSubitemDlg)
	{
		int isel = GetListSelection(m_hwndListList);
		if (isel == -1)
			return;

		TVITEM subitem;
		subitem.hItem = TreeView_GetSelection(m_hwndListList);
		subitem.mask = TVIF_TEXT | TVIF_IMAGE;
		subitem.pszText = szSubitemName;
		subitem.cchTextMax = kMax;
		TreeView_GetItem(m_hwndListList, &subitem);

		if (kridOpenProjSearch == subitem.iImage)
			return;
	}

	// Go through our vector, looking for a match for the name
	wchar wszName[kMax+1];
#ifdef UNICODE
	wcsncpy_s(wszName, szName, kMax);
#else
	mbstowcs(wszName, szName, kMax);
#endif
	wszName[kMax] = 0;
	int i;
	for (i = 0; i < m_vqstuDatabases.Size(); i++)
	{
		if (0 == wcscmp(m_vqstuDatabases[i].Bstr(), wszName))
			break;
	}
	Assert(i < m_vqstuDatabases.Size());
	// We have a match. It will coincide with something in our database name vector.
	if (i < m_vqstuDatabases.Size())
	{
		m_stuDatabase = m_vqstuDatabases[i];
		m_stuMachine = m_szServerName;
		m_fProjectIsSelected = true;
	}

	if (m_rid == kridOpenProjSubitemDlg)
	{
		i = GetListSelection(m_hwndListList);
		if (i > -1)
		{
			m_hvoSubitem = m_vsi[i].GetHvo();
			m_stuSubitemName = m_vsi[i].GetName();
		}
	}
}

/*----------------------------------------------------------------------------------------------
	User command to remove a language project.

	@param pcmd Menu command (This parameter is not used in in this method.)

	@return True.
----------------------------------------------------------------------------------------------*/
bool OpenProjDlg::OnRemoveProject()
{
	StrApp strProject(m_vqstuDatabases[m_iProj]);
	ConfirmRemoveProjectDlgPtr qcrp;
	qcrp.Create();
	qcrp->SetProjectName(strProject.Chars());
	StrApp strFile;
	int ich = m_strHelpFullUrl.FindStr(_T("::/"));
	if (ich > 0)
		strFile.Assign(m_strHelpFullUrl.Chars(), ich);
	qcrp->SetHelpFilename(strFile.Chars());
	if (qcrp->DoModal(m_hwnd) == kctidOk)
	{
		WaitCursor wc;
		IOleDbEncapPtr qode; // Declare before qodc.
		IOleDbCommandPtr qodc;
		StrUni stuServer = m_stuLocalServer;
		StrUni stuDatabase(L"master");
		qode.CreateInstance(CLSID_OleDbEncap);
		CheckHr(qode->Init(stuServer.Bstr(), stuDatabase.Bstr(), m_qfist, koltMsgBox,
			koltvForever));
		CheckHr(qode->CreateCommand(&qodc));
		StrUni stu;
		// Check first to see if there are any users.
		stu.Format(L"select rtrim([sproc].[hostname]), "
			L"   rtrim([sproc].[nt_domain]) + '\\' + rtrim([sproc].[nt_username]) "
			L"from sysprocesses [sproc] "
			L"join sysdatabases [sdb] "
			L"   on [sdb].[dbid] = [sproc].[dbid] and [name] = '%s' "
			L"where sproc.spid != @@spid",
			m_vqstuDatabases[m_iProj].Chars());
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		ComBool fMoreRows;
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			StrApp strbTitle(kstidOpenProjRemoveMsgTitle);
			StrApp strb(kstidOpenProjCannotRemove);
			::MessageBox(m_hwnd, strb.Chars(), strbTitle.Chars(), MB_OK);
		}
		else
		{
			stu.Format(L"DROP DATABASE [%s]", m_vqstuDatabases[m_iProj].Chars());
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtNoResults));
			HTREEITEM hItem = TreeView_GetSelection(
				::GetDlgItem(m_hwnd, kctidOpenProjChooseComputer));
			FillListControl(hItem);
		}
		wc.RestoreCursor();
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Enable/disable the "Remove this Language Project..." menu item.

	@param hmenu Handle to main popup menu.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool OpenProjDlg::SetRemoveProjectState(HMENU hmenu)
{
	StrApp str = m_stuLocalServer;
	// Disable, if not on local computer.
	UINT ui = (_tcsicmp(m_szServerName, str.Chars()) != 0) ? MF_GRAYED : MF_ENABLED;
	EnableMenuItem(hmenu, kcidOpenProjDelete, MF_BYCOMMAND | ui);

	return true;
}


/*----------------------------------------------------------------------------------------------
	The default OnHelp shows the help page for the dialog if there is one.
----------------------------------------------------------------------------------------------*/
bool OpenProjDlg::OnHelp()
{
#ifdef DEBUG
	if (!m_pszHelpUrl)
		::MessageBoxA(NULL, "Missing a help page for this dialog.", NULL,
			MB_OK | MB_ICONWARNING);
#endif
	if (m_pszHelpUrl)
	{
		HtmlHelp(::GetDesktopWindow(), m_pszHelpUrl, HH_DISPLAY_TOPIC, NULL);
		return true;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Show help information for the requested control.
----------------------------------------------------------------------------------------------*/
bool OpenProjDlg::OnHelpInfo(HELPINFO * phi)
{
	AssertPtr(phi);

	// Ensure that we can get to a writing system factory for creating the AfContextHelpWnd.
	if (m_qwsf || MainWindow())
	{
		// Get the coordinates of the control and center the tooltip underneath the control.
		Rect rc;
		::GetWindowRect((HWND)phi->hItemHandle, &rc);
		phi->MousePos.x = rc.left + (rc.Width() / 2);
		phi->MousePos.y = rc.bottom + 1;

		AfContextHelpWndPtr qchw;
		qchw.Attach(NewObj AfContextHelpWnd);
		if (m_qwsf)
			qchw->SetLgWritingSystemFactory(m_qwsf);
		qchw->Create(m_hwnd, phi);
	}
	return true;
}

//:>********************************************************************************************
//:>	IMPLEMENTATION OF ConfirmRemoveProjectDlg.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ConfirmRemoveProjectDlg::ConfirmRemoveProjectDlg()
{
	m_rid = kridOpenProjDeleteDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/File/Remove_Project.htm");
}


/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool ConfirmRemoveProjectDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	StrApp strFmt;
	StrApp strText;
	HWND hwnd;

	HICON hicon = ::LoadIcon(NULL, IDI_WARNING);
	if (hicon)
	{
		hwnd = ::GetDlgItem(m_hwnd, kridOpenProjDeleteIcon);
		::SendMessage(hwnd, STM_SETICON, (WPARAM)hicon, (LPARAM)0);
	}

	hwnd = ::GetDlgItem(m_hwnd, kcidOpenProjDeleteMsg);
	strFmt.Load(kstidOpenProjDeleteMsgFmt);
	strText.Format(strFmt.Chars(), m_strProject.Chars());
	::SetWindowText(hwnd, strText.Chars());

	hwnd = ::GetDlgItem(m_hwnd, kcidOpenProjConfirmMsg);
	strFmt.Load(kstidOpenProjConfirmMsgFmt);
	strText.Format(strFmt.Chars(), m_strProject.Chars());
	::SetWindowText(hwnd, strText.Chars());

	::SetFocus(::GetDlgItem(m_hwnd, kctidCancel));

	SuperClass::OnInitDlg(hwndCtrl, lp);
	return false;
}

/*----------------------------------------------------------------------------------------------
	The default OnHelp shows the help page for the dialog if there is one.
----------------------------------------------------------------------------------------------*/
bool ConfirmRemoveProjectDlg::OnHelp()
{
	if (m_pszHelpUrl && m_strHelpFilename.Length())
	{
		AfUtil::ShowHelpFile(m_strHelpFilename.Chars(), m_pszHelpUrl);
		return true;
	}
	return false;
}


//:>********************************************************************************************
//:>	IMPLEMENTATION OF ConfirmRemoveProjectSubitemDlg.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ConfirmRemoveProjectSubitemDlg::ConfirmRemoveProjectSubitemDlg()
{
	m_rid = kridOpenProjDeleteDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/File/Open_a_FieldWorks_project.htm");
}


/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool ConfirmRemoveProjectSubitemDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	StrApp strFmt;
	StrApp strText;
	StrApp strType(kstidOpenProjSubitemName);
	if (!strType.Length())
		strType.Load(kstidOpenProjDefaultSubitemName);
	StrApp strTypeSm(kstidOpenProjSubitemNameSm);
	if (!strTypeSm.Length())
		strType.Load(kstidOpenProjDefaultSubitemName);
	HWND hwnd;

	HICON hicon = ::LoadIcon(NULL, IDI_WARNING);
	if (hicon)
	{
		hwnd = ::GetDlgItem(m_hwnd, kridOpenProjDeleteIcon);
		::SendMessage(hwnd, STM_SETICON, (WPARAM)hicon, (LPARAM)0);
	}

	strFmt.Load(kstidOpenProjSubitemRemoveMsgTitle);
	strText.Format(strFmt.Chars(), strType.Chars());
	::SetWindowText(m_hwnd, strText.Chars());

	strFmt.Load(kstidOpenProjSubitemDeleteMsgFmt);
	strText.Format(strFmt.Chars(),
		m_strSubitem.Chars(), strTypeSm.Chars(), m_strProject.Chars());
	::SetWindowText(::GetDlgItem(m_hwnd, kcidOpenProjDeleteMsg), strText.Chars());

	strFmt.Load(kstidOpenProjSubitemConfirmMsgFmt);
	strText.Format(strFmt.Chars(), m_strSubitem.Chars(), strTypeSm.Chars());
	::SetWindowText(::GetDlgItem(m_hwnd, kcidOpenProjConfirmMsg), strText.Chars());

	::SetWindowText(::GetDlgItem(m_hwnd, kctidOk), _T("Delete"));

	::SetFocus(::GetDlgItem(m_hwnd, kctidCancel));

	SuperClass::OnInitDlg(hwndCtrl, lp);
	return false;
}

//:>********************************************************************************************
//:>	IMPLEMENTATION OF CleOpenProjDlg.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	User command to remove a topics list from a language project.

	@param pcmd Menu command (This parameter is not used in in this method.)

	@return True.
----------------------------------------------------------------------------------------------*/
bool CleOpenProjDlg::OnRemoveList()
{
	HTREEITEM htri = TreeView_GetSelection(m_hwndListList);
	Assert(htri);
	if (!htri)
		return false;
	TVITEM tvi;
	tvi.hItem = htri;
	tvi.mask = TVIF_PARAM;
	TreeView_GetItem(m_hwndListList, &tvi);
	Assert(tvi.lParam != LB_ERR); // something should always be selected.
	Assert(tvi.lParam == m_iSubitem);

	HVO hvoCur = m_vsi[m_iSubitem].GetHvo();
	Assert(!m_vsi[m_iSubitem].GetOwner());

	// The rest of this method is cribbed from TlsListsDlg::OnDelList(), subtracting the
	// parts that deal with the TlsListDlg itself.  Any bugs here may need to be fixed there
	// as well.
	AfLpInfo * plpi = MainWindow()->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	UserViewSpecVec vuvs = pdbi->GetUserViewSpecs();
	Vector<TlsObject> vto; // List of objects.

	// Find a DataEntry View and see if the list is referenced by any fields
	for (int iuv = 0; iuv < vuvs.Size(); ++iuv)
	{
		if (vuvs[iuv]->m_vwt != kvwtDE)
			continue;

		// Process each data entry RecordSpec.
		RecordSpecPtr qrsp;
		ClevRspMap::iterator ithmclevrspLim = vuvs[iuv]->m_hmclevrsp.End();
		for (ClevRspMap::iterator it = vuvs[iuv]->m_hmclevrsp.Begin(); it != ithmclevrspLim;
			++it)
		{
			ClsLevel clev = it.GetKey();
			vuvs[iuv]->m_hmclevrsp.Retrieve(clev, qrsp);
			AssertPtr(qrsp);
			for (int ifld = 0; ifld < qrsp->m_vqbsp.Size(); ++ifld)
			{
				if (qrsp->m_vqbsp[ifld]->m_hvoPssl != hvoCur)
					continue;
				// This list is used by a custom field so notify the user then exit.
				const achar * pszHelpUrl;
				pszHelpUrl = m_pszHelpUrl;

				m_pszHelpUrl = _T("DeletingAViewFilterSortMethodO.htm");
				StrApp strLab;
				strLab.Load(kstidCleNotDeleteList);
				StrApp strM;
				strM.Load(kstidCleNotDeleteListMsg);
				StrApp strMsg;

				StrApp strCust;
				const OLECHAR * prgwch;
				int cch;
				qrsp->m_vqbsp[ifld]->m_qtssLabel->LockText(&prgwch, &cch);
				strCust.Assign(prgwch, cch);
				qrsp->m_vqbsp[ifld]->m_qtssLabel->UnlockText(prgwch);

				strMsg.Format(strM.Chars(),strCust.Chars());
				::MessageBox(m_hwnd, strMsg.Chars(), strLab.Chars(), MB_HELP |
					MB_OK | MB_DEFBUTTON2 | MB_ICONINFORMATION);
				m_pszHelpUrl = pszHelpUrl;
				return true;
			}
		}
	}

	// This list is NOT used by a custom field so we can delete it.
	// Make sure the user really wants to delete the list.

	StrApp strDatabase(m_vqstuDatabases[m_iProj]);
	StrApp strSubitem(m_vsi[m_iSubitem].GetName());
	ConfirmRemoveProjectSubitemDlgPtr qcrps;
	qcrps.Create();
	qcrps->Initialize(strDatabase.Chars(), strSubitem.Chars());
	// We need to set the Help File and URL. Unfortunately, there is no reference to
	// the help file alone, because the parent class OpenProjDlg::Init() method is
	// given a full help url.
	// Get FW code root folder:
	StrAppBufPath strbpFwPath;
	strbpFwPath.Assign(DirectoryFinder::FwRootCodeDir().Chars());

	StrApp strHelpFile = strbpFwPath.Chars();
	strHelpFile.Append(_T("\\Helps\\FieldWorks_Topics_List_Editor_Help.chm"));
	qcrps->SetHelpFile(strHelpFile.Chars());
	qcrps->SetHelpUrl(_T("User_Interface/Menus/File/Delete_Topics_List.htm"));
	if (qcrps->DoModal(m_hwnd) != kctidOk)
		return true;

	// It's essential that we not allow partial updates or we can damage the database to where
	// a user can't get started again.
	IOleDbEncapPtr qode;
	bool fCurrentDb = false;
	if (m_vqstuDatabases[m_iProj] == pdbi->DbName())
	{
		pdbi->GetDbAccess(&qode);
		fCurrentDb = true;
	}
	else
	{
		qode.CreateInstance(CLSID_OleDbEncap);
		CheckHr(qode->Init(m_stuServerName.Bstr(), m_vqstuDatabases[m_iProj].Bstr(),
			m_qfist, koltMsgBox, koltvForever));
	}
	AssertPtr(qode);
	IOleDbCommandPtr qodc;
	StrUni stuSql;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	int nErr = 0;
	WaitCursor wc;

	CheckHr(qode->CreateCommand(&qodc));
	// GetRowset will fail after DeleteObj$  if nocount is off.
	stuSql.Format(L"declare @fIsNocountOn int, @err integer; "
		L"set @fIsNocountOn = @@options & 512; if @fIsNocountOn = 0 set nocount on; "
		L"EXEC @err = DeleteObjects '%d'; select @err;  "
		L"if @fIsNocountOn = 0 set nocount off", hvoCur);
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&nErr),
			isizeof(int), &cbSpaceTaken, &fIsNull, 0));
	}
	if (nErr)
	{
		Assert(false);
		return true;
	}

	if (fCurrentDb)
	{
		Vector<HVO> & vhvo = plpi->GetPsslIds();
		for (int ihvo = 0; ihvo < vhvo.Size(); ++ihvo)
		{
			if (vhvo[ihvo] == hvoCur)
			{
				vhvo.Delete (ihvo);
				break;
			}
		}
	}

	m_vsi.Delete(m_iSubitem);
	TreeView_DeleteItem(m_hwndListList, htri);
	if (m_iSubitem == m_vsi.Size())
		--m_iSubitem;

	return true;
}


/*----------------------------------------------------------------------------------------------
	Enable/disable the "Remove this Topics List..." menu item.

	@param hmenu parent popup menu.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool CleOpenProjDlg::SetRemoveListState(HMENU hmenu)
{
	StrApp str = m_stuLocalServer;
	// Disable, if not on local computer, or not a built-in list.
	UINT ui = ((_tcsicmp(m_szServerName, str.Chars()) != 0)
		|| (m_vsi[m_iSubitem].GetOwner() != 0))? MF_GRAYED : MF_ENABLED;
	EnableMenuItem(hmenu, kcidCleDeleteList, MF_BYCOMMAND | ui);

	return true;
}


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)

// We have to do explicit instantiation here because SubitemInfo is a protected embedded class
// in OpenProjDlg.
#include "Vector_i.cpp"

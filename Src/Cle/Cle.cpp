/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2000, SIL International. All rights reserved.

File: Cle.cpp
Responsibility: Rand Burgett
Last reviewed: never

Description:
	This file provides the base for Research Notebook functions.  It contains class
	definitions for the following classes:

		CleChangeWatcher : IVwNotifyChange
		CleDbInfo : AfDbInfo
		CleLpInfo : AfLpInfo
		CleApp : AfDbApp
		CleMainWnd : RecMainWnd
		CleTreeBar : AfTreeBar
		CleListBar : AfListBar
		CleCaptionBar : AfCaptionBar
		CleFilterNoMatchDlg : FwFilterNoMatchDlg
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"
#include "Set_i.cpp"
#include "HashMap_i.cpp"
#include "MultiMap_i.cpp"
#include "GpHashMap_i.cpp" // Needed for release version.

#undef THIS_FILE
DEFINE_THIS_FILE

#undef LOG_FILTER_SQL
//#define LOG_FILTER_SQL 1
#ifdef LOG_FILTER_SQL
// Making this a static global avoids conditional method argument.
static FILE * fp = NULL;
#endif
//:End Ignore

// Create one global instance. It has to exist before WinMain is called.
static CleApp g_app;

BEGIN_CMD_MAP(CleApp)
	ON_CID_ALL(kcidFileOpen, &CleApp::CmdFileOpenProj, NULL)
	ON_CID_ALL(kcidFileBackup, &CleApp::CmdFileBackup, NULL)
	ON_CID_ALL(kcidFileExit, &CleApp::CmdFileExit, NULL)
	ON_CID_ALL(kcidHelpAbout, &CleApp::CmdHelpAbout, NULL)
	ON_CID_ALL(kcidWndCascad, &CleApp::CmdWndCascade, NULL)
	ON_CID_ALL(kcidWndTile, &CleApp::CmdWndTileHoriz, NULL)
	ON_CID_ALL(kcidWndSideBy, &CleApp::CmdWndTileVert, NULL)
END_CMD_MAP_NIL()

/*----------------------------------------------------------------------------------------------
	The command map for tree bar window.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(CleTreeBar)
	ON_CID_GEN(kcidTrBarInsert, &CleTreeBar::CmdTrbMenu, &CleTreeBar::CmsTrbMenu)
	ON_CID_GEN(kcidTrBarInsertBef, &CleTreeBar::CmdTrbMenu, &CleTreeBar::CmsTrbMenu)
	ON_CID_GEN(kcidTrBarInsertAft, &CleTreeBar::CmdTrbMenu, &CleTreeBar::CmsTrbMenu)
	ON_CID_GEN(kcidTrBarInsertSub, &CleTreeBar::CmdTrbMenu, &CleTreeBar::CmsTrbMenu)
	ON_CID_GEN(kcidTrBarMerge, &CleTreeBar::CmdTrbMenu, &CleTreeBar::CmsTrbMenu)
	ON_CID_GEN(kcidTrBarDelete, &CleTreeBar::CmdTrbMenu, &CleTreeBar::CmsTrbMenu)
	ON_CID_GEN(kcidViewTreeAbbrevs, &CleTreeBar::CmdTrbMenu, &CleTreeBar::CmsTrbMenu)
	ON_CID_GEN(kcidViewTreeNames, &CleTreeBar::CmdTrbMenu, &CleTreeBar::CmsTrbMenu)
	ON_CID_GEN(kcidViewTreeBoth, &CleTreeBar::CmdTrbMenu, &CleTreeBar::CmsTrbMenu)
END_CMD_MAP_NIL()

/*----------------------------------------------------------------------------------------------
	The command map for the main window.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(CleMainWnd)
	ON_CID_GEN(kcidViewStatBar, &CleMainWnd::CmdSbToggle, &CleMainWnd::CmsSbUpdate)
	ON_CID_ME(kcidToolsOpts, &RecMainWnd::CmdToolsOpts, NULL)

	ON_CID_ALL(kcidFileNewLP, &CleMainWnd::CmdFileNewProj, NULL)
	ON_CID_ALL(kcidFileNewTL, &CleMainWnd::CmdFileNewTList, NULL)
	ON_CID_ALL(kcidFileSave, &CleMainWnd::CmdFileSave, &CleMainWnd::CmsFileSave)
	ON_CID_ALL(kcidFilePropsTL, &CleMainWnd::CmdListsProps, NULL)
	ON_CID_ALL(kcidFilePropsLP, &RecMainWnd::CmdFileProjProps, NULL)
	ON_CID_ALL(kcidFileExpt, &CleMainWnd::CmdFileExport, &CleMainWnd::CmsFileExport)

	ON_CID_ALL(kcidEditDel, &CleMainWnd::CmdEditDelete, &RecMainWnd::CmsHaveRecord)
	ON_CID_ALL(kcidEditUndo, &RecMainWnd::CmdEditUndo, &RecMainWnd::CmsEditUndo)
	ON_CID_ALL(kcidEditRedo, &RecMainWnd::CmdEditRedo, &RecMainWnd::CmsEditRedo)

	ON_CID_ME(kcidDataFirst, &RecMainWnd::CmdRecSel, &RecMainWnd::CmsRecSelUpdate)
	ON_CID_ME(kcidDataLast, &RecMainWnd::CmdRecSel, &RecMainWnd::CmsRecSelUpdate)
	ON_CID_ME(kcidDataNext, &RecMainWnd::CmdRecSel, &RecMainWnd::CmsRecSelUpdate)
	ON_CID_ME(kcidDataPrev, &RecMainWnd::CmdRecSel, &RecMainWnd::CmsRecSelUpdate)

	ON_CID_ME(kcidHelpConts,     &CleMainWnd::CmdHelpFw,   NULL)
	ON_CID_ME(kcidHelpApp,       &CleMainWnd::CmdHelpApp,  NULL)
	ON_CID_ME(kcidHelpWhatsThis, &CleMainWnd::CmdHelpMode, NULL)

	ON_CID_ME(kcidWndNew, &CleMainWnd::CmdWndNew, NULL)

	ON_CID_ME(kcidWndSplit, &CleMainWnd::CmdWndSplit, &CleMainWnd::CmsWndSplit)
	ON_CID_ME(kcidViewVbar, &CleMainWnd::CmdVbToggle, &CleMainWnd::CmsVbUpdate)
	ON_CID_ME(kcidViewSbar, &CleMainWnd::CmdSbToggle, &CleMainWnd::CmsSbUpdate)
	ON_CID_ME(kcidViewViewsConfig, &RecMainWnd::CmdToolsOpts, NULL)
	ON_CID_ME(kcidViewFltrsConfig, &RecMainWnd::CmdToolsOpts, NULL)
	ON_CID_ME(kcidViewSortsConfig, &RecMainWnd::CmdToolsOpts, NULL)
	ON_CID_ME(kcidViewTreeAbbrevs, &CleMainWnd::CmdViewTree, &CleMainWnd::CmsViewTree)
	ON_CID_ME(kcidViewTreeNames, &CleMainWnd::CmdViewTree, &CleMainWnd::CmsViewTree)
	ON_CID_ME(kcidViewTreeBoth, &CleMainWnd::CmdViewTree, &CleMainWnd::CmsViewTree)
//	ON_CID_ME(kcidViewOlaysConfig, &RecMainWnd::CmdToolsOpts, NULL)

	ON_CID_CHILD(kcidExpViews, &CleMainWnd::CmdViewExpMenu, &CleMainWnd::CmsViewExpMenu)
	ON_CID_CHILD(kcidExpFilters, &CleMainWnd::CmdViewExpMenu, &CleMainWnd::CmsViewExpMenu)
	ON_CID_CHILD(kcidExpSortMethods, &CleMainWnd::CmdViewExpMenu, &CleMainWnd::CmsViewExpMenu)

	ON_CID_ME(kcidInsListItem, &CleMainWnd::CmdInsertEntry, &RecMainWnd::CmsInsertEntry)
	ON_CID_ME(kcidInsListItemBef, &CleMainWnd::CmdInsertEntry, &RecMainWnd::CmsInsertEntry)
	ON_CID_ME(kcidInsListItemAft, &CleMainWnd::CmdInsertEntry, &RecMainWnd::CmsInsertEntry)
	ON_CID_ME(kcidInsListSubItem, &CleMainWnd::CmdInsertEntry, &RecMainWnd::CmsInsertEntry)

	ON_CID_ME(kcidExtLinkOpen, &RecMainWnd::CmdExternalLink, NULL)
	ON_CID_ME(kcidExtLinkOpenWith, &RecMainWnd::CmdExternalLink, NULL)
	ON_CID_ME(kcidExtLink, &RecMainWnd::CmdExternalLink, &RecMainWnd::CmsInsertLink)
	ON_CID_ME(kcidExtLinkRemove, &RecMainWnd::CmdExternalLink, NULL)

	ON_CID_CHILD(kcidViewFullWindow, &RecMainWnd::CmdViewFullWindow,
			&RecMainWnd::CmsViewFullWindow)
END_CMD_MAP_NIL()

/*----------------------------------------------------------------------------------------------
	The command map for our Cle generic list bar window.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(CleListBar)
	ON_CID_GEN(kcidViewViewsConfig, &CleListBar::CmdToolsOpts, NULL)
	ON_CID_GEN(kcidViewFltrsConfig, &CleListBar::CmdToolsOpts, NULL)
	ON_CID_GEN(kcidViewSortsConfig, &CleListBar::CmdToolsOpts, NULL)
END_CMD_MAP_NIL()

//:>********************************************************************************************
//:>    Generic factory stuff to allow creating an instance of AfFwTool with CoCreateInstance,
//:>	so as to start up new main windows from another EXE (e.g., the Explorer).
//:>********************************************************************************************
static GenericFactory g_fact(
	kpszCleProgId,
	&CLSID_ChoicesListEditor,
	_T("SIL Choices List Editor"),
	_T("Apartment"),
	&AfFwTool::CreateCom);

/*----------------------------------------------------------------------------------------------
	Get the hvo of the Weather Conditions list. This list is chosen because it exists and
	is not empty.
	@param qaflp Smart pointer to the New Project dialog.
	@return hvo of the Weather Conditions list, or 0 if not obtained.
----------------------------------------------------------------------------------------------*/
HVO GetWeatherConditionsHvo(const BSTR bstrServer, const BSTR bstrDatabase,
							const HVO hvoLP)
{
	HVO hvoWeatherConditions = 0;
	try
	{
		IOleDbEncapPtr qode;				// Current connection. Declare before qodc.
		IOleDbCommandPtr qodc;				// Currently-executing command.
		IStreamPtr qfist;
		AfApp::Papp()->GetLogPointer(&qfist); // Get the IStream for logging, if it exists.
		qode.CreateInstance(CLSID_OleDbEncap);
		CheckHr(qode->Init(bstrServer, bstrDatabase, qfist, koltMsgBox, koltvForever));
		CheckHr(qode->CreateCommand(&qodc));
		StrUni stuCommand;
		ComBool fMoreRows;
		ComBool fIsNull;			// True if the value sought by GetColValue is NULL.
		ULONG cbSpaceTaken;			// Size of data returned by GetColValue.
		stuCommand.Format(L"SELECT Dst FROM LangProject_WeatherConditions WHERE Src = %d",
			hvoLP);
		CheckHr(qodc->ExecCommand(stuCommand.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		Assert (fMoreRows);
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoWeatherConditions),
			sizeof(hvoWeatherConditions), &cbSpaceTaken, &fIsNull, 0));
	}
	catch (...)
	{
		ThrowInternalError(E_UNEXPECTED);	// The Weather Conditions list "has to be there".
	}
	return hvoWeatherConditions;
}

//:>********************************************************************************************
//:>	CleApp methods.
//:>********************************************************************************************

#define REGPROGNAME _T("CLE")	// Program name for registry key.

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CleApp::CleApp()
{
	s_fws.SetRoot(REGPROGNAME);		// => "Software\\SIL\\FieldWorks\\CLE";

	AfApp::Papp()->SetHelpBaseName(_T("\\Helps\\FieldWorks_Topics_List_Editor_Help.chm"));
}


/*----------------------------------------------------------------------------------------------
	Initialize the application. Display the splash screen, open initial window, connect to
	database, initialize status bar, initialize panes, remove splash screen.
----------------------------------------------------------------------------------------------*/
void CleApp::Init(void)
{
	// Call init before any other database activity.
	SuperClass::Init();

	AfWnd::RegisterClass(_T("CleMainWnd"), 0, 0, 0, COLOR_3DFACE, (int)kridCleIcon);

	// If we are starting as a server, we don't open a window.
	if (HasEmbedding())
		return;

	StrUni stuDefListName(kstidDefaultListName);
	StrUni stuProjTableName(L"LangProject");
	HashMapStrUni<StrUni> hmsustuOptions;
	HVO hvoLpId = 0;
	HVO hvoListId;
	bool fContinueLoop = true;
	bool bFileOpened = false;
	StrUni stuServer;
	StrUni stuDatabase;
	StrUni stuListName;
	bool fAllowNoOwner = true;
	int iRetVal;
	HVO hvoNew = -1;

	while (fContinueLoop)
	{
		OptionsReturnValue orv = ProcessDBOptions(kclidCmPossibilityList, &stuDefListName,
			kclidLangProject, &stuProjTableName, &hmsustuOptions, hvoLpId, hvoListId,
			true, fAllowNoOwner);
		if (orv == korvInvalidObjectName)
		{
			// Retrieve the list name and put up "list not found" dialog.
			StrUni stuKey(L"item");
			hmsustuOptions.Retrieve(stuKey, &stuListName);
			StrApp strList(stuListName.Chars());
			CleLstNotFndDlgPtr qlnf;
			qlnf.Create();
			qlnf->SetList(strList.Chars());
			iRetVal = qlnf->DoModal(NULL);
			switch (iRetVal)
			{
			case kctidLstNotFndOpen:
				iRetVal = kctidPrjNotFndOpen;
				break;
			case kctidLstNotFndNew:
				{ // Block
					StrUni stuKey(L"server");
					hmsustuOptions.Retrieve(stuKey, &stuServer);
					stuKey = L"database";
					hmsustuOptions.Retrieve(stuKey, &stuDatabase);
					AfDbInfo * pdbi = GetDbInfo(stuDatabase.Chars(), stuServer.Chars());
					IOleDbEncapPtr qode;
					pdbi->GetDbAccess(&qode);
					// Create a new list:
					hvoNew = NewTopicsList(qode);
					hvoListId = hvoNew;
					iRetVal = IDOK;
				} // End block
				break;
			case kctidLstNotFndExit:
			default:	// Fallthrough.
				iRetVal = kctidPrjNotFndExit;
			}
		}
		else
		{
			iRetVal = ProcessOptRetVal(orv, &hmsustuOptions, true);
		}

		switch (iRetVal)
		{
		case IDOK:
			fContinueLoop = false;
			break;	// No problem, continue with the found project.
		case kctidPrjNotFndOpen:
			{
				// User chose to open a project of their choice. Execute the File-Open dialog.
				FileOpenProjectInfoPtr qfopi;
				// May return null if cancelled.
				qfopi.Attach(DoFileOpenProject());

				if (qfopi.Ptr() != NULL && qfopi->m_fHaveProject && qfopi->m_fHaveSubitem)
				{
					stuDatabase = qfopi->m_stuDatabase;
					stuServer = qfopi->m_stuMachine;
					stuListName = qfopi->m_stuSubitemName;
					hvoLpId = qfopi->m_hvoProj;
					hvoListId = qfopi->m_hvoSubitem;
					fContinueLoop = false;
					bFileOpened = true;
				}
			}
			break;
		case kctidPrjNotFndNew:
			{
				// Bring up the C# wizard.
				FwCoreDlgs::ICreateLangProjectPtr qclp;
				qclp.CreateInstance("FwCoreDlgs.CreateLangProject");
				IHelpTopicProviderPtr qhtprov = new HelpTopicProvider(m_strHelpBaseName.Chars());
				qclp->SetDialogProperties(qhtprov);
				long nRet;
				CheckHr(qclp->DisplayDialog(&nRet));
				if (nRet == kctidOk)	// If project is created successfully.
				{
					// Get relevant data from the wizard.
					SmartBstr sbstrDatabase;
					SmartBstr sbstrServer;

					CheckHr(qclp->GetServerName(&sbstrServer));
					CheckHr(qclp->GetDatabaseName(&sbstrDatabase));
					CheckHr(qclp->GetProjLP(&hvoLpId));
					stuServer.Assign(sbstrServer.Chars());
					stuDatabase.Assign(sbstrDatabase.Chars());
					hvoListId = GetWeatherConditionsHvo(sbstrServer, sbstrDatabase, hvoLpId);
					fContinueLoop = false;
					bFileOpened = true;
				}
				else if (nRet == -1)
				{
					DWORD dwError = ::GetLastError();
					achar rgchMsg[MAX_PATH+1];
					DWORD cch = ::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, NULL, dwError, 0,
						rgchMsg, MAX_PATH, NULL);
					rgchMsg[cch] = 0;
					StrApp strTitle(kstidWizProjMsgCaption);
					::MessageBox(m_hwnd, rgchMsg, strTitle.Chars(), MB_OK | MB_ICONWARNING);
				}
			}
			break;
		case kctidPrjNotFndRestore:
			{
				DIFwBackupDbPtr qzbkup;
				qzbkup.CreateInstance(CLSID_FwBackup);
				qzbkup->Init(this, 0);
				int nUserAction;
				qzbkup->UserConfigure(NULL, (ComBool)true, &nUserAction);
				qzbkup->Close();
				if (nUserAction != 4/*BackupHandler::kRestoreOk*/)
					AfApp::Papp()->Quit(true);	// Backup/Restore didn't open a window.
			}
			return;	// Assume that restore will have opened a frame window etc.
		case kctidPrjNotFndExit:
		default:	// Fallthrough.	E.g. when 'x' is used to close the dialog.
			AfApp::Papp()->Quit(true);
			return;
		}
	}
	if (!bFileOpened)
	{
		// Fish the server and database names from the map.
		StrUni stuKey(L"server");
		hmsustuOptions.Retrieve(stuKey, &stuServer);
		stuKey = L"database";
		hmsustuOptions.Retrieve(stuKey, &stuDatabase);
	}

	AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
	Assert(pdapp);
	if (!pdapp->CheckDbVerCompatibility(stuServer.Chars(), stuDatabase.Chars()))
	{
		AfApp::Papp()->Quit(true);
		return;
	}
	// Start up the application.
	Assert(hvoLpId);
	Assert(hvoListId);
	IFwToolPtr qtool;
	qtool.CreateInstance(CLSID_ChoicesListEditor);
	long htool;
	int pidNew;

	IStreamPtr qfist;
	GetLogPointer(&qfist); // Get the IStream for logging, if it exists.
	IOleDbEncapPtr qode;
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(stuServer.Bstr(), stuDatabase.Bstr(), qfist,
		koltMsgBox, koltvForever));
	int wsUser = UserWs(qode);
	// Close the connection before opening the application since we only want one connection
	// open to avoid various problems with backup, synchronization, etc.
	qode.Clear();
	CheckHr(qtool->NewMainWnd(stuServer.Bstr(), stuDatabase.Bstr(),
		hvoLpId, hvoListId, wsUser, 0, 0, &pidNew, &htool));
	// Split the status bar into five sections of varying widths:
	//   1. record id
	//   2. progress / info
	//   3. sort
	//   4. filter
	//   5. count
	CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(m_qafwCur.Ptr());
	// m_qafwCur is not set if we latched onto another app, but this initialization will
	// already be done.
	bool fCloseApp = false;
	if (pcmw)
	{
		AfDbInfo * pdbi = GetDbInfo(stuDatabase.Chars(), stuServer.Chars());
		AfStatusBarPtr qstbr = pcmw->GetStatusBarWnd();
		Assert(qstbr);
		qstbr->InitializePanes();
		pcmw->UpdateStatusBar();
		if (hvoNew != -1)
			::SendMessage(pcmw->Hwnd(), WM_COMMAND, kcidFilePropsTL, 0);
		pcmw->CheckEmptyRecords(NULL, NULL, fCloseApp);
		if (!fCloseApp)
			pdbi->CheckTransactionKludge();
	}
	if (!fCloseApp)
	{
		// Check if a backup is overdue.
		HWND hwnd = NULL;
		if (pcmw)
			hwnd = pcmw->Hwnd();
		DIFwBackupDbPtr qzbkup;
		qzbkup.CreateInstance(CLSID_FwBackup);
		qzbkup->Init(this, (int)hwnd);
		IHelpTopicProviderPtr qhtprov = new HelpTopicProvider(m_strHelpBaseName.Chars());
		qzbkup->CheckForMissedSchedules(qhtprov);
		qzbkup->Close();
	}
}


/*----------------------------------------------------------------------------------------------
	Gets the App and Database Version information from the app.

	@param nAppVer Out Application Version
	@param nErlyVer Out The earliest database version that is Compatible with this Application
					Version.
	@param nLastVer Out The Last known database version that is compatible with this Application
					version.

	@return always false.
----------------------------------------------------------------------------------------------*/
bool CleApp::GetAppVer(int & nAppVer, int & nErlyVer, int & nLastVer)
{
	nAppVer = knApplicationVersion;
	nErlyVer = knDbVerCompatEarliest;
	nLastVer = knDbVerCompatLastKnown;
	return false;
}

/*----------------------------------------------------------------------------------------------
	Gets the resource id of the App name from the app.

	@return resource id
----------------------------------------------------------------------------------------------*/
int CleApp::GetAppNameId()
{
	return kstidChoicesListEditor;
}


/*----------------------------------------------------------------------------------------------
	Gets the resource id of the App name to be used in Properites Dialog from the app.

	@return resource id
----------------------------------------------------------------------------------------------*/
int CleApp::GetAppPropNameId()
{
	return kstidChoicesListEditor;
}


/*----------------------------------------------------------------------------------------------
	Gets the resource id of the Application icon from the app.

	@return resource id
----------------------------------------------------------------------------------------------*/
int CleApp::GetAppIconId()
{
	return kridCleIcon;
}


/*----------------------------------------------------------------------------------------------
	Return the matching AfDbInfo. If it is not already loaded, create it now.

	@param pszDbName Db name
	@param pszSvrName Server name

	@return Smart pointer to Db Into object
----------------------------------------------------------------------------------------------*/
AfDbInfo * CleApp::GetDbInfo(const OLECHAR * pszDbName, const OLECHAR * pszSvrName)
{
	AssertPsz(pszDbName);
	AssertPsz(pszSvrName);
	SetSplashLoadingMessage(pszDbName);

	int cdbi = m_vdbi.Size();
	for (int idbi = 0; idbi < cdbi; idbi++)
	{
		if (wcscmp(pszDbName, m_vdbi[idbi]->DbName()) == 0 &&
			wcscmp(pszSvrName, m_vdbi[idbi]->ServerName()) == 0)
		{
			return m_vdbi[idbi];
		}
	}

	// We didn't find it in the cache, so load it now.
	CleDbInfoPtr qdbi;
	qdbi.Create();
	qdbi->Init(pszSvrName, pszDbName, m_qfistLog);
	m_vdbi.Push(qdbi.Ptr());
	return qdbi;
}

/*----------------------------------------------------------------------------------------------
	Gets the default backup directory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CleApp::GetDefaultBackupDirectory(BSTR * pbstrDefBackupDir)
{
	BEGIN_COM_METHOD;

	// TODO: return the default backup directory (My Documents/My FieldWorks/Backups)
	*pbstrDefBackupDir = NULL;

	END_COM_METHOD(g_fact, IID_IBackupDelegates);
}

/*----------------------------------------------------------------------------------------------
	Create a new main window, and set up the wcs.

	@param wcs The struct that is used by the caller to set up the window more.

	@return Pointer to a new RecMainWnd.
----------------------------------------------------------------------------------------------*/
RecMainWnd * CleApp::CreateMainWnd(WndCreateStruct & wcs, FileOpenProjectInfo * pfopi)
{
	wcs.InitMain(_T("CleMainWnd"));
	CleMainWndPtr qcmw;
	qcmw.Create();
	qcmw->SetHvoPssl(pfopi->m_hvoSubitem);
	RecMainWnd * prmw = qcmw;
	qcmw.Detach();
	return prmw;
}

/*----------------------------------------------------------------------------------------------
	Open a new main window on the specified data.
	Temporarily, fail if we have a window open and it is a different database. Later, we
	will be able to handle multiple databases.

	See ${AfDbApp#NewMainWnd}
----------------------------------------------------------------------------------------------*/
void CleApp::NewMainWnd(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
	int hvoMainObj, int encUi, int nTool, int nParam, DWORD dwRegister)
{
	CleMainWndPtr qcmw;
	qcmw.Create();
	qcmw->SetHvoPssl(hvoMainObj);
	SuperClass::NewMainWnd(bstrServerName, bstrDbName, hvoLangProj, hvoMainObj, encUi, nTool,
		nParam, qcmw, _T("CleMainWnd"), kridSplashStartMessage, dwRegister);
}


/*----------------------------------------------------------------------------------------------
	Open a new main window on the specified data using a specified view and starting field.
	Temporarily, fail if we have a window open and it is a different database. Later, we
	will be able to handle multiple databases.

	See ${AfDbApp#NewMainWndWithSel}
----------------------------------------------------------------------------------------------*/
void CleApp::NewMainWndWithSel(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
	int hvoMainObj, int encUi, int nTool, int nParam, const HVO * prghvo, int chvo,
		const int * prgflid, int cflid, int ichCur, int nView, DWORD dwRegister)
{
	CleMainWndPtr qcmw;
	qcmw.Create();
	qcmw->SetHvoPssl(hvoMainObj);
	qcmw->SetStartupInfo(prghvo, chvo, prgflid, cflid, ichCur, nView);
	SuperClass::NewMainWnd(bstrServerName, bstrDbName, hvoLangProj, hvoMainObj, encUi, nTool,
		nParam, qcmw, _T("CleMainWnd"), kridSplashStartMessage, dwRegister);

	AfStatusBarPtr qstbr = qcmw->GetStatusBarWnd();
	Assert(qstbr);
	qstbr->InitializePanes();
	qcmw->UpdateStatusBar();
	bool fCancel;
	qcmw->CheckEmptyRecords(NULL, NULL, fCancel);
}


/*----------------------------------------------------------------------------------------------
	Re-open a new main window on the specified data. This is intended to be used after a Restore
	operation, that the user is not left with nothing after his windows were shut down prior
	to the restore. It can also be used after other	major operations such as replacing all
	encodings.
	@param pszDbName Name of the database to open.
	@param pszSvrNam Name of the database server.
	@param hvo Optional HVO of list to open. It defaults to the first list in the database, if
		zero.
----------------------------------------------------------------------------------------------*/
void CleApp::ReopenDbAndOneWindow(const OLECHAR * pszDbName, const OLECHAR * pszSvrName,
	HVO hvo)
{
	ComBool fIsNull;
	ComBool fMoreRows;
	HRESULT hr;
	ULONG luSpaceTaken;
	IOleDbEncapPtr qode; // Declare before qodc.
	IOleDbCommandPtr qodc;
	StrUni stuSql;
	StrUni stuServerName = pszSvrName;
	StrUni stuDatabase = pszDbName;
	HVO hvoPssl = hvo;
	HVO hvoLp = 0;

	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(hr = qode->Init(stuServerName.Bstr(), stuDatabase.Bstr(), m_qfistLog, koltMsgBox,
		koltvForever));
	CheckHr(hr = qode->CreateCommand(&qodc));

	// If we decide to have multiple language projects in a database, these queries need to
	// be made smarter to make sure we get the correct language project for the given list.
	if (!hvo)
		stuSql = L"declare @lp int, @pl int "
				L"select top 1 @lp = id from LangProject "
				L"select top 1 @pl = id from CmPossibilityList "
				L"select @lp, @pl";
	else
		stuSql = L"select top 1 id from LangProject";

	CheckHr(hr = qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(hr = qodc->GetRowset(0));
	CheckHr(hr = qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		CheckHr(hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoLp), isizeof(hvoLp),
			&luSpaceTaken, &fIsNull, 0));
		if (!hvo)
		{
			CheckHr(hr = qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hvoPssl),
				isizeof(hvoPssl), &luSpaceTaken, &fIsNull, 0));
		}
	}

	// Now launch a list editor on the desired list.
	int ws = UserWs(qode);
	long htool = 0;
	int pidNew;
	IFwToolPtr qtool;
	qtool.CreateInstance(CLSID_ChoicesListEditor);
	CheckHr(qtool->NewMainWnd(stuServerName.Bstr(), stuDatabase.Bstr(),  hvoLp, hvoPssl, ws,
		0, // tool-dependent identifier of which tool to use; does nothing for RN yet
		0, // another tool-dependend parameter, does nothing in RN yet
		&pidNew,
		&htool)); // value you can pass to CloseMainWnd if you want.
}


//:>********************************************************************************************
//:>	CleMainWnd methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CleMainWnd::CleMainWnd()
{
	m_dxpDeTreeWidth = 180; // Default tree width if nothing comes from registry.

	m_ivblt[kvbltTree] = 0;
	m_ivblt[kvbltView] = 1;
	m_ivblt[kvbltFilter] = 2;
	m_ivblt[kvbltSort] = 3;
	m_ivbltMax = m_ivblt[kvbltSort];
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CleMainWnd::~CleMainWnd()
{
}


void CleMainWnd::MakeJumpWindow(Vector<HVO> & vhvo, Vector<int> & vflid, int nView)
{
	WndCreateStruct wcs;
	wcs.InitMain(_T("CleMainWnd"));

	PrepareNewWindowLocation();

	CleMainWndPtr qcmw;
	qcmw.Create();
	qcmw->Init(m_qlpi);
	qcmw->SetStartupInfo(vhvo.Begin(), vhvo.Size(), vflid.Begin(), vflid.Size(), 0, nView);
	qcmw->CreateHwnd(wcs);
}

VwCustDocVc * CleMainWnd::CreateCustDocVc(UserViewSpec * puvs)
{
	return NewObj CleCustDocVc(puvs, m_qlpi, this);
}

AfSplitChild * CleMainWnd::CreateNewSplitChild()
{
	if (m_vwt == kvwtDE)
		return NewObj CleDeSplitChild();

	return SuperClass::CreateNewSplitChild();
}

AfDbInfo * CleMainWnd::CheckEmptyRecords(AfDbInfo * pdbi, StrUni stuProject, bool & fCancel)
{
	fCancel = false;
	if (!RawRecordCount())
	{
		// Empty database.
		CleEmptyListDlgPtr qremp;
		qremp.Create();
		StrApp strProj(GetRootObjName());
		qremp->SetProject(strProj.Chars());
		int ctid = qremp->DoModal(Hwnd());
		switch (ctid)
		{
		case kctidEmptyLNewItem:
			::SendMessage(Hwnd(), WM_COMMAND, kcidInsListItem, 0);
			break;
		case kctidEmptyLImport:
//			::SendMessage(Hwnd(), WM_COMMAND, kcidFileImpt, 0);
			break;
		case kctidCancel:
			::PostMessage(Hwnd(), WM_COMMAND, kcidFileExit, 0);
			fCancel = true;
			break;
		}
	}
	return pdbi;
}

static int PossListHvoForFlid(AfDbInfo * pdbi, int flid)
{
	FldSpecPtr qfsp = pdbi->FindFldSpec(flid);
	AssertPtr(qfsp);
	Assert(qfsp->m_ft == kftRefCombo || qfsp->m_ft == kftRefAtomic ||
		qfsp->m_ft == kftRefSeq);
	return qfsp->m_hvoPssl;
}

/*----------------------------------------------------------------------------------------------
	Returns a reference to the vector of filter menu nodes.  If the size of the vector (m_vfmn)
	is 0 then the menu nodes are created being pushed into the vector.  Then go through
	all the menu items recursively and assign the field type for each field.

	@param plpi ptr to Language Project Info object.

	@return reference to the vector of the filter menu nodes.
----------------------------------------------------------------------------------------------*/
FilterMenuNodeVec * CleMainWnd::GetFilterMenuNodes(AfLpInfo * plpi)
{
	AssertPtr(plpi);

	if (m_vfmn.Size() != 0)
		return &m_vfmn;

	FilterMenuNodePtr qfmnPopup;
	FilterMenuNodePtr qfmnLeaf;

	Vector<HVO> & vhvoPssl = plpi->GetPsslIds();
	FilterMenuNodeVec vfmnPerson;

	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	IFwMetaDataCachePtr qmdc;
	pdbi->GetFwMetaDataCache(&qmdc);
	IOleDbEncapPtr qode;
	pdbi->GetDbAccess(&qode);

	int clidPss = GetRecordClid();
	Vector<FieldData> vfd;
	Vector<FieldData> vfdPerson;
	HashMap<int, LabelData> hmclidld;
	HashMap<int, HashMap<int, LabelData> > hmflidhmclidld;
	HashMap<int, Vector<int> > hmclidvclidBase;

#ifdef LOG_FILTER_SQL
	FILE * fp = fopen("c:/FW/DebugOutput.txt", "a");
	StrAnsiBufBig stab;
	if (fp)
	{
		fprintf(fp, "\n\
===============================================================================\n");
		time_t nTime;
		time(&nTime);
		fprintf(fp, "DEBUG CleMainWnd::GetFilterMenuNodes(AfLpInfo * plpi) at %s",
			ctime(&nTime));
	}
#endif
	try
	{
		IOleDbCommandPtr qodc;
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;
		StrUni stuQuery;
		StrUni stuT;

		int ws = pdbi->UserWs();

		Vector<int> vclid;		// Class ids of possibilities and their parent classes.
		Set<int> setclid;		// Easiest way to keep track of unique class ids.
		vclid.Push(clidPss);
		setclid.Insert(clidPss);

		// Now add the base classes (if any) of the element classes.
		int clid;
		int iclid;
		for (iclid = 0; iclid < vclid.Size(); ++iclid)
		{
			unsigned long cid;
			CheckHr(qmdc->GetBaseClsId(vclid[iclid], &cid));
			if (cid)
			{
#ifdef LOG_FILTER_SQL
				if (fp)
					fprintf(fp, "    Base Class of %d = %d\n", vclid[iclid], cid);
#endif
				Vector<int> vclidBase;
				if (!hmclidvclidBase.Retrieve(vclid[iclid], &vclidBase))
					vclidBase.Clear();
				vclidBase.Push(cid);
				hmclidvclidBase.Insert(vclid[iclid], vclidBase, true);
				clid = cid;
				if (!setclid.IsMember(clid))
				{
					setclid.Insert(clid);
					vclid.Push(clid);
				}
			}
		}
		// Convert the list of classes to a string for use in following queries.
		StrUni stuClasses;
		for (iclid = 0; iclid < vclid.Size(); ++iclid)
		{
			if (iclid)
				stuClasses.FormatAppend(L",%d", vclid[iclid]);
			else
				stuClasses.FormatAppend(L"%d", vclid[iclid]);
		}
		// Load the relevant field names from the user views in the database.
		// Ensure we get the CmPerson fields from the user views.
		StrUni stuAllClasses(stuClasses);
		clid = kclidCmPerson;
		if (!setclid.IsMember(clid))
			stuAllClasses.FormatAppend(L",%d", clid);
		stuQuery.Format(L"select distinct uvf.Flid, uvr.Clsid, mt.Txt"
			L" from UserViewField uvf"
			L" join UserViewField_Label mt on uvf.Id = mt.Obj"
			L" join CmObject cmo on uvf.Id = cmo.Id"
			L" join UserViewRec uvr on cmo.Owner$ = uvr.Id"
			L" where uvf.Flid in (select Id from Field$ where Class in (%s))"
			L"       AND uvr.Clsid in (%s)"
			L"       AND (mt.Ws = %d)"
			L" order by uvf.Flid",
			stuAllClasses.Chars(), stuAllClasses.Chars(), ws);
#ifdef LOG_FILTER_SQL
		if (fp)
		{
			stab.Format("%S", stuQuery.Chars());
			fprintf(fp, "SQL QUERY =\n%s\n\n", stab.Chars());
		}
#endif
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		const int kcchBuffer = MAX_PATH;
		OLECHAR rgchName[kcchBuffer];
		LabelData ld;
		LabelData ldT;
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&ld.flid), isizeof(ld.flid),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&ld.clid), isizeof(ld.clid),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(3, reinterpret_cast <BYTE *>(rgchName),
				isizeof(rgchName), &cbSpaceTaken, &fIsNull, 2));
			if (!fIsNull)
			{
#ifdef LOG_FILTER_SQL
				if (fp)
				{
					stab.Format("ld.flid = %d, ld.clid = %d, ld.stuLabel = \"%S\"",
						ld.flid, ld.clid, rgchName);
					fprintf(fp, "%s\n", stab.Chars());
				}
#endif
				ld.stuLabel.Assign(rgchName);
				hmclidld.Clear();
				if (hmflidhmclidld.Retrieve(ld.flid, &hmclidld) &&
					hmclidld.Retrieve(ld.clid, &ldT) &&
					(ld.stuLabel.Length() < ldT.stuLabel.Length()))
				{
					Assert(ld.flid == ldT.flid);
					Assert(ld.clid == ldT.clid);
#ifdef LOG_FILTER_SQL
					if (fp)
					{
						stab.Format(
							"        ldT.flid = %d, ldT.clid = %d, ldT.stuLabel = \"%S\"",
							ldT.flid, ldT.clid, ldT.stuLabel.Chars());
						fprintf(fp, "%s\n", stab.Chars());
					}
#endif
					ld.stuLabel = ldT.stuLabel;
				}
				hmclidld.Insert(ld.clid, ld, true);
				hmflidhmclidld.Insert(ld.flid, hmclidld, true);
			}
			CheckHr(qodc->NextRow(&fMoreRows));
		}

		// Get the basic field information of interest from the database.
		stuQuery.Format(L"select Id,Class,DstCls from Field$ "
			L"where Class in (%s)",
			stuClasses.Chars());
#ifdef LOG_FILTER_SQL
		if (fp)
		{
			stab.Format("%S", stuQuery.Chars());
			fprintf(fp, "\nSQL QUERY =\n%s\n\n", stab.Chars());
		}
#endif
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		FieldData fd;
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&fd.flid), isizeof(fd.flid),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&fd.clid), isizeof(fd.clid),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&fd.clidDest),
				isizeof(fd.clidDest), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				fd.clidDest = 0;
			// Store this field only if we have a proper label for it.
			if (hmflidhmclidld.Retrieve(fd.flid, &hmclidld))
			{
				bool fFound = hmclidld.Retrieve(clidPss, &ldT);
				if (!fFound)
					fFound = hmclidld.Retrieve(fd.clid, &ldT);
#ifdef LOG_FILTER_SQL
				if (fp)
					fprintf(fp, "fd.flid = %d, fd.clid = %d, fd.clidDest = %d%s\n",
						fd.flid, fd.clid, fd.clidDest,
						fFound ? "" : " (NO LABEL - NOT STORED)");
#endif
				if (fFound)
					vfd.Push(fd);
			}
#ifdef LOG_FILTER_SQL
			else if (fp)
			{
				fprintf(fp,
					"fd.flid = %d, fd.clid = %d, fd.clidDest = %d (NOT STORED - NO LABEL)\n",
					fd.flid, fd.clid, fd.clidDest);
			}
#endif
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		stuQuery.Format(L"select Id,Class,DstCls from Field$ "
			L"where Class in (%d,%d)",
			kclidCmPerson, kclidCmPossibility);
#ifdef LOG_FILTER_SQL
		if (fp)
		{
			stab.Format("%S", stuQuery.Chars());
			fprintf(fp, "\nSQL QUERY =\n%s\n\n", stab.Chars());
		}
#endif
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&fd.flid), isizeof(fd.flid),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&fd.clid), isizeof(fd.clid),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&fd.clidDest),
				isizeof(fd.clidDest), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				fd.clidDest = 0;
			// Store this field only if we have a proper label for it.
			if (hmflidhmclidld.Retrieve(fd.flid, &hmclidld))
			{
				bool fFound = hmclidld.Retrieve(fd.clid, &ldT);
#ifdef LOG_FILTER_SQL
				if (fp)
					fprintf(fp, "fd.flid = %d, fd.clid = %d, fd.clidDest = %d%s\n",
						fd.flid, fd.clid, fd.clidDest,
						fFound ? "" : " (NO LABEL - NOT STORED)");
#endif
				if (fFound)
					vfdPerson.Push(fd);
			}
#ifdef LOG_FILTER_SQL
			else if (fp)
			{
				fprintf(fp,
					"fd.flid = %d, fd.clid = %d, fd.clidDest = %d (NOT STORED - NO LABEL)\n",
					fd.flid, fd.clid, fd.clidDest);
			}
#endif
			CheckHr(qodc->NextRow(&fMoreRows));
		}
	}
	catch (...)	// Was empty.
	{
		throw;	// For now we have nothing to add, so pass it on up.
	}
#ifdef LOG_FILTER_SQL
	if (fp)
		fclose(fp);
#endif

	// Build the submenu nodes for CmPerson fields.
	int ifd;
	LabelData ld;
	for (ifd = 0; ifd < vfdPerson.Size(); ++ifd)
	{
		int flid = vfdPerson[ifd].flid;
		// Ignore these three fields: they're generally worthless for filtering.
		if (flid == kflidCmPossibility_DateCreated || flid == kflidCmPossibility_DateModified ||
			flid == kflidCmPossibility_HelpId)
		{
			continue;
		}
		hmflidhmclidld.Retrieve(flid, &hmclidld);
		int clid = kclidCmPerson;
		if (!hmclidld.Retrieve(clid, &ld))
		{
			clid = kclidCmPossibility;
			if (!hmclidld.Retrieve(clid, &ld))
				continue;
		}
		qfmnLeaf.Create();
		qfmnLeaf->m_stuText = ld.stuLabel;
		qfmnLeaf->m_fmnt = kfmntLeaf;
		qfmnLeaf->m_flid = flid;
		switch (flid)
		{
		case kflidCmPossibility_Confidence:
			qfmnLeaf->m_proptype = kfptPossList;
			qfmnLeaf->m_hvo = vhvoPssl[CleLpInfo::kpidPsslCon];
			break;
		case kflidCmPossibility_Discussion:
			qfmnLeaf->m_proptype = kfptStText;
			break;
		case kflidCmPerson_Education:
			qfmnLeaf->m_proptype = kfptPossList;
			qfmnLeaf->m_hvo = vhvoPssl[CleLpInfo::kpidPsslEdu];
			break;
		case kflidCmPerson_Gender:
			qfmnLeaf->m_proptype = kfptEnumListReq;
			qfmnLeaf->m_stid = kstidEnumGender;
			break;
		case kflidCmPerson_IsResearcher:
			qfmnLeaf->m_proptype = kfptBoolean;
			qfmnLeaf->m_stid = kstidEnumNoYes;
			break;
		case kflidCmPerson_PlaceOfBirth:
			qfmnLeaf->m_proptype = kfptPossList;
			qfmnLeaf->m_hvo = vhvoPssl[CleLpInfo::kpidPsslLoc];
			break;
		case kflidCmPerson_PlacesOfResidence:
			qfmnLeaf->m_proptype = kfptPossList;
			qfmnLeaf->m_hvo = vhvoPssl[CleLpInfo::kpidPsslLoc];
			break;
		case kflidCmPerson_Positions:
			qfmnLeaf->m_proptype = kfptPossList;
			qfmnLeaf->m_hvo = vhvoPssl[CleLpInfo::kpidPsslPsn];
			break;
		case kflidCmPossibility_Restrictions:
			qfmnLeaf->m_proptype = kfptPossList;
			qfmnLeaf->m_hvo = vhvoPssl[CleLpInfo::kpidPsslRes];
			break;
		case kflidCmPossibility_Status:
			qfmnLeaf->m_proptype = kfptPossList;
			qfmnLeaf->m_hvo = vhvoPssl[CleLpInfo::kpidPsslAna];
			break;
		default:
			{
				int nType = kcptNil;
				qmdc->GetFieldType(flid, &nType);
				if (nType == kcptBoolean)
				{
					qfmnLeaf->m_proptype = kcptBoolean;
					qfmnLeaf->m_stid = kstidEnumBool;
				}
			}
			break;
		}
		FilterMenuNode::AddSortedMenuNode(vfmnPerson, qfmnLeaf);
	}

	qfmnLeaf.Create();
	qfmnLeaf->m_stuText.Load(kstidFltrPersonMenuLabel);
	qfmnLeaf->m_fmnt = kfmntLeaf;
	qfmnLeaf->m_flid  = 0;
	qfmnLeaf->m_proptype = kfptPossList;
	qfmnLeaf->m_hvo = vhvoPssl[CleLpInfo::kpidPsslPeo];
	vfmnPerson.Insert(0, qfmnLeaf);
	qfmnLeaf.Create();					// Separator line.
	qfmnLeaf->m_stuText.Clear();
	qfmnLeaf->m_fmnt = kfmntLeaf;
	qfmnLeaf->m_flid  = 0;
	qfmnLeaf->m_proptype = kcptNil;
	vfmnPerson.Insert(1, qfmnLeaf);

	// Create the top level menu.
	int ifmn;
	MultiMap<int,int> mmclidifmn;
	FilterMenuNodePtr qfmn;
	qfmn.Create();
	qfmn->m_fmnt = kfmntClass;
	qfmn->m_clid = clidPss;
	ifmn = m_vfmn.Size();
	mmclidifmn.Insert(clidPss, ifmn);
	Vector<int> vclidBase;
	if (hmclidvclidBase.Retrieve(clidPss, &vclidBase))
	{
		for (int i = 0; i < vclidBase.Size(); ++i)
			mmclidifmn.Insert(vclidBase[i], ifmn);
	}
	StrUni stuFmt(kstidPossibilityFmt);
	StrUni stu;
	stu = GetPossListInfoPtr()->GetName();
	qfmn->m_stuText.Format(stuFmt.Chars(), stu.Chars());
	m_vfmn.Push(qfmn);

	// Add the submenu elements.
	for (ifd = 0; ifd < vfd.Size(); ++ifd)
	{
		int flid = vfd[ifd].flid;
		int clid = vfd[ifd].clid;
		int clidDest = vfd[ifd].clidDest;
		MultiMap<int,int>::iterator itmm;
		MultiMap<int,int>::iterator itmmMin;
		MultiMap<int,int>::iterator itmmLim;
		if (!mmclidifmn.Retrieve(clid, &itmmMin, &itmmLim))
		{
			Assert(false);
			continue;
		}
		if (!hmflidhmclidld.Retrieve(flid, &hmclidld))
		{
			Assert(false);
			continue;
		}
		bool fFound = hmclidld.Retrieve(clidPss, &ld);
		if (!fFound)
			fFound = hmclidld.Retrieve(clid, &ld);
		if (!fFound)
			continue;
		if (clidDest == kclidCmPerson && flid != kflidCmPossibility_Researchers)
		{
			// Attach the proper submenu for CmPerson nodes (other than Researchers).
			if (vfmnPerson.Size())
			{
				qfmnPopup.Create();
				qfmnPopup->m_stuText = ld.stuLabel;
				qfmnPopup->m_fmnt = kfmntField;
				qfmnPopup->m_flid = flid;
				qfmnPopup->m_hvo = PossListHvoForFlid(pdbi, flid);
				_CopyMenuNodeVector(qfmnPopup->m_vfmnSubItems, vfmnPerson);
				for (itmm = itmmMin; itmm != itmmLim; ++itmm)
				{
					ifmn = itmm->GetValue();
					m_vfmn[ifmn]->AddSortedSubItem(qfmnPopup);
				}
			}
		}
		else
		{
			qfmnLeaf.Create();
			qfmnLeaf->m_stuText = ld.stuLabel;
			qfmnLeaf->m_fmnt = kfmntLeaf;
			qfmnLeaf->m_flid = flid;
			if (clidDest == kclidStText)
			{
				// Mark structured text node.
				qfmnLeaf->m_proptype = kfptStText;
			}
			else if (clidDest == kclidCmPossibility || clidDest == kclidCmLocation ||
				clidDest == kclidCmPerson)
			{
				// Mark possibility list nodes.
				qfmnLeaf->m_proptype = kfptPossList;
				qfmnLeaf->m_hvo = PossListHvoForFlid(pdbi, flid);
				Assert(qfmnLeaf->m_hvo);
#ifdef DEBUG
				switch (flid)
				{
				case kflidCmPossibility_Restrictions:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[CleLpInfo::kpidPsslRes]);
					break;
				case kflidCmPossibility_Confidence:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[CleLpInfo::kpidPsslCon]);
					break;
				case kflidCmPossibility_Status:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[CleLpInfo::kpidPsslAna]);
					break;
				case kflidCmPossibility_Researchers:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[CleLpInfo::kpidPsslPeo]);
					break;
				case kflidCmPerson_Education:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[CleLpInfo::kpidPsslEdu]);
					break;
				case kflidCmPerson_PlaceOfBirth:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[CleLpInfo::kpidPsslLoc]);
					break;
				case kflidCmPerson_PlacesOfResidence:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[CleLpInfo::kpidPsslLoc]);
					break;
				case kflidCmPerson_Positions:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[CleLpInfo::kpidPsslPsn]);
					break;
				}
#endif
			}
			else
			{
				switch (flid)
				{
				case kflidCmPerson_Gender:
					qfmnLeaf->m_proptype = kfptEnumListReq;
					qfmnLeaf->m_stid = kstidEnumGender;
					break;
				case kflidCmPerson_IsResearcher:
					qfmnLeaf->m_proptype = kfptBoolean;
					qfmnLeaf->m_stid = kstidEnumNoYes;
					break;
				default:
					{
						int nType = kcptNil;
						qmdc->GetFieldType(flid, &nType);
						if (nType == kcptBoolean)
						{
							qfmnLeaf->m_proptype = kcptBoolean;
							qfmnLeaf->m_stid = kstidEnumBool;
						}
					}
					break;
				}
				// Nothing more to set for other types of nodes.
			}
			// Store this menu node.
			for (itmm = itmmMin; itmm != itmmLim; ++itmm)
			{
				ifmn = itmm->GetValue();
				m_vfmn[ifmn]->AddSortedSubItem(qfmnLeaf);
			}
		}
	}

	// Go through all the menu items recursively and assign the type for each field.
	int cfmn = m_vfmn.Size();
	for (ifmn = 0; ifmn < cfmn; ifmn++)
		_AssignFieldTypes(qmdc, m_vfmn[ifmn]);

	return &m_vfmn;
}

/*----------------------------------------------------------------------------------------------
	Returns a pointer to the vector of sort menu nodes.  If the size of the vector (m_vsmn)
	is 0 then the menu nodes are created being pushed into the vector.  Then go through
	all the menu items recursively and assign the field type for each field.

	@param plpi ptr to Language Project Info object.

	@return reference to the vector of the sort menu nodes.
----------------------------------------------------------------------------------------------*/
SortMenuNodeVec * CleMainWnd::GetSortMenuNodes(AfLpInfo * plpi)
{
	// TODO SteveMc(RandyR): Add support for PartOfSpeech.
	AssertPtr(plpi);

	if (m_vsmn.Size() != 0)
		return &m_vsmn;

	SortMenuNodePtr qsmnPopup;
	SortMenuNodePtr qsmnLeaf;
	SortMenuNodePtr qsmnCopy;

#ifdef DEBUG
	Vector<HVO> & vhvoPssl = plpi->GetPsslIds();
#endif

	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	IFwMetaDataCachePtr qmdc;
	pdbi->GetFwMetaDataCache(&qmdc);
	IOleDbEncapPtr qode;
	pdbi->GetDbAccess(&qode);

	int clidPss = GetRecordClid();
	Vector<FieldData> vfd;
	HashMap<int, LabelData> hmclidld;
	HashMap<int, HashMap<int, LabelData> > hmflidhmclidld;
	HashMap<int, Vector<int> > hmclidvclidBase;
	HashMap<int, FieldData> hmflidfd;
	int wsName = 0;			// used for default sort method.

#ifdef LOG_FILTER_SQL
	FILE * fp = fopen("c:/FW/DebugOutput.txt", "a");
	StrAnsiBufBig stab;
	if (fp)
	{
		fprintf(fp, "\n\
===============================================================================\n");
		time_t nTime;
		time(&nTime);
		fprintf(fp, "DEBUG CleMainWnd::GetSortMenuNodes(AfLpInfo * plpi) at %s",
			ctime(&nTime));
	}
#endif
	try
	{
		IOleDbCommandPtr qodc;
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;
		StrUni stuQuery;
		StrUni stuT;

		int ws = pdbi->UserWs();

		Vector<int> vclid;		// Class ids of possibilities and their parent classes.
		Set<int> setclid;		// Easiest way to keep track of unique class ids.
		vclid.Push(clidPss);
		setclid.Insert(clidPss);

		// Now add the base classes (if any) of the element classes.
		int clid;
		int iclid;
		for (iclid = 0; iclid < vclid.Size(); ++iclid)
		{
			unsigned long cid;
			CheckHr(qmdc->GetBaseClsId(vclid[iclid], &cid));
			if (cid)
			{
#ifdef LOG_FILTER_SQL
				if (fp)
					fprintf(fp, "    Base Class of %d = %d\n", vclid[iclid], cid);
#endif
				Vector<int> vclidBase;
				if (!hmclidvclidBase.Retrieve(vclid[iclid], &vclidBase))
					vclidBase.Clear();
				vclidBase.Push(cid);
				hmclidvclidBase.Insert(vclid[iclid], vclidBase, true);
				clid = cid;
				if (!setclid.IsMember(clid))
				{
					setclid.Insert(clid);
					vclid.Push(clid);
				}
			}
		}
		// Add any other CmPossibility based classes, since they are used as fields of each
		// other.  setclid.IsMember() will tell us which actually belong to the current type
		// of possibility item.
		static int rgclidPoss[] =
		{
			kclidCmPossibility, kclidCmLocation, kclidCmPerson, kclidCmAnthroItem,
			kclidCmCustomItem
		};
		const int kcclidPoss = isizeof(rgclidPoss) / isizeof(int);
		for (iclid = 0; iclid < kcclidPoss; ++iclid)
		{
			if (!setclid.IsMember(rgclidPoss[iclid]))
				vclid.Push(rgclidPoss[iclid]);
		}
		// Convert the list of classes to a string for use in following queries.
		StrUni stuClasses;
		for (iclid = 0; iclid < vclid.Size(); ++iclid)
		{
			if (iclid)
				stuClasses.FormatAppend(L",%d", vclid[iclid]);
			else
				stuClasses.FormatAppend(L"%d", vclid[iclid]);
		}
		// REVIEW (SteveMiller): Using the IN clause is generally one of the
		// slowest way to do something in SQL. OR clauses are worse. Don't
		// know if it can be helped here, but 'twould be nice.

		// Load the relevant field names from the user views in the database.
		stuQuery.Format(L"SELECT DISTINCT"
			L"  uvf.Flid, uvr.Clsid, mt.Txt, uvf.WritingSystem, uvf.WsSelector%n"
			L" FROM UserViewField uvf%n"
			L" JOIN UserViewField_Label mt ON uvf.Id = mt.Obj%n"
			L" JOIN CmObject cmo ON uvf.Id = cmo.Id%n"
			L" JOIN UserViewRec uvr ON cmo.Owner$ = uvr.Id%n"
			L" JOIN Field$ fld on fld.id = uvf.Flid%n"
			L"WHERE uvf.Flid IN (SELECT Id FROM Field$ WHERE Class IN (%s))%n"
			L"       AND uvr.Clsid IN (%s)%n"
			L"       AND (mt.Ws = %d)%n"
			// We don't try to sort by Structured Text fields or "Big" String/Unicode fields.
			L"    AND (fld.DstCls IS NULL OR fld.DstCls <> %d)%n"
			L"    AND (fld.Type NOT IN (%d,%d,%d,%d))%n"
			L"ORDER BY uvf.Flid",
			stuClasses.Chars(), stuClasses.Chars(), ws, kclidStText,
			kcptBigString, kcptMultiBigString, kcptBigUnicode, kcptMultiBigUnicode);
#ifdef LOG_FILTER_SQL
		if (fp)
		{
			stab.Format("%S", stuQuery.Chars());
			fprintf(fp, "SQL QUERY =\n%s\n\n", stab.Chars());
		}
#endif
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		const int kcchBuffer = MAX_PATH;
		OLECHAR rgchName[kcchBuffer];
		LabelData ld;
		LabelData ldT;
		ComBool fNullWs;
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&ld.flid), isizeof(ld.flid),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&ld.clid), isizeof(ld.clid),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(3, reinterpret_cast <BYTE *>(rgchName),
				isizeof(rgchName), &cbSpaceTaken, &fIsNull, 2));
			CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&ld.wsMagic),
				isizeof(ld.wsMagic), &cbSpaceTaken, &fNullWs, 0));
			if (fNullWs)
			{
				CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(&ld.wsMagic),
					isizeof(ld.wsMagic), &cbSpaceTaken, &fNullWs, 0));
			}
			if (!fIsNull && !fNullWs)
			{
				// Save the RnGenericRec_Title's writing system for default sort.
				if (ld.flid == kflidCmPossibility_Name && ld.clid == kclidCmPossibility)
					wsName = ld.wsMagic;
#ifdef LOG_FILTER_SQL
				if (fp)
				{
					stab.Format("ld.flid = %d, ld.clid = %d, ld.stuLabel = \"%S\"",
						ld.flid, ld.clid, rgchName);
					fprintf(fp, "%s\n", stab.Chars());
				}
#endif
				ld.stuLabel.Assign(rgchName);
				hmclidld.Clear();
				if (hmflidhmclidld.Retrieve(ld.flid, &hmclidld) &&
					hmclidld.Retrieve(ld.clid, &ldT) &&
					(ld.stuLabel.Length() < ldT.stuLabel.Length()))
				{
					Assert(ld.flid == ldT.flid);
					Assert(ld.clid == ldT.clid);
#ifdef LOG_FILTER_SQL
					if (fp)
					{
						stab.Format(
							"        ldT.flid = %d, ldT.clid = %d, ldT.stuLabel = \"%S\"",
							ldT.flid, ldT.clid, ldT.stuLabel.Chars());
						fprintf(fp, "%s\n", stab.Chars());
					}
#endif
					ld.stuLabel = ldT.stuLabel;
					ld.wsMagic = ldT.wsMagic;
				}
				hmclidld.Insert(ld.clid, ld, true);
				hmflidhmclidld.Insert(ld.flid, hmclidld, true);
			}
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		// Get the basic field information of interest from the database.
		stuQuery.Format(L"SELECT DISTINCT f.Id,f.Type,f.Class,f.DstCls%n"
			L"FROM Field$ f%n"
			L"LEFT OUTER JOIN UserViewField u ON u.Flid = f.Id%n"
			L"WHERE f.Class IN (%s)%n"
			// We don't try to sort by Structured Text fields or "Big" String/Unicode fields.
			L"    AND (f.DstCls IS NULL OR f.DstCls <> %d)%n"
			L"    AND (f.Type NOT IN (%d,%d,%d,%d))",
			stuClasses.Chars(), kclidStText,
			kcptBigString, kcptMultiBigString, kcptBigUnicode, kcptMultiBigUnicode);
#ifdef LOG_FILTER_SQL
		if (fp)
		{
			stab.Format("%S", stuQuery.Chars());
			fprintf(fp, "\nSQL QUERY =\n%s\n\n", stab.Chars());
		}
#endif
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		FieldData fd;
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&fd.flid), isizeof(fd.flid),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&fd.type), isizeof(fd.type),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&fd.clid), isizeof(fd.clid),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&fd.clidDest),
				isizeof(fd.clidDest), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				fd.clidDest = 0;

			// Store this field only if we have a proper label for it.
			if (hmflidhmclidld.Retrieve(fd.flid, &hmclidld))
			{
				bool fFound = hmclidld.Retrieve(clidPss, &ldT);
				if (!fFound)
					fFound = hmclidld.Retrieve(fd.clid, &ldT);
#ifdef LOG_FILTER_SQL
				if (fp)
					fprintf(fp, "fd.flid = %d, fd.clid = %d, fd.clidDest = %d%s\n",
						fd.flid, fd.clid, fd.clidDest,
						fFound ? "" : " (NO LABEL - NOT STORED)");
#endif
				if (fFound)
				{
					hmflidfd.Insert(fd.flid, fd, true);
					if (setclid.IsMember(fd.clid))
						vfd.Push(fd);
				}
			}
#ifdef LOG_FILTER_SQL
			else if (fp)
			{
				fprintf(fp,
					"fd.flid = %d, fd.clid = %d, fd.clidDest = %d (NOT STORED - NO LABEL)\n",
					fd.flid, fd.clid, fd.clidDest);
			}
#endif
			CheckHr(qodc->NextRow(&fMoreRows));
		}
	}
	catch (...)	// Was empty.
	{
		throw;	// For now we have nothing to add, so pass it on up.
	}
#ifdef LOG_FILTER_SQL
	if (fp)
		fclose(fp);
#endif

	int ifd;
	LabelData ld;

	// Build the submenu nodes for CmPossibility / CmPerson / CmLocation / ... fields.
	StrUni stuAbbrDef(kstidSortOnAbbr);
	StrUni stuNameDef(kstidSortOnName);
	SortMenuNodeVec vsmnPoss;
	qsmnLeaf.Create();
	qsmnLeaf->m_smnt = ksmntLeaf;
	qsmnLeaf->m_flid = kflidCmPossibility_Abbreviation;
	qsmnLeaf->m_stuText = stuAbbrDef;		// Default value.
	if (hmflidhmclidld.Retrieve(qsmnLeaf->m_flid, &hmclidld))
	{
		int clidPoss = kclidCmPossibility;
		if (hmclidld.Retrieve(clidPoss, &ld))
		{
			qsmnLeaf->m_stuText = ld.stuLabel;
			stuAbbrDef = ld.stuLabel;
		}
	}
	SortMenuNode::AddSortedMenuNode(vsmnPoss, qsmnLeaf);
	qsmnLeaf.Create();
	qsmnLeaf->m_smnt = ksmntLeaf;
	qsmnLeaf->m_flid = kflidCmPossibility_Name;
	qsmnLeaf->m_stuText = stuNameDef;
	if (hmflidhmclidld.Retrieve(qsmnLeaf->m_flid, &hmclidld))
	{
		int clidPoss = kclidCmPossibility;
		if (hmclidld.Retrieve(clidPoss, &ld))
		{
			qsmnLeaf->m_stuText = ld.stuLabel;
			stuNameDef = ld.stuLabel;
		}
	}
	SortMenuNode::AddSortedMenuNode(vsmnPoss, qsmnLeaf);
	SortMenuNodeVec vsmnLocation;
	qsmnLeaf.Create();
	qsmnLeaf->m_smnt = ksmntLeaf;
	qsmnLeaf->m_flid = kflidCmPossibility_Abbreviation;
	qsmnLeaf->m_stuText = stuAbbrDef;		// Default value.
	if (hmflidhmclidld.Retrieve(qsmnLeaf->m_flid, &hmclidld))
	{
		int clidPoss = kclidCmLocation;
		if (hmclidld.Retrieve(clidPoss, &ld))
			qsmnLeaf->m_stuText = ld.stuLabel;
	}
	SortMenuNode::AddSortedMenuNode(vsmnLocation, qsmnLeaf);
	qsmnLeaf.Create();
	qsmnLeaf->m_smnt = ksmntLeaf;
	qsmnLeaf->m_flid = kflidCmPossibility_Name;
	qsmnLeaf->m_stuText = stuNameDef;
	if (hmflidhmclidld.Retrieve(qsmnLeaf->m_flid, &hmclidld))
	{
		int clidPoss = kclidCmLocation;
		if (hmclidld.Retrieve(clidPoss, &ld))
			qsmnLeaf->m_stuText = ld.stuLabel;
	}
	SortMenuNode::AddSortedMenuNode(vsmnLocation, qsmnLeaf);
	SortMenuNodeVec vsmnPerson;
	qsmnLeaf.Create();
	qsmnLeaf->m_smnt = ksmntLeaf;
	qsmnLeaf->m_flid = kflidCmPossibility_Abbreviation;
	qsmnLeaf->m_stuText = stuAbbrDef;		// Default value.
	if (hmflidhmclidld.Retrieve(qsmnLeaf->m_flid, &hmclidld))
	{
		int clidPoss = kclidCmPerson;
		if (hmclidld.Retrieve(clidPoss, &ld))
			qsmnLeaf->m_stuText = ld.stuLabel;
	}
	SortMenuNode::AddSortedMenuNode(vsmnPerson, qsmnLeaf);
	qsmnLeaf.Create();
	qsmnLeaf->m_smnt = ksmntLeaf;
	qsmnLeaf->m_flid = kflidCmPossibility_Name;
	qsmnLeaf->m_stuText = stuNameDef;
	if (hmflidhmclidld.Retrieve(qsmnLeaf->m_flid, &hmclidld))
	{
		int clidPoss = kclidCmPerson;
		if (hmclidld.Retrieve(clidPoss, &ld))
			qsmnLeaf->m_stuText = ld.stuLabel;
	}
	SortMenuNode::AddSortedMenuNode(vsmnPerson, qsmnLeaf);
	SortMenuNodeVec vsmnAnthroItem;
	qsmnLeaf.Create();
	qsmnLeaf->m_smnt = ksmntLeaf;
	qsmnLeaf->m_flid = kflidCmPossibility_Abbreviation;
	qsmnLeaf->m_stuText = stuAbbrDef;		// Default value.
	if (hmflidhmclidld.Retrieve(qsmnLeaf->m_flid, &hmclidld))
	{
		int clidPoss = kclidCmAnthroItem;
		if (hmclidld.Retrieve(clidPoss, &ld))
			qsmnLeaf->m_stuText = ld.stuLabel;
	}
	SortMenuNode::AddSortedMenuNode(vsmnAnthroItem, qsmnLeaf);
	qsmnLeaf.Create();
	qsmnLeaf->m_smnt = ksmntLeaf;
	qsmnLeaf->m_flid = kflidCmPossibility_Name;
	qsmnLeaf->m_stuText = stuNameDef;
	if (hmflidhmclidld.Retrieve(qsmnLeaf->m_flid, &hmclidld))
	{
		int clidPoss = kclidCmAnthroItem;
		if (hmclidld.Retrieve(clidPoss, &ld))
			qsmnLeaf->m_stuText = ld.stuLabel;
	}
	SortMenuNode::AddSortedMenuNode(vsmnAnthroItem, qsmnLeaf);
	SortMenuNodeVec vsmnCustomItem;
	qsmnLeaf.Create();
	qsmnLeaf->m_smnt = ksmntLeaf;
	qsmnLeaf->m_flid = kflidCmPossibility_Abbreviation;
	qsmnLeaf->m_stuText = stuAbbrDef;		// Default value.
	if (hmflidhmclidld.Retrieve(qsmnLeaf->m_flid, &hmclidld))
	{
		int clidPoss = kclidCmCustomItem;
		if (hmclidld.Retrieve(clidPoss, &ld))
			qsmnLeaf->m_stuText = ld.stuLabel;
	}
	SortMenuNode::AddSortedMenuNode(vsmnCustomItem, qsmnLeaf);
	qsmnLeaf.Create();
	qsmnLeaf->m_smnt = ksmntLeaf;
	qsmnLeaf->m_flid = kflidCmPossibility_Name;
	qsmnLeaf->m_stuText = stuNameDef;
	if (hmflidhmclidld.Retrieve(qsmnLeaf->m_flid, &hmclidld))
	{
		int clidPoss = kclidCmCustomItem;
		if (hmclidld.Retrieve(clidPoss, &ld))
			qsmnLeaf->m_stuText = ld.stuLabel;
	}
	SortMenuNode::AddSortedMenuNode(vsmnCustomItem, qsmnLeaf);

	// Create the top level menu.
	int ismn;
	MultiMap<int,int> mmclidismn;
	SortMenuNodePtr qsmn;
	qsmn.Create();
	qsmn->m_smnt = ksmntClass;
	qsmn->m_clid = clidPss;
	ismn = m_vsmn.Size();
	mmclidismn.Insert(clidPss, ismn);
	Vector<int> vclidBase;
	if (hmclidvclidBase.Retrieve(clidPss, &vclidBase))
	{
		for (int i = 0; i < vclidBase.Size(); ++i)
			mmclidismn.Insert(vclidBase[i], ismn);
	}
	StrUni stuFmt(kstidPossibilityFmt);
	StrUni stu;
	stu = GetPossListInfoPtr()->GetName();
	qsmn->m_stuText.Format(stuFmt.Chars(), stu.Chars());
	m_vsmn.Push(qsmn);

	// Add the submenu elements.
	for (ifd = 0; ifd < vfd.Size(); ++ifd)
	{
		int flid = vfd[ifd].flid;
		// Ignore this field: it's generally worthless for sorting.
		if (flid == kflidCmPossibility_HelpId)
			continue;

		int clid = vfd[ifd].clid;
		int clidDest = vfd[ifd].clidDest;
		MultiMap<int,int>::iterator itmm;
		MultiMap<int,int>::iterator itmmMin;
		MultiMap<int,int>::iterator itmmLim;
		if (!mmclidismn.Retrieve(clid, &itmmMin, &itmmLim))
		{
			Assert(false);
			continue;
		}
		if (!hmflidhmclidld.Retrieve(flid, &hmclidld))
		{
			Assert(false);
			continue;
		}
		bool fFound = hmclidld.Retrieve(clidPss, &ld);
		if (!fFound)
			fFound = hmclidld.Retrieve(clid, &ld);
		if (!fFound)
			continue;

		if (clidDest == kclidCmPossibility ||
			clidDest == kclidCmLocation ||
			clidDest == kclidCmAnthroItem ||
			clidDest == kclidCmPerson ||
			clidDest == kclidCmCustomItem)
		{
			qsmnPopup.Create();
			qsmnPopup->m_stuText = ld.stuLabel;
			qsmnPopup->m_smnt = ksmntField;
			qsmnPopup->m_flid = flid;
			// Mark possibility list nodes.
			qsmnPopup->m_proptype = kfptPossList;
			qsmnPopup->m_hvo = PossListHvoForFlid(pdbi, flid);
			Assert(qsmnPopup->m_hvo);
#ifdef DEBUG
			switch (flid)
			{
			case kflidCmPossibility_Restrictions:
				Assert(qsmnPopup->m_hvo == vhvoPssl[CleLpInfo::kpidPsslRes]);
				break;
			case kflidCmPossibility_Confidence:
				Assert(qsmnPopup->m_hvo == vhvoPssl[CleLpInfo::kpidPsslCon]);
				break;
			case kflidCmPossibility_Status:
				Assert(qsmnPopup->m_hvo == vhvoPssl[CleLpInfo::kpidPsslAna]);
				break;
			case kflidCmPossibility_Researchers:
				Assert(qsmnPopup->m_hvo == vhvoPssl[CleLpInfo::kpidPsslPeo]);
				break;
			case kflidCmPerson_Education:
				Assert(qsmnPopup->m_hvo == vhvoPssl[CleLpInfo::kpidPsslEdu]);
				break;
			case kflidCmPerson_PlaceOfBirth:
				Assert(qsmnPopup->m_hvo == vhvoPssl[CleLpInfo::kpidPsslLoc]);
				break;
			case kflidCmPerson_PlacesOfResidence:
				Assert(qsmnPopup->m_hvo == vhvoPssl[CleLpInfo::kpidPsslLoc]);
				break;
			case kflidCmPerson_Positions:
				Assert(qsmnPopup->m_hvo == vhvoPssl[CleLpInfo::kpidPsslPsn]);
				break;
			}
#endif
			switch (clidDest)
			{
			case kclidCmPossibility:
				_CopyMenuNodeVector(qsmnPopup->m_vsmnSubItems, vsmnPoss);
				break;
			case kclidCmLocation:
				_CopyMenuNodeVector(qsmnPopup->m_vsmnSubItems, vsmnLocation);
				break;
			case kclidCmAnthroItem:
				_CopyMenuNodeVector(qsmnPopup->m_vsmnSubItems, vsmnAnthroItem);
				break;
			case kclidCmPerson:
				_CopyMenuNodeVector(qsmnPopup->m_vsmnSubItems, vsmnPerson);
				break;
			case kclidCmCustomItem:
				_CopyMenuNodeVector(qsmnPopup->m_vsmnSubItems, vsmnCustomItem);
				break;
			default:
				Assert(false);		// SHOULD NEVER HAPPEN!
				_CopyMenuNodeVector(qsmnPopup->m_vsmnSubItems, vsmnPoss);
				break;
			}
			// Store this menu node.
			for (itmm = itmmMin; itmm != itmmLim; ++itmm)
			{
				ismn = itmm->GetValue();
				m_vsmn[ismn]->AddSortedSubItem(qsmnPopup);
			}
		}
		else
		{
			qsmnLeaf.Create();
			qsmnLeaf->m_stuText = ld.stuLabel;
			qsmnLeaf->m_smnt = ksmntLeaf;
			qsmnLeaf->m_flid = flid;
			switch (flid)
			{
			case kflidCmPerson_Gender:
				qsmnLeaf->m_proptype = kfptEnumListReq;
				qsmnLeaf->m_stid = kstidEnumGender;
				break;
			case kflidCmPerson_IsResearcher:
				qsmnLeaf->m_proptype = kfptBoolean;
				qsmnLeaf->m_stid = kstidEnumNoYes;
				break;
			default:
				{
					int nType = kcptNil;
					qmdc->GetFieldType(flid, &nType);
					if (nType == kcptBoolean)
					{
						qsmnLeaf->m_proptype = kcptBoolean;
						qsmnLeaf->m_stid = kstidEnumBool;
					}
				}
				break;
			}
			// Store this menu node.
			for (itmm = itmmMin; itmm != itmmLim; ++itmm)
			{
				ismn = itmm->GetValue();
				m_vsmn[ismn]->AddSortedSubItem(qsmnLeaf);
			}
		}
	}

	// Go through all the menu items recursively and assign the type for each field.
	int csmn = m_vsmn.Size();
	for (ismn = 0; ismn < csmn; ismn++)
		_AssignFieldTypes(qmdc, m_vsmn[ismn]);

	// Go through all the menu items recursively and add writing system/old writing
	// system/collation submenus as needed.
	SortMenuNodeVec vsmnLang;
	for (int ismn = 0; ismn < csmn; ismn++)
		_AddLanguageChoices(plpi, hmflidfd, vsmnLang, m_vsmn[ismn], 0);

	// Create the default sort method while we have the necessary information.
	if (!m_asiDefault.m_stuName.Length())
	{
		m_asiDefault.m_stuName.Load(kstidSortDefaultName);
		m_asiDefault.m_fIncludeSubfields = false;
		m_asiDefault.m_hvo = 0;

		m_asiDefault.m_stuPrimaryField.Format(L"%d,%d",
			clidPss, kflidCmPossibility_DateCreated);
		m_asiDefault.m_wsPrimary = 0;
		m_asiDefault.m_collPrimary = 0;
		m_asiDefault.m_fPrimaryReverse = false;

		m_asiDefault.m_stuSecondaryField.Format(L"%d,%d", clidPss, kflidCmPossibility_Name);
		// Find the writing system/collation information.
		switch (wsName)
		{
		case 0:
		case kwsAnals:
		case kwsAnal:
		case kwsAnalVerns:
			m_asiDefault.m_wsSecondary = plpi->AnalWs();
			break;
		case kwsVerns:
		case kwsVern:
		case kwsVernAnals:
			m_asiDefault.m_wsSecondary = plpi->VernWs();
			break;
		default:
			m_asiDefault.m_wsSecondary = wsName;
			break;
		}
		m_asiDefault.m_collSecondary = 0;		// Default collation.
		m_asiDefault.m_fSecondaryReverse = false;

		m_asiDefault.m_stuTertiaryField.Clear();
		m_asiDefault.m_wsTertiary = 0;
		m_asiDefault.m_collTertiary = 0;
		m_asiDefault.m_fTertiaryReverse = false;

		m_asiDefault.m_clidRec = GetRecordClid();
		m_asiDefault.m_fMultiOutput = false;
	}

	return &m_vsmn;
}


/*----------------------------------------------------------------------------------------------
	Load settings specific to this window from the registry. Load the viewbar, treebar,
	status bar, and tool bars settings.  Get the last data record that was showing. Load the
	window position.  Set the last overlay, filter, sort order, and the last view according to
	the registry.

	@param pszRoot The string that is used for this app as the root for all registry entries.
	@param fRecursive If true, then every child window will load their settings.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::LoadSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);

	SuperClass::LoadSettings(pszRoot, fRecursive);

	bool fPath = m_vhvoPath.Size();

	FwSettings * pfws = AfApp::GetSettings();
	DWORD dwT;

	// Read the viewbar settings. If the settings aren't there, use default values.
	DWORD dwViewbarFlags;
	if (!pfws->GetDword(pszRoot, _T("Viewbar Flags"), &dwViewbarFlags))
	{
		dwViewbarFlags = kmskShowLargeViewIcons
			| kmskShowLargeFilterIcons
			| kmskShowLargeSortIcons
			| kmskShowViewBar;
	}

	m_qvwbrs->ChangeIconSize(m_ivblt[kvbltView], dwViewbarFlags & kmskShowLargeViewIcons);
	m_qvwbrs->ChangeIconSize(m_ivblt[kvbltFilter], dwViewbarFlags & kmskShowLargeFilterIcons);
	m_qvwbrs->ChangeIconSize(m_ivblt[kvbltSort], dwViewbarFlags & kmskShowLargeSortIcons);

	if (!pfws->GetDword(pszRoot, _T("LastViewBarGroup"), &dwT))
		dwT = m_ivblt[kvbltView];
	m_qvwbrs->SetCurrentList(Min((uint)dwT, (uint)(m_ivbltMax)));

	m_qvwbrs->ShowViewBar(dwViewbarFlags & kmskShowViewBar);

	::ShowWindow(m_hwnd, SW_SHOW);
	OnIdle();
	::UpdateWindow(m_hwnd);

	Set<int> sisel;

	// TODO DarrellZ: Figure out which sort order, if any, should be selected.
	// Right now I'm selecting the Default Sort one.
	int isort = 0;
	sisel.Clear();
	sisel.Insert(isort);
	m_qvwbrs->SetSelection(m_ivblt[kvbltSort], sisel);

	// Filters are disabled if we don't get a valid item from the registry or if
	// we are opening the window on a specific item.
	int cflt = m_qlpi->GetDbInfo()->GetFilterCount();
	if (!pfws->GetDword(pszRoot, _T("LastFilter"), &dwT) || (uint)dwT >= (uint)cflt || fPath)
		dwT = 0; // No Filter.
	sisel.Clear();
	int ifltr = (int)dwT;
	sisel.Insert(ifltr);
	m_qvwbrs->SetSelection(m_ivblt[kvbltFilter], sisel);

	// Get the record to show. This must be done before the first view is created, but
	// after any sorting or filtering has been applied. The desired record is the one
	// specified as a startup parameter, or the last one saved in the registry.
	if (fPath)
	{
		dwT = 0; // Default to first if we can't find the top item.
		// In Notebook, DE views we generally display top level entries
		// which show nested subentries, so a path of IDs from top level to nested
		// level makes sense. It would also make sense here once we get a document
		// view, but we currently have all items in a flat list instead of
		// being nested, so if a user gives us a path from top level to nested
		// level, we really want the last item (assuming it is a CmPossibility)
		// so that our DE will show that item. So for now we'll pick out the last
		// item and ignore the flids.
		HVO hvo = m_vhvoPath[m_vhvoPath.Size() - 1];
		// Find the index for the specified hvo.
		for (int ihvo = m_vhcFilteredRecords.Size(); --ihvo >= 0; )
		{
			if (m_vhcFilteredRecords[ihvo].hvo == hvo)
			{
				dwT = ihvo;
				break;
			}
		}
	}
	else if (pfws->GetDword(pszRoot, _T("LastDataRecord"), &dwT))
	{
		if ((int)dwT < 0)
			dwT = 0;
		if (dwT >= (uint)m_vhcFilteredRecords.Size())
			dwT = max(-1, m_vhcFilteredRecords.Size() - 1);
	}

	if (m_vhcFilteredRecords.Size())
	{
		SetCurRecIndex((int)dwT);
	}
	else
	{
		SetCurRecIndex(-1);
	}

	// NOTE: Make sure this is done after the filter is selected, or else none of the entries
	// will be shown in document or browse view.
	// Select the view specified by startup or that was last open.
	if (fPath || m_nView)
	{
		UserViewSpecVec & vuvs = m_qlpi->GetDbInfo()->GetUserViewSpecs();
		if (m_nView == -1)
		{
			// For -1, find the first data entry view.
			for (int iuvs = 0; iuvs < vuvs.Size(); ++iuvs)
			{
				if (vuvs[iuvs]->m_vwt == kvwtDE)
				{
					m_nView = iuvs;
					break;
				}
			}
		}
		Assert((uint)m_nView < (uint)vuvs.Size());
		dwT = m_nView;
	}
	else if (!pfws->GetDword(pszRoot, _T("LastView"), &dwT))
		dwT = 0;
	AssertObj(m_qvwbrs);
	AssertObj(m_qvwbrs->GetViewBar());
	AssertObj(dynamic_cast<AfWnd *>(m_qvwbrs->GetViewBar()->GetList(m_ivblt[kvbltView])));
	int iview = Min((uint)dwT,
		(uint)(m_qvwbrs->GetViewBar()->GetList(m_ivblt[kvbltView])->GetSize() - 1));
	sisel.Clear();
	sisel.Insert(iview);
	m_qvwbrs->SetSelection(m_ivblt[kvbltView], sisel);
	m_qvwbrs->ShowViewBar(dwViewbarFlags & kmskShowViewBar);
	if (fPath)
	{
		// Clear out the startup info.
		m_vhvoPath.Clear();
		m_vflidPath.Clear();
		m_nView = 0;
	}
}


/*----------------------------------------------------------------------------------------------
	Load the default toolbar setup, including each toolbar's width and
	whether or not it should be on a separate line.
	Each band has the following format for the flag:
		LOWORD(vflag[iband])           = width of the band
		HIWORD(vflag[iband]) & 0x8000  = 1 if the next toolbar should be on a new line
		HIWORD(vflag[iband]) & ~0x8000 = toolbar index in m_vqtlbr
	Subclasses should override this in order to use different default settings.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::LoadDefaultToolbarFlags(Vector<DWORD> & vflag, DWORD & dwBarFlags)
{
	dwBarFlags = 0x7f; // Show all 7 toolbars.
	int cband = m_vqtlbr.Size();
	int rgitlbr[] = { 0, 1, 6, 2, 4, 3, 5 };
	bool rgfBreak[] = { true, true, false, true, true, false, false };
	int rgdxpBar[] = { 0x018b, 0x000ee, 0x0193, 0x0140, 0x0076, 0x0150, 0x00f4 };
	for (int iband = 0; iband < cband; iband++)
	{
		vflag[iband] = MAKELPARAM(rgdxpBar[iband], rgitlbr[iband] | (rgfBreak[iband] << 15));
	}
}


/*----------------------------------------------------------------------------------------------
	Return the path extension that allows suitable help to be found for how the application
	saves data. The DataNotebook one is provided as a default.
----------------------------------------------------------------------------------------------*/
achar * CleMainWnd::HowToSaveHelpString()
{
	return _T("HowTheTopicsListEditorSavesYou.htm");
}


/*----------------------------------------------------------------------------------------------
	Handle the Topics Lists Properties command.

	@param pcmd Pointer to the menu command.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CmdListsProps(Cmd * pcmd)
{
	AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
	Assert(pdapp);
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);

	if (!pdapp->AreAllWndsOkToChange(pdbi, false)) // Don't check required fields.
		return true;

	if (!pdapp->SaveAllWndsEdits(pdbi))
		return true;

	if (!pdapp->CloseAllWndsEdits())
		return true;

	int nResponse;
	int wsMagic = m_qlpi->GetPsslWsFromDb(m_hvoPssl);
	nResponse = pdapp->TopicsListProperties(m_qlpi, m_hvoPssl, wsMagic, m_hwnd);
	if (nResponse == kctidOk)
	{
		m_wsPssl = m_qlpi->GetPsslWsFromDb(m_hvoPssl);
		SyncInfo sync(ksyncFullRefresh, m_hvoPssl, 0);
		m_qlpi->StoreAndSync(sync);
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Get the drag text for this object. This will be used to follow the mouse during a drag
	and also will be pasted into a text if dropped into a text location. Assuming entries,
	we show the type of entry, title, and date.
	@param hvo The object we plan to drag.
	@param clid The class of the object we plan to drag.
	@param pptss Pointer in which to return the drag text.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::GetDragText(HVO hvo, int clid, ITsString ** pptss)
{
	Assert(hvo);
	Assert(clid);
	AssertPtr(pptss);
	*pptss = NULL;
	try
	{
		switch (clid)
		{
		default:
			{
				CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
				Assert(pcmw);

				PossListInfo * ppli = pcmw->GetPossListInfoPtr();
				AssertPtr(ppli);
				int iItem = ppli->GetIndexFromId (hvo);
				PossItemInfo * ppii = ppli->GetPssFromIndex(iItem);
				StrUni stuName;
				ppii->GetName(stuName);

				AssertPtr(m_qcvd);
				int flid;
				CheckHr(m_qcvd->get_ObjOwnFlid(hvo, &flid));
				StrUni stuType;
				stuType.Load(flid == kflidCmPossibility_SubPossibilities ? kstidListItem : kstidListItem);
				StrUni stuMsg;
				stuMsg.Format(L"%s - %s", stuType.Chars(), stuName.Chars());

				int ws = UserWs();
				ITsIncStrBldrPtr qtisb;
				qtisb.CreateInstance(CLSID_TsIncStrBldr);
				CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
				CheckHr(qtisb->AppendRgch(stuMsg.Chars(), stuMsg.Length()));
				CheckHr(qtisb->GetString(pptss));
				break;
			}
		}
	}
	catch (...)
	{
		Assert(false);
	}
}


/*----------------------------------------------------------------------------------------------
	Create the client window needed for the specified view.

	@param vwt View Type to be created(Data Entry, Browse, or Document)
	@param vqafcw Vector of pointers to put the new view in.
	@param wid Window ID to use for the new view.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::CreateClient(UserViewType vwt, Vector<AfClientWndPtr> & vqafcw, int wid)
{
	AfClientWndPtr qafcw;

	switch (vwt)
	{
	case kvwtDE:
		qafcw.Attach(NewObj AfClientRecDeWnd);
		qafcw->Create(_T(""), kimagDataEntry, wid);
		break;
#if 0 // ENHANCE RandB (JohnT): reinstate when we have browse view.
	case kvwtBrowse:
		qafcw.Attach(NewObj AfClientRecVwWnd);
		qafcw->Create(_T(""), kimagBrowse, wid);
		break;
#endif
	case kvwtDoc:
		qafcw.Attach(NewObj AfClientRecVwWnd);
		qafcw->Create(_T(""), kimagDocument, wid);
		break;
	default:
		Assert(false); // We should never get here.
		break;
	}
	vqafcw.Push(qafcw);
}


/*----------------------------------------------------------------------------------------------
	Load main data.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::LoadMainData()
{
	// Load the vector of objects we can move through.
	// TODO JohnT: make this list subject to filtering; make Doc view use this list,
	// not do a new query.
	m_ItemClsid = GetPossListInfoPtr()->GetItemClsid();
	m_qcvd->SetTags(kflidCmPossibilityList_Possibilities); // add sorting flid?
	GetSortMenuNodes(m_qlpi);		// This is needed for the status bar if sorting is on.

	// Insert the poss list into the viewbar.
	PossListInfo * ppli = GetPossListInfoPtr();
	int cpii = ppli->GetCount();
	Assert(m_vhcRecords.Size() == 0);
	for (int ipii = 0; ipii < cpii; ipii++)
	{
		PossItemInfo * ppii = ppli->GetPssFromIndex(ipii);
		AssertPtr(ppii);
		HvoClsid hc;
		hc.hvo = ppii->GetPssId();
		hc.clsid = m_ItemClsid;
		m_vhcRecords.Push(hc);
		m_qcvd->CacheIntProp(hc.hvo, kflidCmObject_Class, hc.clsid);
		// Review (KenZ/SharonC): do we need to save the owner of the objects as well?
		// If so, it's not quite as simple as the line below.
		//CacheObjProp(hc.hvo, kflidCmObject_Owner, hvoRoot);
	}
}


/*----------------------------------------------------------------------------------------------
	The hwnd has been attached.  Set the default caption text in case the database cannot be
	opened.  Load key ids, encodings, and styles from the database.  Set the caption of the
	main window.  Create the main frame window and set it as the current window. Also create
	the rebar, status bar, mdi child window, and viewbar.  Insert the poss list into the
	viewbar, and set the Action Handler.  Load the vector of objects we can move through.
	Insert the list of views, filters, sort orders into the viewbar.  Create the menu bar and
	toolbars.  Load PageSetup info and window settings.  Update the icons for the formatting
	toolbar drop-down buttons.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::PostAttach(void)
{
	m_wsPssl = m_qlpi->GetPsslWsFromDb(m_hvoPssl);
	PossListInfoPtr qpli;
	GetLpInfo()->LoadPossList(m_hvoPssl, m_wsPssl, &qpli);

	SuperClass::PostAttach();

	// Remove unwanted toolbar buttons.
	AfToolBar * ptlbr = GetToolBar(kridTBarIns);
	HWND hwnd = ptlbr->Hwnd();
	int cbtn = ::SendMessage(hwnd, TB_BUTTONCOUNT , 0, 0);
	PossListInfo * ppli = GetPossListInfoPtr();
	for (int ibtn = cbtn - 1; ibtn > -1; ibtn--)
	{
		TBBUTTON tbb;
		::SendMessage(hwnd, TB_GETBUTTON, ibtn, (LPARAM)&tbb);
		switch (tbb.idCommand)
		{
		case kcidInsListSubItem:
			if (ppli->GetDepth() == 1)
				::SendMessage(hwnd, TB_DELETEBUTTON, (WPARAM)ibtn, 0);
			break;
		case kcidInsListItemAft:
		case kcidInsListItemBef:
			if (ppli->GetIsSorted())
				::SendMessage(hwnd, TB_DELETEBUTTON, (WPARAM)ibtn, 0);
			break;
		case kcidInsListItem:
			if (!ppli->GetIsSorted())
				::SendMessage(hwnd, TB_DELETEBUTTON, (WPARAM)ibtn, 0);
			break;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Clears and then loads information into the view bar. This assumes AfLpInfo and AfDbInfo
	have been properly initialized.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::LoadViewBar()
{
	StrAppBuf strbTemp; // Holds temp string, e.g., strings used as tab captions.
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	ILgWritingSystemFactoryPtr qwsf;
	pdbi->GetLgWritingSystemFactory(&qwsf);
	PossListInfo * ppli = GetPossListInfoPtr();

	AfViewBar * pvwbr = m_qvwbrs->GetViewBar();
	// If we are reloading, first clear out any existing list bars.
	int clist = pvwbr->GetListCount();
	int listCur;
	// First, save the current selections for each AfListBar.
	Set<int> siselView;
	Set<int> siselFilter;
	Set<int> siselSort;
	HVO hvoSel = 0;
	if (clist)
	{
		// Save the current selection in the tree.
		FW_TVITEM tvi;
		tvi.mask = TVIF_HANDLE | TVIF_PARAM;
		tvi.hItem = TreeView_GetSelection(m_hwndTrBr);
		if (TreeView_GetItem(m_hwndTrBr, &tvi))
		{
			FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
			AssertPtr(pfti);
			hvoSel = pfti->lParam;
		}
		listCur = m_qvwbrs->GetCurrentList();
		m_qvwbrs->GetSelection(m_ivblt[kvbltView], siselView);
		m_qvwbrs->GetSelection(m_ivblt[kvbltFilter], siselFilter);
		m_qvwbrs->GetSelection(m_ivblt[kvbltSort], siselSort);
		pvwbr->Clear();
	}

	CleTreeBarPtr qtrbr;
	qtrbr.Create();
	qtrbr->Init(m_wsPssl, m_hvoPssl, m_qlpi);
	qtrbr->SetWritingSystemFactory(qwsf);
	strbTemp.Load(kstidList);
	m_qvwbrs->AddTree(strbTemp.Chars(), NULL, WS_CHILD |  WS_VISIBLE | WS_TABSTOP
		| TVS_HASLINES |TVS_HASBUTTONS | TVS_LINESATROOT | TVS_SHOWSELALWAYS, qtrbr);
	AssertPtr(pvwbr);
	m_qtrbr = dynamic_cast<CleTreeBar *>(pvwbr->GetList(m_ivblt[kvbltTree]));
	AssertPtr(m_qtrbr.Ptr());
	m_qtrbr->SetWritingSystemFactory(qwsf);	// probably not needed, but...
	m_hwndTrBr = m_qtrbr->Hwnd();

	// Initialize the drag and drop package:
	m_plddDragDrop.Init(ppli, true, m_hwndTrBr, m_hwnd);
	qtrbr->SetDragDropHandler(&m_plddDragDrop);

	int pnt = ppli->GetDisplayOption();
	m_qtrbr->SetPossNameType(pnt);

	// Insert the list of views into the viewbar.
	strbTemp.Load(kstidViews);
	CleListBarPtr qclb;
	qclb.Create();
	qclb->Init(m_ivblt[kvbltView]);
	m_qvwbrs->AddList(strbTemp.Chars(), m_rghiml[0], m_rghiml[1], false, qclb);
	int cv = m_qmdic->GetChildCount();
	for (int iv = 0; iv < cv; iv++)
	{
		AfClientWnd * pafcw = m_qmdic->GetChildFromIndex(iv);
		AssertPtr(pafcw);
		m_qvwbrs->AddListItem(m_ivblt[kvbltView], pafcw->GetViewName(), pafcw->GetImageIndex());
	}

	int clidRec = GetRecordClid();

	// Insert the list of filters into the viewbar.
	strbTemp.Load(kstidFilters);
	qclb.Create();
	qclb->Init(m_ivblt[kvbltFilter]);
	m_qvwbrs->AddList(strbTemp.Chars(), m_rghiml[0], m_rghiml[1], false, qclb);
	strbTemp.Load(kstidNoFilter);
	m_qvwbrs->AddListItem(m_ivblt[kvbltFilter], strbTemp.Chars(), kimagFilterNone);
	cv = pdbi->GetFilterCount();
	for (int iv = 0; iv < cv; iv++)
	{
		AppFilterInfo & afi = pdbi->GetFilterInfo(iv);
		if (afi.m_clidRec == clidRec)
		{
			strbTemp = afi.m_stuName.Chars();
			m_qvwbrs->AddListItem(m_ivblt[kvbltFilter], strbTemp.Chars(),
				afi.m_fSimple ? kimagFilterSimple : kimagFilterFull);
		}
	}

	// Insert the list of sort orders into the viewbar.
	strbTemp.Load(kstidSortMethods);
	qclb.Create();
	qclb->Init(m_ivblt[kvbltSort]);
	m_qvwbrs->AddList(strbTemp.Chars(), m_rghiml[0], m_rghiml[1], false, qclb);
	strbTemp.Load(kstidDefaultSort);
	m_qvwbrs->AddListItem(m_ivblt[kvbltSort], strbTemp.Chars(), kimagSort);
	cv = pdbi->GetSortCount();
	for (int iv = 0; iv < cv; iv++)
	{
		AppSortInfo & asi = pdbi->GetSortInfo(iv);
		if (asi.m_clidRec == clidRec)
		{
			strbTemp = asi.m_stuName.Chars();
			m_qvwbrs->AddListItem(m_ivblt[kvbltSort], strbTemp.Chars(), kimagSort);
		}
	}
	if (clist)
	{
		// Now restore the original AfListBar selections.
		m_qvwbrs->SetCurrentList(listCur);
		m_qvwbrs->SetSelection(m_ivblt[kvbltView], siselView);
		m_qvwbrs->SetSelection(m_ivblt[kvbltFilter], siselFilter);
		m_qvwbrs->SetSelection(m_ivblt[kvbltSort], siselSort);
		m_qvwbrs->ShowViewBar(GetViewbarSaveFlags() & kmskShowViewBar);
	}
	RefreshTreeView(hvoSel);
}


/*----------------------------------------------------------------------------------------------
	Called when window is gaining or losing activation.
	If we are being activated, show all the overlays, otherwise hide them all.

	@param fActivating true if gaining activation, false if losing activation.
	@param hwnd handle to the window.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::OnActivate(bool fActivating, HWND hwnd)
{
	SuperClass::OnActivate(fActivating, hwnd);
	m_plddDragDrop.KillDrag();
}


/*----------------------------------------------------------------------------------------------
	Return the selected treebar tree item.

	@return The selected treebar tree item.
----------------------------------------------------------------------------------------------*/
HTREEITEM CleMainWnd::GetCurTreeSel()
{
	Assert(m_hwndTrBr);
	HTREEITEM hitem;
	hitem = TreeView_GetSelection(m_hwndTrBr);
	TreeView_SelectItem(m_hwndTrBr, hitem);
	return hitem;
}

/*----------------------------------------------------------------------------------------------
	This is called when any menu item is expanded, and allows the app to modify the menu.

	@param hmenu Handle to the menu that is being expanded right now.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::FixMenu(HMENU hmenu)
{
	int cmni = ::GetMenuItemCount(hmenu);
	int imni;

	for (imni = cmni - 1; imni > -1; imni--)
	{
		UINT nmId = GetMenuItemID(hmenu, imni);
		if (nmId == kcidInsListSubItem || nmId == kcidInsListItemAft ||
			nmId == kcidInsListItemBef || nmId == kcidInsListItem)
		{
			// This is the insert menu.
			for (imni = cmni - 1; imni > -1; imni--)
			{
				UINT nmId = GetMenuItemID(hmenu, imni);
				if (nmId == kcidInsListSubItem || nmId == kcidInsListItemAft ||
					nmId == kcidInsListItemBef || nmId == kcidInsListItem)
					::DeleteMenu(hmenu, imni, MF_BYPOSITION);
			}

			if (GetPossListInfoPtr()->GetIsSorted())
			{
				StrApp str(kcidInsListItem);
				int ich = str.FindStr(L"\n");
				Assert(ich != -1);
				str = str.Right(str.Length() - ich - 1);
				ich = str.FindStr(L"\n");
				Assert(ich != -1);
				str = str.Left(ich);
				::InsertMenu(hmenu, 0, MF_STRING, kcidInsListItem, str.Chars());
			}
			else
			{
				StrApp strBef(kcidInsListItemBef);
				int ich = strBef.FindStr(L"\n");
				Assert(ich != -1);
				strBef = strBef.Right(strBef.Length() - ich - 1);
				ich = strBef.FindStr(L"\n");
				Assert(ich != -1);
				strBef = strBef.Left(ich);
				::InsertMenu(hmenu, 0, MF_STRING, kcidInsListItemBef, strBef.Chars());

				StrApp str(kcidInsListItemAft);
				ich = str.FindStr(L"\n");
				Assert(ich != -1);
				str = str.Right(str.Length() - ich - 1);
				ich = str.FindStr(L"\n");
				Assert(ich != -1);
				str = str.Left(ich);
				::InsertMenu(hmenu, 0, MF_STRING, kcidInsListItemAft, str.Chars());
			}

			if (GetPossListInfoPtr()->GetDepth() > 1)
			{
				StrApp str(kcidInsListSubItem);
				int ich = str.FindStr(L"\n");
				Assert(ich != -1);
				str = str.Right(str.Length() - ich - 1);
				ich = str.FindStr(L"\n");
				Assert(ich != -1);
				str = str.Left(ich);
				::InsertMenu(hmenu, 0, MF_STRING, kcidInsListSubItem, str.Chars());
			}
		// we break here because we only need to do this operation once.
		break;
		}
	}

	SuperClass::FixMenu(hmenu);
}


/*----------------------------------------------------------------------------------------------
	Return the selected treebar tree item.

	@return The selected treebar tree item.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::SetTreeSel(HTREEITEM hitem)
{
	Assert(m_hwndTrBr);
	TreeView_SelectItem(m_hwndTrBr, hitem);
}

/*----------------------------------------------------------------------------------------------
	Called when the user has selected a menu item in the tree bar pop up menu.  It sets whether
	abbreviations or names are shown.  Deletes or adds tree items.  If adding a item, it makes
	sure the label is unique.

	@param pcnd menu command
----------------------------------------------------------------------------------------------*/
void CleMainWnd::OnTreeMenuChange(Cmd * pcmd)
{
	Assert(m_hwndTrBr);
	Assert(m_qtrbr);
	AssertObj(pcmd);
	switch (pcmd->m_cid)
	{
	case kcidViewTreeAbbrevs:
	case kcidViewTreeNames:
	case kcidViewTreeBoth:
		{
			PossListInfo * ppli = GetPossListInfoPtr();
			PossNameType pnt;
			if (pcmd->m_cid == kcidViewTreeNames)
				pnt = kpntName;
			else if (pcmd->m_cid == kcidViewTreeBoth)
				pnt = kpntNameAndAbbrev;
			else
				pnt = kpntAbbreviation;

			if (pnt != ppli->GetDisplayOption())
				ppli->SetDisplayOption(pnt); // This will update everything.
			break;
		}
	case kcidTrBarMerge:
		{
			CmdMerge();
			break;
		}
	case kcidTrBarDelete:
		{
			CmdEditDelete(pcmd);
			break;
		}
	case kcidTrBarInsert:
	case kcidTrBarInsertBef:
		{
			InsertEntry((int)kcidInsListItemBef);
			break;
		}
	case kcidTrBarInsertAft:
		{
			InsertEntry((int)kcidInsListItemAft);
			break;
		}
	case kcidTrBarInsertSub:
		{
			InsertEntry((int)kcidInsListSubItem);
			break;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Merges the currently selected item into another item.  This Opens the merge dialog and
	allows the user to select an item to merge into.  It then does the merge.
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CmdMerge()
{
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
		RecMainWnd * prmwLoop = dynamic_cast<RecMainWnd *>(vqafw[iafw].Ptr());
		AssertPtr(prmwLoop);
		if (!prmwLoop->IsOkToChange())
		{
			// Bring the bad window to the top.
			::SetForegroundWindow(prmwLoop->Hwnd());
			return false;
		}
	}

	// Get the source object.
	HVO	hvoSrc = m_vhcFilteredRecords[m_ihvoCurr].hvo;

	// Can't merge an item if its IsProtected flag is set.
	if (!IsPossibilityDeletable(hvoSrc, kstidCantMergeItem))
		return true;

	AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(m_qmdic->GetCurChild());
	AfDeSplitChildPtr qadsc = dynamic_cast<AfDeSplitChild *>(qafcrw->GetPane(0));
	qadsc->CloseAllEditors();

	PossChsrMrgPtr qpcm;
	qpcm.Create();
	PossListInfoPtr qpli = GetPossListInfoPtr();
	qpcm->SetDialogValues(qpli, hvoSrc);
	if (qpcm->DoModal(m_hwnd) != kctidOk)
		return true;
	HVO hvoDst = qpcm->GetSelHvo();

	// do a merge of two list items
	WaitCursor wc;
	StrUni stuCmd;
	ComBool fMoreRows;
	ComBool fIsNull;
	ULONG cbSpaceTaken;
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	pdbi->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));
	HashMap<GUID,GUID> hmguidGuid;
	GUID uidSrc;
	GUID uidDst;
	stuCmd.Format(L"select [Guid$] from cmobject where id = %d", hvoSrc);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	fIsNull = TRUE;
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&uidSrc), sizeof(uidSrc),
			&cbSpaceTaken, &fIsNull, 0));
	}

	if (!fIsNull)
	{
		stuCmd.Format(L"select [Guid$] from cmobject where id = %d", hvoDst);
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		fIsNull = TRUE;
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&uidDst), sizeof(uidDst),
				&cbSpaceTaken, &fIsNull, 0));
		}
	}

	if (!fIsNull)
	{
		hmguidGuid.Insert(uidSrc, uidDst);
		AfProgressDlgPtr qprog;
		try
		{
			// Save data before calling crawler since it uses a different connection.
			RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(AfApp::Papp()->GetCurMainWnd());
			AssertPtr(prmw);
			prmw->SaveData();
			StrUni stuServer = pdbi->ServerName();
			StrUni stuDB = pdbi->DbName();

			FwDbChangeOverlayTags dsc(hmguidGuid);
			qprog.Create();
			qprog->DoModeless(NULL);
			qprog->SetRange(0, 100);
			IAdvInd3Ptr qadvi3;
			qprog->QueryInterface(IID_IAdvInd3, (void **)&qadvi3);

			IStreamPtr qfist;
			CheckHr(AfApp::Papp()->GetLogPointer(&qfist));
			if (dsc.Init(stuServer, stuDB, qfist, qadvi3))
			{
				dsc.ResetConnection();
				dsc.CreateCommand();
				dsc.DoAll(kstidMergeItemPhaseOne, kstidMergeItemPhaseOne, false);
			}
			dsc.ResetConnection();
			dsc.Terminate(1);		// value doesn't matter with no relaunch!
			if (qprog)
				qprog->DestroyHwnd();
		}
		catch (...)
		{
			if (qprog)
				qprog->DestroyHwnd();
			return false;
		}
	}
	qpli->MergeItem(hvoSrc, hvoDst);

	// Set the index to the destination record.
	int ihvo;
	for (ihvo = m_vhcFilteredRecords.Size(); --ihvo >= 0; )
	{
		if (m_vhcFilteredRecords[ihvo].hvo == hvoDst)
		{
			m_ihvoCurr = ihvo;
			break;
		}
	}
	if (ihvo < 0)
		m_ihvoCurr = 0;

	// Update all lists now that we've changed.
	SyncInfo sync(ksyncMergePss, qpli->GetPsslId(), 0);
	return m_qlpi->StoreAndSync(sync);
}

/*----------------------------------------------------------------------------------------------
	Go through the tree to check to see if the name and abbreviation are unique.

	@param staName Name to check
	@param staAbbr Abbriviation to check

	@return true if unique.
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CheckUnique(StrAnsi staName, StrAnsi staAbbr)
{
	PossListInfo * ppli = GetPossListInfoPtr();
	int cpii = ppli->GetCount();
	for (int ipii = 0; ipii < cpii; ipii++)
	{
		PossItemInfo * ppii = ppli->GetPssFromIndex(ipii);
		AssertPtr(ppii);
		StrUni stu;
		ppii->GetName(stu, kpntName);
		if (stu == staName)
			return false;
		ppii->GetName(stu, kpntAbbreviation);
		if (stu == staAbbr)
			return false;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	The user has selected a different item in the tree bar, so load that record. This also
	gets called as a side effect when we set the record index. When called in that mode
	we don't want to do anything here.

	@param hItem handle to treeview item
	@param hvoItem handle to treeview item object
----------------------------------------------------------------------------------------------*/
void CleMainWnd::OnTreeBarChange(HTREEITEM hItem, HVO hvoItem)
{
	if (m_fSettingRecIndex)
		return;

	AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(m_qmdic->GetCurChild());
	if (!qafcrw)
		return;
	if (!qafcrw->IsOkToChange(m_fCheckRequired))
	{
		SetCurRecIndex(m_ihvoCurr);		// Restore the tree bar location.  See CLE-70.
		return;
	}

	// Need to close editor here to save current cursor information.
	CleDeSplitChild * pcleadsc = dynamic_cast<CleDeSplitChild *>(qafcrw->CurrentPane());
	if (pcleadsc)
		pcleadsc->CloseEditor();

	// For now we save data whenever we go to a new record in order to clear the undo stack.
	// Otherwise users can undo things that do not show up on the screen which is very
	// confusing. Of course, there are various other ways this can still happen such as
	// clicking in another FW window, switching views, etc. This is only a partial solution
	// until Undo can be enhanced to include full cursor location information.
	SaveData();

	int crec = m_vhcFilteredRecords.Size();
	for (int irec = 0; irec < crec; irec++)
	{
		if (m_vhcFilteredRecords[irec].hvo == hvoItem)
		{
			m_ihvoCurr = irec;
			break;
		}
	}
	// [Note: DispCurRec call must be done after m_ihvoCurr is set,
	// or it won't work right, since we may be jumping several records here.]
	if (pcleadsc && pcleadsc->GetNeedSync())
	{
		// Update all lists now that we've changed.
		SyncInfo sync(ksyncPossList, GetRootObj(), 0);
		m_qlpi->StoreAndSync(sync);
		pcleadsc->SetNeedSync(false);
	}
	else
		qafcrw->DispCurRec(0,0);
}


/*----------------------------------------------------------------------------------------------
	Handle First, Last, Next, Previous record.
	@param pcmd Ptr to menu command
	@return true if successfully handled
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CmdRecSel(Cmd * pcmd)
{
	AssertObj(pcmd);

	if (!m_qmdic)
		return false;

	AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(m_qmdic->GetCurChild());
	// Can't change records if window isn't ready to close.
	if (!qafcrw || !qafcrw->IsOkToChange(true))
		return false;
	CleDeSplitChild * pcleadsc = dynamic_cast<CleDeSplitChild *>(qafcrw->CurrentPane());
	if (pcleadsc)
	{
		pcleadsc->CloseEditor();
		if (pcleadsc->GetNeedSync())
		{
			// Update all lists now that we've changed.
			SyncInfo sync(ksyncPossList, GetRootObj(), 0);
			m_qlpi->StoreAndSync(sync);
			pcleadsc->SetNeedSync(false);
		}
	}
	return SuperClass::CmdRecSel(pcmd);
}


/*----------------------------------------------------------------------------------------------
	The user has selected a different item in the view bar.
	For Views:  Clear the active rootbox.  Save the current changes (if any) to the database.
		Switch to the selected view.  Update the caption bar.
	For Filters, Sort order, or Overlays:
		Switch to the selected filter, sort order, or overlay, then update the caption bar.

	@param ilist viewbar vector index
	@param siselOld the viewbar index of what was selected earlier
	@param siselNew the viewbar index of what is now selected

	@return true
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::OnViewBarChange(int ilist, Set<int> & siselOld, Set<int> & siselNew)
{
	bool fStatusHidden = !::IsWindowVisible(m_qstbr->Hwnd());
	// Don't allow changes if editors can't be closed.
	if (!IsOkToChange())
		return false;
	bool fChanging = !siselOld.Equals(siselNew);
	int ivblt = GetViewbarListType(ilist);

	switch (ivblt)
	{
	case kvbltTree: // Tree bar
		{
			AfCaptionBar * pcpbr = m_qmdic->GetCaptionBar();
			AssertPtr(pcpbr);
			pcpbr->SetIconImage(m_ivblt[kvbltTree], kimagTree);
		}
		break;

	case kvbltView: // Views
		{
			::SetFocus(NULL);

			// Clear out the active rootbox.
			if (fChanging)
				SetActiveRootBox(NULL);

			Assert(siselNew.Size() == 1);
			int iview = *siselNew.Begin();
			WaitCursor wc;
			if (fStatusHidden)
			{
				::ShowWindow(m_qstbr->Hwnd(), SW_SHOW);
				::SendMessage(m_hwnd, WM_SIZE, kwstRestored, 0);
			}
			StrApp strMsg;
			UserViewSpecVec & vuvs = m_qlpi->GetDbInfo()->GetUserViewSpecs();
			Assert((uint)iview < (uint)vuvs.Size());
			switch (vuvs[iview]->m_vwt)
			{
			case kvwtDE:
				strMsg.Load(kstidStBar_CreatingDataEntryView);
				break;
			case kvwtDoc:
				strMsg.Load(kstidStBar_CreatingDocumentView);
				break;
			case kvwtBrowse:
				strMsg.Load(kstidStBar_CreatingBrowseView);
				break;
			default:
				Assert(false);
				strMsg.Load(kstidStBar_CreatingUnknownView);
				break;
			}
			m_vwt = vuvs[iview]->m_vwt;
			m_qstbr->StartProgressBar(strMsg.Chars(), 0, 1000, 50);
			// Save the current changes (if any) to the database, unless we are not
			// actually changing anything (e.g., for a refresh)
			if (fChanging)
			{
				// Close open field editor on changing view so we store the cursor location.
				AfDeSplitChild * padsc = CurrentDeWnd();
				if (padsc)
					padsc->CloseEditor();
				SaveData();
			}
			m_qstbr->StepProgressBar();

			if (m_qmdic->SetCurChildFromIndex(iview))
			{
				AfClientRecWnd * prcw = dynamic_cast<AfClientRecWnd *>(m_qmdic->GetCurChild());
				Assert(prcw);
				prcw->DispCurRec(0,0);
			}
			else
			{
				// Force the focus to go back to the current view window if this is the
				// current top-level window.
				if (AfApp::Papp()->GetCurMainWnd() == this)
					::SetFocus(m_hwnd);
			}

			// Update the caption bar.
			UpdateCaptionBar();

			AfCaptionBar * pcpbr = m_qmdic->GetCaptionBar();
			AssertPtr(pcpbr);
			AfClientWnd * pafcw = m_qmdic->GetCurChild();
			AssertPtr(pafcw);
			pcpbr->SetIconImage(m_ivblt[kvbltView], pafcw->GetImageIndex());
			m_qstbr->EndProgressBar();
			if (fStatusHidden)
			{
				::ShowWindow(m_qstbr->Hwnd(), SW_HIDE);
				::SendMessage(m_hwnd, WM_SIZE, kwstRestored, 0);
			}
		}
		UpdateStatusBar();
		break;

	case kvbltFilter: // Filters
		{
			bool fCancel = m_fFilterCancelled;

			if (siselNew.Size() == 0)
			{
				// This can happen if a filter that requires a prompt is selected when the
				// program first starts up and the user cancels the filter. In that case, no
				// filter was previously selected, so we default to selecting 'No Filter'.
				int iflt = 0;
				siselNew.Insert(iflt);
				m_qvwbrs->SetSelection(m_ivblt[kvbltFilter], siselNew);
				return true;
			}

			Assert(siselNew.Size() == 1);
			int ifltr = m_qlpi->GetDbInfo()->ComputeFilterIndex(*siselNew.Begin(),
				GetRecordClid());
			AppFilterInfo afi;
			if (ifltr > -1)
				m_qlpi->GetDbInfo()->GetFilterInfo(ifltr);

			if (fStatusHidden)
			{
				::ShowWindow(m_qstbr->Hwnd(), SW_SHOW);
				::SendMessage(m_hwnd, WM_SIZE, kwstRestored, 0);
			}

			StrApp strMsg;
			if (ifltr < 0)
			{
				strMsg.Load(kstidStBar_RemovingFilter);
			}
			else
			{
				StrApp strFmt(kstidStBar_ApplyingFilter);
				StrApp strName = afi.m_stuName;
				strMsg.Format(strFmt.Chars(), strName.Chars());
			}
			m_qstbr->StartProgressBar(strMsg.Chars(), 0, 1000, 50);

			// Update the caption bar.
			AfCaptionBar * pcpbr = m_qmdic->GetCaptionBar();
			AssertPtr(pcpbr);
			int imag = kimagFilterNone;
			if (ifltr > -1)
				imag = afi.m_fSimple ? kimagFilterSimple : kimagFilterFull;
			pcpbr->SetIconImage(m_ivblt[kvbltFilter], imag);
			// Get the current sort method.
			int isrt = -1;
			Set<int> sisort;
			m_qvwbrs->GetSelection(m_ivblt[kvbltSort], sisort);
			if (sisort.Size())
			{
				Assert(sisort.Size() == 1);
				isrt = m_qlpi->GetDbInfo()->ComputeSortIndex(*sisort.Begin(), GetRecordClid());
			}
			FwFilterXrefUtil fxref;
			ApplyFilterAndSort(ifltr, fCancel, isrt, &fxref);

			m_qstbr->EndProgressBar();
			UpdateStatusBar();
			if (fStatusHidden)
			{
				::ShowWindow(m_qstbr->Hwnd(), SW_HIDE);
				::SendMessage(m_hwnd, WM_SIZE, kwstRestored, 0);
			}
			m_fFilterCancelled = fCancel;
			if (fCancel)
				m_qvwbrs->SetSelection(m_ivblt[kvbltFilter], siselOld);
		}
		break;


	case kvbltSort: // Sort orders
		{
			Assert(siselNew.Size() == 1);
			int isrt = m_qlpi->GetDbInfo()->ComputeSortIndex(*siselNew.Begin(),
				GetRecordClid());
			if (fStatusHidden)
			{
				::ShowWindow(m_qstbr->Hwnd(), SW_SHOW);
				::SendMessage(m_hwnd, WM_SIZE, kwstRestored, 0);
			}
			StrApp strMsg;
			if (isrt < 0)
			{
				strMsg.Load(kstidStBar_ApplyingDefaultSort);
			}
			else
			{
				StrApp strFmt(kstidStBar_ApplyingSort);
				AppSortInfo & asi = m_qlpi->GetDbInfo()->GetSortInfo(isrt);
				StrApp strName = asi.m_stuName;
				strMsg.Format(strFmt.Chars(), strName.Chars());
			}
			m_qstbr->StartProgressBar(strMsg.Chars(), 0, 1000, 50);

			// Update the caption bar.
			// REVIEW: This seems rather pointless with only one possible icon.
			AfCaptionBar * pcpbr = m_qmdic->GetCaptionBar();
			AssertPtr(pcpbr);
			pcpbr->SetIconImage(m_ivblt[kvbltSort], kimagSort);

			Set<int> sifilt;
			m_qvwbrs->GetSelection(m_ivblt[kvbltFilter], sifilt);
			int iflt = -1;
			bool fNoPrompt = true;
			if (sifilt.Size())
			{
				iflt = m_qlpi->GetDbInfo()->ComputeFilterIndex(*sifilt.Begin(),
					GetRecordClid());
			}

			FwFilterXrefUtil fxref;
			ApplyFilterAndSort(iflt, fNoPrompt, isrt, &fxref);

			m_qstbr->EndProgressBar();
			UpdateStatusBar();
			if (fStatusHidden)
			{
				::ShowWindow(m_qstbr->Hwnd(), SW_HIDE);
				::SendMessage(m_hwnd, WM_SIZE, kwstRestored, 0);
			}
		}
		break;

	default:
		Assert(false);
		break;
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
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
				m_hvoTarget = m_plddDragDrop.GetSourceHvo();
				SyncInfo sync(ksyncMoveEntry, m_hvoPssl, 0);
				return m_qlpi->StoreAndSync(sync);
			}
			return true;
		}
		break;
	case WM_WINDOWPOSCHANGING:
		m_plddDragDrop.KillDrag();
		return false;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Returns the HVO of the current record or subrecord.
----------------------------------------------------------------------------------------------*/
HVO CleMainWnd::GetCurRec()
{
	HVO hvoCurRec = 0;
	Assert(m_vhvoPath.Size() >= m_vflidPath.Size());
	for (int iflid = 0; iflid < m_vflidPath.Size(); ++iflid)
	{
		if (m_vflidPath[iflid] == kflidCmPossibility_SubPossibilities)
			continue;
		hvoCurRec = m_vhvoPath[iflid];
		break;
	}
	// This normally shouldn't happen, but somehow it did when I opened a second Doc window.
	// So in case m_vhvoPath/m_vflidPath are 0, then we'll resort to getting the current rec.
	if (!hvoCurRec)
		hvoCurRec = m_vhcFilteredRecords[m_ihvoCurr].hvo;
	return hvoCurRec;
}


/*----------------------------------------------------------------------------------------------
	Respond to the Edit...Delete menu item.
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CmdEditDelete(Cmd * pcmd)
{
	// Get the object we are deleting, its owner, and its owning flid.
	HVO	hvo = m_vhcFilteredRecords[m_ihvoCurr].hvo;
	HVO hvoOwn; // Owner of current object.
	int flid; // Property of owner that holds object to delete.
	AssertPtr(m_qcvd);
	CheckHr(m_qcvd->get_ObjOwner(hvo, &hvoOwn));
	CheckHr(m_qcvd->get_ObjOwnFlid(hvo, &flid));

	// See if there is selected text
	IVwRootBoxPtr qrootb = GetActiveRootBox(true);
	IVwSelectionPtr qvwsel = NULL;
	if (qrootb)
		CheckHr(qrootb->get_Selection(&qvwsel));
	ITsStringPtr qtssSel;
	SmartBstr sbstr = L"";
	if (qvwsel) // fails also if no root box
		CheckHr(qvwsel->GetSelectionString(&qtssSel, sbstr));
	int cchSel = 0;
	if (qtssSel)
		CheckHr(qtssSel->get_Length(&cchSel));
	if (cchSel)
	{
		// there is a current selection so put up the delete dialog.
		DeleteDlgPtr qdd;
		qdd.Create();
		StrApp strMsg;
		strMsg.Load(flid == kflidCmPossibility_SubPossibilities ?
			kstidListSubitem : kstidListItem);
		qdd->SetDialogValue(strMsg);
		if (kctidOk != qdd->DoModal(m_hwnd))
		{
			return true;
		}
		if (!qdd->GetDialogValue())
		{
			// Delete the selected text.
			IVwRootSitePtr qvrs;
			CheckHr(qrootb->get_Site(&qvrs));
			AfVwRootSite * pvwnd = dynamic_cast<AfVwRootSite *>(qvrs.Ptr());
			pvwnd->OnChar(127, 1, 83);
			return true;
		}
	}

	// Abort if any editor can't be changed.  Because we don't currently have a way force a
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
		RecMainWnd * prmwLoop = dynamic_cast<RecMainWnd *>(vqafw[iafw].Ptr());
		AssertPtr(prmwLoop);
		if (!prmwLoop->IsOkToChange())
		{
			// Bring the bad window to the top.
			::SetForegroundWindow(prmwLoop->Hwnd());
			return false;
		}
		// Close any active editors before we delete, or we may try saving something to
		// a record that has already been deleted.
		prmwLoop->CloseActiveEditors();
	}

	StrApp strType;
	StrApp strSub;
	int clid;
	m_qcvd->get_ObjClid(hvo, &clid);
	strType.Load(flid == kflidCmPossibility_SubPossibilities ?
		kstidListSubitem : kstidListItem);
	strSub.Load(kstidListSubitems);

	// Don't let an item be deleted if the IsProtected flag is set.
	if (!IsPossibilityDeletable(hvo, kstidCantDeleteItem))
		return true;

	// put up the delete object dialog
	ITsStringPtr qtss;
	DeleteObjDlgPtr qdo;
	qdo.Create();
	GetDragText(hvo, clid, &qtss);
	qdo->SetDialogValues(hvo, qtss, strType, strSub, kclidCmPossibility, m_qlpi->GetDbInfo());
	qdo->SetHelpUrl(_T("User_Interface/Menus/Edit/Delete_an_item_or_subitem.htm")); // Different from standard help in NoteBook
	if (kctidOk != qdo->DoModal(m_hwnd))
		return true;

	WaitCursor wc;

	// Delete the item from the database, first deleting it from any string overlay tags.
	StrUni stuCmd;
	ComBool fMoreRows;
	ComBool fIsNull;
	ULONG cbSpaceTaken;
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	pdbi->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));
	stuCmd.Format(L"select [Guid$] from CmObject where Id = %d", hvo);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		GUID guid;
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&guid), sizeof(guid),
			&cbSpaceTaken, &fIsNull, 0));
		AfProgressDlgPtr qprog;
		try
		{
			// Save data before calling crawler since it uses a different connection.
			RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(AfApp::Papp()->GetCurMainWnd());
			AssertPtr(prmw);
			prmw->SaveData();
			StrUni stuServer = pdbi->ServerName();
			StrUni stuDB = pdbi->DbName();

			FwDbDeleteOverlayTags dsc(&guid);
			qprog.Create();
			qprog->DoModeless(NULL);
			qprog->SetRange(0, 100);
			IAdvInd3Ptr qadvi3;
			qprog->QueryInterface(IID_IAdvInd3, (void **)&qadvi3);
			IStreamPtr qfist;
			CheckHr(AfApp::Papp()->GetLogPointer(&qfist));
			if (dsc.Init(stuServer, stuDB, qfist, qadvi3))
			{
				dsc.ResetConnection();
				dsc.CreateCommand();
				dsc.DoAll(kstidDeleteItemPhaseOne, kstidDeleteItemPhaseOne, false);
			}
			dsc.ResetConnection();
			dsc.Terminate(1);		// value doesn't matter with no relaunch!
			if (qprog)
				qprog->DestroyHwnd();
		}
		catch (...)
		{
			if (qprog)
				qprog->DestroyHwnd();
			return false;
		}
	}
	qodc.Clear();
	pdbi->DeleteObject(hvo);

	// Update all lists now that we've changed.
	SyncInfo sync(ksyncDelPss, m_hvoPssl, hvo);
	m_qlpi->StoreAndSync(sync);

	// Check for empty window.
	for (iafw = 0; iafw < cafw; iafw++)
	{
		CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(vqafw[iafw].Ptr());
		AssertPtr(pcmw);
		PossListInfoPtr qpli = pcmw->GetPossListInfoPtr();

		if ((pcmw->GetLpInfo() == m_qlpi) && (qpli->GetPsslId() == m_hvoPssl)
			&& !pcmw->RawRecordCount())
		{
			bool fCancel;
			pcmw->CheckEmptyRecords(NULL, NULL, fCancel);
		}
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	This reloads the main records.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::LoadData()
{
	AssertPtr(m_qlpi);
	AssertPtr(m_qcvd);

	HVO hvoCur = 0;
	if (m_vhcFilteredRecords.Size())
	{
		Assert(m_ihvoCurr < m_vhcFilteredRecords.Size());
		hvoCur = m_vhcFilteredRecords[m_ihvoCurr].hvo;
	}
	int ihvoCur = m_ihvoCurr;
	m_ihvoCurr = -1;

	m_qcvd->SetTags(kflidCmPossibilityList_Possibilities);
	m_vhcRecords.Clear();
	m_qcvd->LoadMainItems(m_hvoPssl, m_vhcRecords);

	m_vhcRecords.Clear();
	PossListInfo * ppli = GetPossListInfoPtr(); // recompute, may have changed above.
	int cpii = ppli->GetCount();
	for (int ipii = 0; ipii < cpii; ipii++)
	{
		PossItemInfo * ppiiTmp = ppli->GetPssFromIndex(ipii);
		AssertPtr(ppiiTmp);
		HvoClsid hc;
		hc.hvo = ppiiTmp->GetPssId();
		hc.clsid = m_ItemClsid;
		m_vhcRecords.Push(hc);
		if (hc.hvo == hvoCur)
			m_ihvoCurr = ipii;
	}
	// If the original item is no longer around, pick something close.
	if (m_ihvoCurr == -1)
	{
		m_ihvoCurr = (uint)ihvoCur >= (uint)m_vhcRecords.Size() ?
			m_vhcRecords.Size() - 1 : ihvoCur;
	}
	m_vhcFilteredRecords = m_vhcRecords;

	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	Set<int> sisel;
	m_qvwbrs->GetSelection(m_ivblt[kvbltFilter], sisel);
	Assert(sisel.Size() == 1);
	int iflt = pdbi->ComputeFilterIndex(*sisel.Begin(), GetRecordClid());
	m_qvwbrs->GetSelection(m_ivblt[kvbltSort], sisel);
	Assert(sisel.Size() == 1);
	int isrt = pdbi->ComputeFilterIndex(*sisel.Begin(), GetRecordClid());
	bool fNoPrompt = true;
	FwFilterXrefUtil fxref;
	ApplyFilterAndSort(iflt, fNoPrompt, isrt, &fxref);
}

/*----------------------------------------------------------------------------------------------
	Bring up another top-level window with the same view.

	@param pcmd menu command

	@return true
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CmdWndNew(Cmd * pcmd)
{
	SaveSettings(NULL);

	AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(m_qmdic->GetCurChild());
	AssertPtr(qafcrw);

	// Save a field editor if open, so that it saves the current cursor location.
	AfClientRecDeWndPtr qcrde = dynamic_cast<AfClientRecDeWnd *>(qafcrw.Ptr());
	if (qcrde)
	{
		AfDeSplitChild * pdsc = dynamic_cast<AfDeSplitChild *>(qcrde->CurrentPane());
		AssertPtr (pdsc);
		AfDeFieldEditor * pdfe = pdsc->GetActiveFieldEditor();
		if (pdfe)
			pdfe->SaveFullCursorInfo();
	}

	WndSettings wndSet;
	qafcrw->GetVwSpInfo(&wndSet);

	// Save the filter selection
	AfViewBarShell * pvwbrs = GetViewBarShell();
	AssertObj(pvwbrs);
	Set<int> sisel;
	pvwbrs->GetSelection(m_ivblt[kvbltFilter], sisel);

	// Save the Sort Method selection
	Set<int> siselSort;
	pvwbrs->GetSelection(m_ivblt[kvbltSort], siselSort);

	WndCreateStruct wcs;
	wcs.InitMain(_T("CleMainWnd"));

	PrepareNewWindowLocation();

	CleMainWndPtr qcmw;
	try
	{
		qcmw.Create();
		qcmw->SetHvoPssl(m_hvoPssl);
		qcmw->Init(m_qlpi);
		Set<int> siselView;
		pvwbrs->GetSelection(m_ivblt[kvbltView], siselView);
		Assert(siselView.Size() == 1);
		int nView = *siselView.Begin();
		qcmw->SetStartupInfo(m_vhvoPath.Begin(), m_vhvoPath.Size(), m_vflidPath.Begin(),
			m_vflidPath.Size(), m_ichCur, nView);
		qcmw->CreateHwnd(wcs);
	}
	catch (...)
	{
		qcmw.Clear();
		StrApp str(kstidOutOfResources);
		::MessageBox(NULL, str.Chars(), _T(""), MB_ICONSTOP | MB_OK);
		return true;
	}

	// Split the status bar into five sections of varying widths:
	//		1. record id
	//		2. progress / info
	//		3. sort
	//		4. filter
	//		5. count
	AfStatusBarPtr qstbr = qcmw->GetStatusBarWnd();
	Assert(qstbr);
	qstbr->InitializePanes();
	qcmw->UpdateStatusBar();

	AfMdiClientWndPtr qmdic = qcmw->GetMdiClientWnd();
	AssertPtr(qmdic);
	qafcrw = dynamic_cast<AfClientRecWnd *>(qmdic->GetCurChild());
	AssertPtr(qafcrw);

	// Restore the filter selection
	pvwbrs = qcmw->GetViewBarShell();
	AssertObj(pvwbrs);
	pvwbrs->SetSelection(m_ivblt[kvbltFilter], sisel);
	// Restore the sort method selection.
	pvwbrs->SetSelection(m_ivblt[kvbltSort], siselSort);
	qafcrw->RestoreVwInfo(wndSet);

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

	@param pcmd Ptr to menu command

	@return
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CmdViewExpMenu(Cmd * pcmd)
{
	AssertPtr(pcmd);

	// Don't allow menus if we can't close the current editor.
	if (!IsOkToChange())
		return false;

	int ma = pcmd->m_rgn[0];
	if (ma == AfMenuMgr::kmaExpandItem)
	{
		m_vExtMnuIdx.Clear();
		// We need to expand the dummy menu item.
		HMENU hmenu = (HMENU)pcmd->m_rgn[1];
		int imni = pcmd->m_rgn[2];
		int & cmniAdded = pcmd->m_rgn[3];

		switch (pcmd->m_cid)
		{
		case kcidExpViews:
			{
				int cview = m_qmdic->GetChildCount();
				int cAdded = 0;
				for (int iview = 0; iview < cview; iview++)
				{
					AfClientWnd * pafcw = m_qmdic->GetChildFromIndex(iview);
					AssertPtr(pafcw);
					::InsertMenu(hmenu, imni + iview, MF_BYPOSITION, kcidMenuItemDynMin + iview,
						pafcw->GetViewName());
					m_vExtMnuIdx.Push(cAdded++);
				}
				cmniAdded = cview;
				return true;
			}

		case kcidExpFilters:
			{
				AfDbInfo * pdbi = m_qlpi->GetDbInfo();
				AssertPtr(pdbi);

				int cAdded = 0;
				StrApp str(kstidNoFilterHotKey);
				::InsertMenu(hmenu, imni, MF_BYPOSITION, kcidMenuItemDynMin, str.Chars());
				m_vExtMnuIdx.Push(cAdded++);
				cmniAdded = 1;

				int cflt = pdbi->GetFilterCount();
				bool fSep = false;

				for (int iflt = 0; iflt < cflt; iflt++)
				{
					AppFilterInfo & afi = pdbi->GetFilterInfo(iflt);
					if (afi.m_clidRec == GetRecordClid())
					{
						str = afi.m_stuName.Chars();
						if (!fSep)
						{
							::InsertMenu(hmenu, imni + 1, MF_BYPOSITION, MF_SEPARATOR, NULL);
							++cmniAdded;
							fSep = true;
						}
						::InsertMenu(hmenu, imni + cmniAdded, MF_BYPOSITION,
							kcidMenuItemDynMin + iflt + 1, str.Chars());
						m_vExtMnuIdx.Push(cAdded++);
						++cmniAdded;
					}
					else
						m_vExtMnuIdx.Push(-1);
				}
				return true;
			}

		case kcidExpSortMethods:
			{
				AfDbInfo * pdbi = m_qlpi->GetDbInfo();
				AssertPtr(pdbi);

				int cAdded = 0;
				StrApp str(kstidDefaultSortHotKey);
				::InsertMenu(hmenu, imni, MF_BYPOSITION, kcidMenuItemDynMin, str.Chars());
				m_vExtMnuIdx.Push(cAdded++);
				cmniAdded = 1;

				int csrt = pdbi->GetSortCount();
				bool fSep = false;

				for (int isrt = 0; isrt < csrt; isrt++)
				{
					AppSortInfo & asi = pdbi->GetSortInfo(isrt);
					if (asi.m_clidRec == GetRecordClid())
					{
						StrApp str(asi.m_stuName.Chars());
						if (!fSep)
						{
							::InsertMenu(hmenu, imni + 1, MF_BYPOSITION, MF_SEPARATOR, NULL);
							++cmniAdded;
							fSep = true;
						}
						::InsertMenu(hmenu, imni + cmniAdded, MF_BYPOSITION,
							kcidMenuItemDynMin + isrt + 1, str.Chars());
						m_vExtMnuIdx.Push(cAdded++);
						++cmniAdded;
					}
					else
						m_vExtMnuIdx.Push(-1);
				}
				return true;
			}
		}
	}
	else if (ma == AfMenuMgr::kmaGetStatusText)
	{
		// We need to return the text for the expanded menu item.
		//    m_rgn[1] holds the index of the selected item.
		//    m_rgn[2] holds a pointer to the string to set

		int imni = pcmd->m_rgn[1];
		StrApp * pstr = (StrApp *)pcmd->m_rgn[2];
		AssertPtr(pstr);

		switch (pcmd->m_cid)
		{
		case kcidExpViews:
			{
				AfClientWnd * pafcw = m_qmdic->GetChildFromIndex(imni);
				AssertPtr(pafcw);
				StrApp strFormat(kstidSelectView);
				pstr->Format(strFormat.Chars(), pafcw->GetViewName());
				return true;
			}
		case kcidExpFilters:
			if (imni == 0)
			{
				if (!AfUtil::GetResourceStr(krstStatusEnabled, kcidViewFltrsNone, *pstr))
					return false;
			}
			else
			{
				AfDbInfo * pdbi = m_qlpi->GetDbInfo();
				AssertPtr(pdbi);
				int iflt = imni - 1;
				Assert(iflt >= 0 && iflt < pdbi->GetFilterCount());
				AppFilterInfo & afi = pdbi->GetFilterInfo(iflt);
				StrApp str(afi.m_stuName.Chars());
				StrApp strFormat(kstidSelectFilter);
				pstr->Format(strFormat.Chars(), str.Chars());
			}
			return true;
		case kcidExpSortMethods:
			if (imni == 0)
			{
				if (!AfUtil::GetResourceStr(krstStatusEnabled, kcidViewSortsNone, *pstr))
					return false;
			}
			else if (imni > 1)
			{
				AfDbInfo * pdbi = m_qlpi->GetDbInfo();
				AssertPtr(pdbi);
				int isrt = imni - 1;
				Assert(isrt >= 0 && isrt < pdbi->GetSortCount());
				AppSortInfo & asi = pdbi->GetSortInfo(isrt);
				StrApp str(asi.m_stuName.Chars());
				StrApp strFormat(kstidSelectSortMethod);
				pstr->Format(strFormat.Chars(), str.Chars());
			}
			return true;
		}
	}
	else if (ma == AfMenuMgr::kmaDoCommand)
	{
		// The user selected an expanded menu item, so perform the command now.
		//    m_rgn[1] holds the menu handle.
		//    m_rgn[2] holds the index of the selected item.

		Set<int> sisel;
		int iitem = pcmd->m_rgn[2];
		switch (pcmd->m_cid)
		{
		case kcidExpViews:
			sisel.Insert(iitem);
			m_qvwbrs->SetSelection(m_ivblt[kvbltView], sisel);
			break;
		case kcidExpFilters:
			if (!iitem)
			{
				sisel.Insert(iitem);
			}
			else
			{
				// Inverse of pdbi->ComputerFilterIndex()
				// iitem = index + 1.  (iitem == 0 => No Filter)
				AfDbInfo * pdbi = m_qlpi->GetDbInfo();
				AssertPtr(pdbi);
				int isel = 0;
				Assert(iitem <= pdbi->GetFilterCount());
				for (int iflt = 0; iflt < iitem; ++iflt)
				{
					AppFilterInfo & afi = pdbi->GetFilterInfo(iflt);
					if (afi.m_clidRec == GetRecordClid())
						++isel;
				}
				sisel.Insert(isel);
			}
			m_qvwbrs->SetSelection(m_ivblt[kvbltFilter], sisel);
			break;
		case kcidExpSortMethods:
			if (!iitem)
			{
				sisel.Insert(iitem);
			}
			else
			{
				// Inverse of pdbi->ComputerSortIndex()
				// iitem = index + 1.  (iitem == 0 => Default Sort)
				AfDbInfo * pdbi = m_qlpi->GetDbInfo();
				AssertPtr(pdbi);
				int isel = 0;
				Assert(iitem <= pdbi->GetSortCount());
				for (int isrt = 0; isrt < iitem; ++isrt)
				{
					AppSortInfo & afi = pdbi->GetSortInfo(isrt);
					if (afi.m_clidRec == GetRecordClid())
						++isel;
				}
				sisel.Insert(isel);
			}
			m_qvwbrs->SetSelection(m_ivblt[kvbltSort], sisel);
			break;
		}
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Set the state for an expanded menu item.
	cms.GetExpMenuItemIndex() returns the index of the item to set the state for.
	To get the menu handle and the old ID of the dummy item that was replaced, call
	AfMenuMgr::GetLastExpMenuInfo.

	@param cms menu command state

	@return true
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CmsViewExpMenu(CmdState & cms)
{
	Set<int> sisel;
	int iitem = cms.GetExpMenuItemIndex();

	switch (cms.Cid())
	{
	case kcidExpViews:
		m_qvwbrs->GetSelection(m_ivblt[kvbltView], sisel);
		break;
	case kcidExpFilters:
		m_qvwbrs->GetSelection(m_ivblt[kvbltFilter], sisel);
		break;
	case kcidExpSortMethods:
		m_qvwbrs->GetSelection(m_ivblt[kvbltSort], sisel);
		break;
	}

	cms.SetCheck(sisel.IsMember(m_vExtMnuIdx[iitem]));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Get the ws value for the given possibility list.
----------------------------------------------------------------------------------------------*/
static int GetPossListWs(int ihvo)
{
	if (ihvo == CleLpInfo::kpidPsslLoc || ihvo == CleLpInfo::kpidPsslPeo)
		return kwsVernAnals;
	else
		return kwsAnals;
}


/*----------------------------------------------------------------------------------------------
	Create RecordSpecs for appropriate classes and levels for the specified type and add them
	to the supplied UserViewSpec. It may be used initially (by altering #if in
	RecMainWnd::InitMdiClient) to save newly created views to the database, and it is also used
	in TlsOptDlg to create a new view from scratch.

	@param vwt view type (e.g., kvwtBrowse, kvwtDE, kvwtDoc)
	@param plpi Ptr to language project info object
	@param puvs Out Ptr to a newly created UserViewSpec that we want to populate.
	@param pvuvs This should normally be NULL.  This value is used only when called from
					TlsOptView
----------------------------------------------------------------------------------------------*/
void CleMainWnd::MakeNewView(UserViewType vwt, AfLpInfo * plpi, UserViewSpec * puvs,
		UserViewSpecVec * pvuvs)
{
	AssertPtr(plpi);

	RecordSpecPtr qrsp;
	Vector<HVO> & vhvoPssl = plpi->GetPsslIds();
	Vector<int> vclid;
	int iclid;
	int stidLabel;
	int stidHelp;
	AfDbInfoPtr qdbi = plpi->GetDbInfo();
	AssertPtr(qdbi);

	puvs->m_vwt = vwt;
	puvs->m_fv = true;
	puvs->m_nMaxLines = 3;
	puvs->m_ws = UserWs();
	puvs->m_guid = CLSID_ChoicesListEditor;

	// If the stylesheet is missing the style for titles, substitute
	// default paragraph characters. Actually, this shouldn't happen now that we have
	// set the style to a factory style.
	StrUni stuTitleChars = L"Title Text";
	AfDbStylesheet * psts = dynamic_cast<AfDbStylesheet *>(GetStylesheet());
	AssertPtr(psts);
	ITsTextPropsPtr qttpBogus;
	HRESULT hr = psts->GetStyleRgch(stuTitleChars.Length(),
		const_cast<OLECHAR *>(stuTitleChars.Chars()), &qttpBogus);
	if (hr == S_FALSE)
		stuTitleChars.Clear();

	UserViewSpecVec vuvs;
	if (pvuvs)
		vuvs = * pvuvs;
	else
		vuvs = m_qlpi->GetDbInfo()->GetUserViewSpecs();
	ILgWritingSystemFactoryPtr qwsf;
	m_qlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);

	int iuvs = 0;
	bool fskipCustFlds = true;
	for (iuvs = 0; iuvs < vuvs.Size(); ++iuvs)
	{
		if (vuvs[iuvs]->m_vwt == kvwtDE)
		{
			fskipCustFlds = false;
			break;
		}
	}

	int wsPssl;
	// For default views we use the same list of fields for top-level items and subitems.
	// Review PM (JohnT): will we do distinct views for subitems in CLI?
	// iLev == 0 fills Major entries. iLev == 1 fills subentries.
	for (int iLev = 0; iLev < 1; ++iLev)
	{
		switch (vwt)
		{
		default:
			Assert(false);
			break;

		case kvwtBrowse:
			// Do nothing for now, but clobber some critical sections,
			// so the calling code knows it is invalid.
			puvs->m_vwt = kvwtLim;
			puvs->m_hmclevrsp.Clear();
#if 0 // ENHANCE RandB (JohnT): rework this (with appropriate fields) for real Browse view.
			// For Browse view we don't deal with subfields.
			if (iLev > 0)
				break;

			qrsp.Create();
			qrsp->Init(puvs, kclidCmPossibility, iLev, vwt, qwsf);

			//		...Fields

			// Finish it off
			IFwMetaDataCachePtr qmdc;
			qdbi->GetFwMetaDataCache(&qmdc);
			qrsp->SetMetaNames(qmdc);
#endif
			break;
		case kvwtDE:
			// These are the kinds of Possibilities we are currently prepared to handle.
			vclid.Push(kclidCmPossibility);
			vclid.Push(kclidCmPerson);
			vclid.Push(kclidCmLocation);
			vclid.Push(kclidCmAnthroItem);
			vclid.Push(kclidCmCustomItem);
#ifdef ADD_LEXTEXT_LISTS
			vclid.Push(kclidCmAnnotationDefn);
			vclid.Push(kclidLexEntryType);
			vclid.Push(kclidMoMorphType);
#endif
			for (iclid = 0; iclid < vclid.Size(); ++iclid)
			{
				qrsp.Create();
				qrsp->Init(puvs, vclid[iclid], iLev, vwt, qwsf);
				// Person and Location are assumed to be vernacular first and then analysis for name and abbreviation
				int wst = kwsVernAnals;

				// Data Entry for CmPossibility (in part).
				// The rest goes in after the subclass stuff.
				if (vclid[iclid] == kclidCmPerson)
				{
					stidLabel = kstidTlsOptPerName; // "Full Name"
					stidHelp = kstidCmPerson_Name;
				}
				else if (vclid[iclid] == kclidCmLocation)
				{
					stidLabel = kstidTlsOptLocName; // "Location Name"
					stidHelp = kstidCmLocation_Name;
				}
				else
				{
					stidLabel = kstidTlsOptClName; // "Name"
					stidHelp = kstidCmPossibility_Name;
					wst = kwsAnals;
				}
				// xx Name
				qrsp->AddField(true, stidLabel, kflidCmPossibility_Name,
					kftMta, wst, stidHelp,
					kFTVisAlways, kFTReqReq);
				// Abbreviation
				qrsp->AddField(true, kstidTlsOptClAbbreviation, kflidCmPossibility_Abbreviation,
					kftMta, wst, kstidCmPossibility_Abbreviation,
					kFTVisAlways, kFTReqReq);
				// Description
				qrsp->AddField(true, kstidTlsOptClDescription, kflidCmPossibility_Description,
					kftMsa, kwsAnals, kstidCmPossibility_Description);
				// Discussion
				if (vclid[iclid] == kclidCmPerson)
				{
					stidLabel = kstidTlsOptPerDiscussion; // "Bio Info"
					stidHelp = kstidCmPerson_Discussion;
				}
				else
				{
					stidLabel = kstidTlsOptClDiscussion; // "Discussion"
					stidHelp = kstidCmPossibility_Discussion;
				}
				qrsp->AddField(true, stidLabel, kflidCmPossibility_Discussion,
					kftStText, kwsAnal, stidHelp);
				// Restrictions
				wsPssl = GetPossListWs(CleLpInfo::kpidPsslRes);
				qrsp->AddPossField(true, kstidTlsOptERestrictions,
					kflidCmPossibility_Restrictions, kftRefSeq,
					kstidCmPossibility_Restrictions,
					vhvoPssl[CleLpInfo::kpidPsslRes], kpntName, false, false, wsPssl);
				// Confidence
				wsPssl = GetPossListWs(CleLpInfo::kpidPsslCon);
				qrsp->AddPossField(true, kstidTlsOptEConfidence,
					kflidCmPossibility_Confidence, kftRefCombo,
					kstidCmPossibility_Confidence,
					vhvoPssl[CleLpInfo::kpidPsslCon], kpntName, false, false, wsPssl);
				// Researchers
				wsPssl = GetPossListWs(CleLpInfo::kpidPsslPeo);
				qrsp->AddPossField(true, kstidTlsOptEResearchers,
					kflidCmPossibility_Researchers, kftRefSeq,
					kstidCmPossibility_Researchers,
					vhvoPssl[CleLpInfo::kpidPsslPeo], kpntName, false, false, wsPssl);
				switch (vclid[iclid])
				{
					case kclidCmPerson:
					{
						// Alias
						qrsp->AddField(true, kstidTlsOptPerAlias, kflidCmPerson_Alias,
							kftMta, kwsAnals, kstidCmPerson_Alias);
						// Gender
						qrsp->AddField(true, kstidTlsOptPerGender, kflidCmPerson_Gender,
							kftEnum, kwsAnal, kstidCmPerson_Gender);
						// DateOfBirth
						qrsp->AddField(true, kstidTlsOptPerDateOfBirth,
							kflidCmPerson_DateOfBirth,
							kftGenDate, kwsAnal, kstidCmPerson_DateOfBirth);
						// PlaceOfBirth
						wsPssl = GetPossListWs(CleLpInfo::kpidPsslLoc);
						qrsp->AddPossField(true, kstidTlsOptPerPlaceOfBirth,
							kflidCmPerson_PlaceOfBirth, kftRefAtomic,
							kstidCmPerson_PlaceOfBirth,
							vhvoPssl[CleLpInfo::kpidPsslLoc],
							kpntName, false, false, wsPssl);
						// IsResearcher
						qrsp->AddField(true, kstidTlsOptPerIsResearcher,
							kflidCmPerson_IsResearcher,
							kftEnum, kwsAnal, kstidCmPerson_IsResearcher);
						// PlacesOfResidence
						wsPssl = GetPossListWs(CleLpInfo::kpidPsslLoc);
						qrsp->AddPossField(true, kstidTlsOptPerPlacesOfResidence,
							kflidCmPerson_PlacesOfResidence, kftRefSeq,
							kstidCmPerson_PlacesOfResidence,
							vhvoPssl[CleLpInfo::kpidPsslLoc],
							kpntName, false, false, wsPssl);
						// Education
						wsPssl = GetPossListWs(CleLpInfo::kpidPsslEdu);
						qrsp->AddPossField(true, kstidTlsOptPerEducation,
							kflidCmPerson_Education, kftRefCombo,
							kstidCmPerson_Education,
							vhvoPssl[CleLpInfo::kpidPsslEdu], kpntName, false, false, wsPssl);
						// DateOfDeath
						qrsp->AddField(true, kstidTlsOptPerDateOfDeath,
							kflidCmPerson_DateOfDeath,
							kftGenDate, kwsAnal, kstidCmPerson_DateOfDeath);
						// Positions
						wsPssl = GetPossListWs(CleLpInfo::kpidPsslPsn);
						qrsp->AddPossField(true, kstidTlsOptPerPositions,
							kflidCmPerson_Positions, kftRefSeq,
							kstidCmPerson_Positions,
							vhvoPssl[CleLpInfo::kpidPsslPsn], kpntName, false, false, wsPssl);
						break;	// End of kclidCmPerson case.
					}
					case kclidCmLocation:
					{
						// Alias
						qrsp->AddField(true, kstidTlsOptLocAlias, kflidCmLocation_Alias,
							kftMta, kwsAnals, kstidCmLocation_Alias);
						break;
					}
#ifdef ADD_LEXTEXT_LISTS
					case kclidCmAnnotationDefn:
						// AllowsComment
						qrsp->AddField(true, kstidTlsOptAnnAllowsComment,
							kflidCmAnnotationDefn_AllowsComment,
							kftEnum, kwsAnal, kstidCmAnnotationDefn_AllowsComment);
						// AllowsFeatureStructure
						qrsp->AddField(true, kstidTlsOptAnnAllowsFeatStruct,
							kflidCmAnnotationDefn_AllowsFeatureStructure,
							kftEnum, kwsAnal, kstidCmAnnotationDefn_AllowsFeatureStructure);
						// AllowsInstanceOf
						qrsp->AddField(true, kstidTlsOptAnnAllowsInstOf,
							kflidCmAnnotationDefn_AllowsInstanceOf,
							kftEnum, kwsAnal, kstidCmAnnotationDefn_AllowsInstanceOf);
						// InstanceOfSignature
						qrsp->AddField(true, kstidTlsOptAnnInstOfSig,
							kflidCmAnnotationDefn_InstanceOfSignature,
							kftInteger, kwsAnal, kstidCmAnnotationDefn_InstanceOfSignature);
						// UserCanCreate
						qrsp->AddField(true, kstidTlsOptAnnUserCanCreate,
							kflidCmAnnotationDefn_UserCanCreate,
							kftEnum, kwsAnal, kstidCmAnnotationDefn_UserCanCreate);
						// CanCreateOrphan
						qrsp->AddField(true, kstidTlsOptAnnCanCreateOrphan,
							kflidCmAnnotationDefn_CanCreateOrphan,
							kftEnum, kwsAnal, kstidCmAnnotationDefn_CanCreateOrphan);
						// PromptUser
						qrsp->AddField(true, kstidTlsOptAnnPromptUser,
							kflidCmAnnotationDefn_PromptUser,
							kftEnum, kwsAnal, kstidCmAnnotationDefn_PromptUser);
						// CopyCutPastable
						qrsp->AddField(true, kstidTlsOptAnnCopyCutPastable,
							kflidCmAnnotationDefn_CopyCutPastable,
							kftEnum, kwsAnal, kstidCmAnnotationDefn_CopyCutPastable);
						// ZeroWidth
						qrsp->AddField(true, kstidTlsOptAnnZeroWidth,
							kflidCmAnnotationDefn_ZeroWidth,
							kftEnum, kwsAnal, kstidCmAnnotationDefn_ZeroWidth);
						// Multi
						qrsp->AddField(true, kstidTlsOptAnnMulti,
							kflidCmAnnotationDefn_Multi,
							kftEnum, kwsAnal, kstidCmAnnotationDefn_Multi);
						// Severity
						qrsp->AddField(true, kstidTlsOptAnnSeverity,
							kflidCmAnnotationDefn_Severity,
							kftInteger, kwsAnal, kstidCmAnnotationDefn_Severity);
						break;
					case kclidLexEntryType:
						// Type
						qrsp->AddField(true, kstidTlsOptTypType,
							kflidLexEntryType_Type,
							kftInteger, kwsAnal, kstidLexEntryType_Type);
						// ReverseAbbr
						qrsp->AddField(true, kstidTlsOptTypReverseAbbr,
							kflidLexEntryType_ReverseAbbr,
							kftInteger, kwsAnal, kstidLexEntryType_ReverseAbbr);
						break;
					case kclidMoMorphType:
						// PostFix
						qrsp->AddField(true, kstidTlsOptMorPostFix,
							kflidMoMorphType_Postfix,
							kftUnicode, kwsAnal, kstidMoMorphType_PostFix);
						// PreFix
						qrsp->AddField(true, kstidTlsOptMorPreFix,
							kflidMoMorphType_Prefix,
							kftUnicode, kwsAnal, kstidMoMorphType_PreFix);
						// SecondaryOrder
						qrsp->AddField(true, kstidTlsOptMorSecondaryOrder,
							kflidMoMorphType_SecondaryOrder,
							kftInteger, kwsAnal, kstidMoMorphType_SecondaryOrder);
						break;
#endif
					case kclidCmAnthroItem:
					case kclidCmCustomItem:
					{
						// No special properties here.
						break;
					}
				}
				// The rest of CmPossibility.
				if (!fskipCustFlds)
				{
					// Go through the RecordSpecs looking for Custom Fields.
					ClevRspMap::iterator ithmclevrspLim = vuvs[iuvs]->m_hmclevrsp.End();
					for (ClevRspMap::iterator it = vuvs[iuvs]->m_hmclevrsp.Begin();
						it != ithmclevrspLim; ++it)
					{
						RecordSpecPtr qrspDE = it.GetValue();
						AssertPtr(qrspDE);
						int cbsp = qrspDE->m_vqbsp.Size();
						for (int ibsp = 0; ibsp < cbsp; ++ibsp)
						{
							if (!qrspDE->m_vqbsp[ibsp]->m_fCustFld)
								continue;
							// This is a Custom field so add it to the new view.
							BlockSpecPtr qbspNew;
							qrspDE->m_vqbsp[ibsp]->NewCopy(&qbspNew);
							qbspNew->m_eVisibility = kFTVisAlways;
							qbspNew->m_fRequired = kFTReqNotReq;
							qrsp->m_vqbsp.Push(qbspNew);
						}
					}
				}

				// HelpId
				qrsp->AddField(true, kstidTlsOptHelpId, kflidCmPossibility_HelpId, kftUnicode,
								kwsAnal, kstidCmPossibility_HelpId, kFTVisNever);
				// DateCreated
				qrsp->AddField(true, kstidTlsOptEDateCreated, kflidCmPossibility_DateCreated,
					kftDateRO, kwsAnal, kstidCmPossibility_DateCreated, kFTVisNever);
				// DateModified
				qrsp->AddField(true, kstidTlsOptEDateModified, kflidCmPossibility_DateModified,
					kftDateRO, kwsAnal, kstidCmPossibility_DateModified, kFTVisNever);
				// Finish it off
				IFwMetaDataCachePtr qmdc;
				qdbi->GetFwMetaDataCache(&qmdc);
				qrsp->SetMetaNames(qmdc);
			}
			break;
		// end of Data Entry view

		case kvwtDoc:
			// These are the kinds of Possibilities we are currently prepared to handle.
			vclid.Push(kclidCmPossibility);
			vclid.Push(kclidCmPerson);
			vclid.Push(kclidCmLocation);
			vclid.Push(kclidCmAnthroItem);
			vclid.Push(kclidCmCustomItem);
#ifdef ADD_LEXTEXT_LISTS
			vclid.Push(kclidCmAnnotationDefn);
			vclid.Push(kclidLexEntryType);
			vclid.Push(kclidMoMorphType);
#endif
			for (iclid = 0; iclid < vclid.Size(); ++iclid)
			{
				qrsp.Create();
				qrsp->Init(puvs, vclid[iclid], iLev, vwt, qwsf);
				// Person and Location are assumed to be vernacular first and then analysis for name and abbreviation
				int wst = kwsVernAnals;

				// Document for CmPossibility (in part).
				// The rest goes in after the subclass stuff.
				if (vclid[iclid] == kclidCmPerson)
					stidLabel = kstidTlsOptPerName;
				else if (vclid[iclid] == kclidCmLocation)
					stidLabel = kstidTlsOptLocName;
				else
				{
					wst = kwsAnals;
					stidLabel = kstidTlsOptClName;
				}
				// Block
				// xx Name
				qrsp->AddField(true, stidLabel, kflidCmPossibility_Name, kftMta, wst,
					NULL, kFTVisAlways, kFTReqReq, stuTitleChars.Chars(), false, true);
				// Block
				// Abbreviation
				qrsp->AddField(true, kstidTlsOptClAbbreviation, kflidCmPossibility_Abbreviation,
					kftMta, wst, NULL,
					kFTVisAlways, kFTReqReq);
				// Block
				// Description
				qrsp->AddField(true, kstidTlsOptClDescription, kflidCmPossibility_Description,
					kftMsa, kwsAnals);
				switch (vclid[iclid])
				{
					case kclidCmPerson:
					{
						// Block
						// General Information group
						qrsp->AddField(true, kstidTlsOptEGeneralInfo, 0,
							kftGroup, kwsAnal, NULL, kFTVisNever);
						// DateOfBirth
						qrsp->AddField(false, kstidTlsOptPerDateOfBirth,
							kflidCmPerson_DateOfBirth,
							kftGenDate, kwsAnal, 0, kFTVisIfData);
						// PlaceOfBirth
						wsPssl = GetPossListWs(CleLpInfo::kpidPsslLoc);
						qrsp->AddPossField(false, kstidTlsOptPerPlaceOfBirth,
							kflidCmPerson_PlaceOfBirth, kftRefAtomic, 0,
							vhvoPssl[CleLpInfo::kpidPsslLoc], kpntName, false, false,
							wsPssl, kFTVisIfData);
						// Gender
						qrsp->AddField(false, kstidTlsOptPerGender,
							kflidCmPerson_Gender,
							kftEnum, kwsAnal, 0, kFTVisIfData);
						// DateOfDeath
						qrsp->AddField(false, kstidTlsOptPerDateOfDeath,
							kflidCmPerson_DateOfDeath,
							kftGenDate, kwsAnal, 0, kFTVisIfData);
						// PlacesOfResidence
						wsPssl = GetPossListWs(CleLpInfo::kpidPsslLoc);
						qrsp->AddPossField(false, kstidTlsOptPerPlacesOfResidence,
							kflidCmPerson_PlacesOfResidence, kftRefSeq, 0,
							vhvoPssl[CleLpInfo::kpidPsslLoc], kpntName, false, false,
							wsPssl, kFTVisIfData);
						// Education
						wsPssl = GetPossListWs(CleLpInfo::kpidPsslEdu);
						qrsp->AddPossField(false, kstidTlsOptPerEducation,
							kflidCmPerson_Education, kftRefCombo, 0,
							vhvoPssl[CleLpInfo::kpidPsslEdu], kpntName, false, false,
							wsPssl, kFTVisIfData);
						// Positions
						wsPssl = GetPossListWs(CleLpInfo::kpidPsslPsn);
						qrsp->AddPossField(false, kstidTlsOptPerPositions,
							kflidCmPerson_Positions, kftRefSeq, 0,
							vhvoPssl[CleLpInfo::kpidPsslPsn], kpntName, false, false,
							wsPssl, kFTVisIfData);
						// IsResearcher
						qrsp->AddField(false, kstidTlsOptPerIsResearcher,
							kflidCmPerson_IsResearcher,
							kftEnum, kwsAnal, 0, kFTVisNever);
						// Alias
						qrsp->AddField(false, kstidTlsOptPerAlias,
							kflidCmPerson_Alias,
							kftMta, kwsAnals, 0, kFTVisNever);
						// end of group
						break;
					}
					case kclidCmLocation:
					{
						// Block
						// Alias
						qrsp->AddField(true, kstidTlsOptLocAlias,
							kflidCmLocation_Alias, kftMta, kwsAnals, NULL, kFTVisIfData);
						break;
					}
#ifdef ADD_LEXTEXT_LISTS
					case kclidCmAnnotationDefn:
						// AllowsComment
						qrsp->AddField(false, kstidTlsOptAnnAllowsComment,
							kflidCmAnnotationDefn_AllowsComment,
							kftEnum, kwsAnal, 0, kFTVisIfData);
						// AllowsFeatureStructure
						qrsp->AddField(false, kstidTlsOptAnnAllowsFeatStruct,
							kflidCmAnnotationDefn_AllowsFeatureStructure,
							kftEnum, kwsAnal, 0, kFTVisIfData);
						// AllowsInstanceOf
						qrsp->AddField(false, kstidTlsOptAnnAllowsInstOf,
							kflidCmAnnotationDefn_AllowsInstanceOf,
							kftEnum, kwsAnal, 0, kFTVisIfData);
						// InstanceOfSignature
						qrsp->AddField(false, kstidTlsOptAnnInstOfSig,
							kflidCmAnnotationDefn_InstanceOfSignature,
							kftInteger, kwsAnal, 0, kFTVisIfData);
						// UserCanCreate
						qrsp->AddField(false, kstidTlsOptAnnUserCanCreate,
							kflidCmAnnotationDefn_UserCanCreate,
							kftEnum, kwsAnal, 0, kFTVisIfData);
						// CanCreateOrphan
						qrsp->AddField(false, kstidTlsOptAnnCanCreateOrphan,
							kflidCmAnnotationDefn_CanCreateOrphan,
							kftEnum, kwsAnal, 0, kFTVisIfData);
						// PromptUser
						qrsp->AddField(false, kstidTlsOptAnnPromptUser,
							kflidCmAnnotationDefn_PromptUser,
							kftEnum, kwsAnal, 0, kFTVisIfData);
						// CopyCutPastable
						qrsp->AddField(false, kstidTlsOptAnnCopyCutPastable,
							kflidCmAnnotationDefn_CopyCutPastable,
							kftEnum, kwsAnal, 0, kFTVisIfData);
						// ZeroWidth
						qrsp->AddField(false, kstidTlsOptAnnZeroWidth,
							kflidCmAnnotationDefn_ZeroWidth,
							kftEnum, kwsAnal, 0, kFTVisIfData);
						// Multi
						qrsp->AddField(false, kstidTlsOptAnnMulti,
							kflidCmAnnotationDefn_Multi,
							kftEnum, kwsAnal, 0, kFTVisIfData);
						// Severity
						qrsp->AddField(false, kstidTlsOptAnnSeverity,
							kflidCmAnnotationDefn_Severity,
							kftInteger, kwsAnal, 0, kFTVisIfData);
						break;
					case kclidLexEntryType:
						// ReverseAbbr
						qrsp->AddField(false, kstidTlsOptTypReverseAbbr,
							kflidLexEntryType_Type,
							kftMta, kwsAnals, 0, kFTVisIfData);
						break;
					case kclidMoMorphType:
						// PostFix
						qrsp->AddField(false, kstidTlsOptMorPostFix,
							kflidMoMorphType_Postfix,
							kftUnicode, kwsAnal, 0, kFTVisIfData);
						// PreFix
						qrsp->AddField(false, kstidTlsOptMorPreFix,
							kflidMoMorphType_Prefix,
							kftUnicode, kwsAnal, 0, kFTVisIfData);
						// SecondaryOrder
						qrsp->AddField(false, kstidTlsOptMorSecondaryOrder,
							kflidMoMorphType_SecondaryOrder,
							kftInteger, kwsAnal, 0, kFTVisIfData);
						break;
#endif
					case kclidCmAnthroItem:
					case kclidCmCustomItem:
					{
						// No special properties here.
						break;
					}
				}

				// 'Discussion'
				if (vclid[iclid] == kclidCmPerson)
					stidLabel = kstidTlsOptPerDiscussion; // Bio Info
				else
					stidLabel = kstidTlsOptClDiscussion;
				qrsp->AddField(true, stidLabel, kflidCmPossibility_Discussion,
					kftStText, kwsAnal, NULL, kFTVisIfData);
				// Other Information group containing: restrictions, confidence,...
				qrsp->AddField(true, kstidTlsOptEOtherInfo, 0,
					kftGroup, kwsAnal, NULL, kFTVisNever);
				// Restrictions
				wsPssl = GetPossListWs(CleLpInfo::kpidPsslRes);
				qrsp->AddPossField(false, kstidTlsOptERestrictions,
					kflidCmPossibility_Restrictions, kftRefSeq, 0,
					vhvoPssl[CleLpInfo::kpidPsslRes], kpntName, false, false,
					wsPssl, kFTVisNever);
				// Confidence
				wsPssl = GetPossListWs(CleLpInfo::kpidPsslCon);
				qrsp->AddPossField(false, kstidTlsOptEConfidence,
					kflidCmPossibility_Confidence, kftRefCombo, 0,
					vhvoPssl[CleLpInfo::kpidPsslCon], kpntName, false, false,
					wsPssl, kFTVisNever);
				// Researchers
				wsPssl = GetPossListWs(CleLpInfo::kpidPsslPeo);
				qrsp->AddPossField(false, kstidTlsOptEResearchers,
					kflidCmPossibility_Researchers, kftRefSeq, 0,
					vhvoPssl[CleLpInfo::kpidPsslPeo], kpntName, false, false,
					wsPssl, kFTVisNever);
				// HelpId
				qrsp->AddField(false, kstidTlsOptHelpId,
					kflidCmPossibility_HelpId,
					kftUnicode, kwsAnal, 0, kFTVisNever);

				// Now add Custom fields here.
				if (!fskipCustFlds)
				{
					// Go through the RecordSpecs looking for Custom Fields.
					ClevRspMap::iterator ithmclevrspLim = vuvs[iuvs]->m_hmclevrsp.End();
					for (ClevRspMap::iterator it = vuvs[iuvs]->m_hmclevrsp.Begin();
						it != ithmclevrspLim; ++it)
					{
						RecordSpecPtr qrspDE = it.GetValue();
						AssertPtr(qrspDE);
						int cbsp = qrspDE->m_vqbsp.Size();
						for (int ibsp = 0; ibsp < cbsp; ++ibsp)
						{
							if (!qrspDE->m_vqbsp[ibsp]->m_fCustFld)
								continue;
							// This is a Custom field so add it to the new view..
							BlockSpecPtr qbspNew;
							qrspDE->m_vqbsp[ibsp]->NewCopy(&qbspNew);
							qbspNew->m_eVisibility = kFTVisIfData;
							qbspNew->m_fRequired = kFTReqNotReq;
							qrsp->m_vqbsp.Push(qbspNew);
						}
					}
				}

				// History group
				qrsp->AddField(true, kstidTlsOptEHistory, 0,
					kftGroup, kwsAnal, NULL, kFTVisNever);
				// DateCreated
				qrsp->AddField(false, kstidTlsOptEDateCreated,
					kflidCmPossibility_DateCreated,
					kftDateRO, kwsAnal, 0, kFTVisNever);
				// DateModified
				qrsp->AddField(false, kstidTlsOptEDateModified,
					kflidCmPossibility_DateModified,
					kftDateRO, kwsAnal, 0, kFTVisNever);
				// end of group
				// Finish it off
				IFwMetaDataCachePtr qmdc;
				qdbi->GetFwMetaDataCache(&qmdc);
				qrsp->SetMetaNames(qmdc);
			}
			break;
		}
	}
}

AfRecCaptionBar * CleMainWnd::CreateCaptionBar()
{
	return NewObj CleCaptionBar(this);
}



/*----------------------------------------------------------------------------------------------
	Return true if a filter is active.
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::IsFilterActive()
{
	Set<int> sisel;
	m_qvwbrs->GetSelection(m_ivblt[kvbltFilter], sisel);
	if (sisel.Size() != 1)
		return false;
	return (*sisel.Begin() != 0); // 0 means No Filter.
}

/*----------------------------------------------------------------------------------------------
	Return true if a sort method  is active (other than the default).
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::IsSortMethodActive()
{
	Set<int> sisel;
	m_qvwbrs->GetSelection(m_ivblt[kvbltSort], sisel);
	if (sisel.Size() != 1)
		return false;
	return (*sisel.Begin() != 0);	// 0 means Default Sort Method.
}


/*----------------------------------------------------------------------------------------------
	Update the Tree View display.
	@param hvoSel the HVO of the item to show as selected. An invalid number means no selection.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::RefreshTreeView(HVO hvoSel)
{
	bool fFlat = (IsFilterActive() || IsSortMethodActive());

	::SendMessage(m_hwndTrBr, WM_SETREDRAW, false, 0);

	// We have to remove the selection first, or we get a bunch of item change notifications.
	TreeView_SelectItem(m_hwndTrBr, NULL);
	TreeView_DeleteAllItems(m_hwndTrBr);

	// Add each new item to the tree.
	FW_TVINSERTSTRUCT tvis;
	// memset needed because TssTreeView::InsertItem() is accessing one or more members
	// that are not initialized.
	memset(&tvis, 0, isizeof(tvis));
	tvis.hParent = TVI_ROOT;
	tvis.hInsertAfter = TVI_LAST;
	tvis.itemex.mask = TVIF_PARAM | TVIF_TEXT;

	int nTopHier = -1;
	Vector<HTREEITEM> vhti;
	vhti.Resize(8);
	int ilevel;
	int ilevelOld = 1;
	vhti.Push(TVI_ROOT);
	bool fFirstItem = true;
	int crecNew = m_vhcFilteredRecords.Size();
	for (int irec = 0; irec < crecNew; irec++)
	{
		PossListInfo * ppli = NULL;
		int ipii = GetPossListInfoPtr()->GetIndexFromId(m_vhcFilteredRecords[irec].hvo, &ppli);
		PossItemInfo * ppii = ppli->GetPssFromIndex(ipii);
		AssertPtr(ppii);

		int nHier = ppii->GetHierLevel();
		if (fFirstItem)
			nTopHier = nHier;

		if (fFlat)
		{
			// The level should always be 1 for a flat (i.e. filtered) list.
			ilevel = 1;
		}
		else
		{
			Assert(nHier >= nTopHier);
			ilevel = nHier;
		}
		if (ilevel > ilevelOld)
		{
			vhti.Resize(ilevel + 1);
			Assert((uint)ilevelOld < (uint)vhti.Size());
			tvis.itemex.cChildren = 1;
		}
		else
		{
			tvis.itemex.cChildren = 0;
		}

		if (!fFirstItem)
			vhti[ilevelOld] = TreeView_InsertItem(m_hwndTrBr, &tvis);

		ilevelOld = ilevel;

		ITsStringPtr qtss;
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);

		StrUni stu;
		ppii->GetName(stu, (PossNameType)m_qtrbr->GetPossNameType());
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ppii->GetWs(), &qtss);
		tvis.itemex.qtss = qtss;

		tvis.hParent = vhti[ilevel - 1];
		tvis.itemex.lParam = ppii->GetPssId();
		fFirstItem = false;
	}
	tvis.itemex.cChildren = 0;
	if (crecNew)	// Else we get a spurious blank item in an "empty" list.
		vhti[ilevelOld] = TreeView_InsertItem(m_hwndTrBr, &tvis);

	if (GetPossListInfoPtr()->GetIsSorted() && !IsSortMethodActive())
	{
		TVSORTCB sortcb;
		sortcb.hParent = NULL;
		sortcb.lpfnCompare = TssTreeView::PossListCompareFunc;
		sortcb.lParam = (LPARAM)GetPossListInfoPtr();
		TreeView_SortChildrenCB(m_hwndTrBr, &sortcb, true);

		// Read back the sorted tree into m_vhcFilteredRecords:
		HvoClsidVec vhcNew;
		ReadBackTree(m_hwndTrBr, TreeView_GetRoot(m_hwndTrBr), vhcNew, m_vhcFilteredRecords);
		m_vhcFilteredRecords = vhcNew;
	}
	// Store items in the cached list of records.
	Vector<HVO> vhvo;
	vhvo.Resize(m_vhcFilteredRecords.Size());
	for (int irecNew = 0; irecNew < crecNew; irecNew++)
		vhvo[irecNew] = m_vhcFilteredRecords[irecNew].hvo;

	m_qcvd->CacheVecProp(m_hvoFilterVec, m_flidFilteredRecords, vhvo.Begin(), vhvo.Size());

	// Find out the index to set the client windows to.
	int ihvoCurr = 0;
	for (int irecNew = 0; irecNew < crecNew; irecNew++)
	{
		if (m_vhcFilteredRecords[irecNew].hvo == hvoSel)
		{
			ihvoCurr = irecNew;
			break;
		}
	}
	SetCurRecIndex(ihvoCurr);

	::SendMessage(m_hwndTrBr, WM_SETREDRAW, true, 0);
	::InvalidateRect(m_hwndTrBr, NULL, FALSE);
	::UpdateWindow(m_hwndTrBr);
}


/*----------------------------------------------------------------------------------------------
	Recursively read TreeView control to assemble a new HvoClsidVec in the order items actually
	appear.
	@param hwndTree Handle to the TreeView control.
	@param hti Item currently being dealt with
	@param vhcNew Vector currently being built up.
	@param vhcOld Original vector, read to obtain clsid values.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::ReadBackTree(HWND hwndTree, HTREEITEM hti, HvoClsidVec &vhcNew,
	HvoClsidVec &vhcOld)
{
	if (!hti)
		return; // Recursion base case
	Assert(hwndTree);

	// Get the HVO of the current item:
	FW_TVITEM tvi;
	tvi.hItem = hti;
	tvi.mask = TVIF_PARAM;
	TreeView_GetItem(hwndTree, &tvi);
	FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
	AssertPtr(pfti);
	HVO hvo = pfti->lParam;

	// Find out which item in the old vector corresponds to this item:
	int ct;
	for (ct=0; ct < vhcOld.Size(); ct++)
	{
		if (vhcOld[ct].hvo == hvo)
		{
			vhcNew.Push(vhcOld[ct]);
			break;
		}
	}

	// Recursively add the current item's children:
	ReadBackTree(hwndTree, TreeView_GetChild(hwndTree, hti), vhcNew, vhcOld);

	// Now do the same with the next sibling:
	ReadBackTree(hwndTree, TreeView_GetNextSibling(hwndTree, hti), vhcNew, vhcOld);
}

/*----------------------------------------------------------------------------------------------
	Process Insert Event/Analysis menu item.  Switch to a Data Entry view if we aren't already
	in one.  If we are filtered, put up dialog and turn it off.	Have database create a new
	record and set hvoNew to the new ID.  et create and modified times to the new time.  If we
	have an event entry, try to set the type to Observation.  Let every top-level window that
	is showing the same language project know about the change.

	@param pcmd Ptr to menu command

	@return false
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CmdInsertEntry(Cmd * pcmd)
{
	AssertObj(pcmd);
	InsertEntry((int) pcmd->m_cid);
	return false;
}
/*----------------------------------------------------------------------------------------------
	Process Insert List item.  Switch to a Data Entry view if we aren't already	in one.  If we
	are filtered, put up dialog and turn it off. Have database create a new record and set
	hvoNew to the new ID. Set created and modified times to the new time. Let every top-level
	window that is showing the same language project know about the change.

	Inserts an Item in the list either before, after, or as a subitem.  It switches the view to
	data entry, adds the item then makes sure the label	is unique.

	@param pcnd menu command

	@return false
----------------------------------------------------------------------------------------------*/
void CleMainWnd::InsertEntry(int cmd)
{
	PossListInfoPtr qpli = GetPossListInfoPtr();

	if (!SwitchToDataEntryView(kimagDataEntry)
		|| !EnsureNoFilter() || !EnsureSafeSort())
		return;

	// Make sure the field editor is closed before adding the new item to avoid the field
	// editor is getting out of sync with the master possiblitites.
	AfClientWnd * pafcw = m_qmdic->GetCurChild();
	pafcw->PrepareToHide();

	// Save the database. Otherwise, we'll get flaky undo/redo since the current record
	// has changed.
	SaveData();

	// Now go ahead and insert the item.
	FW_TVITEM tvi;
	HVO hvoItem;
	HTREEITEM hitem;

	// Get the selected tree item.
	hitem = TreeView_GetNextItem(m_hwndTrBr, NULL, TVGN_CARET);

	int ipssNew;
	if (hitem)
	{
		// Get hvo of item
		tvi.hItem = hitem;
		tvi.mask = TVIF_PARAM;
		TreeView_GetItem(m_hwndTrBr, &tvi);
		FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
		AssertPtr(pfti);
		hvoItem = pfti->lParam;
		if (hvoItem == -1)
			ipssNew = -1;
		else
			ipssNew = qpli->GetIndexFromId(hvoItem);
	}
	else
	{
		ipssNew = -1;
	}

	StrUni stuName("");
	StrUni stuAbbr("");

	bool fOK;
	switch (cmd)
	{
	case kcidInsListItem:
	case kcidInsListItemBef:
		fOK = qpli->InsertPss(ipssNew, stuAbbr.Chars(), stuName.Chars(), kpilBefore,
			&ipssNew);
		break;
	case kcidInsListItemAft:
		fOK = qpli->InsertPss(ipssNew, stuAbbr.Chars(), stuName.Chars(), kpilAfter,
			&ipssNew);
		break;
	case kcidInsListSubItem:
		fOK = qpli->InsertPss(ipssNew, stuAbbr.Chars(), stuName.Chars(), kpilUnder,
			&ipssNew);
		break;
	}

	if (!fOK)
	{
		StrApp str(kstidCannotInsertListItem);
		::MessageBox(m_hwnd, str.Chars(), NULL, MB_OK | MB_ICONSTOP);
		return;
	}

	HVO hvoNew = qpli->GetPssFromIndex(ipssNew)->GetPssId();
	// Update everything now that we've changed.
	m_hvoTarget = hvoNew; // Show the newly inserted item.
	SyncInfo sync(ksyncAddPss, m_hvoPssl, hvoNew);
	m_qlpi->StoreAndSync(sync);
}


/*----------------------------------------------------------------------------------------------
	Refresh the tree control so it redraws all the items.

	@param pcmd Ptr to menu command

	@return true
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CmdViewTree(Cmd * pcmd)
{
	AssertPtr(pcmd);

	int pnt;
	switch (pcmd->m_cid)
	{
	case kcidViewTreeAbbrevs:
		pnt = kpntAbbreviation;
		break;
	case kcidViewTreeNames:
		pnt = kpntName;
		break;
	case kcidViewTreeBoth:
		pnt = kpntNameAndAbbrev;
		break;
	default:
		Assert(false); // Should never get here.
		break;
	}

	m_qtrbr->SetPossNameType(pnt);
	RefreshTreeView(m_vhcFilteredRecords[m_ihvoCurr].hvo);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Check the current label type for the tree.

	@param cms menu command state

	@return true
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CmsViewTree(CmdState & cms)
{
	int pnt = m_qtrbr->GetPossNameType();
	switch (cms.Cid())
	{
	case kcidViewTreeAbbrevs:
		cms.SetCheck(pnt == kpntAbbreviation);
		break;
	case kcidViewTreeNames:
		cms.SetCheck(pnt == kpntName);
		break;
	case kcidViewTreeBoth:
		cms.SetCheck(pnt == kpntNameAndAbbrev);
		break;
	default:
		Assert(false); // Should never get here.
		break;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	This method recursively searches through a tree to find the item with the given ID.
	The search starts a a given node, and searches all children or siblings after that node.

	@param hitem handle to tree item from which to start the search.  This must be a valid
		tree item.
	@param hvoPss Id of the item to be found.

	@return handle to tree item found or NULL if not found.
----------------------------------------------------------------------------------------------*/
HTREEITEM CleMainWnd::FindItemFromId(HTREEITEM hitem, HVO hvoPss)
{
	if (!hitem)
		return NULL;

	FW_TVITEM tvi;
	tvi.mask = TVIF_HANDLE | TVIF_PARAM;
	while (hitem)
	{
		tvi.hItem = hitem;
		if (TreeView_GetItem(m_hwndTrBr, &tvi))
		{
			FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
			AssertPtr(pfti);
			if (pfti->lParam == hvoPss)
				return hitem;
		}
		HTREEITEM hitemT = TreeView_GetChild(m_hwndTrBr, hitem);
		if (hitemT)
			hitemT = FindItemFromId(hitemT, hvoPss);
		if (hitemT)
			return hitemT;
		hitem = TreeView_GetNextSibling(m_hwndTrBr, hitem);
	}

	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Sets the current record index to the ID that is passed in.  This also selects this node in
	the treeView.

	@param hvoPss  id of the record that is to set to current.
----------------------------------------------------------------------------------------------*/
void CleMainWnd::SetCurRecIndex(int ihvo)
{
	SuperClass::SetCurRecIndex(ihvo);

	if (ihvo != -1 && m_vhcFilteredRecords.Size())
	{
		// We want to select the item in the tree, but this will normally fire OnTreeBarChange
		// which is used when the user clicks on an item. In this case, though, we do not want
		// OnTreeBarChange to call DispCurRec, so we use this flag to keep this from happening.
		m_fSettingRecIndex = true;
		HTREEITEM hitem = FindItemFromId(TreeView_GetRoot(m_hwndTrBr),
			m_vhcFilteredRecords[ihvo].hvo);
		if (hitem)
		{
			TreeView_SelectItem(m_hwndTrBr, hitem);
			TreeView_EnsureVisible(m_hwndTrBr, hitem);
			::InvalidateRect(m_hwndTrBr, NULL, FALSE);
			::UpdateWindow(m_hwndTrBr);
		}
		m_fSettingRecIndex = false;
	}
}


/*----------------------------------------------------------------------------------------------
	Handle the File / New Topics List command.

	@param pcmd Pointer to the menu command.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CmdFileNewTList(Cmd * pcmd)
{
	AfDbInfoPtr qdbi = m_qlpi->GetDbInfo();
	AssertPtr(qdbi);
	IOleDbEncapPtr qode;
	qdbi->GetDbAccess(&qode);
	AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
	AssertPtr(pdapp);
	HVO hvo = pdapp->NewTopicsList(qode);
	if (hvo == -1)
		return false; // TODO AListairI: give an error message.

	AfLpInfo * plpi = GetLpInfo();
	AssertPtr(plpi);

	// Start a new window.
	IFwToolPtr qtool;
	qtool.CreateInstance(CLSID_ChoicesListEditor);
	long htool;
	int pidNew;
	SmartBstr sbstrServer(qdbi->ServerName());
	SmartBstr sbstrDatabase(qdbi->DbName());

	CheckHr(qtool->NewMainWnd(sbstrServer, sbstrDatabase, plpi->GetLpId(), hvo, plpi->AnalWs(),
		0, 0, &pidNew, &htool));

	::SendMessage((HWND)htool, WM_COMMAND, kcidFilePropsTL, 0);

	CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(AfWnd::GetAfWnd((HWND)htool));
	if (pcmw)
	{
		bool fCancel;
		pcmw->CheckEmptyRecords(NULL, NULL, fCancel);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle the File / New Language Project command.

	@param pcmd Pointer to the menu command.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CmdFileNewProj(Cmd * pcmd)
{
	AssertObj(pcmd);
	// Start of using C# New Language Project dialog.
	FwCoreDlgs::ICreateLangProjectPtr qclp;
	qclp.CreateInstance("FwCoreDlgs.CreateLangProject");
	IHelpTopicProviderPtr qhtprov = new HelpTopicProvider(AfApp::Papp()->GetHelpBaseName());
	qclp->SetDialogProperties(qhtprov);
	long nRet;
	CheckHr(qclp->DisplayDialog(&nRet));
	if (nRet == kctidOk)
	{
		SmartBstr sbstrDatabase;
		SmartBstr sbstrServer;
		HVO hvoLP;
		CheckHr(qclp->GetDatabaseName(&sbstrDatabase));
		CheckHr(qclp->GetServerName(&sbstrServer));
		CheckHr(qclp->GetProjLP(&hvoLP));

		// The language project has been created.  Now open it.
		CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
		AssertPtr(pcmw);
		Rect rcT;
		::GetWindowRect(pcmw->Hwnd(), &rcT);
		int dypCaption = ::GetSystemMetrics(SM_CYCAPTION) + ::GetSystemMetrics(SM_CYSIZEFRAME);
		rcT.Offset(dypCaption, dypCaption);
		AfGfx::EnsureVisibleRect(rcT);
		WndCreateStruct wcs;
		wcs.InitMain(_T("CleMainWnd"));
		CleMainWndPtr qcmw;
		qcmw.Create();
		AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
		AssertPtr(pdapp);
		AfDbInfo * pdbi = pdapp->GetDbInfo(sbstrDatabase.Chars(), sbstrServer.Chars());
		AfLpInfo * plpi = pdbi->GetLpInfo(hvoLP);
		HVO hvoPssl = GetWeatherConditionsHvo(sbstrServer, sbstrDatabase, hvoLP);
		Assert (hvoPssl);	// Weather Condiditions list is assumed to exist.
		qcmw->SetHvoPssl(hvoPssl);
		qcmw->Init(plpi);
		qcmw->CreateHwnd(wcs);
		AfStatusBarPtr qstbr = qcmw->GetStatusBarWnd();
		Assert(qstbr);
		qstbr->InitializePanes();
		qcmw->UpdateStatusBar();
		::MoveWindow(qcmw->Hwnd(), rcT.left, rcT.top, rcT.Width(), rcT.Height(), true);
		qcmw->Show(SW_SHOWNORMAL);

		// The project is open. If the current list is empty, put up the empty list dialog.
		bool fCancel;
		qcmw->CheckEmptyRecords(NULL, NULL, fCancel);
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the File / Export command.

	@param pcmd Pointer to the menu command.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::CmdFileExport(Cmd * pcmd)
{
	AssertObj(pcmd);

	IFwExportDlgPtr qfexp;
	qfexp.CreateInstance(CLSID_FwExportDlg);
	// We need the style sheet!
	IVwStylesheetPtr qvss;
	AfStylesheet * pasts = GetStylesheet();
	AssertPtr(pasts);
	CheckHr(pasts->QueryInterface(IID_IVwStylesheet, (void **)&qvss));

	// We need the data notebook customization for exporting.
	CleCustomExportPtr qclcex;
	qclcex.Attach(NewObj CleCustomExport(m_qlpi, this));
	IFwCustomExportPtr qfcex;
	CheckHr(qclcex->QueryInterface(IID_IFwCustomExport, (void **)&qfcex));
	StrUni stuRegProgName(REGPROGNAME);
	StrUni stuHelpFile(AfApp::Papp()->GetHelpFile());
	StrUni stuHelpTopic(L"User_Interface/Menus/File/Export/Export.htm");
	CheckHr(qfexp->Initialize(reinterpret_cast<DWORD>(m_hwnd),
		qvss,
		qfcex,
		const_cast<CLSID *>(AfApp::Papp()->GetAppClsid()),
		stuRegProgName.Bstr(),
		stuHelpFile.Bstr(),
		stuHelpTopic.Bstr(),
		m_qlpi->GetLpId(),
		0,		// No possibility list class "owns" another possibility list.
		kflidCmPossibility_SubPossibilities));

	// We need the current view type.
	Set<int> sisel;
	m_qvwbrs->GetSelection(m_ivblt[kvbltView], sisel);
	Assert(sisel.Size() == 1);
	int iview = *sisel.Begin();
	Assert((UINT)iview < (UINT)m_qlpi->GetDbInfo()->GetUserViewSpecs().Size());
	UserViewSpec * puvs = m_qlpi->GetDbInfo()->GetUserViewSpecs()[iview];
	int vwt = puvs->m_vwt;
	// We need the vector of database obj ids and class ids (but split into individual vectors).
	Vector<int> vhvo;
	Vector<int> vclid;
	for (int ihc = 0; ihc < m_vhcFilteredRecords.Size(); ++ihc)
	{
		vhvo.Push(m_vhcFilteredRecords[ihc].hvo);
		vclid.Push(m_vhcFilteredRecords[ihc].clsid);
	}
	int nRet;
	CheckHr(qfexp->DoDialog(vwt, vhvo.Size(), vhvo.Begin(), vclid.Begin(), &nRet));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Reload reload the ws then call superclass for the real work.
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::FullRefresh()
{
	m_wsPssl = m_qlpi->GetPsslWsFromDb(m_hvoPssl);
	return SuperClass::FullRefresh();
}
/*----------------------------------------------------------------------------------------------
	Synchronize all windows in this application with any changes made in the database.
	@param sync -> The information describing a given change.
----------------------------------------------------------------------------------------------*/
bool CleMainWnd::Synchronize(SyncInfo & sync)
{
	switch (sync.msg)
	{
	case ksyncPageSetup:
		LoadPageSetup();
		return true;

	case ksyncMoveEntry:
	case ksyncPromoteEntry:
	case ksyncUndoRedo:
	case ksyncMergePss:
		// For merging, we know the list. For UndoRedo, we don't.
		if (sync.msg == ksyncMergePss && sync.hvo != m_hvoPssl)
			break;
		else
		{
			LoadData();
			RefreshTreeView(m_vhcFilteredRecords[m_ihvoCurr].hvo);
			// Refresh the windows since the current item may have changed (merged/deleted).
			int crncw = m_qmdic->GetChildCount();
			for (int irncw = 0; irncw < crncw; irncw++)
			// Now refresh the windows.
			{
				AfClientWnd * pafcwT = m_qmdic->GetChildFromIndex(irncw);
				AfClientRecWnd * pafcrwT = dynamic_cast<AfClientRecWnd *>(pafcwT);
				if (pafcrwT && !pafcrwT->FullRefresh())
					return false;
			}
			UpdateStatusBar();
			UpdateCaptionBar();
			return true;
		}

	case ksyncPossList:
		m_wsPssl = m_qlpi->GetPsslWsFromDb(m_hvoPssl);
	case ksyncDelPss:
	case ksyncAddPss:
		if (sync.hvo == m_hvoPssl)
		{
			// My list was changed, so we need to refresh the tree view and list of records.
			int chvoOld;
			CheckHr(m_qcvd->get_VecSize(m_hvoFilterVec, m_flidFilteredRecords, &chvoOld));
			LoadData();
			m_stuHeaderDefault.Assign(GetRootObjName()); // This doesn't seem right??
			// The dragged item should remain the selected item in the window in which it
			// was moved, but not other windows.
			HVO hvoT = 0;
			bool fMenuInsert = false; // User chose insert from some menu.
			if (m_hvoTarget)
			{
				fMenuInsert = true;
				hvoT = m_hvoTarget;
				m_hvoTarget = 0;
			}
			else if (m_vhcFilteredRecords.Size())
				hvoT = m_vhcFilteredRecords[m_ihvoCurr].hvo;
			m_qtrbr->SetPossNameType(GetPossListInfoPtr()->GetDisplayOption());
			RefreshTreeView(hvoT);

			// Go through all open main windows that use the same database and fix
			// the "Insert" toolbar buttons.
			AfDbInfo * pdbi = m_qlpi->GetDbInfo();
			AssertPtr(pdbi);
			Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
			int cqafw = vqafw.Size();
			for (int iqafw = 0; iqafw < cqafw; ++iqafw)
			{
				CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(vqafw[iqafw].Ptr());
				AssertObj(pcmw);
				if (pdbi == pcmw->GetLpInfo()->GetDbInfo())
				{
					AfToolBarPtr qtlbr = pcmw->GetToolBar(kridTBarIns);

					//  Remove all existing toolbar buttons.
					HWND hwnd = qtlbr->Hwnd();
					int cbtn = ::SendMessage(hwnd, TB_BUTTONCOUNT , 0, 0);

					for (int ibtn = cbtn - 1; ibtn > -1; ibtn--)
					{
						::SendMessage(hwnd, TB_DELETEBUTTON, (WPARAM)ibtn, 0);
					}

					// Insert all toolbar buttons
					qtlbr->Load();

					PossListInfoPtr qpli = GetPossListInfoPtr();

					// delete all unwanted toolbar buttons
					cbtn = ::SendMessage(hwnd, TB_BUTTONCOUNT , 0, 0);
					for (int ibtn = cbtn - 1; ibtn > -1; ibtn--)
					{
						TBBUTTON tbb;
						::SendMessage(hwnd, TB_GETBUTTON, ibtn, (LPARAM)&tbb);
						switch (tbb.idCommand)
						{
						case kcidInsListSubItem:
							if (qpli->GetDepth() < 2)
								::SendMessage(hwnd, TB_DELETEBUTTON, (WPARAM)ibtn, 0);
							break;
						case kcidInsListItemAft:
						case kcidInsListItemBef:
							if (qpli->GetIsSorted())
								::SendMessage(hwnd, TB_DELETEBUTTON, (WPARAM)ibtn, 0);
							break;
						case kcidInsListItem:
							if (!qpli->GetIsSorted())
								::SendMessage(hwnd, TB_DELETEBUTTON, (WPARAM)ibtn, 0);
							break;
						}
					}
				}
			}
			// Refresh the windows since the current item may have changed (merged/deleted).
			int crncw = m_qmdic->GetChildCount();
			for (int irncw = 0; irncw < crncw; irncw++)
			{
				AfClientWnd * pafcwT = m_qmdic->GetChildFromIndex(irncw);
				AfClientRecWnd * pafcrwT = dynamic_cast<AfClientRecWnd *>(pafcwT);
				if (!pafcrwT)
					continue;
				AfClientRecDeWnd * pacrw = dynamic_cast<AfClientRecDeWnd *>(pafcrwT);
				if (pacrw)
				{
					// For data entry views, we only want to completely redraw them if we
					// have changed entries (e.g., added a new item from a menu or deleted
					// an item). Otherwise we don't want to redraw everything because this
					// will cause problems. For example, a user-defined atomic reference field
					// to a people list. If you are open in a people list and then add a new
					// item (red squiggle) to this user-defined field, it will crash if the
					// old field editors are all deleted and redrawn because the old
					// field isn't completely done when the synch processing occurs which
					// tries to close the old editor which then crashes.
					if (fMenuInsert || sync.msg != ksyncAddPss && hvoT != sync.flid)
						pacrw->FullRefresh();
					continue;
				}
				// It seems this should work with Synchronize, but I (KenZ) was unable to get
				// this to work. I tried numerous things in AfClientRecVwWnd::Synchronize to
				// get things to update, but something always went wrong. Either not scrolling
				// at all, not placing the cursor in the result, or not positioning records
				// correctly. For example, if the cursor is in the Doc Tiga entry when it is
				// unsorted, after switching to sorted With File...Properties, it is ending
				// up in the Sembilan entry, and Tiga remains in its original position even
				// though the cache is properly updated and we are sending PropChanged.
				// It seems the only thing that works is a full refresh here.
				//if (pafcrwT && !pafcrwT->Synchronize(sync))
				if (!pafcrwT->FullRefresh()) // Use this for doc/browse views.
					return false;
			}
			UpdateStatusBar();
			UpdateCaptionBar();
			UpdateTitleBar(); // Title bar includes name of list which may have changed.
			return true;
		}
		break;

	default:
		break;
	}
	return SuperClass::Synchronize(sync);
}

void CleMainWnd::SetCaptionBarIcons(AfCaptionBar * pcpbr)
{
	AssertPtr(pcpbr);

	pcpbr->AddIcon(kimagTree);
	pcpbr->AddIcon(kimagDataEntry);
	pcpbr->AddIcon(kimagFilterNone);
	pcpbr->AddIcon(kimagSort);
}


//:>********************************************************************************************
//:>	CleTreeBar methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Construction
----------------------------------------------------------------------------------------------*/
CleTreeBar::CleTreeBar()
{
}


/*------------------------------------------------------------------------------------------
	delete a item from the treebar Tree control.

	@param hItem handle to tree item to be deleted.

	@return id of the record that was deleted or NULL if item not found.
------------------------------------------------------------------------------------------*/
HVO CleTreeBar::DelTreeItem(HTREEITEM hItem)
{
	HVO hvoItem;
	FW_TVITEM tvi;
	tvi.hItem = hItem;
	tvi.mask = TVIF_PARAM;
	if (TreeView_GetItem(m_hwnd, &tvi))
	{
		FwTreeItem * pfti = (FwTreeItem *)tvi.lParam;
		AssertPtr(pfti);
		hvoItem = pfti->lParam;
		TreeView_DeleteItem(m_hwnd, hItem);
	}
	else
	{
		hvoItem = NULL;
	}

	return hvoItem;
}


/*------------------------------------------------------------------------------------------
	Add an item to the treebar Tree control.

	@param hvoItem ID of record that is to be associated with the new tree item.
	@param hParent handle to tree item which is to be the parent of the new item.  If NULL
		then the root is assumed.

	@return handle to tree node that was just added.
------------------------------------------------------------------------------------------*/
HTREEITEM CleTreeBar::AddTreeItem(HVO hvoItem, HTREEITEM hParent)
{
	Assert(hvoItem);
	FW_TVITEMEX tvi;
	FW_TVINSERTSTRUCT tvins;

	static HTREEITEM hItem;

	if (!hParent)
		hParent =  TVI_ROOT;

	tvi.mask = TVIF_TEXT | TVIF_PARAM;
	tvi.lParam = hvoItem;

	PossListInfo * ppli = NULL;
	int ipss = GetPossListInfoPtr()->GetIndexFromId(hvoItem, &ppli);
	PossItemInfo * ppii = ppli->GetPssFromIndex(ipss);
	AssertPtr(ppii);
	StrUni stu;
	ppii->GetName(stu, (PossNameType)m_pnt);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), MainWindow()->UserWs(), &tvi.qtss);

	tvins.itemex = tvi;
	tvins.hInsertAfter = TVI_LAST;
	tvins.hParent = hParent;
	hItem = TreeView_InsertItem(m_hwnd, &tvins);
	if (GetPossListInfoPtr()->GetIsSorted())
	{
		TVSORTCB sortcb;
		sortcb.hParent = hParent;
		sortcb.lpfnCompare = TssTreeView::PossListCompareFunc;
		sortcb.lParam = (LPARAM)ppli;
		TreeView_SortChildrenCB(m_hwnd, &sortcb, true);
	}
	return hItem;
}


/*----------------------------------------------------------------------------------------------
	Process notifications from user.

	See ${AfWnd#OnNotifyChild}
----------------------------------------------------------------------------------------------*/
bool CleTreeBar::OnNotifyThis(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	switch (pnmh->code)
	{
	case NM_RCLICK:
		{
			Point pt;
			::GetCursorPos(&pt);

			TVHITTESTINFO tvhti;
			::GetCursorPos(&tvhti.pt);
			::ScreenToClient(m_hwnd, &tvhti.pt);

			if (TreeView_HitTest(m_hwnd, &tvhti))
			{
				if (tvhti.hItem)
				{
					// Select the item that was right clicked on
					TreeView_SelectItem(m_hwnd, tvhti.hItem);
				}
			}
			OnContextMenu(m_hwnd, pt);
			return true;
		}

// This notification is now taken care of by the TssTreeView class.
#if 0
	case TVN_GETDISPINFO:
		{
			// OLD COMMENT: Shouldn't be needed for text retrieval any more:
			Assert(false);
/*
			NMTVDISPINFO * pntvdi = (NMTVDISPINFO *)pnmh;
			Assert(pntvdi->item.mask == TVIF_TEXT);
			FwTreeItem * pfti = (FwTreeItem *)pntvdi->item.lParam;
			AssertPtr(pfti);
			HVO hvo = pfti->lParam;
			if (!hvo)
				break;	// Nothing to draw if there is no item, e.g. on empty list.
			StrUni stu;
			PossListInfo * ppli = NULL;
			int ipss = GetPossListInfoPtr()->GetIndexFromId(hvo, &ppli);
			PossItemInfo * ppii = ppli->GetPssFromIndex(ipss);
			AssertPtr(ppii);
			ppii->GetName(stu, (PossNameType)m_pnt);

			StrAnsi sta(stu);
			strncpy(pntvdi->item.pszText, sta.Chars(), pntvdi->item.cchTextMax);
			*/
		}
		break;
#endif

	case TVN_BEGINDRAG:
		{ // Begin block
			// Only allow drag and drop if no filter or sort method is active:
			CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
			AssertPtr(pcmw);
			if (!pcmw->IsFilterActive() && !pcmw->IsSortMethodActive())
				return m_pplddDragDrop->BeginDrag(pnmh);
		} // End block
		break;
	case TVN_SELCHANGED:
		HTREEITEM hItem = ((NMTREEVIEW *)pnmh)->itemNew.hItem;
		if (hItem != NULL)
		{
			FwTreeItem * pfti = (FwTreeItem *)(((NMTREEVIEW *)pnmh)->itemNew.lParam);
			AssertPtr(pfti);
			HVO hvoItem = (HVO)pfti->lParam;
			RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
			AssertObj(prmw);
			// When deleting all items to redraw, we don't want to process
			// a tree bar change since it will try to DispCurRec when things are
			// in a bad state, resulting in various Asserts.
			if (!m_fNoUpdate)
				prmw->OnTreeBarChange(hItem, hvoItem);
		}
		break;
	}

	return SuperClass::OnNotifyThis(ctidFrom, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.

	See ${AfWnd#FWndProcPre}
----------------------------------------------------------------------------------------------*/
bool CleTreeBar::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_ERASEBKGND:
		return true;
	case WM_SETFOCUS:
		{
			CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
			AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(
				pcmw->GetMdiClientWnd()->GetCurChild());
			::SetFocus(qafcrw->Hwnd());
			return true;
		}
	case WM_DESTROY:
		{
			// We don't want screen updates happening when we are deleting items.
			// Otherwise we end up with crashes in RnCustDocVc::Display and other
			// places where it tries to display the current record when the cache
			// and other things are in a broken state. This especially happened with
			// two lists open in doc view and F5 was pressed.
			m_fNoUpdate = true;
			bool fRet = SuperClass::FWndProc(wm, wp, lp, lnRet);
			m_fNoUpdate = false;
		return fRet;
		}
	default:
		return SuperClass::FWndProc(wm, wp, lp, lnRet);
	}
}


/*----------------------------------------------------------------------------------------------
	Create and show the context menu for the tree bar.

	@param hwnd handle to menu owner window
	@param pt point to position the menu at

	@return true
----------------------------------------------------------------------------------------------*/
bool CleTreeBar::OnContextMenu(HWND hwnd, Point pt)
{
	HMENU hmenuPopup = ::CreatePopupMenu();

	StrApp str;
	if (GetPossListInfoPtr()->GetIsSorted())
	{
		AfUtil::GetResourceStr(krstItem, kcidTrBarInsert, str);
		::AppendMenu(hmenuPopup, MF_STRING, kcidTrBarInsert, str.Chars());
	}
	else
	{
		AfUtil::GetResourceStr(krstItem, kcidTrBarInsertBef, str);
		::AppendMenu(hmenuPopup, MF_STRING, kcidTrBarInsertBef, str.Chars());
		AfUtil::GetResourceStr(krstItem, kcidTrBarInsertAft, str);
		::AppendMenu(hmenuPopup, MF_STRING, kcidTrBarInsertAft, str.Chars());
	}

	if (GetPossListInfoPtr()->GetDepth() > 1)
	{
		AfUtil::GetResourceStr(krstItem, kcidTrBarInsertSub, str);
		::AppendMenu(hmenuPopup, MF_STRING, kcidTrBarInsertSub, str.Chars());
	}

	AfUtil::GetResourceStr(krstItem, kcidTrBarMerge, str);
	::AppendMenu(hmenuPopup, MF_STRING, kcidTrBarMerge, str.Chars());
	AfUtil::GetResourceStr(krstItem, kcidTrBarDelete, str);
	::AppendMenu(hmenuPopup, MF_STRING, kcidTrBarDelete, str.Chars());
	::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
	AfUtil::GetResourceStr(krstItem, kcidViewTreeAbbrevs, str);
	::AppendMenu(hmenuPopup, MF_STRING, kcidViewTreeAbbrevs, str.Chars());
	AfUtil::GetResourceStr(krstItem, kcidViewTreeNames, str);
	::AppendMenu(hmenuPopup, MF_STRING, kcidViewTreeNames, str.Chars());
	AfUtil::GetResourceStr(krstItem, kcidViewTreeBoth, str);
	::AppendMenu(hmenuPopup, MF_STRING, kcidViewTreeBoth, str.Chars());
	::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
	AfUtil::GetResourceStr(krstItem, kcidHideVBar, str);
	::AppendMenu(hmenuPopup, MF_STRING, kcidHideVBar, str.Chars());

	TrackPopupWithHelp(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y,
		MainWindow()->UserWs());

	::DestroyMenu(hmenuPopup);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Tree bar pop up menu item selected.

	@param pcmd menu command

	@return true
----------------------------------------------------------------------------------------------*/
bool CleTreeBar::CmdTrbMenu(Cmd * pcmd)
{
	AssertObj(pcmd);

	switch (pcmd->m_cid)
	{
	case kcidViewTreeAbbrevs:
		m_pnt = kpntAbbreviation;
		break;
	case kcidViewTreeNames:
		m_pnt = kpntName;
		break;
	case kcidViewTreeBoth:
		m_pnt = kpntNameAndAbbrev;
		break;
	}

	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertObj(prmw);
	prmw->OnTreeMenuChange(pcmd);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Set the state of the tree bar menu item.

	@param cms menu command state

	@return true
----------------------------------------------------------------------------------------------*/
bool CleTreeBar::CmsTrbMenu(CmdState & cms)
{
	switch (cms.Cid())
	{
	case kcidViewTreeAbbrevs:
		cms.SetCheck(m_pnt == kpntAbbreviation);
		break;
	case kcidViewTreeNames:
		cms.SetCheck(m_pnt == kpntName);
		break;
	case kcidViewTreeBoth:
		cms.SetCheck(m_pnt == kpntNameAndAbbrev);
		break;
	case kcidTrBarInsert:
	case kcidTrBarInsertBef:
	case kcidTrBarInsertAft:
	case kcidTrBarInsertSub:
	case kcidTrBarDelete:
	case kcidTrBarMerge:
		cms.Enable(true);
		break;
	}
	return true;
}


//:>********************************************************************************************
//:>	CleDbInfo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CleDbInfo::CleDbInfo()
{
}


/*----------------------------------------------------------------------------------------------
	Return the AfLpInfo from the cache corresponding to the language project ID passed in. If
	it has not been created yet, create it now.

	@param hvoLp language project ID

	@return language project info
----------------------------------------------------------------------------------------------*/
AfLpInfo * CleDbInfo::GetLpInfo(HVO hvoLp)
{
	int clpi = m_vlpi.Size();
	for (int ilpi = 0; ilpi < clpi; ilpi++)
	{
		if (hvoLp == m_vlpi[ilpi]->GetLpId())
			return m_vlpi[ilpi];
	}

	// We didn't find it in the cache, so create it now.
	CleLpInfoPtr qlpi;
	qlpi.Create();
	qlpi->Init(this, hvoLp);
	qlpi->OpenProject();
	m_vlpi.Push((AfLpInfo *)qlpi.Ptr());
	return qlpi;
}


/*----------------------------------------------------------------------------------------------
	For Browse views we have one RecordSpec that is saved in the database. After instantiating
	that RecordSpec, we call this method to generate RecordSpecs that are used for loading
	data from the database. In here we add a RecordSpec for every concrete class needed to
	handle the BlockSpecs, adding partially filled BlockSpecs for each field to the appropriate
	RecordSpecs. We are only storing top-level RecordSpecs at this point. If we need to display
	subentries as well, then we'll need to add additional RecordSpecs for subentries.

	@param puvs Out Ptr to user view spec.
----------------------------------------------------------------------------------------------*/
void CleDbInfo::CompleteBrowseRecordSpec(UserViewSpec * puvs)
{
	AssertPtr(puvs);
	AssertPtr(m_qmdc);
	// The caller should have initialized the UserViewSpec with one RecordSpec.
	Assert(puvs->m_hmclevrsp.Size() == 1);

	RecordSpecPtr qrsp; // The display RecordSpec passed in to this function.
	ClsLevel clev;
	clev.m_nLevel = 0; // We are only reading top-level information.
	clev.m_clsid = 0; // This is the dummy display RecordSpec.
	puvs->m_hmclevrsp.Retrieve(clev, qrsp);
	AssertPtr(qrsp);

	RecordSpecPtr qrspPss;

	// Create the RecordSpec used for loading data and add them to the UserViewSpec.
	clev.m_clsid = kclidCmPossibility;
	qrspPss.Attach(NewObj RecordSpec(kclidCmPossibility, 0));
	qrspPss->m_fNoSave = true;
	puvs->m_hmclevrsp.Insert(clev, qrspPss, true);

	// Now go through the BlockSpecs for the view and add appropiate items to our
	// load RecordSpecs.
	for (int ibsp = 0; ibsp < qrsp->m_vqbsp.Size(); ++ibsp)
	{
		BlockSpecPtr qbsp = qrsp->m_vqbsp[ibsp];
		AssertPtr(qbsp);

		// Store the new class in the right RecordSpec(s).
		ulong clsid;
		CheckHr(m_qmdc->GetOwnClsId(qbsp->m_flid, &clsid));
		switch (clsid)
		{
		case kclidCmPossibility:
			qrspPss->m_vqbsp.Push(qbsp);
			break;
		default:
			Assert(false);
			break;
		}
	}

	// Since we are using existing BlockSpecs that have had their metanames already set,
	// we don't need to set them here.
}


//:>********************************************************************************************
//:>	CleLpInfo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CleLpInfo::CleLpInfo()
{
	// The ProjIds before kpidPsslLim are defined by the system (i.e. not user-definable), so
	// make sure we have enough room to store those.
	m_vhvoPsslIds.Resize(kpidPsslLim);
}


/*----------------------------------------------------------------------------------------------
	Open a language project.
	@return true if successful
----------------------------------------------------------------------------------------------*/
bool CleLpInfo::OpenProject()
{
	if (!LoadWritingSystems())
		return false;

	// Load the project ids and names.
	if (!LoadProjBasics())
		return false;

	// Load the stylesheet for this language project.
	m_qdsts.Attach(NewObj AfDbStylesheet);
	m_qdsts->Init(this, m_hvoLp);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Load basic information for the project (ids, names).
	@return true if successful
----------------------------------------------------------------------------------------------*/
bool CleLpInfo::LoadProjBasics()
{
	// Set up the PrjIds array of ids for this project.
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stu;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	OLECHAR rgchProjName[MAX_PATH];
	m_vhvoPsslIds.Clear(); // Clear any old values.
	m_vhvoPsslIds.Resize(kpidPsslLim);

	//  Obtain pointer to IOleDbEncap interface and execute the given SQL select command.
	AssertPtr(m_qdbi);
	m_qdbi->GetDbAccess(&qode);

	// Get the language project name.
	try
	{
		stu.Format(L"exec GetOrderedMultiTxt '%d', %d",
			m_hvoLp, kflidCmProject_Name);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		Assert(fMoreRows); // This proc should always return something.
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchProjName),
			MAX_PATH * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));

		m_stuPrjName = rgchProjName;
	}
	catch (...)
	{
		return false;
	}

	try
	{
		stu.Format(
		L"select con.dst con, res.dst res, peo.dst peo, "
		L"loc.dst loc, ana.dst ana, edu.dst edu, psn.dst psn "
		L"from LangProject lp "
		L"left outer join LangProject_ConfidenceLevels con "
			L"on con.src = lp.id "
		L"left outer join LangProject_Restrictions res "
			L"on res.src = lp.id "
		L"left outer join LangProject_People peo on peo.src = lp.id "
		L"left outer join LangProject_Locations loc on loc.src = lp.id "
		L"left outer join LangProject_AnalysisStatus ana "
			L"on ana.src = lp.id "
		L"left outer join LangProject_Education edu on edu.src = lp.id "
		L"left outer join LangProject_Positions psn on psn.src = lp.id "
		L"where lp.id = %d", m_hvoLp);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslCon]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslRes]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslPeo]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslLoc]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslAna]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(6, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslEdu]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(7, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslPsn]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
		}
		else
		{
			// If we don't have any rows, we used a wrong language project id (m_hvoLp),
			// or one or more essential possibility lists are missing,
			// so return and let the user open a project from the Debug menu (for now).
			Assert(false);
			return false;
		}

		// Load any custom lists that there are.
		if (!LoadCustomLists())
			return false;
	}
	catch (...)
	{
		return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Store the given sync info in the database if there is more than one connection,
	and synchronize the current application with the desired change.
	@param sync The Sync information to store describing a given change.
----------------------------------------------------------------------------------------------*/
bool CleLpInfo::StoreAndSync(SyncInfo & sync)
{
	StoreSync(sync);
	AfDbApp * papp = dynamic_cast<AfDbApp *>(AfApp::Papp());
	if (papp)
		return papp->Synchronize(sync, this);
	return true;
}


//:>********************************************************************************************
//:>	CleChangeWatcher methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CleChangeWatcher::CleChangeWatcher(ISilDataAccess * psda)
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
	m_qsda = psda;
	psda->AddNotification(this);

}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CleChangeWatcher::~CleChangeWatcher()
{
	m_qsda->RemoveNotification(this);
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP CleChangeWatcher::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwNotifyChange)
		*ppv = static_cast<IVwNotifyChange *>(this);
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


/*----------------------------------------------------------------------------------------------
	Pass it on to the appropriate notifier(s), if any.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CleChangeWatcher::PropChanged(HVO vwobj, PropTag tag, int ivMin, int cvIns,
	int cvDel)
{
	BEGIN_COM_METHOD

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwNotifyChange)
}


//:>********************************************************************************************
//:>	CleFilterNoMatchDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Launches the Tools-Options dialog with the Filter tab selected.

	@param Out pptod pointer to Tools-Options dialog
----------------------------------------------------------------------------------------------*/
void CleFilterNoMatchDlg::GetTlsOptDlg(TlsOptDlg ** pptod)
{
	AssertPtr(pptod);

	// Launch the Tools-Options dialog with the Filter tab selected.
	CleTlsOptDlgPtr qctod;
	qctod.Create();

	TlsDlgValue tgv;
	tgv.itabInitial = CleTlsOptDlg::kidlgFilters;
	tgv.clsid = 0;
	tgv.nLevel = 0;
	tgv.iv1 = m_iflt;
	tgv.iv2 = 0;

	qctod->SetDialogValues(tgv);
	*pptod = qctod.Detach();
}


//:>********************************************************************************************
//:>	CleListBar methods.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Bring up the Tools Option dialog.

	@param pcmd ptr to menu command.(This parameter is not used in in this method.)

	@return true.
----------------------------------------------------------------------------------------------*/
bool CleListBar::CmdToolsOpts(Cmd * pcmd)
{
	AssertObj(pcmd);

	CleTlsOptDlgPtr qctod;
	qctod.Create();
	// Get a pointer to the current MainWnd which should be a RecMainWnd.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(AfApp::Papp()->GetCurMainWnd());
	Assert(prmw);

	TlsDlgValue tgv;
	tgv.clsid = 0;
	tgv.nLevel = 0;
	tgv.iv2 = 0;

	switch (pcmd->m_cid)
	{
	default:
		tgv.iv1 = 0;
		tgv.itabInitial = 0;
		break;
	case kcidViewViewsConfig:
		tgv.iv1 = m_qvwbrs->GetContextSelection(prmw->GetViewbarListIndex(kvbltView));
		tgv.itabInitial = CleTlsOptDlg::kidlgViews;
		break;
	case kcidViewFltrsConfig:
		// Subtract one because of the No Filter item.
		tgv.iv1 = m_qvwbrs->GetContextSelection(prmw->GetViewbarListIndex(kvbltFilter)) - 1;
		tgv.itabInitial = CleTlsOptDlg::kidlgFilters;
		break;
	case kcidViewSortsConfig:
		// Subtract one because of the Default Sort item.
		tgv.iv1 = m_qvwbrs->GetContextSelection(prmw->GetViewbarListIndex(kvbltSort)) - 1;
		tgv.itabInitial = CleTlsOptDlg::kidlgSortMethods;
		break;
	}

	// Close editors and save window information; run the dialog; restore windows.
	prmw->RunTlsOptDlg(qctod, tgv);

	// Moved from TlsOptDlg::SaveFilterValues() to prevent massive use of
	// "GDI Object" and "User Object" resources.
	Vector<int> vifltNew = qctod->GetNewFilterIndexes();
	Vector<AfViewBarShell *> vpvwbrs = qctod->GetFilterViewBars();
	Assert(vifltNew.Size() == vpvwbrs.Size());
	qctod.Clear();
	if (vifltNew.Size())
	{
		Set<int> sisel;
		int iwnd;
		int cwnd = vifltNew.Size();
		for (iwnd = 0; iwnd < cwnd; iwnd++)
		{
			if (vpvwbrs[iwnd])
			{
				sisel.Clear();
				sisel.Insert(vifltNew[iwnd]);
				vpvwbrs[iwnd]->SetSelection(prmw->GetViewbarListIndex(kvbltFilter), sisel);
			}
		}
	}

	return true;
}


//:>********************************************************************************************
//:>	CleCaptionBar methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CleCaptionBar::CleCaptionBar(RecMainWnd * prmwMain)
	: AfRecCaptionBar(prmwMain)
{
}


/*----------------------------------------------------------------------------------------------
	Create and show the appropriate context menu based on the icon that was right-clicked on.

	@param ibtn captionbar index for the button
	@param pt point at which to place the menu
----------------------------------------------------------------------------------------------*/
void CleCaptionBar::ShowContextMenu(int ibtn, Point pt)
{
	HMENU hmenuPopup = ::CreatePopupMenu();
	StrApp str(kstidConfigureVBar);
	int ivblt = m_prmwMain->GetViewbarListType(ibtn);

	switch (ivblt)
	{
	case kvbltTree:
		::AppendMenu(hmenuPopup, MF_STRING, kcidViewTreeAbbrevs, _T("Show &Abbreviations"));
		::AppendMenu(hmenuPopup, MF_STRING, kcidViewTreeNames, _T("Show &Names"));
		::AppendMenu(hmenuPopup, MF_STRING, kcidViewTreeBoth, _T("Sho&w Abbreviations and Names"));
		break;
	case kvbltView:
		::AppendMenu(hmenuPopup, MF_STRING, kcidExpViews, NULL);
		::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
		::AppendMenu(hmenuPopup, MF_STRING, kcidViewViewsConfig, str.Chars());
		break;
	case kvbltFilter:
		::AppendMenu(hmenuPopup, MF_STRING, kcidExpFilters, NULL);
		::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
		::AppendMenu(hmenuPopup, MF_STRING, kcidViewFltrsConfig, str.Chars());
		break;
	case kvbltSort:
		::AppendMenu(hmenuPopup, MF_STRING, kcidExpSortMethods, NULL);
		::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
		::AppendMenu(hmenuPopup, MF_STRING, kcidViewSortsConfig, str.Chars());
		break;
	default:
		Assert(false); // Should never happen.
		break;
	}

	::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y, 0,
		m_prmwMain->Hwnd(), NULL);

	::DestroyMenu(hmenuPopup);
}


//:>********************************************************************************************
//:>	CleEmptyListDlg Implementation.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool CleEmptyListDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Display the "Information" icon.
	HICON hicon = ::LoadIcon(NULL, MAKEINTRESOURCE(IDI_INFORMATION));
	if (hicon)
	{
		::SendMessage(::GetDlgItem(m_hwnd, kridEmptyListIcon), STM_SETICON, (WPARAM)hicon,
			(LPARAM)0);
	}

	// Set the font for the header, and display the header.
	m_hfontLarge = AfGdi::CreateFont(16, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
		::SendMessage(::GetDlgItem(m_hwnd, kridEmptyListHeader), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	StrApp strFmt(kstidEmptyListHeaderFmt);
	StrApp str;
	str.Format(strFmt.Chars(), m_strProj.Chars());
	::SetWindowText(::GetDlgItem(m_hwnd, kridEmptyListHeader), str.Chars());
	::EnableWindow(::GetDlgItem(m_hwnd, kctidEmptyLImport), false);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool CleEmptyListDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctidFrom)
		{
		case kctidEmptyLNewItem:
		case kctidEmptyLImport:
		case kctidCancel:
			::EndDialog(m_hwnd, ctidFrom);
			return true;
		}
	}
	return false;
}

//:>********************************************************************************************
//:>	Debug methods.
//:>********************************************************************************************

#ifdef DEBUG
/*----------------------------------------------------------------------------------------------
	Convert a numeric notification code to its ASCII symbolic name.

	@param nc numeric notification code

	@return ASCII symbolic name
----------------------------------------------------------------------------------------------*/
const char * NotificationCode(int nc)
{
	static const char * aszNotificationCodes[] = {
		"CBN_ERRSPACE",			// -1
		"CBN_SELCHANGE",		//  1
		"CBN_DBLCLK",			//  2
		"CBN_SETFOCUS",			//  3
		"CBN_KILLFOCUS",		//  4
		"CBN_EDITCHANGE",		//  5
		"CBN_EDITUPDATE",		//  6
		"CBN_DROPDOWN",			//  7
		"CBN_CLOSEUP",			//  8
		"CBN_SELENDOK",			//  9
		"CBN_SELENDCANCEL"		// 10
	};
	static char szBuf[20];
	if (nc == -1)
		return aszNotificationCodes[0];
	if (nc > 0 && nc <= 10)
		return aszNotificationCodes[nc];
	sprintf_s(szBuf, "%d (?)", nc);
	return szBuf;
}


/*----------------------------------------------------------------------------------------------
	Convert a command id code to its ASCII symbolic name.

	@param cid command id code

	@return ASCII symbolic name
----------------------------------------------------------------------------------------------*/
const char * CidCode(int cid)
{
	switch (cid)
	{
	case kcidFileExit:				return "kcidFileExit";				break;
	case kcidHelpAbout:				return "kcidHelpAbout";				break;
	case kcidWndCascad:				return "kcidWndCascad";				break;
	case kcidWndTile:				return "kcidWndTile";				break;
	case kcidWndSideBy:				return "kcidWndSideBy";				break;
	case kcidViewStatBar:			return "kcidViewStatBar";			break;
	case kcidFilePageSetup:			return "kcidFilePageSetup";			break;
	case kcidToolsOpts:				return "kcidToolsOpts";				break;
	case kcidFmtPara:				return "kcidFmtPara";				break;
	case kcidFmtBdr:				return "kcidFmtBdr";				break;
	case kcidFmtFnt:				return "kcidFmtFnt";				break;
	case kcidFmtBulNum:				return "kcidFmtBulNum";				break;
	case kcidFmtStyles:				return "kcidFmtStyles";				break;
	case kcidFmttbStyle:			return "kcidFmttbStyle";			break;
	case kcidFmttbWrtgSys:			return "kcidFmttbWrtgSys";			break;
	case kcidFmttbFnt:				return "kcidFmttbFnt";				break;
	case kcidFmttbFntSize:			return "kcidFmttbFntSize";			break;
	case kcidFmttbBold:				return "kcidFmttbBold";				break;
	case kcidFmttbItal:				return "kcidFmttbItal";				break;
	case kcidFmttbAlignLeft:		return "kcidFmttbAlignLeft";		break;
	case kcidFmttbAlignCntr:		return "kcidFmttbAlignCntr";		break;
	case kcidFmttbAlignRight:		return "kcidFmttbAlignRight";		break;
	case kcidFmttbLstNum:			return "kcidFmttbLstNum";			break;
	case kcidFmttbLstBullet:		return "kcidFmttbLstBullet";		break;
	case kcidFmttbUnind:			return "kcidFmttbUnind";			break;
	case kcidFmttbInd:				return "kcidFmttbInd";				break;
	case kcidFmttbApplyBdr:			return "kcidFmttbApplyBdr";			break;
	case kcidFmttbApplyBgrndColor:	return "kcidFmttbApplyBgrndColor";	break;
	case kcidFmttbApplyFgrndColor:	return "kcidFmttbApplyFgrndColor";	break;
	case kcidDataFirst:				return "kcidDataFirst";				break;
	case kcidDataLast:				return "kcidDataLast";				break;
	case kcidDataNext:				return "kcidDataNext";				break;
	case kcidDataPrev:				return "kcidDataPrev";				break;
	case kcidHelpWhatsThis:			return "kcidHelpWhatsThis";			break;
	case kcidHelpConts:				return "kcidHelpConts";				break;
	case kcidWndNew:				return "kcidWndNew";				break;
	case kcidViewVbar:				return "kcidViewVbar";				break;
	}
	static char szBuf[20];
	sprintf_s(szBuf, "%d (?)", cid);
	return szBuf;
}
#endif

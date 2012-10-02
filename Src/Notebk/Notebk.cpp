/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2000, SIL International. All rights reserved.

File: NoteBk.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	This file provides the base for Research Notebook functions.  It contains class
	definitions for the following classes:

		RnDbInfo : AfDbInfo
		RnLpInfo : AfLpInfo
		RnApp : AfDbApp
		RnMainWnd : RecMainWnd
		RnOverlayListBar : AfOverlayListBar
		RnTagOverlayTool : AfTagOverlayTool
		RnListBar : AfListBar
		RnCaptionBar : AfCaptionBar
		RnFilterNoMatchDlg : FwFilterNoMatchDlg
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
// This only works if it is AFTER the #pragma above.
#import "CSharpLinker.tlb" raw_interfaces_only rename_namespace("Cls")

#undef THIS_FILE
DEFINE_THIS_FILE

#import "FwCoreDlgs.tlb" raw_interfaces_only rename_namespace("FwCoreDlgs")

#undef LOG_FILTER_SQL
//#define LOG_FILTER_SQL 1
#ifdef LOG_FILTER_SQL
// Making this a static global avoids conditional method argument.
static FILE * fp = NULL;
#endif
//:End Ignore

// Create one global instance. It has to exist before WinMain is called.
RnApp g_app;

#ifdef JohnT_3_13_02_ATL7
// If using ATL 7 we need something like this...but more, because it won't work unless
// the Module is initialized with a suitable type library. Currently we have fudged
// things to use ATL 3 (the VS 6 version).
// If you try again you also need a line like this:
//#define _ATL_SINGLE_THREADED
// probably in main.h or similar; otherwise instantiating the DummyModule initializes
// COM multithreaded which conflicts with the way ModuleEntry tries to initialize it.
// We need one global instance of this to make ATL (e.g., used to host the web browser in
// the choices list chooser) work.

class CAtlDummyModule : public CAtlExeModuleT< CAtlDummyModule >
{
};

CAtlDummyModule s_atlmod;
#endif

BEGIN_CMD_MAP(RnApp)
	ON_CID_ALL(kcidFileOpen, &RnApp::CmdFileOpenProj, NULL)
	ON_CID_ALL(kcidFileBackup, &RnApp::CmdFileBackup, NULL)
	ON_CID_ALL(kcidFileExit, &RnApp::CmdFileExit, NULL)
	ON_CID_ALL(kcidHelpAbout, &RnApp::CmdHelpAbout, NULL)
	ON_CID_ALL(kcidWndCascad, &RnApp::CmdWndCascade, NULL)
	ON_CID_ALL(kcidWndTile, &RnApp::CmdWndTileHoriz, NULL)
	ON_CID_ALL(kcidWndSideBy, &RnApp::CmdWndTileVert, NULL)
END_CMD_MAP_NIL()


/*----------------------------------------------------------------------------------------------
	The Research Notebook command map for the main window.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(RnMainWnd)
	ON_CID_GEN(kcidViewStatBar, &RnMainWnd::CmdSbToggle, &RnMainWnd::CmsSbUpdate)
	ON_CID_ME(kcidToolsOpts, &RecMainWnd::CmdToolsOpts, NULL)

	ON_CID_ALL(kcidFileNewLP, &RnMainWnd::CmdFileNewProj, NULL)
	ON_CID_ALL(kcidFilePropsDN, &RnMainWnd::CmdFileDnProps, NULL)
	ON_CID_ALL(kcidFilePropsLP, &RecMainWnd::CmdFileProjProps, NULL)
	ON_CID_ALL(kcidFileSave, &RnMainWnd::CmdFileSave, &RnMainWnd::CmsFileSave)
	ON_CID_ALL(kcidFileImpt, &RnMainWnd::CmdFileImport, NULL)
	ON_CID_ALL(kcidFileExpt, &RnMainWnd::CmdFileExport, &RnMainWnd::CmsFileExport)

	ON_CID_ALL(kcidEditDel, &RnMainWnd::CmdDelete, &RecMainWnd::CmsHaveRecord)
	ON_CID_ALL(kcidEditUndo, &RecMainWnd::CmdEditUndo, &RecMainWnd::CmsEditUndo)
	ON_CID_ALL(kcidEditRedo, &RecMainWnd::CmdEditRedo, &RecMainWnd::CmsEditRedo)

	ON_CID_ME(kcidDataFirst, &RecMainWnd::CmdRecSel, &RecMainWnd::CmsRecSelUpdate)
	ON_CID_ME(kcidDataLast, &RecMainWnd::CmdRecSel, &RecMainWnd::CmsRecSelUpdate)
	ON_CID_ME(kcidDataNext, &RecMainWnd::CmdRecSel, &RecMainWnd::CmsRecSelUpdate)
	ON_CID_ME(kcidDataPrev, &RecMainWnd::CmdRecSel, &RecMainWnd::CmsRecSelUpdate)

	ON_CID_ME(kcidHelpConts,     &RnMainWnd::CmdHelpFw,   NULL)
	ON_CID_ME(kcidHelpApp,       &RnMainWnd::CmdHelpApp,  NULL)
	ON_CID_ME(kcidHelpWhatsThis, &RnMainWnd::CmdHelpMode, NULL)
	ON_CID_ME(kcidHelpStudentManual, &RnMainWnd::CmdTraining, NULL)
	ON_CID_ME(kcidHelpExercises,     &RnMainWnd::CmdTraining, NULL)
	ON_CID_ME(kcidHelpInstructorGuide, &RnMainWnd::CmdTraining, NULL)

	ON_CID_ME(kcidWndNew, &RnMainWnd::CmdWndNew, NULL)
	ON_CID_ME(kcidWndSplit, &RnMainWnd::CmdWndSplit, &RnMainWnd::CmsWndSplit)
	ON_CID_ME(kcidViewVbar, &RnMainWnd::CmdVbToggle, &RnMainWnd::CmsVbUpdate)
	ON_CID_ME(kcidViewSbar, &RnMainWnd::CmdSbToggle, &RnMainWnd::CmsSbUpdate)
	ON_CID_ME(kcidViewViewsConfig, &RecMainWnd::CmdToolsOpts, NULL)
	ON_CID_ME(kcidViewFltrsConfig, &RecMainWnd::CmdToolsOpts, NULL)
	ON_CID_ME(kcidViewSortsConfig, &RecMainWnd::CmdToolsOpts, NULL)
	ON_CID_ME(kcidViewOlaysConfig, &RecMainWnd::CmdToolsOpts, NULL)

	ON_CID_CHILD(kcidExpViews, &RnMainWnd::CmdViewExpMenu, &RnMainWnd::CmsViewExpMenu)
	ON_CID_CHILD(kcidExpFilters, &RnMainWnd::CmdViewExpMenu, &RnMainWnd::CmsViewExpMenu)
	ON_CID_CHILD(kcidExpSortMethods, &RnMainWnd::CmdViewExpMenu, &RnMainWnd::CmsViewExpMenu)
	ON_CID_CHILD(kcidExpOverlays, &RnMainWnd::CmdViewExpMenu, &RnMainWnd::CmsViewExpMenu)

	ON_CID_ME(kcidInsEntryEvent, &RnMainWnd::CmdInsertEntry, NULL)
	ON_CID_ME(kcidInsEntryAnal, &RnMainWnd::CmdInsertEntry, NULL)
	ON_CID_ME(kcidToolsLists, &RecMainWnd::CmdTlsLists, NULL)
	ON_CID_ME(kcidStats, &RecMainWnd::CmdStats, NULL)
	ON_CID_CHILD(kcidViewFullWindow, &RecMainWnd::CmdViewFullWindow,
		&RecMainWnd::CmsViewFullWindow)

	ON_CID_ME(kcidExtLinkOpen, &RecMainWnd::CmdExternalLink, NULL)
	ON_CID_ME(kcidExtLinkOpenWith, &RecMainWnd::CmdExternalLink, NULL)
	ON_CID_ME(kcidExtLink, &RecMainWnd::CmdExternalLink, &AfMainWnd::CmsInsertLink)
	ON_CID_ME(kcidExtLinkRemove, &RecMainWnd::CmdExternalLink, NULL)
	ON_CID_ME(kcidExtLinkUrl, &RecMainWnd::CmdExternalLink, &AfMainWnd::CmsInsertLink)
END_CMD_MAP_NIL()


/*----------------------------------------------------------------------------------------------
	The command map for our RN generic list bar window.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(RnListBar)
	ON_CID_GEN(kcidViewViewsConfig, &RnListBar::CmdToolsOpts, NULL)
	ON_CID_GEN(kcidViewFltrsConfig, &RnListBar::CmdToolsOpts, NULL)
	ON_CID_GEN(kcidViewSortsConfig, &RnListBar::CmdToolsOpts, NULL)
END_CMD_MAP_NIL()


/*----------------------------------------------------------------------------------------------
	The command map for our RN overlay list bar window.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(RnOverlayListBar)
	ON_CID_GEN(kcidViewOlaysConfig, &RnOverlayListBar::CmdToolsOpts, NULL)
END_CMD_MAP_NIL()


//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance of AfFwTool with CoCreateInstance,
//:>	so as to start up new main windows from another EXE (e.g., the Explorer).
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Notebook"),
	&CLSID_ResearchNotebook,
	_T("SIL Research Notebook"),
	_T("Apartment"),
	&AfFwTool::CreateCom);


//:>********************************************************************************************
//:>	RnApp methods.
//:>********************************************************************************************

#define REGPROGNAME _T("Data Notebook")

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnApp::RnApp()
{
	s_fws.SetRoot(REGPROGNAME);//"Software\\SIL\\FieldWorks\\Data Notebook"

	AfApp::Papp()->SetHelpBaseName(_T("\\Helps\\FieldWorks_Data_Notebook_Help.chm"));
}

// Test function.
int getBreak(const OLECHAR * pchw, BreakIterator * pbrkit, int ich)
{
	pbrkit->setText(pchw);
	return pbrkit->following(ich);
}

/*----------------------------------------------------------------------------------------------
	Initialize the application. Display the splash screen, open initial window, connect to
	database, initialize status bar, initialize panes, remove splash screen.
----------------------------------------------------------------------------------------------*/
void RnApp::Init(void)
{
/*	TEST OF ICU ACCESSIBILITY: THESE TEST LINES SHOULD COMPILE WHEN UNCOMMENTED.
	UnicodeString unis(L"ICU string");
	StrUni stu(L"StrUni string");
	StrUni stu1(unis.getBuffer(), unis.length());
	UnicodeString unis1(stu.Chars(), stu.Length());
	stu1.Assign(unis.getBuffer(), unis.length());
	unis1.setTo(stu.Chars(), stu.Length());


// Test of ICU line breaking for PUA surrogate pairs.
	BreakIterator * pbrkit;

	try
	{
		const char rgchLang[3] = {'e', 'n', 0};
		const char rgchCountry[3] = {'G', 'B', 0};
		Locale loc(rgchLang, rgchCountry);
		u_setDataDirectory("c:\\work\\fw\\distfiles\\icu");
		UErrorCode uec = U_ZERO_ERROR;
		pbrkit = BreakIterator::createLineInstance(loc, uec);
		const OLECHAR * pch = L"( mmm ) nnn";
		const OLECHAR * pch1 = L"\xdb80\xdc0f mmm \xdb80\xdc10 nnn"; // Parens are plane 15.
		int ichBreak, ichBreak1, chp, chp1;
		wchar chHigh, chLow;
		ToSurrogate(0xF000F, &chHigh, &chLow);	// Just to check on the conversion.
		if (U_SUCCESS(uec))
		{
			chp = u_getIntPropertyValue('(', UCHAR_LINE_BREAK);
			chp = u_getIntPropertyValue(')', UCHAR_LINE_BREAK);
			chp = u_getIntPropertyValue(0xF000F, UCHAR_LINE_BREAK);	// Check that the right...
			chp1 = u_getIntPropertyValue(0xF0010, UCHAR_LINE_BREAK); // ...properties are there.
			ichBreak = getBreak(pch, pbrkit, 0);
			ichBreak1 = getBreak(pch1, pbrkit, 0);
			StrApp str(_T("\nLine Breaking Tests\n"));
			::OutputDebugString(str.Chars());
			str.Format(_T("Plane 0 break expected at 8, actually at %d%n"), ichBreak);
			::OutputDebugString(str.Chars());
			str.Format(_T("Plane 15 break expected at 10, actually at %d%n"), ichBreak1);
			::OutputDebugString(str.Chars());
			ichBreak = getBreak(pch, pbrkit, ichBreak);
			ichBreak1 = getBreak(pch1, pbrkit, ichBreak1);
			ichBreak1 = getBreak(pch1, pbrkit, ichBreak1);
			ichBreak1 = getBreak(pch1, pbrkit, ichBreak1); // Get further break points.
		}
	}
	catch (...)
	{
	}
	if (pbrkit)
		delete pbrkit;
*/
	// Call init before any other database activity.
	SuperClass::Init();

	AfWnd::RegisterClass(_T("RnMainWnd"), 0, 0, 0, COLOR_3DFACE, (int)kridNoteBkIcon);

	// If we are starting as a server, we don't open a window.
	if (HasEmbedding())
		return;

	StrUni stuProjTableName(L"LangProject");
	HashMapStrUni<StrUni> hmsustuOptions;
	HVO hvoLpId = 0;
	HVO hvoNbkId = 0;
	StrUni stuServer;
	StrUni stuDatabase;

	OptionsReturnValue orv = ProcessDBOptions(kclidRnResearchNbk, NULL,
		kclidLangProject, &stuProjTableName, &hmsustuOptions, hvoLpId, hvoNbkId, true);
	int ctid = ProcessOptRetVal(orv, &hmsustuOptions, true);

	bool fLoop = true;
	while (fLoop)
	{
		fLoop = false;

		bool bContinue = true;
		bool bFileOpened = false;
		while (bContinue)
		{
			switch (ctid)	// ctid is the id of the button clicked in PrjNotFndDlg.
			{
			case IDOK:
				bContinue = false;
				break;		// No problem, continue with the found project.
			case kctidPrjNotFndOpen:
				{
					// User chose to open a project of their choice.
					// Execute the File-Open dialog.
					FileOpenProjectInfoPtr qfopi;
					qfopi.Attach(DoFileOpenProject());
					if (!qfopi)
						break;
					hvoNbkId = NbkIdFromLpId(qfopi->m_hvoProj, qfopi->m_stuMachine,
						qfopi->m_stuDatabase);
					if (!hvoNbkId)
						break;	// There was no Data Notebook, so go through the loop again.
					stuDatabase.Assign(qfopi->m_stuDatabase);
					stuServer.Assign(qfopi->m_stuMachine);
					hvoLpId = qfopi->m_hvoProj;
					bContinue = false;
					bFileOpened = true;
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

						// Get the hvo of the Data Notebook.
						hvoNbkId = NbkIdFromLpId(hvoLpId, stuServer, stuDatabase);
						if (!hvoNbkId)
						{
							Assert(false);	// Creation wizard should always make a Notebook.
							break;
						}
						bContinue = false;
						bFileOpened = true;
					}
					else if (nRet == -1)
					{
						DWORD dwError = ::GetLastError();
						achar rgchMsg[MAX_PATH+1];
						DWORD cch = ::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, NULL, dwError,
							0, rgchMsg, MAX_PATH, NULL);
						rgchMsg[cch] = 0;
						StrApp strTitle(kstidWizProjMsgCaption);
						::MessageBox(m_hwnd, rgchMsg, strTitle.Chars(), MB_OK | MB_ICONWARNING);
					}
				}
				break;
			case kctidPrjNotFndRestore:
				{ // Begin block
					DIFwBackupDbPtr qzbkup;
					qzbkup.CreateInstance(CLSID_FwBackup);
					qzbkup->Init(this, 0);
					int nUserAction;
					qzbkup->UserConfigure(NULL, (ComBool)true, &nUserAction);
					qzbkup->Close();
					if (nUserAction == 4) // kRestoreOk - successful Restore
						return;
				} // End block
				break;
			case kctidPrjNotFndExit:
			default:	// Fallthrough.	E.g. when 'x' is used to close the dialog.
				AfApp::Papp()->Quit(true);
				return;
			}

			if (bContinue)
				ctid = ProcessOptRetVal(orv, &hmsustuOptions, true);
		}
		if (!bFileOpened)
		{
			// Fish the server and database names from the map.
			StrUni stuKey;
			stuKey = L"server";
			hmsustuOptions.Retrieve(stuKey, &stuServer);
			stuKey = L"database";
			hmsustuOptions.Retrieve(stuKey, &stuDatabase);
		}

		AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
		Assert(pdapp);
		if (!pdapp->CheckDbVerCompatibility(stuServer.Chars(), stuDatabase.Chars()))
		{
			// The user did not want to update the old Db so allow all options.
			AfPrjNotFndDlgPtr qpnf;
			qpnf.Create();
			qpnf->SetProject(_T(""));
			ctid = qpnf->DoModal(NULL);
			fLoop = true;
		}
	}

	IOleDbEncapPtr qode;
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(stuServer.Bstr(), stuDatabase.Bstr(), m_qfistLog, koltMsgBox,
		koltvForever));
	int wsUser = UserWs(qode);
	// Close the connection before opening the application since we only want one connection
	// open to avoid various problems with backup, synchronization, etc.
	qode.Clear();

	// Start up the application.
	Assert(hvoLpId);
	Assert (hvoNbkId);
	IFwToolPtr qtool;
	qtool.CreateInstance(CLSID_ResearchNotebook);
	long htool;
	int pidNew;
	CheckHr(qtool->NewMainWnd(stuServer.Bstr(), stuDatabase.Bstr(),
		hvoLpId, hvoNbkId, wsUser, 0, 0, &pidNew, &htool));
	// Split the status bar into five sections of varying widths:
	//	1. record id
	//	2. progress / info
	//	3. sort
	//	4. filter
	//	5. count
	RnMainWnd * prnmw = dynamic_cast<RnMainWnd *>(m_qafwCur.Ptr());
	// m_qafwCur is not set if we latched onto another app, but this initialization will
	// already be done.
	bool fCloseApp = false;
	if (prnmw)
	{
		AfDbInfo * pdbi = GetDbInfo(stuDatabase.Chars(), stuServer.Chars());
		AfLpInfo * plpi = pdbi->GetLpInfo(hvoLpId);
		AfStatusBarPtr qstbr = prnmw->GetStatusBarWnd();
		Assert(qstbr);
		qstbr->InitializePanes();
		prnmw->UpdateStatusBar();
		StrUni stuProjName(plpi->PrjName());
		pdbi = prnmw->CheckEmptyRecords(pdbi, plpi->PrjName(), fCloseApp);
		pdbi->CheckTransactionKludge();
	}
	if (!fCloseApp)
	{
		// Check if a backup is overdue.
		HWND hwnd = NULL;
		if (prnmw)
			hwnd = prnmw->Hwnd();
		DIFwBackupDbPtr qzbkup;
		qzbkup.CreateInstance(CLSID_FwBackup);
		qzbkup->Init(this, (int)hwnd);
		IHelpTopicProviderPtr qhtprov = new HelpTopicProvider(m_strHelpBaseName.Chars());
		qzbkup->CheckForMissedSchedules(qhtprov);
		qzbkup->Close();
	}
}

/*----------------------------------------------------------------------------------------------
	Gets the Data Notebook Id from the Project Id.

	@param LpId In Hvo of the Project.
	@param stuServer In Computer to be accessed.
	@param stuDatabase In Database to be accessed.

	@return Hvo of the Notebook, or 0 if none found.
----------------------------------------------------------------------------------------------*/
HVO RnApp::NbkIdFromLpId(HVO hvoLpId, const StrUni& stuServer, const StrUni& stuDatabase)
{
	StrUni stuSqlStmt;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	HVO hvoNbkId = 0;
	int nVer = 0;
	try
	{
		// Get version number
		stuSqlStmt = L"select DbVer from version$";
		qode.CreateInstance(CLSID_OleDbEncap);
		CheckHr(qode->Init(stuServer.Bstr(), stuDatabase.Bstr(), m_qfistLog, koltMsgBox,
			koltvForever));
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&nVer), sizeof(int),
				&cbSpaceTaken, &fIsNull, 0));
		}
		// Get Data Notebook id. We can get here with an unmigrated database when we
		// open it from the Poject Not Found dialog.
		StrUni stuLP = nVer <=200202 ? L"LanguageProject" : L"LangProject";
		stuSqlStmt.Format(
			L"select Dst from %s_ResearchNotebook "
			L"where Src = %d", stuLP.Chars(), hvoLpId);
		CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoNbkId),
				isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
		}
	}
	catch (...)
	{
		return 0;
	}
	return hvoNbkId;
}
/*----------------------------------------------------------------------------------------------
	Gets the App database Version information from the app.

	@param nAppVer Out Application Version
	@param nErlyVer Out The earliest database version compatible with this App Version.
	@param nLastVer Out The last known database version compatible with this App version.

	@return always false.
----------------------------------------------------------------------------------------------*/
bool RnApp::GetAppVer(int & nAppVer, int & nErlyVer, int & nLastVer)
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
int RnApp::GetAppNameId()
{
	return kstidAppName;
}
/*----------------------------------------------------------------------------------------------
	Gets the resource id of the App name to be used in the Propeties Dialog from the app.

	@return resource id
----------------------------------------------------------------------------------------------*/
int RnApp::GetAppPropNameId()
{
	return kstidResearchNotebook;
}

/*----------------------------------------------------------------------------------------------
	Gets the resource id of the Application icon from the app.

	@return resource id
----------------------------------------------------------------------------------------------*/
int RnApp::GetAppIconId()
{
	return kridNoteBkIcon;
}

/*----------------------------------------------------------------------------------------------
	Gets the default backup directory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnApp::GetDefaultBackupDirectory(BSTR * pbstrDefBackupDir)
{
	BEGIN_COM_METHOD;

	// TODO: return the default backup directory (My Documents/My FieldWorks/Backups)
	*pbstrDefBackupDir = NULL;

	END_COM_METHOD(g_fact, IID_IBackupDelegates);
}

/*----------------------------------------------------------------------------------------------
	Return the matching AfDbInfo. If it is not already loaded, create it now.

	@param pszDbName Db name
	@param pszSvrName Server name
	@return Smart pointer to Db Info object
----------------------------------------------------------------------------------------------*/
AfDbInfo * RnApp::GetDbInfo(const OLECHAR * pszDbName, const OLECHAR * pszSvrName)
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
	RnDbInfoPtr qdbi;
	qdbi.Create();
	qdbi->Init(pszSvrName, pszDbName, m_qfistLog);
	m_vdbi.Push((AfDbInfo *)qdbi.Ptr());
	return qdbi;
}

/*----------------------------------------------------------------------------------------------
	Create a new main window, and set up the wcs.

	@param wcs The struct that is used by the caller to set up the window more.

	@return Pointer to a new RecMainWnd.
----------------------------------------------------------------------------------------------*/
RecMainWnd * RnApp::CreateMainWnd(WndCreateStruct & wcs, FileOpenProjectInfo * pfopi)
{
	wcs.InitMain(_T("RnMainWnd"));
	RnMainWndPtr qrnmw;
	qrnmw.Create();
	RecMainWnd * prmw = qrnmw;
	qrnmw.Detach();
	return prmw;
}


/*----------------------------------------------------------------------------------------------
	Open a new main window on the specified data.
	Temporarily, fail if we have a window open and it is a different database. Later, we
	will be able to handle multiple databases.

	See ${AfDbApp#NewMainWnd}
----------------------------------------------------------------------------------------------*/
void RnApp::NewMainWnd(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
	int hvoMainObj, int encUi, int nTool, int nParam, DWORD dwRegister)
{
	RnMainWndPtr qrnmw;
	qrnmw.Create();
	SuperClass::NewMainWnd(bstrServerName, bstrDbName, hvoLangProj, hvoMainObj, encUi, nTool,
		nParam, qrnmw, _T("RnMainWnd"), kridSplashStartMessage, dwRegister);
}


/*----------------------------------------------------------------------------------------------
	Open a new main window on the specified data using a specified view and starting field.
	Temporarily, fail if we have a window open and it is a different database. Later, we
	will be able to handle multiple databases.

	See ${AfDbApp#NewMainWndWithSel}
----------------------------------------------------------------------------------------------*/
void RnApp::NewMainWndWithSel(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
	int hvoMainObj, int encUi, int nTool, int nParam, const HVO * prghvo, int chvo,
		const int * prgflid, int cflid, int ichCur, int nView, DWORD dwRegister)
{
	RnMainWndPtr qrnmw;
	qrnmw.Create();
	qrnmw->SetStartupInfo(prghvo, chvo, prgflid, cflid, ichCur, nView);
	SuperClass::NewMainWnd(bstrServerName, bstrDbName, hvoLangProj, hvoMainObj, encUi, nTool,
		nParam, qrnmw, _T("RnMainWnd"), kridSplashStartMessage, dwRegister);
}

/*----------------------------------------------------------------------------------------------
	Re-open a new main window on the specified data. This is intended to be used after a Restore
	operation, so that the user is not left with nothing after his windows were shut down prior
	to the restore. It can also be used after other major operations such as replacing all
	encodings.
----------------------------------------------------------------------------------------------*/
void RnApp::ReopenDbAndOneWindow(const OLECHAR * pszDbName, const OLECHAR * pszSvrName,
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

	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(hr = qode->Init(stuServerName.Bstr(), stuDatabase.Bstr(), m_qfistLog,
		koltMsgBox, koltvForever));
	CheckHr(hr = qode->CreateCommand(&qodc));

	stuSql = L"select top 1 [owner$], [id] from [RnResearchNbk_]";

	CheckHr(hr = qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(hr = qodc->GetRowset(0));
	CheckHr(hr = qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		HVO hvoLp = 0;
		HVO hvoMain = 0;
		CheckHr(hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoLp), isizeof(hvoLp),
			&luSpaceTaken, &fIsNull, 0));
		CheckHr(hr = qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hvoMain), isizeof(hvoMain),
			&luSpaceTaken, &fIsNull, 0));

		int ws = UserWs(qode);
		long htool;
		int pidNew;
		IFwToolPtr qtool;
		qtool.CreateInstance(CLSID_ResearchNotebook);
		CheckHr(qtool->NewMainWnd(stuServerName.Bstr(), stuDatabase.Bstr(),  hvoLp, hvoMain, ws,
			0, // tool-dependent identifier of which tool to use; does nothing for RN yet
			0, // another tool-dependend parameter, does nothing in RN yet
			&pidNew,
			&htool)); // value you can pass to CloseMainWnd if you want.
	}
}


//:>********************************************************************************************
//:>	RnMainWnd methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnMainWnd::RnMainWnd()
{
	m_dxpDeTreeWidth = 116; // Default tree width if nothing comes from registry.

	m_ivblt[kvbltView] = 0;
	m_ivblt[kvbltFilter] = 1;
	m_ivblt[kvbltSort] = 2;
	m_ivblt[kvbltOverlay] = 3;
	m_ivbltMax = m_ivblt[kvbltOverlay];
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
RnMainWnd::~RnMainWnd()
{
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void RnMainWnd::MakeJumpWindow(Vector<HVO> & vhvo, Vector<int> & vflid, int nView)
{
	WndCreateStruct wcs;
	wcs.InitMain(_T("RnMainWnd"));

	PrepareNewWindowLocation();

	RnMainWndPtr qrnmw;
	qrnmw.Create();
	qrnmw->Init(m_qlpi);
	qrnmw->SetStartupInfo(vhvo.Begin(), vhvo.Size(), vflid.Begin(), vflid.Size(), 0, nView);
	qrnmw->CreateHwnd(wcs);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwCustDocVc * RnMainWnd::CreateCustDocVc(UserViewSpec * puvs)
{
	return NewObj RnCustDocVc(puvs, m_qlpi);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
AfSplitChild * RnMainWnd::CreateNewSplitChild()
{
	if (m_vwt == kvwtDE)
		return NewObj RnDeSplitChild();
	if (m_vwt == kvwtDoc)
		return NewObj RnDocSplitChild();
	if (m_vwt == kvwtBrowse)
		return NewObj RnBrowseSplitChild();

	return SuperClass::CreateNewSplitChild();
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwCustBrowseVc *  RnMainWnd::CreateCustBrowseVc(UserViewSpec * puvs,
											int dypHeader, int nMaxLines, HVO hvoRootObjId)
{
	RnCustBrowseVc*  pvc =  NewObj RnCustBrowseVc(puvs, m_qlpi, dypHeader, nMaxLines, hvoRootObjId);
	pvc->SetSortKeyInfo(&m_vskhSortKeys);

	Set<int> sisel;
	m_qvwbrs->GetSelection(m_ivblt[kvbltSort], sisel);
	if (sisel.Size())
	{
		AfDbInfo * pdbi = m_qlpi->GetDbInfo();
		Assert(sisel.Size() == 1);
		int isort = pdbi->ComputeSortIndex(*sisel.Begin(), GetRecordClid());
		if (isort >= 0)
		{
			AppSortInfo & asi = pdbi->GetSortInfo(isort);
			Vector<int> vflid;
			SortMethodUtil::ParseFieldPath(asi.m_stuPrimaryField, vflid);
			if (vflid.Size() > 0)
				pvc->SetPrimarySortKeyFlid(vflid[1]); // 1st element is clid, 2nd is flid
		}
	}

	return pvc;
}

/*----------------------------------------------------------------------------------------------
	Check the database/project for being empty.  If it is, prompt the user for what to do (add
	a record, import a Shoebox file, or exit).

	@param pdbi Pointer to the application's AfDbInfo object for this main window.
	@param stuProject Default name of the language project.
	@param fCancel Output flag to cancel loading this database/project.

	@return pointer to a valid AfDbInfo object after fixing up the database -- the input object
		may be invalidated.
----------------------------------------------------------------------------------------------*/
AfDbInfo * RnMainWnd::CheckEmptyRecords(AfDbInfo * pdbi, StrUni stuProject, bool & fCancel)
{
	CheckAnthroList();		// Check first that the anthropology categories list exists.

	AfDbInfo * pdbiRet = pdbi;
	fCancel = false;
	if (!RawRecordCount())
	{
		// Empty database.
		RnEmptyNotebookDlgPtr qremp;
		qremp.Create();
		StrApp strProj(m_qlpi->PrjName());
		if (!strProj.Length())
			strProj.Assign(stuProject.Chars());
		qremp->SetProject(strProj.Chars());
		AfApp::Papp()->EnableMainWindows(false);
		int ctid = qremp->DoModal(Hwnd());
		AfApp::Papp()->EnableMainWindows(true);
		switch (ctid)
		{
		case kctidEmptyNewEvent:
			::SendMessage(Hwnd(), WM_COMMAND, kcidInsEntryEvent, 0);
			break;
		case kctidEmptyNewAnalysis:
			::SendMessage(Hwnd(), WM_COMMAND, kcidInsEntryAnal, 0);
			break;
		case kctidEmptyImport:
			{
				StrUni stuDatabase(pdbi->DbName());
				StrUni stuServer(pdbi->ServerName());
				::SendMessage(Hwnd(), WM_COMMAND, kcidFileImpt, 0);
				// Importing destroys and rebuilds the database connection, so get it again.
				// Even pdbi itself is invalid at this point!
				AfDbApp * pdbap = dynamic_cast<AfDbApp *>(AfApp::Papp());
				AssertPtr(pdbap);
				pdbiRet = pdbap->GetDbInfo(stuDatabase.Chars(), stuServer.Chars());
			}
			break;
		case kctidCancel:
			::SendMessage(Hwnd(), WM_COMMAND, kcidFileExit, 0);
			fCancel = true;
			break;
		}
	}
	return pdbiRet;
}


/*----------------------------------------------------------------------------------------------
	Get the drag text for this object. This will be used to follow the mouse during a drag
	and also will be pasted into a text if dropped into a text location. Assuming entries,
	we show the type of entry, title, and date.
	@param hvo The object we plan to drag.
	@param clid The class of the object we plan to drag.
	@param pptss Pointer in which to return the drag text.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::GetDragText(HVO hvo, int clid, ITsString ** pptss)
{
	Assert(hvo);
	Assert(clid);
	AssertPtr(pptss);
	*pptss = NULL;
	try
	{
		// Make sure the data we want is loaded.
		IDbColSpecPtr qdcs;
		StrUni stuSql;
		AssertPtr(m_qcvd);
		bool fLoaded = false;
		HVO hvoOwn;
		int64 ntim;
		ITsStringPtr qtss;
		CheckHr(m_qcvd->get_ObjectProp(hvo, kflidCmObject_Owner, &hvoOwn));
		if (hvoOwn)
		{
			CheckHr(m_qcvd->get_TimeProp(hvo, kflidRnGenericRec_DateCreated, &ntim));
			if (ntim)
			{
				CheckHr(m_qcvd->get_StringProp(hvo, kflidRnGenericRec_Title, &qtss));
				if (qtss)
				{
					int cch;
					CheckHr(qtss->get_Length(&cch));
					if (cch)
						fLoaded = true;
				}
			}
		}

		if (!fLoaded)
		{
			// If any field is missing from the cache, load everything.
			stuSql.Format(L"select id, Owner$, DateCreated, Title, Title_Fmt "
				L"from RnGenericRec_ "
				L"where id = %d", hvo);
			qdcs.CreateInstance(CLSID_DbColSpec);
			CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
			CheckHr(qdcs->Push(koctObj, 1, kflidCmObject_Owner, 0));
			CheckHr(qdcs->Push(koctTime, 1, kflidRnGenericRec_DateCreated, 0));
			CheckHr(qdcs->Push(koctString, 1, kflidRnGenericRec_Title, 0));
			CheckHr(qdcs->Push(koctFmt, 1, kflidRnGenericRec_Title, 0));

			// Execute the query and store results in the cache.
			CheckHr(m_qcvd->Load(stuSql.Bstr(), qdcs, hvo, 0, NULL, NULL));
			// Get the results.
			CheckHr(m_qcvd->get_ObjectProp(hvo, kflidCmObject_Owner, &hvoOwn));
			CheckHr(m_qcvd->get_TimeProp(hvo, kflidRnGenericRec_DateCreated, &ntim));
			CheckHr(m_qcvd->get_StringProp(hvo, kflidRnGenericRec_Title, &qtss));
		}

		int ws = UserWs();
		RnLpInfo * plpi = dynamic_cast<RnLpInfo *>(m_qlpi.Ptr());
		AssertPtr(plpi);

		// Now construct the string we want to return.
		int stid;
		if (clid == kclidRnEvent)
		{
			if (plpi->GetRnId() == hvoOwn)
				stid = kstidEvent;
			else
				stid = kstidSubevent;
		}
		else if (clid == kclidRnAnalysis)
		{
			if (plpi->GetRnId() == hvoOwn)
				stid = kstidAnalysis;
			else
				stid = kstidSubanalysis;
		}
		else
		{
			// For any other class, use the default for now.
			SuperClass::GetDragText(hvo, clid, pptss);
			return;
		}
		StrUni stu(stid);
		StrUni stuSep(kstidSpHyphenSp);

		ITsIncStrBldrPtr qtisb;
		qtisb.CreateInstance(CLSID_TsIncStrBldr);
		CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
		CheckHr(qtisb->AppendRgch(stu.Chars(), stu.Length()));
		CheckHr(qtisb->AppendRgch(stuSep.Chars(), stuSep.Length()));
		CheckHr(qtisb->AppendTsString(qtss));
		CheckHr(qtisb->AppendRgch(stuSep.Chars(), stuSep.Length()));
		// Leave the date blank if a date doesn't exist.
		if (ntim)
		{
			// Convert the date to a system date.
			SilTime tim = ntim;
			SYSTEMTIME stim;
			stim.wYear = (unsigned short) tim.Year();
			stim.wMonth = (unsigned short) tim.Month();
			stim.wDay = (unsigned short) tim.Date();

			// Then format it to a time based on the current user locale.
			achar rgchDate[50]; // Tuesday, August 15, 2000   mardi 15 août 2000
			::GetDateFormat(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &stim, NULL, rgchDate, 50);
			stu = rgchDate;
			CheckHr(qtisb->AppendRgch(stu.Chars(), stu.Length()));
		}
		CheckHr(qtisb->GetString(pptss));
	}
	catch (...)
	{
		Assert(false);
	}
}


/*----------------------------------------------------------------------------------------------
	Load settings specific to this window from the registry. Load the viewbar, status bar, and
	tool bars settings.  Get the last data record that was showing. Load the window position.
	Set the last overlay, filter, sort order, and the last view according to the registry.

	@param pszRoot The string that is used for this app as the root for all registry entries.
	@param fRecursive If true, then every child window will be load their settings.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::LoadSettings(const achar * pszRoot, bool fRecursive)
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
			| kmskShowLargeOverlayIcons
			| kmskShowViewBar;
	}

	m_qvwbrs->ChangeIconSize(m_ivblt[kvbltView], dwViewbarFlags & kmskShowLargeViewIcons);
	m_qvwbrs->ChangeIconSize(m_ivblt[kvbltFilter], dwViewbarFlags & kmskShowLargeFilterIcons);
	m_qvwbrs->ChangeIconSize(m_ivblt[kvbltSort], dwViewbarFlags & kmskShowLargeSortIcons);
	m_qvwbrs->ChangeIconSize(m_ivblt[kvbltOverlay], dwViewbarFlags & kmskShowLargeOverlayIcons);

	if (!pfws->GetDword(pszRoot, _T("LastViewBarGroup"), &dwT))
		dwT = m_ivblt[kvbltView];
	m_qvwbrs->SetCurrentList(Min((uint)dwT, (uint)(m_ivbltMax)));
	m_qvwbrs->ShowViewBar(dwViewbarFlags & kmskShowViewBar);

	// Display the main window without client information.
	::ShowWindow(m_hwnd, SW_SHOW);
	OnIdle();
	::UpdateWindow(m_hwnd);

	Set<int> sisel;

	// NOTE: Make sure this is done after the main window is shown, so the overlay tool windows
	// don't show up before the main window does.
	if (!pfws->GetDword(pszRoot, _T("Open Overlays"), &dwT))
		dwT = 1; // No Overlay.
	sisel.Clear();
	if (dwT & 1)
	{
		// If the No Overlay item is selected, no other overlay should be selected.
		int iovr = 0;
		sisel.Insert(iovr);
	}
	else
	{
		int covr = m_qlpi->GetOverlayCount();
		for (int iovr = 1; iovr <= covr; iovr++)
		{
			if (dwT & (1 << iovr))
				sisel.Insert(iovr);
		}
	}
	m_qvwbrs->SetSelection(m_ivblt[kvbltOverlay], sisel);

	// Figure out which sort method, if any, should be selected.
	// Use the default sort method if we don't get a valid item from the registry, or if
	// we are opening the window on a specific item.
	int csrt = m_qlpi->GetDbInfo()->GetSortCount();
	if (!pfws->GetDword(pszRoot, _T("LastSort"), &dwT) || (uint)dwT > (uint)csrt || fPath)
	{
		dwT = 0; // Default Sort.
	}
	sisel.Clear();
	int isort = (int)dwT;
	sisel.Insert(isort);
	m_qvwbrs->SetSelection(m_ivblt[kvbltSort], sisel);

	// NOTE: Make sure this is done after the main window is shown, so if the last filter
	// needs a prompt, it won't show up before the main window does.
	int cflt = m_qlpi->GetDbInfo()->GetFilterCount();
	// Filters are disabled if we don't get a valid item from the registry or if
	// we are opening the window on a specific item.
	if (!pfws->GetDword(pszRoot, _T("LastFilter"), &dwT) || (uint)dwT > (uint)cflt ||
		fPath)
	{
		dwT = 0; // No Filter.
	}
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
		HVO hvo = m_vhvoPath[0];
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
			dwT = max(0, m_vhcFilteredRecords.Size() - 1);
	}
	SetCurRecIndex((int)dwT);

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
	// This will load the data from the database.
	m_qvwbrs->SetSelection(m_ivblt[kvbltView], sisel);
	// This will display the selected record in the client window.
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
		LOWORD(vflag[iband])           = width (in pixels) of the band
		HIWORD(vflag[iband]) & 0x8000  = 1 if the next toolbar should be on a new line
		HIWORD(vflag[iband]) & ~0x8000 = toolbar index in m_vqtlbr
	Subclasses should override this in order to use different default settings.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::LoadDefaultToolbarFlags(Vector<DWORD> & vflag, DWORD & dwBarFlags)
{
	dwBarFlags = 0x7f; // Show all 7 toolbars.
	int cband = m_vqtlbr.Size();
	int rgdxpBar[] = { 0x018b, 0x00ee, 0x0193, 0x0140, 0x0098, 0x0154, 0x0080 };	// width
	bool rgfBreak[] = { true, true, false, true, true, false, false };				// new line
	int rgitlbr[] = { 0, 1, 6, 2, 4, 3, 5 };										// index
	for (int iband = 0; iband < cband; iband++)
	{
		vflag[iband] = MAKELPARAM(rgdxpBar[iband], rgitlbr[iband] | (rgfBreak[iband] << 15));
	}
}


/*----------------------------------------------------------------------------------------------
	Create the client window needed for the specified view.

	@param vwt View Type to be created(Data Entry, Browse, or Document)
	@param vqafcw Vector of pointers to put the new view in.
	@param wid Window ID to use for the new view.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::CreateClient(UserViewType vwt, Vector<AfClientWndPtr> & vqafcw, int wid)
{
	AfClientWndPtr qafcw;

	switch (vwt)
	{
	case kvwtDE:
		qafcw.Attach(NewObj AfClientRecDeWnd);
		qafcw->Create(_T(""), kimagDataEntry, wid);
		break;
	case kvwtBrowse:
		{ //BLOCK for pcrvw
			qafcw.Attach(NewObj AfClientRecVwWnd);
			qafcw->Create(_T(""), kimagBrowse, wid);
			AfClientRecVwWnd * pcrvw = dynamic_cast<AfClientRecVwWnd *>(qafcw.Ptr());
			Assert(pcrvw);
			pcrvw->EnableHScroll();	// Browse view needs horizontal scroll bar.
		}
		break;
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
void RnMainWnd::LoadMainData()
{
	// Load the vector of objects we can move through.
	HVO hvoRnId = dynamic_cast<RnLpInfo *>(m_qlpi.Ptr())->GetRnId();
	m_qcvd->SetTags(kflidRnResearchNbk_Records, kflidRnGenericRec_DateCreated);
	GetSortMenuNodes(m_qlpi);		// This creates the default sort as a side-effect.
	m_qcvd->LoadMainItems(hvoRnId, m_vhcRecords, &m_asiDefault, &m_vskhSortKeys);
}


/*----------------------------------------------------------------------------------------------
	Clears and then loads information into the view bar. This assumes AfLpInfo and AfDbInfo
	have been properly initialized.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::LoadViewBar()
{
	StrAppBuf strbTemp; // Holds temp string, e.g., strings used as tab captions.
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();

	// First, save the current selections for each AfListBar.
	Set<int> siselView;
	Set<int> siselFilter;
	Set<int> siselSort;
	Set<int> siselOverlay;

	// If we are reloading, first clear out any existing list bars.
	AfViewBar * pvwbr = m_qvwbrs->GetViewBar();
	AssertPtr(pvwbr);
	int clist = pvwbr->GetListCount();
	int listCur;

	int cWbrsViews = 0;
	if (clist)
	{
		AfViewBarBase * pavbb = pvwbr->GetList(m_ivblt[kvbltView]);
		AssertPtr(pavbb);
		int cWbrsViews = pavbb->GetSize();

		listCur = m_qvwbrs->GetCurrentList();
		if (cWbrsViews)
			m_qvwbrs->GetSelection(m_ivblt[kvbltView], siselView);
		m_qvwbrs->GetSelection(m_ivblt[kvbltFilter], siselFilter);
		m_qvwbrs->GetSelection(m_ivblt[kvbltSort], siselSort);
		m_qvwbrs->GetSelection(m_ivblt[kvbltOverlay], siselOverlay);
		pvwbr->Clear();
	}

	// Insert the list of views into the viewbar.
	strbTemp.Load(kstidViews);
	RnListBarPtr qrlb;
	qrlb.Create();
	qrlb->Init(m_ivblt[kvbltView]);
	m_qvwbrs->AddList(strbTemp.Chars(), m_rghiml[0], m_rghiml[1], false, qrlb);
	int cv = m_qmdic->GetChildCount();
	for (int iv = 0; iv < cv; iv++)
	{
		AfClientWnd * pafcw = m_qmdic->GetChildFromIndex(iv);
		AssertPtr(pafcw);
		m_qvwbrs->AddListItem(m_ivblt[kvbltView], pafcw->GetViewName(), pafcw->GetImageIndex());
	}

	// Insert the list of filters into the viewbar.
	strbTemp.Load(kstidFilters);
	qrlb.Create();
	qrlb->Init(m_ivblt[kvbltFilter]);
	m_qvwbrs->AddList(strbTemp.Chars(), m_rghiml[0], m_rghiml[1], false, qrlb);
	strbTemp.Load(kstidNoFilter);
	m_qvwbrs->AddListItem(m_ivblt[kvbltFilter], strbTemp.Chars(), kimagFilterNone);
	cv = pdbi->GetFilterCount();
	for (int iv = 0; iv < cv; iv++)
	{
		AppFilterInfo & afi = pdbi->GetFilterInfo(iv);
		if (afi.m_clidRec == GetRecordClid())
		{
			strbTemp = afi.m_stuName.Chars();
			m_qvwbrs->AddListItem(m_ivblt[kvbltFilter], strbTemp.Chars(),
				afi.m_fSimple ? kimagFilterSimple : kimagFilterFull);
		}
	}

	// Insert the list of sort orders into the viewbar.
	strbTemp.Load(kstidSortMethods);
	qrlb.Create();
	qrlb->Init(m_ivblt[kvbltSort]);
	m_qvwbrs->AddList(strbTemp.Chars(), m_rghiml[0], m_rghiml[1], false, qrlb);
	strbTemp.Load(kstidDefaultSort);
	m_qvwbrs->AddListItem(m_ivblt[kvbltSort], strbTemp.Chars(), kimagSort);
	cv = pdbi->GetSortCount();
	for (int iv = 0; iv < cv; iv++)
	{
		AppSortInfo & asi = pdbi->GetSortInfo(iv);
		if (asi.m_clidRec == GetRecordClid())
		{
			strbTemp = asi.m_stuName.Chars();
			m_qvwbrs->AddListItem(m_ivblt[kvbltSort], strbTemp.Chars(), kimagSort);
		}
	}

	// Insert the list of overlays into the viewbar.
	RnOverlayListBarPtr qrolb;
	qrolb.Create();
	qrolb->Init(m_hwnd, (RnLpInfo *)m_qlpi.Ptr());
	strbTemp.Load(kstidOverlays);
	m_qvwbrs->AddList(strbTemp.Chars(), m_rghiml[0], m_rghiml[1], true, qrolb);
	strbTemp.Load(kstidNoOverlay);
	m_qvwbrs->AddListItem(m_ivblt[kvbltOverlay], strbTemp.Chars(), kimagOverlayNone);
	cv = m_qlpi->GetOverlayCount();
	for (int iv = 0; iv < cv; iv++)
	{
		AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(iv);
		strbTemp = aoi.m_stuName.Chars();
		m_qvwbrs->AddListItem(m_ivblt[kvbltOverlay], strbTemp.Chars(), kimagOverlay);
	}
	if (clist)
	{
		// Now restore the original AfListBar selections.
		m_qvwbrs->SetCurrentList(listCur);
		m_qvwbrs->SetSelection(m_ivblt[kvbltFilter], siselFilter);
		m_qvwbrs->SetSelection(m_ivblt[kvbltSort], siselSort);
		m_qvwbrs->SetSelection(m_ivblt[kvbltOverlay], siselOverlay); // This needs current list set.
		m_qvwbrs->ShowViewBar(GetViewbarSaveFlags() & kmskShowViewBar);
		if (cWbrsViews)
			m_qvwbrs->SetSelection(m_ivblt[kvbltView], siselView);
	}
}

/*----------------------------------------------------------------------------------------------
	Show (if true) or hide (if false) all overlays. if fRemerge is true, also recompute the
	overlay.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::ShowAllOverlays(bool fShow, bool fRemerge)
{
	AfViewBar * pvwbr = m_qvwbrs->GetViewBar();
	AssertPtr(pvwbr);
	RnOverlayListBar * prolb =
		dynamic_cast<RnOverlayListBar *>(pvwbr->GetList(m_ivblt[kvbltOverlay]));
	AssertPtr(prolb);
	if (fRemerge)
	{
		Set<int> siselT;
		prolb->GetSelection(siselT);
		pvwbr->OnSelChanged(prolb, siselT, siselT);
	}
	// If we are being activated, show all the overlays, otherwise hide them all.
	if (fShow)
		prolb->ShowAllOverlays();
	else
		prolb->HideAllOverlays();
}


/*----------------------------------------------------------------------------------------------
	Enable or disable this window and all its modeless popups.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::EnableWindow(bool fEnable)
{
	SuperClass::EnableWindow(fEnable);

	// Enable or disable the overlay windows.
	int ctot = m_qlpi->GetOverlayCount();
	for (int itot = 0; itot < ctot; itot++)
	{
		AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(itot);
		if (aoi.m_qtot && aoi.m_qtot->Hwnd())
			::EnableWindow(aoi.m_qtot->Hwnd(), fEnable);
	}
}


/*----------------------------------------------------------------------------------------------
	The user has selected a different item in the view bar.
	For Views:	Clear the active rootbox.  Save the current changes (if any) to the database.
		Switch to the selected view.  Update the caption bar.
	For Filters, Sort order, or Overlays:
		Switch to the selected filter, sort order, or overlay, then update the caption bar.

	@param ilist viewbar vector index
	@param siselOld the viewbar index of what was selected earlier
	@param siselNew the viewbar index of what is now selected

	@return true
----------------------------------------------------------------------------------------------*/
bool RnMainWnd::OnViewBarChange(int ilist, Set<int> & siselOld, Set<int> & siselNew)
{
	bool fStatusHidden = !::IsWindowVisible(m_qstbr->Hwnd());
	// Don't allow changes if editors can't be closed.
	if (!IsOkToChange())
		return false;
	bool fChanging = !siselOld.Equals(siselNew);
	int ivblt = GetViewbarListType(ilist);

	switch (ivblt)
	{
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
				AfClientRecWnd * pafcrw =
					dynamic_cast<AfClientRecWnd *>(m_qmdic->GetCurChild());
				Assert(pafcrw);
				pafcrw->DispCurRec(0,0);
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
#if 0
			StrApp str;
			if (pafcw->GetImageIndex() == kimagDataEntry)
			{
				if (m_vhcFilteredRecords[m_ihvoCurr].clsid == kclidRnEvent)
					str.Load(kstidEvent);
				else
					str.Load(kstidAnalysis);
				str.FormatAppend(" - %s", pafcw->GetViewName());
			}
			else
			{
				str = pafcw->GetViewName();
			}
			pcpbr->SetCaptionText(str.Chars());
#endif
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
			if (ifltr >= 0)
				afi = m_qlpi->GetDbInfo()->GetFilterInfo(ifltr);

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
			RnFilterXrefUtil rxref;
			ApplyFilterAndSort(ifltr, fCancel, isrt, &rxref);

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

			RnFilterXrefUtil rxref;
			ApplyFilterAndSort(iflt, fNoPrompt, isrt, &rxref);

			m_qstbr->EndProgressBar();
			UpdateStatusBar();
			if (fStatusHidden)
			{
				::ShowWindow(m_qstbr->Hwnd(), SW_HIDE);
				::SendMessage(m_hwnd, WM_SIZE, kwstRestored, 0);
			}
		}
		break;

	case kvbltOverlay: // Overlays
		{
			MergeOverlays(siselNew);

			// Update the caption bar.
			AfCaptionBar * pcpbr = m_qmdic->GetCaptionBar();
			AssertPtr(pcpbr);
			int imag = *siselNew.Begin() == 0 ? kimagOverlayNone : kimagOverlay;
			pcpbr->SetIconImage(m_ivblt[kvbltOverlay], imag);
		}
		break;
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	If the given overlay is currently showing, remerge all open overlays.

	@param iovr Overlay index
	@param ibtn Button Context
----------------------------------------------------------------------------------------------*/
void RnMainWnd::OnChangeOverlay(int iovr)
{
	Set<int> sisel;
	m_qvwbrs->GetSelection(m_ivblt[kvbltOverlay], sisel);
	int isel = iovr + 1; // We add 1 because the first item is 'No Overlay'.
	if (sisel.IsMember(isel))
		MergeOverlays(sisel);
}


/*----------------------------------------------------------------------------------------------
	A record or subrecord has been deleted. A deleted subrecord might also be a top-level entry
	if a filter is on. This method does not update the real (i.e. what gets put in the
	database) cache vector property. Since this method always updates the vector in memory
	(m_vhcRecords) except when the record is a nested subrecord, the real cache property must
	be updated when this method is called to keep the two lists synchronized. The vector of
	filtered records in memory (m_vhcFilteredRecords) is updated.

	@param flid field Id of record to be deleted
	@param hvoDel Hvo of object to be deleted
	@param fCheckEmpty Flag whether to check if hvoDel was the last record.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::OnDeleteRecord(int flid, HVO hvoDel, bool fCheckEmpty)
{
	// Note subrecords may be displayed as part of the current record as well as a separate
	// top-level record. First, make sure it is deleted from the current main record.
	if (flid == kflidRnGenericRec_SubRecords)
	{
		int cwnd = m_qmdic->GetChildCount();
		// Go through all DE windows.
		for (int iwnd = 0; iwnd < cwnd; ++iwnd)
		{
			AfClientRecDeWndPtr qcrde =
				dynamic_cast<AfClientRecDeWnd *>(m_qmdic->GetChildFromIndex(iwnd));
			if (!qcrde || !qcrde->Hwnd())
				continue; // Not an active data entry window.
			HVO hvoOwn;
			m_qcvd->get_ObjectProp(hvoDel, kflidCmObject_Owner, &hvoOwn);
			Assert(hvoOwn);
			// Make sure it is deleted from both panes.
			dynamic_cast<AfDeSplitChild*>(qcrde->GetPane(0))->DeleteTreeNode(hvoDel, 0);
			if (dynamic_cast<AfDeSplitChild*>(qcrde->GetPane(1)))
				dynamic_cast<AfDeSplitChild*>(qcrde->GetPane(1))->DeleteTreeNode(hvoDel, 0);
		}
	}

	bool fDelCurRec = m_vhcFilteredRecords[m_ihvoCurr].hvo == hvoDel;

	// Delete the record from member variables but not the cache.
	DeleteMainRecord(hvoDel, false);

	// We need to update all the views if we're deleting the current record.
	if (fDelCurRec)
	{
		// This is the only safe way to handle deletions since multiple windows may be open on
		// the same language project.
		AfDbApp * papp = dynamic_cast<AfDbApp *>(AfApp::Papp());
		Assert(papp);
		papp->FullRefresh(GetLpInfo());
	}

	UpdateStatusBar();

	if (fCheckEmpty)
	{
		// Check for an empty database.
		AfLpInfo * plpi = GetLpInfo();
		AssertPtr(plpi);
		StrApp strProj(plpi->PrjName());
		CheckForNoRecords(strProj.Chars());
	}
}

static const int kcchBuffer = MAX_PATH;

/*----------------------------------------------------------------------------------------------
	Get the names of the roles that can be used for participants.  Compare these to any existing
	list in the filter menu, and update the filter menu if necessary.

	@param vfmnRoles Reference to a vector of filter menu nodes for output.
	@param plpi Pointer to the language project info, for obtaining the database connection.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::GetFilterMenuRoles(FilterMenuNodeVec & vfmnRoles, AfLpInfo * plpi)
{
	IOleDbEncapPtr qode;
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetDbAccess(&qode);
	Vector<HVO> & vhvoPssl = plpi->GetPsslIds();
	int ws = UserWs();
	try
	{
		IOleDbCommandPtr qodc;
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;
		StrUni stuQuery;

		stuQuery.Format(L"SELECT Obj, Txt%n"
			L"FROM CmPossibility_Name pn%n"
			L"JOIN CmObject o ON o.[Id] = pn.Obj AND o.Owner$ = %d%n"
			L"WHERE pn.Ws = %d%n",
			vhvoPssl[RnLpInfo::kpidPsslRol], ws);

#ifdef LOG_FILTER_SQL
		StrAnsiBufBig stab;
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
		HVO hvo;
		OLECHAR rgchName[kcchBuffer];
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo),
				isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(rgchName),
				isizeof(rgchName), &cbSpaceTaken, &fIsNull, 2));
#ifdef LOG_FILTER_SQL
			if (fp)
			{
				stab.Format("Role hvo = %d, Name = \"%S\"", hvo, rgchName);
				fprintf(fp, "%s\n", stab.Chars());
			}
#endif
			FilterMenuNodePtr qfmnPopup;
			qfmnPopup.Create();
			qfmnPopup->m_stuText = rgchName;
			qfmnPopup->m_fmnt = kfmntField;
			qfmnPopup->m_flid = kflidRnRoledPartic_Participants;
			qfmnPopup->m_proptype = kfptRoledParticipant;
			qfmnPopup->m_hvo = hvo;
			FilterMenuNode::AddSortedMenuNode(vfmnRoles, qfmnPopup);
			CheckHr(qodc->NextRow(&fMoreRows));
		}

		// Find the corresponding vector of menu nodes.
		FilterMenuNodePtr qfmnEvent;
		int ifmn;
		for (ifmn = 0; ifmn < m_vfmn.Size(); ++ifmn)
		{
			if (m_vfmn[ifmn]->m_fmnt == kfmntClass && m_vfmn[ifmn]->m_clid == kclidRnEvent)
			{
				qfmnEvent = m_vfmn[ifmn];
				break;
			}
		}
		if (!qfmnEvent)
			return;				// No event class at top level, return this vector of nodes.
		FilterMenuNodePtr qfmnRoledPart;
		for (ifmn = 0; ifmn < qfmnEvent->m_vfmnSubItems.Size(); ++ifmn)
		{
			if (qfmnEvent->m_vfmnSubItems[ifmn]->m_fmnt == kfmntField &&
				qfmnEvent->m_vfmnSubItems[ifmn]->m_flid == kflidRnEvent_Participants)
			{
				qfmnRoledPart = qfmnEvent->m_vfmnSubItems[ifmn];
				break;
			}
		}
		if (!qfmnRoledPart)
			return;				// Roled Participant not in the menu yet.

		bool fChanged = false;
		FilterMenuNodeVec & vfmnOld = qfmnRoledPart->m_vfmnSubItems;
		if (vfmnRoles.Size() != vfmnOld.Size() - 3)
		{
			fChanged = true;
		}
		else
		{
			for (ifmn = 3; ifmn < vfmnOld.Size(); ++ifmn)
			{
				if (vfmnRoles[ifmn-3]->m_stuText != vfmnOld[ifmn]->m_stuText ||
					vfmnRoles[ifmn-3]->m_fmnt != vfmnOld[ifmn]->m_fmnt ||
					vfmnRoles[ifmn-3]->m_flid != vfmnOld[ifmn]->m_flid ||
					vfmnRoles[ifmn-3]->m_proptype != vfmnOld[ifmn]->m_proptype ||
					vfmnRoles[ifmn-3]->m_hvo != vfmnOld[ifmn]->m_hvo)
				{
					fChanged = true;
					break;
				}
			}
		}
		if (!fChanged)
		{
			vfmnRoles.Clear();
			return;				// Roled Participant not in the menu!?
		}
		// Add separator line to menu if it is needed.
		if (vfmnOld.Size() == 2 && vfmnRoles.Size())
		{
			FilterMenuNodePtr qfmnLeaf;
			qfmnLeaf.Create();
			qfmnLeaf->m_stuText.Clear();
			qfmnLeaf->m_fmnt = kfmntLeaf;
			qfmnLeaf->m_flid  = 0;
			qfmnLeaf->m_proptype = kcptNil;
			vfmnOld.Insert(2, qfmnLeaf);
		}
		// Replace the old roles in the menu with the new roles.
		if (vfmnOld.Size() >= 3)
			vfmnOld.Replace(3, vfmnOld.Size(), vfmnRoles.Begin(), vfmnRoles.Size());
		// Remove the separator line if it is no longer needed.
		if (vfmnOld.Size() == 3)
			vfmnOld.Delete(2);
		// Add the submenus to the new roles.
		for (ifmn = 3; ifmn < vfmnOld.Size(); ++ifmn)
			_CopyMenuNodeVector(vfmnOld[ifmn]->m_vfmnSubItems, vfmnOld[0]->m_vfmnSubItems);
	}
	catch (...)
	{
	}
	vfmnRoles.Clear();
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
	Returns a pointer to the vector of filter menu nodes.  If the size of the vector (m_vfmn)
	is 0 then the menu nodes are are created being pushed into the vectors.  Then go through
	all the menu items recursively and assign the field type for each field.

	@param plpi ptr to Language Project Info object.

	@return pointer to the vector of the filter menu nodes.
----------------------------------------------------------------------------------------------*/
FilterMenuNodeVec * RnMainWnd::GetFilterMenuNodes(AfLpInfo * plpi)
{
	AssertPtr(plpi);

	FilterMenuNodePtr qfmnPopup;
	FilterMenuNodePtr qfmnLeaf;
	FilterMenuNodePtr qfmnCopy;
	FilterMenuNodeVec vfmnRoles;

	if (m_vfmn.Size() != 0)
	{
		// Redo only those parts of the menu that can vary dynamically while the program is
		// running.
		GetFilterMenuRoles(vfmnRoles, plpi);
		return &m_vfmn;
	}

#ifdef LOG_FILTER_SQL
	fp = fopen("c:/FW/DebugOutput.txt", "a");
	StrAnsiBufBig stab;
	if (fp)
	{
		fprintf(fp, "\n\
===============================================================================\n");
		time_t nTime;
		time(&nTime);
		fprintf(fp, "DEBUG RnMainWnd::GetFilterMenuNodes(AfLpInfo * plpi) at %s",
			ctime(&nTime));
	}
#endif

	Vector<HVO> & vhvoPssl = plpi->GetPsslIds();
	FilterMenuNodeVec vfmnPerson;
	FilterMenuNodeVec vfmnOverlayTag;

	/*------------------------------------------------------------------------------------------
		Create the top level popup menus (Event, Analysis, and Any).
	------------------------------------------------------------------------------------------*/
	Vector<int> vclidTop;
	FilterMenuNodePtr qfmnEvent;
	qfmnEvent.Create();
	qfmnEvent->m_stuText.Load(kstidEvent);						// "Event"
	qfmnEvent->m_fmnt = kfmntClass;
	qfmnEvent->m_clid = kclidRnEvent;
	m_vfmn.Push(qfmnEvent);
	vclidTop.Push(qfmnEvent->m_clid);

	FilterMenuNodePtr qfmnAnalysis;
	qfmnAnalysis.Create();
	qfmnAnalysis->m_stuText.Load(kstidAnalysis);				// "Analysis"
	qfmnAnalysis->m_fmnt = kfmntClass;
	qfmnAnalysis->m_clid = kclidRnAnalysis;
	m_vfmn.Push(qfmnAnalysis);
	vclidTop.Push(qfmnAnalysis->m_clid);

	FilterMenuNodePtr qfmnGeneric;
	qfmnGeneric.Create();
	qfmnGeneric->m_stuText.Load(kstidGenericRecord);			// "Any Entry Type"
	qfmnGeneric->m_fmnt = kfmntClass;
	qfmnGeneric->m_clid = kclidRnGenericRec;
	m_vfmn.Push(qfmnGeneric);
	vclidTop.Push(qfmnGeneric->m_clid);

	IOleDbEncapPtr qode;
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetDbAccess(&qode);

	Vector<FieldData> vfd;
	Vector<FieldData> vfdPerson;
	HashMap<int, LabelData> hmclidld;
	HashMap<int, HashMap<int, LabelData> > hmflidhmclidld;

	try
	{
		IOleDbCommandPtr qodc;
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;
		StrUni stuQuery;

		int ws = UserWs();

		// Load the relevant field names from the user views in the database.
		StrUni stuClasses;
		stuClasses.Format(L"%d,%d,%d,%d,%d,%d",
			kclidRnGenericRec, kclidRnAnalysis, kclidRnEvent, kclidRnRoledPartic,
			kclidCmPerson, kclidCmPossibility);
		stuQuery.Format(L"SELECT DISTINCT uvf.Flid, uvr.Clsid, mt.Txt%n"
			L" FROM UserViewField uvf%n"
			L" JOIN UserViewField_Label mt ON uvf.[Id] = mt.Obj%n"
			L" JOIN CmObject cmo ON uvf.[Id] = cmo.[Id]%n"
			L" JOIN UserViewRec uvr ON cmo.Owner$ = uvr.[Id]%n "
			L"WHERE uvf.Flid IN (SELECT [Id] FROM Field$ WHERE Class IN (%s))%n"
			L"       AND uvr.Clsid IN (%s)%n"
			L"       AND (mt.Ws = %d)%n"
			L" ORDER BY uvf.Flid",
			stuClasses.Chars(), stuClasses.Chars(), ws);
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
		stuQuery.Format(L"SELECT Id,Type,Class,DstCls FROM Field$%n"
			L"WHERE Class IN (%s)",
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
		bool fNeedOverlayListNames = false;
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
				bool fFound = false;
				for (int iclid = 0; iclid < vclidTop.Size(); ++iclid)
				{
					if (hmclidld.Retrieve(vclidTop[iclid], &ldT))
					{
						fFound = true;
						break;
					}
				}
				if (!fFound)
					fFound = hmclidld.Retrieve(fd.clid, &ldT);
#ifdef LOG_FILTER_SQL
				if (fp)
					fprintf(fp,
						"fd.flid = %7d, .type = %2d, .clid = %4d, .clidDest = %4d%s\n",
						fd.flid, fd.type, fd.clid, fd.clidDest,
						fFound ? "" : " (NO LABEL - NOT STORED)");
#endif
				if (fFound)
				{
					switch (fd.clid)
					{
					case kclidCmPossibility:
					case kclidCmPerson:
						vfdPerson.Push(fd);
						break;
					case kclidRnRoledPartic:
						break;
					default:
						if (fd.flid == kflidRnGenericRec_PhraseTags)
							fNeedOverlayListNames = true;
						vfd.Push(fd);
						break;
					}
				}
			}
			// Make sure that "Overlay Tags" are added to the menu if they exist.
			else if (fd.flid == kflidRnGenericRec_PhraseTags)
			{
#ifdef LOG_FILTER_SQL
				if (fp)
					fprintf(fp,
		"fd.flid = %7d, .type = %2d, .clid = %4d, .clidDest = %4d (STORED WITHOUT A LABEL)\n",
						fd.flid, fd.type, fd.clid, fd.clidDest);
#endif
				fNeedOverlayListNames = true;
				vfd.Push(fd);
			}
#ifdef LOG_FILTER_SQL
			else if (fp)
			{
				fprintf(fp,
		"fd.flid = %7d, .type = %2d, .clid = %4d, .clidDest = %4d (NOT STORED - NO LABEL)\n",
					fd.flid, fd.type, fd.clid, fd.clidDest);
			}
#endif
			CheckHr(qodc->NextRow(&fMoreRows));
		}

		if (fNeedOverlayListNames)
		{
			int cvo = m_qlpi->GetOverlayCount();
			for (int ivo = 0; ivo < cvo; ++ivo)
			{
				AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(ivo);
				qfmnLeaf.Create();
				qfmnLeaf->m_stuText = aoi.m_stuName;
				qfmnLeaf->m_fmnt = kfmntLeaf;
				// The m_flid variable is used to store the index into the overlay list instead
				// of the flid of the tag, since the parent menu item of the tag stores that
				// information instead.
				qfmnLeaf->m_flid = ivo;
				qfmnLeaf->m_proptype = kfptTagList;
				qfmnLeaf->m_hvo = aoi.m_hvoPssl;
				FilterMenuNode::AddSortedMenuNode(vfmnOverlayTag, qfmnLeaf);
			}
		}

		GetFilterMenuRoles(vfmnRoles, plpi);

		// Add the two standard menu entries to the beginning of the Roled Participant submenu.
		qfmnPopup.Create();
		qfmnPopup->m_stuText.Load(kstidFltrAnyRoleMenuLabel);
		qfmnPopup->m_fmnt = kfmntField;
		qfmnPopup->m_flid = kflidRnRoledPartic_Participants;
		qfmnPopup->m_proptype = kfptRoledParticipant;
		qfmnPopup->m_hvo = -1;
		vfmnRoles.Insert(0, qfmnPopup);
		qfmnPopup.Create();
		qfmnPopup->m_stuText.Load(kstidFltrNoRoleMenuLabel);
		qfmnPopup->m_fmnt = kfmntField;
		qfmnPopup->m_flid = kflidRnRoledPartic_Participants;
		qfmnPopup->m_proptype = kfptRoledParticipant;
		qfmnPopup->m_hvo = 0;
		vfmnRoles.Insert(1, qfmnPopup);
		if (vfmnRoles.Size() > 2)
		{
			qfmnLeaf.Create();					// Separator line.
			qfmnLeaf->m_stuText.Clear();
			qfmnLeaf->m_fmnt = kfmntLeaf;
			qfmnLeaf->m_flid = 0;
			qfmnLeaf->m_proptype = kcptNil;
			vfmnRoles.Insert(2, qfmnLeaf);
		}
	}
	catch (...)	// Was empty.
	{
		throw;	// For now we have nothing to add, so pass it on up.
	}
#ifdef LOG_FILTER_SQL
	if (fp)
	{
		fclose(fp);
		fp = NULL;
	}
#endif

	IFwMetaDataCachePtr qmdc;
	plpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);

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
			qfmnLeaf->m_hvo = vhvoPssl[RnLpInfo::kpidPsslCon];
			break;
		case kflidCmPossibility_Discussion:
			qfmnLeaf->m_proptype = kfptStText;
			break;
		case kflidCmPerson_Education:
			qfmnLeaf->m_proptype = kfptPossList;
			qfmnLeaf->m_hvo = vhvoPssl[RnLpInfo::kpidPsslEdu];
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
			qfmnLeaf->m_hvo = vhvoPssl[RnLpInfo::kpidPsslLoc];
			break;
		case kflidCmPerson_PlacesOfResidence:
			qfmnLeaf->m_proptype = kfptPossList;
			qfmnLeaf->m_hvo = vhvoPssl[RnLpInfo::kpidPsslLoc];
			break;
		case kflidCmPerson_Positions:
			qfmnLeaf->m_proptype = kfptPossList;
			qfmnLeaf->m_hvo = vhvoPssl[RnLpInfo::kpidPsslPsn];
			break;
		case kflidCmPossibility_Restrictions:
			qfmnLeaf->m_proptype = kfptPossList;
			qfmnLeaf->m_hvo = vhvoPssl[RnLpInfo::kpidPsslRes];
			break;
		case kflidCmPossibility_Status:
			qfmnLeaf->m_proptype = kfptPossList;
			qfmnLeaf->m_hvo = vhvoPssl[RnLpInfo::kpidPsslAna];
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
	qfmnLeaf->m_hvo = vhvoPssl[RnLpInfo::kpidPsslPeo];
	vfmnPerson.Insert(0, qfmnLeaf);
	qfmnLeaf.Create();					// Separator line.
	qfmnLeaf->m_stuText.Clear();
	qfmnLeaf->m_fmnt = kfmntLeaf;
	qfmnLeaf->m_flid  = 0;
	qfmnLeaf->m_proptype = kcptNil;
	vfmnPerson.Insert(1, qfmnLeaf);

	// Finish filling in the Roled Participant popup menu entries.
	for (int i = 0; i < vfmnRoles.Size(); ++i)
	{
		if (vfmnRoles[i]->m_fmnt == kfmntField)
			_CopyMenuNodeVector(vfmnRoles[i]->m_vfmnSubItems, vfmnPerson);
	}

	// Add the submenu elements.
	for (ifd = 0; ifd < vfd.Size(); ++ifd)
	{
		int flid = vfd[ifd].flid;
		int clid = vfd[ifd].clid;
		int clidDest = vfd[ifd].clidDest;
		if (hmflidhmclidld.Retrieve(flid, &hmclidld))
		{
			bool fFound = false;
			for (int i = 0; i < vclidTop.Size(); ++i)
			{
				if (hmclidld.Retrieve(vclidTop[i], &ld))
				{
					fFound = true;
					break;
				}
			}
			if (!fFound)
			{
				Assert(false);
				continue;
			}
		}
		else if (flid == kflidRnGenericRec_PhraseTags)
		{
			ld.flid = flid;
			ld.clid = kclidRnGenericRec;
			ld.stuLabel.Load(kstidGenericRecordPhraseTags);
		}
		else
		{
			Assert(false);
			continue;
		}
		if (flid == kflidRnGenericRec_PhraseTags)
		{
			// Attach the submenu for "Overlay Tags".
			qfmnPopup.Create();
			qfmnPopup->m_stuText = ld.stuLabel;
			qfmnPopup->m_fmnt = kfmntField;
			qfmnPopup->m_flid = flid;
			_CopyMenuNodeVector(qfmnPopup->m_vfmnSubItems, vfmnOverlayTag);
			qfmnGeneric->AddSortedSubItem(qfmnPopup);
			_CopyMenuNode(&qfmnCopy, qfmnPopup);
			qfmnAnalysis->AddSortedSubItem(qfmnCopy);
			_CopyMenuNode(&qfmnCopy, qfmnPopup);
			qfmnEvent->AddSortedSubItem(qfmnCopy);
		}
		else if (flid == kflidRnGenericRec_SubRecords)
		{
			// We don't filter on subentries at this point.
			continue;
		}
		else if (clidDest == kclidCmPerson && flid != kflidRnGenericRec_Researchers)
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
				switch (clid)
				{
				case kclidRnGenericRec:
					qfmnGeneric->AddSortedSubItem(qfmnPopup);
					_CopyMenuNode(&qfmnCopy, qfmnPopup);
					qfmnAnalysis->AddSortedSubItem(qfmnCopy);
					_CopyMenuNode(&qfmnCopy, qfmnPopup);
					qfmnEvent->AddSortedSubItem(qfmnCopy);
					break;
				case kclidRnAnalysis:
					qfmnAnalysis->AddSortedSubItem(qfmnPopup);
					break;
				case kclidRnEvent:
					qfmnEvent->AddSortedSubItem(qfmnPopup);
					break;
				}
			}
		}
		else if (clidDest == kclidRnRoledPartic)
		{
			// Build and attach the submenu for "Roled Participant" nodes.
			if (vfmnRoles.Size())
			{
				qfmnPopup.Create();
				qfmnPopup->m_stuText = ld.stuLabel;
				qfmnPopup->m_fmnt = kfmntField;
				qfmnPopup->m_flid = flid;
				_CopyMenuNodeVector(qfmnPopup->m_vfmnSubItems, vfmnRoles);
				qfmnEvent->AddSortedSubItem(qfmnPopup);
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
				// Mark structured text nodes.
				qfmnLeaf->m_proptype = kfptStText;
			}
			else if (clidDest == kclidRnGenericRec || clidDest == kclidRnEvent ||
				clidDest == kclidRnAnalysis)
			{
				// Mark cross reference type nodes (subentries are excluded above).
				switch (vfd[ifd].type)
				{
				case kcptReferenceAtom:
					qfmnLeaf->m_proptype = kfptCrossRef;
					break;
				case kcptReferenceCollection:
				case kcptReferenceSequence:
					qfmnLeaf->m_proptype = kfptCrossRefList;
					break;
				default:
					Assert(false);
					break;
				}
			}
			else if (clidDest == kclidCmPossibility || clidDest == kclidCmAnthroItem ||
				clidDest == kclidCmLocation || clidDest == kclidCmPerson)
			{
				// Mark possibility list nodes.
				qfmnLeaf->m_proptype = kfptPossList;
				qfmnLeaf->m_hvo = PossListHvoForFlid(pdbi, flid);
				Assert(qfmnLeaf->m_hvo);
#ifdef DEBUG
				switch (flid)
				{
				case kflidRnGenericRec_Confidence:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[RnLpInfo::kpidPsslCon]);
					break;
				case kflidRnGenericRec_Restrictions:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[RnLpInfo::kpidPsslRes]);
					break;
				case kflidRnAnalysis_Status:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[RnLpInfo::kpidPsslAna]);
					break;
				case kflidRnEvent_Type:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[RnLpInfo::kpidPsslTyp]);
					break;
				case kflidRnEvent_Weather:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[RnLpInfo::kpidPsslWea]);
					break;
				case kflidRnEvent_TimeOfEvent:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[RnLpInfo::kpidPsslTim]);
					break;
				case kflidRnGenericRec_AnthroCodes:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[RnLpInfo::kpidPsslAnit]);
					break;
				case kflidRnEvent_Locations:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[RnLpInfo::kpidPsslLoc]);
					break;
				case kflidRnGenericRec_Researchers:
					Assert(qfmnLeaf->m_hvo == vhvoPssl[RnLpInfo::kpidPsslPeo]);
					break;
				}
#endif
			}
			else
			{
				// Nothing more to set for other types of nodes.
			}
			// Store this menu node in the appropriate master menu(s).
			switch (clid)
			{
			case kclidRnGenericRec:
				qfmnGeneric->AddSortedSubItem(qfmnLeaf);
				_CopyMenuNode(&qfmnCopy, qfmnLeaf);
				qfmnAnalysis->AddSortedSubItem(qfmnCopy);
				_CopyMenuNode(&qfmnCopy, qfmnLeaf);
				qfmnEvent->AddSortedSubItem(qfmnCopy);
				break;
			case kclidRnAnalysis:
				qfmnAnalysis->AddSortedSubItem(qfmnLeaf);
				break;
			case kclidRnEvent:
				qfmnEvent->AddSortedSubItem(qfmnLeaf);
				break;
			}
		}
	}

	// Go through all the menu items recursively and assign the type for each field.

	int cfmn = m_vfmn.Size();
	for (int ifmn = 0; ifmn < cfmn; ifmn++)
		_AssignFieldTypes(qmdc, m_vfmn[ifmn]);

	return &m_vfmn;
}

/*----------------------------------------------------------------------------------------------
	Get the names of the roles that can be used for participants.  Compare these to any existing
	list in the sort menu, and update the sort menu if necessary.

	@param vsmnRoles Reference to a vector of sort menu nodes for output.
	@param plpi Pointer to the language project info, for obtaining the database connection.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::GetSortMenuRoles(SortMenuNodeVec & vsmnRoles, AfLpInfo * plpi)
{
	IOleDbEncapPtr qode;
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetDbAccess(&qode);
	Vector<HVO> & vhvoPssl = plpi->GetPsslIds();
	int ws = UserWs();
	try
	{
		IOleDbCommandPtr qodc;
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;
		StrUni stuQuery;

		stuQuery.Format(L"SELECT Obj, Txt%n"
			L"FROM CmPossibility_Name pn%n"
			L"JOIN CmObject o ON o.[Id] = pn.Obj AND o.Owner$ = %d%n"
			L"WHERE pn.Ws = %d%n",
			vhvoPssl[RnLpInfo::kpidPsslRol], ws);

#ifdef LOG_FILTER_SQL
		StrAnsiBufBig stab;
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
		HVO hvo;
		OLECHAR rgchName[kcchBuffer];
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo),
				isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(rgchName),
				isizeof(rgchName), &cbSpaceTaken, &fIsNull, 2));
#ifdef LOG_FILTER_SQL
			if (fp)
			{
				stab.Format("Role hvo = %d, Name = \"%S\"", hvo, rgchName);
				fprintf(fp, "%s\n", stab.Chars());
			}
#endif
			SortMenuNodePtr qsmnPopup;
			qsmnPopup.Create();
			qsmnPopup->m_stuText = rgchName;
			qsmnPopup->m_smnt = ksmntField;
			qsmnPopup->m_flid = kflidRnRoledPartic_Participants;
			qsmnPopup->m_proptype = kfptRoledParticipant;
			qsmnPopup->m_hvo = hvo;
			SortMenuNode::AddSortedMenuNode(vsmnRoles, qsmnPopup);
			CheckHr(qodc->NextRow(&fMoreRows));
		}

		// Find the corresponding vector of menu nodes.
		SortMenuNodePtr qsmnEvent;
		int ismn;
		for (ismn = 0; ismn < m_vsmn.Size(); ++ismn)
		{
			if (m_vsmn[ismn]->m_smnt == ksmntClass && m_vsmn[ismn]->m_clid == kclidRnEvent)
			{
				qsmnEvent = m_vsmn[ismn];
				break;
			}
		}
		if (!qsmnEvent)
			return;				// No event class at top level, return this vector of nodes.
		SortMenuNodePtr qsmnRoledPart;
		for (ismn = 0; ismn < qsmnEvent->m_vsmnSubItems.Size(); ++ismn)
		{
			if (qsmnEvent->m_vsmnSubItems[ismn]->m_smnt == ksmntField &&
				qsmnEvent->m_vsmnSubItems[ismn]->m_flid == kflidRnEvent_Participants)
			{
				qsmnRoledPart = qsmnEvent->m_vsmnSubItems[ismn];
				break;
			}
		}
		if (!qsmnRoledPart)
			return;				// Roled Participant not in the menu yet.

		bool fChanged = false;
		SortMenuNodeVec & vsmnOld = qsmnRoledPart->m_vsmnSubItems;
		if (vsmnRoles.Size() != vsmnOld.Size() - 3)
		{
			fChanged = true;
		}
		else
		{
			for (ismn = 3; ismn < vsmnOld.Size(); ++ismn)
			{
				if (vsmnRoles[ismn-3]->m_stuText != vsmnOld[ismn]->m_stuText ||
					vsmnRoles[ismn-3]->m_smnt != vsmnOld[ismn]->m_smnt ||
					vsmnRoles[ismn-3]->m_flid != vsmnOld[ismn]->m_flid ||
					vsmnRoles[ismn-3]->m_proptype != vsmnOld[ismn]->m_proptype ||
					vsmnRoles[ismn-3]->m_hvo != vsmnOld[ismn]->m_hvo)
				{
					fChanged = true;
					break;
				}
			}
		}
		if (!fChanged)
		{
			vsmnRoles.Clear();
			return;				// Roled Participant not in the menu!?
		}
		// Add separator line to menu if it is now needed.
		if (vsmnOld.Size() == 2 && vsmnRoles.Size())
		{
			SortMenuNodePtr qsmnLeaf;
			qsmnLeaf.Create();
			qsmnLeaf->m_stuText.Clear();
			qsmnLeaf->m_smnt = ksmntLeaf;
			qsmnLeaf->m_flid  = 0;
			qsmnLeaf->m_proptype = kcptNil;
			vsmnOld.Insert(2, qsmnLeaf);
		}
		// Replace the old roles in the menu with the new roles.
		if (vsmnOld.Size() >= 3)
			vsmnOld.Replace(3, vsmnOld.Size(), vsmnRoles.Begin(), vsmnRoles.Size());
		// Remove the separator line if it is no longer needed.
		if (vsmnOld.Size() == 3)
			vsmnOld.Delete(2);
		// Add the submenus to the new roles.
		for (ismn = 3; ismn < vsmnOld.Size(); ++ismn)
			_CopyMenuNodeVector(vsmnOld[ismn]->m_vsmnSubItems, vsmnOld[0]->m_vsmnSubItems);
	}
	catch (...)
	{
	}
	vsmnRoles.Clear();
}

/*----------------------------------------------------------------------------------------------
	Returns a pointer to the vector of sort menu nodes.  If the size of the vector (m_vsmn)
	is 0 then the vector of menu nodes is created.

	@param plpi ptr to Language Project Info object.

	@return pointer to the vector of the sort list nodes.
----------------------------------------------------------------------------------------------*/
SortMenuNodeVec * RnMainWnd::GetSortMenuNodes(AfLpInfo * plpi)
{
	AssertPtr(plpi);

	SortMenuNodePtr qsmnPopup;
	SortMenuNodePtr qsmnLeaf;
	SortMenuNodePtr qsmnCopy;
	SortMenuNodeVec vsmnRoles;

	if (m_vsmn.Size() != 0)
	{
		// Redo only those parts of the menu that can vary dynamically while the program is
		// running.
		GetSortMenuRoles(vsmnRoles, plpi);
		return &m_vsmn;
	}

#ifdef LOG_FILTER_SQL
	fp = fopen("c:/FW/DebugOutput.txt", "a");
	StrAnsiBufBig stab;
	if (fp)
	{
		fprintf(fp, "\n\
===============================================================================\n");
		time_t nTime;
		time(&nTime);
		fprintf(fp, "DEBUG RnMainWnd::GetSortMenuNodes(AfLpInfo * plpi) at %s",
			ctime(&nTime));
	}
#endif

#ifdef DEBUG
	Vector<HVO> & vhvoPssl = plpi->GetPsslIds();
#endif

	int iclid;
	int rgclidPoss[] =
	{
		kclidCmPossibility, kclidCmLocation, kclidCmPerson, kclidCmAnthroItem, kclidCmCustomItem
	};
	const int kcclidPoss = isizeof(rgclidPoss) / isizeof(int);
	Set<int> setclidPoss;
	for (iclid = 0; iclid < kcclidPoss; ++iclid)
		setclidPoss.Insert(rgclidPoss[iclid]);

	/*------------------------------------------------------------------------------------------
		Create the top level popup menus (Event, Analysis, and Any).
	------------------------------------------------------------------------------------------*/
	Vector<int> vclidTop;
	SortMenuNodePtr qsmnEvent;
	qsmnEvent.Create();
	qsmnEvent->m_stuText.Load(kstidEvent);						// "Event"
	qsmnEvent->m_smnt = ksmntClass;
	qsmnEvent->m_clid = kclidRnEvent;
	m_vsmn.Push(qsmnEvent);
	vclidTop.Push(qsmnEvent->m_clid);

	SortMenuNodePtr qsmnAnalysis;
	qsmnAnalysis.Create();
	qsmnAnalysis->m_stuText.Load(kstidAnalysis);				// "Analysis"
	qsmnAnalysis->m_smnt = ksmntClass;
	qsmnAnalysis->m_clid = kclidRnAnalysis;
	m_vsmn.Push(qsmnAnalysis);
	vclidTop.Push(qsmnAnalysis->m_clid);

	SortMenuNodePtr qsmnGeneric;
	qsmnGeneric.Create();
	qsmnGeneric->m_stuText.Load(kstidGenericRecord);			// "Any Entry Type"
	qsmnGeneric->m_smnt = ksmntClass;
	qsmnGeneric->m_clid = kclidRnGenericRec;
	m_vsmn.Push(qsmnGeneric);
	vclidTop.Push(qsmnGeneric->m_clid);

	IOleDbEncapPtr qode;
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetDbAccess(&qode);

	Vector<FieldData> vfd;
	HashMap<int, LabelData> hmclidld;
	HashMap<int, HashMap<int, LabelData> > hmflidhmclidld;
	HashMap<int, FieldData> hmflidfd;
	int wsTitle = 0;

	try
	{
		IOleDbCommandPtr qodc;
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;
		StrUni stuQuery;

		int ws = UserWs();

		// Load the relevant field names from the user views in the database.
		StrUni stuClasses;
		stuClasses.Format(L"%d,%d,%d,%d",
			kclidRnGenericRec, kclidRnAnalysis, kclidRnEvent, kclidRnRoledPartic);
		for (iclid = 0; iclid < kcclidPoss; ++iclid)
			stuClasses.FormatAppend(L",%d", rgclidPoss[iclid]);
		stuQuery.Format(L"SELECT DISTINCT"
			L" uvf.Flid, uvr.Clsid, mt.Txt, uvf.WritingSystem, uvf.WsSelector%n"
			L" FROM UserViewField uvf%n"
			L" JOIN UserViewField_Label mt on uvf.Id = mt.Obj%n"
			L" JOIN CmObject cmo on uvf.Id = cmo.Id%n"
			L" JOIN UserViewRec uvr on cmo.Owner$ = uvr.Id%n"
			L" JOIN Field$ fld on fld.id = uvf.Flid%n "
			L"WHERE uvf.Flid IN (SELECT Id FROM Field$ WHERE Class IN (%s))%n"
			L"    AND uvr.Clsid IN (%s)%n"
			L"    AND (mt.Ws = %d)%n"
			// We don't try to sort by Structured Text fields or "Big" String/Unicode fields.
			L"    AND (fld.DstCls IS NULL OR fld.DstCls <> %d)%n"
			L"    AND (fld.Type NOT IN (%d,%d,%d,%d))%n"
			L" ORDER BY uvf.Flid",
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
				if (ld.flid == kflidRnGenericRec_Title && ld.clid == kclidRnGenericRec)
					wsTitle = ld.wsMagic;
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
				bool fFound = false;
				for (iclid = 0; iclid < vclidTop.Size(); ++iclid)
				{
					if (hmclidld.Retrieve(vclidTop[iclid], &ldT))
					{
						fFound = true;
						break;
					}
				}
				if (!fFound)
					fFound = hmclidld.Retrieve(fd.clid, &ldT);
#ifdef LOG_FILTER_SQL
				if (fp)
					fprintf(fp,
					"fd.flid=%7d, .type=%2d, .clid=%4d, .clidDest=%4d; ld.ws=%2d; %S\n",
						fd.flid, fd.type, fd.clid, fd.clidDest,
						fFound ? ldT.wsMagic : 0,
						fFound ? ldT.stuLabel.Chars() : L"(NO LABEL - NOT STORED)");
#endif
				if (fFound)
				{
					if (fd.clid != kclidRnRoledPartic)
					{
						hmflidfd.Insert(fd.flid, fd, true);
						if (!setclidPoss.IsMember(fd.clid))
							vfd.Push(fd);
					}
				}
			}
#ifdef LOG_FILTER_SQL
			else if (fp)
			{
				fprintf(fp,
		"fd.flid = %7d, .type = %2d, .clid = %4d, .clidDest = %4d (NOT STORED - NO LABEL)\n",
					fd.flid, fd.type, fd.clid, fd.clidDest);
			}
#endif
			CheckHr(qodc->NextRow(&fMoreRows));
		}

		GetSortMenuRoles(vsmnRoles, plpi);

		// Add the two standard menu entries to the beginning of the Roled Participant submenu.
		qsmnPopup.Create();
		qsmnPopup->m_stuText.Load(kstidFltrAnyRoleMenuLabel);
		qsmnPopup->m_smnt = ksmntField;
		qsmnPopup->m_flid = kflidRnRoledPartic_Participants;
		qsmnPopup->m_proptype = kfptRoledParticipant;
		qsmnPopup->m_hvo = -1;
		vsmnRoles.Insert(0, qsmnPopup);
		qsmnPopup.Create();
		qsmnPopup->m_stuText.Load(kstidFltrNoRoleMenuLabel);
		qsmnPopup->m_smnt = ksmntField;
		qsmnPopup->m_flid = kflidRnRoledPartic_Participants;
		qsmnPopup->m_proptype = kfptRoledParticipant;
		qsmnPopup->m_hvo = 0;
		vsmnRoles.Insert(1, qsmnPopup);
		if (vsmnRoles.Size() > 2)
		{
			qsmnLeaf.Create();					// Separator line.
			qsmnLeaf->m_stuText.Clear();
			qsmnLeaf->m_smnt = ksmntLeaf;
			qsmnLeaf->m_flid = 0;
			qsmnLeaf->m_proptype = kcptNil;
			vsmnRoles.Insert(2, qsmnLeaf);
		}
	}
	catch (...)	// Was empty.
	{
		throw;	// For now we have nothing to add, so pass it on up.
	}
#ifdef LOG_FILTER_SQL
	if (fp)
	{
		fclose(fp);
		fp = NULL;
	}
#endif

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
			qsmnLeaf->m_wsMagic = ld.wsMagic;
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
			qsmnLeaf->m_wsMagic = ld.wsMagic;
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
		{
			qsmnLeaf->m_stuText = ld.stuLabel;
			qsmnLeaf->m_wsMagic = ld.wsMagic;
		}
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
		{
			qsmnLeaf->m_stuText = ld.stuLabel;
			qsmnLeaf->m_wsMagic = ld.wsMagic;
		}
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
		{
			qsmnLeaf->m_stuText = ld.stuLabel;
			qsmnLeaf->m_wsMagic = ld.wsMagic;
		}
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
		{
			qsmnLeaf->m_stuText = ld.stuLabel;
			qsmnLeaf->m_wsMagic = ld.wsMagic;
		}
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
		{
			qsmnLeaf->m_stuText = ld.stuLabel;
			qsmnLeaf->m_wsMagic = ld.wsMagic;
		}
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
		{
			qsmnLeaf->m_stuText = ld.stuLabel;
			qsmnLeaf->m_wsMagic = ld.wsMagic;
		}
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
		{
			qsmnLeaf->m_stuText = ld.stuLabel;
			qsmnLeaf->m_wsMagic = ld.wsMagic;
		}
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
		{
			qsmnLeaf->m_stuText = ld.stuLabel;
			qsmnLeaf->m_wsMagic = ld.wsMagic;
		}
	}
	SortMenuNode::AddSortedMenuNode(vsmnCustomItem, qsmnLeaf);

	// Finish filling in the Roled Participant popup menu entries.
	for (int i = 0; i < vsmnRoles.Size(); ++i)
	{
		if (vsmnRoles[i]->m_smnt == ksmntField)
			_CopyMenuNodeVector(vsmnRoles[i]->m_vsmnSubItems, vsmnPerson);
	}

	IFwMetaDataCachePtr qmdc;
	plpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);

	// Add the submenu elements.
	int ifd;
	for (ifd = 0; ifd < vfd.Size(); ++ifd)
	{
		int flid = vfd[ifd].flid;
		int clid = vfd[ifd].clid;
		int clidDest = vfd[ifd].clidDest;
		if (hmflidhmclidld.Retrieve(flid, &hmclidld))
		{
			bool fFound = false;
			for (int i = 0; i < vclidTop.Size(); ++i)
			{
				if (hmclidld.Retrieve(vclidTop[i], &ld))
				{
					fFound = true;
					break;
				}
			}
			if (!fFound)
			{
				Assert(false);
				continue;
			}
		}
		else
		{
			Assert(false);
			continue;
		}
		if (flid == kflidRnGenericRec_PhraseTags || flid == kflidRnGenericRec_SubRecords)
		{
			// We don't sort on overlays or subentries per se.
			continue;
		}
		else if (clidDest == kclidRnRoledPartic)
		{
			// Build and attach the submenu for "Roled Participant" nodes.
			if (vsmnRoles.Size())
			{
				qsmnPopup.Create();
				qsmnPopup->m_stuText = ld.stuLabel;
				qsmnPopup->m_wsMagic = ld.wsMagic;
				qsmnPopup->m_smnt = ksmntField;
				qsmnPopup->m_flid = flid;
				_CopyMenuNodeVector(qsmnPopup->m_vsmnSubItems, vsmnRoles);
				qsmnEvent->AddSortedSubItem(qsmnPopup);
			}
		}
		else if (clidDest == kclidCmPossibility ||
			clidDest == kclidCmLocation ||
			clidDest == kclidCmAnthroItem ||
			clidDest == kclidCmPerson ||
			clidDest == kclidCmCustomItem)
		{
			qsmnPopup.Create();
			qsmnPopup->m_stuText = ld.stuLabel;
			qsmnPopup->m_wsMagic = ld.wsMagic;
			qsmnPopup->m_smnt = ksmntField;
			qsmnPopup->m_flid = flid;
			// Mark possibility list nodes.
			qsmnPopup->m_proptype = kfptPossList;
			qsmnPopup->m_hvo = PossListHvoForFlid(pdbi, flid);
			Assert(qsmnPopup->m_hvo);
#ifdef DEBUG
			switch (flid)
			{
			case kflidRnGenericRec_Confidence:
				Assert(qsmnPopup->m_hvo == vhvoPssl[RnLpInfo::kpidPsslCon]);
				break;
			case kflidRnGenericRec_Restrictions:
				Assert(qsmnPopup->m_hvo == vhvoPssl[RnLpInfo::kpidPsslRes]);
				break;
			case kflidRnAnalysis_Status:
				Assert(qsmnPopup->m_hvo == vhvoPssl[RnLpInfo::kpidPsslAna]);
				break;
			case kflidRnEvent_Type:
				Assert(qsmnPopup->m_hvo == vhvoPssl[RnLpInfo::kpidPsslTyp]);
				break;
			case kflidRnEvent_Weather:
				Assert(qsmnPopup->m_hvo == vhvoPssl[RnLpInfo::kpidPsslWea]);
				break;
			case kflidRnEvent_TimeOfEvent:
				Assert(qsmnPopup->m_hvo == vhvoPssl[RnLpInfo::kpidPsslTim]);
				break;
			case kflidRnGenericRec_AnthroCodes:
				Assert(qsmnPopup->m_hvo == vhvoPssl[RnLpInfo::kpidPsslAnit]);
				break;
			case kflidRnEvent_Locations:
				Assert(qsmnPopup->m_hvo == vhvoPssl[RnLpInfo::kpidPsslLoc]);
				break;
			case kflidRnGenericRec_Researchers:
				Assert(qsmnPopup->m_hvo == vhvoPssl[RnLpInfo::kpidPsslPeo]);
				break;
			case kflidRnEvent_Sources:
				Assert(qsmnPopup->m_hvo == vhvoPssl[RnLpInfo::kpidPsslPeo]);
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

			switch (clid)
			{
			case kclidRnGenericRec:
				qsmnGeneric->AddSortedSubItem(qsmnPopup);
				break;
			case kclidRnAnalysis:
				qsmnAnalysis->AddSortedSubItem(qsmnPopup);
				break;
			case kclidRnEvent:
				qsmnEvent->AddSortedSubItem(qsmnPopup);
				break;
			}
		}
		else
		{
			qsmnLeaf.Create();
			qsmnLeaf->m_stuText = ld.stuLabel;
			qsmnLeaf->m_wsMagic = ld.wsMagic;
			qsmnLeaf->m_smnt = ksmntLeaf;
			qsmnLeaf->m_flid = flid;
			if (clidDest == kclidStText)
			{
				// Mark structured text nodes.
				qsmnLeaf->m_proptype = kfptStText;
			}
			else if (clidDest == kclidRnGenericRec || clidDest == kclidRnEvent ||
				clidDest == kclidRnAnalysis)
			{
				// Mark cross reference type nodes (subentries are excluded above).
				switch (vfd[ifd].type)
				{
				case kcptReferenceAtom:
					qsmnLeaf->m_proptype = kfptCrossRef;
					break;
				case kcptReferenceCollection:
				case kcptReferenceSequence:
					qsmnLeaf->m_proptype = kfptCrossRefList;
					break;
				default:
					Assert(false);
					break;
				}
			}
			else
			{
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
			}
			// Store this menu node in the appropriate master menu(s).
			switch (clid)
			{
			case kclidRnGenericRec:
				qsmnGeneric->AddSortedSubItem(qsmnLeaf);
				break;
			case kclidRnAnalysis:
				qsmnAnalysis->AddSortedSubItem(qsmnLeaf);
				break;
			case kclidRnEvent:
				qsmnEvent->AddSortedSubItem(qsmnLeaf);
				break;
			}
		}
	}

	// Go through all the menu items recursively and assign the type for each field.
	int csmn = m_vsmn.Size();
	for (int ismn = 0; ismn < csmn; ismn++)
		_AssignFieldTypes(qmdc, m_vsmn[ismn]);

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
			kclidRnGenericRec, kflidRnGenericRec_DateCreated);
		m_asiDefault.m_wsPrimary = 0;
		m_asiDefault.m_collPrimary = 0;
		m_asiDefault.m_fPrimaryReverse = false;

		m_asiDefault.m_stuSecondaryField.Format(L"%d,%d",
			kclidRnGenericRec, kflidRnGenericRec_Title);
		// Find the writing system/collation information.
		switch (wsTitle)
		{
		case 0:
		case kwsAnals:
		case kwsAnalVerns:
		case kwsAnal:
			m_asiDefault.m_wsSecondary = plpi->AnalWs();
			break;
		case kwsVerns:
		case kwsVernAnals:
		case kwsVern:
			m_asiDefault.m_wsSecondary = plpi->VernWs();
			break;
		default:
			m_asiDefault.m_wsSecondary = wsTitle;
			break;
		}
		m_asiDefault.m_collSecondary = 0;		// Default collation.
		m_asiDefault.m_fSecondaryReverse = false;

		m_asiDefault.m_stuTertiaryField.Clear();
		m_asiDefault.m_wsTertiary = 0;
		m_asiDefault.m_collTertiary = 0;
		m_asiDefault.m_fTertiaryReverse = false;

		m_asiDefault.m_clidRec = kclidRnGenericRec;
		m_asiDefault.m_fMultiOutput = false;
	}
	// Fill the additional information needed for sorting by cross reference fields.
	if (!m_asiXref.m_stuName.Length())
	{
		m_asiXref.m_stuName.Assign(L"ADDITIONAL INFO FOR CROSS REFERENCES");
		m_asiXref.m_fIncludeSubfields = false;
		m_asiXref.m_hvo = 0;

		m_asiXref.m_stuPrimaryField.Format(L"%d", kflidRnGenericRec_Title);
		m_asiXref.m_wsPrimary = m_asiDefault.m_wsSecondary;
		m_asiXref.m_collPrimary = m_asiDefault.m_collSecondary;
		m_asiXref.m_fPrimaryReverse = false;

		m_asiXref.m_stuSecondaryField.Format(L"%d", kflidRnGenericRec_DateCreated);
		m_asiXref.m_wsSecondary = 0;
		m_asiXref.m_collSecondary = 0;
		m_asiXref.m_fSecondaryReverse = false;

		m_asiXref.m_stuTertiaryField.Clear();
		m_asiXref.m_wsTertiary = 0;
		m_asiXref.m_collTertiary = 0;
		m_asiXref.m_fTertiaryReverse = false;
		m_asiXref.m_clidRec = kclidRnGenericRec;
	}
	return &m_vsmn;
}

/*----------------------------------------------------------------------------------------------
	This reloads the main records, and reapplies any active filter or sort method.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::LoadData()
{
	m_vhcRecords.Clear();
	AssertPtr(m_qcvd);
	HVO hvoRnId = dynamic_cast<RnLpInfo *>(m_qlpi.Ptr())->GetRnId();
	m_qcvd->SetTags(kflidRnResearchNbk_Records, kflidRnGenericRec_DateCreated);
	m_qcvd->LoadMainItems(hvoRnId, m_vhcRecords, &m_asiDefault, &m_vskhSortKeys);
	m_vhcFilteredRecords = m_vhcRecords;
	if (m_ihvoCurr >= m_vhcFilteredRecords.Size())
		m_ihvoCurr = m_vhcFilteredRecords.Size() - 1;

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
	RnFilterXrefUtil rxref;
	ApplyFilterAndSort(iflt, fNoPrompt, isrt, &rxref);
}


/*----------------------------------------------------------------------------------------------
	Check for an empty data notebook, and prompt the user for what to do if it is empty.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::CheckForNoRecords(const achar * pszProjName)
{
	if (!RawRecordCount())
	{
		// Empty database.
		RnEmptyNotebookDlgPtr qremp;
		qremp.Create();
		qremp->SetProject(pszProjName);
		AfApp::Papp()->EnableMainWindows(false);
		int ctid = qremp->DoModal(Hwnd());
		AfApp::Papp()->EnableMainWindows(true);
		switch (ctid)
		{
		case kctidEmptyNewEvent:
			::SendMessage(Hwnd(), WM_COMMAND, kcidInsEntryEvent, 0);
			break;
		case kctidEmptyNewAnalysis:
			::SendMessage(Hwnd(), WM_COMMAND, kcidInsEntryAnal, 0);
			break;
		case kctidEmptyImport:
			::SendMessage(Hwnd(), WM_COMMAND, kcidFileImpt, 0);
			break;
		case kctidCancel:
			// PostMessage must be used here instead of SendMessage or we'll get a crash when
			// you delete the last record and then choose Exit from this dialog. The crash
			// results from SendMessage closing things down before the right+click context
			// menu (in AfDeSplitChild::OnContextMenu) has been adequately destroyed.
			::PostMessage(Hwnd(), WM_COMMAND, kcidFileExit, 0);
			break;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Check whether the anthropology list has been initialized.  If not, initialize it and create
	the corresponding overlays.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::CheckAnthroList()
{
	AssertPtr(m_qlpi);

	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	IOleDbEncapPtr qode;
	pdbi->GetDbAccess(&qode);
	AssertPtr(qode.Ptr());

	IFwCheckAnthroListPtr qfcal;
	qfcal.CreateInstance(CLSID_FwCheckAnthroList);
	SmartBstr sbstrHelpFile(AfApp::Papp()->GetHelpFile());
	SmartBstr sbstr(m_qlpi->PrjName());
	CheckHr(qfcal->put_HelpFilename(sbstrHelpFile));
	CheckHr(qfcal->CheckAnthroList(qode, (DWORD)m_hwnd, sbstr, pdbi->UserWs()));
}


/*----------------------------------------------------------------------------------------------
	Bring up another top-level window with the same view.

	@param pcmd menu command

	@return true
----------------------------------------------------------------------------------------------*/
bool RnMainWnd::CmdWndNew(Cmd * pcmd)
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
		{
			pdfe->SaveEdit();
			pdfe->SaveFullCursorInfo();
		}
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
	wcs.InitMain(_T("RnMainWnd"));

	PrepareNewWindowLocation();

	RnMainWndPtr qrnmw;
	try
	{
		qrnmw.Create();
		qrnmw->Init(m_qlpi);
		Set<int> siselView;
		pvwbrs->GetSelection(m_ivblt[kvbltView], siselView);
		Assert(siselView.Size() == 1);
		int nView = *siselView.Begin();
		qrnmw->SetStartupInfo(m_vhvoPath.Begin(), m_vhvoPath.Size(), m_vflidPath.Begin(),
			m_vflidPath.Size(), m_ichCur, nView);
		qrnmw->CreateHwnd(wcs);
	}
	catch (...)
	{
		qrnmw.Clear();
		StrApp str(kstidOutOfResources);
		::MessageBox(NULL, str.Chars(), _T(""), MB_ICONSTOP | MB_OK);
		return true;
	}

	// Split the status bar into five sections of varying widths:
	//	1. record id
	//	2. progress / info
	//	3. sort
	//	4. filter
	//	5. count
	AfStatusBarPtr qstbr = qrnmw->GetStatusBarWnd();
	Assert(qstbr);
	qstbr->InitializePanes();
	qrnmw->UpdateStatusBar();

	AfMdiClientWndPtr qmdic = qrnmw->GetMdiClientWnd();
	AssertPtr(qmdic);
	qafcrw = dynamic_cast<AfClientRecWnd *>(qmdic->GetCurChild());
	AssertPtr(qafcrw);

	// Restore the filter selection
	pvwbrs = qrnmw->GetViewBarShell();
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
bool RnMainWnd::CmdViewExpMenu(Cmd * pcmd)
{
	AssertPtr(pcmd);

	// Don't allow menus if we can't close the current editor.
	if (!IsOkToChange())
		return false;

	int ma = pcmd->m_rgn[0];
	if (ma == AfMenuMgr::kmaExpandItem)
	{
		// We need to expand the dummy menu item.
		HMENU hmenu = (HMENU)pcmd->m_rgn[1];
		int imni = pcmd->m_rgn[2];
		int & cmniAdded = pcmd->m_rgn[3];

		switch (pcmd->m_cid)
		{
		case kcidExpViews:
			{
				int cview = m_qmdic->GetChildCount();
				for (int iview = 0; iview < cview; iview++)
				{
					AfClientWnd * pafcw = m_qmdic->GetChildFromIndex(iview);
					AssertPtr(pafcw);
					::InsertMenu(hmenu, imni + iview, MF_BYPOSITION, kcidMenuItemDynMin + iview,
						pafcw->GetViewName());
				}
				cmniAdded = cview;
				return true;
			}

		case kcidExpFilters:
			{
				AfDbInfo * pdbi = m_qlpi->GetDbInfo();
				AssertPtr(pdbi);

				StrApp str(kstidNoFilterHotKey);
				::InsertMenu(hmenu, imni, MF_BYPOSITION, kcidMenuItemDynMin, str.Chars());
				cmniAdded = 1;

				int cflt = pdbi->GetFilterCount();
				if (cflt)
				{
					::InsertMenu(hmenu, imni + 1, MF_BYPOSITION, MF_SEPARATOR, NULL);
					cmniAdded++;
				}

				for (int iflt = 0; iflt < cflt; iflt++)
				{
					AppFilterInfo & afi = pdbi->GetFilterInfo(iflt);
					if (afi.m_clidRec == GetRecordClid())
					{
						str = afi.m_stuName.Chars();
						::InsertMenu(hmenu, imni + iflt + cmniAdded, MF_BYPOSITION,
							kcidMenuItemDynMin + iflt + 1, str.Chars());
					}
				}
				cmniAdded += cflt;
				return true;
			}

		case kcidExpSortMethods:
			{
				AfDbInfo * pdbi = m_qlpi->GetDbInfo();
				AssertPtr(pdbi);

				StrApp str(kstidDefaultSortHotKey);
				::InsertMenu(hmenu, imni, MF_BYPOSITION, kcidMenuItemDynMin, str.Chars());
				cmniAdded = 1;

				int csrt = pdbi->GetSortCount();
				if (csrt)
				{
					::InsertMenu(hmenu, imni + 1, MF_BYPOSITION, MF_SEPARATOR, NULL);
					cmniAdded++;
				}
				for (int isrt = 0; isrt < csrt; isrt++)
				{
					AppSortInfo & asi = pdbi->GetSortInfo(isrt);
					if (asi.m_clidRec == GetRecordClid())
					{
						StrApp str(asi.m_stuName.Chars());
						::InsertMenu(hmenu, imni + isrt + cmniAdded, MF_BYPOSITION,
							kcidMenuItemDynMin + isrt + 1, str.Chars());
					}
				}
				cmniAdded += csrt;
				return true;
			}

		case kcidExpOverlays:
			{
				StrApp str(kstidNoOverlayHotKey);
				::InsertMenu(hmenu, imni, MF_BYPOSITION, kcidMenuItemDynMin, str.Chars());
				cmniAdded = 1;

				int covr = m_qlpi->GetOverlayCount();
				if (covr)
				{
					::InsertMenu(hmenu, imni + 1, MF_BYPOSITION, MF_SEPARATOR, NULL);
					cmniAdded++;
				}
				for (int iovr = 0; iovr < covr; iovr++)
				{
					AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(iovr);
					StrApp str(aoi.m_stuName.Chars());
					::InsertMenu(hmenu, imni + iovr + cmniAdded, MF_BYPOSITION,
						kcidMenuItemDynMin + iovr + 1, str.Chars());
				}
				cmniAdded += covr;
				return true;
			}
		}
	}
	else if (ma == AfMenuMgr::kmaGetStatusText)
	{
		// We need to return the text for the expanded menu item.
		//	m_rgn[1] holds the index of the selected item.
		//	m_rgn[2] holds a pointer to the string to set

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
				int iflt = pdbi->ComputeFilterIndex(imni, GetRecordClid());
				Assert(iflt >= 0);
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
			else
			{
				AfDbInfo * pdbi = m_qlpi->GetDbInfo();
				AssertPtr(pdbi);
				int isrt = pdbi->ComputeSortIndex(imni, GetRecordClid());
				Assert(isrt >= 0);
				AppSortInfo & asi = pdbi->GetSortInfo(isrt);
				StrApp str(asi.m_stuName.Chars());
				StrApp strFormat(kstidSelectSortMethod);
				pstr->Format(strFormat.Chars(), str.Chars());
			}
			return true;
		case kcidExpOverlays:
			if (imni == 0)
			{
				if (!AfUtil::GetResourceStr(krstStatusEnabled, kcidViewOlaysNone, *pstr))
					return false;
			}
			else
			{
				AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(imni - 1);
				StrApp str(aoi.m_stuName.Chars());
				StrApp strFormat(kstidSelectOverlay);
				pstr->Format(strFormat.Chars(), str.Chars());
			}
			return true;
		}
	}
	else if (ma == AfMenuMgr::kmaDoCommand)
	{
		// The user selected an expanded menu item, so perform the command now.
		//	m_rgn[1] holds the menu handle.
		//	m_rgn[2] holds the index of the selected item.

		Set<int> sisel;

		int iitem = pcmd->m_rgn[2];
		switch (pcmd->m_cid)
		{
		case kcidExpViews:
			sisel.Insert(iitem);
			m_qvwbrs->SetSelection(m_ivblt[kvbltView], sisel);
			break;
		case kcidExpFilters:
			sisel.Insert(iitem);
			m_qvwbrs->SetSelection(m_ivblt[kvbltFilter], sisel);
			break;
		case kcidExpSortMethods:
			sisel.Insert(iitem);
			m_qvwbrs->SetSelection(m_ivblt[kvbltSort], sisel);
			break;
		case kcidExpOverlays:
			m_qvwbrs->GetSelection(m_ivblt[kvbltOverlay], sisel);
			if (sisel.IsMember(iitem))
				sisel.Delete(iitem);
			else
				sisel.Insert(iitem);
			m_qvwbrs->SetSelection(m_ivblt[kvbltOverlay], sisel);
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
bool RnMainWnd::CmsViewExpMenu(CmdState & cms)
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
	case kcidExpOverlays:
		m_qvwbrs->GetSelection(m_ivblt[kvbltOverlay], sisel);
		break;
	}

	cms.SetCheck(sisel.IsMember(iitem));

	return true;
}


/*----------------------------------------------------------------------------------------------
	Get the ws value for the given possibility list.
----------------------------------------------------------------------------------------------*/
static int GetPossListWs(int ihvo)
{
	if (ihvo == RnLpInfo::kpidPsslLoc || ihvo == RnLpInfo::kpidPsslPeo)
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
	@param pvuvs This should normally be NULL. vuvs value is only used when called from TlsOptView
----------------------------------------------------------------------------------------------*/
void RnMainWnd::MakeNewView(UserViewType vwt, AfLpInfo * plpi, UserViewSpec * puvs,
		UserViewSpecVec * pvuvs)
{
	AssertPtr(plpi);

	RecordSpecPtr qrsp;
	Vector<HVO> & vhvoPssl = plpi->GetPsslIds();
	AfDbInfoPtr qdbi = plpi->GetDbInfo();
	AssertPtr(qdbi);

	puvs->m_vwt = vwt;
	puvs->m_fv = true;
	puvs->m_nMaxLines = 5;
	puvs->m_fIgnorHier = false;
	puvs->m_ws = UserWs();
	puvs->m_guid = CLSID_ResearchNotebook;
	IFwMetaDataCachePtr qmdc;
	qdbi->GetFwMetaDataCache(&qmdc);

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
	switch (vwt)
	{
	default:
		Assert(false);
		break;
	case kvwtBrowse:
		// Browse Events and Analyses
		qrsp.Create();
		qrsp->Init(puvs, 0, 0, vwt, qwsf);	// Dummy id used for Browse display.
		// DateOfEvent
		qrsp->AddField(true, kstidTlsOptEDateOfEvent, kflidRnEvent_DateOfEvent,
			kftGenDate, kwsAnal, kstidRnEvent_DateOfEvent, kFTVisAlways, kFTReqWs);
		// Title
		qrsp->AddField(true, kstidTlsOptETitle, kflidRnGenericRec_Title,
			kftString, kwsAnal, kstidRnGenericRec_Title, kFTVisAlways, kFTReqWs,
			stuTitleChars.Chars());
		// AnthroCodes
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslAnit);
		qrsp->AddPossField(true, kstidTlsOptAnthroCodes, kflidRnGenericRec_AnthroCodes,
			kftRefSeq, kstidRnGenericRec_AnthroCodes,
			vhvoPssl[RnLpInfo::kpidPsslAnit], kpntNameAndAbbrev, false, false, wsPssl);
		// Locations
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslLoc);
		qrsp->AddPossField(true, kstidTlsOptELocations, kflidRnEvent_Locations,
			kftRefSeq, kstidRnEvent_Locations,
			vhvoPssl[RnLpInfo::kpidPsslLoc], kpntName, false, false,
			wsPssl, kFTVisAlways, kFTReqWs);
		// Sources
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslPeo);
		qrsp->AddPossField(true, kstidTlsOptESources, kflidRnEvent_Sources,
			kftRefSeq, kstidRnEvent_Sources,
			vhvoPssl[RnLpInfo::kpidPsslPeo], kpntName, false, false,
			wsPssl);
		// SeeAlso
		qrsp->AddField(true, kstidTlsOptESeeAlso, kflidRnGenericRec_SeeAlso,
			kftObjRefSeq, kwsAnal, kstidRnGenericRec_SeeAlso, kFTVisAlways, kFTReqNotReq,
			L"Internal Link");
		// FurtherQuestions
		qrsp->AddField(true, kstidTlsOptAFurtherQuestions,
			kflidRnGenericRec_FurtherQuestions,
			kftStText, kwsAnal, kstidRnGenericRec_FurtherQuestions);
		// Finish it off
		qrsp->SetMetaNames(qmdc);
		plpi->GetDbInfo()->CompleteBrowseRecordSpec(puvs);

		// Browse Roled Participants.
		// Note: This must be included in order to load the reference properties, even
		// though we don't actually show these fields in the Tools...Options dialog.
		qrsp.Create();
		qrsp->Init(puvs, kclidRnRoledPartic, 0, vwt, qwsf);
		// Participants
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslPeo);
		qrsp->AddPossField(true, kstidTlsOptParticipants,
			kflidRnRoledPartic_Participants,
			kftRefSeq, 0,
			vhvoPssl[RnLpInfo::kpidPsslPeo],
			kpntName, false, false, wsPssl);
		// Role
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslRol);
		qrsp->AddPossField(true, kstidTlsOptRole, kflidRnRoledPartic_Role,
			kftRefAtomic, 0,
			vhvoPssl[RnLpInfo::kpidPsslRol], kpntName, false, false, wsPssl);
		// Finish it off
		qrsp->SetMetaNames(qmdc);
		break;	// End of kvwtBrowse
	case kvwtDE:
		// DE RnEvent.
		qrsp.Create();
		qrsp->Init(puvs, kclidRnEvent, 0, vwt, qwsf);
		// Title
		qrsp->AddField(true, kstidTlsOptETitle, kflidRnGenericRec_Title,
			kftString, kwsAnal, kstidRnGenericRec_Title,
			kFTVisAlways, kFTReqWs, stuTitleChars.Chars());
		// Restrictions
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslRes);
		qrsp->AddPossField(true, kstidTlsOptERestrictions, kflidRnGenericRec_Restrictions,
			kftRefSeq, kstidRnGenericRec_Restrictions,
			vhvoPssl[RnLpInfo::kpidPsslRes], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// DateOfEvent
		qrsp->AddField(true, kstidTlsOptEDateOfEvent, kflidRnEvent_DateOfEvent,
			kftGenDate, kwsAnal, kstidRnEvent_DateOfEvent,
			kFTVisAlways, kFTReqWs);
		// TimeOfEvent
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslTim);
		qrsp->AddPossField(true, kstidTlsOptETimeOfEvent, kflidRnEvent_TimeOfEvent,
			kftRefSeq, kstidRnEvent_TimeOfEvent,
			vhvoPssl[RnLpInfo::kpidPsslTim], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Researchers
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslPeo);
		qrsp->AddPossField(true, kstidTlsOptEResearchers, kflidRnGenericRec_Researchers,
			kftRefSeq, kstidRnGenericRec_Researchers,
			vhvoPssl[RnLpInfo::kpidPsslPeo], kpntName, false, false,
			wsPssl, kFTVisAlways, kFTReqWs);
		// Sources
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslPeo);
		qrsp->AddPossField(true, kstidTlsOptESources, kflidRnEvent_Sources,
			kftRefSeq, kstidRnEvent_Sources,
			vhvoPssl[RnLpInfo::kpidPsslPeo], kpntName, false, false,
			wsPssl);
		// Participants
		qrsp->AddField(true, kstidTlsOptParticipants, kflidRnEvent_Participants,
			kftExpandable, kwsVernAnals, kstidRnEvent_Participants, kFTVisIfData);
		// Confidence
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslCon);
		qrsp->AddPossField(true, kstidTlsOptEConfidence, kflidRnGenericRec_Confidence,
			kftRefCombo, kstidRnGenericRec_Confidence,
			vhvoPssl[RnLpInfo::kpidPsslCon], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Locations
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslLoc);
		qrsp->AddPossField(true, kstidTlsOptELocations, kflidRnEvent_Locations,
			kftRefSeq, kstidRnEvent_Locations,
			vhvoPssl[RnLpInfo::kpidPsslLoc], kpntName, false, false,
			wsPssl, kFTVisAlways, kFTReqWs);
		// Type
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslTyp);
		qrsp->AddPossField(true, kstidTlsOptEType, kflidRnEvent_Type,
			kftRefAtomic, kstidRnEvent_Type,
			vhvoPssl[RnLpInfo::kpidPsslTyp], kpntName, false, false,
			wsPssl, kFTVisAlways, kFTReqWs);
		// Description
		qrsp->AddField(true, kstidTlsOptEDescription, kflidRnEvent_Description,
			kftStText, kwsAnal, kstidRnEvent_Description, kFTVisAlways, kFTReqWs);
		// AnthroCodes
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslAnit);
		qrsp->AddPossField(true, kstidTlsOptAnthroCodes, kflidRnGenericRec_AnthroCodes,
			kftRefSeq, kstidRnGenericRec_AnthroCodes,
			vhvoPssl[RnLpInfo::kpidPsslAnit], kpntNameAndAbbrev, false, false, wsPssl);
		// SeeAlso
		qrsp->AddField(true, kstidTlsOptESeeAlso, kflidRnGenericRec_SeeAlso,
			kftObjRefSeq, kwsAnal, kstidRnGenericRec_SeeAlso, kFTVisAlways, kFTReqNotReq,
			L"Internal Link");
		// FurtherQuestions
		qrsp->AddField(true, kstidTlsOptAFurtherQuestions,
			kflidRnGenericRec_FurtherQuestions,
			kftStText, kwsAnal, kstidRnGenericRec_FurtherQuestions);
		// Weather
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslWea);
		qrsp->AddPossField(true, kstidTlsOptEWeather, kflidRnEvent_Weather,
			kftRefSeq, kstidRnEvent_Weather,
			vhvoPssl[RnLpInfo::kpidPsslWea], kpntName, true, false,
			wsPssl, kFTVisIfData);
		// ExternalMaterials
		qrsp->AddField(true, kstidTlsOptEExternalMaterials,
			kflidRnGenericRec_ExternalMaterials,
			kftStText, kwsAnal, kstidRnGenericRec_ExternalMaterials,
			kFTVisIfData);
		// PersonalNotes
		qrsp->AddField(true, kstidTlsOptEPersonalNotes,
			kflidRnEvent_PersonalNotes,
			kftStText, kwsAnal, kstidRnEvent_PersonalNotes,
			kFTVisIfData);
		// Now add Custom fields here.
		if (!fskipCustFlds)
		{
			// Go through the RecordSpecs looking for Custom Fields.
			ClevRspMap::iterator ithmclevrspLim = vuvs[iuvs]->m_hmclevrsp.End();
			for (ClevRspMap::iterator it = vuvs[iuvs]->m_hmclevrsp.Begin();
				it != ithmclevrspLim; ++it)
			{
				ClsLevel clev = it.GetKey();
				if (clev.m_clsid != kclidRnEvent)
					continue;
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
		// DateCreated
		qrsp->AddField(true, kstidTlsOptEDateCreated, kflidRnGenericRec_DateCreated,
			kftDateRO, kwsAnal, kstidRnGenericRec_DateCreated, kFTVisNever);
		// DateModified
		qrsp->AddField(true, kstidTlsOptEDateModified, kflidRnGenericRec_DateModified,
			kftDateRO, kwsAnal, kstidRnGenericRec_DateModified, kFTVisNever);
/*
		// VersionHistory
		qrsp->AddField(true, kstidTlsOptEVersionHistory, kflidRnGenericRec_VersionHistory,
			kftStText, kwsAnal, kstidRnGenericRec_VersionHistory, kFTVisNever);
*/
		// SubRecords
		qrsp->AddHierField(true, kstidTlsOptSubentries, kflidRnGenericRec_SubRecords,
			kwsAnal, kstidRnEvent_SubRecords, konsNumDot);
		// Finish it off
		qrsp->SetMetaNames(qmdc);

		// DE RnAnalysis.
		qrsp.Create();
		qrsp->Init(puvs, kclidRnAnalysis, 0, vwt, qwsf);
		// Title
		qrsp->AddField(true, kstidTlsOptATitle, kflidRnGenericRec_Title,
			kftString, kwsAnal, kstidRnGenericRec_Title,
			kFTVisAlways, kFTReqWs, stuTitleChars.Chars());
		// Researchers
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslPeo);
		qrsp->AddPossField(true, kstidTlsOptAResearchers, kflidRnGenericRec_Researchers,
			kftRefSeq, kstidRnGenericRec_Researchers,
			vhvoPssl[RnLpInfo::kpidPsslPeo], kpntName, false, false,
			wsPssl, kFTVisAlways, kFTReqWs);
		// Hypothesis
		qrsp->AddField(true, kstidTlsOptAHypothesis, kflidRnAnalysis_Hypothesis,
			kftStText, kwsAnal, kstidRnAnalysis_Hypothesis, kFTVisIfData);
		// Discussion
		qrsp->AddField(true, kstidTlsOptADiscussion, kflidRnAnalysis_Discussion,
			kftStText, kwsAnal, kstidRnAnalysis_Discussion, kFTVisAlways, kFTReqWs);
		// AnthroCodes
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslAnit);
		qrsp->AddPossField(true, kstidTlsOptAnthroCodes, kflidRnGenericRec_AnthroCodes,
			kftRefSeq, kstidRnGenericRec_AnthroCodes,
			vhvoPssl[RnLpInfo::kpidPsslAnit], kpntNameAndAbbrev, false, false, wsPssl);
		// Conclusions
		qrsp->AddField(true, kstidTlsOptAConclusions, kflidRnAnalysis_Conclusions,
			kftStText, kwsAnal, kstidRnAnalysis_Conclusions, kFTVisIfData);
		// SupportingEvidence
		qrsp->AddField(true, kstidTlsOptASupportingEvidence, kflidRnAnalysis_SupportingEvidence,
			kftObjRefSeq, kwsAnal, kstidRnAnalysis_SupportingEvidence,
			kFTVisIfData, kFTReqNotReq, L"Internal Link");
		// CounterEvidence
		qrsp->AddField(true, kstidTlsOptACounterEvidence, kflidRnAnalysis_CounterEvidence,
			kftObjRefSeq, kwsAnal, kstidRnAnalysis_CounterEvidence,
			kFTVisIfData, kFTReqNotReq, L"Internal Link");
		// SupersededBy
		qrsp->AddField(true, kstidTlsOptASupersededBy, kflidRnAnalysis_SupersededBy,
			kftObjRefSeq, kwsAnal, kstidRnAnalysis_SupersededBy,
			kFTVisIfData, kFTReqNotReq, L"Internal Link");
		// SeeAlso
		qrsp->AddField(true, kstidTlsOptASeeAlso, kflidRnGenericRec_SeeAlso,
			kftObjRefSeq, kwsAnal, kstidRnGenericRec_SeeAlso, kFTVisIfData, kFTReqNotReq,
			L"Internal Link");
		// Status
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslAna);
		qrsp->AddPossField(true, kstidTlsOptAStatus, kflidRnAnalysis_Status,
			kftRefCombo, kstidRnAnalysis_Status,
			vhvoPssl[RnLpInfo::kpidPsslAna], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Confidence
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslCon);
		qrsp->AddPossField(true, kstidTlsOptEConfidence, kflidRnGenericRec_Confidence,
			kftRefCombo, kstidRnGenericRec_Confidence,
			vhvoPssl[RnLpInfo::kpidPsslCon], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Restrictions
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslRes);
		qrsp->AddPossField(true, kstidTlsOptARestrictions, kflidRnGenericRec_Restrictions,
			kftRefSeq, kstidRnGenericRec_Restrictions,
			vhvoPssl[RnLpInfo::kpidPsslRes], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// ExternalMaterials
		qrsp->AddField(true, kstidTlsOptEExternalMaterials,
			kflidRnGenericRec_ExternalMaterials,
			kftStText, kwsAnal, kstidRnGenericRec_ExternalMaterials,
			kFTVisIfData);
		// FurtherQuestions
		qrsp->AddField(true, kstidTlsOptAFurtherQuestions,
			kflidRnGenericRec_FurtherQuestions,
			kftStText, kwsAnal, kstidRnGenericRec_FurtherQuestions);
		// ResearchPlan
		qrsp->AddField(true, kstidTlsOptAResearchPlan,
			kflidRnAnalysis_ResearchPlan,
			kftStText, kwsAnal, kstidRnAnalysis_ResearchPlan,
			kFTVisIfData);
		// Now add Custom fields here.
		if (!fskipCustFlds)
		{
			// Go through the RecordSpecs looking for Custom Fields.
			ClevRspMap::iterator ithmclevrspLim = vuvs[iuvs]->m_hmclevrsp.End();
			for (ClevRspMap::iterator it = vuvs[iuvs]->m_hmclevrsp.Begin();
				it != ithmclevrspLim; ++it)
			{
				ClsLevel clev = it.GetKey();
				if (clev.m_clsid != kclidRnAnalysis)
					continue;
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
					qbspNew->m_eVisibility = kFTVisAlways;
					qbspNew->m_fRequired = kFTReqNotReq;
					qrsp->m_vqbsp.Push(qbspNew);
				}
			}
		}
		// DateCreated
		qrsp->AddField(true, kstidTlsOptEDateCreated, kflidRnGenericRec_DateCreated,
			kftDateRO, kwsAnal, kstidRnGenericRec_DateCreated, kFTVisNever);
		// DateModified
		qrsp->AddField(true, kstidTlsOptEDateModified, kflidRnGenericRec_DateModified,
			kftDateRO, kwsAnal, kstidRnGenericRec_DateModified, kFTVisNever);
/*
		// VersionHistory
		qrsp->AddField(true, kstidTlsOptAVersionHistory, kflidRnGenericRec_VersionHistory,
			kftStText, kwsAnal, kstidRnGenericRec_VersionHistory, kFTVisNever);
*/
		// SubRecords
		qrsp->AddHierField(true, kstidTlsOptASubentries, kflidRnGenericRec_SubRecords,
			kwsAnal, kstidRnAnalysis_SubRecords, konsNumDot);
		// Finish it off
		qrsp->SetMetaNames(qmdc);

		// Data Entry Roled Participants.
		// Note: This must be included in order to load the reference properties, even
		// though we don't actually show these fields in the Tools...Options dialog.
		qrsp.Create();
		qrsp->Init(puvs, kclidRnRoledPartic, 0, vwt, qwsf);
		// Participants
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslPeo);
		qrsp->AddPossField(true, kstidTlsOptParticipants, kflidRnRoledPartic_Participants,
			kftRefSeq, kstidRnRoledPartic_Participants,
			vhvoPssl[RnLpInfo::kpidPsslPeo],
			kpntName, false, false, wsPssl);
		// Role
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslRol);
		qrsp->AddPossField(true, kstidTlsOptRole, kflidRnRoledPartic_Role,
			kftRefAtomic, kstidRnRoledPartic_Role,
			vhvoPssl[RnLpInfo::kpidPsslRol], kpntName, false, false, wsPssl);
		// Finish it off
		qrsp->SetMetaNames(qmdc);
		break;	// End of kvwtDE
	case kvwtDoc:
		// Document RnEvent.
		qrsp.Create();
		qrsp->Init(puvs, kclidRnEvent, 0, vwt, qwsf);
		// Title block.
		// Title
		qrsp->AddField(true, kstidTlsOptETitle, kflidRnGenericRec_Title, kftTitleGroup,
			kwsAnal, 0, kFTVisIfData, kFTReqNotReq, stuTitleChars.Chars(), false, true);
		// Make a dummy entry type field, so the user can control visibility. Make sure
		// it is a type that will not cause any data to be loaded.
		qrsp->AddField(false, kstidLabelEntryType, 0,
			kftDummy, kwsAnal, 0,
			kFTVisIfData);
		// End Title block.
		// AnthroCodes
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslAnit);
		qrsp->AddPossField(true, kstidTlsOptAnthroCodes, kflidRnGenericRec_AnthroCodes,
			kftRefSeq, 0,
			vhvoPssl[RnLpInfo::kpidPsslAnit], kpntNameAndAbbrev, false, false,
			wsPssl, kFTVisIfData);
		// Description
		qrsp->AddField(true, kstidTlsOptEDescription, kflidRnEvent_Description,
			kftStText, kwsAnal, 0, kFTVisIfData);
		// FurtherQuestions
		qrsp->AddField(true, kstidTlsOptAFurtherQuestions,
			kflidRnGenericRec_FurtherQuestions,
			kftStText, kwsAnal, 0, kFTVisIfData);
		// PersonalNotes
		qrsp->AddField(true, kstidTlsOptEPersonalNotes,
			kflidRnEvent_PersonalNotes,
			kftStText, kwsAnal, 0, kFTVisIfData);
		// Document classifications block.
		qrsp->AddField(true, kstidTlsOptEClassifications,
			0, kftGroup, kwsAnal, 0, kFTVisNever);
			// Type
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslTyp);
		qrsp->AddPossField(false, kstidTlsOptEType, kflidRnEvent_Type,
			kftRefAtomic, 0,
			vhvoPssl[RnLpInfo::kpidPsslTyp], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// DateOfEvent
		qrsp->AddField(false, kstidTlsOptEDateOfEvent, kflidRnEvent_DateOfEvent,
			kftGenDate, kwsAnal, 0, kFTVisIfData);
		// TimeOfEvent
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslTim);
		qrsp->AddPossField(false, kstidTlsOptETimeOfEvent, kflidRnEvent_TimeOfEvent,
			kftRefSeq, 0,
			vhvoPssl[RnLpInfo::kpidPsslTim], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Sources
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslPeo);
		qrsp->AddPossField(false, kstidTlsOptESources, kflidRnEvent_Sources,
			kftRefSeq, 0,
			vhvoPssl[RnLpInfo::kpidPsslPeo], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Participants
		qrsp->AddField(false, kstidTlsOptParticipants, kflidRnEvent_Participants,
			kftExpandable, kwsVernAnals, 0, kFTVisIfData);
		// Locations
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslLoc);
		qrsp->AddPossField(false, kstidTlsOptELocations, kflidRnEvent_Locations,
			kftRefSeq, 0,
			vhvoPssl[RnLpInfo::kpidPsslLoc], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Confidence
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslCon);
		qrsp->AddPossField(false, kstidTlsOptEConfidence, kflidRnGenericRec_Confidence,
			kftRefAtomic, 0,
			vhvoPssl[RnLpInfo::kpidPsslCon], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Weather
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslWea);
		qrsp->AddPossField(false, kstidTlsOptEWeather, kflidRnEvent_Weather,
			kftRefSeq, 0,
			vhvoPssl[RnLpInfo::kpidPsslWea], kpntName, true, false,
			wsPssl, kFTVisIfData);
		// Researchers
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslPeo);
		qrsp->AddPossField(false, kstidTlsOptEResearchers, kflidRnGenericRec_Researchers,
			kftRefSeq, 0,
			vhvoPssl[RnLpInfo::kpidPsslPeo], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Restrictions
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslRes);
		qrsp->AddPossField(false, kstidTlsOptERestrictions, kflidRnGenericRec_Restrictions,
			kftRefSeq, 0,
			vhvoPssl[RnLpInfo::kpidPsslRes], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// End of document classifications block.
		// ExternalMaterials
		qrsp->AddField(true, kstidTlsOptEExternalMaterials,
			kflidRnGenericRec_ExternalMaterials,
			kftStText, kwsAnal, 0,
			kFTVisIfData);
		// SeeAlso
		qrsp->AddField(true, kstidTlsOptASeeAlso, kflidRnGenericRec_SeeAlso,
			kftObjRefSeq, kwsAnal, kstidRnGenericRec_SeeAlso,
			kFTVisIfData, kFTReqNotReq, L"Internal Link");
		// Now add Custom fields here.
		if (!fskipCustFlds)
		{
			// Go through the RecordSpecs looking for Custom Fields.
			ClevRspMap::iterator ithmclevrspLim = vuvs[iuvs]->m_hmclevrsp.End();
			for (ClevRspMap::iterator it = vuvs[iuvs]->m_hmclevrsp.Begin();
				 it != ithmclevrspLim; ++it)
			{
				ClsLevel clev = it.GetKey();
				if (clev.m_clsid != kclidRnEvent)
					continue;
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
		// History block
		qrsp->AddField(true, kstidTlsOptEHistory, 0,
			kftGroupOnePerLine, kwsAnal, 0, kFTVisNever);
		// DateCreated
		qrsp->AddField(false, kstidTlsOptDocDateCreated, kflidRnGenericRec_DateCreated,
			kftDateRO, kwsAnal, 0, kFTVisNever);
		// DateModified
		qrsp->AddField(false, kstidTlsOptDocDateModified, kflidRnGenericRec_DateModified,
			kftDateRO, kwsAnal, 0, kFTVisNever);
/*
			// VersionHistory
			qrsp->AddField(false, kstidTlsOptEVersionHistory,
				kflidRnGenericRec_VersionHistory,
				kftStText, kwsAnal, 0, kFTVisNever);
*/
		// End history block
		// SubRecords
		qrsp->AddHierField(true, kstidTlsOptSubentries, kflidRnGenericRec_SubRecords,
			kwsAnal, 0, konsNumDot, false, kFTVisIfData);
		// Finish it off
		qrsp->SetMetaNames(qmdc);

		// Document RnAnalysis
		qrsp.Create();
		qrsp->Init(puvs, kclidRnAnalysis, 0, vwt, qwsf);
		// Title block.
		// Title
		qrsp->AddField(true, kstidTlsOptATitle, kflidRnGenericRec_Title, kftTitleGroup,
			kwsAnal, 0, kFTVisIfData, kFTReqNotReq, stuTitleChars.Chars(), false, true);
		// Make a dummy entry type field, so the user can control visibility. Make sure
		// it is a type that will not cause any data to be loaded.
		qrsp->AddField(false, kstidLabelEntryType, 0,
			kftDummy, kwsAnal, 0,
			kFTVisIfData);
		// End Title block.
		// AnthroCodes
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslAnit);
		qrsp->AddPossField(true, kstidTlsOptAnthroCodes, kflidRnGenericRec_AnthroCodes,
			kftRefSeq, 0,
			vhvoPssl[RnLpInfo::kpidPsslAnit], kpntNameAndAbbrev, false, false,
			wsPssl, kFTVisIfData);
		// Hypothesis
		qrsp->AddField(true, kstidTlsOptAHypothesis, kflidRnAnalysis_Hypothesis,
			kftStText, kwsAnal, 0, kFTVisIfData);
		// Discussion
		qrsp->AddField(true, kstidTlsOptADiscussion, kflidRnAnalysis_Discussion,
			kftStText, kwsAnal, 0, kFTVisIfData);
		// Conclusions
		qrsp->AddField(true, kstidTlsOptAConclusions, kflidRnAnalysis_Conclusions,
			kftStText, kwsAnal, 0, kFTVisIfData);
		// FurtherQuestions
		qrsp->AddField(true, kstidTlsOptAFurtherQuestions,
			kflidRnGenericRec_FurtherQuestions,
			kftStText, kwsAnal, 0, kFTVisIfData);
		// ResearchPlan
		qrsp->AddField(true, kstidTlsOptAResearchPlan, kflidRnAnalysis_ResearchPlan,
			kftStText, kwsAnal, 0, kFTVisIfData);
		// SupportingEvidence
		qrsp->AddField(true, kstidTlsOptASupportingEvidence, kflidRnAnalysis_SupportingEvidence,
			kftObjRefSeq, kwsAnal, 0, kFTVisIfData, kFTReqNotReq, L"Internal Link");
		// CounterEvidence
		qrsp->AddField(true, kstidTlsOptACounterEvidence, kflidRnAnalysis_CounterEvidence,
			kftObjRefSeq, kwsAnal, 0, kFTVisIfData, kFTReqNotReq, L"Internal Link");
		// SupersededBy
		qrsp->AddField(true, kstidTlsOptASupersededBy, kflidRnAnalysis_SupersededBy,
			kftObjRefSeq, kwsAnal, 0, kFTVisIfData, kFTReqNotReq, L"Internal Link");
		// SeeAlso
		qrsp->AddField(true, kstidTlsOptASeeAlso, kflidRnGenericRec_SeeAlso,
			kftObjRefSeq, kwsAnal, 0, kFTVisIfData, kFTReqNotReq, L"Internal Link");
		// ExternalMaterials
		qrsp->AddField(true, kstidTlsOptEExternalMaterials,
			kflidRnGenericRec_ExternalMaterials,
			kftStText, kwsAnal, 0, kFTVisIfData);
		// Document classifications block.
		qrsp->AddField(true, kstidTlsOptEClassifications,
			0, kftGroup, kwsAnal, 0, kFTVisNever);
			// Status
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslAna);
		qrsp->AddPossField(false, kstidTlsOptAStatus, kflidRnAnalysis_Status,
			kftRefAtomic, 0,
			vhvoPssl[RnLpInfo::kpidPsslAna], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Confidence
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslCon);
		qrsp->AddPossField(false, kstidTlsOptAConfidence, kflidRnGenericRec_Confidence,
			kftRefAtomic, 0,
			vhvoPssl[RnLpInfo::kpidPsslCon], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Researchers
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslPeo);
		qrsp->AddPossField(false, kstidTlsOptAResearchers, kflidRnGenericRec_Researchers,
			kftRefSeq, 0,
			vhvoPssl[RnLpInfo::kpidPsslPeo], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Restrictions
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslRes);
		qrsp->AddPossField(false, kstidTlsOptARestrictions,
			kflidRnGenericRec_Restrictions, kftRefSeq, 0,
			vhvoPssl[RnLpInfo::kpidPsslRes], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// End of document classifications block.
		// Now add Custom fields here.
		if (!fskipCustFlds)
		{
			// Go through the RecordSpecs looking for Custom Fields.
			ClevRspMap::iterator ithmclevrspLim = vuvs[iuvs]->m_hmclevrsp.End();
			for (ClevRspMap::iterator it = vuvs[iuvs]->m_hmclevrsp.Begin();
				 it != ithmclevrspLim; ++it)
			{
				ClsLevel clev = it.GetKey();
				if (clev.m_clsid != kclidRnAnalysis)
					continue;
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
		// History block
		qrsp->AddField(true, kstidTlsOptEHistory, 0,
			kftGroupOnePerLine, kwsAnal, 0, kFTVisNever);
		// DateCreated
		qrsp->AddField(false, kstidTlsOptDocDateCreated, kflidRnGenericRec_DateCreated,
			kftDateRO, kwsAnal, 0, kFTVisNever);
		// DateModified
		qrsp->AddField(false, kstidTlsOptDocDateModified, kflidRnGenericRec_DateModified,
			kftDateRO, kwsAnal, 0, kFTVisNever);
/*
			// VersionHistory
			qrsp->AddField(false, kstidTlsOptEVersionHistory,
				kflidRnGenericRec_VersionHistory,
				kftStText, kwsAnal, 0, kFTVisNever);
*/
		// End history block
		// SubRecords
		qrsp->AddHierField(true, kstidTlsOptSubentries, kflidRnGenericRec_SubRecords,
			kwsAnal, 0, konsNumDot, false, kFTVisIfData);
		// Finish it off
		qrsp->SetMetaNames(qmdc);

		// Document Roled Participants.
		// Note: This must be included in order to load the reference properties, even
		// though we don't actually show these fields in the Tools...Options dialog.
		qrsp.Create();
		qrsp->Init(puvs, kclidRnRoledPartic, 0, vwt, qwsf);
		// Participants
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslPeo);
		qrsp->AddPossField(true, kstidTlsOptParticipants, kflidRnRoledPartic_Participants,
			kftRefSeq, 0,
			vhvoPssl[RnLpInfo::kpidPsslPeo], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Role
		wsPssl = GetPossListWs(RnLpInfo::kpidPsslRol);
		qrsp->AddPossField(true, kstidTlsOptRole, kflidRnRoledPartic_Role,
			kftRefAtomic, kstidRnRoledPartic_Role,
			vhvoPssl[RnLpInfo::kpidPsslRol], kpntName, false, false,
			wsPssl, kFTVisIfData);
		// Finish it off
		qrsp->SetMetaNames(qmdc);
		break; // End of kvwtDoc
	}
}

/*----------------------------------------------------------------------------------------------
	Returns a string containing the query to get the references to the items in a list from the
	database.  Used by the TlsStatsDlg.

	@param iList - index of the list into the AfLpInfo's vector of possibility list ids
	@param pstuQuery - pointer to the string of the query.
----------------------------------------------------------------------------------------------*/
void RnMainWnd::GetStatsQuery(int iList, StrUni * pstuQuery)
{
	Assert(pstuQuery);
	Assert(m_qlpi);
	Vector<HVO> & vPsslIds = m_qlpi->GetPsslIds();
	Assert((unsigned)iList < (unsigned)vPsslIds.Size());
	int hvoList = vPsslIds[iList];
	Assert(hvoList);

	pstuQuery->Clear();		// just to be safe...

	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	pdbi->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));
	StrUni stuCmd;
	stuCmd.Format(L"SELECT DISTINCT f.Type, f.Name, c.Name%n"
		L"FROM UserViewField uvf%n"
		L"JOIN Field$ f ON f.Id = uvf.Flid%n"
		L"JOIN Class$ c ON c.Id = f.Class%n"
		L"WHERE uvf.PossList = %d", hvoList);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	ComBool fMoreRows;
	ComBool fIsNull;
	ULONG cbSpaceTaken;
	int nType;
	const int kcchBuffer = MAX_PATH;
	wchar rgchField[kcchBuffer];
	wchar rgchClass[kcchBuffer];
	CheckHr(qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&nType), sizeof(nType),
			&cbSpaceTaken, &fIsNull, 0));
		if (fIsNull)
			continue;
		CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(rgchField),
			kcchBuffer * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
		if (fIsNull)
			continue;
		CheckHr(qodc->GetColValue(3, reinterpret_cast <BYTE *>(rgchClass),
			kcchBuffer * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
		if (fIsNull)
			continue;
		if (pstuQuery->Length())
			pstuQuery->Append(L" UNION ALL ");
		switch (nType)
		{
		case kcptReferenceAtom:
			pstuQuery->FormatAppend(L"SELECT %<0>s dst FROM %<1>s WHERE %<0>s IS NOT NULL",
				rgchField, rgchClass);
			break;
		case kcptReferenceCollection:
		case kcptReferenceSequence:
			pstuQuery->FormatAppend(L"SELECT dst FROM %s_%s", rgchClass, rgchField);
			break;
		default:
			Assert(nType == kcptReferenceAtom ||
				nType == kcptReferenceCollection ||
				nType == kcptReferenceSequence);
			pstuQuery->Clear();
			return;
		}
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	qodc.Clear();
	if (pstuQuery->Length())
		pstuQuery->Append(" UNION ALL ");
	pstuQuery->Append(L"SELECT dst FROM RnGenericRec_PhraseTags ORDER BY dst");
}


/*----------------------------------------------------------------------------------------------
	Return true if a filter is active.
----------------------------------------------------------------------------------------------*/
bool RnMainWnd::IsFilterActive()
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
bool RnMainWnd::IsSortMethodActive()
{
	Set<int> sisel;
	m_qvwbrs->GetSelection(m_ivblt[kvbltSort], sisel);
	if (sisel.Size() != 1)
		return false;
	return (*sisel.Begin() != 0);	// 0 means Default Sort Method.
}

/*----------------------------------------------------------------------------------------------
	Returns the HVO of the current record or subrecord.
----------------------------------------------------------------------------------------------*/
HVO RnMainWnd::GetCurRec()
{
	HVO hvoCurRec = 0;
	Assert(m_vhvoPath.Size() >= m_vflidPath.Size());
	for (int iflid = 0; iflid < m_vflidPath.Size(); ++iflid)
	{
		if (m_vflidPath[iflid] == kflidRnGenericRec_SubRecords)
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
	@param pcmd Menu command info.
	@return true to stop further processing.
----------------------------------------------------------------------------------------------*/
bool RnMainWnd::CmdDelete(Cmd * pcmd)
{
	// Get the current record we are working with.
	HVO hvo = GetCurRec();
	return CmdDelete1(pcmd, hvo);
}


/*----------------------------------------------------------------------------------------------
	Respond to the Edit...Delete menu item.
	@param pcmd Menu command info.
	@param hvo HVO of the object to be be deleted.
	@return true to stop further processing.
----------------------------------------------------------------------------------------------*/
bool RnMainWnd::CmdDelete1(Cmd * pcmd, HVO hvo)
{
	// Get the current record we are working with.
	HVO hvoOwn; // Owner of current object.
	int flid; // Property of owner that holds object to delete.
	CheckHr(m_qcvd->get_ObjOwner(hvo, &hvoOwn));
	CheckHr(m_qcvd->get_ObjOwnFlid(hvo, &flid));

	AfDeSplitChild * padsc = CurrentDeWnd();
	int cchSel = 0;
	if (padsc)
	{
		// It is a data entry window
		AfDeFieldEditorPtr qdfe = padsc->GetActiveFieldEditor();
		if (qdfe && qdfe->IsTextSelected())
		{
			// there is a current selection so put up the delete dialog.
			DeleteDlgPtr qdd;
			qdd.Create();
			StrApp strMsg;
			strMsg.Load(flid == kflidRnGenericRec_SubRecords ? kstidSubentry : kstidEntry);
			qdd->SetDialogValue(strMsg);
			if (kctidOk != qdd->DoModal(m_hwnd))
			{
				return true;
			}
			WaitCursor wc;

			if (!qdd->GetDialogValue())
			{
				// Delete the selected text.
				qdfe->DeleteSelectedText();

/*				IVwRootSitePtr qvrs;
				CheckHr(qrootb->get_Site(&qvrs));
				AfVwRootSite * pvwnd = dynamic_cast<AfVwRootSite *>(qvrs.Ptr());
				pvwnd->OnChar(127, 1, 83);
*/
				return true;
			}
		}
	}
	else
	{
		// It is a Browse or Document window.
		// See if there is selected text
		IVwRootBoxPtr qrootb = GetActiveRootBox(true);
		IVwSelectionPtr qvwsel = NULL;
		if (qrootb)
			CheckHr(qrootb->get_Selection(&qvwsel));
		ITsStringPtr qtssSel;
		SmartBstr sbstr = L"";
		if (qvwsel) // fails also if no root box
			CheckHr(qvwsel->GetSelectionString(&qtssSel, sbstr));
		if (qtssSel)
			CheckHr(qtssSel->get_Length(&cchSel));
		if (cchSel)
		{
			// There is a current selection so put up the delete dialog unless the selection
			// spans more than one field, in which case go straight to deletion of the record.
			IVwRootSitePtr qvrs;
			CheckHr(qrootb->get_Site(&qvrs));
			AfVwRootSite * pvwnd = dynamic_cast<AfVwRootSite *>(qvrs.Ptr());
			if (pvwnd->SelectionInOneField())
			{
				DeleteDlgPtr qdd;
				qdd.Create();
				StrApp strMsg;
				strMsg.Load(flid == kflidRnGenericRec_SubRecords ? kstidSubentry : kstidEntry);
				qdd->SetDialogValue(strMsg);
				if (kctidOk != qdd->DoModal(m_hwnd))
				{
					return true;
				}
				WaitCursor wc;

				if (!qdd->GetDialogValue())
				{
					// Delete the selected text.
					pvwnd->OnChar(127, 1, 83);
					return true;
				}
			}
		}
	}

	// Abort if the current editor can't be changed. Actually, we could go ahead anyway, since
	// the only way to currently delete a record is to have the active editor in the record to
	// be deleted. However, this will probably change with right mouse menus. Also, we don't
	// currently have a way to force a close without an assert, and I want to keep the assert
	// there to catch programming errors. We could add a boolean argument to EndEdit to override
	// the assert, but this hardly seems worth it, especially considering above comments. So for
	// now, we force users to produce valid data prior to deleting a record. Also, since
	// updating other windows may cause their editors to close, we also need to check that they
	// are legal.
	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	int cwnd = vqafw.Size();
	int iwnd;
	for (iwnd = 0; iwnd < cwnd; iwnd++)
	{
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(vqafw[iwnd].Ptr());
		AssertPtr(prmw);
		if (!prmw->IsOkToChange())
		{
			// Bring the bad window to the top.
			::SetForegroundWindow(prmw->Hwnd());
			return false;
		}
		// Close any active editors before we delete, or we may try saving something to
		// a record that has already been deleted.
		prmw->CloseActiveEditors();
	}

	StrApp strType;
	StrApp strSub;
	int clid;
	m_qcvd->get_ObjClid(hvo, &clid);
	strType.Load(flid == kflidRnGenericRec_SubRecords ? kstidSubentry : kstidEntry);
	strSub.Load(kstidSubentries);

	// put up the delete object dialog
	ITsStringPtr qtss;
	DeleteObjDlgPtr qdo;
	qdo.Create();
	GetDragText(hvo, clid, &qtss);
	qdo->SetDialogValues(hvo, qtss, strType, strSub, kclidRnGenericRec, m_qlpi->GetDbInfo());
	if (kctidOk != qdo->DoModal(m_hwnd))
		return true;

	WaitCursor wc;

	// Update the data cache
	int ihvo;
	int chvo;
	CheckHr(m_qcvd->get_VecSize(hvoOwn, flid, &chvo));
	if (chvo)
	{
		// Find the index to the item we are deleting.
		for (ihvo = 0; ihvo < chvo; ++ihvo)
		{
			HVO hvoT;
			CheckHr(m_qcvd->get_VecItem(hvoOwn, flid, ihvo, &hvoT));
			if (hvoT == hvo)
				break;
		}
	}
	else
	{
		// Signal that the owner's properties are not cached, just this one.
		ihvo = -1;
	}

	// Start undo action (e.g., "Undo Delete Analysis").
	StrUni stu;
	if (clid == kclidRnEvent)
		stu.Load(flid == kflidRnGenericRec_SubRecords ? kstidEventSubentry : kstidEvent);
	else
		stu.Load(flid == kflidRnGenericRec_SubRecords ? kstidAnalSubentry : kstidAnalysis);
	StrUni stuUndoFmt;
	StrUni stuRedoFmt;
	StrUtil::MakeUndoRedoLabels(kstidUndoDeleteX, &stuUndoFmt, &stuRedoFmt);
	StrUni stuUndo;
	StrUni stuRedo;
	stuUndo.Format(stuUndoFmt.Chars(), stu.Chars());
	stuRedo.Format(stuRedoFmt.Chars(), stu.Chars());
	CheckHr(m_qcvd->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));

	// Delete the object, remove it from the vector, and notify that the prop changed.
	CheckHr(m_qcvd->DeleteObjOwner(hvoOwn, hvo, flid, ihvo));
	CheckHr(m_qcvd->PropChanged(NULL, kpctNotifyAll, hvoOwn, flid, ihvo, 0, 1));
	// RemoveObjRefs has been subsumed by DeleteObjOwner.
//	// If we have references, they may be stored at various places in the cache.
//	// Flush the cache of all of these references.
//	CheckHr(m_qcvd->RemoveObjRefs(hvo));

	CheckHr(m_qcvd->EndUndoTask());

	// Delete the (sub)record from the vector of visible records.
	// Let every top-level window that is showing the same RN know about the change.
	for (iwnd = 0; iwnd < cwnd; iwnd++)
	{
		RnMainWnd * prnmw = dynamic_cast<RnMainWnd *>(vqafw[iwnd].Ptr());
		AssertPtr(prnmw);
		if (prnmw->GetLpInfo() == GetLpInfo())
			prnmw->OnDeleteRecord(flid, hvo);
	}

	if (!RecordCount())
	{
		// We just deleted the last item: check whether we have a filter turned on.
		AfViewBarShellPtr qvwbrs = GetViewBarShell();
		// We need to check for qvwbrs here. If a person deletes the last record, the
		// OnDeleteRecord call above pops up a dialog allowing the user to add a new entry
		// or exit. If they hit exit, qvwbrs is cleared prior to finishing out this method.
		if (!qvwbrs)
			return true;
		Set<int> siselFilter;
		qvwbrs->GetSelection(m_ivblt[kvbltFilter], siselFilter);
		int iflt = 0;
		if (siselFilter.Size())
			iflt = *siselFilter.Begin();
		if (iflt)
		{
			--iflt;							// adjust for first filter being "No filter"
			UpdateStatusBar();		// This turns the filter status pane on.
			StrUni stuNoMatch;
			stuNoMatch.Load(kstidStBar_NoFilterMatch);
			AfStatusBarPtr qstbr = GetStatusBarWnd();
			Assert(qstbr);
			qstbr->SetRecordInfo(NULL, stuNoMatch.Chars(), NULL);
			qstbr->SetLocationStatus(0, 0);

			// There are no longer any records that match the filter, so let the user
			// make a choice as to what they want to do.

			RnFilterNoMatchDlgPtr qfltnm;
			qfltnm.Create();
			qfltnm->SetDialogValues(iflt, this);
			int ifltNew = 0;
			if (qfltnm->DoModal(m_hwnd) == kctidOk)
			{
				qfltnm->GetDialogValues(ifltNew);
				// Since the first filter is actually the No Filter, we have to add one
				// to the number retrieved from the dialog.
				ifltNew++;
				// Moved from TlsOptDlg::SaveFilterValues() to prevent massive use of
				// "GDI Object" and "User Object" resources.
				Vector<int> vifltNew = qfltnm->GetNewFilterIndexes();
				Vector<AfViewBarShell *> vpvwbrs = qfltnm->GetFilterViewBars();
				Assert(vifltNew.Size() == vpvwbrs.Size());
				qfltnm.Clear();
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
							vpvwbrs[iwnd]->SetSelection(m_ivblt[kvbltFilter], sisel);
						}
					}
				}
			}
			else
			{
				qfltnm.Clear();
			}
			if (ifltNew != -1)
			{
				Set<int> sisel;
				sisel.Insert(ifltNew);
				qvwbrs->SetSelection(m_ivblt[kvbltFilter], sisel);
			}
		}
		else
		{
			// "No filter" is selected, we've just deleted the last record in the database!
		}
	}

	return true;
}

void RnMainWnd::SetCaptionBarIcons(AfCaptionBar * pcpbr)
{
	AssertPtr(pcpbr);

	pcpbr->AddIcon(kimagDataEntry);
	pcpbr->AddIcon(kimagFilterNone);
	pcpbr->AddIcon(kimagSort);
	pcpbr->AddIcon(kimagOverlayNone);
}


/*----------------------------------------------------------------------------------------------
	Process Insert Event/Analysis menu item.  Switch to a Data Entry view if we aren't already
	in one.  If we are filtered, put up dialog and turn it off. Have database create a new
	record and set hvoNew to the new ID.  et create and modified times to the new time.  If we
	have an event entry, try to set the type to Observation.  Let every top-level window that
	is showing the same language project know about the change.

	@param pcmd Ptr to menu command

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool RnMainWnd::CmdInsertEntry(Cmd * pcmd)
{
	AssertObj(pcmd);

	// Warn user if there are any problems with closing the current window.
	// Only check for required fields if we are currently in a data entry window.
	AfClientWnd * pafcw = m_qmdic->GetCurChild();
	if (!IsOkToChange(pafcw->GetImageIndex() == kimagDataEntry))
		return false;

	// Switch to data entry mode and make sure there are no filters active.
	if (!SwitchToDataEntryView(kimagDataEntry)
		|| !EnsureNoFilter() || !EnsureSafeSort())
		return false;

	try
	{
		// Save any changes before adding a new entry.
		SaveData();
		// Now insert the new entry and display it.
		int clid = pcmd->m_cid == kcidInsEntryEvent ? kclidRnEvent : kclidRnAnalysis;
		int kstid = clid == kclidRnEvent ? kstidEvent : kstidAnalysis;
		AfClientRecDeWndPtr qcrde =
				dynamic_cast<AfClientRecDeWnd *>(m_qmdic->GetCurChild());
		AssertPtr(qcrde);
		HVO hvoNew;
		RnLpInfo * plpi = dynamic_cast<RnLpInfo *>(GetLpInfo());
		AssertPtr(plpi);
		HVO hvoRnb = plpi->GetRnId();

		// We need to close the current editor. If not, we can have a temporary edit started
		// (e.g., in AfDeEdBoxBut::BeginTempEdit() which isn't completed until after we create
		// the new entry. As a result, when the temporary edit finishes, we lose the undo for
		// creating the new entry.
		AfDeSplitChild * padsc = dynamic_cast<AfDeSplitChild *>(qcrde->CurrentPane());
		AssertPtr(padsc);
		padsc->CloseEditor();

		hvoNew = CreateUndoableObject(hvoRnb, kflidRnResearchNbk_Records, 0,
					clid, kstid, -1);
		if (hvoNew == 0)
			return false;	// Couldn't make a new one.

		// Let every top-level window that is showing the same language project know about
		// the change.
		Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
		int cwnd = vqafw.Size();
		int ihvo;

#ifdef DEBUG
		int ihvoT = -1;
#endif
		for (int iwnd = 0; iwnd < cwnd; iwnd++)
		{
			RnMainWnd * prnmw = dynamic_cast<RnMainWnd *>(vqafw[iwnd].Ptr());
			AssertPtr(prnmw);
			if (prnmw->GetLpInfo() == plpi)
			{
				ihvo = prnmw->OnInsertRecord(clid, hvoNew);
#ifdef DEBUG
				// Make sure we are inserting the new record at the same spot in each window.
				// Each main window is using the same cache, so the number of records in
				// each window must be the same.
				Assert(ihvoT == -1 || ihvoT == ihvo);
				ihvoT = ihvo;
#endif
			}
		}
//		SetCurRecIndex(ihvo);

		CheckHr(qcrde->DispCurRec(0, 0));
		return true;
	}
	catch (...)
	{
		Assert(false);
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Override method (and call it) in order to set additional information on new event/analysis.

	@param hvoOwner Id of the owner of the new object.
	@param flidOwn flid where the new object will be placed.
	@param clsid Class id of object to be created.

	@return Id of new object, or 0 if not created.
----------------------------------------------------------------------------------------------*/
HVO RnMainWnd::CreateUndoableObjectCore(HVO hvoOwner, int flidOwn, int clsid, int ihvo)
{
	Assert(m_qcvd);
	// Superclass handles basic creation. It also caches class and owner.
	HVO hvoNew = SuperClass::CreateUndoableObjectCore(hvoOwner, flidOwn, clsid, ihvo);
	if (hvoNew == 0)
		return 0;	// Couldn't make a new one.

	try
	{
		RnLpInfo * plpi = dynamic_cast<RnLpInfo *>(GetLpInfo());
		AssertPtr(plpi);

		// Set times.
		SilTime stim = SilTime::CurTime();
		CheckHr(m_qcvd->SetTime(hvoNew, kflidRnGenericRec_DateCreated, stim.AsInt64()));
		CheckHr(m_qcvd->SetTime(hvoNew, kflidRnGenericRec_DateModified, stim.AsInt64()));

		// If we have an event entry, try to set the type to Observation
		if (clsid == kclidRnEvent)
		{
			// An event entry gets preset to Observation
			int hvopssl = plpi->GetPsslIds()[RnLpInfo::kpidPsslTyp];
			PossListInfoPtr qpli;
			PossItemInfo * ppii;
			StrUni stu;
			plpi->LoadPossList(hvopssl, plpi->AnalWs(), &qpli);
			Assert(qpli);
			int ipss = qpli->GetCount();
			while (--ipss >= 0)
			{
				ppii = qpli->GetPssFromIndex(ipss);
				Assert(ppii);
				ppii->GetName(stu, kpntName);
				if (stu.EqualsCI(L"Observation"))
					break;
			}
			int hvopss = ppii->GetPssId();
			CheckHr(m_qcvd->SetObjProp(hvoNew, kflidRnEvent_Type, hvopss));
		}

		// Get the RecordSpec for the inserted object. The first DE Spec will do.
		UserViewSpecVec & vuvs = plpi->GetDbInfo()->GetUserViewSpecs();
		UserViewSpec * puvs;
		int iv;
		for (iv = 0; iv < vuvs.Size(); ++iv)
		{
			if (vuvs[iv]->m_vwt == kvwtDE)
				break;
		}
		puvs = vuvs[iv];
		AssertPtr(puvs);
		ClsLevel clev(clsid, 0);
		RecordSpecPtr qrsp;
		puvs->m_hmclevrsp.Retrieve(clev, qrsp);
		AssertPtr(qrsp);

		// For all fields holding a structured text, create StText objects.
		BlockSpec * pbsp;
		int cbsp = qrsp->m_vqbsp.Size();
		for (int ibsp = 0; ibsp < cbsp; ++ibsp)
		{
			pbsp = qrsp->m_vqbsp[ibsp];
			if (pbsp->m_ft == kftStText)
				StVc::MakeEmptyStText(m_qcvd, hvoNew, pbsp->m_flid, plpi->ActualWs(pbsp->m_ws));
		}
	}
	catch (...)
	{
		return 0;
	}
	return hvoNew;
}

/*----------------------------------------------------------------------------------------------
	Handle the File / Import / Standard Format command.

	@param pcmd Pointer to the menu command.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool RnMainWnd::CmdFileImport(Cmd * pcmd)
{
	AssertObj(pcmd);

	RnLpInfo * prlpi = dynamic_cast<RnLpInfo *>(m_qlpi.Ptr());
	Assert(prlpi);
	ILgWritingSystemFactoryPtr qwsf;
	prlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);

	RnImportWizardPtr qriw;
	qriw.Create();
	qriw->Initialize(MainWindow(), qwsf);
	int n = qriw->DoModal(m_hwnd);
	if (n == kctidOk)
	{
		qriw->ImportShoeboxDatabase(prlpi);
	}
	else if (n == -1)
	{
		DWORD dwError = ::GetLastError();
		achar rgchMsg[MAX_PATH+1];
		DWORD cch = ::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, NULL, dwError, 0,
			rgchMsg, MAX_PATH, NULL);
		rgchMsg[cch] = 0;
		StrApp strTitle(kstidImportMsgCaption);
		::MessageBox(m_hwnd, rgchMsg, strTitle.Chars(), MB_OK | MB_ICONWARNING);
	}
	else if (n == kctidCancel && !RawRecordCount())
	{
		// User has cancelled and the database is empty.
		RnEmptyNotebookDlgPtr qremp;
		qremp.Create();
		StrApp strProjectName(prlpi->PrjName());
		qremp->SetProject(strProjectName);
		int ctid = qremp->DoModal(m_hwnd);
		switch (ctid)
		{
		case kctidEmptyNewEvent:
			::SendMessage(m_hwnd, WM_COMMAND, kcidInsEntryEvent, 0);
			break;
		case kctidEmptyNewAnalysis:
			::SendMessage(m_hwnd, WM_COMMAND, kcidInsEntryAnal, 0);
			break;
		case kctidEmptyImport:
			// PostMessage must be used here to avoid a recursive call.
			::PostMessage(m_hwnd, WM_COMMAND, kcidFileImpt, 0);
			break;
		case kctidCancel:
			// PostMessage must be used here instead of SendMessage or we'll get a crash when
			// you delete the last record and then choose Exit from this dialog. The crash
			// results from SendMessage closing things down before the right+click context
			// menu (in AfDeSplitChild::OnContextMenu) has been adequately destroyed.
			::PostMessage(m_hwnd, WM_COMMAND, kcidFileExit, 0);
			break;
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the File / Export Format command.

	@param pcmd Pointer to the menu command.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool RnMainWnd::CmdFileExport(Cmd * pcmd)
{
	AssertObj(pcmd);
	AssertPtr(dynamic_cast<RnLpInfo *>(m_qlpi.Ptr()));

	IFwExportDlgPtr qfexp;
	qfexp.CreateInstance(CLSID_FwExportDlg);
	// We need the style sheet!
	IVwStylesheetPtr qvss;
	AfStylesheet * pasts = GetStylesheet();
	AssertPtr(pasts);
	CheckHr(pasts->QueryInterface(IID_IVwStylesheet, (void **)&qvss));

	// We need the data notebook customization for exporting.
	RnCustomExportPtr qrcex;
	qrcex.Attach(NewObj RnCustomExport(m_qlpi, this));
	IFwCustomExportPtr qfcex;
	CheckHr(qrcex->QueryInterface(IID_IFwCustomExport, (void **)&qfcex));
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
		dynamic_cast<RnLpInfo *>(m_qlpi.Ptr())->GetRnId(),
		kflidRnGenericRec_SubRecords));

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
	Handle the File / New Language Project command.

	@param pcmd Pointer to the menu command.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool RnMainWnd::CmdFileNewProj(Cmd * pcmd)
{
	AssertObj(pcmd);

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
		RnMainWnd * prnmw = dynamic_cast<RnMainWnd *>(MainWindow());
		AssertPtr(prnmw);
		Rect rcT;
		::GetWindowRect(prnmw->Hwnd(), &rcT);
		int dypCaption = ::GetSystemMetrics(SM_CYCAPTION) + ::GetSystemMetrics(SM_CYSIZEFRAME);
		rcT.Offset(dypCaption, dypCaption);
		AfGfx::EnsureVisibleRect(rcT);
		WndCreateStruct wcs;
		wcs.InitMain(_T("RnMainWnd"));
		RnMainWndPtr qrnmw;
		qrnmw.Create();
		AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
		AssertPtr(pdapp);
		AfDbInfo * pdbi = pdapp->GetDbInfo(sbstrDatabase.Chars(), sbstrServer.Chars());
		AfLpInfo * plpi = pdbi->GetLpInfo(hvoLP);
		qrnmw->Init(plpi);
		qrnmw->CreateHwnd(wcs);
		AfStatusBarPtr qstbr = qrnmw->GetStatusBarWnd();
		Assert(qstbr);
		qstbr->InitializePanes();
		qrnmw->UpdateStatusBar();
		::MoveWindow(qrnmw->Hwnd(), rcT.left, rcT.top, rcT.Width(), rcT.Height(), true);
		qrnmw->Show(SW_SHOWNORMAL);
		Assert(!qrnmw->RawRecordCount());

		// Initialize the anthropology categories list.
		qrnmw->CheckAnthroList();
		// The project is open. Now ask what to do to start filling it in.
		StrApp strDbName(sbstrDatabase.Chars());
		qrnmw->CheckForNoRecords(strDbName.Chars());
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle the File / Data Notebook Properties command.

	@param pcmd Pointer to the menu command.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool RnMainWnd::CmdFileDnProps(Cmd * pcmd)
{
	AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
	Assert(pdapp);
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);

	if (!pdapp->AreAllWndsOkToChange(pdbi, false)) // Don't check required fields.
		return true;

	if (!pdapp->SaveAllWndsEdits(pdbi))
		return true;

	AssertObj(pcmd);
	HICON hicon;
	hicon = ::LoadIcon(pdapp->GetInstance(), MAKEINTRESOURCE(kridNoteBkIcon));
	StrApp strName(m_qlpi->PrjName());

	FwPropDlgPtr qfwpd;
	qfwpd.Create();

	StrApp strSize;
	strSize.Format(_T("%d %r"), RawRecordCount(), kstidPropEntries);

	StrApp strLoc = pdbi->ServerName();
	if (strLoc.Length())
		strLoc.Replace(strLoc.Length() - 6, strLoc.Length(), " "); // Remove /SILFW from server.
	strLoc.Append(m_qlpi->PrjName());

	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	OLECHAR rgch[MAX_PATH];
	pdbi->GetDbAccess(&qode);
	StrUni stuSql;
	stuSql.Format(L"exec GetOrderedMultiTxt '%d', %d, 1", GetRootObj(), kflidCmMajorObject_Name);
	CheckHr(qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	Assert(fMoreRows); // This proc should always return something.
	CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgch), isizeof(rgch), &cbSpaceTaken,
		&fIsNull, 2));
	Assert(cbSpaceTaken <= isizeof(rgch));
	StrApp strRnName(rgch);

	StrApp strDescription;
	stuSql.Format(L"exec GetOrderedMultiTxt '%d', %d, 1", GetRootObj(),
		kflidCmMajorObject_Description);
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgch), isizeof(rgch),
			&cbSpaceTaken, &fIsNull, 2));
		if (cbSpaceTaken > isizeof(rgch))
		{
			Vector<OLECHAR> vch;
			vch.Resize(cbSpaceTaken / isizeof(OLECHAR));
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(vch.Begin()),
				vch.Size() * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
			strDescription.Assign(vch.Begin());
		}
		else
		{
			strDescription.Assign(rgch);
		}
	}

	StrApp strType(kstidResearchNotebook);

	qfwpd->Initialize(m_qlpi, hicon, strRnName.Chars(), strType.Chars(), strLoc.Chars(),
		strSize, GetRootObj(), strDescription.Chars(), NULL,
		_T("User_Interface/Menus/File/Notebook_Properties.htm"),
		kctidGeneralPropTabDnName, NULL);
	qfwpd->SetDateCreatedFlid(kflidCmMajorObject_DateCreated);
	qfwpd->SetDateModifiedFlid(kflidCmMajorObject_DateModified);
	int n = qfwpd->DoModal(m_hwnd);
	if (n == kctidOk)
	{
		// If we changed the name, make sure all title bars for the same database are updated.
		if (m_qlpi)
		{
			if (!strRnName.Equals(qfwpd->GetName()))
			{
				// Set the Name of the Notebook.
				StrUni stuName(qfwpd->GetName());
				StrUtil::NormalizeStrUni(stuName, UNORM_NFD);
				stuSql.Format(L"exec SetMultiTxt$ %d, %d, %d, ?",
					kflidCmMajorObject_Name, GetRootObj(), UserWs());
				CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
					(ULONG *)stuName.Chars(), stuName.Length() * 2));
				CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
				dynamic_cast<RnLpInfo *>(m_qlpi.Ptr())->SetRnName(stuName);
				SavePageSetup();
				// Notify the database that we've made a change and update all our windows.
				SyncInfo sync(ksyncPageSetup, 0, 0);
				m_qlpi->StoreAndSync(sync);
			}

			if (!strDescription.Equals(qfwpd->GetDescription()))
			{
				// Set the Description of the Notebook.
				StrUni stuDescription(qfwpd->GetDescription());
				StrUtil::NormalizeStrUni(stuDescription, UNORM_NFD);
				stuSql.Format(L"exec SetMultiBigStr$ %d, %d, %d, ?, ?",
					kflidCmMajorObject_Description, GetRootObj(), UserWs());
				CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
					(ULONG *)stuDescription.Chars(), stuDescription.Length() * 2));

				ITsStringPtr qtss;
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				CheckHr(qtsf->MakeString(stuDescription.Bstr(), UserWs(), &qtss));

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
				CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
				delete[] rgbFmt;
			}

			ShowAllOverlays(false);
			m_qlpi->ClearOverlays();
			ShowAllOverlays(true, true);
		}
	}
	if (n == -1)
	{
		DWORD dwError = ::GetLastError();
		achar rgchMsg[MAX_PATH+1];
		DWORD cch = ::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, NULL, dwError, 0,
			rgchMsg, MAX_PATH, NULL);
		rgchMsg[cch] = 0;
		StrApp strTitle(kstidLangProjPropMsgCaption);
		::MessageBox(m_hwnd, rgchMsg, strTitle.Chars(), MB_OK | MB_ICONWARNING);
		return false;
	}
	return true;
}

AfRecCaptionBar * RnMainWnd::CreateCaptionBar()
{
	return NewObj RnCaptionBar(this);
}



/*----------------------------------------------------------------------------------------------
	Bring up the Reports dialog, run it, save the results.
----------------------------------------------------------------------------------------------*/
bool RnMainWnd::CmdStats(Cmd * pcmd)
{
	AssertObj(pcmd);
	TlsStatsDlgPtr qrptd;
	qrptd.Create();

	// Use our window title as default header.
	achar rgchName[MAX_PATH];
	::SendMessage(m_hwnd, WM_GETTEXT, MAX_PATH, (LPARAM)rgchName);

	// Note: Do not use & reference here. We need to make a copy of the original
	// list because we are changing the list and we don't want to ruin the original one.
	Vector<HVO> vhvoPssl = m_qlpi->GetPsslIds();
	Vector<bool> vfDisplaySettings;

	int cv = vhvoPssl.Size();
	int i;
	for (i = 0; i < cv; ++i)
	{
		if (i == RnLpInfo::kpidPsslAnit)
			vfDisplaySettings.Push(true);
		else
			vfDisplaySettings.Push(false);
	}

	// Remove two lists that can't be referenced from within data notebook records.
	const long pss = 0;
	const bool flag = false;
	vhvoPssl.Replace(RnLpInfo::kpidPsslPsn, RnLpInfo::kpidPsslPsn + 1, &pss, 1);
	vfDisplaySettings.Replace(RnLpInfo::kpidPsslPsn, RnLpInfo::kpidPsslPsn + 1, &flag, 1);

	vhvoPssl.Replace(RnLpInfo::kpidPsslEdu, RnLpInfo::kpidPsslEdu + 1, &pss, 1);
	vfDisplaySettings.Replace(RnLpInfo::kpidPsslEdu, RnLpInfo::kpidPsslEdu + 1, &flag, 1);

	qrptd->SetDialogValues(m_qlpi->AnalWs(), vhvoPssl, vfDisplaySettings);
	qrptd->DoModal(Hwnd());
	return true;
}

bool RnMainWnd::FindInDictionary(IVwRootBox * prootb)
{
	if (!prootb)
		return false;
	IVwSelectionPtr qsel;
	CheckHr(prootb->get_Selection(&qsel));
	if (!qsel)
		return false;
	AfLpInfoPtr qlpi = GetLpInfo();
	AssertPtr(qlpi);
	IFwMetaDataCachePtr qmdc;
	qlpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
	AssertPtr(qmdc);
	IOleDbEncapPtr qode;
	qlpi->GetDbInfo()->GetDbAccess(&qode);

	ISilDataAccessPtr qsda;
	CheckHr(prootb->get_DataAccess(&qsda));
	IVwOleDbDaPtr qda;
	CheckHr(qsda->QueryInterface(IID_IVwOleDbDa, (void**)&qda));

	Cls::IDictionaryFinderPtr qdf;
	qdf.CreateInstance(L"SIL.FieldWorks.DictionaryFinder");
	qdf->FindInDictionary(qode, qmdc, qda, qsel);
	return true;
}

bool RnMainWnd::EnableCmdIfVernacularSelection(IVwRootBox * prootb, CmdState & cms)
{
	bool fEnable = false;
	IVwSelectionPtr qsel;
	// Check for a rootbox and a selection.
	if (prootb)
		CheckHr(prootb->get_Selection(&qsel));
	IVwOleDbDaPtr qda;
	if (qsel)
	{
		ISilDataAccessPtr qsda;
		CheckHr(prootb->get_DataAccess(&qsda));
		if (qsda)
		{
			// AfDeFeTags and AfDeFeRefs type slices can have an IVwCacheDa instead of an
			// IVwOleDbDa for their underlying ISilDataAccess.  So don't crash, but don't
			// enable either since the handler requires IVwOleDbDa.
			HRESULT hr = qsda->QueryInterface(IID_IVwOleDbDa, (void**)&qda);
			if (FAILED(hr))
				qda = NULL;
		}
	}
	// Check for vernacular ws at selection.
	if (qsel && qda)
	{
		ITsStringPtr qtssAnchor, qtssEnd;
		ITsTextPropsPtr qttp;
		int ichAnchor, ichEnd;
		ComBool fAssocPrev;
		HVO hvoObj;
		PropTag tag;
		int cch = 0;
		int wsAnchor = 0;
		int wsEnd = 0;
		int var;
		ComBool fRange = FALSE;
		ComBool fEndFirst = FALSE;
		CheckHr(qsel->TextSelInfo(false, &qtssAnchor, &ichAnchor, &fAssocPrev, &hvoObj, &tag, &wsAnchor));		// Anchor
		CheckHr(qsel->get_IsRange(&fRange));
		if (fRange)
		{
			CheckHr(qsel->get_EndBeforeAnchor(&fEndFirst));
			CheckHr(qsel->TextSelInfo(true, &qtssEnd, &ichEnd, &fAssocPrev, &hvoObj, &tag, &wsEnd));	// End
			if (fEndFirst)
				--ichAnchor;
			else
				--ichEnd;
		}
		if (qtssAnchor)
		{
			CheckHr(qtssAnchor->get_Length(&cch));
			if (cch > 0 && ichAnchor >= 0 && ichAnchor <= cch)	// See DN-836.
			{
				CheckHr(qtssAnchor->get_PropertiesAt(ichAnchor, &qttp));
				if (qttp)
					CheckHr(qttp->GetIntPropValues(ktptWs, &var, &wsAnchor));
			}
		}
		if (qtssEnd)
		{
			int cchEnd;
			CheckHr(qtssEnd->get_Length(&cchEnd));
			if (cchEnd > 0 && ichEnd >= 0 && ichEnd <= cchEnd)	// See DN-836.
			{
				CheckHr(qtssEnd->get_PropertiesAt(ichEnd, &qttp));
				if (qttp)
					CheckHr(qttp->GetIntPropValues(ktptWs, &var, &wsEnd));
			}
		}
		else
		{
			wsEnd = wsAnchor;
		}
		if (wsAnchor == wsEnd && cch > 0)
		{
			Vector<int> & vws = m_qlpi->VernWss();
			for (int i = 0; i < vws.Size(); ++i)
			{
				if (wsAnchor == vws[i])
				{
					fEnable = true;
					break;
				}
			}
			// Include Analysis writing systems.  See comment on TE-6374.
			Vector<int> & vwsA = m_qlpi->AnalWss();
			for (int i = 0; i < vwsA.Size(); ++i)
			{
				if (wsAnchor == vwsA[i])
				{
					fEnable = true;
					break;
				}
			}
		}
	}
	cms.Enable(fEnable);
	return true;
}

bool RnMainWnd::HandleContextMenu(HWND hwnd, Point pt, IVwRootBox * prootb,
	AfVwRecSplitChild * prsc)
{
	if (prootb)
	{
		IVwSelectionPtr qvwsel;

		// See if we are over an External Link.
		bool fFoundLinkStyle;
		StrAppBuf strbFile;
		HMENU hmenuPopup = ::CreatePopupMenu();
		if (prsc->GetExternalLinkSel(fFoundLinkStyle, &qvwsel, strbFile) && fFoundLinkStyle)
		{
			CheckHr(qvwsel->Install());
			StrApp str;
			str.Load(kstidExtLinkOpen);
			::AppendMenu(hmenuPopup, MF_STRING, kcidExtLinkOpen, str.Chars());
			str.Load(kstidExtLinkOpenWith);
			::AppendMenu(hmenuPopup, MF_STRING, kcidExtLinkOpenWith, str.Chars());
			str.Load(kstidExtLink);
			::AppendMenu(hmenuPopup, MF_STRING, kcidExtLink, str.Chars());
			str.Load(kstidExtLinkRemove);
			::AppendMenu(hmenuPopup, MF_STRING, kcidExtLinkRemove, str.Chars());
		}
		prsc->AddExtraContextMenuItems(hmenuPopup);
		Point ptClient(pt);
		MapWindowPoints(NULL, hwnd, (POINT *)&ptClient, 1);
		TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_LEFTBUTTON | TPM_RIGHTBUTTON,
			pt.x, pt.y, 0, Hwnd(), NULL);
		::DestroyMenu(hmenuPopup);
		return true;
	}
	return false;
}

//:>********************************************************************************************
//:>	RnDbInfo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnDbInfo::RnDbInfo()
{
}


/*----------------------------------------------------------------------------------------------
	Return the AfLpInfo from the cache corresponding to the language project ID passed in. If
	it has not been created yet, create it now.

	@param hvoLp language project ID

	@return language project info
----------------------------------------------------------------------------------------------*/
AfLpInfo * RnDbInfo::GetLpInfo(HVO hvoLp)
{
	int clpi = m_vlpi.Size();
	for (int ilpi = 0; ilpi < clpi; ilpi++)
	{
		if (hvoLp == m_vlpi[ilpi]->GetLpId())
			return m_vlpi[ilpi];
	}

	// We didn't find it in the cache, so create it now.
	RnLpInfoPtr qlpi;
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
	RecordSpecs. For RnGenericRec, we add the BlockSpecs to both RnEvent and RnAnalysis.
	We are only storing top-level RecordSpecs at this point. If we need to display subentries
	as well, then we'll need to add additional RecordSpecs for subentries.

	@param puvs Out Ptr to user view spec.
----------------------------------------------------------------------------------------------*/
void RnDbInfo::CompleteBrowseRecordSpec(UserViewSpec * puvs)
{
	AssertPtr(puvs);
	AssertPtr(m_qmdc);

	RecordSpecPtr qrsp; // The display RecordSpec passed in to this function.
	ClsLevel clev;
	clev.m_nLevel = 0; // We are only reading top-level information.
	clev.m_clsid = 0; // This is the dummy display RecordSpec.
	puvs->m_hmclevrsp.Retrieve(clev, qrsp);
	AssertPtr(qrsp);

	RecordSpecPtr qrspEvent;
	RecordSpecPtr qrspAnalysis;

	// Create the two RecordSpecs used for loading data and add them to the UserViewSpec.
	clev.m_clsid = kclidRnEvent;
	qrspEvent.Attach(NewObj RecordSpec(kclidRnEvent, 0));
	qrspEvent->m_fNoSave = true;
	puvs->m_hmclevrsp.Insert(clev, qrspEvent, true);
	clev.m_clsid = kclidRnAnalysis;
	qrspAnalysis.Attach(NewObj RecordSpec(kclidRnAnalysis, 0));
	qrspAnalysis->m_fNoSave = true;
	puvs->m_hmclevrsp.Insert(clev, qrspAnalysis, true);

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
		case kclidRnEvent:
			qrspEvent->m_vqbsp.Push(qbsp);
			break;
		case kclidRnAnalysis:
			qrspAnalysis->m_vqbsp.Push(qbsp);
			break;
		case kclidRnGenericRec:
			qrspEvent->m_vqbsp.Push(qbsp);
			qrspAnalysis->m_vqbsp.Push(qbsp);
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
//:>	RnLpInfo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnLpInfo::RnLpInfo()
{
	// The ProjIds before kpidPsslLim are defined by the system (i.e. not user-definable), so
	// make sure we have enough room to store those.
	m_vhvoPsslIds.Resize(kpidPsslLim);
	m_hvoRn = 0;
}


/*----------------------------------------------------------------------------------------------
	Open a language project.
	@return true if successful
----------------------------------------------------------------------------------------------*/
bool RnLpInfo::OpenProject()
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
bool RnLpInfo::LoadProjBasics()
{
	// Set up the PrjIds array of ids for this project.
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stu;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	OLECHAR rgchProjName[MAX_PATH];
	OLECHAR rgchRNName[MAX_PATH];
	m_vhvoPsslIds.Clear(); // Clear any old values.
	m_vhvoPsslIds.Resize(kpidPsslLim);

	// Obtain pointer to IOleDbEncap interface and execute the given SQL select command.
	AssertPtr(m_qdbi);
	m_qdbi->GetDbAccess(&qode);

	try
	{
	/* Sample query:
	select rnb.dst rnb, con.dst con, res.dst res, wea.dst wea, rol.dst rol, peo.dst peo,
	loc.dst loc, ana.dst ana, typ.dst typ, tim.dst tim, edu.dst edu, pos.dst pos, cpn.Txt
	from LangProject lp
	left outer join LangProject_ResearchNotebook rnb on rnb.src = lp.id
	left outer join LangProject_ConfidenceLevels con on con.src = lp.id
	left outer join LangProject_Restrictions res on res.src = lp.id
	left outer join LangProject_WeatherConditions wea on wea.src = lp.id
	left outer join LangProject_Roles rol on rol.src = lp.id
	left outer join LangProject_People peo on peo.src = lp.id
	left outer join LangProject_Locations loc on loc.src = lp.id
	left outer join LangProject_AnalysisStatus ana on ana.src = lp.id
	left outer join RnResearchNbk_EventTypes typ on typ.src = rnb.dst
	left outer join LangProject_TimeOfDay tim on tim.src = lp.id
	left outer join LangProject_Education edu on edu.src = lp.id
	left outer join LangProject_Positions psn on psn.src = lp.id
	left outer join CmProject_Name cpn on cpn.obj = lp.id
	where lp.id = 1 and cpn.Ws = 740664001

	Sample results:
		rnb   con  res  wea  rol  peo  loc  ana  typ  tim  edu  pos  anit  Txt
		----- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ----------
		1611  25   30   35   62   66   77   13   1707 86   17   98   443  FRN-French
		*/
		stu.Format(L"select rnb.dst rnb, con.dst con, res.dst res, wea.dst wea, rol.dst rol, "
			L"peo.dst peo, loc.dst loc, ana.dst ana, typ.dst typ, tim.dst tim, edu.dst edu, "
			L"psn.dst psn, anit.dst anit "
			L"from LangProject lp "
			L"left outer join LangProject_ResearchNotebook rnb"
			L" on rnb.src = lp.id "
			L"left outer join LangProject_ConfidenceLevels con"
			L" on con.src = lp.id "
			L"left outer join LangProject_Restrictions res"
			L" on res.src = lp.id "
			L"left outer join LangProject_WeatherConditions wea"
			L" on wea.src = lp.id "
			L"left outer join LangProject_Roles rol on rol.src = lp.id "
			L"left outer join LangProject_People peo on peo.src = lp.id "
			L"left outer join LangProject_Locations loc"
			L" on loc.src = lp.id "
			L"left outer join LangProject_AnalysisStatus ana"
			L" on ana.src = lp.id "
			L"left outer join RnResearchNbk_EventTypes typ"
			L" on typ.src = rnb.dst "
			L"left outer join LangProject_TimeOfDay tim"
			L" on tim.src = lp.id "
			L"left outer join LangProject_Education edu"
			L" on edu.src = lp.id "
			L"left outer join LangProject_Positions psn"
			L" on psn.src = lp.id "
			L"left outer join LangProject_AnthroList anit"
			L" on anit.src = lp.id "
			L"where lp.id = %d", m_hvoLp);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&m_hvoRn),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslCon]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslRes]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslWea]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslRol]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(6, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslPeo]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(7, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslLoc]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(8, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslAna]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(9, reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslTyp]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(10,
				reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslTim]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(11,
				reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslEdu]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(12,
				reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslPsn]),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(13,
				reinterpret_cast<BYTE *>(&m_vhvoPsslIds[kpidPsslAnit]),
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

		stu.Format(L"exec GetOrderedMultiTxt '%d', %d",
			m_hvoLp, kflidCmProject_Name);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		Assert(fMoreRows); // This proc should always return something.
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchProjName),
			MAX_PATH * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));

		stu.Format(L"exec GetOrderedMultiTxt '%d', %d",
			m_hvoRn, kflidCmMajorObject_Name);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		Assert(fMoreRows); // This proc should always return something.
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchRNName),
			MAX_PATH * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));

		m_stuPrjName = rgchProjName;
		m_stuRNName = rgchRNName;
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
bool RnLpInfo::StoreAndSync(SyncInfo & sync)
{
	StoreSync(sync);
	AfDbApp * papp = dynamic_cast<AfDbApp *>(AfApp::Papp());
	if (papp)
		return papp->Synchronize(sync, this);
	return true;
}


//:>********************************************************************************************
//:>	RnOverlayListBar methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Create and return one of our overlay tool windows.
----------------------------------------------------------------------------------------------*/
void RnOverlayListBar::CreateOverlayTool(AfTagOverlayTool ** pprtot)
{
	AssertPtr(pprtot);
	*pprtot = NewObj RnTagOverlayTool;
}


/*----------------------------------------------------------------------------------------------
	Return the 'Configure' command ID for the context menu.
---------------------------------------------------------------------------------------------*/
int RnOverlayListBar::GetConfigureCid()
{
	return kcidViewOlaysConfig;
}

/*----------------------------------------------------------------------------------------------
	Bring up the Tools Option dialog.
----------------------------------------------------------------------------------------------*/
bool RnOverlayListBar::CmdToolsOpts(Cmd * pcmd)
{
	AssertObj(pcmd);
	Assert(pcmd->m_cid == kcidViewOlaysConfig);

	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);
	TlsOptDlgPtr qtod;
	qtod.Attach(prmw->GetTlsOptDlg());
	AssertObj(qtod);

	// Subtract one because of the No Overlay item.
	int iv = m_qvwbrs->GetContextSelection(prmw->GetViewbarListIndex(kvbltOverlay)) - 1;
	TlsDlgValue tgv;
	tgv.itabInitial = RnTlsOptDlg::kidlgOverlays;
	tgv.clsid = 0;
	tgv.nLevel = 0;
	tgv.iv1 = iv;
	tgv.iv2 = 0;
	qtod->SetDialogValues(tgv);

	if (qtod->DoModal(m_hwnd) == kctidOk)
	{
		qtod->ClearNewFilterIndexes();
		qtod->ClearFilterViewBars();
		qtod->SaveDialogValues();

		// Moved from TlsOptDlg::SaveFilterValues() to prevent massive use of
		// "GDI Object" and "User Object" resources.
		Vector<int> vifltNew = qtod->GetNewFilterIndexes();
		Vector<AfViewBarShell *> vpvwbrs = qtod->GetFilterViewBars();
		Assert(vifltNew.Size() == vpvwbrs.Size());
		qtod.Clear();
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
	}
	return true;
}


//:>********************************************************************************************
//:>	RnTagOverlayTool methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Open a dialog that allows the user to modify an overlay.
----------------------------------------------------------------------------------------------*/
bool RnTagOverlayTool::OnConfigureTag(int iovr, int itag)
{
	RnTlsOptDlgPtr qrtod;
	qrtod.Create();

	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);

	TlsDlgValue tgv;
	tgv.itabInitial = RnTlsOptDlg::kidlgOverlays;
	tgv.clsid = 0;
	tgv.nLevel = 0;
	tgv.iv1 = iovr;
	tgv.iv2 = itag;

	qrtod->SetDialogValues(tgv);
	if (qrtod->DoModal(m_hwnd) == kctidOk)
	{
		qrtod->ClearNewFilterIndexes();
		qrtod->ClearFilterViewBars();

		qrtod->SaveDialogValues();

		// Moved from TlsOptDlg::SaveFilterValues() to prevent massive use of
		// "GDI Object" and "User Object" resources.
		Vector<int> vifltNew = qrtod->GetNewFilterIndexes();
		Vector<AfViewBarShell *> vpvwbrs = qrtod->GetFilterViewBars();
		Assert(vifltNew.Size() == vpvwbrs.Size());
		qrtod.Clear();
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
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Notify each main window that is showing the same language project of the change in the
	given overlay.

	@param iovr
----------------------------------------------------------------------------------------------*/
bool RnTagOverlayTool::OnChangeOverlay(int iovr)
{
	RecMainWnd * prmwCur = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmwCur);
	AfLpInfo * plpiCur = prmwCur->GetLpInfo();
	AssertPtr(plpiCur);

	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	int cwnd = vqafw.Size();
	for (int iwnd = 0; iwnd < cwnd; iwnd++)
	{
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(vqafw[iwnd].Ptr());
		AssertPtr(prmw);
		if (prmw->GetLpInfo() == plpiCur)
			prmw->OnChangeOverlay(iovr);
	}

	// Redraw the list window.
	AssertPtr(m_qtopl);
	::InvalidateRect(m_qtopl->Hwnd(), NULL, true);

	return true;
}


//:>********************************************************************************************
//:>	RnFilterNoMatchDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Create the Data Notebook version of the Tools-Options.
	Launch it with the Filter tab selected.

	@param pptod out ptr to Tools-Options dialog after it is exited.
----------------------------------------------------------------------------------------------*/
void RnFilterNoMatchDlg::GetTlsOptDlg(TlsOptDlg ** pptod)
{
	AssertPtr(pptod);

	// Launch the Tools-Options dialog with the Filter tab selected.
	RnTlsOptDlgPtr qrtod;
	qrtod.Create();

	TlsDlgValue tgv;
	tgv.itabInitial = RnTlsOptDlg::kidlgFilters;
	tgv.clsid = 0;
	tgv.nLevel = 0;
	tgv.iv1 = m_iflt;
	tgv.iv2 = 0;
	qrtod->SetDialogValues(tgv);
	*pptod = qrtod.Detach();
}


//:>********************************************************************************************
//:>	RnListBar methods.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Bring up the Tools Option dialog.

	@param pcmd ptr to menu command.(This parameter is not used in in this method.)

	@return true.
----------------------------------------------------------------------------------------------*/
bool RnListBar::CmdToolsOpts(Cmd * pcmd)
{
	AssertObj(pcmd);

	// Get a pointer to the current MainWnd which should be a RecMainWnd.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(AfApp::Papp()->GetCurMainWnd());
	Assert(prmw);
	TlsOptDlgPtr qtod;
	qtod.Attach(prmw->GetTlsOptDlg());
	AssertObj(qtod);

	TlsDlgValue tgv;
	tgv.clsid = 0;
	tgv.nLevel = 0;
	tgv.iv2 = 0;

	switch (pcmd->m_cid)
	{
	case kcidViewViewsConfig:
		tgv.iv1 = m_qvwbrs->GetContextSelection(prmw->GetViewbarListIndex(kvbltView));
		tgv.itabInitial = RnTlsOptDlg::kidlgViews;
		break;
	case kcidViewFltrsConfig:
		// Subtract one because of the No Filter item.
		tgv.iv1 = m_qvwbrs->GetContextSelection(prmw->GetViewbarListIndex(kvbltFilter)) - 1;
		tgv.itabInitial = RnTlsOptDlg::kidlgFilters;
		break;
	case kcidViewSortsConfig:
		// Subtract one because of the Default Sort item.
		tgv.iv1 = m_qvwbrs->GetContextSelection(prmw->GetViewbarListIndex(kvbltSort)) - 1;
		tgv.itabInitial = RnTlsOptDlg::kidlgSortMethods;
		break;
	default:
		tgv.iv1 = 0;
		tgv.itabInitial = 0;
		break;
	}

	// Close editors and save window information; run the dialog; restore windows.
	prmw->RunTlsOptDlg(qtod, tgv);

	// Moved from TlsOptDlg::SaveFilterValues() to prevent massive use of
	// "GDI Object" and "User Object" resources.
	Vector<int> vifltNew = qtod->GetNewFilterIndexes();
	Vector<AfViewBarShell *> vpvwbrs = qtod->GetFilterViewBars();
	Assert(vifltNew.Size() == vpvwbrs.Size());
	qtod.Clear();
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
//:>	RnCaptionBar methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnCaptionBar::RnCaptionBar(RecMainWnd * prmwMain)
	: AfRecCaptionBar(prmwMain)
{
}


/*----------------------------------------------------------------------------------------------
	Show the appropriate context menu based on the icon that was right-clicked on.

	@param ibtn captionbar index for the button
	@param pt point at which to place the menu
----------------------------------------------------------------------------------------------*/
void RnCaptionBar::ShowContextMenu(int ibtn, Point pt)
{
	HMENU hmenuPopup = ::CreatePopupMenu();
	StrApp str(kstidConfigureVBar);
	int ivblt = m_prmwMain->GetViewbarListType(ibtn);

	switch (ivblt)
	{
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
	case kvbltOverlay:
		::AppendMenu(hmenuPopup, MF_STRING, kcidExpOverlays, NULL);
		::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
		::AppendMenu(hmenuPopup, MF_STRING, kcidViewOlaysConfig, str.Chars());
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
//:>	RnEmptyNotebookDlg Implementation.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnEmptyNotebookDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Display the "Information" icon.
	HICON hicon = ::LoadIcon(NULL, MAKEINTRESOURCE(IDI_INFORMATION));
	if (hicon)
	{
		::SendMessage(::GetDlgItem(m_hwnd, kridEmptyNotebookIcon), STM_SETICON, (WPARAM)hicon,
			(LPARAM)0);
	}

	// Set the font for the header, and display the header.
	m_hfontLarge = AfGdi::CreateFont(16, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
		::SendMessage(::GetDlgItem(m_hwnd, kridEmptyNotebookHeader), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	StrApp strFmt(kstidEmptyNotebookHeaderFmt);
	StrApp str;
	str.Format(strFmt.Chars(), m_strProj.Chars());
	::SetWindowText(::GetDlgItem(m_hwnd, kridEmptyNotebookHeader), str.Chars());

	// Subclass the Event and Analysis buttons to show the same images as the menu commands.
	AfButtonPtr qbtn;
	qbtn.Create();
	AfMenuMgr * pmum = AfApp::GetMenuMgr();
	HIMAGELIST himl = pmum->GetImageList();

	qbtn->SubclassButton(m_hwnd, kctidEmptyNewEvent, kbtImage, himl,
		pmum->GetImagFromCid(kcidInsEntryEvent));
	qbtn.Clear();

	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidEmptyNewAnalysis, kbtImage, himl,
		pmum->GetImagFromCid(kcidInsEntryAnal));

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnEmptyNotebookDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctidFrom)
		{
		case kctidEmptyNewEvent:
		case kctidEmptyNewAnalysis:
		case kctidEmptyImport:
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
		"CBN_SELCHANGE",		//	1
		"CBN_DBLCLK",			//	2
		"CBN_SETFOCUS",			//	3
		"CBN_KILLFOCUS",		//	4
		"CBN_EDITCHANGE",		//	5
		"CBN_EDITUPDATE",		//	6
		"CBN_DROPDOWN",			//	7
		"CBN_CLOSEUP",			//	8
		"CBN_SELENDOK",			//	9
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
	case kcidToolsLists:			return "kcidToolsLists";			break;
	case kcidStats:					return "kcidStats";					break;
	case kcidToolsOpts:				return "kcidToolsOpts";				break;
	case kcidFmtPara:				return "kcidFmtPara";				break;
	case kcidFmtBdr:				return "kcidFmtBdr";				break;
	case kcidFmtFnt:				return "kcidFmtFnt";				break;
	case kcidFmtBulNum:				return "kcidFmtBulNum";				break;
	case kcidFmtStyles:				return "kcidFmtStyles";				break;
	case kcidFmttbStyle:			return "kcidFmttbStyle";			break;
	case kcidEditSrchQuick:			return "kcidEditSrchQuick";			break;
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

//:>********************************************************************************************
//:>	RnFilterXrefUtil methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Process a cross reference field for the From clause of a filter's constructed SQL query.

	@param flid Field id of the cross reference target.
	@param fJoin Flag whether this needs a new join clause.
	@param ialias Reference to the alias counter for the From clause under construction.
	@param ialiasLastClass The alias index for the most recent class encountered in the filter.
	@param pmdc Pointer to the database meta-data cache object.
	@param stuFromClause Reference to the from clause being constructed.
	@param stuWJoin Reference to the subordinate where/from/join clause used for negations.
	@param sbstrClass Reference to the class name of the cross reference target.
	@param sbstrField Reference to the field name of the cross reference target.
	@param stuAliasText Reference to an SQL alias string which is modified by this method.
	@param stuAliasId Reference to an SQL alias string which is modified by this method.

	@return True if cross references are valid, false if they are invalid.
----------------------------------------------------------------------------------------------*/
bool RnFilterXrefUtil::ProcessCrossRefColumn(int flid, bool fJoin, int & ialias,
	int ialiasLastClass, IFwMetaDataCache * pmdc, StrUni & stuFromClause, StrUni & stuWJoin,
	SmartBstr & sbstrClass, SmartBstr & sbstrField, StrUni & stuAliasText, StrUni & stuAliasId)
{
	Assert(flid == kflidRnGenericRec_SeeAlso ||
		flid == kflidRnAnalysis_SupersededBy || flid == kflidRnAnalysis_SupportingEvidence);

	if (flid == kflidRnGenericRec_SeeAlso ||
		flid == kflidRnAnalysis_SupersededBy || flid == kflidRnAnalysis_SupportingEvidence)
	{
		if (fJoin)
		{
			stuFromClause.FormatAppend(L""
				L"  left outer join %s as t%d on t%d.id = %s%n",
				sbstrClass.Chars(), ialias, ialias, stuAliasText.Chars());
			stuWJoin.Format(L""
				L"  join %s as w%d on w%d.id = w%s%n",
				sbstrClass.Chars(), ialias, ialias, stuAliasText.Chars() + 1);
			stuAliasText.Format(L"t%d.%s", ialias, sbstrField.Chars());
			ialias++;
		}
		else
		{
			Assert(ialiasLastClass != -1);
			stuWJoin.Clear();
			stuAliasText.Format(L"t%d.%s", ialiasLastClass, sbstrField.Chars());
		}
		CheckHr(pmdc->GetOwnClsName(kflidRnGenericRec_Title, &sbstrClass));
		CheckHr(pmdc->GetFieldName(kflidRnGenericRec_Title, &sbstrField));
		stuFromClause.FormatAppend(L""
			L"  left outer join %s_ as t%d on t%d.id = %s%n",
			sbstrClass.Chars(), ialias, ialias, stuAliasText.Chars());
		stuWJoin.FormatAppend(L""
			L"  join %s_ as w%d on w%d.id = w%s%n",
			sbstrClass.Chars(), ialias, ialias,
			stuAliasText.Chars() + 1);
		stuAliasId.Format(L"t%d.id", ialias);
		stuAliasText.Format(L"t%d.%s", ialias, sbstrField.Chars());
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Process a cross reference list field for the From clause of a filter's constructed SQL
	query.

	@param flid Field id of the cross reference target.
	@param ialias Reference to the alias counter for the From clause under construction.
	@param ialiasLastClass The alias index for the most recent class encountered in the filter.
	@param pmdc Pointer to the database meta-data cache object.
	@param stuFromClause Reference to the from clause being constructed.
	@param stuWJoin Reference to the subordinate where/from/join clause used for negations.
	@param sbstrClass Reference to the class name of the cross reference target.
	@param sbstrField Reference to the field name of the cross reference target.
	@param stuAliasText Reference to an SQL alias string which is modified by this method.
	@param stuAliasId Reference to an SQL alias string which is modified by this method.

	@return True if cross references are valid, false if they are invalid.
----------------------------------------------------------------------------------------------*/
bool RnFilterXrefUtil::ProcessCrossRefListColumn(int flid, int & ialias, int ialiasLastClass,
	IFwMetaDataCache * pmdc, StrUni & stuFromClause, StrUni & stuWJoin, SmartBstr & sbstrClass,
	SmartBstr & sbstrField, StrUni & stuAliasText, StrUni & stuAliasId)
{
	Assert(flid == kflidRnGenericRec_SeeAlso ||
		flid == kflidRnAnalysis_SupersededBy || flid == kflidRnAnalysis_SupportingEvidence);

	if (flid == kflidRnGenericRec_SeeAlso ||
		flid == kflidRnAnalysis_SupersededBy || flid == kflidRnAnalysis_SupportingEvidence)
	{
		stuFromClause.FormatAppend(L""
			L"  left outer join %s_%s as t%d on t%d.src = %s%n",
			sbstrClass.Chars(), sbstrField.Chars(), ialias, ialias,
			stuAliasText.Chars());
		stuWJoin.Format(L""
			L"  join %s_%s as w%d on w%d.src = w%s%n",
			sbstrClass.Chars(), sbstrField.Chars(), ialias, ialias,
			stuAliasText.Chars() + 1);

		CheckHr(pmdc->GetOwnClsName(kflidRnGenericRec_Title, &sbstrClass));
		CheckHr(pmdc->GetFieldName(kflidRnGenericRec_Title, &sbstrField));
		ialias++;
		stuFromClause.FormatAppend(L"  left outer join %s_ as t%d "
			L"on t%d.id = t%d.dst%n",
			sbstrClass.Chars(), ialias, ialias, ialias - 1);
		stuWJoin.FormatAppend(L"  join %s_ as w%d"
			L" on w%d.id = w%d.dst%n",
			sbstrClass.Chars(), ialias, ialias, ialias - 1);
		stuAliasId.Format(L"t%d.id", ialias);
		stuAliasText.Format(L"t%d.%s", ialias, sbstrField.Chars());
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Double any single quotes in the string, so that the string can be used in an SQL literal.

	@param stu Reference to the string to check and fix as needed.
----------------------------------------------------------------------------------------------*/
static void FixQuotes(StrUni & stu)
{
	int ich = stu.FindCh('\'', 0);
	while (ich >= 0 && ich < stu.Length())
	{
		stu.Replace(ich, ich, "'");			// insert another single quote
		ich = stu.FindCh('\'', ich + 2);	// look for another single quote
	}
};

/*----------------------------------------------------------------------------------------------
	Fix the 'alias text' value for a cross reference title.

	@param stuAliasText Reference to the SQL reference string which is modified by this method.
	@param flid Field id of the target of this filter.

	@return True if cross references are valid, false if they are invalid.
----------------------------------------------------------------------------------------------*/
bool RnFilterXrefUtil::FixCrossRefTitle(StrUni & stuAliasText, int flid)
{
	Assert(flid == kflidRnGenericRec_SeeAlso ||
		flid == kflidRnAnalysis_SupersededBy || flid == kflidRnAnalysis_SupportingEvidence);

	if (flid == kflidRnGenericRec_SeeAlso ||
		flid == kflidRnAnalysis_SupersededBy || flid == kflidRnAnalysis_SupportingEvidence)
	{
		StrUni stuAlias(stuAliasText);
		int ich = stuAlias.FindCh('.');
		Assert(ich > 0);
		stuAlias.Replace(ich, stuAlias.Length(), L"");
		StrUni stuField(stuAliasText);
		StrUni stuAnalysis(kstidAnalysis);
		StrUni stuSubAnalysis(kstidSubanalysis);
		StrUni stuEvent(kstidEvent);
		StrUni stuSubEvent(kstidSubevent);
		StrUni stuSep(kstidSpHyphenSp);

		FixQuotes(stuAnalysis);
		FixQuotes(stuSubAnalysis);
		FixQuotes(stuEvent);
		FixQuotes(stuSubEvent);
		FixQuotes(stuSep);

		StrUni stu;
		stu.Format(L"CASE%n"
			L"   WHEN %s.Class$ = %d AND %s.OwnFlid$ = %d THEN '%s'%n"
			L"   WHEN %s.Class$ = %d AND %s.OwnFlid$ = %d THEN '%s'%n"
			L"   WHEN %s.Class$ = %d AND %s.OwnFlid$ = %d THEN '%s'%n"
			L"   ELSE '%s'%n"
			L"END+'%s'+ISNULL(%s, '')+'%s'",
			stuAlias.Chars(), kclidRnAnalysis, stuAlias.Chars(),
			kflidRnResearchNbk_Records, stuAnalysis.Chars(),
			stuAlias.Chars(), kclidRnAnalysis, stuAlias.Chars(),
			kflidRnGenericRec_SubRecords, stuSubAnalysis.Chars(),
			stuAlias.Chars(), kclidRnEvent, stuAlias.Chars(),
			kflidRnResearchNbk_Records, stuEvent.Chars(), stuSubEvent.Chars(),
			stuSep.Chars(), stuField.Chars(), stuSep.Chars());

		// Convert the date information.

		StrAppBuf strbDateFmt;
		int cch = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SSHORTDATE,
			const_cast<achar *>(strbDateFmt.Chars()), strbDateFmt.kcchMaxStr);
		strbDateFmt.SetLength(cch - 1);		// cch includes the terminating NUL.
		int cchSame;
		for (ich = 0; ich < strbDateFmt.Length(); ++ich)
		{
			switch (strbDateFmt[ich])
			{
			case 'd':
				cchSame = 1;
				while (ich + 1 < strbDateFmt.Length() && strbDateFmt[ich+1] == 'd')
				{
					++ich;
					++cchSame;
				}
				/*
				  d		Day of month as digits with no leading zero for single-digit days.
				  dd	Day of month as digits with leading zero for single-digit days.
				  ddd	Day of week as a three-letter abbreviation.
				  dddd	Day of week as its full name.
				*/
				switch (cchSame)
				{
				case 1:
					stu.FormatAppend(L"+%nDATENAME(day, %s.DateCreated)", stuAlias.Chars());
					break;
				case 2:
					stu.FormatAppend(L"+%n"
						L"CASE WHEN DATEPART(day, %s.DateCreated) < 10 THEN '0' ELSE '' END+%n"
						L"DATENAME(day, %s.DateCreated)",
						stuAlias.Chars(), stuAlias.Chars());
					break;
				case 3:
					stu.FormatAppend(L"+%nLEFT(DATENAME(weekday, %s.DateCreated), 3)",
						stuAlias.Chars());
					break;
				case 4:
					// Treat 5 or more the same as 4.  It shouldn't happen!
				default:
					stu.FormatAppend(L"+%nDATENAME(weekday, %s.DateCreated)", stuAlias.Chars());
					break;
				}
				break;
			case 'M':
				cchSame = 1;
				while (ich + 1 < strbDateFmt.Length() && strbDateFmt[ich+1] == 'M')
				{
					++ich;
					++cchSame;
				}
				/*
				  M		Month as digits with no leading zero for single-digit months.
				  MM	Month as digits with leading zero for single-digit months.
				  MMM	Month as a three-letter abbreviation.
				  MMMM	Month as its full name.
				*/
				switch (cchSame)
				{
				case 1:
					stu.FormatAppend(L"+%nDATEPART(month, %s.DateCreated)", stuAlias.Chars());
					break;
				case 2:
					stu.FormatAppend(L"+%n"
						L"CASE WHEN DATEPART(month,%s.DateCreated) < 10 THEN '0' ELSE '' END+%n"
						L"DATEPART(month, %s.DateCreated)",
						stuAlias.Chars(), stuAlias.Chars());
					break;
				case 3:
					stu.FormatAppend(L"+%nLEFT(DATENAME(month, %s.DateCreated), 3)",
						stuAlias.Chars());
					break;
				case 4:
					// Treat 5 or more the same as 4.  It shouldn't happen!
				default:
					stu.FormatAppend(L"+%nDATENAME(month, %s.DateCreated)", stuAlias.Chars());
					break;
				}
				break;
			case 'y':
				cchSame = 1;
				while (ich + 1 < strbDateFmt.Length() && strbDateFmt[ich+1] == 'y')
				{
					++ich;
					++cchSame;
				}
				/*
				  y    Year as last two digits, but with no leading zero for years less than 10.
				  yy   Year as last two digits, but with leading zero for years less than 10.
				  yyyy Year represented by full four digits.
				*/
				switch (cchSame)
				{
				case 1:
					stu.FormatAppend(L"+%n"
						L"CONVERT(nvarchar,DATEPART(year, %s.DateCreated) %% 100)",
						stuAlias.Chars());
					break;
				case 2:
					stu.FormatAppend(L"+%nRIGHT(DATENAME(year, %s.DateCreated), 2)",
						stuAlias.Chars());
					break;
				case 3:
					// Treat 3 the same as 4.  It shouldn't happen!
				case 4:
					// Treat 5 or more the same as 4.  It shouldn't happen!
				default:
					stu.FormatAppend(L"+%nDATENAME(year, %s.DateCreated)", stuAlias.Chars());
					break;
				}
				break;
			case 'g':
				/*
				  gg	Period/era string. (?)
				*/
				// We don't know how to handle this, so ignore it.
				break;
			default:
				/*
				  Anything else is a literal character.
				*/
				stu.FormatAppend(L"+'");
				if (strbDateFmt[ich] == '\'')
					stu.Append(L"''");
				else
					stu.Append(strbDateFmt.Chars() + ich, 1);
				while (ich + 1 < strbDateFmt.Length() && strbDateFmt[ich+1] != 'd' &&
					strbDateFmt[ich+1] != 'M' && strbDateFmt[ich+1] != 'y' &&
					strbDateFmt[ich+1] != 'g')
				{
					++ich;
					if (strbDateFmt[ich] == '\'')
						stu.Append(L"''");
					else
						stu.Append(strbDateFmt.Chars() + ich, 1);
				}
				stu.FormatAppend(L"'");
				break;
			}
		}
		stuAliasText = stu;
		return true;
	}
	return false;
}

#include "Vector_i.cpp"
#include "Set_i.cpp"
#include "GpHashMap_i.cpp" // Needed for release version.
#include "HashMap_i.cpp"

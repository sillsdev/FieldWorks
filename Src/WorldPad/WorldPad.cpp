/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WorldPad.cpp
Responsibility: Sharon Correll
Last reviewed: never

Description:
	This class provides the base for WorldPad functions.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:End Ignore

// Create one global instance. It has to exist before WinMain is called.
static WpApp g_app;


const int knStatusTimer = 5;

BEGIN_CMD_MAP(WpApp)
	ON_CID_ALL(kcidFileOpen, &WpApp::CmdFileOpen, NULL)
	ON_CID_ALL(kcidFileSave, &WpApp::CmdFileSave, &WpApp::CmsFileSave)
	ON_CID_ALL(kcidFileSaveAs, &WpApp::CmdFileSaveAs, NULL)
	ON_CID_ALL(kcidFileExit, &WpApp::CmdFileExit, NULL)
//	ON_CID_ALL(kcidToolsOpts, &WpApp::CmdToolsOpts, NULL)
	ON_CID_ALL(kcidHelpAbout, &WpApp::CmdHelpAbout, NULL)
	ON_CID_ALL(kcidWndCascad, &WpApp::CmdWndCascade, NULL)
	ON_CID_ALL(kcidWndTile, &WpApp::CmdWndTileHoriz, NULL)
	ON_CID_ALL(kcidWndSideBy, &WpApp::CmdWndTileVert, NULL)
#if 0 // ifdef DEBUG
//	ON_CID_ALL(kcidPossChsr, &WpApp::CmdPossChsr, NULL)
#endif
END_CMD_MAP_NIL()


//:>--------------------------------------------------------------------------------------------
//:>	The command map for the main window.
//:>--------------------------------------------------------------------------------------------
BEGIN_CMD_MAP(WpMainWnd)
	ON_CID_GEN(kcidViewStatBar, &WpMainWnd::CmdSbToggle, &WpMainWnd::CmsSbUpdate)
	ON_CID_ME(kcidToolsOpts, &WpMainWnd::CmdToolsOpts, NULL)
	ON_CID_ME(kcidToolsOldWritingSystems, &WpMainWnd::CmdToolsWritingSysProps, NULL)

	ON_CID_ME(kcidHelpConts,     &WpMainWnd::CmdHelpFw,   NULL)
	ON_CID_ME(kcidHelpApp,       &WpMainWnd::CmdHelpApp,  NULL)
	ON_CID_ME(kcidHelpWhatsThis, &WpMainWnd::CmdHelpMode, NULL)

	ON_CID_ME(kcidFileNew, &WpMainWnd::CmdWndNew, NULL)
	ON_CID_ME(kcidFileClose, &WpMainWnd::CmdWndClose, &WpMainWnd::CmsWndClose)

	ON_CID_ME(kcidFilePageSetup, &WpMainWnd::CmdFilePageSetup, NULL)

	ON_CID_ME(kcidWndNew, &WpMainWnd::CmdWndNew, NULL)
	ON_CID_ME(kcidWndSplit, &WpMainWnd::CmdWndSplit, &WpMainWnd::CmsWndSplit)

	ON_CID_ME(kcidExtLinkOpen,     &AfMainWnd::CmdExternalLink, NULL)
	ON_CID_ME(kcidExtLinkOpenWith, &AfMainWnd::CmdExternalLink, NULL)
	ON_CID_ME(kcidExtLink,         &AfMainWnd::CmdExternalLink, &AfMainWnd::CmsInsertLink)
	ON_CID_ME(kcidExtLinkRemove,   &AfMainWnd::CmdExternalLink, NULL)
END_CMD_MAP_NIL()


//:>--------------------------------------------------------------------------------------------
//:>	The command map for the child window.
//:>--------------------------------------------------------------------------------------------
BEGIN_CMD_MAP(WpChildWnd)
	ON_CID_ALL(kcidEditUndo, &WpChildWnd::CmdEditUndo, &WpChildWnd::CmsEditUndo)
	ON_CID_ALL(kcidEditRedo, &WpChildWnd::CmdEditRedo, &WpChildWnd::CmsEditRedo)
//	ON_CID_ALL(kcidFmtBulNum, &WpChildWnd::CmdFmtBulNum, &AfVwSplitChild::CmsFmtBulNum)
//	ON_CID_ALL(kcidFmtBdr, &WpChildWnd::CmdFmtBdr, &AfVwSplitChild::CmsFmtBdr)
	ON_CID_ALL(kcidFmtDoc, &WpChildWnd::CmdFmtDoc, NULL)
	ON_CID_ALL(kcidEditSelAll, &WpChildWnd::CmdEditSelAll1, &WpChildWnd::CmsEditSelAll1)
END_CMD_MAP_NIL()


//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance of AfFwTool with CoCreateInstance,
//:>	so as to start up new main windows from another EXE (e.g., the Explorer).
//:>********************************************************************************************
static GenericFactory g_factWorldPad(
	_T("SIL.WorldPad"),
	&CLSID_WorldPad,
	_T("SIL WorldPad"),
	_T("Apartment"),
	&AfFwTool::CreateCom);


/*----------------------------------------------------------------------------------------------
	Constructor for WpApp.
----------------------------------------------------------------------------------------------*/
WpApp::WpApp()
{
	s_fws.SetRoot(_T("WorldPad"));//"Software\\SIL\\FieldWorks\\WorldPad";

	AfApp::Papp()->SetHelpBaseName(_T("\\Helps\\FieldWorks_WorldPad_Help.chm"));

	m_fLogicalArrow = true;
	m_fLogicalShiftArrow = true;
	m_fLogicalHomeEnd = true;
	m_fGraphiteLog = false;
}

/*----------------------------------------------------------------------------------------------
	Initialize the application.
----------------------------------------------------------------------------------------------*/
void WpApp::Init(void)
{
	Vector<StrUni> vstuArg;
	StrUni stuKey;

	// Check for -install option.
	stuKey = L"install";
	if (m_hmsuvstuCmdLine.Retrieve(stuKey, &vstuArg))
	{
		RunInstall();
		m_fQuit = true;
		return;
	}

	// Check for -uninstall option.
	stuKey = L"uninstall";
	if (m_hmsuvstuCmdLine.Retrieve(stuKey, &vstuArg))
	{
		RunUninstall();
		m_fQuit = true;
		return;
	}

	// Check for -vertical option.
	m_fVertical = false;
	stuKey = L"v";
	if (m_hmsuvstuCmdLine.Retrieve(stuKey, &vstuArg))
	{
		m_fVertical = true;
	}


	AfApp::Init();

	AfWnd::RegisterClass(_T("WpMainWnd"), 0, 0, 0, COLOR_3DFACE, (int)kridWorldPadIcon);

	IFwToolPtr qtool;
	qtool.CreateInstance(CLSID_WorldPad);
	HANDLE_PTR htool;
	int pidNew;
	stuKey = L"filename";
	Vector<StrUni> vstuFilenames;
	m_hmsuvstuCmdLine.Retrieve(stuKey, &vstuFilenames);
	if (vstuFilenames.Size() == 0)
	{
		// Open a new document.
		StrUni stuFilename;
		vstuFilenames.Push(stuFilename);
	}
	// We have a chicken and eggs problem here: at this point, the default factory is empty, so
	// we can't feed in the user writing system id.
	int wsUser = 0;
	StrUni stuEmpty;	// IFwTool::NewMainWnd requires a non-NULL BSTR for the server name.
	for (int ifile = 0; ifile < vstuFilenames.Size(); ifile++)
	{
		// We are using the DbName parameter here to pass filenames in.
		CheckHr(qtool->NewMainWnd(stuEmpty.Bstr(), vstuFilenames[ifile].Bstr(), 0, 0,
			wsUser, 0, 0, &pidNew, &htool));
	}
}

/*----------------------------------------------------------------------------------------------
	Look for the default template and return its name (path).
----------------------------------------------------------------------------------------------*/
bool WpApp::FindDefaultTemplate(StrAnsi * pstaDefaultTemplate)
{
	pstaDefaultTemplate->Assign(DirectoryFinder::FwRootCodeDir().Chars());
	int cch = pstaDefaultTemplate->Length();
	if (cch)
	{
		if ((*pstaDefaultTemplate)[cch - 1] != '\\')
			(*pstaDefaultTemplate) += "\\";
		(*pstaDefaultTemplate) += "WorldPad\\default.wpt";
		FILE * f;
		if (!fopen_s(&f, pstaDefaultTemplate->Chars(), "r"))
		{
			// File exists.
			fclose(f);
			return true;
		}
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Do what's needed when installing WorldPad.
	Review: Decide whether we want to zap any old list of encodings.
----------------------------------------------------------------------------------------------*/
void WpApp::RunInstall()
{
}

/*----------------------------------------------------------------------------------------------
	Do what's needed when uninstalling WorldPad.
----------------------------------------------------------------------------------------------*/
void WpApp::RunUninstall()
{
	////DeleteAllEncodings(); -- no longer do this; they are needed for FW in general
}

/*----------------------------------------------------------------------------------------------
	Pass the idle message on to the main window.
----------------------------------------------------------------------------------------------*/
void WpApp::OnIdle()
{
	// REVIEW DarrellZ: Does this need to call OnIdle for all the top-level windows
	// or just for the one that has the focus?
	AssertObj(m_qafwCur);

	// Any updates that happen during the idling should be part of the previous sequence of
	// actions for undoing:
	WpMainWnd * pwpwnd = dynamic_cast<WpMainWnd *>(m_qafwCur.Ptr());
	AssertPtr(pwpwnd);
	WpSplitWnd * pwsw = (pwpwnd) ? pwpwnd->SplitWnd() : NULL;
	WpChildWnd * pwcw = (pwsw) ? pwsw->ChildWnd() : NULL;
	WpDa * pda = (pwcw) ? pwcw->DataAccess() : NULL;
	if (pda)
		pda->ContinueUndoTask();

	m_qafwCur->OnIdle();

	// Clean up any open sequence of undo actions:
	if (pda)
		pda->EndOuterUndoTask();
}

/*----------------------------------------------------------------------------------------------
	Handle the File/Open command.
----------------------------------------------------------------------------------------------*/
bool WpApp::CmdFileOpen(Cmd * pcmd)
{
	WpMainWnd * pwpwnd = dynamic_cast<WpMainWnd *>(m_qafwCur.Ptr());
	AssertPtr(pwpwnd);
	int fiet = pwpwnd->FileType();
	//if (fiet == kfietHtml)
	//	fiet = kfietUnknown;

	OPENFILENAME ofn;		// common dialog box structure
	static achar szFile[260] = {0};	// buffer for filename

	// Initialize OPENFILENAME
	ZeroMemory(&ofn, sizeof(OPENFILENAME));
	// the constant below is required for backward compatibility with Windows 95/98
	ofn.lStructSize = OPENFILENAME_SIZE_VERSION_400;
	ofn.hwndOwner = pwpwnd->Hwnd();
	ofn.lpstrFile = szFile;
	ofn.nMaxFile = sizeof(szFile);
//	ofn.lpstrFilter =
//		"All\0*.*\0WorldPad (*.wpx) \0*.wpx\0UTF-16\0*.TXT\0UTF-8\0*.TXT\0ANSI\0*.TXT\0";
	// line below must match file-types enumeration:
	ofn.lpstrFilter =
		_T("All\0*.*\0WorldPad XML (*.wpx) \0*.wpx\0WorldPad Template (*.wpt) \0*.wpt\0")
		_T("Plain Text (*.txt)\0*.TXT\0");
	ofn.nFilterIndex = (fiet <= 0 || fiet >= kfietLim) ? kfietXml : fiet;
	ofn.lpstrFileTitle = NULL;
	ofn.nMaxFileTitle = 0;
	ofn.lpstrInitialDir = NULL;
	ofn.Flags = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST | OFN_HIDEREADONLY;

	//	Turn off any special keyboards they were using for their data.
	//	Review: this feels like a kludge. Is there a better way?
	pwpwnd->ActivateDefaultKeyboard();

	// Display the Open dialog box.
	bool f = GetOpenFileName(&ofn);

	pwpwnd->RestoreCurrentKeyboard();

	if (!f)
		return false;

	fiet = ofn.nFilterIndex;
	if (fiet >= kfietPlain)
		fiet = kfietUnknown;
	if (WpMainWnd::GuessFileType(szFile) == kfietTemplate) // .wpt extension
		fiet = kfietTemplate;

	pwpwnd->SetAutoDefault(false);

	//	Create the new window, initialized from the file.
	StrAnsi staFile;
	staFile.Assign(szFile);		// FIX ME FOR PROPER CODE CONVERSION!
	return pwpwnd->DoOpenFile(const_cast<char *>(staFile.Chars()), fiet);
}

/*----------------------------------------------------------------------------------------------
	Enable the File-Save command.
----------------------------------------------------------------------------------------------*/
bool WpApp::CmsFileSave(CmdState &cms)
{
//	WpMainWnd * pwpwnd = dynamic_cast<WpMainWnd *>(m_qafwCur.Ptr());
//	AssertPtr(pwpwnd);
//	bool f = pwpwnd->DataAccess()->IsDirty();
//	cms.Enable(f);
	cms.Enable(true); // always allowed
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the File-Save command.
----------------------------------------------------------------------------------------------*/
bool WpApp::CmdFileSave(Cmd * pcmd)
{
	WpMainWnd * pwpwnd = dynamic_cast<WpMainWnd *>(m_qafwCur.Ptr());
	AssertPtr(pwpwnd);

	// Now we allowing Ctrl-S all the time, whether anything has changed or not.
//	if (!pwpwnd->DataAccess()->IsDirty())
//		return false; // eg, they hit CTRL-S when nothing has changed

	return pwpwnd->FileSave();
}

bool WpMainWnd::FileSave()
{
	WpSplitWnd * pwsw = SplitWnd();
	WpChildWnd * pwcw = pwsw->ChildWnd();
	if (!pwcw->CommitSelection())
		return false;
	StrAnsi staFileName = FileName();

	// Strictly speaking the following shouldn't be necessary, because we never storet the HTM file name,
	// but just in case.
	bool fHtml = false;
	if (staFileName.Length())
	{
		int ichDot = staFileName.ReverseFindCh('.');
		if (ichDot > -1)
		{
			StrAnsi staExt(staFileName.Chars() + ichDot, staFileName.Length() - ichDot);
			fHtml = (staExt == "htm" || staExt == "html");
		}
	}

	if (staFileName == "" || fHtml)
	{
		return FileSaveAs();
	}
	else
	{
		return pwcw->DataAccess()->SaveToFile(staFileName, FileType(), this);
	}
}

/*----------------------------------------------------------------------------------------------
	Handle the File-Save As command.
----------------------------------------------------------------------------------------------*/
bool WpApp::CmdFileSaveAs(Cmd * pcmd)
{
	WpMainWnd * pwpwnd = dynamic_cast<WpMainWnd *>(m_qafwCur.Ptr());
	AssertPtr(pwpwnd);
	return pwpwnd->FileSaveAs();
}

bool WpMainWnd::FileSaveAs()
{
	StrApp strWp(kstidAppName);
	WpSplitWnd * pwsw = SplitWnd();
	WpChildWnd * pwcw = pwsw->ChildWnd();

	StrApp strFileMinusExt = RemoveStdExt(FileName());
	OPENFILENAME ofn;		// common dialog box structure
	static achar szFile[260] = {0};	// buffer for filename
	memcpy(szFile, strFileMinusExt.Chars(), strFileMinusExt.Length() * isizeof(OLECHAR));

	int fietT = (m_fiet <= 0 || m_fiet > kfietLim) ? kfietXml : m_fiet;
	fietT = (fietT == kfietTemplate) ? kfietXml : fietT;

	Vector<AfExportStyleSheet> vess;
	FindXslTransforms(vess);
	// TEMP
//	if (vss.Size() > 0)
//		RunXslTransform(vss, 0, _T("c:\\Graphite\\WorldPad files\\FromXSLT.wpx"));

	Vector<StrApp> vstrFileType;
	Vector<StrApp> vstrExt;

	vstrFileType.Push(_T("WorldPad XML (*.wpx)"));
	vstrExt.Push(_T("*.wpx"));
	vstrFileType.Push(_T("WorldPad Template (*.wpt)"));
	vstrExt.Push(_T("*.wpt"));
	vstrFileType.Push(_T("Plain Text (*.txt)"));
	vstrExt.Push(_T("*.txt"));
	int iFirstEss = vstrFileType.Size() + 2;	// 0-based, while fiet is 1-based but with an
												// extra option (All) at the beginning
	for (int iess = 0; iess < vess.Size(); iess++)
	{
		vstrFileType.Push(vess[iess].m_strTitle);
		StrApp strExt;
		strExt.Format(_T("*.%s"), vess[iess].m_strOutputExt.Chars());
		vstrExt.Push(strExt);
	}


	achar rgchFilter[1000];
	ZeroMemory(rgchFilter, 1000 * isizeof(achar));
	achar * pch = rgchFilter;
	for (int istr = 0; istr < vstrFileType.Size(); istr++)
	{
		int cchLeft = rgchFilter + 1000 - pch;
		if (vstrFileType[istr].Length() + vstrExt[istr].Length() + 2 > cchLeft)
			break;

		memcpy(pch, vstrFileType[istr].Chars(), isizeof(achar) * (vstrFileType[istr].Length()+1));
		pch += vstrFileType[istr].Length() + 1;
		memcpy(pch, vstrExt[istr].Chars(),isizeof(achar) * (vstrExt[istr].Length()+1));
		pch += vstrExt[istr].Length() + 1;
	}

LGetName:
	// Initialize OPENFILENAME
	ZeroMemory(&ofn, sizeof(OPENFILENAME));
	// the constant below is required for backward compatibility with Windows 95/98
	ofn.lStructSize = OPENFILENAME_SIZE_VERSION_400;
	ofn.hwndOwner = Hwnd();
	ofn.lpstrFile = szFile;
	ofn.nMaxFile = sizeof(szFile);
	// line below must match file-types enumeration:
	ofn.lpstrFilter = rgchFilter;
//		_T("WorldPad XML (*.wpx)\0*.wpx\0WorldPad Template (*.wpt)\0*.wpt\0")
//		_T("Plain Text (*.txt)\0*.TXT\0");
	ofn.nFilterIndex = std::min(3, fietT - 1); // -1 to skip "All" option which is not offered
	ofn.lpstrFileTitle = NULL;	// dialog title
	ofn.nMaxFileTitle = 0;
	ofn.lpstrInitialDir = NULL;
	ofn.Flags = OFN_PATHMUSTEXIST | OFN_OVERWRITEPROMPT;

	//	Turn off any special keyboards they were using for their data.
	//	Review: this feels like a kludge. Is there a better way?
	ActivateDefaultKeyboard();

	// Display the Open dialog box.
	bool f = GetSaveFileName(&ofn);

	RestoreCurrentKeyboard();

	if (!f)
		return false;

	int fiet = ofn.nFilterIndex + 1; // + 1 to skip "All" option which was not offered

	StrApp strFileName(szFile);
	bool fXslt = (fiet >= iFirstEss);
	int iessSel;

	if (fiet >= kfietPlain && !fXslt)
	{
		WpSavePlainTextDlgPtr qdlg;
		qdlg.Create();
		if (qdlg->DoModal(m_hwnd) == kctidOk)
		{
			int i = qdlg->SelectedEncoding();	// UTF-16, UTF-8, or ANSI
			fiet += i;
		}
		else
			return false; // cancel
	}

	if (!pwcw->CommitSelection())
	{
		Assert(false);
		StrApp strMsg(kstidCouldntCommit);
		int nRet = ::MessageBox(m_hwnd, strMsg.Chars(), strWp.Chars(),
			MB_ICONEXCLAMATION | MB_OKCANCEL);
		if (nRet == IDCANCEL)
			return false;	// cancel
	}

	bool fChangedName = false;
	if (fXslt)
	{
		iessSel = fiet - iFirstEss;
		//	Add the appropriate file extension, if needed.
		if (strFileName.FindCh('.') == -1)
		{
			strFileName.Format(_T("%s.%s"), strFileName.Chars(), vess[iessSel].m_strOutputExt.Chars());
			fChangedName = true;
		}
	}
	else
	{
		fChangedName = AddFileExt(strFileName, fiet);
	}
	StrAnsi staFileName(strFileName);

	if (fChangedName)
	{
		FILE * f;
		if (!fopen_s(&f, staFileName.Chars(), "rb"))
		{
			// File already exists.
			fclose(f);
			StrUni strbRes(kstidOverwriteFile);
			StrUni strbMsg;
			strbMsg.Format(strbRes, strFileName.Chars());
			int nRet = ::MessageBox(m_hwnd, strbMsg.Chars(), strWp.Chars(),
				MB_ICONEXCLAMATION | MB_YESNO);
			if (nRet != IDYES)
				goto LGetName;
		}
	}

	if (fXslt)
	{
		// XSLT
		bool f = RunXslTransform(vess, iessSel, strFileName);
		if (!f)
			goto LGetName;
		return true; // don't change file name
	}

	int fietExt = this->GuessFileType(strFileName);
	if (fietExt == kfietXml || fietExt == kfietTemplate)
		fiet = fietExt;

	if (!pwcw->DataAccess()->SaveToFile(staFileName, fiet, this))
	{
		fietT = fiet;
		goto LGetName;
	}

	StrApp strTitle;
	SetFileName(staFileName.Chars(), fiet, &strTitle, true);
	::SendMessage(Hwnd(), WM_SETTEXT, 0, (LPARAM)strTitle.Chars());
	pwcw->DataAccess()->ClearUndo();
	SetAutoDefault(false);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Add an appropriate file extension to the file name.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::AddFileExt(StrApp & strFileName, int fiet)
{
	if (strFileName.FindCh('.') > -1)
		return false;	// already an extension

	if (fiet == kfietXml)
		strFileName += _T(".wpx");
	else if (fiet == kfietTemplate)
		strFileName += _T(".wpt");
	else
		strFileName += _T(".txt");

	return true;
}

/*----------------------------------------------------------------------------------------------
	Guess the file type from the extension.
----------------------------------------------------------------------------------------------*/
int WpMainWnd::GuessFileType(StrApp strFileName)
{
	int ichDot = strFileName.ReverseFindCh('.');
	if (ichDot == -1)
		// No extension
		return kfietUnknown;

	StrApp strExt(strFileName.Chars() + ichDot, strFileName.Length() - ichDot);
	if (strExt == _T(".wpt"))
		return kfietTemplate;
	else if (strExt == _T(".wpx"))
		return kfietXml;
	else
		return kfietUnknown;
}

/*----------------------------------------------------------------------------------------------
	Remove the standard extensions (.wpx, .wpt, .txt) from the file name for display in the
	dialog.
----------------------------------------------------------------------------------------------*/
StrApp WpMainWnd::RemoveStdExt(StrApp strFileName)
{
	int ichDot = strFileName.FindCh('.');
	if (ichDot == -1)
		return strFileName;	// no extension

	StrApp strExt(strFileName.Chars() + ichDot, strFileName.Length() - ichDot);
	if (strExt == _T(".wpx") || strExt == _T(".wpt") || strExt == _T(".txt"))
	{
		StrApp strRet(strFileName.Chars(), ichDot);
		return strRet;
	}
	else
		return strFileName;
}

/*----------------------------------------------------------------------------------------------
	Clean up the application.
----------------------------------------------------------------------------------------------*/
void WpApp::CleanUp(void)
{
	// Todo DarrelZ (JohnT): This should probably go into AfApp...
	m_qafwCur.Clear();

	// NOTE: This has to go backwards, because the window will remove itself from this
	// vector when it handles the clse message.
	for (int iwnd = m_vqafw.Size(); --iwnd >= 0; )
		::SendMessage(m_vqafw[iwnd]->Hwnd(), WM_CLOSE, 0, 0);
	AfApp::CleanUp();
}

/*----------------------------------------------------------------------------------------------
	Return an indication of the behavior of some of the special keys (arrows, home, end).
----------------------------------------------------------------------------------------------*/
int WpApp::ComplexKeyBehavior(int chw, VwShiftStatus ss)
{
	if (chw == VK_HOME || chw == VK_END)
	{
		return (int)m_fLogicalHomeEnd;
	}
	else if (chw == VK_LEFT || chw == VK_RIGHT || chw == VK_F7 || chw == VK_F8)
	{
		if (ss == kfssShift)
			return (int)m_fLogicalShiftArrow;
		else
			return (int)m_fLogicalArrow;
	}
	else
		return 0;
}

/*----------------------------------------------------------------------------------------------
	For each writing system that uses Graphite, set the flag that says whether to output
	the transduction log.
----------------------------------------------------------------------------------------------*/
void WpApp::SetGraphiteLoggingForAllEnc(bool f)
{
	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
	int cws;
	CheckHr(qwsf->get_NumberOfWs(&cws));
	int * prgenc = NewObj int[cws];
	CheckHr(qwsf->GetWritingSystems(prgenc, cws));
	for (int iws = 0; iws < cws; ++iws)
	{
		IWritingSystemPtr qws;
		CheckHr(qwsf->get_EngineOrNull(prgenc[iws], &qws));
		if (!qws)
			continue;
		CheckHr(qws->SetTracing((int)f));
	}

	delete[] prgenc;
}

/*----------------------------------------------------------------------------------------------
	Delete all the encodings from the registry.
----------------------------------------------------------------------------------------------*/
void WpApp::DeleteAllEncodings()
{
	WpDa::DeleteRegWsKey();
}

/*----------------------------------------------------------------------------------------------
	When a fundamental change has happened to one of the styles, we need to completely
	reconstruct the contents of the window.
----------------------------------------------------------------------------------------------*/
bool WpApp::OnStyleNameChange(IVwStylesheet * psts, ISilDataAccess * psda)
{
	WpMainWndPtr qwpwnd = dynamic_cast<WpMainWnd *>(GetCurMainWnd());
	Assert(qwpwnd);
	qwpwnd->SplitWnd()->ChildWnd()->ReconstructAndReplaceSel();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Open a new main window on the specified data.
	Temporarily, fail if we have a window open and it is a different database. Later, we
	will be able to handle multiple databases.
----------------------------------------------------------------------------------------------*/
void WpApp::NewMainWnd(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
	int hvoMainObj, int encUi, int nTool, int nParam, DWORD dwRegister)
{
	WndCreateStruct wcs;
	wcs.InitMain(_T("WpMainWnd"));

	WpMainWndPtr qwnd;
	qwnd.Create();

	if (m_fSplashNeeded && !m_qSplashScreenWnd.Ptr())
	{
		// Display the splash screen. Note that this is before an 'initial message' is
		// available, but at least we will get the screen up relatively early.
		m_qSplashScreenWnd.CreateInstance("FwCoreDlgs.FwSplashScreen");
		m_qSplashScreenWnd->Show();
		m_qSplashScreenWnd->Refresh();
	}

	// We have a chicken and eggs problem here: at this point, the default factory is empty.
	// But we need the user writing system id before going any further!
	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
	int wsUser = 0;
	StrUni stuWs(kstidUserWs);
	Assert(stuWs.Length());
	IWritingSystemPtr qws;
	CheckHr(qwsf->get_Engine(stuWs.Bstr(), &qws));
	CheckHr(qws->get_WritingSystem(&wsUser));

	qwnd->CreateHwnd(wcs);
	qwnd->SetStatusBarText();
	qwnd->Show(m_nShow);

	StrApp strWp(kstidAppName);
	::SendMessage(qwnd->Hwnd(), WM_SETTEXT, 0, (LPARAM)strWp.Chars());

	// Initialize with the encodings in the Language Definition files (if any), and those in the
	// registry (if any).  Registry storage is automatically upgraded to Language Definition
	// file storage in this step.
	qwnd->DataAccess()->InitWithPersistedWs(qwnd->Hwnd());
	qwnd->UpdateToolBarWrtSysControl();

	WpSplitWnd * pwsw = qwnd->SplitWnd();
	AssertPtr(pwsw);
	WpChildWnd * pwcw = pwsw->ChildWnd();
	AssertPtr(pwcw);

	// ENHANCE SharonC (RandyR): This needs to be upgraded some day to use the
	// m_hmsuvstuCmdLine data member. You will find the filename under the key "filename".
	// It will have the quotes removed already. I would have done this for you,
	// but you use StrAnsi, and the map stores the filename as a StrUni, and I didn't want
	// to do all the work needed to upgrade all the methods you call in this area.
	// I figured I'd make things worse by trying to help here. :-)
	StrAnsi staInitFile(bstrDbName);
	if (staInitFile.Length() == 0)
	{
		if (!FindDefaultTemplate(&staInitFile))
			staInitFile.Clear();
		else
			qwnd->SetAutoDefault(true);
	}

	if (staInitFile.Length())
	{
		// Try to convert to long pathname, since W98 gives us the short one when someone
		// drags a file to the icon. We must use the A version of the routine since we
		// are (probably unwisely, but I (JohnT) did not want to mess with it) using
		// StrAnsi.
		GetLongPathname(staInitFile.Chars(), staInitFile);
		// Load the file.
		int fiet = kfietUnknown;
		int fietGuess = qwnd->GuessFileType(staInitFile);
		int fls = kflsOkay;
		pwcw->DataAccess()->LoadIntoEmpty(staInitFile.Chars(), &fiet, qwnd, pwcw, NULL, &fls);
		if (fls == kflsAborted)
		{
			qwnd->ReplaceWithEmptyWindow();
		}
		else
		{
			StrApp strTitle;
			qwnd->SetFileName(staInitFile.Chars(), fietGuess, &strTitle, false);
			::SendMessage(qwnd->Hwnd(), WM_SETTEXT, 0, (LPARAM)strTitle.Chars());
			if (fls == kflsPartial)
				qwnd->AdjustNameOfPartialFile();

			qwnd->SetStatusBarText();

			qwnd->UpdateToolBarWrtSysControl();		// since there may be new encodings loaded
													// from the file

			pwcw->MakeInitialSel();

			pwcw->DataAccess()->ClearUndo();
		}
	}
	else
	{
		pwcw->DataAccess()->ClearUndo(); // can't undo anything that was done as part of setup
	}

	// Main window is now visible; thus we're now done with splash screen
	if (m_qSplashScreenWnd.Ptr())
	{
		// Close the splash screen
		m_qSplashScreenWnd->Close();
		m_qSplashScreenWnd.Clear();
	}

	Assert(m_dwRegister || dwRegister);
	if (m_dwRegister == 0)
		m_dwRegister = dwRegister;
}


/*----------------------------------------------------------------------------------------------
	Return a pointer to the main application.
----------------------------------------------------------------------------------------------*/
WpApp * WpApp::MainApp()
{
	return &g_app;
}

/***********************************************************************************************
	WpMainWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
WpMainWnd::WpMainWnd()
{
	m_fDirty = false;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
WpMainWnd::~WpMainWnd()
{
}

/*----------------------------------------------------------------------------------------------
	Return the object that manages the data.
----------------------------------------------------------------------------------------------*/
WpDa * WpMainWnd::DataAccess()
{
	return SplitWnd()->DataAccess();
}

/*----------------------------------------------------------------------------------------------
	Load settings specific to this window.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::LoadSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);

	SuperClass::LoadSettings(pszRoot, fRecursive);

	// Get window position.
	LoadWindowPosition(pszRoot, _T("Position"));

	FwSettings * pfws = AfApp::GetSettings();

	// Read the toolbar settings from storage. If the settings aren't there, use
	// default settings.
	DWORD dwToolbarFlags;
	if (!pfws->GetDword(pszRoot, _T("Toolbar Flags"), &dwToolbarFlags))
		dwToolbarFlags = (DWORD)-1; // Show all toolbars by default.
	LoadToolbars(pfws, pszRoot, dwToolbarFlags);
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
void WpMainWnd::LoadDefaultToolbarFlags(Vector<DWORD> & vflag, DWORD & dwBarFlags)
{
	dwBarFlags = 0x1f; // Show all 5 toolbars.
	int cband = m_vqtlbr.Size();
	int rgitlbr[] = { 0, 1, 3, 4, 2 };
	bool rgfBreak[] = { true, true, false, false, true };
	int rgdxpBar[] = { 0x0032, 0x0113, 0x0022, 0x036f, 0x011c };
	for (int iband = 0; iband < cband; iband++)
	{
		vflag[iband] = MAKELPARAM(rgdxpBar[iband], rgitlbr[iband] | (rgfBreak[iband] << 15));
	}
}


/*----------------------------------------------------------------------------------------------
	Save settings specific to this window.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::SaveSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);

	SuperClass::SaveSettings(pszRoot, fRecursive);

	SaveWindowPosition(pszRoot, _T("Position"));

	FwSettings * pfws = AfApp::GetSettings();

	// Store the visibility settings for the view bar and the toolbars.
	SaveToolbars(pfws, pszRoot, _T("Toolbar Flags"));
}

/*----------------------------------------------------------------------------------------------
	The hwnd has been attached.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::PostAttach(void)
{

	SuperClass::PostAttach();

	// Create the main data window and attach it
	bool fScrollHoriz = WpApp::MainApp()->Vertical();
	m_qwsw.Attach(NewObj WpSplitWnd(fScrollHoriz));
	m_qwsw->Init(this);
	WndCreateStruct wcs;
	wcs.InitChild(_T("AfClientWnd"), m_hwnd, 1000);
	wcs.dwExStyle |= WS_EX_CLIENTEDGE;
	wcs.style &= ~WS_CLIPCHILDREN; // allows the gray splitter bar to draw over the child windows.

	m_qwpsts.Attach(NewObj WpStylesheet);
	// Note :we initialize the stylesheet in WpChildWnd::Init, after all the windows and the
	// data access object have been set up but before any file is loaded.

	m_qwsw->CreateHwnd(wcs);
	::ShowWindow(m_qwsw->Hwnd(), SW_SHOW);

	// Create the toolbars.
	const int rgrid[] =
	{
		kridTBarStd,
		kridTBarFmtg,
		kridTBarIns,
		kridTBarWnd,
		kridTBarInvisible,
	};

	GetMenuMgr()->LoadToolBars(rgrid, SizeOfArray(rgrid));
	GetMenuMgr()->LoadAccelTable(kridAccelStd, 0, m_hwnd);

	// Create the menu bar.
	StrAppBuf strbTemp;
	AfMenuBarPtr qmnbr;
	qmnbr.Create();
	strbTemp.Load(kstidMenu);
	qmnbr->Initialize(m_hwnd, kridAppMenu, kridAppMenu, strbTemp.Chars());
	m_vqtlbr.Push(qmnbr.Ptr());

	// Create the toolbars.
	AfToolBarPtr qtlbr;
	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.

	qtlbr.Create();
	strbTemp.Load(kstidTBarStd);
	qtlbr->Initialize(kridTBarStd, kridTBarStd, strbTemp.Chars());
	qtlbr->SetWritingSystemFactory(qwsf);
	m_vqtlbr.Push(qtlbr);

	qtlbr.Create();
	strbTemp.Load(kstidTBarFmtg);
	qtlbr->Initialize(kridTBarFmtg, kridTBarFmtg, strbTemp.Chars());
	qtlbr->SetWritingSystemFactory(qwsf);
	m_vqtlbr.Push(qtlbr);

	qtlbr.Create();
	strbTemp.Load(kstidTBarIns);
	qtlbr->Initialize(kridTBarIns, kridTBarIns, strbTemp.Chars());
	qtlbr->SetWritingSystemFactory(qwsf);
	m_vqtlbr.Push(qtlbr);

	qtlbr.Create();
	strbTemp.Load(kstidTBarWnd);
	qtlbr->Initialize(kridTBarWnd, kridTBarWnd, strbTemp.Chars());
	qtlbr->SetWritingSystemFactory(qwsf);
	m_vqtlbr.Push(qtlbr);

	// Load window settings.
	LoadSettings(NULL, false);

	g_app.AddCmdHandler(this, 1);

	// Update the icons for the formatting toolbar drop-down buttons.
	UpdateToolBarIcon(kridTBarFmtg, kcidFmttbApplyBgrndColor, m_clrBack);
	UpdateToolBarIcon(kridTBarFmtg, kcidFmttbApplyFgrndColor, m_clrFore);
	UpdateToolBarIcon(kridTBarFmtg, kcidFmttbApplyBdr, (COLORREF) m_bpBorderPos);

	// Set the status bar to a safe default state, and let it know the user interface writing
	// system id.
	m_qstbr->RestoreStatusText();
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));
	m_qstbr->SetUserWs(wsUser);
}

/*----------------------------------------------------------------------------------------------
	The window has been moved or resized.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::OnSize(int wst, int dxp, int dyp)
{
	::MoveWindow(m_qwsw->Hwnd(), 0, 0, dxp, dyp, false);
	return SuperClass::OnSize(wst, dxp, dyp);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void WpMainWnd::OnToolBarButtonAdded(AfToolBar * ptlbr, int ibtn, int cid)
{
	AssertPtr(ptlbr);

	switch (cid)
	{
	case kcidFmttbStyle:
		{
			//------------------------------------------------------------------------------
			// The style control.
			AfToolBarComboPtr qtbc;
			ptlbr->SetupComboControl(&qtbc, cid, 100, 200, true);
			int width = 190;
			::SendMessage(qtbc->GetComboControl(), CB_SETDROPPEDWIDTH , (WPARAM)width, 0);

			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);

			FW_COMBOBOXEXITEM fcbi;
			memset(&fcbi, 0, isizeof(fcbi));
			fcbi.mask = CBEIF_TEXT;
			fcbi.iIndent = 0;
		}
		return;

	case kcidFmttbWrtgSys:
		{
			//------------------------------------------------------------------------------
			// The old writing system control.
			AfToolBarComboPtr qtbc;
			ptlbr->SetupComboControl(&qtbc, cid, 80, 200, true);
			int width = 100;
			::SendMessage(qtbc->GetComboControl(), CB_SETDROPPEDWIDTH , (WPARAM)width, 0);

			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);

			FW_COMBOBOXEXITEM fcbi;
			memset(&fcbi, 0, isizeof(fcbi));
			fcbi.mask = CBEIF_TEXT;
			fcbi.iIndent = 0;

			// Get the memory-based factory.
			ILgWritingSystemFactoryPtr qwsf;
			qwsf.CreateInstance(CLSID_LgWritingSystemFactory);
			int cws;
			CheckHr(qwsf->get_NumberOfWs(&cws));
			int * prgenc = NewObj int[cws];
			CheckHr(qwsf->GetWritingSystems(prgenc, cws));
			for (int iws = 0; iws < cws; iws++)
			{
				if (prgenc[iws] == 0)
					continue;

				StrApp str;
				SplitWnd()->ChildWnd()->UiNameOfWs(prgenc[iws], &str);
				StrUni stu(str);
				qtsf->MakeStringRgch(stu.Chars(), stu.Length(), UserWs(), &fcbi.qtss);
				fcbi.iItem = iws;
				qtbc->InsertItem(&fcbi);
			}
			qtbc->SetCurSel(0);
			delete[] prgenc;
		}
		return;

	case kcidFmttbFnt:
		{
			//------------------------------------------------------------------------------
			// The font family control.
			AfToolBarComboPtr qtbc;
			ptlbr->SetupComboControl(&qtbc, cid, 120, 200, true);
			int width = 190;
			::SendMessage(qtbc->GetComboControl(), CB_SETDROPPEDWIDTH , (WPARAM)width, 0);

			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);

			FW_COMBOBOXEXITEM fcbi;
			memset(&fcbi, 0, isizeof(fcbi));
			fcbi.mask = CBEIF_TEXT;
			fcbi.iIndent = 0;

			// Add the predefined names to the combo box.
			Vector<StrUni> vstuDefaults;
			FwStyledText::FontUiStrings(true, vstuDefaults);
			int ifnt;
			for (ifnt = 0; ifnt < vstuDefaults.Size(); ifnt++)
			{
				qtsf->MakeStringRgch(vstuDefaults[ifnt], vstuDefaults[ifnt].Length(),
					UserWs(), &fcbi.qtss);
				fcbi.iItem = ifnt;
				qtbc->InsertItem(&fcbi);
			}
			// Get the currently available fonts via the LgFontManager.
			ILgFontManagerPtr qfm;
			SmartBstr bstrNames;
			qfm.CreateInstance(CLSID_LgFontManager);
			HRESULT hr = qfm->AvailableFonts(&bstrNames);
			if (SUCCEEDED(hr))
			{
				StrUni stuNameList;
				// Convert BSTR to StrUni.
				stuNameList.Assign(bstrNames.Bstr(), BstrLen(bstrNames.Bstr()));
				int cchLength = stuNameList.Length();
				int ichMin = 0; // Index of the beginning of a font name.
				int ichLim = 0; // Index that is one past the end of a font name.
				// Add each font name to the combo box.
				while (ichLim < cchLength)
				{
					ichLim = stuNameList.FindCh(L',', ichMin);
					if (ichLim == -1) // i.e., if not found.
					{
						ichLim = cchLength;
					}
					qtsf->MakeStringRgch(stuNameList.Chars() + ichMin, ichLim - ichMin,
						UserWs(), &fcbi.qtss);
					fcbi.iItem = ifnt++;
					qtbc->InsertItem(&fcbi);
					ichMin = ichLim + 1;
				}
			}
			StrApp str(kstidDefaultSerif);
			::SetWindowText(qtbc->Hwnd(), str.Chars());
		}
		return;

	case kcidFmttbFntSize:
		{
			//------------------------------------------------------------------------------
			// The font size control.
			AfToolBarComboPtr qtbc;
			ptlbr->SetupComboControl(&qtbc, cid, 40, 200, false);

			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);

			FW_COMBOBOXEXITEM fcbi;
			memset(&fcbi, 0, isizeof(fcbi));
			fcbi.mask = CBEIF_TEXT;
			fcbi.iIndent = 0;

			const wchar * krgszSizes[] = {
				L"8", L"9", L"10", L"11", L"12", L"14", L"16", L"18", L"20", L"22", L"24",
				L"26", L"28", L"36", L"48", L"72"
			};
			const int kcSizes = isizeof(krgszSizes) / isizeof(wchar *);
			for (int isiz = 0; isiz < kcSizes; isiz++)
			{
				qtsf->MakeStringRgch(krgszSizes[isiz], StrLen(krgszSizes[isiz]),
					UserWs(), &fcbi.qtss);
				fcbi.iItem = isiz;
				qtbc->InsertItem(&fcbi);
			}
			qtbc->SetCurSel(4);
		}
		return;
	case kcidEditUndo:
	case kcidEditRedo:
		// Don't add the drop-down buttons to these items.
		return;
	}

	SuperClass::OnToolBarButtonAdded(ptlbr, ibtn, cid);
}


/*----------------------------------------------------------------------------------------------
	Reposition our client window.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::OnClientSize(void)
{
	SuperClass::OnClientSize();

	Rect rc;
	SuperClass::GetClientRect(rc);

	if (m_qwsw)
		::MoveWindow(m_qwsw->Hwnd(), rc.left, rc.top, rc.Width(), rc.Height(), true);

	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(!lnRet);

	if (wm == WM_SETFOCUS && m_qwsw && m_qwsw->Hwnd())
		SetFocus(m_qwsw->Hwnd());

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	The window is closing. Write settings out to the registry, and give them a chance to
	save the changes to their data.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::OnClose()
{
	// Review SharonC (JohnT): is this where we need to check about saving the document?

	WpSplitWnd * pwsw = SplitWnd();
	WpChildWnd * pwcw = pwsw->ChildWnd();
	pwcw->CommitSelection();

	if (!OfferToSave())
		return true;	// cancelled; don't run the default routine that closes the window

	//	Put the current list of encodings into the registry.
	DataAccess()->PersistAllWs(m_hwnd);

	return SuperClass::OnClose();
}

/*----------------------------------------------------------------------------------------------
	If the document has been modified, ask if they want to save.
	Return false if they decided to cancel the operation.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::OfferToSave()
{
	StrApp strWp(kstidAppName);
	if (DataAccess()->IsDirty() || m_fDirty)
	{
		StrApp strRes;
		StrApp strMsg;
		StrApp strFileName;
		if (FileName().Length())
		{
			strRes.Load(kstidSaveChanges);
			strFileName = FileName(); // convert to Unicode
			strMsg.Format(strRes, strFileName.Chars());
		}
		else
		{
			strMsg.Load(kstidSaveChangesNoName);
		}
		int nRet = ::MessageBox(m_hwnd, strMsg.Chars(), strWp.Chars(),
			MB_YESNOCANCEL | MB_ICONEXCLAMATION);

		if (nRet == IDYES)
		{
			if (FileSave()) // false if they cancelled the save
			{
				m_fDirty = false;
				return true;
			}
			else
				return false;
		}
		else if (nRet == IDCANCEL)
		{
			return false;
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	As it finally goes away, make doubly sure all pointers get cleared. This helps break cycles.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::OnReleasePtr()
{
	// By contract we must clear all our own smart pointers.
	m_qwsw.Clear();
	// To prevent spurious memory leaks shut down the writing system factory, releasing
	// cached encodings, old writing systems, etc. But only if we are the last window!
	if (AfApp::Papp()->GetMainWndCount() == 0)
	{
		ILgWritingSystemFactoryBuilderPtr qwsfb;
		qwsfb.CreateInstance(CLSID_LgWritingSystemFactoryBuilder);
		AssertPtr(qwsfb);
		CheckHr(qwsfb->ShutdownAllFactories());
		((WpApp *)(AfApp::Papp()))->ClearWsFactory();
	}
	SuperClass::OnReleasePtr();
}

/*----------------------------------------------------------------------------------------------
	Handle notification messages for the main window.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	if (SuperClass::OnNotifyChild(ctid, pnmh, lnRet))
		return true;

	if (pnmh->code == CBN_DROPDOWN && ctid == kcidFmttbStyle)
	{
		return OnStyleDropDown(pnmh->hwndFrom);
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle the File-Close menu option
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdWndClose(Cmd * pcmd)
{
	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	bool fLastWindow = (vqafw.Size() == 1);

	// This is kind of convoluted. If needed, we open any new window before closing the last
	// one, so we don't end up leaving the program. But we ask about saving first, so we
	// don't interrupt things between opening the new window and closing this one.
	if (!OfferToSave())
		return true; // cancelled

	DataAccess()->ClearChanges();	// don't ask about saving again

	if (fLastWindow)
		CmdWndNew(NULL);

	::SendMessage(m_hwnd, WM_CLOSE, 0, 0);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Enable the File-Close menu option
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmsWndClose(CmdState & cms)
{
	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	bool fLastWindow = (vqafw.Size() == 1);

	cms.Enable(!DataAccess()->IsEmpty() || !fLastWindow);
	return true;
}

/*----------------------------------------------------------------------------------------------
	If the current client window is already split, unsplit it. Otherwise split it in half
	horizontally.

	@param pcmd menu command

	@return true
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdWndSplit(Cmd * pcmd)
{
	AssertPtr(pcmd);
	WpSplitWndPtr qwpsplf = dynamic_cast<WpSplitWnd *>(SplitWnd());
	Assert(qwpsplf);
	if (qwpsplf->GetPane(1) == NULL)
	{
		qwpsplf->SplitWindow(-1);
		// Store a pointer to this main window in both of the child panes.
		WpChildWndPtr qwpw = dynamic_cast<WpChildWnd *>(qwpsplf->GetPane(1));
		if (qwpw)
		{
			qwpw->SetMainWnd(this);
		}
	}
	else
	{
		qwpsplf->UnsplitWindow(true);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Set the state of the split window toolbar/menu item. It should be checked when the window
	is split.

	@param cms menu command state

	@return true
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmsWndSplit(CmdState & cms)
{
	if (SplitWnd())
		cms.SetCheck(SplitWnd()->GetPane(1));

	return true;
}

/*----------------------------------------------------------------------------------------------
	Bring up the Page Setup dialog, run it, set m_fDirty flag (if needed).
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdFilePageSetup(Cmd * pcmd)

{
	AssertObj(pcmd);

	FilPgSetDlgPtr qfpsd;
	qfpsd.Create();

	ITsStringPtr qtssTitle;

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	qtsf->MakeStringRgch(m_stuHeaderDefault.Chars(), m_stuHeaderDefault.Length(), UserWs(),
		&qtssTitle);

	qfpsd->SetDialogValues(m_dxmpLeftMargin, m_dxmpRightMargin, m_dympTopMargin,
		m_dympBottomMargin, m_dympHeaderMargin, m_dympFooterMargin, m_nOrient,
		m_qtssHeader, m_qtssFooter, m_fHeaderOnFirstPage, m_dympPageHeight,
		m_dxmpPageWidth, m_sPgSize,
		AfApp::Papp()->GetMsrSys(), qtssTitle);

	// Run the dialog.
	if (qfpsd->DoModal(Hwnd()) == kctidOk)
	{
		// Get the output values.
		qfpsd->GetDialogValues(&m_dxmpLeftMargin, &m_dxmpRightMargin, &m_dympTopMargin,
			&m_dympBottomMargin, &m_dympHeaderMargin, &m_dympFooterMargin, &m_nOrient,
			&m_qtssHeader, &m_qtssFooter, &m_fHeaderOnFirstPage, &m_dympPageHeight,
			&m_dxmpPageWidth, &m_sPgSize);
		m_fDirty = true;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	This is called when any menu item is expanded, and allows the app to modify the menu. We
	use it here to see if the Split Window menu item needs to be replaced with Remove Split.

	@param hmenu Handle to the menu that is being expanded right now.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::FixMenu(HMENU hmenu)
{
	int cmni = ::GetMenuItemCount(hmenu);
	int imni;

	for (imni = cmni - 1; imni > -1; imni--)
	{
		UINT nmId = GetMenuItemID(hmenu, imni);
		if (nmId == kcidWndSplit)
		{
			// Check whether the window is already split or not:
			if (SplitWnd()->GetPane(1))
			{
				// Make this menu item say "Remove split":
				StrApp str(kcidWndSplitOff);
				::ModifyMenu(hmenu, kcidWndSplit, MF_BYCOMMAND | MF_STRING, kcidWndSplit,
					str.Chars());
			}
			else
			{
				// Make this menu item say "Split Window":
				StrApp str(kcidWndSplitOn);
				::ModifyMenu(hmenu, kcidWndSplit, MF_BYCOMMAND | MF_STRING, kcidWndSplit,
					str.Chars());
			}
		}
	}
	SuperClass::FixMenu(hmenu);
}

/*----------------------------------------------------------------------------------------------
	Open the file with the given name, and load it either into this window (if it is empty)
	or a newly-created one.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::DoOpenFile(char * szFile, int fiet)
{
	WpChildWnd * pwcw = SplitWnd()->ChildWnd();
	pwcw->CommitSelection();

	bool fEmpty = (pwcw->DataAccess()->IsEmpty() && FileName() == "");
	if (fEmpty)
	{
		// Load into this window.
		ITsTextPropsPtr qttp;
		pwcw->GetPropsOfSelection(&qttp);
		if (fiet >= kfietPlain)
			fiet = kfietUnknown;	// user has not distinguished between UTF-16, UTF-8, & ANSI
		int fls = kflsOkay;
		pwcw->DataAccess()->LoadIntoEmpty(szFile, &fiet, this, pwcw, qttp, &fls);
		if (fls == kflsAborted)
		{
			ReplaceWithEmptyWindow();
			return false;
		}
		if (!pwcw->DataAccess()->IsEmpty())
		{
			StrApp strTitle;
			SetFileName(szFile, fiet, &strTitle, false);
			::SendMessage(Hwnd(), WM_SETTEXT, 0, (LPARAM)strTitle.Chars());
			if (fls == kflsPartial)
				AdjustNameOfPartialFile();
		}
		SetStatusBarText();

		UpdateToolBarWrtSysControl();	// since there may be new encodings loaded
										// from the file

		pwcw->MakeInitialSel();

		return true;
	}
	else
	{
		return NewWindow(szFile, fiet);
	}
}

/*----------------------------------------------------------------------------------------------
	Close the current window and open another that is completely empty. This happens
	when there is some kind of fatal error in loading into an empty window.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::ReplaceWithEmptyWindow()
{
	CmdWndNew(NULL);

	// Move the new window to where this one is.
	// NOTE: this code is not working!
	WINDOWPLACEMENT wp = { isizeof(wp) };
	::GetWindowPlacement(m_hwnd, &wp);
	RECT rc = wp.rcNormalPosition;
	HWND hwndNew = ::GetNextWindow(m_hwnd, GW_HWNDPREV);

//	HDWP hdwp = ::BeginDeferWindowPos(1);
//	if (hdwp)
//	{
//		hdwp = ::DeferWindowPos(hdwp, hwndNew, NULL, rc.left, rc.top, rc.Width(),
//			rc.Height(), SWP_NOACTIVATE);
//		::EndDeferWindowPos(hdwp);
//	}

	::MoveWindow(hwndNew, rc.left, rc.top, (rc.right - rc.left), (rc.bottom - rc.top), true);

	DataAccess()->ClearChanges();
	::SendMessage(m_hwnd, WM_CLOSE, 0, 0);
}

/*----------------------------------------------------------------------------------------------
	Bring up the Options dialog.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdToolsOpts(Cmd * pcmd)
{
	WpApp * pwpapp = dynamic_cast<WpApp *>(AfApp::Papp());
	Assert(pwpapp);

	WpOptionsDlgPtr qdlg;
	qdlg.Create();
	if (qdlg->DoModal(m_hwnd) == kctidOk)
	{
		qdlg->ModifyAppFlags(pwpapp);
		pwpapp->SetGraphiteLoggingForAllEnc(pwpapp->GraphiteLogging());
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Bring up the Writing System Properties dialog.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdToolsWritingSysProps(Cmd * pcmd)
{
	WpWrSysDlgPtr qdlg;
	qdlg.Create();

	WpApp * pwpapp = dynamic_cast<WpApp *>(AfApp::Papp());
	Assert(pwpapp);
	int ws;
	SplitWnd()->ChildWnd()->GetWsOfSelection(&ws);
	qdlg->Init(ws, pwpapp->GraphiteLogging());

	if (qdlg->DoModal(m_hwnd) == kctidOk)
	{
		qdlg->ModifyEncodings(DataAccess());
		if (qdlg->RenderingChanged())
		{
			OnStylesheetChange();
		}

		Vector<AfMainWndPtr> vqafw = AfApp::Papp()->GetMainWindows();
		for (int iwnd = 0; iwnd < vqafw.Size(); iwnd++)
		{
			WpMainWnd * pwpwndTmp = dynamic_cast<WpMainWnd *>(vqafw[iwnd].Ptr());
			if (pwpwndTmp)
				pwpwndTmp->UpdateToolBarWrtSysControl();
		}
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Reactivate the default keyboard.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::ActivateDefaultKeyboard()
{
	// For comparison, we want only the LANGID portion of the HKL.
	HKL hklCurr = reinterpret_cast<HKL>(LANGIDFROMLCID(::GetKeyboardLayout(0)));
	LCID lcidDefault = AfApp::GetDefaultKeyboard();
	// For keyboard selection, we want only the LANGID portion of the LCID.
	HKL hklDefault = reinterpret_cast<HKL>(LANGIDFROMLCID(lcidDefault));
	if (hklCurr != hklDefault)
	{
#if 99
		StrAnsi sta;
		sta.Format("WpMainWnd::ActivateDefaultKeyboard() -"
			" ::ActivateKeyboardLayout(%x, KLF_SETFORPROCESS);\n",
			hklDefault);
		::OutputDebugStringA(sta.Chars());
#endif
		::ActivateKeyboardLayout(hklDefault, KLF_SETFORPROCESS);
	}
}

/*----------------------------------------------------------------------------------------------
	Restore the keyboard that is needed for the current selection.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::RestoreCurrentKeyboard()
{
	WpSplitWnd * pwsw = SplitWnd();
	WpChildWnd * pwcw = pwsw->ChildWnd();
	pwcw->RestoreCurrentKeyboard();
}

void WpChildWnd::RestoreCurrentKeyboard()
{
	if (!m_qrootb)
		return;
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
		return;
	HandleSelectionChange(qvwsel);
}

/*----------------------------------------------------------------------------------------------
	Bring up another top-level window with an empty document.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdWndNew(Cmd * pcmd)
{
	StrAnsi staDefaultTemplate;
	if (WpApp::FindDefaultTemplate(&staDefaultTemplate))
		return NewWindow(staDefaultTemplate.Chars(), kfietTemplate);
	else
		return NewWindow("", kfietXml);
}

/*----------------------------------------------------------------------------------------------
	Enter What's This help mode.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::CmdHelpMode(Cmd * pcmd)
{
	ToggleHelpMode();
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the Help-About command.
----------------------------------------------------------------------------------------------*/
bool WpApp::CmdHelpAbout(Cmd * pcmd)
{
	AssertObj(pcmd);
	ShowHelpAbout();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Bring up another top-level window, initialized from the given file, or empty.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::NewWindow(const char * szFileName, int fiet)
{
	SaveSettings(NULL);

	WndCreateStruct wcs;
	wcs.InitMain(_T("WpMainWnd"));

	WpMainWndPtr qwnd;
	qwnd.Create();
	StrApp strTitle;
	qwnd->SetFileName(szFileName, fiet, &strTitle, false);
	qwnd->SetLauncherWindow(this);
	// This is kind of a kludge. If we are loading a template, set the file name so it will
	// be loaded when we open then window, and delete it below, so that won't be the official
	// name of the document.
	if (fiet == kfietTemplate)
		qwnd->TempSetFileName(szFileName);
	qwnd->CreateHwnd(wcs);
	::SendMessage(qwnd->Hwnd(), WM_SETTEXT, 0, (LPARAM)strTitle.Chars());
	qwnd->SetStatusBarText();

	Rect rc;
	::GetWindowRect(m_hwnd, &rc);
	int dypCaption = ::GetSystemMetrics(SM_CYCAPTION) + ::GetSystemMetrics(SM_CYSIZEFRAME);
	rc.Offset(dypCaption, dypCaption);
	AfGfx::EnsureVisibleRect(rc);
	::MoveWindow(qwnd->Hwnd(), rc.left, rc.top, rc.Width(), rc.Height(), true);

//	Frame windows do this automatically.	AfApp::Papp()->AddWindow(qwnd);

	// Clear this, since it is only used for the file-load process:
	qwnd->SetLauncherWindow(NULL);

	int fls = qwnd->DataAccess()->FileLoadStatus();
	if (fls == kflsAborted)
	{
		// Close the window.
		::SendMessage(qwnd->Hwnd(), WM_CLOSE, 0, 0);
		return true;
	}
	else if (fls == kflsPartial)
	{
		qwnd->AdjustNameOfPartialFile();
	}

	qwnd->Show(SW_SHOW);
	qwnd->UpdateToolBarWrtSysControl();

	if (fiet == kfietTemplate)
		qwnd->TempSetFileName("");

	return true;
}

/*----------------------------------------------------------------------------------------------
	Update the items in the writing system control due to the fact that changes have been
	made to the list of writing systems.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::UpdateToolBarWrtSysControl()
{
	AfToolBarPtr qtlbrFmt = GetToolBar(kridTBarFmtg);
	HWND hwndToolBar = qtlbrFmt->Hwnd();
	HWND hwndWrtgSys = ::GetDlgItem(hwndToolBar, kcidFmttbWrtgSys);

	//	Clear the old items from the combo box.
	::SendMessage(hwndWrtgSys, CB_RESETCONTENT, 0, 0);

	//	Make a sorted list of writing systems.
	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
	int cws;
	CheckHr(qwsf->get_NumberOfWs(&cws));
	int * prgenc = NewObj int[cws];
	CheckHr(qwsf->GetWritingSystems(prgenc, cws));
	Vector<StrAnsi> vstaWss;
	int iws;
	for (iws = 0; iws < cws; ++iws)
	{
		if (prgenc[iws] == 0)
			continue;

		StrApp str;
		SplitWnd()->ChildWnd()->UiNameOfWs(prgenc[iws], &str);
		StrAnsi sta(str);
		int iIns;
		for (iIns = 0; iIns < vstaWss.Size(); iIns++)
		{
			if (vstaWss[iIns] > sta)
				break;
		}
		vstaWss.Insert(iIns, sta);
	}
	Assert(vstaWss.Size() == cws || vstaWss.Size() == cws - 1);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	for (iws = 0; iws < vstaWss.Size(); ++iws)
	{
		StrUni stu(vstaWss[iws]);
		ITsStringPtr qtss;
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), UserWs(), &qtss);
		::SendMessage(hwndWrtgSys, FW_CB_ADDSTRING, 0, (LPARAM)qtss.Ptr());
	}

	delete[] prgenc;
}

/*----------------------------------------------------------------------------------------------
	Update the list of fonts in the font control to be those appropriate to the current
	writing system.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::UpdateFontControlForWs()
{
	Vector<StrApp> vstrFonts;
	SplitWnd()->ChildWnd()->GetCurrentFontChoices(vstrFonts);

	AfToolBarPtr qtlbrFmt = GetToolBar(kridTBarFmtg);
	HWND hwndToolBar = qtlbrFmt->Hwnd();
	HWND hwndFonts = ::GetDlgItem(hwndToolBar, kcidFmttbFnt);

	//	Clear the old items from the combo box.
	::SendMessage(hwndFonts, CB_RESETCONTENT, 0, 0);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	for (int i = 0; i < vstrFonts.Size(); ++i)
	{
		StrUni stu(vstrFonts[i]);
		ITsStringPtr qtss;
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), UserWs(), &qtss);
		::SendMessage(hwndFonts, FW_CB_ADDSTRING, 0, (LPARAM)qtss.Ptr());
	}
}

/*----------------------------------------------------------------------------------------------
	Store the name of the current file, return the name of the window to use in the title bar.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::SetFileName(const char * pszFile, int fiet, StrApp * pstrTitle, bool fSaving)
{
	AssertPtr(pstrTitle);
	AssertPszN(pszFile);

	if (fSaving || fiet != kfietTemplate)
		m_staFileName = pszFile;

	if (fiet != -1) // if -1, don't change it
		m_fiet = fiet;

	if (fiet == kfietTemplate && !fSaving)
		m_fiet = kfietXml;

	if (m_staFileName.Length() == 0 || (!fSaving && fiet == kfietTemplate))
	{
		pstrTitle->Load(kstidAppName);
		return;
	}

	//	Take off the path.
	const char * pszName = strrchr(pszFile, '\\');
	if (pszName)
		++pszName;
	else
		pszName = pszFile;
	StrApp strRes(kstidWindowLabel);
	StrApp strName(pszName);
	pstrTitle->Format(strRes.Chars(), strName.Chars());
	m_stuHeaderDefault = strName;

	//	If there is no page header, store the file title there.
	if (!m_qtssHeader)
	{
		StrUni stuHeader(strName);
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		qtsf->MakeStringRgch(stuHeader.Chars(), stuHeader.Length(), UserWs(), &m_qtssHeader);
	}
}

/*----------------------------------------------------------------------------------------------
	When a file was loaded with errors, adjust the name of the file to be something like
	"MyFile-PartiallyRecovered.wpx".

	Assumes there is a valid file name in m_staFileName.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::AdjustNameOfPartialFile()
{
	// Find the extension.
	int ichExt = m_staFileName.Length() - 1;
	while (ichExt >= 0 && m_staFileName[ichExt] != '.')
		ichExt--;

	// Generate the new name.
	StrAnsi staNew;
	if (ichExt < 0)
		staNew = m_staFileName;
	else
		staNew = m_staFileName.Left(ichExt);
	staNew += "-PartiallyRecovered";
	if (ichExt >= 0)
		staNew += m_staFileName.Right(m_staFileName.Length() - ichExt);

	// Take off path for showing in title bar.
	int ich = staNew.Length() - 1;
	while (ich >= 0 && staNew[ich] != '\\')
		ich--;
	ich++;
	StrApp strRes(kstidWindowLabel);
	StrApp strLabel;
	StrApp strNew = staNew.Right(staNew.Length() - ich);
	strLabel.Format(strRes, strNew.Chars());

	// Update the title bar.
	m_staFileName = staNew;
	::SendMessage(Hwnd(), WM_SETTEXT, 0, (LPARAM)strLabel.Chars());
}

/*----------------------------------------------------------------------------------------------
	Rename or delete all occurrences of the given styles in the document.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::RenameAndDeleteStyles(Vector<StrUni> & vstuOldNames,
	Vector<StrUni> & vstuNewNames, Vector<StrUni> & vstuDelNames)
{
	SplitWnd()->ChildWnd()->DataAccess()->RenameAndDeleteStyles(
		vstuOldNames, vstuNewNames, vstuDelNames);
}

/*----------------------------------------------------------------------------------------------
	Return the object that manages the language encodings.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::GetLgWritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
#if 3
	AssertPtr(ppwsf);
	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
	*ppwsf = qwsf.Detach();
#else
	WpChildWnd * wcw = m_qwsw->ChildWnd();
	if (!wcw)
		return;
	WpDa * pda = wcw->DataAccess();
	if (pda)
		CheckHr(pda->get_WritingSystemFactory(ppwsf));
#endif
}

/*----------------------------------------------------------------------------------------------
	Return the user interface writing system id.
----------------------------------------------------------------------------------------------*/
int WpMainWnd::UserWs()
{
	if (!m_wsUser)
	{
		ILgWritingSystemFactoryPtr qwsf;
		qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
		CheckHr(qwsf->get_UserWs(&m_wsUser));
	}
	return m_wsUser;
}

/***********************************************************************************************
	WpSplitWnd stuff.
***********************************************************************************************/

WpSplitWnd::WpSplitWnd(bool fScrollHoriz) : SuperClass(fScrollHoriz)
{
}

void WpSplitWnd::OnReleasePtr()
{
}

void WpSplitWnd::CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew)
{
	WpChildWndPtr qwcw;
	if (WpApp::MainApp()->Vertical())
		qwcw.Attach(NewObj WpChildWnd(true, true));
	else
		qwcw.Attach(NewObj WpChildWnd);
	WndCreateStruct wcs;

	wcs.InitChild(_T("AfVwWnd"), m_hwnd, 0);
	wcs.style |=  WS_VISIBLE;

	*psplcNew = qwcw;
	WpChildWnd * pwcwFirst = dynamic_cast<WpChildWnd *>(psplcCopy);
	if (pwcwFirst)
		pwcwFirst->CopyRootTo(qwcw);
	else
	{
		int fiet = MainWnd()->FileType();
		qwcw->Init(MainWnd()->FileName(), &fiet, MainWnd());
		MainWnd()->SetFileType(fiet);	// to what we figured out from the file itself
	}
	qwcw->CreateHwnd(wcs);

	AddRefObj(*psplcNew);
}

WpDa * WpSplitWnd::DataAccess()
{
	return ChildWnd()->DataAccess();
}

WpChildWnd * WpSplitWnd::ChildWnd()
{
	WpChildWnd * pwcw = dynamic_cast<WpChildWnd *>(GetPane(0));
	Assert(pwcw);
	return pwcw;
}

/***********************************************************************************************
	WpChildWnd stuff.
***********************************************************************************************/

static DummyFactory g_fact(_T("SIL.WorldPad.WpChildWnd"));

/*----------------------------------------------------------------------------------------------
	Make a root box.
----------------------------------------------------------------------------------------------*/
void WpChildWnd::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
	IVwRootBox ** pprootb, bool fPrintingSel)
{
	AssertPtrN(pwsf);
	*pprootb = NULL;

	IVwRootBoxPtr qrootb;
	if (WpApp::MainApp()->Vertical())
		qrootb.CreateInstance(CLSID_VwInvertedRootBox);
	else
		qrootb.CreateInstance(CLSID_VwRootBox);
	// SetSite takes an IVwRootSite, which this class implements.
	CheckHr(qrootb->SetSite(this));
	HVO hvoDoc = khvoText;
	int frag = kfrText;
	if (pwsf)
		CheckHr(m_qda->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(m_qda));
	AfStylesheetPtr qwpsts = m_qwpwnd->GetStylesheet();

	if (fPrintingSel)
	{
		// TODO: FIGURE OUT HOW TO INITIALIZE THE NEW ROOT BOX WITH ONLY THE SELECTION FROM
		// m_qrootb.
		IVwViewConstructor * pvvc = m_qvc;
		CheckHr(qrootb->SetRootObjects(&hvoDoc, &pvvc, &frag, qwpsts, 1));
	}
	else
	{
		// We need a pointer to the pointer, and we can't use &m_qvc because that clears the
		// pointer!!
		IVwViewConstructor * pvvc = m_qvc;
		CheckHr(qrootb->SetRootObjects(&hvoDoc, &pvvc, &frag, qwpsts, 1));
	}

	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	pafw->RegisterRootBox(qrootb);
	*pprootb = qrootb.Detach();
}

/*----------------------------------------------------------------------------------------------
	Return true if the OS supports automatic keyboard switching.
	CURRENTLY NOT USED.
----------------------------------------------------------------------------------------------*/
bool WpChildWnd::AutoKeyboardSupport()
{
	OSVERSIONINFOEX osvi;
	BOOL bOsVersionInfoEx;
	ZeroMemory(&osvi, sizeof(OSVERSIONINFOEX));
	osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);
	bOsVersionInfoEx = GetVersionEx((OSVERSIONINFO *) &osvi);
	if (!bOsVersionInfoEx)
	{
		// If OSVERSIONINFOEX doesn't work, try OSVERSIONINFO.
		osvi.dwOSVersionInfoSize = sizeof (OSVERSIONINFO);
		if (!GetVersionEx((OSVERSIONINFO *) &osvi))
			return FALSE;
	}

	switch (osvi.dwPlatformId)
	{
	case VER_PLATFORM_WIN32_NT:			// Windows 2000 or NT
		return true;
	case VER_PLATFORM_WIN32_WINDOWS:	// Windows 95 or 98
	default:
		return false;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle the Undo command
----------------------------------------------------------------------------------------------*/
bool WpChildWnd::CmdEditUndo(Cmd * pcmd)
{
	if (!m_qda)
		return false;

#ifdef JohnT_10_17_2001_AutoUndoPropChange
	// This used to be necessary until I enhanced VwUndoDa to broadcast PropChanged for all
	// changes caused by Undo items.
	if (m_qda->Undo())
	{
		ReconstructAndReplaceSel();
	}
#else
	m_qda->Undo();
#endif
	return true;
}

/*----------------------------------------------------------------------------------------------
	Enable or disable the Undo command
----------------------------------------------------------------------------------------------*/
bool WpChildWnd::CmsEditUndo(CmdState & cms)
{
	bool f;
	if (!m_qda)
		f = false;
	else
		f = m_qda->CanUndo();
	cms.Enable(f);
	StrApp staLabel = MainWnd()->UndoRedoText(false);
	cms.SetText(staLabel, staLabel.Length());
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the Redo command
----------------------------------------------------------------------------------------------*/
bool WpChildWnd::CmdEditRedo(Cmd * pcmd)
{
	if (!m_qda)
		return false;

	if (m_qda->Redo())
	{
		ReconstructAndReplaceSel();
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Enable or disable the Redo command
----------------------------------------------------------------------------------------------*/
bool WpChildWnd::CmsEditRedo(CmdState & cms)
{
	bool f;
	if (!m_qda)
		f = false;
	else
		f = m_qda->CanRedo();
	cms.Enable(f);
	StrApp staLabel = MainWnd()->UndoRedoText(true);
	cms.SetText(staLabel, staLabel.Length());
	return true;
}

/*----------------------------------------------------------------------------------------------
	Create a selection at the beginning of the document.
----------------------------------------------------------------------------------------------*/
void WpChildWnd::MakeInitialSel()
{
	if (!m_qrootb)
		return;

	CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, NULL));
}

/*----------------------------------------------------------------------------------------------
	Select the entire document.
----------------------------------------------------------------------------------------------*/
bool WpChildWnd::CmdEditSelAll1(Cmd * pcmd)
{
	SelectAll();
	return true;
}

/*----------------------------------------------------------------------------------------------
	Bring up the dialog to set the document direction.
	CURRENTLY NOT BEING USED.
----------------------------------------------------------------------------------------------*/
bool WpChildWnd::CmdFmtDoc(Cmd * pcmd)
{
	WpDocDlgPtr qdlg;
	qdlg.Create();
	qdlg->SetDataAccess(m_qda);
	if (qdlg->DoModal(m_hwnd) == kctidOk)
	{
		ReconstructAndReplaceSel();
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Reconstruct the view, remembering the selection so we can replace it.
----------------------------------------------------------------------------------------------*/
void WpChildWnd::ReconstructAndReplaceSel()
{
	// Save the view selection level information by calling AllTextSelInfo on the
	// selection.
	bool fMakeSel = false;
	int cvsli = 0;
	VwSelLevInfo * prgvsli = NULL;
	int ihvoRoot;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int encSel;
	ComBool fAssocPrev;
	int ihvoEnd;
	ITsTextPropsPtr qttpSel;
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (qvwsel)
	{
		CheckHr(qvwsel->CLevels(false, &cvsli));
		cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
		prgvsli = NewObj VwSelLevInfo[cvsli];

		CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, cvsli, prgvsli,
			&tagTextProp, &cpropPrevious,
			&ichAnchor, &ichEnd, &encSel, &fAssocPrev, &ihvoEnd, &qttpSel));
		fMakeSel = true;
	}

	int nRtl = m_qda->DocRightToLeft();
	if (ViewConstructor())
		ViewConstructor()->SetRightToLeft((bool)nRtl);
	m_qrootb->Reconstruct();

	// Now restore the selection by a call to MakeTextSelection on the RootBox pointer.
	if (fMakeSel)
	{
		m_qrootb->MakeTextSelection(ihvoRoot, cvsli, prgvsli,
			tagTextProp, cpropPrevious,
			ichAnchor, ichEnd, encSel, fAssocPrev, ihvoEnd, qttpSel, true, NULL);
		delete[] prgvsli;
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the window, using the text from the given file.
----------------------------------------------------------------------------------------------*/
void WpChildWnd::Init(StrAnsi staFileName, int * pft, WpMainWnd * pwpwnd)
{
	AssertPtr(pwpwnd);

	m_fCanDoRtl = true;
	SetOneDefaultFont(true);
	m_hklCurr = (HKL)-1;
	m_qvc.Attach(NewObj StVc(pwpwnd->UserWs()));
	if (!WpApp::MainApp()->Vertical())
		m_qvc->BeLazy();
	m_qda.Attach(NewObj WpDa);
	m_qwpwnd = pwpwnd;
	WpStylesheet * pwpsts = dynamic_cast<WpStylesheet *>(m_qwpwnd->GetStylesheet());
	Assert(pwpsts);
	if (pwpsts)
		pwpsts->Init(m_qda); // do this before we load from the file
	m_qda->InitNew(staFileName, pft, pwpwnd, this);
	int nRtl = m_qda->DocRightToLeft();
	m_qvc->SetRightToLeft((bool)nRtl);
}

/*----------------------------------------------------------------------------------------------
	Initialize a second child to use the same root box as the first one.
	This should generally initialize anything that Init does.
----------------------------------------------------------------------------------------------*/
void WpChildWnd::CopyRootTo(AfVwSplitChild * pavscOther)
{
	SuperClass::CopyRootTo(pavscOther);
	WpChildWnd * pwcw = dynamic_cast<WpChildWnd *>(pavscOther);
	Assert(pwcw);
	pwcw->m_qvc = m_qvc;
	pwcw->m_qda = m_qda;
}

/*----------------------------------------------------------------------------------------------
	Force the window to redo the layout and redraw.
	NOT CURRENTLY BEING USED
----------------------------------------------------------------------------------------------*/
#if 0
bool WpChildWnd::Relayout()
{
	InitGraphics();
	int dxdAvailWidth = LayoutWidth();
	m_dxdLayoutWidth = dxdAvailWidth;
	// If we have less than 1 point, probably the window has not received its initial
	// OnSize message yet, and we can't do a meaningful layout.
	if (m_dxdLayoutWidth < 2)
	{
		m_dxdLayoutWidth = -50000; // No drawing until we get reasonable size.
		UninitGraphics();
		return true;
	}
	if (FAILED(m_qrootb->Layout(m_qvg, dxdAvailWidth)))
	{
		Warn("Root box layout failed");
		m_dxdLayoutWidth = -50000; // No drawing until we get successful layout.
	}
	Invalidate();
	UninitGraphics();
	return true;
}
#endif

/*----------------------------------------------------------------------------------------------
	Override to make an initial selection, which can't be done until the superclass
	method creates the root box.
----------------------------------------------------------------------------------------------*/
int WpChildWnd::OnCreate(CREATESTRUCT * pcs)
{
	int result = SuperClass::OnCreate(pcs);
	Assert(m_qrootb);
	if (DataAccess()->FileLoadStatus() != kflsAborted)
	{
		// If we are in the process of splitting, there will already be a selection--don't
		// lose it!
		IVwSelectionPtr qsel;
		m_qrootb->get_Selection(&qsel);
		if (!qsel)
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, NULL));
	}
	return result;
}

/*----------------------------------------------------------------------------------------------
	Commit the window's selection.
----------------------------------------------------------------------------------------------*/
bool WpChildWnd::CommitSelection()
{
	IVwSelectionPtr qsel;
	CheckHr(m_qrootb->get_Selection(&qsel));
	if (!qsel)
		return true;
	ComBool fOkay;
	CheckHr(qsel->Commit(&fOkay));
	return (bool)fOkay;
}

/*----------------------------------------------------------------------------------------------
	As it finally goes away, make doubly sure all pointers get cleared. This helps break cycles.
----------------------------------------------------------------------------------------------*/
void WpChildWnd::OnReleasePtr()
{
	// By contract we must clear all our own smart pointers.
	m_qvc.Clear();
	m_qda.Clear();
	SuperClass::OnReleasePtr();
}

/*----------------------------------------------------------------------------------------------
	Return the list of fonts that are appropriate for the writing system of the current
	selection. This used to determine this based on whether any of them were Graphite
	renderers that could not alter font. Maybe one day it will use Unicode ranges to decide
	which fonts can REALLY work. For now, assume all fonts are useable for everything.
----------------------------------------------------------------------------------------------*/
void WpChildWnd::GetCurrentFontChoices(Vector<StrApp> & vstrFonts)
{
	// Get selection. Can't do command unless we have one.
	if (!m_qrootb)
		return;

	//	Include all fonts (now independent of whether any use Graphite).

	ILgFontManagerPtr qfm;
	SmartBstr bstrNames;

	qfm.CreateInstance(CLSID_LgFontManager);
	CheckHr(qfm->AvailableFonts(&bstrNames));
	static long ipszList = 0; // Returned value from SendMessage.

	StrApp strNameList;
	strNameList.Assign(bstrNames.Bstr(), BstrLen(bstrNames.Bstr())); // Convert BSTR to StrApp.
	int cchLength = strNameList.Length();
	StrApp strName; // Individual font name.
	int ichMin = 0; // Index of the beginning of a font name.
	int ichLim = 0; // Index that is one past the end of a font name.

	// Add the three predefined names to the combo box.
	// Review JohnT: should these come from a resource? Dangerous--they are hard-coded in
	// multiple places.
	Vector<StrUni> vstuDefaultFont;
	FwStyledText::FontUiStrings(true, vstuDefaultFont);
	for (int istu = 0; istu < vstuDefaultFont.Size(); istu++)
	{
		StrApp str(vstuDefaultFont[istu]);
		vstrFonts.Push(str);
	}

	// Add each font name to the combo box.
	while (ichLim < cchLength)
	{
		ichLim = strNameList.FindCh(L',', ichMin);
		if (ichLim == -1) // i.e., if not found.
		{
			ichLim = cchLength;
		}

		strName.Assign(strNameList.Chars() + ichMin, ichLim - ichMin);
		vstrFonts.Push(strName);

		ichMin = ichLim + 1;
	}
}

/*----------------------------------------------------------------------------------------------
	Get the writing system and old writing system for the current selection.
----------------------------------------------------------------------------------------------*/
void WpChildWnd::GetWsOfSelection(int * pws)
{
	*pws = 0;	// default if we don't find anything else

	if (!m_qrootb)
		return;
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
		return;
	GetWsOfSelection(qvwsel, pws);
}

/*----------------------------------------------------------------------------------------------
	Get the writing system and old writing system for given selection.
----------------------------------------------------------------------------------------------*/
void WpChildWnd::GetWsOfSelection(IVwSelection * pvwsel, int * pws)
{
	*pws = 0;
	IVwSelectionPtr qvwsel;
	TtpVec vqttp;
	VwPropsVec vqvps;

	if (!GetCharacterProps(&qvwsel, vqttp, vqvps))
		return;
	int cttp = vqttp.Size();

	int ittp;
	ITsTextProps * pttp;

	for (ittp = 0; ittp < cttp; ++ittp)
	{
		pttp = vqttp[ittp];
		int var;
		HRESULT hr;
		CheckHr(hr = pttp->GetIntPropValues(ktptWs, &var, pws));
		if (hr == S_OK)
			return;
	}
}

/*----------------------------------------------------------------------------------------------
	Get the properties for the given selection.
----------------------------------------------------------------------------------------------*/
void WpChildWnd::GetPropsOfSelection(ITsTextProps ** ppttpRet)
{
	*ppttpRet = NULL;

	if (!m_qrootb)
		return;
	IVwSelectionPtr qvwsel;
	TtpVec vqttp;
	VwPropsVec vqvps;

	if (!GetCharacterProps(&qvwsel, vqttp, vqvps))
		return;

	*ppttpRet = vqttp[0];
	AddRefObj(*ppttpRet);
}

/*----------------------------------------------------------------------------------------------
	Update the paragraph direction based on the first character in the paragraph, or the
	old writing system of the selection.

	NOT CURRENTLY USED.
----------------------------------------------------------------------------------------------*/
#if 0
bool WpChildWnd::UpdateParaDirection(IVwSelection * pvwselNew)
{
	IVwSelectionPtr qvwsel;
	HVO hvoText;
	int tagText;
	VwPropsVec vqvps;
	int ihvoFirst, ihvoLast;
	ISilDataAccessPtr qsda;
	TtpVec vqttp;

	// Get the paragraph properties from the selection.
	if (!GetParagraphProps(&qvwsel, hvoText, tagText, vqvps, ihvoFirst, ihvoLast, &qsda, vqttp))
		return false;

	ComBool fRange;
	CheckHr(qvwsel->get_IsRange(&fRange));
	if (fRange)
		return false;

	Assert(vqvps.Size() == 1);
	Assert(vqttp.Size() == 1);

	if (vqttp[0].Ptr() == NULL)
		return false; // for now

	//	Get the current paragraph direction.
	int nVar, nRtlOld;
	CheckHr(vqttp[0]->GetIntPropValues(ktptRightToLeft, &nVar, &nRtlOld));

	if (nVar == 1)
	{
		//	The paragraph direction was set explicitly by the user; don't change it.
		return false;
	}

	ComBool fEndPoint, fAssocPrev;
	int ich, ws;
	ITsStringPtr qtss;
	HVO hvoObj;
	PropTag tag;
	CheckHr(qvwsel->TextSelInfo(false, &qtss, &ich, &fAssocPrev, &hvoObj, &tag, &ws));

	HVO hvoPara;
	CheckHr(qsda->get_VecItem(hvoText, tagText, ihvoFirst, &hvoPara));

	if (ich == 0)
	{
		GetWsOfSelection(pvwselNew, &ws);
	}
	else
	{
		//	Get the writing system of the first character.
		ITsStringPtr qtss;
		CheckHr(qsda->get_StringProp(hvoPara, kflidStTxtPara_Contents, &qtss));
		ITsTextPropsPtr qttpStr1;
		CheckHr(qtss->get_PropertiesAt(0, &qttpStr1));
		int var;
		CheckHr(qttpStr1->GetIntPropValues(ktptWs, &var, &ws));
	}

	//	Get the direction of the initial old writing system.
	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
	IWritingSystemPtr qws;
	CheckHr(qwsf->get_EngineOrNull(ws, &qws));
	if (!qws)
		return false;
	ComBool fRtlWs;
	CheckHr(qws->get_RightToLeft(&fRtlWs));

	bool fRtlOld = (nRtlOld == -1) ? !fRtlWs : (bool)nRtlOld;

	if (fRtlOld == fRtlWs)
		//	Don't set it unless it is actually changing, to avoid bogus redraws.
		return false;

	//	Set the direction for the paragraph.
	ITsTextPropsPtr qttpPara;
	CheckHr(qsda->get_UnknownProp(hvoPara, kflidStPara_StyleRules, IID_ITsTextProps,
		(void**)&qttpPara));
	ITsPropsBldrPtr qtpb;
	CheckHr(qttpPara->GetBldr(&qtpb));
	CheckHr(qtpb->SetIntPropValues(ktptRightToLeft, ktpvEnum, (int)fRtlWs));
	CheckHr(qtpb->GetTextProps(&qttpPara));
	WpDaPtr qwpda = dynamic_cast<WpDa *>(qsda.Ptr());
	Assert(qwpda);
	CheckHr(qwpda->SetUnknown(hvoPara, kflidStPara_StyleRules, qttpPara));

	//	Force a redraw by faking a property change.
	//	This will destroy the selection, so first, save it.
	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
	VwSelLevInfo * prgvsli;

	try
	{
		prgvsli = NewObj VwSelLevInfo[cvsli];
		int ihvoRoot;
		PropTag tagTextProp;
		int cpropPrevious;
		int ichAnchor;
		int ichEnd;
		int encSel;
		ComBool fAssocPrev;
		int ihvoEnd;
		ITsTextProps * pttpSel;

		// Save the view selection level information by calling AllTextSelInfo on the
		// selection.
		CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, cvsli, prgvsli, &tagTextProp, &cpropPrevious,
			&ichAnchor, &ichEnd, &encSel, &fAssocPrev, &ihvoEnd, &pttpSel));

		// Broadcast the fake property change to all roots, by calling PropChanged on the
		// SilDataAccess pointer.
		qsda->PropChanged(m_qrootb, kpctNotifyMeThenAll, hvoText,
			tagText, ihvoFirst, ihvoFirst + 1, 1);

		// Now restore the selection by a call to MakeTextSelection on the RootBox pointer.
		// DO NOT CheckHr. This may legitimately fail, e.g., if there is no editable field.
		// REVIEW JohnT: Should we try again, e.g., to make a non-editable one?
		m_qrootb->MakeTextSelection(ihvoRoot, cvsli, prgvsli, tagTextProp, cpropPrevious,
			ichAnchor, ichEnd, encSel, fAssocPrev, ihvoEnd, pttpSel, true, NULL);

		// And clean up.
		if (prgvsli)
			delete[] prgvsli;
	}
	catch (...)
	{
		// Properly delete the array of VwSelLevInfo.
		if (prgvsli)
			delete prgvsli;
		throw;
	}

	return true;
}
#endif

/*----------------------------------------------------------------------------------------------
	Open a version of the styles dialog that enables the Font Features button.
----------------------------------------------------------------------------------------------*/
bool WpChildWnd::OpenFormatStylesDialog(HWND hwnd, bool fCanDoRtl, bool fOuterRtl,
	IVwStylesheet * past, TtpVec & vqttpPara, TtpVec & vqttpChar, bool fCanFormatChar,
	StrUni * pstuStyleName, bool & fStylesChanged, bool & fApply, bool & fReloadDb)
{
	AfStylesDlg afsd;
	// The next three lines are required by the refactoring of the styles dialog code to allow
	// embedding it in a COM DLL.  We don't use the COM/DLL access here in order to keep the
	// distribution of WorldPad as simple as possible.
	afsd.SetMsrSys(AfApp::Papp()->GetMsrSys());
	afsd.SetHelpFile(AfApp::Papp()->GetHelpFile());

	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
	afsd.SetLgWritingSystemFactory(qwsf);
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));
	afsd.SetUserWs(wsUser);

	ISilDataAccessPtr qsda;
	CheckHr(past->get_DataAccess(&qsda));
	int cws;
	CheckHr(qsda->get_WritingSystemsOfInterest(0, NULL, &cws));
	Vector<int> vwsAvailable;
	vwsAvailable.Resize(cws);
	CheckHr(qsda->get_WritingSystemsOfInterest(vwsAvailable.Size(),
		vwsAvailable.Begin(), &cws));
	return afsd.AdjustTsTextProps(hwnd, fCanDoRtl, fOuterRtl,
		true, // enable Font Features
		true, // one default font
		past, vqttpPara, vqttpChar, fCanFormatChar, vwsAvailable, 0, NULL,
		pstuStyleName, fStylesChanged, fApply, fReloadDb);
}


//:>--------------------------------------------------------------------------------------------
//:>	IDropTarget methods for WpChildWnd
//:>	TODO ?? (SharonC): Enhance to handle copying and pasting text
//:>--------------------------------------------------------------------------------------------

/*----------------------------------------------------------------------------------------------
	Happens when the cursor enters the window area with possibly something to drop.
	Indicates whether a drop can be accepted, and if so, the effect of the drop.
	See MS documentation for IDropTarget::DragEnter.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WpChildWnd::DragEnter(IDataObject * pdo, DWORD grfKeyState, POINTL ptl,
	DWORD * pdwEffect)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pdo);
	ChkComArgPtrN(pdwEffect);

	FORMATETC fe = {CF_HDROP, NULL, DVASPECT_CONTENT, -1, TYMED_FILE};
	if (SUCCEEDED(pdo->QueryGetData(&fe)))
	{
		m_fDropFile = TRUE;
	}
	return DragOver(grfKeyState, ptl, pdwEffect);

	END_COM_METHOD(g_fact, IID_IDropTarget);
}

/*----------------------------------------------------------------------------------------------
	Happens when the cursor moves out of the window area or when the operation is cancelled
	or finished.
	See MS documentation for IDropTarget::DragLeave.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WpChildWnd::DragLeave(void)
{
	BEGIN_COM_METHOD;

	if (m_fDropFile)
	{
		m_fDropFile = false;
	}
	return S_OK;

	END_COM_METHOD(g_fact, IID_IDropTarget);
}

/*----------------------------------------------------------------------------------------------
	Happens while the cursor is over the target window.
	See MS documentation for IDropTarget::DragOver.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WpChildWnd::DragOver(DWORD grfKeyState, POINTL ptl, DWORD * pdwEffect)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pdwEffect);

	m_grfKeyState = grfKeyState;
	if (m_fDropFile)
	{
		*pdwEffect = DROPEFFECT_COPY;
	}
	m_dwEffect = *pdwEffect;
	return S_OK;

	END_COM_METHOD(g_fact, IID_IDropTarget);
}

/*----------------------------------------------------------------------------------------------
	Happens when the user drops something into the target window.
	See MS documentation for IDropTarget::Drop.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WpChildWnd::Drop(IDataObject * pdo, DWORD grfKeyState, POINTL ptl,
	DWORD * pdwEffect)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pdo);
	ChkComArgPtrN(pdwEffect);

	if (!pdo || !pdwEffect)
	{
		DragLeave();
		return E_INVALIDARG;
	}
	if (DROPEFFECT_NONE == m_dwEffect)
		return S_OK;
	HRESULT hr;
	if (m_fDropFile)
	{
		FORMATETC fe = {CF_HDROP, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL};
		STGMEDIUM medium;
		if (SUCCEEDED(hr = pdo->GetData(&fe, &medium)))
		{
			MainWnd()->OnDropFiles((HDROP)medium.hGlobal, m_grfKeyState & MK_RBUTTON);
			ReleaseStgMedium(&medium);
		}
	}
	*pdwEffect = m_dwEffect;
	DragLeave();
	return hr;
	END_COM_METHOD(g_fact, IID_IDropTarget);
}

/*----------------------------------------------------------------------------------------------
	Open the dropped file(s).
----------------------------------------------------------------------------------------------*/
long WpMainWnd::OnDropFiles(HDROP hDropInfo, BOOL fContext)
{
	int cFiles = DragQueryFile(hDropInfo, 0xFFFFFFFF, NULL, 0);
	achar szFilename[MAX_PATH];
	for (int iFile = 0; iFile < cFiles; iFile++)
	{
		ZeroMemory(szFilename, sizeof(szFilename));
		DragQueryFile(hDropInfo, iFile, szFilename, sizeof(szFilename));
		StrAnsi staFile;
		staFile.Assign(szFilename);		// FIX ME FOR PROPER CODE CONVERSION!
		// 1 means no file type specified
		DoOpenFile(const_cast<char *>(staFile.Chars()), kfietUnknown);
	}
	return 0;
}

//:>********************************************************************************************
//:>	Save Plain Text dialog
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize the dialog with the options.
----------------------------------------------------------------------------------------------*/
bool WpSavePlainTextDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Subclass the Help button to show the standard icon.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	StrApp strRes;

	HWND hwndTextEnc = ::GetDlgItem(m_hwnd, kctidTextWs);
	//	Fill in the options in the list box. The items below should correspond to the
	//	kfiet constants in the enumeration.
	if (m_fUtf16)
	{
		strRes.Load(kstidUtf16);
		::SendMessage(hwndTextEnc, LB_ADDSTRING, 0, (LPARAM)strRes.Chars());
	}
	strRes.Load(kstidUtf8);
	::SendMessage(hwndTextEnc, LB_ADDSTRING, 0, (LPARAM)strRes.Chars());
	strRes.Load(kstidAnsi);
	::SendMessage(hwndTextEnc, LB_ADDSTRING, 0, (LPARAM)strRes.Chars());

	m_cTextEnc = (m_fUtf16) ? 3 : 2;

	if (m_fSaving)
		strRes.Load(kstidSavePlainText);
	else
		strRes.Load(kstidOpenPlainText);
	::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)strRes.Chars());

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Handle the behavior of various controls.
----------------------------------------------------------------------------------------------*/
bool WpSavePlainTextDlg::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	HWND hwndTextEnc = ::GetDlgItem(m_hwnd, kctidTextWs);
	int iSel = ::SendMessage(hwndTextEnc, LB_GETCURSEL, 0, 0);

	switch (id)
	{
	case kctidTextWs:
		switch (pnmh->code)
		{
		case LBN_SELCHANGE:
			Assert(iSel >= 0 || iSel < m_cTextEnc);
			m_iSel = iSel;
			break;
		default:
			break;
		}
		break;

	default:
		break;
	}

	return SuperClass::OnNotifyChild(id, pnmh, lnRet);
}

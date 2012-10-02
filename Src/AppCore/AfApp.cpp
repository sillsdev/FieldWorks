/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfApp.cpp
Responsibility: Steve McConnel (was Shon Katzenberger)
Last reviewed:

	This file contains class definitions for the following classes:
		FilterMenuNode : GenRefObj
		SortMenuNode : GenRefObj
		AfApp : CmdHandler
		AfDbApp : AfApp
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:> These have to be instantiated somewhere.
AfApp * AfApp::s_papp = NULL;
int AfApp::s_cmsgSinceIdle;
//:End Ignore

/*----------------------------------------------------------------------------------------------
	We can't have our own WinMain, since we are using code from Generic, and that uses
	ModuleEntry, and it insists on owning WinMain in an exe. Instead, we implement this.
	It is a static function declared in ModuleEntry.h

	@param hinst Handle to the instance of this application.
	@param pszCmdLine Pointer to the command line string used to launch this application.
	@param nShowCmd Specify how the main window is to be shown.  See MSDN documentation for
					ShowWindow for details.

	@return 1 if successful, 0 if an error occurs before the program really get under way.
----------------------------------------------------------------------------------------------*/
int ModuleEntry::Run(HINSTANCE hinst, LPSTR pszCmdLine, int nShowCmd)
{
	AfApp * papp = AfApp::Papp();
	AssertPtr(papp);

	StrApp strCmdLine(pszCmdLine);
	return papp->Run(hinst, strCmdLine.Chars(), nShowCmd);
}


//:>********************************************************************************************
//:>	AfApp methods.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Constructor for the application base class.
----------------------------------------------------------------------------------------------*/
AfApp::AfApp()
{
	Assert(!s_papp);
	s_papp = this;
	StrUtil::InitIcuDataDir();
	SetFwPaths();
}


/*----------------------------------------------------------------------------------------------
	Destructor for the application base class.
----------------------------------------------------------------------------------------------*/
AfApp::~AfApp()
{
	Assert(m_vqafw.Size() == 0);
	for (int iwnd = m_vqafw.Size(); --iwnd >= 0; )
		DestroyWindow(m_vqafw[iwnd]->Hwnd());
	Assert(s_papp == this);
	s_papp = NULL;
}

/*----------------------------------------------------------------------------------------------
	Warns the user that the action they are about to do is not undoable.

	@return true to continue and false to not proceede.
----------------------------------------------------------------------------------------------*/
	bool AfApp::ConfirmUndoableAction()
	{
		StrUni stuMsg(kstidConfirmUndoableActionMsg);
		StrUni stuCpt(kstidConfirmUndoableActionCpt);

		int iResult = ::MessageBox(NULL, stuMsg.Chars(), stuCpt.Chars(),
			MB_YESNO | MB_ICONINFORMATION | MB_DEFBUTTON1);

		return iResult == IDYES ? true : false;
	}

/*----------------------------------------------------------------------------------------------
	Pop up a modal help window for the current window.

	@param pcmd Pointer to menu command.  (This parameter is not used in in this method.)

	@return true.
----------------------------------------------------------------------------------------------*/
bool AfApp::CmdHelpAbout(Cmd * pcmd)
{
	AssertObj(pcmd);
	ShowHelpAbout();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Run the application. Calls Init, Loop and CleanUp.

	@param hinst Handle to the instance of this application.
	@param pszCmdLine Pointer to the command line string used to launch this application.
	@param nShow Specify how the main window is to be shown.  See MSDN documentation for
					ShowWindow for details.

	@return 1 if successful, 0 if an error occurs before the program really get under way.
----------------------------------------------------------------------------------------------*/
int AfApp::Run(HINSTANCE hinst, Pcsz pszCmdLine, int nShow)
{
	AssertObj(this);
	// inches is the default measurement system until something else is loaded from registry.
	m_nMsrSys = kninches;

	m_hinst = hinst;
	m_pszCmdLine = pszCmdLine;

	StrUni stuKey(L"help");
	Vector<StrUni> vstuArg;
	try
	{
		ParseCommandLine(pszCmdLine);
	}
	catch (...)
	{
		// Clear out old values, and add 'help' for usage.
		m_hmsuvstuCmdLine.Clear();
		m_hmsuvstuCmdLine.Insert(stuKey, vstuArg);
	}
	// Check for 'help' on command line.
	if (m_hmsuvstuCmdLine.Retrieve(stuKey, &vstuArg))
	{
		// "FieldWorks %s, Version (%d.%d.%d.%d)\n\nUsage: %s [%s] [options]\n\nOptions:\n\n%s"
		// kstidCmdLineUsage is the above string.
		// Parameters to string formatter, in order of appearance:
		// %s - application name from kstidAppName.
		// %d - major version number dwMajor
		// other %d - three levels of minor version numbers (dwMinor1, dwMinor2, dwMinor3)
		// %s - program name from pszFilename, but sans path & extension (strFilename).
		// %s - name of item to be edited from app specific definition of kstidCmdLineName.
		// %s - app specific list of options from kstidCmdLineOptions.
		StrApp strBase(kstidCmdLineUsage);
		StrApp strAppName(kstidAppName);
		StrApp strName(kstidCmdLineName);
		StrApp strOptions(kstidCmdLineOptions);
		achar * pszFilename = const_cast<achar *>(ModuleEntry::GetModulePathName());
		StrApp strTemp(pszFilename);
		int iDot = strTemp.ReverseFindCh('.');
		int iSlash = strTemp.ReverseFindCh('\\');
		StrApp strFilename = strTemp.Mid(iSlash + 1, iDot - iSlash - 1);
		// Get the version information.
		DWORD dwT;
		DWORD cbT = ::GetFileVersionInfoSize(pszFilename, &dwT);
		Vector<BYTE> vbVerInfo;
		vbVerInfo.Resize(cbT);
		DWORD dwMajor = 0;
		DWORD dwMinor1 = 0;
		DWORD dwMinor2 = 0;
		DWORD dwMinor3 = 0;
		if (::GetFileVersionInfo(pszFilename, 0, cbT, vbVerInfo.Begin()))
		{
			VS_FIXEDFILEINFO * pffi;
			uint nT;
			::VerQueryValue(vbVerInfo.Begin(), _T("\\"), (void **)&pffi, &nT);
			dwMajor = HIWORD(pffi->dwFileVersionMS);
			dwMinor1 = LOWORD(pffi->dwFileVersionMS);
			dwMinor2 = HIWORD(pffi->dwFileVersionLS);
			dwMinor3 = LOWORD(pffi->dwFileVersionLS);
		}
		StrApp strUsage;
		strUsage.Format(strBase.Chars(),
				strAppName.Chars(),
				dwMajor, dwMinor1, dwMinor2, dwMinor3,
				strFilename.Chars(), strName.Chars(), strOptions.Chars());
		::MessageBox(NULL, strUsage.Chars(), strAppName.Chars(), MB_OK | MB_ICONINFORMATION);
		return 1;
	}

	m_nShow = nShow;

	// 0 indicates that we didn't successfully enter the main loop.
	int nRet = 0;

	try
	{
		// Check for drop-dead date.
		if (SilTime::CurTime() - DropDeadDate() > 0)
		{
			StrApp staMsg(kstidDropDead);
			::MessageBox(0, staMsg.Chars(), NULL, MB_OK + MB_ICONERROR + MB_APPLMODAL);
			return 1;
		}
		// Add the application to the command handler list.
		AddCmdHandler(this, kcmhlApp, kgrfcmmAll);

		// See if a splash screen is needed. This uses the fact that if the application is
		// started because a COM interface (e.g. backup) needs to run, then the commandline
		// contains "-Embedding", and no splash screen should be shown.
		m_fSplashNeeded = true;
		if (_tcsicmp(pszCmdLine, _T("-Embedding")) == 0)
			m_fSplashNeeded = false;

		// If we need the splash screen, check the registry to make sure it isn't disabled.
		if (m_fSplashNeeded)
		{
			FwSettings::GetBool(_T("Software\\SIL\\FieldWorks"), NULL, _T("DisableSplashScreen"),
				&m_fSplashNeeded);
		}

		if (m_fSplashNeeded && !m_qSplashScreenWnd.Ptr())
		{
			// Display the splash screen. Note that this is before an 'initial message' is
			// available, but at least we will get the screen up relatively early.
			ShowSplashScreen();
		}
		Init();
	}
	catch (Throwable & thr)
	{
		// If an error block has been set then display any message within it.
		int hHelpId = thr.HelpId();
		if (hHelpId == -1)
		{
			::MessageBox(NULL, thr.Message(), NULL, MB_ICONERROR);
		}
		CleanUp();	// We were expecting an error block, but will do nothing if not found.
		return 0;
	}
	catch (...)
	{
		// We need this here to clean up global smart pointers before OleUninitialize gets
		// called.  Should this be in a try/catch loop? Is that allowed in a catch block?
		CleanUp();
		// Notify the user that initialization failed.
		// We are giving an earlier error message if MSDE isn't running. This situation
		// probably means the specified database was not present.
		StrApp str(kstidInitAppError);
		::MessageBox(NULL, str.Chars(),	NULL, MB_ICONERROR);
		return 0;
	}

	// If the Init method wants to quit, we won't have any windows to handle messages, so
	// return before entering our message loop.
	// REVIEW ShonK: This might need to use PeekMessage to make sure there aren't any messages
	// in the queue.
	if (m_fQuit)
		return nRet;

	try
	{
		nRet = 1;
		nRet = Loop();
	}
	catch (...)
	{
		// TODO ShonK: Notify the user of this error.
	}

	try
	{
		CleanUp();
	}
	catch (...)
	{
		// TODO ShonK: Notify the user of this error.
	}

	return nRet;
}

/*----------------------------------------------------------------------------------------------
	ShowSplashScreen routine.  Displays the splash screen.
----------------------------------------------------------------------------------------------*/
void AfApp::ShowSplashScreen()
{
	m_qSplashScreenWnd.CreateInstance("FwCoreDlgs.FwSplashScreen");
	CheckHr(m_qSplashScreenWnd->Show()); // Needs to be done first!

	StrApp strAppName(kstidAppName);
	CheckHr(m_qSplashScreenWnd->put_ProdName(strAppName.Bstr()));

	// Get the file version data ...
	StrUni stuProdVersion;
	DWORD cDaysSince1900 = 0;
	StrUni stuFwVersion;
	GetVersionInfo(stuProdVersion, cDaysSince1900, stuFwVersion);

	// December 6, 2006 = 39057.
	if (cDaysSince1900 > 30000)		// version structure with embedded OLE Automation Date.
		CheckHr(m_qSplashScreenWnd->put_ProdOADate(cDaysSince1900));
	CheckHr(m_qSplashScreenWnd->put_ProdVersion(stuProdVersion.Bstr()));
	CheckHr(m_qSplashScreenWnd->put_FieldworksVersion(stuFwVersion.Bstr()));

	CheckHr(m_qSplashScreenWnd->Refresh());
}

/*----------------------------------------------------------------------------------------------
	ShowHelpAbout routine.  Displays the Help/About dialog.
----------------------------------------------------------------------------------------------*/
void AfApp::ShowHelpAbout()
{
	FwCoreDlgs::IFwHelpAboutPtr qabt;
	qabt.CreateInstance("FwCoreDlgs.FwHelpAbout");
	StrApp strAppName(kstidAppName);
	CheckHr(qabt->put_ProdName(strAppName.Bstr()));

	// Get the file version data ...
	StrUni stuProdVersion;
	DWORD cDaysSince1900 = 0;
	StrUni stuFwVersion;
	GetVersionInfo(stuProdVersion, cDaysSince1900, stuFwVersion);

	// December 6, 2006 = 39057.
	if (cDaysSince1900 > 30000)		// version structure with embedded OLE Automation Date.
		CheckHr(qabt->put_ProdOADate(cDaysSince1900));
	CheckHr(qabt->put_ProdVersion(stuProdVersion.Bstr()));
	CheckHr(qabt->put_FieldworksVersion(stuFwVersion.Bstr()));

	StrUni stuDriveLetter(ModuleEntry::GetModulePathName(), 1);
	CheckHr(qabt->put_DriveLetter(stuDriveLetter.Bstr()));
	long bla;
	CheckHr(qabt->ShowDialog(&bla));
}

/*----------------------------------------------------------------------------------------------
	Get the file version data ...
----------------------------------------------------------------------------------------------*/
void AfApp::GetVersionInfo(StrUni & stuProdVersion, DWORD & cDaysSince1900,
	StrUni & stuFwVersion)
{
	achar * pszFilename = const_cast<achar *>(ModuleEntry::GetModulePathName());
	DWORD dwT;
	DWORD cbT = ::GetFileVersionInfoSize(pszFilename, &dwT);
	Vector<BYTE> vbVerInfo;
	vbVerInfo.Resize(cbT);
	DWORD Version = 0;
	DWORD Milestone = 0;
	DWORD Revision = 0;
	DWORD FwVersionMajor = 0;
	DWORD FwVersionMinor = 0;
	DWORD FwRevision = 0;

	if (::GetFileVersionInfo(pszFilename, 0, cbT, vbVerInfo.Begin()))
	{
		VS_FIXEDFILEINFO * pffi;
		uint nT;
		::VerQueryValue(vbVerInfo.Begin(), _T("\\"), (void **)&pffi, &nT);
		Version = HIWORD(pffi->dwFileVersionMS);
		Milestone = LOWORD(pffi->dwFileVersionMS);
		Revision = HIWORD(pffi->dwFileVersionLS);
		cDaysSince1900 = LOWORD(pffi->dwFileVersionLS);
		FwVersionMajor = HIWORD(pffi->dwProductVersionMS);
		FwVersionMinor = LOWORD(pffi->dwProductVersionMS);
		FwRevision = HIWORD(pffi->dwProductVersionLS);
	}
	StrUni stuAppFmt(kstidAppVersion);
	stuProdVersion.Format(stuAppFmt.Chars(), Version, Milestone, Revision, cDaysSince1900);
	if (FwRevision != 0)
	{
		StrUni stuFwFmt(kstidFwVersionWithRev);
		stuFwVersion.Format(stuFwFmt.Chars(), FwVersionMajor, FwVersionMinor, FwRevision);
	}
	else
	{
		StrUni stuFwFmt(kstidFwVersion);
		stuFwVersion.Format(stuFwFmt.Chars(), FwVersionMajor, FwVersionMinor);
	}
}

/*----------------------------------------------------------------------------------------------
	KillApp routine.  Closes all windows, then the app closes after the last window.
----------------------------------------------------------------------------------------------*/
void AfApp::KillApp()
{
	for (int iwnd = m_vqafw.Size(); --iwnd >= 0; )
		DestroyWindow(m_vqafw[iwnd]->Hwnd());
}


/*----------------------------------------------------------------------------------------------
	Quit routine.  May or may not initiate the quit sequence (depending on user input).

	@param fForce Flag whether to force the application to quit.
----------------------------------------------------------------------------------------------*/
void AfApp::Quit(bool fForce)
{
	if (m_fQuit || FQueryQuit(fForce) || fForce)
	{
#if 0 // pre-server-object code
		PostQuitMessage(0);
#else
		// It should look to the user as if we have quit, but don't actually do it unless
		// all exported objects have closed. Just close all our windows. If we are not
		// exporting an object, this will cause an actual quit when the last closes.
		for (int iwnd = m_vqafw.Size(); --iwnd >= 0; )
			::SendMessage(m_vqafw[iwnd]->Hwnd(), WM_CLOSE, 0, 0);
#endif
		m_fQuit = true;
	}
}


/*----------------------------------------------------------------------------------------------
	Initialize the application.
----------------------------------------------------------------------------------------------*/
void AfApp::Init()
{
	AfWnd::RegisterClass(_T("AfClientWnd"), kgrfwcsDef | kfwcsOwnDc, (int)IDC_ARROW,
		0, COLOR_WINDOW, 0);
	AfWnd::RegisterClass(_T("AfVwWnd"), kgrfwcsDef, NULL,
		0, COLOR_WINDOW, 0);
	AfWnd::RegisterClass(_T("AfDeSplitChild"), kgrfwcsDef, NULL,
		0, COLOR_WINDOW, 0);
	INITCOMMONCONTROLSEX iccex = { isizeof(iccex), ICC_USEREX_CLASSES };
	::InitCommonControlsEx(&iccex);	//Enables use of ComboBoxEx32.

	// JT: code to create an OLEDB object moved to RnApp for now. AfApp needs to be able
	// to work without one (e.g., for WorldPad)

	//	Open log file.
	//		The log file has the same name and path as the current application's executable
	//	file, with extension of .log instead of .exe.
	//		If there is a failure then warn user and ensure that the FileStream pointer
	//  is cleared.
	//	TODO JohnL: Use CheckHr instead of CheckExtHr when FileStream is tidied up.
	//
	try
	{
		// Try to open an existing log file for this application, or create if not there.
		bool bNewLog = true;	// true if writing from start of file.
		StrAnsiBufPath stabpLogFile = ModuleEntry::GetModulePathName();
		int ich = stabpLogFile.FindCh('.');
		if (ich < 0)
			throw;	// No point in going on: should never happen.
		stabpLogFile.Replace(ich+1, stabpLogFile.Length(), "log");

		// Opens existing or creates new.
		// NOTE: this call will fail if a second instance of the application has been
		//       created (using a different process), because at this point in the
		//       initialization the check for an existing instance has not been performed.
		//       The failure does not matter, since the process is terminated very soon.
		FileStream::Create(stabpLogFile.Chars(), STGM_READWRITE, &m_qfistLog);

		// See if the log was begun to-day. Do this by reading the first line of the log and
		// extracting the time. There are two reasons for not using the file creation time:
		//  1. If you delete a file and then immediately create another file with the same
		//     name, the new file will, or may, have creation date of the file it supersedes!!
		//  2. FAT file systems store the creation time as Local Time whereas NTFS stores it
		//     as Universal Time.
		Vector<char> vch;
		ULONG cch;
		vch.Resize(11);	// Enough to get the date.
		m_qfistLog->Read(vch.Begin(), 10, &cch);
		vch[10] = 0;
		if (cch > 0)
		{	// Not a new file. Read the date from the first line. If this fails we'll just
			// end up creating a new file, which is deemed to be OK.
			SilTime stimLog;
			StrUtil::ParseDateWithFormat(vch.Begin(), "yyyy-MM-dd", &stimLog);
			SYSTEMTIME systimeNowLocal;
			GetLocalTime(&systimeNowLocal);
			LARGE_INTEGER li;
			li.QuadPart = 0;
			ULARGE_INTEGER libPosition;
			if (stimLog.Date() == systimeNowLocal.wDay &&
				stimLog.Month() == systimeNowLocal.wMonth &&
				stimLog.Year() == systimeNowLocal.wYear)
			{
				// If file was created to-day then seek to the end for future writes.
				CheckExtHr(m_qfistLog->Seek(li, STREAM_SEEK_END, &libPosition),
					m_qfistLog, IID_IStream);
				bNewLog = false;
			}
			else
			{	// Not created to-day, so set size to 0 and seek pointer to bof.
				ULARGE_INTEGER libSize;
				libSize.QuadPart = 0;
				CheckExtHr(m_qfistLog->SetSize(libSize), m_qfistLog, IID_IStream);
				CheckExtHr(m_qfistLog->Seek(li, STREAM_SEEK_SET, &libPosition),
					m_qfistLog, IID_IStream);
			}
		}
		// Write a text for this initialization. (It is consistent with utf8.)
		SilTime stimNow = SilTime::CurTime();
		StrAnsi sta;
		if (bNewLog)
		{
			sta.Format("%t", &stimNow);
		}
		else
		{
			sta.Format("%n%n%t", &stimNow);
		}
		// Chop off the milliseconds.
		sta.Replace(sta.Length()-4, sta.Length(), "  Initialization of application.\r\n\r\n");
		CheckExtHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch),
			m_qfistLog, IID_IStream);
	}
	catch (Throwable& thr)
	{
		StrApp strMsg(kstidNoLogging);
		strMsg.Append("\r\n");
		strMsg.Append(thr.Message());
		::OutputDebugString(strMsg.Chars());
		m_qfistLog.Clear();
		return;
	}
	catch (...)
	{
		StrApp strMsg(kstidNoLogging);
		::OutputDebugString(strMsg.Chars());
		m_qfistLog.Clear();
		return;
	}
}


/*----------------------------------------------------------------------------------------------
	Return a pointer to the logging file stream interface if it is not null.

	@param ppfist

	@return
----------------------------------------------------------------------------------------------*/
HRESULT AfApp::GetLogPointer(IStream** ppfist)
{
	ChkComOutPtr(ppfist);
	if (!m_qfistLog)
		return S_FALSE;	// Logging is "optional" so returning NULL is all right.
	IStreamPtr qfist(m_qfistLog);
	*ppfist = qfist.Detach();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Pass the idle message on to the main window.
----------------------------------------------------------------------------------------------*/
void AfApp::OnIdle()
{
	// Since we can start up as a server for the explorer, we can't always count on having
	// a window. We need to call OnIdle on all windows to keep them updated, but we must
	// call it first on the current window. Failing to call it first on the current window
	// can produce some very subtle timing problems causing a crash. For example, when we
	// were iterating up through the windows, if you opened two document windows and in the
	// second window deleted an entry, then undeleted it, then deleted a character from the
	// title, we would get a crash in the final assert of ActionHandler::CleanUpRedoActions
	// while the second window was checking the enable status on the format combobox.
	if (m_qafwCur)
		m_qafwCur->OnIdle();
	for (int iwnd = m_vqafw.Size(); --iwnd >= 0; )
	{
		if (m_qafwCur.Ptr() != m_vqafw[iwnd])
			m_vqafw[iwnd]->OnIdle();
	}
}

/*----------------------------------------------------------------------------------------------
	Return the time at which this application should stop functioning.
	The default is not until AD3000!

	NOTE: If you override this to something that might fire, you need to define a string
	resource kstidDropDead with a suitable message.

	@return
----------------------------------------------------------------------------------------------*/
SilTime AfApp::DropDeadDate()
{
	return SilTime(3000, 1, 1);
}


/*----------------------------------------------------------------------------------------------
	Clean up the application.
----------------------------------------------------------------------------------------------*/
void AfApp::CleanUp()
{
	// We've already received WM_QUIT, we can't get any more WM_COMMAND, so this is safe;
	// and it seems to help W98 cleanup problems if we release the command handlers before
	// we get to the AfApp destructor.
	m_cex._BuryAllHandlers();
}


/*----------------------------------------------------------------------------------------------
	Decide whether we're allowed to quit (ask the user if appropriate).
	This is an application-wide test. If you want to close some windows in an application
	while keeping others open, you'll need to override the Quit() method.

	@param fForce Flag whether to force the application to quit.

	@return True.  Override this method if you ever want anything else.
----------------------------------------------------------------------------------------------*/
bool AfApp::FQueryQuit(bool fForce)
{
	return true;
}


/*----------------------------------------------------------------------------------------------
	The main loop.

	@return The exit value associated with a WM_QUIT message.
----------------------------------------------------------------------------------------------*/
int AfApp::Loop()
{
	MSG msg;

	for (;;)
	{
		try
		{
			if (!m_fQuit)
				TopOfLoop();

			if (FDispatchNextCmd())
				continue;

			// Handle system events.
			if (!FGetNextMessage(&msg))
				return msg.wParam;

			// Translate accelerator keys. We can only do this if we have a main window,
			// since the menu manager which does the translation is part of it.
			if (m_qafwCur && !FTransAccel(&msg))
				continue;

			// Translate messages.
			if (!FTransMsg(&msg))
				continue;

			// Dispatch messages.
			if (!FDispatchMsg(&msg))
				EnqueueCid(kcidIdle);
		}
		catch (Throwable & thr)
		{
			thr; // Avoid a stupid warning.
			// TODO ShonK: Notify the user of this error.
		}
		catch (...)
		{
			// TODO ShonK: What should we do if some other exception was thrown?
			throw;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Default top of main loop handling.
----------------------------------------------------------------------------------------------*/
void AfApp::TopOfLoop()
{
}


/*----------------------------------------------------------------------------------------------
	This method can be called by message handler functions that know they do not do anything
	that would require OnIdle to be called. This is mainly useful for OnTimer methods or methods
	that frequently get called.
----------------------------------------------------------------------------------------------*/
void AfApp::SuppressIdle()
{
	if (s_cmsgSinceIdle > 0)
		s_cmsgSinceIdle--;
}


/*----------------------------------------------------------------------------------------------
	Get the next message from the OS.

	@param pmsg Pointer to a data structure for returning the message.

	@return False if we should exit the main event loop (WM_QUIT message was retrieved),
					otherwise true.
----------------------------------------------------------------------------------------------*/
bool AfApp::FGetNextMessage(MSG * pmsg)
{
	if (s_cmsgSinceIdle && !PeekMessage(pmsg, NULL, 0, 0, PM_NOREMOVE))
	{
		s_cmsgSinceIdle = 0; // no real messages since OnIdle
		OnIdle();
	}

	BOOL fRet = ::GetMessage(pmsg, NULL, 0, 0) != 0;

	// ENHANCE JohnT (version 2?): figure why we get this message, and if we ever do anything with
	// it, we may need to remove this. At present our app does nothing with it, but we receive
	// it during idle time and it triggers spurious computation to re-enable buttons.
	if (pmsg->message != 280) // WM_IME_REPORT is somehow not in our include path!
		s_cmsgSinceIdle++;
#if 0
	if (s_cmsgSinceIdle == 1)
	{
		StrAnsi staMsg;
		staMsg.Format("First non-idle message to window %d is %d\n",
			(int)m_hwnd, (int)pmsg->message);
		::OutputDebugStringA(staMsg.Chars());
	}
#endif
	return fRet;
}


/*----------------------------------------------------------------------------------------------
	Translate the message.  This handles translating WM_KEY events to WM_CHAR.

	@param pmsg Pointer to the message information.

	@return True if the message should be dispatched, otherwise false since the message has
					already been handled.
----------------------------------------------------------------------------------------------*/
bool AfApp::FTransMsg(MSG * pmsg)
{
	AssertPtr(pmsg);

	::TranslateMessage(pmsg);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Dispatch the message.

	@param pmsg Pointer to the message information.

	@return True if there was something to do; false otherwise.
----------------------------------------------------------------------------------------------*/
bool AfApp::FDispatchMsg(MSG * pmsg)
{
	AssertPtr(pmsg);

	::DispatchMessage(pmsg);
	return pmsg->message != WM_TIMER;
}


/*----------------------------------------------------------------------------------------------
	This static method returns the current key state.

	@param fAsync Flag whether to get the asynchronous key state via GetAsyncKeyState.

	@return the current key state
----------------------------------------------------------------------------------------------*/
uint AfApp::GrfmstCur(bool fAsync)
{
	short (WINAPI *pfnT)(int);
	uint grfmst;

	pfnT = fAsync ? &GetAsyncKeyState : &GetKeyState;
	grfmst &= 0;
	if ((*pfnT)(VK_CONTROL) < 0)
		grfmst |= kfmstCtrl;
	if ((*pfnT)(VK_SHIFT) < 0)
		grfmst |= kfmstShift;
	if ((*pfnT)(VK_MENU) < 0)
		grfmst |= kfmstAlt;
	if ((*pfnT)(VK_LBUTTON) < 0)
		grfmst |= kfmstLBtn;
	if ((*pfnT)(VK_RBUTTON) < 0)
		grfmst |= kfmstRBtn;
	if ((*pfnT)(VK_MBUTTON) < 0)
		grfmst |= kfmstMBtn;

	return grfmst;
}


/*----------------------------------------------------------------------------------------------
	Return the menu manager of the current active window if there is one.

	@param ppmum Address of a pointer to an AfMenuMgr (used to instantiate menu managers when
			there's no AfApp or AfMainWnd in sight).
	@return pointer to the menu manager
----------------------------------------------------------------------------------------------*/
AfMenuMgr * AfApp::GetMenuMgr(AfMenuMgr ** ppmum)
{
	// If the caller provides a value, use it.
	if (ppmum && *ppmum)
		return *ppmum;
	// AI: !s_papp can happen in the DbServices module, when there is not necessarily anything
	// in s_papp:
	// JT: !s_papp->m_qafwCur can happen when started up by the explorer, as no initial window
	// is opened until after control returns to the explorer so it can launch one.
	if (!s_papp || !s_papp->m_qafwCur)
	{
		if (!ppmum)
		{
			return NULL;
		}
		else
		{
			*ppmum = NewObj AfMenuMgr(NULL);
			// Finish initializing so it will display menu checkmarks properly.
			(*ppmum)->LoadToolBar(kridTBarStd);
			return *ppmum;
		}
	}
	AssertObj(s_papp->m_qafwCur);
	return s_papp->m_qafwCur->GetMenuMgr();
}


/*----------------------------------------------------------------------------------------------
	Tries to launch the specified hot link, and handles any errors that may occur.
	Parameters are as for ::ShellExecute()
----------------------------------------------------------------------------------------------*/
bool AfApp::LaunchHL(HWND hwnd, LPCTSTR pszOperation, LPCTSTR pszFile, LPCTSTR pszParameters,
	LPCTSTR pszDirectory, int nShowCmd)
{
	int ridError = AfUtil::Execute(hwnd, pszOperation, pszFile, pszParameters, pszDirectory,
		nShowCmd);
	if (!ridError)
		return true;

	StrApp strFilename;
	StrApp strTitle;
	StrApp str;
	StrApp strError;
	strError.Load(ridError);
	if (ridError == kstidErrorNoAssoc)
	{
		// No application is associated with the given file name extension.
		strTitle.Load(kstidExtLinkFileAssociation);
		strFilename =
			_T("Basic_Tasks/Creating_Hyperlinks/Unassociated_External_File_Extension.htm");
		str.Load(kstidHLErrorMsg3);
	}
	else
	{
		strTitle.Load(kstidHLErrorTitle);
		strFilename = _T("Basic_Tasks/Creating_Hyperlinks/Broken_External_Link.htm");
		if (ridError == kstidErrorFileNotFound)
			str.Load(kstidHLErrorMsg2);
		else
			str.Load(kstidHLErrorMsg);
	}
	StrApp strFolder(pszFile);
	StrApp strFileOnly(pszFile);
	StrAppBuf strbFile(pszFile);

	int i = strbFile.ReverseFindCh('\\');
	if (i > 0)
	{
		strFolder = strFolder.Left(i);
		strFileOnly = strFileOnly.Right(strFileOnly.Length() - i - 1);
	}

	StrApp strBody;
	strBody.Format(str.Chars(), strFileOnly.Chars(), strFolder.Chars(), strError.Chars());

	// Enable display of a help page from a non-dialog context
	StrApp strHelpUrl(Papp()->GetHelpFile());  // path
	strHelpUrl.Append("::/");
	strHelpUrl.Append(strFilename);
	AfMainWndPtr qafwTop = Papp()->GetCurMainWnd();
	qafwTop->SetFullHelpUrl(strHelpUrl.Chars());

	int nRet = ::MessageBox(qafwTop->Hwnd(), strBody.Chars(), strTitle.Chars(),
		MB_ICONEXCLAMATION | MB_YESNO | MB_HELP);
	if (IDYES == nRet)
	{
		Cmd cmd;
		cmd.m_cid = kcidExtLink;
		qafwTop->CmdExternalLink(&cmd);
	}
	qafwTop->ClearFullHelpUrl();

	return false;
}

/*----------------------------------------------------------------------------------------------
	Changes the top-level window that currently has the focus.

	@param pafw Pointer to a top-level window object.
----------------------------------------------------------------------------------------------*/
void AfApp::SetCurrentWindow(AfMainWnd * pafw)
{
	AssertObj(pafw);
	m_qafwCur = pafw;
}


/*----------------------------------------------------------------------------------------------
	Add the specified window to the vector of top-level windows.

	@param pafw Pointer to a top-level window object.
----------------------------------------------------------------------------------------------*/
void AfApp::AddWindow(AfMainWnd * pafw)
{
	AssertObj(pafw);

	m_vqafw.Push(pafw);
	SetCurrentWindow(pafw);
}


/*----------------------------------------------------------------------------------------------
	Remove the specified window from the vector of top-level windows.

	@param pafw Pointer to a top-level window object.
----------------------------------------------------------------------------------------------*/
void AfApp::RemoveWindow(AfMainWnd * pafw)
{
	AssertObj(pafw);

	for (int iwnd = m_vqafw.Size(); --iwnd >= 0; )
	{
		if (m_vqafw[iwnd] == pafw)
		{
			RemoveCmdHandler(m_vqafw[iwnd], 1);
			m_vqafw[iwnd].Clear();
			m_vqafw.Delete(iwnd);
			if (m_vqafw.Size() == 0)
				m_qafwCur.Clear();
			if (m_vqafw.Size() == 0 && m_cunkExport == 1 && m_dwRegister != 0)
			{
				IRunningObjectTablePtr qrot;
				if (SUCCEEDED(::GetRunningObjectTable(0, &qrot)))
				{
					qrot->Revoke(m_dwRegister);
					m_dwRegister = 0;
				}
			}
			// If that was the last top-level window that was open, and we don't have any
			// objects exported to other processes, shut down the application.
			// (See comments on m_cunkExport. I don't like this approach much--ideally
			// the ModuleEntry reference counts should do it--but I don't have a better idea.)
			if (m_vqafw.Size() == 0 && m_cunkExport == 0)
			{
				Assert(_CrtCheckMemory());
				PostQuitMessage(0);
			}
			return;
		}
	}

	Assert(false); // This should never happen.
}


/*----------------------------------------------------------------------------------------------
	Enable or disable all top-level windows.

	@param fEnable Flag whether to enable (true) or disable (false) all top-level windows.
----------------------------------------------------------------------------------------------*/
void AfApp::EnableMainWindows(bool fEnable)
{
	// This should allow nesting. In other words, calling EnableMainWindows(false) twice
	// should require two calls to EnableMainWindows(true) before the top level windows
	// are actually enabled. An example of where this was a problem was the Tools/Options
	// dialog, which could open a PossListChooser dialog. Before, when you closed the
	// PossListChooser, you could select the main window. This takes care of that problem.
	static int s_iEnableLevel = 0;
	if (!fEnable)
		s_iEnableLevel--;
	else if (++s_iEnableLevel != 0)
		return;

	int cwnd = m_vqafw.Size();
	for (int iwnd = 0; iwnd < cwnd; iwnd++)
		m_vqafw[iwnd]->EnableWindow(fEnable);
}


/*----------------------------------------------------------------------------------------------
	Decrement the count of objects (currently, typically FwTool objects) that this application
	has made available to other processes.  If the count goes to zero, post a WM_QUIT message.
----------------------------------------------------------------------------------------------*/
void AfApp::DecExportedObjects()
{
	long cunkRemaining = ::InterlockedDecrement(&m_cunkExport);
	Assert(cunkRemaining >= 0);
	if (cunkRemaining == 0 && m_vqafw.Size() == 0)
	{
		// We can't post a regular window message, because all our windows are closed.
		::PostQuitMessage(0);
	}
}


/*----------------------------------------------------------------------------------------------
	Set the FieldWorks root directories.
----------------------------------------------------------------------------------------------*/
void AfApp::SetFwPaths()
{
	m_strFwDataPath.Assign(DirectoryFinder::FwRootDataDir().Chars());
	m_strFwCodePath.Assign(DirectoryFinder::FwRootCodeDir().Chars());
}

/*----------------------------------------------------------------------------------------------
	Show the help file for the application. If pszPage is not NULL, we want to open a
	specific page within the help file.

	@param pszPage String identifying the specific page desired, or NULL.

	@return True if successful, false if the help file does not exist.
----------------------------------------------------------------------------------------------*/
bool AfApp::ShowHelpFile(const achar * pszPage)
{
	AssertPszN(pszPage);
	const achar * pszHelp = GetHelpFile();
	if (!pszHelp)
		return false;

	StrAppBufPath strbpHelp;

	strbpHelp.Assign(pszHelp);
	if (pszPage)
	{
		strbpHelp.Append(_T("::/"));
		strbpHelp.Append(pszPage);
	}
	HtmlHelp(::GetDesktopWindow(), strbpHelp.Chars(), HH_DISPLAY_TOPIC, NULL);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Show the specified training file for the application.

	@param pFilespec
	@return True if successful, false if the help file does not exist.
----------------------------------------------------------------------------------------------*/
bool AfApp::ShowTrainingFile(const achar * pszFilespec)
{
	AssertPsz(pszFilespec);

	// Set the FieldWorks code root directory.
	StrApp strTutor = GetFwCodePath();
	strTutor.Append(pszFilespec);

	StrApp strFile(strTutor.Chars());
	int ridError = AfUtil::Execute(NULL, _T("open"), strFile.Chars(), NULL, NULL, SW_SHOW);
	if (ridError)
	{
		StrApp str(kstidTrainErrorMsg);
		StrApp str2(kstidTrainErrorTitle);
		StrApp str3(ridError);

		StrApp strError;
		strError.Format(str.Chars(), strFile.Chars());
		strError.Append(" ");
		strError.Append(str3);
		::MessageBox(NULL, strError.Chars(), str2.Chars(), MB_OK);
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Go through the list of open top-level windows in z-order and insert any window that belongs
	to this application into the vector. Minimized top-level windows do not get added to the
	vector unless fAllWindows is true.

	@param vhwnd Reference to a vector of window handles for returning results.
	@param fAllWindows Flag whether minimized windows get added to the output vector.
----------------------------------------------------------------------------------------------*/
void AfApp::_GetZOrders(Vector<HWND> & vhwnd, bool fAllWindows)
{
	vhwnd.Clear();

	HWND hwnd = ::GetNextWindow(m_qafwCur->Hwnd(), GW_HWNDFIRST);
	int cwnd = m_vqafw.Size();
	int cwndT = 0;
	while (hwnd)
	{
		for (int iwnd = 0; iwnd < cwnd; iwnd++)
		{
			if (m_vqafw[iwnd]->Hwnd() == hwnd)
			{
				++cwndT;
				if (fAllWindows || !::IsIconic(hwnd))
				{
					vhwnd.Push(hwnd);
					break;
				}
			}
		}
		if (cwndT == cwnd)
			break; // Exit if all our windows are accounted for.
		hwnd = ::GetNextWindow(hwnd, GW_HWNDNEXT);
	}
}


/*----------------------------------------------------------------------------------------------
	Cascade all the top-level windows.

	@param pcmd

	@return
----------------------------------------------------------------------------------------------*/
bool AfApp::CmdWndCascade(Cmd * pcmd)
{
	// ENHANCE DarrellZ: Make this work better for multiple monitors.

	int dzpCaption = ::GetSystemMetrics(SM_CYCAPTION) + ::GetSystemMetrics(SM_CYSIZEFRAME);
	Rect rcScreen;
	::SystemParametersInfo(SPI_GETWORKAREA, 0, &rcScreen, 0);

	int width = rcScreen.Width() * 2 / 3;
	int height = rcScreen.Height() * 2 / 3;
	Rect rc(rcScreen);
	rc.right = rc.left + width;
	rc.bottom = rc.top + height;

	Vector<HWND> vhwnd;
	_GetZOrders(vhwnd);

	HDWP hdwp = ::BeginDeferWindowPos(vhwnd.Size());
	if (!hdwp)
		return false;
	for (int iwnd = vhwnd.Size(); --iwnd >= 0; )
	{
		// REVIEW DarrellZ: If we don't restore the window first, it isn't handled correctly
		// if it is maximized. Is there any better to do this? This is kind of ugly, because
		// first the window gets restored, then it gets moved to the real final position.
		::ShowWindow(vhwnd[iwnd], SW_SHOWNOACTIVATE);

		hdwp = ::DeferWindowPos(hdwp, vhwnd[iwnd], NULL, rc.left, rc.top, rc.Width(),
			rc.Height(), SWP_NOACTIVATE);
		rc.Offset(dzpCaption, dzpCaption);
		if (rc.bottom > rcScreen.bottom || rc.right > rcScreen.right)
		{
			// When a window has hit the bottom or right, start over at the upper left.
			rc.top = 0;
			rc.bottom = rc.top + height;
			rc.left = 0;
			rc.right = rc.left + width;
		}
		AfGfx::EnsureVisibleRect(rc);
	}
	::EndDeferWindowPos(hdwp);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Tile all the top-level windows so that they all have the same height and their width is the
	width of the screen.

	Review (SharonC): Should we get rid of our custom implementation and just use the
	system command?

	@param pcmd

	@return
----------------------------------------------------------------------------------------------*/
bool AfApp::CmdWndTileHoriz(Cmd * pcmd)
{
	// ENHANCE DarrellZ: Make this work better for multiple monitors.

	Rect rcWork;
	::SystemParametersInfo(SPI_GETWORKAREA, 0, &rcWork, 0);

	Vector<HWND> vhwnd;
	_GetZOrders(vhwnd);

	Rect rc(rcWork);
	rc.top = rc.bottom - rc.Height() / vhwnd.Size();

	if (rc.Height() < kdypMin)
	{
		StrAppBuf strbMsg(kstidMsgCannotTile);
		StrAppBuf strbApp(GetAppNameId());
		MessageBox(m_qafwCur->Hwnd(), strbMsg.Chars(), strbApp.Chars(),
			MB_OK | MB_ICONINFORMATION);

		// Use the system command.
//		::TileWindows(NULL, MDITILE_HORIZONTAL, NULL, vhwnd.Size(), &(vhwnd[0]));

		return true;
	}

	HDWP hdwp = ::BeginDeferWindowPos(vhwnd.Size());
	if (!hdwp)
		return false;
	for (int iwnd = vhwnd.Size(); --iwnd >= 0; )
	{
		// REVIEW DarrellZ: If we don't restore the window first, it isn't handled correctly
		// if it is maximized. Is there any better to do this? This is kind of ugly, because
		// first the window gets restored, then it gets moved to the real final position.
		::ShowWindow(vhwnd[iwnd], SW_SHOWNOACTIVATE);

		if (iwnd == 0)
			rc.top = rcWork.top;
		hdwp = ::DeferWindowPos(hdwp, vhwnd[iwnd], NULL, rc.left, rc.top, rc.Width(),
			rc.Height(), SWP_NOACTIVATE);
		rc.Offset(0, -rc.Height());
	}
	::EndDeferWindowPos(hdwp);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Tile all the top-level windows so that they all have the same width and their height is the
	height of the screen.

	Review (SharonC): Should we get rid of our custom implementation and just use the
	system command?

	@param pcmd

	@return
----------------------------------------------------------------------------------------------*/
bool AfApp::CmdWndTileVert(Cmd * pcmd)
{
	// ENHANCE DarrellZ: Make this work better for multiple monitors.

	Rect rcWork;
	::SystemParametersInfo(SPI_GETWORKAREA, 0, &rcWork, 0);

	Vector<HWND> vhwnd;
	_GetZOrders(vhwnd);

	Rect rc(rcWork);
	rc.left = rc.right - rc.Width() / vhwnd.Size();

	if (rc.Width() < kdxpMinClient + kdxpMinViewBar + RecMainWnd::kdxpSplitter)
	{
		StrAppBuf strbMsg(kstidMsgCannotTile);
		StrAppBuf strbApp(GetAppNameId());
		MessageBox(m_qafwCur->Hwnd(), strbMsg.Chars(), strbApp.Chars(),
			MB_OK | MB_ICONINFORMATION);

		// Use the system command.
//		::TileWindows(NULL, MDITILE_VERTICAL, NULL, vhwnd.Size(), &(vhwnd[0]));

		return true;
	}

	HDWP hdwp = ::BeginDeferWindowPos(vhwnd.Size());
	if (!hdwp)
		return false;
	for (int iwnd = vhwnd.Size(); --iwnd >= 0; )
	{
		// REVIEW DarrellZ: If we don't restore the window first, it isn't handled correctly
		// if it is maximized. Is there any better to do this? This is kind of ugly, because
		// first the window gets restored, then it gets moved to the real final position.
		::ShowWindow(vhwnd[iwnd], SW_SHOWNOACTIVATE);

		if (iwnd == 0)
			rc.left = rcWork.left;
		hdwp = ::DeferWindowPos(hdwp, vhwnd[iwnd], NULL, rc.left, rc.top, rc.Width(),
			rc.Height(), SWP_NOACTIVATE);
		rc.Offset(-rc.Width(), 0);
	}
	::EndDeferWindowPos(hdwp);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle the exit command.

	@param pcmd

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfApp::CmdFileExit(Cmd * pcmd)
{
	Quit();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Check an HRESULT generated in the course of producing an error report. If we get an error
	here we crash and burn.

	@param hr HRESULT to be checked
----------------------------------------------------------------------------------------------*/
void AfApp::CheckErrorHr(HRESULT hr)
{
	if (!FAILED(hr))
		return;
	Assert(false);
	// If this happens in non-debug mode, put up an English-only message and die.
	::MessageBox(NULL, _T("Error reporting failed"), _T("Error"),
		MB_OK | MB_ICONERROR | MB_TASKMODAL);
	AfApp::Papp()->Quit(true);
}


/*----------------------------------------------------------------------------------------------
	Handle a top-level error, typically a Throwable that has been caught; if thr is null, we
	encountered a catch "...", unlikely now we have a structured exception handler installed.

	There are several possibilities to consider:
	0. We don't have a throwable. This indicates a problem caught by "catch (...)" where we
	have no idea what went wrong.
	1. Out of memory. We do our best to report this without allocating any more, then quit,
	after saving or not as the case may be.
	2. A Throwable with a help ID of -1, the output of CheckHr. The real error information
	is in the current error info object.
	3. A Throwable resulting from a CheckHr that has not been converted into an error object,
	that is, the error was in a component called directly from EXE code. We have a stack dump,
	and may choose to display it.
	4. A Throwable resulting from a "standard exception" (divide by zero, etc.) caught by the
	exception handler and converted into a ThrowableSd. This has a stack dump but no message.
	5. Similar, but the result of calling ThrowInternal when our code detected an error.
	This may have a description and help ID already.
	6. A Throwable with a real help ID, and message, reporting an actual user error. Should
	have a real description and help ID but no stack dump.
----------------------------------------------------------------------------------------------*/
void AfApp::HandleTopLevelError(Throwable * pthr)
{
	StrUni stuDesc;
	StrApp strDesc;
	StrApp strError;
	StrApp strFatal;

	int hHelpId = pthr->HelpId();
	StrUni stuHelpPath;
	AfMainWndPtr qafwTop = s_papp->GetCurMainWnd();
	uint uStyle = MB_OK;	// This is here to avoid being skipped by the goto (error C2362).
	if (!pthr)
	{
		StrUni stuUserMsgFmt;
		stuUserMsgFmt.Load(kstidInternalError);
		// Would it be better to strip off the path?
		StrUni stuModName = ModuleEntry::GetModulePathName();
		StrUni stuMsg;
		StrUni stuSnippet1(kstidUnknExcnError);
		StrUni stuSnippet2(kstidCghtExcnError);
		stuMsg.Format(stuUserMsgFmt, stuSnippet1.Chars(), stuModName.Chars());
		stuMsg.Append(ThrowableSd::MoreSep());
		stuMsg.Append(stuSnippet2);
		stuDesc = stuMsg;
		hHelpId = khcidNoHelpAvailable;
		stuHelpPath = GetModuleHelpFilePath();
	}
	else
	{
		hHelpId = pthr->HelpId();
		HRESULT hrErr = pthr->Error();
		if (hrErr == E_OUTOFMEMORY)
		{
			// REVIEW JohnT: should we have some block in reserve to free, to be sure we can launch
			// the dialog?
			static StrApp s_strOutOfMemory(kstidOutOfMemory);
			static StrApp s_strError(kstidMiscError);
			MessageBox(NULL, s_strOutOfMemory.Chars(), s_strError.Chars(),
				MB_OK | MB_ICONERROR | MB_TASKMODAL);

			goto LCleanup;
		}
		ThrowableSd * pthsd = dynamic_cast<ThrowableSd *>(pthr);
		if (hHelpId == -1)
		{
			// All the info is in the current error object. CheckHr has already confirmed its
			// existence, we just need to retrieve it.  However, this may not be foolproof!
			IErrorInfoPtr qerrinfo;
			GetErrorInfo(0, &qerrinfo);
			if (qerrinfo)
			{
				SmartBstr sbstrT;
				CheckErrorHr(qerrinfo->GetDescription(&sbstrT));
				stuDesc = sbstrT.Chars();
				ulong uHelpId;
				CheckErrorHr(qerrinfo->GetHelpContext(&uHelpId));
				hHelpId = (int) uHelpId;
				CheckErrorHr(qerrinfo->GetHelpFile(&sbstrT));
				stuHelpPath = sbstrT.Chars();
			}
		}
		if (!stuHelpPath.Length())
		{
			// Make the message from info in the throwable itself. hHelpId is already set.
			// Assume it applies to our own help file.
			stuHelpPath = GetModuleHelpFilePath();

			// Figure the description. This is similar to code in HandleThrowable(), but
			// not quite enough to make a method, I think.
			StrUni stuMsg = pthr->Message();
			if (!stuMsg.Length())
			{
				// Come up with a default message indicating an internal error.

				// Crack the HRESULT
				LPTSTR lpstrMsgBuf;
				::FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
					NULL,
					hrErr,
					0, // smart search for useful languages
					reinterpret_cast<achar *>(&lpstrMsgBuf),
					0,
					NULL);
				StrUni stuHrMsg = lpstrMsgBuf;
				::LocalFree(lpstrMsgBuf); // Free the buffer.
				int cch = stuHrMsg.Length();
				if (cch > 1 && stuHrMsg[cch - 2] == '\r')
					stuHrMsg.Replace(cch - 2, cch, (OLECHAR *)NULL);

				// Make a standard internal error report
				StrUni stuUserMsgFmt;
				stuUserMsgFmt.Load(kstidInternalError);
				// Would it be better to strip off the path?
				StrUni stuModName = ModuleEntry::GetModulePathName();
				stuMsg.Format(stuUserMsgFmt, stuHrMsg.Chars(), stuModName.Chars());
			}
			// OK, we have a basic message (what the user will see). If we have stack dump
			// info, add it in.
			if (pthsd)
				stuDesc.Format(L"%s%s%S", stuMsg.Chars(), ThrowableSd::MoreSep(), pthsd->GetDump());
			else
				stuDesc = stuMsg;
		}
	} // got a throwable

	// At this point stuDesc contains the message, possibly including stuff after
	// ThrowableSd::MoreSep() that the user should not see. hHelpId and stuHelpPath contain the
	// info we need if the user asks for help. If there is "extra" material, copy the whole
	// message to the clipboard.
	OLECHAR * pchSep;
	pchSep = (OLECHAR*)wcsstr(stuDesc.Chars(), ThrowableSd::MoreSep());
	if (pchSep)
	{
		// Display a truncated message.
		strDesc.Assign(stuDesc.Chars(), pchSep - stuDesc.Chars());
		// TODO JohnT: once WorldPad ships, track down cause of empty messages...one appears to
		// be handling exceptions,
		// for example, do an access violation in VwTextSelection::SetSelectionProps.
		if (!strDesc.Length())
			strDesc = StrUni(kstidProgError); //"A programming error has occurred.";
		strDesc.Append(StrUni(kstidMailInstructions));
		// "\nA message with instructions on how to send it to the developers has been copied
		// to the clipboard.\n"
		//	"Paste the contents of the clipboard into a new email message or text file.");

		// Put the full version in the clipboard, with extra info.
		StrUni stuReport;
		StrUni stuModName = ModuleEntry::GetModulePathName();
		StrUni stuSupportName(kstidSupportEmail);
		StrUni stuIntro(kstidErrorEmail);
		stuIntro.Append(L"An internal error has been detected in program %s, %s\r\n"
						L"Details follow: \r\n\r\n%s\r\n");
		stuReport.Format(stuIntro.Chars(), stuSupportName.Chars(), stuModName.Chars(),
			GetModuleVersion(stuModName.Chars()).Chars(), stuDesc.Chars());
		IDataObjectPtr qdobj;
		StringDataObject::Create(const_cast<OLECHAR *>(stuReport.Chars()), &qdobj);
		if (::OleSetClipboard(qdobj) == S_OK)
		{
			ModuleEntry::SetClipboard(qdobj);
		}
	}
	else
	{
		strDesc = stuDesc;
	}
	if (strDesc.Length() == 0)
	{
		StrApp strSupportName(kstidSupportEmail);
		StrApp strMsg(kstidUnknErrorEmail);
		strDesc.Format(strMsg.Chars(), strSupportName.Chars());
	}

	// Find out whether there is full help path information by looking for the "::/" separator.
	// If there is, add a Help button to the Message box and set the full help URL.
	if (stuHelpPath.FindStr(L"::/") >= 0)
	{
		uStyle |= MB_HELP;
		StrApp strHelpUrl(stuHelpPath);
		qafwTop->SetFullHelpUrl(strHelpUrl.Chars());
	}
	strError.Load(kstidMiscError);
	strFatal.Load(kstidFatalError);
	strDesc.Append(strFatal.Chars());
	::MessageBox(qafwTop->Hwnd(), strDesc.Chars(), strError.Chars(), uStyle);
	qafwTop->ClearFullHelpUrl();	// Clear the stored Url to prevent inadvertent re-use.
	AfApp::Papp()->KillApp();

LCleanup:
	// Todo JohnT: Call a virtual method which is overridden at appropriate levels to
	// do things like aborting any active transaction.
	return;
}

/*----------------------------------------------------------------------------------------------
	Parse the command line, and store the results in m_hmcvstaCmdLine. The map key is the
	option tag. The map value is a vector that holds the values. The vector may be empty
	for cases where the option tag is all there is (e.g., -Embedding). "filename" is a reserved
	key for the case where there is no switch, but there is an argument.

	@param pszCmdLine Pointer to the command line string used to launch this application.

	@exception E_FAIL is returned for errors that do not have a specific error message defined.
				E_UNEXPECTED is returned if we have gone past the end of the buffer.
----------------------------------------------------------------------------------------------*/
void AfApp::ParseCommandLine(Pcsz pszCmdLine)
{
	// ENHANCE RandyR: Don't clear it, when we support getting programmer defined
	// switches, which will likely be placed in the map, but without content in the vectors.
	m_hmsuvstuCmdLine.Clear();
	if (_tcslen(pszCmdLine) == 0)
		return;

	int cch;
	StrUni stuKey;
	StrUni stuArg;
	Vector<StrUni> vstuArg;
	const achar * pszEnd = pszCmdLine + _tcslen(pszCmdLine);
	const achar * pszArg = pszCmdLine;
	const achar * psz = NULL;
	bool fFoundQuote = false;
	bool fFoundDoubledQuote = false;
	StrUni stuQuoteCpy;

	while (pszArg && *pszArg)	// Check for null pointer, or end of string.
	{
		pszArg += _tcsspn(pszArg, _T(" \t\r\n"));	// Remove leading whitespace.
		switch (pszArg[0])
		{
		case '-':	// Fall through.
		case '/':	// Start of option (map key).
			{
				if (vstuArg.Size())
				{
					stuKey.ToLower();
					m_hmsuvstuCmdLine.Insert(stuKey, vstuArg, true);
					stuKey.Clear();
					vstuArg.Clear();
				}
				else if (stuKey.Length())
				{
					// Found a tag in the previous pass, but it has no argument, so save it in
					// the map with a value of an empty vector, before processing current tag.
					vstuArg.Clear();
					stuKey.ToLower();
					m_hmsuvstuCmdLine.Insert(stuKey, vstuArg, true);
					stuKey.Clear();
				}
				++pszArg;
				// The user may have just put an argument right next to the marker,
				// so we need to split the tag from the argument at this point.
				// At the end of this case statement, psz will point to one character past
				// the end of the key.

				// First, check for approved multi-character tags.
				if ((pszArg + 9 <= pszEnd)
					&& (_tcsncicmp(pszArg, _T("embedding"), 9) == 0))
				{
					psz = pszArg + 9;
				}
				else if ((pszArg + 7 <= pszEnd)
					&& (_tcsncicmp(pszArg, _T("install"), 7) == 0))
				{
					psz = pszArg + 7;
				}
				else if ((pszArg + 9 <= pszEnd)
					&& (_tcsncicmp(pszArg, _T("uninstall"), 9) == 0))
				{
					psz = pszArg + 9;
				}
				else if ((pszArg + 10 <= pszEnd)
					&& (_tcsncicmp(pszArg, _T("automation"), 10) == 0))
				{
					psz = pszArg + 10;
				}
				else if ((pszArg + 4 <= pszEnd)
					&& (_tcsncicmp(pszArg, _T("help"), 4) == 0))
				{
					psz = pszArg + 4;
				}
				else if ((pszArg + 2 <= pszEnd)
					&& (_tcsncicmp(pszArg, _T("db"), 2) == 0))
				{
					psz = pszArg + 2;
				}
/* ENHANCE RandyR: Add other approved multi-character tags here.
				else if ((pszArg + 2 <= pszEnd)
					&& (_strnicmp(pszArg, "pt", 2) == 0))
				{
					psz = pszArg + 2;
				}
*/
				else if (pszArg < pszEnd)
				{
					// It is a single character tag.
					psz = pszArg + 1;
				}
				else
					ThrowHr(E_UNEXPECTED);	// Have gone past end, so drop dead.
				if (psz)
					cch = psz - pszArg;
				else
					cch = _tcslen(pszArg);
				stuKey.Assign(pszArg, cch);
				if ((stuKey == "?") || (stuKey == "h"))	// Variants of help.
					stuKey = L"help";
				break;
			}
		default:
			{
				// Argument(s) to option. There can be several.
				switch (pszArg[0])	// Inner switch.
				{
				case '"':	// Fall through.
				case '\'':	// Start of quoted argument(s) to option.
					{
						achar ch;
						//formerly - int ch;
						if (pszArg[1] == '-' || pszArg[1] == '/')
						{
							// REVIEW RandyR: What is the best exception here?,
							// The quote surrounds the entire thing,
							// as in: "-n Parts Of Speech".
							ThrowHr(E_FAIL);
						}
						stuQuoteCpy.Assign(pszArg, 1);
						ch = *pszArg;
						++pszArg;
						psz = _tcschr(pszArg, ch);	// Look for closing quote.
						while (psz && *psz)
						{
							// Check for doubled quotes.
							if (psz[1] == ch)
							{
								fFoundDoubledQuote = true;
								psz += 2;
							}
							else
								break;
							psz = _tcschr(psz, ch);	// Look for closing quote.
						}
						fFoundQuote = true;
						break;
					}
				default:
					{
						// Look for next space.
						psz = _tcspbrk(pszArg, _T(" \t\r\n"));
						break;
					}
				}	// End inner switch.
				if (psz)
				{
					cch = psz - pszArg;
					if (fFoundQuote)
					{
						fFoundQuote = false;
						++psz;	// Move past end quote mark.)
					}
				}
				else
				{
					if (fFoundQuote)
						ThrowHr(E_FAIL);	// No end quote.
					cch = _tcslen(pszArg);
				}
				stuArg.Assign(pszArg, cch);
				if (fFoundDoubledQuote)
				{
					// Remove doubled quotes.
					fFoundDoubledQuote = false;
					int i = stuArg.FindCh(stuQuoteCpy.GetAt(0));
					while (i != -1)
					{
						if (stuQuoteCpy.GetAt(0) == stuArg.GetAt(i + 1))
							stuArg.Replace(i, i + 1, L"");
						i = stuArg.FindCh(stuQuoteCpy.GetAt(0), i + 1);
					}
				}
				if ((stuKey == L"filename") && (vstuArg.Size() > 0))
					ThrowHr(E_FAIL);	// Second argument not allowed here.
				vstuArg.Push(stuArg);
				stuArg.Clear();
				// There may not be a key, in case this is the first argument,
				// as in Worldpad wanting to open a file.
				if (!stuKey.Length())
					stuKey = L"filename";
				break;
			}
		}
		pszArg = psz;
	}

	// Save final tag.
	if (stuKey.Length())
	{
		stuKey.ToLower();
		m_hmsuvstuCmdLine.Insert(stuKey, vstuArg, true);
	}
	return;
}

/*----------------------------------------------------------------------------------------------
	Get the default keyboard setting from the registry.

	@return LCID
----------------------------------------------------------------------------------------------*/
LCID AfApp::GetDefaultKeyboard()
{
	int nRet = 0;
	StrApp str; //("Keyboard Layout\\Preload");

	OSVERSIONINFO osv;
	osv.dwOSVersionInfoSize = isizeof(OSVERSIONINFO);
	::GetVersionEx(&osv);
	if (osv.dwMajorVersion >= 5)
	{
		if (FwSettings::GetString(_T("Keyboard Layout\\Preload"), NULL, _T("1"), str))
			nRet = _tcstoul(str.Chars(), NULL, 16);
	}
	else
	{
		// possibly Win98 which has a different key
		if (FwSettings::GetString(_T("Keyboard Layout\\Preload\\1"), NULL, NULL, str))
			nRet = _tcstoul(str.Chars(), NULL, 16);
	}

	return (LCID)nRet;
}

/*----------------------------------------------------------------------------------------------
	Return the default fonts for the given writing system. These may be read either from
	the writing system's member variables or from the font initialization.

	This method is here on AfApp because it is used by at least two very distinct places
	in the code.
----------------------------------------------------------------------------------------------*/
void AfApp::DefaultFontsForWs(IWritingSystem * pws, StrUni & stuDefSerif,
	StrUni & stuDefSans, StrUni & stuDefMono, StrUni & stuDefBodyFont)
{
	SmartBstr sbstr;
	CheckHr(pws->get_DefaultSerif(&sbstr));
	if (sbstr)
		stuDefSerif.Assign(sbstr.Chars());
	else
		stuDefSerif.Clear();
	CheckHr(pws->get_DefaultSansSerif(&sbstr));
	if (sbstr)
		stuDefSans.Assign(sbstr.Chars());
	else
		stuDefSans.Clear();
	CheckHr(pws->get_DefaultMonospace(&sbstr));
	if (sbstr)
		stuDefMono.Assign(sbstr.Chars());
	else
		stuDefMono.Clear();
	CheckHr(pws->get_DefaultBodyFont(&sbstr));
	if (sbstr)
		stuDefBodyFont.Assign(sbstr.Chars());
	else
		stuDefBodyFont.Clear();
}


//:>********************************************************************************************
//:>	AfDbApp methods.
//:>********************************************************************************************

static DummyFactory g_fact(_T("SIL.AppCore.AfDbApp"));

/*----------------------------------------------------------------------------------------------
	We close all windows (open on the current notebook) from the top down (via Z-order) and
	stop at the point one fails.  It will look to the user as if we have quit, but we don't
	actually do it unless all exported objects have closed. Doing this will cause an actual
	quit when the last closes.	We first get a sorted list (via Z-order) of windows we want to
	close and then work through this list starting at our current window and going down the
	Z-order.

	@param fForce if true then Quit, otherwise ignore quit.
----------------------------------------------------------------------------------------------*/
void AfDbApp::Quit(bool fForce)
{
	if (m_fQuit || FQueryQuit(fForce) || fForce)
	{
		m_fQuit = true;
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qafwCur.Ptr());
		// If DB initialization fails, we may get here without a valid pointer.
		if (!prmw)
			return;
		AfLpInfo * plpi = prmw->GetLpInfo();
		AssertPtr(plpi);
		HVO hvoRoot = prmw->GetRootObj();
		Assert(hvoRoot);
		// It should look to the user as if we have quit, but don't actually do it unless
		// all exported objects have closed. Just close all our windows. If we are not
		// exporting an object, this will cause an actual quit when the last closes.

		// JohnW wants to close windows from the top down and stop at the point one fails.
		// We want to go through all windows starting at our current window and going down the
		// Z-order, however, if we do this directly, when we delete a window, the Z-order gets
		// messed up and returns NULL before going through remaining windows. Thus the only way
		// to do this is to first get a sorted list (via Z-order) of windows we want to close
		// and then work through this list.
		int cwnd = m_vqafw.Size();
		Vector<HWND> vhwnd;
		_GetZOrders(vhwnd, true);

		// Now that we have a sorted list, process the list.
		for (int ihwnd = 0; ihwnd < cwnd; ++ihwnd)
		{
			prmw = dynamic_cast<RecMainWnd *>(AfWnd::GetAfWnd(vhwnd[ihwnd]));
			Assert(prmw);
			AfLpInfo * plpiT = prmw->GetLpInfo();
			AssertPtr(plpiT);
			if (plpi == plpiT && prmw->GetRootObj() == hvoRoot)
			{
				// The window is showing the same object from the same language project.
				if (prmw->IsOkToChange())
					::SendMessage(vhwnd[ihwnd], WM_CLOSE, 0, 0);
				else
				{
					// If we can't close a window, stop closing at this point.
					// Make sure the window is not minimized and make it active.
					::ShowWindow(vhwnd[ihwnd], SW_RESTORE);
					m_fQuit = false;
					return;
				}
			}
			else
				// Don't close the application if there are other language projects open.
				m_fQuit = false;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Fully refresh everything related to the database containing the specified language project.
	@param plpi Pointer to the AfLpInfo for the language project we want to synchronize.
----------------------------------------------------------------------------------------------*/
bool AfDbApp::FullRefresh(AfLpInfo * plpi)
{
	WaitCursor wc;

	AfDbInfo * pdbi = plpi->GetDbInfo();
	Vector<AfLpInfoPtr> vlpi = pdbi->GetLpiVec();
	int cwnd = m_vqafw.Size();
	int iwnd;
	// LoadViewBar needs to have various caches in AfLpInfo updated before being called.
	// LoadViewBar calls OnViewBarChange, which closes any open editors. However, when
	// closing an open AfDeFeTags editor, we check the CustViewDa cache to see if we've
	// made changes, and writes out the changes. If the CustViewDa is cleared while the
	// editor is open, this results in doubling the items in the list when it is closed.
	// So to avoid this cyclical problem, we need to close all editors prior to clearing
	// the view cache. Of course, this defeats (or complicates) our desire to keep the
	// same field and possibly chooser open following a refresh.
	// A further complication is that a field, such as Name in list editor which is required
	// will test for valid data prior to deleting the editors. This test occurs in
	// OnTreeBarChange. This test must also be done prior to clearing the cache, or it
	// will think the field is lacking required information. To avoid this problem, we
	// delete all field editors prior to clearing the cache.
	RecMainWnd * prmw;
	SyncInfo sync(ksyncFullRefresh, 0, 0);
	for (iwnd = 0; iwnd < cwnd; ++iwnd)
	{
		prmw = dynamic_cast<RecMainWnd *>(m_vqafw[iwnd].Ptr());
		Assert(prmw);
		if (prmw->GetLpInfo()->GetDbInfo() == pdbi)
			if (!prmw->PreSynchronize(sync))
				return false;
	}

	// Refresh data stored in AfDbInfo.
	int grdbi =	kfdbiSortSpecs | kfdbiFilters | kfdbiMetadata | kfdbiUserViews |
		kfdbiEncFactories | kfdbiLpInfo;
	if (!pdbi->FullRefresh(grdbi))
		return false;

	// Now refresh all of the windows associated with the database.
	for (int iwnd = 0; iwnd < cwnd; ++iwnd)
	{
		prmw = dynamic_cast<RecMainWnd *>(m_vqafw[iwnd].Ptr());
		Assert(prmw);
		if (prmw->GetLpInfo()->GetDbInfo() == pdbi)
			if (!prmw->FullRefresh())
				return false;
	}

	// Make sure the focus gets set to the right window.
	::SetFocus(GetCurMainWnd()->Hwnd());
	return true;
}


/*----------------------------------------------------------------------------------------------
	Reads any changes from Synch$ table since the last synchronize made by other applications.
	Synchronize all windows that use a given database. Store the latest id to which we have
	synchronized.
	@param plpi Pointer to the AfLpInfo for the language project we want to synchronize.
----------------------------------------------------------------------------------------------*/
bool AfDbApp::DbSynchronize(AfLpInfo * plpi)
{
	IOleDbEncapPtr qode; // Declare before qodc.
	IOleDbCommandPtr qodc;
	unsigned long cbSpaceTaken;
	ComBool fMoreRows;
	ComBool fIsNull = true;
	plpi->GetDbInfo()->GetDbAccess(&qode);
	int nLastSync = plpi->GetLastSync();
	GUID guidSync = plpi->GetSyncGuid();
	CheckHr(qode->CreateCommand(&qodc));
	// Get a list (oldest to newest) of any sync changes made by other applications
	// since my last synchronization.
	StrUni stuQuery =
		L"select Id, Msg, ObjId, ObjFlid from sync$"
		L"  where id > ? and LpInfoId <> ?"
		L"  order by id desc";
	CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
		(ULONG *) &nLastSync, sizeof(int)));
	CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_GUID,
		(ULONG *)&guidSync, isizeof(GUID)));
	CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	Vector<SyncInfo> vsync;
	Set<SyncInfo> ssync;
	SyncInfo sync;
	bool fFullRefresh = false;
	// Store the resulting synch records in vsync, eliminating any duplicates.
	// Note: I couldn't find any way to use 'distinct' in a SQL query to eliminate
	// duplicates without altering the order, so we'll do it here.
	while (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&nLastSync),
			isizeof(int), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(&sync.msg),
			isizeof(int), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(3, reinterpret_cast <BYTE *>(&sync.hvo),
			isizeof(int), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(4, reinterpret_cast <BYTE *>(&sync.flid),
			isizeof(int), &cbSpaceTaken, &fIsNull, 0));
		if (!ssync.IsMember(sync))
		{
			ssync.Insert(sync);
			vsync.Push(sync);
			// Catch anything that will require a full refresh, since it
			// will supersede any other sync message.
			if (sync.msg == ksyncFullRefresh || sync.msg == ksyncWs)
			{
				fFullRefresh = true;
				break;
			}
		}
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	// This must be cleared to free the pointer for later uses in various submethods.
	qodc.Clear();
	qode.Clear();
	int csync = vsync.Size();
	if (!csync)
		return true; // No changes.

	plpi->SetLastSync(nLastSync); // Store the latest sync id.

	// If anything requires a full refresh, ignore everything else.
	if (fFullRefresh)
		return Synchronize(sync, plpi);

	// Process each sync message from other applications.
	//   Enhance: We should try to make this smart enough to skip multiple updates for
	//   the same field and if something requires complete redraws, skip all other updates.
	//   Using a map instead of a vector would be one way to avoid total duplicates.
	for (int isync = 0; isync < csync; ++isync)
	{
		if (!Synchronize(vsync[isync], plpi))
			return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	PreSynchronize as a separate step. This is currently used by UndoRedo, which needs to
	do the cleaning up and saving the scroll position before doing the actual Undo/Redo.
	@param sync -> The information describing a given change.
	@param plpi Pointer to the AfLpInfo for the language project we want to synchronize.
----------------------------------------------------------------------------------------------*/
bool AfApp::PreSynchronize(SyncInfo & sync, AfLpInfo * plpi)
{
	RecMainWnd * prmw;
	int cwnd = m_vqafw.Size();
	for (int iwnd = 0; iwnd < cwnd; ++iwnd)
	{
		prmw = dynamic_cast<RecMainWnd *>(m_vqafw[iwnd].Ptr());
		Assert(prmw);
		if (prmw->GetLpInfo() == plpi)
			if (!prmw->PreSynchronize(sync))
				return false;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Synchronize all windows that use a given database for a given change. In addition to
	synchronizing changes made by other applications, this can also be used within a given
	application to update windows and various caches, as needed.
	@param sync -> The information describing a given change.
	@param plpi Pointer to the AfLpInfo for the language project we want to synchronize.
----------------------------------------------------------------------------------------------*/
bool AfDbApp::Synchronize(SyncInfo & sync, AfLpInfo * plpi)
{
	if (sync.msg == ksyncFullRefresh || sync.msg == ksyncCustomField)
		return FullRefresh(plpi); // Handle full refresh.

	WaitCursor wc;

	int cwnd = m_vqafw.Size();
	int iwnd;
	// LoadViewBar needs to have various caches in AfLpInfo updated before being called.
	// LoadViewBar calls OnViewBarChange, which closes any open editors. However, when
	// closing an open AfDeFeTags editor, we check the CustViewDa cache to see if we've
	// made changes, and writes out the changes. If the CustViewDa is cleared while the
	// editor is open, this results in doubling the items in the list when it is closed.
	// So to avoid this cyclical problem, we need to close all editors prior to clearing
	// the view cache. Of course, this defeats (or complicates) our desire to keep the
	// same field and possibly chooser open following a refresh.
	// A further complication is that a field, such as Name in list editor which is required
	// will test for valid data prior to deleting the editors. This test occurs in
	// OnTreeBarChange. This test must also be done prior to clearing the cache, or it
	// will think the field is lacking required information. To avoid this problem, we
	// delete all field editors prior to clearing the cache.

	RecMainWnd * prmw;
	for (iwnd = 0; iwnd < cwnd; ++iwnd)
	{
		prmw = dynamic_cast<RecMainWnd *>(m_vqafw[iwnd].Ptr());
		Assert(prmw);
		if (prmw->GetLpInfo() == plpi)
			if (!prmw->PreSynchronize(sync))
				return false;
	}

	// Have the LpInfo process the sync message.
	if (!plpi->Synchronize(sync))
		return false;

	// Have each window for the same language project process each sync message.
	for (int iwnd = 0; iwnd < cwnd; ++iwnd)
	{
		prmw = dynamic_cast<RecMainWnd *>(m_vqafw[iwnd].Ptr());
		Assert(prmw);
		if (prmw->GetLpInfo() == plpi)
			if (!prmw->Synchronize(sync))
				return false;
	}

	// Make sure the focus gets set to the right window.
	::SetFocus(GetCurMainWnd()->Hwnd());

	return true;
}

/*----------------------------------------------------------------------------------------------
	Goes through all windows and run SaveEdit for each open edit box.

	@param prmw Pointer to main window.

	@return
----------------------------------------------------------------------------------------------*/
bool AfDbApp::SaveAllWndsEdits(AfDbInfo * pdbi)
{
	AssertPtr(pdbi);

	Vector<AfMainWndPtr> & vqafw = GetMainWindows();
	int cqafw = vqafw.Size();
	for (int iqafw = 0; iqafw < cqafw; ++iqafw)
	{
		RecMainWndPtr qrmw = dynamic_cast<RecMainWnd *>(vqafw[iqafw].Ptr());
		AssertObj(qrmw);
		AfMdiClientWndPtr qmdic = qrmw->GetMdiClientWnd();
		Assert(qmdic);
		AfClientRecWndPtr qrcw = dynamic_cast<AfClientRecWnd *>(qmdic->GetCurChild());
		Assert(qrcw);
		AfDeSplitChildPtr qadsc = dynamic_cast<AfDeSplitChild *>(qrcw->CurrentPane());
		if (qadsc)
		{
			AfDeFieldEditorPtr qdfe = qadsc->GetActiveFieldEditor();
			if (qdfe && pdbi == qrmw->GetLpInfo()->GetDbInfo() && !qdfe->SaveEdit())
				return false;
		}
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Goes through all windows closes all active edit boxes.

	@return true if successful
----------------------------------------------------------------------------------------------*/
bool AfDbApp::CloseAllWndsEdits()
{
	Vector<AfMainWndPtr> & vqafw = GetMainWindows();

	int cqafw = vqafw.Size();
	for (int iqafw = 0; iqafw < cqafw; ++iqafw)
	{
		RecMainWnd * prmwOther = dynamic_cast<RecMainWnd *>(vqafw[iqafw].Ptr());
		AssertObj(prmwOther);
		if (!prmwOther->CloseActiveEditors())
			return false;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Goes through all windows and see if they have valid data and are able to be closed if need
	be.  This is usually called before intering a dialog that may need to update all windows.

	@param prmw Pointer to main window.
	@param fChkReq

	@return
----------------------------------------------------------------------------------------------*/
bool AfDbApp::AreAllWndsOkToChange(AfDbInfo * pdbi, bool fChkReq)
{
	AssertPtr(pdbi);

	Vector<AfMainWndPtr> & vqafw = GetMainWindows();
	int cqafw = vqafw.Size();
	for (int iqafw = 0; iqafw < cqafw; ++iqafw)
	{
		RecMainWnd * prmwOther = dynamic_cast<RecMainWnd *>(vqafw[iqafw].Ptr());
		AssertObj(prmwOther);
		if ((pdbi == prmwOther->GetLpInfo()->GetDbInfo()) && !prmwOther->IsOkToChange(fChkReq))
			return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Initialize the application.
	Note: This should be called before any other database operations since this does one-time
	initialization after program installation.
----------------------------------------------------------------------------------------------*/
void AfDbApp::Init()
{
	SuperClass::Init();

	// Make sure ICU files are properly updated after installation.
	// (Can't use FwSettings here because we use HKLM)
	HKEY hk;
	long lRet = ::RegOpenKeyEx(HKEY_LOCAL_MACHINE, _T("Software\\SIL"), 0,
		KEY_ALL_ACCESS, &hk);
	if (lRet == ERROR_SUCCESS)
	{
		DWORD dwInitIcu;
		DWORD cb = isizeof(dwInitIcu);
		DWORD dwT;
		long lT;
		// Get the flag from the registry in dwInitIcu.
		lT = ::RegQueryValueEx(hk, _T("InitIcu"), NULL, &dwT, (BYTE *)&dwInitIcu, &cb);
		if (lT == ERROR_SUCCESS && dwInitIcu != 0)
		{
			StrApp strCmd(ModuleEntry::GetModulePathName());
			int iSlash = strCmd.ReverseFindCh('\\');
			Assert(iSlash > 0);
			strCmd.Replace(iSlash + 1, strCmd.Length(), _T("InstallLanguage.exe"));
			strCmd.FormatAppend(_T(" -o"));
			DWORD dwRes;
			SilUtil::ExecCmd(strCmd.Chars(), true, true, &dwRes);
			if (true)	// was having problems with icu if bad xml lang files existed
			{
				dwInitIcu = 0;
				lRet = ::RegSetValueEx(hk, _T("InitIcu"), NULL, REG_DWORD, (BYTE *)&dwInitIcu,
					isizeof(DWORD));
				Assert(lRet == ERROR_SUCCESS);
			}
		}
	}
	RegCloseKey(hk);

	// Try one-time initialization on MSDE.
	IOleDbEncapPtr qode;
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->InitMSDE(m_qfistLog, false));
	achar psz[MAX_COMPUTERNAME_LENGTH + 1];
	ulong cch = isizeof(psz);
	::GetComputerName(psz, &cch);
	StrUni stuMachine(psz);
	m_stuLocalServer.Format(L"%s\\SILFW", stuMachine.Chars());
}


/*----------------------------------------------------------------------------------------------
	User command to open a language project. The method first calls the dialog by which the
	user navigates to the desired project, and then creates a new main window for that project.

	@param pcmd Menu command (This parameter is not used in in this method.)

	@return false if dialog cancelled or incompatible with this version, otherwise returns true.
----------------------------------------------------------------------------------------------*/
bool AfDbApp::CmdFileOpenProj(Cmd * pcmd)
{
	AssertObj(pcmd);

	// Save the database. Otherwise, if we happen to open the same database after making
	// unsaved changes, we'll get a lock. Also, The file open dialog uses a separate connection
	// so to be sure we don't get into problems with SQL Audit Login/Logout, we'll save first.
	AssertPtr(m_qafwCur);
	m_qafwCur->SaveData();

	// Execute the File-Open dialog.
	FileOpenProjectInfoPtr qfopi;
	qfopi.Attach(DoFileOpenProject());
	if (!qfopi || !qfopi->m_fHaveProject)
		return true;

	StrUni stuDatabase = qfopi->m_stuDatabase;
	StrUni stuServer = qfopi->m_stuMachine;
	if (!stuServer.EqualsCI(m_stuLocalServer))
		stuServer.Append(L"\\SILFW");
	if (!CheckDbVerCompatibility(stuServer.Chars(), stuDatabase.Chars()))
		return false;

	WaitCursor wc;

	Rect rcT;
	::GetWindowRect(m_qafwCur->Hwnd(), &rcT);
	int dypCaption = ::GetSystemMetrics(SM_CYCAPTION) + ::GetSystemMetrics(SM_CYSIZEFRAME);
	rcT.Offset(dypCaption, dypCaption);
	AfGfx::EnsureVisibleRect(rcT);

	// Process new window.
	WndCreateStruct wcs;
	RecMainWndPtr qrmw;
	qrmw.Attach(CreateMainWnd(wcs, qfopi));
	AfDbInfo * pdbi = GetDbInfo(stuDatabase.Chars(), stuServer.Chars());
	AfLpInfo * plpi = pdbi->GetLpInfo(qfopi->m_hvoProj);
	qrmw->Init(plpi);
	qrmw->CreateHwnd(wcs);
	AfStatusBarPtr qstbr = qrmw->GetStatusBarWnd();
	Assert(qstbr);
	qstbr->InitializePanes();
	qrmw->UpdateStatusBar();
	::MoveWindow(qrmw->Hwnd(), rcT.left, rcT.top, rcT.Width(), rcT.Height(), true);
	qrmw->Show(m_nShow);

	wc.RestoreCursor();	// Do it now, rather than when it goes out of scope.

	bool fCancel;
	pdbi = qrmw->CheckEmptyRecords(pdbi, qfopi->m_stuProject, fCancel);
	if (fCancel)
		return true;

	pdbi->CheckTransactionKludge();

	return true;
}

/*----------------------------------------------------------------------------------------------
	Get new project to work on.

	@return Pointer to FileOpenProjectInfo, which has the results.
----------------------------------------------------------------------------------------------*/
FileOpenProjectInfo * AfDbApp::DoFileOpenProject()
{
	FileOpenProjectInfoPtr qfopi;
	qfopi.Create();

	IStreamPtr qfist;
	CheckHr(GetLogPointer(&qfist));
	SmartBstr sbstrLocalServer(GetLocalServer());
	IOpenFWProjectDlgPtr qofwpd;
	qofwpd.CreateInstance(CLSID_OpenFWProjectDlg);
	SmartBstr sbstrHelp;
	GetOpenProjHelpUrl(&sbstrHelp);
	AfMainWnd * pafw = GetCurMainWnd();
	HWND hwndMain = pafw ? pafw->Hwnd() : 0;
	StrUni stuUserWs(kstidUserWs);

	CheckHr(qofwpd->Show(qfist,
		NULL,
		sbstrLocalServer,
		stuUserWs.Bstr(),
		(DWORD)hwndMain,
		GetAllowOPPopupMenu(),
		GetOPSubitemClid(),
		sbstrHelp));

	ComBool fHaveProject;
	int nProjectId;
	SmartBstr sbstrProject;
	SmartBstr sbstrDatabase;
	SmartBstr sbstrServer;
	GUID guid;
	ComBool fHaveSubitem;
	int nSubitemId;
	SmartBstr sbstrSubitem;

	CheckHr(qofwpd->GetResults(&fHaveProject,
		&nProjectId,
		&sbstrProject,
		&sbstrDatabase,
		&sbstrServer,
		&guid,
		&fHaveSubitem,
		&nSubitemId,
		&sbstrSubitem));

	FileOpenProjectInfo * pfopi = NULL;
	if (fHaveProject)
	{
		qfopi->m_fHaveProject = fHaveProject;
		qfopi->m_stuProject = sbstrProject.Chars();
		qfopi->m_stuDatabase = sbstrDatabase.Chars();
		qfopi->m_stuMachine = sbstrServer.Chars();
		qfopi->m_hvoProj = (HVO)nProjectId;
		pfopi = qfopi;
		qfopi.Detach();
	}
	if (fHaveSubitem)
	{
		Assert(fHaveProject);
		pfopi->m_fHaveSubitem = fHaveSubitem;
		pfopi->m_hvoSubitem = (HVO)nSubitemId;
		pfopi->m_stuSubitemName = sbstrSubitem.Chars();
	}

	return pfopi;	// Note: May be NULL;
}



/*----------------------------------------------------------------------------------------------
	Looks up version of a FW database.
	@param pszSvrName A string designating the server name
	@param pszDbName A string designating the database name
	@return version number assigned by the LSDev team, or -1 if an error occurs.
----------------------------------------------------------------------------------------------*/
int AfDbApp::GetDbVersion(const OLECHAR * pszSvrName, const OLECHAR * pszDbName)
{
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG luSpaceTaken;
	IOleDbEncapPtr qode; // Declare before qodc.
	IOleDbCommandPtr qodc;
	StrUni stuSvrName;
	StrUni stuDbName;
	StrUni stuSql;
	IStreamPtr qfist;
	int nDbVer = -1; // This will be the return value. Assume the worst at first.

	try
	{
		// Connect to the database.
		stuSvrName = pszSvrName;
		stuDbName = pszDbName;

		// Get the IStream pointer for logging. NULL returned if no log file.
		CheckHr(GetLogPointer(&qfist));
		qode.CreateInstance(CLSID_OleDbEncap);
		try {
			CheckHr(qode->Init(stuSvrName.Bstr(), stuDbName.Bstr(), qfist, koltMsgBox, koltvForever));
		}
		catch (Throwable & thr) {
			::OutputDebugString(thr.Message());

			// This failure may mean SQL Server is not set up with proper security
			// authentication. You can check with SQL Server Enterprise Manager. Right+click
			// on your server and choose Properties. Click the Security tab, and check
			// Authentication. It should be SQL Server and Windows NT. If it is set to
			// Windows NT only, it will cause this error. After correcting the authentication,
			// you must stop SQL Server and restart it before this will work correctly.
			DisplayErrorInfo((IUnknown *)qode);
			return -1;
		}

		CheckHr(qode->CreateCommand(&qodc));
		stuSql.Format(L"select DbVer from Version$");
		try {
			CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
		}
		catch (Throwable & thr) {
			::OutputDebugString(thr.Message());

			StrApp str(kstiddbeNoVerTbl);
			::MessageBox(NULL, str.Chars(), NULL, MB_ICONERROR);
			return -1;
		}
		CheckHr(qodc->GetRowset(1));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (!fMoreRows)
		{
			StrApp str(kstiddbeNoDbVer);
			::MessageBox(NULL, str.Chars(), NULL, MB_ICONERROR);
			return -1;
		}

		luSpaceTaken = 0;
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&nDbVer), sizeof(int),
			&luSpaceTaken, &fIsNull, 0));
		qodc.Clear();
		if ((luSpaceTaken < sizeof(int)) || (fIsNull))
		{
			StrApp str(kstiddbeNoDbVer);
			::MessageBox(NULL, str.Chars(), NULL, MB_ICONERROR);
			return -1;
		}
	}
	catch (...)
	{
		return -1;
	}
	return nDbVer;
}

/*----------------------------------------------------------------------------------------------
	Sets version of a FW database.
	@param pszSvrName A string designating the server name
	@param pszDbName A string designating the database name
	@param nVersion Version to assign to database.
----------------------------------------------------------------------------------------------*/
void AfDbApp::SetDbVersion(const OLECHAR * pszSvrName, const OLECHAR * pszDbName, int nVersion)
{
	IOleDbEncapPtr qode; // Declare before qodc.
	IOleDbCommandPtr qodc;
	StrUni stuSvrName;
	StrUni stuDbName;
	StrUni stuSql;
	IStreamPtr qfist;

	try
	{
		// Connect to the database.
		stuSvrName = pszSvrName;
		stuDbName = pszDbName;

		// Get the IStream pointer for logging. NULL returned if no log file.
		CheckHr(GetLogPointer(&qfist));
		qode.CreateInstance(CLSID_OleDbEncap);
		CheckHr(qode->Init(stuSvrName.Bstr(), stuDbName.Bstr(), qfist, koltMsgBox,
			koltvForever));
		CheckHr(qode->CreateCommand(&qodc));
		stuSql.Format(L"update [Version$] set [DbVer]=%d", nVersion);
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
	}
	catch (Throwable& thr)
	{
		::OutputDebugString(thr.Message());
		StrApp str(kstiddbeNoVerTbl);
		::MessageBox(NULL, str.Chars(), NULL, MB_ICONERROR);
		return;
	}
	catch (...)
	{
		; // Errors should already have been dealt with
	}
}

/*----------------------------------------------------------------------------------------------
	Checks for compatibility between the application and the database. If they match then
	return true. Otherwise an appropriate error message is displayed, and the method returns
	false after the user responds to the message.
	@param pszSvrName A string designating the server name
	@param pszDbName A string designating the database name
	@return true if the app and db are compatible. false if they are incompatible (after
	raising an error message).
----------------------------------------------------------------------------------------------*/
bool AfDbApp::CheckDbVerCompatibility(const OLECHAR * pszSvrName, const OLECHAR * pszDbName)
{
	// TODO: Check that DB is not on another machine

	int dbVersion = GetDbVersion(pszSvrName, pszDbName);

	// Could not open database - calling dialog should handle this
	if (dbVersion < 0)
		return false;

	if (dbVersion == kdbAppVersion)
		return true;

	// Is user running an old version of the App against a newer database?
	if (dbVersion > kdbAppVersion)
	{
		// The DB is newer then the App, so it cannot be opened.
		StrApp strTtl(kstiddbeOldAppTtl);
		StrApp strMsg(kstiddbeOldApp);
		StrApp strFmt;
		StrApp strDbName(pszDbName);
		strFmt.Format(strMsg.Chars(), strDbName.Chars());
		MessageBox(NULL, strFmt.Chars(),strTtl.Chars(),
			MB_TASKMODAL | MB_OK | MB_ICONEXCLAMATION | MB_TOPMOST);
		return false;
	}

	StrApp strDbName(pszDbName);

	// Let user decide if database should be upgraded:
	//REVIEW (Mark B.): This piece was commented out because (1) the dialog always displayed underneith
	//its parent and (2) we see little reason to ask for confirmation since the Upgrade process
	//now does comprehensive sanity checking and works very reliably.
	//StrApp strTtl(kstiddbeOldDbTtl);
	//StrApp strMsg(kstiddbeOldDb);
	//StrApp strFmt;
	//strFmt.Format(strMsg.Chars(), strDbName.Chars(), dbVersion, kdbAppVersion);
	//if (IDNO == ::MessageBox(NULL, strFmt.Chars(), strTtl.Chars(),
	//		MB_TASKMODAL | MB_YESNO | MB_ICONQUESTION | MB_TOPMOST))
	//{
	//	return false;
	//}

	// Upgrade the database
	int nDestVersion(kdbAppVersion);
	try
	{
		IMigrateDataPtr qmd;
		qmd.CreateInstance(CLSID_MigrateData);
		IStreamPtr qfist;
		CheckHr(GetLogPointer(&qfist));
		WaitCursor wc;
		if (!SUCCEEDED(qmd->Migrate(strDbName.Bstr(), nDestVersion, qfist)))
			return false;
	}
	catch (...)
	{
		// Could not migrate for some reason:
		return false;
	}

	// Set version in database to match application version. This might have been done already!
	SetDbVersion(pszSvrName, pszDbName, kdbAppVersion);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Displays an error message by calling ErrMsgBox(). If this method is called without
	specifying a parameter the error message is a "standard" text (from kstidDbError).
	Otherwise the parameter is assumed to be an interface pointer from an interface which
	may support COM error objects and may, in that case, have set up an error object.
	If there is an error object its Description text is retrieved and presented to ErrMsgBox().
----------------------------------------------------------------------------------------------*/
void AfDbApp::DisplayErrorInfo(IUnknown * punk)
{
	HRESULT hr;
	ISupportErrorInfoPtr qsei;
	IErrorInfoPtr qei;

	if (!punk)
	{
		// No error interface, so display a standard message.
		StrApp str(kstidDbError);
		::MessageBox(NULL, str.Chars(), NULL, MB_ICONERROR);
		return;
	}
	CheckHr(punk->QueryInterface(IID_ISupportErrorInfo, (void **)&qsei));
	CheckHr(hr = qsei->InterfaceSupportsErrorInfo(IID_IOleDbEncap));
	if (hr == S_OK)
	{
		hr = GetErrorInfo(0, &qei); // can't wrap GetErrorInfo with CheckHr!
		CheckHr(hr);
		if (hr == S_FALSE)
		{
			// There was no error object.
			StrApp str(kstidDbError);
			::MessageBox(NULL, str.Chars(), NULL, MB_ICONERROR);
			return;
		}
		else
		{
			SmartBstr sbstr;
			CheckHr(qei->GetDescription(&sbstr));
			StrApp str(sbstr.Chars());
			::MessageBox(NULL, str.Chars(), NULL, MB_ICONERROR);
		}
	}
}
/*----------------------------------------------------------------------------------------------
	CleanUp the application.
----------------------------------------------------------------------------------------------*/
void AfDbApp::CleanUp()
{
	SuperClass::CleanUp();

	int cdbi = m_vdbi.Size();
	for (int idbi = 0; idbi < cdbi; idbi++)
	{
		m_vdbi[idbi]->CleanUp();
		m_vdbi[idbi].Clear();
	}
	m_vdbi.Clear();

	m_qafwCur.Clear();
	// NOTE: This has to go backwards, because the window will remove itself from this
	// vector when it handles the close message.
	for (int iwnd = m_vqafw.Size(); --iwnd >= 0; )
		::SendMessage(m_vqafw[iwnd]->Hwnd(), WM_CLOSE, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Delete a AfDbInfo pointer from m_vdbi

	@param pdbi Pointer to AfDbInfo to be deleted
----------------------------------------------------------------------------------------------*/
void AfDbApp::DelDbInfo(AfDbInfo * pdbi)
{
	for (int idbi = m_vdbi.Size(); --idbi >= 0; )
	{
		if (m_vdbi[idbi] == pdbi)
		{
			m_vdbi.Delete(idbi);// .erase(m_vdbi.begin() + idbi);
			break;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Gets the App database Version information from the app.

	@param nAppVer
	@param nErlyVer
	@param nLastVer

	@return True if successful, false if an error occurs.  Override this method if you ever want
					to get anything but false.
----------------------------------------------------------------------------------------------*/
bool AfDbApp::GetAppVer(int & nAppVer, int & nErlyVer, int & nLastVer)
{
	return false;
}


/*----------------------------------------------------------------------------------------------
	Close any windows associated with a database, save the database, clear all caches, and
	shutdown the connection to the database.
	Note: If you are closing all windows, doing some processing, then opening a new window
	again, you'll need to call IncExportedObjects() on the app prior to calling this, then
	DecExportedObjects() after you have opened the new window. Otherwise when FwTool shuts
	down from the first window, it causes a quit message to be sent which immediately shuts
	down your new window.

	@param pszDbName Name of the database to close.
	@param pszSvrName Name of the server hosting the database.
	@param fOkToClose True to close the application if there are no further connections after
		the requested connection is closed. False leaves the application open.
	@return True if any windows were closed.
----------------------------------------------------------------------------------------------*/
bool AfDbApp::CloseDbAndWindows(const OLECHAR * pszDbName, const OLECHAR * pszSvrName,
	bool fOkToClose)
{
	bool fClosedWindow = false;

	// Get db info for the selected database, if present.
	// Note: We can't use GetDbInfo because it will create it if it doesn't exist.
	AfDbInfo * pdbi;
	int cdbi = m_vdbi.Size();
	int idbi;
	for (idbi = 0; idbi < cdbi; idbi++)
	{
		pdbi = m_vdbi[idbi];
		if (wcscmp(pszDbName, m_vdbi[idbi]->DbName()) == 0 &&
			wcscmp(pszSvrName, m_vdbi[idbi]->ServerName()) == 0)
		{
			break;
		}
	}
	if (idbi == cdbi)
		return false; // The requested database is not open.

	// Close any windows associated with this database.
	// NOTE: This has to go backwards, because the window will remove itself from this
	// vector when it handles the close message.
	for (int iafw = m_vqafw.Size(); --iafw >= 0; )
	{
		AfMainWnd * pafw = m_vqafw[iafw].Ptr();
		if (!pafw || !pafw->GetLpInfo())
			continue;
		if (pafw->GetLpInfo()->GetDbInfo() != pdbi)
			continue; // Skip windows not associated with this database.
		if (m_qafwCur == pafw)
			m_qafwCur.Clear();
		// Closing the window also removes the AfDbInfo if the last window is closed.
		::SendMessage(pafw->Hwnd(), WM_CLOSE, 0, 0); // Close the window.
		fClosedWindow = true;
	}
	// If we deleted the current window, set the current window to the first window.
	if (m_vqafw.Size() && !m_qafwCur)
		m_qafwCur = m_vqafw[0];

	// Close the application if requested, and if there are no more open connections.
	if (fOkToClose && cdbi < 2)
		Quit(true); // The final db info will be deleted here.

	return fClosedWindow;
}


/*----------------------------------------------------------------------------------------------
	Open a new main window on the specified data.

	@param bstrServerName Server name
	@param bstrDbName Db name
	@param hvoLangProj which language project within the database
	@param hvoMainObj the top-level object on which to open the window.
	@param encUi the user-interface writing system
	@param nTool tool-dependent identifier of which tool to use
	@param nParam another tool-dependend parameter
	@param prmw Pointer to new main window
	@param pszClassT name of the class
	@param nridInitialMessage such as SplashStartMessage
----------------------------------------------------------------------------------------------*/
void AfDbApp::NewMainWnd(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
	int hvoMainObj, int encUi, int nTool, int nParam, RecMainWnd * prmw, achar * pszClassT,
	int nridInitialMessage, DWORD dwRegister)
{
	AssertPtr(prmw);
	AssertPsz(pszClassT);

	// Display the splash screen.
	if (m_fSplashNeeded && !m_qSplashScreenWnd.Ptr())
	{
		ShowSplashScreen();
	}
	StrUni stuMessage;
	stuMessage.Load(nridInitialMessage);
	if (m_qSplashScreenWnd.Ptr())
	{
		m_qSplashScreenWnd->put_Message(stuMessage.Bstr());
		m_qSplashScreenWnd->Show();
		m_qSplashScreenWnd->Refresh();
	}

	OLECHAR * pszDb = bstrDbName ? bstrDbName : L"";
	const OLECHAR * pszSvr = bstrServerName ? bstrServerName : m_stuLocalServer.Chars();
	AfDbInfo * pdbi = GetDbInfo(pszDb, pszSvr);
	if (!pdbi)
		ThrowHr(WarnHr(E_FAIL));
	AfLpInfo * plpi = pdbi->GetLpInfo(hvoLangProj);
	if (!plpi)
		ThrowHr(WarnHr(E_FAIL));
	prmw->Init(plpi);

	WndCreateStruct wcs;
	wcs.InitMain(pszClassT);
	prmw->CreateHwnd(wcs);
	prmw->Show(m_nShow);
	// This is an attempt to stop a random problem where windows come up without the caption
	// bar or the side bar and the DE client portion is displayed under the rebar. It seems to
	// happen most often after importing from SFM, restoring from backup, etc. In this broken
	// state, a resize would fix the problem. So we're trying a resize here to see if this
	// eliminates the problem. (KenZ)
	// KenZ: I've removed this since I've now fixed the real cause for the problem (I think)
	// in AfMainWnd::OnSize and RecMainWnd::OnClientSize where we were calculating improper
	// dimensions prior to the main window being visible.
	//::SendMessage(prmw->Hwnd(), WM_SIZE, kwstRestored, 0);

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
	Produces a unique name that can be used to create a new Topice List.
	@param pode Pointer to database encapsulation object.
	@param ws Language writing system to check existing names under.
	@param stuName [out] Unique name.
	@return True if all went well, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfDbApp::MakeUniqueTopicsListName(IOleDbEncap * pode, int ws, StrUni &stuName)
{
	AssertPtr(pode);
	Vector<StrUni> vstuNames;

	try
	{
		// Make a list of all existing Possibility List names with the correct writing system:
		IOleDbCommandPtr qodc;
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG luSpaceTaken;
		StrUni StuSql;
		StuSql.Format(L"select [n].[txt] from CmPossibilityList [p]"
			L" join CmMajorObject_Name [n] On [p].[Id] = [n].[Obj]"
			L" where [n].[ws] = %d order by [n].[txt]", ws);
		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(StuSql.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		while (fMoreRows)
		{
			OLECHAR rgchName[255];
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchName), isizeof(rgchName),
				&luSpaceTaken, &fIsNull, 2));
			if (!fIsNull)
			{
				StrUni stu(rgchName);
				vstuNames.Push(stu);
			}
			CheckHr(qodc->NextRow(&fMoreRows));
		}
	}
	catch (...)
	{
		return false;
	}

	// Load standard name:
	stuName.Load(kstidTlsListsAddLst);

	// Check the name to see if it is already a list. If it is then make a name with a number
	// appended. Keep trying until we get a name that is not in the list.
	bool fContinue = vstuNames.Size() > 0;
	StrUni stuNameBase = stuName;
	int ncnt = 0;
	while (fContinue)
	{
		for (int n = 0; n < vstuNames.Size(); n++)
		{
			StrUni stu = vstuNames[n];
			if (stuName.Equals(stu))
			{
				// The name already exists: make a name with a number appended.
				ncnt++;
				stuName.Format(L"%s %d", stuNameBase.Chars(), ncnt);
				break; // Stop the for loop, ready to try again.
			}
			else if (stu > stuName || n == vstuNames.Size() - 1)
			{
				// Our query returned items sorted in alphabetical order, and our search has
				// gone past where our candidate name would be, so the search must be over:
				fContinue = false;
				break;
			}
		} // Next string in vector
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the user interface writing system id for the given database, using the program resources
	to select the ICU Locale.

	@param pode Pointer to database encapsulation object.

	@return HVO of user interface writing system.
----------------------------------------------------------------------------------------------*/
int AfDbApp::UserWs(IOleDbEncap * pode)
{
	AssertPtr(pode);

	int wsUser = 0;

	IOleDbCommandPtr qodc;
	CheckHr(pode->CreateCommand(&qodc));

	StrUni stuUserWs(kstidUserWs);
	StrUni stuSql;
	stuSql.Format(L"SELECT [Id] FROM LgWritingSystem WHERE ICULocale = N'%s'",
		stuUserWs.Chars());
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	ComBool fMoreRows;
	CheckHr(qodc->NextRow(&fMoreRows));
	ComBool fIsNull;
	ULONG cbSpaceTaken;
	CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&wsUser), sizeof(wsUser),
		&cbSpaceTaken, &fIsNull, 0));
	Assert(!fIsNull);
	Assert(wsUser);

	return wsUser;
}

/*----------------------------------------------------------------------------------------------
	Create a new Topics List, inventing a new, unique name for it, and setting its language
	writing system to be the topmost current analysis writing system.
	@param pode Pointer to database encapsulation object.
	@return HVO of new list if all went OK, else -1.
----------------------------------------------------------------------------------------------*/
int AfDbApp::NewTopicsList(IOleDbEncap * pode)
{
	AssertPtr(pode);

	// Determine the topmost current analysis writing system:
	int ws;
	StrUni stuSql = L"select top 1 [le].[Id] "
		L" from LangProject_CurAnalysisWss [la]"
		L" join LgWritingSystem [le] on [le].[id] = [la].[dst]"
		L" order by [la].[ord]";
	IOleDbCommandPtr qodc;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG luSpaceTaken;
	CheckHr(pode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&ws), isizeof(ws),
			&luSpaceTaken, &fIsNull, 0));
		if (fIsNull)
			ws = UserWs(pode);
	}

	StrUni stuName;
	if (MakeUniqueTopicsListName(pode, ws, stuName))
		return NewTopicsList(pode, stuName, ws, kwsAnal);

	return -1;
}

/*----------------------------------------------------------------------------------------------
	Create a new Topics List.
	@param pode Pointer to database encapsulation object.
	@param stuName Name of new list. This will also be the initial abbreviation and description.
	@param ws Writing system.
	@param wsMagic List pseudo Writing system (one of the special codes).
	@param hvoCopy Existing list to copy from, or -1 for an empty list
	@return HVO of new list if all went OK, else -1.
----------------------------------------------------------------------------------------------*/
int AfDbApp::NewTopicsList(IOleDbEncap * pode, StrUni stuName, int ws, int wsMagic, HVO hvoCopy)
{
	AssertPtr(pode);
	IOleDbCommandPtr qodc;

	// It's essential that we not allow partial updates or we can damage the database to where
	// a user can't get started again.
	HVO hvo;
	try
	{
		pode->BeginTrans();

		StrUni stuSql;
		ComBool fIsNull;

		if (hvoCopy >= 0)
		{
			// Copy an existing list:
			WaitCursor wc;

			CheckHr(pode->CreateCommand(&qodc));
			qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *) &hvo,
				sizeof(HVO));
			// ENHANCE: (SteveMi) CopyPossibilityList$ gives a guid in the third parameter,
			// which can be changed to an output parameter here.
			stuSql.Format(L"exec CopyObj$ %d, NULL, NULL, ? output", hvoCopy);
			CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
			qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvo), sizeof(HVO), &fIsNull);
			if (fIsNull)
			{
				Assert(false);
				return -1; // Something went wrong.
			}

			Assert(hvo);
		}
		else
		{
			// Create a new List:
			WaitCursor wc;

			CheckHr(pode->CreateCommand(&qodc));
			qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *) &hvo,
				sizeof(HVO));
			stuSql.Format(L"exec CreateObject$ %d, ? output, null", kclidCmPossibilityList);
			CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
			qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvo), sizeof(HVO), &fIsNull);
			if (fIsNull)
			{
				Assert(false);
				return -1; // Something went wrong.
			}

			Assert(hvo);

			// Set date created/modified.
			stuSql.Format(L"update CmMajorObject set DateCreated = getdate(), "
				L"DateModified = getdate()", hvo);
			CheckHr(pode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

			// Set the Abbreviation of the list
			CheckHr(pode->CreateCommand(&qodc));
			stuSql.Format(L"exec SetMultiTxt$ %d, %d, %d, ?", kflidCmPossibilityList_Abbreviation,
				hvo, ws);
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)stuName.Chars(), stuName.Length() * 2));
			CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

			// Set the Description of the list.
			CheckHr(pode->CreateCommand(&qodc));
			stuSql.Format(L"exec SetMultiBigStr$ %d, %d, %d, ?, ?", kflidCmMajorObject_Description,
				hvo, ws);
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)stuName.Chars(), stuName.Length() * 2));

			ITsStringPtr qtss;
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			CheckHr(qtsf->MakeString(stuName.Bstr(), ws, &qtss));

			const int kcbFmtBufMax = 1024;
			int cbFmtBufSize = kcbFmtBufMax;
			int cbFmtSpaceTaken;
			byte * rgbFmt = NewObj byte[kcbFmtBufMax];
			try {
				HRESULT hr;
				CheckHr(hr = qtss->SerializeFmtRgb(rgbFmt, cbFmtBufSize, &cbFmtSpaceTaken));
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
				CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
					reinterpret_cast<ULONG *>(rgbFmt), cbFmtSpaceTaken));
				CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
			}
			catch(...) {
				Assert(rgbFmt);
				delete[] rgbFmt;
				throw;
			}
			Assert(rgbFmt);
			delete[] rgbFmt;

			CheckHr(pode->CreateCommand(&qodc));
			stuSql.Format(L"update [CmPossibilityList] set [ItemClsid]=%d where id=%d",
				kclidCmCustomItem, hvo);
			CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

			// Set the Writing System of the List
			StrUni stuCmd;
			if ((unsigned)wsMagic < (unsigned)kwsLim)
			{
				stuCmd.Format(L"UPDATE [CmPossibilityList]"
					L" SET [WritingSystem]=%d, [WsSelector]=null"
					L" WHERE Id = %d",
					wsMagic, hvo);
			}
			else
			{
				stuCmd.Format(L"UPDATE [CmPossibilityList]"
					L" SET [WritingSystem]=null, [WsSelector]=%d"
					L" WHERE Id = %d",
					wsMagic, hvo);
			}
			CheckHr(pode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		}

		// Set the Name of the list
		CheckHr(pode->CreateCommand(&qodc));
		stuSql.Format(L"exec SetMultiTxt$ %d, %d, %d, ?", kflidCmMajorObject_Name, hvo, ws);
		StrUtil::NormalizeStrUni(stuName, UNORM_NFD);
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)stuName.Chars(), stuName.Length() * 2));
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

		CheckHr(pode->CommitTrans());
	}
	catch(...)
	{
		pode->RollbackTrans();
		throw;	// For now we have nothing to add, so pass it on up.
	}

	return hvo;
}

/*----------------------------------------------------------------------------------------------
	Run the topics list properties dialog, allowing user to alter name etc.
	@param plpi Current Language project info.
	@param hvoPssl HVO of possibility list.
	@param wsMagic Writing system of possibility list.
	@return hwndOwner
	@return The response from the DoModal call on the dialog.
----------------------------------------------------------------------------------------------*/
int AfDbApp::TopicsListProperties(AfLpInfo * plpi, HVO hvoPssl, int wsMagic, HWND hwndOwner)
{
	AssertPtr(plpi);

	int tempWS;
	PossListInfoPtr qpli;
	plpi->LoadPossList(hvoPssl, wsMagic, &qpli);
	tempWS = qpli->GetWs();

	HICON hicon;
	hicon = ::LoadIcon(GetInstance(), MAKEINTRESOURCE(kridPossChsrIcon));
	StrApp strName(qpli->GetName());
	StrApp strAbbr(qpli->GetAbbr());
	StrApp strDesc(qpli->GetDescription());
	StrApp strHelpF(qpli->GetHelpFile());

	tempWS = qpli->GetWs();

	AfDbInfo * pdbi = plpi->GetDbInfo();
	StrApp strLoc = pdbi->ServerName();
	if (strLoc.Length())
		strLoc.Replace(strLoc.Length() - 6, strLoc.Length(), " "); // Remove /SILFW from server.
	strLoc.Append(plpi->PrjName());

	tempWS = qpli->GetWs();

	StrApp strSize;
	strSize.Format(_T("%d %r"),qpli->GetCount(), kstidPropItems);
	StrApp strType(kstidTlsLstsList);
	StrApp strTitle(kstidListsPropMsgCaption);

	tempWS = qpli->GetWs();

	ListsPropDlgPtr qlppd;
	tempWS = qpli->GetWs();

	qlppd.Create();

	tempWS = qpli->GetWs();

	qlppd->Initialize(plpi, hicon, strName.Chars(), strType.Chars(),
		strLoc.Chars(), strSize.Chars(), qpli->GetPsslId(), strDesc.Chars(),
		strAbbr.Chars(), strHelpF.Chars(), kctidGeneralPropTabListName,
		qpli->GetWs());

	bool fsorted = qpli->GetIsSorted();
	int idepth = qpli->GetDepth();
	bool fdup = qpli->GetAllowDup();
	int idispOpt = qpli->GetDisplayOption();
	qlppd->InitializeList(fsorted, idepth, fdup, idispOpt, strTitle.Chars());

	qlppd->SetDateCreatedFlid(kflidCmMajorObject_DateCreated);
	qlppd->SetDateModifiedFlid(kflidCmMajorObject_DateModified);
	int nResponse;
	nResponse = qlppd->DoModal(hwndOwner);
	if (nResponse == -1)
	{
		DWORD dwError = ::GetLastError();
		achar rgchMsg[MAX_PATH+1];
		DWORD cch = ::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, NULL, dwError, 0,
			rgchMsg, MAX_PATH, NULL);
		rgchMsg[cch] = 0;
		::MessageBox(hwndOwner, rgchMsg, strTitle.Chars(), MB_OK | MB_ICONWARNING);
	}

	if (nResponse == kctidOk)
	{
		CustViewDaPtr qcvd;
		plpi->GetDataAccess(&qcvd);
		AssertPtr(qcvd);

		StrUni stuUndoFmt;
		StrUni stuRedoFmt;
		StrUtil::MakeUndoRedoLabels(kstidUndoChangesTo, &stuUndoFmt, &stuRedoFmt);
		StrUni stuUndo;
		StrUni stuRedo;
		StrUni stuLabel(kstidListsProperties);
		stuUndo.Format(stuUndoFmt.Chars(), stuLabel.Chars());
		stuRedo.Format(stuRedoFmt.Chars(), stuLabel.Chars());
		CheckHr(qcvd->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));


		qcvd->SetInt(hvoPssl, kflidCmPossibilityList_DisplayOption, qlppd->GetDispOpt());
		qcvd->SetInt(hvoPssl, kflidCmPossibilityList_IsSorted, qlppd->GetSorted());
		qcvd->SetInt(hvoPssl, kflidCmPossibilityList_Depth, qlppd->GetDepth());
		qcvd->SetInt(hvoPssl, kflidCmPossibilityList_PreventDuplicates, !qlppd->GetDuplicates());
		int ws = qlppd->GetWs();
		if ((unsigned)ws < (unsigned)kwsLim)
		{
			// Need to use ObjProp here because table needs NULL instead of 0
			qcvd->SetObjProp(hvoPssl, kflidCmPossibilityList_WritingSystem, ws);
			qcvd->SetInt(hvoPssl, kflidCmPossibilityList_WsSelector, 0);
		}
		else
		{
			// Need to use ObjProp here because table needs NULL instead of 0
			qcvd->SetObjProp(hvoPssl, kflidCmPossibilityList_WritingSystem, 0);
			qcvd->SetInt(hvoPssl, kflidCmPossibilityList_WsSelector, ws);
		}

		// If we made a change of writing systems, reset all view specs that
		// depend on that list.
		if (ws != wsMagic)
		{
			IOleDbEncapPtr qode; // Declare before qodc.
			IOleDbCommandPtr qodc;
			StrUni stuSql;
			pdbi->GetDbAccess(&qode);
			CheckHr(qode->CreateCommand(&qodc));
			stuSql.Format(
				L"update UserViewField "
				L"set WsSelector = pssl.WsSelector, WritingSystem = pssl.WritingSystem "
				L"from CmPossibilityList pssl, UserViewField uvf "
				L"where pssl.Id = %d and uvf.PossList = %d", hvoPssl, hvoPssl);
			CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
		}

		ws = qpli->GetTitleWs();
		ITsStringPtr qtssName;
		ITsStringPtr qtssAbbr;
		ITsStringPtr qtssDesc;
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);

		StrUni stu(qlppd->GetName());
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, &qtssName);
		qcvd->SetMultiStringAlt(hvoPssl, kflidCmMajorObject_Name, ws, qtssName);

		StrUni stuAbbr(qlppd->GetAbbr());
		qtsf->MakeStringRgch(stuAbbr.Chars(), stuAbbr.Length(), ws, &qtssAbbr);
		qcvd->SetMultiStringAlt(hvoPssl, kflidCmPossibilityList_Abbreviation, ws, qtssAbbr);

		StrUni stuHF(qlppd->GetHelpFile());
		qcvd->SetUnicode(hvoPssl, kflidCmPossibilityList_HelpFile,
			const_cast<OLECHAR *>(stuHF.Chars()), stuHF.Length());

		StrUni stuD(qlppd->GetDescription());
		qtsf->MakeStringRgch(stuD.Chars(), stuD.Length(), ws, &qtssDesc);
		qcvd->SetMultiStringAlt(hvoPssl, kflidCmMajorObject_Description, ws, qtssDesc);
		CheckHr(qcvd->EndUndoTask());
		// The caller is responsible for updating all lists
	}
	return nResponse;
}


/*----------------------------------------------------------------------------------------------
	Splash window messages

	@param pwszMessage
----------------------------------------------------------------------------------------------*/
void AfDbApp::SetSplashMessage(const wchar * pwszMessage)
{
	if (m_qSplashScreenWnd.Ptr())
	{
		StrUni stu(pwszMessage);
		m_qSplashScreenWnd->put_Message(stu.Bstr());
		m_qSplashScreenWnd->Show();
		m_qSplashScreenWnd->Refresh();
	}
}

/*----------------------------------------------------------------------------------------------

	@param nMessageId
----------------------------------------------------------------------------------------------*/
void AfDbApp::SetSplashMessage(uint nMessageId)
{
	if (m_qSplashScreenWnd.Ptr())
	{
		StrUni stu;
		stu.Load(nMessageId);
		m_qSplashScreenWnd->put_Message(stu.Bstr());
		m_qSplashScreenWnd->Show();
		m_qSplashScreenWnd->Refresh();
	}
}

/*----------------------------------------------------------------------------------------------

	@param pwszItemBeingLoaded
----------------------------------------------------------------------------------------------*/
void AfDbApp::SetSplashLoadingMessage(const wchar * pwszItemBeingLoaded)
{
	if (m_qSplashScreenWnd.Ptr())
	{
		StrUni stu(pwszItemBeingLoaded);
		m_qSplashScreenWnd->put_Message(stu.Bstr());
		m_qSplashScreenWnd->Show();
		m_qSplashScreenWnd->Refresh();
	}
}

/*----------------------------------------------------------------------------------------------
	Handle the AfDbApp-specific command line options. The information may be from the command
	line, the Registry, or provided as default values. The results will be placed in
	the input map. Valid keys for it will be: server, database, and item.
	The id of the root object will be returned in hvoRootObjId.

	Assumptions:
	- The project is derived from CmProject.
	- The root object is derived from CmMajorObject.

	@param clidRootObj The class id of the root object.
	@param pstuDefRootObj Pointer to the name of the default root object.
		(NULL is fine, if the first one found is OK.)
	@param clidProject Class id for the project
	@param pstuProjTableName Pointer to a database table name, which is where to find the
		project for clidProject.
	@param phmsustuOptions Pointer to a map that gets the resulting options. (output param)
	@param hvoPId Reference to the id of the project. (output param)
		This will be 0, if not successful in getting it.
	@param hvoRootObjId Reference to the id of the root object. (output param)
		This will be 0, if not successful in getting it.
	@param fUseOptions False to skip using command line options and Registry settings.
		This can be used to just use the default values, in case the other two options
		are not able to get connected properly. It defaults to 'true', which is
		to use the other means.
	@param fAllowNoOwner If true then no error is given if there is no owner. Default = false.

	@return An OptionsReturnValue, which gives korvSuccess (0) for successful,
		otherwise something greater than 0, but less than the upper limit of korvLim.
----------------------------------------------------------------------------------------------*/
OptionsReturnValue AfDbApp::ProcessDBOptions(int clidRootObj, StrUni * pstuDefRootObj,
		int clidProject, StrUni * pstuProjTableName,
		HashMapStrUni<StrUni> * phmsustuOptions, HVO & hvoPId, HVO & hvoRootObjId,
		bool fUseOptions, bool fAllowNoOwner)
{
	Assert(phmsustuOptions);
	Assert(pstuProjTableName);
	// Clear out return parameters.
	phmsustuOptions->Clear();
	hvoRootObjId = 0;

	/*
	* There can be up to four command line arguments, each with a default value:
	*	-filename	name of main object (default: pstuDefRootObj)
	*	-c			computer (default: local)
	*   -db			database name (default: first FW database)
	* We also try to get things from the Registry, if there are no command line options.
	*/
	StrUni stuKey;
	Vector<StrUni> vstuArg;
	StrUni stuDatabase;
	StrUni stuServer;
	StrUni stuName;
	StrUni stu;
	Vector<StrUni> vstuDBNames;
	StrUni stuSqlStmt;
	bool fDBFound = false;

	if (fUseOptions)
	{
		if (m_hmsuvstuCmdLine.Size())
		{
			// Use command line options.
			stuKey = L"db";	// Check for database option.
			if (m_hmsuvstuCmdLine.Retrieve(stuKey, &vstuArg) && vstuArg.Size() > 0)
				stuDatabase = vstuArg[0];
			stuKey = L"c";	// Check for server option.
			if (m_hmsuvstuCmdLine.Retrieve(stuKey, &vstuArg) && vstuArg.Size() > 0)
				stuServer = vstuArg[0];
			stuKey = L"filename";	// Check for list name option.
			if (m_hmsuvstuCmdLine.Retrieve(stuKey, &vstuArg) && vstuArg.Size() > 0)
				stuName = vstuArg[0];
		}
		else
		{
			// Use Registry settings, rather than command line options.
			FwSettings * pfs = AfApp::GetSettings();
			AssertPtr(pfs);
			StrApp str;
			if (pfs->GetString(NULL, _T("LatestDatabaseName"), str))
				stuDatabase.Assign(str);
			if (pfs->GetString(NULL, _T("LatestDatabaseServer"), str))
				stuServer.Assign(str);
			if (pfs->GetString(NULL, _T("LatestRootObjectName"), str))
				stuName.Assign(str);
		}
		if (stuDatabase.Length())
		{
			stuKey = L"database";
			phmsustuOptions->Insert(stuKey, stuDatabase);
		}
		if (stuServer.Length())
		{
			stuKey = L"server";
			phmsustuOptions->Insert(stuKey, stuServer);
		}
		if (stuName.Length())
		{
			StrUtil::NormalizeStrUni(stuName, UNORM_NFD);
			stuKey = L"item";
			phmsustuOptions->Insert(stuKey, stuName);
		}
	}
	// If we don't have a database, try this one (See DN-805).
	if (!stuDatabase.Length())
		stuDatabase = L"Lela-Teli 3";
	stuKey = L"database";
	phmsustuOptions->Insert(stuKey, stuDatabase, true);
	// Try to get connected to something.
	if (!stuServer.Length())
		stuServer = m_stuLocalServer;
	stuKey = L"server";
	phmsustuOptions->Insert(stuKey, stuServer, true);
	try
	{
		IOleDbEncapPtr qode; // Declare before qodc.
		IOleDbCommandPtr qodc;
		OLECHAR rgchName[8000];
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG cbSpaceTaken = 0;

		// Connect to master DB, to see if there are any FW databases.
		// Cache all FW database names.
		qode.CreateInstance(CLSID_OleDbEncap);
		StrUni stuMasterDB(L"master");
		HRESULT hr;
		IgnoreHr(hr = qode->Init(stuServer.Bstr(), stuMasterDB.Bstr(), m_qfistLog, koltMsgBox,
			koltvForever));
		if (FAILED(hr))
		{
			return korvNoServer;
		}
		CheckHr(qode->CreateCommand(&qodc));

		// Now check for FW databases.
		stuSqlStmt = L"exec master..sp_GetFWDBs";
		// This query can fail (see RAID #3862), under the following circumstances:
		//    Stop SQL Server;
		//    Launch Notebook twice, in quick succession, from an explorer or desktop icon.
		// If you followed the above instructions, one instance of Notebook would succeed, the
		// other would assert. The fix below works, but is rather a kludge. The original bug
		// would produce an E_UNEXPECTED error in the call to GetRowset(). This fix just tries
		// upto 10 times, with a 2-second sleep between attempts:
		int nAttempts = 10;
		do
		{
			try {
				CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtStoredProcedure));
				CheckHr(qodc->GetRowset(0));
				break;
			}
			catch (Throwable & thr) {
				thr;
				Sleep(2000);
			}
		} while (--nAttempts);
		CheckHr(qodc->NextRow(&fMoreRows));
		if (!fMoreRows)
		{
			hvoRootObjId = 0;
			return korvNoFWDatabases;	// No FW databases.
		}
		while (fMoreRows)
		{
			ZeroMemory(rgchName, 8000);
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchName),
				isizeof(rgchName), &cbSpaceTaken, &fIsNull, 2));
			if (!cbSpaceTaken)
			{
				phmsustuOptions->Clear();
				hvoRootObjId = 0;
				return korvNoFWDatabases;	// No name for database. Treat the same as no DB.
			}
			stu = rgchName;
			vstuDBNames.Push(stu);
			if (stu.EqualsCI(stuDatabase))
			{
				vstuDBNames.Clear();
				vstuDBNames.Push(stu);	// Only leave the good DB name in the vector.
				fDBFound = true;
				break;
			}
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		qodc.Clear();
		// TODO (?): This causes in SQL Profiler an error: "clean_tables_xact:
		// active sdes for tabid -717845089." This is an error that doesn't have a
		// whole lot of documentation anywhere. Could it be that the connection
		// should be closed properly instead of just clearing the pointer? -- SteveMiller
		qode.Clear();

		if (stuDatabase.Length() && !fDBFound)	// Have name, but couldn't find it.
			return korvInvalidDatabaseName;

		// Spin through list of FW databases, and try to find a project.
		for (int i = 0; i < vstuDBNames.Size(); i++)
		{
			stu = vstuDBNames[i];
			qode.CreateInstance(CLSID_OleDbEncap);
			CheckHr(qode->Init(stuServer.Bstr(), stu.Bstr(), m_qfistLog, koltMsgBox,
				koltvForever));
			// Get the first project, since the user isn't particular.
			// Since "LanguageProject" changed to "LangProject", it's safer to use the clid.
			// See DN-834.
			stuSqlStmt.Format(L"SELECT TOP 1 Id FROM CmObject WHERE Class$=%d", clidProject);
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (fMoreRows)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoPId),
					isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
				ZeroMemory(rgchName, 8000);
				break;
			}
			qodc.Clear();
			qode.Clear();
		}
		// Put database name in map.
		if (!stu.EqualsCI(stuDatabase))
		{
			stuKey = L"database";
			phmsustuOptions->Insert(stuKey, stu, true);
		}
		// Check for qode & hvoPId.
		if (!qode || !hvoPId)
		{
			// The project name in the command line or the Registry was not found
			// in any database. I suppose it could also mean that there was a project,
			// but with no name.
			return korvInvalidProjectName;
		}

		// Get the specified item, or the default one, if not specified.
		Vector<HVO> vhvoObjs;
		Vector<StrUni> vstuNames;
		StrUni stuNameTmp;
		if (!stuName.Length() && pstuDefRootObj)
		{
			stuName = *pstuDefRootObj;
			StrUtil::NormalizeStrUni(stuName, UNORM_NFD);
		}
		bool fHaveLength = (stuName.Length() > 0 && !stuName.Equals(L"***"));
		stuSqlStmt = L"select o.Id, isnull(n.Txt, '***')";
		stuSqlStmt.Append(L" from CmObject o ");
		stuSqlStmt.Append(L"left outer join CmMajorObject_Name n ");
		stuSqlStmt.FormatAppend(L"On n.Obj=o.Id where o.Class$=%d", clidRootObj);
		if (fHaveLength)
			stuSqlStmt.FormatAppend(L" and n.Txt='%s'", stuName.Chars());
		stuSqlStmt.Append(L" Order by o.Id;");
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (!fMoreRows)
		{
			// No such object for the given class, or with the given name,
			// if there is one at all.
			hvoRootObjId = 0;
			if (stuName.Length())
			{
				// An object name was provided (or default used), but was not found at all.
				stuKey = L"item";
				phmsustuOptions->Insert(stuKey, stuName, true);
				return korvInvalidObjectName;
			}
			// There was no name given or as a default. We only had a class id, such
			// as might be the case for a data notebook.
			// There will be no 'item' key in output param.
			return korvInvalidObject;
		}
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoRootObjId),
				isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
			vhvoObjs.Push(hvoRootObjId);	// Cache object id.
			CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(rgchName),
				isizeof(rgchName), &cbSpaceTaken, &fIsNull, 2));
			stuNameTmp = fIsNull ? L"" : rgchName;
			vstuNames.Push(stuNameTmp);		// Cache name.
			CheckHr(qodc->NextRow(&fMoreRows));
		}

		// Make sure we have an item that is owned by the project.
		int cIds = vhvoObjs.Size();
		Assert(cIds);
		Assert(cIds == vstuNames.Size());
		int fResult = 0;
		HVO hvoOldId = 0;
		for (int i = 0; i < cIds; i++)
		{
			hvoRootObjId = vhvoObjs[i];
			if (hvoRootObjId == hvoOldId)
				continue;	// Don't bother checking the database, since the Id was just
							// checked, and we know it is owned by the project. This happens
							// because the names could be in different encodings.
			stuSqlStmt.Format(
				L"declare @result smallint, @proj int "
				L"select top(1) @proj = id from CmProject "
				L"set @result = dbo.fnIsInOwnershipPath$(%d, @proj) "
				L"select @result ", hvoRootObjId);
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (!fMoreRows)
			{
				// I don't think this can happen, since if the code blew up, we would be
				// in the 'catch' code. But we'll do a general SQL failure.
				// We could put the object name in the map, but I don't think the
				// situation is salvageable.
				return korvSQLError;
			}
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&fResult),
				isizeof(fResult), &cbSpaceTaken, &fIsNull, 0));
			if (fResult == -1)
			{
				// The call to fnIsInOwnershipPath$ failed, so treat it as SQL failure.
				return korvSQLError;
			}
			if (!stuName.Length())
				stuName = vstuNames[i];	// Store a name, so we can put it in the
											// Registry later.
			if (!fResult && fAllowNoOwner)
			{
				StrUni stuSql;
				stuSql.Format(L"Select Owner$ from CmObject where id = %d",
					hvoRootObjId);

				CheckHr(qode->CreateCommand(&qodc));
				CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));
				HVO hvoOwner;
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoOwner),
					isizeof(hvoOwner), &cbSpaceTaken, &fIsNull, 0));
				if (!hvoOwner)
					fResult = true;
			}
			if (fResult)
			{
				break;	// Quit loop, since hvoListId is ultimately owned by the project.
			}
			hvoOldId = hvoRootObjId;
		}
		// Put object name in map.
		stuKey = L"item";
		phmsustuOptions->Insert(stuKey, stuName, true);
		if (!fResult)
			return korvInvalidOwner;	// The object specified is not owned by a project.

		return korvSuccess;
	}
	catch (...)
	{
		return korvSQLError;
	}
}

/*----------------------------------------------------------------------------------------------
	Check the return value from ProcessDBOptions. This is a temporary method to handle problems
	found by ProcessDBOptions. It just puts up a basic message box, not the specified dlg(s)
	that allow the user to interactively figure out how to continue, in the event of
	a failure to properly start an app.

	** Now enhanced for apps which can handle the specified dialog.

	@param orv The value returned by ProcessDBOptions.
	@param phmsustuOptions Pointer to a map that has part of the results of
		calling ProcessDBOptions.
	@param bUseDialog Set true if the 'proper' dialog return values can be handled by the
		application which calls this method.

	@return IDOK if orv is korvSuccess, IDCANCEL if user wants to quit,
		IDRETRY if user wants to try again to start app using the default startup values.
----------------------------------------------------------------------------------------------*/
int AfDbApp::ProcessOptRetVal(OptionsReturnValue orv, HashMapStrUni<StrUni> * phmsustuOptions,
	bool bUseDialog)
{
	Assert(phmsustuOptions);
	Assert((orv >= korvSuccess) && (orv < korvLim));

	if (orv == korvSuccess)
		return IDOK;

	// Note: Not all calls to the map will have stuff, if there were problems in
	// ProcessDBOptions.
	StrApp strMsg;
	StrUni stuKey;
	StrUni stuServer;
	StrUni stuDB;
	StrUni stuName;
	stuKey = L"server";
	phmsustuOptions->Retrieve(stuKey, &stuServer);
	StrApp strServer(stuServer);
	if (!strServer.Length())
	{
		DWORD dw = MAX_COMPUTERNAME_LENGTH + 1;
		LPTSTR lpName;
		achar szName[MAX_COMPUTERNAME_LENGTH + 1];
		lpName = szName;
		if (GetComputerName(lpName, &dw))
			strServer = lpName;
	}
	stuKey = L"database";
	phmsustuOptions->Retrieve(stuKey, &stuDB);
	stuKey = L"item";
	StrApp strProj(stuDB);
	phmsustuOptions->Retrieve(stuKey, &stuName);
	StrApp strName(stuName);
	switch (orv)
	{
	case korvInvalidDatabaseName:
		{
			StrApp strTemplate(kstidInvalidDatabaseName);
			strMsg.Format(strTemplate.Chars(), stuDB.Chars());
			break;
		}
	case korvNoServer:	// No Server.
		{
			StrApp strTemplate(kstidNoCompError);
			strMsg.Format(strTemplate.Chars(), strServer.Chars());
			break;
		}
	case korvNoFWDatabases:	// No FieldWorks databases in the specified server.
		{
			StrApp strTemplate(kstidNoDataError);
			strMsg.Format(strTemplate.Chars(), strServer.Chars());
			break;
		}
	case korvInvalidOwner:	// The major object is not owned by the project.
		{
			StrApp strTemplate(kstidMissObjError);
			strMsg.Format(strTemplate.Chars(), strName.Chars(), strProj.Chars(),
				strServer.Chars());
			break;
		}
	case korvInvalidObjectName:	// Specified object name not found.
		{
			StrApp strTemplate(kstidNoObjError);
			strMsg.Format(strTemplate.Chars(), strName.Chars(), strProj.Chars(),
				strServer.Chars());
			break;
		}
	case korvInvalidObject:	// Specified object class not found. (It had no name.)
		{
			StrApp strTemplate(kstidMissDataError);
			strMsg.Format(strTemplate.Chars(), strProj.Chars(), strServer.Chars());
			break;
		}
	case korvSQLError:	// Unspecified SQL error.
		{
			strMsg.Load(kstidSqlError);
			break;
		}
	default:
		{
			Assert(false);
			strMsg.Load(kstidUnknError);
			break;
		}
	}

	if (bUseDialog)
	{
		// TODO JohnL(RandyR): We now use the new -db command line switch,
		// (or the database name that was stored in the Registry)
		// which may leave you with nothing in strProj,
		// if ProcessDBOptions couldn't find the database.
		// The database is checked before the project name.
		AfPrjNotFndDlgPtr qpnf;
		qpnf.Create();
		qpnf->SetProject(strProj.Chars());
		int iRetVal = qpnf->DoModal(NULL);
		return iRetVal;
	}
	else
	{
		StrApp strAddendum(kstidInitRetry);
		strMsg.Append(strAddendum);
		StrApp strAppTitle(kstidAppName);
		return ::MessageBox(NULL, strMsg.Chars(), strAppTitle.Chars(),
							MB_RETRYCANCEL | MB_ICONEXCLAMATION);
	}
}


/*----------------------------------------------------------------------------------------------
	Bring up backup/restore dialog.

	@param pcmd
	@return true
----------------------------------------------------------------------------------------------*/
bool AfDbApp::CmdFileBackup(Cmd * pcmd)
{
	AssertObj(pcmd); // Not used
	AfApp * papp = AfApp::Papp();
	if (papp)
		papp->EnableMainWindows(false);
	DIFwBackupDbPtr qzbkup;
	qzbkup.CreateInstance(CLSID_FwBackup);
//	qzbkup->Init((int)this, (int)m_qafwCur->Hwnd());
	qzbkup->Init(this, (int)m_qafwCur->Hwnd());
	IHelpTopicProviderPtr qhtprov = new HelpTopicProvider(m_strHelpBaseName.Chars());
	int nUserAction;
	qzbkup->UserConfigure(qhtprov, (ComBool)false, &nUserAction);
	qzbkup->Close();
	if (papp)
		papp->EnableMainWindows(true);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Communicate that a style name has changed (or been deleted). Return true if this
	method handled the update.
	@param psts
	@param psda
	@return true if this method handled the update
----------------------------------------------------------------------------------------------*/
bool AfDbApp::OnStyleNameChange(IVwStylesheet * psts, ISilDataAccess * psda)
{
	if (psda)
		psda->EndOuterUndoTask();

	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(GetCurMainWnd());
	if (prmw)
		prmw->LoadUserViews();

	SyncInfo sync(ksyncFullRefresh, 0, 0);
	if (prmw)
	{
		AfLpInfo * plpi = prmw->GetLpInfo();
		AssertPtr(plpi);
		plpi->StoreAndSync(sync);
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Determine if a character is contained in a string
----------------------------------------------------------------------------------------------*/
static bool CharInString(achar ch, achar * charList)
{
	for (int i = 0; charList[i]; i++)
		if (ch == (achar) charList[i])
			return true;

	return false;
}

/*----------------------------------------------------------------------------------------------
	Produce a version of the given name that can be used as a file name. This is done by
	replacing invalid characters with underscores '_'.

	@param strName Name to be filtered.
	@return Filtered name.

	The filtering required for Backup must leave as many characters unchanged as possible, as
	the resulting file name may be seen by the user when restoring, etc.
	Currently we only use this in backup and restore. We must not allow parentheses in project
	names, because they have a special meaning for certain old projects where project and
	database name differ. This list should (roughly?) correspond to
	MiscUtils.GetInvalidProjectNameChars with FilenameFilterStrength.kFilterProjName.
----------------------------------------------------------------------------------------------*/
StrApp AfDbApp::FilterForFileName(StrApp strName)
{
	achar * pch = NewObj achar [1 + strName.Length()];
	_tcscpy_s(pch, 1 + strName.Length(), strName.Chars());
	StrApp strReturn("");

	achar * invalidChars = _T("<>|\".\x7f?*:/\\'[]()"); // SQL commands also restrict ', [, and ] for now

	for (int i = 0; i < strName.Length(); ++i)
	{
		if (CharInString(pch[i], invalidChars) || (unsigned)pch[i] < ' ') // reject control chars too
		{
			strReturn.Append("_");
		}
		else
		{
			strReturn.Append(pch + i, 1);
		}
	}
	delete[] pch;
	return strReturn;
}

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  IUnknown, and IAdvInd are supported.

	@param riid Reference to the desired interface GUID.
	@param ppv Address of a pointer for returning the desired interface pointer.

	@return S_OK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDbApp::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IBackupDelegates *>(this));
	else if (riid == IID_IBackupDelegates)
		*ppv = static_cast<IBackupDelegates *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(
			static_cast<IUnknown *>(static_cast<IBackupDelegates *>(this)), IID_IBackupDelegates);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Increment the reference count. (We use the inherited GenRefObj reference count, but COM
	expects AddRef and release to return the reference count.

	@return The updated reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) AfDbApp::AddRef()
{
	return SuperClass::AddRef();
}

/*----------------------------------------------------------------------------------------------
	Decrement the reference count. (See notes on AddRef.)

	@return The updated reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) AfDbApp::Release()
{
	return SuperClass::Release();
}

/*----------------------------------------------------------------------------------------------
	Returns whatever GetLocalServer() returns but as a BSTR.

	@param pbstrSvrName Bstr to receive the name of the local server (e.g., ls-zook\\SILFW).

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDbApp::GetLocalServer_Bkupd(BSTR * pbstrSvrName)
{
	BEGIN_COM_METHOD;

	StrUni stuSvrName = GetLocalServer();
	stuSvrName.GetBstr(pbstrSvrName);

	END_COM_METHOD(g_fact, IID_IBackupDelegates);
}

/*----------------------------------------------------------------------------------------------
	Return a pointer to the logging file stream interface if it is not null.

	@param ppfist Receives a pointer to the logging file stream interface.

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDbApp::GetLogPointer_Bkupd(IStream** ppfist)
{
	BEGIN_COM_METHOD;

	GetLogPointer(ppfist);

	END_COM_METHOD(g_fact, IID_IBackupDelegates);
}

/*----------------------------------------------------------------------------------------------
	Call SaveData on each of the app's main windows which is connected to the specified
	database.

	@param pszServer Name of the server.
	@param pszDbName Name of the database to be saved.

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDbApp::SaveAllData_Bkupd(const OLECHAR * pszServer, const OLECHAR * pszDbName)
{
	BEGIN_COM_METHOD;

	// Get list of all the application main windows, so we can instruct them to save data if
	// necessary, later on:
	Vector<AfMainWndPtr> vqafw = GetMainWindows();
	StrUni stuDbName = pszDbName;
	StrUni stuServer = pszServer;

	// See if any of the application main windows connect to this database.
	for (int i = 0; i < vqafw.Size(); i++)
	{
		// Get next window in list.
		AfMainWnd * pafw = vqafw[i].Ptr();
		AssertPtr(pafw);
		// Get its language project info.
		AfLpInfo * plpi = pafw->GetLpInfo();
		if (plpi)
		{
			// Get the language project's database info.
			AfDbInfo * pdbi = plpi->GetDbInfo();
			AssertPtr(pdbi);

			// If the database names match.
			if (stuDbName.EqualsCI(pdbi->DbName()) && stuServer.EqualsCI(pdbi->ServerName()))
				pafw->SaveData(); // Save data in this window
		}
	}

	END_COM_METHOD(g_fact, IID_IBackupDelegates);
}

/*----------------------------------------------------------------------------------------------
	Call SaveData on each of the app's main windows which is connected to the specified
	database.

	@param pszServer Name of the server.
	@param pszDbName Name of the database to be saved.

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDbApp::IsDbOpen_Bkupd(const OLECHAR * pszServer, const OLECHAR * pszDbName,
	ComBool * pfIsOpen)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pszServer);
	ChkComArgPtr(pszDbName);
	ChkComOutPtr(pfIsOpen);		// sets *pfIsOpen to FALSE.

	// Get list of all the application main windows, so we can check for their open database.
	Vector<AfMainWndPtr> vqafw = GetMainWindows();
	StrUni stuDbName = pszDbName;
	StrUni stuServer = pszServer;

	// See if any of the application main windows connect to this database.
	for (int i = 0; i < vqafw.Size(); i++)
	{
		// Get next window in list.
		AfMainWnd * pafw = vqafw[i].Ptr();
		AssertPtr(pafw);
		// Get its language project info.
		AfLpInfo * plpi = pafw->GetLpInfo();
		if (plpi)
		{
			// Get the language project's database info.
			AfDbInfo * pdbi = plpi->GetDbInfo();
			AssertPtr(pdbi);

			// If the database names match.
			if (stuDbName.EqualsCI(pdbi->DbName()) && stuServer.EqualsCI(pdbi->ServerName()))
			{
				*pfIsOpen = TRUE;
				return S_OK;
			}
		}
	}

	END_COM_METHOD(g_fact, IID_IBackupDelegates);
}

/*----------------------------------------------------------------------------------------------
	Calls CloseDbAndWindows method.

	@param pszSvrName Name of the server hosting the database.
	@param pszDbName Name of the database to close.
	@param fOkToClose True to close the application if there are no further connections after
		the requested connection is closed. False leaves the application open.
	@param pfWindowsClosed Receives True if any windows were closed.

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDbApp::CloseDbAndWindows_Bkupd(
		const OLECHAR * pszSvrName, const OLECHAR * pszDbName,
		ComBool fOkToClose, ComBool * pfWindowsClosed)
{
	BEGIN_COM_METHOD;

	*pfWindowsClosed = CloseDbAndWindows(pszDbName, pszSvrName, fOkToClose);

	END_COM_METHOD(g_fact, IID_IBackupDelegates);
}

/*----------------------------------------------------------------------------------------------
	Calls IncExportedObjects method.

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDbApp::IncExportedObjects_Bkupd()
{
	BEGIN_COM_METHOD;

	IncExportedObjects();

	END_COM_METHOD(g_fact, IID_IBackupDelegates);
}

/*----------------------------------------------------------------------------------------------
	Calls DecExportedObjects method.

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDbApp::DecExportedObjects_Bkupd()
{
	BEGIN_COM_METHOD;

	DecExportedObjects();

	END_COM_METHOD(g_fact, IID_IBackupDelegates);
}

/*----------------------------------------------------------------------------------------------
	Calls CheckDbVerCompatibility method.

	@param pszSvrName Name of the server hosting the database.
	@param pszDbName Name of the database to close.
	@param pfCompatible Receives true if the app and db are compatible. false if they are
	incompatible (after raising an error message).

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDbApp::CheckDbVerCompatibility_Bkupd(
		const OLECHAR * pszSvrName, const OLECHAR * pszDbName, ComBool * pfCompatible)
{
	BEGIN_COM_METHOD;

	*pfCompatible = CheckDbVerCompatibility(pszSvrName, pszDbName);

	END_COM_METHOD(g_fact, IID_IBackupDelegates);
}

/*----------------------------------------------------------------------------------------------
	Calls ReopenDbAndOneWindow method.

	@param pszSvrName Name of the server hosting the database.
	@param pszDbName Name of the database to close.

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDbApp::ReopenDbAndOneWindow_Bkupd(
		const OLECHAR * pszSvrName, const OLECHAR * pszDbName)
{
	BEGIN_COM_METHOD;

	ReopenDbAndOneWindow(pszDbName, pszSvrName);

	END_COM_METHOD(g_fact, IID_IBackupDelegates);
}


//:>********************************************************************************************
//:>	RegistryCleaner class and global. When a Fieldworks App is unregistered, it cleans out
//:>	all its private registry settings.
//:>********************************************************************************************

class RegistryCleaner : public ModuleEntry
{
public:
	virtual void UnregisterServer()
	{
		// There may not be any settings, during unregistration:
		FwSettings * pfws = AfApp::GetSettings();
		if (pfws)
			pfws->RemoveAll();
	}
};

static RegistryCleaner g_regclean;


//:>********************************************************************************************
//:>	FilterMenuNode methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Add an element to m_vfmnSubItems, sorting by its m_stuText value.

	@param pfmn Pointer to a FilterMenuNode subitem to insert.
----------------------------------------------------------------------------------------------*/
void FilterMenuNode::AddSortedSubItem(FilterMenuNode * pfmn)
{
	AddSortedMenuNode(m_vfmnSubItems, pfmn);
}

/*----------------------------------------------------------------------------------------------
	Add an element to the vector of FilterMenuNodes, sorting by its m_stuText value.

	@param vfmn Vector of pointers to FilterMenuNodes.
	@param pfmn Pointer to a FilterMenuNode subitem to insert.
----------------------------------------------------------------------------------------------*/
void FilterMenuNode::AddSortedMenuNode(FilterMenuNodeVec & vfmn, FilterMenuNode * pfmn)
{
	int iv;
	int ivLim;
	for (iv = 0, ivLim = vfmn.Size(); iv < ivLim; )
	{
		int ivMid = (iv + ivLim) / 2;
		if (vfmn[ivMid]->m_stuText < pfmn->m_stuText)
			iv = ivMid + 1;
		else
			ivLim = ivMid;
	}
	vfmn.Insert(iv, pfmn);
}


//:>********************************************************************************************
//:>	SortMenuNode methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Add an element to m_vsmnSubItems, sorting by its m_stuText value.

	@param psmn Pointer to a SortMenuNode subitem to insert.
----------------------------------------------------------------------------------------------*/
void SortMenuNode::AddSortedSubItem(SortMenuNode * psmn)
{
	AddSortedMenuNode(m_vsmnSubItems, psmn);
}

/*----------------------------------------------------------------------------------------------
	Add an element to the vector of SortMenuNodes, sorting by its m_stuText value.

	@param vsmn Vector of pointers to SortMenuNodes.
	@param psmn Pointer to a SortMenuNode subitem to insert.
----------------------------------------------------------------------------------------------*/
void SortMenuNode::AddSortedMenuNode(SortMenuNodeVec & vsmn, SortMenuNode * psmn)
{
	int iv;
	int ivLim;
	for (iv = 0, ivLim = vsmn.Size(); iv < ivLim; )
	{
		int ivMid = (iv + ivLim) / 2;
		if (vsmn[ivMid]->m_stuText < psmn->m_stuText)
			iv = ivMid + 1;
		else
			ivLim = ivMid;
	}
	vsmn.Insert(iv, psmn);
}

// Semi-Explicit instantiation.
#include "Vector_i.cpp"
#include "HashMap_i.cpp"
#include "Set_i.cpp"

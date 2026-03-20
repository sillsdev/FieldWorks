/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: testViews.cpp
Responsibility:
Last reviewed:

	Global initialization/cleanup for unit testing the Views DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#if !defined(WIN32) && !defined(_M_X64)
#define INITGUID
#endif
#include "testViews.h"
#include "RedirectHKCU.h"
#include "DebugProcs.h"
#include <cstdlib>
#include <cstring>
#include <csignal>
#include <stdexcept>
#if !defined(WIN32) && !defined(_M_X64)
// These define GUIDs that we need to define globally somewhere
#include "TestVwTxtSrc.h"
#include "TestLayoutPage.h"
#endif

namespace
{
	Pfn_Assert g_previousAssertProc = NULL;

	void RestorePreviousAssertProc()
	{
		if (g_previousAssertProc != NULL)
		{
			SetAssertProc(g_previousAssertProc);
			g_previousAssertProc = NULL;
		}
	}

	void TerminateOnSigAbrt(int)
	{
		TerminateProcess(GetCurrentProcess(), 3);
	}

	bool IsFailureInjectionEnabled(const char * pszName)
	{
		size_t cchValue = 0;
		char * pszValue = NULL;
		const bool fEnabled =
			_dupenv_s(&pszValue, &cchValue, pszName) == 0 &&
			pszValue != NULL &&
			strcmp(pszValue, "1") == 0;
		free(pszValue);
		return fEnabled;
	}

	void WINAPI ThrowingAssertProc(const char * pszExp, const char * pszFile, int nLine, HMODULE)
	{
		char szMessage[1024];
		sprintf_s(
			szMessage,
			"Native assert fired during test: %s (%s:%d)",
			pszExp,
			pszFile,
			nLine
		);
		fprintf(stderr, "%s\n", szMessage);
		fflush(stderr);
		throw std::runtime_error(szMessage);
	}
}

namespace unitpp
{
	void GlobalSetup(bool verbose)
	{
		printf("DEBUG: Entering GlobalSetup\n");
		fflush(stdout);
		g_previousAssertProc = SetAssertProc(ThrowingAssertProc);
		ShowAssertMessageBox(0); // Disable assertion dialogs
		printf("DEBUG: After ShowAssertMessageBox\n");
		fflush(stdout);
#if defined(WIN32) || defined(_M_X64)
		ModuleEntry::DllMain(0, DLL_PROCESS_ATTACH);
		printf("DEBUG: After DllMain\n");
		fflush(stdout);
#endif
		auto hr = ::OleInitialize(NULL);
		printf("DEBUG: After OleInitialize (hr=0x%08X)\n", hr);
		fflush(stdout);
		RedirectRegistry();
		printf("DEBUG: After RedirectRegistry\n");
		fflush(stdout);
		StrUtil::InitIcuDataDir();
		printf("DEBUG: After InitIcuDataDir\n");
		fflush(stdout);

	}
	void GlobalTeardown()
	{
		signal(SIGABRT, TerminateOnSigAbrt);

		const bool fInjectTeardownAssert = IsFailureInjectionEnabled("FW_TEST_INDUCE_TEARDOWN_ASSERT");
		const bool fInjectTeardownAbort = IsFailureInjectionEnabled("FW_TEST_INDUCE_TEARDOWN_ABORT");
		if (fInjectTeardownAssert || fInjectTeardownAbort)
			RestorePreviousAssertProc();

		if (fInjectTeardownAssert)
			AssertMsg(false, "Injected teardown assert for native test infrastructure validation");

		if (fInjectTeardownAbort)
		{
			_set_error_mode(_OUT_TO_STDERR);
			_set_abort_behavior(0, _WRITE_ABORT_MSG | _CALL_REPORTFAULT);
			abort();
		}

		::OleUninitialize();
#if defined(WIN32) || defined(_M_X64)
		ModuleEntry::DllMain(0, DLL_PROCESS_DETACH);
#endif

		RestorePreviousAssertProc();
	}
}

namespace TestViews
{
	ILgWritingSystemFactoryPtr g_qwsf;
	int g_wsEng = 0;
	int g_wsFrn = 0;
	int g_wsGer = 0;
	int g_wsTest = 0;
	int g_wsTest2 = 0;

	// Create a dummy writing system factory with English and French.
	void CreateTestWritingSystemFactory()
	{
		StrUni stuWs;
		ILgWritingSystemPtr qws;
		g_qwsf.Attach(NewObj MockLgWritingSystemFactory);

		// Add a writing system for English.
		stuWs.Assign(L"en");
		CheckHr(g_qwsf->get_Engine(stuWs.Bstr(), &qws));
		MockLgWritingSystem* mws = dynamic_cast<MockLgWritingSystem*>(qws.Ptr());
		StrUni stuTimesNewRoman(L"Times New Roman");
		mws->put_DefaultFontName(stuTimesNewRoman.Bstr());
		CheckHr(qws->get_Handle(&g_wsEng));
		CheckHr(g_qwsf->put_UserWs(g_wsEng));

		// Add a writing system for French.
		stuWs.Assign(L"fr");
		CheckHr(g_qwsf->get_Engine(stuWs.Bstr(), &qws));
		mws = dynamic_cast<MockLgWritingSystem*>(qws.Ptr());
		mws->put_DefaultFontName(stuTimesNewRoman.Bstr());
		CheckHr(qws->get_Handle(&g_wsFrn));

		// Add a writing system for German.
		stuWs.Assign(L"de");
		CheckHr(g_qwsf->get_Engine(stuWs.Bstr(), &qws));
		mws = dynamic_cast<MockLgWritingSystem*>(qws.Ptr());
		mws->put_DefaultFontName(stuTimesNewRoman.Bstr());
		CheckHr(qws->get_Handle(&g_wsGer));
	}

	// Free the dummy writing system factory.
	void CloseTestWritingSystemFactory()
	{
		if (g_qwsf)
		{
			g_qwsf.Clear();
		}
	}
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

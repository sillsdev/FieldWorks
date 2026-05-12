/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: testGeneric.cpp
Responsibility: Dan Hinton (and possibly Alistair Imrie, if Dan has died or something)
Last reviewed:

	Global initialization/cleanup for unit testing the Language DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "testGenericLib.h"
#include "RedirectHKCU.h"
#include "DebugProcs.h"
#include <cstdlib>
#include <cstring>
#include <csignal>
#include <stdexcept>

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
		g_previousAssertProc = SetAssertProc(ThrowingAssertProc);
		ShowAssertMessageBox(0); // Disable assertion dialogs
#if defined(WIN32) || defined(WIN64)
		ModuleEntry::DllMain(0, DLL_PROCESS_ATTACH);
#endif
		::OleInitialize(NULL);
		RedirectRegistry();
		StrUtil::InitIcuDataDir();
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

#if defined(WIN32) || defined(WIN64)
		ModuleEntry::DllMain(0, DLL_PROCESS_DETACH);
#endif
		::OleUninitialize();

		RestorePreviousAssertProc();
	}
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkGenLib-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)

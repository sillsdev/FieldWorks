/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: DebugProc.cpp
Responsibility: Darrell Zook
Last reviewed:

	Code for debug utilities.
-------------------------------------------------------------------------------*//*:End Ignore*/
#if WIN32
#define WINDOWS_LEAN_AND_MEAN
#include "Windows.h"
#else
#include <COM.h>
#include <Hacks.h>
#include <unistd.h>
#endif
#include <stdio.h>
#include <assert.h>
#if WIN32
#include <CrtDbg.h>
#endif
#include <signal.h>

#define isizeof (int)sizeof

typedef void (__stdcall * Pfn_Assert)(const char * pszExp, const char * pszFile, int nLine,
	HMODULE hmod);
typedef Pfn_Assert Pfn_Warn;

void APIENTRY DefWarnProc(const char * pszExp, const char * pszFile, int nLine, HMODULE hmod);
void APIENTRY DefAssertProc(const char * pszExp, const char * pszFile, int nLine, HMODULE hmod);
bool GetShowAssertMessageBox();

typedef void (__stdcall * _DBG_REPORT_HOOK)(int, char *);
typedef int (__stdcall * _DBG_DISPLAYMSGBOX_HOOK)(char *);

Pfn_Assert g_pfnAssert = &DefAssertProc;
Pfn_Warn g_pfnWarn = &DefWarnProc;
long g_crefWarnings;
long g_crefAsserts;
long g_crefMemory;
long g_crefDisableNewAfter = -1;
bool g_fShowMessageBox = ::GetShowAssertMessageBox();
_DBG_REPORT_HOOK g_ReportHook = NULL;

void __cdecl SilAssert (
		const char * expr,
		const char * filename,
		unsigned lineno
		);

/*----------------------------------------------------------------------------------------------
	Default Warn Proc. Sends message to debug output.
	TODO: update to be consistent with _UNICODE: strchr -> _tcschr, _snprintf ->
	_sntprintf, put _T() around the strings, etc.
----------------------------------------------------------------------------------------------*/
void WINAPI DefWarnProc(const char * pszExp, const char * pszFile, int nLine, HMODULE hmod)
{
	char sz[256];
#if WIN32
	char szModule[MAX_PATH];
	GetModuleFileName(hmod, szModule, isizeof(szModule));
	char * pszDll = strrchr(szModule, '\\');
	if (pszDll)
	{
		_snprintf_s(sz, (sizeof(sz)/sizeof(sz[0])) - 1, _TRUNCATE, "%s(%d) in %s: warning : %s\n", pszFile, nLine,
			pszDll + 1, pszExp);
	}
	else
#endif
	{
		_snprintf_s(sz, (sizeof(sz)/sizeof(sz[0])) - 1, _TRUNCATE, "%s(%d): warning : %s\n", pszFile, nLine, pszExp);
	}
	OutputDebugString(sz);
}

/*----------------------------------------------------------------------------------------------
	Default Assert Proc. Sends message to debug output.
----------------------------------------------------------------------------------------------*/
void WINAPI DefAssertProc(const char * pszExp, const char * pszFile, int nLine, HMODULE hmod)
{
	SilAssert(pszExp, pszFile, nLine);
	UNREFERENCED_PARAMETER(hmod);
}

/*----------------------------------------------------------------------------------------------
	Entry point for the WarnProc family of messages.
----------------------------------------------------------------------------------------------*/
__declspec(dllexport) void WINAPI WarnProc(const char * pszExp, const char * pszFile,
	int nLine, bool fCritical, HMODULE hmod)
{
	if (fCritical || g_crefWarnings >= 0)
		(*g_pfnWarn)(pszExp, pszFile, nLine, hmod);
}

/*----------------------------------------------------------------------------------------------
	Entry point for AssertProc.
----------------------------------------------------------------------------------------------*/
__declspec(dllexport) void WINAPI AssertProc(const char * pszExp, const char * pszFile,
	int nLine, bool fCritical, HMODULE hmod)
{
	if (fCritical || g_crefAsserts >= 0)
		(*g_pfnAssert)(pszExp, pszFile, nLine, hmod);
}

/*----------------------------------------------------------------------------------------------
	Entry point to set the WarnProc function.
----------------------------------------------------------------------------------------------*/
__declspec(dllexport) Pfn_Warn WINAPI SetWarnProc(Pfn_Warn pfnWarn)
{
	Pfn_Warn pfnOldWarn = g_pfnWarn;
	g_pfnWarn = pfnWarn;
	return pfnOldWarn;
}

/*----------------------------------------------------------------------------------------------
	Entry point to set the AssertProc function.
----------------------------------------------------------------------------------------------*/
extern "C" __declspec(dllexport) Pfn_Assert APIENTRY SetAssertProc(Pfn_Assert pfnAssert)
{
	Pfn_Assert pfnOldAssert = g_pfnAssert;
	g_pfnAssert = pfnAssert;
	return pfnOldAssert;
}

/*----------------------------------------------------------------------------------------------
	If g_crefWarnings < 0, warnings will not do anything.
----------------------------------------------------------------------------------------------*/
__declspec(dllexport) int WINAPI HideWarnings(bool f)
{
	if (f)
		return InterlockedDecrement(&g_crefWarnings);
	else
		return InterlockedIncrement(&g_crefWarnings);
}

/*----------------------------------------------------------------------------------------------
	If g_crefAsserts < 0, asserts will not do anything.
----------------------------------------------------------------------------------------------*/
__declspec(dllexport) int WINAPI HideAsserts(bool f)
{
	if (f)
		return InterlockedDecrement(&g_crefAsserts);
	else
		return InterlockedIncrement(&g_crefAsserts);
}

/*----------------------------------------------------------------------------------------------
	If g_crefWarnings < 0, warnings will not do anything.
	If g_crefAsserts < 0, asserts will not do anything.
----------------------------------------------------------------------------------------------*/
__declspec(dllexport) int WINAPI HideErrors(bool f)
{
	if (f)
	{
		InterlockedDecrement(&g_crefWarnings);
		return InterlockedDecrement(&g_crefAsserts);
	}
	else
	{
		InterlockedIncrement(&g_crefWarnings);
		return InterlockedIncrement(&g_crefAsserts);
	}
}


/*----------------------------------------------------------------------------------------------
	If g_crefMemory < 0, NewObj and new will return NULL instead of allocating memory.
----------------------------------------------------------------------------------------------*/
__declspec(dllexport) int WINAPI DisableNew(bool f)
{
	if (f)
		return InterlockedDecrement(&g_crefMemory);
	else
		return InterlockedIncrement(&g_crefMemory);
}

/*----------------------------------------------------------------------------------------------
	If cnew is negative, this memory disabler will be disabled.
	Otherwise, cnew represents the number of times new can be called before it will fail.
----------------------------------------------------------------------------------------------*/
__declspec(dllexport) int WINAPI DisableNewAfter(int cnew)
{
	if (cnew >= 0)
		return g_crefDisableNewAfter = cnew;
	else
		return g_crefDisableNewAfter = -1;
}

/*----------------------------------------------------------------------------------------------
	The debug version of several memory allocation functions (new, realloc, malloc, calloc,
	BSTR functions, and CoTaskMem functions) call this first to see if the memory should be
	allocated.
----------------------------------------------------------------------------------------------*/
__declspec(dllexport) bool WINAPI CanAllocate()
{
	if (g_crefMemory < 0 || g_crefDisableNewAfter == 0)
		return false;
	if (g_crefDisableNewAfter > 0)
		--g_crefDisableNewAfter;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Set to true to show a message box for asserts (default). If false no message box is
	displayed.
----------------------------------------------------------------------------------------------*/
extern "C" __declspec(dllexport) void APIENTRY ShowAssertMessageBox(int fShowMessageBox)
{
	g_fShowMessageBox = (fShowMessageBox != 0);
}

/*----------------------------------------------------------------------------------------------
	Set a hook method that receives all output instead of printing to OutputDebugString.
	This allows displaying debug strings in managed code even if unmanaged debugging is not
	enabled.
	NOTE: We have to use __stdcall (to which APIENTRY resolves) in order to be easily able
	to call from managed code!
	REMARK: We could probably have used SetWarnProc/SetAssertProc, but I like what SilAssert
	does and don't want to rebuild that in managed code.
----------------------------------------------------------------------------------------------*/
extern "C" __declspec(dllexport) _DBG_REPORT_HOOK APIENTRY DbgSetReportHook(_DBG_REPORT_HOOK hook)
{
	_DBG_REPORT_HOOK oldHook = g_ReportHook;
	g_ReportHook = hook;
	return oldHook;
}

/*----------------------------------------------------------------------------------------------
	Handler that intercepts debug output. If a ReportHook is set it sends the output to that,
	otherwise it outputs it with OutputDebugString and printf.
----------------------------------------------------------------------------------------------*/
int __cdecl ReportHandler(int nReportType, char* szMsg, int* pRet)
{
	*pRet = 0;
	if (g_ReportHook)
		g_ReportHook(nReportType, szMsg);
	else
	{
		switch (nReportType)
		{
			case _CRT_ASSERT:
			case _CRT_ERROR:
			case _CRT_WARN:
				OutputDebugString(szMsg);
				break;
		}
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Sets the report hook that intercepts all debug messages
----------------------------------------------------------------------------------------------*/
extern "C" __declspec(dllexport) int APIENTRY DebugProcsInit(void)
{
#if WIN32
	int nRet = _CrtSetReportHook2(_CRT_RPTHOOK_INSTALL, ReportHandler);
	return nRet;
#else
	return 0;
#endif
}

/*----------------------------------------------------------------------------------------------
	Removes the report hook that intercepts all debug messages
----------------------------------------------------------------------------------------------*/
extern "C" __declspec(dllexport) int APIENTRY DebugProcsExit(void)
{
#if WIN32
	int nRet = _CrtSetReportHook2(_CRT_RPTHOOK_REMOVE, ReportHandler);
	return nRet;
#else
	return 0;
#endif
}

/*----------------------------------------------------------------------------------------------
	Returns the AssertMessageBox value from the registry; if not set return the value of the
	environment variable AssertUiEnabled; if not set return true
----------------------------------------------------------------------------------------------*/
bool GetShowAssertMessageBox()
{
#if WIN32
	HKEY hk;
	if (::RegOpenKeyEx(HKEY_LOCAL_MACHINE, "Software\\SIL\\FieldWorks", 0,
			KEY_QUERY_VALUE, &hk) == ERROR_SUCCESS)
	{
		DWORD fShowAssertMessageBox = true;
		DWORD cb = sizeof(fShowAssertMessageBox);
		DWORD dwT;
		LONG ret = ::RegQueryValueEx(hk, "AssertMessageBox", NULL, &dwT,
			(LPBYTE)&fShowAssertMessageBox, &cb);
		RegCloseKey(hk);
		if (ret == ERROR_SUCCESS)
			return fShowAssertMessageBox ? true : false; // otherwise we get a performance warning
	}
// getenv is deprecated on Windows
#pragma warning(push)
#pragma warning(disable: 4996)

// Windows doesn't know strcasecmp, it calls it stricmp instead...
#ifndef strcasecmp
#define strcasecmp stricmp
#endif

#endif // WIN32
	const char* pEnvVar = getenv("AssertUiEnabled");
	return !pEnvVar ||
		strcasecmp(pEnvVar, "0") != 0 &&
		strcasecmp(pEnvVar, "false") != 0 &&
		strcasecmp(pEnvVar, "no") != 0;
#if WIN32
#pragma warning(pop)
#endif // WIN32
}

/*----------------------------------------------------------------------------------------------
	The following is basically copied from Microsoft's assert.c file. The key change is to
	use OutputDebugStr to log the error message, as well as MessageBox to display it.
	This is helpful especially when an Assert triggers during program shutdown, which causes
	MessageBox to fail somehow (producing a clang), and the program to abort with all kinds
	of spurious memory leaks caused by missing the usual shutdown messages (WM_NCDESTROY
	especially) for the individual windows.
----------------------------------------------------------------------------------------------*/

/***
*assert.c - Display a message and abort
*
*       Copyright (c) 1988-2001, Microsoft Corporation. All rights reserved.
*
*Purpose:
*
*******************************************************************************/

/*
 * assertion format string for use with output to stderr
 */
static char _assertstring[] = "Assertion failed: %s, file %s, line %d\n";

/*      Format of MessageBox for assertions:
*
*       ================= Microsft Visual C++ Debug Library ================
*
*       Assertion Failed!
*
*       Program: c:\test\mytest\foo.exe
*       File: c:\test\mytest\bar.c
*       Line: 69
*
*       Expression: <expression>
*
*       For information on how your program can cause an assertion
*       failure, see the Visual C++ documentation on asserts
*
*       (Press Retry to debug the application - JIT must be enabled)
*
*       ===================================================================
*/

/*
 * assertion string components for message box
 */
#define BOXINTRO    "Assertion failed!"
#define PROGINTRO   "Program: "
#define FILEINTRO   "File: "
#define LINEINTRO   "Line: "
#define EXPRINTRO   "Expression: "
#define INFOINTRO   "For information on how your program can cause an assertion\n" \
					"failure, see the Visual C++ documentation on asserts"
#define HELPINTRO   "(Press Retry to debug the application - JIT must be enabled)"

static const char * dotdotdot = "...";
static const char * newline = "\n";
static const char * dblnewline = "\n\n";

#define DOTDOTDOTSZ 3
#define NEWLINESZ   1
#define DBLNEWLINESZ   2

#define MAXLINELEN  60 /* max length for line in message box */
#define ASSERTBUFSZ (MAXLINELEN * 9) /* 9 lines in message box */

#if defined (_M_IX86)
#define _DbgBreak() __asm { int 3 }
#elif defined (_M_ALPHA)
void _BPT();
#pragma intrinsic(_BPT)
#define _DbgBreak() _BPT()
#elif defined (_M_IA64)
void __break(int);
#pragma intrinsic (__break)
#define _DbgBreak() __break(0x80016)
#else  /* defined (_M_IA64) */
#define _DbgBreak() DebugBreak()
#endif  /* defined (_M_IA64) */

/***
*_assert() - Display a message and abort
*
*Purpose:
*       The assert macro calls this routine if the assert expression is
*       true.  By placing the assert code in a subroutine instead of within
*       the body of the macro, programs that call assert multiple times will
*       save space.
*
*Entry:
*
*Exit:
*
*Exceptions:
*
* NOTE: this is intented to be used by unmanaged applications only; managed
* apps should instantiate the class in DebugProcs.cs that has its own
* AssertProc!
*
*******************************************************************************/

void __cdecl SilAssert (
		const char * expr,
		const char * filename,
		unsigned lineno
		)
{
	/*
	 * Build the assertion message, then display it.
	 */
	int nCode = IDIGNORE;
	char * pch;
	char assertbuf[ASSERTBUFSZ];
	char progname[MAX_PATH + 1];

	/*
		* Line 1: box intro line
		*/
	strcpy_s( assertbuf, ASSERTBUFSZ, BOXINTRO );
	strcat_s( assertbuf, ASSERTBUFSZ, dblnewline );

#if WIN32
	// TODO-Linux: Find filename of managed executable
	/*
		* Line 2: program line
		*/
	strcat_s( assertbuf, ASSERTBUFSZ, PROGINTRO );

	progname[MAX_PATH] = '\0';
	if ( !GetModuleFileName( NULL, progname, MAX_PATH ))
		strcpy_s( progname, ASSERTBUFSZ, "<program name unknown>");

	pch = (char *)progname;

	/* sizeof(PROGINTRO) includes the NULL terminator */
	if ( sizeof(PROGINTRO) + strlen(progname) + NEWLINESZ > MAXLINELEN )
	{
		int cch = (sizeof(PROGINTRO) + strlen(progname) + NEWLINESZ) - MAXLINELEN;
		pch += cch;
		strncpy_s( pch, sizeof(progname) - cch, dotdotdot, DOTDOTDOTSZ );
	}

	strcat_s( assertbuf, ASSERTBUFSZ, pch );
	strcat_s( assertbuf, ASSERTBUFSZ, newline );
#endif

	/*
		* Line 3: file line
		*/
	strcat_s( assertbuf, ASSERTBUFSZ, FILEINTRO );

	/* sizeof(FILEINTRO) includes the NULL terminator */
	if ( sizeof(FILEINTRO) + strlen(filename) + NEWLINESZ > MAXLINELEN )
	{
		size_t p, len, ffn;

		pch = (char *) filename;
		ffn = MAXLINELEN - sizeof(FILEINTRO) - NEWLINESZ;

		for ( len = strlen(filename), p = 1;
				pch[len - p] != '\\' && pch[len - p] != '/' && p < len;
				p++ );

		/* keeping pathname almost 2/3rd of full filename and rest
			* is filename
			*/
		if ( (ffn - ffn/3) < (len - p) && ffn/3 > p )
		{
			/* too long. using first part of path and the
				filename string */
			strncat_s( assertbuf, ASSERTBUFSZ, pch, ffn - DOTDOTDOTSZ - p );
			strcat_s( assertbuf, ASSERTBUFSZ, dotdotdot );
			strcat_s( assertbuf, ASSERTBUFSZ, pch + len - p );
		}
		else if ( ffn - ffn/3 > len - p )
		{
			/* pathname is smaller. keeping full pathname and putting
				* dotdotdot in the middle of filename
				*/
			p = p/2;
			strncat_s( assertbuf, ASSERTBUFSZ, pch, ffn - DOTDOTDOTSZ - p );
			strcat_s( assertbuf, ASSERTBUFSZ, dotdotdot );
			strcat_s( assertbuf, ASSERTBUFSZ, pch + len - p );
		}
		else
		{
			/* both are long. using first part of path. using first and
				* last part of filename.
				*/
			strncat_s( assertbuf, ASSERTBUFSZ, pch, ffn - ffn/3 - DOTDOTDOTSZ );
			strcat_s( assertbuf, ASSERTBUFSZ, dotdotdot );
			strncat_s( assertbuf, ASSERTBUFSZ, pch + len - p, ffn/6 - 1 );
			strcat_s( assertbuf, ASSERTBUFSZ, dotdotdot );
			strcat_s( assertbuf, ASSERTBUFSZ, pch + len - (ffn/3 - ffn/6 - 2) );
		}

	}
	else
		/* plenty of room on the line, just append the filename */
		strcat_s( assertbuf, ASSERTBUFSZ, filename );

	strcat_s( assertbuf, ASSERTBUFSZ, newline );

	/*
		* Line 4: line line
		*/
	strcat_s( assertbuf, ASSERTBUFSZ, LINEINTRO );
	_itoa_s( lineno, assertbuf + strlen(assertbuf), sizeof(assertbuf) - strlen(assertbuf), 10 );
	strcat_s( assertbuf, ASSERTBUFSZ, dblnewline );

	/*
		* Line 5: message line
		*/
	strcat_s( assertbuf, ASSERTBUFSZ, EXPRINTRO );

	/* sizeof(HELPINTRO) includes the NULL terminator */

	if (    strlen(assertbuf) +
			strlen(expr) +
			2*DBLNEWLINESZ +
			sizeof(INFOINTRO)-1 +
			sizeof(HELPINTRO) > ASSERTBUFSZ )
	{
		strncat_s( assertbuf, ASSERTBUFSZ,
			expr,
			ASSERTBUFSZ -
			(strlen(assertbuf) +
			DOTDOTDOTSZ +
			2*DBLNEWLINESZ +
			sizeof(INFOINTRO)-1 +
			sizeof(HELPINTRO)) );
		strcat_s( assertbuf, ASSERTBUFSZ, dotdotdot );
	}
	else
		strcat_s( assertbuf, ASSERTBUFSZ, expr );

	strcat_s( assertbuf, ASSERTBUFSZ, dblnewline );

	/*
		* Line 6, 7: info line
		*/

	strcat_s(assertbuf, ASSERTBUFSZ, INFOINTRO);
	strcat_s( assertbuf, ASSERTBUFSZ, dblnewline );

	/*
		* Line 8: help line
		*/
	strcat_s(assertbuf, ASSERTBUFSZ, HELPINTRO);

	// SIL addition: log the message using OutputDebugString

	if (g_ReportHook)
		g_ReportHook(_CRT_ASSERT, assertbuf);
	else
		OutputDebugString(assertbuf);

	// NOTE: this method is intented to be used by unmanaged apps only;
	// managed apps should use DebugProcs.AssertProc in DebugProcs.cs

	/*
	 * Write out via MessageBox
	 */
	if (g_fShowMessageBox)
	{
		nCode = MessageBoxA(NULL, assertbuf,
			"SIL Program Failure warning (Assert failed)",
			MB_ABORTRETRYIGNORE|MB_ICONHAND|MB_SETFOREGROUND|MB_TASKMODAL);

		/* Abort: abort the program */
		if (nCode == IDABORT)
		{
			/* raise abort signal */
			raise(SIGABRT);

			/* We usually won't get here, but it's possible that
				SIGABRT was ignored.  So exit the program anyway. */

			_exit(3);
		}

		/* Retry: call the debugger */
		if (nCode == IDRETRY)
		{
#if WIN32
			_DbgBreak();
#else
			raise(SIGTRAP);
#endif
			/* return to user code */
			return;
		}

		if (nCode != IDIGNORE)
			abort();
	}
	else // !g_fShowMessageBox
	{
		// if we don't show a message box we should at least abort (and output the assertion
		// text if we haven't done that already). Note that we don't call _exit(3) as above
		// so that we can trap the signal and ignore it in unit tests
		if (g_ReportHook)
			OutputDebugString(assertbuf);
		raise(SIGABRT);
	}

	/* Ignore: continue execution */
	return;
}

//-------------------------------------------------------------------------------------------------
// <copyright file="dutil.cpp" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
//    Utility layer that provides standard support for asserts, exit macros
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// Asserts & Tracing
#ifdef DEBUG

const int DUTIL_STRING_BUFFER = 1024;
static HMODULE Dutil_hAssertModule = NULL;
static DUTIL_ASSERTDISPLAYFUNCTION Dutil_pfnDisplayAssert = NULL;
static BOOL Dutil_fNoAsserts = FALSE;
static REPORT_LEVEL Dutil_rlCurrentTrace = REPORT_STANDARD;
static BOOL Dutil_fTraceFilenames = FALSE;


/*******************************************************************
 Dutil_SetAssertModule

*******************************************************************/
extern "C" void DAPI Dutil_SetAssertModule(
	__in HMODULE hAssertModule
	)
{
	Dutil_hAssertModule = hAssertModule;
}


/*******************************************************************
 Dutil_SetAssertDisplayFunction

*******************************************************************/
extern "C" void DAPI Dutil_SetAssertDisplayFunction(
	__in DUTIL_ASSERTDISPLAYFUNCTION pfn
	)
{
	Dutil_pfnDisplayAssert = pfn;
}


/*******************************************************************
 Dutil_AssertMsg

*******************************************************************/
extern "C" void DAPI Dutil_AssertMsg(
	__in LPCSTR szMessage
	)
{
	static BOOL fInAssert = FALSE; // TODO: make this thread safe (this is a cheap hack to prevent re-entrant Asserts)

	HRESULT hr = S_OK;
	DWORD er;

	int id = IDRETRY;
	HKEY hkDebug = NULL;
	HANDLE hAssertFile = INVALID_HANDLE_VALUE;
	char szPath[MAX_PATH] = "";
	DWORD cch;

	if (fInAssert)
		return;
	fInAssert = TRUE;

	char szMsg[DUTIL_STRING_BUFFER];
	hr = StringCchCopyA(szMsg, countof(szMsg), szMessage);
	ExitOnFailure(hr, "failed to copy message while building assert message");

	if (Dutil_pfnDisplayAssert)
	{
		// call custom function to display the assert string
		if (!Dutil_pfnDisplayAssert(szMsg))
			ExitFunction();
	}
	else
		OutputDebugStringA(szMsg);

	if (!Dutil_fNoAsserts)
	{
		er = ::RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Delivery\\Debug", 0, KEY_QUERY_VALUE, &hkDebug);
		if (ERROR_SUCCESS == er)
		{
			cch = countof(szPath);
			er = ::RegQueryValueExA(hkDebug, "DeliveryAssertsLog", NULL, NULL, reinterpret_cast<BYTE*>(szPath), &cch);
			if (ERROR_SUCCESS == er)
			{
				hAssertFile = ::CreateFileA(szPath, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
				if (INVALID_HANDLE_VALUE != hAssertFile)
				{
					::SetFilePointer(hAssertFile, 0, 0, FILE_END);
					StringCchCatA(szMsg, countof(szMsg), "\r\n");
					::WriteFile(hAssertFile, szMsg, lstrlenA(szMsg), &cch, NULL);
				}
			}
		}

		// if anything went wrong while fooling around with the registry, just show the usual assert dialog box
		if (ERROR_SUCCESS != er)
		{
			hr = StringCchCatA(szMsg, countof(szMsg), "\nAbort=Debug, Retry=Skip, Ignore=Skip all");
			ExitOnFailure(hr, "failed to concat string while building assert message");

			id = ::MessageBoxA(0, szMsg, "Debug Assert Message",
							   MB_SERVICE_NOTIFICATION | MB_TOPMOST |
							   MB_DEFBUTTON2 | MB_ABORTRETRYIGNORE);
		}
	}

	if (id == IDABORT)
	{
		if (Dutil_hAssertModule)
		{
			::GetModuleFileNameA(Dutil_hAssertModule, szPath, countof(szPath));

			hr = StringCchPrintfA(szMsg, countof(szMsg), "Module is running from: %s\nIf you are not using pdb-stamping, place your PDB near the module and attach to process id: %d (0x%x)", szPath, ::GetCurrentProcessId(), ::GetCurrentProcessId());
			if (SUCCEEDED(hr))
				::MessageBoxA(0, szMsg, "Debug Assert Message", MB_SERVICE_NOTIFICATION | MB_TOPMOST | MB_OK);
		}

		::DebugBreak();
	}
	else if (id == IDIGNORE)
		Dutil_fNoAsserts = TRUE;

LExit:
	if (INVALID_HANDLE_VALUE != hAssertFile)
		::CloseHandle(hAssertFile);
	if (hkDebug)
		::RegCloseKey(hkDebug);
	fInAssert = FALSE;
}


/*******************************************************************
 Dutil_Assert

*******************************************************************/
extern "C" void DAPI Dutil_Assert(
	__in LPCSTR szFile,
	__in int iLine
	)
{
	HRESULT hr = S_OK;
	char szMessage[DUTIL_STRING_BUFFER];
	hr = StringCchPrintfA(szMessage, countof(szMessage), "Assertion failed in %s, %i", szFile, iLine);
	if (SUCCEEDED(hr))
		Dutil_AssertMsg(szMessage);
	else
		Dutil_AssertMsg("Assert failed to build string");
}


/*******************************************************************
 Dutil_AssertSz

*******************************************************************/
extern "C" void DAPI Dutil_AssertSz(
	__in LPCSTR szFile,
	__in int iLine,
	__in LPCSTR szMsg
	)
{
	HRESULT hr = S_OK;
	char szMessage[DUTIL_STRING_BUFFER];

	hr = StringCchPrintfA(szMessage, countof(szMessage), "Assertion failed in %s, %i\n%s", szFile, iLine, szMsg);
	if (SUCCEEDED(hr))
		Dutil_AssertMsg(szMessage);
	else
		Dutil_AssertMsg("Assert failed to build string");
}


/*******************************************************************
 Dutil_TraceSetLevel

*******************************************************************/
extern "C" void DAPI Dutil_TraceSetLevel(
	__in REPORT_LEVEL rl,
	__in BOOL fTraceFilenames
	)
{
	Dutil_rlCurrentTrace = rl;
	Dutil_fTraceFilenames = fTraceFilenames;
}


/*******************************************************************
 Dutil_TraceGetLevel

*******************************************************************/
extern "C" REPORT_LEVEL DAPI Dutil_TraceGetLevel(
	)
{
	return Dutil_rlCurrentTrace;
}


/*******************************************************************
 Dutil_Trace

*******************************************************************/
extern "C" void DAPI Dutil_Trace(
	__in LPCSTR szFile,
	__in int iLine,
	__in REPORT_LEVEL rl,
	__in LPCSTR szFormat,
	...
	)
{
	AssertSz(REPORT_NONE != rl, "REPORT_NONE is not a valid tracing level");

	HRESULT hr = S_OK;
	char szOutput[DUTIL_STRING_BUFFER];
	char szMsg[DUTIL_STRING_BUFFER];

	if (Dutil_rlCurrentTrace < rl)
		return;

	va_list args;
	va_start(args, szFormat);
	hr = StringCchVPrintfA(szOutput, countof(szOutput), szFormat, args);
	va_end(args);

	if (SUCCEEDED(hr))
	{
		LPCSTR szPrefix = "Trace/u";
		char szMsg[DUTIL_STRING_BUFFER];
		switch (rl)
		{
		case REPORT_STANDARD:
			szPrefix = "Trace/s";
			break;
		case REPORT_VERBOSE:
			szPrefix = "Trace/v";
			break;
		case REPORT_DEBUG:
			szPrefix = "Trace/d";
			break;
		}

		if (Dutil_fTraceFilenames)
			hr = StringCchPrintfA(szMsg, countof(szMsg), "%s [%s,%d]: %s\r\n", szPrefix, szFile, iLine, szOutput);
		else
			hr = StringCchPrintfA(szMsg, countof(szMsg), "%s: %s\r\n", szPrefix, szOutput);

		if (SUCCEEDED(hr))
			OutputDebugStringA(szMsg);
		// else fall through to the case below
	}

	if (FAILED(hr))
	{
		if (Dutil_fTraceFilenames)
			StringCchPrintfA(szMsg, countof(szMsg), "Trace [%s,%d]: message too long, skipping\r\n", szFile, iLine);
		else
			StringCchPrintfA(szMsg, countof(szMsg), "Trace: message too long, skipping\r\n");
		OutputDebugStringA(szMsg);
	}
}


/*******************************************************************
 Dutil_TraceError

*******************************************************************/
extern "C" void DAPI Dutil_TraceError(
	__in LPCSTR szFile,
	__in int iLine,
	__in REPORT_LEVEL rl,
	__in HRESULT hrError,
	__in LPCSTR szFormat,
	...
	)
{
	HRESULT hr = S_OK;
	char szOutput[DUTIL_STRING_BUFFER];
	char szMsg[DUTIL_STRING_BUFFER];

	// if this is NOT an error report and we're not logging at this level, bail
	if (REPORT_ERROR != rl && Dutil_rlCurrentTrace < rl)
		return;

	va_list args;
	va_start(args, szFormat);
	hr = StringCchVPrintfA(szOutput, countof(szOutput), szFormat, args);
	va_end(args);

	if (SUCCEEDED(hr))
	{
		if (Dutil_fTraceFilenames)
		{
			if (FAILED(hrError))
				hr = StringCchPrintfA(szMsg, countof(szMsg), "TraceError 0x%x [%s,%d]: %s\r\n", hrError, szFile, iLine, szOutput);
			else
				hr = StringCchPrintfA(szMsg, countof(szMsg), "TraceError [%s,%d]: %s\r\n", szFile, iLine, szOutput);
		}
		else
		{
			if (FAILED(hrError))
				hr = StringCchPrintfA(szMsg, countof(szMsg), "TraceError 0x%x: %s\r\n", hrError, szOutput);
			else
				hr = StringCchPrintfA(szMsg, countof(szMsg), "TraceError: %s\r\n", szOutput);
		}

		if (SUCCEEDED(hr))
			OutputDebugStringA(szMsg);
		// else fall through to the failure case below
	}

	if (FAILED(hr))
	{
		if (Dutil_fTraceFilenames)
		{
			if (FAILED(hrError))
				StringCchPrintfA(szMsg, countof(szMsg), "TraceError 0x%x [%s,%d]: message too long, skipping\r\n", hrError, szFile, iLine);
			else
				StringCchPrintfA(szMsg, countof(szMsg), "TraceError [%s,%d]: message too long, skipping\r\n", szFile, iLine);
		}
		else
		{
			if (FAILED(hrError))
				StringCchPrintfA(szMsg, countof(szMsg), "TraceError 0x%x: message too long, skipping\r\n", hrError);
			else
				StringCchPrintfA(szMsg, countof(szMsg), "TraceError: message too long, skipping\r\n");
		}

		OutputDebugStringA(szMsg);
	}
}

#endif // DEBUG

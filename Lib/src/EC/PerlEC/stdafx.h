// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once

#ifndef STRICT
#define STRICT
#endif

// THESE VERSION NUMBERS SHOULD REFLECT THE OVERALL SETTINGS OF THE BUILD RATHER THAN BEING SET HERE.
// Modify the following defines if you have to target a platform prior to the ones specified below.
// Refer to MSDN for the latest info on corresponding values for different platforms.
//#ifndef WINVER				// Allow use of features specific to Windows 95 and Windows NT 4 or later.
//#define WINVER 0x0400		// Change this to the appropriate value to target Windows 98 and Windows 2000 or later.
//#endif

//#ifndef _WIN32_WINNT		// Allow use of features specific to Windows NT 4 or later.
//#define _WIN32_WINNT 0x0400	// Change this to the appropriate value to target Windows 2000 or later.
//#endif

//#ifndef _WIN32_WINDOWS		// Allow use of features specific to Windows 98 or later.
//#define _WIN32_WINDOWS 0x0410 // Change this to the appropriate value to target Windows Me or later.
//#endif

//#ifndef _WIN32_IE			// Allow use of features specific to IE 4.0 or later.
//#define _WIN32_IE 0x0400	// Change this to the appropriate value to target IE 5.0 or later.
//#endif

#define _ATL_APARTMENT_THREADED
#define _ATL_NO_AUTOMATIC_NAMESPACE

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS	// some CString constructors will be explicit

// turns off ATL's hiding of some common and often safely ignored warning messages
#define _ATL_ALL_WARNINGS
#include <afxwin.h>

#ifdef _RICHEDIT_VER
#undef _RICHEDIT_VER
#endif
#define _RICHEDIT_VER 0x0210

#include <afxdisp.h>

#include "resource.h"
#include <atlbase.h>
#include <atlcom.h>

using namespace ATL;
#include "ECEncConverter.h" // COM interface definition

#include "PXPerlWrap.h"
using namespace PXPerlWrap;

extern CPerlInterpreter interp;
extern HRESULT InitPerl();
extern void UnLoadPerl();
extern CString GetPXPerlWrapStdError();
extern CString GetPXPerlWrapStdOutput();
extern void SetDataEncodingMode(UTF8Mode eUtf8Mode);

extern void EnumRegKeys(LPCTSTR lpszRegKey, LPCTSTR lpszPrefix, CStringArray& astrKeys);
extern void WritePerlDistroPaths(CStringArray& astrPaths);
extern void WritePerlModulePaths(CStringArray& astrModules);

#define PERLEXPR_REG_ROOT           _T("SOFTWARE\\SIL\\SilEncConverters40\\ConvertersSupported\\SIL.PerlExpression")
#define PERLEXPR_REG_ADD_KEY(s)     _T("SOFTWARE\\SIL\\SilEncConverters40\\ConvertersSupported\\SIL.PerlExpression\\") ## s
#define PERLEXPR_PATHS_KEY          PERLEXPR_REG_ADD_KEY(_T("PerlPaths"))
#define PERLEXPR_MODULES_KEY        PERLEXPR_REG_ADD_KEY(_T("DefaultModules"))

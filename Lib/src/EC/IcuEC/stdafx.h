// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once
// Level 4 warnings are pretty drastic. This list of ones to disable is copied from generic/common.h
#pragma warning(disable: 4065) // Switch statement contains default but no case.
#pragma warning(disable: 4097) // typedef-name 'xxx' used as synonym for class-name 'yyy'.
#pragma warning(disable: 4100) // unreferenced formal parameter.
#pragma warning(disable: 4192) // automatically excluding while importing.
#pragma warning(disable: 4201) // nonstandard extension used : nameless struct/union.
#pragma warning(disable: 4290) // exception specification ignored.
#pragma warning(disable: 4310) // cast truncates constant value.
#pragma warning(disable: 4355) // 'this' used in base member initializer list.
#pragma warning(disable: 4505) // unreferenced local function has been removed.
#pragma warning(disable: 4510) // default constructor could not be generated - caused by
	// applying ComSmartPtr to a non-interface class.
#pragma warning(disable: 4511) // copy constructor could not be generated.
#pragma warning(disable: 4512) // assignment operator could not be generated.
#pragma warning(disable: 4610) // class 'xxx' can never be instantiated - user defined
	// constructor required - caused by applying ComSmartPtr to a non-interface class.
#pragma warning(disable: 4660) // template-class specialization is already instantiated.
#pragma warning(disable: 4701) // local variable 'xxx' may be used without being initialized.
	// We would like to keep this warning (4701) enabled but the compiler applies it in
	// places that are obviously OK.
#pragma warning(disable: 4702) // unreachable code. We would like to keep this warning (4702)
	// enabled but the compiler applies it in places that are obviously OK.
#pragma warning(disable: 4710) // not inlined.
#pragma warning(disable: 4786) // identifier truncated in debug info.
#pragma warning(disable: 4800) // forcing value to bool 'true' or 'false' (performance warning).

#ifndef STRICT
#define STRICT
#endif

// THESE VERSION NUMBERS SHOULD REFLECT THE OVERALL SETTINGS OF THE BUILD RATHER THAN BEING SET HERE.
// Modify the following defines if you have to target a platform prior to the ones specified below.
// Refer to MSDN for the latest info on corresponding values for different platforms.
//#ifndef WINVER				// Allow use of features specific to Windows 95 and Windows NT 4 or later.
//#define WINVER 0x0500		// Windows 2000 or later.
//#endif

//#ifndef _WIN32_WINNT		// Allow use of features specific to Windows NT 4 or later.
//#define _WIN32_WINNT 0x0500	// Windows 2000 or later.
//#endif

//#ifndef _WIN32_WINDOWS		// Allow use of features specific to Windows 98 or later.
//#define _WIN32_WINDOWS 0x0500 // Windows 2000 or later.
//#endif

//#ifndef _WIN32_IE			// Allow use of features specific to IE 4.0 or later.
//#define _WIN32_IE 0x0500	// Windows 2000 or later.
//#endif

#define _ATL_APARTMENT_THREADED
#define _ATL_NO_AUTOMATIC_NAMESPACE

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS	// some CString constructors will be explicit

// turns off ATL's hiding of some common and often safely ignored warning messages
#define _ATL_ALL_WARNINGS

#include <afxwin.h>
#include <afxdisp.h>

#include <comsvcs.h>

#include "resource.h"
#include <atlbase.h>
#include <atlcom.h>
#include <afxdlgs.h>
#include <afxdtctl.h>		// MFC support for Internet Explorer 4 Common Controls

using namespace ATL;
#include "ECEncConverter.h" // COM interface definition

#define IcuInactivityWarningTimeOut 60000   // 60 seconds of inactivity means clean up resources

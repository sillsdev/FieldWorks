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
#include <afxdisp.h>

#include <comsvcs.h>

#include "resource.h"
#include <atlbase.h>
#include <atlcom.h>
#include <afxdlgs.h>
#include <afxdtctl.h>		// MFC support for Internet Explorer 4 Common Controls
#include <afxext.h>

using namespace ATL;
#include "ECEncConverter.h" // COM interface definition

// make my own version of certain Python macros (since they aren't doing type checking or null pointer testing!)
#define MyPyTuple_Check(op)     ((!op) ? FALSE : PyObject_TypeCheck(op, &PyTuple_Type))
#define MyPyFunction_Check(op)  ((!op) ? FALSE : PyObject_TypeCheck(op, &PyFunction_Type))
#define MyPyCallable_Check(op)  ((!op) ? FALSE : PyCallable_Check(op))
#define MyPyClass_Check(op)     ((!op) ? FALSE : PyObject_TypeCheck(op, &PyClass_Type))
#define MyPyInstance_Check(op)  ((!op) ? FALSE : PyObject_TypeCheck(op, &PyInstance_Type))
#define MyPyString_Check(op)    ((!op) ? FALSE : PyObject_TypeCheck(op, &PyString_Type))
#define MyPyTraceBack_Check(op) ((!op) ? FALSE : PyObject_TypeCheck(op, &PyTraceBack_Type))
#define MyPyType_Check(op)      ((!op) ? FALSE : PyObject_TypeCheck(op, &PyType_Type))
#define MyPyDict_Check(op)      ((!op) ? FALSE : PyObject_TypeCheck(op, &PyDict_Type))
#define MyPyList_Check(op)      ((!op) ? FALSE : PyObject_TypeCheck(op, &PyList_Type))

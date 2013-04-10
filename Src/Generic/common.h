/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Common.h
Responsibility:
Last reviewed:

	Common generic header file.

	This header file checks for the following compile time switches:

		USING_MFC
-------------------------------------------------------------------------------*//*:End Ignore*/

#pragma once
#ifndef _COMMON_H_
#define _COMMON_H_ 1

// One of the following two lines should be commented out.
//#undef _UNICODE
#define _UNICODE

#ifdef _UNICODE
#define UNICODE
#else
#undef UNICODE
#endif

// In COM a LONG is always 32bit long, regardless of the architectur and OS. On 64bit Linux
// a long is 64bit long, so we can't use ULONG in COM interfaces and needs to be replaced with
// the 32bit equivalent. However, on Windows ULONG is a typedef and used in external header
// files, so we can't simply replace ULONG with UINT32. Instead we use the following defines:
#ifdef WIN32
#define UCOMINT32	ULONG
#define COMINT32	LONG
#else
#define UCOMINT32	UINT32
#define COMINT32	INT32
#endif

/***********************************************************************************************
	Set macros for DLL symbol visibility -- see http://gcc.gnu.org/wiki/Visibility
***********************************************************************************************/
#if defined(__GNUC__) && __GNUC__ >= 4
#define HAVE_GCCVISIBILITYPATCH 1
#endif

#ifdef _MSC_VER
  #if BUILDING_DLL
	#define DLLEXPORT __declspec(dllexport)
  #else
	#define DLLEXPORT __declspec(dllimport)
  #endif
  #define DLLLOCAL
#else
  #if HAVE_GCCVISIBILITYPATCH
	#define DLLEXPORT __attribute__ ((visibility("default")))
	#define DLLLOCAL __attribute__ ((visibility("hidden")))
  #else
	#define DLLEXPORT
	#define DLLLOCAL
  #endif
#endif


/***********************************************************************************************
	Set the ENTER_DLL macro appropriately. This should be called at all potential DLL
	entry points including COM methods.
***********************************************************************************************/
#ifndef ENTER_DLL
	#ifdef USING_MFC
		#define ENTER_DLL() AFX_MANAGE_STATE(AfxGetStaticModuleState())
	#else
		#define ENTER_DLL()
	#endif // USING_MFC
#endif // !ENTER_DLL


/***********************************************************************************************
	Windows / Framework headers.
***********************************************************************************************/
#if WIN32

#define STRICT 1
#ifdef USING_MFC
	#include <afxwin.h>
#else // !USING_MFC
	#define WINVER 0x0501
	#undef _WIN32_WINNT
	#define _WIN32_WINNT WINVER
	#include <windows.h>
	#include <shlwapi.h>
	#include <commctrl.h>
#endif // !USING_MFC

#include <ole2.h>

#else // !WIN32

#include "IcuCommon.h" // Enables general access to ICU (International Components for Unicode).
#include <Hacks.h>
#include <COM.h>
#include <COMInterfaces.h>
#include <COMInterfacesMore.h>
#include <winnt.h>
#include <WinBase.h>
#include <WinError.h>

#endif // !WIN32

// This keeps all the MIDL generated header files from including windows.h and ole2.h.
#define COM_NO_WINDOWS_H


/***********************************************************************************************
	Standard Headers.
***********************************************************************************************/
#ifdef WIN32
#include <malloc.h>
#endif // WIN32
#include <stdio.h>
#include <stdarg.h>
#ifdef WIN32
#include <sys\timeb.h>
#endif // WIN32
#include <time.h>
#include <math.h>
#include <limits.h>
#ifdef WIN32
#include <tchar.h>
#endif // WIN32

#ifdef WIN32
#include <crtdbg.h>
#endif // WIN32

#include <exception>
#include <new>
#if !WIN32
#include <string>
#include <cstdlib>
#endif // !WIN32

// These are needed for the Task Scheduler section of FwExplorer.
#ifdef WIN32
#include <initguid.h>
#pragma warning(push)
#pragma warning(disable: 4268) // 'const' static/global data initialized with compiler generated
// default constructor fills the object with zeros
// without disabling this warning for mstask.h the compiler complains on IID_ITask - obviously
// caused by __declspec(selectany)
#include <mstask.h>
#pragma warning(pop)

#include <Usp10.h> // For Uniscribe (currently only in Language DLL).

#pragma warning(push)
#pragma warning(disable: 4127)
#include "IcuCommon.h" // Enables general access to ICU (International Components for Unicode).
#pragma warning(pop)

#endif // WIN32

// Used for stack dumping and the like.

#ifdef WIN32
// imagehlp.h must be compiled with packing to eight-byte-boundaries,
// but does nothing to enforce that.
#pragma pack( push, before_imagehlp, 8 )
#include <imagehlp.h>
#include <Tlhelp32.h>
#pragma pack( pop, before_imagehlp )
#endif // WIN32

#ifdef WIN32
// Get the MS Text Services Framework interfaces.
#include <Msctf.h>
// This allows us to implement IAccessible (currently only in Views DLL).
#include <Oleacc.h>
#endif // WIN32

/***********************************************************************************************
	Debug related definitions.
***********************************************************************************************/
#include "debug.h"


/***********************************************************************************************
	Turn off the goofy warnings.
***********************************************************************************************/
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


/***********************************************************************************************
	Simple types.
***********************************************************************************************/
#if WIN32
typedef wchar_t  wchar;	// UTF-16
typedef int     wwchar;	// UTF-32
#else //WIN32
typedef OLECHAR  wchar;	// UTF-16
typedef wchar_t wwchar;	// UTF-32
#endif //WIN32
typedef char schar;
typedef unsigned char uchar;
typedef unsigned char byte;
typedef unsigned short ushort;
typedef unsigned int uint;
typedef unsigned long ulong;
typedef unsigned short UOLECHAR;
typedef __int64 int64; // Hungarian: lln
typedef unsigned __int64 uint64; // Hungarian llu


// TODO ShonK: Make generic use these where appropriate.
#ifdef UNICODE
typedef wchar achar;
#else // !UNICODE
typedef schar achar;
#endif // UNICODE

typedef achar * Psz;
typedef const achar * Pcsz;


/*************************************************************************************
	Offsets and addresses. AddrOf is useful for taking the address of an object
	that overloads the & operator.
*************************************************************************************/
#undef offsetof
#define offsetof(cls,fld) ((int)&((cls *)0)->fld)

#define addrsafe_offsetof(cls,fld) reinterpret_cast<int>(AddrOf(((cls *)0)->fld))

template<typename T> inline T * AddrOf(T & x)
{
	T * pt;
#ifdef _MSC_VER
	__asm
	{
		mov eax,x
		mov pt,eax
	}
#else
	pt = (T*)&(char&)x;
#endif
	return pt;
}


// This is to make a signed isizeof operator, otherwise we get tons of warnings about
// signed / unsigned mismatches.
#ifndef isizeof
	#define isizeof(T) ((int)sizeof(T))
#endif

#define SizeOfArray(rgt) (isizeof(rgt) / isizeof(rgt[0]))


/***********************************************************************************************
	Tests for valid strings and pointers.
***********************************************************************************************/
inline bool ValidPsz(const wchar *pszw)
{
	// Note: IsBadStringPtrW is not implemented on Win9x without the Microsoft Layer for
	// Unicode, but this is only used in Asserts, so it will not affect the end user, and
	// after Version 1 we will not be supporting Win98 anyway.
#if WIN32
	return pszw != NULL && !::IsBadStringPtrW(pszw, 0x10000000);
#else
	return pszw != NULL && !::IsBadReadPtr(pszw, 1);
#endif
}

inline bool ValidPsz(const schar *pszs)
{
#if WIN32
	return pszs != NULL && !::IsBadStringPtrA(pszs, 0x10000000);
#else
	return pszs != NULL && !::IsBadReadPtr(pszs, 1);
#endif
}

#if !WIN32
inline bool ValidPsz(const wchar_t *pszs)
{
	return pszs != NULL && !::IsBadReadPtr(pszs, 1);
}
#endif

template<typename T> inline bool ValidReadPtr(T *pt)
{
	return pt != NULL && !::IsBadReadPtr(pt, isizeof(T));
}

template<typename T> inline bool ValidWritePtr(T *pt)
{
	return pt != NULL && !::IsBadWritePtr(pt, isizeof(T));
}

inline bool ValidReadPtrSize(const void *pv, int cb)
{
	if (cb < 0)
		return false;
	if (cb == 0)
		return true;
	return pv != NULL && !::IsBadReadPtr(pv, cb);
}

inline bool ValidWritePtrSize(void *pv, int cb)
{
	if (cb < 0)
		return false;
	if (cb == 0)
		return true;
	return pv != NULL && !::IsBadWritePtr(pv, cb);
}

inline bool ValidBstr(BSTR bstr)
{
	if (!bstr || ::IsBadReadPtr((byte *)bstr - isizeof(int), isizeof(int) + isizeof(OLECHAR)))
		return false;
	int cb = ((int *)bstr)[-1];
	if (::IsBadReadPtr((byte *)bstr - isizeof(int), isizeof(int) + isizeof(OLECHAR) + cb))
		return false;
	if (cb < 0 || (cb % isizeof(OLECHAR)) != 0)
		return false;
	if (bstr[cb / isizeof(OLECHAR)])
		return false;
	return true;
}


/*************************************************************************************
	Cast "operators".
*************************************************************************************/
template<typename T> inline T * GetPtr(void *pv, int ib)
{
	return reinterpret_cast<T *>((byte *)pv + ib);
}


/*************************************************************************************
	Utility headers.
*************************************************************************************/
#include "UtilInt.h"
#include "UtilRect.h"
#include "UtilTime.h" // Before UtilString.
#include "GenericResource.h" // Before UtilString
#include "Throwable.h"
#include "UtilMem.h"
#include "UtilString.h"
#include "UtilCom.h"
#include "Mutex.h"
#include "GenSmartPtr.h"
#include "UtilSort.h"
#include "StackDumper.h" // Before SmartVariant.h, but after UtilCom.h
#include "SmartVariant.h"
#include "SmartBstr.h"
#include "UtilPersist.h"
#include "UtilRegistry.h"
#include "LinkedList.h"
#include "BinTree.h"
#include "Vector.h"
#include "ComVector.h"
#include "HashMap.h"
#include "ComHashMap.h"
#include "GpHashMap.h"
#include "Set.h"
#include "MultiMap.h"
#include "ComMultiMap.h"
#include "ModuleEntry.h"
#include "UtilXml.h"
#include "UtilTypeLib.h"
#include "GenericFactory.h"
#include "FileStrm.h"
#include "ResourceStrm.h"
#include "StringStrm.h"
#ifdef WIN32
#include "DataStream.h"
#endif // WIN32
#include "DataReader.h"
#include "DataWriter.h"
#include "TextProps.h"
#include "DispatchImpl.h"
#ifdef WIN32
#include "UtilFile.h"
#include <process.h>
#endif
#include "OleStringLiteral.h"

#include "CSupportErrorInfo.h"
#include "MakeDir.h"
#include "UtilSil.h"
#include "FwSettings.h"

#ifndef LF_FACESIZE // isn't defined on Linux
#define LF_FACESIZE 32
#endif

#endif // !_COMMON_H_

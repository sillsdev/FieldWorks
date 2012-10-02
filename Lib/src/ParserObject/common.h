/*----------------------------------------------------------------------------------------------
File: common.h
Responsibility: various.
Last reviewed: Not yet.

Owner: Summer Institute of Linguistics, 7500 West Camp Wisdom Road, Dallas,
Texas 75237. (972)708-7400.

Notice: All rights reserved Worldwide. This material contains the valuable properties of the
Summer Institute of Linguistics of Dallas, Texas, United States of America, embodying
substantial creative efforts and confidential information, ideas and expressions, NO PART of
which may be reproduced or transmitted in any form or by any means, electronic, mechanical, or
otherwise, including photographic and recording or in connection with any information storage or
retrieval system without the permission in writing from the Summer Institute of Linguistics.
COPYRIGHT (C) 1998 by the Summer Institute of Linguistics. All rights reserved.

Description :
	Common generic header file.

	This header file switches on:

		NO_EXCEPTIONS - don't include exception related declarations.
		CSP_ORIGINAL  - use the original definition of _com_ptr_t.
		NO_STL        - don't include STL related declarations.
		NO_STD_CRT    - don't include common standard C runtime headers.
----------------------------------------------------------------------------------------------*/
#ifndef COMMON_H
#define COMMON_H


/*************************************************************************************
	Warnings to turn off.
*************************************************************************************/
#pragma warning(disable: 4065) // Switch statement contains default but no case.
#pragma warning(disable: 4355) // 'this' used in base member initializer list.
#pragma warning(disable: 4786) // identifier truncated in debug info.
#pragma warning(disable: 4290) // exception specification ignored.
#pragma warning(disable: 4192) // automatically excluding while importing.
#pragma warning(disable: 4800) // forcing value to bool 'true' or 'false' (performance warning).
#pragma warning(disable: 4530) // get rid of the "C++ exceotuin handler used,
								//  but undwind semantics not enabled.

/*************************************************************************************
	Windows / Framework headers.
*************************************************************************************/
#define STRICT 1
#include <windows.h>
#include <ole2.h>

// This keeps all the MIDL generated header files from including windows.h and ole2.h.
#define COM_NO_WINDOWS_H


/*************************************************************************************
	Standard Headers.
*************************************************************************************/
#ifndef NO_STL
#include <vector>
#include <string>
#include <map>
#include <iostream>
#include <fstream>
#endif //!NO_STL


#ifdef STD_CRT
#include <malloc.h>
#include <stdio.h>
#endif //STD_CRT


/*************************************************************************************
	Debug related definitions.
*************************************************************************************/
#include "debug.h"


/*************************************************************************************
	Common simple types.
*************************************************************************************/
typedef wchar_t wchar;
typedef char schar;
typedef unsigned char uchar;
typedef unsigned char byte;
typedef unsigned short ushort;
typedef unsigned int uint;
typedef unsigned long ulong;
typedef unsigned short UOLECHAR;


/*************************************************************************************
	Tests for valid strings. TODO: Determine if IsBadStringPtrW is implemented on
	Win9x.
*************************************************************************************/
inline bool ValidPsz(const wchar * pszw)
	{ return !::IsBadStringPtrW(pszw, 0x10000000); }
inline bool ValidPsz(const schar * pszs)
	{ return !::IsBadStringPtrA(pszs, 0x10000000); }


/*************************************************************************************
	Offsets and addresses. AddrOf is useful for taking the address of an object
	that overloads the & operator.
*************************************************************************************/
#ifndef offsetof
#define offsetof(cls,fld) ((int)&((cls *)0)->fld)
#endif //!offsetof

#define addrsafe_offsetof(cls,fld) reinterpret_cast<int>(AddrOf(((cls *)0)->fld))

template<typename T> inline T * AddrOf(T & x)
{
	T *pt;
	__asm {
		mov eax,x
		mov pt,eax
	}
	return pt;
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
/* removed by jrg for use in the esces project
#include "Mutex.h"
#include "UtilCom.h"
#include "UtilString.h"
#include "UtilPersist.h"
#include "UtilRegistry.h"
#include "LinkedList.h"
#include "DllEntry.h"
#include "UtilTypeLib.h"
#include "GenericFactory.h"
*/
#endif //!COMMON_H

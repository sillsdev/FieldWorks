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


/*************************************************************************************
	Standard Headers.
*************************************************************************************/
#include <vector>
#include <string>
#include <iostream>
#include <fstream>

typedef std::string STR;


/*************************************************************************************
	Debug related definitions.
*************************************************************************************/
#include "debug.h"


/*************************************************************************************
	Common simple types.
*************************************************************************************/
typedef wchar_t wchar;
typedef signed char schar;
typedef unsigned char uchar;
typedef unsigned char byte;
typedef unsigned short ushort;
typedef unsigned int uint;
typedef unsigned long ulong;
typedef unsigned short UOLECHAR;

#include "Windows.h"
#include "xmlparse.h"
#include "Resource.h"
#include "Root.h"


inline void PrintHex(std::ostream & stm, ulong lu, int cb)
{
	static const char rgchHex[] = "0123456789ABCDEF";
	for (int inib = cb * 2; inib-- > 0; )
		stm << rgchHex[(lu >> (inib * 4)) & 0x0F];
}


inline void PrintGuid(std::ostream & stm, GUID & guid, char *pszDelim1, char *pszDelim2)
{
	PrintHex(stm, guid.Data1, 4);
	stm << pszDelim1;
	PrintHex(stm, guid.Data2, 2);
	stm << pszDelim1;
	PrintHex(stm, guid.Data3, 2);
	stm << pszDelim1;
	PrintHex(stm, guid.Data4[0], 1);
	stm << pszDelim2;
	PrintHex(stm, guid.Data4[1], 1);
	stm << pszDelim1;
	PrintHex(stm, guid.Data4[2], 1);
	for (int ib = 3; ib < 8; ib++) {
		stm << pszDelim2;
		PrintHex(stm, guid.Data4[ib], 1);
	}
}

template<typename T> inline T Max(T t1, T t2)
{
	if (t1 >= t2)
		return t1;
	return t2;
}


const int kmidMax = 1000;
const int kclidMax = 1000;
const int kflidMax = 1000;

extern STR g_strSearchPath;

extern ModuleParser g_mop;

#endif //!COMMON_H

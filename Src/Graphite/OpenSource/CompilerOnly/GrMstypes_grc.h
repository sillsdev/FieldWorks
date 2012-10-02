#ifndef _MSTYPES_H
#define _MSTYPES_H
/********
This header contains the MS Win32 specific types that are still used
throughout the Graphite code.

It is intended to allow Graphite to build on non-Win32 platforms.

  **Do NOT include this when building against WIN32**

  TSE - 15/07/2003
********/

#if defined(_WIN32)
  #error Do not include this header when building against Win32 APIs
#else

#include <algorithm>

using std::min;
using std::max;

//#define __int64 		long long
#define SUCCEEDED(Status) 	((HRESULT)(Status) >= 0)
#define FAILED(Status) 		((HRESULT)(Status)<0)
#define IS_ERROR(Status) 	((unsigned long)(Status) >> 31 == SEVERITY_ERROR)
#define __RPC_FAR

#define ETO_GLYPH_INDEX 	0x0010	// from WinGDI.h

#if 0 // def __GNUC__
#define PACKED __attribute__((packed))
#ifndef _stdcall
#define _stdcall __attribute__((stdcall))
#endif
#ifndef __stdcall
#define __stdcall __attribute__((stdcall))
#endif
#ifndef _cdecl
#define _cdecl __attribute__((cdecl))
#endif
#ifndef __cdecl
#define __cdecl __attribute__((cdecl))
#endif
#ifndef __declspec
#define __declspec(e) __attribute__((e))
#endif
#ifndef _declspec
#define _declspec(e) __attribute__((e))
#endif
#else
#define PACKED
#define _cdecl
#define __cdecl
#endif

#define WINAPI __stdcall

#if defined(GR_NAMESPACE)
namespace gr
{
#endif

typedef wchar_t  OLECHAR;
typedef const OLECHAR	*LPCOLESTR;
typedef OLECHAR			*BSTR;

typedef unsigned char	byte;

typedef unsigned short  WORD;
typedef unsigned int 	UINT;
typedef unsigned long 	DWORD;

typedef signed long	    HRESULT;
typedef DWORD 			COLORREF;
typedef unsigned short* LPWSTR;

inline const long InterlockedIncrement(long *const intr_lck) {
	return ++*intr_lck;
}

inline const long InterlockedDecrement(long *const intr_lck) {
	return --*intr_lck;
}

inline const int MulDiv(const int v, const int n, const int d) {
		return int(n < 0 ? double(v * n)/double(d) - 0.5 : double(v * n)/double(d) + 0.5);
}


#if defined(GR_NAMESPACE)
}
#endif

#define S_OK		0
#define E_FAIL		0x80004005L
#define E_POINTER 	0x80004003L
#define E_OUTOFMEMORY	0x8007000EL
#define E_UNEXPECTED	0x80000003L
#define E_INVALIDARG	0x80000002L
#define E_NOTIMPL	0x80000004L

#endif // defined(_WIN32)
#endif // include guard

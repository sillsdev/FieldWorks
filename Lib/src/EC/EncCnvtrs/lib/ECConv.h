#pragma once

#include <atlconv.h>

inline LPSTR WINAPI MyAtlW2AHelper(LPSTR lpa, int nChars, LPCWSTR lpw, int nWChars, UINT acp)
{
	ATLASSERT(lpw != NULL);
	ATLASSERT(lpa != NULL);

	lpa[0] = '\0';
	int naLen = WideCharToMultiByte(acp, 0, lpw, nWChars, lpa, nChars, NULL, NULL);
	lpa[naLen] = 0;
	return lpa;
}

inline LPWSTR WINAPI MyAtlA2WHelper(LPWSTR lpw, int nWChars, LPCSTR lpa, int nChars, UINT acp)
{
	ATLASSERT(lpa != NULL);
	ATLASSERT(lpw != NULL);

	lpw[0] = '\0';
	int nwLen = MultiByteToWideChar(acp, 0, lpa, nChars, lpw, nWChars);
	lpw[nwLen] = 0;
	return lpw;
}

// UTF-8 CP conversion to Wide (and vise-versa)
#define MyW2U8(lpw, lpwLen) (\
	((_lpw = lpw) == NULL) ? NULL : (\
		_convert = (lpwLen+1)*4,\
		MyAtlW2AHelper((LPSTR) alloca(_convert), _convert, _lpw, lpwLen, CP_UTF8)))
#define MyU82W(lpa, lpaLen) (\
	((_lpa = lpa) == NULL) ? NULL : (\
		_convert = (lpaLen+1),\
		MyAtlA2WHelper((LPWSTR) alloca(_convert*3), _convert, _lpa, lpaLen, CP_UTF8)))

// symbol CP conversion to Wide (and vise-versa)
#define MyW2S(lpw, lpwLen) (\
	((_lpw = lpw) == NULL) ? NULL : (\
		_convert = (lpwLen+1)*4,\
		MyAtlW2AHelper((LPSTR) alloca(_convert), _convert, _lpw, lpwLen, CP_SYMBOL)))
#define MyS2W(lpa, lpaLen) (\
	((_lpa = lpa) == NULL) ? NULL : (\
		_convert = (lpaLen+1),\
		MyAtlA2WHelper((LPWSTR) alloca(_convert*2), _convert, _lpa, lpaLen, CP_SYMBOL)))

// CP_ACP versions.
#define MyW2A(lpw, lpwLen) (\
	((_lpw = lpw) == NULL) ? NULL : (\
		_convert = (lpwLen+1)*4,\
		MyAtlW2AHelper((LPSTR) alloca(_convert), _convert, _lpw, lpwLen, _acp)))

#define MyA2W(lpa, lpaLen) (\
	((_lpa = lpa) == NULL) ? NULL : (\
		_convert = (lpaLen+1),\
		MyAtlA2WHelper((LPWSTR) alloca(_convert*2), _convert, _lpa, lpaLen, _acp)))

#define MyU82CW(lpa,lpaLen)   ((LPCWSTR)MyU82W(lpa,lpaLen))
#define MyW2CU8(lpw,lpwLen)   ((LPCSTR)MyW2U8(lpw,lpwLen))

#define MyS2CW(lpa,lpaLen)    ((LPCWSTR)MyS2W(lpa,lpaLen))
#define MyW2CS(lpw,lpwLen)    ((LPCSTR)MyW2S(lpw,lpwLen))

#define MyA2CW(lpa,lpaLen)    ((LPCWSTR)MyA2W(lpa,lpaLen))

#define MyW2CA(lpw,lpwLen)    ((LPCSTR)MyW2A(lpw,lpwLen))

#ifdef _UNICODE
	#define T2CU8   W2CU8
	#define T2U8    W2U8
	#define U82T    U82W
	#define U82CT   U82CW
	inline LPWSTR   OLE2W(LPOLESTR lp, int ) { return lp; }
#else   // _UNICODE
	inline LPCSTR T2CU8(LPCTSTR lp) { return lp; }  // Ansi *is* UTF-8
	inline LPSTR T2U8(LPTSTR lp)    { return lp; }
	inline LPCTSTR U82CT(LPCSTR lp) { return lp; }
	inline LPTSTR U82T(LPSTR lp)    { return lp; }
	inline LPWSTR   OLE2W(LPOLESTR lp, int ) { return lp; }
#endif

#define OLE2CU8 W2CU8
#define OLE2U8  W2U8
#define U82COLE U82CW
#define U82OLE  U82W
#define MyA2OLE MyA2W
#define MyOLE2A MyW2A

// Py #define OLE2CT(lpo) W2CT(lpo)

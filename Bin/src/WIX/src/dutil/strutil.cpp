//-------------------------------------------------------------------------------------------------
// <copyright file="strutil.cpp" company="Microsoft">
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
//    String helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

/********************************************************************
StrAlloc - allocates or reuses dynamic string memory

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
extern "C" HRESULT DAPI DAPI StrAlloc(
	__inout LPWSTR* ppwz,
	__in DWORD_PTR cch
	)
{
	Assert(ppwz && cch);
	HRESULT hr = S_OK;
	LPWSTR pwz = NULL;

	if (cch >= MAXDWORD / sizeof(WCHAR))
	{
		ExitOnFailure1(hr = E_OUTOFMEMORY, "Not enough memory to allocate string of size: %d", cch);
	}

	if (*ppwz)
		pwz = static_cast<LPWSTR>(MemReAlloc(*ppwz, sizeof(WCHAR) * cch, FALSE));
	else
		pwz = static_cast<LPWSTR>(MemAlloc(sizeof(WCHAR) * cch, TRUE));

	ExitOnNull1(pwz, hr, E_OUTOFMEMORY, "failed to allocate string, len: %d", cch);

	*ppwz = pwz;
LExit:
	return hr;
}


/********************************************************************
StrAnsiAlloc - allocates or reuses dynamic ANSI string memory

NOTE: caller is responsible for freeing ppsz even if function fails
********************************************************************/
extern "C" HRESULT DAPI StrAnsiAlloc(
	__inout LPSTR* ppsz,
	__in DWORD_PTR cch
	)
{
	Assert(ppsz && cch);
	HRESULT hr = S_OK;
	LPSTR psz = NULL;

	if (cch >= MAXDWORD / sizeof(WCHAR))
	{
		ExitOnFailure1(hr = E_OUTOFMEMORY, "Not enough memory to allocate string of size: %d", cch);
	}

	if (*ppsz)
		psz = static_cast<LPSTR>(MemReAlloc(*ppsz, sizeof(CHAR) * cch, FALSE));
	else
		psz = static_cast<LPSTR>(MemAlloc(sizeof(CHAR) * cch, TRUE));

	ExitOnNull1(psz, hr, E_OUTOFMEMORY, "failed to allocate string, len: %d", cch);

	*ppsz = psz;
LExit:
	return hr;
}


/********************************************************************
StrAllocString - allocates or reuses dynamic string memory and copies in an existing string

NOTE: caller is responsible for freeing ppwz even if function fails
NOTE: cchSource does not have to equal the length of wzSource
NOTE: if cchSource == 0, length of wzSource is used instead
********************************************************************/
extern "C" HRESULT DAPI StrAllocString(
	__inout LPWSTR* ppwz,
	__in LPCWSTR wzSource,
	__in DWORD_PTR cchSource
	)
{
	Assert(ppwz && wzSource); // && *wzSource);

	HRESULT hr = S_OK;
	DWORD_PTR cch = 0;

	if (*ppwz)
	{
		cch = MemSize(*ppwz);  // get the count in bytes so we can check if it failed (returns -1)
		if (-1 == cch)
			ExitOnFailure(hr = E_INVALIDARG, "failed to get size of destination string");
		cch /= sizeof(WCHAR);  //convert the count in bytes to count in characters
	}

	if (0 == cchSource)
		cchSource = lstrlenW(wzSource);

	if (cch < cchSource + 1)
	{
		cch = cchSource + 1;   // add one for the null terminator
		hr = StrAlloc(ppwz, cch);
		ExitOnFailure1(hr, "failed to allocate string from string: %S", wzSource);
	}

	// copy everything (the NULL terminator will be included)
	hr = StringCchCopyNExW(*ppwz, cch, wzSource, cchSource, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);

LExit:
	return hr;
}


/********************************************************************
StrAnsiAllocString - allocates or reuses dynamic ANSI string memory and copies in an existing string

NOTE: caller is responsible for freeing ppsz even if function fails
NOTE: cchSource must equal the length of wzSource (not including the NULL terminator)
NOTE: if cchSource == 0, length of wzSource is used instead
********************************************************************/
extern "C" HRESULT DAPI StrAnsiAllocString(
	__inout LPSTR* ppsz,
	__in LPCWSTR wzSource,
	__in DWORD_PTR cchSource,
	__in UINT uiCodepage
	)
{
	Assert(ppsz && wzSource);

	HRESULT hr = S_OK;
	LPSTR psz = NULL;
	DWORD_PTR cch = 0;
	DWORD_PTR cchDest = cchSource; // at least enough

	if (*ppsz)
	{
		cch = MemSize(*ppsz);  // get the count in bytes so we can check if it failed (returns -1)
		if (-1 == cch)
			ExitOnFailure(hr = E_INVALIDARG, "failed to get size of destination string");
		cch /= sizeof(CHAR);  //convert the count in bytes to count in characters
	}

	if (0 == cchSource)
	{
		cchDest = ::WideCharToMultiByte(uiCodepage, 0, wzSource, -1, NULL, 0, NULL, NULL);
		if (0 == cchDest)
		{
			ExitWithLastError1(hr, "failed to get required size for conversion to ANSI: %S", wzSource);
		}

		--cchDest; // subtract one because WideChageToMultiByte includes space for the NULL terminator that we track below
	}
	else if (L'\0' == wzSource[cchSource]) // if the source already had a null terminator, don't count that in the character count because we track it below
	{
		cchDest = cchSource - 1;
	}

	if (cch < cchDest + 1)
	{
		cch = cchDest + 1;   // add one for the NULL terminator
		if (cch >= MAXDWORD / sizeof(WCHAR))
		{
			ExitOnFailure1(hr = E_OUTOFMEMORY, "Not enough memory to allocate string of size: %d", cch);
		}

		if (*ppsz)
		{
			psz = static_cast<LPSTR>(MemReAlloc(*ppsz, sizeof(CHAR) * cch, TRUE));
		}
		else
		{
			psz = static_cast<LPSTR>(MemAlloc(sizeof(CHAR) * cch, TRUE));
		}
		ExitOnNull1(psz, hr, E_OUTOFMEMORY, "failed to allocate string, len: %d", cch);

		*ppsz = psz;
	}

	if (0 == ::WideCharToMultiByte(uiCodepage, 0, wzSource, 0 == cchSource ? -1 : (int)cchSource, *ppsz, (int)cch, NULL, NULL))
	{
		ExitWithLastError1(hr, "failed to convert to ansi: %S", wzSource);
	}
	(*ppsz)[cchDest] = L'\0';

LExit:
	return hr;
}


/********************************************************************
StrAllocStringAnsi - allocates or reuses dynamic string memory and copies in an existing ANSI string

NOTE: caller is responsible for freeing ppwz even if function fails
NOTE: cchSource must equal the length of wzSource (not including the NULL terminator)
NOTE: if cchSource == 0, length of wzSource is used instead
********************************************************************/
extern "C" HRESULT DAPI StrAllocStringAnsi(
	__inout LPWSTR* ppwz,
	__in LPCSTR szSource,
	__in DWORD_PTR cchSource,
	__in UINT uiCodepage
	)
{
	Assert(ppwz && szSource);

	HRESULT hr = S_OK;
	LPWSTR pwz = NULL;
	DWORD_PTR cch = 0;
	DWORD_PTR cchDest = cchSource;  // at least enough

	if (*ppwz)
	{
		cch = MemSize(*ppwz);  // get the count in bytes so we can check if it failed (returns -1)
		if (-1 == cch)
		{
			ExitOnFailure(hr = E_INVALIDARG, "failed to get size of destination string");
		}
		cch /= sizeof(WCHAR);  //convert the count in bytes to count in characters
	}

	if (0 == cchSource)
	{
		cchDest = ::MultiByteToWideChar(uiCodepage, 0, szSource, -1, NULL, 0);
		if (0 == cchDest)
		{
			ExitWithLastError1(hr, "failed to get required size for conversion to unicode: %s", szSource);
		}

		--cchDest; //subtract one because MultiByteToWideChar includes space for the NULL terminator that we track below
	}
	else if (L'\0' == szSource[cchSource]) // if the source already had a null terminator, don't count that in the character count because we track it below
	{
		cchDest = cchSource - 1;
	}

	if (cch < cchDest + 1)
	{
		cch = cchDest + 1;
		if (cch >= MAXDWORD / sizeof(WCHAR))
		{
			ExitOnFailure1(hr = E_OUTOFMEMORY, "Not enough memory to allocate string of size: %d", cch);
		}

		if (*ppwz)
		{
			pwz = static_cast<LPWSTR>(MemReAlloc(*ppwz, sizeof(WCHAR) * cch, TRUE));
		}
		else
		{
			pwz = static_cast<LPWSTR>(MemAlloc(sizeof(WCHAR) * cch, TRUE));
		}

		ExitOnNull1(pwz, hr, E_OUTOFMEMORY, "failed to allocate string, len: %d", cch);

		*ppwz = pwz;
	}

	if (0 == ::MultiByteToWideChar(uiCodepage, 0, szSource, 0 == cchSource ? -1 : (int)cchSource, *ppwz, (int)cch))
	{
		ExitWithLastError1(hr, "failed to convert to unicode: %s", szSource);
	}
	(*ppwz)[cchDest] = L'\0';

LExit:
	return hr;
}


/********************************************************************
StrAllocPrefix - allocates or reuses dynamic string memory and
				 prefixes a string

NOTE: caller is responsible for freeing ppwz even if function fails
NOTE: cchPrefix does not have to equal the length of wzPrefix
NOTE: if cchPrefix == 0, length of wzPrefix is used instead
********************************************************************/
extern "C" HRESULT DAPI StrAllocPrefix(
	__inout LPWSTR* ppwz,
	__in LPCWSTR wzPrefix,
	__in DWORD_PTR cchPrefix
	)
{
	Assert(ppwz && wzPrefix);

	HRESULT hr = S_OK;
	DWORD_PTR cch = 0;
	DWORD_PTR cchLen = 0;

	if (*ppwz)
	{
		cch = MemSize(*ppwz);  // get the count in bytes so we can check if it failed (returns -1)
		if (-1 == cch)
			ExitOnFailure(hr = E_INVALIDARG, "failed to get size of destination string");
		cch /= sizeof(WCHAR);  //convert the count in bytes to count in characters

		StringCchLengthW(*ppwz, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchLen));
	}

	Assert(cchLen <= cch);

	if (0 == cchPrefix)
	{
		StringCchLengthW(wzPrefix, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchPrefix));
	}

	if (cch - cchLen < cchPrefix + 1)
	{
		cch = cchPrefix + cchLen + 1;
		hr = StrAlloc(ppwz, cch);
		ExitOnFailure1(hr, "failed to allocate string from string: %S", wzPrefix);
	}

	if (*ppwz)
	{
		DWORD_PTR cb = cch * sizeof(WCHAR);
		DWORD_PTR cbPrefix = cchPrefix * sizeof(WCHAR);

		memmove(*ppwz + cchPrefix, *ppwz, cb - cbPrefix);
		memcpy(*ppwz, wzPrefix, cbPrefix);
	}
	else
	{
		ExitOnFailure(hr = E_UNEXPECTED, "for some reason our buffer is still null");
	}

LExit:
	return hr;
}


/********************************************************************
StrAllocConcat - allocates or reuses dynamic string memory and adds an existing string

NOTE: caller is responsible for freeing ppwz even if function fails
NOTE: cchSource does not have to equal the length of wzSource
NOTE: if cchSource == 0, length of wzSource is used instead
********************************************************************/
extern "C" HRESULT DAPI StrAllocConcat(
	__inout LPWSTR* ppwz,
	__in LPCWSTR wzSource,
	__in DWORD_PTR cchSource
	)
{
	Assert(ppwz && wzSource); // && *wzSource);

	HRESULT hr = S_OK;
	DWORD_PTR cch = 0;
	DWORD_PTR cchLen = 0;

	if (*ppwz)
	{
		cch = MemSize(*ppwz);  // get the count in bytes so we can check if it failed (returns -1)
		if (-1 == cch)
			ExitOnFailure(hr = E_INVALIDARG, "failed to get size of destination string");
		cch /= sizeof(WCHAR);  //convert the count in bytes to count in characters

		StringCchLengthW(*ppwz, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchLen));
	}

	Assert(cchLen <= cch);

	if (0 == cchSource)
		StringCchLengthW(wzSource, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchSource));

	if (cch - cchLen < cchSource + 1)
	{
		cch = (cchSource + cchLen + 1) * 2;
		hr = StrAlloc(ppwz, cch);
		ExitOnFailure1(hr, "failed to allocate string from string: %S", wzSource);
	}

	if (*ppwz)
		hr = StringCchCatNExW(*ppwz, cch, wzSource, cchSource, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
	else
		ExitOnFailure(hr = E_UNEXPECTED, "for some reason our buffer is still null");

LExit:
	return hr;
}


/********************************************************************
StrAllocFormatted - allocates or reuses dynamic string memory and formats it

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
extern "C" HRESULT DAPI StrAllocFormatted(
	__inout LPWSTR* ppwz,
	__in LPCWSTR wzFormat,
	...
	)
{
	Assert(ppwz && wzFormat && *wzFormat);

	HRESULT hr = S_OK;
	va_list args;

	va_start(args, wzFormat);
	hr = StrAllocFormattedArgs(ppwz, wzFormat, args);
	va_end(args);

	return hr;
}


/********************************************************************
StrAnsiAllocFormatted - allocates or reuses dynamic ANSI string memory and formats it

NOTE: caller is responsible for freeing ppsz even if function fails
********************************************************************/
extern "C" HRESULT DAPI StrAnsiAllocFormatted(
	__inout LPSTR* ppsz,
	__in LPCSTR szFormat,
	...
	)
{
	Assert(ppsz && szFormat && *szFormat);

	HRESULT hr = S_OK;
	va_list args;

	va_start(args, szFormat);
	hr = StrAnsiAllocFormattedArgs(ppsz, szFormat, args);
	va_end(args);

	return hr;
}


/********************************************************************
StrAllocFormattedArgs - allocates or reuses dynamic string memory
and formats it with the passed in args

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
extern "C" HRESULT DAPI StrAllocFormattedArgs(
	__inout LPWSTR* ppwz,
	__in LPCWSTR wzFormat,
	__in va_list args
	)
{
	Assert(ppwz && wzFormat && *wzFormat);

	HRESULT hr = S_OK;
	DWORD_PTR cch = 0;
	LPWSTR pwzOriginal = NULL;
	DWORD_PTR cchOriginal = 0;

	if (*ppwz)
	{
		cch = MemSize(*ppwz);  // get the count in bytes so we can check if it failed (returns -1)
		if (-1 == cch)
			ExitOnFailure(hr = E_INVALIDARG, "failed to get size of destination string");
		cch /= sizeof(WCHAR);  //convert the count in bytes to count in characters

		cchOriginal = lstrlenW(*ppwz);
	}

	if (0 == cch)   // if there is no space in the string buffer
	{
		cch = 256;
		hr = StrAlloc(ppwz, cch);
		ExitOnFailure1(hr, "failed to allocate string to format: %S", wzFormat);
	}

	// format the message (grow until it fits or there is a failure)
	do
	{
		hr = StringCchVPrintfW(*ppwz, cch, wzFormat, args);
		if (STRSAFE_E_INSUFFICIENT_BUFFER == hr)
		{
			if (!pwzOriginal)
			{
				// this allows you to pass the original string as a formatting argument and not crash
				// save the original string and free it after the printf is complete
				pwzOriginal = *ppwz;
				*ppwz = NULL;
				// StringCchVPrintfW starts writing to the string...
				// NOTE: this hack only works with sprintf(&pwz, "%s ...", pwz, ...);
				pwzOriginal[cchOriginal] = 0;
			}
			cch *= 2;
			hr = StrAlloc(ppwz, cch);
			ExitOnFailure1(hr, "failed to allocate string to format: %S", wzFormat);
			hr = S_FALSE;
		}
	} while (S_FALSE == hr);
	ExitOnFailure(hr, "failed to format string");

LExit:
	ReleaseStr((void*) pwzOriginal);
	return hr;
}


/********************************************************************
StrAnsiAllocFormattedArgs - allocates or reuses dynamic ANSI string memory
and formats it with the passed in args

NOTE: caller is responsible for freeing ppsz even if function fails
********************************************************************/
extern "C" HRESULT DAPI StrAnsiAllocFormattedArgs(
	__inout LPSTR* ppsz,
	__in LPCSTR szFormat,
	__in va_list args
	)
{
	Assert(ppsz && szFormat && *szFormat);

	HRESULT hr = S_OK;
	DWORD_PTR cch = *ppsz ? MemSize(*ppsz) / sizeof(CHAR) : 0;
	LPSTR pszOriginal = NULL;
	DWORD cchOriginal = 0;

	if (*ppsz)
	{
		cch = MemSize(*ppsz);  // get the count in bytes so we can check if it failed (returns -1)
		if (-1 == cch)
			ExitOnFailure(hr = E_INVALIDARG, "failed to get size of destination string");
		cch /= sizeof(CHAR);  //convert the count in bytes to count in characters

		cchOriginal = lstrlenA(*ppsz);
	}

	if (0 == cch)   // if there is no space in the string buffer
	{
		cch = 256;
		hr = StrAnsiAlloc(ppsz, cch);
		ExitOnFailure1(hr, "failed to allocate string to format: %s", szFormat);
	}

	// format the message (grow until it fits or there is a failure)
	do
	{
		hr = StringCchVPrintfA(*ppsz, cch, szFormat, args);
		if (STRSAFE_E_INSUFFICIENT_BUFFER == hr)
		{
			if (!pszOriginal)
			{
				// this allows you to pass the original string as a formatting argument and not crash
				// save the original string and free it after the printf is complete
				pszOriginal = *ppsz;
				*ppsz = NULL;
				// StringCchVPrintfW starts writing to the string...
				// NOTE: this hack only works with sprintf(&pwz, "%s ...", pwz, ...);
				pszOriginal[cchOriginal] = 0;
			}
			cch *= 2;
			hr = StrAnsiAlloc(ppsz, cch);
			ExitOnFailure1(hr, "failed to allocate string to format: %S", szFormat);
			hr = S_FALSE;
		}
	} while (S_FALSE == hr);
	ExitOnFailure(hr, "failed to format string");

LExit:
	ReleaseStr((void*) pszOriginal);
	return hr;
}


/********************************************************************
StrMaxLength - returns maximum number of characters that can be stored in dynamic string p

NOTE:  assumes Unicode string
********************************************************************/
extern "C" HRESULT DAPI StrMaxLength(
	__in LPVOID p,
	__out DWORD_PTR* pcch
	)
{
	Assert(pcch);
	HRESULT hr = S_OK;

	if (p)
	{
		*pcch = MemSize(p);   // get size of entire buffer
		if (-1 == *pcch)
		{
			ExitFunction1(hr = E_FAIL);
		}

		*pcch /= sizeof(WCHAR);   // reduce to count of characters
	}
	else
	{
		*pcch = 0;
	}
	Assert(S_OK == hr);

LExit:
	return hr;
}


/********************************************************************
StrSize - returns count of bytes in dynamic string p

********************************************************************/
extern "C" HRESULT DAPI StrSize(
	__in LPVOID p,
	__out DWORD_PTR* pcb
	)
{
	Assert(p && pcb);
	HRESULT hr = S_OK;

	*pcb = MemSize(p);
	if (-1 == *pcb)
	{
		hr = E_FAIL;
	}

	return hr;
}

/********************************************************************
StrFree - releases dynamic string memory allocated by any StrAlloc*() functions

********************************************************************/
extern "C" HRESULT DAPI StrFree(
	__in LPVOID p
	)
{
	Assert(p);

	HRESULT hr = MemFree(p);
	ExitOnFailure(hr, "failed to free string");
LExit:
	return hr;
}


/****************************************************************************
StrCurrentTime - gets the current time in string format

****************************************************************************/
extern "C" HRESULT DAPI StrCurrentTime(
	__inout LPWSTR* ppwz,
	__in BOOL fGMT
	)
{
	SYSTEMTIME st;

	if (fGMT)
	{
		::GetSystemTime(&st);
	}
	else
	{
		SYSTEMTIME stGMT;
		TIME_ZONE_INFORMATION tzi;

		::GetTimeZoneInformation(&tzi);
		::GetSystemTime(&stGMT);
		::SystemTimeToTzSpecificLocalTime(&tzi, &stGMT, &st);
	}

	return StrAllocFormatted(ppwz, L"%02d:%02d:%02d", st.wHour, st.wMinute, st.wSecond);
}


/****************************************************************************
StrCurrentDateTime - gets the current date and time in string format

****************************************************************************/
extern "C" HRESULT DAPI StrCurrentDateTime(
	__inout LPWSTR* ppwz,
	__in BOOL fGMT
	)
{
	HRESULT hr = S_OK;
	SYSTEMTIME st;

	if (fGMT)
	{
		::GetSystemTime(&st);
	}
	else
	{
		SYSTEMTIME stGMT;
		TIME_ZONE_INFORMATION tzi;

		::GetTimeZoneInformation(&tzi);
		::GetSystemTime(&stGMT);
		::SystemTimeToTzSpecificLocalTime(&tzi, &stGMT, &st);
	}

	return StrAllocFormatted(ppwz, L"%04d/%02d/%02d %02d:%02d:%02d", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond);
}


static inline BYTE HexCharToByte(
	__in WCHAR wc
	)
{
	Assert(L'0' <= wc && wc <= L'9' || L'a' <= wc && wc <= L'f' || L'A' <= wc && wc <= L'F');  // make sure wc is a hex character

	BYTE b;
	if (L'0' <= wc && wc <= L'9')
		b = wc - L'0';
	else if ('a' <= wc && wc <= 'f')
		b = wc - L'0' - (L'a' - L'9' - 1);
	else  // must be (L'A' <= wc && wc <= L'F')
		b = wc - L'0' - (L'A' - L'9' - 1);

	Assert(0 <= b && b <= 15);
	return b;
}


/****************************************************************************
StrHexEncode - converts an array of bytes to a text string

NOTE: wzDest must have space for cbSource * 2 + 1 characters
****************************************************************************/
extern "C" HRESULT DAPI StrHexEncode(
	__in_ecount(cbSource) const BYTE* pbSource,
	__in DWORD_PTR cbSource,
	__out_ecount(cchDest) LPWSTR wzDest,
	__in DWORD_PTR cchDest
	)
{
	Assert(pbSource && wzDest);

	HRESULT hr = S_OK;
	DWORD i;
	BYTE b;

	if (cchDest < 2 * cbSource + 1)
		ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER));

	for (i = 0;  i < cbSource;  i++)
	{
		b = (*pbSource) >> 4;
		*(wzDest++) = (WCHAR)(L'0' + b + ((b < 10) ? 0 : L'A'-L'9'-1));
		b = (*pbSource) & 0xF;
		*(wzDest++) = (WCHAR)(L'0' + b + ((b < 10) ? 0 : L'A'-L'9'-1));

		pbSource++;
	}

	*wzDest = 0;
LExit:
	return hr;
}


/****************************************************************************
StrHexDecode - converts a string of text to array of bytes

NOTE: wzSource must contain even number of characters
****************************************************************************/
extern "C" HRESULT DAPI StrHexDecode(
	__in LPCWSTR wzSource,
	__out_bcount(cbDest) BYTE* pbDest,
	__in DWORD_PTR cbDest
	)
{
	Assert(wzSource && pbDest);

	HRESULT hr = S_OK;
	DWORD cchSource = lstrlenW(wzSource);
	DWORD i;
	BYTE b;

	Assert(0 == cchSource % 2);
	if (cbDest < cchSource * sizeof(WCHAR))
		ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER));

	for (i = 0;  i < cchSource;  i += 2)
	{
		b = HexCharToByte(*wzSource++);
		(*pbDest) = b << 4;

		b = HexCharToByte(*wzSource++);
		(*pbDest) |= b & 0xF;

		pbDest++;
	}

LExit:
	return hr;
}


/****************************************************************************
Base85 encoding/decoding data

****************************************************************************/
const WCHAR Base85EncodeTable[] = L"!%'()*+,-./0123456789:;?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_abcdefghijklmnopqrstuvwxyz{|}~";

const BYTE Base85DecodeTable[256] =
{
	85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
	85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
	85,  0, 85, 85, 85,  1, 85,  2,  3,  4,  5,  6,  7,  8,  9, 10,
	11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 85, 85, 85, 23,
	24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39,
	40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 85, 52, 53, 54,
	85, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69,
	70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85,
	85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
	85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
	85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
	85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
	85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
	85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
	85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
	85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85
};

const UINT Base85PowerTable[4] = { 1, 85, 85*85, 85*85*85 };


/****************************************************************************
StrAllocBase85Encode - converts an array of bytes into an XML compatible string

****************************************************************************/
extern "C" HRESULT DAPI StrAllocBase85Encode(
	__in_bcount(cbSource) const BYTE* pbSource,
	__in DWORD_PTR cbSource,
	__inout LPWSTR* pwzDest
	)
{
	HRESULT hr = S_OK;
	DWORD_PTR cchDest = 0;
	LPWSTR wzDest;
	DWORD_PTR iSource = 0;
	DWORD_PTR iDest = 0;

	if (!pwzDest || !pbSource)
	{
		return E_INVALIDARG;
	}

	// calc actual size of output
	cchDest = cbSource / 4;
	cchDest += cchDest * 4;
	if (cbSource & 3)
	{
		cchDest += (cbSource & 3) + 1;
	}
	cchDest++; // add room for null terminator

	hr = StrAlloc(pwzDest, cchDest);
	ExitOnFailure(hr, "failed to allocate destination string");

	wzDest = *pwzDest;

	// first, encode full words
	for (iSource = 0, iDest = 0; (iSource + 4 < cbSource) && (iDest + 5 < cchDest); iSource += 4, iDest += 5)
	{
		int n = pbSource[iSource] + (pbSource[iSource + 1] << 8) + (pbSource[iSource + 2] << 16) + (pbSource[iSource + 3] << 24);
		int k = n / 85;

		wzDest[iDest] = Base85EncodeTable[n - k*85];
		n = k / 85;

		wzDest[iDest + 1] = Base85EncodeTable[k - n*85];
		k = n / 85;

		wzDest[iDest + 2] = Base85EncodeTable[n - k*85];
		n = k / 85;

		wzDest[iDest + 3] = Base85EncodeTable[k - n*85];
		wzDest[iDest + 4] = Base85EncodeTable[n];
	}

	// encode any remaining bytes
	if (iSource < cbSource)
	{
		int n = 0;
		for (DWORD i = 0; iSource + i < cbSource; ++i)
		{
			n += pbSource[iSource + i] << (i << 3);
		}

		for (/* iSource already initialized */; iSource < cbSource && iDest < cchDest; ++iSource, ++iDest)
		{
			int k = n / 85;
			wzDest[iDest] = Base85EncodeTable[n - k*85];
			n = k;
		}

		wzDest[iDest] = Base85EncodeTable[n];
		++iDest;
	}
	Assert(iSource == cbSource);
	Assert(iDest == cchDest - 1);

	wzDest[iDest] = L'\0';
	hr = S_OK;

LExit:
	return hr;
}


/****************************************************************************
StrAllocBase85Decode - converts a string of text to array of bytes

NOTE: Use MemFree() to release the allocated stream of bytes
****************************************************************************/
extern "C" HRESULT DAPI StrAllocBase85Decode(
	__in LPCWSTR wzSource,
	__out BYTE** ppbDest,
	__out DWORD_PTR* pcbDest
	)
{
	HRESULT hr = S_OK;
	DWORD_PTR cchSource = lstrlenW(wzSource);
	DWORD_PTR i, n, k;

	BYTE* pbDest;
	DWORD_PTR cbDest;

	if (!wzSource || !ppbDest || !pcbDest)
	{
		return E_INVALIDARG;
	}

	// evaluate size of output and check it
	k = cchSource / 5;
	cbDest = k << 2;
	k = cchSource - k * 5;
	if (k)
	{
		if (1 == k)
		{
			// decode error -- encoded size cannot equal 1 mod 5
			return E_UNEXPECTED;
		}

		cbDest += k - 1;
	}

	*ppbDest = static_cast<BYTE*>(MemAlloc(cbDest, FALSE));
	ExitOnNull(*ppbDest, hr, E_OUTOFMEMORY, "failed allocate memory to decode the string");

	pbDest = *ppbDest;
	*pcbDest = cbDest;

	// decode full words first
	while (5 <= cchSource)
	{
		k = Base85DecodeTable[wzSource[0]];
		if (85 == k)
		{
			// illegal symbol
			return E_UNEXPECTED;
		}
		n = k;

		k = Base85DecodeTable[wzSource[1]];
		if (85 == k)
		{
			// illegal symbol
			return E_UNEXPECTED;
		}
		n += k * 85;

		k = Base85DecodeTable[wzSource[2]];
		if (85 == k)
		{
			// illegal symbol
			return E_UNEXPECTED;
		}
		n += k * (85 * 85);

		k = Base85DecodeTable[wzSource[3]];
		if (85 == k)
		{
			// illegal symbol
			return E_UNEXPECTED;
		}
		n += k * (85 * 85 * 85);

		k = Base85DecodeTable[wzSource[4]];
		if (85 == k)
		{
			// illegal symbol
			return E_UNEXPECTED;
		}
		k *= (85 * 85 * 85 * 85);

		// if (k + n > (1u << 32)) <=> (k > ~n) then decode error
		if (k > ~n)
		{
			// overflow
			return E_UNEXPECTED;
		}

		n += k;

		pbDest[0] = (BYTE) n;
		pbDest[1] = (BYTE) (n >> 8);
		pbDest[2] = (BYTE) (n >> 16);
		pbDest[3] = (BYTE) (n >> 24);

		wzSource += 5;
		pbDest += 4;
		cchSource -= 5;
	}

	if (cchSource)
	{
		n = 0;
		for (i = 0; i < cchSource; ++i)
		{
			k = Base85DecodeTable[wzSource[i]];
			if (85 == k)
			{
				// illegal symbol
				return E_UNEXPECTED;
			}

			n += k * Base85PowerTable[i];
		}

		for (i = 1; i < cchSource; ++i)
		{
			*pbDest++ = (BYTE)n;
			n >>= 8;
		}

		if (0 != n)
		{
			// decode error
			return E_UNEXPECTED;
		}
	}

	hr = S_OK;
LExit:
	return hr;
}


/****************************************************************************
MultiSzLen - calculates the length of a MULTISZ string including all nulls
including the double null terminator at the end of the MULTISZ.

NOTE: returns 0 if the multisz in not properly terminated with two nulls
****************************************************************************/
extern "C" HRESULT DAPI MultiSzLen(
	__in LPCWSTR pwzMultiSz,
	__out DWORD_PTR* pcch
	)
{
	Assert(pcch);

	HRESULT hr = S_OK;
	LPCWSTR wz = pwzMultiSz;
	DWORD_PTR dwMaxSize = 0;

	hr = StrMaxLength((LPVOID)pwzMultiSz, &dwMaxSize);
	ExitOnFailure(hr, "failed to get the max size of a string while calculating MULTISZ length");

	*pcch = 0;
	while (*pcch < dwMaxSize)
	{
		if (L'\0' == *wz && L'\0' == *(wz + 1))
			break;

		wz++;
		*pcch = *pcch + 1;
	}

	// Add two for the last 2 NULLs (that we looked ahead at)
	*pcch = *pcch + 2;

	// If we've walked off the end then the length is 0
	if (*pcch > dwMaxSize)
		*pcch = 0;

LExit:
	return hr;
}


/****************************************************************************
MultiSzPrepend - prepends a string onto the front of a MUTLISZ

****************************************************************************/
extern "C" HRESULT DAPI MultiSzPrepend(
	__inout LPWSTR* ppwzMultiSz,
	__inout_opt DWORD_PTR *pcchMultiSz,
	__in LPCWSTR pwzInsert
	)
{
	Assert(ppwzMultiSz && pwzInsert && *pwzInsert);

	HRESULT hr =S_OK;
	LPWSTR pwzResult = NULL;
	DWORD_PTR cchResult = 0;
	DWORD_PTR cchInsert = 0;
	DWORD_PTR cchMultiSz = 0;

	// Get the lengths of the MULTISZ (and prime it if it's not initialized)
	if (pcchMultiSz && 0 != *pcchMultiSz)
	{
		cchMultiSz = *pcchMultiSz;
	}
	else
	{
		hr = MultiSzLen(*ppwzMultiSz, &cchMultiSz);
		ExitOnFailure(hr, "failed to get length of multisz");
	}

	cchInsert = lstrlenW(pwzInsert);

	cchResult = cchInsert + cchMultiSz + 1;

	// Allocate the result buffer
	hr = StrAlloc(&pwzResult, cchResult);
	ExitOnFailure(hr, "failed to allocate result string");

	// Prepend
	hr = StringCchCopyW(pwzResult, cchResult, pwzInsert);
	ExitOnFailure1(hr, "failed to copy prepend string: %S", pwzInsert);

	// If there was no MULTISZ, double null termiate our result, otherwise, copy the MULTISZ in
	if (0 == cchMultiSz)
	{
		pwzResult[cchResult + 1] = L'\0';
		cchResult++;
	}
	else
	{
		// Copy the rest
		::CopyMemory(pwzResult + cchInsert + 1, *ppwzMultiSz, cchMultiSz * sizeof(WCHAR));

		// Free the old buffer
		ReleaseNullStr(*ppwzMultiSz);
	}

	// Set the result
	*ppwzMultiSz = pwzResult;

	if (pcchMultiSz)
		*pcchMultiSz = cchResult;

	pwzResult = NULL;

LExit:
	ReleaseNullStr(pwzResult);

	return hr;
}

/****************************************************************************
MultiSzFindSubstring - case insensative find of a string in a MULTISZ that contains the
specified sub string and returns the index of the
string in the MULTISZ, the address, neither, or both

NOTE: returns S_FALSE if the string is not found
****************************************************************************/
extern "C" HRESULT DAPI MultiSzFindSubstring(
	__in LPCWSTR pwzMultiSz,
	__in LPCWSTR pwzSubstring,
	__out_opt DWORD_PTR* pdwIndex,
	__out_opt LPCWSTR* ppwzFoundIn
	)
{
	Assert(pwzMultiSz && *pwzMultiSz && pwzSubstring && *pwzSubstring);

	HRESULT hr = S_FALSE; // Assume we won't find it (the glass is half empty)
	LPCWSTR wz = pwzMultiSz;
	DWORD_PTR dwIndex = 0;
	DWORD_PTR cchMultiSz = 0;
	DWORD_PTR cchProgress = 0;

	hr = MultiSzLen(pwzMultiSz, &cchMultiSz);
	ExitOnFailure(hr, "failed to get the length of a MULTISZ string");

	// Find the string containing the sub string
	hr = S_OK;
	while (NULL == wcsistr(wz, pwzSubstring))
	{
		// Slide through to the end of the current string
		while (L'\0' != *wz && cchProgress < cchMultiSz)
		{
			wz++;
			cchProgress++;
		}

		// If we're done, we're done
		if (L'\0' == *(wz + 1) || cchProgress >= cchMultiSz)
		{
			hr = S_FALSE;
			break;
		}

		// Move on to the next string
		wz++;
		dwIndex++;
	}
	Assert(S_OK == hr || S_FALSE == hr);

	// If we found it give them what they want
	if (S_OK == hr)
	{
		if (pdwIndex)
			*pdwIndex = dwIndex;

		if (ppwzFoundIn)
			*ppwzFoundIn = wz;
	}

LExit:
	return hr;
}

/****************************************************************************
MultiSzFindString - finds a string in a MULTISZ and returns the index of
the string in the MULTISZ, the address or both

NOTE: returns S_FALSE if the string is not found
****************************************************************************/
extern "C" HRESULT DAPI MultiSzFindString(
	__in LPCWSTR pwzMultiSz,
	__in LPCWSTR pwzString,
	__out DWORD_PTR* pdwIndex,
	__out LPCWSTR* ppwzFound
	)
{
	Assert(pwzMultiSz && *pwzMultiSz && pwzString && *pwzString && (pdwIndex || ppwzFound));

	HRESULT hr = S_FALSE; // Assume we won't find it
	LPCWSTR wz = pwzMultiSz;
	DWORD_PTR dwIndex = 0;
	DWORD_PTR cchMutliSz = 0;
	DWORD_PTR cchProgress = 0;

	hr = MultiSzLen(pwzMultiSz, &cchMutliSz);
	ExitOnFailure(hr, "failed to get the length of a MULTISZ string");

	// Find the string
	hr = S_OK;
	while (0 != lstrcmpW(wz, pwzString))
	{
		// Slide through to the end of the current string
		while (L'\0' != *wz && cchProgress < cchMutliSz)
		{
			wz++;
			cchProgress++;
		}

		// If we're done, we're done
		if (L'\0' == *(wz + 1) || cchProgress >= cchMutliSz)
		{
			hr = S_FALSE;
			break;
		}

		// Move on to the next string
		wz++;
		dwIndex++;
	}
	Assert(S_OK == hr || S_FALSE == hr);

	// If we found it give them what they want
	if (S_OK == hr)
	{
		if (pdwIndex)
			*pdwIndex = dwIndex;

		if (ppwzFound)
			*ppwzFound = wz;
	}

LExit:
	return hr;
}

/****************************************************************************
MultiSzRemoveString - removes string from a MULTISZ at the specified
index

NOTE: does an in place removal without shrinking the memory allocation

NOTE: returns S_FALSE if the MULTISZ has fewer strings than dwIndex
****************************************************************************/
extern "C" HRESULT DAPI MultiSzRemoveString(
	__inout LPWSTR* ppwzMultiSz,
	__in DWORD_PTR dwIndex
	)
{
	Assert(ppwzMultiSz && *ppwzMultiSz);

	HRESULT hr = S_OK;
	LPCWSTR wz = *ppwzMultiSz;
	LPCWSTR wzNext = NULL;
	DWORD_PTR dwCurrentIndex = 0;
	DWORD_PTR cchMultiSz = 0;
	DWORD_PTR cchProgress = 0;

	hr = MultiSzLen(*ppwzMultiSz, &cchMultiSz);
	ExitOnFailure(hr, "failed to get the length of a MULTISZ string");

	// Find the index we want to remove
	hr = S_OK;
	while (dwCurrentIndex < dwIndex)
	{
		// Slide through to the end of the current string
		while (L'\0' != *wz && cchProgress < cchMultiSz)
		{
			wz++;
			cchProgress++;
		}

		// If we're done, we're done
		if (L'\0' == *(wz + 1) || cchProgress >= cchMultiSz)
		{
			hr = S_FALSE;
			break;
		}

		// Move on to the next string
		wz++;
		cchProgress++;
		dwCurrentIndex++;
	}
	Assert(S_OK == hr || S_FALSE == hr);

	// If we found the index to be removed
	if (S_OK == hr)
	{
		wzNext = wz;

		// Slide through to the end of the current string
		while (L'\0' != *wzNext && cchProgress < cchMultiSz)
		{
			wzNext++;
			cchProgress++;
		}

		// Something weird has happend if we're past the end of the MULTISZ
		if (cchProgress > cchMultiSz)
			ExitOnFailure(hr = E_UNEXPECTED, "failed to move past the string to be removed from MULTISZ");

		// Move on to the next character
		wzNext++;
		cchProgress++;

		::MoveMemory((LPVOID)wz, (LPVOID)wzNext, (cchMultiSz - cchProgress) * sizeof(WCHAR));
	}

LExit:
	return hr;
}

/****************************************************************************
MultiSzInsertString - inserts new string at the specified index

****************************************************************************/
extern "C" HRESULT DAPI MultiSzInsertString(
	__inout LPWSTR* ppwzMultiSz,
	__inout_opt DWORD_PTR *pcchMultiSz,
	__in DWORD_PTR dwIndex,
	__in LPCWSTR pwzInsert
	)
{
	Assert(ppwzMultiSz && pwzInsert && *pwzInsert);

	HRESULT hr = S_OK;
	LPCWSTR wz = *ppwzMultiSz;
	LPCWSTR wzNext = NULL;
	DWORD_PTR dwCurrentIndex = 0;
	DWORD_PTR cchProgress = 0;
	LPWSTR pwzResult = NULL;
	DWORD_PTR cchResult = 0;
	DWORD_PTR cchString = lstrlenW(pwzInsert);
	DWORD_PTR cchMultiSz = 0;

	if (pcchMultiSz && 0 != *pcchMultiSz)
	{
		cchMultiSz = *pcchMultiSz;
	}
	else
	{
		hr = MultiSzLen(*ppwzMultiSz, &cchMultiSz);
		ExitOnFailure(hr, "failed to get the length of a MULTISZ string");
	}

	// Find the index we want to insert at
	hr = S_OK;
	while (dwCurrentIndex < dwIndex)
	{
		// Slide through to the end of the current string
		while (L'\0' != *wz && cchProgress < cchMultiSz)
		{
			wz++;
			cchProgress++;
		}

		// If we're done, we're done
		if ((dwCurrentIndex + 1 != dwIndex && L'\0' == *(wz + 1)) || cchProgress >= cchMultiSz)
			ExitOnFailure1(hr = HRESULT_FROM_WIN32(ERROR_OBJECT_NOT_FOUND), "requested to insert into an invalid index: %d in a MULTISZ", dwIndex);

		// Move on to the next string
		wz++;
		cchProgress++;
		dwCurrentIndex++;
	}

	//
	// Insert the string
	//
	cchResult = cchMultiSz + cchString + 1;

	hr = StrAlloc(&pwzResult, cchResult);
	ExitOnFailure(hr, "failed to allocate result string for MULTISZ insert");

	// Copy the part before the insert
	::CopyMemory(pwzResult, *ppwzMultiSz, cchProgress * sizeof(WCHAR));

	// Copy the insert part
	::CopyMemory(pwzResult + cchProgress, pwzInsert, (cchString + 1) * sizeof(WCHAR));

	// Copy the part after the insert
	::CopyMemory(pwzResult + cchProgress + cchString + 1, wz, (cchMultiSz - cchProgress) * sizeof(WCHAR));

	// Free the old buffer
	ReleaseNullStr(*ppwzMultiSz);

	// Set the result
	*ppwzMultiSz = pwzResult;

	// If they wanted the resulting length, let 'em have it
	if (pcchMultiSz)
		*pcchMultiSz = cchResult;

	pwzResult = NULL;

LExit:
	ReleaseNullStr(pwzResult);

	return hr;
}

/****************************************************************************
MultiSzReplaceString - replaces string at the specified index with a new one

****************************************************************************/
extern "C" HRESULT DAPI MultiSzReplaceString(
	__inout LPWSTR* ppwzMultiSz,
	__in DWORD_PTR dwIndex,
	__in LPCWSTR pwzString
	)
{
	Assert(ppwzMultiSz && pwzString && *pwzString);

	HRESULT hr = S_OK;

	hr = MultiSzRemoveString(ppwzMultiSz, dwIndex);
	ExitOnFailure1(hr, "failed to remove string from MULTISZ at the specified index: %d", dwIndex);

	hr = MultiSzInsertString(ppwzMultiSz, NULL, dwIndex, pwzString);
	ExitOnFailure1(hr, "failed to insert string into MULTISZ at the specified index: %d", dwIndex);

LExit:
	return hr;
}


/****************************************************************************
wcsistr - case insensative find a substring

****************************************************************************/
LPCWSTR wcsistr(
	__in LPCWSTR wzString,
	__in LPCWSTR wzCharSet
	)
{
	LPCWSTR wzSource = wzString;
	LPCWSTR wzSearch = NULL;
	DWORD_PTR cchSourceIndex = 0;

	// Walk through wzString (the source string) one character at a time
	while (*wzSource)
	{
		cchSourceIndex = 0;
		wzSearch = wzCharSet;

		// Look ahead in the source string until we get a full match or we hit the end of the source
		while (L'\0' != wzSource[cchSourceIndex] && L'\0' != *wzSearch && towlower(wzSource[cchSourceIndex]) == towlower(*wzSearch))
		{
			cchSourceIndex++;
			wzSearch++;
		}

		// If we found it, return the point that we found it at
		if (L'\0' == *wzSearch)
		{
			return wzSource;
		}

		// Walk ahead one character
		wzSource++;
	}

	return NULL;
}

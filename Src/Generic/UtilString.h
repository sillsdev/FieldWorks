/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilString.h
Responsibility: LarryW
Last reviewed: 27Sep99

Description:
	This file with its matching .cpp file provides string related utilities. There are two
	types of strings.

	The first type is based on the class StrBase<> and allocates space for its characters on the
	heap, so the actual StrAnsi and StrUni objects are small. An assignment of one of these
	strings to another simply results in the two pointing to the same block of memory. If an
	allocation fails, an exception is thrown.

	The three instantiations of StrBase<> are:

		StrUni, which contains unicode (16 bit) characters.
		StrAnsi, which contains ansi (8 bit) characters.
		StrApp, which contains achar characters. achar is typedefed in Common.h to be either
			wchar (#ifdef UNICODE) or schar (for all other cases). schar is typedefed as char.

	The second type of string is based on the class StrBaseBuf<> and contains a maximum number
	of characters stored as part of the object. An assignment of one of these strings to
	another copies the characters from one to the other. If an edit command results in an
	overflow of characters, this type of string holds those characters that fit, and is
	put into an overflow state. The overflow state propagates in future edit commands on the
	string. Reassignment of characters that fit into the string clears the overflow state.

	The template instantiations of StrBaseBuf<>, where "Uni" refers to unicode characters,
	"Ansi" refers to ansi characters, and "App" refers to achar characters, include the
	following:

		Name                                Size (chars)   Purpose
		--------------------------------    ------------   -----------------------------------
		StrUniBufSmall, StrAnsiBufSmall,
			StrAppBufSmall                        32       small strings, e.g., for an integer
		StrUniBuf, StrAnsiBuf, StrAppBuf         260       ~1/4 K string, e.g., for a message
		StrUniBufPath, StrAnsiBufPath,
			StrAppBufPath                   MAX_PATH       for path names
		StrUniBufBig, StrAnsiBufBig,
			StrAppBufBig                        1024       1 K for larger strings
		StrUniBufHuge, StrAnsiBufHuge,
			StrAppBufHuge                      16384       10 K for really large strings

	Template instantiations for StrApp also exists in the five given sizes above.

	MAX_PATH is defined in Windef.h to be 260 characters, and is the maximum length for a path.

	StrBaseBufCore<> is the baseclass of StrBaseBuf<> that implements the core functionality of
	StrBaseBuf<> that is not dependent on size.

	Note: Possible string utilities functions for the future, if needed, include:
		TrimLeft(cch), TrimRight(cch),
		ReplaceBy(chFrom, chTo),
		CompareCI in its variety of forms.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef UTILSTRING_H
#define UTILSTRING_H 1
/*:End Ignore*/

#include "UnicodeConverter.h"

//:Associate with TextFormatter<>.
const int kradMax = 36; // For converting from numeric to text with any allowable radix.
extern const char g_rgchDigits[kradMax];


//:>********************************************************************************************
//:>	General text manipulation functions.
//:>********************************************************************************************

/*-----------------------------------------------------------------------------------*//*:Ignore
	Char Type Based Type Defns. The following template class is so we can define schar and
	wchar to be opposite each other. XChar in templates below is defined to mean one type;
	YChar is defined to mean the other type. ZChar is used to mean the third character type,
	which is always a 32-bit "wide wide" character (ie UTF-32).
	This also applies to other declarations that are specific to a character type.
-------------------------------------------------------------------------------*//*:End Ignore*/
template<typename XChar> class CharDefns;

/*----------------------------------------------------------------------------------------------
	The template class CharDefns<wchar> is for wide (16 bit) characters. It is used by
	StrBase<wchar>, StrBaseBufCore<>, and text manipulation functions to define schar and wchar
	to be opposite each other.

	When CharDefns<wchar> is used in template functions, XChar is defined to mean wchar;
	YChar is defined to mean schar. This also applies to other declarations that are specific
	to a character type. CharDefns<wchar> defines OtherChar1 to mean schar, and OtherChar2
	as a third character type (wwchar) used to represent UTF-32.
----------------------------------------------------------------------------------------------*/
template<> class CharDefns<wchar>
{
public:
	typedef schar  OtherChar1;
	typedef wwchar OtherChar2;
	typedef void (*PfnWriteChars)(void * pv, const wchar * prgch, int cch);
};

typedef CharDefns<wchar>::PfnWriteChars PfnWriteCharsW;

/*----------------------------------------------------------------------------------------------
	The template class CharDefns<schar> is for ansi (8 bit) characters. It is used by
	StrBase<schar>, StrBaseBufCore<>, and text manipulation functions to define schar and wchar
	to be opposite each other.

	When CharDefns<schar> is used in template functions, XChar is defined to mean schar;
	YChar is defined to mean wchar. This also applies to other declarations that are specific
	to a character type. CharDefns<schar> defines OtherChar1 to mean wchar, and OtherChar2
	as a third character type (wwchar) used to represent UTF-32.
----------------------------------------------------------------------------------------------*/
template<> class CharDefns<schar>
{
public:
	typedef wchar  OtherChar1;
	typedef wwchar OtherChar2;
	typedef void (*PfnWriteChars)(void * pv, const schar * prgch, int cch);
};

typedef CharDefns<schar>::PfnWriteChars PfnWriteCharsA;


//:>********************************************************************************************
//:>	Text manipulation functions for changing case.
//:>********************************************************************************************

//:Associate with "Generic Text Manipulation Functions".
/*-----------------------------------------------------------------------------------*//*:Ignore
	Ansi case mapping functions. These functions convert single-byte characters to lower or
	upper case as indicated.

	Note: Do not do pointer arithmetic in the argument to tolower and toupper. The tolower and
	toupper macros evaluate the input argument twice. Therefore the compiler performs any
	pointer arithmetic specified in a macro argument twice. The following code is correct. If
	the code was implemented as "toupper(*prgch++)" instead, you would produce incorrect
	results, such as corrupted strings or a GP fault.

	tolower converts a character to lowercase, if it isn't already lowercase, and returns the
	converted character. toupper converts a character to uppercase, if it isn't already
	uppercase, and returns the converted character.

	The exact behavior of ToUpper and ToLower depends on the LC_TYPE setting of the current
	Locale (see setlocale). By default, since the program starts up with setlocale(LC_ALL, "C"),
	only the standard 7-bit ASCII cased characters are converted (a-z or A-Z).

	If you use a different locale, you must make sure that the character passed as a parameter
	is indeed valid for the code page of that locale.
-------------------------------------------------------------------------------*//*:End Ignore*/

/*----------------------------------------------------------------------------------------------
	This ansi case mapping function converts cch single-byte characters in prgch to upper case.

	The exact behavior of ToUpper depends on the LC_TYPE setting of the current Locale
	(see setlocale in the MSDN Library). By default, since the program starts up with
	setlocale(LC_ALL, "C"), only the standard 7-bit ASCII cased characters are converted
	(a-z or A-Z). If you use a different locale, you must make sure that the character passed
	as a parameter is indeed valid for the code page of that locale.
----------------------------------------------------------------------------------------------*/
inline void ToUpper(schar * prgch, int cch)
{
	AssertArray(prgch, cch);
	for ( ; --cch >= 0; prgch++)
		*prgch = (schar)toupper(*prgch);
}

/*----------------------------------------------------------------------------------------------
	This ansi case mapping function converts the single-byte character ch to upper case.

	The exact behavior of ToUpper depends on the LC_TYPE setting of the current Locale
	(see setlocale in the MSDN Library). By default, since the program starts up with
	setlocale(LC_ALL, "C"), only the standard 7-bit ASCII cased characters are converted
	(a-z or A-Z). If you use a different locale, you must make sure that the character passed
	as a parameter is indeed valid for the code page of that locale.
----------------------------------------------------------------------------------------------*/
inline schar ToUpper(schar ch)
{
	return (schar)toupper(ch);
}


/*----------------------------------------------------------------------------------------------
	This ansi case mapping function converts cch single-byte characters in prgch to lower case.

	The exact behavior of ToLower depends on the LC_TYPE setting of the current Locale
	(see setlocale in the MSDN Library). By default, since the program starts up with
	setlocale(LC_ALL, "C"), only the standard 7-bit ASCII cased characters are converted
	(a-z or A-Z). If you use a different locale, you must make sure that the character passed
	as a parameter is indeed valid for the code page of that locale.
----------------------------------------------------------------------------------------------*/
inline void ToLower(schar * prgch, int cch)
{
	AssertArray(prgch, cch);
	for ( ; --cch >= 0; prgch++)
		*prgch = (schar)tolower(*prgch);
}

/*----------------------------------------------------------------------------------------------
	This ansi case mapping function converts the single-byte character ch to lower case.

	The exact behavior of ToLower depends on the LC_TYPE setting of the current Locale
	(see setlocale in the MSDN Library). By default, since the program starts up with
	setlocale(LC_ALL, "C"), only the standard 7-bit ASCII cased characters are converted
	(a-z or A-Z). If you use a different locale, you must make sure that the character passed
	as a parameter is indeed valid for the code page of that locale.
----------------------------------------------------------------------------------------------*/
inline schar ToLower(schar ch)
{
	return (schar)tolower(ch);
}


/*-----------------------------------------------------------------------------------*//*:Ignore
	Unicode case mapping functions.
-------------------------------------------------------------------------------*//*:End Ignore*/

/*----------------------------------------------------------------------------------------------
	This unicode case mapping function converts cch wide (16-bit) characters in prgch to
	upper case.
----------------------------------------------------------------------------------------------*/
void ToUpper(wchar * prgch, int cch);

/*----------------------------------------------------------------------------------------------
	This unicode case mapping function converts the wide (16-bit) character ch to upper case.
----------------------------------------------------------------------------------------------*/
wchar ToUpper(wchar ch);

/*----------------------------------------------------------------------------------------------
	This unicode case mapping function converts cch wide (16-bit) characters in prgch to
	lower case.
----------------------------------------------------------------------------------------------*/
void ToLower(wchar * prgch, int cch);

/*----------------------------------------------------------------------------------------------
	This unicode case mapping function converts the wide (16-bit) character ch to lower case.
----------------------------------------------------------------------------------------------*/
wchar ToLower(wchar ch);


/*----------------------------------------------------------------------------------------------
	Get a pointer and cch (count of characters) for a string with id stid defined in a resource
	header file.

	@h3{Parameters}
		stid -- string id, e.g., kstidComment (defined in a resource header file).
----------------------------------------------------------------------------------------------*/
void GetResourceString(const wchar ** pprgch, int * pcch, int stid);

/*----------------------------------------------------------------------------------------------
	Return the length (number of characters) of a string. Surrogate pairs and other 'characters'
	that occupy more than the usual space (e.g., in ASCII double-byte encodings) are not
	recognized. StrLen just counts bytes or wchars.

	This is defined to give us something with the same name to use in other template functions.
	This handles NULL as a zero-length string.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	inline int StrLen(const XChar * psz)
{
	AssertPszN(psz);

	if (!psz)
		return 0;

	const XChar * pch;
	for (pch = psz; *pch; pch++)
		;
	return pch - psz;
}


//:Associate with "Generic Text Comparison Functions".
/*----------------------------------------------------------------------------------------------
	Case sensitive naive binary comparison of strings containing ansi (8 bit) characters.

	@h3{Return value}
	Returns negative, zero, or positive according to whether (prgch1, cch1) is less than,
	equal to, or greater than (prgch2, cch2).
----------------------------------------------------------------------------------------------*/
inline int CompareRgch(const char * prgch1, int cch1, const char * prgch2, int cch2)
{
	int cch = Min(cch1, cch2);

	for ( ; --cch >= 0; prgch1++, prgch2++)
	{
		if (*prgch1 != *prgch2)
			return (int)(uchar)*prgch1 - (int)(uchar)*prgch2;
	}
	return cch1 - cch2;
}

/*----------------------------------------------------------------------------------------------
	Case sensitive naive binary comparison of strings containing unicode (16 bit) characters.

	@h3{Return value}
	Returns negative, zero, or positive according to whether (prgch1, cch1) is less than,
	equal to, or greater than (prgch2, cch2).
----------------------------------------------------------------------------------------------*/
inline int CompareRgch(const wchar * prgch1, int cch1, const wchar * prgch2, int cch2)
{
	int cch = Min(cch1, cch2);

	for ( ; --cch >= 0; prgch1++, prgch2++)
	{
		if (*prgch1 != *prgch2)
			return (int)*prgch1 - (int)*prgch2;
	}
	return cch1 - cch2;
}


/*----------------------------------------------------------------------------------------------
	Case insensitive equality. This function can be used for either ansi (8 bit) or unicode
	(16-bit) characters.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	inline bool EqualsRgchCI(const XChar * prgch1, const XChar * prgch2, int cch)
{
	for ( ; --cch >= 0; prgch1++, prgch2++)
	{
		if (*prgch1 != *prgch2 && ToLower(*prgch1) != ToLower(*prgch2))
			return false;
	}

	return true;
}


//:Associate with "Generic Text Formatting Functions".
/*----------------------------------------------------------------------------------------------
	Format a string (prgchFmt, cchFmt) to a call back function.

	Parameters
		pfnWrite -- pointer to the function used by FormatText to write text to
			a stream or some type of string object.
		pv -- pointer to a stream or some type of string object. It is supplied
			by the caller of FormatText and passed on as the first argument
			whenever pfnWrite is called.
		prgchFmt -- string used as the template.
		cchFmt -- number of characters in the template string.
		vaArgList -- additional parameters used with the template string.

	This is instantiated only for <wchar, PfnWriteCharsW> and <schar, PfnWriteCharsA>.
----------------------------------------------------------------------------------------------*/
template<typename XChar, typename Pfn>
	void FormatText(Pfn pfnWrite, void * pv,
		const XChar * prgchFmt, int cchFmt, va_list vaArgList);


/*----------------------------------------------------------------------------------------------
	Format the template string pszFmt to an IStream. The arguments must all be 4 bytes long.
	See FormatText.

	@h3{Examples}
	@code{
		FormatToStream(qstrm, "</FwDatabase>%n");
		FormatToStream(pstrm, "<%S>", pszName);
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void FormatToStream(IStream * pstrm, const XChar * pszFmt, ...);

/*----------------------------------------------------------------------------------------------
	Format the template string (prgchFmt, cchFmt) to an IStream. The arguments must all be 4
	bytes long. See FormatText.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void FormatToStreamRgch(IStream * pstrm, const XChar * prgchFmt, int cchFmt, ...);

template<typename XChar>
	void FormatToStreamCore(IStream * pstrm, const XChar * prgchFmt, int cchFmt,
		 va_list vaArgList);


//:>********************************************************************************************
//:>	Text conversion functions for converting between unicode and ansi.
//:>********************************************************************************************

//:Associate with "Generic Text Conversion Functions".
/*----------------------------------------------------------------------------------------------
	Convert from Unicode to 8-bit, either using the given codepage or the ANSI codepage.

	If cchwSrc is -1, WideCharToMultiByte assumes that prgchwSrc is null-terminated and the
	length is calculated automatically.

	@h3{Return value}
	If WideCharToMultiByte succeeds, and cchsDst is nonzero, the return value is the number of
	bytes written to the buffer pointed to by prgchsDst. If it succeeds, and cchsDst is zero,
	the return value is the required size, in bytes, for a buffer that can receive the
	translated string. If it fails, the return value is zero.

	@h3{Parameters}
	@code{
		prgchwSrc -- source unicode string.
		cchwSrc -- count of characters in source string.
		prgchsDst -- destination ansi string.
		cchsDst -- count of characters in destination string.
	}
----------------------------------------------------------------------------------------------*/
inline int ConvertText(const wchar * prgchwSrc, int cchwSrc, schar * prgchsDst, int cchsDst,
	int nCodePage = CP_UTF8)
{
	AssertArray(prgchwSrc, cchwSrc);
	AssertArray(prgchsDst, cchsDst);

	return WideCharToMultiByte(nCodePage, 0, prgchwSrc, cchwSrc, prgchsDst, cchsDst, NULL, NULL);
}


/*----------------------------------------------------------------------------------------------
	Convert from 8-bit to Unicode, either using the given codepage or the ANSI codepage.

	If cchsSrc is -1, MultiByteToWideChar assumes that prgchsSrc is null-terminated and the
	length is calculated automatically.

	@h3{Return value}
	If MultiByteToWideChar succeeds, and cchwDst is nonzero, the return value is the number of
	wide characters written to the buffer pointed to by prgchwDst. If it succeeds, and cchwDst
	is zero, the return value is the required size, in wide characters, for a buffer that can
	receive the translated string. If it fails, the return value is zero.

	@h3{Parameters}
	@code{
		prgchsSrc -- source ansi string.
		cchsSrc -- count of characters in source string.
		prgchwDst -- destination unicode string.
		cchwDst -- count of characters in destination string.
	}

	@null{
	REVIEW LarryW: MultiByteToWideChar fails if MB_ERR_INVALID_CHARS is set and it encounters
	an invalid character in the source string. An invalid character is one that would translate
	to the default character if MB_ERR_INVALID_CHARS was not set, but is not the default
	character in the source string, or when a lead byte is found in a string and there is no
	valid trail byte for DBCS strings. When an invalid character is found, and
	MB_ERR_INVALID_CHARS _is_ set, the function returns 0 and sets GetLastError with the error
	ERROR_NO_UNICODE_TRANSLATION. Since we are not setting MB_ERR_INVALID_CHARS, it appears
	that we can still find out the count of characters. To verify this, we will need to test
	this with a multi-byte character set installed, such as the Japanese version of Windows.
	At the moment, this is not available.
	}
----------------------------------------------------------------------------------------------*/
inline int ConvertText(const schar * prgchsSrc, int cchsSrc, wchar * prgchwDst, int cchwDst,
	int nCodePage = CP_UTF8)
{
	AssertArray(prgchsSrc, cchsSrc);
	AssertArray(prgchwDst, cchwDst);

	return MultiByteToWideChar(nCodePage, 0, prgchsSrc, cchsSrc, prgchwDst, cchwDst);
}

#ifndef WIN32
/*----------------------------------------------------------------------------------------------
	Convert from UTF-32 to UTF-16.

	If cchsSrc is -1, u_strFromWCS assumes that prgchsSrc is null-terminated and the
	length is calculated automatically.

	@h3{Return value}
	If MultiByteToWideChar succeeds, and cchwDst is nonzero, the return value is the number of
	wide characters written to the buffer pointed to by prgchwDst. If it succeeds, and cchwDst
	is zero, the return value is the required size, in wide characters, for a buffer that can
	receive the translated string. If it fails, the return value is zero.

	@h3{Parameters}
	@code{
		prgchsSrc -- source UTF-32 string.
		cchsSrc -- count of characters in source string.
		prgchwDst -- destination UTF-16 string.
		cchwDst -- count of characters in destination string.
	}
----------------------------------------------------------------------------------------------*/
inline int ConvertText(const wwchar * prgchwwSrc, int cchwwSrc, wchar * prgchwDst, int cchwDst,
	int nCodePage = CP_UTF8)
{
	AssertArray(prgchwwSrc, cchwwSrc);
	AssertArray(prgchwDst, cchwDst);

	UErrorCode status = U_ZERO_ERROR;
	int32_t unitsWritten = 0;

	if (cchwDst == 0)
		prgchwDst = 0; // Request pre-flighting

	u_strFromWCS(prgchwDst, cchwDst, &unitsWritten, prgchwwSrc, cchwwSrc, &status);

	if (U_FAILURE(status) && status != U_BUFFER_OVERFLOW_ERROR)
		return 0;

	return unitsWritten;
}

/*----------------------------------------------------------------------------------------------
	Convert from UTF-32 to UTF-8.

	If cchsSrc is -1, UnicodeConverter::Convert assumes that prgchSrc is null-terminated and the
	length is calculated automatically.

	@h3{Return value}
	If UnicodeConverter::Convert succeeds, and cchDst is nonzero, the return value is the number of
	characters written to the buffer pointed to by prgchDst. If it succeeds, and cchDst
	is zero, the return value is the required size, in characters, for a buffer that can
	receive the translated string. If it fails, the return value is zero.

	@h3{Parameters}
	@code{
		prgchsrc -- source UTF-32 string.
		cchsrc -- count of characters in source string.
		prgchDst -- destination UTF-8 string.
		cchDst -- count of characters in destination string.
	}
----------------------------------------------------------------------------------------------*/
inline int ConvertText(const wwchar * prgchSrc, int cchSrc, schar * prgchDst, int cchDst,
	int nCodePage = CP_UTF8)
{
	AssertArray(prgchSrc, cchSrc);
	AssertArray(prgchDst, cchDst);

	Assert(nCodePage == CP_UTF8);

	if (cchDst == 0)
		prgchDst = 0; // Request pre-flighting

	return UnicodeConverter::Convert(prgchSrc, cchSrc, prgchDst, cchDst);
}

/*----------------------------------------------------------------------------------------------
	Convert from UTF-8 to UTF-32.

	If cchsrc is -1, UnicodeConverter::Convert assumes that prgchSrc is null-terminated and the
	length is calculated automatically.

	@h3{Return value}
	If UnicodeConverter::Convert succeeds, and cchDst is nonzero, the return value is the number of
	characters written to the buffer pointed to by prgchDst. If it succeeds, and cchDst
	is zero, the return value is the required size, in characters, for a buffer that can
	receive the translated string. If it fails, the return value is zero.

	@h3{Parameters}
	@code{
		prgchSrc -- source UTF-8 string.
		cchSrc -- count of characters in source string.
		prgchDst -- destination UTF-32 string.
		cchDst -- count of characters in destination string.
	}
----------------------------------------------------------------------------------------------*/
inline int ConvertText(const schar * prgchSrc, int cchSrc, wwchar * prgchDst, int cchDst,
	int nCodePage = CP_UTF8)
{
	AssertArray(prgchSrc, cchSrc);
	AssertArray(prgchDst, cchDst);

	Assert(nCodePage == CP_UTF8);

	if (cchDst == 0)
		prgchDst = 0; // Request pre-flighting

	return UnicodeConverter::Convert(prgchSrc, cchSrc, prgchDst, cchDst);
}
#endif

/*----------------------------------------------------------------------------------------------
	Do a trivial copy. It is useful for implementing template functions. E.g., see
	${StrBase<>#GetBstr}, or ${StrBaseBuf<>#Assign}.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	inline int ConvertText(const XChar * prgchSrc, int cchSrc, XChar * prgchDst, int cchDst)
{
	AssertArray(prgchSrc, cchSrc);
	AssertArray(prgchDst, cchDst);
	Assert(cchDst >= cchSrc || 0 == cchDst);

	if (!cchDst)
		return cchSrc;

	CopyItems(prgchSrc, prgchDst, cchSrc);
	return cchSrc;
}


/*----------------------------------------------------------------------------------------------
	Determine the largest number of YChars that can be converted to cxchMax or fewer XChars.
	This does a binary search.
----------------------------------------------------------------------------------------------*/
template<typename XChar, typename YChar>
	inline int CychFitConvertedText(const YChar * prgych, int cych, int cxchMax)
{
	AssertArray(prgych, cych);
	Assert(0 <= cxchMax);

	if (0 >= cxchMax)
		return 0;

	// The most common case is that each ych becomes a single xch, so test for this first.
	if (cych > cxchMax &&
		ConvertText(prgych, cxchMax, (XChar*)NULL, 0) <= cxchMax &&
		ConvertText(prgych, cxchMax + 1, (XChar*)NULL, 0) > cxchMax)
	{
		return cxchMax;
	}

	int cychMin, cychLim;

	for (cychMin = 0, cychLim = cych; cychMin < cychLim; )
	{
		int cychT = (uint)(cychMin + cychLim + 1) / 2;
		Assert(cychMin < cychT && cychT <= cychLim);

		int cxchT = ConvertText(prgych, cychT, (XChar*)NULL, 0);
		if (cxchT > cxchMax)
			cychLim = cychT - 1;
		else
			cychMin = cychT;
	}
	return cychMin;
}


//:>********************************************************************************************
//:>	Smart string class StrBase<>.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Smart string class StrBase<> is used to implement strings of unicode or ansi characters. It
	creates an instance of the embedded struct StrBuffer, storing the characters on the heap.
	The StrBuffer is stored in the member variable m_pbuf.

	Note: Don't use new or alloc on StrBase<>. StrBuffer has its own static create function that
	it uses to appropriately allocate space for the characters.

	StrBuffer keeps a reference count. Thus, assignment of one StrBase<> to another simply
	results in the two pointing to the same block of memory. However, if one of them is now
	changed in some way, the other one is not affected; the modified one will be given a new
	StrBuffer.

	If the allocation of space for characters fails, an exception is thrown.

	StrBuffer stores the byte count in its member variable m_cb immediately followed by the
	characters in its member variable m_rgch[]. This layout matches a BSTR, so if a client asks
	for a BSTR it gets a pointer to m_rgch.

	The three instantiations of StrBase<> are:
	@code{
		StrUni, which contains unicode (16 bit) characters.
		StrAnsi, which contains ansi (8 bit) characters.
		StrApp, which contains achar characters. achar is typedefed in Common.h as wchar
			for UNICODE and as schar, i.e., char, for all other cases.
	}

	For a comparison of the other smart string class, see ${StrBaseBuf<>}.

	@h3{Hungarian: stb}
----------------------------------------------------------------------------------------------*/
template<typename XChar> class StrBase
{
public:
#ifdef DEBUG
	// Check to make certain we have a valid internal state for debugging purposes.
	bool AssertValid(void) const
	{
		AssertPtr(this);
		AssertObj(m_pbuf);
		return true;
	}
#if WIN32
	#define DBWINIT() m_dbw1.m_pstrbase = this; // so DebugWatch can find string
#else
	#define DBWINIT()
#endif
#else
	#define DBWINIT()
#endif //DEBUG

	// The other character type.
	typedef typename CharDefns<XChar>::OtherChar1 YChar;
#ifndef WIN32
	typedef typename CharDefns<XChar>::OtherChar2 ZChar;
#endif

	/*------------------------------------------------------------------------------------------
		Destructor.
	------------------------------------------------------------------------------------------*/
	~StrBase<XChar>(void)
	{
		AssertObj(this);
		if (m_pbuf)
		{
			m_pbuf->Release();
			m_pbuf = NULL;
		}
	}

	/*------------------------------------------------------------------------------------------
		Generic constructor.
	------------------------------------------------------------------------------------------*/
	StrBase<XChar>(void)
	{
		m_pbuf = &s_bufEmpty;
		m_pbuf->AddRef();
		AssertObj(this);
		DBWINIT();
	}

	/*------------------------------------------------------------------------------------------
		Construct a new StrBase<> from a StrBase<Ychar> of the other character type.
	------------------------------------------------------------------------------------------*/
	StrBase<XChar>(const StrBase<YChar> & stb)
	{
		AssertObj(&stb);
		m_pbuf = &s_bufEmpty;
		m_pbuf->AddRef();
		int cch = stb.Length();
		if (cch)
			_Replace(0, 0, stb.m_pbuf->m_rgch, 0, cch);
		DBWINIT();
	}

	/*------------------------------------------------------------------------------------------
		Construct a new StrBase<> from a StrBase<Xchar> of the same character type.
	------------------------------------------------------------------------------------------*/
	StrBase<XChar>(const StrBase<XChar> & stb)
	{
		AssertObj(&stb);
		m_pbuf = stb.m_pbuf;
		m_pbuf->AddRef();
		AssertObj(this);
		DBWINIT();
	}

	/*------------------------------------------------------------------------------------------
		Construct a new StrBase<> from a zero-terminated string of either character type.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar> StrBase<XChar>(const AnyChar * psz)
	{
		AssertPszN(psz);
		m_pbuf = &s_bufEmpty;
		m_pbuf->AddRef();
		int cch = StrLen(psz);
		if (cch)
			_Replace(0, 0, psz, 0, cch);
		DBWINIT();
	}

	/*------------------------------------------------------------------------------------------
		Construct a new StrBase<> from a string, of either character type, with cch characters.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar> StrBase<XChar>(const AnyChar * prgch, int cch)
	{
		AssertArray(prgch, cch);
		m_pbuf = &s_bufEmpty;
		m_pbuf->AddRef();
		if (cch)
			_Replace(0, 0, prgch, 0, cch);
		DBWINIT();
	}

	/*------------------------------------------------------------------------------------------
		Construct a new StrBase<> from a string with id stid defined in a resource header file.
	------------------------------------------------------------------------------------------*/
	StrBase<XChar>(const int stid)
	{
		m_pbuf = &s_bufEmpty;
		m_pbuf->AddRef();
		const wchar *prgchw;
		int cch;

		::GetResourceString(&prgchw, &cch, stid);
		if (cch)
			_Replace(0, 0, prgchw, 0, cch);
		DBWINIT();
	}

	/*------------------------------------------------------------------------------------------
		Construct a new StrBase<> from a string template with id stidFmt defined in a resource
		header file.
	------------------------------------------------------------------------------------------*/
	template<typename T> StrBase<XChar>(const int stidFmt, T n0, ...)
	{
		m_pbuf = &s_bufEmpty;
		m_pbuf->AddRef();
		const wchar *prgchwFmt;
		int cchFmt;

		::GetResourceString(&prgchwFmt, &cchFmt, stidFmt);
		va_list argList;
		va_start(argList, n0);
		FormatCore(prgchwFmt, cchFmt, n0, argList);
		va_end(argList);
		DBWINIT();
	}


	/*------------------------------------------------------------------------------------------
		Create a new internal buffer of size cch and return a pointer to the characters.
		This preserves the characters currently in the string, up the the min of the old and
		new sizes. It is expected that the caller will fill in any newly allocated characters.
	------------------------------------------------------------------------------------------*/
	void SetSize(int cch, XChar ** pprgch);


	//:>****************************************************************************************
	//:>	Array-like functionality.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Get the length, i.e., the number of char or wchar characters (as opposed to a count of
		logical characters.
	------------------------------------------------------------------------------------------*/
	int Length(void) const
	{
		AssertObj(this);
		return m_pbuf->Cch();
	}

	/*------------------------------------------------------------------------------------------
		Make the string empty, by making the buffer empty.
	------------------------------------------------------------------------------------------*/
	void Clear(void)
	{
		AssertObj(this);
		_SetBuf(&s_bufEmpty);
	}

	/*------------------------------------------------------------------------------------------
		Return the character at index ich. Asserts that ich is in range.
	------------------------------------------------------------------------------------------*/
	XChar GetAt(int ich) const
	{
		AssertObj(this);
		Assert((uint)ich <= (uint)m_pbuf->Cch());
		return m_pbuf->m_rgch[ich];
	}

	/*------------------------------------------------------------------------------------------
		Return the character at index ich. Use for read only. Asserts that ich is in range.
	------------------------------------------------------------------------------------------*/
	XChar operator [] (int ich) const
	{
		AssertObj(this);
		Assert((uint)ich <= (uint)m_pbuf->Cch());
		return m_pbuf->m_rgch[ich];
	}

	/*------------------------------------------------------------------------------------------
		Set the character at index ich to ch.
	------------------------------------------------------------------------------------------*/
	void SetAt(int ich, XChar ch);


	//:>****************************************************************************************
	//:>	Access.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Return a read-only pointer to the characters.
	------------------------------------------------------------------------------------------*/
	const XChar * Chars(void) const
	{
		AssertObj(this);
		return m_pbuf->m_rgch;
	}

	/*------------------------------------------------------------------------------------------
		Return a (read-only) pointer to the characters as a BSTR; this is valid only when XChar
		is OLECHAR.
	------------------------------------------------------------------------------------------*/
	XChar * Bstr(void) const
	{
		AssertObj(this);
		Assert(isizeof(XChar) == isizeof(OLECHAR));
		return m_pbuf->m_rgch;
	}

	/*------------------------------------------------------------------------------------------
		Get an allocated BSTR that the caller is responsible for freeing.
	------------------------------------------------------------------------------------------*/
	void GetBstr(BSTR * pbstr) const;


	/*------------------------------------------------------------------------------------------
		Return a read-only pointer to the characters. This is the cast operator.
	------------------------------------------------------------------------------------------*/
	operator const XChar *(void) const
	{
		AssertObj(this);
		return m_pbuf->m_rgch;
	}

	/*------------------------------------------------------------------------------------------
		Return true if the count of bytes in the buffer is not zero.
	------------------------------------------------------------------------------------------*/
	operator bool(void) const
	{
		AssertObj(this);
		return m_pbuf->m_cb;
	}
	/*------------------------------------------------------------------------------------------
		Return true if the count of bytes in the buffer is zero.
	------------------------------------------------------------------------------------------*/
	bool operator !(void) const
	{
		AssertObj(this);
		return !m_pbuf->m_cb;
	}

	//:>****************************************************************************************
	//:>	Assignment.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Assign the value of this StrBase<> to be the same as the value of a StrBase<> of the
		other character type.
	------------------------------------------------------------------------------------------*/
	void Assign(const StrBase<YChar> & stb)
	{
		AssertObj(this);
		AssertObj(&stb);
		_Replace(0, m_pbuf->Cch(), stb.m_pbuf->m_rgch, 0, stb.m_pbuf->Cch());
	}

	/*------------------------------------------------------------------------------------------
		Assign the value of this StrBase<> to be the same as the value of another StrBase<>
		of the same character type.
	------------------------------------------------------------------------------------------*/
	void Assign(const StrBase<XChar> & stb)
	{
		AssertObj(this);
		AssertObj(&stb);
		_SetBuf(stb.m_pbuf);
	}

	/*------------------------------------------------------------------------------------------
		Assign the characters from the given zero-terminated string psz, of either character
		type, to be the value of this StrBase<>.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		void Assign(const AnyChar * psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		_Replace(0, m_pbuf->Cch(), psz, 0, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Assign the characters from the given string (prgch, cch), of either character type,
		to be the value of this StrBase<>.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		void Assign(const AnyChar * prgch, int cch)
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		_Replace(0, m_pbuf->Cch(), prgch, 0, cch);
	}

	/*------------------------------------------------------------------------------------------
		Assign the value of this StrBase<> to be the same as the string with id stid defined in
		a resource header file.
	------------------------------------------------------------------------------------------*/
	void Load(const int stid)
	{
		AssertObj(this);
		const wchar *prgchw;
		int cch;

		::GetResourceString(&prgchw, &cch, stid);
		if (cch)
			_Replace(0, m_pbuf->Cch(), prgchw, 0, cch);
	}

	//:> Assignment operators.
	/*------------------------------------------------------------------------------------------
		Assign the value of this StrBase<> to be the same as the value of a StrBase<> of the
		other character type.
	------------------------------------------------------------------------------------------*/
	StrBase<XChar> & operator = (const StrBase<YChar> & stb)
	{
		AssertObj(this);
		AssertObj(&stb);
		_Replace(0, m_pbuf->Cch(), stb.m_pbuf->m_rgch, 0, stb.m_pbuf->Cch());
		return *this;
	}

	/*------------------------------------------------------------------------------------------
		Assign the value of this StrBase<> to be the same as the value of another StrBase<> of
		the same character type.
	------------------------------------------------------------------------------------------*/
	StrBase<XChar> & operator = (const StrBase<XChar> & stb)
	{
		AssertObj(this);
		AssertObj(&stb);
		_SetBuf(stb.m_pbuf);
		return *this;
	}

	/*------------------------------------------------------------------------------------------
		Assign the characters from the given zero-terminated string psz, of either character
		type, to be the value of this StrBase<>.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBase<XChar> & operator = (const AnyChar * psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		_Replace(0, m_pbuf->Cch(), psz, 0, StrLen(psz));
		return *this;
	}


	/*------------------------------------------------------------------------------------------
		Assign the value of stbOut to be the same as the value of a StrBase<> of the
		other character type. Use the given codepage to convert between Unicode and 8-bit data.
	------------------------------------------------------------------------------------------*/
	static void AssignViaCodePage(const StrBase<YChar> & stbIn, StrBase<XChar> &stbOut,
		int nCodePage)
	{
		AssertObj(&stbOut);
		AssertObj(&stbIn);
		stbOut._Replace(0, stbOut.m_pbuf->Cch(), stbIn.m_pbuf->m_rgch, 0, stbIn.m_pbuf->Cch(),
			nCodePage);
	}

	/*------------------------------------------------------------------------------------------
		Read cch characters from the IStream and set the value of this StrBase<> to those
		characters.
	------------------------------------------------------------------------------------------*/
	void ReadChars(IStream * pstrm, int cch);


	//:>****************************************************************************************
	//:>	Concatenation.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Append a copy of the value of a StrBase<>, of the other character type, to the value
		of this StrBase<>.
	------------------------------------------------------------------------------------------*/
	void Append(const StrBase<YChar> & stb)
	{
		AssertObj(this);
		AssertObj(&stb);
		int cchCur = m_pbuf->Cch();
		_Replace(cchCur, cchCur, stb.m_pbuf->m_rgch, 0, stb.m_pbuf->Cch());
	}

	/*------------------------------------------------------------------------------------------
		Append a copy of the value of another StrBase<>, of the same character type, to the
		value of this StrBase<>.
	------------------------------------------------------------------------------------------*/
	void Append(const StrBase<XChar> & stb)
	{
		AssertObj(this);
		AssertObj(&stb);
		int cchCur = m_pbuf->Cch();
		if (!cchCur)
			_SetBuf(stb.m_pbuf);
		else
			_Replace(cchCur, cchCur, stb.m_pbuf->m_rgch, 0, stb.m_pbuf->Cch());
	}

	/*------------------------------------------------------------------------------------------
		Append a copy of the zero-terminated string psz, of either character type, to the value
		of this StrBase<>.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		void Append(const AnyChar * psz)
	{
		AssertPszN(psz);
		int cchCur = m_pbuf->Cch();
		_Replace(cchCur, cchCur, psz, 0, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Append a copy of the string (prgch, cch), of either character type, to the value of
		this StrBase<>.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		void Append(const AnyChar * prgch, int cch)
	{
		int cchCur = m_pbuf->Cch();
		_Replace(cchCur, cchCur, prgch, 0, cch);
	}

	/*------------------------------------------------------------------------------------------
		Append a copy of the string with id stid defined in a resource header file to the value
		of this StrBase<>.
	------------------------------------------------------------------------------------------*/
	void AppendLoad(const int stid)
	{
		AssertObj(this);
		const wchar *prgchw;
		int cch;
		int cchCur = m_pbuf->Cch();

		::GetResourceString(&prgchw, &cch, stid);
		if (cch)
			_Replace(cchCur, cchCur, prgchw, 0, cch);
	}

	//:> += operators.
	/*------------------------------------------------------------------------------------------
		Append a copy of the value of a StrBase<>, which may be of either character type, to
		the value of this StrBase<>.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBase<XChar> & operator += (const StrBase<AnyChar> & stb)
	{
		Append(stb);
		return *this;
	}

	/*------------------------------------------------------------------------------------------
		Append a copy of the zero-terminated string psz, of either character type, to the value
		of this StrBase<>.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBase<XChar> & operator += (const AnyChar * psz)
	{
		Append(psz);
		return *this;
	}


	//:> + operators.
	/*------------------------------------------------------------------------------------------
		Return a new StrBase<> with the value of this StrBase<> followed by the value of another
		StrBase<>, which may be of either character type.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBase<XChar> operator + (const StrBase<AnyChar> & stb) const
	{
		StrBase<XChar> stbRet(this);
		stbRet.Append(stb);
		return stbRet;
	}

	/*------------------------------------------------------------------------------------------
		Return a new StrBase<> with the value of this StrBase<> followed by a copy of the
		zero-terminated string psz, of either character type.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBase<XChar> operator + (const AnyChar * psz) const
	{
		StrBase<XChar> stbRet(this);
		stbRet.Append(psz);
		return stbRet;
	}


	//:>****************************************************************************************
	//:>	Comparison.
	//:>****************************************************************************************

	//:> Equality operators.
	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is equal to the value of stb, another StrBase<> of the
		same character type. Two StrBase<>'s are equal if they both contain the exact sequence
		of characters and if both have the same character count.
	------------------------------------------------------------------------------------------*/
	bool Equals(const StrBase<XChar> & stb) const
	{
		AssertObj(this);
		AssertObj(&stb);
		if (m_pbuf == stb.m_pbuf)
			return true;
		if (m_pbuf->m_cb != stb.m_pbuf->m_cb)
			return false;
		return 0 == memcmp(m_pbuf->m_rgch, stb.m_pbuf->m_rgch, m_pbuf->m_cb);
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is equal to the value of the zero-terminated string psz,
		of the same character type. A StrBase<> is equal to a zero-terminated string if they
		both contain the exact sequence of characters and if both have the same character count.
	------------------------------------------------------------------------------------------*/
	bool Equals(const XChar * psz) const
	{
		AssertObj(this);
		AssertPszN(psz);
		return Equals(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is equal to the value of the string (prgch, cch), of the
		same character type. A StrBase<> is equal to a string if they both contain the exact
		sequence of characters and if both have the same character count.
	------------------------------------------------------------------------------------------*/
	bool Equals(const XChar * prgch, int cch) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		if (m_pbuf->m_cb != cch * isizeof(XChar))
			return false;
		return 0 == memcmp(m_pbuf->m_rgch, prgch, cch * isizeof(XChar));
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is equal to the value of stb, another StrBase<> of the
		same character type. Two StrBase<>'s are equal if they both contain the exact sequence
		of characters and if both have the same character count. (See ${StrBase<>#Equals}).
	------------------------------------------------------------------------------------------*/
	bool operator == (const StrBase<XChar> & stb) const
	{
		AssertObj(&stb);
		return Equals(stb);
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is equal to the value of the zero-terminated string psz,
		of the same character type. A StrBase<> is equal to a zero-terminated string if they
		both contain the exact sequence of characters and if both have the same character count.
		(See ${StrBase<>#Equals}).
	------------------------------------------------------------------------------------------*/
	bool operator == (const XChar * psz) const
	{
		AssertPszN(psz);
		return Equals(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is not equal to the value of stb, another StrBase<> of the
		same character type. Two StrBase<>'s are equal if they both contain the exact sequence
		of characters and if both have the same character count. (See ${StrBase<>#Equals}).
	------------------------------------------------------------------------------------------*/
	bool operator != (const StrBase<XChar> & stb) const
	{
		AssertObj(&stb);
		return !Equals(stb);
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is not equal to the value of the zero-terminated string
		psz, of the same character type. A StrBase<> is equal to a zero-terminated string if
		they both contain the exact sequence of characters and if both have the same character
		count. (See ${StrBase<>#Equals}).
	------------------------------------------------------------------------------------------*/
	bool operator != (const XChar * psz) const
	{
		AssertPszN(psz);
		return !Equals(psz, StrLen(psz));
	}

	//:> Greater than and less than comparisons.
	/*------------------------------------------------------------------------------------------
		Case sensitive naive binary comparison of this StrBase<> with another StrBase<> that
		contains the same type of characters.

		@h3{Return value}
		Returns negative, zero, or positive according to whether this StrBase<> is less than,
		equal to, or greater than stb.
	------------------------------------------------------------------------------------------*/
	int Compare(const StrBase<XChar> & stb) const
	{
		AssertObj(this);
		AssertObj(&stb);
		return Compare(stb.m_pbuf->m_rgch, stb.m_pbuf->Cch());
	}

	/*------------------------------------------------------------------------------------------
		Case sensitive naive binary comparison of this StrBase<> with a zero-terminated string
		psz of the same character type.

		@h3{Return value}
		Returns negative, zero, or positive according to whether this StrBase<> is less than,
		equal to, or greater than the zero-terminated string psz.
	------------------------------------------------------------------------------------------*/
	int Compare(const XChar * psz) const
	{
		AssertObj(this);
		AssertPszN(psz);
		return Compare(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Case sensitive naive binary comparison of this StrBase<> with a string (prgch, cch) of
		the same character type. (See CompareRgch).

		@h3{Return value}
		Returns negative, zero, or positive according to whether this StrBase<> is less than,
		equal to, or greater than the string (prgch, cch).
	------------------------------------------------------------------------------------------*/
	int Compare(const XChar * prgch, int cch) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		return CompareRgch(m_pbuf->m_rgch, m_pbuf->Cch(), prgch, cch);
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is less than another StrBase<> of the same character type,
		based on a case sensitive naive binary comparison (see ${StrBase<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator < (const StrBase<XChar> & stb) const
	{
		AssertObj(&stb);
		return Compare(stb) < 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is less than a zero-terminated string psz of the same
		character type, based on a case sensitive naive binary comparison
		(see ${StrBase<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator < (const XChar * psz) const
	{
		AssertPszN(psz);
		return Compare(psz, StrLen(psz)) < 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is greater than another StrBase<> of the same character
		type, based on a case sensitive naive binary comparison (see ${StrBase<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator > (const StrBase<XChar> & stb) const
	{
		AssertObj(&stb);
		return Compare(stb) > 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is greater than a zero-terminated string psz of the same
		character type, based on a case sensitive naive binary comparison
		(see ${StrBase<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator > (const XChar * psz) const
	{
		AssertPszN(psz);
		return Compare(psz, StrLen(psz)) > 0;
	}

	//:> REVIEW Testing(LarryW): Is the following comment still true?
	//:> The operators <= and => will return true if two NULL buffers are compared.
	//:> However, the equality operator == will return false if a buffer m_pbuf is NULL.
	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is less than or equal to another StrBase<> of the same
		character type, based on a case sensitive naive binary comparison
		(see ${StrBase<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator <= (const StrBase<XChar> & stb) const
	{
		AssertObj(&stb);
		return Compare(stb) <= 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is less than or equal to a zero-terminated string psz of
		the same character type, based on a case sensitive naive binary comparison
		(see ${StrBase<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator <= (const XChar * psz) const
	{
		AssertPszN(psz);
		return Compare(psz, StrLen(psz)) <= 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is greater than or equal to another StrBase<> of the same
		character type, based on a case sensitive naive binary comparison
		(see ${StrBase<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator >= (const StrBase<XChar> & stb) const
	{
		AssertObj(&stb);
		return Compare(stb) >= 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is greater than or equal to a zero-terminated string psz
		of the same character type, based on a case sensitive naive binary comparison
		(see ${StrBase<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator >= (const XChar * psz) const
	{
		AssertPszN(psz);
		return Compare(psz, StrLen(psz)) >= 0;
	}


	//:> Case insensitive compare.
	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is equal to another StrBase<> of the same character type,
		based on a case insensitive comparison.
	------------------------------------------------------------------------------------------*/
	bool EqualsCI(const StrBase<XChar> & stb) const
	{
		AssertObj(this);
		AssertObj(&stb);
		if (m_pbuf == stb.m_pbuf)
			return true;
		if (m_pbuf->m_cb != stb.m_pbuf->m_cb)
			return false;
		return EqualsRgchCI(m_pbuf->m_rgch, stb.m_pbuf->m_rgch, m_pbuf->Cch());
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is equal to the value of the zero-terminated string psz,
		of the same character type, based on a case insensitive comparison.
	------------------------------------------------------------------------------------------*/
	bool EqualsCI(const XChar * psz) const
	{
		AssertObj(this);
		AssertPszN(psz);
		return EqualsCI(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBase<> is equal to the value of the string (prgch, cch), of the
		same character type, based on a case insensitive comparison.
	------------------------------------------------------------------------------------------*/
	bool EqualsCI(const XChar * prgch, int cch) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		if (m_pbuf->m_cb != cch * isizeof(XChar))
			return false;
		return EqualsRgchCI(m_pbuf->m_rgch, prgch, m_pbuf->Cch());
	}


	//:>****************************************************************************************
	//:>	Extraction.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Extract the first (that is, leftmost) cch characters from this StrBase<> and return a
		copy of the extracted substring. If cch exceeds the string length, then the entire
		string is extracted.
	------------------------------------------------------------------------------------------*/
	StrBase<XChar> Left(int cch) const
	{
		AssertObj(this);
		Assert(0 <= cch);

		int cchCur = Length();
		if (cch >= cchCur)
			return *this;
		return StrBase<XChar>(m_pbuf->m_rgch, cch);
	}

	/*------------------------------------------------------------------------------------------
		Extract a substring of length cch characters from this StrBase<>, starting at position
		ichMin. Return a copy of the extracted substring. If cch exceeds the string length
		minus ichMin, then the right-most substring is extracted. If ichMin exceeds the string
		length, an empty string is returned.
	------------------------------------------------------------------------------------------*/
	StrBase<XChar> Mid(int ichMin, int cch) const
	{
		AssertObj(this);
		Assert(0 <= ichMin);
		Assert(0 <= cch);

		// If ichMin exceeds the string length, return an empty string.
		int cchCur = Length();
		if (ichMin >= cchCur || cch <= 0)
			return StrBase<XChar>();

		if (cch >= cchCur - ichMin)
		{
			if (!ichMin)
				return *this;
			cch = cchCur - ichMin;
		}
		return StrBase<XChar>(m_pbuf->m_rgch + ichMin, cch);
	}

	/*------------------------------------------------------------------------------------------
		Extract the last (that is, rightmost) cch characters from this StrBase<> and return a
		copy of the extracted substring. If cch exceeds the string length, then the entire
		string is extracted.
	------------------------------------------------------------------------------------------*/
	StrBase<XChar> Right(int cch) const
	{
		AssertObj(this);
		Assert(0 <= cch);

		int cchCur = Length();
		if (cch >= cchCur)
			return *this;
		return StrBase<XChar>(m_pbuf->m_rgch + cchCur - cch, cch);
	}


	//:>****************************************************************************************
	//:>	Conversion.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Convert characters to lower case. If this string object is sharing a buffer, then the
		existing characters are copied into a new buffer solely owned by this StrBase<>.
	------------------------------------------------------------------------------------------*/
	void ToLower(void);

	/*------------------------------------------------------------------------------------------
		Convert characters to upper case. If this string object is sharing a buffer, then the
		existing characters are copied into a new buffer solely owned by this StrBase<>.
	------------------------------------------------------------------------------------------*/
	void ToUpper(void);

	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with the characters from stb, a
		StrBase<> consisting of the other type of character.
	------------------------------------------------------------------------------------------*/
	void Replace(int ichMin, int ichLim, StrBase<YChar> & stb)
	{
		AssertObj(this);
		Assert((uint)ichMin <= (uint)ichLim && (uint)ichLim <= (uint)m_pbuf->Cch());
		AssertObj(&stb);

		_Replace(ichMin, ichLim, stb.m_pbuf->m_rgch, 0, stb.m_pbuf->Cch());
	}

	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with the characters from stb, a
		StrBase<> consisting of the same type of character.
	------------------------------------------------------------------------------------------*/
	void Replace(int ichMin, int ichLim, StrBase<XChar> & stb)
	{
		AssertObj(this);
		Assert((uint)ichMin <= (uint)ichLim && (uint)ichLim <= (uint)m_pbuf->Cch());
		AssertObj(&stb);

		if (0 == ichMin && ichLim == m_pbuf->Cch())
			_SetBuf(stb.m_pbuf);
		else
			_Replace(ichMin, ichLim, stb.m_pbuf->m_rgch, 0, stb.m_pbuf->Cch());
	}

	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with the characters from the
		zero-terminated string psz of either character type.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		void Replace(int ichMin, int ichLim, const AnyChar * psz)
	{
		AssertObj(this);
		Assert((uint)ichMin <= (uint)ichLim && (uint)ichLim <= (uint)m_pbuf->Cch());
		AssertPszN(psz);
		_Replace(ichMin, ichLim, psz, 0, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with the characters from the
		string (prgch, cch) of either character type.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		void Replace(int ichMin, int ichLim, const AnyChar * prgch, int cch)
	{
		AssertObj(this);
		Assert((uint)ichMin <= (uint)ichLim && (uint)ichLim <= (uint)m_pbuf->Cch());
		AssertArray(prgch, cch);
		_Replace(ichMin, ichLim, prgch, 0, cch);
	}

	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with cchIns instances of the character
		chIns.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		void ReplaceFill(int ichMin, int ichLim, const AnyChar chIns, int cchIns)
	{
		AssertObj(this);
		Assert((uint)ichMin <= (uint)ichLim && (uint)ichLim <= (uint)m_pbuf->Cch());
		Assert(cchIns >= 0);
		_Replace(ichMin, ichLim, NULL, chIns, cchIns);
	}

	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with the characters from the
		string with id stid defined in a resource header file.
	------------------------------------------------------------------------------------------*/
	void Replace(int ichMin, int ichLim, const int stid)
	{
		AssertObj(this);
		Assert((uint)ichMin <= (uint)ichLim && (uint)ichLim <= (uint)m_pbuf->Cch());
		const wchar *prgchw;
		int cch;

		::GetResourceString(&prgchw, &cch, stid);
		if (cch)
			_Replace(ichMin, ichLim, prgchw, 0, cch);
	}

	//:>****************************************************************************************
	//:>	Formatting.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBase<> with a new string constructed by formatting the
		string template stbFmt, another StrBase<>. See FormatText.
	------------------------------------------------------------------------------------------*/
	void Format(StrBase<XChar> * stbFmt, ...)
	{
		AssertObj(this);
		AssertObj(stbFmt);

		va_list argList;
		va_start(argList, stbFmt);
		FormatCore(stbFmt->m_pbuf->m_rgch, stbFmt->m_pbuf->Cch(), argList);
		va_end(argList);
	}

	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBase<> with a new string constructed by formatting the
		zero-terminated string template pszFmt of the same character type. See FormatText.
	------------------------------------------------------------------------------------------*/
	void Format(const XChar * pszFmt, ...)
	{
		AssertObj(this);
		AssertPsz(pszFmt);

		va_list argList;
		va_start(argList, pszFmt);
		FormatCore(pszFmt, StrLen(pszFmt), argList);
		va_end(argList);
	}

	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBase<> with a new string constructed by formatting the
		zero-terminated string template pszFmt of the other character type. See FormatText.
	------------------------------------------------------------------------------------------*/
	void Format(const YChar * pszFmt, ...)
	{
		AssertObj(this);
		AssertPsz(pszFmt);

		va_list argList;
		va_start(argList, pszFmt);
		FormatCore(pszFmt, StrLen(pszFmt), argList);
		va_end(argList);
	}

#ifndef WIN32
	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBase<> with a new string constructed by formatting the
		zero-terminated string template pszFmt of the third character type. See FormatText.
	------------------------------------------------------------------------------------------*/
	void Format(const ZChar * pszFmt, ...)
	{
		AssertObj(this);
		AssertPsz(pszFmt);

		va_list argList;
		va_start(argList, pszFmt);
		FormatCore(pszFmt, StrLen(pszFmt), argList);
		va_end(argList);
	}
#endif

	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBase<> with a new string constructed by formatting the
		string template (prgchFmt, cchFmt), of the same character type. See FormatText.
	------------------------------------------------------------------------------------------*/
	void FormatRgch(const XChar * prgchFmt, int cchFmt, ...)
	{
		AssertObj(this);
		AssertArray(prgchFmt, cchFmt);
		va_list argList;
		va_start(argList, cchFmt);
		FormatCore(prgchFmt, cchFmt, argList);
		va_end(argList);
	}

	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBase<> with a new string constructed by formatting the
		string template (prgchFmt, cchFmt), of the other character type. See FormatText.
	------------------------------------------------------------------------------------------*/
	void FormatRgch(const YChar * prgchFmt, int cchFmt, ...)
	{
		AssertObj(this);
		AssertArray(prgchFmt, cchFmt);

		va_list argList;
		va_start(argList, cchFmt);
		FormatCore(prgchFmt, cchFmt, argList);
		va_end(argList);
	}

#ifndef WIN32
	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBase<> with a new string constructed by formatting the
		string template (prgchFmt, cchFmt), of the third character type. See FormatText.
	------------------------------------------------------------------------------------------*/
	void FormatRgch(const ZChar * prgchFmt, int cchFmt, ...)
	{
		AssertObj(this);
		AssertArray(prgchFmt, cchFmt);
		va_list argList;
		va_start(argList, cchFmt);
		FormatCore(prgchFmt, cchFmt, argList);
		va_end(argList);
	}
#endif

	/*------------------------------------------------------------------------------------------
		Assign the value of this StrBase<> to the result of formatting the string template with
		id stidFmt defined in a resource header file. See FormatText.
	------------------------------------------------------------------------------------------*/
	void FormatLoad(int stidFmt, ...)
	{
		AssertObj(this);
		const wchar *prgchwFmt;
		int cchFmt;

		::GetResourceString(&prgchwFmt, &cchFmt, stidFmt);
		va_list argList;
		va_start(argList, stidFmt);
		FormatCore(prgchwFmt, cchFmt, argList);
		va_end(argList);
	}

	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBase<> with a new string constructed by formatting the
		string template (prgchFmt, cchFmt), of the same character type. See FormatText.

		@h3{Parameters}
		@code{
			prgchFmt -- string, of the same type of characters as this StrBase<>, used as the
						template.
			cchFmt -- number of characters in the template string.
			vaArgList -- additional parameters used with the template string.
		}
	------------------------------------------------------------------------------------------*/
	void FormatCore(const XChar * prgchFmt, int cchFmt, va_list vaArgList);

	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBase<> with a new string constructed by formatting the
		string template (prgchFmt, cchFmt), of the other character type. See FormatText.

		@h3{Parameters}
		@code{
			prgchFmt -- string, of the other type of characters as this StrBase<>, used as the
						template.
			cchFmt -- number of characters in the template string.
			vaArgList -- additional parameters used with the template string.
		}
	------------------------------------------------------------------------------------------*/
	void FormatCore(const YChar * prgchFmt, int cchFmt, va_list vaArgList);

#ifndef WIN32
	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBase<> with a new string constructed by formatting the
		string template (prgchFmt, cchFmt), of the third character type. See FormatText.

		@h3{Parameters}
		@code{
			prgchFmt -- string, of the other type of characters as this StrBase<>, used as the
						template.
			cchFmt -- number of characters in the template string.
			prguData -- additional parameters used with the template string.
		}
	------------------------------------------------------------------------------------------*/
	void FormatCore(const ZChar * prgchFmt, int cchFmt, va_list vaArgList);
#endif

	//:> Format-appending strings.
	/*------------------------------------------------------------------------------------------
		Append, to the buffer of this StrBase<>, a string constructed by formatting the string
		template stbFmt, another StrBase<>. See FormatText.
	------------------------------------------------------------------------------------------*/
	void FormatAppend(StrBase<XChar> * stbFmt, ...)
	{
		AssertObj(this);
		AssertObj(stbFmt);

		va_list argList;
		va_start(argList, stbFmt);
		FormatAppendCore(stbFmt->m_pbuf->m_rgch, stbFmt->m_pbuf->Cch(), argList);
		va_end(argList);
	}

	/*------------------------------------------------------------------------------------------
		Append, to the buffer of this StrBase<>, a string constructed by formatting the
		zero-terminated string template pszFmt of any character type. See FormatText.
	------------------------------------------------------------------------------------------*/
	template<class AnyChar>
	void FormatAppend(const AnyChar * pszFmt, ...)
	{
		AssertObj(this);
		AssertPsz(pszFmt);
		va_list argList;
		va_start(argList, pszFmt);
		FormatAppendCore(pszFmt, StrLen(pszFmt), argList);
		va_end(argList);
	}

	/*------------------------------------------------------------------------------------------
		Append, to the buffer of this StrBase<>, a string constructed by formatting the string
		template with id stidFmt defined in a resource header file. See FormatText.
	------------------------------------------------------------------------------------------*/
	void FormatAppendLoad(int stidFmt, ...)
	{
		AssertObj(this);
		const wchar *prgchwFmt;
		int cchFmt;

		::GetResourceString(&prgchwFmt, &cchFmt, stidFmt);
		va_list argList;
		va_start(argList, stidFmt);
		FormatAppendCore(prgchwFmt, cchFmt, argList);
		va_end(argList);
	}

	/*------------------------------------------------------------------------------------------
		Append, to the buffer of this StrBase<>, a string constructed by formatting the
		string template (prgchFmt, cchFmt) of any character type. See FormatText.
	------------------------------------------------------------------------------------------*/
	template<class AnyChar>
	void FormatAppendRgch(const AnyChar * prgchFmt, int cchFmt, ...)
	{
		AssertObj(this);
		AssertArray(prgchFmt, cchFmt);
		va_list argList;
		va_start(argList, cchFmt);
		FormatAppendCore(prgchFmt, cchFmt, argList);
		va_end(argList);
	}

	/*------------------------------------------------------------------------------------------
		Append, to the buffer of this StrBase<>, a new string constructed by formatting the
		string template (prgchFmt, cchFmt) of the same character type. See FormatText.

		@h3{Parameters}
		@code{
			prgchFmt -- string, of the same type of characters as this StrBase<>, used as the
						template.
			cchFmt -- number of characters in the template string.
			vaArgList -- additional parameters used with the template string.
		}
	------------------------------------------------------------------------------------------*/
	void FormatAppendCore(const XChar * prgchFmt, int cchFmt, va_list vaArgList);

	/*------------------------------------------------------------------------------------------
		Append, to the buffer of this StrBase<>, a new string constructed by formatting the
		string template (prgchFmt, cchFmt) of the other character type. See FormatText.

		@h3{Parameters}
		@code{
			prgchFmt -- string, of the other type of characters as this StrBase<>, used as the
						template.
			cchFmt -- number of characters in the template string.
			vaArgList		-- additional parameters used with the template string.
		}
	------------------------------------------------------------------------------------------*/
	void FormatAppendCore(const YChar * prgchFmt, int cchFmt, va_list vaArgList);

#ifndef WIN32
	/*------------------------------------------------------------------------------------------
		Append, to the buffer of this StrBase<>, a new string constructed by formatting the
		string template (prgchFmt, cchFmt) of the third character type. See FormatText.

		@h3{Parameters}
		@code{
			prgchFmt -- string, of the third type of characters as this StrBase<>, used as the
						template.
			cchFmt -- number of characters in the template string.
			prguData -- additional parameters used with the template string.
		}
	------------------------------------------------------------------------------------------*/
	void FormatAppendCore(const ZChar * prgchFmt, int cchFmt, va_list vaArgList);
#endif

	//:>****************************************************************************************
	//:>	Searching.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no less than ichMin, of the first character in this
		StrBase<> that matches the requested character, ch. Return -1 if the character, ch, is
		not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int FindCh(XChar ch, int ichMin = 0) const
	{
		AssertObj(this);
		Assert(ichMin >= 0);

		int ich;
		int cch = Length();

		for (ich = ichMin; ich < cch; ich++)
		{
			if (m_pbuf->m_rgch[ich] == ch)
				return ich;
		}

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no less than ichMin, of the first character in this
		StrBase<> that matches the requested character, ch, not considering the case. Return -1
		if the character, ch, is not found.
	------------------------------------------------------------------------------------------*/
	int FindChCI(XChar ch, int ichMin = 0) const
	{
		AssertObj(this);
		Assert(ichMin >= 0);

		int ich;
		int cch = Length();

		ch = ::ToLower(ch);
		for (ich = ichMin; ich < cch; ich++)
		{
			if (::ToLower(m_pbuf->m_rgch[ich]) == ch)
				return ich;
		}

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no greater than ichLast, of the last character in this
		StrBase<> that matches the requested character, ch. Return -1 if the character,	ch, is
		not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int ReverseFindCh(XChar ch, int ichLast = 0x7FFFFFFF) const
	{
		AssertObj(this);
		Assert(ichLast >= 0);

		if (ichLast >= Length())
			ichLast = Length() - 1;

		int ich;
		for (ich = ichLast; ich >= 0; --ich)
		{
			if (m_pbuf->m_rgch[ich] == ch)
				return ich;
		}

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no greater than ichLast, of the last character in this
		StrBase<> that matches the requested character, ch, not considering the case. Return
		-1 if the character, ch, is	not found.
	------------------------------------------------------------------------------------------*/
	int ReverseFindChCI(XChar ch, int ichLast = 0x7FFFFFFF) const
	{
		AssertObj(this);
		Assert(ichLast >= 0);

		if (ichLast >= Length())
			ichLast = Length() - 1;
		ch = ::ToLower(ch);

		int ich;
		for (ich = ichLast; ich >= 0; --ich)
		{
			if (::ToLower(m_pbuf->m_rgch[ich]) == ch)
				return ich;
		}

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no less than ichMin, of the first character of the first
		substring in this StrBase<> that matches the substring, stb, passed as a parameter.
		Return -1 if the substring is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int FindStr(const StrBase<XChar> & stb, int ichMin = 0) const
	{
		AssertObj(this);
		AssertObj(&stb);
		Assert(ichMin >= 0);

		if (m_pbuf == stb.m_pbuf)
			return ichMin == 0 ? 0 : -1;

		return FindStr(stb.m_pbuf->m_rgch, stb.Length(), ichMin);
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no less than ichMin, of the first character of the first
		substring in this StrBase<> that matches the substring, psz, passed as a parameter.
		Return -1 if the substring is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int FindStr(const XChar * psz, int ichMin = 0) const
	{
		AssertObj(this);
		AssertPszN(psz);
		Assert(ichMin >= 0);

		return FindStr(psz, StrLen(psz), ichMin);
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no less than ichMin, of the first character of the first
		substring in this StrBase<> that matches the substring, (prgch, cch), passed as
		parameters. Return -1 if the substring is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int FindStr(const XChar * prgch, int cch, int ichMin) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		Assert(ichMin >= 0);

		if (!cch)
			return ichMin <= Length() ? ichMin : -1;

		// Last position in m_rgch where prgch can be.
		int ichLast = Length() - cch;
		int ich;

		for (ich = ichMin; ich <= ichLast; ich++)
		{
			if (m_pbuf->m_rgch[ich] == prgch[0] &&
				0 == memcmp(m_pbuf->m_rgch + ich, prgch, cch * isizeof(XChar)))
			{
				return ich;
			}
		}

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no less than ichMin, of the first character of the first
		substring in this StrBase<> that matches the substring, stb, passed as a parameter,
		not considering the case. Return -1 if the substring is not found.
	------------------------------------------------------------------------------------------*/
	int FindStrCI(const StrBase<XChar> & stb, int ichMin = 0) const
	{
		AssertObj(this);
		AssertObj(&stb);
		Assert(ichMin >= 0);

		if (m_pbuf == stb.m_pbuf)
			return ichMin == 0 ? 0 : -1;

		return FindStrCI(stb.m_pbuf->m_rgch, stb.Length(), ichMin);
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no less than ichMin, of the first character of the first
		substring in this StrBase<> that matches the substring, psz, passed as a parameter,
		not considering the case. Return -1 if the substring is not found.
	------------------------------------------------------------------------------------------*/
	int FindStrCI(const XChar * psz, int ichMin = 0) const
	{
		AssertObj(this);
		AssertPszN(psz);
		Assert(ichMin >= 0);

		return FindStrCI(psz, StrLen(psz), ichMin);
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no less than ichMin, of the first character of the first
		substring in this StrBase<> that matches the substring, (prgch, cch), passed as
		parameters, not considering the case. Return -1 if the substring is not found.
	------------------------------------------------------------------------------------------*/
	int FindStrCI(const XChar * prgch, int cch, int ichMin) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		Assert(ichMin >= 0);

		if (!cch)
			return ichMin <= Length() ? ichMin : -1;

		// Last position in m_rgch where prgch can be.
		int ichLast = Length() - cch;
		int ich;
		XChar ch = ::ToLower(prgch[0]);

		for (ich = ichMin; ich <= ichLast; ich++)
		{
			if (::ToLower(m_pbuf->m_rgch[ich]) == ch &&
				EqualsRgchCI(m_pbuf->m_rgch + ich, prgch, cch))
			{
				return ich;
			}
		}

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no greater than ichLast, of the first character of the
		last substring in this StrBase<> that matches the substring, stb, passed as a parameter.
		Return -1 if the substring is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int ReverseFindStr(const StrBase<XChar> & stb, int ichLast = 0x7FFFFFFF) const
	{
		AssertObj(this);
		AssertObj(&stb);
		Assert(ichLast >= 0);

		if (m_pbuf == stb.m_pbuf)
			return 0;

		return ReverseFindStr(stb.m_pbuf->m_rgch, stb.Length(), ichLast);
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no greater than ichLast, of the first character of the
		last substring in this StrBase<> that matches the substring, psz, passed as a parameter.
		Return -1 if the substring is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int ReverseFindStr(const XChar * psz, int ichLast = 0x7FFFFFFF) const
	{
		AssertObj(this);
		AssertPszN(psz);
		Assert(ichLast >= 0);

		return ReverseFindStr(psz, StrLen(psz), ichLast);
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no greater than ichLast, of the first character of the
		last substring in this StrBase<> that matches the substring, (prgch, cch), passed as
		parameters. Return -1 if the substring is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int ReverseFindStr(const XChar * prgch, int cch, int ichLast = 0x7FFFFFFF) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		Assert(ichLast >= 0);

		if (ichLast > Length() - cch)
			ichLast = Length() - cch;

		int ich;
		for (ich = ichLast; ich >= 0; --ich)
		{
			if (m_pbuf->m_rgch[ich] == prgch[0] &&
				0 == memcmp(m_pbuf->m_rgch + ich, prgch, cch * isizeof(XChar)))
			{
				return ich;
			}
		}

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no greater than ichLast, of the first character of the
		last substring in this StrBase<> that matches the substring, stb, passed as a parameter,
		not considering the case. Return -1 if the substring is not found.
	------------------------------------------------------------------------------------------*/
	int ReverseFindStrCI(const StrBase<XChar> & stb, int ichLast = 0x7FFFFFFF) const
	{
		AssertObj(this);
		AssertObj(&stb);
		Assert(ichLast >= 0);

		if (m_pbuf == stb.m_pbuf)
			return 0;

		return ReverseFindStrCI(stb.m_pbuf->m_rgch, stb.Length(), ichLast);
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no greater than ichLast, of the first character of the
		last substring in this StrBase<> that matches the substring, psz, passed as a parameter,
		not considering the case. Return -1 if the substring is not found.
	------------------------------------------------------------------------------------------*/
	int ReverseFindStrCI(const XChar * psz, int ichLast = 0x7FFFFFFF) const
	{
		AssertObj(this);
		AssertPszN(psz);
		Assert(ichLast >= 0);

		return ReverseFindStrCI(psz, StrLen(psz), ichLast);
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no greater than ichLast, of the first character of the
		last substring in this StrBase<> that matches the substring, (prgch, cch), passed as
		parameters, not considering the case. Return -1 if the substring is not found.
	------------------------------------------------------------------------------------------*/
	int ReverseFindStrCI(const XChar * prgch, int cch, int ichLast = 0x7FFFFFFF) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		Assert(ichLast >= 0);

		if (ichLast > Length() - cch)
			ichLast = Length() - cch;

		XChar ch = ::ToLower(prgch[0]);
		int ich;
		for (ich = ichLast; ich >= 0; --ich)
		{
			if (::ToLower(m_pbuf->m_rgch[ich]) == ch &&
				EqualsRgchCI(m_pbuf->m_rgch + ich, prgch, cch))
			{
				return ich;
			}
		}

		return -1;
	}

protected:
	friend class StrBase<YChar>;

	//:>****************************************************************************************
	//:>	Struct StrBuffer.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		The struct StrBuffer is used by StrBase<> to store the characters on the heap. StrBuffer
		keeps a reference count. Thus, assignment of one StrBase<> to another simply results in
		the two pointing to the same block of memory.

		StrBuffer stores the byte count in its member variable m_cb immediately followed by the
		characters in its member variable m_rgch[]. This layout matches a BSTR, so if a client
		asks for a BSTR it gets a pointer to m_rgch.

		The static instance of StrBuffer, s_bufEmpty, is used to represent an empty string.

		@h3{Hungarian: buf}
	------------------------------------------------------------------------------------------*/
	struct StrBuffer
	{
	public:
		//:> We track the ref count minus one so the static StrBase::s_bufEmpty is useable
		//:> without initialization by a constructor. This is so other constructors can use
		//:> instances of StrBase without having to deal with the possibility of s_bufEmpty
		//:> being uninitialized.
		long m_crefMinusOne; // The reference count minus one.
		int m_cb; // The byte count.
		XChar m_rgch[1]; // The characters.

#ifdef DEBUG
		// Check to make certain we have a valid internal state for debugging purposes.
		bool AssertValid(void)
		{
			AssertPtr(this);
			Assert(0 <= m_crefMinusOne);
			Assert(m_cb >= 0);
			Assert(0 == m_cb % isizeof(XChar));
			AssertArray(m_rgch, m_cb / isizeof(XChar) + 1);
			Assert(0 == m_rgch[m_cb / isizeof(XChar)]);
			return true;
		}
#endif

		// Static method to create a new StrBuffer. This calls malloc to allocate the buffer.
		static StrBuffer * Create(int cch);

		// Add a reference to this StrBuffer, by incrementing m_crefMinusOne.
		void AddRef(void)
		{
			AssertObj(this);
			DoAssert(0 < InterlockedIncrement(&m_crefMinusOne));
		}

		// Release a reference to this StrBuffer, by decrementing m_crefMinusOne.
		void Release(void)
		{
			AssertObj(this);
			Assert(0 <= m_crefMinusOne);
			if (0 > InterlockedDecrement(&m_crefMinusOne) && this != &s_bufEmpty)
				free(this);
		}

		// Return the count of characters.
		int Cch(void)
		{
			Assert(0 == m_cb % isizeof(XChar));
			return (uint)m_cb / isizeof(XChar);
		}
	}; // End of StrBuffer.


	// Protected StrBase<> memory management functions.
	StrBuffer * m_pbuf; // Pointer to a StrBuffer which holds the characters for this StrBase<>.
	static StrBuffer s_bufEmpty; // The empty buffer (placed 2nd for sake of debugger display).

	/*------------------------------------------------------------------------------------------
		Set the buffer (m_pbuf) for this StrBase<> to pbuf, passed as a parameter, without
		incrementing the reference count. This assumes a reference count is being transferred
		in.
	------------------------------------------------------------------------------------------*/
	void _AttachBuf(StrBuffer * pbuf)
	{
		AssertPtr(pbuf);
		AssertPtr(m_pbuf);
		Assert(pbuf != m_pbuf);
		m_pbuf->Release();
		m_pbuf = pbuf;
		AssertObj(this);
	}

	/*------------------------------------------------------------------------------------------
		Set the buffer (m_pbuf) for this StrBase<> to pbuf, passed as a parameter, by calling
		AddRef() on the buffer, pbuf.
	------------------------------------------------------------------------------------------*/
	void _SetBuf(StrBuffer * pbuf)
	{
		AssertPtr(pbuf);
		AssertPtr(m_pbuf);
		if (pbuf != m_pbuf)
		{
			pbuf->AddRef();
			m_pbuf->Release();
			m_pbuf = pbuf;
		}
		AssertObj(this);
	}

#ifdef WIN32
	void _Replace(int ichMin, int ichLim, const XChar * prgchIns, XChar chIns, int cchIns)
	{
		_Replace(ichMin, ichLim, prgchIns, chIns, cchIns, CP_ACP);
	}
	void _Replace(int ichMin, int ichLim, const YChar * prgchIns, YChar chIns, int cchIns)
	{
		_Replace(ichMin, ichLim, prgchIns, chIns, cchIns, CP_ACP);
	}
#else
	template<typename AnyChar1, typename AnyChar2>
	void _Replace(int ichMin, int ichLim, const AnyChar1 * prgchIns, AnyChar2 chIns, int cchIns)
	{
		_Replace(ichMin, ichLim, prgchIns, chIns, cchIns, CP_UTF8);
	}
	void _Replace(int ichMin, int ichLim, const ZChar * prgchIns, ZChar chIns, int cchIns,
		int nCodePage);
#endif
	void _Replace(int ichMin, int ichLim, const XChar * prgchIns, XChar chIns, int cchIns,
		int nCodePage);
	void _Replace(int ichMin, int ichLim, const YChar * prgchIns, YChar chIns, int cchIns,
		int nCodePage);
	void _Copy(void);

	/*------------------------------------------------------------------------------------------
		Callback for formatting text to a StrBase<>. See FormatText.

		@h3{Parameters}
		@code{
			pv -- pointer to a stream or some type of string object. It is supplied by the
				caller of FormatText and passed on as the first argument whenever pfnWrite is
				called.
			prgch -- string used as the template.
			cch -- number of characters in the template string.
		}
	------------------------------------------------------------------------------------------*/
	static void FormatCallback(void * pv, const XChar * prgch, int cch);

#if WIN32
#ifdef DEBUG
	class Dbw1 : public DebugWatch
	{
		virtual OLECHAR * Watch();
		StrBase<XChar> * m_pstrbase; // so DebugWatch can find string
		friend StrBase<XChar>;
	};
	Dbw1 m_dbw1;
	friend Dbw1;
#endif //DEBUG
#endif

}; //:> End of StrBase<>.


//:Associate with "Generic Text Comparison Functions".
/*----------------------------------------------------------------------------------------------
	Return true if this zero-terminated string is equal to the StrBase<> stb of the same
	character type. (See ${StrBase<>#Equals}).
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	inline bool operator == (const XChar * psz, const StrBase<XChar> & stb)
{
	AssertPszN(psz);
	return stb.Equals(psz, StrLen(psz));
}


/*----------------------------------------------------------------------------------------------
	Return true if this zero-terminated string is not equal to the StrBase<> stb of the same
	character type. (See ${StrBase<>#Equals}).
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	inline bool operator != (const XChar * psz, const StrBase<XChar> & stb)
{
	AssertPszN(psz);
	AssertObj(&stb);
	return !stb.Equals(psz, StrLen(psz));
}


//:Associate with StrBase<>.
// Typedef and instantiate StrBase<wchar> for unicode characters. @h3{Hungarian: stu}
typedef StrBase<wchar> StrUni;

// Typedef and instantiate StrBase<schar> for ansi characters. @h3{Hungarian: sta}
typedef StrBase<schar> StrAnsi;

// Typedef and instantiate StrBase<achar> for achar characters. @h3{Hungarian: str}
typedef StrBase<achar> StrApp;


//:>********************************************************************************************
//:>	Smart string class StrBaseBuf<>.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	StrBaseBufCore<> is the baseclass of StrBaseBuf<> that implements the core functionality
	independent of size. It contains a maximum number of characters stored as part of the
	object.

	A user should not create an instance of StrBaseBufCore<>. Rather, a user should create an
	instance of StrBaseBuf<>, which in turn will create an instance of StrBaseBufCore<>.

	StrBaseBufCore<> stores the byte count in its member variable m_cb immediately followed by
	the first character in its member variable m_rgch. The total buffer space for StrBaseBuf
	is m_rgch followed by StrBaseBuf::m_rgch2. This layout matches a BSTR, so if a client asks
	for a BSTR it gets a pointer to StrBaseBufCore<>.m_rgch.

	If an edit command results in an overflow of characters, this type of string holds those
	characters that fit, and is put into an overflow state by setting the member variable
	m_fOverflow to true. The overflow state propagates in future edit commands on the string.
	Reassignment of characters that fit into the string clears the overflow state.

	For a comparison of the other smart string class, see ${StrBase<>}.

	@h3{Hungarian: stbbc}
----------------------------------------------------------------------------------------------*/

template<typename XChar> class StrBaseBufCore
{
public:
#ifdef DEBUG
	// Check to make certain we have a valid internal state for debugging purposes.
	bool AssertValid(void) const
	{
		AssertPtr(this);
		Assert(m_cb >= 0);
		Assert(m_cb % isizeof(XChar) == 0);
		AssertArray(m_rgch, m_cb / isizeof(XChar) + 1);
		Assert(m_rgch[m_cb / isizeof(XChar)] == 0);
		return true;
	}
#endif // DEBUG

	// The other character type.
	typedef typename CharDefns<XChar>::OtherChar1 YChar;
#ifndef WIN32
	typedef typename CharDefns<XChar>::OtherChar2 ZChar;
#endif

	//:>****************************************************************************************
	//:>	Array-like functionality.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Return the value of the overflow flag, m_fOverflow. If an edit command results in an
		overflow of characters, a StrBaseBuf<> holds those characters that fit, and is put into
		an overflow state by setting the member variable m_fOverflow to true. The overflow state
		propagates in future edit commands on the string. Reassignment of characters that fit
		into the string clears the overflow state.
	------------------------------------------------------------------------------------------*/
	bool Overflow(void) const
	{
		AssertObj(this);
		return m_fOverflow;
	}

	/*------------------------------------------------------------------------------------------
		Return true if m_cb, the count of bytes for this StrBaseBufCore<>, is zero (indicating
		an empty string).
	------------------------------------------------------------------------------------------*/
	bool IsEmpty(void) const
	{
		AssertObj(this);
		return 0 == m_cb;
	}

	/*------------------------------------------------------------------------------------------
		Return the number of char or wchar characters (as opposed to a count of logical
		characters) in this StrBaseBufCore<>.
	------------------------------------------------------------------------------------------*/
	int Length(void) const
	{
		AssertObj(this);
		Assert(m_cb >= 0);
		return (uint)m_cb / isizeof(XChar);
	}

	/*------------------------------------------------------------------------------------------
		Make the StrBaseBufCore<> empty by setting the byte count, m_cb, to zero and setting
		the first character to zero. Also clear (set to false) the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	void Clear(void)
	{
		AssertObj(this);
		_Clear();
	}

	/*------------------------------------------------------------------------------------------
		Return a reference to the character at the zero based index ich. If ich is out of
		range, return zero.
	------------------------------------------------------------------------------------------*/
	XChar GetAt(int ich) const
	{
		AssertObj(this);
		Assert((uint)ich <= (uint)Length() || ich >= 0 && m_fOverflow);

		if (ich > Length())
			return 0;
		return m_rgch[ich];
	}

	/*------------------------------------------------------------------------------------------
		Return a reference to the character at the zero based index ich. The caller is
		responsible to ensure ich is in range.
	------------------------------------------------------------------------------------------*/
	XChar & operator [] (int ich)
	{
		AssertObj(this);
		Assert((uint)ich <= (uint)Length() || ich >= 0 && m_fOverflow);

		return m_rgch[ich];
	}

	/*------------------------------------------------------------------------------------------
		Set the character at index ich to be ch. If ich is out of range, nothing is done.
	------------------------------------------------------------------------------------------*/
	void SetAt(int ich, XChar ch)
	{
		AssertObj(this);

		if ((uint)ich > (uint)Length())
		{
			Assert(ich >= 0 && m_fOverflow);
			return;
		}
		m_rgch[ich] = ch;
	}


	//:>****************************************************************************************
	//:>	Access.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Return a pointer to the characters. This is read-only! Do not use this after the
		lifetime of the object.
	------------------------------------------------------------------------------------------*/
	const XChar * Chars(void) const
	{
		AssertObj(this);
		return m_rgch;
	}

	/*------------------------------------------------------------------------------------------
		Return a pointer to the characters as a BSTR; this is only valid when XChar is OLECHAR.
		Do not use this after the lifetime of the object.
	------------------------------------------------------------------------------------------*/
	XChar * Bstr(void)
	{
		AssertObj(this);
		Assert(isizeof(XChar) == isizeof(OLECHAR));
		return m_rgch;
	}

	/*------------------------------------------------------------------------------------------
		Get an allocated BSTR that the caller is responsible for freeing.
	------------------------------------------------------------------------------------------*/
	void GetBstr(BSTR * pbstr) const;


	//:>****************************************************************************************
	//:>	Logical operators.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Return a read-only pointer to the characters. This is the cast operator.
	------------------------------------------------------------------------------------------*/
	operator const XChar *(void) const
	{
		AssertObj(this);
		return m_rgch;
	}

	/*------------------------------------------------------------------------------------------
		Return true if m_cb, the count of bytes, is greater than zero.
	------------------------------------------------------------------------------------------*/
	operator bool(void) const
	{
		AssertObj(this);
		return m_cb > 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if m_cb, the count of bytes, is equal to zero.
	------------------------------------------------------------------------------------------*/
	bool operator !(void) const
	{
		AssertObj(this);
		return 0 == m_cb;
	}


	//:>****************************************************************************************
	//:>	Comparison.
	//:>****************************************************************************************

	//:> Equality operators.
	/*------------------------------------------------------------------------------------------
	@null{
		The Equals method differs from the Compare method when a StrBaseBufCore<> is in the
		overflow state. The Equals method will immediately return false whenever a
		StrBaseBufCore<> is in the overflow state, whether the StrBaseBufCore<> is the receiver
		of the method call or is a parameter to the call.

		The Compare method, on the other hand, compares the contents of a StrBaseBufCore<> even
		if it is in the overflow state. The Compare method is used by the operators <, >, <=,
		and =>.
	}
	------------------------------------------------------------------------------------------*/
	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is equal to the value of stbbc, another
		StrBaseBufCore<> of the same character type. Two StrBaseBufCore<>'s are equal if they
		both contain the exact sequence of characters and if both have the same character count.
		If either this StrBaseBufCore<> or the parameter, stbbc, is in the overflow state,
		return false.
	------------------------------------------------------------------------------------------*/
	bool Equals(const StrBaseBufCore<XChar> & stbbc) const
	{
		AssertObj(this);
		AssertObj(&stbbc);
		if (m_fOverflow || stbbc.Overflow())
			return false;
		if (m_cb != stbbc.m_cb)
			return false;
		return 0 == memcmp(m_rgch, stbbc.m_rgch, m_cb);
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is equal to the value of the zero-terminated string
		psz of the same character type. Two strings are equal if they both contain the exact
		sequence of characters and if both have the same character count. If this
		StrBaseBufCore<> is in the overflow state, return false.
	------------------------------------------------------------------------------------------*/
	bool Equals(const XChar * psz) const
	{
		AssertObj(this);
		AssertPszN(psz);
		return Equals(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is equal to the value of the string (prgch, cch)
		of the same character type. Two strings are equal if they both contain the exact
		sequence of characters and if both have the same character count. If this
		StrBaseBufCore<> is in the overflow state, return false.
	------------------------------------------------------------------------------------------*/
	bool Equals(const XChar * prgch, int cch) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		if (m_fOverflow)
			return false;
		if (m_cb != cch * isizeof(XChar))
			return false;
		return 0 == memcmp(m_rgch, prgch, m_cb);
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is equal to the value of stbbc, another
		StrBaseBufCore<> of the same character type. Two StrBaseBufCore<>'s are equal if they
		both contain the exact sequence of characters and if both have the same character count.
		If either this StrBaseBufCore<> or the parameter, stbbc, is in the overflow state,
		return false. (See ${StrBaseBufCore<>#Equals}).
	------------------------------------------------------------------------------------------*/
	bool operator == (const StrBaseBufCore<XChar> & stbbc) const
	{
		AssertObj(&stbbc);
		return Equals(stbbc);
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is equal to the value of the zero-terminated string
		psz, of the same character type. Two strings are equal if they both contain the exact
		sequence of characters and if both have the same character count. If this
		StrBaseBufCore<> is in the overflow state, return false.
		(See ${StrBaseBufCore<>#Equals}).
	------------------------------------------------------------------------------------------*/
	bool operator == (const XChar * psz) const
	{
		AssertPszN(psz);
		return Equals(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is not equal to the value of stbbc, another
		StrBaseBufCore<> of the same character type. Two StrBaseBufCore<>'s are equal if they
		both contain the exact sequence of characters and if both have the same character count.
		If either this StrBaseBufCore<> or the parameter, stbbc, is in the overflow state,
		return false. (See ${StrBaseBufCore<>#Equals}).
	------------------------------------------------------------------------------------------*/
	bool operator != (const StrBaseBufCore<XChar> & stbbc) const
	{
		AssertObj(&stbbc);
		return !Equals(stbbc);
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is not equal to the value of the zero-terminated
		string psz, of the same character type. Two strings are equal if they both contain the
		exact sequence of characters and if both have the same character count. If this
		StrBaseBufCore<> is in the overflow state, return false.
		(See ${StrBaseBufCore<>#Equals}).
	------------------------------------------------------------------------------------------*/
	bool operator != (const XChar * psz) const
	{
		AssertPszN(psz);
		return !Equals(psz, StrLen(psz));
	}

	//:> Greater than and less than comparisons.
	/*------------------------------------------------------------------------------------------
		Case sensitive naive binary comparison of this StrBaseBufCore<> with another
		StrBaseBufCore<> that contains the same type of characters.

		@h3{Return value}
		Returns negative, zero, or positive according to whether this StrBaseBufCore<> is less
		than, equal to, or greater than stbbc.
	------------------------------------------------------------------------------------------*/
	int Compare(const StrBaseBufCore<XChar> & stbbc) const
	{
		AssertObj(this);
		AssertObj(&stbbc);
		return Compare(stbbc.m_rgch, stbbc.Length());
	}

	/*------------------------------------------------------------------------------------------
		Case sensitive naive binary comparison of this StrBaseBufCore<> with a zero-terminated
		string psz of the same character type.

		@h3{Return value}
		Returns negative, zero, or positive according to whether this StrBaseBufCore<> is less
		than, equal to, or greater than the zero-terminated string psz.
	------------------------------------------------------------------------------------------*/
	int Compare(const XChar * psz) const
	{
		AssertObj(this);
		AssertPszN(psz);
		return Compare(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Case sensitive naive binary comparison of this StrBaseBufCore<> with a string
		(prgch, cch) of the same character type. (See CompareRgch).

		@h3{Return value}
		Returns negative, zero, or positive according to whether this StrBaseBufCore<> is less
		than, equal to, or greater than the string (prgch, cch).
	------------------------------------------------------------------------------------------*/
	int Compare(const XChar * prgch, int cch) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		return CompareRgch(m_rgch, Length(), prgch, cch);
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is less than another StrBaseBufCore<> of the same
		character type, based on a case sensitive naive binary comparison (see
		${StrBaseBufCore<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator < (const StrBaseBufCore<XChar> & stbbc) const
	{
		AssertObj(&stbbc);
		return Compare(stbbc) < 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is less than a zero-terminated string psz, of the
		same character type, based on a case sensitive naive binary comparison
		(see ${StrBaseBufCore<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator < (const XChar * psz) const
	{
		AssertPszN(psz);
		return Compare(psz, StrLen(psz)) < 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is greater than another StrBaseBufCore<> of the
		same character type, based on a case sensitive naive binary comparison (see
		${StrBaseBufCore<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator > (const StrBaseBufCore<XChar> & stbbc) const
	{
		AssertObj(&stbbc);
		return Compare(stbbc) > 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is greater than a zero-terminated string psz, of
		the same character type, based on a case sensitive naive binary comparison
		(see ${StrBaseBufCore<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator > (const XChar * psz) const
	{
		AssertPszN(psz);
		return Compare(psz, StrLen(psz)) > 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is less than or equal to another StrBaseBufCore<>
		of the same character type, based on a case sensitive naive binary comparison
		(see ${StrBaseBufCore<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator <= (const StrBaseBufCore<XChar> & stbbc) const
	{
		AssertObj(&stbbc);
		return Compare(stbbc) <= 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is less than or equal to a zero-terminated string
		psz, of the same character type, based on a case sensitive naive binary comparison (see
		${StrBaseBufCore<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator <= (const XChar * psz) const
	{
		AssertPszN(psz);
		return Compare(psz, StrLen(psz)) <= 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is greater than or equal to another
		StrBaseBufCore<> of the same character type, based on a case sensitive naive binary
		comparison (see ${StrBaseBufCore<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator >= (const StrBaseBufCore<XChar> & stbbc) const
	{
		AssertObj(&stbbc);
		return Compare(stbbc) >= 0;
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is greater than or equal to a zero-terminated
		string psz, of the same character type, based on a case sensitive naive binary
		comparison (see ${StrBaseBufCore<>#Compare}).
	------------------------------------------------------------------------------------------*/
	bool operator >= (const XChar * psz) const
	{
		AssertPszN(psz);
		return Compare(psz, StrLen(psz)) >= 0;
	}


	//:> Case insensitive compare.
	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is equal to another StrBaseBufCore<> of the same
		character type, based on a case insensitive comparison.
	------------------------------------------------------------------------------------------*/
	bool EqualsCI(const StrBaseBufCore<XChar> & stbbc) const
	{
		AssertObj(this);
		AssertObj(&stbbc);
		if (m_fOverflow || stbbc.Overflow())
			return false;
		if (m_cb != stbbc.m_cb)
			return false;
		return EqualsRgchCI(m_rgch, stbbc.m_rgch, Length());
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is equal to the value of the zero-terminated
		string psz, of the same character type, based on a case insensitive comparison.
	------------------------------------------------------------------------------------------*/
	bool EqualsCI(const XChar * psz) const
	{
		AssertPszN(psz);
		return EqualsCI(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Return true if this StrBaseBufCore<> is equal to the value of the string (prgch, cch)
		of the same character type.
	------------------------------------------------------------------------------------------*/
	bool EqualsCI(const XChar * prgch, int cch) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		if (m_fOverflow)
			return false;
		if (m_cb != cch * isizeof(XChar))
			return false;
		return EqualsRgchCI(m_rgch, prgch, cch);
	}


	//:>****************************************************************************************
	//:>	Conversion.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Convert characters to lower case.
	------------------------------------------------------------------------------------------*/
	void ToLower (void)
	{
		AssertObj(this);
		::ToLower(m_rgch, Length());
		AssertObj(this);
	}

	/*------------------------------------------------------------------------------------------
		Convert characters to upper case.
	------------------------------------------------------------------------------------------*/
	void ToUpper (void)
	{
		AssertObj(this);
		::ToUpper(m_rgch, Length());
		AssertObj(this);
	}

	/*------------------------------------------------------------------------------------------
		Fill the range of characters [ichMin, ichLim) with the character ch.
	------------------------------------------------------------------------------------------*/
	void FillChars(int ichMin, int ichLim, XChar ch)
	{
		AssertObj(this);
		Assert(0 <= (uint)ichMin && (uint)ichMin <= (uint)ichLim &&
			(uint)ichLim <= (uint)Length());

		// Return if there is no range to fill.
		if (ichMin == ichLim)
			return;

		// Fill in the range with ch.
		for (int i = ichMin; i < ichLim; i++)
			m_rgch[i] = ch;

		AssertObj(this);
	}


	//:>****************************************************************************************
	//:>	Searching.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no less than ichMin, of the first character in this
		StrBaseBufCore<> that matches the requested character, ch. Return -1 if the character,
		ch, is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int FindCh(XChar ch, int ichMin = 0) const
	{
		AssertObj(this);
		Assert(ichMin >= 0);

		int cch = Length();
		for (int ich = ichMin; ich < cch; ich++)
		{
			if (m_rgch[ich] == ch)
				return ich;
		}

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no less than ichMin, of the first character in this
		StrBaseBufCore<> that matches the requested character, ch, not considering the case.
		Return -1 if the character, ch, is not found.
	------------------------------------------------------------------------------------------*/
	int FindChCI(XChar ch, int ichMin = 0) const
	{
		AssertObj(this);
		Assert(ichMin >= 0);

		int cch = Length();
		ch = ::ToLower(ch);
		for (int ich = ichMin; ich < cch; ich++)
		{
			if (::ToLower(m_rgch[ich]) == ch)
				return ich;
		}

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no greater than ichLast, of the last character in this
		StrBaseBufCore<> that matches the requested character, ch. Return -1 if the character,
		ch, is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int ReverseFindCh(XChar ch, int ichLast = 0x7FFFFFFF) const
	{
		AssertObj(this);
		Assert(ichLast >= 0);

		if (ichLast >= Length())
			ichLast = Length() - 1;

		for (int ich = ichLast; ich >= 0; --ich)
		{
			if (m_rgch[ich] == ch)
				return ich;
		}

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index, no greater than ichLast, of the last character in this
		StrBaseBufCore<> that matches the requested character, ch, not considering the case.
		Return -1 if the character, ch, is not found.
	------------------------------------------------------------------------------------------*/
	int ReverseFindChCI(XChar ch, int ichLast = 0x7FFFFFFF) const
	{
		AssertObj(this);
		Assert(ichLast >= 0);

		if (ichLast >= Length())
			ichLast = Length() - 1;
		ch = ::ToLower(ch);

		for (int ich = ichLast; ich >= 0; --ich)
		{
			if (::ToLower(m_rgch[ich]) == ch)
				return ich;
		}

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index of the first character of the first substring in this
		StrBaseBufCore<> that matches the substring, stbbc, passed as a parameter. Return -1
		if the substring is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int FindStr(const StrBaseBufCore<XChar> & stbbc) const
	{
		AssertObj(this);
		AssertObj(&stbbc);
		return FindStr(stbbc.m_rgch, stbbc.Length());
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index of the first character of the first substring in this
		StrBaseBufCore<> that matches the substring, psz, passed as a parameter. Return -1
		if the substring is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int FindStr(const XChar * psz) const
	{
		AssertObj(this);
		AssertPszN(psz);
		return FindStr(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index of the first character of the first substring in this
		StrBaseBufCore<> that matches the substring, (prgch, cch), passed as a parameter.
		Return -1 if the substring is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int FindStr(const XChar * prgch, int cch) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		if (0 == m_cb || 0 == cch)
			return -1;

		int ichLast = Length() - cch; // Last position in m_rgch where prgch can be.
		for (int i = 0; i <= ichLast; i++)
			if (m_rgch[i] == prgch[0])
			{
				if (0 == memcmp(m_rgch + i, prgch, cch * isizeof(XChar)))
					return i;
			};

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index of the first character of the first substring in this
		StrBaseBufCore<> that matches the substring, stbbc, passed as a parameter, not
		considering the case. Return -1 if the substring is not found.
	------------------------------------------------------------------------------------------*/
	int FindStrCI(const StrBaseBufCore<XChar> & stbbc) const
	{
		AssertObj(this);
		AssertObj(&stbbc);
		return FindStrCI(stbbc.m_rgch, stbbc.Length());
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index of the first character of the first substring in this
		StrBaseBufCore<> that matches the substring, psz, passed as a parameter, not
		considering the case. Return -1 if the substring is not found.
	------------------------------------------------------------------------------------------*/
	int FindStrCI(const XChar * psz) const
	{
		AssertObj(this);
		AssertPszN(psz);
		return FindStrCI(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index of the first character of the first substring in this
		StrBaseBufCore<> that matches the substring, (prgch, cch), passed as a parameter, not
		considering the case. Return -1 if the substring is not found.
	------------------------------------------------------------------------------------------*/
	int FindStrCI(const XChar * prgch, int cch) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		if (0 == m_cb || 0 == cch)
			return -1;

		int ichLast = Length() - cch; // Last position in m_rgch where prgch can be.
		for (int i = 0; i <= ichLast; i++)
			if (::ToLower(m_rgch[i]) == ::ToLower(prgch[0]))
			{
				if (EqualsRgchCI(m_rgch + i, prgch, cch))
					return i;
			};

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index of the first character of the last substring in this
		StrBaseBufCore<> that matches the substring, stbbc, passed as a parameter. Return -1
		if the substring is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int ReverseFindStr(const StrBaseBufCore<XChar> & stbbc) const
	{
		AssertObj(this);
		AssertObj(&stbbc);
		return ReverseFindStr(stbbc.m_rgch, stbbc.Length());
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index of the first character of the last substring in this
		StrBaseBufCore<> that matches the substring, psz, passed as a parameter. Return -1
		if the substring is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int ReverseFindStr(const XChar * psz) const
	{
		AssertObj(this);
		AssertPszN(psz);
		return ReverseFindStr(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index of the first character of the last substring in this
		StrBaseBufCore<> that matches the substring, (prgch, cch), passed as a parameter.
		Return -1 if the substring is not found. This is case sensitive.
	------------------------------------------------------------------------------------------*/
	int ReverseFindStr(const XChar * prgch, int cch) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		if (0 == m_cb || 0 == cch)
			return -1;

		int ichLast = Length() - cch; // Last position in m_rgch where prgch can be.
		for (int i = ichLast; i >= 0; i--)
			if (m_rgch[i] == prgch[0])
			{
				if (0 == memcmp(m_rgch + i, prgch, cch * isizeof(XChar)))
					return i;
			};

		return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index of the first character of the last substring in this
		StrBaseBufCore<> that matches the substring, stbbc, passed as a parameter, not
		considering the case. Return -1 if the substring is not found.
	------------------------------------------------------------------------------------------*/
	int ReverseFindStrCI(const StrBaseBufCore<XChar> & stbbc) const
	{
		AssertObj(this);
		AssertObj(&stbbc);
		return ReverseFindStrCI(stbbc.m_rgch, stbbc.Length());
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index of the first character of the last substring in this
		StrBaseBufCore<> that matches the substring, psz, passed as a parameter, not
		considering the case. Return -1 if the substring is not found.
	------------------------------------------------------------------------------------------*/
	int ReverseFindStrCI(const XChar * psz) const
	{
		AssertObj(this);
		AssertPszN(psz);
		return ReverseFindStrCI(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Return the zero-based index of the first character of the last substring in this
		StrBaseBufCore<> that matches the substring, (prgch, cch), passed as a parameter, not
		considering the case. Return -1 if the substring is not found.
	------------------------------------------------------------------------------------------*/
	int ReverseFindStrCI(const XChar * prgch, int cch) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		if (0 == m_cb || 0 == cch)
			return -1;

		int ichLast = Length() - cch; // Last position in m_rgch where prgch can be.
		for (int i = ichLast; i >= 0; i--)
			if (::ToLower(m_rgch[i]) == ::ToLower(prgch[0]))
			{
				if (EqualsRgchCI(m_rgch + i, prgch, cch))
					return i;
			};

		return -1;
	}


protected:
	friend class StrBaseBufCore<YChar>;

//:Ignore
//This causes severe problems, for example, Set<StrAnsiBufSmall> cannot add data reliably.
//#ifdef DEBUG
//	class Dbw1 : public DebugWatch
//	{
//		virtual OLECHAR * Watch();
//		StrBaseBufCore<XChar> * m_pstrbase;
//		friend StrBaseBufCore<XChar>;
//	};
//	Dbw1 m_dbw1;
//	friend Dbw1;
//#endif //DEBUG
//:End Ignore

	/*------------------------------------------------------------------------------------------
		This is set to true when the StrBaseBufCore<> is put into an overflow (error) state;
		otherwise it is false. If an edit command results in an overflow of characters, a
		StrBaseBufCore<> holds those characters that fit, and is put into an overflow state by
		setting the member variable m_fOverflow to true. The overflow state propagates in
		future edit commands on the string. Reassignment of characters that fit into the string
		clears the overflow flag. Calling the Clear method also clears the overflow flag.
	------------------------------------------------------------------------------------------*/
	bool m_fOverflow;

	/*------------------------------------------------------------------------------------------
		The count of bytes taken up by the characters in the buffer. StrBaseBufCore<> stores
		the byte count in its member variable m_cb immediately followed by the first character
		in its member variable m_rgch. The total buffer space for StrBaseBuf is m_rgch followed
		by StrBaseBuf::m_rgch2. This layout matches a BSTR, so if a client asks for a BSTR it
		gets a pointer to StrBaseBufCore<>.m_rgch.
	------------------------------------------------------------------------------------------*/
	int m_cb;

	/*------------------------------------------------------------------------------------------
		This holds the first character for a StrBaseBuf<>; the total buffer space for
		StrBaseBuf is m_rgch[1] followed by StrBaseBuf::m_rgch2[kcchMax].
		NO MEMORY after this should be allocated in StrBaseBufCore.
	------------------------------------------------------------------------------------------*/
	XChar m_rgch[1];

	/*------------------------------------------------------------------------------------------
		Generic constructor.
	------------------------------------------------------------------------------------------*/
	StrBaseBufCore<XChar>(void)
	{
		_Clear();
// See the comment above.
//#ifdef DEBUG
//		m_dbw1.m_pstrbase = this;
//#endif // DEBUG
	}

	/*------------------------------------------------------------------------------------------
		Set the length (number of characters) of this StrBaseBufCore<> to be cch. m_rgch[cch]
		is set to zero to mark the end of the characters.
	------------------------------------------------------------------------------------------*/
	void _SetLen(int cch)
	{
		Assert(cch >= 0);
		AssertArray(m_rgch, cch + 1);
		m_rgch[cch] = 0;
		m_cb = cch * isizeof(XChar);
	}

	/*------------------------------------------------------------------------------------------
		Clear the buffer of this StrBaseBufCore<> by setting its length to zero. m_rgch[cch]
		is set to zero to mark the end of the characters. Also clear (set to false) the
		overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	void _Clear(void)
	{
		_SetLen(0);
		m_fOverflow = false;
		AssertObj(this);
	}
}; //:> End of StrBaseBufCore<>.


//:Associate with StrBaseBuf<>.
#define kcchMaxBufDef 260 // This is 260 so it accomodates MAX_PATH characters.

/*----------------------------------------------------------------------------------------------
	StrBaseBuf<> contains a maximum number of characters stored as part of the object, i.e., we
	don't allocate memory in addition to what is stored on the stack. An assignment of one
	StrBaseBuf<> to another copies the characters from one to the other. If an edit command
	results in an overflow of characters, StrBaseBuf<> holds those characters that fit, and is
	put into an overflow state by setting StrBaseBufCore<>.m_fOverflow to true. The overflow
	state propagates in future edit commands on the string. Reassignment of characters that fit
	into the string clears the overflow state.

	The total buffer space for a StrBaseBuf<> is StrBaseBufCore<>.m_rgch followed by the
	StrBaseBuf member variable m_rgch2.

	See ${StrBaseBufCore<>}, which implements methods that are not size dependent. StrBaseBuf<>
	inherits from StrBaseBufCore<>.

	There is a variety of template instantiations of StrBaseBuf<> based on size and character
	type. Each instantiation name includes one of the following substrings:
	@code{
		"Uni" -- refers to unicode (16-bit) characters. In such instantiations, "XChar" is
			replaced with "wchar", which Common.h typedefs as wchar_t.
		"Ansi" -- refers to ansi (8-bit) characters. In such instantiations, "XChar" is
			replaced with "schar" (short character), which Common.h typedefs as char.
		"App" -- refers to "achar" characters. In such instantiations, "XChar" is replaced
			with "achar", which Common.h typedefs as wchar for UNICODE and as schar otherwise.

	The template instantiations of StrBaseBuf<> include the following:

		Name                                Size (chars)   Purpose
		--------------------------------    ------------   -----------------------------------
		StrUniBufSmall, StrAnsiBufSmall,
			StrAppBufSmall                        32       small strings, e.g., for an integer
		StrUniBuf, StrAnsiBuf, StrAppBuf         260       ~1/4 K string, e.g., for a message
		StrUniBufPath, StrAnsiBufPath,
			StrAppBufPath                   MAX_PATH       for path names
		StrUniBufBig, StrAnsiBufBig,
			StrAppBufBig                        1024       1 K for larger strings
		StrUniBufHuge, StrAnsiBufHuge,
			StrAppBufHuge                      16384       10 K for really large strings
	}

	MAX_PATH is defined in Windef.h to be 260 characters, and is the maximum length for a path.
----------------------------------------------------------------------------------------------*/
template<typename XChar, int kcchMax = kcchMaxBufDef>
	class StrBaseBuf : public StrBaseBufCore<XChar>
{
public:
	typedef typename StrBaseBufCore<XChar>::YChar YChar;
#ifndef WIN32
	typedef typename StrBaseBufCore<XChar>::ZChar ZChar;
#endif
	using StrBaseBufCore<XChar>::Length;
	using StrBaseBufCore<XChar>::_SetLen;
	using StrBaseBufCore<XChar>::m_fOverflow;
	using StrBaseBufCore<XChar>::m_rgch;
	using StrBaseBufCore<XChar>::_Clear;

#ifdef DEBUG
	// Check to make certain we have a valid internal state for debugging purposes.
	bool AssertValid(void) const
	{
		AssertPtr(this);
		Assert(kcchMax >= 0);
#if WIN32
		Assert(m_cb <= kcchMax * isizeof(XChar));
#endif
		StrBaseBufCore<XChar>::AssertValid();
		return true;
	}
#endif // DEBUG.

	enum { kcchMaxStr = kcchMax };

	//:>****************************************************************************************
	//:>	Construction.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Generic constructor.
	------------------------------------------------------------------------------------------*/
	StrBaseBuf<XChar, kcchMax>(void)
	{
		AssertObj(this);
	}

	/*------------------------------------------------------------------------------------------
		Construct a new StrBaseBuf<> from a StrBaseBuf<Zchar> of either character type.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBaseBuf<XChar, kcchMax>(const StrBaseBufCore<AnyChar> & stbbc)
	{
		Assign(stbbc);
	}

	/*------------------------------------------------------------------------------------------
		Construct a new StrBaseBuf<> from a zero-terminated string of either character type.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBaseBuf<XChar, kcchMax>(const AnyChar * psz)
	{
		Assign(psz);
	}

	/*------------------------------------------------------------------------------------------
		Construct a new StrBaseBuf<> from a string, of either character type, with cch
		characters.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBaseBuf<XChar, kcchMax>(const AnyChar * prgch, int cch)
	{
		Assign(prgch, cch);
	}

	/*------------------------------------------------------------------------------------------
		Construct a new StrBaseBuf<> from a string with id stid defined in a resource header
		file.
	------------------------------------------------------------------------------------------*/
	StrBaseBuf<XChar, kcchMax>(const int stid)
	{
		const wchar *prgchw;
		int cch;

		::GetResourceString(&prgchw, &cch, stid);
		if (cch)
			Assign(prgchw, cch);
	}


	//:>****************************************************************************************
	//:>	Array-like functionality.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Set the length of the StrBaseBuf<>. This is provided so that the user can clear the
		overflow flag.
	------------------------------------------------------------------------------------------*/
	bool SetLength(int cch)
	{
		Assert((uint)cch <= (uint)kcchMax);
		if ((uint)cch <= (uint)kcchMax)
		{
			_SetLen(cch);
			m_fOverflow = false;
		}
		else
		{
			_SetLen(kcchMax);
			m_fOverflow = true;
		}
		AssertObj(this);
		return !m_fOverflow;
	}


	//:>****************************************************************************************
	//:>	Assignment.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Assign the value of this StrBaseBuf<> to be the same as the value of a StrBaseBuf<> of
		either character type. If there is an overflow, copy the characters that fit and set
		the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		bool Assign(const StrBaseBufCore<AnyChar> & stbbc)
	{
		AssertObj(this);
		AssertObj(&stbbc);

		Assign(stbbc.Chars(), stbbc.Length());
		if (stbbc.Overflow())
			m_fOverflow = true;
		return !m_fOverflow;
	}

	/*------------------------------------------------------------------------------------------
		Assign the characters from the given zero-terminated string psz, of either character
		type, to be the value of this StrBaseBuf<>. If there is an overflow, copy the
		characters that fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		bool Assign(const AnyChar * psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		return Assign(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Assign the characters from the given string (prgch, cch) of the same character type to
		be the value of this StrBaseBuf<>. If there is an overflow, copy the characters that
		fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	bool Assign(const XChar * prgch, int cch);

	/*------------------------------------------------------------------------------------------
		Assign the characters from the given string (prgch, cch) of the other character type to
		be the value of this StrBaseBuf<>. If there is an overflow, copy the characters that
		fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	bool Assign(const YChar * prgch, int cch);

#ifndef WIN32
	/*------------------------------------------------------------------------------------------
		Assign the characters from the given string (prgch, cch) of the third character type to
		be the value of this StrBaseBuf<>. If there is an overflow, copy the characters that
		fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	bool Assign(const ZChar * prgch, int cch);
#endif

	//:> Assignment operators.
	/*------------------------------------------------------------------------------------------
		Assign the value of this StrBaseBuf<> to be the same as the value of a StrBaseBuf<> of
		either character type. If there is an overflow, copy the characters that fit and set
		the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBaseBuf<XChar, kcchMax> & operator = (const StrBaseBufCore<AnyChar> & stbbc)
	{
		AssertObj(this);
		AssertObj(&stbbc);
		Assign(stbbc);
		return *this;
	}
	/*------------------------------------------------------------------------------------------
		Assign the characters from the given zero-terminated string psz, of either character
		type, to be the value of this StrBaseBuf<>. If there is an overflow, copy the
		characters that fit and	set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBaseBuf<XChar, kcchMax> & operator = (const AnyChar * psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		Assign(psz, StrLen(psz));
		return *this;
	}

	/*------------------------------------------------------------------------------------------
		Assign the value of this StrBaseBuf<> to be the same as the string with id stid defined
		in a resource header file. If there is an overflow, copy the characters that fit and
		set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	bool Load(const int stid)
	{
		AssertObj(this);
		const wchar *prgchw;
		int cch;

		::GetResourceString(&prgchw, &cch, stid);
		return Assign(prgchw, cch);
	}

	/*------------------------------------------------------------------------------------------
		Read cch characters from the IStream and set the value of this StrBaseBuf<> to those
		characters. If cch is greater than the maximum number of characters allowed, adjust cch
		to copy the characters that fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	void ReadChars(IStream * pstrm, int cch);


	//:>****************************************************************************************
	//:>	Concatenation.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Append a copy of the value of a StrBaseBufCore<>, of either character type, to the
		value of this StrBaseBuf<>. If there is an overflow, copy the characters that fit and
		set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		bool Append(const StrBaseBufCore<AnyChar> & stbbc)
	{
		AssertObj(this);
		AssertObj(&stbbc);

		if (!Append(stbbc.Chars(), stbbc.Length()) || stbbc.Overflow())
			m_fOverflow = true;
		return !m_fOverflow;
	}

	/*------------------------------------------------------------------------------------------
		Append a copy of the zero-terminated string psz, of either character type, to the value
		of this StrBase<>. If there is an overflow, copy the characters that fit and set the
		overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		bool Append(const AnyChar * psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		return Append(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Append a copy of the characters from the given string (prgch, cch) of the same
		character type to the value of this StrBaseBuf<>. If there is an overflow, copy the
		characters that fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	bool Append(const XChar * prgch, int cch);

	/*------------------------------------------------------------------------------------------
		Append a copy of the characters from the given string (prgch, cch) of the other
		character type to the value of this StrBaseBuf<>. If there is an overflow, copy the
		characters that fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	bool Append(const YChar * prgch, int cch);

#ifndef WIN32
	/*------------------------------------------------------------------------------------------
		Append a copy of the characters from the given string (prgch, cch) of the third
		character type to the value of this StrBaseBuf<>. If there is an overflow, copy the
		characters that fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	bool Append(const ZChar * prgch, int cch);
#endif

	//:> += operators.
	/*------------------------------------------------------------------------------------------
		Append a copy of the value of a StrBaseBufCore<>, which may be of either character
		type, to the value of this StrBaseBuf<>. If there is an overflow, copy the characters
		that fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBaseBuf<XChar, kcchMax> & operator += (const StrBaseBufCore<AnyChar> & stbbc)
	{
		AssertObj(this);
		AssertObj(&stbbc);
		Append(stbbc);
		return *this;
	}

	/*------------------------------------------------------------------------------------------
		Append a copy of the value of the zero-terminated string psz, of either character type,
		to the value of this StrBaseBuf<>. If there is an overflow, copy the characters that
		fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBaseBuf<XChar, kcchMax> & operator += (const AnyChar * psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		Append(psz, StrLen(psz));
		return *this;
	}

	//:> + operators
	/*------------------------------------------------------------------------------------------
		Return a new StrBaseBuf<> with the value of this StrBaseBuf<> followed by the value of
		another StrBaseBuf<>, which may be of either character type. If there is an overflow,
		copy the characters that fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBaseBuf<XChar, kcchMax> operator + (const StrBaseBufCore<AnyChar> & stbbc) const
	{
		AssertObj(this);
		AssertObj(&stbbc);
		StrBaseBuf<XChar, kcchMax> stbbRet(this);
		stbbRet.Append(stbbc);
		return stbbRet;
	}

	/*------------------------------------------------------------------------------------------
		Return a new StrBaseBuf<> with the value of this StrBaseBuf<> followed by a copy of the
		zero-terminated string psz of either character type. If there is an overflow, copy the
		characters that fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		StrBaseBuf<XChar, kcchMax> operator + (const AnyChar * psz) const
	{
		AssertObj(this);
		AssertPszN(psz);
		StrBaseBuf<XChar, kcchMax> stbbRet(this);
		stbbRet.Append(psz);
		return stbbRet;
	}

	/*------------------------------------------------------------------------------------------
		Append a copy of the string with id stid defined in a resource header file to the value
		of this StrBaseBuf<>. If there is an overflow, copy the characters that fit and set
		the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	bool AppendLoad(const int stid)
	{
		AssertObj(this);
		const wchar *prgchw;
		int cch;

		::GetResourceString(&prgchw, &cch, stid);
		return Append(prgchw,  cch);
	}


	//:>****************************************************************************************
	//:>	Extraction.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Extract the first (that is, leftmost) cch characters from this StrBaseBuf<> and return
		a copy of the extracted substring. If cch exceeds the string length, then the entire
		string is extracted.
	------------------------------------------------------------------------------------------*/
	StrBaseBuf<XChar, kcchMax> Left(int cch) const
	{
		AssertObj(this);
		Assert(0 <= cch);

		int cchCur = Length();
		if (cch > cchCur)
			cch = cchCur;

		StrBaseBuf<XChar, kcchMax> stbbRet;
		stbbRet.Assign(m_rgch, cch);
		AssertObj(&stbbRet);
		return stbbRet;
	}

	/*------------------------------------------------------------------------------------------
		Extract a substring of length cch characters from this StrBaseBuf<>, starting at
		position ichMin (zero-based). Return a copy of the extracted substring. If cch exceeds
		the string length minus ichMin, then the right-most substring is extracted. If ichMin
		exceeds the string length, an empty string is returned.
	------------------------------------------------------------------------------------------*/
	StrBaseBuf<XChar, kcchMax> Mid(int ichMin, int cch) const
	{
		AssertObj(this);
		int cchCur = Length();
		Assert(0 <= ichMin && (uint)ichMin <= (uint)cchCur);
		Assert(0 <= cch);

		// If ichMin exceeds the string length, return an empty string.
		if (ichMin > cchCur)
		{
			StrBaseBuf<XChar, kcchMax> stbbRet;
			return stbbRet;
		}

		if (cch > cchCur - ichMin)
			cch = cchCur - ichMin;

		StrBaseBuf<XChar, kcchMax> stbbRet;
		stbbRet.Assign(m_rgch + ichMin, cch);
		AssertObj(&stbbRet);
		return stbbRet;
	}

	/*------------------------------------------------------------------------------------------
		Extract the last (that is, rightmost) cch characters from this StrBaseBuf<> and return
		a copy of the extracted substring. If cch exceeds the string length, then the entire
		string is extracted.
	------------------------------------------------------------------------------------------*/
	StrBaseBuf<XChar, kcchMax> Right(int cch) const
	{
		AssertObj(this);
		Assert(0 <= cch);

		int cchCur = Length();
		if (cch > cchCur)
			cch = cchCur;

		StrBaseBuf<XChar, kcchMax> stbbRet;
		stbbRet.Assign(m_rgch + cchCur - cch, cch);
		AssertObj(&stbbRet);
		return stbbRet;
	}


	//:>****************************************************************************************
	//:>	Conversion.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with the characters from stbbc, a
		StrBaseBuf<> consisting of either type of character. If there is an overflow, copy the
		characters that fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		bool Replace(int ichMin, int ichLim, StrBaseBufCore<AnyChar> & stbbc)
	{
		AssertObj(this);
		AssertObj(&stbbc);
		Assert(0 <= ichMin && ichMin <= ichLim);

		if (!Replace(ichMin, ichLim, stbbc.Chars(), stbbc.Length()) || stbbc.Overflow())
			m_fOverflow = true;
		return !m_fOverflow;
	}

	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with the characters from the
		zero-terminated string psz of either character type. If there is an overflow, copy the
		characters that fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	template<typename AnyChar>
		bool Replace(int ichMin, int ichLim, const AnyChar * psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		return Replace(ichMin, ichLim, psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with the characters from the
		string (prgch, cch) of the same type of character. If there is an overflow, copy the
		characters that fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	bool Replace(int ichMin, int ichLim, const XChar * prgch, int cch);

	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with the characters from the
		string (prgch, cch) of the other type of character. If there is an overflow, copy the
		characters that fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	bool Replace(int ichMin, int ichLim, const YChar * prgch, int cch);

#ifndef WIN32
	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with the characters from the
		string (prgch, cch) of the third type of character. If there is an overflow, copy the
		characters that fit and set the overflow flag, m_fOverflow.
	------------------------------------------------------------------------------------------*/
	bool Replace(int ichMin, int ichLim, const ZChar * prgch, int cch);
#endif

	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with cchIns instances of the character
		chIns. If there is an overflow, copy the characters that fit and set the overflow flag,
		m_fOverflow.
	------------------------------------------------------------------------------------------*/
	bool ReplaceFill(int ichMin, int ichLim, const XChar chIns, int cchIns);


	//:>****************************************************************************************
	//:>	Formatting.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBaseBuf<> with a new string constructed by formatting the
		string template stbbcFmt, another StrBaseBuf<>. See FormatText.
	------------------------------------------------------------------------------------------*/
	bool Format(StrBaseBufCore<XChar> * stbbcFmt, ...)
	{
		AssertObj(this);
		AssertObj(stbbcFmt);
		Assert(!stbbcFmt->Overflow());

		va_list argList;
		va_start(argList, stbbcFmt);
		return FormatCore(stbbcFmt->Chars(), stbbcFmt->Length(), argList);
		va_end(argList);
	}

	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBaseBuf<> with a new string constructed by formatting
		the zero-terminated string template pszFmt of any character type. See FormatText.
	------------------------------------------------------------------------------------------*/
	template<class AnyChar>
	bool Format(const AnyChar * pszFmt, ...)
	{
		AssertObj(this);
		AssertPsz(pszFmt);

		va_list argList;
		va_start(argList, pszFmt);
		bool fReturn = FormatCore(pszFmt, StrLen(pszFmt), argList);
		va_end(argList);
		return fReturn;
	}

	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBaseBuf<> with a new string constructed by formatting the
		string template (prgchFmt, cchFmt) of any character type. See FormatText.
	------------------------------------------------------------------------------------------*/
	template<class AnyChar>
	bool FormatRgch(const AnyChar * prgchFmt, int cchFmt, ...)
	{
		AssertObj(this);
		AssertArray(prgchFmt, cchFmt);

		va_list argList;
		va_start(argList, cchFmt);
		bool fReturn = FormatCore(prgchFmt, cchFmt, argList);
		va_end(argList);
		return fReturn;
	}

	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBaseBuf<> with a new string constructed by formatting the
		string template (prgchFmt, cchFmt) of the same character type. Set m_fOverflow to true
		if FormatText failed. See FormatText.

		@h3{Parameters}
		@code{
			prgchFmt -- string, of the same type of characters as this StrBaseBuf<>, used as the
						template.
			cchFmt -- number of characters in the template string.
			vaArgList -- additional parameters used with the template string.
		}
	------------------------------------------------------------------------------------------*/
	bool FormatCore(const XChar * prgchFmt, int cchFmt, va_list vaArgList);

#ifdef WIN32
	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBaseBuf<> with a new string constructed by formatting the
		string template (prgchFmt, cchFmt) of the other character type. Set m_fOverflow to true
		if FormatText failed. See FormatText.

		@h3{Parameters}
		@code{
			prgchFmt -- string, of the other type of characters as this StrBaseBuf<>, used as the
						template.
			cchFmt -- number of characters in the template string.
			prguData -- additional parameters used with the template string.
		}
	------------------------------------------------------------------------------------------*/
	bool FormatCore(const YChar * prgchFmt, int cchFmt, va_list vaArgList);
#else
	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBaseBuf<> with a new string constructed by formatting the
		string template (prgchFmt, cchFmt) of the other character type. Set m_fOverflow to true
		if FormatText failed. See FormatText.

		@h3{Parameters}
		@code{
			prgchFmt -- string, of the other type of characters as this StrBaseBuf<>, used as the
						template.
			cchFmt -- number of characters in the template string.
			prguData -- additional parameters used with the template string.
		}
	------------------------------------------------------------------------------------------*/
	bool FormatCore(const YChar * prgchFmt, int cchFmt, const uint * prguData);

	/*------------------------------------------------------------------------------------------
		Replace the buffer for this StrBaseBuf<> with a new string constructed by formatting the
		string template (prgchFmt, cchFmt) of the third character type. Set m_fOverflow to true
		if FormatText failed. See FormatText.

		@h3{Parameters}
		@code{
			prgchFmt -- string, of the third type of characters as this StrBaseBuf<>, used as the
						template.
			cchFmt -- number of characters in the template string.
			prguData -- additional parameters used with the template string.
		}
	------------------------------------------------------------------------------------------*/
	bool FormatCore(const ZChar * prgchFmt, int cchFmt, va_list vaArgList);
#endif


	//:> Format-appending strings.
	/*------------------------------------------------------------------------------------------
		Append, to the buffer of this StrBaseBuf<>, a string constructed by formatting the
		string template stbbcFmt, another StrBaseBuf<>. See FormatText.
	------------------------------------------------------------------------------------------*/
	bool FormatAppend(StrBaseBufCore<XChar> * stbbcFmt, ...)
	{
		AssertObj(this);
		AssertObj(stbbcFmt);
		Assert(!stbbcFmt->Overflow());

		va_list argList;
		va_start(argList, stbbcFmt);
		bool fReturn = FormatAppendCore(stbbcFmt->Chars(), stbbcFmt->Length(), argList);
		va_end(argList);
		return fReturn;
	}

	/*------------------------------------------------------------------------------------------
		Append, to the buffer of this StrBaseBuf<>, a string constructed by formatting the
		zero-terminated string template pszFmt of any character type. See FormatText.
	------------------------------------------------------------------------------------------*/
	template<class AnyChar>
	bool FormatAppend(const AnyChar * pszFmt, ...)
	{
		AssertObj(this);
		AssertPsz(pszFmt);

		va_list argList;
		va_start(argList, pszFmt);
		bool fReturn = FormatAppendCore(pszFmt, StrLen(pszFmt), argList);
		va_end(argList);
		return fReturn;
	}

	/*------------------------------------------------------------------------------------------
		Append, to the buffer of this StrBaseBuf<>, a string constructed by formatting the
		string template (prgchFmt, cchFmt) of the same character type. See FormatText.
	------------------------------------------------------------------------------------------*/
	template<class AnyChar>
	bool FormatAppendRgch(const AnyChar * prgchFmt, int cchFmt, ...)
	{
		AssertObj(this);
		AssertArray(prgchFmt, cchFmt);

		va_list argList;
		va_start(argList, cchFmt);
		bool fReturn = FormatAppendCore(prgchFmt, cchFmt, argList);
		va_end(argList);
		return fReturn;
	}

	/*------------------------------------------------------------------------------------------
		Append, to the buffer of this StrBaseBuf<>, a new string constructed by formatting the
		string template (prgchFmt, cchFmt) of the same character type. Set m_fOverflow to true
		if FormatText failed. See FormatText.

		@h3{Parameters}
		@code{
			prgchFmt -- string, of the same type of characters as this StrBaseBuf<>, used as the
						template.
			cchFmt -- number of characters in the template string.
			vaArgList	-- additional parameters used with the template string.
		}
	------------------------------------------------------------------------------------------*/
	bool FormatAppendCore(const XChar * prgchFmt, int cchFmt, va_list vaArgList);

	/*------------------------------------------------------------------------------------------
		Append, to the buffer of this StrBaseBuf<>, a new string constructed by formatting the
		string template (prgchFmt, cchFmt) of the other character type. Set m_fOverflow to true
		if FormatText failed. See FormatText.

		@h3{Parameters}
		@code{
			prgchFmt -- string, of the other type of characters as this StrBaseBuf<>, used as the
						template.
			cchFmt -- number of characters in the template string.
			prguData -- additional parameters used with the template string.
		}
	------------------------------------------------------------------------------------------*/
	bool FormatAppendCore(const YChar * prgchFmt, int cchFmt, va_list vaArgList);

#ifndef WIN32
	/*------------------------------------------------------------------------------------------
		Append, to the buffer of this StrBaseBuf<>, a new string constructed by formatting the
		string template (prgchFmt, cchFmt) of the third character type. Set m_fOverflow to true
		if FormatText failed. See FormatText.

		@h3{Parameters}
		@code{
			prgchFmt -- string, of the third type of characters as this StrBaseBuf<>, used as the
						template.
			cchFmt -- number of characters in the template string.
			prguData -- additional parameters used with the template string.
		}
	------------------------------------------------------------------------------------------*/
	bool FormatAppendCore(const ZChar * prgchFmt, int cchFmt, va_list vaArgList);
#endif

protected:
	// The total buffer space for a StrBaseBuf<> is StrBaseBufCore<>.m_rgch followed by the
	// StrBaseBuf member variable m_rgch2.
	XChar m_rgch2[kcchMax]; // The characters following StrBaseBufCore<>.m_rgch.

	/*------------------------------------------------------------------------------------------
		Callback for formatting text to a StrBaseBuf<>. See FormatText.

		@h3{Parameters}
		@code{
			pv -- pointer to a stream or some type of string object. It is supplied by the
				caller of FormatText and passed on as the first argument whenever pfnWrite is
				called.
			prgch -- string used as the template.
			cch -- number of characters in the template string.
		}
	------------------------------------------------------------------------------------------*/
	static void FormatCallback(void * pv, const XChar * prgch, int cch);
}; //:> End of StrBaseBuf<>.


//:Associate with "Generic Text Comparison Functions".
/*----------------------------------------------------------------------------------------------
	Return true if this zero-terminated string of the same character type is equal to the
	StrBaseBufCore<> stbbc. (See ${StrBaseBufCore<>#Equals}).
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	inline bool operator == (const XChar * psz, const StrBaseBufCore<XChar> & stbbc)
{
	AssertPszN(psz);
	AssertObj(&stbbc);
	return stbbc.Equals(psz, StrLen(psz));
}

/*----------------------------------------------------------------------------------------------
	Return true if this zero-terminated string of the same character type is not equal to the
	StrBaseBufCore<> stbbc. (See ${StrBaseBufCore<>#Equals}).
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	inline bool operator != (const XChar * psz, const StrBaseBufCore<XChar> & stbbc)
{
	AssertPszN(psz);
	AssertObj(&stbbc);
	return !stbbc.Equals(psz, StrLen(psz));
}


//:Associate with StrBaseBuf<>.
const int kcchMaxBufShort = 32; // Size of short StrBaseBuf<>.
const int kcchMaxBufBig = 1024; // Size of large StrBaseBuf<>.
const int kcchMaxBufHuge = 16384; // Size of really large StrBaseBuf<>.

//:>********************************************************************************************
//:>	Instantiations of short StrBaseBuf<>.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Typedef and instantiate short StrBaseBuf<> of 32 unicode characters.
	For example, these can be used for integers.

	@h3{Hungarian: stubs}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<wchar, kcchMaxBufShort> StrUniBufSmall;
/*----------------------------------------------------------------------------------------------
	Typedef and instantiate short StrBaseBuf<> of 32 ansi characters.
	For example, these can be used for integers.

	@h3{Hungarian: stabs}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<schar, kcchMaxBufShort> StrAnsiBufSmall;
/*----------------------------------------------------------------------------------------------
	Typedef and instantiate short StrBaseBuf<> of 32 achar characters.
	For example, these can be used for integers.

	@h3{Hungarian: strbs}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<achar, kcchMaxBufShort> StrAppBufSmall;


//:>********************************************************************************************
//:>	Instantiations of medium StrBaseBuf<>.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Typedef and instantiate medium size (~ 1/4 K) StrBaseBuf<> of 260 unicode characters.
	For example, these can be used for messages.

	@h3{Hungarian: stub}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<wchar, kcchMaxBufDef> StrUniBuf;

/*----------------------------------------------------------------------------------------------
	Typedef and instantiate medium size (~ 1/4 K) StrBaseBuf<> of 260 ansi characters.
	For example, these can be used for messages.

	@h3{Hungarian: stab}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<schar, kcchMaxBufDef> StrAnsiBuf;

/*----------------------------------------------------------------------------------------------
	Typedef and instantiate medium size (~ 1/4 K) StrBaseBuf<> of 260 achar characters.
	For example, these can be used for messages.

	@h3{Hungarian: strb}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<achar, kcchMaxBufDef> StrAppBuf;


//:>********************************************************************************************
//:>	Instantiations of pathname StrBaseBuf<>.
//:>********************************************************************************************
#if MAX_PATH <= kcchMaxBufDef
/*----------------------------------------------------------------------------------------------
	Typedef and instantiate pathname size (~ 1/4 K) StrBaseBuf<> of MAX_PATH unicode characters.
	These can be used for pathnames.

	@h3{Hungarian: stubp}
----------------------------------------------------------------------------------------------*/
typedef StrUniBuf StrUniBufPath;

/*----------------------------------------------------------------------------------------------
	Typedef and instantiate pathname size (~ 1/4 K) StrBaseBuf<> of MAX_PATH ansi characters.
	These can be used for pathnames.

	@h3{Hungarian: stabp}
----------------------------------------------------------------------------------------------*/
typedef StrAnsiBuf StrAnsiBufPath;

/*----------------------------------------------------------------------------------------------
	Typedef and instantiate pathname size (~ 1/4 K) StrBaseBuf<> of MAX_PATH achar characters.
	These can be used for pathnames.

	@h3{Hungarian: strbp}
----------------------------------------------------------------------------------------------*/
typedef StrAppBuf StrAppBufPath;

#else
#warning "MAX_PATH is bigger than kcchMaxDef"
/*----------------------------------------------------------------------------------------------
	Typedef and instantiate pathname size (~ 1/4 K) StrBaseBuf<> of MAX_PATH unicode characters.
	These can be used for pathnames.

	@h3{Hungarian: stubp}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<wchar, MAX_PATH> StrUniBufPath;

/*----------------------------------------------------------------------------------------------
	Typedef and instantiate pathname size (~ 1/4 K) StrBaseBuf<> of MAX_PATH ansi characters.
	These can be used for pathnames.

	@h3{Hungarian: stabp}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<schar, MAX_PATH> StrAnsiBufPath;

/*----------------------------------------------------------------------------------------------
	Typedef and instantiate pathname size (~ 1/4 K) StrBaseBuf<> of MAX_PATH achar characters.
	These can be used for pathnames.

	@h3{Hungarian: strbp}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<achar, MAX_PATH> StrAppBufPath;
#endif


//:>********************************************************************************************
//:>	Instantiations of large StrBaseBuf<>.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Typedef and instantiate large size (~ 1 K) StrBaseBuf<> of 1024 unicode characters.
	Use when you need a large string for something.

	@h3{Hungarian: stubb}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<wchar, kcchMaxBufBig> StrUniBufBig;

/*----------------------------------------------------------------------------------------------
	Typedef and instantiate large size (~ 1 K) StrBaseBuf<> of 1024 ansi characters.
	Use when you need a large string for something.

	@h3{Hungarian: stabb}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<schar, kcchMaxBufBig> StrAnsiBufBig;

/*----------------------------------------------------------------------------------------------
	Typedef and instantiate large size (~ 1 K) StrBaseBuf<> of 1024 achar characters.
	Use when you need a large string for something.

	@h3{Hungarian: strbb}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<achar, kcchMaxBufBig> StrAppBufBig;


//:>********************************************************************************************
//:>	Instantiations of huge StrBaseBuf<>.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Typedef and instantiate really large size (~ 10 K) StrBaseBuf<> of 16384 unicode characters.
	Use when you need a really large string for something.

	@h3{Hungarian: stubh}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<wchar, kcchMaxBufHuge> StrUniBufHuge;

/*----------------------------------------------------------------------------------------------
	Typedef and instantiate really large size (~ 10 K) StrBaseBuf<> of 16384 ansi characters.
	Use when you need a really large string for something.

	@h3{Hungarian: stabh}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<schar, kcchMaxBufHuge> StrAnsiBufHuge;

/*----------------------------------------------------------------------------------------------
	Typedef and instantiate really large size (~ 10 K) StrBaseBuf<> of 16384 achar characters.
	Use when you need a really large string for something.

	@h3{Hungarian: strbh}
----------------------------------------------------------------------------------------------*/
typedef StrBaseBuf<achar, kcchMaxBufHuge> StrAppBufHuge;


//:Associate with StrUtil.
/***********************************************************************************************
	This namespace provides a place for string utility functions to live without having to be
	global functions.
***********************************************************************************************/
namespace StrUtil
{


	UChar32 ToLower(UChar32 uch32Character);

	/*------------------------------------------------------------------------------------------
		Convert the decimal integer string (prgch, cch) to an integer. The result is placed in
		pn. When non-NULL, pcchRead returns the number of characters consumed from the input.
		This may be less than cch if a non-numeric/sign character occurs in the string.

		@h3{Return value}
		@code{
			true, when cch characters are consumed.
			false, when less than cch characters are consumed.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		bool ParseInt(const XChar * prgch, int cch, int * pn, int * pcchRead = NULL);

	/*------------------------------------------------------------------------------------------
		Convert the decimal integer string psz to an integer.

		@h3{Return value}
		@code{
			0, if the string contains a non-alpha character.
			Otherwise, the resulting integer is returned.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		int ParseInt(const XChar * psz);

	/*------------------------------------------------------------------------------------------
		Convert the hexadecimal string (prgch, cch) to an integer. The result is placed in
		pn. When non-NULL, pcchRead returns the number of characters consumed from the input.
		This may be less than cch if a non-numeric/sign character occurs in the string.

		@h3{Return value}
		@code{
			true, when cch characters are consumed.
			false, when less than cch characters are consumed.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		bool ParseHexInt(const XChar * prgch, int cch, int * pn, int * pcchRead = NULL);

	/*------------------------------------------------------------------------------------------
		Convert the hexadecimal string psz to an integer.

		@h3{Return value}
		@code{
			0, if the string contains a non-alpha character.
			Otherwise, the resulting integer is returned.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		int ParseHexInt(const XChar * psz);

	/*------------------------------------------------------------------------------------------
		Convert the roman numeral string (prgch, cch) (any case) to an integer. The result is
		placed in pn. When non-NULL, pcchRead returns the number of characters consumed from the
		input. This may be less than cch if an illegal character occurs in the string.

		@h3{Return value}
		@code{
			true, when cch characters are consumed.
			false, when less than cch characters are consumed.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		bool ParseRomanNumeral(const XChar * prgch, int cch, int * pn, int * pcchRead = NULL);

	/*------------------------------------------------------------------------------------------
		Convert the roman numeral string psz (any case) to an integer.

		@h3{Return value}
		@code{
			0, if the string contains an illegal character.
			Otherwise, the resulting integer is returned.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		int ParseRomanNumeral(const XChar * psz);

	/*------------------------------------------------------------------------------------------
		Convert the alpha outline string (prgch, cch) (any case) to an integer (e.g, a = 1,
		b = 2, z = 26, aa = 27, etc.). The result is placed in pn. When non-NULL, pcchRead
		returns the number of characters consumed from the input. This may be less than cch if
		a non-alpha character occurs in the string.

		@h3{Return value}
		@code{
			true, when cch characters are consumed.
			false, when less than cch characters are consumed.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		bool ParseAlphaOutline(const XChar * prgch, int cch, int * pn, int * pcchRead = NULL);

	/*------------------------------------------------------------------------------------------
		Convert the roman numeral string psz (any case) to an integer.

		@h3{Return value}
		@code{
			0, if the string contains an illegal character.
			Otherwise, the resulting integer is returned.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		int ParseAlphaOutline(const XChar * psz);

	/*------------------------------------------------------------------------------------------
		Convert the generic date string (prgch, cch) (e.g., abt. 23 Mar 1993 BC) to a GenDate
		integer (e.g., -199303232). The result is placed in pgdat. When non-NULL, pcchRead
		returns the number of characters consumed from the input. This may be less than cch if
		an illegal character occurs in the string. The function returns true when cch characters
		are consumed. If it returns false, it probably indicates the input had an error, so the
		output is probably garbage.

		GenDates are stored as an int (4 bytes). This is a decimal representation of a generic
		date without a time. It's up to the software to make sure the int is a valid date.
		The range is 21474 BC through 21474 AD. The format is:
			[-]YYYYMMDDP

		YYYY is the 1-5 digit year (negative is BC) 0000 is unknown.
		MM is the 2 digit month (for BC months it is 13 - M) 00 is unknown
		DD is the 2 digit day (for BC days it is 32 - D) 00 is unknown
		P is one of the following:
		@code{
			0 = Before (If GenDate = 0, it means nothing is entered)
			1 = Exact
			2 = Approximate
			3 = After
		}

		@h3{Return value}
		@code{
			true, when cch characters are consumed.
			false, when less than cch characters are consumed, indicating that pgdat is garbage.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		bool ParseGenDate(const XChar * prgch, int cch, int * pgdat, int * pcchRead = NULL);

	/*------------------------------------------------------------------------------------------
		Convert the generic date string psz to a GenDate integer (e.g., 193012251). (0 is a
		missing generic date).

		@h3{Return value}
		@code{
			0, if the string contains an illegal character.
			Otherwise, the resulting integer is returned.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		int ParseGenDate(const XChar * psz);

	/*------------------------------------------------------------------------------------------
		Convert the date/time string (prgch, cch) to an SilTime. The result is placed in pstim.
		When non-NULL, pcchRead returns the number of characters consumed from the input.
		This may be less than cch if an illegal character occurs in the string, or if the
		date/time string is part of a larger string.

		@h3{Return value}
		@code{
			true, when cch characters are consumed.
			false, when less than cch characters are consumed.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		bool ParseDateTime(const XChar * prgch, int cch, SilTime * pstim, int * pcchRead = NULL);

	/*------------------------------------------------------------------------------------------
		Convert the date/time string psz to an SilTime. (0 is also a valid time).

		@h3{Return value}
		@code{
			0, if the string contains an illegal character.
			Otherwise, the resulting integer is returned.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		SilTime ParseDateTime(const XChar * psz);

	/*------------------------------------------------------------------------------------------
		Convert the date string (prgch, cch) to an SilTime. The result is placed in pstim.
		When non-NULL, pcchRead returns the number of characters consumed from the input.
		This may be less than cch if an illegal character occurs in the string, or if the
		date/time string is part of a larger string.

		@h3{Return value}
		@code{
			true, when cch characters are consumed.
			false, when less than cch characters are consumed.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		bool ParseDate(const XChar * prgch, int cch, SilTime * pstim, int * pcchRead = NULL);

	/*------------------------------------------------------------------------------------------
		Convert the date string psz to an SilTime. (0 is also a valid time).

		@h3{Return value}
		@code{
			0, if the string contains an illegal character.
			Otherwise, the resulting integer is returned.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		SilTime ParseDate(const XChar * psz);

	/*------------------------------------------------------------------------------------------
		Convert the time string (prgch, cch) to an SilTime. The result is placed in pstim.
		When non-NULL, pcchRead returns the number of characters consumed from the input.
		This may be less than cch if an illegal character occurs in the string, or if the
		date/time string is part of a larger string.

		@h3{Return value}
		@code{
			true, when cch characters are consumed.
			false, when less than cch characters are consumed.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		bool ParseTime(const XChar * prgch, int cch, SilTime * pstim, int * pcchRead = NULL);

	/*------------------------------------------------------------------------------------------
		Convert the time string psz to an SilTime. (0 is also a valid time).

		@h3{Return value}
		@code{
			0, if the string contains an illegal character.
			Otherwise, the resulting integer is returned.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		SilTime ParseTime(const XChar * psz);

	/*------------------------------------------------------------------------------------------
		Get the day of week string given the day (1-7)
	------------------------------------------------------------------------------------------*/
	StrUni * GetDayOfWeekStr(int dayofweek, bool longformat);

	/*------------------------------------------------------------------------------------------
		Get the month string given the month (1-12)
	------------------------------------------------------------------------------------------*/
	StrUni * GetMonthStr(int month, bool longformat);

	/*------------------------------------------------------------------------------------------
		Convert the date string (pszDate) to an SilTime using the given format (pszFmt).  The
		result is placed in pstim.

		@h3{Return value}
		@code{
			The number of characters consumed in the string, or 0 if an error occurs.
		}
	------------------------------------------------------------------------------------------*/
	template<typename XChar>
		int ParseDateWithFormat(const XChar * pszDate, const XChar * pszFmt, SilTime * pstim);


	void MakeUndoRedoLabels(int stid, StrUni * pstuUndo, StrUni * pstuRedo);

	void InitIcuDataDir();

	bool NormalizeStrUni(StrUni & stu, UNormalizationMode nm);

	/*------------------------------------------------------------------------------------------
		Trim white spaces from input string returning trimmed string in output string.

		@h3{Return value}
		@code{
			void.
		}
	------------------------------------------------------------------------------------------*/
	void TrimWhiteSpace(const char * pszIn, StrAnsi & staOut);
	void TrimWhiteSpace(const wchar * pszIn, StrUni & stuOut);

	/*------------------------------------------------------------------------------------------
		Skip spaces at beginning.

		@h3{Return value}
		@code{
			pointer to trimmed string.
		}
	------------------------------------------------------------------------------------------*/
	const char * SkipLeadingWhiteSpace(const char * psz);
	const wchar * SkipLeadingWhiteSpace(const wchar * psz);

	/*------------------------------------------------------------------------------------------
		Skip spaces at end of string.

		@h3{Return value}
		@code{
			length of trimmed string.
		}
	------------------------------------------------------------------------------------------*/
	unsigned LengthLessTrailingWhiteSpace(const char * psz);
	unsigned LengthLessTrailingWhiteSpace(const wchar * psz);

	int Compare(const OLECHAR * prgchA, int cchA, const OLECHAR * prgchB, int cchB,
		RuleBasedCollator * prbc, int * pcMatched = NULL);

	void StoreUtf16FromUtf8(const char * prgch, int cch, StrUni & stu, bool fAppend = false);

	/*------------------------------------------------------------------------------------------
		Create a std::wstring from a const OLECHAR*.
	------------------------------------------------------------------------------------------*/
	std::wstring wstring(const OLECHAR* prgch, int cch = -1);

	void FixForSqlQuotedString(StrUni & stu);

}; // end namespace StrUtil

#endif //!UTILSTRING_H

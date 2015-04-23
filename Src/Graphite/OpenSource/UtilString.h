/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

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
			utf16 (#ifdef UNICODE) or schar (for all other cases). schar is typedefed as char.

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
#ifdef _MSC_VER
#pragma once
#endif
#ifndef UTILSTRING_H
#define UTILSTRING_H 1
/*:End Ignore*/

#include <cstdlib>
#include <cctype>
#include <cstring>
#include <algorithm>
#include "GrCommon.h"
#include "Throwable.h"
//#include "common.h"
//#include "debug.h"

namespace gr
{


//:Associate with TextFormatter<>.
const int kradMax = 36; // For converting from numeric to text with any allowable radix.
extern const char g_rgchDigits[kradMax];


//:>********************************************************************************************
//:>	General text manipulation functions.
//:>********************************************************************************************

/*-----------------------------------------------------------------------------------*//*:Ignore
	Char Type Based Type Defns. The following template class is so we can define schar and
	utf16 to be opposite each other. XChar in templates below is defined to mean one type;
	YChar is defined to mean the other type. This also applies to other declarations that are
	specific to a character type.
-------------------------------------------------------------------------------*//*:End Ignore*/
template<typename XChar> class CharDefns;

/*----------------------------------------------------------------------------------------------
	The template class CharDefns<utf16> is for wide (16 bit) characters. It is used by
	StrBase<utf16>, StrBaseBufCore<>, and text manipulation functions to define schar and utf16
	to be opposite each other.

	When CharDefns<utf16> is used in template functions, XChar is defined to mean utf16;
	YChar is defined to mean schar. This also applies to other declarations that are specific
	to a character type. CharDefns<utf16> also defines OtherChar to mean schar.
----------------------------------------------------------------------------------------------*/
template<> class CharDefns<utf16>
{
public:
	typedef schar OtherChar;
	typedef void (*PfnWriteChars)(void * pv, const utf16 * prgch, int cch);
};

typedef CharDefns<utf16>::PfnWriteChars PfnWriteCharsW;

/*----------------------------------------------------------------------------------------------
	The template class CharDefns<schar> is for ansi (8 bit) characters. It is used by
	StrBase<schar>, StrBaseBufCore<>, and text manipulation functions to define schar and utf16
	to be opposite each other.

	When CharDefns<schar> is used in template functions, XChar is defined to mean schar;
	YChar is defined to mean utf16. This also applies to other declarations that are specific
	to a character type. CharDefns<schar> also defines OtherChar to mean utf16.
----------------------------------------------------------------------------------------------*/
template<> class CharDefns<schar>
{
public:
	typedef utf16 OtherChar;
	typedef void (*PfnWriteChars)(void * pv, const schar * prgch, int cch);
};

typedef CharDefns<schar>::PfnWriteChars PfnWriteCharsA;

#ifdef GR_FW
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
void ToUpper(utf16 * prgch, int cch);

/*----------------------------------------------------------------------------------------------
	This unicode case mapping function converts the wide (16-bit) character ch to upper case.
----------------------------------------------------------------------------------------------*/
utf16 ToUpper(utf16 ch);

/*----------------------------------------------------------------------------------------------
	This unicode case mapping function converts cch wide (16-bit) characters in prgch to
	lower case.
----------------------------------------------------------------------------------------------*/
void ToLower(utf16 * prgch, int cch);

/*----------------------------------------------------------------------------------------------
	This unicode case mapping function converts the wide (16-bit) character ch to lower case.
----------------------------------------------------------------------------------------------*/
utf16 ToLower(utf16 ch);

#endif


/*----------------------------------------------------------------------------------------------
	Get a pointer and cch (count of characters) for a string with id stid defined in a resource
	header file.

	@h3{Parameters}
		stid -- string id, e.g., kstidComment (defined in a resource header file).
----------------------------------------------------------------------------------------------*/
#ifdef GR_FW
void GetResourceString(const utf16 ** pprgch, int * pcch, int stid);
#endif

/*----------------------------------------------------------------------------------------------
	Return the length (number of characters) of a string. Surrogate pairs and other 'characters'
	that occupy more than the usual space (e.g., in ASCII double-byte encodings) are not
	recognized. StrLen just counts bytes or utf16s.

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
	int cch = (cch1 < cch2) ? cch1 : cch2;

	for ( ; --cch >= 0; prgch1++, prgch2++)
	{
		if (*prgch1 != *prgch2)
			return (int)(unsigned char)*prgch1 - (int)(unsigned char)*prgch2;
	}
	return cch1 - cch2;
}

/*----------------------------------------------------------------------------------------------
	Case sensitive naive binary comparison of strings containing unicode (16 bit) characters.

	@h3{Return value}
	Returns negative, zero, or positive according to whether (prgch1, cch1) is less than,
	equal to, or greater than (prgch2, cch2).
----------------------------------------------------------------------------------------------*/
inline int CompareRgch(const utf16 * prgch1, int cch1, const utf16 * prgch2, int cch2)
{
	int cch = (cch1 < cch2) ? cch1 : cch2;

	for ( ; --cch >= 0; prgch1++, prgch2++)
	{
		if (*prgch1 != *prgch2)
			return (int)*prgch1 - (int)*prgch2;
	}
	return cch1 - cch2;
}

#ifdef GR_FW

/*----------------------------------------------------------------------------------------------
	Case insensitive equality. This function can be used for either ansi (8 bit) or unicode
	(16-bit) characters.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	inline bool EqualsRgchCI(const XChar * prgch1, const XChar * prgch2, int cch)
{
	for ( ; --cch >= 0; prgch1++, prgch2++)
	{
		if (*prgch1 != *prgch2 && /*gr::*/ToLower(*prgch1) != /*gr::*/ToLower(*prgch2))
			return false;
	}

	return true;
}

#endif
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

inline int ConvertText(const utf16 * prgchwSrc, int cchwSrc, schar * prgchsDst, int cchsDst)
{
	AssertArray(prgchwSrc, cchwSrc);
	AssertArray(prgchsDst, cchsDst);

	return Platform_UnicodeToANSI(prgchwSrc, cchwSrc, prgchsDst, cchsDst);
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
inline int ConvertText(const schar * prgchsSrc, int cchsSrc, utf16 * prgchwDst, int cchwDst)
{
	AssertArray(prgchsSrc, cchsSrc);
	AssertArray(prgchwDst, cchwDst);

	return Platform_AnsiToUnicode(prgchsSrc, cchsSrc, prgchwDst, cchwDst);
}

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

	std::copy(prgchSrc, prgchSrc + cchSrc, prgchDst);
	//	CopyItems(prgchSrc, prgchDst, cchSrc);
	return cchSrc;
}


/*----------------------------------------------------------------------------------------------
	Determine the largest number of YChars that can be converted to cxchMax or fewer XChars.
	This does a binary search.
----------------------------------------------------------------------------------------------*/
template<typename YChar>
	inline int CychFitConvertedText(const YChar * prgych, int cych, int cxchMax)
{
	AssertArray(prgych, cych);
	Assert(0 <= cxchMax);

	if (0 >= cxchMax)
		return 0;

	// The most common case is that each ych becomes a single xch, so test for this first.
	if (cych > cxchMax &&
		ConvertText(prgych, cxchMax, (typename CharDefns<YChar>::OtherChar *)NULL, 0) <= cxchMax &&
		ConvertText(prgych, cxchMax + 1, (typename CharDefns<YChar>::OtherChar *)NULL, 0) > cxchMax)
	{
		return cxchMax;
	}

	int cychMin, cychLim;

	for (cychMin = 0, cychLim = cych; cychMin < cychLim; )
	{
		int cychT = (unsigned int)(cychMin + cychLim + 1) / 2;
		Assert(cychMin < cychT && cychT <= cychLim);

		int cxchT = ConvertText(prgych, cychT, (typename CharDefns<YChar>::OtherChar *)NULL, 0);
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
		StrApp, which contains achar characters. achar is typedefed in Common.h as utf16
			for UNICODE and as schar, i.e., char, for all other cases.
	}

	For a comparison of the other smart string class, see ${StrBaseBuf<>}.

	@h3{Hungarian: stb}
----------------------------------------------------------------------------------------------*/
template<typename XChar> class StrBase
{
public:
#ifdef DEBUG__XX
	// Check to make certain we have a valid internal state for debugging purposes.
	bool AssertValid(void) const
	{
		AssertPtr(this);
		AssertObj(m_pbuf);
		return true;
	}
	#define DBWINIT() m_dbw1.m_pstrbase = this; // so DebugWatch can find string
#else
	#define DBWINIT()
#endif //DEBUG

	// The other character type.
	typedef typename CharDefns<XChar>::OtherChar YChar;

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
	template<typename ZChar> StrBase<XChar>(const ZChar * psz)
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
	template<typename ZChar> StrBase<XChar>(const ZChar * prgch, int cch)
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
#ifdef GR_FW
	StrBase<XChar>(const int stid)
	{
		m_pbuf = &s_bufEmpty;
		m_pbuf->AddRef();
		const utf16 *prgchw;
		int cch;

		::GetResourceString(&prgchw, &cch, stid);
		if (cch)
			_Replace(0, 0, prgchw, 0, cch);
		DBWINIT();
	}
#endif
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
		Get the length, i.e., the number of char or utf16 characters (as opposed to a count of
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
		Assert((unsigned int)ich <= (unsigned int)m_pbuf->Cch());
		return m_pbuf->m_rgch[ich];
	}

	/*------------------------------------------------------------------------------------------
		Return the character at index ich. Use for read only. Asserts that ich is in range.
	------------------------------------------------------------------------------------------*/
	XChar operator [] (int ich) const
	{
		AssertObj(this);
		Assert((unsigned int)ich <= (unsigned int)m_pbuf->Cch());
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
	template<typename ZChar>
		void Assign(const ZChar * psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		_Replace(0, m_pbuf->Cch(), psz, 0, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Assign the characters from the given string (prgch, cch), of either character type,
		to be the value of this StrBase<>.
	------------------------------------------------------------------------------------------*/
	template<typename ZChar>
		void Assign(const ZChar * prgch, int cch)
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		_Replace(0, m_pbuf->Cch(), prgch, 0, cch);
	}

	/*------------------------------------------------------------------------------------------
		Assign the value of this StrBase<> to be the same as the string with id stid defined in
		a resource header file.
	------------------------------------------------------------------------------------------*/
	/*void Load(const int stid)
	{
		AssertObj(this);
		const utf16 *prgchw;
		int cch;

		::GetResourceString(&prgchw, &cch, stid);
		if (cch)
			_Replace(0, m_pbuf->Cch(), prgchw, 0, cch);
	}*/

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
	template<typename ZChar>
		StrBase<XChar> & operator = (const ZChar * psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		_Replace(0, m_pbuf->Cch(), psz, 0, StrLen(psz));
		return *this;
	}


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
	template<typename ZChar>
		void Append(const ZChar * psz)
	{
		AssertPszN(psz);
		int cchCur = m_pbuf->Cch();
		_Replace(cchCur, cchCur, psz, 0, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Append a copy of the string (prgch, cch), of either character type, to the value of
		this StrBase<>.
	------------------------------------------------------------------------------------------*/
	template<typename ZChar>
		void Append(const ZChar * prgch, int cch)
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		int cchCur = m_pbuf->Cch();
		_Replace(cchCur, cchCur, prgch, 0, cch);
	}

	/*------------------------------------------------------------------------------------------
		Append a copy of the string with id stid defined in a resource header file to the value
		of this StrBase<>.
	------------------------------------------------------------------------------------------*/
#ifdef GR_FW
	void AppendLoad(const int stid)
	{
		AssertObj(this);
		const utf16 *prgchw;
		int cch;
		int cchCur = m_pbuf->Cch();

		::GetResourceString(&prgchw, &cch, stid);
		if (cch)
			_Replace(cchCur, cchCur, prgchw, 0, cch);
	}
#endif

	//:> += operators.
	/*------------------------------------------------------------------------------------------
		Append a copy of the value of a StrBase<>, which may be of either character type, to
		the value of this StrBase<>.
	------------------------------------------------------------------------------------------*/
	template<typename ZChar>
		StrBase<XChar> & operator += (const StrBase<ZChar> & stb)
	{
		Append(stb);
		return *this;
	}

	/*------------------------------------------------------------------------------------------
		Append a copy of the zero-terminated string psz, of either character type, to the value
		of this StrBase<>.
	------------------------------------------------------------------------------------------*/
	template<typename ZChar>
		StrBase<XChar> & operator += (const ZChar * psz)
	{
		Append(psz);
		return *this;
	}


	//:> + operators.
	/*------------------------------------------------------------------------------------------
		Return a new StrBase<> with the value of this StrBase<> followed by the value of another
		StrBase<>, which may be of either character type.
	------------------------------------------------------------------------------------------*/
	template<typename ZChar>
		StrBase<XChar> operator + (const StrBase<ZChar> & stb) const
	{
		StrBase<XChar> stbRet(this);
		stbRet.Append(stb);
		return stbRet;
	}

	/*------------------------------------------------------------------------------------------
		Return a new StrBase<> with the value of this StrBase<> followed by a copy of the
		zero-terminated string psz, of either character type.
	------------------------------------------------------------------------------------------*/
	template<typename ZChar>
		StrBase<XChar> operator + (const ZChar * psz) const
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
		if (m_pbuf->m_cb != size_t(cch) * sizeof(XChar))
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

#ifdef GR_FW

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
		if (m_pbuf->m_cb != size_t(cch) * sizeof(XChar))
			return false;
		return EqualsRgchCI(m_pbuf->m_rgch, prgch, m_pbuf->Cch());
	}
#endif

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
#ifdef GR_FW
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
#endif
	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with the characters from stb, a
		StrBase<> consisting of the other type of character.
	------------------------------------------------------------------------------------------*/
	void Replace(int ichMin, int ichLim, StrBase<YChar> & stb)
	{
		AssertObj(this);
		Assert((unsigned int)ichMin <= (unsigned int)ichLim && (unsigned int)ichLim <= (unsigned int)m_pbuf->Cch());
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
		Assert((unsigned int)ichMin <= (unsigned int)ichLim && (unsigned int)ichLim <= (unsigned int)m_pbuf->Cch());
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
	template<typename ZChar>
		void Replace(int ichMin, int ichLim, const ZChar * psz)
	{
		AssertObj(this);
		Assert((unsigned int)ichMin <= (unsigned int)ichLim && (unsigned int)ichLim <= (unsigned int)m_pbuf->Cch());
		AssertPszN(psz);
		_Replace(ichMin, ichLim, psz, 0, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with the characters from the
		string (prgch, cch) of either character type.
	------------------------------------------------------------------------------------------*/
	template<typename ZChar>
		void Replace(int ichMin, int ichLim, const ZChar * prgch, int cch)
	{
		AssertObj(this);
		Assert((unsigned int)ichMin <= (unsigned int)ichLim && (unsigned int)ichLim <= (unsigned int)m_pbuf->Cch());
		AssertArray(prgch, cch);
		_Replace(ichMin, ichLim, prgch, 0, cch);
	}

	/*------------------------------------------------------------------------------------------
		Replace the range of characters [ichMin, ichLim) with cchIns instances of the character
		chIns.
	------------------------------------------------------------------------------------------*/
	template<typename ZChar>
		void ReplaceFill(int ichMin, int ichLim, const ZChar chIns, int cchIns)
	{
		AssertObj(this);
		Assert((unsigned int)ichMin <= (unsigned int)ichLim && (unsigned int)ichLim <= (unsigned int)m_pbuf->Cch());
		Assert(cchIns >= 0);
		_Replace(ichMin, ichLim, NULL, chIns, cchIns);
	}


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
#ifdef GR_FW

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

		ch = /*gr::*/ToLower(ch);
		for (ich = ichMin; ich < cch; ich++)
		{
			if (/*gr::*/ToLower(m_pbuf->m_rgch[ich]) == ch)
				return ich;
		}

		return -1;
	}
#endif
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
#ifdef GR_FW

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
		ch = /*gr::*/ToLower(ch);

		int ich;
		for (ich = ichLast; ich >= 0; --ich)
		{
			if (/*gr::*/ToLower(m_pbuf->m_rgch[ich]) == ch)
				return ich;
		}

		return -1;
	}
#endif
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
	int FindStr(const XChar * prgch, int cch, int ichMin = 0) const
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
#ifdef GR_FW

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
	int FindStrCI(const XChar * prgch, int cch, int ichMin = 0) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		Assert(ichMin >= 0);

		if (!cch)
			return ichMin <= Length() ? ichMin : -1;

		// Last position in m_rgch where prgch can be.
		int ichLast = Length() - cch;
		int ich;
		XChar ch = /*gr::*/ToLower(prgch[0]);

		for (ich = ichMin; ich <= ichLast; ich++)
		{
			if (/*gr::*/ToLower(m_pbuf->m_rgch[ich]) == ch &&
				EqualsRgchCI(m_pbuf->m_rgch + ich, prgch, cch))
			{
				return ich;
			}
		}

		return -1;
	}
#endif
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
#ifdef GR_FW

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

		XChar ch = /*gr::*/ToLower(prgch[0]);
		int ich;
		for (ich = ichLast; ich >= 0; --ich)
		{
			if (/*gr::*/ToLower(m_pbuf->m_rgch[ich]) == ch &&
				EqualsRgchCI(m_pbuf->m_rgch + ich, prgch, cch))
			{
				return ich;
			}
		}

		return -1;
	}
#endif
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
		size_t m_cb; // The byte count.
		XChar m_rgch[1]; // The characters.

#ifdef DEBUG
		// Check to make certain we have a valid internal state for debugging purposes.
		bool AssertValid(void)
		{
			AssertPtr(this);
			Assert(0 <= m_crefMinusOne);
			Assert(m_cb >= 0);
			Assert(0 == m_cb % isizeof(XChar));
			AssertArray(m_rgch, m_cb / sizeof(XChar) + 1);
			Assert(0 == m_rgch[m_cb / sizeof(XChar)]);
			return true;
		}
#endif

		// Static method to create a new StrBuffer. This calls malloc to allocate the buffer.
		static StrBuffer * Create(int cch)
		{
			Assert(0 <= cch);

			int cb = sizeof(StrBuffer) + cch * sizeof(XChar);
			Assert(cb >= sizeof(StrBuffer));

			StrBuffer * pbuf = (StrBuffer *)malloc(cb);
			if (!pbuf)
				ThrowHr(WarnHr(E_OUTOFMEMORY));
			pbuf->m_crefMinusOne = 0;
			pbuf->m_cb = cch * sizeof(XChar);
			pbuf->m_rgch[cch] = 0;

			AssertObj(pbuf);
			return pbuf;
		}

		// Add a reference to this StrBuffer, by incrementing m_crefMinusOne.
		void AddRef(void)
		{
			AssertObj(this);
			m_crefMinusOne++;
			//DoAssert(0 < InterlockedIncrement(&m_crefMinusOne));
		}

		// Release a reference to this StrBuffer, by decrementing m_crefMinusOne.
		void Release(void)
		{
			AssertObj(this);
			Assert(0 <= m_crefMinusOne);
			if(0 > --m_crefMinusOne)
				free(this);
		}

		// Return the count of characters.
		int Cch(void)
		{
			Assert(0 == m_cb % sizeof(XChar));
			return (unsigned int)m_cb / sizeof(XChar);
		}
	}; // End of StrBuffer.


	// Protected StrBase<> memory management functions.
	static StrBuffer s_bufEmpty; // The empty buffer.
	StrBuffer * m_pbuf; // Pointer to a StrBuffer which holds the characters for this StrBase<>.

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

	void _Replace(int ichMin, int ichLim, const XChar * prgchIns, XChar chIns, int cchIns);
		//int nCodePage);
	void _Replace(int ichMin, int ichLim, const YChar * prgchIns, YChar chIns, int cchIns);
		//int nCodePage);
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
// Typedef and instantiate StrBase<utf16> for unicode characters. @h3{Hungarian: stu}
typedef StrBase<utf16> StrUni;

// Typedef and instantiate StrBase<schar> for ansi characters. @h3{Hungarian: sta}
typedef StrBase<schar> StrAnsi;

// Typedef and instantiate StrBase<achar> for achar characters. @h3{Hungarian: str}
typedef StrBase<achar> StrApp;

} // namespace gr

#if !defined(GR_NAMESPACE)
using namespace gr;
#endif

#include "UtilString_i.cpp"

#endif //!UTILSTRING_H

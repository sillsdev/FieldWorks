/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: UtilXml.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	code for utility functions used in XML import or export
	this includes such things as converting between UTF-8 and UTF-16
----------------------------------------------------------------------------------------------*/
#include "main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


/***********************************************************************************************
	Functions
***********************************************************************************************/
// This includes a number of enum definitions for useful constants plus a function shared with
// the TESO dll code that doesn't use anything else in Generic.
#include "DecodeUtf8_i.c"

//:End Ignore

//:Associate with "XML and Unicode Utility Functions"
/*----------------------------------------------------------------------------------------------
	Return the number of bytes needed to store the 16-bit Unicode string as XML UTF-8.

	@param rgchwSrc Pointer to an array of wide (Unicode) characters.
	@param cchwSrc Number of wide characters in rgchwSrc.  This may be greater than the number
					of actual Unicode characters due to surrogate pairs.
	@param fXml Flag whether the output needs to have XML character code escapes for "<>&".
----------------------------------------------------------------------------------------------*/
int CountXmlUtf8FromUtf16(const wchar * rgchwSrc, int cchwSrc, bool fXml)
{
	AssertArray(rgchwSrc, cchwSrc);

	int cchChar;
	const wchar * pchw;
	const wchar * pchwLim = rgchwSrc + cchwSrc;

	int cchDst = 0;

	for (pchw = rgchwSrc; pchw < pchwLim; )
	{
		ulong luChar = *pchw++;
		if (kSurrogateHighFirst <= luChar && luChar <= kSurrogateHighLast && pchw < pchwLim)
		{
			ulong luChar2 = *pchw;
			if (kSurrogateLowFirst <= luChar2 && luChar2 <= kSurrogateLowLast)
			{
				luChar -= kSurrogateHighFirst;
				luChar <<= kSurrogateShift;
				luChar += luChar2 - kSurrogateLowFirst;
				luChar += kSurrogateBase;
				++pchw;
			}
		}
		if (luChar > kUnicodeMax)
		{
			luChar = kReplacementChar;
		}

		if (luChar < kUtf8Min2)
		{
			cchChar = 1;
			if (fXml)
			{
				switch (luChar)
				{
				case '<':
					cchChar = 4;
					break;
				case '>':
					cchChar = 4;
					break;
				case '&':
					cchChar = 5;
					break;
				case '"':
					cchChar = 6;
					break;
				}
			}
		}
		else if (luChar < kUtf8Min3)
		{
			cchChar = 2;
		}
		else if (luChar < kUtf8Min4)
		{
			cchChar = 3;
		}
		else if (luChar < kUtf8Min5)
		{
			cchChar = 4;
		}
		else if (luChar < kUtf8Min6)
		{
			cchChar = 5;
		}
		else
		{
			cchChar = 6;
		}

		cchDst += cchChar;
	}

	return cchDst;
}


/*----------------------------------------------------------------------------------------------
	Return the number of bytes needed to store the 16-bit Unicode string as plain UTF-8;
	don't do the special stuff needed for XML.

	@param rgchwSrc Pointer to an array of wide (Unicode) characters.
	@param cchwSrc Number of wide characters in rgchwSrc.  This may be greater than the number
					of actual Unicode characters due to surrogate pairs.
----------------------------------------------------------------------------------------------*/
int CountUtf8FromUtf16(const wchar * rgchwSrc, int cchwSrc)
{
	return CountXmlUtf8FromUtf16(rgchwSrc, cchwSrc, false);
}

/*----------------------------------------------------------------------------------------------
	Convert the 16-bit Unicode string to UTF-8, returning the length in bytes of the UTF-8
	string.  Care is taken not to overflow the output buffer.

	@param rgchDst Pointer to an output array of (8-bit) characters.
	@param cchMaxDst Maximum number of (8-bit) characters that can be stored in rgchDst.
	@param rgchwSrc Pointer to an input array of wide (Unicode) characters.
	@param cchwSrc Number of wide characters in rgchwSrc.  This may be greater than the number
					of actual Unicode characters due to surrogate pairs.
	@param fXml Flag whether the output needs to have XML character code escapes for "<>&"".

	@return Number of characters required for the output buffer. If cchMaxDst is less than or
					equal to the return value, all of the output was written.
----------------------------------------------------------------------------------------------*/
int ConvertUtf16ToXmlUtf8(char * rgchDst, int cchMaxDst, const wchar * rgchwSrc, int cchwSrc,
	bool fXml)
{
	AssertArray(rgchDst, cchMaxDst);
	AssertArray(rgchwSrc, cchwSrc);

	int cchChar;
	char rgchChar[8];
	const char * prgchChar;
	const wchar * pchw;
	const wchar * pchwLim = rgchwSrc + cchwSrc;

	// TODO SteveMc(ShonK): This needs a better name. When you decide on hungarian tags for
	// the constants above use the same tag here.
	byte bFirstMark = 0;

	int cchDst = 0;

	for (pchw = rgchwSrc; pchw < pchwLim; )
	{
		ulong luChar = *pchw++;
		if (kSurrogateHighFirst <= luChar && luChar <= kSurrogateHighLast && pchw < pchwLim)
		{
			ulong luChar2 = *pchw;
			if (kSurrogateLowFirst <= luChar2 && luChar2 <= kSurrogateLowLast)
			{
				luChar -= kSurrogateHighFirst;
				luChar <<= kSurrogateShift;
				luChar += luChar2 - kSurrogateLowFirst;
				luChar += kSurrogateBase;
				++pchw;
			}
		}
		if (luChar > kUnicodeMax)
		{
			luChar = kReplacementChar;
		}

		prgchChar = NULL;

		if (luChar < kUtf8Min2)
		{
			if (fXml)
			{
				switch (luChar)
				{
				case '<':
					prgchChar = "&lt;";
					cchChar = 4;
					break;
				case '>':
					prgchChar = "&gt;";
					cchChar = 4;
					break;
				case '&':
					prgchChar = "&amp;";
					cchChar = 5;
					break;
				case '"':
					prgchChar = "&quot;";
					cchChar = 6;
					break;
				default:
					bFirstMark = kUtf8Flag1;
					cchChar = 1;
					break;
				}
			}
			else
			{
				bFirstMark = kUtf8Flag1;
				cchChar = 1;
			}
		}
		else if (luChar < kUtf8Min3)
		{
			bFirstMark = kUtf8Flag2;
			cchChar = 2;
		}
		else if (luChar < kUtf8Min4)
		{
			bFirstMark = kUtf8Flag3;
			cchChar = 3;
		}
		else if (luChar < kUtf8Min5)
		{
			bFirstMark = kUtf8Flag4;
			cchChar = 4;
		}
		else if (luChar < kUtf8Min6)
		{
			bFirstMark = kUtf8Flag5;
			cchChar = 5;
		}
		else
		{
			bFirstMark = kUtf8Flag6;
			cchChar = 6;
		}

		if (!prgchChar)
		{
			prgchChar = rgchChar;

			char * pch = &rgchChar[cchChar];

			switch (cchChar)
			{
			case 6:
				*--pch = (char)((luChar & kByteMask) | kByteMark);
				luChar >>= kByteShift;
				// fall through
			case 5:
				*--pch = (char)((luChar & kByteMask) | kByteMark);
				luChar >>= kByteShift;
				// fall through
			case 4:
				*--pch = (char)((luChar & kByteMask) | kByteMark);
				luChar >>= kByteShift;
				// fall through
			case 3:
				*--pch = (char)((luChar & kByteMask) | kByteMark);
				luChar >>= kByteShift;
				// fall through
			case 2:
				*--pch = (char)((luChar & kByteMask) | kByteMark);
				luChar >>= kByteShift;
				// fall through
			case 1:
				*--pch = (char)(luChar | bFirstMark);
				break;
			default:
				Assert(false);		// can't happen!!
			}
		}

		if (cchDst < cchMaxDst)
		{
			CopyItems(prgchChar, rgchDst + cchDst, Min(cchChar, cchMaxDst - cchDst));
		}
		else
		{
			// REVIEW: should we somehow signal an error on overflowing the output buffer?
			ThrowHr(E_OUTOFMEMORY, StrUni(L"BUFFER OVERFLOW in ConvertUtf16ToXmlUtf8()").Chars());
		}
		cchDst += cchChar;
	}

	return cchDst;
}

/*----------------------------------------------------------------------------------------------
	Convert the 16-bit Unicode string to plain UTF-8; don't do the special stuff needed for
	XML.

	@param rgchDst Pointer to an output array of (8-bit) characters.
	@param cchMaxDst Maximum number of (8-bit) characters that can be stored in rgchDst.
	@param rgchwSrc Pointer to an input array of wide (Unicode) characters.
	@param cchwSrc Number of wide characters in rgchwSrc.  This may be greater than the number
					of actual Unicode characters due to surrogate pairs.

	@return Number of characters required for the output buffer. If cchMaxDst is less than or
					equal to the return value, all of the output was written.
----------------------------------------------------------------------------------------------*/
int ConvertUtf16ToUtf8(char * rgchDst, int cchMaxDst, const wchar * rgchwSrc, int cchwSrc)
{
	return ConvertUtf16ToXmlUtf8(rgchDst, cchMaxDst, rgchwSrc, cchwSrc, false);
}

/*----------------------------------------------------------------------------------------------
	Convert a character (char) string in UTF-8 to a wide character (wchar) string encoded in
	UTF-16.  Both the input and output strings are NUL terminated.

	@param pszwDst Pointer to an output array of wide (16-bit Unicode) characters.
	@param cchwDst Maximum number of (16-bit) characters that can be stored in pszDst,
					including the terminating NUL.
	@param pszSrc Pointer to the input UTF-8 character string.

	@return Number of 16-bit characters embedded in the UTF-8 string.  This may be greater than
					the actual number of Unicode characters, due to surrogate pairs.
----------------------------------------------------------------------------------------------*/
int SetUtf16FromUtf8(wchar * pszwDst, int cchwDst, const char * pszSrc)
{
	AssertArray(pszwDst, cchwDst);
	if ((NULL == pszSrc) || (*pszSrc == '\0') || (cchwDst == 0))
		return 0;

	int cbUtf8;
	const char * p;
	int cchw = 0;
	int cchSrc = strlen(pszSrc);
	for (p = pszSrc; *p; p += cbUtf8, cchSrc -= cbUtf8)
	{
		long lnUnicode = DecodeUtf8(p, cchSrc, cbUtf8);
		if (lnUnicode == -1)
		{
			return 0;
		}
		else if (lnUnicode > kUtf16Max)
		{
			// valid UCS-4, but invalid UTF-16
			lnUnicode = kReplacementChar;
		}
		else if (lnUnicode > kUcs2Max)
		{
			// invalid UCS-2, but valid UTF-16:  convert to surrogate pairs
			lnUnicode -= kSurrogateBase;
			if (cchw < cchwDst)
				pszwDst[cchw++] = (wchar)((lnUnicode >> kSurrogateShift) +
											   kSurrogateHighFirst);
			lnUnicode = (lnUnicode & kSurrogateMask) + kSurrogateLowFirst;
		}
		if (cchw < cchwDst)
			pszwDst[cchw++] = (wchar)lnUnicode;
	}
	pszwDst[cchwDst-1] = '\0';
	return cchw;
}

/*----------------------------------------------------------------------------------------------
	Convert a range of UTF-8 characters to a range of UTF-16 characters.  The output buffer is
	not NUL terminated.

	@param rgchwDst Pointer to an output array of wide (16-bit) characters.
	@param cchwDst Maximum number of wide (16-bit) characters that can be stored in rgchwDst.
					Due to surrogate pairs, this may be greater than the number of actual
					Unicode characters that can be stored.
	@param rgchSrc Pointer to an input array of (8-bit) characters.
	@param cchSrc Number of characters in rgchSrc.

	@return the number of 16-bit characters embedded in the UTF-8 string.  Due to surrogate
					pairs, this may be greater than the number of actual Unicode characters.
----------------------------------------------------------------------------------------------*/
int SetUtf16FromUtf8(wchar * rgchwDst, int cchwDst, const char * rgchSrc, int cchSrc)
{
	AssertArray(rgchwDst, cchwDst);
	if (!cchwDst || !rgchSrc || !cchSrc)
		return 0;

	int cbUtf8;
	int cchw = 0;
	for (int ich = 0; ich < cchSrc; ich += cbUtf8)
	{
		long lnUnicode = DecodeUtf8(rgchSrc + ich, cchSrc - ich, cbUtf8);
		if (lnUnicode == -1)
		{
			return 0;
		}
		else if (lnUnicode > kUtf16Max)
		{
			// Valid UCS-4, but invalid UTF-16.
			lnUnicode = kReplacementChar;
		}
		else if (lnUnicode > kUcs2Max)
		{
			// Invalid UCS-2, but valid UTF-16:  convert to surrogate pairs.
			lnUnicode -= kSurrogateBase;
			if (cchw < cchwDst)
				rgchwDst[cchw++] = (wchar)((lnUnicode >> kSurrogateShift) +
											   kSurrogateHighFirst);
			lnUnicode = (lnUnicode & kSurrogateMask) + kSurrogateLowFirst;
		}
		if (cchw < cchwDst)
			rgchwDst[cchw++] = (wchar)lnUnicode;
	}
	return cchw;
}

/*----------------------------------------------------------------------------------------------
	Return the number of 16-bit characters encoded in the UTF-8 string.  Surrogate pairs count
	as two characters.

	@param pszUtf8 Pointer to a NUL-terminated character string containing UTF-8 data.
----------------------------------------------------------------------------------------------*/
int CountUtf16FromUtf8(const char * pszUtf8)
{
	if ((NULL == pszUtf8) || (*pszUtf8 == '\0'))
		return 0;

	int cbUtf8 = 0;
	const char*	p;
	int cchw = 0;
	int cchSrc = strlen(pszUtf8);
	for (p = pszUtf8; *p; p += cbUtf8, cchSrc -= cbUtf8)
	{
		long lnUnicode = DecodeUtf8(p, cchSrc, cbUtf8);
		if (lnUnicode == -1)
		{
			return 0;
		}
		else if (kUcs2Max < lnUnicode && lnUnicode <= kUtf16Max)
		{
			// invalid UCS-2, but valid UTF-16:  convert to surrogate pairs
			cchw++;
		}
		cchw++;
	}
	return cchw;
}

/*----------------------------------------------------------------------------------------------
	Return the number of 16-bit characters encoded in the range of UTF-8 characters.  Surrogate
	pairs count as two characters.

	@param rgchUtf8 Pointer to an array containing UTF-8 data.
	@param cch Number of (8-bit) characters in rgchUtf8.
----------------------------------------------------------------------------------------------*/
int CountUtf16FromUtf8(const char * rgchUtf8, int cch)
{
	if (!rgchUtf8 || !cch)
		return 0;
	int cbUtf8 = 0;
	int cchw = 0;
	for (int ich = 0; ich < cch; ich += cbUtf8)
	{
		long lnUnicode = DecodeUtf8(rgchUtf8 + ich, cch - ich, cbUtf8);
		if (lnUnicode == -1)
		{
			return 0;
		}
		else if (kUcs2Max < lnUnicode && lnUnicode <= kUtf16Max)
		{
			// invalid UCS-2, but valid UTF-16:  convert to surrogate pairs
			cchw++;
		}
		cchw++;
	}
	return cchw;
}

/*----------------------------------------------------------------------------------------------
	Check whether the given number of characters are all ASCII whitespace.

	@param rgch Pointer to an array of 8-bit characters.
	@param cch Number of characters in rgch.

	@return true if all ASCII whitespace, otherwise false.
----------------------------------------------------------------------------------------------*/
bool IsAllSpaces(const char * rgch, int cch)
{
	for (int ich = 0; ich < cch; ++ich)
	{
		if (!isascii(rgch[ich]))
			return false;
		if (!isspace(rgch[ich]))
			return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Check whether the given number of characters are all whitespace.

	@param rgch Pointer to an array of Unicode characters.
	@param cch Number of characters in rgch.

	@return true if all whitespace, otherwise false.
----------------------------------------------------------------------------------------------*/
bool IsAllWhiteSpace(const OLECHAR * str, int cch)
{
	for (int ich = 0; ich < cch; ++ich)
	{
		if (!iswspace(str[ich]))
			return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Converts to a surrogate character.  If the character is a surrogate, split it into
	*pchOut1 (High) and *pchOut2 (Low) and return true.  Otherwise, transfer the value
	to uchOut1 and return false.
	@param ch32In  [In] UTF32 character.
	@param pchOut1 [Out] pointer to UTF16 High surrogate.
	@param pchOut2 [Out] pointer to UTF16 Low surrogate.
	@return true if character is a surrogate, false if not.
----------------------------------------------------------------------------------------------*/
bool ToSurrogate(uint ch32In, wchar * pchOut1, wchar * pchOut2)
{
	// If ch32In is actually a surrogate character...
	if ((ch32In >= 0x10000) && (ch32In <= 0x10FFFF ))
	{
//		uchOut1 = (UChar)((uch32In - 0x10000) / 1024 + 0xD800);
//		uchOut2 = (UChar)((uch32In - 0x10000) % 1024 + 0xDC00);
		*pchOut1 = (wchar)((ch32In >> 10) + 0xD7C0);
		*pchOut2 = (wchar)((ch32In & 0x3FF) + 0xDC00);
		return true;
	}
	else
	{
		*pchOut1 = wchar(ch32In);
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Converts chIn1 (High) and chIn2 (Low) into a composite surrogate value *pch32Out.
	Returns false if the two characters were an invalid surrogate pair.
	@param chIn1    [In]  UTF16 High surrogate.
	@param chIn2    [In]  UTF16 Low surrogate.
	@param pch32Out [Out] pointer to UTF32 character.
	@return true if the input is a surrogate pair, false if not.
----------------------------------------------------------------------------------------------*/
bool FromSurrogate(wchar chIn1, wchar chIn2, uint * pch32Out)
{
	if ((chIn1 < 0xD800) || (chIn1 > 0xDBFF) || (chIn2 < 0xDC00) || (chIn2 > 0xDFFF))
	{
		return false;
	}
	else
	{
		*pch32Out = ((chIn1 - 0xD800) << 10) + chIn2 + 0x2400;
		return true;
	}
}

/*----------------------------------------------------------------------------------------------
	Convert a numeric character entity in the string to the corresponding single character.
	This must handle surrogate pairs as well as normal UTF-16 codes.
----------------------------------------------------------------------------------------------*/
static bool DecodeNumericEntity(StrUni & stu, int ich, wchar ch2)
{
	bool fHex = false;
	int ichMin = ich + 2;
	if (ch2 == L'x')
	{
		++ichMin;
		fHex = true;
	}
	int ichLim = stu.FindCh(L';', ichMin);
	if (ichLim > ichMin)
	{
		bool fOk;
		int n;
		int cch;
		if (fHex)
			fOk = StrUtil::ParseHexInt(stu.Chars() + ichMin, ichLim - ichMin, &n, &cch);
		else
			fOk = StrUtil::ParseInt(stu.Chars() + ichMin, ichLim - ichMin, &n, &cch);
		if (fOk && cch == ichLim - ichMin)
		{
			wchar rgch[2];
			if (ToSurrogate(n, &rgch[0], &rgch[1]))
				stu.Replace(ich, ichLim + 1, rgch, 2);
			else
				stu.Replace(ich, ichLim + 1, rgch, 1);
			return true;
		}
		else
		{
			return false;
		}
	}
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Convert any of the standard character entities in the string to the corresponding single
	characters.  Returns true if there were no character entities detected, or if all entities
	were valid and properly decoded.
----------------------------------------------------------------------------------------------*/
bool DecodeCharacterEntities(StrUni & stu)
{
	int ich = -1;
	while ((ich = stu.FindCh(L'&', ich + 1)) >= 0)
	{
		if (ich + 3 < stu.Length())
		{
			wchar ch1 = stu.GetAt(ich + 1);
			wchar ch2 = stu.GetAt(ich + 2);
			wchar ch3 = stu.GetAt(ich + 3);
			if (ch1 == L'l' && ch2 == L't' && ch3 == L';')
				stu.Replace(ich, ich + 4, L"<");	// &lt;
			else if (ch1 == L'g' && ch2 == L't' && ch3 == L';')
				stu.Replace(ich, ich + 4, L">");	// &gt;
			else if (ch1 == L'a' && ch2 == L'p' && ch3 == L'o')
			{
				if (ich + 5 < stu.Length() &&
					stu.GetAt(ich + 4) == L's' && stu.GetAt(ich + 5) == L';')
				{
					stu.Replace(ich, ich + 6, L"'");
				}
				else
				{
					return false;
				}
			}
			else if (ch1 == L'q' && ch2 == L'u' && ch3 == L'o')
			{
				if (ich + 5 < stu.Length() &&
					stu.GetAt(ich + 4) == L't' && stu.GetAt(ich + 5) == L';')
				{
					stu.Replace(ich, ich + 6, L"\"");
				}
				else
				{
					return false;
				}
			}
			else if (ch1 == L'a' && ch2 == L'm' && ch3 == L'p')
			{
				if (ich + 4 < stu.Length() && stu.GetAt(ich + 4) == L';')
				{
					stu.Replace(ich, ich + 5, L"&");
				}
				else
				{
					return false;
				}
			}
			else if (ch1 == L'#')
			{
				if (!DecodeNumericEntity(stu, ich, ch2))
					return false;
			}
		}
		else
		{
			return false;
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Iterates forwards to the next code point; moves past the whole of a surrogate pair.
	Leavess ich unchanged and returns false if it is already at or past the end of the array.
	Assumes that a low surrogate was preceded by a high surrogate.
	@param ich    [In/Out]	index of current/new position within (or one past the end of) rgch.
	@param prgch  [In]		pointer to UTF16 character array.
	@param ichLim [In]		index of last+1 character in array.
	@return true if the iteration was possible, else false.
----------------------------------------------------------------------------------------------*/
bool NextCodePoint(int & ich, const wchar * prgch, const int & ichLim)
{
	AssertPtrSize(prgch, ichLim);
	Assert(ichLim >= ich);
	if (ich >= ichLim)
		return false; // We were already at or past the array limit.
	++ich;
	if (ich == ichLim)
		return true;	// We are now at array limit.
	if (IsLowSurrogate(prgch[ich]))
		++ich;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Iterates backwards to the previous code point; moves past the whole of a surrogate pair.
	Leaves ich unchanged and returns false if it is already at or before beginning of array.
	@param ich    [In/Out]	index of current/new position within rgch.
	@param prgch  [In]		pointer to UTF16 character array.
	@return true if the iteration was possible, else false.
----------------------------------------------------------------------------------------------*/
bool PreviousCodePoint(int & ich, const wchar * prgch)
{
	AssertPtrSize(prgch, ich);
	if (ich <= 0)
		return false;
	--ich;
	if (IsLowSurrogate(prgch[ich]))
	{
		if (!ich)
			return false; // Low surrogate was first character of array.
		--ich;
	}
	return true;
}


//:>********************************************************************************************
//:>	XmlStringStream methods.
//:>********************************************************************************************

static DummyFactory g_fact(_T("SIL.AppCore.XmlStringStream"));

/*----------------------------------------------------------------------------------------------
	Create a new XmlStringStream object, returning it through ppxss.

	@param ppxss Address of a pointer to an XmlStringStream object.
----------------------------------------------------------------------------------------------*/
void XmlStringStream::Create(XmlStringStream ** ppxss)
{
	AssertPtr(ppxss);
	Assert(!*ppxss);

	*ppxss = NewObj XmlStringStream();
}


/*----------------------------------------------------------------------------------------------
	Standard COM QueryInterface method.

	@param iid Reference to a COM Interface GUID.
	@param ppv Address of a pointer to receive the desired COM interface pointer, or NULL.

	@return S_OK or E_NOINTERFACE
----------------------------------------------------------------------------------------------*/
STDMETHODIMP XmlStringStream::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_IStream)
		*ppv = static_cast<IStream *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IStream);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Standard COM AddRef method.

	@return The reference count after incrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) XmlStringStream::AddRef()
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Standard COM Release method.

	@return The reference count after decrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) XmlStringStream::Release()
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Convert m_sta from binhex to bytes and place them in the buffer at pv, setting pcbRead
	to the number of bytes placed in the buffer (if pcbRead is non-NULL).
	End of stream is indicated when pcbRead is < cb.

	This implements a standard ISequentialStream method.

	@param pv Pointer to buffer into which the stream is read.
	@param cb Number of bytes to read.
	@param pcbRead Pointer to integer that contains the actual number of bytes read.

	@return S_OK, STG_E_INVALIDPOINTER, or E_FAIL
----------------------------------------------------------------------------------------------*/
STDMETHODIMP XmlStringStream::Read(void * pv, UCOMINT32 cb, UCOMINT32 * pcbRead)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg((byte *)pv, cb);
	ChkComArgPtrN(pcbRead);

	Assert(!(m_sta.Length() & 1)); // Count of binhex characters must be even.
	byte * pb = (byte *)pv;
	const char * pch = m_sta.Chars();
	char ch;
	int n;
	int nT;

	// Convert binhex to bytes while transferring them to output buffer.
	while (*pch && cb)
	{
		// Verify that the characters are really Hex digits.
		// If they're not, return an error.
		ch = ::ToUpper(*pch++) & 0xFF;
		if (!isascii(ch) || !isxdigit(ch))
			return E_INVALIDARG;
		n = ch - '0';
		if (n > 9)
			n += '0' + 10 - 'A';
		nT = (::ToUpper(*pch++) & 0xFF) - '0';
		if (nT > 9)
			nT += '0' + 10 - 'A';
		*pb++ = (byte)(n << 4 | nT);
		--cb;
	}
	cb = pb - (byte *)pv;

	// Nuke the part of the string we wrote out.
	m_sta.Replace(0, cb << 1, (const char *)NULL);
	if (pcbRead)
		*pcbRead = cb;

	return S_OK;
	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Convert the input buffer (cb bytes at pv) to binhex and store it in m_sta.
	If pcbWritten is non-NULL, set it to the number of bytes written.

	This implements a standard ISequentialStream method.

	@param pv Pointer to the buffer address from which the stream is written.
	@param cb Number of bytes to write.
	@param pcbWritten Pointer to integer that contains the actual number of bytes written.

	@return S_OK, STG_E_INVALIDPOINTER, or E_FAIL
----------------------------------------------------------------------------------------------*/
STDMETHODIMP XmlStringStream::Write(const void * pv, UCOMINT32 cb, UCOMINT32 * pcbWritten)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg((byte *)pv, cb);
	ChkComArgPtrN(pcbWritten);
	Assert((int)cb >= 0);

	int cchCur = m_sta.Length();
	Assert(!(cchCur & 1)); // Count of binhex characters must be even.

	if (!cb)
	{
		if (pcbWritten)
			*pcbWritten = 0;
		return S_OK;
	}

	// Convert the input to binhex.
	char * pch;
	int ib;
	byte * rgb = (byte *)pv;

	m_sta.SetSize(cchCur + 2 * cb, &pch);
	pch += cchCur;

	for (ib = 0; ib < (int)cb; ++ib)
	{
		*pch++ = g_rgchDigits[rgb[ib] >> 4];
		*pch++ = g_rgchDigits[rgb[ib] & 0xF];
	}
	if (pcbWritten)
		*pcbWritten = cb;

	END_COM_METHOD(g_fact, IID_IStream);
}

//:Ignore
/***********************************************************************************************
	We are not using the remaining methods for Fieldworks XML work.
***********************************************************************************************/

STDMETHODIMP XmlStringStream::Seek(LARGE_INTEGER dlibMove, DWORD dwOrigin,
	ULARGE_INTEGER * plibNewPosition)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_fact, IID_IStream);
}

STDMETHODIMP XmlStringStream::SetSize(ULARGE_INTEGER libNewSize)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_fact, IID_IStream);
}

STDMETHODIMP XmlStringStream::CopyTo(IStream * pstm, ULARGE_INTEGER cb,
	ULARGE_INTEGER * pcbRead, ULARGE_INTEGER * pcbWritten)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_fact, IID_IStream);
}

STDMETHODIMP XmlStringStream::Commit(DWORD grfCommitFlags)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_fact, IID_IStream);
}

STDMETHODIMP XmlStringStream::Revert()
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_fact, IID_IStream);
}

STDMETHODIMP XmlStringStream::LockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb,
	DWORD dwLockType)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_fact, IID_IStream);
}

STDMETHODIMP XmlStringStream::UnlockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb,
	DWORD dwLockType)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_fact, IID_IStream);
}

STDMETHODIMP XmlStringStream::Stat(STATSTG * pstatstg, DWORD grfStatFlag)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_fact, IID_IStream);
}

STDMETHODIMP XmlStringStream::Clone(IStream ** ppstm)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_fact, IID_IStream);
}
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Write the given UTF-16 Unicode string to an XML file as UTF-8.

	@param pstrm IStream interface pointer.
	@param rgchTxt Array of Unicode character data from a database entry.
	@param cchTxt Number of 16-bit characters in rgchTxt.
----------------------------------------------------------------------------------------------*/
void WriteXmlUnicode(IStream * pstrm, const OLECHAR * rgchTxt, int cchTxt)
{
	AssertPtr(pstrm);
	// Passing in a NULL BSTR with a count of 0 is okay.
	if (rgchTxt != NULL || cchTxt != 0)
	{
		AssertPtr(rgchTxt);
		AssertArray(rgchTxt, cchTxt);
	}
	if (!cchTxt || !rgchTxt)
		return;

	UErrorCode uerr = U_ZERO_ERROR;

#define UTF16BUFSIZE 4096

	// Use an ICU function to normalize the Unicode string.
	const Normalizer2* norm = SilUtil::GetIcuNormalizer(UNORM_NFC);
	UnicodeString input(rgchTxt, cchTxt);
	UnicodeString output = norm->normalize(input, uerr);
	Assert(U_SUCCESS(uerr));

#if 0	/* cannot use this block of code due to XML encodings of <>& */
	// Use an ICU function to convert from UTF-16 to UTF-8.
	char rgchBuffer[UTF16BUFSIZE * 2];
	char * prgchUtf8 = rgchBuffer;
	int32_t cchBuf = UTF16BUFSIZE * 2;
	int32_t cchUtf8 = 0;
	u_strToUTF8(prgchUtf8, cchBuf, &cchUtf8, prgchNorm, cchNorm, &uerr);
	Vector<char> vchs;
	if (uerr == U_BUFFER_OVERFLOW_ERROR)
	{
		vchs.Resize(cchUtf8 + 1);
		prgchUtf8 = vchs.Begin();
		cchBuf = vchs.Size();
		u_strToUTF8(prgchUtf8, cchBuf, &cchUtf8, prgchNorm, cchNorm, &uerr);
	}
	Assert(U_SUCCESS(uerr));
#endif

	// Convert the normalized UTF-16 characters to XML style UTF-8 characters.
	char rgchsBuffer[UTF16BUFSIZE * 2];
	ulong cchsBuffer = isizeof(rgchsBuffer);
	char * prgchUtf8 = rgchsBuffer;
	ulong cchUtf8 = CountXmlUtf8FromUtf16(output.getBuffer(), output.length());
	Vector<char> vchs;
	if (cchUtf8 >= cchsBuffer)
	{
		vchs.Resize(cchUtf8 + 1);
		prgchUtf8 = vchs.Begin();
		cchsBuffer = cchUtf8 + 1;
	}
	ConvertUtf16ToXmlUtf8(prgchUtf8, cchsBuffer, output.getBuffer(), output.length());

	if (!prgchUtf8[cchUtf8 - 1])
		cchUtf8--; // If there is a null at the end of the string, remove it

	// Write the UTF-8 data to the string.
	WriteBuf(pstrm, prgchUtf8, cchUtf8);
}

#include "Vector_i.cpp"
template class Vector<OLECHAR>;
template class Vector<char>;

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkall.bat"
// End: (These 4 lines are useful to Steve McConnel.)

/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilString.cpp
Responsibility: LarryW
Last reviewed: 27Sep99

	Code for string utilities.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE
/*:End Ignore*/
#include <vector>

//:Associate with TextFormatter<>.
// const char g_rgchDigits[] = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
const char g_rgchDigits[36] = {'0','1','2','3','4','5','6','7','8','9','A','B','C','D','E',
	'F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'};

static const wchar szSpace[5] = L" \t\r\n"; // whitespace characters
static const StrUniBuf stbSpace(" \t\r\n");


//:Associate with StrUtil.
static void LoadDaysAbbr();
static void LoadDays();
static void LoadMonthsAbbr();
static void LoadMonths();

//:Associate with "Generic Text Formatting Functions".
/*----------------------------------------------------------------------------------------------
	Instantiate FormatText for XChar = wchar.
----------------------------------------------------------------------------------------------*/
template
	void FormatText<wchar, PfnWriteCharsW>(PfnWriteCharsW pfnWrite,
		void * pv, const wchar * prgchFmt, int cchFmt, va_list vaArgList);

/*----------------------------------------------------------------------------------------------
	Instantiate FormatText for XChar = schar.
----------------------------------------------------------------------------------------------*/
template
	void FormatText<schar, PfnWriteCharsA>(PfnWriteCharsA pfnWrite,
		void * pv, const schar * prgchFmt, int cchFmt, va_list vaArgList);


/*----------------------------------------------------------------------------------------------
	Helper class for FormatText.

	@h3{Hungarian: tfmt}
----------------------------------------------------------------------------------------------*/
template<typename XChar, typename Pfn>
	class TextFormatter
{
public:
	/*------------------------------------------------------------------------------------------
		This initializes the TextFormatter and calls FormatCore inside a try/catch block.
		FormatCore and methods it calls throw an hresult when something goes wrong.
		FormatText calls this method.
	------------------------------------------------------------------------------------------*/
	void Format(Pfn pfnWrite, void * pv, const XChar * prgchFmt, int cchFmt,
		va_list vaArgList);

protected:

	void FormatDate(XChar rgchTerm[], int& cch, int nGenDate, bool fLongDate);

	// The other character type.
	typedef typename CharDefns<XChar>::OtherChar YChar;

	/*------------------------------------------------------------------------------------------
		This is the data write order used by ${TextFormatter#FormatCore}. These indicate the
		order in which to write the padding (1), the sign (2) and the text (3). The order is
		read right to left.
	------------------------------------------------------------------------------------------*/
	enum
	{
		kdwoPadFirst =  0x0321,
		kdwoPadSecond = 0x0312,
		kdwoPadLast =   0x0132,
		kdwoTextOnly =  0x0003,
	};

	// Buffer size.
	enum { kcchBuf = 1024 };

	Pfn m_pfnWrite; // Pointer to the function used by FormatText to write text to a stream or
					// some type of string object.
	void * m_pvParam; // Pointer to a stream or some type of string object. It is supplied by
					// the caller of FormatText and passed on as the first argument whenever
					// pfnWrite is called.
	const XChar * m_pchIn; // Pointer to characters in the input string, the format template.
	const XChar * m_pchInLim; // Pointer to one past the last character in the input string.
	XChar m_chCur; // Current character from the input string.

	XChar * m_pchOut; // Pointer to a character in the output string, m_rgch.
	XChar * m_pchOutLim; // Pointer to one past the last character in m_rgch.
	XChar m_rgch[kcchBuf]; // The output string constructed by processing the format template.
	StrBase<XChar> m_stbT; // Temporary string used as the format template is processed.

	// Throw an error with the given HRESULT.
	void Error(HRESULT hr = E_UNEXPECTED)
	{
		ThrowHr(WarnHr(hr));
	}

	// If the expression fT is false, throw an error.
	void Ensure(bool fT)
	{
		if (!fT)
			Error();
	}

	/*------------------------------------------------------------------------------------------
		See if there is additional input. If not, return false. If so, set m_chCur to the
		new character, increment m_pchIn, and return true.
	------------------------------------------------------------------------------------------*/
	bool FFetchChar(void)
	{
		if (m_pchIn >= m_pchInLim)
			return false;
		m_chCur = *m_pchIn++;
		return true;
	}

	// Fetch a new character. If there are no more characters, throw an error.
	void FetchChar(void)
	{
		Ensure(m_pchIn < m_pchInLim);
		m_chCur = *m_pchIn++;
	}

	/*------------------------------------------------------------------------------------------
		Read the input and parse a non-negative integer. If the integer is greater than nMax,
		throw an error.
	------------------------------------------------------------------------------------------*/
	int FetchInt(int nMax)
	{
		int nRet;

		if (m_chCur < '0' || m_chCur > '9')
			Error();

		for (nRet = 0; m_chCur >= '0' && m_chCur <= '9'; )
		{
			nRet = nRet * 10 + m_chCur - '0';
			if (nRet >= nMax)
				Error();
			FetchChar();
		}
		return nRet;
	}

	// Send whatever is in the buffer to the write function. If there's an error, throw it.
	void FlushBuffer(void)
	{
		Assert(m_rgch <= m_pchOut && m_pchOut <= m_pchOutLim);
		if (m_rgch < m_pchOut)
		{
			(*m_pfnWrite)(m_pvParam, m_rgch, m_pchOut - m_rgch);
			m_pchOut = m_rgch;
		}
	}

	// Write a single character into the buffer.
	void WriteChar(XChar ch)
	{
		if (m_pchOutLim <= m_pchOut)
			FlushBuffer();
		*m_pchOut++ = ch;
	}

	// Write the given characters to the buffer.
	void WriteChars(const XChar * prgch, int cch)
	{
		AssertArray(prgch, cch);

		if (cch > m_pchOutLim - m_pchOut)
		{
			FlushBuffer();
			if (cch > kcchBuf)
			{
				(*m_pfnWrite)(m_pvParam, prgch, cch);
				return;
			}
		}
		Assert(cch <= m_pchOutLim - m_pchOut);
		CopyItems(prgch, m_pchOut, cch);
		m_pchOut += cch;
	}

	// Write a single character multiple times into the buffer.
	void FillWithChar(XChar ch, int cch)
	{
		for (;;)
		{
			int cchT = m_pchOutLim - m_pchOut;
			if (cchT >= cch)
			{
				FillChars(m_pchOut, ch, cch);
				m_pchOut += cch;
				return;
			}
			FillChars(m_pchOut, ch, cchT);
			FlushBuffer();
			cch -= cchT;
		}
	}

	// Return a pointer and cch for a particular string resource and instance.
	void GetResourceString(const XChar ** pprgch, int * pcch, int stid);

	// Do the actual work of formatting.
	void FormatCore(va_list vaArgList);
}; // End of TextFormatter.


/*----------------------------------------------------------------------------------------------
	Return a pointer and cch for a particular string resource and instance, unicode char
	version.
----------------------------------------------------------------------------------------------*/
template<>
	void TextFormatter<wchar, PfnWriteCharsW>::GetResourceString(
		const wchar ** pprgch, int * pcch, int stid)
{
	AssertPtr(pprgch);
	AssertPtr(pcch);

	::GetResourceString(pprgch, pcch, stid);
}

/*----------------------------------------------------------------------------------------------
	Return a pointer and cch for a particular string resource and instance, short (8-bit) char
	version.
----------------------------------------------------------------------------------------------*/
template<>
	void TextFormatter<schar, PfnWriteCharsA>::GetResourceString(
		const schar ** pprgch, int * pcch, int stid)
{
	AssertPtr(pprgch);
	AssertPtr(pcch);

	const wchar * prgchw;
	int cchw;

	::GetResourceString(&prgchw, &cchw, stid);

	// Convert the wchar string to schar.
	m_stbT.Assign(prgchw, cchw);
	*pprgch = m_stbT.Chars();
	*pcch = m_stbT.Length();
}


/*----------------------------------------------------------------------------------------------
	This initializes the TextFormatter and calls FormatCore inside a try/catch block.
	FormatCore and methods it calls throw an hresult when something goes wrong.
	FormatText calls this method.
----------------------------------------------------------------------------------------------*/
template<typename XChar, typename Pfn>
	void TextFormatter<XChar, Pfn>::Format(Pfn pfnWrite, void * pv,
		const XChar * prgchFmt, int cchFmt, va_list vaArgList)
{
	AssertPfn(pfnWrite);
	AssertArray(prgchFmt, cchFmt);

	m_pfnWrite = pfnWrite;
	m_pvParam = pv;
	m_pchIn = prgchFmt;
	m_pchInLim = prgchFmt + cchFmt;
	m_pchOut = m_rgch;
	m_pchOutLim = m_rgch + kcchBuf;

	FormatCore(vaArgList);
	FlushBuffer();
}


/*----------------------------------------------------------------------------------------------
	Helper function for FormatDate() to format day of month
----------------------------------------------------------------------------------------------*/
static int FormatDateDay(int nFmt, wchar * rgch, int count, int nYear, int nMonth, int nDay)
{
	switch (nFmt)
	{
	case 1: // day of month without leading zeros
		return swprintf_s(rgch, count, L"%d", nDay);
	case 2: // day of month always 2 digits
		return swprintf_s(rgch, count, nYear >= 1000 ? L"%02d" : L"%d", nDay);
	case 3: // abbreviated day of week
	case 4: // unabbreviated day of week
		// let system compute day of week
		SYSTEMTIME st = { 0, 0, 0, 0, 0, 0, 0, 0 };
		FILETIME ft;
		st.wYear = (WORD)nYear;
		st.wMonth = (WORD)nMonth;
		st.wDay = (WORD)nDay;
		bool f;
		f = SystemTimeToFileTime (&st, &ft);
		Assert (f);
		f = FileTimeToSystemTime (&ft, &st);
		StrUni *pstu = StrUtil::GetDayOfWeekStr (st.wDayOfWeek+1, nFmt==4);
		// return result
		wcscpy_s(rgch, count, pstu->Chars());
		return pstu->Length();
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Helper function for FormatDate() to format month
----------------------------------------------------------------------------------------------*/
static int FormatDateMonth (int nFmt, wchar *rgch, int count, int nMonth)
{
	switch (nFmt)
	{
	case 1: // numeric month without leading zeros
		return swprintf_s(rgch, count, L"%d", nMonth);
	case 2: // numeric month always 2 digits
		return swprintf_s(rgch, count, L"%02d", nMonth);
	case 3: // abbreviated month
	case 4: // unabbreviated month
		StrUni *pstu = StrUtil::GetMonthStr (nMonth, nFmt==4);
		wcscpy_s (rgch, count, pstu->Chars());
		return pstu->Length();
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Helper function for FormatDate() to format year
----------------------------------------------------------------------------------------------*/
static int FormatDateYear (int n, wchar *rgch, int count, int nYear)
{
	switch (n)
	{
	case 1: // only last digit of year
		return swprintf_s(rgch, count, L"%d", nYear % 10);
	case 2: // last 2 digits of year
		return swprintf_s(rgch, count, L"%02d", nYear % 100);
	case 4: // all digits of year
		return swprintf_s(rgch, count, L"%d", nYear);
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Text formatting: Date
----------------------------------------------------------------------------------------------*/
template<typename XChar, typename Pfn>
	void TextFormatter<XChar, Pfn>::FormatDate(
		XChar rgchTerm[], int & Xcch, int nGenDate, bool fLongDate)
{
	wchar rgchTemp[80] = L""; // do everything with wide chars
	int cch = 0;

	if (nGenDate == 0)
		return; // date is blank

	// Decode date
	int n = nGenDate;
	int nPrec, nYear, nMonth, nDay;
	if (n < 0)
		n = -n;
	nPrec = n % 10;
	n /= 10;
	nDay = n % 100;
	n /= 100;
	nMonth = n % 100;
	nYear = n / 100;
	if (nYear == 0)
		nYear = 1;

	// Select date format
	StrUniBuf stubFmt;
	achar rgchFmt[81];
	int cchFmt;
	if (nDay == 0 && nMonth == 0)
	{
		stubFmt.Assign(L"yyyy");
	}
	else if (nDay == 0)
	{
		cchFmt = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SYEARMONTH, rgchFmt, 80);
		stubFmt.Assign(rgchFmt);
		if (!stubFmt.Length())
			stubFmt.Assign(L"MMMM, yyyy");
	}
	else if (fLongDate)
	{
		achar rgchFmt[81];
		cchFmt = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SLONGDATE, rgchFmt, 80);
		stubFmt.Assign(rgchFmt);
	}
	else
	{
		achar rgchFmt[81];
		cchFmt = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SSHORTDATE, rgchFmt, 80);
		stubFmt.Assign(rgchFmt);
	}

	// Format extra stuff, if any
	if (nPrec != 1 && nPrec < 4)
	{
		LoadDateQualifiers();
		Assert(g_fDoneDateQual);
		wcscpy_s(rgchTemp + cch, SizeOfArray(rgchTemp) - cch, g_rgchwDatePrec[nPrec]);
		cch += wcslen(g_rgchwDatePrec[nPrec]);
		wcscpy_s(rgchTemp + cch, SizeOfArray(rgchTemp) - cch, L" ");
		++cch;
	}

	if (nGenDate > 0 && nDay == 0 && (nMonth == 0 || nYear < 1900))
	{
		wcscpy_s(rgchTemp + cch, SizeOfArray(rgchTemp) - cch, L"AD ");
		cch += 3;
	}

	// TODO SteveMc: this may not work for some non-English languages
	bool fDiscard = false; // Used to omit day of week and punctuation

	// Format date proper
	for (int i = 0; i < stubFmt.Length(); )
	{
		int n;
		wchar ch = stubFmt.GetAt(i);
		for (n = 1; n < 4 && i + n < stubFmt.Length() && stubFmt.GetAt(i + n) == ch; ++n)
		{			// count run of same char
		}
		switch (ch)
		{
		case 'd': // day
			fDiscard = false;
			if (nDay != 0)
			{
				if (n >= 3 && (nGenDate < 0 || nYear < 1900 || nMonth <= 0))
					fDiscard = true; // don't show day of week and punctuation after it
				else
					cch += FormatDateDay(n, rgchTemp + cch, SizeOfArray(rgchTemp) - cch, nYear, nMonth, nDay);
			}
			break;
		case 'M': // month
			fDiscard = false;
			if (nMonth != 0)
				cch += FormatDateMonth(n, rgchTemp + cch, SizeOfArray(rgchTemp) - cch, nMonth);
			break;
		case 'y': // year
			fDiscard = false;
			cch += FormatDateYear(n, rgchTemp + cch, SizeOfArray(rgchTemp) - cch, nYear);
			break;
		case 'g': // period/era
			fDiscard = false;
			wcscpy_s(rgchTemp + cch, SizeOfArray(rgchTemp) - cch, (nGenDate < 0) ? L"BC" : L"AD");
			break;
		case '\'': // quoted data
			i++; // skip leading quote mark
			for (; i < stubFmt.Length() && stubFmt.GetAt(i) != '\''; i++)
				rgchTemp[cch++] = stubFmt.GetAt(i);
			break; // trailing quote will be skipped below
		default: // just copy anything else
			if (!fDiscard)
			{
				for (int j = 0; j < n; j++)
					rgchTemp[cch++] = stubFmt.GetAt(i+j);
			}
			break;
		}
		i += n;
	}

	if (nGenDate < 0)
	{
		wcscpy_s(rgchTemp+cch, SizeOfArray(rgchTemp) - cch, L" BC");
		cch += 3;
	}
	StrBase<XChar> stbT(rgchTemp, cch);		// This handles conversion from Unicode if needed.
	for (int i = 0; i < cch; i++)
		rgchTerm[Xcch++] = stbT.GetAt(i);	// return answer
}

// requires a fixed size parameter list!
uint GetParameter(va_list vaArgList, uint iParameter)
{
	uint iReturn;
	for(uint i=0; i<= iParameter; ++i)
	{
		iReturn = va_arg(vaArgList, uint);
	}
	return iReturn;
}
/*----------------------------------------------------------------------------------------------
	Do the actual text formatting. This throws an hresult if something goes wrong.
----------------------------------------------------------------------------------------------*/
template<typename XChar, typename Pfn>
	void TextFormatter<XChar, Pfn>::FormatCore(va_list vaArgList)
{
	XChar rgchTerm[256];
	const XChar * prgchTerm;
	int cch;
	int cchMin;
	int cchPad;
	int ivArg; // Used for argument reordering.
	uint uT;
	uint uRad;
	XChar * pchT;
	XChar chSign, chPad;
	int dwo;
	GUID * pguid;
	SilTime * pstim;
	SilTimeInfo sti;
	int iv;
	uint * puT;
	int cactRepeat;

	ivArg = 0;

	while (FFetchChar()) // Set m_chCur to the new character and increment m_pchIn.
	{
		// Until the character '%' is found, write the fetched character to the output string.
		if (m_chCur != '%')
		{
			WriteChar(m_chCur); // Write a single char to m_pchOut; increment m_pchOut.
			continue;
		}
		FetchChar(); // m_chCur = *m_pchIn++;

		// First check for the char '%', line termination 'n', or argument reordering '<d>'.
		switch (m_chCur)
		{
		case '%':
			WriteChar('%');
			continue;

		case 'n':
			// Write a line termination appropriate for the OS.
#ifdef WIN32
			rgchTerm[0] = 0x0D; // CR.
			rgchTerm[1] = 0x0A; // LF.
			WriteChars(rgchTerm, 2); // Write CRLF to m_pchOut; increase m_pchOut by 2.
#elif defined(MAC)
			WriteChar(0x0D); // Write CR to m_pchOut; increment m_pchOut.
#else // !WIN32 && !MAC
			WriteChar(0x0A); // Write LF to m_pchOut; increment m_pchOut.
#endif // !WIN32 && !MAC
			continue;

		case '<':
			// Handle argument reordering ('<', 0 based decimal number, '>').
			FetchChar(); // m_chCur = *m_pchIn++;
			ivArg = FetchInt(100); // Parse a non-negative integer. Update m_chCur and m_pchOut.
			Ensure(m_chCur == '>');
			FetchChar(); // m_chCur = *m_pchIn++;
			break;
		}

		dwo = kdwoPadFirst; // Default write order: padding, sign, text.
		chSign = 0; // Default sign, no sign.
		chPad = ' '; // Default padding character, a space.

		// Get qualifiers (count, left justify, sign, chPad, cchMin).
		if (m_chCur == '^')
		{
			// Repeat. This consumes an extra parameter, a count.
			AssertArray(vaArgList, ivArg + 1);
			cactRepeat = static_cast<int>(GetParameter(vaArgList, ivArg));
			++ivArg;
			Ensure(cactRepeat < 0x00010000);
			FetchChar(); // m_chCur = *m_pchIn++;
		}
		else
			cactRepeat = 1; // Default repeat count is 1.

		if (m_chCur == '-')
		{
			// Left justify.
			dwo = kdwoPadLast; // Write order: sign, text, padding.
			FetchChar(); // m_chCur = *m_pchIn++;
		}
		else
		{
			if (m_chCur == '+')
			{
				// Explicit plus sign.
				chSign = '+';
				FetchChar(); // m_chCur = *m_pchIn++;
			}
			if (m_chCur == '0')
			{
				chPad = '0';
				dwo = kdwoPadSecond; // Write order: sign, padding, text.
				FetchChar(); // m_chCur = *m_pchIn++;
			}
		}

		if (m_chCur >= '0' && m_chCur <= '9')
			cchMin = FetchInt(kcchBuf); // Parse non-negative integer. Update m_chCur, m_pchOut.
		else if (m_chCur == '*')
		{
			// Minimum field width. '*' consumes an extra parameter.
			AssertArray(vaArgList, ivArg + 1);
			cchMin = static_cast<int>(GetParameter(vaArgList, ivArg));
			++ivArg;
			Ensure((uint)cchMin < (uint)kcchBuf);
		}
		else
			cchMin = 0;

		// If we're not printing a signed decimal number, chSign is illegal.
		Ensure(!chSign || m_chCur == 'd');

		// Get the parameter.
		AssertArray(vaArgList, ivArg + 1);
		uT = GetParameter(vaArgList, ivArg);
		++ivArg;

		// Code after the switch assumes that prgchTerm points to the characters to write
		// and cch is the number of characters to write.
		prgchTerm = rgchTerm;
		switch (m_chCur)
		{
		case 'c':
			// (int)XChar.
			rgchTerm[0] = (XChar)uT;
			cch = 1;
			break;

		case 's':
			// XChar * psz. (Strings are the same size, schar or wchar).
			prgchTerm = reinterpret_cast<XChar *>(uT);
			AssertPsz(prgchTerm);
			cch = StrLen(prgchTerm);
			break;

		case 'r':
			// Int as stid: resource string id looked up in appropiate DLL.
			GetResourceString(&prgchTerm, &cch, (int)uT);
			AssertPsz(prgchTerm);
			break;

		case 'S':
			// YChar * psz. (Strings are different size, one is schar and one is wchar).
			{ // Block
				YChar * pszy = reinterpret_cast<YChar *>(uT);
				AssertPsz(pszy);
				m_stbT.Assign(pszy);
			}
			prgchTerm = m_stbT.Chars();
			cch = m_stbT.Length();
			break;

		case 'b':
			// BSTR bstr: containing XChar characters.
			prgchTerm = reinterpret_cast<XChar *>(uT);
			if (!prgchTerm)
				cch = 0;
			else
			{
				AssertPtr(((int *)prgchTerm) - 1);
				cch = ((int *)prgchTerm)[-1];
				Assert(cch >= 0 && cch % isizeof(XChar) == 0);
				cch /= isizeof(XChar);
			}
			AssertArray(prgchTerm, cch);
			break;

		case 'B':
			// BSTR bstr: containing YChar characters.
			{ // Block
				YChar * prgchy = reinterpret_cast<YChar *>(uT);
				if (!prgchy)
					cch = 0;
				else
				{
					AssertPtr(((int *)prgchy) - 1);
					cch = ((int *)prgchy)[-1];
					Assert(cch >= 0 && cch % isizeof(YChar) == 0);
					cch /= isizeof(YChar);
				}
				AssertArray(prgchy, cch);
				m_stbT.Assign(prgchy, cch);
			}
			prgchTerm = m_stbT.Chars();
			cch = m_stbT.Length();
			break;

		case 'a':
			// XChar * prgch, int cch. Consume another parameter.
			prgchTerm = reinterpret_cast<XChar *>(uT);
			AssertArray(vaArgList, ivArg + 1);
			cch = static_cast<int>(GetParameter(vaArgList, ivArg));
			++ivArg;
			Ensure((uint)cch < 0x00010000);
			AssertArray(prgchTerm, cch);
			break;

		case 'A':
			// YChar * prgch, int cch. Consume another parameter.
			{ // Block
				YChar * prgchy = reinterpret_cast<YChar *>(uT);
				AssertArray(vaArgList, ivArg + 1);
				cch = static_cast<int>(GetParameter(vaArgList, ivArg));
				++ivArg;
				Ensure((uint)cch < 0x00010000);
				AssertArray(prgchy, cch);
				m_stbT.Assign(prgchy, cch);
			}
			prgchTerm = m_stbT.Chars();
			cch = m_stbT.Length();
			break;

		case 'h':
			// Int as a 4 character value: 'xxxx'.
			for (cch = 4; cch > 0 && uT; uT >>= 8)
			{
				XChar ch = (XChar)(byte)uT;
				if (!ch)
					ch = 1;
				rgchTerm[--cch] = ch;
			}
			prgchTerm = rgchTerm + cch;
			cch = 4 - cch;
			break;

		case 'x':
			// Hex. If cchMin is not 0, don't make it longer than cchMin.
			if (cchMin > 0 && cchMin < 8)
				uT &= (1L << (cchMin * 4)) - 1;
			uRad = 16;
			goto LUnsigned;

		case 'd':
			// Signed decimal.
			if ((int)uT < 0)
			{
				chSign = '-';
				uT = -(int)uT;
			}
			// Fall through.
		case 'u':
			// Unsigned decimal.
			uRad = 10;
LUnsigned:
			pchT = rgchTerm + SizeOfArray(rgchTerm);
			cch = 0;
			do
			{
				*--pchT = g_rgchDigits[uT % uRad];
				cch++;
				uT /= uRad;
			} while (uT);
			prgchTerm = pchT;
			break;

		case 'g':
			// GUID * pguid displayed without curly braces in registry format.
			pguid = reinterpret_cast<GUID *>(uT);
			AssertPtr(pguid);

			pchT = rgchTerm;
			for (iv = 28; iv >= 0; iv -= 4)
				*pchT++ = g_rgchDigits[(pguid->Data1 >> iv) & 0x0F];
			*pchT++ = '-';
			for (iv = 12; iv >= 0; iv -= 4)
				*pchT++ = g_rgchDigits[(pguid->Data2 >> iv) & 0x0F];
			*pchT++ = '-';
			for (iv = 12; iv >= 0; iv -= 4)
				*pchT++ = g_rgchDigits[(pguid->Data3 >> iv) & 0x0F];
			*pchT++ = '-';
			for (iv = 0; iv < 8; iv++)
			{
				if (iv == 2)
					*pchT++ = '-';
				*pchT++ = g_rgchDigits[(pguid->Data4[iv] >> 4) & 0x0F];
				*pchT++ = g_rgchDigits[pguid->Data4[iv] & 0x0F];
			}
			Assert(pchT == rgchTerm + 36);
			cch = pchT - rgchTerm;
			prgchTerm = rgchTerm;
			break;

		case 'G':
			// GUID * pguid displayed as a string of 26 digits and uppercase letters.
			puT = reinterpret_cast<uint *>(uT);
			AssertArray(puT, 4);

			pchT = rgchTerm;
			for (iv = 0; iv < 4; iv++)
			{
				uT = puT[iv];
				*pchT++ = g_rgchDigits[(uT >>  0) & 0x1F];
				*pchT++ = g_rgchDigits[(uT >>  5) & 0x1F];
				*pchT++ = g_rgchDigits[(uT >> 10) & 0x1F];
				*pchT++ = g_rgchDigits[(uT >> 15) & 0x1F];
				*pchT++ = g_rgchDigits[(uT >> 20) & 0x1F];
				*pchT++ = g_rgchDigits[(uT >> 25) & 0x1F];
			}
			uT = (puT[0] >> 30) | ((puT[1] >> 30) << 2) |
				((puT[2] >> 30) << 4) | ((puT[3] >> 30) << 6);
			*pchT++ = g_rgchDigits[(uT >>  0) & 0x1F];
			*pchT++ = g_rgchDigits[(uT >>  5) & 0x1F];
			Assert(pchT == rgchTerm + 26);
			cch = pchT - rgchTerm;
			prgchTerm = rgchTerm;
			break;

		case 't':
			// SilTime *: local time displayed as yyyy-mm-dd:hour:min:sec.msec.
			// A year of zero is 1 BC, -1 is 2 BC, etc.
		case 'T':
			// SilTime *: UTC time displayed as yyyy-mm-dd:hour:min:sec.msec.
			// A year of zero is 1 BC, -1 is 2 BC, etc.
			pstim = reinterpret_cast<SilTime *>(uT);
			AssertPtr(pstim);

			pstim->GetTimeInfo(&sti, m_chCur == 'T');

			// Year.
			uT = sti.year < 0 ? -sti.year : sti.year;
			pchT = rgchTerm + SizeOfArray(rgchTerm);
			cch = 0;
			do
			{
				*--pchT = g_rgchDigits[uT % 10];
				cch++;
				uT /= 10;
			} while (uT);

			if (sti.year < 0)
			{
				rgchTerm[0] = '-';
				MoveItems(pchT, rgchTerm + 1, cch);
				cch++;
			}
			else
				MoveItems(pchT, rgchTerm, cch);

			// Month.
			rgchTerm[cch++] = '-';
			rgchTerm[cch++] = g_rgchDigits[sti.ymon / 10];
			rgchTerm[cch++] = g_rgchDigits[sti.ymon % 10];

			// Day.
			rgchTerm[cch++] = '-';
			rgchTerm[cch++] = g_rgchDigits[sti.mday / 10];
			rgchTerm[cch++] = g_rgchDigits[sti.mday % 10];

			// Hour.
			rgchTerm[cch++] = ':';
			rgchTerm[cch++] = g_rgchDigits[sti.hour / 10];
			rgchTerm[cch++] = g_rgchDigits[sti.hour % 10];

			// Minute.
			rgchTerm[cch++] = ':';
			rgchTerm[cch++] = g_rgchDigits[sti.min / 10];
			rgchTerm[cch++] = g_rgchDigits[sti.min % 10];

			// Second.
			rgchTerm[cch++] = ':';
			rgchTerm[cch++] = g_rgchDigits[sti.sec / 10];
			rgchTerm[cch++] = g_rgchDigits[sti.sec % 10];

			// MilliSecond.
			rgchTerm[cch++] = '.';
			rgchTerm[cch++] = g_rgchDigits[sti.msec / 100];
			rgchTerm[cch++] = g_rgchDigits[(sti.msec / 10) % 10];
			rgchTerm[cch++] = g_rgchDigits[sti.msec % 10];
			break;

		case 'M': // Int as a uppercase Roman numeral string, e.g., MCMIX.
		case 'm': // Int as a lowercase Roman numeral string: mcmix.
			{
				// Anything illegal returns a ?.
				if (uT <= 0)
				{
					rgchTerm[0] = '?';
					cch = 1;
					break;
				}

				static XChar conv[3][3] =
				{
					'i', 'v', 'x', // Use when cDigits = 0 (1-9).
					'x', 'l', 'c', // Use when cDigits = 1 (10-90).
					'c', 'd', 'm' // Use when cDigits = 2 (100-900).
				};
				cch = 0;
				int ich;
				StrAppBuf strbT;
				strbT.Format(_T("%d"), uT);
				for (ich = 0; ich < strbT.Length(); ++ich)
				{
					// Current digit being processed.
					int nDigit = strbT[ich] - '0';
					// Decimal digits to right of current digit.
					int cDigits = strbT.Length() - ich - 1;
					if (cDigits > 2)
					{
						for (int i = 1; i <= nDigit * pow(10.0, cDigits - 3); ++i)
							rgchTerm[cch++] = 'm';
						continue;
					}
					switch (nDigit)
					{
					case 0:
						break;
					case 1:
						rgchTerm[cch++] = conv[cDigits][0];
						break;
					case 2:
						rgchTerm[cch++] = conv[cDigits][0];
						rgchTerm[cch++] = conv[cDigits][0];
						break;
					case 3:
						rgchTerm[cch++] = conv[cDigits][0];
						rgchTerm[cch++] = conv[cDigits][0];
						rgchTerm[cch++] = conv[cDigits][0];
						break;
					case 4:
						rgchTerm[cch++] = conv[cDigits][0];
						rgchTerm[cch++] = conv[cDigits][1];
						break;
					case 5:
						rgchTerm[cch++] = conv[cDigits][1];
						break;
					case 6:
						rgchTerm[cch++] = conv[cDigits][1];
						rgchTerm[cch++] = conv[cDigits][0];
						break;
					case 7:
						rgchTerm[cch++] = conv[cDigits][1];
						rgchTerm[cch++] = conv[cDigits][0];
						rgchTerm[cch++] = conv[cDigits][0];
						break;
					case 8:
						rgchTerm[cch++] = conv[cDigits][1];
						rgchTerm[cch++] = conv[cDigits][0];
						rgchTerm[cch++] = conv[cDigits][0];
						rgchTerm[cch++] = conv[cDigits][0];
						break;
					case 9:
						rgchTerm[cch++] = conv[cDigits][0];
						rgchTerm[cch++] = conv[cDigits][2];
						break;
					default:
						Assert(false);
						break;
					}
				}
				if (m_chCur == 'M')
					for (ich = 0; ich < cch; ++ich)
						rgchTerm[ich] = (XChar)toupper(rgchTerm[ich]);
			}
			break;

		case 'O': // Int as uppercase alpha outline string: A, ... Z, AA, BB, ... ZZ, AAA, ...
		case 'o': // Int as lowercase alpha outline string: a, ... z, aa, bb, ... zz, aaa, ...
			{
				// Anything illegal returns a ?.
				if (uT <= 0)
				{
					rgchTerm[0] = '?';
					cch = 1;
					break;
				}
				cch = ((uT - 1) / 26) + 1;
				XChar ch = (XChar)(((uT - 1) % 26) + ((m_chCur == 'o') ? 'a' : 'A'));
				for (int ich = 0; ich < cch; ich++)
				{
					rgchTerm[ich] = ch;
				}

				/*
				// Alternate approach generating A, ... Z, AA, ... AZ, BA, ..., BZ, AAA.
				XChar str[20];
				int ich = 0;
				cch = 0;
				// Convert the number to letters in reverse order.
				for ( ; uT; ++ich)
				{
					str[ich] = (XChar)((m_chCur == 'o' ? 'a' : 'A') + (uT + 25) % 26);
					uT = (uT - 1) / 26;
				}

				// Send back the reversed string.
				for ( ; ich--; ++cch)
					rgchTerm[cch] = str[ich];
				*/
			}
			break;

		case 'D': // Int as generic date string
			cch = 0;
			FormatDate (rgchTerm, cch, (int)uT, cchMin > 0);
			cchMin = 0; // so we don't get a space for blank long date
			break;

		default:
			Error(); // Unexpected character.
		}

		// Set cchPad to the number of characters to pad.
		cchPad = Max(0, cchMin - cch - (chSign != 0));

		// If we don't need to pad or add a sign simplify dwo.
		if (!cchPad && !chSign)
			dwo = kdwoTextOnly; // Write order: text only.

		while (--cactRepeat >= 0)
		{
			int dwoT;

			for (dwoT = dwo; dwoT; dwoT >>= 4)
			{
				switch (dwoT & 0x0F)
				{
				default:
					Assert(false);
					// Fall through.
				case 1: // Add padding.
					if (cchPad)
						FillWithChar(chPad, cchPad);
					break;

				case 2: // Add the sign.
					if (chSign)
						WriteChar(chSign);
					break;

				case 3: // Add the text.
					WriteChars(prgchTerm, cch);
					break;
				}
			}
		}
	} // End of while (FFetchChar()).

	FlushBuffer();
} // End of TextFormatter<>::FormatCore.


//:Associate with "Generic Text Formatting Functions".
/*----------------------------------------------------------------------------------------------
	Format text data using the given format string and any additional parameters (ala sprintf).
	All parameters are assumed to be 4 bytes long.

	@h3{Parameters}
	@code{
		pfnWrite -- pointer to the function used by FormatText to write text to
			a stream or some type of string object.
		pv -- pointer to a stream or some type of string object. It is supplied
			by the caller of FormatText and passed on as the first argument
			whenever pfnWrite is called.
		prgchFmt -- string used as the template.
		cchFmt -- number of characters in the template string.
		vaArgList -- additional parameters used with the template string.
	}

	Returns an error code if the PfnWriteChars function fails or the format string is invalid.

	In the comments below, YChar indicates CharDefns<XChar>::OtherChar.

	@code{
	These control characters don't consume a parameter:

		%n An end of line sequence appropriate for the platform.
		%% a percent sign.

	These control characters consume a single parameter:

		%c (int)XChar.
		%s XChar * psz. (Strings are the same size, schar or wchar).
		%S YChar * psz. (Strings are different size, one is schar and one is wchar).
		%b BSTR bstr: containing XChar characters.
		%B BSTR bstr: containing YChar characters.
		%d signed decimal.
		%u unsigned decimal.
		%x hex.
		%h int as a 4 character value: 'xxxx'.
		%g GUID * pguid displayed without curly braces in registry format.
		%G GUID * pguid displayed as a string of 26 digits and uppercase letters.
		%t SilTime *: local time displayed as yyyy-mm-dd:hour:min:sec.msec.
			A year of zero is 1 BC, -1 is 2 BC, etc.
		%T SilTime *: UTC time displayed as yyyy-mm-dd:hour:min:sec.msec.
			A year of zero is 1 BC, -1 is 2 BC, etc.
		%m int as a lowercase roman numeral string: mcmix.
		%M int as a uppercase roman numeral string: MCMIX.
		%o int as a lowercase alpha outline string: a, b, ... z, aa, bb, ... zz, aaa, bbb, ...
		%O int as a uppercase alpha outline string: A, B, ... Z, AA, BB, ... ZZ, AAA, BBB, ...
		%r int as stid: resource string id looked up in appropiate DLL.
		%D int as a generic date string. %D for short date, %1D for long date

	These control characters consume two parameters.

		%a XChar * prgch, int cch.
		%A YChar * prgch, int cch.

	The controls that consume at least one parameter support the following options,
	in this order:

		Argument reordering ('<', 0 based decimal number, '>').
		Repeat ('^') : this consumes an extra parameter - a count.
		Left justify ('-').
		Explicit plus sign ('+').
		Zero padding instead of space padding ('0').
		Minimum field width (decimal number or '*'). '*' consumes an extra parameter.
	}

	These all go between the '%' and the control character. Note that argument reordering
	affects everything after the reordered arg in the control string. Eg, "%<1>d %<0>d %d %d"
	will use arguments in the order { 1, 0, 1, 2 }. If you just want to switch two arguments,
	the one following needs a number also. So the above example would be "%<1>d %<0>d %<2>d %d",
	producing { 1, 0, 2, 3 }.

	WARNING: all arguments should be 4 bytes long.
----------------------------------------------------------------------------------------------*/
template<typename XChar, typename Pfn>
	void FormatText(Pfn pfnWrite, void * pv, const XChar * prgchFormat, int cchFormat,
		va_list vaArgList)
{
	AssertPfn(pfnWrite);
	AssertArray(prgchFormat, cchFormat);

	TextFormatter<XChar, Pfn> tfmt;

	tfmt.Format(pfnWrite, pv, prgchFormat, cchFormat, vaArgList);
}


/*----------------------------------------------------------------------------------------------
	Instantiate FormatToStream functions.
----------------------------------------------------------------------------------------------*/
template
	void FormatToStream<wchar>(IStream * pstrm, const wchar * pszFmt, ...);
template
	void FormatToStream<schar>(IStream * pstrm, const schar * pszFmt, ...);

template
	void FormatToStreamRgch<wchar>(IStream * pstrm,
		const wchar * prgchFmt, int cchFmt, ...);
template
	void FormatToStreamRgch<schar>(IStream * pstrm,
		const schar * prgchFmt, int cchFmt, ...);

template
	void FormatToStreamCore<wchar>(IStream * pstrm,
		const wchar * prgchFmt, int cchFmt, va_list vaArgList);
template
	void FormatToStreamCore<schar>(IStream * pstrm,
		const schar * prgchFmt, int cchFmt, va_list vaArgList);

/*----------------------------------------------------------------------------------------------
	Callback for FormatToStream. This method is called by FormatToStream, FormatToStreamRgch,
	and FormatToStreamCore. See FormatText.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	static void FormatCallbackStream(void * pv, const XChar * prgch, int cch)
{
	IStream * pstrm = (IStream *)pv;
	AssertPtr(pstrm);
	AssertArray(prgch, cch);

	if (cch)
		WriteBuf(pstrm, prgch, cch * isizeof(XChar));
}


/*----------------------------------------------------------------------------------------------
	Format the template string pszFmt to an IStream. The arguments must all be 4 bytes long.
	See FormatText.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void FormatToStream(IStream * pstrm, const XChar * pszFmt, ...)
{
	AssertPtr(pstrm);
	AssertPsz(pszFmt);

	va_list argList;
	va_start(argList, pszFmt);
	FormatText((CharDefns<XChar>::PfnWriteChars)&FormatCallbackStream<XChar>, pstrm,
		pszFmt, StrLen(pszFmt), argList);
	va_end(argList);
}


/*----------------------------------------------------------------------------------------------
	Format the template string (prgchFmt, cchFmt) to an IStream. The arguments must all be 4
	bytes long. See FormatText.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void FormatToStreamRgch(IStream * pstrm, const XChar * prgchFmt, int cchFmt, ...)
{
	AssertPtr(pstrm);
	AssertArray(prgchFmt, cchFmt);

	va_list argList;
	va_start(argList, cchFmt);
	FormatText((CharDefns<XChar>::PfnWriteChars)&FormatCallbackStream<XChar>, pstrm,
		prgchFmt, cchFmt, argList);
	va_end(argList);
}


/*----------------------------------------------------------------------------------------------
	Format the items in vaArgList according to the specifications in the template string
	(prgchFmt, cchFmt). Append the output to the IStream. See FormatText.

	Note: The arguments must all be 4 bytes long.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void FormatToStreamCore(IStream * pstrm, const XChar * prgchFmt, int cchFmt,
		va_list vaArgList)
{
	AssertPtr(pstrm);
	AssertArray(prgchFmt, cchFmt);

	FormatText((CharDefns<XChar>::PfnWriteChars)&FormatCallbackStream<XChar>, pstrm,
		prgchFmt, cchFmt, vaArgList);
}


//:>********************************************************************************************
//:>	Smart string class StrBase<>.
//:>********************************************************************************************

//:Associate with StrBase<wchar>.
template class StrBase<wchar>; // Instantiation for XChar = wchar.
StrBase<wchar>::StrBuffer StrBase<wchar>::s_bufEmpty; // Instantiation of empty wchar buffer.

//:Associate with StrBase<schar>.
template class StrBase<schar>; // Instantiation for XChar = schar.
StrBase<schar>::StrBuffer StrBase<schar>::s_bufEmpty; // Instantiation of empty schar buffer.


/*----------------------------------------------------------------------------------------------
	Create a new internal buffer of size cch and return a pointer to the characters.
	This preserves the characters currently in the string, up the the min of the old and
	new sizes. It is expected that the caller will fill in any newly allocated characters.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::SetSize(int cchNew, XChar ** pprgch)
{
	AssertObj(this);
	AssertPtr(pprgch);

	if (!cchNew)
	{
		_SetBuf(&s_bufEmpty);
		*pprgch = NULL;
		return;
	}

	int cchCur = m_pbuf->Cch();

	if (cchNew != cchCur || m_pbuf->m_crefMinusOne)
	{
		StrBuffer * pbuf = StrBuffer::Create(cchNew);
		if (cchCur > 0)
			CopyItems(m_pbuf->m_rgch, pbuf->m_rgch, Min(cchCur, cchNew));
		_AttachBuf(pbuf);
	}

	*pprgch = m_pbuf->m_rgch;

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Set the character at index ich to ch.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::SetAt(int ich, XChar ch)
{
	AssertObj(this);
	Assert(ich >= 0);
	Assert((uint)ich < (uint)m_pbuf->Cch());

	// If ch is the same as the character already at ich, return.
	if (ch == m_pbuf->m_rgch[ich])
		return;

	// If this string object is sharing a buffer, then you must not change the other string
	// sharing the buffer.  Rather, copy it.
	if (m_pbuf->m_crefMinusOne > 0)
		_Copy();

	AssertObj(m_pbuf);
	Assert(m_pbuf->m_crefMinusOne == 0);
	m_pbuf->m_rgch[ich] = ch;

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Get an allocated BSTR that the caller is responsible for freeing.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::GetBstr(BSTR * pbstr) const
{
	AssertObj(this);
	AssertPtr(pbstr);
	Assert(!*pbstr); // We do not free any old BSTRs, so the client must take care of this.

	if (!m_pbuf->m_cb)
		return;

	int cchw = ConvertText(m_pbuf->m_rgch, m_pbuf->Cch(), (OLECHAR *)NULL, 0);
	if (!cchw)
		ThrowHr(WarnHr(E_UNEXPECTED));
	*pbstr = SysAllocStringLen(NULL, cchw);
	if (!*pbstr)
		ThrowHr(WarnHr(E_OUTOFMEMORY));
	ConvertText(m_pbuf->m_rgch, m_pbuf->Cch(), *pbstr, cchw);
}


/*----------------------------------------------------------------------------------------------
	Convert characters to lower case. If this string object is sharing a buffer, then the
	existing characters are copied into a new buffer solely owned by this StrBase<>.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::ToLower(void)
{
	AssertObj(this);

	if (!m_pbuf->Cch())
		return;

	// If this string object is sharing a buffer, then you must not change the other string
	// sharing the buffer. Rather, copy it.
	if (m_pbuf->m_crefMinusOne > 0)
		_Copy();

	AssertObj(m_pbuf);
	Assert(m_pbuf->m_crefMinusOne == 0);
	::ToLower(m_pbuf->m_rgch, m_pbuf->Cch());

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Convert characters to upper case. If this string object is sharing a buffer, then the
	existing characters are copied into a new buffer solely owned by this StrBase<>.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::ToUpper(void)
{
	AssertObj(this);

	if (!m_pbuf->Cch())
		return;

	// If this string object is sharing a buffer, then you must not change the other string
	// sharing the buffer. Rather, copy it.
	if (m_pbuf->m_crefMinusOne > 0)
		_Copy();

	AssertObj(m_pbuf);
	Assert(m_pbuf->m_crefMinusOne == 0);
	::ToUpper(m_pbuf->m_rgch, m_pbuf->Cch());

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Copy the existing characters into a new buffer solely owned by this StrBase<>. This is
	so we can modify characters in place.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::_Copy(void)
{
	AssertObj(this);
	Assert(m_pbuf->m_crefMinusOne > 0);
	Assert(m_pbuf->Cch() > 0);

	// Allocate the new buffer.
	int cch = m_pbuf->Cch();
	StrBuffer * pbuf = StrBuffer::Create(cch);

	CopyItems(m_pbuf->m_rgch, pbuf->m_rgch, cch);

	// Set our buffer to the new one.
	_AttachBuf(pbuf);

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Read cch characters from the IStream and set the value of this StrBase<> to those
	characters.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::ReadChars(IStream * pstrm, int cch)
{
	AssertObj(this);
	AssertPtr(pstrm);

	if (!cch)
	{
		_SetBuf(&s_bufEmpty);
		return;
	}

	_AttachBuf(StrBuffer::Create(cch));
	ReadBuf(pstrm, m_pbuf->m_rgch, cch * isizeof(XChar));

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Replace the range [ichMin, ichLim) with the given characters of the same type.

	WARNING: We need to take care not to free the existing buffer until the operation succeeds.
	This is in case the input uses the existing buffer.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::_Replace(int ichMin, int ichLim,
		const XChar * prgchIns, XChar chIns, int cchIns, int nCodePage)
{
	AssertObj(this);
	Assert(cchIns >= 0);
	AssertArrayN(prgchIns, cchIns);
	Assert(!chIns || !prgchIns);

	int cchCur = m_pbuf->Cch();
	Assert((uint)ichMin <= (uint)ichLim && (uint)ichLim <= (uint)cchCur);

	if (!cchIns)
	{
		// Nothing's being inserted.
		if (ichMin == ichLim)
		{
			// Nothing's being deleted either so just return.
			return;
		}
		if (!ichMin && ichLim == cchCur)
		{
			// Everything is being deleted so clear the string.
			_SetBuf(&s_bufEmpty);
			return;
		}
	}

	StrBuffer * pbuf;
	int cchNew = cchCur + cchIns - ichLim + ichMin;

	if (cchNew == cchCur && !m_pbuf->m_crefMinusOne)
	{
		// The buffer size is staying the same and we own the characters.
		pbuf = m_pbuf;
	}
	else
	{
		// Allocate the new buffer.
		pbuf = StrBuffer::Create(cchNew);
	}

	// Copy the text.
	if (ichMin > 0 && pbuf != m_pbuf)
		CopyItems(m_pbuf->m_rgch, pbuf->m_rgch, ichMin);
	if (cchIns > 0)
	{
		if (prgchIns)
			CopyItems(prgchIns, pbuf->m_rgch + ichMin, cchIns);
		else
			FillChars(pbuf->m_rgch + ichMin, chIns, cchIns);
	}
	if (pbuf != m_pbuf)
	{
		if (ichLim < cchCur)
			CopyItems(m_pbuf->m_rgch + ichLim, pbuf->m_rgch + ichMin + cchIns, cchCur - ichLim);
		// Set our buffer to the new one.
		_AttachBuf(pbuf);
	}

	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Replace the range [ichMin, ichLim) with the given characters of the other type. Use the given
	codepage to convert between Unicode and 8-bit data.

	WARNING: We need to take care not to free the existing buffer until the operation succeeds.
	This is in case the input uses the existing buffer.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::_Replace(int ichMin, int ichLim,
		const YChar * prgchIns, YChar chIns, int cchIns, int nCodePage)
{
	AssertObj(this);
	Assert(cchIns >= 0);
	AssertArray(prgchIns, cchIns);

	// These are used ony when prgchIns is NULL.
	const int kcchMaxChar = 8;
	XChar rgchChar[kcchMaxChar];
	int cchChar;

	int cchCur = m_pbuf->Cch();
	Assert((uint)ichMin <= (uint)ichLim && (uint)ichLim <= (uint)cchCur);

	// Determine the number of characters we're inserting.
	int cchDst;

	if (cchIns)
	{
		if (prgchIns)
		{
			cchDst = ConvertText(prgchIns, cchIns, (XChar *)NULL, 0, nCodePage);
			if (!cchDst)
				ThrowHr(WarnHr(E_FAIL));
			Assert(cchCur + cchDst > cchCur);
		}
		else
		{
			cchChar = ConvertText(&chIns, 1, rgchChar, kcchMaxChar, nCodePage);
			if (!cchChar)
				ThrowHr(WarnHr(E_FAIL));
			Assert((uint)cchChar <= (uint)kcchMaxChar);
			cchDst = cchChar * cchIns;
			Assert(cchCur + cchDst > cchCur);
		}
	}
	else
		cchDst = 0;

	// Allocate the new buffer.
	StrBuffer * pbuf;
	int cchNew = cchCur + cchDst - ichLim + ichMin;

	if (cchNew == cchCur && !m_pbuf->m_crefMinusOne)
	{
		// The buffer size is staying the same and we own the characters.
		pbuf = m_pbuf;
	}
	else
	{
		// Allocate the new buffer.
		pbuf = StrBuffer::Create(cchNew);
	}

	// Copy and convert the text.
	if (ichMin > 0 && pbuf != m_pbuf)
		CopyItems(m_pbuf->m_rgch, pbuf->m_rgch, ichMin);
	if (cchDst > 0)
	{
		if (prgchIns)
			ConvertText(prgchIns, cchIns, pbuf->m_rgch + ichMin, cchDst, nCodePage);
		else
		{
			XChar * pch = pbuf->m_rgch + ichMin;
			if (cchChar == 1)
				FillChars(pch, rgchChar[0], cchIns);
			else
			{
				int cchT;
				for (cchT = cchIns; --cchT >= 0; )
				{
					CopyItems(rgchChar, pch, cchChar);
					pch += cchChar;
				}
			}
		}
	}
	if (pbuf != m_pbuf)
	{
		if (ichLim < cchCur)
			CopyItems(m_pbuf->m_rgch + ichLim, pbuf->m_rgch + ichMin + cchDst, cchCur - ichLim);
		// Set our buffer to the new one.
		_AttachBuf(pbuf);
	}

	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Replace the buffer for this StrBase<> with a new string constructed by formatting the
	string template (prgchFmt, cchFmt). See FormatText.

	@h3{Parameters}
	@code{
		prgchFmt -- string, of the same type of characters as this StrBase<>, used as the
					template.
		cchFmt -- number of characters in the template string.
		vaArgList -- additional parameters used with the template string.
	}

	WARNING: We need to take care not to free the existing buffer until the operation succeeds.
	This is in case the input uses the existing buffer.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::FormatCore(const XChar * prgchFmt, int cchFmt, va_list vaArgList)
{
	AssertObj(this);
	AssertArray(prgchFmt, cchFmt);

	StrBase<XChar> stb;

	FormatText((CharDefns<XChar>::PfnWriteChars)&FormatCallback, &stb,
		prgchFmt, cchFmt, vaArgList);

	_SetBuf(stb.m_pbuf);
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Replace the buffer for this StrBase<> with a new string constructed by formatting the
	string template (prgchFmt, cchFmt). See FormatText.

	@h3{Parameters}
	@code{
		prgchFmt -- string, of the other type of characters as this StrBase<>, used as the
					template.
		cchFmt -- number of characters in the template string.
		vaArgList -- additional parameters used with the template string.
	}

	WARNING: We need to take care not to free the existing buffer until the operation succeeds.
	This is in case the input uses the existing buffer.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::FormatCore(const YChar * prgchFmt, int cchFmt, va_list vaArgList)
{
	AssertObj(this);
	AssertArray(prgchFmt, cchFmt);

	StrBase<XChar> stbFmt(prgchFmt, cchFmt);
	StrBase<XChar> stb;

	FormatText((CharDefns<XChar>::PfnWriteChars)&FormatCallback, &stb,
		stbFmt.Chars(), stbFmt.Length(), vaArgList);

	_SetBuf(stb.m_pbuf);
	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Append, to the buffer of this StrBase<>, a new string constructed by formatting the
	string template (prgchFmt, cchFmt). See FormatText.

	@h3{Parameters}
	@code{
		prgchFmt -- string, of the same type of characters as this StrBase<>, used as the
					template.
		cchFmt -- number of characters in the template string.
		vaArgList -- additional parameters used with the template string.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::FormatAppendCore(const XChar * prgchFmt, int cchFmt,
		va_list vaArgList)
{
	AssertObj(this);
	AssertArray(prgchFmt, cchFmt);

	StrBase<XChar> stb = *this;

	FormatText((CharDefns<XChar>::PfnWriteChars)&FormatCallback, &stb,
		prgchFmt, cchFmt, vaArgList);

	_SetBuf(stb.m_pbuf);
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Append, to the buffer of this StrBase<>, a new string constructed by formatting the
	string template (prgchFmt, cchFmt). See FormatText.

	@h3{Parameters}
	@code{
		prgchFmt -- string, of the other type of characters as this StrBase<>, used as the
					template.
		cchFmt -- number of characters in the template string.
		vaArgList -- additional parameters used with the template string.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::FormatAppendCore(const YChar * prgchFmt, int cchFmt,
		va_list vaArgList)
{
	AssertObj(this);
	AssertArray(prgchFmt, cchFmt);

	StrBase<XChar> stbFmt(prgchFmt, cchFmt);
	StrBase<XChar> stb = *this;

	FormatText((CharDefns<XChar>::PfnWriteChars)&FormatCallback, &stb,
		stbFmt.Chars(), stbFmt.Length(), vaArgList);

	_SetBuf(stb.m_pbuf);
	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Callback for formatting text to a StrBase<>. See FormatText.

	@h3{Parameters}
	@code{
		pv -- pointer to a stream or some type of string object. It is supplied by the
			caller of FormatText and passed on as the first argument whenever pfnWrite is
			called.
		prgch -- string used as the template.
		cch -- number of characters in the template string.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::FormatCallback(void * pv, const XChar * prgch, int cch)
{
	StrBase<XChar> * pstb = (StrBase<XChar> *)pv;
	AssertObj(pstb);
	AssertArray(prgch, cch);

	int cchCur = pstb->m_pbuf->Cch();
	pstb->_Replace(cchCur, cchCur, prgch, 0, cch);
}

/*----------------------------------------------------------------------------------------------
	Return an actual character pointer of a description of the current properties.
	This is meant for use in the debugger, to be displayed in the Output window.
----------------------------------------------------------------------------------------------*/
#ifdef DEBUG
// bammel: Can't get the following to work, it gives unresolved external during link:
//template<typename XChar>
//    OLECHAR * StrBase<XChar>::Dbw1::Watch() ...

OLECHAR * StrBase<wchar>::Dbw1::Watch()
{
	if (!::_CrtIsValidPointer(this, isizeof(this), TRUE ))
		return L"A messed-up string (1)";
	if (!dynamic_cast<StrBase<wchar>::Dbw1 *>(this))
		return L"A messed-up string (2)";
	if (!::_CrtIsValidPointer(m_pstrbase, isizeof(*m_pstrbase), TRUE ))
		return L"A messed-up string (3)";
	StrBuffer * pbuf = m_pstrbase->m_pbuf;
	if (!::_CrtIsValidPointer(pbuf, isizeof(*pbuf), TRUE ))
		return L"A messed-up string (4)";
	if (!::_CrtIsValidPointer(pbuf->m_rgch, pbuf->m_cb, TRUE ))
		return L"A messed-up string (5)";
	int iv, cv = pbuf->m_cb / sizeof(pbuf->m_rgch[0]);

	m_nSerial++;
	Output("#%d %d \"", m_nSerial, cv);
	for (iv = 0; iv < cv; iv++)
		Output("%lc", (wchar)pbuf->m_rgch[iv]);
	Output("\"\n");

	return L"See Debugger Output window";
}

OLECHAR * StrBase<schar>::Dbw1::Watch()
{
	if (!::_CrtIsValidPointer(this, isizeof(this), TRUE ))
		return L"A messed-up string (1)";
	if (!dynamic_cast<StrBase<schar>::Dbw1 *>(this))
		return L"A messed-up string (2)";
	if (!::_CrtIsValidPointer(m_pstrbase, isizeof(*m_pstrbase), TRUE ))
		return L"A messed-up string (3)";
	StrBuffer * pbuf = m_pstrbase->m_pbuf;
	if (!::_CrtIsValidPointer(pbuf, isizeof(*pbuf), TRUE ))
		return L"A messed-up string (4)";
	if (!::_CrtIsValidPointer(pbuf->m_rgch, pbuf->m_cb, TRUE ))
		return L"A messed-up string (5)";
	int iv, cv = pbuf->m_cb / sizeof(pbuf->m_rgch[0]);

	m_nSerial++;
	Output("#%d %d \"", m_nSerial, cv);
	for (iv = 0; iv < cv; iv++)
		Output("%lc", (wchar)pbuf->m_rgch[iv]);
	Output("\"\n");

	return L"See Debugger Output window";
}
#endif //DEBUG

//:>********************************************************************************************
//:>	Smart string class StrBaseBuf<>.
//:>********************************************************************************************

//:Associate with StrBaseBuf<>.
template class StrBaseBufCore<wchar>; // Instantiation for XChar = wchar.
template class StrBaseBufCore<schar>; // Instantiation for XChar = schar.

template class StrBaseBuf<wchar, kcchMaxBufShort>; // Instantiation for wchar, size=32 chars.
template class StrBaseBuf<schar, kcchMaxBufShort>; // Instantiation for schar, size=32 chars.

template class StrBaseBuf<wchar, kcchMaxBufDef>; // Instantiation for wchar, size=260 chars.
template class StrBaseBuf<schar, kcchMaxBufDef>; // Instantiation for schar, size=260 chars.

#if MAX_PATH > kcchMaxBufDef
#warning "MAX_PATH is bigger than kcchMaxBufDef"
template class StrBaseBuf<wchar, MAX_PATH>; // Instantiation for wchar, size=MAX_PATH chars.
template class StrBaseBuf<schar, MAX_PATH>; // Instantiation for schar, size=MAX_PATH chars.
#endif

template class StrBaseBuf<wchar, kcchMaxBufBig>; // Instantiation for wchar, size=1024 chars.
template class StrBaseBuf<schar, kcchMaxBufBig>; // Instantiation for schar, size=1024 chars.

template class StrBaseBuf<wchar, kcchMaxBufHuge>; // Instantiation for wchar, size=16384 chars.
template class StrBaseBuf<schar, kcchMaxBufHuge>; // Instantiation for schar, size=16384 chars.


/*----------------------------------------------------------------------------------------------
	Get an allocated BSTR that the caller is responsible for freeing.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBaseBufCore<XChar>::GetBstr(BSTR * pbstr) const
{
	AssertObj(this);
	AssertPtr(pbstr);
	Assert(!*pbstr);

	if (!m_cb)
		return;

	int cchw = ConvertText(m_rgch, Length(), (OLECHAR *)NULL, 0);
	if (!cchw)
		ThrowHr(WarnHr(E_UNEXPECTED));
	*pbstr = SysAllocStringLen(NULL, cchw);
	if (!*pbstr)
		ThrowHr(WarnHr(E_OUTOFMEMORY));
	ConvertText(m_rgch, Length(), *pbstr, cchw);
}


/*----------------------------------------------------------------------------------------------
	Assign the characters from the given string (prgch, cch) of the same character type to be
	the value of this StrBaseBuf<>. If there is an overflow, copy the characters that fit and
	set the overflow flag, m_fOverflow.
----------------------------------------------------------------------------------------------*/
template<typename XChar, int kcchMax>
	bool StrBaseBuf<XChar, kcchMax>::Assign(const XChar * prgch, int cch)
{
	AssertObj(this);
	AssertArray(prgch, cch);

	// Check that the chars to insert do not overlap the buffer.
	Assert((byte *)(prgch + cch) <= (byte *)this || (byte *)prgch >= (byte *)(this + 1));

	// Check that the string fits.
	if (cch > kcchMax)
	{
		m_fOverflow = true;
		cch = kcchMax; // Don't copy more than kcchMax chars.
	}
	else
		m_fOverflow = false;

	// Copy the text.
	CopyItems(prgch, m_rgch, cch);
	_SetLen(cch);

	AssertObj(this);
	return !m_fOverflow;
}

/*----------------------------------------------------------------------------------------------
	Assign the characters from the given string (prgch, cch) of the other character type to be
	the value of this StrBaseBuf<>. If there is an overflow, copy the characters that fit and
	set the overflow flag, m_fOverflow.
----------------------------------------------------------------------------------------------*/
template<typename XChar, int kcchMax>
	bool StrBaseBuf<XChar, kcchMax>::Assign(const YChar * prgych, int cychSrc)
{
	AssertObj(this);
	AssertArray(prgych, cychSrc);

	// Check that the chars to insert do not overlap the buffer.
	Assert((byte *)(prgych + cychSrc) <= (byte *)this ||
		(byte *)prgych > (byte *)(this + 1));

	// Get the size and try to convert the characters. If there is an overflow, ConvertText
	// returns zero.
	int cxchDst = ConvertText(prgych, cychSrc, m_rgch, kcchMax);

	// Check that the string fits.
	if (cxchDst == 0 && cychSrc > 0)
	{
		m_fOverflow = true;
		// Determine the characters that fit.
		cychSrc = CychFitConvertedText(prgych, cychSrc, kcchMax);
		cxchDst = ConvertText(prgych, cychSrc, m_rgch, kcchMax);
		Assert(cxchDst <= kcchMax);
	}
	else
		m_fOverflow = false;

	_SetLen(cxchDst);

	AssertObj(this);
	return !m_fOverflow;
}


/*----------------------------------------------------------------------------------------------
	Read cch characters from the IStream and set the value of this StrBaseBuf<> to those
	characters. If cch is greater than the maximum number of characters allowed, adjust cch
	to copy the characters that fit and set the overflow flag, m_fOverflow.
----------------------------------------------------------------------------------------------*/
template<typename XChar, int kcchMax>
	void StrBaseBuf<XChar, kcchMax>::ReadChars(IStream * pstrm, int cch)
{
	AssertObj(this);
	AssertPtr(pstrm);

	if (!cch)
	{
		_Clear();
		return;
	}

	// Adjust cch if it is greater than the maximum number of characters allowed.
	if (cch > kcchMax)
	{
		m_fOverflow = true;
		cch = kcchMax;
	}

	ReadBuf(pstrm, m_rgch, cch * isizeof(XChar));
	_SetLen(cch);

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Append a copy of the characters from the given string (prgch, cch) of the same character
	type to the value of this StrBaseBuf<>. If there is an overflow, copy the characters that
	fit and set the overflow flag, m_fOverflow.
----------------------------------------------------------------------------------------------*/
template<typename XChar, int kcchMax>
	bool StrBaseBuf<XChar, kcchMax>::Append(const XChar * prgch, int cch)
{
	AssertObj(this);
	AssertArray(prgch, cch);

	// Return if there are no characters to copy.
	if (!cch)
		return !m_fOverflow;

	int cchCur = Length();

	// Check that the chars to insert do not overlap where we will write new chars.
	Assert(prgch + cch <= m_rgch + cchCur || (byte *)prgch >= (byte *)(this + 1));

	// Check that the string fits.
	int cchNew = cchCur + cch;
	if ((uint)cchNew > (uint)kcchMax)
	{
		m_fOverflow = true;
		// Determine the characters that fit.
		cch = kcchMax - cchCur;
		cchNew = kcchMax;
	}

	// Copy the text.
	if (cch > 0)
		CopyItems(prgch, m_rgch + cchCur, cch);
	_SetLen(cchNew);

	AssertObj(this);
	return !m_fOverflow;
}

/*----------------------------------------------------------------------------------------------
	Append a copy of the characters from the given string (prgch, cch) of the other character
	type to the value of this StrBaseBuf<>. If there is an overflow, copy the characters that
	fit and set the overflow flag, m_fOverflow.
----------------------------------------------------------------------------------------------*/
template<typename XChar, int kcchMax>
	bool StrBaseBuf<XChar, kcchMax>::Append(const YChar * prgych, int cychSrc)
{
	AssertObj(this);
	AssertArray(prgych, cychSrc);

	// Return if there are no characters to copy.
	if (!cychSrc)
		return !m_fOverflow;

	int cchCur = Length();

	// Check that the chars to insert do not overlap where we will write new chars.
	Assert((byte *)(prgych + cychSrc) <= (byte *)(m_rgch + cchCur) ||
		(byte *)prgych >= (byte *)(this + 1));

	// Get the size and try to convert the characters. If there is an overflow, ConvertText
	// returns zero.
	int cxchDst = ConvertText(prgych, cychSrc, m_rgch + cchCur, kcchMax - cchCur);

	// Check that the string fits.
	if (cxchDst == 0)
	{
		m_fOverflow = true;
		// Determine the characters that fit.
		cychSrc = CychFitConvertedText(prgych, cychSrc, kcchMax - cchCur);
		cxchDst = ConvertText(prgych, cychSrc, m_rgch + cchCur, kcchMax - cchCur);
		Assert(cxchDst <= kcchMax - cchCur);
	}
	Assert((uint)(cchCur + cxchDst) <= (uint)kcchMax);

	_SetLen(cchCur + cxchDst);

	AssertObj(this);
	return !m_fOverflow;
}


/*----------------------------------------------------------------------------------------------
	Replace the range of characters [ichMin, ichLim) with the characters from the
	string (prgch, cch) of the same type of character. If there is an overflow, copy the
	characters that fit and set the overflow flag, m_fOverflow.
----------------------------------------------------------------------------------------------*/
template<typename XChar, int kcchMax>
	bool StrBaseBuf<XChar, kcchMax>::Replace(int ichMin, int ichLim,
		const XChar * prgchIns, int cchIns)
{
	AssertObj(this);
	AssertArray(prgchIns, cchIns);
	Assert(0 <= ichMin && (uint)ichMin <= (uint)ichLim &&
		((uint)ichLim <= (uint)Length() || m_fOverflow));

	// Limit ichMin and ichLim to the length.
	int cchCur = Length();
	if (ichMin > cchCur)
		ichMin = cchCur;
	if (ichLim > cchCur)
		ichLim = cchCur;

	// Return if there are no characters to insert and no characters to delete.
	if (!cchIns && ichMin == ichLim)
		return !m_fOverflow;

	// Check that the chars to insert do not overlap the buffer.
	Assert(prgchIns + cchIns <= m_rgch + ichMin || (byte *)prgchIns > (byte *)(this + 1));

	// Check that the replacement fits.
	if ((uint)(ichMin + cchIns) <= (uint)kcchMax)
	{
		// Check whether the right-most substring may need to be truncated.
		if (cchCur + cchIns - (ichLim - ichMin) > kcchMax)
		{
			m_fOverflow = true;
			// Truncate the current text by adjusting cchCur.
			cchCur = kcchMax - cchIns + ichLim - ichMin;
		}
		// Move the right-most characters.
		if (ichLim < cchCur && ichLim != ichMin + cchIns)
			MoveItems(m_rgch + ichLim, m_rgch + ichMin + cchIns, cchCur - ichLim);

		// Insert the text.
		if (cchIns > 0)
			CopyItems(prgchIns, m_rgch + ichMin, cchIns);
		_SetLen(cchCur + cchIns - (ichLim - ichMin));
	}
	else // The text to be inserted will itself overflow the buffer.
	{
		m_fOverflow = true;
		// Truncate the text being inserted by adjusting cchIns.
		CopyItems(prgchIns, m_rgch + ichMin, kcchMax - ichMin);
		_SetLen(kcchMax);
	}

	AssertObj(this);
	return !m_fOverflow;
}

/*----------------------------------------------------------------------------------------------
	Replace the range of characters [ichMin, ichLim) with the characters from the
	string (prgch, cch) of the other type of character. If there is an overflow, copy the
	characters that fit and set the overflow flag, m_fOverflow.
----------------------------------------------------------------------------------------------*/
template<typename XChar, int kcchMax>
	bool StrBaseBuf<XChar, kcchMax>::Replace(int ichMin, int ichLim,
		const YChar * prgych, int cychSrc)
{
	AssertObj(this);
	AssertArray(prgych, cychSrc);
	Assert(0 <= ichMin && (uint)ichMin <= (uint)ichLim &&
		((uint)ichLim <= (uint)Length() || m_fOverflow));

	// Limit ichMin and ichLim to the length.
	int cchCur = Length();
	if (ichMin > cchCur)
		ichMin = cchCur;
	if (ichLim > cchCur)
		ichLim = cchCur;

	// Return if there are no characters to insert and no characters to delete.
	if (!cychSrc && ichMin == ichLim)
		return !m_fOverflow;

	// Check that the chars to insert do not overlap the buffer.
	Assert((byte *)(prgych + cychSrc) <= (byte *)(m_rgch + ichMin) ||
		(byte *)prgych >= (byte *)(this + 1));

	// Get the size of text to insert.
	int cxchDst = ConvertText(prgych, cychSrc, (XChar *)NULL, 0);

	// Check that the replacement fits.
	if ((uint)(ichMin + cxchDst) <= (uint)kcchMax)
	{
		// Check whether the right-most substring may need to be truncated.
		if (cchCur + cxchDst - (ichLim - ichMin) > kcchMax)
		{
			m_fOverflow = true;
			// Truncate the current text by adjusting cchCur.
			cchCur = kcchMax - cxchDst + ichLim - ichMin;
		}
		// Move the right-most characters.
		if (ichLim < cchCur && ichLim != ichMin + cxchDst)
			MoveItems(m_rgch + ichLim, m_rgch + ichMin + cxchDst, cchCur - ichLim);

		// Copy and convert the text.
		if (cxchDst > 0)
			ConvertText(prgych, cychSrc, m_rgch + ichMin, cxchDst);
		_SetLen(cchCur + cxchDst - (ichLim - ichMin));
	}
	else // The text to be inserted will itself overflow the buffer.
	{
		m_fOverflow = true;
		// Truncate the text being inserted.
		cychSrc = CychFitConvertedText(prgych, cychSrc, kcchMax - ichMin);
		cxchDst = ConvertText(prgych, cychSrc, m_rgch + ichMin, kcchMax - ichMin);
		_SetLen(ichMin + cxchDst);
	}

	AssertObj(this);
	return !m_fOverflow;
}


/*----------------------------------------------------------------------------------------------
	Replace the range of characters [ichMin, ichLim) with cchIns instances of the character
	chIns of the same type. If there is an overflow, copy the characters that fit and set the
	overflow flag, m_fOverflow.
----------------------------------------------------------------------------------------------*/
template<typename XChar, int kcchMax>
	bool StrBaseBuf<XChar, kcchMax>::ReplaceFill(int ichMin, int ichLim,
		const XChar chIns, int cchIns)
{
	AssertObj(this);
	Assert(0 <= ichMin && (uint)ichMin <= (uint)ichLim &&
		((uint)ichLim <= (uint)Length() || m_fOverflow));
	Assert(0 <= cchIns);

	// Limit ichMin and ichLim to the length.
	int cchCur = Length();
	if (ichMin > cchCur)
		ichMin = cchCur;
	if (ichLim > cchCur)
		ichLim = cchCur;

	// Return if there are no characters to insert and no characters to delete.
	if (!cchIns && ichMin == ichLim)
		return !m_fOverflow;

	// Check that the replacement fits.
	if ((uint)(ichMin + cchIns) <= (uint)kcchMax)
	{
		// Check whether the right-most substring may need to be truncated.
		if (cchCur + cchIns - (ichLim - ichMin) > kcchMax)
		{
			m_fOverflow = true;
			// Truncate the current text by adjusting cchCur.
			cchCur = kcchMax - cchIns + ichLim - ichMin;
		}
		// Move the right-most characters.
		if (ichLim < cchCur && ichLim != ichMin + cchIns)
			MoveItems(m_rgch + ichLim, m_rgch + ichMin + cchIns, cchCur - ichLim);

		// Insert the character chIns repeatedly for cchIns positions.
		if (cchIns > 0)
		{
			int ichFill = ichMin + cchIns;
			for (int i = ichMin; i < ichFill; i++)
				m_rgch[i] = chIns;
		}
		_SetLen(cchCur + cchIns - (ichLim - ichMin));
	}
	else // The text to be inserted will itself overflow the buffer.
	{
		m_fOverflow = true;
		// Truncate the text being inserted by adjusting cchIns.
		int ichFill = kcchMax - ichMin;
		for (int i = ichMin; i < ichFill; i++)
			m_rgch[i] = chIns;
		_SetLen(kcchMax);
	}

	AssertObj(this);
	return !m_fOverflow;
}


/*----------------------------------------------------------------------------------------------
	Replace the buffer for this StrBaseBuf<> with a new string constructed by formatting the
	string template (prgchFmt, cchFmt). Set m_fOverflow to true if FormatText failed. See
	FormatText.

	@h3{Parameters}
	@code{
		prgchFmt -- string, of the same type of characters as this StrBaseBuf<>, used as the
					template.
		cchFmt -- number of characters in the template string.
		vaArgList -- additional parameters used with the template string.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar, int kcchMax>
	bool StrBaseBuf<XChar, kcchMax>::FormatCore(const XChar * prgchFmt, int cchFmt,
		va_list vaArgList)
{
	AssertArray(prgchFmt, cchFmt);

	_Clear();
	// REVIEW Development(DarrellZ): Is this right?
	try
	{
		FormatText((CharDefns<XChar>::PfnWriteChars)&FormatCallback, this,
			prgchFmt, cchFmt, vaArgList);
	}
	catch (...)
	{
		m_fOverflow = true;
	}

	return !m_fOverflow;
}


/*----------------------------------------------------------------------------------------------
	Append, to the buffer of this StrBaseBuf<>, a new string constructed by formatting the
	string template (prgchFmt, cchFmt). Set m_fOverflow to true if FormatText failed. See
	FormatText.

	@h3{Parameters}
	@code{
		prgchFmt -- string, of the same type of characters as this StrBaseBuf<>, used as the
					template.
		cchFmt -- number of characters in the template string.
		vaArgList -- additional parameters used with the template string.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar, int kcchMax>
	bool StrBaseBuf<XChar, kcchMax>::FormatAppendCore(const XChar * prgchFmt, int cchFmt,
		va_list vaArgList)
{
	AssertArray(prgchFmt, cchFmt);

	// REVIEW Development(DarrellZ): Is this right?
	try
	{
		FormatText((CharDefns<XChar>::PfnWriteChars)&FormatCallback, this,
			prgchFmt, cchFmt, vaArgList);
	}
	catch (...)
	{
		m_fOverflow = true;
	}

	return !m_fOverflow;
}


/*----------------------------------------------------------------------------------------------
	Callback for formatting text to a StrBaseBuf<>. See FormatText.

	@h3{Parameters}
	@code{
		pv -- pointer to a stream or some type of string object. It is supplied by the
			caller of FormatText and passed on as the first argument whenever pfnWrite is
			called.
		prgch -- string used as the template.
		cch -- number of characters in the template string.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar, int kcchMax>
	void StrBaseBuf<XChar, kcchMax>::FormatCallback(void * pv, const XChar * prgch, int cch)
{
	StrBaseBuf<XChar, kcchMax> * pstbb = (StrBaseBuf<XChar, kcchMax> *)pv;
	AssertObj(pstbb);
	AssertArray(prgch, cch);

	if (!pstbb->Append(prgch, cch))
		ThrowHr(WarnHr(E_FAIL));
}

//:Ignore
//Dbw1 causes severe problems, for example, Set<StrAnsiBufSmall> cannot add data reliably.
///*--------------------------------------------------------------------------------------------
//	Return an actual character pointer of a description of the current properties.
//	This is meant for use in the debugger, to be displayed in the Output window.
//--------------------------------------------------------------------------------------------*/
//#ifdef DEBUG
//// bammel: Can't get the following to work, it gives unresolved external during link:
////template<typename XChar>
////    OLECHAR * StrBaseBufCore<XChar>::Dbw1::Watch() ...
//
//OLECHAR * StrBaseBufCore<wchar>::Dbw1::Watch()
//{
//	int iv, cv = m_pstrbase->m_cb / sizeof(m_pstrbase->m_rgch[0]);
//
//	m_nSerial++;
//	Output("#%d %d \"", m_nSerial, cv);
//	for (iv = 0; iv < cv; iv++)
//		Output("%lc", (wchar)m_pstrbase->m_rgch[iv]);
//	Output("\"\n");
//
//	return L"See Debugger Output window";
//}
//
//OLECHAR * StrBaseBufCore<schar>::Dbw1::Watch()
//{
//	int iv, cv = m_pstrbase->m_cb / sizeof(m_pstrbase->m_rgch[0]);
//
//	m_nSerial++;
//	Output("#%d %d \"", m_nSerial, cv);
//	for (iv = 0; iv < cv; iv++)
//		Output("%lc", (wchar)m_pstrbase->m_rgch[iv]);
//	Output("\"\n");
//
//	return L"See Debugger Output window";
//}
//#endif //DEBUG
//:End Ignore

//:Associate with "Generic Text Manipulation Functions".
//:>********************************************************************************************
//:>	Mapping Unicode characters to uppercase.
//:>********************************************************************************************

static const wchar kchMinUpper1 = 0x0061;
static const wchar kchLimUpper1 = 0x0293;

static const wchar g_mpchchUpper1[kchLimUpper1 - kchMinUpper1] =
{
	0x0041, 0x0042, 0x0043, 0x0044, 0x0045, 0x0046, 0x0047, 0x0048, 0x0049, 0x004a,
	0x004b, 0x004c, 0x004d, 0x004e, 0x004f, 0x0050, 0x0051, 0x0052, 0x0053, 0x0054,
	0x0055, 0x0056, 0x0057, 0x0058, 0x0059, 0x005a, 0x007b, 0x007c, 0x007d, 0x007e,
	0x007f, 0x0080, 0x0081, 0x0082, 0x0083, 0x0084, 0x0085, 0x0086, 0x0087, 0x0088,
	0x0089, 0x008a, 0x008b, 0x008c, 0x008d, 0x008e, 0x008f, 0x0090, 0x0091, 0x0092,
	0x0093, 0x0094, 0x0095, 0x0096, 0x0097, 0x0098, 0x0099, 0x009a, 0x009b, 0x009c,
	0x009d, 0x009e, 0x009f, 0x00a0, 0x00a1, 0x00a2, 0x00a3, 0x00a4, 0x00a5, 0x00a6,
	0x00a7, 0x00a8, 0x00a9, 0x00aa, 0x00ab, 0x00ac, 0x00ad, 0x00ae, 0x00af, 0x00b0,
	0x00b1, 0x00b2, 0x00b3, 0x00b4, 0x00b5, 0x00b6, 0x00b7, 0x00b8, 0x00b9, 0x00ba,
	0x00bb, 0x00bc, 0x00bd, 0x00be, 0x00bf, 0x00c0, 0x00c1, 0x00c2, 0x00c3, 0x00c4,
	0x00c5, 0x00c6, 0x00c7, 0x00c8, 0x00c9, 0x00ca, 0x00cb, 0x00cc, 0x00cd, 0x00ce,
	0x00cf, 0x00d0, 0x00d1, 0x00d2, 0x00d3, 0x00d4, 0x00d5, 0x00d6, 0x00d7, 0x00d8,
	0x00d9, 0x00da, 0x00db, 0x00dc, 0x00dd, 0x00de, 0x00df, 0x00c0, 0x00c1, 0x00c2,
	0x00c3, 0x00c4, 0x00c5, 0x00c6, 0x00c7, 0x00c8, 0x00c9, 0x00ca, 0x00cb, 0x00cc,
	0x00cd, 0x00ce, 0x00cf, 0x00d0, 0x00d1, 0x00d2, 0x00d3, 0x00d4, 0x00d5, 0x00d6,
	0x00f7, 0x00d8, 0x00d9, 0x00da, 0x00db, 0x00dc, 0x00dd, 0x00de, 0x0178, 0x0100,
	0x0100, 0x0102, 0x0102, 0x0104, 0x0104, 0x0106, 0x0106, 0x0108, 0x0108, 0x010a,
	0x010a, 0x010c, 0x010c, 0x010e, 0x010e, 0x0110, 0x0110, 0x0112, 0x0112, 0x0114,
	0x0114, 0x0116, 0x0116, 0x0118, 0x0118, 0x011a, 0x011a, 0x011c, 0x011c, 0x011e,
	0x011e, 0x0120, 0x0120, 0x0122, 0x0122, 0x0124, 0x0124, 0x0126, 0x0126, 0x0128,
	0x0128, 0x012a, 0x012a, 0x012c, 0x012c, 0x012e, 0x012e, 0x0130, 0x0049, 0x0132,
	0x0132, 0x0134, 0x0134, 0x0136, 0x0136, 0x0138, 0x0139, 0x0139, 0x013b, 0x013b,
	0x013d, 0x013d, 0x013f, 0x013f, 0x0141, 0x0141, 0x0143, 0x0143, 0x0145, 0x0145,
	0x0147, 0x0147, 0x0149, 0x014a, 0x014a, 0x014c, 0x014c, 0x014e, 0x014e, 0x0150,
	0x0150, 0x0152, 0x0152, 0x0154, 0x0154, 0x0156, 0x0156, 0x0158, 0x0158, 0x015a,
	0x015a, 0x015c, 0x015c, 0x015e, 0x015e, 0x0160, 0x0160, 0x0162, 0x0162, 0x0164,
	0x0164, 0x0166, 0x0166, 0x0168, 0x0168, 0x016a, 0x016a, 0x016c, 0x016c, 0x016e,
	0x016e, 0x0170, 0x0170, 0x0172, 0x0172, 0x0174, 0x0174, 0x0176, 0x0176, 0x0178,
	0x0179, 0x0179, 0x017b, 0x017b, 0x017d, 0x017d, 0x0053, 0x0180, 0x0181, 0x0182,
	0x0182, 0x0184, 0x0184, 0x0186, 0x0187, 0x0187, 0x0189, 0x018a, 0x018b, 0x018b,
	0x018d, 0x018e, 0x018f, 0x0190, 0x0191, 0x0191, 0x0193, 0x0194, 0x0195, 0x0196,
	0x0197, 0x0198, 0x0198, 0x019a, 0x019b, 0x019c, 0x019d, 0x019e, 0x019f, 0x01a0,
	0x01a0, 0x01a2, 0x01a2, 0x01a4, 0x01a4, 0x01a6, 0x01a7, 0x01a7, 0x01a9, 0x01aa,
	0x01ab, 0x01ac, 0x01ac, 0x01ae, 0x01af, 0x01af, 0x01b1, 0x01b2, 0x01b3, 0x01b3,
	0x01b5, 0x01b5, 0x01b7, 0x01b8, 0x01b8, 0x01ba, 0x01bb, 0x01bc, 0x01bc, 0x01be,
	0x01bf, 0x01c0, 0x01c1, 0x01c2, 0x01c3, 0x01c4, 0x01c4, 0x01c4, 0x01c7, 0x01c7,
	0x01c7, 0x01ca, 0x01ca, 0x01ca, 0x01cd, 0x01cd, 0x01cf, 0x01cf, 0x01d1, 0x01d1,
	0x01d3, 0x01d3, 0x01d5, 0x01d5, 0x01d7, 0x01d7, 0x01d9, 0x01d9, 0x01db, 0x01db,
	0x01dd, 0x01de, 0x01de, 0x01e0, 0x01e0, 0x01e2, 0x01e2, 0x01e4, 0x01e4, 0x01e6,
	0x01e6, 0x01e8, 0x01e8, 0x01ea, 0x01ea, 0x01ec, 0x01ec, 0x01ee, 0x01ee, 0x01f0,
	0x01f1, 0x01f1, 0x01f1, 0x01f4, 0x01f4, 0x01f6, 0x01f7, 0x01f8, 0x01f9, 0x01fa,
	0x01fa, 0x01fc, 0x01fc, 0x01fe, 0x01fe, 0x0200, 0x0200, 0x0202, 0x0202, 0x0204,
	0x0204, 0x0206, 0x0206, 0x0208, 0x0208, 0x020a, 0x020a, 0x020c, 0x020c, 0x020e,
	0x020e, 0x0210, 0x0210, 0x0212, 0x0212, 0x0214, 0x0214, 0x0216, 0x0216, 0x0218,
	0x0219, 0x021a, 0x021b, 0x021c, 0x021d, 0x021e, 0x021f, 0x0220, 0x0221, 0x0222,
	0x0223, 0x0224, 0x0225, 0x0226, 0x0227, 0x0228, 0x0229, 0x022a, 0x022b, 0x022c,
	0x022d, 0x022e, 0x022f, 0x0230, 0x0231, 0x0232, 0x0233, 0x0234, 0x0235, 0x0236,
	0x0237, 0x0238, 0x0239, 0x023a, 0x023b, 0x023c, 0x023d, 0x023e, 0x023f, 0x0240,
	0x0241, 0x0242, 0x0243, 0x0244, 0x0245, 0x0246, 0x0247, 0x0248, 0x0249, 0x024a,
	0x024b, 0x024c, 0x024d, 0x024e, 0x024f, 0x0250, 0x0251, 0x0252, 0x0181, 0x0186,
	0x0255, 0x0189, 0x018a, 0x018e, 0x018f, 0x025a, 0x0190, 0x025c, 0x025d, 0x025e,
	0x025f, 0x0193, 0x0261, 0x0262, 0x0194, 0x0264, 0x0265, 0x0266, 0x0267, 0x0197,
	0x0196, 0x026a, 0x026b, 0x026c, 0x026d, 0x026e, 0x019c, 0x0270, 0x0271, 0x019d,
	0x0273, 0x0274, 0x0275, 0x0276, 0x0277, 0x0278, 0x0279, 0x027a, 0x027b, 0x027c,
	0x027d, 0x027e, 0x027f, 0x0280, 0x0281, 0x0282, 0x01a9, 0x0284, 0x0285, 0x0286,
	0x0287, 0x01ae, 0x0289, 0x01b1, 0x01b2, 0x028c, 0x028d, 0x028e, 0x028f, 0x0290,
	0x0291, 0x01b7
};


static const wchar kchMinUpper2 = 0x03ac;
static const wchar kchLimUpper2 = 0x0587;

static const wchar g_mpchchUpper2[kchLimUpper2 - kchMinUpper2] =
{
	0x0386, 0x0388, 0x0389, 0x038a, 0x03b0, 0x0391, 0x0392, 0x0393, 0x0394, 0x0395,
	0x0396, 0x0397, 0x0398, 0x0399, 0x039a, 0x039b, 0x039c, 0x039d, 0x039e, 0x039f,
	0x03a0, 0x03a1, 0x03c2, 0x03a3, 0x03a4, 0x03a5, 0x03a6, 0x03a7, 0x03a8, 0x03a9,
	0x03aa, 0x03ab, 0x038c, 0x038e, 0x038f, 0x03cf, 0x0392, 0x0398, 0x03d2, 0x03d3,
	0x03d4, 0x03a6, 0x03a0, 0x03d7, 0x03d8, 0x03d9, 0x03da, 0x03db, 0x03dc, 0x03dd,
	0x03de, 0x03df, 0x03e0, 0x03e1, 0x03e2, 0x03e2, 0x03e4, 0x03e4, 0x03e6, 0x03e6,
	0x03e8, 0x03e8, 0x03ea, 0x03ea, 0x03ec, 0x03ec, 0x03ee, 0x03ee, 0x039a, 0x03a1,
	0x03f2, 0x03f3, 0x03f4, 0x03f5, 0x03f6, 0x03f7, 0x03f8, 0x03f9, 0x03fa, 0x03fb,
	0x03fc, 0x03fd, 0x03fe, 0x03ff, 0x0400, 0x0401, 0x0402, 0x0403, 0x0404, 0x0405,
	0x0406, 0x0407, 0x0408, 0x0409, 0x040a, 0x040b, 0x040c, 0x040d, 0x040e, 0x040f,
	0x0410, 0x0411, 0x0412, 0x0413, 0x0414, 0x0415, 0x0416, 0x0417, 0x0418, 0x0419,
	0x041a, 0x041b, 0x041c, 0x041d, 0x041e, 0x041f, 0x0420, 0x0421, 0x0422, 0x0423,
	0x0424, 0x0425, 0x0426, 0x0427, 0x0428, 0x0429, 0x042a, 0x042b, 0x042c, 0x042d,
	0x042e, 0x042f, 0x0410, 0x0411, 0x0412, 0x0413, 0x0414, 0x0415, 0x0416, 0x0417,
	0x0418, 0x0419, 0x041a, 0x041b, 0x041c, 0x041d, 0x041e, 0x041f, 0x0420, 0x0421,
	0x0422, 0x0423, 0x0424, 0x0425, 0x0426, 0x0427, 0x0428, 0x0429, 0x042a, 0x042b,
	0x042c, 0x042d, 0x042e, 0x042f, 0x0450, 0x0401, 0x0402, 0x0403, 0x0404, 0x0405,
	0x0406, 0x0407, 0x0408, 0x0409, 0x040a, 0x040b, 0x040c, 0x045d, 0x040e, 0x040f,
	0x0460, 0x0460, 0x0462, 0x0462, 0x0464, 0x0464, 0x0466, 0x0466, 0x0468, 0x0468,
	0x046a, 0x046a, 0x046c, 0x046c, 0x046e, 0x046e, 0x0470, 0x0470, 0x0472, 0x0472,
	0x0474, 0x0474, 0x0476, 0x0476, 0x0478, 0x0478, 0x047a, 0x047a, 0x047c, 0x047c,
	0x047e, 0x047e, 0x0480, 0x0480, 0x0482, 0x0483, 0x0484, 0x0485, 0x0486, 0x0487,
	0x0488, 0x0489, 0x048a, 0x048b, 0x048c, 0x048d, 0x048e, 0x048f, 0x0490, 0x0490,
	0x0492, 0x0492, 0x0494, 0x0494, 0x0496, 0x0496, 0x0498, 0x0498, 0x049a, 0x049a,
	0x049c, 0x049c, 0x049e, 0x049e, 0x04a0, 0x04a0, 0x04a2, 0x04a2, 0x04a4, 0x04a4,
	0x04a6, 0x04a6, 0x04a8, 0x04a8, 0x04aa, 0x04aa, 0x04ac, 0x04ac, 0x04ae, 0x04ae,
	0x04b0, 0x04b0, 0x04b2, 0x04b2, 0x04b4, 0x04b4, 0x04b6, 0x04b6, 0x04b8, 0x04b8,
	0x04ba, 0x04ba, 0x04bc, 0x04bc, 0x04be, 0x04be, 0x04c0, 0x04c1, 0x04c1, 0x04c3,
	0x04c3, 0x04c5, 0x04c6, 0x04c7, 0x04c7, 0x04c9, 0x04ca, 0x04cb, 0x04cb, 0x04cd,
	0x04ce, 0x04cf, 0x04d0, 0x04d0, 0x04d2, 0x04d2, 0x04d4, 0x04d4, 0x04d6, 0x04d6,
	0x04d8, 0x04d8, 0x04da, 0x04da, 0x04dc, 0x04dc, 0x04de, 0x04de, 0x04e0, 0x04e0,
	0x04e2, 0x04e2, 0x04e4, 0x04e4, 0x04e6, 0x04e6, 0x04e8, 0x04e8, 0x04ea, 0x04ea,
	0x04ec, 0x04ed, 0x04ee, 0x04ee, 0x04f0, 0x04f0, 0x04f2, 0x04f2, 0x04f4, 0x04f4,
	0x04f6, 0x04f7, 0x04f8, 0x04f8, 0x04fa, 0x04fb, 0x04fc, 0x04fd, 0x04fe, 0x04ff,
	0x0500, 0x0501, 0x0502, 0x0503, 0x0504, 0x0505, 0x0506, 0x0507, 0x0508, 0x0509,
	0x050a, 0x050b, 0x050c, 0x050d, 0x050e, 0x050f, 0x0510, 0x0511, 0x0512, 0x0513,
	0x0514, 0x0515, 0x0516, 0x0517, 0x0518, 0x0519, 0x051a, 0x051b, 0x051c, 0x051d,
	0x051e, 0x051f, 0x0520, 0x0521, 0x0522, 0x0523, 0x0524, 0x0525, 0x0526, 0x0527,
	0x0528, 0x0529, 0x052a, 0x052b, 0x052c, 0x052d, 0x052e, 0x052f, 0x0530, 0x0531,
	0x0532, 0x0533, 0x0534, 0x0535, 0x0536, 0x0537, 0x0538, 0x0539, 0x053a, 0x053b,
	0x053c, 0x053d, 0x053e, 0x053f, 0x0540, 0x0541, 0x0542, 0x0543, 0x0544, 0x0545,
	0x0546, 0x0547, 0x0548, 0x0549, 0x054a, 0x054b, 0x054c, 0x054d, 0x054e, 0x054f,
	0x0550, 0x0551, 0x0552, 0x0553, 0x0554, 0x0555, 0x0556, 0x0557, 0x0558, 0x0559,
	0x055a, 0x055b, 0x055c, 0x055d, 0x055e, 0x055f, 0x0560, 0x0531, 0x0532, 0x0533,
	0x0534, 0x0535, 0x0536, 0x0537, 0x0538, 0x0539, 0x053a, 0x053b, 0x053c, 0x053d,
	0x053e, 0x053f, 0x0540, 0x0541, 0x0542, 0x0543, 0x0544, 0x0545, 0x0546, 0x0547,
	0x0548, 0x0549, 0x054a, 0x054b, 0x054c, 0x054d, 0x054e, 0x054f, 0x0550, 0x0551,
	0x0552, 0x0553, 0x0554, 0x0555, 0x0556
};


static const wchar kchMinUpper3 = 0x1e01;
static const wchar kchLimUpper3 = 0x1ff4;

static const wchar g_mpchchUpper3[kchLimUpper3 - kchMinUpper3] =
{
	0x1e00, 0x1e02, 0x1e02, 0x1e04, 0x1e04, 0x1e06, 0x1e06, 0x1e08, 0x1e08, 0x1e0a,
	0x1e0a, 0x1e0c, 0x1e0c, 0x1e0e, 0x1e0e, 0x1e10, 0x1e10, 0x1e12, 0x1e12, 0x1e14,
	0x1e14, 0x1e16, 0x1e16, 0x1e18, 0x1e18, 0x1e1a, 0x1e1a, 0x1e1c, 0x1e1c, 0x1e1e,
	0x1e1e, 0x1e20, 0x1e20, 0x1e22, 0x1e22, 0x1e24, 0x1e24, 0x1e26, 0x1e26, 0x1e28,
	0x1e28, 0x1e2a, 0x1e2a, 0x1e2c, 0x1e2c, 0x1e2e, 0x1e2e, 0x1e30, 0x1e30, 0x1e32,
	0x1e32, 0x1e34, 0x1e34, 0x1e36, 0x1e36, 0x1e38, 0x1e38, 0x1e3a, 0x1e3a, 0x1e3c,
	0x1e3c, 0x1e3e, 0x1e3e, 0x1e40, 0x1e40, 0x1e42, 0x1e42, 0x1e44, 0x1e44, 0x1e46,
	0x1e46, 0x1e48, 0x1e48, 0x1e4a, 0x1e4a, 0x1e4c, 0x1e4c, 0x1e4e, 0x1e4e, 0x1e50,
	0x1e50, 0x1e52, 0x1e52, 0x1e54, 0x1e54, 0x1e56, 0x1e56, 0x1e58, 0x1e58, 0x1e5a,
	0x1e5a, 0x1e5c, 0x1e5c, 0x1e5e, 0x1e5e, 0x1e60, 0x1e60, 0x1e62, 0x1e62, 0x1e64,
	0x1e64, 0x1e66, 0x1e66, 0x1e68, 0x1e68, 0x1e6a, 0x1e6a, 0x1e6c, 0x1e6c, 0x1e6e,
	0x1e6e, 0x1e70, 0x1e70, 0x1e72, 0x1e72, 0x1e74, 0x1e74, 0x1e76, 0x1e76, 0x1e78,
	0x1e78, 0x1e7a, 0x1e7a, 0x1e7c, 0x1e7c, 0x1e7e, 0x1e7e, 0x1e80, 0x1e80, 0x1e82,
	0x1e82, 0x1e84, 0x1e84, 0x1e86, 0x1e86, 0x1e88, 0x1e88, 0x1e8a, 0x1e8a, 0x1e8c,
	0x1e8c, 0x1e8e, 0x1e8e, 0x1e90, 0x1e90, 0x1e92, 0x1e92, 0x1e94, 0x1e94, 0x1e96,
	0x1e97, 0x1e98, 0x1e99, 0x1e9a, 0x1e9b, 0x1e9c, 0x1e9d, 0x1e9e, 0x1e9f, 0x1ea0,
	0x1ea0, 0x1ea2, 0x1ea2, 0x1ea4, 0x1ea4, 0x1ea6, 0x1ea6, 0x1ea8, 0x1ea8, 0x1eaa,
	0x1eaa, 0x1eac, 0x1eac, 0x1eae, 0x1eae, 0x1eb0, 0x1eb0, 0x1eb2, 0x1eb2, 0x1eb4,
	0x1eb4, 0x1eb6, 0x1eb6, 0x1eb8, 0x1eb8, 0x1eba, 0x1eba, 0x1ebc, 0x1ebc, 0x1ebe,
	0x1ebe, 0x1ec0, 0x1ec0, 0x1ec2, 0x1ec2, 0x1ec4, 0x1ec4, 0x1ec6, 0x1ec6, 0x1ec8,
	0x1ec8, 0x1eca, 0x1eca, 0x1ecc, 0x1ecc, 0x1ece, 0x1ece, 0x1ed0, 0x1ed0, 0x1ed2,
	0x1ed2, 0x1ed4, 0x1ed4, 0x1ed6, 0x1ed6, 0x1ed8, 0x1ed8, 0x1eda, 0x1eda, 0x1edc,
	0x1edc, 0x1ede, 0x1ede, 0x1ee0, 0x1ee0, 0x1ee2, 0x1ee2, 0x1ee4, 0x1ee4, 0x1ee6,
	0x1ee6, 0x1ee8, 0x1ee8, 0x1eea, 0x1eea, 0x1eec, 0x1eec, 0x1eee, 0x1eee, 0x1ef0,
	0x1ef0, 0x1ef2, 0x1ef2, 0x1ef4, 0x1ef4, 0x1ef6, 0x1ef6, 0x1ef8, 0x1ef8, 0x1efa,
	0x1efb, 0x1efc, 0x1efd, 0x1efe, 0x1eff, 0x1f08, 0x1f09, 0x1f0a, 0x1f0b, 0x1f0c,
	0x1f0d, 0x1f0e, 0x1f0f, 0x1f08, 0x1f09, 0x1f0a, 0x1f0b, 0x1f0c, 0x1f0d, 0x1f0e,
	0x1f0f, 0x1f18, 0x1f19, 0x1f1a, 0x1f1b, 0x1f1c, 0x1f1d, 0x1f16, 0x1f17, 0x1f18,
	0x1f19, 0x1f1a, 0x1f1b, 0x1f1c, 0x1f1d, 0x1f1e, 0x1f1f, 0x1f28, 0x1f29, 0x1f2a,
	0x1f2b, 0x1f2c, 0x1f2d, 0x1f2e, 0x1f2f, 0x1f28, 0x1f29, 0x1f2a, 0x1f2b, 0x1f2c,
	0x1f2d, 0x1f2e, 0x1f2f, 0x1f38, 0x1f39, 0x1f3a, 0x1f3b, 0x1f3c, 0x1f3d, 0x1f3e,
	0x1f3f, 0x1f38, 0x1f39, 0x1f3a, 0x1f3b, 0x1f3c, 0x1f3d, 0x1f3e, 0x1f3f, 0x1f48,
	0x1f49, 0x1f4a, 0x1f4b, 0x1f4c, 0x1f4d, 0x1f46, 0x1f47, 0x1f48, 0x1f49, 0x1f4a,
	0x1f4b, 0x1f4c, 0x1f4d, 0x1f4e, 0x1f4f, 0x1f50, 0x1f59, 0x1f52, 0x1f5b, 0x1f54,
	0x1f5d, 0x1f56, 0x1f5f, 0x1f58, 0x1f59, 0x1f5a, 0x1f5b, 0x1f5c, 0x1f5d, 0x1f5e,
	0x1f5f, 0x1f68, 0x1f69, 0x1f6a, 0x1f6b, 0x1f6c, 0x1f6d, 0x1f6e, 0x1f6f, 0x1f68,
	0x1f69, 0x1f6a, 0x1f6b, 0x1f6c, 0x1f6d, 0x1f6e, 0x1f6f, 0x1fba, 0x1fbb, 0x1fc8,
	0x1fc9, 0x1fca, 0x1fcb, 0x1fda, 0x1fdb, 0x1ff8, 0x1ff9, 0x1fea, 0x1feb, 0x1ffa,
	0x1ffb, 0x1f7e, 0x1f7f, 0x1f88, 0x1f89, 0x1f8a, 0x1f8b, 0x1f8c, 0x1f8d, 0x1f8e,
	0x1f8f, 0x1f88, 0x1f89, 0x1f8a, 0x1f8b, 0x1f8c, 0x1f8d, 0x1f8e, 0x1f8f, 0x1f98,
	0x1f99, 0x1f9a, 0x1f9b, 0x1f9c, 0x1f9d, 0x1f9e, 0x1f9f, 0x1f98, 0x1f99, 0x1f9a,
	0x1f9b, 0x1f9c, 0x1f9d, 0x1f9e, 0x1f9f, 0x1fa8, 0x1fa9, 0x1faa, 0x1fab, 0x1fac,
	0x1fad, 0x1fae, 0x1faf, 0x1fa8, 0x1fa9, 0x1faa, 0x1fab, 0x1fac, 0x1fad, 0x1fae,
	0x1faf, 0x1fb8, 0x1fb9, 0x1fb2, 0x1fbc, 0x1fb4, 0x1fb5, 0x1fb6, 0x1fb7, 0x1fb8,
	0x1fb9, 0x1fba, 0x1fbb, 0x1fbc, 0x1fbd, 0x1fbe, 0x1fbf, 0x1fc0, 0x1fc1, 0x1fc2,
	0x1fcc, 0x1fc4, 0x1fc5, 0x1fc6, 0x1fc7, 0x1fc8, 0x1fc9, 0x1fca, 0x1fcb, 0x1fcc,
	0x1fcd, 0x1fce, 0x1fcf, 0x1fd8, 0x1fd9, 0x1fd2, 0x1fd3, 0x1fd4, 0x1fd5, 0x1fd6,
	0x1fd7, 0x1fd8, 0x1fd9, 0x1fda, 0x1fdb, 0x1fdc, 0x1fdd, 0x1fde, 0x1fdf, 0x1fe8,
	0x1fe9, 0x1fe2, 0x1fe3, 0x1fe4, 0x1fec, 0x1fe6, 0x1fe7, 0x1fe8, 0x1fe9, 0x1fea,
	0x1feb, 0x1fec, 0x1fed, 0x1fee, 0x1fef, 0x1ff0, 0x1ff1, 0x1ff2, 0x1ffc
};


static const wchar kchMinUpper4 = 0x2170;
static const wchar kchLimUpper4 = 0x2180;
static const wchar kdchUpper4 = (wchar)-16;


static const wchar kchMinUpper5 = 0x24d0;
static const wchar kchLimUpper5 = 0x24ea;
static const wchar kdchUpper5 = (wchar)-26;


static const wchar kchMinUpper6 = 0xff41;
static const wchar kchLimUpper6 = 0xff5b;
static const wchar kdchUpper6 = (wchar)-32;


/*----------------------------------------------------------------------------------------------
	This unicode case mapping function converts cch wide (16-bit) characters in prgch to
	upper case.
----------------------------------------------------------------------------------------------*/
void ToUpper(wchar * prgch, int cch)
{
	Assert(cch >= 0);
	AssertArray(prgch, cch);

	wchar ch;
	wchar *pch;
	wchar *pchLim = prgch + cch;

	for (pch = prgch; pch < pchLim; ++pch)
	{
		ch = *pch;

		// Try the common case first.
		if (*pch < kchLimUpper1)
		{
			if (*pch >= kchMinUpper1)
				*pch = g_mpchchUpper1[*pch - kchMinUpper1];
		}
		else if (*pch < kchLimUpper4)
		{
			if (*pch < kchMinUpper3)
			{
				if (*pch < kchLimUpper2)
				{
					if (*pch >= kchMinUpper2)
						*pch = g_mpchchUpper2[*pch - kchMinUpper2];
				}
			}
			else if (*pch < kchLimUpper3)
				*pch = g_mpchchUpper3[*pch - kchMinUpper3];
			else if (*pch >= kchMinUpper4)
				*pch += kdchUpper4;
		}
		else if (*pch < kchMinUpper6)
		{
			if (*pch < kchLimUpper5)
			{
				if (*pch >= kchMinUpper5)
					*pch += kdchUpper5;
			}
		}
		else if (*pch < kchLimUpper6)
			*pch += kdchUpper6;
	}
}


/*----------------------------------------------------------------------------------------------
	This unicode case mapping function converts the wide (16-bit) character ch to upper case.
----------------------------------------------------------------------------------------------*/
wchar ToUpper(wchar ch)
{
	// Try the common case first.
	if (ch < kchLimUpper1)
	{
		if (ch >= kchMinUpper1)
			return g_mpchchUpper1[ch - kchMinUpper1];
	}
	else if (ch < kchLimUpper4)
	{
		if (ch < kchMinUpper3)
		{
			if (ch < kchLimUpper2)
			{
				if (ch >= kchMinUpper2)
					return g_mpchchUpper2[ch - kchMinUpper2];
			}
		}
		else if (ch < kchLimUpper3)
			return g_mpchchUpper3[ch - kchMinUpper3];
		else if (ch >= kchMinUpper4)
			return (wchar)(ch + kdchUpper4);
	}
	else if (ch < kchMinUpper6)
	{
		if (ch < kchLimUpper5)
		{
			if (ch >= kchMinUpper5)
				return (wchar)(ch + kdchUpper5);
		}
	}
	else if (ch < kchLimUpper6)
		return (wchar)(ch + kdchUpper6);

	return ch;
}


//:>********************************************************************************************
//:>	Mapping Unicode characters to lower case.
//:>********************************************************************************************

static const wchar kchMinLower1 = 0x0041;
static const wchar kchLimLower1 = 0x0217;

static const wchar g_mpchchLower1[kchLimLower1 - kchMinLower1] =
{
	0x0061, 0x0062, 0x0063, 0x0064, 0x0065, 0x0066, 0x0067, 0x0068, 0x0069, 0x006a,
	0x006b, 0x006c, 0x006d, 0x006e, 0x006f, 0x0070, 0x0071, 0x0072, 0x0073, 0x0074,
	0x0075, 0x0076, 0x0077, 0x0078, 0x0079, 0x007a, 0x005b, 0x005c, 0x005d, 0x005e,
	0x005f, 0x0060, 0x0061, 0x0062, 0x0063, 0x0064, 0x0065, 0x0066, 0x0067, 0x0068,
	0x0069, 0x006a, 0x006b, 0x006c, 0x006d, 0x006e, 0x006f, 0x0070, 0x0071, 0x0072,
	0x0073, 0x0074, 0x0075, 0x0076, 0x0077, 0x0078, 0x0079, 0x007a, 0x007b, 0x007c,
	0x007d, 0x007e, 0x007f, 0x0080, 0x0081, 0x0082, 0x0083, 0x0084, 0x0085, 0x0086,
	0x0087, 0x0088, 0x0089, 0x008a, 0x008b, 0x008c, 0x008d, 0x008e, 0x008f, 0x0090,
	0x0091, 0x0092, 0x0093, 0x0094, 0x0095, 0x0096, 0x0097, 0x0098, 0x0099, 0x009a,
	0x009b, 0x009c, 0x009d, 0x009e, 0x009f, 0x00a0, 0x00a1, 0x00a2, 0x00a3, 0x00a4,
	0x00a5, 0x00a6, 0x00a7, 0x00a8, 0x00a9, 0x00aa, 0x00ab, 0x00ac, 0x00ad, 0x00ae,
	0x00af, 0x00b0, 0x00b1, 0x00b2, 0x00b3, 0x00b4, 0x00b5, 0x00b6, 0x00b7, 0x00b8,
	0x00b9, 0x00ba, 0x00bb, 0x00bc, 0x00bd, 0x00be, 0x00bf, 0x00e0, 0x00e1, 0x00e2,
	0x00e3, 0x00e4, 0x00e5, 0x00e6, 0x00e7, 0x00e8, 0x00e9, 0x00ea, 0x00eb, 0x00ec,
	0x00ed, 0x00ee, 0x00ef, 0x00f0, 0x00f1, 0x00f2, 0x00f3, 0x00f4, 0x00f5, 0x00f6,
	0x00d7, 0x00f8, 0x00f9, 0x00fa, 0x00fb, 0x00fc, 0x00fd, 0x00fe, 0x00df, 0x00e0,
	0x00e1, 0x00e2, 0x00e3, 0x00e4, 0x00e5, 0x00e6, 0x00e7, 0x00e8, 0x00e9, 0x00ea,
	0x00eb, 0x00ec, 0x00ed, 0x00ee, 0x00ef, 0x00f0, 0x00f1, 0x00f2, 0x00f3, 0x00f4,
	0x00f5, 0x00f6, 0x00f7, 0x00f8, 0x00f9, 0x00fa, 0x00fb, 0x00fc, 0x00fd, 0x00fe,
	0x00ff, 0x0101, 0x0101, 0x0103, 0x0103, 0x0105, 0x0105, 0x0107, 0x0107, 0x0109,
	0x0109, 0x010b, 0x010b, 0x010d, 0x010d, 0x010f, 0x010f, 0x0111, 0x0111, 0x0113,
	0x0113, 0x0115, 0x0115, 0x0117, 0x0117, 0x0119, 0x0119, 0x011b, 0x011b, 0x011d,
	0x011d, 0x011f, 0x011f, 0x0121, 0x0121, 0x0123, 0x0123, 0x0125, 0x0125, 0x0127,
	0x0127, 0x0129, 0x0129, 0x012b, 0x012b, 0x012d, 0x012d, 0x012f, 0x012f, 0x0069,
	0x0131, 0x0133, 0x0133, 0x0135, 0x0135, 0x0137, 0x0137, 0x0138, 0x013a, 0x013a,
	0x013c, 0x013c, 0x013e, 0x013e, 0x0140, 0x0140, 0x0142, 0x0142, 0x0144, 0x0144,
	0x0146, 0x0146, 0x0148, 0x0148, 0x0149, 0x014b, 0x014b, 0x014d, 0x014d, 0x014f,
	0x014f, 0x0151, 0x0151, 0x0153, 0x0153, 0x0155, 0x0155, 0x0157, 0x0157, 0x0159,
	0x0159, 0x015b, 0x015b, 0x015d, 0x015d, 0x015f, 0x015f, 0x0161, 0x0161, 0x0163,
	0x0163, 0x0165, 0x0165, 0x0167, 0x0167, 0x0169, 0x0169, 0x016b, 0x016b, 0x016d,
	0x016d, 0x016f, 0x016f, 0x0171, 0x0171, 0x0173, 0x0173, 0x0175, 0x0175, 0x0177,
	0x0177, 0x00ff, 0x017a, 0x017a, 0x017c, 0x017c, 0x017e, 0x017e, 0x017f, 0x0180,
	0x0253, 0x0183, 0x0183, 0x0185, 0x0185, 0x0254, 0x0188, 0x0188, 0x0256, 0x0257,
	0x018c, 0x018c, 0x018d, 0x0258, 0x0259, 0x025b, 0x0192, 0x0192, 0x0260, 0x0263,
	0x0195, 0x0269, 0x0268, 0x0199, 0x0199, 0x019a, 0x019b, 0x026f, 0x0272, 0x019e,
	0x019f, 0x01a1, 0x01a1, 0x01a3, 0x01a3, 0x01a5, 0x01a5, 0x01a6, 0x01a8, 0x01a8,
	0x0283, 0x01aa, 0x01ab, 0x01ad, 0x01ad, 0x0288, 0x01b0, 0x01b0, 0x028a, 0x028b,
	0x01b4, 0x01b4, 0x01b6, 0x01b6, 0x0292, 0x01b9, 0x01b9, 0x01ba, 0x01bb, 0x01bd,
	0x01bd, 0x01be, 0x01bf, 0x01c0, 0x01c1, 0x01c2, 0x01c3, 0x01c6, 0x01c6, 0x01c6,
	0x01c9, 0x01c9, 0x01c9, 0x01cc, 0x01cc, 0x01cc, 0x01ce, 0x01ce, 0x01d0, 0x01d0,
	0x01d2, 0x01d2, 0x01d4, 0x01d4, 0x01d6, 0x01d6, 0x01d8, 0x01d8, 0x01da, 0x01da,
	0x01dc, 0x01dc, 0x01dd, 0x01df, 0x01df, 0x01e1, 0x01e1, 0x01e3, 0x01e3, 0x01e5,
	0x01e5, 0x01e7, 0x01e7, 0x01e9, 0x01e9, 0x01eb, 0x01eb, 0x01ed, 0x01ed, 0x01ef,
	0x01ef, 0x01f0, 0x01f3, 0x01f3, 0x01f3, 0x01f5, 0x01f5, 0x01f6, 0x01f7, 0x01f8,
	0x01f9, 0x01fb, 0x01fb, 0x01fd, 0x01fd, 0x01ff, 0x01ff, 0x0201, 0x0201, 0x0203,
	0x0203, 0x0205, 0x0205, 0x0207, 0x0207, 0x0209, 0x0209, 0x020b, 0x020b, 0x020d,
	0x020d, 0x020f, 0x020f, 0x0211, 0x0211, 0x0213, 0x0213, 0x0215, 0x0215, 0x0217
};


static const wchar kchMinLower2 = 0x0386;
static const wchar kchLimLower2 = 0x0557;

static const wchar g_mpchchLower2[kchLimLower2 - kchMinLower2] =
{
	0x03ac, 0x0387, 0x03ad, 0x03ae, 0x03af, 0x038b, 0x03cc, 0x038d, 0x03cd, 0x03ce,
	0x0390, 0x03b1, 0x03b2, 0x03b3, 0x03b4, 0x03b5, 0x03b6, 0x03b7, 0x03b8, 0x03b9,
	0x03ba, 0x03bb, 0x03bc, 0x03bd, 0x03be, 0x03bf, 0x03c0, 0x03c1, 0x03a2, 0x03c3,
	0x03c4, 0x03c5, 0x03c6, 0x03c7, 0x03c8, 0x03c9, 0x03ca, 0x03cb, 0x03ac, 0x03ad,
	0x03ae, 0x03af, 0x03b0, 0x03b1, 0x03b2, 0x03b3, 0x03b4, 0x03b5, 0x03b6, 0x03b7,
	0x03b8, 0x03b9, 0x03ba, 0x03bb, 0x03bc, 0x03bd, 0x03be, 0x03bf, 0x03c0, 0x03c1,
	0x03c2, 0x03c3, 0x03c4, 0x03c5, 0x03c6, 0x03c7, 0x03c8, 0x03c9, 0x03ca, 0x03cb,
	0x03cc, 0x03cd, 0x03ce, 0x03cf, 0x03d0, 0x03d1, 0x03d2, 0x03d3, 0x03d4, 0x03d5,
	0x03d6, 0x03d7, 0x03d8, 0x03d9, 0x03da, 0x03db, 0x03dc, 0x03dd, 0x03de, 0x03df,
	0x03e0, 0x03e1, 0x03e3, 0x03e3, 0x03e5, 0x03e5, 0x03e7, 0x03e7, 0x03e9, 0x03e9,
	0x03eb, 0x03eb, 0x03ed, 0x03ed, 0x03ef, 0x03ef, 0x03f0, 0x03f1, 0x03f2, 0x03f3,
	0x03f4, 0x03f5, 0x03f6, 0x03f7, 0x03f8, 0x03f9, 0x03fa, 0x03fb, 0x03fc, 0x03fd,
	0x03fe, 0x03ff, 0x0400, 0x0451, 0x0452, 0x0453, 0x0454, 0x0455, 0x0456, 0x0457,
	0x0458, 0x0459, 0x045a, 0x045b, 0x045c, 0x040d, 0x045e, 0x045f, 0x0430, 0x0431,
	0x0432, 0x0433, 0x0434, 0x0435, 0x0436, 0x0437, 0x0438, 0x0439, 0x043a, 0x043b,
	0x043c, 0x043d, 0x043e, 0x043f, 0x0440, 0x0441, 0x0442, 0x0443, 0x0444, 0x0445,
	0x0446, 0x0447, 0x0448, 0x0449, 0x044a, 0x044b, 0x044c, 0x044d, 0x044e, 0x044f,
	0x0430, 0x0431, 0x0432, 0x0433, 0x0434, 0x0435, 0x0436, 0x0437, 0x0438, 0x0439,
	0x043a, 0x043b, 0x043c, 0x043d, 0x043e, 0x043f, 0x0440, 0x0441, 0x0442, 0x0443,
	0x0444, 0x0445, 0x0446, 0x0447, 0x0448, 0x0449, 0x044a, 0x044b, 0x044c, 0x044d,
	0x044e, 0x044f, 0x0450, 0x0451, 0x0452, 0x0453, 0x0454, 0x0455, 0x0456, 0x0457,
	0x0458, 0x0459, 0x045a, 0x045b, 0x045c, 0x045d, 0x045e, 0x045f, 0x0461, 0x0461,
	0x0463, 0x0463, 0x0465, 0x0465, 0x0467, 0x0467, 0x0469, 0x0469, 0x046b, 0x046b,
	0x046d, 0x046d, 0x046f, 0x046f, 0x0471, 0x0471, 0x0473, 0x0473, 0x0475, 0x0475,
	0x0477, 0x0477, 0x0479, 0x0479, 0x047b, 0x047b, 0x047d, 0x047d, 0x047f, 0x047f,
	0x0481, 0x0481, 0x0482, 0x0483, 0x0484, 0x0485, 0x0486, 0x0487, 0x0488, 0x0489,
	0x048a, 0x048b, 0x048c, 0x048d, 0x048e, 0x048f, 0x0491, 0x0491, 0x0493, 0x0493,
	0x0495, 0x0495, 0x0497, 0x0497, 0x0499, 0x0499, 0x049b, 0x049b, 0x049d, 0x049d,
	0x049f, 0x049f, 0x04a1, 0x04a1, 0x04a3, 0x04a3, 0x04a5, 0x04a5, 0x04a7, 0x04a7,
	0x04a9, 0x04a9, 0x04ab, 0x04ab, 0x04ad, 0x04ad, 0x04af, 0x04af, 0x04b1, 0x04b1,
	0x04b3, 0x04b3, 0x04b5, 0x04b5, 0x04b7, 0x04b7, 0x04b9, 0x04b9, 0x04bb, 0x04bb,
	0x04bd, 0x04bd, 0x04bf, 0x04bf, 0x04c0, 0x04c2, 0x04c2, 0x04c4, 0x04c4, 0x04c5,
	0x04c6, 0x04c8, 0x04c8, 0x04c9, 0x04ca, 0x04cc, 0x04cc, 0x04cd, 0x04ce, 0x04cf,
	0x04d1, 0x04d1, 0x04d3, 0x04d3, 0x04d5, 0x04d5, 0x04d7, 0x04d7, 0x04d9, 0x04d9,
	0x04db, 0x04db, 0x04dd, 0x04dd, 0x04df, 0x04df, 0x04e1, 0x04e1, 0x04e3, 0x04e3,
	0x04e5, 0x04e5, 0x04e7, 0x04e7, 0x04e9, 0x04e9, 0x04eb, 0x04eb, 0x04ec, 0x04ed,
	0x04ef, 0x04ef, 0x04f1, 0x04f1, 0x04f3, 0x04f3, 0x04f5, 0x04f5, 0x04f6, 0x04f7,
	0x04f9, 0x04f9, 0x04fa, 0x04fb, 0x04fc, 0x04fd, 0x04fe, 0x04ff, 0x0500, 0x0501,
	0x0502, 0x0503, 0x0504, 0x0505, 0x0506, 0x0507, 0x0508, 0x0509, 0x050a, 0x050b,
	0x050c, 0x050d, 0x050e, 0x050f, 0x0510, 0x0511, 0x0512, 0x0513, 0x0514, 0x0515,
	0x0516, 0x0517, 0x0518, 0x0519, 0x051a, 0x051b, 0x051c, 0x051d, 0x051e, 0x051f,
	0x0520, 0x0521, 0x0522, 0x0523, 0x0524, 0x0525, 0x0526, 0x0527, 0x0528, 0x0529,
	0x052a, 0x052b, 0x052c, 0x052d, 0x052e, 0x052f, 0x0530, 0x0561, 0x0562, 0x0563,
	0x0564, 0x0565, 0x0566, 0x0567, 0x0568, 0x0569, 0x056a, 0x056b, 0x056c, 0x056d,
	0x056e, 0x056f, 0x0570, 0x0571, 0x0572, 0x0573, 0x0574, 0x0575, 0x0576, 0x0577,
	0x0578, 0x0579, 0x057a, 0x057b, 0x057c, 0x057d, 0x057e, 0x057f, 0x0580, 0x0581,
	0x0582, 0x0583, 0x0584, 0x0585, 0x0586
};

static const wchar kchMinLower3 = 0x10a0;
static const wchar kchLimLower3 = 0x10c6;
static const wchar kdchLower3 = 48;


static const wchar kchMinLower4 = 0x1e00;
static const wchar kchLimLower4 = 0x1ffd;

static const wchar g_mpchchLower4[kchLimLower4 - kchMinLower4] =
{
	0x1e01, 0x1e01, 0x1e03, 0x1e03, 0x1e05, 0x1e05, 0x1e07, 0x1e07, 0x1e09, 0x1e09,
	0x1e0b, 0x1e0b, 0x1e0d, 0x1e0d, 0x1e0f, 0x1e0f, 0x1e11, 0x1e11, 0x1e13, 0x1e13,
	0x1e15, 0x1e15, 0x1e17, 0x1e17, 0x1e19, 0x1e19, 0x1e1b, 0x1e1b, 0x1e1d, 0x1e1d,
	0x1e1f, 0x1e1f, 0x1e21, 0x1e21, 0x1e23, 0x1e23, 0x1e25, 0x1e25, 0x1e27, 0x1e27,
	0x1e29, 0x1e29, 0x1e2b, 0x1e2b, 0x1e2d, 0x1e2d, 0x1e2f, 0x1e2f, 0x1e31, 0x1e31,
	0x1e33, 0x1e33, 0x1e35, 0x1e35, 0x1e37, 0x1e37, 0x1e39, 0x1e39, 0x1e3b, 0x1e3b,
	0x1e3d, 0x1e3d, 0x1e3f, 0x1e3f, 0x1e41, 0x1e41, 0x1e43, 0x1e43, 0x1e45, 0x1e45,
	0x1e47, 0x1e47, 0x1e49, 0x1e49, 0x1e4b, 0x1e4b, 0x1e4d, 0x1e4d, 0x1e4f, 0x1e4f,
	0x1e51, 0x1e51, 0x1e53, 0x1e53, 0x1e55, 0x1e55, 0x1e57, 0x1e57, 0x1e59, 0x1e59,
	0x1e5b, 0x1e5b, 0x1e5d, 0x1e5d, 0x1e5f, 0x1e5f, 0x1e61, 0x1e61, 0x1e63, 0x1e63,
	0x1e65, 0x1e65, 0x1e67, 0x1e67, 0x1e69, 0x1e69, 0x1e6b, 0x1e6b, 0x1e6d, 0x1e6d,
	0x1e6f, 0x1e6f, 0x1e71, 0x1e71, 0x1e73, 0x1e73, 0x1e75, 0x1e75, 0x1e77, 0x1e77,
	0x1e79, 0x1e79, 0x1e7b, 0x1e7b, 0x1e7d, 0x1e7d, 0x1e7f, 0x1e7f, 0x1e81, 0x1e81,
	0x1e83, 0x1e83, 0x1e85, 0x1e85, 0x1e87, 0x1e87, 0x1e89, 0x1e89, 0x1e8b, 0x1e8b,
	0x1e8d, 0x1e8d, 0x1e8f, 0x1e8f, 0x1e91, 0x1e91, 0x1e93, 0x1e93, 0x1e95, 0x1e95,
	0x1e96, 0x1e97, 0x1e98, 0x1e99, 0x1e9a, 0x1e9b, 0x1e9c, 0x1e9d, 0x1e9e, 0x1e9f,
	0x1ea1, 0x1ea1, 0x1ea3, 0x1ea3, 0x1ea5, 0x1ea5, 0x1ea7, 0x1ea7, 0x1ea9, 0x1ea9,
	0x1eab, 0x1eab, 0x1ead, 0x1ead, 0x1eaf, 0x1eaf, 0x1eb1, 0x1eb1, 0x1eb3, 0x1eb3,
	0x1eb5, 0x1eb5, 0x1eb7, 0x1eb7, 0x1eb9, 0x1eb9, 0x1ebb, 0x1ebb, 0x1ebd, 0x1ebd,
	0x1ebf, 0x1ebf, 0x1ec1, 0x1ec1, 0x1ec3, 0x1ec3, 0x1ec5, 0x1ec5, 0x1ec7, 0x1ec7,
	0x1ec9, 0x1ec9, 0x1ecb, 0x1ecb, 0x1ecd, 0x1ecd, 0x1ecf, 0x1ecf, 0x1ed1, 0x1ed1,
	0x1ed3, 0x1ed3, 0x1ed5, 0x1ed5, 0x1ed7, 0x1ed7, 0x1ed9, 0x1ed9, 0x1edb, 0x1edb,
	0x1edd, 0x1edd, 0x1edf, 0x1edf, 0x1ee1, 0x1ee1, 0x1ee3, 0x1ee3, 0x1ee5, 0x1ee5,
	0x1ee7, 0x1ee7, 0x1ee9, 0x1ee9, 0x1eeb, 0x1eeb, 0x1eed, 0x1eed, 0x1eef, 0x1eef,
	0x1ef1, 0x1ef1, 0x1ef3, 0x1ef3, 0x1ef5, 0x1ef5, 0x1ef7, 0x1ef7, 0x1ef9, 0x1ef9,
	0x1efa, 0x1efb, 0x1efc, 0x1efd, 0x1efe, 0x1eff, 0x1f00, 0x1f01, 0x1f02, 0x1f03,
	0x1f04, 0x1f05, 0x1f06, 0x1f07, 0x1f00, 0x1f01, 0x1f02, 0x1f03, 0x1f04, 0x1f05,
	0x1f06, 0x1f07, 0x1f10, 0x1f11, 0x1f12, 0x1f13, 0x1f14, 0x1f15, 0x1f16, 0x1f17,
	0x1f10, 0x1f11, 0x1f12, 0x1f13, 0x1f14, 0x1f15, 0x1f1e, 0x1f1f, 0x1f20, 0x1f21,
	0x1f22, 0x1f23, 0x1f24, 0x1f25, 0x1f26, 0x1f27, 0x1f20, 0x1f21, 0x1f22, 0x1f23,
	0x1f24, 0x1f25, 0x1f26, 0x1f27, 0x1f30, 0x1f31, 0x1f32, 0x1f33, 0x1f34, 0x1f35,
	0x1f36, 0x1f37, 0x1f30, 0x1f31, 0x1f32, 0x1f33, 0x1f34, 0x1f35, 0x1f36, 0x1f37,
	0x1f40, 0x1f41, 0x1f42, 0x1f43, 0x1f44, 0x1f45, 0x1f46, 0x1f47, 0x1f40, 0x1f41,
	0x1f42, 0x1f43, 0x1f44, 0x1f45, 0x1f4e, 0x1f4f, 0x1f50, 0x1f51, 0x1f52, 0x1f53,
	0x1f54, 0x1f55, 0x1f56, 0x1f57, 0x1f58, 0x1f51, 0x1f5a, 0x1f53, 0x1f5c, 0x1f55,
	0x1f5e, 0x1f57, 0x1f60, 0x1f61, 0x1f62, 0x1f63, 0x1f64, 0x1f65, 0x1f66, 0x1f67,
	0x1f60, 0x1f61, 0x1f62, 0x1f63, 0x1f64, 0x1f65, 0x1f66, 0x1f67, 0x1f70, 0x1f71,
	0x1f72, 0x1f73, 0x1f74, 0x1f75, 0x1f76, 0x1f77, 0x1f78, 0x1f79, 0x1f7a, 0x1f7b,
	0x1f7c, 0x1f7d, 0x1f7e, 0x1f7f, 0x1f80, 0x1f81, 0x1f82, 0x1f83, 0x1f84, 0x1f85,
	0x1f86, 0x1f87, 0x1f80, 0x1f81, 0x1f82, 0x1f83, 0x1f84, 0x1f85, 0x1f86, 0x1f87,
	0x1f90, 0x1f91, 0x1f92, 0x1f93, 0x1f94, 0x1f95, 0x1f96, 0x1f97, 0x1f90, 0x1f91,
	0x1f92, 0x1f93, 0x1f94, 0x1f95, 0x1f96, 0x1f97, 0x1fa0, 0x1fa1, 0x1fa2, 0x1fa3,
	0x1fa4, 0x1fa5, 0x1fa6, 0x1fa7, 0x1fa0, 0x1fa1, 0x1fa2, 0x1fa3, 0x1fa4, 0x1fa5,
	0x1fa6, 0x1fa7, 0x1fb0, 0x1fb1, 0x1fb2, 0x1fb3, 0x1fb4, 0x1fb5, 0x1fb6, 0x1fb7,
	0x1fb0, 0x1fb1, 0x1f70, 0x1f71, 0x1fb3, 0x1fbd, 0x1fbe, 0x1fbf, 0x1fc0, 0x1fc1,
	0x1fc2, 0x1fc3, 0x1fc4, 0x1fc5, 0x1fc6, 0x1fc7, 0x1f72, 0x1f73, 0x1f74, 0x1f75,
	0x1fc3, 0x1fcd, 0x1fce, 0x1fcf, 0x1fd0, 0x1fd1, 0x1fd2, 0x1fd3, 0x1fd4, 0x1fd5,
	0x1fd6, 0x1fd7, 0x1fd0, 0x1fd1, 0x1f76, 0x1f77, 0x1fdc, 0x1fdd, 0x1fde, 0x1fdf,
	0x1fe0, 0x1fe1, 0x1fe2, 0x1fe3, 0x1fe4, 0x1fe5, 0x1fe6, 0x1fe7, 0x1fe0, 0x1fe1,
	0x1f7a, 0x1f7b, 0x1fe5, 0x1fed, 0x1fee, 0x1fef, 0x1ff0, 0x1ff1, 0x1ff2, 0x1ff3,
	0x1ff4, 0x1ff5, 0x1ff6, 0x1ff7, 0x1f78, 0x1f79, 0x1f7c, 0x1f7d, 0x1ff3
};


static const wchar kchMinLower5 = 0x2160;
static const wchar kchLimLower5 = 0x2170;
static const wchar kdchLower5 = 16;


static const wchar kchMinLower6 = 0x24b6;
static const wchar kchLimLower6 = 0x24d0;
static const wchar kdchLower6 = 26;


static const wchar kchMinLower7 = 0xff21;
static const wchar kchLimLower7 = 0xff3b;
static const wchar kdchLower7 = 32;


/*----------------------------------------------------------------------------------------------
	This unicode case mapping function converts cch wide (16-bit) characters in prgch to
	lower case.
----------------------------------------------------------------------------------------------*/
void ToLower(wchar * prgch, int cch)
{
	AssertArray(prgch, cch);

	wchar * pch;
	wchar * pchLim = prgch + cch;

	for (pch = prgch; pch < pchLim; ++pch)
	{
		if (*pch < kchLimLower1)
		{
			if (*pch >= kchMinLower1)
				*pch = g_mpchchLower1[*pch - kchMinLower1];
		}
		else if (*pch < kchLimLower4)
		{
			if (*pch < kchMinLower3)
			{
				if (*pch < kchLimLower2)
				{
					if (*pch >= kchMinLower2)
						*pch = g_mpchchLower2[*pch - kchMinLower2];
				}
			}
			else if (*pch < kchLimLower3)
				*pch += kdchLower3;
			else if (*pch >= kchMinLower4)
				*pch = g_mpchchLower4[*pch - kchMinLower4];
		}
		else if (*pch < kchLimLower6)
		{
			if (*pch < kchLimLower5)
			{
				if (*pch >= kchMinLower5)
					*pch += kdchLower5;
			}
			else if (*pch >= kchMinLower6)
				*pch += kdchLower6;
		}
		else if (*pch < kchLimLower7)
		{
			if (*pch >= kchMinLower7)
				*pch += kdchLower7;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	This unicode case mapping function converts the wide (16-bit) character ch to lower case.
----------------------------------------------------------------------------------------------*/
wchar ToLower(wchar ch)
{
	if (ch < kchLimLower1)
	{
		if (ch >= kchMinLower1)
			return g_mpchchLower1[ch - kchMinLower1];
	}
	else if (ch < kchLimLower4)
	{
		if (ch < kchMinLower3)
		{
			if (ch < kchLimLower2)
			{
				if (ch >= kchMinLower2)
					return g_mpchchLower2[ch - kchMinLower2];
			}
		}
		else if (ch < kchLimLower3)
			return (wchar)(ch + kdchLower3);
		else if (ch >= kchMinLower4)
			return g_mpchchLower4[ch - kchMinLower4];
	}
	else if (ch < kchLimLower6)
	{
		if (ch < kchLimLower5)
		{
			if (ch >= kchMinLower5)
				return (wchar)(ch + kdchLower5);
		}
		else if (ch >= kchMinLower6)
			return (wchar)(ch + kdchLower6);
	}
	else if (ch < kchLimLower7)
	{
		if (ch >= kchMinLower7)
			return (wchar)(ch + kdchLower7);
	}

	return ch;
}

/*----------------------------------------------------------------------------------------------
	Get a pointer and cch (count of characters) for a string with id stid defined in a resource
	header file.

	Note: according to MSDN comments on LockResource and FreeResource, it is NOT necessary
	to free a resource in 32-bit Windows, and indeed, it is not clear that it is possible to
	free a string resource.

	Note that while it would make a much cleaner implementation of this method to use LoadString
	(after changing the parameters and users so the user supplies a buffer), and possibly
	it would reduce the program's working set, we can't do this because Windows 98 has no
	LoadStringW to get the Unicode version of the string resource.

	@h3{Parameters}
		stid -- string id, e.g., kstidComment (defined in a resource header file).
----------------------------------------------------------------------------------------------*/
void GetResourceString(const wchar ** pprgch, int * pcch, int stid)
{
	if (stid == 25903)
	{
		Warn("UtilString.cpp  GetResourceStringstid == 25903");
	}
	AssertPtr(pprgch);
	AssertPtr(pcch);
	HRSRC hrsrc;
	HGLOBAL hgl;
	HINSTANCE hinst;
	const wchar * pch;
	int stidTbl = (stid >> 4) + 1;
	int cst = stid & 0x0F;

	// TODO Development(JeffG): Design some scheme to look for the possible dlls where
	// the string can be found. For now just get the default dll with
	// ModuleEntry::GetModuleHandle().

	hinst = ModuleEntry::GetModuleHandle();	// ignore a NULL here, it may be test code which
											// doesn't know about windows instance handles.
	if (NULL == (hrsrc = FindResource(hinst, MAKEINTRESOURCE(stidTbl), RT_STRING)) ||
		NULL == (hgl = LoadResource(hinst, hrsrc)) ||
		NULL == (pch = (const wchar *)LockResource(hgl)))
	{
		// REVIEW JohnT(ShonK): Should we throw an exception here?
		// ThrowHr(WarnHr(E_FAIL));
#ifdef DEBUG
		StrAnsi sta;
		sta.Format("Missing resource ID: %d", stid);
		Warn(sta.Chars());
#endif
		*pprgch = NULL;
		*pcch = 0;
		return;
	}

	// Find the string in the list of string for this resource.
	while (--cst >= 0)
	{
		AssertPtr(pch);
		AssertArray(pch, *pch + 1);
		pch += *pch + 1;
	}

	*pcch = *pch++;
	*pprgch = pch;

	AssertArray(*pprgch, *pcch);
}


//:>********************************************************************************************
//:>	StrUtil methods.
//:>********************************************************************************************

// Explicit instantiations.
namespace StrUtil
{
	//:> Instantiations for schar.
	/*------------------------------------------------------------------------------------------
		Convert the decimal integer schar (8-bit) string to an integer.
	------------------------------------------------------------------------------------------*/
	template
		bool ParseInt<schar>(const schar * prgch, int cch, int * pn, int * pcchRead);
	template
		int ParseInt<schar>(const schar * psz);
	template
		bool ParseHexInt<schar>(const schar * prgch, int cch, int * pn, int * pcchRead);
	template
		int ParseHexInt<schar>(const schar * psz);

	/*------------------------------------------------------------------------------------------
		Convert the roman numeral schar (8-bit) string to an integer.
	------------------------------------------------------------------------------------------*/
	template
		bool ParseRomanNumeral<schar>(const schar * prgch, int cch, int * pn, int * pcchRead);
	template
		int ParseRomanNumeral<schar>(const schar * psz);

	/*------------------------------------------------------------------------------------------
		Convert an alpha outline schar (8-bit) string to an integer (e.g, a = 1, b = 2, z = 26,
		aa = 27, etc.).
	------------------------------------------------------------------------------------------*/
	template
		bool ParseAlphaOutline<schar>(const schar * prgch, int cch, int * pn, int * pcchRead);
	template
		int ParseAlphaOutline<schar>(const schar * psz);

	/*------------------------------------------------------------------------------------------
		Convert the generic date schar (8-bit) string to a GenDate integer (e.g., 193012251).
		(0 is a missing generic date).
	------------------------------------------------------------------------------------------*/
	template
		bool ParseGenDate<schar>(const schar * prgch, int cch, int * pn, int * pcchRead);
	template
		int ParseGenDate<schar>(const schar * psz);

	/*------------------------------------------------------------------------------------------
		Convert the date/time schar (8-bit) string to an SilTime. (0 is also a valid time).
	------------------------------------------------------------------------------------------*/
	template
		bool ParseDateTime<schar>(const schar * prgch, int cch, SilTime * pstim, int * pcchRead);
	template
		SilTime ParseDateTime<schar>(const schar * psz);
	template
		bool ParseDate<schar>(const schar * prgch, int cch, SilTime * pstim, int * pcchRead);
	template
		SilTime ParseDate<schar>(const schar * psz);
	template
		bool ParseTime<schar>(const schar * prgch, int cch, SilTime * pstim, int * pcchRead);
	template
		SilTime ParseTime<schar>(const schar * psz);
	template
		int ParseDateWithFormat<schar>(const schar * pszDate, const schar * pszFmt,
			SilTime * pstim);

	//:> Instantiations for wchar.
	/*------------------------------------------------------------------------------------------
		Convert the decimal integer wchar (16-bit) string to an integer.
	------------------------------------------------------------------------------------------*/
	template
		bool ParseInt<wchar>(const wchar * prgch, int cch, int * pn, int * pcchRead);
	template
		int ParseInt<wchar>(const wchar * psz);
	template
		bool ParseHexInt<wchar>(const wchar * prgch, int cch, int * pn, int * pcchRead);
	template
		int ParseHexInt<wchar>(const wchar * psz);

	/*------------------------------------------------------------------------------------------
		Convert the roman numeral wchar (16-bit) string to an integer.
	------------------------------------------------------------------------------------------*/
	template
		bool ParseRomanNumeral<wchar>(const wchar * prgch, int cch, int * pn,
			int * pcchRead);
	template
		int ParseRomanNumeral<wchar>(const wchar * psz);

	/*------------------------------------------------------------------------------------------
		Convert an alpha outline wchar (16-bit) string to an integer (e.g, a = 1, b = 2, z = 26,
		aa = 27, etc.).
	------------------------------------------------------------------------------------------*/
	template
		bool ParseAlphaOutline<wchar>(const wchar * prgch, int cch, int * pn,
			int * pcchRead);
	template
		int ParseAlphaOutline<wchar>(const wchar * psz);

	/*------------------------------------------------------------------------------------------
		Convert the generic date wchar (16-bit) string to a GenDate integer (e.g., 193012251).
		(0 is a missing generic date).
	------------------------------------------------------------------------------------------*/
	template
		bool ParseGenDate<wchar>(const wchar * prgch, int cch, int * pn, int * pcchRead);
	template
		int ParseGenDate<wchar>(const wchar * psz);

	/*------------------------------------------------------------------------------------------
		Convert the date/time wchar (16-bit) string to an SilTime. (0 is also a valid time).
	------------------------------------------------------------------------------------------*/
	template
		bool ParseDateTime<wchar>(const wchar * prgch, int cch, SilTime * pstim, int * pcchRead);
	template
		SilTime ParseDateTime<wchar>(const wchar * psz);

	template
		int ParseDateWithFormat<wchar>(const wchar * pszDate, const wchar * pszFmt,
			SilTime * pstim);
}


/*----------------------------------------------------------------------------------------------
	Convert the roman numeral string psz (any case) to an integer.

	@h3{Return value}
	@code{
		0, if the string contains an illegal character.
		Otherwise, the resulting integer is returned.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	int StrUtil::ParseRomanNumeral(const XChar * psz)
{
	AssertPsz(psz);
	int cch = 0;
	while (psz[cch])
		++cch;
	int n;
	return ParseRomanNumeral(psz, cch, &n, NULL) ? n : 0;
}

/**----------------------------------------------------------------------------------------------
	Convert the roman numeral string (prgch, cch) (any case) to an integer. The result is
	placed in pn. When non-NULL, pcchRead returns the number of characters consumed from the
	input. This may be less than cch if an illegal character occurs in the string.

	@h3{Return value}
	@code{
		true, when cch characters are consumed.
		false, when less than cch characters are consumed.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	bool StrUtil::ParseRomanNumeral(const XChar * prgch, int cch, int * pn, int * pcchRead)
{
	AssertArray(prgch, cch);
	AssertPtr(pn);
	AssertPtrN(pcchRead);

	int n = 0;
	int ich = 0;
	for ( ; ich < cch; ++ich)
	{
		XChar chPrev = 0;
		XChar chNext = 0;
		if (ich < cch - 1)
			chNext = prgch[ich + 1];
		if (ich)
			chPrev = prgch[ich - 1];
		switch (prgch[ich])
		{
		case 'I':
		case 'i':
			if ((chNext == 'v') || (chNext == 'V'))
				n += 4;
			else if ((chNext == 'x') || (chNext == 'X'))
				n += 9;
			else
				n += 1;
			break;

		case 'V':
		case 'v':
			if ((chPrev != 'i') || (chPrev != 'I'))
				n += 5;
			break;

		case 'X':
		case 'x':
			if ((chPrev != 'i') || (chPrev != 'I'))
				if ((chNext == 'l') || (chNext == 'L'))
					n += 40;
				else if ((chNext == 'c') || (chNext == 'C'))
					n += 90;
				else
					n += 10;
			break;

		case 'L':
		case 'l':
			if ((chPrev != 'x') || (chPrev != 'X'))
				n += 50;
			break;

		case 'C':
		case 'c':
			if ((chPrev != 'x') || (chPrev != 'X'))
				if ((chNext == 'd') || (chNext == 'D'))
					n += 400;
				else if ((chNext == 'm') || (chNext == 'M'))
					n += 900;
				else
					n += 100;
			break;

		case 'D':
		case 'd':
			if ((chPrev != 'c') || (chPrev != 'C'))
				n += 500;
			break;

		case 'M':
		case 'm':
			if ((chPrev != 'c') || (chPrev != 'C'))
				n += 1000;
			break;

		default:
			*pn = n;
			if (pcchRead)
				*pcchRead = ich;
			return false; // Something was illegal.
		}
	}
	*pn = n;
	if (pcchRead)
		*pcchRead = ich;
	return ich == cch;
}


/*----------------------------------------------------------------------------------------------
	Convert the alpha outline string psz (any case) to an integer.

	The format we are using is "A, ..., Z, AA, BB, CC". So if the there is more than one
	character, they must all match; otherwise we return 0.

	@h3{Return value}
	@code{
		0, if the string contains an illegal character.
		Otherwise, the resulting integer is returned.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	int StrUtil::ParseAlphaOutline(const XChar * psz)
{
	AssertPsz(psz);

	int cch = 0;
	XChar ch = (XChar)(psz[0] | 0x20);
	while (psz[cch])
	{
		if ((XChar)(psz[cch] | 0x20) != ch)
			return 0;
		++cch;
	}
	int n;
	return ParseAlphaOutline(psz, cch, &n, NULL) ? n : 0;
}

/*----------------------------------------------------------------------------------------------
	Converts an alpha outline string (prgch, cch) (any case) to an integer (e.g, a = 1,
	b = 2, z = 26, aa = 27, bb = 28, etc.). The result is placed in pn. When non-NULL, pcchRead
	returns the number of characters consumed from the input. This may be less than cch if
	a non-alpha character occurs in the string, or if we hit an alphabetic character that
	does not match the first.

	@h3{Return value}
	@code{
		true, when cch characters are consumed.
		false, when less than cch characters are consumed.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	bool StrUtil::ParseAlphaOutline(const XChar * prgch, int cch, int * pn, int * pcchRead)
{
	AssertArray(prgch, cch);
	AssertPtr(pn);
	AssertPtrN(pcchRead);

	int ich = 0;
	XChar chFirst = (XChar)(prgch[0] | 0x20); // lower case
	for ( ; ich < cch; ++ich)
	{
		XChar ch = (XChar)(prgch[ich] | 0x20);
		if (ch < 'a' || ch > 'z' || ch != chFirst)
			break;
	}
	*pn = ((ich - 1) * 26) + chFirst - 'a' + 1;
	if (pcchRead)
		*pcchRead = ich;
	return (ich == cch);

/*
	// Alternate approach for format "A, ..., Z, AA, AB, AC"
	int n = 0;
	int ich = 0;
	for ( ; ich < cch; ++ich)
	{
		XChar ch = (XChar)(prgch[ich] | 0x20);
		if (ch < 'a' || ch > 'z')
			break;
		n = n * 26 + ch - 'a' + 1;
	}
	*pn = n;
	if (pcchRead)
		*pcchRead = ich;
	return ich == cch;
*/
}


/*----------------------------------------------------------------------------------------------
	Convert the decimal integer string psz to an integer.

	@h3{Return value}
	@code{
		0, if the string contains a non-alpha character.
		Otherwise, the resulting integer is returned.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	int StrUtil::ParseInt(const XChar * psz)
{
	AssertPsz(psz);

	int cch = 0;
	while (psz[cch])
		++cch;
	int n;
	return ParseInt(psz, cch, &n, NULL) ? n : 0;
}

/*----------------------------------------------------------------------------------------------
	Convert the decimal integer string (prgch, cch) to an integer. The result is placed in
	pn. When non-NULL, pcchRead returns the number of characters consumed from the input.
	This may be less than cch if a non-numeric/sign character occurs in the string.

	@h3{Return value}
	@code{
		true, when cch characters are consumed.
		false, when less than cch characters are consumed.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	bool StrUtil::ParseInt(const XChar * prgch, int cch, int * pn, int * pcchRead)
{
	AssertArray(prgch, cch);
	AssertPtr(pn);
	AssertPtrN(pcchRead);

	int n = 0;
	bool fNeg = prgch[0] == '-';
	int ich = (fNeg || prgch[0] == '+') ? 1 : 0;
	for ( ; ich < cch; ++ich)
	{
		int nT = prgch[ich] - '0';
		if ((uint)nT > 9)
			break;
		n = n * 10 + nT;
	}
	*pn = fNeg ? -n : n;
	if (pcchRead)
		*pcchRead = ich;
	return ich == cch;
}

/*----------------------------------------------------------------------------------------------
	Convert the hexadecimal string psz to an integer.

	@h3{Return value}
	@code{
		0, if the string contains a non-alpha character.
		Otherwise, the resulting integer is returned.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	int StrUtil::ParseHexInt(const XChar * psz)
{
	AssertPsz(psz);

	int cch = 0;
	while (psz[cch])
		++cch;
	int n;
	return ParseHexInt(psz, cch, &n, NULL) ? n : 0;
}

/*----------------------------------------------------------------------------------------------
	Convert the hexadecimal string (prgch, cch) to an integer. The result is placed in
	pn. When non-NULL, pcchRead returns the number of characters consumed from the input.
	This may be less than cch if a non-numeric/sign character occurs in the string.

	@h3{Return value}
	@code{
		true, when cch characters are consumed.
		false, when less than cch characters are consumed.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	bool StrUtil::ParseHexInt(const XChar * prgch, int cch, int * pn, int * pcchRead)
{
	AssertArray(prgch, cch);
	AssertPtr(pn);
	AssertPtrN(pcchRead);

	int n = 0;
	int nT;
	bool fNeg = prgch[0] == '-';
	int ich = (fNeg || prgch[0] == '+') ? 1 : 0;
	for ( ; ich < cch; ++ich)
	{
		nT = prgch[ich];
		if ((nT >= '0') && (nT <= '9'))
			nT = nT - '0';
		else if ((nT >= 'a') && (nT <= 'f'))
			nT = nT - 'a' + 10;
		else if ((nT >= 'A') && (nT <= 'F'))
			nT = nT - 'A' + 10;
		else
			break;
		n = n * 16 + nT;
	}
	*pn = fNeg ? -n : n;
	if (pcchRead)
		*pcchRead = ich;
	return ich == cch;
}

//:Associate with StrUtil.

// Locale date & time strings
static StrUni g_rgstuDaysAbbr[7];		// Abbreviated days of week
static StrUni g_rgstuDays[7];			// Unabbreviated days of week
static StrUni g_rgstuMonthsAbbr[12];	// Abbreviated months
static StrUni g_rgstuMonths[12];		// Unabbreviated months
static StrUni g_rgstuSTimeFormat;		// Time format
static StrUni g_rgstuS1159;				// AM time-marker
static StrUni g_rgstuS2359;				// PM time-marker
static StrUni g_rgstuSDecimal;			// Decimal separator

/*----------------------------------------------------------------------------------------------
	If the locale changes, re-load the locale strings
----------------------------------------------------------------------------------------------*/
static void CheckForLocaleChange()
{
	static LCID lcid = (LCID)~0;
	LCID lcid1 = ::GetUserDefaultLCID();
	if (lcid1 != lcid)
	{
		lcid = lcid1;
		g_rgstuDaysAbbr[0].Clear();
		g_rgstuDays[0].Clear();
		g_rgstuMonthsAbbr[0].Clear();
		g_rgstuMonths[0].Clear();
		g_rgstuSTimeFormat.Clear();
		g_rgstuS1159.Clear();
		g_rgstuS2359.Clear();
		g_rgstuSDecimal.Clear();
	}
}

/*----------------------------------------------------------------------------------------------
	Make sure the static array of day (of week) abbreviations has been initialized properly
	according to the locale.
----------------------------------------------------------------------------------------------*/
static void LoadDaysAbbr()
{
	CheckForLocaleChange();
	if (g_rgstuDaysAbbr[0].Length())
		return;
	LCTYPE rgTypes[7] =
	{
		LOCALE_SABBREVDAYNAME7, LOCALE_SABBREVDAYNAME1, LOCALE_SABBREVDAYNAME2,
		LOCALE_SABBREVDAYNAME3, LOCALE_SABBREVDAYNAME4, LOCALE_SABBREVDAYNAME5,
		LOCALE_SABBREVDAYNAME6
	};
	achar rgch[80];
	int cch;
	for (int i = 0; i < 7; ++i)
	{
		cch = ::GetLocaleInfo(LOCALE_USER_DEFAULT, rgTypes[i], rgch, 80);
		g_rgstuDaysAbbr[i].Assign(rgch);
	}
}

/*----------------------------------------------------------------------------------------------
	Make sure the static array of day (of week) names has been initialized properly
	according to the locale.
----------------------------------------------------------------------------------------------*/
static void LoadDays()
{
	CheckForLocaleChange();
	if (g_rgstuDays[0].Length())
		return;
	LCTYPE rgTypes[7] =
	{
		LOCALE_SDAYNAME7, LOCALE_SDAYNAME1, LOCALE_SDAYNAME2, LOCALE_SDAYNAME3,
		LOCALE_SDAYNAME4, LOCALE_SDAYNAME5, LOCALE_SDAYNAME6
	};
	achar rgch[80];
	int cch;
	for (int i = 0; i < 7; ++i)
	{
		cch = ::GetLocaleInfo(LOCALE_USER_DEFAULT, rgTypes[i], rgch, 80);
		g_rgstuDays[i].Assign(rgch);
	}
}

/*----------------------------------------------------------------------------------------------
	Make sure the static array of month abbreviations has been initialized properly
	according to the locale.
----------------------------------------------------------------------------------------------*/
static void LoadMonthsAbbr()
{
	CheckForLocaleChange();
	if (g_rgstuMonthsAbbr[0].Length())
		return;
	LCTYPE rgTypes[12] =
	{
		LOCALE_SABBREVMONTHNAME1,  LOCALE_SABBREVMONTHNAME2,  LOCALE_SABBREVMONTHNAME3,
		LOCALE_SABBREVMONTHNAME4,  LOCALE_SABBREVMONTHNAME5,  LOCALE_SABBREVMONTHNAME6,
		LOCALE_SABBREVMONTHNAME7,  LOCALE_SABBREVMONTHNAME8,  LOCALE_SABBREVMONTHNAME9,
		LOCALE_SABBREVMONTHNAME10, LOCALE_SABBREVMONTHNAME11, LOCALE_SABBREVMONTHNAME12
	};
	achar rgch[80];
	int cch;
	for (int i = 0; i < 12; ++i)
	{
		cch = ::GetLocaleInfo(LOCALE_USER_DEFAULT, rgTypes[i], rgch, 80);
		g_rgstuMonthsAbbr[i].Assign(rgch);
	}
}

/*----------------------------------------------------------------------------------------------
	Make sure the static array of month names has been initialized properly
	according to the locale.
----------------------------------------------------------------------------------------------*/
static void LoadMonths()
{
	CheckForLocaleChange();
	if (g_rgstuMonths[0].Length())
		return;
	LCTYPE rgTypes[12] =
	{
		LOCALE_SMONTHNAME1,  LOCALE_SMONTHNAME2,  LOCALE_SMONTHNAME3,  LOCALE_SMONTHNAME4,
		LOCALE_SMONTHNAME5,  LOCALE_SMONTHNAME6,  LOCALE_SMONTHNAME7,  LOCALE_SMONTHNAME8,
		LOCALE_SMONTHNAME9,  LOCALE_SMONTHNAME10, LOCALE_SMONTHNAME11, LOCALE_SMONTHNAME12
	};
	achar rgch[80];
	int cch;
	for (int i = 0; i < 12; ++i)
	{
		cch = ::GetLocaleInfo(LOCALE_USER_DEFAULT, rgTypes[i], rgch, 80);
		g_rgstuMonths[i].Assign(rgch);
	}
}


/*----------------------------------------------------------------------------------------------
	Get the day of week string given the day (1-7)
----------------------------------------------------------------------------------------------*/
StrUni * StrUtil::GetDayOfWeekStr(int day, bool longformat)
{
	static StrUni blank;
	if (!longformat)
	{
		LoadDaysAbbr();
		return (day >= 1 && day <= 7) ? &g_rgstuDaysAbbr[day-1] : &blank;
	}
	else
	{
		LoadDays();
		return (day >= 1 && day <= 7) ? &g_rgstuDays[day-1] : &blank;
	}
}

/*----------------------------------------------------------------------------------------------
	Get the month string given the month (1-12)
----------------------------------------------------------------------------------------------*/
StrUni * StrUtil::GetMonthStr(int month, bool longformat)
{
	static StrUni blank;
	if (!longformat)
	{
		LoadMonthsAbbr();
		return (month >= 1 && month <= 12) ? &g_rgstuMonthsAbbr[month-1] : &blank;
	}
	else
	{
		LoadMonths();
		return (month >= 1 && month <= 12) ? &g_rgstuMonths[month-1] : &blank;
	}
}

/*----------------------------------------------------------------------------------------------
	Convert the generic date string psz to a GenDate integer (e.g., 193012251). (0 is a
	missing generic date). Return the resulting integer. Return 0 if the string contains an
	illegal character.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	int StrUtil::ParseGenDate(const XChar * psz)
{
	AssertPsz(psz);

	int cch = 0;
	while (psz[cch])
		++cch;
	int gdat;
	return ParseGenDate(psz, cch, &gdat, NULL) ? gdat : 0;
}


/*----------------------------------------------------------------------------------------------
   Helper function for ParseGenDate()
----------------------------------------------------------------------------------------------*/
typedef wchar *pwchar;
typedef const wchar * pcwchar;

static void SkipWhitespace (pcwchar pwdate)
{
	// REVIEW SteveMc(SBammel): - is whitespace language-dependent?
	while (*pwdate != 0 && *pwdate <= ' ')
		pwdate++;
}

/*----------------------------------------------------------------------------------------------
   Helper function for ParseGenDate()
----------------------------------------------------------------------------------------------*/
static int MatchLeadingChars (pwchar pwarg, pcwchar pwval, int minmatch, bool casesensitive)
{
	int nval = wcslen (pwval);
	bool eq;
	for (int i=0; i<nval; i++)
	{
		if (pwval[i] == 0)
			return (i >= minmatch) ? i : -1; // match if end of string and minimum matched
		if (casesensitive)
			eq = (pwarg[i] == pwval[i]);
		else
			eq = (ToLower(pwarg[i]) == ToLower(pwval[i]));
		if (!eq)
			return (i >= minmatch) ? i : -1; // match if minimum chars matched
	}
	return nval;
}

/*----------------------------------------------------------------------------------------------
   Helper function for ParseGenDate()
----------------------------------------------------------------------------------------------*/
static int ParseStringSet (pwchar& pwdate, const wchar *set[], int minmatch, bool casesensitive)
{
	int nFound = 0, iFound = 0, nLen = -1;
	SkipWhitespace (pwdate);
	for (int i=0; set[i] != NULL; i++)
	{
		int n = MatchLeadingChars (pwdate, set[i], minmatch, casesensitive);
		if (n > 0)
			nFound++, iFound = i, nLen = n;
	}
	if (nFound == 1)
	{
		pwdate += nLen;
		return iFound;
	}
	else
		return -1; // not found or ambiguous
}

/*----------------------------------------------------------------------------------------------
   Helper function for ParseGenDate()
----------------------------------------------------------------------------------------------*/
static int ParseYearOnly (pwchar pwdate, SilTimeInfo& sti)
{
	int cch = 0, year = 0;
	for (int i=0; i<5; i++)
	{
		wchar ch = pwdate[i];
		if (ch >= '0' && ch <= '9')
		{
			year = 10*year + ch - '0';
			cch++;
		}
	}
	if (cch <= 4)
	{
		sti.year = year;
		sti.mday = sti.ymon = 0;
		return cch;
	}
	else
		return 0;
}

/*----------------------------------------------------------------------------------------------
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
		0 = Before (If GenDate = 0, it means nothing is entered)
		1 = Exact
		2 = Approximate
		3 = After
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	bool StrUtil::ParseGenDate(const XChar * prgch, int cch, int * pgdat, int * pcchRead)
{
	AssertArray(prgch, cch);
	AssertPtr(pgdat);
	AssertPtrN(pcchRead);

	// do all processing with wide characters
	wchar *pwdate0, *pwdate, wdate[81];
	int i = 0;
	for (; prgch[i] && i<SizeOfArray(wdate)-1; i++)
		wdate[i] = prgch[i];
	wdate[i] = 0;
	pwdate0 = pwdate = wdate;

	int nPrec = 1, nBC = 1, cchDate;
	bool fPrec_found = false, fBC_found = false;
	SilTimeInfo sti = { 0,0,0,0,0,0,0,0 };

	SkipWhitespace (pwdate);
	for (int rep = 0; rep < 2; rep++) // repeat to look for BC or AD at beginning or end
	{
		bool fFound = true;
		while (fFound)
		{
			fFound = false;
			if (!fPrec_found)
			{
				LoadDateQualifiers();
				Assert(g_fDoneDateQual);
				int n = ParseStringSet (pwdate, g_rgchwDatePrec, 3, false);
				if (n == 4) // special test for abbreviation "abt" for "about"
					n = 2;
				if (n >= 0)
				{
					nPrec = n;
					fPrec_found = fFound = true;
					SkipWhitespace (pwdate);
				}
			}
			if (!fBC_found) // BC or AD may be before or after date
			{
				LoadDateQualifiers();
				Assert(g_fDoneDateQual);
				int n = ParseStringSet (pwdate, g_rgchwDateBC_AD, 2, false);
				if (n >= 0)
				{
					if (n == 0)
						nBC = -1; // "BC" found
					fBC_found = fFound = true;
					SkipWhitespace (pwdate);
				}
			}
		}
		if (rep == 1) // look for date only once
			break;

		pwdate += (cchDate = ParseDateString (pwdate, &sti)); // will also get month/date
		SkipWhitespace (pwdate);
		if (cchDate == 0)
		{
			pwdate += (cchDate = ParseYearOnly (pwdate, sti));
			SkipWhitespace (pwdate);
		}
		if (cchDate == 0)
			return false;
	}

	// Make a generic date int.
	*pgdat = nBC * (sti.year * 100000L + sti.ymon * 1000L + sti.mday * 10 + nPrec);

	if (pcchRead)
		*pcchRead = pwdate - pwdate0;
	return *pwdate == 0;
}

/*----------------------------------------------------------------------------------------------
	Helper function for ParseFormattedDate()
----------------------------------------------------------------------------------------------*/
static bool ValidateDate(int year, int ymon, int mday)
{
	int days_in_month;
	if (year < -9999 || year > 9999 || !year)
		return false;
	if (ymon < 1 || ymon > 12)
		return false;
	if ((100*year + ymon) == 175209)
		days_in_month = 19; // the month the calendar was changed
	else
	{
		static int m[12] = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
		m[1] = (((year%4) == 0 && (year%100) != 0) || (year%1000) == 0) ? 29 : 28;
		days_in_month = m[ymon-1];
	}
	if (mday < 1 || mday > days_in_month)
		return false;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Helper function to parse quoted text in date and time formats
----------------------------------------------------------------------------------------------*/
static int ParseFmtQuotedText (const wchar *pszFmt, pcwchar& pszDate)
{
	const wchar *pszFmt0 = pszFmt;
	pszFmt++; // skip leading quote

	while (*pszFmt && *pszFmt != '\'')
	{
		if (*pszFmt <= ' ')
		{
			pszFmt++;
			SkipWhitespace (pszDate);
		}
		else if (ToLower(*pszFmt) == ToLower(*pszDate))
		{
			*pszFmt++;
			*pszDate++;
		}
		else
			return -1; // failed to match
	}

	pszFmt++; // skip trailing quote
	return pszFmt - pszFmt0; // return number of characters parsed
}

/*----------------------------------------------------------------------------------------------
	Try parsing a date string into a SilTimeInfo data structure following a specified format.

	The format for a date string consists of a NUL-terminated strings containing literal
	separator characters plus date elements drawn from the following.

	@code{
	Day   Meaning
	----  -------
	d     Day of the month as digits without leading zeros for single digit days.
	dd    Day of the month as digits with leading zeros for single digit days.
	ddd   Day of the week as a 3-letter abbreviation given by a LOCALE_SABBREVDAYNAME value.
	dddd  Day of the week given by a LOCALE_SDAYNAME value.

	Month Meaning
	----- -------
	M     Month as digits without leading zeros for single digit months.
	MM    Month as digits with leading zeros for single digit months.
	MMM   Month as a three letter abbreviation given by a LOCALE_SABBREVMONTHNAME value.
	MMMM  Month given by a LOCALE_SMONTHNAME value.

	Year  Meaning
	----  -------
	y     Year represented only be the last digit.
	yy    Year represented only be the last two digits.
	yyyy  Year represented by the full 4 digits.

	Era   Meaning
	----  -------
	gg    Period/era string as specified by the CAL_SERASTRING value. The gg format picture in
		  a date string is ignored if there is no associated era string.
	}

	In the preceding formats, the letters d, g, and y must be lowercase and the letter M must
	be uppercase.

	@param pszFmt Date format string as described above.
	@param pszDateString Date string that may (or may not) be in the specified format.
	@param psti Pointer to an SilTimeInfo data structure used for output.

	@return The number of characters consumed from pszDateString to fill psti, or zero if an
					error occurs.
----------------------------------------------------------------------------------------------*/
static int ParseFormattedDate(const wchar * pszFmt, const wchar * pszDateString,
	SilTimeInfo * psti)
{
	AssertPsz(pszFmt);
	AssertPsz(pszDateString);
	AssertPtr(psti);

	wchar ch;
	int i;
	int cch;
	int cchUsed;
	bool fDayPresent = false;
	bool fMonthPresent = false;
	bool fYearPresent = false;
	const wchar * pszDate = pszDateString;
	wchar * pszNewDate;
	for (pszDate += wcsspn(pszDate, szSpace); *pszFmt; pszFmt += cch)
	{
		ch = pszFmt[0];
		for (i = 0; pszFmt[i]; ++i)
		{
			if (pszFmt[i] != ch)
				break;
		}
		cch = i;
		switch (ch)
		{
		case 'd':
			switch (cch)
			{
			case 1:
			case 2:
				fDayPresent = true;
				psti->mday = (unsigned short)wcstoul(pszDate, &pszNewDate, 10);
				cchUsed = pszNewDate - pszDate;
				if (cchUsed < 1 || psti->mday > 31)
					return 0;
				pszDate = pszNewDate;
				break;
			case 3: // Abbreviated day of week as specified by a LOCALE_SABBREVDAYNAME value.
			case 4: // Unabbreviated day of the week as specified by a LOCALE_SDAYNAME value.
				cchUsed = 0;
				for (i = 0; i < 7; ++i)
				{
					StrUni *pstu = StrUtil::GetDayOfWeekStr(i + 1, cch == 4);
					if (!_wcsnicmp(pszDate, pstu->Chars(), pstu->Length()))
					{
						psti->wday = i + 1;
						cchUsed = pstu->Length();
						pszDate += cchUsed;
						break;
					}
				}
				if (cchUsed == 0)
					return 0;
				break;
			default:
				return 0;
			}
			break;

		case 'M':
			fMonthPresent = true;
			switch (cch)
			{
			case 1:
			case 2:
				psti->ymon = (unsigned short)wcstoul(pszDate, &pszNewDate, 10);
				cchUsed = pszNewDate - pszDate;
				if (cchUsed < 1 || cchUsed > 2)
					return 0;
				pszDate = pszNewDate;
				break;
			case 3: // Abbreviated month as specified by a LOCALE_SABBREVMONTHNAME value.
			case 4:	// Unabbreviated month as specified by a LOCALE_SMONTHNAME value.
				cchUsed = 0;
				for (i = 1; i <= 12; ++i)
				{
					StrUni *pstu = StrUtil::GetMonthStr(i, cch == 4);
					if (!_wcsnicmp(pszDate, pstu->Chars(), pstu->Length()))
					{
						psti->ymon = i;
						cchUsed = pstu->Length();
						pszDate += cchUsed;
						break;
					}
				}
				if (cchUsed == 0)
					return 0;
				break;

			default:
				return 0;
			}
			break;

		case 'y':
			fYearPresent = true;
			switch (cch)
			{
			case 1:
				psti->year = (unsigned short)(2000 + wcstoul(pszDate, &pszNewDate, 10));
				cchUsed = pszNewDate - pszDate;
				if (cchUsed != 1)
				{
					return 0;
				}
				else
				{
					// REVIEW SteveMc: Is there a better, more correct adjustment?
					SYSTEMTIME systime;
					::GetSystemTime(&systime);
					if (psti->year > systime.wYear)
						psti->year -= 100;
				}
				pszDate = pszNewDate;
				break;
			case 2:
				psti->year = (unsigned short)(2000 + wcstoul(pszDate, &pszNewDate, 10));
				cchUsed = pszNewDate - pszDate;
				if (cchUsed == 0 || cchUsed > 2)
				{
					return 0;
				}
				else
				{
					// REVIEW SteveMc: Is there a better, more correct adjustment?
					SYSTEMTIME systime;
					::GetSystemTime(&systime);
					if (psti->year > systime.wYear)
						psti->year -= 100;
				}
				pszDate = pszNewDate;
				break;
			case 4:
				psti->year = (unsigned short)wcstoul(pszDate, &pszNewDate, 10);
				cchUsed = pszNewDate - pszDate;
				if (cchUsed == 0)
					return 0;
				pszDate = pszNewDate;
				break;
			default:
				return 0;
				break;
			}
			break;

		case 'g':
			// TODO SteveMc: IMPLEMENT ME!
			return 0;
			break;

		case '\'': // quoted text
			cch = ParseFmtQuotedText (pszFmt, pszDate);
			if (cch < 0)
				return 0; // text not found
			break;

		case ' ':
			pszDate += wcsspn(pszDate, szSpace);
			break;

		default:
			// Check for matching separators.
			pszDate += wcsspn(pszDate, szSpace);
			for (int j = 0; j < cch; ++j)
			{
				if (*pszDate == ch)
				{
					++pszDate;
				}
				else
				{
					return 0;
				}
			}
			pszDate += wcsspn(pszDate, szSpace);
			break;
		}
	}

	bool fOk = ValidateDate(fYearPresent ? psti->year : 2000, fMonthPresent ? psti->ymon : 1,
		fDayPresent ? psti->mday : 1);
	return fOk ? pszDate - pszDateString : 0;
}

/*----------------------------------------------------------------------------------------------
	Parse a date string into a SilTimeInfo data structure, trying a variety of formats, starting
	with those defined for the current locale.

	@param pszDateString Character string ostensibly containing a date.
	@param psti Pointer to an SilTimeInfo data structure used for output.

	@return The number of characters consumed from pszDateString to fill psti, or zero if an
					error occurs.
----------------------------------------------------------------------------------------------*/
static int ParseDateString(const wchar * pszDateString, SilTimeInfo * psti)
{
	AssertPsz(pszDateString);
	AssertPtr(psti);

	int cch;
	achar rgch[80];
	cch = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SLONGDATE, rgch, 80);
	StrUniBuf stubLongFmt(rgch);
	cch = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SSHORTDATE, rgch, 80);
	StrUniBuf stubShortFmt(rgch);
	cch = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SYEARMONTH, rgch, 80);
	StrUniBuf stubMonthFmt(rgch);
	if (!stubMonthFmt.Length())
		stubMonthFmt.Assign(L"MMMM, yyyy");
	cch = 0;
	// Try the short format first.
	if (stubShortFmt.Length())
	{
		cch = ParseFormattedDate(stubShortFmt.Chars(), pszDateString, psti);
	}
	if (!cch)
	{
		// Try the long format.
		if (stubLongFmt.Length())
			cch = ParseFormattedDate(stubLongFmt.Chars(), pszDateString, psti);
	}
	if (!cch)
	{
		// Try a revised long format (remove the day of the week, if present).
		StrUniBuf stubFmt(stubLongFmt);
		int ich = stubFmt.FindStr(L"dddd, ");
		if (ich >= 0)
		{
			stubFmt.Replace(ich, ich + 6, L"");
			cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
		}
		else
		{
			ich = stubFmt.FindStr(L"dddd");
			if (ich >= 0)
			{
				stubFmt.Replace(ich, ich + 4, L"");
				cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
			}
		}
	}
	if (!cch)
	{
		// Try a canonical long form.
		cch = ParseFormattedDate(L"MMMM d, yyyy", pszDateString, psti);
	}
	if (!cch)
	{
		// Try another canonical long form.
		cch = ParseFormattedDate(L"MMM d, yyyy", pszDateString, psti);
	}
	if (!cch)
	{
		// Try another canonical long form.
		cch = ParseFormattedDate(L"MMM. d, yyyy", pszDateString, psti);
	}
	if (!cch)
	{
		// Try a revised short format (flip between 2-digit and 4-digit year).
		StrUniBuf stubFmt(stubShortFmt);
		int ich = stubFmt.FindStr(L"yyyy");
		if (ich >= 0)
		{
			stubFmt.Replace(ich, ich + 4, L"yy");
		}
		else
		{
			ich = stubFmt.FindStr(L"yy");
			if (ich >= 0)
				stubFmt.Replace(ich, ich + 2, L"yyyy");
			else
				stubFmt.Clear();
		}
		if (stubFmt.Length())
			cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
	}
	if (!cch)
	{
		// Try progressively shortening the month specifier in the short format.
		StrUniBuf stubFmt(stubShortFmt);
		int ich = stubFmt.FindStr(L"MMMM");
		if (ich >= 0)
		{
			stubFmt.Replace(ich, ich + 1, L"");
			cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
		}
		if (!cch)
		{
			ich = stubFmt.FindStr(L"MMM");
			if (ich >= 0)
			{
				stubFmt.Replace(ich, ich + 1, L"");
				cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
			}
			if (!cch)
			{
				ich = stubFmt.FindStr(L"MM");
				if (ich >= 0)
				{
					stubFmt.Replace(ich, ich + 1, L"");
					cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
				}
			}
		}
	}
	if (!cch)
	{
		// Try shortening the date specifier in the short format.
		StrUniBuf stubFmt(stubShortFmt);
		int ich = stubFmt.FindStr(L"dd");
		if (ich >= 0)
		{
			stubFmt.Replace(ich, ich + 1, L"");
			cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
		}
	}
	if (!cch)
	{
		// Try progressively lengthening the month specifier in the short format.
		StrUniBuf stubFmt(stubShortFmt);
		int ich = stubFmt.FindStr(L"M");
		if (ich >= 0 && stubFmt.GetAt(ich + 1) != 'M')
		{
			stubFmt.Replace(ich, ich, L"M");
			cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
		}
		if (!cch)
		{
			ich = stubFmt.FindStr(L"MM");
			if (ich >= 0 && stubFmt.GetAt(ich + 2) != 'M')
			{
				stubFmt.Replace(ich, ich, L"M");
				cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
			}
			if (!cch)
			{
				ich = stubFmt.FindStr(L"MMM");
				if (ich >= 0 && stubFmt.GetAt(ich + 3) != 'M')
				{
					stubFmt.Replace(ich, ich, L"M");
					cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
				}
			}
		}
	}
	if (!cch)
	{
		// Try lengthening the date specifier in the short format.
		StrUniBuf stubFmt(stubShortFmt);
		int ich = stubFmt.FindStr(L"d");
		if (ich >= 0 && stubFmt.GetAt(ich + 1) != 'd')
		{
			stubFmt.Replace(ich, ich, L"d");
			cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
		}
	}
	if (!cch && stubMonthFmt.Length())
	{
		// Try the YearMonth form.
		cch = ParseFormattedDate(stubMonthFmt.Chars(), pszDateString, psti);
		if (!cch)
		{
			// Try progressively shortening the month specifier in the YearMonth format.
			StrUniBuf stubFmt(stubMonthFmt);
			int ich = stubFmt.FindStr(L"MMMM");
			if (ich >= 0)
			{
				stubFmt.Replace(ich, ich + 1, L"");
				cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
			}
			if (!cch)
			{
				ich = stubFmt.FindStr(L"MMM");
				if (ich >= 0)
				{
					stubFmt.Replace(ich, ich + 1, L"");
					cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
				}
				if (!cch)
				{
					ich = stubFmt.FindStr(L"MM");
					if (ich >= 0)
					{
						stubFmt.Replace(ich, ich + 1, L"");
						cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
					}
				}
			}
		}
		if (!cch)
		{
			// Try progressively lengthening the month specifier in the YearMonth format.
			StrUniBuf stubFmt(stubMonthFmt);
			int ich = stubFmt.FindStr(L"M");
			if (ich >= 0 && stubFmt.GetAt(ich + 1) != 'M')
			{
				stubFmt.Replace(ich, ich, L"M");
				cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
			}
			if (!cch)
			{
				ich = stubFmt.FindStr(L"MM");
				if (ich >= 0 && stubFmt.GetAt(ich + 2) != 'M')
				{
					stubFmt.Replace(ich, ich, L"M");
					cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
				}
				if (!cch)
				{
					ich = stubFmt.FindStr(L"MMM");
					if (ich >= 0 && stubFmt.GetAt(ich + 3) != 'M')
					{
						stubFmt.Replace(ich, ich, L"M");
						cch = ParseFormattedDate(stubFmt.Chars(), pszDateString, psti);
					}
				}
			}
		}
	}
	if (!cch)
	{
		// Try a partial long form.
		cch = ParseFormattedDate(L"MMMM yyyy", pszDateString, psti);
	}
	if (!cch)
	{
		// Try another partial long form.
		cch = ParseFormattedDate(L"MMM yyyy", pszDateString, psti);
	}
	return cch;
}

/*----------------------------------------------------------------------------------------------
	Helper function for ParseTimeString()
----------------------------------------------------------------------------------------------*/
static int ParseTimeStringTry(const wchar * pszFmt, const wchar * pszTime, SilTimeInfo * psti)
{
	AssertPsz(pszTime);
	AssertPtr(psti);

	int cch = 0, cchUsed;

	const wchar * pszTimeBeg = pszTime;
	wchar * pszNewTime;
	bool f24hour = false, fPM = false;

	psti->hour = psti->min = psti->sec = psti->msec = 0;
	cch = 0;
	for (pszTime += wcsspn(pszTime, szSpace); *pszFmt; pszFmt += cch)
	{
		wchar ch = pszFmt[0];
		for (cch = 0; pszFmt[cch] && pszFmt[cch]==ch; ++cch); // count run of chars

		switch (ch)
		{
		case 'h': // 12-hour format
			psti->hour = (int)wcstoul(pszTime, &pszNewTime, 10);
			cchUsed = pszNewTime - pszTime;
			if (psti->hour == 12)
				psti->hour = 0;
			if (cchUsed < 1 || psti->hour > 11)
				return 0;
			pszTime = pszNewTime;
			break;
		case 'H': // 24-hour format
			f24hour = true;
			psti->hour = (int)wcstoul(pszTime, &pszNewTime, 10);
			cchUsed = pszNewTime - pszTime;
			if (cchUsed < 1 || psti->hour > 23)
				return 0;
			pszTime = pszNewTime;
			break;
		case 'm': // minutes
			psti->min = (int)wcstoul(pszTime, &pszNewTime, 10);
			cchUsed = pszNewTime - pszTime;
			if (cchUsed < 1 || psti->min > 59)
				return 0;
			pszTime = pszNewTime;
			break;
		case 's': // seconds
			psti->sec = (int)wcstoul(pszTime, &pszNewTime, 10);
			cchUsed = pszNewTime - pszTime;
			if (cchUsed < 1 || psti->sec > 59)
				return 0;
			pszTime = pszNewTime;
			if (wcsncmp(pszTime, g_rgstuSDecimal.Chars(), g_rgstuSDecimal.Length()) == 0)
			{	// milliseconds expected after decimal separator
				pszTime += g_rgstuSDecimal.Length();
				psti->msec = (int)wcstoul(pszTime, &pszNewTime, 10);
				cchUsed = pszNewTime - pszTime;
				if (cchUsed == 1)
					psti->msec *= 100;
				else if (cchUsed == 2)
					psti->msec *= 10;
				else if (cchUsed != 3)
					return 0; // milliseconds improper
				pszTime = pszNewTime;
			}
			break;
		case 't': // time marker (AM/PM)
			pszTime += wcsspn(pszTime, szSpace);
			fPM = (_wcsnicmp(pszTime, g_rgstuS2359.Chars(), g_rgstuS2359.Length()) == 0);
			if (fPM)
				pszTime += g_rgstuS2359.Length();
			else if (_wcsnicmp(pszTime, g_rgstuS1159.Chars(), g_rgstuS1159.Length()) == 0)
				pszTime += g_rgstuS1159.Length();
			else
				return 0; // required time marker not found
			break;
		case '\'': // quoted text
			cch = ParseFmtQuotedText (pszFmt, pszTime);
			if (cch < 0)
				return 0; // text not found
			break;
		case ' ':
			pszTime += wcsspn(pszTime, szSpace);
			break;
		default:
			// Check for matching separators.
			pszTime += wcsspn(pszTime, szSpace);
			for (int j = 0; j < cch; ++j)
				if (*pszTime++ != ch)
					return 0;
			pszTime += wcsspn(pszTime, szSpace);
			break;
		}
	}
	if (fPM && !f24hour)
		psti->hour += 12;
	return pszTime - pszTimeBeg;
}

/*----------------------------------------------------------------------------------------------
	ParseTimeString helper function to remove item from time format
----------------------------------------------------------------------------------------------*/
static void ParseTimeRemoveFormatItem (StrUniBuf & stuFmt, wchar item)
{
	const wchar * fmt = stuFmt.Chars();
	wchar ch, ans[80];
	Assert(wcslen(fmt) < SizeOfArray(ans));

	int i2 = 0, prev_item_index = 0;
	bool fQuote = false;
	for (int i = 0; (ch = fmt[i]) != 0; i++)
	{
		if (ch == '\'')
			fQuote = !fQuote;
		if (ch == item)
			i2 = prev_item_index;
		else
		{
			if (!fQuote && (ch == 'H' || ch == 'h' || ch == 'm' || ch == 's' || ch == 't'))
				prev_item_index = i + 1;
			ans[i2++] = ch;
		}
	}
	ans[i2] = '\0';
	stuFmt.Assign(ans);
}

/*----------------------------------------------------------------------------------------------
	Parse a time string into a SilTimeInfo data structure, using locale information to specify
	the format of the time value in the string.

	@param pszTime Character string ostensibly containing a time.
	@param psti Pointer to an SilTimeInfo data structure used for output.

	@return The number of characters consumed from pszDateString to fill psti, or zero if an
					error occurs.
----------------------------------------------------------------------------------------------*/
static int ParseTimeString(const wchar * pszTime, SilTimeInfo * psti)
{
	AssertPsz(pszTime);
	AssertPtr(psti);

	int cch;

	CheckForLocaleChange();
	if (!g_rgstuSTimeFormat.Length())
	{
		achar rgch[80];
		cch = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_STIMEFORMAT, rgch, 80);
		g_rgstuSTimeFormat.Assign(rgch);
		cch = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_S1159, rgch, 80);
		g_rgstuS1159.Assign(rgch);
		cch = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_S2359, rgch, 80);
		g_rgstuS2359.Assign(rgch);
		cch = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SDECIMAL, rgch, 80);
		g_rgstuSDecimal.Assign(rgch);
	}
	StrUniBuf stubSTimeFormat(g_rgstuSTimeFormat.Chars());
	cch = ParseTimeStringTry(stubSTimeFormat, pszTime, psti);
	if (cch) return cch;

	ParseTimeRemoveFormatItem (stubSTimeFormat, 's'); // remove seconds
	cch = ParseTimeStringTry(stubSTimeFormat, pszTime, psti);
	if (cch) return cch;

	ParseTimeRemoveFormatItem (stubSTimeFormat, 'm'); // no minutes or seconds
	cch = ParseTimeStringTry(stubSTimeFormat, pszTime, psti);
	return cch;
}

/*----------------------------------------------------------------------------------------------
	Convert the date/time string psz to an SilTime.

	@param psz Character string ostensibly containing a date and/or time.

	@return SilTime value corresponding to the date and time given in the string, or zero if
					the string contains an illegal character.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	SilTime StrUtil::ParseDateTime(const XChar * psz)
{
	AssertPsz(psz);

	int cch = 0;
	while (psz[cch])
		++cch;
	SilTime stim;
	return ParseDateTime(psz, cch, &stim, NULL) ? stim : 0;
}


/*----------------------------------------------------------------------------------------------
	Convert the date/time string (prgch, cch) to an SilTime. The result is placed in pstim.
	When non-NULL, pcchRead returns the number of characters consumed from the input.
	This may be less than cch if an illegal character occurs in the string, or if the
	date/time string is part of a larger string. The function returns 'true' when cch
	characters are consumed; false otherwise.

	@param prgch Pointer to an array of characters ostensibly containing a date and/or time.
	@param cch Number of characters in prgch.
	@param pstim Pointer to an SilTime object used to return the parsed value.
	@param pcchRead Pointer to an integer used to return the number of characters consumed in
					prgch to produce *pstim.

	@return True if all of the characters in prgch are consumed parsing the date and time,
					otherwise false.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	bool StrUtil::ParseDateTime(const XChar * prgch, int cch, SilTime * pstim, int * pcchRead)
{
	AssertArray(prgch, cch);
	AssertPtr(pstim);
	AssertPtrN(pcchRead);

	SilTimeInfo sti = { 0,0,0,0,0,0,0,0 };
	StrUni stuT(prgch, cch); // Do all parsing using unicode.
	int cchDate = ParseDateString(stuT.Chars(), &sti);
	int cchTime = ParseTimeString(stuT.Chars() + cchDate, &sti);
	if (cchTime && !cchDate)
	{
		// In case the date follows the time.
		cchDate = ParseDateString(stuT.Chars() + cchTime, &sti);
	}
	if (cchDate && sti.mday == 0)
		sti.mday = 1;
	if (cchDate || cchTime)
	{
		// Consume any trailing whitespace.
		const XChar * pchLim = prgch + cch;
		const XChar * pch = prgch + cchDate + cchTime;
		for ( ; pch < pchLim; ++pch)
		{
			if (stbSpace.FindCh(*pch) == -1)
				break;
			++cchDate;
		}
	}
	SilTime stim(sti);
	*pstim = stim;
	if (pcchRead)
		*pcchRead = cchDate + cchTime;
	return cchDate + cchTime == cch;
}

/*----------------------------------------------------------------------------------------------
	Convert the date string psz to an SilTime.

	@param psz Character string ostensibly containing a date.

	@return SilTime value corresponding to the date given in the string, or zero if
					the string contains an illegal character.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	SilTime StrUtil::ParseDate(const XChar * psz)
{
	AssertPsz(psz);

	int cch = 0;
	while (psz[cch])
		++cch;
	SilTime stim;
	return ParseDate(psz, cch, &stim, NULL) ? stim : 0;
}


/*----------------------------------------------------------------------------------------------
	Convert the date string (prgch, cch) to an SilTime. The result is placed in pstim.
	When non-NULL, pcchRead returns the number of characters consumed from the input.
	This may be less than cch if an illegal character occurs in the string, or if the
	date string is part of a larger string. The function returns 'true' when cch
	characters are consumed; false otherwise.

	@param prgch Pointer to an array of characters ostensibly containing a date.
	@param cch Number of characters in prgch.
	@param pstim Pointer to an SilTime object used to return the parsed value.
	@param pcchRead Pointer to an integer used to return the number of characters consumed in
					prgch to produce *pstim.

	@return True if all of the characters in prgch are consumed parsing the date,
					otherwise false.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	bool StrUtil::ParseDate(const XChar * prgch, int cch, SilTime * pstim, int * pcchRead)
{
	AssertArray(prgch, cch);
	AssertPtr(pstim);
	AssertPtrN(pcchRead);

	SilTimeInfo sti = { 0,0,0,0,0,0,0,0 };
	StrUni stuT(prgch, cch); // Do all parsing using unicode.
	int cchDate = ParseDateString(stuT.Chars(), &sti);
	if (cchDate && sti.mday == 0)
		sti.mday = 1;
	if (cchDate)
	{
		// Consume any trailing whitespace.
		const XChar * pchLim = prgch + cch;
		const XChar * pch = prgch + cchDate;
		for ( ; pch < pchLim; ++pch)
		{
			if (stbSpace.FindCh(*pch) == -1)
				break;
			++cchDate;
		}
	}
	SilTime stim(sti);
	*pstim = stim;
	if (pcchRead)
		*pcchRead = cchDate;
	return cchDate == cch;
}


/*----------------------------------------------------------------------------------------------
	Convert the time string psz to an SilTime.

	@param psz Character string ostensibly containing a time.

	@return SilTime value corresponding to the time given in the string, or zero if
					the string contains an illegal character.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	SilTime StrUtil::ParseTime(const XChar * psz)
{
	AssertPsz(psz);

	int cch = 0;
	while (psz[cch])
		++cch;
	SilTime stim;
	return ParseTime(psz, cch, &stim, NULL) ? stim : 0;
}


/*----------------------------------------------------------------------------------------------
	Convert the time string (prgch, cch) to an SilTime. The result is placed in pstim.
	When non-NULL, pcchRead returns the number of characters consumed from the input.
	This may be less than cch if an illegal character occurs in the string, or if the
	time string is part of a larger string. The function returns 'true' when cch
	characters are consumed; false otherwise.

	@param prgch Pointer to an array of characters ostensibly containing a time.
	@param cch Number of characters in prgch.
	@param pstim Pointer to an SilTime object used to return the parsed value.
	@param pcchRead Pointer to an integer used to return the number of characters consumed in
					prgch to produce *pstim.

	@return True if all of the characters in prgch are consumed parsing the time,
					otherwise false.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	bool StrUtil::ParseTime(const XChar * prgch, int cch, SilTime * pstim, int * pcchRead)
{
	AssertArray(prgch, cch);
	AssertPtr(pstim);
	AssertPtrN(pcchRead);

	SilTimeInfo sti = { 0,0,0,0,0,0,0,0 };
	StrUni stuT(prgch, cch); // Do all parsing using unicode.
	int cchTime = ParseTimeString(stuT.Chars(), &sti);
	if (cchTime)
	{
		// Consume any trailing whitespace.
		const XChar * pchLim = prgch + cch;
		const XChar * pch = prgch + cchTime;
		for ( ; pch < pchLim; ++pch)
		{
			if (stbSpace.FindCh(*pch) == -1)
				break;
			++cchTime;
		}
	}
	SilTime stim(sti);
	*pstim = stim;
	if (pcchRead)
		*pcchRead = cchTime;
	return cchTime == cch;
}

/*----------------------------------------------------------------------------------------------
	Convert the date string (pszDate) to an SilTime using the given format (pszFmt). The result
	is placed in pstim.

	@param pszDate Pointer to a date string, ostensibly in the given format.
	@param pszFmt Pointer to a date format string.
	@param pstim Pointer to an SilTime object used to return the parsed value.

	@return The number of characters consumed from the input, or zero if an error occurs.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	int StrUtil::ParseDateWithFormat(const XChar * pszDate, const XChar * pszFmt,
		SilTime * pstim)
{
	AssertPsz(pszDate);
	AssertPsz(pszFmt);
	AssertPtr(pstim);

	StrUni stuFmt(pszFmt);
	StrUni stuDate(pszDate);
	SilTimeInfo sti = { 0,0,0,0,0,0,0,0 };
	int cch = ParseFormattedDate(stuFmt.Chars(), stuDate.Chars(), &sti);
	if (cch)
	{
		SilTime stim(sti);
		*pstim = stim;
	}
	return cch;
}

/*----------------------------------------------------------------------------------------------
	Function to create appropriate labels for Undo tasks, with the action names coming
	from the stid.
----------------------------------------------------------------------------------------------*/
void StrUtil::MakeUndoRedoLabels(int stid, StrUni * pstuUndo, StrUni * pstuRedo)
{
	StrUni stuRes(stid);
	StrUni stuUndo;
	StrUni stuRedo;
	int ichNl = stuRes.FindCh(L'\n');
	if (ichNl == -1)
	{
		// Insert the string (describing the task) into the undo/redo frames.
		StrUni stuUndoFrm(kstidUndoFrame);
		StrUni stuRedoFrm(kstidRedoFrame);
		pstuUndo->Format(stuUndoFrm.Chars(), stuRes.Chars());
		pstuRedo->Format(stuRedoFrm.Chars(), stuRes.Chars());
	}
	else
	{
		// The resource string contains two separate strings separated by a new-line.
		// The first half is for Undo and the second for Redo.
		pstuUndo->Replace(0, 0, stuRes.Chars(), ichNl);
		stuRes.Replace(0, ichNl + 1, L"", 0);
		*pstuRedo = stuRes;
	}
}

/*----------------------------------------------------------------------------------------------
	Normalize the StrUni string.

	@param stu String to normalize.
	@param nm Normalization mode

	@return true if successful, false if unsuccessful, or invalid input.
----------------------------------------------------------------------------------------------*/
bool StrUtil::NormalizeStrUni(StrUni & stu, UNormalizationMode nm)
{
	if (!stu.Length() || nm == UNORM_NONE)
		return true;		// empty strings are normalized by definition.

	bool fOk = true;
	UNormalizationCheckResult x;
	UErrorCode uerr = U_ZERO_ERROR;
	x = unorm_quickCheck(stu.Chars(), stu.Length(), nm, &uerr);
	Assert(U_SUCCESS(uerr));
	if (U_SUCCESS(uerr) && x != UNORM_YES)
	{
		int32_t cchOut;
		Vector<wchar> vchOut;
		vchOut.Resize(stu.Length() * 2);
		cchOut = unorm_normalize(stu.Chars(), stu.Length(), nm, 0,
			vchOut.Begin(), vchOut.Size(), &uerr);
		if (uerr == U_BUFFER_OVERFLOW_ERROR)
		{
			vchOut.Resize(cchOut);
			uerr = U_ZERO_ERROR;
			cchOut = unorm_normalize(stu.Chars(), stu.Length(), nm, 0,
				vchOut.Begin(), vchOut.Size(), &uerr);
		}
		fOk = U_SUCCESS(uerr);
		Assert(fOk);
		if (fOk)
			stu.Assign(vchOut.Begin(), cchOut);
	}

	return fOk;
}


/*----------------------------------------------------------------------------------------------
	Lowercase the characters in a Unicode string.  This wraps the ICU function u_strToLower,
	which is safer than the built-in StrUni::ToLower method.  (There's no guarantee that
	case-conversion results in the same length string.)

	@param stu Reference to the StrUni string to lowercase.
	@param pszLocale Pointer to the ICU Locale string (eg, "en").  Defaults to NULL.
----------------------------------------------------------------------------------------------*/
void StrUtil::ToLower(StrUni & stu, const char * pszLocale)
{
	if (stu.Length() == 0)
		return;
	Assert(sizeof(UChar) == sizeof(wchar));
	Vector<UChar> vchLower;
	vchLower.Resize(stu.Length() * 2);		// double should be ample.
	UErrorCode uerr = U_ZERO_ERROR;
	int32_t cch = u_strToLower(vchLower.Begin(), vchLower.Size(),
		stu.Chars(), stu.Length(), pszLocale, &uerr);
	if (U_SUCCESS(uerr) && cch >= vchLower.Size())
	{
		vchLower.Resize(cch + 1);
		cch = u_strToLower(vchLower.Begin(), vchLower.Size(),
			stu.Chars(), stu.Length(), pszLocale, &uerr);
	}
	if (U_SUCCESS(uerr))
		stu.Assign(vchLower.Begin(), cch);
	else
		stu.ToLower();						// fall back to our default built-in operation...
}


/*------------------------------------------------------------------------------------------
	Trim white spaces from input string returning trimmed string in output string.

	@h3{Return value}
	@code{
		void.
	}
------------------------------------------------------------------------------------------*/
void StrUtil::TrimWhiteSpace(const char * pszIn, StrAnsi & staOut)
{
	StrAnsi staT;
	// Assign doesn't allow overlapping input, so make a copy of the input string if
	// the input and output strings are identical.
	if (pszIn == staOut.Chars())
	{
		staT.Assign(pszIn);
		pszIn = staT.Chars();
	}
	const char * psz = StrUtil::SkipLeadingWhiteSpace(pszIn);
	unsigned cch = StrUtil::LengthLessTrailingWhiteSpace(psz);
	staOut.Assign(psz, cch);
}

void StrUtil::TrimWhiteSpace(const wchar * pszIn, StrUni & stuOut)
{
	StrUni stuT;
	// Assign doesn't allow overlapping input, so make a copy of the input string if
	// the input and output strings are identical.
	if (pszIn == stuOut.Chars())
	{
		stuT.Assign(pszIn);
		pszIn = stuT.Chars();
	}
	const wchar * psz = StrUtil::SkipLeadingWhiteSpace(pszIn);
	unsigned cch = StrUtil::LengthLessTrailingWhiteSpace(psz);
	stuOut.Assign(psz, cch);
}

/*----------------------------------------------------------------------------------------------
	Convert the array of UTF-8 characters to UTF-16, and store them in the StrUni object.

	@param prgch Pointer to an array of character data; not NUL-terminated.
	@param cch Number of characters (bytes) in prgch.
	@param stu Reference to the output StrUni (UTF-16) object.
	@param fAppend Flag to append to stu rather than assign.
----------------------------------------------------------------------------------------------*/
void StrUtil::StoreUtf16FromUtf8(const char * prgch, int cch, StrUni & stu, bool fAppend)
{
	// Convert these chars to UTF-16 and store them.
	wchar szwBuffer[400];		// Use stack temp space for smaller amounts.
	Vector<wchar> vchw;
	wchar * prgchw;
	int cchw = CountUtf16FromUtf8(prgch, cch);
	if (cchw <= 400)
	{
		prgchw = szwBuffer;
	}
	else
	{
		vchw.Resize(cchw);		// Too much for stack: use temp heap storage.
		prgchw = &vchw[0];
	}
	SetUtf16FromUtf8(prgchw, cchw, prgch, cch);
	if (fAppend)
		stu.Append(prgchw, cchw);
	else
		stu.Assign(prgchw, cchw);
}

/*----------------------------------------------------------------------------------------------
	This method fixes the given string to be suitable for quoting in SQL query string.  The
	essential operation is replacing every single quote character with two single quote
	characters.  See LT-9115.

	@param stu Reference to the string to modify.
----------------------------------------------------------------------------------------------*/
void StrUtil::FixForSqlQuotedString(StrUni & stu)
{
	int ich = stu.FindCh('\'');
	while (ich >= 0)
	{
		stu.Replace(ich + 1, ich + 1, "'", 1);
		ich = stu.FindCh('\'', ich + 2);
	}
}

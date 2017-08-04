/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2004-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: DecodeUtf8_i.c
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	code for the DecodeUtf8 function.  This file should be #include'd after any necessary
	header files or typedefs.
----------------------------------------------------------------------------------------------*/
/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/

// kUtf8MinN is the minimum legal value for an N byte sequence
// kUtf8MaskN is the mask value of the first byte of an N byte sequence
// kUtf8FlagN is the flag value of the first byte of an N byte sequence
// kUnicodeMax is the maximum legal Unicode (UCS4) character
// kReplacementChar is a special Unicode value to signal an illegal char
// kSurrogate... are values related to handling surrogate pairs
// kByte... are values related to storing UTF-8 bytes
enum
{
	kUtf8Min1 = 0x00,
	kUtf8Min2 = 0x80,
	kUtf8Min3 = 0x800,
	kUtf8Min4 = 0x10000,
	kUtf8Min5 = 0x200000,
	kUtf8Min6 = 0x4000000
};

enum
{
	kUtf8Mask1 = 0x7F,
	kUtf8Mask2 = 0x1F,
	kUtf8Mask3 = 0x0F,
	kUtf8Mask4 = 0x07,
	kUtf8Mask5 = 0x03,
	kUtf8Mask6 = 0x01
};

enum
{
	kUtf8Flag1 = 0x00,
	kUtf8Flag2 = 0xC0,
	kUtf8Flag3 = 0xE0,
	kUtf8Flag4 = 0xF0,
	kUtf8Flag5 = 0xF8,
	kUtf8Flag6 = 0xFC
};

enum
{
	kUcs2Max    =     0xFFFF,
	kUtf16Max   =   0x10FFFF,
	kUnicodeMax = 0x7FFFFFFF,
	kReplacementChar = 0xFFFD
};

enum
{
	kSurrogateShift = 10,
	kSurrogateBase  = 0x0010000,
	kSurrogateMask  = 0x3FF,
	kSurrogateHighFirst = 0xD800,
	kSurrogateHighLast  = 0xDBFF,
	kSurrogateLowFirst  = 0xDC00,
	kSurrogateLowLast   = 0xDFFF
};

enum
{
	kByteMask = 0x3F,
	kByteMark = 0x80,
	kByteShift = 6
};


/***********************************************************************************************
	Functions
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Decode 1-6 bytes in the character string from UTF-8 format to Unicode (UCS-4).
	As a side-effect, cbOut is set to the number of UTF-8 bytes consumed.

	@param rgchUtf8 Pointer to a a character array containing UTF-8 data.
	@param cchUtf8 Number of characters in the array.
	@param cbOut Reference to an integer for holding the number of input (8-bit) characters
					consumed to produce the single output Unicode character.

	@return A single Unicode (UCS-4) character.  If an error occurs, return -1.
----------------------------------------------------------------------------------------------*/
long DecodeUtf8(const char * rgchUtf8, int cchUtf8, int & cbOut)
{
	// check for valid input
	AssertArray(rgchUtf8, cchUtf8);
	if ((cchUtf8 == 0) || (rgchUtf8[0] == '\0'))
	{
		cbOut = (cchUtf8) ? 1 : 0;
		return 0;
	}
	//
	// decode the first byte of the UTF-8 sequence
	//
	long lnUnicode;
	int cbExtra;
	int chsUtf8 = *rgchUtf8++ & 0xFF;
	if (chsUtf8 >= kUtf8Flag6)				// 0xFC
	{
		lnUnicode = chsUtf8 & kUtf8Mask6;
		cbExtra = 5;
	}
	else if (chsUtf8 >= kUtf8Flag5)			// 0xF8
	{
		lnUnicode = chsUtf8 & kUtf8Mask5;
		cbExtra = 4;
	}
	else if (chsUtf8 >= kUtf8Flag4)			// 0xF0
	{
		lnUnicode = chsUtf8 & kUtf8Mask4;
		cbExtra = 3;
	}
	else if (chsUtf8 >= kUtf8Flag3)			// 0xE0
	{
		lnUnicode = chsUtf8 & kUtf8Mask3;
		cbExtra = 2;
	}
	else if (chsUtf8 >= kUtf8Flag2)			// 0xC0
	{
		lnUnicode = chsUtf8 & kUtf8Mask2;
		cbExtra = 1;
	}
	else									// 0x00
	{
		lnUnicode = chsUtf8;
		cbExtra = 0;
	}
	if (cbExtra >= cchUtf8)
	{
		return -1;
	}

	switch (cbExtra)
	{
	case 5:
		lnUnicode <<= kByteShift;
		chsUtf8 = *rgchUtf8++ & 0xFF;
		if ((chsUtf8 & ~kByteMask) != 0x80)
			return -1;
		lnUnicode += chsUtf8 & kByteMask;
		// fall through
	case 4:
		lnUnicode <<= kByteShift;
		chsUtf8 = *rgchUtf8++ & 0xFF;
		if ((chsUtf8 & ~kByteMask) != 0x80)
			return -1;
		lnUnicode += chsUtf8 & kByteMask;
		// fall through
	case 3:
		lnUnicode <<= kByteShift;
		chsUtf8 = *rgchUtf8++ & 0xFF;
		if ((chsUtf8 & ~kByteMask) != 0x80)
			return -1;
		lnUnicode += chsUtf8 & kByteMask;
		// fall through
	case 2:
		lnUnicode <<= kByteShift;
		chsUtf8 = *rgchUtf8++ & 0xFF;
		if ((chsUtf8 & ~kByteMask) != 0x80)
			return -1;
		lnUnicode += chsUtf8 & kByteMask;
		// fall through
	case 1:
		lnUnicode <<= kByteShift;
		chsUtf8 = *rgchUtf8++ & 0xFF;
		if ((chsUtf8 & ~kByteMask) != 0x80)
			return -1;
		lnUnicode += chsUtf8 & kByteMask;
		break;
	case 0:
		// already handled
		break;
	default:
		Assert(false);
	}
	if ((ulong)lnUnicode > kUnicodeMax)
	{
		return -1;
	}
	cbOut = cbExtra + 1;
	return lnUnicode;
}

/*----------------------------------------------------------------------------------------------
	Decode bytes in the character string as UTF-8 characters, translating a number string into
	an integer.  Stop as soon as a non-digit Unicode character is encountered.

	@param pch Pointer to a a character array containing UTF-8 data.
	@param cch Number of characters in the array.

	@return An integer from the string, 0 if it does not start as a number.
----------------------------------------------------------------------------------------------*/
int Utf8NumberToInt(const char * pch, int cch)
{
	int nVal = 0;
	int cchUsed;
	const char * pchEnd = pch + cch;
	while (pch < pchEnd)
	{
		UChar32 ch = DecodeUtf8(pch, pchEnd - pch, cchUsed);
		pch += cchUsed;
		int32_t nDigit = u_charDigitValue(ch);
		if (nDigit < 0 || nDigit > 9)
			break;
		nVal = nVal * 10 + nDigit;
	}
	return nVal;
}

/*----------------------------------------------------------------------------------------------
	Decode 1-2 wchars in the character string from UTF-16 format to Unicode (UCS-4).
	As a side-effect, cchUsed is set to the number of 16-bit characters consumed.

	@param rgchUtf16 Pointer to a a character array containing UTF-16 data.
	@param cchUtf16 Number of characters in the array.
	@param cbOut Reference to an integer for holding the number of input (16-bit) characters
					consumed to produce the single output Unicode character.

	@return A single Unicode (UCS-4) character.  If an error occurs, return -1.
----------------------------------------------------------------------------------------------*/
long DecodeUtf16(const wchar_t * rgchUtf16, int cchUtf16, int & cchUsed)
{
	// check for valid input
	AssertArray(rgchUtf16, cchUtf16);
	if ((cchUtf16 == 0) || (rgchUtf16[0] == 0))
	{
		cchUsed = (cchUtf16) ? 1 : 0;
		return 0;
	}
	UChar32 ch1 = rgchUtf16[0];
	cchUsed = 1;
	if (kSurrogateHighFirst <= ch1 && ch1 <= kSurrogateHighLast)
	{
		if (cchUtf16 < 2)
			return -1;			// we expect both halves of a surrogate pair.
		UChar32 ch2 = rgchUtf16[1];
		if (kSurrogateLowFirst <= ch2 && ch2 <= kSurrogateLowLast)
		{
			ch1 -= kSurrogateHighFirst;
			ch1 <<= kSurrogateShift;
			ch1 += ch2 - kSurrogateLowFirst;
			ch1 += kSurrogateBase;
			cchUsed = 2;
			if (ch1 > kUnicodeMax)
				ch1 = kReplacementChar;
		}
		else
		{
			return -1;			// we expect both halves of a surrogate pair.
		}
	}
	else if (kSurrogateLowFirst <= ch1 && ch1 <= kSurrogateLowLast)
	{
		return -1;		// we expect the high half first.
	}

	return ch1;
}

/*----------------------------------------------------------------------------------------------
	Decode 1-2 wchars in the character string as UTF-16 characters, translating a number string
	into an integer.  Stop as soon as a non-digit Unicode character is encountered.

	@param pch Pointer to a a character array containing UTF-8 data.
	@param cch Number of characters in the array.

	@return An integer from the string, 0 if it does not start as a number.
----------------------------------------------------------------------------------------------*/
int Utf16NumberToInt(const wchar_t * pch, int cch)
{
	int nVal = 0;
	int cchUsed;
	const wchar_t * pchEnd = pch + cch;
	while (pch < pchEnd)
	{
		UChar32 ch = DecodeUtf16(pch, pchEnd - pch, cchUsed);
		pch += cchUsed;
		int32_t nDigit = u_charDigitValue(ch);
		if (nDigit < 0 || nDigit > 9)
			break;
		nVal = nVal * 10 + nDigit;
	}
	return nVal;
}


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkall.bat"
// End: (These 4 lines are useful to Steve McConnel.)

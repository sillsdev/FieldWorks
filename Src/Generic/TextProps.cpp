/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TextProps.cpp
Responsibility: Steve McConnel (was Shon Katzenberger)
Last reviewed:

Description:
	Implement text property byte strings.  This uses DataReader and DataWriter objects.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE


/*----------------------------------------------------------------------------------------------
	Find the given scp in the given byte string. Returns true iff the scp was found. In
	either case, ib is set to where the scp should be.
----------------------------------------------------------------------------------------------*/
static bool FindScp(int scp, const byte * prgb, int cb, int & ib)
{
	AssertArray(prgb, cb);
	int scpString;
	int nCmp;

	// See if it is already in the group.
	for (ib = 0; ib < cb; )
	{
		// Get the scp value for the current string position.
		scpString = TextProps::DecodeScp(prgb + ib);
		// Ignore data size when comparing the scp, hence the >> by 2.
		nCmp = (scp >> 2) - (scpString >> 2);
		if (nCmp <= 0)
		{
			if (nCmp < 0)
				return false;	// We've found an scp greater than the one we are looking for.
			return true;
		}

		// Skip this scp, since it is less than (and thus before) the one we are looking for.
		ib += TextProps::SizeScp(scpString) + TextProps::CbScpData(scpString);
		Assert(ib <= cb);
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Find the given scp in the given byte string. Returns true iff the scp was found. In
	either case, ib is set to where the scp should be.
----------------------------------------------------------------------------------------------*/
static bool FindScp(const byte * prgbScp, int cbScp, const byte * prgb, int cb, int & ib)
{
	AssertArray(prgbScp, cbScp);
	Assert(cbScp == 1 || cbScp == 2 || cbScp == 5);
	AssertArray(prgb, cb);

	int scp = TextProps::DecodeScp(prgbScp);
	return FindScp(scp, prgb, cb, ib);
}


/*----------------------------------------------------------------------------------------------
	Get the values for the scp from the binary text property storage.  scp must refer to an
	integer valued property.
----------------------------------------------------------------------------------------------*/
bool TextProps::FGetTextPropValue(int scp, const byte * prgb, int cb,
	int * pnVal1, int * pnVal2)
{
	AssertArray(prgb, cb);
	AssertPtrN(pnVal1);
	AssertPtrN(pnVal2);

	int ib;

	if (!FindScp(scp, prgb, cb, ib))
		return false;

	byte bT = prgb[ib];
	int cbData = TextProps::CbScpData(bT);
	Assert(cbData <= 2 * isizeof(int));

	ib += CbScpCode(bT);
	if (pnVal1)
	{
		*pnVal1 = 0;
		CopyBytes(prgb + ib, pnVal1, Min(cbData, isizeof(int)));
	}
	if (pnVal2)
	{
		*pnVal2 = 0;
		if (cbData > isizeof(int))
			CopyBytes(prgb + ib + isizeof(int), pnVal2, cbData - isizeof(int));
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Serialize the scp as it appears in byte strings.
	An scp is always positive. It is one of the constants used to identify a numeric
	property in a TsTextProps, shifted left two bits, and or'd with a 2-bit value which
	indicates how many bytes are required to store the value (2^n bytes, where n is the
	two-bit value).
	The number occupies one byte if it fits in 7 bits; this is indicated by the high bit
	of the first (only) byte being zero.
	The number occupies two bytes if it fits in 14 bits; this is indicated by the two high
	bits of the first byte having the value 10.
	The number occupies five bytes otherwise; this is indicated by the two high bits of the
	first byte having the value 11.
	In the second and third cases, the first byte also holds the low six bits of the number.
	The remaining one or four bytes holds the balance of the value, shifted six bits.
----------------------------------------------------------------------------------------------*/
int TextProps::EncodeScp(int scp, byte * prgb)
{
	AssertArray(prgb, kcbMaxScp);

	if (!(scp & 0xFFFFFF80))
	{
		prgb[0] = (byte)scp;
		return 1;
	}
	if (!(scp & 0xFFFFC000))
	{
		prgb[0] = (byte)((scp & 0x3F) | 0x80);
		prgb[1] = (byte)(scp >> 6);
		return 2;
	}
	prgb[0] = (byte)((scp & 0x3F) | 0xC0);
	scp = (scp >> 6) & 0x03FFFFFF;
	CopyBytes(&scp, prgb + 1, 4);

	return 5;
}


/*----------------------------------------------------------------------------------------------
	Decode an scp value which has been encoded into a byte array pointed to by prgb.
	Return the scp as an integer.
	This method uses a long-winded conversion in the case of a 5-byte scp. However, these
	are not currently used, and there seems little probability that it will be in the near
	future.  The chosen 5-byte conversion is thought to be robust in that it should work on
	all processors. WARNING: the 5-byte case has not been tested.
----------------------------------------------------------------------------------------------*/
int TextProps::DecodeScp(const byte * prgb)
{
	byte bT = *prgb;
	int scp;
	int cbScp = CbScpCode(bT);
	if (cbScp == 1)
	{
		scp = bT;
		return scp;
	}
	if (cbScp == 2)
		scp = *(prgb + 1); // Set up scp to 2nd byte of two.
	else if (cbScp == 5)
	{
		scp = 0;
		for (int i = 4; i > 0; --i)	// Begin with high byte.
		{
			scp <<= 8;
			scp |= *(prgb + i);
		}
	}
	else
	{
		Assert(false);
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	scp <<= 6;
	scp |= bT & 0x3F;	// Put in the relevant lowest bits from the first byte of the scp.
	return scp;
}


/*----------------------------------------------------------------------------------------------
	Serialize a string property length into 1, 2, or 4 bytes.

	@param cch Length of a string property.
	@param prgb Pointer to an output buffer.

	@return Number of bytes needed to serialize cch.
----------------------------------------------------------------------------------------------*/
int TextProps::EncodeCch(int cch, byte * prgb)
{
	AssertArray(prgb, 4);

	if (cch & 0xC0000000)
		return 0;					// ERROR: number negative or too big!
	if (!(cch & 0xFFFFFF80))
	{
		prgb[0] = (byte)cch;
		return 1;
	}
	if (!(cch & 0xFFFFC000))
	{
		prgb[0] = (byte)((cch & 0x3F) | 0x80);
		prgb[1] = (byte)(cch >> 6);
		return 2;
	}
	prgb[0] = (byte)((cch & 0x3F) | 0xC0);
	cch >>= 6;
	CopyBytes(&cch, prgb + 1, 3);
	return 4;
}


/*----------------------------------------------------------------------------------------------
	Read a persistent text property code for either an integer-valued property or a string-
	valued property.

	@param pdrdr Pointer to an object containing binary formatting information.
----------------------------------------------------------------------------------------------*/
int TextProps::ReadTextPropCode(DataReader * pdrdr)
{
	AssertPtr(pdrdr);
	byte bT;
	int scp;
	pdrdr->ReadBuf(&bT, 1);
	int cbScp = CbScpCode(bT);
	if (cbScp == 1)
	{
		scp = bT;
	}
	else
	{
		if (cbScp == 2)
		{
			scp = 0; // JT: vital because ReadBuf only sets one byte of it.
			pdrdr->ReadBuf(&scp, 1);
		}
		else if (cbScp == 5)
		{
			pdrdr->ReadBuf(&scp, 4);
		}
		else
		{
			Assert(false);		// THIS SHOULD NEVER HAPPEN!
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		scp <<= 6;
		scp |= bT & 0x3F;
	}
	return scp;
}


/*----------------------------------------------------------------------------------------------
	Read the length of the character string stored for a string-valued text property.

	@param pdrdr Pointer to an object containing binary formatting information.
----------------------------------------------------------------------------------------------*/
int TextProps::ReadStrPropLength(DataReader * pdrdr)
{
	AssertPtr(pdrdr);
	byte bT;
	int cch = 0;
	pdrdr->ReadBuf(&bT, 1);
	int cbCch = !(bT & 0x80) ? 1 : (bT & 0x40) ? 4 : 2;
	if (cbCch == 1)
	{
		cch = bT;
	}
	else
	{
		if (cbCch == 2)
		{
			cch = 0; // JT: vital because ReadBuf only sets one byte of it.
			pdrdr->ReadBuf(&cch, 1);
		}
		else if (cbCch == 4)
		{
			pdrdr->ReadBuf(&cch, 3);
		}
		else
		{
			Assert(false);		// THIS SHOULD NEVER HAPPEN!
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		cch <<= 6;
		cch |= bT & 0x3F;
	}
	return cch;
}


/*----------------------------------------------------------------------------------------------
	Read a single string valued text property.
----------------------------------------------------------------------------------------------*/
void TextProps::ReadTextStrProp(DataReader * pdrdr, TextStrProp * ptxsp)
{
	AssertPtr(pdrdr);
	AssertPtr(ptxsp);

	ptxsp->m_tpt = ReadTextPropCode(pdrdr);
	wchar * prgch;
	int cch = ReadStrPropLength(pdrdr);
	ptxsp->m_stuVal.SetSize(cch, &prgch);
	pdrdr->ReadBuf(prgch, cch * isizeof(wchar));
}


/*----------------------------------------------------------------------------------------------
	Write a single string valued text property.
----------------------------------------------------------------------------------------------*/
void TextProps::WriteTextStrProp(DataWriter * pdw, TextStrProp * ptxsp)
{
	AssertPtr(pdw);
	AssertPtr(ptxsp);

	int cch = ptxsp->m_stuVal.Length();
	if (!cch)
		return;
	WriteTextPropCode(pdw, ptxsp->m_tpt);
	WriteStrPropLength(pdw, cch);
	pdw->WriteBuf(ptxsp->m_stuVal.Chars(), cch * isizeof(wchar));
}

/*----------------------------------------------------------------------------------------------
	Decode a 30-bit number stored in 1, 2, or 4 bytes, and update the byte pointer if ppb is not
	NULL.
@line	rgb[0] & 0x80 == 0x00 => 7-bit value (rgb[0] & 0x7F)
@line	rgb[0] & 0xC0 == 0x80 => 14-bit value (rgb[1] << 6 | rgb[0] & 0x3F)
@line	rgb[0] & 0xC0 == 0xC0 => 30-bit value (rgb[1-3] << 6 | rgb[0] & 0x3F).  The top two
				bits of the result are always zero.

	@param rgb Pointer to a range of bytes that starts with an encoded 30-bit number.
	@param cb Number of bytes in rgb.
	@param ppb Address of a pointer to receive the address of the first byte in rgb that has
			not yet been decoded.  This will be rgb+1, rgb+2, or rgb+4 if no error occurs, or
			NULL if an error occurs.  ppb itself may be NULL, which prevents any information
			being passed back to the caller.

	@return The 32-bit number encoded at the beginning of rgb, or -1 if an error occurs.
----------------------------------------------------------------------------------------------*/
int TextProps::DecodeCch(const byte * rgb, int cb, const byte ** ppb)
{
	int nVal;
	int nT;
	if (cb < 1)
		return -1;
	nVal = rgb[0];
	if (nVal & 0x80)
	{
		if (cb < 2)
			return -1;
		if (nVal & 0x40)
		{
			if (cb < 4)
				return -1;
			cb = 4;
			nT = 0;
			memcpy(&nT, rgb + 1, 3);
		}
		else
		{
			cb = 2;
			nT = rgb[1] & 0xFF;
		}
		nVal &= 0x3F;
		nVal |= nT << 6;
	}
	else
	{
		cb = 1;
	}
	if (ppb)
		*ppb = rgb + cb;
	return nVal;
}

/*----------------------------------------------------------------------------------------------
	Decode a 32-bit number stored in 1, 2, or 5 bytes, and update the byte pointer if ppb is not
	NULL.
@line	rgb[0] & 0x80 == 0x00 => 7-bit value (rgb[0] & 0x7F).
@line	rgb[0] & 0xC0 == 0x80 => 14-bit value (rgb[1] << 6 | rgb[0] & 0x3F).
@line	rgb[0] & 0xC0 == 0xC0 => 32-bit value (rgb[1-5] << 6 | rgb[0] & 0x3F).  This ignores
			the top 6 bits of the most significant byte.

	@param rgb Pointer to a range of bytes that starts with an encoded 32-bit number.
	@param cb Number of bytes in rgb.
	@param ppb Address of a pointer to receive the address of the first byte in rgb that has
			not yet been decoded.  This will be rgb+1, rgb+2, or rgb+5 if no error occurs, or
			NULL if an error occurs.  ppb itself may be NULL, which prevents any information
			being passed back to the caller.

	@return The 32-bit number encoded at the beginning of rgb, or -1 if an error occurs.
----------------------------------------------------------------------------------------------*/
int TextProps::DecodeScp(const byte * rgb, int cb, const byte ** ppb)
{
	int nVal;
	int nT;
	if (ppb)
		*ppb = NULL;
	if (cb < 1)
		return -1;
	nVal = rgb[0];
	if (nVal & 0x80)
	{
		if (cb < 2)
			return -1;
		if (nVal & 0x40)
		{
			if (cb < 5)
				return -1;
			cb = 5;
			memcpy(&nT, rgb + 1, 4);
		}
		else
		{
			cb = 2;
			nT = rgb[1] & 0xFF;
		}
		nVal &= 0x3F;
		nVal |= nT << 6;
	}
	else
	{
		cb = 1;
	}
	if (ppb)
		*ppb = rgb + cb;
	return nVal;
}

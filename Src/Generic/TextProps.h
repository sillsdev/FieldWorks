/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TextProps.h
Responsibility: Steve McConnel (was Shon Katzenberger)
Last reviewed:

Description:
	Implement text property byte strings.  This uses DataReader and DataWriter objects.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TextProps_H
#define TextProps_H 1

namespace TextProps
{
	/*------------------------------------------------------------------------------------------
		Data for a single integer valued text property.  Both the "scp" and "tpt" versions of
		the type information is stored.
		Hungarian: txip
	------------------------------------------------------------------------------------------*/
	struct TextIntProp
	{
		int m_scp;		// Encodes the storage length of the data.  This is the persistent form.
		int m_tpt;		// Barebones type information.
		int m_nVal;
		int m_nVar;
	};

	/*------------------------------------------------------------------------------------------
		Data for a single string valued text property.
		Hungarian: txsp
	------------------------------------------------------------------------------------------*/
	struct TextStrProp
	{
		int m_tpt;			// Barebones type information -- this is also the persistent form.
		StrUni m_stuVal;	// Data value: not necessarily a "string" -- may be one or more
							// GUIDs or other binary data.
	};

	const int kcbMaxScp = 5;
	const int kcbMaxCch = 4;

	bool FGetTextPropValue(int scp, const byte * prgb, int cb, int * pnVal1, int * pnVal2);
	int EncodeScp(int scp, byte * prgb);
	int DecodeScp(const byte * prgb);
	int EncodeCch(int cch, byte * prgb);
	int ReadTextPropCode(DataReader * pdrdr);
	int ReadStrPropLength(DataReader * pdrdr);
	int ConvertTptToScp(int tpt);
	void ReadTextIntProp(DataReader * pdrdr, TextIntProp * ptip);
	void WriteTextIntProp(DataWriter * pdw, TextIntProp * ptip);
	void ReadTextStrProp(DataReader * pdrdr, TextStrProp * ptsp);
	void WriteTextStrProp(DataWriter * pdw, TextStrProp * ptsp);

	int DecodeCch(const byte * rgb, int cb, const byte ** ppb);
	int DecodeScp(const byte * rgb, int cb, const byte ** ppb);

	/*------------------------------------------------------------------------------------------
		Return the length of the scp code in a text prop byte string.
	------------------------------------------------------------------------------------------*/
	inline int CbScpCode(byte bT)
	{
		return !(bT & 0x80) ? 1 : (bT & 0x40) ? 5 : 2;
	}

	/*------------------------------------------------------------------------------------------
		Return the length of the scp data in a text prop byte string.
	------------------------------------------------------------------------------------------*/
	inline int CbScpData(int scp)
	{
		return 1 << (scp & 0x03);
	}

	/*------------------------------------------------------------------------------------------
		Returns the size of an scp (1, 2 or 5 bytes).
		Similar to EncodeScp, but does not produce the byte array.
	------------------------------------------------------------------------------------------*/
	inline int SizeScp(int scp)
	{
		if (!(scp & 0xFFFFFF80))
			return 1;
		if (!(scp & 0xFFFFC000))
			return 2;
		return 5;
	}


	/*------------------------------------------------------------------------------------------
		Write a persistent text property code for either an integer-valued property or a string-
		valued property.

		@param pdrdr Pointer to an object containing binary formatting information.
	------------------------------------------------------------------------------------------*/
	inline void WriteTextPropCode(DataWriter * pdw, int scp)
	{
		AssertPtr(pdw);
		Assert(scp);

		byte rgb[kcbMaxScp];
		int cb = EncodeScp(scp, rgb);
		Assert(cb <= kcbMaxScp);
		pdw->WriteBuf(rgb, cb);
	}


	/*------------------------------------------------------------------------------------------
		Write the length of the character string stored for a string-valued text property.

		@param pdw Pointer to an object containing binary formatting information.
	------------------------------------------------------------------------------------------*/
	inline void WriteStrPropLength(DataWriter * pdw, int cch)
	{
		AssertPtr(pdw);
		Assert(cch);

		byte rgb[kcbMaxCch];
		int cb = EncodeCch(cch, rgb);
		Assert(cb <= kcbMaxCch);
		pdw->WriteBuf(rgb, cb);
	}

}

#endif /*TextProps_H*/

/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TsMultiStr.h
Responsibility: Jeff Gayle.
Last reviewed: Not yet.

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef __TsMultiStr_H_
#define __TsMultiStr_H_


/*----------------------------------------------------------------------------------------------
	Hungarian: tse
----------------------------------------------------------------------------------------------*/
struct TsStrEntry
{
	int m_ws;
	ITsStringPtr m_qtss;
};


/*----------------------------------------------------------------------------------------------
	Hungarian: ztms
----------------------------------------------------------------------------------------------*/
class TsMultiString : public ITsMultiString
{
public:
	// Static Methods
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);

	// IUnknown Methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHODIMP_(ULONG) AddRef(void);
	STDMETHODIMP_(ULONG) Release(void);

	// ITsMultiString Methods.
	STDMETHOD(get_StringCount)(int * pctss);
	STDMETHOD(GetStringFromIndex)(int iws, int * pws, ITsString ** ptss);
	STDMETHOD(get_String)(int ws, ITsString ** pptss);
	STDMETHOD(putref_String)(int ws, ITsString * ptss);

#ifdef POSSIBLE_FUTURE_ENHANCEMENTS	// REVIEW ShonK: Do we need these?
	// Deserialize Interface methods.
	STDMETHOD(Deserialize)(IStream * pstrm);
	STDMETHOD(DeserializeRgb)(const OLECHAR * prgchTxt, int * pcchTxt, const BYTE * prgbFmt,
		int * pcbFmt);

	// Serialize Interface methods.
	STDMETHOD(SerializeFmt)(IStream * pstrm);
	STDMETHOD(SerializeFmtRgb)(BYTE * prgb, int cbMax, int * pcb,
		BSTR * pbstr);
#endif /*POSSIBLE_FUTURE_ENHANCEMENTS*/

protected:
	// Member variables
	int m_cref;

	// The list of alternate strings and their writing system values.
	Vector<TsStrEntry> m_vtse;

	TsMultiString();
	~TsMultiString();

	bool FindStrEntry(int ws, int * pienc);
};

#endif	// __TsMultiStr_H_

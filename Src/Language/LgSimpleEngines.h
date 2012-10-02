/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgSimpleEngines.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This file is intended to contain a number of simple engine implementations suitable for
	inclusion in the base DLL. Currently there is only one, a collater based on the OS
	built-in capabilities.

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LGSIMPLEENGINES_INCLUDED
#define LGSIMPLEENGINES_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: LgSystemCollater
Description:
	This class implements a simple collater which is initialized with a Windows language ID
	and compares strings or generates keys using the standard OS approach for that language.
Hungarian: syscoll
----------------------------------------------------------------------------------------------*/
class LgSystemCollater :
	public ILgCollatingEngine,
	public ISimpleInit
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// ISimpleInit Methods
	STDMETHOD(InitNew)(const BYTE * prgb, int cb);
	STDMETHOD(get_InitializationData)(BSTR * pbstr);

	//ILgCollatingEngine Methods
	STDMETHOD(get_SortKey)(BSTR bstrValue, LgCollatingOptions colopt, BSTR * pbstrKey);
	STDMETHOD(SortKeyRgch)(const OLECHAR * pch, int cchIn, LgCollatingOptions colopt,
		int cchMaxOut, OLECHAR * pchKey, int * pcchOut);
	STDMETHOD(Compare)(BSTR bstrValue1, BSTR bstrValue2, LgCollatingOptions colopt,
		int * pnVal);
	STDMETHOD(get_WritingSystemFactory)(ILgWritingSystemFactory ** pwsf);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);
	STDMETHOD(get_SortKeyVariant)(BSTR bstrValue, LgCollatingOptions colopt, VARIANT * psaKey);
	STDMETHOD(CompareVariant)(VARIANT saValue1, VARIANT saValue2, LgCollatingOptions colopt,
		int * pnVal);
	STDMETHOD(Open)(BSTR bstrLocale);
	STDMETHOD(Close)();

	// Member variable access

	// Other public methods

protected:
	// Member variables
	long m_cref;

	ILgWritingSystemFactoryPtr m_qwsf;

	LCID m_locale; // A windows Locale derived from the string passed to put_Setup().
		// Currently this class is only designed to handle simple sorting, so it
		// always uses SORT_DEFAULT (that is, it won't support Japanese, Chinese,
		// and Korean). Therefore we only need a language ID to produce a locale.
		// The langid is directly obtained by reading the setup string.

	// Static methods

	// Constructors/destructors/etc.
	LgSystemCollater();
	virtual ~LgSystemCollater();

	// Other protected methods
};

DEFINE_COM_PTR(LgSystemCollater);
#endif  //LGSIMPLEENGINES_INCLUDED

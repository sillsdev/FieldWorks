/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2002-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: LgIcuCharPropEngine.h
Responsibility: Charley Wesley
Last reviewed: Not yet.

Description:
	The character property engine provides character properties from the Unicode character
	property tables, using ICU to do so.  It is possible that a solution will be implemented
	that allows adding custom characters, but that is not supported at this time.

	Using ICU v.2.4, implements Unicode 3.2.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LGICUCHARPROPENGINE_INCLUDED
#define LGICUCHARPROPENGINE_INCLUDED

class LgIcuCharPropEngine :
	public ILgIcuCharPropEngine,
	public ISimpleInit
{
public:
	//:> Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	//:> Constructors/destructors/etc.
	LgIcuCharPropEngine();
	LgIcuCharPropEngine(BSTR bstrLanguage, BSTR bstrScript, BSTR bstrCountry, BSTR bstrVariant);
	virtual ~LgIcuCharPropEngine();

	// static method to return singleton Unicode character properties object.
	static HRESULT GetUnicodeCharProps(ILgCharacterPropertyEngine ** pplcpe);

	//:> IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

/*----------------------------------------------------------------------------------------------
	Guide to comments at the end of each function: Functions that have been updated to use
	overridden character props are denoted with a //. Functions that needed no work for the
	update but are correct as they stand now are denoted with a // as well. Ones that still
	need to be updated are denoted with a //**.

	NOTE: GetLineBreakProps/Status/Info are all marked as completed, but they don't support
	surrogate characters--in the overrides *or* in real characters.  This needs to be fixed
	for the second release.
----------------------------------------------------------------------------------------------*/

	//:> ISimpleInit Methods
	STDMETHOD(InitNew)(const BYTE * prgb, int cb);
	STDMETHOD(get_InitializationData)(BSTR * pbstr);

	//:> ILgCharacterPropertyEngine
	STDMETHOD(get_IsWordForming)(int ch, ComBool *pfRet);
	STDMETHOD(GetLineBreakProps)(const OLECHAR * prgchIn, int cchIn, byte * prglbpOut);//**
	STDMETHOD(GetLineBreakInfo)(const OLECHAR * prgchIn, int cchIn, int ichMin,
		int ichLim, byte * prglbsOut, int * pichBreak);//**
	STDMETHOD(put_LineBreakText)(OLECHAR * prgchIn, int cchMax); //**
	STDMETHOD(GetLineBreakText)(int cchMax, OLECHAR * prgchOut, int * pcchOut); //**
	STDMETHOD(LineBreakBefore)(int ichIn, int * pichOut, LgLineBreak * plbWeight); //**
	STDMETHOD(LineBreakAfter)(int ichIn, int * pichOut, LgLineBreak * plbWeight); //**

	//:> ILgIcuCharPropEngine
	STDMETHOD(Initialize)(BSTR bstrLanguage, BSTR bstrScript, BSTR bstrCountry, BSTR bstrVariant);
	STDMETHOD(InitCharOverrides)(BSTR bstrWsCharsList);

	//:> Member variable access

	//:> Other public methods

	bool IsPlausibleUnicodeRgch(OLECHAR * prgch, int cch); //
	bool IsPlausibleUnicodeCh(int ch); //
	void CheckUnicodeChar(int ch); //

protected:
	//:> Member variables
	long m_cref;
	Locale * m_pLocale;
	BreakIterator * m_pBrkit;
	UnicodeString m_usBrkIt;	// the string that BreakIterator operates on.
	Set<int> m_siWordformingOverrides;

	int m_cchBrkMax;  // Measures the size of the text in the BreakIterator.

#if WIN32
	IUnknownPtr m_qunkMarshaler;
#endif
	Mutex m_mutex;

	//:> Static members
	static const byte s_rglbs[32][32]; // Look-up table for GetLineBreakStatus.

	//:> Constructors/destructors/etc.

	void CleanupBreakIterator();
	void SetupBreakIterator();
	void ConsiderAdd(OLECHAR chFirst, OLECHAR chSecond, OLECHAR chThird);
};

#endif  // LGICUCHARPROPENGINE_INCLUDED

/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

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
	STDMETHOD(get_GeneralCategory)(int ch, LgGeneralCharCategory * pcc); //
	STDMETHOD(get_BidiCategory)(int ch, LgBidiCategory * pbic); //
	STDMETHOD(get_IsLetter)(int ch, ComBool *pfRet);
	STDMETHOD(get_IsWordForming)(int ch, ComBool *pfRet);
	STDMETHOD(get_IsPunctuation)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsNumber)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsSeparator)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsSymbol)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsMark)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsOther)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsUpper)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsLower)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsTitle)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsModifier)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsOtherLetter)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsOpen)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsClose)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsWordMedial)(int ch, ComBool *pfRet); //
	STDMETHOD(get_IsControl)(int ch, ComBool *pfRet); //
	STDMETHOD(get_ToLowerCh)(int ch, int *pch); //
	STDMETHOD(get_ToUpperCh)(int ch, int *pch); //
	STDMETHOD(get_ToTitleCh)(int ch, int *pch); //
	STDMETHOD(ToLower)(BSTR bstr, BSTR * pbstr); //
	STDMETHOD(ToUpper)(BSTR bstr, BSTR * pbstr); //
	STDMETHOD(ToTitle)(BSTR bstr, BSTR * pbstr); //
	STDMETHOD(ToLowerRgch)(OLECHAR * prgchIn,  int cchIn,
						   OLECHAR * prgchOut, int cchOut, int * cchRet); //
	STDMETHOD(ToUpperRgch)(OLECHAR * prgchIn,  int cchIn,
						   OLECHAR * prgchOut, int cchOut, int * cchRet); //
	STDMETHOD(ToTitleRgch)(OLECHAR * prgchIn,  int cchIn,
						   OLECHAR * prgchOut, int cchOut, int * cchRet); //
	STDMETHOD(get_IsUserDefinedClass)(int ch, int chClass, ComBool * pfRet); //
	STDMETHOD(get_SoundAlikeKey)(BSTR bstrValue, BSTR * pbstrKey); //
	STDMETHOD(get_CharacterName)(int ch, BSTR * pbstrName); //
	STDMETHOD(get_Decomposition)(int ch, BSTR * pbstr); //**
	STDMETHOD(DecompositionRgch)(int ch, int cchMax, OLECHAR * prgch,
		int * pcch, ComBool * pfHasDecomp); //**
	STDMETHOD(get_FullDecomp)(int ch, BSTR * pbstrOut);	 //**
	STDMETHOD(FullDecompRgch)(int ch, int cchMax, OLECHAR * prgch,
		int * pcch, ComBool * pfHasDecomp); //**
	STDMETHOD(get_NumericValue)(int ch, int * pn); //
	STDMETHOD(get_CombiningClass)(int ch, int * pn); //
	STDMETHOD(get_Comment)(int ch, BSTR * pbstr); //
	STDMETHOD(GetLineBreakProps)(const OLECHAR * prgchIn, int cchIn, byte * prglbpOut);//**
	STDMETHOD(GetLineBreakStatus)(const byte * prglbpIn, int cb, byte * prglbsOut);//**
	STDMETHOD(GetLineBreakInfo)(const OLECHAR * prgchIn, int cchIn, int ichMin,
		int ichLim, byte * prglbsOut, int * pichBreak);//**
//	STDMETHOD(NormalizeKd)(BSTR bstr, BSTR * pbstr); //**
	STDMETHOD(NormalizeKdRgch)(OLECHAR * prgchIn, int cchIn, OLECHAR * prgchOut,
		int cchMaxOut, int * pcchOut); //**
	STDMETHOD(NormalizeD)(BSTR bstr, BSTR * pbstr); //**
	STDMETHOD(NormalizeDRgch)(OLECHAR * prgchIn, int cchIn, OLECHAR * prgchOut,
		int cchMaxOut, int * pcchOut); //**
	STDMETHOD(StripDiacritics)(BSTR bstr, BSTR * pbstr); //
	STDMETHOD(StripDiacriticsRgch)(OLECHAR * prgchIn, int cchIn, OLECHAR * prgchOut,
		int cchMaxOut, int * pcchOut); //
	STDMETHOD(put_Locale)(int nLocale); //
	STDMETHOD(get_Locale)(int * pnLocale); //
	STDMETHOD(put_LineBreakText)(OLECHAR * prgchIn, int cchMax); //**
	STDMETHOD(GetLineBreakText)(int cchMax, OLECHAR * prgchOut, int * pcchOut); //**
	STDMETHOD(LineBreakBefore)(int ichIn, int * pichOut, LgLineBreak * plbWeight); //**
	STDMETHOD(LineBreakAfter)(int ichIn, int * pichOut, LgLineBreak * plbWeight); //**

	//:> ILgIcuCharPropEngine
	STDMETHOD(Initialize)(BSTR bstrLanguage, BSTR bstrScript, BSTR bstrCountry, BSTR bstrVariant);
	STDMETHOD(InitCharOverrides)(BSTR bstrWsCharsList);

	//:> Member variable access

	//:> Other public methods

	LgGeneralCharCategory GenCategory(int ch); //
	bool IsPlausibleUnicodeRgch(OLECHAR * prgch, int cch); //
	bool IsPlausibleUnicodeCh(int ch); //
	void CheckUnicodeChar(int ch); //
	void ConvertCase(BSTR bstr, BSTR * pbstr, LgGeneralCharCategory ccTo); //
	void ConvertCaseRgch(OLECHAR * prgchIn, int cchIn,
		OLECHAR * prgchOut, int cchOut, int * cchRet, LgGeneralCharCategory ccTo); //
	LgGeneralCharCategory ConvertCharCategory(int nICUCat); //
	LgBidiCategory ConvertBidiCategory(int nICUCat); //
	void OLECHARToUChar(OLECHAR *prgchIn, UChar *src, int cchIn); //
	void UCharToOLECHAR(UChar *src, OLECHAR *prgchIn, int srcLength); //
	void SetCharOverrideTables(OverriddenCharProps * pocpData); //
	void WCharToUChar(const wchar *wchIn, UChar *uchOut, int cchIn); //

protected:
	//:> Member variables
	long m_cref;
	Locale * m_pLocale;
	BreakIterator * m_pBrkit;
	UnicodeString m_usBrkIt;	// the string that BreakIterator operates on.
	Set<int> m_siWordformingOverrides;

	int m_cchBrkMax;  // Measures the size of the text in the BreakIterator.
	OverriddenCharProps * m_pocpData;

#if WIN32
	IUnknownPtr m_qunkMarshaler;
#endif
	Mutex m_mutex;

	//:> Static members
	static const byte s_rglbs[32][32]; // Look-up table for GetLineBreakStatus.

	//:> Constructors/destructors/etc.

	//:> Other protected methods
	CharacterPropertyObject * GetOverrideChar(UChar32 chIn); //
	void Normalize(UNormalizationMode mode, BSTR bstr, BSTR * pbstr);
	void NormalizeRgch(UNormalizationMode mode, OLECHAR * prgchIn, int cchIn,
		OLECHAR * prgchOut, int cchMaxOut, int * pcchOut);

	void CleanupBreakIterator();
	void SetupBreakIterator();
	void ConsiderAdd(OLECHAR chFirst, OLECHAR chSecond, OLECHAR chThird);
};

#endif  // LGICUCHARPROPENGINE_INCLUDED

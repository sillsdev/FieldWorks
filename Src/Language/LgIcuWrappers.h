/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgIcuWrappers.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LgIcuWrappersr_INCLUDED
#define LgIcuWrappersr_INCLUDED

DEFINE_COM_PTR(IEnumCodePage);
DEFINE_COM_PTR(IMultiLanguage2);

/*----------------------------------------------------------------------------------------------
Class: LgIcuConverterEnumerator
Description:
Hungarian: lcpe
----------------------------------------------------------------------------------------------*/
class LgIcuConverterEnumerator : ILgIcuConverterEnumerator
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	LgIcuConverterEnumerator();
	virtual ~LgIcuConverterEnumerator();

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

	// ILgIcuConverterEnumerator Methods
	STDMETHOD(get_Count)(int * pcconv);
	STDMETHOD(get_ConverterName)(int iconv, BSTR * pbstrName);
	STDMETHOD(get_ConverterId)(int iconv, BSTR * pbstrName);

protected:
	// Member variables
	long m_cref;
};

class LgIcuTransliteratorEnumerator : ILgIcuTransliteratorEnumerator
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	LgIcuTransliteratorEnumerator();
	virtual ~LgIcuTransliteratorEnumerator();

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

	// ILgIcuTransliteratorEnumerator Methods
	STDMETHOD(get_Count)(int * pctrans);
	STDMETHOD(get_TransliteratorName)(int itrans, BSTR * pbstrName);
	STDMETHOD(get_TransliteratorId)(int itrans, BSTR * pbstrName);

protected:
	// Member variables
	long m_cref;

	IEnumCodePagePtr m_qecp;
};

class LgIcuLocaleEnumerator : ILgIcuLocaleEnumerator
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	LgIcuLocaleEnumerator();
	virtual ~LgIcuLocaleEnumerator();

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

	// ILgIcuLocaleEnumerator Methods
	STDMETHOD(get_Count)(int * pclocale);
	STDMETHOD(get_Name)(int iloc, BSTR * pbstrName);
	STDMETHOD(get_Language)(int iloc, BSTR * pbstrName);
	STDMETHOD(get_Country)(int iloc,BSTR * pbstrName);
	STDMETHOD(get_Variant)(int iloc, BSTR * pbstrName);
	STDMETHOD(get_DisplayName)(int iloc, BSTR bstrLocaleName, BSTR * pbstrName);

protected:
	// Member variables
	long m_cref;

	// This is the list of locales returned by Locale::getAvailableLocales.
	// The memory does NOT belong to us and should NOT be deleted.
	// It is not clear how long we can hold onto this pointer; it is recommended that
	// instances of this class be created, used, and disposed of promptly.
	const Locale * m_prgLocales;
	int32_t m_clocale; // number of items in m_prgLocales.
};
DEFINE_COM_PTR(LgIcuResourceBundle);

class LgIcuResourceBundle : ILgIcuResourceBundle
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	LgIcuResourceBundle(ResourceBundle rb);
	virtual ~LgIcuResourceBundle();

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

	STDMETHOD(Init)(BSTR bstrPath, BSTR locale);
	STDMETHOD(get_Key)(BSTR * pbstrKey);
	STDMETHOD(get_String)(BSTR * pbstrString);
	STDMETHOD(get_Name)(BSTR * pbstrName);
	STDMETHOD(get_GetSubsection)(BSTR bstrSectionName,
		ILgIcuResourceBundle ** pprb);
	STDMETHOD(get_HasNext)(ComBool * pfHasNext);
	STDMETHOD(get_Next)(ILgIcuResourceBundle ** pprb);
	STDMETHOD(get_Size)(int * pcrb);
	STDMETHOD(get_StringEx)(int irb, BSTR * pbstr);

protected:
	// Member variables
	long m_cref;
	ResourceBundle m_rb;
};

#endif  //LgIcuWrappersr_INCLUDED

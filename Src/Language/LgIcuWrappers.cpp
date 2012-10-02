/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgIcuWrappers.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Currently contains implementation of
		LgIcuConverterEnumerator
		LgIcuTransliteratorEnumerator

-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

LgIcuConverterEnumerator::LgIcuConverterEnumerator()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();

}

LgIcuConverterEnumerator::~LgIcuConverterEnumerator()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language.LgIcuConverterEnumerator"),
	&CLSID_LgIcuConverterEnumerator,
	_T("SIL code page enumerator"),
	_T("Apartment"),
	&LgIcuConverterEnumerator::CreateCom);


void LgIcuConverterEnumerator::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgIcuConverterEnumerator> qlcpe;
	qlcpe.Attach(NewObj LgIcuConverterEnumerator());		// ref count initialy 1
	CheckHr(qlcpe->QueryInterface(riid, ppv));
}



//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgIcuConverterEnumerator::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgIcuConverterEnumerator *>(this));
	else if (riid == IID_ILgIcuConverterEnumerator)
		*ppv = static_cast<IUnknown *>(static_cast<ILgIcuConverterEnumerator *>(this));
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(static_cast<ILgIcuConverterEnumerator *>(this),
			IID_ILgIcuConverterEnumerator);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   ILgIcuConverterEnumerator Methods
//:>********************************************************************************************

STDMETHODIMP LgIcuConverterEnumerator::get_Count(int * pcconv)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pcconv);
	StrUtil::InitIcuDataDir();
	*pcconv = ucnv_countAvailable();
	END_COM_METHOD(g_fact, IID_ILgIcuConverterEnumerator);
}

STDMETHODIMP LgIcuConverterEnumerator::get_ConverterName(int iconv, BSTR * pbstrName)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrName);
	StrUtil::InitIcuDataDir();
	const char * canonicalName = ucnv_getAvailableName(iconv);
	UErrorCode err = U_ZERO_ERROR;
	const char * ianaName = ucnv_getStandardName(canonicalName, "IANA", &err);
	if (!ianaName)
		ianaName = canonicalName;
	if (!ianaName)
		return S_OK; // no useable name obtainable.
	// These names are guaranteed to be 7-bit ASCII (common chars in ASCII and EBCDIC)
	*pbstrName = AsciiToBstr(ianaName);
	END_COM_METHOD(g_fact, IID_ILgIcuConverterEnumerator);
}

STDMETHODIMP LgIcuConverterEnumerator::get_ConverterId(int iconv, BSTR * pbstrName)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrName);
	StrUtil::InitIcuDataDir();
	const char * canonicalName = ucnv_getAvailableName(iconv);
	// These names are guaranteed to be 7-bit ASCII (common chars in ASCII and EBCDIC)
	int cch = strlen(canonicalName);
	*pbstrName = ::SysAllocStringLen(NULL, cch);
	if (!*pbstrName)
		ThrowHr(WarnHr(E_OUTOFMEMORY));
	::MultiByteToWideChar(CP_ACP, 0, canonicalName, cch, *pbstrName, cch);
	END_COM_METHOD(g_fact, IID_ILgIcuConverterEnumerator);
}

//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

LgIcuTransliteratorEnumerator::LgIcuTransliteratorEnumerator()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();

}

LgIcuTransliteratorEnumerator::~LgIcuTransliteratorEnumerator()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factTrans(
	_T("SIL.Language.LgIcuTransliteratorEnumerator"),
	&CLSID_LgIcuTransliteratorEnumerator,
	_T("SIL code page enumerator"),
	_T("Apartment"),
	&LgIcuTransliteratorEnumerator::CreateCom);


void LgIcuTransliteratorEnumerator::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgIcuTransliteratorEnumerator> qlcpe;
	qlcpe.Attach(NewObj LgIcuTransliteratorEnumerator());		// ref count initialy 1
	CheckHr(qlcpe->QueryInterface(riid, ppv));
}



//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgIcuTransliteratorEnumerator::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgIcuTransliteratorEnumerator *>(this));
	else if (riid == IID_ILgIcuTransliteratorEnumerator)
		*ppv = static_cast<IUnknown *>(static_cast<ILgIcuTransliteratorEnumerator *>(this));
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(static_cast<ILgIcuTransliteratorEnumerator *>(this),
			IID_ILgIcuTransliteratorEnumerator);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   ILgIcuTransliteratorEnumerator Methods
//:>********************************************************************************************

STDMETHODIMP LgIcuTransliteratorEnumerator::get_Count(int * pctrans)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pctrans);
	StrUtil::InitIcuDataDir();
	*pctrans = Transliterator:: countAvailableIDs();

	END_COM_METHOD(g_factTrans, IID_ILgIcuTransliteratorEnumerator);
}

STDMETHODIMP LgIcuTransliteratorEnumerator::get_TransliteratorName(int itrans, BSTR * pbstrName)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrName);
	StrUtil::InitIcuDataDir();
	UnicodeString id = Transliterator::getAvailableID(itrans);
	UnicodeString name;
	Transliterator::getDisplayName(id, name);
	*pbstrName = UnicodeStringToBstr(name);
	END_COM_METHOD(g_factTrans, IID_ILgIcuTransliteratorEnumerator);
}

STDMETHODIMP LgIcuTransliteratorEnumerator::get_TransliteratorId(int itrans, BSTR * pbstrName)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrName);
	StrUtil::InitIcuDataDir();
	UnicodeString id = Transliterator::getAvailableID(itrans);
	*pbstrName = ::UnicodeStringToBstr(id);
	END_COM_METHOD(g_factTrans, IID_ILgIcuTransliteratorEnumerator);
}

//:>********************************************************************************************
//:>	   LgIcuResourceBundle
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

LgIcuResourceBundle::LgIcuResourceBundle(ResourceBundle rb) : m_rb(rb)
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();

}

LgIcuResourceBundle::~LgIcuResourceBundle()
{
	ModuleEntry::ModuleRelease();
}
//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factRb(
	_T("SIL.Language.LgIcuResourceBundle"),
	&CLSID_LgIcuResourceBundle,
	_T("SIL Icu Resource Bundle"),
	_T("Apartment"),
	&LgIcuResourceBundle::CreateCom);


void LgIcuResourceBundle::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgIcuResourceBundle> qlcpe;
	StrUtil::InitIcuDataDir();

	UErrorCode uerr = U_ZERO_ERROR;
	ResourceBundle rbt(NULL, Locale("en"), uerr);
	if (U_FAILURE(uerr))
		ThrowHr(E_FAIL);
	qlcpe.Attach(NewObj LgIcuResourceBundle(rbt));		// ref count initialy 1
	CheckHr(qlcpe->QueryInterface(riid, ppv));
}

//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgIcuResourceBundle::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgIcuResourceBundle *>(this));
	else if (riid == IID_ILgIcuResourceBundle)
		*ppv = static_cast<IUnknown *>(static_cast<ILgIcuResourceBundle *>(this));
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(static_cast<ILgIcuResourceBundle *>(this),
			IID_ILgIcuResourceBundle);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	   LgIcuLocaleEnumerator
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

LgIcuLocaleEnumerator::LgIcuLocaleEnumerator()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_prgLocales = Locale::getAvailableLocales(m_clocale);
}

LgIcuLocaleEnumerator::~LgIcuLocaleEnumerator()
{
	ModuleEntry::ModuleRelease();
}
//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factLocEnum(
	_T("SIL.Language.LgIcuLocaleEnumerator"),
	&CLSID_LgIcuLocaleEnumerator,
	_T("SIL Icu Locale Enumerator"),
	_T("Apartment"),
	&LgIcuLocaleEnumerator::CreateCom);


void LgIcuLocaleEnumerator::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgIcuLocaleEnumerator> qlcpe;
	StrUtil::InitIcuDataDir();
	qlcpe.Attach(NewObj LgIcuLocaleEnumerator());		// ref count initialy 1
	CheckHr(qlcpe->QueryInterface(riid, ppv));
}

//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgIcuLocaleEnumerator::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgIcuLocaleEnumerator *>(this));
	else if (riid == IID_ILgIcuLocaleEnumerator)
		*ppv = static_cast<IUnknown *>(static_cast<ILgIcuLocaleEnumerator *>(this));
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(static_cast<ILgIcuLocaleEnumerator *>(this),
			IID_ILgIcuLocaleEnumerator);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   ILgIcuLocaleEnumerator Methods
//:>********************************************************************************************

// Get the count of available converters.
STDMETHODIMP LgIcuLocaleEnumerator::get_Count(int * pclocale)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pclocale);
	*pclocale = m_clocale;
	END_COM_METHOD(g_factLocEnum, IID_ILgIcuLocaleEnumerator);
}


// Get the nth locale name. (ICU getName.)
STDMETHODIMP LgIcuLocaleEnumerator::get_Name(int iloc, BSTR * pbstrName)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrName);
	if (iloc >= m_clocale)
		ThrowHr(WarnHr(E_INVALIDARG));
	*pbstrName = AsciiToBstr(m_prgLocales[iloc].getName());
	END_COM_METHOD(g_factLocEnum, IID_ILgIcuLocaleEnumerator);
}

// Get the nth locale language identifier. (ICU getLanguage.)
STDMETHODIMP LgIcuLocaleEnumerator::get_Language(int iloc, BSTR * pbstrName)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrName);
	if (iloc >= m_clocale)
		ThrowHr(WarnHr(E_INVALIDARG));
	*pbstrName = AsciiToBstr(m_prgLocales[iloc].getLanguage());
	END_COM_METHOD(g_factLocEnum, IID_ILgIcuLocaleEnumerator);
}

// Get the nth locale country. (ICU getCountry.)
STDMETHODIMP LgIcuLocaleEnumerator::get_Country(int iloc,BSTR * pbstrName)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrName);
	if (iloc >= m_clocale)
		ThrowHr(WarnHr(E_INVALIDARG));
	*pbstrName = AsciiToBstr(m_prgLocales[iloc].getCountry());
	END_COM_METHOD(g_factLocEnum, IID_ILgIcuLocaleEnumerator);
}

// Get the nth locale variant. (ICU getVariant.)
STDMETHODIMP LgIcuLocaleEnumerator::get_Variant(int iloc, BSTR * pbstrName)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrName);
	if (iloc >= m_clocale)
		ThrowHr(WarnHr(E_INVALIDARG));
	*pbstrName = AsciiToBstr(m_prgLocales[iloc].getVariant());
	END_COM_METHOD(g_factLocEnum, IID_ILgIcuLocaleEnumerator);
}

// Get the display name of the locale represented by this enumerator.
// The display name will be in the selected locale if it is non-empty;
// pass null or an empty string to get the system default locale.
STDMETHODIMP LgIcuLocaleEnumerator::get_DisplayName(int iloc, BSTR bstrLocaleName,
	BSTR * pbstrName)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrName);
	ChkComBstrArgN(bstrLocaleName);
	if (iloc >= m_clocale)
		ThrowHr(WarnHr(E_INVALIDARG));
	UnicodeString ust;
	if (BstrLen(bstrLocaleName) == 0)
		m_prgLocales[iloc].getDisplayName(ust);
	else
	{
		StrAnsi staLocaleName(bstrLocaleName);
		Locale loc = Locale::createFromName(staLocaleName.Chars());
		m_prgLocales[iloc].getDisplayName(loc, ust);
	}

	*pbstrName = UnicodeStringToBstr(ust);
	END_COM_METHOD(g_factLocEnum, IID_ILgIcuLocaleEnumerator);

}

//:>********************************************************************************************
//:>	   ILgIcuResourceBundle Methods
//:>********************************************************************************************

// Initialize the root resource bundle. The path may be null to use the standard
// FieldWorks ICU data directory.
STDMETHODIMP LgIcuResourceBundle::Init(BSTR bstrPath, BSTR bstrLocale)
{
	BEGIN_COM_METHOD
	ChkComBstrArgN(bstrPath);
	ChkComBstrArg(bstrLocale);
	StrUtil::InitIcuDataDir();
	StrUni stuDir;
	UnicodeString usDir;
	if (bstrPath)
		stuDir = bstrPath;
		// Otherwise let ICU figure out the right directory
	usDir = stuDir.Chars();

	UErrorCode uerr = U_ZERO_ERROR;
	StrAnsi staLocale(bstrLocale);
	ResourceBundle rbt(usDir, Locale(staLocale.Chars()), uerr);
	if (U_FAILURE(uerr))
		ThrowHr(E_FAIL);
	m_rb = rbt;
	END_COM_METHOD(g_factRb, IID_ILgIcuResourceBundle);
}

// Get the key of the bundle. (Icu getKey.)
STDMETHODIMP LgIcuResourceBundle::get_Key(BSTR * pbstrKey)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrKey);

	*pbstrKey = AsciiToBstr(m_rb.getKey());

	END_COM_METHOD(g_factRb, IID_ILgIcuResourceBundle);
}

// Get the 'string' of the bundle. (Icu getString.)
STDMETHODIMP LgIcuResourceBundle::get_String(BSTR * pbstrString)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrString);
	UErrorCode uerr = U_ZERO_ERROR;
	UnicodeString ust = m_rb.getString(uerr);
	if (U_FAILURE(uerr))
		ThrowHr(E_FAIL);
	*pbstrString = UnicodeStringToBstr(ust);

	END_COM_METHOD(g_factRb, IID_ILgIcuResourceBundle);
}

// Get the name of the bundle. (Icu getName.) Note that the Key and String of the
// bundle are often more useful.
STDMETHODIMP LgIcuResourceBundle::get_Name(BSTR * pbstrName)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrName);
	*pbstrName = AsciiToBstr(m_rb.getName());
	END_COM_METHOD(g_factRb, IID_ILgIcuResourceBundle);
}

// Get another resource bundle for a subsection of this one. (Icu get.)
STDMETHODIMP LgIcuResourceBundle::get_GetSubsection(BSTR bstrSectionName,
	ILgIcuResourceBundle ** pprb)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pprb);
	StrAnsi staName(bstrSectionName);
	UErrorCode uerr = U_ZERO_ERROR;
	ResourceBundle rbt(m_rb.get(staName.Chars(), uerr));
	if (U_FAILURE(uerr))
		return S_OK;
	LgIcuResourceBundlePtr qrb;
	qrb.Attach(NewObj LgIcuResourceBundle(rbt));
	*pprb = qrb.Detach();

	END_COM_METHOD(g_factRb, IID_ILgIcuResourceBundle);
}

// Determine whether the bundle has more sub-resources accessible through get_Next.
// (Icu hasNext.)
STDMETHODIMP LgIcuResourceBundle::get_HasNext(ComBool * pfHasNext)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfHasNext);
	*pfHasNext = m_rb.hasNext();
	END_COM_METHOD(g_factRb, IID_ILgIcuResourceBundle);
}

// Get the 'next' subsection and advance. The first call to this retrieves the first
// item. Call only while HasNext returns true. (Icu getNext.)
STDMETHODIMP LgIcuResourceBundle::get_Next(ILgIcuResourceBundle ** pprb)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pprb);
	UErrorCode uerr = U_ZERO_ERROR;
	ResourceBundle rbt = m_rb.getNext(uerr);
	if (U_FAILURE(uerr))
		ThrowHr(E_FAIL);
	LgIcuResourceBundlePtr qrb;
	qrb.Attach(NewObj LgIcuResourceBundle(rbt));
	*pprb = qrb.Detach();
	END_COM_METHOD(g_factRb, IID_ILgIcuResourceBundle);
}

// Get the size of the bundle. (Icu getSize.)
STDMETHODIMP LgIcuResourceBundle::get_Size(int * pcrb)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pcrb);
	*pcrb = m_rb.getSize();
	END_COM_METHOD(g_factRb, IID_ILgIcuResourceBundle);
}

// Get the ith string. (Icu getStringEx.)
STDMETHODIMP LgIcuResourceBundle::get_StringEx(int irb, BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	UErrorCode uerr = U_ZERO_ERROR;
	UnicodeString ust = m_rb.getStringEx(irb, uerr);
	if (U_FAILURE(uerr))
		ThrowHr(E_FAIL);
	*pbstr = UnicodeStringToBstr(ust);
	END_COM_METHOD(g_factRb, IID_ILgIcuResourceBundle);
}

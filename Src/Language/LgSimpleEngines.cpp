/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgSimpleEngines.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This file contains implementation code for several of the simpler implementations of
	Language-related "engines." Specifically,
		- LgSystemCollater (ILgCollatingEngine)

-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

#if !WIN32
#include "LocaleIndex.h"
#endif

//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Constructors/Destructor
//:>********************************************************************************************

LgSystemCollater::LgSystemCollater()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

LgSystemCollater::~LgSystemCollater()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language1.LgSystemCollater"),
	&CLSID_LgSystemCollater,
	_T("SIL OS collater"),
	_T("Apartment"),
	&LgSystemCollater::CreateCom);


void LgSystemCollater::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgSystemCollater> qsyscoll;
	qsyscoll.Attach(NewObj LgSystemCollater());		// ref count initialy 1
	CheckHr(qsyscoll->QueryInterface(riid, ppv));
}



//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgSystemCollater::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgCollatingEngine *>(this));
	else if (riid == IID_ILgCollatingEngine)
		*ppv = static_cast<ILgCollatingEngine *>(this);
	else if (riid == IID_ISimpleInit)
		*ppv = static_cast<ISimpleInit *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<ILgCollatingEngine *>(this),
			IID_ISimpleInit, IID_ILgCollatingEngine);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   ISimpleInit Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize an instance from a ClassInitMoniker

	To create a suitable moniker, do something like this:

	int lid = MAKELANGID(LANG_DUTCH, SUBLANG_DUTCH); // see MAKELANGID doc for constants
	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	hr = qcim->InitNew(CLSID_LgSystemCollater, &lid, isizeof(lid));

	ENHANCE JohnT: do we need more error handling here? For example, should we fail here if the
	code page is not installed?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgSystemCollater::InitNew(const BYTE * prgb, int cb)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgb, cb);

	if (cb != isizeof(int))
		ThrowHr(WarnHr(E_INVALIDARG));

	int lid = *(int *)prgb;

	// See comments on m_locale.
#if WIN32
	m_locale = MAKELCID(lid, SORT_DEFAULT);
#else
	m_locale = lid; // SORT_DEFAULT = 0 thus LCID = lid
#endif

	END_COM_METHOD(g_fact, IID_ISimpleInit);
}

/*----------------------------------------------------------------------------------------------
	Return the initialization value previously set by InitNew.

	@param pbstr Pointer to a BSTR for returning the initialization data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgSystemCollater::get_InitializationData(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_ISimpleInit);
}


//:>********************************************************************************************
//:>	   ILgCollatingEngine Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Generate a sort key.
	If output pointer is null, just gives the needed length in *pcchOut.
	This is the main interesting routine, which uses Windows system code to actually make
	the sort key.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgSystemCollater::SortKeyRgch(const OLECHAR * pch, int cchIn,
	LgCollatingOptions colopt, int cchMaxOut, OLECHAR * pchKey, int * pcchOut)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(pch, cchIn);
	ChkComArrayArg(pchKey, cchMaxOut);
	if ((uint) colopt > (uint)fcoLim)
		ThrowInternalError(E_INVALIDARG, "Invalid collating options");
	ChkComOutPtr(pcchOut);

	// For no characters we just generate an empty key.
	if (cchIn == 0)
	{
		*pcchOut = 0;
		return S_OK;
	}

#if WIN32
	DWORD dwMapFlags = LCMAP_SORTKEY;
	if (colopt & fcoIgnoreCase)
		dwMapFlags |= NORM_IGNORECASE;

#ifdef UNICODE
	int cchOut = ::LCMapString(m_locale, dwMapFlags, pch, cchIn, pchKey, cchMaxOut);
	if (cchOut == 0)
	{
		LPVOID lpMsgBuf;
		DWORD dw = GetLastError();

		FormatMessage(
			FORMAT_MESSAGE_ALLOCATE_BUFFER |
			FORMAT_MESSAGE_FROM_SYSTEM |
			FORMAT_MESSAGE_IGNORE_INSERTS,
			NULL,
			dw,
			MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
			(LPTSTR) &lpMsgBuf,
			0, NULL );

		StrAnsi msg;
		msg.Format("LCMapString failed with locale %d (%x), input %s, error code %d (%x): %S",
			m_locale, m_locale, pch, dw, dw, lpMsgBuf);
		Warn(msg.Chars());

		LocalFree(lpMsgBuf);

		return E_FAIL;
	}
	*pcchOut = cchOut;
#else
	// NealA: XXX We should look at how this affects UNICODE compliance. pchsIn should
	//            probably be an achar, which means other things need to change to.
	// Windows documentation is conflicting on LCMapstring. The article "Unicode support
	// in Win32" says that there is an LCMapstringW function which works as early as
	// Win32s 1.2, and certainly in Win95. However, the documentation of LCMapString itself
	// says Unicode support (=> working _W version) on NT only. I decided to be safe and
	// assume it does not work on all the platforms we need. So, convert the string to
	// MBCS using the appropriate code page, then use the _A version of LCMapString.

	// According to TN059, WideCharToMultiByte never produces more than two
	// MBCS characters per Unicode character.
	// This routine throws a stack overflow exception if it can't get enough memory.
	// Note: a possible problem is strings large enough to make a stack overflow
	// occur, but MFC apparently things this strategy good enough for AFX.
	char * pchsIn = (char *)_alloca(cchIn * 2 + 1);
	int cchsIn; // count of chars in single-byte string

	// Figure code page from locale.

	achar rgchBuf[7]; // to receive string version of code page

	if (0 == ::GetLocaleInfo(m_locale, LOCALE_IDEFAULTANSICODEPAGE, rgchBuf, 7))
	{
		Warn("Could not get code page info");
		return E_FAIL;
	}
	int nCodePage = _tstoi(rgchBuf);
	// Check it is installed
	if(!::IsValidCodePage(nCodePage))
	{
		Warn("comparison code page not installed");
		return E_FAIL;
	}

	// Convert to MBCS using the code page derived from the locale.
	// WC_SEPCHARS is the default and fastest way of handling unconvertible characters.
	// It translates them into an arbitrary default character.
	cchsIn = ::WideCharToMultiByte(nCodePage, 0, pch, cchIn, pchsIn, cchIn * 2 + 1,
		NULL, // no default char override
		NULL); // don't want to know if default char used
	if (!cchsIn)
	{
		// conversion failed somehow...
		int nError = GetLastError();
		nError = nError; // to defeat warning
		return E_FAIL;
	}

	// We have room for this many bytes output.
	int cchsMaxOut = cchMaxOut * isizeof(OLECHAR);

	int cchsOut;
	cchsOut = ::LCMapStringA(m_locale, dwMapFlags, pchsIn, cchsIn,
		reinterpret_cast<achar *>(pchKey), cchsMaxOut);
	if (cchsOut == 0)
	{
		Warn("MapString failed");
		return E_FAIL;
	}

	// Make sure we have an even number of bytes. Add another null if necessary.
	// Note: is tempting just to discard the trailing null if we have an odd number
	// of bytes. However, then the size of buffer we need from the client can be one
	// more than the actual size of key we generate, which could be confusing, especially
	// if the client is using the size to allocate a buffer just the right size, and then
	// expects all its contents to be meaningful.
	char * pchsFix = (char *) pchKey;
	if (cchsOut & 1)
	{
		// If generating output, add null
		if (cchsMaxOut)
			*(pchsFix + cchsOut) = 0;
		cchsOut++;
	}

	int cchOut = cchsOut / 2; // Includes one or two trailing nulls
	if (cchsMaxOut)
	{
		// Windows returns a byte-oriented key, but this interface calls for a wide char
		// oriented one. Because Windows is little-endian, in each wide character the
		// least significant byte comes first. To get the right results from comparing
		// wide characters, we have to reverse the bytes. Do this only if actually
		// generating output.
		for (int i = 0; i< cchOut; i++)
		{
			char csTemp = *pchsFix;
			*pchsFix = *(pchsFix + 1);
			pchsFix++;
			*pchsFix = csTemp;
			pchsFix++;
		}
	}

	*pcchOut = cchOut;
#endif //UNICODE
#else //!WIN32
	// Create an ICU Locale for the current LCID
	std::string language = LocaleIndex::Instance().GetLanguage(m_locale);
	std::string country = LocaleIndex::Instance().GetCountry(m_locale);
	Locale locale(language.c_str(), country.c_str());
	// Obtain an ICU Collator for the ICU Locale
	UErrorCode status = U_ZERO_ERROR;
	Collator* collator = Collator::createInstance(locale, status);
	if (status != U_ZERO_ERROR)  // Do we have an ICU Collator?
	{
		Warn("Unable to obtain an ICU Collator");
		return E_FAIL;
	}
	// If needed, set the collator's strength attribute such that case will be ignored
	UCollationStrength strength = colopt & fcoIgnoreCase ? UCOL_SECONDARY : UCOL_DEFAULT;
	collator->setAttribute(UCOL_STRENGTH, strength, status);
	if (status != U_ZERO_ERROR)  // Was the strength attribute set successfully?
	{
		Warn("Unable to set ICU Collator's strength attribute");
		return E_FAIL;
	}
	// Generate the sort key
	int32_t cchOut = collator->getSortKey(pch, cchIn, reinterpret_cast<uint8_t*>(pchKey),
		cchMaxOut);
	if (cchMaxOut < cchOut)  // Was the supplied output buffer large enough?
	{
		Warn("Collator::getSortKey() requires a longer output buffer");
		return E_FAIL;
	}
	*pcchOut = cchOut;
	// TODO-Linux: delete collator?
#endif //WIN32

	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}

/*----------------------------------------------------------------------------------------------
	Generate the sort key as a BSTR
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgSystemCollater::get_SortKey(BSTR bstrValue, LgCollatingOptions colopt,
	BSTR * pbstrKey)
{
	BEGIN_COM_METHOD
	ChkComBstrArg(bstrValue);
	ChkComOutPtr(pbstrKey);

	HRESULT hr;
	int cchw;
	*pbstrKey = NULL;
	// Passing 0 and null just produces a length
	IgnoreHr(hr = SortKeyRgch(bstrValue, BstrLen(bstrValue), colopt, 0, NULL, &cchw));
	if (FAILED(hr))
		return hr;

	BSTR bstrOut;
	bstrOut = SysAllocStringLen(NULL, cchw);
	if (!bstrOut)
		return E_OUTOFMEMORY;
	IgnoreHr(hr = SortKeyRgch(bstrValue, BstrLen(bstrValue), colopt, cchw, bstrOut, &cchw));
	if (FAILED(hr))
	{
		SysFreeString(bstrOut);
		return hr;
	}
	*pbstrKey = bstrOut;
	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}


/*----------------------------------------------------------------------------------------------
	Do a direct string comparison.
	Note that, contrary to what the contract implies, this routine is not more
	efficient than the client just retrieving the keys and comparing them.
	OPTIMIZE: would we benefit significantly by implementing this using CompareString?
	Unfortunately, it is hard to avoid the need to do the WideCharToMultiByte conversion
	for the whole of both strings...
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgSystemCollater::Compare(BSTR bstrValue1, BSTR bstrValue2,
	LgCollatingOptions colopt, int * pnVal)
{
	BEGIN_COM_METHOD
	ChkComBstrArgN(bstrValue1);
	ChkComBstrArgN(bstrValue2);
	ChkComOutPtr(pnVal);

	HRESULT hr;

#if WIN32
	int cchw1;
	int cchw2;

	IgnoreHr(hr = SortKeyRgch(bstrValue1, BstrLen(bstrValue1), colopt, 0, NULL, &cchw1));
	if (FAILED(hr))
		return hr;
	IgnoreHr(hr = SortKeyRgch(bstrValue2, BstrLen(bstrValue2), colopt, 0, NULL, &cchw2));
	if (FAILED(hr))
		return hr;

	OLECHAR * pchKey1 = (OLECHAR *) _alloca(cchw1 * isizeof(OLECHAR));
	OLECHAR * pchKey2 = (OLECHAR *) _alloca(cchw2 * isizeof(OLECHAR));

	IgnoreHr(hr = SortKeyRgch(bstrValue1, BstrLen(bstrValue1), colopt, cchw1, pchKey1, &cchw1));
	if (FAILED(hr))
		return hr;
	IgnoreHr(hr = SortKeyRgch(bstrValue2, BstrLen(bstrValue2), colopt, cchw2, pchKey2, &cchw2));
	if (FAILED(hr))
		return hr;
	int nVal = wcsncmp(pchKey1, pchKey2, min(cchw1, cchw2));
	if (!nVal)
	{
		// equal as far as length of shortest key
		if (cchw1 < cchw2)
			nVal = -1;
		else if (cchw1 > cchw2)
			nVal = 1;
	}
	*pnVal = nVal;
#else //!WIN32
	// Create an ICU Locale for the current LCID
	std::string language = LocaleIndex::Instance().GetLanguage(m_locale);
	std::string country = LocaleIndex::Instance().GetCountry(m_locale);
	Locale locale(language.c_str(), country.c_str());
	// Obtain an ICU Collator for the ICU Locale
	UErrorCode status = U_ZERO_ERROR;
	Collator* collator = Collator::createInstance(locale, status);
	if (status != U_ZERO_ERROR)  // Do we have an ICU Collator?
	{
		Warn("Unable to obtain an ICU Collator");
		return E_FAIL;
	}
	// If needed, set the collator's strength attribute such that case will be ignored
	UCollationStrength strength = colopt & fcoIgnoreCase ? UCOL_SECONDARY : UCOL_DEFAULT;
	collator->setAttribute(UCOL_STRENGTH, strength, status);
	if (status != U_ZERO_ERROR)  // Was the strength attribute set successfully?
	{
		Warn("Unable to set ICU Collator's strength attribute");
		return E_FAIL;
	}
	// Compare the strings
	UCollationResult result = collator->compare(bstrValue1, BstrLen(bstrValue1),
		bstrValue2, BstrLen(bstrValue2), status);
	if (status != U_ZERO_ERROR)  // Was string comparison successful?
	{
		Warn("Unable to compare strings with ICU Collator");
		return E_FAIL;
	}
	switch(result)
	{
		case UCOL_LESS:
			*pnVal = -1;
			break;
		case UCOL_GREATER:
			*pnVal = 1;
			break;
		default:  // UCOL_EQUAL
			*pnVal = 0;
	}
	// TODO-Linux: delete collator?
#endif
	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}


/*----------------------------------------------------------------------------------------------
	Get the writing system factory for this simple collator.

	@param ppwsf Address where to store a pointer to the writing system factory that stores/produces
					this old writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgSystemCollater::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppwsf);
	AssertPtr(m_qwsf);

	*ppwsf = m_qwsf;
	if (*ppwsf)
		(*ppwsf)->AddRef();

	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}

/*----------------------------------------------------------------------------------------------
	Set the writing system factory for this simple collator.

	@param pwsf Pointer to the writing system factory that stores/produces this old writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgSystemCollater::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pwsf);

	m_qwsf = pwsf;

	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}


/*----------------------------------------------------------------------------------------------
	Generate the sort key as a "SAFEARRAY".
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgSystemCollater::get_SortKeyVariant(BSTR bstrValue, LgCollatingOptions colopt,
	VARIANT * psaKey)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Do a direct string comparison using "SAFEARRAY"s.
	Note that, contrary to what the contract implies, this routine is not more
	efficient than the client just retrieving the keys and comparing them.
	OPTIMIZE: would we benefit significantly by implementing this using CompareString?
	Unfortunately, it is hard to avoid the need to do the WideCharToMultiByte conversion
	for the whole of both strings...
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgSystemCollater::CompareVariant(VARIANT saValue1, VARIANT saValue2,
	LgCollatingOptions colopt, int * pnVal)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Initialize the collating engine to the given locale.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgSystemCollater::Open(BSTR bstrLocale)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Close any open collating engine to the given locale.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgSystemCollater::Close()
{
	return E_NOTIMPL;
}

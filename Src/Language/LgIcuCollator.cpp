/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgIcuCollator.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

#if WIN32

//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Constructors/Destructor
//:>********************************************************************************************

LgIcuCollator::LgIcuCollator()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

LgIcuCollator::~LgIcuCollator()
{
	Close();
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language1.LgIcuCollator"),
	&CLSID_LgIcuCollator,
	_T("SIL Icu collater"),
	_T("Apartment"),
	&LgIcuCollator::CreateCom);


void LgIcuCollator::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgIcuCollator> qico;
	qico.Attach(NewObj LgIcuCollator());		// ref count initialy 1
	CheckHr(qico->QueryInterface(riid, ppv));
}



//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgIcuCollator::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgCollatingEngine *>(this));
	else if (riid == IID_ILgCollatingEngine)
		*ppv = static_cast<ILgCollatingEngine *>(this);
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
//:>	   ILgCollatingEngine Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Generate a sort key.
	If output pointer is null, just gives the needed length in *pcchOut.
	This is the main interesting routine, which uses Windows system code to actually make
	the sort key.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCollator::SortKeyRgch(const OLECHAR * pch, int cchIn,
	LgCollatingOptions colopt, int cchMaxOut, OLECHAR * pchKey, int * pcchOut)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(pch, cchIn);
	ChkComArrayArg(pchKey, cchMaxOut);

	return E_NOTIMPL;

	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}

/*----------------------------------------------------------------------------------------------
	Generate the sort key as a BSTR
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCollator::get_SortKey(BSTR bstrValue, LgCollatingOptions colopt,
	BSTR * pbstrKey)
{
	BEGIN_COM_METHOD
	ChkComBstrArg(bstrValue);
	ChkComOutPtr(pbstrKey);

	return E_NOTIMPL;

	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}


/*----------------------------------------------------------------------------------------------
	Generate the sort key as a byte *
----------------------------------------------------------------------------------------------*/
byte * LgIcuCollator::GetSortKey(BSTR bstrValue, byte* prgbKey, int32_t* pcbKey)
{
	byte * pbKey;
	int32_t crgbKey = *pcbKey;
	EnsureCollator();
	*pcbKey = m_pCollator->getSortKey(bstrValue, BstrLen(bstrValue),
		prgbKey, crgbKey);
	if (*pcbKey > crgbKey)
	{
		// sort key is too long, the caller has to pass us a bigger buffer.
		pbKey = NULL;
	}
	else
	{
		// sort key is less than 1024 bytes
		pbKey = prgbKey;
	}

	return pbKey;
}

/*----------------------------------------------------------------------------------------------
	Generate the sort key as a byte *, allocating memory if needed.

	The second argument is a fixed array of (*pcbKey)+1 bytes.  The fourth argument is a
	reference to a vector that can be dynamically allocated if more memory is needed.

	The return value points to either the fixed array or the beginning of the vector, whichever
	actually holds the entire sort key.  *pcbKey is set to the actual length (not counting the
	terminating NUL byte).
----------------------------------------------------------------------------------------------*/
byte * LgIcuCollator::GetSortKey(BSTR bstrValue, byte * prgbKey, int32_t * pcbKey,
	Vector<byte> & vbKey)
{
	int32_t cbKey = *pcbKey;
	byte * pbKey = GetSortKey(bstrValue, prgbKey, &cbKey);
	if (cbKey > *pcbKey)
	{
		int32_t cbKey1 = cbKey + 1;
		vbKey.Resize(cbKey1); // Sometimes it seems to answer one less.
		pbKey = GetSortKey(bstrValue, vbKey.Begin(), &cbKey1);
		Assert(cbKey == cbKey1);
		Assert(cbKey1 + 1 <= vbKey.Size()); // As long as it fits assume OK.
		vbKey[cbKey] = 0;		// pure paranoia -- it's supposed to be NUL-terminated.
	}
	*pcbKey = cbKey;
	return pbKey;
}

/*----------------------------------------------------------------------------------------------
	Do a direct string comparison.
	Note that, contrary to what the contract implies, this routine is not more
	efficient than the client just retrieving the keys and comparing them.
	OPTIMIZE: would we benefit significantly by implementing this using CompareString?
	Unfortunately, it is hard to avoid the need to do the WideCharToMultiByte conversion
	for the whole of both strings...
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCollator::Compare(BSTR bstrValue1, BSTR bstrValue2,
	LgCollatingOptions colopt, int * pnVal)
{
	BEGIN_COM_METHOD
	ChkComBstrArgN(bstrValue1);
	ChkComBstrArgN(bstrValue2);
	ChkComOutPtr(pnVal);

	EnsureCollator();

	int32_t cbKey1 = keySize;
	byte rgbKey1[keySize+1];
	Vector<byte> vbKey1;
	byte * pbKey1 = GetSortKey(bstrValue1, rgbKey1, &cbKey1, vbKey1);

	int32_t cbKey2 = keySize;
	byte rgbKey2[keySize+1];
	Vector<byte> vbKey2;
	byte * pbKey2 = GetSortKey(bstrValue2, rgbKey2, &cbKey2, vbKey2);

	*pnVal = strcmp((char *)pbKey1, (char *)pbKey2);

	return S_OK;

	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}


/*----------------------------------------------------------------------------------------------
	Get the writing system factory for this simple collator.

	@param ppwsf Address where to store a pointer to the writing system factory that stores (or
					produces) this writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCollator::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
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

	@param pwsf Pointer to the writing system factory that stores/produces this writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCollator::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pwsf);

	m_qwsf = pwsf;

	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}


/*----------------------------------------------------------------------------------------------
	Generate the sort key as a "SAFEARRAY".
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCollator::get_SortKeyVariant(BSTR bstrValue, LgCollatingOptions colopt,
	VARIANT * psaKey)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrValue);
	ChkComArgPtr(psaKey);
	psaKey->vt = VT_NULL;

	EnsureCollator();

	int32_t cbKey = keySize;
	byte rgbKey[keySize+1];
	Vector<byte> vbKey;
	byte * pbKey = GetSortKey(bstrValue, rgbKey, &cbKey, vbKey);

	// Allocate the safe array.
	SAFEARRAYBOUND rgsabound[1];
	rgsabound[0].lLbound = 0;
	rgsabound[0].cElements = cbKey;
	SAFEARRAY FAR * psa = ::SafeArrayCreate(VT_UI1, 1, rgsabound);
	// Copy the key data to the safe array.
	byte * pbOut;
	CheckHr(::SafeArrayAccessData(psa, (void HUGEP **)&pbOut));
	memcpy(pbOut, pbKey, cbKey);
	CheckHr(::SafeArrayUnaccessData(psa));
	// Push the safe array to the output pointer.
	psaKey->vt = VT_UI1 | VT_ARRAY;
	V_ARRAY(psaKey) = psa;

	return S_OK;

	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}

/*----------------------------------------------------------------------------------------------
	Do a direct string comparison using "SAFEARRAY"s.
	Note that, contrary to what the contract implies, this routine is not more
	efficient than the client just retrieving the keys and comparing them.
	OPTIMIZE: would we benefit significantly by implementing this using CompareString?
	Unfortunately, it is hard to avoid the need to do the WideCharToMultiByte conversion
	for the whole of both strings...
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCollator::CompareVariant(VARIANT saKey1, VARIANT saKey2,
	LgCollatingOptions colopt, int * pnVal)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnVal);
#if WIN32

	EnsureCollator();

	UINT cDim = ::SafeArrayGetDim(V_ARRAY(&saKey1));
	if (cDim != 1)
		return E_INVALIDARG;
	UINT cElemSize = ::SafeArrayGetElemsize(V_ARRAY(&saKey1));
	if (cElemSize != 1)
		return E_INVALIDARG;
	cDim = ::SafeArrayGetDim(V_ARRAY(&saKey2));
	if (cDim != 1)
		return E_INVALIDARG;
	cElemSize = ::SafeArrayGetElemsize(V_ARRAY(&saKey2));
	if (cElemSize != 1)
		return E_INVALIDARG;

	byte * pbKey1;
	CheckHr(::SafeArrayAccessData(V_ARRAY(&saKey1), (void HUGEP **)&pbKey1));
	byte * pbKey2;
	CheckHr(::SafeArrayAccessData(V_ARRAY(&saKey2), (void HUGEP **)&pbKey2));
	if (pbKey1 == NULL)
	{
		*pnVal = 1;
	}
	else if (pbKey2 == NULL)
	{
		*pnVal = -1;
	}
	else
	{
		*pnVal = strcmp((char *)pbKey1, (char *)pbKey2);
	}
	CheckHr(::SafeArrayUnaccessData(V_ARRAY(&saKey1)));
	CheckHr(::SafeArrayUnaccessData(V_ARRAY(&saKey2)));

	return S_OK;
#else
	// TODO-Linux: does this need porting?
	printf("Warning: using unported method LgIcuCollator::CompareVariant\n");
	fflush(stdout);
#endif //WIN32

	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}

/*----------------------------------------------------------------------------------------------
	Initialize the collating engine to the given locale.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCollator::Open(BSTR bstrLocale)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrLocale);

	if (m_pCollator)
	{
		delete m_pCollator;
		m_pCollator = NULL;
	}
	m_stuLocale = bstrLocale;
	EnsureCollator();
	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}


/*----------------------------------------------------------------------------------------------
	Ensure that we have a collator.
----------------------------------------------------------------------------------------------*/
void LgIcuCollator::EnsureCollator()
{
	if (m_pCollator)
		return; // we already have one.
	UErrorCode uerr = U_ZERO_ERROR;
	if (m_stuLocale.Length() == 0)
	{
		m_pCollator = Collator::createInstance(uerr);
	}
	else
	{
		StrAnsi sta(m_stuLocale.Bstr());
		char rgchLoc[128];
		int32_t cch = uloc_getName(sta.Chars(), rgchLoc, sizeof(rgchLoc), &uerr);
		Assert(cch < 128);
		rgchLoc[cch] = 0;
		if (U_FAILURE(uerr))
			ThrowHr(E_FAIL);
		const Locale loc = Locale::createFromName (rgchLoc);
		m_pCollator = Collator::createInstance (loc, uerr);
	}
	if (U_FAILURE(uerr))
		ThrowHr(E_FAIL);
}

/*----------------------------------------------------------------------------------------------
	Close any open collating engine.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCollator::Close()
{
	BEGIN_COM_METHOD;

	if (m_pCollator)
	{
		delete m_pCollator;
		m_pCollator = NULL;
	}
	m_stuLocale.Clear();
	return S_OK;

	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}

#endif

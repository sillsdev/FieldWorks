/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgCodePageEnumerator.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

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

LgCodePageEnumerator::LgCodePageEnumerator()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();

}

LgCodePageEnumerator::~LgCodePageEnumerator()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language.LgCodePageEnumerator"),
	&CLSID_LgCodePageEnumerator,
	_T("SIL code page enumerator"),
	_T("Apartment"),
	&LgCodePageEnumerator::CreateCom);


void LgCodePageEnumerator::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgCodePageEnumerator> qlcpe;
	qlcpe.Attach(NewObj LgCodePageEnumerator());		// ref count initialy 1
	CheckHr(qlcpe->QueryInterface(riid, ppv));
}



//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgCodePageEnumerator::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgCodePageEnumerator *>(this));
	else if (riid == IID_ILgCodePageEnumerator)
		*ppv = static_cast<IUnknown *>(static_cast<ILgCodePageEnumerator *>(this));
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(static_cast<ILgCodePageEnumerator *>(this),
			IID_ILgCodePageEnumerator);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   ILgCodePageEnumerator Methods
//:>********************************************************************************************

STDMETHODIMP LgCodePageEnumerator::Init()
{
	BEGIN_COM_METHOD
	IMultiLanguage2Ptr qml2;
	qml2.CreateInstance(CLSID_CMultiLanguage);
	CheckHr(qml2->EnumCodePages(MIMECONTF_VALID, 0, &m_qecp));
	END_COM_METHOD(g_fact, IID_ILgCodePageEnumerator);
}

STDMETHODIMP LgCodePageEnumerator::Next(int * pnId, BSTR * pbstrName)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pnId);
	ChkComOutPtr(pbstrName);
	if (!m_qecp)
		ThrowHr(WarnHr(E_UNEXPECTED));
	MIMECPINFO info;

	ULONG celt;
	CheckHr(m_qecp->Next(1, &info, &celt));
	if (celt == 1)
	{
		*pbstrName = SysAllocString(info.wszDescription);
		if (!*pbstrName)
			ThrowHr(WarnHr(E_OUTOFMEMORY));
		*pnId = info.uiCodePage;
	}
	END_COM_METHOD(g_fact, IID_ILgCodePageEnumerator);
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

LgLanguageEnumerator::LgLanguageEnumerator()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_ulCount = 0;
	m_iLangId = 0;
}

LgLanguageEnumerator::~LgLanguageEnumerator()
{
	ModuleEntry::ModuleRelease();
	if (m_prgLangIds)
	{
		CoTaskMemFree(m_prgLangIds);
		m_prgLangIds = NULL;
		m_ulCount = 0;
	}

}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factLang(
	_T("SIL.Language.LgLanguageEnumerator"),
	&CLSID_LgLanguageEnumerator,
	_T("SIL language enumerator"),
	_T("Apartment"),
	&LgLanguageEnumerator::CreateCom);


void LgLanguageEnumerator::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgLanguageEnumerator> qlcpe;
	qlcpe.Attach(NewObj LgLanguageEnumerator());		// ref count initialy 1
	CheckHr(qlcpe->QueryInterface(riid, ppv));
}



//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgLanguageEnumerator::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgLanguageEnumerator *>(this));
	else if (riid == IID_ILgLanguageEnumerator)
		*ppv = static_cast<IUnknown *>(static_cast<ILgLanguageEnumerator *>(this));
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(static_cast<ILgLanguageEnumerator *>(this),
			IID_ILgLanguageEnumerator);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   ILgLanguageEnumerator Methods
//:>********************************************************************************************

STDMETHODIMP LgLanguageEnumerator::Init()
{
	BEGIN_COM_METHOD
	ITfInputProcessorProfilesPtr qProfiles;
	qProfiles.CreateInstance(CLSID_TF_InputProcessorProfiles);

	m_iLangId = 0;
	HRESULT hr = qProfiles->GetLanguageList(&m_prgLangIds, &m_ulCount);
	if (!m_prgLangIds)
	{
		m_ulCount = 0;
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	return hr;

	END_COM_METHOD(g_factLang, IID_ILgLanguageEnumerator);
}

STDMETHODIMP LgLanguageEnumerator::Next(int * pnLoc, BSTR * pbstrName)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pnLoc);
	ChkComOutPtr(pbstrName);
	if (!m_prgLangIds)
		ThrowHr(WarnHr(E_UNEXPECTED));
	if (m_iLangId >= m_ulCount)
		return S_OK; // past end, leave *pnLoc 0 and *pbstrName null.

	wchar   szLangName[MAX_PATH];

	//Get the language name and print it to the debug window.
	if( 0 == GetLocaleInfoW(MAKELCID(m_prgLangIds[m_iLangId], SORT_DEFAULT),
				LOCALE_SLANGUAGE,
				szLangName,
				MAX_PATH))
	{
		//InKey can give values that are not supported by the operating system.
		//Return the bad id with an error message
		*pnLoc = m_prgLangIds[m_iLangId++];
		return E_FAIL;
	}

	*pbstrName = SysAllocString(szLangName);
	if (!*pbstrName)
		ThrowHr(WarnHr(E_OUTOFMEMORY));
	*pnLoc = m_prgLangIds[m_iLangId++];

	END_COM_METHOD(g_factLang, IID_ILgLanguageEnumerator);
}

HRESULT hr;

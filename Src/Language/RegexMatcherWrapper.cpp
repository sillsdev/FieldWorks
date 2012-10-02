/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: RegexMatcherWrapper.cpp
Responsibility: Dan Hinton/John Thomson
Last reviewed: Not yet.

Description:
	A class implementing an interface that allows C# objects to perform necessary locking of ICU resources
	prior to calling functions that use more direct marshalling. The idea is that code that
	calls ICU via C# marshalling and code that calls it via one of our COM interfaces
	must both ensure thread locking on a single access point. This provides it.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables, etc.
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Constructor/Destructor
//:>********************************************************************************************

RegexMatcherWrapper::RegexMatcherWrapper()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_status = U_ZERO_ERROR;
}

RegexMatcherWrapper::~RegexMatcherWrapper()
{
	if (m_pmatcher)
	{
		delete m_pmatcher;
		m_pmatcher = NULL;
	}
	if (m_pusInput)
	{
		delete m_pusInput;
		m_pusInput = NULL;
	}

	ModuleEntry::ModuleRelease();
	// Review: should we have it release the semaphore if not previously done??
}

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language.RegexMatcherWrapper"),
	&CLSID_RegexMatcherWrapper,
	_T("SIL ICU locking"),
	_T("Apartment"),
	&RegexMatcherWrapper::CreateCom);


void RegexMatcherWrapper::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<RegexMatcherWrapper> qzlock;

	qzlock.Attach(NewObj RegexMatcherWrapper());		// ref count initially 1
	CheckHr(qzlock->QueryInterface(riid, ppv));
}



//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP RegexMatcherWrapper::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IRegexMatcher *>(this));
	else if (riid == IID_IRegexMatcher)
		*ppv = static_cast<IRegexMatcher *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(static_cast<IRegexMatcher *>(this),
			IID_IRegexMatcher);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	IRegexMatcherWrapper Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize the pattern to be searched. This must be done before calling other
	methods.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RegexMatcherWrapper::Init(BSTR bstrPattern, ComBool fMatchCase)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrPattern);
	if (m_pmatcher)
	{
		delete(m_pmatcher);
		m_pmatcher = NULL;
	}
	uint32_t flags = 0;
	if (!fMatchCase)
		flags |= UREGEX_CASE_INSENSITIVE;
	m_status = U_ZERO_ERROR;
	m_pmatcher = new RegexMatcher(UnicodeString(bstrPattern, BstrLen(bstrPattern)), flags,
		m_status);
	if (U_FAILURE(m_status))
	{
		if (m_pmatcher)
		{
			delete m_pmatcher;
			m_pmatcher = NULL;
		}
	}
	END_COM_METHOD(g_fact, IID_IRegexMatcher);
}
/*----------------------------------------------------------------------------------------------
	This oddly named method is named for the one in the real RegexMatcher. It sets the
	input that will be searched.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RegexMatcherWrapper::Reset(BSTR bstrInput)
{
	BEGIN_COM_METHOD;
	if (m_pmatcher)		// LT-3262 can be null from Init when there is some U_FAILURE code
	{
		if (m_pusInput)
		{
			delete m_pusInput;
			m_pusInput = NULL;
		}
		m_status = U_ZERO_ERROR;
		m_pusInput = new UnicodeString(bstrInput, BstrLen(bstrInput));
		m_pmatcher->reset(*m_pusInput);
	}
	END_COM_METHOD(g_fact, IID_IRegexMatcher);
}

/*----------------------------------------------------------------------------------------------
	This finds the first occurrence of the pattern in the input (starting at ich),
	if any, and returns a boolean indicating whether it was found. The match may start
	exactly at ich.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RegexMatcherWrapper::Find(int ich, ComBool * pfFound)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfFound);
	if (m_pmatcher)
	{
		m_status = U_ZERO_ERROR;
		*pfFound = m_pmatcher->find(ich, m_status);
		if (U_FAILURE(m_status))
			*pfFound = false;
	}
	END_COM_METHOD(g_fact, IID_IRegexMatcher);
}

/*----------------------------------------------------------------------------------------------
	Obtain the start of the indexed group. 0 obtains the start of the entire match;
	1 obtains the part matched by the first () group, and so forth. Using an out-of-range
	index returns -1. (It does not produce a bad HRESULT.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RegexMatcherWrapper::get_Start(int igroup, int * pich)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pich);
	*pich = -1;
	if (m_pmatcher)
	{
		m_status = U_ZERO_ERROR;
		*pich = m_pmatcher->start(igroup, m_status);
		if (U_FAILURE(m_status))
			*pich = -1;
	}
	END_COM_METHOD(g_fact, IID_IRegexMatcher);
}

/*----------------------------------------------------------------------------------------------
	Returns the end of the indicated group. Following the ICU terminology here...
	in our jargon it would be the limit of the group, that is, the index of the character
	AFTER the last one matched.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RegexMatcherWrapper::get_End(int igroup, int * pich)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pich);
	*pich = -1;
	if (m_pmatcher)
	{
		m_status = U_ZERO_ERROR;
		*pich = m_pmatcher->end(igroup, m_status);
		if (U_FAILURE(m_status))
			*pich = -1;
	}
	END_COM_METHOD(g_fact, IID_IRegexMatcher);
}


/*----------------------------------------------------------------------------------------------
	Get any error message from a prior operation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RegexMatcherWrapper::get_ErrorMessage(BSTR * pbstrMsg)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrMsg);
	StrUni stuMsg(u_errorName(m_status));
	*pbstrMsg = SysAllocString(stuMsg.Chars());
	END_COM_METHOD(g_fact, IID_IRegexMatcher);
}

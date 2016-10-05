/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TsStringFactory.cpp
Responsibility: Jeff Gayle
Last reviewed:

	Implementation of ITsStringFactory.

	This is a thread-safe, "agile" component.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE


// The class factory for TsStrFact.
static GenericFactory g_factStrFact(
	_T("FieldWorks.TsStrFactory"),
	&CLSID_TsStrFactory,
	_T("FieldWorks String Factory"),
	_T("Both"),
	&TsStrFact::CreateCom);

//:End Ignore

/*----------------------------------------------------------------------------------------------
	Called by the GenericFactory to "create" an ITsStringFactory. It just returns the global
	one.
----------------------------------------------------------------------------------------------*/
void TsStrFact::CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	if (punkOuter)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	CheckHr(KernelGlobals::g_strf->QueryInterface(iid, ppv));
}


/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrFact::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_ITsStrFactory)
		*ppv = static_cast<ITsStrFactory *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ITsStrFactory);
		return S_OK;
	}
#if WIN32
	else if (iid == IID_IMarshal)
		return m_qunkMarshaler->QueryInterface(iid, ppv);
#endif
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Since this is a singleton, just addref the module.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) TsStrFact::AddRef(void)
{
	return ModuleEntry::ModuleAddRef();
}


/*----------------------------------------------------------------------------------------------
	Since this is a singleton, just release the module.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) TsStrFact::Release(void)
{
	return ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	Make a string with the given characters and writing system and no style.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrFact::MakeString(BSTR bstr, int ws, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);
	ChkComOutPtr(pptss);
	if ((uint)ws > kwsLim)
		ThrowInternalError(E_INVALIDARG, "Magic writing system invalid in string");
	Assert(ws);
	if (!ws)
		ThrowInternalError(E_INVALIDARG, "writing system zero invalid in string");

	// Deliberately duplicated for performance from MakeStringRgch
	ComSmartPtr<TsTextProps> qttp;
	TsIntProp tip;
	tip.m_tpt = ktptWs;
	tip.m_nVar = 0;
	tip.m_nVal = ws;		// Writing system
	TsTextProps::CreateCanonical(&tip, 1, NULL, 0, &qttp);
	TsStrSingle::Create(bstr, BstrLen(bstr), qttp, pptss);

	END_COM_METHOD(g_factStrFact, IID_ITsStrFactory);
}


/*----------------------------------------------------------------------------------------------
	Make a string with the given characters and writing system and no style.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrFact::MakeStringRgch(const OLECHAR * prgch, int cch, int ws,
	ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgch, cch);
	ChkComOutPtr(pptss);
	if ((uint)ws > kwsLim)
		ThrowInternalError(E_INVALIDARG, "Magic writing system invalid in string");
	Assert(ws);
	if (!ws)
		ThrowInternalError(E_INVALIDARG, "writing system zero invalid in string");

	// This code is duplicated (for performance) in MakeString.
	ComSmartPtr<TsTextProps> qttp;
	TsIntProp tip;
	tip.m_tpt = ktptWs;
	tip.m_nVar = 0;
	tip.m_nVal = ws;		// Writing system
	TsTextProps::CreateCanonical(&tip, 1, NULL, 0, &qttp);
	TsStrSingle::Create(prgch, cch, qttp, pptss);

	END_COM_METHOD(g_factStrFact, IID_ITsStrFactory);
}

/*----------------------------------------------------------------------------------------------
	Make a string with the given characters and and properties.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrFact::MakeStringWithPropsRgch(const OLECHAR * prgch, int cch,
	ITsTextProps * pttp, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgch, cch);
	ChkComArgPtr(pttp);
	ChkComOutPtr(pptss);

	DataReaderRgb drr(prgch, cch * isizeof(OLECHAR));
	TsStrSingle::Create(&drr, cch, pttp, pptss);

	END_COM_METHOD(g_factStrFact, IID_ITsStrFactory);
}


/*----------------------------------------------------------------------------------------------
	Get an ITsStrBldr initially with empty state.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrFact::GetBldr(ITsStrBldr ** pptsb)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptsb);

	TsStrBldr::Create(NULL, 0, NULL, 0,  pptsb);

	END_COM_METHOD(g_factStrFact, IID_ITsStrFactory);
}


/*----------------------------------------------------------------------------------------------
	Get an ITsIncStrBldr with initially empty state.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrFact::GetIncBldr(ITsIncStrBldr ** pptisb)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptisb);

	TsIncStrBldr::Create(NULL, 0, NULL, 0, pptisb);

	END_COM_METHOD(g_factStrFact, IID_ITsStrFactory);
}

/*----------------------------------------------------------------------------------------------
	Return an empty string in the specified writing system.
	These are cached so a new object does not have to be created every time.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrFact::EmptyString(int ws, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);
	ITsStringPtr qtss;
	LOCK (m_mutex)
	{
		if (m_hmwsqtssEmptyStrings.Retrieve(ws, qtss))
		{
			*pptss = qtss.Detach();
		}
		else
		{
			CheckHr(MakeString(NULL, ws, pptss));
			m_hmwsqtssEmptyStrings.Insert(ws, *pptss);
		}
	}
	END_COM_METHOD(g_factStrFact, IID_ITsStrFactory);
}

#include "ComHashMap_i.cpp"
template class ComHashMap<int, ITsString>;

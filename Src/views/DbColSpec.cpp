/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: DbColSpec.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This file contains class definitions for the following class:
		DbColSpec

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE
//  Note: Including these 2 files will lead to code bloat.
// Note: All of these are now in ExplicitInstantiation.cpp
//#include "HashMap.h"
//#include "HashMap_i.cpp"
//#include "Vector_i.cpp"

// Explicit instantiation
//template Vector<byte>;
//template Vector<wchar>;
//template Vector<HVO>;


//:>********************************************************************************************
//:>	DbColSpec - Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factDbColSpec(
	_T("SIL.Views.DbColSpec"),
	&CLSID_DbColSpec,
	_T("SIL DB Column Specification"),
	_T("Apartment"),
	&DbColSpec::CreateCom);


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
DbColSpec::DbColSpec()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
DbColSpec::~DbColSpec()
{
	ModuleEntry::ModuleRelease();
}


//:>********************************************************************************************
//:>	DbColSpec - IUnknown methods
//:>********************************************************************************************
void DbColSpec::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
	{
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	}
	ComSmartPtr<DbColSpec> qzdcs;
	qzdcs.Attach(NewObj DbColSpec());	// ref count initially 1
	CheckHr(qzdcs->QueryInterface(riid, ppv));
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbColSpec::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IDbColSpec *>(this));
	else if (iid == IID_IDbColSpec)
		*ppv = static_cast<IDbColSpec *>(this);
	else if (&iid == &CLSID_DbColSpec)
		*ppv = static_cast<IDbColSpec *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IDbColSpec);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) DbColSpec::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) DbColSpec::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}


/*----------------------------------------------------------------------------------------------
	${IDbColSpec#Clear}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbColSpec::Clear()
{
	BEGIN_COM_METHOD;

	voct.Clear();
	viBaseCol.Clear();
	vtag.Clear();
	vws.Clear();

	END_COM_METHOD(g_factDbColSpec, IID_IDbColSpec);
}


/*----------------------------------------------------------------------------------------------
	${IDbColSpec#Push}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbColSpec::Push(int oct, int iBaseCol, PropTag tag, int ws)
{
	BEGIN_COM_METHOD;

	voct.Push(oct);
	viBaseCol.Push(iBaseCol);
	vtag.Push(tag);
	vws.Push(ws);

	END_COM_METHOD(g_factDbColSpec, IID_IDbColSpec);
}


/*----------------------------------------------------------------------------------------------
	${IDbColSpec#Size}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbColSpec::Size(int * pc)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pc);

	*pc = voct.Size();

	END_COM_METHOD(g_factDbColSpec, IID_IDbColSpec);
}


/*----------------------------------------------------------------------------------------------
	${IDbColSpec#GetColInfo}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbColSpec::GetColInfo(int iIndex, int * poct, int * piBaseCol, PropTag * ptag,
	int * pws)
{
	BEGIN_COM_METHOD;
	Assert(iIndex >= 0);
	Assert(iIndex < voct.Size());
	if ((unsigned)iIndex >= (unsigned)voct.Size())
		return E_INVALIDARG;
	ChkComOutPtr(poct);
	ChkComOutPtr(piBaseCol);
	ChkComArgPtr(ptag);
	ChkComOutPtr(pws);

	*poct = voct[iIndex];
	*piBaseCol = viBaseCol[iIndex];
	*ptag = vtag[iIndex];
	*pws = vws[iIndex];

	END_COM_METHOD(g_factDbColSpec, IID_IDbColSpec);
}


/*----------------------------------------------------------------------------------------------
	${IDbColSpec#GetDbColType}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbColSpec::GetDbColType(int iIndex, int * poct)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(poct);
	Assert(iIndex >= 0);
	Assert(iIndex < voct.Size());
	if ((unsigned)iIndex >= (unsigned)voct.Size())
		return E_INVALIDARG;

	*poct = voct[iIndex];

	END_COM_METHOD(g_factDbColSpec, IID_IDbColSpec);
}


/*----------------------------------------------------------------------------------------------
	${IDbColSpec#GetBaseCol}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbColSpec::GetBaseCol(int iIndex, int * piBaseCol)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(piBaseCol);
	Assert(iIndex >= 0);
	Assert(iIndex < viBaseCol.Size());
	if ((unsigned)iIndex >= (unsigned)viBaseCol.Size())
		return E_INVALIDARG;

	*piBaseCol = viBaseCol[iIndex];

	END_COM_METHOD(g_factDbColSpec, IID_IDbColSpec);
}


/*----------------------------------------------------------------------------------------------
	${IDbColSpec#GetTag}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbColSpec::GetTag(int iIndex, PropTag * ptag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptag);
	Assert(iIndex >= 0);
	Assert(iIndex < vtag.Size());
	if ((unsigned)iIndex >= (unsigned)vtag.Size())
		return E_INVALIDARG;

	*ptag = vtag[iIndex];

	END_COM_METHOD(g_factDbColSpec, IID_IDbColSpec);
}


/*----------------------------------------------------------------------------------------------
	${IDbColSpec#GetWs}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbColSpec::GetWs(int iIndex, int * pws)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pws);
	Assert(iIndex >= 0);
	Assert(iIndex < vws.Size());
	if ((unsigned)iIndex >= (unsigned)vws.Size())
		return E_INVALIDARG;

	*pws = vws[iIndex];

	END_COM_METHOD(g_factDbColSpec, IID_IDbColSpec);
}

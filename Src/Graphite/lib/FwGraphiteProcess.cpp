/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: FwGraphiteProcess.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description: Contains the implementation of the GrEngine class.
----------------------------------------------------------------------------------------------*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

//:End Ignore

//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************
//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Graphite.FwGraphiteProcess"),
	&CLSID_FwGraphiteProcess,
	_T("SIL Graphite renderer wrapper"),
	_T("Apartment"),
	&FwGraphiteProcess::CreateCom);


void FwGraphiteProcess::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<FwGraphiteProcess> qgreng;
	qgreng.Attach(NewObj FwGraphiteProcess());		// ref count initially 1
	CheckHr(qgreng->QueryInterface(riid, ppv));
}

//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

FwGraphiteProcess::FwGraphiteProcess()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

FwGraphiteProcess::~FwGraphiteProcess()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	IJustifyingRenderer interface methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGraphiteProcess::QueryInterface(REFIID riid, void ** ppv)
{
	if (!ppv)
		return E_POINTER;
	AssertPtr(ppv);
	*ppv = NULL;
	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IJustifyingRenderer *>(this));

	else if (riid == IID_IJustifyingRenderer)
		*ppv = static_cast<IJustifyingRenderer *>(this);
	else if (&riid == &CLSID_FwGraphiteProcess)
		*ppv = static_cast<FwGraphiteProcess *>(this);

	// John says this is okay because we're only using this to call back to the
	// Graphite engine itself. Normally this is not a good thing to do.
	else if (riid == CLSID_FwGraphiteProcess)
		*ppv = static_cast<FwGraphiteProcess *>(this);

	else if (riid == IID_ISupportErrorInfo)
	{
		// for error reporting:
		*ppv = NewObj CSupportErrorInfo(static_cast<IJustifyingRenderer *>(this),
			IID_IJustifyingRenderer);
		return NOERROR;
	}
	else
		return E_NOINTERFACE;
	AddRef();
	return NOERROR;
}

/*----------------------------------------------------------------------------------------------
	Get the value of a glyph's (slot) attribute from the Graphite engine.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGraphiteProcess::GetGlyphAttributeFloat(int iGlyph, int jgat, int nLevel,
	float * pValueRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pValueRet);
	return (HRESULT)m_pgproc->getGlyphAttribute(iGlyph, jgat, nLevel, pValueRet);
	END_COM_METHOD(g_fact, IID_IJustifyingRenderer);
}

STDMETHODIMP FwGraphiteProcess::GetGlyphAttributeInt(int iGlyph, int jgat, int nLevel,
	int * pValueRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pValueRet);
	return (HRESULT)m_pgproc->getGlyphAttribute(iGlyph, jgat, nLevel, pValueRet);
	END_COM_METHOD(g_fact, IID_IJustifyingRenderer);
}

/*----------------------------------------------------------------------------------------------
	Set the value of a glyph's (slot) attribute in the Graphite engine.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGraphiteProcess::SetGlyphAttributeFloat(int iGlyph, int jgat, int nLevel,
	float value)
{
	BEGIN_COM_METHOD;
	return (HRESULT)m_pgproc->setGlyphAttribute(iGlyph, jgat, nLevel, value);
	END_COM_METHOD(g_fact, IID_IJustifyingRenderer);
}

STDMETHODIMP FwGraphiteProcess::SetGlyphAttributeInt(int iGlyph, int jgat, int nLevel,
	int value)
{
	BEGIN_COM_METHOD;
	return (HRESULT)m_pgproc->setGlyphAttribute(iGlyph, jgat, nLevel, value);
	END_COM_METHOD(g_fact, IID_IJustifyingRenderer);
}


//:>********************************************************************************************
//:>	GraphiteProcess interface methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get the value of a glyph's (slot) attribute from the Graphite engine.
----------------------------------------------------------------------------------------------*/
GrResult FwGraphiteProcess::getGlyphAttribute(int iGlyph, int jgat, int nLevel,
	float * pValueRet)
{
	return (GrResult)GetGlyphAttributeFloat(iGlyph, jgat, nLevel, pValueRet);
}

GrResult FwGraphiteProcess::getGlyphAttribute(int iGlyph, int jgat, int nLevel,
	int * pValueRet)
{
	return (GrResult)GetGlyphAttributeInt(iGlyph, jgat, nLevel, pValueRet);
}

/*----------------------------------------------------------------------------------------------
	Set the value of a glyph's (slot) attribute in the Graphite engine.
----------------------------------------------------------------------------------------------*/
GrResult FwGraphiteProcess::setGlyphAttribute(int iGlyph, int jgat, int nLevel, float value)
{
	return (GrResult)SetGlyphAttributeFloat(iGlyph, jgat, nLevel, value);
}

GrResult FwGraphiteProcess::setGlyphAttribute(int iGlyph, int jgat, int nLevel, int value)
{
	return (GrResult)SetGlyphAttributeInt(iGlyph, jgat, nLevel, value);
}

//:End Ignore

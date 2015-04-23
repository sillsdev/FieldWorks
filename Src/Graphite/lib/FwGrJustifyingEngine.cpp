/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrEngine.cpp
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
	_T("SIL.Graphite.FwGrJustifyingEngine"),
	&CLSID_FwGrJustifyingEngine,
	_T("SIL Graphite renderer wrapper"),
	_T("Apartment"),
	&FwGrJustifyingEngine::CreateCom);


void FwGrJustifyingEngine::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<FwGrJustifyingEngine> qgreng;
	qgreng.Attach(NewObj FwGrJustifyingEngine());		// ref count initialy 1
	CheckHr(qgreng->QueryInterface(riid, ppv));
}

//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

FwGrJustifyingEngine::FwGrJustifyingEngine()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

FwGrJustifyingEngine::~FwGrJustifyingEngine()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	IJustifyingRenderer interface methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrJustifyingEngine::QueryInterface(REFIID riid, void ** ppv)
{
	if (!ppv)
		return E_POINTER;
	AssertPtr(ppv);
	*ppv = NULL;
	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IJustifyingRenderer *>(this));

	else if (riid == IID_IJustifyingRenderer)
		*ppv = static_cast<IJustifyingRenderer *>(this);
	else if (&riid == &CLSID_FwGrJustifyingEngine)
		*ppv = static_cast<FwGrJustifyingEngine *>(this);

	// John says this is okay because we're only using this to call back to the
	// Graphite engine itself. Normally this is not a good thing to do.
	else if (riid == CLSID_FwGrJustifyingEngine)
		*ppv = static_cast<FwGrJustifyingEngine *>(this);

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
	Get the value of a glyph attribute from the Graphite engine.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrJustifyingEngine::GetGlyphAttribute(int iGlyph, int jgat, int nLevel,
	int * pnValueRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnValueRet);
	//return (HRESULT)m_qfge->GetGlyphAttribute(iGlyph, jgat, nLevel, pnValueRet);
	return (HRESULT)m_pgproc->GetGlyphAttribute(iGlyph, jgat, nLevel, pnValueRet);
	END_COM_METHOD(g_fact, IID_IJustifyingRenderer);
}

/*----------------------------------------------------------------------------------------------
	Set the value of a glyph attribute in the Graphite engine.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrJustifyingEngine::SetGlyphAttribute(int iGlyph, int jgat, int nLevel, int nValue)
{
	BEGIN_COM_METHOD;
	//return (HRESULT)m_qfge->SetGlyphAttribute(iGlyph, jgat, nLevel, nValue);
	return (HRESULT)m_pgproc->SetGlyphAttribute(iGlyph, jgat, nLevel, nValue);
	END_COM_METHOD(g_fact, IID_IJustifyingRenderer);
}


//:>********************************************************************************************
//:>	GraphiteProcess interface methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get the value of a glyph attribute from the Graphite engine.
----------------------------------------------------------------------------------------------*/
GrResult FwGrJustifyingEngine::GetGlyphAttribute(int iGlyph, int jgat, int nLevel,
	int * pnValueRet)
{
	return (GrResult)GetGlyphAttribute(iGlyph, jgat, nLevel, pnValueRet);
}

/*----------------------------------------------------------------------------------------------
	Set the value of a glyph attribute in the Graphite engine.
----------------------------------------------------------------------------------------------*/
GrResult FwGrJustifyingEngine::SetGlyphAttribute(int iGlyph, int jgat, int nLevel, int nValue)
{
	return (GrResult)SetGlyphAttribute(iGlyph, jgat, nLevel, nValue);
}

//:End Ignore

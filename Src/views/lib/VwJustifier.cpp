/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwJustifier.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implements the actual functions needed, which generally access GrJustifier.
-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE
/***********************************************************************************************
	Forward declarations
***********************************************************************************************/

/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/

/***********************************************************************************************
	Two local classes, copied from AfGfx.h. Maybe we should move them to somewhere they
	can be shared more easily?
***********************************************************************************************/

/***********************************************************************************************
	Constructors/Destructor
***********************************************************************************************/

VwJustifier::VwJustifier() : GrJustifier()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	//Init();
}

VwJustifier::~VwJustifier()
{
	ModuleEntry::ModuleRelease();
}


/***********************************************************************************************
	Generic factory stuff to allow creating an instance with CoCreateInstance.
***********************************************************************************************/
static GenericFactory g_fact(
	_T("SIL.Text.VwJustifier"),
	&CLSID_VwJustifier,
	_T("SIL Justifier"),
	_T("Apartment"),
	&VwJustifier::CreateCom);


void VwJustifier::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwJustifier> qjus;
	qjus.Attach(NewObj VwJustifier());		// ref count initialy 1
	CheckHr(qjus->QueryInterface(riid, ppv));
}

/***********************************************************************************************
	IUnknown Methods
***********************************************************************************************/
STDMETHODIMP VwJustifier::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	//if (&riid == &CLID_VWJUSTIFIER_IMPL)
	//	*ppv = static_cast<VwJustifier *>(this);
	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwJustifier)
		*ppv = static_cast<IVwJustifier *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<IVwJustifier *>(this),
			IID_IVwJustifier, IID_IVwJustifier);
		return S_OK;
	}
	// TODO: do we need to add a case for GrJustifier?????????
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

/***********************************************************************************************
	IVwJustifier Interface Methods
***********************************************************************************************/
STDMETHODIMP VwJustifier::AdjustGlyphWidths(IJustifyingRenderer * pjrend,
	int iGlyphMin, int iGlyphLim,
	float dxCurrentWidth, float dxDesiredWidth)
{
	BEGIN_COM_METHOD;

	FwGraphiteProcess * pfgje;
	CheckHr(pjrend->QueryInterface(CLSID_FwGraphiteProcess, (void**)&pfgje));

	return (HRESULT)GrJustifier::adjustGlyphWidths(pfgje, iGlyphMin, iGlyphLim,
		dxCurrentWidth, dxDesiredWidth);

	END_COM_METHOD(g_fact, IID_IVwJustifier);
}

//STDMETHODIMP VwJustifier::SuggestShrinkAndBreak(IJustifyingRenderer * pjrend,
//	int iGlyphMin, int iGlyphLim, float dxsWidth, LgLineBreak lbPref, LgLineBreak lbMax,
//	float * pdxShrink, LgLineBreak * plbToTry)
//{
//	BEGIN_COM_METHOD;
//
//	gr::FwGraphiteProcess * pfgje;
//	CheckHr(pjrend->QueryInterface(CLSID_FwGraphiteProcess, (void**)&pfgje));
//
//	return (HRESULT)GrJustifier::suggestShrinkAndBreak(pfgje,
//		iGlyphMin, iGlyphLim, dxsWidth, lbPref, lbMax, pdxShrink, plbToTry);
//
//	END_COM_METHOD(g_fact, IID_IVwJustifier);
//}
/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, SIL International. All rights reserved.

File: SampleInterface.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	Implementation of a trivial demo interface.
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

SampleInterface::SampleInterface()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

SampleInterface::~SampleInterface()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	"SIL.Samples.SampleInterface",
	&CLSID_SampleInterface,
	"SIL Sample",
	"Apartment",
	&SampleInterface::CreateCom);


void SampleInterface::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<SampleInterface> qxsi;
	qxsi.Attach(NewObj SampleInterface());		// ref count initialy 1
	CheckHr(qxsi->QueryInterface(riid, ppv));
}

//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP SampleInterface::QueryInterface(REFIID riid, void **ppv)
{
	if (!ppv)
		return E_POINTER;
	AssertPtr(ppv);
	*ppv = NULL;
	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_ISampleInterface)
		*ppv = static_cast<ISampleInterface *>(this);
	else
		return E_NOINTERFACE;
	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   ISampleInterface Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	A simple demonstration method which returns a fixed string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SampleInterface::get_HelloWorldString(BSTR * pbstr)
{
	AssertPtrN(pbstr);
	if (!pbstr)
		return E_POINTER;
	*pbstr = SysAllocString(L"Hello World from the SimpleDll");
	if (!*pbstr)
		return E_OUTOFMEMORY;
	return S_OK;
}

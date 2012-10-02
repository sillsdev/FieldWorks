/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgIcuLocking.cpp
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

LgIcuLocking::LgIcuLocking()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

LgIcuLocking::~LgIcuLocking()
{
	ModuleEntry::ModuleRelease();
	// Review: should we have it release the semaphore if not previously done??
}

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language.LgIcuLocking"),
	&CLSID_LgIcuLocking,
	_T("SIL ICU locking"),
	_T("Apartment"),
	&LgIcuLocking::CreateCom);


void LgIcuLocking::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgIcuLocking> qzlock;

	qzlock.Attach(NewObj LgIcuLocking());		// ref count initially 1
	CheckHr(qzlock->QueryInterface(riid, ppv));
}



//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgIcuLocking::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgIcuLocking *>(this));
	else if (riid == IID_ILgIcuLocking)
		*ppv = static_cast<ILgIcuLocking *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(static_cast<ILgIcuLocking *>(this),
			IID_ILgIcuLocking);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	ILgIcuLocking Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Call at start of block requiring protection.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuLocking::Lock()
{
	BEGIN_COM_METHOD
		StrUtil::InitAndLockIcuMutex();
	END_COM_METHOD(g_fact, IID_ILgIcuLocking);
}

/*----------------------------------------------------------------------------------------------
	Call at end of block requiring protection.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuLocking::Unlock()
{
	BEGIN_COM_METHOD
		StrUtil::ReleaseIcuMutex();
	END_COM_METHOD(g_fact, IID_ILgIcuLocking);
}
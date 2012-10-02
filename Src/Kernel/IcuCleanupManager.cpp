/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: IcuCleanupManager.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	A singleton class implementing an interface for registering callbacks where we need to be
	informed of IcuCleanup calls.
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

IcuCleanupManager::IcuCleanupManager()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

IcuCleanupManager::~IcuCleanupManager()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language.IcuCleanupManager"),
	&CLSID_IcuCleanupManager,
	_T("SIL ICU cleanup"),
	_T("Apartment"),
	&IcuCleanupManager::CreateCom);


// Does not usually actually create one, only if no static instance exists.
void IcuCleanupManager::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<IcuCleanupManager> qzlock;
	if (!s_qicln)
		s_qicln.Attach(NewObj IcuCleanupManager());		// ref count initially 1
	CheckHr(s_qicln->QueryInterface(riid, ppv));
}



//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP IcuCleanupManager::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IIcuCleanupManager *>(this));
	else if (riid == IID_IIcuCleanupManager)
		*ppv = static_cast<IIcuCleanupManager *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(static_cast<IIcuCleanupManager *>(this),
			IID_IIcuCleanupManager);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	IIcuCleanupManager Methods
//:>********************************************************************************************
ComSmartPtr<IcuCleanupManager> IcuCleanupManager::s_qicln;

/*----------------------------------------------------------------------------------------------
	Register a callback to be notified when a cleanup is performed.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP IcuCleanupManager::RegisterCleanupCallback(IIcuCleanupCallback * piclncb)
{
	BEGIN_COM_METHOD;
	m_viclncbCallbacks.Push(piclncb);
	END_COM_METHOD(g_fact, IID_IIcuCleanupManager);
}

/*----------------------------------------------------------------------------------------------
	Unregister a callback no longer needed.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP IcuCleanupManager::UnregisterCleanupCallback(IIcuCleanupCallback * piclncb)
{
	BEGIN_COM_METHOD;
	for (int i = 0; i < m_viclncbCallbacks.Size(); i++)
	{
		if (m_viclncbCallbacks[i] == piclncb)
		{
			m_viclncbCallbacks.Delete(i, i + 1);
			break;
		}
	}
	// give this instance a chance to go away if not being used.
	if (m_viclncbCallbacks.Size() == 0)
		s_qicln.Clear();

	END_COM_METHOD(g_fact, IID_IIcuCleanupManager);
}

/*----------------------------------------------------------------------------------------------
	Perform a cleanup and inform everyone.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP IcuCleanupManager::Cleanup()
{
	BEGIN_COM_METHOD;

	// Calling DoneCleanup() may unregister the callback, so start at the top...
	for (int i = m_viclncbCallbacks.Size() - 1; i >= 0; --i)
	{
		m_viclncbCallbacks[i]->DoneCleanup();
	}

	StrUtil::RestartIcu();

	END_COM_METHOD(g_fact, IID_IIcuCleanupManager);
}

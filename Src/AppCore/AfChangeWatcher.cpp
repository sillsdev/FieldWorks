/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfChangeWatcher.cpp
Responsibility: Bryan Wussow
Last reviewed:

Description:
	class AfChangeWatcher : public IVwNotifyChange
	This base class is used to receive notifications of changes to properties and
	then execute side effect actions.
	To use it, derive your own change watcher class from this base class. For an example see
	class SeChangeWatcher.
-------------------------------------------------------------------------------*//*:End Ignore*/

#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	AfChangeWatcher methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor, Init.
----------------------------------------------------------------------------------------------*/
AfChangeWatcher::AfChangeWatcher()
{
	ModuleEntry::ModuleAddRef();  //Keep DLL module in memory while this object is instantiated
	m_cref = 1;
}

void AfChangeWatcher::Init(ISilDataAccess * psda, PropTag tag)
{
	AssertPtr(psda);
	m_psda = psda;
	CheckHr(m_psda->AddNotification(this)); // register this in the ISilDataAccess
	m_tag = tag;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfChangeWatcher::~AfChangeWatcher()
{
	CheckHr(m_psda->RemoveNotification(this));
	ModuleEntry::ModuleRelease();
}

static DummyFactory g_fact(_T("SIL.AppCore.AfChangeWatcher"));

/*----------------------------------------------------------------------------------------------
	QueryInterface.
	Returns a pointer to a specified interface on an object to which a client currently holds
	an interface pointer.
	@param riid  Identifier of the requested interface.
	@param ppv  Address of output variable that receives the interface pointer requested in riid.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfChangeWatcher::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwNotifyChange)
		*ppv = static_cast<IVwNotifyChange *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwNotifyChange);
		return NOERROR;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


/*----------------------------------------------------------------------------------------------
	PropChanged
	This function is called by the ISilDataAccess to notify us when a property (tag) has been
	changed on a viewable object (hvo).
	If the tag matches the defined tag, the derived class's DoEffectsOfPropChange() is called.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfChangeWatcher::PropChanged(HVO hvo, PropTag tag, int ivMin, int cvIns,
											int cvDel)
{
	BEGIN_COM_METHOD
	if (tag == m_tag)
		DoEffectsOfPropChange(hvo, ivMin, cvIns, cvDel);
	END_COM_METHOD(g_fact, IID_IVwNotifyChange)
}

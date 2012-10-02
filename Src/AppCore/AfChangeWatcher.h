/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfChangeWatcher.h
Responsibility: Bryan Wussow
Last reviewed:
Description:
	class AfChangeWatcher : public IVwNotifyChange
	This base class is used to receive notifications of changes to properties and
	then execute side effect actions.
	To use it, derive your own change watcher class from this base class. For an example see
	class SeChangeWatcher.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef CHG_WATCHER_INCLUDED
#define CHG_WATCHER_INCLUDED 1

/*----------------------------------------------------------------------------------------------
	The AfChangeWatcher class receives notifications and checks if they match the
	tag of interest. When it matches, DoEffectsOfPropChange() is called.
	Hungarian: chgw
----------------------------------------------------------------------------------------------*/

class AfChangeWatcher : public IVwNotifyChange
{
	typedef IVwNotifyChange SuperClass;

public:
	AfChangeWatcher();
	void Init(ISilDataAccess * psda, PropTag tag);
	virtual ~AfChangeWatcher();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// IVwNotifyChange methods
	STDMETHOD(PropChanged)(HVO hvoPara, int tag, int ivMin, int cvIns, int cvDel);

protected:
	//note: Because this object will be registered in the ISilDataAccess and tracked
	// there by a smart pointer,
	// use regular pointers in this class so that it won't be deadlocked when Da is going away.
	ISilDataAccess * m_psda;
	PropTag m_tag; // the property tag that the caller wants to be notified about
	long m_cref;

	// subclass must implement this function to do the effects desired
	// see IVwNotifyChange for parameter details
	virtual void DoEffectsOfPropChange(HVO hvoPara, int ivMin, int cvIns, int cvDel)
		= 0;
};

#endif // CHG_WATCHER_INCLUDED

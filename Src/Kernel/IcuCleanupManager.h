/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: IcuCleanupManager.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	See the cpp.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef IcuCleanupManager_INCLUDED
#define IcuCleanupManager_INCLUDED

class IcuCleanupManager : public IIcuCleanupManager
{
	typedef ComVector<IIcuCleanupCallback> VecCallback; // Hungarian viclncb
public:
	//:> Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	//:> Constructors/destructors/etc.
	IcuCleanupManager();
	virtual ~IcuCleanupManager();


	//:> IUnknown methods
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

	STDMETHOD(RegisterCleanupCallback)(IIcuCleanupCallback * piclncb);
	STDMETHOD(UnregisterCleanupCallback)(IIcuCleanupCallback * piclncb);
	STDMETHOD(Cleanup)();


protected:
	//:> Member variables
	long m_cref;
	VecCallback m_viclncbCallbacks;
	// The one instance.
	static ComSmartPtr<IcuCleanupManager> s_qicln;
};

#endif  // IcuCleanupManager_INCLUDED

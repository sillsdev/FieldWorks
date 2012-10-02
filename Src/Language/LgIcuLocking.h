/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgIcuLocking.h
Responsibility: Dan Hinton/John Thomson
Last reviewed: Not yet.

Description:
	See the cpp.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LGICULOCKING_INCLUDED
#define LGICULOCKING_INCLUDED

class LgIcuLocking :
	public ILgIcuLocking
{
public:
	//:> Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	//:> Constructors/destructors/etc.
	LgIcuLocking();
	virtual ~LgIcuLocking();


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


	//:> ILgIcuLocking Methods
	STDMETHOD(Lock)();
	STDMETHOD(Unlock)();

protected:
	//:> Member variables
	long m_cref;
};

#endif  // LGICULOCKING_INCLUDED

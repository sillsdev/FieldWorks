/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilTypeLib.h
Responsibility:
Last reviewed:

Description:
	Provides type library and type info utilities.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef UTILTYPELIB_H
#define UTILTYPELIB_H 1

#ifndef WIN32
class ITypeInfo;
class ITypeLib;
#endif

void LoadTypeInfo(int rid, REFIID iid, ITypeInfo ** ppti);
void LoadTypeLibrary(int rid, ITypeLib **pptl);


class TypeInfoHolder
{
protected:
	long m_cref;
	const IID * m_piid;
	int m_ridTypeLib;
	ITypeInfo * m_pti;

public:
	TypeInfoHolder(REFIID riid, int ridTypeLib = 0)
	{
		m_cref = 0;
		m_piid = &riid;
		m_ridTypeLib = ridTypeLib;
		m_pti = NULL;
	}
	~TypeInfoHolder(void)
	{
		ReleaseObj(m_pti);
	}

	void AddRef(void) {
		InterlockedIncrement(&m_cref);
	}
	void Release(void) {
		if (0 == InterlockedDecrement(&m_cref)) {
			ReleaseObj(m_pti);
		}
	}
	void GetTI(ITypeInfo ** ppti);
};


#endif //!UTILTYPELIB_H

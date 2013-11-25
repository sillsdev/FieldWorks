/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

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

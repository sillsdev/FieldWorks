/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwBaseVirtualHandler.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Provides a default implementation of IVwVirtualHandler to facilitate virtual properties in
	View data caches. Subclasses must at least implement Load().
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWBASE_VH_INCLUDED
#define VWBASE_VH_INCLUDED

class VwBaseVirtualHandler : public IVwVirtualHandler
{
public:
	// Static methods

	// Constructors/destructors/etc.
	VwBaseVirtualHandler();
	virtual ~VwBaseVirtualHandler();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}
	// IVwVirtualHandler methods
	STDMETHOD(put_ClassName)(BSTR bstr);
	STDMETHOD(get_ClassName)(BSTR * pbstr);
	STDMETHOD(put_FieldName)(BSTR bstr);
	STDMETHOD(get_FieldName)(BSTR * pbstr);
	STDMETHOD(put_Tag)(PropTag tag);
	STDMETHOD(get_Tag)(PropTag * ptag);
	STDMETHOD(put_Type)(int cpt);
	STDMETHOD(get_Type)(int * pcpt);
	STDMETHOD(put_Writeable)(ComBool f);
	STDMETHOD(get_Writeable)(ComBool * pf);
	STDMETHOD(put_ComputeEveryTime)(ComBool f);
	STDMETHOD(get_ComputeEveryTime)(ComBool * pf);
	// Deliberately omitted; non-abstract subclasses must implement
	// STDMETHOD(Load)(HVO hvo, PropTag tag, int ws, IVwCacheDa * pcda);
	STDMETHOD(Replace)(HVO hvo, PropTag tag, int ihvoMin, int ihvoLim,
		HVO * prghvo, int chvo, ISilDataAccess * psda);
	STDMETHOD(WriteObj)(HVO hvo, PropTag tag, int ws, IUnknown * punk,
		ISilDataAccess * psda);
	STDMETHOD(WriteInt64)(HVO hvo, PropTag tag, int64 val,
		ISilDataAccess * psda);
	STDMETHOD(WriteUnicode)(HVO hvo, PropTag tag, BSTR bstr,
		ISilDataAccess * psda);
	STDMETHOD(PreLoad)(int chvo, HVO * prghvo, PropTag tag, int ws, IVwCacheDa * pcda);

	STDMETHOD(Initialize)(BSTR bstrData);
	STDMETHOD(DoesResultDependOnProp)(HVO hvoObj, HVO hvoChange,
		PropTag tag, int ws, ComBool * pfDepends);
	STDMETHOD(SetLoadForAllOfClass)(ComBool fLoadAll);

protected:
	// member variables
	long m_cref;
	StrUni m_stuClass;
	StrUni m_stuField;
	PropTag m_tag;
	int m_cpt;
	bool m_fWriteable;
	bool m_fComputeEveryTime;
};

#endif // !VWBASE_VH_INCLUDED

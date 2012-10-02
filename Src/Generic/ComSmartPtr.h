/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ComSmartPtr.h
Responsibility: Shon Katzenberger
Last reviewed:

	Smart pointer class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef ComSmartPtr_H
#define ComSmartPtr_H 1

// NOTE: include ComSmartPtr.h before comdef.h so that we use our definition instead of
// the one provided in comip.h
#define _COM_SMARTPTR ComSmartPtr
#define _COM_SMARTPTR_TYPEDEF(Interface, IID) \
	typedef _COM_SMARTPTR<Interface> Interface ## Ptr


template<class T>
	class IntfNoRelease : public T
{
private:
	STDMETHOD_(UCOMINT32, AddRef)(void) = 0;
	STDMETHOD_(UCOMINT32, Release)(void) = 0;
};


/*----------------------------------------------------------------------------------------------
	Smart pointer class. This handles automatically addrefing and releasing com interface
	pointers.
----------------------------------------------------------------------------------------------*/
template<typename _Interface> class ComSmartPtr
{
public:
	typedef _Interface Interface;

	static const IID & GetIID()
	{
		return __uuidof(Interface);
	}

	// Destructor.
	~ComSmartPtr(void)
	{
		if (m_pobj)
		{
			m_pobj->Release();
			m_pobj = NULL;
		}
	}

	// Default constructor.
	ComSmartPtr(void)
	{
		m_pobj = NULL;
	}

	// Copy the pointer and AddRef().
	ComSmartPtr(const ComSmartPtr & qobj)
	{
		m_pobj = qobj.m_pobj;
		if (m_pobj)
			m_pobj->AddRef();
	}

	// Saves the interface.
	ComSmartPtr(Interface * pobj)
	{
		m_pobj = pobj;
		if (m_pobj)
			m_pobj->AddRef();
	}

	// Stores the interface.
	ComSmartPtr & operator=(Interface * pobj)
	{
		if (m_pobj != pobj)
		{
			if (pobj)
				pobj->AddRef();
			if (m_pobj)
				m_pobj->Release();
			m_pobj = pobj;
		}
		return *this;
	}

	// Copies and AddRef()'s the interface.
	ComSmartPtr & operator=(const ComSmartPtr & qobj)
	{
		return operator=(qobj.m_pobj);
	}

	// Saves/sets the interface without AddRef()ing. This call
	// will release any previously acquired interface.
	void Attach(Interface * pobj)
	{
		if (m_pobj)
			m_pobj->Release();
		m_pobj = pobj;
	}

	// Simply NULL the interface pointer so that it isn't Released()'ed.
	Interface * Detach()
	{
		Interface * pobj = m_pobj;
		m_pobj = NULL;
		return pobj;
	}

	// Return the interface. This value may be NULL.
	operator Interface *() const
	{
		return m_pobj;
	}

	// Return the interface. This value may be NULL.
	Interface * Ptr(void) const
	{
		return m_pobj;
	}

	// Returns the address of the interface pointer contained in this
	// class. This is useful when using the COM/OLE interfaces to create
	// this interface.
	Interface ** operator&()
	{
		if (m_pobj)
		{
			m_pobj->Release();
			m_pobj = NULL;
		}
		return &m_pobj;
	}

	// Allows this class to be used as the interface itself.
	// Also provides simple error checking.
	IntfNoRelease<Interface> * operator->() const
	{
		AssertPtr(m_pobj);
		return reinterpret_cast<IntfNoRelease<Interface> *>(m_pobj);
	}

	// This operator is provided so that simple boolean expressions will
	// work.  For example: "if (!p) ...". Returns true if the pointer is NULL.
	bool operator!() const
	{
		return m_pobj == NULL;
	}

	// Compare two pointers.
	template<typename _InterfacePtr> bool operator==(_InterfacePtr pobj) const
	{
		return pobj == m_pobj || SameObject(pobj, m_pobj);
	}

	// Compares 2 ComSmartPtr's.
	bool operator==(const ComSmartPtr & qobj) const
	{
		return qobj.m_pobj == m_pobj || SameObject(qobj.m_pobj, m_pobj);
	}

	// Compare to pointers
	template<typename _InterfacePtr> bool operator!=(_InterfacePtr pobj) const
	{
		return pobj != m_pobj && !SameObject(pobj, m_pobj);
	}

	// Compares 2 ComSmartPtr's.
	bool operator!=(const ComSmartPtr & qobj) const
	{
		return qobj.m_pobj != m_pobj && !SameObject(qobj.m_pobj, m_pobj);
	}

	// Just clears the pointer.
	void Clear()
	{
		if (m_pobj)
		{
			m_pobj->Release();
			m_pobj = NULL;
		}
	}

	// Just AddRef's the pointer if it's not null. Who cares if it's already null?
	void AddRef()
	{
		if (m_pobj)
			m_pobj->AddRef();
	}

	// Loads an interface for the provided CLSID.
	void CreateInstance(const CLSID & clsid, DWORD dwClsContext = CLSCTX_ALL);

	// Creates the class specified by pszClsid.
	// pszClsid may contain a class id or a prog id string.
	void CreateInstance(LPCOLESTR pszClsid, DWORD dwClsContext = CLSCTX_ALL)
	{
		AssertPtr(pszClsid);

		CLSID clsid;

		if (pszClsid[0] == '{')
			CheckHr(CLSIDFromString(const_cast<LPOLESTR>(pszClsid), &clsid));
		else
			CheckHr(CLSIDFromProgID(const_cast<LPOLESTR>(pszClsid), &clsid));

		if (m_pobj)
		{
			m_pobj->Release();
			m_pobj = NULL;
		}
		CheckHr(CoCreateInstance(clsid, NULL, dwClsContext, GetIID(), (void **)&m_pobj));
	}

	// Creates the class specified by Ansi pszClsid.
	// pszClsid may contain a class id or a prog id string.
	void CreateInstance(LPCSTR pszClsid, DWORD dwClsContext = CLSCTX_ALL)
	{
		AssertPtr(pszClsid);

#if WIN32
		StrUni stu;
		stu.Assign(pszClsid);

		CreateInstance(stu.Chars(), dwClsContext);
#else
		CreateInstance(pszClsid, dwClsContext);
#endif
	}

	// Attach to the active object specified by clsid. Any previous interface is released.
	void GetActiveObject(const CLSID & clsid)
	{
		if (m_pobj)
		{
			m_pobj->Release();
			m_pobj = NULL;
		}

		ComSmartPtr<IUnknown> qunk;
		CheckHr(::GetActiveObject(clsid, NULL, &qunk));
		CheckHr(qunk->QueryInterface(GetIID(), (void **)&m_pobj));
	}

	// Attach to the active object specified by pszClsid.
	// pszClsid may contain a class id or a prog id string.
	void GetActiveObject(LPCOLESTR pszClsid)
	{
		AssertPtr(pszClsid);

		CLSID clsid;

		if (pszClsid[0] == '{')
			CheckHr(CLSIDFromString(pszClsid, &clsid));
		else
			CheckHr(CLSIDFromProgID(pszClsid, &clsid));

		GetActiveObject(clsid);
	}

	// Attach to the active object specified by pszClsid.
	// pszClsid may contain a class id or a prog id string.
	void GetActiveObject(LPCSTR pszClsid)
	{
		AssertPtr(pszClsid);

#if WIN32
		StrUni stu;
		stu.Assign(pszClsid);

		GetActiveObject(stu.Chars());
#else
		GetActiveObject(pszClsid);
#endif
	}

	void Assign(IUnknown * punk)
	{
		AssertPtrN(punk);

		if (!punk)
		{
			Clear();
			return;
		}

		Interface * pobj;

		CheckHr(punk->QueryInterface(GetIID(), (void **)&pobj));
		Clear();
		m_pobj = pobj;
	}

private:
	// The Interface pointer.
	Interface * m_pobj;
};


/***********************************************************************************************
	Reverse comparison operators for ComSmartPtr.
***********************************************************************************************/
template<typename _Interface, typename _InterfacePtr>
	bool operator==(_Interface * pobj, const ComSmartPtr<_InterfacePtr> & qobj)
{
	return qobj == pobj;
}

template<typename _Interface, typename _InterfacePtr>
	bool operator!=(_Interface * pobj, const ComSmartPtr<_InterfacePtr> & qobj)
{
	return qobj != pobj;
}

#endif // !ComSmartPtr_H

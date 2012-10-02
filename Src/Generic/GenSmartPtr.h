/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GenSmartPtr.h
Responsibility: Shon Katzenberger
Last reviewed:

	Reference countable base class and smart pointer class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef GenSmartPtr_H
#define GenSmartPtr_H 1


/*----------------------------------------------------------------------------------------------
	A framework ref-countable object.
----------------------------------------------------------------------------------------------*/
class GenRefObj
{
public:
	GenRefObj(void)
	{
		m_cref = 1;
	}

	virtual ~GenRefObj(void)
	{
	}

	virtual ULONG STDMETHODCALLTYPE AddRef(void)
	{
		InterlockedIncrement(&m_cref);
		return m_cref;
	}

	virtual ULONG STDMETHODCALLTYPE Release(void)
	{
		ulong cref = InterlockedDecrement(&m_cref);
		if (!cref)
		{
			m_cref = 1;
			delete this;
		}
		return cref;

	}
	/*void AddRef(void)
	{
		InterlockedIncrement(&m_cref);
	}

	void Release(void)
	{
		if (!InterlockedDecrement(&m_cref))
		{
			m_cref = 1;
			delete this;
		}
	}*/

#ifdef DEBUG
	bool AssertValid(void)
	{
		AssertPtr(this);
		Assert(m_cref > 0);
		return true;
	}
#endif // DEBUG

protected:
	long m_cref;
};

/*************************************************************************************
	Useful macros to AddRef "this" or any GenRefObj object in the current scope.
*************************************************************************************/
class _Lock_GenRefObj
{
private:
	GenRefObj *m_punk;
public:
	_Lock_GenRefObj(GenRefObj *punk) {
		m_punk = punk;
		if (m_punk)
			m_punk->AddRef();
	}
	~_Lock_GenRefObj(void) {
		if (m_punk)
			m_punk->Release();
	}
};

#define GenLockThis() _Lock_GenRefObj _lock_this_##__LINE__(this)
#define GenLockObj(pobj) _Lock_GenRefObj _lock_obj_##__LINE__(pobj)




/*----------------------------------------------------------------------------------------------
	Smart pointer class. This handles automatically addrefing and releasing com interface
	pointers.
----------------------------------------------------------------------------------------------*/
template<typename _Cls> class GenSmartPtr
{
public:
	typedef typename _Cls Class;

	// Destructor.
	~GenSmartPtr(void)
	{
		if (m_pobj)
		{
			m_pobj->Release();
			m_pobj = NULL;
		}
	}

	// Default constructor.
	GenSmartPtr(void)
	{
		m_pobj = NULL;
	}

	// Copy the pointer and AddRef().
	template<typename _Cls> GenSmartPtr(const GenSmartPtr & qobj)
	{
		m_pobj = qobj.m_pobj;
		if (m_pobj)
			m_pobj->AddRef();
	}

	// Saves the interface.
	GenSmartPtr(Class * pobj)
	{
		m_pobj = pobj;
		if (m_pobj)
			m_pobj->AddRef();
	}

	// Stores the interface.
	GenSmartPtr & operator=(Class * pobj)
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
	template<typename _Cls> GenSmartPtr & operator=(const GenSmartPtr & qobj)
	{
		return operator=(qobj.m_pobj);
	}

	// Saves/sets the interface without AddRef()ing. This call
	// will release any previously acquired interface.
	void Attach(Class * pobj)
	{
		if (m_pobj)
			m_pobj->Release();
		m_pobj = pobj;
	}

	// Simply NULL the interface pointer so that it isn't Released()'ed.
	Class * Detach()
	{
		Class * pobj = m_pobj;
		m_pobj = NULL;
		return pobj;
	}

	// Return the interface. This value may be NULL.
	operator Class *() const
	{
		return m_pobj;
	}

	// Return the interface. This value may be NULL.
	Class * Ptr(void) const
	{
		return m_pobj;
	}

	// Returns the address of the interface pointer contained in this
	// class. This is useful when using the COM/OLE interfaces to create
	// this interface.
	Class ** operator&()
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
	IntfNoRelease<Class> * operator->() const
	{
		AssertPtr(m_pobj);
		return reinterpret_cast<IntfNoRelease<Class> *>(m_pobj);
	}

	// This operator is provided so that simple boolean expressions will
	// work.  For example: "if (!p) ...". Returns true if the pointer is NULL.
	bool operator!() const
	{
		return m_pobj == NULL;
	}

	// Compare with pointer.
	template<typename _ClsPtr> bool operator==(_ClsPtr pobj) const
	{
		return pobj == m_pobj;
	}

	template<typename _ClsPtr> bool operator!=(_ClsPtr pobj) const
	{
		return pobj != m_pobj;
	}

	// Compares with GenSmartPtr.
	template<> bool operator==(const GenSmartPtr & qobj) const
	{
		return qobj.m_pobj == m_pobj;
	}

	template<> bool operator!=(const GenSmartPtr & qobj) const
	{
		return qobj.m_pobj != m_pobj;
	}

	// Clears the pointer.
	void Clear()
	{
		if (m_pobj)
		{
			m_pobj->Release();
			m_pobj = NULL;
		}
	}

	// AddRefs the pointer if it's not null.
	void AddRef()
	{
		if (m_pobj)
			m_pobj->AddRef();
	}

	void Create(void)
	{
		if (m_pobj)
		{
			m_pobj->Release();
			m_pobj = NULL;
		}
		m_pobj = NewObj Class;
	}

private:
	// The Class pointer.
	Class * m_pobj;
};


/*----------------------------------------------------------------------------------------------
	Reverse comparison operators for GenSmartPtr.
----------------------------------------------------------------------------------------------*/
template<typename _Cls, typename _ClsPtr>
	bool operator==(_Cls * pobj, const GenSmartPtr<_ClsPtr> & qobj)
{
	return qobj == pobj;
}

template<typename _Cls, typename _ClsPtr>
	bool operator!=(_Cls * pobj, const GenSmartPtr<_ClsPtr> & qobj)
{
	return qobj != pobj;
}

/*----------------------------------------------------------------------------------------------
	ValidReadPtr for a smart pointer. This is so AssertPtr works on a smart pointer.
----------------------------------------------------------------------------------------------*/
template<typename T> inline bool ValidReadPtr(const GenSmartPtr<T> & qt)
{
	return !::IsBadReadPtr((T *)qt, isizeof(T));
}

#endif // !GenSmartPtr_H

/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GenericFactory.h
Responsibility: Darrell Zook
Last reviewed: 9/8/99

Description:
	Header file for GenericFactory.cpp, which supplies a generic class factory.

	This file should be included in every DLL that implements COM objects that can be
		created using CoCreateInstance.

	A static GenericFactory variable should be created for each COM class that is creatable
		via CoCreate Instance. An example of how this should be done is shown below
		(replace each line with the correct information for your class):

		GenericFactory g_factObjectViewer(
			"SIL.ObjectViewer",						<-- ProgID
			&CLSID_ObjectViewer,					<-- CLSID
			"Fieldworks Object Viewer Application",	<-- Description of the class
			"Apartment",							<-- Threading model
			&CObjectViewer::CreateCom);				<-- Static create method

	For most objects, the threading model should be "Apartment". This means that it will
		do all its calculations on its own thread and doesn't need to communicate with
		any other threads. Use "Both" if you are sure your object is thread safe and you
		need to communicate with other threads.
	NOTE: The threading model should never be "Free".
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef GENERICFACTORY_H
#define GENERICFACTORY_H 1


// An object create function. GenericFactory's constructor needs one of these.
typedef void (*PfnCreateObj)(IUnknown * punkOuter, REFIID iid, void ** ppvObj);

/*----------------------------------------------------------------------------------------------
	This is a base class that contains a little bit of the functionality of GenericFactory:
	the ability to store a progid. It is used by the END_COM_METHOD macro for classes that
	don't have a real class factory.

	Hungarian: dfact
----------------------------------------------------------------------------------------------*/
class DummyFactory
{
protected:
	// The name of this DLL.
	LPCTSTR m_pszProgId;

public:
	// Constructor
	DummyFactory(LPCTSTR pszProgId)
	{
		AssertPtr(pszProgId);
		m_pszProgId = pszProgId;
	}
	LPCTSTR GetProgId(void)
		{ return m_pszProgId; }
	// Useful for interrogating special derived factories:
	virtual bool IsRealFactory()
		{ return false; }
};

/*----------------------------------------------------------------------------------------------
	This is a generic class factory. Instances of this should be globals, not allocated with
		new. This is intended to be generic enough to be suitable for any object that can be
		instantiated with CoCreateInstance.

	Hungarian: fact
----------------------------------------------------------------------------------------------*/
class GenericFactory :
	public IClassFactory,
	public DummyFactory,
	public BalTreeBase<GenericFactory, CLSID>
{
public:
	// These methods are used to register and unregister the factories.
	static void RegisterFactories();
	static void UnregisterFactories();
	static void RegisterClassObjects(void);
	static void UnregisterClassObjects(void);

	// This method is used to find a specific factory.
	static void GetClassFactory(REFCLSID clsid, REFIID iid, void ** ppv);

	// Constructor
	GenericFactory(
		LPCTSTR pszProgId, const CLSID * pclsid, LPCTSTR pszDesc,
		LPCTSTR pszThreadModel, PfnCreateObj pfnCreate);

	// Function required by BalTreeBase.
	int CompareKey(const CLSID & clsid);

	// Methods of IClassFactory.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void);
	STDMETHOD_(UCOMINT32, Release)(void);
	STDMETHOD(LockServer)(BOOL fLock);
	STDMETHOD(CreateInstance)(IUnknown * punkOuter, REFIID iid, void ** ppv);

	// Member access functions.
	const CLSID & GetClsid(void)
		{ return *m_pclsid; }

	// Other methods used to register and unregister a factory.
	virtual void Register(void);
	virtual void Unregister(void);
	virtual void RegisterClassObject(void);
	virtual void UnregisterClassObject(void);

	// Useful for interrogating special derived factories:
	virtual bool IsRealFactory()
		{ return true; }

protected:
	static GenericFactory * s_pfactRoot;

	// Points to the clsid.
	const CLSID * m_pclsid;

	// Description of the DLL.
	LPCTSTR m_pszDesc;

	// Threading model.
	LPCTSTR m_pszThreadModel;

	// Function to create a COM object.
	PfnCreateObj m_pfnCreate;

	// Identifies the registered class object.
	DWORD m_dwRegister;

	static void RegisterFactoryNode(GenericFactory * pfact);
	static void UnregisterFactoryNode(GenericFactory * pfact);

private:
#if WIN32
	HKEY GetClassesRoot(RegKey& baseKey);
#endif

#ifdef DEBUG
	// This variable is used to try to enforce that there are no local variables of
	// type GenericFactory. If this is not initialized to 0, it is not global.
	int m_nDummy;
#endif

	// These make it illegal to use new and delete on a GenericFactory.
	void * operator new(size_t cb) throw()
	{
		Assert(false);
		return NULL;
	}
	void operator delete(void * pv)
	{
		Assert(false);
	}
};

#endif //!GENERICFACTORY_H

/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GenericFactory.cpp
Responsibility: Steve McConnel (was Darrell Zook)
Last reviewed: 9/8/99

Description:
	Generic ClassFactory.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop

#include "BinTree_i.cpp"

#if !WIN32
#include <iostream>
#include "COMLibrary.h"
#endif

#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	This class is used to hook module entry points.
	It more logically belongs in GenericFactory.cpp, from whence I (JT) removed it, but
	that file is compiled into a library and can't differ for
	Hungarian: fme
----------------------------------------------------------------------------------------------*/
class FactoryModuleEntry : public ModuleEntry
{
public:

	FactoryModuleEntry()
	{
	}
	virtual void ProcessAttach(void)
		{ GenericFactory::RegisterClassObjects(); }
	virtual void ProcessDetach(void)
		{ GenericFactory::UnregisterClassObjects(); }

	virtual void GetClassFactory(REFCLSID clsid, REFIID iid, void ** ppv)
		{ GenericFactory::GetClassFactory(clsid, iid, ppv); }
	virtual void RegisterServer(void)
		{ GenericFactory::RegisterFactories(); }
	virtual void UnregisterServer(void)
		{ GenericFactory::UnregisterFactories(); }
};

FactoryModuleEntry g_fme;

// Don't auto initialize this! It will be set to 0 automatically before any
// constructor code is run. If you auto-initialize it we run the risk of some
// constructor code accessing it before it has been set.
GenericFactory * GenericFactory::s_pfactRoot;

static DummyFactory g_fact(_T("SIL.AppCore.GenericFactory"));

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
GenericFactory::GenericFactory(
	LPCTSTR pszProgId, const CLSID * pclsid, LPCTSTR pszDesc,
	LPCTSTR pszThreadModel, PfnCreateObj pfnCreate)
: DummyFactory(pszProgId), BalTreeBase<GenericFactory, CLSID>(&s_pfactRoot, *pclsid)
{
	AssertPtr(pclsid);
	AssertPtr(pszDesc);
	AssertPtr(pszThreadModel);
	AssertPfn(pfnCreate);
#ifdef DEBUG
	Assert(0 == m_nDummy);
#endif

	m_pclsid = pclsid;
	m_pszDesc = pszDesc;
	m_pszThreadModel = pszThreadModel;
	m_pfnCreate = pfnCreate;
}

/*----------------------------------------------------------------------------------------------
	QueryInterface
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GenericFactory::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	// Set to the proper interface
	if (IID_IUnknown == iid)
		*ppv = static_cast<IUnknown *>(this);
	else if (IID_IClassFactory == iid)
		*ppv = static_cast<IClassFactory *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IClassFactory);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	This just does a ModuleAddRef since GenericFactories are globals.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) GenericFactory::AddRef(void)
{
	return ModuleEntry::ModuleAddRef();
}

/*----------------------------------------------------------------------------------------------
	This just does a ModuleRelease since GenericFactories are globals.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) GenericFactory::Release(void)
{
	return ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	LockServer - just calls AddRef or Release.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GenericFactory::LockServer(BOOL fLock)
{
	if (fLock)
		AddRef();
	else
		Release();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Creates an object of the specified CLSID. It uses the create function passed in by
		the derived class in the constructor to create the object.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GenericFactory::CreateInstance(IUnknown * punkOuter, REFIID iid, void ** ppv)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppv);

	(*m_pfnCreate)(punkOuter, iid, ppv);

	END_COM_METHOD(g_fact, IID_IClassFactory);
}


/*----------------------------------------------------------------------------------------------
	Method to compare a CLSID with *m_pclsid. This is required by BalTreeBase.

	It returns:
		< 0 if clsid is less than the key in this node, *m_pclsid.
		  0 if clsid is equal to the key in this node, *m_pclsid.
		> 0 if clsid is greater than the key in this node, *m_pclsid.

	Arguments:
		[in] clsid -> References the key to compare.
----------------------------------------------------------------------------------------------*/
int GenericFactory::CompareKey(const CLSID & clsid)
{
	AssertPtr(m_pclsid);
	if (&clsid == m_pclsid)
		return 0;
	return memcmp(&clsid, m_pclsid, isizeof(CLSID));
}

/*----------------------------------------------------------------------------------------------
	Static method to register the class objects (if in an exe server).
----------------------------------------------------------------------------------------------*/
void GenericFactory::RegisterClassObjects(void)
{
	if (ModuleEntry::IsExe() && s_pfactRoot)
		s_pfactRoot->RegisterClassObject();
}

/*----------------------------------------------------------------------------------------------
	Recursive instance method to register this class object and all its children
		(if in an exe server).
----------------------------------------------------------------------------------------------*/
void GenericFactory::RegisterClassObject(void)
{
	CheckHr(CoRegisterClassObject(*m_pclsid, static_cast<IClassFactory *>(this),
		CLSCTX_LOCAL_SERVER, REGCLS_MULTIPLEUSE, &m_dwRegister));
	if (m_rgpobj[0])
		m_rgpobj[0]->RegisterClassObject();
	if (m_rgpobj[1])
		m_rgpobj[1]->RegisterClassObject();
}

/*----------------------------------------------------------------------------------------------
	Static method to unregister the class objects (if in an exe server).
----------------------------------------------------------------------------------------------*/
void GenericFactory::UnregisterClassObjects(void)
{
	if (ModuleEntry::IsExe() && s_pfactRoot)
		s_pfactRoot->UnregisterClassObject();
}

/*----------------------------------------------------------------------------------------------
	Recursive instance method to unregister this class object and all its children
		(if in an exe server).
----------------------------------------------------------------------------------------------*/
void GenericFactory::UnregisterClassObject(void)
{
	CheckHr(CoRevokeClassObject(m_dwRegister));
	if (m_rgpobj[0])
		m_rgpobj[0]->UnregisterClassObject();
	if (m_rgpobj[1])
		m_rgpobj[1]->UnregisterClassObject();
}

/*----------------------------------------------------------------------------------------------
	Static method to find a class factory (from the given CLSID). If the requested class
	factory is not found, *ppv is set to NULL.
----------------------------------------------------------------------------------------------*/
void GenericFactory::GetClassFactory(REFCLSID clsid, REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	GenericFactory * pfact = Find(s_pfactRoot, clsid);
	if (pfact)
		CheckHr(pfact->QueryInterface(iid, ppv));
}


/*----------------------------------------------------------------------------------------------
	Static method to register all class factories.
----------------------------------------------------------------------------------------------*/
void GenericFactory::RegisterFactories()
{
	RegisterFactoryNode(s_pfactRoot);
}


/*----------------------------------------------------------------------------------------------
	Recursive static method to register the current node and all its children.
----------------------------------------------------------------------------------------------*/
void GenericFactory::RegisterFactoryNode(GenericFactory * pfact)
{
	if (!pfact)
		return;

	pfact->Register();
	RegisterFactoryNode(pfact->m_rgpobj[0]);
	RegisterFactoryNode(pfact->m_rgpobj[1]);
}

/*----------------------------------------------------------------------------------------------
	Static method to unregister all class factories.
----------------------------------------------------------------------------------------------*/
void GenericFactory::UnregisterFactories()
{
	UnregisterFactoryNode(s_pfactRoot);
}

/*----------------------------------------------------------------------------------------------
	Recursive static method to unregister the current node and all its children.
----------------------------------------------------------------------------------------------*/
void GenericFactory::UnregisterFactoryNode(GenericFactory * pfact)
{
	if (!pfact)
		return;

	pfact->Unregister();
	UnregisterFactoryNode(pfact->m_rgpobj[0]);
	UnregisterFactoryNode(pfact->m_rgpobj[1]);
}

#ifdef WIN32
/*----------------------------------------------------------------------------------------------
	Returns the registry key used for registering classes. This is normally HKEY_CLASSES_ROOT,
	unless we register on a per user basis in which case it is
	HKEY_CURRENT_USER\Software\Classes.
	When we register per user hkCuClassesRoot contains the opened key so that it can be
	properly closed at the end.
----------------------------------------------------------------------------------------------*/
HKEY GenericFactory::GetClassesRoot(RegKey& hkCuClassesRoot)
{
	hkCuClassesRoot = NULL;
	if (ModuleEntry::PerUserRegistration())
	{
		// register per user
		DWORD dwT;
		if (ERROR_SUCCESS != ::RegCreateKeyEx(HKEY_CURRENT_USER, _T("Software\\Classes"), 0, REG_NONE,
			REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, NULL, &hkCuClassesRoot, &dwT))
		{
			ThrowHr(WarnHr(E_FAIL));
		}
		return hkCuClassesRoot;
	}
	return HKEY_CLASSES_ROOT;
}
#else
	// TODO-Linux: port
#endif

/*----------------------------------------------------------------------------------------------
	Add information for this class to the registry.

	The following keys are created by Register and are deleted by Unregister below.
		The text in brackets are keys; the text following the line with brackets gives the
		values stored in the key (@ = default value). The values between double wedges are
		unique for each class.

		[HKEY_CLASSES_ROOT\<<m_pszProgId>>]
		@=<<m_pszDesc>>

		[HKEY_CLASSES_ROOT\<<m_pszProgId>>\CLSID]
		@=<<*m_pclsid>>

		[HKEY_CLASSES_ROOT\CLSID\<<*m_pclsid>>]
		@=<<m_pszDesc>>

		[HKEY_CLASSES_ROOT\CLSID\<<*m_pclsid>>\ProgID]
		@=<<m_pszProgId>>

		[HKEY_CLASSES_ROOT\CLSID\<<*m_pclsid>>\InprocServer32]
		@=<<Pathname of module>>
		"ThreadingModel"=<<m_pszThreadModel>>

		--- OR, if it is an EXE---
		[HKEY_CLASSES_ROOT\CLSID\<<*m_pclsid>>\LocalServer32]
		@=<<Pathname of module>>
		(Note that a LocalServer does not have a threading model--JohnT)

	If we are registering per user we use [HKEY_CURRENT_USER\Software\Classes] instead of
	HKEY_CLASSES_ROOT.
----------------------------------------------------------------------------------------------*/
void GenericFactory::Register()
{
#if WIN32

	// Register server info
	DWORD dwT;
	RegKey hkProgId;
	RegKey hkProgIdClsid;
	RegKey hkClsid;
	RegKey hkMyClsid;
	RegKey hkMyClsidProgId;
	RegKey hkMyClsidServer;
	StrAppBuf strbClsid;
	const achar * pszPath;

	// Make a clean start
	try
	{
		Unregister();
	}
	catch (...)
	{
	}

	// Get pathname to the DLL or EXE
	pszPath = ModuleEntry::GetModulePathName();

	DoAssert(strbClsid.Format(_T("{%g}"), m_pclsid));

	RegKey hkCuClassesRoot;
	HKEY hkClassesRoot = GetClassesRoot(hkCuClassesRoot);

	// Create the ProgID key and set its default value to the description string.
	if (ERROR_SUCCESS != ::RegCreateKeyEx(hkClassesRoot, m_pszProgId, 0, REG_NONE,
		REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, NULL, &hkProgId, &dwT))
	{
		ThrowHr(WarnHr(E_FAIL));
	}
	if (ERROR_SUCCESS != ::RegSetValueEx(hkProgId, NULL, 0, REG_SZ,
		(const BYTE *)m_pszDesc, (StrLen(m_pszDesc) + 1) * isizeof(achar)))
	{
		ThrowHr(WarnHr(E_FAIL));
	}

	// Create the ProgID\CLSID key and set its value to the class ID string.
	if (ERROR_SUCCESS != ::RegCreateKeyEx(hkProgId, _T("CLSID"), 0, REG_NONE,
		REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, NULL, &hkProgIdClsid, &dwT))
	{
		ThrowHr(WarnHr(E_FAIL));
	}
	if (ERROR_SUCCESS != ::RegSetValueEx(hkProgIdClsid, NULL, 0, REG_SZ,
		(const BYTE *)strbClsid.Chars(), (strbClsid.Length() + 1) * isizeof(achar)))
	{
		ThrowHr(WarnHr(E_FAIL));
	}

	// Open CLSID key.
	if (ERROR_SUCCESS != RegCreateKeyEx(hkClassesRoot, _T("CLSID"), 0, REG_NONE,
		REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, NULL, &hkClsid, &dwT))
	{
		ThrowHr(WarnHr(E_FAIL));
	}

	// Create CLSID\clsid key and set its value to the description string.
	if (ERROR_SUCCESS != ::RegCreateKeyEx(hkClsid, strbClsid.Chars(), 0, REG_NONE,
		REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, NULL, &hkMyClsid, &dwT))
	{
		ThrowHr(WarnHr(E_FAIL));
	}
	if (ERROR_SUCCESS != ::RegSetValueEx(hkMyClsid, NULL, 0, REG_SZ,
		(const BYTE *)m_pszDesc, (StrLen(m_pszDesc) + 1) * isizeof(achar)))
	{
		ThrowHr(WarnHr(E_FAIL));
	}

	// Create the CLSID\clsid\ProgID key and set its value to the ProgID.
	if (ERROR_SUCCESS != ::RegCreateKeyEx(hkMyClsid, _T("ProgID"), 0, REG_NONE,
		REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, NULL, &hkMyClsidProgId, &dwT))
	{
		ThrowHr(WarnHr(E_FAIL));
	}
	if (ERROR_SUCCESS != ::RegSetValueEx(hkMyClsidProgId, NULL, 0, REG_SZ,
		(const BYTE *)m_pszProgId, (StrLen(m_pszProgId) + 1) * isizeof(achar)))
	{
		ThrowHr(WarnHr(E_FAIL));
	}

	// Rest depends on whether we are an EXE or DLL. Easiest is to check the pathname
	// Review JohnT: is there any other strategy that might be more reliable?
	// Do e.g. Korean Windows also use .EXE?

	int cchPath = _tcslen(pszPath);
	if (cchPath < 4)
		ThrowHr(WarnHr(E_FAIL)); // neither exe nor dll!

	achar chEx1 = pszPath[cchPath - 3];
	achar chEx2 = pszPath[cchPath - 2];
	achar chEx3 = pszPath[cchPath - 1];
	bool fExe = chEx1 == 'e' || chEx1 == 'E';
	if (fExe)
	{
		if ((chEx2 != 'x' && chEx2 != 'X') ||
			(chEx3 != 'e' && chEx3 != 'E'))
		{
			ThrowHr(WarnHr(E_FAIL));
		}
	}
	else
	{
		if ((chEx1 != 'd' && chEx1 != 'D') ||
			(chEx2 != 'l' && chEx2 != 'L') ||
			(chEx3 != 'l' && chEx3 != 'L'))
		{
			ThrowHr(WarnHr(E_FAIL));
		}
	}

	if (fExe)
	{
		// Create the CLSID\clsid\LocalServer32 key and set its value to the EXE path.
		if (ERROR_SUCCESS != ::RegCreateKeyEx(hkMyClsid, _T("LocalServer32"), 0, REG_NONE,
			REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, NULL, &hkMyClsidServer, &dwT))
		{
			ThrowHr(WarnHr(E_FAIL));
		}
	}
	else
	{
		// Create the CLSID\clsid\InprocServer32 key and set its value to the DLL path.
		if (ERROR_SUCCESS != ::RegCreateKeyEx(hkMyClsid, _T("InprocServer32"), 0, REG_NONE,
			REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, NULL, &hkMyClsidServer, &dwT))
		{
			ThrowHr(WarnHr(E_FAIL));
		}
	}

	if (ERROR_SUCCESS != ::RegSetValueEx(hkMyClsidServer, NULL, 0, REG_SZ,
		(const BYTE *)pszPath, (StrLen(pszPath) + 1) * isizeof(achar)))
	{
		ThrowHr(WarnHr(E_FAIL));
	}

	if (!fExe)
	{
		// Set the CLSID\clsid\InprocServer32\ThreadingModel value.
		if (ERROR_SUCCESS != ::RegSetValueEx(hkMyClsidServer, _T("ThreadingModel"), 0, REG_SZ,
			(const BYTE *)m_pszThreadModel, (StrLen(m_pszThreadModel) + 1) * isizeof(achar)))
		{
			ThrowHr(WarnHr(E_FAIL));
		}
	}

#else //WIN32

	CoRegisterClassInfo(m_pclsid, StrAnsi(m_pszProgId).Chars(),
		StrAnsi(m_pszDesc).Chars());

#endif//WIN32
}

/*----------------------------------------------------------------------------------------------
	Remove information for this class from the registry.
----------------------------------------------------------------------------------------------*/
void GenericFactory::Unregister()
{
#if WIN32

	RegKey hkMyOldProgId;
	RegKey hkClsid;

	// Find the clsid that was originally stored in ProgID.
	achar szClsid[100] = {0};
	DWORD cchClsid = sizeof(szClsid);
	StrAppBuf strbProgId = m_pszProgId;
	strbProgId += _T("\\CLSID");
	RegKey hkCuClassesRoot;
	HKEY hkClassesRoot = GetClassesRoot(hkCuClassesRoot);

	long lnRes = RegOpenKeyEx(hkClassesRoot, strbProgId.Chars(), 0, KEY_READ | KEY_WRITE,
		&hkMyOldProgId);
	if (ERROR_SUCCESS == lnRes)
	{
		RegQueryValueEx(hkMyOldProgId, NULL, NULL, NULL, (BYTE *)szClsid, &cchClsid);
	}
	else if (ERROR_FILE_NOT_FOUND != lnRes)
		ThrowHr(WarnHr(E_FAIL));
	hkMyOldProgId.Close();

	// Delete ProgID. Don't return an error if the key didn't exist before.
	lnRes = DeleteSubKey(hkClassesRoot, m_pszProgId);
	if (ERROR_SUCCESS != lnRes && ERROR_FILE_NOT_FOUND != lnRes)
		ThrowHr(WarnHr(E_FAIL));

	// Delete CLSID\clsid.
	lnRes = RegOpenKeyEx(hkClassesRoot, _T("CLSID"), 0, KEY_READ | KEY_WRITE, &hkClsid);
	if (ERROR_SUCCESS == lnRes)
	{
		StrAppBuf strbClsid;

		// Delete the original CLSID\clsid that was stored in ProgID
		if (*szClsid)
		{
			lnRes = DeleteSubKey(hkClsid, szClsid);
			if (ERROR_SUCCESS != lnRes && ERROR_FILE_NOT_FOUND != lnRes)
				ThrowHr(WarnHr(E_FAIL));
		}

		// Delete the current CLSID\clsid
		if (!strbClsid.Format(_T("{%g}"), m_pclsid))
			ThrowHr(WarnHr(E_FAIL));
		lnRes = DeleteSubKey(hkClsid, strbClsid.Chars());
		if (ERROR_SUCCESS != lnRes && ERROR_FILE_NOT_FOUND != lnRes)
			ThrowHr(WarnHr(E_FAIL));
	}
	else if (ERROR_FILE_NOT_FOUND != lnRes)
		ThrowHr(WarnHr(E_FAIL));

#else //WIN32

	// TODO-Linux: port
	// Call equivalent functionality from COMSupportLibrary

#endif//WIN32
}

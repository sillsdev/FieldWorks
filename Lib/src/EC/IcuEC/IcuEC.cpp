// IcuEC.cpp : Implementation of DLL Exports.

#include "stdafx.h"
#include "resource.h"
#include "IcuEC.h"
#include "dlldatax.h"
#include "unicode/putil.h"
#include "unicode/uclean.h"

class CIcuECModule : public CAtlDllModuleT< CIcuECModule >
{
public :
	CIcuECModule()
	{
		const char * pszDir = u_getDataDirectory();
		if (!pszDir || !*pszDir)
		{
			// The ICU Data Directory is not yet set.  Get the root directory from the registry
			// and set the ICU data directory based on that value.
			CRegKey keyFW;
			if( keyFW.Open(HKEY_LOCAL_MACHINE, _T("Software\\SIL"), KEY_READ) == ERROR_SUCCESS )
			{
				ULONG nValueSize = _MAX_PATH;
				TCHAR lpValue[_MAX_PATH];
				if( keyFW.QueryStringValue(_T("Icu40Dir"),lpValue,&nValueSize) == ERROR_SUCCESS )
				{
					USES_CONVERSION;
					u_setDataDirectory(OLE2A(lpValue));
				}
			}
		}
		// ICU docs say to do this after the directory is set, but before others are called.
		// And it can be called n times with little hit, but is Required for multi-threaded
		// use of ICU.
		UErrorCode status = U_ZERO_ERROR;
		u_init(&status);
	}
	~CIcuECModule()
	{
		// This will release the hold on cnvalias.icu (and possibly other ICU files) when we are done.
		// Don't call u_cleanup() here. u_cleanup() works on the application level. If the application
		// keeps running, but just our thread exits (in which we had loaded IcuEC), any calls to
		// ICU will fail after calling u_cleanup() (because the data directory isn't set anymore).
		//u_cleanup();
	}
	DECLARE_LIBID(LIBID_IcuECLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_ICUEC, "{BD78AF0B-F806-48f9-A980-71A16867DE34}")
};

CIcuECModule _AtlModule;

class CIcuECApp : public CWinApp
{
public:

// Overrides
	virtual BOOL InitInstance();
	virtual int ExitInstance();

	DECLARE_MESSAGE_MAP()
};

BEGIN_MESSAGE_MAP(CIcuECApp, CWinApp)
END_MESSAGE_MAP()

CIcuECApp theApp;

BOOL CIcuECApp::InitInstance()
{
#ifdef _MERGE_PROXYSTUB
	if (!PrxDllMain(m_hInstance, DLL_PROCESS_ATTACH, NULL))
		return FALSE;
#endif
	BOOL bRet = CWinApp::InitInstance();
	return bRet;
}

int CIcuECApp::ExitInstance()
{
	return CWinApp::ExitInstance();
}

// Used to determine whether the DLL can be unloaded by OLE
STDAPI DllCanUnloadNow(void)
{
#ifdef _MERGE_PROXYSTUB
	HRESULT hr = PrxDllCanUnloadNow();
	if (FAILED(hr))
		return hr;
#endif
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return (AfxDllCanUnloadNow()==S_OK && _AtlModule.GetLockCount()==0) ? S_OK : S_FALSE;
}


// Returns a class factory to create an object of the requested type
STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppv)
{
#ifdef _MERGE_PROXYSTUB
	if (PrxDllGetClassObject(rclsid, riid, ppv) == S_OK)
		return S_OK;
#endif
	return _AtlModule.DllGetClassObject(rclsid, riid, ppv);
}

// DllRegisterServer - Adds entries to the system registry
STDAPI DllRegisterServer(void)
{
	// registers object, typelib and all interfaces in typelib
	HRESULT hr = _AtlModule.DllRegisterServer();
#ifdef _MERGE_PROXYSTUB
	if (FAILED(hr))
		return hr;
	hr = PrxDllRegisterServer();
#endif
	// This is needed to release a hold on ICU data files caused by u_init().
	// (at least cnvalias.icu, to be specific).  Without this, the FieldWorks Nant build
	// process fails when it tries to replace the entire set of files by unzipping Icu40.zip.
	u_cleanup();
	return hr;
}


// DllUnregisterServer - Removes entries from the system registry
STDAPI DllUnregisterServer(void)
{
	HRESULT hr = _AtlModule.DllUnregisterServer();
#ifdef _MERGE_PROXYSTUB
	if (FAILED(hr))
		return hr;
	hr = PrxDllRegisterServer();
	if (FAILED(hr))
		return hr;
	hr = PrxDllUnregisterServer();
#endif
	// This is needed to release a hold on ICU data files caused by u_init().
	// (at least cnvalias.icu, to be specific).  Without this, the FieldWorks Nant build
	// process fails when it tries to replace the entire set of files by unzipping Icu40.zip.
	u_cleanup();
	return hr;
}

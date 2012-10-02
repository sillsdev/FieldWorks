#include "StdAfx.h"
#include "clrloader.h"

// some interfaces that will be used are from this namespace.
using namespace mscorlib;

// static class variables

// auto_ptr is used to enforce clean-up on DLL unload.
std::auto_ptr<CCLRLoader> CCLRLoader::m_pInstance;


// forward declarations
static HRESULT GetDllDirectory(TCHAR *szPath, DWORD nPathBufferSize);

// class functions

CCLRLoader *CCLRLoader::TheInstance()
{
	if (m_pInstance.get() == NULL)
	{
		m_pInstance = std::auto_ptr<CCLRLoader>(new CCLRLoader());
	}

	return m_pInstance.get();
}

CCLRLoader::CCLRLoader(void)
	: m_pHost(NULL)
	, m_pLocalDomain(NULL)
{
}

CCLRLoader::~CCLRLoader(void)
{
	if (m_pLocalDomain)
	{
		m_pLocalDomain->Release();
	}

	if (m_pHost)
	{
		m_pHost->Release();
	}
}

// LoadCLR: This method starts up the Common Language Runtime
HRESULT CCLRLoader::LoadCLR()
{
	HRESULT hr = S_OK;

	// ensure the CLR is only loaded once.
	if (m_pHost != NULL)
		return hr;

	// Load runtime into the process ...
	hr = CorBindToRuntimeEx(
		 // version, use default
		0,
		 // flavor, use default
		0,
		 // domain-neutral"ness" and gc settings
		STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN |	STARTUP_CONCURRENT_GC,
		CLSID_CorRuntimeHost,
		IID_ICorRuntimeHost,
		(PVOID*) &m_pHost);

	// couldn't load....
	if (!SUCCEEDED(hr))
	{
		return hr;
	}

	// start CLR
	return m_pHost->Start();
}

// CreateLocalAppDomain: the function creates AppDomain with BaseDirectory
// set to location of unmanaged DLL containing this code. Assuming that the
// target assembly is located in the same directory, the classes from this
// assemblies can be instantiated by calling _AppDomain::Load() method.
HRESULT CCLRLoader::CreateLocalAppDomain()
{
	USES_CONVERSION;

	HRESULT hr = S_OK;

	// ensure the domain is created only once
	if (m_pLocalDomain != NULL)
	{
		return hr;
	}

	CComPtr<IUnknown> pDomainSetupPunk;
	CComPtr<IAppDomainSetup> pDomainSetup;
	CComPtr<IUnknown> pLocalDomainPunk;
	TCHAR szDirectory[MAX_PATH + 1];

	// Create an AppDomainSetup with the base directory pointing to the
	// location of the managed DLL. The assumption is made that the
	// target assembly is located in the same directory
	IfFailGo( m_pHost->CreateDomainSetup(&pDomainSetupPunk) );
	IfFailGo( pDomainSetupPunk->QueryInterface(__uuidof(pDomainSetup),
		(LPVOID*)&pDomainSetup) );

	// Get the location of the hosting DLL
	IfFailGo( ::GetDllDirectory(szDirectory, sizeof(szDirectory)/sizeof(szDirectory[0])) );

	// Configure the AppDomain to search for assemblies in the above directory
	pDomainSetup->put_ApplicationBase(CComBSTR(szDirectory));

	// Create an AppDomain that will run the managed assembly
	IfFailGo( m_pHost->CreateDomainEx(T2W(szDirectory),
		pDomainSetupPunk, 0, &pLocalDomainPunk) );

	// Cast IUnknown pointer to _AppDomain pointer
	IfFailGo( pLocalDomainPunk->QueryInterface(__uuidof(m_pLocalDomain),
		(LPVOID*)&m_pLocalDomain) );

Error:
   return hr;
}

// CreateInstance:
HRESULT CCLRLoader::CreateInstance(LPCWSTR szAssemblyName, LPCWSTR szClassName, const IID &riid, void ** ppvObject)
{
	HRESULT hr = S_OK;
	_ObjectHandle *pObjHandle = NULL;
	VARIANT v;

	// Ensure the common language runtime is running ...
	IfFailGo( LoadCLR() );

	// In order to securely load an assembly, its fully qualified strong name
	// and not the filename must be used. To do that, the target AppDomain's
	// base directory needs to point to the directory where the assembly is
	// residing. CreateLocalAppDomain() ensures that such AppDomain exists.
	IfFailGo( CreateLocalAppDomain() );

	// Create an instance of the managed class
	IfFailGo(m_pLocalDomain->CreateInstance(CComBSTR(szAssemblyName),
		CComBSTR(szClassName), &pObjHandle));

	// extract interface pointer from the object handle
	VariantInit(&v);
	hr = pObjHandle->Unwrap(&v);
	// assert(v.pdispVal);
	IfFailGo(v.pdispVal->QueryInterface(riid, ppvObject));

Error:

   return hr;
}

// GetDllDirectory() gets loaction directory of DLL containing this code
static HRESULT GetDllDirectory(TCHAR *szPath, DWORD nPathBufferSize)
{
	HMODULE hInstance = _AtlBaseModule.GetModuleInstance();

	if (hInstance == 0)
	{
		return E_FAIL;
	}

	TCHAR szModule[MAX_PATH + 1];
	DWORD dwFLen = ::GetModuleFileName(hInstance, szModule, MAX_PATH);

	if (dwFLen == 0)
	{
		return E_FAIL;
	}

	TCHAR *pszFileName;
	dwFLen = ::GetFullPathName(szModule, nPathBufferSize, szPath, &pszFileName);
	if (dwFLen == 0 || dwFLen >= nPathBufferSize)
	{
		return E_FAIL;
	}

	*pszFileName = 0;
	return S_OK;
}

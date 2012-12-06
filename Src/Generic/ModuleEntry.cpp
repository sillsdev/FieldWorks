/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ModuleEntry.cpp
Responsibility: Darrell Zook
Last reviewed: 9/8/99

Description:
	Main module entry points.

	Executable modules that use this code fall into the following categories:
		1) EXE Servers (#define EXE_MODULE)
		2) MFC ActiveX Controls (#define USING_MFC and USING_MFC_ACTIVEX)
		3) MFC DLLs (#define USING_MFC)
		4) non-MFC DLLs
		5) non-MFC EXE Servers (REVIEW DarrellZ: What needs to be done for these?)

	For more information on using this file, look in ModuleEntry.h.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#ifdef _MERGE_PROXYSTUB
#include "proxystub.h"
#endif
#ifndef WIN32
#include <sys/prctl.h>
#include <sys/types.h>
#endif
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


#if WIN32
#define DLLEXPORT__
#else
#define DLLEXPORT__ DLLEXPORT
#endif

// Don't auto initialize these! They will be set to 0 automatically before any
// constructor code is run. If you auto-initialize them we run the risk of some
// constructor code accessing and modifying them before they've been set.
HMODULE ModuleEntry::s_hmod;
long ModuleEntry::s_crefModule;
ModuleEntry * ModuleEntry::s_pmeFirst;
#if WIN32
StrAppBufPath ModuleEntry::s_strbpPath;
#else
TCHAR ModuleEntry::s_strbpPath[MAX_PATH];
#endif
ulong ModuleEntry::s_tid;			// Stays zero for non-EXE modules.

#if WIN32 // TODO-Linux
// The following instantiates the ATLConModule class so that we can use ATL to host ActiveX
// controls.
ATLConModule ModuleEntry::s_AtlModule;
bool ModuleEntry::s_fPerUserRegistration = false;
#endif // WIN32

#ifdef EXE_MODULE
IDataObjectPtr ModuleEntry::s_qdobjClipboard;	// data stored in clipboard by this app.
bool ModuleEntry::s_fIsExe = true;

#if WIN32
// The following GUID is used by the ATLConModule class in ModuleEntry.h.
// {52117230-1096-4d37-990F-A70C793D60BA}
const GUID LIBID_ATLConModule = { 0x52117230, 0x1096, 0x4d37,
								  { 0x99, 0xf, 0xa7, 0xc, 0x79, 0x3d, 0x60, 0xba } };
#endif
#else // EXE_MODULE
bool ModuleEntry::s_fIsExe = false;

#if WIN32
// The following GUID is used by the ATLConModule class in ModuleEntry.h.
// {FC20CBD9-9D85-4432-949F-51F690DF47C5}
const GUID LIBID_ATLConModule = { 0xfc20cbd9, 0x9d85, 0x4432,
								  { 0x94, 0x9f, 0x51, 0xf6, 0x90, 0xdf, 0x47, 0xc5 } };
#endif
#endif // EXE_MODULE


/*----------------------------------------------------------------------------------------------
	The constructor and destructor add and remove the item from the linked list.
----------------------------------------------------------------------------------------------*/
ModuleEntry::ModuleEntry(void) :
	LLBase<ModuleEntry>(&s_pmeFirst)
{
}

ModuleEntry::~ModuleEntry()
{
}

#ifdef EXE_MODULE

/***********************************************************************************************
	The code in this section only gets included for EXE servers.
/**********************************************************************************************/

void ModuleEntry::SetClipboard(IDataObject * pdobjClipboard)
{
	s_qdobjClipboard = pdobjClipboard;
}

/*----------------------------------------------------------------------------------------------
	For an exe, we post a WM_QUIT message to the main thread when the module reference
		count goes to zero.
----------------------------------------------------------------------------------------------*/
long ModuleEntry::ModuleRelease(void)
{
	long ln = InterlockedDecrement(&s_crefModule);
	if (!ln)
	{
		if (CoSuspendClassObjects() == S_OK)
			PostThreadMessage(s_tid, WM_QUIT, 0, 0);
	}
	return ln;
}

// Function used when command line includes -RegRedirect argument, to override main registry hives
// in accordance with what the WIX Tallow utility expects. This enables the installer to acquire
// at compile-time all registry settings that would be written during registration.
void OverrideRegistryKeys()
{
	HKEY hKey;
	if (ERROR_SUCCESS == RegCreateKeyEx(HKEY_LOCAL_MACHINE, L"SOFTWARE\\WiX\\HKCR", 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hKey, NULL))
	{
		// Create a new CLSID in our redirected hive, in case the registration function wants to read it:
		HKEY hKeyClsid;
		RegCreateKeyEx(hKey, L"CLSID", 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hKeyClsid, NULL);

		RegOverridePredefKey(HKEY_CLASSES_ROOT, hKey);
		RegCloseKey(hKeyClsid);
		RegCloseKey(hKey);
	}
	if (ERROR_SUCCESS == RegCreateKeyEx(HKEY_LOCAL_MACHINE, L"SOFTWARE\\WiX\\HKCU", 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hKey, NULL))
	{
		RegOverridePredefKey(HKEY_CURRENT_USER, hKey);
		RegCloseKey(hKey);
	}
	if (ERROR_SUCCESS == RegCreateKeyEx(HKEY_LOCAL_MACHINE, L"SOFTWARE\\WiX\\HKU", 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hKey, NULL))
	{
		RegOverridePredefKey(HKEY_USERS, hKey);
		RegCloseKey(hKey);
	}
	if (ERROR_SUCCESS == RegCreateKeyEx(HKEY_LOCAL_MACHINE, L"SOFTWARE\\WiX\\HKLM", 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hKey, NULL))
	{
		RegOverridePredefKey(HKEY_LOCAL_MACHINE, hKey);
		RegCloseKey(hKey);
	}
}

#ifdef USING_MFC

// Experimental extensions by JohnT for using ModuleEntry with an MFC EXE. Overriding WinMain
// is not a good idea. Instead, call ModuleEntry::Startup(m_hInstance, m_lpCmdLine) from your
// InitInstance(), and ModuleEntry::Shutdown() from your ExitInstance method.
// Note that Startup calls OleInitialize for you, and Shutdown calls OleUninitialize.
/*----------------------------------------------------------------------------------------------
	Call in starting up an MFC EXE, passing the command line.
	Returns true if program was invoked just to register server. This means you should quit
	without ever showing your main window.
----------------------------------------------------------------------------------------------*/
bool ModuleEntry::Startup(HINSTANCE hinst, LPSTR pszCmdLine)
{
	AssertPsz(pszCmdLine);

	// Initialize COM
	HRESULT hr = OleInitialize(NULL);
	s_qdobjClipboard.Clear();

	if (FAILED(hr))
	{
		WarnHr(hr);
		// REVIEW DarrellZ (JohnT): Should we alert the user? Probably, the app is going to quit
		return true;
	}

	s_hmod = hinst;
	s_tid = ::GetCurrentThreadId();

	// Look through the command line to see if the EXE server should be registered or
	// unregistered. If it should, quit the EXE server after performing the appropriate action.
	if (*pszCmdLine == '-' || *pszCmdLine == '/')
	{
		if (stricmp(pszCmdLine + 1, "UnregServer") == 0)
		{
			hr = ModuleEntry::ModuleUnregisterServer();
			return true;
		}
		else if (stricmp(pszCmdLine + 1, "RegServer") == 0)
		{
			hr = ModuleEntry::ModuleRegisterServer();
			return true;
		}
		else if (stricmp(pszCmdLine + 1, "RegRedirect") == 0)
		{
			// Used only during creation of installer:
			OverrideRegistryKeys();
			hr = ModuleEntry::ModuleRegisterServer();
			return true;
		}
	}

	ModuleEntry * pme;

	for (pme = s_pmeFirst; pme; pme = pme->m_pobjNext)
	{
		try
		{
			pme->ProcessAttach();
		}
		catch (const Throwable & thr)
		{
			hr = thr.Error();
		}
		catch (...)
		{
			hr = WarnHr(E_FAIL);
		}
		WarnHr(hr);
	}
	return false;
}

void ModuleEntry::ShutDown()
{
	ModuleEntry * pme;

	for (pme = s_pmeFirst; pme; pme = pme->m_pobjNext)
	{
		try
		{
			pme->ProcessDetach();
		}
		catch (const Throwable & thr)
		{
			WarnHr(thr.Error());
		}
		catch (...)
		{
			return WarnHr(E_FAIL);
		}
	}

	// Uninitialize COM, first shutting down the clipboard.
	if (s_qdobjClipboard.Ptr())
	{
		hr = OleIsCurrentClipboard(s_qdobjClipboard.Ptr());
		WarnHr(hr);
		if (hr == S_OK)
		{
			WarnHr(OleFlushClipboard());
		}
		s_qdobjClipboard.Clear();
	}
	OleUninitialize();
}

#else // !USING_MFC (but still EXE_MODULE)

/*----------------------------------------------------------------------------------------------
	The main entry point to the exe server.
----------------------------------------------------------------------------------------------*/
extern "C" int WINAPI WinMain(HINSTANCE hinst, HINSTANCE hinstPrev, LPSTR pszCmdLine,
	int nShowCmd)
{
	return ModuleEntry::WinMain(hinst, hinstPrev, pszCmdLine, nShowCmd);
}

/*----------------------------------------------------------------------------------------------
	WinMain for an exe server.
----------------------------------------------------------------------------------------------*/
int ModuleEntry::WinMain(HINSTANCE hinst, HINSTANCE hinstPrev, LPSTR pszCmdLine,
	int nShowCmd)
{
	AssertPsz(pszCmdLine);
	Assert(!hinstPrev);

	// Initialize COM
	HRESULT hr = OleInitialize(NULL);
	s_qdobjClipboard.Clear();

	if (FAILED(hr))
	{
		WarnHr(hr);
		// REVIEW DarrellZ: Should we alert the user?
		return 0;
	}

	s_hmod = hinst;
	s_tid = ::GetCurrentThreadId();

	int nRet = 0;
	bool fRun = true;

	// Look through the command line to see if the EXE server should be registered or
	// unregistered. If it should, quit the EXE server after performing the appropriate action.
	if (*pszCmdLine == '-' || *pszCmdLine == '/')
	{
		if (_stricmp(pszCmdLine + 1, "UnregServer") == 0)
		{
			hr = ModuleEntry::ModuleUnregisterServer();
			fRun = false;
		}
		else if (_stricmp(pszCmdLine + 1, "RegServer") == 0)
		{
			hr = ModuleEntry::ModuleRegisterServer();
			fRun = false;
		}
		else if (_stricmp(pszCmdLine + 1, "RegRedirect") == 0)
		{
			// Used only during creation of installer:
			OverrideRegistryKeys();
			hr = ModuleEntry::ModuleRegisterServer();
			return true;
		}
	}

	if (fRun)
	{
		hr = ModuleEntry::ModuleProcessAttach();
		WarnHr(hr);

		// NealA: I am working on this for UNICODE support
		// if not removed by 12-2002 please remove these comments
		// StrApp strCmdLine = pszCmdLine;
		nRet = Run(hinst, pszCmdLine, nShowCmd);

		hr = ModuleEntry::ModuleProcessDetach();
		WarnHr(hr);
		// Check for memory leaks in the main program.
		_CrtSetDbgFlag(_CrtSetDbgFlag(_CRTDBG_REPORT_FLAG) | _CRTDBG_LEAK_CHECK_DF);
	}
	else
	{
		WarnHr(hr);
	}

	// Uninitialize COM, first shutting down the clipboard.
	if (s_qdobjClipboard.Ptr())
	{
		hr = OleIsCurrentClipboard(s_qdobjClipboard.Ptr());
		WarnHr(hr);
		if (hr == S_OK)
		{
			WarnHr(OleFlushClipboard());
		}
		s_qdobjClipboard.Clear();
	}
	OleUninitialize();

	return nRet;
}
#endif // !USING_MFC

#else // !EXE_MODULE

/***********************************************************************************************
	The code in this section only gets included for DLLs.
/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	For a DLL, we don't have a global place to record this, so ignore it.
----------------------------------------------------------------------------------------------*/
void ModuleEntry::SetClipboard(IDataObject * pdobjClipboard)
{
}

/*----------------------------------------------------------------------------------------------
	For a DLL, we just decrement the count. (But DON'T put this inline in the header! It
	messes up the strategy for linking in the right version of ModuleEntry for DLLs versus
	EXEs.)
----------------------------------------------------------------------------------------------*/
long ModuleEntry::ModuleRelease(void)
		{ return InterlockedDecrement(&s_crefModule); }

#ifndef USING_MFC

/*----------------------------------------------------------------------------------------------
	DllMain. This is the main DLL entry point for a non-MFC DLL. For an MFC DLL, DllMain is
		in DllModul.cpp. Both DllMains call ModuleEntry::DllMain.
----------------------------------------------------------------------------------------------*/
extern "C" DLLEXPORT__ BOOL WINAPI DllMain(HMODULE hmod, DWORD dwReason, PVOID pvReserved)
{
#ifdef _MERGE_PROXYSTUB
	if (!PrxDllMain(hmod, dwReason, pvReserved))
		return FALSE;
#endif
	ENTER_DLL();
	return ModuleEntry::DllMain(hmod, dwReason);
}

#endif // !USING_MFC


/*----------------------------------------------------------------------------------------------
	This does the real work for DllMain. This gets called whether or not you use MFC.
----------------------------------------------------------------------------------------------*/
BOOL ModuleEntry::DllMain(HMODULE hmod, DWORD dwReason)
{
	bool fRet = true;
	ModuleAddRef();
	HRESULT hr;

#ifndef WIN32
#ifdef PR_SET_PTRACER
	// PR_SET_PTRACER might not be defined on all versions
	// Since Ubuntu 10.10 a normal user usually isn't allowed to use PTRACE anymore which
	// prevents call stacks and asserts from working properly. A workaround is to allow it
	// specifically for the current process (see /etc/sysctl.d/10-ptrace.conf)
	prctl(PR_SET_PTRACER, getpid(), 0, 0, 0);
#endif
#endif

	switch (dwReason)
	{
	case DLL_PROCESS_ATTACH:
		s_hmod = hmod;
		hr = ModuleProcessAttach();
		if (FAILED(hr))
			fRet = false;
		break;

	case DLL_THREAD_ATTACH:
		Assert(hmod == s_hmod);
		hr = ModuleThreadAttach();
		if (FAILED(hr))
			fRet = false;
		break;

	case DLL_PROCESS_DETACH:
		Assert(hmod == s_hmod);
		hr = ModuleProcessDetach();
		if (FAILED(hr))
			fRet = false;

#ifdef _MSC_VER
		// Memory leak detection at shutdown can be unreliable when COM objects are used in
		// managed code.

		// Check for memory leaks.
		//_CrtSetDbgFlag(_CrtSetDbgFlag(_CRTDBG_REPORT_FLAG) | _CRTDBG_LEAK_CHECK_DF);
#endif
		break;

	case DLL_THREAD_DETACH:
		Assert(hmod == s_hmod);
		hr = ModuleThreadDetach();
		if (FAILED(hr))
			fRet = false;
		break;

	default:
		fRet = false;
		break;
	}

	ModuleRelease();
	return fRet;
}


/*----------------------------------------------------------------------------------------------
	Retrieves the class factory for the given class ID. This function is required by COM and
		must be exported from the DLL.
----------------------------------------------------------------------------------------------*/
STDAPI DLLEXPORT__ DllGetClassObject(REFCLSID clsid, REFIID iid, VOID ** ppv)
{
	ENTER_DLL();
#ifdef _MERGE_PROXYSTUB
	if (PrxDllGetClassObject(clsid, iid, ppv) == S_OK)
		return S_OK;
#endif
	ModuleEntry::ModuleAddRef();
	HRESULT hr = ModuleEntry::ModuleGetClassObject(clsid, iid, ppv);
	ModuleEntry::ModuleRelease();
	return hr;
}

#ifdef USING_MFC_ACTIVEX

extern const GUID CDECL BASED_CODE _tlid;
extern const WORD _wVerMajor;
extern const WORD _wVerMinor;

/*----------------------------------------------------------------------------------------------
	Registers the DLL. This function is required by COM and must be exported from the DLL.
----------------------------------------------------------------------------------------------*/
STDAPI DLLEXPORT__ DllRegisterServer(void)
{
	AFX_MANAGE_STATE(_afxModuleAddrThis);

	if (!AfxOleRegisterTypeLib(AfxGetInstanceHandle(), _tlid))
		return ResultFromScode(SELFREG_E_TYPELIB);

	if (!COleObjectFactoryEx::UpdateRegistryAll(TRUE))
		return ResultFromScode(SELFREG_E_CLASS);

	return ModuleEntry::ModuleRegisterServer();
}

/*----------------------------------------------------------------------------------------------
	Unregisters the DLL. This function is required by COM and must be exported from the DLL.
----------------------------------------------------------------------------------------------*/
STDAPI DLLEXPORT__ DllUnregisterServer(void)
{
	AFX_MANAGE_STATE(_afxModuleAddrThis);

	if (!AfxOleUnregisterTypeLib(_tlid, _wVerMajor, _wVerMinor))
		return ResultFromScode(SELFREG_E_TYPELIB);

	if (!COleObjectFactoryEx::UpdateRegistryAll(FALSE))
		return ResultFromScode(SELFREG_E_CLASS);

	return ModuleEntry::ModuleUnregisterServer();
}

#else // !USING_MFC_ACTIVEX

/*----------------------------------------------------------------------------------------------
	Registers the DLL. This function is required by COM and must be exported from the DLL.
----------------------------------------------------------------------------------------------*/
STDAPI DLLEXPORT__ DllRegisterServer(void)
{
	ENTER_DLL();
	ModuleEntry::ModuleAddRef();
	HRESULT hr = ModuleEntry::ModuleRegisterServer();
#ifdef _MERGE_PROXYSTUB
	if (SUCCEEDED(hr) && !ModuleEntry::PerUserRegistration())
		hr = PrxDllRegisterServer();
#endif
	ModuleEntry::ModuleRelease();
	return hr;
}

/*----------------------------------------------------------------------------------------------
	Unregisters the DLL. This function is required by COM and must be exported from the DLL.
----------------------------------------------------------------------------------------------*/
STDAPI DLLEXPORT__ DllUnregisterServer(void)
{
	ENTER_DLL();
	ModuleEntry::ModuleAddRef();
	HRESULT hr = ModuleEntry::ModuleUnregisterServer();
#ifdef _MERGE_PROXYSTUB
	if (!ModuleEntry::PerUserRegistration())
	{
		if (SUCCEEDED(hr))
			hr = PrxDllRegisterServer();
		if (SUCCEEDED(hr))
			hr = PrxDllUnregisterServer();
	}
#endif
	ModuleEntry::ModuleRelease();
	return hr;
}

/*----------------------------------------------------------------------------------------------
	Adds/Removes entries to the system registry per user per machine
----------------------------------------------------------------------------------------------*/
STDAPI DLLEXPORT__ DllInstall(BOOL fInstall, LPCWSTR pszCmdLine)
{
	ENTER_DLL();
	HRESULT hr = E_FAIL;
#if WIN32 // TODO-Linux
	static const wchar_t szUserSwitch[] = _T("user");

	if (pszCmdLine != NULL)
	{
		if (_wcsnicmp(pszCmdLine, szUserSwitch, _countof(szUserSwitch)) == 0)
		{
			ModuleEntry::SetPerUserRegistration(true);
		}
	}

	if (fInstall)
	{
		hr = DllRegisterServer();
		if (FAILED(hr))
		{
			DllUnregisterServer();
		}
	}
	else
	{
		hr = DllUnregisterServer();
	}
#endif // WIN32
	return hr;
}
#endif // !USING_MFC_ACTIVEX

/*----------------------------------------------------------------------------------------------
	Tests to see if the DLL can unload. This function is required by COM and must be
		exported from the DLL.
----------------------------------------------------------------------------------------------*/
STDAPI DLLEXPORT__ DllCanUnloadNow(void)
{
	ENTER_DLL();
#ifdef _MERGE_PROXYSTUB
	HRESULT hr = PrxDllCanUnloadNow();
	if (hr != S_OK)
		return hr;
#endif
	return ModuleEntry::ModuleCanUnloadNow();
}

#endif // !EXE_MODULE

/***********************************************************************************************
	The code in this section gets included for both EXE servers and DLLs.
/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Static method to call the ProcessAttach methods of all ModuleEntry objects.
----------------------------------------------------------------------------------------------*/
HRESULT ModuleEntry::ModuleProcessAttach(void)
{
	ModuleEntry * pme;
	HRESULT hr = S_OK;

	for (pme = s_pmeFirst; pme; pme = pme->m_pobjNext)
	{
		try
		{
			pme->ProcessAttach();
		}
		catch (const Throwable & thr)
		{
			hr = WarnHr(thr.Error());
		}
		catch (...)
		{
			return WarnHr(E_FAIL);
		}
	}

	return hr;
}


/*----------------------------------------------------------------------------------------------
	Static method to call the ProcessDetach methods of all ModuleEntry objects.
----------------------------------------------------------------------------------------------*/
HRESULT ModuleEntry::ModuleProcessDetach(void)
{
	ModuleEntry * pme;
	HRESULT hr = S_OK;

	for (pme = s_pmeFirst; pme; pme = pme->m_pobjNext)
	{
		try
		{
			pme->ProcessDetach();
		}
		catch (const Throwable & thr)
		{
			hr = WarnHr(thr.Error());
		}
		catch (...)
		{
			return WarnHr(E_FAIL);
		}
	}

	return hr;
}


/*----------------------------------------------------------------------------------------------
	Static method to call the ThreadAttach methods of all ModuleEntry objects.
----------------------------------------------------------------------------------------------*/
HRESULT ModuleEntry::ModuleThreadAttach(void)
{
	ModuleEntry * pme;
	HRESULT hr = S_OK;

	for (pme = s_pmeFirst; pme; pme = pme->m_pobjNext)
	{
		try
		{
			pme->ThreadAttach();
		}
		catch (const Throwable & thr)
		{
			hr = WarnHr(thr.Error());
		}
		catch (...)
		{
			return WarnHr(E_FAIL);
		}
	}

	return hr;
}


/*----------------------------------------------------------------------------------------------
	Static method to call the ThreadDetach methods of all ModuleEntry objects.
----------------------------------------------------------------------------------------------*/
HRESULT ModuleEntry::ModuleThreadDetach(void)
{
	ModuleEntry * pme;
	HRESULT hr = S_OK;

	for (pme = s_pmeFirst; pme; pme = pme->m_pobjNext)
	{
		try
		{
			pme->ThreadDetach();
		}
		catch (const Throwable & thr)
		{
			hr = WarnHr(thr.Error());
		}
		catch (...)
		{
			return WarnHr(E_FAIL);
		}
	}

	return hr;
}


/*----------------------------------------------------------------------------------------------
	Static method to find a class factory (from the given CLSID) from all the class factories
	that are in the linked list. If the requested class factory is not found, *ppv is set
	to NULL and CLASS_E_CLASSNOTAVAILABLE is returned.
----------------------------------------------------------------------------------------------*/
HRESULT ModuleEntry::ModuleGetClassObject(REFCLSID clsid, REFIID iid, void ** ppv)
{
	AssertPtrN(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	// This block of code is largely copied from the AssertNoErrorInfo method in throwable.h.
	// Here, however, we don't assert, but just dump a warning to the output window and
	// discard the spurious error info. This prevents asserts if Windows.Forms calls
	// a class factory (as it has been known to do) with spurious error info registered.
#ifdef DEBUG
	IErrorInfo * pIErrorInfo = NULL;
	HRESULT hr = GetErrorInfo(0, &pIErrorInfo);
	Assert(SUCCEEDED(hr));

	if(pIErrorInfo != NULL) {
		BSTR bstr;
		hr = pIErrorInfo->GetDescription(&bstr);
		Assert(SUCCEEDED(hr));
		::OutputDebugString(bstr);
		::SysFreeString(bstr);
		hr = pIErrorInfo->GetSource(&bstr);
		Assert(SUCCEEDED(hr));
		::OutputDebugString(bstr);
		::SysFreeString(bstr);
		pIErrorInfo->Release();
	}
#endif

	ModuleEntry * pme;

	try
	{
		for (pme = s_pmeFirst; pme; pme = pme->m_pobjNext)
		{
			AssertPtr(pme);
			pme->GetClassFactory(clsid, iid, ppv);
			if (*ppv)
				return S_OK;
		}
	}
	catch (const Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}

	return CLASS_E_CLASSNOTAVAILABLE;
}


/*----------------------------------------------------------------------------------------------
	Static method that registers all instances of ModuleEntry.
----------------------------------------------------------------------------------------------*/
HRESULT ModuleEntry::ModuleRegisterServer()
{
	ModuleEntry * pme;
	HRESULT hrRet = S_OK;

	for (pme = s_pmeFirst; pme; pme = pme->m_pobjNext)
	{
		AssertPtr(pme);
		try
		{
			pme->RegisterServer();
		}
		catch (const Throwable & thr)
		{
			hrRet = thr.Error();
		}
		catch (...)
		{
			return WarnHr(E_FAIL);
		}
	}

	return hrRet;
}


/*----------------------------------------------------------------------------------------------
	Static method that unregisters all instances of ModuleEntry.
----------------------------------------------------------------------------------------------*/
HRESULT ModuleEntry::ModuleUnregisterServer(void)
{
	ModuleEntry * pme;
	HRESULT hrRet = S_OK;

	for (pme = s_pmeFirst; pme; pme = pme->m_pobjNext)
	{
		AssertPtr(pme);
		try
		{
			pme->UnregisterServer();
		}
		catch (const Throwable & thr)
		{
			hrRet = thr.Error();
		}
		catch (...)
		{
			return WarnHr(E_FAIL);
		}
	}

	return hrRet;
}


/*----------------------------------------------------------------------------------------------
	Static method that checks the module reference count and calls all instances of ModuleEntry
		to see if the module can be unloaded.
----------------------------------------------------------------------------------------------*/
HRESULT ModuleEntry::ModuleCanUnloadNow(void)
{
	if (s_crefModule != 0)
		return S_FALSE;

	ModuleEntry * pme;

	for (pme = s_pmeFirst; pme; pme = pme->m_pobjNext)
	{
		AssertPtr(pme);
		if (!pme->CanUnload())
			return S_FALSE;
	}

	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Static method that returns the path name of the dll or exe.
----------------------------------------------------------------------------------------------*/
LPCTSTR ModuleEntry::GetModulePathName()
{
#if WIN32
	if (s_strbpPath.Length() == 0)
	{
		ULONG cchMod;

		s_strbpPath.SetLength(MAX_PATH);
		cchMod = GetModuleFileName(s_hmod, &s_strbpPath[0], MAX_PATH);
		s_strbpPath.SetLength(cchMod);
		if (!cchMod)
			ThrowHr(WarnHr(E_FAIL));
	}

	return s_strbpPath.Chars();
#else
	if (s_strbpPath[0] == 0)
	{
		ULONG cchMod;

		cchMod = GetModuleFileName(s_hmod, &s_strbpPath[0], MAX_PATH);
		if (!cchMod)
			ThrowHr(WarnHr(E_FAIL));
	}

	return s_strbpPath;
#endif
}

#ifndef SUPPRESS_FW_EXCEPTION_HANDLING
/*----------------------------------------------------------------------------------------------
	This class and the following instance arrange to install an error handler that makes
	Throwable exceptions if an exception occurs. It only works ideally for errors in the
	main program, not in other COM components.

	Define the constant above in compiling this module if you don't want this behavior.
----------------------------------------------------------------------------------------------*/
class TransFuncInstaller : public ModuleEntry
{
	virtual void ProcessAttach(void)
	{
		_set_se_translator(TransFuncDump);
	}
};

static TransFuncInstaller s_tfi; // existence of an instance gets the above called.
#endif // !SUPPRESS_FW_EXCEPTION_HANDLING

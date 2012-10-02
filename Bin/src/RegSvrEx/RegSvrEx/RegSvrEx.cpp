// RegSvrEx.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "resource.h"

bool ShowUsage()
{
	bool bRet = false;

	HMODULE hMod = ::GetModuleHandle(NULL);

	//Load the usage text form the resource and show
	HRSRC hrsrc = ::FindResource(hMod, MAKEINTRESOURCE(IDR_USAGE), TEXT("Custom"));

	if (hrsrc)
	{
		HGLOBAL hglbl = ::LoadResource(hMod, hrsrc);

		if (hglbl)
		{
			LPVOID lpv = ::LockResource(hglbl);
			DWORD dwSize = SizeofResource(hMod, hrsrc);

			bRet = ::WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), lpv, dwSize, &dwSize, NULL) ? true : false;
		}
	}

	return bRet;
}

HRESULT GetHresultFromWin32(DWORD dw = GetLastError())
{
	return HRESULT_FROM_WIN32(dw);
}

void ShowErrorMessage(HRESULT hr)
{
	DWORD dwSize;

	CHAR szErrorMessagePrefix[64];
	int nChars = LoadStringA(NULL, IDS_ERRORMESSAGE, szErrorMessagePrefix, sizeof(szErrorMessagePrefix)/sizeof(CHAR));

	HANDLE hStdErr = GetStdHandle(STD_OUTPUT_HANDLE);

	dwSize = nChars;
	::WriteFile(hStdErr, szErrorMessagePrefix, nChars, &dwSize, NULL);

	//Write the dll name
	DWORD dwFlags = FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS;
	LPSTR szMessage = NULL;

	dwSize = ::FormatMessageA(dwFlags, NULL, hr, 0, reinterpret_cast<LPSTR>(&szMessage), 0, NULL);

	if ((szMessage == NULL) || (dwSize == 0))
	{
		wsprintfA(szErrorMessagePrefix, "%hu ", hr);
		szMessage = szErrorMessagePrefix;

		::WriteFile(hStdErr, szMessage, dwSize, &dwSize, NULL);
	}
	else
	{
		::WriteFile(hStdErr, szMessage, dwSize, &dwSize, NULL);
		LocalFree(reinterpret_cast<HLOCAL>(szMessage));
	}

}

HRESULT OverrideClassesRoot(HKEY hKeyBase, LPCWSTR szOverrideKey)
{
	HKEY hKey;
	LONG l = RegOpenKey(hKeyBase, szOverrideKey, &hKey);

	if (l == ERROR_SUCCESS)
	{
		l = RegOverridePredefKey(HKEY_CLASSES_ROOT, hKey);

		RegCloseKey(hKey);
	}

	return GetHresultFromWin32(l);
}

#pragma pack(push)
#pragma pack(1)

struct LoadLibraryCode
{
	WCHAR m_szLibPath[_MAX_PATH + 1];

	//Save registers
	BYTE m_pushEAX;
	BYTE m_pushECX;
	BYTE m_pushEDX;
	//push m_szLibPath
	BYTE m_push;
	DWORD m_dwAddrLibPath; //Address of lib path in the traget process
	//call LoadLibraryW
	BYTE m_call;
	DWORD m_dwRelAddrLoadLibraryW;
	BYTE m_popEDX;
	BYTE m_popECX;
	BYTE m_popEAX;
	BYTE m_jmp;
	DWORD_PTR m_dwRelAddr; //jump back to original address

	LoadLibraryCode(DWORD dwAddrToJump, DWORD dwRemoteAddrOfThis)
	{
		::GetModuleFileNameW(NULL, m_szLibPath, _MAX_PATH + 1);
		lstrcpyW(::PathFindFileNameW(m_szLibPath), L"RegInDll.Dll");


		m_pushEAX = 0x50; //               push        eax
		m_pushECX = 0x51; //               push        ecx
		m_pushEDX = 0x52; //               push        edx
		m_push = 0x68;
		m_dwAddrLibPath = dwRemoteAddrOfThis;
		m_call = 0xE8;	  //			   call

		DWORD dwAddrLoadLibraryW = PtrToUlong(GetProcAddress(GetModuleHandle(L"kernel32.dll"), "LoadLibraryW"));
		m_dwRelAddrLoadLibraryW = dwAddrLoadLibraryW - (dwRemoteAddrOfThis + ((BYTE*)&m_dwRelAddrLoadLibraryW - (BYTE*)this) + sizeof(DWORD));

		m_popEDX = 0x5A;  //               pop         edx
		m_popECX = 0x59;  //               pop         ecx
		m_popEAX = 0x58;  //               pop         eax
		m_jmp = 0xE9;     //			   jmp
		m_dwRelAddr = dwAddrToJump - (dwRemoteAddrOfThis + sizeof(LoadLibraryCode)) ;
	}

	DWORD GetRemoteCodeAddr(DWORD dwRemoteAddr)
	{
		return dwRemoteAddr + ((BYTE*)&m_pushEAX - (BYTE*)this);
	}
};

#pragma pack(pop)

HRESULT RegisterExe(LPCWSTR szExe, bool bUnregister, bool bCurrentUser)
{
	LPWSTR szCmdLine =  bUnregister ? L"/UnregServer" : L"/RegServer";

	STARTUPINFO si = {0};
	si.cb = sizeof(STARTUPINFO);
	si.wShowWindow = SW_SHOWDEFAULT;

	PROCESS_INFORMATION pi = {0};

	HRESULT hr = S_OK;

	BOOL b = CreateProcess(szExe, szCmdLine, NULL, NULL, FALSE, CREATE_SUSPENDED , NULL, NULL, &si, &pi);

	if (b)
	{
		//If the registration is required for the current user inject code to override the keys
		if (bCurrentUser)
		{
			CONTEXT ctxt = {0};
			ctxt.ContextFlags = CONTEXT_FULL;
			GetThreadContext(pi.hThread, &ctxt);

			LPVOID pvDestAddr = VirtualAllocEx(pi.hProcess, NULL, sizeof(LoadLibraryCode), MEM_COMMIT, PAGE_EXECUTE_READWRITE);

			DWORD dwDestAddr = PtrToUlong(pvDestAddr);
			LoadLibraryCode code(ctxt.Eip, dwDestAddr);

			WriteProcessMemory(pi.hProcess, pvDestAddr, &code, sizeof(LoadLibraryCode), NULL);

			ctxt.Eip = code.GetRemoteCodeAddr(dwDestAddr);
			SetThreadContext(pi.hThread, &ctxt);
		}

		ResumeThread(pi.hThread);
		CloseHandle(pi.hThread);

		WaitForSingleObject(pi.hProcess, INFINITE);

		DWORD dwResult = 0;
		GetExitCodeProcess(pi.hProcess, &dwResult);

		hr = GetHresultFromWin32(dwResult);

		CloseHandle(pi.hProcess);
	}
	else
		hr = GetHresultFromWin32();

	return hr;
}

HRESULT RegisterDll(LPCWSTR szDll, bool bUnregister, bool bCurrentUser)
{
	LPCSTR szFunction =  bUnregister ? "DllUnregisterServer" : "DllRegisterServer";
	HRESULT hr = S_OK;

	HMODULE hMod = LoadLibrary(szDll);

	if (hMod != NULL)
	{
		typedef HRESULT (_stdcall *DLLPROC)();
		DLLPROC pfnDllProc = reinterpret_cast<DLLPROC>(GetProcAddress(hMod, szFunction));

		if (pfnDllProc)
		{
			if (bCurrentUser)
			{
				//Override HKEY_CLASSES_ROOT
				hr = OverrideClassesRoot(HKEY_CURRENT_USER, L"Software\\Classes");
			}

			if (SUCCEEDED(hr))
				hr = (*pfnDllProc)();
		}
		else
			hr = GetHresultFromWin32();

		FreeLibrary(hMod);
	}
	else
		hr = GetHresultFromWin32();

	return hr;
}

int wmain(int argc, WCHAR* argv[])
{
	if ((argc == 1) || ((argc == 2) && (lstrcmpi(argv[1], L"/?") == 0)))
	{
		ShowUsage();
		return E_INVALIDARG;
	}

	int i = argc - 1;

	//File name is always the last option
	LPCWSTR szFilePath = argv[i];

	//Find whether the file is a .exe or a .dll
	LPCWSTR szExtension = PathFindExtension(szFilePath);

	bool bExe = false;
	bool bCurrentUser = false;
	bool bUnregister = false;

	if (lstrcmpi(szExtension, TEXT(".exe")) == 0)
		bExe = true;
	//else default to dll

	//Parse the command line to find the other options
	i--;

	while(i > 0)
	{
		if (lstrcmpi(argv[i], L"/u") == 0)
			bUnregister = true;
		else if (lstrcmpi(argv[i], L"/c") == 0)
			bCurrentUser = true;
//		else
//		{
//			//Ignore the option
//		}

		i--;
	}

	HRESULT hr;

	if (bExe)
	{
		hr = RegisterExe(szFilePath, bUnregister, bCurrentUser);
	}
	else
	{
		hr = RegisterDll(szFilePath, bUnregister, bCurrentUser);
	}

	if (FAILED(hr))
		ShowErrorMessage(hr);

	return hr;
}

#ifndef DEBUG

extern "C" int mainCRTStartup()
{
	int argc = 0;
	WCHAR* *argv;

	argv = CommandLineToArgvW(GetCommandLine(), &argc);

	return wmain(argc, argv);
}

#endif

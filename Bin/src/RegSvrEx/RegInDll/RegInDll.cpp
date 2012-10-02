// RegInDll.cpp : Defines the entry point for the DLL application.
//

#include "stdafx.h"

LONG OverrideClassesRoot(HKEY hKeyBase, LPCWSTR szOverrideKey)
{
	HKEY hKey;
	LONG l = RegOpenKey(hKeyBase, szOverrideKey, &hKey);

	if (l == ERROR_SUCCESS)
	{
		l = RegOverridePredefKey(HKEY_CLASSES_ROOT, hKey);

		RegCloseKey(hKey);
	}

	return l;
}

BOOL APIENTRY DllMain( HMODULE hModule,
					   DWORD  ul_reason_for_call,
					   LPVOID lpReserved
					 )
{
	if (ul_reason_for_call == DLL_PROCESS_ATTACH)
	{
		DisableThreadLibraryCalls(hModule);

		//Failed to override the key for some reason so just terminate the process
		LONG l = OverrideClassesRoot(HKEY_CURRENT_USER, L"Software\\Classes");

		if (l != ERROR_SUCCESS)
			ExitProcess(l);
	}

	return TRUE;
}

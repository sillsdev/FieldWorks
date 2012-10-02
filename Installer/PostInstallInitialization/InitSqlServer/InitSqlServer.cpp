// Utility to initialize FieldWorks access to SQL Server.
// This used to be done by FieldWorks applications, but is now done at the end
// of the installation sequence, while we still have administrator privileges.

#include <windows.h>

// Application entry point
int APIENTRY WinMain(HINSTANCE hInstance, HINSTANCE /*hPrevInstance*/,
					 LPSTR /*lpCmdLine*/, int /*nCmdShow*/)
{
	const wchar_t * pszDbAccessDll = L"DbAccess.dll";
	LONG lResult;
	HKEY hKey;

	// Look up the FieldWorks code directory in the registry:
	lResult = RegOpenKeyEx(HKEY_LOCAL_MACHINE, L"SOFTWARE\\SIL\\FieldWorks", 0, KEY_READ, &hKey);
	if (ERROR_SUCCESS != lResult)
		return -1;

	DWORD cbData = 0;

	// Fetch required buffer size:
	lResult = RegQueryValueEx(hKey, L"RootCodeDir", NULL, NULL, NULL, &cbData);
	if (cbData == 0)
		return -2;

	int cchDbAccessDllPath = cbData + 1 + wcslen(pszDbAccessDll);
	wchar_t * pszDbAccessDllPath = new wchar_t [cchDbAccessDllPath];

	lResult = RegQueryValueEx(hKey, L"RootCodeDir", NULL, NULL, LPBYTE(pszDbAccessDllPath), &cbData);
	if (ERROR_SUCCESS != lResult)
		return -3;

	// Form full path to DbAccess.dll:
	if (pszDbAccessDllPath[cbData - 1] != '\\')
		wcscat_s(pszDbAccessDllPath, cchDbAccessDllPath, L"\\");
	wcscat_s(pszDbAccessDllPath, cchDbAccessDllPath, pszDbAccessDll);

	// Load the DbAccess.dll:
	HMODULE hmodDbAccess = LoadLibrary(pszDbAccessDllPath);
	if (!hmodDbAccess)
		return -4;

	// Get pointer to ExtInitMSDE function:
	typedef void (WINAPI * ExtInitMSDEFn)(HWND, HINSTANCE, LPSTR, int);
	ExtInitMSDEFn _ExtInitMSDE;
	_ExtInitMSDE = (ExtInitMSDEFn)GetProcAddress(hmodDbAccess, "ExtInitMSDE");

	if (!_ExtInitMSDE)
	{
		FreeLibrary(hmodDbAccess);
		return -5;
	}

	_ExtInitMSDE(NULL, hInstance, "force", SW_SHOW);

	FreeLibrary(hmodDbAccess);
	hmodDbAccess = NULL;

	return 0;
}

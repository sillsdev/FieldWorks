#include <windows.h>
#include <stdio.h>
#include <msi.h>
#include <msiquery.h>
#include <tchar.h>


// Returns true if the given file exists in the given directory.
bool FileExists(_TCHAR * pszDirectory, rsize_t cbDirectory, _TCHAR * pszFileName)
{
	bool fReturn = false;
	const int kcchBufLen = 2048;
	_TCHAR pszPath[kcchBufLen];
	_tcscpy_s(pszPath, cbDirectory, pszDirectory);
	_tcscat_s(pszPath, cbDirectory, _T("\\"));
	_tcscat_s(pszPath, cbDirectory, pszFileName);
	FILE * f;
	if (_tfopen_s(&f, pszPath, _T("r")) == 0)
	{
		fReturn = true;
		fclose(f);
	}
	return fReturn;
}

// Uses the installer's INSTALLDIR variable to search for some of the .exe files used by
// FieldWorks. In some rare cases, a user's machine may have lost the record of FW being
// installed, while retaining some FW files. This situation leads to an error during
// installation.
// However, if the internal variable UPGRADE_ANY is set to "1", this means an older version
// of FW has been detected which can ve upgraded, therefore we expect to find the .exe files,
// and it is not a problem. So we don't make a fuss in that case.
extern "C" __declspec(dllexport) UINT CheckForFwResidueFiles(MSIHANDLE hInstall)
{
	const int kcchStringBufLen = 2048;
	_TCHAR pszInstallDir[kcchStringBufLen] = { 0 };
	_TCHAR pszUpgradeAny[kcchStringBufLen] = { 0 };

	try
	{
		// Get the MSI UPGRADE_ANY variable:
		DWORD cch = kcchStringBufLen;
		MsiGetProperty(hInstall, _T("UPGRADE_ANY"), pszUpgradeAny, &cch);

		// If the variable is set to "1", we don't need to bother any more with this test:
		if (_tcscmp(pszUpgradeAny, _T("1")) == 0)
			return ERROR_SUCCESS;

		// Get the MSI INSTALLDIR variable:
		cch = kcchStringBufLen;
		MsiGetProperty(hInstall, _T("INSTALLDIR"), pszInstallDir, &cch);

#ifdef _DEBUG
		MessageBox(NULL, pszInstallDir, _T("INSTALLDIR"), 0);
#endif
		if (FileExists(pszInstallDir, kcchStringBufLen, _T("Flex.exe"))
			|| FileExists(pszInstallDir, kcchStringBufLen, _T("TE.exe"))
			|| FileExists(pszInstallDir, kcchStringBufLen, _T("FwListEditor.exe"))
			|| FileExists(pszInstallDir, kcchStringBufLen, _T("FwNotebook.exe"))
			|| FileExists(pszInstallDir, kcchStringBufLen, _T("InstallLanguage.exe"))
			|| FileExists(pszInstallDir, kcchStringBufLen, _T("WorldPad.exe")))
		{
			MsiSetProperty(hInstall, _T("FW_RESIDUE_FILES_FOUND"), _T("1"));
		}
	}
	catch (...)
	{
		MsiSetProperty(hInstall, _T("FW_RESIDUE_FILES_FOUND"), _T("-1"));
		return ERROR_SUCCESS;
	}

	return ERROR_SUCCESS;
}

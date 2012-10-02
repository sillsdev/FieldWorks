#include <windows.h>
#include <stdio.h>
#include <msi.h>
#include <msiquery.h>
#include <tchar.h>


// Compares FW build date with value in FW CoreInstallation value in registry.
// If there is a discrepancy, a report is constructed, telling the user which
// installed features will not work if the installation continues.
// The following MSI properties contain the relevant data:
// BUILD_DATE : The date of the FW core inside the installer
// FEATURE_LIST : a comma-seperated list of features in the installer.
// CORE_FEATURES_REPORT : To be defined with the generated report, if needed.
// CORE_FEATURES_RETURN : Set to 0 if there is no problem, 1 if there is a clash
// of installations, and 2 if this function failed for some reason.
extern "C" __declspec(dllexport) UINT CheckCoreFeatures(MSIHANDLE hInstall)
{
	const int kcchStringBufLen = 2048;
	_TCHAR pszBuildDate[kcchStringBufLen] = { 0 };
	_TCHAR pszFeatureList[kcchStringBufLen] = { 0 };
	_TCHAR pszReport[kcchStringBufLen] = { 0 };

	try
	{
		// Get the MSI variables:
		DWORD cch = kcchStringBufLen;
		MsiGetProperty(hInstall, _T("BUILD_DATE"), pszBuildDate, &cch);
		cch = kcchStringBufLen;
		MsiGetProperty(hInstall, _T("FEATURE_LIST"), pszFeatureList, &cch);

#ifdef _DEBUG
		MessageBox(NULL, pszFeatureList, pszBuildDate, 0);
#endif

		// Check for previous records:
		HKEY hKey;
		LONG lResult = RegOpenKeyEx(HKEY_LOCAL_MACHINE,
			_T("Software\\SIL\\FieldWorks\\CoreInstallation"), NULL, KEY_READ, &hKey);

		if (ERROR_SUCCESS != lResult)
		{
			// We will assume that the key couldn't be read because it doesn't exist.
			// Therefore, there can be no clash:
			return ERROR_SUCCESS;
		}

		// Get the date of the intalled core files:
		const int cchDateString = 50; // Max size for date string
		_TCHAR szDateString[cchDateString];
		DWORD cbData = cchDateString;
		lResult = RegQueryValueEx(hKey, NULL, NULL, NULL, (LPBYTE)szDateString, &cbData);

		if (ERROR_SUCCESS != lResult)
		{
			_tcscat_s(pszReport, kcchStringBufLen, _T("Error - could not retrieve date of previous Core build.\n"));
			throw 1;
		}

		// See if the date value differs from the given value:
		int nCmpResult = _tcscmp(szDateString, pszBuildDate);
		if (nCmpResult == 0)
		{
			// Dates are the same:
			return ERROR_SUCCESS;
		}
		if (nCmpResult < 0)
		{
			// Installed features are older than current installer's version:
			_tcscat_s(pszReport, kcchStringBufLen, _T("This installer will install core FieldWorks files that are newer than ones you already have. If you continue, you will almost certainly render the following applications unusable:\n"));

			// Examine listed applications in the registry, and report any that are not in the
			// given feature list.
			// Get the lengths of the longest value name and longest product key:
			DWORD ctchLongestName;
			DWORD ctbLongestData;
			lResult = RegQueryInfoKey(hKey, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
				&ctchLongestName, &ctbLongestData, NULL, NULL);

			if (lResult != ERROR_SUCCESS)
			{
				_tcscat_s(pszReport, kcchStringBufLen, _T("Error - cannot retrieve installed features list.\n"));
				throw 2;
			}

			// Prepare to enumerate values, to see if we can find a match:
			_TCHAR * pszFeatureName = new _TCHAR [ctbLongestData + 1]; // Allow terminating 0
			DWORD cbData = ctbLongestData + 1;
			_TCHAR * pszFeature = new _TCHAR [ctchLongestName + 1]; // Allow terminating 0
			DWORD cbValue = ctchLongestName + 1;
			int iValue = 0;
			bool fFoundAny = false;
			while (ERROR_SUCCESS == RegEnumValue(hKey, iValue++, pszFeature, &cbValue,
				NULL, NULL, (LPBYTE)pszFeatureName, &cbData))
			{
				// See if found feature appears in our list to ignore:
				if (_tcsstr(pszFeatureList, pszFeature) != NULL && pszFeature[0] != 0)
				{
					// Report feature's full name:
					_tcscat_s(pszReport, kcchStringBufLen, pszFeatureName);
					_tcscat_s(pszReport, kcchStringBufLen, _T("\n"));
					fFoundAny = true;
				}
				cbValue = ctchLongestName + 1;
				cbData = ctbLongestData + 1;
			}
			delete[] pszFeatureName;
			delete[] pszFeature;

			if (fFoundAny)
				_tcscat_s(pszReport, kcchStringBufLen, _T("\nIt is advised that you first cancel this installation, then uninstall the named applications, then restart this installation.\n"));
			else
				pszReport[0] = 0;
		}
		else
		{
			// Installed features are newer than current installer's version:
			_tcscat_s(pszReport, kcchStringBufLen, _T("This installer contains core FieldWorks files that are older than those that already exist on your computer. If you continue with this installation, the existing files will not be overwritten. Thus, you will not get a complete installation of applications, and you almost certainly will not be able to use them.\n\nTo achieve a complete installation, you must first cancel this installation, then uninstall the newer applications, then restart this installation."));
		}
		if (pszReport[0] != 0)
		{
			MsiSetProperty(hInstall, _T("CORE_FEATURES_REPORT"), pszReport);
			MsiSetProperty(hInstall, _T("CORE_FEATURES_RETURN"), _T("1"));
			return ERROR_SUCCESS;
		}
	}
	catch (...)
	{
		_tcscat_s(pszReport, kcchStringBufLen, _T("Core Features Test failed. Unable to determine if there is a risk of overwriting existing FieldWorks core files."));
		MsiSetProperty(hInstall, _T("CORE_FEATURES_REPORT"), pszReport);
		MsiSetProperty(hInstall, _T("CORE_FEATURES_RETURN"), _T("2"));
		return ERROR_SUCCESS;
	}
	return ERROR_SUCCESS;
}

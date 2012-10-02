//-------------------------------------------------------------------------------------------------
// <copyright file="ngenca.cpp" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
//    NetFx custom action code.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

#define NGEN_DEBUG   0x0001
#define NGEN_NODEP  0x0002
#define NGEN_PROFILE 0x0004
#define NGEN_32BIT  0x0008
#define NGEN_64BIT  0x0010

#define NGEN_TIMEOUT 60000 // 60 seconds

LPCWSTR vcsNgenQuery =
	L"SELECT `NetFxNativeImage`.`File_`, `NetFxNativeImage`.`NetFxNativeImage`, `NetFxNativeImage`.`Priority`, `NetFxNativeImage`.`Attributes`, `NetFxNativeImage`.`File_Application`, `NetFxNativeImage`.`Directory_ApplicationBase`, `File`.`Component_` "
	L"FROM `NetFxNativeImage`, `File` WHERE `File`.`File`=`NetFxNativeImage`.`File_`";
enum eNgenQuery { ngqFile = 1, ngqId, ngqPriority, ngqAttributes, ngqFileApp, ngqDirAppBase, ngqComponent };

LPCWSTR vcsNgenGac =
	L"SELECT `MsiAssembly`.`File_Manifest` "
	L"FROM `File`, `MsiAssembly` WHERE `File`.`Component_`=`MsiAssembly`.`Component_` AND `File`.`File`=?";
enum eNgenGac { nggManifest = 1 };

LPCWSTR vcsNgenStrongName =
	L"SELECT `Name`,`Value` FROM `MsiAssemblyName` WHERE `Component_`=?";
enum eNgenStrongName { ngsnName = 1, ngsnValue };

// Gets the path to ngen.exe
static HRESULT GetNgenPath(
	__out LPWSTR* ppwzNgenPath,
	__in BOOL f64BitFramework
	)
{
	Assert(ppwzNgenPath);
	HRESULT hr = S_OK;

	LPWSTR pwzVersion = NULL;
	LPWSTR pwzWindowsFolder = NULL;

	hr = WcaGetProperty(L"NetFxVersion", &pwzVersion);
	ExitOnFailure(hr, "failed to get NetFxVersion property");

	if (!*pwzVersion)
		ExitOnFailure(hr = E_INVALIDARG, "The NetFxVersion property must be set in order to use the Ngen custom actions.");

	hr = WcaGetProperty(L"WindowsFolder", &pwzWindowsFolder);
	ExitOnFailure(hr, "failed to get WindowsFolder property");

	hr = StrAllocString(ppwzNgenPath, pwzWindowsFolder, 0);
	ExitOnFailure1(hr, "failed to copy to NgenPath windows folder: %S", pwzWindowsFolder);

	if (f64BitFramework)
	{
		hr = StrAllocConcat(ppwzNgenPath, L"Microsoft.NET\\Framework64\\", 0);
		ExitOnFailure(hr, "failed to copy platform portion of ngen path");
	}
	else
	{
		hr = StrAllocConcat(ppwzNgenPath, L"Microsoft.NET\\Framework\\", 0);
		ExitOnFailure(hr, "failed to copy platform portion of ngen path");
	}

	hr = StrAllocConcat(ppwzNgenPath, pwzVersion, 0);
	ExitOnFailure(hr, "failed to copy platform portion of ngen path");

	hr = StrAllocConcat(ppwzNgenPath, L"\\ngen.exe", 0);
	ExitOnFailure(hr, "failed to copy platform portion of ngen path");

LExit:
	ReleaseStr(pwzVersion);
	ReleaseStr(pwzWindowsFolder);

	return hr;
}


static HRESULT GetStrongName(
	__out LPWSTR* ppwzStrongName,
	__in LPCWSTR pwzComponent
	)
{
	Assert(ppwzStrongName);
	HRESULT hr = S_OK;

	PMSIHANDLE hView = NULL;
	PMSIHANDLE hComponentRec = NULL;
	PMSIHANDLE hRec = NULL;

	LPWSTR pwzData = NULL;
	LPWSTR pwzName = NULL;
	LPWSTR pwzVersion = NULL;
	LPWSTR pwzCulture = NULL;
	LPWSTR pwzPublicKeyToken = NULL;

	hComponentRec = ::MsiCreateRecord(1);
	hr = WcaSetRecordString(hComponentRec, 1, pwzComponent);
	ExitOnFailure1(hr, "failed to set component value in record to: %S", pwzComponent);

	// get the name value records for this component
	hr = WcaOpenView(vcsNgenStrongName, &hView);
	ExitOnFailure(hr, "failed to open view on NetFxNativeImage table");

	hr = WcaExecuteView(hView, hComponentRec);
	ExitOnFailure(hr, "failed to execute strong name view");

	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		hr = WcaGetRecordString(hRec, ngsnName, &pwzData);
		ExitOnFailure1(hr, "failed to get MsiAssemblyName.Name for component: %S", pwzComponent);

		if (0 == lstrcmpW(L"name", pwzData))
		{
			hr = WcaGetRecordString(hRec, ngsnValue, &pwzName);
			ExitOnFailure2(hr, "failed to get MsiAssemblyName.Value for component: %S Name: %S", pwzComponent, pwzData);
		}
		else if (0 == lstrcmpW(L"version", pwzData))
		{
			hr = WcaGetRecordString(hRec, ngsnValue, &pwzVersion);
			ExitOnFailure2(hr, "failed to get MsiAssemblyName.Value for component: %S Name: %S", pwzComponent, pwzData);
		}
		else if (0 == lstrcmpW(L"culture", pwzData))
		{
			hr = WcaGetRecordString(hRec, ngsnValue, &pwzCulture);
			ExitOnFailure2(hr, "failed to get MsiAssemblyName.Value for component: %S Name: %S", pwzComponent, pwzData);
		}
		else if (0 == lstrcmpW(L"publicKeyToken", pwzData))
		{
			hr = WcaGetRecordString(hRec, ngsnValue, &pwzPublicKeyToken);
			ExitOnFailure2(hr, "failed to get MsiAssemblyName.Value for component: %S Name: %S", pwzComponent, pwzData);
		}
	}
	if (E_NOMOREITEMS == hr)
		hr = S_OK;
	ExitOnFailure1(hr, "failed while looping through all names and values in MsiAssemblyName table for component: %S", pwzComponent);

	hr = StrAllocFormatted(ppwzStrongName, L"\"%s, Version=%s, Culture=%s, PublicKeyToken=%s\"", pwzName, pwzVersion, pwzCulture, pwzPublicKeyToken);
	ExitOnFailure1(hr, "failed to format strong name for component: %S", pwzComponent);

LExit:
	ReleaseStr(pwzData);
	ReleaseStr(pwzName);
	ReleaseStr(pwzVersion);
	ReleaseStr(pwzCulture);
	ReleaseStr(pwzPublicKeyToken);

	return hr;
}

static HRESULT CreateInstallCommand(
	__out LPWSTR* ppwzCommandLine,
	__in LPCWSTR pwzNgenPath,
	__in LPCWSTR pwzFile,
	__in int iPriority,
	__in int iAttributes,
	__in LPCWSTR pwzFileApp,
	__in LPCWSTR pwzDirAppBase
	)
{
	Assert(ppwzCommandLine && pwzNgenPath && *pwzNgenPath && pwzFile && *pwzFile&& pwzFileApp && pwzDirAppBase);
	HRESULT hr = S_OK;

	LPWSTR pwzQueueString = NULL;

	hr = StrAllocFormatted(ppwzCommandLine, L"%s install %s", pwzNgenPath, pwzFile);
	ExitOnFailure(hr, "failed to assemble install command line");

	if (iPriority > 0)
	{
		hr = StrAllocFormatted(&pwzQueueString, L" /queue:%d", iPriority);
		ExitOnFailure(hr, "failed to format queue string");

		hr = StrAllocConcat(ppwzCommandLine, pwzQueueString, 0);
		ExitOnFailure(hr, "failed to add queue string to NGEN command line");
	}

	if (NGEN_DEBUG & iAttributes)
	{
		hr = StrAllocConcat(ppwzCommandLine, L" /Debug", 0);
		ExitOnFailure(hr, "failed to add debug to NGEN command line");
	}

	if (NGEN_PROFILE & iAttributes)
	{
		hr = StrAllocConcat(ppwzCommandLine, L" /Profile", 0);
		ExitOnFailure(hr, "failed to add profile to NGEN command line");
	}

	if (NGEN_NODEP & iAttributes)
	{
		hr = StrAllocConcat(ppwzCommandLine, L" /NoDependencies", 0);
		ExitOnFailure(hr, "failed to add no dependencies to NGEN command line");
	}

	// If it's more than just two quotes around an empty string
	if (2 > lstrlenW(pwzFileApp))
	{
		hr = StrAllocConcat(ppwzCommandLine, L" /ExeConfig:", 0);
		ExitOnFailure(hr, "failed to add exe config to NGEN command line");

		hr = StrAllocConcat(ppwzCommandLine, pwzFileApp, 0);
		ExitOnFailure(hr, "failed to add file app to NGEN command line");
	}

	if (2 > lstrlenW(pwzDirAppBase))
	{
		hr = StrAllocConcat(ppwzCommandLine, L" /AppBase:", 0);
		ExitOnFailure(hr, "failed to add app base to NGEN command line");

		hr = StrAllocConcat(ppwzCommandLine, pwzDirAppBase, 0);
		ExitOnFailure(hr, "failed to add dir app base to NGEN command line");
	}

LExit:
	return hr;
}

/******************************************************************
 SchedNetFx - entry point for NetFx Custom Action

********************************************************************/
extern "C" UINT __stdcall SchedNetFx(
	__in MSIHANDLE hInstall
	)
{
	// AssertSz(FALSE, "debug SchedNetFx");

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	LPWSTR pwzInstallCustomActionData = NULL;
	LPWSTR pwzUninstallCustomActionData = NULL;
	UINT uiCost = 0;

	PMSIHANDLE hView = NULL;
	PMSIHANDLE hRec = NULL;
	PMSIHANDLE hViewGac = NULL;
	PMSIHANDLE hRecGac = NULL;

	LPWSTR pwzId = NULL;
	LPWSTR pwzData = NULL;
	LPWSTR pwzTemp = NULL;
	LPWSTR pwzFile = NULL;
	int iPriority = 0;
	int iAssemblyCost = 0;
	int iAttributes = 0;
	LPWSTR pwzFileApp = NULL;
	LPWSTR pwzDirAppBase = NULL;
	LPWSTR pwzComponent = NULL;

	INSTALLSTATE isInstalled;
	INSTALLSTATE isAction;

	LPWSTR pwz32Ngen = NULL;
	LPWSTR pwz64Ngen = NULL;

	BOOL fNeedInstallUpdate32 = FALSE;
	BOOL fNeedUninstallUpdate32 = FALSE;
	BOOL fNeedInstallUpdate64 = FALSE;
	BOOL fNeedUninstallUpdate64 = FALSE;

	// initialize
	hr = WcaInitialize(hInstall, "SchedNetFx");
	ExitOnFailure(hr, "failed to initialize");

	hr = GetNgenPath(&pwz32Ngen, FALSE);
	ExitOnFailure(hr, "failed to get 32bit ngen.exe path");

	hr = GetNgenPath(&pwz64Ngen, TRUE);
	ExitOnFailure(hr, "failed to get 64bit ngen.exe path");

	// loop through all the NetFx records
	hr = WcaOpenExecuteView(vcsNgenQuery, &hView);
	ExitOnFailure(hr, "failed to open view on NetFxNativeImage table");

	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		// Get Id
		hr = WcaGetRecordString(hRec, ngqId, &pwzId);
		ExitOnFailure(hr, "failed to get NetFxNativeImage.NetFxNativeImage");

		// Get File
		hr = WcaGetRecordString(hRec, ngqFile, &pwzData);
		ExitOnFailure1(hr, "failed to get NetFxNativeImage.File_ for record: %S", pwzId);
		hr = StrAllocFormatted(&pwzTemp, L"\"[#%s]\"",pwzData);
		ExitOnFailure1(hr, "failed to format file string for file: %S", pwzData);
		hr = WcaGetFormattedString(pwzTemp, &pwzFile);
		ExitOnFailure1(hr, "failed to get formatted string for file: %S", pwzData);

		// Get Priority
		hr = WcaGetRecordInteger(hRec, ngqPriority, &iPriority);
		ExitOnFailure1(hr, "failed to get NetFxNativeImage.Priority for record: %S", pwzId);

		if (0 == iPriority)
			iAssemblyCost = COST_NGEN_BLOCKING;
		else
			iAssemblyCost = COST_NGEN_NONBLOCKING;

		// Get Attributes
		hr = WcaGetRecordInteger(hRec, ngqAttributes, &iAttributes);
		ExitOnFailure1(hr, "failed to get NetFxNativeImage.Attributes for record: %S", pwzId);

		// Get File_Application
		hr = WcaGetRecordString(hRec, ngqFileApp, &pwzData);
		ExitOnFailure1(hr, "failed to get NetFxNativeImage.File_Application for record: %S", pwzId);
		hr = StrAllocFormatted(&pwzTemp, L"\"[#%s]\"",pwzData);
		ExitOnFailure1(hr, "failed to format file application string for file: %S", pwzData);
		hr = WcaGetFormattedString(pwzTemp, &pwzFileApp);
		ExitOnFailure1(hr, "failed to get formatted string for file application: %S", pwzData);

		// Get Directory_ApplicationBase
		hr = WcaGetRecordString(hRec, ngqDirAppBase, &pwzData);
		ExitOnFailure1(hr, "failed to get NetFxNativeImage.Directory_ApplicationBase for record: %S", pwzId);
		hr = StrAllocFormatted(&pwzTemp, L"\"[%s]\"",pwzData);
		ExitOnFailure1(hr, "failed to format directory application base string for file: %S", pwzData);
		hr = WcaGetFormattedString(pwzTemp, &pwzDirAppBase);
		ExitOnFailure1(hr, "failed to get formatted string for directory application base: %S", pwzData);

		// Get Component
		hr = WcaGetRecordString(hRec, ngqComponent, &pwzComponent);
		ExitOnFailure1(hr, "failed to get NetFxNativeImage.Directory_ApplicationBase for record: %S", pwzId);
		er = ::MsiGetComponentStateW(hInstall, pwzComponent, &isInstalled, &isAction);
		ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "failed to get install state for Component: %S", pwzComponent);

		//
		// Figure out if it's going to be GAC'd.  The possibility exists that no assemblies are going to be GAC'd
		// so we have to check for the MsiAssembly table first.
		//
		if (S_OK == WcaTableExists(L"MsiAssembly"))
		{
			hr = WcaOpenView(vcsNgenGac, &hViewGac);
			ExitOnFailure(hr, "failed to open view on File/MsiAssembly tables");

			hr = WcaExecuteView(hViewGac, hRec);
			ExitOnFailure(hr, "failed to execute view on File/MsiAssembly tables");

			hr = WcaFetchSingleRecord(hViewGac, &hRecGac);
			ExitOnFailure(hr, "failed to fetch Component for secure object");

			if (S_FALSE != hr)
			{
				hr = WcaGetRecordString(hRecGac, nggManifest, &pwzData);
				ExitOnFailure(hr, "failed to get MsiAssembly.File_Manifest");

				// If it's in the GAC replace the file name with the strong name
				if (L'\0' == pwzData[0])
				{
					hr = GetStrongName(&pwzFile, pwzComponent);
					ExitOnFailure1(hr, "failed to get strong name for component: %S", pwzData);
				}
			}
		}

		//
		// Schedule the work
		//
		if (!(iAttributes & NGEN_32BIT) && !(iAttributes & NGEN_64BIT))
			ExitOnFailure1(hr = E_INVALIDARG, "Neither 32bit nor 64bit is specified for NGEN of file: %S", pwzFile);

		if (WcaIsInstalling(isInstalled, isAction) || WcaIsReInstalling(isInstalled, isAction))
		{
			if (iAttributes & NGEN_32BIT)
			{
				// Assemble the install command line
				hr = CreateInstallCommand(&pwzData, pwz32Ngen, pwzFile, iPriority, iAttributes, pwzFileApp, pwzDirAppBase);
				ExitOnFailure(hr, "failed to create install command line");

				hr = WcaWriteStringToCaData(pwzData, &pwzInstallCustomActionData);
				ExitOnFailure1(hr, "failed to add install command to custom action data: %S", pwzData);

				hr = WcaWriteIntegerToCaData(iAssemblyCost, &pwzInstallCustomActionData);
				ExitOnFailure1(hr, "failed to add cost to custom action data: %S", pwzData);

				uiCost += iAssemblyCost;

				fNeedInstallUpdate32 = TRUE;
			}

			if (iAttributes & NGEN_64BIT)
			{
				// Assemble the install command line
				hr = CreateInstallCommand(&pwzData, pwz64Ngen, pwzFile, iPriority, iAttributes, pwzFileApp, pwzDirAppBase);
				ExitOnFailure(hr, "failed to create install command line");

				hr = WcaWriteStringToCaData(pwzData, &pwzInstallCustomActionData); // command
				ExitOnFailure1(hr, "failed to add install command to custom action data: %S", pwzData);

				hr = WcaWriteIntegerToCaData(iAssemblyCost, &pwzInstallCustomActionData); // cost
				ExitOnFailure1(hr, "failed to add cost to custom action data: %S", pwzData);

				uiCost += iAssemblyCost;

				fNeedInstallUpdate64 = TRUE;
			}
		}
		else if (WcaIsUninstalling(isInstalled, isAction))
		{
			if (iAttributes & NGEN_32BIT)
			{
				hr = StrAllocFormatted(&pwzData, L"%s uninstall %s", pwz32Ngen, pwzFile);
				ExitOnFailure(hr, "failed to create update 32 command line");

				hr = WcaWriteStringToCaData(pwzData, &pwzUninstallCustomActionData); // command
				ExitOnFailure1(hr, "failed to add install command to custom action data: %S", pwzData);

				hr = WcaWriteIntegerToCaData(COST_NGEN_NONBLOCKING, &pwzUninstallCustomActionData); // cost
				ExitOnFailure1(hr, "failed to add cost to custom action data: %S", pwzData);

				uiCost += COST_NGEN_NONBLOCKING;

				fNeedUninstallUpdate32 = TRUE;
			}

			if (iAttributes & NGEN_64BIT)
			{
				hr = StrAllocFormatted(&pwzData, L"%s uninstall %s", pwz64Ngen, pwzFile);
				ExitOnFailure(hr, "failed to create update 64 command line");

				hr = WcaWriteStringToCaData(pwzData, &pwzUninstallCustomActionData); // command
				ExitOnFailure1(hr, "failed to add install command to custom action data: %S", pwzData);

				hr = WcaWriteIntegerToCaData(COST_NGEN_NONBLOCKING, &pwzUninstallCustomActionData); // cost
				ExitOnFailure1(hr, "failed to add cost to custom action data: %S", pwzData);

				uiCost += COST_NGEN_NONBLOCKING;

				fNeedUninstallUpdate64 = TRUE;
			}
		}
	}
	if (E_NOMOREITEMS == hr)
		hr = S_OK;
	ExitOnFailure(hr, "failed while looping through all objects to secure");

	// If we need 32 bit install update
	if (fNeedInstallUpdate32)
	{
		hr = StrAllocFormatted(&pwzData, L"%s update /queue", pwz32Ngen);
		ExitOnFailure(hr, "failed to create install update 32 command line");

		hr = WcaWriteStringToCaData(pwzData, &pwzInstallCustomActionData); // command
		ExitOnFailure1(hr, "failed to add install command to install custom action data: %S", pwzData);

		hr = WcaWriteIntegerToCaData(COST_NGEN_NONBLOCKING, &pwzInstallCustomActionData); // cost
		ExitOnFailure1(hr, "failed to add cost to install custom action data: %S", pwzData);

		uiCost += COST_NGEN_NONBLOCKING;
	}

	// If we need 32 bit uninstall update
	if (fNeedUninstallUpdate32)
	{
		hr = StrAllocFormatted(&pwzData, L"%s update /queue", pwz32Ngen);
		ExitOnFailure(hr, "failed to create uninstall update 32 command line");

		hr = WcaWriteStringToCaData(pwzData, &pwzUninstallCustomActionData); // command
		ExitOnFailure1(hr, "failed to add install command to uninstall custom action data: %S", pwzData);

		hr = WcaWriteIntegerToCaData(COST_NGEN_NONBLOCKING, &pwzUninstallCustomActionData); // cost
		ExitOnFailure1(hr, "failed to add cost to uninstall custom action data: %S", pwzData);

		uiCost += COST_NGEN_NONBLOCKING;
	}

	// If we need 64 bit install update
	if (fNeedInstallUpdate64)
	{
		hr = StrAllocFormatted(&pwzData, L"%s update /queue", pwz64Ngen);
		ExitOnFailure(hr, "failed to create install update 64 command line");

		hr = WcaWriteStringToCaData(pwzData, &pwzInstallCustomActionData); // command
		ExitOnFailure1(hr, "failed to add install command to install custom action data: %S", pwzData);

		hr = WcaWriteIntegerToCaData(COST_NGEN_NONBLOCKING, &pwzInstallCustomActionData); // cost
		ExitOnFailure1(hr, "failed to add cost to install custom action data: %S", pwzData);

		uiCost += COST_NGEN_NONBLOCKING;
	}

	// If we need 64 bit install update
	if (fNeedUninstallUpdate64)
	{
		hr = StrAllocFormatted(&pwzData, L"%s update /queue", pwz64Ngen);
		ExitOnFailure(hr, "failed to create uninstall update 64 command line");

		hr = WcaWriteStringToCaData(pwzData, &pwzUninstallCustomActionData); // command
		ExitOnFailure1(hr, "failed to add install command to uninstall custom action data: %S", pwzData);

		hr = WcaWriteIntegerToCaData(COST_NGEN_NONBLOCKING, &pwzUninstallCustomActionData); // cost
		ExitOnFailure1(hr, "failed to add cost to uninstall custom action data: %S", pwzData);

		uiCost += COST_NGEN_NONBLOCKING;
	}

	// Add to progress bar
	if ((pwzInstallCustomActionData && *pwzInstallCustomActionData) || (pwzUninstallCustomActionData && *pwzUninstallCustomActionData))
	{
		hr = WcaProgressMessage(uiCost, TRUE);
		ExitOnFailure(hr, "failed to extend progress bar for NetFxExecuteNativeImage");
	}

	// Schedule the install custom action
	if (pwzInstallCustomActionData && *pwzInstallCustomActionData)
	{
		hr = WcaSetProperty(L"NetFxExecuteNativeImageInstall", pwzInstallCustomActionData);
		ExitOnFailure(hr, "failed to schedule NetFxExecuteNativeImageInstall action");

		hr = WcaSetProperty(L"NetFxExecuteNativeImageCommitInstall", pwzInstallCustomActionData);
		ExitOnFailure(hr, "failed to schedule NetFxExecuteNativeImageCommitInstall action");
	}

	// Schedule the uninstall custom action
	if (pwzUninstallCustomActionData && *pwzUninstallCustomActionData)
	{
		hr = WcaSetProperty(L"NetFxExecuteNativeImageUninstall", pwzUninstallCustomActionData);
		ExitOnFailure(hr, "failed to schedule NetFxExecuteNativeImageUninstall action");

		hr = WcaSetProperty(L"NetFxExecuteNativeImageCommitUninstall", pwzUninstallCustomActionData);
		ExitOnFailure(hr, "failed to schedule NetFxExecuteNativeImageCommitUninstall action");
	}


LExit:
	ReleaseStr(pwzInstallCustomActionData);
	ReleaseStr(pwzUninstallCustomActionData);
	ReleaseStr(pwzId);
	ReleaseStr(pwzData);
	ReleaseStr(pwzTemp);
	ReleaseStr(pwzFile);
	ReleaseStr(pwzFileApp);
	ReleaseStr(pwzDirAppBase);
	ReleaseStr(pwzComponent);
	ReleaseStr(pwz32Ngen);
	ReleaseStr(pwz64Ngen);

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/******************************************************************
 ExecNetFx - entry point for NetFx Custom Action

*******************************************************************/
extern "C" UINT __stdcall ExecNetFx(
	__in MSIHANDLE hInstall
	)
{
//    AssertSz(FALSE, "debug ExecNetFx");

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	LPWSTR pwzCustomActionData = NULL;
	LPWSTR pwzData = NULL;
	LPWSTR pwz = NULL;
	int iCost = 0;

	// initialize
	hr = WcaInitialize(hInstall, "ExecNetFx");
	ExitOnFailure(hr, "failed to initialize");

	hr = WcaGetProperty( L"CustomActionData", &pwzCustomActionData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzCustomActionData);

	pwz = pwzCustomActionData;

	// loop through all the passed in data
	while (pwz && *pwz)
	{
		hr = WcaReadStringFromCaData(&pwz, &pwzData);
		ExitOnFailure(hr, "failed to read command line from custom action data");

		hr = WcaReadIntegerFromCaData(&pwz, &iCost);
		ExitOnFailure(hr, "failed to read cost from custom action data");

		hr = QuietExec(pwzData, NGEN_TIMEOUT);
		ExitOnFailure1(hr, "failed to execute Ngen command: %S", pwzData);

		// Tick the progress bar along for this assembly
		hr = WcaProgressMessage(iCost, FALSE);
		ExitOnFailure1(hr, "failed to tick progress bar for command line: %S", pwzData);
	}

LExit:
	ReleaseStr(pwzCustomActionData);
	ReleaseStr(pwzData);

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}

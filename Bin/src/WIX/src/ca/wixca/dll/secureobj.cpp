//-------------------------------------------------------------------------------------------------
// <copyright file="secureobj.cpp" company="Microsoft">
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
//    Code to secure objects in custom actions when the installer cannot.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// structs
LPCWSTR wzQUERY_SECUREOBJECTS = L"SELECT `SecureObject`, `Table`, `Domain`, `User`, `Permission` FROM `SecureObjects`";
enum eQUERY_SECUREOBJECTS { QSO_SECUREOBJECT = 1, QSO_TABLE, QSO_DOMAIN, QSO_USER, QSO_PERMISSION };

LPCWSTR wzQUERY_SERVICECOMPONENT = L"SELECT `Component_`, `Name` FROM `ServiceInstall` WHERE `ServiceInstall`=?";
LPCWSTR wzQUERY_CREATEFOLDERCOMPONENT = L"SELECT `Component_`, `Directory_` FROM `CreateFolder` WHERE `Directory_`=?";
LPCWSTR wzQUERY_FILECOMPONENT = L"SELECT `Component_`, `File` FROM `File` WHERE `File`=?";
LPCWSTR wzQUERY_REGISTRYCOMPONENT = L"SELECT `Component_`, `Registry`, `Root`, `Key` FROM `Registry` WHERE `Registry`=?";
enum eQUERY_OBJECTCOMPONENT { QSOC_COMPONENT = 1, QSOC_OBJECTNAME, QSOC_REGROOT, QSOC_REGKEY};

enum eOBJECTTYPE { OT_UNKNOWN, OT_SERVICE, OT_FOLDER, OT_FILE, OT_REGISTRY };

/******************************************************************
 CaSchedSecureObjects - entry point for CaReadSecureObjects Custom Action

 called as Type 1 CustomAction (binary DLL) from Windows Installer
 in InstallExecuteSequence before CaSecureObjects
******************************************************************/
extern "C" UINT __stdcall SchedSecureObjects(
	__in MSIHANDLE hInstall
	)
{
//	AssertSz(FALSE, "debug SchedSecureObjects");
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	LPWSTR pwzData = NULL;
	LPWSTR pwzTable = NULL;
	LPWSTR pwzTargetPath = NULL;
	LPWSTR pwzFormattedString = NULL;

	int iRoot = 0;
	int iAllUsers = 0;
	LPWSTR pwzKey = NULL;

	PMSIHANDLE hView = NULL;
	PMSIHANDLE hRec = NULL;

	MSIHANDLE hViewObject = NULL; // Don't free this since it's always a copy of either hViewService or hViewCreateFolder
	PMSIHANDLE hViewService = NULL;
	PMSIHANDLE hViewCreateFolder = NULL;
	PMSIHANDLE hViewFile = NULL;
	PMSIHANDLE hViewRegistry = NULL;
	PMSIHANDLE hRecObject = NULL;

	INSTALLSTATE isInstalled;
	INSTALLSTATE isAction;

	LPWSTR pwzCustomActionData = NULL;
	DWORD cchCustomActionData = 0;

	DWORD cObjects = 0;
	eOBJECTTYPE eType = OT_UNKNOWN;

	//
	// initialize
	//
	hr = WcaInitialize(hInstall, "SchedSecureObjects");
	ExitOnFailure(hr, "failed to initialize");

	//
	// loop through all the objects to be secured
	//
	hr = WcaOpenExecuteView(wzQUERY_SECUREOBJECTS, &hView);
	ExitOnFailure(hr, "failed to open view on SecureObjects table");
	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		hViewObject = NULL;
		eType = OT_UNKNOWN;

		hr = WcaGetRecordString(hRec, QSO_TABLE, &pwzTable);
		ExitOnFailure(hr, "failed to get object table");

		// ensure we're looking at a known table
		if (0 == lstrcmpW(L"ServiceInstall", pwzTable))
		{
			eType = OT_SERVICE;
		}
		else if (0 == lstrcmpW(L"CreateFolder", pwzTable))
		{
			eType = OT_FOLDER;
		}
		else if (0 == lstrcmpW(L"File", pwzTable))
		{
			eType = OT_FILE;
		}
		else if (0 == lstrcmpW(L"Registry", pwzTable))
		{
			eType = OT_REGISTRY;
		}
		else
		{
			ExitOnFailure1(hr = E_INVALIDARG, "unknown SecureObject.Table: %S", pwzTable);
		}

		// if we haven't opened a view on the ServiceInstall/CreateFolder table, do that now
		if (OT_SERVICE == eType)
		{
			if (!hViewService)
			{
				hr = WcaTableExists(pwzTable);
				if (S_FALSE == hr)
					hr = E_UNEXPECTED;
				ExitOnFailure1(hr, "failed to open %s table to secure object", pwzTable);

				hr = WcaOpenView(wzQUERY_SERVICECOMPONENT, &hViewService);
				ExitOnFailure(hr, "failed to open view on ServiceInstall table");
			}

			hViewObject = hViewService;
		}
		else if (OT_FOLDER == eType)
		{
			if (!hViewCreateFolder)
			{
				hr = WcaTableExists(pwzTable);
				if (S_FALSE == hr)
					hr = E_UNEXPECTED;
				ExitOnFailure1(hr, "failed to open %s table to secure object", pwzTable);

				hr = WcaOpenView(wzQUERY_CREATEFOLDERCOMPONENT, &hViewCreateFolder);
				ExitOnFailure(hr, "failed to open view on CreateFolder table");
			}

			hViewObject = hViewCreateFolder;
		}
		else if (OT_FILE== eType)
		{
			if (!hViewFile)
			{
				hr = WcaTableExists(pwzTable);
				if (S_FALSE == hr)
					hr = E_UNEXPECTED;
				ExitOnFailure1(hr, "failed to open %s table to secure object", pwzTable);

				hr = WcaOpenView(wzQUERY_FILECOMPONENT, &hViewFile);
				ExitOnFailure(hr, "failed to open view on CreateFolder table");
			}

			hViewObject = hViewFile;
		}
		else if (OT_REGISTRY== eType)
		{
			if (!hViewRegistry)
			{
				hr = WcaTableExists(pwzTable);
				if (S_FALSE == hr)
					hr = E_UNEXPECTED;
				ExitOnFailure1(hr, "failed to open %s table to secure object", pwzTable);

				hr = WcaOpenView(wzQUERY_REGISTRYCOMPONENT, &hViewRegistry);
				ExitOnFailure(hr, "failed to open view on CreateFolder table");
			}

			hViewObject = hViewRegistry;
		}

		Assert(hViewObject);

		// execute a view looking for the object's Component_
		hr = WcaExecuteView(hViewObject, hRec);
		ExitOnFailure1(hr, "failed to execute view on %S table", pwzData);
		hr = WcaFetchSingleRecord(hViewObject, &hRecObject);
		ExitOnFailure(hr, "failed to fetch Component for secure object");

		hr = WcaGetRecordString(hRecObject, QSOC_COMPONENT, &pwzData);
		ExitOnFailure(hr, "failed to get Component name for secure object");

		//
		// if we are installing this Component
		//
		er = ::MsiGetComponentStateW(hInstall, pwzData, &isInstalled, &isAction);
		ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "failed to get install state for Component: %S", pwzData);

		if (WcaIsInstalling(isInstalled, isAction))
		{
			// add the data to the CustomActionData
			hr = WcaGetRecordString(hRecObject, QSOC_OBJECTNAME, &pwzData);
			ExitOnFailure(hr, "failed to get name of object");

			if (OT_SERVICE == eType)
			{
				hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
				ExitOnFailure(hr, "failed to add data to CustomActionData");
			}
			else if (OT_FOLDER == eType)
			{
				hr = WcaGetTargetPath(pwzData, &pwzTargetPath);
				ExitOnFailure1(hr, "failed to get target path for directory id: %S", pwzData);
				hr = WcaWriteStringToCaData(pwzTargetPath, &pwzCustomActionData);
				ExitOnFailure(hr, "failed to add data to CustomActionData");
			}
			else if (OT_FILE == eType)
			{
				hr = StrAllocFormatted(&pwzFormattedString, L"[#%s]", pwzData);
				ExitOnFailure1(hr, "failed to create formatted string for securing file object: %S", pwzData);

				hr = WcaGetFormattedString(pwzFormattedString, &pwzTargetPath);
				ExitOnFailure2(hr, "failed to get file path from formatted string: %S for secure object: %S", pwzFormattedString, pwzData);

				hr = WcaWriteStringToCaData(pwzTargetPath, &pwzCustomActionData);
				ExitOnFailure(hr, "failed to add data to CustomActionData");
			}
			else if (OT_REGISTRY == eType)
			{
				hr = WcaGetRecordInteger(hRecObject, QSOC_REGROOT, &iRoot);
				ExitOnFailure1(hr, "Failed to get reg key root for secure object: %S", pwzData);

				hr = WcaGetRecordFormattedString(hRecObject, QSOC_REGKEY, &pwzKey);
				ExitOnFailure1(hr, "Failed to get reg key for secure object: %S", pwzData);

				// Decode the root value
				if (-1 == iRoot)
				{
					// They didn't specify a root so that means it's either HKCU or HKLM depending on ALLUSERS property
					hr = WcaGetIntProperty(L"ALLUSERS", &iAllUsers);
					ExitOnFailure(hr, "failed to get value of ALLUSERS property");

					if (1 == iAllUsers)
					{
						hr = StrAllocString(&pwzTargetPath, L"MACHINE\\", 0);
						ExitOnFailure(hr, "failed to allocate target registry string with HKLM root");
					}
					else
					{
						hr = StrAllocString(&pwzTargetPath, L"CURRENT_USER\\", 0);
						ExitOnFailure(hr, "failed to allocate target registry string with HKCU root");
					}
				}
				else if (/*msidbRegistryRootClassesRoot*/ 0 == iRoot)
				{
					hr = StrAllocString(&pwzTargetPath, L"CLASSES_ROOT\\", 0);
					ExitOnFailure(hr, "failed to allocate target registry string with HKCR root");
				}
				else if (/*msidbRegistryRootCurrentUser*/ 1 == iRoot)
				{
					hr = StrAllocString(&pwzTargetPath, L"CURRENT_USER\\", 0);
					ExitOnFailure(hr, "failed to allocate target registry string with HKCU root");
				}
				else if (/*msidbRegistryRootLocalMachine*/ 2 == iRoot)
				{
					hr = StrAllocString(&pwzTargetPath, L"MACHINE\\", 0);
					ExitOnFailure(hr, "failed to allocate target registry string with HKLM root");
				}
				else if (/*msidbRegistryRootUsers*/ 3 == iRoot)
				{
					hr = StrAllocString(&pwzTargetPath, L"USERS\\", 0);
					ExitOnFailure(hr, "failed to allocate target registry string with HKU root");
				}
				else
				{
					ExitOnFailure2(hr = E_UNEXPECTED, "Unknown registry key root specified for secure object: '%S' root: %d", pwzData, iRoot);
				}

				hr = StrAllocConcat(&pwzTargetPath, pwzKey, 0);
				ExitOnFailure2(hr, "Failed to concat key: %S for secure object: %S", pwzKey, pwzData);

				hr = WcaWriteStringToCaData(pwzTargetPath, &pwzCustomActionData);
				ExitOnFailure(hr, "failed to add data to CustomActionData");
			}
			else
			{
				AssertSz(FALSE, "How did you get here?");
			}

			hr = WcaWriteStringToCaData(pwzTable, &pwzCustomActionData);
			ExitOnFailure(hr, "failed to add data to CustomActionData");

			hr = WcaGetRecordFormattedString(hRec, QSO_DOMAIN, &pwzData);
			ExitOnFailure(hr, "failed to get domain for user to configure object");
			hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
			ExitOnFailure(hr, "failed to add data to CustomActionData");

			hr = WcaGetRecordFormattedString(hRec, QSO_USER, &pwzData);
			ExitOnFailure(hr, "failed to get user to configure object");
			hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
			ExitOnFailure(hr, "failed to add data to CustomActionData");

			hr = WcaGetRecordString(hRec, QSO_PERMISSION, &pwzData);
			ExitOnFailure(hr, "failed to get domain for user to configure object");
			hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
			ExitOnFailure(hr, "failed to add data to CustomActionData");

			cObjects++;
		}
	}

	// if we looped through all records all is well
	if (E_NOMOREITEMS == hr)
		hr = S_OK;
	ExitOnFailure(hr, "failed while looping through all objects to secure");

	//
	// schedule the custom action and add to progress bar
	//
	if (pwzCustomActionData && *pwzCustomActionData)
	{
		Assert(0 < cObjects);

		hr = WcaDoDeferredAction(L"ExecSecureObjects", pwzCustomActionData, cObjects * COST_SECUREOBJECT);
		ExitOnFailure(hr, "failed to schedule ExecSecureObjects action");
	}

LExit:
	ReleaseStr(pwzCustomActionData);
	ReleaseStr(pwzData);
	ReleaseStr(pwzTable);
	ReleaseStr(pwzTargetPath);
	ReleaseStr(pwzFormattedString);
	ReleaseStr(pwzKey);

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/******************************************************************
 CaExecSecureObjects - entry point for SecureObjects Custom Action
				   called as Type 1025 CustomAction (deferred binary DLL)

 NOTE: deferred CustomAction since it modifies the machine
 NOTE: CustomActionData == wzObject\twzTable\twzDomain\twzUser\tdwPermissions\twzObject\t...
******************************************************************/
extern "C" UINT __stdcall ExecSecureObjects(
	__in MSIHANDLE hInstall
	)
{
//	AssertSz(FALSE, "debug ExecSecureObjects");
	HRESULT hr = S_OK;
	DWORD er = ERROR_SUCCESS;

	LPWSTR pwz = NULL;
	LPWSTR pwzData = NULL;
	LPWSTR pwzObject = NULL;
	LPWSTR pwzTable = NULL;
	LPWSTR pwzDomain = NULL;
	DWORD dwRevision = 0;
	LPWSTR pwzUser = NULL;
	DWORD dwPermissions = 0;
	LPWSTR pwzAccount = NULL;
	PSID psid = NULL;

	EXPLICIT_ACCESSW ea = {0};
	SE_OBJECT_TYPE objectType = SE_UNKNOWN_OBJECT_TYPE;
	PSECURITY_DESCRIPTOR psd = NULL;
	SECURITY_DESCRIPTOR_CONTROL sdc = {0};
	SECURITY_INFORMATION si = {0};
	PACL pAclExisting = NULL;   // doesn't get freed
	PACL pAclNew = NULL;

	PMSIHANDLE hActionRec = ::MsiCreateRecord(1);

	//
	// initialize
	//
	hr = WcaInitialize(hInstall, "ExecSecureObjects");
	ExitOnFailure(hr, "failed to initialize");

	hr = WcaGetProperty(L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzData);

	pwz = pwzData;

	//
	// loop through all the passed in data
	//
	while (pwz && *pwz)
	{
		hr = WcaReadStringFromCaData(&pwz, &pwzObject);
		ExitOnFailure(hr, "failed to process CustomActionData");

		hr = WcaReadStringFromCaData(&pwz, &pwzTable);
		ExitOnFailure(hr, "failed to process CustomActionData");
		hr = WcaReadStringFromCaData(&pwz, &pwzDomain);
		ExitOnFailure(hr, "failed to process CustomActionData");
		hr = WcaReadStringFromCaData(&pwz, &pwzUser);
		ExitOnFailure(hr, "failed to process CustomActionData");
		hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&dwPermissions));
		ExitOnFailure(hr, "failed to processCustomActionData");

		WcaLog(LOGMSG_VERBOSE, "Securing Object: %S Type: %S User: %S", pwzObject, pwzTable, pwzUser);

		//
		// create the appropriate SID
		//

		// figure out the right user to put into the access block
		if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"Everyone"))
		{
			hr = AclGetWellKnownSid(WinWorldSid, &psid);
		}
		else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"Administrators"))
		{
			hr = AclGetWellKnownSid(WinBuiltinAdministratorsSid, &psid);
		}
		else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"LocalSystem"))
		{
			hr = AclGetWellKnownSid(WinLocalSystemSid, &psid);
		}
		else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"LocalService"))
		{
			hr = AclGetWellKnownSid(WinLocalServiceSid, &psid);
		}
		else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"NetworkService"))
		{
			hr = AclGetWellKnownSid(WinNetworkServiceSid, &psid);
		}
		else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"AuthenticatedUser"))
		{
			hr = AclGetWellKnownSid(WinAuthenticatedUserSid, &psid);
		}
		else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"Guests"))
		{
			hr = AclGetWellKnownSid(WinBuiltinGuestsSid, &psid);
		}
		else if(!*pwzDomain && 0 == lstrcmpW(pwzUser, L"CREATOR OWNER"))
		{
			hr = AclGetWellKnownSid(WinCreatorOwnerSid, &psid);
		}
		else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"INTERACTIVE"))
		{
			hr = AclGetWellKnownSid(WinInteractiveSid, &psid);
		}
		else if(!*pwzDomain && 0 == lstrcmpW(pwzUser, L"Users"))
		{
			hr = AclGetWellKnownSid(WinBuiltinUsersSid, &psid);
		}
		else
		{
			hr = StrAllocFormatted(&pwzAccount, L"%s\\%s", *pwzDomain ? pwzDomain : L".", pwzUser);
			ExitOnFailure(hr, "failed to build domain user name");

			hr = AclGetAccountSid(NULL, pwzAccount, &psid);
		}
		ExitOnFailure3(hr, "failed to get sid for account: %S%S%S", pwzDomain, *pwzDomain ? L"\\" : L"", pwzUser);

		//
		// build up the explicit access
		//
		ea.grfAccessPermissions = dwPermissions;
		ea.grfAccessMode = SET_ACCESS;

		if (0 == lstrcmpW(L"CreateFolder", pwzTable))
		{
			ea.grfInheritance = SUB_CONTAINERS_AND_OBJECTS_INHERIT;
		}
		else
		{
			ea.grfInheritance = NO_INHERITANCE;
		}

		::BuildTrusteeWithSidW(&ea.Trustee, psid);

		if (0 == lstrcmpW(L"ServiceInstall", pwzTable))
		{
			objectType = SE_SERVICE;

			// always add these permissions for services
			// these are basic permissions that are often forgotten
			dwPermissions |= SERVICE_QUERY_CONFIG | SERVICE_QUERY_STATUS | SERVICE_ENUMERATE_DEPENDENTS | SERVICE_INTERROGATE;
		}
		else if (0 == lstrcmpW(L"CreateFolder", pwzTable) || 0 == lstrcmpW(L"File", pwzTable))
		{
			objectType = SE_FILE_OBJECT;
		}
		else if (0 == lstrcmpW(L"Registry", pwzTable))
		{
			objectType = SE_REGISTRY_KEY;
		}

		if (SE_UNKNOWN_OBJECT_TYPE != objectType)
		{
			er = ::GetNamedSecurityInfoW(pwzObject, objectType, DACL_SECURITY_INFORMATION, NULL, NULL, &pAclExisting, NULL, &psd);
			ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "failed to get security info for object: %S", pwzObject);

			//Need to see if DACL is protected so getting Descriptor information
			if(!::GetSecurityDescriptorControl(psd, &sdc, &dwRevision))
			{
				ExitOnLastError1(hr, "failed to get security descriptor control for object: %S", pwzObject);
			}

			er = ::SetEntriesInAclW(1, &ea, pAclExisting, &pAclNew);
			ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "failed to add ACLs for object: %S", pwzObject);

			if (sdc & SE_DACL_PROTECTED)
			{
				si = DACL_SECURITY_INFORMATION | PROTECTED_DACL_SECURITY_INFORMATION;
			}
			else
			{
				si = DACL_SECURITY_INFORMATION;
			}
			er = ::SetNamedSecurityInfoW(pwzObject, objectType, si, NULL, NULL, pAclNew, NULL);
			MessageExitOnFailure1(hr = HRESULT_FROM_WIN32(er), msierrSecureObjectsFailedSet, "failed to set security info for object: %S", pwzObject);
		}
		else
		{
			MessageExitOnFailure1(hr = E_UNEXPECTED, msierrSecureObjectsUnknownType, "unknown object type: %S", pwzTable);
		}

		hr = WcaProgressMessage(COST_SECUREOBJECT, FALSE);
		ExitOnFailure(hr, "failed to send progress message");

		objectType = SE_UNKNOWN_OBJECT_TYPE;
	}

LExit:
	ReleaseStr(pwzUser);
	ReleaseStr(pwzDomain);
	ReleaseStr(pwzTable);
	ReleaseStr(pwzObject);
	ReleaseStr(pwzData);
	ReleaseStr(pwzAccount);

	if (pAclNew)
		::LocalFree(pAclNew);
	if (psd)
		::LocalFree(psd);
	if (psid)
		AclFreeSid(psid);

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}

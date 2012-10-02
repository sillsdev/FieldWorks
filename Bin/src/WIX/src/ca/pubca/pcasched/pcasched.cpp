//-------------------------------------------------------------------------------------------------
// <copyright file="pcasched.cpp" company="Microsoft">
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
//    Windows Installer XML Public Scheduling CustomAction.
// </summary>
//-------------------------------------------------------------------------------------------------
#include "precomp.h"

/********************************************************************
 DllMain - standard entry point for all WiX CustomActions

********************************************************************/
extern "C" BOOL WINAPI DllMain(
	IN HINSTANCE hInst,
	IN ULONG ulReason,
	IN LPVOID)
{
	switch(ulReason)
	{
	case DLL_PROCESS_ATTACH:
		WcaGlobalInitialize(hInst);
		break;

	case DLL_PROCESS_DETACH:
		WcaGlobalFinalize();
		break;
	}

	return TRUE;
}


/********************************************************************
 ConfigureComPlusInstall - CUSTOM ACTION ENTRY POINT for installing COM+ components

********************************************************************/
extern "C" UINT __stdcall ConfigureComPlusInstall(MSIHANDLE hInstall)
{
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	BOOL fInitializedCom = FALSE;

	ICOMAdminCatalog* piCatalog = NULL;

	CPI_PARTITION_LIST partList;
	CPI_PARTITION_ROLE_LIST partRoleList;
	CPI_USER_IN_PARTITION_ROLE_LIST usrInPartRoleList;
	CPI_PARTITION_USER_LIST partUsrList;
	CPI_APPLICATION_LIST appList;
	CPI_APPLICATION_ROLE_LIST appRoleList;
	CPI_USER_IN_APPLICATION_ROLE_LIST usrInAppRoleList;
	CPI_ASSEMBLY_LIST asmList;
	CPI_SUBSCRIPTION_LIST subList;

	LPWSTR pwzRollbackFileName = NULL;
	LPWSTR pwzActionData = NULL;
	LPWSTR pwzRollbackActionData = NULL;
	LPWSTR pwzCommitActionData = NULL;

	int iVersionNT = 0;
	int iProgress = 0;
	int iCommitProgress = 0;

	::ZeroMemory(&partList, sizeof(CPI_PARTITION_LIST));
	::ZeroMemory(&partRoleList, sizeof(CPI_PARTITION_ROLE_LIST));
	::ZeroMemory(&usrInPartRoleList, sizeof(CPI_USER_IN_PARTITION_ROLE_LIST));
	::ZeroMemory(&partUsrList, sizeof(CPI_PARTITION_USER_LIST));
	::ZeroMemory(&appList, sizeof(CPI_APPLICATION_LIST));
	::ZeroMemory(&appRoleList, sizeof(CPI_APPLICATION_ROLE_LIST));
	::ZeroMemory(&usrInAppRoleList, sizeof(CPI_USER_IN_APPLICATION_ROLE_LIST));
	::ZeroMemory(&asmList, sizeof(CPI_ASSEMBLY_LIST));
	::ZeroMemory(&subList, sizeof(CPI_SUBSCRIPTION_LIST));

	// initialize
	hr = WcaInitialize(hInstall, "ConfigureComPlusInstall");
	ExitOnFailure(hr, "Failed to initialize");

	hr = ::CoInitialize(NULL);
	ExitOnFailure(hr, "Failed to initialize COM");
	fInitializedCom = TRUE;

	CpiInitialize();

	// check for the prerequsite tables
	if (!CpiTableExists(cptComPlusPartition) && !CpiTableExists(cptComPlusApplication) && !CpiTableExists(cptComPlusAssembly))
	{
		WcaLog(LOGMSG_VERBOSE, "skipping install COM+ CustomAction, no ComPlusPartition, ComPlusApplication or ComPlusAssembly table present");
		ExitFunction1(hr = S_FALSE);
	}

	// make sure we can access the COM+ admin catalog
	do {
		hr = CpiGetAdminCatalog(&piCatalog);
		if (FAILED(hr))
		{
			WcaLog(LOGMSG_STANDARD, "Failed to get COM+ admin catalog");
			er = WcaErrorMessage(msierrComPlusCannotConnect, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
			switch (er)
			{
			case IDABORT:
				ExitFunction(); // exit with hr from CpiGetAdminCatalog() to kick off a rollback
			case IDRETRY:
				hr = S_FALSE;
				break;
			case IDIGNORE:
				ExitFunction1(hr = S_OK); // pretend everything is okay and bail
			}
		}
	} while (S_FALSE == hr);

	// get NT version
	hr = WcaGetIntProperty(L"VersionNT", &iVersionNT);
	ExitOnFailure(hr, "Failed to get VersionNT property");

	// read elements
	if (502 <= iVersionNT && CpiTableExists(cptComPlusPartition))
	{
		hr = CpiPartitionsRead(&partList);
		MessageExitOnFailure(hr, msierrComPlusPartitionReadFailed, "Failed to read ComPlusPartitions table");
	}

	if (502 <= iVersionNT && CpiTableExists(cptComPlusPartitionRole))
	{
		hr = CpiPartitionRolesRead(&partList, &partRoleList);
		MessageExitOnFailure(hr, msierrComPlusPartitionRoleReadFailed, "Failed to read ComPlusPartitionRole table");
	}

	if (502 <= iVersionNT && (CpiTableExists(cptComPlusUserInPartitionRole) || CpiTableExists(cptComPlusGroupInPartitionRole)))
	{
		hr = CpiUsersInPartitionRolesRead(&partRoleList, &usrInPartRoleList);
		MessageExitOnFailure(hr, msierrComPlusUserInPartitionRoleReadFailed, "Failed to read ComPlusUserInPartitionRole table");
	}

	if (502 <= iVersionNT && CpiTableExists(cptComPlusPartitionUser))
	{
		hr = CpiPartitionUsersRead(&partList, &partUsrList);
		MessageExitOnFailure(hr, msierrComPlusPartitionUserReadFailed, "Failed to read ComPlusPartitionUser table");
	}

	if (CpiTableExists(cptComPlusApplication))
	{
		hr = CpiApplicationsRead(&partList, &appList);
		MessageExitOnFailure(hr, msierrComPlusApplicationReadFailed, "Failed to read ComPlusApplication table");
	}

	if (CpiTableExists(cptComPlusApplicationRole))
	{
		hr = CpiApplicationRolesRead(&appList, &appRoleList);
		MessageExitOnFailure(hr, msierrComPlusApplicationRoleReadFailed, "Failed to read ComPlusApplicationRole table");
	}

	if (CpiTableExists(cptComPlusUserInApplicationRole) || CpiTableExists(cptComPlusGroupInApplicationRole))
	{
		hr = CpiUsersInApplicationRolesRead(&appRoleList, &usrInAppRoleList);
		MessageExitOnFailure(hr, msierrComPlusUserInApplicationRoleReadFailed, "Failed to read ComPlusUserInApplicationRole table");
	}

	if (CpiTableExists(cptComPlusAssembly))
	{
		hr = CpiAssembliesRead(&appList, &appRoleList, &asmList);
		MessageExitOnFailure(hr, msierrComPlusAssembliesReadFailed, "Failed to read ComPlusAssembly table");
	}

	if (CpiTableExists(cptComPlusSubscription))
	{
		hr = CpiSubscriptionsRead(&asmList, &subList);
		MessageExitOnFailure(hr, msierrComPlusSubscriptionReadFailed, "Failed to read ComPlusSubscription table");
	}

	// verify elements
	hr = CpiPartitionsVerifyInstall(&partList);
	ExitOnFailure(hr, "Failed to verify partitions");

	hr = CpiApplicationsVerifyInstall(&appList);
	ExitOnFailure(hr, "Failed to verify applications");

	hr = CpiApplicationRolesVerifyInstall(&appRoleList);
	ExitOnFailure(hr, "Failed to verify application roles");

	hr = CpiAssembliesVerifyInstall(&asmList);
	ExitOnFailure(hr, "Failed to verify assemblies");

	if (subList.iInstallCount)
	{
		hr = CpiSubscriptionsVerifyInstall(&subList);
		ExitOnFailure(hr, "Failed to verify subscriptions");
	}

	// schedule
	if (partList.iInstallCount || appList.iInstallCount || usrInAppRoleList.iInstallCount ||
		appRoleList.iInstallCount || asmList.iInstallCount || asmList.iRoleInstallCount || subList.iInstallCount)
	{
		// create rollback file name
		hr = CpiGetTempFileName(&pwzRollbackFileName);
		ExitOnFailure(hr, "Failed to get rollback file name");

		// schedule rollback prepare custom action
		hr = WcaDoDeferredAction(L"ComPlusRollbackInstallPrepare", pwzRollbackFileName, 0);
		ExitOnFailure(hr, "Failed to schedule ComPlusRollbackInstallPrepare");

		// schedule prepare custom action
		hr = WcaDoDeferredAction(L"ComPlusInstallPrepare", pwzRollbackFileName, 0);
		ExitOnFailure(hr, "Failed to schedule ComPlusInstallPrepare");

		// schedule rollback custom action
		hr = WcaWriteStringToCaData(pwzRollbackFileName, &pwzRollbackActionData);
		ExitOnFailure(hr, "Failed to add rollback file name to rollback custom action data");

		hr = CpiSubscriptionsInstall(&subList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to install subscriptions");
		hr = CpiRoleAssignmentsInstall(&asmList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to install assemblies");
		hr = CpiAssembliesInstall(&asmList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to install assemblies");
		hr = CpiUsersInApplicationRolesInstall(&usrInAppRoleList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to install users in application roles");
		hr = CpiApplicationRolesInstall(&appRoleList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to install application roles");
		hr = CpiApplicationsInstall(&appList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to install applications");
		hr = CpiPartitionUsersInstall(&partUsrList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to install partition users");
		hr = CpiUsersInPartitionRolesInstall(&usrInPartRoleList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to install users in partition roles");
		hr = CpiPartitionsInstall(&partList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to install partitions");

		hr = WcaDoDeferredAction(L"ComPlusRollbackInstallExecute", pwzRollbackActionData, 0);
		ExitOnFailure(hr, "Failed to schedule ComPlusRollbackInstallExecute");

		// schedule install custom action
		hr = WcaWriteStringToCaData(pwzRollbackFileName, &pwzActionData);
		ExitOnFailure(hr, "Failed to add rollback file name to custom action data");

		hr = CpiPartitionsInstall(&partList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to install partitions");
		hr = CpiUsersInPartitionRolesInstall(&usrInPartRoleList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to install users in partition roles");
		hr = CpiPartitionUsersInstall(&partUsrList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to install partition users");
		hr = CpiApplicationsInstall(&appList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to install applications");
		hr = CpiApplicationRolesInstall(&appRoleList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to install application roles");
		hr = CpiUsersInApplicationRolesInstall(&usrInAppRoleList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to install users in application roles");
		hr = CpiAssembliesInstall(&asmList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to install assemblies");
		hr = CpiRoleAssignmentsInstall(&asmList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to install assemblies");
		hr = CpiSubscriptionsInstall(&subList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to install subscriptions");

		hr = WcaDoDeferredAction(L"ComPlusInstallExecute", pwzActionData, iProgress);
		ExitOnFailure(hr, "Failed to schedule ComPlusInstallExecute");

		// schedule install commit custom action
		hr = WcaWriteStringToCaData(pwzRollbackFileName, &pwzCommitActionData);
		ExitOnFailure(hr, "Failed to add rollback file name to commit custom action data");

		hr = CpiAssembliesInstall(&asmList, rmCommit, &pwzCommitActionData, &iCommitProgress);
		ExitOnFailure(hr, "Failed to install assemblies");
		hr = CpiRoleAssignmentsInstall(&asmList, rmCommit, &pwzCommitActionData, &iCommitProgress);
		ExitOnFailure(hr, "Failed to install assemblies");
		hr = CpiSubscriptionsInstall(&subList, rmCommit, &pwzCommitActionData, &iCommitProgress);
		ExitOnFailure(hr, "Failed to install subscriptions");

		hr = WcaDoDeferredAction(L"ComPlusInstallExecuteCommit", pwzCommitActionData, iCommitProgress);
		ExitOnFailure(hr, "Failed to schedule ComPlusInstallExecuteCommit");

		// schedule commit custom action
		hr = WcaDoDeferredAction(L"ComPlusInstallCommit", pwzRollbackFileName, 0);
		ExitOnFailure(hr, "Failed to schedule ComPlusInstallCommit");
	}

	hr = S_OK;

LExit:
	// clean up
	ReleaseObject(piCatalog);

	ReleaseStr(pwzRollbackFileName);
	ReleaseStr(pwzActionData);
	ReleaseStr(pwzRollbackActionData);
	ReleaseStr(pwzCommitActionData);

	CpiPartitionListFree(&partList);
	CpiPartitionRoleListFree(&partRoleList);
	CpiUserInPartitionRoleListFree(&usrInPartRoleList);
	CpiPartitionUserListFree(&partUsrList);
	CpiApplicationListFree(&appList);
	CpiApplicationRoleListFree(&appRoleList);
	CpiUserInApplicationRoleListFree(&usrInAppRoleList);
	CpiAssemblyListFree(&asmList);
	CpiSubscriptionListFree(&subList);

	// unitialize
	CpiFinalize();

	if (fInitializedCom)
		::CoUninitialize();

	er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/********************************************************************
 ConfigureComPlusUninstall - CUSTOM ACTION ENTRY POINT for uninstalling COM+ components

********************************************************************/
extern "C" UINT __stdcall ConfigureComPlusUninstall(MSIHANDLE hInstall)
{
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	BOOL fInitializedCom = FALSE;

	ICOMAdminCatalog* piCatalog = NULL;

	CPI_PARTITION_LIST partList;
	CPI_PARTITION_ROLE_LIST partRoleList;
	CPI_USER_IN_PARTITION_ROLE_LIST usrInPartRoleList;
	CPI_PARTITION_USER_LIST partUsrList;
	CPI_APPLICATION_LIST appList;
	CPI_APPLICATION_ROLE_LIST appRoleList;
	CPI_USER_IN_APPLICATION_ROLE_LIST usrInAppRoleList;
	CPI_ASSEMBLY_LIST asmList;
	CPI_SUBSCRIPTION_LIST subList;

	LPWSTR pwzRollbackFileName = NULL;
	LPWSTR pwzActionData = NULL;
	LPWSTR pwzRollbackActionData = NULL;

	int iVersionNT = 0;
	int iProgress = 0;

	::ZeroMemory(&partList, sizeof(CPI_PARTITION_LIST));
	::ZeroMemory(&partRoleList, sizeof(CPI_PARTITION_ROLE_LIST));
	::ZeroMemory(&usrInPartRoleList, sizeof(CPI_USER_IN_PARTITION_ROLE_LIST));
	::ZeroMemory(&partUsrList, sizeof(CPI_PARTITION_USER_LIST));
	::ZeroMemory(&appList, sizeof(CPI_APPLICATION_LIST));
	::ZeroMemory(&appRoleList, sizeof(CPI_APPLICATION_ROLE_LIST));
	::ZeroMemory(&usrInAppRoleList, sizeof(CPI_USER_IN_APPLICATION_ROLE_LIST));
	::ZeroMemory(&asmList, sizeof(CPI_ASSEMBLY_LIST));
	::ZeroMemory(&subList, sizeof(CPI_SUBSCRIPTION_LIST));

	// initialize
	hr = WcaInitialize(hInstall, "ConfigureComPlusUninstall");
	ExitOnFailure(hr, "Failed to initialize");

	hr = ::CoInitialize(NULL);
	ExitOnFailure(hr, "Failed to initialize COM");
	fInitializedCom = TRUE;

	CpiInitialize();

	// check for the prerequsite tables
	if (!CpiTableExists(cptComPlusPartition) && !CpiTableExists(cptComPlusApplication) && !CpiTableExists(cptComPlusAssembly))
	{
		WcaLog(LOGMSG_VERBOSE, "skipping uninstall COM+ CustomAction, no ComPlusPartition, ComPlusApplication or ComPlusAssembly table present");
		ExitFunction1(hr = S_FALSE);
	}

	// make sure we can access the COM+ admin catalog
	do {
		hr = CpiGetAdminCatalog(&piCatalog);
		if (FAILED(hr))
		{
			WcaLog(LOGMSG_STANDARD, "Failed to get COM+ admin catalog");
			er = WcaErrorMessage(msierrComPlusCannotConnect, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
			switch (er)
			{
			case IDABORT:
				ExitFunction(); // exit with hr from CpiGetAdminCatalog() to kick off a rollback
			case IDRETRY:
				hr = S_FALSE;
				break;
			case IDIGNORE:
				ExitFunction1(hr = S_OK); // pretend everything is okay and bail
			}
		}
	} while (S_FALSE == hr);

	// get NT version
	hr = WcaGetIntProperty(L"VersionNT", &iVersionNT);
	ExitOnFailure(hr, "Failed to get VersionNT property");

	// read elements
	if (502 <= iVersionNT && CpiTableExists(cptComPlusPartition))
	{
		hr = CpiPartitionsRead(&partList);
		MessageExitOnFailure(hr, msierrComPlusPartitionReadFailed, "Failed to read ComPlusPartitions table");
	}

	if (502 <= iVersionNT && CpiTableExists(cptComPlusPartitionRole))
	{
		hr = CpiPartitionRolesRead(&partList, &partRoleList);
		MessageExitOnFailure(hr, msierrComPlusPartitionRoleReadFailed, "Failed to read ComPlusPartitionRole table");
	}

	if (502 <= iVersionNT && (CpiTableExists(cptComPlusUserInPartitionRole) || CpiTableExists(cptComPlusGroupInPartitionRole)))
	{
		hr = CpiUsersInPartitionRolesRead(&partRoleList, &usrInPartRoleList);
		MessageExitOnFailure(hr, msierrComPlusUserInPartitionRoleReadFailed, "Failed to read ComPlusUserInPartitionRole table");
	}

	if (502 <= iVersionNT && CpiTableExists(cptComPlusPartitionUser))
	{
		hr = CpiPartitionUsersRead(&partList, &partUsrList);
		MessageExitOnFailure(hr, msierrComPlusPartitionUserReadFailed, "Failed to read ComPlusPartitionUser table");
	}

	if (CpiTableExists(cptComPlusApplication))
	{
		hr = CpiApplicationsRead(&partList, &appList);
		MessageExitOnFailure(hr, msierrComPlusApplicationReadFailed, "Failed to read ComPlusApplication table");
	}

	if (CpiTableExists(cptComPlusApplicationRole))
	{
		hr = CpiApplicationRolesRead(&appList, &appRoleList);
		MessageExitOnFailure(hr, msierrComPlusApplicationRoleReadFailed, "Failed to read ComPlusApplicationRole table");
	}

	if (CpiTableExists(cptComPlusUserInApplicationRole) || CpiTableExists(cptComPlusGroupInApplicationRole))
	{
		hr = CpiUsersInApplicationRolesRead(&appRoleList, &usrInAppRoleList);
		MessageExitOnFailure(hr, msierrComPlusUserInApplicationRoleReadFailed, "Failed to read ComPlusUserInApplicationRole table");
	}

	if (CpiTableExists(cptComPlusAssembly))
	{
		hr = CpiAssembliesRead(&appList, &appRoleList, &asmList);
		MessageExitOnFailure(hr, msierrComPlusAssembliesReadFailed, "Failed to read ComPlusAssembly table");
	}

	if (CpiTableExists(cptComPlusSubscription))
	{
		hr = CpiSubscriptionsRead(&asmList, &subList);
		MessageExitOnFailure(hr, msierrComPlusSubscriptionReadFailed, "Failed to read ComPlusSubscription table");
	}

	// verify elements
	hr = CpiPartitionsVerifyUninstall(&partList);
	ExitOnFailure(hr, "Failed to verify partitions");

	hr = CpiApplicationsVerifyUninstall(&appList);
	ExitOnFailure(hr, "Failed to verify applications");

	hr = CpiApplicationRolesVerifyUninstall(&appRoleList);
	ExitOnFailure(hr, "Failed to verify application roles");

	hr = CpiAssembliesVerifyUninstall(&asmList);
	ExitOnFailure(hr, "Failed to verify assemblies");

	if (subList.iUninstallCount)
	{
		hr = CpiSubscriptionsVerifyUninstall(&subList);
		ExitOnFailure(hr, "Failed to verify subscriptions");
	}

	// schedule
	if (partList.iUninstallCount || appList.iUninstallCount || appRoleList.iUninstallCount ||
		usrInAppRoleList.iUninstallCount || asmList.iUninstallCount || asmList.iRoleUninstallCount || subList.iUninstallCount)
	{
		// create rollback file name
		hr = CpiGetTempFileName(&pwzRollbackFileName);
		ExitOnFailure(hr, "Failed to get rollback file name");

		// schedule rollback prepare custom action
		hr = WcaDoDeferredAction(L"ComPlusRollbackInstallPrepare", pwzRollbackFileName, 0);
		ExitOnFailure(hr, "Failed to schedule ComPlusRollbackInstallPrepare");

		// schedule prepare custom action
		hr = WcaDoDeferredAction(L"ComPlusInstallPrepare", pwzRollbackFileName, 0);
		ExitOnFailure(hr, "Failed to schedule ComPlusInstallPrepare");

		// schedule rollback custom action
		hr = WcaWriteStringToCaData(pwzRollbackFileName, &pwzRollbackActionData);
		ExitOnFailure(hr, "Failed to add rollback file name to rollback custom action data");

		hr = CpiPartitionsUninstall(&partList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to uninstall partitions");
		hr = CpiUsersInPartitionRolesUninstall(&usrInPartRoleList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to uninstall users in partition roles");
		hr = CpiPartitionUsersUninstall(&partUsrList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to uninstall partition users");
		hr = CpiApplicationsUninstall(&appList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to uninstall applications");
		hr = CpiApplicationRolesUninstall(&appRoleList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to uninstall application roles");
		hr = CpiUsersInApplicationRolesUninstall(&usrInAppRoleList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to uninstall users in application roles");
		hr = CpiAssembliesUninstall(&asmList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to uninstall assemblies");
		hr = CpiRoleAssignmentsUninstall(&asmList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to uninstall assemblies");
		hr = CpiSubscriptionsUninstall(&subList, rmRollback, &pwzRollbackActionData, NULL);
		ExitOnFailure(hr, "Failed to uninstall subscriptions");

		hr = WcaDoDeferredAction(L"ComPlusRollbackUninstallExecute", pwzRollbackActionData, 0);
		ExitOnFailure(hr, "Failed to schedule ComPlusRollbackUninstallExecute");

		// schedule install custom action
		hr = WcaWriteStringToCaData(pwzRollbackFileName, &pwzActionData);
		ExitOnFailure(hr, "Failed to add rollback file name to custom action data");

		hr = CpiSubscriptionsUninstall(&subList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to uninstall subscriptions");
		hr = CpiRoleAssignmentsUninstall(&asmList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to uninstall assemblies");
		hr = CpiAssembliesUninstall(&asmList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to uninstall assemblies");
		hr = CpiUsersInApplicationRolesUninstall(&usrInAppRoleList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to uninstall users in application roles");
		hr = CpiApplicationRolesUninstall(&appRoleList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to uninstall application roles");
		hr = CpiApplicationsUninstall(&appList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to uninstall applications");
		hr = CpiPartitionUsersUninstall(&partUsrList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to uninstall partition users");
		hr = CpiUsersInPartitionRolesUninstall(&usrInPartRoleList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to uninstall users in partition roles");
		hr = CpiPartitionsUninstall(&partList, rmDeferred, &pwzActionData, &iProgress);
		ExitOnFailure(hr, "Failed to uninstall partitions");

		hr = WcaDoDeferredAction(L"ComPlusUninstallExecute", pwzActionData, iProgress);
		ExitOnFailure(hr, "Failed to schedule ComPlusUninstallExecute");

		// schedule commit custom action
		hr = WcaDoDeferredAction(L"ComPlusInstallCommit", pwzRollbackFileName, 0);
		ExitOnFailure(hr, "Failed to schedule ComPlusInstallCommit");
	}

	hr = S_OK;

LExit:
	// clean up
	ReleaseObject(piCatalog);

	ReleaseStr(pwzRollbackFileName);
	ReleaseStr(pwzActionData);
	ReleaseStr(pwzRollbackActionData);

	CpiPartitionListFree(&partList);
	CpiPartitionRoleListFree(&partRoleList);
	CpiUserInPartitionRoleListFree(&usrInPartRoleList);
	CpiPartitionUserListFree(&partUsrList);
	CpiApplicationListFree(&appList);
	CpiApplicationRoleListFree(&appRoleList);
	CpiUserInApplicationRoleListFree(&usrInAppRoleList);
	CpiAssemblyListFree(&asmList);
	CpiSubscriptionListFree(&subList);

	// unitialize
	CpiFinalize();

	if (fInitializedCom)
		::CoUninitialize();

	er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/********************************************************************
 MessageQueuingInstall - CUSTOM ACTION ENTRY POINT for installing MSMQ message queues

********************************************************************/
extern "C" UINT __stdcall MessageQueuingInstall(MSIHANDLE hInstall)
{
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	MQI_MESSAGE_QUEUE_LIST lstMessageQueues;
	MQI_MESSAGE_QUEUE_PERMISSION_LIST lstMessageQueuePermissions;

	int iCost = 0;
	LPWSTR pwzRollbackActionData = NULL;
	LPWSTR pwzExecuteActionData = NULL;

	::ZeroMemory(&lstMessageQueues, sizeof(lstMessageQueues));
	::ZeroMemory(&lstMessageQueuePermissions, sizeof(lstMessageQueuePermissions));

	// initialize
	hr = WcaInitialize(hInstall, "MessageQueuingInstall");
	ExitOnFailure(hr, "Failed to initialize");

	// read message queues
	hr = MqiMessageQueueRead(&lstMessageQueues);
	ExitOnFailure(hr, "Failed to read MessageQueue table");

	// read message queue permissions
	hr = MqiMessageQueuePermissionRead(&lstMessageQueues, &lstMessageQueuePermissions);
	ExitOnFailure(hr, "Failed to read message queue permissions");

	if (lstMessageQueues.iInstallCount || lstMessageQueuePermissions.iInstallCount)
	{
		// schedule rollback action
		hr = MqiMessageQueuePermissionInstall(&lstMessageQueuePermissions, &pwzRollbackActionData);
		ExitOnFailure(hr, "Failed to add message queue permissions to rollback action data");

		hr = MqiMessageQueueInstall(&lstMessageQueues, &pwzRollbackActionData);
		ExitOnFailure(hr, "Failed to add message queues to rollback action data");

		hr = WcaDoDeferredAction(L"MessageQueuingRollbackInstall", pwzRollbackActionData, 0);
		ExitOnFailure(hr, "Failed to schedule MessageQueuingRollbackInstall");

		// schedule execute action
		hr = MqiMessageQueueInstall(&lstMessageQueues, &pwzExecuteActionData);
		ExitOnFailure(hr, "Failed to add message queues to execute action data");
		iCost += lstMessageQueues.iInstallCount * COST_MESSAGE_QUEUE_CREATE;

		hr = MqiMessageQueuePermissionInstall(&lstMessageQueuePermissions, &pwzExecuteActionData);
		ExitOnFailure(hr, "Failed to add message queue permissions to execute action data");
		iCost += lstMessageQueues.iInstallCount * COST_MESSAGE_QUEUE_PERMISSION_ADD;

		hr = WcaDoDeferredAction(L"MessageQueuingExecuteInstall", pwzExecuteActionData, iCost);
		ExitOnFailure(hr, "Failed to schedule MessageQueuingExecuteInstall");
	}

	hr = S_OK;

LExit:
	// clean up
	MqiMessageQueueFreeList(&lstMessageQueues);
	MqiMessageQueuePermissionFreeList(&lstMessageQueuePermissions);

	ReleaseStr(pwzRollbackActionData);
	ReleaseStr(pwzExecuteActionData);

	er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/********************************************************************
 MessageQueuingUninstall - CUSTOM ACTION ENTRY POINT for uninstalling MSMQ message queues

********************************************************************/
extern "C" UINT __stdcall MessageQueuingUninstall(MSIHANDLE hInstall)
{
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	MQI_MESSAGE_QUEUE_LIST lstMessageQueues;
	MQI_MESSAGE_QUEUE_PERMISSION_LIST lstMessageQueuePermissions;

	int iCost = 0;
	LPWSTR pwzRollbackActionData = NULL;
	LPWSTR pwzExecuteActionData = NULL;

	::ZeroMemory(&lstMessageQueues, sizeof(lstMessageQueues));
	::ZeroMemory(&lstMessageQueuePermissions, sizeof(lstMessageQueuePermissions));

	// initialize
	hr = WcaInitialize(hInstall, "MessageQueuingUninstall");
	ExitOnFailure(hr, "Failed to initialize");

	// read message queues
	hr = MqiMessageQueueRead(&lstMessageQueues);
	ExitOnFailure(hr, "Failed to read MessageQueue table");

	// read message queue permissions
	hr = MqiMessageQueuePermissionRead(&lstMessageQueues, &lstMessageQueuePermissions);
	ExitOnFailure(hr, "Failed to read message queue permissions");

	if (lstMessageQueues.iUninstallCount || lstMessageQueuePermissions.iUninstallCount)
	{
		// schedule rollback action
		hr = MqiMessageQueueUninstall(&lstMessageQueues, &pwzRollbackActionData);
		ExitOnFailure(hr, "Failed to add message queues to rollback action data");

		hr = MqiMessageQueuePermissionUninstall(&lstMessageQueuePermissions, &pwzRollbackActionData);
		ExitOnFailure(hr, "Failed to add message queue permissions to rollback action data");

		hr = WcaDoDeferredAction(L"MessageQueuingRollbackUninstall", pwzRollbackActionData, 0);
		ExitOnFailure(hr, "Failed to schedule MessageQueuingRollbackUninstall");

		// schedule execute action
		hr = MqiMessageQueuePermissionUninstall(&lstMessageQueuePermissions, &pwzExecuteActionData);
		ExitOnFailure(hr, "Failed to add message queue permissions to execute action data");

		hr = MqiMessageQueueUninstall(&lstMessageQueues, &pwzExecuteActionData);
		ExitOnFailure(hr, "Failed to add message queues to execute action data");
		iCost += lstMessageQueues.iUninstallCount * COST_MESSAGE_QUEUE_DELETE;

		hr = WcaDoDeferredAction(L"MessageQueuingExecuteUninstall", pwzExecuteActionData, iCost);
		ExitOnFailure(hr, "Failed to schedule MessageQueuingExecuteUninstall");
	}

	hr = S_OK;

LExit:
	// clean up
	MqiMessageQueueFreeList(&lstMessageQueues);
	MqiMessageQueuePermissionFreeList(&lstMessageQueuePermissions);

	ReleaseStr(pwzRollbackActionData);
	ReleaseStr(pwzExecuteActionData);

	er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}

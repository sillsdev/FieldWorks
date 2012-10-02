//-------------------------------------------------------------------------------------------------
// <copyright file="serviceconfig.cpp" company="Microsoft">
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
//    Code to configure services when the installer cannot.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// prototype
SC_ACTION_TYPE GetSCActionType(__in LPCWSTR pwzActionTypeName);
HRESULT GetSCActionTypeString(__in SC_ACTION_TYPE type, __out_ecount(cchLen) LPWSTR pwzActionTypeString, __in DWORD cchLen);

// structs
LPCWSTR wzQUERY_SERVICECONFIG = L"SELECT `ServiceName`, `Component_`, `NewService`, `FirstFailureActionType`, `SecondFailureActionType`, `ThirdFailureActionType`, `ResetPeriodInDays`, `RestartServiceDelayInSeconds`, `ProgramCommandLine`, `RebootMessage` FROM `ServiceConfig`";
enum eQUERY_SERVICECONFIG { QSC_SERVICENAME = 1, QSC_COMPONENT, QSC_NEWSERVICE, QSC_FIRSTFAILUREACTIONTYPE, QSC_SECONDFAILUREACTIONTYPE, QSC_THIRDFAILUREACTIONTYPE, QSC_RESETPERIODINDAYS, QSC_RESTARTSERVICEDELAYINSECONDS, QSC_PROGRAMCOMMANDLINE, QSC_REBOOTMESSAGE };

// consts
LPCWSTR c_wzActionTypeNone = L"none";
LPCWSTR c_wzActionTypeReboot = L"reboot";
LPCWSTR c_wzActionTypeRestart = L"restart";
LPCWSTR c_wzActionTypeRunCommand = L"runCommand";

/******************************************************************
 CaSchedServiceConfig - entry point for CaSchedServiceConfig Custom Action

 called as Type 1 CustomAction (binary DLL) from Windows Installer
 in InstallExecuteSequence before CaExecServiceConfig
********************************************************************/
extern "C" UINT __stdcall SchedServiceConfig(
	__in MSIHANDLE hInstall
	)
{
//	AssertSz(FALSE, "debug SchedServiceConfig");
	HRESULT hr = S_OK;
	UINT uiResult = ERROR_SUCCESS;
	DWORD dwError = 0;

	LPWSTR pwzData = NULL;
	int iData = 0;
	BOOL fExistingService = FALSE;

	PMSIHANDLE hView = NULL;
	PMSIHANDLE hRec = NULL;

	INSTALLSTATE isInstalled;
	INSTALLSTATE isAction;

	SC_HANDLE hSCM = NULL;
	SC_HANDLE hService = NULL;

	LPSERVICE_FAILURE_ACTIONSW psfa;

	LPWSTR pwzCustomActionData = NULL;
	LPWSTR pwzRollbackCustomActionData = NULL;

	DWORD cServices = 0;

	DWORD dwRestartDelay = 0;
	WCHAR wzActionName[32] = { 0 };

	DWORD dwSizeNeeded = 0;

	// initialize
	hr = WcaInitialize(hInstall, "SchedServiceConfig");
	ExitOnFailure(hr, "failed to initialize");

	//Get a handle to the service control manager
	hSCM = ::OpenSCManagerW(NULL, NULL, SC_MANAGER_CONNECT);
	if (hSCM == NULL)
		ExitOnLastError(hr, "failed to get handle to SCM");

	// loop through all the services to be configured
	hr = WcaOpenExecuteView(wzQUERY_SERVICECONFIG, &hView);
	ExitOnFailure(hr, "failed to open view on ServiceConfig table");

	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		hr = WcaGetRecordInteger(hRec, QSC_NEWSERVICE, &iData);
		ExitOnFailure(hr, "failed to get object NewService");

		fExistingService = 1 != iData;

		// Get component name
		hr = WcaGetRecordString(hRec, QSC_COMPONENT, &pwzData);
		ExitOnFailure(hr, "failed to get component name");

		// check if we are installing this Component
		hr = ::MsiGetComponentStateW(hInstall, pwzData, &isInstalled, &isAction);
		ExitOnFailure1(hr = HRESULT_FROM_WIN32(hr), "failed to get install state for Component: %S", pwzData);

		// We want to configure either a service we're installing or one already on the box
		if (WcaIsInstalling(isInstalled, isAction))
		{
			// Check if we're configuring an existing service
			if (fExistingService)
			{
				// Confirm the service is actually on the box
				hr = WcaGetRecordFormattedString(hRec, QSC_SERVICENAME, &pwzData);
				ExitOnFailure(hr, "failed to get object NewService");

				//Get a handle to the service
				hService = ::OpenServiceW(hSCM, pwzData, SERVICE_QUERY_CONFIG);
				if (hService == NULL)
				{
					dwError = ::GetLastError();
					hr = HRESULT_FROM_WIN32(dwError);
					if (hr == ERROR_SERVICE_DOES_NOT_EXIST)
					{
						ExitOnFailure1(hr, "Service \"%s\" does not exist on this system.", pwzData);
					}
					else
					{
						ExitOnFailure1(hr, "Failed to get handle to the service \"%S\".", pwzData);
					}
				}

				// Get Current Service Config info
				if(!::QueryServiceConfig2W(hService, SERVICE_CONFIG_FAILURE_ACTIONS, NULL, 0, &dwSizeNeeded) && ERROR_INSUFFICIENT_BUFFER != ::GetLastError())
				{
					ExitOnLastError(hr, "Failed to get current service config info.");
				}

				// Alloc space we were told we needed
				psfa = (LPSERVICE_FAILURE_ACTIONSW) MemAlloc(dwSizeNeeded, TRUE);
				ExitOnNull(psfa, hr, E_OUTOFMEMORY, "failed to allocate memory for service failure actions.");

				// Now do the real query
				if (!::QueryServiceConfig2W(hService, SERVICE_CONFIG_FAILURE_ACTIONS, (LPBYTE)psfa, dwSizeNeeded, &dwSizeNeeded))
					ExitOnLastError(hr, "failed to Query Service.");

				// Build up rollback CA data so we can restore service state if necessary
				hr = WcaWriteStringToCaData(pwzData, &pwzRollbackCustomActionData);
				ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");

				// If this service struct is empty, fill in defualt values
				if(psfa->cActions < 3)
				{
					hr = WcaWriteStringToCaData(c_wzActionTypeNone, &pwzRollbackCustomActionData);
					ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");

					hr = WcaWriteStringToCaData(c_wzActionTypeNone, &pwzRollbackCustomActionData);
					ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");

					hr = WcaWriteStringToCaData(c_wzActionTypeNone, &pwzRollbackCustomActionData);
					ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");
				}
				else
				{
					// psfa actually had actions defined, so use them

					// action 1
					hr = GetSCActionTypeString(psfa->lpsaActions[0].Type, (LPWSTR)wzActionName, 32);
					ExitOnFailure(hr, "failed to query SFA object");

					if (SC_ACTION_RESTART == psfa->lpsaActions[0].Type)
						dwRestartDelay = psfa->lpsaActions[0].Delay / 1000;

					hr = WcaWriteStringToCaData(wzActionName, &pwzRollbackCustomActionData);
					ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");

					// action 2
					hr = GetSCActionTypeString(psfa->lpsaActions[1].Type, (LPWSTR)wzActionName, 32);
					ExitOnFailure(hr, "failed to query SFA object");

					if (SC_ACTION_RESTART == psfa->lpsaActions[1].Type)
						dwRestartDelay = psfa->lpsaActions[1].Delay / 1000;

					hr = WcaWriteStringToCaData(wzActionName, &pwzRollbackCustomActionData);
					ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");

					// action 3
					hr = GetSCActionTypeString(psfa->lpsaActions[2].Type, (LPWSTR)wzActionName, 32);
					ExitOnFailure(hr, "failed to query SFA object");

					if (SC_ACTION_RESTART == psfa->lpsaActions[2].Type)
						dwRestartDelay = psfa->lpsaActions[2].Delay / 1000;

					hr = WcaWriteStringToCaData(wzActionName, &pwzRollbackCustomActionData);
					ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");
				}

				hr = WcaWriteIntegerToCaData(psfa->dwResetPeriod / (24 * 60 * 60), &pwzRollbackCustomActionData);
				ExitOnFailure(hr, "failed to add data to CustomActionData");

				hr = WcaWriteIntegerToCaData(dwRestartDelay, &pwzRollbackCustomActionData);
				ExitOnFailure(hr, "failed to add data to CustomActionData");

				// check for value being null
				if(!psfa->lpCommand)
					psfa->lpCommand = L"";
				hr = WcaWriteStringToCaData(psfa->lpCommand, &pwzRollbackCustomActionData);
				ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");

				// check for value being null
				if(!psfa->lpRebootMsg)
					psfa->lpRebootMsg = L"";
				hr = WcaWriteStringToCaData(psfa->lpRebootMsg, &pwzRollbackCustomActionData);
				ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");

				// Clear up per-service values
				if(psfa)
					MemFree(psfa);
			}

			// add the data to the CustomActionData (for install)
			hr = WcaGetRecordFormattedString(hRec, QSC_SERVICENAME, &pwzData);
			ExitOnFailure(hr, "failed to get name of service");
			hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
			ExitOnFailure(hr, "failed to add data to CustomActionData");

			hr = WcaGetRecordString(hRec, QSC_FIRSTFAILUREACTIONTYPE, &pwzData);
			ExitOnFailure(hr, "failed to get first failure action type");
			hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
			ExitOnFailure(hr, "failed to add data to CustomActionData");

			hr = WcaGetRecordString(hRec, QSC_SECONDFAILUREACTIONTYPE, &pwzData);
			ExitOnFailure(hr, "failed to get second failure action type");
			hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
			ExitOnFailure(hr, "failed to add data to CustomActionData");

			hr = WcaGetRecordString(hRec, QSC_THIRDFAILUREACTIONTYPE, &pwzData);
			ExitOnFailure(hr, "failed to get third failure action type");
			hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
			ExitOnFailure(hr, "failed to add data to CustomActionData");

			hr = WcaGetRecordInteger(hRec, QSC_RESETPERIODINDAYS, &iData);
			if (hr == S_FALSE) // deal w/ possible null value
				iData = 0;
			ExitOnFailure(hr, "failed to get reset period in days between service restart attempts.");
			hr = WcaWriteIntegerToCaData(iData, &pwzCustomActionData);
			ExitOnFailure(hr, "failed to add data to CustomActionData");

			hr = WcaGetRecordInteger(hRec, QSC_RESTARTSERVICEDELAYINSECONDS, &iData);
			if (hr == S_FALSE) // deal w/ possible null value
				iData = 0;
			ExitOnFailure(hr, "failed to get server restart delay value.");
			hr = WcaWriteIntegerToCaData(iData, &pwzCustomActionData);
			ExitOnFailure(hr, "failed to add data to CustomActionData");

			hr = WcaGetRecordString(hRec, QSC_PROGRAMCOMMANDLINE, &pwzData); // null value already dealt w/ properly
			ExitOnFailure(hr, "failed to get command line to run on service failure.");
			hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
			ExitOnFailure(hr, "failed to add data to CustomActionData");

			hr = WcaGetRecordString(hRec, QSC_REBOOTMESSAGE, &pwzData); // null value already dealt w/ properly
			ExitOnFailure(hr, "failed to get message to send to users when server reboots due to service failure.");
			hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
			ExitOnFailure(hr, "failed to add data to CustomActionData");

			cServices++;
			::CloseServiceHandle(hService);
			hService = NULL;
		}
	}

	// if we looped through all records all is well
	if (E_NOMOREITEMS == hr)
		hr = S_OK;
	ExitOnFailure(hr, "failed while looping through all objects to secure");

	// setup CustomActionData and add to progress bar for download
	if (pwzRollbackCustomActionData && *pwzRollbackCustomActionData)
	{
		Assert(0 < cServices);

		hr = WcaDoDeferredAction(L"ExecServiceConfigRollback", pwzRollbackCustomActionData, cServices * COST_SERVICECONFIG);
		ExitOnFailure(hr, "failed to schedule ExecSecureObjects action");
	}

	// schedule the custom action and add to progress bar
	if (pwzCustomActionData && *pwzCustomActionData)
	{
		Assert(0 < cServices);

		hr = WcaDoDeferredAction(L"ExecServiceConfig", pwzCustomActionData, cServices * COST_SERVICECONFIG);
		ExitOnFailure(hr, "failed to schedule ExecSecureObjects action");
	}

LExit:
	// Clean up handles
	if (hService != NULL)
		::CloseServiceHandle(hService);
	if (hSCM != NULL)
		::CloseServiceHandle(hSCM);

	ReleaseStr(pwzCustomActionData);
	ReleaseStr(pwzRollbackCustomActionData);
	ReleaseStr(pwzData);

	if (FAILED(hr))
		uiResult = ERROR_INSTALL_FAILURE;
	return WcaFinalize(uiResult);
}


/******************************************************************
 CaExecServiceConfig - entry point for ServiceConfig Custom Action
				   called as Type 1025 CustomAction (deferred binary DLL)

 NOTE: deferred CustomAction since it modifies the machine
 NOTE: CustomActionData == wzServiceName\twzFirstFailureActionType\twzSecondFailureActionType\twzThirdFailureActionType\tdwResetPeriodInDays\tdwRestartServiceDelayInSeconds\twzProgramCommandLine\twzRebootMessage\twzServiceName\t...
*******************************************************************/
extern "C" UINT __stdcall ExecServiceConfig(
	__in MSIHANDLE hInstall
	)
{
//	AssertSz(FALSE, "debug ExecServiceConfig");
	HRESULT hr = S_OK;
	UINT uiResult = ERROR_SUCCESS;
	DWORD dwError = 0;
	LPVOID lpMsgBuf = NULL;

	LPWSTR pwzData = NULL;
	LPWSTR pwz = NULL;

	LPWSTR pwzServiceName = NULL;
	LPWSTR pwzFirstFailureActionType = NULL;
	LPWSTR pwzSecondFailureActionType = NULL;
	LPWSTR pwzThirdFailureActionType = NULL;
	LPWSTR pwzProgramCommandLine = NULL;
	LPWSTR pwzRebootMessage = NULL;
	DWORD dwResetPeriodInDays = 0;
	DWORD dwRestartServiceDelayInSeconds = 0;

	SC_HANDLE hSCM = NULL;
	SC_HANDLE hService = NULL;
	DWORD dwOpenServiceAccess = SERVICE_CHANGE_CONFIG; // SERVICE_CHANGE_CONFIG is needed for ChangeServiceConfig2()

	SERVICE_FAILURE_ACTIONSW sfa;
	SC_ACTION actions[3];
	BOOL fResult = FALSE;

	// initialize
	hr = WcaInitialize(hInstall, "ExecServiceConfig");
	ExitOnFailure(hr, "failed to initialize");

	hr = WcaGetProperty( L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzData);

	pwz = pwzData;

	// loop through all the passed in data
	while (pwz && *pwz)
	{
		hr = WcaReadStringFromCaData(&pwz, &pwzServiceName);
		ExitOnFailure(hr, "failed to process CustomActionData");
		hr = WcaReadStringFromCaData(&pwz, &pwzFirstFailureActionType);
		ExitOnFailure(hr, "failed to process CustomActionData");
		hr = WcaReadStringFromCaData(&pwz, &pwzSecondFailureActionType);
		ExitOnFailure(hr, "failed to process CustomActionData");
		hr = WcaReadStringFromCaData(&pwz, &pwzThirdFailureActionType);
		ExitOnFailure(hr, "failed to process CustomActionData");
		hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&dwResetPeriodInDays));
		ExitOnFailure(hr, "failed to process CustomActionData");
		hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&dwRestartServiceDelayInSeconds));
		ExitOnFailure(hr, "failed to process CustomActionData");
		hr = WcaReadStringFromCaData(&pwz, &pwzProgramCommandLine);
		ExitOnFailure(hr, "failed to process CustomActionData");
		hr = WcaReadStringFromCaData(&pwz, &pwzRebootMessage);
		ExitOnFailure(hr, "failed to process CustomActionData");

		WcaLog(LOGMSG_VERBOSE, "Configuring Service: %S", pwzServiceName);

		// build up SC_ACTION array
		// TODO: why is delay only respected when SC_ACTION_RESTART is requested?
		actions[0].Type = GetSCActionType(pwzFirstFailureActionType);
		actions[0].Delay = 0;
		if (SC_ACTION_RESTART == actions[0].Type)
		{
			actions[0].Delay = dwRestartServiceDelayInSeconds * 1000; // seconds to milliseconds
			dwOpenServiceAccess |= SERVICE_START; // must have SERVICE_START access in order to handle SC_ACTION_RESTART action;
		}

		actions[1].Type = GetSCActionType(pwzSecondFailureActionType);
		actions[1].Delay = 0;
		if (SC_ACTION_RESTART == actions[1].Type)
		{
			actions[1].Delay = dwRestartServiceDelayInSeconds * 1000; // seconds to milliseconds
			dwOpenServiceAccess |= SERVICE_START; // must have SERVICE_START access in order to handle SC_ACTION_RESTART action;
		}

		actions[2].Type = GetSCActionType(pwzThirdFailureActionType);
		actions[2].Delay = 0;
		if (SC_ACTION_RESTART == actions[2].Type)
		{
			actions[2].Delay = dwRestartServiceDelayInSeconds * 1000; // seconds to milliseconds
			dwOpenServiceAccess |= SERVICE_START; // must have SERVICE_START access in order to handle SC_ACTION_RESTART action;
		}

		// build up the SERVICE_FAILURE_ACTIONSW struct
		sfa.dwResetPeriod = dwResetPeriodInDays * 24 * 60 * 60; // days to seconds
		sfa.lpRebootMsg = pwzRebootMessage;
		sfa.lpCommand = pwzProgramCommandLine;
		sfa.cActions = 3;  // the UI always shows 3 actions, so we'll always do 3
		sfa.lpsaActions = actions;

		// Get a handle to the service control manager (if we don't already have)
		if (NULL == hSCM)
		{
			hSCM = ::OpenSCManagerW(NULL, NULL, SC_MANAGER_CONNECT);
			if (hSCM == NULL)
			{
				dwError = ::GetLastError();
				::FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL, dwError, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPWSTR)&lpMsgBuf, 0, NULL);

				ExitOnFailure1(hr = HRESULT_FROM_WIN32(dwError), "failed to get handle to SCM. Error: %S", (LPWSTR)lpMsgBuf);
			}
		}

		hService = ::OpenServiceW(hSCM, pwzServiceName, dwOpenServiceAccess);
		if (hService == NULL)
		{
			dwError = ::GetLastError();
			hr = HRESULT_FROM_WIN32(dwError);
			if (dwError == ERROR_SERVICE_DOES_NOT_EXIST)
			{
				ExitOnFailure1(hr, "Service \"%S\" does not exist on this system.", pwzServiceName);
			}
			else
			{
				::FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL, dwError, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPWSTR)&lpMsgBuf, 0, NULL);
				ExitOnFailure2(hr, "Failed to get handle to the service \"%S\". Error: %S", pwzServiceName, (LPWSTR)lpMsgBuf);
			}
		}

		// Call ChangeServiceConfig2 to actually set up the failure actions
		fResult = ChangeServiceConfig2W(hService, SERVICE_CONFIG_FAILURE_ACTIONS, (LPVOID)&sfa);
		if (fResult == FALSE)
		{
			dwError = ::GetLastError();
			hr = HRESULT_FROM_WIN32(dwError);
			::FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL, dwError, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPWSTR)&lpMsgBuf, 0, NULL);

			// check if this is a service that can't be modified
			if(dwError == ERROR_CANNOT_DETECT_PROCESS_ABORT)
			{
				WcaLog(LOGMSG_STANDARD, "WARNING: Service \"%S\" is not configurable on this server and will not be set.", pwzServiceName);
			}
			ExitOnFailure1(hr, "Cannot change service configuration. Error: %S", (LPWSTR)lpMsgBuf);
		}

		// Per-service cleanup
		dwResetPeriodInDays = 0;
		dwRestartServiceDelayInSeconds = 0;

		hr = WcaProgressMessage(COST_SERVICECONFIG, FALSE);
		ExitOnFailure(hr, "failed to send progress message");
	}

LExit:
	// Clean up handles
	ReleaseStr(pwzServiceName);
	ReleaseStr(pwzFirstFailureActionType);
	ReleaseStr(pwzSecondFailureActionType);
	ReleaseStr(pwzThirdFailureActionType);
	ReleaseStr(pwzProgramCommandLine);
	ReleaseStr(pwzRebootMessage);
	ReleaseStr(pwzData);

	if (lpMsgBuf) // Allocated with FormatString
		::LocalFree(lpMsgBuf);

	if (hService)
		::CloseServiceHandle(hService);
	if (hSCM)
		::CloseServiceHandle(hSCM);

	if (FAILED(hr))
		uiResult = ERROR_INSTALL_FAILURE;
	return WcaFinalize(uiResult);
}

/**********************************************************
GetSCActionType - helper function to return the SC_ACTION_TYPE
				  for a given string matching the allowed set.
				  REBOOT, RESTART, RUN_COMMAND and NONE
**********************************************************/
SC_ACTION_TYPE GetSCActionType(LPCWSTR pwzActionTypeName)
{
	SC_ACTION_TYPE actionType;

	// verify that action types are valid. if not, just default to NONE
	if (0 == lstrcmpiW(c_wzActionTypeReboot, pwzActionTypeName))
	{
		actionType = SC_ACTION_REBOOT;
	}
	else if (0 == lstrcmpiW(c_wzActionTypeRestart, pwzActionTypeName))
	{
		actionType = SC_ACTION_RESTART;
	}
	else if (0 == lstrcmpiW(c_wzActionTypeRunCommand, pwzActionTypeName))
	{
		actionType = SC_ACTION_RUN_COMMAND;
	}
	else
	{
		// default to none
		actionType = SC_ACTION_NONE;
	}

	return actionType;
}

HRESULT GetSCActionTypeString(__in SC_ACTION_TYPE type, __out_ecount(cchLen) LPWSTR pwzActionTypeString, __in DWORD cchLen)
{
	HRESULT hr = S_OK;

	if(cchLen < 15)
	{
		// todo: find correct error code here.
		hr = E_UNEXPECTED;
		return hr;
	}

	switch(type)
	{
	case SC_ACTION_REBOOT:
		hr = StringCchCopyW(pwzActionTypeString, cchLen, c_wzActionTypeReboot);
		ExitOnFailure(hr, "Failed to copy 'reboot' into action type.");
		break;
	case SC_ACTION_RESTART:
		hr = StringCchCopyW(pwzActionTypeString, cchLen, c_wzActionTypeRestart);
		ExitOnFailure(hr, "Failed to copy 'restart' into action type.");
		break;
	case SC_ACTION_RUN_COMMAND:
		hr = StringCchCopyW(pwzActionTypeString, cchLen, c_wzActionTypeRunCommand);
		ExitOnFailure(hr, "Failed to copy 'runCommand' into action type.");
		break;
	case SC_ACTION_NONE:
		hr = StringCchCopyW(pwzActionTypeString, cchLen, c_wzActionTypeNone);
		ExitOnFailure(hr, "Failed to copy 'none' into action type.");
		break;
	default:
		break;
	}

LExit:

	return hr;
}

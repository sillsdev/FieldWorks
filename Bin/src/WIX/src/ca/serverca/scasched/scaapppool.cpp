//-------------------------------------------------------------------------------------------------
// <copyright file="scaapppool.cpp" company="Microsoft">
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
//    Application pool functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

/*------------------------------------------------------------------
AppPool table:

Column           Type   Nullable     Example Value
AppPool          s72    No           TestPool
Name             s72    No           "TestPool"
Component_       s72    No           ComponentName
Attributes       i2     No           8 (APATTR_OTHERUSER)
User_            s72    Yes          UserKey
RecycleMinutes   i2     Yes          500
RecycleRequests  i2     Yes          5000
RecycleTimes     s72    Yes          "1:45,13:30,22:00"
IdleTimeout      i2     Yes          15
QueueLimit       i2     Yes          500
CPUMon           s72    Yes          "65,500,1" (65% CPU usage, 500 minutes, Shutdown Action)
MaxProc          i2     Yes          5

Notes:
RecycleTimes is a comma delimeted list of times.  CPUMon is a
comma delimeted list of the following format:
<percent CPU usage>,<refress minutes>,<Action>.  The values for
Action are 1 (Shutdown) and 0 (No Action).

------------------------------------------------------------------*/

// sql queries
LPCWSTR vcsAppPoolQuery = L"SELECT `AppPool`, `Name`, `Component_`, `Attributes`, `User_`, `RecycleMinutes`, `RecycleRequests`, `RecycleTimes`, `VirtualMemory`, `PrivateMemory`, `IdleTimeout`, `QueueLimit`, `CPUMon`, `MaxProc` FROM `IIsAppPool`";
enum eAppPoolQuery { apqAppPool = 1, apqName, apqComponent, apqAttributes, apqUser, apqRecycleMinutes, apqRecycleRequests, apqRecycleTimes, apqVirtualMemory, apqPrivateMemory, apqIdleTimeout, apqQueueLimit, apqCpuMon, apqMaxProc };


// prototypes
static HRESULT AppPoolExists(
	__in IMSAdminBase* piMetabase,
	__in LPCWSTR wzAppPool
	);

static HRESULT AddAppPoolToList(
	__in SCA_APPPOOL** ppsapList
	);


// functions

void ScaAppPoolFreeList(
	__in SCA_APPPOOL* psapList
	)
{
	SCA_APPPOOL* psapDelete = psapList;
	while (psapList)
	{
		psapDelete = psapList;
		psapList = psapList->psapNext;

		MemFree(psapDelete);
	}
}


HRESULT ScaAppPoolRead(
	__inout SCA_APPPOOL** ppsapList
	)
{
	Assert(ppsapList);

	HRESULT hr = S_OK;
	UINT uiResult = ERROR_SUCCESS;

	PMSIHANDLE hView, hRec;
	LPWSTR pwzData = NULL;
	INSTALLSTATE isInstalled = INSTALLSTATE_UNKNOWN;
	INSTALLSTATE isAction = INSTALLSTATE_UNKNOWN;
	SCA_APPPOOL* psap = NULL;

	if (S_OK != WcaTableExists(L"IIsAppPool"))
	{
		WcaLog(LOGMSG_VERBOSE, "Skipping ScaInstallAppPool() - required table not present");
		ExitFunction1(hr = S_FALSE);
	}

	// loop through all the AppPools
	hr = WcaOpenExecuteView(vcsAppPoolQuery, &hView);
	ExitOnFailure(hr, "failed to open view on IIsAppPool table");
	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		BOOL fHasComponent = FALSE;

		// Get the Component first.  If there is a Component and it is not being modified during
		// this transaction, skip processing this whole record.
		hr = WcaGetRecordString(hRec, apqComponent, &pwzData);
		ExitOnFailure(hr, "failed to get AppPool.Component");

		if (pwzData && *pwzData)
		{
			fHasComponent = TRUE;

			uiResult = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &isInstalled, &isAction);
			hr = HRESULT_FROM_WIN32(uiResult);
			ExitOnFailure(hr, "Failed to get AppPool.Component state");

			if (!WcaIsInstalling(isInstalled, isAction) &&
				!WcaIsReInstalling(isInstalled, isAction) &&
				!WcaIsUninstalling(isInstalled, isAction))
			{
				continue; // skip this record.
			}
		}

		// Add this record's information into the list of things to process.
		hr = AddAppPoolToList(ppsapList);
		ExitOnFailure(hr, "failed to add app pool to app pool list");

		psap = *ppsapList;

		psap->fHasComponent = fHasComponent;
		if (psap->fHasComponent)
		{
			hr = StringCchCopyW(psap->wzComponent, countof(psap->wzComponent), pwzData);
			ExitOnFailure1(hr, "failed to copy component name: %S", pwzData);

			psap->isInstalled = isInstalled;
			psap->isAction = isAction;
		}

		hr = WcaGetRecordString(hRec, apqAppPool, &pwzData);
		ExitOnFailure(hr, "failed to get AppPool.AppPool");
		hr = StringCchCopyW(psap->wzAppPool, countof(psap->wzAppPool), pwzData);
		ExitOnFailure1(hr, "failed to copy AppPool name: %S", pwzData);

		hr = WcaGetRecordFormattedString(hRec, apqName, &pwzData);
		ExitOnFailure(hr, "failed to get AppPool.Name");
		hr = StringCchCopyW(psap->wzName, countof(psap->wzName), pwzData);
		ExitOnFailure1(hr, "failed to copy app pool name: %S", pwzData);
		hr = StringCchPrintfW(psap->wzKey, countof(psap->wzKey), L"/LM/W3SVC/AppPools/%s", pwzData);
		ExitOnFailure(hr, "failed to format app pool key name");

		hr = WcaGetRecordInteger(hRec, apqAttributes, &psap->iAttributes);
		ExitOnFailure(hr, "failed to get AppPool.Attributes");

		hr = WcaGetRecordString(hRec, apqUser, &pwzData);
		ExitOnFailure(hr, "failed to get AppPool.User");
		hr = ScaGetUser(pwzData, &psap->suUser);
		ExitOnFailure1(hr, "failed to get user: %S", pwzData);

		hr = WcaGetRecordInteger(hRec, apqRecycleRequests, &psap->iRecycleRequests);
		ExitOnFailure(hr, "failed to get AppPool.RecycleRequests");

		hr = WcaGetRecordInteger(hRec, apqRecycleMinutes, &psap->iRecycleMinutes);
		ExitOnFailure(hr, "failed to get AppPool.Minutes");

		hr = WcaGetRecordString(hRec, apqRecycleTimes, &pwzData);
		ExitOnFailure(hr, "failed to get AppPool.RecycleTimes");
		hr = StringCchCopyW(psap->wzRecycleTimes, countof(psap->wzRecycleTimes), pwzData);
		ExitOnFailure1(hr, "failed to copy recycle value: %S", pwzData);

		hr = WcaGetRecordInteger(hRec, apqVirtualMemory, &psap->iVirtualMemory);
		ExitOnFailure(hr, "failed to get AppPool.VirtualMemory");

		hr = WcaGetRecordInteger(hRec, apqPrivateMemory, &psap->iPrivateMemory);
		ExitOnFailure(hr, "failed to get AppPool.PrivateMemory");

		hr = WcaGetRecordInteger(hRec, apqIdleTimeout, &psap->iIdleTimeout);
		ExitOnFailure(hr, "failed to get AppPool.IdleTimeout");

		hr = WcaGetRecordInteger(hRec, apqQueueLimit, &psap->iQueueLimit);
		ExitOnFailure(hr, "failed to get AppPool.QueueLimit");

		hr = WcaGetRecordString(hRec, apqCpuMon, &pwzData);
		ExitOnFailure(hr, "failed to get AppPool.CPUMon");
		hr = StringCchCopyW(psap->wzCpuMon, countof(psap->wzCpuMon), pwzData);
		ExitOnFailure1(hr, "failed to copy cpu monitor value: %S", pwzData);

		hr = WcaGetRecordInteger(hRec, apqMaxProc, &psap->iMaxProcesses);
		ExitOnFailure(hr, "failed to get AppPool.MaxProc");
	}

	if (E_NOMOREITEMS == hr)
	{
		hr = S_OK;
	}
	ExitOnFailure(hr, "failure while processing AppPools");

LExit:
	ReleaseStr(pwzData);
	return hr;
}


HRESULT ScaFindAppPool(
	__in IMSAdminBase* piMetabase,
	__in LPCWSTR wzAppPool,
	__out_ecount(cchName) LPWSTR wzName,
	__in DWORD cchName,
	__in SCA_APPPOOL *psapList
	)
{
	Assert(piMetabase && wzAppPool && *wzAppPool && wzName && *wzName);

	HRESULT hr = S_OK;

	// check memory first
	SCA_APPPOOL* psap = psapList;
	for (; psap; psap = psap->psapNext)
	{
		if (0 == lstrcmpW(psap->wzAppPool, wzAppPool))
		{
			break;
		}
	}
	ExitOnNull1(psap, hr, HRESULT_FROM_WIN32(ERROR_NOT_FOUND), "Could not find the app pool: %S", wzAppPool);

	// copy the web app pool name
	hr = StringCchCopyW(wzName, cchName, psap->wzName);
	ExitOnFailure1(hr, "failed to copy app pool name: %s", psap->wzName);

	// if it's not being installed now, check if it exists already
	if (!psap->fHasComponent)
	{
		hr = AppPoolExists(piMetabase, psap->wzName);
		ExitOnFailure1(hr, "failed to check for existence of app pool: %S", psap->wzName);
	}

LExit:
	return hr;
}


static HRESULT AppPoolExists(
	__in IMSAdminBase* piMetabase,
	__in LPCWSTR wzAppPool
	)
{
	Assert(piMetabase && wzAppPool && *wzAppPool);

	HRESULT hr = S_OK;
	WCHAR wzSubKey[METADATA_MAX_NAME_LEN];

	for (DWORD dwIndex = 0; SUCCEEDED(hr); dwIndex++)
	{
		hr = piMetabase->EnumKeys(METADATA_MASTER_ROOT_HANDLE, L"/LM/W3SVC/AppPools", wzSubKey, dwIndex);
		if (SUCCEEDED(hr) && 0 == lstrcmpW(wzSubKey, wzAppPool))
		{
			hr = S_OK;
			break;
		}
	}

	if (E_NOMOREITEMS == hr)
	{
		hr = S_FALSE;
	}

	return hr;
}


HRESULT ScaAppPoolInstall(
	__in IMSAdminBase* piMetabase,
	__in SCA_APPPOOL* psapList
	)
{
	Assert(piMetabase);

	HRESULT hr = S_OK;

	for (SCA_APPPOOL* psap = psapList; psap; psap = psap->psapNext)
	{
		// if we are installing the web site
		if (psap->fHasComponent && WcaIsInstalling(psap->isInstalled, psap->isAction))
		{
			hr = ScaWriteAppPool(piMetabase, psap);
			ExitOnFailure1(hr, "failed to write AppPool '%S' to metabase", psap->wzAppPool);
		}
	}

LExit:
	return hr;
}


HRESULT ScaAppPoolUninstall(
	__in IMSAdminBase* piMetabase,
	__in SCA_APPPOOL* psapList
	)
{
	Assert(piMetabase);

	HRESULT hr = S_OK;

	for (SCA_APPPOOL* psap = psapList; psap; psap = psap->psapNext)
	{
		// if we are uninstalling the web site
		if (psap->fHasComponent && WcaIsUninstalling(psap->isInstalled, psap->isAction))
		{
			hr = ScaRemoveAppPool(piMetabase, psap);
			ExitOnFailure1(hr, "Failed to remove AppPool '%S' from metabase", psap->wzAppPool);
		}
	}

LExit:
	return hr;
}


HRESULT ScaWriteAppPool(
	__in IMSAdminBase* piMetabase,
	__in SCA_APPPOOL* psap
	)
{
	Assert(piMetabase && psap);

	HRESULT hr = S_OK;
	DWORD dwIdentity = -1;
	BOOL fExists = FALSE;
	LPWSTR pwzValue = NULL;
	LPWSTR wz = NULL;

	hr = AppPoolExists(piMetabase, psap->wzName);
	ExitOnFailure(hr, "failed to check if app pool already exists");
	if (S_FALSE == hr)
	{
		// didn't find the AppPool key, so we need to create it
		hr = ScaCreateMetabaseKey(piMetabase, psap->wzKey, L"");
		ExitOnFailure1(hr, "failed to create AppPool key: %S", psap->wzKey);

		// mark it as an AppPool
		hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_KEY_TYPE, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)L"IIsApplicationPool");
		ExitOnFailure1(hr, "failed to mark key as AppPool key: %S", psap->wzKey);

		// TODO: Make this an Attribute?
		// set autostart value
		hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_APPPOOL_AUTO_START, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)1);
		ExitOnFailure1(hr, "failed to mark key as AppPool key: %S", psap->wzKey);
	}
	else
	{
		fExists = TRUE;
	}

	//
	// Set the AppPool Recycling Tab
	//
	if (MSI_NULL_INTEGER != psap->iRecycleMinutes)
	{
		hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_APPPOOL_PERIODIC_RESTART_TIME, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)psap->iRecycleMinutes));
		ExitOnFailure(hr, "failed to set periodic restart time");
	}

	if (MSI_NULL_INTEGER != psap->iRecycleRequests)
	{
		hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_APPPOOL_PERIODIC_RESTART_REQUEST_COUNT, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)psap->iRecycleRequests));
		ExitOnFailure(hr, "failed to set periodic restart request count");
	}

	if (psap->wzRecycleTimes && *psap->wzRecycleTimes)
	{
		// Add another NULL' onto pwz since it's a 'MULTISZ'
		hr = StrAllocString(&pwzValue, psap->wzRecycleTimes, 0);
		ExitOnFailure(hr, "failed to allocate string for MULTISZ");
		hr = StrAllocConcat(&pwzValue, L"\0", 1);
		ExitOnFailure(hr, "failed to add second null to RecycleTime multisz");

		// Replace the commas with NULLs
		wz = pwzValue;
		while (NULL != (wz = wcschr(wz, L',')))
		{
			*wz = L'\0';
			wz++;
		}

		hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_APPPOOL_PERIODIC_RESTART_SCHEDULE, METADATA_INHERIT, IIS_MD_UT_SERVER, MULTISZ_METADATA, (LPVOID)pwzValue);
		ExitOnFailure(hr, "failed to set periodic restart schedule");
	}

	if (MSI_NULL_INTEGER != psap->iVirtualMemory)
	{
		hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_APPPOOL_PERIODIC_RESTART_MEMORY, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)psap->iVirtualMemory));
		ExitOnFailure(hr, "failed to set periodic restart memory count");
	}

	if (MSI_NULL_INTEGER != psap->iPrivateMemory)
	{
		hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_APPPOOL_PERIODIC_RESTART_PRIVATE_MEMORY, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)psap->iPrivateMemory));
		ExitOnFailure(hr, "failed to set periodic restart private memory count");
	}


	//
	// Set AppPool Performance Tab
	//
	if (MSI_NULL_INTEGER != psap->iIdleTimeout)
	{
		hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_APPPOOL_IDLE_TIMEOUT, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)psap->iIdleTimeout));
		ExitOnFailure(hr, "failed to set idle timeout value");
	}

	if (MSI_NULL_INTEGER != psap->iQueueLimit)
	{
		hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_APPPOOL_UL_APPPOOL_QUEUE_LENGTH, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)psap->iQueueLimit));
		ExitOnFailure(hr, "failed to set request queue limit value");
	}

	if(psap->wzCpuMon && *psap->wzCpuMon)
	{
		hr = StrAllocString(&pwzValue, psap->wzCpuMon, 0);
		ExitOnFailure(hr, "failed to allocate CPUMonitor string");

		DWORD dwPercent = 0;
		DWORD dwRefreshMinutes = 0;
		DWORD dwAction = 0;

		dwPercent = wcstoul(pwzValue, &wz, 10);
		if (100  < dwPercent)
			ExitOnFailure1(hr = E_INVALIDARG, "invalid maximum cpu percentage value: %d", dwPercent);
		if (wz && L',' == *wz)
		{
			wz++;
			dwRefreshMinutes = wcstoul(wz, &wz, 10);
			if (wz && L',' == *wz)
			{
				wz++;
				dwAction = wcstoul(wz, &wz, 10);
			}
		}

		if (dwPercent)
		{
			hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_CPU_LIMIT, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)(dwPercent * 1000)));
			ExitOnFailure(hr, "failed to set CPU percentage max");
		}
		if (dwRefreshMinutes)
		{
			hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_CPU_RESET_INTERVAL, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwRefreshMinutes));
			ExitOnFailure(hr, "failed to set refresh CPU minutes");
		}
		if (dwAction)
		{
			// 0 = No Action
			// 1 = Shutdown
			hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_CPU_ACTION, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwAction));
			ExitOnFailure(hr, "failed to set CPU action");
		}
	}

	if (MSI_NULL_INTEGER != psap->iMaxProcesses)
	{
		hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_APPPOOL_MAX_PROCESS_COUNT, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)psap->iMaxProcesses));
		ExitOnFailure(hr, "failed to set web garden maximum worker processes");
	}

	// TODO: Health Tab if anyone wants it?

	//
	// Set the AppPool Identity tab
	//
	if (psap->iAttributes & APATTR_NETSERVICE)
	{
		dwIdentity = MD_APPPOOL_IDENTITY_TYPE_NETWORKSERVICE;
	}
	else if (psap->iAttributes & APATTR_LOCSERVICE)
	{
		dwIdentity = MD_APPPOOL_IDENTITY_TYPE_LOCALSERVICE;
	}
	else if (psap->iAttributes & APATTR_LOCSYSTEM)
	{
		dwIdentity = MD_APPPOOL_IDENTITY_TYPE_LOCALSYSTEM;
	}
	else if (psap->iAttributes & APATTR_OTHERUSER)
	{
		dwIdentity = MD_APPPOOL_IDENTITY_TYPE_SPECIFICUSER;
	}

	if (-1 != dwIdentity)
	{
		hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_APPPOOL_IDENTITY_TYPE, METADATA_INHERIT , IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwIdentity));
		ExitOnFailure(hr, "failed to set app pool identity");

		if (MD_APPPOOL_IDENTITY_TYPE_SPECIFICUSER == dwIdentity)
		{
			if ( psap->suUser.wzDomain && *psap->suUser.wzDomain )
			{
				hr = StrAllocFormatted(&pwzValue, L"%s\\%s", psap->suUser.wzDomain, psap->suUser.wzName);
				ExitOnFailure2(hr, "failed to format user name: %S domain: %S", psap->suUser.wzName, psap->suUser.wzDomain);
			}
			else
			{
				hr = StrAllocFormatted(&pwzValue, L"%s", psap->suUser.wzName);
				ExitOnFailure1(hr, "failed to format user name: %S", psap->suUser.wzName);
			}

			hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_WAM_USER_NAME, METADATA_INHERIT , IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)pwzValue);
			ExitOnFailure(hr, "failed to set app pool identity name");

			hr = ScaWriteMetabaseValue(piMetabase, psap->wzKey, NULL, MD_WAM_PWD, METADATA_INHERIT | METADATA_SECURE, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)psap->suUser.wzPassword);
			ExitOnFailure(hr, "failed to set app pool identity password");
		}
	}

LExit:
	ReleaseNullStr(pwzValue);

	return hr;
}


HRESULT ScaRemoveAppPool(
	__in IMSAdminBase* piMetabase,
	__in SCA_APPPOOL* psap
	)
{
	Assert(piMetabase && psap);

	HRESULT hr = S_OK;

	// simply remove the root key and everything else is pulled at the same time
	hr = ScaDeleteMetabaseKey(piMetabase, psap->wzKey, L"");
	ExitOnFailure1(hr, "failed to delete AppPool key: %S", psap->wzKey);

	// TODO: Maybe check to make sure any web sites that are using this AppPool are put back in the 'DefaultAppPool'

LExit:
	return hr;
}


static HRESULT AddAppPoolToList(
	__in SCA_APPPOOL** ppsapList
	)
{
	HRESULT hr = S_OK;
	SCA_APPPOOL* psap = (SCA_APPPOOL*)MemAlloc(sizeof(**ppsapList), TRUE);
	ExitOnNull(psap, hr, E_OUTOFMEMORY, "failed to allocate memory for new element in app pool list");

	psap->psapNext = *ppsapList;
	*ppsapList = psap;

LExit:
	return hr;
}

//-------------------------------------------------------------------------------------------------
// <copyright file="scaexec.cpp" company="Microsoft">
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
//    Entry points into several server custom actions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


/********************************************************************
 DllMain - standard entry point for all WiX CustomActions

********************************************************************/
extern "C" BOOL WINAPI DllMain(
	__in HINSTANCE hInst,
	__in ULONG ulReason,
	__in LPVOID)
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
 StartMetabaseTransaction - CUSTOM ACTION ENTRY POINT for backing up metabase

  Input:  deferred CustomActionData - BackupName
********************************************************************/
extern "C" UINT __stdcall StartMetabaseTransaction(MSIHANDLE hInstall)
{
//AssertSz(FALSE, "debug here");
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	IMSAdminBase* piMetabase = NULL;
	LPWSTR pwzData = NULL;

	// initialize
	hr = WcaInitialize(hInstall, "StartMetabaseTransaction");
	ExitOnFailure(hr, "failed to initialize StartMetabaseTransaction");

	hr = ::CoInitialize(NULL);
	ExitOnFailure(hr, "failed to initialize COM");
	hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, (void**)&piMetabase);
	MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");

	hr = WcaGetProperty(L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	// back up the metabase
	Assert(lstrlenW(pwzData) < MD_BACKUP_MAX_LEN);

	// MD_BACKUP_OVERWRITE = Overwrite if a backup of the same name and version exists in the backup location
	hr = piMetabase->Backup(pwzData, MD_BACKUP_NEXT_VERSION, MD_BACKUP_OVERWRITE);
	MessageExitOnFailure1(hr, msierrIISFailedStartTransaction, "failed to begin metabase transaction: '%S'", pwzData);

	hr = WcaProgressMessage(COST_IIS_TRANSACTIONS, FALSE);
LExit:
	ReleaseStr(pwzData);
	ReleaseObject(piMetabase);

	::CoUninitialize();

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/********************************************************************
 RollbackMetabaseTransaction - CUSTOM ACTION ENTRY POINT for unbacking up metabase

  Input:  deferred CustomActionData - BackupName
********************************************************************/
extern "C" UINT __stdcall RollbackMetabaseTransaction(MSIHANDLE hInstall)
{
//AssertSz(FALSE, "debug here");
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	IMSAdminBase* piMetabase = NULL;
	LPWSTR pwzData = NULL;
	DWORD cchData = 0;

	hr = WcaInitialize(hInstall, "RollbackMetabaseTransaction");
	ExitOnFailure(hr, "failed to initialize");

	hr = ::CoInitialize(NULL);
	ExitOnFailure(hr, "failed to initialize COM");
	hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, (void**)&piMetabase);
	ExitOnFailure(hr, "failed to get IID_IIMSAdminBase object");


	hr = WcaGetProperty( L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	hr = piMetabase->Restore(pwzData, MD_BACKUP_HIGHEST_VERSION, 0);
	ExitOnFailure1(hr, "failed to rollback metabase transaction: '%S'", pwzData);

	hr = piMetabase->DeleteBackup(pwzData, MD_BACKUP_HIGHEST_VERSION);
	ExitOnFailure1(hr, "failed to cleanup metabase transaction '%S', continuing", pwzData);

LExit:
	ReleaseStr(pwzData);
	ReleaseObject(piMetabase);

	::CoUninitialize();

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/********************************************************************
 CommitMetabaseTransaction - CUSTOM ACTION ENTRY POINT for unbacking up metabase

  Input:  deferred CustomActionData - BackupName
 * *****************************************************************/
extern "C" UINT __stdcall CommitMetabaseTransaction(MSIHANDLE hInstall)
{
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	IMSAdminBase* piMetabase = NULL;
	LPWSTR pwzData = NULL;
	DWORD cchData = 0;

	hr = WcaInitialize(hInstall, "CommitMetabaseTransaction");
	ExitOnFailure(hr, "failed to initialize");

	hr = ::CoInitialize(NULL);
	ExitOnFailure(hr, "failed to initialize COM");
	hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, (void**)&piMetabase);
	ExitOnFailure(hr, "failed to get IID_IIMSAdminBase object");


	hr = WcaGetProperty( L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	hr = piMetabase->DeleteBackup(pwzData, MD_BACKUP_HIGHEST_VERSION);
	ExitOnFailure1(hr, "failed to cleanup metabase transaction: '%S'", pwzData);

LExit:
	ReleaseStr(pwzData);
	ReleaseObject(piMetabase);

	::CoUninitialize();

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/********************************************************************
 CreateMetabaseKey - Installs metabase keys

  Input:  deferred CustomActionData - Key
 * *****************************************************************/
static HRESULT CreateMetabaseKey(__in LPWSTR* ppwzCustomActionData, __in IMSAdminBase* piMetabase)
{
//AssertSz(FALSE, "debug here");
	HRESULT hr = S_OK;

	METADATA_HANDLE mhRoot = 0;

	LPWSTR pwzData = NULL;
	DWORD cchData = 0;

	LPCWSTR pwzKey;

	int i;

	hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzData);
	ExitOnFailure(hr, "failed to read key from custom action data");

	hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhRoot);
	for (i = 0; i < 30 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i++)
	{
		::Sleep(1000);
		WcaLog(LOGMSG_STANDARD, "failed to open root key, retrying %d time(s)...", i);
		hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhRoot);
	}
	MessageExitOnFailure1(hr, msierrIISFailedOpenKey, "failed to open metabase key: %S", L"/LM");

	pwzKey = pwzData + 3;

	WcaLog(LOGMSG_VERBOSE, "Creating Metabase Key: %S", pwzKey);

	hr = piMetabase->AddKey(mhRoot, pwzKey);
	if (HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS) == hr)
	{
		WcaLog(LOGMSG_VERBOSE, "Key `%S`already existed, continuing.", pwzData);
		hr = S_OK;
	}
	MessageExitOnFailure1(hr, msierrIISFailedCreateKey, "failed to create metabase key: %S", pwzKey);

	hr = WcaProgressMessage(COST_IIS_CREATEKEY, FALSE);
LExit:
	if (mhRoot)
		piMetabase->CloseKey(mhRoot);

	return hr;
}


/********************************************************************
 WriteMetabaseValue -Installs metabase values

  Input:  deferred CustomActionData - Key\tIdentifier\tAttributes\tUserType\tDataType\tData
 * *****************************************************************/
static HRESULT WriteMetabaseValue(__in LPWSTR* ppwzCustomActionData, __in IMSAdminBase* piMetabase)
{
//AssertSz(FALSE, "debug here");
	HRESULT hr = S_OK;

	METADATA_HANDLE mhKey = 0;

	LPWSTR pwzKey = NULL;
	LPWSTR pwzTemp = NULL;
	DWORD dwData = 0;
	BOOL fFreeData = FALSE;
	METADATA_RECORD mr;
	::ZeroMemory((LPVOID)&mr, sizeof(mr));

	int i;

	// get the key first
	hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzKey);
	ExitOnFailure(hr, "failed to read key");
	hr = WcaReadIntegerFromCaData(ppwzCustomActionData, (int *)&mr.dwMDIdentifier);
	ExitOnFailure(hr, "failed to read identifier");
	hr = WcaReadIntegerFromCaData(ppwzCustomActionData, (int *)&mr.dwMDAttributes);
	ExitOnFailure(hr, "failed to read attributes");
	hr = WcaReadIntegerFromCaData(ppwzCustomActionData, (int *)&mr.dwMDUserType);
	ExitOnFailure(hr, "failed to read user type");
	hr = WcaReadIntegerFromCaData(ppwzCustomActionData, (int *)&mr.dwMDDataType);
	ExitOnFailure(hr, "failed to read data type");

	switch (mr.dwMDDataType) // data
	{
	case DWORD_METADATA:
		hr = WcaReadIntegerFromCaData(ppwzCustomActionData, (int *)&dwData);
		mr.dwMDDataLen = sizeof(dwData);
		mr.pbMDData = (BYTE*)&dwData;
		break;
	case STRING_METADATA:
		hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzTemp);
		mr.dwMDDataLen = (lstrlenW(pwzTemp) + 1) * sizeof(WCHAR);
		mr.pbMDData = (BYTE*)pwzTemp;
		break;
	case MULTISZ_METADATA:
		{
		hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzTemp);
		mr.dwMDDataLen = (lstrlenW(pwzTemp) + 1) * sizeof(WCHAR);
		for (LPWSTR pwzT = pwzTemp; *pwzT; pwzT++)
		{
			if (MAGIC_MULTISZ_CHAR == *pwzT)
				*pwzT = L'\0';
		}
		mr.pbMDData = (BYTE*)pwzTemp;
		}
		break;
	case BINARY_METADATA:
		hr = WcaReadStreamFromCaData(ppwzCustomActionData, &mr.pbMDData, (DWORD_PTR *)&mr.dwMDDataLen);
		fFreeData = TRUE;
		break;
	default:
		hr = E_UNEXPECTED;
		break;
	}
	ExitOnFailure(hr, "failed to parse CustomActionData into metabase record");

	WcaLog(LOGMSG_VERBOSE, "Writing Metabase Value Under Key: %S ID: %d", pwzKey, mr.dwMDIdentifier);

	hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, pwzKey, METADATA_PERMISSION_WRITE, 10, &mhKey);
	for (i = 0; i < 30 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i++)
	{
		::Sleep(1000);
		WcaLog(LOGMSG_STANDARD, "failed to open '%S' key, retrying %d time(s)...", pwzKey, i);
		hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, pwzKey, METADATA_PERMISSION_WRITE, 10, &mhKey);
	}
	MessageExitOnFailure1(hr, msierrIISFailedOpenKey, "failed to open metabase key: %S", pwzKey);

	hr = piMetabase->SetData(mhKey, L"", &mr);
	MessageExitOnFailure1(hr, msierrIISFailedWriteData, "failed to write data to metabase key: %S", pwzKey);

	hr = WcaProgressMessage(COST_IIS_WRITEKEY, FALSE);
LExit:
	if (mhKey)
		piMetabase->CloseKey(mhKey);

	if (fFreeData && mr.pbMDData)
		MemFree(mr.pbMDData);

	return hr;
}


/********************************************************************
 CreateAspApp - Creates applications in IIS

  Input:  deferred CustomActionData - MetabaseRoot\tInProc
 * ****************************************************************/
static HRESULT CreateAspApp(__in LPWSTR* ppwzCustomActionData, __in IWamAdmin* piWam)
{
	HRESULT hr = S_OK;

	LPWSTR pwzRoot = NULL;
	BOOL fInProc;

	hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzRoot); // MetabaseRoot
	ExitOnFailure(hr, "failed to get metabase root");
	hr = WcaReadIntegerFromCaData(ppwzCustomActionData, (int *)&fInProc); // InProc
	ExitOnFailure(hr, "failed to get in proc flag");

	WcaLog(LOGMSG_VERBOSE, "Creating ASP App: %S", pwzRoot);

	hr = piWam->AppCreate(pwzRoot, fInProc);
	MessageExitOnFailure1(hr, msierrIISFailedCreateApp, "failed to create web application: %S", pwzRoot);

	hr = WcaProgressMessage(COST_IIS_CREATEAPP, FALSE);
LExit:
	return hr;
}


/********************************************************************
 DeleteMetabaseKey - Deletes metabase keys

  Input:  deferred CustomActionData - Key
 ******************************************************************/
static HRESULT DeleteMetabaseKey(__in LPWSTR *ppwzCustomActionData, __in IMSAdminBase* piMetabase)
{
	HRESULT hr = S_OK;

	METADATA_HANDLE mhRoot = 0;

	LPWSTR pwzData = NULL;

	LPCWSTR pwzKey;
	int i;

	hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzData);
	ExitOnFailure(hr, "failed to read key to be deleted");

	hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhRoot);
	for (i = 0; i < 30 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i++)
	{
		::Sleep(1000);
		WcaLog(LOGMSG_STANDARD, "failed to open root key, retrying %d time(s)...", i);
		hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhRoot);
	}
	MessageExitOnFailure1(hr, msierrIISFailedOpenKey, "failed to open metabase key: %S", L"/LM");

	pwzKey = pwzData + 3;

	WcaLog(LOGMSG_VERBOSE, "Deleting Metabase Key: %S", pwzKey);

	hr = piMetabase->DeleteKey(mhRoot, pwzKey);
	if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
	{
		WcaLog(LOGMSG_STANDARD, "Key `%S`did not exist, continuing.", pwzData);
		hr = S_OK;
	}
	MessageExitOnFailure1(hr, msierrIISFailedDeleteKey, "failed to delete metabase key: %S", pwzData);

	hr = WcaProgressMessage(COST_IIS_DELETEKEY, FALSE);
LExit:
	if (mhRoot)
		piMetabase->CloseKey(mhRoot);

	return hr;
}


/********************************************************************
 WriteMetabaseChanges - CUSTOM ACTION ENTRY POINT for IIS Metabase changes

 *******************************************************************/
extern "C" UINT __stdcall WriteMetabaseChanges(MSIHANDLE hInstall)
{
//AssertSz(FALSE, "debug here");
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;
	IMSAdminBase* piMetabase = NULL;
	IWamAdmin* piWam = NULL;

	LPWSTR pwzData = NULL;
	LPWSTR pwz = NULL;
	int iAction = -1;

	hr = WcaInitialize(hInstall, "WriteMetabaseChanges");
	ExitOnFailure(hr, "failed to initialize");

	hr = ::CoInitialize(NULL);
	ExitOnFailure(hr, "failed to initialize COM");

	hr = WcaGetProperty( L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzData);

	pwz = pwzData;

	while (S_OK == (hr = WcaReadIntegerFromCaData(&pwz, &iAction)))
	{
		switch (iAction)
		{
		case MBA_CREATEAPP:
			if (NULL == piWam)
			{
				hr = ::CoCreateInstance(CLSID_WamAdmin, NULL, CLSCTX_ALL, IID_IWamAdmin, (void**)&piWam);
				MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IWamAdmin object");
			}

			hr = CreateAspApp(&pwz, piWam);
			ExitOnFailure(hr, "failed to create ASP App");
			break;
		case MBA_CREATEKEY:
			if (NULL == piMetabase)
			{
				hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, (void**)&piMetabase);
				MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");
			}

			hr = CreateMetabaseKey(&pwz, piMetabase);
			ExitOnFailure(hr, "failed to create metabase key");
			break;
		case MBA_DELETEKEY:
			if (NULL == piMetabase)
			{
				hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, (void**)&piMetabase);
				MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");
			}

			hr = DeleteMetabaseKey(&pwz, piMetabase);
			ExitOnFailure(hr, "failed to delete metabase key");
			break;
		case MBA_WRITEKEY:
			if (NULL == piMetabase)
			{
				hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, (void**)&piMetabase);
				MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");
			}

			hr = WriteMetabaseValue(&pwz, piMetabase);
			ExitOnFailure(hr, "failed to write metabase value");
			break;
		default:
			ExitOnFailure1(hr = E_UNEXPECTED, "Unexpected metabase action specified: %d", iAction);
			break;
		}
	}
	if (E_NOMOREITEMS == hr) // If there are no more items, all is well
		hr = S_OK;

LExit:
	ReleaseStr(pwzData);
	ReleaseObject(piMetabase);
	ReleaseObject(piWam);

	::CoUninitialize();

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/********************************************************************
 ErrorOut - CUSTOM ACTION ENTRY POINT for blowing up

 *******************************************************************/
extern "C" UINT __stdcall ErrorOut(MSIHANDLE hInstall)
{
Assert(FALSE);
	UINT er = ERROR_INSTALL_FAILURE;

	HRESULT hr = WcaInitialize(hInstall, "ErrorOut");
	ExitOnFailure(hr, "failed to initialize");

LExit:
	return WcaFinalize(er);
}


/********************************************************************
 * CreateDatabase - CUSTOM ACTION ENTRY POINT for creating databases
 *
 *  Input:  deferred CustomActionData - DbKey\tServer\tInstance\tDatabase\tAttributes\tIntegratedAuth\tUser\tPassword
 * ****************************************************************/
extern "C" UINT __stdcall CreateDatabase(MSIHANDLE hInstall)
{
//AssertSz(FALSE, "debug here");
	UINT er = ERROR_SUCCESS;
	HRESULT hr = S_OK;

	LPWSTR pwzData = NULL;
	DWORD cchData = 0;

	IDBCreateSession* pidbSession = NULL;
	BSTR bstrErrorDescription = NULL;
	LPWSTR pwz = NULL;
	LPWSTR pwzDatabaseKey = NULL;
	LPWSTR pwzServer = NULL;
	LPWSTR pwzInstance = NULL;
	LPWSTR pwzDatabase = NULL;
	LPWSTR pwzTemp = NULL;
	int iAttributes;
	BOOL fIntegratedAuth;
	LPWSTR pwzUser = NULL;
	LPWSTR pwzPassword = NULL;
	BOOL fHaveDbFileSpec = FALSE;
	SQL_FILESPEC sfDb;
	BOOL fHaveLogFileSpec = FALSE;
	SQL_FILESPEC sfLog;

	memset(&sfDb, 0, sizeof(sfDb));
	memset(&sfLog, 0, sizeof(sfLog));

	hr = WcaInitialize(hInstall, "CreateDatabase");
	ExitOnFailure(hr, "failed to initialize");

	hr = ::CoInitialize(NULL);
	ExitOnFailure(hr, "failed to intialize COM");

	hr = WcaGetProperty( L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzData);

	pwz = pwzData;
	hr = WcaReadStringFromCaData(&pwz, &pwzDatabaseKey); // SQL Server
	ExitOnFailure1(hr, "failed to read database key from custom action data: %S", pwz);
	hr = WcaReadStringFromCaData(&pwz, &pwzServer); // SQL Server
	ExitOnFailure1(hr, "failed to read server from custom action data: %S", pwz);
	hr = WcaReadStringFromCaData(&pwz, &pwzInstance); // SQL Server Instance
	ExitOnFailure1(hr, "failed to read server instance from custom action data: %S", pwz);
	hr = WcaReadStringFromCaData(&pwz, &pwzDatabase); // SQL Database
	ExitOnFailure1(hr, "failed to read server instance from custom action data: %S", pwz);
	hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
	ExitOnFailure1(hr, "failed to read attributes from custom action data: %S", pwz);
	hr = WcaReadIntegerFromCaData(&pwz, (int *)&fIntegratedAuth); // Integrated Windows Authentication?
	ExitOnFailure1(hr, "failed to read integrated auth flag from custom action data: %S", pwz);
	hr = WcaReadStringFromCaData(&pwz, &pwzUser); // SQL User
	ExitOnFailure1(hr, "failed to read user from custom action data: %S", pwz);
	hr = WcaReadStringFromCaData(&pwz, &pwzPassword); // SQL User Password
	ExitOnFailure1(hr, "failed to read user from custom action data: %S", pwz);

	// db file spec
	hr = WcaReadIntegerFromCaData(&pwz, (int *) &fHaveDbFileSpec);
	ExitOnFailure1(hr, "failed to read db file spec from custom action data: %S", pwz);

	if (fHaveDbFileSpec)
	{
		hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
		ExitOnFailure1(hr, "failed to read db file spec name from custom action data: %S", pwz);
		hr = StringCchCopyW(sfDb.wzName, countof(sfDb.wzName), pwzTemp);
		ExitOnFailure1(hr, "failed to copy db file spec name: %S", pwzTemp);

		hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
		ExitOnFailure1(hr, "failed to read db file spec filename from custom action data: %S", pwz);
		hr = StringCchCopyW(sfDb.wzFilename, countof(sfDb.wzFilename), pwzTemp);
		ExitOnFailure1(hr, "failed to copy db file spec filename: %S", pwzTemp);

		hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
		ExitOnFailure1(hr, "failed to read db file spec size from custom action data: %S", pwz);
		hr = StringCchCopyW(sfDb.wzSize, countof(sfDb.wzSize), pwzTemp);
		ExitOnFailure1(hr, "failed to copy db file spec size value: %S", pwzTemp);

		hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
		ExitOnFailure1(hr, "failed to read db file spec max size from custom action data: %S", pwz);
		hr = StringCchCopyW(sfDb.wzMaxSize, countof(sfDb.wzMaxSize), pwzTemp);
		ExitOnFailure1(hr, "failed to copy db file spec max size: %S", pwzTemp);

		hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
		ExitOnFailure1(hr, "failed to read db file spec grow from custom action data: %S", pwz);
		hr = StringCchCopyW(sfDb.wzGrow, countof(sfDb.wzGrow), pwzTemp);
		ExitOnFailure1(hr, "failed to copy db file spec grow value: %S", pwzTemp);
	}

	// log file spec
	hr = WcaReadIntegerFromCaData(&pwz, (int *) &fHaveLogFileSpec);
	ExitOnFailure1(hr, "failed to read log file spec from custom action data: %S", pwz);
	if (fHaveLogFileSpec)
	{
		hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
		ExitOnFailure1(hr, "failed to read log file spec name from custom action data: %S", pwz);
		hr = StringCchCopyW(sfLog.wzName, countof(sfDb.wzName), pwzTemp);
		ExitOnFailure1(hr, "failed to copy log file spec name: %S", pwzTemp);

		hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
		ExitOnFailure1(hr, "failed to read log file spec filename from custom action data: %S", pwz);
		hr = StringCchCopyW(sfLog.wzFilename, countof(sfDb.wzFilename), pwzTemp);
		ExitOnFailure1(hr, "failed to copy log file spec filename: %S", pwzTemp);

		hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
		ExitOnFailure1(hr, "failed to read log file spec size from custom action data: %S", pwz);
		hr = StringCchCopyW(sfLog.wzSize, countof(sfDb.wzSize), pwzTemp);
		ExitOnFailure1(hr, "failed to copy log file spec size value: %S", pwzTemp);

		hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
		ExitOnFailure1(hr, "failed to read log file spec max size from custom action data: %S", pwz);
		hr = StringCchCopyW(sfLog.wzMaxSize, countof(sfDb.wzMaxSize), pwzTemp);
		ExitOnFailure1(hr, "failed to copy log file spec max size: %S", pwzTemp);

		hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
		ExitOnFailure1(hr, "failed to read log file spec grow from custom action data: %S", pwz);
		hr = StringCchCopyW(sfLog.wzGrow, countof(sfDb.wzGrow), pwzTemp);
		ExitOnFailure1(hr, "failed to copy log file spec grow value: %S", pwzTemp);
	}

	if (iAttributes & SCADB_CONFIRM_OVERWRITE)
	{
		// Check if the database already exists
		hr = SqlDatabaseExists(pwzServer, pwzInstance, pwzDatabase, fIntegratedAuth, pwzUser, pwzPassword, &bstrErrorDescription);
		MessageExitOnFailure2(hr, msierrSQLFailedCreateDatabase, "failed to check if database exists: '%S', error: %S", pwzDatabase, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription);

		if (S_OK == hr) // found an existing database, confirm that they don't want to stop before it gets trampled
		{
			hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
			if (IDNO == WcaErrorMessage(msierrSQLDatabaseAlreadyExists, hr, MB_YESNO, 1, pwzDatabase))
				ExitOnFailure(hr, "failed to initialize");
		}
	}

	hr = SqlDatabaseEnsureExists(pwzServer, pwzInstance, pwzDatabase, fIntegratedAuth, pwzUser, pwzPassword, fHaveDbFileSpec ? &sfDb : NULL, fHaveLogFileSpec ? &sfLog : NULL, &bstrErrorDescription);
	if ((iAttributes & SCADB_CONTINUE_ON_ERROR) && FAILED(hr))
	{
		WcaLog(LOGMSG_STANDARD, "Error 0x%x: failed to create SQL database but continuing, error: %S, Database: %S", hr, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription, pwzDatabase);
		hr = S_OK;
	}
	MessageExitOnFailure2(hr, msierrSQLFailedCreateDatabase, "failed to create to database: '%S', error: %S", pwzDatabase, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription);

	hr = WcaProgressMessage(COST_SQL_CONNECTDB, FALSE);
LExit:
	ReleaseStr(pwzDatabaseKey);
	ReleaseStr(pwzServer);
	ReleaseStr(pwzInstance);
	ReleaseStr(pwzDatabase);
	ReleaseStr(pwzUser);
	ReleaseStr(pwzPassword);
	ReleaseObject(pidbSession);
	ReleaseBSTR(bstrErrorDescription);

	::CoUninitialize();

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/********************************************************************
 DropDatabase - CUSTOM ACTION ENTRY POINT for removing databases

  Input:  deferred CustomActionData - DbKey\tServer\tInstance\tDatabase\tAttributes\tIntegratedAuth\tUser\tPassword
 * ****************************************************************/
extern "C" UINT __stdcall DropDatabase(MSIHANDLE hInstall)
{
//Assert(FALSE);
	UINT er = ERROR_SUCCESS;
	HRESULT hr = S_OK;

	LPWSTR pwzData = NULL;
	IDBCreateSession* pidbSession = NULL;
	BSTR bstrErrorDescription = NULL;
	LPWSTR pwz = NULL;
	LPWSTR pwzDatabaseKey = NULL;
	LPWSTR pwzServer = NULL;
	LPWSTR pwzInstance = NULL;
	LPWSTR pwzDatabase = NULL;
	long lAttributes;
	BOOL fIntegratedAuth;
	LPWSTR pwzUser = NULL;
	LPWSTR pwzPassword = NULL;


	hr = WcaInitialize(hInstall, "DropDatabase");
	ExitOnFailure(hr, "failed to initialize");

	hr = ::CoInitialize(NULL);
	ExitOnFailure(hr, "failed to intialize COM");

	hr = WcaGetProperty( L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzData);

	pwz = pwzData;
	hr = WcaReadStringFromCaData(&pwz, &pwzDatabaseKey);
	ExitOnFailure(hr, "failed to read database key");
	hr = WcaReadStringFromCaData(&pwz, &pwzServer);
	ExitOnFailure(hr, "failed to read server");
	hr = WcaReadStringFromCaData(&pwz, &pwzInstance);
	ExitOnFailure(hr, "failed to read instance");
	hr = WcaReadStringFromCaData(&pwz, &pwzDatabase);
	ExitOnFailure(hr, "failed to read database");
	hr = WcaReadIntegerFromCaData(&pwz, (int *)&lAttributes);
	ExitOnFailure(hr, "failed to read attributes");
	hr = WcaReadIntegerFromCaData(&pwz, (int *)&fIntegratedAuth); // Integrated Windows Authentication?
	ExitOnFailure(hr, "failed to read integrated auth flag");
	hr = WcaReadStringFromCaData(&pwz, &pwzUser);
	ExitOnFailure(hr, "failed to read user");
	hr = WcaReadStringFromCaData(&pwz, &pwzPassword);
	ExitOnFailure(hr, "failed to read password");

	hr = SqlDropDatabase(pwzServer, pwzInstance, pwzDatabase, fIntegratedAuth, pwzUser, pwzPassword, &bstrErrorDescription);
	if ((lAttributes & SCADB_CONTINUE_ON_ERROR) && FAILED(hr))
	{
		WcaLog(LOGMSG_STANDARD, "Error 0x%x: failed to drop SQL database but continuing, error: %S, Database: %S", hr, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription, pwzDatabase);
		hr = S_OK;
	}
	MessageExitOnFailure2(hr, msierrSQLFailedDropDatabase, "failed to drop to database: '%S', error: %S", pwzDatabase, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription);

	hr = WcaProgressMessage(COST_SQL_CONNECTDB, FALSE);

LExit:
	ReleaseStr(pwzDatabaseKey);
	ReleaseStr(pwzServer);
	ReleaseStr(pwzInstance);
	ReleaseStr(pwzDatabase);
	ReleaseStr(pwzUser);
	ReleaseStr(pwzPassword);
	ReleaseStr(pwzData);
	ReleaseObject(pidbSession);
	ReleaseBSTR(bstrErrorDescription);

	::CoUninitialize();

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/********************************************************************
 ExecuteSqlStrings - CUSTOM ACTION ENTRY POINT for running SQL strings

  Input:  deferred CustomActionData - DbKey\tServer\tInstance\tDatabase\tAttributes\tIntegratedAuth\tUser\tPassword\tSQLKey1\tSQLString1\tSQLKey2\tSQLString2\tSQLKey3\tSQLString3\t...
		  rollback CustomActionData - same as above
 * ****************************************************************/
extern "C" UINT __stdcall ExecuteSqlStrings(MSIHANDLE hInstall)
{
//Assert(FALSE);
	UINT er = ERROR_SUCCESS;
	HRESULT hr = S_OK;

	LPWSTR pwzData = NULL;
	IDBCreateSession* pidbSession = NULL;
	BSTR bstrErrorDescription = NULL;

	LPWSTR pwz = NULL;
	LPWSTR pwzDatabaseKey = NULL;
	LPWSTR pwzServer = NULL;
	LPWSTR pwzInstance = NULL;
	LPWSTR pwzDatabase = NULL;
	int iAttributesDB;
	int iAttributesSQL;
	BOOL fIntegratedAuth;
	LPWSTR pwzUser = NULL;
	LPWSTR pwzPassword = NULL;
	LPWSTR pwzSqlKey = NULL;
	LPWSTR pwzSql = NULL;

	hr = WcaInitialize(hInstall, "ExecuteSqlStrings");
	ExitOnFailure(hr, "failed to initialize");

	hr = ::CoInitialize(NULL);
	ExitOnFailure(hr, "failed to intialize COM");

	hr = WcaGetProperty( L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzData);

	pwz = pwzData;
	hr = WcaReadStringFromCaData(&pwz, &pwzDatabaseKey);
	ExitOnFailure(hr, "failed to read database key");
	hr = WcaReadStringFromCaData(&pwz, &pwzServer);
	ExitOnFailure(hr, "failed to read server");
	hr = WcaReadStringFromCaData(&pwz, &pwzInstance);
	ExitOnFailure(hr, "failed to read instance");
	hr = WcaReadStringFromCaData(&pwz, &pwzDatabase);
	ExitOnFailure(hr, "failed to read database");
	hr = WcaReadIntegerFromCaData(&pwz, &iAttributesDB);
	ExitOnFailure(hr, "failed to read attributes");
	hr = WcaReadIntegerFromCaData(&pwz, (int *)&fIntegratedAuth); // Integrated Windows Authentication?
	ExitOnFailure(hr, "failed to read integrated auth flag");
	hr = WcaReadStringFromCaData(&pwz, &pwzUser);
	ExitOnFailure(hr, "failed to read user");
	hr = WcaReadStringFromCaData(&pwz, &pwzPassword);
	ExitOnFailure(hr, "failed to read password");

	hr = SqlConnectDatabase(pwzServer, pwzInstance, pwzDatabase, fIntegratedAuth, pwzUser, pwzPassword, &pidbSession);
	if ((iAttributesDB & SCADB_CONTINUE_ON_ERROR) && FAILED(hr))
	{
		WcaLog(LOGMSG_STANDARD, "Error 0x%x: continuing after failure to connect to database: %S", hr, pwzDatabase);
		ExitFunction1(hr = S_OK);
	}
	MessageExitOnFailure1(hr, msierrSQLFailedConnectDatabase, "failed to connect to database: '%S'", pwzDatabase);

	while (S_OK == hr && S_OK == (hr = WcaReadStringFromCaData(&pwz, &pwzSqlKey)))
	{
		hr = WcaReadIntegerFromCaData(&pwz, &iAttributesSQL);
		ExitOnFailure1(hr, "failed to read attributes for SQL string: %S", pwzSqlKey);

		hr = WcaReadStringFromCaData(&pwz, &pwzSql);
		ExitOnFailure1(hr, "failed to read SQL string for key: %S", pwzSqlKey);

		WcaLog(LOGMSG_VERBOSE, "Executing SQL string: %S", pwzSql);
		hr = SqlSessionExecuteQuery(pidbSession, pwzSql, NULL, NULL, &bstrErrorDescription);
		if ((iAttributesSQL & SCASQL_CONTINUE_ON_ERROR) && FAILED(hr))
		{
			WcaLog(LOGMSG_STANDARD, "Error 0x%x: failed to execute SQL string but continuing, error: %S, SQL key: %S SQL string: %S", hr, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription, pwzSqlKey, pwzSql);
			hr = S_OK;
		}
		MessageExitOnFailure3(hr, msierrSQLFailedExecString, "failed to execute SQL string, error: %S, SQL key: %S SQL string: %S", NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription, pwzSqlKey, pwzSql);

		WcaProgressMessage(COST_SQL_STRING, FALSE);
	}
	if (E_NOMOREITEMS == hr)
		hr = S_OK;

LExit:
	ReleaseStr(pwzDatabaseKey);
	ReleaseStr(pwzServer);
	ReleaseStr(pwzInstance);
	ReleaseStr(pwzDatabase);
	ReleaseStr(pwzUser);
	ReleaseStr(pwzPassword);
	ReleaseStr(pwzData);

	ReleaseBSTR(bstrErrorDescription);
	ReleaseObject(pidbSession);

	::CoUninitialize();

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/********************************************************************
 (Un)registerPerfmon - CUSTOM ACTION ENTRY POINT for (Un)registering PerfMon counters

 Input:  deferred CustomActionData -
	wzFile or wzName
 * ****************************************************************/
typedef DWORD (STDAPICALLTYPE *PFNPERFCOUNTERTEXTSTRINGS)(LPWSTR lpCommandLine, BOOL bQuietModeArg);

extern "C" UINT __stdcall RegisterPerfmon(MSIHANDLE hInstall)
{
//    Assert(FALSE);
	UINT er = ERROR_SUCCESS;
	HRESULT hr = S_OK;
	LPWSTR pwzData = NULL;

	HMODULE hMod = NULL;
	PFNPERFCOUNTERTEXTSTRINGS pfnPerfCounterTextString;
	DWORD_PTR dwRet;
	LPWSTR pwzShortPath = NULL;
	DWORD_PTR cchShortPath = MAX_PATH;
	DWORD_PTR cchShortPathLength  = 0;

	LPWSTR pwzCommand = NULL;
	DWORD_PTR cchCommand = 0;

	hr = WcaInitialize(hInstall, "RegisterPerfmon");
	ExitOnFailure(hr, "failed to initialize");

	hr = WcaGetProperty(L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzData);

	// do the perfmon registration
	if (NULL == hMod)
		hMod = ::LoadLibraryW(L"loadperf.dll");
	ExitOnNullWithLastError(hMod, hr, "failed to load DLL for PerfMon");

	pfnPerfCounterTextString = (PFNPERFCOUNTERTEXTSTRINGS)::GetProcAddress(hMod, "LoadPerfCounterTextStringsW");
	if (NULL == pfnPerfCounterTextString)
		ExitWithLastError(hr, "failed to get DLL function for PerfMon");

	hr = StrAlloc(&pwzShortPath, cchShortPath);
	ExitOnFailure(hr, "failed to allocate string");

	WcaLog(LOGMSG_VERBOSE, "Converting DLL path to short format: %S", pwzData);
	cchShortPathLength = ::GetShortPathNameW(pwzData, pwzShortPath, cchShortPath);
	if (cchShortPathLength > cchShortPath)
	{
		cchShortPath = cchShortPathLength + 1;
		hr = StrAlloc(&pwzShortPath, cchShortPath);
		ExitOnFailure(hr, "failed to allocate string");

		cchShortPathLength = ::GetShortPathNameW(pwzData, pwzShortPath, cchShortPath);
	}

	if (0 == cchShortPathLength)
	{
		ExitWithLastError1(hr, "failed to get short path format of path: %S", pwzData);
	}

	hr = StrAllocFormatted(&pwzCommand, L"lodctr \"%s\"", pwzShortPath);
	ExitOnFailure(hr, "failed to format lodctr string");

	WcaLog(LOGMSG_VERBOSE, "RegisterPerfmon running command: '%S'", pwzCommand);
	dwRet = (*pfnPerfCounterTextString)(pwzCommand, TRUE);
	if (dwRet != ERROR_SUCCESS && dwRet != ERROR_ALREADY_EXISTS)
	{
		hr = HRESULT_FROM_WIN32(dwRet);
		MessageExitOnFailure1(hr, msierrPERFMONFailedRegisterDLL, "failed to register with PerfMon, DLL: %S", pwzData);
	}

	hr = S_OK;
LExit:
	ReleaseStr(pwzData);

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


extern "C" UINT __stdcall UnregisterPerfmon(MSIHANDLE hInstall)
{
//    Assert(FALSE);
	UINT er = ERROR_SUCCESS;
	HRESULT hr = S_OK;
	LPWSTR pwzData = NULL;

	HMODULE hMod = NULL;
	PFNPERFCOUNTERTEXTSTRINGS pfnPerfCounterTextString;
	DWORD dwRet;
	WCHAR wz[255];

	hr = WcaInitialize(hInstall, "UnregisterPerfmon");
	ExitOnFailure(hr, "failed to initialize");

	hr = WcaGetProperty(L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzData);

	// do the perfmon unregistration
	hr = E_FAIL;
	if (hMod == NULL)
		hMod = LoadLibraryW(L"loadperf.dll");
	ExitOnNullWithLastError(hMod, hr, "failed to load DLL for PerfMon");

	pfnPerfCounterTextString = (PFNPERFCOUNTERTEXTSTRINGS)::GetProcAddress(hMod, "UnloadPerfCounterTextStringsW");
	ExitOnNullWithLastError(pfnPerfCounterTextString, hr, "failed to get DLL function for PerfMon");

	StringCchPrintfW(wz, countof(wz), L"unlodctr \"%s\"", pwzData);
	WcaLog(LOGMSG_VERBOSE, "UnregisterPerfmon running command: '%S'", wz);
	dwRet = (*pfnPerfCounterTextString)(wz, TRUE);
	// if the counters aren't registered, then OK to continue
	if (dwRet != ERROR_SUCCESS && dwRet != ERROR_FILE_NOT_FOUND && dwRet != ERROR_BADKEY)
	{
		hr = HRESULT_FROM_WIN32(dwRet);
		MessageExitOnFailure1(hr, msierrPERFMONFailedUnregisterDLL, "failed to unregsister with PerfMon, DLL: %S", pwzData);
	}

	hr = S_OK;
LExit:
	ReleaseStr(pwzData);

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/********************************************************************
 * CreateSmb - CUSTOM ACTION ENTRY POINT for creating fileshares
 *
 * Input:  deferred CustomActionData -
 *    wzFsKey\twzShareDesc\twzFullPath\tfIntegratedAuth\twzUserName\tnPermissions\twzUserName\tnPermissions...
 *
 * ****************************************************************/
extern "C" UINT __stdcall CreateSmb(MSIHANDLE hInstall)
{
//AssertSz(0, "debug CreateSmb");
	UINT er = ERROR_SUCCESS;
	HRESULT hr = S_OK;

	LPWSTR pwzData = NULL;
	LPWSTR pwz = NULL;
	LPWSTR pwzFsKey = NULL;
	LPWSTR pwzShareDesc = NULL;
	LPWSTR pwzDirectory = NULL;
	LPCWSTR pwzTemp = NULL;
	DWORD nExPermissions = 0;
	BOOL fIntegratedAuth;
	LPWSTR pwzExUser = NULL;
	SCA_SMBP ssp = {0};
	DWORD dwExUserPerms = 0;
	DWORD dwCounter = 0;
	SCA_SMBP_USER_PERMS* pUserPermsList = NULL;

	hr = WcaInitialize(hInstall, "CreateSmb");
	ExitOnFailure(hr, "failed to initialize");

	hr = WcaGetProperty( L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzData);

	pwz = pwzData;
	hr = WcaReadStringFromCaData(&pwz, &pwzFsKey); // share name
	ExitOnFailure(hr, "failed to read share name");
	hr = WcaReadStringFromCaData(&pwz, &pwzShareDesc); // share description
	ExitOnFailure(hr, "failed to read share name");
	hr = WcaReadStringFromCaData(&pwz, &pwzDirectory); // full path to share
	ExitOnFailure(hr, "failed to read share name");
	hr = WcaReadIntegerFromCaData(&pwz, (int *)&fIntegratedAuth);
	ExitOnFailure(hr, "failed to read integrated authentication");

	hr = WcaReadIntegerFromCaData(&pwz, (int *)&dwExUserPerms);
	ExitOnFailure(hr, "failed to read count of permissions to set");
	if(dwExUserPerms > 0)
	{
		pUserPermsList = (SCA_SMBP_USER_PERMS*)MemAlloc(sizeof(SCA_SMBP_USER_PERMS)*dwExUserPerms, TRUE);
		ExitOnNull(pUserPermsList, hr, E_OUTOFMEMORY, "failed to allocate memory for permissions structure");

		//Pull out all of the ExUserPerm strings
		for(dwCounter=0;dwCounter<dwExUserPerms;dwCounter++)
		{
			hr = WcaReadStringFromCaData(&pwz, &pwzExUser); // user account
			ExitOnFailure(hr, "failed to read user account");
			pUserPermsList[dwCounter].wzUser = pwzExUser;
			pwzExUser = NULL;

			hr = WcaReadIntegerFromCaData(&pwz, (int *)&nExPermissions);
			ExitOnFailure(hr, "failed to read count of permissions");
			pUserPermsList[dwCounter].nPermissions = nExPermissions;
			nExPermissions = 0;
		}
	}

	ssp.wzKey = pwzFsKey;
	ssp.wzDescription = pwzShareDesc;
	ssp.wzDirectory = pwzDirectory;
	ssp.fUseIntegratedAuth = fIntegratedAuth;
	ssp.dwUserPermissionCount = dwExUserPerms;
	ssp.pUserPerms = pUserPermsList;

	hr = ScaEnsureSmbExists(&ssp);
	MessageExitOnFailure1(hr, msierrSMBFailedCreate, "failed to create to share: '%S'", pwzFsKey);

	hr = WcaProgressMessage(COST_SMB_CREATESMB, FALSE);

LExit:
	ReleaseStr(pwzFsKey);
	ReleaseStr(pwzShareDesc);
	ReleaseStr(pwzDirectory);
	ReleaseStr(pwzData);

	if (pUserPermsList)
		MemFree(pUserPermsList);

	::CoUninitialize();   // CAREVIEW: no CoInitialize()

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}



/********************************************************************
 DropSmb - CUSTOM ACTION ENTRY POINT for creating fileshares

 Input:  deferred CustomActionData - wzFsKey\twzShareDesc\twzFullPath\tnPermissions\tfIntegratedAuth\twzUserName\twzPassword

 * ****************************************************************/
extern "C" UINT __stdcall DropSmb(MSIHANDLE hInstall)
{
	//AssertSz(0, "debug DropSmb");
	UINT er = ERROR_SUCCESS;
	HRESULT hr = S_OK;

	LPWSTR pwzData = NULL;
	LPWSTR pwz = NULL;
	LPWSTR pwzFsKey = NULL;
	SCA_SMBP ssp = {0};

	hr = WcaInitialize(hInstall, "DropSmb");
	ExitOnFailure(hr, "failed to initialize");

	hr = WcaGetProperty( L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzData);

	pwz = pwzData;
	hr = WcaReadStringFromCaData(&pwz, &pwzFsKey); // share name
	ExitOnFailure(hr, "failed to read share name");

	ssp.wzKey = pwzFsKey;

	hr = ScaDropSmb(&ssp);
	MessageExitOnFailure1(hr, msierrSMBFailedDrop, "failed to create to share: '%S'", pwzFsKey);

	hr = WcaProgressMessage(COST_SMB_DROPSMB, FALSE);

LExit:
	ReleaseStr(pwzFsKey);
	ReleaseStr(pwzData);

	::CoUninitialize();   // CAREVIEW: no CoInitialize()

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


static HRESULT CreateADsPath(
	__in LPCWSTR wzObjectDomain,
	__in LPCWSTR wzObjectName,
	__out BSTR *pbstrAdsPath
	)
{
	Assert(wzObjectDomain && wzObjectName && *wzObjectName);

	HRESULT hr = S_OK;
	LPWSTR pwzAdsPath = NULL;

	hr = StrAllocString(&pwzAdsPath, L"WinNT://", 0);
	ExitOnFailure(hr, "failed to allocate AdsPath string");

	if (*wzObjectDomain)
	{
		hr = StrAllocFormatted(&pwzAdsPath, L"%s/%s", wzObjectDomain, wzObjectName);
		ExitOnFailure(hr, "failed to allocate AdsPath string");
	}
	else if (NULL != wcsstr(wzObjectName, L"\\") || NULL != wcsstr(wzObjectName, L"/"))
	{
		hr = StrAllocConcat(&pwzAdsPath, wzObjectName, 0);
		ExitOnFailure1(hr, "failed to concat objectname: %S", wzObjectName);
	}
	else
	{
		hr = StrAllocConcat(&pwzAdsPath, L"Localhost/", 0);
		ExitOnFailure(hr, "failed to concat LocalHost/");

		hr = StrAllocConcat(&pwzAdsPath, wzObjectName, 0);
		ExitOnFailure1(hr, "failed to concat object name: %S", wzObjectName);
	}

	*pbstrAdsPath = ::SysAllocString(pwzAdsPath);

LExit:

	ReleaseStr(pwzAdsPath);

	return hr;
}


static HRESULT AddUserToGroup(
	__in WCHAR *wzUser,
	__in LPCWSTR wzUserDomain,
	__in LPCWSTR wzGroup,
	__in LPCWSTR wzGroupDomain
	)
{
	Assert(wzUser && *wzUser && wzUserDomain && wzGroup && *wzGroup && wzGroupDomain);

	HRESULT hr = S_OK;
	IADsGroup *pGroup = NULL;
	BSTR bstrUser = NULL;
	BSTR bstrGroup = NULL;
	LPCWSTR wz = NULL;
	LPWSTR pwzUser = NULL;
	LOCALGROUP_MEMBERS_INFO_3 lgmi;

	if (*wzGroupDomain)
		wz = wzGroupDomain;

	// Try adding it to the global group first
	UINT ui = ::NetGroupAddUser(wz, wzGroup, wzUser);
	if (NERR_GroupNotFound == ui)
	{
		// Try adding it to the local group
		if (wzUserDomain)
		{
			hr = StrAllocFormatted(&pwzUser, L"%s\\%s", wzUserDomain, wzUser);
			ExitOnFailure(hr, "failed to allocate user domain string");
		}

		lgmi.lgrmi3_domainandname = (NULL == pwzUser ? wzUser : pwzUser);
		ui = ::NetLocalGroupAddMembers(wz, wzGroup, 3 , (LPBYTE)&lgmi, 1);
	}
	hr = HRESULT_FROM_WIN32(ui);
	if (HRESULT_FROM_WIN32(ERROR_MEMBER_IN_ALIAS) == hr) // if they're already a member of the group don't report an error
		hr = S_OK;

	//
	// If we failed, try active directory
	//
	if (FAILED(hr))
	{
		WcaLog(LOGMSG_VERBOSE, "Failed to add user: %S, domain %S to group: %S, domain: %S with error 0x%x.  Attempting to use Active Directory", wzUser, wzUserDomain, wzGroup, wzGroupDomain, hr);

		hr = CreateADsPath(wzUserDomain, wzUser, &bstrUser);
		ExitOnFailure2(hr, "failed to create user ADsPath for user: %S domain: %S", wzUser, wzUserDomain);

		hr = CreateADsPath(wzGroupDomain, wzGroup, &bstrGroup);
		ExitOnFailure2(hr, "failed to create group ADsPath for group: %S domain: %S", wzGroup, wzGroupDomain);

		hr = ::ADsGetObject(bstrGroup,IID_IADsGroup,(void**)&pGroup);
		ExitOnFailure1(hr, "Failed to get group '%S'.", (WCHAR*)bstrGroup );

		hr = pGroup->Add(bstrUser);
		if ((HRESULT_FROM_WIN32(ERROR_OBJECT_ALREADY_EXISTS) == hr) || (HRESULT_FROM_WIN32(ERROR_MEMBER_IN_ALIAS) == hr))
			hr = S_OK;

		ExitOnFailure2(hr, "Failed to add user %S to group '%S'.", (WCHAR*)bstrUser, (WCHAR*)bstrGroup );
	}

LExit:
	ReleaseObject(pGroup);
	ReleaseBSTR(bstrUser);
	ReleaseBSTR(bstrGroup);

	return hr;
}


static void SetUserPasswordAndAttributes(
	__in USER_INFO_1* puserInfo,
	__in LPWSTR wzPassword,
	__in int iAttributes
	)
{
	Assert(puserInfo);

	// Set the User's password
	puserInfo->usri1_password = wzPassword;

	// Apply the Attributes
	if (SCAU_DONT_EXPIRE_PASSWRD & iAttributes)
		puserInfo->usri1_flags |= UF_DONT_EXPIRE_PASSWD;
	else
		puserInfo->usri1_flags &= ~UF_DONT_EXPIRE_PASSWD;

	if (SCAU_PASSWD_CANT_CHANGE & iAttributes)
		puserInfo->usri1_flags |= UF_PASSWD_CANT_CHANGE;
	else
		puserInfo->usri1_flags &= ~UF_PASSWD_CANT_CHANGE;

	if (SCAU_DISABLE_ACCOUNT & iAttributes)
		puserInfo->usri1_flags |= UF_ACCOUNTDISABLE;
	else
		puserInfo->usri1_flags &= ~UF_ACCOUNTDISABLE;

	if (SCAU_PASSWD_CHANGE_REQD_ON_LOGIN & iAttributes) // TODO: for some reason this doesn't work
		puserInfo->usri1_flags |= UF_PASSWORD_EXPIRED;
	else
		puserInfo->usri1_flags &= ~UF_PASSWORD_EXPIRED;
}


/********************************************************************
 CreateUser - CUSTOM ACTION ENTRY POINT for creating users

  Input:  deferred CustomActionData - UserName\tDomain\tPassword\tAttributes\tGroupName\tDomain\tGroupName\tDomain...
 * *****************************************************************/
extern "C" UINT __stdcall CreateUser(
	__in MSIHANDLE hInstall
	)
{
	//AssertSz(0, "Debug CreateUser");

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;
	UINT ui = 0;

	LPWSTR pwzData = NULL;
	LPWSTR pwz = NULL;
	LPWSTR pwzName = NULL;
	LPWSTR pwzDomain = NULL;
	LPWSTR pwzPassword = NULL;
	LPWSTR pwzGroup = NULL;
	LPWSTR pwzGroupDomain = NULL;
	PDOMAIN_CONTROLLER_INFOW pDomainControllerInfo = NULL;
	DWORD dwReturn = 0;
	int iAttributes = 0;

	USER_INFO_1 userInfo;
	USER_INFO_1* puserInfo = NULL;
	DWORD dw;
	LPCWSTR wz = NULL;

	hr = WcaInitialize(hInstall, "CreateUser");
	ExitOnFailure(hr, "failed to initialize");

	hr = WcaGetProperty( L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzData);

	//
	// Read in the CustomActionData
	//
	pwz = pwzData;
	hr = WcaReadStringFromCaData(&pwz, &pwzName);
	ExitOnFailure(hr, "failed to read user name from custom action data");

	hr = WcaReadStringFromCaData(&pwz, &pwzDomain);
	ExitOnFailure(hr, "failed to read domain from custom action data");

	hr = WcaReadStringFromCaData(&pwz, &pwzPassword);
	ExitOnFailure(hr, "failed to read password from custom action data");

	hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
	ExitOnFailure(hr, "failed to read attributes from custom action data");

	if (!(SCAU_DONT_CREATE_USER & iAttributes))
	{
		::ZeroMemory(&userInfo, sizeof(USER_INFO_1));
		userInfo.usri1_name = pwzName;
		userInfo.usri1_priv = USER_PRIV_USER;
		userInfo.usri1_flags = UF_SCRIPT;
		userInfo.usri1_home_dir = NULL;
		userInfo.usri1_comment = NULL;
		userInfo.usri1_script_path = NULL;

		SetUserPasswordAndAttributes(&userInfo, pwzPassword, iAttributes);

		//
		// Create the User
		//
		if (pwzDomain && *pwzDomain)
		{
			dwReturn = DsGetDcNameW( NULL, (LPCWSTR)pwzDomain, NULL, NULL, NULL, &pDomainControllerInfo );
			if( dwReturn == ERROR_NO_SUCH_DOMAIN )
			wz = pwzDomain;
			else
				wz = pDomainControllerInfo->DomainControllerName + 2;  //Add 2 so that we don't get the \\ prefix
		}
		ui = ::NetUserAdd(wz, 1, (LPBYTE)&userInfo, &dw);
		if (NERR_UserExists == ui)
		{
			if (SCAU_UPDATE_IF_EXISTS & iAttributes)
			{
				ui = ::NetUserGetInfo(wz, pwzName, 1, (LPBYTE*)&puserInfo);
				if (NERR_Success == ui)
				{
					// Change the existing user's password and attributes again then try
					// to update user with this new data
					SetUserPasswordAndAttributes(puserInfo, pwzPassword, iAttributes);

					ui = ::NetUserSetInfo(wz, pwzName, 1, (LPBYTE)puserInfo, &dw);
				}
			}
			else if (!(SCAU_FAIL_IF_EXISTS & iAttributes))
				ui = NERR_Success;
		}
		else if (NERR_PasswordTooShort == ui || NERR_PasswordTooLong == ui)
		{
			MessageExitOnFailure1(hr = HRESULT_FROM_WIN32(ui), msierrUSRFailedUserCreatePswd, "failed to create user: %S due to invalid password.", pwzName);
		}
		MessageExitOnFailure1(hr = HRESULT_FROM_WIN32(ui), msierrUSRFailedUserCreate, "failed to create user: %S", pwzName);
	}

	//
	// Add the users to groups
	//
	while (S_OK == (hr = WcaReadStringFromCaData(&pwz, &pwzGroup)))
	{
		hr = WcaReadStringFromCaData(&pwz, &pwzGroupDomain);
		ExitOnFailure1(hr, "failed to get domain for group: %S", pwzGroup);

		hr = AddUserToGroup(pwzName, pwzDomain, pwzGroup, pwzGroupDomain);
		MessageExitOnFailure2(hr, msierrUSRFailedUserGroupAdd, "failed to add user: %S to group %S", pwzName, pwzGroup);
	}
	if (E_NOMOREITEMS == hr) // if there are no more items, all is well
		hr = S_OK;
	ExitOnFailure1(hr, "failed to get next group in which to include user:%S", pwzName);

LExit:
	if (puserInfo)
		::NetApiBufferFree((LPVOID)puserInfo);

	if (pDomainControllerInfo)
		::NetApiBufferFree((LPVOID)pDomainControllerInfo);

	ReleaseStr(pwzData);
	ReleaseStr(pwzName);
	ReleaseStr(pwzDomain);
	ReleaseStr(pwzPassword);
	ReleaseStr(pwzGroup);
	ReleaseStr(pwzGroupDomain);

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/********************************************************************
 RemoveUser - CUSTOM ACTION ENTRY POINT for removing users

  Input:  deferred CustomActionData - Name\tDomain
 * *****************************************************************/
extern "C" UINT __stdcall RemoveUser(
	MSIHANDLE hInstall
	)
{
	//AssertSz(0, "Debug RemoveAccount");

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;
	UINT ui = 0;

	LPWSTR pwzData = NULL;
	LPWSTR pwz = NULL;
	LPWSTR pwzName = NULL;
	LPWSTR pwzDomain= NULL;
	LPCWSTR wz = NULL;
	PDOMAIN_CONTROLLER_INFOW pDomainControllerInfo = NULL;
	DWORD dwReturn = 0;

	hr = WcaInitialize(hInstall, "RemoveUser");
	ExitOnFailure(hr, "failed to initialize");

	hr = WcaGetProperty(L"CustomActionData", &pwzData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzData);

	//
	// Read in the CustomActionData
	//
	pwz = pwzData;
	hr = WcaReadStringFromCaData(&pwz, &pwzName);
	ExitOnFailure(hr, "failed to read name from custom action data");

	hr = WcaReadStringFromCaData(&pwz, &pwzDomain);
	ExitOnFailure(hr, "failed to read domain from custom action data");

	//
	// Remove the User Account
	//
	if (pwzDomain && *pwzDomain)
	{
		dwReturn = DsGetDcNameW( NULL, (LPCWSTR)pwzDomain, NULL, NULL, NULL, &pDomainControllerInfo );
		if( dwReturn == ERROR_NO_SUCH_DOMAIN )
		wz = pwzDomain;
		else
			wz = pDomainControllerInfo->DomainControllerName + 2;  //Add 2 so that we don't get the \\ prefix
	}

	ui = ::NetUserDel(wz, pwzName);
	if (NERR_UserNotFound == ui)
	{
		ui = NERR_Success;
	}
	ExitOnFailure1(hr = HRESULT_FROM_WIN32(ui), "failed to delete user account: %S", pwzName);

LExit:
	if (pDomainControllerInfo)
		::NetApiBufferFree((LPVOID)pDomainControllerInfo);

	ReleaseStr(pwzData);
	ReleaseStr(pwzName);
	ReleaseStr(pwzDomain);

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;

	return WcaFinalize(er);
}

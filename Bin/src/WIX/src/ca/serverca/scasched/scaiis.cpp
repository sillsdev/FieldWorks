//-------------------------------------------------------------------------------------------------
// <copyright file="scaiis.h" company="Microsoft">
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
//    IIS functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// globals
LPWSTR vpwzCustomActionData = NULL;
DWORD vdwCustomActionCost = 0;

// prototypes
static HRESULT ScaAddToMetabaseConfiguration(LPCWSTR pwzData, DWORD dwCost);

HRESULT ScaMetabaseTransaction(LPCWSTR wzBackup)
{
	HRESULT hr = S_OK;

	// TODO: These functions have been reported to hang IIS (O11:51709).  They may have been fixed in IIS6, but if not, need to be re-written the hard way

	hr = WcaDoDeferredAction(L"StartMetabaseTransaction", wzBackup, COST_IIS_TRANSACTIONS);
	ExitOnFailure(hr, "Failed to schedule StartMetabaseTransaction");

	hr = WcaDoDeferredAction(L"RollbackMetabaseTransaction", wzBackup, 0);   // rollback cost is irrelevant
	ExitOnFailure(hr, "Failed to schedule RollbackMetabaseTransaction");

	hr = WcaDoDeferredAction(L"CommitMetabaseTransaction", wzBackup, 0);  // commit is free
	ExitOnFailure(hr, "Failed to schedule StartMetabaseTransaction");

LExit:
	return hr;
}


HRESULT ScaCreateWeb(IMSAdminBase* piMetabase, LPCWSTR wzWeb, LPCWSTR wzWebBase)
{
	Assert(piMetabase);

	HRESULT hr = S_OK;
	UINT ui = 0;

	hr = ScaCreateMetabaseKey(piMetabase, wzWebBase, L"");
	ExitOnFailure(hr, "Failed to create web");

	hr = ScaWriteMetabaseValue(piMetabase, wzWebBase, L"", MD_KEY_TYPE, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)L"IIsWebServer");
	ExitOnFailure(hr, "Failed to set key type for web");

	hr = ScaCreateMetabaseKey(piMetabase, wzWebBase, L"Root");
	ExitOnFailure(hr, "Failed to create web root");

	hr = ScaWriteMetabaseValue(piMetabase, wzWebBase, L"Root", MD_KEY_TYPE, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)L"IIsWebVirtualDir");
	ExitOnFailure(hr, "Failed to set key type for web root");

	ui = 0x4000003e; // 1073741886;	// default directory browsing rights
	hr = ScaWriteMetabaseValue(piMetabase, wzWebBase, L"Root", MD_DIRECTORY_BROWSING, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)ui));
	ExitOnFailure(hr, "Failed to set directory browsing for web");

	hr = ScaCreateMetabaseKey(piMetabase, wzWebBase, L"Filters");
	ExitOnFailure(hr, "Failed to create web filters root");

	hr = ScaWriteMetabaseValue(piMetabase, wzWebBase, L"Filters", MD_KEY_TYPE, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)L"IIsFilters");
	ExitOnFailure(hr, "Failed to set key for web filters root");

	hr = ScaWriteMetabaseValue(piMetabase, wzWebBase, L"Filters", MD_FILTER_LOAD_ORDER, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)L"");
	ExitOnFailure(hr, "Failed to set empty load order for web");

LExit:
	return hr;
}


HRESULT ScaCreateApp(IMSAdminBase* piMetabase, LPCWSTR wzWebRoot,
					 DWORD dwIsolation)
{
	Assert(piMetabase);
	Unused(piMetabase);

	HRESULT hr = S_OK;
	WCHAR wzKey[METADATA_MAX_NAME_LEN];
	BOOL fInProc = FALSE;

	WCHAR* pwzCustomActionData = NULL;

	hr = WcaWriteIntegerToCaData(MBA_CREATEAPP, &pwzCustomActionData);
	ExitOnFailure(hr, "Failed to add metabase create app directive to CustomActionData");

	StringCchCopyW(wzKey, countof(wzKey), wzWebRoot);
	hr = WcaWriteStringToCaData(wzKey, &pwzCustomActionData);
	ExitOnFailure(hr, "Failed to add metabase key to CustomActionData");

	if (0 == dwIsolation)
		fInProc = TRUE;
	else
		fInProc = FALSE;

	hr = WcaWriteIntegerToCaData(fInProc, &pwzCustomActionData);
	ExitOnFailure(hr, "Failed to add isolation value to CustomActionData");

	hr = ScaAddToMetabaseConfiguration(pwzCustomActionData, COST_IIS_CREATEAPP);
	ExitOnFailure2(hr, "Failed to add ScaCreateApp action data: %S cost: %d", pwzCustomActionData, COST_IIS_CREATEAPP);

LExit:
	ReleaseStr(pwzCustomActionData);

	return hr;
}


HRESULT ScaAddFilterToLoadOrder(IMSAdminBase* piMetabase, LPCWSTR wzFilterRoot,
								LPCWSTR wzFilter, int iLoadOrder)
{
	Assert(piMetabase);

	HRESULT hr = S_OK;

	METADATA_HANDLE mhRoot = 0;
	int i;
	int cFilter = lstrlenW(wzFilter);

	METADATA_RECORD mr;
	DWORD dwRequired = 0;
	DWORD cchData = 255;
	::ZeroMemory(&mr, sizeof(mr));
	mr.dwMDIdentifier = MD_FILTER_LOAD_ORDER;
	mr.dwMDAttributes = METADATA_NO_ATTRIBUTES;
	mr.dwMDUserType = IIS_MD_UT_SERVER;
	mr.dwMDDataType = ALL_METADATA;
	mr.dwMDDataLen = cchData;
	mr.pbMDData = (BYTE*)new WCHAR[mr.dwMDDataLen];
	ExitOnNull(mr.pbMDData, hr, E_OUTOFMEMORY, "failed to allocate memory for MDData in metadata record");
	::ZeroMemory(mr.pbMDData, mr.dwMDDataLen * sizeof(WCHAR));

	WCHAR* pwzLoadOrder = NULL;
	DWORD cchLoadOrder = 0;
	LPWSTR pwz = NULL;

	hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, wzFilterRoot, METADATA_PERMISSION_READ | METADATA_PERMISSION_WRITE, 10, &mhRoot);
	for (i = 0; i < 30 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i++)
	{
		::Sleep(1000);
		WcaLog(LOGMSG_STANDARD, "Failed to open root key, retrying %d time(s)...", i);
		hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, wzFilterRoot, METADATA_PERMISSION_READ | METADATA_PERMISSION_WRITE, 10, &mhRoot);
	}

	if (SUCCEEDED(hr))
	{
		hr = piMetabase->GetData(mhRoot, L"", &mr, &dwRequired);
		if (HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) == hr)
		{
			mr.dwMDDataLen = cchData = dwRequired;
			delete [] mr.pbMDData;
			mr.pbMDData = (BYTE*)new WCHAR[mr.dwMDDataLen];
			ExitOnNull(mr.pbMDData, hr, E_OUTOFMEMORY, "failed to allocate memory for MDData in metadata record");
			::ZeroMemory(mr.pbMDData, mr.dwMDDataLen * sizeof(WCHAR));

			hr = piMetabase->GetData(mhRoot, L"", &mr, &dwRequired);
		}
	}

	//
	// Allow adding a Filter if /Filters node or /Filters/FilterLoadOrder property
	// doesn't exist (yet)
	//
	if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr || MD_ERROR_DATA_NOT_FOUND == hr)
		hr = S_OK;
	ExitOnFailure(hr, "Failed to get filter load order");

	//
	// If the filter name ends with '\0' or ',' and
	// the filter name begins at the beginning of the list or with ','
	// Then we've found the exact filter by name.
	//
	// If the filter isn't already in the load order, add it
	//
	if (mr.pbMDData)
	{
		pwz = const_cast<LPWSTR>(wcsstr(reinterpret_cast<LPCWSTR>(mr.pbMDData), wzFilter));

		if (NULL != pwz &&
			(L'\0' == *(pwz + cFilter) || L',' == *(pwz + cFilter)) &&
			(pwz == reinterpret_cast<LPCWSTR>(mr.pbMDData) || L',' == *(pwz-1)))
		{
		}
		else
		{
			pwz = NULL;
			if (0 <= iLoadOrder)
			{
				pwz = (LPWSTR)mr.pbMDData;
				for (i = 0; i < iLoadOrder && pwz; i++)
				{
					pwz = wcsstr(pwz, L",");
				}
			}

			cchLoadOrder = mr.dwMDDataLen + cFilter + 1;
			pwzLoadOrder = new WCHAR[cchLoadOrder];
			ExitOnNull(pwzLoadOrder, hr, E_OUTOFMEMORY, "failure while trying to set load order");

			*pwzLoadOrder = 0;
			if (NULL == pwz)	// put the filter at the end of the order
			{
				// tack on a comma if there are other filters in the order
				if (*mr.pbMDData)
				{
					StringCchCopyW(pwzLoadOrder, cchLoadOrder, (LPCWSTR)mr.pbMDData);
					StringCchCatW(pwzLoadOrder, cchLoadOrder, L",");
				}

				StringCchCatW(pwzLoadOrder, cchLoadOrder, wzFilter);
			}
			else if (L',' == *pwz)	// put the filter in the middle of the order
			{
				*pwz = 0;
				StringCchPrintfW(pwzLoadOrder, cchLoadOrder, L"%s,%s,%s", reinterpret_cast<LPWSTR>(mr.pbMDData), wzFilter, pwz + 1);
			}
			else	// put the filter at the beginning of the order
			{
				StringCchPrintfW(pwzLoadOrder, cchLoadOrder, L"%s,%s", wzFilter, reinterpret_cast<LPWSTR>(mr.pbMDData));
			}

			hr = ScaWriteMetabaseValue(piMetabase, wzFilterRoot, L"", MD_FILTER_LOAD_ORDER, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)pwzLoadOrder);
			ExitOnFailure(hr, "Failed to write filter load order to metabase");
		}
	}

LExit:
	if (pwzLoadOrder)
		delete [] pwzLoadOrder;

	if (mr.pbMDData)
		delete [] mr.pbMDData;

	if (mhRoot)
		piMetabase->CloseKey(mhRoot);

	return hr;
}


HRESULT ScaRemoveFilterFromLoadOrder(IMSAdminBase* piMetabase,
									 LPCWSTR wzFilterRoot, LPCWSTR wzFilter)
{
	Assert(piMetabase);
	HRESULT hr = S_OK;

	METADATA_HANDLE mhRoot = 0;
	int i;
	int cFilter = lstrlenW(wzFilter);

	LPCWSTR pwzStart = NULL;
	LPCWSTR pwzFind = NULL;
	LPCWSTR pwzNext = NULL;
	LPWSTR pwzLoadOrder = NULL;
	DWORD cchLoadOrder = 0;

	DWORD cchData = 0;
	METADATA_RECORD mr;
	::ZeroMemory(&mr, sizeof(mr));
	mr.dwMDIdentifier = MD_FILTER_LOAD_ORDER;
	mr.dwMDAttributes = METADATA_NO_ATTRIBUTES;
	mr.dwMDUserType = IIS_MD_UT_SERVER;
	mr.dwMDDataType = ALL_METADATA;
	mr.dwMDDataLen = cchData = 0;
	mr.pbMDData = NULL;

	// open the filter metabase key
	hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, wzFilterRoot, METADATA_PERMISSION_READ | METADATA_PERMISSION_WRITE, 10, &mhRoot);
	for (i = 0; i < 30 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i++)
	{
		::Sleep(1000);
		WcaLog(LOGMSG_STANDARD, "Failed to open root key, retrying %d time(s)...", i);
		hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, wzFilterRoot, METADATA_PERMISSION_READ | METADATA_PERMISSION_WRITE, 10, &mhRoot);
	}

	if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr || MD_ERROR_DATA_NOT_FOUND == hr)
	{
		WcaLog(LOGMSG_STANDARD, "No Filter to remove at path: '%S'", wzFilterRoot);
		hr = S_OK;
		ExitFunction();
	}
	ExitOnFailure(hr, "Failed to open filter root key");

	hr = MetaGetValue(piMetabase, mhRoot, L"", &mr);
	ExitOnFailure1(hr, "Failed to get load order for '%S'", wzFilterRoot);
	ExitOnNull1(mr.pbMDData, hr, E_UNEXPECTED, "Failed to get load order for '%S'", wzFilterRoot);

	pwzStart = (LPWSTR)mr.pbMDData;
	do
	{
		if (!pwzFind)
			pwzFind = pwzStart;
		else
			pwzFind += cFilter;

		pwzFind = wcsstr(pwzFind, wzFilter);
		if (!pwzFind)
			break;

		//
		// Make sure to only match [wzFilter] and NOT foobar[wzFilter]
		//
		if ( pwzFind > pwzStart && L',' != *(pwzFind-1) )
			continue;

		if (L',' == *(pwzFind + cFilter))
			pwzNext = pwzFind + cFilter + 1;
		else if(L'\0' == *(pwzFind + cFilter))
			pwzNext = pwzFind + cFilter;
	}while (!pwzNext);

	if (!pwzFind)
	{
		hr = S_FALSE;
		WcaLog(LOGMSG_STANDARD, "Filter '%S' was not load order: '%S'", wzFilter, wzFilterRoot);
		ExitFunction();
	}

	cchLoadOrder = mr.dwMDDataLen + 1;
	pwzLoadOrder = new WCHAR[cchLoadOrder];
	::ZeroMemory(pwzLoadOrder, (mr.dwMDDataLen + 1) * sizeof(WCHAR));

	//
	// The substring of FilterLoadOrder PRIOR to "Filter", not including trailing ","
	// If "Filter" was first, string remains "" because of ZeroMemory(pwzLoadOrder) above
	//
	if (pwzFind != pwzStart)
	{
		StringCchCopyNW(pwzLoadOrder, cchLoadOrder, pwzStart, pwzFind - pwzStart - 1);
	}

	//
	// If the "Filter" being removed is not first or last, add a ","
	//
	if (pwzFind != pwzStart && NULL != *pwzNext )
	{
		StringCchCatW(pwzLoadOrder, cchLoadOrder, L",");
	}

	//
	// The substring of FilterLoadOrder AFTER "Filter", not including any leading ","
	// pwzNext is "" if "Filter" is last
	//
	StringCchCatW(pwzLoadOrder, cchLoadOrder, pwzNext);

	//
	// TODO: Finally, remove ANY other instance of wzFilter in pwzLoadOrder
	//

	hr = ScaWriteMetabaseValue(piMetabase, wzFilterRoot, L"", MD_FILTER_LOAD_ORDER, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)pwzLoadOrder);
	ExitOnFailure1(hr, "Failed to remove Filter '%S'from load order", wzFilter);

LExit:
	if (pwzLoadOrder)
		delete [] pwzLoadOrder;

	MetaFreeValue(&mr);

	if (mhRoot)
		piMetabase->CloseKey(mhRoot);

	return hr;
}


HRESULT ScaCreateMetabaseKey(IMSAdminBase* piMetabase, LPCWSTR wzRootKey,
							 LPCWSTR wzSubKey)
{
	Assert(piMetabase);
	Unused(piMetabase);

	HRESULT hr = S_OK;
	WCHAR wzKey[METADATA_MAX_NAME_LEN];
	WCHAR* pwzCustomActionData = NULL;

	StringCchCopyW(wzKey, countof(wzKey), wzRootKey);
	if (L'/' != *(wzKey + lstrlenW(wzRootKey)))
		StringCchCatW(wzKey, countof(wzKey), L"/");
	if (wzSubKey && *wzSubKey)
	{
		if (L'/' == *wzSubKey)
			StringCchCatW(wzKey, countof(wzKey), wzSubKey + 1);
		else
			StringCchCatW(wzKey, countof(wzKey), wzSubKey);
	}

	hr = WcaWriteIntegerToCaData(MBA_CREATEKEY, &pwzCustomActionData);
	ExitOnFailure(hr, "Failed to add metabase delete key directive to CustomActionData");

	hr = WcaWriteStringToCaData(wzKey, &pwzCustomActionData);
	ExitOnFailure(hr, "Failed to add metabase key to CustomActionData");

	hr = ScaAddToMetabaseConfiguration(pwzCustomActionData, COST_IIS_CREATEKEY);
	ExitOnFailure2(hr, "Failed to add ScaCreateMetabaseKey action data: %S cost: %d", pwzCustomActionData, COST_IIS_CREATEKEY);

LExit:
	ReleaseStr(pwzCustomActionData);

	return hr;
}


HRESULT ScaDeleteMetabaseKey(IMSAdminBase* piMetabase, LPCWSTR wzRootKey,
							 LPCWSTR wzSubKey)
{
	Assert(piMetabase);
	Unused(piMetabase);

	HRESULT hr = S_OK;
	WCHAR wzKey[METADATA_MAX_NAME_LEN];
	WCHAR* pwzCustomActionData = NULL;

	StringCchCopyW(wzKey, countof(wzKey), wzRootKey);
	if (L'/' != *(wzKey + lstrlenW(wzRootKey)))
		StringCchCatW(wzKey, countof(wzKey), L"/");
	if (*wzSubKey)
	{
		if (L'/' == *wzSubKey)
			StringCchCatW(wzKey, countof(wzKey), wzSubKey + 1);
		else
			StringCchCatW(wzKey, countof(wzKey), wzSubKey);
	}

	hr = WcaWriteIntegerToCaData(MBA_DELETEKEY, &pwzCustomActionData);
	ExitOnFailure(hr, "Failed to add metabase delete key directive to CustomActionData");

	hr = WcaWriteStringToCaData(wzKey, &pwzCustomActionData);
	ExitOnFailure(hr, "Failed to add metabase key to CustomActionData");

	hr = ScaAddToMetabaseConfiguration(pwzCustomActionData, COST_IIS_DELETEKEY);
	ExitOnFailure2(hr, "Failed to add ScaDeleteMetabaseKey action data: %S cost: %d", pwzCustomActionData, COST_IIS_DELETEKEY);

LExit:
	ReleaseStr(pwzCustomActionData);

	return hr;
}


HRESULT ScaWriteMetabaseValue(IMSAdminBase* piMetabase, LPCWSTR wzRootKey,
							  LPCWSTR wzSubKey, DWORD dwIdentifier,
							  DWORD dwAttributes, DWORD dwUserType,
							  DWORD dwDataType, LPVOID pvData)
{
	Assert(piMetabase && (pvData || (DWORD_METADATA == dwDataType)));	// pvData may be 0 if it is DWORD data
	Unused(piMetabase);

	HRESULT hr = S_OK;
	WCHAR wzKey[METADATA_MAX_NAME_LEN];
	WCHAR* pwzCustomActionData = NULL;

	StringCchCopyW(wzKey, countof(wzKey), wzRootKey);
	if (L'/' != *(wzKey + lstrlenW(wzRootKey)))
		StringCchCatW(wzKey, countof(wzKey), L"/");
	if (wzSubKey && *wzSubKey)
	{
		if (L'/' == *wzSubKey)
			StringCchCatW(wzKey, countof(wzKey), wzSubKey + 1);
		else
			StringCchCatW(wzKey, countof(wzKey), wzSubKey);
	}

	hr = WcaWriteIntegerToCaData(MBA_WRITEKEY, &pwzCustomActionData);
	ExitOnFailure(hr, "Failed to add metabase write key directive to CustomActionData");

	hr = WcaWriteStringToCaData(wzKey, &pwzCustomActionData);
	ExitOnFailure(hr, "Failed to add metabase key to CustomActionData");

	hr = WcaWriteIntegerToCaData(dwIdentifier, &pwzCustomActionData);
	ExitOnFailure(hr, "Failed to add metabase identifier to CustomActionData");

	hr = WcaWriteIntegerToCaData(dwAttributes, &pwzCustomActionData);
	ExitOnFailure(hr, "Failed to add metabase attributes to CustomActionData");

	hr = WcaWriteIntegerToCaData(dwUserType, &pwzCustomActionData);
	ExitOnFailure(hr, "Failed to add metabase user type to CustomActionData");

	hr = WcaWriteIntegerToCaData(dwDataType, &pwzCustomActionData);
	ExitOnFailure(hr, "Failed to add metabase data type to CustomActionData");

	switch (dwDataType)
	{
	case DWORD_METADATA:
		hr = WcaWriteIntegerToCaData((DWORD)((DWORD_PTR)pvData), &pwzCustomActionData);
		break;
	case STRING_METADATA:
		hr = WcaWriteStringToCaData((LPCWSTR)pvData, &pwzCustomActionData);
		break;
	case MULTISZ_METADATA:
		{
		// change NULLs to unprintable character  to create a 'safe' MULTISZ string
		LPWSTR pwz = (LPWSTR)pvData;
		for (;;)
		{
			if ('\0' == *pwz)
			{
				*pwz = MAGIC_MULTISZ_CHAR;
				if ('\0' == *(pwz + 1))	// second null back to back means end of string
					break;
			}

			pwz++;
		}

		hr = WcaWriteStringToCaData((LPCWSTR)pvData, &pwzCustomActionData);
		}
		break;
	case BINARY_METADATA:
		hr = WcaWriteStreamToCaData(((BLOB*) pvData)->pBlobData, ((BLOB*) pvData)->cbSize, &pwzCustomActionData);
		break;
	default:
		hr = E_UNEXPECTED;
	}
	ExitOnFailure(hr, "Failed to add metabase data to CustomActionData");

	// TODO: maybe look the key up and make sure we're not just writing the same value that already there

	hr = ScaAddToMetabaseConfiguration(pwzCustomActionData, COST_IIS_WRITEKEY);
	ExitOnFailure2(hr, "Failed to add ScaWriteMetabaseValue action data: %S, cost: %d", pwzCustomActionData, COST_IIS_WRITEKEY);

LExit:
	ReleaseStr(pwzCustomActionData);

	return hr;
}

static HRESULT ScaAddToMetabaseConfiguration(LPCWSTR pwzData, DWORD dwCost)
{
	HRESULT hr = S_OK;

	hr = WcaWriteStringToCaData(pwzData, &vpwzCustomActionData);
	ExitOnFailure1(hr, "failed to add to metabase configuration data string: %S", pwzData);

	vdwCustomActionCost += dwCost;

LExit:
	return hr;
}

HRESULT ScaScheduleMetabaseConfiguration()
{
	HRESULT hr = S_OK;

	if (vpwzCustomActionData && *vpwzCustomActionData)
	{
		hr = WcaDoDeferredAction(L"WriteMetabaseChanges", vpwzCustomActionData, vdwCustomActionCost);
		ExitOnFailure(hr, "Failed to schedule ConfigureMetabase custom action");

		ReleaseStr(vpwzCustomActionData);
	}
	else
		hr = S_FALSE;

LExit:
	return hr;
}

HRESULT ScaLoadMetabase(IMSAdminBase** ppiMetabase)
{
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	// if IIS was uninstalled (thus no IID_IMSAdminBase) allow the
	// user to still uninstall this package by clicking "Ignore"
	do
	{
		hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, (void**)ppiMetabase);
		if (FAILED(hr))
		{
			WcaLog(LOGMSG_STANDARD, "failed to get IID_IMSAdminBase Object");
			er = WcaErrorMessage(msierrIISCannotConnect, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
			switch (er)
			{
			case IDABORT:
				ExitFunction();   // bail with the error result from the CoCreate to kick off a rollback
			case IDRETRY:
				hr = S_FALSE;   // hit me, baby, one more time
				break;
			case IDIGNORE:
				hr = S_OK;  // pretend everything is okay and bail
			}
		}
	} while (S_FALSE == hr);

LExit:
	return hr;
}

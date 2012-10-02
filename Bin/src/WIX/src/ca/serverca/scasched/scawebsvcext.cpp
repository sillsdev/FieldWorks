//-------------------------------------------------------------------------------------------------
// <copyright file="scawebsvcext.h" company="Microsoft">
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
//    IIS Web Service Extension Table functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// sql queries
LPCWSTR vcsWebSvcExtQuery = L"SELECT `Component_`, `File`, `Description`, `Group`, `Attributes` FROM `IIsWebServiceExtension`";
enum eWebSvcExtQuery { ldqComponent=1 , ldqFile, ldqDescription, ldqGroup, ldqAttributes };


LPCWSTR vcsWebSvcExtRoot = L"/LM/W3SVC";

// prototypes for private helper functions
static HRESULT AddWebSvcExtToList(
	__in SCA_WEBSVCEXT** ppsWseList
	);

static HRESULT ScaCheckWebSvcExtValue(
	__in IMSAdminBase* piMetabase,
	__in DWORD dwMDIdentifier
	);

static HRESULT ScaWebSvcExtInstall(
	__in LPWSTR *pwzWebSvcExtList,
	__in DWORD_PTR *pcchWebSvcExtList,
	__in SCA_WEBSVCEXT* psWseList
	);

static HRESULT ScaWebSvcExtUninstall(
	__in LPWSTR *pwzWebSvcExtList,
	__in const DWORD *pcchWebSvcExtList,
	__in SCA_WEBSVCEXT* psWseList
	);

// functions

HRESULT __stdcall ScaWebSvcExtRead(
	__in SCA_WEBSVCEXT** ppsWseList
	)
{
	Assert(ppsWseList);

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;
	PMSIHANDLE hView;
	PMSIHANDLE hRec;
	LPWSTR pwzData = NULL;
	INSTALLSTATE isInstalled = INSTALLSTATE_UNKNOWN;
	INSTALLSTATE isAction = INSTALLSTATE_UNKNOWN;
	SCA_WEBSVCEXT* psWebSvcExt = NULL;

	// check to see if necessary tables are specified
	if (S_OK != WcaTableExists(L"IIsWebServiceExtension"))
	{
		WcaLog(LOGMSG_VERBOSE, "Skipping ScaInstallWebSvcExt() because IIsWebServiceExtension table not present");
		hr = S_FALSE;
		goto LExit;
	}

	// loop through all the web service extensions
	hr = WcaOpenExecuteView(vcsWebSvcExtQuery, &hView);
	ExitOnFailure(hr, "Failed to open view on IIsWebServiceExtension table");
	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		// Get the Component first.  If the Component is not being modified during
		// this transaction, skip processing this whole record.
		hr = WcaGetRecordString(hRec, ldqComponent, &pwzData);
		ExitOnFailure(hr, "Failed to get Component for WebSvcExt");

		er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &isInstalled, &isAction);
		hr = HRESULT_FROM_WIN32(er);
		ExitOnFailure(hr, "Failed to get Component state for WebSvcExt");

		if (!WcaIsInstalling(isInstalled, isAction) &&
			!WcaIsReInstalling(isInstalled, isAction) &&
			!WcaIsUninstalling(isInstalled, isAction))
		{
			continue; // skip this record.
		}

		hr = AddWebSvcExtToList(ppsWseList);
		ExitOnFailure(hr, "failed to add element to web svc ext list");

		psWebSvcExt = *ppsWseList;
		Assert(psWebSvcExt);

		psWebSvcExt->isInstalled = isInstalled;
		psWebSvcExt->isAction = isAction;

		hr = WcaGetRecordFormattedString(hRec, ldqFile, &pwzData);
		ExitOnFailure(hr, "Failed to get File for WebSvcExt");
		hr = StringCchCopyW(psWebSvcExt->wzFile, countof(psWebSvcExt->wzFile), pwzData);
		ExitOnFailure(hr, "Failed to copy File for WebSvcExt");

		hr = WcaGetRecordFormattedString(hRec, ldqDescription, &pwzData);
		ExitOnFailure(hr, "Failed to get Description for WebSvcExt");
		hr = StringCchCopyW(psWebSvcExt->wzDescription, countof(psWebSvcExt->wzDescription), pwzData);
		ExitOnFailure(hr, "Failed to copy Description for WebSvcExt");

		hr = WcaGetRecordFormattedString(hRec, ldqGroup, &pwzData);
		ExitOnFailure(hr, "Failed to get Group for WebSvcExt");
		hr = StringCchCopyW(psWebSvcExt->wzGroup, countof(psWebSvcExt->wzGroup), pwzData);
		ExitOnFailure(hr, "Failed to copy Group for WebSvcExt");

		hr = WcaGetRecordInteger(hRec, ldqAttributes, &psWebSvcExt->iAttributes);
		ExitOnFailure(hr, "Failed to get Attributes for WebSvcExt");
	}

	if (E_NOMOREITEMS == hr)
		hr = S_OK;
	ExitOnFailure(hr, "Failure while processing WebSvcExt");

LExit:
	ReleaseStr(pwzData);

	return hr;
}


// Commit does both install and uninstall
HRESULT __stdcall ScaWebSvcExtCommit(
	__in IMSAdminBase* piMetabase,
	__in SCA_WEBSVCEXT* psWseList
	)
{
	Assert(piMetabase);

	HRESULT hr = S_OK;
	METADATA_RECORD mr;

	LPWSTR wzWebSvcExtList = NULL;
	DWORD cbWebSvcExtList = 0;
	DWORD_PTR cchWebSvcExtList = 0;

	if(!psWseList)
	{
		WcaLog(LOGMSG_VERBOSE, "Skipping ScaWebSvcExtCommit() because there are no web service extensions in the list");
		ExitFunction();
	}

	// Get current set of web service extensions.
	::ZeroMemory(&mr, sizeof(mr));
	mr.dwMDIdentifier = MD_WEB_SVC_EXT_RESTRICTION_LIST;
	mr.dwMDAttributes = 0;
	mr.dwMDUserType  = IIS_MD_UT_SERVER;
	mr.dwMDDataType = ALL_METADATA;
	mr.pbMDData = NULL;
	mr.dwMDDataLen = 0;

	hr = piMetabase->GetData(METADATA_MASTER_ROOT_HANDLE, vcsWebSvcExtRoot, &mr, &cbWebSvcExtList);
	if (MD_ERROR_DATA_NOT_FOUND == hr)
	{
		WcaLog(LOGMSG_VERBOSE, "Skipping ScaWebSvcExtCommit() because WebSvcExtRestrictionList value is not present");
		ExitFunction();
	}
	else if (HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) == hr)
	{
		// cchWebSvcExtList is returned in bytes. Convert to WCHAR size to call StrAlloc
		cchWebSvcExtList = cbWebSvcExtList / sizeof(WCHAR);
		hr = StrAlloc(&wzWebSvcExtList, cchWebSvcExtList);
		ExitOnFailure(hr, "Failed allocating space for web service extensions");
	}
	else
	{
		ExitOnFailure(hr, "Failed retrieving web service extensions");
	}

	mr.pbMDData = (unsigned char*)wzWebSvcExtList;
	mr.dwMDDataLen = cbWebSvcExtList;

	hr = piMetabase->GetData(METADATA_MASTER_ROOT_HANDLE, vcsWebSvcExtRoot, &mr, &cbWebSvcExtList);
	ExitOnFailure(hr, "Failed retrieving web service extensions");

	// Make changes to local copy of metabase
	while (psWseList)
	{
		if (WcaIsInstalling(psWseList->isInstalled, psWseList->isAction))
		{
			hr = ScaWebSvcExtInstall(&wzWebSvcExtList, &cchWebSvcExtList, psWseList);
			ExitOnFailure(hr, "Failed to install Web Service extension");
		}
		else if (WcaIsUninstalling(psWseList->isInstalled, psWseList->isAction))
		{
			hr = ScaWebSvcExtUninstall(&wzWebSvcExtList, (DWORD *)&cchWebSvcExtList, psWseList);
			ExitOnFailure(hr,"Failed to uninstall Web Service extension");
		}

		psWseList = psWseList->psWseNext;
	}

	// Write Metabase
	hr = ScaWriteMetabaseValue(piMetabase, vcsWebSvcExtRoot, NULL, MD_WEB_SVC_EXT_RESTRICTION_LIST, METADATA_INHERIT, IIS_MD_UT_FILE, MULTISZ_METADATA, wzWebSvcExtList);
	ExitOnFailure1(hr, "Failed to write WebServiceExtensions: '%S'", wzWebSvcExtList);

LExit:
	ReleaseStr(wzWebSvcExtList);

	return hr;
}


HRESULT __stdcall ScaWebSvcExtInstall(
	__in LPWSTR *ppwzWebSvcExtList,
	__in DWORD_PTR *pcchWebSvcExtList,
	__in SCA_WEBSVCEXT* psWseList
	)
{
	Assert( ppwzWebSvcExtList && pcchWebSvcExtList && psWseList);
	Assert(*ppwzWebSvcExtList);

	HRESULT hr = S_OK;

	LPWSTR pwzWebSvcExt = NULL;
	DWORD cchWebSvcExt = 0;
	int iAllow;
	int iUiDeletable;

	BOOL fAlreadyExists = FALSE;
	DWORD_PTR dwIndex = -1;
	LPCWSTR wzFoundString = NULL;

	// Check if it's already in there
	hr = MultiSzFindSubstring(*ppwzWebSvcExtList, psWseList->wzFile, &dwIndex, &wzFoundString);
	ExitOnFailure1(hr, "failed to search for string:%S in web service extension MULTISZ", psWseList->wzFile);

	if (S_FALSE != hr && NULL != wcsstr(wzFoundString, psWseList->wzGroup) && NULL != wcsstr(wzFoundString, psWseList->wzDescription))
	{
		fAlreadyExists = TRUE;
	}

	// If it doesn't already exist, add it
	if (!fAlreadyExists)
	{
		// Construct the single string in the format required for the WebSvc Ext list in metabase
		iAllow = (psWseList->iAttributes & 1);
		iUiDeletable = ((psWseList->iAttributes >> 1) & 1);
		hr = StrAllocFormatted(&pwzWebSvcExt, L"%d,%s,%d,%s,%s",iAllow, psWseList->wzFile, iUiDeletable, psWseList->wzGroup, psWseList->wzDescription);
		ExitOnFailure(hr, "Failure allocating space for web service extensions");

		hr = MultiSzPrepend(ppwzWebSvcExtList, pcchWebSvcExtList, pwzWebSvcExt);
		ExitOnFailure1(hr, "failed to prepend web service extention string: %S", pwzWebSvcExt);
	}

LExit:
	ReleaseStr(pwzWebSvcExt);

	return hr;
}


HRESULT __stdcall ScaWebSvcExtUninstall(
	__in LPWSTR *ppwzWebSvcExtList,
	__in const DWORD
	*pcchWebSvcExtList,
	__in SCA_WEBSVCEXT* psWseList
	)
{
	Assert(ppwzWebSvcExtList && *ppwzWebSvcExtList && pcchWebSvcExtList && psWseList);
	Assert(*ppwzWebSvcExtList);

	HRESULT hr = S_OK;
	DWORD_PTR dwIndex = -1;
	LPCWSTR wzFoundString = NULL;

	// Find the string to remove
	hr = MultiSzFindSubstring(*ppwzWebSvcExtList, psWseList->wzFile, &dwIndex, &wzFoundString);
	ExitOnFailure1(hr, "failed to search for string:%S in web service extension MULTISZ", psWseList->wzFile);

	// If we found a match (ignoring the Allow and Deletable flags)
	if (S_FALSE != hr && NULL != wcsstr(wzFoundString, psWseList->wzGroup) && NULL != wcsstr(wzFoundString, psWseList->wzDescription))
	{
		hr = MultiSzRemoveString(ppwzWebSvcExtList, dwIndex);
		ExitOnFailure1(hr, "failed to remove string: %d from web service extension MULTISZ", dwIndex);
	}

LExit:
	return hr;
}


static HRESULT ScaCheckWebSvcExtValue(
	__in IMSAdminBase* piMetabase,
	__in DWORD dwMDIdentifier
	)
{
	if (!piMetabase)
	{
		return E_INVALIDARG;
	}

	HRESULT hr = S_OK;
	METADATA_RECORD mr = { 0 };
	DWORD cch = 0;

	mr.dwMDIdentifier = dwMDIdentifier;
	mr.dwMDUserType  = IIS_MD_UT_SERVER;

	hr = piMetabase->GetData(METADATA_MASTER_ROOT_HANDLE, vcsWebSvcExtRoot, &mr, &cch);
	if(HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) == hr)
	{
		hr = S_OK;
	}
	else if(MD_ERROR_DATA_NOT_FOUND == hr)
	{
		hr = S_FALSE;
	}

	return hr;
}


void ScaWebSvcExtFreeList(
	__in SCA_WEBSVCEXT* psWseList
	)
{
	SCA_WEBSVCEXT* psWseDelete = psWseList;
	while (psWseList)
	{
		psWseDelete = psWseList;
		psWseList = psWseList->psWseNext;
		MemFree(psWseDelete);
	}
}


static HRESULT AddWebSvcExtToList(
	__in SCA_WEBSVCEXT** ppsWseList
	)
{
	HRESULT hr = S_OK;

	SCA_WEBSVCEXT* psWse = (SCA_WEBSVCEXT*)MemAlloc(sizeof(SCA_WEBSVCEXT), TRUE);
	ExitOnNull(psWse, hr, E_OUTOFMEMORY, "failed to allocate element for web svc ext list");

	psWse->psWseNext = *ppsWseList;
	*ppsWseList = psWse;

LExit:
	return hr;
}

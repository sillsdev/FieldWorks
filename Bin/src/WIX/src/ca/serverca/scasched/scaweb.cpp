//-------------------------------------------------------------------------------------------------
// <copyright file="scaweb.h" company="Microsoft">
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
//    IIS Web Table functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

//Adding this because delivery doesn't have the updated specstrings.h that windows build does
#ifndef __in_xcount
#define __in_xcount(size)
#endif

// sql queries
LPCWSTR vcsWebQuery = L"SELECT `Web`, `Component_`, `Description`, `ConnectionTimeout`, `Directory_`, `State`, `Attributes`, `DirProperties_`, `Application_`, "
					  L"`Address`, `IP`, `Port`, `Header`, `Secure`, `Log_` FROM `IIsWebSite`, `IIsWebAddress` "
					  L"WHERE `KeyAddress_`=`Address` ORDER BY `Sequence`";

enum eWebQuery { wqWeb = 1, wqComponent , wqDescription, wqConnectionTimeout, wqDirectory,
				 wqState, wqAttributes, wqProperties, wqApplication, wqAddress, wqIP, wqPort, wqHeader, wqSecure, wqLog};

LPCWSTR vcsWebAddressQuery = L"SELECT `Address`, `IP`, `Port`, `Header`, `Secure` "
							 L"FROM `IIsWebAddress` WHERE `Web_`=?";
enum eWebAddressQuery { waqAddress = 1, waqIP, waqPort, waqHeader, waqSecure };


LPCWSTR vcsWebBaseQuery = L"SELECT `Web`, `IP`, `Port`, `Header`, `Secure` "
						  L"FROM `IIsWebSite`, `IIsWebAddress` "
						  L"WHERE `KeyAddress_`=`Address` AND `Web`=?";
enum eWebBaseQuery { wbqWeb = 1, wbqIP, wbqPort, wbqHeader, wbqSecure};



// prototypes for private helper functions
static SCA_WEB* NewWeb();
static SCA_WEB* AddWebToList(
	__in SCA_WEB* pswList,
	__in SCA_WEB* psw
	);
static HRESULT ScaWebFindBase(
	__in IMSAdminBase* piMetabase,
	__in SCA_WEB* pswList,
	__in LPCWSTR wzWeb,
	__in LPCWSTR wzIP,
	__in int iPort,
	__in LPCWSTR wzHeader,
	__in BOOL fSecure,
	__out_ecount(*pcchWebBase) LPWSTR wzWebBase,
	__inout DWORD* pcchWebBase
	);
static HRESULT ScaWebFindFreeBase(
	__in IMSAdminBase* piMetabase,
	__in_xcount(unknown) SCA_WEB* pswList,
	__out_ecount(cchWebBase) LPWSTR wzWebBase,
	__in DWORD cchWebBase
	);
static HRESULT ScaWebWrite(
	__in IMSAdminBase* piMetabase,
	__in SCA_WEB* psw,
	__in SCA_APPPOOL * psapList
	);
static HRESULT ScaWebRemove(
	__in IMSAdminBase* piMetabase,
	__in const SCA_WEB* psw);
static void Sort(
	__in_ecount(cArray) DWORD dwArray[],
	__in int cArray
	);


HRESULT ScaWebsRead(
	__in IMSAdminBase* piMetabase,
	__in SCA_WEB** ppswList,
	__in SCA_HTTP_HEADER** ppshhList,
	__in SCA_WEB_ERROR** ppsweList
	)
{
	Assert(piMetabase && ppswList);

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	BOOL fIIsWebSiteTable = FALSE;
	BOOL fIIsWebAddressTable = FALSE;
	BOOL fIIsWebApplicationTable = FALSE;

	PMSIHANDLE hView, hRec;
	PMSIHANDLE hViewAddresses, hRecAddresses;
	PMSIHANDLE hViewApplications, hRecApplications;

	SCA_WEB* psw = NULL;
	LPWSTR pwzData = NULL;

	DWORD dwLen = 0;

	// check to see what tables are available
	fIIsWebSiteTable = (S_OK == WcaTableExists(L"IIsWebSite"));
	fIIsWebAddressTable = (S_OK == WcaTableExists(L"IIsWebAddress"));
	fIIsWebApplicationTable = (S_OK == WcaTableExists(L"IIsWebApplication"));

	if (!fIIsWebSiteTable || !fIIsWebAddressTable)
	{
		WcaLog(LOGMSG_VERBOSE, "Required tables not present");
		hr = S_FALSE;
		goto LExit;
	}

	// open the view on webs' addresses
	hr = WcaOpenView(vcsWebAddressQuery, &hViewAddresses);
	ExitOnFailure(hr, "Failed to open view on IIsWebAddress table");

	// open the view on webs' applications
	if (fIIsWebApplicationTable)
	{
		hr = WcaOpenView(vcsWebApplicationQuery, &hViewApplications);
		ExitOnFailure(hr, "Failed to open view on IIsWebApplication table");
	}

	// loop through all the webs
	hr = WcaOpenExecuteView(vcsWebQuery, &hView);
	ExitOnFailure(hr, "Failed to execute view on IIsWebSite table");
	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		psw = NewWeb();
		if (!psw)
		{
			hr = E_OUTOFMEMORY;
			break;
		}

		// get the darwin information
		hr = WcaGetRecordString(hRec, wqWeb, &pwzData);
		ExitOnFailure(hr, "Failed to get Web");
		StringCchCopyW(psw->wzKey, countof(psw->wzKey), pwzData);

		// get component install state
		hr = WcaGetRecordString(hRec, wqComponent, &pwzData);
		ExitOnFailure(hr, "Failed to get Component for Web");
		StringCchCopyW(psw->wzComponent, countof(psw->wzComponent), pwzData);
		if (*(psw->wzComponent))
		{
			psw->fHasComponent = TRUE;

			er = ::MsiGetComponentStateW(WcaGetInstallHandle(), psw->wzComponent, &psw->isInstalled, &psw->isAction);
			hr = HRESULT_FROM_WIN32(er);
			ExitOnFailure(hr, "Failed to get web Component state");
		}

		// get the web's key address
		hr = WcaGetRecordString(hRec, wqAddress, &pwzData);
		ExitOnFailure(hr, "Failed to get Address for Web");
		StringCchCopyW(psw->swaKey.wzKey, countof(psw->swaKey.wzKey), pwzData);

		hr = WcaGetRecordFormattedString(hRec, wqIP, &pwzData);
		ExitOnFailure(hr, "Failed to get IP for Web");
		StringCchCopyW(psw->swaKey.wzIP, countof(psw->swaKey.wzIP), pwzData);

		hr = WcaGetRecordFormattedString(hRec, wqPort, &pwzData);
		ExitOnFailure(hr, "Failed to get Web Address port");
		psw->swaKey.iPort = wcstol(pwzData, NULL, 10);
		if (0 == psw->swaKey.iPort)
			ExitOnFailure1(hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA), "invalid port provided for web site: %S", psw->wzDescription);

		hr = WcaGetRecordFormattedString(hRec, wqHeader, &pwzData);
		ExitOnFailure(hr, "Failed to get Header for Web");
		StringCchCopyW(psw->swaKey.wzHeader, countof(psw->swaKey.wzHeader), pwzData);

		hr = WcaGetRecordInteger(hRec, wqSecure, &psw->swaKey.fSecure);
		ExitOnFailure(hr, "Failed to get if Web is secure");
		if (S_FALSE == hr)
			psw->swaKey.fSecure = FALSE;

		// TODO: fix this to look for the description as well (or is address enough)?
		// find the web root
		dwLen = METADATA_MAX_NAME_LEN;
		hr = ScaWebFindBase(piMetabase, *ppswList,
							psw->wzKey,
							psw->swaKey.wzIP,
							psw->swaKey.iPort,
							psw->swaKey.wzHeader,
							psw->swaKey.fSecure,
							psw->wzWebBase, &dwLen);
		if (S_OK == hr)
		{
			psw->fBaseExists = TRUE;
		}
		else if (S_FALSE == hr && FALSE == psw->fHasComponent) // if we're not installing it, fail if it wasn't found
		{
			ExitOnFailure1(hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND), "failed to find web site: '%S'", psw->wzKey);
		}
		else if (S_FALSE == hr)
		{
			dwLen = METADATA_MAX_NAME_LEN;
			hr = ScaWebFindFreeBase(piMetabase, *ppswList, psw->wzWebBase, dwLen);
			psw->fBaseExists = FALSE;
		}
		ExitOnFailure(hr, "Failed to find web root");

		// get any extra web addresses
		hr = WcaExecuteView(hViewAddresses, hRec);
		ExitOnFailure(hr, "Failed to execute view on extra IIsWebAddress table");
		while (S_OK == (hr = WcaFetchRecord(hViewAddresses, &hRecAddresses)))
		{
			if (MAX_ADDRESSES_PER_WEB <= psw->cExtraAddresses)
			{
				hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
				ExitOnFailure(hr, "Failure to get more extra web addresses, max exceeded.");
			}

			hr = WcaGetRecordString(hRecAddresses, waqAddress, &pwzData);
			ExitOnFailure(hr, "Failed to get extra web Address");

			// if this isn't the key address add it
			if (0 != lstrcmpW(pwzData, psw->swaKey.wzKey))
			{
				StringCchCopyW(psw->swaExtraAddresses[psw->cExtraAddresses].wzKey,
					countof(psw->swaExtraAddresses[psw->cExtraAddresses].wzKey), pwzData);

				hr = WcaGetRecordFormattedString(hRecAddresses, waqIP, &pwzData);
				ExitOnFailure(hr, "Failed to get extra web IP");
				StringCchCopyW(psw->swaExtraAddresses[psw->cExtraAddresses].wzIP, countof(psw->swaExtraAddresses[psw->cExtraAddresses].wzIP), pwzData);

				hr = WcaGetRecordFormattedString(hRecAddresses, waqPort, &pwzData);
				ExitOnFailure(hr, "Failed to get port for extra web IP");
				psw->swaExtraAddresses[psw->cExtraAddresses].iPort= wcstol(pwzData, NULL, 10);

				hr = WcaGetRecordFormattedString(hRecAddresses, waqHeader, &pwzData);
				ExitOnFailure(hr, "Failed to get header for extra web IP");
				StringCchCopyW(psw->swaExtraAddresses[psw->cExtraAddresses].wzHeader, countof(psw->swaExtraAddresses[psw->cExtraAddresses].wzHeader), pwzData);

				hr = WcaGetRecordInteger(hRecAddresses, waqSecure, &psw->swaExtraAddresses[psw->cExtraAddresses].fSecure);
				ExitOnFailure(hr, "Failed to get if secure extra web IP");
				if (S_FALSE == hr)
					psw->swaExtraAddresses[psw->cExtraAddresses].fSecure = FALSE;

				psw->cExtraAddresses++;
			}
		}

		if (E_NOMOREITEMS == hr)
			hr = S_OK;
		ExitOnFailure(hr, "Failure occured while getting extra web addresses");

		// get the web's description
		hr = WcaGetRecordFormattedString(hRec, wqDescription, &pwzData);
		ExitOnFailure(hr, "Failed to get Description for Web");
		StringCchCopyW(psw->wzDescription, countof(psw->wzDescription), pwzData);

		hr = WcaGetRecordInteger(hRec, wqConnectionTimeout, &psw->iConnectionTimeout);
		ExitOnFailure(hr, "Failed to get connection timeout for Web");

		if (psw->fHasComponent) // If we're installing it, it needs a dir
		{
			// get the web's directory
			hr = WcaGetRecordString(hRec, wqDirectory, &pwzData);
			ExitOnFailure(hr, "Failed to get Directory for Web");

			WCHAR wzPath[MAX_PATH];
			dwLen = countof(wzPath);
			if (INSTALLSTATE_SOURCE == psw->isAction)
				er = ::MsiGetSourcePathW(WcaGetInstallHandle(), pwzData, wzPath, &dwLen);
			else
				er = ::MsiGetTargetPathW(WcaGetInstallHandle(), pwzData, wzPath, &dwLen);
			hr = HRESULT_FROM_WIN32(er);
			ExitOnFailure(hr, "Failed to get Source/TargetPath for Directory");

			if (dwLen > countof(wzPath))
			{
				hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
				ExitOnFailure(hr, "Failed because Source/TargetPath for Directory was greater than MAX_PATH.");
			}

			// remove traling backslash
			if (dwLen > 0 && wzPath[dwLen-1] == L'\\')
				wzPath[dwLen-1] = 0;
			StringCchCopyW(psw->wzDirectory, countof(psw->wzDirectory), wzPath);
		}

		hr = WcaGetRecordInteger(hRec, wqState, &psw->iState);
		ExitOnFailure(hr, "Failed to get state for Web");

		hr = WcaGetRecordInteger(hRec, wqAttributes, &psw->iAttributes);
		ExitOnFailure(hr, "Failed to get attributes for Web");

		// get the dir properties for this web
		hr = WcaGetRecordString(hRec, wqProperties, &pwzData);
		ExitOnFailure(hr, "Failed to get directory properties for Web");
		if (*pwzData)
		{
			hr = ScaGetWebDirProperties(pwzData, &psw->swp);
			ExitOnFailure(hr, "Failed to get directory properties for Web");

			psw->fHasProperties = TRUE;
		}

		// get the application information for this web
		hr = WcaGetRecordString(hRec, wqApplication, &pwzData);
		ExitOnFailure(hr, "Failed to get application identifier for Web");
		if (*pwzData)
		{
			hr = ScaGetWebApplication(NULL, pwzData, &psw->swapp);
			ExitOnFailure(hr, "Failed to get application for Web");

			psw->fHasApplication = TRUE;
		}

		// get the SSL certificates
		hr = ScaSslCertificateRead(psw->wzKey, &(psw->pswscList));
		ExitOnFailure(hr, "Failed to get SSL Certificates.");

		// get the custom headers
		if (*ppshhList)
		{
			hr = ScaGetHttpHeader(hhptWeb, psw->wzKey, ppshhList, &(psw->pshhList));
			ExitOnFailure(hr, "Failed to get Custom HTTP Headers");
		}

		// get the errors
		if (*ppsweList)
		{
			hr = ScaGetWebError(weptWeb, psw->wzKey, ppsweList, &(psw->psweList));
			ExitOnFailure(hr, "Failed to get Custom Errors");
		}

		// get the log information for this web
		hr = WcaGetRecordString(hRec, wqLog, &pwzData);
		ExitOnFailure(hr, "Failed to get log identifier for Web");
		if (*pwzData)
		{
			hr = ScaGetWebLog(piMetabase, pwzData, &psw->swl);
			ExitOnFailure(hr, "Failed to get Log for Web.");

			psw->fHasLog = TRUE;
		}

		*ppswList = AddWebToList(*ppswList, psw);
		psw = NULL; // set the web NULL so it doesn't accidentally get freed below
	}

	if (E_NOMOREITEMS == hr)
		hr = S_OK;

LExit:
	// if anything was left over after an error clean it all up
	if (psw)
		ScaWebsFreeList(psw);

	ReleaseStr(pwzData);

	return hr;
}


HRESULT ScaWebsGetBase(
	__in IMSAdminBase* piMetabase,
	__in SCA_WEB* pswList,
	__in LPCWSTR wzWeb,
	__out_ecount(*pcchWebBase)LPWSTR wzWebBase,
	__inout DWORD* pcchWebBase
	)
{
	HRESULT hr = S_OK;
	PMSIHANDLE hView, hRec;

	WCHAR wzIP[MAX_PATH];
	int iPort = -1;
	WCHAR wzHeader[MAX_PATH];
	BOOL fSecure = FALSE;

	LPWSTR pwzData = NULL;

	hr = WcaTableExists(L"IIsWebSite");
	if (S_FALSE == hr)
		hr = E_ABORT;
	ExitOnFailure(hr, "IIsWebSite table does not exists or there was an error");

	hr = WcaTableExists(L"IIsWebAddress");
	if (S_FALSE == hr)
		hr = E_ABORT;
	ExitOnFailure(hr, "IIsWebAddress table does not exists or there was an error");

	hRec = ::MsiCreateRecord(1);
	hr = WcaSetRecordString(hRec, 1, wzWeb);
	ExitOnFailure(hr, "Failed to set record to look up Web base");

	hr = WcaOpenView(vcsWebBaseQuery, &hView);
	ExitOnFailure(hr, "Failed to open view on IIsWebSite table to find web base");
	hr = WcaExecuteView(hView, hRec);
	ExitOnFailure(hr, "Failed to execute view on IIsWebSite table");

	// get the web information
	hr = WcaFetchSingleRecord(hView, &hRec);
	if (S_OK == hr)
	{
		// get the data to search for
		hr = WcaGetRecordFormattedString(hRec, wbqIP, &pwzData);
		ExitOnFailure(hr, "Failed to get IP for Web for VirtualDir");
		StringCchCopyW(wzIP, countof(wzIP), pwzData);

		hr = WcaGetRecordFormattedString(hRec, wbqPort, &pwzData);
		ExitOnFailure(hr, "Failed to get port for extra web IP");
		iPort= wcstol(pwzData, NULL, 10);

		hr = WcaGetRecordFormattedString(hRec, wbqHeader, &pwzData);
		ExitOnFailure(hr, "Failed to get Header for Web for VirtualDir");
		StringCchCopyW(wzHeader, countof(wzHeader), pwzData);

		hr = WcaGetRecordInteger(hRec, wbqSecure, &fSecure);
		if (S_FALSE == hr)
			fSecure = FALSE;

		// find the web or find the next free web location
		hr = ScaWebFindBase(piMetabase, pswList, wzWeb, wzIP, iPort, wzHeader, fSecure, wzWebBase, pcchWebBase);
		if (S_FALSE == hr)
			hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
		ExitOnFailure(hr, "Failed to find Web base");
	}
	else if (S_FALSE == hr)
		hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);

LExit:
	ReleaseStr(pwzData);

	return hr;
}


HRESULT ScaWebsInstall(
	__in IMSAdminBase* piMetabase,
	__in SCA_WEB* pswList,
	__in SCA_APPPOOL * psapList
	)
{
	HRESULT hr = S_OK;
	SCA_WEB* psw = pswList;

	while (psw)
	{
		// if we are installing the web site
		if (psw->fHasComponent && WcaIsInstalling(psw->isInstalled, psw->isAction))
		{
			hr = ScaWebWrite(piMetabase, psw, psapList);
			ExitOnFailure1(hr, "failed to write web '%S' to metabase", psw->wzKey);
		}

		psw = psw->pswNext;
	}

LExit:
	return hr;
}


HRESULT ScaWebsUninstall(
	__in IMSAdminBase* piMetabase,
	__in SCA_WEB* pswList
	)
{
	HRESULT hr = S_OK;
	SCA_WEB* psw = pswList;

	while (psw)
	{
		// if we are uninstalling the web site
		if (psw->fHasComponent && WcaIsUninstalling(psw->isInstalled, psw->isAction))
		{
			hr = ScaWebRemove(piMetabase, psw);
			ExitOnFailure1(hr, "Failed to remove web '%S' from metabase", psw->wzKey);
		}

		psw = psw->pswNext;
	}

LExit:
	return hr;
}


void ScaWebsFreeList(
	__in SCA_WEB* pswList
	)
{
	SCA_WEB* pswDelete = pswList;
	while (pswList)
	{
		pswDelete = pswList;
		pswList = pswList->pswNext;

		// Free the SSL, headers and errors list first
		ScaSslCertificateFreeList(pswDelete->pswscList);
		ScaHttpHeaderFreeList(pswDelete->pshhList);
		ScaWebErrorFreeList(pswDelete->psweList);
		MemFree(pswDelete);
	}
}


// private helper functions

static SCA_WEB* NewWeb()
{
	SCA_WEB* psw = (SCA_WEB*)MemAlloc(sizeof(SCA_WEB), TRUE);
	Assert(psw);
	return psw;
}


static SCA_WEB* AddWebToList(
	__in SCA_WEB* pswList,
	__in SCA_WEB* psw
	)
{
	if (pswList)
	{
		SCA_WEB* pswTemp = pswList;
		while (pswTemp->pswNext)
		{
			pswTemp = pswTemp->pswNext;
		}

		pswTemp->pswNext = psw;
	}
	else
	{
		pswList = psw;
	}

	return pswList;
}


static HRESULT ScaWebFindBase(
	__in IMSAdminBase* piMetabase,
	__in SCA_WEB* pswList,
	__in LPCWSTR wzWeb,
	__in LPCWSTR wzIP,
	__in int iPort,
	__in LPCWSTR wzHeader,
	__in BOOL fSecure,
	__out_ecount(*pcchWebBase) LPWSTR wzWebBase,
	__inout DWORD* pcchWebBase
	)
{
	Assert(piMetabase && pcchWebBase);

	HRESULT hr = S_OK;
	BOOL fFound = FALSE;

	WCHAR wzKey[METADATA_MAX_NAME_LEN];
	WCHAR wzSubkey[METADATA_MAX_NAME_LEN];
	DWORD dwIndex = 0;
	DWORD dwLen = 0;

	METADATA_RECORD mr;
	::ZeroMemory(&mr, sizeof(mr));

	METADATA_RECORD mrAddress;
	::ZeroMemory(&mrAddress, sizeof(mrAddress));

	// try to find the web in memory first
	for (SCA_WEB* psw = pswList; psw; psw = psw->pswNext)
	{
		if (0 == lstrcmpW(wzWeb, psw->wzKey))
		{
			if ((0 == lstrcmpW(wzIP, psw->swaKey.wzIP) || 0 == lstrcmpW(wzIP, L"*") || 0 == lstrcmpW(psw->swaKey.wzIP, L"*")) &&
				iPort == psw->swaKey.iPort &&
				0 == lstrcmpW(wzHeader, psw->swaKey.wzHeader) &&
				fSecure == psw->swaKey.fSecure)
			{
				// if the passed in buffer wasn't big enough
				dwLen = lstrlenW(psw->wzWebBase);
				if (*pcchWebBase < dwLen)
					hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
				else
					StringCchCopyW(wzWebBase, *pcchWebBase, psw->wzWebBase);
				*pcchWebBase = dwLen;

				fFound = TRUE;
				break;
			}
			else
			{
				WcaLog(LOGMSG_STANDARD, "Found web `%S`but data did not match.", wzWeb);
				hr = E_UNEXPECTED;
				break;
			}
		}
	}
	ExitOnFailure(hr, "Failure occured while searching for web in memory");

	// if we didn't find a web in memory matching look in the metabase
	if (!fFound)
	{
		LPWSTR pwzExists = NULL;
		LPCWSTR pwzIPExists = NULL;
		LPCWSTR pwzPortExists = NULL;
		int iPortExists = 0;
		LPCWSTR pwzHeaderExists = NULL;

		mr.dwMDIdentifier = MD_KEY_TYPE;
		mr.dwMDAttributes = METADATA_INHERIT;
		mr.dwMDUserType = IIS_MD_UT_SERVER;
		mr.dwMDDataType = ALL_METADATA;
		mr.dwMDDataLen = 0;
		mr.pbMDData = NULL;

		mrAddress.dwMDIdentifier = (fSecure) ? MD_SECURE_BINDINGS : MD_SERVER_BINDINGS;
		mrAddress.dwMDAttributes = METADATA_INHERIT;
		mrAddress.dwMDUserType = IIS_MD_UT_SERVER;
		mrAddress.dwMDDataType = ALL_METADATA;
		mrAddress.dwMDDataLen = 0;
		mrAddress.pbMDData = NULL;

		// loop through the "web keys" looking for the "IIsWebServer" key that matches wzWeb
		for (dwIndex = 0; SUCCEEDED(hr); dwIndex++)
		{
			hr = piMetabase->EnumKeys(METADATA_MASTER_ROOT_HANDLE, L"/LM/W3SVC", wzSubkey, dwIndex);
			if (SUCCEEDED(hr))
			{
				StringCchPrintfW(wzKey, countof(wzKey), L"/LM/W3SVC/%s", wzSubkey);
				hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzKey,
									  &mr);
				if (MD_ERROR_DATA_NOT_FOUND == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
				{
					hr = S_FALSE;  // didn't find anything, try next one
					continue;
				}
				ExitOnFailure(hr, "Failed to get key from metabase while searching for web servers");

				// if we have an IIsWebServer store the key
				if (0 == lstrcmpW(L"IIsWebServer", (LPCWSTR)mr.pbMDData))
				{
					hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzKey,
									  &mrAddress);
					if (MD_ERROR_DATA_NOT_FOUND == hr)
					{
						hr = S_FALSE;
						continue;
					}

					ExitOnFailure(hr, "Failed to get address from metabase while searching for web servers");

					// break down the first address into parts
					pwzIPExists = (LPCWSTR)mrAddress.pbMDData;
					pwzExists = const_cast<LPWSTR>(wcsstr(pwzIPExists, L":"));
					*pwzExists = L'\0';

					pwzPortExists = pwzExists + 1;
					pwzExists = const_cast<LPWSTR>(wcsstr(pwzPortExists, L":"));
					*pwzExists = L'\0';
					iPortExists = wcstol(pwzPortExists, NULL, 10);

					pwzHeaderExists = pwzExists + 1;

					// compare the passed in address with the address listed for this web
					if (S_OK == hr &&
						(0 == lstrcmpW(wzIP, pwzIPExists) || 0 == lstrcmpW(wzIP, L"*")) &&
						iPort == iPortExists &&
						0 == lstrcmpW(wzHeader, pwzHeaderExists))
					{
						// if the passed in buffer wasn't big enough
						if (*pcchWebBase < mr.dwMDDataLen)
							hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
						else
							StringCchCopyW(wzWebBase, *pcchWebBase, wzKey);
						*pcchWebBase = mr.dwMDDataLen;

						fFound = TRUE;
						break;
					}
				}
			}
		}

		if (E_NOMOREITEMS == hr)
		{
			Assert(!fFound);
			hr = S_FALSE;
		}
	}

LExit:
	MetaFreeValue(&mr);
	MetaFreeValue(&mrAddress);

	if (!fFound && SUCCEEDED(hr))
		hr = S_FALSE;

	return hr;
}


static HRESULT ScaWebFindFreeBase(
	__in IMSAdminBase* piMetabase,
	__in_xcount(unknown) SCA_WEB* pswList,
	__out_ecount(cchWebBase) LPWSTR wzWebBase,
	__in DWORD cchWebBase
	)
{
	Assert(piMetabase);

	HRESULT hr = S_OK;

	WCHAR wzKey[METADATA_MAX_NAME_LEN];
	WCHAR wzSubkey[METADATA_MAX_NAME_LEN];
	DWORD* prgdwSubKeys = NULL;
	DWORD cSubKeys = 128;
	DWORD cSubKeysFilled = 0;
	DWORD dwIndex = 0;

	SCA_WEB* psw = NULL;
	DWORD i;
	DWORD dwKey;

	METADATA_RECORD mr;
	::ZeroMemory(&mr, sizeof(METADATA_RECORD));
	mr.dwMDIdentifier = MD_KEY_TYPE;
	mr.dwMDAttributes = 0;
	mr.dwMDUserType = IIS_MD_UT_SERVER;
	mr.dwMDDataType = STRING_METADATA;
	mr.dwMDDataLen = 0;
	mr.pbMDData = NULL;

	prgdwSubKeys = (DWORD*)MemAlloc(cSubKeys * sizeof(DWORD), TRUE);
	ExitOnNull(prgdwSubKeys, hr, E_OUTOFMEMORY, "failed to allocate space for web site keys");

	// loop through the "web keys" looking for the "IIsWebServer" key that matches wzWeb
	for (dwIndex = 0; SUCCEEDED(hr); dwIndex++)
	{
		hr = piMetabase->EnumKeys(METADATA_MASTER_ROOT_HANDLE, L"/LM/W3SVC", wzSubkey, dwIndex);
		if (SUCCEEDED(hr))
		{
			StringCchPrintfW(wzKey, countof(wzKey), L"/LM/W3SVC/%s", wzSubkey);
			hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzKey, &mr);
			if (MD_ERROR_DATA_NOT_FOUND == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
			{
				hr = S_FALSE;  // didn't find anything, try next one
				continue;
			}
			ExitOnFailure(hr, "Failed to get key from metabase while searching for free web root");

			// if we have a IIsWebServer get the address information
			if (0 == lstrcmpW(L"IIsWebServer", (LPCWSTR)mr.pbMDData))
			{
				if (cSubKeysFilled >= cSubKeys)
				{
					cSubKeys = cSubKeys * 2;
					prgdwSubKeys = (DWORD*)MemReAlloc(prgdwSubKeys, cSubKeys * sizeof(DWORD), FALSE);
					ExitOnNull(prgdwSubKeys, hr, E_OUTOFMEMORY, "failed to allocate space for web site keys");
				}

				prgdwSubKeys[cSubKeysFilled] = wcstol(wzSubkey, NULL, 10);
				cSubKeysFilled++;
				Sort(prgdwSubKeys, cSubKeysFilled);
			}
		}
	}

	if (E_NOMOREITEMS == hr)
		hr = S_OK;
	ExitOnFailure(hr, "Failed to find free web root");

	// add all the webs created in memory
	CONST WCHAR *pcchSlash;
	for (psw = pswList; psw; psw = psw->pswNext)
	{
		// find the last slash in the web root because the root # is after it
		pcchSlash = NULL;
		for (CONST WCHAR *pcch = psw->wzWebBase; pcch && *pcch; pcch++)
		{
			if (L'/' == *pcch)
				pcchSlash = pcch;
		}
		Assert(pcchSlash && L'/' == *pcchSlash); // should always find a slash

		prgdwSubKeys[cSubKeysFilled] = wcstol(pcchSlash + 1, NULL, 10);
		cSubKeysFilled++;
		Sort(prgdwSubKeys, cSubKeysFilled);

		if (cSubKeysFilled >= cSubKeys)
		{
			cSubKeys = cSubKeys * 2;
			prgdwSubKeys = (DWORD*)MemReAlloc(prgdwSubKeys, cSubKeys * sizeof(DWORD), FALSE);
			ExitOnNull(prgdwSubKeys, hr, E_OUTOFMEMORY, "failed to allocate space for web site keys");
		}
	}

	// find the lowest free web root
	dwKey  = 1;
	for (i = 0; i < cSubKeysFilled; i++)
	{
		if (dwKey < prgdwSubKeys[i])
			break;

		dwKey = prgdwSubKeys[i] + 1;
	}

	StringCchPrintfW(wzWebBase, cchWebBase, L"/LM/W3SVC/%u", dwKey);

LExit:
	MetaFreeValue(&mr);

	if (prgdwSubKeys)
		MemFree(prgdwSubKeys);

	return hr;
}


static HRESULT ScaWebWrite(
	__in IMSAdminBase* piMetabase,
	__in SCA_WEB* psw,
	__in SCA_APPPOOL * psapList)
{
	HRESULT hr = S_OK;

	UINT ui = 0;
	WCHAR wzIP[64];
	WCHAR wzBindings[1024];
	WCHAR wzSecureBindings[1024];
	WCHAR* pcchNext;        // used to properly create the MULTI_SZ
	DWORD cchPcchNext;
	WCHAR* pcchSecureNext ; // used to properly create the MULTI_SZ
	DWORD cchPcchSecureNext;

	// if the web root doesn't exist create it
	if (!psw->fBaseExists)
	{
		hr = ScaCreateWeb(piMetabase, psw->wzKey, psw->wzWebBase);
		ExitOnFailure(hr, "Failed to create web");
	}
	else if (psw->iAttributes & SWATTRIB_NOCONFIGUREIFEXISTS) // if we're not supposed to configure existing webs, bail
	{
		Assert(psw->fBaseExists);

		hr = S_FALSE;
		WcaLog(LOGMSG_VERBOSE, "Skipping configuration of existing web: %S", psw->wzKey);
		ExitFunction();
	}

	// put the secure and non-secure bindings together as MULTI_SZs
	::ZeroMemory(wzBindings, sizeof(wzBindings));
	pcchNext = wzBindings;
	cchPcchNext = countof(wzBindings);
	::ZeroMemory(wzSecureBindings, sizeof(wzSecureBindings));
	pcchSecureNext = wzSecureBindings;
	cchPcchSecureNext = countof(wzSecureBindings);

	// set the IP address appropriately
	if (0 == lstrcmpW(psw->swaKey.wzIP, L"*"))
		::ZeroMemory(wzIP, sizeof(wzIP));
	else
		StringCchCopyW(wzIP, countof(wzIP), psw->swaKey.wzIP);

	WCHAR wzBinding[256];
	StringCchPrintfW(wzBinding, countof(wzBinding), L"%s:%d:%s", wzIP, psw->swaKey.iPort, psw->swaKey.wzHeader);
	if (psw->swaKey.fSecure)
	{
		StringCchCopyW(pcchSecureNext, cchPcchSecureNext, wzBinding);
		pcchSecureNext += lstrlenW(wzBinding) + 1;
		cchPcchSecureNext -= lstrlenW(wzBinding) + 1;
	}
	else
	{
		StringCchCopyW(pcchNext, cchPcchNext, wzBinding);
		pcchNext += lstrlenW(wzBinding) + 1;
		cchPcchNext -= lstrlenW(wzBinding) + 1;
	}

	for (ui = 0; ui < psw->cExtraAddresses; ui++)
	{
		// set the IP address appropriately
		if (0 == lstrcmpW(psw->swaExtraAddresses[ui].wzIP, L"*"))
			::ZeroMemory(wzIP, sizeof(wzIP));
		else
			StringCchCopyW(wzIP, countof(wzIP), psw->swaExtraAddresses[ui].wzIP);

		StringCchPrintfW(wzBinding, countof(wzBinding), L"%s:%d:%s", wzIP, psw->swaExtraAddresses[ui].iPort, psw->swaExtraAddresses[ui].wzHeader);
		if (psw->swaExtraAddresses[ui].fSecure)
		{
			StringCchCopyW(pcchSecureNext, cchPcchSecureNext, wzBinding);
			pcchSecureNext += lstrlenW(wzBinding) + 1;
			cchPcchSecureNext -= lstrlenW(wzBinding) + 1;
		}
		else
		{
			StringCchCopyW(pcchNext, cchPcchNext, wzBinding);
			pcchNext += lstrlenW(wzBinding) + 1;
			cchPcchNext -= lstrlenW(wzBinding) + 1;
		}
	}

	// now write the bindings to the metabase
	hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_SERVER_BINDINGS, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, MULTISZ_METADATA, wzBindings);
	ExitOnFailure(hr, "Failed to write server bindings for Web");
	hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_SECURE_BINDINGS, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, MULTISZ_METADATA, wzSecureBindings);
	ExitOnFailure(hr, "Failed to write secure bindings for Web");

	// write the target path for the web's directory to the metabase
	hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"/Root", MD_VR_PATH, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, psw->wzDirectory);
	ExitOnFailure(hr, "Failed to write virtual root path for Web");

	// write the description for the web to the metabase
	hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_SERVER_COMMENT, METADATA_INHERIT, IIS_MD_UT_SERVER, STRING_METADATA, psw->wzDescription);
	ExitOnFailure(hr, "Failed to write description for Web");

	ui = psw->iConnectionTimeout;
	if(MSI_NULL_INTEGER != ui)
	{
		hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_CONNECTION_TIMEOUT, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)ui));
		ExitOnFailure(hr, "Failed to write connection timeout for Web");
	}

	ui = psw->iState;
	if (MSI_NULL_INTEGER != ui)
	{
		if (2 == ui)
		{
			ui = 1;
			hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_SERVER_AUTOSTART, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)ui));
			ExitOnFailure(hr, "Failed to write auto start flag for Web");
			ui = 2;
		}

		if (1 == ui || 2 == ui)
		{
			ui = 1; // start command
			hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_SERVER_COMMAND, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)ui));
			ExitOnFailure(hr, "Failed to start Web");
		}
		else if (0 == ui)
		{
			ui = 2; // stop command
			hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_SERVER_COMMAND, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)ui));
			ExitOnFailure(hr, "Failed to stop Web");
		}
		else
		{
			hr = E_UNEXPECTED;
			ExitOnFailure(hr, "Unexpected value for Web State");
		}
	}

	WCHAR wzRootOfWeb[METADATA_MAX_NAME_LEN];
	StringCchPrintfW(wzRootOfWeb, countof(wzRootOfWeb), L"%s/Root", psw->wzWebBase);

	// write the web dirproperties information
	if (psw->fHasProperties)
	{
		hr = ScaWriteWebDirProperties(piMetabase, wzRootOfWeb, &psw->swp);
		ExitOnFailure(hr, "Failed to write web security information to metabase");
	}

	// write the application information
	if (psw->fHasApplication)
	{
		hr = ScaWriteWebApplication(piMetabase, wzRootOfWeb, &psw->swapp, psapList);
		ExitOnFailure(hr, "Failed to write web application information to metabase");
	}

	// write the SSL certificate information
	if (psw->pswscList)
	{
		hr = ScaSslCertificateWriteMetabase(piMetabase, psw->wzWebBase, psw->pswscList);
		ExitOnFailure1(hr, "Failed to write SSL certificates for Web site: %S", psw->wzKey);
	}

	// write the headers
	if (psw->pshhList)
	{
		hr = ScaWriteHttpHeader(piMetabase, hhptWeb, psw->wzWebBase, psw->pshhList);
		ExitOnFailure1(hr, "Failed to write custom HTTP headers for Web site: %S", psw->wzKey);
	}

	// write the errors
	if (psw->psweList)
	{
		hr = ScaWriteWebError(piMetabase, weptWeb, psw->wzWebBase, psw->psweList);
		ExitOnFailure1(hr, "Failed to write custom web errors for Web site: %S", psw->wzKey);
	}

	// write the log information to the metabase
	if (psw->fHasLog)
	{
		hr = ScaWriteWebLog(piMetabase, psw->wzWebBase, &psw->swl);
		ExitOnFailure(hr, "Failed to write web log information to metabase");
	}

LExit:
	return hr;
}


static HRESULT ScaWebRemove(
	__in IMSAdminBase* piMetabase,
	__in const SCA_WEB* psw
	)
{
	HRESULT hr = S_OK;

	// simply remove the root key and everything else is pulled at the same time
	hr = ScaDeleteMetabaseKey(piMetabase, psw->wzWebBase, L"");
	ExitOnFailure1(hr, "Failed to remove web '%S' from metabase", psw->wzKey);

LExit:
	return hr;
}


// insertion sort
static void Sort(
	__in_ecount(cArray) DWORD dwArray[],
	__in int cArray
	)
{
	int i, j;
	DWORD dwData;

	for (i = 1; i < cArray; i++)
	{
		dwData = dwArray[i];

		j = i - 1;
		while (0 <= j && dwArray[j] > dwData)
		{
			dwArray[j + 1] = dwArray[j];
			j--;
		}

		dwArray[j + 1] = dwData;
	}
}

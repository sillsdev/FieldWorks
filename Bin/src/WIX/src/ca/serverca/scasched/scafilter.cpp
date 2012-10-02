#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scafilter.cpp" company="Microsoft">
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
//    IIS Filter functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// sql queries
LPCWSTR vcsFilterQuery = L"SELECT `Web_`, `Name`, `Component_`, `Path`, `Description`, `Flags`, `LoadOrder` "
						 L"FROM `IIsFilter`";
enum eFilterQuery { fqWeb = 1, fqFilter, fqComponent , fqPath, fqDescription, fqFlags, fqLoadOrder };

// prototypes
static HRESULT AddFilterToList(SCA_FILTER** ppsfList);


UINT __stdcall ScaFiltersRead(IMSAdminBase* piMetabase,
							  SCA_WEB* pswList, SCA_FILTER** ppsfList)
{
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;
	PMSIHANDLE hView, hRec;
	PMSIHANDLE hViewWeb, hRecWeb;

	BOOL fIIsWebFilterTable = FALSE;

	LPWSTR pwzData = NULL;

	SCA_FILTER* psf = NULL;
	DWORD dwLen = 0;

	// check for required table
	if (S_OK != WcaTableExists(L"IIsFilter"))
	{
		WcaLog(LOGMSG_VERBOSE, "Skipping ScaInstallFilters() - no IIsFilter table");
		ExitFunction1(hr = S_FALSE);
	}

	// loop through all the filters
	hr = WcaOpenExecuteView(vcsFilterQuery, &hView);
	ExitOnFailure(hr, "Failed to open view on IIsFilter table");
	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		hr = AddFilterToList(ppsfList);
		ExitOnFailure(hr, "failed to add filter to list");

		psf = *ppsfList;

		// get component install state
		hr = WcaGetRecordString(hRec, fqComponent, &pwzData);
		ExitOnFailure(hr, "Failed to get Filter.Component_");
		er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &psf->isInstalled, &psf->isAction);
		hr = HRESULT_FROM_WIN32(er);
		ExitOnFailure(hr, "Failed to get Component state for filter");

		::ZeroMemory(psf->wzFilterRoot, sizeof(psf->wzFilterRoot));

		hr = WcaGetRecordString(hRec, fqWeb, &pwzData);
		ExitOnFailure(hr, "Failed to get Web for VirtualDir");

		if (*pwzData)
		{
			dwLen = countof(psf->wzWebBase);
			hr = ScaWebsGetBase(piMetabase, pswList, pwzData, psf->wzWebBase, &dwLen);
			ExitOnFailure(hr, "Failed to get base of web for Filter");

			StringCchPrintfW(psf->wzFilterRoot, countof(psf->wzFilterRoot), L"%s/Filters", psf->wzWebBase);
		}
		else
		{
			StringCchCopyW(psf->wzFilterRoot, countof(psf->wzFilterRoot), L"/LM/W3SVC/Filters");
		}

		// filter key
		hr = WcaGetRecordString(hRec, fqFilter, &pwzData);
		ExitOnFailure(hr, "Failed to get Filter.Filter");
		StringCchCopyW(psf->wzKey, countof(psf->wzKey), pwzData);

		// filter path
		hr = WcaGetRecordFormattedString(hRec, fqPath, &pwzData);
		ExitOnFailure(hr, "Failed to get Filter.Path");
		StringCchCopyW(psf->wzPath, countof(psf->wzPath), pwzData);

		// filter description
		hr = WcaGetRecordFormattedString(hRec, fqDescription, &pwzData);
		ExitOnFailure(hr, "Failed to get Filter.Description");
		StringCchCopyW(psf->wzDescription, countof(psf->wzDescription), pwzData);

		// filter flags
		hr = WcaGetRecordInteger(hRec, fqFlags, &psf->iFlags);
		ExitOnFailure(hr, "Failed to get Filter.Flags");

		// filter load order
		hr = WcaGetRecordInteger(hRec, fqLoadOrder, &psf->iLoadOrder);
		ExitOnFailure(hr, "Failed to get Filter.LoadOrder");
	}

	if (E_NOMOREITEMS == hr)
		hr = S_OK;
	ExitOnFailure(hr, "Failure while processing filters");

LExit:
	ReleaseStr(pwzData);
	return hr;
}


HRESULT ScaFiltersInstall(IMSAdminBase* piMetabase, SCA_FILTER* psfList)
{
	HRESULT hr = S_OK;
	SCA_FILTER* psf = psfList;

	while (psf)
	{
		if (WcaIsInstalling(psf->isInstalled, psf->isAction))
		{
			hr = ScaCreateMetabaseKey(piMetabase, psf->wzFilterRoot, psf->wzKey);
			ExitOnFailure1(hr, "Failed to create key for filter '%S'", psf->wzKey);

			hr = ScaWriteMetabaseValue(piMetabase, psf->wzFilterRoot, psf->wzKey, MD_KEY_TYPE, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)L"IIsFilter");
			ExitOnFailure1(hr, "Failed to write key type for filter '%S'", psf->wzKey);

			// filter path
			hr = ScaWriteMetabaseValue(piMetabase, psf->wzFilterRoot, psf->wzKey, MD_FILTER_IMAGE_PATH, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)psf->wzPath);
			ExitOnFailure1(hr, "Failed to write Path for filter '%S'", psf->wzKey);

			// filter description
			hr = ScaWriteMetabaseValue(piMetabase, psf->wzFilterRoot, psf->wzKey, MD_FILTER_DESCRIPTION, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)psf->wzDescription);
			ExitOnFailure1(hr, "Failed to write Description for filter ''%S", psf->wzKey);

			// filter flags
			if (MSI_NULL_INTEGER != psf->iFlags)
			{
				hr = ScaWriteMetabaseValue(piMetabase, psf->wzFilterRoot, psf->wzKey, MD_FILTER_FLAGS, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)psf->iFlags));
				ExitOnFailure1(hr, "Failed to write Flags for filter '%S'", psf->wzKey);
			}

			// filter load order
			if (MSI_NULL_INTEGER != psf->iLoadOrder)
			{
				hr = ScaAddFilterToLoadOrder(piMetabase, psf->wzFilterRoot, psf->wzKey, psf->iLoadOrder);
				ExitOnFailure1(hr, "Failed to add filter '%S' to load order", psf->wzKey);
			}
		}

		psf = psf->psfNext;
	}

LExit:
	return hr;
}


HRESULT ScaFiltersUninstall(IMSAdminBase* piMetabase, SCA_FILTER* psfList)
{
	HRESULT hr = S_OK;
	SCA_FILTER* psf = psfList;

	while (psf)
	{
		if (WcaIsUninstalling(psf->isInstalled, psf->isAction))
		{
			hr = ScaRemoveFilterFromLoadOrder(piMetabase, psf->wzFilterRoot, psf->wzKey);
			ExitOnFailure1(hr, "Failed to remove filter '%S' from load order", psf->wzKey);

			// remove the filter from the load order and remove the filter's key
			hr = ScaDeleteMetabaseKey(piMetabase, psf->wzFilterRoot, psf->wzKey);
			ExitOnFailure1(hr, "Failed to remove web '%S' from metabase", psf->wzKey);
		}

		psf = psf->psfNext;
	}

LExit:
	return hr;
}


void ScaFiltersFreeList(SCA_FILTER* psfList)
{
	SCA_FILTER* psfDelete = psfList;
	while (psfList)
	{
		psfDelete = psfList;
		psfList = psfList->psfNext;

		MemFree(psfDelete);
	}
}


// private helper functions
static HRESULT AddFilterToList(SCA_FILTER** ppsfList)
{
	HRESULT hr = S_OK;
	SCA_FILTER* psf = (SCA_FILTER*)MemAlloc(sizeof(SCA_FILTER), TRUE);
	ExitOnNull(psf, hr, E_OUTOFMEMORY, "failed to add filter to filter list");

	psf->psfNext = *ppsfList;
	*ppsfList = psf;

LExit:
	return hr;
}

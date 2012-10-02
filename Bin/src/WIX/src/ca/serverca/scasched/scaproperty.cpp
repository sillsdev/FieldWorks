//-------------------------------------------------------------------------------------------------
// <copyright file="scaproperty.cpp" company="Microsoft">
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
//    IIS Property functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

/*------------------------------------------------------------------
IIsProperty table:

Property  Component_  Attributes  Value
s72      s72         i4          s255
------------------------------------------------------------------*/

// sql queries
LPCWSTR vcsPropertyQuery = L"SELECT `Property`, `Component_`, `Attributes`, `Value` "
						 L"FROM `IIsProperty`";

enum ePropertyQuery { sqProperty = 1, sqComponent, sqAttributes, sqValue };


// prototypes
static HRESULT AddPropertyToList(
	SCA_PROPERTY** ppspList
	);


// functions
void ScaPropertyFreeList(
	SCA_PROPERTY* pspList
	)
{
	SCA_PROPERTY* pspDelete = pspList;
	while (pspList)
	{
		pspDelete = pspList;
		pspList = pspList->pspNext;

		MemFree(pspDelete);
	}
}


HRESULT ScaPropertyRead(
	SCA_PROPERTY** ppspList
	)
{
	Assert(ppspList);

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;
	PMSIHANDLE hView, hRec;

	BOOL fIIsPropertyTable = FALSE;
	LPWSTR pwzData = NULL;
	SCA_PROPERTY* pss;

	// check to see what tables are available
	fIIsPropertyTable = (S_OK == WcaTableExists(L"IIsProperty"));

	if (!fIIsPropertyTable)
	{
		WcaLog(LOGMSG_VERBOSE, "Skipping ScaInstallProperty() - required table not present");
		ExitFunction1(hr = S_FALSE);
	}

	// loop through all the Settings
	hr = WcaOpenExecuteView(vcsPropertyQuery, &hView);
	ExitOnFailure(hr, "failed to open view on IIsProperty table");
	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		hr = AddPropertyToList(ppspList);
		ExitOnFailure(hr, "failed to add property to list");

		pss = *ppspList;

		hr = WcaGetRecordString(hRec, sqProperty, &pwzData);
		ExitOnFailure(hr, "failed to get IIsProperty.Property");
		hr = StringCchCopyW(pss->wzProperty, countof(pss->wzProperty), pwzData);
		ExitOnFailure1(hr, "failed to copy Property name: %S", pwzData);

		hr = WcaGetRecordString(hRec, sqValue, &pwzData);
		ExitOnFailure(hr, "failed to get IIsProperty.Value");
		hr = StringCchCopyW(pss->wzValue, countof(pss->wzValue), pwzData);
		ExitOnFailure1(hr, "failed to copy Property value: %S", pwzData);

		hr = WcaGetRecordInteger(hRec, sqAttributes, &pss->iAttributes);
		ExitOnFailure(hr, "failed to get IIsProperty.Attributes");

		hr = WcaGetRecordString(hRec, sqComponent, &pwzData);
		ExitOnFailure(hr, "failed to get IIsProperty.Component");
		hr = StringCchCopyW(pss->wzComponent, countof(pss->wzComponent), pwzData);
		ExitOnFailure1(hr, "failed to copy component name: %S", pwzData);

		er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pss->wzComponent, &pss->isInstalled, &pss->isAction);
		hr = HRESULT_FROM_WIN32(er);
		ExitOnFailure(hr, "Failed to get IIsProperty.Component state");
	}

	if (E_NOMOREITEMS == hr)
		hr = S_OK;
	ExitOnFailure(hr, "failure while processing IIsProperty table");

LExit:
	ReleaseStr(pwzData);

	return hr;
}


HRESULT ScaPropertyInstall(
	IMSAdminBase* piMetabase,
	SCA_PROPERTY* pspList
	)
{
	Assert(piMetabase);

	HRESULT hr = S_OK;

	for (SCA_PROPERTY* psp = pspList; psp; psp = psp->pspNext)
	{
		// if we are installing the web site
		if (WcaIsInstalling(psp->isInstalled, psp->isAction))
		{
			hr = ScaWriteProperty(piMetabase, psp);
			ExitOnFailure1(hr, "failed to write Property '%S' to metabase", psp->wzProperty);
		}
	}

LExit:
	return hr;
}


HRESULT ScaPropertyUninstall(
	IMSAdminBase* piMetabase,
	SCA_PROPERTY* pspList
	)
{
	Assert(piMetabase);

	HRESULT hr = S_OK;

	for (SCA_PROPERTY* psp = pspList; psp; psp = psp->pspNext)
	{
		// if we are uninstalling the web site
		if (WcaIsUninstalling(psp->isInstalled, psp->isAction))
		{
			hr = ScaRemoveProperty(piMetabase, psp);
			ExitOnFailure1(hr, "Failed to remove Property '%S' from metabase", psp->wzProperty);
		}
	}

LExit:
	return hr;
}


HRESULT ScaWriteProperty(
	IMSAdminBase* piMetabase,
	SCA_PROPERTY* psp
	)
{
	Assert(piMetabase && psp);

	HRESULT hr = S_OK;
	DWORD dwValue;
	LPWSTR wz = NULL;

	//
	// Figure out what setting we're writing and write it
	//
	if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_IIS5_ISOLATION_MODE))
	{
		dwValue = 1;
		hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, MD_GLOBAL_STANDARD_APP_MODE_ENABLED, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
		ExitOnFailure(hr, "failed to set IIs5IsolationMode");
	}
	else if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_MAX_GLOBAL_BANDWIDTH))
	{
		dwValue = wcstoul(psp->wzValue, &wz, 10) * 1024; // remember, the value shown is in kilobytes, the value saved is in bytes
		hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, MD_MAX_GLOBAL_BANDWIDTH, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
		ExitOnFailure(hr, "failed to set MaxGlobalBandwidth");
	}
	else if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_LOG_IN_UTF8))
	{
		dwValue = 1;
		hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, MD_GLOBAL_LOG_IN_UTF_8, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
		ExitOnFailure(hr, "failed to set LogInUTF8");
	}
	else if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_ETAG_CHANGENUMBER))
	{
		dwValue = wcstoul(psp->wzValue, &wz, 10);
		hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, /*MD_ETAG_CHANGENUMBER*/ 2039, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
		ExitOnFailure(hr, "failed to set EtagChangenumber");
	}
LExit:
	return hr;
}


HRESULT ScaRemoveProperty(
	IMSAdminBase* piMetabase,
	SCA_PROPERTY* psp
	)
{
	Assert(piMetabase && psp);

	HRESULT hr = S_OK;
	DWORD dwValue;

	if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_IIS5_ISOLATION_MODE))
	{
		dwValue = 0;
		hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, MD_GLOBAL_STANDARD_APP_MODE_ENABLED, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
		ExitOnFailure(hr, "failed to clear IIs5IsolationMode");
	}
	else if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_MAX_GLOBAL_BANDWIDTH))
	{
		dwValue = -1; // This unchecks the box
		hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, MD_MAX_GLOBAL_BANDWIDTH, METADATA_NO_ATTRIBUTES , IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
		ExitOnFailure(hr, "failed to clear MaxGlobalBandwidth");
	}
	else if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_LOG_IN_UTF8))
	{
		dwValue = 0;
		hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, MD_GLOBAL_LOG_IN_UTF_8, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
		ExitOnFailure(hr, "failed to clear LogInUTF8");
	}

LExit:
	return hr;
}


static HRESULT AddPropertyToList(
	SCA_PROPERTY** ppspList
	)
{
	HRESULT hr = S_OK;
	SCA_PROPERTY* psp = (SCA_PROPERTY*)MemAlloc(sizeof(SCA_PROPERTY), TRUE);
	ExitOnNull(psp, hr, E_OUTOFMEMORY, "failed to allocate memory for new property list element");

	psp->pspNext = *ppspList;
	*ppspList = psp;

LExit:
	return hr;
}

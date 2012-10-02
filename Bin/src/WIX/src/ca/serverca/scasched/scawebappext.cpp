//-------------------------------------------------------------------------------------------------
// <copyright file="scawebappext.cpp" company="Microsoft">
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
//    IIS Web Application Extension functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// sql queries
LPCWSTR vcsWebAppExtensionQuery = L"SELECT `Extension`, `Verbs`, `Executable`, "
								 L"`Attributes` FROM `IIsWebApplicationExtension` WHERE `Application_`=?";
enum eWebAppExtensionQuery { wappextqExtension = 1, wappextqVerbs,
							 wappextqExecutable, wappextqAttributes };

// prototypes for private helper functions
static HRESULT NewAppExt(
	SCA_WEB_APPLICATION_EXTENSION** ppswappext
	);
SCA_WEB_APPLICATION_EXTENSION* AddAppExtToList(
	SCA_WEB_APPLICATION_EXTENSION* pswappextList,
	SCA_WEB_APPLICATION_EXTENSION* pswappext
	);



HRESULT ScaWebAppExtensionsRead(
	LPCWSTR wzApplication,
	SCA_WEB_APPLICATION_EXTENSION** ppswappextList
	)
{
	HRESULT hr = S_OK;
	PMSIHANDLE hView, hRec;

	SCA_WEB_APPLICATION_EXTENSION* pswappext = NULL;
	LPWSTR pwzData = NULL;

	// check pre-requisites
	hr = WcaTableExists(L"IIsWebApplicationExtension");
	if (S_FALSE == hr)
		ExitFunction();

	// convert the string into a msi record
	hRec = ::MsiCreateRecord(1);
	hr = WcaSetRecordString(hRec, 1, wzApplication);
	ExitOnFailure(hr, "Failed to set record to look up Web Application");

	// open and execute the view on the applicatoin extension table
	hr = WcaOpenView(vcsWebAppExtensionQuery, &hView);
	ExitOnFailure(hr, "Failed to open view on IIsWebApplicationExtension table");

	hr = WcaExecuteView(hView, hRec);
	ExitOnFailure1(hr, "Failed to execute view on IIsWebApplicationExtension table looking Application: %S", wzApplication);

	// get the application extention information
	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		hr = NewAppExt(&pswappext);
		ExitOnFailure(hr, "failed to create new web app extension");

		// get the extension
		hr = WcaGetRecordString(hRec, wappextqExtension, &pwzData);
		ExitOnFailure(hr, "Failed to get Web Application Extension");
		StringCchCopyW(pswappext->wzExtension, countof(pswappext->wzExtension), pwzData);

		// application extension verbs
		hr = WcaGetRecordString(hRec, wappextqVerbs, &pwzData);
		ExitOnFailure1(hr, "Failed to get Verbs for Application: '%S'", wzApplication);
		StringCchCopyW(pswappext->wzVerbs, countof(pswappext->wzVerbs), pwzData);

		// extension executeable
		hr = WcaGetRecordFormattedString(hRec, wappextqExecutable, &pwzData);
		ExitOnFailure1(hr, "Failed to get Executable for Application: '%S'", wzApplication);
		StringCchCopyW(pswappext->wzExecutable, countof(pswappext->wzExecutable), pwzData);

		hr = WcaGetRecordInteger(hRec, wappextqAttributes, &pswappext->iAttributes);
		if (S_FALSE == hr)
		{
			pswappext->iAttributes = 0;
			hr = S_OK;
		}
		ExitOnFailure(hr, "Failed to get App isolation");

		*ppswappextList = AddAppExtToList(*ppswappextList, pswappext);
		pswappext = NULL;	// set the appext NULL so it doesn't accidentally get freed below
	}

	if (E_NOMOREITEMS == hr)
		hr = S_OK;

LExit:
	// if anything was left over after an error clean it all up
	if (pswappext)
		ScaWebAppExtensionsFreeList(pswappext);

	ReleaseStr(pwzData);

	return hr;
}



HRESULT ScaWebAppExtensionsWrite(IMSAdminBase* piMetabase, LPCWSTR wzRootOfWeb,
	SCA_WEB_APPLICATION_EXTENSION* pswappextList
	)
{
	HRESULT hr = S_OK;

	LPWSTR wzAppExt = NULL;
	DWORD cchAppExt;
	WCHAR wzAppExtension[1024];
	WCHAR wzAppExtensions[65536];
	SCA_WEB_APPLICATION_EXTENSION* pswappext = NULL;

	if (!pswappextList)
		ExitFunction();

	::ZeroMemory(wzAppExtensions, sizeof(wzAppExtensions));
	wzAppExt = wzAppExtensions;
	cchAppExt = countof(wzAppExtensions);
	pswappext = pswappextList;

	while (pswappext)
	{
		if (0 == lstrcmpW(wzAppExtension, L"*"))
			StringCchPrintfW(wzAppExtension, countof(wzAppExtension), L"*,%s,%d", pswappext->wzExecutable, pswappext->iAttributes);
		else if (*pswappext->wzExtension)
			StringCchPrintfW(wzAppExtension, countof(wzAppExtension), L".%s,%s,%d", pswappext->wzExtension, pswappext->wzExecutable, pswappext->iAttributes);
		else   // blank means "*" (all)
			StringCchPrintfW(wzAppExtension, countof(wzAppExtension), L"*,%s,%d", pswappext->wzExecutable, pswappext->iAttributes);

		// if verbs were specified and not the keyword "all"
		if (pswappext->wzVerbs[0] && CSTR_EQUAL != CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pswappext->wzVerbs, -1, L"all", -1))
		{
			StringCchCatW(wzAppExtension, countof(wzAppExtension), L",");
			StringCchCatW(wzAppExtension, countof(wzAppExtension), pswappext->wzVerbs);
		}

		StringCchCopyW(wzAppExt, cchAppExt, wzAppExtension);
		wzAppExt += lstrlenW(wzAppExtension) + 1;
		cchAppExt -= lstrlenW(wzAppExtension) + 1;
		pswappext = pswappext->pswappextNext;
	}

	if (*wzAppExtensions)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_SCRIPT_MAPS, METADATA_INHERIT, IIS_MD_UT_FILE, MULTISZ_METADATA, wzAppExtensions);
		ExitOnFailure1(hr, "Failed to write AppExtension: '%S'", wzAppExtension);
	}

LExit:
	return hr;
}


void ScaWebAppExtensionsFreeList(
	SCA_WEB_APPLICATION_EXTENSION* pswappextList
	)
{
	SCA_WEB_APPLICATION_EXTENSION* pswappextDelete = pswappextList;
	while (pswappextList)
	{
		pswappextDelete = pswappextList;
		pswappextList = pswappextList->pswappextNext;

		MemFree(pswappextDelete);
	}
}



// private helper functions

static HRESULT NewAppExt(
	SCA_WEB_APPLICATION_EXTENSION** ppswappext
	)
{
	HRESULT hr = S_OK;
	SCA_WEB_APPLICATION_EXTENSION* pswappext = (SCA_WEB_APPLICATION_EXTENSION*)MemAlloc(sizeof(SCA_WEB_APPLICATION_EXTENSION), TRUE);
	ExitOnNull(pswappext, hr, E_OUTOFMEMORY, "failed to allocate memory for new web app ext element");

	*ppswappext = pswappext;

LExit:
	return hr;
}


SCA_WEB_APPLICATION_EXTENSION* AddAppExtToList(
	SCA_WEB_APPLICATION_EXTENSION* pswappextList,
	SCA_WEB_APPLICATION_EXTENSION* pswappext
	)
{
	if (pswappextList)
	{
		SCA_WEB_APPLICATION_EXTENSION* pswappextT = pswappextList;
		while (pswappextT->pswappextNext)
			pswappextT = pswappextT->pswappextNext;

		pswappextT->pswappextNext = pswappext;
	}
	else
		pswappextList = pswappext;

	return pswappextList;
}

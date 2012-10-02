//-------------------------------------------------------------------------------------------------
// <copyright file="scawebdir.h" company="Microsoft">
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
//    IIS Web Directory functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// sql queries
LPCWSTR vcsWebDirQuery = L"SELECT `Web_`, `WebDir`, `Component_`, `Path`, `DirProperties_`, `Application_`"
									   L"FROM `IIsWebDir`";
enum eWebDirQuery { wdqWeb = 1, wdqWebDir, wdqComponent , wdqPath, wdqProperties, wdqApplication };

// prototypes
HRESULT AddWebDirToList(SCA_WEBDIR** ppswdList);


UINT __stdcall ScaWebDirsRead(IMSAdminBase* piMetabase, SCA_WEB* pswList, SCA_WEBDIR** ppswdList)
{
	Assert(piMetabase && ppswdList);

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;
	PMSIHANDLE hView, hRec;

	LPWSTR pwzData = NULL;
	SCA_WEBDIR* pswd;
	DWORD dwLen = 0;

	// check to see if necessary tables are specified
	if (S_OK != WcaTableExists(L"IIsWebDir"))
	{
		WcaLog(LOGMSG_VERBOSE, "Skipping ScaInstallWebDirs() because IIsWebDir table not present");
		hr = S_FALSE;
		goto LExit;
	}

	// loop through all the web directories
	hr = WcaOpenExecuteView(vcsWebDirQuery, &hView);
	ExitOnFailure(hr, "Failed to open view on IIsWebDir table");
	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		hr = AddWebDirToList(ppswdList);
		ExitOnFailure(hr, "failed to add web dir to list");

		pswd = *ppswdList;
		Assert(pswd);

		// get component install state
		hr = WcaGetRecordString(hRec, wdqComponent, &pwzData);
		ExitOnFailure(hr, "Failed to get Component for WebDirs");
		StringCchCopyW(pswd->wzComponent, countof(pswd->wzComponent), pwzData);

		er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &pswd->isInstalled, &pswd->isAction);
		hr = HRESULT_FROM_WIN32(er);
		ExitOnFailure(hr, "Failed to get Component state for WebDirs");

		hr = WcaGetRecordString(hRec, wdqWeb, &pwzData);
		ExitOnFailure(hr, "Failed to get Web for WebDir");

		dwLen = countof(pswd->wzWebBase);
		hr = ScaWebsGetBase(piMetabase, pswList, pwzData, pswd->wzWebBase, &dwLen);
		ExitOnFailure(hr, "Failed to get base of web for WebDir");

		hr = WcaGetRecordFormattedString(hRec, wdqPath, &pwzData);
		ExitOnFailure(hr, "Failed to get Path for WebDir");

		StringCchPrintfW(pswd->wzWebDirRoot, countof(pswd->wzWebDirRoot), L"%s/Root/%s", pswd->wzWebBase, pwzData);

		// get the directory properties for this web
		hr = WcaGetRecordString(hRec, wdqProperties, &pwzData);
		ExitOnFailure(hr, "Failed to get security identifier for WebDir");
		if (*pwzData)
		{
			hr = ScaGetWebDirProperties(pwzData, &pswd->swp);
			ExitOnFailure(hr, "Failed to get properties for WebDir");

			pswd->fHasProperties = TRUE;
		}

		// get the application information for this web directory
		hr = WcaGetRecordString(hRec, wdqApplication, &pwzData);
		ExitOnFailure(hr, "Failed to get application identifier for WebDir");
		if (*pwzData)
		{
			hr = ScaGetWebApplication(NULL, pwzData, &pswd->swapp);
			ExitOnFailure(hr, "Failed to get application for WebDir");

			pswd->fHasApplication = TRUE;
		}
	}

	if (E_NOMOREITEMS == hr)
		hr = S_OK;
	ExitOnFailure(hr, "Failure while processing WebDirs");

LExit:
	ReleaseStr(pwzData);

	return hr;
}


HRESULT ScaWebDirsInstall(IMSAdminBase* piMetabase, SCA_WEBDIR* pswdList, SCA_APPPOOL * psapList)
{
	HRESULT hr = S_OK;
	SCA_WEBDIR* pswd = pswdList;
	int i;

	while (pswd)
	{
		// if we are installing the web site
		if (WcaIsInstalling(pswd->isInstalled, pswd->isAction))
		{
			hr = ScaCreateMetabaseKey(piMetabase, pswd->wzWebDirRoot, L"");
			ExitOnFailure(hr, "Failed to create key for WebDir");
			hr = ScaWriteMetabaseValue(piMetabase, pswd->wzWebDirRoot, L"", MD_KEY_TYPE, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)L"IIsWebDirectory");
			ExitOnFailure(hr, "Failed to write key type for for WebDir");
			i = 0x4000003e; // 1073741886;	// default directory browsing rights
			hr = ScaWriteMetabaseValue(piMetabase, pswd->wzWebDirRoot, L"", MD_DIRECTORY_BROWSING, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)i));
			ExitOnFailure(hr, "Failed to set directory browsing for WebDir");

			// get the security information for this web
			if (pswd->fHasProperties)
			{
				ScaWriteWebDirProperties(piMetabase, pswd->wzWebDirRoot, &pswd->swp);
				ExitOnFailure(hr, "Failed to write properties for WebDir");
			}

			// get the application information for this web directory
			if (pswd->fHasApplication)
			{
				hr = ScaWriteWebApplication(piMetabase, pswd->wzWebDirRoot, &pswd->swapp, psapList);
				ExitOnFailure(hr, "Failed to write application for WebDir");
			}
		}

		pswd = pswd->pswdNext;
	}

LExit:
	return hr;
}


HRESULT ScaWebDirsUninstall(IMSAdminBase* piMetabase, SCA_WEBDIR* pswdList)
{
	Assert(piMetabase);

	HRESULT hr = S_OK;
	SCA_WEBDIR* pswd = pswdList;

	while (pswd)
	{
		if (WcaIsUninstalling(pswd->isInstalled, pswd->isAction))
		{
			hr = ScaDeleteMetabaseKey(piMetabase, pswd->wzWebDirRoot, L"");
			ExitOnFailure1(hr, "Failed to remove WebDir '%S' from metabase", pswd->wzKey);
		}

		pswd = pswd->pswdNext;
	}

LExit:
	return hr;
}


void ScaWebDirsFreeList(SCA_WEBDIR* pswdList)
{
	SCA_WEBDIR* pswdDelete = pswdList;
	while (pswdList)
	{
		pswdDelete = pswdList;
		pswdList = pswdList->pswdNext;

		MemFree(pswdDelete);
	}
}


HRESULT AddWebDirToList(SCA_WEBDIR** ppswdList)
{
	HRESULT hr = S_OK;
	SCA_WEBDIR* pswd = (SCA_WEBDIR*)MemAlloc(sizeof(SCA_WEBDIR), TRUE);
	ExitOnNull(pswd, hr, E_OUTOFMEMORY, "failed to allocate element for web dir list");

	pswd->pswdNext = *ppswdList;
	*ppswdList = pswd;

LExit:
	return hr;
}

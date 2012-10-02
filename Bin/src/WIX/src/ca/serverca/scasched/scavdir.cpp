//-------------------------------------------------------------------------------------------------
// <copyright file="scavdir.cpp" company="Microsoft">
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
//    IIS Virtual Directory functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// sql queries
LPCWSTR vcsVDirQuery = L"SELECT `Web_`, `VirtualDir`, `Component_`, `Alias`, `Directory_`, `DirProperties_`, `Application_`"
L"FROM `IIsWebVirtualDir`";
LPCWSTR vcsVDirPropertiesQuery = L"SELECT `Web_`, `VirtualDir`, `Component_`, `Alias`, `Directory_`, `DirProperties_`, `Application_`"
L"FROM `IIsWebVirtualDir` WHERE `VirtualDir`=?";
enum eVDirQuery { vdqWeb = 1, vdqVDir, vdqComponent , vdqAlias, vdqDirectory, vdqProperties, vdqApplication };

// prototypes
static HRESULT AddVirtualDirToList(
	__in SCA_VDIR** psvdList
	);


HRESULT __stdcall ScaVirtualDirsRead(
	__in IMSAdminBase* piMetabase,
	__in SCA_WEB* pswList,
	__in SCA_VDIR** ppsvdList,
	__in SCA_MIMEMAP** ppsmmList,
	__in SCA_HTTP_HEADER** ppshhList,
	__in SCA_WEB_ERROR** ppsweList
	)
{
	Assert(piMetabase && ppsvdList);

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;
	PMSIHANDLE hView, hRec;

	SCA_VDIR* pvdir = NULL;
	LPWSTR pwzData = NULL;
	DWORD cchData = 0;

	DWORD dwLen = 0;

	// check to see if necessary tables are specified
	if (S_OK != WcaTableExists(L"IIsWebVirtualDir"))
	{
		WcaLog(LOGMSG_VERBOSE, "Skipping ScaIntsallVirtualDirs() because IIsWebVirtualDir table not present");
		hr = S_FALSE;
		goto LExit;
	}

	// loop through all the vdirs
	hr = WcaOpenExecuteView(vcsVDirQuery, &hView);
	ExitOnFailure(hr, "Failed to open view on IIsWebVirtualDir table");
	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		hr = AddVirtualDirToList(ppsvdList);
		ExitOnFailure(hr, "failed to add virtual dir to list");

		pvdir = *ppsvdList;

		// get component install state
		hr = WcaGetRecordString(hRec, vdqComponent, &pwzData);
		ExitOnFailure(hr, "Failed to get Component for VirtualDirs");

		er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &pvdir->isInstalled, &pvdir->isAction);
		hr = HRESULT_FROM_WIN32(er);
		ExitOnFailure(hr, "Failed to get Component state for VirtualDirs");

		// get the web key
		hr = WcaGetRecordString(hRec, vdqWeb, &pwzData);
		ExitOnFailure(hr, "Failed to get Web for VirtualDir");

		dwLen = countof(pvdir->wzWebBase);
		hr = ScaWebsGetBase(piMetabase, pswList, pwzData, pvdir->wzWebBase, &dwLen);
		ExitOnFailure1(hr, "Failed to get base of web: %S for VirtualDir", pwzData);

		hr = WcaGetRecordFormattedString(hRec, vdqAlias, &pwzData);
		ExitOnFailure(hr, "Failed to get Alias for VirtualDir");

		hr = StringCchPrintfW(pvdir->wzVDirRoot, countof(pvdir->wzVDirRoot), L"%s/Root/%s", pvdir->wzWebBase, pwzData);
		ExitOnFailure(hr, "Failed to set VDirRoot for VirtualDir");

		// get the vdir's directory
		hr = WcaGetRecordString(hRec, vdqDirectory, &pwzData);
		ExitOnFailure(hr, "Failed to get Directory for VirtualDir");

		WCHAR wzTargetPath[MAX_PATH];
		dwLen = countof(wzTargetPath);
		if (INSTALLSTATE_SOURCE == pvdir->isAction)
			er = ::MsiGetSourcePathW(WcaGetInstallHandle(), pwzData, wzTargetPath, &dwLen);
		else
			er = ::MsiGetTargetPathW(WcaGetInstallHandle(), pwzData, wzTargetPath, &dwLen);
		hr = HRESULT_FROM_WIN32(er);
		ExitOnFailure(hr, "Failed to get TargetPath for Directory for VirtualDir");
		// remove trailing backslash(es)
		while (dwLen > 0 && wzTargetPath[dwLen-1] == L'\\')
		{
			wzTargetPath[dwLen-1] = L'\0';
			dwLen--;
		}
		StringCchCopyW(pvdir->wzDirectory, countof(pvdir->wzDirectory), wzTargetPath);

		// get the security information for this web
		hr = WcaGetRecordString(hRec, vdqProperties, &pwzData);
		ExitOnFailure(hr, "Failed to get web directory identifier for VirtualDir");
		if (*pwzData)
		{
			hr = ScaGetWebDirProperties(pwzData, &pvdir->swp);
			ExitOnFailure(hr, "Failed to get web directory for VirtualDir");

			pvdir->fHasProperties = TRUE;
		}

		// get the application information for this web
		hr = WcaGetRecordString(hRec, vdqApplication, &pwzData);
		ExitOnFailure(hr, "Failed to get application identifier for VirtualDir");
		if (*pwzData)
		{
			hr = ScaGetWebApplication(NULL, pwzData, &pvdir->swapp);
			ExitOnFailure(hr, "Failed to get application for VirtualDir");

			pvdir->fHasApplication = TRUE;
		}

		hr = WcaGetRecordString(hRec, vdqVDir, &pwzData);
		ExitOnFailure(hr, "Failed to get VDir for VirtualDir");

		if (*pwzData && *ppsmmList)
		{
			hr = ScaGetMimeMap(mmptVDir, pwzData, ppsmmList, &pvdir->psmm);
			ExitOnFailure(hr, "Failed to get mimemap for VirtualDir");
		}

		if (*pwzData && *ppshhList)
		{
			hr = ScaGetHttpHeader(hhptVDir, pwzData, ppshhList, &pvdir->pshh);
			ExitOnFailure1(hr, "Failed to get custom HTTP headers for VirtualDir: %S", pwzData);
		}

		if (*pwzData && *ppsweList)
		{
			hr = ScaGetWebError(weptVDir, pwzData, ppsweList, &pvdir->pswe);
			ExitOnFailure1(hr, "Failed to get custom web errors for VirtualDir: %S", pwzData);
		}
	}

	if (E_NOMOREITEMS == hr)
		hr = S_OK;
	ExitOnFailure(hr, "Failure while processing VirtualDirs");

LExit:
	ReleaseStr(pwzData);
	return hr;
}


HRESULT ScaVirtualDirsInstall(
	__in IMSAdminBase* piMetabase,
	__in SCA_VDIR* psvdList,
	__in SCA_APPPOOL * psapList
	)
{
	Assert(piMetabase);

	HRESULT hr = S_OK;
	SCA_VDIR* psvd = psvdList;
	int i;

	while (psvd)
	{
		if (WcaIsInstalling(psvd->isInstalled, psvd->isAction))
		{
			hr = ScaCreateMetabaseKey(piMetabase, psvd->wzVDirRoot, L"");
			ExitOnFailure(hr, "Failed to create key for VirtualDir");
			hr = ScaWriteMetabaseValue(piMetabase, psvd->wzVDirRoot, L"", MD_KEY_TYPE, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)L"IIsWebVirtualDir");
			ExitOnFailure(hr, "Failed to write key type for for VirtualDir");
			i = 0x4000003e; // 1073741886;	// default directory browsing rights
			hr = ScaWriteMetabaseValue(piMetabase, psvd->wzVDirRoot, L"", MD_DIRECTORY_BROWSING, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)i));
			ExitOnFailure(hr, "Failed to set directory browsing for VirtualDir");

			hr = ScaWriteMetabaseValue(piMetabase, psvd->wzVDirRoot, L"", MD_VR_PATH, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)psvd->wzDirectory);
			ExitOnFailure(hr, "Failed to write Directory for VirtualDir");

			if (psvd->fHasProperties)
			{
				ScaWriteWebDirProperties(piMetabase, psvd->wzVDirRoot, &psvd->swp);
				ExitOnFailure(hr, "Failed to write directory properties for VirtualDir");
			}

			if (psvd->fHasApplication)
			{
				hr = ScaWriteWebApplication(piMetabase, psvd->wzVDirRoot, &psvd->swapp, psapList);
				ExitOnFailure(hr, "Failed to write application for VirtualDir");
			}

			if (psvd->psmm)
			{
				hr = ScaWriteMimeMap(piMetabase, psvd->wzVDirRoot, psvd->psmm);
				ExitOnFailure(hr, "Failed to write mimemap for VirtualDir");
			}

			if (psvd->pshh)
			{
				hr = ScaWriteHttpHeader(piMetabase, hhptVDir, psvd->wzVDirRoot, psvd->pshh);
				ExitOnFailure(hr, "Failed to write custom HTTP headers for VirtualDir");
			}

			if (psvd->pswe)
			{
				hr = ScaWriteWebError(piMetabase, weptVDir, psvd->wzVDirRoot, psvd->pswe);
				ExitOnFailure(hr, "Failed to write custom web errors for VirtualDir");
			}
		}

		psvd = psvd->psvdNext;
	}

LExit:
	return hr;
}


HRESULT ScaVirtualDirsUninstall(
	__in IMSAdminBase* piMetabase,
	__in SCA_VDIR* psvdList
	)
{
	Assert(piMetabase);

	HRESULT hr = S_OK;
	SCA_VDIR* psvd = psvdList;

	while (psvd)
	{
		if (WcaIsUninstalling(psvd->isInstalled, psvd->isAction))
		{
			hr = ScaDeleteMetabaseKey(piMetabase, psvd->wzVDirRoot, L"");
			ExitOnFailure1(hr, "Failed to remove VirtualDir '%S' from metabase", psvd->wzKey);
		}

		psvd = psvd->psvdNext;
	}

LExit:
	return hr;
}


void ScaVirtualDirsFreeList(
	__in SCA_VDIR* psvdList
	)
{
	SCA_VDIR* psvdDelete = psvdList;
	while (psvdList)
	{
		psvdDelete = psvdList;
		psvdList = psvdList->psvdNext;

		if (psvdDelete->psmm)
		{
			ScaMimeMapFreeList(psvdDelete->psmm);
		}

		if (psvdDelete->pswe)
		{
			ScaWebErrorFreeList(psvdDelete->pswe);
		}

		MemFree(psvdDelete);
	}
}


static HRESULT AddVirtualDirToList(
	__in SCA_VDIR** ppsvdList
	)
{
	HRESULT hr = S_OK;
	SCA_VDIR* psvd = (SCA_VDIR*)MemAlloc(sizeof(SCA_VDIR), TRUE);
	ExitOnNull(psvd, hr, E_OUTOFMEMORY, "failed to allocate memory for new vdir list element");

	psvd->psvdNext= *ppsvdList;
	*ppsvdList = psvd;

LExit:
	return hr;
}


HRESULT ScaVirtualDirGetAlias(
	__in LPCWSTR wzVirtualDir,
	__out LPWSTR* ppwzData
	)
{
	Assert(wzVirtualDir && *wzVirtualDir);

	HRESULT hr = S_OK;
	PMSIHANDLE hView, hRec;

	hr = WcaTableExists(L"IIsWebVirtualDir");
	if (S_FALSE == hr)
		hr = E_ABORT;
	ExitOnFailure(hr, "IIsWebVirtualDir table does not exist or error");

	hRec = ::MsiCreateRecord(1);
	if (!hRec)
	{
		hr = E_OUTOFMEMORY;
		ExitOnFailure(hr, "Failed to create record for lookup.");
	}
	hr = WcaSetRecordString(hRec, 1, wzVirtualDir);
	ExitOnFailure(hr, "Failed to look up VirtualDir Alias");

	hr = WcaOpenView(vcsVDirPropertiesQuery, &hView);
	ExitOnFailure(hr, "Failed to open view on VirtualDir");
	hr = WcaExecuteView(hView, hRec);
	ExitOnFailure(hr, "Failed to exectue view on VirtualDir");

	hr = WcaFetchSingleRecord(hView, &hRec);
	if (S_OK == hr)
	{
#if DEBUG
		// check that vdir matches
		hr = WcaGetRecordString(hRec, vdqVDir, ppwzData);
		ExitOnFailure(hr, "Failed to get IIsWebVirtualDir.VirtualDir");
		Assert(0 == lstrcmpW(*ppwzData, wzVirtualDir));
#endif

		hr = WcaGetRecordString(hRec, vdqAlias, ppwzData);
		ExitOnFailure(hr, "Failed to get Alias for VirtualDir");
	}
	else if (E_NOMOREITEMS == hr)
	{
		WcaLog(LOGMSG_STANDARD, "Error: Cannot locate IIsWebVirtualDir.VirtualDir='%S'", wzVirtualDir);
		hr = E_FAIL;
	}
	else
	{
		ExitOnFailure(hr, "Error or found multiple matching VirtualDir rows");
	}

LExit:
	return hr;
}


HRESULT ScaVirtualDirGetComponent(
	__in LPCWSTR wzVirtualDir,
	__out LPWSTR* ppwzData
	)
{
	Assert(wzVirtualDir && *wzVirtualDir);

	HRESULT hr = S_OK;
	PMSIHANDLE hView, hRec;

	hr = WcaTableExists(L"IIsWebVirtualDir");
	if (S_FALSE == hr)
		hr = E_ABORT;
	ExitOnFailure(hr, "IIsWebVirtualDir table does not exist or error");

	hRec = ::MsiCreateRecord(1);
	if (!hRec)
	{
		hr = E_OUTOFMEMORY;
		ExitOnFailure(hr, "Failed to create record for lookup.");
	}
	hr = WcaSetRecordString(hRec, 1, wzVirtualDir);
	ExitOnFailure(hr, "Failed to look up VirtualDir Component");

	hr = WcaOpenView(vcsVDirPropertiesQuery, &hView);
	ExitOnFailure(hr, "Failed to open view on VirtualDir");
	hr = WcaExecuteView(hView, hRec);
	ExitOnFailure(hr, "Failed to exectue view on VirtualDir");

	hr = WcaFetchSingleRecord(hView, &hRec);
	if (S_OK == hr)
	{
#if DEBUG
		// check that vdir matches
		hr = WcaGetRecordString(hRec, vdqVDir, ppwzData);
		ExitOnFailure(hr, "Failed to get IIsWebVirtualDir.VirtualDir");
		Assert(0 == lstrcmpW(*ppwzData, wzVirtualDir));
#endif

		hr = WcaGetRecordString(hRec, vdqComponent, ppwzData);
		ExitOnFailure(hr, "Failed to get Component for VirtualDir");
	}
	else if (E_NOMOREITEMS == hr)
	{
		WcaLog(LOGMSG_STANDARD, "Error: Cannot locate IIsWebVirtualDir.VirtualDir='%S'", wzVirtualDir);
		hr = E_FAIL;
	}
	else
	{
		ExitOnFailure(hr, "Error or found multiple matching VirtualDir rows");
	}

LExit:
	return hr;
}

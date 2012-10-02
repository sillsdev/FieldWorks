#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scawebapp.cpp" company="Microsoft">
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
//    IIS Web Application functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// sql queries
LPCWSTR vcsWebApplicationQuery = L"SELECT `Name`, `Isolation`, `AllowSessions`, `SessionTimeout`, "
								 L"`Buffer`, `ParentPaths`, `DefaultScript`, `ScriptTimeout`, "
								 L"`ServerDebugging`, `ClientDebugging`, `AppPool_` "
								 L"FROM `IIsWebApplication` WHERE `Application`=?";
enum eWebApplicationQuery { wappqName = 1, wappqIsolation, wappqAllowSession,
							wappqSessionTimeout, wappqBuffer, wappqParentPaths,
							wappqDefaultScript, wappqScriptTimeout,
							wappqServerDebugging, wappqClientDebugging, wappqAppPool};


HRESULT ScaGetWebApplication(MSIHANDLE hViewApplications,
							 LPCWSTR pwzApplication, SCA_WEB_APPLICATION* pswapp)
{
	HRESULT hr = S_OK;

	PMSIHANDLE hView, hRec;
	LPWSTR pwzData = NULL;

	hr = WcaTableExists(L"IIsWebApplication");
	if (S_FALSE == hr)
		hr = E_ABORT;
	ExitOnFailure(hr, "IIsWebApplication table does not exists or error");

	hRec = ::MsiCreateRecord(1);
	hr = WcaSetRecordString(hRec, 1, pwzApplication);
	ExitOnFailure(hr, "Failed to set record to look up Web Application");

	// if the view wasn't provided open one
	if (!hViewApplications)
	{
		hr = WcaOpenView(vcsWebApplicationQuery, &hView);
		ExitOnFailure(hr, "Failed to open view on IIsWebApplication table");
	}
	else
		hView = hViewApplications;

	hr = WcaExecuteView(hView, hRec);
	ExitOnFailure1(hr, "Failed to execute view on IIsWebApplication table looking Application: %S", pwzApplication);

	// get the application information
	hr = WcaFetchSingleRecord(hView, &hRec);
	if (S_OK == hr)
	{
		// application name
		hr = WcaGetRecordFormattedString(hRec, wappqName, &pwzData);
		ExitOnFailure(hr, "Failed to get Name of App");
		StringCchCopyW(pswapp->wzName, countof(pswapp->wzName), pwzData);

		hr = WcaGetRecordInteger(hRec, wappqIsolation, &pswapp->iIsolation);
		ExitOnFailure1(hr, "Failed to get App isolation: '%S'", pswapp->wzName);

		hr = WcaGetRecordInteger(hRec, wappqAllowSession, &pswapp->fAllowSessionState);

		hr = WcaGetRecordInteger(hRec, wappqSessionTimeout, &pswapp->iSessionTimeout);

		hr = WcaGetRecordInteger(hRec, wappqBuffer, &pswapp->fBuffer);

		hr = WcaGetRecordInteger(hRec, wappqParentPaths, &pswapp->fParentPaths);

		hr = WcaGetRecordString(hRec, wappqDefaultScript, &pwzData);
		ExitOnFailure1(hr, "Failed to get default scripting language for App: '%S'", pswapp->wzName);
		StringCchCopyW(pswapp->wzDefaultScript, countof(pswapp->wzDefaultScript), pwzData);

		// asp script timeout
		hr = WcaGetRecordInteger(hRec, wappqScriptTimeout, &pswapp->iScriptTimeout);
		ExitOnFailure1(hr, "Failed to get scripting timeout for App: '%S'", pswapp->wzName);

		// asp server-side script debugging
		hr = WcaGetRecordInteger(hRec, wappqServerDebugging, &pswapp->fServerDebugging);

		// asp client-side script debugging
		hr = WcaGetRecordInteger(hRec, wappqClientDebugging, &pswapp->fClientDebugging);

		hr = WcaGetRecordString(hRec, wappqAppPool, &pwzData);
		ExitOnFailure1(hr, "Failed to get AppPool for App: '%S'", pswapp->wzName);
		hr = StringCchCopyW(pswapp->wzAppPool, countof(pswapp->wzAppPool), pwzData);
		ExitOnFailure2(hr, "failed to copy AppPool: '%S' for App: '%S'", pwzData, pswapp->wzName);

		// app extensions
		hr = ScaWebAppExtensionsRead(pwzApplication, &pswapp->pswappextList);
		ExitOnFailure1(hr, "Failed to read AppExtensions for App: '%S'", pswapp->wzName);

		hr = S_OK;
	}
	else if (E_NOMOREITEMS == hr)
	{
		WcaLog(LOGMSG_STANDARD, "Error: Cannot locate IIsWebApplication.Application='%S'", pwzApplication);
		hr = E_FAIL;
	}
	else
		ExitOnFailure(hr, "Error or found multiple matching Application rows");

LExit:
	ReleaseStr(pwzData);

	return hr;
}


HRESULT ScaWriteWebApplication(IMSAdminBase* piMetabase, LPCWSTR wzRootOfWeb,
							   SCA_WEB_APPLICATION* pswapp, SCA_APPPOOL * psapList)
{
	HRESULT hr = S_OK;
	WCHAR wzAppPoolName[MAX_PATH];

	hr = ScaCreateApp(piMetabase, wzRootOfWeb, pswapp->iIsolation);
	ExitOnFailure(hr, "Failed to create ASP App");

	// Medium Isolation seems to have to be set through the metabase
	if (2 == pswapp->iIsolation)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_APP_ISOLATED, METADATA_INHERIT, IIS_MD_UT_WAM, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->iIsolation));
		ExitOnFailure1(hr, "Failed to write isolation value for App: '%S'", pswapp->wzName);
	}

	// application name
	hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_APP_FRIENDLY_NAME, METADATA_INHERIT, IIS_MD_UT_WAM, STRING_METADATA, pswapp->wzName);
	ExitOnFailure1(hr, "Failed to write Name of App: '%S'", pswapp->wzName);

	// allow session state
	if (MSI_NULL_INTEGER != pswapp->fAllowSessionState)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_ALLOWSESSIONSTATE, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->fAllowSessionState));
		ExitOnFailure1(hr, "Failed to write allow session information for App: '%S'", pswapp->wzName);
	}

	// session timeout
	if (MSI_NULL_INTEGER != pswapp->iSessionTimeout)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_SESSIONTIMEOUT, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->iSessionTimeout));
		ExitOnFailure1(hr, "Failed to write session timeout for App: '%S'", pswapp->wzName);
	}

	// asp buffering
	if (MSI_NULL_INTEGER != pswapp->fBuffer)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_BUFFERINGON, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->fBuffer));
		ExitOnFailure1(hr, "Failed to write buffering flag for App: '%S'", pswapp->wzName);
	}

	// asp parent paths
	if (MSI_NULL_INTEGER != pswapp->fParentPaths)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_ENABLEPARENTPATHS, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->fParentPaths));
		ExitOnFailure1(hr, "Failed to write parent paths flag for App: '%S'", pswapp->wzName);
	}

	// default scripting language
	if (*pswapp->wzDefaultScript)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_SCRIPTLANGUAGE, METADATA_INHERIT, ASP_MD_UT_APP, STRING_METADATA, pswapp->wzDefaultScript);
		ExitOnFailure1(hr, "Failed to write default scripting language for App: '%S'", pswapp->wzName);
	}

	// asp script timeout
	if (MSI_NULL_INTEGER != pswapp->iScriptTimeout)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_SCRIPTTIMEOUT, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->iScriptTimeout));
		ExitOnFailure1(hr, "Failed to write script timeout for App: '%S'", pswapp->wzName);
	}

	// asp server-side script debugging
	if (MSI_NULL_INTEGER != pswapp->fServerDebugging)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_ENABLESERVERDEBUG, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->fServerDebugging));
		ExitOnFailure1(hr, "Failed to write ASP server-side script debugging flag for App: '%S'", pswapp->wzName);
	}

	// asp server-side script debugging
	if (MSI_NULL_INTEGER != pswapp->fClientDebugging)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_ENABLECLIENTDEBUG, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->fClientDebugging));
		ExitOnFailure1(hr, "Failed to write ASP client-side script debugging flag for App: '%S'", pswapp->wzName);
	}

	// AppPool
	if (*pswapp->wzAppPool)
	{
		hr = ScaFindAppPool(piMetabase, pswapp->wzAppPool, wzAppPoolName, countof(wzAppPoolName), psapList);
		ExitOnFailure1(hr, "failed to find app pool: %S", pswapp->wzAppPool);
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_APP_APPPOOL_ID, METADATA_INHERIT, IIS_MD_UT_SERVER, STRING_METADATA, wzAppPoolName);
		ExitOnFailure1(hr, "Failed to write default AppPool for App: '%S'", pswapp->wzName);
	}

	if (pswapp->pswappextList)
	{
		hr = ScaWebAppExtensionsWrite(piMetabase, wzRootOfWeb, pswapp->pswappextList);
		ExitOnFailure1(hr, "Failed to write AppExtensions for App: '%S'", pswapp->wzName);
	}

LExit:
	return hr;
}

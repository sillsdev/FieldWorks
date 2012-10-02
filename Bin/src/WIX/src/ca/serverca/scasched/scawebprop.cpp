#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scawebprop.h" company="Microsoft">
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
//    Web directory property functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// sql queries
LPCWSTR vcsWebDirPropertiesQuery = L"SELECT `DirProperties`, `Access`, `Authorization`, `AnonymousUser_`, `IIsControlledPassword`, `LogVisits`, `Index`, `DefaultDoc`, `AspDetailedError`, `HttpExpires`, `CacheControlMaxAge`, `CacheControlCustom`, `NoCustomError`, `AccessSSLFlags`, `AuthenticationProviders`"
								   L"FROM `IIsWebDirProperties` WHERE `DirProperties`=?";
enum eWebDirPropertiesQuery { wpqProperties = 1, wpqAccess, wpqAuthorization, wpqUser, wpqControlledPassword, wpqLogVisits, wpqIndex, wpqDefaultDoc,  wpqAspDetailedError, wpqHttpExp, wpqCCMaxAge, wpqCCCustom, wpqNoCustomError, wpqAccessSSLFlags, wpqAuthenticationProviders };

HRESULT ScaGetWebDirProperties(LPCWSTR wzProperties, SCA_WEB_PROPERTIES* pswp)
{
	Assert(wzProperties && *wzProperties && pswp);

	HRESULT hr = S_OK;
	PMSIHANDLE hView, hRec;
	LPWSTR pwzData = NULL;

	hr = WcaTableExists(L"IIsWebDirProperties");
	if (S_FALSE == hr)
		hr = E_ABORT;
	ExitOnFailure(hr, "IIsWebDirProperties table does not exists or error");

	hRec = ::MsiCreateRecord(1);
	hr = WcaSetRecordString(hRec, 1, wzProperties);
	ExitOnFailure(hr, "Failed to look up Web DirProperties");

	hr = WcaOpenView(vcsWebDirPropertiesQuery, &hView);
	ExitOnFailure(hr, "Failed to open view on WebDirProperties");
	hr = WcaExecuteView(hView, hRec);
	ExitOnFailure(hr, "Failed to exectue view on WebDirProperties");

	hr = WcaFetchSingleRecord(hView, &hRec);
	if (S_OK == hr)
	{
		hr = WcaGetRecordString(hRec, wpqProperties, &pwzData);
		ExitOnFailure(hr, "Failed to get IIsWebDirProperties.DirProperties");
		StringCchCopyW(pswp->wzKey, countof(pswp->wzKey), pwzData);

		Assert(0 == lstrcmpW(pswp->wzKey, wzProperties));

		hr = WcaGetRecordInteger(hRec, wpqAccess, &pswp->iAccess);
		hr = WcaGetRecordInteger(hRec, wpqAuthorization, &pswp->iAuthorization);

		// if allow anonymous users
		if (S_OK == hr && pswp->iAuthorization & 1)
		{
			// if there is an anonymous user specified
			hr = WcaGetRecordString(hRec, wpqUser, &pwzData);
			ExitOnFailure(hr, "Failed to get AnonymousUser_");
			if (pwzData && *pwzData)
			{
				hr = WcaGetRecordInteger(hRec, wpqControlledPassword, &pswp->fIIsControlledPassword);
				ExitOnFailure(hr, "Failed to get IIsControlledPassword");
				if (S_FALSE == hr)
				{
					pswp->fIIsControlledPassword = FALSE;
					hr = S_OK;
				}

				hr = ScaGetUser(pwzData, &pswp->scau);
				ExitOnFailure(hr, "Failed to get User information for Web");

				pswp->fHasUser = TRUE;
			}
			else
				pswp->fHasUser = FALSE;
		}

		hr = WcaGetRecordInteger(hRec, wpqLogVisits, &pswp->fLogVisits);
		ExitOnFailure(hr, "Failed to get IIsWebDirProperties.LogVisits");

		hr = WcaGetRecordInteger(hRec, wpqIndex, &pswp->fIndex);
		ExitOnFailure(hr, "Failed to get IIsWebDirProperties.Index");

		hr = WcaGetRecordString(hRec, wpqDefaultDoc, &pwzData);
		ExitOnFailure(hr, "Failed to get IIsWebDirProperties.DefaultDoc");
		if (pwzData && *pwzData)
		{
			pswp->fHasDefaultDoc = TRUE;
			if (0 == lstrcmpW(L"-", pwzData))   // remove any existing default documents by setting them blank
				pswp->wzDefaultDoc[0] = L'\0';
			else   // set the default documents
				StringCchCopyW(pswp->wzDefaultDoc, countof(pswp->wzDefaultDoc), pwzData);
		}
		else
		{
			pswp->fHasDefaultDoc = FALSE;
		}

		hr = WcaGetRecordInteger(hRec, wpqAspDetailedError, &pswp->fAspDetailedError);
		ExitOnFailure(hr, "Failed to get IIsWebDirProperties.AspDetailedError");

		hr = WcaGetRecordString(hRec, wpqHttpExp, &pwzData);
		ExitOnFailure(hr, "Failed to get IIsWebDirProperties.HttpExp");
		if (pwzData && *pwzData)
		{
			pswp->fHasHttpExp = TRUE;
			if (0 == lstrcmpW(L"-", pwzData))   // remove any existing default expiration settings by setting them blank
				pswp->wzHttpExp[0] = L'\0';
			else   // set the expiration setting
				StringCchCopyW(pswp->wzHttpExp, countof(pswp->wzHttpExp), pwzData);
		}
		else
		{
			pswp->fHasHttpExp = FALSE;
		}

		hr = WcaGetRecordInteger(hRec, wpqCCMaxAge, &pswp->iCacheControlMaxAge);
		ExitOnFailure(hr, "failed to get IIsWebDirProperties.CacheControlMaxAge");

		hr = WcaGetRecordString(hRec, wpqCCCustom, &pwzData);
		ExitOnFailure(hr, "Failed to get IIsWebDirProperties.CacheControlCustom");
		if (pwzData && *pwzData)
		{
			pswp->fHasCacheControlCustom = TRUE;
			if (0 == lstrcmpW(L"-", pwzData))   // remove any existing default cache control custom settings by setting them blank
				pswp->wzCacheControlCustom[0] = L'\0';
			else   // set the custom cache control setting
				StringCchCopyW(pswp->wzCacheControlCustom, countof(pswp->wzCacheControlCustom), pwzData);
		}
		else
		{
			pswp->fHasCacheControlCustom = FALSE;
		}

		hr = WcaGetRecordInteger(hRec, wpqNoCustomError, &pswp->fNoCustomError);
		ExitOnFailure(hr, "failed to get IIsWebDirProperties.NoCustomError");
		if (MSI_NULL_INTEGER == pswp->fNoCustomError)
			pswp->fNoCustomError = FALSE;

		hr = WcaGetRecordInteger(hRec, wpqAccessSSLFlags, &pswp->iAccessSSLFlags);
		ExitOnFailure(hr, "failed to get IIsWebDirProperties.AccessSSLFlags");

		hr = WcaGetRecordString(hRec, wpqAuthenticationProviders, &pwzData);
		ExitOnFailure(hr, "Failed to get IIsWebDirProperties.AuthenticationProviders");
		if(pwzData && *pwzData)
			StringCchCopyW(pswp->wzAuthenticationProviders, countof(pswp->wzAuthenticationProviders), pwzData);
		else
			pswp->wzAuthenticationProviders[0] = L'\0';
	}
	else if (E_NOMOREITEMS == hr)
	{
		WcaLog(LOGMSG_STANDARD, "Error: Cannot locate IIsWebDirProperties.DirProperties='%S'", wzProperties);
		hr = E_FAIL;
	}
	else
		ExitOnFailure(hr, "Error or found multiple matching Properties rows");

LExit:
	ReleaseStr(pwzData);

	return hr;
}


HRESULT ScaWriteWebDirProperties(IMSAdminBase* piMetabase, LPCWSTR wzRootOfWeb,
								 SCA_WEB_PROPERTIES* pswp)
{
	HRESULT hr = S_OK;
	DWORD dw = 0;
	WCHAR wz[METADATA_MAX_NAME_LEN + 1];

	// write the access permissions to the metabase
	if (MSI_NULL_INTEGER != pswp->iAccess)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ACCESS_PERM, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswp->iAccess));
		ExitOnFailure(hr, "Failed to write access permissions for Web");
	}

	if (MSI_NULL_INTEGER != pswp->iAuthorization)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_AUTHORIZATION, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswp->iAuthorization));
		ExitOnFailure(hr, "Failed to write authorization for Web");
	}

	if (pswp->fHasUser)
	{
		Assert(pswp->scau.wzName && *pswp->scau.wzName);
		// write the user name
		if (*pswp->scau.wzDomain)
			StringCchPrintfW(wz, countof(wz), L"%s\\%s", pswp->scau.wzDomain, pswp->scau.wzName);
		else
			StringCchCopyW(wz, countof(wz), pswp->scau.wzName);
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ANONYMOUS_USER_NAME, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)wz);
		ExitOnFailure(hr, "Failed to write anonymous user name for Web");

		// write the password
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ANONYMOUS_PWD, METADATA_INHERIT | METADATA_SECURE, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)pswp->scau.wzPassword);
		ExitOnFailure(hr, "Failed to write anonymous user password for Web");

		// store whether IIs controls password
		dw = (pswp->fIIsControlledPassword) ? TRUE : FALSE;
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ANONYMOUS_USE_SUBAUTH, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)dw));
		ExitOnFailure(hr, "Failed to write if IIs controls user password for Web");
	}

	if (MSI_NULL_INTEGER != pswp->fLogVisits)
	{
		// The sense of this boolean value is reversed - it is "don't log", not "log visits."
		dw = (pswp->fLogVisits) ? FALSE : TRUE;
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_DONT_LOG, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)dw));
		ExitOnFailure(hr, "Failed to write authorization for Web");
	}

	if (MSI_NULL_INTEGER != pswp->fIndex)
	{
		dw = (pswp->fIndex) ? TRUE : FALSE;
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_IS_CONTENT_INDEXED, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)dw));
		ExitOnFailure(hr, "Failed to write authorization for Web");
	}

	if (pswp->fHasDefaultDoc)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_DEFAULT_LOAD_FILE, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)pswp->wzDefaultDoc);
		ExitOnFailure(hr, "Failed to write default documents for Web");
	}

	if (MSI_NULL_INTEGER != pswp->fAspDetailedError)
	{
		dw = (pswp->fAspDetailedError) ? TRUE : FALSE;
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_SCRIPTERRORSSENTTOBROWSER, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)dw));
		ExitOnFailure(hr, "Failed to write ASP script error for Web");
	}

	if (pswp->fHasHttpExp)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_HTTP_EXPIRES, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)pswp->wzHttpExp);
		ExitOnFailure(hr, "Failed to write HTTP Expiration for Web");
	}

	if (MSI_NULL_INTEGER != pswp->iCacheControlMaxAge)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_CC_MAX_AGE, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswp->iCacheControlMaxAge));
		ExitOnFailure(hr, "Failed to write Cache Control Max Age for Web");
	}

	if (pswp->fHasCacheControlCustom)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_CC_OTHER, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)pswp->wzCacheControlCustom);
		ExitOnFailure(hr, "Failed to write Cache Control Custom for Web");
	}

	if (pswp->fNoCustomError)
	{
		memset(wz, 0, sizeof(wz));
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_CUSTOM_ERROR, METADATA_INHERIT, IIS_MD_UT_FILE, MULTISZ_METADATA, wz);
		ExitOnFailure(hr, "Failed to write Custom Error for Web");
	}

	if (MSI_NULL_INTEGER != pswp->iAccessSSLFlags)
	{
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_SSL_ACCESS_PERM, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswp->iAccessSSLFlags));
		ExitOnFailure(hr, "Failed to write AccessSSLFlags for Web");
	}

	if (pswp->wzAuthenticationProviders && *pswp->wzAuthenticationProviders)
	{
		StringCchCopyW(wz, countof(wz), pswp->wzAuthenticationProviders);
		hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_NTAUTHENTICATION_PROVIDERS, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)wz);
		ExitOnFailure(hr, "Failed to write AuthenticationProviders for Web");
	}

LExit:
	return hr;
}

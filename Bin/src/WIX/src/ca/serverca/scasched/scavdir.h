#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scavdir.h" company="Microsoft">
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

#include "scawebprop.h"
#include "scawebapp.h"
#include "scamimemap.h"
#include "scaapppool.h"

struct SCA_VDIR
{
	// darwin information
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	WCHAR wzComponent[MAX_DARWIN_KEY + 1];
	INSTALLSTATE isInstalled;
	INSTALLSTATE isAction;

	// metabase information
	WCHAR wzWebKey[MAX_DARWIN_KEY + 1];
	WCHAR wzWebBase[METADATA_MAX_NAME_LEN + 1];
	WCHAR wzVDirRoot[METADATA_MAX_NAME_LEN + 1];

	// iis configuation information
	WCHAR wzDirectory[MAX_PATH];

	BOOL fHasProperties;
	SCA_WEB_PROPERTIES swp;

	BOOL fHasApplication;
	SCA_WEB_APPLICATION swapp;

	SCA_MIMEMAP* psmm; // mime mappings
	SCA_HTTP_HEADER* pshh; // custom web errors
	SCA_WEB_ERROR* pswe; // custom web errors

	SCA_VDIR* psvdNext;
};


// prototypes
HRESULT __stdcall ScaVirtualDirsRead(
	__in IMSAdminBase* piMetabase,
	__in SCA_WEB* pswList,
	__in SCA_VDIR** ppsvdList,
	__in SCA_MIMEMAP** ppsmmList,
	__in SCA_HTTP_HEADER** ppshhList,
	__in SCA_WEB_ERROR** ppsweList
	);

HRESULT ScaVirtualDirsInstall(
	__in IMSAdminBase* piMetabase,
	__in SCA_VDIR* psvdList,
	__in SCA_APPPOOL * psapList
	);

HRESULT ScaVirtualDirsUninstall(
	__in IMSAdminBase* piMetabase,
	__in SCA_VDIR* psvdList
	);

void ScaVirtualDirsFreeList(
	__in SCA_VDIR* psvdList
	);

HRESULT ScaVirtualDirGetAlias(
	__in LPCWSTR wzVirtualDir,
	__out LPWSTR* ppwzData
	);

HRESULT ScaVirtualDirGetComponent(
	__in LPCWSTR wzVirtualDir,
	__out LPWSTR* ppwzData
	);

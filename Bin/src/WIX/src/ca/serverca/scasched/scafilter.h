#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scafilter.h" company="Microsoft">
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

#include "scaweb.h"

struct SCA_FILTER
{
	// darwin information
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	WCHAR wzComponent[MAX_DARWIN_KEY + 1];
	INSTALLSTATE isInstalled;
	INSTALLSTATE isAction;

	// metabase information
	WCHAR wzWebKey[MAX_DARWIN_KEY + 1];
	WCHAR wzWebBase[METADATA_MAX_NAME_LEN + 1];
	WCHAR wzFilterRoot[METADATA_MAX_NAME_LEN + 1];

	// iis configuation information
	WCHAR wzPath[MAX_PATH];
	WCHAR wzDescription[MAX_DARWIN_COLUMN + 1];
	int iFlags;
	int iLoadOrder;

	SCA_FILTER* psfNext;
};


// prototypes
UINT __stdcall ScaFiltersRead(IMSAdminBase* piMetabase,
							  SCA_WEB* pswList, SCA_FILTER** ppsfList);

HRESULT ScaFiltersInstall(IMSAdminBase* piMetabase, SCA_FILTER* psfList);

HRESULT ScaFiltersUninstall(IMSAdminBase* piMetabase, SCA_FILTER* psfList);

void ScaFiltersFreeList(SCA_FILTER* psfList);

#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scawebsvcext.h" company="Microsoft">
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
//    IIS Web Service Extension functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

enum SCA_WEBSVCEXT_ATTRIBUTES { SWSEATTRIB_ALLOW = 1, SWSEATTRIB_UIDELETABLE = 2 };

struct SCA_WEBSVCEXT
{
	// darwin information
	INSTALLSTATE isInstalled;
	INSTALLSTATE isAction;

	// iis configuation information
	WCHAR wzFile[MAX_PATH + 1];
	WCHAR wzDescription[MAX_DARWIN_COLUMN + 1];
	WCHAR wzGroup[MAX_DARWIN_COLUMN + 1];

	int iAttributes;

	SCA_WEBSVCEXT* psWseNext;
};

HRESULT __stdcall ScaWebSvcExtRead(
	__in SCA_WEBSVCEXT** ppsWseList
	);

HRESULT ScaWebSvcExtCommit(
	__in IMSAdminBase* piMetabase,
	__in SCA_WEBSVCEXT* psWseList
	);

void ScaWebSvcExtFreeList(
	__in SCA_WEBSVCEXT* psWseList
	);

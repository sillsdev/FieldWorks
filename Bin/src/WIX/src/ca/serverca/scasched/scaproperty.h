#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scaproperty.h" company="Microsoft">
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

// Settings
#define wzIISPROPERTY_IIS5_ISOLATION_MODE L"IIs5IsolationMode"
#define wzIISPROPERTY_MAX_GLOBAL_BANDWIDTH L"MaxGlobalBandwidth"
#define wzIISPROPERTY_LOG_IN_UTF8 L"LogInUTF8"
#define wzIISPROPERTY_ETAG_CHANGENUMBER L"ETagChangeNumber"

struct SCA_PROPERTY
{
	// iis configuation information
	WCHAR wzProperty[MAX_DARWIN_KEY + 1];
	WCHAR wzComponent[MAX_DARWIN_KEY + 1];
	INSTALLSTATE isInstalled;
	INSTALLSTATE isAction;
	INT iAttributes;
	WCHAR wzValue[MAX_DARWIN_COLUMN + 1];

	SCA_PROPERTY *pspNext;
};


// prototypes

HRESULT ScaPropertyRead(
	SCA_PROPERTY** ppspList
	);

void ScaPropertyFreeList(
	SCA_PROPERTY* pspList
	);

HRESULT ScaPropertyInstall(
	IMSAdminBase* piMetabase,
	SCA_PROPERTY* pspList
	);

HRESULT ScaPropertyUninstall(
	IMSAdminBase* piMetabase,
	SCA_PROPERTY* pspList
	);

HRESULT ScaWriteProperty(
	IMSAdminBase* piMetabase,
	SCA_PROPERTY* psp
	);

HRESULT ScaRemoveProperty(
	IMSAdminBase* piMetabase,
	SCA_PROPERTY* psp
	);

#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scasmb.h" company="Microsoft">
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
//    File share functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "scauser.h"

// structs
// Structure used to hold and extra user/permission pairs from the FileSharePermissions Table
struct SCA_SMB_EX_USER_PERMS
{
	int nPermissions;
	SCA_USER scau;
	SCA_SMB_EX_USER_PERMS* pExUserPermsNext;
};

struct SCA_SMB  // hungarian ss
{
	WCHAR wzId[MAX_DARWIN_KEY + 1];
	WCHAR wzShareName[MAX_DARWIN_KEY + 1];
	WCHAR wzDescription[MAX_DARWIN_KEY + 1];
	WCHAR wzComponent[MAX_DARWIN_KEY + 1];
	WCHAR wzDirectory[MAX_PATH + 1];

	int nUserPermissionCount;
	int nPermissions;
	SCA_SMB_EX_USER_PERMS* pExUserPerms;

	INSTALLSTATE isInstalled, isAction;

	BOOL fUseIntegratedAuth;
	BOOL fLegacyUserProvided;
	struct SCA_USER scau;

	struct SCA_SMB* pssNext;
};


#define RESERVED 0

// schedule prototypes
HRESULT ScaSmbRead(SCA_SMB** ppssList);
HRESULT ScaSmbExPermsRead(SCA_SMB* pss);
HRESULT ScaSmbUninstall(SCA_SMB* pssList);
HRESULT ScaSmbInstall(SCA_SMB* pssList);
void ScaSmbFreeList(SCA_SMB* pssList);

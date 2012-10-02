#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scasmbexec.cpp" company="Microsoft">
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

//Structure used to hold the permission User Name pairs
struct SCA_SMBP_USER_PERMS
{
	DWORD nPermissions;
	WCHAR* wzUser;
	//Not adding Password because I can't find anywhere that it is used
};

struct SCA_SMBP  // hungarian ssp
{
	WCHAR* wzKey;
	WCHAR* wzDescription;
	WCHAR* wzComponent;
	WCHAR* wzDirectory;  // full path of the dir to share to

	DWORD dwUserPermissionCount;  //Count of SCA_SMBP_EX_USER_PERMS structures
	SCA_SMBP_USER_PERMS* pUserPerms;
	BOOL fUseIntegratedAuth;
};


HRESULT ScaEnsureSmbExists(SCA_SMBP* pssp);
HRESULT ScaDropSmb(SCA_SMBP* pssp);

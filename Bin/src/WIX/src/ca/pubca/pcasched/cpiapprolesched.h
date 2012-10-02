#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="cpiapprolesched.h" company="Microsoft">
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
//    COM+ application role functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------


// structs

struct CPI_APPLICATION_ROLE
{
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	WCHAR wzName[MAX_DARWIN_COLUMN + 1];

	int iPropertyCount;
	CPI_PROPERTY* pProperties;

	BOOL fHasComponent;
	BOOL fReferencedForInstall;
	BOOL fReferencedForUninstall;
	BOOL fObjectNotFound;

	INSTALLSTATE isInstalled, isAction;

	CPI_APPLICATION* pApplication;

	ICatalogCollection* piUsersColl;

	CPI_APPLICATION_ROLE* pNext;
};

struct CPI_APPLICATION_ROLE_LIST
{
	CPI_APPLICATION_ROLE* pFirst;

	int iInstallCount;
	int iUninstallCount;
};

struct CPI_USER_IN_APPLICATION_ROLE
{
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	LPWSTR pwzAccount;

	INSTALLSTATE isInstalled, isAction;

	CPI_APPLICATION_ROLE* pApplicationRole;

	CPI_USER_IN_APPLICATION_ROLE* pNext;
};

struct CPI_USER_IN_APPLICATION_ROLE_LIST
{
	CPI_USER_IN_APPLICATION_ROLE* pFirst;

	int iInstallCount;
	int iUninstallCount;
};


// function prototypes

void CpiApplicationRoleListFree(
	CPI_APPLICATION_ROLE_LIST* pList
	);
HRESULT CpiApplicationRolesRead(
	CPI_APPLICATION_LIST* pAppList,
	CPI_APPLICATION_ROLE_LIST* pAppRoleList
	);
HRESULT CpiApplicationRolesVerifyInstall(
	CPI_APPLICATION_ROLE_LIST* pList
	);
HRESULT CpiApplicationRolesVerifyUninstall(
	CPI_APPLICATION_ROLE_LIST* pList
	);
void CpiApplicationRoleAddReferenceInstall(
	CPI_APPLICATION_ROLE* pItm
	);
void CpiApplicationRoleAddReferenceUninstall(
	CPI_APPLICATION_ROLE* pItm
	);
HRESULT CpiApplicationRolesInstall(
	CPI_APPLICATION_ROLE_LIST* pList,
	int iRunMode,
	LPWSTR* ppwzActionData,
	int* piProgress
	);
HRESULT CpiApplicationRolesUninstall(
	CPI_APPLICATION_ROLE_LIST* pList,
	int iRunMode,
	LPWSTR* ppwzActionData,
	int* piProgress
	);
HRESULT CpiApplicationRoleFindByKey(
	CPI_APPLICATION_ROLE_LIST* pList,
	LPCWSTR pwzKey,
	CPI_APPLICATION_ROLE** ppAppRole
	);
void CpiUserInApplicationRoleListFree(
	CPI_USER_IN_APPLICATION_ROLE_LIST* pList
	);
HRESULT CpiUsersInApplicationRolesRead(
	CPI_APPLICATION_ROLE_LIST* pAppRoleList,
	CPI_USER_IN_APPLICATION_ROLE_LIST* pUsrInAppRoleList
	);
HRESULT CpiUsersInApplicationRolesInstall(
	CPI_USER_IN_APPLICATION_ROLE_LIST* pList,
	int iRunMode,
	LPWSTR* ppwzActionData,
	int* piProgress
	);
HRESULT CpiUsersInApplicationRolesUninstall(
	CPI_USER_IN_APPLICATION_ROLE_LIST* pList,
	int iRunMode,
	LPWSTR* ppwzActionData,
	int* piProgress
	);

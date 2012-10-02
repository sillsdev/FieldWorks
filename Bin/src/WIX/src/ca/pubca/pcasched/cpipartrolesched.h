#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="cpipartrolesched.h" company="Microsoft">
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
//    COM+ partition role functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------


// structs

struct CPI_PARTITION_ROLE
{
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	WCHAR wzName[MAX_DARWIN_COLUMN + 1];

	CPI_PARTITION* pPartition;

	ICatalogCollection* piUsersColl;

	CPI_PARTITION_ROLE* pNext;
};

struct CPI_PARTITION_ROLE_LIST
{
	CPI_PARTITION_ROLE* pFirst;
};

struct CPI_USER_IN_PARTITION_ROLE
{
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	LPWSTR pwzAccount;

	INSTALLSTATE isInstalled, isAction;

	CPI_PARTITION_ROLE* pPartitionRole;

	CPI_USER_IN_PARTITION_ROLE* pNext;
};

struct CPI_USER_IN_PARTITION_ROLE_LIST
{
	CPI_USER_IN_PARTITION_ROLE* pFirst;

	int iInstallCount;
	int iUninstallCount;
};


// function prototypes

void CpiPartitionRoleListFree(
	CPI_PARTITION_ROLE_LIST* pList
	);
HRESULT CpiPartitionRolesRead(
	CPI_PARTITION_LIST* pPartList,
	CPI_PARTITION_ROLE_LIST* pPartRoleList
	);
HRESULT CpiPartitionRoleFindByKey(
	CPI_PARTITION_ROLE_LIST* pList,
	LPCWSTR pwzKey,
	CPI_PARTITION_ROLE** ppPartRole
	);

void CpiUserInPartitionRoleListFree(
	CPI_USER_IN_PARTITION_ROLE_LIST* pList
	);
HRESULT CpiUsersInPartitionRolesRead(
	CPI_PARTITION_ROLE_LIST* pPartRoleList,
	CPI_USER_IN_PARTITION_ROLE_LIST* pUsrInPartRoleList
	);
HRESULT CpiUsersInPartitionRolesInstall(
	CPI_USER_IN_PARTITION_ROLE_LIST* pList,
	int iRunMode,
	LPWSTR* ppwzActionData,
	int* piProgress
	);
HRESULT CpiUsersInPartitionRolesUninstall(
	CPI_USER_IN_PARTITION_ROLE_LIST* pList,
	int iRunMode,
	LPWSTR* ppwzActionData,
	int* piProgress
	);

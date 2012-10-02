#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="cpipartsched.h" company="Microsoft">
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
//    COM+ partition functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------


// structs

struct CPI_PARTITION
{
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	WCHAR wzID[CPI_MAX_GUID + 1];
	WCHAR wzName[MAX_DARWIN_COLUMN + 1];

	int iPropertyCount;
	CPI_PROPERTY* pProperties;

	BOOL fHasComponent;
	BOOL fReferencedForInstall;
	BOOL fReferencedForUninstall;
	BOOL fObjectNotFound;

	INSTALLSTATE isInstalled, isAction;

	ICatalogCollection* piApplicationsColl;
	ICatalogCollection* piRolesColl;

	CPI_PARTITION* pNext;
};

struct CPI_PARTITION_LIST
{
	CPI_PARTITION* pFirst;

	int iInstallCount;
	int iUninstallCount;
};

struct CPI_PARTITION_USER
{
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	LPWSTR pwzAccount;

	BOOL fNoFind;

	INSTALLSTATE isInstalled, isAction;

	CPI_PARTITION* pPartition;

	CPI_PARTITION_USER* pNext;
};

struct CPI_PARTITION_USER_LIST
{
	CPI_PARTITION_USER* pFirst;

	int iInstallCount;
	int iUninstallCount;
};


// function prototypes

void CpiPartitionListFree(
	CPI_PARTITION_LIST* pList
	);
HRESULT CpiPartitionsRead(
	CPI_PARTITION_LIST* pPartList
	);
HRESULT CpiPartitionsVerifyInstall(
	CPI_PARTITION_LIST* pList
	);
HRESULT CpiPartitionsVerifyUninstall(
	CPI_PARTITION_LIST* pList
	);
void CpiPartitionAddReferenceInstall(
	CPI_PARTITION* pItm
	);
void CpiPartitionAddReferenceUninstall(
	CPI_PARTITION* pItm
	);
HRESULT CpiPartitionsInstall(
	CPI_PARTITION_LIST* pList,
	int iRunMode,
	LPWSTR* ppwzActionData,
	int* piProgress
	);
HRESULT CpiPartitionsUninstall(
	CPI_PARTITION_LIST* pList,
	int iRunMode,
	LPWSTR* ppwzActionData,
	int* piProgress
	);
HRESULT CpiPartitionFindByKey(
	CPI_PARTITION_LIST* pList,
	LPCWSTR wzKey,
	CPI_PARTITION** ppItm
	);
HRESULT CpiGetApplicationsCollForPartition(
	CPI_PARTITION* pPart,
	ICatalogCollection** ppiAppColl
	);
HRESULT CpiGetPartitionUsersCollection(
	CPI_PARTITION* pPart,
	ICatalogCollection** ppiPartUsrColl
	);
HRESULT CpiGetRolesCollForPartition(
	CPI_PARTITION* pPart,
	ICatalogCollection** ppiRolesColl
	);
void CpiPartitionUserListFree(
	CPI_PARTITION_USER_LIST* pList
	);
HRESULT CpiPartitionUsersRead(
	CPI_PARTITION_LIST* pPartList,
	CPI_PARTITION_USER_LIST* pPartUsrList
	);
HRESULT CpiPartitionUsersInstall(
	CPI_PARTITION_USER_LIST* pList,
	int iRunMode,
	LPWSTR* ppwzActionData,
	int* piProgress
	);
HRESULT CpiPartitionUsersUninstall(
	CPI_PARTITION_USER_LIST* pList,
	int iRunMode,
	LPWSTR* ppwzActionData,
	int* piProgress
	);

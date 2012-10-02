#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="cpisubssched.h" company="Microsoft">
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
//    COM+ subscription functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------


// structs

struct CPI_SUBSCRIPTION
{
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	WCHAR wzID[CPI_MAX_GUID + 1];
	WCHAR wzName[MAX_DARWIN_COLUMN + 1];
	WCHAR wzEventCLSID[CPI_MAX_GUID + 1];
	WCHAR wzPublisherID[CPI_MAX_GUID + 1];

	BOOL fObjectNotFound;

	int iPropertyCount;
	CPI_PROPERTY* pProperties;

	INSTALLSTATE isInstalled, isAction;

	CPI_ASSEMBLY* pAssembly;
	CPI_COMPONENT* pComponent;

	CPI_SUBSCRIPTION* pNext;
};

struct CPI_SUBSCRIPTION_LIST
{
	CPI_SUBSCRIPTION* pFirst;

	int iInstallCount;
	int iCommitCount;
	int iUninstallCount;
};


// function prototypes

void CpiSubscriptionListFree(
	CPI_SUBSCRIPTION_LIST* pList
	);
HRESULT CpiSubscriptionsRead(
	CPI_ASSEMBLY_LIST* pAsmList,
	CPI_SUBSCRIPTION_LIST* pSubList
	);
HRESULT CpiSubscriptionsVerifyInstall(
	CPI_SUBSCRIPTION_LIST* pList
	);
HRESULT CpiSubscriptionsVerifyUninstall(
	CPI_SUBSCRIPTION_LIST* pList
	);
HRESULT CpiSubscriptionsInstall(
	CPI_SUBSCRIPTION_LIST* pList,
	int iRunMode,
	LPWSTR* ppwzActionData,
	int* piProgress
	);
HRESULT CpiSubscriptionsUninstall(
	CPI_SUBSCRIPTION_LIST* pList,
	int iRunMode,
	LPWSTR* ppwzActionData,
	int* piProgress
	);

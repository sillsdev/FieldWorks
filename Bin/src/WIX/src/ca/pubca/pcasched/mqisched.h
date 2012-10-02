#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="cpiasmsched.h" company="Microsoft">
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
//    MSMQ functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------


// structs

struct MQI_MESSAGE_QUEUE
{
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	int iBasePriority;
	int iJournalQuota;
	WCHAR wzLabel[MAX_DARWIN_COLUMN + 1];
	WCHAR wzMulticastAddress[MAX_DARWIN_COLUMN + 1];
	WCHAR wzPathName[MAX_DARWIN_COLUMN + 1];
	int iPrivLevel;
	int iQuota;
	WCHAR wzServiceTypeGuid[MAX_DARWIN_COLUMN + 1];
	int iAttributes;

	INSTALLSTATE isInstalled, isAction;

	MQI_MESSAGE_QUEUE* pNext;
};

struct MQI_MESSAGE_QUEUE_LIST
{
	MQI_MESSAGE_QUEUE* pFirst;

	int iInstallCount;
	int iUninstallCount;
};

struct MQI_MESSAGE_QUEUE_PERMISSION
{
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	WCHAR wzDomain[MAX_DARWIN_COLUMN + 1];
	WCHAR wzName[MAX_DARWIN_COLUMN + 1];
	int iPermissions;

	MQI_MESSAGE_QUEUE* pMessageQueue;

	INSTALLSTATE isInstalled, isAction;

	MQI_MESSAGE_QUEUE_PERMISSION* pNext;
};

struct MQI_MESSAGE_QUEUE_PERMISSION_LIST
{
	MQI_MESSAGE_QUEUE_PERMISSION* pFirst;

	int iInstallCount;
	int iUninstallCount;
};


// function prototypes

HRESULT MqiMessageQueueRead(
	MQI_MESSAGE_QUEUE_LIST* pList
	);
HRESULT MqiMessageQueueInstall(
	MQI_MESSAGE_QUEUE_LIST* pList,
	LPWSTR* ppwzActionData
	);
HRESULT MqiMessageQueueUninstall(
	MQI_MESSAGE_QUEUE_LIST* pList,
	LPWSTR* ppwzActionData
	);
void MqiMessageQueueFreeList(
	MQI_MESSAGE_QUEUE_LIST* pList
	);
HRESULT MqiMessageQueuePermissionRead(
	MQI_MESSAGE_QUEUE_LIST* pMessageQueueList,
	MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList
	);
HRESULT MqiMessageQueuePermissionInstall(
	MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList,
	LPWSTR* ppwzActionData
	);
HRESULT MqiMessageQueuePermissionUninstall(
	MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList,
	LPWSTR* ppwzActionData
	);
void MqiMessageQueuePermissionFreeList(
	MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList
	);

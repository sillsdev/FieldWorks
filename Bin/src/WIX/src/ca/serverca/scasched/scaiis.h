#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scaiis.h" company="Microsoft">
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
//    IIS functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

HRESULT ScaMetabaseTransaction(LPCWSTR wzBackup);

HRESULT ScaCreateWeb(IMSAdminBase* piMetabase, LPCWSTR wzWeb, LPCWSTR wzWebBase);

HRESULT ScaCreateApp(IMSAdminBase* piMetabase, LPCWSTR wzWebRoot,
					 DWORD dwIsolation);

HRESULT ScaAddFilterToLoadOrder(IMSAdminBase* piMetabase, LPCWSTR wzFilterRoot,
								LPCWSTR wzFilter, int iLoadOrder);

HRESULT ScaRemoveFilterFromLoadOrder(IMSAdminBase* piMetabase,
									 LPCWSTR wzFilterRoot, LPCWSTR wzFilter);

HRESULT ScaCreateMetabaseKey(IMSAdminBase* piMetabase, LPCWSTR wzRootKey,
							 LPCWSTR wzSubKey);

HRESULT ScaDeleteMetabaseKey(IMSAdminBase* piMetabase, LPCWSTR wzRootKey,
							 LPCWSTR wzSubKey);

HRESULT ScaWriteMetabaseValue(IMSAdminBase* piMetabase, LPCWSTR wzRootKey,
							  LPCWSTR wzSubKey, DWORD dwIdentifier,
							  DWORD dwAttributes, DWORD dwUserType,
							  DWORD dwDataType, LPVOID pvData);

HRESULT ScaScheduleMetabaseConfiguration();


HRESULT ScaLoadMetabase(IMSAdminBase** piMetabase);
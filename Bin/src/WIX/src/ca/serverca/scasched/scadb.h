#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scadb.h" company="Microsoft">
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
//    DB functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "scauser.h"
#include "sqlutil.h"

struct SCA_DB
{
	// darwin information
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	BOOL fHasComponent;
	WCHAR wzComponent[MAX_DARWIN_KEY + 1];
	INSTALLSTATE isInstalled, isAction;

	WCHAR wzServer[MAX_DARWIN_COLUMN + 1];
	WCHAR wzInstance[MAX_DARWIN_COLUMN + 1];
	WCHAR wzDatabase[MAX_DARWIN_COLUMN + 1];

	int iAttributes;

	BOOL fUseIntegratedAuth;
	SCA_USER scau;

	BOOL fHasDbSpec;
	SQL_FILESPEC sfDb;
	BOOL fHasLogSpec;
	SQL_FILESPEC sfLog;

	SCA_DB* psdNext;
};


// prototypes
HRESULT ScaDbsRead(SCA_DB** ppsdList);

SCA_DB* ScaDbsFindDatabase(LPCWSTR wzSqlDb, SCA_DB* psdList);

HRESULT ScaDbsInstall(SCA_DB* psdList);

HRESULT ScaDbsUninstall(SCA_DB* psdList);

void ScaDbsFreeList(SCA_DB* psdList);

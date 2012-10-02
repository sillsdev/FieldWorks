#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scasqlstr.h" company="Microsoft">
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
//    SQL String functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "scauser.h"
#include "scadb.h"

struct SCA_SQLSTR
{
	// darwin information
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	WCHAR wzComponent[MAX_DARWIN_KEY + 1];
	INSTALLSTATE isInstalled, isAction;

	WCHAR wzSqlDb[MAX_DARWIN_COLUMN + 1];

	BOOL fHasUser;
	SCA_USER scau;

	LPWSTR pwzSql;
	int iAttributes;
	int iSequence; //used to sequence SqlString and SqlScript tables together

	SCA_SQLSTR* psssNext;
};


// prototypes
HRESULT ScaSqlStrsRead(
	SCA_SQLSTR** ppsssList
	);

HRESULT ScaSqlStrsReadScripts(
	SCA_SQLSTR** ppsssList
	);

HRESULT ScaSqlStrsInstall(
	SCA_DB* psdList,
	SCA_SQLSTR* psssList
	);

HRESULT ScaSqlStrsUninstall(
	SCA_DB* psdList,
	SCA_SQLSTR* psssList
	);

void ScaSqlStrsFreeList(
	SCA_SQLSTR* psssList
	);

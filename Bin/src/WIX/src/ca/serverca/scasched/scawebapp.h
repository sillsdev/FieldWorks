#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scawebapp.h" company="Microsoft">
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
//    IIS Web Application functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "scaapppool.h"
#include "scawebappext.h"

// global sql queries provided for optimization
extern LPCWSTR vcsWebApplicationQuery;
const int MAX_APP_NAME = 32;

// structs
struct SCA_WEB_APPLICATION
{
	WCHAR wzName[MAX_APP_NAME + 1];

	int iIsolation;
	BOOL fAllowSessionState;
	int iSessionTimeout;
	BOOL fBuffer;
	BOOL fParentPaths;

	WCHAR wzDefaultScript[MAX_DARWIN_COLUMN + 1];
	int iScriptTimeout;
	BOOL fServerDebugging;
	BOOL fClientDebugging;
	WCHAR wzAppPool[MAX_DARWIN_COLUMN + 1];

	SCA_WEB_APPLICATION_EXTENSION* pswappextList;
};


// prototypes
HRESULT ScaGetWebApplication(MSIHANDLE hViewApplications,
							 LPCWSTR pwzApplication, SCA_WEB_APPLICATION* pswapp);

HRESULT ScaWriteWebApplication(IMSAdminBase* piMetabase, LPCWSTR wzRootOfWeb,
							   SCA_WEB_APPLICATION* pswapp, SCA_APPPOOL * psapList);

#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scamimemap.h" company="Microsoft">
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
//    IIS Mime Map functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

enum eMimeMapParentType	{ mmptVDir = 1 };

struct SCA_MIMEMAP
{
	// iis configuation information
	WCHAR wzMimeMap[MAX_DARWIN_KEY + 1];
	int iParentType;
	WCHAR wzParentValue[MAX_DARWIN_KEY + 1];
	WCHAR wzMimeType[MAX_DARWIN_KEY + 1];
	WCHAR wzExtension[MAX_DARWIN_KEY + 1];


	SCA_MIMEMAP* psmmNext;
};


// prototypes

HRESULT __stdcall ScaMimeMapRead(SCA_MIMEMAP** ppsmmList);

HRESULT ScaGetMimeMap(int iParentType, LPCWSTR wzParentValue, SCA_MIMEMAP **psmmList, SCA_MIMEMAP **ppsmmOut);

HRESULT ScaMimeMapCheckList(SCA_MIMEMAP* psmmList);

void ScaMimeMapFreeList(SCA_MIMEMAP* psmmList);

HRESULT ScaWriteMimeMap(IMSAdminBase* piMetabase, LPCWSTR wzRootOfWeb,
							   SCA_MIMEMAP* psmmList);

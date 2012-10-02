#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scaweberr.h" company="Microsoft">
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
//    IIS Web Error functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

enum eWebErrorParentType { weptVDir = 1, weptWeb };

struct SCA_WEB_ERROR
{
	int iErrorCode;
	int iSubCode;

	int iParentType;
	WCHAR wzParentValue[MAX_DARWIN_KEY + 1];

	WCHAR wzFile[MAX_PATH];
	WCHAR wzURL[MAX_PATH]; // TODO: this needs to be bigger than MAX_PATH

	SCA_WEB_ERROR *psweNext;
};

// prototypes
HRESULT ScaWebErrorRead(SCA_WEB_ERROR **ppsweList);
void ScaWebErrorFreeList(SCA_WEB_ERROR *psweList);
HRESULT ScaWebErrorCheckList(SCA_WEB_ERROR* psweList);
HRESULT ScaGetWebError(int iParentType, LPCWSTR wzParentValue, SCA_WEB_ERROR **ppsweList, SCA_WEB_ERROR **ppsweOut);
HRESULT ScaWriteWebError(IMSAdminBase* piMetabase, int iParentType, LPCWSTR wzRoot, SCA_WEB_ERROR* psweList);

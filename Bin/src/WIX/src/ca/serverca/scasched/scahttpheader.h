#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scahttpHeader.h" company="Microsoft">
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
//    IIS HTTP Header functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

enum eHttpHeaderParentType { hhptVDir = 1, hhptWeb };

struct SCA_HTTP_HEADER
{
	int iParentType;
	WCHAR wzParentValue[MAX_DARWIN_KEY + 1];

	WCHAR wzName[MAX_PATH];
	WCHAR wzValue[MAX_PATH];
	int iAttributes;

	SCA_HTTP_HEADER* pshhNext;
};

// prototypes
HRESULT ScaHttpHeaderRead(
	__in SCA_HTTP_HEADER **ppshhList
	);
void ScaHttpHeaderFreeList(
	__in SCA_HTTP_HEADER *pshhList
	);
HRESULT ScaHttpHeaderCheckList(
	__in SCA_HTTP_HEADER* pshhList
	);
HRESULT ScaGetHttpHeader(
	__in int iParentType,
	__in LPCWSTR wzParentValue,
	__in SCA_HTTP_HEADER** ppshhList,
	__out SCA_HTTP_HEADER** ppshhOut
	);
HRESULT ScaWriteHttpHeader(
	__in IMSAdminBase* piMetabase,
	int iParentType,
	LPCWSTR wzRoot,
	SCA_HTTP_HEADER* pshhList
	);

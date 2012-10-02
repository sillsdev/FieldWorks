#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scassl.h" company="Microsoft">
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
//    SSL functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

// misc macros
#define MD_SSL_CERT_HASH                ( IIS_MD_SSL_BASE+6 )
#define MD_SSL_CERT_STORE_NAME          ( IIS_MD_SSL_BASE+11 )
//#define WIDE(x)		WIDE2(x)
//#define WIDE2(x)	L ## x


// structs
struct SCA_WEB_SSL_CERTIFICATE
{
	WCHAR wzStoreName[65];
	BYTE rgbSHA1Hash[CB_CERTIFICATE_HASH];

	SCA_WEB_SSL_CERTIFICATE* pNext;
};


// prototypes
HRESULT ScaSslCertificateRead(
	__in LPCWSTR wzWebId,
	__inout SCA_WEB_SSL_CERTIFICATE** ppswscList
	);

HRESULT ScaSslCertificateWriteMetabase(
	__in IMSAdminBase* piMetabase,
	__in LPCWSTR wzWebBase,
	__in SCA_WEB_SSL_CERTIFICATE* pswscList
	);

void ScaSslCertificateFreeList(
	__in SCA_WEB_SSL_CERTIFICATE* pswscList
	);

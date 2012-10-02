#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scawebaddr.h" company="Microsoft">
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
//    IIS Web Address functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

// global sql queries provided for optimization
extern LPCWSTR vcsAddressQuery;

// structs
struct SCA_WEB_ADDRESS
{
	WCHAR wzKey [MAX_DARWIN_KEY + 1];

	WCHAR wzIP[MAX_DARWIN_COLUMN + 1];
	int iPort;
	WCHAR wzHeader[MAX_DARWIN_COLUMN + 1];
	BOOL fSecure;
};


// prototypes
HRESULT ScaGetWebAddress(MSIHANDLE hViewAddresses, LPCWSTR wzAddress,
						 SCA_WEB_ADDRESS* pswa);

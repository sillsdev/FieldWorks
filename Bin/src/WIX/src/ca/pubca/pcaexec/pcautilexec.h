//-------------------------------------------------------------------------------------------------
// <copyright file="pcautilexec.cpp" company="Microsoft">
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
//    Public Custom Action utility functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------


HRESULT PcaActionDataMessage(
	DWORD cArgs,
	...
	);
HRESULT PcaAccountNameToSid(
	LPCWSTR pwzAccountName,
	PSID* ppSid
	);
HRESULT PcaSidToAccountName(
	PSID pSid,
	LPWSTR* ppwzAccountName
	);
HRESULT PcaBuildAccountName(
	LPCWSTR pwzDomain,
	LPCWSTR pwzName,
	LPWSTR* ppwzAccount
	);
HRESULT PcaGuidFromString(
	LPCWSTR pwzGuid,
	GUID* pGuid
	);

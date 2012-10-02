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
//    Web address functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// sql queries
LPCWSTR vcsAddressQuery = L"SELECT `Address`, `Web_`, `IP`, `Port`, `Header`, `Secure` "
							 L"FROM `IIsWebAddress` WHERE `Address_`=?";
enum eAddressQuery { aqAddress = 1, aqWeb, aqIP, aqPort, aqHeader, aqSecure };



HRESULT ScaGetWebAddress(MSIHANDLE hViewAddresses, LPCWSTR wzAddress,
						 SCA_WEB_ADDRESS* pswa)
{
	Assert(wzAddress && *wzAddress && pswa);

	HRESULT hr = S_OK;
	PMSIHANDLE hView, hRec;
	LPWSTR pwzData = NULL;

	hr = WcaTableExists(L"IIsWebAddress");
	if (S_FALSE == hr)
		hr = E_ABORT;
	ExitOnFailure(hr, "IIsWebAdddress table does not exists or error");

	hRec = ::MsiCreateRecord(1);
	hr = WcaSetRecordString(hRec, 1, wzAddress);
	ExitOnFailure(hr, "Failed to set record to look up Web Address");

	// if the view wasn't provided open one
	if (!hViewAddresses)
	{
		hr = WcaOpenView(vcsAddressQuery, &hView);
		ExitOnFailure(hr, "Failed to open view on IIsWebAddress table");
	}
	else
		hView = hViewAddresses;

	hr = WcaExecuteView(hView, hRec);
	ExitOnFailure(hr, "Failed to execute view on IIsWebAddress table");

	// get the application information
	hr = WcaFetchSingleRecord(hView, &hRec);
	if (S_OK == hr)
	{
		hr = WcaGetRecordString(hRec, aqAddress, &pwzData);
		ExitOnFailure(hr, "Failed to get Key for Web Address");
		StringCchCopyW(pswa->wzKey, countof(pswa->wzKey), pwzData);

		// TODO: get aqWeb

		hr = WcaGetRecordFormattedString(hRec, aqIP, &pwzData);
		ExitOnFailure(hr, "Failed to get IP for Web Address");
		StringCchCopyW(pswa->wzIP, countof(pswa->wzIP), pwzData);

		hr = WcaGetRecordFormattedString(hRec, aqPort, &pwzData);
		ExitOnFailure(hr, "Failed to get Web Address port");
		pswa->iPort = wcstol(pwzData, NULL, 10);

		hr = WcaGetRecordFormattedString(hRec, aqHeader, &pwzData);
		ExitOnFailure(hr, "Failed to get Header for Web Address");
		StringCchCopyW(pswa->wzHeader, countof(pswa->wzHeader), pwzData);

		hr = WcaGetRecordInteger(hRec, aqSecure, &pswa->fSecure);
		ExitOnFailure(hr, "Failed to get if Web Address is secure");
		if (S_FALSE == hr)
			pswa->fSecure = FALSE;
	}
	else if (E_NOMOREITEMS == hr)
	{
		WcaLog(LOGMSG_STANDARD, "Error: Cannot locate IIsWebAddress.Address='%S'", wzAddress);
		hr = E_FAIL;
	}
	else
		ExitOnFailure(hr, "Error or found multiple matching Address rows");

LExit:
	ReleaseStr(pwzData);

	return hr;
}

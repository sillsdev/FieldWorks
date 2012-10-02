//-------------------------------------------------------------------------------------------------
// <copyright file="scacertexec.h" company="Microsoft">
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
//    Certificate execution functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

#define SIXTY_FOUR_MEG 64 * 1024 * 1024

// prototypes
static HRESULT ExecuteCertificateOperation(
	__in MSIHANDLE hInstall,
	__in SCA_ACTION saAction,
	__in DWORD dwStoreRoot
	);

static HRESULT ReadCertificateFile(
	__in LPCWSTR wzPath,
	__out BYTE** prgbData,
	__out DWORD* pcbData
	);

static HRESULT InstallCertificate(
	__in HCERTSTORE hStore,
	__in BOOL fUserCertificateStore,
	__in LPCWSTR wzName,
	__in_opt BYTE* rgbData,
	__in DWORD cbData,
	__in_opt LPCWSTR wzPFXPassword
	);

static HRESULT UninstallCertificate(
	__in HCERTSTORE hStore,
	__in LPCWSTR wzName
	);


/* ****************************************************************
 AddUserCertificate - CUSTOM ACTION ENTRY POINT for adding per-user
					  certificates

 * ***************************************************************/
extern "C" UINT __stdcall AddUserCertificate(
	__in MSIHANDLE hInstall
	)
{
	HRESULT hr = S_OK;
	DWORD er = ERROR_SUCCESS;

	hr = WcaInitialize(hInstall, "AddUserCertificate");
	ExitOnFailure(hr, "Failed to initialize AddUserCertificate.");

	hr = ExecuteCertificateOperation(hInstall, SCA_ACTION_INSTALL, CERT_SYSTEM_STORE_CURRENT_USER);
	ExitOnFailure(hr, "Failed to install per-user certificate.");

LExit:
	er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/* ****************************************************************
 AddMachineCertificate - CUSTOM ACTION ENTRY POINT for adding
						 per-machine certificates

 * ***************************************************************/
extern "C" UINT __stdcall AddMachineCertificate(
	__in MSIHANDLE hInstall
	)
{
	HRESULT hr = S_OK;
	DWORD er = ERROR_SUCCESS;

	hr = WcaInitialize(hInstall, "AddMachineCertificate");
	ExitOnFailure(hr, "Failed to initialize AddMachineCertificate.");

	hr = ExecuteCertificateOperation(hInstall, SCA_ACTION_INSTALL, CERT_SYSTEM_STORE_LOCAL_MACHINE);
	ExitOnFailure(hr, "Failed to install per-machine certificate.");

LExit:
	er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/* ****************************************************************
 DeleteUserCertificate - CUSTOM ACTION ENTRY POINT for deleting
						 per-user certificates

 * ***************************************************************/
extern "C" UINT __stdcall DeleteUserCertificate(
	__in MSIHANDLE hInstall
	)
{
	HRESULT hr = S_OK;
	DWORD er = ERROR_SUCCESS;

	hr = WcaInitialize(hInstall, "DeleteUserCertificate");
	ExitOnFailure(hr, "Failed to initialize DeleteUserCertificate.");

	hr = ExecuteCertificateOperation(hInstall, SCA_ACTION_UNINSTALL, CERT_SYSTEM_STORE_CURRENT_USER);
	ExitOnFailure(hr, "Failed to uninstall per-user certificate.");

LExit:
	er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/* ****************************************************************
 DeleteMachineCertificate - CUSTOM ACTION ENTRY POINT for deleting
							per-machine certificates

 * ***************************************************************/
extern "C" UINT __stdcall DeleteMachineCertificate(
	__in MSIHANDLE hInstall
	)
{
	HRESULT hr = S_OK;
	DWORD er = ERROR_SUCCESS;

	hr = WcaInitialize(hInstall, "DeleteMachineCertificate");
	ExitOnFailure(hr, "Failed to initialize DeleteMachineCertificate.");

	hr = ExecuteCertificateOperation(hInstall, SCA_ACTION_UNINSTALL, CERT_SYSTEM_STORE_LOCAL_MACHINE);
	ExitOnFailure(hr, "Failed to uninstall per-machine certificate.");

LExit:
	er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


static HRESULT ExecuteCertificateOperation(
	__in MSIHANDLE hInstall,
	__in SCA_ACTION saAction,
	__in DWORD dwStoreLocation
	)
{
	//AssertSz(FALSE, "Debug ExecuteCertificateOperation() here.");
	Assert(saAction & SCA_ACTION_INSTALL || saAction & SCA_ACTION_UNINSTALL);

	HRESULT hr = S_OK;
	LPWSTR pwzCaData = NULL;
	LPWSTR pwz;
	LPWSTR pwzName = NULL;
	LPWSTR pwzStore = NULL;
	int iAttributes = 0;
	LPWSTR pwzPFXPassword = NULL;
	LPWSTR pwzFilePath = NULL;
	BYTE* pbData = NULL;
	DWORD cbData = 0;

	HCERTSTORE hCertStore = NULL;

	hr = WcaGetProperty(L"CustomActionData", &pwzCaData);
	ExitOnFailure(hr, "Failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzCaData);

	pwz = pwzCaData;
	hr = WcaReadStringFromCaData(&pwz, &pwzName);
	ExitOnFailure(hr, "Failed to parse certificate name.");
	hr = WcaReadStringFromCaData(&pwz, &pwzStore);
	ExitOnFailure(hr, "Failed to parse CustomActionData, StoreName");
	hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
	ExitOnFailure(hr, "Failed to parse certificate attribute");
	if (SCA_ACTION_INSTALL == saAction) // install operations need more data
	{
		if (iAttributes & SCA_CERT_INSTALLED_FILE_PATH)
		{
			hr = WcaReadStringFromCaData(&pwz, &pwzFilePath);
			ExitOnFailure(hr, "Failed to parse path to certficate file.");

			hr = FileReadUntil(&pbData, &cbData, pwzFilePath, SIXTY_FOUR_MEG);
			ExitOnFailure(hr, "Failed to read certificate from file path.");
		}
		else
		{
			hr = WcaReadStreamFromCaData(&pwz, &pbData, (DWORD_PTR*)&cbData);
			ExitOnFailure(hr, "Failed to parse certficate stream.");
		}

		hr = WcaReadStringFromCaData(&pwz, &pwzPFXPassword);
		ExitOnFailure(hr, "Failed to parse certificate password.");
	}

	// Open the right store.
	hCertStore = ::CertOpenStore(CERT_STORE_PROV_SYSTEM, 0, NULL, dwStoreLocation, pwzStore);
	MessageExitOnNullWithLastError1(hCertStore, hr, msierrCERTFailedOpen, "Failed to open certificate store: %S", pwzStore);

	if (SCA_ACTION_INSTALL == saAction) // install operations need more data
	{
		hr = InstallCertificate(hCertStore, (dwStoreLocation == CERT_SYSTEM_STORE_CURRENT_USER), pwzName, pbData, cbData, pwzPFXPassword);
		ExitOnFailure(hr, "Failed to install certificate.");
	}
	else
	{
		Assert(SCA_ACTION_UNINSTALL == saAction);

		hr = UninstallCertificate(hCertStore, pwzName);
		ExitOnFailure(hr, "Failed to uninstall certificate.");
	}

LExit:
	if (hCertStore)
	{
		::CertCloseStore(hCertStore, 0);
	}

	ReleaseMem(pbData);
	ReleaseStr(pwzFilePath);
	ReleaseStr(pwzPFXPassword);
	ReleaseStr(pwzStore);
	ReleaseStr(pwzName);
	ReleaseStr(pwzCaData);
	return hr;
}


static HRESULT InstallCertificate(
	__in HCERTSTORE hStore,
	__in BOOL fUserCertificateStore,
	__in LPCWSTR wzName,
	__in_opt BYTE* rgbData,
	__in DWORD cbData,
	__in_opt LPCWSTR wzPFXPassword
	)
{
	HRESULT hr = S_OK;

	HCERTSTORE hPfxCertStore = NULL;
	PCCERT_CONTEXT pCertContext = NULL;
	PCCERT_CONTEXT pCertContextDelete = NULL;
	CERT_BLOB blob = { 0 };
	DWORD dwEncodingType;
	DWORD dwContentType;
	DWORD dwFormatType;

	// Figure out what type of blob (certificate or PFX) we're dealing with here.
	blob.pbData = rgbData;
	blob.cbData = cbData;

	if (!::CryptQueryObject(CERT_QUERY_OBJECT_BLOB, &blob, CERT_QUERY_CONTENT_FLAG_ALL, CERT_QUERY_FORMAT_FLAG_ALL, 0, &dwEncodingType, &dwContentType, &dwFormatType, NULL, NULL, (LPCVOID*)&pCertContext))
	{
		ExitWithLastError1(hr, "Failed to parse the certificate blob: %S", wzName);
	}

	if (!pCertContext)
	{
		// If we have a PFX blob, get the first certificate out of the PFX and use that instead of the PFX.
		if (dwContentType & CERT_QUERY_CONTENT_PFX)
		{
			hPfxCertStore = ::PFXImportCertStore((CRYPT_DATA_BLOB*)&blob, wzPFXPassword, fUserCertificateStore ? CRYPT_USER_KEYSET : CRYPT_MACHINE_KEYSET);
			ExitOnNullWithLastError(hPfxCertStore, hr, "Failed to open PFX file.");

			// There should be at least one certificate in the PFX.
			pCertContext = ::CertEnumCertificatesInStore(hPfxCertStore, NULL);
			ExitOnNullWithLastError(pCertContext, hr, "Failed to read first certificate out of PFX file.");
		}
		else
		{
			hr = E_UNEXPECTED;
			ExitOnFailure(hr, "Unexpected certificate type processed.");
		}
	}


	// Update the friendly name of the certificate to be configured.
	blob.pbData = (BYTE*)wzName;
	blob.cbData = (lstrlenW(wzName) + 1) * sizeof(WCHAR); // including terminating null

	if (!::CertSetCertificateContextProperty(pCertContext, CERT_FRIENDLY_NAME_PROP_ID, 0, &blob))
	{
		ExitWithLastError1(hr, "Failed to set the friendly name of the certificate: %S", wzName);
	}

	WcaLog(LOGMSG_STANDARD, "Adding certificate: %S", wzName);
	if (!::CertAddCertificateContextToStore(hStore, pCertContext, CERT_STORE_ADD_REPLACE_EXISTING, NULL))
	{
		MessageExitOnLastError(hr, msierrCERTFailedAdd, "Failed to add certificate to the store.");
	}

	hr = WcaProgressMessage(COST_CERT_ADD, FALSE);
	ExitOnFailure(hr, "Failed to send install progress message.");

LExit:
	if (pCertContext)
	{
		::CertFreeCertificateContext(pCertContext);
	}

	// Close the stores after the context's are released.
	if (hPfxCertStore)
	{
		::CertCloseStore(hPfxCertStore, 0);
	}

	return hr;
}


static HRESULT UninstallCertificate(
	__in HCERTSTORE hStore,
	__in LPCWSTR wzName
	)
{
	HRESULT hr = S_OK;
	PCCERT_CONTEXT pCertContext = NULL;

	WcaLog(LOGMSG_STANDARD, "Deleting certificate with friendly name: %S", wzName);
/*
	pCertContextDelete = ::CertFindCertificateInStore(hCertStore, PKCS_7_ASN_ENCODING | X509_ASN_ENCODING, 0, CERT_FIND_EXISTING, pCertContext, NULL);
	if (pCertContextDelete)
	{
		if (!::CertDeleteCertificateFromStore(pCertContextDelete))
		{
			ExitWithLastError1(hr, "Failed to delete certificate: %S", wzName);
		}

		pCertContextDelete = NULL; // deleting a certificate free its context.
	}
	// else if we can't find the certificate in the store just ignore everything.
*/

	// Loop through all certificates in the store, deleting the ones that match our friendly name.
	pCertContext = ::CertFindCertificateInStore(hStore, PKCS_7_ASN_ENCODING | X509_ASN_ENCODING, 0, CERT_FIND_ANY, NULL, NULL);
	while (pCertContext)
	{
		WCHAR wzFriendlyName[256] = { 0 };
		DWORD cbFriendlyName = sizeof(wzFriendlyName);

		if (::CertGetCertificateContextProperty(pCertContext, CERT_FRIENDLY_NAME_PROP_ID, reinterpret_cast<BYTE*>(wzFriendlyName), &cbFriendlyName) &&
			CSTR_EQUAL == ::CompareStringW(LOCALE_SYSTEM_DEFAULT, 0, wzName, 0, wzFriendlyName, 0))
		{
			PCCERT_CONTEXT pCertContextDelete = ::CertDuplicateCertificateContext(pCertContext); // duplicate the context so we can delete it with out disrupting the looping
			if(pCertContextDelete)
			{
				if (!::CertDeleteCertificateFromStore(pCertContextDelete))
				{
					WcaLog(LOGMSG_STANDARD, "Failed to delete certificate with friendly name: %S, continuing anyway.", wzFriendlyName);
				}
				pCertContextDelete = NULL;
			}
		}

		 // Next certificate in the store.
		PCCERT_CONTEXT pNext = ::CertFindCertificateInStore(hStore, PKCS_7_ASN_ENCODING | X509_ASN_ENCODING, 0, CERT_FIND_ANY, NULL, pCertContext);
		::CertFreeCertificateContext(pCertContext);
		pCertContext = pNext;
	}

	hr = WcaProgressMessage(COST_CERT_DELETE, FALSE);
	ExitOnFailure(hr, "Failed to send uninstall progress message.");

LExit:
	if(pCertContext)
	{
		::CertFreeCertificateContext(pCertContext);
	}

	return hr;
}

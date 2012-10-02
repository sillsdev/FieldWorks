//-------------------------------------------------------------------------------------------------
// <copyright file="winterop.cpp" company="Microsoft">
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
// WiX shim to native code, when necessary
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


HRESULT ResetAcls(
	__in LPCWSTR pwzFiles[],
	__in DWORD cFiles
	)
{
	HRESULT hr;
	ACL* pacl = NULL;
	DWORD cbAcl = sizeof(ACL);

	OSVERSIONINFO osvi;

	osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);
	if (!::GetVersionExA(&osvi))
	{
		ExitOnLastError(hr, "failed to get OS version");
	}

	// If we're running on NT 4 or earlier, or ME or earlier, don't reset ACLs.
	if (4 >= osvi.dwMajorVersion)
	{
		ExitFunction1(hr = S_FALSE);
	}

	// create an empty (not NULL!) ACL to use on all the files
	pacl = static_cast<ACL*>(MemAlloc(cbAcl, FALSE));
	ExitOnNull(pacl, hr, E_OUTOFMEMORY, "failed to allocate ACL");

	if (!::InitializeAcl(pacl, cbAcl, ACL_REVISION))
	{
		ExitOnLastError(hr, "failed to initialize ACL");
	}

	// reset the existing security permissions on each file
	for (DWORD i = 0; i < cFiles; ++i)
	{
		hr = ::SetNamedSecurityInfoW(const_cast<LPWSTR>(pwzFiles[i]), SE_FILE_OBJECT, DACL_SECURITY_INFORMATION | UNPROTECTED_DACL_SECURITY_INFORMATION, NULL, NULL, pacl, NULL);
		ExitOnFailure1(hr = HRESULT_FROM_WIN32(hr), "failed to set security descriptor for file: %S", pwzFiles[i]);
	}

	AssertSz(::IsValidAcl(pacl), "ResetAcls() - created invalid ACL");
	Assert(S_OK == hr);
LExit:
	if (pacl)
	{
		MemFree(pacl);
	}

	return hr;
}


HRESULT CreateCabBegin(
	__in LPCWSTR wzCab,
	__in LPCWSTR wzCabDir,
	__in DWORD dwMaxSize,
	__in DWORD dwMaxThresh,
	__in COMPRESSION_TYPE ct,
	__out HANDLE *phContext
	)
{
	return CabCBegin(wzCab, wzCabDir, dwMaxSize, dwMaxThresh, ct, phContext);
}


HRESULT CreateCabAddFile(
	__in LPCWSTR wzFile,
	__in LPCWSTR wzToken,
	__in HANDLE hContext
	)
{
	return CabCAddFile(wzFile, wzToken, hContext);
}


HRESULT CreateCabAddFiles(
	__in LPCWSTR pwzFiles[],
	__in LPCWSTR pwzTokens[],
	__in DWORD cFiles,
	__in HANDLE hContext
	)
{
	HRESULT hr = S_OK;
	DWORD i;

	Assert(pwzFiles);
	Assert(hContext);

	for (i = 0; i < cFiles; i++)
	{
		hr = CreateCabAddFile(
			pwzFiles[i],
			pwzTokens ? pwzTokens[i] : NULL,
			hContext
			);
		ExitOnFailure1(hr, "Failed to add file %S to cab", pwzFiles[i]);
	}

LExit:
	return hr;
}


HRESULT CreateCabFinish(
	__in HANDLE hContext
	)
{
	return CabCFinish(hContext);
}


HRESULT ExtractCabBegin()
{
	return CabInitialize(FALSE);
}


HRESULT ExtractCab(
	__in LPCWSTR wzCabinet,
	__in LPCWSTR wzExtractDir
	)
{
	return CabExtract(wzCabinet, L"*", wzExtractDir, NULL, NULL);
}


void ExtractCabFinish()
{
	CabUninitialize();
	return;
}


BOOL WINAPI DllMain(
	__in HINSTANCE hInstance,
	__in DWORD dwReason,
	__in LPVOID lpvReserved
	)
{
	switch(dwReason)
	{
		case DLL_PROCESS_ATTACH:
		case DLL_PROCESS_DETACH:
		case DLL_THREAD_ATTACH:
		case DLL_THREAD_DETACH:
			break;
	}

	return TRUE;
}

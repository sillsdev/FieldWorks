//-------------------------------------------------------------------------------------------------
// <copyright file="dirutil.cpp" company="Microsoft">
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
//    Directory helper funtions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


/*******************************************************************
 DirExists

*******************************************************************/
extern "C" BOOL DAPI DirExists(
	__in LPCWSTR wzPath,
	__out_opt DWORD *pdwAttributes
	)
{
	Assert(wzPath);

	HRESULT hr = S_OK;
	BOOL fExists = FALSE;

	DWORD dwAttributes = ::GetFileAttributesW(wzPath);
	if (0xFFFFFFFF == dwAttributes) // TODO: figure out why "INVALID_FILE_ATTRIBUTES" can't be used here
		ExitFunction();

	if (dwAttributes & FILE_ATTRIBUTE_DIRECTORY)
	{
		if (pdwAttributes)
			*pdwAttributes = dwAttributes;
		fExists = TRUE;
	}

LExit:
	return fExists;
}


/*******************************************************************
 DirCreateTempPath

*******************************************************************/
extern "C" HRESULT DAPI DirCreateTempPath(
	__in LPCWSTR wzPrefix,
	__in LPWSTR wzPath,
	__in DWORD cchPath
	)
{
	Assert(wzPrefix);
	Assert(wzPath);

	HRESULT hr = S_OK;

	WCHAR wzDir[MAX_PATH];
	WCHAR wzFile[MAX_PATH];
	DWORD cch = 0;

	cch = ::GetTempPathW(countof(wzDir), wzDir);
	if (!cch || cch >= countof(wzDir))
	{
		ExitWithLastError(hr, "Failed to GetTempPath.");
	}

	if (!::GetTempFileNameW(wzDir, wzPrefix, 0, wzFile))
	{
		ExitWithLastError(hr, "Failed to GetTempFileName.");
	}

	hr = ::StringCchCopyW(wzPath, cchPath, wzFile);

LExit:
	return hr;
}


/*******************************************************************
 DirEnsureExists

*******************************************************************/
extern "C" HRESULT DAPI DirEnsureExists(
	__in LPCWSTR wzPath,
	__in_opt LPSECURITY_ATTRIBUTES psa
	)
{
	HRESULT hr = S_OK;
	UINT er;

	// try to create this directory
	if (!::CreateDirectoryW(wzPath, psa))
	{
		// if the directory already exists, bail
		er = ::GetLastError();
		if (ERROR_ALREADY_EXISTS == er)
			ExitFunction1(hr = S_OK);

		// get the parent path and try to create it
		LPWSTR pwzLastSlash = NULL;
		for (LPWSTR pwz = const_cast<LPWSTR>(wzPath); *pwz; pwz++)
			if (*pwz == L'\\')
				pwzLastSlash = pwz;

		// if there is no parent directory fail
		ExitOnNullDebugTrace(pwzLastSlash, hr, HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND), "cannot find parent path");

		*pwzLastSlash = L'\0';	// null terminate the parent path
		hr = DirEnsureExists(wzPath, psa);   // recurse!
		*pwzLastSlash = L'\\';  // put the slash back
		ExitOnFailureDebugTrace1(hr, "failed to create path: %S", wzPath);

		// try to create the directory now that all parents are created
		if (!::CreateDirectoryW(wzPath, psa))
		{
			// if the directory already exists for some reason no error
			er = ::GetLastError();
			if (ERROR_ALREADY_EXISTS == er)
				hr = S_FALSE;
			else
				hr = HRESULT_FROM_WIN32(er);
		}
		else
			hr = S_OK;
	}

LExit:
	return hr;
}

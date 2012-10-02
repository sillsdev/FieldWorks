//-------------------------------------------------------------------------------------------------
// <copyright file="fileutil.cpp" company="Microsoft">
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
//    File helper funtions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

/*******************************************************************
 FileFromPath -  returns a pointer to the file part of the path

********************************************************************/
extern "C" LPWSTR DAPI FileFromPath(
	__in LPCWSTR wzPath
	)
{
	if (!wzPath)
		return NULL;

	LPWSTR wzFile = const_cast<LPWSTR>(wzPath);
	for (LPWSTR wz = wzFile; *wz; wz++)
	{
		// valid delineators
		//     \ => Windows path
		//     / => unix and URL path
		//     : => relative path from mapped root
		if (L'\\' == *wz || L'/' == *wz || L':' == *wz)
			wzFile = wz + 1;
	}

	return wzFile;
}


/*******************************************************************
 FileResolvePath - gets the full path to a file resolving environment
				   variables along the way.

********************************************************************/
extern "C" HRESULT DAPI FileResolvePath(
	__in LPCWSTR wzRelativePath,
	__out LPWSTR *ppwzFullPath
	)
{
	Assert(wzRelativePath && *wzRelativePath);

	HRESULT hr = S_OK;
	DWORD cch = 0;
	LPWSTR pwzExpandedPath = NULL;
	DWORD cchExpandedPath = 0;

	LPWSTR pwzFullPath = NULL;
	DWORD cchFullPath = 0;

	LPWSTR wzFileName = NULL;

	//
	// First, expand any environment variables.
	//
	cchExpandedPath = MAX_PATH;
	hr = StrAlloc(&pwzExpandedPath, cchExpandedPath);
	ExitOnFailure(hr, "Failed to allocate space for expanded path.");

	cch = ::ExpandEnvironmentStringsW(wzRelativePath, pwzExpandedPath, cchExpandedPath);
	if (0 == cch)
	{
		ExitWithLastError1(hr, "Failed to expand environment variables in string: %S", wzRelativePath);
	}
	else if (cchExpandedPath < cch)
	{
		cchExpandedPath = cch;
		hr = StrAlloc(&pwzExpandedPath, cchExpandedPath);
		ExitOnFailure(hr, "Failed to re-allocate more space for expanded path.");

		cch = ::ExpandEnvironmentStringsW(wzRelativePath, pwzExpandedPath, cchExpandedPath);
		if (0 == cch)
		{
			ExitWithLastError1(hr, "Failed to expand environment variables in string: %S", wzRelativePath);
		}
		else if (cchExpandedPath < cch)
		{
			hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
			ExitOnFailure(hr, "Failed to allocate buffer for expanded path.");
		}
	}

	//
	// Second, get the full path.
	//
	cchFullPath = MAX_PATH;
	hr = StrAlloc(&pwzFullPath, cchFullPath);
	ExitOnFailure(hr, "Failed to allocate space for full path.");

	cch = ::GetFullPathNameW(pwzExpandedPath, cchFullPath, pwzFullPath, &wzFileName);
	if (0 == cch)
	{
		ExitWithLastError1(hr, "Failed to get full path for string: %S", pwzExpandedPath);
	}
	else if (cchFullPath < cch)
	{
		cchFullPath = cch;
		hr = StrAlloc(&pwzFullPath, cchFullPath);
		ExitOnFailure(hr, "Failed to re-allocate more space for full path.");

		cch = ::GetFullPathNameW(pwzExpandedPath, cchFullPath, pwzFullPath, &wzFileName);
		if (0 == cch)
		{
			ExitWithLastError1(hr, "Failed to get full path for string: %S", pwzExpandedPath);
		}
		else if (cchFullPath < cch)
		{
			hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
			ExitOnFailure(hr, "Failed to allocate buffer for full path.");
		}
	}

	*ppwzFullPath = pwzFullPath;
	pwzFullPath = NULL;

LExit:
	ReleaseStr(pwzFullPath);
	ReleaseStr(pwzExpandedPath);

	return hr;
}


/*******************************************************************
 FileVersionFromString

*******************************************************************/
extern "C" HRESULT DAPI FileVersionFromString(
	__in LPCWSTR wzVersion,
	__out DWORD *pdwVerMajor,
	__out DWORD* pdwVerMinor
	)
{
	Assert(pdwVerMajor && pdwVerMinor);

	HRESULT hr = S_OK;
	LPCWSTR pwz = wzVersion;
	DWORD dw;

	dw = wcstoul(pwz, (WCHAR**)&pwz, 10);
	if (pwz && L'.' == *pwz && dw < 0x10000)
	{
		*pdwVerMajor = dw << 16;
		pwz++;
	}
	else
	{
		ExitFunction1(hr = S_FALSE);
	}

	dw = wcstoul(pwz, (WCHAR**)&pwz, 10);
	if (pwz && L'.' == *pwz && dw < 0x10000)
	{
		*pdwVerMajor |= dw;
		pwz++;
	}
	else
	{
		ExitFunction1(hr = S_FALSE);
	}

	dw = wcstoul(pwz, (WCHAR**)&pwz, 10);
	if (pwz && L'.' == *pwz && dw < 0x10000)
	{
		*pdwVerMinor = dw << 16;
		pwz++;
	}
	else
	{
		ExitFunction1(hr = S_FALSE);
	}

	dw = wcstoul(pwz, (WCHAR**)&pwz, 10);
	if (pwz && L'\0' == *pwz && dw < 0x10000)
	{
		*pdwVerMinor |= dw;
	}
	else
	{
		ExitFunction1(hr = S_FALSE);
	}

LExit:
	return hr;
}


/*******************************************************************
 FileSizeByHandle

********************************************************************/
extern "C" HRESULT DAPI FileSizeByHandle(
	__in HANDLE hFile,
	__out LONGLONG* pllSize
	)
{
	Assert(INVALID_HANDLE_VALUE != hFile && pllSize);
	HRESULT hr;
	LARGE_INTEGER li;

	*pllSize = 0;

	if (!::GetFileSizeEx(hFile, &li))
		ExitOnLastErrorDebugTrace(hr, "failed to get size of file to verify resource");

	*pllSize = li.QuadPart;
	hr = S_OK;
LExit:
	return hr;
}


/*******************************************************************
 FileExistsEx

********************************************************************/
extern "C" BOOL DAPI FileExistsEx(
	__in LPCWSTR wzPath,
	__out_opt DWORD *pdwAttributes
	)
{
	Assert(wzPath && *wzPath);
	BOOL fExists = FALSE;

	WIN32_FIND_DATAW fd;
	HANDLE hff;

	memset(&fd, 0, sizeof(fd));
	if (INVALID_HANDLE_VALUE != (hff = ::FindFirstFileW(wzPath, &fd)))
	{
		::FindClose(hff);
		if (!(fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
		{
			if (pdwAttributes)
				*pdwAttributes = fd.dwFileAttributes;
			fExists = TRUE;
		}
	}

	return fExists;
}


/*******************************************************************
 FileRead - read a file into memory

********************************************************************/
extern "C" HRESULT DAPI FileRead(
	__inout LPBYTE* ppbDest,
	__out DWORD* pcbDest,
	__in LPCWSTR wzSrcPath
	)
{
	HRESULT hr = FileReadUntil(ppbDest, pcbDest, wzSrcPath, 0xFFFFFFFF);
	return hr;
}


/*******************************************************************
 FileRead - read a file into memory with a maximum size

********************************************************************/
extern "C" HRESULT DAPI FileReadUntil(
	__inout LPBYTE* ppbDest,
	__out DWORD* pcbDest,
	__in LPCWSTR wzSrcPath,
	__in DWORD cbMaxRead
	)
{
	Assert(ppbDest && pcbDest && wzSrcPath && *wzSrcPath);

	HRESULT hr = S_OK;

	HANDLE hFile = INVALID_HANDLE_VALUE;
	LARGE_INTEGER liFileSize = { 0 };
	DWORD cbData = 0;
	BYTE* pbData = NULL;

	hFile = ::CreateFileW(wzSrcPath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
	if (INVALID_HANDLE_VALUE == hFile)
	{
		ExitWithLastError1(hr, "Failed to open file: %S", wzSrcPath);
	}

	if (!::GetFileSizeEx(hFile, &liFileSize))
	{
		ExitWithLastError1(hr, "Failed to get size of file: %S", wzSrcPath);
	}

	if (cbMaxRead < liFileSize.QuadPart)
	{
		hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
		ExitOnFailure1(hr, "Failed to load file: %S, too large.", wzSrcPath);
	}

	cbData = liFileSize.LowPart; // should only need the low part because we cap at DWORD

	if (*ppbDest)
	{
		pbData = static_cast<BYTE*>(MemReAlloc(*ppbDest, cbData, TRUE));
	}
	else
	{
		pbData = static_cast<BYTE*>(MemAlloc(cbData, TRUE));
	}
	ExitOnNull1(pbData, hr, E_OUTOFMEMORY, "Failed to allocate memory to read in file: %S", wzSrcPath);

	DWORD cbTotalRead = 0;
	DWORD cbRead = 0;
	do
	{
		if (!::ReadFile(hFile, pbData + cbTotalRead, cbData - cbTotalRead, &cbRead, NULL))
		{
			ExitWithLastError1(hr, "Failed to read from file: %S", wzSrcPath);
		}

		cbTotalRead += cbRead;
	} while (cbRead);

	if (cbTotalRead != cbData)
	{
		hr = E_UNEXPECTED;
		ExitOnFailure1(hr, "Failed to completely read file: %S", wzSrcPath);
	}

	*ppbDest = pbData;
	pbData = NULL;
	*pcbDest = cbData;

LExit:
	ReleaseMem(pbData);
	ReleaseFile(hFile);

	return hr;
}


/*******************************************************************
 FileEnsureMove

*******************************************************************/
extern "C" HRESULT DAPI FileEnsureMove(
	__in LPCWSTR wzSource,
	__in LPCWSTR wzTarget,
	__in BOOL fOverwrite,
	__in BOOL fAllowCopy
	)
{
	HRESULT hr = S_OK;
	DWORD er;

	DWORD dwFlags = 0;

	if (fOverwrite)
	{
		dwFlags |= MOVEFILE_REPLACE_EXISTING;
	}
	if (fAllowCopy)
	{
		dwFlags |= MOVEFILE_COPY_ALLOWED;
	}

	// try to move the file first
	if (::MoveFileExW(wzSource, wzTarget, dwFlags))
	{
		ExitFunction();  // we're done
	}

	er = ::GetLastError();  // check the error and do the right thing below
	if (!fOverwrite && (ERROR_FILE_EXISTS == er || ERROR_ALREADY_EXISTS == er))
	{
		// if not overwriting this is an expected error
		ExitFunction1(hr = S_FALSE);
	}
	else if (ERROR_PATH_NOT_FOUND == er)  // if the path doesn't exist
	{
		// try to create the directory then do the copy
		LPWSTR pwzLastSlash = NULL;
		for (LPWSTR pwz = const_cast<LPWSTR>(wzTarget); *pwz; pwz++)
		{
			if (*pwz == L'\\')
			{
				pwzLastSlash = pwz;
			}
		}

		if (pwzLastSlash)
		{
			*pwzLastSlash = L'\0';	// null terminate
			hr = DirEnsureExists(wzTarget, NULL);
			*pwzLastSlash = L'\\';	// now put the slash back
			ExitOnFailureDebugTrace2(hr, "failed to create directory while moving file: '%S' to: '%S'", wzSource, wzTarget);

			// try to move again
			if (!::MoveFileExW(wzSource, wzTarget, dwFlags))
			{
				ExitOnLastErrorDebugTrace2(hr, "failed to move file: '%S' to: '%S'", wzSource, wzTarget);
			}
		}
		else // no path was specified so just return the error
			hr = HRESULT_FROM_WIN32(er);
	}
	else // unexpected error
		hr = HRESULT_FROM_WIN32(er);

LExit:
	return hr;
}

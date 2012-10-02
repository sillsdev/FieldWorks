#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="fileutil.h" company="Microsoft">
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
//    Header for file helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#ifdef __cplusplus
extern "C" {
#endif

#define ReleaseFile(h) if (INVALID_HANDLE_VALUE != h) { ::CloseHandle(h); h = INVALID_HANDLE_VALUE; }

LPWSTR DAPI FileFromPath(
	__in LPCWSTR wzPath
	);
HRESULT DAPI FileResolvePath(
	__in LPCWSTR wzRelativePath,
	__out LPWSTR *ppwzFullPath
	);
HRESULT DAPI FileVersionFromString(
	__in LPCWSTR wzVersion,
	__out DWORD *pdwVerMajor,
	__out DWORD* pdwVerMinor
	);
HRESULT DAPI FileSizeByHandle(
	__in HANDLE hFile,
	__out LONGLONG* pllSize
	);
BOOL DAPI FileExistsEx(
	__in LPCWSTR wzPath,
	__out_opt DWORD *pdwAttributes
	);
HRESULT DAPI FileRead(
	__out LPBYTE* ppbDest,
	__out DWORD* pcbDest,
	__in LPCWSTR wzSrcPath
	);
HRESULT DAPI FileReadUntil(
	__out LPBYTE* ppbDest,
	__out DWORD* pcbDest,
	__in LPCWSTR wzSrcPath,
	__in DWORD cbMaxRead
	);
HRESULT DAPI FileEnsureMove(
	__in LPCWSTR wzSource,
	__in LPCWSTR wzTarget,
	__in BOOL fOverwrite,
	__in BOOL fAllowCopy
	);

#ifdef __cplusplus
}
#endif

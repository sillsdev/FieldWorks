//-------------------------------------------------------------------------------------------------
// <copyright file="cabcutil.cpp" company="Microsoft">
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
//    Cabinet creation helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

static CAB_CALLBACK_OPEN_FILE vpfnOpenFile = NULL;
static CAB_CALLBACK_READ_FILE vpfnReadFile = NULL;
static CAB_CALLBACK_WRITE_FILE vpfnWriteFile = NULL;
static CAB_CALLBACK_SEEK_FILE vpfnSeekFile = NULL;
static CAB_CALLBACK_CLOSE_FILE vpfnCloseFile = NULL;

static HRESULT vhrLastError = S_OK;

// structs
struct CABC_DATA
{
	HFCI hfci;
	ERF erf;
	CCAB ccab;
	TCOMP tc;
};

//
// prototypes
//
static int DIAMONDAPI CabCFilePlaced(__in PCCAB pccab, __in char *szFile, __in long cbFile, __in BOOL fContinuation, __out void *pv);
static void * DIAMONDAPI CabCAlloc(__in ULONG cb);
static void DIAMONDAPI CabCFree(__out void *pv);
static INT_PTR DIAMONDAPI CabCOpen(__in char *pszFile, __in int oflag, __in int pmode, __out int *err, __out void *pv);
static UINT FAR DIAMONDAPI CabCRead(__in INT_PTR hf, __out void FAR *memory, UINT cb, __out int *err, __out void *pv);
static UINT FAR DIAMONDAPI CabCWrite(__in INT_PTR hf, __in void FAR *memory, __in UINT cb, __out int *err, __out void *pv);
static long FAR DIAMONDAPI CabCSeek(__in INT_PTR hf, __in long dist, __in int seektype, __out int *err, __out void *pv);
static int FAR DIAMONDAPI CabCClose(__in INT_PTR hf, __out int *err, __out void *pv);
static int DIAMONDAPI CabCDelete(__in char *szFile, __out int *err, __out void *pv);
static BOOL DIAMONDAPI CabCGetTempFile(__out_ecount(cchFile) char *szFile, __in int cchFile, __out void *pv);
static BOOL DIAMONDAPI CabCGetNextCabinet(__in PCCAB pccab, __in ULONG ul, __out void *pv);
static INT_PTR DIAMONDAPI CabCGetOpenInfo(__in char *pszName, __out USHORT *pdate, __out USHORT *ptime, __out USHORT *pattribs, __out int *err, __out void *pv);
static long DIAMONDAPI CabCStatus(__in UINT ui, __in ULONG cb1, __in ULONG cb2, __out void *pv);


/********************************************************************
CabcBegin - begins creating a cabinet

NOTE: phContext must be the same handle used in AddFile and Finish.
wzCabDir can be L"", but not NULL.  dwMaxSize and dwMaxThresh can
be NULL.  A large default value will be used in that case.  tc can be NULL.
LZX with Lowest Memory will be used as a default.
********************************************************************/
extern "C" HRESULT DAPI CabCBegin(
	__in LPCWSTR wzCab,
	__in LPCWSTR wzCabDir,
	__in DWORD dwMaxSize,
	__in DWORD dwMaxThresh,
	__in COMPRESSION_TYPE ct,
	__out HANDLE *phContext
	)
{
	Assert(wzCab && *wzCab && phContext);

	HRESULT hr = S_OK;
	CABC_DATA *pcd = NULL;

	WCHAR wzPathBuffer [MAX_PATH] = L"";
	size_t cchPathBuffer;
	if (wzCabDir)
	{
		hr = StringCchLengthW(wzCabDir, MAX_PATH, &cchPathBuffer);
		ExitOnFailure(hr, "Failed to get length of cab directory");

		// Need room to terminate with L'\\' and L'\0'
		Assert(cchPathBuffer < (MAX_PATH -1));

		hr = StringCchCopyW(wzPathBuffer, countof(wzPathBuffer), wzCabDir);
		ExitOnFailure(hr, "Failed to copy cab directory to buffer");

		if (L'\\' != wzPathBuffer[cchPathBuffer - 1])
		{
			hr = StringCchCatW(wzPathBuffer, countof(wzPathBuffer), L"\\");
			ExitOnFailure(hr, "Failed to cat \\ to end of buffer");
			cchPathBuffer++;
		}
	}

	pcd = reinterpret_cast<CABC_DATA*>(MemAlloc(sizeof(CABC_DATA), TRUE));
	ExitOnNull(pcd, hr, E_OUTOFMEMORY, "failed to allocate cab creation data structure");

	if (NULL == dwMaxSize)
		pcd->ccab.cb = CAB_MAX_SIZE;
	else
		pcd->ccab.cb = dwMaxSize;

	if (NULL == dwMaxThresh)
		pcd->ccab.cbFolderThresh = CAB_MAX_SIZE;
	else
		pcd->ccab.cbFolderThresh = dwMaxThresh;

	// Translate the compression type
	if (COMPRESSION_TYPE_NONE == ct)
		pcd->tc = tcompTYPE_NONE;
	else if (COMPRESSION_TYPE_LOW == ct)
		pcd->tc = tcompTYPE_LZX | tcompLZX_WINDOW_LO;
	else if (COMPRESSION_TYPE_MEDIUM == ct)
		pcd->tc = TCOMPfromLZXWindow(18);
	else if (COMPRESSION_TYPE_HIGH == ct)
		pcd->tc = tcompTYPE_LZX | tcompLZX_WINDOW_HI;
	else if (COMPRESSION_TYPE_MSZIP == ct)
		pcd->tc = tcompTYPE_MSZIP;
	else
		ExitOnFailure(hr = E_INVALIDARG, "invalid compression type specified");

	if (0 == ::WideCharToMultiByte(CP_ACP, 0, wzCab, -1, pcd->ccab.szCab, sizeof(pcd->ccab.szCab), NULL, NULL))
		ExitOnLastError(hr, "failed to convert cab name to multi-byte");

	if (0 ==  ::WideCharToMultiByte(CP_ACP, 0, wzPathBuffer, -1, pcd->ccab.szCabPath, sizeof(pcd->ccab.szCab), NULL, NULL))
		ExitOnLastError(hr, "failed to convert cab dir to multi-byte");

	pcd->hfci = ::FCICreate(&(pcd->erf), CabCFilePlaced, CabCAlloc, CabCFree, CabCOpen, CabCRead, CabCWrite, CabCClose, CabCSeek, CabCDelete, CabCGetTempFile, &(pcd->ccab), NULL);

	if (pcd->hfci == NULL || pcd->erf.fError)
	{
		// If we have a last error, use that, otherwise return the useless error
		hr = FAILED(vhrLastError) ? vhrLastError : E_FAIL;
		ExitOnFailure2(hr, "failed to create FCI object Oper: 0x%x Type: 0x%x", pcd->erf.erfOper, pcd->erf.erfType);  // TODO: can these be converted to HRESULTS?
	}

	*phContext = pcd;
LExit:
	if (FAILED(hr) && pcd && pcd->hfci)
		::FCIDestroy(pcd->hfci);
	return hr;
}


/********************************************************************
CabCNextCab - This will be useful when creating multiple cabs.
Haven't needed it yet.
********************************************************************/
extern "C" HRESULT DAPI CabCNextCab(
	__in HANDLE hContext
	)
{
	// TODO: Make the appropriate FCIFlushCabinet and FCIFlushFolder calls
	return E_NOTIMPL;
}


/********************************************************************
CabcAddFile - adds a file to a cabinet

NOTE: hContext must be the same used in Begin and Finish
if wzToken is null, the file's original name is used within the cab
********************************************************************/
extern "C" HRESULT DAPI CabCAddFile(
	__in LPCWSTR wzFile,
	__in LPCWSTR wzToken,
	__in HANDLE hContext
	)
{
	Assert(wzFile && *wzFile && hContext);

	HRESULT hr = S_OK;
	LPCWSTR wzFileName = NULL;
	LPSTR pszFile = NULL;
	LPSTR pszFileName = NULL;
	CABC_DATA *pcd = reinterpret_cast<CABC_DATA*>(hContext);

	// use the token if given
	if (wzToken && *wzToken)
		wzFileName = wzToken;
	else
		wzFileName = FileFromPath(wzFile);

	// convert filenames to ANSI
	hr = StrAnsiAllocString(&pszFile, wzFile, 0, CP_ACP);
	ExitOnFailure1(hr, "failed to convert to ANSI: %S", wzFile);
	hr = StrAnsiAllocString(&pszFileName, wzFileName, 0, CP_ACP);
	ExitOnFailure1(hr, "failed to convert to ANSI: %S", wzFileName);

	// add the file to the cab
	if (!::FCIAddFile(pcd->hfci, pszFile, pszFileName, FALSE, CabCGetNextCabinet, CabCStatus, CabCGetOpenInfo, pcd->tc))
	{
		// If we have a last error, use that, otherwise return the useless error
		hr = FAILED(vhrLastError) ? vhrLastError : E_FAIL;
		ExitOnFailure3(hr, "failed to add file to FCI object Oper: 0x%x Type: 0x%x File: %s", pcd->erf.erfOper, pcd->erf.erfType, pszFile);  // TODO: can these be converted to HRESULTS?
	}

LExit:
	ReleaseNullStr(pszFile);
	ReleaseNullStr(pszFileName);

	return hr;
}


/********************************************************************
CabcFinish - finishes making a cabinet

NOTE: hContext must be the same used in Begin and AddFile
*********************************************************************/
extern "C" HRESULT DAPI CabCFinish(
	__in HANDLE hContext
	)
{
	Assert(hContext);

	HRESULT hr = S_OK;
	CABC_DATA *pcd = reinterpret_cast<CABC_DATA*>(hContext);

	if (!::FCIFlushCabinet(pcd->hfci, FALSE, CabCGetNextCabinet, CabCStatus))
	{
		// If we have a last error, use that, otherwise return the useless error
		hr = FAILED(vhrLastError) ? vhrLastError : E_FAIL;
		ExitOnFailure2(hr, "failed to flush FCI object Oper: 0x%x Type: 0x%x", pcd->erf.erfOper, pcd->erf.erfType);  // TODO: can these be converted to HRESULTS?
	}

	::FCIDestroy(pcd->hfci);

	MemFree(pcd);

LExit:
	return hr;
}


//
// private
//

/********************************************************************
FCI callback functions

*********************************************************************/

static int DIAMONDAPI CabCFilePlaced(
	__in PCCAB pccab,
	__in char *szFile,
	__in long cbFile,
	__in BOOL fContinuation,
	__out void *pv
	)
{
	return 0;
}


static void * DIAMONDAPI CabCAlloc(
	__in ULONG cb
	)
{
	return MemAlloc(cb, FALSE);
}


static void DIAMONDAPI CabCFree(
	__out void *pv
	)
{
	MemFree(pv);
}


static INT_PTR DIAMONDAPI CabCOpen(
	__in char *pszFile,
	__in int oflag,
	__in int pmode,
	__out int *err,
	__out void *pv
	)
{
	HRESULT hr = S_OK;
	INT_PTR pFile = -1;
	DWORD dwAccess = 0;
	DWORD dwDisposition = 0;
	DWORD dwAttributes = 0;

	//
	// Translate flags for CreateFile
	//
	if (oflag & _O_CREAT)
	{
		if (pmode == _S_IREAD)
			dwAccess |= GENERIC_READ;
		else if (pmode == _S_IWRITE)
			dwAccess |= GENERIC_WRITE;
		else if (pmode == (_S_IWRITE | _S_IREAD))
			dwAccess |= GENERIC_READ | GENERIC_WRITE;

		if (oflag & _O_SHORT_LIVED)
			dwDisposition = FILE_ATTRIBUTE_TEMPORARY;
		else if (oflag & _O_TEMPORARY)
			dwAttributes |= FILE_FLAG_DELETE_ON_CLOSE;
		else if (oflag & _O_EXCL)
			dwDisposition = CREATE_NEW;
	}
	if (oflag & _O_TRUNC)
		dwDisposition = CREATE_ALWAYS;

	if (!dwAccess)
		dwAccess = GENERIC_READ;
	if (!dwDisposition)
		dwDisposition = OPEN_EXISTING;
	if (!dwAttributes)
		dwAttributes = FILE_ATTRIBUTE_NORMAL;

	if (vpfnOpenFile)
	{
		WCHAR wzFile[MAX_PATH];
		DWORD cchFile = countof(wzFile);

		cchFile = ::MultiByteToWideChar(CP_ACP, 0, pszFile, -1, wzFile, cchFile);
		if (0 == cchFile)
			ExitOnLastError1(hr, "failed to convert to unicode, path: %s", pszFile);

		hr = vpfnOpenFile(wzFile, &pFile);
	}
	else   // do the default
	{
		pFile = reinterpret_cast<INT_PTR>(::CreateFileA(pszFile, dwAccess, FILE_SHARE_READ, NULL, dwDisposition, dwAttributes, NULL));
		if (INVALID_HANDLE_VALUE == reinterpret_cast<HANDLE>(pFile))
			ExitOnLastError1(hr, "failed to open file: %s", pszFile);
	}

LExit:
	if (FAILED(hr))
		vhrLastError = hr;

	return FAILED(hr) ? -1 : pFile;
}


static UINT FAR DIAMONDAPI CabCRead(
	__in INT_PTR hf,
	__out void FAR *memory,
	__in UINT cb,
	__out int *err,
	__out void *pv
	)
{
	HRESULT hr = S_OK;
	DWORD cbRead = 0;

	if (vpfnReadFile)
	{
		hr = vpfnReadFile(hf, pv, cb, &cbRead);
	}
	else
	{
		Assert(hf);
		if (!::ReadFile(reinterpret_cast<HANDLE>(hf), memory, cb, &cbRead, NULL))
		{
			*err = ::GetLastError();
			ExitOnLastError(hr, "failed to read during cabinet extraction");
		}
	}

LExit:
	if (FAILED(hr))
		vhrLastError = hr;

	return FAILED(hr) ? -1 : cbRead;
}


static UINT FAR DIAMONDAPI CabCWrite(
	__in INT_PTR hf,
	__in void FAR *memory,
	__in UINT cb,
	__out int *err,
	__out void *pv
	)
{
	HRESULT hr = S_OK;
	DWORD cbWrite = 0;

	if (vpfnWriteFile)
	{
		hr = vpfnWriteFile(hf, pv, cb, &cbWrite);
	}
	else
	{
		Assert(hf);
		if (!::WriteFile(reinterpret_cast<HANDLE>(hf), memory, cb, &cbWrite, NULL))
		{
			*err = ::GetLastError();
			ExitOnLastError(hr, "failed to write during cabinet extraction");
		}
	}

LExit:
	if (FAILED(hr))
		vhrLastError = hr;

	return FAILED(hr) ? -1 : cbWrite;
}


static long FAR DIAMONDAPI CabCSeek(
	__in INT_PTR hf,
	__in long dist,
	__in int seektype,
	__out int *err,
	__out void *pv
	)
{
	HRESULT hr = S_OK;
	DWORD dwMoveMethod;
	LONG lMove = 0;

	switch (seektype)
	{
	case 0:   // SEEK_SET
		dwMoveMethod = FILE_BEGIN;
		break;
	case 1:   /// SEEK_CUR
		dwMoveMethod = FILE_CURRENT;
		break;
	case 2:   // SEEK_END
		dwMoveMethod = FILE_END;
		break;
	default :
		dwMoveMethod = 0;
		ExitOnFailure1(hr = E_UNEXPECTED, "unexpected seektype in FDISeek(): %d", seektype);
	}

	if (vpfnSeekFile)
	{
		lMove = vpfnSeekFile(hf, dist, dwMoveMethod);
	}
	else
	{
		// SetFilePointer returns -1 if it fails (this will cause FDI to quit with an FDIERROR_USER_ABORT error.
		// (Unless this happens while working on a cabinet, in which case FDI returns FDIERROR_CORRUPT_CABINET)
		lMove = ::SetFilePointer(reinterpret_cast<HANDLE>(hf), dist, NULL, dwMoveMethod);
		if (0xFFFFFFFF == lMove)
		{
			*err = ::GetLastError();
			ExitOnLastError1(hr, "failed to move file pointer %d bytes", dist);
		}
	}

LExit:
	if (FAILED(hr))
		vhrLastError = hr;

	return FAILED(hr) ? -1 : lMove;
}


static int FAR DIAMONDAPI CabCClose(
	__in INT_PTR hf,
	__out int *err,
	__out void *pv
	)
{
	HRESULT hr = S_OK;

	if (vpfnCloseFile)
	{
		hr = vpfnCloseFile(hf);
	}
	else
	{
		if (!::CloseHandle(reinterpret_cast<HANDLE>(hf)))
		{
			*err = ::GetLastError();
			ExitOnLastError(hr, "failed to close file during cabinet extraction");
		}
	}

LExit:
	if (FAILED(hr))
		vhrLastError = hr;

	return FAILED(hr) ? -1 : 0;
}


static int DIAMONDAPI CabCDelete(
	__in char *szFile,
	__out int *err,
	__out void *pv
	)
{
	::DeleteFileA(szFile);
	return 0;
}


static BOOL DIAMONDAPI CabCGetTempFile(
	__out_ecount(cchFile) char *szFile,
	__in int cchFile,
	__out void *pv
	)
{
	static DWORD dwIndex = 0;

	HRESULT hr;

	LPSTR pszTempPath = NULL;
	DWORD cchTempPath = MAX_PATH;

	LPSTR pszTempFile = NULL;
	DWORD cchTempFile = 0;

	DWORD dwProcessId = ::GetCurrentProcessId();
	HANDLE hTempFile = INVALID_HANDLE_VALUE;

	hr = StrAnsiAlloc(&pszTempPath, cchTempPath);
	ExitOnFailure(hr, "failed to allocate memory for the temp path");
	::GetTempPathA(cchTempPath, pszTempPath);

	for (DWORD i = 0; i < 0xFFFFFFFF; ++i)
	{
		::InterlockedIncrement(reinterpret_cast<LONG*>(&dwIndex));

		hr = StrAnsiAllocFormatted(&pszTempFile, "%s\\%08x.%03x", pszTempPath, dwIndex, dwProcessId);
		ExitOnFailure(hr, "failed to allocate memory for log file");

		hTempFile = ::CreateFileA(pszTempFile, 0, FILE_SHARE_DELETE, NULL, CREATE_NEW, FILE_ATTRIBUTE_TEMPORARY | FILE_FLAG_DELETE_ON_CLOSE, NULL);
		if (INVALID_HANDLE_VALUE != hTempFile)
		{
			// we found one that doesn't exist
			hr = S_OK;
			break;
		}
		else
		{
			hr = E_FAIL; // this file was taken so be pessimistic and assume we're not going to find one.
		}
	}
	ExitOnFailure(hr, "failed to find temporary file.");

	// TODO: Remember temp files so that we can ensure they're cleaned up later (especially if there's a failure)

	hr = StringCchCopyA(szFile, cchFile, pszTempFile);
	ExitOnFailure1(hr, "failed to copy to out parameter filename: %s", pszTempFile);

LExit:
	ReleaseStr(pszTempFile);
	ReleaseStr(pszTempPath);

	if (INVALID_HANDLE_VALUE != hTempFile)
	{
		::CloseHandle(hTempFile);
	}

	if (FAILED(hr))
	{
		vhrLastError = hr;
	}

	return FAILED(hr)? FALSE : TRUE;
}


static BOOL DIAMONDAPI CabCGetNextCabinet(
	__in PCCAB pccab,
	__in ULONG ul,
	__out void *pv
	)
{
	return(FALSE);
}


static INT_PTR DIAMONDAPI CabCGetOpenInfo(
	__in char *pszName,
	__out USHORT *pdate,
	__out USHORT *ptime,
	__out USHORT *pattribs,
	__out int *err,
	__out void *pv
	)
{
	WIN32_FILE_ATTRIBUTE_DATA fad;

	if (::GetFileAttributesEx(pszName, GetFileExInfoStandard, &fad))
	{
		*pattribs = static_cast<USHORT>(fad.dwFileAttributes);
		FILETIME ftLocal;
		::FileTimeToLocalFileTime(&fad.ftLastWriteTime, &ftLocal);
		::FileTimeToDosDateTime(&ftLocal, pdate, ptime);
	}
	else
	{
		*err = ::GetLastError();
	}

	return CabCOpen(pszName, _O_BINARY|_O_RDONLY, 0, err, pv);
}


static long DIAMONDAPI CabCStatus(
	__in UINT ui,
	__in ULONG cb1,
	__in ULONG cb2,
	__out void *pv
	)
{
	return 0;
}

//-------------------------------------------------------------------------------------------------
// <copyright file="cabutil.cpp" company="Microsoft">
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
//    Cabinet decompression helper functions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// external prototypes
typedef BOOL (FAR DIAMONDAPI *PFNFDIDESTROY)(VOID*);
typedef HFDI (FAR DIAMONDAPI *PFNFDICREATE)(PFNALLOC, PFNFREE, PFNOPEN, PFNREAD, PFNWRITE, PFNCLOSE, PFNSEEK, int, PERF);
typedef BOOL (FAR DIAMONDAPI *PFNFDIISCABINET)(HFDI, INT_PTR, PFDICABINETINFO);
typedef BOOL (FAR DIAMONDAPI *PFNFDICOPY)(HFDI, char *, char *, int, PFNFDINOTIFY, PFNFDIDECRYPT, void *);


//
// static globals
//
static HMODULE vhCabinetDll = NULL;
static CAB_CALLBACK_OPEN_FILE vpfnOpenFile = NULL;
static CAB_CALLBACK_READ_FILE vpfnReadFile = NULL;
static CAB_CALLBACK_WRITE_FILE vpfnWriteFile = NULL;
static CAB_CALLBACK_SEEK_FILE vpfnSeekFile = NULL;
static CAB_CALLBACK_CLOSE_FILE vpfnCloseFile = NULL;
static CAB_CALLBACK_BEGIN_FILE vpfnBeginFile = NULL;
static CAB_CALLBACK_END_FILE vpfnEndFile = NULL;

static HFDI vhfdi = NULL;
static PFNFDICREATE vpfnFDICreate = NULL;
static PFNFDICOPY vpfnFDICopy = NULL;
static PFNFDIISCABINET vpfnFDIIsCabinet = NULL;
static PFNFDIDESTROY vpfnFDIDestroy = NULL;
static ERF verf;

//
// structs
//
struct CAB_CALLBACK_STRUCT
{
	BOOL fStopExtracting;   // flag set when no more files are needed
	LPCWSTR pwzExtract;         // file to extract ("*" means extract all)
	LPCWSTR pwzExtractDir;      // directory to extract files to

	// possible user data
	CAB_CALLBACK_PROGRESS pfnProgress;
	LPVOID pvContext;
};

//
// prototypes
//
static LPVOID DIAMONDAPI CabExtractAlloc(__in DWORD dwSize);
static void DIAMONDAPI CabExtractFree(__in LPVOID pvData);
static INT_PTR FAR DIAMONDAPI CabExtractOpen(__in char FAR *pszFile, __in int oflag, __in int pmode);
static UINT FAR DIAMONDAPI CabExtractRead(__in INT_PTR hf, __out void FAR *pv, __in UINT cb);
static UINT FAR DIAMONDAPI CabExtractWrite(__in INT_PTR hf, __in void FAR *pv, __in UINT cb);
static int FAR DIAMONDAPI CabExtractClose(__in INT_PTR hf);
static long FAR DIAMONDAPI CabExtractSeek(__in INT_PTR hf, __in long dist, __in int seektype);
static INT_PTR DIAMONDAPI CabExtractCallback(__in FDINOTIFICATIONTYPE iNotification, __inout FDINOTIFICATION *pFDINotify);


inline HRESULT LoadCabinetDll()
{
	HRESULT hr = S_OK;
	if (!vhCabinetDll)
	{
		vhCabinetDll = ::LoadLibraryW(L"CABINET");
		ExitOnNullWithLastError(vhCabinetDll, hr, "failed to load CABINET.DLL");

		// retrieve all address functions
		vpfnFDICreate = reinterpret_cast<PFNFDICREATE>(::GetProcAddress(vhCabinetDll, "FDICreate"));
		ExitOnNullWithLastError(vpfnFDICreate, hr, "failed to import FDICreate from CABINET.DLL");
		vpfnFDICopy = reinterpret_cast<PFNFDICOPY>(::GetProcAddress(vhCabinetDll, "FDICopy"));
		ExitOnNullWithLastError(vpfnFDICopy, hr, "failed to import FDICopy from CABINET.DLL");
		vpfnFDIIsCabinet = reinterpret_cast<PFNFDIISCABINET>(::GetProcAddress(vhCabinetDll, "FDIIsCabinet"));
		ExitOnNullWithLastError(vpfnFDIIsCabinet, hr, "failed to import FDIIsCabinetfrom CABINET.DLL");
		vpfnFDIDestroy = reinterpret_cast<PFNFDIDESTROY>(::GetProcAddress(vhCabinetDll, "FDIDestroy"));
		ExitOnNullWithLastError(vpfnFDIDestroy, hr, "failed to import FDIDestroyfrom CABINET.DLL");

		vhfdi = vpfnFDICreate(CabExtractAlloc, CabExtractFree, CabExtractOpen, CabExtractRead, CabExtractWrite, CabExtractClose, CabExtractSeek, cpuUNKNOWN, &verf);
		ExitOnNull(vhfdi, hr, E_FAIL, "failed to initialize cabinet.dll");
	}

LExit:
	if (FAILED(hr) && vhCabinetDll)
	{
		::FreeLibrary(vhCabinetDll);
		vhCabinetDll = NULL;
	}

	return hr;
}


/********************************************************************
 CabInitialize - initializes internal static variables

********************************************************************/
extern "C" HRESULT DAPI CabInitialize(
	__in BOOL fDelayLoad
	)
{
	HRESULT hr = S_OK;

	if (!fDelayLoad)
	{
		hr = LoadCabinetDll();
		ExitOnFailure(hr, "failed to load CABINET.DLL");
	}

LExit:
	return hr;
}


/********************************************************************
 CabUninitialize - initializes internal static variables

********************************************************************/
extern "C" void DAPI CabUninitialize(
	)
{
	if (vhfdi)
	{
		if (vpfnFDIDestroy)
			vpfnFDIDestroy(vhfdi);
		vhfdi = NULL;
	}

	vpfnFDICreate = NULL;
	vpfnFDICopy =NULL;
	vpfnFDIIsCabinet = NULL;
	vpfnFDIDestroy = NULL;

	vpfnOpenFile = NULL;
	vpfnReadFile = NULL;
	vpfnWriteFile = NULL;
	vpfnSeekFile = NULL;
	vpfnCloseFile = NULL;

	if (vhCabinetDll)
	{
		::FreeLibrary(vhCabinetDll);
		vhCabinetDll = NULL;
	}
}


/********************************************************************
 CabExtract - extracts one or all files from a cabinet

 NOTE: wzCabinet must be full path to cabinet file
	   wzExtractFile can be a single file id or "*" to extract all files
	   wzExttractDir must be normalized (end in a "\")
	   if pfnBeginFile is NULL pfnEndFile must be NULL and vice versa
********************************************************************/
extern "C" HRESULT DAPI CabExtract(
	__in LPCWSTR wzCabinet,
	__in LPCWSTR wzExtractFile,
	__in LPCWSTR wzExtractDir,
	__in CAB_CALLBACK_PROGRESS pfnProgress,
	__in LPVOID pvContext
	)
{
	HRESULT hr = S_OK;
	BOOL fResult;

	LPCWSTR pwz = NULL;
	CHAR szCabDirectory[MAX_PATH];
	CHAR szCabFile[MAX_PATH];

	CAB_CALLBACK_STRUCT ccs;

	//
	// ensure the cabinet.dll is loaded
	//
	if (!vhfdi)
	{
		hr = LoadCabinetDll();
		ExitOnFailure(hr, "failed to load CABINET.DLL");
	}

	//
	// split the cabinet full path into directory and filename and convert to multi-byte (ick!)
	//
	pwz = FileFromPath(wzCabinet);
	ExitOnNull1(pwz, hr, E_INVALIDARG, "failed to process cabinet path: %S", wzCabinet);

	if (!::WideCharToMultiByte(CP_ACP, 0, wzCabinet, (int)(pwz - wzCabinet), szCabDirectory, countof(szCabDirectory), NULL, NULL))
		ExitOnLastError1(hr, "failed to convert cabinet directory to ASCII: %S", wzCabinet);
	*(szCabDirectory + static_cast<int>(pwz - wzCabinet)) = '\0';

	if (!::WideCharToMultiByte(CP_ACP, 0, pwz, -1, szCabFile, countof(szCabFile), NULL, NULL))
		ExitOnLastError1(hr, "failed to convert cabinet filename to ASCII: %S", pwz);

	//
	// iterate through files in cabinet extracting them to the callback function
	//
	ccs.fStopExtracting = FALSE;
	ccs.pwzExtract = wzExtractFile;
	ccs.pwzExtractDir = wzExtractDir;
	ccs.pfnProgress = pfnProgress;
	ccs.pvContext = pvContext;

	fResult = vpfnFDICopy(vhfdi, szCabFile, szCabDirectory, 0, CabExtractCallback, NULL, static_cast<void*>(&ccs));
	if (!fResult && !ccs.fStopExtracting)   // if something went wrong and it wasn't us just stopping the extraction, then return a failure
		ExitOnLastError1(hr, "failed to extract cabinet file: %S", wzCabinet);

LExit:
	return hr;
}


//
// private
//

/****************************************************************************
 default extract routines

****************************************************************************/
static LPVOID DIAMONDAPI CabExtractAlloc(__in DWORD dwSize)
{
	return MemAlloc(dwSize, FALSE);
}


static void DIAMONDAPI CabExtractFree(LPVOID pvData)
{
	MemFree(pvData);
}


static INT_PTR FAR DIAMONDAPI CabExtractOpen(__in char FAR *pszFile, __in int oflag, __in int pmode)
{
	HRESULT hr = S_OK;
	INT_PTR pFile = -1;

	// if FDI asks for some crazy mode (in low memory situation it could ask for a scratch file) fail
	if ((oflag != (/*_O_BINARY*/ 0x8000 | /*_O_RDONLY*/ 0x0000)) || (pmode != (_S_IREAD | _S_IWRITE)))
		ExitOnFailure(hr = E_OUTOFMEMORY, "FDI asked for to create a scratch file, which is crazy madness");

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
		pFile = reinterpret_cast<INT_PTR>(::CreateFileA(pszFile, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL));
		if (INVALID_HANDLE_VALUE == reinterpret_cast<HANDLE>(pFile))
			ExitOnLastError1(hr, "failed to open file: %s", pszFile);
	}

LExit:
	return FAILED(hr) ? -1 : pFile;
}


static UINT FAR DIAMONDAPI CabExtractRead(__in INT_PTR hf, __out void FAR *pv, __in UINT cb)
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
		if (!::ReadFile(reinterpret_cast<HANDLE>(hf), pv, cb, &cbRead, NULL))
			ExitOnLastError(hr, "failed to read during cabinet extraction");
	}

LExit:
	return FAILED(hr) ? -1 : cbRead;
}


static UINT FAR DIAMONDAPI CabExtractWrite(__in INT_PTR hf, __in void FAR *pv, __in UINT cb)
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
		if (!::WriteFile(reinterpret_cast<HANDLE>(hf), pv, cb, &cbWrite, NULL))
			ExitOnLastError(hr, "failed to write during cabinet extraction");
	}

LExit:
	return FAILED(hr) ? -1 : cbWrite;
}


static long FAR DIAMONDAPI CabExtractSeek(__in INT_PTR hf, __in long dist, __in int seektype)
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
			ExitOnLastError1(hr, "failed to move file pointer %d bytes", dist);
	}

LExit:
	return FAILED(hr) ? -1 : lMove;
}


int FAR DIAMONDAPI CabExtractClose(__in INT_PTR hf)
{
	HRESULT hr = S_OK;

	if (vpfnCloseFile)
	{
		hr = vpfnCloseFile(hf);
	}
	else
	{
		if (!::CloseHandle(reinterpret_cast<HANDLE>(hf)))
			ExitOnLastError(hr, "failed to close file during cabinet extraction");
	}

LExit:
	return FAILED(hr) ? -1 : 0;
}


static INT_PTR DIAMONDAPI CabExtractCallback(__in FDINOTIFICATIONTYPE iNotification, __inout FDINOTIFICATION *pFDINotify)
{
	Assert(pFDINotify->pv);

	HRESULT hr = S_OK;
	INT_PTR ipResult = 0;   // result to return on success

	CAB_CALLBACK_STRUCT* pccs = static_cast<CAB_CALLBACK_STRUCT*>(pFDINotify->pv);
	LPCSTR sz;
	WCHAR wz[MAX_PATH];
	FILETIME ft;

	switch(iNotification)
	{
	case fdintCOPY_FILE:  // begin extracting a resource from cabinet
		Assert(pFDINotify->psz1);

		if (pccs->fStopExtracting)
			ExitFunction1(hr = S_FALSE);   // no more extracting

		// convert params to useful variables
		sz = static_cast<LPCSTR>(pFDINotify->psz1);
		if (!::MultiByteToWideChar(CP_ACP, 0, sz, -1, wz, countof(wz)))
			ExitOnLastError1(hr, "failed to convert cabinet file id to unicode: %s", sz);

		if (pccs->pfnProgress)
		{
			hr = pccs->pfnProgress(TRUE, wz, pccs->pvContext);
			if (S_OK != hr)
				ExitFunction();
		}

		if (L'*' == *pccs->pwzExtract || 0 == lstrcmpW(pccs->pwzExtract, wz))
		{
			// get the created date for the resource in the cabinet
			FILETIME ftLocal;
			if (!::DosDateTimeToFileTime(pFDINotify->date, pFDINotify->time, &ftLocal))
				ExitOnLastError1(hr, "failed to get time for resource: %S", wz);
			LocalFileTimeToFileTime(&ftLocal, &ft);

			if (vpfnBeginFile)
			{
				hr = vpfnBeginFile(wz, &ft, pFDINotify->cb, pccs->pvContext, &ipResult);
				if (S_FALSE == hr)   // if returned skip file
				{
					hr = S_OK;
					ipResult = 0;
				}
			}
			else
			{
				WCHAR wzPath[MAX_PATH];
				hr = StringCchCopyW(wzPath, countof(wzPath), pccs->pwzExtractDir);
				ExitOnFailure2(hr, "failed to copy in extract directory: %S for file: %S", pccs->pwzExtractDir, wz);
				hr = StringCchCatW(wzPath, countof(wzPath), wz);
				ExitOnFailure2(hr, "failed to concat onto path: %S file: %S", wzPath, wz);

				ipResult = reinterpret_cast<INT_PTR>(::CreateFileW(wzPath, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL));
				if (INVALID_HANDLE_VALUE == reinterpret_cast<HANDLE>(ipResult))
					ExitOnLastError1(hr, "failed to create file: %s", wzPath);

				::SetFileTime(reinterpret_cast<HANDLE>(ipResult), &ft, &ft, &ft);   // try to set the file time (who cares if it fails)

				if (::SetFilePointer(reinterpret_cast<HANDLE>(ipResult), pFDINotify->cb, NULL, FILE_BEGIN))   // try to set the end of the file (don't worry if this fails)
				{
					if (::SetEndOfFile(reinterpret_cast<HANDLE>(ipResult)))
					{
						::SetFilePointer(reinterpret_cast<HANDLE>(ipResult), 0, NULL, FILE_BEGIN);  // reset the file pointer
					}
				}
			}

		}
		else  // resource wasn't requested, skip it
		{
			hr = S_OK;
			ipResult = 0;
		}

		break;
	case fdintCLOSE_FILE_INFO:  // resource extraction complete
		Assert(pFDINotify->hf && pFDINotify->psz1);

		// convert params to useful variables
		sz = static_cast<LPCSTR>(pFDINotify->psz1);
		if (!::MultiByteToWideChar(CP_ACP, 0, sz, -1, wz, countof(wz)))
			ExitOnLastError1(hr, "failed to convert cabinet file id to unicode: %s", sz);

		if (vpfnEndFile)
		{
			hr = vpfnEndFile(wz, pccs->pvContext, pFDINotify->hf);
		}
		else   // just close the file
		{
			::CloseHandle(reinterpret_cast<HANDLE>(pFDINotify->hf));
		}

		if (pccs->pfnProgress)
			hr = pccs->pfnProgress(FALSE, wz, pccs->pvContext);

		if (S_OK == hr && L'*' == *pccs->pwzExtract)   // if everything is okay and we're extracting all files, keep going
			ipResult = TRUE;
		else   // something went wrong or we only needed to extract one file
		{
			hr = S_OK;
			ipResult = FALSE;
			pccs->fStopExtracting = TRUE;
		}

		break;
	case fdintPARTIAL_FILE:   // no action needed for these messages, fall through
	case fdintNEXT_CABINET:
	case fdintENUMERATE:
	case fdintCABINET_INFO:
		break;
	default:
		AssertSz(FALSE, "CabExtractCallback() - unknown FDI notification command");
	};

LExit:
	return (S_OK == hr) ? ipResult : -1;
}

#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="memutil.cpp" company="Microsoft">
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
//    Memory helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


#if DEBUG
static BOOL vfMemInitialized = FALSE;
#endif

extern "C" HRESULT DAPI MemInitialize()
{
#if DEBUG
	vfMemInitialized = TRUE;
#endif
	return S_OK;
}

extern "C" void DAPI MemUninitialize()
{
#if DEBUG
	vfMemInitialized = FALSE;
#endif
}

extern "C" LPVOID DAPI MemAlloc(
	__in SIZE_T cbSize,
	__in BOOL fZero
	)
{
//	AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
	return ::HeapAlloc(::GetProcessHeap(), fZero ? HEAP_ZERO_MEMORY : 0, cbSize);
}


extern "C" LPVOID DAPI MemReAlloc(
	__in LPVOID pv,
	__in SIZE_T cbSize,
	__in BOOL fZero
	)
{
//	AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
	return ::HeapReAlloc(::GetProcessHeap(), fZero ? HEAP_ZERO_MEMORY : 0, pv, cbSize);
}


extern "C" HRESULT DAPI MemFree(
	__in LPVOID pv
	)
{
//	AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
	return ::HeapFree(::GetProcessHeap(), 0, pv) ? S_OK : E_FAIL;
}


extern "C" SIZE_T DAPI MemSize(
	__in LPVOID pv
	)
{
//	AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
	return ::HeapSize(::GetProcessHeap(), 0, pv);
}

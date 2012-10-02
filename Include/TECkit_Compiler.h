/*
	TECkit_Compiler.h

	Public API to the TECkit compiler library.

	Jonathan Kew	22-Dec-2001
					14-May-2002		added WINAPI to function declarations
					 5-Jul-2002		corrected placement of WINAPI/CALLBACK to keep MS compiler happy
*/

/*
	TECkit_Compiler.h
	Copyright (c) 2002 SIL International.
*/

#ifndef __TECkit_Compiler_H__
#define __TECkit_Compiler_H__

#include "TECkit_Common.h"

#ifdef __cplusplus
extern "C" {
#endif

#ifdef _WIN32
/* MS compiler has predefined _WIN32, so assume Windows target  */
#include <windef.h>
#else
/* not the MS compiler, so try Metrowerks' platform macros */
#if __dest_os == __win32_os
#include <windef.h>
#else
#define WINAPI
#define CALLBACK
#endif
#endif

typedef void (CALLBACK *TECkit_ErrorFn)(void* userData, char* msg, char* param, UInt32 line);

TECkit_Status
WINAPI
TECkit_Compile(char* txt, UInt32 len, Byte doCompression, TECkit_ErrorFn errFunc, void* userData, Byte** outTable, UInt32* outLen);

void
WINAPI
TECkit_DisposeCompiled(Byte* table);

UInt32
WINAPI
TECkit_GetCompilerVersion();

#ifdef __cplusplus
}
#endif

#endif

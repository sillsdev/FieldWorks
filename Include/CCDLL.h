/**************************************************************************/
/* ccdll.h  Doug Case 2/96  This is the .h file for users of CC.DLL,
 *                          the Consistent Changes DLL.  This contains
 *                          the definitions of the DLL interfaces.
 *                          This contains defines that allow this to
 *                          work with Visual C++ in 16 bit or 32 bit mode.
 *
 *                          NOTE: It is assumed that callers will use the
 *                          large memory model, and that having "FAR" in
 *                          the function definitions is redundant, so "FAR"
 *                          is not included in the definitions.
 *
 *
 *
 * PROPRIETARY RIGHTS NOTICE:  All rights reserved.  This material contains
 *		the valuable properties of Jungle Aviation and Radio Service, Inc.
 *		of Waxhaw, North Carolina, United States of America (JAARS)
 *		embodying substantial creative efforts and confidential information,
 *		ideas and expressions, no part of which may be reproduced or transmitted
 *		in any form or by any means, electronic, mechanical, or otherwise,
 *		including photocopying and recording or in connection with any
 *		information storage or retrieval system without the permission in
 *		writing from JAARS.
 *
 * COPYRIGHT NOTICE:  Copyright <C> 1980 - 1996
 *		an unpublished work by the Summer Institute of Linguistics, Inc.
 *
 *
 *
 *
 * Change History:
 *
 *  06-Feb-96      DRC Original version
 *  01-Sep-97      DAR Corrected interface for Office 97
 *                 Added interface for Debugging
 *
 *
 *
 **************************************************************************/
#if defined(WIN32)
#if defined(__BORLANDC__)
#undef WINAPI
#define WINAPI _stdcall
#endif
#endif

#ifdef __cplusplus
extern "C"
{
#endif

// define return codes from the CC DLL routines
#define CC_SUCCESS                   0
#define CC_GOT_FULL_BUFFER           0
#define CC_GOT_END_OF_DATA           1
#define CC_SUCCESS_BINARY            1
#define CC_CALL_AGAIN_FOR_MORE_DATA  2
#define CC_SYNTAX_ERROR             -2

// The following typedefs define the callback routines that
// are used by some of the CC DLL interfaces.
typedef int WINAPI CCInputProc(char * lpInputBuffer, int nInputBufferSize, long * lpUserData);

typedef int WINAPI CCOutputProc(char *lpOutputBuffer, int nOutputBufferSize, long *lpUserData);

typedef int WINAPI CCMatchLineCallback(HANDLE hCCTable, unsigned iLine);

typedef int WINAPI CCExecuteLineCallback(HANDLE hCCTable, unsigned iLine);

typedef int WINAPI CCErrorCallback(short nMsgIndex, short unsigned wParam, long unsigned lParam, long *lpUserData);

typedef int WINAPI CCCompileErrorCallback(char * lpszMessage, unsigned iLine, unsigned iCharacter);

#if !defined(_WINDLL) || defined(_WindowsExe)
// the following are the CC DLL interfaces...

int WINAPI CCLoadTable(char *lpszCCTableFile,
						   HANDLE *hpLoadHandle,
						   HINSTANCE hinstCurrent);

int WINAPI CCLoadTableFromBufferWithErrorCallback(const char *lpszBuffer,
									HANDLE FAR *hpLoadHandle,
									CCCompileErrorCallback * lpCCCompileErrorCallback);

int WINAPI CCLoadTableFromBuffer(char *lpszBuffer,
									HANDLE FAR *hpLoadHandle);

int WINAPI CCReinitializeTable(HANDLE hReHandle);

int WINAPI CCUnloadTable(HANDLE hUnlHandle);

int WINAPI CCSetDebugCallbacks(HANDLE hCCTHandle,
					 CCMatchLineCallback * lpCCMatchLineCallback,
					 CCExecuteLineCallback * lpCCExecuteLineCallback);

int WINAPI CCSetErrorCallBack(HANDLE hErrHandle,
		  CCErrorCallback * lpCCErrorCallback);

int WINAPI CCSetUpInputFilter(HANDLE hSetUpHandle,
						   CCInputProc *lpInCBFunct, long lUserInputCBData);

BOOL WINAPI CCQueryStore(HANDLE hCCTable, const char * pszStoreName, char * pszValue, unsigned nLenValue);

BOOL WINAPI CCQuerySwitch(HANDLE hCCTable, const char * pszSwitchName);

int WINAPI CCQueryInput(HANDLE hCCTable, char * pszInputBuffer, unsigned nLenBuffer);

int WINAPI CCQueryOutput(HANDLE hCCTable, char * pszOutputBuffer, unsigned nLenBuffer);

int WINAPI CCGetActiveGroups(HANDLE hCCTable, char * pszActiveGroups, unsigned nLenActiveGroups);

int WINAPI CCFlush(HANDLE hFlushHandle);

int WINAPI CCGetBuffer(HANDLE hGetHandle,
						   char *lpOutputBuffer, int *npOutBufLen);

int WINAPI CCProcessBuffer(HANDLE hProHandle,
						   char *lpInputBuffer, int nInBufLen,
						   char *lpOutputBuffer, int *npOutBufLen);

int WINAPI CCMultiProcessBuffer(HANDLE hMultiHandle,
						   char *lpInputBuffer, int nInBufLen,
						   BOOL bLastCallWithInput, char *lpOutputBuffer,
						   int *npOutBufLen);

int WINAPI CCSetUpOutputFilter (HANDLE hOutHandle,
						   CCOutputProc *lpOutCBFunct,
						   long lUserOutputCBData);

int WINAPI CCPutBuffer (HANDLE hPutHandle,
						   char *lpPutBuffer, int nInBufLen,
						   BOOL bLastBuffer);

int WINAPI CCProcessFile (HANDLE hProFileHandle,
						   char *lpInputFile, char *lpOutputFile,
						   BOOL bAppendOutput);
#ifdef __cplusplus
};   // extern "C"
#endif
#endif

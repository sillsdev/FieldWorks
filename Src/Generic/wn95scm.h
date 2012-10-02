/********************************************************************************/
/* Copyright (C) 1997 Microsoft Corp.
/********************************************************************************/

#ifndef _WN95SCMH_
#define _WN95SCMH_

#ifdef __cplusplus
extern "C"{
#endif

// Numeric constants
#define SERVICE_UNKNOWN                             0
enum enumSQLSCMCommands					   		   {SQLSCMCmd_STOP,
													SQLSCMCmd_PAUSE,
													SQLSCMCmd_START,
													SQLSCMCmd_CONTINUE};

// Function prototypes
BOOL _declspec(dllimport)   SQLSCMLocalServiceControlA(LPSTR lpszSvc,int iCommand,LPDWORD pdwErr,int iCount,void *lpStartParameter);
BOOL _declspec(dllimport)   SQLSCMLocalServiceControlW(LPWSTR lpszSvc,int iCommand,LPDWORD pdwErr,int iCount,void *lpStartParameter);
DWORD _declspec(dllimport)  SQLSCMGetLocalServiceStateA(LPSTR lpszSvc,LPDWORD pdwErr);
DWORD _declspec(dllimport)  SQLSCMGetLocalServiceStateW(LPWSTR pwszSvc,LPDWORD pdwErr);

#ifdef UNICODE
#define SQLSCMLocalServiceControl  SQLSCMLocalServiceControlW
#define SQLSCMGetLocalServiceState SQLSCMGetLocalServiceStateW
#else
#define SQLSCMLocalServiceControl  SQLSCMLocalServiceControlA
#define SQLSCMGetLocalServiceState SQLSCMGetLocalServiceStateA
#endif // !UNICODE


#ifdef __cplusplus
}
#endif

#endif

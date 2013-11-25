/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: DebugProc.h
Responsibility: TE Team

	Declarations debug utilities.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef DebugProcs_INCLUDED
#define DebugProcs_INCLUDED

typedef void (__stdcall * _DBG_REPORT_HOOK)(int, char *);
typedef void (__stdcall * Pfn_Assert)(const char * pszExp, const char * pszFile, int nLine,
	HMODULE hmod);

extern "C" _DBG_REPORT_HOOK APIENTRY DbgSetReportHook(_DBG_REPORT_HOOK hook);
extern "C" int APIENTRY DebugProcsInit(void);
extern "C" int APIENTRY DebugProcsExit(void);
extern "C" void APIENTRY ShowAssertMessageBox(int fShowMessageBox);
extern "C" Pfn_Assert APIENTRY SetAssertProc(Pfn_Assert pfnAssert);

#endif

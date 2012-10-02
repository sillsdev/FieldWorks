/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: DebugProc.h
Responsibility: TE Team

	Declarations debug utilities.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef DebugProcs_INCLUDED
#define DebugProcs_INCLUDED

typedef void (__stdcall * _DBG_REPORT_HOOK)(int, char *);

extern "C" _DBG_REPORT_HOOK APIENTRY DbgSetReportHook(_DBG_REPORT_HOOK hook);
extern "C" int APIENTRY DebugProcsInit(void);
extern "C" int APIENTRY DebugProcsExit(void);
extern "C" void APIENTRY ShowAssertMessageBox(int fShowMessageBox);

#endif

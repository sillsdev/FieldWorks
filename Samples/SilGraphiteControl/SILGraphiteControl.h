#pragma once

// SILGraphiteControl.h : main header file for SILGraphiteControl.DLL

#if !defined( __AFXCTL_H__ )
#error include 'afxctl.h' before including this file
#endif

#include "resource.h"       // main symbols


// CSILGraphiteControlApp : See SILGraphiteControl.cpp for implementation.

class CSILGraphiteControlApp : public COleControlModule
{
public:
	BOOL InitInstance();
	int ExitInstance();
};

extern const GUID CDECL _tlid;
extern const WORD _wVerMajor;
extern const WORD _wVerMinor;

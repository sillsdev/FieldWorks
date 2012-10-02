// PXPerlWrapDLL.h : main header file for the PXPerlWrapDLL DLL
//

#pragma once

#ifndef __AFXWIN_H__
	#error include 'stdafx.h' before including this file for PCH
#endif

#include "resource.h"		// main symbols


// CPXPerlWrapDLLApp
// See PXPerlWrapDLL.cpp for the implementation of this class
//

class CPXPerlWrapDLLApp : public CWinApp
{
public:
	CPXPerlWrapDLLApp();

// Overrides
public:
	virtual BOOL InitInstance();

	DECLARE_MESSAGE_MAP()
};

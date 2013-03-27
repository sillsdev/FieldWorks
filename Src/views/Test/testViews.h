/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testViews.h
Responsibility:
Last reviewed:

	Global header for unit testing the Views DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTVIEWS_H_INCLUDED
#define TESTVIEWS_H_INCLUDED

#pragma once
#include "Main.h"

namespace TestViews
{
	extern ILgWritingSystemFactoryPtr g_qwsf;
	extern int g_wsEng;
	extern int g_wsFrn;
	extern int g_wsGer;

	void CreateTestWritingSystemFactory();
	void CloseTestWritingSystemFactory();

	// These functions are used for test setup and teardown (only)

#ifdef WIN32

	inline HDC GetTestDC()
	{
		return ::GetDC(NULL);
	}

	inline int ReleaseTestDC(HDC hdc)
	{
		return ::ReleaseDC(NULL, hdc);
	}

#else //WIN32

	inline HDC GetTestDC()
	{
		return 0; // Linux doesn't need a DC
	}

	inline int ReleaseTestDC(HDC)
	{
		return 0;
	}

#endif //WIN32
};

#include <unit++.h>
#include "DummyRootsite.h"
#include "DummyBaseVc.h"
#include "MockLgWritingSystemFactory.h"

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*TESTVIEWS_H_INCLUDED*/

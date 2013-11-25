/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

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

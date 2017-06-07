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

#ifdef WIN32
#define NEWLINE "\r\n"
#else
#define NEWLINE "\n"
#endif

namespace TestViews
{
	static const StrUni g_pszTest(L"This is a test!");
	static const int g_cchTest = 15;
	static const StrUni g_pszTest2(L"TESTING");
	static const int g_cchTest2 = 7;

	const StrUni kszEng(L"en");
	const StrUni kszTest(L"test");
	const StrUni kszTest2(L"tst2");

	extern ILgWritingSystemFactoryPtr g_qwsf;
	extern int g_wsEng;
	extern int g_wsFrn;
	extern int g_wsGer;
	extern int g_wsTest;
	extern int g_wsTest2;

	enum
	{
		// Arbitrary values chosen more or less at random...
		kwsENG = 25,
		kwsSPN = 26,
		kwsSTK = 123456789
	};

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
#include "MockRenderEngineFactory.h"

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*TESTVIEWS_H_INCLUDED*/

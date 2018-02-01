/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: testFwKernel.h
Responsibility:
Last reviewed:

	Global header for unit testing the FwKernel DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTFWKERNEL_H_INCLUDED
#define TESTFWKERNEL_H_INCLUDED

#pragma once
#include "Main.h"
#include "MockLgWritingSystemFactory.h"
#include <unit++.h>

#if WIN32
#define NEWLINE "\r\n"
#else
#define NEWLINE "\n"
#endif

// Static data shared by various test suites.
namespace TestFwKernel
{
	static const StrUni g_pszTest(L"This is a test!");
	static const int g_cchTest = 15;
	static const StrUni g_pszTest2(L"TESTING");
	static const int g_cchTest2 = 7;

	const StrUni kszEng(L"en");
	const StrUni kszTest(L"test");
	const StrUni kszTest2(L"tst2");

	extern int g_wsEng;
	extern int g_wsTest;
	extern int g_wsTest2;

	enum
	{
		// Arbitrary values chosen more or less at random...
		kwsENG = 25,
		kwsSPN = 26,
		kwsSTK = 123456789
	};
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkfwk-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*TESTFWKERNEL_H_INCLUDED*/
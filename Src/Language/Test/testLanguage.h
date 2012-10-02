/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testLanguage.h
Responsibility:
Last reviewed:

	Global header for unit testing the Language DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLANGUAGE_H_INCLUDED
#define TESTLANGUAGE_H_INCLUDED

#pragma once
#include "Main.h"
#include <unit++.h>

#if !WIN32
// Stream insertion operators for FW string types
namespace std
{
	   inline std::ostream& operator << (std::ostream& stream, const OLECHAR* text)
	   {
			   return stream << StrAnsi(text).Chars();
	   }

	   inline std::ostream& operator << (std::ostream& stream, const StrUni& text)
	   {
			   return stream << text.Chars();
	   }
}
#endif //!WIN32

namespace TestLanguage
{
	const StrUni kszEng(L"en");
	const StrUni kszTest(L"test");
	const StrUni kszTest2(L"tst2");

	extern int g_wsEng;
	extern int g_wsTest;
	extern int g_wsTest2;
}


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*TESTLANGUAGE_H_INCLUDED*/

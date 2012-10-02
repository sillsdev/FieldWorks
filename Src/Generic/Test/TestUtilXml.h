/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestUtilXml.h
Responsibility:
Last reviewed:

	Unit tests for the functions/classes from Generic/UtilXml.cpp
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTUTILXML_H_INCLUDED
#define TESTUTILXML_H_INCLUDED

#pragma once

#include "testGenericLib.h"

namespace TestGenericLib
{
	class TestUtilXml : public unitpp::suite
	{
#define LATIN_CAPITAL_A L"\x0041"
#define COMBINING_DIAERESIS L"\x0308" // cc 230
#define COMBINING_MACRON L"\x0304" // cc 230
#define A_WITH_DIAERESIS L"\x00C4" // decomposes to 0041 0308
#define A_WITH_DIAERESIS_AND_MACRON L"\x01DE"	// decomposes to 00C4 0304 and hence to
												// 0041 0308 0304
#define SMALL_A L"\x0061"
#define COMBINING_DOT_BELOW L"\x0323" // cc 220
#define a_WITH_DOT_BELOW L"\x1EA1" // decomposes to 0061 0323
#define COMBINING_OVERLINE L"\x0305" // not involved in any compositions with characters; cc 230
#define COMBINING_LEFT_HALF_RING_BELOW L"\x031C" // not involved in any compositions; cc 220.
#define SPACE L"\x0020"
#define COMBINING_BREVE L"\x0306" // cc 230
#define BREVE L"\x02D8" // compatibility decomposition to 0020 0306
#define a_WITH_DIAERESIS L"\x00E4" // decomposes to 0061 0308.
#define a_WITH_DIAERESIS_AND_MACRON L"\x01DF"
#define MUSICAL_SYMBOL_MINIMA L"\xD834\xDDBB" // 1D1BB decomposes to 1D1B9 1D165
#define MUSICAL_SYMBOL_SEMIBREVIS_WHITE L"\xD834\xDDB9" // 1D1B9
#define MUSICAL_SYMBOL_COMBINING_STEM L"\xD834\xDD65" // 1D165

		void testWriteXmlUnicode()
		{
			StrUni stuInput = L"a" COMBINING_DIAERESIS COMBINING_DIAERESIS COMBINING_DIAERESIS
				COMBINING_DOT_BELOW a_WITH_DIAERESIS COMBINING_DOT_BELOW;
			// knmNFC : expand, reorder, compose =>
			// a_WITH_DOT_BELOW COMBINING_DIAERESIS COMBINING_DIAERESIS COMBINING_DIAERESIS
			//		a_WITH_DOT_BELOW COMBINING_DIAERESIS
			StrAnsi staOutput1 = "\xE1\xBA\xA1\xCC\x88\xCC\x88\xCC\x88\xE1\xBA\xA1\xCC\x88";
			StrAnsiStreamPtr qstas1;
			StrAnsiStream::Create(&qstas1);
			WriteXmlUnicode(qstas1.Ptr(), stuInput.Chars(), stuInput.Length());
			unitpp::assert_true("WriteAsXml test 1", staOutput1 == qstas1->m_sta);
		}

	public:
		TestUtilXml();
	};
}

#include <Vector_i.cpp>
template Vector<char>;

#endif /*TESTUTILXML_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkGenLib-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
